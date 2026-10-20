using System;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Overworld;
using BattleWeather = PokemonPlatinumEngine.Battle.Sim.BattleWeather;

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

/// <summary>Everything that changes with the time of day: light, fog, background, grading, the lights after dark and the sky.</summary>
internal readonly record struct LightRig(
    SceneLighting Light,
    Color Background,
    Vector3 FogColor,
    float FogAmount,
    float FogNear,
    float FogFar,
    PostSettings Post,
    float LampGlow,           // lights that burn all night: street lamps, shops, signs (in rooms: daylight through the windows)
    float Rim,
    SkyColors Sky,
    float CloudShade = 0f,
    float HomeGlow = 0f)      // the windows of homes (in rooms: how far the glass has turned to the sky's colour)
{
    /// <summary>What lit glass turns into: lamplight outdoors, the sky seen through a window indoors.</summary>
    public Vector3 GlowColor { get; init; } = ArtLook.Lamplight;

    /// <summary>Tint of the light thrown on the ground (pools under lamps, patches under windows).</summary>
    public Vector3 LightTint { get; init; } = Vector3.One;

    /// <summary>
    /// How far the light people stand in is drawn toward its own grey (style guide, "Climates"; <see cref="ArtLook.ForPeople"/>):
    /// where a place's light is strongly coloured, a pale coat lit by the whole of it takes the colour of a face.
    /// Nought nearly everywhere: people are lit as the scenery is.
    /// </summary>
    public float PeopleWash { get; init; }

    public static LightRig Lerp(LightRig a, LightRig b, float t) => new(
        SceneLighting.Lerp(a.Light, b.Light, t), PixelCanvas.Mix(a.Background, b.Background, t),
        Vector3.Lerp(a.FogColor, b.FogColor, t), a.FogAmount + (b.FogAmount - a.FogAmount) * t,
        a.FogNear + (b.FogNear - a.FogNear) * t, a.FogFar + (b.FogFar - a.FogFar) * t,
        PostSettings.Lerp(a.Post, b.Post, t), a.LampGlow + (b.LampGlow - a.LampGlow) * t,
        a.Rim + (b.Rim - a.Rim) * t, SkyColors.Lerp(a.Sky, b.Sky, t), a.CloudShade + (b.CloudShade - a.CloudShade) * t,
        a.HomeGlow + (b.HomeGlow - a.HomeGlow) * t)
    {
        GlowColor = Vector3.Lerp(a.GlowColor, b.GlowColor, t),
        LightTint = Vector3.Lerp(a.LightTint, b.LightTint, t),
        PeopleWash = a.PeopleWash + (b.PeopleWash - a.PeopleWash) * t
    };
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

    /// <summary>
    /// How far the field straightens upright things on screen (see <see cref="FieldShaders.SetUpright"/>): without
    /// it, everything tall leans outward by up to 20° toward the sides of the screen.
    /// </summary>
    public const float FieldUpright = 1f;

    /// <summary>Warm lamplight: what lit windows and street lamps glow with.</summary>
    public static readonly Vector3 Lamplight = new(1f, 0.8f, 0.46f);

    public static LightRig FieldRig(float hour, bool indoors) => AtHour(hour, indoors ? IndoorRigFor : FieldRigFor);

    /// <summary>The field's light on a map: a room's by the clock through its windows, a cave's own, the sky's by the hour.</summary>
    public static LightRig FieldRig(float hour, Overworld.Map map) =>
        map.IsVoid ? VoidFieldRig : map.IsUnderwater ? UnderwaterFieldRig : map.IsCave && !map.IsIndoors ? CaveFieldRig : FieldRig(hour, map.IsIndoors);

    /// <summary>
    /// The sea floor (plan 21 · H5; style guide, "The sea floor"): one rig whatever the hour, as a cave's is. The
    /// light comes down through the water from nearly overhead, blue-green, with blue all round and a little warmth
    /// bounced off the sand; a close blue fog swallows the distance; the colour is drained and the corners darken. No
    /// sky, no cloud shade, no lamps.
    /// </summary>
    public static readonly LightRig UnderwaterFieldRig = new(
        new SceneLighting(Dir(-0.1f, 0.95f, 0.3f), V(0.5f, 0.66f, 0.66f), V(0.38f, 0.5f, 0.62f), V(0.36f, 0.4f, 0.36f)),
        Rgb(22, 58, 78), V(0.1f, 0.3f, 0.38f), 0.5f, 36f, 60f,
        FieldDayPost with
        {
            BloomThreshold = 0.9f, BloomStrength = 0.22f, Saturation = 0.8f, Contrast = 1.04f,
            ShadowTint = V(0.84f, 0.96f, 1.12f), HighlightTint = V(0.94f, 1.04f, 1.02f), Vignette = 0.34f
        },
        0f, 0.3f, NoSky);

    /// <summary>
    /// The Distortion World (style guide, "The Distortion World"): it ignores the clock. A dim, cool key from high
    /// over the left, violet light from all round and a cold teal bounce from below; the void behind everything is a
    /// deep violet that far islands fade into; the colour is drained and the corners close in.
    /// </summary>
    public static readonly LightRig VoidFieldRig = new(
        new SceneLighting(Dir(-0.32f, 0.88f, 0.34f), V(0.5f, 0.48f, 0.6f), V(0.46f, 0.42f, 0.58f), V(0.3f, 0.36f, 0.38f)),
        Rgb(36, 30, 50), V(0.16f, 0.13f, 0.21f), 0.4f, 40f, 76f,
        FieldDayPost with
        {
            BloomThreshold = 0.92f, BloomStrength = 0.2f, Saturation = 0.78f, Contrast = 1.08f,
            ShadowTint = V(0.92f, 0.86f, 1.16f), HighlightTint = V(0.98f, 1.03f, 1.0f), Vignette = 0.42f
        },
        0f, 0.32f, NoSky);

    /// <summary>
    /// The field inside a cave (style guide, "Caves"): it ignores the clock. A low warm key from the upper left,
    /// cool shade from above and a dim bounce from the floor; no fog, no cloud shade, no lamps; a strong vignette.
    /// The background is what the rock's tops fall to, so the picture ends in rock.
    /// </summary>
    public static readonly LightRig CaveFieldRig = new(
        // The key falls from nearly overhead, a little from the south-west: a wall's shadow stays on the lip
        // before it and never crosses a passage
        new SceneLighting(Dir(-0.18f, 0.92f, 0.35f), V(0.6f, 0.52f, 0.42f), V(0.44f, 0.47f, 0.58f), V(0.36f, 0.33f, 0.32f)),
        Rgb(24, 20, 26), V(0.09f, 0.08f, 0.1f), 0f, 60f, 120f,
        FieldDayPost with
        {
            BloomThreshold = 0.9f, BloomStrength = 0.2f, Saturation = 0.96f, Contrast = 1.06f,
            ShadowTint = V(0.86f, 0.9f, 1.12f), HighlightTint = V(1.06f, 1.0f, 0.9f), Vignette = 0.4f
        },
        0f, 0.3f, NoSky);

    /// <summary>How much of the sun gets through a kind of weather (style guide, "Life").</summary>
    public static float SunThrough(FieldWeather weather) => weather switch
    {
        FieldWeather.Clear => 1f,
        FieldWeather.Cloudy => 0.45f,
        FieldWeather.Rain => 0.3f,
        FieldWeather.HeavyRain => 0.22f,
        FieldWeather.Thunderstorm => 0.2f,
        FieldWeather.Snow => 0.6f,
        FieldWeather.HeavySnow => 0.35f,
        FieldWeather.Blizzard => 0.25f,
        FieldWeather.Hail => 0.4f,
        FieldWeather.Fog => 0.5f,
        FieldWeather.Sandstorm => 0.55f,
        _ => 0.75f
    };

    /// <summary>
    /// The field's light under a kind of weather (style guide, "Life"): part of the sun is lost and half of
    /// what it would have laid on level ground comes down from the whole sky instead, so shadows grow faint
    /// before the picture grows dark; fog thickens, and for fog, sandstorms and blizzards it comes near and
    /// takes their colour.
    /// </summary>
    public static LightRig Weathered(LightRig rig, FieldWeather weather)
    {
        if (weather == FieldWeather.Clear) return rig;
        float sun = SunThrough(weather);
        float fog = weather switch
        {
            FieldWeather.Rain => 0.12f, FieldWeather.HeavyRain or FieldWeather.Thunderstorm => 0.2f,
            FieldWeather.HeavySnow => 0.3f, FieldWeather.Blizzard => 0.4f, FieldWeather.Fog => 0.55f,
            FieldWeather.Sandstorm => 0.35f, FieldWeather.Hail => 0.08f, _ => 0.05f
        };
        bool thick = weather is FieldWeather.Fog or FieldWeather.Sandstorm or FieldWeather.HeavySnow or FieldWeather.Blizzard;
        var colour = weather == FieldWeather.Sandstorm ? V(0.86f, 0.74f, 0.52f) : V(0.9f, 0.92f, 0.95f);
        // What level ground loses is the lost sun times how high the sun stands: a low sun had little to give it
        var lost = rig.Light.SunColor * (1f - sun) * MathF.Max(0f, rig.Light.SunDirection.Y);
        float scattered = (lost.X + lost.Y + lost.Z) / 3f * 0.5f;
        // Light from an overcast sky is grey, a little to the blue; a sandstorm's is the sand's own colour
        var sky = weather == FieldWeather.Sandstorm ? V(1.08f, 0.98f, 0.8f) : V(0.94f, 1f, 1.08f);
        return rig with
        {
            Light = rig.Light with { SunColor = rig.Light.SunColor * sun, SkyAmbient = rig.Light.SkyAmbient + sky * scattered },
            FogAmount = MathF.Min(0.9f, rig.FogAmount + fog),
            FogNear = thick ? MathF.Min(rig.FogNear, 36f) : rig.FogNear,
            FogFar = thick ? MathF.Min(rig.FogFar, 54f) : rig.FogFar,
            FogColor = thick ? Vector3.Lerp(rig.FogColor, colour, 0.7f) : rig.FogColor,
            // Under cloud nothing drifts across the ground, and rain takes some colour out of things
            CloudShade = 0f,
            Post = Weathers.IsRain(weather) ? rig.Post with { Saturation = rig.Post.Saturation * 0.8f } : rig.Post
        };
    }

    /// <summary>
    /// The light in snow country (style guide, the areas' table: a low sun and cold grading), laid over the hour's
    /// rig: the sun lower, dimmer and cooler, more light bounced up off the snow, colder fog and grading, and bloom
    /// only on what is brighter than the snow, which would otherwise glow to a blank by day.
    /// </summary>
    public static LightRig Snowbound(LightRig rig)
    {
        var sun = rig.Light.SunDirection;
        var low = Vector3.Normalize(new Vector3(sun.X, sun.Y * 0.7f, sun.Z));
        return rig with
        {
            Light = rig.Light with
            {
                SunDirection = low,
                SunColor = rig.Light.SunColor * 0.74f * V(0.92f, 0.97f, 1.08f),
                SkyAmbient = rig.Light.SkyAmbient * V(0.94f, 0.98f, 1.06f),
                GroundAmbient = rig.Light.GroundAmbient * 1.15f
            },
            FogColor = Vector3.Lerp(rig.FogColor, V(0.8f, 0.86f, 0.96f), 0.5f),
            Post = rig.Post with
            {
                Saturation = rig.Post.Saturation * 0.9f,
                Contrast = rig.Post.Contrast * 1.04f,
                BloomThreshold = MathF.Max(rig.Post.BloomThreshold, 0.95f),
                BloomStrength = rig.Post.BloomStrength * 0.5f,
                HighlightTint = V(0.98f, 1.0f, 1.06f),
                ShadowTint = V(0.86f, 0.92f, 1.18f)
            }
        };
    }

    /// <summary>
    /// The light of a place with a climate of its own (plan 01 · M10, part b; style guide, "Climates"), laid over the
    /// rig it would have had.
    /// <para>A volcano in the open: ash hangs in the air, so the sun is dimmer and redder, the shade turns from violet
    /// to umber, an ashen haze closes in, and what burns of itself (lava) glows by day as lamps do by night. Inside
    /// it the rock is lit from below by its own heat: a red-orange bounce off the floor under a dull amber key.</para>
    /// <para>A temple of ice: a pale, blue-white key, blue shade, a cyan bounce off the ice, colour drained a little,
    /// and bloom only on what is brighter than the ice.</para>
    /// </summary>
    /// <summary>How far people's light is drawn toward grey inside a volcano (style guide, "Climates"): all the way, since the grade over the picture warms them by itself.</summary>
    public const float VolcanicPeopleWash = 1f;

    /// <summary>
    /// The light people stand in where a place's own is strongly coloured (plan 01 · M12): each of its three lights
    /// drawn <paramref name="wash"/> of the way to a grey as bright as itself, so the place stays as bright on them
    /// and a white coat stays paler than a face. The same light when the wash is nought.
    /// </summary>
    public static SceneLighting ForPeople(SceneLighting light, float wash)
    {
        if (wash <= 0f) return light;
        static Vector3 Toward(Vector3 colour, float t)
        {
            float grey = 0.299f * colour.X + 0.587f * colour.Y + 0.114f * colour.Z;
            return Vector3.Lerp(colour, new Vector3(grey), t);
        }
        return light with { SunColor = Toward(light.SunColor, wash), SkyAmbient = Toward(light.SkyAmbient, wash), GroundAmbient = Toward(light.GroundAmbient, wash) };
    }

    public static LightRig InClimate(LightRig rig, Overworld.AreaClimate climate, bool cave)
    {
        switch (climate)
        {
            case Overworld.AreaClimate.Volcanic when cave:
                return rig with
                {
                    Light = new SceneLighting(rig.Light.SunDirection, V(0.66f, 0.47f, 0.34f), V(0.47f, 0.36f, 0.4f), V(0.54f, 0.3f, 0.2f)),
                    Background = Rgb(30, 16, 18),
                    FogColor = V(0.14f, 0.07f, 0.06f),
                    LampGlow = VolcanicGlow,
                    GlowColor = LavaGlow,
                    PeopleWash = VolcanicPeopleWash,
                    Post = rig.Post with { Saturation = 1.02f, ShadowTint = V(1.08f, 0.86f, 0.9f), HighlightTint = V(1.1f, 0.98f, 0.84f) }
                };
            case Overworld.AreaClimate.Volcanic:
                return rig with
                {
                    Light = rig.Light with
                    {
                        SunColor = rig.Light.SunColor * 0.82f * V(1.06f, 0.93f, 0.82f),
                        SkyAmbient = rig.Light.SkyAmbient * V(1.04f, 0.95f, 0.9f),
                        GroundAmbient = rig.Light.GroundAmbient * V(1.2f, 1f, 0.86f)
                    },
                    FogColor = Vector3.Lerp(rig.FogColor, V(0.5f, 0.44f, 0.42f), 0.7f),
                    FogAmount = MathF.Min(0.9f, rig.FogAmount + 0.14f),
                    FogNear = MathF.Min(rig.FogNear, 40f),
                    FogFar = MathF.Min(rig.FogFar, 66f),
                    CloudShade = 0f,
                    // No brighter after dark, and in lava's own colour: at a lamp's full strength, lit by lamplight, it is pale
                    LampGlow = VolcanicGlow,
                    GlowColor = LavaGlow,
                    Post = rig.Post with
                    {
                        Saturation = rig.Post.Saturation * 0.92f,
                        ShadowTint = V(1.02f, 0.9f, 0.96f),
                        HighlightTint = V(1.08f, 0.99f, 0.88f)
                    }
                };
            case Overworld.AreaClimate.Frozen when cave:
                return rig with
                {
                    Light = new SceneLighting(rig.Light.SunDirection, V(0.56f, 0.6f, 0.68f), V(0.42f, 0.5f, 0.66f), V(0.34f, 0.42f, 0.5f)),
                    Background = Rgb(16, 22, 36),
                    FogColor = V(0.07f, 0.09f, 0.14f),
                    Post = rig.Post with
                    {
                        Saturation = rig.Post.Saturation * 0.9f,
                        BloomThreshold = MathF.Max(rig.Post.BloomThreshold, 0.95f),
                        BloomStrength = rig.Post.BloomStrength * 0.5f,
                        ShadowTint = V(0.82f, 0.9f, 1.2f),
                        HighlightTint = V(0.96f, 1f, 1.08f)
                    }
                };
            case Overworld.AreaClimate.Frozen:
                return Snowbound(rig);
            default:
                return rig;
        }
    }

    /// <summary>How brightly lava burns in a volcano's light, by day and by night: the glow of what is marked as lit in the art.</summary>
    public const float VolcanicGlow = 0.75f;

    /// <summary>What glows in a volcano's light turns into this, as lit glass elsewhere turns into <see cref="Lamplight"/>: lava's orange.</summary>
    public static readonly Vector3 LavaGlow = new(1f, 0.58f, 0.2f);

    public static LightRig BattleRig(float hour) => AtHour(hour, BattleRigFor);

    // ------------------------------------------------------------------ field (HD-2D)

    // Strong tilt-shift depth of field, warm highlights and violet shade
    private static readonly PostSettings FieldDayPost = new(6f, 0.95f, 0.3f, 0.86f, 0.38f, 1.1f, 1.08f,
        V(0.88f, 0.92f, 1.1f), V(1.07f, 1.0f, 0.9f), 0.26f, AoStrength: 0.4f);

    private static readonly SkyColors NoSky = new(Color.Black, Color.Black, Color.Black, Color.White, 0f);

    public static LightRig FieldRigFor(TimeOfDay time) => time switch
    {
        // Pale gold light through a morning haze
        TimeOfDay.Morning => new LightRig(
            new SceneLighting(Dir(-0.62f, 0.6f, 0.5f), V(0.64f, 0.54f, 0.46f), V(0.52f, 0.56f, 0.72f), V(0.46f, 0.45f, 0.44f)),
            Rgb(40, 96, 70), V(0.84f, 0.86f, 0.94f), 0.32f, 40f, 68f,
            FieldDayPost with { BloomStrength = 0.44f, Saturation = 1.04f, Contrast = 1.05f, ShadowTint = V(0.9f, 0.92f, 1.1f), HighlightTint = V(1.06f, 1.0f, 0.95f), Vignette = 0.24f },
            0f, 0.3f, NoSky, CloudShade: 0.14f),
        // Golden late-morning sun with violet-blue shade, like a lit diorama
        TimeOfDay.Day => new LightRig(
            new SceneLighting(Dir(-0.6f, 0.8f, 0.42f), V(0.66f, 0.55f, 0.4f), V(0.5f, 0.55f, 0.74f), V(0.46f, 0.44f, 0.4f)),
            Rgb(38, 100, 66), V(0.72f, 0.82f, 0.92f), 0.18f, 44f, 72f,
            FieldDayPost, 0f, 0.3f, NoSky, CloudShade: 0.2f),
        // Low orange sun, magenta-violet shade, lamps coming on
        TimeOfDay.Twilight => new LightRig(
            new SceneLighting(Dir(-0.72f, 0.42f, 0.5f), V(0.9f, 0.56f, 0.32f), V(0.5f, 0.44f, 0.62f), V(0.44f, 0.36f, 0.36f)),
            Rgb(40, 60, 62), V(0.9f, 0.62f, 0.52f), 0.28f, 42f, 70f,
            FieldDayPost with { BloomThreshold = 0.8f, BloomStrength = 0.55f, Saturation = 1.12f, ShadowTint = V(0.88f, 0.84f, 1.1f), HighlightTint = V(1.1f, 0.96f, 0.82f), Vignette = 0.3f },
            0.6f, 0.35f, NoSky, CloudShade: 0.1f, HomeGlow: 0.45f),
        // Cool moonlight; street lamps and warm windows glow and bloom
        TimeOfDay.Night => new LightRig(
            new SceneLighting(Dir(-0.38f, 0.86f, 0.34f), V(0.27f, 0.31f, 0.44f), V(0.22f, 0.26f, 0.4f), V(0.14f, 0.15f, 0.21f)),
            Rgb(12, 26, 34), V(0.1f, 0.14f, 0.28f), 0.35f, 40f, 68f,
            FieldDayPost with { BloomThreshold = 0.55f, BloomStrength = 0.7f, Saturation = 0.8f, Contrast = 1.06f, ShadowTint = V(0.85f, 0.9f, 1.18f), HighlightTint = V(1.0f, 1.0f, 1.05f), Vignette = 0.4f },
            1f, 0.5f, NoSky, HomeGlow: 1f),
        // Deeper night: the lamps burn on, most homes have gone dark
        _ => new LightRig(
            new SceneLighting(Dir(-0.34f, 0.88f, 0.3f), V(0.2f, 0.24f, 0.36f), V(0.17f, 0.2f, 0.33f), V(0.11f, 0.12f, 0.17f)),
            Rgb(8, 18, 26), V(0.07f, 0.1f, 0.22f), 0.4f, 38f, 66f,
            FieldDayPost with { BloomThreshold = 0.55f, BloomStrength = 0.6f, Saturation = 0.72f, Contrast = 1.06f, ShadowTint = V(0.85f, 0.9f, 1.2f), HighlightTint = V(1.0f, 1.0f, 1.06f), Vignette = 0.45f },
            1f, 0.5f, NoSky)
    };

    private static readonly PostSettings RoomPost =
        FieldDayPost with { TiltShift = 4f, Dof = 0.6f, FocusBand = 0.36f, BloomThreshold = 0.93f, BloomStrength = 0.22f };

    /// <summary>
    /// Rooms get a gentler blur and less glow than the field, and follow the clock through their windows: by day
    /// the sun throws patches of light on the floor (<see cref="LightRig.LampGlow"/>), at twilight the glass turns
    /// orange, and at night it is dark blue and the room is lit, warmer and a little dimmer, by its lamps.
    /// </summary>
    public static LightRig IndoorRigFor(TimeOfDay time) => time switch
    {
        TimeOfDay.Morning => new LightRig(
            new SceneLighting(Dir(-0.4f, 0.78f, 0.48f), V(0.44f, 0.42f, 0.38f), V(0.66f, 0.65f, 0.64f), V(0.5f, 0.47f, 0.44f)),
            Color.Black, V(0f, 0f, 0f), 0f, 100f, 200f, RoomPost, 0.8f, 0.3f, NoSky)
            { GlowColor = V(0.86f, 0.9f, 0.96f), LightTint = V(0.92f, 0.96f, 1f) },
        TimeOfDay.Day => new LightRig(
            new SceneLighting(Dir(-0.35f, 0.82f, 0.46f), V(0.46f, 0.42f, 0.36f), V(0.66f, 0.63f, 0.6f), V(0.52f, 0.47f, 0.42f)),
            Color.Black, V(0f, 0f, 0f), 0f, 100f, 200f, RoomPost, 1f, 0.3f, NoSky)
            { GlowColor = V(0.86f, 0.9f, 0.96f), LightTint = V(1f, 0.96f, 0.84f) },
        TimeOfDay.Twilight => new LightRig(
            new SceneLighting(Dir(-0.5f, 0.7f, 0.5f), V(0.5f, 0.41f, 0.31f), V(0.63f, 0.58f, 0.56f), V(0.5f, 0.44f, 0.4f)),
            Color.Black, V(0f, 0f, 0f), 0f, 100f, 200f,
            RoomPost with { Saturation = 1.08f, HighlightTint = V(1.08f, 0.99f, 0.9f) }, 0.75f, 0.3f, NoSky, HomeGlow: 0.85f)
            { GlowColor = V(1f, 0.64f, 0.42f), LightTint = V(1f, 0.6f, 0.34f) },
        _ => new LightRig(
            new SceneLighting(Dir(-0.3f, 0.86f, 0.42f), V(0.5f, 0.41f, 0.28f), V(0.5f, 0.45f, 0.44f), V(0.4f, 0.34f, 0.3f)),
            Color.Black, V(0f, 0f, 0f), 0f, 100f, 200f,
            RoomPost with { BloomThreshold = 0.86f, BloomStrength = 0.3f, Saturation = 1.06f, ShadowTint = V(0.86f, 0.88f, 1.12f), HighlightTint = V(1.1f, 1f, 0.86f), Vignette = 0.4f },
            0f, 0.3f, NoSky, HomeGlow: 1f)
            { GlowColor = V(0.07f, 0.1f, 0.22f) }
    };

    // ------------------------------------------------------------------ battles (3D)

    private static readonly PostSettings BattleDayPost = new(2f, 0f, 0.5f, 0.86f, 0.28f, 1.05f, 1.05f,
        V(0.94f, 0.97f, 1.06f), V(1.04f, 1.01f, 0.95f), 0.1f, AoStrength: 0.6f, OutlineStrength: 0.35f);

    private static LightRig Battle(SceneLighting light, SkyColors sky, float fog, float rim, PostSettings post) =>
        new(light, sky.Horizon, Of(sky.Horizon), fog, 40f, 140f, post, 0f, rim, sky, sky.Stars > 0f ? 0f : 0.16f);

    // ------------------------------------------------------------------ arenas (plan 04 · G8)

    /// <summary>
    /// The light of a battle's arena: grass and water stages take the time of day's battle rig; forest, snow and
    /// sand grade it their own way; caves and halls have fixed rigs that ignore the clock, and rooms follow it
    /// through their windows.
    /// </summary>
    public static LightRig ArenaRig(ArenaSpec spec, float hour) => spec.Kind switch
    {
        Overworld.BattleArena.Forest => ForestGrade(BattleRig(hour)),
        Overworld.BattleArena.Snow => SnowGrade(BattleRig(hour)),
        Overworld.BattleArena.Sand => SandGrade(BattleRig(hour)),
        Overworld.BattleArena.Cave => CaveRig,
        Overworld.BattleArena.Indoors => AtHour(hour, RoomBattleRigFor),
        Overworld.BattleArena.Gym or Overworld.BattleArena.League => HallRig(spec.Theme, spec.Kind == Overworld.BattleArena.League),
        _ => BattleRig(hour)
    };

    /// <summary>
    /// The arena's light under a battle's weather (plan 25 · Y1; style guide, "Weather in battle"), eased in by
    /// <paramref name="strength"/> (0..1): rain, hail, a sandstorm and fog take the light of the field's weather they
    /// come from (<see cref="Weathers.FromBattle"/>, <see cref="Weathered"/>), with the sky drawn toward their own
    /// colour at its own brightness and the stars hidden; the harsh sun has a rig of its own (<see cref="HarshSun"/>).
    /// </summary>
    public static LightRig InBattleWeather(LightRig rig, BattleWeather weather, float strength = 1f)
    {
        strength = Math.Clamp(strength, 0f, 1f);
        if (weather == BattleWeather.None || strength <= 0f) return rig;
        LightRig under;
        if (weather is BattleWeather.Sun or BattleWeather.HarshSun) under = HarshSun(rig);
        else
        {
            under = Weathered(rig, Weathers.FromBattle(weather));
            var (tint, cover) = weather switch
            {
                BattleWeather.Sandstorm => (V(1.12f, 0.98f, 0.76f), 0.75f),
                BattleWeather.Fog => (V(0.98f, 1f, 1.02f), 0.85f),
                BattleWeather.Hail or BattleWeather.Snow => (V(0.96f, 1f, 1.06f), 0.6f),
                // The heavy rain hides more of the sky than rain; the strong winds only thin it (plan 06 · R19b)
                BattleWeather.HeavyRain => (V(0.92f, 0.96f, 1.04f), 0.85f),
                BattleWeather.StrongWinds => (V(0.98f, 1.02f, 1.04f), 0.35f),
                _ => (V(0.94f, 0.98f, 1.04f), 0.7f)
            };
            var sky = new SkyColors(Clouded(rig.Sky.Zenith, tint, cover), Clouded(rig.Sky.Middle, tint, cover), Clouded(rig.Sky.Horizon, tint, cover),
                Clouded(rig.Sky.CloudTint, tint, cover * 0.8f), rig.Sky.Stars * (1f - cover));
            under = under with { Sky = sky, Background = sky.Horizon };
        }
        return strength >= 1f ? under : LightRig.Lerp(rig, under, strength);
    }

    /// <summary>A colour drawn <paramref name="cover"/> of the way to a grey of its own brightness, tinted.</summary>
    private static Color Clouded(Color c, Vector3 tint, float cover)
    {
        float grey = (0.299f * c.R + 0.587f * c.G + 0.114f * c.B) / 255f;
        var mixed = Vector3.Lerp(Of(c), tint * grey, cover);
        return new Color((int)Math.Clamp(mixed.X * 255f, 0f, 255f), (int)Math.Clamp(mixed.Y * 255f, 0f, 255f), (int)Math.Clamp(mixed.Z * 255f, 0f, 255f), 255);
    }

    /// <summary>
    /// The harsh sun in battle (plan 25 · Y1), which the field has no weather for: a stronger, warmer key, warm light
    /// from the sky, more bloom on a lower threshold, warm highlights and a paler, warmer sky.
    /// </summary>
    public static LightRig HarshSun(LightRig r)
    {
        var sky = new SkyColors(PixelCanvas.Mix(r.Sky.Zenith, Rgb(150, 190, 236), 0.35f), PixelCanvas.Mix(r.Sky.Middle, Rgb(236, 226, 196), 0.45f),
            PixelCanvas.Mix(r.Sky.Horizon, Rgb(255, 236, 196), 0.55f), PixelCanvas.Mix(r.Sky.CloudTint, Rgb(255, 246, 220), 0.4f), r.Sky.Stars * 0.5f);
        return r with
        {
            Light = r.Light with
            {
                SunColor = r.Light.SunColor * V(1.34f, 1.18f, 0.9f),
                SkyAmbient = r.Light.SkyAmbient * V(1.06f, 1.0f, 0.9f),
                GroundAmbient = r.Light.GroundAmbient * V(1.12f, 1.04f, 0.9f)
            },
            Sky = sky,
            Background = sky.Horizon,
            FogColor = Vector3.Lerp(r.FogColor, V(1f, 0.9f, 0.74f), 0.4f),
            FogAmount = r.FogAmount + 0.06f,
            CloudShade = r.CloudShade * 0.5f,
            Post = r.Post with
            {
                BloomThreshold = r.Post.BloomThreshold - 0.08f,
                BloomStrength = r.Post.BloomStrength + 0.18f,
                Saturation = r.Post.Saturation * 1.05f,
                HighlightTint = r.Post.HighlightTint * V(1.05f, 1.01f, 0.92f),
                ShadowTint = r.Post.ShadowTint * V(1.02f, 0.98f, 0.96f)
            }
        };
    }

    /// <summary>
    /// The stage's light under Gravity (plan 25 · Y2; style guide, "What lies on the field"), eased in by
    /// <paramref name="strength"/> (0..1): the key and both ambients lowered to 84 %, the shadows drawn a little
    /// toward violet and the vignette 0.08 stronger. The look is our own: Platinum draws nothing while Gravity lasts.
    /// </summary>
    public static LightRig UnderGravity(LightRig r, float strength)
    {
        strength = Math.Clamp(strength, 0f, 1f);
        if (strength <= 0f) return r;
        var under = r with
        {
            Light = r.Light with
            {
                SunColor = r.Light.SunColor * GravityDim,
                SkyAmbient = r.Light.SkyAmbient * GravityDim,
                GroundAmbient = r.Light.GroundAmbient * GravityDim
            },
            Post = r.Post with
            {
                ShadowTint = r.Post.ShadowTint * V(0.97f, 0.94f, 1.05f),
                Vignette = r.Post.Vignette + 0.08f
            }
        };
        return strength >= 1f ? under : LightRig.Lerp(r, under, strength);
    }

    /// <summary>How far Gravity lowers the stage's key and ambient light.</summary>
    public const float GravityDim = 0.84f;

    /// <summary>
    /// Under the trees: less sun, green shade and haze, a stronger vignette. After dark only the green haze is
    /// added, so the Pokémon still read clearly (night battles stay brighter than the night field).
    /// </summary>
    public static LightRig ForestGrade(LightRig r)
    {
        bool night = r.Sky.Stars > 0f;
        return r with
        {
            Light = night ? r.Light : r.Light with
            {
                SunColor = r.Light.SunColor * 0.8f,
                SkyAmbient = r.Light.SkyAmbient * V(0.84f, 0.96f, 0.82f),
                GroundAmbient = r.Light.GroundAmbient * V(0.86f, 1.0f, 0.84f)
            },
            FogColor = Vector3.Lerp(r.FogColor, night ? V(0.12f, 0.2f, 0.16f) : V(0.4f, 0.56f, 0.44f), 0.55f),
            FogAmount = r.FogAmount + (night ? 0.05f : 0.15f),
            FogNear = 22f,
            FogFar = 90f,
            CloudShade = r.CloudShade + 0.12f,
            Post = r.Post with
            {
                Vignette = r.Post.Vignette + (night ? 0.06f : 0.16f),
                Saturation = r.Post.Saturation * 0.97f,
                ShadowTint = r.Post.ShadowTint * V(0.95f, 1.02f, 0.96f)
            }
        };
    }

    private static readonly SkyColors Overcast = new(Rgb(150, 176, 214), Rgb(196, 212, 234), Rgb(232, 238, 246), Rgb(244, 246, 250), 0f);

    /// <summary>On snow: a paler, cooler sky, light bounced up off the ground, softer contrast.</summary>
    public static LightRig SnowGrade(LightRig r)
    {
        bool night = r.Sky.Stars > 0f;
        var sky = SkyColors.Lerp(r.Sky, Overcast, night ? 0.15f : 0.6f);
        return r with
        {
            Light = r.Light with
            {
                SunColor = r.Light.SunColor * V(0.86f, 0.92f, 1.0f),
                SkyAmbient = r.Light.SkyAmbient * V(1.0f, 1.05f, 1.12f),
                GroundAmbient = r.Light.GroundAmbient * V(1.25f, 1.3f, 1.42f)
            },
            Sky = sky,
            Background = sky.Horizon,
            FogColor = night ? r.FogColor : Vector3.Lerp(r.FogColor, V(0.88f, 0.91f, 0.96f), 0.7f),
            FogAmount = r.FogAmount + 0.15f,
            Post = r.Post with
            {
                Saturation = r.Post.Saturation * 0.88f,
                Contrast = 1.0f,
                BloomThreshold = Math.Max(r.Post.BloomThreshold, 0.92f),
                ShadowTint = r.Post.ShadowTint * V(0.97f, 0.99f, 1.05f)
            }
        };
    }

    private static readonly SkyColors Haze = new(Rgb(120, 164, 214), Rgb(196, 206, 214), Rgb(240, 226, 196), Rgb(252, 244, 228), 0f);

    /// <summary>In the desert: a hot, hazy light, warm fog and a little more glow.</summary>
    public static LightRig SandGrade(LightRig r)
    {
        bool night = r.Sky.Stars > 0f;
        var sky = SkyColors.Lerp(r.Sky, Haze, night ? 0.15f : 0.5f);
        return r with
        {
            Light = r.Light with
            {
                SunColor = r.Light.SunColor * V(1.12f, 1.04f, 0.92f),
                GroundAmbient = r.Light.GroundAmbient * V(1.15f, 1.08f, 0.95f)
            },
            Sky = sky,
            Background = sky.Horizon,
            FogColor = night ? r.FogColor : Vector3.Lerp(r.FogColor, V(0.94f, 0.86f, 0.72f), 0.6f),
            FogAmount = r.FogAmount + 0.12f,
            Post = r.Post with
            {
                BloomStrength = r.Post.BloomStrength + 0.08f,
                HighlightTint = r.Post.HighlightTint * V(1.03f, 1.0f, 0.95f),
                Saturation = r.Post.Saturation * 1.03f
            }
        };
    }

    /// <summary>A cave ignores the clock: a warm, torch-like key from the upper left, cool blue shade, dark haze.</summary>
    public static readonly LightRig CaveRig = new(
        new SceneLighting(Dir(-0.5f, 0.75f, 0.45f), V(0.58f, 0.45f, 0.31f), V(0.26f, 0.3f, 0.4f), V(0.23f, 0.2f, 0.18f)),
        Rgb(20, 16, 18), V(0.1f, 0.09f, 0.11f), 0.55f, 18f, 75f,
        BattleDayPost with
        {
            BloomThreshold = 0.62f, BloomStrength = 0.45f, Saturation = 0.95f, Contrast = 1.08f,
            ShadowTint = V(0.84f, 0.9f, 1.14f), HighlightTint = V(1.08f, 1.0f, 0.88f), Vignette = 0.42f
        },
        1f, 0.5f, new SkyColors(Rgb(16, 13, 16), Rgb(24, 20, 22), Rgb(36, 30, 32), Rgb(40, 36, 40), 0f));

    /// <summary>A room in battle: the indoor light of the time of day, graded like the battles.</summary>
    public static LightRig RoomBattleRigFor(TimeOfDay time)
    {
        var room = IndoorRigFor(time);
        var walls = new SkyColors(Rgb(190, 176, 156), Rgb(214, 202, 182), Rgb(232, 222, 202), Color.White, 0f);
        return new LightRig(room.Light, walls.Horizon, V(0.82f, 0.76f, 0.68f), 0.12f, 30f, 120f,
            BattleDayPost with { HighlightTint = V(1.05f, 1.01f, 0.94f), Vignette = 0.18f },
            0.5f + 0.5f * room.LampGlow, 0.35f, walls) { GlowColor = room.GlowColor };
    }

    /// <summary>A gym hall or a League room: a fixed key light, ambient tinted by the type, its lamps lit.</summary>
    public static LightRig HallRig(Data.PokemonType? theme, bool league)
    {
        var (_, _, wall, _, accent, _) = BattleArenas.HallColors(theme, league);
        var tint = Of(accent);
        float dim = league ? 0.78f : 1f;
        var light = new SceneLighting(Dir(-0.45f, 0.85f, 0.42f), V(0.5f, 0.47f, 0.42f) * (league ? 1.05f : 1f),
            Vector3.Lerp(V(0.52f, 0.54f, 0.62f), tint, 0.16f) * dim, Vector3.Lerp(V(0.46f, 0.44f, 0.42f), tint, 0.1f) * dim);
        var w = Of(wall);
        var sky = new SkyColors(Fade(wall, 0.45f), Fade(wall, 0.6f), Fade(wall, 0.8f), Color.White, 0f);
        return new LightRig(light, sky.Horizon, w * 0.6f, league ? 0.35f : 0.22f, 30f, 110f,
            BattleDayPost with
            {
                BloomThreshold = league ? 0.7f : 0.8f, BloomStrength = league ? 0.45f : 0.32f,
                Vignette = league ? 0.38f : 0.2f, Saturation = 1.05f
            },
            1f, league ? 0.55f : 0.4f, sky);
    }

    private static Color Fade(Color c, float f) => new((int)(c.R * f), (int)(c.G * f), (int)(c.B * f), 255);

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
            new SceneLighting(Dir(-0.4f, 0.85f, 0.4f), V(0.27f, 0.3f, 0.38f), V(0.29f, 0.32f, 0.4f), V(0.18f, 0.19f, 0.23f)),
            new SkyColors(Rgb(8, 14, 38), Rgb(18, 32, 72), Rgb(40, 58, 100), Rgb(70, 84, 120), 1f), 0.4f, 0.55f,
            BattleDayPost with { BloomThreshold = 0.75f, BloomStrength = 0.35f, Saturation = 0.84f, ShadowTint = V(0.88f, 0.92f, 1.12f), HighlightTint = V(0.98f, 1.0f, 1.06f), Vignette = 0.25f }),
        _ => Battle(
            new SceneLighting(Dir(-0.36f, 0.88f, 0.36f), V(0.22f, 0.25f, 0.33f), V(0.24f, 0.27f, 0.35f), V(0.15f, 0.16f, 0.2f)),
            new SkyColors(Rgb(4, 8, 26), Rgb(12, 22, 54), Rgb(28, 42, 80), Rgb(50, 60, 90), 1f), 0.45f, 0.55f,
            BattleDayPost with { BloomThreshold = 0.75f, BloomStrength = 0.3f, Saturation = 0.78f, ShadowTint = V(0.88f, 0.92f, 1.14f), HighlightTint = V(0.98f, 1.0f, 1.06f), Vignette = 0.3f })
    };
}
