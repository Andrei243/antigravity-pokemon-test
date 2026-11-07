using System.Numerics;
using OpenWorldBake;
using PokemonPlatinumEngine.Graphics;

/// <summary>
/// The open world's prototype renderer (plan 32 · OW0; nothing of it is in the game): the baked chunks of the
/// gate's hill drawn through the field's own programs in the scene target at 1920×1080 (a step of half the High
/// preset's 4K target, plan 32 · decision 5), with a sky, one shadow map round the player, the battle's light rigs
/// by the hour, trees as meshes near and as impostors of eight views far, two people as live rigs and a wild
/// Pokémon as its model. It draws into the game's own virtual screen and window, so a frame of it costs what a
/// frame of the game would; it never calls the engine's Update or Draw, and changes nothing the game draws.
/// </summary>
sealed class OpenProtoScene
{
    public const float Near = 0.3f, Far = 1400f;
    /// <summary>Trees nearer the camera than this are meshes, the rest impostors (plan 32: 120 m).</summary>
    public const float TreeMeshReach = 120f;
    /// <summary>Chunks nearer the camera than these are drawn at the finest level, then the second; the rest at the third.</summary>
    public const float FinestReach = 70f, SecondReach = 230f;
    const int ImpostorW = 128, ImpostorH = 160;

    readonly RenderContext context;
    readonly Bake bake;
    public readonly CollisionMesh Collision;
    readonly Dictionary<(int, int, int), (SceneMeshes Meshes, int Triangles)> terrain = new();
    // Each chunk's skirts apart from its surface: drawn in the colour pass only, never cast into the shadow map
    readonly Dictionary<(int, int, int), SceneMeshes> skirts = new();

    /// <summary>Diagnostic switches for the gate's look: the skirts drawn, the shadow map's casters drawn, the post's depth outline.</summary>
    public bool DrawSkirts = true, CastShadows = true, Outline = true;
    readonly Dictionary<(int, int), (SceneMeshes Meshes, int Triangles, int Trees)> treeChunks = new();
    readonly Dictionary<(int, int), List<TreeSpot>> treesByChunk = new();
    readonly Texture2D grain;
    readonly (Texture2D Atlas, Material Material, float Height, float Width)[] impostorKinds = new (Texture2D, Material, float, float)[2];
    Mesh impostorMesh;
    bool impostorLoaded;
    Vector3 impostorCamera = new(float.NaN);

    // What stands in the scene
    public Vector3 PlayerFeet, PersonFeet, PokemonFeet;
    public float PlayerYaw, PersonYaw, PokemonYaw;
    public CharacterPose PlayerPose, PersonPose = new() { Time = 0.7f };
    public float Grown = 1.6f, PersonGrown = 1.65f;
    public string PlayerLook = "player", PersonLook = "lass", Species = "Wooloo";
    public Camera3D Camera;
    public float Hour = 12f, Time;
    public bool ShowVolumes;
    public List<Vector3>? NavPath;

    // What the last frame drew
    public readonly int[] ChunksAt = new int[3];
    public int TerrainTriangles, TreeChunksNear, TreeMeshes, TreeTriangles, Impostors, RigTriangles, PokemonTriangles, ShadowTriangles;
    public int TreesTotal => treesByChunk.Values.Sum(l => l.Count);

