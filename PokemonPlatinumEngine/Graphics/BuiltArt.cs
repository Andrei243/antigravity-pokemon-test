using System;
using Raylib_cs;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// The art of Johto's built places that are no plain room or cave (plan 20, wave 77; style guide, "Rooms", "A wooden
/// tower" and "Walkways over the dark", and "Caves", "Caves that are built"): the timber a tower's and a gym's floors
/// stand on and the rails along their edges, Sprout Tower's pillar, and the floors, rock tops, faces and steles of a cave
/// that is a building (the Burned Tower's charred timbers, the Ruins of Alph's dressed stone). No GPU calls.
/// </summary>
internal static class BuiltArt
{
    private const int T = GroundBaker.ArtTile;

    private static Color Rgb(int r, int g, int b) => new(r, g, b, 255);

    // ------------------------------------------------------------------ timber over the dark

    /// <summary>The timber of beams, posts and rails.</summary>
    public static readonly Tone Timber = Tone.Of(120, 82, 52, 156, 112, 74, 80, 54, 34);

    /// <summary>
    /// The face a wooden floor stands on over the dark (a tower's, the Violet Gym's walkways), 32 by
    /// <see cref="NatureArt.FaceCap"/> + <see cref="NatureArt.FaceBody"/>: a beam along the cap, lit along its top, then a
    /// trestle in the body, a post at each end of the tile and a brace across between them, with the dark under the floor
    /// showing between its timbers.
    /// </summary>
    public static PixelCanvas TimberFace()
    {
        var c = new PixelCanvas(32, NatureArt.FaceCap + NatureArt.FaceBody);
        var dark = Rgb(30, 24, 26);
        var brace = Rgb(96, 64, 42);
        var braceLight = Rgb(124, 86, 56);
        for (int y = 0; y < c.Height; y++)
            for (int x = 0; x < 32; x++)
            {
                Color col;
                if (y < NatureArt.FaceCap)
                    col = y == 0 ? Timber.Light : y >= NatureArt.FaceCap - 2 ? Timber.Dark : y % 5 == 3 && x % 11 < 6 ? Timber.Dark : Timber.Base;
                else
                {
                    // The body repeats whole (48 rows): a post of six texels at the tile's west end and one cross beam three
                    // rows deep between the posts at the body's head, the dark under the floor between them
                    int row = y - NatureArt.FaceCap;
                    if (x < 6) col = x == 0 ? Timber.Light : x == 5 ? Timber.Dark : Timber.Base;
                    else col = row < 3 ? (row == 0 ? braceLight : brace) : dark;
                }
                c.SetRaw(x, y, col);
            }
        return c;
    }

    /// <summary>A rail's post, four texels square and <paramref name="height"/> high, lit on its west.</summary>
    public static void PaintPost(PixelCanvas c)
    {
        for (int y = 0; y < c.Height; y++)
            for (int x = 0; x < c.Width; x++)
                c.SetRaw(x, y, y == 0 ? Timber.Light : x == 0 ? Timber.Light : x == c.Width - 1 ? Timber.Dark : Timber.Base);
    }

    /// <summary>A rail's bar, lit along its top and dark along its foot.</summary>
    public static void PaintBar(PixelCanvas c)
    {
        for (int y = 0; y < c.Height; y++)
            for (int x = 0; x < c.Width; x++)
                c.SetRaw(x, y, y == 0 ? Timber.Light : y == c.Height - 1 ? Timber.Dark : Timber.Base);
    }

    /// <summary>The cap of a post or a bar seen from above.</summary>
    public static void PaintCap(PixelCanvas c)
    {
        for (int y = 0; y < c.Height; y++)
            for (int x = 0; x < c.Width; x++)
                c.SetRaw(x, y, x == 0 || y == 0 ? Timber.Light : Timber.Base);
    }

    // ------------------------------------------------------------------ Sprout Tower's pillar

    /// <summary>The pillar's own timber: a dark old trunk, darker than the floor's boards.</summary>
    public static readonly Tone Trunk = Tone.Of(104, 70, 46, 132, 92, 60, 70, 46, 30);

