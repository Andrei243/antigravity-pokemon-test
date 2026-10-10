using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models;

/// <summary>What using an item on a Pokémon came to: whether it did anything, and what to tell the player.</summary>
public sealed record ItemUseResult(bool Applied, string Message)
{
    /// <summary>The level it grew to, when the item raised its level.</summary>
    public int? NewLevel { get; init; }

    /// <summary>Moves of the levels it reached that it learned into a free place.</summary>
    public IReadOnlyList<string> Learned { get; init; } = Array.Empty<string>();

    /// <summary>Moves of those levels it couldn't learn, knowing four already: the bag asks about each.</summary>
    public IReadOnlyList<string> Wanted { get; init; } = Array.Empty<string>();

    public static readonly ItemUseResult Nothing = new(false, "It won't have any effect.");
}

/// <summary>
/// An item used on one Pokémon from the bag (plan 06 · R11), by the item table's use parameters: Platinum's
/// <c>Pokemon_CheckItemEffects</c> (would it do anything?) and <c>Pokemon_ApplyItemEffects</c> (do it), in the
/// original's order: the conditions, the HP (or a revival), a level, PP Ups, PP, effort, and last the friendship
/// the item gives, only once something has been done. No drawing or input. The battle has its own rules for an
/// item used in battle (<c>BattleCore.Items.cs</c>); evolution stones are <see cref="Evolution"/>'s and the
/// Gracidea <see cref="FormRules"/>'.
/// </summary>
public static class ItemUse
{
    /// <summary>A move with fewer PP than this can't take a PP Up (<c>PP_UP_REQUIREMENT</c>: Sketch).</summary>
    public const int PPUpRequirement = 5;

    private static bool Has(ItemData item, string key) => item.Use != null && item.Use.ContainsKey(key);

    private static int Num(ItemData item, string key) => EffortRules.Use(item, key);

    private static readonly string[] Friendship = { "friendshipLow", "friendshipMed", "friendshipHigh" };

    /// <summary>
    /// Whether the bag uses it on a Pokémon by its parameters: medicine and the berries that heal or restore
    /// something (their field use is <c>Healing</c> or <c>Berry</c>, <c>UseHealingItemFromMenu</c>). Berries with
    /// nothing to heal (Razz, Bluk) are for berry soil and Poffins.
    /// </summary>
    public static bool IsUsedOnPokemon(ItemData item) =>
        item.FieldUse is "Healing" or "Berry" && item.Use != null && item.Use.Keys.Any(k => !Friendship.Contains(k));

    /// <summary>Whether it is used on one move of the Pokémon's, chosen after the Pokémon: an Ether, a PP Up, a Leppa Berry.</summary>
    public static bool NeedsMove(ItemData item) =>
        Has(item, "ppUp") || Has(item, "ppMax") || (Has(item, "ppRestored") && !Has(item, "restorePPAllMoves"));

    /// <summary>Sacred Ash: it brings round every fainted Pokémon of the team at once, with no Pokémon chosen.</summary>
    public static bool RevivesAll(ItemData item) => Has(item, "reviveAll");

    private static bool Heals(ItemData item, StatusCondition status) => status switch
    {
        StatusCondition.Sleep => Has(item, "healSleep"),
        StatusCondition.Poison or StatusCondition.Toxic => Has(item, "healPoison"),
        StatusCondition.Burn => Has(item, "healBurn"),
        StatusCondition.Freeze => Has(item, "healFreeze"),
        StatusCondition.Paralyze => Has(item, "healParalysis"),
        _ => false
    };

    private static bool HealsAnyStatus(ItemData item) =>
        Has(item, "healSleep") || Has(item, "healPoison") || Has(item, "healBurn") || Has(item, "healFreeze") || Has(item, "healParalysis");

    private static Move? MoveIn(Pokemon p, int slot) => slot >= 0 && slot < p.Moves.Count ? p.Moves[slot] : null;

    private static bool MissingPP(Pokemon p, int slot) => MoveIn(p, slot) is { } m && m.CurrentPP < m.MaxPP;

    private static bool CanTakePPUp(Move m) => m.PPUps < 3 && m.Data.MaxPP >= PPUpRequirement;

