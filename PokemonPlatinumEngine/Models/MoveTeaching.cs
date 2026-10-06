using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models;

/// <summary>What the party shows beside a Pokémon while a TM or an HM is being taught (Platinum's ABLE, NOT ABLE, LEARNED).</summary>
public enum TeachAnswer { Able, NotAble, Learned }

/// <summary>
/// Moves taught from the bag (plan 06 · R11): a TM or an HM, by the species' list of the machines it learns
/// (<see cref="PokemonSpecies.TmMoves"/>, the decompilation's own for Platinum's species), and a move a Rare Candy's
/// level brings to a Pokémon that knows four already. A TM is used up when it teaches, as in Platinum, and an HM
/// isn't; a move an HM taught can't be forgotten to make room (only the Move Deleter takes one away). No drawing or
/// input.
/// </summary>
public static class MoveTeaching
{
    /// <summary>A TM or an HM: an item of the TMs pocket that teaches a move.</summary>
    public static bool IsMachine(ItemData item) => item.Pocket == ItemPocket.TMsAndHMs && item.TeachesMove != null;

    public static bool IsHm(ItemData item) => IsMachine(item) && item.Name.StartsWith("HM", StringComparison.Ordinal);

    private static readonly Lazy<HashSet<string>> HmMoves = new(() =>
        ItemDatabase.GetAll().Where(IsHm).Select(i => i.TeachesMove!).ToHashSet(StringComparer.OrdinalIgnoreCase));

    /// <summary>Whether a move is one an HM teaches (Cut, Fly, Surf, Strength, Defog, Rock Smash, Waterfall, Rock Climb).</summary>
    public static bool IsHmMove(string move) => HmMoves.Value.Contains(move);

    /// <summary>Whether the Pokémon's species can learn what the machine teaches.</summary>
    public static bool CanLearn(Pokemon p, ItemData machine) =>
        IsMachine(machine) && p.Species.TmMoves?.Contains(machine.Name) == true;

    /// <summary>What the party shows beside the Pokémon for the machine.</summary>
    public static TeachAnswer Answer(Pokemon p, ItemData machine) =>
        p.Knows(machine.TeachesMove!) ? TeachAnswer.Learned : CanLearn(p, machine) ? TeachAnswer.Able : TeachAnswer.NotAble;

    /// <summary>Why the move in this place can't be forgotten to make room, or null when it can.</summary>
    public static string? WhyNotForget(Pokemon p, int slot) =>
        slot >= 0 && slot < p.Moves.Count && IsHmMove(p.Moves[slot].Name) ? "HM moves can't be forgotten now." : null;

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
            return $"{p.DisplayName} learned {move}!";
        }
        if (forget < 0 || forget >= p.Moves.Count) throw new ArgumentOutOfRangeException(nameof(forget), "A Pokémon with four moves forgets one to learn another");
        string old = p.Moves[forget].Name;
        p.Moves[forget] = learned;
        return $"1, 2, and... Poof! {p.DisplayName} forgot {old}. And... {p.DisplayName} learned {move}!";
    }
}
