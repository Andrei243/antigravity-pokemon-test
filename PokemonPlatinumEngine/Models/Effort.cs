using System;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models;

/// <summary>
/// Effort values as Platinum counts them (plan 06 · R10): what a Pokémon gains for each foe it helps beat, and what
/// the vitamins and the berries that lower them do. At most 255 in a stat and 510 in all; a vitamin raises a stat
/// only while it is under 100. The modern rules' limits (252 a stat, vitamins up to it) are the
/// <see cref="Ruleset"/>'s. No drawing or input.
/// </summary>
public static class EffortRules
{
    public const int MaxStat = 255, MaxTotal = 510;

    /// <summary>What a vitamin raises a stat to at most (<c>MAX_EV_VITAMIN</c>); an EV berry lowers anything above it to it.</summary>
    public const int VitaminLimit = 100;

    /// <summary>The stats in the order the original goes through them: HP, Attack, Defense, Speed, Sp. Atk, Sp. Def.</summary>
    public static readonly StatType[] Order = { StatType.HP, StatType.Attack, StatType.Defense, StatType.Speed, StatType.SpAttack, StatType.SpDefense };

    /// <summary>The hold effects of the Power items, each adding its own number to one stat's gain (<c>HOLD_EFFECT_LVLUP_*_EV_UP</c>).</summary>
    private static string PowerItemFor(StatType stat) => stat switch
    {
        StatType.HP => "LvlupHpEvUp",
        StatType.Attack => "LvlupAtkEvUp",
        StatType.Defense => "LvlupDefEvUp",
        StatType.Speed => "LvlupSpeedEvUp",
        StatType.SpAttack => "LvlupSpatkEvUp",
        _ => "LvlupSpdefEvUp"
    };

    /// <summary>The Macho Brace's hold effect, which doubles every gain.</summary>
    public const string MachoBrace = "EvsUpSpeedDown";

    public static int Get(Pokemon p, StatType stat) => stat switch
    {
        StatType.HP => p.EvHP,
        StatType.Attack => p.EvAttack,
        StatType.Defense => p.EvDefense,
        StatType.SpAttack => p.EvSpAttack,
        StatType.SpDefense => p.EvSpDefense,
        StatType.Speed => p.EvSpeed,
        _ => 0
    };

    public static void Set(Pokemon p, StatType stat, int value)
    {
        switch (stat)
        {
            case StatType.HP: p.EvHP = value; break;
            case StatType.Attack: p.EvAttack = value; break;
            case StatType.Defense: p.EvDefense = value; break;
            case StatType.SpAttack: p.EvSpAttack = value; break;
            case StatType.SpDefense: p.EvSpDefense = value; break;
            case StatType.Speed: p.EvSpeed = value; break;
        }
    }

    public static int Total(Pokemon p) => p.EvHP + p.EvAttack + p.EvDefense + p.EvSpAttack + p.EvSpDefense + p.EvSpeed;

    public static int YieldOf(StatSpread? spread, StatType stat) => spread == null ? 0 : stat switch
    {
        StatType.HP => spread.HP,
        StatType.Attack => spread.Attack,
        StatType.Defense => spread.Defense,
        StatType.SpAttack => spread.SpAttack,
        StatType.SpDefense => spread.SpDefense,
        StatType.Speed => spread.Speed,
        _ => 0
    };

    /// <summary>
    /// <c>BattleScript_CalcEffortValues</c>: what a Pokémon gains for a foe it helped beat, stat by stat in the
    /// original's order. Each stat's gain is the foe's yield, with a Power item's number added to its own stat,
    /// doubled by Pokérus (caught or cured) and again by the Macho Brace, then cut to what the two limits leave;
    /// once the total reaches 510 the rest is skipped. Its stats are not reckoned again: as in the original, the
    /// effort shows at its next level (or whenever its stats are next worked out).
    /// </summary>
    public static void Gain(Pokemon p, StatSpread? yield, Ruleset? rules = null)
    {
        int statCap = (rules ?? Ruleset.Current).EvStatCap;
        string? hold = p.HeldItem?.HoldEffect;
        int param = p.HeldItem?.HoldParam ?? 0;
        int total = Total(p);
        foreach (var stat in Order)
        {
            if (total >= MaxTotal) break;
            int gain = YieldOf(yield, stat);
            if (hold == PowerItemFor(stat)) gain += param;
            if (p.Pokerus != 0) gain *= 2;
            if (hold == MachoBrace) gain *= 2;

            int current = Get(p, stat);
            gain = Math.Min(gain, MaxTotal - total);
            gain = Math.Max(0, Math.Min(gain, statCap - current));
            Set(p, stat, current + gain);
            total += gain;
        }
    }

