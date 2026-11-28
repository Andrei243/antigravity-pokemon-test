using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// The loop catch (plan 44 · SD10; <c>docs/mechanics/rulings.md</c>, "The loop catch"): a loop closes round a point,
/// the Pokémon's path is the same for a seed, a strike breaks the line and three loops calm it.
/// </summary>
public class LoopCatchTests
{
    private static LoopTaskData Task(string id = "fallen_trunk") => LoopTasks.Get(id)!;

    private static LoopInput Lifted => new(Vector2.Zero, null, false);

    private static List<Vector2> Ring(Vector2 at, float radius, int points = 32) =>
        Enumerable.Range(0, points).Select(i => at + new Vector2(MathF.Cos(i * MathF.Tau / points), MathF.Sin(i * MathF.Tau / points)) * radius).ToList();

    /// <summary>A circle drawn round where the Pokémon stood as it began, a little past a full turn, at once.</summary>
    private static LoopEvents DrawRound(LoopCatch game, float radius, Func<Vector2>? middle = null, int steps = 28)
    {
        var all = LoopEvents.None;
        for (int i = 0; i <= steps + 3; i++)
        {
            float a = i * MathF.Tau / steps;
            var at = (middle?.Invoke() ?? game.At) + new Vector2(MathF.Cos(a), MathF.Sin(a)) * radius;
            all |= game.Step(new LoopInput(Vector2.Zero, at, true));
            if ((all & LoopEvents.Looped) != 0) break;
        }
        return all;
    }

    [Fact]
    public void TheTasksReadAndEveryOneIsASpeciesWithAReachADiscCanGoRound()
    {
        Assert.Equal(8, LoopTasks.All.Count);
        Assert.Equal(LoopTasks.All.Count, LoopTasks.All.Select(t => t.Id).Distinct().Count());
        foreach (var task in LoopTasks.All)
        {
            Assert.NotNull(PokemonDatabase.Get(task.Species));
            Assert.InRange(task.Reach, 150f, 220f);
            Assert.True(task.StrikeEvery > LoopCatch.WindUpSeconds + 1f);
            // A circle just outside its reach fits in the line's length and on the disc's field
            Assert.True(MathF.Tau * (task.Reach + LoopAutopilot.Margin) < LoopCatch.LineLength);
            Assert.True(LoopCatch.RoamLeast.X - task.Reach > 0 && LoopCatch.RoamLeast.Y - (task.Reach + LoopAutopilot.Margin) >= LoopCatch.DiscLeast.Y - 1);
        }
    }

    [Fact]
    public void ALoopClosesRoundAPoint()
    {
        var middle = new Vector2(900, 500);
        Assert.True(LoopCatch.Counts(Ring(middle, 120), middle));
        Assert.False(LoopCatch.Counts(Ring(middle + new Vector2(300, 0), 120), middle));
        // Too tight round the body, and too small to count
        Assert.False(LoopCatch.Counts(Ring(middle, 20), middle));
        Assert.False(LoopCatch.Counts(Ring(middle + new Vector2(30, 0), 45), middle));
        Assert.True(LoopCatch.Holds(Ring(middle, 120), middle + new Vector2(100, 0)));
        Assert.False(LoopCatch.Holds(Ring(middle, 120), middle + new Vector2(130, 0)));
    }

    [Fact]
    public void ALineDrawnRoundTheBodyIsCountedAsItCrossesItself()
    {
        var game = new LoopCatch(Task(), 7);
        var events = DrawRound(game, 160);
        Assert.True((events & LoopEvents.Looped) != 0);
        Assert.Equal(1, game.Calm);
        Assert.True(game.Line.Count <= 1);
        Assert.Equal(LoopCatch.Charges, game.Strength);
    }

    [Fact]
    public void ALoopThatHoldsNothingIsCutOutAndCountsNothing()
    {
        var game = new LoopCatch(Task(), 7);
        var away = game.At + new Vector2(game.At.X < 960 ? 480 : -480, 0);
        var events = DrawRound(game, 80, () => away);
        Assert.Equal(LoopEvents.None, events & LoopEvents.Looped);
        Assert.Equal(0, game.Calm);
        Assert.True(game.Drawing);
    }

    [Fact]
    public void ThreeLoopsCalmIt()
    {
        var game = new LoopCatch(Task(), 7);
        for (int i = 0; i < LoopCatch.CalmNeeded; i++)
        {
            Assert.Equal(LoopOutcome.Going, game.Outcome);
            DrawRound(game, 160);
            game.Step(Lifted);
        }
        Assert.Equal(LoopCatch.CalmNeeded, game.Calm);
        Assert.Equal(LoopOutcome.Calmed, game.Outcome);
        // Nothing moves once the try is over
        int ticks = game.Ticks;
        Assert.Equal(LoopEvents.None, game.Step(Lifted));
        Assert.Equal(ticks, game.Ticks);
    }