    public OpenProtoScene(RenderContext context, Bake bake, CollisionMesh collision, List<TreeSpot> trees)
    {
        this.context = context;
        this.bake = bake;
        Collision = collision;
        context.EnsureLoaded();
        var shaders = context.Shaders;

        // A small grain per cover, 16 texels a metre, point-sampled near and blended far (the plan's look, a first cut)
        var canvas = new PixelCanvas(32, 32);
        for (int y = 0; y < 32; y++)
            for (int x = 0; x < 32; x++)
            {
                double n = Noise.Hash01(x, y, 5) * 0.6 + Noise.Hash01(x / 2, y / 2, 6) * 0.4;
                int v = (int)(214 + 41 * n);
                canvas.SetRaw(x, y, new Color(v, v, v, 255));
            }
        grain = canvas.ToTexture(1, "openproto");
        Raylib.GenTextureMipmaps(ref grain);
        Raylib.SetTextureWrap(grain, TextureWrap.Repeat);
        Rlgl.TextureParameters(grain.Id, 0x2800, 0x2600);   // magnified: nearest, whole texels
        Rlgl.TextureParameters(grain.Id, 0x2801, 0x2703);   // minified: blended between mip levels

        foreach (var ((cx, cz, lod), chunk) in bake.Chunks)
        {
            var b = new MeshBuilder();
            var hung = new MeshBuilder();
            int surface = chunk.GroundTriangles + chunk.ApronTriangles;
            for (int t = 0; t < chunk.TriangleCount; t++)
            {
                var into = t < surface ? b : hung;
                int ia = chunk.Indices[t * 3], ib = chunk.Indices[t * 3 + 1], ic = chunk.Indices[t * 3 + 2];
                var pa = Pos(chunk, ia); var pb = Pos(chunk, ib); var pc = Pos(chunk, ic);
                var face = Vector3.Cross(pb - pa, pc - pa);
                // Box mapping by the face's own axis: the grain is never stretched down a cliff or a cave's wall
                var ax = Vector3.Abs(face);
                Vector2 Uv(Vector3 p) => (ax.Y >= ax.X && ax.Y >= ax.Z ? new Vector2(p.X, p.Z) : ax.X >= ax.Z ? new Vector2(p.Z, -p.Y) : new Vector2(p.X, -p.Y)) * 0.5f;
                into.Tri(pa, pb, pc, Uv(pa), Uv(pb), Uv(pc), Nrm(chunk, ia), Nrm(chunk, ib), Nrm(chunk, ic),
                    Pictures.CoverColour(chunk.Covers[ia]), Pictures.CoverColour(chunk.Covers[ib]), Pictures.CoverColour(chunk.Covers[ic]));
            }
            var meshes = new SceneMeshes();
            meshes.Add(b.Upload("openproto"), grain, MeshPass.Opaque, null, shaders);
            terrain[(cx, cz, lod)] = (meshes, chunk.TriangleCount);
            if (hung.VertexCount > 0)
            {
                var skirtMeshes = new SceneMeshes();
                skirtMeshes.Add(hung.Upload("openproto"), grain, MeshPass.Opaque, null, shaders);
                skirts[(cx, cz, lod)] = skirtMeshes;
            }
        }

        foreach (var tree in trees)
        {
            var key = bake.World.ChunkOf(tree.X, tree.Z);
            if (!treesByChunk.TryGetValue(key, out var list)) treesByChunk[key] = list = new List<TreeSpot>();
            list.Add(tree);
        }
        // The trees of the chunks within 160 m of the hill as meshes (the battle's soft trees, made for a low camera)
        foreach (var (key, list) in treesByChunk)
        {
            var (ox, oz) = bake.World.ChunkOrigin(key.Item1, key.Item2);
            if (Math.Abs(ox + 16) > 160 || Math.Abs(oz + 16) > 160) continue;
            var batches = new MeshBatches();
            foreach (var tree in list)
            {
                var one = new MeshBatches();
                if (tree.Pine) TreeModels.Pine(one, 0, 0, tree.Seed, 17, (float)tree.Scale, soft: true);
                else TreeModels.Round(one, 0, 0, tree.Seed, 17, (float)tree.Scale, soft: true);
                foreach (var (tex, pass, _, builder) in one.All)
                    batches.For(tex, pass).Append(builder, Matrix4x4.CreateTranslation((float)tree.X, (float)tree.Y - 0.05f, (float)tree.Z));
            }
            int triangles = batches.All.Sum(e => e.Builder.VertexCount / 3);
            treeChunks[key] = (SceneMeshes.Upload(batches, shaders, prepassEverything: true), triangles, list.Count);
        }
        BakeImpostors();
    }

    static Vector3 Pos(ChunkMesh c, int v) => new(c.Positions[v * 3] / 1000f, c.Positions[v * 3 + 1] / 1000f, c.Positions[v * 3 + 2] / 1000f);
    static Vector3 Nrm(ChunkMesh c, int v) => new(c.Normals[v * 3], c.Normals[v * 3 + 1], c.Normals[v * 3 + 2]);

    // ------------------------------------------------------------------ impostors