    /// <summary>
    /// A face of Sprout Tower's pillar (style guide, "A wooden tower"): the trunk's grain in streaks down its length, a band
    /// of dark iron two texels deep every 40 rows with a rivet every 8 across, and a rope of straw (<c>226,206,150</c>)
    /// round it four texels deep near its foot. Lit on its west edge, shaded on its east.
    /// </summary>
    public static void PaintPillar(PixelCanvas c)
    {
        var iron = Rgb(52, 50, 58);
        var rivet = Rgb(120, 118, 128);
        var straw = Tone.Of(226, 206, 150);
        int w = c.Width, h = c.Height;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int fromFoot = h - 1 - y;
                Color col;
                if (fromFoot >= 18 && fromFoot < 22) col = (x + fromFoot) % 6 < 3 ? straw.Base : fromFoot == 21 ? straw.Light : straw.Dark;
                else if (y % 40 is 6 or 7) col = x % 8 == 4 && y % 40 == 6 ? rivet : iron;
                else
                {
                    // Grain: streaks a texel wide, placed by the column, broken here and there along their length
                    bool streak = GroundBaker.Rand01(x, y / 9, 631) < 0.16f;
                    col = x == 0 ? Trunk.Light : x >= w - 2 ? Trunk.Dark : streak ? PixelCanvas.Shadow(Trunk.Base, 0.18f) : Trunk.Base;
                }
                c.SetRaw(x, y, col);
            }
    }

    /// <summary>The pillar's top: the end of the trunk, rings of its grain round a dark heart.</summary>
    public static void PaintPillarTop(PixelCanvas c)
    {
        float cx = (c.Width - 1) / 2f, cy = (c.Height - 1) / 2f;
        for (int y = 0; y < c.Height; y++)
            for (int x = 0; x < c.Width; x++)
            {
                float r = MathF.Max(MathF.Abs(x - cx), MathF.Abs(y - cy));
                int ring = (int)(r / 5f);
                c.SetRaw(x, y, x == 0 || y == 0 ? Trunk.Light : r < 3f ? Trunk.Dark : ring % 2 == 0 ? Trunk.Base : PixelCanvas.Shadow(Trunk.Base, 0.12f));
            }
    }

    // ------------------------------------------------------------------ caves that are built

    /// <summary>
    /// One tile of a built cave's ground in its look (style guide, "Caves that are built"): its floor, its sound floor (what
    /// the original lays in pale tiles) or the top of its rock. Rock's own look is never painted here.
    /// </summary>
    public static void PaintGround(PixelCanvas c, int ox, int oy, int sx, int sy, CaveLook look, TileType type)
    {
        for (int y = 0; y < T; y++)
            for (int x = 0; x < T; x++)
            {
                int gx = sx * T + x, gy = sy * T + y;
                var col = look == CaveLook.Ruins ? RuinsTexel(type, gx, gy) : CharredTexel(type, gx, gy);
                c.SetRaw(ox + x, oy + y, col);
            }
    }

    private static Color RuinsTexel(TileType type, int gx, int gy)
    {
        if (type == TileType.CaveWall)
        {
            // The top of a wall of rough-dressed blocks: dark, its joints darker still
            int row = gy / 12, along = gx + row % 2 * 10, bx = along / 20;
            if (gy % 12 == 11 || along % 20 == 19) return Rgb(66, 56, 46);
            return GroundBaker.Rand01(bx, row, 641) < 0.3f ? Rgb(104, 90, 72) : Rgb(94, 80, 64);
        }
        if (type == TileType.Paving)
        {
            // Dressed blocks of paler stone, a tile wide and half a tile deep, each course half a block along from the
            // last; one block in five is carved with a line round it three texels in from its edge
            int row = gy / 16, along = gx + row % 2 * 16, bx = along / 32, lx = along % 32, ly = gy % 16;
            var stone = GroundBaker.Rand01(bx, row, 643) < 0.3f ? Rgb(196, 176, 136) : Rgb(206, 186, 146);
            if (lx == 31 || ly == 15) return Rgb(140, 118, 88);
            if (lx == 0 || ly == 0) return PixelCanvas.Light1(stone, 0.3f);
            bool carved = GroundBaker.Rand01(bx, row, 645) < 0.2f;
            if (carved && ((lx == 3 || lx == 28) && ly >= 3 && ly <= 12 || (ly == 3 || ly == 12) && lx >= 3 && lx <= 28)) return PixelCanvas.Shadow(stone, 0.16f);
            return stone;
        }
        {
            // Flagstones of worn sandstone, 32 by 16, each course half a stone along from the last, a crack here and there
            int row = gy / 16, along = gx + row % 2 * 16, bx = along / 32, lx = along % 32, ly = gy % 16;
            float pick = GroundBaker.Rand01(bx, row, 647);
            var stone = pick < 0.25f ? Rgb(166, 140, 104) : pick < 0.5f ? Rgb(186, 160, 120) : Rgb(176, 150, 112);
            if (lx == 31 || ly == 15) return Rgb(120, 100, 76);
            if (lx == 0 || ly == 0) return PixelCanvas.Light1(stone, 0.25f);
            bool cracked = GroundBaker.Rand01(bx, row, 649) < 0.06f;
            if (cracked && ly >= 4 && ly <= 11 && lx == 10 + (ly - 4) * 2 / 3) return PixelCanvas.Shadow(stone, 0.3f);
            return stone;
        }
    }

    private static Color CharredTexel(TileType type, int gx, int gy)
    {
        if (type == TileType.CaveWall)
        {
            // A heap of burnt beams: dark timbers a tile long lying across one another, lit along their tops
            int row = gy / 10, along = gx + row * 13 % 32, beam = along / 32;
            int ly = gy % 10;
            var wood = GroundBaker.Rand01(beam, row, 651) < 0.4f ? Rgb(50, 40, 36) : Rgb(42, 34, 32);
            if (ly == 9 || along % 32 == 0) return Rgb(24, 20, 20);
            if (ly == 0) return Rgb(70, 56, 48);
            // An ember now and then, two texels across
            return GroundBaker.Rand01(gx / 2, gy / 2, 653) < 0.004f ? Rgb(150, 66, 36) : wood;
        }
        // Floorboards 8 deep running east and west, the sound ones scorched brown, the rest charred black and grey with ash
        bool sound = type == TileType.Paving;
        int r = gy / 8, a = gx + r * 23 % 48, board = a / 48, j = a % 48;
        if (gy % 8 == 7 || j == 0) return sound ? Rgb(70, 52, 40) : Rgb(28, 22, 22);
        float tone = GroundBaker.Rand01(board, r, sound ? 655 : 657);
        var wood2 = sound
            ? tone < 0.3f ? Rgb(132, 100, 72) : tone < 0.6f ? Rgb(110, 82, 60) : Rgb(122, 92, 66)
            : tone < 0.3f ? Rgb(74, 56, 46) : tone < 0.6f ? Rgb(56, 42, 36) : Rgb(64, 48, 40);
        // Soot darkens a sound board toward its ends; ash lies grey on the charred ones in drifts of a few boards
        if (sound && (j < 4 || j > 43)) return PixelCanvas.Shadow(wood2, 0.3f);
        if (!sound)
        {
            // Ash lies along a board: a drift of grey from a few texels in to its middle, lighter along its top row
            if (GroundBaker.Rand01(board, r, 659) < 0.16f && j >= 6 && j < 30) return gy % 8 == 0 ? Rgb(126, 120, 114) : Rgb(104, 98, 94);
            if (GroundBaker.Rand01(gx / 2, gy / 2, 663) < 0.003f) return Rgb(170, 74, 40);
        }
        return wood2;
    }

    /// <summary>
    /// The face between a built cave's levels, 32 by <see cref="NatureArt.FaceCap"/> + <see cref="NatureArt.FaceBody"/>:
    /// courses of dressed sandstone in the Ruins, twelve rows to a course under a pale coping; charred upright planks under a
    /// burnt beam in the Burned Tower.
    /// </summary>
    public static PixelCanvas Face(CaveLook look)
    {
        var c = new PixelCanvas(32, NatureArt.FaceCap + NatureArt.FaceBody);
        for (int y = 0; y < c.Height; y++)
            for (int x = 0; x < 32; x++)
            {
                Color col;
                if (look == CaveLook.Ruins)
                {
                    if (y < NatureArt.FaceCap)
                        col = y == 0 ? Rgb(232, 214, 176) : y >= NatureArt.FaceCap - 2 ? Rgb(150, 128, 96) : Rgb(214, 194, 154);
                    else
                    {
                        int row = (y - NatureArt.FaceCap) % 48, course = row / 12, ly = row % 12, along = (x + course % 2 * 12) % 24;
                        col = ly == 11 || along == 23 ? Rgb(100, 84, 64) : ly == 0 || along == 0 ? Rgb(196, 172, 132)
                            : ly == 10 || along == 22 ? Rgb(140, 118, 88) : Rgb(170, 146, 110);
                    }
                }
                else
                {
                    if (y < NatureArt.FaceCap)
                        col = y == 0 ? Rgb(84, 64, 52) : y >= NatureArt.FaceCap - 2 ? Rgb(26, 20, 20) : (x * 7 + y) % 13 == 0 ? Rgb(36, 28, 26) : Rgb(54, 42, 36);
                    else
                    {
                        int px = x % 8;
                        float burn = GroundBaker.Rand01(x / 8, (y - NatureArt.FaceCap) / 6, 671);
                        col = px == 7 ? Rgb(24, 20, 20) : px == 0 ? Rgb(76, 58, 48)
                            : burn < 0.2f ? Rgb(40, 32, 30) : burn < 0.3f ? Rgb(96, 90, 86) : Rgb(60, 46, 40);
                    }
                }
                c.SetRaw(x, y, col);
            }
        return c;
    }

    /// <summary>
    /// A carved stele of the Ruins (style guide, "Caves that are built"), the Ruins' own in place of a signpost: an upright
    /// slab of sandstone 22 texels wide and 28 high on a plinth, lit on its west edge, its face cut with three rows of small
    /// glyphs of our own (rings, bars and hooks, never any game's letters) in a shade of the stone.
    /// </summary>
    public static void PaintStele(PixelCanvas c)
    {
        var stone = Tone.Of(186, 160, 120);
        var cut = Rgb(116, 96, 70);
        int w = c.Width, h = c.Height;
        // The plinth, wider than the slab, along the foot
        c.Rect(2, h - 5, w - 4, 5, stone.Dark);
        c.HLine(2, h - 5, w - 4, stone.Base);
        // The slab, its head rounded off by a texel at each corner
        int x0 = 6, x1 = w - 6, y0 = 2, y1 = h - 5;
        c.Rect(x0, y0, x1 - x0, y1 - y0, stone.Base);
        c.VLine(x0, y0 + 1, y1 - y0 - 1, stone.Light);
        c.HLine(x0 + 1, y0, x1 - x0 - 2, stone.Light);
        c.VLine(x1 - 1, y0 + 1, y1 - y0 - 1, stone.Dark);
        c.SetRaw(x0, y0, Color.Blank);
        c.SetRaw(x1 - 1, y0, Color.Blank);
        // Three rows of three glyphs, each four texels square
        for (int row = 0; row < 3; row++)
            for (int col = 0; col < 3; col++)
            {
                int gx = x0 + 2 + col * 6, gy = y0 + 4 + row * 7, kind = (row * 3 + col) % 4;
                switch (kind)
                {
                    case 0: c.HLine(gx, gy, 4, cut); c.HLine(gx, gy + 3, 4, cut); c.VLine(gx, gy, 4, cut); c.VLine(gx + 3, gy, 4, cut); break;
                    case 1: c.VLine(gx + 1, gy, 4, cut); c.HLine(gx, gy + 3, 4, cut); break;
                    case 2: c.HLine(gx, gy, 4, cut); c.VLine(gx + 3, gy, 4, cut); c.HLine(gx + 1, gy + 3, 3, cut); break;
                    default: c.VLine(gx, gy, 4, cut); c.HLine(gx, gy + 1, 4, cut); c.SetRaw(gx + 3, gy + 3, cut); break;
                }
            }
    }
}
