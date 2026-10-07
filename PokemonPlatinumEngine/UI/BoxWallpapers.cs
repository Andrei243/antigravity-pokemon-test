using System;
using Raylib_cs;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.UI;

/// <summary>
/// The PC's twenty-four box wallpapers (<see cref="PcBoxes.WallpaperNames"/>), painted in code (style guide, "Box
/// wallpapers"): each a soft gradient with one simple motif of our own, never the games' pictures, and quiet enough
/// that the menu icons read on it. Painted smooth (every edge anti-aliased) into a <see cref="PixelCanvas"/>, without
/// the GPU, in the layout units of the box's lining (<see cref="Width"/> by <see cref="Height"/>) at whatever scale is
/// asked; the screen paints each once, at half size, and draws it smoothed.
/// </summary>
public static class BoxWallpapers
{
    /// <summary>The lining of the box's panel, in layout units: what a wallpaper is designed for.</summary>
    public const float Width = 768, Height = 876;

    /// <summary>How rounded the lining's corners are, in layout units.</summary>
    public const float Corner = 24;

    /// <summary>The scale the screen paints at: half the layout's size.</summary>
    public const float ScreenScale = 0.5f;

    /// <summary>Paints wallpaper <paramref name="index"/> at <paramref name="scale"/> pixels to a layout unit, its corners rounded off.</summary>
    public static PixelCanvas Paint(int index, float scale = ScreenScale)
    {
        var p = new Painter(scale);
        switch (index)
        {
            case 0: Forest(p); break;
            case 1: City(p); break;
            case 2: Desert(p); break;
            case 3: Savanna(p); break;
            case 4: Crag(p); break;
            case 5: Volcano(p); break;
            case 6: Snow(p); break;
            case 7: Cave(p); break;
            case 8: Beach(p); break;
            case 9: Seafloor(p); break;
            case 10: River(p); break;
            case 11: Sky(p); break;
            case 12: PokeCenter(p); break;
            case 13: Machine(p); break;
            case 14: Checks(p); break;
            case 16: Distortion(p); break;
            case 17: Contest(p); break;
            case 18: Nostalgic(p); break;
            case 19: Croagunk(p); break;
            case 20: Trio(p); break;
            case 21: PikaPika(p); break;
            case 22: Legend(p); break;
            case 23: Galactic(p); break;
            default: Simple(p); break;
        }
        return p.Finish(Corner);
    }

    private static Color C(int r, int g, int b, int a = 255) => new(r, g, b, a);

    /// <summary>A number from 0 to 1 for <paramref name="i"/>, the same on every machine and every run.</summary>
    private static float Hash(int i, int salt)
    {
        unchecked
        {
            uint h = (uint)(i * 374761393 + salt * 668265263 + 1442695041);
            h = (h ^ (h >> 13)) * 1274126177u;
            h ^= h >> 16;
            return (h & 0xFFFFFF) / 16777216f;
        }
    }

    // ------------------------------------------------------------------ the sixteen every game has

    private static void Forest(Painter p)
    {
        p.Gradient(C(210, 234, 200), C(166, 208, 160));
        p.Below(x => 560 + 26 * MathF.Sin(x / 96f), C(184, 220, 176));
        for (int row = 0; row < 5; row++)
        {
            float y = 170 + row * 176;
            for (int i = 0; i < 7; i++)
            {
                float x = -30 + i * 128 + (row % 2) * 64;
                float s = 0.8f + 0.3f * Hash(row * 7 + i, 1);
                var tree = row % 2 == 0 ? C(146, 194, 146) : C(156, 202, 152);
                p.RoundRect(x - 6 * s, y - 6, 12 * s, 34 * s, 4, C(150, 170, 128));
                p.Tri(x, y - 92 * s, x - 34 * s, y - 34 * s, x + 34 * s, y - 34 * s, tree);
                p.Tri(x, y - 66 * s, x - 44 * s, y - 2, x + 44 * s, y - 2, tree);
            }
        }
    }

