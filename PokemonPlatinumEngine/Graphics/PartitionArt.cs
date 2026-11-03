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
        PartitionLook.Table => 18,
        PartitionLook.Crate => 26,
        PartitionLook.Console => 24,
        PartitionLook.Case => 24,
        PartitionLook.Lattice => 28,
        PartitionLook.Fence => 18,
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
        int variant = look is PartitionLook.Rock or PartitionLook.Crate ? (int)(Hash(tx, ty, 17) % 3) : 0;
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
        PartitionLook.Table => Rgb(84, 54, 34),
        PartitionLook.Console => Rgb(150, 144, 128),
        PartitionLook.Case => Rgb(196, 160, 72),
        PartitionLook.Lattice => Rgb(62, 40, 28),
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
            case PartitionLook.Table:
                // Planks of dark wood running across, a seam every eight texels, a faint grain
                if (y % 8 == 7) return Rgb(96, 62, 40);
                return Hash(x / 5, y, 113) % 6 == 0 ? Rgb(130, 88, 58) : Rgb(140, 96, 64);
            case PartitionLook.Crate:
            {
                // Two crates to a tile, their lids of pale slats framed darker
                int cx = x % 16, cy = y % 16;
                if (cx is 0 or 15 || cy is 0 or 15) return Rgb(132, 96, 58);
                if (cy % 5 == 0) return Rgb(160, 120, 76);
                var slat = variant == 1 ? Rgb(196, 156, 104) : variant == 2 ? Rgb(186, 146, 96) : Rgb(204, 164, 110);
                return n % 13 == 0 ? Shadow(slat, 0.1f) : slat;
            }
            case PartitionLook.Console:
            {
                // A cream steel desk with a dark panel of lit buttons set in its back half
                if (x < 6 || x > 25 || y < 5 || y > 14) return n % 17 == 0 ? Rgb(206, 200, 182) : Rgb(222, 216, 198);
                if (x is 6 or 25 || y is 5 or 14) return Rgb(150, 144, 128);
                if ((x - 9) % 4 == 0 && (y - 8) % 3 == 0) return Hash(x, y, 7) % 3 == 0 ? Rgb(232, 84, 72) : Hash(x, y, 9) % 2 == 0 ? Rgb(108, 214, 128) : Rgb(244, 208, 84);
                return Rgb(58, 64, 78);
            }
            case PartitionLook.Case:
            {
                // Glass over a bed of deep teal cloth with a pale stone laid on it, a sheen across the glass
                if (x + y is 20 or 21) return Rgb(206, 228, 234);
                float dx = (x - 15.5f) / 5f, dy = (y - 15.5f) / 3f;
                if (dx * dx + dy * dy <= 1f) return dy < -0.3f ? Rgb(236, 226, 204) : Rgb(212, 200, 174);
                return n % 19 == 0 ? Rgb(52, 84, 92) : Rgb(60, 94, 102);
            }
            case PartitionLook.Lattice:
            {
                // Paper between a grid of dark timber, the top rail wider
                if (x % 8 == 0 || y % 8 == 0) return Rgb(84, 56, 38);
                return n % 23 == 0 ? Rgb(232, 224, 204) : Rgb(242, 236, 218);
            }
            case PartitionLook.Fence:
            {
                // A top rail of weathered wood over packed earth, a wisp of straw here and there
                if (y >= 12 && y <= 19) return y is 12 or 19 ? Rgb(110, 82, 56) : Rgb(156, 120, 84);
                uint wisp = Hash(x / 2, y, 61);
                return wisp % 9 == 0 ? Rgb(206, 174, 96) : wisp % 4 == 0 ? Rgb(112, 90, 66) : Rgb(124, 100, 74);
            }
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
            case PartitionLook.Table:
            {
                // A top's edge four texels deep, then the shade under it with a stout leg at each end
                if (y < 4) return y == 0 ? Rgb(160, 112, 76) : y == 3 ? Rgb(96, 62, 40) : Rgb(128, 86, 56);
                bool leg = x is >= 2 and <= 5 or >= 26 and <= 29;
                if (leg) return x is 2 or 26 ? Rgb(124, 84, 54) : Rgb(104, 68, 44);
                return Rgb(52, 40, 34);
            }
            case PartitionLook.Crate:
            {
                // Two crates to a tile, their sides of planks with a frame, a cross brace and a stencilled mark
                int cx = x % 16;
                if (cx is 0 or 15 || y is 0 || y == h - 1) return Rgb(120, 86, 52);
                if (y == 1 || cx == 1) return Rgb(214, 176, 120);
                if (Math.Abs(cx - 1 - (y - 1) * 14 / Math.Max(1, h - 2)) <= 1) return Rgb(146, 106, 64);
                if (y % 6 == 0) return Rgb(156, 116, 72);
                if (x / 16 == variant % 2 && cx >= 9 && cx <= 12 && y >= h / 2 && y <= h / 2 + 3) return Rgb(60, 52, 48);
                return n % 9 == 0 ? Rgb(178, 136, 86) : Rgb(188, 146, 94);
            }
            case PartitionLook.Console:
            {
                // Cream steel panels, a dark sloping band of lit buttons under the top, a kick at the foot
                if (y == 0) return Rgb(236, 230, 214);
                if (y < 7) return (x % 5 == 2 && y == 4) ? (Hash(x, 3, 13) % 2 == 0 ? Rgb(108, 214, 128) : Rgb(244, 208, 84)) : Rgb(58, 64, 78);
                if (y >= h - 3) return Rgb(84, 86, 96);
                if (x % 16 == 15) return Rgb(178, 172, 154);
                if (x % 16 == 0) return Rgb(232, 226, 210);
                if (y == 12 && x % 16 is >= 5 and <= 10) return Rgb(150, 144, 128);
                return n % 13 == 0 ? Rgb(206, 200, 182) : Rgb(214, 208, 190);
            }
            case PartitionLook.Case:
            {
                // A brass-edged glass front over a plinth of dark wood half its height
                int glass = h / 2;
                if (y == 0 || y == glass) return Rgb(196, 160, 72);
                if (y < glass) return (x + y) % 14 < 2 ? Rgb(220, 238, 244) : x % 16 == 15 ? Rgb(196, 160, 72) : Rgb(150, 176, 186);
                if (y >= h - 2) return Rgb(52, 34, 26);
                return x % 16 == 15 ? Rgb(70, 46, 32) : n % 11 == 0 ? Rgb(92, 60, 40) : Rgb(100, 66, 44);
            }
            case PartitionLook.Lattice:
            {
                // Paper panels in a grid of dark timber, a deep rail at the top and the foot
                if (y < 3 || y >= h - 4) return y == 0 ? Rgb(110, 76, 52) : Rgb(70, 46, 32);
                if (x % 8 == 0 || (y - 3) % 7 == 0) return Rgb(84, 56, 38);
                return n % 29 == 0 ? Rgb(228, 220, 198) : Rgb(240, 234, 214);
            }
            case PartitionLook.Fence:
            {
                // Two rails between posts at each end of the tile, straw heaped at their foot
                if (y >= h - 4) return Hash(x / 2, y, 67) % 4 == 0 ? Rgb(196, 162, 84) : Rgb(220, 188, 104);
                bool post = x is 0 or 1 or 30 or 31;
                if (post) return x is 0 or 30 ? Rgb(132, 98, 66) : Rgb(108, 78, 52);
                if (y is >= 1 and <= 4 || y is >= 8 and <= 11) return y is 1 or 8 ? Rgb(170, 134, 96) : Rgb(146, 112, 78);
                return Rgb(82, 64, 50);
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
