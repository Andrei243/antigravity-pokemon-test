using System;
using Raylib_cs;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// Pixel art for rooms at the field's 32 texels per tile (style guide, "Rooms"): the floor (planks or tiles, with
/// the walls' contact shade in two flat steps) and the walls (crown moulding, wallpaper, chair rail, panelled
/// wainscot, skirting). Outdoor ground is <see cref="PixelGround"/>. Also the shared tile lookup and hash noise
/// for the field builders. No GPU calls.
/// </summary>
internal static class GroundBaker
{
    public const int ArtTile = 32;

    private static Color Rgb(int r, int g, int b) => new(r, g, b, 255);

    // ------------------------------------------------------------------ floors

    private static readonly Color Plank = Rgb(210, 160, 108);
    private static readonly Color PlankLight = Rgb(220, 172, 120);
    private static readonly Color PlankDark = Rgb(198, 148, 98);
    private static readonly Color PlankGroove = Rgb(156, 112, 76);

    /// <summary>Paints a room's floor into one texture laid over the 3D floor.</summary>
    public static PixelCanvas BakeInterior(Map map)
    {
        var c = new PixelCanvas(map.Width * ArtTile, map.Height * ArtTile);
        for (int y = 0; y < c.Height; y++)
            for (int x = 0; x < c.Width; x++)
                c.SetRaw(x, y, FloorTexel(map.Interior, x, y));

        // The walls shade the floor beside them: two flat steps
        int left = ArtTile, right = (map.Width - 1) * ArtTile, top = 2 * ArtTile, bottom = (map.Height - 1) * ArtTile;
        foreach (var (from, depth, amount) in new[] { (0, 6, 0.22f), (6, 6, 0.11f) })
        {
            Pix.Shade(c, left, top + from, right - left, depth, amount);
            Pix.Shade(c, left + from, top + 12, depth, bottom - top - 12, amount);
            Pix.Shade(c, right - from - depth, top + 12, depth, bottom - top - 12, amount);
        }

        for (int tx = 0; tx < map.Width; tx++)
            if (map.GetGroundTile(tx, map.Height - 1) == TileType.Door) ExitMat(c, tx * ArtTile, (map.Height - 1) * ArtTile, map.Interior);
        return c;
    }

    private static Color FloorTexel(InteriorStyle style, int x, int y)
    {
        if (style == InteriorStyle.House)
        {
            // Planks 8 wide running east-west, their joints staggered row by row; whole planks vary in tone
            int row = y / 8, along = x + row * 23 % 56, plank = along / 56, j = along % 56;
            if (y % 8 == 7 || j == 0) return PlankGroove;
            uint pick = Hash(plank, row, 601) % 5;
            var tone = pick == 0 ? PlankLight : pick == 1 ? PlankDark : Plank;
            // A short line of grain on about one plank in three
            int grain = 12 + (int)(Hash(plank, row, 602) % 24);
            if (Hash(plank, row, 603) % 3 == 0 && y % 8 == 3 && j >= grain && j < grain + 7) return PixelCanvas.Shadow(tone, 0.1f);
            return tone;
        }

        var (a, b) = style switch
        {
            InteriorStyle.PokemonCenter => (Rgb(250, 242, 232), Rgb(240, 214, 212)),
            InteriorStyle.PokeMart => (Rgb(230, 238, 246), Rgb(206, 220, 236)),
            _ => (Rgb(240, 242, 246), Rgb(222, 226, 234))
        };
        // Tiles 16 square in two close tones: a grout line on the right and at the foot, a light edge on the left and top
        var tile = (x / 16 + y / 16) % 2 == 0 ? a : b;
        int lx = x % 16, ly = y % 16;
        return lx == 15 || ly == 15 ? PixelCanvas.Shadow(b, 0.15f) : lx == 0 || ly == 0 ? PixelCanvas.Light1(tile, 0.35f) : tile;
    }

    /// <summary>The mat at a room's door, with an arrow pointing out.</summary>
    private static void ExitMat(PixelCanvas c, int ox, int oy, InteriorStyle style)
    {
        var mat = Tone.Of(style == InteriorStyle.PokeMart ? Rgb(80, 132, 220) : Rgb(212, 76, 76));
        c.Rect(ox + 2, oy + 3, 28, 24, mat.Deep);
        Pix.Raised(c, ox + 3, oy + 4, 26, 22, mat);
        c.Rect(ox + 14, oy + 8, 4, 7, Color.White);
        for (int i = 0; i < 5; i++) c.HLine(ox + 11 + i, oy + 15 + i, 10 - i * 2, Color.White);
    }

    // ------------------------------------------------------------------ walls

