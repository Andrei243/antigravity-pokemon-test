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
}
