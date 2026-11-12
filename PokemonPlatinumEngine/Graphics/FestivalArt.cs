using System;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// The festivals' things (plan 40 · GE9; style guide, "Life", "Festivals"): cards of our own a tile across, in the
/// scenery's 32 texels a tile, each outlined like every card. What glows after dark (a lantern's core, a bulb, a star) is
/// marked lit with <see cref="ArtSheet.PublicLight"/> after the outline, and only its core, so the rim keeps its colour.
/// Painted in code, no GPU calls.
/// </summary>
internal static class FestivalArt
{
    public const int LanternsWidth = 32, LanternsHeight = 46;

    private static Color Rgb(int r, int g, int b) => new(r, g, b, 255);

    private static readonly Color Wood = Rgb(96, 64, 44), WoodLit = Rgb(128, 90, 62), Cord = Rgb(70, 52, 40);
    private static readonly Color Red = Rgb(214, 72, 56), RedDark = Rgb(160, 46, 40), Orange = Rgb(236, 140, 52), OrangeDark = Rgb(186, 98, 36);
    private static readonly Color Gold = Rgb(220, 176, 70), Cream = Rgb(236, 226, 196);
    private static readonly Color PineDark = Rgb(40, 96, 64), PineLit = Rgb(64, 128, 80), Snow = Rgb(214, 226, 244);

    /// <summary>A pole from (x, top) down to the card's foot: two texels, its left one lit.</summary>
    private static void Pole(PixelCanvas c, int x, int top, Color face, Color lit)
    {
        c.Rect(x, top, 2, c.Height - top, face);
        c.VLine(x, top, c.Height - top, lit);
    }

    /// <summary>A round paper lantern 6 by 7 whose top is at (x, y): its paper, a darker band at top and foot, and its core to be lit.</summary>
    private static void Lantern(PixelCanvas c, int x, int y, Color paper, Color band)
    {
        c.Rect(x + 1, y, 4, 7, paper);
        c.Rect(x, y + 1, 6, 5, paper);
        c.HLine(x + 1, y, 4, band);
        c.HLine(x + 1, y + 6, 4, band);
        c.VLine(x + 5, y + 2, 3, band);
        c.Set(x + 2, y - 1, Cord);
    }

    /// <summary>Marks a lantern's core lit (after the outline): two texels by three in its middle.</summary>
    private static void LightLantern(PixelCanvas c, int x, int y) => Pix.Lit(c, x + 2, y + 2, 2, 3, ArtSheet.PublicLight);

    /// <summary>Paper lanterns on a string, 32 by 46: two poles, a sagging cord and four lanterns, red and orange in turn.</summary>
    public static void PaintLanterns(PixelCanvas c)
    {
        Pole(c, 0, 8, Wood, WoodLit);
        Pole(c, 30, 8, Wood, WoodLit);
        // The cord sags three texels at its middle
        for (int x = 2; x < 30; x++)
        {
            float t = (x - 16f) / 14f;
            c.Set(x, 9 + (int)MathF.Round(3f * (1f - t * t)), Cord);
        }
        int[] at = { 4, 10, 16, 22 };
        for (int i = 0; i < at.Length; i++)
        {
            int x = at[i];
            float t = (x + 3 - 16f) / 14f;
            int y = 11 + (int)MathF.Round(3f * (1f - t * t));
            Lantern(c, x, y, i % 2 == 0 ? Red : Orange, i % 2 == 0 ? RedDark : OrangeDark);
        }
        Pix.Outline(c);
        for (int i = 0; i < at.Length; i++)
        {
            int x = at[i];
            float t = (x + 3 - 16f) / 14f;
            LightLantern(c, x, 11 + (int)MathF.Round(3f * (1f - t * t)));
        }
    }

