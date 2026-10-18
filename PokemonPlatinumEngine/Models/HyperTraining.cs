using System;
using System.Linq;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models;

/// <summary>
/// Hyper training (plan 06 · R28b; Sun and Moon on, Mr. Hyper's): a Bottle Cap makes one of a Pokémon's stats grow as if
/// its IV were 31, a Gold Bottle Cap all six. The IV itself is left as it was (<see cref="Pokemon.HyperTrained"/>):
/// what an Egg inherits and Hidden Power reads is the Pokémon's own. As Scarlet and Violet have it, a Pokémon of level
/// 50 or more may be trained (Sun, Moon and Sword and Shield asked for 100), and a stat whose IV is 31 already, or that
/// has been trained, can't be. Nobody in the game trains yet: the later regions' chapters put Mr. Hyper somewhere, and
/// call these. No drawing or input.
/// </summary>
public static class HyperTraining
{
    /// <summary>The IV a trained stat is reckoned with.</summary>
    public const int Perfect = 31;

    /// <summary>The lowest level a Pokémon may be trained at (Scarlet and Violet's).</summary>
    public const int Level = 50;

    public const string BottleCap = "Bottle Cap", GoldBottleCap = "Gold Bottle Cap";

    /// <summary>All six stats' bits.</summary>
    public const int All = 0b111111;

    /// <summary>A stat's bit in <see cref="Pokemon.HyperTrained"/>, in <see cref="EffortRules.Order"/>'s order.</summary>
    public static int Bit(StatType stat) => Array.IndexOf(EffortRules.Order, stat) is var i and >= 0 ? 1 << i : 0;

    /// <summary>One of the two caps hyper training is paid in.</summary>
    public static bool IsCap(ItemData item) => item.Name is BottleCap or GoldBottleCap;

    /// <summary>A stat's own IV.</summary>
    public static int IvOf(Pokemon p, StatType stat) => stat switch
    {
        StatType.HP => p.IvHP,
        StatType.Attack => p.IvAttack,
        StatType.Defense => p.IvDefense,
        StatType.SpAttack => p.IvSpAttack,
        StatType.SpDefense => p.IvSpDefense,
        _ => p.IvSpeed
    };

    /// <summary>Whether this stat of the Pokémon could be trained: not an Egg, level 50 or more, the IV under 31 and not trained already.</summary>
    public static bool CanTrain(Pokemon p, StatType stat) =>
        !p.IsEgg && p.Level >= Level && Bit(stat) != 0 && IvOf(p, stat) < Perfect && !p.IsHyperTrained(stat);

    /// <summary>Whether a cap would do anything for it: a Bottle Cap for the stat named, a Gold Bottle Cap for any of the six.</summary>
    public static bool WouldHelp(ItemData cap, Pokemon p, StatType stat) =>
        cap.Name == GoldBottleCap ? EffortRules.Order.Any(s => CanTrain(p, s)) : cap.Name == BottleCap && CanTrain(p, stat);

    /// <summary>Trains the stat (a Bottle Cap) or every stat it can (a Gold Bottle Cap) and reckons the stats again; false, changing nothing, when it can't.</summary>
    public static bool Train(ItemData cap, Pokemon p, StatType stat = StatType.HP)
    {
        if (!WouldHelp(cap, p, stat)) return false;
        foreach (var s in cap.Name == GoldBottleCap ? EffortRules.Order : new[] { stat })
            if (CanTrain(p, s)) p.HyperTrained |= Bit(s);
        p.ReckonStats();
        return true;
    }
}
