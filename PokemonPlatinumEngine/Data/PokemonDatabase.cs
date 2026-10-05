using System;
using System.Collections.Generic;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Data;

/// <summary>Every species, read from <c>Data/species.json</c> the first time it is used.</summary>
public static class PokemonDatabase
{
    public const string FileName = "species.json";

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

    public static IEnumerable<PokemonSpecies> GetAll() => SpeciesByName.Values;

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
