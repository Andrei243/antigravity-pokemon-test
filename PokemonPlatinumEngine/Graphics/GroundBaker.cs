using System;
using Raylib_cs;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// Pixel art for rooms at the field's 32 texels per tile: the floor (planks or tiles, with contact shading along
/// the walls and daylight from the windows) and the wall strip. Outdoor ground is <see cref="PixelGround"/>. Also
/// the shared tile lookup and hash noise for the field builders.
/// </summary>
internal static class GroundBaker
{
    public const int ArtTile = 32;

    /// <summary>Paints a room's floor into one texture laid over the 3D floor.</summary>
    public static PixelCanvas BakeInterior(Map map)
    {
        var canvas = new PixelCanvas(map.Width * ArtTile, map.Height * ArtTile);
        var room = new Room(map, canvas);
        for (int ty = 0; ty < map.Height; ty++)
            for (int tx = 0; tx < map.Width; tx++)
                room.Paint(tx, ty);
        room.Occlusion();
        return canvas;
    }

    /// <summary>
    /// One tile-wide strip of interior wall, three rows tall: crown moulding, wallpaper, chair rail,
    /// wainscot and skirting board. It tiles horizontally along every wall of the room.
    /// </summary>
    public static PixelCanvas BakeWallStrip(InteriorStyle style)
    {
        const int w = ArtTile, h = ArtTile * 3;
        var c = new PixelCanvas(w, h);
        var (paper, pattern, wainscot, trim) = style switch
        {
            InteriorStyle.PokemonCenter => (new Color(250, 242, 240, 255), new Color(242, 214, 216, 255), new Color(232, 108, 116, 255), new Color(248, 248, 250, 255)),
            InteriorStyle.PokeMart => (new Color(234, 242, 250, 255), new Color(214, 226, 242, 255), new Color(76, 128, 216, 255), new Color(248, 248, 250, 255)),
            InteriorStyle.Lab => (new Color(238, 240, 244, 255), new Color(224, 228, 236, 255), new Color(150, 160, 186, 255), new Color(250, 250, 252, 255)),
            _ => (new Color(246, 232, 200, 255), new Color(232, 212, 172, 255), new Color(170, 118, 78, 255), new Color(250, 244, 226, 255))
        };

        int wainscotTop = 62, railY = 58;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                Color col;
                if (y < 5) col = y < 3 ? new Color(70, 64, 88, 255) : trim;
                else if (y < railY)
                {
                    // Wallpaper: soft vertical stripes with a small repeating motif
                    col = (x / 4) % 2 == 0 ? paper : PixelCanvas.Mix(paper, pattern, 0.45f);
                    int mx = (x + (y / 12 % 2) * 8) % 16, my = y % 12;
                    if ((mx == 7 || mx == 8) && (my == 5 || my == 6)) col = pattern;
                }
                else if (y < wainscotTop) col = y == railY ? PixelCanvas.Light1(trim, 0.2f) : trim;
                else if (y < h - 6)
                {
                    col = wainscot;
                    if (x % 16 == 0) col = PixelCanvas.Shadow(wainscot, 0.25f);
                    else if (x % 16 == 1) col = PixelCanvas.Light1(wainscot, 0.25f);
                }
                else col = y < h - 5 ? PixelCanvas.Light1(new Color(110, 78, 56, 255), 0.3f) : new Color(110, 78, 56, 255);
                c.Set(x, y, col);
            }
        return c;
    }

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

    private enum Kind { Void, Grass, Path, Water, Building, Floor }

    private sealed class Room
    {
        private readonly Map map;
        private readonly PixelCanvas c;

        public Room(Map map, PixelCanvas canvas)
        {
            this.map = map;
            c = canvas;
        }

        private static int OX(int tx) => tx * ArtTile;
        private static int OY(int ty) => ty * ArtTile;

        public void Paint(int tx, int ty)
        {
            var t = map.GetGroundTile(tx, ty);
            PaintFloor(OX(tx), OY(ty), tx, ty);
            if (t == TileType.Door) PaintExitMat(OX(tx), OY(ty));
        }

        /// <summary>Darkens the floor along the walls of a room.</summary>
        public void Occlusion()
        {
            int left = OX(1), right = OX(map.Width - 1), top = OY(2), bottom = OY(map.Height - 1);
            for (int d = 0; d < 10; d++)
            {
                var shade = new Color(20, 12, 10, (int)(80 * (1f - d / 10f)));
                c.HLine(left, top + d, right - left, shade);
                c.VLine(left + d, top, bottom - top, shade);
                c.VLine(right - 1 - d, top, bottom - top, shade);
            }

            // Daylight falling through the back-wall windows onto the floor
            foreach (var p in map.Props)
            {
                if (p.Type != PropType.Window) continue;
                for (int y = 0; y < ArtTile * 2; y++)
                {
                    int skew = y / 3;
                    var glow = new Color(255, 250, 220, (int)(46 * (1f - y / (ArtTile * 2f))));
                    c.HLine(OX(p.X) + 4 - skew, top + y, p.Width * ArtTile - 8, glow);
                }
            }
        }

        private void PaintFloor(int ox, int oy, int tx, int ty)
        {
            switch (map.Interior)
            {
                case InteriorStyle.House:
                    PaintPlanks(ox, oy, tx, ty);
                    break;
                case InteriorStyle.PokemonCenter:
                    PaintTiles(ox, oy, tx, ty, new Color(250, 242, 232, 255), new Color(240, 214, 212, 255));
                    break;
                case InteriorStyle.PokeMart:
                    PaintTiles(ox, oy, tx, ty, new Color(230, 238, 246, 255), new Color(206, 220, 236, 255));
                    break;
                default:
                    PaintTiles(ox, oy, tx, ty, new Color(240, 242, 246, 255), new Color(222, 226, 234, 255));
                    break;
            }
        }

        private void PaintPlanks(int ox, int oy, int tx, int ty)
        {
            var wood = new Color(210, 160, 108, 255);
            for (int row = 0; row < 4; row++)
            {
                int y0 = oy + row * 8;
                int plankRow = ty * 4 + row;
                int joint = (int)(Hash(plankRow, 0, 600) % 48);
                for (int y = 0; y < 8; y++)
                    for (int x = 0; x < ArtTile; x++)
                    {
                        int gx = tx * ArtTile + x;
                        int plank = (gx + joint) / 48;
                        float tone = 0.9f + GroundBaker.Rand01(plank, plankRow, 601) * 0.16f;
                        float grain = MathF.Sin((gx + joint) * 0.35f + y * 1.7f + plank) * 0.035f;
                        var col = MeshBuilder.Scale(wood, tone + grain);
                        if (y == 7) col = MeshBuilder.Scale(wood, 0.62f);
                        else if (y == 0) col = MeshBuilder.Scale(wood, tone * 1.08f);
                        if ((gx + joint) % 48 == 0) col = MeshBuilder.Scale(wood, 0.66f);
                        c.Set(ox + x, y0 + y, col);
                    }
            }
        }

        private void PaintTiles(int ox, int oy, int tx, int ty, Color a, Color b)
        {
            for (int y = 0; y < ArtTile; y++)
                for (int x = 0; x < ArtTile; x++)
                {
                    int cellX = (tx * ArtTile + x) / 16, cellY = (ty * ArtTile + y) / 16;
                    var col = (cellX + cellY) % 2 == 0 ? a : b;
                    int lx = x % 16, ly = y % 16;
                    if (lx == 15 || ly == 15) col = PixelCanvas.Shadow(b, 0.15f);
                    else if (lx == 0 || ly == 0) col = PixelCanvas.Light1(col, 0.35f);
                    else if (lx + ly < 7) col = PixelCanvas.Light1(col, 0.12f);
                    c.Set(ox + x, oy + y, col);
                }
        }

        private void PaintExitMat(int ox, int oy)
        {
            var mat = map.Interior == InteriorStyle.PokeMart ? new Color(80, 132, 220, 255) : new Color(212, 76, 76, 255);
            c.Rect(ox + 2, oy + 3, 28, 24, PixelCanvas.Shadow(mat, 0.4f));
            c.Rect(ox + 3, oy + 4, 26, 22, mat);
            c.Rect(ox + 6, oy + 7, 20, 16, PixelCanvas.Light1(mat, 0.2f));
            // Arrow pointing out of the room
            for (int i = 0; i < 5; i++) c.HLine(ox + 16 - 5 + i, oy + 15 + i, 10 - i * 2, Color.White);
            c.Rect(ox + 14, oy + 9, 4, 6, Color.White);
        }
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