    /// <summary>A pine and a round tree, each rendered from eight sides into an atlas (plan 32: eight-way billboards baked once).</summary>
    void BakeImpostors()
    {
        var shaders = context.Shaders;
        var target = Gpu.Target(ImpostorW, ImpostorH, "openproto");
        for (int kind = 0; kind < 2; kind++)
        {
            var one = new MeshBatches();
            if (kind == 0) TreeModels.Pine(one, 0, 0, 3, 17, 1f, soft: true);
            else TreeModels.Round(one, 0, 0, 3, 17, 1f, soft: true);
            float top = 0, half = 0;
            foreach (var (_, _, _, b) in one.All)
            {
                var (min, max) = b.Bounds();
                top = MathF.Max(top, max.Y);
                half = MathF.Max(half, MathF.Max(MathF.Max(-min.X, max.X), MathF.Max(-min.Z, max.Z)));
            }
            var meshes = SceneMeshes.Upload(one, shaders);
            float frameH = MathF.Max(top * 1.06f, half * 2.1f * ImpostorH / ImpostorW);
            var atlas = new PixelCanvas(ImpostorW * 8, ImpostorH);
            for (int k = 0; k < 8; k++)
            {
                float a = k * MathF.PI / 4f;
                var look = new Vector3(0, frameH / 2f, 0);
                var camera = new Camera3D(look + new Vector3(MathF.Sin(a), 0.14f, MathF.Cos(a)) * 40f, look, Vector3.UnitY, frameH, CameraProjection.Orthographic);
                var light = new SceneLighting(Vector3.Normalize(new Vector3(MathF.Sin(a), 1.2f, MathF.Cos(a))),
                    new Vector3(0.3f, 0.3f, 0.28f), new Vector3(0.72f, 0.74f, 0.76f), new Vector3(0.6f, 0.6f, 0.58f));
                Raylib.BeginTextureMode(context.Shadows.Target);
                Raylib.ClearBackground(Color.White);
                Raylib.EndTextureMode();
                Raylib.BeginTextureMode(target);
                Raylib.ClearBackground(new Color(0, 0, 0, 0));
                Rlgl.SetClipPlanes(1.0, 100.0);
                Raylib.BeginMode3D(camera);
                shaders.SetStudio();
                shaders.SetLighting(Matrix4x4.Identity, light, camera.Position, 1f);
                shaders.SetCharacterStyle(shadowStrength: 0f, rimStrength: 0f);
                shaders.SetWorldRamp(ArtLook.BattleRamp);
                shaders.SetGlow(0f, 0f, Vector3.Zero);
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
                atlas.Blit(frame, k * ImpostorW, 0);
            }
            meshes.Unload();
            var tex = atlas.ToTexture(1, "openproto");
            Raylib.SetTextureWrap(tex, TextureWrap.Clamp);
            Raylib.GenTextureMipmaps(ref tex);
            Raylib.SetTextureFilter(tex, TextureFilter.Trilinear);
            impostorKinds[kind] = (tex, RenderContext.MaterialFor(shaders.World, tex), frameH, frameH * ImpostorW / ImpostorH);
            if (ImpostorAtlas == null) ImpostorAtlas = new PixelCanvas[2];
            ImpostorAtlas[kind] = atlas;
        }
        Gpu.Unload(target);
    }

    /// <summary>The two atlases as baked, for the harness to write out and look at.</summary>
    public PixelCanvas[]? ImpostorAtlas;

