using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Data;

/// <summary>
/// A region's map as its file gives it (<c>world/&lt;region&gt;/townmap.json</c>, written by hand; plan 01 · M12;
/// <c>docs/data-files.md</c>, "The region map"): the grid, a cell a chunk of the overworld, the blocks of it that
/// have something to say, what is said of each area and landmark, the places the story reveals and where Fly lands.
/// </summary>
public sealed class RegionMapFile
{
    public const string FileName = "townmap.json";

    /// <summary>The map of the game the grid lies over: the region's overworld.</summary>
    public string Map { get; set; } = "";

    public int Columns { get; set; }
    public int Rows { get; set; }

    /// <summary>The cells the screen shows: left, top, columns, rows.</summary>
    public int[] View { get; set; } = Array.Empty<int>();

    /// <summary>The cells the cursor may stand on: left, top, right, bottom, each one counted.</summary>
    public int[] Cursor { get; set; } = Array.Empty<int>();

    /// <summary>Where the cursor stands when the player is nowhere on the map.</summary>
    public int[] Start { get; set; } = Array.Empty<int>();

    public List<RegionHiddenRecord> Hidden { get; set; } = new();
    public List<RegionPlaceRecord> Areas { get; set; } = new();
    public List<RegionPlaceRecord> Landmarks { get; set; } = new();
    public List<RegionBlockRecord> Blocks { get; set; } = new();
    public List<RegionFlyRecord> Fly { get; set; } = new();
}

/// <summary>A place the map leaves out until a variable of the story holds its number.</summary>
public sealed class RegionHiddenRecord
{
    public string Key { get; set; } = "";
    public string Variable { get; set; } = "";
    public int Value { get; set; }

    /// <summary>Painted as open water until it is revealed, where the world has land there all along.</summary>
    public bool Sea { get; set; }
}

/// <summary>An area or a landmark: what is said of it, and the flag its words wait for.</summary>
public sealed class RegionPlaceRecord
{
    public string Key { get; set; } = "";

    /// <summary>A landmark's own name; an area has none (a block says its place's name).</summary>
    public string? Name { get; set; }

    public string? Flag { get; set; }

    /// <summary>The places (an area's key or a room's name) whose first arrival sets <see cref="Flag"/>, where no script does.</summary>
    public List<string>? Arrival { get; set; }

    public string About { get; set; } = "";
}

public sealed class RegionBlockRecord
{
    public int X { get; set; }
    public int Y { get; set; }
    public RegionBlockKind Kind { get; set; }
    public string Name { get; set; } = "";
    public string Area { get; set; } = "";
    public string? Landmark { get; set; }
    public string? Hidden { get; set; }
}

public sealed class RegionFlyRecord
{
    /// <summary>The spawn location's number (<see cref="SpawnLocations"/>).</summary>
    public int Spawn { get; set; }

    /// <summary>Its cells on the map, <c>"x,y"</c> each, separated by spaces.</summary>
    public string Cells { get; set; } = "";

    /// <summary>The flag that opens it, where that isn't the spawn location's own.</summary>
    public string? Flag { get; set; }

    /// <summary>Nothing is drawn of it until it is open.</summary>
    public bool Unseen { get; set; }
}

/// <summary>What a block is, which the original shows as the kind of signpost its name stands on.</summary>
public enum RegionBlockKind
{
    Route,
    Town,
    Landmark
}

/// <summary>A place the story reveals (<c>SystemVars_CheckHiddenLocation</c>).</summary>
public sealed record RegionHiddenPlace(string Key, string Variable, int Value, bool Sea);

/// <summary>An area or a landmark of the map: its words, and the flag they wait for (null: always told).</summary>
public sealed record RegionPlace(string Key, string? Name, string About, string? Flag, IReadOnlyList<string> Arrival);

/// <summary>A cell of the map with something to say (the original's <c>TownMapBlock</c>).</summary>
public sealed record RegionBlock(int X, int Y, RegionBlockKind Kind, string Name, RegionPlace Area, RegionPlace? Landmark, RegionHiddenPlace? Hidden);

/// <summary>Where Fly lands, as the map shows it (the original's <c>sFlyLocations</c>).</summary>
public sealed record RegionFlySpot(int Spawn, IReadOnlyList<(int X, int Y)> Cells, string? Flag, bool Unseen);

/// <summary>
/// A region's map (plan 01 · M12): the one model the Town Map, Fly's map and whatever else shows the region are
/// drawn from. A cell is a chunk of the overworld, 32 tiles by 32, as the Pokédex's area page and the Pokétch's map
/// lay the region out; a <see cref="RegionBlock"/> is a cell with something to say. Nothing here knows the game's
/// state: what a game has put on the map is a <see cref="Models.TownMapState"/>. No drawing or input.
/// </summary>
public sealed class RegionMap
{
    private readonly Dictionary<(int X, int Y), RegionBlock> byCell = new();
    private readonly Dictionary<string, List<string>> arrivals = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The region's folder under <c>Data/world</c>: "sinnoh".</summary>
    public string Region { get; }

