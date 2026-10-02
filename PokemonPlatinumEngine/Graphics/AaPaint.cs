using System;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// Anti-aliased shapes for smooth textures (faces, eyes, markings): each pixel's coverage comes from its signed
/// distance to the shape over one pixel, composited over what is there. No GPU calls.
/// </summary>
internal static class AaPaint
{
    public static void Plot(PixelCanvas c, int x, int y, Color col, float coverage)
    {
        if (coverage <= 0f || !c.InBounds(x, y)) return;
        var dst = c.Get(x, y);
        float a = Math.Clamp(coverage, 0f, 1f);
        float da = dst.A / 255f;
        float outA = a + da * (1f - a);
        if (outA <= 0f) return;
        int r = (int)((col.R * a + dst.R * da * (1f - a)) / outA);
        int g = (int)((col.G * a + dst.G * da * (1f - a)) / outA);
        int b = (int)((col.B * a + dst.B * da * (1f - a)) / outA);
        c.SetRaw(x, y, new Color(r, g, b, (int)(outA * 255f)));
    }

    public static void Ellipse(PixelCanvas c, Vector2 at, float rx, float ry, Color col)
    {
        for (int y = (int)(at.Y - ry - 2); y <= (int)(at.Y + ry + 2); y++)
            EllipseRow(c, at, rx, ry, y, col);
    }

    public static void EllipseRow(PixelCanvas c, Vector2 at, float rx, float ry, int y, Color col)
    {
        if (rx <= 0f || ry <= 0f) return;
        for (int x = (int)(at.X - rx - 2); x <= (int)(at.X + rx + 2); x++)
        {
            var p = new Vector2(x + 0.5f - at.X, y + 0.5f - at.Y);
            // Distance to the ellipse, near enough for a one-pixel edge
            float k0 = new Vector2(p.X / rx, p.Y / ry).Length();
            float k1 = new Vector2(p.X / (rx * rx), p.Y / (ry * ry)).Length();
            float d = k1 > 1e-6f ? k0 * (k0 - 1f) / k1 : -Math.Min(rx, ry);
            Plot(c, x, y, col, 0.5f - d);
        }
    }

    /// <summary>The lower half of an ellipse (an open mouth).</summary>
    public static void HalfDisc(PixelCanvas c, Vector2 at, float rx, float ry, Color col)
    {
        for (int y = (int)at.Y; y <= (int)(at.Y + ry + 2); y++)
            EllipseRow(c, at, rx, ry, y, col);
    }

    public static void Line(PixelCanvas c, Vector2 a, Vector2 b, float width, Color col)
    {
        var min = Vector2.Min(a, b) - new Vector2(width + 2);
        var max = Vector2.Max(a, b) + new Vector2(width + 2);
        var ab = b - a;
        float len2 = Math.Max(1e-6f, ab.LengthSquared());
        for (int y = (int)min.Y; y <= (int)max.Y; y++)
            for (int x = (int)min.X; x <= (int)max.X; x++)
            {
                var p = new Vector2(x + 0.5f, y + 0.5f);
                float t = Math.Clamp(Vector2.Dot(p - a, ab) / len2, 0f, 1f);
                float d = Vector2.Distance(p, a + ab * t) - width * 0.5f;
                Plot(c, x, y, col, 0.5f - d);
            }
    }

    /// <summary>An arc of an ellipse from angle <paramref name="from"/> to <paramref name="to"/> (0 = right, π/2 = down).</summary>
    public static void Arc(PixelCanvas c, Vector2 at, float rx, float ry, float from, float to, float width, Color col)
    {
        const int Steps = 24;
        var prev = at + new Vector2(MathF.Cos(from) * rx, MathF.Sin(from) * ry);
        for (int i = 1; i <= Steps; i++)
        {
            float t = from + (to - from) * i / Steps;
            var next = at + new Vector2(MathF.Cos(t) * rx, MathF.Sin(t) * ry);
            Line(c, prev, next, width, col);
            prev = next;
        }
    }
    /// <summary>A convex polygon (corners in either winding order).</summary>
    public static void Polygon(PixelCanvas c, Vector2[] corners, Color col)
    {
        var min = corners[0];
        var max = corners[0];
        foreach (var p in corners)
        {
            min = Vector2.Min(min, p);
            max = Vector2.Max(max, p);
        }
        // Signed area tells which side of each edge is inside
        float area = 0f;
        for (int i = 0; i < corners.Length; i++)
        {
            var a = corners[i];
            var b = corners[(i + 1) % corners.Length];
            area += a.X * b.Y - b.X * a.Y;
        }
        float sign = area >= 0f ? 1f : -1f;
        for (int y = (int)min.Y - 1; y <= (int)max.Y + 1; y++)
            for (int x = (int)min.X - 1; x <= (int)max.X + 1; x++)
            {
                var p = new Vector2(x + 0.5f, y + 0.5f);
                float d = float.MinValue;
                for (int i = 0; i < corners.Length; i++)
                {
                    var a = corners[i];
                    var e = corners[(i + 1) % corners.Length] - a;
                    float len = e.Length();
                    if (len < 1e-6f) continue;
                    // Distance outside this edge (positive outside)
                    float outside = sign * (e.X * (a.Y - p.Y) - e.Y * (a.X - p.X)) / len;
                    d = Math.Max(d, outside);
                }
                Plot(c, x, y, col, 0.5f - d);
            }
    }
}
