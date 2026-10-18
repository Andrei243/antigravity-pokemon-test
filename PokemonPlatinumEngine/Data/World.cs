using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Data;

/// <summary>
/// The imported world of one region, read from <c>Data/world/&lt;region&gt;/</c>: the index of what is built
/// (<c>world.json</c>), the matrices, chunks and areas <c>tools/MapImporter</c> wrote, and the overlays written by
/// hand. It makes the game's maps from them (<see cref="BuildMaps"/>). Chunks that have no file yet are forest.
/// </summary>
public sealed class World
{
    public const string Folder = "world";
    public const string IndexFile = "world.json";

    public WorldIndexFile Index { get; }

    private readonly string folder;
    private readonly Dictionary<int, WorldMatrixFile> matrices = new();
    private readonly Dictionary<int, WorldChunkFile?> chunks = new();
    private readonly Dictionary<string, WorldAreaFile?> areas = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, WorldOverlayFile?> overlays = new(StringComparer.OrdinalIgnoreCase);

    private World(string folder, WorldIndexFile index)
    {
        this.folder = folder;
        Index = index;
    }

    /// <summary>Every region that has a world folder, in name order.</summary>
    public static List<World> LoadAll()
    {
        var worlds = new List<World>();
        string root = GameDataFiles.PathOf(Folder);
        if (!Directory.Exists(root)) return worlds;
        foreach (string dir in Directory.GetDirectories(root).OrderBy(d => d, StringComparer.Ordinal))
        {
            string relative = Path.Combine(Folder, Path.GetFileName(dir));
            if (File.Exists(GameDataFiles.PathOf(Path.Combine(relative, IndexFile))))
                worlds.Add(new World(relative, GameDataFiles.Load<WorldIndexFile>(Path.Combine(relative, IndexFile))));
        }
        return worlds;
    }

    /// <summary>
    /// A world read from any folder that has the layout of <c>Data/world/&lt;region&gt;/</c>, with the index given
    /// instead of read: how tools look at an import the game doesn't ship (the importer's whole output).
    /// </summary>
    public static World Open(string folder, WorldIndexFile index) => new(Path.GetFullPath(folder), index);

    private T? Optional<T>(string file) where T : class
    {
        string relative = Path.Combine(folder, file);
        return File.Exists(GameDataFiles.PathOf(relative)) ? GameDataFiles.Load<T>(relative) : null;
    }

    // The files read so far are kept for everyone who asks: tests and the chunks' workers ask from several threads at
    // once, so the caches are filled under one lock.
    private readonly object reading = new();

    public WorldMatrixFile Matrix(int id)
    {
        lock (reading)
        {
            if (!matrices.TryGetValue(id, out var matrix))
                matrices[id] = matrix = GameDataFiles.Load<WorldMatrixFile>(Path.Combine(folder, "matrices", $"{id:000}.json"));
            return matrix;
        }
    }

    /// <summary>A chunk, or null while its file isn't part of the game yet.</summary>
    public WorldChunkFile? Chunk(int id)
    {
        lock (reading)
        {
            if (!chunks.TryGetValue(id, out var chunk))
            {
                chunk = Optional<WorldChunkFile>(Path.Combine("chunks", $"{id:000}.json"));
                chunk?.Validate();
                chunks[id] = chunk;
            }
            return chunk;
        }
    }

    public WorldAreaFile? Area(string key)
    {
        lock (reading)
        {
            if (!areas.TryGetValue(key, out var area)) areas[key] = area = Optional<WorldAreaFile>(Path.Combine("areas", key + ".json"));
            return area;
        }
    }

    /// <summary>
    /// Platinum's weather calendar, each place's weather for each day of a leap year (<see cref="WorldCalendarFile"/>);
    /// empty when the folder has none.
    /// </summary>
    public IReadOnlyDictionary<string, FieldWeather[]> Calendar => calendar ??= LoadCalendar();
    private Dictionary<string, FieldWeather[]>? calendar;

    private Dictionary<string, FieldWeather[]> LoadCalendar()
    {
        var days = new Dictionary<string, FieldWeather[]>(StringComparer.OrdinalIgnoreCase);
        if (Optional<WorldCalendarFile>(WorldCalendarFile.FileName) is not { } file) return days;
        for (int p = 0; p < file.Places.Count; p++)
            days[file.Places[p]] = file.Days.Select(day => Weathers.Of(day[p])).ToArray();
        return days;
    }

    /// <summary>
    /// The region's tables of wild Pokémon that belong to no one area (plan 06 · R13, <see cref="WorldEncountersFile"/>):
    /// the honey trees', the Great Marsh's and the Trophy Garden's dailies and Feebas's tiles. Empty when the folder has none.
    /// </summary>
    public WorldEncountersFile Encounters => encounters ??= Optional<WorldEncountersFile>(WorldEncountersFile.FileName) ?? new();
    private WorldEncountersFile? encounters;

    public WorldOverlayFile? Overlay(string key)
    {
        lock (reading)
        {
            if (!overlays.TryGetValue(key, out var overlay)) overlays[key] = overlay = Optional<WorldOverlayFile>(Path.Combine("overlays", key + ".json"));
            return overlay;
        }
    }

    /// <summary>Whether an area is built: its people stand in it and the player can walk there.</summary>
    public bool IsOpen(string area) => Index.Areas.Contains(area, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// The map an area lies on, or null if the game makes no map of its matrix. A matrix the original uses for
    /// several places (the Solaceon Ruins' rooms) is a map for each, told apart by the area it is made for.
    /// </summary>
    public WorldMapEntry? MapOf(string area) =>
        Area(area) is { } file
            ? Index.Maps.FirstOrDefault(m => m.Matrix == file.Matrix && string.Equals(m.Area, area, StringComparison.OrdinalIgnoreCase))
              ?? Index.Maps.FirstOrDefault(m => m.Matrix == file.Matrix && m.Area == null)
            : null;

    /// <summary>The chunk under a tile of a matrix, with the tile's place in it; null where there is none.</summary>
    public (WorldChunkFile Chunk, int X, int Z)? ChunkAt(int matrixId, int x, int z)
    {
        var matrix = Matrix(matrixId);
        const int t = WorldChunkFile.Tiles;
        if (x < 0 || z < 0 || x >= matrix.Width * t || z >= matrix.Height * t) return null;
        int id = matrix.ChunkAt(x / t, z / t);
        return id != WorldMatrixFile.NoChunk && Chunk(id) is { } chunk ? (chunk, x % t, z % t) : null;
    }

    public TileBehavior BehaviourAt(int matrixId, int x, int z) =>
        ChunkAt(matrixId, x, z) is { } at ? at.Chunk.BehaviourAt(at.X, at.Z) : TileBehavior.None;

    public List<Map> BuildMaps()
    {
        var maps = Index.Maps.Select(entry => WorldMapBuilder.Build(this, entry)).ToList();
        // What the story reveals later is built like the rest, and the map keeps a copy of it to put back
        foreach (var hidden in Index.Hidden)
            maps.FirstOrDefault(m => m.Name == hidden.Map)?.AddHiddenPlace(hidden.Var, hidden.Value, hidden.ChunkX, hidden.ChunkY, hidden.ChunksWide, hidden.ChunksHigh);
        // Chunks the story swaps for others are built a second time by themselves, and the map keeps both grounds
        foreach (var swap in Index.Swaps)
            if (maps.FirstOrDefault(m => m.Name == swap.Map) is { } map && Index.Maps.FirstOrDefault(e => e.Name == swap.Map) is { } entry)
                map.AddSwappedPlace(swap.Var, swap.Value, swap.Cells.Select(c => (c.ChunkX, c.ChunkY)), WorldMapBuilder.BuildSwapped(this, entry, swap));
        // Turnback Cave's doors are aimed as the player comes into each room: they learn here where each door leads
        foreach (string area in TurnbackCave.Everywhere)
            for (int door = 0; door < TurnbackCave.Doors.Count; door++)
                if (WorldMapBuilder.WayInto(this, area, door) is { } way) TurnbackCave.Learn(area, door, way);
        return maps;
    }
}

/// <summary>
/// Makes a playable <see cref="Map"/> of a matrix: tiles from each chunk's cover and behaviours, buildings and
/// what else stands about from its models (<see cref="WorldModels"/>), and for the areas that are open their
/// signposts, people, doors and wild Pokémon. The map's tiles are the matrix's own, so on the overworld a
/// position is a position in the whole region.
/// </summary>
public static class WorldMapBuilder
{
    private const int T = WorldChunkFile.Tiles;

    /// <summary>The height of Sinnoh's lowlands in the original's data, in tiles: its sea lies half a tile lower.</summary>
    public const float GroundLevel = 1f;

    public static Map Build(World world, WorldMapEntry entry) => Build(world, entry, null);

    /// <summary>
    /// A map as it is round a swap's chunks once they lie in their cells (plan 01 · M10, part b; <see cref="WorldSwap"/>):
    /// built as ever from those chunks and the ring of chunks round them, which are the ground beside them, with
    /// forest everywhere else and nobody on it. <see cref="Map.AddSwappedPlace"/> copies the swapped cells' ground
    /// from it, and it is let go.
    /// </summary>
    public static Map BuildSwapped(World world, WorldMapEntry entry, WorldSwap swap)
    {
        foreach (var cell in swap.Cells)
            if (world.Chunk(cell.Chunk) == null)
                throw new InvalidDataException($"{World.IndexFile} swaps chunk {cell.Chunk} into {entry.Name}, and the game has no file of it: run tools/MapImporter with --data.");
        return Build(world, entry, swap);
    }

