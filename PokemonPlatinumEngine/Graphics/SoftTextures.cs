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
}
