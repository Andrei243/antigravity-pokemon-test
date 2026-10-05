using System;
using System.Collections.Generic;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Battle;

public enum BattleFormat
{
    /// <summary>One Pokémon per side.</summary>
    Single,
    /// <summary>Two Pokémon per side (twins, Galactic pairs, two trainers who spot the player together).</summary>
    Double
}

/// <summary>Everything a battle starts from.</summary>
public sealed class BattleSetup
{
    public required Party PlayerParty { get; init; }
    public required Inventory Inventory { get; init; }
    public required Pokedex Pokedex { get; init; }

    /// <summary>Where caught Pokémon go when the party is full.</summary>
    public List<Pokemon>? PcStorage { get; init; }

    public BattleFormat Format { get; init; } = BattleFormat.Single;

    /// <summary>The wild Pokémon met (one, or two in a wild double battle). Empty in trainer battles.</summary>
    public List<Pokemon> WildPokemon { get; init; } = new();

    /// <summary>
    /// The opposing trainers: one, or two in a double battle where each sends one Pokémon at a time (two trainers
    /// at once). A single trainer in a double battle sends two at a time.
    /// </summary>
    public List<Trainer> Trainers { get; init; } = new();

    /// <summary>
    /// Random numbers for every roll in the battle (fixed seeds make tests repeatable). A
    /// <see cref="Sim.BattleRandom"/> also lets a test fix the rolls of one kind.
    /// </summary>
    public Random? Random { get; init; }

    /// <summary>The rules the battle is fought by; left out, those of the game in progress.</summary>
    public Data.Ruleset? Rules { get; init; }

    /// <summary>
    /// What the rules ask of the world outside the battle (water or a cave underfoot, night, which species the
    /// player has caught); left out, a battle on land by day, with the Pokédex answering for what was caught.
    /// </summary>
    public Sim.BattleConditions? Conditions { get; init; }

    /// <summary>The trainer's first Pokémon when it isn't the first in their party (the old constructor's foe argument).</summary>
    internal Pokemon? FirstTrainerPokemon { get; init; }
}
