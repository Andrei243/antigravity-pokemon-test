using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models;

/// <summary>What the party shows beside a Pokémon while a TM or an HM is being taught (Platinum's ABLE, NOT ABLE, LEARNED).</summary>
public enum TeachAnswer { Able, NotAble, Learned }

/// <summary>
/// Moves taught from the bag (plan 06 · R11): a TM or an HM, by the species' list of the machines it learns
/// (<see cref="PokemonSpecies.TmMoves"/>, the decompilation's own for Platinum's species, and under the modern rules
/// <see cref="PokemonSpecies.LaterMachines"/> too, plan 36 · TR9), and a move a Rare Candy's level brings to a Pokémon
/// that knows four already. A TM is used up when it teaches, as in Platinum, the later machines of ours too (plan 39's
/// decision 4: used up in every game), and an HM, which can't be thrown away, isn't (<see cref="IsUsedUp"/>). A move an
/// HM taught can't be forgotten to make room (only the Move Deleter takes one away). No drawing or input.
/// </summary>
public static class MoveTeaching
{
    /// <summary>
    /// The later games' machines of ours (plan 36 · TR9, decision 7): ids <see cref="LaterFrom"/> to <see cref="LaterTo"/>
    /// (the block from 10000 on: their two hundred and more don't fit in the hundred of 9800 the plan first gave them, and
    /// plan 39 · CR1's Crafting Kit holds 9900), named on from Platinum's TM92 (TM93 is the first), which only the modern
    /// rules teach (<see cref="Ruleset.LaterMachines"/>).
    /// </summary>
    public const int LaterFrom = 10000, LaterTo = 10399;

    /// <summary>A TM or an HM: an item of the TMs pocket that teaches a move.</summary>
    public static bool IsMachine(ItemData item) => item.Pocket == ItemPocket.TMsAndHMs && item.TeachesMove != null;

    public static bool IsHm(ItemData item) => IsMachine(item) && item.Name.StartsWith("HM", StringComparison.Ordinal);

    /// <summary>One of the later games' machines of ours (TM93 on).</summary>
    public static bool IsLater(ItemData item) => IsMachine(item) && item.Id >= LaterFrom && item.Id <= LaterTo;

    /// <summary>
    /// Whether teaching with the machine uses it up: a TM of Platinum's, a TR and a later machine of ours are (plan 39's
    /// decision 4: used up in every game, made again at a PC with the crafting kit), and an HM, the machine that can't be
    /// thrown away, isn't.
    /// </summary>
    public static bool IsUsedUp(ItemData item) => IsMachine(item) && !item.CantBeTossed;

    private static readonly Lazy<HashSet<string>> HmMoves = new(() =>
        ItemDatabase.GetAll().Where(IsHm).Select(i => i.TeachesMove!).ToHashSet(StringComparer.OrdinalIgnoreCase));

    /// <summary>Whether a move is one an HM teaches (Cut, Fly, Surf, Strength, Defog, Rock Smash, Waterfall, Rock Climb).</summary>
    public static bool IsHmMove(string move) => HmMoves.Value.Contains(move);

    /// <summary>Whether the Pokémon's species can learn what the machine teaches by the rules of the game in progress (never an Egg, plan 06 · R15).</summary>
    public static bool CanLearn(Pokemon p, ItemData machine) => CanLearn(p, machine, Ruleset.Current);

    /// <summary>Whether the Pokémon's species can learn what the machine teaches by these rules (never an Egg).</summary>
    public static bool CanLearn(Pokemon p, ItemData machine, Ruleset rules) =>
        IsMachine(machine) && !p.IsEgg && !HeartLock.Refuses(p, LockedUse.Teach) && Takes(p.Species, machine, rules);

    /// <summary>
    /// Whether a species takes a machine by these rules: its own list (<see cref="PokemonSpecies.TmMoves"/>), and under the
    /// modern rules the machines its later games taught it (<see cref="PokemonSpecies.LaterMachines"/>). Platinum's rules
    /// never teach a later machine of ours (plan 36 · TR9).
    /// </summary>
    public static bool Takes(PokemonSpecies species, ItemData machine, Ruleset rules) =>
        (rules.LaterMachines || !IsLater(machine))
        && (species.TmMoves?.Contains(machine.Name) == true || rules.LaterMachines && species.LaterMachines?.Contains(machine.Name) == true);

    /// <summary>What the party shows beside the Pokémon for the machine.</summary>
    public static TeachAnswer Answer(Pokemon p, ItemData machine) =>
        p.IsEgg ? TeachAnswer.NotAble : p.Knows(machine.TeachesMove!) ? TeachAnswer.Learned : CanLearn(p, machine) ? TeachAnswer.Able : TeachAnswer.NotAble;

    /// <summary>Why the move in this place can't be forgotten to make room, or null when it can.</summary>
    public static string? WhyNotForget(Pokemon p, int slot) =>
        slot >= 0 && slot < p.Moves.Count && IsHmMove(p.Moves[slot].Name) ? Text.Of("teaching.hmMove") : null;

    /// <summary>
    /// Teaches the move: into a free place, or in place of the move at <paramref name="forget"/> (with all its PP).
    /// Returns what to tell the player.
    /// </summary>
    public static string Learn(Pokemon p, string move, int forget = -1)
    {
        var learned = MoveDatabase.Create(move);
        if (p.Moves.Count < 4)
        {
            p.Moves.Add(learned);
            FormRules.ByMoves(p);
            return Text.Of("bag.learned", ("name", p.DisplayName), ("move", move));
        }
        if (forget < 0 || forget >= p.Moves.Count) throw new ArgumentOutOfRangeException(nameof(forget), "A Pokémon with four moves forgets one to learn another");
        string old = p.Moves[forget].DisplayName;
        p.Moves[forget] = learned;
        // Keldeo's form goes with Secret Sword (plan 06 · R29)
        FormRules.ByMoves(p);
        return Text.Of("teaching.forgot", ("name", p.DisplayName), ("old", old), ("move", LocalNames.Move(move)));
    }
}
