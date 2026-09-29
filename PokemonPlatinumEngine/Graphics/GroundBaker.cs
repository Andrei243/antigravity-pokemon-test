using System;
using System.Collections.Generic;
using Raylib_cs;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// Paints a map's terrain into one pixel-art texture that is laid over the 3D ground. Tiles are painted with
/// knowledge of their neighbours (grass fringes on paths, stone rims around ponds, contact shadows under
/// buildings and trees). Anything that stands up (buildings, trees, tall grass, signs) is modelled in 3D instead.
/// </summary>
internal static class GroundBaker
{
    public const int ArtTile = 16;

    // Platinum's bright, minty field palette
    private static readonly Color GrassBase = new(118, 220, 136, 255);
    private static readonly Color GrassDark = new(84, 188, 110, 255);
    private static readonly Color GrassLight = new(170, 242, 176, 255);
    private static readonly Color ForestFloor = new(62, 150, 96, 255);
    private static readonly Color TallGround = new(40, 132, 74, 255);

    private static readonly Color PathBase = new(242, 220, 152, 255);
    private static readonly Color PathDark = new(220, 190, 122, 255);
    private static readonly Color PathLight = new(252, 240, 198, 255);
    private static readonly Color PathEdge = new(206, 180, 118, 255);

    private static readonly Color PondRim = new(160, 150, 138, 255);
    private static readonly Color Sand = new(240, 226, 176, 255);
    private static readonly Color OutlineDark = new(44, 36, 52, 255);

    public static PixelCanvas BakeGround(Map map, int margin, IReadOnlyList<BuildingInfo> buildings)
    {
        int tilesW = map.Width + margin * 2;
        int tilesH = map.Height + margin * 2;
        var canvas = new PixelCanvas(tilesW * ArtTile, tilesH * ArtTile);
        var ctx = new Ctx(map, canvas, margin);

        for (int ty = -margin; ty < map.Height + margin; ty++)
            for (int tx = -margin; tx < map.Width + margin; tx++)
                ctx.Paint(tx, ty);

        // Contact shadows are painted last so they darken whatever ground is underneath
        if (!map.IsIndoors)
        {
            foreach (var b in buildings) ctx.BuildingShadow(b);
            for (int ty = -margin; ty < map.Height + margin; ty++)
                for (int tx = -margin; tx < map.Width + margin; tx++)
                    if (ctx.TypeAt(tx, ty) is TileType.Tree or TileType.TreeTrunk) ctx.TreeShadow(tx, ty);
        }
        return canvas;
    }

