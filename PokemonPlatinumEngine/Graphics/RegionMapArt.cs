using System;
using System.Collections.Generic;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// The picture of a region on its map (plan 01 · M12; style guide, "The Town Map"): painted from the world's own
/// tiles, a texel a tile, in a palette of its own. Nothing of the original's picture is in it. Where the region has
/// ground of its own (its towns, routes and what lies in view of them) each tile is the colour of what it is; where
/// it has only the forest, rock and sea that fill the space between its places, and where it has nothing at all,
/// the land's outline is made up: a cell with no chunk is sea where open water or the map's edge can be come to
/// from it and land otherwise, and every border between two such cells is bent by noise, so no coast follows a
/// chunk's edge. The sea deepens away from the shore, mountains rise in bands to a cap of snow (of ash round a
/// volcano), forest is frosted in snow country, a building is a roof over a wall. No GPU call: it paints into a
/// <see cref="PixelCanvas"/>, the same picture every time.
/// </summary>
public static class RegionMapArt
{
    /// <summary>Texels to a cell: one to a tile.</summary>
    public const int CellTexels = Map.ChunkTiles;

    // What a texel is
    private const byte Sea = 0, Forest = 1, Broadleaf = 2, Mountain = 3, Lawn = 4, TallGrass = 5, Flowers = 6, Path = 7, Dirt = 8, Sand = 9,
        Snow = 10, Ice = 11, Marsh = 12, Paving = 13, Walkway = 14, Wood = 15, Bare = 16, Dark = 17, Roof = 18, Wall = 19, Puddle = 20;

    private const int Classes = 21;

    // A cell with nothing in it, until the map has made up what it is
    private const byte Empty = 255;

    // What the country round a cell is like
    private const byte Snowy = 1, Volcanic = 2;

    public static readonly Color SeaDeep = new(56, 104, 180, 255), SeaMid = new(70, 124, 198, 255), SeaShallow = new(94, 150, 214, 255), Shore = new(150, 194, 234, 255);
    private static readonly Color Pine = new(50, 108, 72, 255), PineDark = new(40, 92, 64, 255), PineLight = new(64, 124, 80, 255);
    private static readonly Color Leaf = new(70, 128, 62, 255), LeafDark = new(56, 110, 54, 255), LeafLight = new(88, 146, 70, 255);
    private static readonly Color Frost = new(104, 148, 136, 255), FrostDark = new(78, 122, 114, 255), FrostLight = new(176, 204, 206, 255);
    private static readonly Color LawnColor = new(134, 192, 106, 255), LawnShade = new(120, 180, 98, 255);
    private static readonly Color GrassColor = new(102, 166, 88, 255), GrassDark = new(88, 152, 80, 255);
    private static readonly Color PathColor = new(228, 208, 152, 255), DirtColor = new(192, 162, 118, 255), SandColor = new(240, 226, 174, 255);
    private static readonly Color SnowColor = new(232, 240, 248, 255), SnowShade = new(208, 222, 240, 255), IceColor = new(186, 222, 240, 255);
    private static readonly Color MarshColor = new(122, 144, 90, 255), PuddleColor = new(128, 168, 204, 255);
    private static readonly Color PavingColor = new(200, 204, 214, 255), PavingShade = new(184, 190, 202, 255), WalkwayColor = new(150, 170, 198, 255);
    private static readonly Color WoodColor = new(178, 134, 86, 255), BareColor = new(188, 172, 142, 255), DarkColor = new(46, 40, 54, 255);
    private static readonly Color[] Rock = { new(126, 106, 90, 255), new(144, 122, 102, 255), new(160, 138, 114, 255), new(176, 156, 132, 255), new(192, 174, 152, 255) };
    private static readonly Color[] Ash = { new(96, 82, 82, 255), new(110, 94, 92, 255), new(124, 106, 102, 255), new(136, 118, 112, 255), new(148, 130, 124, 255) };
    private static readonly Color PeakSnow = new(230, 236, 246, 255), PeakShade = new(204, 214, 232, 255);
    private static readonly Color PeakAsh = new(84, 72, 76, 255), PeakGlow = new(214, 110, 70, 255);
    private static readonly Color WallColor = new(244, 236, 214, 255), FlowerPink = new(240, 150, 184, 255), FlowerGold = new(246, 214, 110, 255);

