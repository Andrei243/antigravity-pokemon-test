using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using Raylib_cs;

namespace ContentCheck;

/// <summary>Something marked on a plan: its tag on the picture, what kind of thing it is, what the key says of it, and its tiles.</summary>
public sealed record PlanMark(string Tag, string Kind, string Text, int X, int Y, int Width = 1, int Depth = 1);

/// <summary>
/// A room or an open area drawn from above with everything that stands on it marked and named: the picture, and the
/// key that says what each mark is. <see cref="PixelOf"/> says where a tile is on the picture.
/// </summary>
public sealed class Plan
{
    public required string Name { get; init; }
    public required PixelCanvas Picture { get; init; }
    public required IReadOnlyList<PlanMark> Marks { get; init; }

    /// <summary>How many pixels a tile takes, and where the plan's first tile (<see cref="X"/>, <see cref="Y"/>) is on the picture.</summary>
    public required int Tile { get; init; }
    public required int X { get; init; }
    public required int Y { get; init; }
    public required int Left { get; init; }
    public required int Top { get; init; }

    /// <summary>The key, a line to a mark: what the tool prints beside the picture's name.</summary>
    public IEnumerable<string> Legend => Marks.Select(m => $"{m.Tag,-4} {m.Text}");

    /// <summary>The picture's pixel at the north-west corner of a tile of the map.</summary>
    public (int X, int Y) PixelOf(int tileX, int tileY) => (Left + (tileX - X) * Tile, Top + (tileY - Y) * Tile);

    public void Save(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        File.WriteAllBytes(path, PngWriter.Encode(Picture));
    }
}

/// <summary>
/// Plans for looking at a room or an open area without the game's window (plan 16 · T8): the ground as the field
/// bakes it (<see cref="PixelGround"/>, on the CPU), a grid with the tiles' numbers along its edges, the tiles that
/// block hatched, and over it the props, buildings, warps, people, signs, triggers, hidden items and tiles read,
/// each with a tag the key under the picture explains. An area is drawn from its own chunks, a chunk at a time:
/// never the map of Sinnoh whole.
/// </summary>
public static class Plans
{
    public static readonly Color PropColor = new(240, 140, 30, 255), BuildingColor = new(150, 96, 56, 255), WarpColor = new(228, 52, 200, 255),
        PersonColor = new(44, 104, 232, 255), TrainerColor = new(222, 44, 44, 255), PokemonColor = new(40, 170, 90, 255), ThingColor = new(250, 250, 250, 255),
        SignColor = new(20, 190, 214, 255), TriggerColor = new(255, 226, 40, 255), ReadColor = new(150, 110, 240, 255);

    private static readonly Color Ink = new(18, 20, 34, 255), Paper = new(244, 242, 234, 255), Faint = new(18, 20, 34, 40), Hatch = new(18, 20, 34, 96),
        WallColor = new(150, 146, 162, 255);

    /// <summary>The longest side, in pixels, a plan's map is fitted to unless a tile's size is asked for: what a reader sees whole.</summary>
    public const int FitTo = 1408;

