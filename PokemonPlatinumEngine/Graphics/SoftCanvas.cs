using System;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// A floating-point RGBA canvas for smooth, filtered textures (the 3D battles), plus the value noise shared by
/// the texture bakers. The opposite of <see cref="PixelCanvas"/>: results are mipmapped and trilinear-filtered.
/// </summary>
internal sealed class SoftCanvas
{
    public int Width { get; }
    public int Height { get; }
    private readonly Vector4[] px;

    public SoftCanvas(int width, int height)
    {
        Width = width;
        Height = height;
        px = new Vector4[width * height];
    }

    public static Vector4 C(int r, int g, int b, int a = 255) => new(r / 255f, g / 255f, b / 255f, a / 255f);

    public void SetPixel(int x, int y, Vector4 c) => px[y * Width + x] = c;

    /// <summary>Uploads with mipmaps and trilinear filtering.</summary>
    public Texture2D ToTexture(bool repeat)
    {
        var canvas = new PixelCanvas(Width, Height);
        for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
            {
                var c = px[y * Width + x];
                canvas.SetRaw(x, y, new Color((int)(Math.Clamp(c.X, 0f, 1f) * 255f + 0.5f), (int)(Math.Clamp(c.Y, 0f, 1f) * 255f + 0.5f),
                    (int)(Math.Clamp(c.Z, 0f, 1f) * 255f + 0.5f), (int)(Math.Clamp(c.W, 0f, 1f) * 255f + 0.5f)));
            }
        var tex = canvas.ToTexture();
        Raylib.GenTextureMipmaps(ref tex);
        Raylib.SetTextureFilter(tex, TextureFilter.Trilinear);
        Raylib.SetTextureWrap(tex, repeat ? TextureWrap.Repeat : TextureWrap.Clamp);
        return tex;
    }

    // ------------------------------------------------------------------ noise

    private static uint Hash(int x, int y, int salt)
    {
        unchecked
        {
            uint h = (uint)(x * 374761393 + y * 668265263 + salt * 1442695041);
            h = (h ^ (h >> 13)) * 1274126177u;
            return h ^ (h >> 16);
        }
    }

    public static float Rand(int x, int y, int salt) => (Hash(x, y, salt) & 0xFFFF) / 65536f;

    /// <summary>Smooth value noise in [0, 1], optionally periodic (for tiling textures).</summary>
    public static float Noise(float x, float y, int salt, int period = 0)
    {
        int x0 = (int)MathF.Floor(x), y0 = (int)MathF.Floor(y);
        float fx = x - x0, fy = y - y0;
        fx = fx * fx * (3 - 2 * fx);
        fy = fy * fy * (3 - 2 * fy);
        int x1 = x0 + 1, y1 = y0 + 1;
        if (period > 0)
        {
            x0 = ((x0 % period) + period) % period; x1 = ((x1 % period) + period) % period;
            y0 = ((y0 % period) + period) % period; y1 = ((y1 % period) + period) % period;
        }
        float a = Rand(x0, y0, salt), b = Rand(x1, y0, salt), c = Rand(x0, y1, salt), d = Rand(x1, y1, salt);
        return a + (b - a) * fx + (c - a) * fy + (a - b - c + d) * fx * fy;
    }

    /// <summary>Two octaves of value noise.</summary>
    public static float Fbm(float x, float y, int salt, int period = 0) =>
        Noise(x, y, salt, period) * 0.65f + Noise(x * 2f, y * 2f, salt + 1, period * 2) * 0.35f;
}
