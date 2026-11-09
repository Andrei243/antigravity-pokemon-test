using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.OpenWorld;

namespace PokemonPlatinumEngine.Graphics.OpenWorld;

/// <summary>
/// The open world's trees and grass (plan 32 · OW9; style guide, "Open world", "Trees" and "Grass"; decision 15): the
/// battle's soft pines and round trees, three shapes of each, every tree nearer the camera than
/// <see cref="TreeMeshReach"/> drawn as its mesh by a transform each (one draw a shape, <c>DrawMeshInstanced</c>, near ones
/// first) and cast into the shadow maps, and every tree beyond it, the map's and the far land's, drawn as an impostor:
/// a card the program turns to the camera, showing the one of its eight views baked once (<see cref="BakeImpostors"/>)
/// nearest the camera's side of it. The map's own impostors stand in one mesh made when the map is, and the program
/// hides those nearer than the reach. The grass's tufts (<see cref="OpenGrass"/>) of the chunks on the card are drawn
/// by a transform each within <see cref="OpenGrass.Reach"/> of the player, parting round whoever walks through them.
/// Every load through <see cref="Gpu"/>, under <c>opentree</c>.
/// </summary>
internal sealed class OpenFoliage
{
    /// <summary>Nearer the camera than this a tree is its mesh, past it its impostor, in metres (the plan's 120 m; decision 15 would bring it to 70 if the floor asks).</summary>
    public const float TreeMeshReach = 120f;

    /// <summary>A tree of size one stands this much taller than the battle's soft tree, which was made for the chibi's scale.</summary>
    public const float TreeScale = 1.5f;

    /// <summary>An impostor's view, in texels, and the atlas's views across.</summary>
    public const int ViewWidth = 128, ViewHeight = 160, Views = 8;

    /// <summary>The atlas's rows: the pines' three shapes, then the round trees'.</summary>
    public const int Rows = 2 * OpenTree.Variants;

    private readonly Mesh[] trees = new Mesh[Rows];
    private readonly int[] treeTriangles = new int[Rows];
    private readonly Mesh[] tufts = new Mesh[OpenGrass.Variants];
    private Material instanced, instancedDepth, impostor;
    private Texture2D atlas;
    private bool loaded, baked;

    // Each row's frame: the view's height and width in metres at size one (as the bake framed it)
    private readonly (float Height, float Width)[] frames = new (float, float)[Rows];

    // The map's trees, their feet found, and the impostors of them and of the far land
    private OpenMap? map;
    private readonly List<(Vector3 Foot, float Scale, float Yaw, int Row)> placed = new();
    private Mesh mapImpostors, farImpostors;
    private bool mapImpostorsLoaded, farImpostorsLoaded;

    // A frame's near trees and tufts, a list of transforms a shape
    private readonly List<(float D, Matrix4x4 M)>[] near = new List<(float, Matrix4x4)>[Rows];
    private readonly List<Matrix4x4>[] grass = new List<Matrix4x4>[OpenGrass.Variants];
    private Matrix4x4[] buffer = new Matrix4x4[256];

    // What the last frame drew, for the harness
    public int NearTrees { get; private set; }
    public int NearTreeTriangles { get; private set; }
    public int FarTrees { get; private set; }
    public int Tufts { get; private set; }
    public int MapTrees => placed.Count;

    /// <summary>The impostors' atlas as baked, for the harness to write out and look at.</summary>
    public PixelCanvas? Atlas { get; private set; }

    public OpenFoliage()
    {
        for (int i = 0; i < Rows; i++) near[i] = new();
        for (int i = 0; i < OpenGrass.Variants; i++) grass[i] = new();
    }

    /// <summary>The meshes and materials, made the first time (the window's thread).</summary>
    public void Ensure(OpenShaders shaders)
    {
        if (loaded) return;
        for (int row = 0; row < Rows; row++)
        {
            var b = TreeOf(row);
            treeTriangles[row] = b.VertexCount / 3;
            trees[row] = b.Upload("opentree");
        }
        for (int v = 0; v < OpenGrass.Variants; v++)
        {
            var b = new MeshBuilder();
            var (root, tip) = OpenGrass.ColoursOf(v);
            SoftFoliage.Tuft(b, 0, 0, 0.5f, 0.42f, 11 + v * 7, root, tip);
            tufts[v] = b.Upload("opentree");
        }
        instanced = RenderContext.MaterialFor(shaders.Instanced, SceneTextures.White);
        instancedDepth = RenderContext.MaterialFor(shaders.InstancedDepth, SceneTextures.White);
        loaded = true;
    }