    /// <summary>
    /// The plans of a room (by its map's name) or of an open area (by its key); <c>name@x,y,w,h</c> for a rectangle of
    /// its tiles alone. An area of more than one chunk gets a plan of the whole of it and one of each chunk.
    /// </summary>
    public static List<Plan> Of(GameContent content, string name, int? tile = null)
    {
        (int X, int Y, int W, int H)? only = null;
        if (name.IndexOf('@') is > 0 and var at)
        {
            int[] numbers = name[(at + 1)..].Split(',').Select(int.Parse).ToArray();
            if (numbers.Length != 4) throw new ArgumentException("a rectangle of tiles is written name@x,y,width,height");
            only = (numbers[0], numbers[1], numbers[2], numbers[3]);
            name = name[..at];
        }

        // An open area first: a key of the world's
        foreach (var region in content.Regions)
        {
            if (!region.World.IsOpen(name) || region.World.MapOf(name) is not { } entry) continue;
            var map = region.Maps?.FirstOrDefault(m => m.Name == entry.Name) ?? throw new ArgumentException("its world would not load: run the check first");
            var bounds = map.AreaBounds(name) ?? throw new ArgumentException("no chunk of its map is the area's");
            bool Belongs(int x, int y) => map.AreaAt(x, y)?.Key is { } key && string.Equals(key, name, StringComparison.OrdinalIgnoreCase);
            if (only is { } part) return new() { Draw(map, $"{name}@{part.X},{part.Y},{part.W},{part.H}", part.X, part.Y, part.W, part.H, tile, Belongs, name) };

            var plans = new List<Plan> { Draw(map, name, bounds.X, bounds.Y, bounds.Width, bounds.Height, tile, Belongs, name) };
            if (bounds.Width > Map.ChunkTiles || bounds.Height > Map.ChunkTiles)
                for (int y = bounds.Y; y < bounds.Y + bounds.Height; y += Map.ChunkTiles)
                    for (int x = bounds.X; x < bounds.X + bounds.Width; x += Map.ChunkTiles)
                        if (Belongs(x, y))
                            plans.Add(Draw(map, $"{name}_{x / Map.ChunkTiles}_{y / Map.ChunkTiles}", x, y, Map.ChunkTiles, Map.ChunkTiles, tile, Belongs, name));
            return plans;
        }

        if (content.MapNamed(name) is not { } room) throw new ArgumentException("is no open area and no map");
        if (room.IsStreamed && only == null && room.Width * room.Height > 4 * Map.ChunkTiles * Map.ChunkTiles)
            throw new ArgumentException($"is a map of {room.Width} by {room.Height} tiles: name one of its areas, or a rectangle of it ({name}@x,y,width,height)");
        return only is { } rectangle
            ? new() { Draw(room, $"{name}@{rectangle.X},{rectangle.Y},{rectangle.W},{rectangle.H}", rectangle.X, rectangle.Y, rectangle.W, rectangle.H, tile, null, null) }
            : new() { Draw(room, name, 0, 0, room.Width, room.Height, tile, null, null) };
    }

    /// <summary>A room's plan: the whole map with a tile of margin round it.</summary>
    public static Plan OfRoom(Map map, int? tile = null) => Draw(map, map.Name, 0, 0, map.Width, map.Height, tile, null, null);

    /// <summary>The ground under a rectangle of a map's tiles at 32 pixels a tile, a tile of margin round it.</summary>
    private static PixelCanvas Ground(Map map, int x0, int y0, int width, int height)
    {
        const int T = GroundBaker.ArtTile, margin = 1;
        // A room is baked whole, as the plan of plan 16 has it; anything else a block of chunks at a time, each with
        // the tiles round it that shape its edge, so a town never asks for more than its own ground
        if (!map.IsStreamed && x0 == 0 && y0 == 0 && width == map.Width && height == map.Height)
            return PixelGround.Bake(map, margin, MapStructures.BuildingsOf(map));

        var ground = new PixelCanvas((width + margin * 2) * T, (height + margin * 2) * T);
        for (int by = y0 - margin; by < y0 + height + margin; by += Map.ChunkTiles)
            for (int bx = x0 - margin; bx < x0 + width + margin; bx += Map.ChunkTiles)
            {
                int w = Math.Min(Map.ChunkTiles, x0 + width + margin - bx), h = Math.Min(Map.ChunkTiles, y0 + height + margin - by);
                var block = PixelGround.Bake(map, new TileWindow(bx, by, w, h), PixelGround.ChunkPad, worldSeeds: true, out _);
                ground.Blit(block, (bx - (x0 - margin)) * T, (by - (y0 - margin)) * T);
            }
        return ground;
    }

