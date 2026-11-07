using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using Xunit;
using static PokemonPlatinumTests.CoreScenario;
using static PokemonPlatinumTests.Scenario;

namespace PokemonPlatinumTests;

/// <summary>
/// Critical captures (plan 40 · GE5, decision 5; <c>docs/mechanics/rulings.md</c>, "The charms, critical captures and EXP
/// for a catch"): under the modern rules a throw draws one roll of 256 first, against the catch rate up to 255 times the
/// caught count's share and twice with the Catching Charm, over 6; a critical capture is one shake check. The numbers
/// are worked out by hand below, from the later games' formula as The Cave of Dragonflies gives it for Generations 6 to 9.
/// </summary>
public class CriticalCaptureTests
{
    private static readonly ItemData PokeBall = ItemDatabase.Get("Poké Ball")!;

    [Fact]
    public void TheCaughtCountGivesTheChanceItsShare()
    {
        // None to 30 species caught, a half to 150, the whole to 300, one and a half to 450, twice to 600, two and a half beyond
        var shares = new (int Caught, int Tenths)[] { (0, 0), (30, 0), (31, 5), (150, 5), (151, 10), (300, 10), (301, 15), (450, 15), (451, 20), (600, 20), (601, 25), (1025, 25) };
        foreach (var (caught, tenths) in shares) Assert.Equal(tenths, Formulas.CriticalCaptureTenths(caught));
    }

    [Fact]
    public void TheChanceIsTheCatchRateTimesTheShareOverSixRoundedDownOnceAtTheEnd()
    {
        // 100 × 1 / 6 = 16.67 → 16; with the Catching Charm 100 × 1 × 2 / 6 = 33.33 → 33, which is not twice 16: the charm
        // doubles before the division
        Assert.Equal(16, Formulas.CriticalCapture(100, 200, catchingCharm: false));
        Assert.Equal(33, Formulas.CriticalCapture(100, 200, catchingCharm: true));
        // The rate is held to 255: 255 × 2.5 / 6 = 106.25 → 106, and 255 × 2.5 × 2 / 6 = 212.5 → 212
        Assert.Equal(106, Formulas.CriticalCapture(300, 601, catchingCharm: false));
        Assert.Equal(212, Formulas.CriticalCapture(300, 601, catchingCharm: true));
        // 15 × 0.5 / 6 = 1.25 → 1; and with thirty species caught or fewer, nothing, charm or not
        Assert.Equal(1, Formulas.CriticalCapture(15, 100, catchingCharm: false));
        Assert.Equal(0, Formulas.CriticalCapture(255, 30, catchingCharm: true));
    }

    /// <summary>A Machamp at full HP: its catch rate 45 in a Poké Ball comes to 45 × (3 − 2) / 3 = 15, well short of a sure catch.</summary>
    private static Pokemon Hard() => Mon("Machamp", 30);

    private static BattleConditions Caught(int species, bool charm = false) => new() { CaughtSpecies = species, CatchingCharm = charm };

    [Fact]
    public void ACriticalCaptureIsOneShakeCheckHeldOrBroken()
    {
        // The Machamp's rate of 15 with 601 species caught and the charm: 15 × 2.5 × 2 / 6 = 12.5 → 12, so a roll of 0
        // is critical. The shakes' rolls are 0 then 65535: a plain throw would hold once and break on the second
        Assert.Equal(15, Formulas.CatchRate(45, 10, Hard().MaxHP, Hard().MaxHP, StatusCondition.None));
        Assert.Equal(12, Formulas.CriticalCapture(15, 601, catchingCharm: true));
        var held = CatchCalculator.Throw(Hard(), PokeBall, new BattleRandom(1).Force(RollKind.CriticalCapture, 0).Force(RollKind.CatchShake, 0, 65535),
            0, Caught(601, charm: true), rules: Ruleset.Modern);
        Assert.Equal((true, 4, true), (held.Critical, held.Shakes, held.IsCaught));

        // Its one check fails: it shook once and broke free
        var broken = CatchCalculator.Throw(Hard(), PokeBall, new BattleRandom(1).Force(RollKind.CriticalCapture, 0).Force(RollKind.CatchShake, 65535),
            0, Caught(601, charm: true), rules: Ruleset.Modern);
        Assert.Equal((true, 1, false), (broken.Critical, broken.Shakes, broken.IsCaught));

        // A roll of 12 is not under 12: a plain throw, four checks, the second of which breaks it
        var plain = CatchCalculator.Throw(Hard(), PokeBall, new BattleRandom(1).Force(RollKind.CriticalCapture, 12).Force(RollKind.CatchShake, 0, 65535),
            0, Caught(601, charm: true), rules: Ruleset.Modern);
        Assert.Equal((false, 1, false), (plain.Critical, plain.Shakes, plain.IsCaught));
    }