    private static void City(Painter p)
    {
        p.Gradient(C(220, 228, 246), C(184, 198, 232));
        for (int i = 0; i < 3; i++) Cloud(p, 140 + i * 250, 110 + 50 * Hash(i, 2), 34 + 10 * Hash(i, 3), C(240, 244, 252, 200));
        // A far row of towers, then a near one with lit windows
        float x = -10;
        for (int i = 0; x < Painter.W; i++)
        {
            float w = 70 + 50 * Hash(i, 4), h = 260 + 220 * Hash(i, 5);
            p.RoundRect(x, Painter.H - h - 120, w, h + 140, 10, C(186, 198, 230));
            x += w + 10;
        }
        x = -20;
        for (int i = 0; x < Painter.W; i++)
        {
            float w = 90 + 60 * Hash(i, 6), h = 160 + 200 * Hash(i, 7);
            float top = Painter.H - h;
            p.RoundRect(x, top, w, h + 20, 10, C(166, 180, 218));
            for (float wy = top + 26; wy < Painter.H - 30; wy += 44)
                for (float wx = x + 18; wx < x + w - 24; wx += 30)
                    if (Hash((int)(wx * 7 + wy), 8) > 0.35f) p.RoundRect(wx, wy, 14, 20, 3, C(206, 216, 242));
            x += w + 14;
        }
    }

    private static void Desert(Painter p)
    {
        p.Gradient(C(252, 234, 196), C(240, 210, 160));
        p.Disc(600, 160, 96, C(255, 242, 218, 150));
        p.Disc(600, 160, 66, C(255, 246, 226));
        p.Below(x => 470 + 40 * MathF.Sin(x / 150f + 1), C(244, 218, 172));
        p.Below(x => 600 + 50 * MathF.Sin(x / 120f + 3), C(236, 206, 156));
        p.Below(x => 740 + 30 * MathF.Sin(x / 100f), C(228, 196, 146));
    }

    private static void Savanna(Painter p)
    {
        p.Gradient(C(252, 226, 180), C(238, 212, 150));
        p.Disc(230, 300, 130, C(255, 238, 200, 160));
        p.Disc(230, 300, 96, C(255, 240, 206));
        p.Below(x => 640 + 14 * MathF.Sin(x / 80f), C(220, 200, 132));
        Acacia(p, 540, 640, 1f);
        Acacia(p, 150, 660, 0.7f);
        for (int i = 0; i < 14; i++)
        {
            float x = 30 + i * 56 + 20 * Hash(i, 9), y = 730 + 110 * Hash(i, 10);
            p.Tri(x, y - 22, x - 10, y, x + 10, y, C(206, 186, 116));
        }
    }

    private static void Acacia(Painter p, float x, float ground, float s)
    {
        var bark = C(176, 154, 104);
        p.Capsule(x, ground, x, ground - 90 * s, 7 * s, bark);
        p.Capsule(x, ground - 60 * s, x - 50 * s, ground - 104 * s, 5 * s, bark);
        p.Capsule(x, ground - 60 * s, x + 56 * s, ground - 100 * s, 5 * s, bark);
        p.Ellipse(x, ground - 112 * s, 130 * s, 30 * s, C(184, 166, 106));
    }

    private static void Crag(Painter p)
    {
        p.Gradient(C(214, 210, 204), C(178, 172, 166));
        var tones = new[] { C(196, 190, 184), C(186, 180, 174), C(204, 198, 192), C(176, 170, 164) };
        for (int row = 0; row < 4; row++)
            for (int i = 0; i < 6; i++)
            {
                float x = -40 + i * 150 + (row % 2) * 75 + 30 * Hash(row * 6 + i, 11);
                float y = 250 + row * 200;
                float h = 120 + 80 * Hash(row * 6 + i, 12), w = 90 + 50 * Hash(row * 6 + i, 13);
                p.Tri(x, y - h, x - w, y, x + w * 0.8f, y, tones[(row + i) % tones.Length]);
                p.Tri(x, y - h, x + w * 0.8f, y, x + w * 0.4f, y - h * 0.4f, Shade(tones[(row + i) % tones.Length], 0.06f));
            }
    }

    private static void Volcano(Painter p)
    {
        p.Gradient(C(242, 206, 184), C(212, 160, 138));
        for (int i = 0; i < 6; i++) p.Disc(330 + 90 * Hash(i, 14) + i * 14, 300 - i * 46, 34 + i * 7, C(232, 216, 212, 170));
        p.Tri(384, 390, 0, Painter.H + 60, Painter.W, Painter.H + 60, C(190, 138, 122));
        p.RoundRect(334, 382, 100, 26, 13, C(236, 164, 124));
        p.Tri(384, 390, 300, 520, 360, 520, C(204, 154, 136));
        for (int i = 0; i < 26; i++) p.Disc(60 + 650 * Hash(i, 15), 120 + 340 * Hash(i, 16), 4 + 4 * Hash(i, 17), C(248, 186, 146, 200));
    }