    /// <summary>A tree's shape of a row: the soft pine's or round tree's of the row's seed, standing at the origin.</summary>
    private static MeshBuilder TreeOf(int row)
    {
        var batches = new MeshBatches();
        int variant = row % OpenTree.Variants;
        if (row < OpenTree.Variants) TreeModels.Pine(batches, 0, 0, 3 + variant * 5, 17 + variant * 3, 1f, soft: true);
        else TreeModels.Round(batches, 0, 0, 3 + variant * 5, 17 + variant * 3, 1f, soft: true);
        var all = new MeshBuilder();
        foreach (var (_, _, _, builder) in batches.All) all.Append(builder, Matrix4x4.Identity);
        return all;
    }

    /// <summary>
    /// The eight views of each shape into the atlas, once (outside any texture mode): each shape lit from above and a
    /// little toward the camera, framed whole, as the gate's prototype baked them. The field's world program draws them.
    /// </summary>
    public void BakeImpostors(RenderContext context, OpenShaders shaders)
    {
        if (baked) return;
        var fs = context.Shaders;
        var target = Gpu.Target(ViewWidth, ViewHeight, "opentree");
        var canvas = new PixelCanvas(ViewWidth * Views, ViewHeight * Rows);
        for (int row = 0; row < Rows; row++)
        {
            var batches = new MeshBatches();
            int variant = row % OpenTree.Variants;
            if (row < OpenTree.Variants) TreeModels.Pine(batches, 0, 0, 3 + variant * 5, 17 + variant * 3, 1f, soft: true);
            else TreeModels.Round(batches, 0, 0, 3 + variant * 5, 17 + variant * 3, 1f, soft: true);
            float top = 0, half = 0;
            foreach (var (_, _, _, b) in batches.All)
            {
                var (min, max) = b.Bounds();
                top = MathF.Max(top, max.Y);
                half = MathF.Max(half, MathF.Max(MathF.Max(-min.X, max.X), MathF.Max(-min.Z, max.Z)));
            }
            var meshes = SceneMeshes.Upload(batches, fs);
            float frameH = MathF.Max(top * 1.06f, half * 2.1f * ViewHeight / ViewWidth);
            frames[row] = (frameH, frameH * ViewWidth / ViewHeight);
            for (int k = 0; k < Views; k++)
            {
                float a = k * MathF.PI / 4f;
                var look = new Vector3(0, frameH / 2f, 0);
                var camera = new Camera3D(look + new Vector3(MathF.Sin(a), 0.14f, MathF.Cos(a)) * 40f, look, Vector3.UnitY, frameH, CameraProjection.Orthographic);
                var light = new SceneLighting(Vector3.Normalize(new Vector3(MathF.Sin(a) * 0.35f, 1f, MathF.Cos(a) * 0.35f)),
                    new Vector3(0.3f, 0.3f, 0.28f), new Vector3(0.72f, 0.74f, 0.76f), new Vector3(0.6f, 0.6f, 0.58f));
                Raylib.BeginTextureMode(context.Shadows.Target);
                Raylib.ClearBackground(Color.White);
                Raylib.EndTextureMode();
                Raylib.BeginTextureMode(target);
                Raylib.ClearBackground(new Color(0, 0, 0, 0));
                Rlgl.SetClipPlanes(1.0, 100.0);
                Raylib.BeginMode3D(camera);
                fs.SetStudio();
                fs.SetLighting(Matrix4x4.Identity, light, camera.Position, 1f);
                fs.SetCharacterStyle(shadowStrength: 0f, rimStrength: 0f);
                fs.SetWorldRamp(ArtLook.BattleRamp);
                fs.SetGlow(0f, 0f, Vector3.Zero);
                fs.SetUpright(0f, 0f);
                context.BindShadowMap();
                Rlgl.DisableBackfaceCulling();
                meshes.Draw();
                Rlgl.EnableBackfaceCulling();
                Raylib.EndMode3D();
                Raylib.EndTextureMode();
                context.UnbindShadowMap();
                Rlgl.SetClipPlanes(0.01, 1000.0);
                var image = Raylib.LoadImageFromTexture(target.Texture);
                Raylib.ImageFlipVertical(ref image);
                var frame = PixelCanvas.FromImage(image);
                Raylib.UnloadImage(image);
                canvas.Blit(frame, k * ViewWidth, row * ViewHeight);
            }
            meshes.Unload();
        }
        Gpu.Unload(target);
        atlas = canvas.ToTexture(1, "opentree");
        Raylib.SetTextureWrap(atlas, TextureWrap.Clamp);
        Raylib.GenTextureMipmaps(ref atlas);
        Raylib.SetTextureFilter(atlas, TextureFilter.Trilinear);
        impostor = RenderContext.MaterialFor(shaders.Impostor, atlas);
        Atlas = canvas;
        baked = true;
    }

