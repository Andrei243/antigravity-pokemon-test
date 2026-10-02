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
}