    private static void Snow(Painter p)
    {
        p.Gradient(C(230, 242, 250), C(204, 224, 242));
        for (int i = 0; i < 70; i++) p.Disc(Painter.W * Hash(i, 18), Painter.H * Hash(i, 19), 3 + 5 * Hash(i, 20), C(250, 252, 255, 210));
        for (int i = 0; i < 7; i++)
        {
            float x = 60 + i * 110 + 30 * Hash(i, 21), y = 120 + 600 * Hash(i, 22), r = 18 + 8 * Hash(i, 23);
            for (int k = 0; k < 3; k++)
            {
                float a = k * MathF.PI / 3f;
                p.Capsule(x - r * MathF.Cos(a), y - r * MathF.Sin(a), x + r * MathF.Cos(a), y + r * MathF.Sin(a), 2.5f, C(248, 251, 255, 230));
            }
        }
        p.Below(x => 760 + 24 * MathF.Sin(x / 70f), C(240, 247, 253));
    }

    private static void Cave(Painter p)
    {
        p.Gradient(C(190, 180, 192), C(150, 140, 160));
        // Small crystals in clusters
        for (int i = 0; i < 5; i++)
        {
            float x = 90 + i * 150 + 40 * Hash(i, 24), y = 280 + 380 * Hash(i, 47);
            p.Diamond(x, y, 9, 22, C(212, 202, 224, 210));
            p.Diamond(x + 16, y + 8, 7, 16, C(204, 194, 218, 210));
            p.Diamond(x - 14, y + 10, 6, 13, C(204, 194, 218, 210));
        }
        for (int i = 0; i < 9; i++)
        {
            float x = 20 + i * 92 + 20 * Hash(i, 25);
            p.Tri(x - 30, -10, x + 30, -10, x, 70 + 110 * Hash(i, 26), C(168, 156, 176));
            float bx = 50 + i * 90 + 20 * Hash(i, 27);
            p.Tri(bx - 34, Painter.H + 10, bx + 34, Painter.H + 10, bx, Painter.H - 60 - 100 * Hash(i, 28), C(164, 152, 174));
        }
    }

    private static void Beach(Painter p)
    {
        p.Gradient(C(212, 238, 250), C(196, 230, 246));
        p.Below(x => 300 + 6 * MathF.Sin(x / 60f), C(164, 212, 234));
        for (int row = 0; row < 4; row++)
            for (int i = 0; i < 6; i++)
            {
                float x = 30 + i * 130 + (row % 2) * 65, y = 350 + row * 64;
                p.Capsule(x, y, x + 50, y, 3, C(222, 242, 250, 170));
            }
        p.Below(x => 600 + 40 * MathF.Sin(x / 200f + 2) - 10, C(238, 248, 252, 220));
        p.Below(x => 600 + 40 * MathF.Sin(x / 200f + 2), C(244, 230, 194));
        for (int i = 0; i < 9; i++) p.Ellipse(60 + 680 * Hash(i, 29), 700 + 150 * Hash(i, 30), 12, 8, C(246, 216, 204));
    }

    private static void Seafloor(Painter p)
    {
        p.Gradient(C(150, 206, 222), C(110, 170, 196));
        for (int i = 0; i < 4; i++) p.Tri(120 + i * 180, -10, 60 + i * 180, -10, 20 + i * 200, 700, C(196, 232, 240, 40));
        for (int i = 0; i < 22; i++) p.Ring(Painter.W * Hash(i, 31), 80 + 640 * Hash(i, 32), 6 + 12 * Hash(i, 33), 2.5f, C(216, 242, 248, 170));
        for (int i = 0; i < 6; i++)
        {
            float x = 60 + i * 130 + 30 * Hash(i, 34), h = 140 + 120 * Hash(i, 35);
            for (int k = 0; k < 8; k++)
            {
                float y0 = Painter.H - k * h / 8f, y1 = Painter.H - (k + 1) * h / 8f;
                p.Capsule(x + 12 * MathF.Sin(k * 1.3f), y0, x + 12 * MathF.Sin((k + 1) * 1.3f), y1, 8 - k * 0.6f, C(118, 180, 160));
            }
        }
        p.Below(x => 810 + 14 * MathF.Sin(x / 90f), C(184, 200, 186));
    }

