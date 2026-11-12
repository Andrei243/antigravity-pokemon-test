using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>Poké Jobs (plan 15 · E3): the board, its clock and its pay, the save, the screen and the PC's door.</summary>
public class PokeJobsTests
{
    private static readonly DateTime Evening = new(2026, 6, 1, 20, 0, 0, DateTimeKind.Utc);

    private static Pokemon Bidoof(int level = 10) => new(PokemonDatabase.Get("Bidoof")!, level, Gender.Male, Nature.Hardy, false);

    private static string Saved(Pokemon p) => JsonSerializer.Serialize(SavedPokemonData.FromPokemon(p));

    [Fact]
    public void EightHoursOfErrandsPayALevelsWorthAndNoMore()
    {
        var board = new JobBoard();
        var bidoof = Bidoof();
        var job = board.Post(bidoof, JobKind.Errand, 8, Evening)!;
        var result = board.Collect(job, Evening.AddHours(8), new Random(1), out var refusal)!;
        Assert.Equal(JobRefusal.None, refusal);
        Assert.Equal(8, result.Hours);
        Assert.Equal(11, result.Pokemon.Level);
        Assert.Equal(1, result.LevelsGained);
        Assert.Equal(result.Pokemon.ExpForCurrentLevel, result.Pokemon.CurrentExp);
        Assert.True(board.IsEmpty);

        // A whole day pays no more than the level's worth either
        var day = board.Post(Bidoof(), JobKind.Errand, 24, Evening)!;
        var long_ = board.Collect(day, Evening.AddDays(3), new Random(1), out _)!;
        Assert.Equal(24, long_.Hours);
        Assert.Equal(11, long_.Pokemon.Level);
        Assert.Equal(1, long_.LevelsGained);

        // Half an errand's EXP in the orchard: four hours there pay a quarter of a level
        var orchard = board.Post(Bidoof(), JobKind.Orchard, 4, Evening)!;
        var picked = board.Collect(orchard, Evening.AddHours(5), new Random(1), out _)!;
        int span = Pokemon.GetExpForLevel(11, PokemonDatabase.Get("Bidoof")!.GrowthRate) - Pokemon.GetExpForLevel(10, PokemonDatabase.Get("Bidoof")!.GrowthRate);
        Assert.Equal(span * 4 / 16, picked.Exp);
        Assert.Equal(2, picked.Berries.Sum(b => b.Count));
        Assert.All(picked.Berries, b => Assert.NotNull(ItemDatabase.Get(b.Item)));
        Assert.Equal(StatType.HP, picked.EffortStat);
        Assert.Equal(4, picked.Effort);
    }

    [Fact]
    public void OnlyWholeHoursArePaidAndNeverMoreThanAskedFor()
    {
        var job = new Job { Started = Evening, Ends = Evening.AddHours(6), Hours = 6 };
        Assert.Equal(0, job.HoursDone(Evening.AddMinutes(59)));
        Assert.Equal(3, job.HoursDone(Evening.AddHours(3.9)));
        Assert.Equal(6, job.HoursDone(Evening.AddDays(2)));
        Assert.Equal(TimeSpan.FromHours(6), job.Left(Evening));
        Assert.Equal(TimeSpan.Zero, job.Left(Evening.AddHours(7)));
    }

    [Fact]
    public void AClockSetBackPaysNothingAndKeepsTheJob()
    {
        var board = new JobBoard();
        var job = board.Post(Bidoof(), JobKind.Errand, 8, Evening)!;
        Assert.Null(board.Collect(job, Evening.AddHours(-3), new Random(1), out var refusal));
        Assert.Equal(JobRefusal.ClockBack, refusal);
        Assert.Single(board.Jobs);
        Assert.Equal(0, job.HoursDone(Evening.AddHours(-3)));
    }

    [Fact]
    public void ABoardHoldsSixAndTakesNoEggButToTheNursery()
    {
        var board = new JobBoard();
        for (int i = 0; i < JobBoard.Slots; i++) Assert.NotNull(board.Post(Bidoof(), JobKind.Errand, 1, Evening));
        Assert.Equal(JobRefusal.Full, board.WhyNot(Bidoof(), 1));
        Assert.Null(board.Post(Bidoof(), JobKind.Errand, 1, Evening));
        var empty = new JobBoard();
        Assert.Equal(JobRefusal.NoHours, empty.WhyNot(Bidoof(), 0));
        Assert.Equal(JobRefusal.NoHours, empty.WhyNot(Bidoof(), 25));
    }

