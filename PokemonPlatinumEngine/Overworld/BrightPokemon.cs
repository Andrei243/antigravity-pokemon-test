using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Overworld;

/// <summary>What a count of one species brings to those of it seen in the grass (plan 38 · EX3): the chance in a hundred one is bright, its perfect IVs, the shiny rolls more.</summary>
public readonly record struct BrightBand(int Chance, int PerfectIvs, int ShinyRolls);

/// <summary>
/// Bright Pokémon (plan 38 · EX3; <c>docs/mechanics/rulings.md</c>, "The field beyond the tables"; after Sword and Shield's
/// brilliant Pokémon): a wild Pokémon in sight (the options' WILD POKéMON row at VISIBLE) may be bright, by bands of how many of
/// its species the player has beaten or caught (<see cref="Research"/>'s counts). Whether one is bright, which of its IVs are
/// perfect and the Egg move it knows are read from its personality and its species, never drawn; the band's shiny rolls are
/// the one thing drawn, and only from <see cref="FirstRollsAt"/> met. GPU-free; no input or drawing.
/// </summary>
public static class BrightPokemon
{
    /// <summary>The count from which a meeting in sight is given shiny rolls more.</summary>
    public const int FirstRollsAt = 50;

    /// <summary>The band of a count of one species beaten or caught.</summary>
    public static BrightBand BandOf(int count) => count switch
    {
        >= 500 => new(6, 3, 5),
        >= 300 => new(6, 3, 4),
        >= 200 => new(6, 3, 3),
        >= 100 => new(5, 3, 2),
        >= 50 => new(4, 3, 1),
        >= 20 => new(3, 2, 0),
        >= 1 => new(2, 1, 0),
        _ => new(0, 0, 0)
    };

    /// <summary>How many of a species the player has beaten or caught, as the Pokédex's research counts them.</summary>
    public static int CountOf(Pokedex pokedex, PokemonSpecies species)
    {
        var record = pokedex.Research.RecordOf(species.DexNumber);
        return record.CountOf("caught") + record.CountOf("defeated");
    }

    // The number a Pokémon's brightness is read from: its personality mixed, so it is not the shiny check's own bits
    private static uint Reading(uint personality, uint salt) => SplitMix.Mix(personality, 0xB417B417u, salt);

    /// <summary>Whether a Pokémon of this personality is bright under a band: its reading under the band's chance in a hundred.</summary>
    public static bool IsBright(uint personality, BrightBand band) => band.Chance > 0 && Reading(personality, 0) % 100u < (uint)band.Chance;

    /// <summary>
    /// The stats a bright one has perfect, in the order its personality picks them: <paramref name="count"/> of the six,
    /// never one twice.
    /// </summary>
    public static IReadOnlyList<StatType> PerfectStats(uint personality, int count)
    {
        var stats = new List<StatType> { StatType.HP, StatType.Attack, StatType.Defense, StatType.SpAttack, StatType.SpDefense, StatType.Speed };
        var chosen = new List<StatType>();
        for (int n = 0; n < count && stats.Count > 0; n++)
        {
            var stat = stats[(int)(Reading(personality, 1u + (uint)n) % (uint)stats.Count)];
            stats.Remove(stat);
            chosen.Add(stat);
        }
        return chosen;
    }

    /// <summary>The first of a species' Egg moves the Pokémon doesn't know and the data has; null where none is.</summary>
    public static string? EggMoveFor(Pokemon pokemon) =>
        pokemon.Species.EggMoves?.FirstOrDefault(m => MoveDatabase.Has(m) && pokemon.Moves.All(known => known.Data.Name != m));

    /// <summary>
    /// Makes a Pokémon bright where its band and personality say it is: its perfect IVs, and the first Egg move it doesn't know
    /// in place of its last move (added where it knows fewer than four). Draws nothing. Returns whether it is bright.
    /// </summary>
    public static bool Apply(Pokemon pokemon, BrightBand band)
    {
        if (pokemon.IsEgg || !IsBright(pokemon.Personality, band)) return false;
        pokemon.Bright = true;
        foreach (var stat in PerfectStats(pokemon.Personality, band.PerfectIvs)) SetIv(pokemon, stat, 31);
        pokemon.ReckonStats();
        if (EggMoveFor(pokemon) is { } move)
        {
            if (pokemon.Moves.Count >= 4) pokemon.Moves.RemoveAt(pokemon.Moves.Count - 1);
            pokemon.Moves.Add(new Move(MoveDatabase.Get(move)));
        }
        return true;
    }

    internal static void SetIv(Pokemon p, StatType stat, int value)
    {
        switch (stat)
        {
            case StatType.HP: p.IvHP = value; break;
            case StatType.Attack: p.IvAttack = value; break;
            case StatType.Defense: p.IvDefense = value; break;
            case StatType.SpAttack: p.IvSpAttack = value; break;
            case StatType.SpDefense: p.IvSpDefense = value; break;
            default: p.IvSpeed = value; break;
        }
    }
}
