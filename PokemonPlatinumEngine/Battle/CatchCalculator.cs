using System;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Battle;

/// <summary>
/// A thrown ball, by Platinum's own arithmetic (<see cref="Formulas.CatchRate"/>, <see cref="Formulas.ShakeRate"/>;
/// the decompilation's <c>BattleScript_CalcCatchShakes</c>): a catch rate from the species, the ball, the foe's HP
/// and its condition, then four rolls against a shake rate worked out from it. The ball holds if all four do.
/// </summary>
public static class CatchCalculator
{
    public struct CatchResult
    {
        public bool IsCaught;
        public int Shakes; // 0, 1, 2, 3, or 4 (4 = caught)
    }

    /// <summary>A ball thrown on a battle's first turn, by day and on land: for callers with no battle round them.</summary>
    /// <param name="rng">The battle's own generator; left out, the rolls are the game's shared chance.</param>
    public static CatchResult AttemptCatch(Pokemon wildPokemon, ItemData ball, Random? rng = null)
    {
        int shakes = Shakes(wildPokemon, ball, rng ?? Core.Dice.Shared, turn: 0, new BattleConditions());
        return new CatchResult { IsCaught = shakes == 4, Shakes = shakes };
    }

    /// <summary>
    /// The Great Marsh's share of a species' catch rate at each of its thirteen stages (<c>sSafariCatchRate</c>):
    /// a quarter at 0, the whole at 6 where every encounter starts, four times at 12.
    /// </summary>
    public static readonly (int Num, int Den)[] SafariStages =
        { (10, 40), (10, 35), (10, 30), (10, 25), (10, 20), (10, 15), (10, 10), (15, 10), (20, 10), (25, 10), (30, 10), (35, 10), (40, 10) };

    /// <summary>How many times the ball shakes: 4 means it held.</summary>
    /// <param name="turn">Turns of the battle already played (the Timer and Quick Balls count them).</param>
    /// <param name="safariStage">For a Safari Ball, the Great Marsh's stage the species' catch rate is taken at.</param>
    public static int Shakes(Pokemon wild, ItemData ball, Random rng, int turn, BattleConditions conditions, int safariStage = 6)
    {
        // A Master Ball holds whatever the rolls would have said
        if (ball.EffectValue >= 9999) return 4;

        int speciesRate = wild.CatchRate;
        if (ball.Name == "Safari Ball") speciesRate = speciesRate * SafariStages[safariStage].Num / SafariStages[safariStage].Den;
        int rate = Formulas.CatchRate(speciesRate, Formulas.BallTenths(ball.Name, wild, turn, conditions),
            wild.MaxHP, wild.CurrentHP, wild.Status);
        if (rate >= 255) return 4;

        int holds = Formulas.ShakeRate(rate);
        int shakes = 0;
        while (shakes < 4 && rng.Roll(RollKind.CatchShake, 65536) < holds) shakes++;
        return shakes;
    }
}