    private static Map Build(World world, WorldMapEntry entry, WorldSwap? swap)
    {
        var matrix = world.Matrix(entry.Matrix);
        // A swap is built by itself: its chunks in their cells, and the chunks beside them as they are
        bool Built(int cx, int cy) => swap == null || swap.Cells.Any(c => Math.Abs(c.ChunkX - cx) <= 1 && Math.Abs(c.ChunkY - cy) <= 1);
        int ChunkOf(int cx, int cy) =>
            swap?.Cells.FirstOrDefault(c => c.ChunkX == cx && c.ChunkY == cy) is { } swapped ? swapped.Chunk : Built(cx, cy) ? matrix.ChunkAt(cx, cy) : WorldMatrixFile.NoChunk;
        bool cave = entry.Setting == MapSetting.Cave, nothing = entry.Setting == MapSetting.Void, room = entry.Setting == MapSetting.Indoors;
        // A room built from a layout is as large as its floor and the walls round it, not the chunk it is laid in: a
        // room is looked at from its middle and walled at its edges (plan 19 · K1)
        // A DS game's room (plan 20 · J2) lies in its chunk's corner too, but the original leaves the chunk round it open:
        // it is as large as what can be walked to from its ways in, and the rest of it is wall
        bool[]? reach = null;
        var (mapWidth, mapHeight) = room && matrix.Width == 1 && matrix.Height == 1 && world.Chunk(ChunkOf(0, 0)) is { } only
            ? world.Index.BuildingsAtDoors ? RoomExtent(only) : RoomExtent(only, reach = RoomReach(only, world.Area(entry.Area ?? "")?.Warps ?? new()))
            // A GBA game's room longer than a chunk (plan 21 · H2, part b: Petalburg's Gym, 112 tiles deep) lies in the
            // west of a column of chunks: it is as wide as the widest of them shows, and as long as all of them, so the
            // camera looks at the hall and not at the column's empty east
            : room && world.Index.BuildingsAtDoors && matrix.Width == 1 && matrix.Height > 1
                && Enumerable.Range(0, matrix.Height).Select(cy => world.Chunk(ChunkOf(0, cy))).ToList() is var column && column.All(c => c != null)
                ? (column.Max(c => RoomExtent(c!).Width), (matrix.Height - 1) * T + RoomExtent(column[^1]!).Height)
                : (matrix.Width * T, matrix.Height * T);
        var map = new Map(mapWidth, mapHeight)
        {
            Name = entry.Name, DisplayName = world.Index.Region, Trees = entry.Trees, GroundLevel = GroundLevel, Setting = entry.Setting
        };
        // A room built from a layout is a room of the kind its name says (plan 19 · K1): a Pokémon Center's floor, a
        // Mart's, a lab's, and a house's for the rest, until the region's plan gives its rooms looks of their own
        if (room) map.Interior = RoomStyleOf(entry.Name);
        // A room whose overlay names its Leader's type is that type's Gym hall (plan 19 · K4: Pewter's of rock)
        if (room && entry.Area != null && world.Overlay(entry.Area)?.ArenaType is { } arena)
        {
            map.Interior = InteriorStyle.Gym;
            map.ArenaType = arena;
            // and its battles are fought on its Leader's stage, as a Gym of Sinnoh's are (plan 19 · K5: Lt. Surge's
            // was fought indoors on a plain floor)
            map.Arena ??= BattleArena.Gym;
        }
        // A place that is a matrix of its own is looked at with its own camera, and may be dark
        if (entry.Area != null && world.Area(entry.Area) is { } own)
        {
            map.Camera = CameraOf(own.Camera);
            map.IsDark = own.Weather == "DarkFlash";
        }

        // Until a chunk says otherwise the world is forest nobody can enter, on Sinnoh's usual ground level; a
        // cave is rock, and the Distortion World nothing
        for (int y = 0; y < map.Height; y++)
            for (int x = 0; x < map.Width; x++)
            {
                map.SetGroundTile(x, y, cave ? TileType.CaveWall : nothing ? TileType.Void : room ? TileType.Wall : TileType.Tree, isSolid: true);
                map.SetHeight(x, y, GroundLevel);
            }

        var areas = new Dictionary<string, MapArea>(StringComparer.OrdinalIgnoreCase);
        var models = new List<Placed>();
        // A GBA room's furniture, from its chunks' props (plan 19 · K2)
        var furniture = new List<Prop>();
        // Tiles that a model stands on: they get their look from the model, not from the ground's cover
        var under = new bool[map.Width * map.Height];
        // Open tiles whose ground the import couldn't name: they take the ground round them
        var vague = new List<(int X, int Z)>();
        // Blocked tiles the original draws nothing on: forest among trees, rock anywhere else
        var blank = new List<(int X, int Z)>();
        // How many of each area's trees are broad-leaved and how many are pines, and how much of its ground is snow or green
        var trees = new Dictionary<MapArea, (int Broad, int Pine, int Snow, int Green)>();
        // The chunks that have trees at all (a chunk of open sea or of bare rock has no say in what a forest beside it is)
        var wooded = new HashSet<(int X, int Y)>();
        for (int cy = 0; cy < matrix.Height; cy++)
            for (int cx = 0; cx < matrix.Width; cx++)
            {
                string key = matrix.AreaAt(cx, cy) ?? entry.Area ?? "";
                if (!areas.TryGetValue(key, out var area)) areas[key] = area = AreaOf(world, key);
                map.SetArea(cx, cy, area);

                int id = ChunkOf(cx, cy);
                if (id == WorldMatrixFile.NoChunk || world.Chunk(id) is not { } chunk) continue;
                // A chunk that holds more than one area (a Game Boy Advance map's edge, plan 18 · W6) says so tile by tile
                MapArea?[]? tilesOwn = null;
                if (chunk.Areas != null && matrix.AreaKeys is { } keys)
                {
                    tilesOwn = new MapArea?[T * T];
                    for (int z = 0; z < T; z++)
                        for (int x = 0; x < T; x++)
                        {
                            if (chunk.AreaIndexAt(x, z) is not { } index) continue;
                            string tileKey = keys[index];
                            if (!areas.TryGetValue(tileKey, out var tileArea)) areas[tileKey] = tileArea = AreaOf(world, tileKey);
                            if (tileArea == area) continue;
                            tilesOwn[z * T + x] = tileArea;
                            map.SetTileArea(cx * T + x, cy * T + z, tileArea);
                        }
                }
                PlaceChunk(map, chunk, cx * T, cy * T, matrix.AltitudeAt(cx, cy) / 2f, area, under, vague, blank, tilesOwn);
                foreach (var prop in chunk.Props)
                {
                    // A GBA room's furniture (plan 19 · K2) is laid once the room's floor and walls are
                    if (prop.Name.StartsWith(FurniturePrefix, StringComparison.Ordinal))
                    {
                        if (Enum.TryParse<PropType>(prop.Name[FurniturePrefix.Length..], out var kind))
                            furniture.Add(new Prop { Type = kind, X = cx * T + (int)prop.BoxX, Y = cy * T + (int)prop.BoxZ, Width = (int)prop.Width, Depth = (int)prop.Depth });
                        continue;
                    }
                    var model = WorldModels.Of(prop.Name, world.Index.Region);
                    // A DS game's room (plan 20 · J2): its models are furniture, laid once the room's floor and walls are
                    if (room && !world.Index.BuildingsAtDoors)
                    {
                        if (model is { Role: ModelRole.Scenery } piece)
                        {
                            // The tiles whose middle lies in the box; a thing smaller than a tile stands on the one under its middle
                            float bx = cx * T + prop.BoxX, bz = cy * T + prop.BoxZ;
                            int x0 = (int)MathF.Floor(bx - 0.5f) + 1, x1 = (int)MathF.Ceiling(bx + prop.Width - 0.5f) - 1;
                            int z0 = (int)MathF.Floor(bz - 0.5f) + 1, z1 = (int)MathF.Ceiling(bz + prop.Depth - 0.5f) - 1;
                            if (x1 < x0) x0 = x1 = (int)MathF.Floor(bx + prop.Width / 2f);
                            if (z1 < z0) z0 = z1 = (int)MathF.Floor(bz + prop.Depth / 2f);
                            furniture.Add(new Prop { Type = piece.Prop, X = x0, Y = z0, Width = x1 - x0 + 1, Depth = z1 - z0 + 1 });
                        }
                        continue;
                    }
                    models.Add(new Placed(prop, cx * T + prop.BoxX, cy * T + prop.BoxZ, model, area));
                }

                var count = trees.GetValueOrDefault(area);
                var before = count;
                foreach (string row in chunk.Cover)
                    foreach (char code in row)
                    {
                        if (code == TerrainCoverCodes.CodeOf(TerrainCover.Broadleaf)) count.Broad++;
                        else if (code == TerrainCoverCodes.CodeOf(TerrainCover.Tree)) count.Pine++;
                        else if (code == TerrainCoverCodes.CodeOf(TerrainCover.Snow)) count.Snow++;
                        else if (code == TerrainCoverCodes.CodeOf(TerrainCover.Grass) || code == TerrainCoverCodes.CodeOf(TerrainCover.TallGrass)) count.Green++;
                    }
                trees[area] = count;
                if (count.Broad + count.Pine > before.Broad + before.Pine) wooded.Add((cx, cy));
                // The forest that fills the world between its places is one area with no trees of its own: a chunk of it
                // that is broad-leaved (the Battle Zone's filler, conttree3) grows round trees, whatever the rest grows
                if (!area.Open && count.Broad - before.Broad > count.Pine - before.Pine) map.SetChunkTrees(cx, cy, TreeStyle.Round);
            }
        foreach (var (area, count) in trees)
        {
            if (count.Broad > count.Pine) area.Trees ??= TreeStyle.Round;
            // Twinleaf Town has patches of snow and is not snow country; Snowpoint City is
            area.Snowbound = count.Snow > count.Green;
        }

        // Where the world has no chunk at all there is forest too. Round the Battle Zone, where every chunk with trees
        // beside such a cell grows round ones, it is that forest; anywhere a pine wood stands beside it, it stays one
        TreeStyle? TreesOfChunk(int cx, int cy) => wooded.Contains((cx, cy)) ? map.TreesAt(cx * T, cy * T) : null;
        if (!cave && !nothing && !room)
            for (int cy = 0; cy < matrix.Height; cy++)
                for (int cx = 0; cx < matrix.Width; cx++)
                {
                    if (ChunkOf(cx, cy) != WorldMatrixFile.NoChunk && world.Chunk(ChunkOf(cx, cy)) != null) continue;
                    var beside = new[] { (0, -1), (1, 0), (-1, 0), (0, 1), (-1, -1), (1, -1), (-1, 1), (1, 1) }
                        .Select(d => TreesOfChunk(cx + d.Item1, cy + d.Item2)).OfType<TreeStyle>().ToList();
                    if (beside.Count > 0 && beside.All(style => style == TreeStyle.Round)) map.SetChunkTrees(cx, cy, TreeStyle.Round);
                }

        // A room cut down to what can be walked to (a DS game's, plan 20 · J2) leaves its chunk's open tiles beyond it
        vague.RemoveAll(t => !map.InBounds(t.X, t.Z));
        blank.RemoveAll(t => !map.InBounds(t.X, t.Z));

        // The tiles that lead somewhere, in every area whose file the game has: a building's way in
        var entrances = new HashSet<(int X, int Z)>();
        foreach (string key in areas.Keys)
            foreach (var warp in world.Area(key)?.Warps ?? new()) entrances.Add((warp.X, warp.Z));

        // A way in under a bridge (Wayward Cave's, under the Cycling Road): the original gives its tile the deck's
        // height, and it is walked into from the ground under the deck. It is the ground's, with the deck over it
        foreach (var (x, z) in entrances)
        {
            if (!map.InBounds(x, z) || map.DeckAt(x, z) != null) continue;
            var (ax, az, _) = Arrival(map.BehaviourAt(x, z), x, z);
            if (!map.InBounds(ax, az) || map.DeckAt(ax, az) is not { } over) continue;
            float here = map.HeightAt(x, z), below = map.HeightAt(ax, az);
            if (MathF.Abs(here - over) < 0.5f && here - below >= FieldMovement.StepLimit)
            {
                map.SetDeck(x, z, here);
                map.SetHeight(x, z, below);
            }
        }

        // What a tile of unnamed ground will be isn't known yet, so nothing takes its own ground from one
        var unnamed = new bool[map.Width * map.Height];
        foreach (var (x, z) in vague) unnamed[z * map.Width + x] = true;

        var parts = PlaceModels(world, entry.Matrix, map, models, under, unnamed, entrances);
        if (cave)
        {
            // A cave nothing leads into (the Underground) names the tiles it is come to by
            var ways = entrances;
            if (entry.Arrivals != null || entry.ArrivalsInEveryChunk != null)
            {
                ways = new HashSet<(int X, int Z)>(entrances);
                foreach (int[] a in entry.Arrivals ?? new List<int[]>()) ways.Add((a[0], a[1]));
                foreach (int[] a in entry.ArrivalsInEveryChunk ?? new List<int[]>())
                    for (int cz = 0; cz * Map.ChunkTiles < map.Height; cz++)
                        for (int cx = 0; cx * Map.ChunkTiles < map.Width; cx++) ways.Add((cx * Map.ChunkTiles + a[0], cz * Map.ChunkTiles + a[1]));
            }
            CaveRock(map, vague, ways);
        }
        else if (nothing)
        {
            VoidIslands(map);
        }
        else if (room)
        {
            // A room built from a layout (plan 18 · W7): what the import couldn't name is floor where it is walked and
            // wall where it is blocked, never lawn and forest
            foreach (var (x, z) in vague) map.SetGroundTile(x, z, TileType.Floor, map.IsSolid(x, z));
            foreach (var (x, z) in blank) map.SetGroundTile(x, z, TileType.Wall, isSolid: true);
            if (world.Index.BuildingsAtDoors) Furnish(map, furniture, entrances);
            else if (reach != null)
            {
                // What no way in reaches is wall, and the back wall is the row before the first that is walked on
                int back = map.Height;
                for (int z = 0; z < map.Height; z++)
                    for (int x = 0; x < map.Width; x++)
                    {
                        if (reach[z * T + x]) { back = Math.Min(back, z); continue; }
                        map.SetGroundTile(x, z, TileType.Wall, isSolid: true);
                    }
                // What hangs on the wall hangs on the wall over the floor's first row
                bool OnTheWall(Prop p) => p.Type is PropType.Clock or PropType.Painting or PropType.Window;
                // A row of furniture standing all along the back wall (Elm's lab: its machines, its desk and its bin)
                // blocks the row before the first one walked on: that row is floor along the wall's foot, blocked
                if (furniture.Any(p => !OnTheWall(p) && p.Y == back - 1 && back >= 2))
                {
                    back--;
                    for (int x = 1; x < map.Width - 1; x++)
                        if (map.GetGroundTile(x, back) == TileType.Wall) map.SetGroundTile(x, back, TileType.Floor, isSolid: true);
                }
                furniture = furniture.Select(p => OnTheWall(p) ? new Prop { Type = p.Type, X = p.X, Y = back, Width = p.Width, Depth = 1 } : p).ToList();
                // A piece of furniture stands on the floor: the tiles under it that no way reaches (it blocks them) are floor
                foreach (var piece in furniture.Where(p => !OnTheWall(p)))
                    for (int z = piece.Y; z < piece.Y + piece.Depth; z++)
                        for (int x = piece.X; x < piece.X + piece.Width; x++)
                            if (map.InBounds(x, z) && z >= back && x > 0 && x < map.Width - 1 && z < map.Height - 1)
                                map.SetGroundTile(x, z, TileType.Floor, map.IsSolid(x, z));
                LayFurniture(map, furniture.Where(p => p.Y >= back && p.X > 0 && p.X + p.Width < map.Width && p.Y + p.Depth < map.Height).ToList(), back);
            }
        }
        else
        {
            // The original paints its dark of a cave's mouth in a few places that lead into nothing (the west end of
            // Route 205's bridge): open ground there with no way in on it or beside it is the ground round it
            foreach (var (x, z) in OpenMouthsLeadingNowhere(map, entrances)) vague.Add((x, z));
            foreach (var (x, z) in vague) under[z * map.Width + x] = true;
            foreach (var (x, z) in vague)
            {
                var like = GroundLike(map, x, z, under);
                // Open ground that does nothing at all is never water: beside a canal it is the walkway over it, which the
                // original builds into the ground's own model (the Battle Frontier's bridges, plan 01 · M10, part b)
                if (like == TileType.Water && !map.IsSolid(x, z) && map.BehaviourAt(x, z) == TileBehavior.None) like = GroundLike(map, x, z, under, dry: true);
                map.SetGroundTile(x, z, like, map.IsSolid(x, z));
            }
            BareRock(map, blank);
            StreetLamps(map, under);
            // The mouth of a cave is no rock: it stays at the level of the ground, a dark hollow under the rock round it
            RaiseRock(map, (x, z) => map.GetGroundTile(x, z) == TileType.Rock && map.IsSolid(x, z));
            // A pool of lava lies level in the rock it has melted into (plan 01 · M10, part b): the rock under it is flat,
            // at the height of the lowest of its tiles, so the rock round it stands up as its rim
            foreach (var lava in map.Props.Where(p => p.Type == PropType.Lava)) Level(map, lava);
        }

        // A town's fences and walls are built like its houses, where its overlay doesn't say
        foreach (var town in parts.Where(p => p.Of.Model?.Town != null && p.Of.Model.Kind is BuildingKind.House or BuildingKind.Apartments)
                     .GroupBy(p => p.Of.Area))
            town.Key.Architecture ??= town.GroupBy(p => p.Of.Model!.Town!.Value).OrderByDescending(g => g.Count()).ThenBy(g => g.Key).First().Key;

        RocksInWater(map);
        foreach (var (key, area) in areas)
            if (area.Open) PlaceEvents(world, map, key, area, people: swap == null);
        // In the Distortion World a way somewhere is a slab set into the island
        if (nothing)
            foreach (var warp in map.Warps)
                if (map.InBounds(warp.SourceX, warp.SourceY) && map.GetGroundTile(warp.SourceX, warp.SourceY) == TileType.DistortionGround)
                    map.SetGroundTile(warp.SourceX, warp.SourceY, TileType.DistortionSlab, map.IsSolid(warp.SourceX, warp.SourceY));

        // Last, because a door knows where it leads only once the warps are in place
        map.PlacedBuildings = parts.Select(part => ToBuilding(map, part, entrances)).ToList();
        // A world whose houses are drawn into its tiles (a GBA game's, plan 18 · W7) has its buildings found at their doors
        if (world.Index.BuildingsAtDoors && !cave && !nothing && !room)
            map.PlacedBuildings.AddRange(BuildingsAtDoors(map, blank, world.Index.Maps.FirstOrDefault(m => m.Matrix == 0)?.Name,
                world.Index.Maps.Where(m => m.Setting != MapSetting.Indoors).Select(m => m.Name)));
        return map;
    }

    /// <summary>
    /// How large a room laid out in one chunk is (plan 19 · K1): its layout lies at the chunk's corner, so the room
    /// reaches one tile past the last tile anyone can stand on, east and south, for the wall there.
    /// </summary>
    public static (int Width, int Height) RoomExtent(WorldChunkFile chunk)
    {
        int maxX = 0, maxZ = 0;
        for (int z = 0; z < T; z++)
            for (int x = 0; x < T; x++)
                if (!chunk.SolidAt(x, z))
                {
                    maxX = Math.Max(maxX, x);
                    maxZ = Math.Max(maxZ, z);
                }
        return (Math.Min(T, maxX + 2), Math.Min(T, maxZ + 2));
    }

    /// <summary>
    /// The tiles of a DS game's room (plan 20 · J2) that can be walked to from its ways in, by the chunk's own tiles:
    /// the original leaves the chunk round the room open, where nobody can come.
    /// </summary>
    public static bool[] RoomReach(WorldChunkFile chunk, IReadOnlyList<AreaWarp> warps)
    {
        var reached = new bool[T * T];
        var open = new Stack<(int X, int Z)>();
        foreach (var w in warps)
            if (w.X >= 0 && w.Z >= 0 && w.X < T && w.Z < T) open.Push((w.X, w.Z));
        var ways = warps.Select(w => (w.X, w.Z)).ToHashSet();
        while (open.Count > 0)
        {
            var (x, z) = open.Pop();
            if (x < 0 || z < 0 || x >= T || z >= T || reached[z * T + x]) continue;
            if (chunk.SolidAt(x, z) && !ways.Contains((x, z))) continue;
            reached[z * T + x] = true;
            open.Push((x + 1, z)); open.Push((x - 1, z)); open.Push((x, z + 1)); open.Push((x, z - 1));
        }
        return reached;
    }

    /// <summary>How large a DS game's room is (plan 20 · J2): one tile past the last tile its ways in reach, east and south, for the wall there.</summary>
    public static (int Width, int Height) RoomExtent(WorldChunkFile chunk, bool[] reach)
    {
        int maxX = 0, maxZ = 0;
        for (int z = 0; z < T; z++)
            for (int x = 0; x < T; x++)
                if (reach[z * T + x])
                {
                    maxX = Math.Max(maxX, x);
                    maxZ = Math.Max(maxZ, z);
                }
        return (Math.Min(T, maxX + 2), Math.Min(T, maxZ + 2));
    }

    /// <summary>The prefix of a chunk's prop that is a piece of a GBA room's furniture (<c>gba:Table</c>; plan 19 · K2).</summary>
    public const string FurniturePrefix = "gba:";

    /// <summary>
    /// A GBA room furnished (plan 19 · K2). The GBA draws a room's back wall two metatiles high and stands what is
    /// against it (a bookshelf, the kitchen, a television) in the lower of the two; here the back wall is one face, so
    /// that row becomes the floor along its foot, blocked as it was, and its furniture stands there; what hangs on the
    /// wall (a window, a painting) hangs on the wall above it. A walkable tile above the wall (the Pokémon Center's)
    /// is wall. Each piece's tiles are floor, blocked or not as the layout says; a piece this game builds a tile at a
    /// time (a bookshelf, a plant) is laid a tile at a time, and a flight of stairs a column at a time.
    /// </summary>
    public static void Furnish(Map map, IReadOnlyList<Prop> furniture, IReadOnlySet<(int X, int Z)> ways)
    {
        // (a way out through the back wall, a gate's door north, is no floor)
        bool Walkable(int x, int y) => map.InBounds(x, y) && !map.IsSolid(x, y) && !ways.Contains((x, y));
        // The first row of the floor: the first that one can walk into from the row below
        int back = -1;
        for (int y = 0; y < map.Height - 2 && back < 0; y++)
            for (int x = 0; x < map.Width; x++)
                if (Walkable(x, y) && Walkable(x, y + 1)) { back = y; break; }
        int wallRow = back - 1;
        if (wallRow >= 0)
        {
            bool Leads(int x, int y) => ways.Contains((x, y));
            for (int y = 0; y < wallRow; y++)
                for (int x = 0; x < map.Width; x++)
                    if (map.GetGroundTile(x, y) != TileType.Wall && !Leads(x, y)) map.SetGroundTile(x, y, TileType.Wall, isSolid: true);
            for (int x = 0; x < map.Width - 1; x++)
            {
                if (map.GetGroundTile(x, wallRow) != TileType.Wall) continue;
                bool floorBelow = false;
                for (int y = back; y < map.Height - 1 && !floorBelow; y++) floorBelow = map.GetGroundTile(x, y) != TileType.Wall;
                if (floorBelow) map.SetGroundTile(x, wallRow, TileType.Floor, isSolid: true);
            }
        }
        // A rug the GBA draws round a table in pieces is one rug under the table
        var rugs = furniture.Where(p => p.Type == PropType.Rug).Select(p => (X0: p.X, Z0: p.Y, X1: p.X + p.Width - 1, Z1: p.Y + p.Depth - 1)).ToList();
        for (bool merged = true; merged;)
        {
            merged = false;
            for (int a = 0; a < rugs.Count && !merged; a++)
                for (int b = a + 1; b < rugs.Count && !merged; b++)
                {
                    var (p, q) = (rugs[a], rugs[b]);
                    if (q.X0 > p.X1 + 1 || p.X0 > q.X1 + 1 || q.Z0 > p.Z1 + 1 || p.Z0 > q.Z1 + 1) continue;
                    rugs[a] = (Math.Min(p.X0, q.X0), Math.Min(p.Z0, q.Z0), Math.Max(p.X1, q.X1), Math.Max(p.Z1, q.Z1));
                    rugs.RemoveAt(b);
                    merged = true;
                }
        }
        furniture = furniture.Where(p => p.Type != PropType.Rug)
            .Concat(rugs.Select(r => new Prop { Type = PropType.Rug, X = r.X0, Y = r.Z0, Width = r.X1 - r.X0 + 1, Depth = r.Z1 - r.Z0 + 1 })).ToList();
        LayFurniture(map, furniture, wallRow);
    }

