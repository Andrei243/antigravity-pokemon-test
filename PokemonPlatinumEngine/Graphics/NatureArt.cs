using System;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// The pixel art of the field's plants and ledges (style guide, "Grass, flowers, trees, ledges and rocks"): every
/// texture is drawn from placed shapes in a few flat shades, with no random per-texel variation. Trees are grey
/// (tinted per tree); the rest carry their own colours. No GPU calls, so the art can be tested.
/// </summary>
internal static class NatureArt
{
    private static Color Rgb(int r, int g, int b) => new(r, g, b, 255);
    private static Color Grey(int v) => new(v, v, v, 255);

    /// <summary>The four flat tones of the tree textures, darkest first.</summary>
    public static readonly int[] TreeTones = { 118, 162, 206, 244 };

    // ------------------------------------------------------------------ grass and flowers

    private static readonly Color[] TallShades = { Rgb(36, 110, 62), Rgb(62, 150, 78), Rgb(106, 196, 98), Rgb(170, 232, 130) };
    private static readonly Color[] LawnShades = { Rgb(70, 150, 82), Rgb(88, 172, 90), Rgb(120, 200, 102), Rgb(160, 222, 122) };

    /// <summary>
    /// A blade from (baseX, bottom row) leaning to tipX at the given height: dark at the foot, lighter upward, one
    /// texel of the lightest shade at the tip; it is two texels thick (three near the foot), the side texels a shade darker.
    /// </summary>
    private static void Blade(PixelCanvas c, int ox, int baseX, int tipX, int height, Color[] shades, int darkRows, int midRows)
    {
        int bottom = c.Height - 1;
        for (int y = 0; y < height; y++)
        {
            float t = height > 1 ? y / (float)(height - 1) : 0f;
            int x = ox + (int)MathF.Round(baseX + (tipX - baseX) * t);
            int shade = y == height - 1 ? 3 : y < darkRows ? 0 : y < darkRows + midRows ? 1 : 2;
            c.SetRaw(((x % c.Width) + c.Width) % c.Width, bottom - y, shades[shade]);
            if (y < height - 2) c.SetRaw((((x + 1) % c.Width) + c.Width) % c.Width, bottom - y, shades[Math.Max(0, shade - 1)]);
            if (y < height * 0.35f) c.SetRaw((((x - 1) % c.Width) + c.Width) % c.Width, bottom - y, shades[Math.Max(0, shade - 1)]);
        }
    }

    /// <summary>Tall grass: 32x16, two clumps of five blades each. Repeats along a row.</summary>
    public static PixelCanvas TallGrass()
    {
        var c = new PixelCanvas(32, 16);
        (int Base, int Tip, int Height)[][] clumps =
        {
            new[] { (3, 0, 9), (12, 15, 10), (5, 3, 13), (10, 12, 13), (8, 8, 15) },
            new[] { (2, 0, 8), (13, 15, 11), (5, 4, 12), (10, 11, 15), (7, 7, 14) }
        };
        for (int k = 0; k < clumps.Length; k++)
            foreach (var (b, tip, height) in clumps[k]) Blade(c, k * 16, b, tip, height, TallShades, 2, 4);
        return c;
    }

    /// <summary>A lawn tuft: 16x12, four short blades in the lawn's shades.</summary>
    public static PixelCanvas LawnTuft()
    {
        var c = new PixelCanvas(16, 12);
        foreach (var (b, tip, height) in new[] { (5, 2, 7), (10, 13, 8), (7, 6, 10), (8, 9, 11) })
            Blade(c, 0, b, tip, height, LawnShades, 1, 3);
        return c;
    }

