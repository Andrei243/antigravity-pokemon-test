using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.OpenWorld;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Graphics.OpenWorld;

/// <summary>
/// What one frame of an open map shows: the map, the player, the camera, the hour, the weather and the cave's share, in
/// metres; and (plan 32 · OW9) who and what stands there beside the player, and whom the grass parts round.
/// </summary>
internal sealed record OpenScene(
    OpenMap Map, Vector3 Feet, float Yaw, CharacterPose Pose, string? Look,
    Vector3 Eye, Vector3 Target, float Hour, FieldWeather Weather, float CaveShare, double Time,
    bool Swimming = false, float Lens = (float)OrbitCamera.LensDegrees,
    IReadOnlyList<OpenFigure>? Figures = null, IReadOnlyList<Vector3>? Walkers = null);

/// <summary>
/// The open world's renderer (plan 32 · OW8 and OW9; style guide, "Open world"): an open map's baked land streamed by
/// its rings, its cover chosen texel by texel and lit through three shadow maps from the sun, its water in the field's
/// pixel art with the mirror on High and Medium, the sky of the hour by the camera's own pitch, the rings to the horizon
/// beyond the region's last chunks (<see cref="OpenHorizon"/>), the weather in the air, the cave's light under rock and
/// the player as plan 30's grown rig; and (OW9) the map's buildings and solids (<see cref="OpenBuildingKit"/>), its trees
/// near and far and the grass's tufts (<see cref="OpenFoliage"/>), and its people, follower and Pokémon
/// (<see cref="OpenFigureDraw"/>), all drawn into the scene target at 1920×1080 and composited under the interface's 4K
/// as the field is. It shares the <see cref="RenderContext"/> with the grid's and the battle's renderers, so every
/// uniform it reads it sets each frame; nothing of theirs changes for it.
/// </summary>
internal sealed class OpenRenderer
{
    /// <summary>The width the scene is drawn at in an open map, on every preset (decision 5).</summary>
    public const int SceneWidth = 1920;

    /// <summary>How far past the region's last chunks the land reaches, in metres (<see cref="OpenHorizon.Reach"/>).</summary>
    public const float HorizonReach = OpenHorizon.Reach;

    private readonly RenderContext context;
    private readonly OpenShaders shaders = new();
    private readonly OpenCascades cascades = new();
    private readonly OpenStream stream = new();
    private readonly OpenRings rings = new();
    private readonly List<WeatherPiece> pieces = new();
    private readonly OpenFoliage foliage = new();
    private readonly OpenBuildingKit buildings = new();
    private readonly OpenFigureDraw figureDraw = new();

    /// <summary>The trees and grass (what they drew last frame), the buildings and the figures (plan 32 · OW9).</summary>
    public OpenFoliage Foliage => foliage;
    public OpenBuildingKit Buildings => buildings;
    public OpenFigureDraw Figures => figureDraw;

    /// <summary>The land beyond the region as it was made for the map in use (its mesh's triangles, its far trees).</summary>
    public int HorizonTriangles { get; private set; }
    public int HorizonTrees { get; private set; }
    private Texture2D atlas;
    private Material terrain, water, depth;
    private bool resources;
    private Mesh horizon;
    private bool horizonLoaded;
    private readonly Matrix4x4[] lightVP = new Matrix4x4[OpenCascades.Count];

    /// <summary>Whether level 0's skirts are drawn: not since the thinned levels meet it on its own vertices (OW2), and a skirt hung from a tunnel's roof along a chunk's border is a fin in the tunnel.</summary>
    public bool DrawSkirts { get; set; }

    // What the last frame drew, for the harness and the profiler
    public int[] ChunksAt { get; private set; } = new int[3];
    public int ChunksDrawn { get; private set; }
    public int TerrainTriangles { get; private set; }
    public int WaterTriangles { get; private set; }
    public int[] ShadowTriangles { get; } = new int[OpenCascades.Count];
    public int WeatherPieces { get; private set; }
    public bool Mirrored { get; private set; }
    public float MirrorPlane { get; private set; }
    public bool PlayerDrawn { get; private set; }
    public float CaveShare { get; private set; }
    public float Step { get; private set; } = 1f;
    public LightRig Rig { get; private set; }
    public OpenStream Stream => stream;
    public OpenRings Rings => rings;
    public Camera3D Camera { get; private set; }

