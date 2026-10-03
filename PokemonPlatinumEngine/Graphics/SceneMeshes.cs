using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// How a batch of static geometry is shaded. <see cref="Light"/> and <see cref="HomeLight"/> are the pools and
/// halos added on top after dark: the first for lights that burn all night, the second for the windows of homes.
/// </summary>
internal enum MeshPass { Opaque, Ground, Water, SoftWater, Light, HomeLight }

/// <summary>A rectangle of a map's tiles: a chunk of a streamed map, or the part of a map a scene covers.</summary>
internal readonly record struct TileWindow(int X, int Y, int Width, int Height)
{
    public int Right => X + Width;
    public int Bottom => Y + Height;
    public bool Contains(int x, int y) => x >= X && y >= Y && x < Right && y < Bottom;
}

/// <summary>A rectangle of ground, x and z in tiles: what the camera can see, or where shadows can reach it from.</summary>
internal readonly record struct GroundRect(float MinX, float MinZ, float MaxX, float MaxZ)
{
    public bool Touches(Vector3 min, Vector3 max) => max.X >= MinX && min.X <= MaxX && max.Z >= MinZ && min.Z <= MaxZ;
}

/// <summary>Collects static geometry into one mesh per texture and pass, which keeps draw calls low.</summary>
internal sealed class MeshBatches
{
    private readonly Dictionary<(uint, MeshPass, int), (Texture2D Tex, MeshBuilder Builder)> map = new();

    /// <summary>
    /// While this is not 0, geometry goes into a mesh of its own for that chunk of the map, and a chunk is only
    /// drawn when it is in view. For what there is a great deal of across a map: its trees.
    /// </summary>
    public int Chunk { get; set; }

    /// <summary>The chunk a tile belongs to: squares of eight tiles.</summary>
    public static int ChunkOf(int tileX, int tileY) => 1 + ((tileX + 64) >> 3) + ((tileY + 64) >> 3) * 256;

    public MeshBuilder For(Texture2D tex, MeshPass pass = MeshPass.Opaque)
    {
        if (!map.TryGetValue((tex.Id, pass, Chunk), out var entry))
        {
            entry = (tex, new MeshBuilder());
            map[(tex.Id, pass, Chunk)] = entry;
        }
        return entry.Builder;
    }

    /// <summary>Adds geometry that was built before its texture existed (an art sheet is uploaded last).</summary>
    public void Attach(Texture2D tex, MeshPass pass, MeshBuilder builder)
    {
        if (builder.VertexCount > 0) For(tex, pass).Append(builder, Matrix4x4.Identity);
    }

    public IEnumerable<(Texture2D Tex, MeshPass Pass, bool Chunked, MeshBuilder Builder)> All
    {
        get { foreach (var (key, e) in map) yield return (e.Tex, key.Item2, key.Item3 != 0, e.Builder); }
    }
}

/// <summary>
/// Uploaded static scenery: each batch drawn with the lit scene shader (or a water shader), and the opaque
/// batches drawn again into the shadow map. Texels marked as lit glass glow after dark (see
/// <see cref="FieldShaders.SetGlow"/>), and the light batches are added on top.
/// </summary>
internal sealed class SceneMeshes
{
    /// <param name="Bounds">Set for a chunk of the map, which is skipped when out of view; null for what is always drawn.</param>
    /// <param name="Prepass">Set for foliage, which lays down its depth before it is shaded (see <see cref="Draw"/>).</param>
    private sealed record Part(Mesh Mesh, Material Main, Material? Depth, (Vector3 Min, Vector3 Max)? Bounds, Material? Prepass);
    private readonly List<Part> parts = new();
    private readonly List<(Mesh Mesh, Material Material, bool Home)> lights = new();