    /// <summary>The map of the game the grid lies over.</summary>
    public string Map { get; }

    public int Columns { get; }
    public int Rows { get; }

    /// <summary>The cells the screen shows.</summary>
    public (int X, int Y, int Width, int Height) View { get; }

    /// <summary>The cells the cursor may stand on, each bound counted.</summary>
    public (int Left, int Top, int Right, int Bottom) Bounds { get; }

    /// <summary>Where the cursor stands when the player is nowhere on the map.</summary>
    public (int X, int Y) Start { get; }

    public IReadOnlyList<RegionHiddenPlace> Hidden { get; }
    public IReadOnlyList<RegionPlace> Areas { get; }
    public IReadOnlyList<RegionPlace> Landmarks { get; }
    public IReadOnlyList<RegionBlock> Blocks { get; }
    public IReadOnlyList<RegionFlySpot> FlySpots { get; }

    public RegionMap(string region, RegionMapFile file)
    {
        Region = region;
        Map = file.Map;
        Columns = file.Columns;
        Rows = file.Rows;
        if (Columns <= 0 || Rows <= 0) throw new InvalidDataException($"The map of {region} has no grid.");
        View = Four(file.View, "view") is var v ? (v[0], v[1], v[2], v[3]) : default;
        Bounds = Four(file.Cursor, "cursor") is var c ? (c[0], c[1], c[2], c[3]) : default;
        if (file.Start.Length != 2) throw new InvalidDataException($"The map of {region}: \"start\" is two numbers.");
        Start = (file.Start[0], file.Start[1]);
        if (View.X < 0 || View.Y < 0 || View.X + View.Width > Columns || View.Y + View.Height > Rows || View.Width <= 0 || View.Height <= 0)
            throw new InvalidDataException($"The map of {region}: its view lies outside its grid.");
        if (Bounds.Left > Bounds.Right || Bounds.Top > Bounds.Bottom || !InView(Bounds.Left, Bounds.Top) || !InView(Bounds.Right, Bounds.Bottom))
            throw new InvalidDataException($"The map of {region}: its cursor's bounds lie outside its view.");
        if (!InBounds(Start.X, Start.Y)) throw new InvalidDataException($"The map of {region}: its cursor starts outside its bounds.");

        Hidden = file.Hidden.Select(h => new RegionHiddenPlace(h.Key, h.Variable, h.Value, h.Sea)).ToList();
        RegionPlace Place(RegionPlaceRecord r) => new(r.Key, r.Name, r.About, r.Flag, r.Arrival ?? new List<string>());
        Areas = file.Areas.Select(Place).ToList();
        Landmarks = file.Landmarks.Select(Place).ToList();
        var areas = Unique(Areas, "area");
        var landmarks = Unique(Landmarks, "landmark");
        var hidden = Hidden.ToDictionary(h => h.Key, StringComparer.OrdinalIgnoreCase);

        var blocks = new List<RegionBlock>();
        foreach (var b in file.Blocks)
        {
            if (!areas.TryGetValue(b.Area, out var area)) throw new InvalidDataException($"The map of {region}: block {b.X},{b.Y} names an area it doesn't have, '{b.Area}'.");
            RegionPlace? landmark = null;
            if (b.Landmark != null && !landmarks.TryGetValue(b.Landmark, out landmark))
                throw new InvalidDataException($"The map of {region}: block {b.X},{b.Y} names a landmark it doesn't have, '{b.Landmark}'.");
            RegionHiddenPlace? hide = null;
            if (b.Hidden != null && !hidden.TryGetValue(b.Hidden, out hide))
                throw new InvalidDataException($"The map of {region}: block {b.X},{b.Y} names a hidden place it doesn't have, '{b.Hidden}'.");
            if (!InView(b.X, b.Y)) throw new InvalidDataException($"The map of {region}: block {b.X},{b.Y} lies outside its view.");
            var block = new RegionBlock(b.X, b.Y, b.Kind, b.Name, area, landmark, hide);
            if (!byCell.TryAdd((b.X, b.Y), block)) throw new InvalidDataException($"The map of {region} has two blocks at {b.X},{b.Y}.");
            blocks.Add(block);
        }
        Blocks = blocks;

        var spots = new List<RegionFlySpot>();
        foreach (var f in file.Fly)
        {
            var cells = ParseCells(f.Cells);
            if (cells.Count == 0) throw new InvalidDataException($"The map of {region}: Fly's place {f.Spawn} has no cell.");
            foreach (var cell in cells)
                if (!byCell.ContainsKey(cell)) throw new InvalidDataException($"The map of {region}: Fly's place {f.Spawn} has a cell with no block, {cell.X},{cell.Y}.");
            spots.Add(new RegionFlySpot(f.Spawn, cells, f.Flag, f.Unseen));
        }
        FlySpots = spots;

        foreach (var place in Areas.Concat(Landmarks))
            foreach (string where in place.Arrival)
            {
                if (place.Flag == null) throw new InvalidDataException($"The map of {region}: '{place.Key}' names an arrival and no flag for it to set.");
                if (!arrivals.TryGetValue(where, out var flags)) arrivals[where] = flags = new List<string>();
                if (!flags.Contains(place.Flag)) flags.Add(place.Flag);
            }
    }