    private FieldWeather weather;
    private double time;

    /// <summary>Where the player's middle was on the picture the last time it was drawn, in shares of its width and height.</summary>
    public Vector2 PlayerOnPicture { get; private set; } = new(0.5f, 0.5f);

    public OpenRenderer(RenderContext context) => this.context = context;

    /// <summary>The step of the preset's target that is 1920 across: half of High's 4K, two thirds of Medium's, the whole of Low's.</summary>
    public static float StepFor(int targetWidth) => targetWidth <= SceneWidth ? 1f : SceneWidth / (float)targetWidth;

    private void EnsureResources()
    {
        context.EnsureLoaded();
        shaders.Load();
        cascades.Ensure(context.Quality.ShadowMapSize);
        foliage.Ensure(shaders);
        // The trees' impostors are baked once, before anything of a frame is set (the bake has its own light)
        foliage.BakeImpostors(context, shaders);
        if (resources) return;
        atlas = OpenCoverArt.Upload();
        terrain = RenderContext.MaterialFor(shaders.Terrain, atlas);
        water = RenderContext.MaterialFor(shaders.Water, atlas);
        depth = context.Depth;
        resources = true;
    }

    /// <summary>
    /// The frame's scene into the scene target at 1920×1080 (outside any texture mode), and the post passes readied:
    /// the chunks streamed, the three shadow maps, the mirror where water is in view on a preset that reflects, the sky,
    /// the land and its skirts, the water, the player and the weather.
    /// </summary>
    public void Render(OpenScene scene)
    {
        EnsureResources();
        // 1920×1080 on every preset; dynamic resolution steps under it, never over (plan 16 · T23)
        Step = StepFor(context.Target.Texture.Width) * context.ResolutionStep;
        context.ResolutionStep = Step;
        weather = scene.Weather;
        time = scene.Time;
        CaveShare = scene.CaveShare;
        var rig = OpenLook.UnderRock(OpenLook.Weathered(OpenLook.Open(scene.Hour), scene.Weather), scene.CaveShare);
        Rig = rig;
        float aspect = (float)context.PartWidth / context.PartHeight;
        var camera = new Camera3D(scene.Eye, scene.Target, Vector3.UnitY, scene.Lens, CameraProjection.Perspective);
        Camera = camera;
        var view = new OpenView(scene.Eye, scene.Target, scene.Lens, aspect, (float)OrbitCamera.Near, (float)OrbitCamera.Far);
        var feet = new OpenVec(scene.Feet.X, scene.Feet.Y, scene.Feet.Z);

        // The chunks: what the camera sees first, then the rest within reach (another map begins the rings afresh)
        if (!ReferenceEquals(stream.Map, scene.Map)) rings.Reset();
        stream.Use(scene.Map);
        rings.Move(feet);
        stream.Update(rings, feet, (x, z) => view.Sees(new Vector3(x * 32f, -400f, z * 32f), new Vector3(x * 32f + 32f, 800f, z * 32f + 32f)));
        PrepareHorizon(scene.Map);
        // What stands on the land (plan 32 · OW9): the map's buildings and trees made once a map, the frame's near trees
        // and tufts, and the figures in view, an impostor's look baked where one is wanted (before any of the frame's
        // uniforms: a bake sets its own light)
        buildings.Use(scene.Map, context, shaders);
        foliage.Prepare(scene.Eye, scene.Feet, stream.Ready);
        var figures = scene.Figures ?? Array.Empty<OpenFigure>();
        figureDraw.SetEye(scene.Eye);
        figureDraw.Prepare(context, shaders, figures, scene.Eye, scene.Time);
        FrameProfiler.Lap(FrameSection.Prepare);

        var fs = context.Shaders;
        fs.SetTime((float)(scene.Time % 3600.0));
        fs.SetWind(Weathers.Wind(scene.Weather));
        fs.SetWalkers(Array.Empty<Vector3>());
        fs.SetUpright(0f, 0f);
        fs.SetGlow(0f, 0f, Vector3.Zero);
        fs.SetFlash(default, 0f);
        fs.SetStageWash(StageLight.None);
        shaders.SetMotion((float)(scene.Time % 3600.0), Weathers.Wind(scene.Weather), Core.Accessibility.ReduceMotion, scene.Walkers ?? Array.Empty<Vector3>());

        // The rig on the screen: the player at the capsule's feet, facing its yaw
        var rigModel = scene.Look is { } look ? CharacterModels.TryGet(look, fs) : null;
        PlayerDrawn = rigModel != null;
        var root = Matrix4x4.CreateRotationY(scene.Yaw) * Matrix4x4.CreateTranslation(scene.Feet);

        RenderShadows(scene, rig, view, rigModel, root);
        FrameProfiler.Lap(FrameSection.Shadows);

        // The uniforms of the frame, the open programs' and the field's (which the player's rig is drawn with)
        shaders.SetLight(rig.Light, scene.Eye, view.Forward, ArtLook.BattleRamp);
        shaders.SetFog(rig.FogColor, rig.FogAmount, rig.FogNear, rig.FogFar, rig.CloudShade, (float)(scene.Time % 3600.0));
        var ends = new Vector3(OpenCascades.Ends[0], OpenCascades.Ends[1], OpenCascades.Ends[2]);
        var texelWorld = new Vector3(TexelWorld(0), TexelWorld(1), TexelWorld(2));
        shaders.SetCascades(lightVP, ends, texelWorld * 1.5f, new Vector3(Bias(0), Bias(1), Bias(2)),
            new Vector3(1f / cascades.SizeOf(0), 1f / cascades.SizeOf(1), 1f / cascades.SizeOf(2)), context.Quality.ShadowTaps);
        var lampAt = scene.Feet + new Vector3(0, (float)(OrbitCamera.Above + OpenLook.LampAbove), 0);
        shaders.SetLamp(lampAt, OpenLook.LampStrength(scene.CaveShare), OpenLook.LampColour, OpenLook.LampReach);
        shaders.SetMirrorClip(false, 0f);
        // Lit glass after dark, as the field's (plan 32 · OW9): lamps and shops all night, homes into the evening
        shaders.SetGlow(rig.LampGlow, rig.HomeGlow, rig.GlowColor);
        // The player is lit by the light about them under rock too, as the rock round them is
        float lamp = OpenLook.LampStrength(scene.CaveShare);
        var peopleLight = lamp > 0f ? rig.Light with { SkyAmbient = rig.Light.SkyAmbient + OpenLook.LampColour * (lamp * 0.6f) } : rig.Light;
        fs.SetLighting(lightVP[0], peopleLight, scene.Eye, 1f / cascades.SizeOf(0));
        fs.SetCharacterStyle(shadowStrength: 1f, rimStrength: rig.Rim);
        fs.SetFog(rig.FogColor, rig.FogAmount, rig.FogNear, rig.FogFar);
        fs.SetCloudShade(rig.CloudShade);

        RenderMirror(scene, rigModel, root);
        FrameProfiler.Lap(FrameSection.Reflections);

        // The scene: the sky, the land, the plain beyond it, the water, the player, the weather
        context.BeginScene();
        Raylib.ClearBackground(ToColour(rig.FogColor));
        var viewMatrix = Raymath.MatrixLookAt(scene.Eye, scene.Target, Vector3.UnitY);
        var projection = Raymath.MatrixPerspective(scene.Lens * Math.PI / 180, aspect, OrbitCamera.Near, OrbitCamera.Far);
        var inverse = Raymath.MatrixInvert(Raymath.MatrixMultiply(viewMatrix, projection));
        shaders.SetSky(inverse, new Vector2(context.PartWidth, context.PartHeight), scene.Eye, rig.Sky, (float)(scene.Time % 3600.0));
        Raylib.BeginShaderMode(shaders.Sky);
        Raylib.DrawRectangle(0, 0, context.PartWidth, context.PartHeight, Color.White);
        Raylib.EndShaderMode();

        Rlgl.SetClipPlanes(OrbitCamera.Near, OrbitCamera.Far);
        Raylib.BeginMode3D(camera);
        cascades.Bind();
        // What stands on the ground first, so the ground is not shaded where it stands
        buildings.Draw();
        foliage.Draw();
        FrameProfiler.Lap(FrameSection.Foliage);
        DrawLand(view, scene);
        FrameProfiler.Lap(FrameSection.Terrain);
        foliage.DrawImpostors(shaders, scene.Eye);
        figureDraw.DrawImpostors(shaders, scene.Eye);
        DrawWater(view);
        FrameProfiler.Lap(FrameSection.Water);
        if (rigModel != null)
        {
            CharacterRenderer.Draw(context, rigModel, scene.Pose, root, CharacterPass.Color);
            CharacterRenderer.Draw(context, rigModel, scene.Pose, root, CharacterPass.Outline);
        }
        figureDraw.DrawLive(context, CharacterPass.Color, scene.Time);
        figureDraw.DrawLive(context, CharacterPass.Outline, scene.Time);
        FrameProfiler.Lap(FrameSection.Characters);
        // The light the buildings throw on the ground after dark, the "!" over a trainer, then the weather
        buildings.DrawLights(rig);
        figureDraw.DrawBubbles(camera, Paper(rig), scene.Time);
        DrawWeather(scene, rig);
        Raylib.EndMode3D();
        Raylib.EndTextureMode();
        cascades.Unbind();
        Rlgl.SetClipPlanes(0.01, 1000.0);
        FrameProfiler.Lap(FrameSection.Scene);

        var middle = Raylib.GetWorldToScreenEx(scene.Feet + new Vector3(0, 0.8f, 0), camera, 1920, 1080);
        PlayerOnPicture = new Vector2(middle.X / 1920f, middle.Y / 1080f);
        context.PreparePost(rig.Post, new DepthRange((float)OrbitCamera.Near, (float)OrbitCamera.Far, scene.Lens, aspect));
    }

