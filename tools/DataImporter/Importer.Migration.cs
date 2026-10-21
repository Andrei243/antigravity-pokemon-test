using System.Globalization;
using System.Text.RegularExpressions;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace DataImporter;

// What a foreign Pokémon's conversion needs (plan 08 · P20): the GBA's species and item numbers, the characters of both
// consoles' strings, and the three lists Platinum's migration refuses by. Numbers and characters only: no sprite, no
// icon, no sound and no line anyone says. Read from the pinned decompilation's C, never from another tool's code.
public sealed partial class Importer
{
    private static readonly int[] MigrationLanguages = { 1, 2, 3, 4, 5, 7 };

    /// <summary>
    /// The migration tables: <c>sGBAToDSSpecies</c> and <c>sSpeciesWithNewAbilities</c> (<c>src/main_menu/gba_pokemon.c</c>),
    /// <c>sGBAHMMoves</c> and <c>sInvalidGBAItems</c> (<c>gba_migrator.c</c>), each item's <c>gbaID</c> (<c>Item_FromGBAID</c>),
    /// and the character tables (<c>gba_convert_string.c</c>, <c>include/constants/charcode.h</c>, <c>tools/msgenc/charmap.txt</c>).
    /// </summary>
    public MigrationData Migration(IReadOnlyList<ItemData> items, IReadOnlyList<MoveData> moves)
    {
        var data = new MigrationData();

        // The species: GBA numbers are defines, DS numbers are the lines of generated/species.txt
        var gbaSpecies = Regex.Matches(decomp.Text("include", "constants", "gba", "species.h"), @"#define\s+(GBA_SPECIES_\w+)\s+(\d+)")
            .ToDictionary(m => m.Groups[1].Value, m => int.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture));
        int GbaNumber(string token) => gbaSpecies.TryGetValue(token, out int n) ? n : Number(token);
        int DsNumber(string token) => token.StartsWith("SPECIES_", StringComparison.Ordinal) ? Index(speciesConstants, token) : Number(token);

        string pokemon = decomp.Source("main_menu", "gba_pokemon.c");
        foreach (Match row in Regex.Matches(Block(pokemon, @"sGBAToDSSpecies\[\] = \{"), @"\{\s*(\w+)\s*,\s*(\w+)\s*\}"))
            data.GbaSpecies[GbaNumber(row.Groups[1].Value)] = DsNumber(row.Groups[2].Value);
        if (data.GbaSpecies.Count != 144 || data.GbaSpecies[277] != 252 || gbaSpecies["GBA_SPECIES_CHIMECHO"] != 411 || gbaSpecies["GBA_SPECIES_EGG"] != 412)
            throw new InvalidDataException($"the GBA species table has {data.GbaSpecies.Count} rows and Treecko is {data.GbaSpecies.GetValueOrDefault(277)}");

        foreach (Match species in Regex.Matches(Block(pokemon, @"sSpeciesWithNewAbilities\[\] = \{"), @"SPECIES_\w+"))
            data.NewAbilities.Add(Index(speciesConstants, species.Value));
        if (data.NewAbilities.Count != 94) throw new InvalidDataException($"{data.NewAbilities.Count} species with new abilities");

        // The moves and items that refuse a Pokémon
        string migrator = decomp.Source("main_menu", "gba_migrator.c");
        var gbaItems = Enumeration(decomp.Text("include", "constants", "gba", "items.h"), "GbaItem");
        foreach (Match move in Regex.Matches(Block(migrator, @"sGBAHMMoves\[\] = \{"), @"MOVE_\w+"))
        {
            string name = string.Join(' ', move.Value["MOVE_".Length..].Split('_').Select(w => char.ToUpperInvariant(w[0]) + w[1..].ToLowerInvariant()));
            if (!moves.Any(m => m.Name == name)) throw new InvalidDataException($"{move.Value} is no move called {name}");
            data.HmMoves.Add(name);
        }
        foreach (Match item in Regex.Matches(Block(migrator, @"sInvalidGBAItems\[\] = \{"), @"GBA_ITEM_\w+"))
            data.InvalidItems.Add(gbaItems[item.Value]);
        if (data.HmMoves.Count != 8 || data.InvalidItems.Count != 145) throw new InvalidDataException($"{data.HmMoves.Count} HM moves and {data.InvalidItems.Count} invalid items");

        // Item_FromGBAID: the first item whose gbaID is the number
        var byId = items.ToDictionary(i => i.Id);
        for (int index = 1; index < itemConstants.Count; index++)
        {
            if (decomp.Item(itemConstants[index]) is not { } json || !json.TryGetProperty("gbaID", out var gba)) continue;
            string id = gba.GetString() ?? "GBA_ITEM_NONE";
            if (id == "GBA_ITEM_NONE" || !byId.TryGetValue(index, out var item)) continue;
            data.GbaItems.TryAdd(gbaItems[id], item.Name);
        }

