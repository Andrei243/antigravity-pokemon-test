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

    /// <summary>Random numbers for every roll in the battle (fixed seeds make tests repeatable).</summary>
    public Random? Random { get; init; }

    /// <summary>The trainer's first Pokémon when it isn't the first in their party (the old constructor's foe argument).</summary>
    internal Pokemon? FirstTrainerPokemon { get; init; }
}
