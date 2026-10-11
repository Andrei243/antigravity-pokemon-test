using System;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// Pixel art for the rooms of the Pokémon League (plan 01 · M9, part 2d; style guide, "The League's rooms"): the faces
/// its floors stand on, its doors and the gate that shuts behind a challenger, the platform of its lifts, the lava
/// under Flint's causeway, the carpets, the Champion's piers and the Hall of Fame's machine. Flat shades in placed
/// shapes, nothing near white, no GPU calls.
/// </summary>
internal static class LeagueArt
{
    private static Color Rgb(int r, int g, int b) => new(r, g, b, 255);

    /// <summary>The gold of every door, rail and rim of the League.</summary>
    public static readonly Tone Gold = Tone.Of(226, 190, 96, 250, 226, 150, 170, 130, 60);

    /// <summary>The dark bronze of the doors' leaves and the lifts' plates.</summary>
    public static readonly Tone Bronze = Tone.Of(70, 78, 116, 104, 114, 156, 44, 50, 80);

    public static readonly Tone LiftPlate = Tone.Of(92, 100, 140, 124, 134, 176, 60, 66, 100);
    public static readonly Tone Marble = Tone.Of(228, 226, 216, 240, 238, 228, 186, 180, 164);
    public static readonly Tone Steel = Tone.Of(60, 66, 96, 92, 100, 136, 38, 42, 66);

    /// <summary>The dark seen through an open door.</summary>
    public static readonly Color Beyond = Rgb(20, 18, 28);

    /// <summary>The stone a room's floors stand on.</summary>
    public enum Stone { Slate, Basalt, Marble, Sandstone }

    private static Tone StoneTone(Stone stone) => stone switch
    {
        Stone.Basalt => Tone.Of(70, 56, 62, 96, 78, 84, 46, 36, 44),
        Stone.Marble => Tone.Of(196, 190, 174, 218, 212, 198, 156, 148, 132),
        Stone.Sandstone => Tone.Of(196, 160, 112, 220, 188, 140, 150, 116, 78),
        _ => Tone.Of(96, 104, 134, 122, 130, 160, 66, 72, 100)
    };

    /// <summary>
    /// The face a room's floors stand on, 32 by <see cref="NatureArt.FaceCap"/> + <see cref="NatureArt.FaceBody"/>: a
    /// pale edge and a band of gold in the cap, then courses of dressed stone twelve rows high, their joints half a
    /// block along from one course to the next, each block lit along its top and left and shaded at its foot and right.
    /// </summary>
    public static PixelCanvas Face(Stone stone)
    {
        var tone = StoneTone(stone);
        var c = new PixelCanvas(32, NatureArt.FaceCap + NatureArt.FaceBody);
        var edge = stone == Stone.Basalt ? Rgb(150, 128, 122) : Rgb(206, 198, 176);
        for (int y = 0; y < c.Height; y++)
            for (int x = 0; x < 32; x++)
            {
                Color col;
                if (y == 0) col = edge;
                else if (y <= 3) col = y == 1 ? Gold.Light : y == 3 ? Gold.Dark : Gold.Base;
                else if (y == 4) col = tone.Deep;
                else
                {
                    // Courses counted from the cap's foot, so the body repeats whole (four courses to its 48 rows)
                    int from = y - NatureArt.FaceCap + 48, course = from / 12, row = from % 12;
                    int along = (x + course % 2 * 16) % 32;
                    col = row == 11 || along == 31 ? tone.Dark : row == 0 || along == 0 ? tone.Light : tone.Base;
                }
                c.SetRaw(x, y, col);
            }
        return c;
    }

    /// <summary>
    /// A door in the back wall, as wide and tall as the canvas (30 by 56): a frame of gold with a round head; open, the
    /// dark of the room beyond with two steps of shade at its jambs; shut, two leaves of bronze in sunken panels,
    /// studded with gold, with a bar across them.
    /// </summary>
    public static void PaintDoor(PixelCanvas c, bool open)
    {
        int w = c.Width, h = c.Height;
        float r = w / 2f;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float dx = x + 0.5f - r, dy = y + 0.5f - r;
                // Round above the springing of the arch, straight below it
                float d = y < r ? MathF.Sqrt(dx * dx + dy * dy) : MathF.Abs(dx);
                if (d > r) continue;
                Color col;
                if (d > r - 1f) col = Gold.Dark;
                else if (d > r - 3f) col = dx < 0 ? Gold.Light : Gold.Base;
                else if (d > r - 4f) col = Gold.Dark;
                else if (open) col = d > r - 6f ? Rgb(52, 46, 66) : d > r - 8f ? Rgb(34, 30, 46) : Beyond;
                else col = Bronze.Base;
                c.SetRaw(x, y, col);
            }
        // The sill
        c.Rect(0, h - 2, w, 2, Gold.Dark);
        c.HLine(1, h - 2, w - 2, Gold.Base);
        if (open) return;

