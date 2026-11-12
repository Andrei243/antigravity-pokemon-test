using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// The sky a flight is seen in (plan 38 · EX7, part a; style guide, "Soaring"): the region's own picture
/// (<see cref="RegionMapPictures"/>, a texel a tile, unfiltered) laid flat at the height of the ground with the sea's deep
/// blue past its edge, under the hour's sky and light; clouds of our own that drift with the wind and throw their shadows
/// on the land; a beacon of light over every open place Fly lands; and the flier's battle model from behind, wings at their
/// beat, with the player sitting on its back, banked into a turn. One 3D scene a frame into the shared scene target, every
/// uniform set here each frame, as <see cref="CourseRenderer"/> does. Where a model isn't made yet it is left out.
/// </summary>
internal sealed class SkyRenderer
{
    /// <summary>World units to a cell of the region map, and the flier's altitude at the lowest and highest heights.</summary>
    public const float CellSize = 8f, LowAltitude = 10f, HighAltitude = 34f;

    /// <summary>The camera's pitch under the horizon at the lowest and highest heights, its distance behind the flier, and the flier's length.</summary>
    public const float PitchLow = 34f, PitchHigh = 52f, Distance = 15f, FlierLength = 3.2f;

    /// <summary>How far the flier leans forward into its flight, in degrees.</summary>
    public const float Lean = 55f;

    /// <summary>The sea past the picture's edge (style guide, "Soaring").</summary>
    public static readonly Color Sea = new(40, 72, 128, 255);

    /// <summary>The beacons' gold, and a cloud's white.</summary>
    public static readonly Color Beacon = new(255, 214, 120, 255), Cloud = new(244, 246, 250, 255);

    /// <summary>A place of the day's beacon (part b): the sky's own violet-blue, told apart from the towns' gold.</summary>
    public static readonly Color PlaceBeacon = new(150, 170, 255, 255);

    private const float Near = 0.5f, Far = 1400f;

    private readonly RenderContext context;
    private Texture2D cloudTexture;
    private bool cloudLoaded;
    private string riderLook = "PLAYER";

    public SkyRenderer(RenderContext context) => this.context = context;

    /// <summary>Whether the flier was inside a cloud at the last frame rendered: the picture is veiled white at a third.</summary>
    public bool InCloud { get; private set; }

    /// <summary>The altitude of a height of one to five, eased between (style guide: never a jump).</summary>
    public static float AltitudeOf(float height) =>
        LowAltitude + (Math.Clamp(height, Soaring.Lowest, Soaring.Highest) - Soaring.Lowest) / (Soaring.Highest - Soaring.Lowest) * (HighAltitude - LowAltitude);

    /// <summary>The camera's pitch at a height, in degrees.</summary>
    public static float PitchOf(float height) =>
        PitchLow + (Math.Clamp(height, Soaring.Lowest, Soaring.Highest) - Soaring.Lowest) / (Soaring.Highest - Soaring.Lowest) * (PitchHigh - PitchLow);

    /// <summary>The flier's place in the world.</summary>
    public static Vector3 PlaceOf(Soaring flight) => new(flight.X * CellSize, AltitudeOf(flight.Height), flight.Y * CellSize);

    /// <summary>The way the flier flies, flat: heading from north, clockwise.</summary>
    public static Vector3 ForwardOf(Soaring flight)
    {
        float rad = flight.Heading * MathF.PI / 180f;
        return new Vector3(MathF.Sin(rad), 0f, -MathF.Cos(rad));
    }

    /// <summary>The camera: behind and above the flier at its height's pitch, looking at it. GPU-free.</summary>
    public static (Vector3 Eye, Vector3 Target) CameraOf(Soaring flight)
    {
        var at = PlaceOf(flight);
        var forward = ForwardOf(flight);
        float pitch = PitchOf(flight.Height) * MathF.PI / 180f;
        var eye = at - forward * (Distance * MathF.Cos(pitch)) + Vector3.UnitY * (Distance * MathF.Sin(pitch));
        return (eye, at + forward * 2f);
    }

    /// <summary>A cloud sheet: its middle in cells, its half size in cells and its altitude.</summary>
    public readonly record struct CloudSheet(float X, float Y, float HalfW, float HalfH, float Altitude);