    /// <summary>
    /// Paints one wall of a room, the full height of the canvas (80 texels) and as wide as the wall: crown
    /// moulding, wallpaper with a small motif, a chair rail, panelled wainscot and a skirting board.
    /// </summary>
    public static void PaintWall(PixelCanvas c, InteriorStyle style)
    {
        var (paper, motif, wainscot, trim, skirting) = style switch
        {
            InteriorStyle.PokemonCenter => (Rgb(250, 242, 240), Rgb(242, 214, 216), Tone.Of(232, 108, 116), Tone.Of(248, 248, 250), Tone.Of(150, 70, 84)),
            InteriorStyle.PokeMart => (Rgb(234, 242, 250), Rgb(214, 226, 242), Tone.Of(76, 128, 216), Tone.Of(248, 248, 250), Tone.Of(52, 84, 150)),
            InteriorStyle.Lab => (Rgb(238, 240, 244), Rgb(224, 228, 236), Tone.Of(150, 160, 186), Tone.Of(250, 250, 252), Tone.Of(96, 104, 128)),
            _ => (Rgb(246, 232, 200), Rgb(232, 212, 172), Tone.Of(178, 124, 82), Tone.Of(250, 244, 226), Tone.Of(120, 84, 58))
        };

        const int crown = 6, rail = 48, panels = 52, skirt = 74;
        int w = c.Width, h = c.Height;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                Color col;
                if (y < crown) col = y == 0 ? trim.Light : y == crown - 1 ? trim.Dark : trim.Base;
                else if (y < rail)
                {
                    // Wallpaper: a four-texel motif on a grid 16 by 14, every other row of them half a step along
                    int row = (y - crown) / 14, my = (y - crown) % 14, mx = (x + row % 2 * 8) % 16;
                    col = (mx is 7 or 8) && (my is 6 or 7) ? motif : y == crown ? PixelCanvas.Shadow(paper, 0.14f) : paper;
                }
                else if (y < panels) col = y == rail ? trim.Light : y == panels - 1 ? trim.Dark : trim.Base;
                else if (y < skirt)
                {
                    // Wainscot: a sunken panel in every 16 texels, between a joint and its light edge
                    int px = x % 16, py = y - panels;
                    col = px == 15 ? wainscot.Dark : px == 0 ? wainscot.Light
                        : py == 0 ? PixelCanvas.Shadow(wainscot.Base, 0.18f)
                        : px >= 3 && px <= 12 && py >= 3 && py <= 18
                            ? (px == 3 || py == 3 ? wainscot.Dark : px == 12 || py == 18 ? wainscot.Light : wainscot.Base)
                            : wainscot.Base;
                }
                else col = y == skirt ? skirting.Light : y == h - 1 ? skirting.Dark : skirting.Base;
                c.SetRaw(x, y, col);
            }
    }

    /// <summary>The dark top of a cut-away wall, with a paler line along its inner edge.</summary>
    public static void PaintWallTop(PixelCanvas c)
    {
        var cap = Tone.Of(62, 56, 74);
        c.Rect(0, 0, c.Width, c.Height, cap.Base);
        Pix.Border(c, 0, 0, c.Width, c.Height, cap.Light);
    }

    // ------------------------------------------------------------------ shared lookups

    /// <summary>Tile type used for scenery outside the map: roads and water continue, everything else is forest.</summary>
    public static TileType? TypeAt(Map map, int x, int y)
    {
        if (map.InBounds(x, y)) return map.GetGroundTile(x, y);
        if (map.IsIndoors) return null;

        var edge = map.GetGroundTile(Math.Clamp(x, 0, map.Width - 1), Math.Clamp(y, 0, map.Height - 1));
        return edge switch
        {
            TileType.Path => TileType.Path,
            TileType.Water => TileType.Water,
            _ => TileType.Tree
        };
    }

    /// <summary>
    /// Whether water lies at a tile: open water, or the water a bridge's deck crosses, which runs on under the
    /// boards (the deck is drawn over it, and the river's shore is not broken by the bridge).
    /// </summary>
    public static bool IsWaterAt(Map map, int x, int y)
    {
        var type = TypeAt(map, x, y);
        return type == TileType.Water || (type is TileType.Planks or TileType.Walkway && map.InBounds(x, y) && map.IsDeepWater(x, y));
    }

    private static uint Hash(int x, int y, int salt)
    {
        unchecked
        {
            uint h = (uint)(x * 374761393 + y * 668265263 + salt * 1442695041);
            h = (h ^ (h >> 13)) * 1274126177u;
            return h ^ (h >> 16);
        }
    }

    public static float Rand01(int x, int y, int salt) => (Hash(x, y, salt) & 0xFFFF) / 65536f;
}
