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
        "/generated/map_headers.txt", "/include/constants/field/map_tile_behaviors.h"
    };

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
    }

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
    public List<(string Species, int Level)> LandEncounters(string name)
    {
        string path = Path.Combine(root, "res", "field", "encounters", name + ".json");
        var slots = new List<(string, int)>();
        if (!File.Exists(path)) return slots;
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        if (!doc.RootElement.TryGetProperty("land_rate", out var rate) || rate.GetInt32() == 0) return slots;
        foreach (var slot in doc.RootElement.GetProperty("land_encounters").EnumerateArray())
            slots.Add((slot.GetProperty("species").GetString() ?? "", slot.GetProperty("level").GetInt32()));
        return slots;
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

    [GeneratedRegex(@"^\s*TILE_BEHAVIOR_(\w+?)\s*(=\s*0)?,", RegexOptions.Multiline)]
    private static partial Regex BehaviourLine();

    /// <summary>The enum lists every value from 0 in order, so a name's position is its value.</summary>
    public static List<string> ParseBehaviourNames(string header) =>
        BehaviourLine().Matches(header).Select(m => m.Groups[1].Value).Where(n => n != "MAX").ToList();
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
