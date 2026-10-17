using System.Linq;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using static PokemonPlatinumTests.Scenario;

namespace PokemonPlatinumTests;

/// <summary>
/// The damage formula of generation 5 on (plan 06 · R19c, <see cref="Ruleset.ModernDamageFormula"/>): its arithmetic
/// against numbers worked out by hand from Pokémon Showdown's (<c>pokeRound</c>, <c>chainMods</c>,
/// <c>getFinalDamage</c>), and the order it puts Platinum's bonuses in. The oracle of plan 24 · X6
/// (<c>OracleTests</c>) holds the whole of it to Showdown's calculator.
/// </summary>
public class ModernFormulaTests
{
    [Fact]
    public void OnlyTheModernRulesTakeTheLaterFormula()
    {
        Assert.False(Ruleset.Platinum.ModernDamageFormula);
        Assert.True(Ruleset.Modern.ModernDamageFormula);
    }

    [Fact]
    public void AHalfIsRoundedDownAndAnythingOverItUp()
    {
        Assert.Equal(1, Formulas.RoundHalfDown(6144));        // 1.5 → 1
        Assert.Equal(2, Formulas.RoundHalfDown(6145));        // a hair over 1.5 → 2
        Assert.Equal(3, Formulas.RoundHalfDown(3 * 4096 + 2048));
        Assert.Equal(75, Formulas.Modify(150, 2048));         // a screen's half of 150
        Assert.Equal(112, Formulas.Modify(150, 3072));        // a spread hit's 112.5 goes down
    }

    [Fact]
    public void BonusesChainInFourThousandNinetySixthsRoundedHalfUp()
    {
        // 4096 × 6144 → 6144; 6144 × 4915 = 30,197,760, + 2048 = 30,199,808, >> 12 = 7373
        Assert.Equal(7373, Formulas.Chain(new[] { 6144, 4915 }, 41, 131072));
        Assert.Equal(131072, Formulas.Chain(Enumerable.Repeat(8192, 8), 41, 131072));
        Assert.Equal(5324, Formulas.In4096ths(1.3f));          // a Life Orb's
        Assert.Equal(4915, Formulas.In4096ths(1.2f));          // a type's item
        Assert.Equal(3072, Formulas.In4096ths(0.75f));         // Filter's
    }

    [Fact]
    public void TheBurnComesAfterTheTypeChartAndTheScreenLast()
    {
        // 100 at its strongest roll; × 6144 = 150; × 2 = 300; burned: 150; behind a screen, × 2048 / 4096 = 75
        Assert.Equal(75, Formulas.ModernFinalDamage(100, 0, 6144, 2f, burned: true, finalMod: 2048));
        // the weakest roll: 100 × 85 / 100 = 85; × 6144 / 4096 = 127.5, rounded half down to 127; × 2 = 254; 127; 63.5 → 63
        Assert.Equal(63, Formulas.ModernFinalDamage(100, 15, 6144, 2f, burned: true, finalMod: 2048));
        // a hit that comes to nothing still does one
        Assert.Equal(1, Formulas.ModernFinalDamage(3, 15, 4096, 0.25f, burned: true, finalMod: 2048));
    }

    [Fact]
    public void UnderTheModernRulesReflectIsAFinalModifier()
    {
        var mine = Mon("Machamp", 50, "Cross Chop");
        var foe = Mon("Snorlax", 50, "Tackle");
        var move = new Move(MoveDatabase.Get("Cross Chop")!.Under(Ruleset.Modern));
        for (int roll = 0; roll < 16; roll++)
        {
            int open = Hit(mine, foe, move, roll, reflect: false);
            int behind = Hit(mine, foe, move, roll, reflect: true);
            Assert.Equal(Formulas.Modify(open, 2048) is var half && half < 1 ? 1 : half, behind);
        }
    }

    private static int Hit(Pokemon mine, Pokemon foe, Move move, int roll, bool reflect)
    {
        var field = new FieldState();
        var a = new Battler(BattleSide.Player, 0) { Pokemon = mine, Field = field };
        var d = new Battler(BattleSide.Enemy, 0) { Pokemon = foe, Field = field };
        field.Battlers = () => new[] { a, d };
        field.Standing = _ => 1;
        if (reflect) field.Side(BattleSide.Enemy).ReflectTurns = 5;
        var rng = new BattleRandom(1).Force(RollKind.Critical, 1).Force(RollKind.Damage, roll);
        return DamageCalculator.Calculate(a, d, move, rng, spread: false, rules: Ruleset.Modern).Damage;
    }
}
