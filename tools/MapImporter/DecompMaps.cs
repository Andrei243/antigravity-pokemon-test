using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace MapImporter;

/// <summary>
/// The map data of the pret/pokeplatinum decompilation: the grids of chunks (<c>res/field/matrices</c>), the chunks
/// themselves (<c>res/field/maps/data</c>), the table of map headers (<c>include/data/map_headers.h</c>), each
/// area's events (<c>res/field/events</c>) and the lists that give ids their names.
/// </summary>
public sealed partial class DecompMaps
{
    /// <summary>What the importer needs of the decompilation: the sparse checkout fetches only these.</summary>
    public static readonly string[] Folders =
    {
        "/res/field/matrices/", "/res/field/maps/data/", "/res/field/events/", "/res/field/area_data/", "/res/field/encounters/",
        "/res/field/props/models/", "/res/text/location_names.json", "/include/data/map_headers.h",
        "/generated/map_headers.txt", "/include/constants/field/map_tile_behaviors.h",
        // What lies on the ground and what is hidden in it: which item, and how many
        "/generated/items.txt", "/generated/vars_flags.txt", "/res/field/scripts/scripts_visible_items.s", "/include/data/field/hidden_items.h"
    };

    /// <summary>The script of the first item ball; the nth ball's is this plus n.</summary>
    public const int FirstVisibleItemScript = 7000;

    /// <summary>The script of the first hidden item; the nth's is this plus n.</summary>
    public const int FirstHiddenItemScript = 8000;

    /// <summary>The destination of a warp whose target a script sets while the game runs: the lifts.</summary>
    public const string DynamicHeader = "MAP_HEADER_DYNAMIC";

    private readonly string root;
    private readonly Dictionary<int, LandData> land = new();
    private readonly Dictionary<int, Matrix> matrices = new();
    private readonly Dictionary<string, AreaEvents> events = new();
    private readonly Dictionary<int, ModelInfo?> propModels = new();

    /// <summary>Map header constants in id order (<c>MAP_HEADER_EVERYWHERE</c> is 0).</summary>
    public IReadOnlyList<string> HeaderIds { get; }

    public IReadOnlyDictionary<string, MapHeader> Headers { get; }

    /// <summary>The location-name text of each label id, for example "Twinleaf Town".</summary>
    public IReadOnlyDictionary<string, string> LocationNames { get; }

    /// <summary>The decompilation's name for each behaviour value, without its <c>TILE_BEHAVIOR_</c> prefix.</summary>
    public IReadOnlyList<string> BehaviourNames { get; }

    /// <summary>Prop model files in id order, without their extension.</summary>
    public IReadOnlyList<string> PropModelFiles { get; }

    /// <summary>Item constants in id order (<c>ITEM_NONE</c> is 0).</summary>
    public IReadOnlyList<string> ItemIds { get; }

    /// <summary>What lies in each item ball, by the ball's number: the item's constant and how many.</summary>
    public IReadOnlyList<(string Item, int Count)> VisibleItems { get; }

    /// <summary>The hidden items, by number: a hidden item's script is 8000 plus its number.</summary>
    public IReadOnlyDictionary<int, HiddenItemEntry> HiddenItems { get; }

    public int MatrixCount { get; }
    public int LandCount { get; }

    public DecompMaps(string root)
    {
        this.root = root;
        if (!File.Exists(Path.Combine(root, "res", "field", "matrices", "map_matrix_000.json")))
            throw new DirectoryNotFoundException($"No pokeplatinum map data in {root}");

        // The list ends with three constants that are not areas (the count and two markers)
        HeaderIds = Lines("generated", "map_headers.txt").Where(l => !l.Contains('=') && l != "MAP_HEADER_COUNT").ToList();
        Headers = ParseHeaders(File.ReadAllText(Path.Combine(root, "include", "data", "map_headers.h")), HeaderIds);
        LocationNames = ParseLocationNames(File.ReadAllText(Path.Combine(root, "res", "text", "location_names.json")));
        BehaviourNames = ParseBehaviourNames(File.ReadAllText(Path.Combine(root, "include", "constants", "field", "map_tile_behaviors.h")));
        PropModelFiles = Lines("res", "field", "props", "models", "map_prop_models.order").Select(Path.GetFileNameWithoutExtension).ToList()!;
        MatrixCount = Lines("res", "field", "matrices", "map_matrices.order").Count;
        LandCount = Lines("res", "field", "maps", "data", "map_data.order").Count;
        ItemIds = Lines("generated", "items.txt");
        VisibleItems = ParseVisibleItems(File.ReadAllText(Path.Combine(root, "res", "field", "scripts", "scripts_visible_items.s")));
        HiddenItems = ParseHiddenItems(File.ReadAllText(Path.Combine(root, "include", "data", "field", "hidden_items.h")),
            File.ReadAllText(Path.Combine(root, "generated", "vars_flags.txt")));
    }

