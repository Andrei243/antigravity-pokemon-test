using System;
using System.Collections.Generic;
using Raylib_cs;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// The HD-2D look's ground: deliberate pixel art at the field's 32 texels per tile, shown point-filtered. Every
/// detail is a placed cluster (tuft marks, pebbles, flowers, rounded path edges with a one-texel rim); light
/// and shade come in two or three flat steps instead of per-texel noise.
/// </summary>
internal static class PixelGround
{
    private const int T = GroundBaker.ArtTile;

    private static readonly Color Grass = new(104, 190, 98, 255);
    private static readonly Color GrassLight = new(120, 200, 102, 255);
    private static readonly Color Tuft = new(70, 150, 82, 255);
    private static readonly Color TuftTip = new(160, 222, 122, 255);
    private static readonly Color Forest = new(58, 126, 82, 255);
    private static readonly Color TallGround = new(40, 112, 66, 255);
    private static readonly Color Path = new(222, 204, 160, 255);
    private static readonly Color PathLight = new(236, 222, 186, 255);
    private static readonly Color PathShade = new(214, 186, 132, 255);
    private static readonly Color PathRim = new(166, 140, 104, 255);
    private static readonly Color Stone = new(168, 160, 150, 255);
    private static readonly Color StoneLight = new(208, 202, 192, 255);
    private static readonly Color Sand = new(238, 224, 172, 255);

    public static PixelCanvas Bake(Map map, int margin, IReadOnlyList<BuildingInfo> buildings)
    {
        int tw = map.Width + margin * 2, th = map.Height + margin * 2;
        int w = tw * T, h = th * T;
        TileType? TypeAt(int tx, int ty) => GroundBaker.TypeAt(map, tx - margin, ty - margin);
        bool IsBuilding(int tx, int ty) => MapStructures.IsBuildingTile(map, tx - margin, ty - margin);
        bool IsPath(int tx, int ty)
        {
            var t = TypeAt(tx, ty);
            if (IsBuilding(tx, ty)) return false;
            if (t is TileType.Path or TileType.Floor or TileType.PC) return true;
            if (t != TileType.Signpost) return false;
            int paths = 0;
            foreach (var (dx, dy) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
                if (TypeAt(tx + dx, ty + dy) == TileType.Path) paths++;
            return paths >= 2;
        }

        var path = Mask(tw, th, IsPath, blur: 5);
        var water = Mask(tw, th, (x, y) => TypeAt(x, y) == TileType.Water, blur: 4);
        var forest = Mask(tw, th, (x, y) => TypeAt(x, y) is TileType.Tree or TileType.TreeTrunk, blur: 10);
        var tall = Mask(tw, th, (x, y) => TypeAt(x, y) == TileType.TallGrass, blur: 3);
        var walls = Mask(tw, th, IsBuilding, blur: 6);

        var c = new PixelCanvas(w, h);
        bool Inside(float[] m, int x, int y) => x >= 0 && y >= 0 && x < w && y < h && m[y * w + x] >= 0.5f;

        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int i = y * w + x;
                float gx = x / (float)T, gy = y / (float)T;

                // Lawn: flat base with clean-edged lighter patches
                var col = SoftCanvas.Fbm(gx / 3.5f, gy / 3.5f, 11) > 0.6f ? GrassLight : Grass;
                if (forest[i] >= 0.5f) col = Forest;
                if (tall[i] >= 0.5f) col = TallGround;

                // Stepped contact shade along walls
                float wall = walls[i];
                if (wall > 0.02f && wall < 0.5f) col = PixelCanvas.Mix(col, new Color(30, 70, 50, 255), wall > 0.2f ? 0.28f : 0.14f);

                if (path[i] >= 0.5f)
                {
                    // Rounded path with a one-texel rim, a light inner line on the lit (north/west) side and flat shade patches
                    col = Path;
                    bool edge = !Inside(path, x - 1, y) || !Inside(path, x + 1, y) || !Inside(path, x, y - 1) || !Inside(path, x, y + 1);
                    bool inner = !Inside(path, x - 2, y) || !Inside(path, x, y - 2);
                    if (edge) col = PathRim;
                    else if (inner) col = PathLight;
                }

                float wm = water[i];
                if (wm >= 0.5f) col = new Color(40, 90, 150, 255);
                else if (wm > 0.2f)
                {
                    bool inMap = map.InBounds((int)gx - margin, (int)gy - margin);
                    col = inMap ? Stone : Sand;
                }
                c.SetRaw(x, y, col);
            }

        // Placed details
        for (int ty = 0; ty < th; ty++)
            for (int tx = 0; tx < tw; tx++)
            {
                var t = TypeAt(tx, ty);
                int ox = tx * T, oy = ty * T;
                if (IsPath(tx, ty)) Pebbles(c, path, w, ox, oy, tx, ty);
                else if (t == TileType.FlowerGrass) Flowers(c, ox, oy, tx, ty);
                else if (t is TileType.Grass or TileType.Tree or TileType.TreeTrunk or TileType.LedgeDown && !IsBuilding(tx, ty)) Tufts(c, path, water, w, ox, oy, tx, ty);
                if (water.Length > 0) ShoreStones(c, water, w, ox, oy, tx, ty);
            }
        return c;
    }