    private static void River(Painter p)
    {
        p.Gradient(C(204, 230, 184), C(178, 214, 162));
        float Middle(float y) => 384 + 150 * MathF.Sin(y / 170f);
        p.Region((x, y) => MathF.Abs(x - Middle(y)) - 132, C(214, 236, 196));
        p.Region((x, y) => MathF.Abs(x - Middle(y)) - 112, C(166, 212, 234));
        for (int i = 0; i < 16; i++)
        {
            float y = 30 + i * 54, x = Middle(y) - 50 + 100 * Hash(i, 36);
            p.Capsule(x - 16, y, x + 16, y, 3, C(214, 236, 248, 190));
        }
        for (int i = 0; i < 18; i++)
        {
            float y = Painter.H * Hash(i, 37), side = Hash(i, 38) > 0.5f ? 1 : -1;
            float x = Middle(y) + side * (180 + 120 * Hash(i, 39));
            p.Ellipse(x, y, 12, 8, C(190, 212, 170));
        }
    }

    private static void Sky(Painter p)
    {
        p.Gradient(C(184, 218, 246), C(226, 240, 252));
        Cloud(p, 180, 200, 56, C(248, 251, 255, 230));
        Cloud(p, 560, 330, 44, C(248, 251, 255, 220));
        Cloud(p, 300, 560, 64, C(248, 251, 255, 230));
        Cloud(p, 640, 740, 40, C(248, 251, 255, 210));
        Cloud(p, 90, 800, 34, C(248, 251, 255, 200));
    }

    private static void Cloud(Painter p, float x, float y, float s, Color c)
    {
        p.Disc(x, y, s, c);
        p.Disc(x - s * 0.95f, y + s * 0.35f, s * 0.68f, c);
        p.Disc(x + s * 0.95f, y + s * 0.3f, s * 0.74f, c);
        p.RoundRect(x - s * 1.5f, y + s * 0.2f, s * 3f, s * 0.75f, s * 0.37f, c);
    }

    private static void PokeCenter(Painter p)
    {
        p.Gradient(C(252, 230, 230), C(244, 210, 214));
        for (int row = 0; row < 7; row++)
            for (int i = 0; i < 6; i++)
            {
                float x = 64 + i * 128 + (row % 2) * 64, y = 60 + row * 128;
                var c = C(255, 242, 242);
                p.RoundRect(x - 26, y - 9, 52, 18, 9, c);
                p.RoundRect(x - 9, y - 26, 18, 52, 9, c);
            }
        p.Region((x, y) => MathF.Abs(y - (Painter.H - 40)) - 14, C(240, 200, 204));
    }

    private static void Machine(Painter p)
    {
        p.Gradient(C(204, 214, 226), C(170, 182, 200));
        var trace = C(156, 170, 192);
        for (int gy = 0; gy < 9; gy++)
            for (int gx = 0; gx < 8; gx++)
            {
                float x = 48 + gx * 96, y = 48 + gy * 96;
                float h = Hash(gy * 8 + gx, 40);
                if (h < 0.55f) p.Capsule(x, y, x + 96, y, 3, trace);
                if (Hash(gy * 8 + gx, 41) < 0.45f) p.Capsule(x, y, x, y + 96, 3, trace);
                if (h > 0.7f) p.Ring(x, y, 9, 4, trace);
                else if (h < 0.15f) p.Disc(x, y, 6, trace);
            }
        foreach (var (x, y) in new[] { (28f, 28f), (Painter.W - 28, 28f), (28f, Painter.H - 28), (Painter.W - 28, Painter.H - 28) })
        {
            p.Disc(x, y, 12, C(186, 196, 212));
            p.Capsule(x - 6, y, x + 6, y, 2, trace);
        }
    }

    private static void Checks(Painter p)
    {
        const float cell = 64;
        p.Gradient(C(214, 226, 248), C(204, 218, 246));
        for (int gy = 0; gy * cell < Painter.H; gy++)
            for (int gx = 0; gx * cell < Painter.W; gx++)
                if ((gx + gy) % 2 == 0) p.Rect(gx * cell, gy * cell, cell, cell, C(192, 208, 242));
    }

    private static void Simple(Painter p)
    {
        p.Gradient(C(238, 242, 248), C(218, 226, 238));
        p.Region((x, y) => MathF.Abs(Painter.RoundBox(x - Painter.W / 2f, y - Painter.H / 2f, Painter.W / 2f - 30, Painter.H / 2f - 30, 20)) - 2,
            C(200, 210, 228));
        for (int i = 0; i < 4; i++) p.Disc(64 + i * 213, Painter.H - 70, 5, C(200, 210, 228));
    }

