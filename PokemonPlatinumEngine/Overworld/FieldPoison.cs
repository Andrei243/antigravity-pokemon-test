using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Overworld;

/// <summary>What a step did to the poisoned Pokémon of the team.</summary>
public enum PoisonStep
{
    /// <summary>Nothing: not the fourth step, or nobody is poisoned.</summary>
    None,
    /// <summary>Each poisoned Pokémon lost a hit point; the screen flashes.</summary>
    Hurt,
    /// <summary>One or more are down to their last hit point, and come through (<c>common.PoisonSurvived</c> says so and cures them).</summary>
    Survived
}

/// <summary>
/// Poison in the field (plan 06 · R13; <c>Field_UpdatePoison</c> and <c>Pokemon_DoPoisonDamage</c>), with no drawing
/// or input: every fourth step each Pokémon of the team that can fight and is poisoned (or badly) loses a hit point,
/// never its last. One left with one hit point comes through it: the poison fades and its friendship falls a little
/// (<c>Pokemon_TrySurvivePoison</c>, <c>FRIENDSHIP_EVENT_POISON_SURVIVE</c>). By the modern rules poison does nothing
/// outside battle (<see cref="Ruleset.PoisonInTheField"/>).
/// </summary>
public static class FieldPoison
{
    /// <summary>A step taken: the count goes round four, and on its fourth step the poison bites.</summary>
    public static PoisonStep Step(SpecialEncounters state, Party party, Ruleset rules)
    {
        if (!rules.PoisonInTheField) return PoisonStep.None;
        state.PoisonSteps = (state.PoisonSteps + 1) % 4;
        if (state.PoisonSteps != 0) return PoisonStep.None;
        return Bite(party);
    }

    /// <summary><c>Pokemon_DoPoisonDamage</c>: a hit point from each poisoned Pokémon that can fight, none from one with only one.</summary>
    public static PoisonStep Bite(Party party)
    {
        int poisoned = 0, survived = 0;
        foreach (var p in party.Members)
        {
            if (p.IsFainted || p.Status is not (StatusCondition.Poison or StatusCondition.Toxic)) continue;
            if (p.CurrentHP > 1) p.CurrentHP--;
            if (p.CurrentHP == 1)
            {
                survived++;
                FriendshipRules.Apply(p, FriendshipEvent.PoisonSurvive);
            }
            poisoned++;
        }
        return survived > 0 ? PoisonStep.Survived : poisoned > 0 ? PoisonStep.Hurt : PoisonStep.None;
    }

    /// <summary><c>Pokemon_TrySurvivePoison</c>: a poisoned Pokémon at one hit point is cured; true if it was.</summary>
    public static bool TrySurvive(Pokemon p)
    {
        if (p.Status is not (StatusCondition.Poison or StatusCondition.Toxic) || p.CurrentHP != 1) return false;
        p.Status = StatusCondition.None;
        return true;
    }
}

/// <summary>
/// Feebas's tiles (plan 06 · R13; <c>feebas_fishing.c</c>), with no drawing or input: of every tile of Mt. Coronet's
/// lake, taken in the original's order and cut into four equal groups, the day's number picks one tile of each group
/// by its four bytes, the highest first. A rod cast onto one of those four hooks Feebas (levels 10 to 20, any rod)
/// one time in two, whatever its own table holds; so the tiles change every day.
/// </summary>
public static class Feebas
{
    public const int MinLevel = 10, MaxLevel = 20;

    /// <summary>The day's four tiles (<c>PlayerAvatar_IsFacingFeebasTile</c>).</summary>
    public static (int X, int Y)[] TilesToday(IReadOnlyList<(int X, int Y)> lake, uint dailyNumber)
    {
        var today = new (int X, int Y)[4];
        int group = lake.Count / 4;
        if (group == 0) return Array.Empty<(int, int)>();
        int excess = lake.Count % 4, overflow = 0;
        for (int i = 0; i < 4; i++)
        {
            int pick = (int)((dailyNumber >> (24 - 8 * i)) & 0xff);
            today[i] = lake[group * i + pick % group + overflow];
            if (excess != 0)
            {
                overflow++;
                excess--;
            }
        }
        return today;
    }

    /// <summary>Whether a cast onto a tile hooks Feebas: half the time nothing is checked at all, then whether the tile is one of the day's four.</summary>
    public static bool Bites(IReadOnlyList<(int X, int Y)> lake, uint dailyNumber, int x, int y, Random rng)
    {
        if (rng.Next(2) == 0) return false;
        return TilesToday(lake, dailyNumber).Contains((x, y));
    }

    /// <summary>The table a cast onto one of the day's tiles fishes from: Feebas in every slot, levels 10 to 20.</summary>
    public static List<WildEncounterEntry> Table(string species, IReadOnlyList<WildEncounterEntry> rod) =>
        rod.Select(slot => new WildEncounterEntry { SpeciesName = species, MinLevel = MinLevel, MaxLevel = MaxLevel, Weight = slot.Weight }).ToList();
}