    /// <summary>Two or three little "v" tufts per tile on a jittered grid, dark with a light tip.</summary>
    private static void Tufts(PixelCanvas c, float[] path, float[] water, int w, int ox, int oy, int tx, int ty)
    {
        int count = 2 + (SoftCanvas.Rand(tx, ty, 21) < 0.4f ? 1 : 0);
        for (int k = 0; k < count; k++)
        {
            int x = ox + 4 + (int)(SoftCanvas.Rand(tx, ty, 22 + k) * (T - 10));
            int y = oy + 6 + (int)(SoftCanvas.Rand(tx, ty, 26 + k) * (T - 10));
            if (path[y * w + x] > 0.3f || water[y * w + x] > 0.1f) continue;
            // Left and right blades leaning out, a taller middle blade
            c.SetRaw(x - 2, y - 1, Tuft); c.SetRaw(x - 1, y, Tuft); c.SetRaw(x - 2, y - 2, TuftTip);
            c.SetRaw(x, y, Tuft); c.SetRaw(x, y - 1, Tuft); c.SetRaw(x, y - 2, Tuft); c.SetRaw(x, y - 3, TuftTip);
            c.SetRaw(x + 1, y, Tuft); c.SetRaw(x + 2, y - 1, Tuft); c.SetRaw(x + 2, y - 2, TuftTip);
        }
    }

    private static void Pebbles(PixelCanvas c, float[] path, int w, int ox, int oy, int tx, int ty)
    {
        if (SoftCanvas.Rand(tx, ty, 31) > 0.5f) return;
        int x = ox + 6 + (int)(SoftCanvas.Rand(tx, ty, 32) * (T - 12));
        int y = oy + 6 + (int)(SoftCanvas.Rand(tx, ty, 33) * (T - 12));
        c.SetRaw(x, y, StoneLight); c.SetRaw(x + 1, y, Stone);
        c.SetRaw(x, y + 1, Stone); c.SetRaw(x + 1, y + 1, Stone);
        c.SetRaw(x, y + 2, PathShade); c.SetRaw(x + 1, y + 2, PathShade);
        if (SoftCanvas.Rand(tx, ty, 34) < 0.5f)
        {
            c.SetRaw(x + 5, y + 3, StoneLight);
            c.SetRaw(x + 5, y + 4, PathShade);
        }
    }

    /// <summary>Flower-bed tiles: four flowers with four petals, a centre and two leaves each.</summary>
    private static void Flowers(PixelCanvas c, int ox, int oy, int tx, int ty)
    {
        var leaf = new Color(60, 146, 76, 255);
        for (int k = 0; k < 4; k++)
        {
            int x = ox + 6 + (k % 2) * 14 + (int)(SoftCanvas.Rand(tx, ty, 41 + k) * 6);
            int y = oy + 6 + (k / 2) * 14 + (int)(SoftCanvas.Rand(tx, ty, 45 + k) * 6);
            float r = SoftCanvas.Rand(tx, ty, 49 + k);
            var petal = r < 0.15f ? new Color(236, 84, 96, 255) : r < 0.25f ? new Color(250, 210, 76, 255) : new Color(252, 252, 252, 255);
            var petalShade = r < 0.25f ? PixelCanvas.Shadow(petal, 0.25f) : new Color(200, 214, 236, 255);
            c.SetRaw(x - 2, y + 2, leaf); c.SetRaw(x + 2, y + 2, leaf); c.SetRaw(x, y + 2, leaf);
            c.SetRaw(x, y - 1, petal); c.SetRaw(x - 1, y, petal); c.SetRaw(x + 1, y, petal); c.SetRaw(x, y + 1, petalShade);
            c.SetRaw(x, y, new Color(250, 196, 64, 255));
        }
    }

    /// <summary>A row of rounded stones where the pond's stone band meets the water.</summary>
    private static void ShoreStones(PixelCanvas c, float[] water, int w, int ox, int oy, int tx, int ty)
    {
        for (int k = 0; k < 6; k++)
        {
            int x = ox + k * 5 + 2, y = oy + (int)(SoftCanvas.Rand(tx, ty, 60 + k) * T);
            int i = y * w + x;
            if (i < 0 || i >= water.Length || water[i] <= 0.28f || water[i] >= 0.45f) continue;
            c.SetRaw(x, y, StoneLight); c.SetRaw(x + 1, y, Stone); c.SetRaw(x, y + 1, Stone); c.SetRaw(x + 1, y + 1, new Color(128, 120, 112, 255));
        }
    }

    /// <summary>Tile flags upsampled to texels and blurred twice with a box of <paramref name="blur"/> texels.</summary>
    internal static float[] Mask(int tw, int th, Func<int, int, bool> on, int blur)
    {
        int w = tw * T, h = th * T;
        var m = new float[w * h];
        for (int ty = 0; ty < th; ty++)
            for (int tx = 0; tx < tw; tx++)
            {
                if (!on(tx, ty)) continue;
                for (int y = 0; y < T; y++)
                    Array.Fill(m, 1f, (ty * T + y) * w + tx * T, T);
            }
        var tmp = new float[w * h];
        for (int pass = 0; pass < 2; pass++)
        {
            BoxBlur(m, tmp, w, h, blur, horizontal: true);
            BoxBlur(tmp, m, w, h, blur, horizontal: false);
        }
        return m;
    }

    private static void BoxBlur(float[] src, float[] dst, int w, int h, int r, bool horizontal)
    {
        int lines = horizontal ? h : w, len = horizontal ? w : h;
        float inv = 1f / (r * 2 + 1);
        for (int l = 0; l < lines; l++)
        {
            int Idx(int k) => horizontal ? l * w + Math.Clamp(k, 0, len - 1) : Math.Clamp(k, 0, len - 1) * w + l;
            float sum = 0f;
            for (int k = -r; k <= r; k++) sum += src[Idx(k)];
            for (int k = 0; k < len; k++)
            {
                dst[Idx(k)] = sum * inv;
                sum += src[Idx(k + r + 1)] - src[Idx(k - r)];
            }
        }
    }
}