    /// <summary>
    /// <c>Pokemon_CheckItemEffects</c>: whether using it on this Pokémon (and, for the items that need one, this
    /// move of its) would do anything: the bag's ABLE and NOT ABLE.
    /// </summary>
    public static bool WouldHelp(ItemData item, Pokemon p, int moveSlot = 0, Ruleset? rules = null)
    {
        // Nothing is used on an Egg (plan 06 · R15)
        if (!IsUsedOnPokemon(item) || p.IsEgg) return false;
        if (Heals(item, p.Status)) return true;

        if ((Has(item, "revive") || Has(item, "reviveAll")) && !Has(item, "levelUp"))
        {
            if (p.IsFainted) return true;
        }
        else if (Has(item, "hpRestored"))
        {
            if (!p.IsFainted && p.CurrentHP < p.MaxHP) return true;
        }

        if (Has(item, "levelUp") && p.Level < 100) return true;
        if ((Has(item, "ppUp") || Has(item, "ppMax")) && MoveIn(p, moveSlot) is { } raised && CanTakePPUp(raised)) return true;
        if (Has(item, "ppRestored") && !Has(item, "restorePPAllMoves") && MissingPP(p, moveSlot)) return true;
        if (Has(item, "restorePPAllMoves") && Enumerable.Range(0, p.Moves.Count).Any(i => MissingPP(p, i))) return true;
        return EffortRules.IsEffortItem(item) && EffortRules.WouldHelp(item, p, rules);
    }

    /// <summary>Whether it would do anything for the Pokémon with any of its moves (for an item that needs a move chosen).</summary>
    public static bool WouldHelpAnyMove(ItemData item, Pokemon p, Ruleset? rules = null) =>
        NeedsMove(item) ? Enumerable.Range(0, p.Moves.Count).Any(i => WouldHelp(item, p, i, rules)) : WouldHelp(item, p, 0, rules);

