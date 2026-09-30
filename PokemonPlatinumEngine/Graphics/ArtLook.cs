using System;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Core;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>Colour grading and screen effects applied when the 3D scene is composited.</summary>
internal readonly record struct PostSettings(
    float TiltShift,          // small blur radius (scene texels) at the top and bottom of the screen
    float Dof,                // 0..1 mix toward a wide blur at the top and bottom (depth of field)
    float FocusBand,          // half-height of the sharp band in the middle (0..1 of the screen)
    float BloomThreshold,     // luma above which pixels glow
    float BloomStrength,
    float Saturation,
    float Contrast,
    Vector3 ShadowTint,       // multiplied into the darks
    Vector3 HighlightTint,    // multiplied into the lights
    float Vignette,           // darkening in the corners (0..1)
    float AoStrength = 0f,    // screen-space ambient occlusion (0..1)
    float OutlineStrength = 0f) // darkening where depth steps (0..1)
{
    public static PostSettings Lerp(PostSettings a, PostSettings b, float t) => new(
        L(a.TiltShift, b.TiltShift, t), L(a.Dof, b.Dof, t), L(a.FocusBand, b.FocusBand, t),
        L(a.BloomThreshold, b.BloomThreshold, t), L(a.BloomStrength, b.BloomStrength, t),
        L(a.Saturation, b.Saturation, t), L(a.Contrast, b.Contrast, t),
        Vector3.Lerp(a.ShadowTint, b.ShadowTint, t), Vector3.Lerp(a.HighlightTint, b.HighlightTint, t),
        L(a.Vignette, b.Vignette, t), L(a.AoStrength, b.AoStrength, t), L(a.OutlineStrength, b.OutlineStrength, t));

    private static float L(float a, float b, float t) => a + (b - a) * t;
}

/// <summary>Colours of the painted battle sky.</summary>
internal readonly record struct SkyColors(Color Zenith, Color Middle, Color Horizon, Color CloudTint, float Stars)
{
    public static SkyColors Lerp(SkyColors a, SkyColors b, float t) => new(
        PixelCanvas.Mix(a.Zenith, b.Zenith, t), PixelCanvas.Mix(a.Middle, b.Middle, t), PixelCanvas.Mix(a.Horizon, b.Horizon, t),
        PixelCanvas.Mix(a.CloudTint, b.CloudTint, t), a.Stars + (b.Stars - a.Stars) * t);
}

/// <summary>Everything that changes with the time of day: light, fog, background, grading, window glow and sky.</summary>
internal readonly record struct LightRig(
    SceneLighting Light,
    Color Background,
    Vector3 FogColor,
    float FogAmount,
    float FogNear,
    float FogFar,
    PostSettings Post,
    float WindowGlow,
    float Rim,
    SkyColors Sky)
{
    public static LightRig Lerp(LightRig a, LightRig b, float t) => new(
        SceneLighting.Lerp(a.Light, b.Light, t), PixelCanvas.Mix(a.Background, b.Background, t),
        Vector3.Lerp(a.FogColor, b.FogColor, t), a.FogAmount + (b.FogAmount - a.FogAmount) * t,
        a.FogNear + (b.FogNear - a.FogNear) * t, a.FogFar + (b.FogFar - a.FogFar) * t,
        PostSettings.Lerp(a.Post, b.Post, t), a.WindowGlow + (b.WindowGlow - a.WindowGlow) * t,
        a.Rim + (b.Rim - a.Rim) * t, SkyColors.Lerp(a.Sky, b.Sky, t));
}

/// <summary>
/// The numbers of the "Sinnoh Diorama" style (docs/art/style-guide.md): an HD-2D field, 3D battles, and a light rig
/// for each of Platinum's five times of day, blended over the hour around each change.
/// </summary>
internal static class ArtLook
{
    /// <summary>How much battle scenery's diffuse light is banded like a cel-shaded model.</summary>
    public const float BattleRamp = 0.85f;

    /// <summary>Outline colour for 3D characters and Pokémon: surface colour mixed this far toward dark ink.</summary>
    public const float OutlineInkAmount = 0.5f;

    private static Vector3 V(float r, float g, float b) => new(r, g, b);
    private static Vector3 Dir(float x, float y, float z) => Vector3.Normalize(new Vector3(x, y, z));
    private static Color Rgb(int r, int g, int b) => new(r, g, b, 255);
    private static Vector3 Of(Color c) => new(c.R / 255f, c.G / 255f, c.B / 255f);

    // ------------------------------------------------------------------ blending over the clock

    /// <summary>Hours at which each period starts (a boundary at 24 wraps to 0).</summary>
    private static readonly (float Hour, TimeOfDay Before, TimeOfDay After)[] Boundaries =
    {
        (4f, TimeOfDay.LateNight, TimeOfDay.Morning),
        (10f, TimeOfDay.Morning, TimeOfDay.Day),
        (17f, TimeOfDay.Day, TimeOfDay.Twilight),
        (20f, TimeOfDay.Twilight, TimeOfDay.Night),
        (24f, TimeOfDay.Night, TimeOfDay.LateNight)
    };

