using Raylib_cs;
using PokemonPlatinumEngine.Overworld;
using static PokemonPlatinumEngine.Graphics.PixelCanvas;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// A room's inner walls built as what they stand for (plan 20 · J15, part s; style guide, "Rooms", "Partitions of their
/// own"): hedges, counters, bulkheads, boulders, blocks of ice and pillars, a box a tile at a time whose top marks the
/// sides that look onto the floor, so a run of them reads as one piece. Our own drawing; no GPU calls but the kit's.
/// </summary>
internal static class PartitionArt
{
    private const int T = 32;

    /// <summary>How tall each look stands, in texels (a room's own inner wall is <see cref="PropModels.InnerWallHeight"/>, 28).</summary>
    public static int HeightOf(PartitionLook look) => look switch
    {
        PartitionLook.Hedge => 24,
        PartitionLook.Counter => 22,
        PartitionLook.Bulkhead => 28,
        PartitionLook.Rock => 22,
        PartitionLook.Ice => 24,
        PartitionLook.Pillar => 44,
        _ => PropModels.InnerWallHeight
    };

    /// <summary>
    /// One tile of a partition at (<paramref name="tx"/>, <paramref name="ty"/>): the faces that look onto the floor
    /// (<paramref name="south"/>, <paramref name="west"/>, <paramref name="east"/>) and a top marked along the sides
    /// that do (<paramref name="north"/> too). Boulders and pillars stand a tile at a time, so their sides all show.
    /// </summary>
    public static void Build(KitBuilder kit, PartitionLook look, int tx, int ty, bool north, bool south, bool west, bool east)
    {
        int h = HeightOf(look);
        // A boulder, a block of ice and a pillar are pieces of their own: each shows all its sides, set a little in
        bool apart = look is PartitionLook.Pillar;
        if (apart) north = south = west = east = true;
        int inset = look == PartitionLook.Pillar ? 4 : 0;
        int variant = look == PartitionLook.Rock ? (int)(Hash(tx, ty, 17) % 3) : 0;
        int fw = T - 2 * inset;
        var top = kit.Face($"partition.{look}.top.{(north ? "n" : "")}{(south ? "s" : "")}{(west ? "w" : "")}{(east ? "e" : "")}.{variant}",
            fw, fw, c => PaintTop(c, look, north, south, west, east, variant));
        var face = kit.Face($"partition.{look}.face.{variant}", fw, h, c => PaintFace(c, look, variant));
        kit.Box(tx * T + inset, tx * T + T - inset, ty * T + inset, ty * T + T - inset, 0, h, top,
            south ? face : null, west ? face : null, east ? face : null);
    }

    /// <summary>The top of a tile of a partition, its rim along the sides that look onto the floor.</summary>
    internal static void PaintTop(PixelCanvas c, PartitionLook look, bool north, bool south, bool west, bool east, int variant = 0)
    {
        int w = c.Width, h = c.Height;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                c.SetRaw(x, y, TopTexel(look, x, y, variant));
        var rim = RimOf(look);
        if (rim is { } r)
        {
            if (north) c.HLine(0, 0, w, r);
            if (south) c.HLine(0, h - 1, w, r);
            if (west) c.VLine(0, 0, h, r);
            if (east) c.VLine(w - 1, 0, h, r);
        }
    }

    /// <summary>A side of a tile of a partition, as tall as its look stands.</summary>
    internal static void PaintFace(PixelCanvas c, PartitionLook look, int variant = 0)
    {
        int w = c.Width, h = c.Height;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                c.SetRaw(x, y, FaceTexel(look, x, y, h, variant));
    }

    private static Color? RimOf(PartitionLook look) => look switch
    {
        PartitionLook.Hedge => Rgb(46, 96, 52),
        PartitionLook.Counter => Rgb(246, 238, 220),
        PartitionLook.Bulkhead => Rgb(220, 184, 64),
        PartitionLook.Ice => Rgb(236, 248, 255),
        _ => null
    };

    private static Color TopTexel(PartitionLook look, int x, int y, int variant)
    {
        uint n = Hash(x, y, 101 + variant);
        switch (look)
        {
            case PartitionLook.Hedge:
            {
                // Leaves in clumps of four texels, light at their upper left; a flower now and then
                uint clump = Hash(x / 4, y / 4, 211);
                if (clump % 23 == 0 && x % 4 == 1 && y % 4 == 1) return clump % 2 == 0 ? Rgb(244, 140, 176) : Rgb(250, 246, 236);
                var leaf = clump % 3 == 0 ? Rgb(78, 150, 72) : clump % 3 == 1 ? Rgb(68, 136, 66) : Rgb(88, 160, 78);
                return (x % 4 == 0 || y % 4 == 0) ? Shadow(leaf, 0.12f) : x % 4 == 1 && y % 4 == 1 ? Light1(leaf, 0.2f) : leaf;
            }
            case PartitionLook.Counter:
                // A pale laminate with a faint fleck
                return n % 19 == 0 ? Rgb(226, 218, 198) : Rgb(236, 228, 208);
            case PartitionLook.Bulkhead:
                // Steel plate with a seam at each tile's middle and rivets along it
                if (x == 15 || y == 15) return Rgb(98, 108, 122);
                if ((x == 13 || x == 17) && y % 6 == 3 || (y == 13 || y == 17) && x % 6 == 3) return Rgb(176, 184, 196);
                return n % 13 == 0 ? Rgb(126, 136, 150) : Rgb(134, 144, 158);
            case PartitionLook.Rock:
            {
                // A rounded top: lighter at its upper left, cracks of shade
                float dx = (x - (14 + variant)) / 16f, dy = (y - 12) / 16f;
                float lit = 1f - Math.Clamp(dx * dx + dy * dy, 0f, 1f);
                var stone = Mix(Rgb(122, 120, 126), Rgb(176, 174, 178), lit);
                if (Hash(x / 3, y / 2, 307 + variant) % 11 == 0) return Shadow(stone, 0.25f);
                return n % 7 == 0 ? Shadow(stone, 0.08f) : stone;
            }
            case PartitionLook.Ice:
                // Pale blue with a diagonal sheen
                return (x + y) % 22 < 3 ? Rgb(226, 242, 252) : n % 17 == 0 ? Rgb(172, 210, 236) : Rgb(186, 220, 242);
            case PartitionLook.Pillar:
                // The capital: pale stone with a raised square
                return x is 3 or 20 || y is 3 or 20 ? Rgb(196, 190, 176) : Rgb(226, 220, 204);
            default:
                return Rgb(128, 128, 128);
        }
    }