    /// <summary>
    /// Paints the cells a region's map shows. <paramref name="world"/> is the region's overworld; <paramref name="hasGround"/>
    /// says whether a cell of the grid has a chunk at all (the habitats' picture knows); <paramref name="revealed"/> are
    /// the hidden places the story has revealed, by their keys: the others are left off.
    /// </summary>
    public static PixelCanvas Paint(Map world, RegionMap region, Func<int, int, bool> hasGround, IReadOnlySet<string> revealed)
    {
        const int T = CellTexels;
        var view = region.View;
        int width = view.Width * T, height = view.Height * T;
        // A margin of cells round the view is worked out too, so nothing changes at the picture's own edge
        const int Margin = 1;
        int gw = view.Width + 2 * Margin, gh = view.Height + 2 * Margin, pw = gw * T, ph = gh * T;
        int originX = (view.X - Margin) * T, originY = (view.Y - Margin) * T;

        // ---- What each cell of the grid is: ground of its own, something the map makes up, or nothing
        var kind = new byte[gw * gh];       // Empty, or the class a made-up cell is filled with
        var own = new bool[gw * gh];        // painted tile for tile
        var watery = new bool[gw * gh];     // ground of its own that is mostly water: the sea goes on through it
        var hiddenSea = new bool[gw * gh];  // a hidden place not yet revealed that is painted as sea
        var hiddenLand = new bool[gw * gh]; // and one that is painted as the land round it
        var country = new byte[gw * gh];    // snow country, a volcano's
        for (int gy = 0; gy < gh; gy++)
            for (int gx = 0; gx < gw; gx++)
            {
                int cx = view.X - Margin + gx, cy = view.Y - Margin + gy, i = gy * gw + gx;
                bool inGrid = cx >= 0 && cy >= 0 && cx < region.Columns && cy < region.Rows && cx < world.ChunkColumns && cy < world.ChunkRows;
                if (region.BlockAt(cx, cy) is { Hidden: { } hide } && !revealed.Contains(hide.Key))
                {
                    // A hidden place not yet revealed is open sea, or whatever land lies round it
                    kind[i] = hide.Sea ? Sea : Empty;
                    hiddenSea[i] = hide.Sea;
                    hiddenLand[i] = !hide.Sea;
                    continue;
                }
                if (!inGrid || !hasGround(cx, cy))
                {
                    kind[i] = Empty;
                    continue;
                }
                var (majority, share, water) = Majority(world, cx, cy);
                var area = world.AreaAt(cx * T, cy * T);
                bool open = area is { Open: true };
                // Ground of an area, or a chunk of scenery with something in it, is painted as it is; a chunk that is
                // all of one thing (sea, forest, rock, a plain lawn) is one of the cells the map shapes
                own[i] = open || share < 0.97f;
                watery[i] = own[i] && water > 0.5f;
                kind[i] = majority;
                if (open) country[i] = area!.Climate == AreaClimate.Volcanic ? Volcanic : area.Snowbound ? Snowy : (byte)0;
            }

        // ---- Nothing is sea where the edge of the map or open water can be come to from it, land otherwise
        var reached = new bool[gw * gh];
        var queue = new Queue<int>();
        void Reach(int gx, int gy)
        {
            if (gx < 0 || gy < 0 || gx >= gw || gy >= gh) return;
            int i = gy * gw + gx;
            if (reached[i] || hiddenLand[i] || !(kind[i] == Empty || (kind[i] == Sea && !own[i]) || watery[i] || hiddenSea[i])) return;
            reached[i] = true;
            queue.Enqueue(i);
        }
        for (int gx = 0; gx < gw; gx++) { Reach(gx, 0); Reach(gx, gh - 1); }
        for (int gy = 0; gy < gh; gy++) { Reach(0, gy); Reach(gw - 1, gy); }
        while (queue.Count > 0)
        {
            int i = queue.Dequeue(), gx = i % gw, gy = i / gw;
            Reach(gx - 1, gy); Reach(gx + 1, gy); Reach(gx, gy - 1); Reach(gx, gy + 1);
        }
        // An arm of that sea one cell wide, with land on three sides, is filled in again
        bool changed = true;
        while (changed)
        {
            changed = false;
            for (int gy = 0; gy < gh; gy++)
                for (int gx = 0; gx < gw; gx++)
                {
                    int i = gy * gw + gx;
                    if (kind[i] != Empty || !reached[i]) continue;
                    int land = 0;
                    foreach (var (dx, dy) in Four)
                    {
                        int nx = gx + dx, ny = gy + dy;
                        if (nx < 0 || ny < 0 || nx >= gw || ny >= gh) continue;
                        int n = ny * gw + nx;
                        bool sea = kind[n] == Empty ? reached[n] : kind[n] == Sea || watery[n];
                        if (!sea) land++;
                    }
                    if (land < 3) continue;
                    reached[i] = false;
                    changed = true;
                }
        }
        for (int i = 0; i < kind.Length; i++)
            if (kind[i] == Empty && reached[i]) kind[i] = Sea;
        // What is left of nothing is land: whatever the nearest cell with land is
        var from = new Queue<int>();
        for (int i = 0; i < kind.Length; i++)
            if (kind[i] != Empty && kind[i] != Sea) from.Enqueue(i);
        while (from.Count > 0)
        {
            int i = from.Dequeue(), gx = i % gw, gy = i / gw;
            foreach (var (dx, dy) in Four)
            {
                int nx = gx + dx, ny = gy + dy;
                if (nx < 0 || ny < 0 || nx >= gw || ny >= gh) continue;
                int n = ny * gw + nx;
                if (kind[n] != Empty) continue;
                kind[n] = Fill(kind[i]);
                from.Enqueue(n);
            }
        }
        for (int i = 0; i < kind.Length; i++)
            if (kind[i] == Empty) kind[i] = Sea;

        // ---- The country round a cell that has no area of its own is its nearest area's, within three cells
        var spread = new Queue<(int Cell, int Steps)>();
        var settled = new bool[gw * gh];
        for (int i = 0; i < kind.Length; i++)
            if (world.AreaAt((view.X - Margin + i % gw) * T, (view.Y - Margin + i / gw) * T) is { Open: true } && own[i])
            {
                settled[i] = true;
                spread.Enqueue((i, 0));
            }
        while (spread.Count > 0)
        {
            var (i, steps) = spread.Dequeue();
            if (steps == 3) continue;
            int gx = i % gw, gy = i / gw;
            for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    int nx = gx + dx, ny = gy + dy;
                    if (nx < 0 || ny < 0 || nx >= gw || ny >= gh || settled[ny * gw + nx]) continue;
                    settled[ny * gw + nx] = true;
                    country[ny * gw + nx] = country[i];
                    spread.Enqueue((ny * gw + nx, steps + 1));
                }
        }

