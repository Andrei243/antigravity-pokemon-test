using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// Bakes each map's ground layer into a single pixel-art texture. Tiles are painted with knowledge of
/// their neighbours (grass fringes on paths, shorelines, roof ridges and eaves, windows, furniture),
/// and outdoor maps get a border of scenery so small maps never show black bars.
/// </summary>
public static class MapRenderer
{
    public const int ArtTile = 16;        // art pixels per tile (drawn at 2x in world space)
    public const int OutdoorMargin = 8;   // tiles of scenery baked around outdoor maps

    private sealed class BakedMap
    {
        public Texture2D Texture;
        public int Margin;
        public readonly List<(int X, int Y)> WaterTiles = new();
    }

    private static readonly Dictionary<string, BakedMap> Cache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<TileType, Texture2D> OverheadCache = new();
    private static Texture2D? tallGrassFront;

    // ------------------------------------------------------------------ palette

    private static readonly Color GrassBase = new(118, 198, 94, 255);
    private static readonly Color GrassDark = new(82, 160, 74, 255);
    private static readonly Color GrassLight = new(160, 222, 120, 255);
    private static readonly Color ForestFloor = new(70, 138, 70, 255);

    private static readonly Color TallBase = new(56, 146, 76, 255);
    private static readonly Color TallDark = new(34, 104, 62, 255);
    private static readonly Color TallBlade = new(98, 186, 92, 255);
    private static readonly Color TallTip = new(170, 228, 128, 255);

    private static readonly Color PathBase = new(228, 204, 152, 255);
    private static readonly Color PathDark = new(200, 172, 120, 255);
    private static readonly Color PathLight = new(242, 226, 186, 255);
    private static readonly Color PathEdge = new(178, 148, 100, 255);

    private static readonly Color WaterBase = new(86, 158, 232, 255);
    private static readonly Color WaterDark = new(62, 126, 210, 255);
    private static readonly Color WaterLight = new(150, 206, 248, 255);
    private static readonly Color Foam = new(232, 246, 255, 255);

    private static readonly Color WallExt = new(246, 238, 220, 255);
    private static readonly Color WallExtShade = new(214, 202, 180, 255);
    private static readonly Color Foundation = new(170, 160, 156, 255);
    private static readonly Color OutlineDark = new(44, 36, 52, 255);

    // ------------------------------------------------------------------ public API

    public static void DrawGround(Map map)
    {
        var baked = GetBaked(map);
        float scale = Player.TileSize / (float)ArtTile;
        float origin = -baked.Margin * Player.TileSize;

        var src = new Rectangle(0, 0, baked.Texture.Width, baked.Texture.Height);
        var dst = new Rectangle(origin, origin, baked.Texture.Width * scale, baked.Texture.Height * scale);
        Raylib.DrawTexturePro(baked.Texture, src, dst, Vector2.Zero, 0f, Color.White);

        // Animated glints on water
        double time = Raylib.GetTime();
        foreach (var (x, y) in baked.WaterTiles)
        {
            float phase = Rand01(x, y, 7);
            float cycle = (float)((time / 2.6 + phase) % 1.0);
            if (cycle > 0.3f) continue;

            float a = MathF.Sin(cycle / 0.3f * MathF.PI);
            int gx = 2 + (int)(Rand01(x, y, 8) * 10);
            int gy = 3 + (int)(Rand01(x, y, 9) * 10);
            var glint = new Color(240, 250, 255, (int)(a * 220));
            Raylib.DrawRectangle(x * Player.TileSize + gx * 2, y * Player.TileSize + gy * 2, 6, 2, glint);
            Raylib.DrawRectangle(x * Player.TileSize + gx * 2 + 2, y * Player.TileSize + gy * 2 - 2, 2, 2, glint);
        }
    }

    public static void DrawOverheadTile(TileType type, int x, int y)
    {
        if (!OverheadCache.TryGetValue(type, out var tex))
        {
            var c = new PixelCanvas(ArtTile, ArtTile);
            if (type == TileType.Tree) c.Blit(BuildTreeSprite(0), 0, -6);
            else c.Rect(0, 0, ArtTile, ArtTile, GrassBase);
            tex = c.ToTexture();
            OverheadCache[type] = tex;
        }
        Raylib.DrawTextureEx(tex, new Vector2(x * Player.TileSize, y * Player.TileSize), 0f, Player.TileSize / (float)ArtTile, Color.White);
    }

    /// <summary>Draws the front blades of a tall grass tile over a character standing in it.</summary>
    public static void DrawTallGrassFront(int tileX, int tileY)
    {
        if (tallGrassFront == null)
        {
            var tile = BuildTallGrassTile();
            var front = new PixelCanvas(ArtTile, ArtTile);
            for (int y = 9; y < ArtTile; y++)
                for (int x = 0; x < ArtTile; x++)
                    front.Set(x, y, tile.Get(x, y));
            tallGrassFront = front.ToTexture();
        }
        Raylib.DrawTextureEx(tallGrassFront.Value, new Vector2(tileX * Player.TileSize, tileY * Player.TileSize), 0f, Player.TileSize / (float)ArtTile, Color.White);
    }