    [Fact]
    public void APokemonPostedIsInNeitherTeamNorBoxAndComesBackWhole()
    {
        var boxes = new PcBoxes();
        var bidoof = Bidoof(12);
        bidoof.Friendship = 90;
        boxes.Store(bidoof);
        string before = Saved(bidoof);
        var board = new JobBoard();
        var screen = new JobsScreen();
        screen.Open(board, boxes, new Inventory(), new Random(1));
        screen.Confirm(Evening);                        // an empty place: choose
        Assert.Equal(JobsPhase.Choose, screen.Phase);
        screen.Confirm(Evening);                        // the Bidoof
        Assert.Equal(JobsPhase.Setup, screen.Phase);
        screen.Move(1);                                 // the hours
        screen.Sideways(1);
        Assert.Equal(9, screen.Hours);
        screen.Confirm(Evening);                        // sent
        Assert.Equal(JobsPhase.Board, screen.Phase);
        Assert.Equal(0, boxes.Count);
        Assert.Single(board.Jobs);

        // Brought back at once it is paid for no hour, and is exactly what it was
        screen.Confirm(Evening.AddMinutes(30));
        Assert.Equal(JobsPhase.Result, screen.Phase);
        Assert.Equal(1, boxes.Count);
        Assert.Equal(before, Saved(boxes.All.Single()));
        Assert.NotNull(screen.TakeCollected());
        Assert.Null(screen.TakeCollected());
        screen.Confirm(Evening);
        Assert.Equal(JobsPhase.Board, screen.Phase);
        screen.Cancel();
        Assert.False(screen.IsActive);
    }

    [Fact]
    public void ASaveWithAJobLoadsADayLaterAndCollects()
    {
        var board = new JobBoard();
        board.Post(Bidoof(), JobKind.HotSpring, 10, Evening);
        var save = new SaveData { Jobs = board.ToSaved(), LastSaved = Evening };
        var read = JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(save))!;
        var loaded = new JobBoard();
        loaded.Restore(read.Jobs);
        var job = Assert.Single(loaded.Jobs);
        Assert.Equal(Evening, job.Started.ToUniversalTime());
        var result = loaded.Collect(job, Evening.AddDays(1), new Random(2), out _)!;
        Assert.Equal(10, result.Hours);
        Assert.Equal(20, result.Friendship);
        Assert.Empty(result.Berries);
        Assert.Equal(StatType.SpDefense, result.EffortStat);

        // An older save reads as no jobs
        var older = JsonSerializer.Deserialize<SaveData>("{}")!;
        var none = new JobBoard();
        none.Restore(older.Jobs);
        Assert.True(none.IsEmpty);
        Assert.Null(none.ToSaved());
    }

    [Fact]
    public void ABidoofLeftOnAnErrandOvernightComesBackALevelHigherFromTheBoard()
    {
        var boxes = new PcBoxes();
        boxes.Store(Bidoof(10));
        var board = new JobBoard();
        var bag = new Inventory();
        var screen = new JobsScreen();
        screen.Open(board, boxes, bag, new Random(3));
        screen.Confirm(Evening);
        screen.Confirm(Evening);
        screen.Confirm(Evening);                        // an errand of eight hours, the board's first choice
        Assert.Equal((JobKind.Errand, 8), (board.Jobs[0].Kind, board.Jobs[0].Hours));
        screen.Close();

        screen.Open(board, boxes, bag, new Random(3));
        screen.Confirm(Evening.AddHours(11));
        var back = screen.TakeCollected()!;
        Assert.Equal(11, back.Pokemon.Level);
        Assert.Equal(11, boxes.All.Single().Level);
        Assert.Equal(back.Berries.Sum(b => b.Count), bag.GetPocketItems(ItemPocket.Berries).Sum(i => i.Quantity));
    }

    [Fact]
    public void ThePcOffersJobsUnderTheModernRulesAndNothingNewUnderPlatinums()
    {
        static HeadlessScriptHost Run(HeadlessScriptHost host)
        {
            var runner = new ScriptRunner(ScriptLibrary.Default, host);
            runner.Start(ScriptLibrary.Default.Find("PC", "common")!, null);
            runner.RunToEnd();
            Assert.Empty(host.Problems);
            return host;
        }

        // Platinum's rules: the boxes at once, nothing asked
        var platinum = Run(new HeadlessScriptHost());
        Assert.Contains("open Pc", platinum.Log);
        Assert.Empty(platinum.Asked);

        // The modern rules: three ways (storage, jobs, off)
        for (int answer = 0; answer < 3; answer++)
        {
            var host = new HeadlessScriptHost { Modern = true };
            host.Answers.Enqueue(answer);
            Run(host);
            Assert.Single(host.Asked);
            Assert.Equal(answer == 0, host.Log.Contains("open Pc"));
            Assert.Equal(answer == 1, host.Log.Contains("open Jobs"));
        }
    }
}
