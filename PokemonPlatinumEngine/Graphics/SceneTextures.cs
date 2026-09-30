using System;
using System.Collections.Generic;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// Procedural textures for the 3D field at 32 texels per tile: tileable materials, foliage with cut-out
/// edges, and decals for doors, windows, signs and furniture. All are point-filtered pixel art.
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

    private static float R(int x, int y, int salt) => (Hash(x, y, salt) & 0xFFFF) / 65536f;

    // ------------------------------------------------------------------ materials

    public static Texture2D White => Get("white", () =>
    {
        var c = new PixelCanvas(4, 4);
        c.Fill(Color.White);
        return c;
    }, repeat: true);

    /// <summary>Light grey shingles, tinted per roof through vertex colors. Rows run along the texture's U axis.</summary>
    public static Texture2D Shingles => Get("shingles", () =>
    {
        var c = new PixelCanvas(32, 32);
        for (int row = 0; row < 4; row++)
        {
            int y0 = row * 8;
            int offset = row % 2 * 5;
            for (int x = 0; x < 32; x++)
            {
                int shingle = (x + offset) / 10;
                float tone = 214 + R(shingle, row, 3) * 30f;
                for (int y = 0; y < 8; y++)
                {
                    float v = tone;
                    if (y == 0) v += 26;
                    else if (y == 1) v += 12;
                    else if (y >= 6) v -= 34 + (y - 6) * 20;
                    if ((x + offset) % 10 == 0 && y < 7) v -= 44;
                    if ((x + offset) % 10 == 1 && y < 7) v += 10;
                    v += (R(x, y0 + y, 4) - 0.5f) * 10f;
                    int g = (int)Math.Clamp(v, 0, 255);
                    c.Set(x, y0 + y, Rgb(g, g, g));
                }
            }
        }
        return c;
    }, repeat: true);

    /// <summary>Horizontal log siding for Sinnoh's wooden houses: rounded logs with grain and dark joints.</summary>
    public static Texture2D WoodSiding => Get("wood", () =>
    {
        var c = new PixelCanvas(32, 32);
        var wood = Rgb(190, 136, 90);
        for (int log = 0; log < 4; log++)
        {
            int y0 = log * 8;
            float logTone = 0.92f + R(log, 0, 5) * 0.14f;
            for (int y = 0; y < 8; y++)
            {
                // Round profile: bright on the upper curve, darker below, a dark gap between logs
                float shade = y switch { 0 => 1.18f, 1 => 1.1f, 2 => 1.02f, 3 => 0.97f, 4 => 0.92f, 5 => 0.84f, 6 => 0.7f, _ => 0.42f };
                for (int x = 0; x < 32; x++)
                {
                    float grain = (R(x / 3, y0 + y, 6) - 0.5f) * 0.1f;
                    if (y is > 0 and < 6 && R(x, y0 + y, 7) < 0.08f) grain -= 0.12f;
                    c.Set(x, y0 + y, MeshBuilder.Scale(wood, shade * logTone + grain));
                }
            }
            // A log end where two logs meet, staggered per row
            int jx = (log * 13 + 6) % 32;
            for (int y = 1; y < 7; y++)
            {
                c.Set(jx, y0 + y, MeshBuilder.Scale(wood, 0.55f));
                c.Set((jx + 1) % 32, y0 + y, MeshBuilder.Scale(wood, 1.12f));
            }
        }
        return c;
    }, repeat: true);

    public static Texture2D Plaster => Get("plaster", () =>
    {
        var c = new PixelCanvas(32, 32);
        for (int y = 0; y < 32; y++)
            for (int x = 0; x < 32; x++)
            {
                int v = 238 + (int)((R(x, y, 8) - 0.5f) * 10) + (int)((R(x / 4, y / 4, 9) - 0.5f) * 8);
                c.Set(x, y, Rgb(v, v - 2, v - 8));
            }
        return c;
    }, repeat: true);

    public static Texture2D Stone => Get("stone", () =>
    {
        var c = new PixelCanvas(32, 32);
        var mortar = Rgb(104, 100, 104);
        c.Fill(mortar);
        for (int row = 0; row < 4; row++)
        {
            int y0 = row * 8;
            int x = row % 2 * 5;
            int i = 0;
            while (x < 32 + 12)
            {
                int w = 7 + (int)(R(i, row, 10) * 7);
                float tone = 0.86f + R(i, row, 11) * 0.22f;
                var stone = MeshBuilder.Scale(Rgb(172, 166, 160), tone);
                for (int yy = 1; yy < 7; yy++)
                    for (int xx = 1; xx < w; xx++)
                    {
                        var col = stone;
                        if (yy == 1 || xx == 1) col = MeshBuilder.Scale(stone, 1.14f);
                        else if (yy == 6 || xx == w - 1) col = MeshBuilder.Scale(stone, 0.8f);
                        c.Set((x + xx) % 32, y0 + yy, col);
                    }
                x += w;
                i++;
            }
        }
        return c;
    }, repeat: true);

    public static Texture2D Bark => Get("bark", () =>
    {
        var c = new PixelCanvas(32, 32);
        var bark = Rgb(116, 80, 54);
        for (int y = 0; y < 32; y++)
            for (int x = 0; x < 32; x++)
            {
                float stripe = (R(x, y / 5, 12) - 0.5f) * 0.4f + ((x % 5 == 0) ? -0.25f : 0f);
                c.Set(x, y, MeshBuilder.Scale(bark, 1f + stripe));
            }
        return c;
    }, repeat: true);

    /// <summary>Dense leaf clusters for the solid core of round tree crowns (tinted per tree).</summary>
    public static Texture2D Leaves => Get("leaves", () => BuildLeaves(cutout: false), repeat: true);

    /// <summary>Leaf clusters with gaps, for the outer shell that gives crowns a leafy silhouette.</summary>
    public static Texture2D LeafShell => Get("leaf_shell", () => BuildLeaves(cutout: true), repeat: true);

    private static PixelCanvas BuildLeaves(bool cutout)
    {
        const int s = 64;
        var c = new PixelCanvas(s, s);
        if (!cutout) c.Fill(Rgb(150, 150, 150));
        for (int i = 0; i < (cutout ? 90 : 170); i++)
        {
            float cx = R(i, 0, 13) * s, cy = R(i, 1, 13) * s;
            float r = 2.2f + R(i, 2, 13) * 2.4f;
            int tone = 150 + (int)(R(i, 3, 13) * 100);
            for (int y = (int)(cy - r - 1); y <= (int)(cy + r + 1); y++)
                for (int x = (int)(cx - r - 1); x <= (int)(cx + r + 1); x++)
                {
                    float u = (x + 0.5f - cx) / r, v = (y + 0.5f - cy) / (r * 0.8f);
                    if (u * u + v * v > 1f) continue;
                    // Light from the upper left on every leaf
                    int t = tone + (int)((-u - v) * 22f);
                    int px = ((x % s) + s) % s, py = ((y % s) + s) % s;
                    c.Set(px, py, Rgb(Math.Clamp(t, 60, 255), Math.Clamp(t, 60, 255), Math.Clamp(t, 60, 255)));
                }
        }
        return c;
    }

    /// <summary>
    /// Pine tier: U wraps around the tree, V runs from the tip (0) to the rim (1). The rim is serrated with
    /// transparent notches so each tier has a needly edge.
    /// </summary>
    public static Texture2D Needles => Get("needles", () =>
    {
        const int w = 64, h = 32;
        var c = new PixelCanvas(w, h);
        for (int x = 0; x < w; x++)
        {
            // Jagged hanging tips along the rim
            int tip = 23 + (int)(MathF.Abs(MathF.Sin(x * 0.9f)) * 6f + R(x, 0, 14) * 3f);
            for (int y = 0; y < Math.Min(h, tip); y++)
            {
                float t = y / (float)h;
                int v = (int)(220 - t * 90);
                if ((x + y) % 5 == 0) v -= 26;
                if ((x * 3 + y * 2) % 11 == 0) v += 20;
                v += (int)((R(x, y, 15) - 0.5f) * 18);
                c.Set(x, y, Rgb(Math.Clamp(v, 40, 255), Math.Clamp(v, 40, 255), Math.Clamp(v, 40, 255)));
            }
        }
        return c;
    }, repeat: true);

    /// <summary>Blades of tall grass with transparent gaps above (cut out by the field shader).</summary>
    public static Texture2D GrassBlades => Get("grass_blades", () =>
    {
        var c = new PixelCanvas(32, 32);
        for (int i = 0; i < 44; i++)
        {
            int x = (int)(R(i, 0, 16) * 32);
            int h = 14 + (int)(R(i, 1, 16) * 16);
            float lean = (R(i, 2, 16) - 0.5f) * 0.5f;
            for (int y = 0; y < h; y++)
            {
                float t = y / (float)h;
                int px = ((x + (int)(lean * y)) % 32 + 32) % 32;
                var col = t < 0.3f ? Rgb(30, 104, 58) : t < 0.65f ? Rgb(56, 150, 76) : t < 0.9f ? Rgb(96, 192, 94) : Rgb(160, 228, 124);
                c.Set(px, 31 - y, col);
                if (t < 0.6f) c.Set((px + 1) % 32, 31 - y, MeshBuilder.Scale(col, 0.82f));
            }
        }
        c.Rect(0, 26, 32, 6, Rgb(30, 102, 58));
        return c;
    }, repeat: true);

    /// <summary>A small clump of grass for scattering over lawns.</summary>
    public static Texture2D GrassTuft => Get("grass_tuft", () =>
    {
        var c = new PixelCanvas(32, 32);
        for (int i = 0; i < 12; i++)
        {
            float x0 = 16 + (i - 6) * 1.5f;
            float lean = (i - 6) * 0.45f;
            int h = 12 + (int)(R(i, 0, 17) * 12);
            for (int y = 0; y < h; y++)
            {
                float t = y / (float)h;
                int px = (int)(x0 + lean * t * 6f);
                var col = t < 0.4f ? Rgb(64, 150, 80) : t < 0.8f ? Rgb(100, 196, 104) : Rgb(170, 234, 140);
                c.Set(px, 31 - y, col);
            }
        }
        return c;
    }, repeat: false);

    /// <summary>A cluster of white (and the odd red) flowers on stems, like Platinum's flower beds.</summary>
    public static Texture2D FlowerTuft => Get("flower_tuft", () =>
    {
        var c = new PixelCanvas(32, 32);
        var stem = Rgb(74, 160, 80);
        var spots = new[] { (8, 12, 0), (17, 8, 1), (24, 14, 2), (13, 18, 3) };
        foreach (var (fx, fy, i) in spots)
        {
            c.Line(fx, fy + 2, fx + (i % 2 == 0 ? 1 : -1), 31, stem);
            bool red = i == 1;
            var petal = red ? Rgb(238, 84, 96) : Rgb(252, 252, 252);
            var shade = red ? Rgb(190, 56, 70) : Rgb(200, 216, 238);
            c.FlatEllipse(fx + 0.5f, fy + 0.5f, 3.2f, 3.2f, petal);
            c.Set(fx, fy + 2, shade); c.Set(fx + 1, fy + 2, shade); c.Set(fx + 2, fy + 1, shade);
            c.Rect(fx, fy, 2, 2, Rgb(250, 204, 70));
        }
        c.FlatEllipse(12, 28, 6, 3, stem);
        c.FlatEllipse(22, 29, 5, 2.5f, stem);
        return c;
    }, repeat: false);

    public static Texture2D Water => Get("water", () =>
    {
        const int s = 64;
        var c = new PixelCanvas(s, s);
        var baseCol = Rgb(74, 158, 234);
        for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float n = MathF.Sin(x * 0.2f + MathF.Sin(y * 0.15f) * 2f) * 0.5f + MathF.Sin(y * 0.33f + x * 0.05f) * 0.5f;
                c.Set(x, y, MeshBuilder.Scale(baseCol, 0.94f + n * 0.06f));
            }
        for (int i = 0; i < 26; i++)
        {
            int wx = (int)(R(i, 0, 19) * s), wy = (int)(R(i, 1, 19) * s);
            int len = 4 + (int)(R(i, 2, 19) * 6);
            for (int k = 0; k < len; k++)
            {
                int x = (wx + k) % s;
                int y = (wy + (k == 0 || k == len - 1 ? 1 : 0)) % s;
                c.Set(x, y, Rgb(176, 224, 252));
                c.Set(x, (y + 1) % s, Rgb(58, 132, 214));
            }
        }
        return c;
    }, repeat: true);

    /// <summary>Smooth noise that wraps every <paramref name="period"/> cells, for seamless tiling textures.</summary>
    private static float TileNoise(float x, float y, int period, int salt)
    {
        int x0 = (int)MathF.Floor(x), y0 = (int)MathF.Floor(y);
        float fx = x - x0, fy = y - y0;
        fx = fx * fx * (3 - 2 * fx);
        fy = fy * fy * (3 - 2 * fy);
        float H(int ix, int iy) => R(((ix % period) + period) % period, ((iy % period) + period) % period, salt);
        float a = H(x0, y0), b = H(x0 + 1, y0), c = H(x0, y0 + 1), d = H(x0 + 1, y0 + 1);
        return a + (b - a) * fx + (c - a) * fy + (a - b - c + d) * fx * fy;
    }

    /// <summary>Seamless 128px meadow for the battle field (covers four world units).</summary>
    public static Texture2D Meadow => ArtLook.ModelBattle ? SoftTextures.Meadow : Get("meadow", () =>
    {
        const int s = 128;
        var c = new PixelCanvas(s, s);
        var baseCol = Rgb(122, 208, 124);
        var dark = PixelCanvas.Shadow(baseCol, 0.2f);
        var light = PixelCanvas.Light1(baseCol, 0.35f);
        for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float n = TileNoise(x / 32f, y / 32f, 4, 30) * 0.65f + TileNoise(x / 8f, y / 8f, 16, 31) * 0.35f;
                var col = n < 0.45f ? PixelCanvas.Mix(baseCol, dark, (0.45f - n) * 1.4f) : PixelCanvas.Mix(baseCol, light, (n - 0.45f) * 0.8f);
                c.Set(x, y, col);
            }
        for (int i = 0; i < 140; i++)
        {
            int bx = (int)(R(i, 0, 32) * s), by = (int)(R(i, 1, 32) * s);
            int h = 3 + (int)(R(i, 2, 32) * 3);
            for (int k = 0; k < h; k++)
                c.Set((bx + (k == h - 1 ? 1 : 0)) % s, ((by - k) % s + s) % s, k == h - 1 ? light : dark);
        }
        return c;
    }, repeat: true);

    /// <summary>Top of a battle platform: lush grass, lighter in the middle with a worn ring near the rim.</summary>
    public static Texture2D PlatformTop => ArtLook.ModelBattle ? SoftTextures.PlatformTop : Get("platform_top", () =>
    {
        const int s = 128;
        var c = new PixelCanvas(s, s);
        var inner = Rgb(150, 226, 138);
        var outer = Rgb(104, 186, 104);
        for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float u = (x + 0.5f) / s * 2f - 1f, v = (y + 0.5f) / s * 2f - 1f;
                float d = MathF.Sqrt(u * u + v * v);
                var col = PixelCanvas.Mix(inner, outer, Math.Clamp((d - 0.25f) / 0.6f, 0f, 1f));
                col = PixelCanvas.Mix(col, PixelCanvas.Shadow(col, 0.2f), TileNoise(x / 10f, y / 10f, 13, 40) * 0.5f);
                if (d > 0.8f && d < 0.86f) col = PixelCanvas.Shadow(col, 0.14f);
                if (d > 0.93f) col = PixelCanvas.Shadow(outer, 0.18f);
                c.Set(x, y, col);
            }
        for (int i = 0; i < 90; i++)
        {
            float a = R(i, 0, 41) * MathF.Tau, rr = MathF.Sqrt(R(i, 1, 41)) * 0.9f;
            int bx = (int)((0.5f + MathF.Cos(a) * rr * 0.5f) * s), by = (int)((0.5f + MathF.Sin(a) * rr * 0.5f) * s);
            c.Set(bx, by, PixelCanvas.Light1(inner, 0.35f));
            c.Set(bx, by + 1, PixelCanvas.Shadow(outer, 0.2f));
        }
        return c;
    }, repeat: false);

    // ------------------------------------------------------------------ decals

    public static Texture2D DoorWood => Get("door_wood", () =>
    {
        var c = new PixelCanvas(32, 56);
        var frame = Rgb(92, 62, 46);
        var wood = Rgb(172, 112, 66);
        c.Fill(frame);
        c.Rect(3, 3, 26, 53, wood);
        // Arched window in the upper half
        c.Rect(8, 8, 16, 12, Rgb(146, 204, 244));
        c.FlatEllipse(16, 8.5f, 8, 4.5f, Rgb(146, 204, 244));
        c.VLine(16, 5, 15, frame);
        c.HLine(8, 13, 16, frame);
        c.Rect(10, 8, 3, 3, Rgb(226, 244, 255));
        // Raised panels
        foreach (int px in new[] { 7, 18 })
        {
            c.Rect(px, 25, 8, 26, MeshBuilder.Scale(wood, 0.84f));
            c.HLine(px, 25, 8, MeshBuilder.Scale(wood, 1.18f));
            c.VLine(px, 25, 26, MeshBuilder.Scale(wood, 1.12f));
        }
        c.Disc(24, 36, 1.6f, Rgb(250, 214, 80));
        c.VLine(3, 3, 53, MeshBuilder.Scale(wood, 1.2f));
        return c;
    }, repeat: false);

    /// <summary>Automatic sliding glass doors used by Pokémon Centers and Marts.</summary>
    public static Texture2D DoorGlass => Get("door_glass", () =>
    {
        var c = new PixelCanvas(48, 56);
        var frame = Rgb(200, 206, 218);
        var glass = Rgb(112, 184, 236);
        c.Fill(frame);
        for (int y = 3; y < 56; y++)
        {
            var g = PixelCanvas.Mix(Rgb(150, 208, 246), glass, y / 56f);
            c.HLine(3, y, 42, g);
        }
        c.VLine(23, 3, 53, frame);
        c.VLine(24, 3, 53, PixelCanvas.Shadow(frame, 0.3f));
        for (int i = 0; i < 12; i++)
        {
            c.Set(6 + i, 30 - i, Rgb(226, 244, 255));
            c.Set(7 + i, 30 - i, Rgb(226, 244, 255));
            c.Set(30 + i, 40 - i, Rgb(226, 244, 255));
        }
        c.HLine(3, 3, 42, PixelCanvas.Shadow(glass, 0.3f));
        c.Rect(19, 27, 3, 6, Rgb(150, 156, 170));
        c.Rect(26, 27, 3, 6, Rgb(150, 156, 170));
        return c;
    }, repeat: false);

    public static Texture2D Window => Get("window", () =>
    {
        var c = new PixelCanvas(32, 24);
        var glass = Rgb(136, 198, 244);
        for (int y = 0; y < 24; y++) c.HLine(0, y, 32, PixelCanvas.Mix(Rgb(186, 226, 252), glass, y / 24f));
        // Reflections, mullions and a pair of curtains
        for (int i = 0; i < 8; i++) { c.Set(4 + i, 12 - i, Color.White); c.Set(5 + i, 12 - i, Color.White); }
        c.VLine(15, 0, 24, Rgb(248, 248, 250));
        c.VLine(16, 0, 24, Rgb(248, 248, 250));
        c.HLine(0, 11, 32, Rgb(248, 248, 250));
        var curtain = Rgb(244, 236, 214);
        c.FlatPoly(curtain, 0, 0, 7, 0, 3, 24, 0, 24);
        c.FlatPoly(curtain, 32, 0, 25, 0, 29, 24, 32, 24);
        return c;
    }, repeat: false);

    public static Texture2D GableWindow => Get("gable_window", () =>
    {
        var c = new PixelCanvas(24, 24);
        c.FlatEllipse(12, 12, 11.5f, 11.5f, Rgb(248, 248, 250));
        c.FlatEllipse(12, 12, 8.5f, 8.5f, Rgb(140, 202, 246));
        c.Rect(11, 3, 2, 18, Rgb(248, 248, 250));
        c.Rect(3, 11, 18, 2, Rgb(248, 248, 250));
        c.Rect(7, 7, 3, 3, Color.White);
        c.OutlinePass(innerSeams: false);
        return c;
    }, repeat: false);

    /// <summary>The Poké Ball emblem on the front of every Pokémon Center.</summary>
    public static Texture2D CenterEmblem => Get("center_emblem", () =>
    {
        var c = new PixelCanvas(64, 64);
        var red = Rgb(236, 64, 56);
        var white = Rgb(250, 250, 252);
        var dark = Rgb(44, 36, 52);
        for (int y = 0; y < 64; y++)
            for (int x = 0; x < 64; x++)
            {
                float u = (x + 0.5f - 32f) / 30f, v = (y + 0.5f - 32f) / 30f;
                float d = u * u + v * v;
                if (d > 1f) continue;
                var col = y < 32 ? red : white;
                float shade = 1f - (u + v) * 0.12f;
                col = MeshBuilder.Scale(col, shade);
                if (u < -0.3f && v < -0.25f && d > 0.4f && d < 0.72f && y < 32) col = PixelCanvas.Light1(red, 0.55f);
                c.Set(x, y, col);
            }
        c.Rect(2, 29, 60, 7, dark);
        c.Disc(32, 32, 13, dark);
        c.Disc(32, 32, 9, white);
        c.Disc(32, 32, 5, Rgb(226, 230, 238));
        c.OutlinePass(innerSeams: false);
        return c;
    }, repeat: false);

    public static Texture2D MartSign => Get("mart_sign", () =>
    {
        var c = new PixelCanvas(80, 32);
        var blue = Rgb(60, 116, 222);
        for (int y = 2; y < 30; y++) c.HLine(2, y, 76, PixelCanvas.Mix(PixelCanvas.Light1(blue, 0.25f), PixelCanvas.Shadow(blue, 0.2f), y / 30f));
        string[][] glyphs =
        {
            new[] { "X...X", "XX.XX", "X.X.X", "X...X", "X...X", "X...X", "X...X" },
            new[] { ".XXX.", "X...X", "X...X", "XXXXX", "X...X", "X...X", "X...X" },
            new[] { "XXXX.", "X...X", "X...X", "XXXX.", "X.X..", "X..X.", "X...X" },
            new[] { "XXXXX", "..X..", "..X..", "..X..", "..X..", "..X..", "..X.." }
        };
        for (int i = 0; i < glyphs.Length; i++)
        {
            // Letters at 2x so they stay readable
            for (int gy = 0; gy < 7; gy++)
                for (int gx = 0; gx < 5; gx++)
                    if (glyphs[i][gy][gx] == 'X') c.Rect(12 + i * 15 + gx * 2, 9 + gy * 2, 2, 2, Color.White);
        }
        c.OutlinePass(innerSeams: false);
        return c;
    }, repeat: false);

    public static Texture2D SignBoard => Get("sign_board", () =>
    {
        var c = new PixelCanvas(32, 20);
        var wood = Rgb(204, 150, 94);
        c.Fill(PixelCanvas.Shadow(wood, 0.45f));
        c.Rect(2, 2, 28, 16, wood);
        c.HLine(2, 2, 28, PixelCanvas.Light1(wood, 0.35f));
        c.HLine(2, 17, 28, PixelCanvas.Shadow(wood, 0.25f));
        for (int y = 6; y <= 13; y += 4) c.HLine(6, y, y == 13 ? 12 : 20, PixelCanvas.Shadow(wood, 0.5f));
        return c;
    }, repeat: false);

    public static Texture2D Plaque => Get("plaque", () =>
    {
        var c = new PixelCanvas(24, 16);
        var brass = Rgb(214, 172, 96);
        c.Fill(Rgb(92, 66, 50));
        c.Rect(2, 2, 20, 12, brass);
        c.HLine(2, 2, 20, PixelCanvas.Light1(brass, 0.4f));
        c.HLine(5, 7, 14, PixelCanvas.Shadow(brass, 0.45f));
        c.HLine(5, 10, 9, PixelCanvas.Shadow(brass, 0.45f));
        return c;
    }, repeat: false);

    public static Texture2D PcScreen => Get("pc_screen", () =>
    {
        var c = new PixelCanvas(24, 20);
        c.Fill(Rgb(206, 210, 222));
        for (int y = 2; y < 16; y++) c.HLine(2, y, 20, PixelCanvas.Mix(Rgb(96, 166, 236), Rgb(52, 110, 206), y / 16f));
        c.HLine(4, 5, 8, Rgb(190, 230, 252));
        c.HLine(4, 8, 12, Rgb(150, 206, 248));
        c.HLine(4, 11, 10, Rgb(150, 206, 248));
        c.Rect(19, 17, 2, 2, Rgb(90, 220, 120));
        return c;
    }, repeat: false);

    /// <summary>Soft round shadow (alpha-blended, not cut out).</summary>
    public static Texture2D ShadowBlob => Get("shadow_blob", () =>
    {
        var c = new PixelCanvas(32, 32);
        for (int y = 0; y < 32; y++)
            for (int x = 0; x < 32; x++)
            {
                float u = (x + 0.5f - 16f) / 16f, v = (y + 0.5f - 16f) / 16f;
                float d = MathF.Sqrt(u * u + v * v);
                if (d < 1f) c.Set(x, y, new Color(0, 0, 0, (int)(90 * Math.Clamp((1f - d) * 2.2f, 0f, 1f))));
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

    /// <summary>Window box full of flowers for the houses.</summary>
    public static Texture2D FlowerBox => Get("flower_box", () =>
    {
        var c = new PixelCanvas(32, 12);
        c.Rect(0, 5, 32, 7, Rgb(150, 98, 60));
        c.HLine(0, 5, 32, Rgb(188, 132, 86));
        for (int i = 0; i < 9; i++)
        {
            int x = 2 + i * 3 + (int)(R(i, 0, 20) * 2);
            var petal = i % 3 == 0 ? Rgb(238, 84, 110) : i % 3 == 1 ? Rgb(252, 252, 252) : Rgb(250, 208, 70);
            c.Rect(x, 1 + (i % 2), 3, 3, petal);
            c.Set(x + 1, 2 + (i % 2), Rgb(250, 200, 70));
            c.Set(x + 1, 4 + (i % 2), Rgb(70, 150, 76));
        }
        return c;
    }, repeat: false);

    // ------------------------------------------------------------------ interior

    public static Texture2D Books => Get("books", () =>
    {
        var c = new PixelCanvas(32, 32);
        var wood = Rgb(150, 100, 62);
        c.Fill(wood);
        Color[] spines = { Rgb(200, 60, 60), Rgb(60, 110, 200), Rgb(70, 160, 90), Rgb(230, 190, 70), Rgb(150, 90, 170), Rgb(236, 236, 230) };
        for (int shelf = 0; shelf < 3; shelf++)
        {
            int y0 = 2 + shelf * 10;
            c.HLine(0, y0 + 9, 32, MeshBuilder.Scale(wood, 0.7f));
            int x = 1;
            int i = 0;
            while (x < 31)
            {
                int w = 2 + (int)(R(i, shelf, 21) * 3);
                int h = 6 + (int)(R(i, shelf, 22) * 3);
                var col = spines[(int)(R(i, shelf, 23) * spines.Length) % spines.Length];
                c.Rect(x, y0 + 9 - h, Math.Min(w, 31 - x), h, col);
                c.VLine(x, y0 + 9 - h, h, MeshBuilder.Scale(col, 1.2f));
                c.HLine(x, y0 + 9 - h + 2, Math.Min(w, 31 - x), MeshBuilder.Scale(col, 0.75f));
                x += w + (R(i, shelf, 24) < 0.2f ? 1 : 0);
                i++;
            }
        }
        return c;
    }, repeat: true);

    public static Texture2D Cabinet => Get("cabinet", () =>
    {
        var c = new PixelCanvas(32, 32);
        var body = Rgb(236, 230, 214);
        c.Fill(body);
        c.Rect(2, 4, 13, 26, MeshBuilder.Scale(body, 0.94f));
        c.Rect(17, 4, 13, 26, MeshBuilder.Scale(body, 0.94f));
        c.HLine(2, 4, 13, MeshBuilder.Scale(body, 1.05f));
        c.HLine(17, 4, 13, MeshBuilder.Scale(body, 1.05f));
        c.Rect(12, 14, 2, 5, Rgb(150, 150, 160));
        c.Rect(18, 14, 2, 5, Rgb(150, 150, 160));
        c.HLine(0, 31, 32, MeshBuilder.Scale(body, 0.6f));
        return c;
    }, repeat: true);

    public static Texture2D TvScreen => Get("tv_screen", () =>
    {
        var c = new PixelCanvas(32, 24);
        c.Fill(Rgb(40, 40, 50));
        for (int y = 2; y < 22; y++) c.HLine(2, y, 28, PixelCanvas.Mix(Rgb(120, 190, 250), Rgb(90, 180, 110), y / 22f));
        // A tiny Pokémon-ish silhouette on screen
        c.FlatEllipse(16, 13, 5, 4, Rgb(250, 214, 70));
        c.Rect(12, 7, 2, 4, Rgb(250, 214, 70));
        c.Rect(18, 7, 2, 4, Rgb(250, 214, 70));
        c.Set(14, 12, Rgb(40, 40, 50));
        c.Set(18, 12, Rgb(40, 40, 50));
        return c;
    }, repeat: false);

    public static Texture2D Console => Get("console", () =>
    {
        var c = new PixelCanvas(32, 32);
        c.Fill(Rgb(172, 178, 196));
        c.Rect(3, 3, 26, 12, Rgb(40, 52, 70));
        for (int i = 0; i < 5; i++) c.HLine(5, 5 + i * 2, 6 + (int)(R(i, 0, 25) * 14), Rgb(110, 230, 150));
        Color[] lights = { Rgb(236, 70, 70), Rgb(250, 208, 70), Rgb(90, 220, 120), Rgb(90, 160, 240) };
        for (int i = 0; i < 8; i++) c.Rect(4 + i * 3, 19, 2, 2, lights[i % lights.Length]);
        c.Rect(4, 24, 24, 5, Rgb(140, 146, 166));
        for (int i = 0; i < 6; i++) c.Rect(5 + i * 4, 25, 3, 3, Rgb(212, 216, 228));
        return c;
    }, repeat: true);

    public static Texture2D Goods => Get("goods", () =>
    {
        var c = new PixelCanvas(32, 32);
        c.Fill(Rgb(206, 214, 226));
        Color[] goods = { Rgb(232, 80, 80), Rgb(80, 150, 232), Rgb(250, 206, 72), Rgb(120, 200, 110), Rgb(236, 236, 240), Rgb(170, 100, 200) };
        for (int shelf = 0; shelf < 3; shelf++)
        {
            int y = 2 + shelf * 10;
            c.HLine(0, y + 8, 32, Rgb(120, 132, 156));
            c.HLine(0, y + 9, 32, Rgb(92, 102, 124));
            for (int i = 0; i < 6; i++)
            {
                var g = goods[(i + shelf * 2) % goods.Length];
                int h = 5 + (i + shelf) % 3;
                c.Rect(1 + i * 5, y + 8 - h, 4, h, g);
                c.HLine(1 + i * 5, y + 8 - h, 4, PixelCanvas.Light1(g, 0.4f));
                c.VLine(4 + i * 5, y + 8 - h, h, PixelCanvas.Shadow(g, 0.3f));
            }
        }
        return c;
    }, repeat: true);

    public static Texture2D Rug(bool pokeCenter) => Get(pokeCenter ? "rug_center" : "rug_house", () =>
    {
        const int s = 64;
        var c = new PixelCanvas(s, s);
        var main = pokeCenter ? Rgb(236, 120, 130) : Rgb(196, 76, 60);
        var border = pokeCenter ? Rgb(250, 236, 238) : Rgb(236, 196, 110);
        c.Fill(main);
        c.Rect(3, 3, s - 6, 3, border);
        c.Rect(3, s - 6, s - 6, 3, border);
        c.Rect(3, 3, 3, s - 6, border);
        c.Rect(s - 6, 3, 3, s - 6, border);
        if (pokeCenter)
        {
            c.Disc(32, 32, 14, Rgb(250, 250, 252));
            for (int y = 18; y < 32; y++)
                for (int x = 18; x < 47; x++)
                    if ((x - 31.5f) * (x - 31.5f) + (y - 31.5f) * (y - 31.5f) < 196f) c.Set(x, y, Rgb(226, 56, 60));
            c.Rect(18, 30, 29, 4, Rgb(60, 50, 60));
            c.Disc(32, 32, 5, Rgb(60, 50, 60));
            c.Disc(32, 32, 3, Rgb(250, 250, 252));
        }
        else
        {
            // Diamond medallion and a dotted inner border
            c.FlatPoly(border, 32, 12, 44, 32, 32, 52, 20, 32);
            c.FlatPoly(main, 32, 18, 38, 32, 32, 46, 26, 32);
            for (int x = 10; x < s - 10; x += 6) { c.Set(x, 9, border); c.Set(x, s - 10, border); }
        }
        return c;
    }, repeat: false);

    public static Texture2D Painting => Get("painting", () =>
    {
        var c = new PixelCanvas(32, 24);
        c.Fill(Rgb(150, 110, 60));
        for (int y = 3; y < 21; y++) c.HLine(3, y, 26, PixelCanvas.Mix(Rgb(150, 210, 250), Rgb(220, 240, 252), y / 21f));
        c.FlatPoly(Rgb(110, 170, 120), 3, 18, 12, 9, 20, 16, 29, 11, 29, 21, 3, 21);
        c.FlatPoly(Rgb(80, 140, 96), 3, 21, 3, 17, 10, 14, 18, 21);
        c.Disc(23, 7, 2.5f, Rgb(250, 230, 120));
        c.HLine(2, 2, 28, Rgb(190, 150, 90));
        return c;
    }, repeat: false);

    public static Texture2D ClockFace => Get("clock", () =>
    {
        var c = new PixelCanvas(32, 32);
        c.Disc(16, 16, 15, Rgb(120, 80, 52));
        c.Disc(16, 16, 12.5f, Rgb(250, 248, 240));
        for (int i = 0; i < 12; i++)
        {
            float a = i * MathF.Tau / 12f;
            c.Set((int)(16 + MathF.Cos(a) * 10), (int)(16 + MathF.Sin(a) * 10), Rgb(60, 50, 50));
        }
        c.Line(16, 16, 16, 8, Rgb(40, 36, 40));
        c.Line(16, 16, 21, 18, Rgb(40, 36, 40));
        c.OutlinePass(innerSeams: false);
        return c;
    }, repeat: false);
}
