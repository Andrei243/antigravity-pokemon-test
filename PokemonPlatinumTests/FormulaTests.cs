using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using static PokemonPlatinumTests.Scenario;

namespace PokemonPlatinumTests;

/// <summary>
/// Platinum's arithmetic (plan 06 · R2), each function against numbers worked out by hand from the
/// decompilation's code: whole numbers, rounded down after every step, in the original's order.
/// </summary>
public class FormulaTests
{
    [Theory]
    [InlineData(100, 0, 100)]
    [InlineData(100, 1, 150)]
    [InlineData(100, -1, 66)]
    [InlineData(101, -1, 67)]
    [InlineData(135, 2, 270)]
    [InlineData(140, -1, 93)]
    [InlineData(99, 6, 396)]
    [InlineData(99, -6, 24)]
    [InlineData(99, 9, 396)] // stages stop at six
    public void AStatByItsStage(int stat, int stage, int expected) => Assert.Equal(expected, Formulas.Staged(stat, stage));

    [Theory]
    [InlineData(60, 1.2f, 72)]
    [InlineData(55, 1.1f, 60)]   // 60.5
    [InlineData(10, 0.9f, 9)]    // a factor that a fraction of two would bring out as 8
    [InlineData(47, 1.5f, 70)]
    [InlineData(47, 2.25f, 105)]
    [InlineData(33, 0.75f, 24)]
    [InlineData(7, 1.3f, 9)]
    [InlineData(100, 1f, 100)]
    public void ABonusIsHundredthsRoundedDown(int value, float factor, int expected) => Assert.Equal(expected, Formulas.Scale(value, factor));

    [Fact]
    public void ADivisionNeverRoundsAHitAwayToNothing()
    {
        Assert.Equal(4, Formulas.Divide(40, 10));
        Assert.Equal(1, Formulas.Divide(15, 10));
        Assert.Equal(1, Formulas.Divide(5, 10));
        Assert.Equal(0, Formulas.Divide(0, 10));
    }

    [Fact]
    public void TheHeartOfTheDamageFormula()
    {
        // 135 × 80 × 22 / 105 / 50 = 45, + 2
        Assert.Equal(47, Formulas.BaseDamage(50, 80, 135, 105, burned: false, spread: false));
        // A burn halves it before the 2 is added: 22 + 2
        Assert.Equal(24, Formulas.BaseDamage(50, 80, 135, 105, burned: true, spread: false));
        // Three quarters for a move that hits several: 33 + 2; and both: 22 × 3 / 4 = 16, + 2
        Assert.Equal(35, Formulas.BaseDamage(50, 80, 135, 105, burned: false, spread: true));
        Assert.Equal(18, Formulas.BaseDamage(50, 80, 135, 105, burned: true, spread: true));

        // The roll takes 0 to 15 hundredths off, and leaves at least one
        Assert.Equal(47, Formulas.Variance(47, 0));
        Assert.Equal(39, Formulas.Variance(47, 15));
        Assert.Equal(1, Formulas.Variance(1, 15));
        Assert.Equal(0, Formulas.Variance(0, 5));
    }

    [Theory]
    [InlineData(100, 0, 0, 100)]
    [InlineData(95, 0, 0, 95)]
    [InlineData(95, -1, 0, 71)]
    [InlineData(100, 1, 0, 133)]
    [InlineData(100, -6, 0, 33)]
    [InlineData(100, 0, 6, 33)]
    [InlineData(70, 2, 0, 116)]
    [InlineData(80, 3, 0, 160)]
    [InlineData(80, 5, 0, 212)]
    [InlineData(100, 6, -6, 300)]
    [InlineData(100, -2, -2, 100)]
    public void AMovesHitRateByTheStages(int accuracy, int accuracyStage, int evasionStage, int expected) =>
        Assert.Equal(expected, Formulas.HitRate(accuracy, accuracyStage, evasionStage));

