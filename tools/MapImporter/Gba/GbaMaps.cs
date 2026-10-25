using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MapImporter.Gba;

/// <summary>
/// One of the Game Boy Advance decompilations (plan 18 · W6): pret/pokefirered (Kanto) or pret/pokeemerald (Hoenn).
/// The two keep their maps alike and differ in a few numbers, which are here.
/// </summary>
public sealed record GbaGame(string Name, string Region, string RegionName, int AttributeBytes, string StartMap)
{
    public static readonly GbaGame FireRed = new("firered", "kanto", "Kanto", 4, "MAP_PALLET_TOWN");
    public static readonly GbaGame Emerald = new("emerald", "hoenn", "Hoenn", 2, "MAP_LITTLEROOT_TOWN");

    public static GbaGame Of(string name) => name.ToLowerInvariant() switch
    {
        "firered" => FireRed,
        "emerald" => Emerald,
        _ => throw new ArgumentException($"Unknown game '{name}': firered or emerald.")
    };

    /// <summary>The behaviour's bits of a metatile's attributes: nine in FireRed's four bytes, eight in Emerald's two.</summary>
    public int BehaviourMask => AttributeBytes == 4 ? 0x1FF : 0xFF;
}

/// <summary>A map's join to another (<c>connections</c> of its <c>map.json</c>): up, down, left, right, dive or emerge.</summary>
public sealed record GbaConnection(string Map, int Offset, string Direction);

/// <summary>
/// A person or a thing of a map's <c>object_events</c> (plan 18 · W7): its looks, where it stands (tiles of its own
/// map), how it moves, whether it is a trainer, its script's label and the flag that hides it ("0" for none). A
/// <c>clone</c> (a neighbour's object shown across the border) is not kept: the neighbour has it.
/// </summary>
public sealed record GbaObject(string LocalId, string Graphics, int X, int Y, int Elevation, string Movement, int RangeX, int RangeY,
    string TrainerType, string SightOrBerryTree, string Script, string Flag);

/// <summary>A tile of a map's <c>warp_events</c>: it leads to the warp numbered <see cref="DestWarp"/> of <see cref="DestMap"/>.</summary>
public sealed record GbaWarp(int X, int Y, int Elevation, string DestMap, int DestWarp);

/// <summary>A tile of a map's <c>coord_events</c>: a trigger (a script while a variable has a value) or a change of weather.</summary>
public sealed record GbaCoord(string Type, int X, int Y, int Elevation, string Var, string VarValue, string Script, string Weather);

/// <summary>
/// A tile of a map's <c>bg_events</c>: a sign (a script read facing it), a hidden item (its item, how many, the flag
/// set once found) or a secret base's spot.
/// </summary>
public sealed record GbaBg(string Type, int X, int Y, int Elevation, string Script, string Item, int Quantity, string Flag);

/// <summary>What the importer keeps of a map's <c>map.json</c>: what it is, how it joins the others, and its four lists of events (W7).</summary>
public sealed class GbaMap
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string Layout { get; init; } = "";
    public string Music { get; init; } = "";
    public string Section { get; init; } = "";
    public string Weather { get; init; } = "";
    public string MapType { get; init; } = "";
    public bool Cycling { get; init; }
    public bool Running { get; init; }
    public bool Escaping { get; init; }
    /// <summary>The map is dark until Flash is used (map.json's <c>requires_flash</c>: Rock Tunnel, plan 19 · K5).</summary>
    public bool Flash { get; init; }
    public string Group { get; init; } = "";
    public List<GbaConnection> Connections { get; init; } = new();
    public List<GbaObject> Objects { get; init; } = new();
    public List<GbaWarp> Warps { get; init; } = new();
    public List<GbaCoord> Coords { get; init; } = new();
    public List<GbaBg> Bgs { get; init; } = new();

    /// <summary>The key the world files know it by: <c>PalletTown</c> is <c>pallet_town</c>, <c>Route1</c> <c>route_1</c>.</summary>
    public string Key => KeyPrefix + GbaMaps.KeyOf(Name);

    /// <summary>What goes before the key where it would meet one of Sinnoh's (<c>kanto_</c> for FireRed's Victory Road).</summary>
    public string KeyPrefix { get; set; } = "";
}