    private static string Path(uint seed, string task, int ticks)
    {
        var game = new LoopCatch(Task(task), seed);
        var said = new StringBuilder();
        for (int t = 0; t < ticks; t++)
        {
            var events = game.Step(Lifted);
            if (t % 30 == 0) said.Append($"{game.At.X:F3},{game.At.Y:F3} {events};");
        }
        return said.ToString();
    }

    [Fact]
    public void ThePathIsTheSameForASeed()
    {
        foreach (var task in LoopTasks.All)
        {
            Assert.Equal(Path(31, task.Id, 1800), Path(31, task.Id, 1800));
            Assert.NotEqual(Path(31, task.Id, 1800), Path(32, task.Id, 1800));
        }
    }

    [Fact]
    public void WhatTheDiscDoesDrawsNothingFromItsChance()
    {
        // Until a loop is counted, a try with the disc busy far off goes as one with the disc lifted
        var still = new LoopCatch(Task("lost_float"), 5);
        var busy = new LoopCatch(Task("lost_float"), 5);
        for (int t = 0; t < 900; t++)
        {
            still.Step(Lifted);
            busy.Step(new LoopInput(new Vector2(t % 80 < 40 ? 1 : -1, 0), null, t % 50 < 25));
            Assert.Equal(still.At, busy.At);
        }
    }

    [Fact]
    public void APokemonKeepsToItsGround()
    {
        foreach (var task in LoopTasks.All)
        {
            var game = new LoopCatch(Task(task.Id), 3);
            for (int t = 0; t < 3600; t++)
            {
                game.Step(Lifted);
                Assert.InRange(game.At.X, LoopCatch.RoamLeast.X, LoopCatch.RoamMost.X);
                Assert.InRange(game.At.Y, LoopCatch.RoamLeast.Y, LoopCatch.RoamMost.Y);
            }
        }
    }

    [Theory]
    [InlineData("Bidoof", LoopGait.Run)]
    [InlineData("Starly", LoopGait.Soar)]
    [InlineData("Magikarp", LoopGait.Dart)]
    [InlineData("Ekans", LoopGait.Weave)]
    [InlineData("Gastly", LoopGait.Drift)]
    [InlineData("Machop", LoopGait.Walk)]
    public void ItMovesByItsShape(string species, LoopGait gait) => Assert.Equal(gait, LoopCatch.GaitOf(PokemonDatabase.Get(species)!));

    [Fact]
    public void ItsPaceGoesByItsSpeed()
    {
        Assert.True(LoopCatch.PaceOf(PokemonDatabase.Get("Pachirisu")!) > LoopCatch.PaceOf(PokemonDatabase.Get("Geodude")!));
        Assert.InRange(LoopCatch.PaceOf(PokemonDatabase.Get("Shuckle")!), 60f, 260f);
        Assert.Equal(260f, LoopCatch.PaceOf(PokemonDatabase.Get("Electrode")!));
    }

    /// <summary>A short line laid <paramref name="from"/> the Pokémon just before a strike lands (it moves on, so an older one would trail behind it).</summary>
    private static LoopEvents HoldALineUntilItStrikes(LoopCatch game, float from)
    {
        for (int t = 0; t < 60 * 30; t++)
        {
            float side = game.At.X < 960 ? 1f : -1f;
            var at = game.At + new Vector2(side * from, t % 2 * 10f);
            var events = game.Step(new LoopInput(Vector2.Zero, at, game.WindingUp && game.WindUpShare > 0.9f));
            if ((events & LoopEvents.Struck) != 0) return events;
        }
        throw new InvalidOperationException("It never struck");
    }

    [Fact]
    public void AStrikeBreaksALineWithinItsReachAndCostsACharge()
    {
        var game = new LoopCatch(Task(), 11);
        var events = HoldALineUntilItStrikes(game, 100);
        Assert.True((events & LoopEvents.Broke) != 0);
        Assert.Equal(LoopCatch.Charges - 1, game.Strength);
        Assert.Empty(game.Line);
        // A broken disc draws nothing until it has been lifted
        game.Step(new LoopInput(Vector2.Zero, game.At + new Vector2(300, 0), true));
        Assert.False(game.Drawing);
        game.Step(Lifted);
        game.Step(new LoopInput(Vector2.Zero, game.At + new Vector2(300, 0), true));
        Assert.True(game.Drawing);
    }