    [Fact]
    public void TheCatchingCharmMakesACriticalOfARollThatWasntOne()
    {
        // 601 caught: 15 × 2.5 / 6 = 6.25 → 6 without the charm, 12 with it; a roll of 8 is critical only with the charm
        var rolls = () => new BattleRandom(1).Force(RollKind.CriticalCapture, 8).Force(RollKind.CatchShake, 0);
        Assert.False(CatchCalculator.Throw(Hard(), PokeBall, rolls(), 0, Caught(601), rules: Ruleset.Modern).Critical);
        Assert.True(CatchCalculator.Throw(Hard(), PokeBall, rolls(), 0, Caught(601, charm: true), rules: Ruleset.Modern).Critical);
    }

    [Fact]
    public void ACriticalCaptureDrawsOneMoreRollAndOnlyWhereItCouldCome()
    {
        // The shakes fixed, so the only number drawn is the critical capture's
        static long Drawn(Ruleset rules, BattleConditions conditions)
        {
            var rng = new BattleRandom(7).Force(RollKind.CatchShake, 0);
            CatchCalculator.Throw(Hard(), PokeBall, rng, 0, conditions, rules: rules);
            return rng.Drawn;
        }
        Assert.Equal(1, Drawn(Ruleset.Modern, Caught(601)));
        // Thirty species caught: no chance, no roll; Platinum's rules: none at all
        Assert.Equal(0, Drawn(Ruleset.Modern, Caught(30, charm: true)));
        Assert.Equal(0, Drawn(Ruleset.Platinum, Caught(601, charm: true)));
        // A Master Ball makes no roll of any kind
        var master = new BattleRandom(7);
        Assert.Equal(4, CatchCalculator.Throw(Hard(), ItemDatabase.Get("Master Ball")!, master, 0, Caught(601, charm: true), rules: Ruleset.Modern).Shakes);
        Assert.Equal(0, master.Drawn);
    }

    [Fact]
    public void ASureCatchIsStillRolledForAndHoldsEitherWay()
    {
        // A Bidoof asleep at 1 HP: 255 × (3M − 2) / 3M, doubled for sleep, is far past 255, a sure catch. Its chance is the
        // rate held to 255: 255 × 2.5 / 6 = 106.25 → 106, so a roll of 105 shows a critical capture and one of 106 doesn't
        static Pokemon Sure()
        {
            var bidoof = Mon("Bidoof", 5);
            bidoof.CurrentHP = 1;
            bidoof.Status = StatusCondition.Sleep;
            return bidoof;
        }
        Assert.True(Formulas.CatchRate(255, 10, Sure().MaxHP, 1, StatusCondition.Sleep) >= 255);
        var critical = CatchCalculator.Throw(Sure(), PokeBall, new BattleRandom(1).Force(RollKind.CriticalCapture, 105), 0, Caught(601), rules: Ruleset.Modern);
        Assert.Equal((true, 4, true), (critical.Critical, critical.Shakes, critical.IsCaught));
        var plain = CatchCalculator.Throw(Sure(), PokeBall, new BattleRandom(1).Force(RollKind.CriticalCapture, 106), 0, Caught(601), rules: Ruleset.Modern);
        Assert.Equal((false, 4, true), (plain.Critical, plain.Shakes, plain.IsCaught));
    }

    private static BattleCore Thrown(Ruleset rules, BattleConditions conditions, BattleRandom rolls)
    {
        var party = new Party();
        party.Add(Mon("Bibarel", 40, "Tackle"));
        var core = new BattleCore(new CoreSetup
        {
            PlayerParty = party, WildPokemon = new List<Pokemon> { Hard() }, Random = rolls, Rules = rules, Conditions = conditions,
            PlayerController = null, EnemyController = null, PlayerName = "Lucas"
        });
        core.Start();
        core.TakeLog();
        core.Submit(BattleChoice.UseItem(Mine, "Poké Ball"), BattleChoice.Fight(Foe, 0));
        return core;
    }

    [Fact]
    public void ABattleTellsACriticalCaptureOnItsThrow()
    {
        var core = Thrown(Ruleset.Modern, Caught(601, charm: true), Steady().Force(RollKind.CriticalCapture, 0).Force(RollKind.CatchShake, 0, 65535));
        Assert.Contains(core.Log, e => e is BallThrown { Ball: "Poké Ball", Shakes: 4, Critical: true });
        Assert.Equal(BattleResult.EnemyCaught, core.Result);

        // Platinum's rules have none: the same rolls make a plain throw, which breaks on its second check
        var platinum = Thrown(Ruleset.Platinum, Caught(601, charm: true), Steady().Force(RollKind.CriticalCapture, 0).Force(RollKind.CatchShake, 0, 65535));
        Assert.Contains(platinum.Log, e => e is BallThrown { Shakes: 1, Critical: false });
        Assert.Equal(BattleResult.None, platinum.Result);
    }