        // ---- What each texel is, and which cell's country it is in
        var what = new byte[pw * ph];
        var where = new byte[pw * ph];
        for (int py = 0; py < ph; py++)
            for (int px = 0; px < pw; px++)
            {
                int gx = px / T, gy = py / T, cell = gy * gw + gx, i = py * pw + px;
                int tx = originX + px, ty = originY + py;
                if (own[cell])
                {
                    what[i] = TileClass(world, tx, ty);
                    where[i] = country[cell];
                    continue;
                }
                // A made-up cell: the class of the cell a point bent by noise falls in
                float bx = px + Bend(tx, ty, 11), by = py + Bend(tx, ty, 23);
                int sx = Math.Clamp((int)MathF.Floor(bx / T), 0, gw - 1), sy = Math.Clamp((int)MathF.Floor(by / T), 0, gh - 1);
                // Round a hidden place the sea stays the sea: nothing of it may show
                what[i] = hiddenSea[cell] ? Sea : Fill(kind[sy * gw + sx]);
                where[i] = country[sy * gw + sx];
            }

        // ---- Buildings: a roof over a wall
        var roofs = new Color[pw * ph];
        if (world.PlacedBuildings != null)
            foreach (var b in world.PlacedBuildings)
            {
                if (b.Forecourt) continue;
                var roof = RoofOf(b);
                for (int y = b.Y0; y <= b.Y1; y++)
                    for (int x = b.X0; x <= b.X1; x++)
                    {
                        int px = x - originX, py = y - originY;
                        if (px < 0 || py < 0 || px >= pw || py >= ph || !own[py / T * gw + px / T]) continue;
                        bool wall = y == b.Y1 && b.Y1 > b.Y0;
                        what[py * pw + px] = wall ? Wall : Roof;
                        roofs[py * pw + px] = y == b.Y0 ? PixelCanvas.Mix(roof, Color.White, 0.22f) : roof;
                    }
            }

