using System;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// CPU-side RGBA canvas used to build all procedural pixel art. Shapes are drawn with
/// cel shading, parts are tracked so an outline pass can separate overlapping pieces,
/// and the result is uploaded as a point-filtered texture so pixels stay crisp when scaled.
/// </summary>
public sealed class PixelCanvas
{
    public int Width { get; }
    public int Height { get; }

    /// <summary>Uniform scale applied to every shape coordinate (lets one drawing render at several sizes).</summary>
    public float Scale { get; set; } = 1f;
    public float OffsetX { get; set; }
    public float OffsetY { get; set; }

    private readonly Color[] pixels;
    private readonly int[] partIds;
    private int currentPart;

    // Light comes from the upper left, slightly toward the viewer
    private static readonly (float X, float Y, float Z) Light = Normalize(-0.45f, -0.7f, 0.62f);

    public PixelCanvas(int width, int height)
    {
        Width = width;
        Height = height;
        pixels = new Color[width * height];
        partIds = new int[width * height];
    }

    // ---------------------------------------------------------------- color helpers

    public static Color Rgb(int r, int g, int b, int a = 255) => new(r, g, b, a);

    public static Color Mix(Color a, Color b, float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        return new Color(
            (int)(a.R + (b.R - a.R) * t),
            (int)(a.G + (b.G - a.G) * t),
            (int)(a.B + (b.B - a.B) * t),
            (int)(a.A + (b.A - a.A) * t));
    }

    /// <summary>Darker, slightly cooler tone (hue-shifted shadows read better than plain black mixes).</summary>
    public static Color Shadow(Color c, float amount = 0.3f) => Mix(c, new Color(38, 28, 82, (int)c.A), amount);

    /// <summary>Lighter, slightly warmer tone.</summary>
    public static Color Light1(Color c, float amount = 0.25f) => Mix(c, new Color(255, 250, 222, (int)c.A), amount);

    public static Color Outline(Color c) => Mix(c, new Color(22, 18, 38, 255), 0.72f);

    // ---------------------------------------------------------------- raw access

    public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;

    public Color Get(int x, int y) => InBounds(x, y) ? pixels[y * Width + x] : default;

    public bool IsOpaque(int x, int y) => InBounds(x, y) && pixels[y * Width + x].A > 0;

    public void Set(int x, int y, Color c)
    {
        if (!InBounds(x, y)) return;
        int i = y * Width + x;
        if (c.A == 255)
        {
            pixels[i] = c;
        }
        else if (c.A > 0)
        {
            var dst = pixels[i];
            if (dst.A == 0)
            {
                pixels[i] = c;
                return;
            }
            float a = c.A / 255f;
            pixels[i] = new Color(
                (int)(dst.R + (c.R - dst.R) * a),
                (int)(dst.G + (c.G - dst.G) * a),
                (int)(dst.B + (c.B - dst.B) * a),
                Math.Max((int)dst.A, (int)c.A));
            return;
        }
        else
        {
            return;
        }
        partIds[i] = currentPart;
    }

    /// <summary>Writes a pixel as-is, alpha included (no blending); for smooth, filtered textures.</summary>
    public void SetRaw(int x, int y, Color c)
    {
        if (InBounds(x, y)) pixels[y * Width + x] = c;
    }

    public void Fill(Color c)
    {
        Array.Fill(pixels, c);
    }

    /// <summary>Starts a new outlined part: the outline pass draws a seam where it overlaps earlier parts.</summary>
    public void Part() => currentPart++;

    // ---------------------------------------------------------------- transform

    private float TX(float x) => x * Scale + OffsetX;
    private float TY(float y) => y * Scale + OffsetY;

    // ---------------------------------------------------------------- flat primitives (canvas coordinates, unscaled)

    public void Rect(int x, int y, int w, int h, Color c)
    {
        for (int yy = y; yy < y + h; yy++)
            for (int xx = x; xx < x + w; xx++)
                Set(xx, yy, c);
    }