    /// <summary>
    /// A room's furniture laid on its floor (plan 19 · K2, and plan 20 · J2's rooms of a DS game): each piece's tiles are
    /// floor, blocked or not as they were; what hangs on the back wall (a window, a painting, a clock) hangs on the
    /// row above <paramref name="wallRow"/>, the floor's foot along the wall, where a piece stands on it; a piece this
    /// game builds a tile at a time is laid a tile at a time.
    /// </summary>
    public static void LayFurniture(Map map, IReadOnlyList<Prop> furniture, int wallRow)
    {
        foreach (var piece in furniture)
        {
            if (piece.Type is PropType.Window or PropType.Painting or PropType.Clock or PropType.WallEmblem)
            {
                // Hung on the back wall: the row above the floor's foot
                if (piece.Y != wallRow || wallRow < 1) continue;
                for (int x = piece.X; x < piece.X + piece.Width; x++)
                    map.Props.Add(new Prop { Type = piece.Type, X = x, Y = wallRow - 1 });
                continue;
            }
            for (int y = piece.Y; y < piece.Y + piece.Depth; y++)
                for (int x = piece.X; x < piece.X + piece.Width; x++)
                    if (map.InBounds(x, y)) map.SetGroundTile(x, y, TileType.Floor, map.IsSolid(x, y));
            switch (piece.Type)
            {
                // A bed is one piece however wide (plan 19 · K3: the bedroom's, three metatiles by three, a bed for two as the Villa's is)
                case PropType.Table or PropType.Counter or PropType.Rug or PropType.StoreShelf or PropType.LabDesk or PropType.Bench or PropType.Bed:
                    map.Props.Add(piece);
                    break;
                case PropType.Stairs or PropType.LabMachine:
                    for (int x = piece.X; x < piece.X + piece.Width; x++)
                        map.Props.Add(new Prop { Type = piece.Type, X = x, Y = piece.Y, Depth = piece.Depth });
                    break;
                default:
                    for (int y = piece.Y; y < piece.Y + piece.Depth; y++)
                        for (int x = piece.X; x < piece.X + piece.Width; x++)
                            map.Props.Add(new Prop { Type = piece.Type, X = x, Y = y });
                    break;
            }
        }
    }

    /// <summary>The look of a room built from a layout, by its map's name (<see cref="MapStructures.KindOfDoor"/>).</summary>
    public static InteriorStyle RoomStyleOf(string name) => MapStructures.KindOfDoor(name) switch
    {
        BuildingKind.PokemonCenter => InteriorStyle.PokemonCenter,
        BuildingKind.PokeMart => InteriorStyle.PokeMart,
        BuildingKind.Lab => InteriorStyle.Lab,
        // A tower's floors are bare stone as the Lost Tower's are (plan 19 · K5: the Pokémon Tower)
        BuildingKind.Tower => InteriorStyle.Lab,
        _ => InteriorStyle.House
    };

    /// <summary>
    /// What a building is by the key of the area its door leads into, for a door kept shut whose room isn't built
    /// (<c>petalburg_city_gym</c>): the same words as <see cref="MapStructures.KindOfDoor"/> reads in a map's name.
    /// </summary>
    public static BuildingKind KindOfArea(string key) => key switch
    {
        _ when key.Contains("pokemon_center") => BuildingKind.PokemonCenter,
        _ when key.EndsWith("_mart") => BuildingKind.PokeMart,
        _ when key.EndsWith("_gym") || key.Contains("_gym_") => BuildingKind.Gym,
        _ when key.EndsWith("_lab") => BuildingKind.Lab,
        _ => BuildingKind.House
    };

    /// <summary>How far a building found at its door reaches: a GBA game's largest houses and halls fit.</summary>
    public const int DoorBuildingWidth = 16, DoorBuildingDepth = 12;

    /// <summary>
    /// The buildings of a map whose world has no models (plan 18 · W7): from each door of an open area (a warp on a
    /// tile of the behaviour <see cref="TileBehavior.Door"/>), the blocked tiles the import couldn't name round it,
    /// row by row: the front row the door is in, as wide as the row behind it (a fence running on from the front wall
    /// is left out), and the rows behind as long as each is about as wide as that (the trees behind a house, which run
    /// on far wider, are not the house), within <see cref="DoorBuildingWidth"/> by <see cref="DoorBuildingDepth"/>.
    /// Its kind is what the door leads into (<see cref="MapStructures.KindOfDoor"/>). The box is laid as a building's
    /// tiles: roof, the front row wall, the doors doors.
    /// </summary>
    public static List<BuildingInfo> BuildingsAtDoors(Map map, IEnumerable<(int X, int Z)> unnamedBlocked, string? overworld = null,
        IEnumerable<string>? openMaps = null)
    {
        var blank = new HashSet<(int X, int Z)>(unnamedBlocked);
        var open = new HashSet<string>(openMaps ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase);
        var buildings = new List<BuildingInfo>();
        var taken = new HashSet<(int X, int Z)>();
        // Every way in, and the doors an overlay keeps shut (plan 21 · H1: Petalburg's Gym), which lead nowhere yet but
        // are a building's doors all the same, of the kind the shut door was noted as (Map.BuildingKinds)
        var ways = map.Warps.Select(w => (X: w.SourceX, Y: w.SourceY, Target: (string?)w.TargetMap))
            .Concat(map.BuildingKinds.Keys.Where(k => map.GetWarpAt(k.X, k.Y) == null).Select(k => (k.X, k.Y, Target: (string?)null)));
        foreach (var warp in ways.OrderBy(w => w.Y).ThenBy(w => w.X).ToList())
        {
            int dx = warp.X, dz = warp.Y;
            if (!map.InBounds(dx, dz) || taken.Contains((dx, dz))) continue;
            // A way off a map of its own straight onto the region's open country is no building's door (plan 19 · K4,
            // part b: the S.S. Anne's gangway at Vermilion's pier)
            if (overworld != null && map.Name != overworld && warp.Target != null && warp.Target.Equals(overworld, StringComparison.OrdinalIgnoreCase)) continue;
            // nor, the other way, a way off the open country onto a map that is no room: open country of its own or a
            // cave (plan 19 · K5: the gangway at Vermilion's pier, which leads onto SSAnne_Exterior, grew a house there)
            if (warp.Target != null && !warp.Target.Equals(map.Name, StringComparison.OrdinalIgnoreCase) && open.Contains(warp.Target)) continue;
            bool isDoor = map.BehaviourAt(dx, dz) == TileBehavior.Door;
            if (!isDoor && (map.IsSolid(dx, dz) || !blank.Contains((dx, dz + 1)) && !blank.Contains((dx, dz - 1)))) continue;
            // The run of such tiles along a row through the door's column, no further than half the widest either way
            bool Wall(int x, int z) => (x == dx && z == dz) || blank.Contains((x, z)) && !taken.Contains((x, z));
            (int A, int B)? Run(int z)
            {
                if (!Wall(dx, z)) return null;
                int a = dx, b = dx;
                while (a - 1 > dx - DoorBuildingWidth / 2 && Wall(a - 1, z)) a--;
                while (b + 1 < dx + DoorBuildingWidth / 2 && Wall(b + 1, z)) b++;
                return (a, b);
            }
            // The building is the body over its front row: a fence that runs on from the front wall is no part of it,
            // and a row that reaches much wider than the body (the trees behind a house) is where it ends
            if (Run(dz) is not { } front) continue;
            int x0, x1, z0, z1;
            bool fromNorth = false;
            if (isDoor && Run(dz - 1) is { } body)
            {
                x0 = Math.Max(front.A, body.A); x1 = Math.Min(front.B, body.B); z0 = dz - 1; z1 = dz;
                while (z0 - 1 > dz - DoorBuildingDepth && Run(z0 - 1) is { } above && above.A >= x0 - 1 && above.B <= x1 + 1
                       && above.B - above.A + 1 >= (x1 - x0 + 1) * 3 / 4)
                    z0--;
            }
            // A gate entered from the north (FireRed's gates of Viridian Forest on Route 2, plan 19 · K2): its door is
            // the open tile before its north wall, and the building lies south of it, its own front shut
            else if (!isDoor && Run(dz + 1) is { } under)
            {
                fromNorth = true;
                x0 = under.A; x1 = under.B; z0 = dz + 1; z1 = dz + 1;
                // A gate is as wide as its widest row (its roof reaches past the doorway either side): every row of
                // blocked tiles nobody named behind the way is the gate's, as far as the largest building reaches
                while (z1 + 1 < dz + DoorBuildingDepth && Run(z1 + 1) is { } below) { x0 = Math.Min(x0, below.A); x1 = Math.Max(x1, below.B); z1++; }
            }
            // The same gate seen from the forest inside it: a way on the open tile before its south wall
            else if (!isDoor && Run(dz - 1) is { } wall)
            {
                x0 = wall.A; x1 = wall.B; z0 = dz - 1; z1 = dz - 1;
                while (z0 - 1 > dz - DoorBuildingDepth && Run(z0 - 1) is { } above) { x0 = Math.Min(x0, above.A); x1 = Math.Max(x1, above.B); z0--; }
            }
            else continue;
            // A run of blocked tiles one row deep before a way (a fence, a hedge) is no building
            if (z1 - z0 < 1) continue;
            // Eaves and corners the original draws beside the walls (a column of blocked tiles nobody named down at least
            // half the house's rows) are the house's too, or they stand beside it as bare rock (plan 19 · K1)
            bool Eaves(int x)
            {
                int n = 0;
                for (int z = z0; z <= z1; z++) if (blank.Contains((x, z)) && !taken.Contains((x, z))) n++;
                return n * 2 >= z1 - z0 + 1;
            }
            while (x0 - 1 > dx - DoorBuildingWidth / 2 && Eaves(x0 - 1)) x0--;
            while (x1 + 1 < dx + DoorBuildingWidth / 2 && Eaves(x1 + 1)) x1++;
            var tiles = new HashSet<(int X, int Z)>();
            for (int z = z0; z <= z1; z++)
                for (int x = x0; x <= x1; x++)
                    if (Wall(x, z)) tiles.Add((x, z));
            var info = new BuildingInfo
            {
                X0 = x0, Y0 = z0, X1 = x1, Y1 = z1, RoofTile = TileType.RoofRed, TownsRoof = true,
                Kind = map.BuildingKinds.TryGetValue((dx, dz), out var noted) ? noted : MapStructures.KindOfDoor(warp.Target) ?? BuildingKind.House
            };
            // The house stands on the ground before its door: its tiles were raised as bare rock while nobody knew what
            // they were (RaiseRock), and a house on that would stand on a plinth (plan 19 · K1)
            float ground = map.HeightAt(dx, fromNorth ? dz : dz + 1);
            for (int z = z0; z <= z1; z++)
                for (int x = x0; x <= x1; x++)
                {
                    taken.Add((x, z));
                    if (tiles.Contains((x, z)) || map.IsSolid(x, z)) map.SetHeight(x, z, ground);
                    if (map.BehaviourAt(x, z) == TileBehavior.Door && map.GetWarpAt(x, z) is { } door)
                    {
                        map.SetGroundTile(x, z, TileType.Door, isSolid: false);
                        if (z == z1) info.Doors.Add((x, door.TargetMap));
                    }
                    // An open tile inside the box (a step beside a porch) stays as it was
                    else if (tiles.Contains((x, z)) || map.IsSolid(x, z)) map.SetGroundTile(x, z, z == z1 ? TileType.Wall : TileType.RoofRed, isSolid: true);
                }
            buildings.Add(info);
        }

        // What the original draws of a house past the box found at its door (a roof's back corner, an eave round a
        // corner: Viridian City's Mart and houses) stood beside it as grey rock. Blocked tiles nobody named within eight
        // steps of a building are the ground round them, still blocked, at the building's foot (plan 19 · K2)
        var near = new Dictionary<(int X, int Z), float>();
        var frontier = new Queue<((int X, int Z) Tile, int Steps, float Ground)>();
        foreach (var b in buildings)
        {
            float foot = map.HeightAt(b.X0, b.Y1);
            for (int z = b.Y0; z <= b.Y1; z++)
                for (int x = b.X0; x <= b.X1; x++) frontier.Enqueue(((x, z), 0, foot));
        }
        while (frontier.Count > 0)
        {
            var ((x, z), steps, foot) = frontier.Dequeue();
            if (steps >= 8) continue;
            foreach (var (sx, sz) in new[] { (0, -1), (0, 1), (-1, 0), (1, 0) })
            {
                var next = (x + sx, z + sz);
                if (!blank.Contains(next) || taken.Contains(next) || near.ContainsKey(next)) continue;
                near[next] = foot;
                frontier.Enqueue((next, steps + 1, foot));
            }
        }
        foreach (var ((x, z), foot) in near)
        {
            TileType? ground = null;
            for (int r = 1; r <= 3 && ground == null; r++)
                foreach (var (sx, sz) in new[] { (0, r), (-r, 0), (r, 0), (0, -r) })
                    if (map.InBounds(x + sx, z + sz) && !map.IsSolid(x + sx, z + sz) && map.GetGroundTile(x + sx, z + sz) is var t
                        && t is not (TileType.Door or TileType.Water or TileType.Wall or TileType.Signpost or TileType.Floor)) { ground = t; break; }
            map.SetGroundTile(x, z, ground == TileType.TallGrass ? TileType.Grass : ground ?? TileType.Tree, isSolid: true);
            map.SetHeight(x, z, foot);
        }
        return buildings;
    }

    /// <summary>Makes the ground under a prop flat, at the height of the lowest tile it covers.</summary>
    private static void Level(Map map, Prop prop)
    {
        float low = float.MaxValue;
        for (int z = prop.Y; z < prop.Y + prop.Depth; z++)
            for (int x = prop.X; x < prop.X + prop.Width; x++)
                if (map.InBounds(x, z)) low = MathF.Min(low, map.HeightAt(x, z));
        if (low == float.MaxValue) return;
        for (int z = prop.Y; z < prop.Y + prop.Depth; z++)
            for (int x = prop.X; x < prop.X + prop.Width; x++)
                if (map.InBounds(x, z)) map.SetHeight(x, z, low);
    }

    /// <summary>The field's camera for one of the original's camera names; anything this game has no own camera for is the default.</summary>
    public static FieldCamera CameraOf(string? name) => name switch
    {
        "Cave" => FieldCamera.Cave,
        "ZoomedIn" => FieldCamera.ZoomedIn,
        // The north face's camera is the overworld's to within a hundredth of a degree (CAMERA_TYPE_MT_CORONET_EXT_NORTH)
        "MtCoronetExtSouth" => FieldCamera.CoronetSouth,
        "StarkMountainRoom2" => FieldCamera.StarkMountain,
        _ => FieldCamera.Default
    };

    /// <summary>How far a cave's rock stands above the floor before it (seen from the south), in tiles.</summary>
    public const float CaveWallRise = 2f;

    /// <summary>
    /// How far the rock stands up right in front of a floor (the row south of it): low, so that it hides no more
    /// than the feet of whoever walks there (at the cave camera's 63° a rise of this much covers under four
    /// tenths of a tile behind it). It is the least that is still a step with a face: anything lower the field
    /// would join to the floor with a slope (<c>Relief.WeldLimit</c>), and the floor would rise to meet the rock.
    /// </summary>
    public const float CaveLipRise = 0.75f;

