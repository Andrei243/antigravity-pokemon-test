using System;
using System.Numerics;

namespace PokemonPlatinumEngine.UI;

/// <summary>
/// What a way into a battle does to the field's camera at a moment (plan 09 · L14): the lens as a factor of the
/// place's own (the camera kit's zoom: 2 shows everything twice as large), how far the point the camera looks at is
/// stepped from where it was (in tiles: east, south), and the widest the lens comes to in the whole closing, which
/// the chunks of a streamed map are made ready for. A lens of 1 and no step is the camera as it was.
/// </summary>
public readonly record struct TransitionLens(float Zoom, Vector2 Step, float Widest)
{
    /// <summary>The place's own lens, looking where it looked.</summary>
    public static readonly TransitionLens None = new(1f, Vector2.Zero, 1f);
}

// The legendary's and the mythical's ways in (plan 09 · L14; style guide, "Life", "Into battle", "The legendary's
// and the mythical's"): the field's camera moving through the camera kit's rig, after EncounterEffect_Legendary and
// EncounterEffect_Mythical of src/overlay005/encounter_effect_core.c (2253-2549), thirty frames to the second, and
// then a white that closes as a shape. Everything here is a function of the kind and the time, as the other ways are;
// GameEngine hands the rig what LensAt says each frame of the closing and lets it go at the midpoint.
public static partial class SceneTransition
{
    /// <summary>The original's frames, thirty to the second.</summary>
    private const float Frame = 1f / 30f;

    /// <summary>
    /// How long both ways move the camera after their flashes (fifty-seven frames, the legendary's widening, wait
    /// and rush and the mythical's sixteen lenses alike), and how long each one's white then takes to close: sixty
    /// frames of the original's brightness fade for the legendary, ten for the mythical.
    /// </summary>
    public const float CameraSeconds = 57f * Frame, LegendaryWhiteSeconds = 60f * Frame, MythicalWhiteSeconds = 10f * Frame;

    /// <summary>
    /// The legendary's beats, in seconds after the flashes: the lens widens over forty frames
    /// (<c>LinearInterpolationTaskS32</c> from frame 1 to 41), waits six, and rushes in over eight (frame 47 to 56).
    /// </summary>
    public const float WidenUntil = 41f * Frame, RushFrom = 47f * Frame, RushUntil = 56f * Frame;

    /// <summary>
    /// How far a turn of the original's camera about the point it looks at steps that point here, in tiles for a
    /// whole sine: a turn swings the picture round its middle, and a step as far as it would swing a point four
    /// tiles off is the jolt without turning the field, whose upright cards are straightened for one pitch.
    /// </summary>
    public const float JoltReach = 4f;

    /// <summary>The original's field camera's field of view, each side of the middle, in its units (65536 to the turn): 8.09°.</summary>
    private const int FieldFov = 0x5C1;

    /// <summary>The original's camera pitch about the point it looks at, in its units: −59.06°.</summary>
    private const int FieldPitch = 0xD602;

    /// <summary>A field of view of the original's, in its units, as a factor of its field camera's: what makes everything as large as it made it.</summary>
    private static float LensOf(int fov) => (float)(Math.Tan(FieldFov * Math.PI / 32768.0) / Math.Tan(fov * Math.PI / 32768.0));

    /// <summary>The lens the legendary's widens to: the original's field of view a 256th of a turn wider (<c>fov + 0x100</c>), 0.85.</summary>
    public static readonly float LegendaryWide = LensOf(FieldFov + 0x100);

    /// <summary>The lens the legendary's rushes in to: the tightest the camera kit's goes.</summary>
    public const float LegendaryRush = Overworld.CameraRig.TightestZoom;

    /// <summary>
    /// The original's sixteen held lenses (<c>sLegendaryEncounterCameraParams</c>): the pitch and the turn about the
    /// upright the camera looks from, its field of view, and how many frames it waits before it is taken
    /// (<c>frameDelay</c>, counted down to below nought: one more frame than it says).
    /// </summary>
    private static readonly (int Pitch, int Turn, int Fov, int Wait)[] HeldLenses =
    {
        (0xD602, 0x0000, 0x5C1, 4), (0xCF02, 0xFF00, 0x601, 4), (0xE602, 0x1000, 0x691, 4), (0xD602, 0x0A00, 0x711, 3),
        (0xE102, 0xF000, 0x780, 3), (0xC602, 0x0000, 0x751, 3), (0xE002, 0xF000, 0x800, 3), (0xD602, 0x0000, 0x802, 3),
        (0xD002, 0x1000, 0x800, 3), (0xD902, 0xF500, 0x751, 3), (0xD002, 0x0A00, 0x4C1, 2), (0xE002, 0xF000, 0x3C1, 2),
        (0xD002, 0xF000, 0x650, 1), (0xE002, 0xA000, 0x241, 1), (0xE1A2, 0x0500, 0x500, 1), (0xD602, 0x0000, 0x241, 1)
    };