    /// <summary>Flowers: three 16x16 frames side by side (white, red, yellow), each a stem, two leaves and a blossom.</summary>
    public static PixelCanvas Flowers()
    {
        var c = new PixelCanvas(48, 16);
        var stem = Rgb(60, 146, 76);
        (Color Petal, Color Shade)[] kinds =
        {
            (Rgb(252, 252, 252), Rgb(200, 214, 236)), (Rgb(236, 84, 96), Rgb(186, 56, 72)), (Rgb(250, 210, 76), Rgb(214, 160, 48))
        };
        for (int k = 0; k < kinds.Length; k++)
        {
            int ox = k * 16;
            for (int y = 8; y <= 15; y++) c.SetRaw(ox + 8, y, stem);
            c.SetRaw(ox + 7, 11, stem); c.SetRaw(ox + 6, 12, stem); c.SetRaw(ox + 5, 12, stem);
            c.SetRaw(ox + 9, 12, stem); c.SetRaw(ox + 10, 13, stem); c.SetRaw(ox + 11, 13, stem);

            // A round blossom five texels across, shaded on the lower right, with a two-texel centre
            for (int y = 3; y <= 7; y++)
                for (int x = 6; x <= 10; x++)
                {
                    int dx = x - 8, dy = y - 5;
                    if (Math.Abs(dx) + Math.Abs(dy) > 3 || (Math.Abs(dx) == 2 && Math.Abs(dy) == 2)) continue;
                    c.SetRaw(ox + x, y, dx + dy >= 2 ? kinds[k].Shade : kinds[k].Petal);
                }
            c.SetRaw(ox + 8, 5, Rgb(250, 196, 64)); c.SetRaw(ox + 7, 5, Rgb(250, 196, 64));
        }
        return c;
    }

    // ------------------------------------------------------------------ trees

