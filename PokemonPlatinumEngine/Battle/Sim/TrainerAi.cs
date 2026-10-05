using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Battle.Sim;

/// <summary>
/// How the opponents choose, wild Pokémon and trainers alike, until plan 06 · R9 writes Platinum's own trainer AI:
/// the move that looks best against the Pokémon across the field (damage from power, type and STAB, with some
/// value on status and stat moves while they would still do something, and a little chance thrown in). It never
/// switches or uses an item, and sends its Pokémon out in the order of its party.
/// </summary>
public sealed class TrainerAi : IBattleController
{
    public static TrainerAi Instance { get; } = new();

    public BattleChoice ChooseAction(BattleCore battle, Battler mine)
    {
        var moves = BattleCore.UsableMoves(mine);
        if (moves.Count == 0) return BattleChoice.Fight(mine.Place, -1);

        var foes = battle.ActiveFoes(mine).ToList();
        var ally = battle.SlotsOf(mine.Side).FirstOrDefault(b => b != mine && b.IsActive);

        BattleChoice? best = null;
        float bestScore = float.MinValue;
        foreach (var move in moves)
        {
            var aims = move.Target == MoveTarget.Selected ? foes.Select(t => (Battler?)t).ToList() : new List<Battler?> { null };
            foreach (var aim in aims)
            {
                float score = Score(battle, mine, move, aim, foes, ally) * (0.85f + 0.3f * (float)battle.Random.RollFraction(RollKind.AiChoice));
                if (score <= bestScore) continue;
                bestScore = score;
                best = BattleChoice.Fight(mine.Place, mine.Pokemon!.Moves.IndexOf(move), aim?.Place);
            }
        }
        return best!;
    }

    public int ChooseReplacement(BattleCore battle, Battler place) =>
        Enumerable.Range(0, battle.PlayerParty.Count).Where(battle.CanSendIn).DefaultIfEmpty(-1).First();

    private static float Score(BattleCore battle, Battler user, Move move, Battler? aim, List<Battler> foes, Battler? ally)
    {
        var data = move.Data;
        if (data.Support == MoveEffectSupport.None) return 1f; // does nothing yet: only when there is nothing else
        if (move.Category == MoveCategory.Status)
        {
            var target = aim ?? foes.FirstOrDefault();
            if (data.HealPercent > 0) return user.Pokemon!.CurrentHP * 2 < user.Pokemon.MaxHP ? 90f : 0f;
            if (data.InflictStatus != StatusCondition.None)
                return target != null && target.Pokemon!.Status == StatusCondition.None ? 55f : 0f;
            if (data.ConfuseChancePercent > 0) return target != null && !target.IsConfused ? 40f : 0f;
            if (data.TargetStatChange is { } stat)
            {
                var who = data.StatChangeTargetSelf ? user : target;
                if (who == null) return 0f;
                int stage = who.Pokemon!.StatStages.GetValueOrDefault(stat);
                return data.StatChangeTargetSelf ? (stage < 2 ? 35f : 0f) : (stage > -2 ? 25f : 0f);
            }
            return 5f;
        }

        float Hit(Battler t) => move.Power * DamageCalculator.Effectiveness(user, t, move, battle.Rules) * (user.HasType(move.Type) ? 1.5f : 1f);

        float score;
        switch (move.Target)
        {
            case MoveTarget.Selected:
            case MoveTarget.RandomFoe:
                score = aim != null ? Hit(aim) : foes.Select(Hit).DefaultIfEmpty(0f).Max();
                break;
            default:
                score = foes.Sum(Hit) * (foes.Count > 1 ? 0.75f : 1f);
                if (move.Target == MoveTarget.AllOthers && ally != null) score -= Hit(ally);
                break;
        }
        return score * (data.Accuracy > 0 ? data.Accuracy / 100f : 1f);
    }
}
