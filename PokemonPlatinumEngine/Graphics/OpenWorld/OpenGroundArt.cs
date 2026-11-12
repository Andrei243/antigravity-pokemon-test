using System;
using Raylib_cs;
using PokemonPlatinumEngine.OpenWorld;

namespace PokemonPlatinumEngine.Graphics.OpenWorld;

/// <summary>
/// What the open world's ground and water are shaded from (style guide, "Open world", "Ground"): one sheet of noise,
/// <see cref="Size"/> texels square and coming round its edges, four kinds in its four channels, read by the programs at
/// several sizes in the world (so nothing repeats to the eye), and the covers' colours as lists the ground's program
/// keeps (<see cref="Palette"/>: the table's base, patch, dark and light of each cover, in the atlas's order). The ground
/// is no longer laid from the covers' pixel tiles (<see cref="OpenCoverArt"/>, which stay the table's own record of each
/// cover's colours and marks): under a camera a few metres off the ground a tile of 32 texels was a wallpaper of blocks.
/// GPU-free but <see cref="Upload"/>.
/// </summary>
internal static class OpenGroundArt
{
    /// <summary>The sheet's side in texels.</summary>
    public const int Size = 256;

    private static uint Hash(int x, int y, int salt)
    {
        unchecked
        {
            uint h = (uint)x * 0x8DA6B343u ^ (uint)y * 0xD8163841u ^ (uint)salt * 0xCB1AB31Fu;
            h ^= h >> 13;
            h *= 0x5BD1E995u;
            h ^= h >> 15;
            return h;
        }
    }

    private static double Rand(int x, int y, int salt) => (Hash(x, y, salt) & 0xFFFFFF) / (double)0x1000000;

    /// <summary>Value noise of <paramref name="cells"/> cells across the sheet, coming round its edges.</summary>
    private static double Value(double u, double v, int cells, int salt)
    {
        double fx = u * cells, fy = v * cells;
        int x0 = (int)Math.Floor(fx), y0 = (int)Math.Floor(fy);
        double tx = fx - x0, ty = fy - y0;
        tx = tx * tx * tx * (tx * (tx * 6 - 15) + 10);
        ty = ty * ty * ty * (ty * (ty * 6 - 15) + 10);
        double V(int cx, int cy) => Rand(((cx % cells) + cells) % cells, ((cy % cells) + cells) % cells, salt);
        double a = V(x0, y0), b = V(x0 + 1, y0), c = V(x0, y0 + 1), d = V(x0 + 1, y0 + 1);
        return a + (b - a) * tx + (c - a) * ty + (a - b - c + d) * tx * ty;
    }

    /// <summary>Octaves of value noise from <paramref name="cells"/> cells, each twice as fine and <paramref name="gain"/> as strong.</summary>
    private static double Fbm(double u, double v, int cells, int octaves, double gain, int salt)
    {
        double sum = 0, amp = 1, total = 0;
        for (int o = 0; o < octaves; o++)
        {
            sum += Value(u, v, cells << o, salt + o * 31) * amp;
            total += amp;
            amp *= gain;
        }
        return sum / total;
    }

    /// <summary>How far a point is from the nearest of the points strewn one to a cell (nought at one, about one at a cell's far corner), coming round the sheet.</summary>
    private static double Cells(double u, double v, int cells, int salt)
    {
        double fx = u * cells, fy = v * cells;
        int cx = (int)Math.Floor(fx), cy = (int)Math.Floor(fy);
        double best = 9;
        for (int j = -1; j <= 1; j++)
            for (int i = -1; i <= 1; i++)
            {
                int x = cx + i, y = cy + j;
                int wx = ((x % cells) + cells) % cells, wy = ((y % cells) + cells) % cells;
                double px = x + 0.15 + 0.7 * Rand(wx, wy, salt), py = y + 0.15 + 0.7 * Rand(wx, wy, salt + 7);
                double d = (px - fx) * (px - fx) + (py - fy) * (py - fy);
                if (d < best) best = d;
            }
        return Math.Min(1, Math.Sqrt(best));
    }

    /// <summary>
    /// The sheet: red, soft cloud (five octaves from four cells); green, another cloud (from eight cells); blue, the
    /// distance to strewn points (pebbles, clumps, cracks between them); alpha, ridges (a cloud folded about its middle).
    /// Each channel stretched to the whole of its range.
    /// </summary>
    public static PixelCanvas PaintNoise()
    {
        var raw = new double[4][];
        for (int k = 0; k < 4; k++) raw[k] = new double[Size * Size];
        for (int y = 0; y < Size; y++)
            for (int x = 0; x < Size; x++)
            {
                double u = (x + 0.5) / Size, v = (y + 0.5) / Size;
                int i = y * Size + x;
                raw[0][i] = Fbm(u, v, 4, 5, 0.55, 11);
                raw[1][i] = Fbm(u, v, 8, 4, 0.5, 211);
                raw[2][i] = Cells(u, v, 16, 401) * 0.8 + Cells(u, v, 48, 433) * 0.2;
                raw[3][i] = 1 - Math.Abs(Fbm(u, v, 6, 4, 0.5, 601) * 2 - 1);
            }
        var canvas = new PixelCanvas(Size, Size);
        var lo = new double[4];
        var hi = new double[4];
        for (int k = 0; k < 4; k++)
        {
            lo[k] = double.MaxValue;
            hi[k] = double.MinValue;
            foreach (double value in raw[k]) { lo[k] = Math.Min(lo[k], value); hi[k] = Math.Max(hi[k], value); }
        }
        byte Of(int k, int i) => (byte)Math.Clamp((int)Math.Round((raw[k][i] - lo[k]) / (hi[k] - lo[k]) * 255), 0, 255);
        for (int y = 0; y < Size; y++)
            for (int x = 0; x < Size; x++)
            {
                int i = y * Size + x;
                canvas.SetRaw(x, y, new Color(Of(0, i), Of(1, i), Of(2, i), Of(3, i)));
            }
        return canvas;
    }

    /// <summary>
    /// The covers' colours in the atlas's order, three numbers a colour and <see cref="OpenCoverArt.Slots"/> colours a
    /// list: each cover's base, its patches', and its marks' dark and light (the style guide's table).
    /// </summary>
    public static (float[] Base, float[] Patch, float[] Dark, float[] Light) Palette()
    {
        int n = OpenCoverArt.Slots;
        var lists = (Base: new float[n * 3], Patch: new float[n * 3], Dark: new float[n * 3], Light: new float[n * 3]);
        for (int i = 0; i < n; i++)
        {
            var (b, p, d, l) = OpenCoverArt.ColoursOf(i < OpenCoverArt.Order.Length ? OpenCoverArt.Order[i] : OpenCover.Grass);
            Put(lists.Base, i, b);
            Put(lists.Patch, i, p);
            Put(lists.Dark, i, d);
            Put(lists.Light, i, l);
        }
        return lists;
    }

    private static void Put(float[] list, int i, Color c)
    {
        list[i * 3] = c.R / 255f;
        list[i * 3 + 1] = c.G / 255f;
        list[i * 3 + 2] = c.B / 255f;
    }

    /// <summary>The sheet on the card: smooth at any size (blended between texels and between mip levels), coming round its edges.</summary>
    public static Texture2D Upload()
    {
        var texture = PaintNoise().ToTexture(1, "open");
        Raylib.GenTextureMipmaps(ref texture);
        Raylib.SetTextureWrap(texture, TextureWrap.Repeat);
        Raylib.SetTextureFilter(texture, TextureFilter.Trilinear);
        return texture;
    }
}
