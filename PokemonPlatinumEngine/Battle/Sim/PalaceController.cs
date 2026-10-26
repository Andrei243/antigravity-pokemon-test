using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Battle.Sim;

/// <summary>
/// How a Pokémon of the Battle Palace chooses for itself (plan 14 · B9; <c>docs/mechanics/rulings.md</c>, "The Battle
/// Palace"): Emerald's <c>ChooseMoveAndTargetInBattlePalace</c>, on both sides of the field. A roll of 0 to 99 and the
/// Pokémon's nature (<see cref="PalaceNatures"/>, the second set of shares once it is at half its HP or less) choose a
/// group of moves; one of the group's moves with PP left is taken by chance (a rule of ours, where Emerald hands the group
/// to its AI), and a move the battle won't let it use is chosen all the same and comes to nothing
/// (<see cref="BattleChoice.Falters"/>). With no move of the group it picks among those it can use, its quirk kept (the
/// support group is never counted as one of several), and then a coin decides whether it can bring itself to use it.
/// Every roll is the battle's own (<see cref="RollKind.Palace"/>), so a Palace battle replays from its seed alone.
/// </summary>
public sealed class PalaceController : IBattleController
{
    public static PalaceController Instance { get; } = new();

    public BattleChoice ChooseAction(BattleCore battle, Battler mine)
    {
        var p = mine.Pokemon!;
        var place = mine.Place;
        // A Pokémon with nothing it can use struggles, as everywhere (Emerald asks AreAllMovesUnusable before the Palace's choice)
        if (battle.UsableMoves(mine).Count == 0) return BattleChoice.Fight(place, -1);

        var style = PalaceNatures.Of(p.Nature);
        int percent = battle.Random.Roll(RollKind.Palace, 100);
        var group = style.GroupFor(percent, battle.PalaceInTheRed(mine));
        var moves = p.Moves;

        // The group's moves with PP left: whatever else would stop one is found out only as it is used
        var inGroup = Enumerable.Range(0, moves.Count).Where(i => PalaceNatures.GroupOf(moves[i].Data) == group && moves[i].CurrentPP > 0).ToList();
        if (inGroup.Count > 0)
        {
            int chosen = inGroup[battle.Random.Roll(RollKind.Palace, inGroup.Count)];
            return BattleChoice.Fight(place, chosen) with { Falters = battle.WhyNotMove(mine, moves[chosen]) != null };
        }

        // None of the group: one of those it can use, from the group with several if there is one such, else any
        var usable = Enumerable.Range(0, moves.Count).Where(i => battle.WhyNotMove(mine, moves[i]) == null).ToList();
        if (usable.Count == 0) return BattleChoice.Fight(place, 0) with { Falters = true };
        int Count(PalaceGroup g) => usable.Count(i => PalaceNatures.GroupOf(moves[i].Data) == g);
        int several = 0;
        PalaceGroup preferred = PalaceGroup.Attack;
        if (Count(PalaceGroup.Attack) >= 2) { several++; preferred = PalaceGroup.Attack; }
        if (Count(PalaceGroup.Defence) >= 2) { several++; preferred = PalaceGroup.Defence; }
        // (Emerald's check of the support group reads the defence group's count against a bigger number, so it never holds)
        var pool = several == 1 ? usable.Where(i => PalaceNatures.GroupOf(moves[i].Data) == preferred).ToList() : usable;
        // Emerald draws a slot of four until one fits (a roll a test has fixed gives up on the first of the pool)
        int pick = -1;
        for (int tries = 0; tries < 64 && !pool.Contains(pick); tries++) pick = battle.Random.Roll(RollKind.Palace, 4);
        if (!pool.Contains(pick)) pick = pool[0];
        // A move off the nature's group is used one time in two
        bool falters = battle.Random.Roll(RollKind.Palace, 100) >= 50;
        return BattleChoice.Fight(place, pick) with { Falters = falters };
    }

    /// <summary>The Palace's Pokémon come in in the team's order: the next that can fight.</summary>
    public int ChooseReplacement(BattleCore battle, Battler place) =>
        Enumerable.Range(0, place.Roster?.Count ?? 0).FirstOrDefault(i => battle.CanSendIn(place, i), -1);
}