    /// <summary>The composite into the current target (the virtual screen's 2D pass), with a storm's lightning over it.</summary>
    public void DrawToScreen(int destWidth, int destHeight, SceneRipple ripple = default)
    {
        context.Composite(new Rectangle(0, 0, destWidth, destHeight), ripple);
        float flash = WeatherFx.Lightning(weather, time);
        if (flash > 0f) Raylib.DrawRectangle(0, 0, destWidth, destHeight, new Color(255, 255, 255, (int)(flash * 255f)));
    }

    // ------------------------------------------------------------------ the shadows

    private float TexelWorld(int i) => sliceRadius[i] * 2f / cascades.SizeOf(i);

    // A bias in each map's own depth: about a few centimetres of the slice's depth range
    private float Bias(int i) => (0.03f + TexelWorld(i) * 0.5f) / (float)sliceRange[i];

    private readonly float[] sliceRadius = new float[OpenCascades.Count];
    private readonly double[] sliceRange = new double[OpenCascades.Count];

    private void RenderShadows(OpenScene scene, LightRig rig, OpenView view, CharacterRig? rigModel, Matrix4x4 root)
    {
        float aspect = (float)context.PartWidth / context.PartHeight;
        var sun = rig.Light.SunDirection;
        for (int i = 0; i < OpenCascades.Count; i++)
        {
            float near = i == 0 ? (float)OrbitCamera.Near : OpenCascades.Ends[i - 1] * (1f - OpenCascades.Blend);
            var (centre, radius) = OpenCascades.Slice(scene.Eye, view.Forward, scene.Lens, aspect, near, OpenCascades.Ends[i]);
            int size = cascades.SizeOf(i);
            var (camera, clipNear, clipFar) = OpenCascades.Fit(centre, radius, sun, size);
            sliceRadius[i] = radius;
            sliceRange[i] = clipFar - clipNear;
            var target = cascades.Target(i);
            Raylib.BeginTextureMode(target);
            Raylib.ClearBackground(Color.White);
            Rlgl.SetClipPlanes(clipNear, clipFar);
            Raylib.BeginMode3D(camera);
            lightVP[i] = Raymath.MatrixMultiply(Rlgl.GetMatrixModelview(), Rlgl.GetMatrixProjection());
            Rlgl.DisableBackfaceCulling();
            ShadowTriangles[i] = 0;
            var forward = Vector3.Normalize(camera.Target - camera.Position);
            var right = Vector3.Normalize(Vector3.Cross(forward, camera.Up));
            var up = Vector3.Cross(right, forward);
            float back = (float)(clipFar - clipNear) - radius;
            foreach (var chunk in stream.Ready)
            {
                if (!InLightBox(chunk.Min, chunk.Max, camera.Target, right, up, forward, radius, back)) continue;
                Raylib.DrawMesh(chunk.Surface, depth, Matrix4x4.Identity);
                ShadowTriangles[i] += chunk.SurfaceTriangles;
            }
            // The buildings and the near trees cast into every map (plan 32 · OW9), the people and Pokémon into the first two, as the player
            ShadowTriangles[i] += buildings.DrawDepth();
            var lightCentre = camera.Target;
            ShadowTriangles[i] += foliage.DrawDepth((min, max) => InLightBox(min, max, lightCentre, right, up, forward, radius, back));
            if (rigModel != null && i < 2) CharacterRenderer.Draw(context, rigModel, scene.Pose, root, CharacterPass.Depth);
            if (i < 2) figureDraw.DrawLive(context, CharacterPass.Depth, scene.Time, centre, radius);
            Rlgl.EnableBackfaceCulling();
            Raylib.EndMode3D();
            Raylib.EndTextureMode();
        }
        Rlgl.SetClipPlanes(0.01, 1000.0);
    }