    /// <summary>
    /// The far trees as cards facing the camera, each showing the one of its eight views nearest the camera's side
    /// of it, a mesh for each kind made again only when the camera has moved.
    /// </summary>
    void PrepareImpostors(Vector3 eye)
    {
        if (impostorLoaded && Vector3.Distance(eye, impostorCamera) < 2f) return;
        if (impostorLoaded) { Gpu.Unload(impostorMesh); impostorLoaded = false; }
        impostorCamera = eye;
        var b = new MeshBuilder();
        Impostors = 0;
        foreach (var (key, list) in treesByChunk)
        {
            if (TreeChunkNear(key, eye)) continue;
            foreach (var tree in list)
            {
                var foot = new Vector3((float)tree.X, (float)tree.Y - 0.1f, (float)tree.Z);
                var to = new Vector3(eye.X - foot.X, 0, eye.Z - foot.Z);
                if (to.LengthSquared() < 1e-4f) continue;
                to = Vector3.Normalize(to);
                var right = new Vector3(to.Z, 0, -to.X);
                int k = ((int)MathF.Round(MathF.Atan2(to.X, to.Z) / (MathF.PI / 4f)) % 8 + 8) % 8;
                var (_, _, h0, w0) = impostorKinds[tree.Pine ? 0 : 1];
                float h = h0 * (float)tree.Scale, w = w0 * (float)tree.Scale;
                // Two kinds in one mesh: the round trees' views sit in the atlas's right half of the u range
                float u0 = (k + (tree.Pine ? 0 : 8)) / 16f, u1 = (k + 1 + (tree.Pine ? 0 : 8)) / 16f;
                var n = Vector3.Normalize(to * 0.35f + Vector3.UnitY);
                b.Quad(foot - right * (w / 2), foot + right * (w / 2), foot + right * (w / 2) + Vector3.UnitY * h, foot - right * (w / 2) + Vector3.UnitY * h,
                    new(u0, 1), new(u1, 1), new(u1, 0), new(u0, 0), Color.White, Color.White, n);
                Impostors++;
            }
        }
        if (b.VertexCount == 0) return;
        impostorMesh = b.Upload("openproto");
        impostorLoaded = true;
    }

    /// <summary>One atlas of both kinds: the pines' eight views, then the round trees'.</summary>
    Material ImpostorMaterial()
    {
        if (bothAtlas is { } m) return m;
        var both = new PixelCanvas(ImpostorW * 16, ImpostorH);
        both.Blit(ImpostorAtlas![0], 0, 0);
        both.Blit(ImpostorAtlas[1], ImpostorW * 8, 0);
        var tex = both.ToTexture(1, "openproto");
        Raylib.SetTextureWrap(tex, TextureWrap.Clamp);
        Raylib.GenTextureMipmaps(ref tex);
        Raylib.SetTextureFilter(tex, TextureFilter.Trilinear);
        bothTexture = tex;
        bothAtlas = RenderContext.MaterialFor(context.Shaders.World, tex);
        return bothAtlas.Value;
    }
    Material? bothAtlas;
    Texture2D bothTexture;

    bool TreeChunkNear((int, int) key, Vector3 eye)
    {
        if (!treeChunks.ContainsKey(key)) return false;
        var (ox, oz) = bake.World.ChunkOrigin(key.Item1, key.Item2);
        return Vector2.Distance(new Vector2(eye.X, eye.Z), new Vector2((float)ox + 16, (float)oz + 16)) < TreeMeshReach;
    }

    // ------------------------------------------------------------------ the frame

    /// <summary>How far the chunks holding a carve keep their finest level.</summary>
    public const float CaveReach = 260f;

    bool CaveChunk(int cx, int cz)
    {
        var (ox, oz) = bake.World.ChunkOrigin(cx, cz);
        return bake.World.Carves.Any(c => c.Max.X >= ox && c.Min.X <= ox + 32 && c.Max.Z >= oz && c.Min.Z <= oz + 32);
    }

    /// <summary>The light by the hour: the battle's rig (made for a camera low over the ground), its fog drawn out to the horizon.</summary>
    LightRig Rig()
    {
        var rig = ArtLook.BattleRig(Hour) with { FogNear = 110f, FogFar = 760f, FogAmount = 0.78f };
        return Outline ? rig : rig with { Post = rig.Post with { OutlineStrength = 0f } };
    }