    /// <summary>The rig for <paramref name="hour"/>: one period's rig, or a blend within half an hour of a change.</summary>
    private static LightRig AtHour(float hour, Func<TimeOfDay, LightRig> rigFor)
    {
        hour = ((hour % 24f) + 24f) % 24f;
        foreach (var (at, before, after) in Boundaries)
        {
            float d = hour - at;
            if (d < -12f) d += 24f;
            if (MathF.Abs(d) < 0.5f)
            {
                float t = d + 0.5f;
                return LightRig.Lerp(rigFor(before), rigFor(after), t * t * (3f - 2f * t));
            }
        }
        return rigFor(GameClock.ForHour((int)hour));
    }

    public static LightRig FieldRig(float hour, bool indoors) => indoors ? Indoors : AtHour(hour, FieldRigFor);

    public static LightRig BattleRig(float hour) => AtHour(hour, BattleRigFor);

    // ------------------------------------------------------------------ field (HD-2D)

    // Strong tilt-shift depth of field, warm highlights and violet shade
    private static readonly PostSettings FieldDayPost = new(6f, 0.95f, 0.3f, 0.86f, 0.38f, 1.1f, 1.08f,
        V(0.88f, 0.92f, 1.1f), V(1.07f, 1.0f, 0.9f), 0.26f);

    private static readonly SkyColors NoSky = new(Color.Black, Color.Black, Color.Black, Color.White, 0f);

    public static LightRig FieldRigFor(TimeOfDay time) => time switch
    {
        // Pale gold light through a morning haze
        TimeOfDay.Morning => new LightRig(
            new SceneLighting(Dir(-0.62f, 0.6f, 0.5f), V(0.64f, 0.54f, 0.46f), V(0.52f, 0.56f, 0.72f), V(0.46f, 0.45f, 0.44f)),
            Rgb(40, 96, 70), V(0.84f, 0.86f, 0.94f), 0.32f, 40f, 68f,
            FieldDayPost with { BloomStrength = 0.44f, Saturation = 1.04f, Contrast = 1.05f, ShadowTint = V(0.9f, 0.92f, 1.1f), HighlightTint = V(1.06f, 1.0f, 0.95f), Vignette = 0.24f },
            0f, 0.3f, NoSky),
        // Golden late-morning sun with violet-blue shade, like a lit diorama
        TimeOfDay.Day => new LightRig(
            new SceneLighting(Dir(-0.6f, 0.8f, 0.42f), V(0.66f, 0.55f, 0.4f), V(0.5f, 0.55f, 0.74f), V(0.46f, 0.44f, 0.4f)),
            Rgb(38, 100, 66), V(0.72f, 0.82f, 0.92f), 0.18f, 44f, 72f,
            FieldDayPost, 0f, 0.3f, NoSky),
        // Low orange sun, magenta-violet shade, lamps coming on
        TimeOfDay.Twilight => new LightRig(
            new SceneLighting(Dir(-0.72f, 0.42f, 0.5f), V(0.8f, 0.48f, 0.28f), V(0.44f, 0.38f, 0.6f), V(0.42f, 0.34f, 0.34f)),
            Rgb(40, 60, 62), V(0.9f, 0.62f, 0.52f), 0.28f, 42f, 70f,
            FieldDayPost with { BloomThreshold = 0.8f, BloomStrength = 0.55f, Saturation = 1.12f, ShadowTint = V(0.88f, 0.84f, 1.1f), HighlightTint = V(1.1f, 0.96f, 0.82f), Vignette = 0.3f },
            0.45f, 0.35f, NoSky),
        // Cool moonlight; warm windows glow and bloom
        TimeOfDay.Night => new LightRig(
            new SceneLighting(Dir(-0.38f, 0.86f, 0.34f), V(0.24f, 0.3f, 0.5f), V(0.2f, 0.26f, 0.44f), V(0.13f, 0.15f, 0.22f)),
            Rgb(12, 26, 34), V(0.1f, 0.14f, 0.28f), 0.35f, 40f, 68f,
            FieldDayPost with { BloomThreshold = 0.55f, BloomStrength = 0.7f, Saturation = 0.88f, Contrast = 1.06f, ShadowTint = V(0.85f, 0.9f, 1.18f), HighlightTint = V(1.0f, 1.0f, 1.05f), Vignette = 0.4f },
            1f, 0.5f, NoSky),
        // Deeper night; fewer lights still on
        _ => new LightRig(
            new SceneLighting(Dir(-0.34f, 0.88f, 0.3f), V(0.18f, 0.23f, 0.4f), V(0.15f, 0.2f, 0.36f), V(0.1f, 0.11f, 0.17f)),
            Rgb(8, 18, 26), V(0.07f, 0.1f, 0.22f), 0.4f, 38f, 66f,
            FieldDayPost with { BloomThreshold = 0.55f, BloomStrength = 0.6f, Saturation = 0.78f, Contrast = 1.06f, ShadowTint = V(0.85f, 0.9f, 1.2f), HighlightTint = V(1.0f, 1.0f, 1.06f), Vignette = 0.45f },
            0.6f, 0.5f, NoSky)
    };