    private static Plan Draw(Map map, string name, int x0, int y0, int width, int height, int? tileSize, Func<int, int, bool>? belongs, string? area)
    {
        if (width <= 0 || height <= 0) throw new ArgumentException("a plan needs at least one tile");
        if ((long)width * height > 4 * Map.ChunkTiles * Map.ChunkTiles * 4)
            throw new ArgumentException($"{width} by {height} tiles is more than a plan draws at once: name a rectangle of it");
        const int T = GroundBaker.ArtTile, margin = 1;
        int tilesWide = width + margin * 2, tilesHigh = height + margin * 2;
        int tile = tileSize ?? new[] { 96, 64, 48, 32, 16, 8 }.FirstOrDefault(size => Math.Max(tilesWide, tilesHigh) * size <= FitTo, 8);
        if (tile is not (8 or 16 or 32 or 48 or 64 or 96)) throw new ArgumentException("a tile is drawn 8, 16, 32, 48, 64 or 96 pixels wide");
        bool Inside(int x, int y) => x >= x0 && y >= y0 && x < x0 + width && y < y0 + height;
        bool Mine(int x, int y) => Inside(x, y) && (belongs == null || belongs(x, y));

        var ground = Ground(map, x0, y0, width, height);

        // ---- what stands there, each with its tag
        var marks = new List<PlanMark>();
        string Tag(string letter) => letter + (marks.Count(m => m.Tag.StartsWith(letter, StringComparison.Ordinal) && char.IsDigit(m.Tag[letter.Length])) + 1);
        foreach (var building in map.PlacedBuildings ?? (map.IsIndoors ? new List<BuildingInfo>() : MapStructures.BuildingsOf(map).ToList()))
            if (Mine(building.X0, building.Y1) || Mine(building.X1, building.Y1) || Mine(building.X0, building.Y0) || Mine(building.X1, building.Y0))
                marks.Add(new PlanMark(Tag("B"), "building", $"{building.Kind} {building.X0},{building.Y0} to {building.X1},{building.Y1}"
                    + (building.Model.Length > 0 ? $" ({building.Model})" : "") + (building.Sign is { Length: > 0 } sign ? $" \"{sign}\"" : ""),
                    building.X0, building.Y0, building.X1 - building.X0 + 1, building.Y1 - building.Y0 + 1));
        foreach (var prop in map.Props.OrderBy(p => p.Y).ThenBy(p => p.X))
            if (Mine(prop.X, prop.Y))
                marks.Add(new PlanMark(Tag("P"), "prop", $"{prop.Type} {prop.X},{prop.Y}" + (prop.Width > 1 || prop.Depth > 1 ? $" {prop.Width}x{prop.Depth}" : "")
                    + (prop.Model.Length > 0 ? $" ({prop.Model})" : ""), prop.X, prop.Y, prop.Width, prop.Depth));
        foreach (var warp in map.Warps.OrderBy(w => w.SourceY).ThenBy(w => w.SourceX))
            if (Mine(warp.SourceX, warp.SourceY))
                marks.Add(new PlanMark(Tag("W"), "warp", $"warp {warp.SourceX},{warp.SourceY} -> {warp.TargetMap} {warp.TargetX},{warp.TargetY} facing {warp.TargetFacing}"
                    + (warp.OpenedBy != null ? $", opened by {warp.OpenedBy}" : "") + (warp.Until != null ? $", until {warp.Until}" : "")
                    + (warp.From != null ? $", from {warp.From}" : "") + (warp.CyclistsOnly ? ", cyclists only" : ""), warp.SourceX, warp.SourceY));
        foreach (var npc in map.Everyone.OrderBy(n => n.GridY).ThenBy(n => n.GridX))
        {
            if (!Mine(npc.GridX, npc.GridY) || area != null && npc.ScriptFile != null && !string.Equals(npc.ScriptFile, area, StringComparison.OrdinalIgnoreCase)) continue;
            string kind = npc.IsTrainer ? "trainer" : npc.IsPokemon ? "pokemon" : npc.IsThing ? "thing" : "person";
            string what = npc.IsItemBall ? $"item {npc.Item}" + (npc.ItemCount > 1 ? $" x{npc.ItemCount}" : "")
                : npc.IsObstacle ? npc.Obstacle.ToString()!
                : npc.IsBerrySoil ? $"berry patch {npc.BerryPatch}"
                : npc.IsApricornTree ? $"Apricorn tree {npc.ApricornColour}"
                : $"{npc.Key ?? npc.Name} \"{npc.Name}\" {npc.NpcType}" + (npc.IsPokemon ? $" {npc.Species}" : "");
            marks.Add(new PlanMark(Tag("N"), kind, $"{what} {npc.GridX},{npc.GridY}" + (npc.IsThing ? "" : $" facing {npc.Facing}")
                + (npc.IsTrainer && npc.TrainerData != null ? $", trainer {npc.TrainerData.Id} sight {npc.TrainerData.SightRange}" : "")
                + (FieldScripts.For(npc) is { } script && !npc.IsThing ? $", script {script}" : "")
                + (npc.Level is { } level ? $", level {level}" : "")
                + (npc.HiddenBy is { Length: > 0 } hidden && !npc.IsItemBall ? $", hidden by {hidden}" : "") + (npc.ShownBy != null ? $", shown by {npc.ShownBy}" : ""), npc.GridX, npc.GridY));
        }
        foreach (var ((x, y), text) in map.Signboards.OrderBy(s => s.Key.Y).ThenBy(s => s.Key.X))
            if (Mine(x, y))
                marks.Add(new PlanMark(Tag("S"), "sign", $"sign {x},{y} \"{Short(text)}\"" + (map.SignScripts.GetValueOrDefault((x, y)) is { } script ? $", script {script}" : ""), x, y));
        foreach (var trigger in map.Triggers.OrderBy(t => t.Y).ThenBy(t => t.X))
            if (Mine(trigger.X, trigger.Y) && (area == null || trigger.ScriptFile == null || string.Equals(trigger.ScriptFile, area, StringComparison.OrdinalIgnoreCase)))
                marks.Add(new PlanMark(Tag("T"), "trigger", $"trigger {trigger.X},{trigger.Y}" + (trigger.Width > 1 || trigger.Depth > 1 ? $" {trigger.Width}x{trigger.Depth}" : "")
                    + $" script {trigger.Script}" + (trigger.Variable != null ? $" while {trigger.Variable} = {trigger.Value}" : ""), trigger.X, trigger.Y, trigger.Width, trigger.Depth));
        foreach (var ((x, y), hidden) in map.HiddenItems.OrderBy(h => h.Key.Y).ThenBy(h => h.Key.X))
            if (Mine(x, y)) marks.Add(new PlanMark(Tag("H"), "hidden", $"hidden {hidden.Item}" + (hidden.Count > 1 ? $" x{hidden.Count}" : "") + $" {x},{y} ({hidden.Flag})", x, y));
        foreach (var ((x, y), script) in map.TileScripts.OrderBy(r => r.Key.Y).ThenBy(r => r.Key.X))
            if (Mine(x, y)) marks.Add(new PlanMark(Tag("R"), "read", $"read {x},{y} script {script}", x, y));
        foreach (var ((x, y), script) in map.BoulderPits.OrderBy(r => r.Key.Y).ThenBy(r => r.Key.X))
            if (Mine(x, y)) marks.Add(new PlanMark(Tag("R"), "read", $"pit {x},{y} script {script}", x, y));

        // ---- the picture: a title, the tiles' numbers along the top and the left, the map, and the key under it
        int textScale = 2, row = PlanFont.Height * textScale + 6;
        int ruler = PlanFont.Width("000", 1) + 6, title = row + 6;
        int mapW = tilesWide * tile, mapH = tilesHigh * tile;
        // The key: each kind of mark (by its tag's letter) in as many columns as its longest line lets the picture
        // hold, so a room's sixty hedges take a quarter of the height
        int character = PlanFont.Width("00", textScale) - PlanFont.Width("0", textScale);
        int longest = marks.Count == 0 ? 0 : marks.Max(m => m.Text.Length + 5);
        // (as wide as the map, the title or the key's longest line, whichever is widest)
        int pictureW = Math.Max(Math.Max(ruler + mapW + 8, PlanFont.Width(name, textScale) + 400), Math.Min(longest * character + 40, 2400));
        var legend = new List<(PlanMark Mark, int X, int Y)>();
        int legendTop = title + ruler + mapH + 10, legendHeight = 0;
        foreach (var group in marks.GroupBy(m => m.Tag[0]))
        {
            var lines = group.ToList();
            int widest = (lines.Max(m => m.Text.Length) + 7) * character;
            int columns = Math.Clamp((pictureW - 16) / widest, 1, 6), perColumn = (lines.Count + columns - 1) / columns;
            for (int i = 0; i < lines.Count; i++) legend.Add((lines[i], 8 + i / perColumn * widest, legendHeight + i % perColumn * row));
            legendHeight += perColumn * row;
        }
        var picture = new PixelCanvas(pictureW, legendTop + legendHeight + 10);
        picture.Fill(Paper);
        int left = ruler, top = title + ruler;
        string size = belongs == null ? $"{width} by {height} tiles" : $"tiles {x0},{y0} to {x0 + width - 1},{y0 + height - 1}";
        PlanFont.Text(picture, 8, 6, $"{name}   {size}   {tile} px a tile", Ink, textScale);

        // The ground, a tile of it to `tile` pixels: the nearest texel where it is drawn larger, the mean where smaller
        for (int py = 0; py < mapH; py++)
            for (int px = 0; px < mapW; px++)
            {
                Color c;
                if (tile >= T) c = ground.Get(px * T / tile, py * T / tile);
                else
                {
                    int n = T / tile, r = 0, g = 0, b = 0;
                    for (int sy = 0; sy < n; sy++)
                        for (int sx = 0; sx < n; sx++)
                        {
                            var s = ground.Get(px * n + sx, py * n + sy);
                            (r, g, b) = (r + s.R, g + s.G, b + s.B);
                        }
                    c = new Color(r / (n * n), g / (n * n), b / (n * n), 255);
                }
                picture.SetRaw(left + px, top + py, c.A == 0 ? Paper : c with { A = 255 });
            }

        (int X, int Y) At(int x, int y) => (left + (x - x0 + margin) * tile, top + (y - y0 + margin) * tile);

        // What blocks, hatched; what isn't the area's, greyed; and the grid. A room's walls and the nothing round its
        // floor are the ground baker's lawn (it bakes the field's ground, and a room's walls are built over it): on a
        // plan they are wall and paper
        for (int y = y0 - margin; y < y0 + height + margin; y++)
            for (int x = x0 - margin; x < x0 + width + margin; x++)
            {
                var (px, py) = At(x, y);
                bool on = map.InBounds(x, y);
                if (map.IsIndoors && !on) picture.Rect(px, py, tile, tile, Paper);
                else if (map.IsIndoors && map.GetGroundTile(x, y) is TileType.Wall or TileType.Void)
                    picture.Rect(px, py, tile, tile, map.GetGroundTile(x, y) == TileType.Void ? Paper : WallColor);
                if (!Mine(x, y) && belongs != null || !on) picture.Rect(px, py, tile, tile, new Color(244, 242, 234, 150));
                else if (map.IsSolid(x, y))
                    for (int d = 0; d < tile * 2; d += Math.Max(4, tile / 6))
                        for (int k = 0; k < tile; k++)
                            if (d - k >= 0 && d - k < tile) picture.Set(px + k, py + d - k, Hatch);
            }
        for (int x = 0; x <= tilesWide; x++) picture.Rect(left + x * tile, top, 1, mapH, Faint);
        for (int y = 0; y <= tilesHigh; y++) picture.Rect(left, top + y * tile, mapW, 1, Faint);
        int every = tile >= 32 ? 1 : tile >= 16 ? 2 : 4;
        for (int x = x0; x < x0 + width; x++)
            if (x % every == 0)
            {
                string number = x.ToString();
                PlanFont.Text(picture, At(x, y0).X + (tile - PlanFont.Width(number, 1)) / 2, top - PlanFont.Height - 3, number, Ink, 1);
            }
        for (int y = y0; y < y0 + height; y++)
            if (y % every == 0)
            {
                string number = y.ToString();
                PlanFont.Text(picture, left - PlanFont.Width(number, 1) - 3, At(x0, y).Y + (tile - PlanFont.Height) / 2, number, Ink, 1);
            }

        // The marks: a frame round what covers tiles, a disc for who stands on one, and the tag in a corner
        // (what covers ground first, then what frames tiles, then who stands on them)
        int tagScale = tile >= 48 ? 2 : 1;
        static int Layer(string kind) => kind switch { "building" => 0, "trigger" => 1, "prop" => 2, "warp" => 3, "sign" or "hidden" or "read" => 4, _ => 5 };
        foreach (var mark in marks.OrderBy(m => Layer(m.Kind)))
        {
            var (px, py) = At(mark.X, mark.Y);
            int w = mark.Width * tile, h = mark.Depth * tile;
            var color = ColorOf(mark.Kind);
            switch (mark.Kind)
            {
                case "prop" or "building":
                    picture.Rect(px, py, w, h, color with { A = (byte)(mark.Kind == "prop" ? 70 : 40) });
                    Frame(picture, px, py, w, h, color, Math.Max(2, tile / 16));
                    break;
                case "trigger":
                    picture.Rect(px, py, w, h, color with { A = 80 });
                    Dashes(picture, px, py, w, h, Ink, Math.Max(1, tile / 24));
                    break;
                case "warp":
                {
                    // Inside the tile's edge, so the frame of a mat it lies on still shows round it
                    int inset = Math.Max(2, tile / 8);
                    picture.Rect(px + inset, py + inset, w - inset * 2, h - inset * 2, color with { A = 110 });
                    Frame(picture, px + inset, py + inset, w - inset * 2, h - inset * 2, color, Math.Max(2, tile / 12));
                    break;
                }
                case "sign" or "hidden" or "read":
                    Frame(picture, px + tile / 6, py + tile / 6, tile - tile / 3, tile - tile / 3, color, Math.Max(2, tile / 12));
                    break;
                default:
                {
                    picture.Disc(px + tile / 2f, py + tile / 2f, tile * 0.34f, Ink);
                    picture.Disc(px + tile / 2f, py + tile / 2f, tile * 0.34f - Math.Max(1, tile / 16), color);
                    // Which way they look: a dot of ink on that side of the disc
                    if (mark.Kind == "thing" || map.Everyone.FirstOrDefault(n => (n.GridX, n.GridY) == (mark.X, mark.Y)) is not { } who) break;
                    var (dx, dy) = who.Facing switch { Direction.Up => (0, -1), Direction.Left => (-1, 0), Direction.Right => (1, 0), _ => (0, 1) };
                    picture.Disc(px + tile / 2f + dx * tile * 0.2f, py + tile / 2f + dy * tile * 0.2f, Math.Max(1.5f, tile * 0.07f), Ink);
                    break;
                }
            }
        }
        // Tags last, over everything: dark on a light plate at the mark's north-west corner
        var tagged = new Dictionary<(int, int), int>();
        foreach (var mark in marks)
        {
            var (px, py) = At(mark.X, mark.Y);
            int w = PlanFont.Width(mark.Tag, tagScale) + 4, h = PlanFont.Height * tagScale + 4;
            // Where two things share a tile (someone on a cushion, a way out on a mat) the second tag goes under the first
            int above = tagged.GetValueOrDefault((mark.X, mark.Y));
            tagged[(mark.X, mark.Y)] = above + 1;
            py += above * (h + 1);
            picture.Rect(px + 1, py + 1, w, h, ColorOf(mark.Kind));
            Frame(picture, px + 1, py + 1, w, h, Ink, 1);
            PlanFont.Text(picture, px + 3, py + 3, mark.Tag, mark.Kind is "trigger" or "thing" or "sign" ? Ink : new Color(255, 255, 255, 255), tagScale);
        }

        foreach (var (mark, x, down) in legend)
        {
            int y = legendTop + down;
            picture.Rect(x, y + 1, 10, PlanFont.Height * textScale, ColorOf(mark.Kind));
            Frame(picture, x, y + 1, 10, PlanFont.Height * textScale, Ink, 1);
            PlanFont.Text(picture, x + 16, y + 1, $"{mark.Tag,-4} {mark.Text}", Ink, textScale);
        }

        return new Plan { Name = name, Picture = picture, Marks = marks, Tile = tile, X = x0, Y = y0, Left = left + margin * tile, Top = top + margin * tile };
    }