    /// <summary>
    /// The map's trees, made once a map (the window's thread): each tree's foot found on the ground the capsule
    /// collides with (the highest ground facing up, or the ground nearest a tree's own height), the impostors of all of
    /// them in one mesh, and the far land's trees (<paramref name="far"/>) in another.
    /// </summary>
    public void Use(OpenMap? next, IReadOnlyList<OpenFarTree>? far)
    {
        if (ReferenceEquals(next, map)) return;
        ForgetMap();
        map = next;
        if (next == null) return;
        var ground = next.Ground;
        foreach (var t in next.Trees)
        {
            double x = t.X / (double)OpenPos.PerMetre, z = t.Z / (double)OpenPos.PerMetre;
            var foot = t.Y is { } y
                ? ground.Footing(x, z, y / (double)OpenPos.PerMetre + 3, y / (double)OpenPos.PerMetre - 3)
                : ground.Footing(x, z, 2000, -2000);
            if (foot is not { } f) continue;
            int row = (t.Kind == OpenTreeKind.Pine ? 0 : OpenTree.Variants) + Math.Clamp(t.Variant, 0, OpenTree.Variants - 1);
            // A little into the ground, so no tree stands over a dip of the triangle it is on
            placed.Add((new Vector3((float)x, (float)f.Y - 0.05f, (float)z), t.Scale * TreeScale, t.Yaw / (float)OpenYaw.Turn * MathF.Tau, row));
        }
        if (placed.Count > 0)
        {
            mapImpostors = ImpostorMesh(placed.ConvertAll(p => (p.Foot, p.Scale, p.Row)));
            mapImpostorsLoaded = true;
        }
        if (far is { Count: > 0 })
        {
            farImpostors = ImpostorMesh(new List<OpenFarTree>(far).ConvertAll(t => (t.Foot, t.Scale * TreeScale, t.Row)));
            farImpostorsLoaded = true;
        }
    }

    /// <summary>The impostors of some trees as one mesh: each a card of two triangles on its foot, its size and row in its normal.</summary>
    private Mesh ImpostorMesh(List<(Vector3 Foot, float Scale, int Row)> list)
    {
        var v = new OpenVertices(list.Count * 6);
        Vector2[] corners = { new(0, 0), new(1, 0), new(1, 1), new(0, 0), new(1, 1), new(0, 1) };
        foreach (var (foot, scale, row) in list)
        {
            var (h, w) = frames[row];
            var size = new Vector3(w * scale, h * scale, row);
            foreach (var c in corners) v.Add(foot, size, c, Color.White);
        }
        var mesh = v.Pack();
        Gpu.Upload(ref mesh, "opentree");
        return mesh;
    }

    /// <summary>
    /// The frame's near trees (within <see cref="TreeMeshReach"/> of the eye, nearest first, a list a shape) and tufts
    /// (within <see cref="OpenGrass.Reach"/> of the player's feet, from the chunks on the card at level 0).
    /// </summary>
    public void Prepare(Vector3 eye, Vector3 feet, IEnumerable<OpenChunkMeshes> chunks)
    {
        foreach (var list in near) list.Clear();
        foreach (var list in grass) list.Clear();
        NearTrees = 0;
        NearTreeTriangles = 0;
        foreach (var (foot, scale, yaw, row) in placed)
        {
            float d = Vector2.Distance(new Vector2(foot.X, foot.Z), new Vector2(eye.X, eye.Z));
            if (d >= TreeMeshReach) continue;
            near[row].Add((d, Matrix4x4.CreateScale(scale) * Matrix4x4.CreateRotationY(yaw) * Matrix4x4.CreateTranslation(foot)));
            NearTrees++;
            NearTreeTriangles += treeTriangles[row];
        }
        foreach (var list in near) list.Sort((a, b) => a.D.CompareTo(b.D));
        Tufts = 0;
        foreach (var chunk in chunks)
        {
            if (chunk.Level != 0 || chunk.Tufts.Count == 0) continue;
            float cx = Math.Clamp(feet.X, chunk.Min.X, chunk.Max.X), cz = Math.Clamp(feet.Z, chunk.Min.Z, chunk.Max.Z);
            if (Vector2.Distance(new Vector2(cx, cz), new Vector2(feet.X, feet.Z)) >= OpenGrass.Reach) continue;
            foreach (var t in chunk.Tufts)
            {
                float d = Vector2.Distance(new Vector2(t.At.X, t.At.Z), new Vector2(feet.X, feet.Z));
                float shown = OpenGrass.Shown(d);
                if (shown <= 0.02f) continue;
                grass[t.Variant].Add(Matrix4x4.CreateScale(t.Scale * shown) * Matrix4x4.CreateRotationY(t.Yaw) * Matrix4x4.CreateTranslation(t.At));
                Tufts++;
            }
        }
    }