    public void HLine(int x, int y, int w, Color c) => Rect(x, y, w, 1, c);
    public void VLine(int x, int y, int h, Color c) => Rect(x, y, 1, h, c);

    public void Line(int x0, int y0, int x1, int y1, Color c)
    {
        int dx = Math.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
        int dy = -Math.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1;
        int err = dx + dy;
        while (true)
        {
            Set(x0, y0, c);
            if (x0 == x1 && y0 == y1) break;
            int e2 = 2 * err;
            if (e2 >= dy) { err += dy; x0 += sx; }
            if (e2 <= dx) { err += dx; y0 += sy; }
        }
    }

    public void Disc(float cx, float cy, float r, Color c) => FlatEllipse(cx, cy, r, r, c);

    /// <summary>Single pixel at a drawing coordinate (respects Scale/Offset).</summary>
    public void Dot(float x, float y, Color c) => Set((int)MathF.Floor(TX(x)), (int)MathF.Floor(TY(y)), c);

    // ---------------------------------------------------------------- scaled shapes (drawing coordinates)

    public void FlatEllipse(float cx, float cy, float rx, float ry, Color c)
    {
        FillShape(cx - rx, cy - ry, cx + rx, cy + ry, (u, v) => u * u + v * v <= 1f, c, shaded: false);
    }

    /// <summary>Cel-shaded ellipse, lit as a sphere.</summary>
    public void Ball(float cx, float cy, float rx, float ry, Color c, bool highlight = true)
    {
        FillShape(cx - rx, cy - ry, cx + rx, cy + ry, (u, v) => u * u + v * v <= 1f, c, shaded: true, highlight: highlight);
    }

    /// <summary>Cel-shaded polygon (convex or concave), lit as if it were a rounded blob.</summary>
    public void Poly(Color c, params float[] xy) => PolyInternal(c, true, xy);

    public void FlatPoly(Color c, params float[] xy) => PolyInternal(c, false, xy);

    public void Tri(float x0, float y0, float x1, float y1, float x2, float y2, Color c) => Poly(c, x0, y0, x1, y1, x2, y2);

    public void FlatTri(float x0, float y0, float x1, float y1, float x2, float y2, Color c) => FlatPoly(c, x0, y0, x1, y1, x2, y2);

    public void Box(float x, float y, float w, float h, Color c, bool shaded = true)
    {
        FillShape(x, y, x + w, y + h, (u, v) => true, c, shaded, highlight: false, flatness: 0.55f);
    }

    /// <summary>Thick stroke between two points (for limbs, tails, antennae).</summary>
    public void Limb(float x0, float y0, float x1, float y1, float r0, float r1, Color c, bool shaded = true)
    {
        float minX = Math.Min(x0 - r0, x1 - r1), maxX = Math.Max(x0 + r0, x1 + r1);
        float minY = Math.Min(y0 - r0, y1 - r1), maxY = Math.Max(y0 + r0, y1 + r1);
        float dx = x1 - x0, dy = y1 - y0;
        float len2 = Math.Max(0.0001f, dx * dx + dy * dy);
        float w = maxX - minX, h = maxY - minY;

        FillShape(minX, minY, maxX, maxY, (u, v) =>
        {
            float px = minX + (u + 1f) * 0.5f * w;
            float py = minY + (v + 1f) * 0.5f * h;
            float t = Math.Clamp(((px - x0) * dx + (py - y0) * dy) / len2, 0f, 1f);
            float qx = x0 + dx * t - px, qy = y0 + dy * t - py;
            float r = r0 + (r1 - r0) * t;
            return qx * qx + qy * qy <= r * r;
        }, c, shaded, highlight: false);
    }

