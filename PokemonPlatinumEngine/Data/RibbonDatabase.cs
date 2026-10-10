using System;
using System.Collections.Generic;
using System.Linq;

namespace PokemonPlatinumEngine.Data;

/// <summary>
/// One of the original's eighty ribbons (<c>generated/ribbons.txt</c>, <c>sRibbonDataTable</c> in <c>src/ribbon.c</c>):
/// its constant, its name from the bank of ribbon names, and the short name and palette number of the picture the
/// original gives it, by which this game chooses a shape and a colour of its own.
/// </summary>
public sealed class RibbonRecord
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";

    /// <summary>The original picture's short name (<c>sinnoh_contest_great</c>): ribbons that share one share a shape.</summary>
    public string Art { get; set; } = "";
    public int Palette { get; set; }

    /// <summary>One of the ribbons no game gives by itself (the original's special descriptions, written by an event).</summary>
    public bool Event { get; set; }
}

/// <summary>
/// Platinum's ribbons (plan 06 · R17b), imported by <c>tools/DataImporter</c>: names and numbers only. A Pokémon keeps
/// its ribbons by their constants (<see cref="Models.Pokemon.Ribbons"/>); the rules are <see cref="Models.RibbonRules"/>.
/// </summary>
public static class RibbonDatabase
{
    public const string FileName = "ribbons.json";

    private static readonly List<RibbonRecord> Ribbons = GameDataFiles.Load<List<RibbonRecord>>(FileName);
    private static readonly Dictionary<string, int> Places = Ribbons.Select((r, i) => (r.Id, i)).ToDictionary(r => r.Id, r => r.i, StringComparer.Ordinal);

    /// <summary>Every ribbon, in the original's order (the order a summary shows them in).</summary>
    public static IReadOnlyList<RibbonRecord> All => Ribbons;

    public static RibbonRecord? Get(string? id) => id != null && Places.TryGetValue(id, out int i) ? Ribbons[i] : null;

    /// <summary>A ribbon's place in the original's order; -1 for a name that is none.</summary>
    public static int Place(string id) => Places.GetValueOrDefault(id, -1);

    public static string Name(string id) => Get(id)?.Name ?? id;
}