    /// <summary>Whether a box can cast into a map's box: within its side and its height on the plane facing the sun, and from behind it to its far side along the light.</summary>
    private static bool InLightBox(Vector3 min, Vector3 max, Vector3 centre, Vector3 right, Vector3 up, Vector3 forward, float radius, float behind)
    {
        float r0 = float.MaxValue, r1 = float.MinValue, u0 = float.MaxValue, u1 = float.MinValue, f0 = float.MaxValue, f1 = float.MinValue;
        for (int c = 0; c < 8; c++)
        {
            var p = new Vector3((c & 1) == 0 ? min.X : max.X, (c & 2) == 0 ? min.Y : max.Y, (c & 4) == 0 ? min.Z : max.Z) - centre;
            float r = Vector3.Dot(p, right), u = Vector3.Dot(p, up), f = Vector3.Dot(p, forward);
            r0 = MathF.Min(r0, r); r1 = MathF.Max(r1, r);
            u0 = MathF.Min(u0, u); u1 = MathF.Max(u1, u);
            f0 = MathF.Min(f0, f); f1 = MathF.Max(f1, f);
        }
        return r1 >= -radius && r0 <= radius && u1 >= -radius && u0 <= radius && f1 >= -behind && f0 <= radius;
    }

    // ------------------------------------------------------------------ the mirror

