using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// Smooth textures for the 3D battles: soft, low-frequency colour fields at 64 texels per unit, mipmapped and
/// filtered, with no per-texel noise (the field's textures are pixel art instead).
/// </summary>
internal static class SoftTextures
{
    private static readonly Dictionary<string, Texture2D> Cache = new();

    private static Texture2D Get(string key, Func<SoftCanvas> build, bool repeat)
    {
        if (Cache.TryGetValue(key, out var tex)) return tex;
        tex = build().ToTexture(repeat);
        Cache[key] = tex;
        return tex;
    }

    private static Vector4 C(int r, int g, int b, int a = 255) => SoftCanvas.C(r, g, b, a);
    private static Vector4 Scale(Vector4 c, float f) => new(c.X * f, c.Y * f, c.Z * f, c.W);

    // ------------------------------------------------------------------ battle ground

    /// <summary>Battle meadow (four world units): broad soft patches of green, no specks.</summary>
    public static Texture2D Meadow => Get("meadow", () =>
    {
        const int s = 256;
        var c = new SoftCanvas(s, s);
        var baseCol = C(114, 190, 100);
        var light = C(138, 202, 104);
        var deep = C(90, 168, 94);
        for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float n = SoftCanvas.Fbm(x / 64f, y / 64f, 30, 4);
                var col = n > 0.5f ? Vector4.Lerp(baseCol, light, (n - 0.5f) * 1.6f) : Vector4.Lerp(baseCol, deep, (0.5f - n) * 1.8f);
                c.SetPixel(x, y, col);
            }
        return c;
    }, repeat: true);

    /// <summary>Top of a battle platform: bright, even grass fading to a darker rim, with one soft worn ring.</summary>
    public static Texture2D PlatformTop => Get("platform_top", () =>
    {
        const int s = 256;
        var c = new SoftCanvas(s, s);
        var inner = C(150, 220, 124);
        var outer = C(100, 182, 96);
        for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float u = (x + 0.5f) / s * 2f - 1f, v = (y + 0.5f) / s * 2f - 1f;
                float d = MathF.Sqrt(u * u + v * v);
                var col = Vector4.Lerp(inner, outer, Math.Clamp((d - 0.2f) / 0.7f, 0f, 1f));
                float ring = MathF.Exp(-MathF.Pow((d - 0.8f) / 0.035f, 2f));
                col = Scale(col, 1f - ring * 0.1f);
                col = Vector4.Lerp(col, C(84, 160, 84), Math.Clamp((d - 0.9f) / 0.1f, 0f, 1f));
                c.SetPixel(x, y, col);
            }
        return c;
    }, repeat: false);

    // ------------------------------------------------------------------ arenas (plan 04 · G8)

    private static Vector4 V(Color c) => new(c.R / 255f, c.G / 255f, c.B / 255f, c.A / 255f);

    /// <summary>
    /// Ground for an arena (four world units, repeating): soft light and deep patches over a base colour, with
    /// <paramref name="other"/> blotches (leaf litter, gravel) and, for sand, wind ripples.
    /// </summary>
    public static Texture2D Ground(string key, Color baseColor, Color light, Color deep, Color other, float otherAmount = 0f, float ripples = 0f) =>
        Get("ground_" + key, () =>
        {
            const int s = 256;
            var c = new SoftCanvas(s, s);
            Vector4 b = V(baseColor), l = V(light), d = V(deep), o = V(other);
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float n = SoftCanvas.Fbm(x / 64f, y / 64f, 30 + key.Length, 4);
                    var col = n > 0.5f ? Vector4.Lerp(b, l, (n - 0.5f) * 1.6f) : Vector4.Lerp(b, d, (0.5f - n) * 1.8f);
                    if (otherAmount > 0f)
                    {
                        float m = SoftCanvas.Fbm(x / 32f, y / 32f, 70 + key.Length, 8);
                        col = Vector4.Lerp(col, o, Math.Clamp((m - (1f - otherAmount)) * 4f, 0f, 1f) * 0.8f);
                    }
                    if (ripples > 0f)
                    {
                        // Wind ripples: soft bands across, bent by the noise
                        float r = MathF.Sin((y / 256f * 18f + n * 3f) * MathF.Tau);
                        col = Scale(col, 1f + r * 0.035f * ripples);
                    }
                    c.SetPixel(x, y, col);
                }
            return c;
        }, repeat: true);

    /// <summary>A platform's top: <paramref name="inner"/> in the middle fading to <paramref name="outer"/>, a worn ring, then the edge colour.</summary>
    public static Texture2D Top(string key, Color inner, Color outer, Color edge, int rings = 1) => Get("top_" + key, () =>
    {
        const int s = 256;
        var c = new SoftCanvas(s, s);
        Vector4 i0 = V(inner), o0 = V(outer), e0 = V(edge);
        for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float u = (x + 0.5f) / s * 2f - 1f, v = (y + 0.5f) / s * 2f - 1f;
                float d = MathF.Sqrt(u * u + v * v);
                var col = Vector4.Lerp(i0, o0, Math.Clamp((d - 0.2f) / 0.7f, 0f, 1f));
                for (int k = 0; k < rings; k++)
                {
                    float at = 0.8f - k * 0.22f;
                    float ring = MathF.Exp(-MathF.Pow((d - at) / 0.035f, 2f));
                    col = rings > 1 ? Vector4.Lerp(col, e0, ring * 0.7f) : Scale(col, 1f - ring * 0.1f);
                }
                col = Vector4.Lerp(col, e0, Math.Clamp((d - 0.9f) / 0.1f, 0f, 1f));
                c.SetPixel(x, y, col);
            }
        return c;
    }, repeat: false);

    /// <summary>Polished hall tiles (repeating, four units): two tones in a checker with thin grout and a soft sheen.</summary>
    public static Texture2D Tiles(string key, Color a, Color b, Color grout) => Get("tiles_" + key, () =>
    {
        const int s = 256, n = 4;
        var c = new SoftCanvas(s, s);
        Vector4 a0 = V(a), b0 = V(b), g0 = V(grout);
        for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                int tx = x * n / s, ty = y * n / s;
                var col = (tx + ty) % 2 == 0 ? a0 : b0;
                float fx = x * n / (float)s - tx, fy = y * n / (float)s - ty;
                // A gentle sheen across each tile, then the grout lines
                col = Scale(col, 1f + 0.05f * (1f - fx - fy));
                float edge = Math.Min(Math.Min(fx, 1f - fx), Math.Min(fy, 1f - fy));
                col = Vector4.Lerp(g0, col, Math.Clamp((edge - 0.012f) / 0.012f, 0f, 1f));
                c.SetPixel(x, y, col);
            }
        return c;
    }, repeat: true);

    /// <summary>Floorboards (repeating, four units): long planks in staggered rows with soft grain.</summary>
    public static Texture2D Planks(string key, Color wood, Color dark) => Get("planks_" + key, () =>
    {
        const int s = 256, rows = 8;
        var c = new SoftCanvas(s, s);
        Vector4 w0 = V(wood), d0 = V(dark);
        for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                int row = y * rows / s;
                float fy = y * rows / (float)s - row;
                float shift = (row % 2) * 0.5f + SoftCanvas.Rand(row, 0, 5) * 0.2f;
                float fx = ((x / (float)s + shift) * 2f) % 1f;
                float grain = SoftCanvas.Fbm(x / 48f, y / 6f, 90 + row, 0);
                var col = Vector4.Lerp(w0, d0, 0.15f + grain * 0.3f + SoftCanvas.Rand(row, (int)((x / (float)s + shift) * 2f), 6) * 0.25f);
                float seam = Math.Min(Math.Min(fy, 1f - fy) * rows * 0.5f, Math.Min(fx, 1f - fx) * 8f);
                col = Vector4.Lerp(Scale(d0, 0.8f), col, Math.Clamp(seam * 6f, 0f, 1f));
                c.SetPixel(x, y, col);
            }
        return c;
    }, repeat: true);

    /// <summary>
    /// A floor emblem, cut out to lie over the tiles: two rings in <paramref name="mark"/> round a four-pointed star
    /// in <paramref name="light"/> (alpha is 1 on the marks and 0 between them).
    /// </summary>
    public static Texture2D Emblem(string key, Color mark, Color light) => Get("emblem_" + key, () =>
    {
        const int s = 512;
        var c = new SoftCanvas(s, s);
        Vector4 m0 = V(mark), l0 = V(light);
        float px = 2f / s;
        for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float u = (x + 0.5f) / s * 2f - 1f, v = (y + 0.5f) / s * 2f - 1f;
                float d = MathF.Sqrt(u * u + v * v);
                float a = MathF.Atan2(v, u);
                float Ring(float r, float w) => Math.Clamp(1f - (MathF.Abs(d - r) - w) / px, 0f, 1f);
                float rings = Math.Max(Ring(0.93f, 0.025f), Math.Max(Ring(0.85f, 0.01f), Ring(0.42f, 0.012f)));
                float edge = 0.08f + 0.3f * MathF.Pow(MathF.Abs(MathF.Cos(a * 2f)), 12f);
                float star = Math.Clamp(1f - (d - edge) / px, 0f, 1f);
                // Transparent texels keep the mark's colour so filtering doesn't darken the edges
                var col = star > rings ? l0 : m0;
                col.W = Math.Max(rings, star);
                c.SetPixel(x, y, col);
            }
        return c;
    }, repeat: false);

    /// <summary>A soft ring of light (white, in alpha), added on top for glowing rims.</summary>
    public static Texture2D GlowRing => Get("glow_ring", () =>
    {
        const int s = 128;
        var c = new SoftCanvas(s, s);
        for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float u = (x + 0.5f) / s * 2f - 1f, v = (y + 0.5f) / s * 2f - 1f;
                float d = MathF.Sqrt(u * u + v * v);
                float a = MathF.Exp(-MathF.Pow((d - 0.82f) / 0.06f, 2f)) + 0.35f * MathF.Exp(-MathF.Pow((d - 0.82f) / 0.16f, 2f));
                c.SetPixel(x, y, new Vector4(1f, 1f, 1f, Math.Clamp(a, 0f, 1f)));
            }
        return c;
    }, repeat: false);

    /// <summary>Battle water (repeats): a calm blue with soft lighter swells; green carries the foam's breakup.</summary>
    public static Texture2D Water => Get("soft_water", () =>
    {
        const int s = 256;
        var c = new SoftCanvas(s, s);
        var deep = C(58, 118, 190);
        var light = C(116, 184, 232);
        for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float swell = SoftCanvas.Fbm(x / 64f, y / 32f, 61, 4);
                float n = SoftCanvas.Fbm(x / 16f, y / 16f, 62, 16);
                var col = Vector4.Lerp(deep, light, Math.Clamp((swell - 0.42f) * 2.2f, 0f, 1f) * 0.55f);
                // The shader reads green as noise for the foam's edge, so keep it close to the colour's own green
                col.Y = Math.Clamp(col.Y * (0.9f + n * 0.2f), 0f, 1f);
                c.SetPixel(x, y, col);
            }
        return c;
    }, repeat: true);
}