    // ------------------------------------------------------------------ the eight a word game unlocks

    private static void Distortion(Painter p)
    {
        p.Gradient(C(166, 188, 182), C(120, 140, 146));
        const float cx = 384, cy = 438;
        for (int k = 1; k <= 9; k++)
        {
            int ring = k;
            p.Region((x, y) =>
            {
                float dx = x - cx, dy = y - cy;
                float angle = MathF.Atan2(dy, dx);
                return MathF.Abs(MathF.Sqrt(dx * dx + dy * dy) + 16 * MathF.Sin(angle * 5 + ring) - ring * 64) - 4;
            }, C(186, 208, 200, 170));
        }
        p.Ellipse(150, 200, 70, 22, C(136, 156, 158));
        p.Tri(90, 205, 210, 205, 150, 270, C(136, 156, 158));
        p.Ellipse(620, 680, 56, 18, C(136, 156, 158));
        p.Tri(572, 684, 668, 684, 620, 736, C(136, 156, 158));
    }

    private static void Contest(Painter p)
    {
        p.Gradient(C(252, 232, 240), C(248, 220, 190));
        for (int row = 0; row < 4; row++)
            for (int i = 0; i < 3; i++)
            {
                float x = 130 + i * 256 + (row % 2) * 128, y = 130 + row * 210;
                p.Tri(x - 4, y + 10, x - 34, y + 92, x - 6, y + 82, C(242, 184, 196));
                p.Tri(x + 4, y + 10, x + 34, y + 92, x + 6, y + 82, C(242, 184, 196));
                for (int k = 0; k < 8; k++)
                {
                    float a = k * MathF.PI / 4f;
                    p.Disc(x + 34 * MathF.Cos(a), y + 34 * MathF.Sin(a), 16, C(248, 206, 150));
                }
                p.Disc(x, y, 30, C(252, 238, 214));
                p.Ring(x, y, 22, 3, C(246, 200, 150));
            }
        for (int i = 0; i < 12; i++)
        {
            float x = Painter.W * Hash(i, 42), y = Painter.H * Hash(i, 43);
            p.Diamond(x, y, 4, 14, C(255, 250, 240, 220));
            p.Diamond(x, y, 14, 4, C(255, 250, 240, 220));
        }
    }

    private static void Nostalgic(Painter p)
    {
        // Four tones of a pale olive, in blocks: an old handheld's screen, our own pattern
        const float block = 24;
        var tones = new[] { C(214, 222, 172), C(202, 212, 158), C(188, 200, 144) };
        p.Rect(0, 0, Painter.W, Painter.H, tones[0]);
        for (int by = 0; by * block < Painter.H; by++)
            for (int bx = 0; bx * block < Painter.W; bx++)
            {
                int d = ((bx + by) % 8 + 8) % 8;
                if (d is 0 or 1) p.Rect(bx * block, by * block, block, block, tones[1]);
                else if (d == 4 && (bx / 2 + by) % 3 == 0) p.Rect(bx * block + 6, by * block + 6, block - 12, block - 12, tones[2]);
            }
        for (int i = 0; i * block < Painter.W; i += 2)
        {
            p.Rect(i * block, 0, block, 8, tones[2]);
            p.Rect(i * block, Painter.H - 8, block, 8, tones[2]);
        }
    }

    private static void Croagunk(Painter p)
    {
        p.Gradient(C(176, 188, 226), C(148, 162, 212));
        for (int i = -4; i < 10; i++)
        {
            float x0 = i * 120;
            p.Region((x, y) => MathF.Abs((x - x0) - y * 0.5f) - 14, C(164, 176, 220, 160));
        }
        for (int row = 0; row < 5; row++)
            for (int i = 0; i < 4; i++)
            {
                float x = 100 + i * 192 + (row % 2) * 96, y = 110 + row * 170;
                p.Disc(x, y, 34, C(244, 178, 132));
                p.Disc(x - 10, y - 10, 12, C(250, 208, 172));
                p.Disc(x + 60, y + 52, 6, C(236, 240, 250));
                p.Disc(x + 76, y + 52, 6, C(236, 240, 250));
            }
    }