    // ------------------------------------------------------------------ baking

    private static BakedMap GetBaked(Map map)
    {
        if (Cache.TryGetValue(map.Name, out var cached)) return cached;

        int margin = map.IsIndoors ? 0 : OutdoorMargin;
        int tilesW = map.Width + margin * 2;
        int tilesH = map.Height + margin * 2;
        var canvas = new PixelCanvas(tilesW * ArtTile, tilesH * ArtTile);
        var baked = new BakedMap { Margin = margin };
        var ctx = new Ctx(map, canvas, margin);

        // Pass 1: ground tiles
        for (int ty = -margin; ty < map.Height + margin; ty++)
        {
            for (int tx = -margin; tx < map.Width + margin; tx++)
            {
                var t = ctx.TypeAt(tx, ty);
                if (t == null) continue;
                ctx.PaintBase(t.Value, tx, ty);
                if (t == TileType.Water) baked.WaterTiles.Add((tx, ty));
            }
        }

        // Pass 2: tall props, top to bottom so lower ones overlap the row above
        for (int ty = -margin; ty < map.Height + margin; ty++)
        {
            for (int tx = -margin; tx < map.Width + margin; tx++)
            {
                var t = ctx.TypeAt(tx, ty);
                if (t != null) ctx.PaintProp(t.Value, tx, ty);
            }
        }

        baked.Texture = canvas.ToTexture();
        Cache[map.Name] = baked;
        return baked;
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

        public TileType? TypeAt(int x, int y)
        {
            if (map.InBounds(x, y)) return map.GetGroundTile(x, y);
            if (map.IsIndoors) return null;

            // Outside the map: extend roads and water outward, everything else becomes forest
            var edge = map.GetGroundTile(Math.Clamp(x, 0, map.Width - 1), Math.Clamp(y, 0, map.Height - 1));
            return edge switch
            {
                TileType.Path => TileType.Path,
                TileType.Water => TileType.Water,
                _ => TileType.Tree
            };
        }

        private Kind KindAt(int x, int y)
        {
            var t = TypeAt(x, y);
            return t switch
            {
                null => Kind.Void,
                TileType.Path => Kind.Path,
                TileType.Water => Kind.Water,
                TileType.RoofRed or TileType.RoofBlue or TileType.Wall or TileType.Door => Kind.Building,
                TileType.Floor or TileType.PC => map.IsIndoors ? Kind.Floor : Kind.Path,
                TileType.Signpost => IsWallSign(x, y) ? Kind.Building : SignGround(x, y),
                _ => Kind.Grass
            };
        }

        private bool IsRoof(int x, int y, TileType roof) => TypeAt(x, y) == roof;

        private bool IsWallLike(int x, int y)
        {
            var t = TypeAt(x, y);
            return t == TileType.Wall || t == TileType.Door || (t == TileType.Signpost && IsWallSign(x, y));
        }

        private bool IsWallSign(int x, int y)
        {
            var l = map.InBounds(x - 1, y) ? map.GetGroundTile(x - 1, y) : (TileType?)null;
            var r = map.InBounds(x + 1, y) ? map.GetGroundTile(x + 1, y) : (TileType?)null;
            return l is TileType.Wall or TileType.Door || r is TileType.Wall or TileType.Door;
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

        // ------------------------------------------------------------ pass 1

        public void PaintBase(TileType t, int tx, int ty)
        {
            int ox = OX(tx), oy = OY(ty);
            switch (t)
            {
                case TileType.Grass: PaintGrass(ox, oy, tx, ty); break;
                case TileType.FlowerGrass: PaintGrass(ox, oy, tx, ty); PaintFlowers(ox, oy, tx, ty); break;
                case TileType.TallGrass: c.Blit(BuildTallGrassTile(), ox, oy); break;
                case TileType.Path: PaintPath(ox, oy, tx, ty); break;
                case TileType.Water: PaintWater(ox, oy, tx, ty); break;
                case TileType.LedgeDown: PaintLedge(ox, oy, tx, ty); break;
                case TileType.Tree:
                case TileType.TreeTrunk: c.Rect(ox, oy, ArtTile, ArtTile, ForestFloor); break;
                case TileType.RoofRed: PaintRoof(ox, oy, tx, ty, TileType.RoofRed, new Color(212, 72, 64, 255)); break;
                case TileType.RoofBlue: PaintRoof(ox, oy, tx, ty, TileType.RoofBlue, new Color(72, 118, 212, 255)); break;
                case TileType.Wall:
                    if (map.IsIndoors) PaintInteriorWall(ox, oy, tx, ty);
                    else PaintExteriorWall(ox, oy, tx, ty, allowWindow: true);
                    break;
                case TileType.Door:
                    if (map.IsIndoors) PaintExitMat(ox, oy, tx, ty);
                    else PaintDoor(ox, oy, tx, ty);
                    break;
                case TileType.Floor:
                case TileType.PC:
                    if (map.IsIndoors) PaintFloor(ox, oy, tx, ty);
                    else PaintShrineFloor(ox, oy, tx, ty);
                    break;
                case TileType.Signpost:
                    if (IsWallSign(tx, ty)) PaintExteriorWall(ox, oy, tx, ty, allowWindow: false);
                    else if (SignGround(tx, ty) == Kind.Path) PaintPath(ox, oy, tx, ty);
                    else PaintGrass(ox, oy, tx, ty);
                    break;
            }
        }

        private void PaintGrass(int ox, int oy, int tx, int ty)
        {
            c.Rect(ox, oy, ArtTile, ArtTile, GrassBase);

            // Sparse tufts and speckles, seeded per tile so the pattern never visibly repeats
            int tufts = 2 + (int)(Rand01(tx, ty, 1) * 3);
            for (int i = 0; i < tufts; i++)
            {
                int px = ox + 1 + (int)(Rand01(tx, ty, 10 + i) * 12);
                int py = oy + 3 + (int)(Rand01(tx, ty, 20 + i) * 11);
                c.Set(px, py, GrassDark);
                c.Set(px + 1, py + 1, GrassDark);
                c.Set(px + 2, py, GrassDark);
                c.Set(px + 1, py - 1, GrassLight);
            }
            for (int i = 0; i < 3; i++)
            {
                c.Set(ox + (int)(Rand01(tx, ty, 30 + i) * 16), oy + (int)(Rand01(tx, ty, 40 + i) * 16), GrassLight);
            }
        }

        private void PaintFlowers(int ox, int oy, int tx, int ty)
        {
            Color[] petals =
            {
                new(236, 76, 88, 255), new(250, 150, 190, 255), new(250, 220, 72, 255), new(250, 250, 250, 255)
            };
            var spots = new[] { (4, 5), (11, 10), (5, 12) };
            for (int i = 0; i < spots.Length; i++)
            {
                if (i == 2 && Rand01(tx, ty, 50) < 0.5f) continue;
                var petal = petals[(int)(Rand01(tx, ty, 51 + i) * petals.Length) % petals.Length];
                int fx = ox + spots[i].Item1, fy = oy + spots[i].Item2;
                c.Set(fx, fy + 2, GrassDark);
                c.Set(fx - 1, fy + 1, GrassDark);
                c.Set(fx, fy - 1, petal);
                c.Set(fx - 1, fy, petal);
                c.Set(fx + 1, fy, petal);
                c.Set(fx, fy + 1, PixelCanvas.Shadow(petal, 0.25f));
                c.Set(fx, fy, new Color(250, 196, 64, 255));
            }
        }

        private void PaintPath(int ox, int oy, int tx, int ty)
        {
            c.Rect(ox, oy, ArtTile, ArtTile, PathBase);
            for (int i = 0; i < 4; i++)
            {
                int px = ox + 1 + (int)(Rand01(tx, ty, 60 + i) * 13);
                int py = oy + 1 + (int)(Rand01(tx, ty, 70 + i) * 13);
                c.Set(px, py, PathDark);
                c.Set(px + 1, py, PathDark);
                c.Set(px, py - 1, PathLight);
            }

            bool up = KindAt(tx, ty - 1) == Kind.Grass;
            bool down = KindAt(tx, ty + 1) == Kind.Grass;
            bool left = KindAt(tx - 1, ty) == Kind.Grass;
            bool right = KindAt(tx + 1, ty) == Kind.Grass;

            for (int i = 0; i < ArtTile; i++)
            {
                int d = 1 + (int)(Rand01(tx * 16 + i, ty, 80) * 2);
                if (up) { c.VLine(ox + i, oy, d, GrassBase); c.Set(ox + i, oy + d, PathEdge); }
                if (down) { c.VLine(ox + i, oy + ArtTile - d, d, GrassBase); c.Set(ox + i, oy + ArtTile - d - 1, PathEdge); }
                if (left) { c.HLine(ox, oy + i, d, GrassBase); c.Set(ox + d, oy + i, PathEdge); }
                if (right) { c.HLine(ox + ArtTile - d, oy + i, d, GrassBase); c.Set(ox + ArtTile - d - 1, oy + i, PathEdge); }
            }

            // Rounded grass nubs on inside corners
            if (!up && !left && KindAt(tx - 1, ty - 1) == Kind.Grass) Nub(ox, oy, 1, 1);
            if (!up && !right && KindAt(tx + 1, ty - 1) == Kind.Grass) Nub(ox + ArtTile - 2, oy, -1, 1);
            if (!down && !left && KindAt(tx - 1, ty + 1) == Kind.Grass) Nub(ox, oy + ArtTile - 2, 1, -1);
            if (!down && !right && KindAt(tx + 1, ty + 1) == Kind.Grass) Nub(ox + ArtTile - 2, oy + ArtTile - 2, -1, -1);
        }

        private void Nub(int x, int y, int sx, int sy)
        {
            c.Rect(x, y, 2, 2, GrassBase);
            c.Set(x + (sx > 0 ? 2 : -1), y + (sy > 0 ? 0 : 1), PathEdge);
            c.Set(x + (sx > 0 ? 0 : 1), y + (sy > 0 ? 2 : -1), PathEdge);
        }

        private void PaintWater(int ox, int oy, int tx, int ty)
        {
            c.Rect(ox, oy, ArtTile, ArtTile, WaterBase);
            for (int i = 0; i < 2; i++)
            {
                int wx = ox + 1 + (int)(Rand01(tx, ty, 90 + i) * 10);
                int wy = oy + 3 + i * 7 + (int)(Rand01(tx, ty, 95 + i) * 3);
                c.HLine(wx, wy, 4, WaterLight);
                c.HLine(wx + 1, wy + 1, 4, WaterDark);
            }

            // Shoreline foam where water meets land
            bool up = KindAt(tx, ty - 1) != Kind.Water;
            bool down = KindAt(tx, ty + 1) != Kind.Water;
            bool left = KindAt(tx - 1, ty) != Kind.Water;
            bool right = KindAt(tx + 1, ty) != Kind.Water;
            if (up) { c.HLine(ox, oy, ArtTile, WaterDark); c.HLine(ox, oy + 1, ArtTile, Foam); c.HLine(ox, oy + 2, ArtTile, WaterLight); }
            if (down) { c.HLine(ox, oy + ArtTile - 1, ArtTile, Foam); c.HLine(ox, oy + ArtTile - 2, ArtTile, WaterLight); }
            if (left) { c.VLine(ox, oy, ArtTile, Foam); c.VLine(ox + 1, oy, ArtTile, WaterLight); }
            if (right) { c.VLine(ox + ArtTile - 1, oy, ArtTile, Foam); c.VLine(ox + ArtTile - 2, oy, ArtTile, WaterLight); }
        }

        private void PaintLedge(int ox, int oy, int tx, int ty)
        {
            PaintGrass(ox, oy, tx, ty);
            bool left = TypeAt(tx - 1, ty) == TileType.LedgeDown;
            bool right = TypeAt(tx + 1, ty) == TileType.LedgeDown;
            var face = new Color(186, 150, 96, 255);
            var faceDark = new Color(150, 116, 74, 255);

            for (int x = 0; x < ArtTile; x++)
            {
                // Taper the ends so the ledge reads as a raised lip rather than a stripe
                int inset = 0;
                if (!left && x < 3) inset = 3 - x;
                if (!right && x > ArtTile - 4) inset = x - (ArtTile - 4);

                int top = 8 + inset;
                c.Set(ox + x, oy + top - 1, GrassLight);
                c.VLine(ox + x, oy + top, 12 - top + 1, face);
                c.Set(ox + x, oy + 12, faceDark);
                c.Set(ox + x, oy + 13, OutlineDark);
                c.Set(ox + x, oy + 14, GrassDark);
            }
        }

        private void PaintRoof(int ox, int oy, int tx, int ty, TileType roof, Color baseColor)
        {
            bool up = IsRoof(tx, ty - 1, roof), down = IsRoof(tx, ty + 1, roof);
            bool left = IsRoof(tx - 1, ty, roof), right = IsRoof(tx + 1, ty, roof);

            int above = 0;
            while (IsRoof(tx, ty - above - 1, roof)) above++;
            int below = 0;
            while (IsRoof(tx, ty + below + 1, roof)) below++;
            float rows = (above + below + 1) * ArtTile;

            var ridge = PixelCanvas.Light1(baseColor, 0.45f);
            for (int y = 0; y < ArtTile; y++)
            {
                int ly = above * ArtTile + y;
                var tone = PixelCanvas.Mix(PixelCanvas.Light1(baseColor, 0.18f), PixelCanvas.Shadow(baseColor, 0.22f), ly / rows);
                for (int x = 0; x < ArtTile; x++)
                {
                    int shingleRow = ly / 4;
                    int gx = (tx * ArtTile + x) + (shingleRow % 2) * 2;
                    Color col = tone;
                    if (ly % 4 == 3) col = PixelCanvas.Shadow(tone, 0.35f);
                    else if (ly % 4 == 0) col = PixelCanvas.Light1(tone, 0.22f);
                    else if (gx % 4 == 0) col = PixelCanvas.Shadow(tone, 0.18f);
                    c.Set(ox + x, oy + y, col);
                }
            }

            if (!up)
            {
                c.HLine(ox, oy, ArtTile, OutlineDark);
                c.HLine(ox, oy + 1, ArtTile, ridge);
                c.HLine(ox, oy + 2, ArtTile, PixelCanvas.Light1(baseColor, 0.3f));
                c.HLine(ox, oy + 3, ArtTile, PixelCanvas.Shadow(baseColor, 0.3f));
            }
            if (!down)
            {
                c.HLine(ox, oy + 12, ArtTile, PixelCanvas.Light1(baseColor, 0.3f));
                c.HLine(ox, oy + 13, ArtTile, baseColor);
                c.HLine(ox, oy + 14, ArtTile, PixelCanvas.Shadow(baseColor, 0.4f));
                c.HLine(ox, oy + 15, ArtTile, OutlineDark);
            }
            if (!left)
            {
                c.VLine(ox, oy, ArtTile, OutlineDark);
                c.VLine(ox + 1, oy + (up ? 0 : 1), ArtTile - (up ? 0 : 1) - (down ? 0 : 1), ridge);
            }
            if (!right)
            {
                c.VLine(ox + ArtTile - 1, oy, ArtTile, OutlineDark);
                c.VLine(ox + ArtTile - 2, oy + (up ? 0 : 1), ArtTile - (up ? 0 : 1) - (down ? 0 : 1), PixelCanvas.Shadow(baseColor, 0.4f));
            }
        }

        private void PaintExteriorWall(int ox, int oy, int tx, int ty, bool allowWindow)
        {
            c.Rect(ox, oy, ArtTile, ArtTile, WallExt);
            for (int y = 3; y < 13; y += 4) c.HLine(ox, oy + y, ArtTile, PixelCanvas.Mix(WallExt, WallExtShade, 0.5f));

            var above = TypeAt(tx, ty - 1);
            if (above is TileType.RoofRed or TileType.RoofBlue)
            {
                c.HLine(ox, oy, ArtTile, WallExtShade);
                c.HLine(ox, oy + 1, ArtTile, PixelCanvas.Mix(WallExt, WallExtShade, 0.5f));
            }

            c.HLine(ox, oy + 13, ArtTile, Foundation);
            c.HLine(ox, oy + 14, ArtTile, PixelCanvas.Shadow(Foundation, 0.2f));
            c.HLine(ox, oy + 15, ArtTile, OutlineDark);

            if (!IsWallLike(tx - 1, ty)) c.VLine(ox, oy, ArtTile, OutlineDark);
            if (!IsWallLike(tx + 1, ty)) { c.VLine(ox + ArtTile - 1, oy, ArtTile, OutlineDark); c.VLine(ox + ArtTile - 2, oy, 13, WallExtShade); }

            bool nextToDoor = TypeAt(tx - 1, ty) == TileType.Door || TypeAt(tx + 1, ty) == TileType.Door;
            if (allowWindow && !nextToDoor)
            {
                var frame = new Color(110, 92, 84, 255);
                var glass = new Color(150, 206, 244, 255);
                c.Rect(ox + 3, oy + 3, 10, 8, frame);
                c.Rect(ox + 4, oy + 4, 8, 6, glass);
                c.VLine(ox + 8, oy + 4, 6, frame);
                c.Set(ox + 5, oy + 5, Color.White);
                c.Set(ox + 6, oy + 5, Color.White);
                c.Set(ox + 5, oy + 6, Color.White);
                c.HLine(ox + 4, oy + 9, 8, PixelCanvas.Shadow(glass, 0.3f));
                c.HLine(ox + 2, oy + 11, 12, new Color(232, 224, 206, 255));
                c.HLine(ox + 2, oy + 12, 12, WallExtShade);
            }
        }

        private void PaintDoor(int ox, int oy, int tx, int ty)
        {
            PaintExteriorWall(ox, oy, tx, ty, allowWindow: false);

            // Awning in the colour of this building's roof
            Color awning = new(212, 72, 64, 255);
            for (int d = 1; d <= 4; d++)
            {
                var t = TypeAt(tx, ty - d);
                if (t == TileType.RoofBlue) { awning = new Color(72, 118, 212, 255); break; }
                if (t == TileType.RoofRed) break;
            }

            var frame = new Color(92, 66, 50, 255);
            var wood = new Color(170, 114, 72, 255);
            c.Rect(ox + 2, oy + 2, 12, 12, frame);
            c.Rect(ox + 3, oy + 3, 10, 11, wood);
            c.VLine(ox + 7, oy + 3, 11, PixelCanvas.Shadow(wood, 0.3f));
            c.VLine(ox + 8, oy + 3, 11, PixelCanvas.Light1(wood, 0.2f));
            c.Rect(ox + 4, oy + 4, 2, 3, new Color(150, 206, 244, 255));
            c.Rect(ox + 10, oy + 4, 2, 3, new Color(150, 206, 244, 255));
            c.Set(ox + 6, oy + 9, new Color(250, 214, 80, 255));
            c.Set(ox + 9, oy + 9, new Color(250, 214, 80, 255));

            c.HLine(ox + 1, oy, 14, OutlineDark);
            c.HLine(ox + 1, oy + 1, 14, awning);
            for (int x = 2; x < 14; x += 3) c.Set(ox + x, oy + 1, Color.White);
            c.HLine(ox + 1, oy + 2, 14, PixelCanvas.Shadow(awning, 0.4f));

            c.Rect(ox + 1, oy + 14, 14, 2, new Color(204, 198, 190, 255));
            c.HLine(ox + 1, oy + 15, 14, new Color(150, 144, 140, 255));
        }

        private void PaintShrineFloor(int ox, int oy, int tx, int ty)
        {
            var stone = new Color(176, 168, 196, 255);
            c.Rect(ox, oy, ArtTile, ArtTile, stone);
            c.HLine(ox, oy + 7, ArtTile, PixelCanvas.Shadow(stone, 0.3f));
            c.VLine(ox + (tx % 2 == 0 ? 7 : 11), oy, 7, PixelCanvas.Shadow(stone, 0.3f));
            c.VLine(ox + 4, oy + 8, 8, PixelCanvas.Shadow(stone, 0.3f));
            c.Set(ox + 2, oy + 2, PixelCanvas.Light1(stone, 0.4f));
            c.Set(ox + 10, oy + 11, PixelCanvas.Light1(stone, 0.4f));
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
                // Wooden planks with staggered joints
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
                // Large checker tiles with grout
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

            // Soft shadow cast by the back wall
            if (TypeAt(tx, ty - 1) == TileType.Wall && IsBackWall(tx, ty - 1))
            {
                c.HLine(ox, oy, ArtTile, new Color(0, 0, 0, 60));
                c.HLine(ox, oy + 1, ArtTile, new Color(0, 0, 0, 30));
            }
        }

        private bool IsBorder(int tx, int ty) => tx == 0 || ty == 0 || tx == map.Width - 1 || ty == map.Height - 1;

        private bool IsBackWall(int tx, int ty) => ty <= 1 && tx > 0 && tx < map.Width - 1;

        private void PaintInteriorWall(int ox, int oy, int tx, int ty)
        {
            if (IsBackWall(tx, ty))
            {
                PaintBackWall(ox, oy, tx, ty);
            }
            else if (IsBorder(tx, ty))
            {
                // Top of the side/front walls, seen from above
                var top = new Color(64, 58, 84, 255);
                var edge = new Color(112, 104, 138, 255);
                c.Rect(ox, oy, ArtTile, ArtTile, top);
                for (int i = 0; i < ArtTile; i += 4) c.Set(ox + (i + ty * 2) % ArtTile, oy + i, PixelCanvas.Light1(top, 0.1f));
                // Light lip only on the side that faces into the room
                bool bottom = ty == map.Height - 1;
                if (tx == 0 && !bottom) c.Rect(ox + ArtTile - 2, oy, 2, ArtTile, edge);
                if (tx == map.Width - 1 && !bottom) c.Rect(ox, oy, 2, ArtTile, edge);
                if (bottom && tx > 0 && tx < map.Width - 1) c.Rect(ox, oy, ArtTile, 2, edge);
                if (bottom && tx == 0) c.Rect(ox + ArtTile - 2, oy, 2, 2, edge);
                if (bottom && tx == map.Width - 1) c.Rect(ox, oy, 2, 2, edge);
            }
            else
            {
                PaintFloor(ox, oy, tx, ty);
                PaintCounter(ox, oy, tx, ty);
            }
        }

        private void PaintBackWall(int ox, int oy, int tx, int ty)
        {
            var (paper, band, _, _) = InteriorColors();
            c.Rect(ox, oy, ArtTile, ArtTile, paper);
            for (int x = 1; x < ArtTile; x += 4) c.VLine(ox + x, oy, ArtTile, PixelCanvas.Shadow(paper, 0.06f));

            if (ty == 0)
            {
                c.Rect(ox, oy, ArtTile, 3, new Color(64, 58, 84, 255));
                c.HLine(ox, oy + 3, ArtTile, PixelCanvas.Shadow(band, 0.2f));
                c.HLine(ox, oy + 4, ArtTile, band);

                // A window every few tiles on the back wall of homes and labs
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

        private void PaintCounter(int ox, int oy, int tx, int ty)
        {
            bool up = IsCounter(tx, ty - 1), down = IsCounter(tx, ty + 1);
            bool left = IsCounter(tx - 1, ty), right = IsCounter(tx + 1, ty);

            (Color top, Color front) = map.Interior switch
            {
                InteriorStyle.PokemonCenter => (new Color(250, 250, 252, 255), new Color(224, 92, 100, 255)),
                InteriorStyle.PokeMart => (new Color(206, 214, 224, 255), new Color(120, 132, 156, 255)),
                InteriorStyle.Lab => (new Color(236, 238, 246, 255), new Color(132, 142, 170, 255)),
                _ => (new Color(222, 172, 116, 255), new Color(164, 114, 76, 255))
            };

            int frontH = down ? 0 : 6;
            int topH = ArtTile - frontH;
            c.Rect(ox, oy, ArtTile, topH, top);
            c.HLine(ox, oy + topH - 1, ArtTile, PixelCanvas.Shadow(top, 0.15f));
            if (!up) c.HLine(ox, oy + 1, ArtTile, PixelCanvas.Light1(top, 0.4f));
            if (frontH > 0)
            {
                c.Rect(ox, oy + topH, ArtTile, frontH, front);
                c.HLine(ox, oy + topH, ArtTile, PixelCanvas.Light1(front, 0.2f));
                c.HLine(ox, oy + ArtTile - 1, ArtTile, OutlineDark);
            }

            if (map.Interior == InteriorStyle.PokeMart)
            {
                // Goods lined up on the shelf
                Color[] goods = { new(232, 80, 80, 255), new(80, 150, 232, 255), new(250, 206, 72, 255), new(120, 200, 110, 255) };
                for (int i = 0; i < 3; i++)
                {
                    var g = goods[(tx + ty + i) % goods.Length];
                    c.Rect(ox + 2 + i * 5, oy + 3, 3, 5, g);
                    c.HLine(ox + 2 + i * 5, oy + 3, 3, PixelCanvas.Light1(g, 0.4f));
                    c.HLine(ox + 2 + i * 5, oy + 8, 3, PixelCanvas.Shadow(g, 0.4f));
                }
            }
            else if (map.Interior == InteriorStyle.Lab)
            {
                c.Rect(ox + 3 + (tx % 3), oy + 3, 5, 4, Color.White);
                c.HLine(ox + 4 + (tx % 3), oy + 4, 3, new Color(150, 150, 170, 255));
            }

            if (!up) c.HLine(ox, oy, ArtTile, OutlineDark);
            if (!left) c.VLine(ox, oy, ArtTile, OutlineDark);
            if (!right) c.VLine(ox + ArtTile - 1, oy, ArtTile, OutlineDark);
        }

        private bool IsCounter(int tx, int ty) =>
            map.InBounds(tx, ty) && map.GetGroundTile(tx, ty) == TileType.Wall && !IsBorder(tx, ty);

        private void PaintExitMat(int ox, int oy, int tx, int ty)
        {
            PaintFloor(ox, oy, tx, ty);
            var mat = map.Interior == InteriorStyle.PokeMart ? new Color(80, 132, 220, 255) : new Color(212, 76, 76, 255);
            c.Rect(ox + 1, oy + 2, 14, 12, PixelCanvas.Shadow(mat, 0.35f));
            c.Rect(ox + 2, oy + 3, 12, 10, mat);
            c.Rect(ox + 4, oy + 5, 8, 6, PixelCanvas.Light1(mat, 0.25f));
            // Down arrow showing this is the way out
            c.HLine(ox + 6, oy + 6, 4, Color.White);
            c.HLine(ox + 6, oy + 7, 4, Color.White);
            c.HLine(ox + 5, oy + 8, 6, Color.White);
            c.HLine(ox + 6, oy + 9, 4, Color.White);
            c.HLine(ox + 7, oy + 10, 2, Color.White);
        }

        // ------------------------------------------------------------ pass 2 (props that overflow their tile)

        public void PaintProp(TileType t, int tx, int ty)
        {
            int ox = OX(tx), oy = OY(ty);
            switch (t)
            {
                case TileType.Tree:
                case TileType.TreeTrunk:
                {
                    var tree = BuildTreeSprite((int)(Rand01(tx, ty, 3) * 3));
                    c.Blit(tree, ox, oy + ArtTile - tree.Height);
                    break;
                }
                case TileType.Signpost when !IsWallSign(tx, ty):
                {
                    var sign = BuildSignSprite();
                    c.Blit(sign, ox, oy + ArtTile - sign.Height);
                    break;
                }
                case TileType.Signpost:
                {
                    // Name plate mounted on a house wall
                    var plate = new Color(196, 150, 96, 255);
                    c.Rect(ox + 2, oy + 4, 12, 7, OutlineDark);
                    c.Rect(ox + 3, oy + 5, 10, 5, plate);
                    c.HLine(ox + 3, oy + 5, 10, PixelCanvas.Light1(plate, 0.35f));
                    c.HLine(ox + 5, oy + 7, 6, PixelCanvas.Shadow(plate, 0.45f));
                    break;
                }
                case TileType.PC:
                {
                    var pc = BuildPcSprite();
                    c.Blit(pc, ox, oy + ArtTile - pc.Height);
                    break;
                }
                case TileType.Door when !map.IsIndoors:
                {
                    // Mark Pokémon Centers and Marts above their doors, like the real signs
                    string? target = map.GetWarpAt(tx, ty)?.TargetMap;
                    if (target == "PokemonCenter") c.Blit(BuildCenterEmblem(), ox + 2, oy - ArtTile + 1);
                    else if (target == "PokeMart") c.Blit(BuildMartEmblem(), ox + 1, oy - ArtTile + 2);
                    break;
                }
            }
        }
    }

    // ------------------------------------------------------------------ shared tile sprites

    private static PixelCanvas BuildTallGrassTile()
    {
        var c = new PixelCanvas(ArtTile, ArtTile);
        c.Rect(0, 0, ArtTile, ArtTile, TallBase);

        // Two staggered rows of blade clumps, DS style
        foreach (var (rowY, shift) in new[] { (7, 0), (15, 2) })
        {
            c.HLine(0, rowY, ArtTile, TallDark);
            for (int cx = shift; cx < ArtTile + 4; cx += 4)
            {
                for (int h = 0; h < 6; h++)
                {
                    int y = rowY - 1 - h;
                    int lx = cx - 1 - h / 3;
                    int rx = cx + 1 + h / 3;
                    if (h < 5) c.Set(lx, y, h == 4 ? TallTip : TallBlade);
                    if (h < 4) c.Set(rx, y, h == 3 ? TallTip : TallDark);
                    c.Set(cx, y, h == 5 ? TallTip : TallBlade);
                }
            }
        }
        return c;
    }

    private static readonly Dictionary<int, PixelCanvas> TreeSprites = new();

    private static PixelCanvas BuildTreeSprite(int variant)
    {
        if (TreeSprites.TryGetValue(variant, out var cached)) return cached;

        var leaf = new Color(58, 148, 78, 255);
        var c = new PixelCanvas(ArtTile, 22);
        c.Rect(6, 17, 4, 4, new Color(118, 80, 52, 255));
        c.VLine(6, 17, 4, new Color(150, 108, 72, 255));
        c.Part();
        c.Ball(8f, 10.5f, 7.6f, 8.2f, leaf);

        // Leaf clusters: small darker arcs give the canopy texture
        var clusterDark = PixelCanvas.Shadow(leaf, 0.3f);
        var clusterLight = PixelCanvas.Light1(leaf, 0.3f);
        var spots = variant switch
        {
            1 => new[] { (5, 6), (10, 9), (6, 13), (11, 14) },
            2 => new[] { (7, 5), (4, 10), (10, 11), (7, 15) },
            _ => new[] { (6, 7), (10, 6), (5, 12), (10, 13) }
        };
        foreach (var (x, y) in spots)
        {
            c.HLine(x - 1, y + 1, 3, clusterDark);
            c.Set(x - 2, y, clusterDark);
            c.Set(x + 2, y, clusterDark);
            c.Set(x, y - 1, clusterLight);
        }
        c.OutlinePass();

        TreeSprites[variant] = c;
        return c;
    }

    private static PixelCanvas BuildSignSprite()
    {
        var wood = new Color(196, 142, 88, 255);
        var c = new PixelCanvas(ArtTile, ArtTile);
        c.Rect(7, 9, 2, 6, PixelCanvas.Shadow(wood, 0.35f));
        c.Part();
        c.Rect(2, 2, 12, 8, wood);
        c.HLine(2, 2, 12, PixelCanvas.Light1(wood, 0.35f));
        c.HLine(2, 9, 12, PixelCanvas.Shadow(wood, 0.3f));
        c.HLine(4, 4, 8, PixelCanvas.Shadow(wood, 0.45f));
        c.HLine(4, 6, 6, PixelCanvas.Shadow(wood, 0.45f));
        c.OutlinePass();
        c.GroundShadow(8, 15, 4, 1.2f, 60);
        return c;
    }

    private static PixelCanvas BuildPcSprite()
    {
        var casing = new Color(214, 218, 230, 255);
        var c = new PixelCanvas(ArtTile, 22);
        c.Rect(1, 13, 14, 8, new Color(176, 136, 98, 255));
        c.HLine(1, 13, 14, new Color(206, 166, 124, 255));
        c.Part();
        c.Rect(3, 2, 10, 10, casing);
        c.Rect(4, 3, 8, 6, new Color(64, 128, 216, 255));
        c.HLine(5, 4, 3, new Color(170, 220, 250, 255));
        c.HLine(4, 10, 8, PixelCanvas.Shadow(casing, 0.2f));
        c.Set(11, 10, new Color(90, 220, 120, 255));
        c.Part();
        c.Rect(4, 14, 8, 2, casing);
        c.OutlinePass();
        return c;
    }

    private static PixelCanvas BuildCenterEmblem()
    {
        var c = new PixelCanvas(12, 12);
        var red = new Color(232, 64, 64, 255);
        var white = new Color(250, 250, 252, 255);
        for (int y = 0; y < 12; y++)
            for (int x = 0; x < 12; x++)
            {
                float u = (x + 0.5f - 6f) / 5f, v = (y + 0.5f - 6f) / 5f;
                if (u * u + v * v <= 1f) c.Set(x, y, y < 6 ? red : white);
            }
        c.HLine(1, 5, 10, OutlineDark);
        c.HLine(1, 6, 10, OutlineDark);
        c.Rect(5, 4, 2, 4, OutlineDark);
        c.Set(5, 5, white); c.Set(6, 5, white); c.Set(5, 6, white); c.Set(6, 6, white);
        c.Set(3, 2, PixelCanvas.Light1(red, 0.5f));
        c.OutlinePass(innerSeams: false);
        return c;
    }

    private static PixelCanvas BuildMartEmblem()
    {
        var c = new PixelCanvas(14, 10);
        var blue = new Color(64, 120, 220, 255);
        c.Rect(1, 1, 12, 8, blue);
        c.HLine(1, 1, 12, PixelCanvas.Light1(blue, 0.35f));
        string[] m =
        {
            "X...X",
            "XX.XX",
            "X.X.X",
            "X...X",
            "X...X"
        };
        c.Stamp(5, 3, m, ch => ch == 'X' ? Color.White : null);
        c.OutlinePass(innerSeams: false);
        return c;
    }

    // ------------------------------------------------------------------ hashing

    private static uint Hash(int x, int y, int salt)
    {
        unchecked
        {
            uint h = (uint)(x * 374761393 + y * 668265263 + salt * 1442695041);
            h = (h ^ (h >> 13)) * 1274126177u;
            return h ^ (h >> 16);
        }
    }

    private static float Rand01(int x, int y, int salt) => (Hash(x, y, salt) & 0xFFFF) / 65536f;
}
