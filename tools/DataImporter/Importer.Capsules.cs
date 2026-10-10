using System.Text.Json;
using System.Text.RegularExpressions;
using PokemonPlatinumEngine.Data;

namespace DataImporter;

// Platinum's Seals, Ball Capsules and ribbons (plan 06 · R17b): the Seals' names, prices and which show a letter, the
// Sunyshore Market's stock by the day, the capsules trainers' Pokémon come out of, the ribbons' names and the short
// names of their pictures, and the backdrops' names. Names and numbers only: no picture, no line anyone says.
public sealed partial class Importer
{
    /// <summary>
    /// The Seals (<c>generated/seals.txt</c>; <c>sealTypeValues</c> in <c>src/applications/capsule_menu/main.c</c>, a row
    /// for each Seal from the dummy on: the picture's file, the name's place in the bank, a number nothing reads, the
    /// particles' file, whether it shows a letter, the price, and its place among those that don't), the market's
    /// stock (<c>include/data/mart_items.h</c>) and the trainers' capsules (<c>res/trainers/ball_capsules.json</c>).
    /// </summary>
    public SealData Seals()
    {
        var constants = decomp.Constants("seals");
        var names = decomp.Messages("ball_seal_names");
        string menu = decomp.Source("applications", "capsule_menu", "main.c");
        string table = Regex.Match(menu, @"sealTypeValues\[SEAL_ID_MAX\] = \{(.*?)\n\};", RegexOptions.Singleline).Groups[1].Value;
        var rows = Regex.Matches(table, @"\{([^{}]*)\}").Select(m => m.Groups[1].Value.Split(',').Select(v => Number(v.Trim())).ToList()).ToList();
        int count = constants.IndexOf("SEAL_ID_MAX");
        if (count != 81 || rows.Count != count || rows.Any(r => r.Count != 7)) throw new InvalidDataException($"{rows.Count} rows of Seal values for {count} Seals");

        var data = new SealData();
        for (int seal = 1; seal < count; seal++)
        {
            var row = rows[seal];
            data.Seals.Add(new SealRecord { Id = constants[seal], Name = Names.Clean(names[row[1]]), Price = row[5], Letter = row[4] != 0 });
        }

        string marts = decomp.Include("data", "mart_items.h");
        var arrays = Regex.Matches(marts, @"const u16 (\w+)\[\] = \{(.*?)\};", RegexOptions.Singleline).ToDictionary(m => m.Groups[1].Value, m => m.Groups[2].Value);
        // The week as .NET counts it, Sunday first
        foreach (string day in new[] { "SUNDAY", "MONDAY", "TUESDAY", "WEDNESDAY", "THURSDAY", "FRIDAY", "SATURDAY" })
        {
            string array = Regex.Match(marts, $@"\[MART_SEAL_ID_SUNYSHORE_{day}\] = (\w+)").Groups[1].Value;
            var stock = Regex.Matches(arrays[array], @"\b\w+_SEAL(_[A-Z])?\b").Select(m => m.Value).ToList();
            if (stock.Count == 0 || stock.Any(s => !constants.Contains(s))) throw new InvalidDataException($"the market's {day} stock isn't Seals");
            data.Market.Add(stock);
        }

        var capsules = JsonDocument.Parse(decomp.Text("res", "trainers", "ball_capsules.json")).RootElement.GetProperty("capsules");
        foreach (var capsule in capsules.EnumerateArray())
            data.TrainerCapsules.Add(capsule.EnumerateArray()
                .Select(s => $"{s.GetProperty("id").GetString()},{s.GetProperty("x").GetInt32()},{s.GetProperty("y").GetInt32()}").ToList());
        return data;
    }

    /// <summary>
    /// The ribbons (<c>generated/ribbons.txt</c>; <c>sRibbonDataTable</c> in <c>src/ribbon.c</c>: each ribbon's picture,
    /// palette and name; a description under the special mask is one an event writes), named from the bank of ribbon
    /// names, whose descriptions are left where they are.
    /// </summary>
    public List<RibbonRecord> Ribbons()
    {
        var constants = decomp.Constants("ribbons");
        var names = decomp.MessagesById("ribbon_names");
        string source = decomp.Source("ribbon.c");
        var ribbons = new List<RibbonRecord>();
        foreach (string constant in constants.TakeWhile(c => c != "RIBBON_MAX"))
        {
            var entry = Regex.Match(source, $@"\[{constant}\] = \{{(.*?)\}},", RegexOptions.Singleline);
            if (!entry.Success) throw new InvalidDataException($"{constant} isn't in the ribbons' table");
            string body = entry.Groups[1].Value;
            string name = Regex.Match(body, @"\.nameID = (\w+)").Groups[1].Value;
            ribbons.Add(new RibbonRecord
            {
                Id = constant,
                Name = Names.Clean(names[name]),
                Art = Regex.Match(body, @"\.spriteID = (\w+)_NCGR").Groups[1].Value,
                Palette = int.Parse(Regex.Match(body, @"\.paletteNum = (\d+)").Groups[1].Value),
                Event = body.Contains("SPECIAL_DESCRIPTION_MASK")
            });
        }
        if (ribbons.Count != 80) throw new InvalidDataException($"{ribbons.Count} ribbons, not 80");
        return ribbons;
    }

    /// <summary>The backdrops' names, in the order of <c>generated/backdrops.txt</c>.</summary>
    public List<string> Backdrops()
    {
        var names = decomp.Messages("contest_backdrop_names").Select(Names.Clean).ToList();
        int count = decomp.Constants("backdrops").IndexOf("BACKDROP_COUNT");
        if (names.Count != count) throw new InvalidDataException($"{names.Count} backdrop names for {count} backdrops");
        return names;
    }

    private static int Number(string text) => text.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? Convert.ToInt32(text[2..], 16) : int.Parse(text);
}
