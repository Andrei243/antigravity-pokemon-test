using System;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// The festivals' things (plan 40 · GE9; style guide, "Life", "Festivals"): cards of our own in the scenery's 32 texels a
/// tile, each outlined like every card, standing at the middle of the one tile they stand in the way on (the lanterns,
/// the bunting and the banner are strung two tiles wide and overhang their neighbours). What glows after dark (a
/// lantern's core, a bulb, a star) is marked lit with <see cref="ArtSheet.PublicLight"/> after the outline, and only its
/// core, so the rim keeps its colour. Painted in code, no GPU calls.
/// </summary>
internal static class FestivalArt
{
    /// <summary>Each kind's card, in texels.</summary>
    public const int LanternsWidth = 64, LanternsHeight = 54, MoonWidth = 32, MoonHeight = 64, PineWidth = 36, PineHeight = 64,
        TreeWidth = 40, TreeHeight = 72, ArchWidth = 56, ArchHeight = 58, StallWidth = 40, StallHeight = 44, BellsWidth = 52,
        BellsHeight = 54, BuntingWidth = 64, BuntingHeight = 52, BasketWidth = 20, BasketHeight = 16, BannerWidth = 56,
        BannerHeight = 50, HeartWidth = 14, HeartHeight = 26;

    private static Color Rgb(int r, int g, int b) => new(r, g, b, 255);

    private static readonly Color Wood = Rgb(96, 64, 44), WoodLit = Rgb(128, 90, 62), Cord = Rgb(70, 52, 40);
    private static readonly Color Red = Rgb(214, 72, 56), RedDark = Rgb(160, 46, 40), Orange = Rgb(236, 140, 52), OrangeDark = Rgb(186, 98, 36);
    private static readonly Color Gold = Rgb(220, 176, 70), Cream = Rgb(236, 226, 196);
    private static readonly Color PineDark = Rgb(40, 96, 64), PineLit = Rgb(64, 128, 80), Snow = Rgb(214, 226, 244);

    /// <summary>A pole from (x, top) down to the card's foot: three texels, its left one lit.</summary>
    private static void Pole(PixelCanvas c, int x, int top, Color face, Color lit)
    {
        c.Rect(x, top, 3, c.Height - top, face);
        c.VLine(x, top, c.Height - top, lit);
    }

    /// <summary>The cord's row at x, sagging <paramref name="sag"/> texels at its middle between two poles.</summary>
    private static int Sag(int x, int left, int right, int top, float sag)
    {
        float mid = (left + right) / 2f, half = (right - left) / 2f;
        float t = (x - mid) / half;
        return top + (int)MathF.Round(sag * (1f - t * t));
    }

    /// <summary>A round paper lantern 8 by 10 whose top is at (x, y): its paper, a darker band at top and foot and down its right side.</summary>
    private static void Lantern(PixelCanvas c, int x, int y, Color paper, Color band)
    {
        c.Rect(x + 1, y, 6, 10, paper);
        c.Rect(x, y + 1, 8, 8, paper);
        c.HLine(x + 1, y, 6, band);
        c.HLine(x + 1, y + 9, 6, band);
        c.VLine(x + 6, y + 2, 6, band);
        c.VLine(x + 7, y + 2, 6, band);
        c.Set(x + 3, y - 1, Cord);
        c.Set(x + 4, y - 1, Cord);
        c.Set(x + 3, y + 10, band);
        c.Set(x + 4, y + 10, band);
    }

    /// <summary>Marks a lantern's core lit (after the outline): three texels by five in its middle.</summary>
    private static void LightLantern(PixelCanvas c, int x, int y) => Pix.Lit(c, x + 2, y + 2, 3, 5, ArtSheet.PublicLight);

    /// <summary>Paper lanterns on a string, 64 by 54: two poles, a sagging cord and five lanterns, red and orange in turn.</summary>
    public static void PaintLanterns(PixelCanvas c)
    {
        int right = c.Width - 3;
        Pole(c, 0, 10, Wood, WoodLit);
        Pole(c, right, 10, Wood, WoodLit);
        for (int x = 3; x < right; x++) c.Set(x, Sag(x, 1, right + 1, 12, 5f), Cord);
        int[] at = { 7, 18, 28, 38, 49 };
        var tops = new int[at.Length];
        for (int i = 0; i < at.Length; i++)
        {
            tops[i] = Sag(at[i] + 4, 1, right + 1, 12, 5f) + 2;
            Lantern(c, at[i], tops[i], i % 2 == 0 ? Red : Orange, i % 2 == 0 ? RedDark : OrangeDark);
        }
        Pix.Outline(c);
        for (int i = 0; i < at.Length; i++) LightLantern(c, at[i], tops[i]);
    }