    private int[] Four(int[] numbers, string what) =>
        numbers.Length == 4 ? numbers : throw new InvalidDataException($"The map of {Region}: \"{what}\" is four numbers.");

    private Dictionary<string, RegionPlace> Unique(IReadOnlyList<RegionPlace> places, string what)
    {
        var byKey = new Dictionary<string, RegionPlace>(StringComparer.OrdinalIgnoreCase);
        foreach (var place in places)
            if (!byKey.TryAdd(place.Key, place)) throw new InvalidDataException($"The map of {Region} has two {what}s called '{place.Key}'.");
        return byKey;
    }

    /// <summary>Whether a cell is one the screen shows.</summary>
    public bool InView(int x, int y) => x >= View.X && y >= View.Y && x < View.X + View.Width && y < View.Y + View.Height;

    /// <summary>Whether the cursor may stand on a cell.</summary>
    public bool InBounds(int x, int y) => x >= Bounds.Left && x <= Bounds.Right && y >= Bounds.Top && y <= Bounds.Bottom;

    /// <summary>The block of a cell, revealed or not; null where the map has nothing to say.</summary>
    public RegionBlock? BlockAt(int x, int y) => byCell.GetValueOrDefault((x, y));

    /// <summary>The cell a tile of the overworld lies in.</summary>
    public static (int X, int Y) CellOf(int tileX, int tileY) => (tileX / Overworld.Map.ChunkTiles, tileY / Overworld.Map.ChunkTiles);

    /// <summary>
    /// The flags a first arrival somewhere sets (an area's key or a room's name), where the original's own map
    /// script sets them and no script of ours does: what a landmark's words on the map wait for.
    /// </summary>
    public IReadOnlyList<string> ArrivalFlags(string place) => arrivals.TryGetValue(place, out var flags) ? flags : Array.Empty<string>();

    /// <summary>
    /// The cell of the map each other map of the game is reached from: the cell of the way in from the overworld,
    /// through whatever rooms lie between (the nearest way in, where there are several). A place no warp leads into
    /// has none. This is where the player is shown when nothing remembers the way they went in by.
    /// </summary>
    public Dictionary<string, (int X, int Y)> PlacesOf(IEnumerable<Map> maps)
    {
        var byName = new Dictionary<string, Map>(StringComparer.Ordinal);
        foreach (var map in maps) byName.TryAdd(map.Name, map);
        var places = new Dictionary<string, (int X, int Y)>(StringComparer.Ordinal);
        if (!byName.TryGetValue(Map, out var overworld)) return places;

        var queue = new Queue<(string Name, (int X, int Y) Cell)>();
        foreach (var warp in overworld.Warps)
            if (warp.TargetMap != Map) queue.Enqueue((warp.TargetMap, CellOf(warp.SourceX, warp.SourceY)));
        while (queue.Count > 0)
        {
            var (name, cell) = queue.Dequeue();
            if (name == Map || !byName.TryGetValue(name, out var map) || !places.TryAdd(name, cell)) continue;
            foreach (var warp in map.Warps) queue.Enqueue((warp.TargetMap, cell));
        }
        return places;
    }

    private static List<(int X, int Y)> ParseCells(string text)
    {
        var cells = new List<(int, int)>();
        foreach (string pair in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = pair.Split(',');
            if (parts.Length != 2) throw new InvalidDataException($"Map cell '{pair}' is not 'x,y'.");
            cells.Add((int.Parse(parts[0], CultureInfo.InvariantCulture), int.Parse(parts[1], CultureInfo.InvariantCulture)));
        }
        return cells;
    }

    private static readonly Dictionary<string, RegionMap?> loaded = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>A region's map, read once; null where the region's folder has none.</summary>
    public static RegionMap? Of(string region)
    {
        lock (loaded)
        {
            if (loaded.TryGetValue(region, out var map)) return map;
            string path = Path.Combine(World.Folder, region.ToLowerInvariant(), RegionMapFile.FileName);
            map = File.Exists(GameDataFiles.PathOf(path)) ? new RegionMap(region.ToLowerInvariant(), GameDataFiles.Load<RegionMapFile>(path)) : null;
            return loaded[region] = map;
        }
    }

    /// <summary>The map of the region a map of the game belongs to; null for a region with no map of its own.</summary>
    public static RegionMap? ForMap(string mapName) => RegionDatabase.RegionOfMap(mapName) is { } region ? Of(region.Id) : null;

    /// <summary>Sinnoh's map.</summary>
    public static RegionMap? Sinnoh => Of("sinnoh");
}
