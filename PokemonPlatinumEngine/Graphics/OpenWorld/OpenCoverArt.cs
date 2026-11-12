using System;
using Raylib_cs;
using PokemonPlatinumEngine.OpenWorld;

namespace PokemonPlatinumEngine.Graphics.OpenWorld;

/// <summary>
/// The open world's covers as pixel art (plan 32 · OW8; style guide, "Open world", the covers' tiles): a tile of
/// <see cref="Tile"/> texels square for each <see cref="OpenCover"/>, repeating every <see cref="Metres"/> metres (16
/// texels a metre), each a flat base with clean-edged patches one step lighter and placed marks, as the field's grounds
/// are painted, every one wrapping round its edges so the tiles meet without a seam. The tiles stand one under another
/// in an atlas of <see cref="Slots"/> cells (<see cref="Paint"/>), each cell <see cref="Cell"/> texels square with the
/// tile in its middle and <see cref="Pad"/> texels of the tile itself, come round its edges, all about it: a far mip
/// level reads a little past a tile's edge, and what it reads there is the tile's own. Like beside like all the same
/// (<see cref="SlotOf"/>). GPU-free. Since the ground is shaded from <see cref="OpenGroundArt"/>, the tiles are the record of
/// each cover's colours and marks (and the slots the ground's vertices name), no longer what is drawn.
/// </summary>
internal static class OpenCoverArt
{
    /// <summary>A tile's side in texels.</summary>
    public const int Tile = 32;

    /// <summary>The tile's own texels round it in its cell, each side.</summary>
    public const int Pad = 16;

    /// <summary>A cell's side in the atlas: the tile and its pad.</summary>
    public const int Cell = Tile + 2 * Pad;

    /// <summary>A tile's side in metres: 16 texels a metre.</summary>
    public const float Metres = 2f;

    /// <summary>The atlas's cells: room for the covers to come.</summary>
    public const int Slots = 16;

    /// <summary>The covers in the atlas's order, like beside like: the grasses and the highland, mud, the path and sand, the cave's floor, the rocks, snow.</summary>
    public static readonly OpenCover[] Order =
    {
        OpenCover.Grass, OpenCover.GrassLight, OpenCover.GrassDeep, OpenCover.Highland, OpenCover.Mud, OpenCover.Path,
        OpenCover.Sand, OpenCover.CaveFloor, OpenCover.Rock, OpenCover.RockDark, OpenCover.Snow, OpenCover.ClimbableRock
    };

    private static readonly int[] slots = BuildSlots();

    private static int[] BuildSlots()
    {
        var s = new int[256];
        for (int i = 0; i < s.Length; i++) s[i] = 0;
        for (int i = 0; i < Order.Length; i++) s[(int)Order[i]] = i;
        return s;
    }

    /// <summary>The atlas row a cover's tile stands in.</summary>
    public static int SlotOf(OpenCover cover) => slots[(int)cover];

    private static Color Rgb(int r, int g, int b) => new(r, g, b, 255);

    /// <summary>A cover's base, its patches', and its marks' dark and light (the style guide's table).</summary>
    public static (Color Base, Color Patch, Color Dark, Color Light) ColoursOf(OpenCover cover) => cover switch
    {
        OpenCover.Grass => (Rgb(104, 190, 98), Rgb(120, 200, 102), Rgb(70, 150, 82), Rgb(160, 222, 122)),
        OpenCover.GrassLight => (Rgb(120, 200, 102), Rgb(132, 208, 110), Rgb(88, 168, 90), Rgb(170, 228, 130)),
        OpenCover.GrassDeep => (Rgb(80, 162, 88), Rgb(92, 174, 94), Rgb(58, 126, 82), Rgb(132, 204, 112)),
        OpenCover.Path => (Rgb(222, 204, 160), Rgb(232, 218, 180), Rgb(168, 160, 150), Rgb(208, 202, 192)),
        OpenCover.Rock => (Rgb(150, 140, 128), Rgb(164, 154, 142), Rgb(112, 104, 98), Rgb(196, 188, 176)),
        OpenCover.RockDark => (Rgb(112, 104, 98), Rgb(124, 116, 108), Rgb(84, 78, 76), Rgb(150, 140, 128)),
        OpenCover.CaveFloor => (Rgb(128, 108, 88), Rgb(140, 120, 98), Rgb(96, 80, 66), Rgb(164, 150, 132)),
        OpenCover.Highland => (Rgb(136, 168, 112), Rgb(148, 178, 122), Rgb(104, 138, 86), Rgb(178, 204, 148)),
        OpenCover.Sand => (Rgb(238, 224, 172), Rgb(246, 236, 196), Rgb(214, 196, 140), Rgb(246, 236, 196)),
        OpenCover.Snow => (Rgb(186, 204, 232), Rgb(196, 212, 238), Rgb(168, 188, 224), Rgb(206, 220, 242)),
        OpenCover.Mud => (Rgb(124, 106, 86), Rgb(140, 122, 98), Rgb(98, 112, 108), Rgb(150, 160, 150)),
        OpenCover.ClimbableRock => (Rgb(138, 124, 108), Rgb(150, 136, 118), Rgb(96, 86, 78), Rgb(188, 176, 158)),
        _ => (Rgb(255, 0, 255), Rgb(255, 0, 255), Rgb(255, 0, 255), Rgb(255, 0, 255))
    };