    [Fact]
    public void GettingAwayIsSpeedAgainstSpeedInOneByte()
    {
        // 60 × 128 / 83 = 92
        Assert.True(Formulas.Escapes(60, 83, 0, 91));
        Assert.False(Formulas.Escapes(60, 83, 0, 92));
        Assert.True(Formulas.Escapes(60, 83, 1, 121));
        Assert.False(Formulas.Escapes(60, 83, 1, 122));
        Assert.True(Formulas.Escapes(125, 83, 0, 255));
        Assert.True(Formulas.Escapes(83, 83, 0, 255));

        // The original keeps the number in one byte, so many failed tries can wrap it round: 64 + 7 × 30 = 274 is 18
        Assert.True(Formulas.Escapes(1, 2, 7, 17));
        Assert.False(Formulas.Escapes(1, 2, 7, 18));
    }

    [Fact]
    public void ExpIsBaseTimesLevelOverSevenShared()
    {
        // 58 × 40 / 7 = 331
        Assert.Equal((331, 0), Formulas.ExpShares(58, 40, fought: 1, holders: 0));
        Assert.Equal((165, 0), Formulas.ExpShares(58, 40, fought: 2, holders: 0));
        // With an Exp. Share half goes to each group: 165, then by its number
        Assert.Equal((165, 165), Formulas.ExpShares(58, 40, fought: 1, holders: 1));
        Assert.Equal((82, 55), Formulas.ExpShares(58, 40, fought: 2, holders: 3));
        Assert.Equal((0, 165), Formulas.ExpShares(58, 40, fought: 0, holders: 1));
        // Nobody's share is less than one
        Assert.Equal((1, 0), Formulas.ExpShares(20, 1, fought: 3, holders: 0));

        // Half as much again with a Lucky Egg, and again from a trainer's Pokémon, one after the other
        Assert.Equal(165, Formulas.ExpFor(165, luckyEgg: false, trainerBattle: false));
        Assert.Equal(247, Formulas.ExpFor(165, luckyEgg: true, trainerBattle: false));
        Assert.Equal(247, Formulas.ExpFor(165, luckyEgg: false, trainerBattle: true));
        Assert.Equal(370, Formulas.ExpFor(165, luckyEgg: true, trainerBattle: true));
        // Another trainer's Pokémon, last: 370 × 150 / 100 = 555; from a game in another language 370 × 170 / 100 = 629
        Assert.Equal(555, Formulas.ExpFor(165, luckyEgg: true, trainerBattle: true, traded: true));
        Assert.Equal(629, Formulas.ExpFor(165, luckyEgg: true, trainerBattle: true, traded: true, foreign: true));
        // The language counts only for a traded Pokémon (BtlCmd_CalcExpGain asks it after the OT check)
        Assert.Equal(370, Formulas.ExpFor(165, luckyEgg: true, trainerBattle: true, foreign: true));
    }

    [Fact]
    public void TheCatchRateAndWhatEachShakeIsRolledAgainst()
    {
        // Species rate × ball / 10 × (3 × max − 2 × HP) / (3 × max)
        Assert.Equal(85, Formulas.CatchRate(255, 10, 30, 30, StatusCondition.None));
        Assert.Equal(170, Formulas.CatchRate(255, 10, 30, 30, StatusCondition.Sleep));
        Assert.Equal(170, Formulas.CatchRate(255, 10, 30, 30, StatusCondition.Freeze));
        Assert.Equal(127, Formulas.CatchRate(255, 10, 30, 30, StatusCondition.Paralyze));
        Assert.Equal(127, Formulas.CatchRate(255, 10, 30, 30, StatusCondition.Toxic));
        Assert.Equal(249, Formulas.CatchRate(255, 10, 30, 1, StatusCondition.None));
        Assert.Equal(89, Formulas.CatchRate(45, 20, 100, 1, StatusCondition.None));
        Assert.Equal(1, Formulas.CatchRate(3, 10, 100, 100, StatusCondition.None));

        // 16,711,680 / rate, its whole square root twice, then 1,048,560 over that
        Assert.Equal(49931, Formulas.ShakeRate(85));   // 196,608 → 443 → 21
        Assert.Equal(61680, Formulas.ShakeRate(170));  // 98,304 → 313 → 17
        Assert.Equal(65535, Formulas.ShakeRate(249));  // 67,115 → 259 → 16
        Assert.Equal(16643, Formulas.ShakeRate(1));    // 16,711,680 → 4,087 → 63
    }

