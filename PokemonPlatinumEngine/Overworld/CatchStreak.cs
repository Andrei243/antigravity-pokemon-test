using System;
using System.Collections.Generic;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Overworld;

/// <summary>
/// The catch streak and the chain of hooks (plan 38 · EX3; <c>docs/mechanics/rulings.md</c>, "The field beyond the tables"),
/// saved as <see cref="SaveData.CatchStreak"/> and left out while both are empty:
/// <list type="bullet">
/// <item>the catch streak, after Let's Go's catch combo: catches in a row of one species met in sight (the options' WILD
/// POKéMON row at VISIBLE), broken by a catch of another species or by the chained species fleeing;</item>
/// <item>the chain of hooks, after X and Y's chain fishing: Pokémon of one species hooked in a row on a rod at one spot, broken
/// by a cast that hooks nothing or lets the bite go, by another species, or by leaving the spot.</item>
/// </list>
/// GPU-free; no input or drawing.
/// </summary>
public sealed class CatchStreak
{
    /// <summary>The species of the streak; null when there is none.</summary>
    public string? Species { get; set; }

    /// <summary>How many of it have been caught in a row.</summary>
    public int Count { get; set; }

    /// <summary>The species of the chain of hooks; null when there is none.</summary>
    public string? HookSpecies { get; set; }

    /// <summary>The map and tile the chain was cast from (a chain is one spot's).</summary>
    public string? HookMap { get; set; }
    public int HookX { get; set; }
    public int HookY { get; set; }

    /// <summary>How many of it have been hooked in a row there.</summary>
    public int Hooks { get; set; }

    /// <summary>Whether there is nothing to keep (a save leaves the field out).</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public bool IsEmpty => Species == null && HookSpecies == null;

    // ---------------------------------------------------------------- the streak

    /// <summary>The streak from which a meeting of its species in sight has its bonus: the 12th catch's Pokémon.</summary>
    public const int FirstStep = 11, SecondStep = 21, ThirdStep = 31;

    /// <summary>The perfect IVs a meeting of the streak's species has by the streak (Let's Go's: two from 11, three from 21, four from 31).</summary>
    public static int PerfectIvs(int streak) => streak >= ThirdStep ? 4 : streak >= SecondStep ? 3 : streak >= FirstStep ? 2 : 0;

    /// <summary>The shiny rolls more a meeting of the streak's species has (Let's Go's: three from 11, seven from 21, eleven from 31).</summary>
    public static int ShinyRolls(int streak) => streak >= ThirdStep ? 11 : streak >= SecondStep ? 7 : streak >= FirstStep ? 3 : 0;

    /// <summary>A Pokémon met in sight is caught: the streak goes on with its species, or begins again with this one.</summary>
    public void Caught(string species)
    {
        if (Species == species) Count++;
        else
        {
            Species = species;
            Count = 1;
        }
    }

    /// <summary>A Pokémon met in sight fled: the streak is broken if it was the streak's species.</summary>
    public void Fled(string species)
    {
        if (Species != species) return;
        Species = null;
        Count = 0;
    }

    /// <summary>The streak a meeting of this species is given; nought for another species.</summary>
    public int StreakFor(string species) => Species == species ? Count : 0;

    // ---------------------------------------------------------------- the chain of hooks

    /// <summary>The most hooks a chain counts towards its rolls (X and Y stop at twenty).</summary>
    public const int MostHooks = 20;

    /// <summary>The shiny rolls more a hooked Pokémon of the chain's species has: two for each hook before it, to forty.</summary>
    public static int HookRolls(int hooks) => 2 * Math.Min(hooks, MostHooks);

    /// <summary>A cast hooks a Pokémon at a spot: the chain goes on where it is of the chain's species at its spot, or begins again.</summary>
    public void Hooked(string species, string map, int x, int y)
    {
        if (HookSpecies == species && HookMap == map && HookX == x && HookY == y) Hooks++;
        else
        {
            HookSpecies = species;
            HookMap = map;
            HookX = x;
            HookY = y;
            Hooks = 1;
        }
    }

    /// <summary>A cast hooks nothing or lets its bite go, or the player has left the spot: the chain is broken.</summary>
    public void BreakChain()
    {
        HookSpecies = null;
        HookMap = null;
        HookX = HookY = 0;
        Hooks = 0;
    }

    /// <summary>The hooks before this one a hooked Pokémon of this species at this spot counts; nought for another or elsewhere.</summary>
    public int ChainFor(string species, string map, int x, int y) =>
        HookSpecies == species && HookMap == map && HookX == x && HookY == y ? Hooks : 0;

    /// <summary>
    /// The streak's bonus on a Pokémon met in sight (after Let's Go's): its perfect IVs, each a stat drawn among those not yet
    /// perfect, and its shiny rolls, drawn only where the streak is at a step. Nothing is drawn below <see cref="FirstStep"/>.
    /// </summary>
    public static void Bonus(Pokemon p, int streak, Random rng, Gender? gender = null, Nature? nature = null)
    {
        int ivs = PerfectIvs(streak);
        if (ivs <= 0) return;
        var stats = new List<StatType> { StatType.HP, StatType.Attack, StatType.Defense, StatType.SpAttack, StatType.SpDefense, StatType.Speed };
        stats.RemoveAll(s => IvOf(p, s) == 31);
        for (int n = ivs - (6 - stats.Count); n > 0 && stats.Count > 0; n--)
        {
            var stat = stats[rng.Next(stats.Count)];
            stats.Remove(stat);
            BrightPokemon.SetIv(p, stat, 31);
        }
        p.ReckonStats();
        p.RerollForShiny(rng, ShinyRolls(streak), gender, nature);
    }

    private static int IvOf(Pokemon p, StatType stat) => stat switch
    {
        StatType.HP => p.IvHP,
        StatType.Attack => p.IvAttack,
        StatType.Defense => p.IvDefense,
        StatType.SpAttack => p.IvSpAttack,
        StatType.SpDefense => p.IvSpDefense,
        _ => p.IvSpeed
    };
}
