using System.Globalization;
using System.Text.RegularExpressions;
using PokemonPlatinumEngine.Data;

namespace MapImporter.Gba;

/// <summary>
/// Emerald's television outbreak (plan 38 · EX1): <c>sPokeOutbreakSpeciesList</c> in <c>src/tv.c</c>, one row each of species,
/// four moves, level and the route it breaks out on, and the share of meetings in that route's grass the show gives it
/// (<c>massOutbreak.probability</c>). Only those numbers are read: the show's words are never kept. Written into the region's
/// <c>encounters.json</c> as <see cref="WorldEncountersFile.Outbreaks"/>. FireRed has no such table.
/// </summary>
public static class GbaOutbreaks
{
    private static readonly Regex Table = new(@"sPokeOutbreakSpeciesList\[\]\s*=\s*\{(?<body>.*?)\n\};", RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex Row = new(
        @"\.species\s*=\s*(?<species>SPECIES_\w+)\s*,.*?\.moves\s*=\s*\{(?<moves>[^}]*)\}\s*,.*?\.level\s*=\s*(?<level>\d+)\s*,.*?\.location\s*=\s*MAP_NUM\((?<map>MAP_\w+)\)",
        RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex Probability = new(@"massOutbreak\.probability\s*=\s*(\d+)\s*;", RegexOptions.Compiled);

    /// <summary>The rows of the outbreak table, or none (and a problem noted) where the decompilation's <c>src/tv.c</c> isn't in the checkout.</summary>
    public static List<OutbreakRow> Read(GbaMaps maps, List<string> problems)
    {
        var rows = new List<OutbreakRow>();
        string path = Path.Combine(maps.Checkout, "src", "tv.c");
        if (maps.Game != GbaGame.Emerald) return rows;
        if (!File.Exists(path)) { problems.Add("outbreaks: src/tv.c isn't in the checkout"); return rows; }
        string text = File.ReadAllText(path);
        int share = Probability.Match(text) is { Success: true } p ? int.Parse(p.Groups[1].Value, CultureInfo.InvariantCulture) : 50;
        if (Table.Match(text) is not { Success: true } table) { problems.Add("outbreaks: no sPokeOutbreakSpeciesList in src/tv.c"); return rows; }
        foreach (Match m in Row.Matches(table.Groups["body"].Value))
        {
            string? species = GbaNames.Species(m.Groups["species"].Value);
            var map = maps.ById.GetValueOrDefault(m.Groups["map"].Value);
            if (species == null || map == null) { problems.Add($"outbreaks: {m.Groups["species"].Value} on {m.Groups["map"].Value} is not in the game's data"); continue; }
            var moves = m.Groups["moves"].Value.Split(',').Select(s => s.Trim()).Where(s => s.Length > 0 && s != "MOVE_NONE")
                .Select(s => GbaNames.Move(s) ?? s).ToList();
            rows.Add(new OutbreakRow
            {
                Area = map.Key, Species = species, Level = int.Parse(m.Groups["level"].Value, CultureInfo.InvariantCulture),
                Moves = moves.Count > 0 ? moves : null, Share = share
            });
        }
        return rows;
    }
}