    /// <summary>The interior back wall (rows 0-1, between the side walls) as one strip, 32 art pixels tall.</summary>
    public static PixelCanvas BakeBackWall(Map map)
    {
        int tiles = Math.Max(1, map.Width - 2);
        var canvas = new PixelCanvas(tiles * ArtTile, ArtTile * 2);
        var ctx = new Ctx(map, canvas, 0);
        for (int tx = 1; tx <= tiles; tx++)
        {
            ctx.PaintBackWall((tx - 1) * ArtTile, 0, tx, 0);
            ctx.PaintBackWall((tx - 1) * ArtTile, ArtTile, tx, 1);
        }
        return canvas;
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

    private sealed class Ctx
    {
        private readonly Map map;
        private readonly PixelCanvas c;
        private readonly int margin;

        public Ctx(Map map, PixelCanvas canvas, int margin)
        {
            this.map = map;
            c = canvas;
            this.margin = margin;
        }

        private int OX(int tx) => (tx + margin) * ArtTile;
        private int OY(int ty) => (ty + margin) * ArtTile;

        public TileType? TypeAt(int x, int y) => GroundBaker.TypeAt(map, x, y);

        private Kind KindAt(int x, int y)
        {
            if (MapStructures.IsBuildingTile(map, x, y)) return Kind.Building;
            return TypeAt(x, y) switch
            {
                null => Kind.Void,
                TileType.Path => Kind.Path,
                TileType.Water => Kind.Water,
                TileType.Floor or TileType.PC => map.IsIndoors ? Kind.Floor : Kind.Path,
                TileType.Signpost => SignGround(x, y),
                _ => Kind.Grass
            };
        }

        private Kind SignGround(int x, int y)
        {
            int paths = 0;
            foreach (var (dx, dy) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
            {
                if (TypeAt(x + dx, y + dy) == TileType.Path) paths++;
            }
            return paths >= 2 ? Kind.Path : Kind.Grass;
        }

        public void Paint(int tx, int ty)
        {
            var t = TypeAt(tx, ty);
            if (t == null) return;
            int ox = OX(tx), oy = OY(ty);

            if (map.IsIndoors)
            {
                if (t == TileType.Door) PaintExitMat(ox, oy, tx, ty);
                else PaintFloor(ox, oy, tx, ty);
                return;
            }

            if (MapStructures.IsBuildingTile(map, tx, ty))
            {
                PaintGrass(ox, oy, tx, ty, GrassBase);
                return;
            }

            switch (t.Value)
            {
                case TileType.FlowerGrass: PaintGrass(ox, oy, tx, ty, GrassBase); PaintFlowers(ox, oy, tx, ty); break;
                case TileType.TallGrass: PaintTallGround(ox, oy, tx, ty); break;
                case TileType.Path: PaintPath(ox, oy, tx, ty); break;
                case TileType.Water: c.Rect(ox, oy, ArtTile, ArtTile, new Color(40, 90, 150, 255)); break;
                case TileType.Tree:
                case TileType.TreeTrunk: PaintGrass(ox, oy, tx, ty, ForestFloor); break;
                case TileType.Floor:
                case TileType.PC: PaintShrineFloor(ox, oy, tx); break;
                case TileType.Signpost:
                    if (SignGround(tx, ty) == Kind.Path) PaintPath(ox, oy, tx, ty);
                    else PaintGrass(ox, oy, tx, ty, GrassBase);
                    Blob(ox + 8f, oy + 11f, 6f, 3f, 70);
                    break;
                default: PaintGrass(ox, oy, tx, ty, GrassBase); break;
            }

            if (t != TileType.Water) PaintShore(ox, oy, tx, ty);
        }

        private void PaintGrass(int ox, int oy, int tx, int ty, Color baseColor)
        {
            c.Rect(ox, oy, ArtTile, ArtTile, baseColor);
            var dark = PixelCanvas.Shadow(baseColor, 0.18f);
            var light = PixelCanvas.Light1(baseColor, 0.35f);

            // Platinum's grass has a faint diagonal checker; a few tufts break up the repetition
            for (int y = 0; y < ArtTile; y += 4)
                for (int x = (y / 4 % 2) * 2; x < ArtTile; x += 4)
                    c.Set(ox + x, oy + y, PixelCanvas.Mix(baseColor, light, 0.45f));

            int tufts = 2 + (int)(Rand01(tx, ty, 1) * 3);
            for (int i = 0; i < tufts; i++)
            {
                int px = ox + 1 + (int)(Rand01(tx, ty, 10 + i) * 12);
                int py = oy + 3 + (int)(Rand01(tx, ty, 20 + i) * 11);
                c.Set(px, py, dark);
                c.Set(px + 1, py + 1, dark);
                c.Set(px + 2, py, dark);
                c.Set(px + 1, py - 1, light);
            }
        }

        private void PaintTallGround(int ox, int oy, int tx, int ty)
        {
            c.Rect(ox, oy, ArtTile, ArtTile, TallGround);
            for (int i = 0; i < 10; i++)
            {
                c.Set(ox + (int)(Rand01(tx, ty, 30 + i) * 16), oy + (int)(Rand01(tx, ty, 40 + i) * 16), PixelCanvas.Shadow(TallGround, 0.25f));
            }
        }

        private void PaintFlowers(int ox, int oy, int tx, int ty)
        {
            // Mostly white clusters like Platinum's flower beds, with the odd red or yellow bloom
            var white = new Color(252, 252, 252, 255);
            var shade = new Color(196, 214, 236, 255);
            for (int i = 0; i < 6; i++)
            {
                int fx = ox + 2 + (int)(Rand01(tx, ty, 60 + i) * 12);
                int fy = oy + 2 + (int)(Rand01(tx, ty, 70 + i) * 12);
                float r = Rand01(tx, ty, 80 + i);
                var petal = r < 0.12f ? new Color(236, 76, 88, 255) : r < 0.2f ? new Color(250, 214, 72, 255) : white;
                c.Set(fx, fy - 1, petal);
                c.Set(fx - 1, fy, petal);
                c.Set(fx + 1, fy, petal);
                c.Set(fx, fy + 1, petal == white ? shade : PixelCanvas.Shadow(petal, 0.3f));
                c.Set(fx, fy, new Color(250, 200, 72, 255));
            }
        }

        private void PaintPath(int ox, int oy, int tx, int ty)
        {
            c.Rect(ox, oy, ArtTile, ArtTile, PathBase);
            for (int i = 0; i < 5; i++)
            {
                int px = ox + 1 + (int)(Rand01(tx, ty, 90 + i) * 13);
                int py = oy + 1 + (int)(Rand01(tx, ty, 95 + i) * 13);
                c.Set(px, py, PathDark);
                c.Set(px + 1, py, PathDark);
                c.Set(px, py - 1, PathLight);
            }

            bool up = KindAt(tx, ty - 1) == Kind.Grass;
            bool down = KindAt(tx, ty + 1) == Kind.Grass;
            bool left = KindAt(tx - 1, ty) == Kind.Grass;
            bool right = KindAt(tx + 1, ty) == Kind.Grass;

            // Ragged grass overhang, then a light sandy border like Platinum's paths
            for (int i = 0; i < ArtTile; i++)
            {
                int d = 1 + (int)(Rand01(tx * 16 + i, ty, 100) * 2);
                if (up) { c.VLine(ox + i, oy, d, GrassBase); c.Set(ox + i, oy + d, PathEdge); c.Set(ox + i, oy + d + 1, PathLight); }
                if (down) { c.VLine(ox + i, oy + ArtTile - d, d, GrassBase); c.Set(ox + i, oy + ArtTile - d - 1, PathEdge); c.Set(ox + i, oy + ArtTile - d - 2, PathLight); }
                if (left) { c.HLine(ox, oy + i, d, GrassBase); c.Set(ox + d, oy + i, PathEdge); c.Set(ox + d + 1, oy + i, PathLight); }
                if (right) { c.HLine(ox + ArtTile - d, oy + i, d, GrassBase); c.Set(ox + ArtTile - d - 1, oy + i, PathEdge); c.Set(ox + ArtTile - d - 2, oy + i, PathLight); }
            }

            if (!up && !left && KindAt(tx - 1, ty - 1) == Kind.Grass) c.Rect(ox, oy, 2, 2, GrassBase);
            if (!up && !right && KindAt(tx + 1, ty - 1) == Kind.Grass) c.Rect(ox + ArtTile - 2, oy, 2, 2, GrassBase);
            if (!down && !left && KindAt(tx - 1, ty + 1) == Kind.Grass) c.Rect(ox, oy + ArtTile - 2, 2, 2, GrassBase);
            if (!down && !right && KindAt(tx + 1, ty + 1) == Kind.Grass) c.Rect(ox + ArtTile - 2, oy + ArtTile - 2, 2, 2, GrassBase);
        }

        /// <summary>Stone rim on land that borders water (sand along the sea outside the map).</summary>
        private void PaintShore(int ox, int oy, int tx, int ty)
        {
            bool inMap = map.InBounds(tx, ty);
            var rim = inMap ? PondRim : Sand;
            var rimLight = PixelCanvas.Light1(rim, 0.35f);
            if (KindAt(tx, ty - 1) == Kind.Water) { c.Rect(ox, oy, ArtTile, 3, rim); c.HLine(ox, oy + 2, ArtTile, rimLight); }
            if (KindAt(tx, ty + 1) == Kind.Water) { c.Rect(ox, oy + ArtTile - 3, ArtTile, 3, rim); c.HLine(ox, oy + ArtTile - 3, ArtTile, rimLight); }
            if (KindAt(tx - 1, ty) == Kind.Water) { c.Rect(ox, oy, 3, ArtTile, rim); c.VLine(ox + 2, oy, ArtTile, rimLight); }
            if (KindAt(tx + 1, ty) == Kind.Water) { c.Rect(ox + ArtTile - 3, oy, 3, ArtTile, rim); c.VLine(ox + ArtTile - 3, oy, ArtTile, rimLight); }
        }

        private void PaintShrineFloor(int ox, int oy, int tx)
        {
            var stone = new Color(176, 168, 196, 255);
            c.Rect(ox, oy, ArtTile, ArtTile, stone);
            c.HLine(ox, oy + 7, ArtTile, PixelCanvas.Shadow(stone, 0.3f));
            c.VLine(ox + (tx % 2 == 0 ? 7 : 11), oy, 7, PixelCanvas.Shadow(stone, 0.3f));
            c.VLine(ox + 4, oy + 8, 8, PixelCanvas.Shadow(stone, 0.3f));
            c.Set(ox + 2, oy + 2, PixelCanvas.Light1(stone, 0.4f));
        }

        // ------------------------------------------------------------ shadows

        /// <summary>Soft shadow: the sun is up and to the left, so buildings shade the ground to their right.</summary>
        public void BuildingShadow(BuildingInfo b)
        {
            float x0 = OX(b.X1 + 1), x1 = x0 + ArtTile * 0.7f;
            float y0 = OY(b.Y0 + 1), y1 = OY(b.Y1 + 1) - ArtTile * 0.2f;
            for (int y = (int)y0; y < (int)y1; y++)
                for (int x = (int)x0; x < (int)x1; x++)
                {
                    float fade = 1f - (x - x0) / (x1 - x0);
                    float fadeIn = Math.Min(1f, (y - y0) / (ArtTile * 0.8f));
                    c.Set(x, y, new Color(20, 40, 30, (int)(55 * fade * fade * fadeIn)));
                }

            // Ambient occlusion along the front wall
            int front = OY(b.Y1 + 1);
            for (int x = OX(b.X0); x < OX(b.X1 + 1); x++)
            {
                c.Set(x, front, new Color(20, 40, 30, 90));
                c.Set(x, front + 1, new Color(20, 40, 30, 45));
            }
        }

        public void TreeShadow(int tx, int ty) => Blob(OX(tx) + 10f, OY(ty) + 8f, 9f, 7f, 60);

        private void Blob(float cx, float cy, float rx, float ry, int alpha)
        {
            for (int y = (int)(cy - ry); y <= (int)(cy + ry); y++)
                for (int x = (int)(cx - rx); x <= (int)(cx + rx); x++)
                {
                    float u = (x + 0.5f - cx) / rx, v = (y + 0.5f - cy) / ry;
                    float d = u * u + v * v;
                    if (d < 1f) c.Set(x, y, new Color(16, 36, 28, (int)(alpha * Math.Min(1f, (1f - d) * 2f))));
                }
        }

        // ------------------------------------------------------------ interiors

        private (Color Paper, Color Band, Color FloorA, Color FloorB) InteriorColors() => map.Interior switch
        {
            InteriorStyle.PokemonCenter => (new(250, 240, 240, 255), new(232, 104, 112, 255), new(250, 242, 232, 255), new(238, 222, 214, 255)),
            InteriorStyle.PokeMart => (new(236, 242, 250, 255), new(80, 132, 220, 255), new(230, 236, 244, 255), new(214, 224, 236, 255)),
            InteriorStyle.Lab => (new(238, 238, 244, 255), new(128, 140, 168, 255), new(234, 234, 240, 255), new(218, 218, 228, 255)),
            _ => (new(246, 230, 196, 255), new(176, 124, 84, 255), new(214, 166, 116, 255), new(196, 146, 100, 255))
        };

        private void PaintFloor(int ox, int oy, int tx, int ty)
        {
            var (_, _, a, b) = InteriorColors();
            if (map.Interior == InteriorStyle.House)
            {
                c.Rect(ox, oy, ArtTile, ArtTile, a);
                for (int row = 0; row < 4; row++)
                {
                    int y = oy + row * 4;
                    c.HLine(ox, y + 3, ArtTile, b);
                    c.HLine(ox, y, ArtTile, PixelCanvas.Light1(a, 0.12f));
                    int joint = ((tx * 5 + row * 7 + ty * 3) % 12) + 2;
                    c.VLine(ox + joint, y, 3, b);
                }
            }
            else
            {
                for (int y = 0; y < ArtTile; y++)
                    for (int x = 0; x < ArtTile; x++)
                    {
                        bool checker = ((x / 8) + (y / 8) + tx + ty) % 2 == 0;
                        var col = checker ? a : b;
                        if (x % 8 == 7 || y % 8 == 7) col = PixelCanvas.Shadow(b, 0.12f);
                        else if (x % 8 == 0 || y % 8 == 0) col = PixelCanvas.Light1(col, 0.3f);
                        c.Set(ox + x, oy + y, col);
                    }
            }

            // Soft shadow cast by the back wall onto the first floor row
            if (ty == 2)
            {
                c.HLine(ox, oy, ArtTile, new Color(0, 0, 0, 70));
                c.HLine(ox, oy + 1, ArtTile, new Color(0, 0, 0, 35));
            }
        }

        public void PaintBackWall(int ox, int oy, int tx, int ty)
        {
            var (paper, band, _, _) = InteriorColors();
            c.Rect(ox, oy, ArtTile, ArtTile, paper);
            for (int x = 1; x < ArtTile; x += 4) c.VLine(ox + x, oy, ArtTile, PixelCanvas.Shadow(paper, 0.06f));

            if (ty == 0)
            {
                c.Rect(ox, oy, ArtTile, 3, new Color(64, 58, 84, 255));
                c.HLine(ox, oy + 3, ArtTile, PixelCanvas.Shadow(band, 0.2f));
                c.HLine(ox, oy + 4, ArtTile, band);

                if ((map.Interior == InteriorStyle.House || map.Interior == InteriorStyle.Lab) && tx % 4 == 3)
                {
                    var frame = new Color(150, 110, 76, 255);
                    c.Rect(ox + 2, oy + 7, 12, 9, frame);
                    c.Rect(ox + 3, oy + 8, 10, 8, new Color(160, 214, 248, 255));
                    c.VLine(ox + 8, oy + 8, 8, frame);
                    c.Set(ox + 4, oy + 9, Color.White);
                    c.Set(ox + 5, oy + 9, Color.White);
                    c.Set(ox + 4, oy + 10, Color.White);
                }
            }
            else
            {
                c.Rect(ox, oy + 10, ArtTile, 3, band);
                c.HLine(ox, oy + 10, ArtTile, PixelCanvas.Light1(band, 0.25f));
                c.Rect(ox, oy + 13, ArtTile, 3, new Color(120, 84, 60, 255));
                c.HLine(ox, oy + 15, ArtTile, OutlineDark);
            }
        }

        private void PaintExitMat(int ox, int oy, int tx, int ty)
        {
            PaintFloor(ox, oy, tx, ty);
            var mat = map.Interior == InteriorStyle.PokeMart ? new Color(80, 132, 220, 255) : new Color(212, 76, 76, 255);
            c.Rect(ox + 1, oy + 2, 14, 12, PixelCanvas.Shadow(mat, 0.35f));
            c.Rect(ox + 2, oy + 3, 12, 10, mat);
            c.Rect(ox + 4, oy + 5, 8, 6, PixelCanvas.Light1(mat, 0.25f));
            c.HLine(ox + 6, oy + 6, 4, Color.White);
            c.HLine(ox + 6, oy + 7, 4, Color.White);
            c.HLine(ox + 5, oy + 8, 6, Color.White);
            c.HLine(ox + 6, oy + 9, 4, Color.White);
            c.HLine(ox + 7, oy + 10, 2, Color.White);
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
