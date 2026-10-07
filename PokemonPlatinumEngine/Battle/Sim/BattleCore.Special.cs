using System;
using System.Linq;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Battle.Sim;

// The battles that play by rules of their own (plan 06 · R9): a wild Pokémon running away (a roamer, one of the
// Great Marsh's), and the Great Marsh's bait and mud. The catching lesson and Pal Park are a ball that can't fail
// (ThrowBall) and, for the lesson, the helper's own AI (TrainerAi). Lines are our own, on the original's beats.
public sealed partial class BattleCore
{
    /// <summary>
    /// <c>BattleControllerPlayer_FleeCommand</c> for the other side: a wild Pokémon runs, unless something binds it or
    /// Mean Look holds it there.
    /// </summary>
    private void Flee(Battler runner)
    {
        var v = runner.Volatile;
        if (v.BindTurns > 0 || v.TrappedBy != null)
        {
            Say($"{runner.Name} tried to get away, but couldn't!");
            return;
        }
        Say($"{runner.Name} ran away!").With(new Left(runner.Place, Ran: true));
        End(BattleResult.EnemyFled);
    }

    /// <summary>
    /// <c>BattleControllerPlayer_SafariBaitCommand</c>: bait makes the Pokémon easier to catch (a stage up), and nine
    /// times in ten likelier to run (a stage up); the tenth time it is too busy eating to think of running.
    /// </summary>
    private void ThrowBait(Battler thrower)
    {
        var foe = EnemySlots[0];
        if (foe.Pokemon == null) return;
        int roll = rng.Roll(RollKind.Safari, 10);
        if (SafariCatchStage < 12) SafariCatchStage++;
        if (roll != 0 && SafariEscapeCount < 12) SafariEscapeCount++;
        Say($"{playerName} tossed some bait toward the wild {foe.Pokemon.DisplayName}!");
        Say(roll == 0 ? $"The wild {foe.Pokemon.DisplayName} is too wrapped up in eating to notice anything!" : $"The wild {foe.Pokemon.DisplayName} is munching away!");
    }

    /// <summary>
    /// <c>BattleControllerPlayer_SafariRockCommand</c>: mud makes the Pokémon less likely to run (a stage down), and
    /// nine times in ten harder to catch (a stage down).
    /// </summary>
    private void ThrowMud(Battler thrower)
    {
        var foe = EnemySlots[0];
        if (foe.Pokemon == null) return;
        int roll = rng.Roll(RollKind.Safari, 10);
        if (SafariEscapeCount > 0) SafariEscapeCount--;
        if (roll != 0 && SafariCatchStage > 0) SafariCatchStage--;
        Say($"{playerName} flung some mud at the wild {foe.Pokemon.DisplayName}!");
        Say(roll == 0 ? $"The wild {foe.Pokemon.DisplayName} is absolutely furious!" : $"The wild {foe.Pokemon.DisplayName} is fuming!");
    }

    /// <summary>
    /// <c>Task_SafariPokemonSetCommandSelection</c>: after the player's throw, the Great Marsh's Pokémon runs when a
    /// roll of 255 comes out at or under its species' flee rate, taken at the escape stage (the same thirteen
    /// fractions as the catch rate's); otherwise it watches.
    /// </summary>
    private void SafariPokemonActs()
    {
        var foe = EnemySlots[0];
        if (foe.Pokemon is not { } p || p.IsFainted) return;
        var (num, den) = CatchCalculator.SafariStages[SafariEscapeCount];
        int fleeRate = p.Species.SafariFleeRate * num / den;
        if (rng.Roll(RollKind.Safari, 255) <= fleeRate)
        {
            Say($"The wild {p.DisplayName} fled!").With(new Left(foe.Place, Ran: true));
            End(BattleResult.EnemyFled);
            return;
        }
        Say($"The wild {p.DisplayName} is keeping a close eye on you.");
    }

    /// <summary><c>BtlCmd_CheckSafariGameDone</c>: with no Safari Ball left, or nowhere to put a catch, the game in the Marsh is over.</summary>
    private void SafariGameOver()
    {
        if (SpecialBalls > 0) return;
        Say("You've thrown your last Safari Ball!");
        Say("That's the end of your time in the Great Marsh.");
        End(BattleResult.PlayerRan);
    }
}