    /// <summary>The near trees into a shadow map, those whose box <paramref name="casts"/> says can cast into it.</summary>
    public int DrawDepth(Func<Vector3, Vector3, bool> casts)
    {
        int triangles = 0;
        for (int row = 0; row < Rows; row++)
        {
            int n = 0;
            foreach (var (_, m) in near[row])
            {
                var at = m.Translation;
                if (!casts(at - new Vector3(3, 0, 3), at + new Vector3(3, 9, 3))) continue;
                Put(ref n, m);
            }
            if (n == 0) continue;
            Raylib.DrawMeshInstanced(trees[row], instancedDepth, buffer, n);
            triangles += n * treeTriangles[row];
        }
        return triangles;
    }

    /// <summary>The near trees and the tufts, in the scene's pass (the scenery's uniforms set): opaque, so the near first saves shading.</summary>
    public void Draw()
    {
        for (int row = 0; row < Rows; row++)
        {
            int n = 0;
            foreach (var (_, m) in near[row]) Put(ref n, m);
            if (n == 0) continue;
            Raylib.DrawMeshInstanced(trees[row], instanced, buffer, n);
            FrameProfiler.Count(n * treeTriangles[row]);
        }
        Rlgl.DisableBackfaceCulling();
        for (int v = 0; v < OpenGrass.Variants; v++)
        {
            int n = 0;
            foreach (var m in grass[v]) Put(ref n, m);
            if (n == 0) continue;
            Raylib.DrawMeshInstanced(tufts[v], instanced, buffer, n);
            FrameProfiler.Count(n * tufts[v].TriangleCount);
        }
        Rlgl.EnableBackfaceCulling();
    }

    /// <summary>The far trees' impostors: the map's beyond the reach, and the far land's, every one (the impostor program's uniforms set).</summary>
    public void DrawImpostors(OpenShaders shaders, Vector3 eye)
    {
        FarTrees = 0;
        if (!baked) return;
        if (mapImpostorsLoaded)
        {
            shaders.SetImpostors(eye, TreeMeshReach, Views, Rows);
            Raylib.DrawMesh(mapImpostors, impostor, Matrix4x4.Identity);
            FarTrees += Math.Max(0, placed.Count - NearTrees);
        }
        if (farImpostorsLoaded)
        {
            shaders.SetImpostors(eye, 0f, Views, Rows);
            Raylib.DrawMesh(farImpostors, impostor, Matrix4x4.Identity);
            FarTrees += farImpostors.VertexCount / 6;
        }
    }

    // A transform into the buffer raylib takes them from (transposed, as raylib's own matrices are laid out)
    private void Put(ref int n, Matrix4x4 m)
    {
        if (n == buffer.Length) Array.Resize(ref buffer, buffer.Length * 2);
        buffer[n++] = Matrix4x4.Transpose(m);
    }

    private void ForgetMap()
    {
        if (mapImpostorsLoaded) Gpu.Unload(mapImpostors);
        if (farImpostorsLoaded) Gpu.Unload(farImpostors);
        mapImpostorsLoaded = farImpostorsLoaded = false;
        placed.Clear();
        map = null;
    }

    public unsafe void Unload()
    {
        ForgetMap();
        if (loaded)
        {
            foreach (var m in trees) Gpu.Unload(m);
            foreach (var m in tufts) Gpu.Unload(m);
            Raylib.MemFree(instanced.Maps);
            Raylib.MemFree(instancedDepth.Maps);
        }
        if (baked)
        {
            Gpu.Unload(atlas);
            Raylib.MemFree(impostor.Maps);
        }
        loaded = baked = false;
    }
}