    /// <summary>
    /// The mythical's sixteen lenses as this game takes them: from when (seconds after the flashes), the lens, and the
    /// step of the point looked at. The first is the place's own; the last is taken as the camera's frames end and
    /// held through the white.
    /// </summary>
    public static readonly (float From, float Zoom, Vector2 Step)[] MythicalLenses = BuildMythical();

    /// <summary>The widest the mythical's lens comes to (the eighth, 0.714).</summary>
    public static readonly float MythicalWidest = Min(MythicalLenses);

    private static (float, float, Vector2)[] BuildMythical()
    {
        var lenses = new (float, float, Vector2)[HeldLenses.Length];
        int frame = 0;
        for (int i = 0; i < HeldLenses.Length; i++)
        {
            var (pitch, turn, fov, wait) = HeldLenses[i];
            frame += wait + 1;
            double flatter = (short)(ushort)(pitch - FieldPitch) * Math.PI / 32768.0, turned = (short)(ushort)turn * Math.PI / 32768.0;
            var step = new Vector2(Texel(JoltReach * (float)Math.Sin(turned)), Texel(-JoltReach * (float)Math.Sin(flatter)));
            lenses[i] = (frame * Frame, LensOf(fov), step);
        }
        return lenses;
    }

    private static float Min((float From, float Zoom, Vector2 Step)[] lenses)
    {
        float least = 1f;
        foreach (var lens in lenses) least = MathF.Min(least, lens.Zoom);
        return least;
    }

    /// <summary>A length in tiles to whole texels of the field (32 to the tile).</summary>
    private static float Texel(float tiles) => MathF.Round(tiles * 32f) / 32f;

    /// <summary>True for the two ways in that move the field's camera.</summary>
    public static bool MovesTheCamera(TransitionKind kind) => kind is TransitionKind.Legendary or TransitionKind.Mythical;

    /// <summary>
    /// The share of the way the legendary's rush has come, 0 to 1, <paramref name="u"/> of its eight frames in: the
    /// original's <c>QuadraticInterpolationTaskFX32</c> from its distance to 2350 units nearer at an opening rate of
    /// half a unit a frame away, which comes to (2354u² − 4u) / 2350: a hair back, then headlong.
    /// </summary>
    public static float RushShare(float u)
    {
        u = Math.Clamp(u, 0f, 1f);
        return (2354f * u * u - 4f * u) / 2350f;
    }

    /// <summary>
    /// What the way does to the field's camera <paramref name="seconds"/> into its closing: the place's own lens
    /// through the flashes and for every way that leaves the camera be, and after them, for the legendary, the lens
    /// widened evenly to <see cref="LegendaryWide"/>, held, and rushed in to <see cref="LegendaryRush"/> on its ratio
    /// along <see cref="RushShare"/>; for the mythical, the last of <see cref="MythicalLenses"/> taken by now. Both
    /// hold their last lens through the white, to the midpoint.
    /// </summary>
    public static TransitionLens LensAt(TransitionKind kind, float seconds)
    {
        if (!MovesTheCamera(kind) || seconds < FlashSeconds) return TransitionLens.None;
        float since = seconds - FlashSeconds;
        if (kind == TransitionKind.Legendary)
        {
            float zoom = since < WidenUntil ? 1f + (LegendaryWide - 1f) * since / WidenUntil
                : since < RushFrom ? LegendaryWide
                : LegendaryWide * MathF.Pow(LegendaryRush / LegendaryWide, RushShare((since - RushFrom) / (RushUntil - RushFrom)));
            return new TransitionLens(zoom, Vector2.Zero, LegendaryWide);
        }
        var held = TransitionLens.None with { Widest = MythicalWidest };
        foreach (var (from, zoom, step) in MythicalLenses)
        {
            // (A hair's grace, so the frame the original takes a lens on finds it taken)
            if (since + 1e-4f < from) break;
            held = new TransitionLens(zoom, step, MythicalWidest);
        }
        return held;
    }

    /// <summary>
    /// How white the wash over the whole picture is, 0 to 255, <paramref name="seconds"/> into closing: the
    /// legendary's long white comes over all of it as its disc grows, to 200 as the disc closes; nothing for any
    /// other way, and nothing as the battle opens.
    /// </summary>
    public static int Wash(TransitionKind kind, bool closing, float seconds)
    {
        if (!closing || kind != TransitionKind.Legendary) return 0;
        return (int)MathF.Round(200f * Closed(kind, true, seconds));
    }
}
