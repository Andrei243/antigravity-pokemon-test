using System;
using System.Collections.Generic;
using Raylib_cs;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// The HD-2D look's ground: deliberate pixel art at the field's 32 texels per tile, shown point-filtered. Every
/// detail is a placed cluster (tuft marks, pebbles, flowers, rounded path edges with a one-texel rim); light
/// and shade come in two or three flat steps instead of per-texel noise. Water is part of it: the bake also
/// produces a mask of the water's surface and how far each texel is from the shore, which the water shader
/// turns into depth bands, foam and wave marks.
/// </summary>
internal static class PixelGround
{
    private const int T = GroundBaker.ArtTile;

    private static readonly Color Grass = new(104, 190, 98, 255);
    private static readonly Color GrassLight = new(120, 200, 102, 255);
    private static readonly Color Tuft = new(70, 150, 82, 255);
    private static readonly Color TuftTip = new(160, 222, 122, 255);
    private static readonly Color Forest = new(58, 126, 82, 255);
    private static readonly Color TallGround = new(48, 124, 70, 255);
    private static readonly Color Path = new(222, 204, 160, 255);
    private static readonly Color PathLight = new(236, 222, 186, 255);
    private static readonly Color PathShade = new(214, 186, 132, 255);
    private static readonly Color PathRim = new(166, 140, 104, 255);
    private static readonly Color Stone = new(168, 160, 150, 255);
    private static readonly Color StoneLight = new(208, 202, 192, 255);
    private static readonly Color Sand = new(238, 224, 172, 255);
    private static readonly Color SandLight = new(246, 236, 196, 255);

    // Water: the bank that shows where land lies to the north, and the bed under the surface
    private static readonly Color BankTop = new(124, 116, 116, 255);
    private static readonly Color BankBase = new(88, 84, 98, 255);
    private static readonly Color SandBankTop = new(196, 178, 134, 255);
    private static readonly Color SandBankBase = new(150, 134, 108, 255);
    private static readonly Color WaterBed = new(58, 118, 190, 255);

    /// <summary>How many texels of bank show below a north shore.</summary>
    public const int BankDepth = 5;

    /// <summary>The mask's red channel is the shore distance in texels times this (so 63 texels and beyond read as 252).</summary>
    public const int MaskScale = 4;

    /// <summary>The radius, in texels, that a boulder standing in water keeps dry around its middle.</summary>
    public const int RockFootprint = 11;

    /// <summary>A ground other than lawn and path: flat base, lighter patches, a darker rim where it meets other ground.</summary>
    private readonly record struct Kind(TileType Type, Color Base, Color Patch, Color Rim, Color Mark, int Salt);

    private static readonly Kind[] Kinds =
    {
        new(TileType.Sand, new(238, 224, 172, 255), new(246, 236, 196, 255), new(206, 188, 138, 255), new(214, 196, 140, 255), 71),
        new(TileType.Dirt, new(176, 136, 96, 255), new(194, 156, 112, 255), new(138, 102, 70, 255), new(146, 108, 74, 255), 72),
        new(TileType.Snow, new(204, 218, 240, 255), new(220, 230, 246, 255), new(168, 186, 222, 255), new(184, 200, 232, 255), 73),
        new(TileType.CaveFloor, new(112, 100, 104, 255), new(130, 118, 120, 255), new(74, 66, 80, 255), new(82, 72, 84, 255), 74)
    };

    public static PixelCanvas Bake(Map map, int margin, IReadOnlyList<BuildingInfo> buildings) => Bake(map, margin, buildings, out _);

    /// <param name="waterMask">
    /// Null when the map has no water. Otherwise a canvas the size of the ground: alpha marks the water's surface,
    /// red is the distance to the shore in texels times <see cref="MaskScale"/>.
    /// </param>
    public static PixelCanvas Bake(Map map, int margin, IReadOnlyList<BuildingInfo> buildings, out PixelCanvas? waterMask)
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
        var water = Mask(tw, th, (x, y) => TypeAt(x, y) == TileType.Water, blur: 8);
        var forest = Mask(tw, th, (x, y) => TypeAt(x, y) is TileType.Tree or TileType.TreeTrunk, blur: 10);
        var tall = Mask(tw, th, (x, y) => TypeAt(x, y) == TileType.TallGrass, blur: 3);
        var walls = Mask(tw, th, IsBuilding, blur: 6);

        // Only the kinds this map uses get a mask
        var kindMasks = new float[Kinds.Length][];
        for (int k = 0; k < Kinds.Length; k++)
        {
            var type = Kinds[k].Type;
            bool used = false;
            for (int ty = 0; ty < map.Height && !used; ty++)
                for (int tx = 0; tx < map.Width && !used; tx++)
                    used = map.GetGroundTile(tx, ty) == type;
            if (used) kindMasks[k] = Mask(tw, th, (x, y) => TypeAt(x, y) == type, blur: 5);
        }

