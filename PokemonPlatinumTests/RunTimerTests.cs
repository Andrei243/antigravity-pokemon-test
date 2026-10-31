using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>Plan 13 · V10: the run timer, its splits, its file and its row of the options.</summary>
public class RunTimerTests
{
    private static readonly DateTime Noon = new(2026, 10, 31, 12, 0, 0, DateTimeKind.Utc);

    private static RunTimer Played(IEnumerable<(float Dt, double Clock)> frames)
    {
        var timer = new RunTimer();
        foreach (var (dt, clock) in frames) timer.Tick(dt, Noon.AddSeconds(clock));
        return timer;
    }

    [Fact]
    public void TheTimerReadsTheSameForTheSameFramesAndTheSameClock()
    {
        var frames = Enumerable.Range(0, 600).Select(i => (Dt: 1f / 60f, Clock: i / 60.0)).ToList();
        var a = Played(frames);
        var b = Played(frames);
        Assert.Equal(a.GameSeconds, b.GameSeconds);
        Assert.Equal(a.RealSeconds, b.RealSeconds);
        Assert.Equal(10.0, a.GameSeconds, 3);
        // The real time is what the clock moved between the frames: the first frame has nothing to count from
        Assert.Equal(599 / 60.0, a.RealSeconds, 6);

        // A clock that stands still (the harness's fixed one) adds no real time; the game's goes on
        var still = Played(Enumerable.Repeat((1f / 60f, 0.0), 120));
        Assert.Equal(0.0, still.RealSeconds);
        Assert.Equal(2.0, still.GameSeconds, 3);
    }

    [Fact]
    public void APauseIsNeverCountedAndTheClockGoingBackAddsNothing()
    {
        var timer = new RunTimer();
        timer.Tick(0.1f, Noon);
        timer.Tick(0.1f, Noon.AddSeconds(1));
        Assert.Equal(1.0, timer.RealSeconds, 6);
        // The title, the options, another window: an hour passes while it stands still
        timer.Pause();
        timer.Tick(0.1f, Noon.AddHours(1));
        Assert.Equal(1.0, timer.RealSeconds, 6);
        timer.Tick(0.1f, Noon.AddHours(1).AddSeconds(2));
        Assert.Equal(3.0, timer.RealSeconds, 6);
        // The clock set back adds nothing, and a machine that slept adds no more than the longest step
        timer.Tick(0.1f, Noon);
        Assert.Equal(3.0, timer.RealSeconds, 6);
        timer.Tick(0.1f, Noon.AddDays(1));
        Assert.Equal(3.0 + RunTimer.LongestStep, timer.RealSeconds, 6);
    }

    [Fact]
    public void ABadgeTakesASplitAtTheTimeTheTimerReads()
    {
        var story = new StoryState();
        var timer = Played(Enumerable.Range(0, 61).Select(i => (Dt: 1f / 60f, Clock: (double)i)));
        Assert.Empty(timer.Take(story));

        story.GiveBadge(RegionDatabase.Sinnoh, "Coal");
        var taken = timer.Take(story);
        Assert.Equal("badge.sinnoh.Coal", Assert.Single(taken).Id);
        var split = Assert.Single(timer.Splits);
        Assert.Equal(60.0, split.RealSeconds, 6);
        Assert.Equal(timer.GameSeconds, split.GameSeconds);
        // Once: looking again takes nothing
        Assert.Empty(timer.Take(story));
        Assert.Null(timer.Finish);
    }

    [Fact]
    public void TheCategoriesChooseWhichSplitsCountAndTheHallOfFameFinishes()
    {
        var story = new StoryState();
        var timer = new RunTimer();
        story.GiveBadge(RegionDatabase.Sinnoh, "Coal");
        timer.Take(story);
        story.Set("FLAG_DEFEATED_COMMANDER_SATURN_VALOR_CAVERN");
        timer.Take(story);
        story.CompleteRegion(RegionDatabase.Sinnoh);
        timer.Take(story);

        Assert.Equal(new[] { "hof.sinnoh" }, timer.In(TimerCategory.HallOfFame).Select(p => p.Point.Id));
        Assert.Equal(new[] { "badge.sinnoh.Coal", "hof.sinnoh" }, timer.In(TimerCategory.Badges).Select(p => p.Point.Id));
        Assert.Equal(new[] { "badge.sinnoh.Coal", "story.sinnoh.valorCavern", "hof.sinnoh" }, timer.In(TimerCategory.Story).Select(p => p.Point.Id));
        Assert.Empty(timer.In(TimerCategory.Off));
        Assert.Equal("hof.sinnoh", timer.Finish!.Id);
        Assert.Equal("hof.sinnoh", timer.Last(TimerCategory.Story)!.Value.Point.Id);
    }