    /// <summary>What lies in the item ball that runs a script, or null for a script that is no item ball's.</summary>
    public (string Item, int Count)? VisibleItem(string script) =>
        int.TryParse(script, out int id) && id >= FirstVisibleItemScript && id - FirstVisibleItemScript < VisibleItems.Count
            && VisibleItems[id - FirstVisibleItemScript] is { Item.Length: > 0 } found ? found : null;

    /// <summary>The hidden item a sign's script stands for, or null for any other sign.</summary>
    public HiddenItemEntry? HiddenItem(string script) =>
        int.TryParse(script, out int id) && id >= FirstHiddenItemScript ? HiddenItems.GetValueOrDefault(id - FirstHiddenItemScript) : null;

    private List<string> Lines(params string[] parts) =>
        File.ReadAllLines(Path.Combine(new[] { root }.Concat(parts).ToArray())).Select(l => l.Trim()).Where(l => l.Length > 0).ToList();

    public LandData Land(int id)
    {
        if (!land.TryGetValue(id, out var data))
        {
            string path = Path.Combine(root, "res", "field", "maps", "data", $"map_data_{id:000}.bin");
            try { data = LandData.Parse(File.ReadAllBytes(path)); }
            catch (InvalidDataException e) { throw new InvalidDataException($"{Path.GetFileName(path)}: {e.Message}", e); }
            land[id] = data;
        }
        return data;
    }

    public Matrix Matrix(int id)
    {
        if (!matrices.TryGetValue(id, out var matrix))
        {
            string path = Path.Combine(root, "res", "field", "matrices", $"map_matrix_{id:000}.json");
            matrices[id] = matrix = MapImporter.Matrix.Parse(id, File.ReadAllText(path));
        }
        return matrix;
    }

    /// <summary>The events of an area, by the name its header gives them (<c>events_twinleaf_town</c>).</summary>
    public AreaEvents Events(string name)
    {
        if (!events.TryGetValue(name, out var e))
        {
            string path = Path.Combine(root, "res", "field", "events", name + ".json");
            events[name] = e = File.Exists(path) ? AreaEvents.Parse(File.ReadAllText(path)) : new AreaEvents();
        }
        return e;
    }

    /// <summary>
    /// The wild Pokémon of an area's grass, slot by slot, by the name its header gives the table
    /// (<c>encounters_route_201</c>); empty when the area has no grass encounters.
    /// </summary>
    public List<(string Species, int Level)> LandEncounters(string name) => LandEncounters(name, out _);

    /// <param name="rate">The table's rate: how often a step in the grass can meet one, out of a hundred (0 with no table).</param>
    public List<(string Species, int Level)> LandEncounters(string name, out int rate)
    {
        string path = Path.Combine(root, "res", "field", "encounters", name + ".json");
        var slots = new List<(string, int)>();
        rate = 0;
        if (!File.Exists(path)) return slots;
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        if (!doc.RootElement.TryGetProperty("land_rate", out var landRate) || landRate.GetInt32() == 0) return slots;
        rate = landRate.GetInt32();
        foreach (var slot in doc.RootElement.GetProperty("land_encounters").EnumerateArray())
            slots.Add((slot.GetProperty("species").GetString() ?? "", slot.GetProperty("level").GetInt32()));
        return slots;
    }

    /// <summary>
    /// The wild Pokémon met surfing on an area's water, slot by slot with each slot's range of levels, and the
    /// table's rate; empty when nothing lives in its water.
    /// </summary>
    public List<(string Species, int MinLevel, int MaxLevel)> WaterEncounters(string name, out int rate) => WaterEncounters(name, "surf", out rate);