    /// <summary>Kanto's moon lantern, 32 by 64: a pole with a crook and a round pale gold lantern with a crescent of shade.</summary>
    public static void PaintMoonLantern(PixelCanvas c)
    {
        Pole(c, 3, 4, Wood, WoodLit);
        c.Rect(3, 4, 18, 3, Wood);
        c.HLine(3, 4, 18, WoodLit);
        c.VLine(19, 7, 3, Cord);
        var gold = Rgb(246, 214, 120);
        var shade = Rgb(214, 170, 80);
        const int size = 22, left = 8, top = 10;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float u = (x + 0.5f - size / 2f) / (size / 2f), v = (y + 0.5f - size / 2f) / (size / 2f);
                if (u * u + v * v > 1f) continue;
                float w = (u - 0.35f) * (u - 0.35f) + v * v;
                c.Set(left + x, top + y, w > 0.75f && u > 0.2f ? shade : gold);
            }
        // Its cap and tassel, red
        c.Rect(left + 8, top - 1, 6, 2, Red);
        c.Rect(left + 9, top + size, 4, 2, Red);
        c.VLine(left + 10, top + size + 2, 4, RedDark);
        Pix.Outline(c);
        Pix.Lit(c, left + 5, top + 5, 11, 12, ArtSheet.PublicLight);
    }

    /// <summary>A pine of three tiers in the field's dark green with snow on each tier; lit bulbs (midwinter) or a star, baubles and a ribbon (the holiday's).</summary>
    public static void PaintPine(PixelCanvas c, bool holiday)
    {
        int w = c.Width, h = c.Height;
        int cx = w / 2;
        // Trunk
        c.Rect(cx - 3, h - 8, 6, 8, Wood);
        c.VLine(cx - 3, h - 8, 8, WoodLit);
        int top = holiday ? 10 : 4;
        int body = h - 8 - top;
        int widest = w / 2 - 1;
        for (int tier = 0; tier < 3; tier++)
        {
            int ty = top + tier * body / 3 - (tier > 0 ? 4 : 0);
            int th = body / 3 + (tier > 0 ? 4 : 0);
            int half = widest - (2 - tier) * widest / 4;
            for (int y = 0; y < th; y++)
            {
                int span = 1 + (half - 1) * y / Math.Max(1, th - 1);
                c.Rect(cx - span, ty + y, span * 2, 1, PineDark);
                c.Rect(cx - span, ty + y, Math.Max(1, span / 2), 1, PineLit);
            }
            // Snow along the tier's edges and its foot
            for (int y = 0; y < th; y += 2)
            {
                int span = 1 + (half - 1) * y / Math.Max(1, th - 1);
                c.Set(cx - span, ty + y, Snow);
                c.Set(cx + span - 1, ty + y, Snow);
            }
            c.HLine(cx - 1, ty, 2, Snow);
            for (int x = cx - half + 2; x < cx + half - 2; x += 3) c.Set(x, ty + th - 1, Snow);
        }
        // Lights at places a third, a half and two thirds down each tier, either side
        var lights = new System.Collections.Generic.List<(int X, int Y)>();
        for (int tier = 0; tier < 3; tier++)
        {
            int ty = top + tier * body / 3 - (tier > 0 ? 4 : 0);
            int th = body / 3 + (tier > 0 ? 4 : 0);
            int half = widest - (2 - tier) * widest / 4;
            foreach (float f in new[] { 0.45f, 0.8f })
            {
                int y = ty + (int)(th * f);
                int span = 1 + (int)((half - 1) * f);
                lights.Add((cx - span / 2 - 1, y));
                lights.Add((cx + span / 2, y + 2));
            }
        }
        if (!holiday)
        {
            for (int i = 0; i < lights.Count; i++)
                c.Rect(lights[i].X, lights[i].Y, 2, 2, i % 2 == 0 ? Rgb(250, 210, 110) : Rgb(230, 90, 80));
            Pix.Outline(c);
            foreach (var (x, y) in lights) Pix.Lit(c, x, y, 2, 2, ArtSheet.PublicLight);
            return;
        }
        // The holiday's pine: a ribbon winding down, baubles and a gold star
        var ribbon = Rgb(206, 52, 56);
        for (int y = top + 8; y < h - 10; y++)
        {
            int span = 2 + (y - top) * (widest - 2) / body;
            int x = cx - span + ((y - top) * 3) % Math.Max(1, span * 2);
            c.Set(x, y, ribbon);
            c.Set(x + 1, y, ribbon);
        }
        Color[] baubles = { Rgb(214, 56, 56), Rgb(90, 130, 220), Gold };
        for (int i = 0; i < lights.Count; i++) c.Rect(lights[i].X, lights[i].Y, 3, 3, baubles[i % 3]);
        var star = Rgb(246, 214, 90);
        c.Rect(cx - 1, 0, 2, 10, star);
        c.Rect(cx - 4, 3, 8, 3, star);
        c.Rect(cx - 2, 2, 4, 6, star);
        c.Set(cx - 5, 4, star);
        c.Set(cx + 4, 4, star);
        Pix.Outline(c);
        Pix.Lit(c, cx - 2, 3, 4, 3, ArtSheet.PublicLight);
    }

    /// <summary>Floaroma's blossom arch, 56 by 58: two posts wound in green holding a round arch of pink and white-pink blossom.</summary>
    public static void PaintArch(PixelCanvas c)
    {
        var vine = Rgb(86, 150, 70);
        var leaf = Rgb(120, 182, 92);
        var pink = Rgb(240, 160, 190);
        var pale = Rgb(250, 214, 226);
        var petal = Rgb(236, 130, 170);
        int w = c.Width, h = c.Height;
        Pole(c, 4, 22, Rgb(150, 112, 78), Rgb(180, 140, 100));
        Pole(c, w - 7, 22, Rgb(150, 112, 78), Rgb(180, 140, 100));
        for (int y = 24; y < h - 2; y += 3)
        {
            c.Set(4, y, vine); c.Set(5, y + 1, leaf); c.Set(6, y + 2, vine);
            c.Set(w - 5, y, vine); c.Set(w - 6, y + 1, leaf); c.Set(w - 7, y + 2, vine);
        }
        // The arch: clusters of 5 by 5 along a half circle, close enough to touch
        float cx = w / 2f, cy = 26f, rx = w / 2f - 6f, ry = 22f;
        // Two rings of it, the inner one a cluster's width in and set between the outer one's
        for (int k = 0; k < 41; k++)
        {
            int i = k % 21;
            bool inner = k > 20;
            if (inner && i == 20) continue;
            float a = MathF.PI * (i + (inner ? 0.5f : 0f)) / 20f;
            float rxi = inner ? rx - 4f : rx, ryi = inner ? ry - 4f : ry;
            int x = (int)MathF.Round(cx - rxi * MathF.Cos(a)) - 2;
            int y = (int)MathF.Round(cy - ryi * MathF.Sin(a)) - 2;
            var bloom = i % 3 == 0 ? pale : pink;
            c.Rect(x + 1, y, 3, 5, bloom);
            c.Rect(x, y + 1, 5, 3, bloom);
            c.Set(x + 2, y + 2, i % 2 == 0 ? petal : Gold);
            if (i % 4 == 1) { c.Set(x - 1, y + 4, leaf); c.Set(x + 5, y + 4, leaf); }
        }
        // A few petals fallen at its feet
        foreach (var (x, y) in new[] { (10, h - 3), (15, h - 2), (22, h - 1), (31, h - 2), (38, h - 1), (45, h - 3) }) c.Set(x, y, petal);
        Pix.Outline(c);
    }

    /// <summary>A stall's awning, 40 by 44: a counter of planks under an awning of red and cream stripes, with a hem cut in steps.</summary>
    public static void PaintStall(PixelCanvas c)
    {
        var plank = Rgb(164, 112, 72);
        var plankTop = Rgb(196, 146, 96);
        var stripe = Rgb(206, 64, 56);
        int w = c.Width, h = c.Height;
        // Posts
        c.Rect(3, 10, 3, h - 10, Wood);
        c.Rect(w - 6, 10, 3, h - 10, Wood);
        // Awning
        for (int x = 0; x < w; x++)
        {
            var col = (x / 5) % 2 == 0 ? stripe : Cream;
            for (int y = 2; y < 13; y++) c.Set(x, y, col);
            if (x % 5 != 4) c.Set(x, 13, col);
            if (x % 5 is 1 or 2) c.Set(x, 14, col);
        }
        c.HLine(0, 2, w, PixelCanvas.Shadow(stripe, 0.2f));
        // Counter
        c.Rect(1, h - 16, w - 2, 16, plank);
        c.HLine(1, h - 16, w - 2, plankTop);
        c.HLine(1, h - 15, w - 2, plankTop);
        for (int y = h - 12; y < h; y += 4) c.HLine(2, y, w - 4, PixelCanvas.Shadow(plank, 0.25f));
        // Two jars and a basket of fruit on the counter
        c.Rect(7, h - 23, 6, 7, Rgb(170, 200, 220));
        c.HLine(7, h - 23, 6, Rgb(120, 90, 60));
        c.Rect(16, h - 21, 8, 5, Rgb(176, 124, 70));
        c.Rect(17, h - 23, 2, 2, Red); c.Rect(20, h - 23, 2, 2, Orange);
        c.Rect(27, h - 22, 7, 6, Rgb(236, 180, 90));
        c.HLine(27, h - 22, 7, Rgb(120, 90, 60));
        Pix.Outline(c);
    }

    /// <summary>Johto's bells and maple garland, 52 by 54: a red-lacquered frame, three bronze bells and a garland of maple leaves.</summary>
    public static void PaintBells(PixelCanvas c)
    {
        var lacquer = Rgb(176, 52, 44);
        var lacquerLit = Rgb(206, 80, 62);
        int w = c.Width;
        Pole(c, 4, 6, lacquer, lacquerLit);
        Pole(c, w - 7, 6, lacquer, lacquerLit);
        c.Rect(0, 4, w, 4, lacquer);
        c.HLine(0, 4, w, lacquerLit);
        c.Rect(4, 12, w - 8, 3, lacquer);
        // Bells
        var bronze = Rgb(176, 130, 58);
        var bronzeLit = Rgb(214, 170, 84);
        foreach (var (x, size) in new[] { (15, 8), (w / 2, 11), (w - 16, 8) })
        {
            int top = 18;
            c.VLine(x, 15, 3, Cord);
            for (int y = 0; y < size + 2; y++)
            {
                int half = Math.Min(size / 2, 1 + y / 2);
                c.Rect(x - half, top + y, half * 2 + 1, 1, y >= size ? bronze : bronzeLit);
                c.Set(x + half, top + y, bronze);
                c.Set(x + half - 1, top + y, bronze);
            }
            c.Rect(x - 1, top + size + 2, 3, 2, Rgb(110, 80, 40));
        }
        // Maple leaves along the beam: a five-pointed leaf in three texels' reach
        Color[] leaves = { Rgb(206, 72, 48), Rgb(226, 128, 52), Gold };
        for (int i = 0; i < 11; i++)
        {
            int x = 2 + i * 4 + (i % 2), y = 8 + (i % 2) * 2;
            var leaf = leaves[i % 3];
            c.Set(x + 2, y, leaf);
            c.Rect(x, y + 1, 5, 1, leaf);
            c.Rect(x + 1, y + 2, 3, 1, leaf);
            c.Set(x + 2, y + 3, PixelCanvas.Shadow(leaf, 0.3f));
        }
        // A few leaves fallen at its feet
        foreach (var (x, y, i) in new[] { (12, c.Height - 2, 0), (24, c.Height - 1, 1), (38, c.Height - 2, 2) })
        {
            c.Rect(x, y, 3, 1, leaves[i]);
            c.Set(x + 1, y - 1, leaves[i]);
        }
        Pix.Outline(c);
    }

    /// <summary>Hoenn's shell and flag bunting, 64 by 52: two poles, a line of triangular flags and a scallop shell on each pole.</summary>
    public static void PaintBunting(PixelCanvas c)
    {
        var poleCol = Rgb(200, 214, 230);
        int right = c.Width - 3;
        Pole(c, 0, 10, poleCol, Rgb(226, 236, 246));
        Pole(c, right, 10, poleCol, Rgb(226, 236, 246));
        Color[] flags = { Rgb(70, 140, 210), Rgb(232, 206, 150), Rgb(236, 120, 100) };
        for (int x = 3; x < right; x++) c.Set(x, Sag(x, 1, right + 1, 13, 4f), Cord);
        for (int i = 0; i < 9; i++)
        {
            int x = 5 + i * 6;
            int y = Sag(x + 3, 1, right + 1, 13, 4f) + 1;
            var f = flags[i % 3];
            for (int r = 0; r < 6; r++) c.Rect(x + r / 2, y + r, 6 - (r / 2) * 2, 1, f);
        }
        // Shells on the poles' tops
        var shell = Rgb(246, 214, 190);
        var ridge = Rgb(214, 160, 140);
        foreach (int x in new[] { -1, right - 1 })
        {
            c.Rect(x, 5, 6, 4, shell);
            c.Rect(x + 1, 4, 4, 1, shell);
            c.Set(x + 1, 7, ridge); c.Set(x + 3, 7, ridge); c.Set(x + 5, 7, ridge);
            c.Rect(x + 2, 9, 2, 1, ridge);
        }
        Pix.Outline(c);
    }

    /// <summary>Easter's basket of painted eggs, 20 by 16: a woven basket heaped with six pastel eggs.</summary>
    public static void PaintEggBasket(PixelCanvas c)
    {
        var weave = Rgb(176, 124, 70);
        var weaveDark = Rgb(140, 96, 54);
        Color[] eggs = { Rgb(240, 180, 200), Rgb(170, 200, 240), Rgb(246, 226, 140), Rgb(180, 220, 160), Rgb(240, 180, 200), Rgb(170, 200, 240) };
        var at = new (int X, int Y)[] { (3, 4), (8, 3), (13, 4), (5, 1), (10, 0), (15, 2) };
        for (int i = 5; i >= 0; i--)
        {
            var (x, y) = at[i];
            var e = eggs[i];
            c.Rect(x + 1, y, 2, 5, e);
            c.Rect(x, y + 1, 4, 3, e);
            var deep = PixelCanvas.Shadow(e, 0.25f);
            if (i % 2 == 0) c.HLine(x, y + 2, 4, deep);
            else { c.Set(x + 1, y + 1, deep); c.Set(x + 2, y + 3, deep); }
        }
        c.Rect(1, 8, 18, 7, weave);
        c.Rect(2, 15, 16, 1, weave);
        for (int y = 9; y < 15; y += 2) c.HLine(1, y, 18, weaveDark);
        c.HLine(1, 8, 18, Rgb(206, 154, 96));
        Pix.Outline(c);
    }

    /// <summary>The New Year's banner, 56 by 50: a deep red cloth with a gold border between two poles, a lantern lit at each end.</summary>
    public static void PaintNewYearBanner(PixelCanvas c)
    {
        int w = c.Width, right = w - 3;
        Pole(c, 0, 4, Wood, WoodLit);
        Pole(c, right, 4, Wood, WoodLit);
        var cloth = Rgb(176, 52, 44);
        c.Rect(4, 6, w - 8, 15, Gold);
        c.Rect(5, 7, w - 10, 13, cloth);
        for (int x = 12; x < w - 12; x += 8) c.Rect(x, 11, 4, 4, Gold);
        c.VLine(8, 21, 3, Cord);
        c.VLine(w - 9, 21, 3, Cord);
        Lantern(c, 5, 25, Red, RedDark);
        Lantern(c, w - 13, 25, Red, RedDark);
        Pix.Outline(c);
        LightLantern(c, 5, 25);
        LightLantern(c, w - 13, 25);
    }

    /// <summary>St Valentine's heart on a post, 14 by 26: a white-painted post tied with a pink bow and a small red heart card.</summary>
    public static void PaintValentineHeart(PixelCanvas c)
    {
        var post = Rgb(226, 222, 212);
        c.Rect(6, 6, 3, 20, post);
        c.VLine(8, 6, 20, Rgb(186, 180, 168));
        var heart = Rgb(214, 72, 90);
        c.Rect(4, 1, 2, 2, heart);
        c.Rect(8, 1, 2, 2, heart);
        c.Rect(4, 2, 6, 2, heart);
        c.Rect(5, 4, 4, 1, heart);
        c.Rect(6, 5, 2, 1, heart);
        c.Set(4, 1, Rgb(240, 130, 140));
        var ribbon = Rgb(236, 140, 170);
        c.HLine(5, 12, 5, ribbon);
        c.Rect(3, 11, 2, 2, ribbon);
        c.Rect(10, 11, 2, 2, ribbon);
        c.VLine(4, 13, 3, ribbon);
        c.VLine(10, 13, 3, ribbon);
        Pix.Outline(c);
    }

    /// <summary>Kalos's music stand, 36 by 56: its card (<see cref="PaintMusicStand"/>).</summary>
    public const int StandWidth = 36, StandHeight = 56;

    /// <summary>
    /// Kalos's Solstice Serenade in Lumiose City, 36 by 56: a music stand of wrought iron (a tripod's three splayed feet, a
    /// rod, a tilted desk holding a sheet of songs on two staves, a scroll of iron curling over it with a small round lamp
    /// at its top) and a bow of violet and gold ribbon tied round the rod, its tails hanging. Only the lamp's core is lit.
    /// </summary>
    public static void PaintMusicStand(PixelCanvas c)
    {
        var iron = Rgb(52, 56, 70);
        var ironLit = Rgb(92, 98, 118);
        int mid = c.Width / 2, h = c.Height;
        // The tripod: three feet splayed from the rod's foot
        for (int i = 0; i < 9; i++)
        {
            c.Set(mid - 1 - i, h - 9 + i, iron);
            c.Set(mid + 1 + i, h - 9 + i, iron);
        }
        c.VLine(mid, h - 9, 9, iron);
        c.HLine(mid - 11, h - 1, 3, iron);
        c.HLine(mid + 9, h - 1, 3, iron);
        // The rod, two texels, its left one lit
        c.Rect(mid - 1, 24, 2, h - 33, iron);
        c.VLine(mid - 1, 24, h - 33, ironLit);
        // The desk: a ledge of iron with the sheet of songs standing on it, leaning back a texel every eight rows
        int deskTop = 10, deskBottom = 26, left = 5, right = c.Width - 6;
        var paper = Rgb(236, 226, 196);
        var paperShade = Rgb(206, 192, 158);
        var ink = Rgb(70, 52, 40);
        for (int y = deskTop; y < deskBottom; y++)
        {
            int lean = (deskBottom - y) / 8;
            c.HLine(left + lean, y, right - left, paper);
            c.Set(right + lean - 1, y, paperShade);
        }
        // Two staves of five lines with a few notes on each
        foreach (int top in new[] { deskTop + 2, deskTop + 9 })
        {
            for (int l = 0; l < 5; l += 2)
            {
                int y = top + l, lean = (deskBottom - y) / 8;
                c.HLine(left + 2 + lean, y, right - left - 4, paperShade);
            }
            foreach (var (nx, ny) in new[] { (left + 5, top + 3), (left + 10, top + 1), (left + 15, top + 2), (left + 20, top), (left + 24, top + 3) })
            {
                c.Rect(nx, ny, 2, 2, ink);
                c.VLine(nx + 1, ny - 3, 3, ink);
            }
        }
        c.Rect(left - 1, deskBottom, right - left + 2, 2, iron);
        c.HLine(left - 1, deskBottom, right - left + 2, ironLit);
        // The scroll of iron over the desk: an arc from the desk's corners curling in at its ends, and the lamp at its top
        for (int x = left; x <= right; x++)
        {
            float t = (x - mid) / (float)(right - left) * 2f;
            int y = deskTop - 2 - (int)MathF.Round(5f * (1f - t * t));
            c.Set(x, y, iron);
        }
        c.Set(left, deskTop - 1, iron); c.Set(left + 1, deskTop, iron);
        c.Set(right, deskTop - 1, iron); c.Set(right - 1, deskTop, iron);
        var glass = Rgb(246, 214, 120);
        c.Rect(mid - 2, 0, 4, 4, glass);
        c.HLine(mid - 2, 0, 4, Rgb(214, 170, 80));
        c.VLine(mid, 4, 2, iron);
        // The bow on the rod: violet and gold loops either side of a knot, two tails hanging
        var violet = Rgb(150, 96, 196);
        var violetDark = Rgb(104, 60, 148);
        c.Rect(mid - 5, 30, 4, 3, violet);
        c.Rect(mid + 1, 30, 4, 3, violet);
        c.Set(mid - 5, 32, violetDark);
        c.Set(mid + 4, 32, violetDark);
        c.Rect(mid - 1, 30, 2, 3, Gold);
        c.VLine(mid - 3, 33, 6, violet);
        c.VLine(mid + 2, 33, 7, Gold);
        c.Set(mid - 3, 39, violetDark);
        Pix.Outline(c);
        Pix.Lit(c, mid - 1, 1, 2, 2, ArtSheet.PublicLight);
    }

    /// <summary>Alola's lei arch, 64 by 54; Galar's hay bales, 44 by 36; Hisui's sheaves, 36 by 56.</summary>
    public const int LeiWidth = 64, LeiHeight = 54, BalesWidth = 44, BalesHeight = 36, SheavesWidth = 36, SheavesHeight = 56;

    /// <summary>A flower of a lei, five texels across: a 3 by 3 middle with a petal out on each side and its heart, centred on (x, y).</summary>
    private static void Flower(PixelCanvas c, int x, int y, int which)
    {
        Color petal = (which % 3) switch { 0 => Rgb(236, 110, 150), 1 => Rgb(246, 236, 200), _ => Rgb(240, 150, 70) };
        c.Rect(x - 1, y - 1, 3, 3, petal); c.Set(x, y - 2, petal); c.Set(x - 2, y, petal); c.Set(x + 2, y, petal); c.Set(x, y + 2, petal);
        c.Set(x, y, which % 3 == 1 ? Rgb(246, 206, 90) : PixelCanvas.Shadow(petal, 0.25f));
    }

    /// <summary>
    /// Alola's Garland Days in Hau'oli City, 64 by 54: two bamboo poles knotted every eight rows, a lei of hibiscus, plumeria
    /// and orange flowers with leaves between swung from top to top, a shorter lei hanging down each pole, and a pale aqua
    /// crystal on each pole's top for the Z-Move poses danced under it. Only the crystals' middles are lit.
    /// </summary>
    public static void PaintLeiArch(PixelCanvas c)
    {
        var bamboo = Rgb(176, 168, 92);
        var bambooLit = Rgb(214, 206, 130);
        var knot = Rgb(132, 124, 66);
        var leaf = Rgb(72, 150, 80);
        int right = c.Width - 4, top = 9;
        foreach (int x in new[] { 1, right })
        {
            Pole(c, x, top, bamboo, bambooLit);
            for (int y = top + 6; y < c.Height; y += 8) c.HLine(x, y, 3, knot);
        }
        // The lei swung between the tops: a flower every five texels, a leaf between
        int n = 0;
        for (int x = 5; x <= right - 2; x += 5, n++)
        {
            int y = Sag(x, 2, right + 2, top + 2, 6f);
            Flower(c, x, y, n);
            int lx = x + 3;
            if (lx < right - 1) c.Set(lx, Sag(lx, 2, right + 2, top + 2, 6f), leaf);
        }
        // A shorter lei down each pole, to half its height
        foreach (int x in new[] { 2, right + 1 })
            for (int y = top + 6, k = 1; y < top + 6 + 18; y += 5, k++)
            {
                Flower(c, x, y, k);
                c.Set(x + (x < 10 ? 2 : -2), y + 2, leaf);
            }
        // A crystal on each pole's top
        var aqua = Rgb(150, 220, 220);
        var aquaDeep = Rgb(90, 170, 180);
        foreach (int x in new[] { 0, right - 1 })
        {
            c.Rect(x + 1, 1, 2, 6, aqua);
            c.Rect(x, 2, 4, 4, aqua);
            c.VLine(x + 3, 2, 4, aquaDeep);
            c.Set(x + 2, 6, aquaDeep);
        }
        Pix.Outline(c);
        Pix.Lit(c, 1, 2, 2, 3, ArtSheet.PublicLight);
        Pix.Lit(c, right, 2, 2, 3, ArtSheet.PublicLight);
    }

    /// <summary>A round bale of straw, its straws in lighter rows every third row.</summary>
    private static void Bale(PixelCanvas c, int x, int y, int w, int h)
    {
        var straw = Rgb(214, 184, 104);
        var shade = Rgb(176, 146, 72);
        c.Rect(x + 1, y, w - 2, h, straw);
        c.Rect(x, y + 1, w, h - 2, straw);
        for (int r = y + 2; r < y + h - 1; r += 3) c.HLine(x + 2, r, w - 4, Rgb(236, 212, 140));
        c.VLine(x + w - 2, y + 1, h - 2, shade);
        c.HLine(x + 1, y + h - 1, w - 2, shade);
    }

    /// <summary>
    /// Galar's Wool Fair in Wedgehurst, 44 by 36: two round bales of straw with a smaller one on them, a prize rosette pinned
    /// to the top bale's face and a tuft of fleece heaped on its top for the shearing. Nothing lit.
    /// </summary>
    public static void PaintHayBales(PixelCanvas c)
    {
        Bale(c, 1, 20, 21, 15);
        Bale(c, 22, 20, 21, 15);
        Bale(c, 12, 7, 20, 14);
        // The fleece on top
        var fleece = Rgb(236, 232, 222);
        var fleeceShade = Rgb(200, 196, 186);
        c.Rect(15, 3, 14, 5, fleece);
        c.Rect(13, 5, 18, 3, fleece);
        for (int x = 14; x < 30; x += 3) c.Set(x, 7, fleeceShade);
        c.Set(18, 2, fleece); c.Set(24, 2, fleece);
        // The rosette with its tails
        var red = Rgb(200, 52, 56);
        c.Rect(19, 10, 6, 6, red);
        c.Rect(18, 11, 8, 4, red);
        c.Rect(21, 12, 2, 2, Gold);
        c.VLine(20, 16, 4, Gold);
        c.VLine(23, 16, 5, Gold);
        Pix.Outline(c);
    }

    /// <summary>A sheaf of wheat whose waist is at (x, y): stalks below, a fringe of ears above, leaning by <paramref name="lean"/>.</summary>
    private static void Sheaf(PixelCanvas c, int x, int y, int lean)
    {
        var wheat = Rgb(220, 184, 96);
        var stalk = Rgb(176, 140, 66);
        var ear = Rgb(236, 206, 120);
        int foot = c.Height - 1;
        for (int r = y; r <= foot; r++)
        {
            int spread = (r - y) / 5;
            int off = lean * (foot - r) / 24;
            c.HLine(x - 2 - spread + off, r, 5 + spread * 2, wheat);
            c.Set(x + 2 + spread + off, r, stalk);
        }
        for (int r = y - 16; r < y; r++)
        {
            int spread = (r - (y - 16)) < 8 ? (r - (y - 16)) / 2 : (y - r) / 2;
            int off = lean * (foot - r) / 24;
            c.HLine(x - 2 - spread + off, r, 5 + spread * 2, wheat);
            if (r % 2 == 0) { c.Set(x - 2 - spread + off, r, ear); c.Set(x + 2 + spread + off, r, ear); }
        }
        c.HLine(x - 3 + lean * (foot - y) / 24, y, 7, Rgb(150, 110, 62));
    }

    /// <summary>
    /// Hisui's Sheaf Feast in Jubilife Village, 36 by 56: a stake with a paper tally of three red marks, and before it three
    /// sheaves of wheat leaning together, each bound at its waist with straw rope. Nothing lit.
    /// </summary>
    public static void PaintSheaves(PixelCanvas c)
    {
        var stake = Rgb(120, 84, 56);
        int mid = c.Width / 2;
        c.Rect(mid - 1, 1, 3, c.Height - 1, stake);
        var paper = Rgb(236, 226, 196);
        c.Rect(mid - 3, 4, 7, 18, paper);
        c.VLine(mid + 3, 4, 18, Rgb(206, 192, 158));
        var red = Rgb(190, 56, 48);
        for (int k = 0; k < 3; k++) c.Rect(mid - 1, 7 + k * 5, 3, 2, red);
        Sheaf(c, mid - 8, 36, 3);
        Sheaf(c, mid + 8, 36, -3);
        Sheaf(c, mid, 38, 0);
        Pix.Outline(c);
    }
}
