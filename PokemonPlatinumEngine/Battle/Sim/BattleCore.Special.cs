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
        if (EmeraldSafari)
        {
            EmeraldSafariPokemonActs(foe, p);
            return;
        }
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

    // Hoenn's Safari Zone (plan 21 · H11, part e): Emerald's Safari battle, from pret/pokeemerald's src/battle_util.c
    // (HandleAction_ThrowPokeblock, HandleAction_GoNear), src/battle_main.c (the factors as the battle begins),
    // src/battle_ai_script_commands.c (Cmd_if_random_safari_flee), src/pokeblock.c (PokeblockAction_UseInBattle) and
    // src/battle_script_commands.c (a Safari Ball's catch rate). The lines are our own, on its beats.

    /// <summary>
    /// <c>sPkblToEscapeFactor</c>: what a Pokéblock takes off the escape factor, by how many have been thrown (the count
    /// raised before it is read, so 1 to 3) and how the Pokémon took it (curious, enthralled, ignoring it).
    /// </summary>
    internal static readonly int[,] PokeblockToEscape = { { 0, 0, 0 }, { 3, 5, 0 }, { 2, 3, 0 }, { 1, 2, 0 }, { 1, 1, 0 } };

    /// <summary><c>sGoNearCounterToCatchFactor</c> and <c>sGoNearCounterToEscapeFactor</c>, by GO NEAR's count before it is raised.</summary>
    internal static readonly int[] GoNearToCatch = { 4, 3, 2, 1 }, GoNearToEscape = { 4, 4, 4, 4 };

    /// <summary>
    /// The escape factor as the turn began: Emerald's wild Pokémon decides whether it runs as the turn's actions are
    /// chosen (its AI's <c>if_random_safari_flee</c>), before the player's throw or step changes the factor.
    /// </summary>
    private int safariFleeFactor;

    /// <summary>A Safari Ball's catch rate in Hoenn's Safari Zone: <c>safariCatchFactor * 1275 / 100</c>, kept to a byte.</summary>
    internal int EmeraldSafariRate => Math.Min(255, SafariCatchFactor * 1275 / 100);

    /// <summary>
    /// How a nature takes a Pokéblock in battle (<c>PokeblockAction_UseInBattle</c>): 0 curious (no gain either way), 1
    /// enthralled (liked), 2 ignoring it (disliked).
    /// </summary>
    internal static int PokeblockReaction(Data.Nature nature, Pokeblock block)
    {
        int gain = Pokeblocks.Gain(nature, block);
        return gain == 0 ? 0 : gain > 0 ? 1 : 2;
    }

    /// <summary>
    /// <c>HandleAction_ThrowPokeblock</c>: the throw count raised (to 3 at most), and while the escape factor is over 1 it
    /// drops by the table's step for that count and the Pokémon's reaction, or to 1 if the step is larger. Emerald's own
    /// bug is kept: a factor equal to the step drops to 0, and a Pokémon at 0 never runs (the "Pokéblock throw glitch").
    /// </summary>
    private void ThrowPokeblock(Battler thrower, Pokeblock block)
    {
        var foe = EnemySlots[0];
        if (foe.Pokemon is not { } p) return;
        int reaction = PokeblockReaction(p.Nature, block);
        PokeblocksThrown++;
        if (SafariPokeblockCount < 3) SafariPokeblockCount++;
        if (SafariEscapeFactor > 1)
        {
            int step = PokeblockToEscape[SafariPokeblockCount, reaction];
            SafariEscapeFactor = SafariEscapeFactor < step ? 1 : SafariEscapeFactor - step;
        }
        string name = block.Name;
        Say(Text.Of("battle.threwAPokeblockAt", ("player", playerName), ("block", name), ("pokemon", p.DisplayName))).With(Moment(foe, BattleMoment.Happy));
        Say(reaction switch
        {
            0 => Text.Of("battle.pokeblockCurious", ("pokemon", p.DisplayName), ("block", name)),
            1 => Text.Of("battle.pokeblockEnthralled", ("pokemon", p.DisplayName), ("block", name)),
            _ => Text.Of("battle.pokeblockIgnored", ("pokemon", p.DisplayName), ("block", name))
        }).With(Moment(foe, reaction == 2 ? BattleMoment.Angry : BattleMoment.Eating));
    }

    /// <summary>
    /// <c>HandleAction_GoNear</c>: the catch factor up by 4, 3, 2 and then 1 each time (20 at most), the escape factor up
    /// by 4 (20 at most); after the third step closer there is no getting closer, though both still rise.
    /// </summary>
    private void GoNear(Battler mover)
    {
        var foe = EnemySlots[0];
        if (foe.Pokemon is not { } p) return;
        SafariCatchFactor = Math.Min(20, SafariCatchFactor + GoNearToCatch[SafariGoNearCount]);
        SafariEscapeFactor = Math.Min(20, SafariEscapeFactor + GoNearToEscape[SafariGoNearCount]);
        if (SafariGoNearCount < 3)
        {
            SafariGoNearCount++;
            Say(Text.Of("battle.creptCloser", ("player", playerName), ("pokemon", p.DisplayName)));
        }
        else Say(Text.Of("battle.cantGetCloser", ("player", playerName), ("pokemon", p.DisplayName)));
    }

    /// <summary>
    /// <c>Cmd_if_random_safari_flee</c>: Emerald's Pokémon runs when a roll of 100 comes out under five times the escape
    /// factor the turn began with; otherwise it watches carefully. The factor the next turn begins with is taken now.
    /// </summary>
    private void EmeraldSafariPokemonActs(Battler foe, Pokemon p)
    {
        int rate = safariFleeFactor * 5;
        safariFleeFactor = SafariEscapeFactor;
        if (rng.Roll(RollKind.Safari, 100) < rate)
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
        Say(Text.Of(EmeraldSafari ? "battle.safariZoneOver" : "battle.thatTheEndOf"));
        End(BattleResult.PlayerRan);
    }
}
