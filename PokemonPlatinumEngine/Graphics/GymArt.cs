using System;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// Pixel art for the things of the Gyms' puzzles that stand in the way like people (plan 01 · M9; style guide,
/// "Gyms"), each drawn as a card as the field's obstacles are: the Veilstone Gym's punching bags and stacks of tyres,
/// and the Hearthome Gym's bollards. Outlined like the field's other sprites. No GPU calls.
/// </summary>
internal static class GymArt
{
    private static Color Rgb(int r, int g, int b) => new(r, g, b, 255);

    /// <summary>A punching bag of red leather hanging from its chain, banded at top and foot, 24 by 44.</summary>
    public static void PaintPunchingBag(PixelCanvas c)
    {
        var leather = Tone.Of(206, 62, 58, 234, 104, 92, 150, 38, 48);
        var band = Tone.Of(250, 246, 238, 255, 255, 255, 196, 192, 196);
        var chain = Rgb(150, 154, 170);
        // The chain runs up out of the top of the card
        for (int y = 0; y < 8; y++) c.Set(12, y, y % 2 == 0 ? chain : Rgb(96, 100, 118));
        c.Rect(9, 7, 7, 2, Rgb(80, 84, 100));
        for (int y = 9; y < 42; y++)
        {
            // A bag rounded at its top and foot, a little fuller in the middle
            int half = y < 12 ? 6 + (y - 9) : y > 38 ? 9 - (y - 38) : 9;
            for (int x = 12 - half; x < 12 + half; x++)
            {
                float u = (x + 0.5f - 12f) / half;
                var tone = y is >= 13 and <= 15 || y is >= 34 and <= 36 ? band : leather;
                c.Set(x, y, u < -0.45f ? tone.Light : u > 0.5f ? tone.Dark : tone.Base);
            }
        }
        // Stitches down the middle and a glint on the leather
        for (int y = 17; y < 33; y += 3) c.Set(12, y, leather.Dark);
        c.Rect(6, 18, 2, 8, Rgb(246, 156, 140));
        Pix.Outline(c);
    }

    /// <summary>Three tyres stacked one on another, 30 by 30.</summary>
    public static void PaintTireStack(PixelCanvas c)
    {
        var rubber = Tone.Of(58, 58, 72, 96, 98, 116, 34, 34, 46);
        for (int tyre = 0; tyre < 3; tyre++)
        {
            int top = 20 - tyre * 8;
            for (int y = top; y < top + 9; y++)
                for (int x = 2; x < 28; x++)
                {
                    float u = (x + 0.5f - 15f) / 13f, v = (y + 0.5f - (top + 4.5f)) / 4.5f;
                    if (u * u + v * v * 0.35f > 1f) continue;
                    var col = v < -0.5f ? rubber.Light : v > 0.5f ? rubber.Dark : rubber.Base;
                    // The tread: a dark notch every four texels across its face
                    if (MathF.Abs(v) < 0.5f && x % 4 == 0) col = rubber.Dark;
                    c.Set(x, y, col);
                }
        }
        // The hole of the top tyre, seen from above
        for (int x = 10; x < 20; x++) c.Set(x, 5, Rgb(24, 22, 32));
        for (int x = 8; x < 22; x++) c.Set(x, 6, Rgb(24, 22, 32));
        Pix.Outline(c);
    }

    /// <summary>A bollard of dark polished stone with a gilded cap, 16 by 28.</summary>
    public static void PaintBollard(PixelCanvas c)
    {
        var stone = Tone.Of(84, 70, 108, 124, 106, 150, 54, 44, 72);
        var gold = Tone.Of(222, 182, 82, 250, 222, 132, 160, 120, 52);
        for (int y = 6; y < 27; y++)
            for (int x = 3; x < 13; x++)
            {
                float u = (x + 0.5f - 8f) / 5f;
                c.Set(x, y, u < -0.4f ? stone.Light : u > 0.5f ? stone.Dark : stone.Base);
            }
        for (int y = 1; y < 7; y++)
        {
            int half = y < 3 ? 2 + y : 6;
            for (int x = 8 - half; x < 8 + half; x++) c.Set(x, y, x < 7 ? gold.Light : x > 9 ? gold.Dark : gold.Base);
        }
        c.Rect(2, 25, 12, 2, stone.Dark);
        Pix.Outline(c);
    }
}
