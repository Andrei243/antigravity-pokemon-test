using System;
using System.Collections.Generic;
using Raylib_cs;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// Paints a map's ground into one texture (32 texels per tile) that is laid over the 3D terrain. Tiles know
/// their neighbours (grass overhanging paths, stone rims round ponds, contact shading at walls and trees).
/// Anything that stands up (buildings, trees, tall grass, furniture) is modelled in 3D instead, and real
/// shadows come from the shadow map.
/// </summary>
internal static class GroundBaker
{
    public const int ArtTile = 32;

    // Platinum's bright, minty field palette
    private static readonly Color GrassBase = new(116, 216, 132, 255);
    private static readonly Color GrassDark = new(78, 178, 100, 255);
    private static readonly Color GrassLight = new(172, 240, 170, 255);
    private static readonly Color ForestFloor = new(62, 146, 94, 255);
    private static readonly Color TallGround = new(36, 124, 68, 255);

    private static readonly Color PathBase = new(240, 218, 150, 255);
    private static readonly Color PathDark = new(210, 180, 116, 255);
    private static readonly Color PathLight = new(252, 240, 198, 255);
    private static readonly Color PathEdge = new(200, 170, 110, 255);

    private static readonly Color PondRim = new(156, 148, 138, 255);
    private static readonly Color Sand = new(238, 224, 172, 255);

    public static PixelCanvas BakeGround(Map map, int margin, IReadOnlyList<BuildingInfo> buildings)
    {
        int tilesW = map.Width + margin * 2;
        int tilesH = map.Height + margin * 2;
        var canvas = new PixelCanvas(tilesW * ArtTile, tilesH * ArtTile);
        var ctx = new Ctx(map, canvas, margin);

        for (int ty = -margin; ty < map.Height + margin; ty++)
            for (int tx = -margin; tx < map.Width + margin; tx++)
                ctx.Paint(tx, ty);

        if (map.IsIndoors)
        {
            ctx.InteriorOcclusion();
        }
        else
        {
            foreach (var b in buildings) ctx.BuildingOcclusion(b);
            for (int ty = -margin; ty < map.Height + margin; ty++)
                for (int tx = -margin; tx < map.Width + margin; tx++)
                    if (TypeAt(map, tx, ty) is TileType.Tree or TileType.TreeTrunk) ctx.TreeOcclusion(tx, ty);
        }
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

        private TileType? TypeAt(int x, int y) => GroundBaker.TypeAt(map, x, y);

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
                PaintFloor(ox, oy, tx, ty);
                if (t == TileType.Door) PaintExitMat(ox, oy);
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
                case TileType.TreeTrunk: PaintForestFloor(ox, oy, tx, ty); break;
                case TileType.Floor:
                case TileType.PC: PaintShrineFloor(ox, oy, tx, ty); break;
                case TileType.Signpost:
                    if (SignGround(tx, ty) == Kind.Path) PaintPath(ox, oy, tx, ty);
                    else PaintGrass(ox, oy, tx, ty, GrassBase);
                    break;
                default: PaintGrass(ox, oy, tx, ty, GrassBase); break;
            }