    private static Color FaceTexel(PartitionLook look, int x, int y, int h, int variant)
    {
        uint n = Hash(x, y, 401 + variant);
        switch (look)
        {
            case PartitionLook.Hedge:
            {
                // Leaves above a planter of cream stone eight texels deep
                if (y >= h - 8)
                    return y == h - 8 ? Rgb(244, 236, 214) : y == h - 1 ? Rgb(170, 156, 132) : x % 16 == 15 ? Rgb(196, 184, 160) : Rgb(222, 212, 188);
                uint clump = Hash(x / 4, y / 4, 503);
                var leaf = clump % 3 == 0 ? Rgb(66, 132, 62) : clump % 3 == 1 ? Rgb(56, 118, 56) : Rgb(74, 142, 68);
                if (y == h - 9) return Shadow(leaf, 0.3f);
                return (x % 4 == 0 || y % 4 == 0) ? Shadow(leaf, 0.14f) : leaf;
            }
            case PartitionLook.Counter:
            {
                // A laminate lip, pale wood panels, a dark kick at the foot
                if (y < 3) return y == 0 ? Rgb(246, 238, 220) : Rgb(210, 200, 180);
                if (y >= h - 3) return Rgb(110, 84, 64);
                if (x % 16 == 15) return Rgb(170, 128, 88);
                if (x % 16 == 0) return Rgb(220, 178, 132);
                return n % 9 == 0 ? Rgb(196, 152, 106) : Rgb(204, 160, 114);
            }
            case PartitionLook.Bulkhead:
            {
                // Grey plate in panels of sixteen, a row of rivets under its top, a band of black and yellow at its foot
                if (y >= h - 4) return ((x + y) / 3) % 2 == 0 ? Rgb(232, 192, 62) : Rgb(48, 46, 52);
                if (y == 0) return Rgb(176, 184, 196);
                if (y == 3 && x % 4 == 2) return Rgb(178, 186, 198);
                if (x % 16 == 15) return Rgb(92, 102, 116);
                if (x % 16 == 0) return Rgb(150, 160, 174);
                return n % 11 == 0 ? Rgb(114, 124, 138) : Rgb(120, 130, 144);
            }
            case PartitionLook.Rock:
            {
                // Rough stone, darker toward its foot, lit at its top
                float down = y / (float)h;
                var stone = Mix(Rgb(150, 148, 154), Rgb(92, 90, 98), down);
                if (Hash(x / 3, y / 2, 521 + variant) % 9 == 0) return Shadow(stone, 0.22f);
                if (y == 0) return Light1(stone, 0.3f);
                return n % 5 == 0 ? Shadow(stone, 0.08f) : stone;
            }
            case PartitionLook.Ice:
            {
                // Pale blue with a light top edge, streaks running down and a darker foot
                if (y == 0) return Rgb(236, 248, 255);
                if (y >= h - 2) return Rgb(122, 168, 204);
                if ((x * 3 + y) % 19 < 2) return Rgb(214, 236, 250);
                return n % 13 == 0 ? Rgb(150, 196, 228) : Rgb(162, 206, 236);
            }
            case PartitionLook.Pillar:
            {
                // Fluted pale stone between a capital and a base
                if (y < 5) return y == 0 ? Rgb(236, 230, 216) : y == 4 ? Rgb(176, 170, 156) : Rgb(220, 214, 198);
                if (y >= h - 5) return y == h - 5 ? Rgb(236, 230, 216) : y == h - 1 ? Rgb(150, 144, 132) : Rgb(212, 206, 190);
                int f = x % 6;
                return f == 0 ? Rgb(176, 170, 156) : f == 1 ? Rgb(232, 226, 210) : Rgb(214, 208, 192);
            }
            default:
                return Rgb(128, 128, 128);
        }
    }

    private static uint Hash(int x, int y, int salt)
    {
        uint h = (uint)(x * 374761393 + y * 668265263 + salt * 1442695041);
        h = (h ^ (h >> 13)) * 1274126177u;
        return h ^ (h >> 16);
    }
}
