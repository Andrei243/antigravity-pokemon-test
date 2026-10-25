using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Data;

/// <summary>
/// Every species, read from <c>Data/species.json</c> the first time it is used. The file holds the games' National
/// Pokédex (1 to 1,025) and, after it, the species of our own that evolve past level 100 (plan 03 · D14), numbered from
/// <see cref="OursFrom"/>. <see cref="Get"/> and <see cref="GetByDex"/> find any of them; <see cref="GetAll"/> is the
/// games' species alone, and whatever means ours too asks <see cref="Everyone"/> (or <see cref="Ours"/>) on purpose.
/// </summary>
public static class PokemonDatabase
{
    public const string FileName = "species.json";

    /// <summary>The first number of a species of our own (plan 03 · D14): 2000 + the number of the species it evolves from.</summary>
    public const int OursFrom = 2000;

    private static readonly List<PokemonSpecies> Games = new(), OwnSpecies = new();

    private static readonly Dictionary<string, PokemonSpecies> SpeciesByName = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<int, PokemonSpecies> SpeciesByDex = new();
    private static readonly Dictionary<string, PokemonSpecies> SpeciesByForm = new(StringComparer.OrdinalIgnoreCase);

    public static void Initialize() { }

    static PokemonDatabase()
    {
        foreach (var species in GameDataFiles.Load<List<PokemonSpecies>>(FileName))
        {
            Register(species);
        }
    }

    private static void Register(PokemonSpecies species)
    {
        SpeciesByName[species.Name] = species;
        SpeciesByDex[species.DexNumber] = species;
        (species.IsOurs ? OwnSpecies : Games).Add(species);
        foreach (var form in species.Forms ?? new()) SpeciesByForm[form.Name] = species;
    }

    public static PokemonSpecies? Get(string name)
    {
        SpeciesByName.TryGetValue(name, out var species);
        return species;
    }

    public static PokemonSpecies? GetByDex(int dexNumber)
    {
        SpeciesByDex.TryGetValue(dexNumber, out var species);
        return species;
    }

    /// <summary>
    /// The games' species, the National Pokédex's 1,025 in its order, and none of ours: what the battle corpus, the draws
    /// of the Frontier's, the Pokéathlon's and Pal Park's, the masters and every count of the games' own go by.
    /// </summary>
    public static IEnumerable<PokemonSpecies> GetAll() => Games;

    /// <summary>The species of our own (plan 03 · D14), in their numbers' order.</summary>
    public static IReadOnlyList<PokemonSpecies> Ours() => OwnSpecies;

    /// <summary>Every species of the data, the games' and then ours: for whatever shows, names, teaches or checks any species there is.</summary>
    public static IEnumerable<PokemonSpecies> Everyone() => Games.Concat(OwnSpecies);

    /// <summary>The species a form belongs to, by the form's name (<c>Rotom-Heat</c>); null if no species has it.</summary>
    public static PokemonSpecies? SpeciesOfForm(string formName)
    {
        SpeciesByForm.TryGetValue(formName, out var species);
        return species;
    }

    /// <summary>
    /// Gives every species and form the values these rules say (<see cref="PokemonSpecies.Modern"/>). Called
    /// through <see cref="Ruleset.Use"/> as a game begins or is loaded.
    /// </summary>
    internal static void UseRules(Ruleset rules)
    {
        foreach (var species in SpeciesByName.Values) species.UseRules(rules);
    }
}