    /// <summary>
    /// The same for one of the original's tables of water slots by its prefix: <c>surf</c>, or a rod's
    /// (<c>old_rod</c>, <c>good_rod</c>, <c>super_rod</c>).
    /// </summary>
    public List<(string Species, int MinLevel, int MaxLevel)> WaterEncounters(string name, string table, out int rate)
    {
        string path = Path.Combine(root, "res", "field", "encounters", name + ".json");
        var slots = new List<(string, int, int)>();
        rate = 0;
        if (!File.Exists(path)) return slots;
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        if (!doc.RootElement.TryGetProperty(table + "_rate", out var surfRate) || surfRate.GetInt32() == 0) return slots;
        rate = surfRate.GetInt32();
        foreach (var slot in doc.RootElement.GetProperty(table + "_encounters").EnumerateArray())
        {
            int a = slot.GetProperty("level_min").GetInt32(), b = slot.GetProperty("level_max").GetInt32();
            slots.Add((slot.GetProperty("species").GetString() ?? "", Math.Min(a, b), Math.Max(a, b)));
        }
        return slots;
    }

    /// <summary>
    /// Every species of an area's table of wild Pokémon, by the way they are met: the twelve grass slots, the two
    /// that the day and the night put in place of slots 2 and 3, the water's slots and each rod's. Null when the area
    /// has no table; a way with a rate of 0 is empty.
    /// </summary>
    public EncounterTable? Encounters(string name)
    {
        string path = Path.Combine(root, "res", "field", "encounters", name + ".json");
        if (!File.Exists(path)) return null;
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        var table = doc.RootElement;
        int Rate(string field) => table.TryGetProperty(field, out var r) ? r.GetInt32() : 0;
        List<string> Slots(string list, string rate) => Rate(rate) == 0 || !table.TryGetProperty(list, out var slots)
            ? new()
            : slots.EnumerateArray().Select(s => s.GetProperty("species").GetString() ?? "").ToList();
        List<string> Species(string list) => Rate("land_rate") == 0 || !table.TryGetProperty(list, out var names)
            ? new()
            : names.EnumerateArray().Select(s => s.GetString() ?? "").ToList();
        return new EncounterTable(
            Slots("land_encounters", "land_rate"), Species("day"), Species("night"),
            Slots("surf_encounters", "surf_rate"),
            Slots("old_rod_encounters", "old_rod_rate"), Slots("good_rod_encounters", "good_rod_rate"), Slots("super_rod_encounters", "super_rod_rate"));
    }