    /// <summary>Renders the scene into the scene target at 1920×1080 and readies the post passes. Outside any texture mode.</summary>
    public void Render()
    {
        context.EnsureLoaded();
        float step = MathF.Min(1f, 1920f / context.Target.Texture.Width);
        context.ResolutionStep = step;
        var shaders = context.Shaders;
        var rig = Rig();
        var light = rig.Light;
        var eye = Camera.Position;

        // Which level each chunk is drawn at, by its distance from the camera
        Array.Clear(ChunksAt);
        TerrainTriangles = 0;
        var drawn = new List<SceneMeshes>();
        for (int cz = 0; cz < bake.World.ChunksZ; cz++)
            for (int cx = 0; cx < bake.World.ChunksX; cx++)
            {
                var (ox, oz) = bake.World.ChunkOrigin(cx, cz);
                float d = Vector2.Distance(new Vector2(eye.X, eye.Z), new Vector2((float)ox + 16, (float)oz + 16));
                int want = d < FinestReach ? 0 : d < SecondReach ? 1 : 2;
                // A chunk with a cave in it keeps its finest level further out: a coarser cell than the rock over a
                // cavern is thick meshes the shell with holes in it (the gate's finding; OW2 makes far levels by
                // thinning the finest mesh instead, which keeps its holes where they are)
                if (CaveChunk(cx, cz) && d < CaveReach) want = 0;
                for (int lod = want; lod < 3; lod++)
                    if (terrain.TryGetValue((cx, cz, lod), out var t))
                    {
                        drawn.Add(t.Meshes);
                        if (DrawSkirts && skirts.TryGetValue((cx, cz, lod), out var hung)) drawn.Add(hung);
                        ChunksAt[lod]++;
                        TerrainTriangles += t.Triangles;
                        break;
                    }
            }
        TreeChunksNear = 0; TreeTriangles = 0; TreeMeshes = 0;
        var nearTrees = new List<SceneMeshes>();
        foreach (var (key, chunk) in treeChunks)
            if (TreeChunkNear(key, eye)) { nearTrees.Add(chunk.Meshes); TreeChunksNear++; TreeTriangles += chunk.Triangles; TreeMeshes += chunk.Trees; }
        PrepareImpostors(eye);
        var impostorMaterial = ImpostorMaterial();
        FrameProfiler.Lap(FrameSection.Prepare);

        shaders.SetTime(Time);
        shaders.SetWind(0f);
        shaders.SetWalkers(new[] { PlayerFeet, PersonFeet });

        // 1. The shadow map round the player: the ground, the near trees, the people and the Pokémon
        var lightCamera = context.Shadows.LightCamera(PlayerFeet, light.SunDirection, 70f);
        Raylib.BeginTextureMode(context.Shadows.Target);
        Raylib.ClearBackground(Color.White);
        Rlgl.SetClipPlanes(1.0, 200.0);
        Raylib.BeginMode3D(lightCamera);
        var lightView = Rlgl.GetMatrixModelview();
        var lightProjection = Rlgl.GetMatrixProjection();
        Rlgl.DisableBackfaceCulling();
        ShadowTriangles = 0;
        for (int cz = 0; cz < bake.World.ChunksZ && CastShadows; cz++)
            for (int cx = 0; cx < bake.World.ChunksX; cx++)
            {
                var (ox, oz) = bake.World.ChunkOrigin(cx, cz);
                if (Vector2.Distance(new Vector2(PlayerFeet.X, PlayerFeet.Z), new Vector2((float)ox + 16, (float)oz + 16)) > 70f) continue;
                for (int lod = 0; lod < 3; lod++)
                    if (terrain.TryGetValue((cx, cz, lod), out var t)) { t.Meshes.DrawDepth(); ShadowTriangles += t.Triangles; break; }
            }
        foreach (var (key, chunk) in treeChunks)
        {
            var (ox, oz) = bake.World.ChunkOrigin(key.Item1, key.Item2);
            if (!CastShadows || Vector2.Distance(new Vector2(PlayerFeet.X, PlayerFeet.Z), new Vector2((float)ox + 16, (float)oz + 16)) > 70f) continue;
            chunk.Meshes.DrawDepth();
            ShadowTriangles += chunk.Triangles;
        }
        if (CastShadows) DrawFigures(CharacterPass.Depth);
        Rlgl.EnableBackfaceCulling();
        Raylib.EndMode3D();
        Raylib.EndTextureMode();
        FrameProfiler.Lap(FrameSection.Shadows);

        shaders.SetLighting(Raymath.MatrixMultiply(lightView, lightProjection), light, eye, context.Shadows.Texel);
        shaders.SetCharacterStyle(shadowStrength: 1f, rimStrength: rig.Rim);
        shaders.SetWorldRamp(ArtLook.BattleRamp);
        shaders.SetFog(rig.FogColor, rig.FogAmount, rig.FogNear, rig.FogFar);
        shaders.SetCloudShade(rig.CloudShade);
        shaders.SetGlow(0f, 0f, Vector3.Zero);
        shaders.SetUpright(0f, 0f);

        // 2. The sky, then the land, the trees near and far, the people and the Pokémon
        context.BeginScene();
        SkyPainter.Draw(Time, context.PartWidth, context.PartHeight, rig.Sky, clouds: true);
        Rlgl.SetClipPlanes(Near, Far);
        Raylib.BeginMode3D(Camera);
        Rlgl.DisableBackfaceCulling();
        context.BindShadowMap();
        foreach (var t in nearTrees) t.Draw();
        foreach (var t in drawn) t.Draw();
        if (impostorLoaded) Raylib.DrawMesh(impostorMesh, impostorMaterial, Matrix4x4.Identity);
        DrawFigures(CharacterPass.Color);
        Rlgl.EnableBackfaceCulling();
        DrawFigures(CharacterPass.Outline);
        if (ShowVolumes) DrawVolumes();
        Raylib.EndMode3D();
        Raylib.EndTextureMode();
        context.UnbindShadowMap();
        Rlgl.SetClipPlanes(0.01, 1000.0);
        FrameProfiler.Lap(FrameSection.Scene);
        context.PreparePost(rig.Post, new DepthRange(Near, Far, Camera.FovY, (float)context.PartWidth / context.PartHeight));
    }

