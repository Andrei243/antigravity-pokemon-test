using System;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Battle;

/// <summary>
/// A thrown ball, by Platinum's own arithmetic (<see cref="Formulas.CatchRate"/>, <see cref="Formulas.ShakeRate"/>;
/// the decompilation's <c>BattleScript_CalcCatchShakes</c>): a catch rate from the species, the ball, the foe's HP
/// and its condition, then four rolls against a shake rate worked out from it. The ball holds if all four do. Under the
/// modern rules a throw may be a critical capture first (plan 40 · GE5, <see cref="Ruleset.CriticalCaptures"/>), which
/// is one roll against the shake rate where a plain throw is four.
/// </summary>
public static partial class CatchCalculator
{
    public struct CatchResult
    {
        public bool IsCaught;
        public int Shakes; // 0, 1, 2, 3, or 4 (4 = caught)

        /// <summary>The throw was a critical capture (plan 40 · GE5): one shake check, so it shook once and held (4) or broke free (1).</summary>
        public bool Critical;
    }

    /// <summary>A ball thrown on a battle's first turn, by day and on land: for callers with no battle round them.</summary>
    /// <param name="rng">The battle's own generator; left out, the rolls are the game's shared chance.</param>
    public static CatchResult AttemptCatch(Pokemon wildPokemon, ItemData ball, Random? rng = null) =>
        Throw(wildPokemon, ball, rng ?? Core.Dice.Shared, turn: 0, new BattleConditions());

    /// <summary>
    /// The Great Marsh's share of a species' catch rate at each of its thirteen stages (<c>sSafariCatchRate</c>):
    /// a quarter at 0, the whole at 6 where every encounter starts, four times at 12.
    /// </summary>
    public static readonly (int Num, int Den)[] SafariStages =
        { (10, 40), (10, 35), (10, 30), (10, 25), (10, 20), (10, 15), (10, 10), (15, 10), (20, 10), (25, 10), (30, 10), (35, 10), (40, 10) };

    /// <summary>How many times the ball shakes: 4 means it held (<see cref="Throw"/>, of which this is the count).</summary>
    public static int Shakes(Pokemon wild, ItemData ball, Random rng, int turn, BattleConditions conditions, int safariStage = 6,
        Ruleset? rules = null, Pokemon? mine = null, int? safariRate = null) =>
        Throw(wild, ball, rng, turn, conditions, safariStage, rules, mine, safariRate).Shakes;

    /// <summary>
    /// A thrown ball: how many times it shakes (4 means it held) and whether it was a critical capture.
    /// <para>
    /// Under rules with <see cref="Ruleset.CriticalCaptures"/> one roll of 256 comes first (<see cref="RollKind.CriticalCapture"/>),
    /// against a chance made of the catch rate, the species caught and the Catching Charm (<see cref="Formulas.CriticalCapture"/>,
    /// from <see cref="BattleConditions.CaughtSpecies"/> and <see cref="BattleConditions.CatchingCharm"/>); it is rolled only
    /// where that chance is above nothing, so a player with thirty species caught or fewer throws as before. A critical
    /// capture makes one shake check against the same rate: held, it shook once and holds (4); broken, it shook once (1). A
    /// rate of 255 or more holds either way, critical or not, as the later games' does.
    /// </para>
    /// </summary>
    /// <param name="turn">Turns of the battle already played (the Timer and Quick Balls count them).</param>
    /// <param name="safariStage">For a Safari Ball, the Great Marsh's stage the species' catch rate is taken at.</param>
    /// <param name="rules">The battle's rules: the later games' balls and bonuses are theirs (plan 06 · R28a), and critical captures (plan 40 · GE5); left out, Platinum's.</param>
    /// <param name="mine">The player's Pokémon on the field, which a Level Ball and a Love Ball go by.</param>
    /// <param name="safariRate">For a Safari Ball in Hoenn's Safari Zone, the catch rate Emerald's catch factor gives in place of the species' (plan 21 · H11, part e).</param>
    public static CatchResult Throw(Pokemon wild, ItemData ball, Random rng, int turn, BattleConditions conditions, int safariStage = 6,
        Ruleset? rules = null, Pokemon? mine = null, int? safariRate = null, bool heldDay = false)
    {
        // A Master Ball holds whatever the rolls would have said (and an Origin Ball and a Park Ball: plan 06 · R28a)
        if (ball.EffectValue >= 9999 || (rules?.LaterBalls == true && AlwaysHolds(ball.Name))) return Held(critical: false);

        int speciesRate = wild.CatchRate;
        if (ball.Name == "Safari Ball")
            speciesRate = safariRate ?? speciesRate * SafariStages[safariStage].Num / SafariStages[safariStage].Den;
        bool night = conditions.Night && !heldDay;
        int tenths = Formulas.BallTenths(ball.Name, wild, turn, conditions, night);
        if (rules?.LaterBalls == true)
        {
            speciesRate = HeavyBallRate(ball.Name, wild, speciesRate);
            tenths = LaterBallTenths(ball.Name, wild, turn, conditions, mine, night);
        }
        // The Catching Power in force as the battle began (plan 36 · TR7) raises the species' rate, at most to 255
        if (conditions.Powers?.Catch is > 0 and var power) speciesRate = Math.Min(255, Models.PowerSet.Raise(speciesRate, power));
        int rate = Formulas.CatchRate(speciesRate, tenths, wild.MaxHP, wild.CurrentHP, wild.Status);

        // A critical capture is decided before the shakes (plan 40 · GE5)
        bool critical = false;
        if (rules?.CriticalCaptures == true && Formulas.CriticalCapture(rate, conditions.CaughtSpecies, conditions.CatchingCharm) is var chance && chance > 0)
            critical = rng.Roll(RollKind.CriticalCapture, 256) < chance;

        if (rate >= 255) return Held(critical);

        int holds = Formulas.ShakeRate(rate);
        if (critical)
        {
            bool held = rng.Roll(RollKind.CatchShake, 65536) < holds;
            return held ? Held(critical: true) : new CatchResult { Shakes = 1, Critical = true };
        }
        int shakes = 0;
        while (shakes < 4 && rng.Roll(RollKind.CatchShake, 65536) < holds) shakes++;
        return new CatchResult { IsCaught = shakes == 4, Shakes = shakes };
    }

    private static CatchResult Held(bool critical) => new() { IsCaught = true, Shakes = 4, Critical = critical };
}
