using System.Text.RegularExpressions;

namespace DataImporter;

/// <summary>
/// Pokémon Showdown's move, item and species tables (<c>data/moves.ts</c>, <c>data/items.ts</c>,
/// <c>data/pokedex.ts</c>), read for what PokeAPI doesn't have: the power a move has as a Z-Move and as a Max Move,
/// what Z-Power adds to a status move, the Z-Moves, Max Moves and G-Max Moves themselves, which Mega Stone and
/// Z-Crystal belongs to whom, and the colour of each form. Only those numbers and names are read; the functions in
/// the files (how each effect works) are not. Showdown is
/// MIT-licensed: see the importer's README and THIRD-PARTY-NOTICES.md.
/// <para>
/// The files are TypeScript, one entry to a block: <c>\tid: {</c> … <c>\t},</c> with one field to a line at two
/// tabs. That is all this reads; a field whose value runs over several lines (a function, a nested table) is
/// taken by its first line and ignored unless it is one of the one-line shapes below.
/// </para>
/// </summary>
public sealed partial class Showdown
{
    public sealed record Entry(string Id, Dictionary<string, string> Fields)
    {
        public string? Text(string key) => Fields.TryGetValue(key, out var v) && Quoted().Match(v) is { Success: true } m ? m.Groups[1].Value : null;
        public int? Number(string key) => Fields.TryGetValue(key, out var v) && int.TryParse(v, out int n) ? n : null;
        public bool Is(string key, string value) => Fields.TryGetValue(key, out var v) && v == value;
        public bool Has(string key) => Fields.ContainsKey(key);
        public string Name => Text("name") ?? Id;
    }

    public IReadOnlyList<Entry> Moves { get; }
    public IReadOnlyList<Entry> Items { get; }

    /// <summary>Species and forms, each form an entry of its own named as the forms here are (<c>Charizard-Mega-X</c>).</summary>
    public IReadOnlyList<Entry> Species { get; }

    public Showdown(string folder)
    {
        string moves = Path.Combine(folder, "data", "moves.ts"), items = Path.Combine(folder, "data", "items.ts");
        string species = Path.Combine(folder, "data", "pokedex.ts");
        if (!File.Exists(moves) || !File.Exists(items) || !File.Exists(species)) throw new DirectoryNotFoundException($"No Pokémon Showdown data in {folder}");
        Moves = Read(moves);
        Items = Read(items);
        Species = Read(species);
    }

    public static List<Entry> Read(string path) => Parse(File.ReadAllLines(path));

    public static List<Entry> Parse(IEnumerable<string> lines)
    {
        var entries = new List<Entry>();
        Entry? current = null;
        foreach (string raw in lines)
        {
            string line = raw.TrimEnd('\r');
            if (current == null)
            {
                if (EntryStart().Match(line) is { Success: true } start) current = new Entry(start.Groups[1].Value, new());
                continue;
            }
            if (line == "\t},")
            {
                entries.Add(current);
                current = null;
                continue;
            }
            if (Field().Match(line) is { Success: true } field)
            {
                string value = field.Groups[2].Value;
                int comment = value.IndexOf(" //", StringComparison.Ordinal);
                if (comment >= 0) value = value[..comment];
                current.Fields.TryAdd(field.Groups[1].Value, value.Trim().TrimEnd(',').Trim());
            }
        }
        return entries;
    }

    /// <summary>The number inside a one-line table: <c>{ basePower: 140 }</c> gives 140 for "basePower".</summary>
    public static int? Inner(string? value, string key) =>
        value != null && Regex.Match(value, $@"\b{key}: (-?\d+)") is { Success: true } m ? int.Parse(m.Groups[1].Value) : null;

    /// <summary>The text inside a one-line table: <c>{ effect: 'heal' }</c> gives "heal" for "effect".</summary>
    public static string? InnerText(string? value, string key) =>
        value != null && Regex.Match(value, $@"\b{key}: ['""]([^'""]+)['""]") is { Success: true } m ? m.Groups[1].Value : null;

    /// <summary>The pairs of a one-line table of numbers: <c>{ boost: { atk: 1, def: 1 } }</c> gives atk 1, def 1 for "boost".</summary>
    public static List<(string Key, int Value)> InnerTable(string? value, string key)
    {
        var pairs = new List<(string, int)>();
        if (value == null || Regex.Match(value, $@"\b{key}: \{{([^}}]*)\}}") is not { Success: true } table) return pairs;
        foreach (Match pair in Regex.Matches(table.Groups[1].Value, @"(\w+): (-?\d+)"))
            pairs.Add((pair.Groups[1].Value, int.Parse(pair.Groups[2].Value)));
        return pairs;
    }

    /// <summary>The pairs of a one-line table of names: <c>{ "Charizard": "Charizard-Mega-X" }</c>.</summary>
    public static List<(string Key, string Value)> Pairs(string? value)
    {
        var pairs = new List<(string, string)>();
        if (value == null) return pairs;
        foreach (Match pair in Regex.Matches(value, @"""([^""]+)"": ""([^""]+)"""))
            pairs.Add((pair.Groups[1].Value, pair.Groups[2].Value));
        return pairs;
    }

    /// <summary>The names of a one-line list: <c>["Pikachu", "Raichu"]</c>.</summary>
    public static List<string> List(string? value) =>
        value == null ? new() : Regex.Matches(value, @"""([^""]+)""").Select(m => m.Groups[1].Value).ToList();

    // An id that starts with a digit is written in quotes ("10000000voltthunderbolt")
    [GeneratedRegex(@"^\t""?(\w+)""?: \{$")]
    private static partial Regex EntryStart();

    [GeneratedRegex(@"^\t\t(\w+): (.+)$")]
    private static partial Regex Field();

    [GeneratedRegex(@"^""(.*)""$")]
    private static partial Regex Quoted();
}