    /// <summary>
    /// A pine tier: 64x32, U wraps round the tree, V runs from the tip (row 0) to the rim. Four bands of scalloped
    /// needles, each scallop a tooth pointing down with shade between the teeth and a highlight on its upper
    /// left; the last band's teeth are cut out, so the tier has a toothed rim.
    /// </summary>
    public static PixelCanvas Needles()
    {
        const int w = 64, h = 32;
        var c = new PixelCanvas(w, h);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int band = y / 8, r = y % 8;
                int xs = (x + (band % 2) * 4) % 8;
                // The tooth is full width for its top four rows, then tapers to two texels
                int half = r < 4 ? 4 : 8 - r;
                bool inside = xs >= 4 - half && xs < 4 + half;
                if (!inside)
                {
                    if (band < 3) c.SetRaw(x, y, Grey(TreeTones[0]));
                    continue;
                }
                int tone = band == 0 ? 2 : 1;
                if (r == 1 && xs is 1 or 2) tone++;
                else if (r >= 6) tone = Math.Max(0, tone - 1);
                c.SetRaw(x, y, Grey(TreeTones[Math.Min(3, tone)]));
            }
        return c;
    }

    /// <summary>Bark: 32x32, grooves every eight texels (broken here and there) with a light line beside each, and two knots.</summary>
    public static PixelCanvas Bark()
    {
        var c = new PixelCanvas(32, 32);
        var bark = Rgb(116, 80, 54);
        var dark = Rgb(86, 58, 42);
        var light = Rgb(146, 104, 70);
        for (int y = 0; y < 32; y++)
            for (int x = 0; x < 32; x++)
            {
                bool gap = (y + x / 8 * 7) % 16 < 3;
                c.SetRaw(x, y, gap ? bark : x % 8 == 0 ? dark : x % 8 == 1 ? light : bark);
            }
        foreach (var (kx, ky) in new[] { (12, 6), (28, 21) })
        {
            c.SetRaw(kx, ky, dark); c.SetRaw(kx + 1, ky, dark);
            c.SetRaw(kx, ky + 1, light); c.SetRaw(kx + 1, ky + 1, light);
        }
        return c;
    }

    /// <summary>
    /// A round crown's leaves: 64x64, overlapping clumps laid like shingles, each mid-toned with a light crescent
    /// on the upper left and a dark one on the lower right. The cut-out version leaves gaps between clumps for
    /// the crown's outer shell.
    /// </summary>
    public static PixelCanvas Leaves(bool cutout)
    {
        const int s = 64;
        var c = new PixelCanvas(s, s);
        if (!cutout) c.Fill(Grey(128));
        int[] jitter = { 0, 3, -2, 2, -3, 1, 2, -1 };
        for (int gy = 0; gy < 4; gy++)
            for (int gx = 0; gx < 4; gx++)
            {
                if (cutout && (gx + gy) % 3 == 0) continue;
                float cx = gx * 16 + 8 + jitter[(gx * 3 + gy) % 8] + (gy % 2) * 8;
                float cy = gy * 16 + 8 + jitter[(gx + gy * 5) % 8];
                const float rx = 11f, ry = 9f;
                for (int y = (int)(cy - ry); y <= (int)(cy + ry); y++)
                    for (int x = (int)(cx - rx); x <= (int)(cx + rx); x++)
                    {
                        float u = (x + 0.5f - cx) / rx, v = (y + 0.5f - cy) / ry;
                        float d = u * u + v * v;
                        if (d > 1f) continue;
                        int tone = u + v < -0.5f ? 2 : u + v > 0.6f && d > 0.5f ? 0 : 1;
                        c.SetRaw(((x % s) + s) % s, ((y % s) + s) % s, Grey(tone == 2 ? 226 : tone == 1 ? 178 : 128));
                    }
            }
        return c;
    }

    // ------------------------------------------------------------------ ledges

    /// <summary>
    /// A ledge's face: 32x12. A bright edge, a grass lip that hangs in scallops, dirt with a broken strata line,
    /// and a dark base.
    /// </summary>
    public static PixelCanvas LedgeFace()
    {
        var c = new PixelCanvas(32, 12);
        var edge = Rgb(160, 222, 122);
        var lip = Rgb(120, 200, 102);
        var lipShade = Rgb(70, 150, 82);
        var dirt = Rgb(178, 138, 90);
        var strata = Rgb(146, 108, 72);
        var dark = Rgb(116, 84, 60);
        for (int x = 0; x < 32; x++)
        {
            // The lip hangs three or four texels, in a pattern that repeats every eight
            int hang = x % 8 is 1 or 2 or 5 ? 4 : 3;
            for (int y = 0; y < 12; y++)
            {
                Color col = y == 0 ? edge
                    : y < hang ? lip
                    : y == hang ? lipShade
                    : y >= 9 ? dark
                    : y == 7 && x % 16 is < 5 or > 8 ? strata
                    : dirt;
                c.SetRaw(x, y, col);
            }
        }
        return c;
    }

    /// <summary>
    /// A ledge's face in a cave: 32x12, in the rock face's shades. A light edge, a bed lit along its top, a
    /// broken line of shade under it, and a dark base.
    /// </summary>
    public static PixelCanvas RockLedgeFace()
    {
        var c = new PixelCanvas(32, 12);
        var edge = Rgb(196, 192, 190);
        var lit = Rgb(176, 170, 166);
        var bed = Rgb(150, 146, 150);
        var shade = Rgb(104, 100, 112);
        var dark = Rgb(74, 70, 86);
        for (int x = 0; x < 32; x++)
            for (int y = 0; y < 12; y++)
            {
                Color col = y == 0 ? edge
                    : y is 1 or 2 ? lit
                    : y >= 10 ? dark
                    : y == 6 && x % 16 is < 6 or > 9 ? shade
                    : y == 7 && x % 16 is < 6 or > 9 ? lit
                    : bed;
                c.SetRaw(x, y, col);
            }
        return c;
    }

    // ------------------------------------------------------------------ faces between levels

    /// <summary>Rows at the top of a face's art that are drawn once (the edge and what hangs from it); the rest repeats downward.</summary>
    public const int FaceCap = 16;

    /// <summary>Rows of a face's art that repeat down a tall face.</summary>
    public const int FaceBody = 48;

    /// <summary>
    /// The face of an earth bank under grass: the ledge's face carried down. A bright edge, a scalloped grass
    /// lip, then dirt with a broken strata line every six rows. 32 by <see cref="FaceCap"/> + <see cref="FaceBody"/>.
    /// </summary>
    public static PixelCanvas BankFace()
    {
        var c = new PixelCanvas(32, FaceCap + FaceBody);
        var edge = Rgb(160, 222, 122);
        var lip = Rgb(120, 200, 102);
        var lipShade = Rgb(70, 150, 82);
        var dirt = Rgb(178, 138, 90);
        var strata = Rgb(146, 108, 72);
        for (int x = 0; x < 32; x++)
        {
            int hang = x % 8 is 1 or 2 or 5 ? 4 : 3;
            for (int y = 0; y < c.Height; y++)
            {
                // Each strata line is broken in a different place, in a pattern that comes round with the body
                int course = y / 6 % 8, along = (x + course * 6) % 16;
                Color col = y == 0 ? edge
                    : y < hang ? lip
                    : y == hang ? lipShade
                    : y % 6 == 3 && along is < 6 or > 9 ? strata
                    : dirt;
                c.SetRaw(x, y, col);
            }
        }
        return c;
    }

    /// <summary>How many tiles of a face one width of the rock art covers: wide enough that its slabs don't repeat tile by tile.</summary>
    public const int RockFaceTiles = 2;

    /// <summary>
    /// A rock face in the boulder's shades: a light top edge, then beds of rock of uneven thickness lying one on
    /// the other. Each bed is lit along its top, with a few longer glints where it bulges, and overhangs the next
    /// in a band of shade whose darkest line is broken; a bed has one or two cracks, each running only part of
    /// the way down it. Long beds and few cracks: strata, not a wall of blocks.
    /// </summary>
    public static PixelCanvas RockFace()
    {
        const int w = 32 * RockFaceTiles;
        var c = new PixelCanvas(w, FaceCap + FaceBody);
        var top = Rgb(196, 192, 190);
        var lit = Rgb(176, 170, 166);
        var rock = Rgb(150, 146, 150);
        var deep = Rgb(104, 100, 112);
        var crack = Rgb(74, 70, 86);

        // Each bed: its height, where its cracks are, and where a glint lies on its upper part (start, length)
        (int Height, int[] Cracks, (int At, int Length)[] Glints)[] beds =
        {
            // The cap, 16 rows
            (6, new[] { 41 }, new[] { (8, 9), (50, 6) }),
            (10, new[] { 14, 55 }, new[] { (24, 11) }),
            // The body, 48 rows, which repeats down a tall face
            (9, new[] { 30 }, new[] { (4, 8), (44, 10) }),
            (12, new[] { 9, 47 }, new[] { (18, 12), (56, 5) }),
            (8, new[] { 24, 61 }, new[] { (34, 9) }),
            (10, new[] { 5 }, new[] { (14, 10), (40, 12) }),
            (9, new[] { 19, 52 }, new[] { (28, 8), (58, 4) })
        };

        int y0 = 0;
        for (int k = 0; k < beds.Length; k++)
        {
            var (height, cracks, glints) = beds[k];
            for (int row = 0; row < height; row++)
                for (int x = 0; x < w; x++)
                {
                    // The shade under the bed above ends in a dark line that is there for a stretch and gone for a stretch
                    bool underLine = row == height - 1 && (x + k * 13) % 21 < 13;
                    Color col = y0 + row == 0 ? top
                        : row == 0 ? lit
                        : underLine ? crack
                        : row >= height - 2 ? deep
                        : rock;

                    // A glint two rows under the bed's top; a crack from a third of the way down, leaning a texel
                    foreach (var (at, length) in glints)
                        if (row == 2 && x >= at && x < at + length) col = lit;
                    foreach (int at in cracks)
                    {
                        if (row < height / 3) continue;
                        int cx = at + (row >= height * 2 / 3 ? 1 : 0);
                        if (x == cx) col = crack;
                        else if (x == cx - 1 && row < height - 2) col = deep;
                    }
                    c.SetRaw(x, y0 + row, col);
                }
            y0 += height;
        }
        return c;
    }

    // ------------------------------------------------------------------ the Distortion World's islands

    /// <summary>How many tiles of an island's edge one width of its underside's art covers, so its points don't repeat tile by tile.</summary>
    public const int UndersideTiles = 4;

    /// <summary>Rows of the underside's art: drawn once from an island's edge down to the void's depth.</summary>
    public const int UndersideRows = 48;

    /// <summary>
    /// The underside of one of the Distortion World's islands (style guide, "The Distortion World"): its stone's
    /// edge and two beds of rock in the island's grey-violet, then rock that darkens in two flat steps as it hangs
    /// down and ends in points of uneven length, each outlined in the darkest shade. Below the points the art is
    /// empty, so the void shows there. 32 × <see cref="UndersideTiles"/> by <see cref="UndersideRows"/>.
    /// </summary>
    public static PixelCanvas IslandUnderside()
    {
        const int w = 32 * UndersideTiles, h = UndersideRows;
        var c = new PixelCanvas(w, h);
        var top = Rgb(164, 154, 180);
        var lit = Rgb(138, 128, 156);
        var rock = Rgb(112, 102, 130);
        var deep = Rgb(86, 78, 104);
        var lower = Rgb(92, 84, 112);
        var lowest = Rgb(70, 62, 90);
        var ink = Rgb(52, 46, 70);

        // Where the rock ends in each column: a wavy line with points hanging from it (centre, half width, depth)
        (int At, int Half, int Depth)[] points =
        {
            (9, 6, 17), (24, 4, 9), (38, 8, 21), (55, 5, 12), (70, 9, 19), (86, 4, 10), (99, 7, 22), (117, 6, 14)
        };
        int Bottom(int x)
        {
            int bottom = 22 + (x * 7 / 11 % 5 == 0 ? 1 : 0) + (x / 13 % 3);
            foreach (var (at, half, depth) in points)
            {
                int d = Math.Min(Math.Abs(x - at), Math.Abs(x - at - w));
                if (d <= half) bottom = Math.Max(bottom, 22 + depth * (half - d) / half);
            }
            return Math.Min(h - 1, bottom);
        }

        for (int x = 0; x < w; x++)
        {
            int bottom = Bottom(x);
            for (int y = 0; y <= bottom; y++)
            {
                // The edge, two beds each lit along its top and shaded under, then the hanging rock in two steps
                Color col = y == 0 ? top
                    : y is 1 or 8 ? lit
                    : y is 6 or 7 or 13 or 14 ? deep
                    : y < 16 ? rock
                    : y < 30 ? lower
                    : lowest;
                // A crack down each bed here and there
                if (y is > 2 and < 6 && (x + 5) % 23 == 0 || y is > 9 and < 13 && (x + 14) % 29 == 0) col = deep;
                // The points' outline: the last texel of each column, and the side of a point where it steps down
                bool stepped = Bottom((x + w - 1) % w) < y || Bottom((x + 1) % w) < y;
                if (y == bottom || (y > 16 && stepped)) col = ink;
                c.SetRaw(x, y, col);
            }
        }
        return c;
    }

    // ------------------------------------------------------------------ falling water

    /// <summary>
    /// A waterfall's sheet, 32 by 32 and repeating: streaks of light and of foam running the way the water
    /// falls, each a different length and broken at a different height, with a dark line down one side.
    /// </summary>
    public static PixelCanvas Waterfall()
    {
        var c = new PixelCanvas(32, 32);
        var water = Rgb(76, 146, 214);
        var light = Rgb(150, 206, 246);
        var foam = Rgb(236, 246, 255);
        var dark = Rgb(46, 104, 186);
        c.Fill(water);
        (int X, int Start, int Length, bool Foam)[] streaks =
        {
            (2, 3, 14, false), (6, 20, 9, true), (9, 8, 18, false), (13, 28, 12, false), (16, 1, 10, true),
            (20, 14, 15, false), (24, 25, 11, true), (27, 6, 13, false), (30, 18, 9, false)
        };
        foreach (var (x, start, length, white) in streaks)
            for (int i = 0; i < length; i++)
            {
                int y = (start + i) % 32;
                c.SetRaw(x, y, white ? foam : light);
                c.SetRaw((x + 31) % 32, y, dark);
            }
        return c;
    }
}