    public static Color ColorOf(string kind) => kind switch
    {
        "prop" => PropColor,
        "building" => BuildingColor,
        "warp" => WarpColor,
        "trainer" => TrainerColor,
        "pokemon" => PokemonColor,
        "thing" => ThingColor,
        "sign" => SignColor,
        "trigger" => TriggerColor,
        "hidden" or "read" => ReadColor,
        _ => PersonColor
    };

    private static void Frame(PixelCanvas c, int x, int y, int w, int h, Color color, int thickness)
    {
        c.Rect(x, y, w, thickness, color);
        c.Rect(x, y + h - thickness, w, thickness, color);
        c.Rect(x, y, thickness, h, color);
        c.Rect(x + w - thickness, y, thickness, h, color);
    }

    private static void Dashes(PixelCanvas c, int x, int y, int w, int h, Color color, int thickness)
    {
        for (int i = 0; i < w; i += 8)
        {
            c.Rect(x + i, y, Math.Min(4, w - i), thickness, color);
            c.Rect(x + i, y + h - thickness, Math.Min(4, w - i), thickness, color);
        }
        for (int i = 0; i < h; i += 8)
        {
            c.Rect(x, y + i, thickness, Math.Min(4, h - i), color);
            c.Rect(x + w - thickness, y + i, thickness, Math.Min(4, h - i), color);
        }
    }

