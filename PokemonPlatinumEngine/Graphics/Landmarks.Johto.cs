using System;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// Johto's landmarks of its own (plan 20 · J8; style guide, "Water"): the whirlpools on the sea of Route 41.
/// No GPU calls.
/// </summary>
internal static partial class Landmarks
{
    private static readonly Color WhirlDeep = Rgb(58, 118, 190), WhirlArm = Rgb(150, 206, 246), WhirlLine = Rgb(36, 84, 160);
    private static readonly Color WhirlEye = Rgb(28, 62, 128), WhirlFoam = Rgb(236, 246, 255);

    /// <summary>How far a whirlpool's sheet lies over the water, in texels.</summary>
    public const int WhirlpoolLift = 2;

    /// <summary>The disc of a whirlpool, in texels across (three tiles less a little, so the open water shows round it).</summary>
    public const int WhirlpoolSize = 92;

    /// <summary>
    /// A whirlpool, centred on the model's box (HeartGold's model is wider than the block of three tiles it turns on,
    /// and stands centred on it): a sheet two texels over the water, turning in four frames.
    /// </summary>
    private static void Whirlpool(KitBuilder kit, int w, int d)
    {
        var top = kit.Frames("whirlpool", WhirlpoolSize, WhirlpoolSize, MovingFrames, 6f, PaintWhirlpool);
        float x0 = MathF.Round(w / 2f - WhirlpoolSize / 2f), z0 = MathF.Round(d / 2f - WhirlpoolSize / 2f);
        kit.Box(x0, x0 + WhirlpoolSize, z0, z0 + WhirlpoolSize, 0, WhirlpoolLift, top: top);
    }

    /// <summary>
    /// A whirlpool seen from above, <paramref name="frame"/> of four (style guide, "Water"): a disc of deep water with
    /// three arms of a spiral, light on their outer side and dark on their inner, wound from the rim into a dark eye
    /// ringed with foam, and broken foam on the rim. Each frame turns the arms a twelfth of a turn; the rim stays.
    /// </summary>
    public static void PaintWhirlpool(PixelCanvas c, int frame = 0)
    {
        int size = c.Width;
        float r = size / 2f, cx = r - 0.5f, cy = r - 0.5f;
        const float Eye = 7f;
        float turn = frame * MathF.PI * 2f / 12f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x - cx, dy = y - cy;
                float dist = MathF.Sqrt(dx * dx + dy * dy);
                if (dist > r - 0.5f) continue;
                if (dist < Eye) { c.SetRaw(x, y, WhirlEye); continue; }
                if (dist < Eye + 1f) { c.SetRaw(x, y, WhirlFoam); continue; }
                // The rim's foam: dashes by the angle, a few texels in from the edge, never turning
                float angle = MathF.Atan2(dy, dx);
                if (dist > r - 3f)
                {
                    int dash = (int)((angle + MathF.PI) / (MathF.PI * 2f) * 40f);
                    c.SetRaw(x, y, dash % 3 == 0 ? WhirlFoam : WhirlDeep);
                    continue;
                }
                // The spiral: three arms, the angle wound by the distance in toward the eye
                float wound = angle + turn + (r - dist) * 0.16f;
                float phase = (wound * 3f / (MathF.PI * 2f)) % 1f;
                if (phase < 0f) phase += 1f;
                // The arms lie about 13 texels apart across the spiral (a third of a turn over the winding of 0.16 a
                // texel): each is three texels across it, with a dark line of one texel on its inner side
                if (phase < 3f / 13f) c.SetRaw(x, y, WhirlArm);
                else if (phase < 4f / 13f) c.SetRaw(x, y, WhirlLine);
                else c.SetRaw(x, y, WhirlDeep);
            }
    }
}
