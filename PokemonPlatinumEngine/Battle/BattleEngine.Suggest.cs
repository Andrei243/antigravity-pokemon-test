using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Battle;

public partial class BattleEngine
{
    /// <summary>
    /// The move a player with nothing else in mind would use now (plan 16 · T7, the playthrough bot; T12's headless
    /// game): the slot in <see cref="MenuBattler"/>'s moves that the rules let it pick and that hits hardest by the
    /// move's power, the same type's bonus, its accuracy and the type chart against the first foe on the field (an
    /// inverse battle's chart where the battle is one), a status move counting for nothing; -1 when nothing can be
    /// picked, which is Struggle. It draws nothing: the trainer AI rolls on the battle's own generator, and a choice
    /// made outside the rules must not move what they roll next, or a recorded battle would no longer replay.
    /// </summary>
    public int Suggest()
    {
        var mine = InCore(MenuBattler);
        var usable = core.UsableMoves(mine);
        if (usable.Count == 0) return -1;
        var foe = core.ActiveFoes(mine).FirstOrDefault();
        var moves = mine.Pokemon!.Moves;
        int best = moves.IndexOf(usable[0]);
        float bestScore = -1f;
        for (int i = 0; i < moves.Count; i++)
        {
            var move = moves[i];
            if (!usable.Contains(move)) continue;
            var data = move.Data;
            float score = 0f;
            if (data.Category != MoveCategory.Status)
            {
                score = System.Math.Max(1, data.Power) * (mine.HasType(data.Type) ? 1.5f : 1f) * (data.Accuracy <= 0 ? 100 : data.Accuracy) / 100f;
                if (foe?.Pokemon != null && foe.Types.Count > 0)
                    score *= TypeChart.GetEffectiveness(data.Type, foe.Types[0], foe.Types.Count > 1 ? foe.Types[1] : null, core.Rules, core.Field.Inverse);
            }
            if (score > bestScore)
            {
                bestScore = score;
                best = i;
            }
        }
        return best;
    }
}
