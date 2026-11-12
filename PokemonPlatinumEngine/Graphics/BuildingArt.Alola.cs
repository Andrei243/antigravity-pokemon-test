using System;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// The art of Alola's last two buildings (plan 23; style guide, "Kinds of building"): the Battle Tree's trunk, roots
/// and crown, and the geothermal power plant's pipes, valves and cooling tower. Flat shades, one-texel bevels and
/// outlines that are never near-black, as everywhere else. No GPU calls.
/// </summary>
internal static partial class BuildingArt
{
    private static readonly Tone Bark = Tone.Of(116, 80, 54, 146, 104, 70, 86, 58, 42);
    private static readonly Color BarkOpening = Rgb(70, 48, 40);
    private static readonly Color LeafDark = Rgb(40, 92, 56), LeafBase = Rgb(62, 128, 68), LeafLit = Rgb(96, 166, 84), LeafTip = Rgb(146, 204, 108);
    private static readonly Color BoardCream = Rgb(240, 226, 190);
    private static readonly Tone PipeSteel = Tone.Of(168, 176, 190, 206, 212, 222, 112, 120, 138);
    private static readonly Color Flange = Rgb(92, 98, 116);
    private static readonly Color ValveRed = Rgb(196, 62, 52);
    private static readonly Tone Concrete = Tone.Of(200, 198, 192, 224, 222, 216, 150, 148, 152);
    private static readonly Color ConcreteRing = Rgb(184, 182, 178);
    private static readonly Color SteamBase = Rgb(222, 228, 236), SteamShade = Rgb(188, 198, 214);

    /// <summary>A fixed scramble of a number, for patterns that look irregular and are the same every time.</summary>
    private static int Scramble(int n)
    {
        unchecked
        {
            uint h = (uint)n * 2654435761u;
            h ^= h >> 15;
            h *= 2246822519u;
            return (int)((h ^ (h >> 13)) & 0x7FFFFFFF);
        }
    }

    // ------------------------------------------------------------------ the Battle Tree

    /// <summary>Bark: upright plates 10 to 18 texels wide, a light line on each one's left and a groove on its right, with a crack across now and then.</summary>
    public static void PaintBark(PixelCanvas c, int seed)
    {
        int w = c.Width, h = c.Height;
        c.Rect(0, 0, w, h, Bark.Base);
        int x = 0, i = seed;
        while (x < w)
        {
            int pw = 10 + Scramble(i) % 9;
            c.VLine(x, 0, h, Bark.Light);
            c.VLine(Math.Min(w - 1, x + pw - 1), 0, h, Bark.Dark);
            for (int y = 6 + Scramble(i * 7 + 3) % 23; y < h - 2; y += 26 + Scramble(i * 13 + y) % 14)
            {
                c.HLine(x + 2, y, Math.Min(pw - 4, w - x - 3), Bark.Dark);
                c.HLine(x + 2, y + 1, Math.Min(pw - 4, w - x - 3), Bark.Light);
            }
            x += pw;
            i++;
        }
    }

