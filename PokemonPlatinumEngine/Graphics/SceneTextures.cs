using System;
using System.Collections.Generic;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// Small procedural textures used by the 3D field: tileable materials (shingles, log siding, plaster,
/// stone, water, grass blades) and decals (doors, windows, signs). All are point-filtered pixel art.
/// </summary>
internal static class SceneTextures
{
    private static readonly Dictionary<string, Texture2D> Cache = new();

    private static Texture2D Get(string key, Func<PixelCanvas> build, bool repeat)
    {
        if (Cache.TryGetValue(key, out var tex)) return tex;
        tex = build().ToTexture();
        Raylib.SetTextureWrap(tex, repeat ? TextureWrap.Repeat : TextureWrap.Clamp);
        Cache[key] = tex;
        return tex;
    }

    private static Color Rgb(int r, int g, int b, int a = 255) => new(r, g, b, a);

    private static uint Hash(int x, int y, int salt)
    {
        unchecked
        {
            uint h = (uint)(x * 374761393 + y * 668265263 + salt * 1442695041);
            h = (h ^ (h >> 13)) * 1274126177u;
            return h ^ (h >> 16);
        }
    }

    // ------------------------------------------------------------------ materials

    public static Texture2D White => Get("white", () =>
    {
        var c = new PixelCanvas(4, 4);
        c.Fill(Color.White);
        return c;
    }, repeat: true);

    /// <summary>Light grey shingles, tinted per roof through vertex colors.</summary>
    public static Texture2D Shingles => Get("shingles", () =>
    {
        var c = new PixelCanvas(16, 16);
        for (int y = 0; y < 16; y++)
            for (int x = 0; x < 16; x++)
            {
                int row = y / 4;
                int v = (y % 4) switch { 0 => 252, 3 => 168, _ => 226 };
                if (y % 4 is 1 or 2 && (x + (row % 2) * 4) % 8 == 0) v = 192;
                if ((Hash(x, y, 3) & 15) == 0) v -= 12;
                c.Set(x, y, Rgb(v, v, v));
            }
        return c;
    }, repeat: true);

    /// <summary>Horizontal log siding for Sinnoh's wooden houses.</summary>
    public static Texture2D WoodSiding => Get("wood", () =>
    {
        var c = new PixelCanvas(16, 16);
        var baseCol = Rgb(188, 136, 92);
        for (int y = 0; y < 16; y++)
            for (int x = 0; x < 16; x++)
            {
                var col = (y % 4) switch
                {
                    0 => Rgb(218, 170, 120),
                    3 => Rgb(116, 78, 54),
                    _ => baseCol
                };
                if (y % 4 is 1 or 2 && (Hash(x, y, 5) & 7) == 0) col = PixelCanvas.Shadow(baseCol, 0.18f);
                c.Set(x, y, col);
            }
        // Log ends where planks meet
        for (int row = 0; row < 4; row++)
        {
            int jx = (row * 5 + 3) % 16;
            c.VLine(jx, row * 4 + 1, 2, Rgb(140, 96, 64));
        }
        return c;
    }, repeat: true);

    public static Texture2D Plaster => Get("plaster", () =>
    {
        var c = new PixelCanvas(16, 16);
        c.Fill(Rgb(244, 242, 236));
        for (int i = 0; i < 18; i++)
        {
            c.Set((int)(Hash(i, 1, 7) % 16), (int)(Hash(i, 2, 7) % 16), Rgb(228, 226, 220));
        }
        return c;
    }, repeat: true);

    public static Texture2D Stone => Get("stone", () =>
    {
        var c = new PixelCanvas(16, 16);
        var stone = Rgb(172, 166, 162);
        var mortar = Rgb(118, 112, 112);
        c.Fill(stone);
        for (int y = 0; y < 16; y += 4)
        {
            c.HLine(0, y + 3, 16, mortar);
            c.HLine(0, y, 16, PixelCanvas.Light1(stone, 0.35f));
            int off = (y / 4) % 2 * 4;
            for (int x = off; x < 16; x += 8) c.VLine(x, y, 3, mortar);
        }
        return c;
    }, repeat: true);

    /// <summary>Blades of tall grass with a transparent sky above them (cut out by the field shader).</summary>
    public static Texture2D GrassBlades => Get("grass_blades", () =>
    {
        var c = new PixelCanvas(16, 16);
        var dark = Rgb(34, 116, 64);
        var mid = Rgb(62, 162, 84);
        var light = Rgb(110, 202, 100);
        var tip = Rgb(170, 234, 128);
        for (int x = 0; x < 16; x++)
        {
            int h = 8 + (int)(Hash(x, 0, 11) % 7);
            if (x % 2 == 1) h -= 2;
            for (int y = 0; y < h; y++)
            {
                float t = y / (float)h;
                var col = t < 0.35f ? dark : t < 0.7f ? mid : light;
                if (y == h - 1) col = tip;
                if (x % 4 == 3 && t > 0.3f) col = PixelCanvas.Shadow(col, 0.25f);
                c.Set(x, 15 - y, col);
            }
        }
        c.Rect(0, 13, 16, 3, dark);
        return c;
    }, repeat: true);