            if (t != TileType.Water) PaintShore(ox, oy, tx, ty);
        }

        // ------------------------------------------------------------ outdoor ground

        private void PaintGrass(int ox, int oy, int tx, int ty, Color baseColor)
        {
            var dark = PixelCanvas.Shadow(baseColor, 0.2f);
            var light = PixelCanvas.Light1(baseColor, 0.4f);

            // Broad light/dark patches that flow across tiles, plus fine grain
            for (int y = 0; y < ArtTile; y++)
                for (int x = 0; x < ArtTile; x++)
                {
                    int gx = ox + x, gy = oy + y;
                    float n = ValueNoise(gx / 40f, gy / 40f) * 0.7f + ValueNoise(gx / 11f, gy / 11f) * 0.3f;
                    var col = n < 0.42f ? PixelCanvas.Mix(baseColor, dark, (0.42f - n) * 1.2f) : PixelCanvas.Mix(baseColor, light, (n - 0.42f) * 0.7f);
                    if ((Hash(gx, gy, 2) & 31) == 0) col = PixelCanvas.Mix(col, light, 0.5f);
                    c.Set(gx, gy, col);
                }

            // Individual blades
            int blades = 16 + (int)(Rand01(tx, ty, 1) * 8);
            for (int i = 0; i < blades; i++)
            {
                int bx = ox + (int)(Rand01(tx, ty, 10 + i) * ArtTile);
                int by = oy + 4 + (int)(Rand01(tx, ty, 40 + i) * (ArtTile - 4));
                int h = 3 + (int)(Rand01(tx, ty, 70 + i) * 3);
                for (int k = 0; k < h; k++)
                {
                    c.Set(bx + (k == h - 1 && i % 2 == 0 ? 1 : 0), by - k, k == h - 1 ? light : k == 0 ? dark : PixelCanvas.Mix(dark, baseColor, 0.6f));
                }
            }
        }

        private void PaintForestFloor(int ox, int oy, int tx, int ty)
        {
            PaintGrass(ox, oy, tx, ty, ForestFloor);
            for (int i = 0; i < 10; i++)
            {
                int x = ox + (int)(Rand01(tx, ty, 100 + i) * ArtTile);
                int y = oy + (int)(Rand01(tx, ty, 120 + i) * ArtTile);
                c.Set(x, y, new Color(110, 92, 60, 255));
                c.Set(x + 1, y, new Color(90, 74, 50, 255));
            }
        }

        private void PaintTallGround(int ox, int oy, int tx, int ty)
        {
            for (int y = 0; y < ArtTile; y++)
                for (int x = 0; x < ArtTile; x++)
                {
                    float n = ValueNoise((ox + x) / 9f, (oy + y) / 9f);
                    c.Set(ox + x, oy + y, PixelCanvas.Mix(TallGround, PixelCanvas.Shadow(TallGround, 0.35f), n * 0.8f));
                }
        }

        private void PaintFlowers(int ox, int oy, int tx, int ty)
        {
            // Mostly white clusters like Platinum's flower beds, with the odd red or yellow bloom
            var white = new Color(252, 252, 252, 255);
            var shade = new Color(196, 214, 238, 255);
            var leaf = new Color(60, 150, 80, 255);
            int count = 6 + (int)(Rand01(tx, ty, 200) * 3);
            for (int i = 0; i < count; i++)
            {
                int fx = ox + 3 + (int)(Rand01(tx, ty, 210 + i) * (ArtTile - 6));
                int fy = oy + 3 + (int)(Rand01(tx, ty, 230 + i) * (ArtTile - 6));
                float r = Rand01(tx, ty, 250 + i);
                bool isWhite = r >= 0.2f;
                var petal = r < 0.12f ? new Color(236, 76, 88, 255) : r < 0.2f ? new Color(250, 214, 72, 255) : white;
                var petalShade = isWhite ? shade : PixelCanvas.Shadow(petal, 0.3f);
                c.Set(fx - 2, fy + 2, leaf);
                c.Set(fx + 2, fy + 2, leaf);
                c.Rect(fx - 1, fy - 2, 3, 1, petal);
                c.Rect(fx - 2, fy - 1, 5, 2, petal);
                c.Rect(fx - 1, fy + 1, 3, 1, petalShade);
                c.Set(fx, fy, new Color(250, 196, 64, 255));
            }
        }

        private void PaintPath(int ox, int oy, int tx, int ty)
        {
            for (int y = 0; y < ArtTile; y++)
                for (int x = 0; x < ArtTile; x++)
                {
                    int gx = ox + x, gy = oy + y;
                    float n = ValueNoise(gx / 14f, gy / 14f);
                    var col = PixelCanvas.Mix(PathBase, PathDark, n * 0.35f);
                    uint h = Hash(gx, gy, 3) & 15;
                    if (h == 0) col = PathDark;
                    else if (h == 1) col = PathLight;
                    c.Set(gx, gy, col);
                }

            // Pebbles with a lit top and a shadowed bottom
            int pebbles = 5 + (int)(Rand01(tx, ty, 300) * 5);
            for (int i = 0; i < pebbles; i++)
            {
                int px = ox + 2 + (int)(Rand01(tx, ty, 310 + i) * (ArtTile - 5));
                int py = oy + 2 + (int)(Rand01(tx, ty, 330 + i) * (ArtTile - 5));
                var stone = PixelCanvas.Mix(new Color(176, 160, 140, 255), new Color(214, 204, 190, 255), Rand01(tx, ty, 350 + i));
                c.Rect(px, py, 3, 2, stone);
                c.Set(px, py, PixelCanvas.Light1(stone, 0.4f));
                c.HLine(px, py + 2, 3, PathEdge);
            }

            bool up = KindAt(tx, ty - 1) == Kind.Grass;
            bool down = KindAt(tx, ty + 1) == Kind.Grass;
            bool left = KindAt(tx - 1, ty) == Kind.Grass;
            bool right = KindAt(tx + 1, ty) == Kind.Grass;

            // Grass creeping over the edges, a few blades reaching further, then a light sandy border
            for (int i = 0; i < ArtTile; i++)
            {
                int d = 2 + (int)(Rand01(tx * 32 + i, ty, 400) * 3);
                if (Rand01(tx * 32 + i, ty, 401) < 0.18f) d += 3;
                if (up) EdgeRun(ox + i, oy, 0, 1, d);
                if (down) EdgeRun(ox + i, oy + ArtTile - 1, 0, -1, d);
                if (left) EdgeRun(ox, oy + i, 1, 0, d);
                if (right) EdgeRun(ox + ArtTile - 1, oy + i, -1, 0, d);
            }

            if (!up && !left && KindAt(tx - 1, ty - 1) == Kind.Grass) c.Disc(ox, oy, 3.5f, GrassBase);
            if (!up && !right && KindAt(tx + 1, ty - 1) == Kind.Grass) c.Disc(ox + ArtTile, oy, 3.5f, GrassBase);
            if (!down && !left && KindAt(tx - 1, ty + 1) == Kind.Grass) c.Disc(ox, oy + ArtTile, 3.5f, GrassBase);
            if (!down && !right && KindAt(tx + 1, ty + 1) == Kind.Grass) c.Disc(ox + ArtTile, oy + ArtTile, 3.5f, GrassBase);
        }

        /// <summary>One column (or row) of grass overhang starting at an edge and running inward.</summary>
        private void EdgeRun(int x, int y, int dx, int dy, int depth)
        {
            for (int k = 0; k < depth; k++)
            {
                var col = k == depth - 1 ? GrassLight : k == 0 ? GrassBase : PixelCanvas.Mix(GrassBase, GrassDark, 0.3f);
                c.Set(x + dx * k, y + dy * k, col);
            }
            c.Set(x + dx * depth, y + dy * depth, PathEdge);
            c.Set(x + dx * (depth + 1), y + dy * (depth + 1), PathLight);
        }

        /// <summary>Rounded stones along land that borders a pond (sand along the sea outside the map).</summary>
        private void PaintShore(int ox, int oy, int tx, int ty)
        {
            bool inMap = map.InBounds(tx, ty);
            void Rim(int x0, int y0, int w, int h, bool horizontal)
            {
                if (!inMap)
                {
                    c.Rect(x0, y0, w, h, Sand);
                    return;
                }
                c.Rect(x0, y0, w, h, new Color(118, 110, 104, 255));
                int count = (horizontal ? w : h) / 5;
                for (int i = 0; i < count; i++)
                {
                    float cx = horizontal ? x0 + i * 5 + 2.5f : x0 + w / 2f;
                    float cy = horizontal ? y0 + h / 2f : y0 + i * 5 + 2.5f;
                    var stone = PixelCanvas.Mix(PondRim, new Color(186, 178, 168, 255), Rand01(tx * 7 + i, ty, 500));
                    c.FlatEllipse(cx, cy, 2.6f, 2.4f, stone);
                    c.Set((int)cx - 1, (int)cy - 1, PixelCanvas.Light1(stone, 0.4f));
                }
            }
            if (KindAt(tx, ty - 1) == Kind.Water) Rim(ox, oy, ArtTile, 6, true);
            if (KindAt(tx, ty + 1) == Kind.Water) Rim(ox, oy + ArtTile - 6, ArtTile, 6, true);
            if (KindAt(tx - 1, ty) == Kind.Water) Rim(ox, oy, 6, ArtTile, false);
            if (KindAt(tx + 1, ty) == Kind.Water) Rim(ox + ArtTile - 6, oy, 6, ArtTile, false);
        }

        private void PaintShrineFloor(int ox, int oy, int tx, int ty)
        {
            var stone = new Color(178, 170, 198, 255);
            for (int y = 0; y < ArtTile; y++)
                for (int x = 0; x < ArtTile; x++)
                {
                    var col = PixelCanvas.Mix(stone, PixelCanvas.Shadow(stone, 0.2f), ValueNoise((ox + x) / 6f, (oy + y) / 6f) * 0.5f);
                    if (y % 16 == 15 || (x + (y / 16) * 8) % 16 == 15) col = PixelCanvas.Shadow(stone, 0.35f);
                    else if (y % 16 == 0 || (x + (y / 16) * 8) % 16 == 0) col = PixelCanvas.Light1(stone, 0.3f);
                    c.Set(ox + x, oy + y, col);
                }
        }

        // ------------------------------------------------------------ contact shading (not shadows)

        /// <summary>Darkens the ground right against a building's walls, where little skylight reaches.</summary>
        public void BuildingOcclusion(BuildingInfo b)
        {
            int x0 = OX(b.X0), x1 = OX(b.X1 + 1), front = OY(b.Y1 + 1), back = OY(b.Y0 + 1);
            for (int d = 0; d < 7; d++)
            {
                int a = (int)(70 * (1f - d / 7f));
                var shade = new Color(10, 30, 20, a);
                c.HLine(x0 - d, front + d, x1 - x0 + d * 2, shade);
                c.VLine(x0 - 1 - d, back, front - back, shade);
                c.VLine(x1 + d, back, front - back, shade);
            }
        }

        public void TreeOcclusion(int tx, int ty) => Blob(OX(tx) + 16f, OY(ty) + 17f, 14f, 11f, 70);

        /// <summary>Darkens the floor along the walls of a room.</summary>
        public void InteriorOcclusion()
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

        private void Blob(float cx, float cy, float rx, float ry, int alpha)
        {
            for (int y = (int)(cy - ry); y <= (int)(cy + ry); y++)
                for (int x = (int)(cx - rx); x <= (int)(cx + rx); x++)
                {
                    float u = (x + 0.5f - cx) / rx, v = (y + 0.5f - cy) / ry;
                    float d = u * u + v * v;
                    if (d < 1f) c.Set(x, y, new Color(16, 36, 28, (int)(alpha * Math.Min(1f, (1f - d) * 1.6f))));
                }
        }

        // ------------------------------------------------------------ interiors

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

    /// <summary>Smooth value noise in [0, 1].</summary>
    private static float ValueNoise(float x, float y)
    {
        int x0 = (int)MathF.Floor(x), y0 = (int)MathF.Floor(y);
        float fx = x - x0, fy = y - y0;
        fx = fx * fx * (3 - 2 * fx);
        fy = fy * fy * (3 - 2 * fy);
        float a = Rand01(x0, y0, 900), b = Rand01(x0 + 1, y0, 900);
        float c = Rand01(x0, y0 + 1, 900), d = Rand01(x0 + 1, y0 + 1, 900);
        return a + (b - a) * fx + (c - a) * fy + (a - b - c + d) * fx * fy;
    }
}
