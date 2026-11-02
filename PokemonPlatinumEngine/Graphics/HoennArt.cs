using System;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// Cards of Hoenn's last things of the map (plan 21 · H12, part p; style guide, "Hoenn's last things"), our own drawings:
/// Birth Island's crystal triangle. Painted in code, no GPU calls.
/// </summary>
internal static class HoennArt
{
    private static Color Rgb(int r, int g, int b) => new(r, g, b, 255);

    /// <summary>The triangle's card's size: a tile across and a little taller, so its point stands above the tile.</summary>
    public const int TriangleWidth = 30, TriangleHeight = 32;

    /// <summary>
    /// Birth Island's triangle, 30 by 32: an upright triangle of pale violet glass on a dark stone foot, its left face lit
    /// and its right in shade, a cyan seam down its middle and a glowing core where the seams meet. Its own shape, no
    /// game's sprite.
    /// </summary>
    public static void PaintTriangle(PixelCanvas c)
    {
        Color lit = Rgb(214, 196, 240), face = Rgb(168, 144, 214), shade = Rgb(118, 96, 172), seam = Rgb(110, 226, 236);
        Color core = Rgb(236, 252, 255), foot = Rgb(70, 66, 84), footLit = Rgb(104, 100, 120);
        const int apexX = 15, apexY = 2, baseY = 26;
        for (int y = apexY; y <= baseY; y++)
        {
            float t = (y - apexY) / (float)(baseY - apexY);
            int half = (int)MathF.Round(1 + t * 12);
            for (int x = apexX - half; x <= apexX + half; x++)
            {
                if (x < 0 || x >= TriangleWidth) continue;
                // The left face takes the light, the right is in shade, the lit rim runs up the left edge
                Color colour = x < apexX ? face : shade;
                if (x == apexX - half || x == apexX - half + 1) colour = lit;
                c.Set(x, y, colour);
            }
        }
        // The seam down the middle and along the foot, and the glowing core where they meet
        c.VLine(apexX, apexY + 2, baseY - apexY - 3, seam);
        c.HLine(apexX - 10, baseY - 1, 21, seam);
        c.Rect(apexX - 2, 17, 5, 5, seam);
        c.Rect(apexX - 1, 18, 3, 3, core);
        // A glint near the point
        c.Set(apexX - 2, apexY + 6, core);
        c.Set(apexX - 3, apexY + 8, lit);
        // The stone foot it stands in
        c.Rect(3, baseY + 1, 24, 4, foot);
        c.HLine(3, baseY + 1, 24, footLit);
        Pix.Outline(c);
    }

    // ------------------------------------------------------------------ the Abandoned Ship's wreck

    private static readonly Tone RustHull = Tone.Of(196, 184, 168, 222, 212, 196, 150, 136, 122);
    private static readonly Tone Rust = Tone.Of(150, 82, 52, 182, 112, 72, 108, 56, 38);
    private static readonly Tone OldWood = Tone.Of(132, 112, 92, 160, 140, 116, 96, 80, 66);
    private static readonly Color Hole = Rgb(34, 30, 34), SeaInside = Rgb(46, 92, 150);

