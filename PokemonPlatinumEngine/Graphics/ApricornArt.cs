using System;
using Raylib_cs;
using PokemonPlatinumEngine.Models;
using static PokemonPlatinumEngine.Graphics.Pix;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// The card an Apricorn tree is drawn with (plan 20 · J6; style guide, "Apricorn trees"): a short round tree a tile
/// wide with a thick trunk and a canopy of broad leaves, and, while it has its fruit, a cluster of three Apricorns
/// (round, with a little stalk) in the colour it bears. Bare, it is the same tree with leaves only. Our own art; no
/// drawing calls, so tests paint it.
/// </summary>
public static class ApricornArt
{
    public const int Width = 32, Height = 48;

    /// <summary>The colour an Apricorn is drawn in: <c>Data/apricorns.json</c>'s.</summary>
    public static Color ColourOf(Apricorn colour)
    {
        var rgb = ApricornData.File.Colours.Find(c => c.Name == colour.ToString())?.Rgb ?? new[] { 200, 60, 60 };
        return Rgb(rgb[0], rgb[1], rgb[2]);
    }

    /// <summary>The key a look is kept under: one card for each colour with its fruit on, and one bare.</summary>
    public static string KeyOf(Apricorn? colour, bool inFruit) => inFruit && colour != null ? colour.ToString()! : "bare";

    public static PixelCanvas Paint(Apricorn? colour, bool inFruit)
    {
        var c = new PixelCanvas(Width, Height);
        var trunk = Rgb(112, 78, 52);
        var trunkLight = Rgb(150, 110, 74);
        var trunkDark = Rgb(78, 54, 40);
        var leaf = Rgb(70, 150, 66);
        var leafLight = Rgb(120, 196, 96);
        var leafDark = Rgb(44, 104, 52);

        // The trunk: three texels thick at the top, five at the foot, with roots spreading
        for (int y = 30; y < Height - 1; y++)
        {
            int half = y > 42 ? 3 : 2;
            for (int x = 16 - half; x < 16 + half; x++) c.Set(x, y, x < 16 - half + 1 ? trunkLight : x >= 16 + half - 1 ? trunkDark : trunk);
        }
        c.HLine(10, Height - 2, 3, trunkDark);
        c.HLine(19, Height - 2, 3, trunkDark);

        // The canopy: a broad round mass, lit from the upper left, with a lower lip of darker leaves
        const float cx = 16f, cy = 18f, rx = 14.5f, ry = 15f;
        for (int y = 0; y < 36; y++)
            for (int x = 0; x < Width; x++)
            {
                float u = (x + 0.5f - cx) / rx, v = (y + 0.5f - cy) / ry;
                if (u * u + v * v > 1f) continue;
                bool lit = u + v < -0.45f, shade = v > 0.38f || u + v > 0.9f;
                c.Set(x, y, lit ? leafLight : shade ? leafDark : leaf);
            }
        // Leaf edges: notches round the rim, the same every time
        foreach (var (x, y) in new[] { (5, 9), (26, 10), (3, 18), (28, 20), (7, 28), (24, 29), (13, 3), (20, 4) })
            c.Set(x, y, leafDark);
        foreach (var (x, y) in new[] { (9, 8), (15, 6), (22, 9), (12, 14), (19, 16) })
            c.Set(x, y, leafLight);

        if (inFruit && colour is { } fruit)
        {
            var body = ColourOf(fruit);
            foreach (var (x, y) in new[] { (9, 22), (20, 25), (14, 13) })
            {
                for (int dy = -3; dy <= 3; dy++)
                    for (int dx = -3; dx <= 3; dx++)
                        if (dx * dx + dy * dy <= 10) c.Set(x + dx, y + dy, body);
                c.Set(x - 1, y - 2, PixelCanvas.Light1(body, 0.55f));
                c.Set(x - 2, y - 1, PixelCanvas.Light1(body, 0.4f));
                c.Set(x + 1, y + 2, PixelCanvas.Shadow(body, 0.35f));
                c.Set(x + 2, y + 1, PixelCanvas.Shadow(body, 0.3f));
                // The stalk, and the hollow of the fruit where it hangs
                c.Set(x, y - 4, trunkDark);
                c.Set(x, y - 3, PixelCanvas.Shadow(body, 0.5f));
            }
        }
        Outline(c);
        return c;
    }
}