    /// <summary>A whole number's hash, the same on any machine (no floating point).</summary>
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

    private static int Wrap(int v) => ((v % Tile) + Tile) % Tile;

    /// <summary>
    /// Value noise round the tile (period <see cref="Tile"/>), <paramref name="cells"/> cells across, smoothly
    /// interpolated: where it is over a threshold a patch lies, so a patch's edge is clean and comes round the tile.
    /// </summary>
    private static double Noise(int x, int y, int cells, int salt)
    {
        double cell = Tile / (double)cells;
        double fx = x / cell, fy = y / cell;
        int x0 = (int)Math.Floor(fx), y0 = (int)Math.Floor(fy);
        double tx = fx - x0, ty = fy - y0;
        tx = tx * tx * (3 - 2 * tx);
        ty = ty * ty * (3 - 2 * ty);
        double V(int cx, int cy) => Rand(((cx % cells) + cells) % cells, ((cy % cells) + cells) % cells, salt);
        double a = V(x0, y0), b = V(x0 + 1, y0), c = V(x0, y0 + 1), d = V(x0 + 1, y0 + 1);
        return a + (b - a) * tx + (c - a) * ty + (a - b - c + d) * tx * ty;
    }

    /// <summary>
    /// Every cover's tile, one under another in <see cref="Order"/>, each in its cell with its pad (the tile come round
    /// its own edges): <see cref="Cell"/> wide and <see cref="Cell"/> × <see cref="Slots"/> tall.
    /// </summary>
    public static PixelCanvas Paint()
    {
        var atlas = new PixelCanvas(Cell, Cell * Slots);
        for (int i = 0; i < Slots; i++)
        {
            var tile = PaintTile(i < Order.Length ? Order[i] : OpenCover.Grass);
            for (int y = 0; y < Cell; y++)
                for (int x = 0; x < Cell; x++)
                    atlas.SetRaw(x, i * Cell + y, tile.Get(Wrap(x - Pad), Wrap(y - Pad)));
        }
        return atlas;
    }

