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

    private static Color Lighter(Color c, float k) => new((byte)(c.R + (255 - c.R) * k), (byte)(c.G + (255 - c.G) * k), (byte)(c.B + (255 - c.B) * k), (byte)255);
    private static Color Darker(Color c, float k) => new((byte)(c.R * (1 - k)), (byte)(c.G * (1 - k)), (byte)(c.B * (1 - k)), (byte)255);

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

    // ------------------------------------------------------------------ the Lilycove Museum's paintings

    /// <summary>A contest painting's prop model: its quality (0 to 4, <c>ContestStat</c>) and the winner's colour as the data names it.</summary>
    public static string ContestPaintingModel(int quality, string? colour) => $"contest:{quality}:{(colour ?? "gray").ToLowerInvariant()}";

    /// <summary>The background of a painting of each quality, cool to tough: the contest's own colours, softened.</summary>
    private static readonly Color[] PaintingSky =
    {
        Rgb(232, 118, 98), Rgb(112, 168, 232), Rgb(246, 166, 196), Rgb(128, 196, 128), Rgb(236, 206, 104)
    };

    /// <summary>The colour the species data names, as a painter's colour.</summary>
    private static Color SpeciesPaint(string colour) => colour switch
    {
        "red" => Rgb(214, 70, 64),
        "blue" => Rgb(70, 112, 210),
        "yellow" => Rgb(240, 208, 70),
        "green" => Rgb(90, 168, 92),
        "black" => Rgb(66, 64, 78),
        "brown" => Rgb(156, 108, 70),
        "purple" => Rgb(150, 98, 186),
        "pink" => Rgb(240, 150, 190),
        "white" => Rgb(240, 238, 232),
        _ => Rgb(156, 160, 170)
    };

    /// <summary>
    /// A winner's painting on the gallery's wall (plan 21 · H11, part f; style guide, "Hoenn's last things", "A painting on
    /// the wall"): a gilt frame two tiles wide, the quality's colour behind and the winner as a round figure of its own
    /// colour, with a sparkle of its quality. Our own drawing, nothing of a game's: the painting looked at closely is the
    /// Fashion Case's viewer (<c>appeals view</c>). 56 by 30.
    /// </summary>
    public static void PaintContestPainting(PixelCanvas c, int quality, string colour)
    {
        const int w = 56, h = 30;
        Pix.Raised(c, 0, 0, w, h, Tone.Of(198, 156, 86));
        var sky = PaintingSky[Math.Clamp(quality, 0, 4)];
        c.Rect(3, 3, w - 6, h - 6, sky);
        // The light falling from the top left, a ground below
        c.Rect(3, 3, w - 6, 4, Lighter(sky, 0.35f));
        c.Rect(3, h - 8, w - 6, 5, Darker(sky, 0.25f));
        // The winner: a body and a head of its colour, its eyes, its shadow on the ground
        var body = SpeciesPaint(colour);
        var shade = Darker(body, 0.3f);
        c.Rect(19, h - 8, 20, 2, Darker(sky, 0.45f));
        Pix.Disc(c, 18, 10, 16, body);
        Pix.Disc(c, 28, 6, 11, body);
        Pix.Disc(c, 20, 18, 6, shade);
        c.Rect(31, 10, 2, 2, Rgb(30, 30, 40));
        c.Rect(35, 10, 2, 2, Rgb(30, 30, 40));
        c.Set(31, 10, Rgb(250, 250, 250));
        c.Set(35, 10, Rgb(250, 250, 250));
        // The quality's sparkle in the corner
        int sx = 46, sy = 9;
        c.Rect(sx - 3, sy, 7, 1, Rgb(255, 250, 220));
        c.Rect(sx, sy - 3, 1, 7, Rgb(255, 250, 220));
        c.Set(sx, sy, Rgb(255, 255, 255));
        Pix.Shade(c, 3, 3, w - 6, 1, 0.25f);
    }

    /// <summary>A painting's card, two tiles wide on the wall above its frame's tiles; the prop's model says what it shows.</summary>
    public static void ContestPainting(KitBuilder kit, string model)
    {
        var parts = model.Split(':');
        int quality = parts.Length > 1 && int.TryParse(parts[1], out int q) ? q : 0;
        string colour = parts.Length > 2 ? parts[2] : "gray";
        var face = kit.Face($"hoenn.painting.{quality}.{colour}", 56, 30, c => PaintContestPainting(c, quality, colour));
        kit.Card(4, 60, 32.5f, 36, 66, face);
    }

    // ------------------------------------------------------------------ the Safari Zone's Pokéblock feeders

    /// <summary>
    /// One of the Safari Zone's Pokéblock feeders (plan 21 · H11, part f; style guide, "Hoenn's last things", "A Pokéblock
    /// feeder"): a little roofed box of pale wood on a post, its open front showing a tray, as a card standing on the
    /// feeder's tile. Our own drawing. 24 by 34.
    /// </summary>
    public static void PaintFeeder(PixelCanvas c)
    {
        Color post = Rgb(120, 92, 64), postLit = Rgb(150, 118, 84), wood = Rgb(214, 184, 132), woodShade = Rgb(176, 146, 98);
        Color roof = Rgb(186, 82, 66), roofLit = Rgb(220, 112, 90), tray = Rgb(96, 70, 48), block = Rgb(240, 120, 150);
        // The post into the ground
        c.Rect(10, 18, 4, 16, post);
        c.VLine(10, 18, 16, postLit);
        // The box
        c.Rect(3, 8, 18, 12, wood);
        c.Rect(3, 17, 18, 3, woodShade);
        // Its open front and the tray inside, a Pokéblock left in it
        c.Rect(6, 11, 12, 6, tray);
        c.Rect(9, 13, 3, 3, block);
        c.Set(9, 13, Rgb(255, 196, 210));
        // The roof, a gable over the box
        for (int y = 0; y < 8; y++)
        {
            int half = 3 + y * 9 / 7;
            c.HLine(12 - half, y + 1, half * 2, y < 2 ? roofLit : roof);
        }
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

    // ------------------------------------------------------------------ the Abandoned Ship's deck

    private static readonly Tone Plate = Tone.Of(150, 146, 140, 176, 172, 164, 112, 108, 106);

    /// <summary>
    /// The Abandoned Ship's deckhouse and bulwarks on its deck (plan 21 · part r; style guide, "Hoenn's last things"): a
    /// box over the prop's tiles from the deck south of it up to the top of what it covers, a texel wider all round than
    /// those tiles so it stands in front of the rock the import lays there. Tall, a wall of white-painted steel gone to rust
    /// with a row of dark portholes; low, a bulwark with a rusted rail along its top. Its roof is rusted plate. The kit's
    /// origin is the north-west corner of its tiles, at the height of their middle.
    /// </summary>
    public static void ShipCabin(KitBuilder kit, Overworld.Map map, Overworld.Prop p)
    {
        float hi = float.MinValue, lo = float.MaxValue;
        for (int y = p.Y; y < p.Y + p.Depth; y++)
            for (int x = p.X; x < p.X + p.Width; x++)
                hi = MathF.Max(hi, Relief.At(map, x + 0.5f, y + 0.5f));
        // It stands on the deck before it: the lowest ground on the row south of it
        for (int x = p.X; x < p.X + p.Width; x++)
            if (map.InBounds(x, p.Y + p.Depth)) lo = MathF.Min(lo, Relief.At(map, x + 0.5f, p.Y + p.Depth + 0.5f));
        if (lo == float.MaxValue || lo > hi) lo = hi;
        float perTexel = kit.VS / 32f;
        int y0 = (int)MathF.Floor((lo - kit.Origin.Y) / perTexel), y1 = (int)MathF.Ceiling((hi - kit.Origin.Y) / perTexel) + 1;
        int tall = Math.Max(4, y1 - y0), w = p.Width * 32 + 2, d = p.Depth * 32 + 2;
        bool house = tall >= 40;
        var roof = kit.Face($"ship.roof.{w}x{d}", w, d, c => PaintShipRoof(c, house));
        var front = kit.Face($"ship.wall.{w}x{tall}", w, tall, c => PaintShipWall(c, house));
        var side = kit.Face($"ship.wall.{d}x{tall}", d, tall, c => PaintShipWall(c, house));
        kit.Box(-1, w - 1, -1, d - 1, y0, y0 + tall, roof, front, side, side, front);
    }

    /// <summary>A deckhouse's roof or a bulwark's top from above: rusted plate in panels with a dark seam round its edge.</summary>
    private static void PaintShipRoof(PixelCanvas c, bool house)
    {
        if (house)
        {
            Pix.Raised(c, 0, 0, c.Width, c.Height, Plate);
            for (int x = 15; x < c.Width; x += 16) c.VLine(x, 0, c.Height, Plate.Dark);
            for (int y = 15; y < c.Height; y += 16) c.HLine(0, y, c.Width, Plate.Dark);
            Streaks(c, c.Width + c.Height);
        }
        else
        {
            // A bulwark's top: the rail, rusted, with the hull's white each side of it
            Pix.Raised(c, 0, 0, c.Width, c.Height, RustHull);
            int mid = c.Height / 2;
            c.Rect(0, mid - 3, c.Width, 6, Rust.Base);
            c.HLine(0, mid - 3, c.Width, Rust.Light);
            c.HLine(0, mid + 2, c.Width, Rust.Dark);
        }
    }

    /// <summary>A deckhouse's wall: white-painted steel streaked with rust, its foot rusted, a row of dark portholes; or a bulwark: the same steel under a rusted rail.</summary>
    private static void PaintShipWall(PixelCanvas c, bool house)
    {
        Pix.Raised(c, 0, 0, c.Width, c.Height, RustHull);
        c.Rect(0, c.Height - 4, c.Width, 4, Rust.Base);
        c.HLine(0, c.Height - 4, c.Width, Rust.Light);
        if (house)
        {
            // A band of plate under the roof and portholes a tile apart, dark and empty
            c.Rect(0, 0, c.Width, 3, Plate.Base);
            c.HLine(0, 3, c.Width, Plate.Dark);
            int row = Math.Min(14, c.Height / 3);
            for (int x = 10; x + 10 < c.Width; x += 32)
            {
                Pix.Disc(c, x, row, 10, Rust.Dark);
                Pix.Disc(c, x + 2, row + 2, 6, Hole);
            }
        }
        else
        {
            // The rail along the bulwark's top
            c.Rect(0, 0, c.Width, 3, Rust.Base);
            c.HLine(0, 3, c.Width, Rust.Dark);
        }
        Streaks(c, c.Width + c.Height);
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