    /// <summary>
    /// The clouds over a region at a time: sheets laid by the region's name on a grid of five cells (a hash, never a draw),
    /// between the second and fourth heights, drifting east with the hour's wind and coming round within the view. GPU-free.
    /// </summary>
    public static List<CloudSheet> CloudsOf(RegionMap region, float time, float wind = 0f)
    {
        var view = region.View;
        var sheets = new List<CloudSheet>();
        uint seed = 2166136261u;
        foreach (char c in region.Region) seed = (seed ^ c) * 16777619u;
        float drift = (0.12f + wind * 0.3f) * time;
        const int Grid = 5;
        for (int gy = 0; gy * Grid < view.Height; gy++)
            for (int gx = -1; gx * Grid < view.Width; gx++)
            {
                uint h = Hash(seed, gx, gy);
                if ((h & 3u) == 0u) continue;
                float fx = (h >> 4 & 255) / 255f, fy = (h >> 12 & 255) / 255f, fs = (h >> 20 & 255) / 255f;
                float span = view.Width + Grid * 2f;
                float x = ((gx * Grid + fx * Grid + drift) % span + span) % span - Grid + view.X;
                float y = view.Y + gy * Grid + fy * Grid;
                float alt = AltitudeOf(2f + fs * 2f);
                sheets.Add(new CloudSheet(x, y, 0.6f + fs * 0.8f, 0.4f + fx * 0.5f, alt));
            }
        return sheets;
    }

    private static uint Hash(uint seed, int x, int y)
    {
        uint h = seed ^ (uint)(x * 73856093) ^ (uint)(y * 19349663);
        h ^= h >> 16; h *= 0x7feb352du; h ^= h >> 15; h *= 0x846ca68bu; h ^= h >> 16;
        return h;
    }

    /// <summary>Whether a place is inside a cloud sheet (within its ellipse and a height and a half of it).</summary>
    public static bool Inside(CloudSheet c, Vector3 at)
    {
        float dx = (at.X / CellSize - c.X) / c.HalfW, dy = (at.Z / CellSize - c.Y) / c.HalfH;
        return dx * dx + dy * dy <= 1f && MathF.Abs(at.Y - c.Altitude) <= 2.5f;
    }

    /// <summary>The look of the light at an hour (the field's outdoor rig, style guide: "The light is the hour's").</summary>
    public static LightRig RigAt(float hour) => ArtLook.FieldRig(hour, indoors: false);

    /// <summary>Renders a frame of the flight offscreen, the rider in <paramref name="riderLook"/>; call outside any other texture mode.</summary>
    public void Render(Soaring flight, Pokemon? flier, float age, float bank, float hour, string riderLook)
    {
        this.riderLook = riderLook;
        context.EnsureLoaded();
        var shaders = context.Shaders;
        var region = flight.Map.Region;
        var rig = RigAt(hour);
        var light = rig.Light;
        float time = age;
        var (eye, target) = CameraOf(flight);
        var camera = new Camera3D(eye, target, Vector3.UnitY, Canvas2D.Layout.FovYFor(40f), CameraProjection.Perspective);
        var at = PlaceOf(flight);
        var clouds = CloudsOf(region, time);
        InCloud = clouds.Exists(c => Inside(c, at));

        shaders.SetTime(time);
        shaders.SetWind(0.3f);
        shaders.SetWalkers(Array.Empty<Vector3>());

        // 1. The shadow map round the flier: only the rider and its flier cast
        var lightCamera = context.Shadows.LightCamera(at, light.SunDirection, 12f);
        Raylib.BeginTextureMode(context.Shadows.Target);
        Raylib.ClearBackground(Color.White);
        Rlgl.SetClipPlanes(1.0, 200.0);
        Raylib.BeginMode3D(lightCamera);
        var lightView = Rlgl.GetMatrixModelview();
        var lightProjection = Rlgl.GetMatrixProjection();
        Rlgl.DisableBackfaceCulling();
        DrawRider(flier, flight, age, bank, CharacterPass.Depth);
        Rlgl.EnableBackfaceCulling();
        Raylib.EndMode3D();
        Raylib.EndTextureMode();

        // 2. The lens's view
        shaders.SetLighting(Raymath.MatrixMultiply(lightView, lightProjection), light, camera.Position, context.Shadows.Texel);
        shaders.SetCharacterStyle(shadowStrength: 1f, rimStrength: rig.Rim);
        shaders.SetWorldRamp(ArtLook.BattleRamp);
        shaders.SetFog(rig.FogColor, 0f, rig.FogNear, rig.FogFar);
        shaders.SetCloudShade(0f);
        shaders.SetGlow(0f, 0f, Vector3.Zero);
        shaders.SetUpright(0f, 0f);
        shaders.SetStageWash(StageLight.None);
        shaders.SetFlash(default, 0f);

        int tw = context.PartWidth, th = context.PartHeight;
        context.BeginScene();
        SkyPainter.Draw(time, tw, th, rig.Sky, clouds: false);
        Rlgl.SetClipPlanes(Near, Far);
        Raylib.BeginMode3D(camera);
        var landTint = LandTint(light);
        DrawLand(region, landTint, rig);
        DrawCloudShadows(clouds);
        context.BindShadowMap();
        Rlgl.DisableBackfaceCulling();
        DrawRider(flier, flight, age, bank, CharacterPass.Color);
        Rlgl.EnableBackfaceCulling();
        DrawRider(flier, flight, age, bank, CharacterPass.Outline);
        context.UnbindShadowMap();
        DrawClouds(clouds, camera.Position, rig, landTint);
        DrawBeacons(flight, time);
        Raylib.EndMode3D();
        Raylib.EndTextureMode();
        Rlgl.SetClipPlanes(0.01, 1000.0);
        // No depth of field nor tilt-shift from the sky: the land is far below and wants to be read sharp
        context.PreparePost(rig.Post with { TiltShift = 0f, Dof = 0f, AoStrength = 0f, OutlineStrength = 0f }, new DepthRange(Near, Far, camera.FovY, (float)context.Width / context.Height));
    }

