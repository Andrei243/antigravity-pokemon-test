using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Data;

/// <summary>
/// The three groups the Battle Palace sorts a Pokémon's moves into (plan 14 · B9; Emerald's <c>PALACE_MOVE_GROUP_*</c>):
/// moves that strike, moves that look after the user, and moves that work on the foe or the field.
/// </summary>
public enum PalaceGroup { Attack, Defence, Support }

/// <summary>The line a Pokémon of the Battle Palace says when it first falls to half its HP (Emerald's <c>B_MSG_*</c> of the four).</summary>
public enum PalaceFlavour { Eager, Glint, Position, Growl }

/// <summary>One nature's row of <c>Data/palace-natures.json</c>: percentages of the attack and defence groups, high and low.</summary>
public sealed class PalaceStyle
{
    public Nature Nature { get; set; }
    public int Attack { get; set; }
    public int Defence { get; set; }
    public int AttackLow { get; set; }
    public int DefenceLow { get; set; }
    public PalaceFlavour Flavour { get; set; }

    /// <summary>The support group's share: what the other two leave.</summary>
    public int Support(bool low) => 100 - (low ? AttackLow + DefenceLow : Attack + Defence);

    /// <summary>
    /// The group a roll of 0 to 99 falls in (<c>ChooseMoveAndTargetInBattlePalace</c>): below the attack share the attack
    /// group, below the attack and defence shares together the defence group, the support group otherwise. Low is the
    /// Pokémon at half its HP or less and awake (<see cref="Battle.Sim.BattleCore.PalaceInTheRed"/>).
    /// </summary>
    public PalaceGroup GroupFor(int percent, bool low)
    {
        int attack = low ? AttackLow : Attack, defence = low ? DefenceLow : Defence;
        if (percent < attack) return PalaceGroup.Attack;
        if (percent < attack + defence) return PalaceGroup.Defence;
        return PalaceGroup.Support;
    }
}

public sealed class PalaceNaturesFile
{
    public List<PalaceStyle> Natures { get; set; } = new();
}

/// <summary>
/// How a Pokémon of the Battle Palace chooses by its nature (plan 14 · B9; <c>docs/mechanics/rulings.md</c>, "The Battle
/// Palace"): the table of <c>Data/palace-natures.json</c> and the group each move is in. GPU-free.
/// </summary>
public static class PalaceNatures
{
    public const string FileName = "palace-natures.json";

    private static Dictionary<Nature, PalaceStyle>? table;

    private static Dictionary<Nature, PalaceStyle> Table => table ??= GameDataFiles.Load<PalaceNaturesFile>(FileName).Natures.ToDictionary(s => s.Nature);

    /// <summary>A nature's row; every nature has one.</summary>
    public static PalaceStyle Of(Nature nature) => Table.TryGetValue(nature, out var style) ? style
        : throw new InvalidOperationException($"{FileName} has no row for {nature}");

    public static IReadOnlyCollection<PalaceStyle> All => Table.Values;

    /// <summary>
    /// The group a move is in (<c>GetBattlePalaceMoveGroup</c>, read by the move's target): aimed at a foe (one, a random
    /// one, both or everyone else, and the user or its ally) it strikes unless it is a status move, which supports; aimed
    /// at the foes' side it supports; aimed at the user or its own side it defends; and anything else falls to the
    /// original's last case and strikes (a weather move, which Emerald aims at every battler). A status move is told by its
    /// category, not by a power of 0, which this game's data gives every move of variable power too.
    /// </summary>
    public static PalaceGroup GroupOf(MoveData move) => move.Target switch
    {
        MoveTarget.Selected or MoveTarget.RandomFoe or MoveTarget.AllFoes or MoveTarget.AllOthers or MoveTarget.UserOrAlly =>
            move.Category == MoveCategory.Status ? PalaceGroup.Support : PalaceGroup.Attack,
        MoveTarget.FoeSide => PalaceGroup.Support,
        MoveTarget.User or MoveTarget.UserSide or MoveTarget.UserAndAllies => PalaceGroup.Defence,
        _ => PalaceGroup.Attack
    };
}