    /// <summary>One cover's tile.</summary>
    public static PixelCanvas PaintTile(OpenCover cover)
    {
        var (baseColour, patch, dark, light) = ColoursOf(cover);
        var tile = new PixelCanvas(Tile, Tile);
        int salt = 101 + (int)cover * 17;
        for (int y = 0; y < Tile; y++)
            for (int x = 0; x < Tile; x++)
                tile.SetRaw(x, y, Noise(x, y, 4, salt) > 0.62 ? patch : baseColour);

        void Put(int x, int y, Color c) => tile.SetRaw(Wrap(x), Wrap(y), c);

        switch (cover)
        {
            case OpenCover.Grass or OpenCover.GrassLight or OpenCover.GrassDeep or OpenCover.Highland:
                // Tufts: a "v" of three blades on a jittered grid of 8 texels, about one cell in two, its middle blade's tip lit
                for (int cy = 0; cy < 4; cy++)
                    for (int cx = 0; cx < 4; cx++)
                    {
                        if (Rand(cx, cy, salt + 1) > 0.55) continue;
                        int x = cx * 8 + 2 + (int)(Rand(cx, cy, salt + 2) * 4), y = cy * 8 + 4 + (int)(Rand(cx, cy, salt + 3) * 3);
                        Put(x, y, dark); Put(x, y - 1, dark); Put(x, y - 2, light);
                        Put(x - 1, y, dark); Put(x - 1, y - 1, dark);
                        Put(x + 1, y, dark); Put(x + 1, y - 1, dark);
                    }
                break;
            case OpenCover.Path:
                // Pebbles: 2×2 with a light texel and a shadow under, one or two to a tile's quarter
                for (int k = 0; k < 6; k++)
                {
                    int x = (int)(Rand(k, 0, salt) * Tile), y = (int)(Rand(k, 1, salt) * Tile);
                    Put(x, y, dark); Put(x + 1, y, dark); Put(x, y + 1, dark); Put(x + 1, y + 1, dark);
                    Put(x, y, light);
                    Put(x, y + 2, Rgb(200, 178, 128)); Put(x + 1, y + 2, Rgb(200, 178, 128));
                }
                break;
            case OpenCover.Rock or OpenCover.RockDark:
                // Strata: bent lines five to nine texels long, each with a light chip over its start
                for (int k = 0; k < 5; k++)
                {
                    int x = (int)(Rand(k, 0, salt) * Tile), y = (int)(Rand(k, 1, salt) * Tile);
                    int length = 5 + (int)(Rand(k, 2, salt) * 5);
                    Put(x, y - 1, light);
                    for (int s = 0; s < length; s++)
                    {
                        Put(x + s, y, dark);
                        double turn = Rand(k, 10 + s, salt);
                        if (turn < 0.2) y--;
                        else if (turn > 0.8) y++;
                    }
                }
                break;
            case OpenCover.CaveFloor:
                // Cracks three to five texels long, and a few pebbles
                for (int k = 0; k < 5; k++)
                {
                    int x = (int)(Rand(k, 0, salt) * Tile), y = (int)(Rand(k, 1, salt) * Tile);
                    int length = 3 + (int)(Rand(k, 2, salt) * 3);
                    for (int s = 0; s < length; s++)
                    {
                        Put(x, y, dark);
                        if (Rand(k, 10 + s, salt) < 0.5) x++; else y++;
                    }
                }
                for (int k = 0; k < 4; k++)
                {
                    int x = (int)(Rand(k, 5, salt) * Tile), y = (int)(Rand(k, 6, salt) * Tile);
                    Put(x, y, light); Put(x + 1, y, light);
                }
                break;
            case OpenCover.Sand:
                // Ripple arcs: four to six texels across with their ends a texel up
                for (int k = 0; k < 6; k++)
                {
                    int x = (int)(Rand(k, 0, salt) * Tile), y = (int)(Rand(k, 1, salt) * Tile);
                    int length = 4 + (int)(Rand(k, 2, salt) * 3);
                    for (int s = 0; s < length; s++) Put(x + s, s == 0 || s == length - 1 ? y - 1 : y, dark);
                }
                break;
            case OpenCover.Snow:
                // Drift lines, three texels long
                for (int k = 0; k < 7; k++)
                {
                    int x = (int)(Rand(k, 0, salt) * Tile), y = (int)(Rand(k, 1, salt) * Tile);
                    for (int s = 0; s < 3; s++) Put(x + s, y, dark);
                }
                break;
            case OpenCover.Mud:
                // Wet patches: flat ovals five texels wide with a light texel
                for (int k = 0; k < 4; k++)
                {
                    int x = (int)(Rand(k, 0, salt) * Tile), y = (int)(Rand(k, 1, salt) * Tile);
                    for (int s = 0; s < 5; s++) Put(x + s, y, dark);
                    for (int s = 1; s < 4; s++) Put(x + s, y + 1, dark);
                    Put(x + 1, y, light);
                }
                break;
            case OpenCover.ClimbableRock:
                // Ledges: level lines four to seven texels long with a lip over each, and holds of 2×2 with a light texel
                for (int k = 0; k < 5; k++)
                {
                    int x = (int)(Rand(k, 0, salt) * Tile), y = (int)(Rand(k, 1, salt) * Tile);
                    int length = 4 + (int)(Rand(k, 2, salt) * 4);
                    for (int s = 0; s < length; s++) { Put(x + s, y - 1, light); Put(x + s, y, dark); }
                }
                for (int k = 0; k < 4; k++)
                {
                    int x = (int)(Rand(k, 20, salt) * Tile), y = (int)(Rand(k, 21, salt) * Tile);
                    Put(x, y, dark); Put(x + 1, y, dark); Put(x, y + 1, dark); Put(x + 1, y + 1, dark);
                    Put(x, y, light);
                }
                break;
        }
        return tile;
    }
}