    /// <summary>
    /// The trunk's front: bark, with the door as an arch cut into it at the foot of the face's middle (a pair of
    /// wooden doors under a lit fanlight), a board reading PARTNERS TREE over the arch (plan 37 · FC3: the approval sheet's 24) and a lantern either side.
    /// </summary>
    public static void PaintTrunkFront(PixelCanvas c, int seed)
    {
        int w = c.Width, h = c.Height, cx = w / 2;
        PaintBark(c, seed);

        // The arch: a rim of light bark round an opening 36 wide whose head is a half circle
        const int aw = 36, legs = 30;
        int r = aw / 2, top = h - legs - r;
        void Arch(int grow, Color col)
        {
            float rr = r + grow;
            for (int y = top - grow; y < h; y++)
            {
                float half = y >= top + r ? rr : MathF.Sqrt(Math.Max(0f, rr * rr - (top + r - y - 0.5f) * (top + r - y - 0.5f)));
                int hw = (int)MathF.Round(half);
                if (hw > 0) c.HLine(cx - hw, y, hw * 2, col);
            }
        }
        Arch(3, Bark.Dark);
        Arch(2, Bark.Light);
        Arch(0, BarkOpening);

        // Two wooden doors with a panel each, and the fanlight over them
        int doorTop = h - 34;
        Pix.Raised(c, cx - 15, doorTop, 15, 34, DoorWood);
        Pix.Raised(c, cx, doorTop, 15, 34, DoorWood);
        Pix.Sunken(c, cx - 12, doorTop + 4, 9, 12, DoorWood);
        Pix.Sunken(c, cx + 3, doorTop + 4, 9, 12, DoorWood);
        Pix.Sunken(c, cx - 12, doorTop + 19, 9, 11, DoorWood);
        Pix.Sunken(c, cx + 3, doorTop + 19, 9, 11, DoorWood);
        c.Rect(cx - 4, doorTop + 17, 2, 3, Brass.Light);
        c.Rect(cx + 2, doorTop + 17, 2, 3, Brass.Light);
        Glass(c, cx - 8, doorTop - 9, 16, 6, ArtSheet.PublicLight);

        // The board over the arch, its name in cream
        const string name = "PARTNERS TREE";
        int bw = Pix.TextWidth(name) + 10, by = top - 3 - 16;
        if (by > 2)
        {
            Pix.Raised(c, cx - bw / 2, by, bw, 13, Boards);
            Pix.Border(c, cx - bw / 2, by, bw, 13, Bark.Dark);
            Pix.Text(c, cx - Pix.TextWidth(name) / 2, by + 3, name, BoardCream);
        }

        // A lantern either side of the arch
        Lantern(c, cx - r - 13, h - 40);
        Lantern(c, cx + r + 8, h - 40);
    }

    /// <summary>
    /// The roots at the trunk's foot, on an upright card: three roots of bark, thick where they leave the trunk (at the
    /// card's right edge) and thinning as they reach out and down to the ground. <paramref name="mirror"/> for the east side.
    /// </summary>
    public static void PaintRoots(PixelCanvas c, bool mirror)
    {
        int w = c.Width, h = c.Height;
        for (int root = 0; root < 3; root++)
        {
            float sx = w - 1, sy = 3 + root * 6;
            float ex = w - 46 - root * 26, ey = h - 2;
            float thick0 = 10 - root * 1.5f, thick1 = 4;
            int steps = (int)(sx - ex);
            for (int pass = 0; pass < 3; pass++)
                for (int k = 0; k <= steps; k++)
                {
                    float t = k / (float)steps;
                    // Out from the trunk first, then down to the ground: the root arches over and settles
                    float x = sx + (ex - sx) * t;
                    float y = sy + (ey - sy) * (t * t);
                    int size = Math.Max(3, (int)MathF.Round(thick0 + (thick1 - thick0) * t));
                    int px = (int)MathF.Round(x - size / 2f), py = (int)MathF.Round(y - size / 2f);
                    if (pass == 0) Pix.Disc(c, px, py + 1, size, Bark.Dark);
                    else if (pass == 1) Pix.Disc(c, px, py, size - 1, Bark.Base);
                    else if (size >= 6) c.HLine(px + 1, py + 1, Math.Max(2, size / 2 - 1), Bark.Light);
                }
        }
        Pix.Outline(c);
        if (mirror) c.MirrorHorizontal();
    }

    /// <summary>One clump of leaves: a dark disc, the base over most of it (leaving a dark crescent lower right), and a light crescent upper left with a tip.</summary>
    private static void LeafClump(PixelCanvas c, int cx, int cy, int size)
    {
        int x = cx - size / 2, y = cy - size / 2;
        Pix.Disc(c, x, y, size, LeafDark);
        Pix.Disc(c, x - 1, y - 2, size - 2, LeafBase);
        int lit = Math.Max(5, size * 3 / 5);
        Pix.Disc(c, x + 1, y, lit, LeafLit);
        Pix.Disc(c, x + 3, y + 3, lit - 1, LeafBase);
        if (size >= 14) c.Rect(x + size / 4, y + 2, 3, 2, LeafTip);
    }