    private static string Short(string text)
    {
        text = text.Replace('\n', ' ');
        return text.Length <= 28 ? text : text[..25] + "...";
    }
}

/// <summary>
/// The lettering of a plan: the field's own five-by-seven capitals (<see cref="Pix.Glyph"/>) with the figures and the
/// signs a key needs, which a signboard never did. Small letters are written as capitals.
/// </summary>
public static class PlanFont
{
    public const int Height = 7;

    private static readonly Dictionary<char, string> More = new()
    {
        ['0'] = ".XXX.X...XX..XXX.X.XXX..XX...X.XXX.", ['1'] = "..X...XX....X....X....X....X...XXX.", ['2'] = ".XXX.X...X....X...X...X...X...XXXXX",
        ['3'] = "XXXXX...X...X.....X.....XX...X.XXX.", ['4'] = "...X...XX..X.X.X..X.XXXXX...X....X.", ['5'] = "XXXXXX....XXXX.....X....XX...X.XXX.",
        ['6'] = "..XX..X...X....XXXX.X...XX...X.XXX.", ['7'] = "XXXXX....X...X...X...X....X....X...", ['8'] = ".XXX.X...XX...X.XXX.X...XX...X.XXX.",
        ['9'] = ".XXX.X...XX...X.XXXX....X...X..XX..",
        ['.'] = ".........................XX...XX...", [','] = "....................XX....X...X....", [':'] = "......XX...XX........XX...XX.......",
        ['-'] = "...............XXXXX...............", ['_'] = "..............................XXXXX", ['('] = "...X...X...X....X....X.....X.....X.",
        [')'] = ".X.....X.....X....X....X...X...X...", ['/'] = "....X....X...X...X...X...X....X....", ['>'] = "X.....X.....X.....X...X...X...X....",
        ['<'] = "...X...X...X...X.....X.....X.....X.", ['='] = "..........XXXXX.....XXXXX..........", ['\''] = "..X....X...X.......................",
        ['"'] = ".X.X..X.X..........................", ['+'] = ".......X....X..XXXXX..X....X.......", ['#'] = ".X.X..X.X.XXXXX.X.X.XXXXX.X.X..X.X.",
        ['!'] = "..X....X....X....X....X.........X..", ['?'] = ".XXX.X...X....X...X...X.........X..", ['['] = ".XXX..X....X....X....X....X....XXX.",
        [']'] = ".XXX....X....X....X....X....X..XXX.", ['*'] = ".....X.X.X.XXX.XXXXX.XXX.X.X.X.....", ['@'] = ".XXX.X...XX.XXXX.X.XX.XXXX.....XXX.",
        ['{'] = "...X...X...X....X....X.....X.....X.", ['}'] = ".X.....X.....X....X....X...X...X...", ['&'] = ".XX..X..X.X.X...X...X.X.XX..X..XX.X",
        ['%'] = "XX..XXX..X...X...X...X...X..XXX..XX", [';'] = "......XX...XX........XX....X...X..."
    };

    static PlanFont()
    {
        foreach (var (letter, rows) in More)
            if (rows.Length != 35) throw new InvalidOperationException($"The plan's '{letter}' is {rows.Length} cells, not seven rows of five.");
    }

    private static string? Glyph(char letter) => letter switch
    {
        'é' or 'É' or 'è' or 'ê' => Pix.Glyph('E'),
        _ => More.GetValueOrDefault(letter) ?? Pix.Glyph(letter)
    };

    public static int Width(string text, int scale) => text.Length == 0 ? 0 : (text.Length * 6 - 1) * scale;

    /// <summary>Writes a text with its top left at a pixel; a character the lettering lacks leaves a gap.</summary>
    public static void Text(PixelCanvas canvas, int x, int y, string text, Color color, int scale = 1)
    {
        for (int i = 0; i < text.Length; i++)
        {
            if (Glyph(text[i]) is not { } rows) continue;
            for (int gy = 0; gy < 7; gy++)
                for (int gx = 0; gx < 5; gx++)
                    if (rows[gy * 5 + gx] == 'X') canvas.Rect(x + (i * 6 + gx) * scale, y + gy * scale, scale, scale, color);
        }
    }
}
