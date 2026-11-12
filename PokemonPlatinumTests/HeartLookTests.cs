using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;
using Xunit;
using static PokemonPlatinumTests.CoreScenario;

namespace PokemonPlatinumTests;

/// <summary>
/// Locked hearts on screen (plan 44 · SD2; style guide, "A locked heart"): the haze set by the log's events alone, the
/// opening's scene a function of time and a button with the level panel's two presses, CALM where RUN stands, and the
/// script's <c>heart</c>.
/// </summary>
public class HeartLookTests
{
    private static Pokemon Locked(string species, int level, string id, params string[] moves)
    {
        var p = Of(Gender.Male, species, level, moves);
        p.Lock = HeartLock.Locked(id);
        return p;
    }

    private static IEnumerable<BattleEvent> Flat(IEnumerable<BattleEvent> log) =>
        log.SelectMany(e => e is Said s ? new[] { e }.Concat(s.Shows).Concat(s.OnImpact) : new[] { e });

    [Fact]
    public void TheHazeIsSetByTheEventsAloneAndGoesWithAReclaim()
    {
        var party = new Party();
        party.Add(Of(Gender.Male, "Luxray", 40, "Spark"));
        var trainer = new Trainer { Id = "lockwork_agent", Name = "Ivo", TrainerClass = "Ace Trainer", PrizeMoney = 600 };
        trainer.Party.Add(Locked("Gible", 20, "lock_test"));
        var core = new BattleCore(new CoreSetup
        {
            PlayerParty = party, Trainers = new List<Trainer> { trainer }, Random = Calm().Force(RollKind.CatchShake, 0).Force(RollKind.CriticalCapture, 255),
            Rules = Ruleset.Modern, PlayerController = null, EnemyController = null, PlayerName = "Lucas", Reclaimer = true
        });
        core.Start();
        var looks = new ConditionLooks();
        foreach (var e in Flat(core.TakeLog())) looks.Tell(e);
        Assert.True(looks.Of(Foe).Haze.Up);
        Assert.Equal(5, looks.Of(Foe).LockBars);
        Assert.False(looks.Of(Mine).Haze.Up);

        core.Submit(BattleChoice.UseItem(Mine, "Poké Ball"), BattleChoice.Fight(Foe, 0));
        foreach (var e in Flat(core.TakeLog())) looks.Tell(e);
        Assert.False(looks.Of(Foe).Haze.Up);
    }

    [Fact]
    public void LooseFlaresTheHazeAndACalmEasesIt()
    {
        var core = Wild(Locked("Gible", 30, "lock_test", "Tackle"), Of(Gender.Male, "Bidoof", 30, "Growl"), Calm().Force(RollKind.Lock, 0, 1), Ruleset.Modern);
        var looks = new ConditionLooks();
        foreach (var e in Flat(core.TakeLog())) looks.Tell(e);
        Assert.True(looks.Of(Mine).Haze.Up);
        core.Submit(BattleChoice.Fight(Mine, 0), BattleChoice.Fight(Foe, 0));
        foreach (var e in Flat(core.TakeLog())) looks.Tell(e);
        Assert.True(looks.Of(Mine).Loose.Up);
        core.Submit(BattleChoice.Calm(Mine), BattleChoice.Fight(Foe, 0));
        foreach (var e in Flat(core.TakeLog())) looks.Tell(e);
        Assert.False(looks.Of(Mine).Loose.Up);
        Assert.True(looks.Of(Mine).Haze.Up);
        Assert.Equal(ConditionLooks.LockRulesHave(core.At(Mine)).Bars, looks.Of(Mine).LockBars);
    }

    [Fact]
    public void TheHazeKeepsUnderItsCapAndOffTheBodysMiddle()
    {
        for (float age = 0f; age < 7f; age += 0.1f)
        {
            var fx = new FxList();
            MoveFx.LockHaze(age, 1f, 1f, 1234, System.Numerics.Vector3.Zero, 1f, 0.4f, fx);
            Assert.True(fx.Count <= MoveFx.HazeCap, $"{fx.Count} at {age}");
        }
    }

    [Fact]
    public void TheOpeningIsAFunctionOfTimeAndAButtonWithTheLevelPanelsTwoPresses()
    {
        var p = Locked("Gible", 10, "lock_test", "Tackle");
        p.Lock = p.Lock! with { Gauge = 0, HeldExp = 3000 };
        var opening = HeartLock.Open(p);
        Assert.True(opening.Levelled);
        var screen = new HeartOpeningScreen();
        screen.Begin(opening);
        Assert.Equal(HeartOpeningPhase.Haze, screen.Phase);
        Assert.Equal(1f, screen.Look().Haze);
        screen.PressConfirm();
        Assert.Equal(HeartOpeningPhase.Haze, screen.Phase);
        for (int i = 0; i < 300 && screen.Phase != HeartOpeningPhase.Announce; i++) screen.Advance(1f / 60f);
        Assert.Equal(HeartOpeningPhase.Announce, screen.Phase);
        Assert.Equal(0f, screen.Look().Haze);
        for (int i = 0; i < 120; i++) screen.Advance(1f / 60f);
        screen.PressConfirm();
        Assert.Equal(HeartOpeningPhase.Levels, screen.Phase);
        Assert.NotNull(screen.Panel);
        screen.PressConfirm();
        Assert.True(screen.IsActive);
        screen.PressConfirm();
        Assert.False(screen.IsActive);
    }

    [Fact]
    public void AnOpeningWithNoLevelEndsAtItsLine()
    {
        var p = Locked("Gible", 10, "lock_test", "Tackle");
        p.Lock = p.Lock! with { Gauge = 0 };
        var screen = new HeartOpeningScreen();
        screen.Begin(HeartLock.Open(p));
        for (int i = 0; i < 600 && screen.Phase != HeartOpeningPhase.Announce; i++) screen.Advance(1f / 60f);
        for (int i = 0; i < 120; i++) screen.Advance(1f / 60f);
        screen.PressConfirm();
        Assert.False(screen.IsActive);
    }

    [Fact]
    public void TheScriptsHeartGivesTheReclaimerRunsTheRiteAndOpensAHeart()
    {
        var host = new HeadlessScriptHost();
        var gible = Locked("Gible", 20, "lock_sinnoh_gible", "Tackle");
        host.Party.Add(gible);
        int Run(string body)
        {
            var runner = new ScriptRunner(ScriptLibrary.Default, host);
            runner.Start(ScriptParser.Parse("t", "script T\n  " + body + "\n  end")[0], null);
            runner.RunToEnd();
            Assert.Empty(host.Problems);
            return runner.Result;
        }
        Assert.Equal(1, Run("heart give"));
        Assert.True(host.Story.Has(ScriptRunner.ReclaimerFlag));
        Assert.Equal(1, Run("heart has"));
        Assert.Equal(0, Run("heart give"));

        Assert.Equal(1, Run("heart spring"));
        Assert.Equal(HeartLock.Full - HeartLock.Fall.Spring, gible.Lock!.Gauge);
        Assert.Equal(2, Run("heart spring"));
        Assert.Equal(1, Run("heart locked"));

        gible.Lock = gible.Lock with { Gauge = 0 };
        Assert.Equal(1, Run("heart open"));
        Assert.Null(gible.Lock);
        Assert.Contains("lock_sinnoh_gible", host.Hearts.Opened);
    }
}