    [Fact]
    public void EveryPointHasWordsAndEveryStoryFlagIsOneAScriptSets()
    {
        foreach (var point in RunTimer.Points)
        {
            Assert.False(string.IsNullOrWhiteSpace(point.Shown));
            Assert.Same(point, RunTimer.PointOf(point.Id));
        }
        // Each of Sinnoh's eight Badges, then its turns, then its Hall of Fame
        var sinnoh = RunTimer.Points.Where(p => p.Region == RegionDatabase.Sinnoh).ToList();
        Assert.Equal(8, sinnoh.Count(p => p.Kind == SplitKind.Badge));
        Assert.Equal(RunTimer.SinnohStory.Count, sinnoh.Count(p => p.Kind == SplitKind.Story));
        Assert.Equal(SplitKind.HallOfFame, sinnoh[^1].Kind);
        string scripts = string.Join("\n", Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "Data", "scripts"), "*.txt").Select(File.ReadAllText));
        foreach (var (_, flag) in RunTimer.SinnohStory)
            Assert.Contains($"setflag {flag}", scripts);
    }

    [Fact]
    public void ATimerBegunOnAGameUnderWayTakesNothingItHadReached()
    {
        var story = new StoryState();
        story.GiveBadge(RegionDatabase.Sinnoh, "Coal");
        var timer = GameEngine.RunTimerBegunLate(3600f, story);
        Assert.True(timer.BegunLate);
        Assert.Equal(3600.0, timer.RealSeconds);
        Assert.Contains("badge.sinnoh.Coal", timer.Before);
        Assert.Empty(timer.Take(story));
        story.GiveBadge(RegionDatabase.Sinnoh, "Forest");
        Assert.Equal("badge.sinnoh.Forest", Assert.Single(timer.Take(story)).Id);
    }

    [Fact]
    public void TheTimeIsWrittenAsTheCornerWritesIt()
    {
        Assert.Equal("0:00.0", RunTimer.Format(0));
        Assert.Equal("12:03.4", RunTimer.Format(723.45));
        Assert.Equal("1:02:03.9", RunTimer.Format(3723.99));
        Assert.Equal("1:02:03", RunTimer.Format(3723.99, tenths: false));
    }

    [Fact]
    public void TheFileHasAPlainShape()
    {
        var story = new StoryState();
        var timer = new RunTimer { RealSeconds = 100, GameSeconds = 90 };
        story.GiveBadge(RegionDatabase.Sinnoh, "Coal");
        timer.Take(story);
        timer.RealSeconds = 250;
        timer.GameSeconds = 230;
        story.GiveBadge(RegionDatabase.Sinnoh, "Forest");
        timer.Take(story);

        using var doc = JsonDocument.Parse(SplitsFile.Of(timer, TimerCategory.Badges, 2, "Lucas").ToJson());
        var root = doc.RootElement;
        Assert.Equal(1, root.GetProperty("format").GetInt32());
        Assert.Equal("Badges", root.GetProperty("category").GetString());
        Assert.Equal(2, root.GetProperty("slot").GetInt32());
        Assert.Equal("Lucas", root.GetProperty("trainer").GetString());
        Assert.False(root.GetProperty("finished").GetBoolean());
        Assert.Equal(250, root.GetProperty("realSeconds").GetDouble());
        var splits = root.GetProperty("splits").EnumerateArray().ToList();
        Assert.Equal(2, splits.Count);
        Assert.Equal("badge.sinnoh.Forest", splits[1].GetProperty("id").GetString());
        Assert.Equal(250, splits[1].GetProperty("realSeconds").GetDouble());
        Assert.Equal(150, splits[1].GetProperty("realSegment").GetDouble());
        Assert.Equal(140, splits[1].GetProperty("gameSegment").GetDouble());
        Assert.False(string.IsNullOrEmpty(splits[0].GetProperty("name").GetString()));
    }

    [Fact]
    public void TheTimerIsKeptWithTheSaveAndAnOlderSaveHasNone()
    {
        var state = new RunState { Timer = new RunTimer { RealSeconds = 12.5, GameSeconds = 11 } };
        state.Timer.Splits.Add(new TimerSplit { Id = "badge.sinnoh.Coal", RealSeconds = 10, GameSeconds = 9 });
        string json = JsonSerializer.Serialize(new SaveData { RunState = state });
        var back = JsonSerializer.Deserialize<SaveData>(json)!.RunState!.Timer!;
        Assert.Equal(12.5, back.RealSeconds);
        Assert.Equal("badge.sinnoh.Coal", Assert.Single(back.Splits).Id);
        Assert.True(new RunState().IsEmpty);
        Assert.False(state.IsEmpty);
        Assert.Null(JsonSerializer.Deserialize<SaveData>("{}")!.RunState);
    }

    [Fact]
    public void TheRowIsOffUnlessChosenAndStandsAfterTheMedals()
    {
        Assert.Equal(TimerCategory.Off, new GameSettings().RunTimer);
        var rows = OptionsScreen.AllRows.ToList();
        Assert.Equal(rows.IndexOf(OptionRow.MedalNotices) + 1, rows.IndexOf(OptionRow.RunTimer));
        Assert.True(rows.IndexOf(OptionRow.RunTimer) < rows.IndexOf(OptionRow.Controls));
        var s = new GameSettings();
        var seen = new List<TimerCategory>();
        for (int i = 0; i < 4; i++)
        {
            OptionsScreen.Change(s, OptionRow.RunTimer, 1);
            seen.Add(s.RunTimer);
            Assert.False(string.IsNullOrWhiteSpace(OptionsScreen.Describe(s, OptionRow.RunTimer).Help));
        }
        Assert.Equal(new[] { TimerCategory.HallOfFame, TimerCategory.Badges, TimerCategory.Story, TimerCategory.Off }, seen);
        s.RunTimer = TimerCategory.Story;
        var json = new JsonSerializerOptions { Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() } };
        Assert.Equal(TimerCategory.Story, JsonSerializer.Deserialize<GameSettings>(JsonSerializer.Serialize(s, json), json)!.RunTimer);
    }

    [Fact]
    public void AHallOfFameRecordKeepsTheRunsTime()
    {
        var entry = new HallOfFameEntry(Noon, Array.Empty<HallOfFameMember>(), 4321.5, 4200);
        var back = JsonSerializer.Deserialize<SavedHallOfFameEntry>(JsonSerializer.Serialize(SavedHallOfFameEntry.From(entry)))!.ToEntry();
        Assert.Equal(4321.5, back.RunSeconds);
        Assert.Equal(4200, back.GameSeconds);
        // A record from before the timer has none
        Assert.Null(JsonSerializer.Deserialize<SavedHallOfFameEntry>("{\"Date\":\"2026-10-31T00:00:00\",\"Team\":[]}")!.ToEntry().RunSeconds);
    }
}