    /// <summary>
    /// On a preset that reflects, with water near the middle of the picture: the land and the player mirrored about the
    /// water's height there into the mirror (style guide, "Open world", "Water"), and the water told of it.
    /// </summary>
    private void RenderMirror(OpenScene scene, CharacterRig? rigModel, Matrix4x4 root)
    {
        Mirrored = false;
        shaders.SetWaterMirror(false, 0f, Matrix4x4.Identity);
        if (!context.HasMirror) return;
        // The water's height nearest where the camera looks: where its ray meets the feet's height, or the feet
        var look = Vector3.Normalize(scene.Target - scene.Eye);
        var at = look.Y < -0.05f ? scene.Eye + look * ((scene.Feet.Y - scene.Eye.Y) / look.Y) : scene.Feet;
        if (stream.WaterNear(at.X, at.Z, 40f) is not { } plane) return;
        Mirrored = true;
        MirrorPlane = plane;
        var eye = new Vector3(scene.Eye.X, 2f * plane - scene.Eye.Y, scene.Eye.Z);
        var target = new Vector3(scene.Target.X, 2f * plane - scene.Target.Y, scene.Target.Z);
        var mirror = new Camera3D(eye, target, Vector3.UnitY, scene.Lens, CameraProjection.Perspective);
        float aspect = Reflections.TargetWidth / (float)Reflections.TargetHeight;
        var view = new OpenView(eye, target, scene.Lens, aspect, (float)OrbitCamera.Near, 300f);

        Raylib.BeginTextureMode(context.Mirror);
        Raylib.ClearBackground(new Color(0, 0, 0, 0));
        Rlgl.SetClipPlanes(OrbitCamera.Near, 300.0);
        Raylib.BeginMode3D(mirror);
        var vp = Raymath.MatrixMultiply(Rlgl.GetMatrixModelview(), Rlgl.GetMatrixProjection());
        cascades.Bind();
        shaders.SetMirrorClip(true, plane - 0.02f);
        // Seen from under the plane, what faces it is the side its reflection shows
        Rlgl.EnableBackfaceCulling();
        foreach (var chunk in stream.Ready)
            if (view.Sees(chunk.Min, chunk.Max) && chunk.Max.Y > plane) Raylib.DrawMesh(chunk.Surface, terrain, Matrix4x4.Identity);
        // What stands over the water is mirrored too (plan 32 · OW9): the buildings, the near trees and the live figures
        buildings.Draw();
        foliage.Draw();
        if (rigModel != null && !scene.Swimming)
        {
            CharacterRenderer.Draw(context, rigModel, scene.Pose, root, CharacterPass.Color);
            CharacterRenderer.Draw(context, rigModel, scene.Pose, root, CharacterPass.Outline);
        }
        figureDraw.DrawLive(context, CharacterPass.Color, scene.Time);
        shaders.SetMirrorClip(false, 0f);
        Raylib.EndMode3D();
        Raylib.EndTextureMode();
        cascades.Unbind();
        Rlgl.SetClipPlanes(0.01, 1000.0);
        shaders.SetWaterMirror(true, plane, vp);
    }