    /// <summary>
    /// What an area's table says of the forms met there (plan 06 · R10): whether Shellos and Gastrodon are the east
    /// sea's (<c>rate_form0</c>, <c>rate_form1</c>, read by <c>AddWildMonToParty</c>), and which of the Unown tables
    /// its Unown come from (<c>unown_table</c>, from 1; 0 for none).
    /// </summary>
    public (bool EastSea, int UnownTable) Forms(string name)
    {
        string path = Path.Combine(root, "res", "field", "encounters", name + ".json");
        if (!File.Exists(path)) return (false, 0);
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        var table = doc.RootElement;
        int Read(string field) => table.TryGetProperty(field, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetInt32() : 0;
        return (Read("rate_form0") != 0 || Read("rate_form1") != 0, Read("unown_table"));
    }

    /// <summary>Name and bounding box of a prop's model; null for an id without a file.</summary>
    public ModelInfo? PropModel(int id)
    {
        if (!propModels.TryGetValue(id, out var info))
        {
            info = null;
            if (id >= 0 && id < PropModelFiles.Count)
            {
                string path = Path.Combine(root, "res", "field", "props", "models", PropModelFiles[id] + ".nsbmd");
                if (File.Exists(path)) info = ModelInfo.Read(File.ReadAllBytes(path));
            }
            propModels[id] = info;
        }
        return info;
    }

    // ---------------------------------------------------------------- the lists

    [GeneratedRegex(@"\[(MAP_HEADER_\w+)\]\s*=\s*\{(.*?)\n\s*\},", RegexOptions.Singleline)]
    private static partial Regex HeaderBlock();

    [GeneratedRegex(@"\.(\w+)\s*=\s*([^,\n]+),")]
    private static partial Regex HeaderField();

    /// <summary>The header table is C source: one <c>[MAP_HEADER_X] = { .field = value, ... }</c> block per area.</summary>
    public static Dictionary<string, MapHeader> ParseHeaders(string source, IReadOnlyList<string> ids)
    {
        var index = ids.Select((id, i) => (id, i)).ToDictionary(p => p.id, p => p.i);
        var headers = new Dictionary<string, MapHeader>();
        foreach (Match block in HeaderBlock().Matches(source))
        {
            string id = block.Groups[1].Value;
            var fields = HeaderField().Matches(block.Groups[2].Value).ToDictionary(m => m.Groups[1].Value, m => m.Groups[2].Value.Trim());
            string Field(string name) => fields.TryGetValue(name, out var v) ? v : throw new InvalidDataException($"{id} has no {name}");

            string encounters = Field("wildEncountersArchiveID");
            headers[id] = new MapHeader(
                Id: id,
                Index: index.TryGetValue(id, out int i) ? i : throw new InvalidDataException($"{id} is not in the list of map headers"),
                AreaData: Field("areaDataArchiveID"),
                Matrix: int.Parse(Field("mapMatrixID")["map_matrix_".Length..]),
                Scripts: Field("scriptsArchiveID"),
                Events: Field("eventsArchiveID"),
                Encounters: encounters == "ENCOUNTERS_NONE" ? null : encounters,
                DayMusic: Field("dayMusicID"),
                NightMusic: Field("nightMusicID"),
                Label: Field("mapLabelTextID"),
                LabelWindow: Field("mapLabelWindowID")["MAP_LABEL_WINDOW_".Length..],
                Weather: Field("weather")["OVERWORLD_WEATHER_".Length..],
                Camera: Field("cameraType")["CAMERA_TYPE_".Length..],
                MapType: Field("mapType")["MAP_TYPE_".Length..],
                BattleBackground: Field("battleBG")["BACKGROUND_".Length..],
                Bike: Field("isBikeAllowed") == "TRUE",
                Running: Field("isRunningAllowed") == "TRUE",
                EscapeRope: Field("isEscapeRopeAllowed") == "TRUE",
                Fly: Field("isFlyAllowed") == "TRUE");
        }
        return headers;
    }

    public static Dictionary<string, string> ParseLocationNames(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var names = new Dictionary<string, string>();
        foreach (var message in doc.RootElement.GetProperty("messages").EnumerateArray())
        {
            var text = message.GetProperty("en_US");
            names[message.GetProperty("id").GetString()!] = text.ValueKind == JsonValueKind.Array
                ? string.Join(" ", text.EnumerateArray().Select(t => t.GetString()))
                : text.GetString() ?? "";
        }
        return names;
    }

    /// <summary>
    /// What lies in each item ball (<c>scripts_visible_items.s</c>). The file lists its scripts in order, the nth
    /// being script 7000 + n, and each sets the item and how many of it before going on to the part they share.
    /// A script that sets no item comes back empty, so the numbers stay the file's.
    /// </summary>
    public static List<(string Item, int Count)> ParseVisibleItems(string source)
    {
        var order = new List<string>();
        var items = new Dictionary<string, string>();
        var counts = new Dictionary<string, int>();
        string? label = null;
        foreach (string raw in source.Split('\n'))
        {
            string line = raw.Trim();
            if (line.StartsWith("ScriptEntry ", StringComparison.Ordinal)) order.Add(line["ScriptEntry ".Length..].Trim());
            else if (line.EndsWith(':')) label = line[..^1];
            else if (label != null && line.StartsWith("SetVar VAR_0x8008,", StringComparison.Ordinal)) items[label] = line[(line.IndexOf(',') + 1)..].Trim();
            else if (label != null && line.StartsWith("SetVar VAR_0x8009,", StringComparison.Ordinal) && int.TryParse(line[(line.IndexOf(',') + 1)..].Trim(), out int count)) counts[label] = count;
        }
        return order.Select(name => items.TryGetValue(name, out string? item) && item.StartsWith("ITEM_", StringComparison.Ordinal)
            ? (item, counts.GetValueOrDefault(name, 1)) : ("", 0)).ToList();
    }

    [GeneratedRegex(@"HIDDEN_ITEM_ENTRY\(\s*(ITEM_\w+)\s*,\s*(\d+)\s*,\s*(\d+)\s*,\s*(FLAG_\w+)\s*\)")]
    private static partial Regex HiddenItemLine();

    /// <summary>
    /// The hidden items (<c>hidden_items.h</c>): one entry each, with the item, how many, how far off the Dowsing
    /// Machine notices it and the flag set once it is found. A hidden item's number is its flag's place among the
    /// hidden items' flags (<c>vars_flags.txt</c>: the first is <c>HIDDEN_ITEM_FLAGS_START</c>, and each line
    /// after it is the next), which is what its script counts from 8000 by. The flags have gaps the table does
    /// not have, so an entry's place in the table is not its number.
    /// </summary>
    public static Dictionary<int, HiddenItemEntry> ParseHiddenItems(string header, string flags)
    {
        var numbers = new Dictionary<string, int>();
        int next = -1;
        foreach (string raw in flags.Split('\n'))
        {
            string line = raw.Trim();
            if (next < 0)
            {
                if (!line.EndsWith("= HIDDEN_ITEM_FLAGS_START", StringComparison.Ordinal)) continue;
                numbers[line[..line.IndexOf(' ')]] = 0;
                next = 1;
            }
            else if (line.StartsWith("HIDDEN_ITEM_FLAGS_END", StringComparison.Ordinal)) break;
            else if (line.Length > 0 && !line.Contains('=')) numbers[line] = next++;
        }

        var items = new Dictionary<int, HiddenItemEntry>();
        foreach (Match m in HiddenItemLine().Matches(header))
        {
            var entry = new HiddenItemEntry(m.Groups[1].Value, int.Parse(m.Groups[2].Value), int.Parse(m.Groups[3].Value), m.Groups[4].Value);
            if (!numbers.TryGetValue(entry.Flag, out int number)) throw new InvalidDataException($"The hidden item's flag {entry.Flag} is not among the hidden items' flags");
            items[number] = entry;
        }
        return items;
    }

    [GeneratedRegex(@"^\s*TILE_BEHAVIOR_(\w+?)\s*(=\s*0)?,", RegexOptions.Multiline)]
    private static partial Regex BehaviourLine();

    /// <summary>The enum lists every value from 0 in order, so a name's position is its value.</summary>
    public static List<string> ParseBehaviourNames(string header) =>
        BehaviourLine().Matches(header).Select(m => m.Groups[1].Value).Where(n => n != "MAX").ToList();
}

/// <summary>An item hidden in the ground: its constant, how many, the Dowsing Machine's range for it and the flag of its finding.</summary>
public sealed record HiddenItemEntry(string Item, int Count, int Range, string Flag);

/// <summary>An area's table of wild Pokémon as species constants, slot by slot (see <see cref="DecompMaps.Encounters"/>).</summary>
public sealed record EncounterTable(
    List<string> Land, List<string> Day, List<string> Night, List<string> Surf, List<string> OldRod, List<string> GoodRod, List<string> SuperRod)
{
    /// <summary>The grass at one of Platinum's times of day: the day and the night put their two species in slots 2 and 3.</summary>
    public List<string> Grass(string time)
    {
        var slots = new List<string>(Land);
        var swap = time switch { "day" => Day, "night" => Night, _ => new List<string>() };
        for (int i = 0; i < swap.Count && 2 + i < slots.Count; i++) slots[2 + i] = swap[i];
        return slots;
    }
}

/// <summary>One area of the game: a town, a route, a cave floor, the inside of a house.</summary>
public sealed record MapHeader(
    string Id, int Index, string AreaData, int Matrix, string Scripts, string Events, string? Encounters,
    string DayMusic, string NightMusic, string Label, string LabelWindow, string Weather, string Camera,
    string MapType, string BattleBackground, bool Bike, bool Running, bool EscapeRope, bool Fly)
{
    /// <summary>The header's own name in lower case, without the <c>MAP_HEADER_</c> prefix: <c>twinleaf_town</c>.</summary>
    public string Key => Id["MAP_HEADER_".Length..].ToLowerInvariant();
}

/// <summary>
/// A grid of chunks. The overworld is matrix 0, 30 by 30, and says for each chunk which area it belongs to and how
/// high it sits; the others are caves and insides of buildings, most a single chunk belonging to the area that
/// loads them.
/// </summary>
public sealed class Matrix
{
    public const int NoLand = -1;