    /// <summary>
    /// <c>Pokemon_ApplyItemEffects</c>: uses it on the Pokémon and says what happened. Nothing is changed when it
    /// would have no effect (<see cref="WouldHelp"/> first, as the bag does).
    /// </summary>
    public static ItemUseResult Apply(ItemData item, Pokemon p, int moveSlot = 0, Ruleset? rules = null)
    {
        if (p.IsEgg) return ItemUseResult.Nothing;
        if (!WouldHelp(item, p, moveSlot, rules)) return ItemUseResult.Nothing;
        string name = p.DisplayName;

        // A vitamin or an EV berry changes effort and pleases (plan 06 · R10's rule, the original's own order)
        if (EffortRules.IsEffortItem(item))
        {
            int before = EffortRules.Total(p);
            if (!EffortRules.UseItem(item, p, rules)) return ItemUseResult.Nothing;
            return new ItemUseResult(true, EffortRules.Total(p) > before ? $"{name}'s base stats grew from the {item.Name}."
                : $"{name} grew friendlier, and its base stats came down a little.");
        }

        var said = new List<string>();
        bool applied = false, found = false;

        // The conditions first
        var status = p.Status;
        if (HealsAnyStatus(item)) found = true;
        if (Heals(item, status))
        {
            p.Status = StatusCondition.None;
            p.SleepTurns = 0;
            p.ToxicCounter = 0;
            applied = true;
            said.Add(status switch
            {
                StatusCondition.Sleep => $"{name} woke up.",
                StatusCondition.Poison or StatusCondition.Toxic => $"{name} was cured of its poisoning.",
                StatusCondition.Burn => $"{name}'s burn was healed.",
                StatusCondition.Freeze => $"{name} thawed out.",
                _ => $"{name} was cured of its paralysis."
            });
        }

        // Then the HP: a revival, or HP restored; a Rare Candy's revival waits for its level
        int hp = p.CurrentHP, max = p.MaxHP;
        bool fainted = p.IsFainted;
        if ((Has(item, "revive") || Has(item, "reviveAll")) && Has(item, "levelUp"))
        {
            found = true;
        }
        else if (Has(item, "hpRestored"))
        {
            if (hp < max || fainted)
            {
                int healed = Restored(hp, max, Num(item, "hpRestored"));
                if (fainted) p.Revive(Math.Max(1, healed));
                else p.CurrentHP = Math.Min(max, hp + healed);
                applied = true;
                said.Add(fainted ? $"{name} came round, with {p.CurrentHP} HP." : $"{name} recovered {p.CurrentHP - hp} HP.");
            }
            found = true;
        }

        // A level, with the moves it brings
        int? newLevel = null;
        IReadOnlyList<string> learned = Array.Empty<string>(), wanted = Array.Empty<string>();
        if (Has(item, "levelUp"))
        {
            if (p.Level < 100)
            {
                int oldMax = p.MaxHP, friendship = p.Friendship;
                p.GainExp(p.ExpForNextLevel - p.CurrentExp, out var newMoves, out var movesWanted);
                // A level's own friendship is the battle's (BattleScript's level-up); the candy gives only the table's
                p.Friendship = friendship;
                // Brought round with the hit points the level gave it (RestorePokemonHP of the difference)
                if (fainted) p.Revive(Math.Max(1, p.MaxHP - oldMax));
                newLevel = p.Level;
                learned = newMoves;
                wanted = movesWanted;
                applied = true;
                said.Add($"{name} grew to Lv. {p.Level}!");
                said.AddRange(newMoves.Select(m => $"{name} learned {m}!"));
            }
            found = true;
        }

        // PP Ups, then PP
        if (Has(item, "ppUp") || Has(item, "ppMax"))
        {
            if (MoveIn(p, moveSlot) is { } m && CanTakePPUp(m))
            {
                int oldMax = m.MaxPP;
                m.PPUps = Has(item, "ppUp") ? Math.Min(3, m.PPUps + 1) : 3;
                m.CurrentPP += m.MaxPP - oldMax;
                applied = true;
                said.Add($"{m.Name}'s PP rose.");
            }
            found = true;
        }
        if (Has(item, "ppRestored"))
        {
            int amount = Num(item, "ppRestored");
            var slots = Has(item, "restorePPAllMoves") ? Enumerable.Range(0, p.Moves.Count) : new[] { moveSlot };
            var restored = slots.Where(i => RestorePP(p, i, amount)).ToList();
            if (restored.Count > 0)
            {
                applied = true;
                said.Add(Has(item, "restorePPAllMoves") ? $"{name}'s PP was restored." : $"{p.Moves[restored[0]].Name}'s PP was restored.");
            }
            found = true;
        }

        if (!applied && found) return ItemUseResult.Nothing;
        FriendshipRules.ChangeByItem(p, EffortRules.FriendshipOf(item));
        return new ItemUseResult(applied, string.Join(" ", said)) { NewLevel = newLevel, Learned = learned, Wanted = wanted };
    }

    /// <summary>
    /// <c>RestorePokemonHP</c>'s amount: all, half or a quarter of the most HP (the table's -1, -2 and -3), or so
    /// many hit points; a Pokémon whose most is 1 (Shedinja) is given 1.
    /// </summary>
    public static int Restored(int current, int max, int amount)
    {
        if (max == 1) return 1;
        return amount switch
        {
            -1 => max,
            -2 => max / 2,
            -3 => max / 4,
            _ => Math.Max(0, amount)
        };
    }

    /// <summary><c>RestorePokemonMovePP</c>: so many PP back to one move (all of them for the table's -1), up to its most.</summary>
    private static bool RestorePP(Pokemon p, int slot, int amount)
    {
        if (MoveIn(p, slot) is not { } m || m.CurrentPP >= m.MaxPP) return false;
        m.CurrentPP = amount < 0 ? m.MaxPP : Math.Min(m.MaxPP, m.CurrentPP + amount);
        return true;
    }

    /// <summary>Sacred Ash: every fainted Pokémon of the team brought round with all its HP; the names of those it brought round.</summary>
    public static List<string> ReviveAll(ItemData item, Party party)
    {
        var revived = new List<string>();
        foreach (var p in party.Members.Where(p => p.IsFainted && !p.IsEgg))
        {
            p.Revive(Restored(0, p.MaxHP, Num(item, "hpRestored")));
            revived.Add(p.DisplayName);
        }
        return revived;
    }
}