    /// <summary>The composite into the game's virtual screen (the interface's 4K), with the alpha kept whole as the engine keeps it.</summary>
    public void Composite(RenderTexture2D virtualScreen)
    {
        Raylib.BeginTextureMode(virtualScreen);
        Raylib.ClearBackground(Color.Black);
        Rlgl.ColorMask(true, true, true, false);
        Raylib.BeginMode2D(new Camera2D { Zoom = GameEngine.RenderScale });
        context.Composite(new Rectangle(0, 0, GameEngine.VirtualWidth, GameEngine.VirtualHeight));
        Raylib.EndMode2D();
        Rlgl.ColorMask(true, true, true, true);
        Raylib.EndTextureMode();
    }

    // ------------------------------------------------------------------ people and the Pokémon

    void DrawFigures(CharacterPass pass)
    {
        if (pass != CharacterPass.Depth) { RigTriangles = 0; PokemonTriangles = 0; }
        DrawRig(CharacterModels.Get(PlayerLook, context.Shaders), PlayerPose, PlayerFeet, PlayerYaw, Grown, pass);
        DrawRig(CharacterModels.Get(PersonLook, context.Shaders), PersonPose with { Time = Time + 0.7f }, PersonFeet, PersonYaw, PersonGrown, pass);
        var model = PokemonModels.Get(Species);
        float height = PokemonDatabase.Get(Species)!.Height;
        float scale = height / MathF.Max(0.05f, model.Height);
        var root = Matrix4x4.CreateScale(scale) * Matrix4x4.CreateRotationY(PokemonYaw) * Matrix4x4.CreateTranslation(PokemonFeet);
        PokemonRenderer.Draw(context, model, new PokePose { Time = Time }, root, pass);
        if (pass == CharacterPass.Color) PokemonTriangles += model.Mesh.TriangleCount;
        if (pass == CharacterPass.Outline) PokemonTriangles += model.Shell.TriangleCount;
        if (pass == CharacterPass.Depth) ShadowTriangles += model.Mesh.TriangleCount;
    }