    public static SceneMeshes Upload(MeshBatches batches, FieldShaders shaders)
    {
        var result = new SceneMeshes();
        foreach (var (tex, pass, chunked, builder) in batches.All)
        {
            if (builder.VertexCount == 0) continue;
            result.Add(builder.Upload(), tex, pass, chunked ? builder.Bounds() : null, shaders);
        }
        return result;
    }

    /// <summary>Adds a mesh that is already on the GPU, to be drawn with a texture in a pass.</summary>
    /// <param name="bounds">Set for one of the map's chunks of trees, which is skipped when out of view and gets a depth pre-pass.</param>
    public void Add(Mesh mesh, Texture2D tex, MeshPass pass, (Vector3 Min, Vector3 Max)? bounds, FieldShaders shaders)
    {
        if (pass is MeshPass.Light or MeshPass.HomeLight)
        {
            // Lights are unlit and added on top of the scene
            lights.Add((mesh, RenderContext.MaterialFor(shaders.Light, tex), pass == MeshPass.HomeLight));
            return;
        }
        var shader = pass == MeshPass.Water ? shaders.Water : pass == MeshPass.SoftWater ? shaders.SoftWater : shaders.World;
        var main = RenderContext.MaterialFor(shader, tex);
        Material? depth = pass == MeshPass.Opaque ? RenderContext.MaterialFor(shaders.Depth, tex) : null;
        // Chunks are the map's trees: many layers of leaves over each other, so they get a depth pre-pass
        Material? prepass = bounds != null && pass == MeshPass.Opaque ? RenderContext.MaterialFor(shaders.Prepass, tex) : null;
        parts.Add(new Part(mesh, main, depth, bounds, prepass));
    }

    private static bool Hidden(Part part, GroundRect? within) =>
        within.HasValue && part.Bounds is { } b && !within.Value.Touches(b.Min, b.Max);

    /// <summary>
    /// Draws the scenery. Foliage goes first, as depth alone: a forest is many cut-out layers over each other,
    /// and shading every layer costs a couple of milliseconds. With the depth laid down (and not written again),
    /// only the leaves that end up visible are shaded, and the ground beneath them is skipped too.
    /// </summary>
    /// <param name="view">The ground the camera can see; chunks of the map outside it are skipped. Null draws everything.</param>
    public void Draw(GroundRect? view = null)
    {
        bool foliage = false;
        foreach (var part in parts)
        {
            if (!part.Prepass.HasValue || Hidden(part, view)) continue;
            if (!foliage)
            {
                Rlgl.DrawRenderBatchActive();
                Rlgl.ColorMask(false, false, false, false);
                foliage = true;
            }
            Raylib.DrawMesh(part.Mesh, part.Prepass.Value, Matrix4x4.Identity);
        }
        if (foliage) Rlgl.ColorMask(true, true, true, true);

        foreach (var part in parts)
        {
            if (Hidden(part, view)) continue;
            if (part.Prepass.HasValue) Rlgl.DisableDepthMask();
            Raylib.DrawMesh(part.Mesh, part.Main, Matrix4x4.Identity);
            if (part.Prepass.HasValue) Rlgl.EnableDepthMask();
        }
    }

    /// <summary>
    /// Adds the light that lamps, doors and windows throw, each kind at its own strength (0 to 1) and all of it
    /// tinted by <paramref name="tint"/>.
    /// </summary>
    public unsafe void DrawLights(float publicLevel, float homeLevel, Vector3 tint)
    {
        if (lights.Count == 0 || (publicLevel <= 0.01f && homeLevel <= 0.01f)) return;
        Rlgl.DrawRenderBatchActive();
        Rlgl.SetBlendMode(BlendMode.Additive);
        Rlgl.DisableDepthMask();
        foreach (var (mesh, material, home) in lights)
        {
            float level = Math.Clamp(home ? homeLevel : publicLevel, 0f, 1f);
            if (level <= 0.01f) continue;
            // Additive blending multiplies by alpha, so the level goes there alone
            material.Maps[(int)MaterialMapIndex.Albedo].Color = new Color(
                (int)(255 * Math.Clamp(tint.X, 0f, 1f)), (int)(255 * Math.Clamp(tint.Y, 0f, 1f)),
                (int)(255 * Math.Clamp(tint.Z, 0f, 1f)), (int)(255 * level));
            Raylib.DrawMesh(mesh, material, Matrix4x4.Identity);
        }
        Rlgl.DrawRenderBatchActive();
        Rlgl.SetBlendMode(BlendMode.Alpha);
        Rlgl.EnableDepthMask();
    }

