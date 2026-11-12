using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 41 · EG6: the Week's Work, the hub's board of missions (<see cref="MissionBoard"/>, <see cref="MissionWeek"/>,
/// <c>Data/missions.json</c>; rulings, "The Week's Work"). A week drawn alike twice and anew on the next Monday, its
/// missions only in the regions finished, progress counted from the engine's events and never another region's, a timed
/// mission's clock standing still outside its region and its end, a reward claimed once, the board round-tripping a
/// save, and a week's five done across two regions and their pennants ranking Pennant Hall.
/// </summary>
[Collection("Shared clocks and dice")]
public class MissionBoardTests
{
    private static readonly DateTime Monday = new(2026, 11, 9);
    private static readonly string[] Two = { RegionDatabase.Sinnoh, RegionDatabase.Kanto };

    private static string Key(Mission m) => $"{m.Kind} {m.Region} {m.Area} {m.Type} {m.Goal} {m.Reward} {m.Item} {m.Minutes}";

    [Fact]
    public void AWeekIsDrawnAlikeHoweverOftenItIsReadAndAnewOnTheNextMonday()
    {
        var week = MissionWeek.Draw(Monday, Two).Select(Key).ToList();
        Assert.Equal(MissionWeek.File.Plain + MissionWeek.File.TimedCount, week.Count);
        Assert.Equal(week, MissionWeek.Draw(Monday, Two).Select(Key));
        // Any day of the week is that week
        Assert.Equal(week, MissionWeek.Draw(Monday.AddDays(6), Two).Select(Key));
        Assert.Equal(MissionWeek.WeekOf(Monday), MissionWeek.WeekOf(Monday.AddDays(6)));
        // The next Monday draws anew
        Assert.NotEqual(week, MissionWeek.Draw(Monday.AddDays(7), Two).Select(Key));
        // The data's version is part of the seed
        Assert.NotEqual(week, MissionWeek.Draw(Monday, Two, version: "another").Select(Key));
        // The last two are timed, the first three are not
        var drawn = MissionWeek.Draw(Monday, Two);
        Assert.All(drawn.Take(MissionWeek.File.Plain), m => Assert.False(m.IsTimed));
        Assert.All(drawn.Skip(MissionWeek.File.Plain), m => Assert.True(m.IsTimed));
        // Nothing is drawn with no region finished
        Assert.Empty(MissionWeek.Draw(Monday, Array.Empty<string>()));
    }

    [Fact]
    public void MissionsAreOnlyInTheRegionsFinishedAndTheirPlacesAreTheRegionsOwn()
    {
        for (int w = 0; w < 30; w++)
            foreach (var m in MissionWeek.Draw(Monday.AddDays(7 * w), Two))
            {
                Assert.Contains(m.Region, Two);
                Assert.True(m.Goal >= 1 && m.Reward > 0, Key(m));
                if (m.Kind is MissionKind.CatchInArea or MissionKind.Landmark)
                    Assert.True(File.Exists(GameDataFiles.PathOf($"world/{RegionDatabase.Get(m.Region)!.Folder}/areas/{m.Area}.json")), Key(m));
                if (m.Item != null) Assert.NotNull(ItemDatabase.Get(m.Item));
                Assert.DoesNotContain("{", MissionWeek.Describe(m));
            }
        // Every place the file names is an area of its region, and a place to catch in has wild Pokémon in its grass
        foreach (var (region, places) in MissionWeek.File.Places)
        {
            string folder = RegionDatabase.Get(region)!.Folder;
            foreach (string area in places.Catch.Concat(places.Landmarks))
                Assert.True(File.Exists(GameDataFiles.PathOf($"world/{folder}/areas/{area}.json")), $"{region} {area}");
            foreach (string area in places.Catch)
                Assert.NotEmpty(GameDataFiles.Load<WorldAreaFile>($"world/{folder}/areas/{area}.json").Land ?? new());
        }
    }

    [Fact]
    public void ProgressIsCountedFromTheEnginesEventsAndNeverAnotherRegions()
    {
        var board = new MissionBoard();
        board.Refresh(Monday, Two);
        var plain = board.Missions.Take(MissionWeek.File.Plain).ToList();
        foreach (var m in plain)
        {
            string other = m.Region == RegionDatabase.Sinnoh ? RegionDatabase.Kanto : RegionDatabase.Sinnoh;
            board.Tell(EventFor(m, other));
            Assert.Equal(0, m.Progress);
            board.Tell(EventFor(m, m.Region));
            Assert.Equal(1, m.Progress);
        }
        // A timed mission counts only while it runs
        var timed = board.Missions[MissionWeek.File.Plain];
        board.Tell(EventFor(timed, timed.Region));
        Assert.Equal(0, timed.Progress);
    }

    /// <summary>An event that counts for a mission, as the engine tells it, in a region.</summary>
    private static MissionEvent EventFor(Mission m, string region) => m.Kind switch
    {
        MissionKind.CatchType => new MissionEvent(MissionEventKind.Caught, region, "", m.Type, null),
        MissionKind.CatchInArea => new MissionEvent(MissionEventKind.Caught, region, m.Area, PokemonType.Normal),
        MissionKind.BeatTrainers => new MissionEvent(MissionEventKind.BeatTrainer, region),
        MissionKind.WinRematch => new MissionEvent(MissionEventKind.BeatRematch, region),
        MissionKind.HatchEggs => new MissionEvent(MissionEventKind.Hatched, region),
        MissionKind.Landmark => new MissionEvent(MissionEventKind.Arrived, region, m.Area),
        MissionKind.WinFacility => new MissionEvent(MissionEventKind.FacilityWin, region),
        _ => new MissionEvent(MissionEventKind.ContestWin, region)
    };

    [Fact]
    public void ATimedMissionsClockStandsStillOutsideItsRegionAndEnds()
    {
        var board = new MissionBoard();
        board.Refresh(Monday, Two);
        int index = MissionWeek.File.Plain;
        var timed = board.Missions[index];
        Assert.False(board.Begin(0), "a mission with no clock isn't begun");
        Assert.True(board.Begin(index));
        Assert.False(board.Begin(index + 1), "one timed mission at a time");
        float full = timed.Minutes * 60f;
        Assert.Equal(full, board.SecondsLeft);
        string other = timed.Region == RegionDatabase.Sinnoh ? RegionDatabase.Kanto : RegionDatabase.Sinnoh;
        Assert.False(board.Tick(600f, other));
        Assert.False(board.Tick(600f, RegionDatabase.Hub));
        Assert.Equal(full, board.SecondsLeft);
        Assert.False(board.Tick(30f, timed.Region));
        Assert.Equal(full - 30f, board.SecondsLeft);
        // (one thing short of its goal, so it is still under way)
        if (timed.Goal > 1)
        {
            board.Tell(EventFor(timed, timed.Region));
            Assert.Equal(1, timed.Progress);
        }
        // Its time runs out: it ends undone, and may be begun again from nought
        Assert.True(board.Tick(full, timed.Region));
        Assert.Null(board.Running);
        Assert.Equal(0, timed.Progress);
        Assert.True(board.Begin(index));
        for (int k = 0; k < timed.Goal; k++) board.Tell(EventFor(timed, timed.Region));
        Assert.True(timed.IsDone);
        Assert.Null(board.Running);
    }

    [Fact]
    public void ARewardIsClaimedOnceAndAPastWeeksWaitsAtTheBoard()
    {
        var board = new MissionBoard();
        board.Refresh(Monday, Two);
        var first = board.Missions[0];
        Assert.Null(board.Claim(0));
        for (int k = 0; k < first.Goal; k++) board.Tell(EventFor(first, first.Region));
        Assert.Equal(1, board.Unclaimed);
        Assert.Equal(first.Reward, board.Claim(0)!.Value.Pennants);
        Assert.Null(board.Claim(0));
        Assert.Equal(0, board.Unclaimed);
        // Done and not claimed when the week turns: it waits, and the week's undone missions go
        var second = board.Missions[1];
        for (int k = 0; k < second.Goal; k++) board.Tell(EventFor(second, second.Region));
        Assert.True(board.Refresh(Monday.AddDays(7), Two));
        Assert.Single(board.Waiting);
        Assert.Equal(second.Reward, board.Waiting[0].Pennants);
        Assert.All(board.Missions, m => Assert.Equal(0, m.Progress));
        // The board's screen claims what waits as it opens, its pennants to Pennant Hall
        var plaza = new PlazaState();
        var screen = new MissionBoardScreen();
        screen.Open(board, plaza, new Inventory());
        Assert.Equal(second.Reward, plaza.Coins);
        Assert.Empty(board.Waiting);
        Assert.Equal(second.Reward, screen.Paid);
    }

    [Fact]
    public void TheBoardRoundTripsASaveAndAnOlderSaveReadsNone()
    {
        var board = new MissionBoard();
        Assert.Null(board.ToSave());
        board.Refresh(Monday, Two);
        board.Tell(EventFor(board.Missions[0], board.Missions[0].Region));
        board.Begin(MissionWeek.File.Plain);
        board.Tick(12.5f, board.Missions[MissionWeek.File.Plain].Region);
        string text = GameDataFiles.Serialize(board.ToSave());
        var back = new MissionBoard();
        back.Load(GameDataFiles.Deserialize<MissionsSave>(text));
        Assert.Equal(text, GameDataFiles.Serialize(back.ToSave()));
        Assert.Equal(board.Missions.Select(Key), back.Missions.Select(Key));
        Assert.Equal(MissionWeek.File.Plain, back.Active);
        // Read again the same week, nothing is drawn anew
        Assert.False(back.Refresh(Monday.AddDays(2), Two));
        var older = new SaveData();
        Assert.Null(older.Missions);
        var none = new MissionBoard();
        none.Load(older.Missions);
        Assert.False(none.Opened);
        Assert.Null(none.ToSave());
    }

    [Fact]
    public void TheHarnesssFixedDateGivesAFixedWeekThroughTheScript()
    {
        GameClock.FixedDate = Monday.AddDays(3);
        try
        {
            var story = new StoryState();
            foreach (string id in Two) story.CompleteRegion(id);
            var host = new HeadlessScriptHost(story, new Party(), new Inventory());
            var runner = new ScriptRunner(ScriptLibrary.Default, host);
            runner.Start(ScriptLibrary.Default.Find("Board", "hub_town")!);
            runner.RunToEnd();
            Assert.Empty(host.Problems);
            Assert.Contains("open MissionBoard", host.Log);
            Assert.Equal(MissionWeek.Draw(Monday, Two).Select(Key), host.Missions!.Missions.Select(Key));
        }
        finally
        {
            GameClock.FixedDate = null;
        }
    }

    [Fact]
    public void AWeeksFiveAreDoneAcrossTwoRegionsAndTheirPennantsRankTheHall()
    {
        var board = new MissionBoard();
        board.Refresh(Monday, Two);
        Assert.Equal(2, board.Missions.Select(m => m.Region).Distinct().Count());
        for (int k = 0; k < board.Missions.Count; k++)
        {
            var m = board.Missions[k];
            if (m.IsTimed) Assert.True(board.Begin(k));
            // A timed one within its minutes: a minute of play in its region before each
            for (int n = 0; n < m.Goal; n++)
            {
                if (m.IsTimed) Assert.False(board.Tick(60f / Math.Max(1, m.Goal), m.Region));
                board.Tell(EventFor(m, m.Region));
            }
            Assert.True(m.IsDone, Key(m));
        }
        // Claimed through the script: the pennants go to Pennant Hall and lift its rank
        var story = new StoryState();
        foreach (string id in Two) story.CompleteRegion(id);
        var host = new HeadlessScriptHost(story, new Party(), new Inventory()) { Missions = board };
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptParser.Parse("test", "script Claim\n  missions claim\n  end\n")[0]);
        runner.RunToEnd();
        int total = board.Missions.Sum(m => m.Reward);
        Assert.Equal(total, runner.Result);
        Assert.Equal(total, host.Plaza!.Earned);
        Assert.True(host.Plaza.Rank >= 2, $"{total} pennants");
        Assert.Equal(0, board.Unclaimed);
    }
}
