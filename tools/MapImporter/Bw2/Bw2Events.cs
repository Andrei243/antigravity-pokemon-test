using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MapImporter.Bw2;

/// <summary>
/// A zone's events (<c>res/field/events/&lt;zone&gt;.json</c>; plan 22 · U2): its people and things (<c>npcs</c>), its
/// ways elsewhere (<c>warps</c>), what is read or found by facing a tile (<c>bg_events</c>) and the rectangles that
/// start a script (<c>triggers</c>). Positions are tiles of the zone's matrix; heights are kept in the units the
/// original writes each in (a person's in fx32, 65536 to a tile; a warp's, a sign's and a trigger's in sixteenths). The
/// scripts are numbers, which the reader keeps: the words they say are never read.
/// </summary>
public sealed record Bw2Events(List<Bw2Npc> Npcs, List<Bw2Warp> Warps, List<Bw2Bg> Signs, List<Bw2Trigger> Triggers)
{
    public static readonly Bw2Events None = new(new(), new(), new(), new());

    public static Bw2Events Parse(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        IEnumerable<JsonElement> List(string name) =>
            root.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Array
            // What stands on a rail (a bridge's or a cave's path the camera follows) has no tile, and is left for the session that opens it
            ? v.EnumerateArray().Where(e => e.TryGetProperty("grid", out _)) : Enumerable.Empty<JsonElement>();
        var npcs = List("npcs").Select(n =>
        {
            var grid = n.GetProperty("grid");
            var area = n.TryGetProperty("area", out var a) ? a : default;
            var data = n.TryGetProperty("params", out var p) && p.ValueKind == JsonValueKind.Array ? p.EnumerateArray().Select(e => e.ValueKind == JsonValueKind.Number ? e.GetInt32() : 0).ToArray() : Array.Empty<int>();
            return new Bw2Npc(Int(n, "id"), Str(n, "model") ?? "", Int(n, "movement"), Int(n, "type"), Name(n, "flag"), Int(n, "script"),
                Str(n, "direction") ?? "DIR_DOWN", data, area.ValueKind == JsonValueKind.Object ? Int(area, "width") : 0,
                area.ValueKind == JsonValueKind.Object ? Int(area, "height") : 0, Int(grid, "x"), Int(grid, "z"), Int(grid, "y"));
        }).ToList();
        var warps = List("warps").Select(w =>
        {
            var grid = w.GetProperty("grid");
            return new Bw2Warp(Str(w, "zone") ?? "", Int(w, "warp"), Int(grid, "x") / 16, Int(grid, "z") / 16,
                Int(grid, "y"), Math.Max(1, Int(grid, "width")), Math.Max(1, Int(grid, "height")));
        }).ToList();
        // A sign's grid is x, the row (y) and its height (z)
        var signs = List("bg_events").Select(b =>
        {
            var grid = b.GetProperty("grid");
            return new Bw2Bg(Int(b, "script"), Int(b, "condition"), Int(grid, "x"), Int(grid, "y"), Int(grid, "z"));
        }).ToList();
        var triggers = List("triggers").Select(t =>
        {
            var grid = t.GetProperty("grid");
            return new Bw2Trigger(Int(t, "script"), Name(t, "var") ?? "", Int(t, "value"), Int(grid, "x"), Int(grid, "z"),
                Math.Max(1, Int(grid, "width")), Math.Max(1, Int(grid, "height")));
        }).ToList();
        return new Bw2Events(npcs, warps, signs, triggers);
    }

