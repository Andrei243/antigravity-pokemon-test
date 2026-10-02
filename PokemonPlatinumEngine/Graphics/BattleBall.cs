using System;
using System.Collections.Concurrent;
using System.Numerics;
using System.Threading.Tasks;
using PokemonPlatinumEngine.Battle;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>The marks on a ball's top half.</summary>
internal enum BallMark { None, Wings, H, Domes, Stripes, Spots }

/// <summary>How one kind of ball looks: its two halves, the band round its middle and its marks.</summary>
internal readonly record struct BallLook(Color Top, Color Bottom, Color Band, Color Accent, BallMark Mark);

/// <summary>A ball's two halves, meshed and (once drawn) on the GPU.</summary>
internal sealed class BallModel
{
    public BallLook Look;
    public SdfMesh Top = new(), Bottom = new(), TopShell = new(), BottomShell = new();
    public SkinnedModel? TopBody, BottomBody, TopOutline, BottomOutline;
    public bool Uploaded;
}

/// <summary>
/// The Poké Ball in 3D (plan 04 · G8): two halves sculpted with the SDF kit, hinged at the back, with a dark band
/// and a white button, in the colours of its kind. Battles draw it thrown by the trainer, opening, wobbling and
/// clicking shut. Sculpting and meshing are GPU-free; models are meshed once and cached like the characters.
/// </summary>
internal static class BattleBall
{
    /// <summary>Radius of the model; battles scale it to the size of the place it is thrown at.</summary>
    public const float Radius = 0.5f;
    private const float Gap = 0.012f;
    private const float Wall = 0.045f;
    private const float Cell = 1f / 80f;
    private const float OutlineWidth = 0.024f;

    /// <summary>How far the lid swings open, radians.</summary>
    public const float OpenAngle = 1.9f;

    private static readonly ConcurrentDictionary<BallLook, Lazy<BallModel>> Models = new();
    private static readonly Matrix4x4[] Skin = { Matrix4x4.Identity };

    private static Color Rgb(int r, int g, int b) => new(r, g, b, 255);

    private static readonly Color Ink = Rgb(46, 42, 58);
    private static readonly Color White = Rgb(244, 244, 248);
    private static readonly Color Inside = Rgb(70, 66, 82);

    /// <summary>The colours of a ball by its item name (anything unknown looks like a Poké Ball).</summary>
    public static BallLook LookOf(string ball) => ball.ToUpperInvariant() switch
    {
        "GREAT BALL" => new(Rgb(54, 116, 226), White, Ink, Rgb(226, 54, 60), BallMark.Wings),
        "ULTRA BALL" => new(Rgb(50, 50, 60), White, Ink, Rgb(250, 206, 50), BallMark.H),
        "MASTER BALL" => new(Rgb(126, 52, 170), White, Ink, Rgb(236, 110, 180), BallMark.Domes),
        "PREMIER BALL" => new(White, White, Rgb(220, 50, 56), Rgb(220, 50, 56), BallMark.None),
        "SAFARI BALL" => new(Rgb(110, 160, 70), Rgb(232, 224, 190), Ink, Rgb(70, 110, 50), BallMark.Spots),
        "NET BALL" => new(Rgb(60, 180, 200), White, Ink, Rgb(30, 70, 110), BallMark.Stripes),
        "DIVE BALL" => new(Rgb(60, 120, 220), White, Ink, Rgb(150, 210, 255), BallMark.Stripes),
        "NEST BALL" => new(Rgb(120, 190, 60), Rgb(244, 220, 120), Ink, Rgb(80, 140, 40), BallMark.None),
        "REPEAT BALL" => new(Rgb(230, 100, 40), Rgb(250, 220, 90), Ink, Rgb(250, 220, 90), BallMark.Stripes),
        "TIMER BALL" => new(White, White, Ink, Rgb(220, 60, 50), BallMark.Stripes),
        "LUXURY BALL" => new(Rgb(48, 44, 52), Rgb(48, 44, 52), Rgb(220, 60, 50), Rgb(240, 200, 80), BallMark.Stripes),
        "DUSK BALL" => new(Rgb(48, 120, 60), Rgb(48, 44, 52), Ink, Rgb(240, 140, 60), BallMark.Stripes),
        "HEAL BALL" => new(Rgb(250, 150, 190), White, Ink, Rgb(255, 230, 240), BallMark.Stripes),
        "QUICK BALL" => new(Rgb(60, 140, 230), Rgb(250, 220, 80), Ink, Rgb(250, 220, 80), BallMark.Stripes),
        _ => new(Rgb(226, 54, 54), White, Ink, Rgb(226, 54, 54), BallMark.None)
    };

