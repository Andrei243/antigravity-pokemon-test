using System;
using Raylib_cs;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using static PokemonPlatinumEngine.Graphics.Pix;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// The card a patch of soft soil is drawn with (plan 06 · R14a; style guide, "Berry patches"): a mound of loam a tile
/// wide, darker and flecked once mulch is laid, and whatever grows in it, stage by stage: a seed, a sprout, a young
/// plant, a plant in flower and a plant in fruit. A berry's colour is its strongest flavour's, as a Poffin's is
/// (spicy red, dry blue, sweet pink, bitter green, sour yellow). Our own art; no drawing calls, so tests paint it.
/// </summary>
public static class BerryArt
{
    public const int Width = 32, Height = 40;

    /// <summary>The colour of a berry: its strongest flavour's, the first of the five where two are equal.</summary>
    public static Color ColourOf(string? berry)
    {
        if (berry == null || Data.ItemDatabase.Get(berry)?.Berry is not { } b) return Rgb(214, 64, 58);
        var flavours = new[] { b.Spiciness, b.Dryness, b.Sweetness, b.Bitterness, b.Sourness };
        int strongest = 0;
        for (int i = 1; i < flavours.Length; i++)
            if (flavours[i] > flavours[strongest]) strongest = i;
        return strongest switch
        {
            0 => Rgb(214, 64, 58),
            1 => Rgb(70, 110, 214),
            2 => Rgb(236, 120, 170),
            3 => Rgb(96, 170, 72),
            _ => Rgb(232, 200, 60)
        };
    }

    /// <summary>The key a look is kept under: one card for each stage, colour and mulch.</summary>
    public static string KeyOf(BerryStage stage, string? berry, bool mulched)
    {
        var c = ColourOf(berry);
        return $"{stage}/{c.R},{c.G},{c.B}/{mulched}";
    }

    public static PixelCanvas Paint(BerryStage stage, string? berry, bool mulched)
    {
        var c = new PixelCanvas(Width, Height);
        var colour = ColourOf(berry);
        PaintPlant(c, stage, colour);
        PaintSoil(c, mulched);
        Outline(c);
        return c;
    }

    /// <summary>The mound, its lowest row the card's foot: lit from the upper left, with crumbs of a darker loam.</summary>
    private static void PaintSoil(PixelCanvas c, bool mulched)
    {
        var top = mulched ? Rgb(92, 64, 50) : Rgb(132, 94, 64);
        var mid = mulched ? Rgb(72, 50, 42) : Rgb(108, 76, 54);
        var low = mulched ? Rgb(54, 38, 34) : Rgb(84, 60, 46);
        var crumb = mulched ? Rgb(138, 112, 74) : Rgb(72, 50, 40);
        const float cx = 16f, cy = 37f, rx = 14f, ry = 5f;
        for (int y = 31; y < Height; y++)
            for (int x = 1; x < Width - 1; x++)
            {
                float u = (x + 0.5f - cx) / rx, v = (y + 0.5f - cy) / ry;
                if (u * u + v * v > 1f) continue;
                c.Set(x, y, u + v < -0.6f ? top : v > 0.45f ? low : mid);
            }
        // Crumbs (mulch's straw, or clods of earth), at places of the card's own, the same every time
        foreach (var (x, y) in new[] { (7, 35), (12, 33), (19, 34), (24, 36), (10, 38), (21, 38), (15, 36) })
            c.Set(x, y, crumb);
    }

    private static void PaintPlant(PixelCanvas c, BerryStage stage, Color colour)
    {
        var stem = Rgb(78, 130, 60);
        var leaf = Rgb(84, 168, 76);
        var leafLight = Rgb(140, 210, 110);
        var leafDark = Rgb(48, 112, 56);

        void Leaf(int x, int y, int dir)
        {
            // A leaf four texels long, its tip up and out
            c.HLine(dir > 0 ? x : x - 3, y, 4, leaf);
            c.HLine(dir > 0 ? x + 1 : x - 3, y - 1, 3, leafLight);
            c.Set(dir > 0 ? x + 4 : x - 4, y - 2, leaf);
            c.Set(dir > 0 ? x : x - 1, y + 1, leafDark);
        }

        void Berry(int x, int y)
        {
            for (int dy = -2; dy <= 2; dy++)
                for (int dx = -2; dx <= 2; dx++)
                    if (dx * dx + dy * dy <= 5) c.Set(x + dx, y + dy, colour);
            c.Set(x - 1, y - 1, PixelCanvas.Light1(colour, 0.5f));
            c.Set(x + 1, y + 1, PixelCanvas.Shadow(colour, 0.35f));
            c.Set(x, y - 3, leafDark);
        }

        void Flower(int x, int y)
        {
            var petal = PixelCanvas.Light1(colour, 0.65f);
            c.Set(x, y - 1, petal);
            c.Set(x - 1, y, petal);
            c.Set(x + 1, y, petal);
            c.Set(x, y + 1, petal);
            c.Set(x, y, Rgb(250, 220, 90));
        }

        switch (stage)
        {
            case BerryStage.Planted:
                // A seed showing in the top of the mound
                c.Rect(15, 31, 3, 2, PixelCanvas.Shadow(colour, 0.25f));
                break;
            case BerryStage.Sprouted:
                c.VLine(16, 27, 6, stem);
                Leaf(17, 28, 1);
                Leaf(15, 29, -1);
                break;
            case BerryStage.Growing:
                c.VLine(16, 17, 16, stem);
                Leaf(17, 20, 1);
                Leaf(15, 23, -1);
                Leaf(17, 26, 1);
                Leaf(15, 29, -1);
                break;
            case BerryStage.Blooming:
            case BerryStage.Fruit:
                c.VLine(16, 10, 23, stem);
                c.Line(16, 18, 10, 13, stem);
                c.Line(16, 16, 22, 11, stem);
                Leaf(17, 22, 1);
                Leaf(15, 25, -1);
                Leaf(17, 28, 1);
                Leaf(15, 19, -1);
                Leaf(11, 15, -1);
                Leaf(21, 13, 1);
                if (stage == BerryStage.Blooming)
                {
                    Flower(10, 11);
                    Flower(16, 8);
                    Flower(22, 9);
                }
                else
                {
                    Berry(9, 14);
                    Berry(16, 10);
                    Berry(23, 13);
                }
                break;
        }
    }
}