    /// <summary>
    /// The Abandoned Ship lying in Route 108's sea (plan 21 · H12, part p; style guide, "Hoenn's last things"): the ferry's
    /// shape gone to ruin, our own drawing. Its stern half (the west) is sunk lower than its bow, its hull a stained white
    /// streaked with rust, its deck of grey weathered planks broken through in places to the sea inside, the cabin's windows
    /// dark and the funnel a rusted stump. The kit's origin is the north-west corner of its tiles; the ship lies east and
    /// west across <paramref name="w"/> texels.
    /// </summary>
    public static void Wreck(KitBuilder kit, int w, int d)
    {
        int x0 = 10, x1 = w - 10, z0 = 10, z1 = d - 8, mid = (x0 + x1) / 2;
        const int bowY = 16, sternY = 8;
        var bowDeck = kit.Face($"wreck.deck.bow.{x1 - mid}x{z1 - z0}", x1 - mid, z1 - z0, c => PaintWreckDeck(c, bow: true));
        var sternDeck = kit.Face($"wreck.deck.stern.{mid - x0}x{z1 - z0}", mid - x0, z1 - z0, c => PaintWreckDeck(c, bow: false));
        var bowSide = kit.Face($"wreck.hull.bow.{x1 - mid}", x1 - mid, bowY, PaintWreckHull);
        var sternSide = kit.Face($"wreck.hull.stern.{mid - x0}", mid - x0, sternY, PaintWreckHull);
        var bowEnd = kit.Face($"wreck.hull.end.{z1 - z0}x{bowY}", z1 - z0, bowY, PaintWreckHull);
        var sternEnd = kit.Face($"wreck.hull.end.{z1 - z0}x{sternY}", z1 - z0, sternY, PaintWreckHull);
        kit.Box(mid, x1, z0, z1, 0, bowY, bowDeck, bowSide, bowEnd, bowEnd);
        kit.Box(x0, mid, z0, z1, 0, sternY, sternDeck, sternSide, sternEnd, sternEnd);

        // The cabin on the bow half, its windows dark, and the funnel's rusted stump on its roof
        int cw = Math.Max(24, (x1 - mid) - 20), cd = Math.Max(20, z1 - z0 - 40);
        if (z1 - z0 - 40 < 20) return;
        int cx0 = mid + 6, cz0 = (z0 + z1 - cd) / 2 - 4;
        var roof = kit.Face($"wreck.roof.{cw}x{cd}", cw, cd, c => { Pix.Raised(c, 0, 0, c.Width, c.Height, RustHull); Streaks(c, 3); });
        var cabin = kit.Face($"wreck.cabin.{cw}", cw, 20, PaintWreckCabin);
        var cabinSide = kit.Face($"wreck.cabin.{cd}", cd, 20, PaintWreckCabin);
        kit.Box(cx0, cx0 + cw, cz0, cz0 + cd, bowY, bowY + 20, roof, cabin, cabinSide, cabinSide);
        var stump = kit.Face("wreck.funnel", 14, 12, c => { Pix.Raised(c, 0, 0, 14, 12, Rust); c.Rect(0, 3, 14, 2, Rust.Dark); });
        var stumpTop = kit.Face("wreck.funnel.top", 14, 14, c => { Pix.Raised(c, 0, 0, 14, 14, Rust); c.Rect(3, 3, 8, 8, Hole); });
        float fx = cx0 + cw / 2f - 7, fz = cz0 + cd / 2f - 7;
        kit.Box(fx, fx + 14, fz, fz + 14, bowY + 20, bowY + 32, stumpTop, stump, stump, stump);
    }

    /// <summary>Rust running down a face in streaks, a few texels wide, every so many texels along it.</summary>
    private static void Streaks(PixelCanvas c, int seed)
    {
        for (int x = 2 + seed % 5; x < c.Width - 1; x += 7 + (x * 3 + seed) % 6)
        {
            int length = 2 + (x * 5 + seed) % Math.Max(2, c.Height - 2);
            for (int y = 0; y < length && y < c.Height; y++)
                c.SetRaw(x, y, y < length / 2 ? Rust.Base : Rust.Light);
        }
    }

    private static void PaintWreckHull(PixelCanvas c)
    {
        Pix.Raised(c, 0, 0, c.Width, c.Height, RustHull);
        // The band along the waterline gone to rust, and rust streaked down from the rail
        c.Rect(0, c.Height - 4, c.Width, 4, Rust.Base);
        c.HLine(0, c.Height - 4, c.Width, Rust.Light);
        Streaks(c, c.Width);
        // Portholes a tile apart, dark and empty
        if (c.Height >= 12)
            for (int x = 14; x + 5 < c.Width; x += 32)
                Pix.Disc(c, x, 3, 4, Hole);
    }

    /// <summary>The deck from above: grey weathered planks inside a stained rail, broken through to the sea in places.</summary>
    private static void PaintWreckDeck(PixelCanvas c, bool bow)
    {
        int w = c.Width, h = c.Height;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                // The bow's end rounded off; the stern's broken square
                int fromX = bow ? w - 1 - x : x, fromY = Math.Min(y, h - 1 - y);
                if (bow && fromX + fromY < 8) continue;
                bool rail = fromY < 3 || fromX < 3;
                c.SetRaw(x, y, rail ? (fromY == 2 || fromX == 2 ? RustHull.Dark : RustHull.Base)
                    : y % 8 == 7 ? OldWood.Dark : (y / 8 + x / 24) % 3 == 1 ? OldWood.Light : OldWood.Base);
            }
        // Holes broken through the planks to the sea inside, more on the sunk stern
        int holes = bow ? 1 : 3;
        for (int i = 0; i < holes; i++)
        {
            int hx = 8 + (i * 37 + (bow ? 11 : 3)) % Math.Max(1, w - 20), hy = 6 + (i * 13 + 5) % Math.Max(1, h - 16);
            c.Rect(hx, hy, 8, 6, Hole);
            c.Rect(hx + 1, hy + 2, 6, 3, SeaInside);
            c.HLine(hx, hy, 8, OldWood.Dark);
        }
    }

    private static void PaintWreckCabin(PixelCanvas c)
    {
        Pix.Raised(c, 0, 0, c.Width, c.Height, RustHull);
        for (int x = 6; x + 10 <= c.Width - 4; x += 14)
        {
            c.Rect(x, 5, 10, 8, Rust.Dark);
            c.Rect(x + 1, 6, 8, 6, Hole);
        }
        c.Rect(0, c.Height - 4, c.Width, 2, Rust.Base);
        Streaks(c, c.Width + 1);
    }
}
