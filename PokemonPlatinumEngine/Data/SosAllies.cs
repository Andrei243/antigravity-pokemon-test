using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Data;

/// <summary>What <c>Data/sos-allies.json</c> holds (plan 14 · B5): call rates where the rule's own isn't wanted, and the pairs of species of other lines.</summary>
public sealed class SosAlliesFile
{
    /// <summary>A species' call rate where it isn't the rule's (<see cref="SosAllies.RateOf"/>): 0 never calls.</summary>
    public Dictionary<string, int> Rates { get; set; } = new();

    /// <summary>Species of another line a species may call besides its own (Sun and Moon's Cubone and Kangaskhan): one way only.</summary>
    public Dictionary<string, List<string>> Pairs { get; set; } = new();
}

/// <summary>
/// Who answers an SOS battle's calls for help, and how readily a species calls (plan 14 · B5; <c>docs/mechanics/rulings.md</c>,
/// "SOS calls"; plan 14, decision 6). GPU-free. A species' <b>call rate</b> is 0, 3, 6, 9 or 15, Sun and Moon's scale: the
/// file's where it names one, none for a legendary or a mythical Pokémon, and otherwise a rule of ours by how easily the
/// species is caught (a catch rate of 190 or more 15, of 120 or more 9, of 45 or more 6, below that 3). Its <b>allies</b>
/// are its own line (every species whose Egg is the same as its own) that its ally's level can have reached by level alone,
/// with the file's pairs beside them.
/// </summary>
public static class SosAllies
{
    public const string FileName = "sos-allies.json";

    /// <summary>How much likelier the caller's own species is to answer than each of the others.</summary>
    public const int OwnWeight = 3;

    private static SosAlliesFile? file;

    /// <summary>The file as read (an empty one where there is none).</summary>
    public static SosAlliesFile File => file ??= System.IO.File.Exists(GameDataFiles.PathOf(FileName))
        ? GameDataFiles.Load<SosAlliesFile>(FileName) : new SosAlliesFile();

    /// <summary>A species' call rate: the file's, none for a legendary or a mythical one, else the rule's by its catch rate.</summary>
    public static int RateOf(PokemonSpecies species)
    {
        if (File.Rates.TryGetValue(species.Name, out int rate)) return Math.Max(0, rate);
        if (species.Legendary || species.Mythical) return 0;
        return species.CatchRate switch { >= 190 => 15, >= 120 => 9, >= 45 => 6, _ => 3 };
    }

    /// <summary>The first of a species' line: the species its Egg is, or itself.</summary>
    public static string LineOf(PokemonSpecies species) => species.Offspring ?? species.Name;

    /// <summary>Every species of a line, the first first and the rest in Pokédex order.</summary>
    public static List<PokemonSpecies> Line(PokemonSpecies species)
    {
        string first = LineOf(species);
        return PokemonDatabase.Everyone().Where(s => LineOf(s) == first).OrderBy(s => s.Name == first ? 0 : 1).ThenBy(s => s.DexNumber).ToList();
    }

    /// <summary>
    /// The lowest level each species of a line can be met at by evolving by level alone from its first (null for one that
    /// only something else brings: a stone, friendship, a trade). The first is level 1.
    /// </summary>
    public static Dictionary<string, int?> LevelsOf(PokemonSpecies species)
    {
        var line = Line(species);
        var levels = line.ToDictionary(s => s.Name, s => s.Name == LineOf(species) ? (int?)1 : null);
        // Each round reaches a stage further; a line is never more than a few long
        for (int round = 0; round < line.Count; round++)
            foreach (var s in line)
            {
                if (levels[s.Name] is not { } from) continue;
                foreach (var evolution in s.Evolutions ?? new())
                {
                    if (!levels.ContainsKey(evolution.TargetSpecies)) continue;
                    int? to = evolution.Method == EvolutionMethod.Level && evolution.Level > 0 ? Math.Max(from, evolution.Level) : null;
                    if (to is { } reached && (levels[evolution.TargetSpecies] is not { } known || reached < known)) levels[evolution.TargetSpecies] = reached;
                }
            }
        return levels;
    }

    /// <summary>
    /// The species that may answer a call of <paramref name="caller"/>'s for an ally of <paramref name="level"/>, each with
    /// its weight: the caller's own (<see cref="OwnWeight"/>), every other of its line that level can have reached by level
    /// (1 each), and the file's pairs for it (1 each). A legendary, a mythical or an Ultra Beast of the file never answers.
    /// </summary>
    public static List<(PokemonSpecies Species, int Weight)> Candidates(PokemonSpecies caller, int level)
    {
        var found = new List<(PokemonSpecies Species, int Weight)> { (caller, OwnWeight) };
        foreach (var (name, reached) in LevelsOf(caller))
            if (name != caller.Name && reached is { } at && at <= level && PokemonDatabase.Get(name) is { } s && RateOf(s) > 0) found.Add((s, 1));
        if (File.Pairs.TryGetValue(caller.Name, out var pairs))
            foreach (string name in pairs)
                if (PokemonDatabase.Get(name) is { } s && found.All(f => f.Species.Name != s.Name)) found.Add((s, 1));
        return found;
    }

    /// <summary>Every species that may ever answer a call of <paramref name="caller"/>'s, at any level: what the screen makes ready before the battle.</summary>
    public static IEnumerable<PokemonSpecies> AnyAlly(PokemonSpecies caller) => Candidates(caller, 100).Select(c => c.Species);
}
