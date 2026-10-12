using System;
using System.Numerics;
using System.Threading.Tasks;
using PokemonPlatinumEngine.Battle;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// How a Substitute's doll and the Pokémon behind it look at one moment (plan 25 · Y1): worked out from the doll's
/// ages alone (<see cref="SubstituteDoll.LookOf"/>).
/// </summary>
internal readonly record struct DollLook
{
    /// <summary>Whether the doll is drawn at all.</summary>
    public bool Visible { get; init; }

    /// <summary>How far above its platform it still is as it drops in, in its own heights.</summary>
    public float Drop { get; init; }

    /// <summary>Squashed (positive) or stretched (negative) on landing and as it breathes.</summary>
    public float Squash { get; init; }

    /// <summary>How far it has stepped aside for its owner's own move, 0..1.</summary>
    public float Aside { get; init; }

    /// <summary>A hit's flinch: a tilt back and a jolt, -1..1.</summary>
    public float Flinch { get; init; }

    /// <summary>How far it has faded into its puff, 0..1 (white and shrinking).</summary>
    public float Fade { get; init; }

    /// <summary>How much of the Pokémon behind it shows (its scale), 0..1: none while the doll stands in front of it.</summary>
    public float PokemonGrow { get; init; }

    /// <summary>No doll: the Pokémon stands as ever.</summary>
    public static DollLook None => new() { PokemonGrow = 1f };
}

/// <summary>
/// The Substitute's doll (plan 25 · Y1; style guide, "The Substitute"): our own small figure, a plush doll of undyed
/// cloth with a round head, two nubs of ears, button eyes, a patch on its belly and a seam stitched down its front,
/// sculpted with the SDF kit, meshed once and cached like <see cref="BattleBall"/> and preloaded with it. It stands on
/// the platform in front of its Pokémon, which shrinks out of sight as the doll drops in and grows back as the doll
/// fades in a puff; it flinches when a hit lands on it, and slides aside for its owner's own move and back. Every look
/// is a pure function of the doll's ages and its seed (<see cref="LookOf"/>, <see cref="Effects"/>).
/// </summary>
internal static class SubstituteDoll
{
    /// <summary>The model's height, its feet at nought.</summary>
    public const float Height = 1f;

    /// <summary>The doll's height on a platform, as a share of the platform's radius.</summary>
    public const float SizeOnPlatform = 0.75f;

    private const float Cell = 1f / 80f;
    private const float OutlineWidth = 0.022f;

    private static readonly Lazy<DollModel> Model = new(Build);
    private static readonly Matrix4x4[] Skin = { Matrix4x4.Identity };

    private static Color Rgb(int r, int g, int b) => new(r, g, b, 255);

    private static readonly Color Cloth = Rgb(226, 210, 176);
    private static readonly Color ClothShade = Rgb(204, 184, 148);
    private static readonly Color Patch = Rgb(196, 118, 92);
    private static readonly Color Thread = Rgb(120, 92, 70);
    private static readonly Color Button = Rgb(52, 44, 58);
    private static readonly Color Cheek = Rgb(238, 168, 150);

    private sealed class DollModel
    {
        public SdfMesh Body = new(), Shell = new();
        public SkinnedModel? Drawn, Outline;
    }

    /// <summary>Starts meshing the doll in the background (with the balls, as a battle's models are first wanted).</summary>
    public static void Preload() => Task.Run(() => Model.Value);

    // ------------------------------------------------------------------ sculpting

    /// <summary>The doll, its feet at the origin and its face toward +Z, <see cref="Height"/> tall.</summary>
    public static SdfModel Sculpt()
    {
        var m = new SdfModel("substitute doll") { ColorSharpness = 0.9f };
        const SurfaceMaterial cloth = SurfaceMaterial.Cloth;

        // Feet and legs: two soft stubs
        foreach (float side in new[] { -1f, 1f })
            m.Ellipsoid(new Vector3(side * 0.12f, 0.07f, 0.03f), new Vector3(0.1f, 0.075f, 0.13f), ClothShade, 0, 0.03f, cloth);

        // The body: a stuffed pear
        m.Ellipsoid(new Vector3(0, 0.3f, 0), new Vector3(0.24f, 0.25f, 0.2f), Cloth, 0, 0.06f, cloth);

        // Stub arms held a little out
        foreach (float side in new[] { -1f, 1f })
            m.Capsule(new Vector3(side * 0.18f, 0.4f, 0.02f), new Vector3(side * 0.31f, 0.26f, 0.06f), 0.075f, 0.065f, Cloth, 0, 0.04f, cloth);

        // The head, a little wider than tall, and its ears
        m.Ellipsoid(new Vector3(0, 0.71f, 0.01f), new Vector3(0.26f, 0.24f, 0.22f), Cloth, 0, 0.07f, cloth);
        foreach (float side in new[] { -1f, 1f })
            m.Capsule(new Vector3(side * 0.15f, 0.86f, 0f), new Vector3(side * 0.22f, 0.97f, -0.01f), 0.07f, 0.05f, ClothShade, 0, 0.03f, cloth);

        // The belly's patch and the seam stitched down the front and over the head
        m.Paint(m.Ellipsoid(new Vector3(0.05f, 0.27f, 0.2f), new Vector3(0.1f, 0.09f, 0.08f), Patch, 0));
        m.Paint(m.Box(new Vector3(0, 0.36f, 0.2f), new Vector3(0.012f, 0.2f, 0.06f), 0f, Thread, 0));
        m.Paint(m.Box(new Vector3(0, 0.88f, 0f), new Vector3(0.012f, 0.06f, 0.24f), 0f, Thread, 0));

        // Button eyes and two cheeks of thread
        foreach (float side in new[] { -1f, 1f })
        {
            m.Paint(m.Ellipsoid(new Vector3(side * 0.09f, 0.74f, 0.21f), new Vector3(0.045f, 0.05f, 0.06f), Button, 0));
            m.Paint(m.Ellipsoid(new Vector3(side * 0.16f, 0.64f, 0.18f), new Vector3(0.045f, 0.03f, 0.06f), Cheek, 0));
        }
        // A stitched mouth: a short line under the eyes
        m.Paint(m.Box(new Vector3(0, 0.63f, 0.22f), new Vector3(0.05f, 0.01f, 0.05f), 0f, Thread, 0));
        return m;
    }