    public static Texture2D Water => Get("water", () =>
    {
        const int s = 32;
        var c = new PixelCanvas(s, s);
        var baseCol = Rgb(84, 166, 238);
        c.Fill(baseCol);
        for (int i = 0; i < 9; i++)
        {
            int wx = (int)(Hash(i, 0, 13) % s), wy = (int)(Hash(i, 1, 13) % s);
            for (int k = 0; k < 5; k++)
            {
                int x = (wx + k) % s;
                int y = (wy + (k == 0 || k == 4 ? 1 : 0)) % s;
                c.Set(x, y, Rgb(168, 220, 250));
                c.Set(x, (y + 1) % s, Rgb(66, 140, 220));
            }
        }
        for (int i = 0; i < 12; i++)
        {
            c.Set((int)(Hash(i, 2, 13) % s), (int)(Hash(i, 3, 13) % s), Rgb(120, 192, 246));
        }
        return c;
    }, repeat: true);

    // ------------------------------------------------------------------ decals

    public static Texture2D DoorWood => Get("door_wood", () =>
    {
        var c = new PixelCanvas(16, 28);
        var frame = Rgb(96, 66, 50);
        var wood = Rgb(170, 110, 66);
        c.Fill(frame);
        c.Rect(2, 2, 12, 26, wood);
        c.Rect(4, 4, 8, 6, Rgb(150, 206, 244));
        c.Set(5, 5, Color.White); c.Set(6, 5, Color.White); c.Set(5, 6, Color.White);
        c.VLine(8, 4, 6, frame);
        c.Rect(4, 13, 3, 11, PixelCanvas.Shadow(wood, 0.2f));
        c.Rect(9, 13, 3, 11, PixelCanvas.Shadow(wood, 0.2f));
        c.Set(11, 17, Rgb(250, 214, 80));
        c.VLine(2, 2, 26, PixelCanvas.Light1(wood, 0.25f));
        return c;
    }, repeat: false);

    /// <summary>Automatic sliding glass doors used by Pokémon Centers and Marts.</summary>
    public static Texture2D DoorGlass => Get("door_glass", () =>
    {
        var c = new PixelCanvas(24, 28);
        var frame = Rgb(196, 202, 214);
        var glass = Rgb(118, 188, 238);
        c.Fill(frame);
        c.Rect(2, 2, 20, 26, glass);
        c.VLine(11, 2, 26, frame);
        c.VLine(12, 2, 26, PixelCanvas.Shadow(frame, 0.3f));
        for (int i = 0; i < 6; i++)
        {
            c.Set(4 + i, 14 - i, Rgb(220, 242, 255));
            c.Set(15 + i, 20 - i, Rgb(220, 242, 255));
        }
        c.HLine(2, 2, 20, PixelCanvas.Shadow(glass, 0.3f));
        return c;
    }, repeat: false);

    public static Texture2D Window => Get("window", () =>
    {
        var c = new PixelCanvas(16, 12);
        var frame = Rgb(248, 248, 250);
        var glass = Rgb(140, 202, 246);
        c.Fill(Rgb(96, 84, 84));
        c.Rect(1, 1, 14, 10, frame);
        c.Rect(2, 2, 12, 7, glass);
        c.VLine(8, 2, 7, frame);
        c.HLine(2, 5, 12, frame);
        c.Set(3, 3, Color.White); c.Set(4, 3, Color.White); c.Set(3, 4, Color.White);
        c.Set(9, 3, Color.White); c.Set(10, 3, Color.White);
        c.HLine(1, 9, 14, Rgb(206, 206, 210));
        return c;
    }, repeat: false);

    public static Texture2D GableWindow => Get("gable_window", () =>
    {
        var c = new PixelCanvas(12, 12);
        c.FlatEllipse(6, 6, 5.5f, 5.5f, Rgb(248, 248, 250));
        c.FlatEllipse(6, 6, 3.8f, 3.8f, Rgb(140, 202, 246));
        c.VLine(6, 2, 8, Rgb(248, 248, 250));
        c.HLine(2, 6, 8, Rgb(248, 248, 250));
        c.Set(4, 4, Color.White);
        c.OutlinePass(innerSeams: false);
        return c;
    }, repeat: false);

    /// <summary>The Poké Ball emblem that sits on the front of every Pokémon Center roof.</summary>
    public static Texture2D CenterEmblem => Get("center_emblem", () =>
    {
        var c = new PixelCanvas(32, 32);
        var red = Rgb(236, 64, 56);
        var white = Rgb(250, 250, 252);
        var dark = Rgb(44, 36, 52);
        for (int y = 0; y < 32; y++)
            for (int x = 0; x < 32; x++)
            {
                float u = (x + 0.5f - 16f) / 15f, v = (y + 0.5f - 16f) / 15f;
                float d = u * u + v * v;
                if (d > 1f) continue;
                var col = y < 16 ? red : white;
                if (u < -0.35f && v < -0.3f && d > 0.45f && d < 0.75f && y < 16) col = PixelCanvas.Light1(red, 0.5f);
                c.Set(x, y, col);
            }
        c.Rect(1, 14, 30, 4, dark);
        c.Disc(16, 16, 6.5f, dark);
        c.Disc(16, 16, 4.2f, white);
        c.OutlinePass(innerSeams: false);
        return c;
    }, repeat: false);