    private static void Trio(Painter p)
    {
        p.Gradient(C(234, 228, 246), C(214, 208, 238));
        var spots = new[] { (284f, 360f, C(248, 228, 156, 190)), (484f, 360f, C(248, 196, 218, 190)), (384f, 530f, C(170, 208, 246, 190)) };
        foreach (var (x, y, c) in spots) p.Disc(x, y, 170, c);
        foreach (var (x, y, _) in spots)
        {
            float gx = x + (x - 384) * 0.45f, gy = y + (y - 417) * 0.45f;
            p.Disc(gx, gy, 16, C(232, 132, 140));
            p.Disc(gx - 5, gy - 5, 5, C(250, 214, 216));
        }
    }

    private static void PikaPika(Painter p)
    {
        p.Gradient(C(252, 240, 168), C(248, 222, 128));
        for (int row = 0; row < 5; row++)
        {
            float y = 120 + row * 170;
            for (int k = 0; k < 9; k++)
            {
                float x0 = -20 + k * 96, x1 = x0 + 96;
                float y0 = y + (k % 2 == 0 ? -26 : 26), y1 = y + (k % 2 == 0 ? 26 : -26);
                p.Capsule(x0, y0, x1, y1, 10, C(244, 210, 104));
            }
            for (int i = 0; i < 3; i++)
            {
                float x = 120 + i * 256 + (row % 2) * 128;
                p.Disc(x - 24, y + 84, 16, C(244, 160, 136));
                p.Disc(x + 24, y + 84, 16, C(244, 160, 136));
            }
        }
    }

    private static void Legend(Painter p)
    {
        p.Gradient(C(196, 204, 230), C(156, 164, 202));
        for (int i = 0; i < 12; i++)
        {
            float a = i * MathF.PI / 6f;
            p.Tri(384, 438, 384 + 900 * MathF.Cos(a), 438 + 900 * MathF.Sin(a), 384 + 900 * MathF.Cos(a + 0.18f), 438 + 900 * MathF.Sin(a + 0.18f),
                C(214, 220, 240, 70));
        }
        // A diamond, a pearl and a hexagon down the diagonal: the three of the region's legend, as shapes of our own
        p.Diamond(200, 250, 90, 110, C(170, 194, 240));
        p.Diamond(200, 250, 50, 62, C(196, 214, 246));
        p.Disc(384, 450, 84, C(238, 206, 226));
        p.Disc(360, 422, 26, C(248, 232, 240));
        p.Hexagon(568, 650, 86, C(232, 214, 166));
        p.Hexagon(568, 650, 50, C(242, 228, 190));
    }

    private static void Galactic(Painter p)
    {
        p.Gradient(C(124, 148, 168), C(94, 116, 140));
        for (int i = 0; i < 60; i++) p.Disc(Painter.W * Hash(i, 44), Painter.H * Hash(i, 45), 1.5f + 2.5f * Hash(i, 46), C(196, 212, 226, 170));
        p.Region((x, y) => MathF.Abs(Painter.EllipseDistance(x - 384, y - 470, 330, 120)) - 2.5f, C(150, 178, 194, 200));
        p.Region((x, y) => MathF.Abs(Painter.EllipseDistance(x - 384, y - 470, 230, 80)) - 2.5f, C(150, 178, 194, 200));
        p.Disc(384, 470, 70, C(150, 178, 196));
        p.Disc(360, 446, 22, C(168, 192, 208));
        p.Disc(714, 470, 12, C(172, 194, 210));
        p.Disc(154, 470, 9, C(172, 194, 210));
    }

    private static Color Shade(Color c, float t) => PixelCanvas.Mix(c, new Color(60, 50, 70, (int)c.A), t);

    // ------------------------------------------------------------------ the painter

    /// <summary>
    /// Smooth shapes laid one over another on a gradient, each found by its signed distance and blended by how much of
    /// a pixel it covers, so every edge is anti-aliased. Coordinates are layout units of the lining.
    /// </summary>
    private sealed class Painter
    {
        public const float W = Width, H = Height;

        private readonly int width, height;
        private readonly float scale;
        private readonly float[] r, g, b;

        public Painter(float scale)
        {
            this.scale = scale;
            width = Math.Max(1, (int)MathF.Round(W * scale));
            height = Math.Max(1, (int)MathF.Round(H * scale));
            r = new float[width * height];
            g = new float[width * height];
            b = new float[width * height];
        }

