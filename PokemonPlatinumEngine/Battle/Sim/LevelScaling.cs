using System;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Battle.Sim;

/// <summary>
/// A Pokémon brought to a level for a battle that is fought at one (plan 14 · B11; written once for plan 07 · O6 and
/// plan 13 · V12 to use): a copy at exactly that level, up or down, with the level's EXP and its stats worked out again,
/// whole, cured and with every move's PP. Unlike the Frontier's <see cref="Models.Frontier.FrontierRules.ForBattle"/>,
/// which only brings a Pokémon down, a Pokémon under the level is raised to it, as the Pokémon World Tournament does.
/// The copy carries no id (plan 07 · O2), so it is never taken for the one it was made from, and nothing it goes
/// through reaches the original. No drawing or input.
/// </summary>
public static class LevelScaling
{
    /// <summary>A copy of <paramref name="pokemon"/> at <paramref name="level"/>.</summary>
    public static Pokemon At(Pokemon pokemon, int level)
    {
        var copy = pokemon.Clone();
        copy.Id = Guid.Empty;
        copy.LevelTo(level);
        copy.CurrentExp = Pokemon.GetExpForLevel(copy.Level, copy.Species.GrowthRate);
        copy.RecalculateStats();
        copy.CurrentHP = copy.MaxHP;
        copy.Status = StatusCondition.None;
        copy.SleepTurns = 0;
        copy.ToxicCounter = 0;
        foreach (var move in copy.Moves) move.CurrentPP = move.MaxPP;
        copy.ResetStatStages();
        return copy;
    }

    /// <summary>Copies of a team at a level, in its order.</summary>
    public static Party TeamAt(System.Collections.Generic.IEnumerable<Pokemon> team, int level)
    {
        var party = new Party();
        foreach (var p in team) party.Add(At(p, level));
        return party;
    }
}