    /// <summary>Draws the rendered sky through the post passes into the current target, veiled where the flier is in a cloud.</summary>
    public void DrawToScreen()
    {
        if (!context.Loaded) return;
        Canvas2D.Draw((w, h) =>
        {
            context.Composite(new Rectangle(0, 0, w, h));
            if (InCloud) Raylib.DrawRectangle(0, 0, w, h, Cloud with { A = 85 });
        });
    }

    // The land's colour under the hour's light: the sun's colour and the sky's, so after dark it is dimmed as the field is
    private static Color LandTint(SceneLighting light)
    {
        var c = light.SunColor * 0.62f + light.SkyAmbient * 0.5f;
        static byte B(float v) => (byte)Math.Clamp(v * 255f, 0f, 255f);
        return new Color(B(c.X), B(c.Y), B(c.Z), (byte)255);
    }

    private static Color Times(Color a, Color tint) => new(a.R * tint.R / 255, a.G * tint.G / 255, a.B * tint.B / 255, a.A);

    // The sea to the horizon, then the region's picture over it at the height of the ground
    private static void DrawLand(RegionMap region, Color tint, LightRig rig)
    {
        var view = region.View;
        float x0 = view.X * CellSize, z0 = view.Y * CellSize, x1 = (view.X + view.Width) * CellSize, z1 = (view.Y + view.Height) * CellSize;
        float cx = (x0 + x1) / 2f, cz = (z0 + z1) / 2f, far = Far;
        var sea = Times(Sea, tint);
        Rlgl.Begin(DrawMode.Quads);
        Rlgl.Color4ub(sea.R, sea.G, sea.B, 255);
        Rlgl.Vertex3f(cx - far, -0.05f, cz - far);
        Rlgl.Vertex3f(cx - far, -0.05f, cz + far);
        Rlgl.Vertex3f(cx + far, -0.05f, cz + far);
        Rlgl.Vertex3f(cx + far, -0.05f, cz - far);
        Rlgl.End();
        if (RegionMapPictures.Get(region) is not var (full, _)) return;
        Rlgl.SetTexture(full.Id);
        Rlgl.Begin(DrawMode.Quads);
        Rlgl.Color4ub(tint.R, tint.G, tint.B, 255);
        Rlgl.TexCoord2f(0f, 0f); Rlgl.Vertex3f(x0, 0f, z0);
        Rlgl.TexCoord2f(0f, 1f); Rlgl.Vertex3f(x0, 0f, z1);
        Rlgl.TexCoord2f(1f, 1f); Rlgl.Vertex3f(x1, 0f, z1);
        Rlgl.TexCoord2f(1f, 0f); Rlgl.Vertex3f(x1, 0f, z0);
        Rlgl.End();
        Rlgl.SetTexture(0);
    }

