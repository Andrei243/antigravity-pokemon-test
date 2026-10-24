using PokemonPlatinumEngine.Core;
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
            Say(Text.Of("battle.triedToGetAway", ("pokemon", runner.Name)));
            return;
        }
        Say(Text.Of("battle.ranAway", ("pokemon", runner.Name))).With(new Left(runner.Place, Ran: true));
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
        Say(Text.Of("battle.tossedSomeBaitToward", ("player", playerName), ("pokemon", foe.Pokemon.DisplayName))).With(Moment(foe, BattleMoment.Happy));
        Say(roll == 0 ? Text.Of("battle.theWildIsToo", ("pokemon", foe.Pokemon.DisplayName)) : Text.Of("battle.theWildIsMunching", ("pokemon", foe.Pokemon.DisplayName))).With(Moment(foe, BattleMoment.Eating));
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
        Say(Text.Of("battle.flungSomeMudAt", ("player", playerName), ("pokemon", foe.Pokemon.DisplayName)));
        Say(roll == 0 ? Text.Of("battle.theWildIsAbsolutely", ("pokemon", foe.Pokemon.DisplayName)) : Text.Of("battle.theWildIsFuming", ("pokemon", foe.Pokemon.DisplayName))).With(Moment(foe, BattleMoment.Angry));
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
            Say(Text.Of("battle.theWildFled", ("pokemon", p.DisplayName))).With(new Left(foe.Place, Ran: true));
            End(BattleResult.EnemyFled);
            return;
        }
        Say(Text.Of("battle.theWildIsKeeping", ("pokemon", p.DisplayName)));
    }

    /// <summary>The Bug-Catching Contest's Sport Ball (plan 15 · E5): thrown only there, and nowhere else.</summary>
    public const string BugContestBall = "Sport Ball";

    /// <summary>
    /// The last Sport Ball of a Bug-Catching Contest thrown and missed (plan 15 · E5): the battle is over, as the Great
    /// Marsh's is with its last ball, and the field ends the contest.
    /// </summary>
    private void ContestBallsGone()
    {
        if (SpecialBalls > 0) return;
        Say(Text.Of("battle.youveThrownYourLastSport"));
        End(BattleResult.PlayerRan);
    }

    /// <summary><c>BtlCmd_CheckSafariGameDone</c>: with no Safari Ball left, or nowhere to put a catch, the game in the Marsh is over.</summary>
    private void SafariGameOver()
    {
        if (SpecialBalls > 0) return;
        Say(Text.Of("battle.youveThrownYourLast"));
        Say(Text.Of("battle.thatTheEndOf"));
        End(BattleResult.PlayerRan);
    }
}