    /// <summary>
    /// A layer of the crown, on an upright card: clumps of leaves in rows from the top down, each row overlapping the
    /// one behind it. A <paramref name="dome"/> is round on top (the back of the crown); otherwise the layer is a band as
    /// wide as the card (the fringe in front, whose foot hangs ragged). <paramref name="solid"/> fills the card first, for
    /// a roof of leaves seen from above.
    /// </summary>
    public static void PaintCrown(PixelCanvas c, int seed, bool dome, bool solid = false, int vineRows = 0, int clearFrom = -1, int clearTo = -1)
    {
        int w = c.Width, h = c.Height;
        if (solid) c.Rect(0, 0, w, h, LeafBase);
        const int pitch = 18;
        int rows = 0;
        int band = h - vineRows;
        for (int y = 12; y < band + 10; y += 13, rows++)
        {
            for (int x = (rows % 2) * (pitch / 2) - 4; x < w + 10; x += pitch)
            {
                int n = Scramble(seed * 977 + rows * 131 + x);
                int size = 18 + n % 9;
                int cx = x + n / 9 % 5 - 2, cy = y + n / 45 % 5 - 2;
                if (dome)
                {
                    // The crown's outline: high in the middle, falling away to its sides
                    float u = (cx - w / 2f) / (w / 2f);
                    float edge = h * (0.06f + 0.5f * u * u);
                    if (cy - size / 2 < edge || Math.Abs(u) > 1.02f) continue;
                }
                else if (!solid)
                {
                    // The fringe's ragged foot: some clumps hang lower than others
                    int foot = band - 4 - n / 225 % 8;
                    if (cy + size / 2 > foot || cx < size / 2 - 6 || cx > w - size / 2 + 6) continue;
                }
                LeafClump(c, cx, Math.Min(cy, h - size / 2 - 1), size);
            }
        }

        // Vines hanging from the fringe's foot over the wall below, kept off the trunk (clearFrom to clearTo)
        if (vineRows > 0)
            for (int x = 6, k = 0; x < w - 6; x += 20 + Scramble(seed * 31 + k) % 12, k++)
            {
                if (x >= clearFrom - 8 && x <= clearTo + 8) continue;
                int top = band - 14, length = 12 + Scramble(seed * 53 + k) % (vineRows - 10);
                c.Rect(x, top, 2, length, LeafDark);
                for (int y = top + 8; y < top + length - 2; y += 7)
                {
                    bool left = (y / 7 + k) % 2 == 0;
                    c.Rect(left ? x - 3 : x + 2, y, 3, 3, LeafBase);
                    c.Rect(left ? x - 3 : x + 3, y, 2, 1, LeafLit);
                }
                c.Rect(x - 1, top + length - 2, 4, 3, LeafBase);
            }
        if (!solid) Pix.Outline(c);
    }

    // ------------------------------------------------------------------ the power plant

    /// <summary>
    /// A pipe's side, shaded round as a cylinder seen side on: its lit edge along its top (or left), the steel, then a
    /// shaded band and a dark rim along its foot (or right); a flange every 32 texels.
    /// </summary>
    public static void PaintPipe(PixelCanvas c, bool upright)
    {
        int w = c.Width, h = c.Height, across = upright ? w : h, along = upright ? h : w;
        for (int a = 0; a < across; a++)
        {
            var col = a < across / 5 ? PipeSteel.Light : a < across * 3 / 5 ? PipeSteel.Base : a < across - 1 ? PipeSteel.Dark : Flange;
            if (upright) c.VLine(a, 0, h, col);
            else c.HLine(0, a, w, col);
        }
        for (int f = 14; f + 3 <= along; f += 32)
        {
            if (upright) { c.Rect(0, f, w, 3, Flange); c.HLine(0, f, w, PipeSteel.Base); }
            else { c.Rect(f, 0, 3, h, Flange); c.VLine(f, 0, h, PipeSteel.Base); }
        }
    }

    /// <summary>A pipe seen from above: its lit crown along the middle, the steel either side.</summary>
    public static void PaintPipeTop(PixelCanvas c)
    {
        int w = c.Width, h = c.Height;
        c.Rect(0, 0, w, h, PipeSteel.Base);
        c.Rect(0, h / 2 - 2, w, 4, PipeSteel.Light);
        for (int f = 14; f + 3 <= w; f += 32) c.Rect(f, 0, 3, h, Flange);
    }

    private static readonly Color StackWhite = Rgb(236, 236, 232);

    /// <summary>
    /// A steel stack's side: shaded round (lit left, dark right), a seam every 24 rows, and red and white bands ten rows
    /// deep at its top.
    /// </summary>
    public static void PaintSteelStack(PixelCanvas c)
    {
        int w = c.Width, h = c.Height;
        for (int y = 0; y < h; y++)
        {
            bool banded = y < 40;
            var tone = banded ? Tone.Of(y / 10 % 2 == 0 ? ValveRed : StackWhite) : PipeSteel;
            for (int x = 0; x < w; x++)
                c.Set(x, y, x < w / 4 ? tone.Light : x < w * 3 / 4 ? tone.Base : tone.Dark);
        }
        for (int y = 46; y < h - 2; y += 24) c.HLine(0, y, w, Flange);
        c.HLine(0, 0, w, Flange);
    }