        // The characters: the GBA's code to the DS's enumerated character, that to the DS's code, that to its character
        var charCodes = Enumeration(decomp.Text("include", "constants", "charcode.h"), "CharCode");
        var dsChars = DsCharacters(decomp.Text("tools", "msgenc", "charmap.txt"));
        data.DsChars = dsChars.Where(c => c.Value.Length > 0 && !c.Value.StartsWith('{') && !c.Value.StartsWith("\\x", StringComparison.Ordinal))
            .ToDictionary(c => c.Key, c => c.Value);
        string Character(string name) => charCodes.TryGetValue(name, out int code) && data.DsChars.TryGetValue(code, out var text) ? text : "";

        var table = Regex.Matches(Block(decomp.Source("main_menu", "gba_convert_string.c"), @"sGBACharToDSChars\[\]\[2\] = \{"), @"\{\s*(CHAR_\w+)\s*,\s*(CHAR_\w+)\s*\}")
            .Select(m => (Japanese: m.Groups[1].Value, International: m.Groups[2].Value)).ToList();
        if (table.Count < 200) throw new InvalidDataException($"{table.Count} rows in the GBA's character table");
        foreach (int language in MigrationLanguages)
        {
            data.GbaChars[language] = table.Select(row => Character(Resolve(language == 1 ? row.Japanese : row.International, language))).ToList();
        }
        return data;
    }

    // Where the original asks a language for a space or a quotation mark (GetSpaceChar and the two quote functions)
    private static string Resolve(string name, int language) => name switch
    {
        "CHAR_WIDE_SPACE" => language == 1 ? "CHAR_WIDE_SPACE" : "CHAR_SPACE",
        "CHAR_JP_DOUBLE_QUOTE_OPEN" => language switch { 2 or 4 or 7 => "CHAR_DOUBLE_QUOTE_OPEN", 3 => "CHAR_DOUBLE_PAREN_OPEN", 5 => "CHAR_DOUBLE_QUOTE_CLOSE_INVERTED", _ => name },
        "CHAR_JP_DOUBLE_QUOTE_CLOSE" => language switch { 2 or 4 or 7 => "CHAR_DOUBLE_QUOTE_CLOSE", 3 => "CHAR_DOUBLE_PAREN_CLOSE", 5 => "CHAR_DOUBLE_QUOTE_OPEN", _ => name },
        _ => name
    };

    /// <summary>The body of the braces a declaration opens (<paramref name="opening"/>, a pattern ending in the brace).</summary>
    private static string Block(string source, string opening)
    {
        var start = Regex.Match(source, opening);
        if (!start.Success) throw new InvalidDataException($"no {opening} in the source");
        int end = source.IndexOf("};", start.Index + start.Length, StringComparison.Ordinal);
        return source[(start.Index + start.Length)..end];
    }

    /// <summary>An enum's names and values: each is the one before it plus one unless it says its own.</summary>
    private static Dictionary<string, int> Enumeration(string header, string name)
    {
        string body = Block(header, @"enum " + name + @"\s*\{");
        var values = new Dictionary<string, int>();
        int next = 0;
        foreach (string raw in Regex.Replace(body, @"//[^\n]*", "").Split(','))
        {
            string entry = raw.Trim();
            if (entry.Length == 0) continue;
            var parts = entry.Split('=', 2);
            string key = parts[0].Trim();
            if (parts.Length == 2)
            {
                string value = parts[1].Trim();
                next = values.TryGetValue(value, out int named) ? named : Number(value);
            }
            values[key] = next++;
        }
        return values;
    }

    /// <summary>The DS's code to its character (<c>0001=　</c>), commands and escapes as they are written.</summary>
    private static Dictionary<int, string> DsCharacters(string charmap)
    {
        var chars = new Dictionary<int, string>();
        foreach (string line in charmap.ReplaceLineEndings("\n").Split('\n'))
        {
            string trimmed = line.TrimStart(' ', '\t');
            if (trimmed.StartsWith("//", StringComparison.Ordinal)) continue;
            var match = Regex.Match(trimmed, @"^([0-9A-Fa-f]{4})=(.*)$");
            if (match.Success) chars[int.Parse(match.Groups[1].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture)] = match.Groups[2].Value;
        }
        return chars;
    }

    private static int Index(List<string> constants, string name)
    {
        int index = constants.IndexOf(name);
        if (index < 0) throw new InvalidDataException($"{name} is no constant the decompilation lists");
        return index;
    }
}
