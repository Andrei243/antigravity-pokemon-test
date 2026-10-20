using System.Globalization;
using System.Text.RegularExpressions;

namespace MapImporter.Hg;

/// <summary>
/// One of HeartGold's areas (plan 20 · J1): a block of <c>src/data/map_headers.h</c>, keyed by its <c>MAP_</c>
/// constant (<c>include/constants/maps.h</c>), with the fields of <c>include/map_header.h</c> this game reads.
/// <see cref="Short"/> is the constant's comment, the original's own short name (<c>T20</c>, <c>R29</c>), which the
/// encounter file and the events' archive go by.
/// </summary>
public sealed record HgHeader(
    string Id, int Index, string Short, int Matrix, string Events, string? Encounters, int AreaData,
    string DayMusic, string NightMusic, string Section, int WorldMapX, int WorldMapY, int Region,
    int Weather, string MapType, int Camera, string FollowMode, string BattleBackground,
    bool Bike, bool EscapeRope, bool Fly, bool OutgoingCalls, bool IncomingCalls, bool RadioSignal)
{
    /// <summary>
    /// The constant without <c>MAP_</c>, in lower case: <c>new_bark</c>, <c>route_29</c>; one another region's world has
    /// already takes Johto's prefix (<see cref="HgWorld.Clashes"/>: <c>johto_victory_road_1f</c>).
    /// </summary>
    public string Key => HgWorld.KeyOf(Id);

    /// <summary>Johto's areas are 0 in <c>regionNo</c>, Kanto's 1.</summary>
    public bool Kanto => Region == 1;

    /// <summary>The area's name, from its map section's constant (<c>MAPSEC_NEW_BARK_TOWN</c> is "New Bark Town"): no text of the game's is read.</summary>
    public string Name => HgNames.Section(Section);
}

/// <summary>
/// The map data of pret/pokeheartgold (plan 20 · J1): the headers, the binary matrices, the chunks in the archive
/// <c>files/a/0/6/5</c>, the building models' names and boxes out of <c>bm_field.narc</c> and <c>bm_room.narc</c>,
/// each area's events and the behaviours' names.
/// </summary>
public sealed partial class HgMaps
{
    public const string ChunkArchive = "files/a/0/6/5";
    public const string FieldModels = "files/fielddata/build_model/bm_field.narc";
    public const string RoomModels = "files/fielddata/build_model/bm_room.narc";

    private readonly string root;
    private readonly Narc chunks;
    private readonly Narc fieldModels;
    private readonly Narc roomModels;
    private readonly Dictionary<int, LandData> land = new();
    private readonly Dictionary<int, Matrix> matrices = new();
    private readonly Dictionary<string, AreaEvents> events = new();
    private readonly Dictionary<(bool, int), ModelInfo?> models = new();
    private readonly Dictionary<int, string> matrixFiles = new();

    /// <summary>The <c>MAP_</c> constants by number (<c>MAP_EVERYWHERE</c> is 0).</summary>
    public IReadOnlyList<string> HeaderIds { get; }
    public IReadOnlyDictionary<string, HgHeader> Headers { get; }

    /// <summary>HeartGold's name for each behaviour value (<c>TILE_BEHAVIOR_HEADBUTT</c>, without the prefix), where it names one.</summary>
    public IReadOnlyDictionary<int, string> BehaviourNames { get; }

    public int LandCount => chunks.Count;
    public int MatrixCount => matrixFiles.Count;

