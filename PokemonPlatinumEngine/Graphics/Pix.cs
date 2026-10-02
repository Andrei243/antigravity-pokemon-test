using System;
using System.Collections.Generic;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>The flat shades of one material: its base colour, the light and dark lines of its bevels, and a deeper shade for grooves.</summary>
internal readonly record struct Tone(Color Base, Color Light, Color Dark, Color Deep)
{
    /// <summary>Shades derived from a base colour: lights lean warm, darks lean violet (style guide, "shade with colour").</summary>
    public static Tone Of(Color c) =>
        new(c, PixelCanvas.Light1(c, 0.3f), PixelCanvas.Shadow(c, 0.24f), PixelCanvas.Shadow(c, 0.46f));

    public static Tone Of(int r, int g, int b) => Of(new Color(r, g, b, 255));

    public static Tone Of(int r, int g, int b, int lr, int lg, int lb, int dr, int dg, int db)
    {
        var dark = new Color(dr, dg, db, 255);
        return new Tone(new Color(r, g, b, 255), new Color(lr, lg, lb, 255), dark, PixelCanvas.Shadow(dark, 0.3f));
    }
}

/// <summary>
/// Drawing helpers for the field's pixel art (buildings, props, furniture): bevelled blocks, one-texel frames,
/// a small pixel alphabet, sprite outlines and the marks that make glass light up after dark. Everything is
/// placed shapes in flat shades; nothing here draws per-texel noise.
/// </summary>
internal static class Pix
{
    public static Color Rgb(int r, int g, int b) => new(r, g, b, 255);

    /// <summary>A raised block: base colour with a light line along the top and left and a dark line along the bottom and right.</summary>
    public static void Raised(PixelCanvas c, int x, int y, int w, int h, Tone t)
    {
        if (w <= 0 || h <= 0) return;
        c.Rect(x, y, w, h, t.Base);
        c.HLine(x, y, w, t.Light);
        c.VLine(x, y, h, t.Light);
        c.HLine(x, y + h - 1, w, t.Dark);
        c.VLine(x + w - 1, y, h, t.Dark);
    }

    /// <summary>A sunken panel: dark along the top and left, light along the bottom and right.</summary>
    public static void Sunken(PixelCanvas c, int x, int y, int w, int h, Tone t)
    {
        if (w <= 0 || h <= 0) return;
        c.Rect(x, y, w, h, t.Base);
        c.HLine(x, y + h - 1, w, t.Light);
        c.VLine(x + w - 1, y, h, t.Light);
        c.HLine(x, y, w, t.Dark);
        c.VLine(x, y, h, t.Dark);
    }

    /// <summary>A one-texel frame around a rectangle.</summary>
    public static void Border(PixelCanvas c, int x, int y, int w, int h, Color col)
    {
        if (w <= 0 || h <= 0) return;
        c.HLine(x, y, w, col);
        c.HLine(x, y + h - 1, w, col);
        c.VLine(x, y, h, col);
        c.VLine(x + w - 1, y, h, col);
    }

    /// <summary>A filled circle whose edge falls on whole texels: <paramref name="size"/> texels across from (x, y).</summary>
    public static void Disc(PixelCanvas c, int x, int y, int size, Color col)
    {
        float r = size / 2f;
        for (int py = 0; py < size; py++)
            for (int px = 0; px < size; px++)
            {
                float dx = px + 0.5f - r, dy = py + 0.5f - r;
                if (dx * dx + dy * dy <= r * r) c.Set(x + px, y + py, col);
            }
    }

    /// <summary>Darkens what is already painted in a rectangle (a cast shadow), keeping each texel's alpha.</summary>
    public static void Shade(PixelCanvas c, int x, int y, int w, int h, float amount)
    {
        for (int py = y; py < y + h; py++)
            for (int px = x; px < x + w; px++)
            {
                if (!c.IsOpaque(px, py)) continue;
                var col = c.Get(px, py);
                var dark = PixelCanvas.Shadow(new Color(col.R, col.G, col.B, (byte)255), amount);
                c.SetRaw(px, py, new Color(dark.R, dark.G, dark.B, col.A));
            }
    }

    /// <summary>
    /// Marks the texels in a rectangle as lit from inside after dark (see <see cref="ArtSheet.PublicLight"/> and
    /// <see cref="ArtSheet.HomeLight"/>). Paint frames and mullions afterwards: they overwrite the mark.
    /// </summary>
    public static void Lit(PixelCanvas c, int x, int y, int w, int h, byte level)
    {
        for (int py = y; py < y + h; py++)
            for (int px = x; px < x + w; px++)
            {
                if (!c.IsOpaque(px, py)) continue;
                var col = c.Get(px, py);
                c.SetRaw(px, py, new Color(col.R, col.G, col.B, level));
            }
    }