        var c = new PixelCanvas(w, h);
        bool Inside(float[] m, int x, int y) => x >= 0 && y >= 0 && x < w && y < h && m[y * w + x] >= 0.5f;
        bool Edge(float[] m, int x, int y) => !Inside(m, x - 1, y) || !Inside(m, x + 1, y) || !Inside(m, x, y - 1) || !Inside(m, x, y + 1);

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

                bool sandy = false;
                for (int k = 0; k < Kinds.Length; k++)
                {
                    var mask = kindMasks[k];
                    if (mask == null) continue;
                    if (Kinds[k].Type == TileType.Sand && mask[i] > 0.25f) sandy = true;
                    if (mask[i] < 0.5f) continue;
                    col = Edge(mask, x, y) ? Kinds[k].Rim
                        : SoftCanvas.Fbm(gx / 3f, gy / 3f, Kinds[k].Salt) > 0.58f ? Kinds[k].Patch : Kinds[k].Base;
                }

                if (path[i] >= 0.5f)
                {
                    // Rounded path with a one-texel rim, a light inner line on the lit (north/west) side and flat shade patches
                    col = Path;
                    bool inner = !Inside(path, x - 2, y) || !Inside(path, x, y - 2);
                    if (Edge(path, x, y)) col = PathRim;
                    else if (inner) col = PathLight;
                }

                // A band of stones (or sand, on beaches and past the map's edge) around water
                float wm = water[i];
                if (wm > 0.3f && wm < 0.5f)
                {
                    bool inMap = map.InBounds((int)gx - margin, (int)gy - margin);
                    col = inMap && !sandy ? Stone : Sand;
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
                else
                {
                    for (int k = 0; k < Kinds.Length; k++)
                        if (t == Kinds[k].Type && kindMasks[k] != null) KindMarks(c, Kinds[k], kindMasks[k], w, ox, oy, tx, ty);
                }
                ShoreStones(c, water, w, ox, oy, tx, ty);
            }