    public static Texture2D MartSign => Get("mart_sign", () =>
    {
        var c = new PixelCanvas(40, 16);
        var blue = Rgb(60, 116, 222);
        c.Rect(1, 1, 38, 14, blue);
        c.HLine(1, 1, 38, PixelCanvas.Light1(blue, 0.35f));
        c.HLine(1, 14, 38, PixelCanvas.Shadow(blue, 0.35f));
        string[][] glyphs =
        {
            new[] { "X...X", "XX.XX", "X.X.X", "X...X", "X...X", "X...X", "X...X" },
            new[] { ".XXX.", "X...X", "X...X", "XXXXX", "X...X", "X...X", "X...X" },
            new[] { "XXXX.", "X...X", "X...X", "XXXX.", "X.X..", "X..X.", "X...X" },
            new[] { "XXXXX", "..X..", "..X..", "..X..", "..X..", "..X..", "..X.." }
        };
        for (int i = 0; i < glyphs.Length; i++)
        {
            c.Stamp(6 + i * 8, 4, glyphs[i], ch => ch == 'X' ? Color.White : null);
        }
        c.OutlinePass(innerSeams: false);
        return c;
    }, repeat: false);

    public static Texture2D SignBoard => Get("sign_board", () =>
    {
        var c = new PixelCanvas(16, 10);
        var wood = Rgb(202, 148, 92);
        c.Fill(PixelCanvas.Shadow(wood, 0.45f));
        c.Rect(1, 1, 14, 8, wood);
        c.HLine(1, 1, 14, PixelCanvas.Light1(wood, 0.35f));
        c.HLine(3, 4, 10, PixelCanvas.Shadow(wood, 0.45f));
        c.HLine(3, 6, 7, PixelCanvas.Shadow(wood, 0.45f));
        return c;
    }, repeat: false);

    public static Texture2D Plaque => Get("plaque", () =>
    {
        var c = new PixelCanvas(12, 8);
        var brass = Rgb(214, 172, 96);
        c.Fill(Rgb(92, 66, 50));
        c.Rect(1, 1, 10, 6, brass);
        c.HLine(1, 1, 10, PixelCanvas.Light1(brass, 0.4f));
        c.HLine(3, 4, 6, PixelCanvas.Shadow(brass, 0.45f));
        return c;
    }, repeat: false);

    public static Texture2D MartGoods => Get("mart_goods", () =>
    {
        var c = new PixelCanvas(16, 16);
        c.Fill(Rgb(206, 214, 226));
        Color[] goods = { Rgb(232, 80, 80), Rgb(80, 150, 232), Rgb(250, 206, 72), Rgb(120, 200, 110) };
        for (int shelf = 0; shelf < 2; shelf++)
        {
            int y = 2 + shelf * 7;
            c.HLine(0, y + 5, 16, Rgb(120, 132, 156));
            for (int i = 0; i < 4; i++)
            {
                var g = goods[(i + shelf) % goods.Length];
                c.Rect(1 + i * 4, y, 3, 5, g);
                c.HLine(1 + i * 4, y, 3, PixelCanvas.Light1(g, 0.4f));
            }
        }
        return c;
    }, repeat: true);

    public static Texture2D PcScreen => Get("pc_screen", () =>
    {
        var c = new PixelCanvas(12, 10);
        c.Fill(Rgb(206, 210, 222));
        c.Rect(1, 1, 10, 7, Rgb(64, 128, 216));
        c.HLine(2, 2, 4, Rgb(170, 220, 250));
        c.HLine(2, 4, 6, Rgb(130, 190, 246));
        c.Set(10, 8, Rgb(90, 220, 120));
        return c;
    }, repeat: false);

    /// <summary>Soft round shadow (drawn alpha-blended, not cut out).</summary>
    public static Texture2D ShadowBlob => Get("shadow_blob", () =>
    {
        var c = new PixelCanvas(32, 32);
        for (int y = 0; y < 32; y++)
            for (int x = 0; x < 32; x++)
            {
                float u = (x + 0.5f - 16f) / 16f, v = (y + 0.5f - 16f) / 16f;
                float d = MathF.Sqrt(u * u + v * v);
                if (d < 1f) c.Set(x, y, new Color(0, 0, 0, (int)(115 * Math.Clamp((1f - d) * 2.2f, 0f, 1f))));
            }
        return c;
    }, repeat: false);

    public static Texture2D Exclamation => Get("exclamation", () =>
    {
        var c = new PixelCanvas(16, 16);
        c.FlatEllipse(8, 8, 7, 7, Color.White);
        var red = Rgb(226, 56, 56);
        c.Rect(7, 3, 2, 6, red);
        c.Rect(7, 10, 2, 2, red);
        c.OutlinePass(innerSeams: false);
        return c;
    }, repeat: false);
}