/// <summary>A layout of <c>layouts.json</c> and its block data: sixteen bits a block, ten of metatile, two of collision, four of elevation.</summary>
public sealed class GbaLayout
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public int Width { get; init; }
    public int Height { get; init; }
    public string Primary { get; init; } = "";
    public string Secondary { get; init; } = "";
    public ushort[] Blocks { get; init; } = Array.Empty<ushort>();

    public ushort Block(int x, int y) => Blocks[y * Width + x];
    public static int MetatileOf(ushort block) => block & 0x3FF;
    public static int CollisionOf(ushort block) => (block >> 10) & 0x3;
    public static int ElevationOf(ushort block) => block >> 12;
}

/// <summary>
/// Reads a GBA decompilation's maps: the map groups, each map's <c>map.json</c>, the layouts and their
/// <c>map.bin</c>, each tileset's <c>metatile_attributes.bin</c>, and the two constant headers that name the
/// behaviours and label the metatiles. Nothing it reads is graphics.
/// </summary>
public sealed class GbaMaps
{
    public GbaGame Game { get; }

    /// <summary>Every map, in the order of the groups (<c>group_order</c>) and of each group's list.</summary>
    public List<GbaMap> Maps { get; } = new();
    public Dictionary<string, GbaMap> ById { get; } = new(StringComparer.Ordinal);
    public Dictionary<string, GbaLayout> Layouts { get; } = new(StringComparer.Ordinal);

    /// <summary>What the reader left out, and why.</summary>
    public List<string> Problems { get; } = new();

    /// <summary>The <c>MB_*</c> names by value, as <c>metatile_behaviors.h</c> defines them.</summary>
    public Dictionary<int, string> BehaviourNames { get; }

    /// <summary><c>METATILE_&lt;Tileset&gt;_&lt;Label&gt;</c>, by the tileset's symbol (<c>gTileset_General</c>) and the metatile.</summary>
    public Dictionary<(string Tileset, int Metatile), string> Labels { get; }

    /// <summary>How many metatiles a primary tileset holds; a layout's metatile past it is its secondary tileset's.</summary>
    public int PrimaryCount { get; }

    private readonly Dictionary<string, ushort[]> attributes = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> attributeFiles;
    private readonly string dir;

    public GbaMaps(string dir, GbaGame game)
    {
        this.dir = dir;
        Game = game;
        BehaviourNames = ParseBehaviours(File.ReadAllText(Path.Combine(dir, "include", "constants", "metatile_behaviors.h")));
        string labels = Path.Combine(dir, "include", "constants", "metatile_labels.h");
        Labels = File.Exists(labels) ? ParseLabels(File.ReadAllText(labels)) : new();
        PrimaryCount = ParseDefine(File.ReadAllText(Path.Combine(dir, "include", "fieldmap.h")), "NUM_METATILES_IN_PRIMARY")
            ?? throw new InvalidDataException("include/fieldmap.h has no NUM_METATILES_IN_PRIMARY");
        attributeFiles = AttributeFiles(
            File.ReadAllText(Path.Combine(dir, "src", "data", "tilesets", "headers.h")),
            File.ReadAllText(Path.Combine(dir, "src", "data", "tilesets", "metatiles.h")));

        using (var layouts = JsonDocument.Parse(File.ReadAllText(Path.Combine(dir, "data", "layouts", "layouts.json"))))
            foreach (var l in layouts.RootElement.GetProperty("layouts").EnumerateArray())
            {
                if (!l.TryGetProperty("id", out var id)) continue;   // an empty place in the table
                var layout = new GbaLayout
                {
                    Id = id.GetString()!,
                    Name = l.GetProperty("name").GetString()!,
                    Width = l.GetProperty("width").GetInt32(),
                    Height = l.GetProperty("height").GetInt32(),
                    Primary = l.GetProperty("primary_tileset").GetString()!,
                    Secondary = l.GetProperty("secondary_tileset").GetString()!,
                    Blocks = ReadBlocks(Path.Combine(dir, l.GetProperty("blockdata_filepath").GetString()!))
                };
                // Emerald keeps a few unused layouts whose block data doesn't fill them: left out and reported
                if (layout.Blocks.Length < layout.Width * layout.Height)
                {
                    Problems.Add($"{layout.Name}: {layout.Blocks.Length} blocks for {layout.Width}×{layout.Height}, left out");
                    continue;
                }
                Layouts[layout.Id] = layout;
            }

        using var groups = JsonDocument.Parse(File.ReadAllText(Path.Combine(dir, "data", "maps", "map_groups.json")));
        foreach (var group in groups.RootElement.GetProperty("group_order").EnumerateArray())
        {
            string groupName = group.GetString()!;
            foreach (var name in groups.RootElement.GetProperty(groupName).EnumerateArray())
            {
                var map = ReadMap(File.ReadAllText(Path.Combine(dir, "data", "maps", name.GetString()!, "map.json")), groupName);
                // Every game has a Victory Road, and Platinum's keys are victory_road_1f and the rest: a GBA game's take
                // the region's prefix (plan 19's design, kept the day FireRed's opened: plan 19 · K7)
                if (map.Name.StartsWith("VictoryRoad", StringComparison.Ordinal)) map.KeyPrefix = game.Region + "_";
                // So has FireRed a Safari Zone, whose keys are safari_zone_north and the rest: Emerald's takes Hoenn's
                // prefix, all seven of its maps alike (plan 21 · H6, part b: hoenn_safari_zone_south)
                if (game == GbaGame.Emerald && map.Name.StartsWith("SafariZone", StringComparison.Ordinal)) map.KeyPrefix = game.Region + "_";
                Maps.Add(map);
                ById[map.Id] = map;
            }
        }
    }

