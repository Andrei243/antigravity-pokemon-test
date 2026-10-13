using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Data;

/// <summary>
/// The kind of place a species lives in, as the Pokédex words it: Platinum's nine (plan 08 · P13). The file writes
/// them by these names (<c>"WatersEdge"</c>); <see cref="DexEntries.Word"/> is how the INFO page says them.
/// </summary>
public enum DexHabitat { Grassland, Forest, WatersEdge, Sea, Cave, Mountain, RoughTerrain, Urban, Rare }

/// <summary>One name's written entries and habitat word in <c>dex-entries.json</c>.</summary>
public sealed class DexEntryRecord
{
    public DexHabitat Habitat { get; set; }

    /// <summary>Two entries, each in our own words (15 to 45 of them), saying different things.</summary>
    public List<string> Entries { get; set; } = new();
}

/// <summary>
/// The Pokédex's written entries (plan 08 · P13; design "Written entries"): <c>Data/dex-entries.json</c>, one object
/// keyed by a species' name (and later a form's) exactly as <c>species.json</c> writes it, in National Pokédex order,
/// written by hand in our own words and never by the importer. Each name has two entries and a habitat word. A name
/// the file lacks has none here, and whoever shows an entry falls back to <see cref="PokemonSpecies.DexEntry"/>,
/// the sentence the importer composes from the data. The words are checked against the games' own by
/// <c>tools/DataImporter --check-entries</c>, never by a test: their text must not enter the repository.
/// </summary>
public static class DexEntries
{
    public const string FileName = "dex-entries.json";

    private static readonly Lazy<Dictionary<string, DexEntryRecord>> Records = new(() =>
        new Dictionary<string, DexEntryRecord>(GameDataFiles.Load<Dictionary<string, DexEntryRecord>>(FileName), StringComparer.Ordinal));

    /// <summary>Every name in the file with its record, in the file's own order.</summary>
    public static IReadOnlyDictionary<string, DexEntryRecord> All => Records.Value;

    /// <summary>The names in the file, in its own order (National Pokédex order).</summary>
    public static IReadOnlyList<string> Names => Records.Value.Keys.ToList();

    /// <summary>A name's record, or null where the file has none.</summary>
    public static DexEntryRecord? Of(string? name) =>
        name != null && Records.Value.TryGetValue(name, out var record) ? record : null;

    /// <summary>A name's written entries, or null where the file has none.</summary>
    public static IReadOnlyList<string>? EntriesOf(string? name) => Of(name)?.Entries;

    /// <summary>A name's habitat, or null where the file has none.</summary>
    public static DexHabitat? HabitatOf(string? name) => Of(name)?.Habitat;

    /// <summary>
    /// How many entries a species has to show: the file's, or one (the generated sentence) where it has none.
    /// </summary>
    public static int CountFor(PokemonSpecies species) => Math.Max(1, EntriesOf(species.Name)?.Count ?? 0);

    /// <summary>
    /// The entry to show: the file's <paramref name="which"/>th (round its entries, so a count of openings will do),
    /// or the generated sentence where the file has none.
    /// </summary>
    public static string EntryFor(PokemonSpecies species, int which = 0)
    {
        var entries = EntriesOf(species.Name);
        if (entries == null || entries.Count == 0) return species.DexEntry;
        int n = entries.Count;
        return entries[((which % n) + n) % n];
    }

    /// <summary>The habitat as the INFO page says it: "Water's edge", "Rough terrain".</summary>
    public static string Word(DexHabitat habitat) => habitat switch
    {
        DexHabitat.Grassland => "Grassland",
        DexHabitat.Forest => "Forest",
        DexHabitat.WatersEdge => "Water's edge",
        DexHabitat.Sea => "Sea",
        DexHabitat.Cave => "Cave",
        DexHabitat.Mountain => "Mountain",
        DexHabitat.RoughTerrain => "Rough terrain",
        DexHabitat.Urban => "Urban",
        DexHabitat.Rare => "Rare",
        _ => habitat.ToString()
    };
}