    // ------------------------------------------------------------------ the land, the plain and the water

    private void DrawLand(OpenView view, OpenScene scene)
    {
        Array.Clear(ChunksAt);
        if (ChunksAt.Length < 3) ChunksAt = new int[3];
        ChunksDrawn = 0;
        TerrainTriangles = 0;
        Rlgl.EnableBackfaceCulling();
        foreach (var chunk in stream.Ready)
        {
            if (chunk.Level < ChunksAt.Length) ChunksAt[chunk.Level]++;
            if (!view.Sees(chunk.Min, chunk.Max)) continue;
            Raylib.DrawMesh(chunk.Surface, terrain, Matrix4x4.Identity);
            FrameProfiler.Count(chunk.SurfaceTriangles);
            TerrainTriangles += chunk.SurfaceTriangles;
            ChunksDrawn++;
        }
        if (horizonLoaded) { Raylib.DrawMesh(horizon, terrain, Matrix4x4.Identity); FrameProfiler.Count(horizon.TriangleCount); }
        if (!DrawSkirts) return;
        // The skirts hang under the borders: drawn from either side, cast nothing
        Rlgl.DisableBackfaceCulling();
        foreach (var chunk in stream.Ready)
            if (chunk.Skirts is { } skirts && view.Sees(chunk.Min - new Vector3(0, 1, 0), chunk.Max))
            {
                Raylib.DrawMesh(skirts, terrain, Matrix4x4.Identity);
                FrameProfiler.Count(skirts.TriangleCount);
            }
        Rlgl.EnableBackfaceCulling();
    }

    private void DrawWater(OpenView view)
    {
        WaterTriangles = 0;
        var mirror = Mirrored ? context.Mirror.Texture : atlas;
        Raylib.SetMaterialTexture(ref water, MaterialMapIndex.Metalness, mirror);
        foreach (var chunk in stream.Ready)
        {
            if (chunk.Water is not { } mesh || !view.Sees(chunk.Min, chunk.Max)) continue;
            Raylib.DrawMesh(mesh, water, Matrix4x4.Identity);
            FrameProfiler.Count(chunk.WaterTriangles);
            WaterTriangles += chunk.WaterTriangles;
        }
    }