    private void PolyInternal(Color c, bool shaded, float[] xy)
    {
        float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
        for (int i = 0; i < xy.Length; i += 2)
        {
            minX = Math.Min(minX, xy[i]); maxX = Math.Max(maxX, xy[i]);
            minY = Math.Min(minY, xy[i + 1]); maxY = Math.Max(maxY, xy[i + 1]);
        }
        float w = Math.Max(0.001f, maxX - minX), h = Math.Max(0.001f, maxY - minY);

        FillShape(minX, minY, maxX, maxY, (u, v) =>
        {
            float px = minX + (u + 1f) * 0.5f * w;
            float py = minY + (v + 1f) * 0.5f * h;
            bool inside = false;
            for (int i = 0, j = xy.Length - 2; i < xy.Length; j = i, i += 2)
            {
                float xi = xy[i], yi = xy[i + 1], xj = xy[j], yj = xy[j + 1];
                if ((yi > py) != (yj > py) && px < (xj - xi) * (py - yi) / (yj - yi) + xi)
                {
                    inside = !inside;
                }
            }
            return inside;
        }, c, shaded, highlight: false, flatness: 0.35f);
    }

    /// <summary>
    /// Rasterizes a shape given in drawing coordinates. <paramref name="inside"/> receives the pixel center
    /// mapped to [-1, 1] across the bounding box; the same mapping drives the fake sphere normal for shading.
    /// </summary>
    private void FillShape(float x0, float y0, float x1, float y1, Func<float, float, bool> inside, Color c,
        bool shaded, bool highlight = true, float flatness = 0f)
    {
        float sx0 = TX(x0), sy0 = TY(y0), sx1 = TX(x1), sy1 = TY(y1);
        float w = Math.Max(0.001f, sx1 - sx0), h = Math.Max(0.001f, sy1 - sy0);

        Color dark = Shadow(c, 0.34f);
        Color light = Light1(c, 0.28f);
        Color shine = Mix(c, new Color(255, 255, 255, (int)c.A), 0.62f);

        for (int py = (int)MathF.Floor(sy0); py <= (int)MathF.Ceiling(sy1); py++)
        {
            for (int px = (int)MathF.Floor(sx0); px <= (int)MathF.Ceiling(sx1); px++)
            {
                float u = ((px + 0.5f) - sx0) / w * 2f - 1f;
                float v = ((py + 0.5f) - sy0) / h * 2f - 1f;
                if (u < -1f || u > 1f || v < -1f || v > 1f || !inside(u, v)) continue;

                Color col = c;
                if (shaded)
                {
                    float nx = u * (1f - flatness), ny = v * (1f - flatness);
                    float nz = MathF.Sqrt(Math.Max(0f, 1f - nx * nx - ny * ny));
                    float lambert = nx * Light.X + ny * Light.Y + nz * Light.Z;
                    if (lambert < 0.12f) col = dark;
                    else if (lambert > 0.93f && highlight) col = shine;
                    else if (lambert > 0.72f) col = light;
                }
                Set(px, py, col);
            }
        }
    }

    // ---------------------------------------------------------------- post passes