        public void Gradient(Color top, Color bottom)
        {
            for (int y = 0; y < height; y++)
            {
                float t = height > 1 ? y / (height - 1f) : 0f;
                float cr = top.R + (bottom.R - top.R) * t, cg = top.G + (bottom.G - top.G) * t, cb = top.B + (bottom.B - top.B) * t;
                for (int x = 0; x < width; x++)
                {
                    int i = y * width + x;
                    r[i] = cr; g[i] = cg; b[i] = cb;
                }
            }
        }

        /// <summary>Whatever the distance says is inside, within a box of layout units (null: everywhere).</summary>
        public void Shape(float x0, float y0, float x1, float y1, Func<float, float, float> distance, Color c)
        {
            int px0 = Math.Clamp((int)MathF.Floor(x0 * scale) - 1, 0, width), px1 = Math.Clamp((int)MathF.Ceiling(x1 * scale) + 1, 0, width);
            int py0 = Math.Clamp((int)MathF.Floor(y0 * scale) - 1, 0, height), py1 = Math.Clamp((int)MathF.Ceiling(y1 * scale) + 1, 0, height);
            float alpha = c.A / 255f;
            for (int py = py0; py < py1; py++)
            {
                float uy = (py + 0.5f) / scale;
                for (int px = px0; px < px1; px++)
                {
                    float d = distance((px + 0.5f) / scale, uy);
                    // An edge a little over one pixel wide
                    float cover = Math.Clamp(0.5f - d * scale / 1.2f, 0f, 1f);
                    if (cover <= 0f) continue;
                    float a = cover * alpha;
                    int i = py * width + px;
                    r[i] += (c.R - r[i]) * a;
                    g[i] += (c.G - g[i]) * a;
                    b[i] += (c.B - b[i]) * a;
                }
            }
        }

        public void Region(Func<float, float, float> distance, Color c) => Shape(0, 0, W, H, distance, c);

        public void Rect(float x, float y, float w, float h, Color c) =>
            Shape(x, y, x + w, y + h, (px, py) => MathF.Max(MathF.Max(x - px, px - (x + w)), MathF.Max(y - py, py - (y + h))), c);

        public void RoundRect(float x, float y, float w, float h, float radius, Color c) =>
            Shape(x, y, x + w, y + h, (px, py) => RoundBox(px - (x + w / 2f), py - (y + h / 2f), w / 2f, h / 2f, radius), c);

        public void Disc(float cx, float cy, float radius, Color c) =>
            Shape(cx - radius, cy - radius, cx + radius, cy + radius, (px, py) => MathF.Sqrt((px - cx) * (px - cx) + (py - cy) * (py - cy)) - radius, c);

        public void Ring(float cx, float cy, float radius, float thickness, Color c) =>
            Shape(cx - radius - thickness, cy - radius - thickness, cx + radius + thickness, cy + radius + thickness,
                (px, py) => MathF.Abs(MathF.Sqrt((px - cx) * (px - cx) + (py - cy) * (py - cy)) - radius) - thickness / 2f, c);

        public void Ellipse(float cx, float cy, float rx, float ry, Color c) =>
            Shape(cx - rx, cy - ry, cx + rx, cy + ry, (px, py) => EllipseDistance(px - cx, py - cy, rx, ry), c);

        /// <summary>A diamond: half its width and half its height from its middle.</summary>
        public void Diamond(float cx, float cy, float hw, float hh, Color c) =>
            Shape(cx - hw, cy - hh, cx + hw, cy + hh, (px, py) => (MathF.Abs(px - cx) / hw + MathF.Abs(py - cy) / hh - 1f) * MathF.Min(hw, hh) * 0.7f, c);

        public void Hexagon(float cx, float cy, float radius, Color c) =>
            Shape(cx - radius, cy - radius, cx + radius, cy + radius, (px, py) =>
            {
                // Inigo Quilez's hexagon, flat at the top
                float x = MathF.Abs(px - cx), y = MathF.Abs(py - cy);
                const float kx = -0.8660254f, ky = 0.5f, kz = 0.57735f;
                float dot = 2f * MathF.Min(kx * x + ky * y, 0f);
                x -= dot * kx;
                y -= dot * ky;
                float inner = radius * 0.8660254f;
                float qx = x - Math.Clamp(x, -kz * inner, kz * inner), qy = y - inner;
                return MathF.Sqrt(qx * qx + qy * qy) * MathF.Sign(qy);
            }, c);