/// <summary>Plan 13 · V10 in the game itself: the timer runs with the game, stands still on the title, splits at a Badge and writes its file only with the row on.</summary>
[Collection("Shared clocks and dice")]
public class RunTimerGameTests
{
    private const float Frame = 1f / 60f;

    private static void Run(GameEngine engine)
    {
        InputManager.Feed(ActionFrame.None);
        engine.Update(Frame);
    }

    [Theory]
    [InlineData(TimerCategory.Off)]
    [InlineData(TimerCategory.Badges)]
    public void ABadgeSplitsAndTheFileIsWrittenOnlyWithTheRowOn(TimerCategory category)
    {
        string folder = Path.Combine(Path.GetTempPath(), "timer-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var paths = SavePaths.Current;
        var fixedClock = WallClock.Fixed;
        SavePaths.Current = SavePaths.In(folder);
        try
        {
            WallClock.Fixed = new DateTime(2026, 10, 31, 12, 0, 0);
            var engine = new GameEngine(new GameSettings { Muted = true, RunTimer = category });
            engine.InitializeHeadless();
            engine.PeopleStayPut = true;
            engine.NewGameRegion = "Sinnoh";
            engine.StartNewGame();
            StoryMigration.PastTheOpening(engine.Drive.Story, ScriptLibrary.Default);
            var game = engine.Drive;
            game.State = GameState.Overworld;
            var timer = game.RunState.Timer!;
            Assert.NotNull(timer);
            Assert.False(timer.BegunLate);

            for (int f = 0; f < 60; f++)
            {
                if (f % 6 == 0) WallClock.Fixed = WallClock.Fixed.Value.AddSeconds(0.1);
                Run(engine);
            }
            Assert.Equal(1.0, timer.GameSeconds, 3);
            Assert.True(timer.RealSeconds > 0.8, $"{timer.RealSeconds}");

            // The title, the options and the close button's question stand it still; the field and a battle run it
            Assert.True(GameEngine.TimerStandsStill(GameState.Options));
            Assert.True(GameEngine.TimerStandsStill(GameState.Title));
            Assert.True(GameEngine.TimerStandsStill(GameState.WindowClosing));
            Assert.False(GameEngine.TimerStandsStill(GameState.Overworld));
            Assert.False(GameEngine.TimerStandsStill(GameState.Battle));

            game.Story.GiveBadge(RegionDatabase.Sinnoh, "Coal");
            Run(engine);
            Assert.Contains(timer.Splits, s => s.Id == "badge.sinnoh.Coal");
            string file = Path.Combine(folder, SavePaths.SplitsName);
            Assert.Equal(category != TimerCategory.Off, File.Exists(file));
            if (category != TimerCategory.Off)
                Assert.Contains("badge.sinnoh.Coal", File.ReadAllText(file));

            // Kept with the save
            Assert.Contains(game.BuildSave().RunState!.Timer!.Splits, s => s.Id == "badge.sinnoh.Coal");
        }
        finally
        {
            InputManager.Feed(null);
            Ruleset.Use(RulesPreset.Platinum);
            WallClock.Fixed = fixedClock;
            SavePaths.Current = paths;
            try { Directory.Delete(folder, true); } catch (IOException) { }
        }
    }
}