    /// <summary>Draws everything that casts shadows, for the shadow-map pass.</summary>
    /// <param name="casters">The ground whose shadows can reach the view; chunks outside it are skipped. Null draws everything.</param>
    public void DrawDepth(GroundRect? casters = null)
    {
        foreach (var part in parts)
        {
            if (part.Depth.HasValue && !Hidden(part, casters)) Raylib.DrawMesh(part.Mesh, part.Depth.Value, Matrix4x4.Identity);
        }
    }

    /// <summary>
    /// Frees the meshes when a chunk of the world is left behind. The materials' shaders and textures are shared
    /// or belong to the scene, so only each material's own table of maps is freed: unloading a material whole
    /// would take the shader with it.
    /// </summary>
    public unsafe void Unload()
    {
        static void Free(Material? material)
        {
            if (material is { } m && m.Maps != null) Raylib.MemFree(m.Maps);
        }
        foreach (var part in parts)
        {
            Raylib.UnloadMesh(part.Mesh);
            Free(part.Main);
            Free(part.Depth);
            Free(part.Prepass);
        }
        foreach (var (mesh, material, _) in lights)
        {
            Raylib.UnloadMesh(mesh);
            Free(material);
        }
        parts.Clear();
        lights.Clear();
    }
}

/// <summary>Trees and foliage cards shared by the field maps and the battle stage.</summary>
internal static class TreeModels
{
    private static readonly Vector3 Up = Vector3.UnitY;

    private static float Rand(int x, int y, int salt) => GroundBaker.Rand01(x, y, salt);

    /// <summary>Sinnoh pine: four tiers of needled cones with serrated, cut-out rims.</summary>
    public static void Pine(MeshBatches batches, float cx, float cz, int seedX, int seedY, float scale = 1f, bool soft = false)
    {
        if (soft)
        {
            SoftFoliage.Pine(batches, cx, cz, seedX, seedY, scale);
            return;
        }
        float s = (0.92f + Rand(seedX, seedY, 3) * 0.16f) * scale;
        batches.For(SceneTextures.Bark).Cylinder(new(cx, 0, cz), 0.12f * scale, 0.08f * scale, 1.1f * s, 7, Color.White, cap: false);

        var tint = PixelCanvas.Mix(new Color(104, 178, 118, 255), new Color(126, 196, 128, 255), Rand(seedX, seedY, 4));
        var b = batches.For(SceneTextures.Needles);
        (float BaseY, float ApexY, float R)[] tiers =
        {
            (0.5f, 2.0f, 0.7f), (1.25f, 2.7f, 0.6f), (2.0f, 3.4f, 0.48f), (2.75f, 4.1f, 0.34f)
        };
        const int segments = 14;
        float spin = Rand(seedX, seedY, 5) * MathF.Tau;
        for (int k = 0; k < tiers.Length; k++)
        {
            var (baseY, apexY, radius) = tiers[k];
            float h = (apexY - baseY) * s, r = radius * s;
            var apex = new Vector3(cx, apexY * s, cz);
            // Lower tiers are a touch darker; everything sways a little, more toward the top
            var tierColor = MeshBuilder.Scale(tint, 0.86f + k * 0.06f);
            float sway = 0.12f + k * 0.08f;

            for (int i = 0; i < segments; i++)
            {
                float a0 = spin + i * MathF.Tau / segments, a1 = spin + (i + 1) * MathF.Tau / segments;
                var d0 = new Vector3(MathF.Cos(a0), 0, MathF.Sin(a0));
                var d1 = new Vector3(MathF.Cos(a1), 0, MathF.Sin(a1));
                var rim0 = new Vector3(cx, baseY * s, cz) + d0 * r;
                var rim1 = new Vector3(cx, baseY * s, cz) + d1 * r;
                var n0 = Vector3.Normalize(d0 * h + Up * r);
                var n1 = Vector3.Normalize(d1 * h + Up * r);
                var na = Vector3.Normalize(n0 + n1);
                float u0 = i / (float)segments * 3f, u1 = (i + 1) / (float)segments * 3f;
                b.Tri(apex, rim0, rim1, new((u0 + u1) / 2f, 0), new(u0, 1), new(u1, 1), na, n0, n1,
                    MeshBuilder.Sway(PixelCanvas.Light1(tierColor, 0.15f), sway), MeshBuilder.Sway(tierColor, sway * 0.6f), MeshBuilder.Sway(tierColor, sway * 0.6f));
            }
        }
    }