        waterMask = PaintWater(c, map, margin, water, w, h);
        return c;
    }

    // ------------------------------------------------------------------ water

    /// <summary>
    /// Paints the shore's rim, the bank under north shores and the bed, and returns the mask of the water's
    /// surface with each texel's distance from the shore (null if there is no water).
    /// </summary>
    private static PixelCanvas? PaintWater(PixelCanvas c, Map map, int margin, float[] water, int w, int h)
    {
        var surface = new bool[w * h];
        bool any = false;
        bool IsWater(int x, int y) => x < 0 || y < 0 || x >= w || y >= h || water[y * w + x] >= 0.5f;

        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int i = y * w + x;
                if (water[i] < 0.5f)
                {
                    // The last texel of land before the water catches the light
                    if (IsWater(x, y + 1) || IsWater(x, y - 1) || IsWater(x - 1, y) || IsWater(x + 1, y))
                        c.SetRaw(x, y, SameColor(c.Get(x, y), Sand) ? SandLight : StoneLight);
                    continue;
                }

                int rows = 0;
                for (int d = 1; d <= BankDepth && rows == 0; d++)
                    if (!IsWater(x, y - d)) rows = d;
                if (rows > 0)
                {
                    bool sand = SameColor(c.Get(x, y - rows), SandLight) || SameColor(c.Get(x, y - rows), Sand);
                    c.SetRaw(x, y, rows <= 2 ? (sand ? SandBankTop : BankTop) : (sand ? SandBankBase : BankBase));
                    continue;
                }
                c.SetRaw(x, y, WaterBed);
                surface[i] = true;
                any = true;
            }
        if (!any) return null;

        // Boulders standing in the water keep a dry patch under them, so foam laps round each one
        foreach (var prop in map.Props)
        {
            if (prop.Type != PropType.Boulder) continue;
            int cx = (int)((prop.X + margin + prop.Width / 2f) * T), cy = (int)((prop.Y + margin + prop.Depth / 2f) * T);
            if (cx < 0 || cy < 0 || cx >= w || cy >= h || !surface[cy * w + cx]) continue;
            for (int y = cy - RockFootprint; y <= cy + RockFootprint; y++)
                for (int x = cx - RockFootprint; x <= cx + RockFootprint; x++)
                {
                    if (x < 0 || y < 0 || x >= w || y >= h) continue;
                    float dx = x - cx, dy = (y - cy) * 1.3f;
                    if (dx * dx + dy * dy > RockFootprint * RockFootprint) continue;
                    surface[y * w + x] = false;
                    c.SetRaw(x, y, BankBase);
                }
        }

        var distance = ShoreDistance(surface, w, h);
        var mask = new PixelCanvas(w, h);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int i = y * w + x;
                if (surface[i]) mask.SetRaw(x, y, new Color(Math.Min(252, (int)MathF.Round(distance[i] * MaskScale)), 0, 0, 255));
            }
        return mask;
    }

    private static bool SameColor(Color a, Color b) => a.R == b.R && a.G == b.G && a.B == b.B;

    /// <summary>
    /// For every texel of <paramref name="surface"/>, the distance in texels to the nearest texel outside it
    /// (0 outside), by a two-pass 3-4 chamfer.
    /// </summary>
    internal static float[] ShoreDistance(bool[] surface, int w, int h)
    {
        const int far = 1 << 20;
        var d = new int[w * h];
        for (int i = 0; i < d.Length; i++) d[i] = surface[i] ? far : 0;

        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int i = y * w + x;
                if (d[i] == 0) continue;
                int v = d[i];
                if (x > 0) v = Math.Min(v, d[i - 1] + 3);
                if (y > 0)
                {
                    v = Math.Min(v, d[i - w] + 3);
                    if (x > 0) v = Math.Min(v, d[i - w - 1] + 4);
                    if (x < w - 1) v = Math.Min(v, d[i - w + 1] + 4);
                }
                d[i] = v;
            }
        for (int y = h - 1; y >= 0; y--)
            for (int x = w - 1; x >= 0; x--)
            {
                int i = y * w + x;
                if (d[i] == 0) continue;
                int v = d[i];
                if (x < w - 1) v = Math.Min(v, d[i + 1] + 3);
                if (y < h - 1)
                {
                    v = Math.Min(v, d[i + w] + 3);
                    if (x < w - 1) v = Math.Min(v, d[i + w + 1] + 4);
                    if (x > 0) v = Math.Min(v, d[i + w - 1] + 4);
                }
                d[i] = v;
            }

        var result = new float[w * h];
        for (int i = 0; i < d.Length; i++) result[i] = d[i] / 3f;
        return result;
    }

    // ------------------------------------------------------------------ placed details

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
            if (i < 0 || i >= water.Length || water[i] <= 0.35f || water[i] >= 0.47f) continue;
            if (!SameColor(c.Get(x, y), Stone)) continue;
            c.SetRaw(x, y, StoneLight); c.SetRaw(x + 1, y, Stone); c.SetRaw(x, y + 1, Stone); c.SetRaw(x + 1, y + 1, new Color(128, 120, 112, 255));
        }
    }

    /// <summary>The marks of sand (ripple arcs), dirt (clods), snow (drift lines) and cave floors (cracks and pebbles).</summary>
    private static void KindMarks(PixelCanvas c, Kind kind, float[] mask, int w, int ox, int oy, int tx, int ty)
    {
        for (int k = 0; k < 2; k++)
        {
            if (SoftCanvas.Rand(tx, ty, kind.Salt * 10 + k) > 0.7f) continue;
            int x = ox + 3 + (int)(SoftCanvas.Rand(tx, ty, kind.Salt * 10 + 2 + k) * (T - 12));
            int y = oy + 3 + k * 14 + (int)(SoftCanvas.Rand(tx, ty, kind.Salt * 10 + 4 + k) * 8);
            if (mask[y * w + x] < 0.75f || mask[(y + 3) * w + x + 6] < 0.75f) continue;

            switch (kind.Type)
            {
                case TileType.Sand:
                    // A ripple: a shallow arc with its ends dipped
                    c.SetRaw(x, y + 1, kind.Mark);
                    for (int d = 1; d <= 4; d++) c.SetRaw(x + d, y, kind.Mark);
                    c.SetRaw(x + 5, y + 1, kind.Mark);
                    break;
                case TileType.Dirt:
                    c.SetRaw(x, y, kind.Patch); c.SetRaw(x + 1, y, kind.Mark);
                    c.SetRaw(x, y + 1, kind.Mark); c.SetRaw(x + 1, y + 1, kind.Mark);
                    c.SetRaw(x + 4, y + 2, kind.Mark); c.SetRaw(x + 5, y + 2, kind.Mark);
                    break;
                case TileType.Snow:
                    for (int d = 0; d < 3; d++) c.SetRaw(x + d, y, kind.Mark);
                    c.SetRaw(x + 3, y + 1, kind.Mark); c.SetRaw(x + 4, y + 1, kind.Mark);
                    break;
                default:
                    if (k == 0)
                    {
                        c.SetRaw(x, y, kind.Mark); c.SetRaw(x + 1, y + 1, kind.Mark); c.SetRaw(x + 2, y + 1, kind.Mark);
                        c.SetRaw(x + 3, y + 2, kind.Mark); c.SetRaw(x + 4, y + 2, kind.Mark); c.SetRaw(x + 4, y + 3, kind.Mark);
                    }
                    else
                    {
                        var pebble = new Color(150, 140, 140, 255);
                        c.SetRaw(x, y, pebble); c.SetRaw(x + 1, y, pebble);
                        c.SetRaw(x, y + 1, kind.Mark); c.SetRaw(x + 1, y + 1, kind.Mark);
                    }
                    break;
            }
        }
    }

    // ------------------------------------------------------------------ masks

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
