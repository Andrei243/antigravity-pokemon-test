using System;
using Raylib_cs;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// The landmarks of Amity Square (plan 06 · R16c; style guide, "Amity Square"): the huts of its ruins, which are
/// walked into, and the desks its two receptionists stand behind. No GPU calls.
/// </summary>
internal static partial class Landmarks
{
    /// <summary>
    /// A hut's walls and the slab that lies on them, in screen rows: together well over the head of whoever walks in,
    /// who is not seen inside. Its doorway, in texels across and rows up.
    /// </summary>
    public const int HutWall = 68, HutSlab = 12, HutDoorWide = 26, HutDoorTall = 50;

    private static readonly Color RuinLight = Rgb(200, 196, 192), RuinMid = Rgb(160, 156, 158), RuinDark = Rgb(112, 108, 122),
        RuinSeam = Rgb(76, 72, 90), RuinGloom = Rgb(30, 28, 42);

    /// <summary>
    /// A hut of the ruins: three walls of great blocks under one slab, with a doorway in its south face over the open
    /// tile of the model's south row. The room behind it (the open tiles under the slab) is the world's: whoever
    /// steps through the doorway is under the slab and out of sight, as in the original. The hut is the room's three
    /// tiles and a wall's either side; the sixth tile of the model's box is the square's own walls or open ground.
    /// </summary>
    private static void RuinHut(KitBuilder kit, Map map, Prop prop, int w, int d)
    {
        const int tile = GroundBaker.ArtTile;
        int south = prop.Y + prop.Depth - 1, door = -1;
        for (int x = prop.X; x < prop.X + prop.Width && door < 0; x++)
            if (!map.IsSolid(x, south) && !map.IsSolid(x, south - 1)) door = x - prop.X;
        int first = door < 0 ? 0 : Math.Max(0, door - 2), last = door < 0 ? prop.Width - 1 : Math.Min(prop.Width - 1, door + 2);
        int x0 = first * tile + 2, x1 = (last + 1) * tile - 2, z0 = 5, z1 = d - 3;
        int doorAt = door < 0 ? -1 : door * tile - x0 + (tile - HutDoorWide) / 2;

        var front = kit.Face($"ruinhut.front.{x1 - x0}.{doorAt}", x1 - x0, HutWall, c => PaintRuinWall(c, doorAt));
        var side = kit.Face($"ruinhut.side.{z1 - z0}", z1 - z0, HutWall, c => PaintRuinWall(c, -1));
        kit.Box(x0, x1, z0, z1, 0, HutWall, south: front, west: side, east: side);
        // The slab: one great stone lying over the walls, a little proud of them all round
        int sx0 = x0 - 4, sx1 = x1 + 4, sz0 = z0 - 2, sz1 = z1 + 4;
        var top = kit.Face($"ruinhut.slab.{sx1 - sx0}x{sz1 - sz0}", sx1 - sx0, sz1 - sz0, PaintRuinSlab);
        var edge = kit.Face($"ruinhut.edge.{sx1 - sx0}", sx1 - sx0, HutSlab, PaintRuinEdge);
        var edgeSide = kit.Face($"ruinhut.edge.{sz1 - sz0}", sz1 - sz0, HutSlab, PaintRuinEdge);
        kit.Box(sx0, sx1, sz0, sz1, HutWall, HutWall + HutSlab, top, edge, edgeSide, edgeSide);
    }

    /// <summary>
    /// A wall of the ruins: courses of great blocks laid dry and worn pale, each a little its own tone, with a doorway
    /// <paramref name="doorAt"/> texels in (none when negative): two uprights, a lintel over them and the dark between.
    /// </summary>
    public static void PaintRuinWall(PixelCanvas c, int doorAt)
    {
        int w = c.Width, h = c.Height;
        c.Rect(0, 0, w, h, RuinMid);
        int y = 0;
        for (int course = 0; y < h; course++)
        {
            int tall = 13 + course * 7 % 6;
            if (h - y - tall < 8) tall = h - y;
            int x = -(course * 11 % 17);
            for (int block = 0; x < w; block++)
            {
                int wide = 24 + (course * 5 + block * 13) % 18;
                int bx = Math.Max(0, x), bw = Math.Min(w, x + wide) - bx;
                if (bw > 0)
                {
                    int step = (course * 3 + block * 7) % 5 - 2;
                    var tone = step >= 0 ? PixelCanvas.Mix(RuinMid, RuinLight, step * 0.22f) : PixelCanvas.Mix(RuinMid, RuinDark, -step * 0.14f);
                    c.Rect(bx, y, bw, tall, tone);
                    c.HLine(bx, y, bw, PixelCanvas.Mix(tone, RuinLight, 0.55f));
                    c.VLine(bx, y, tall, PixelCanvas.Mix(tone, RuinLight, 0.3f));
                    c.VLine(bx + bw - 1, y, tall, PixelCanvas.Mix(tone, RuinDark, 0.6f));
                    c.HLine(bx, y + tall - 1, bw, RuinSeam);
                }
                x += wide;
            }
            y += tall;
        }
        // The foot stands in its own shade, and a little moss has crept up it from the lawn
        Pix.Shade(c, 0, h - 5, w, 5, 0.16f);
        var moss = Rgb(104, 138, 96);
        for (int x = 3; x < w - 4; x += 9 + x * 7 % 5)
        {
            int tall = 1 + x * 5 % 3;
            c.Rect(x, h - tall, 3 + x % 2, tall, moss);
        }
        if (doorAt < 0) return;

        // The doorway: an upright either side, the lintel across them, and the dark of the room behind
        const int jamb = 7, lintel = 10;
        int top = h - HutDoorTall;
        Pix.Raised(c, doorAt - jamb, top - lintel, HutDoorWide + 2 * jamb, lintel, Stone);
        Pix.Raised(c, doorAt - jamb, top, jamb, HutDoorTall, Stone);
        Pix.Raised(c, doorAt + HutDoorWide, top, jamb, HutDoorTall, Stone);
        c.Rect(doorAt, top, HutDoorWide, HutDoorTall, RuinGloom);
        // Daylight reaches a little way in over the sill
        for (int i = 0; i < 9; i++)
            c.HLine(doorAt, h - 1 - i, HutDoorWide, PixelCanvas.Mix(RuinGloom, RuinDark, (9 - i) / 14f));
        c.VLine(doorAt, top, HutDoorTall, Rgb(18, 16, 28));
        c.HLine(doorAt, top, HutDoorWide, Rgb(18, 16, 28));
    }