    [Fact]
    public void AStrikeLeavesALineOutOfItsReachAlone()
    {
        var game = new LoopCatch(Task(), 11);
        var events = HoldALineUntilItStrikes(game, 330);
        Assert.Equal(LoopEvents.None, events & LoopEvents.Broke);
        Assert.Equal(LoopCatch.Charges, game.Strength);
        Assert.True(game.Drawing);
    }

    [Fact]
    public void ADiscWithNoChargeLeftEndsTheTry()
    {
        var game = new LoopCatch(Task(), 11);
        for (int i = 0; i < LoopCatch.Charges; i++)
        {
            HoldALineUntilItStrikes(game, 100);
            game.Step(Lifted);
        }
        Assert.Equal(0, game.Strength);
        Assert.Equal(LoopOutcome.SlippedAway, game.Outcome);
    }

    [Fact]
    public void TheTimingAssistDoublesTheWindUpOrMakesTheLiftItself()
    {
        int WindUpTicks(TimingAssist assist)
        {
            var game = new LoopCatch(Task(), 11, assist);
            while ((game.Step(Lifted) & LoopEvents.WindUp) == 0) { }
            int ticks = 0;
            while (game.WindingUp) { game.Step(Lifted); ticks++; }
            return ticks;
        }
        Assert.InRange(WindUpTicks(TimingAssist.Generous), WindUpTicks(TimingAssist.Off) * 2 - 1, WindUpTicks(TimingAssist.Off) * 2 + 1);

        var helped = new LoopCatch(Task(), 11, TimingAssist.Automatic);
        var events = HoldALineUntilItStrikes(helped, 100);
        Assert.Equal(LoopEvents.None, events & LoopEvents.Broke);
        Assert.Equal(LoopCatch.Charges, helped.Strength);
        Assert.False(helped.Drawing);
    }

    [Fact]
    public void TheLineIsOnlySoLong()
    {
        var game = new LoopCatch(Task(), 2);
        for (int t = 0; t < 600; t++) game.Step(new LoopInput(Vector2.Zero, new Vector2(200 + t * 12 % 1500, 150 + t / 125 * 20), true));
        float length = 0f;
        for (int i = 1; i < game.Line.Count; i++) length += Vector2.Distance(game.Line[i - 1], game.Line[i]);
        Assert.True(length <= LoopCatch.LineLength + 0.01f);
    }

    [Fact]
    public void TheStickMovesTheDiscAndHeldItDraws()
    {
        var game = new LoopCatch(Task(), 2);
        var start = game.Disc;
        for (int t = 0; t < 30; t++) game.Step(new LoopInput(new Vector2(-1, 0), null, true));
        Assert.InRange(start.X - game.Disc.X, LoopCatch.DiscSpeed * 0.45f, LoopCatch.DiscSpeed * 0.55f);
        Assert.True(game.Drawing);
    }

    [Fact]
    public void TheAutopilotCalmsEveryTasksPokemonOnAHundredSeeds()
    {
        foreach (var task in LoopTasks.All)
        {
            var lost = Enumerable.Range(1, 100).Where(seed => LoopAutopilot.Play(new LoopCatch(task, (uint)seed)) != LoopOutcome.Calmed).ToList();
            Assert.True(lost.Count == 0, $"{task.Id} slipped away on seeds {string.Join(", ", lost)}");
        }
    }

    [Fact]
    public void TheScreenPlaysATryByButtonsAlone()
    {
        var screen = new LoopScreen();
        var game = new LoopCatch(Task(), 9);
        screen.Open(game);
        Assert.Equal(LoopPhase.Brief, screen.Phase);
        screen.Advance(1f, new LoopInput(Vector2.Zero, null, true));
        Assert.Equal(0, game.Ticks);

        screen.Confirm();
        Assert.Equal(LoopPhase.Playing, screen.Phase);
        // The press that began it draws nothing until the button has come up
        screen.Advance(0.1f, new LoopInput(Vector2.Zero, null, true));
        Assert.False(game.Drawing);
        screen.Advance(0.1f, Lifted);
        screen.Advance(0.1f, new LoopInput(Vector2.Zero, null, true));
        Assert.True(game.Drawing);

        screen.Autopilot = true;
        for (int i = 0; i < 60 * 180 && screen.Phase == LoopPhase.Playing; i++) screen.Advance(1f / 60f, Lifted);
        Assert.Equal(LoopPhase.Over, screen.Phase);
        Assert.True(screen.Calmed);
        // The end stands a moment before a press puts it away
        screen.Confirm();
        Assert.True(screen.IsActive);
        screen.Advance(LoopScreen.EndHold + 0.1f, Lifted);
        screen.Confirm();
        Assert.False(screen.IsActive);
    }

