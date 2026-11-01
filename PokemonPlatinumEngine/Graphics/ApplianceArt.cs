using System;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// Rotom's five appliances on Silph Co.'s research floor (plan 20 · J15, part n; style guide, "Rotom's appliances"):
/// cards a tile across, each a household machine seen from the front with a darker right side and a lit top edge, a
/// little orange-and-pale-blue lead at its foot, and the one-texel outline every card has. Painted in code, no GPU calls.
/// </summary>
internal static class ApplianceArt
{
    private static Color Rgb(int r, int g, int b) => new(r, g, b, 255);

    private static readonly Color White = Rgb(226, 222, 212), WhiteLit = Rgb(244, 242, 236), WhiteShade = Rgb(176, 170, 160);
    private static readonly Color Grey = Rgb(140, 140, 150), Dark = Rgb(48, 52, 66);
    private static readonly Color LeadOrange = Rgb(236, 128, 52), LeadBlue = Rgb(170, 206, 232);

    /// <summary>A box from (x, y), w by h: its face, a lit top row, a shaded right column and a shaded foot row.</summary>
    private static void Box(PixelCanvas c, int x, int y, int w, int h, Color face, Color lit, Color shade)
    {
        c.Rect(x, y, w, h, face);
        c.HLine(x, y, w, lit);
        c.VLine(x + w - 1, y + 1, h - 1, shade);
        c.VLine(x + w - 2, y + 1, h - 1, shade);
        c.HLine(x, y + h - 1, w, shade);
    }

    /// <summary>The plug lead that says it is an appliance: a short orange flex ending in a pale blue plug, from (x, y) to the left.</summary>
    private static void Lead(PixelCanvas c, int x, int y)
    {
        for (int i = 0; i < 4; i++) c.Set(x - i, y, LeadOrange);
        c.Rect(x - 5, y - 1, 2, 2, LeadBlue);
    }

    /// <summary>The microwave, 30 by 22: a white box with a dark window glowing warm at its left and three buttons at its right.</summary>
    public static void PaintMicrowave(PixelCanvas c)
    {
        Lead(c, 7, 18);
        Box(c, 7, 4, 22, 15, White, WhiteLit, WhiteShade);
        c.Rect(9, 7, 12, 9, Dark);
        c.Rect(11, 9, 8, 5, Rgb(236, 150, 70));
        c.HLine(11, 9, 8, Rgb(250, 196, 120));
        foreach (int y in new[] { 7, 10, 13 }) c.Rect(23, y, 3, 2, Grey);
        c.Rect(8, 19, 3, 1, WhiteShade);
        c.Rect(24, 19, 3, 1, WhiteShade);
        Pix.Outline(c);
    }

    /// <summary>The washing machine, 28 by 30: a white box, a strip of controls along its top and a round blue door ringed in grey.</summary>
    public static void PaintWashingMachine(PixelCanvas c)
    {
        Lead(c, 7, 26);
        Box(c, 7, 2, 20, 26, White, WhiteLit, WhiteShade);
        c.HLine(8, 6, 17, WhiteShade);
        c.Rect(9, 3, 3, 2, Grey);
        c.Rect(20, 3, 4, 2, Rgb(96, 150, 206));
        for (int y = 9; y <= 25; y++)
            for (int x = 8; x <= 25; x++)
            {
                float u = (x + 0.5f - 16.5f) / 7.5f, v = (y + 0.5f - 17f) / 7.5f;
                float d = u * u + v * v;
                if (d > 1f) continue;
                c.Set(x, y, d > 0.66f ? Grey : u + v < -0.6f ? Rgb(150, 196, 236) : Rgb(96, 150, 206));
            }
        Pix.Outline(c);
    }

    /// <summary>The refrigerator, 24 by 40: a tall pale blue box in two doors, a handle on each.</summary>
    public static void PaintRefrigerator(PixelCanvas c)
    {
        Box(c, 3, 1, 18, 37, Rgb(200, 222, 236), Rgb(226, 240, 248), Rgb(150, 176, 196));
        c.HLine(3, 13, 16, Rgb(150, 176, 196));
        var handle = Rgb(120, 128, 140);
        c.VLine(5, 6, 5, handle);
        c.VLine(5, 16, 9, handle);
        c.Rect(4, 38, 2, 2, Rgb(96, 104, 118));
        c.Rect(17, 38, 2, 2, Rgb(96, 104, 118));
        Lead(c, 13, 39);
        Pix.Outline(c);
    }

    /// <summary>The fan, 26 by 32: a round grey guard with three pale blue blades about a white hub, on a stem over a flat foot.</summary>
    public static void PaintElectricFan(PixelCanvas c)
    {
        var guard = Rgb(150, 156, 166);
        var blade = Rgb(170, 206, 232);
        Lead(c, 6, 30);
        c.Rect(6, 28, 14, 3, guard);
        c.HLine(6, 28, 14, Rgb(190, 196, 204));
        c.Rect(12, 19, 2, 9, Rgb(120, 126, 138));
        for (int y = 1; y <= 19; y++)
            for (int x = 3; x <= 22; x++)
            {
                float u = (x + 0.5f - 13f) / 9.5f, v = (y + 0.5f - 10.5f) / 9.5f;
                float d = u * u + v * v;
                if (d > 1f) continue;
                if (d > 0.8f) { c.Set(x, y, guard); continue; }
                // Three blades: the angle round the hub within a band of each third of a turn
                float a = MathF.Atan2(v, u) + MathF.PI;
                float k = a / (MathF.Tau / 3f) % 1f;
                if (k < 0.45f && d > 0.04f) c.Set(x, y, blade);
                else if ((x + y) % 4 == 0) c.Set(x, y, guard);
            }
        c.Rect(12, 9, 3, 3, White);
        Pix.Outline(c);
    }

    /// <summary>The mower, 30 by 22: a grass green body over two black wheels, with a bent grey handle rising behind it.</summary>
    public static void PaintLawnmower(PixelCanvas c)
    {
        var handle = Rgb(120, 126, 138);
        for (int i = 0; i < 8; i++) c.Set(20 + i / 2, 2 + i, handle);
        c.HLine(19, 2, 4, handle);
        Lead(c, 6, 14);
        Box(c, 6, 9, 22, 8, Rgb(86, 170, 86), Rgb(130, 204, 118), Rgb(56, 120, 60));
        c.Rect(10, 11, 6, 2, Rgb(56, 120, 60));
        var wheel = Rgb(44, 44, 52);
        foreach (int x in new[] { 8, 22 })
        {
            c.Rect(x, 16, 5, 5, wheel);
            c.Set(x + 2, 18, Grey);
        }
        Pix.Outline(c);
    }
}