    /// <summary>Kanto's moon lantern, 24 by 48: a pole with a crook and a round pale gold lantern with a crescent of shade.</summary>
    public static void PaintMoonLantern(PixelCanvas c)
    {
        Pole(c, 3, 4, Wood, WoodLit);
        c.Rect(3, 4, 12, 2, Wood);
        c.VLine(14, 6, 3, Cord);
        var gold = Rgb(246, 214, 120);
        var shade = Rgb(214, 170, 80);
        for (int y = 0; y < 14; y++)
            for (int x = 0; x < 14; x++)
            {
                float u = (x + 0.5f - 7f) / 7f, v = (y + 0.5f - 7f) / 7f;
                if (u * u + v * v > 1f) continue;
                float w = (u - 0.35f) * (u - 0.35f) + v * v;
                c.Set(8 + x, 9 + y, w > 0.75f && u > 0.2f ? shade : gold);
            }
        Pix.Outline(c);
        Pix.Lit(c, 11, 12, 8, 8, ArtSheet.PublicLight);
    }

    /// <summary>A pine of three tiers in the field's dark green with snow on each tier; lit bulbs (midwinter) or a star and baubles (the holiday's).</summary>
    public static void PaintPine(PixelCanvas c, bool holiday)
    {
        int w = c.Width, h = c.Height;
        int cx = w / 2;
        // Trunk
        c.Rect(cx - 2, h - 6, 4, 6, Wood);
        c.VLine(cx - 2, h - 6, 6, WoodLit);
        int top = holiday ? 8 : 4;
        int body = h - 6 - top;
        // Three tiers, each wider than the one above it and overlapping it
        for (int tier = 0; tier < 3; tier++)
        {
            int ty = top + tier * body / 3 - (tier > 0 ? 3 : 0);
            int th = body / 3 + (tier > 0 ? 3 : 0);
            int half = 6 + tier * 4;
            for (int y = 0; y < th; y++)
            {
                int span = 1 + (half - 1) * y / Math.Max(1, th - 1);
                c.Rect(cx - span, ty + y, span * 2, 1, PineDark);
                c.Rect(cx - span, ty + y, Math.Max(1, span / 2), 1, PineLit);
            }
            // Snow along the tier's top edge
            for (int y = 0; y < th; y += 2)
            {
                int span = 1 + (half - 1) * y / Math.Max(1, th - 1);
                c.Set(cx - span, ty + y, Snow);
                c.Set(cx + span - 1, ty + y, Snow);
            }
            c.HLine(cx - 1, ty, 2, Snow);
        }
        var lights = new (int X, int Y)[] { (cx - 3, top + 6), (cx + 2, top + 9), (cx - 6, top + 17), (cx + 4, top + 19), (cx - 2, top + 22), (cx - 9, top + 30), (cx + 7, top + 31), (cx + 1, top + 34) };
        if (!holiday)
        {
            for (int i = 0; i < lights.Length; i++)
                c.Rect(lights[i].X, lights[i].Y, 1, 2, i % 2 == 0 ? Rgb(250, 210, 110) : Rgb(230, 90, 80));
            Pix.Outline(c);
            foreach (var (x, y) in lights) Pix.Lit(c, x, y, 1, 2, ArtSheet.PublicLight);
            return;
        }
        // The holiday's pine: a ribbon winding down, baubles and a gold star
        var ribbon = Rgb(206, 52, 56);
        for (int y = top + 6; y < h - 8; y++)
        {
            int span = 2 + (y - top) * 13 / body;
            int x = cx - span + ((y - top) * 3) % (span * 2);
            c.Set(x, y, ribbon);
        }
        Color[] baubles = { Rgb(214, 56, 56), Rgb(90, 130, 220), Gold };
        for (int i = 0; i < lights.Length; i++) c.Rect(lights[i].X, lights[i].Y, 2, 2, baubles[i % 3]);
        var star = Rgb(246, 214, 90);
        c.Rect(cx - 1, 1, 2, 7, star);
        c.Rect(cx - 3, 3, 6, 3, star);
        c.Set(cx - 4, 4, star);
        c.Set(cx + 3, 4, star);
        Pix.Outline(c);
        Pix.Lit(c, cx - 1, 3, 2, 3, ArtSheet.PublicLight);
    }