    /// <summary>
    /// The land beyond the region's last chunks (style guide, "Open world", "Beyond the region"; plan 32 · OW9's rings
    /// to the horizon, <see cref="OpenHorizon"/>), made once a map: rings round the region's extent that meet its border
    /// at the border's own heights and ease to its median by 124 m, as OW8's plain did, and rise from there into far hills
    /// with their forests out to the fog. The heights are the ground's the rules collide with, read every 2 m round the border.
    /// </summary>
    private void PrepareHorizon(OpenMap map)
    {
        var bounds = stream.Bounds;
        if (bounds.X1 < bounds.X0)
        {
            foliage.Use(map, null);
            return;
        }
        if (horizonLoaded && ReferenceEquals(horizonMap, map)) return;
        if (horizonLoaded) Gpu.Unload(horizon);
        horizonLoaded = false;
        var ground = map.Ground;
        // The rings to the horizon (plan 32 · OW9), their hills the map's own by its name, and their far forests
        var land = OpenHorizon.Of(bounds, (x, z) => ground.Footing(x, z, 2000, -2000)?.Y is { } y ? (float)y : null, Core.SplitMix.Fnv(map.Name));
        horizon = land.Mesh.Pack();
        Gpu.Upload(ref horizon, "openchunk");
        HorizonTriangles = land.Mesh.Triangles;
        HorizonTrees = land.Trees.Count;
        horizonLoaded = true;
        horizonMap = map;
        foliage.Use(map, land.Trees);
    }

    private OpenMap? horizonMap;

    /// <summary>How far under the region the first ring begins, and how far out it keeps the border's own heights, in metres (<see cref="OpenHorizon"/>'s).</summary>
    public const float HorizonUnder = OpenHorizon.Under, HorizonNear = OpenHorizon.Near;

    /// <summary>How far out the land has come to the border's median height, in metres, where the far hills begin.</summary>
    public const float HorizonLevel = OpenHorizon.Level;

    /// <summary>How far under the border's own height the first rings lie: under the region's ground, too little for the outlines.</summary>
    public const float HorizonDrop = OpenHorizon.Drop;

    /// <summary>
    /// The land beyond a region as vertices (GPU-free; <see cref="OpenHorizon.Of"/> without its trees, the hills of seed
    /// nought): the region's extent in chunks, and the ground's height at a point (null where there is none).
    /// </summary>
    public static OpenVertices Horizon((int X0, int Z0, int X1, int Z1) bounds, Func<float, float, float?> heightAt) =>
        OpenHorizon.Of(bounds, heightAt, 0u, trees: false).Mesh;

    // ------------------------------------------------------------------ the weather

    // Where the weather stops in each metre's column of the map: the first surface a ray down from the sky meets
    private readonly Dictionary<(int, int), float> shelter = new();
    private OpenMap? shelterMap;

    /// <summary>How high the sky's weather comes down in a column of the map: the first ground a ray from far over it meets, kept once asked (a metre's column).</summary>
    private float ShelterAt(OpenMap map, float x, float z)
    {
        if (!ReferenceEquals(shelterMap, map)) { shelter.Clear(); shelterMap = map; }
        var key = ((int)MathF.Floor(x), (int)MathF.Floor(z));
        if (shelter.TryGetValue(key, out float height)) return height;
        const double From = 400;
        var hit = map.Ground.Raycast(new OpenVec(key.Item1 + 0.5, From, key.Item2 + 0.5), new OpenVec(0, -1, 0), From * 2);
        height = hit is { } t ? (float)(From - t) : float.MinValue;
        shelter[key] = height;
        return height;
    }