    /// <summary>The change an item makes to a stat's effort (its <c>use</c> numbers: a vitamin's +10, a berry's -10); 0 when it makes none.</summary>
    public static int ChangeBy(ItemData item, StatType stat) => Use(item, stat switch
    {
        StatType.HP => "hpEVs",
        StatType.Attack => "atkEVs",
        StatType.Defense => "defEVs",
        StatType.SpAttack => "spatkEVs",
        StatType.SpDefense => "spdefEVs",
        StatType.Speed => "speedEVs",
        _ => ""
    });

    /// <summary>One of the item table's use numbers (items.json's <c>use</c>); 0 when the item has none.</summary>
    public static int Use(ItemData item, string key) => item.Use != null && item.Use.TryGetValue(key, out int value) ? value : 0;

    /// <summary>The friendship an item gives, for the three bands (below 100, below 200, from 200).</summary>
    public static (int Low, int Mid, int High) FriendshipOf(ItemData item) =>
        (Use(item, "friendshipLow"), Use(item, "friendshipMed"), Use(item, "friendshipHigh"));

    /// <summary>Whether the item changes anyone's effort at all: a vitamin, or a berry that lowers it.</summary>
    public static bool IsEffortItem(ItemData item)
    {
        foreach (var stat in Order) if (ChangeBy(item, stat) != 0) return true;
        return false;
    }

    /// <summary>
    /// <c>CalculateEVUpdate</c>: the stat's effort after an item's change, or null when it changes nothing (a
    /// berry on a stat at 0; a vitamin on a stat at 100 or more, or when the total is full). A raise stops at 100
    /// and at what the total leaves; a cut stops at 0, and a stat above 100 comes down to 100 at once.
    /// </summary>
    public static int? AfterItem(int current, int others, int change, int limit = VitaminLimit)
    {
        if (current == 0 && change < 0) return null;
        if (current >= limit && change > 0) return null;
        if (current + others >= MaxTotal && change > 0) return null;
        current += change;
        current = Math.Clamp(current, 0, limit);
        if (current + others > MaxTotal) current = MaxTotal - others;
        return current;
    }

    /// <summary>
    /// Uses a vitamin or an EV berry (<c>Pokemon_ApplyItemEffects</c>): the effort change, then the item's
    /// friendship by the three bands, if the change did anything. A berry also works when it changes no effort but
    /// would raise friendship (<c>CheckFriendshipItemEffect</c>). Shedinja's HP effort can't be raised. Returns
    /// false, changing nothing, when it would have no effect.
    /// </summary>
    public static bool UseItem(ItemData item, Pokemon p, Ruleset? rules = null)
    {
        int limit = (rules ?? Ruleset.Current).VitaminLimit;
        if (!WouldHelp(item, p, rules)) return false;
        bool changed = false, raises = false;
        foreach (var stat in Order)
        {
            int change = ChangeBy(item, stat);
            if (change == 0 || (stat == StatType.HP && p.Species.Name == "Shedinja")) continue;
            if (change > 0) raises = true;
            int current = Get(p, stat);
            if (AfterItem(current, Total(p) - current, change, limit) is { } after)
            {
                Set(p, stat, after);
                changed = true;
            }
        }
        if (changed) p.ReckonStats();
        // A vitamin that changed nothing does nothing at all; a berry still pleases
        if (!changed && raises) return false;
        FriendshipRules.ChangeByItem(p, FriendshipOf(item));
        return true;
    }

    /// <summary>Whether using it on this Pokémon would do anything (<c>Pokemon_CheckItemEffects</c>).</summary>
    public static bool WouldHelp(ItemData item, Pokemon p, Ruleset? rules = null)
    {
        int limit = (rules ?? Ruleset.Current).VitaminLimit;
        foreach (var stat in Order)
        {
            int change = ChangeBy(item, stat);
            if (change == 0 || (stat == StatType.HP && p.Species.Name == "Shedinja")) continue;
            int current = Get(p, stat);
            if (change > 0 && current < limit && Total(p) < MaxTotal) return true;
            if (change < 0 && (current > 0 || FriendshipRules.WouldRaise(p, FriendshipOf(item)))) return true;
        }
        return false;
    }
}
