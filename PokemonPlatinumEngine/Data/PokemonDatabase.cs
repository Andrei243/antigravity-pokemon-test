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
}