    /// <summary>Floaroma's blossom arch, 32 by 46: two posts wound in green holding a round arch of pink and white-pink blossom.</summary>
    public static void PaintArch(PixelCanvas c)
    {
        var vine = Rgb(86, 150, 70);
        var pink = Rgb(240, 160, 190);
        var pale = Rgb(250, 214, 226);
        var petal = Rgb(236, 130, 170);
        Pole(c, 2, 14, Rgb(150, 112, 78), Rgb(180, 140, 100));
        Pole(c, 28, 14, Rgb(150, 112, 78), Rgb(180, 140, 100));
        for (int y = 16; y < 44; y += 3)
        {
            c.Set(2, y, vine);
            c.Set(3, y + 1, vine);
            c.Set(29, y, vine);
            c.Set(28, y + 1, vine);
        }
        // The arch: clusters of 3 by 3 along a half circle
        for (int i = 0; i <= 12; i++)
        {
            float a = MathF.PI * i / 12f;
            int x = (int)MathF.Round(16 - 13 * MathF.Cos(a)) - 1;
            int y = (int)MathF.Round(16 - 12 * MathF.Sin(a)) - 1;
            c.Rect(x, y, 3, 3, i % 2 == 0 ? pink : pale);
            c.Set(x + 1, y + 1, i % 3 == 0 ? petal : vine);
        }
        // A few petals fallen at its feet
        foreach (var (x, y) in new[] { (6, 44), (9, 43), (14, 45), (21, 44), (25, 45) }) c.Set(x, y, petal);
        Pix.Outline(c);
    }

    /// <summary>A stall's awning, 32 by 40: a counter of planks under an awning of five stripes, red and cream, with a scalloped hem.</summary>
    public static void PaintStall(PixelCanvas c)
    {
        var plank = Rgb(164, 112, 72);
        var plankTop = Rgb(196, 146, 96);
        var stripe = Rgb(206, 64, 56);
        // Posts
        c.Rect(2, 8, 2, 32, Wood);
        c.Rect(28, 8, 2, 32, Wood);
        // Awning
        for (int x = 0; x < 32; x++)
        {
            var col = (x / 4) % 2 == 0 ? stripe : Cream;
            for (int y = 2; y < 11; y++) c.Set(x, y + (y == 2 && (x < 2 || x > 29) ? 1 : 0), col);
            if (x % 4 != 3) c.Set(x, 11, col);
        }
        c.HLine(1, 2, 30, PixelCanvas.Shadow(stripe, 0.2f));
        // Counter
        c.Rect(1, 26, 30, 14, plank);
        c.HLine(1, 26, 30, plankTop);
        c.HLine(1, 27, 30, plankTop);
        for (int y = 30; y < 40; y += 4) c.HLine(2, y, 28, PixelCanvas.Shadow(plank, 0.25f));
        // Two jars on the counter
        c.Rect(7, 20, 5, 6, Rgb(170, 200, 220));
        c.HLine(7, 20, 5, Rgb(120, 90, 60));
        c.Rect(19, 21, 6, 5, Rgb(236, 180, 90));
        c.HLine(19, 21, 6, Rgb(120, 90, 60));
        Pix.Outline(c);
    }