    /// <summary>
    /// Finishes a cave's rock (plan 01 · M5; style guide, "Caves"). Open ground the import couldn't name is floor
    /// where it can be reached from one of the cave's ways in, and whatever can't be reached is rock: the space
    /// beyond the walls, which the original leaves open and empty. Then every tile of rock is raised above the
    /// floor nearest to it: <see cref="CaveWallRise"/>, or only <see cref="CaveLipRise"/> where floor lies
    /// right behind it, so a wall is tall where its face shows and low where it would hide the way.
    /// </summary>
    private static void CaveRock(Map map, List<(int X, int Z)> vague, HashSet<(int X, int Z)> entrances)
    {
        int w = map.Width, h = map.Height;
        bool IsRock(int x, int z) => map.GetGroundTile(x, z) == TileType.CaveWall;

        // What can be reached from the cave's ways in, over anything that isn't blocked (water is ridden, a ledge
        // hopped, a cracked rock smashed: none of them is a blocked tile), and over a Bicycle's ramp, which is: it is
        // jumped the way it faces, to one tile past it in low gear and three in top (Victory Road's second floor)
        var reached = new bool[w * h];
        var open = new Queue<(int X, int Z)>();
        void Reach(int x, int z)
        {
            if (!map.InBounds(x, z) || reached[z * w + x] || map.IsSolid(x, z)) return;
            reached[z * w + x] = true;
            open.Enqueue((x, z));
        }
        // A rock face is a blocked tile that Rock Climb takes one up or down along its grain (Mt. Coronet's upper
        // floors lie past them): the floor at its far end is reached all the same, as the field's rule reaches it
        void Step(int x, int z, int dx, int dz)
        {
            int nx = x + dx, nz = z + dz;
            if (map.InBounds(nx, nz) && map.BehaviourAt(nx, nz) is var face
                && (face == TileBehavior.RockClimbNorthSouth && dz != 0 || face == TileBehavior.RockClimbEastWest && dx != 0))
                while (map.InBounds(nx, nz) && map.BehaviourAt(nx, nz) == face) { nx += dx; nz += dz; }
            Reach(nx, nz);
        }
        foreach (var (x, z) in entrances)
        {
            // A way in may itself be a blocked tile (a door): the cave starts beside it
            Reach(x, z);
            Reach(x + 1, z); Reach(x - 1, z); Reach(x, z + 1); Reach(x, z - 1);
        }
        while (open.Count > 0)
        {
            var (x, z) = open.Dequeue();
            Step(x, z, 1, 0); Step(x, z, -1, 0); Step(x, z, 0, 1); Step(x, z, 0, -1);
            foreach (var way in new[] { Direction.Left, Direction.Right })
            {
                var (dx, dz) = FieldMovement.Delta(way);
                if (!map.InBounds(x + dx, z + dz) || FieldMovement.RampDirection(map.BehaviourAt(x + dx, z + dz)) != way) continue;
                Reach(x + 2 * dx, z + 2 * dz);
                Reach(x + 4 * dx, z + 4 * dz);
            }
        }
        foreach (var (x, z) in vague)
            if (reached[z * w + x]) map.SetGroundTile(x, z, TileType.CaveFloor, isSolid: false);
        // The rest is rock: the space past the walls, and any pocket of floor the walls close in
        for (int z = 0; z < h; z++)
            for (int x = 0; x < w; x++)
                if (!map.IsSolid(x, z) && !reached[z * w + x]) map.SetGroundTile(x, z, TileType.CaveWall, isSolid: true);
        OpenWaysOut(map, entrances);
        RaiseRock(map, IsRock);
    }

    /// <summary>
    /// A cave's ways out, seen from inside: the original leaves the cave by a mat at the floor's edge (<c>Exit…</c>),
    /// walked off into the rock, and paints the rock past it as the dark of a passage. That tile is the mouth's dark
    /// here too, at the mat's height, so it stays a hollow under the rock round it instead of a wall like the rest.
    /// </summary>
    private static void OpenWaysOut(Map map, HashSet<(int X, int Z)> entrances)
    {
        foreach (var (x, z) in entrances)
        {
            if (!map.InBounds(x, z) || map.IsSolid(x, z)) continue;
            Direction? way = map.BehaviourAt(x, z) switch
            {
                TileBehavior.ExitNorth => Direction.Up,
                TileBehavior.ExitSouth => Direction.Down,
                TileBehavior.ExitWest => Direction.Left,
                TileBehavior.ExitEast => Direction.Right,
                _ => null
            };
            if (way is not { } out_) continue;
            var (dx, dz) = FieldMovement.Delta(out_);
            int bx = x + dx, bz = z + dz;
            if (!map.InBounds(bx, bz) || map.GetGroundTile(bx, bz) != TileType.CaveWall) continue;
            map.SetGroundTile(bx, bz, TileType.CaveMouth, isSolid: true);
            map.SetHeight(bx, bz, map.HeightAt(x, z));
        }
    }

    /// <summary>How far below the islands of the Distortion World the drop under them is drawn, in tiles: the depth of their undersides.</summary>
    public const float VoidDrop = 3f;

    /// <summary>
    /// How far the Distortion World's rock stands above the island it rims: the least that is still a step with a
    /// face, so a path between two rims stays in view.
    /// </summary>
    public const float VoidRimRise = 0.75f;

    /// <summary>
    /// Finishes the Distortion World's islands (plan 01 · M8; style guide, "The Distortion World"). Water that reaches
    /// the map's edge is the sea far under the islands: it lies at the drop's depth and fills the nothing round
    /// them, so the islands stand out of it. Everything else that is nothing lies <see cref="VoidDrop"/> under the
    /// nearest island, so each island's edge shows its underside; and the rock that rims an island stands
    /// <see cref="VoidRimRise"/> above it.
    /// </summary>
    private static void VoidIslands(Map map)
    {
        int w = map.Width, h = map.Height;
        bool Nothing(int x, int z) => map.GetGroundTile(x, z) == TileType.Void;
        bool Water(int x, int z) => map.GetGroundTile(x, z) == TileType.Water;

        // The sea: water joined to the map's edge, and the nothing joined to it
        var sea = new bool[w * h];
        var flood = new Queue<(int X, int Z)>();
        for (int z = 0; z < h; z++)
            for (int x = 0; x < w; x++)
                if ((x == 0 || z == 0 || x == w - 1 || z == h - 1) && Water(x, z))
                {
                    sea[z * w + x] = true;
                    flood.Enqueue((x, z));
                }
        while (flood.Count > 0)
        {
            var (x, z) = flood.Dequeue();
            foreach (var (dx, dz) in new[] { (0, 1), (1, 0), (-1, 0), (0, -1) })
            {
                int nx = x + dx, nz = z + dz;
                if (!map.InBounds(nx, nz) || sea[nz * w + nx] || !(Water(nx, nz) || Nothing(nx, nz))) continue;
                sea[nz * w + nx] = true;
                flood.Enqueue((nx, nz));
            }
        }
        for (int i = 0; i < sea.Length; i++)
        {
            if (!sea[i]) continue;
            int x = i % w, z = i / w;
            // Nobody surfs on the sea down there; a gap over it is still jumped
            if (Nothing(x, z) && FieldMovement.LongJumpDirection(map.BehaviourAt(x, z)) == null) map.SetBehaviour(x, z, TileBehavior.Sea);
            map.SetGroundTile(x, z, TileType.Water, isSolid: true);
        }

        // The height of the nearest island, for each tile of the drop and of the sea
        bool Below(int x, int z) => sea[z * w + x] || Nothing(x, z);
        var near = new float[w * h];
        var seen = new bool[w * h];
        var front = new Queue<(int X, int Z)>();
        for (int z = 0; z < h; z++)
            for (int x = 0; x < w; x++)
            {
                if (Below(x, z)) continue;
                near[z * w + x] = map.HeightAt(x, z);
                seen[z * w + x] = true;
                front.Enqueue((x, z));
            }
        if (front.Count == 0) return;
        while (front.Count > 0)
        {
            var (x, z) = front.Dequeue();
            foreach (var (dx, dz) in new[] { (0, 1), (1, 0), (-1, 0), (0, -1) })
            {
                int nx = x + dx, nz = z + dz;
                if (!map.InBounds(nx, nz) || seen[nz * w + nx]) continue;
                seen[nz * w + nx] = true;
                near[nz * w + nx] = near[z * w + x];
                front.Enqueue((nx, nz));
            }
        }
        // The sea is one level, under the lowest island that stands in it
        float seaLevel = float.MaxValue;
        for (int i = 0; i < sea.Length; i++)
            if (sea[i]) seaLevel = MathF.Min(seaLevel, MathF.Floor(near[i]) - VoidDrop);
        for (int z = 0; z < h; z++)
            for (int x = 0; x < w; x++)
            {
                if (sea[z * w + x]) map.SetHeight(x, z, seaLevel);
                else if (Below(x, z)) map.SetHeight(x, z, MathF.Floor(near[z * w + x]) - VoidDrop);
                else if (map.GetGroundTile(x, z) == TileType.Rock && map.IsSolid(x, z)) map.SetHeight(x, z, map.HeightAt(x, z) + VoidRimRise);
            }
    }

    /// <summary>
    /// What a tile is in the Distortion World: what the original doesn't say anything of is nothing where it blocks
    /// and the islands' stone where it is walked on, and a gap jumped across is nothing too. Rock, water, grass and
    /// trees are what they are.
    /// </summary>
    public static (TileType Type, bool Solid, PropType? Prop) InTheVoid(TerrainCover cover, TileBehavior behaviour, bool solid, TileType type, bool blocks, PropType? prop)
    {
        if (FieldMovement.LongJumpDirection(behaviour) != null) return (TileType.Void, true, null);
        // A ledge is a drop from one island down onto the next (B5F's), hopped whatever the original's blocked flag says
        if (FieldMovement.LedgeDirection(behaviour) != null) return (type, blocks, prop);
        if (cover == TerrainCover.Unknown) return solid ? (TileType.Void, true, null) : (TileType.DistortionGround, false, null);
        // The original's marks on its floors (behaviour 0x08) are the stone like the rest: the ways somewhere get slabs
        if (type == TileType.CaveFloor) return (TileType.DistortionGround, blocks, null);
        return (type, blocks, prop);
    }

    /// <summary>
    /// A blocked tile with nothing drawn on it in the original is forest where trees stand within
    /// <see cref="BlankReach"/> tiles of it, and rock anywhere else: the bare ground round Mt. Coronet's faces and
    /// past Spear Pillar's way in was pine forest because whatever blocks without saying what it is was taken for a tree.
    /// </summary>
    private static void BareRock(Map map, List<(int X, int Z)> blank)
    {
        var isBlank = new HashSet<(int X, int Z)>(blank);
        bool TreeNear(int x, int z)
        {
            for (int dz = -BlankReach; dz <= BlankReach; dz++)
                for (int dx = -BlankReach; dx <= BlankReach; dx++)
                {
                    int nx = x + dx, nz = z + dz;
                    if (map.InBounds(nx, nz) && !isBlank.Contains((nx, nz)) && map.GetGroundTile(nx, nz) == TileType.Tree) return true;
                }
            return false;
        }
        // Decided on the map as it was, so the first tiles turned to rock don't decide their neighbours
        var bare = blank.Where(t => !TreeNear(t.X, t.Z)).ToList();
        foreach (var (x, z) in bare) map.SetGroundTile(x, z, TileType.Rock, isSolid: true);
    }

    /// <summary>How far a blocked tile nobody named looks for trees before it is taken for forest.</summary>
    public const int BlankReach = 2;

    /// <summary>
    /// Stands rock up above the ground (style guide, "Caves" and "Rock outdoors"): each tile of rock on the
    /// height of the nearest ground that isn't rock, plus <see cref="CaveWallRise"/>, or only
    /// <see cref="CaveLipRise"/> where open ground lies right behind it. A cave's walls are such rock, and so are
    /// the mountainsides of the open country: the world's own heights say nothing of rock nobody walks on, which
    /// by them would lie as flat as the road beside it.
    /// </summary>
    private static void RaiseRock(Map map, Func<int, int, bool> IsRock)
    {
        int w = map.Width, h = map.Height;

        // Each tile of rock stands on the height of the nearest ground that isn't rock
        var near = new float[w * h];
        var seen = new bool[w * h];
        var front = new Queue<(int X, int Z)>();
        for (int z = 0; z < h; z++)
            for (int x = 0; x < w; x++)
            {
                if (IsRock(x, z)) continue;
                // Water is drawn level with its banks (Relief), so rock beside it stands on the bank's height
                near[z * w + x] = map.IsDeepWater(x, z) ? MathF.Ceiling(map.HeightAt(x, z) - 0.01f) : map.HeightAt(x, z);
                seen[z * w + x] = true;
                front.Enqueue((x, z));
            }
        if (front.Count == 0) return;
        while (front.Count > 0)
        {
            var (x, z) = front.Dequeue();
            foreach (var (dx, dz) in new[] { (0, 1), (1, 0), (-1, 0), (0, -1) })
            {
                int nx = x + dx, nz = z + dz;
                if (!map.InBounds(nx, nz) || seen[nz * w + nx]) continue;
                seen[nz * w + nx] = true;
                near[nz * w + nx] = near[z * w + x];
                front.Enqueue((nx, nz));
            }
        }

        for (int z = 0; z < h; z++)
            for (int x = 0; x < w; x++)
            {
                if (!IsRock(x, z)) continue;
                bool floorBehind = map.InBounds(x, z - 1) && !IsRock(x, z - 1);
                map.SetHeight(x, z, near[z * w + x] + (floorBehind ? CaveLipRise : CaveWallRise));
            }
    }

    /// <summary>
    /// A city's street lamps. The import knows a lamp by its texture, and Jubilife's stand on a texture it takes
    /// for a fence: one tile of "fence" by itself, with no fence beside it, in a paved street. Such a tile is a
    /// lamp standing on the paving round it; a post alone in a meadow stays a post.
    /// </summary>
    private static void StreetLamps(Map map, bool[] under)
    {
        var fenced = new HashSet<(int X, int Y)>();
        foreach (var prop in map.Props)
            if (prop.Type is PropType.Fence or PropType.LowWall) fenced.Add((prop.X, prop.Y));

        var lamps = new List<Prop>();
        foreach (var prop in map.Props)
        {
            if (prop.Type != PropType.Fence || prop.Width != 1 || prop.Depth != 1) continue;
            var (x, y) = (prop.X, prop.Y);
            if (fenced.Contains((x + 1, y)) || fenced.Contains((x - 1, y)) || fenced.Contains((x, y + 1)) || fenced.Contains((x, y - 1))) continue;
            under[y * map.Width + x] = true;
            if (GroundLike(map, x, y, under) != TileType.Paving) continue;
            lamps.Add(prop);
        }
        foreach (var post in lamps)
        {
            map.Props.Remove(post);
            map.Props.Add(new Prop { Type = PropType.LampPost, X = post.X, Y = post.Y });
            map.SetGroundTile(post.X, post.Y, TileType.Paving, isSolid: true);
        }
    }