        // The leaves: a seam down the middle, three sunken panels to a leaf, a stud at each panel's corners, a bar
        int mid = w / 2;
        c.VLine(mid - 1, 5, h - 7, Bronze.Dark);
        c.VLine(mid, 5, h - 7, Bronze.Light);
        foreach (int px in new[] { 6, mid + 2 })
            foreach (int py in new[] { 13, 27, 41 })
            {
                Pix.Sunken(c, px, py, mid - 8, 11, Bronze);
                c.SetRaw(px + 1, py + 1, Gold.Base);
                c.SetRaw(px + mid - 10, py + 1, Gold.Base);
            }
        c.Rect(5, 24, w - 10, 3, Gold.Base);
        c.HLine(5, 24, w - 10, Gold.Light);
        c.HLine(5, 26, w - 10, Gold.Dark);
        Pix.Disc(c, mid - 3, 22, 7, Gold.Dark);
        Pix.Disc(c, mid - 2, 23, 5, Gold.Light);
    }

    /// <summary>The top of the gate that shuts the front wall behind a challenger: bronze between two rails of gold, studded.</summary>
    public static void PaintGateTop(PixelCanvas c)
    {
        int w = c.Width, h = c.Height;
        c.Rect(0, 0, w, h, Bronze.Base);
        c.Rect(0, 0, w, 3, Gold.Base);
        c.HLine(0, 0, w, Gold.Light);
        c.Rect(0, h - 3, w, 3, Gold.Base);
        c.HLine(0, h - 1, w, Gold.Dark);
        for (int x = 4; x < w - 2; x += 6) c.Rect(x, h / 2 - 1, 2, 2, Gold.Light);
        c.VLine(0, 0, h, Gold.Light);
        c.VLine(w - 1, 0, h, Gold.Dark);
    }

    /// <summary>The face of the gate toward the room: upright bars of bronze under a rail of gold.</summary>
    public static void PaintGateFront(PixelCanvas c)
    {
        int w = c.Width, h = c.Height;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                c.SetRaw(x, y, y < 3 ? (y == 0 ? Gold.Light : y == 2 ? Gold.Dark : Gold.Base) : x % 6 == 0 ? Bronze.Light : x % 6 == 5 ? Bronze.Dark : Bronze.Base);
        c.HLine(0, h - 1, w, Bronze.Deep);
    }

    /// <summary>
    /// The top of a lift's platform, three tiles by two (96 by 64): plates of bronze in a rim of gold, chevrons of gold
    /// along its north and south edges, and at its middle a ring of gold with a ball's line through it.
    /// </summary>
    public static void PaintLiftTop(PixelCanvas c)
    {
        int w = c.Width, h = c.Height;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                Color col;
                bool rim = x < 3 || y < 3 || x >= w - 3 || y >= h - 3;
                if (rim) col = x == 0 || y == 0 ? Gold.Light : x == w - 1 || y == h - 1 ? Gold.Dark : Gold.Base;
                else if (y < 9 || y >= h - 9)
                {
                    // Chevrons pointing up the shaft
                    int k = (x + (y < 9 ? y : h - 1 - y)) % 10;
                    col = k < 4 ? Gold.Base : Bronze.Dark;
                }
                else
                {
                    // Plates sixteen texels square
                    int px = (x - 3) % 18, py = (y - 9) % 23;
                    col = px == 17 || py == 22 ? LiftPlate.Dark : px == 0 || py == 0 ? LiftPlate.Light : LiftPlate.Base;
                }
                c.SetRaw(x, y, col);
            }
        // The emblem: a ring and a ball's line
        float cx = w / 2f, cy = h / 2f;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float d = MathF.Sqrt((x + 0.5f - cx) * (x + 0.5f - cx) + (y + 0.5f - cy) * (y + 0.5f - cy));
                if (d < 15f && d >= 12f) c.SetRaw(x, y, x + y < cx + cy ? Gold.Light : Gold.Base);
                else if (d < 12f) c.SetRaw(x, y, MathF.Abs(y + 0.5f - cy) < 1.5f ? Gold.Base : d < 4f ? Gold.Light : Bronze.Base);
            }
    }

    /// <summary>The side of a lift's platform: bronze under a line of gold.</summary>
    public static void PaintLiftSide(PixelCanvas c)
    {
        Pix.Raised(c, 0, 0, c.Width, c.Height, Bronze);
        c.Rect(0, 0, c.Width, 2, Gold.Base);
        c.HLine(0, 0, c.Width, Gold.Light);
    }

    /// <summary>A rail of gold up a shaft's wall, as tall as the canvas: lit down its left, shaded down its right, a collar every sixteen rows.</summary>
    public static void PaintRail(PixelCanvas c)
    {
        for (int y = 0; y < c.Height; y++)
            for (int x = 0; x < c.Width; x++)
                c.SetRaw(x, y, y % 16 == 15 ? Gold.Dark : x == 0 ? Gold.Light : x == c.Width - 1 ? Gold.Dark : Gold.Base);
    }

    private static readonly Color Molten = Rgb(226, 96, 36), Deep = Rgb(200, 70, 30), Vein = Rgb(250, 170, 60), Spark = Rgb(255, 214, 110),
        Crust = Rgb(120, 40, 30), CrustEdge = Rgb(84, 28, 26);

    /// <summary>The lava's pattern repeats every two tiles each way.</summary>
    public const int LavaTile = 64;

    // Plates of crust: each a few rectangles that make one irregular slab, in texels of the pattern
    private static readonly (int X, int Y, int W, int H)[][] Plates =
    {
        new[] { (4, 6, 14, 5), (7, 4, 8, 9), (2, 8, 5, 2) },
        new[] { (36, 3, 10, 4), (39, 1, 12, 5), (44, 5, 9, 4) },
        new[] { (22, 20, 16, 6), (26, 17, 9, 12), (34, 23, 7, 5) },
        new[] { (50, 30, 9, 8), (47, 33, 14, 4) },
        new[] { (6, 38, 12, 7), (10, 35, 10, 12), (4, 41, 4, 3) },
        new[] { (30, 46, 15, 5), (33, 43, 8, 10) },
        new[] { (52, 52, 8, 6), (49, 54, 13, 3) },
        new[] { (16, 56, 9, 5), (19, 54, 5, 9) }
    };

    /// <summary>
    /// A quarter of the lava's pattern, a tile of 32 by 32 (<paramref name="qx"/>, <paramref name="qy"/>: which quarter of
    /// the 64 by 64 that repeats): molten orange with pools of a deeper red, veins that brighten and fade, a few sparks,
    /// and plates of dark crust, lit along their upper edge by the glow under them, that rock a texel or two to and fro.
    /// Four frames to the round.
    /// </summary>
    public static void PaintLava(PixelCanvas c, int frame, int qx, int qy)
    {
        var whole = new PixelCanvas(LavaTile, LavaTile);
        void Put(int x, int y, Color col) => whole.SetRaw(((x % LavaTile) + LavaTile) % LavaTile, ((y % LavaTile) + LavaTile) % LavaTile, col);
        void Block(int x, int y, int w, int h, Color col)
        {
            for (int py = 0; py < h; py++)
                for (int px = 0; px < w; px++) Put(x + px, y + py, col);
        }
        Block(0, 0, LavaTile, LavaTile, Molten);
        // Pools of deeper red, in steps
        foreach (var (x, y, w, h) in new[] { (18, 6, 14, 8), (20, 4, 9, 12), (40, 14, 16, 9), (44, 11, 8, 15), (0, 24, 12, 9), (2, 21, 7, 14), (22, 34, 13, 7), (42, 42, 9, 8), (56, 4, 8, 12) })
            Block(x, y, w, h, Deep);
        // Veins: short bright runs that step once along their way; the brightest at the middle frames
        var vein = frame is 1 or 2 ? Vein : PixelCanvas.Mix(Molten, Vein, 0.6f);
        foreach (var (x, y, len) in new[] { (20, 13, 10), (48, 22, 9), (2, 30, 11), (36, 37, 8), (14, 50, 12), (54, 46, 8), (30, 60, 10), (58, 14, 7) })
            for (int i = 0; i < len; i++) Put(x + i, y + (i > len / 2 ? 1 : 0), vein);
        foreach (var (x, y) in new[] { (24, 13), (50, 22), (6, 30), (38, 37), (18, 50), (56, 46), (60, 14) })
            if ((x + frame) % 2 == 0) Put(x, y, Spark);
        // The crust, each plate rocking its own way
        for (int p = 0; p < Plates.Length; p++)
        {
            int rock = new[] { 0, 1, 2, 1 }[(frame + p) % 4] * (p % 2 == 0 ? 1 : -1);
            foreach (var (x, y, w, h) in Plates[p]) Block(x + rock - 1, y - 1, w + 2, h + 2, CrustEdge);
            foreach (var (x, y, w, h) in Plates[p]) Block(x + rock, y - 1, w, 1, Vein);
            foreach (var (x, y, w, h) in Plates[p]) Block(x + rock, y, w, h, Crust);
        }
        for (int y = 0; y < c.Height; y++)
            for (int x = 0; x < c.Width; x++) c.SetRaw(x, y, whole.Get(qx * 32 + x, qy * 32 + y));
    }

    /// <summary>
    /// A stretch of carpet as wide as the canvas and a tile long: its field, a border of gold three texels in from each
    /// side, and a small diamond at its middle; an end carries the border across.
    /// </summary>
    public static void PaintCarpet(PixelCanvas c, Tone field, bool top, bool bottom)
    {
        int w = c.Width, h = c.Height;
        c.Rect(0, 0, w, h, field.Base);
        c.VLine(0, 0, h, field.Dark);
        c.VLine(w - 1, 0, h, field.Dark);
        c.Rect(3, 0, 2, h, Gold.Base);
        c.Rect(w - 5, 0, 2, h, Gold.Base);
        if (top)
        {
            c.HLine(0, 0, w, field.Dark);
            c.Rect(3, 3, w - 6, 2, Gold.Base);
            c.Rect(3, 0, 2, 3, field.Base);
            c.Rect(w - 5, 0, 2, 3, field.Base);
        }
        if (bottom)
        {
            c.HLine(0, h - 1, w, field.Dark);
            c.Rect(3, h - 5, w - 6, 2, Gold.Base);
            c.Rect(3, h - 3, 2, 3, field.Base);
            c.Rect(w - 5, h - 3, 2, 3, field.Base);
        }
        // The diamond
        int cx = w / 2, cy = h / 2;
        for (int i = -3; i <= 3; i++)
            c.HLine(cx - (3 - Math.Abs(i)), cy + i, 2 * (3 - Math.Abs(i)) + 1, i < 0 ? Gold.Light : Gold.Base);
    }

    /// <summary>A face of one of the Champion's piers: marble in sunken panels between a band of gold at head and foot.</summary>
    public static void PaintPier(PixelCanvas c)
    {
        int w = c.Width, h = c.Height;
        c.Rect(0, 0, w, h, Marble.Base);
        c.VLine(0, 0, h, Marble.Light);
        c.VLine(w - 1, 0, h, Marble.Dark);
        foreach (int y in new[] { 0, h - 6 })
        {
            c.Rect(0, y, w, 6, Gold.Base);
            c.HLine(0, y, w, Gold.Light);
            c.HLine(0, y + 5, w, Gold.Dark);
        }
        int panels = Math.Max(1, w / 26), pw = (w - 6) / panels;
        for (int i = 0; i < panels; i++) Pix.Sunken(c, 4 + i * pw, 10, pw - 3, h - 20, Marble);
    }

    /// <summary>The top of a pier: marble inside a line of gold.</summary>
    public static void PaintPierTop(PixelCanvas c)
    {
        c.Rect(0, 0, c.Width, c.Height, Marble.Base);
        Pix.Border(c, 0, 0, c.Width, c.Height, Gold.Base);
        Pix.Border(c, 3, 3, c.Width - 6, c.Height - 6, Marble.Dark);
    }

    /// <summary>The top of the Hall of Fame's machine: dark steel edged in gold, six sockets for Poké Balls in a ring.</summary>
    public static void PaintConsoleTop(PixelCanvas c)
    {
        int w = c.Width, h = c.Height;
        c.Rect(0, 0, w, h, Steel.Base);
        Pix.Border(c, 0, 0, w, h, Gold.Base);
        c.HLine(0, 0, w, Gold.Light);
        for (int i = 0; i < 6; i++)
        {
            double a = Math.PI * 2 * i / 6;
            int sx = (int)MathF.Round(w / 2f + (float)Math.Cos(a) * w * 0.3f) - 4, sy = (int)MathF.Round(h / 2f + (float)Math.Sin(a) * h * 0.3f) - 4;
            Pix.Disc(c, sx, sy, 9, Gold.Dark);
            Pix.Disc(c, sx + 1, sy + 1, 7, Steel.Dark);
            c.SetRaw(sx + 3, sy + 3, Steel.Light);
        }
        Pix.Disc(c, w / 2 - 3, h / 2 - 3, 7, Rgb(96, 170, 236));
    }

    /// <summary>
    /// A Poké Ball standing in one of the machine's sockets (plan 02 · S14; style guide, "The League's rooms"): the
    /// field's own ball, ten texels across in the middle of a card of twelve, outlined.
    /// </summary>
    public static void PaintMachineBall(PixelCanvas c)
    {
        Color red = Rgb(218, 62, 58), redLight = Rgb(244, 120, 104), redDark = Rgb(170, 42, 56);
        Color white = Rgb(240, 240, 236), whiteShade = Rgb(198, 200, 214), band = Rgb(58, 52, 72);
        float middle = c.Width / 2f;
        const float radius = 5f;
        for (int y = 0; y < c.Height; y++)
            for (int x = 0; x < c.Width; x++)
            {
                float u = (x + 0.5f - middle) / radius, v = (y + 0.5f - middle) / radius;
                if (u * u + v * v > 1f) continue;
                if (y == c.Height / 2 - 1 || y == c.Height / 2) c.Set(x, y, band);
                else if (v < 0f) c.Set(x, y, u + v * 1.1f < -0.72f ? redLight : u > 0.55f ? redDark : red);
                else c.Set(x, y, u + v > 0.62f ? whiteShade : white);
            }
        // The button: two texels of white on the band
        c.Set(c.Width / 2 - 1, c.Height / 2 - 1, white);
        c.Set(c.Width / 2, c.Height / 2 - 1, white);
        c.Set(c.Width / 2 - 1, c.Height / 2, white);
        c.Set(c.Width / 2, c.Height / 2, white);
        Pix.Outline(c);
    }

    /// <summary>The front of the machine's console: panels of steel under a line of gold, a row of small lights.</summary>
    public static void PaintConsoleFront(PixelCanvas c)
    {
        int w = c.Width, h = c.Height;
        Pix.Raised(c, 0, 0, w, h, Steel);
        c.Rect(0, 0, w, 2, Gold.Base);
        c.HLine(0, 0, w, Gold.Light);
        for (int x = 6; x < w - 6; x += 10)
        {
            Pix.Sunken(c, x, 6, 7, h - 10, Steel);
            c.SetRaw(x + 3, 8, x % 20 == 6 ? Rgb(110, 220, 150) : Rgb(96, 170, 236));
        }
    }

    /// <summary>The machine's screen: lit blue in a frame of gold, a ball's outline on it and lines of a record under.</summary>
    public static void PaintScreen(PixelCanvas c)
    {
        int w = c.Width, h = c.Height;
        var lit = Rgb(96, 170, 236);
        var deep = Rgb(52, 108, 190);
        c.Rect(0, 0, w, h, Gold.Base);
        c.HLine(0, 0, w, Gold.Light);
        c.VLine(0, 0, h, Gold.Light);
        c.HLine(0, h - 1, w, Gold.Dark);
        c.VLine(w - 1, 0, h, Gold.Dark);
        c.Rect(3, 3, w - 6, h - 6, deep);
        c.Rect(4, 4, w - 8, h - 8, lit);
        // A ball's outline at the left, and three lines of a record beside it
        float cx = 16.5f, cy = h / 2f;
        for (int y = 4; y < h - 4; y++)
            for (int x = 4; x < 30; x++)
            {
                float d = MathF.Sqrt((x + 0.5f - cx) * (x + 0.5f - cx) + (y + 0.5f - cy) * (y + 0.5f - cy));
                if (d < 9f && d >= 7.5f || d < 7.5f && MathF.Abs(y + 0.5f - cy) < 1f || d < 2.5f) c.SetRaw(x, y, Rgb(232, 244, 252));
            }
        for (int i = 0; i < 4; i++) c.Rect(32, 9 + i * 7, i % 2 == 0 ? w - 40 : w - 48, 2, Rgb(200, 228, 250));
    }

    /// <summary>A pilaster of gold, as tall as the canvas: fluted, with a capital and a base.</summary>
    public static void PaintPilaster(PixelCanvas c)
    {
        int w = c.Width, h = c.Height;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                c.SetRaw(x, y, y < 5 || y >= h - 5 ? (y == 0 || y == h - 5 ? Gold.Light : Gold.Base)
                    : x == 0 ? Gold.Light : x == w - 1 ? Gold.Dark : x % 3 == 1 ? Gold.Dark : Gold.Base);
    }
}