    /// <summary>
    /// Adds a 1px outline around the silhouette (tinted from the neighbouring color) and
    /// draws seams where a later part overlaps an earlier one.
    /// </summary>
    public void OutlinePass(bool innerSeams = true)
    {
        var src = (Color[])pixels.Clone();
        var parts = (int[])partIds.Clone();

        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                int i = y * Width + x;
                if (src[i].A == 0)
                {
                    // Outer outline: any opaque 4-neighbour
                    Color? n = null;
                    if (x > 0 && src[i - 1].A > 0) n = src[i - 1];
                    else if (x < Width - 1 && src[i + 1].A > 0) n = src[i + 1];
                    else if (y > 0 && src[i - Width].A > 0) n = src[i - Width];
                    else if (y < Height - 1 && src[i + Width].A > 0) n = src[i + Width];
                    if (n.HasValue) pixels[i] = Outline(n.Value);
                }
                else if (innerSeams)
                {
                    // Seam: this pixel belongs to an earlier part that a later part sits on top of
                    int p = parts[i];
                    bool seam =
                        (x > 0 && src[i - 1].A > 0 && parts[i - 1] > p) ||
                        (x < Width - 1 && src[i + 1].A > 0 && parts[i + 1] > p) ||
                        (y > 0 && src[i - Width].A > 0 && parts[i - Width] > p) ||
                        (y < Height - 1 && src[i + Width].A > 0 && parts[i + Width] > p);
                    if (seam) pixels[i] = Mix(src[i], new Color(22, 18, 38, 255), 0.55f);
                }
            }
        }
    }

    /// <summary>Soft elliptical drop shadow under a sprite (drawn beneath existing pixels).</summary>
    public void GroundShadow(float cx, float cy, float rx, float ry, byte alpha = 70)
    {
        for (int y = (int)(cy - ry); y <= (int)(cy + ry); y++)
            for (int x = (int)(cx - rx); x <= (int)(cx + rx); x++)
            {
                float u = (x + 0.5f - cx) / rx, v = (y + 0.5f - cy) / ry;
                if (u * u + v * v <= 1f && !IsOpaque(x, y))
                {
                    pixels[y * Width + x] = new Color(0, 0, 0, (int)alpha);
                }
            }
    }

    public void MirrorHorizontal()
    {
        for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width / 2; x++)
            {
                int a = y * Width + x, b = y * Width + (Width - 1 - x);
                (pixels[a], pixels[b]) = (pixels[b], pixels[a]);
                (partIds[a], partIds[b]) = (partIds[b], partIds[a]);
            }
    }

    /// <summary>Copies another canvas onto this one at (dx, dy), skipping transparent pixels.</summary>
    public void Blit(PixelCanvas src, int dx, int dy)
    {
        for (int y = 0; y < src.Height; y++)
            for (int x = 0; x < src.Width; x++)
            {
                var c = src.pixels[y * src.Width + x];
                if (c.A > 0) Set(dx + x, dy + y, c);
            }
    }

    /// <summary>Draws an ASCII pixel map. Characters missing from the palette (e.g. '.') are transparent.</summary>
    public void Stamp(int ox, int oy, string[] rows, Func<char, Color?> palette, bool flipX = false)
    {
        for (int y = 0; y < rows.Length; y++)
        {
            string row = rows[y];
            for (int x = 0; x < row.Length; x++)
            {
                var c = palette(row[x]);
                if (c.HasValue)
                {
                    int px = flipX ? ox + row.Length - 1 - x : ox + x;
                    Set(px, oy + y, c.Value);
                }
            }
        }
    }

    /// <summary>Copies an image's pixels into a new canvas.</summary>
    public static unsafe PixelCanvas FromImage(Image image)
    {
        var canvas = new PixelCanvas(image.Width, image.Height);
        Color* colors = Raylib.LoadImageColors(image);
        for (int i = 0; i < canvas.pixels.Length; i++) canvas.pixels[i] = colors[i];
        Raylib.UnloadImageColors(colors);
        return canvas;
    }

    // ---------------------------------------------------------------- upload

    public unsafe Image ToImage(int upscale = 1)
    {
        int w = Width * upscale, h = Height * upscale;
        Image img = Raylib.GenImageColor(w, h, new Color(0, 0, 0, 0));
        byte* data = (byte*)img.Data;
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                var c = pixels[(y / upscale) * Width + (x / upscale)];
                int i = (y * w + x) * 4;
                data[i] = c.R;
                data[i + 1] = c.G;
                data[i + 2] = c.B;
                data[i + 3] = c.A;
            }
        }
        return img;
    }

    public Texture2D ToTexture(int upscale = 1)
    {
        Image img = ToImage(upscale);
        Texture2D tex = Raylib.LoadTextureFromImage(img);
        Raylib.UnloadImage(img);
        Raylib.SetTextureFilter(tex, TextureFilter.Point);
        return tex;
    }

    private static (float X, float Y, float Z) Normalize(float x, float y, float z)
    {
        float len = MathF.Sqrt(x * x + y * y + z * z);
        return (x / len, y / len, z / len);
    }
}