    [Fact]
    public void EachBallHasItsStrength()
    {
        var water = Mon("Blastoise", 50);
        var plain = Mon("Machamp", 50);
        var young = Mon("Bidoof", 5);
        var day = new BattleConditions();
        int Tenths(string ball, Pokemon target, int turn = 0, BattleConditions? conditions = null) => Formulas.BallTenths(ball, target, turn, conditions ?? day);

        Assert.Equal(10, Tenths("Poké Ball", plain));
        Assert.Equal(15, Tenths("Great Ball", plain));
        Assert.Equal(20, Tenths("Ultra Ball", plain));
        Assert.Equal(10, Tenths("Luxury Ball", plain));

        Assert.Equal(30, Tenths("Net Ball", water));
        Assert.Equal(30, Tenths("Net Ball", Mon("Kricketot", 5)));
        Assert.Equal(10, Tenths("Net Ball", plain));

        Assert.Equal(35, Tenths("Nest Ball", young));
        Assert.Equal(10, Tenths("Nest Ball", Mon("Bidoof", 35)));
        Assert.Equal(10, Tenths("Nest Ball", Mon("Bidoof", 40)));

        Assert.Equal(40, Tenths("Quick Ball", plain, turn: 0));
        Assert.Equal(10, Tenths("Quick Ball", plain, turn: 1));
        Assert.Equal(10, Tenths("Timer Ball", plain, turn: 0));
        Assert.Equal(22, Tenths("Timer Ball", plain, turn: 12));
        Assert.Equal(40, Tenths("Timer Ball", plain, turn: 99));

        Assert.Equal(10, Tenths("Dusk Ball", plain));
        Assert.Equal(35, Tenths("Dusk Ball", plain, conditions: new BattleConditions { Night = true }));
        Assert.Equal(35, Tenths("Dusk Ball", plain, conditions: new BattleConditions { Terrain = BattleTerrain.Cave }));
        Assert.Equal(10, Tenths("Dive Ball", water));
        Assert.Equal(35, Tenths("Dive Ball", water, conditions: new BattleConditions { Terrain = BattleTerrain.Water }));
        Assert.Equal(10, Tenths("Repeat Ball", plain));
        Assert.Equal(30, Tenths("Repeat Ball", plain, conditions: new BattleConditions { HasCaught = s => s.Name == "Machamp" }));

        // A Master Ball needs no arithmetic, and a ball that is certain makes no rolls
        var rolls = new BattleRandom(1);
        Assert.Equal(4, CatchCalculator.Shakes(plain, ItemDatabase.Get("Master Ball")!, rolls, 0, day));
        var sleeping = Mon("Bidoof", 5);
        sleeping.CurrentHP = 1;
        sleeping.Status = StatusCondition.Sleep;
        Assert.Equal(4, CatchCalculator.Shakes(sleeping, ItemDatabase.Get("Poké Ball")!, rolls, 0, day));
        Assert.Equal(0, rolls.Drawn);
    }

    // ------------------------------------------------------------------ the order of a hit's steps

    [Fact]
    public void ALifeOrbsBonusComesBeforeTheRoll()
    {
        // Machamp's Strength on Blastoise is 47: × 130 / 100 = 61. The weakest roll then takes 15 hundredths off
        // that: 51. (The bonus after the roll would be 39 × 1.3 = 50.)
        var machamp = Mon("Machamp", 50, "Strength");
        machamp.HeldItem = ItemDatabase.Get("Life Orb");
        var blastoise = Mon("Blastoise", 50);
        Assert.Equal(61, Damage(machamp, blastoise, "Strength"));
        Assert.Equal(51, Damage(machamp, blastoise, "Strength", Steady().Force(RollKind.Damage, 15)));
    }