    public GbaLayout LayoutOf(GbaMap map) =>
        Layouts.TryGetValue(FirstLayouts.GetValueOrDefault(map.Id) ?? map.Layout, out var layout)
            ? layout : throw new InvalidDataException($"{map.Name}: no layout {map.Layout}");

    /// <summary>
    /// The layout a map is first seen in where the game's own script lays another over the map's (plan 21 · H8, part c):
    /// Emerald's Sky Pillar is laid clean by each floor's ON_TRANSITION until Rayquaza has come down
    /// (<c>VAR_SKY_PILLAR_STATE</c> under 2), and cracked only afterwards, which the post-game's climb crosses on the Mach
    /// Bike (plan 21 · H12, which swaps the map's own layout back in).
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string> FirstLayouts = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        // (Route 131 is always laid with Sky Pillar's island in it: its ON_TRANSITION sets the layout unconditionally)
        ["MAP_ROUTE131"] = "LAYOUT_ROUTE131_SKY_PILLAR",
        ["MAP_SKY_PILLAR_1F"] = "LAYOUT_SKY_PILLAR_1F_CLEAN",
        ["MAP_SKY_PILLAR_2F"] = "LAYOUT_SKY_PILLAR_2F_CLEAN",
        ["MAP_SKY_PILLAR_3F"] = "LAYOUT_SKY_PILLAR_3F_CLEAN",
        ["MAP_SKY_PILLAR_4F"] = "LAYOUT_SKY_PILLAR_4F_CLEAN",
        ["MAP_SKY_PILLAR_5F"] = "LAYOUT_SKY_PILLAR_5F_CLEAN",
        ["MAP_SKY_PILLAR_TOP"] = "LAYOUT_SKY_PILLAR_TOP_CLEAN"
    };

    /// <summary>The tileset a block's metatile is drawn from, and its number within it.</summary>
    public (string Tileset, int Index) TilesetOf(GbaLayout layout, int metatile) =>
        metatile < PrimaryCount ? (layout.Primary, metatile) : (layout.Secondary, metatile - PrimaryCount);

    /// <summary>The raw attributes of a metatile (its behaviour in the low bits), nought for one the file doesn't reach.</summary>
    public int AttributesOf(GbaLayout layout, int metatile)
    {
        var (tileset, index) = TilesetOf(layout, metatile);
        var table = Attributes(tileset);
        return index < table.Length ? table[index] : 0;
    }

    public int BehaviourOf(GbaLayout layout, int metatile) => AttributesOf(layout, metatile) & Game.BehaviourMask;

    public string? LabelOf(GbaLayout layout, int metatile) =>
        Labels.GetValueOrDefault((metatile < PrimaryCount ? layout.Primary : layout.Secondary, metatile));

    // Emerald keeps two bytes a metatile, FireRed four; only the low two carry the behaviour in either
    private ushort[] Attributes(string tileset)
    {
        if (attributes.TryGetValue(tileset, out var cached)) return cached;
        ushort[] table = Array.Empty<ushort>();
        if (attributeFiles.TryGetValue(tileset, out var file) && File.Exists(Path.Combine(dir, file)))
        {
            byte[] bytes = File.ReadAllBytes(Path.Combine(dir, file));
            int step = Game.AttributeBytes;
            table = new ushort[bytes.Length / step];
            for (int i = 0; i < table.Length; i++) table[i] = (ushort)(bytes[i * step] | bytes[i * step + 1] << 8);
        }
        return attributes[tileset] = table;
    }

    // ---------------------------------------------------------------- the parsers, public for the tests

    public static ushort[] ReadBlocks(string path)
    {
        byte[] bytes = File.ReadAllBytes(path);
        var blocks = new ushort[bytes.Length / 2];
        for (int i = 0; i < blocks.Length; i++) blocks[i] = (ushort)(bytes[i * 2] | bytes[i * 2 + 1] << 8);
        return blocks;
    }

    public static GbaMap ReadMap(string json, string group = "")
    {
        using var doc = JsonDocument.Parse(json);
        var r = doc.RootElement;
        string Str(string name) => r.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString()! : "";
        bool Bool(string name) => r.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.True;
        var connections = new List<GbaConnection>();
        if (r.TryGetProperty("connections", out var list) && list.ValueKind == JsonValueKind.Array)
            foreach (var c in list.EnumerateArray())
                connections.Add(new GbaConnection(c.GetProperty("map").GetString()!, c.GetProperty("offset").GetInt32(), c.GetProperty("direction").GetString()!));

        // Each event's fields, read the same way whichever game wrote it: a number may be written as a string ("0")
        static string S(JsonElement e, string name) => e.TryGetProperty(name, out var v)
            ? v.ValueKind switch { JsonValueKind.String => v.GetString()!, JsonValueKind.Number => v.GetRawText(), JsonValueKind.True => "1", JsonValueKind.False => "0", _ => "" }
            : "";
        static int I(JsonElement e, string name, int otherwise = 0) => e.TryGetProperty(name, out var v)
            ? v.ValueKind == JsonValueKind.Number ? v.GetInt32()
              : v.ValueKind == JsonValueKind.String && int.TryParse(v.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int n) ? n : otherwise
            : otherwise;
        IEnumerable<JsonElement> List(string name) => r.TryGetProperty(name, out var l) && l.ValueKind == JsonValueKind.Array ? l.EnumerateArray() : Enumerable.Empty<JsonElement>();

        var objects = new List<GbaObject>();
        foreach (var o in List("object_events"))
        {
            if (S(o, "type") == "clone") continue;
            objects.Add(new GbaObject(S(o, "local_id"), S(o, "graphics_id"), I(o, "x"), I(o, "y"), I(o, "elevation"), S(o, "movement_type"),
                I(o, "movement_range_x"), I(o, "movement_range_y"), S(o, "trainer_type"), S(o, "trainer_sight_or_berry_tree_id"), S(o, "script"), S(o, "flag")));
        }
        var warps = List("warp_events").Select(w => new GbaWarp(I(w, "x"), I(w, "y"), I(w, "elevation"), S(w, "dest_map"), I(w, "dest_warp_id"))).ToList();
        var coords = List("coord_events").Select(c => new GbaCoord(S(c, "type"), I(c, "x"), I(c, "y"), I(c, "elevation"), S(c, "var"), S(c, "var_value"),
            S(c, "script"), S(c, "weather"))).ToList();
        // A secret base's spot keeps its base's id where a sign keeps its script (plan 21 · H10: SECRET_BASE_RED_CAVE1_1)
        var bgs = List("bg_events").Select(b => new GbaBg(S(b, "type"), I(b, "x"), I(b, "y"), I(b, "elevation"),
            S(b, "type") == "secret_base" ? S(b, "secret_base_id") : S(b, "script"), S(b, "item"),
            I(b, "quantity", 1), S(b, "flag"))).ToList();
        return new GbaMap
        {
            Id = Str("id"), Name = Str("name"), Layout = Str("layout"), Music = Str("music"), Section = Str("region_map_section"),
            Weather = Str("weather"), MapType = Str("map_type"), Cycling = Bool("allow_cycling"), Running = Bool("allow_running"),
            Escaping = Bool("allow_escaping"), Flash = Bool("requires_flash"), Group = group, Connections = connections,
            Objects = objects, Warps = warps, Coords = coords, Bgs = bgs
        };
    }

    /// <summary><c>metatile_behaviors.h</c>: FireRed's <c>#define MB_X 0xNN</c> lines, or Emerald's <c>enum</c> counted from nought.</summary>
    public static Dictionary<int, string> ParseBehaviours(string header)
    {
        var names = new Dictionary<int, string>();
        foreach (Match m in Regex.Matches(header, @"^\s*#define\s+(MB_\w+)\s+(0x[0-9A-Fa-f]+|\d+)", RegexOptions.Multiline))
            if (m.Groups[1].Value is not ("MB_INVALID" or "NUM_METATILE_BEHAVIORS")) names.TryAdd(Number(m.Groups[2].Value), m.Groups[1].Value);
        var body = Regex.Match(header, @"enum\s*\{(.*?)\}", RegexOptions.Singleline);
        if (body.Success)
        {
            int next = 0;
            foreach (string raw in body.Groups[1].Value.Split(','))
            {
                string entry = Regex.Replace(raw, @"//[^\n]*|/\*.*?\*/", "", RegexOptions.Singleline).Trim();
                if (entry.Length == 0) continue;
                var parts = entry.Split('=', 2);
                string name = parts[0].Trim();
                if (parts.Length == 2) next = Number(parts[1].Trim());
                if (name.StartsWith("MB_", StringComparison.Ordinal)) names.TryAdd(next, name);
                next++;
            }
        }
        return names;
    }

    /// <summary><c>metatile_labels.h</c>: <c>#define METATILE_General_Grass 0x001</c>, by <c>gTileset_General</c> and 1.</summary>
    public static Dictionary<(string Tileset, int Metatile), string> ParseLabels(string header)
    {
        var labels = new Dictionary<(string, int), string>();
        foreach (Match m in Regex.Matches(header, @"^\s*#define\s+METATILE_([A-Za-z0-9]+)_(\w+)\s+(0x[0-9A-Fa-f]+|\d+)", RegexOptions.Multiline))
            labels.TryAdd(("gTileset_" + m.Groups[1].Value, Number(m.Groups[3].Value)), m.Groups[2].Value);
        return labels;
    }

    public static int? ParseDefine(string header, string name) =>
        Regex.Match(header, @"^\s*#define\s+" + name + @"\s+(0x[0-9A-Fa-f]+|\d+)", RegexOptions.Multiline) is { Success: true } m ? Number(m.Groups[1].Value) : null;

    /// <summary>Each tileset's attributes file: <c>headers.h</c> names its table, <c>metatiles.h</c> the file the table is read from.</summary>
    public static Dictionary<string, string> AttributeFiles(string headers, string metatiles)
    {
        var files = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (Match m in Regex.Matches(metatiles, @"(gMetatileAttributes_\w+)\[\]\s*=\s*INCBIN_U(?:16|32)\(""([^""]+)""\)"))
            files[m.Groups[1].Value] = m.Groups[2].Value;
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (Match m in Regex.Matches(headers, @"struct\s+Tileset\s+(gTileset_\w+)\s*=\s*\{(.*?)\};", RegexOptions.Singleline))
            if (Regex.Match(m.Groups[2].Value, @"\.metatileAttributes\s*=\s*(\w+)") is { Success: true } a && files.TryGetValue(a.Groups[1].Value, out var file))
                result[m.Groups[1].Value] = file;
        return result;
    }

    private static int Number(string text) => text.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
        ? int.Parse(text.AsSpan(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture)
        : int.Parse(text, CultureInfo.InvariantCulture);

    /// <summary><c>PalletTown</c> to <c>pallet_town</c>, <c>Route1</c> to <c>route_1</c>, <c>SSAnne_1F</c> to <c>ss_anne_1f</c>.</summary>
    public static string KeyOf(string name)
    {
        string spaced = Regex.Replace(name, @"(?<=[a-z])(?=[A-Z0-9])|(?<=[A-Z])(?=[A-Z][a-z])|(?<=[0-9])(?=[A-Z][a-z])", "_");
        return Regex.Replace(spaced.ToLowerInvariant(), "_+", "_").Trim('_');
    }
}