    /// <summary>The meshed model for a ball (built now if needed; a background build in progress is waited for).</summary>
    public static BallModel Get(string ball) => Get(LookOf(ball));

    public static BallModel Get(BallLook look) => Models.GetOrAdd(look, l => new Lazy<BallModel>(() => Build(l))).Value;

    /// <summary>Starts meshing these balls in the background.</summary>
    public static void Preload(params string[] balls)
    {
        foreach (var ball in balls)
        {
            var lazy = Models.GetOrAdd(LookOf(ball), l => new Lazy<BallModel>(() => Build(l)));
            Task.Run(() => lazy.Value);
        }
    }

    // ------------------------------------------------------------------ sculpting

    /// <summary>The top half: a hollow shell above the band, with its kind's marks.</summary>
    public static SdfModel TopModel(BallLook look)
    {
        var m = new SdfModel($"ball top {look}") { ColorSharpness = 0.92f };
        const SurfaceMaterial plastic = SurfaceMaterial.Plastic;
        m.Sphere(Vector3.Zero, Radius, look.Top, 0, mat: plastic);
        if (look.Mark == BallMark.Domes)
        {
            // The Master Ball's two round bumps
            m.Sphere(new Vector3(0.3f, 0.3f, 0.02f), 0.15f, look.Accent, 0, 0.03f, plastic);
            m.Sphere(new Vector3(-0.3f, 0.3f, 0.02f), 0.15f, look.Accent, 0, 0.03f, plastic);
        }
        m.Sphere(Vector3.Zero, Radius - Wall, look.Top, 0, op: SdfOp.Cut);
        m.Box(new Vector3(0, -Radius, 0), new Vector3(1f, Radius + Gap, 1f), 0f, look.Top, 0, op: SdfOp.Cut);

        Marks(m, look);
        m.Paint(m.Box(new Vector3(0, Gap + 0.045f, 0), new Vector3(1f, 0.045f, 1f), 0f, look.Band, 0));
        m.Paint(m.Sphere(Vector3.Zero, Radius - Wall + 0.012f, Inside, 0));
        return m;
    }

    /// <summary>The bottom half: a hollow shell below the band, with the button at the front.</summary>
    public static SdfModel BottomModel(BallLook look)
    {
        var m = new SdfModel($"ball bottom {look}") { ColorSharpness = 0.92f };
        const SurfaceMaterial plastic = SurfaceMaterial.Plastic;
        m.Sphere(Vector3.Zero, Radius, look.Bottom, 0, mat: plastic);
        m.Sphere(Vector3.Zero, Radius - Wall, look.Bottom, 0, op: SdfOp.Cut);
        m.Box(new Vector3(0, Radius, 0), new Vector3(1f, Radius + Gap, 1f), 0f, look.Bottom, 0, op: SdfOp.Cut);
        m.Paint(m.Box(new Vector3(0, -Gap - 0.045f, 0), new Vector3(1f, 0.045f, 1f), 0f, look.Band, 0));
        m.Paint(m.Sphere(Vector3.Zero, Radius - Wall + 0.012f, Inside, 0));

        // The button: a dark ring round a white disc, facing forward across the seam
        var forward = Quaternion.CreateFromAxisAngle(Vector3.UnitX, MathF.PI / 2f);
        m.Cylinder(new Vector3(0, 0, Radius - 0.03f), 0.14f, 0.05f, 0.02f, look.Band, 0, mat: plastic, rotation: forward);
        m.Cylinder(new Vector3(0, 0, Radius + 0.01f), 0.09f, 0.035f, 0.02f, White, 0, mat: plastic, rotation: forward);
        return m;
    }