    /// <summary>The top of a steel stack: a rim round its dark mouth.</summary>
    public static void PaintSteelStackTop(PixelCanvas c)
    {
        Pix.Raised(c, 0, 0, c.Width, c.Height, PipeSteel);
        c.Rect(3, 3, c.Width - 6, c.Height - 6, Rgb(72, 74, 88));
    }

    /// <summary>A valve wheel on its pipe, on a card 14 texels square: a red ring, a cross of spokes and a hub.</summary>
    public static void PaintValve(PixelCanvas c)
    {
        int s = Math.Min(c.Width, c.Height);
        Pix.Disc(c, 0, 0, s, ValveRed);
        var clear = new Color(0, 0, 0, 0);
        float r = s / 2f - 2.5f;
        for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float dx = x + 0.5f - s / 2f, dy = y + 0.5f - s / 2f;
                bool spoke = Math.Abs(dx) < 1f || Math.Abs(dy) < 1f;
                if (dx * dx + dy * dy < r * r && !spoke) c.SetRaw(x, y, clear);
            }
        c.Rect(s / 2 - 2, s / 2 - 2, 4, 4, PixelCanvas.Shadow(ValveRed, 0.3f));
        c.Rect(s / 2 - 2, s / 2 - 2, 2, 2, PixelCanvas.Light1(ValveRed, 0.3f));
        Pix.Outline(c);
    }

    /// <summary>The height of the cooling tower's card above its shell's foot that the plume of steam takes, in texels.</summary>
    public const int SteamRows = 44;

    /// <summary>
    /// A cooling tower on an upright card: a shell of pale concrete wide at its foot, curving in to its waist at two
    /// thirds of its height and out again to its lip, rings every 16 rows, an orange band near the lip, its dark mouth
    /// seen over it, and a still plume of steam drifting east as it rises.
    /// </summary>
    public static void PaintCoolingTower(PixelCanvas c)
    {
        int w = c.Width, h = c.Height, cx = w / 2;
        int lipY = SteamRows, tall = h - lipY;
        // Its lip, its waist (a third of the way down) and its foot, as shares of the card's width
        float lipHalf = w * 0.36f, waistHalf = w * 0.31f, footHalf = w * 0.46f;
        float HalfAt(int t)
        {
            float waist = tall / 3f;
            if (t < waist) { float k = t / waist; return lipHalf - (lipHalf - waistHalf) * (2f * k - k * k); }
            float u = (t - waist) / (tall - waist);
            return waistHalf + (footHalf - waistHalf) * MathF.Pow(u, 1.6f);
        }
        for (int t = 0; t < tall; t++)
        {
            int y = lipY + t, hw = (int)MathF.Round(HalfAt(t));
            bool band = t >= 8 && t < 14;
            var tone = band ? Tone.Of(PlantOrange) : Concrete;
            for (int x = cx - hw; x < cx + hw; x++)
            {
                float u = (x + 0.5f - cx) / hw;
                var col = u < -0.62f ? tone.Light : u > 0.38f ? tone.Dark : tone.Base;
                if (!band && t % 16 == 8 && u >= -0.62f && u <= 0.38f) col = ConcreteRing;
                c.Set(x, y, col);
            }
        }
        // The mouth, seen from above over the lip: a rim and the dark inside
        int lip = (int)MathF.Round(HalfAt(0));
        c.FlatEllipse(cx, lipY, lip, 7, Concrete.Light);
        c.FlatEllipse(cx, lipY, lip - 4, 5, Rgb(92, 94, 104));

        // The plume: puffs rising from the mouth and drifting east, growing as they go
        for (int k = 0; k < 4; k++)
        {
            int size = 26 + k * 6;
            int px = cx - size / 2 + k * 9 - 14, py = lipY - 8 - k * 9 - size / 2;
            Pix.Disc(c, px + 2, py + 3, size, SteamShade);
            Pix.Disc(c, px, py, size - 2, SteamBase);
        }
        Pix.Outline(c);
    }
}