    /// <summary>Johto's bells and maple garland, 32 by 46: a red-lacquered frame, three bronze bells and a garland of maple leaves.</summary>
    public static void PaintBells(PixelCanvas c)
    {
        var lacquer = Rgb(176, 52, 44);
        var lacquerLit = Rgb(206, 80, 62);
        Pole(c, 2, 6, lacquer, lacquerLit);
        Pole(c, 28, 6, lacquer, lacquerLit);
        c.Rect(0, 4, 32, 3, lacquer);
        c.HLine(0, 4, 32, lacquerLit);
        c.Rect(2, 9, 28, 2, lacquer);
        // Bells
        var bronze = Rgb(176, 130, 58);
        var bronzeLit = Rgb(214, 170, 84);
        foreach (var (x, size) in new[] { (8, 5), (16, 7), (24, 5) })
        {
            int top = 13;
            c.VLine(x, 11, 2, Cord);
            for (int y = 0; y < size + 2; y++)
            {
                int half = Math.Min(size / 2, 1 + y / 2);
                c.Rect(x - half, top + y, half * 2 + 1, 1, y == size + 1 ? bronze : bronzeLit);
                c.Set(x + half, top + y, bronze);
            }
            c.Set(x, top + size + 2, Rgb(110, 80, 40));
        }
        // Maple leaves along the beam
        Color[] leaves = { Rgb(206, 72, 48), Rgb(226, 128, 52), Gold };
        for (int i = 0; i < 9; i++)
        {
            int x = 2 + i * 3 + (i % 2), y = 7 + (i % 2);
            var leaf = leaves[i % 3];
            c.Set(x + 1, y, leaf);
            c.Rect(x, y + 1, 3, 1, leaf);
            c.Set(x + 1, y + 2, leaf);
        }
        Pix.Outline(c);
    }

    /// <summary>Hoenn's shell and flag bunting, 32 by 46: two poles, a line of triangular flags and a scallop shell on each pole.</summary>
    public static void PaintBunting(PixelCanvas c)
    {
        var poleCol = Rgb(200, 214, 230);
        Pole(c, 1, 8, poleCol, Rgb(226, 236, 246));
        Pole(c, 29, 8, poleCol, Rgb(226, 236, 246));
        Color[] flags = { Rgb(70, 140, 210), Rgb(232, 206, 150), Rgb(236, 120, 100) };
        for (int x = 3; x < 29; x++)
        {
            float t = (x - 16f) / 13f;
            c.Set(x, 11 + (int)MathF.Round(2f * (1f - t * t)), Cord);
        }
        for (int i = 0; i < 6; i++)
        {
            int x = 4 + i * 4;
            float t = (x + 2 - 16f) / 13f;
            int y = 12 + (int)MathF.Round(2f * (1f - t * t));
            var f = flags[i % 3];
            for (int r = 0; r < 4; r++) c.Rect(x + r / 2, y + r, 4 - r, 1, f);
        }
        // Shells on the poles' tops
        var shell = Rgb(246, 214, 190);
        var ridge = Rgb(214, 160, 140);
        foreach (int x in new[] { 0, 28 })
        {
            c.Rect(x, 4, 4, 3, shell);
            c.Rect(x + 1, 3, 2, 1, shell);
            c.Set(x + 1, 5, ridge);
            c.Set(x + 2, 5, ridge);
            c.Rect(x + 1, 7, 2, 1, ridge);
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
        // The eggs behind first, then the front row
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

    /// <summary>The New Year's banner, 32 by 44: a deep red cloth with a gold border between two poles, two lanterns lit at its ends.</summary>
    public static void PaintNewYearBanner(PixelCanvas c)
    {
        Pole(c, 0, 4, Wood, WoodLit);
        Pole(c, 30, 4, Wood, WoodLit);
        var cloth = Rgb(176, 52, 44);
        c.Rect(3, 6, 26, 10, Gold);
        c.Rect(4, 7, 24, 8, cloth);
        foreach (int x in new[] { 9, 15, 21 }) c.Rect(x, 10, 2, 2, Gold);
        Lantern(c, 3, 19, Red, RedDark);
        Lantern(c, 23, 19, Red, RedDark);
        c.VLine(5, 16, 2, Cord);
        c.VLine(25, 16, 2, Cord);
        Pix.Outline(c);
        LightLantern(c, 3, 19);
        LightLantern(c, 23, 19);
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
