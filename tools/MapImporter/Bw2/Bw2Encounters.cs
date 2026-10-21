using System.Text.Json;
using MapImporter.Gba;
using PokemonPlatinumEngine.Data;

namespace MapImporter.Bw2;

/// <summary>
/// A zone's wild Pokémon (<c>res/field/encounters/&lt;name&gt;.json</c>; plan 22 · U2): a set of tables for the whole
/// year (<c>all_year</c>) or one for each season (<c>spring</c> to <c>winter</c>), each with its rates and the grass's,
/// the dark grass's, the shaking grass's, the water's, the rippling water's and the two rods' slots. A table that
/// differs between the versions gives both (<c>black2</c>, <c>white2</c>), and the reader takes Black 2's. A slot is a
/// species, its lowest and highest level and a form, which the reader leaves out (the rulings).
/// </summary>
public sealed class Bw2Encounters
{
    /// <summary>The seasons in the original's order; <c>all_year</c> is read as spring with no others.</summary>
    public static readonly string[] Seasons = { "spring", "summer", "autumn", "winter" };

    /// <summary>
    /// How the encounter rate climbs for each setting of a zone's rate (<c>ENCOUNT_RATE_CONFIGS</c> in the original's
    /// <c>field_encount.c</c>: a grass's, a dark grass's and the water's rate are a number from 1 into these twelve rows).
    /// The game's rate is the row's largest number, which stands in for the climb until plan 22 · U4 ports it (the rulings).
    /// </summary>
    public static readonly int[][] RateConfigs =
    {
        new[] { 3, 1, 13, 2 }, new[] { 3, 1, 19, 2 }, new[] { 2, 3, 13, 3 }, new[] { 2, 2, 19, 2 }, new[] { 5, 1, 31, 2 }, new[] { 5, 1, 31, 2 },
        new[] { 2, 18, 7, 8 }, new[] { 2, 9, 7, 4 }, new[] { 2, 3, 13, 3 }, new[] { 1, 12, 7, 3 }, new[] { 1, 9, 7, 3 }, new[] { 2, 3, 13, 3 }
    };

    /// <summary>The game's rate for a setting of the zone's rate; null for none (0) or one past the table.</summary>
    public static int? Rate(int setting) => setting >= 1 && setting <= RateConfigs.Length ? RateConfigs[setting - 1].Max() : null;

    /// <summary>The fishing rods' slots are met 60, 30, 5, 4 and 1 times in a hundred, as the water's are.</summary>
    public static readonly int[] RodWeights = { 60, 30, 5, 4, 1 };

    /// <summary>One season's tables.</summary>
    public sealed class Season
    {
        public string Name { get; init; } = "";
        public Dictionary<string, int> Rates { get; } = new(StringComparer.Ordinal);
        public Dictionary<string, List<(string Species, int Min, int Max)>> Tables { get; } = new(StringComparer.Ordinal);
    }

    public List<Season> BySeason { get; } = new();

    /// <summary>True for a zone whose Pokémon change with the season.</summary>
    public bool Seasonal => BySeason.Count > 1;

    public static Bw2Encounters Parse(string json)
    {
        var result = new Bw2Encounters();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        foreach (string name in root.TryGetProperty("all_year", out _) ? new[] { "all_year" } : Seasons)
        {
            if (!root.TryGetProperty(name, out var s) || s.ValueKind != JsonValueKind.Object) continue;
            var season = new Season { Name = name == "all_year" ? "spring" : name };
            if (s.TryGetProperty("rates", out var rates))
                foreach (var r in rates.EnumerateObject())
                    if (r.Value.ValueKind == JsonValueKind.Number) season.Rates[r.Name] = r.Value.GetInt32();
            foreach (var table in s.EnumerateObject())
            {
                if (table.Name == "rates") continue;
                var slots = table.Value;
                // A table the versions differ in: Black 2's
                if (slots.ValueKind == JsonValueKind.Object)
                    slots = slots.TryGetProperty("black2", out var b) ? b : default;
                if (slots.ValueKind != JsonValueKind.Array) continue;
                var list = new List<(string, int, int)>();
                foreach (var slot in slots.EnumerateArray())
                {
                    if (slot.ValueKind != JsonValueKind.Array || slot.GetArrayLength() < 3) continue;
                    list.Add((slot[0].GetString() ?? "", slot[1].GetInt32(), slot[2].GetInt32()));
                }
                season.Tables[table.Name] = list;
            }
            result.BySeason.Add(season);
        }
        return result;
    }

    /// <summary>A table as an area's slots, by the data's names; null where it has none or no rate. <paramref name="range"/> keeps the top level.</summary>
    public static List<AreaEncounter>? Slots(Season season, string table, bool range, int[]? weights, string where, List<string> problems)
    {
        if (!season.Tables.TryGetValue(table, out var slots) || slots.Count == 0) return null;
        return slots.Select((s, i) =>
        {
            string? species = GbaNames.Species(s.Species);
            if (species == null)
            {
                string problem = $"{where}: {s.Species} is not in the game's data";
                if (!problems.Contains(problem)) problems.Add(problem);
                species = s.Species;
            }
            return new AreaEncounter
            {
                Species = species, Level = s.Min,
                MaxLevel = range || s.Max != s.Min ? Math.Max(s.Min, s.Max) : null,
                Weight = weights != null && i < weights.Length ? weights[i] : null
            };
        }).ToList();
    }
}