    private static void Marks(SdfModel m, BallLook look)
    {
        switch (look.Mark)
        {
            case BallMark.Wings:
                // The Great Ball's red patches either side of the top
                foreach (float side in new[] { -1f, 1f })
                    m.Paint(m.Ellipsoid(new Vector3(side * 0.34f, 0.28f, 0f), new Vector3(0.16f, 0.13f, 0.4f), look.Accent, 0));
                break;
            case BallMark.H:
                // The Ultra Ball's yellow H: a bar down each side joined over the top
                foreach (float side in new[] { -1f, 1f })
                    m.Paint(m.Box(new Vector3(side * 0.3f, 0.3f, 0f), new Vector3(0.07f, 0.24f, 0.6f), 0f, look.Accent, 0));
                m.Paint(m.Box(new Vector3(0, 0.46f, 0f), new Vector3(0.32f, 0.08f, 0.07f), 0f, look.Accent, 0));
                break;
            case BallMark.Domes:
                // An "M" on the front, in white strokes
                var front = new Vector3(0, 0.27f, Radius);
                m.Paint(m.Box(front + new Vector3(-0.1f, 0, 0), new Vector3(0.022f, 0.08f, 0.1f), 0f, White, 0));
                m.Paint(m.Box(front + new Vector3(0.1f, 0, 0), new Vector3(0.022f, 0.08f, 0.1f), 0f, White, 0));
                m.Paint(m.Box(front + new Vector3(-0.05f, 0.03f, 0), new Vector3(0.018f, 0.06f, 0.1f), 0f, White, 0,
                    rotation: Quaternion.CreateFromAxisAngle(Vector3.UnitZ, 0.6f)));
                m.Paint(m.Box(front + new Vector3(0.05f, 0.03f, 0), new Vector3(0.018f, 0.06f, 0.1f), 0f, White, 0,
                    rotation: Quaternion.CreateFromAxisAngle(Vector3.UnitZ, -0.6f)));
                break;
            case BallMark.Stripes:
                m.Paint(m.Box(new Vector3(0, 0.3f, 0f), new Vector3(1f, 0.05f, 1f), 0f, look.Accent, 0));
                break;
            case BallMark.Spots:
                for (int i = 0; i < 5; i++)
                {
                    float a = i * MathF.Tau / 5f;
                    m.Paint(m.Sphere(new Vector3(MathF.Cos(a) * 0.36f, 0.3f, MathF.Sin(a) * 0.36f), 0.1f, look.Accent, 0));
                }
                break;
        }
    }

    /// <summary>Sculpts and meshes both halves and their outline shells (no GPU calls).</summary>
    internal static BallModel Build(BallLook look)
    {
        var top = TopModel(look);
        var bottom = BottomModel(look);
        return new BallModel
        {
            Look = look,
            Top = SdfCache.Get(top, Cell),
            Bottom = SdfCache.Get(bottom, Cell),
            TopShell = SdfCache.Get(top, Cell * 1.25f, OutlineWidth),
            BottomShell = SdfCache.Get(bottom, Cell * 1.25f, OutlineWidth)
        };
    }

    // ------------------------------------------------------------------ placing

    /// <summary>About where the player's hand lets go of a ball thrown at a wild Pokémon.</summary>
    public static readonly Vector3 CaptureHand = BattleStage.ThrowSpot + new Vector3(0, 1.4f, 0);

    /// <summary>How high a ball thrown at a wild Pokémon arcs on its way there.</summary>
    public const float CaptureArc = 2.2f;