    [Fact]
    public void BackLeavesTheBriefAndGivesATryUp()
    {
        var screen = new LoopScreen();
        screen.Open(new LoopCatch(Task(), 9));
        screen.Cancel();
        Assert.False(screen.IsActive);
        Assert.False(screen.Calmed);

        screen.Open(new LoopCatch(Task(), 9));
        screen.Confirm();
        screen.Cancel();
        Assert.Equal(LoopPhase.Over, screen.Phase);
        Assert.False(screen.Calmed);
        screen.Cancel();
        Assert.False(screen.IsActive);
    }

    // ------------------------------------------------------------------ the warden

    private static OpeningTests.Game AtTheStation()
    {
        var game = new OpeningTests.Game();
        game.Arrive("Sinnoh", 169, 662);
        return game;
    }

    private static int Has(OpeningTests.Game game, string item) => game.Bag.GetQuantity(ItemDatabase.Get(item)!);

    private static void Talk(OpeningTests.Game game, params int[] answers)
    {
        foreach (int answer in answers) game.Answers.Enqueue(answer);
        game.Talk("loop_warden");
    }

    [Fact]
    public void TheWardensTasksComeInTurnAndEachPaysOnce()
    {
        var game = AtTheStation();
        string[] prizes = { "Potion", "Escape Rope", "Poké Ball", "Super Potion", "Net Ball", "Sitrus Berry", "Revive", "Ultra Ball" };
        for (int i = 0; i < LoopTasks.All.Count; i++)
        {
            int had = Has(game, prizes[i]);
            Talk(game, 0);
            Assert.Contains($"open LoopCatch {LoopTasks.All[i].Id} 1", game.Last.Log);
            Assert.Equal(i + 1, game.Story.Var("VAR_LOOP_CATCH_TASKS"));
            Assert.True(Has(game, prizes[i]) > had, $"task {i} paid no {prizes[i]}");
        }
        // Every job done: one errand a day, a Revive for it
        int revives = Has(game, "Revive");
        game.Rolls.Enqueue(3);
        Talk(game, 0);
        Assert.Contains("open LoopCatch dim_lantern 1", game.Last.Log);
        Assert.Equal(revives + 1, Has(game, "Revive"));
        Assert.True(game.Story.Has("FLAG_DAILY_LOOP_CATCH"));
        Talk(game, 0);
        Assert.DoesNotContain(game.Last.Log, l => l.StartsWith("open LoopCatch", StringComparison.Ordinal));
        Assert.Equal(revives + 1, Has(game, "Revive"));
    }

    [Fact]
    public void ANoAndTheExplanationOpenNothing()
    {
        var game = AtTheStation();
        foreach (int answer in new[] { 1, 2 })
        {
            Talk(game, answer);
            Assert.DoesNotContain(game.Last.Log, l => l.StartsWith("open LoopCatch", StringComparison.Ordinal));
            Assert.Equal(0, game.Story.Var("VAR_LOOP_CATCH_TASKS"));
        }
    }
}

/// <summary>The loop catch in a game with no window (plan 44 · SD10): a script's try played at once, and the screen by the engine's own frames.</summary>
[Collection("Shared clocks and dice")]
public class LoopCatchGameTests
{
    [Fact]
    public void AGameWithNoWindowPlaysATryAtOnceAndTouchesNothingOfThePlayers()
    {
        string folder = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "loopcatch-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var paths = SavePaths.Current;
        try
        {
            SavePaths.Current = SavePaths.In(folder);
            Dice.Seed(31);
            var engine = new GameEngine(new GameSettings { Muted = true });
            engine.InitializeHeadless();
            engine.NewGameRegion = "Sinnoh";
            engine.StartNewGame();
            var game = engine.Drive;
            Assert.True(game.OpenLoop("fallen_trunk", 4));
            Assert.Equal(0, game.Party.Count);
            Assert.NotEqual(GameState.Loop, game.State);
            Assert.False(game.OpenLoop("no_such_task", 4));
        }
        finally
        {
            Dice.Seed(null);
            Ruleset.Use(RulesPreset.Platinum);
            PlayerIdentity.InRegion(RegionDatabase.First);
            SavePaths.Current = paths;
            try { Directory.Delete(folder, recursive: true); } catch (IOException) { }
        }
    }
}