    private static DollModel Build()
    {
        var sculpt = Sculpt();
        return new DollModel
        {
            Body = SdfCache.Get(sculpt, Cell),
            Shell = SdfCache.Get(sculpt, Cell * 1.25f, OutlineWidth)
        };
    }

    // ------------------------------------------------------------------ the look at an age

    private static float Smooth(float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        return t * t * (3f - 2f * t);
    }

    /// <summary>
    /// The doll and its Pokémon at the doll's ages. Dropping in: the Pokémon shrinks away over the first half while
    /// the doll falls from above and lands with a squash. Standing: it breathes; a hit tilts it back with a jolt; for
    /// its owner's own move it slides aside while the Pokémon shows, and back once the move has played. Fading: it
    /// whitens and shrinks into its puff while the Pokémon grows back. Nothing here reads a clock but the doll's own.
    /// </summary>
    public static DollLook LookOf(SubstituteView? doll)
    {
        if (doll == null || doll.Age < 0f) return DollLook.None;
        if (!doll.Up)
        {
            float u = Math.Clamp(doll.Age / BattleAnimator.SubstituteOutTime, 0f, 1f);
            return new DollLook
            {
                Visible = u < 1f,
                Fade = u,
                Squash = -0.15f * MathF.Sin(u * MathF.PI),
                PokemonGrow = Smooth((u - 0.25f) / 0.75f)
            };
        }

        float t = doll.Age / BattleAnimator.SubstituteInTime;
        if (t < 1f)
        {
            // Shrinks out over the first 45%; the doll falls from the 30% mark and lands at 80%, settling after
            float grow = 1f - Smooth(t / 0.45f);
            float fall = Math.Clamp((t - 0.3f) / 0.5f, 0f, 1f);
            float land = Math.Clamp((t - 0.8f) / 0.2f, 0f, 1f);
            return new DollLook
            {
                Visible = t >= 0.3f,
                Drop = 2.2f * (1f - fall * fall),
                Squash = land > 0f ? 0.22f * MathF.Sin(land * MathF.PI) * (1f - land * 0.4f) : -0.08f * fall,
                PokemonGrow = grow
            };
        }

        float aside = 0f;
        if (doll.AsideAge >= 0f)
        {
            float a = doll.AsideAge / BattleAnimator.SubstituteAsideTime;
            aside = Smooth(a / 0.2f) * (1f - Smooth((a - 0.8f) / 0.2f));
        }
        float flinch = 0f;
        if (doll.HitAge >= 0f)
        {
            float h = Math.Clamp(doll.HitAge / BattleAnimator.SubstituteHitTime, 0f, 1f);
            flinch = MathF.Sin(h * MathF.PI * 3f) * (1f - h);
        }
        return new DollLook
        {
            Visible = true,
            Squash = 0.03f * MathF.Sin(doll.Age * 2.4f + doll.Seed % 7),
            Aside = aside,
            Flinch = flinch,
            PokemonGrow = aside
        };
    }

    /// <summary>The doll's height on a platform of this radius (a double battle's places are smaller by <paramref name="fit"/>).</summary>
    public static float SizeOn(float platformRadius, float fit) => platformRadius * SizeOnPlatform * fit;