        public void Capsule(float ax, float ay, float bx, float by, float radius, Color c) =>
            Shape(MathF.Min(ax, bx) - radius, MathF.Min(ay, by) - radius, MathF.Max(ax, bx) + radius, MathF.Max(ay, by) + radius, (px, py) =>
            {
                float dx = bx - ax, dy = by - ay;
                float len2 = dx * dx + dy * dy;
                float t = len2 > 0f ? Math.Clamp(((px - ax) * dx + (py - ay) * dy) / len2, 0f, 1f) : 0f;
                float qx = px - ax - dx * t, qy = py - ay - dy * t;
                return MathF.Sqrt(qx * qx + qy * qy) - radius;
            }, c);

        public void Tri(float ax, float ay, float bx, float by, float cx, float cy, Color c) =>
            Shape(MathF.Min(ax, MathF.Min(bx, cx)), MathF.Min(ay, MathF.Min(by, cy)), MathF.Max(ax, MathF.Max(bx, cx)), MathF.Max(ay, MathF.Max(by, cy)),
                (px, py) => TriangleDistance(px, py, ax, ay, bx, by, cx, cy), c);

        /// <summary>Everything below a curve across the whole width.</summary>
        public void Below(Func<float, float> curve, Color c) => Region((x, y) => curve(x) - y, c);

        public static float RoundBox(float x, float y, float hw, float hh, float radius)
        {
            radius = MathF.Min(radius, MathF.Min(hw, hh));
            float qx = MathF.Abs(x) - hw + radius, qy = MathF.Abs(y) - hh + radius;
            float outside = MathF.Sqrt(MathF.Max(qx, 0f) * MathF.Max(qx, 0f) + MathF.Max(qy, 0f) * MathF.Max(qy, 0f));
            return outside + MathF.Min(MathF.Max(qx, qy), 0f) - radius;
        }

        /// <summary>Near enough a distance to an ellipse's edge for drawing it.</summary>
        public static float EllipseDistance(float x, float y, float rx, float ry)
        {
            float k = MathF.Sqrt(x * x / (rx * rx) + y * y / (ry * ry));
            return (k - 1f) * MathF.Min(rx, ry);
        }

        private static float TriangleDistance(float px, float py, float ax, float ay, float bx, float by, float cx, float cy)
        {
            // Inigo Quilez's signed distance to a triangle
            float e0x = bx - ax, e0y = by - ay, e1x = cx - bx, e1y = cy - by, e2x = ax - cx, e2y = ay - cy;
            float v0x = px - ax, v0y = py - ay, v1x = px - bx, v1y = py - by, v2x = px - cx, v2y = py - cy;
            float t0 = Math.Clamp((v0x * e0x + v0y * e0y) / (e0x * e0x + e0y * e0y), 0f, 1f);
            float t1 = Math.Clamp((v1x * e1x + v1y * e1y) / (e1x * e1x + e1y * e1y), 0f, 1f);
            float t2 = Math.Clamp((v2x * e2x + v2y * e2y) / (e2x * e2x + e2y * e2y), 0f, 1f);
            float q0x = v0x - e0x * t0, q0y = v0y - e0y * t0;
            float q1x = v1x - e1x * t1, q1y = v1y - e1y * t1;
            float q2x = v2x - e2x * t2, q2y = v2y - e2y * t2;
            float s = MathF.Sign(e0x * e2y - e0y * e2x);
            float d = MathF.Min(MathF.Min(q0x * q0x + q0y * q0y, q1x * q1x + q1y * q1y), q2x * q2x + q2y * q2y);
            float side = MathF.Min(MathF.Min(s * (v0x * e0y - v0y * e0x), s * (v1x * e1y - v1y * e1x)), s * (v2x * e2y - v2y * e2x));
            return -MathF.Sqrt(d) * MathF.Sign(side);
        }

        /// <summary>The picture, its corners rounded off to nothing (keeping their colour, so it smooths cleanly).</summary>
        public PixelCanvas Finish(float corner)
        {
            var canvas = new PixelCanvas(width, height);
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    int i = y * width + x;
                    float d = RoundBox((x + 0.5f) / scale - W / 2f, (y + 0.5f) / scale - H / 2f, W / 2f, H / 2f, corner);
                    float a = Math.Clamp(0.5f - d * scale, 0f, 1f);
                    canvas.SetRaw(x, y, new Color((byte)Math.Clamp(MathF.Round(r[i]), 0, 255), (byte)Math.Clamp(MathF.Round(g[i]), 0, 255),
                        (byte)Math.Clamp(MathF.Round(b[i]), 0, 255), (byte)MathF.Round(a * 255)));
                }
            return canvas;
        }
    }
}