    public HgMaps(string root)
    {
        this.root = root;
        if (!File.Exists(Path.Combine(root, ChunkArchive))) throw new DirectoryNotFoundException($"No pokeheartgold map data in {root}");
        chunks = Narc.Read(Path.Combine(root, ChunkArchive));
        fieldModels = Narc.Read(Path.Combine(root, FieldModels));
        roomModels = Narc.Read(Path.Combine(root, RoomModels));
        var ids = ParseMapIds(File.ReadAllText(Path.Combine(root, "include", "constants", "maps.h")));
        HeaderIds = ids.Select(i => i.Id).ToList();
        Headers = ParseHeaders(File.ReadAllText(Path.Combine(root, "src", "data", "map_headers.h")), ids);
        BehaviourNames = ParseBehaviourNames(File.ReadAllText(Path.Combine(root, "include", "constants", "metatile_behavior.h")));
        foreach (string file in Directory.EnumerateFiles(Path.Combine(root, "files", "fielddata", "mapmatrix", "map_matrix"), "map_matrix_*.bin"))
        {
            var m = Regex.Match(Path.GetFileName(file), @"^map_matrix_(\d{4})");
            if (m.Success) matrixFiles[int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture)] = file;
        }
    }

    public LandData Land(int id)
    {
        if (!land.TryGetValue(id, out var data))
        {
            try { data = LandData.Parse(chunks.Member(id), hgss: true); }
            catch (InvalidDataException e) { throw new InvalidDataException($"{ChunkArchive} member {id}: {e.Message}", e); }
            land[id] = data;
        }
        return data;
    }

    public Matrix Matrix(int id)
    {
        if (!matrices.TryGetValue(id, out var matrix))
        {
            if (!matrixFiles.TryGetValue(id, out string? file)) throw new InvalidDataException($"No map_matrix_{id:0000}");
            matrix = MapImporter.Matrix.ParseBinary(id, File.ReadAllBytes(file), n => n < HeaderIds.Count ? HeaderIds[n] : $"MAP_{n}");
            if (id == SafariMatrix) matrix = matrix.Widened(SafariWidth).WithCells(SafariCells());
            matrices[id] = matrix;
        }
        return matrix;
    }

    /// <summary>The Safari Zone's matrix (<c>D47R0102</c>): a ring of trees round a hole of three chunks by two, and its way in below.</summary>
    public const int SafariMatrix = 212;

    /// <summary>The chunk of the Safari Zone's first area, the Plains; area <c>n</c>'s is this plus <c>n</c> (and its own matrix 197 plus <c>n</c>).</summary>
    public const int SafariFirstChunk = 652;

    /// <summary>The six areas of the Safari Zone's opening arrangement, by HeartGold's numbers (<c>include/constants/safari.h</c>), row by row from the north-west.</summary>
    public static readonly int[] SafariOpening = { 0, 1, 2, 3, 4, 5 };

    /// <summary>The six the Area Customizer would swap in (Forest, Swamp, Marshland; Wasteland, Mountain, Desert), laid east of the opening six.</summary>
    public static readonly int[] SafariBeyond = { 6, 7, 8, 9, 10, 11 };

    /// <summary>The zone's matrix made wide enough for both sixes: the ring, six chunks, the ring.</summary>
    public const int SafariWidth = 8;

    /// <summary>
    /// The hole HeartGold fills as the zone loads (<c>src/map_matrix.c</c>: the cell of place <c>i</c> of the
    /// arrangement, <c>x = i % 3</c>, <c>y = i / 3</c>, inside the ring, is chunk 652 plus its area's number), filled
    /// with the opening arrangement (<c>SafariZone_InitAreaSet</c>: Plains, Meadow and Savannah north, Peak, Rocky Beach
    /// and Wetland south), each cell the area of its own header (<c>MAP_SAFARI_ZONE_01</c> for the Plains) so that the
    /// game finds its wild Pokémon by place. The Area Customizer's other six are laid in the same way east of them, in a
    /// matrix widened for them (<see cref="SafariWidth"/>): all twelve areas lie open at once, where HeartGold shows six
    /// of them at a time (docs/mechanics/rulings.md, "The Johto Safari Zone").
    /// </summary>
    private IEnumerable<(int X, int Y, int Land, string Header)> SafariCells() =>
        SafariOpening.Select((area, i) => (i % 3 + 1, i / 3 + 1, area))
            .Concat(SafariBeyond.Select((area, i) => (i % 3 + 4, i / 3 + 1, area)))
            .Select(c => (c.Item1, c.Item2, SafariFirstChunk + c.area, $"MAP_SAFARI_ZONE_{c.area + 1:00}"));

    /// <summary>Every area of the zone, the opening six first.</summary>
    public static IEnumerable<int> SafariAreas => SafariOpening.Concat(SafariBeyond);

    public IEnumerable<int> MatrixIds => matrixFiles.Keys.OrderBy(k => k);

    /// <summary>A building model's name and box: of the field's set outdoors, of the rooms' set in a room.</summary>
    public ModelInfo? Model(int id, bool room)
    {
        if (!models.TryGetValue((room, id), out var info))
        {
            var narc = room ? roomModels : fieldModels;
            info = id >= 0 && id < narc.Count ? ModelInfo.Read(narc.Member(id)) : null;
            models[(room, id)] = info;
        }
        return info;
    }

    public int ModelCount(bool room) => room ? roomModels.Count : fieldModels.Count;

    /// <summary>The events of an area by the archive's member its header names (<c>057_T20</c>), as <see cref="AreaEvents"/>.</summary>
    public AreaEvents Events(string name)
    {
        if (!events.TryGetValue(name, out var e))
        {
            string path = Path.Combine(root, "files", "fielddata", "eventdata", "zone_event", name + ".json");
            events[name] = e = File.Exists(path) ? HgEvents.Parse(File.ReadAllText(path)) : new AreaEvents();
        }
        return e;
    }

    public string PathOf(params string[] parts) => Path.Combine(new[] { root }.Concat(parts).ToArray());

    // ---------------------------------------------------------------- the lists

    [GeneratedRegex(@"^#define\s+(MAP_\w+)\s+(\d+)\s*//\s*MAP_(\w+)", RegexOptions.Multiline)]
    private static partial Regex MapIdLine();

    /// <summary><c>#define MAP_NEW_BARK 60 // MAP_T20</c>: each map's constant, number and short name, by number.</summary>
    public static List<(string Id, int Index, string Short)> ParseMapIds(string header)
    {
        var ids = MapIdLine().Matches(header).Select(m => (m.Groups[1].Value, int.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture), m.Groups[3].Value))
            .Where(i => i.Item1 != "MAP_COUNT").OrderBy(i => i.Item2).ToList();
        for (int i = 0; i < ids.Count; i++)
            if (ids[i].Item2 != i) throw new InvalidDataException($"maps.h numbers {ids[i].Item1} {ids[i].Item2}, where {i} was expected");
        return ids;
    }

    [GeneratedRegex(@"\[(MAP_\w+)\]\s*=\s*\{(.*?)\n\s*\},", RegexOptions.Singleline)]
    private static partial Regex HeaderBlock();

    [GeneratedRegex(@"\.(\w+)\s*=\s*([^,\n]+),")]
    private static partial Regex HeaderField();

    /// <summary><c>src/data/map_headers.h</c>: one <c>[MAP_X] = { .field = value, … }</c> block for each area.</summary>
    public static Dictionary<string, HgHeader> ParseHeaders(string source, IReadOnlyList<(string Id, int Index, string Short)> ids)
    {
        var index = ids.ToDictionary(i => i.Id, i => i);
        var headers = new Dictionary<string, HgHeader>(StringComparer.Ordinal);
        foreach (Match block in HeaderBlock().Matches(source))
        {
            string id = block.Groups[1].Value;
            var fields = HeaderField().Matches(block.Groups[2].Value).ToDictionary(m => m.Groups[1].Value, m => m.Groups[2].Value.Trim());
            string Field(string name) => fields.TryGetValue(name, out var v) ? v : throw new InvalidDataException($"{id} has no {name}");
            int Number(string name) => int.Parse(Field(name), CultureInfo.InvariantCulture);
            bool Flag(string name) => Field(name) == "TRUE";
            if (!index.TryGetValue(id, out var entry)) throw new InvalidDataException($"{id} is not in maps.h");
            var matrix = Regex.Match(Field("matrixId"), @"map_matrix_(\d{4})");
            var events = Regex.Match(Field("eventsBank"), @"^NARC_zone_event_(\w+)_bin$");
            if (!matrix.Success || !events.Success) throw new InvalidDataException($"{id}: its matrix or its events are not named as the archives name them");
            string encounters = Field("wildEncounterBank");
            headers[id] = new HgHeader(
                Id: id, Index: entry.Index, Short: entry.Short,
                Matrix: int.Parse(matrix.Groups[1].Value, CultureInfo.InvariantCulture),
                Events: events.Groups[1].Value,
                Encounters: encounters == "ENCDATA_NA" ? null : encounters["ENCDATA_".Length..],
                AreaData: Number("areaDataBank"),
                DayMusic: Field("dayMusicId"), NightMusic: Field("nightMusicId"),
                Section: Field("mapsec"),
                WorldMapX: Number("worldMapX"), WorldMapY: Number("worldMapY"),
                Region: Field("regionNo") == "MAP_REGION_KANTO" ? 1 : 0,
                Weather: Number("weather"),
                MapType: Field("mapType")["MAP_TYPE_".Length..],
                Camera: Number("cameraType"),
                FollowMode: Field("followMode")["MAP_FOLLOWMODE_".Length..],
                BattleBackground: Field("battleBg")["BATTLE_BG_".Length..],
                Bike: Flag("bikeAllowed"), EscapeRope: Flag("escapeRopeAllowed"), Fly: Flag("flyAllowed"),
                OutgoingCalls: Flag("outgoingCalls"), IncomingCalls: Flag("incomingCalls"), RadioSignal: Flag("radioSignal"));
        }
        return headers;
    }

    /// <summary>
    /// <c>enum TILE_BEHAVIOR</c>: every value from 0 in order, most of them a bare number (<c>TILE_BEHAVIOR_4</c>);
    /// the ones it names, by value. <c>TILE_BEHAVIOR_NONE = 255</c> is set apart.
    /// </summary>
    public static Dictionary<int, string> ParseBehaviourNames(string header)
    {
        var names = new Dictionary<int, string>();
        int start = header.IndexOf("enum TILE_BEHAVIOR", StringComparison.Ordinal);
        if (start < 0) throw new InvalidDataException("metatile_behavior.h has no enum TILE_BEHAVIOR");
        int value = 0;
        foreach (Match m in Regex.Matches(header[start..], @"^\s*TILE_BEHAVIOR_(\w+?)\s*(?:=\s*(\d+))?\s*,?\s*$", RegexOptions.Multiline))
        {
            if (m.Groups[2].Success) value = int.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture);
            string name = m.Groups[1].Value;
            if (!Regex.IsMatch(name, @"^\d+$")) names[value] = name;
            value++;
        }
        return names;
    }
}

/// <summary>HeartGold's constants as words: a map section's, a song's.</summary>
public static class HgNames
{
    /// <summary><c>MAPSEC_NEW_BARK_TOWN</c> to "New Bark Town", <c>MAPSEC_ROUTE_29</c> to "Route 29", <c>MAPSEC_MT_MORTAR</c> to "Mt. Mortar".</summary>
    public static string Section(string constant)
    {
        string body = constant.StartsWith("MAPSEC_", StringComparison.Ordinal) ? constant["MAPSEC_".Length..] : constant;
        var words = body.Split('_', StringSplitOptions.RemoveEmptyEntries).Select(w => w switch
        {
            "MT" => "Mt.",
            "SS" => "S.S.",
            "OF" or "THE" or "AND" => w.ToLowerInvariant(),
            _ => char.ToUpperInvariant(w[0]) + w[1..].ToLowerInvariant()
        }).ToList();
        if (words.Count > 0) words[0] = char.ToUpperInvariant(words[0][0]) + words[0][1..];
        return string.Join(' ', words).Replace("Pokemon", "Pokémon", StringComparison.Ordinal);
    }
}