    /// <summary>A rig posed, stretched into plan 30 · FM0's grown stand-in at a height in metres, and drawn for a pass.</summary>
    void DrawRig(CharacterRig rig, CharacterPose pose, Vector3 feet, float yaw, float grown, CharacterPass pass)
    {
        if (!rig.Uploaded || rig.Body == null) return;
        rig.Animate(pose);
        float scale = rig.Scale * FreeMovePeople.Grow(rig, grown);
        var model = Matrix4x4.CreateScale(scale) * Matrix4x4.CreateRotationY(yaw) * Matrix4x4.CreateTranslation(feet);
        switch (pass)
        {
            case CharacterPass.Depth:
                rig.Body.Draw(context.DepthSkinned, rig.Skin, model);
                rig.ClothesBody?.Draw(context.DepthSkinned, rig.Skin, model);
                ShadowTriangles += rig.Body.Mesh.TriangleCount + (rig.ClothesBody?.Mesh.TriangleCount ?? 0);
                break;
            case CharacterPass.Color:
                rig.Body.Draw(context.ToonSkinned, rig.Skin, model);
                rig.ClothesBody?.Draw(context.ToonSkinned, rig.Skin, model);
                if (rig.FaceModel != null && rig.FaceMaterials.Length > 0)
                    rig.FaceModel.Draw(rig.FaceMaterial(CharacterAnimation.ExpressionOf(pose), pose.Blink), rig.Skin, model);
                RigTriangles += rig.Body.Mesh.TriangleCount + (rig.ClothesBody?.Mesh.TriangleCount ?? 0) + (rig.FaceModel?.Mesh.TriangleCount ?? 0);
                break;
            case CharacterPass.Outline:
                if (rig.Outline == null) break;
                context.Shaders.SetOutlineWidth(0f);
                Rlgl.DrawRenderBatchActive();
                Rlgl.EnableBackfaceCulling();
                Rlgl.SetCullFace(0);
                rig.Outline.Draw(context.OutlineSkinned, rig.Skin, model);
                rig.ClothesOutline?.Draw(context.OutlineSkinned, rig.Skin, model);
                Rlgl.SetCullFace(1);
                RigTriangles += rig.Outline.Mesh.TriangleCount + (rig.ClothesOutline?.Mesh.TriangleCount ?? 0);
                break;
        }
    }

    // ------------------------------------------------------------------ the volumes (a debug look, plan 32 · OW10's layers)

    void DrawVolumes()
    {
        // Through the rock, as a debug layer is: the trigger in the cavern and the path through the tunnel are under the hill
        Rlgl.DrawRenderBatchActive();
        Rlgl.DisableDepthTest();
        var trigger = Prototype.Trigger;
        var min = new Vector3((float)trigger.Min.X, (float)trigger.Min.Y, (float)trigger.Min.Z);
        var max = new Vector3((float)trigger.Max.X, (float)trigger.Max.Y, (float)trigger.Max.Z);
        Raylib.DrawCubeWiresV((min + max) / 2, max - min, new Color(230, 40, 40, 255));
        var zone = Prototype.Zone;
        for (int i = 0; i < zone.Polygon.Length; i++)
        {
            var (ax, az) = zone.Polygon[i]; var (bx, bz) = zone.Polygon[(i + 1) % zone.Polygon.Length];
            foreach (float y in new[] { (float)zone.MinY, (float)zone.MaxY })
                Raylib.DrawLine3D(new Vector3((float)ax, y, (float)az), new Vector3((float)bx, y, (float)bz), new Color(240, 200, 40, 255));
            Raylib.DrawLine3D(new Vector3((float)ax, (float)zone.MinY, (float)az), new Vector3((float)ax, (float)zone.MaxY, (float)az), new Color(240, 200, 40, 160));
        }
        if (NavPath != null)
            for (int i = 0; i + 1 < NavPath.Count; i++)
                Raylib.DrawLine3D(NavPath[i] + Vector3.UnitY * 0.15f, NavPath[i + 1] + Vector3.UnitY * 0.15f, new Color(40, 220, 230, 255));
        Rlgl.DrawRenderBatchActive();
        Rlgl.EnableDepthTest();
    }

    public void Unload()
    {
        foreach (var t in terrain.Values) t.Meshes.Unload();
        foreach (var t in skirts.Values) t.Unload();
        foreach (var t in treeChunks.Values) t.Meshes.Unload();
        terrain.Clear(); skirts.Clear(); treeChunks.Clear();
        if (impostorLoaded) Gpu.Unload(impostorMesh);
        impostorLoaded = false;
        foreach (var k in impostorKinds) if (k.Atlas.Id != 0) Gpu.Unload(k.Atlas);
        if (bothTexture.Id != 0) Gpu.Unload(bothTexture);
        Gpu.Unload(grain);
    }
}