    private static int Int(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetInt32() : 0;

    private static string? Str(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    /// <summary>A field that is a constant's name or a number, where 0 is none (an object's flag, a trigger's variable).</summary>
    private static string? Name(JsonElement e, string name) =>
        !e.TryGetProperty(name, out var v) ? null
        : v.ValueKind == JsonValueKind.String ? v.GetString()
        : v.ValueKind == JsonValueKind.Number && v.GetInt32() != 0 ? "0x" + v.GetInt32().ToString("x4", CultureInfo.InvariantCulture)
        : null;

    // ---------------------------------------------------------------- the original's numbers of things

    /// <summary>The first script of the global file of item balls (<c>global_7000.s</c>): an object whose script is 7000 + n holds its <c>Script_{n+1}</c>'s item.</summary>
    public const int ItemBalls = 7000;

    /// <summary>The first script of the global file of hidden items (<c>global_8000.s</c>).</summary>
    public const int HiddenItems = 8000;

    /// <summary>A trainer's own script: 3000 + the trainer's number (a pair's second, 5000 + it).</summary>
    public const int Trainers = 3000, SecondOfAPair = 5000;

    /// <summary>A sign's condition that is read, and one that is an item nobody sees.</summary>
    public const int Read = 0, Hidden = 2;

    /// <summary>
    /// What each script of a global file sets before it calls the common one, by the script's number from 0: the item ball
    /// file's <c>WorkSetConst 0x800c, item</c> and <c>0x800d, count</c>, the hidden items' <c>WorkSet 0x8000, item</c>,
    /// <c>0x8001, count</c> and <c>0x8002, flag</c>. A script that sets none of them is left out.
    /// </summary>
    public static Dictionary<int, Dictionary<int, int>> GlobalScripts(string source)
    {
        var result = new Dictionary<int, Dictionary<int, int>>();
        Dictionary<int, int>? current = null;
        foreach (string raw in source.Split('\n'))
        {
            string line = raw.Trim();
            var label = Regex.Match(line, @"^Script_(\d+):$");
            if (label.Success)
            {
                current = new Dictionary<int, int>();
                result[int.Parse(label.Groups[1].Value, CultureInfo.InvariantCulture) - 1] = current;
                continue;
            }
            var set = Regex.Match(line, @"^WorkSet(?:Const)?\s+0x([0-9a-fA-F]+)\s*,\s*(\d+)");
            if (set.Success && current != null)
                current[int.Parse(set.Groups[1].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture)] = int.Parse(set.Groups[2].Value, CultureInfo.InvariantCulture);
        }
        foreach (int empty in result.Where(kv => kv.Value.Count == 0).Select(kv => kv.Key).ToList()) result.Remove(empty);
        return result;
    }

    /// <summary>
    /// Our name for one of the original's event flags (plan 22 · U2): <c>EVENT_FLAG_0x02e9</c> is <c>FLAG_UNOVA_0x02e9</c>,
    /// and a flag the decompilation has named keeps its name after the prefix (<c>FLAG_UNOVA_&lt;NAME&gt;</c>). Unova's
    /// flags carry the region's name, so none is ever another region's.
    /// </summary>
    public static string Flag(string original) =>
        "FLAG_UNOVA_" + (original.StartsWith("EVENT_FLAG_", StringComparison.Ordinal) ? original["EVENT_FLAG_".Length..] : original.StartsWith("FLAG_", StringComparison.Ordinal) ? original["FLAG_".Length..] : original);

    /// <summary>A flag by its number: <c>FLAG_UNOVA_0x0b04</c>.</summary>
    public static string Flag(int number) => "FLAG_UNOVA_0x" + number.ToString("x4", CultureInfo.InvariantCulture);

    /// <summary>Our name for one of the original's event variables: <c>EVENT_WORK_0x40a1</c> is <c>VAR_UNOVA_0x40a1</c>.</summary>
    public static string Var(string original) =>
        "VAR_UNOVA_" + (original.StartsWith("EVENT_WORK_", StringComparison.Ordinal) ? original["EVENT_WORK_".Length..] : original.StartsWith("VAR_", StringComparison.Ordinal) ? original["VAR_".Length..] : original);

    /// <summary>
    /// A movement code as the area files name the original's movements (<see cref="PokemonPlatinumEngine.Overworld.PersonMovement"/>):
    /// Black 2's codes read from the field's actors' table, each the nearest of Platinum's words; one that walks a
    /// path of its own, or that a script drives, stands still.
    /// </summary>
    public static string Movement(int code) => code switch
    {
        2 or 12 or 13 => "look_around",
        3 or (>= 38 and <= 44) => "wander_around",
        4 => "wander_north_and_south",
        5 => "wander_west_and_east",
        6 => "look_north_and_south",
        7 => "look_west_and_east",
        8 => "look_north_and_west",
        9 => "look_north_and_east",
        10 => "look_south_and_west",
        11 => "look_south_and_east",
        _ => "none"
    };

    /// <summary>A facing as the area files keep it: 0 north, 1 south, 2 west, 3 east.</summary>
    public static int Facing(string direction) => direction switch
    {
        "DIR_UP" => 0,
        "DIR_LEFT" => 2,
        "DIR_RIGHT" => 3,
        _ => 1
    };
}

/// <summary>A person or thing of a zone: its id, model, movement, the flag that hides it, its script, facing, its own numbers and where it stands.</summary>
public sealed record Bw2Npc(int Id, string Model, int Movement, int Type, string? Flag, int Script, string Direction, int[] Params,
    int RangeX, int RangeZ, int X, int Z, int Y)
{
    /// <summary>Its model without the prefix, lower case: <c>OBJ_CODE_YOUNGSTER</c> is <c>youngster</c>, <c>OBJ_CODE_291</c> is <c>291</c>.</summary>
    public string Looks => (Model.StartsWith("OBJ_CODE_", StringComparison.Ordinal) ? Model["OBJ_CODE_".Length..] : Model).ToLowerInvariant();

    /// <summary>How high it stands, in tiles.</summary>
    public float Height => Y / 65536f;
}

/// <summary>A way elsewhere: the zone and the warp of it it leads to, its first tile, its height in sixteenths and how many tiles it spans.</summary>
public sealed record Bw2Warp(string Zone, int To, int X, int Z, int Y, int Width, int Height);

/// <summary>Something read or found by facing a tile: its script, its condition (read, a hidden item, or nothing), its tile and its height in sixteenths.</summary>
public sealed record Bw2Bg(int Script, int Condition, int X, int Z, int Y);

/// <summary>A rectangle that starts a script while a variable has a value.</summary>
public sealed record Bw2Trigger(int Script, string Var, int Value, int X, int Z, int Width, int Depth);
