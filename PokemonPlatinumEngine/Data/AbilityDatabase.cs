using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle.Effects;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Data;

/// <summary>An ability: its name, a short description (our own words) and what it does in battle.</summary>
public sealed class Ability
{
    public string Name { get; }
    public string Description { get; }

    /// <summary>What it does in battle; null while it isn't implemented yet (it then shows up but does nothing).</summary>
    public BattleEffect? Effect { get; }

    public bool IsImplemented => Effect != null;

    public Ability(string name, string description, BattleEffect? effect)
    {
        Name = name;
        Description = description;
        Effect = effect;
    }
}

/// <summary>
/// Abilities by name: their names and descriptions come from <c>Data/abilities.json</c>, what they do in battle from
/// <see cref="AbilityEffectTable"/>. An ability with no effect here shows up but does nothing yet. Which
/// abilities each species can have is <see cref="PokemonSpecies.Abilities"/>, read from <c>species.json</c>.
/// </summary>
public static class AbilityDatabase
{
    public const string FileName = "abilities.json";

    /// <summary>One entry of <c>abilities.json</c>.</summary>
    public sealed class AbilityRecord
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Generation { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    private static readonly Dictionary<string, Ability> Abilities = new(StringComparer.OrdinalIgnoreCase);

    static AbilityDatabase()
    {
        foreach (var record in GameDataFiles.Load<List<AbilityRecord>>(FileName))
        {
            Abilities[record.Name] = new Ability(record.Name, record.Description, AbilityEffectTable.Get(record.Name));
        }
    }

    public static void Initialize() { }

    public static Ability? Get(string? name) => name != null && Abilities.TryGetValue(name, out var a) ? a : null;

    public static IEnumerable<Ability> GetAll() => Abilities.Values;

    /// <summary>The abilities a species can have (one or two).</summary>
    public static IReadOnlyList<string> ForSpecies(PokemonSpecies species)
    {
        return species.Abilities;
    }
}
