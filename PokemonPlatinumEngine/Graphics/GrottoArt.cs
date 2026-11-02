using System;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// The mouth of a Hidden Grotto (plan 22 · U11; style guide, "A grotto's hollow"): a card a tile across, a low mound of
/// leaves round a dark opening arched by two roots, outlined like every card. Painted in code, no GPU calls.
/// </summary>
internal static class GrottoArt
{
    private static Color Rgb(int r, int g, int b) => new(r, g, b, 255);

    private static readonly Color Leaf = Rgb(70, 128, 62), LeafLit = Rgb(104, 160, 84), LeafShade = Rgb(46, 92, 48);
    private static readonly Color Dark = Rgb(28, 30, 26), Deep = Rgb(14, 16, 14);
    private static readonly Color Bark = Rgb(104, 76, 50), BarkLit = Rgb(138, 104, 70);

    /// <summary>The hollow, 32 by 30: a mound from row 6 to the foot, its opening 14 wide and 13 deep at the foot's middle.</summary>
    public static void PaintHollow(PixelCanvas c)
    {
        const int cx = 16, foot = 29;
        // The mound: half an ellipse 15 wide each way and 23 high, lit on its upper left, shaded on its right and foot
        for (int y = 6; y <= foot; y++)
            for (int x = 1; x <= 30; x++)
            {
                float dx = (x + 0.5f - cx) / 15f, dy = (foot + 0.5f - (y + 0.5f)) / 23f;
                if (dx * dx + dy * dy > 1f) continue;
                bool lit = dx < -0.15f && dy > 0.45f || (x + y * 3) % 11 == 0 && dy > 0.3f;
                bool shade = dx > 0.55f || dy < 0.18f || (x * 7 + y) % 13 == 0;
                c.Set(x, y, lit ? LeafLit : shade ? LeafShade : Leaf);
            }
        // Tufts standing up from the rim
        foreach (int x in new[] { 6, 11, 17, 22, 26 })
        {
            int top = 6 + (int)(23 * (1 - Math.Sqrt(Math.Max(0, 1 - Math.Pow((x + 0.5f - cx) / 15f, 2))))) - 2;
            c.Set(x, top, LeafLit);
            c.Set(x, top + 1, Leaf);
            c.Set(x + 1, top + 1, LeafShade);
        }
        // The opening: an arch 14 wide and 13 deep, darkest at its middle
        for (int y = foot - 12; y <= foot; y++)
            for (int x = cx - 7; x < cx + 7; x++)
            {
                float dx = (x + 0.5f - cx) / 7f, dy = (foot + 0.5f - (y + 0.5f)) / 13f;
                if (dx * dx + dy * dy > 1f) continue;
                c.Set(x, y, dx * dx + dy * dy < 0.35f ? Deep : Dark);
            }
        // Two roots arching over it, from either side of the foot
        for (int y = foot - 13; y <= foot; y++)
        {
            float t = (foot - y) / 13f;
            int off = (int)Math.Round(8 * Math.Sqrt(Math.Max(0, 1 - t * t)));
            c.Set(cx - off - 1, y, y < foot - 9 ? BarkLit : Bark);
            c.Set(cx + off, y, Bark);
        }
        Pix.Outline(c);
    }
}