    public int Id { get; private init; }
    public string Name { get; private init; } = "";
    public int Width { get; private init; }
    public int Height { get; private init; }

    /// <summary>Land data id per cell, row by row; <see cref="NoLand"/> where there is none.</summary>
    public int[] Land { get; private init; } = Array.Empty<int>();

    /// <summary>The area each chunk belongs to; null when every chunk belongs to the area that loads the matrix.</summary>
    public string[]? Headers { get; private init; }

    /// <summary>How high each chunk sits, in half tiles; null when all sit at zero.</summary>
    public int[]? Altitudes { get; private init; }

    public int LandAt(int x, int y) => Land[y * Width + x];
    public string? HeaderAt(int x, int y) => Headers?[y * Width + x];
    public int AltitudeAt(int x, int y) => Altitudes?[y * Width + x] ?? 0;

    public static Matrix Parse(int id, string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var maps = root.GetProperty("maps");
        int height = maps.GetArrayLength();
        int width = height == 0 ? 0 : maps[0].GetArrayLength();

        T[]? Grid<T>(string property, Func<JsonElement, T> read)
        {
            var rows = root.GetProperty(property);
            if (rows.GetArrayLength() == 0) return null;
            if (rows.GetArrayLength() != height || rows.EnumerateArray().Any(r => r.GetArrayLength() != width))
                throw new InvalidDataException($"Matrix {id}: {property} is not {width} by {height}");
            return rows.EnumerateArray().SelectMany(r => r.EnumerateArray()).Select(read).ToArray();
        }

        return new Matrix
        {
            Id = id,
            Name = root.GetProperty("name").GetString() ?? "",
            Width = width,
            Height = height,
            Land = Grid("maps", e => e.GetString() is "MAP_NONE" or null ? NoLand : int.Parse(e.GetString()!["MAP_".Length..]))!,
            Headers = Grid("headers", e => e.GetString()!),
            Altitudes = Grid("altitudes", e => e.GetInt32())
        };
    }
}

/// <summary>What is placed on an area, in tiles of the area's matrix (for the overworld: global tile coordinates).</summary>
public sealed class AreaEvents
{
    [JsonPropertyName("bg_events")] public List<BgEvent> Signs { get; set; } = new();
    [JsonPropertyName("object_events")] public List<ObjectEvent> Objects { get; set; } = new();
    [JsonPropertyName("warp_events")] public List<WarpEvent> Warps { get; set; } = new();
    [JsonPropertyName("coord_events")] public List<CoordEvent> Triggers { get; set; } = new();