    /// <summary>Rooms are lit by their own lamps, so they don't follow the clock; they get a gentler blur and less glow.</summary>
    public static readonly LightRig Indoors = new(
        new SceneLighting(Dir(-0.35f, 0.82f, 0.46f), V(0.46f, 0.42f, 0.36f), V(0.66f, 0.63f, 0.6f), V(0.52f, 0.47f, 0.42f)),
        Color.Black, V(0f, 0f, 0f), 0f, 100f, 200f,
        FieldDayPost with { TiltShift = 4f, Dof = 0.6f, FocusBand = 0.36f, BloomThreshold = 0.93f, BloomStrength = 0.22f },
        0f, 0.3f, NoSky);

    // ------------------------------------------------------------------ battles (3D)

    private static readonly PostSettings BattleDayPost = new(2f, 0.15f, 0.5f, 0.86f, 0.28f, 1.05f, 1.05f,
        V(0.94f, 0.97f, 1.06f), V(1.04f, 1.01f, 0.95f), 0.1f, AoStrength: 0.6f, OutlineStrength: 0.35f);

    private static LightRig Battle(SceneLighting light, SkyColors sky, float fog, float rim, PostSettings post) =>
        new(light, sky.Horizon, Of(sky.Horizon), fog, 40f, 140f, post, 0f, rim, sky);

    public static LightRig BattleRigFor(TimeOfDay time) => time switch
    {
        TimeOfDay.Morning => Battle(
            new SceneLighting(Dir(-0.55f, 0.62f, 0.5f), V(0.5f, 0.44f, 0.38f), V(0.54f, 0.57f, 0.72f), V(0.5f, 0.48f, 0.44f)),
            new SkyColors(Rgb(104, 150, 214), Rgb(178, 196, 232), Rgb(246, 224, 206), Rgb(255, 240, 236), 0f), 0.4f, 0.32f,
            BattleDayPost with { HighlightTint = V(1.05f, 1.0f, 0.96f) }),
        TimeOfDay.Day => Battle(
            new SceneLighting(Dir(-0.5f, 0.9f, 0.5f), V(0.47f, 0.43f, 0.35f), V(0.54f, 0.6f, 0.72f), V(0.5f, 0.5f, 0.4f)),
            new SkyColors(Rgb(78, 146, 226), Rgb(140, 194, 244), Rgb(224, 238, 250), Color.White, 0f), 0.35f, 0.3f,
            BattleDayPost),
        TimeOfDay.Twilight => Battle(
            new SceneLighting(Dir(-0.7f, 0.42f, 0.5f), V(0.66f, 0.4f, 0.24f), V(0.46f, 0.38f, 0.56f), V(0.42f, 0.34f, 0.32f)),
            new SkyColors(Rgb(54, 62, 128), Rgb(190, 110, 132), Rgb(252, 168, 108), Rgb(255, 196, 170), 0f), 0.35f, 0.45f,
            BattleDayPost with { BloomStrength = 0.38f, Saturation = 1.1f, ShadowTint = V(0.9f, 0.86f, 1.08f), HighlightTint = V(1.08f, 0.97f, 0.86f) }),
        TimeOfDay.Night => Battle(
            new SceneLighting(Dir(-0.4f, 0.85f, 0.4f), V(0.22f, 0.28f, 0.46f), V(0.24f, 0.3f, 0.46f), V(0.15f, 0.17f, 0.24f)),
            new SkyColors(Rgb(8, 14, 38), Rgb(18, 32, 72), Rgb(40, 58, 100), Rgb(70, 84, 120), 1f), 0.4f, 0.55f,
            BattleDayPost with { BloomThreshold = 0.75f, BloomStrength = 0.35f, Saturation = 0.9f, ShadowTint = V(0.88f, 0.92f, 1.12f), HighlightTint = V(0.98f, 1.0f, 1.06f), Vignette = 0.25f }),
        _ => Battle(
            new SceneLighting(Dir(-0.36f, 0.88f, 0.36f), V(0.18f, 0.23f, 0.4f), V(0.2f, 0.25f, 0.4f), V(0.12f, 0.14f, 0.2f)),
            new SkyColors(Rgb(4, 8, 26), Rgb(12, 22, 54), Rgb(28, 42, 80), Rgb(50, 60, 90), 1f), 0.45f, 0.55f,
            BattleDayPost with { BloomThreshold = 0.75f, BloomStrength = 0.3f, Saturation = 0.82f, ShadowTint = V(0.88f, 0.92f, 1.14f), HighlightTint = V(0.98f, 1.0f, 1.06f), Vignette = 0.3f })
    };
}