    /// <summary>The slab over a hut, seen from above: one worn stone, cracked across, with lichen in its hollows.</summary>
    public static void PaintRuinSlab(PixelCanvas c)
    {
        int w = c.Width, h = c.Height;
        c.Rect(0, 0, w, h, PixelCanvas.Mix(RuinMid, RuinLight, 0.45f));
        c.HLine(0, h - 1, w, RuinLight);
        c.HLine(0, h - 2, w, RuinLight);
        c.VLine(0, 0, h, RuinLight);
        c.VLine(w - 1, 0, h, RuinMid);
        c.HLine(0, 0, w, RuinMid);
        // Two cracks that wander across it
        for (int crack = 0; crack < 2; crack++)
        {
            int y = h / 4 + crack * h / 2;
            for (int x = 6 + crack * 14; x < w - 8; x++)
            {
                if (x * 7 % 23 == crack) y += x % 2 == 0 ? 1 : -1;
                y = Math.Clamp(y, 3, h - 5);
                if (x * 3 % 29 > 3) c.Set(x, y, RuinDark);
            }
        }
        // Lichen: a few flat patches of grey green
        var lichen = Rgb(150, 166, 140);
        for (int patch = 0; patch < 5; patch++)
        {
            int x = 10 + patch * (w - 24) / 5 + patch * 7 % 9, y = 5 + patch * 11 % Math.Max(1, h - 14);
            c.Rect(x, y, 5 + patch % 3, 3, lichen);
            c.Rect(x + 1, y - 1, 3 + patch % 2, 5, lichen);
        }
    }

    /// <summary>The edge of the slab: a band of the same stone, lit along its top and dark along its foot.</summary>
    public static void PaintRuinEdge(PixelCanvas c)
    {
        int w = c.Width, h = c.Height;
        c.Rect(0, 0, w, h, RuinMid);
        c.HLine(0, 0, w, RuinLight);
        c.HLine(0, 1, w, PixelCanvas.Mix(RuinMid, RuinLight, 0.5f));
        c.HLine(0, h - 2, w, RuinDark);
        c.HLine(0, h - 1, w, RuinSeam);
        for (int x = 17; x < w - 6; x += 31 + x % 7) c.VLine(x, 2, h - 4, RuinDark);
    }

    /// <summary>
    /// A receptionist's desk in Amity Square: a counter down the tile she serves over (the original's Counter
    /// behaviour, east or west of her) with a return at each end, which leaves the tile she stands on open.
    /// </summary>
    private static void ReceptionDesk(KitBuilder kit, Map map, Prop prop, int w, int d)
    {
        const int tile = GroundBaker.ArtTile;
        bool servesEast = map.BehaviourAt(prop.X + prop.Width - 1, prop.Y + prop.Depth / 2) == TileBehavior.Counter;
        int half = w / 2, z0 = 5, z1 = d - 4;
        Art Top(int cw, int cd) => kit.Face($"booth.counter.top.{cw}x{cd}", cw, cd, c => PaintCounter(c, top: true, board: false));
        Art Front(int cw) => kit.Face($"booth.counter.{cw}.plain", cw, BoothCounter, c => PaintCounter(c, top: false, board: false));

        int sx0 = servesEast ? half + 2 : 3, sx1 = servesEast ? w - 3 : half - 2;
        kit.Box(sx0, sx1, z0, z1, 0, BoothCounter, Top(sx1 - sx0, z1 - z0), Front(sx1 - sx0), Front(z1 - z0), Front(z1 - z0));
        int rx0 = servesEast ? 3 : half - 2, rx1 = servesEast ? half + 2 : w - 3, deep = tile - 6 - z0;
        kit.Box(rx0, rx1, z0, z0 + deep, 0, BoothCounter, Top(rx1 - rx0, deep), Front(rx1 - rx0), Front(deep), Front(deep));
        kit.Box(rx0, rx1, z1 - deep, z1, 0, BoothCounter, Top(rx1 - rx0, deep), Front(rx1 - rx0), Front(deep), Front(deep));
    }
}