    /// <summary>
    /// A rock standing in the sea is a blocked tile whose behaviour says nothing of water, so by itself it would
    /// be a boulder on a square of dirt. Wherever such a boulder touches water (or another that does), the tile
    /// is water too: the sea runs round the rock, and the rock keeps its tile blocked.
    /// </summary>
    private static void RocksInWater(Map map)
    {
        var rocks = new HashSet<(int X, int Y)>();
        foreach (var prop in map.Props)
            if (prop.Type == PropType.Boulder && map.GetGroundTile(prop.X, prop.Y) == TileType.Dirt) rocks.Add((prop.X, prop.Y));

        // Standing in the water, not on the bank above it
        bool AtLevel(int ax, int ay, int bx, int by) => MathF.Abs(map.HeightAt(ax, ay) - map.HeightAt(bx, by)) < 0.26f;

        var wet = new Queue<(int X, int Y)>();
        foreach (var (x, y) in rocks)
            foreach (var (dx, dy) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
                if (map.InBounds(x + dx, y + dy) && map.GetGroundTile(x + dx, y + dy) == TileType.Water && AtLevel(x, y, x + dx, y + dy))
                {
                    wet.Enqueue((x, y));
                    break;
                }

        while (wet.Count > 0)
        {
            var (x, y) = wet.Dequeue();
            if (!rocks.Remove((x, y))) continue;
            map.SetGroundTile(x, y, TileType.Water, isSolid: true);
            map.SetBehaviour(x, y, TileBehavior.Sea);
            foreach (var (dx, dy) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
                if (rocks.Contains((x + dx, y + dy)) && AtLevel(x, y, x + dx, y + dy)) wet.Enqueue((x + dx, y + dy));
        }
    }

    private static MapArea AreaOf(World world, string key)
    {
        var file = world.Area(key);
        var overlay = world.Overlay(key);
        var area = new MapArea
        {
            Key = key,
            DisplayName = overlay?.Name ?? file?.Name ?? "",
            BgmTrack = overlay?.BgmTrack ?? "",
            Open = file != null && world.IsOpen(key),
            Weather = Weathers.Of(file?.Weather),
            Calendar = file?.Weather is { } named && world.Calendar.TryGetValue(named, out var days) ? days : null,
            Trees = overlay?.Trees,
            Architecture = overlay?.Architecture,
            Climate = overlay?.Climate ?? AreaClimate.Temperate,
            Arena = overlay?.BattleArena,
            BattleBackground = file?.BattleBackground ?? "",
            Kind = file?.Kind ?? "",
            BikeAllowed = file?.Bike ?? false,
            EscapeRopeAllowed = file?.EscapeRope ?? false,
            FlyAllowed = file?.Fly ?? false,
            EastSea = file?.EastSea ?? false,
            UnownTable = file?.UnownTable ?? 0
        };
        if (overlay?.EvolutionSites != null) area.EvolutionSites.AddRange(overlay.EvolutionSites);

        static void Fill(List<WildEncounterEntry> table, List<AreaEncounter>? slots, int[] weights)
        {
            for (int slot = 0; slot < (slots?.Count ?? 0); slot++)
            {
                var wild = slots![slot];
                table.Add(new WildEncounterEntry
                {
                    SpeciesName = wild.Species,
                    MinLevel = wild.Level,
                    MaxLevel = wild.MaxLevel ?? wild.Level,
                    Weight = wild.Weight ?? (slot < weights.Length ? weights[slot] : 1),
                    DaySpecies = wild.Day,
                    NightSpecies = wild.Night
                });
            }
        }
        Fill(area.WildEncounters, file?.Land, WorldAreaFile.LandSlotWeights);
        Fill(area.WaterEncounters, file?.Water, WorldAreaFile.WaterSlotWeights);
        area.LandRate = file?.LandRate ?? 0;
        // What the time of day, a swarm and the Poké Radar put in the land's slots (plan 06 · R13)
        area.DaySlots.AddRange(file?.Day ?? new());
        area.NightSlots.AddRange(file?.Night ?? new());
        area.SwarmSlots.AddRange(file?.Swarm ?? new());
        area.RadarSlots.AddRange(file?.Radar ?? new());
        // What the Pokégear's radio brings in on its days (plan 20 · J3)
        area.HoennSounds.AddRange(file?.HoennSounds ?? new());
        area.SinnohSounds.AddRange(file?.SinnohSounds ?? new());
        // Diamond's and Pearl's species of ours, once the National Pokédex is had (plan 03 · D12a)
        area.NationalSlots.AddRange(overlay?.National?.Land ?? new());
        area.NationalSuperRod.AddRange(overlay?.National?.SuperRod ?? new());
        area.WaterRate = file?.WaterRate ?? 0;
        // The rods' tables: the Old Rod's slots bite as often as the water's, the other two's by their own weights
        Fill(area.RodEncounters[(int)FishingRod.Old], file?.OldRod, WorldAreaFile.WaterSlotWeights);
        Fill(area.RodEncounters[(int)FishingRod.Good], file?.GoodRod, WorldAreaFile.RodSlotWeights);
        Fill(area.RodEncounters[(int)FishingRod.Super], file?.SuperRod, WorldAreaFile.RodSlotWeights);
        area.RodRates[(int)FishingRod.Old] = file?.OldRodRate ?? 0;
        area.RodRates[(int)FishingRod.Good] = file?.GoodRodRate ?? 0;
        area.RodRates[(int)FishingRod.Super] = file?.SuperRodRate ?? 0;
        return area;
    }

    // ---------------------------------------------------------------- tiles

    /// <summary>
    /// What the game draws for a tile of the world: its type, whether it blocks, and what stands on it. What the
    /// tile does stays its behaviour (<see cref="Map.BehaviourAt"/>): a ledge is hopped, open water waits for
    /// Surf without being blocked, and a door is shut until a warp opens it.
    /// </summary>
    public static (TileType Type, bool Solid, PropType? Prop) Look(TerrainCover cover, TileBehavior behaviour, bool solid)
    {
        switch (behaviour)
        {
            case TileBehavior.LedgeSouth:
                return (TileType.LedgeDown, false, null);
            case TileBehavior.LedgeWest:
                return (TileType.LedgeLeft, false, null);
            case TileBehavior.LedgeEast:
                return (TileType.LedgeRight, false, null);
            // The corner piece at the south end of a ledge that faces west or east: the same ridge, and a wall
            case TileBehavior.LedgeCornerSouthWest:
                return (TileType.LedgeLeft, true, null);
            case TileBehavior.LedgeCornerSouthEast:
                return (TileType.LedgeRight, true, null);
            case TileBehavior.Door:
                return (TileType.Door, true, null);   // opened when a warp is put on it
            case TileBehavior.River or TileBehavior.Sea or TileBehavior.Waterfall:
                // A blocked tile of water is a rock standing in it
                return (TileType.Water, solid, solid && cover == TerrainCover.Boulder ? PropType.Boulder : null);
            case TileBehavior.Ice:
                return (TileType.Ice, solid, null);
        }

        return cover switch
        {
            TerrainCover.Grass => (TileType.Grass, solid, null),
            TerrainCover.Flowers => (TileType.FlowerGrass, solid, null),
            TerrainCover.TallGrass => (TileType.TallGrass, solid, null),
            TerrainCover.Path => (TileType.Path, solid, null),
            TerrainCover.Paving => (TileType.Paving, solid, null),
            TerrainCover.Bridge => (TileType.Planks, solid, null),
            TerrainCover.Walkway => (TileType.Walkway, solid, null),
            TerrainCover.Steps => (TileType.Stairs, solid, null),
            TerrainCover.Sand => (TileType.Sand, solid, null),
            TerrainCover.Rock => (TileType.Rock, solid, null),
            TerrainCover.Marsh => (TileType.Marsh, solid, null),
            TerrainCover.Puddle => (TileType.Puddle, solid, null),
            TerrainCover.CaveFloor => (TileType.CaveFloor, solid, null),
            // The dark of a cave's mouth: the hole in the rock where it blocks, the way in where it is open
            TerrainCover.CaveMouth => (TileType.CaveMouth, solid, null),
            // The dark under the trees where a forest is entered, the same way round
            TerrainCover.ForestMouth => (TileType.ForestMouth, solid, null),
            TerrainCover.Snow => (TileType.Snow, solid, null),
            TerrainCover.Ice => (TileType.Ice, solid, null),
            // Water that is walked and does nothing at all, neither surfed nor waded: a walkway the original lays over a
            // canal as part of the ground's own model (the Battle Frontier's, plan 01 · M10, part b). Shallows have a
            // behaviour of their own
            TerrainCover.Water when !solid && behaviour == TileBehavior.None => (TileType.Paving, false, null),
            TerrainCover.Water => (TileType.Water, solid, null),
            // A rock face is bare rock nobody walks on; the drop beside it comes from the heights
            TerrainCover.Cliff => (TileType.Rock, true, null),
            TerrainCover.Boulder => (TileType.Dirt, true, PropType.Boulder),
            TerrainCover.Fence => (TileType.Grass, true, PropType.Fence),
            // A street lamp; the ground at its foot is whatever lies round it (PlaceModels)
            TerrainCover.Lamp => (TileType.Path, true, PropType.LampPost),
            TerrainCover.Building => (TileType.Wall, true, null),
            // Trees of either kind (the area's style says which), and whatever blocks without saying what it is;
            // open ground nobody named is lawn until its neighbours say otherwise
            _ => solid ? (TileType.Tree, true, null) : (TileType.Grass, false, null)
        };
    }

    /// <summary>
    /// What a tile is in a cave: whatever blocks the way without being water or something built is the cave's own
    /// rock (what the open country takes for a tree, a fence, a rock face), and a rock that stands on the floor
    /// stands on the cave's floor.
    /// </summary>
    public static (TileType Type, PropType? Prop) InACave(TileType type, bool solid, PropType? prop) => type switch
    {
        TileType.Tree or TileType.Rock when solid => (TileType.CaveWall, null),
        TileType.Grass when solid => (TileType.CaveWall, null),
        // Seen from inside, a way out is the floor that leads to it and the rock round it
        TileType.CaveMouth => (solid ? TileType.CaveWall : TileType.CaveFloor, null),
        TileType.Dirt when prop == PropType.Boulder => (TileType.CaveFloor, prop),
        _ => (type, prop)
    };

    /// <summary>Behaviours whose plates lie one over the other: the deck of a bridge, and the ground or water under it.</summary>
    private static bool HasDeck(TileBehavior behaviour) => behaviour is TileBehavior.Bridge or TileBehavior.BridgeEnd
        or TileBehavior.BridgeOverCave or TileBehavior.BridgeOverWater or TileBehavior.BridgeOverSnow
        || FieldMovement.BikePlankRunsNorthSouth(behaviour) != null;

    /// <param name="altitude">How high the matrix sets the whole chunk, in tiles.</param>
    /// <param name="under">Marks the tiles whose look is settled later, by the model that stands on them.</param>
    /// <param name="vague">Gathers the open tiles of ground nobody named.</param>
    /// <param name="blank">Gathers the blocked tiles the original draws nothing on.</param>
    private static void PlaceChunk(Map map, WorldChunkFile chunk, int ox, int oy, float altitude, MapArea area, bool[] under,
        List<(int X, int Z)> vague, List<(int X, int Z)> blank, MapArea?[]? own = null)
    {
        var chunkArea = area;
        for (int z = 0; z < T; z++)
            for (int x = 0; x < T; x++)
            {
                area = own?[z * T + x] ?? chunkArea;
                var cover = chunk.CoverAt(x, z);
                var behaviour = chunk.BehaviourAt(x, z);
                bool solid = chunk.SolidAt(x, z);
                map.SetBehaviour(ox + x, oy + z, behaviour);
                PlaceHeight(map, chunk, x, z, ox, oy, altitude, behaviour);

                var (type, blocks, prop) = Look(cover, behaviour, solid);
                // Soft soil is blocked by the patch that stands on it (an object of the area, plan 06 · R14a), not by
                // the tile: its ground is the ground round it, and nothing grows on it but what is planted
                if (behaviour == TileBehavior.BerrySoil)
                {
                    blocks = false;
                    prop = null;
                    if (cover is TerrainCover.Unknown or TerrainCover.Tree or TerrainCover.Broadleaf)
                    {
                        type = TileType.Grass;
                        vague.Add((ox + x, oy + z));
                    }
                }
                if (map.IsCave) (type, prop) = InACave(type, blocks, prop);
                else if (map.IsVoid) (type, blocks, prop) = InTheVoid(cover, behaviour, solid, type, blocks, prop);
                // A room's blocked texture that the cover's rules take for a fence (a rail along a DS room's back wall:
                // Route 34's Day Care, Goldenrod's Underground, plan 20 · J5) is the room's wall, which builds no fence
                else if (map.IsIndoors && prop == PropType.Fence) prop = null;
                // An area that isn't built yet is scenery: seen from its neighbours, entered by nobody
                map.SetGroundTile(ox + x, oy + z, type, blocks || !area.Open);
                if (cover is TerrainCover.Building or TerrainCover.Lamp && solid) under[(oy + z) * map.Width + ox + x] = true;
                if (cover == TerrainCover.Unknown && !solid && type == TileType.Grass) vague.Add((ox + x, oy + z));
                if (cover == TerrainCover.Unknown && solid && type == TileType.Tree) blank.Add((ox + x, oy + z));
                if (prop is { } standing && (blocks || !area.Open)) map.Props.Add(new Prop { Type = standing, X = ox + x, Y = oy + z });
            }
    }

    /// <summary>
    /// A tile's ground from the chunk's plates: the plate under its middle gives its height and slope. Where two
    /// lie one over the other at a bridge, the lower is the ground and the upper the deck.
    /// </summary>
    private static void PlaceHeight(Map map, WorldChunkFile chunk, int x, int z, int ox, int oy, float altitude, TileBehavior behaviour)
    {
        float mx = x + 0.5f, mz = z + 0.5f;
        HeightPlate? ground = null, upper = null;
        foreach (var plate in chunk.Heights)
        {
            if (!plate.Contains(mx, mz)) continue;
            float h = plate.HeightAt(mx, mz);
            if (ground == null || h < ground.HeightAt(mx, mz)) ground = plate;
            if (upper == null || h > upper.HeightAt(mx, mz)) upper = plate;
        }
        if (ground == null) return;

        // Where plates overlap without a bridge the walker's own is the upper one (a terrace over a hidden floor)
        bool deck = HasDeck(behaviour) && upper != ground && upper!.HeightAt(mx, mz) - ground.HeightAt(mx, mz) >= 0.75f;
        var stand = deck ? ground : upper!;
        map.SetHeight(ox + x, oy + z, altitude + stand.HeightAt(mx, mz), stand.SlopeX, stand.SlopeZ);
        if (deck) map.SetDeck(ox + x, oy + z, altitude + upper!.HeightAt(mx, mz));
    }

    // ---------------------------------------------------------------- models: buildings and what stands about

    /// <summary>
    /// A prop as large as a building, and not a sheet the size of the whole chunk (a lake's water): what a model
    /// the catalogue doesn't know is taken for.
    /// </summary>
    public static bool IsBuilding(ChunkProp prop) =>
        prop.Width >= 2.5f && prop.Depth >= 2f && prop.Height >= 1.25f && !(prop.Width > T - 1 && prop.Depth > T - 1);

    /// <summary>One of the original's models where it stands on the map: its box starts at (X, Z), in tiles.</summary>
    private sealed record Placed(ChunkProp Prop, float X, float Z, WorldModel? Model, MapArea Area)
    {
        public float X1 => X + Prop.Width;
        public float Z1 => Z + Prop.Depth;

        /// <summary>Whether a tile's middle lies in the box.</summary>
        public bool Covers(int tx, int tz) => tx + 0.5f > X && tx + 0.5f < X1 && tz + 0.5f > Z && tz + 0.5f < Z1;

        public bool IsBuilding => Model != null ? Model.Role == ModelRole.Building : WorldMapBuilder.IsBuilding(Prop);
        public bool Stands => IsBuilding || Model?.Role == ModelRole.Scenery;
    }

    /// <summary>One block of a building: a rectangle of the tiles its model blocks, and for the main block its porch.</summary>
    private sealed record Part(Placed Of, int X0, int Z0, int X1, int Z1, bool Annex)
    {
        /// <summary>The blocked tiles (their x) of an entrance on the row before the front wall.</summary>
        public List<int> Porch { get; } = new();

        /// <summary>A low platform before a building whose doors are reached along a passage (<see cref="WorldModel.Hollow"/>), not a wing with walls and a roof.</summary>
        public bool Forecourt { get; init; }
    }

    /// <summary>Towns whose yards are fenced in wood; elsewhere what a model leaves standing about is a low wall.</summary>
    private static bool FencesInWood(Architecture? town) =>
        town is Architecture.Timber or Architecture.Plaster or Architecture.Clapboard or Architecture.Cottage or Architecture.Farm
            or Architecture.Marsh or Architecture.Resort or Architecture.HalfTimber or Architecture.Evergreen or Architecture.Breeze
            or Architecture.Bayside or Architecture.Sapling or Architecture.Meadow or Architecture.Woodland or Architecture.Heritage or Architecture.Islet;

    /// <summary>
    /// Cuts a set of tiles into rectangles, the largest first: a house is one, a museum with a wing two. Ties go
    /// to the one further north, then west, then the wider, so the same tiles always give the same blocks.
    /// </summary>
    public static List<(int X0, int Z0, int X1, int Z1)> Rectangles(IEnumerable<(int X, int Z)> tiles)
    {
        var left = new HashSet<(int X, int Z)>(tiles);
        var result = new List<(int X0, int Z0, int X1, int Z1)>();
        while (left.Count > 0)
        {
            (int X0, int Z0, int X1, int Z1) best = default;
            int bestArea = 0;
            foreach (var (x0, z0) in left)
            {
                int reach = int.MaxValue;
                for (int z = z0; left.Contains((x0, z)); z++)
                {
                    int x = x0;
                    while (x < reach && left.Contains((x + 1, z))) x++;
                    reach = x;
                    int area = (reach - x0 + 1) * (z - z0 + 1);
                    bool better = area > bestArea
                        || area == bestArea && (z0 < best.Z0 || z0 == best.Z0 && (x0 < best.X0 || x0 == best.X0 && reach > best.X1));
                    if (!better) continue;
                    best = (x0, z0, reach, z);
                    bestArea = area;
                }
            }
            result.Add(best);
            for (int z = best.Z0; z <= best.Z1; z++)
                for (int x = best.X0; x <= best.X1; x++)
                    left.Remove((x, z));
        }
        return result;
    }

    /// <summary>Open tiles of a cave's mouth with no way in on them or beside them.</summary>
    private static List<(int X, int Z)> OpenMouthsLeadingNowhere(Map map, HashSet<(int X, int Z)> entrances)
    {
        var found = new List<(int X, int Z)>();
        for (int z = 0; z < map.Height; z++)
            for (int x = 0; x < map.Width; x++)
            {
                if (map.GetGroundTile(x, z) != TileType.CaveMouth || map.IsSolid(x, z)) continue;
                bool leads = false;
                for (int dz = -1; dz <= 1 && !leads; dz++)
                    for (int dx = -1; dx <= 1 && !leads; dx++)
                        leads = entrances.Contains((x + dx, z + dz));
                if (!leads) found.Add((x, z));
            }
        return found;
    }

    /// <summary>How many tiles of open ground shut in under a house may be and still be a pocket of the house.</summary>
    public const int PocketTiles = 2;

    /// <summary>The widest and deepest a model's box may be, in tiles, for a pocket to make one block of it: a house, not a museum.</summary>
    public const int HouseWidth = 6, HouseDepth = 4;

    /// <summary>
    /// The open tiles under a building's box that no way leads out of: each patch of open ground there which stays
    /// within two tiles of the box, however one walks it. A passage that runs out into the grounds is no such patch.
    /// </summary>
    /// <param name="most">The most tiles a patch may have to count: a pocket is a tile or two, a hall's inside as big as it is.</param>
    private static List<(int X, int Z)> Enclosed(Map map, Placed model, Func<int, int, bool> blocked, int most = int.MaxValue)
    {
        int x0 = (int)MathF.Floor(model.X), x1 = (int)MathF.Floor(model.X1), z0 = (int)MathF.Floor(model.Z), z1 = (int)MathF.Floor(model.Z1);
        var seen = new HashSet<(int X, int Z)>();
        var enclosed = new List<(int X, int Z)>();
        for (int z = z0; z <= z1; z++)
            for (int x = x0; x <= x1; x++)
            {
                if (!map.InBounds(x, z) || !model.Covers(x, z) || blocked(x, z) || !seen.Add((x, z))) continue;
                var patch = new List<(int X, int Z)>();
                var open = new Queue<(int X, int Z)>();
                open.Enqueue((x, z));
                bool leadsOut = false;
                while (open.Count > 0)
                {
                    var (cx, cz) = open.Dequeue();
                    patch.Add((cx, cz));
                    foreach (var (dx, dz) in new[] { (0, -1), (1, 0), (-1, 0), (0, 1) })
                    {
                        int nx = cx + dx, nz = cz + dz;
                        if (!map.InBounds(nx, nz) || blocked(nx, nz)) continue;
                        if (nx < x0 - 2 || nx > x1 + 2 || nz < z0 - 2 || nz > z1 + 2) { leadsOut = true; continue; }
                        if (seen.Add((nx, nz))) open.Enqueue((nx, nz));
                    }
                }
                if (!leadsOut && patch.Count <= most) enclosed.AddRange(patch.Where(t => model.Covers(t.X, t.Z)));
            }
        return enclosed;
    }

    /// <summary>
    /// The largest rectangle of a building's tiles that ends on the row before its passage and holds the tile behind
    /// one of its doors: the building proper, with its doors in its front.
    /// </summary>
    private static (int X0, int Z0, int X1, int Z1)? FrontBlock(List<(int X, int Z)> tiles, int front, HashSet<(int X, int Z)> entrances)
    {
        var set = tiles.ToHashSet();
        (int X0, int Z0, int X1, int Z1)? best = null;
        int bestArea = 0;
        foreach (var (ax, _) in tiles.Where(t => t.Z == front && entrances.Contains((t.X, front + 1))))
        {
            int x0 = int.MinValue, x1 = int.MaxValue;
            for (int z = front; set.Contains((ax, z)); z--)
            {
                int left = ax, right = ax;
                while (set.Contains((left - 1, z))) left--;
                while (set.Contains((right + 1, z))) right++;
                x0 = Math.Max(x0, left);
                x1 = Math.Min(x1, right);
                int area = (x1 - x0 + 1) * (front - z + 1);
                if (x1 > x0 && z < front && area > bestArea)
                {
                    bestArea = area;
                    best = (x0, z, x1, front);
                }
            }
        }
        return best;
    }

    /// <summary>The first row, from the north, on which a way into a building lies under or beside its box; null if it has none.</summary>
    private static int? PassageRow(Placed model, HashSet<(int X, int Z)> entrances)
    {
        int x0 = (int)MathF.Floor(model.X) - 1, x1 = (int)MathF.Floor(model.X1) + 1, z0 = (int)MathF.Floor(model.Z), z1 = (int)MathF.Floor(model.Z1);
        int? row = null;
        foreach (var (x, z) in entrances)
            if (x >= x0 && x <= x1 && z >= z0 && z <= z1 && (row == null || z < row)) row = z;
        return row;
    }

    /// <summary>
    /// The ground a tile under a model takes: that of the nearest tile round it which no model stands on, so a
    /// lamp in a paved street stands on paving and a ship in the harbour on water.
    /// </summary>
    /// <param name="dry">Whether water round the tile is passed over: the tile is walked, so it lies on something.</param>
    private static TileType GroundLike(Map map, int x, int z, bool[] under, bool[]? unnamed = null, bool dry = false)
    {
        for (int reach = 1; reach <= 4; reach++)
        {
            // Made or bare ground at this distance wins over lawn at the same distance: a heap of coal between
            // a paved yard and a row of trees lies on the paving
            bool lawn = false;
            foreach (var (dx, dz) in new[] { (0, reach), (-reach, 0), (reach, 0), (0, -reach), (-reach, reach), (reach, reach), (-reach, -reach), (reach, -reach) })
            {
                int nx = x + dx, nz = z + dz;
                if (!map.InBounds(nx, nz) || under[nz * map.Width + nx] || unnamed?[nz * map.Width + nx] == true) continue;
                if (dry && map.GetGroundTile(nx, nz) == TileType.Water) continue;
                switch (map.GetGroundTile(nx, nz))
                {
                    case TileType.Path or TileType.Paving or TileType.Sand or TileType.Snow or TileType.Dirt or TileType.Rock or TileType.Water
                        or TileType.Planks or TileType.Ice or TileType.Marsh or TileType.CaveFloor:
                        return map.GetGroundTile(nx, nz);
                    case TileType.Grass or TileType.FlowerGrass or TileType.TallGrass or TileType.Tree:
                        lawn = true;
                        break;
                }
            }
            if (lawn) return TileType.Grass;
        }
        return TileType.Grass;
    }

    /// <summary>
    /// Gives every model its place on the map. A building's model takes the tiles the world blocks under its
    /// box, cut into rectangles: each rectangle two tiles or more each way is a block of the building (roof,
    /// with a row of wall along its front, and its doors). Thin pieces on the row before the main block's
    /// front, beside a way in, are its porch; any others are a fence or a low wall. Something that only stands
    /// there becomes a prop over its box. Returns the buildings' blocks.
    /// </summary>
    private static List<Part> PlaceModels(World world, int matrixId, Map map, List<Placed> models, bool[] under, bool[] unnamed, HashSet<(int X, int Z)> entrances)
    {
        bool Blocked(int x, int z) => world.ChunkAt(matrixId, x, z) is { } at && at.Chunk.SolidAt(at.X, at.Z);

        // Every blocked tile under a model's box is that model's; where boxes overlap (a Pokémon Center inside
        // the box of a museum's forecourt) it is the smaller model's
        var owner = new Dictionary<(int X, int Z), Placed>();
        foreach (var model in models.Where(m => m.Stands).OrderBy(m => m.Prop.Width * m.Prop.Depth))
            for (int z = (int)MathF.Floor(model.Z); z <= (int)MathF.Floor(model.Z1); z++)
                for (int x = (int)MathF.Floor(model.X); x <= (int)MathF.Floor(model.X1); x++)
                    if (map.InBounds(x, z) && model.Covers(x, z) && Blocked(x, z) && !owner.ContainsKey((x, z))) owner[(x, z)] = model;

        // A building whose outline alone the original blocks (the Battle Frontier's halls, plan 01 · M10, part b) has
        // its inside too: the open tiles under its box that no way leads out of are its own, and blocked from here on
        foreach (var model in models.Where(m => m.Model is { Role: ModelRole.Building, Hollow: true }))
            foreach (var (x, z) in Enclosed(map, model, Blocked))
            {
                owner.TryAdd((x, z), model);
                map.SetSolid(x, z, true);
            }

        // A pocket in a house (plan 01 · M11, part c): the original leaves a tile or two open in the middle of some
        // houses (Route 225's, the Survival Area's two and its Battleground, Route 212's), shut in by the house's own
        // walls, where nobody can come and no way leads. Cut into rectangles round the pocket, such a house fell apart
        // into a small block, a fence and a door under the fence, or into two halves with a gap between: a building no
        // bigger than a house that shuts a pocket in is one block, the box of the tiles it blocks
        var pocketed = models.Where(m => m.Stands && m.IsBuilding && m.Model?.Hollow != true && m.Prop.Width <= HouseWidth && m.Prop.Depth <= HouseDepth
            && Enclosed(map, m, Blocked, most: PocketTiles).Any(t => !entrances.Contains(t))).ToHashSet<Placed>(ReferenceEqualityComparer.Instance);

        foreach (var (x, z) in owner.Keys) under[z * map.Width + x] = true;
        // What the ground's cover guessed at such a tile (a boulder, a fence) gives way to the model
        map.Props.RemoveAll(prop => prop.Type is PropType.Boulder or PropType.Fence && owner.ContainsKey((prop.X, prop.Y)));

        bool WayIn(int x, int z) => map.BehaviourAt(x, z) == TileBehavior.Door || entrances.Contains((x, z));

        var parts = new List<Part>();
        var inPart = new HashSet<(int X, int Z)>();
        var fenced = new List<(int X, int Z, bool Wood)>();
        var conveyed = new List<(int X, int Z)>();
        foreach (var group in owner.GroupBy(kv => kv.Value, ReferenceEqualityComparer.Instance).Select(g => ((Placed)g.Key!, g.Select(kv => kv.Key).ToList())))
        {
            var (model, tiles) = group;
            if (!model.IsBuilding) continue;

            // Such a building's doors are reached along a passage between its wings. What it blocks from the passage's
            // first row south is a low forecourt, so the way in stays in sight, and the building stands north of it
            int? front = null;
            if (model.Model?.Hollow == true && PassageRow(model, entrances) is { } passage)
            {
                front = passage - 1;
                foreach (var r in Rectangles(tiles.Where(t => t.Z >= passage)))
                {
                    parts.Add(new Part(model, r.X0, r.Z0, r.X1, r.Z1, Annex: true) { Forecourt = true });
                    for (int z = r.Z0; z <= r.Z1; z++)
                        for (int x = r.X0; x <= r.X1; x++)
                        {
                            if (!map.InBounds(x, z)) continue;
                            inPart.Add((x, z));
                            map.SetGroundTile(x, z, z == r.Z1 ? TileType.Wall : TileType.RoofRed, isSolid: true);
                        }
                }
                tiles = tiles.Where(t => t.Z < passage).ToList();
                if (tiles.Count == 0) continue;
            }

            var blocks = new List<(int X0, int Z0, int X1, int Z1)>();
            var thin = new HashSet<(int X, int Z)>();
            // The building proper is the largest block that ends on the row before the passage with a door behind it
            if (front is { } row0 && FrontBlock(tiles, row0, entrances) is { } whole)
            {
                blocks.Add(whole);
                tiles = tiles.Where(t => t.X < whole.X0 || t.X > whole.X1 || t.Z < whole.Z0 || t.Z > whole.Z1).ToList();
            }
            foreach (var r in Rectangles(tiles))
            {
                if (r.X1 > r.X0 && r.Z1 > r.Z0) { blocks.Add(r); continue; }
                for (int z = r.Z0; z <= r.Z1; z++)
                    for (int x = r.X0; x <= r.X1; x++)
                        thin.Add((x, z));
            }
            if (blocks.Count == 0 || pocketed.Contains(model))
            {
                // Nothing two tiles each way (a house whose middle the world leaves open), or a pocket shut in: the whole
                // of it is one block
                blocks.Clear();
                var (bx0, bz0, bx1, bz1) = (tiles.Min(t => t.X), tiles.Min(t => t.Z), tiles.Max(t => t.X), tiles.Max(t => t.Z));
                if (bx1 == bx0 || bz1 == bz0) continue;
                blocks.Add((bx0, bz0, bx1, bz1));
                thin.Clear();
            }

            // The main block is the one with the way in (in its front row, or on the row before it), else the largest
            bool Entered((int X0, int Z0, int X1, int Z1) r) =>
                Enumerable.Range(r.X0, r.X1 - r.X0 + 1).Any(x => WayIn(x, r.Z1) || WayIn(x, r.Z1 + 1) && !blocks.Any(o => o != r && x >= o.X0 && x <= o.X1 && r.Z1 + 1 >= o.Z0 && r.Z1 + 1 <= o.Z1));
            var main = blocks.Any(Entered) ? blocks.First(Entered) : blocks[0];
            foreach (var r in blocks)
            {
                var part = new Part(model, r.X0, r.Z0, r.X1, r.Z1, Annex: r != main);
                parts.Add(part);
                for (int z = r.Z0; z <= r.Z1; z++)
                    for (int x = r.X0; x <= r.X1; x++)
                    {
                        if (!map.InBounds(x, z)) continue;
                        inPart.Add((x, z));
                        var type = map.BehaviourAt(x, z) == TileBehavior.Door ? TileType.Door : z == r.Z1 ? TileType.Wall : TileType.RoofRed;
                        map.SetGroundTile(x, z, type, isSolid: true);
                    }
                if (r != main) continue;

                // Its porch: on the row before the front, the thin pieces that touch a way in, or hold one
                int row = r.Z1 + 1;
                bool OfPorch(int x) => x >= r.X0 && x <= r.X1 && (thin.Contains((x, row)) || WayIn(x, row));
                for (int x = r.X0; x <= r.X1; x++)
                {
                    if (!WayIn(x, row)) continue;
                    for (int dir = -1; dir <= 1; dir += 2)
                        for (int px = x; OfPorch(px); px += dir)
                            if (thin.Remove((px, row)))
                            {
                                part.Porch.Add(px);
                                inPart.Add((px, row));
                                map.SetGroundTile(px, row, map.BehaviourAt(px, row) == TileBehavior.Door ? TileType.Door : TileType.Wall, isSolid: true);
                            }
                }
                part.Porch.Sort();
            }
            // A yard's thin pieces carry its conveyors; anyone else's are a fence or a low wall
            if (model.Model?.Thin == PropType.Conveyor)
            {
                conveyed.AddRange(thin);
                continue;
            }
            bool wood = FencesInWood(model.Model?.Town);
            foreach (var (x, z) in thin) fenced.Add((x, z, wood));
        }

        // Everything else a model stands on shows the ground round it; a ship lies in the water
        for (int z = 0; z < map.Height; z++)
            for (int x = 0; x < map.Width; x++)
            {
                if (!under[z * map.Width + x] || inPart.Contains((x, z)) || map.GetGroundTile(x, z) == TileType.Door) continue;
                bool afloat = owner.TryGetValue((x, z), out var on) && on.Model is { Role: ModelRole.Scenery, Prop: PropType.Boat };
                map.SetGroundTile(x, z, afloat ? TileType.Water : GroundLike(map, x, z, under, unnamed), isSolid: true);
            }
        foreach (var (x, z, wood) in fenced) map.Props.Add(new Prop { Type = wood ? PropType.Fence : PropType.LowWall, X = x, Y = z });
        map.Props.AddRange(Conveyors(conveyed, (x, z) => !map.InBounds(x, z) || Blocked(x, z)));

        foreach (var model in models)
        {
            if (model.Model is not { Role: ModelRole.Scenery } scenery) continue;
            // The tiles whose middle lies in the box; a thing smaller than a tile stands on the one under its middle
            int x0 = (int)MathF.Floor(model.X - 0.5f) + 1, x1 = (int)MathF.Ceiling(model.X1 - 0.5f) - 1;
            int z0 = (int)MathF.Floor(model.Z - 0.5f) + 1, z1 = (int)MathF.Ceiling(model.Z1 - 0.5f) - 1;
            if (x1 < x0) x0 = x1 = (int)MathF.Floor((model.X + model.X1) / 2f);
            if (z1 < z0) z0 = z1 = (int)MathF.Floor((model.Z + model.Z1) / 2f);
            // Boats moored side by side whose boxes overlap (Pastoria's two) each keep their own side of the line
            // halfway between them, or their decks would run together into one slab
            if (scenery.Prop == PropType.Boat)
                foreach (var other in models)
                {
                    if (ReferenceEquals(other, model) || other.Model?.Prop != PropType.Boat
                        || other.X >= model.X1 || model.X >= other.X1 || other.Z >= model.Z1 || model.Z >= other.Z1) continue;
                    float mx = (model.X + model.X1) / 2f, mz = (model.Z + model.Z1) / 2f, ox = (other.X + other.X1) / 2f, oz = (other.Z + other.Z1) / 2f;
                    // A tile is this boat's when its middle lies on this boat's side of the line
                    if (MathF.Abs(mz - oz) >= MathF.Abs(mx - ox))
                    {
                        float line = (mz + oz) / 2f;
                        if (mz < oz) z1 = Math.Min(z1, (int)MathF.Ceiling(line - 0.5f) - 1);
                        else z0 = Math.Max(z0, (int)MathF.Ceiling(line - 0.5f));
                    }
                    else
                    {
                        float line = (mx + ox) / 2f;
                        if (mx < ox) x1 = Math.Min(x1, (int)MathF.Ceiling(line - 0.5f) - 1);
                        else x0 = Math.Max(x0, (int)MathF.Ceiling(line - 0.5f));
                    }
                }
            // A ship moored at a pier keeps off its planks: the original's box reaches over the row of the pier the
            // gangway lies on (the Fight Area's ferry), where people stand and walk
            if (scenery.Prop == PropType.Boat)
            {
                bool Pier(int x, int z) => map.InBounds(x, z) && !Blocked(x, z) && map.GetGroundTile(x, z) == TileType.Planks;
                bool RowOnPier(int z) => Enumerable.Range(x0, x1 - x0 + 1).Count(x => Pier(x, z)) * 2 > x1 - x0 + 1;
                bool ColumnOnPier(int x) => Enumerable.Range(z0, z1 - z0 + 1).Count(z => Pier(x, z)) * 2 > z1 - z0 + 1;
                while (z1 > z0 && RowOnPier(z0)) z0++;
                while (z1 > z0 && RowOnPier(z1)) z1--;
                while (x1 > x0 && ColumnOnPier(x0)) x0++;
                while (x1 > x0 && ColumnOnPier(x1)) x1--;
            }
            // A stack of rock in the sea stands on the rock the world blocks: Sunyshore's box lies two rows south of
            // its blocked tiles, over water a swimmer crosses. It takes the blocked tiles joined to those under its box
            if (scenery.Prop == PropType.SeaStack) (x0, z0, x1, z1) = BlockedRound(x0, z0, x1, z1, Blocked);
            if (!map.InBounds(x0, z0)) continue;
            // A swimming pool is set in paving from edge to edge, so no lawn grows up through its water
            if (scenery.Prop == PropType.Pool)
                for (int z = z0; z <= z1; z++)
                    for (int x = x0; x <= x1; x++)
                        if (map.InBounds(x, z)) map.SetGroundTile(x, z, TileType.Paving, map.IsSolid(x, z));
            map.Props.Add(new Prop
            {
                Type = scenery.Prop, X = x0, Y = z0, Width = x1 - x0 + 1, Depth = z1 - z0 + 1,
                Height = model.Prop.Height, Model = model.Prop.Name
            });
        }
        return parts;
    }

    /// <summary>
    /// The rectangle of the blocked tiles joined to those inside a rectangle, reaching no more than
    /// <see cref="BlockedReach"/> tiles past it; the rectangle itself when none of it is blocked.
    /// </summary>
    public static (int X0, int Z0, int X1, int Z1) BlockedRound(int x0, int z0, int x1, int z1, Func<int, int, bool> blocked)
    {
        var seen = new HashSet<(int X, int Z)>();
        var open = new Queue<(int X, int Z)>();
        for (int z = z0; z <= z1; z++)
            for (int x = x0; x <= x1; x++)
                if (blocked(x, z) && seen.Add((x, z))) open.Enqueue((x, z));
        if (seen.Count == 0) return (x0, z0, x1, z1);
        while (open.Count > 0)
        {
            var (x, z) = open.Dequeue();
            foreach (var (nx, nz) in new[] { (x + 1, z), (x - 1, z), (x, z + 1), (x, z - 1) })
                if (nx >= x0 - BlockedReach && nx <= x1 + BlockedReach && nz >= z0 - BlockedReach && nz <= z1 + BlockedReach
                    && blocked(nx, nz) && seen.Add((nx, nz)))
                    open.Enqueue((nx, nz));
        }
        return (seen.Min(t => t.X), seen.Min(t => t.Z), seen.Max(t => t.X), seen.Max(t => t.Z));
    }

    /// <summary>How many rows of a swimmer are under the water of a pool they stand in: to the chest.</summary>
    public const int PoolDepth = 10;

    /// <summary>How far past its box a sea stack's blocked rock is looked for, in tiles.</summary>
    public const int BlockedReach = 3;

    /// <summary>How far a conveyor reaches from a gantry, in tiles, toward what feeds it or what it fills.</summary>
    public const int ConveyorReach = 8;

    /// <summary>
    /// Lays a yard's conveyors from the thin pieces its model blocks (plan 01 · M5; style guide, "The mine").
    /// A straight run of three pieces or more carries a belt along itself. Two pieces that face each other
    /// across one open tile are the legs of a gantry, and the belt passes between them, across the pair: it
    /// runs on either way over the open ground until something blocked takes it up (a shed, a heap of coal, a
    /// run that carries it on), if that is within <see cref="ConveyorReach"/>. Whatever is left is a pier
    /// standing alone. Returns the belts (<see cref="PropType.Conveyor"/>, one tile wide and as long as they
    /// run) and what carries them (<see cref="PropType.Gantry"/>).
    /// </summary>
    public static List<Prop> Conveyors(IEnumerable<(int X, int Z)> thin, Func<int, int, bool> blocked)
    {
        var pieces = new HashSet<(int X, int Z)>(thin);
        var props = new List<Prop>();
        if (pieces.Count == 0) return props;

        int Run(int x, int z, int dx, int dz)
        {
            int n = 1;
            for (int i = 1; pieces.Contains((x + dx * i, z + dz * i)); i++) n++;
            for (int i = 1; pieces.Contains((x - dx * i, z - dz * i)); i++) n++;
            return n;
        }

        // Gantries first: a pair east and west of an open tile (the commoner), then north and south of one
        var legs = new HashSet<(int X, int Z)>();
        var through = new List<(int X, int Z, bool NorthSouth)>();
        var ordered = pieces.OrderBy(p => p.Z).ThenBy(p => p.X).ToList();
        foreach (var (x, z) in ordered)
            if (pieces.Contains((x + 2, z)) && !blocked(x + 1, z))
            {
                legs.Add((x, z));
                legs.Add((x + 2, z));
                through.Add((x + 1, z, true));
            }
        foreach (var (x, z) in ordered)
            if (!legs.Contains((x, z)) && pieces.Contains((x, z + 2)) && !legs.Contains((x, z + 2)) && !blocked(x, z + 1))
            {
                legs.Add((x, z));
                legs.Add((x, z + 2));
                through.Add((x, z + 1, false));
            }

        // The belts, tile by tile: true where one runs north and south. Of what is left, a straight run of three
        // or more carries one along itself
        var belt = new Dictionary<(int X, int Z), bool>();
        var carriers = new HashSet<(int X, int Z)>();
        pieces.ExceptWith(legs);
        foreach (var (x, z) in pieces)
        {
            int ns = Run(x, z, 0, 1), ew = Run(x, z, 1, 0);
            if (Math.Max(ns, ew) < 3) continue;
            carriers.Add((x, z));
            belt[(x, z)] = ns >= ew;
        }

        // One gantry for each row of such pairs, and the belt through it, as far as it finds something to meet
        foreach (var group in through.GroupBy(t => (t.NorthSouth, Line: t.NorthSouth ? t.X : t.Z)))
        {
            bool ns = group.Key.NorthSouth;
            var along = group.Select(t => ns ? t.Z : t.X).OrderBy(v => v).ToList();
            for (int i = 0; i < along.Count;)
            {
                int j = i;
                while (j + 1 < along.Count && along[j + 1] == along[j] + 1) j++;
                int from = along[i], to = along[j], line = group.Key.Line;
                props.Add(ns
                    ? new Prop { Type = PropType.Gantry, X = line - 1, Y = from, Width = 3, Depth = to - from + 1 }
                    : new Prop { Type = PropType.Gantry, X = from, Y = line - 1, Width = to - from + 1, Depth = 3 });
                for (int v = from; v <= to; v++) belt[ns ? (line, v) : (v, line)] = ns;

                foreach (int step in new[] { -1, 1 })
                {
                    var reach = new List<(int X, int Z)>();
                    bool met = false;
                    for (int n = 1; n <= ConveyorReach + 1; n++)
                    {
                        int v = (step < 0 ? from : to) + step * n;
                        var tile = ns ? (line, v) : (v, line);
                        if (blocked(tile.Item1, tile.Item2) || belt.ContainsKey(tile)) { met = true; break; }
                        reach.Add(tile);
                    }
                    if (met && reach.Count <= ConveyorReach)
                        foreach (var tile in reach) belt[tile] = ns;
                }
                i = j + 1;
            }
        }

        foreach (var (x, z) in pieces.Where(p => !carriers.Contains(p)).OrderBy(p => p.Z).ThenBy(p => p.X))
            props.Add(new Prop { Type = PropType.Gantry, X = x, Y = z });

        // The belts as runs
        foreach (var column in belt.Where(b => b.Value).GroupBy(b => b.Key.X).OrderBy(g => g.Key))
        {
            var rows = column.Select(b => b.Key.Z).OrderBy(v => v).ToList();
            for (int i = 0; i < rows.Count;)
            {
                int j = i;
                while (j + 1 < rows.Count && rows[j + 1] == rows[j] + 1) j++;
                props.Add(new Prop { Type = PropType.Conveyor, X = column.Key, Y = rows[i], Width = 1, Depth = rows[j] - rows[i] + 1 });
                i = j + 1;
            }
        }
        foreach (var row in belt.Where(b => !b.Value).GroupBy(b => b.Key.Z).OrderBy(g => g.Key))
        {
            var columns = row.Select(b => b.Key.X).OrderBy(v => v).ToList();
            for (int i = 0; i < columns.Count;)
            {
                int j = i;
                while (j + 1 < columns.Count && columns[j + 1] == columns[j] + 1) j++;
                props.Add(new Prop { Type = PropType.Conveyor, X = columns[i], Y = row.Key, Width = columns[j] - columns[i] + 1, Depth = 1 });
                i = j + 1;
            }
        }
        return props;
    }

    /// <summary>The building a block of a model is: what kind, how it is built, and where its doors are.</summary>
    private static BuildingInfo ToBuilding(Map map, Part part, HashSet<(int X, int Z)> entrances)
    {
        var model = part.Of.Model;
        var kind = model?.Kind ?? BuildingKind.House;
        var info = new BuildingInfo
        {
            X0 = part.X0, Y0 = part.Z0, X1 = part.X1, Y1 = part.Z1,
            Kind = kind, RoofTile = TileType.RoofRed,
            Model = part.Of.Prop.Name,
            Town = model?.Town,
            Storeys = part.Annex ? 1 : model?.Storeys ?? 0,
            Sign = part.Annex ? null : model?.Sign,
            Theme = kind == BuildingKind.Gym ? WorldModels.GymTheme(part.Of.Area.Key) : null,
            Annex = part.Annex,
            Forecourt = part.Forecourt,
            Height = part.Annex ? 0f : part.Of.Prop.Height
        };
        if (part.Annex) return info;

        info.Porch.AddRange(part.Porch);
        int row = part.Z1 + 1;
        for (int x = part.X0; x <= part.X1; x++)
        {
            // In the wall itself; or on the row before it: in the porch's front, or open ground between its sides
            if (map.GetGroundTile(x, part.Z1) == TileType.Door) info.Doors.Add((x, map.GetWarpAt(x, part.Z1)?.TargetMap));
            else if (map.InBounds(x, row) && (map.GetGroundTile(x, row) == TileType.Door && part.Porch.Contains(x)
                         || entrances.Contains((x, row)) && !MapStructures.IsBuildingTile(map, x, row)))
            {
                info.Doors.Add((x, map.GetWarpAt(x, row)?.TargetMap));
                if (!part.Porch.Contains(x)) info.PorchIsOpen = true;
            }
        }

        // A gate house on a road that runs east and west is entered through its ends
        for (int z = part.Z0; z <= part.Z1; z++)
            if (entrances.Contains((part.X0 - 1, z)) || entrances.Contains((part.X1 + 1, z)) || map.BehaviourAt(part.X0, z) == TileBehavior.Door || map.BehaviourAt(part.X1, z) == TileBehavior.Door)
                info.SideDoors.Add(z);
        return info;
    }

    // ---------------------------------------------------------------- people, signs, doors

    /// <summary>
    /// Which of our looks plays one of the original's people, by the name of their looks: their own when
    /// <c>Data/characters.json</c> has it, else the one it stands as for now (plan 11 · C1, <see cref="CharacterStyles.CharacterFor"/>).
    /// </summary>
    public static string CharacterFor(string looks) => CharacterStyles.Default.CharacterFor(looks);

    /// <summary>
    /// The species (or form) one of the original's objects is, by the name of its looks, where it is a Pokémon
    /// standing in the field (plan 10 · F1): the mine's Machop, the Windworks' Drifloon, the lake guardians. Null for
    /// anyone and anything else. A trainer is never a Pokémon, whatever looks they wear (Route 209's Poké Kid).
    /// </summary>
    public static string? SpeciesFor(string looks) => looks switch
    {
        "machop" => "Machop",
        "drifloon" => "Drifloon",
        "pachirisu" => "Pachirisu",
        "buneary" => "Buneary",
        "happiny" => "Happiny",
        "pikachu" => "Pikachu",
        "clefairy" => "Clefairy",
        "croagunk" => "Croagunk",
        "psyduck" => "Psyduck",
        "starly" => "Starly",
        "magikarp" => "Magikarp",
        // FireRed's (plan 19 · K1): Speary, in a house of Viridian City
        "spearow" => "Spearow",
        "uxie" => "Uxie",
        "mesprit" => "Mesprit",
        "azelf" => "Azelf",
        "giratina_altered" => "Giratina",
        "giratina_origin" => "Giratina-Origin",
        // Plan 02 · S15, part b: Stark Mountain's, Snowpoint Temple's and Fullmoon Island's
        "heatran" => "Heatran",
        "regigigas" => "Regigigas",
        "cresselia" => "Cresselia",
        // Plan 21 · H2, part b: Mr. Briney's Peeko, the Pokémon of Rustboro's flats, and Walda's dolls (stood as the
        // Pokémon they are made after, small as they are: a doll has no look of its own yet)
        "wingull" => "Wingull",
        "skitty" => "Skitty",
        "skitty_doll" => "Skitty",
        "treecko_doll" => "Treecko",
        "torchic_doll" => "Torchic",
        "mudkip_doll" => "Mudkip",
        "pikachu_doll" => "Pikachu",
        _ => null
    };

    private static bool IsSignpost(string looks) => looks is "map_signpost" or "arrow_signpost" or "signboard" or "trainer_tips_signpost" or "gym_signpost";

    /// <summary>
    /// A flag of the original's that lasts only while the player is in its area (<c>FLAG_MAP_LOCAL_...</c>, cleared
    /// on leaving: <see cref="Story.StoryState.ClearLocal"/>) made the area's own, since one map of the world holds
    /// many areas that number their local flags alike: <c>FLAG_MAP_LOCAL_HIDE_OBSTACLE_1_ETERNA_CITY</c>. Any other
    /// flag is left as it is.
    /// </summary>
    public static string LocalFlag(string flag, string areaKey) =>
        flag.StartsWith(Story.StoryState.LocalFlagPrefix, StringComparison.Ordinal) ? flag + "_" + areaKey.ToUpperInvariant() : flag;

    /// <summary>The obstacle an object of the original is, by the name of its looks: what Cut, Rock Smash and Strength clear.</summary>
    public static PropType? ObstacleFor(string looks) => looks switch
    {
        // (HeartGold's small tree is "tree", plan 20 · J2)
        // (and Emerald's, plan 21 · H1)
        "cut_tree" or "tree" or "cuttable_tree" => PropType.CutTree,
        "rock_smash" or "breakrock" or "breakable_rock" => PropType.CrackedRock,
        "strength_boulder" or "pushable_boulder" => PropType.StrengthBoulder,
        _ => null
    };

    private static Direction FacingOf(int facing) => facing switch
    {
        0 => Direction.Up,
        2 => Direction.Left,
        3 => Direction.Right,
        _ => Direction.Down
    };

    /// <param name="people">
    /// False for a map built only for its ground (<see cref="BuildSwapped"/>): what the events do to the tiles is done
    /// (a signpost's, a door's, a way in that leads nowhere), and nobody is made to stand there.
    /// </param>
    private static void PlaceEvents(World world, Map map, string key, MapArea area, bool people = true)
    {
        var file = world.Area(key)!;
        var overlay = world.Overlay(key);

        foreach (var o in file.Objects)
        {
            if (overlay?.HeldBack?.Contains(o.Id) == true) continue;
            // A copy of a neighbouring area's object, there so it shows across the border: the neighbour places it
            if (o.HiddenBy != null && o.HiddenBy.StartsWith("MAP_HEADER_", StringComparison.Ordinal)) continue;
            // The two halves of a route list each other's people and signs without saying so: whoever stands on
            // the other half, where that half has the same thing on the same tile, is the other half's to place
            if (map.InBounds(o.X, o.Z) && map.AreaAt(o.X, o.Z) is { Open: true } there && there != area
                && world.Area(there.Key)?.Objects.Any(twin => twin.X == o.X && twin.Z == o.Z && twin.Looks == o.Looks) == true) continue;

            if (IsSignpost(o.Looks) && o.HiddenBy == null)
            {
                map.SetGroundTile(o.X, o.Z, TileType.Signpost, isSolid: true);
                map.Signboards[(o.X, o.Z)] = overlay?.Signs?.GetValueOrDefault(o.Id) ?? area.DisplayName;
                map.SignKeys[(o.X, o.Z)] = o.Id;
                if (overlay?.SignScripts?.GetValueOrDefault(o.Id) is { Length: > 0 } read) map.SignScripts[(o.X, o.Z)] = read;
                continue;
            }
            if (o.Looks == "mailbox")
            {
                map.AddProp(PropType.Mailbox, o.X, o.Z);
                map.Signboards[(o.X, o.Z)] = overlay?.Signs?.GetValueOrDefault(o.Id) ?? "A mailbox.";
                map.SignKeys[(o.X, o.Z)] = o.Id;
                if (overlay?.SignScripts?.GetValueOrDefault(o.Id) is { Length: > 0 } opened) map.SignScripts[(o.X, o.Z)] = opened;
                continue;
            }
            // An obstacle is an object of the map, as the original's is (plan 02 · S2): cleared by its field move, it
            // stays gone by its own flag, which is local to its area and cleared when the player leaves it
            if (!people)
            {
                // Only the tile someone who never leaves it keeps open inside a thing that blocks the way (below)
                if (overlay?.People?.GetValueOrDefault(o.Id) is { Still: true } && ObstacleFor(o.Looks) == null && o.Patch == null && o.Item == null
                    && map.IsSolid(o.X, o.Z) && map.Props.Any(p => p.Model.Length > 0 && p.Covers(o.X, o.Z)))
                    map.SetSolid(o.X, o.Z, false);
                continue;
            }
            if (ObstacleFor(o.Looks) is { } obstacle)
            {
                var thing = map.AddObstacle(obstacle, o.X, o.Z);
                thing.HiddenBy = o.HiddenBy is { } flag ? LocalFlag(flag, key) : null;
                thing.Key = o.Id;
                thing.ScriptFile = key;
                continue;
            }
            // A patch of soft soil is the save's berry patch its object names (plan 06 · R14a)
            if (o.Patch is { } patch)
            {
                map.NPCs.Add(new NPC
                {
                    Name = "Soft soil",
                    NpcType = NPC.BerrySoilType,
                    GridX = o.X,
                    GridY = o.Z,
                    BerryPatch = patch,
                    Key = o.Id,
                    ScriptFile = key
                });
                continue;
            }
            // An item in its ball lies where the original has it, and stays gone by the original's own flag
            if (o.Item != null && o.HiddenBy != null)
            {
                map.NPCs.Add(new NPC
                {
                    Name = o.Item,
                    NpcType = NPC.ItemBallType,
                    GridX = o.X,
                    GridY = o.Z,
                    Item = o.Item,
                    ItemCount = o.Count ?? 1,
                    HiddenBy = o.HiddenBy,
                    Key = o.Id,
                    ScriptFile = key
                });
                continue;
            }

            // People appear only once the game says who they are
            if (overlay?.People == null || !overlay.People.TryGetValue(o.Id, out var person)) continue;
            // A Pokémon of the original's stands as itself, unless the overlay makes someone else of it (plan 10 · F1)
            string? species = person.NpcType == null && person.Trainer == null ? SpeciesFor(o.Looks) : person.NpcType == NPC.PokemonType ? person.Species : null;
            // A trainer is the table's, by the id its object's script gives (TRAINER_YOUNGSTER_TRISTAN), or the one the
            // overlay names for someone a script battles; the overlay gives their lines alone (plan 08 · P7)
            MapFile.TrainerRecord? trainer = null;
            if (person.Trainer is { } lines)
            {
                var record = TrainerDatabase.Get(o.Script, world.Index.Region) ?? TrainerDatabase.Get(lines.Id, world.Index.Region)
                    ?? throw new InvalidDataException($"{key}: {o.Id} battles, and neither its script '{o.Script}' nor the overlay names a trainer of trainers.json");
                // Name and class left to the table, which Fill gives a trainer whose map wrote no team
                trainer = new MapFile.TrainerRecord
                {
                    Id = record.Id, Name = "", DialogueBefore = lines.DialogueBefore, DialogueAfter = lines.DialogueAfter,
                    // How far they see is the original's own number; nought for someone it never set watching
                    SightRange = o.Sight ?? 0
                };
            }
            // Where they stand is the area file's, unless the overlay says where (the Underground's hikers)
            int px = person.At is { Length: 2 } ? person.At[0] : o.X, pz = person.At is { Length: 2 } ? person.At[1] : o.Z;
            var npc = MapFile.BuildNpc(new MapFile.NpcRecord
            {
                Id = person.Id ?? trainer?.Id,
                Name = person.Name,
                NpcType = person.NpcType ?? (species != null ? NPC.PokemonType : CharacterFor(o.Looks)),
                Species = species,
                X = px,
                Y = pz,
                Facing = FacingOf(o.Facing),
                Dialog = person.Dialog,
                IsStarterBriefcase = person.IsStarterBriefcase,
                IsHealingNurse = person.IsHealingNurse,
                Script = person.Script,
                // The flag that hides them is the original's own unless the overlay says otherwise
                HiddenBy = person.HiddenBy is { } hiddenBy ? (hiddenBy.Length > 0 ? hiddenBy : null) : o.HiddenBy,
                ShownBy = person.ShownBy,
                Trainer = trainer,
                Item = person.Item
            }, map.Name);
            // Someone the original stands on a bridge's deck stands there, over whoever walks under it
            if (o.Y is > 0 && map.DeckAt(o.X, o.Z) is { } deck) npc.Level = deck;
            // Something the overlay lays on a table stands on its top (plan 19 · K3)
            // (drawn there only: they are spoken to from the floor before the furniture, as from beside it)
            if (person.Level is { } above) npc.RaisedBy = above;
            // Scripts call them by their id in the area's file, and look their own scripts up in the area's
            npc.Key = o.Id;
            npc.LinesPlace = key;
            npc.LinesKey = o.Id;
            npc.ScriptFile = key;
            // How they move about of their own accord, within the original's range round where they stand (plan 02 · S6)
            if (!person.Still) npc.Movement = PersonMovement.Parse(o.Movement, o.RangeX, o.RangeZ, o.X, o.Z, npc.Facing);
            // Someone the original stands inside a thing that blocks the way (the swimmer in the Resort Area's pool) keeps
            // their tile, where the overlay says they never leave it: they are what is in the way there
            else if (map.IsSolid(px, pz) && map.Props.FirstOrDefault(p => p.Model.Length > 0 && p.Covers(px, pz)) is { } inside)
            {
                map.SetSolid(px, pz, false);
                if (inside.Type == PropType.Pool) npc.Wading = PoolDepth;
            }
            map.NPCs.Add(npc);
        }

        foreach (var record in people ? overlay?.Npcs ?? new() : new())
        {
            var npc = MapFile.BuildNpc(record, map.Name);
            npc.LinesPlace = key;
            npc.ScriptFile = key;
            // An electric gate stands on a tile of the gap it shuts, which the original blocks while its beams are
            // on: the tile is open under it, and the gate is what is in the way (plan 19 · K4, part b)
            if (npc.NpcType == NPC.ElectricGateType && map.InBounds(npc.GridX, npc.GridY))
            {
                // ...and floored like the floor it joins, the first open tile south of it, so the room is built with
                // the gap where FireRed's beams stand, not a wall across it
                for (int below = npc.GridY + 1; below < Math.Min(map.Height, npc.GridY + 5); below++)
                    if (!map.IsSolid(npc.GridX, below))
                    {
                        map.SetGroundTile(npc.GridX, npc.GridY, map.GetGroundTile(npc.GridX, below), isSolid: false);
                        break;
                    }
                map.SetSolid(npc.GridX, npc.GridY, false);
            }
            // A boulder among them is put back where it stood when the player leaves, as every boulder is
            map.Add(npc);
        }

        // What is hidden in the ground, found by looking at its tile; and what is read there, where the overlay
        // gives the original's script one of ours
        foreach (var s in file.Signs)
        {
            if (!map.InBounds(s.X, s.Z)) continue;
            if (s.Type == AreaSign.HiddenItem && s.Item != null && s.Flag != null)
                map.HiddenItems[(s.X, s.Z)] = new HiddenItem(s.Item, s.Count ?? 1, s.Flag, s.Range ?? 0);
            else if (overlay?.Read?.GetValueOrDefault(s.Script) is { Length: > 0 } read)
                map.TileScripts[(s.X, s.Z)] = read;
            // A GBA game's signpost is a tile and a sign event (plan 19 · K1): where the overlay gives the sign's script
            // our words, a blocked tile of the open country that does nothing stands a signpost, read like one of
            // Sinnoh's; in a room (a bookshelf, a PC) and on a door the tile is what it was, and is read all the same
            else if (s.Type != AreaSign.HiddenItem && s.Script.Length > 0 && overlay?.Signs?.GetValueOrDefault(s.Script) is { } text)
            {
                if (!map.IsIndoors && map.IsSolid(s.X, s.Z) && map.BehaviourAt(s.X, s.Z) == TileBehavior.None)
                    map.SetGroundTile(s.X, s.Z, TileType.Signpost, isSolid: true);
                map.Signboards[(s.X, s.Z)] = text;
                map.SignKeys[(s.X, s.Z)] = s.Script;
            }
        }

        // The original's triggers that have a script of ours: its tiles, its variable and its value; and ours, where
        // the original has none, on the tiles the overlay gives
        foreach (var bound in overlay?.Triggers ?? new())
        {
            if (bound.Trigger is not { } number)
            {
                if (bound.X is not { } x || bound.Z is not { } z)
                    throw new InvalidDataException($"A trigger in the overlay of {key} gives neither the original's number nor its own tiles.");
                map.Triggers.Add(new StepTrigger
                {
                    X = x, Y = z, Width = bound.Width, Depth = bound.Depth, Script = bound.Script, ScriptFile = key,
                    Variable = bound.Variable, Value = bound.Value
                });
                continue;
            }
            if (number < 0 || number >= file.Triggers.Count)
                throw new InvalidDataException($"The overlay of {key} gives a script to trigger {number}, and the area has {file.Triggers.Count}.");
            var t = file.Triggers[number];
            if (!int.TryParse(t.Value, out int value))
                throw new InvalidDataException($"Trigger {number} of {key} waits for the value '{t.Value}', which is no number.");
            map.Triggers.Add(new StepTrigger
            {
                X = t.X, Y = t.Z, Width = t.Width, Depth = t.Depth, Script = bound.Script, ScriptFile = key,
                Variable = t.Variable.Length > 0 ? t.Variable : null, Value = value
            });
        }
        // What is read where nothing shows, and the pits a pushed boulder falls into (plan 02 · S12)
        foreach (var read in overlay?.ReadAt ?? new()) map.TileScripts[(read.X, read.Z)] = read.Script;
        foreach (var pit in overlay?.Pits ?? new())
        {
            map.BoulderPits[(pit.X, pit.Z)] = pit.Script;
            // In the Distortion World a pit is the tile the original drops its boulder through, which the import
            // knows only as blocked: nothing, let into the island, as deep as the nothing round the islands
            // (VoidIslands has run by now)
            if (!map.IsVoid || map.GetGroundTile(pit.X, pit.Z) == TileType.Void) continue;
            float island = MathF.Floor(map.HeightAt(pit.X, pit.Z));
            map.SetGroundTile(pit.X, pit.Z, TileType.Void, isSolid: true);
            map.SetHeight(pit.X, pit.Z, island - VoidDrop);
        }
        foreach (var prop in overlay?.Props ?? new())
        {
            var placed = map.AddProp(prop.Type, prop.X, prop.Y, prop.Width, prop.Depth);
            // Whoever never leaves a thing the overlay lays down (the attendant inside a booth of the Battle Frontier,
            // plan 01 · M10, part b) keeps their tile, as someone inside a model of the world does: they are what is in
            // the way there
            foreach (var o in file.Objects)
                if (placed.Covers(o.X, o.Z) && overlay?.People?.GetValueOrDefault(o.Id) is { Still: true }) map.SetSolid(o.X, o.Z, false);
        }

        for (int i = 0; i < file.Warps.Count; i++)
        {
            var from = file.Warps[i];
            if (overlay?.Locked?.Contains(i) == true)
            {
                // A shut door of a world whose houses are found at their doors is still its building's door (plan 21 · H1)
                if (world.Index.BuildingsAtDoors) map.BuildingKinds[(from.X, from.Z)] = KindOfArea(from.To);
                continue;
            }
            Warp? warp = null;
            if (overlay?.Doors?.FirstOrDefault(d => d.Warp == i) is { } door)
            {
                warp = new Warp { TargetMap = door.Map, TargetX = door.X, TargetY = door.Y, TargetFacing = door.Facing, OpenedBy = door.OpenedBy };
            }
            else
            {
                warp = Join(world, from.To, from.ToWarp);
            }
            if (warp == null) continue;

            warp.SourceX = from.X;
            warp.SourceY = from.Z;
            // A way the story switches: there until a flag or from it, and shut meanwhile (Map.ApplyDoors keeps it so)
            if (overlay?.Switches?.FirstOrDefault(s => s.Warp == i) is { } switched)
            {
                warp.Until = switched.Until;
                warp.From = switched.From;
                warp.Shut = switched.From != null;
            }
            map.Warps.Add(warp);
            // A door is a blocked tile in the world's data; here stepping onto it is what takes the warp
            if (map.GetGroundTile(from.X, from.Z) == TileType.Door) map.SetSolid(from.X, from.Z, false);
        }

        foreach (var exit in overlay?.Exits ?? new())
            map.Warps.Add(new Warp { SourceX = exit.X, SourceY = exit.Z, TargetMap = exit.Map, TargetX = exit.ToX, TargetY = exit.ToY, TargetFacing = exit.Facing });

        // A way in that leads nowhere yet (a cave's mouth, a gate house, a hall without a door) is closed: one
        // can't walk into an opening and stand in the wall. A door is shut by itself.
        foreach (var from in file.Warps)
            if (!map.WarpsAt(from.X, from.Z).Any() && map.GetGroundTile(from.X, from.Z) != TileType.Door) map.SetSolid(from.X, from.Z, true);
    }

    /// <summary>The way into an open area through one of its warps (Turnback Cave's doors, which are aimed as the player comes in).</summary>
    public static Warp? WayInto(World world, string area, int toWarp) => Join(world, area, toWarp);

    /// <summary>A warp onto one of an open area's warps, coming out one step from it; null while the area isn't open or has no map.</summary>
    private static Warp? Join(World world, string area, int toWarp)
    {
        if (!world.IsOpen(area) || world.MapOf(area) is not { } onto || world.Area(area) is not { } target || toWarp >= target.Warps.Count) return null;
        var to = target.Warps[toWarp];
        var (x, z, facing) = Arrival(world.BehaviourAt(target.Matrix, to.X, to.Z), to.X, to.Z);
        // Stairs in a passage that runs past them (Snowpoint Temple's, plan 01 · M10, part b: blocked to the east and
        // the west, walked off to the north or the south) put whoever comes up them down in the passage: the original
        // leaves the player on the stairs' own tile, which here would take the warp again
        bool Blocked(int tx, int tz) => world.ChunkAt(target.Matrix, tx, tz) is not { } at || at.Chunk.SolidAt(at.X, at.Z);
        // HeartGold's ways out (plan 20 · J2) are told the same way, but a few face a wall (Elm's house in New Bark
        // Town, whose door's tile says "out to the west" with the lab's wall there): whoever comes out stands on the
        // open tile beside it, as on stairs
        bool besideIt = world.BehaviourAt(target.Matrix, to.X, to.Z) is TileBehavior.StairsEast or TileBehavior.StairsWest
            || string.Equals(world.Index.Region, RegionDatabase.Johto, StringComparison.OrdinalIgnoreCase) && (x != to.X || z != to.Z);
        // FireRed's stairs are taken only walking onto them the way they go, so a flight with a gravestone beside it (the
        // Pokémon Tower's fifth floor, plan 19 · K5) is walked off to the north as often as to the south, and the way
        // on is the north: whoever comes up a flight of Kanto's whose side is shut steps off northwards first
        bool northFirst = string.Equals(world.Index.Region, RegionDatabase.Kanto, StringComparison.OrdinalIgnoreCase);
        var sides = northFirst
            ? new[] { (0, -1, Direction.Up), (0, 1, Direction.Down), (-1, 0, Direction.Left), (1, 0, Direction.Right) }
            : new[] { (0, 1, Direction.Down), (0, -1, Direction.Up), (-1, 0, Direction.Left), (1, 0, Direction.Right) };
        if (besideIt && Blocked(x, z))
            foreach (var (dx, dz, way) in sides)
                if (!Blocked(to.X + dx, to.Z + dz))
                {
                    (x, z, facing) = (to.X + dx, to.Z + dz, way);
                    break;
                }
        return new Warp { TargetMap = onto.Name, TargetX = x, TargetY = z, TargetFacing = facing };
    }

    /// <summary>
    /// Where someone coming through a warp is put down: one step out of the warp's tile, the way they would be
    /// walking as they come out, so they don't stand on it and take it again.
    /// </summary>
    public static (int X, int Z, Direction Facing) Arrival(TileBehavior warpTile, int x, int z) => warpTile switch
    {
        // Taken walking north: one comes out of it walking south
        TileBehavior.Door or TileBehavior.EntranceNorth or TileBehavior.ExitNorth => (x, z + 1, Direction.Down),
        TileBehavior.EntranceSouth or TileBehavior.ExitSouth => (x, z - 1, Direction.Up),
        TileBehavior.EntranceEast or TileBehavior.ExitEast or TileBehavior.StairsEast => (x - 1, z, Direction.Left),
        TileBehavior.EntranceWest or TileBehavior.ExitWest or TileBehavior.StairsWest => (x + 1, z, Direction.Right),
        _ => (x, z, Direction.Down)
    };
}