    /// <summary>
    /// Where the doll stands: on <paramref name="feet"/>, facing <paramref name="toward"/>, <paramref name="size"/> tall,
    /// as <paramref name="look"/> has it (dropping, squashed, aside, flinching, fading).
    /// </summary>
    public static Matrix4x4 Place(Vector3 feet, Vector3 toward, float size, DollLook look)
    {
        var flat = new Vector3(toward.X, 0, toward.Z);
        flat = flat.LengthSquared() > 1e-6f ? Vector3.Normalize(flat) : Vector3.UnitZ;
        var aside = Vector3.Normalize(Vector3.Cross(Vector3.UnitY, flat));
        float yaw = MathF.Atan2(flat.X, flat.Z);
        float shrink = 1f - 0.7f * look.Fade;
        float s = size / Height * shrink;
        var at = feet
            + Vector3.UnitY * look.Drop * size
            + aside * look.Aside * size * 1.1f
            - flat * look.Flinch * size * 0.06f;
        return Matrix4x4.CreateScale(1f + look.Squash * 0.6f, 1f - look.Squash, 1f + look.Squash * 0.6f)
            * Matrix4x4.CreateRotationX(-look.Flinch * 0.35f)
            * Matrix4x4.CreateRotationY(yaw)
            * Matrix4x4.CreateScale(s)
            * Matrix4x4.CreateTranslation(at);
    }

    /// <summary>
    /// What flies about the doll: a ring of dust as it lands, a few stars as a hit makes it flinch, and its puff of
    /// smoke as it fades. A pure function of its ages and its seed.
    /// </summary>
    public static void Effects(SubstituteView? doll, Vector3 feet, float size, FxList fx)
    {
        if (doll == null || doll.Age < 0f) return;
        int seed = doll.Seed;
        if (doll.Up)
        {
            // Dust as it lands (at 80% of its drop)
            float t = (doll.Age / BattleAnimator.SubstituteInTime - 0.8f) / 0.5f;
            if (t >= 0f && t < 1f)
            {
                for (int i = 0; i < 10; i++)
                {
                    float a = i * MathF.Tau / 10f + Fx.Rand(seed, i, 1) * 0.5f;
                    var dir = new Vector3(MathF.Cos(a), 0, MathF.Sin(a));
                    var p = feet + dir * size * (0.25f + 0.5f * Fx.EaseOut(t)) + Vector3.UnitY * size * (0.05f + 0.12f * t);
                    fx.Sprite(FxShape.Smoke, p, size * (0.12f + 0.1f * t), new Color(232, 222, 200, (int)(170 * (1f - t))), FxBlend.Alpha, depth: true);
                }
            }
            if (doll.HitAge >= 0f)
            {
                float h = Math.Clamp(doll.HitAge / BattleAnimator.SubstituteHitTime, 0f, 1f);
                for (int i = 0; i < 4; i++)
                {
                    float a = Fx.Rand(seed + 31, i, 2) * MathF.Tau;
                    var p = feet + Vector3.UnitY * size * (0.8f + 0.25f * h) + new Vector3(MathF.Cos(a), 0, MathF.Sin(a)) * size * (0.2f + 0.3f * h);
                    fx.Sprite(FxShape.Star, p, size * 0.1f * (1f - h * 0.5f), new Color(255, 236, 150, (int)(230 * (1f - h))), FxBlend.Alpha);
                }
            }
            return;
        }

        // The puff it fades into
        float u = Math.Clamp(doll.Age / BattleAnimator.SubstituteOutTime, 0f, 1f);
        if (u >= 1f) return;
        for (int i = 0; i < 12; i++)
        {
            var off = new Vector3(Fx.Rand(seed, i, 3) - 0.5f, Fx.Rand(seed, i, 4), Fx.Rand(seed, i, 5) - 0.5f);
            var p = feet + new Vector3(off.X * size * (0.5f + 0.7f * u), size * (0.2f + off.Y * 0.6f + 0.3f * u), off.Z * size * (0.5f + 0.7f * u));
            fx.Sprite(FxShape.Smoke, p, size * (0.22f + 0.3f * Fx.EaseOut(u)), new Color(240, 238, 246, (int)(210 * (1f - u))), FxBlend.Alpha, depth: true);
        }
    }

    // ------------------------------------------------------------------ drawing

    /// <summary>Draws the doll in one of the character passes; a fading doll whitens in the colour pass.</summary>
    public static void Draw(RenderContext context, Matrix4x4 world, CharacterPass pass, float fade)
    {
        var m = Model.Value;
        m.Drawn ??= SkinnedModel.Upload(m.Body, 1);
        m.Outline ??= SkinnedModel.Upload(m.Shell, 1);
        switch (pass)
        {
            case CharacterPass.Depth:
                m.Drawn.Draw(context.DepthSkinned, Skin, world);
                break;
            case CharacterPass.Color:
                if (fade > 0f) context.Shaders.SetFlash(Color.White, Math.Clamp(fade, 0f, 1f) * 0.9f);
                m.Drawn.Draw(context.ToonSkinned, Skin, world);
                if (fade > 0f) context.Shaders.SetFlash(default, 0f);
                break;
            case CharacterPass.Outline:
                context.Shaders.SetOutlineWidth(0f);
                Rlgl.DrawRenderBatchActive();
                Rlgl.EnableBackfaceCulling();
                Rlgl.SetCullFace(0);
                m.Outline.Draw(context.OutlineSkinned, Skin, world);
                Rlgl.SetCullFace(1);
                break;
        }
    }
}