        // ---- How far each texel of sea is from land, of mountain from its foot, of forest from its edge
        var toLand = Distances(what, pw, ph, c => c == Sea, 14);
        var toFoot = Distances(what, pw, ph, c => c == Mountain, 40);
        var toEdge = Distances(what, pw, ph, c => c is Forest or Broadleaf, 3);

        // ---- Colours
        var canvas = new PixelCanvas(width, height);
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                int px = x + Margin * T, py = y + Margin * T, i = py * pw + px;
                int tx = originX + px, ty = originY + py;
                uint h = Hash(tx, ty);
                Color c = what[i] switch
                {
                    Sea => toLand[i] <= 1 ? Shore : toLand[i] <= 4 + (int)(h & 1) ? SeaShallow : toLand[i] <= 10 + (int)(h >> 3 & 1) ? SeaMid : SeaDeep,
                    Forest or Broadleaf when where[i] == Snowy => toEdge[i] <= 1 ? FrostDark : (h & 15) == 0 ? FrostLight : (h & 15) == 1 ? FrostDark : Frost,
                    Forest => Trees(toEdge[i], h, Pine, PineDark, PineLight),
                    Broadleaf => Trees(toEdge[i], h, Leaf, LeafDark, LeafLight),
                    Mountain => Peak(toFoot[i], h, where[i] == Volcanic),
                    Lawn => (h & 15) == 0 ? LawnShade : LawnColor,
                    TallGrass => ((tx + ty) & 1) == 0 ? GrassColor : GrassDark,
                    Flowers => (h & 3) == 0 ? ((h >> 4 & 1) == 0 ? FlowerPink : FlowerGold) : LawnColor,
                    Path => PathColor,
                    Dirt => DirtColor,
                    Sand => SandColor,
                    Snow => (h & 7) == 0 ? SnowShade : SnowColor,
                    Ice => IceColor,
                    Marsh => ((tx ^ ty) & 1) == 0 ? MarshColor : GrassDark,
                    Puddle => PuddleColor,
                    Paving => (h & 7) == 0 ? PavingShade : PavingColor,
                    Walkway => WalkwayColor,
                    Wood => WoodColor,
                    Bare => where[i] == Volcanic ? ((h & 7) == 0 ? Ash[2] : Ash[4]) : (h & 7) == 0 ? Rock[3] : BareColor,
                    Dark => DarkColor,
                    Roof => roofs[i],
                    Wall => WallColor,
                    _ => LawnColor
                };
                // A building stands a little off its ground: the tile under its wall is in its shade
                if (what[i] != Roof && what[i] != Wall && py > 0 && what[i - pw] == Wall) c = PixelCanvas.Mix(c, DarkColor, 0.28f);
                // Land falls to the water on its south and east in a darker edge
                else if (what[i] != Sea && what[i] != Roof && what[i] != Wall
                    && ((py + 1 < ph && what[i + pw] == Sea) || (px + 1 < pw && what[i + 1] == Sea))) c = PixelCanvas.Mix(c, DarkColor, 0.2f);
                canvas.SetRaw(x, y, c);
            }
        return canvas;
    }

    /// <summary>The picture at half its size, each texel the mean of four: for where the map is shown small and smoothed.</summary>
    public static PixelCanvas Halved(PixelCanvas picture)
    {
        var half = new PixelCanvas(picture.Width / 2, picture.Height / 2);
        for (int y = 0; y < half.Height; y++)
            for (int x = 0; x < half.Width; x++)
            {
                Color a = picture.Get(2 * x, 2 * y), b = picture.Get(2 * x + 1, 2 * y), c = picture.Get(2 * x, 2 * y + 1), d = picture.Get(2 * x + 1, 2 * y + 1);
                half.SetRaw(x, y, new Color((a.R + b.R + c.R + d.R + 2) / 4, (a.G + b.G + c.G + d.G + 2) / 4, (a.B + b.B + c.B + d.B + 2) / 4, 255));
            }
        return half;
    }

    private static readonly (int X, int Y)[] Four = { (-1, 0), (1, 0), (0, -1), (0, 1) };

    /// <summary>What a made-up cell is filled with: the forest, the rock or the sea of the cell it follows.</summary>
    private static byte Fill(byte majority) => majority switch
    {
        Sea => Sea,
        Mountain or Bare => Mountain,
        Broadleaf => Broadleaf,
        _ => Forest
    };

    /// <summary>
    /// What most of a chunk is among forest, rock and sea, how much of it is one thing (whatever that is: a chunk of
    /// plain lawn is as much scenery as one of forest), and how much of it is water.
    /// </summary>
    private static (byte Class, float Share, float Water) Majority(Map world, int cx, int cy)
    {
        const int T = CellTexels;
        Span<int> counts = stackalloc int[Classes];
        for (int y = cy * T; y < cy * T + T; y++)
            for (int x = cx * T; x < cx * T + T; x++) counts[TileClass(world, x, y)]++;
        int sea = counts[Sea], rock = counts[Mountain], pine = counts[Forest], leaf = counts[Broadleaf];
        int most = Math.Max(Math.Max(sea, rock), pine + leaf);
        byte c = most == 0 ? Forest : most == sea ? Sea : most == rock ? Mountain : leaf > pine ? Broadleaf : Forest;
        int top = pine + leaf;
        for (int i = 0; i < Classes; i++)
            if (i != Forest && i != Broadleaf) top = Math.Max(top, counts[i]);
        return (c, top / (float)(T * T), sea / (float)(T * T));
    }

    /// <summary>What a tile of the world is on the map.</summary>
    private static byte TileClass(Map world, int x, int y)
    {
        if (!world.InBounds(x, y)) return Sea;
        if (world.DeckAt(x, y) != null) return Wood;
        bool solid = world.IsSolid(x, y);
        return world.GetGroundTile(x, y) switch
        {
            TileType.Water => Sea,
            TileType.Tree or TileType.TreeTrunk or TileType.ForestMouth => world.TreesAt(x, y) == TreeStyle.Round ? Broadleaf : Forest,
            TileType.Rock => solid ? Mountain : Bare,
            TileType.CaveWall => Mountain,
            TileType.CaveMouth => Dark,
            TileType.CaveFloor => Bare,
            TileType.TallGrass => TallGrass,
            TileType.FlowerGrass => Flowers,
            TileType.Path or TileType.Signpost => Path,
            TileType.Dirt => Dirt,
            TileType.Sand or TileType.Kelp => Sand,
            TileType.Snow => Snow,
            TileType.Ice => Ice,
            TileType.Marsh => Marsh,
            TileType.Puddle => Puddle,
            TileType.Paving or TileType.Floor or TileType.Door or TileType.PC => Paving,
            TileType.Walkway => Walkway,
            TileType.Planks or TileType.Stairs => Wood,
            TileType.RoofRed or TileType.RoofBlue or TileType.RoofGreen or TileType.Wall => Paving,
            _ => Lawn
        };
    }

    private static Color Trees(byte toEdge, uint h, Color body, Color dark, Color light) =>
        toEdge <= 1 ? dark : (h & 7) == 0 ? light : (h & 7) == 1 ? dark : body;

    /// <summary>
    /// A mountain's colour by how far in from its foot a texel is: four bands of rock and snow on the heights, or,
    /// round a volcano, bands of ash and a dark crown with embers in it.
    /// </summary>
    private static Color Peak(byte toFoot, uint h, bool volcanic)
    {
        int jitter = (int)(h & 1);
        if (toFoot >= 24 + jitter)
            return volcanic ? ((h & 31) == 0 ? PeakGlow : PeakAsh) : (h & 7) == 0 ? PeakShade : PeakSnow;
        int band = toFoot <= 2 ? 0 : toFoot <= 6 + jitter ? 1 : toFoot <= 12 + jitter ? 2 : toFoot <= 18 + jitter ? 3 : 4;
        var bands = volcanic ? Ash : Rock;
        return (h & 15) == 0 && band > 0 ? bands[band - 1] : bands[band];
    }

    private static Color RoofOf(BuildingInfo b) => b.Kind switch
    {
        BuildingKind.PokemonCenter => new Color(226, 84, 78, 255),
        BuildingKind.PokeMart => new Color(70, 128, 220, 255),
        BuildingKind.Gym => new Color(236, 172, 60, 255),
        BuildingKind.Lab or BuildingKind.Office or BuildingKind.TvStation or BuildingKind.Terminal or BuildingKind.Factory or BuildingKind.Warehouse
            => new Color(132, 150, 176, 255),
        BuildingKind.Galactic => new Color(104, 112, 150, 255),
        BuildingKind.Gate => new Color(110, 160, 150, 255),
        BuildingKind.League or BuildingKind.Temple or BuildingKind.Shrine or BuildingKind.Tower or BuildingKind.Lighthouse or BuildingKind.Chapel
            => new Color(214, 204, 186, 255),
        _ => b.RoofTile switch
        {
            TileType.RoofBlue => new Color(98, 138, 206, 255),
            TileType.RoofGreen => new Color(96, 164, 118, 255),
            _ => new Color(204, 116, 88, 255)
        }
    };

    /// <summary>
    /// For every texel that <paramref name="inside"/> holds of, how far it is to the nearest that it doesn't, up to
    /// <paramref name="limit"/>, counted in steps along the grid and its diagonals (two sweeps).
    /// </summary>
    private static byte[] Distances(byte[] what, int w, int h, Func<byte, bool> inside, int limit)
    {
        var d = new byte[w * h];
        byte far = (byte)Math.Min(limit, 250);
        for (int i = 0; i < d.Length; i++) d[i] = inside(what[i]) ? far : (byte)0;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int i = y * w + x;
                if (d[i] == 0) continue;
                int best = d[i];
                if (x > 0) best = Math.Min(best, d[i - 1] + 1);
                if (y > 0) best = Math.Min(best, d[i - w] + 1);
                if (x > 0 && y > 0) best = Math.Min(best, d[i - w - 1] + 1);
                if (x + 1 < w && y > 0) best = Math.Min(best, d[i - w + 1] + 1);
                d[i] = (byte)best;
            }
        for (int y = h - 1; y >= 0; y--)
            for (int x = w - 1; x >= 0; x--)
            {
                int i = y * w + x;
                if (d[i] == 0) continue;
                int best = d[i];
                if (x + 1 < w) best = Math.Min(best, d[i + 1] + 1);
                if (y + 1 < h) best = Math.Min(best, d[i + w] + 1);
                if (x + 1 < w && y + 1 < h) best = Math.Min(best, d[i + w + 1] + 1);
                if (x > 0 && y + 1 < h) best = Math.Min(best, d[i + w - 1] + 1);
                d[i] = (byte)best;
            }
        return d;
    }

    /// <summary>How far a point is pushed along one axis, in texels: two octaves of smooth noise, the same for a tile every time.</summary>
    private static float Bend(int x, int y, int seed) =>
        (Noise(x / 38f, y / 38f, seed) - 0.5f) * 26f + (Noise(x / 11f, y / 11f, seed + 7) - 0.5f) * 8f;

    private static float Noise(float x, float y, int seed)
    {
        int x0 = (int)MathF.Floor(x), y0 = (int)MathF.Floor(y);
        float fx = x - x0, fy = y - y0;
        fx = fx * fx * (3f - 2f * fx);
        fy = fy * fy * (3f - 2f * fy);
        float a = Unit(x0, y0, seed), b = Unit(x0 + 1, y0, seed), c = Unit(x0, y0 + 1, seed), d = Unit(x0 + 1, y0 + 1, seed);
        return a + (b - a) * fx + (c - a) * fy + (a - b - c + d) * fx * fy;
    }

    private static float Unit(int x, int y, int seed) => (Hash(x + seed * 7919, y - seed * 104729) & 0xFFFF) / 65535f;

    /// <summary>A number that is a tile's own: the same on every run and every machine.</summary>
    private static uint Hash(int x, int y)
    {
        unchecked
        {
            uint h = (uint)x * 0x9E3779B1u ^ (uint)y * 0x85EBCA77u;
            h ^= h >> 15;
            h *= 0x2C1B3C6Du;
            h ^= h >> 12;
            h *= 0x297A2D39u;
            h ^= h >> 15;
            return h;
        }
    }
}