    public static AreaEvents Parse(string json) => JsonSerializer.Deserialize<AreaEvents>(json, Options) ?? new AreaEvents();

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        Converters = { new NumberOrName() }
    };

    /// <summary>Scripts, flags and values are numbers in most files and named constants in the rest.</summary>
    private sealed class NumberOrName : JsonConverter<string>
    {
        public override string Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options) => reader.TokenType switch
        {
            JsonTokenType.String => reader.GetString() ?? "",
            JsonTokenType.Number => reader.GetInt64().ToString(),
            JsonTokenType.True => "1",
            JsonTokenType.False => "0",
            _ => throw new JsonException($"Expected a number or a name, found {reader.TokenType}")
        };

        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options) => writer.WriteStringValue(value);
    }

    /// <summary>Something to read or find: a signboard, a hidden item.</summary>
    public sealed class BgEvent
    {
        public string Script { get; set; } = "";
        public int Type { get; set; }
        public int X { get; set; }
        public int Z { get; set; }
        public int Y { get; set; }
        public string PlayerFacingDir { get; set; } = "";
    }

    /// <summary>A person or an object that stands on the map: who it looks like, how it moves, which script it runs.</summary>
    public sealed class ObjectEvent
    {
        public string Id { get; set; } = "";
        public string GraphicsId { get; set; } = "";
        public string MovementType { get; set; } = "";
        public string TrainerType { get; set; } = "";
        public string HiddenFlag { get; set; } = "";
        public string Script { get; set; } = "";
        public int InitialDir { get; set; }

        /// <summary>What the object keeps for its script: for a trainer, first, how many tiles ahead they see.</summary>
        public List<int> Data { get; set; } = new();
        public int MovementRangeX { get; set; }
        public int MovementRangeZ { get; set; }
        public int X { get; set; }
        public int Z { get; set; }
        public int Y { get; set; }
    }

    public sealed class WarpEvent
    {
        public int X { get; set; }
        public int Z { get; set; }
        public string DestHeaderId { get; set; } = "";
        public int DestWarpId { get; set; }
    }

    /// <summary>A rectangle of tiles that starts a script when stepped on while a variable has a value.</summary>
    public sealed class CoordEvent
    {
        public string Script { get; set; } = "";
        public int X { get; set; }
        public int Z { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Length { get; set; }
        public string Var { get; set; } = "";
        public string Value { get; set; } = "";
    }
}