    /// <summary>Where a ball thrown at a foe opens: over its middle, a little toward the camera.</summary>
    public static Vector3 CaptureOpen(FxPlaces places, int slot)
    {
        float h = places.Height(BattleSide.Enemy, slot);
        var feet = places.Feet(BattleSide.Enemy, slot);
        var toCamera = Vector3.Normalize(new Vector3(BattleStage.CameraPosition.X - feet.X, 0, BattleStage.CameraPosition.Z - feet.Z));
        return places.Body(BattleSide.Enemy, slot) + Vector3.UnitY * h * 0.35f + toCamera * h * 0.25f;
    }

    /// <summary>
    /// Where the ball is along a throw: a high arc from <paramref name="from"/> to <paramref name="to"/>, the top
    /// <paramref name="height"/> above the higher end (t 0..1).
    /// </summary>
    public static Vector3 Arc(Vector3 from, Vector3 to, float height, float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        var p = Vector3.Lerp(from, to, t);
        p.Y += height * 4f * t * (1f - t);
        return p;
    }

    /// <summary>
    /// The ball's placement: <paramref name="center"/> is the middle of the ball, <paramref name="size"/> its world
    /// diameter, <paramref name="yaw"/> turns its button to face that way, <paramref name="tumble"/> spins it end
    /// over end, <paramref name="rock"/> rocks it on its base (a wobble).
    /// </summary>
    public static Matrix4x4 Place(Vector3 center, float size, float yaw, float tumble = 0f, float rock = 0f)
    {
        float s = size / (2f * Radius);
        return Matrix4x4.CreateRotationX(tumble)
            * Matrix4x4.CreateTranslation(0, Radius, 0) * Matrix4x4.CreateRotationZ(rock) * Matrix4x4.CreateTranslation(0, -Radius, 0)
            * Matrix4x4.CreateRotationY(yaw)
            * Matrix4x4.CreateScale(s)
            * Matrix4x4.CreateTranslation(center);
    }

    // ------------------------------------------------------------------ drawing

    private static void EnsureUploaded(BallModel m)
    {
        if (m.Uploaded) return;
        m.TopBody = SkinnedModel.Upload(m.Top, 1);
        m.BottomBody = SkinnedModel.Upload(m.Bottom, 1);
        m.TopOutline = SkinnedModel.Upload(m.TopShell, 1);
        m.BottomOutline = SkinnedModel.Upload(m.BottomShell, 1);
        m.Uploaded = true;
    }

    /// <param name="open">0 shut .. 1 the lid swung right back on its hinge.</param>
    public static void Draw(RenderContext context, BallModel m, Matrix4x4 world, float open, CharacterPass pass)
    {
        EnsureUploaded(m);
        // The lid turns about the hinge at the back of the seam
        var lid = Matrix4x4.CreateTranslation(0, 0, Radius) * Matrix4x4.CreateRotationX(-open * OpenAngle) * Matrix4x4.CreateTranslation(0, 0, -Radius) * world;
        switch (pass)
        {
            case CharacterPass.Depth:
                m.BottomBody!.Draw(context.DepthSkinned, Skin, world);
                m.TopBody!.Draw(context.DepthSkinned, Skin, lid);
                break;
            case CharacterPass.Color:
                m.BottomBody!.Draw(context.ToonSkinned, Skin, world);
                m.TopBody!.Draw(context.ToonSkinned, Skin, lid);
                break;
            case CharacterPass.Outline:
                context.Shaders.SetOutlineWidth(0f);
                Rlgl.DrawRenderBatchActive();
                Rlgl.EnableBackfaceCulling();
                Rlgl.SetCullFace(0);
                m.BottomOutline!.Draw(context.OutlineSkinned, Skin, world);
                m.TopOutline!.Draw(context.OutlineSkinned, Skin, lid);
                Rlgl.SetCullFace(1);
                break;
        }
    }
}