    [Fact]
    public void AHitThatIsntStoppedIsNeverRoundedDownToNothing()
    {
        // Bidoof's Tackle on Aggron (Steel and Rock): 9 × 35 × 4 / 185 / 50 = 0, + 2; its own type: 3; then × 5 / 10
        // twice, which would be 1 and then nothing
        var bidoof = Mon("Bidoof", 5, "Tackle");
        var aggron = Mon("Aggron", 50);
        Assert.Equal((9, 185), (bidoof.Attack, aggron.Defense));
        var hit = DamageCalculator.CalculateDamage(bidoof, aggron, bidoof.Moves[0], Steady(), Ruleset.Platinum);
        Assert.Equal(1, hit.Damage);
        Assert.Equal(0.25f, hit.TypeMultiplier);
        Assert.True(hit.IsNotVeryEffective);
    }

    [Fact]
    public void StruggleHasNoTypeButCanBeACriticalHit()
    {
        var struggle = new Move(BattleCore.StruggleData);
        int Hit(Pokemon user, Pokemon target, BattleRandom rolls) => DamageCalculator.CalculateDamage(user, target, struggle, rolls, Ruleset.Platinum).Damage;

        // 135 × 50 × 22 / 105 / 50 = 28, + 2
        var machamp = Mon("Machamp", 50);
        var blastoise = Mon("Blastoise", 50);
        Assert.Equal(30, Hit(machamp, blastoise, Steady()));
        Assert.Equal(60, Hit(machamp, blastoise, Steady().Force(RollKind.Critical, 0)));

        // No bonus for a Normal type using it (115 × 50 × 22 / 105 / 50 = 24, + 2), and a Ghost is hit like anyone
        Assert.Equal(26, Hit(Mon("Snorlax", 50), blastoise, Steady()));
        Assert.True(Hit(machamp, Mon("Gengar", 50), Steady()) > 1);
    }

    [Fact]
    public void AnItemThatLowersAccuracyTakesItsTenthOffTheHitRate()
    {
        // Tackle's 95 with Bright Powder across the field: 95 × 90 / 100 = 85
        List<string> With(int roll)
        {
            var foe = Mon("Bidoof", 20);
            foe.HeldItem = ItemDatabase.Get("Bright Powder");
            return Turn(Battle(Mon("Bidoof", 20, "Tackle"), foe, Steady().Force(RollKind.Accuracy, roll)));
        }
        Assert.DoesNotContain("Bidoof's attack missed!", With(84));
        Assert.Contains("Bidoof's attack missed!", With(85));
    }

    [Fact]
    public void ALuckyEggAndATrainersPokemonEachAddHalf()
    {
        // Through a battle: a trainer's level 40 Bidoof, beaten by a Pokémon holding a Lucky Egg
        var machamp = Mon("Machamp", 50, "Strength");
        machamp.HeldItem = ItemDatabase.Get("Lucky Egg");
        var foe = Mon("Bidoof", 40);
        var party = new Party();
        party.Add(machamp);
        var theirs = new Party();
        theirs.Add(foe);
        var core = new BattleCore(new CoreSetup
        {
            PlayerParty = party, Trainers = new List<Trainer> { new() { Name = "Tester", TrainerClass = "Youngster", Party = theirs } },
            Random = Steady(), Rules = Ruleset.Platinum
        });
        core.Start();
        core.Submit(BattleChoice.Fight(new Place(BattleSide.Player, 0), 0));

        int share = foe.Species.BaseExpYield * 40 / 7;
        var lines = core.TakeLog().OfType<Said>().Select(s => s.Text).ToList();
        Assert.Contains($"Machamp gained {share * 150 / 100 * 150 / 100} EXP. Points!", lines);
        Assert.Equal(BattleResult.PlayerVictory, core.Result);
    }
}