    /// <summary>Round broadleaf tree: a few leafy clumps on a short trunk.</summary>
    public static void Round(MeshBatches batches, float cx, float cz, int seedX, int seedY, float scale = 1f, bool soft = false)
    {
        if (soft)
        {
            SoftFoliage.Round(batches, cx, cz, seedX, seedY, scale);
            return;
        }
        float s = (0.92f + Rand(seedX, seedY, 3) * 0.16f) * scale;
        batches.For(SceneTextures.Bark).Cylinder(new(cx, 0, cz), 0.14f * scale, 0.1f * scale, 0.9f * s, 7, Color.White, cap: false);

        // The crown hangs low over a short trunk, so trees cut off by the top of the screen still read as foliage
        var tint = PixelCanvas.Mix(new Color(96, 176, 104, 255), new Color(118, 192, 108, 255), Rand(seedX, seedY, 4));
        LeafBall(batches, new Vector3(cx, 1.45f * s, cz), 0.74f * s, 0.86f * s, tint, 0.25f);
        LeafBall(batches, new Vector3(cx - 0.22f * scale, 2.05f * s, cz + 0.06f * scale), 0.5f * s, 0.58f * s, PixelCanvas.Light1(tint, 0.08f), 0.4f);
        LeafBall(batches, new Vector3(cx + 0.26f * scale, 1.85f * s, cz - 0.1f * scale), 0.46f * s, 0.52f * s, tint, 0.35f);
    }

    /// <summary>A round clump of leaves: a solid core plus a cut-out shell for a leafy outline.</summary>
    private static void LeafBall(MeshBatches batches, Vector3 center, float radius, float height, Color tint, float sway)
    {
        Icosphere.Add(batches.For(SceneTextures.Leaves), center, new Vector3(radius, height, radius) * 0.9f, MeshBuilder.Scale(tint, 0.8f), 0, 1.4f, sway);
        Icosphere.Add(batches.For(SceneTextures.LeafShell), center, new Vector3(radius, height, radius) * 1.08f, tint, 1, 1.4f, sway);
    }

    /// <summary>
    /// An upright card facing the camera (which always looks north), swaying at the top: a grass tuft or a flower.
    /// <paramref name="x"/> is its left edge; <paramref name="u0"/> and <paramref name="u1"/> pick a frame of the texture.
    /// </summary>
    public static void Card(MeshBuilder b, float x, float z, float w, float h, float u0, float u1)
    {
        var normal = Vector3.Normalize(new Vector3(0, 0.8f, 0.6f));
        b.Quad(new(x, 0, z), new(x + w, 0, z), new(x + w, h, z), new(x, h, z), new(u0, 1), new(u1, 1), new(u1, 0), new(u0, 0),
            Color.White, MeshBuilder.Sway(Color.White, 1f), normal);
    }
}