    private void DrawWeather(OpenScene scene, LightRig rig)
    {
        // Nothing falls under what roofs it over (a tunnel seen from its mouth) or inside rock: only over the first ground
        // the sky meets
        OpenWeatherFx.Pieces(scene.Weather, scene.Eye, scene.Time, 1f - scene.CaveShare, pieces, (x, z) => ShelterAt(scene.Map, x, z));
        WeatherPieces = pieces.Count;
        if (pieces.Count == 0) return;
        // Lit by the scene's light as it stands: no rain glows at night
        var lightLevel = rig.Light.SkyAmbient * 0.8f + rig.Light.SunColor * MathF.Max(0f, rig.Light.SunDirection.Y) * 0.9f;
        float lit = Math.Clamp((lightLevel.X + lightLevel.Y + lightLevel.Z) / 3f * 1.35f, 0.25f, 1.1f);
        Rlgl.DrawRenderBatchActive();
        Raylib.BeginBlendMode(BlendMode.Alpha);
        Rlgl.DisableDepthMask();
        Rlgl.DisableBackfaceCulling();
        Rlgl.SetTexture(Rlgl.GetTextureIdDefault());
        Rlgl.Begin(DrawMode.Quads);
        foreach (var piece in pieces)
        {
            var along = piece.Head - piece.Tail;
            var toEye = scene.Eye - (piece.Head + piece.Tail) * 0.5f;
            var side = Vector3.Cross(along.LengthSquared() > 1e-8f ? along : Vector3.UnitY, toEye);
            if (side.LengthSquared() < 1e-10f) continue;
            side = Vector3.Normalize(side) * (piece.Width * 0.5f);
            var c = piece.Colour;
            Rlgl.Color4ub((byte)Math.Min(255, c.R * lit), (byte)Math.Min(255, c.G * lit), (byte)Math.Min(255, c.B * lit), c.A);
            // A flake is as long as it is wide: a square facing the camera
            var tail = along.LengthSquared() < piece.Width * piece.Width ? piece.Head - Vector3.Normalize(Vector3.Cross(side, toEye)) * piece.Width : piece.Tail;
            Rlgl.Vertex3f(piece.Head.X - side.X, piece.Head.Y - side.Y, piece.Head.Z - side.Z);
            Rlgl.Vertex3f(tail.X - side.X, tail.Y - side.Y, tail.Z - side.Z);
            Rlgl.Vertex3f(tail.X + side.X, tail.Y + side.Y, tail.Z + side.Z);
            Rlgl.Vertex3f(piece.Head.X + side.X, piece.Head.Y + side.Y, piece.Head.Z + side.Z);
        }
        Rlgl.End();
        Rlgl.DrawRenderBatchActive();
        Rlgl.EnableDepthMask();
        Rlgl.EnableBackfaceCulling();
        Raylib.EndBlendMode();
    }

    private static Color ToColour(Vector3 c) =>
        new((int)Math.Clamp(c.X * 255f, 0f, 255f), (int)Math.Clamp(c.Y * 255f, 0f, 255f), (int)Math.Clamp(c.Z * 255f, 0f, 255f), 255);

    /// <summary>How bright a bubble's paper is under a rig: dimmed some of the way with the scene after dark, as the grid's (it is paper, not a lamp).</summary>
    private static float Paper(LightRig rig)
    {
        var level = rig.Light.SkyAmbient * 0.8f + rig.Light.SunColor * MathF.Max(0f, rig.Light.SunDirection.Y) * 0.9f;
        float brightness = Math.Clamp((level.X + level.Y + level.Z) / 3f * 1.35f, 0f, 1f);
        return 0.35f + 0.65f * brightness;
    }

    /// <summary>Lets go of the map's chunks, buildings and trees (another map, or none).</summary>
    public void Forget()
    {
        stream.Use(null);
        buildings.Use(null, context, shaders);
        foliage.Use(null, null);
        // The land beyond is made again with the trees, whatever map comes next
        horizonMap = null;
    }

    public void Unload()
    {
        stream.Use(null);
        cascades.Unload();
        if (horizonLoaded) Gpu.Unload(horizon);
        horizonLoaded = false;
        horizonMap = null;
        if (resources) Gpu.Unload(atlas);
        resources = false;
        buildings.Unload();
        foliage.Unload();
        figureDraw.Unload();
        shaders.Unload();
    }
}