    private Texture2D CloudTexture
    {
        get
        {
            if (cloudLoaded) return cloudTexture;
            // A soft-edged white sheet: full in its middle, fading over its outer third
            const int size = 96;
            var canvas = new PixelCanvas(size, size);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f) / size * 2f - 1f, dy = (y + 0.5f) / size * 2f - 1f;
                    float d = MathF.Sqrt(dx * dx + dy * dy);
                    float a = Math.Clamp((1f - d) / 0.34f, 0f, 1f);
                    canvas.SetRaw(x, y, new Color((byte)255, (byte)255, (byte)255, (byte)(a * 255)));
                }
            cloudTexture = canvas.ToTexture(owner: "sky");
            Raylib.SetTextureFilter(cloudTexture, TextureFilter.Bilinear);
            cloudLoaded = true;
            return cloudTexture;
        }
    }

    private void FlatQuad(float cx, float cz, float hw, float hh, float y, Color c)
    {
        Rlgl.SetTexture(CloudTexture.Id);
        Rlgl.Begin(DrawMode.Quads);
        Rlgl.Color4ub(c.R, c.G, c.B, c.A);
        Rlgl.TexCoord2f(0f, 0f); Rlgl.Vertex3f(cx - hw, y, cz - hh);
        Rlgl.TexCoord2f(0f, 1f); Rlgl.Vertex3f(cx - hw, y, cz + hh);
        Rlgl.TexCoord2f(1f, 1f); Rlgl.Vertex3f(cx + hw, y, cz + hh);
        Rlgl.TexCoord2f(1f, 0f); Rlgl.Vertex3f(cx + hw, y, cz - hh);
        Rlgl.End();
        Rlgl.SetTexture(0);
    }

    // A cloud's shadow on the land: its shape in black at a sixth, under where the sun stands over it
    private void DrawCloudShadows(List<CloudSheet> clouds)
    {
        Rlgl.DisableDepthMask();
        foreach (var c in clouds) FlatQuad(c.X * CellSize, c.Y * CellSize, c.HalfW * CellSize, c.HalfH * CellSize, 0.02f, new Color((byte)0, (byte)0, (byte)0, (byte)42));
        Rlgl.DrawRenderBatchActive();
        Rlgl.EnableDepthMask();
    }

    // The sheets themselves, the farthest first, soft white at two thirds tinted by the sky's cloud colour
    private void DrawClouds(List<CloudSheet> clouds, Vector3 eye, LightRig rig, Color light)
    {
        clouds.Sort((a, b) =>
            Vector3.DistanceSquared(new Vector3(b.X * CellSize, b.Altitude, b.Y * CellSize), eye)
                .CompareTo(Vector3.DistanceSquared(new Vector3(a.X * CellSize, a.Altitude, a.Y * CellSize), eye)));
        // The sheets take the hour's light as the land does, so after dark they are dim and the beacons are brightest
        var tint = Times(PixelCanvas.Mix(Cloud, rig.Sky.CloudTint, 0.35f), light);
        Rlgl.DisableDepthMask();
        foreach (var c in clouds)
        {
            // Two thirds white, fading out as the camera comes near a sheet, so it never hides the flier (the veil does that)
            float near = Vector3.Distance(new Vector3(c.X * CellSize, c.Altitude, c.Y * CellSize), eye) - c.HalfW * CellSize;
            byte alpha = (byte)(170 * Math.Clamp((near - 4f) / 16f, 0f, 1f));
            if (alpha > 0) FlatQuad(c.X * CellSize, c.Y * CellSize, c.HalfW * CellSize, c.HalfH * CellSize, c.Altitude, tint with { A = alpha });
        }
        Rlgl.DrawRenderBatchActive();
        Rlgl.EnableDepthMask();
    }

    /// <summary>The height of a beacon's column, past the highest altitude, and how its ring pulses: once a second.</summary>
    public const float BeaconTop = HighAltitude + 30f;

    public static float Pulse(float time) => time % 1f;

    // A column of light over each open place: gold, white under the flier; brightest at its foot, thinning upward,
    // with a ring at its foot that pulses once a second. Drawn after the clouds, so none hides a beacon's foot.
    private static void DrawBeacons(Soaring flight, float time)
    {
        var under = flight.BeaconUnder;
        var placeUnder = under == null ? flight.PlaceUnder : null;
        Rlgl.DisableDepthMask();
        Rlgl.DisableBackfaceCulling();
        var columns = flight.Beacons.Select(s => (Cell: s.Spot.Cells[0], Lit: s == under, Colour: Beacon))
            .Concat(flight.Places.Select(p => (Cell: (p.X, p.Y), Lit: p == placeUnder, Colour: PlaceBeacon))).ToList();
        foreach (var (cell, lit, own) in columns)
        {
            var (cx, cy) = cell;
            float x = (cx + 0.5f) * CellSize, z = (cy + 0.5f) * CellSize, r = CellSize * 0.35f;
            var colour = lit ? Color.White : own;
            // The column under the flier stops short of it, or the camera would look out from inside it
            float top = lit ? Math.Max(2f, AltitudeOf(flight.Height) - FlierLength) : BeaconTop;
            for (int k = 0; k < 2; k++)
            {
                float ax = k == 0 ? r : 0f, az = k == 0 ? 0f : r;
                Rlgl.Begin(DrawMode.Quads);
                Rlgl.Color4ub(colour.R, colour.G, colour.B, 200);
                Rlgl.Vertex3f(x - ax, 0.05f, z - az);
                Rlgl.Color4ub(colour.R, colour.G, colour.B, 0);
                Rlgl.Vertex3f(x - ax, top, z - az);
                Rlgl.Vertex3f(x + ax, top, z + az);
                Rlgl.Color4ub(colour.R, colour.G, colour.B, 200);
                Rlgl.Vertex3f(x + ax, 0.05f, z + az);
                Rlgl.End();
            }
            // The ring at its foot, growing and fading once a second
            float p = Pulse(time);
            Raylib.DrawCircle3D(new Vector3(x, 0.08f, z), r * (0.8f + 0.9f * p), Vector3.UnitX, 90f, colour with { A = (byte)(230 * (1f - p)) });
            Raylib.DrawCircle3D(new Vector3(x, 0.08f, z), r * 0.75f, Vector3.UnitX, 90f, colour with { A = 230 });
        }
        Rlgl.DrawRenderBatchActive();
        Rlgl.EnableBackfaceCulling();
        Rlgl.EnableDepthMask();
    }

    /// <summary>
    /// The flier's root: its battle model scaled to <see cref="FlierLength"/> at its height, turned to fly away from the
    /// camera and banked into the turn by up to 20°. GPU-free.
    /// </summary>
    public static Matrix4x4 FlierRoot(Soaring flight, float bank, float modelHeight)
    {
        float scale = FlierLength / Math.Max(0.001f, modelHeight);
        float yaw = MathF.PI - flight.Heading * MathF.PI / 180f;
        var at = PlaceOf(flight);
        // Leant forward into its flight about its middle, so its back is under the rider and the camera sees it from behind
        return Matrix4x4.CreateScale(scale) * Matrix4x4.CreateTranslation(0f, -FlierLength * 0.5f, 0f) * Matrix4x4.CreateRotationX(Lean * MathF.PI / 180f)
            * Matrix4x4.CreateRotationZ(-bank * 20f * MathF.PI / 180f) * Matrix4x4.CreateRotationY(yaw) * Matrix4x4.CreateTranslation(at);
    }

    private void DrawRider(Pokemon? flier, Soaring flight, float age, float bank, CharacterPass pass)
    {
        if (flier == null || !PokemonModels.TryGet(flier.LookName, out var model) || model.Height <= 0.001f) return;
        var root = FlierRoot(flight, bank, model.Height);
        // Wings at their beat: the body plan's walk, a beat a little under a second
        var pose = new PokePose { Time = age, Walk = age * 1.2f % 1f, Moving = 1f };
        PokemonRenderer.Draw(context, model, pose, root, pass);
        // The player on its back, sitting, the rig's own look in the field
        if (CharacterModels.TryGet(riderLook, context.Shaders) is { } rig)
        {
            var back = Vector3.Transform(new Vector3(0f, model.Height * 0.55f, -model.Height * 0.28f), root);
            float yaw = MathF.PI - flight.Heading * MathF.PI / 180f;
            var riderRoot = Matrix4x4.CreateScale(0.9f) * Matrix4x4.CreateRotationZ(-bank * 20f * MathF.PI / 180f)
                * Matrix4x4.CreateRotationY(yaw) * Matrix4x4.CreateTranslation(back);
            var riderPose = new CharacterPose { Time = age, Stance = PokemonPlatinumEngine.Overworld.Stance.SitGround };
            CharacterRenderer.Draw(context, rig, riderPose, riderRoot, pass);
        }
    }

    /// <summary>Lets go of the sky's own textures.</summary>
    public void Unload()
    {
        if (!cloudLoaded) return;
        Gpu.Unload(cloudTexture);
        cloudLoaded = false;
    }
}
