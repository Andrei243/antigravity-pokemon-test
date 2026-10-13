using System.Globalization;
using System.Text;
using PokemonPlatinumEngine.Data;

namespace DataImporter;

/// <summary>
/// <c>--check-entries</c> (plan 08 · P13): holds the Pokédex entries we wrote (<c>dex-entries.json</c>) against the
/// games' own, which PokeAPI keeps as <c>pokemon_species_flavor_text.csv</c> in the importer's git-ignored cache.
/// Any run of six words an entry shares with any English flavour text is reported, with the species and the words,
/// so that the entry can be written again. The flavour texts are read in memory only: nothing of them is written
/// anywhere, and the report names only the six words an entry of ours already holds. It can't be a unit test,
/// because the original's text must never enter the repository.
/// </summary>
public static class EntryCheck
{
    /// <summary>The number of words in a row that counts as shared.</summary>
    public const int Run = 6;

    /// <summary>Checks the file; returns the number of findings (0 when nothing is shared).</summary>
    public static int Check(string entriesPath, string pokeApiCsv)
    {
        var entries = GameDataFiles.Deserialize<Dictionary<string, DexEntryRecord>>(File.ReadAllText(entriesPath));
        var flavour = CsvTable.Load(Path.Combine(pokeApiCsv, "pokemon_species_flavor_text.csv"));
        var speciesNames = CsvTable.Load(Path.Combine(pokeApiCsv, "pokemon_species_names.csv")).Rows
            .Where(r => r.Int("local_language_id") == PokeApi.English)
            .GroupBy(r => r.Int("pokemon_species_id")).ToDictionary(g => g.Key, g => g.First()["name"]);

        // Every run of six words of every English flavour text, with the species whose text it is
        var runs = new Dictionary<string, int>(StringComparer.Ordinal);
        int texts = 0;
        foreach (var row in flavour.Rows)
        {
            if (row.Int("language_id") != PokeApi.English) continue;
            texts++;
            int species = row.Int("species_id");
            var words = Words(row["flavor_text"]);
            for (int i = 0; i + Run <= words.Count; i++) runs.TryAdd(string.Join(' ', words.Skip(i).Take(Run)), species);
        }

        int findings = 0, checkedEntries = 0;
        foreach (var (name, record) in entries)
        {
            for (int e = 0; e < record.Entries.Count; e++)
            {
                checkedEntries++;
                var words = Words(record.Entries[e]);
                var shared = new List<(string Run, int Species)>();
                for (int i = 0; i + Run <= words.Count; i++)
                {
                    string run = string.Join(' ', words.Skip(i).Take(Run));
                    if (runs.TryGetValue(run, out int species)) shared.Add((run, species));
                }
                foreach (var (run, species) in shared)
                {
                    findings++;
                    Console.WriteLine($"  {name}, entry {e + 1}: \"{run}\" (in a text of {speciesNames.GetValueOrDefault(species, $"species {species}")})");
                }
            }
        }
        Console.WriteLine(findings == 0
            ? $"{checkedEntries} entries of {entries.Count} names share no run of {Run} words with {texts} English flavour texts."
            : $"{findings} runs of {Run} words shared with the flavour texts: write those entries again.");
        return findings;
    }

    /// <summary>
    /// A text's words as the check compares them: split at anything but a letter or a digit's neighbours (spaces,
    /// line and page breaks, dashes), lower case, letters and digits only, accents taken off ("POKéMON" is "pokemon").
    /// </summary>
    public static List<string> Words(string text)
    {
        var words = new List<string>();
        var word = new StringBuilder();
        foreach (char c in text.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue;
            if (char.IsLetterOrDigit(c)) word.Append(char.ToLowerInvariant(c));
            else if (c is '\'' or '’' or '­') continue;   // within a word: "it's" is "its"
            else if (word.Length > 0) { words.Add(word.ToString()); word.Clear(); }
        }
        if (word.Length > 0) words.Add(word.ToString());
        return words;
    }
}