    /// <summary>
    /// A one-texel outline round a cut-out sprite, in a darker shade of whatever it touches (never near-black),
    /// like the characters' sprites.
    /// </summary>
    public static void Outline(PixelCanvas c, float amount = 0.55f)
    {
        var ink = new Color(40, 30, 56, 255);
        var marks = new List<(int X, int Y, Color Col)>();
        for (int y = 0; y < c.Height; y++)
            for (int x = 0; x < c.Width; x++)
            {
                if (c.IsOpaque(x, y)) continue;
                Color? n = c.IsOpaque(x, y + 1) ? c.Get(x, y + 1) : c.IsOpaque(x, y - 1) ? c.Get(x, y - 1)
                    : c.IsOpaque(x - 1, y) ? c.Get(x - 1, y) : c.IsOpaque(x + 1, y) ? c.Get(x + 1, y) : null;
                if (!n.HasValue) continue;
                var mixed = PixelCanvas.Mix(new Color(n.Value.R, n.Value.G, n.Value.B, (byte)255), ink, amount);
                marks.Add((x, y, mixed));
            }
        foreach (var (x, y, col) in marks) c.SetRaw(x, y, col);
    }

    // ------------------------------------------------------------------ lettering

    // Our own 5x7 alphabet for signs: seven rows of five texels per letter, read left to right, top to bottom
    private static readonly Dictionary<char, string> Glyphs = new()
    {
        ['A'] = ".XXX.X...XX...XXXXXXX...XX...XX...X",
        ['B'] = "XXXX.X...XX...XXXXX.X...XX...XXXXX.",
        ['C'] = ".XXX.X...XX....X....X....X...X.XXX.",
        ['D'] = "XXXX.X...XX...XX...XX...XX...XXXXX.",
        ['E'] = "XXXXXX....X....XXXX.X....X....XXXXX",
        ['F'] = "XXXXXX....X....XXXX.X....X....X....",
        ['G'] = ".XXX.X...XX....X.XXXX...XX...X.XXX.",
        ['H'] = "X...XX...XX...XXXXXXX...XX...XX...X",
        ['I'] = "XXXXX..X....X....X....X....X..XXXXX",
        ['J'] = "..XXX...X....X....X....X.X..X..XX..",
        ['K'] = "X...XX..X.X.X..XX...X.X..X..X.X...X",
        ['L'] = "X....X....X....X....X....X....XXXXX",
        ['M'] = "X...XXX.XXX.X.XX.X.XX...XX...XX...X",
        ['N'] = "X...XXX..XX.X.XX..XXX...XX...XX...X",
        ['O'] = ".XXX.X...XX...XX...XX...XX...X.XXX.",
        ['P'] = "XXXX.X...XX...XXXXX.X....X....X....",
        ['Q'] = ".XXX.X...XX...XX...XX.X.XX..X..XX.X",
        ['R'] = "XXXX.X...XX...XXXXX.X.X..X..X.X...X",
        ['S'] = ".XXXXX....X.....XXX.....X....XXXXX.",
        ['T'] = "XXXXX..X....X....X....X....X....X..",
        ['U'] = "X...XX...XX...XX...XX...XX...X.XXX.",
        ['V'] = "X...XX...XX...XX...XX...X.X.X...X..",
        ['W'] = "X...XX...XX...XX.X.XX.X.XXX.XXX...X",
        ['X'] = "X...XX...X.X.X...X...X.X.X...XX...X",
        ['Y'] = "X...XX...X.X.X...X....X....X....X..",
        ['Z'] = "XXXXX....X...X...X...X...X....XXXXX"
    };

    public static int TextWidth(string text, int scale = 1) => text.Length == 0 ? 0 : (text.Length * 6 - 1) * scale;

    public const int TextHeight = 7;

    /// <summary>Capital letters in the sign alphabet; spaces and unknown characters leave a gap.</summary>
    public static void Text(PixelCanvas c, int x, int y, string text, Color col, int scale = 1)
    {
        for (int i = 0; i < text.Length; i++)
        {
            if (!Glyphs.TryGetValue(char.ToUpperInvariant(text[i]), out var rows)) continue;
            for (int gy = 0; gy < 7; gy++)
                for (int gx = 0; gx < 5; gx++)
                    if (rows[gy * 5 + gx] == 'X') c.Rect(x + (i * 6 + gx) * scale, y + gy * scale, scale, scale, col);
        }
    }
}