    [Fact]
    public void ARecordedBattleWithACriticalCaptureReplays()
    {
        // Nothing forced: a seed whose throw is a critical capture by its own numbers (15 × 2.5 × 2 / 6 = 12 of 256 a throw,
        // the Poké Ball thrown until the Machamp is caught or the turns run out), recorded, written, read and fought again
        // (Every move a move of the data, as a recorded file keeps moves by name: the scenarios' Idle is none)
        BattleEngine? battle = null;
        for (uint seed = 1; seed < 4000 && battle == null; seed++)
        {
            var party = new Party();
            party.Add(Mon("Bibarel", 40, "Tackle"));
            var bag = new Inventory();
            bag.AddItem(PokeBall, 30);
            var engine = new BattleEngine(new BattleSetup
            {
                PlayerParty = party, Inventory = bag, Pokedex = new Pokedex(), WildPokemon = new List<Pokemon> { Mon("Machamp", 30, "Leer") },
                Random = new BattleRandom(seed), Rules = Ruleset.Modern, PlayerName = "Lucas", Conditions = Caught(601, charm: true)
            });
            Settle(engine);
            for (int turn = 0; turn < 12 && !engine.IsBattleOver; turn++)
            {
                engine.UseItem(PokeBall);
                Settle(engine);
            }
            if (engine.Core.Log.Any(e => e is BallThrown { Critical: true })) battle = engine;
        }
        Assert.NotNull(battle);
        var file = battle!.RecordFile(new DateTime(2026, 11, 7))!;
        Assert.Equal((601, true), (file.Conditions.CaughtSpecies, file.Conditions.CatchingCharm));
        var reading = BattleRecordFile.Read(file.Serialize());
        Assert.Equal(ReplayRefusal.None, reading.Refused);
        var again = reading.File!.Replay();
        Assert.Equal(battle.Result, again.Result);
        Assert.Equal(Golden.BattleText.Written(battle.Core), Golden.BattleText.Written(again));
        Assert.Contains(again.Log, e => e is BallThrown { Critical: true });
    }

    [Fact]
    public void ACriticalCaptureIsSeenShakingOnceInTheAirAndOnceOnThePlatform()
    {
        // The rattle in the air comes between the ball shutting over the foe and its drop; one wobble follows, either way
        Assert.Equal(1, BattleAnimator.BallWobbles(4, critical: true));
        Assert.Equal(1, BattleAnimator.BallWobbles(1, critical: true));
        Assert.Equal(3, BattleAnimator.BallWobbles(4));
        var order = new List<BallPhase>();
        for (float t = 0f; t < BattleAnimator.BallThrowTime(4, critical: true) + 0.3f; t += 0.01f)
        {
            var (phase, _, _) = BattleAnimator.BallStage(t, 4, critical: true);
            if (order.Count == 0 || order[^1] != phase) order.Add(phase);
        }
        Assert.Equal(new[] { BallPhase.RunIn, BallPhase.Flight, BallPhase.Open, BallPhase.Rattle, BallPhase.Drop, BallPhase.Wobble, BallPhase.Pause, BallPhase.Click, BallPhase.Rest }, order);
        // A plain throw's times are as they were: no rattle in them
        Assert.Equal(BattleAnimator.BallArriveTime + BattleAnimator.BallOpenTime + BattleAnimator.BallDropTime + 3 * (BattleAnimator.BallWobbleTime + BattleAnimator.BallPauseTime),
            BattleAnimator.BallSettleTime(4), 5);
        Assert.Equal(BattleAnimator.BallSettleTime(1) + BattleAnimator.BallRattleTime, BattleAnimator.BallSettleTime(4, critical: true), 5);
    }

    [Fact]
    public void ACriticalCaptureIsHeardRingingThenShakingOnce()
    {
        var party = new Party();
        party.Add(Mon("Bibarel", 40, "Tackle"));
        var bag = new Inventory();
        bag.AddItem(PokeBall);
        var battle = new BattleEngine(new BattleSetup
        {
            PlayerParty = party, Inventory = bag, Pokedex = new Pokedex(), WildPokemon = new List<Pokemon> { Hard() },
            Random = Steady().Force(RollKind.CriticalCapture, 0).Force(RollKind.CatchShake, 0), Rules = Ruleset.Modern, Conditions = Caught(601, charm: true)
        });
        Settle(battle);
        var heard = AudioManager.Listen();
        try
        {
            battle.UseItem(PokeBall);
            for (int i = 0; i < 900 && !battle.IsBattleOver; i++)
            {
                if (battle.IsWaitingForConfirm) battle.ConfirmMessage();
                battle.Update(1f / 60f);
            }
        }
        finally
        {
            AudioManager.StopListening();
        }
        Assert.Equal(new[] { "ball_throw", "ball_critical", "ball_shake", "ball_click" }, heard.Where(s => s.StartsWith("ball_")).ToList());
    }
}
