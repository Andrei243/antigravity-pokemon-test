using System;
using System.IO;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.Frontier;
using PokemonPlatinumEngine.UI;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 13 · V14: the daily run, a round of the Battle Factory drawn from the day's date. A date gives the same run
/// however often and wherever it is drawn (nothing of <see cref="Dice"/> or the computer's clock goes in), other dates
/// give other runs, the round follows the day of the week, and <c>daily.json</c> keeps each day's first run alone.
/// </summary>
[Collection("Shared clocks and dice")]
public class DailyRunTests
{
    private static readonly DateTime Day = new(2026, 11, 3);   // a Tuesday

    // What a run is to whoever compares two: its seed, its six (set, IVs, personality, number) and its seven trainers
    private static string Fingerprint(DailyRun run) =>
        run.Seed + "|" + string.Join(",", run.Offer.Select(r => $"{r.Set}/{r.Ivs}/{r.Personality}/{r.OtId}")) + "|" + string.Join(",", run.Trainers);

    [Fact]
    public void ADateGivesTheSameRunWhateverTheGamesChanceSays()
    {
        Dice.Seed(1);
        var first = DailyRun.For(Day);
        Dice.Seed(987654);
        _ = Dice.Shared.Next();
        var second = DailyRun.For(Day);
        Dice.Seed(null);
        Assert.Equal(Fingerprint(first), Fingerprint(second));
        Assert.Equal(DailyRun.Battles, first.Trainers.Count);
        Assert.Equal(BattleFactoryChallenge.Offered, first.Offer.Count);
        Assert.Equal(first.Trainers.Count, first.Trainers.Distinct().Count());
        // A seed is the date's and the data's alone: the same on every machine with the same data
        Assert.Equal(DailyRun.SeedFor(Day, "abc"), DailyRun.SeedFor(Day, "abc"));
        Assert.NotEqual(DailyRun.SeedFor(Day, "abc"), DailyRun.SeedFor(Day, "abd"));
        Assert.Equal(DailyRun.SeedFor(Day, GameDataFiles.Version), first.Seed);
    }

    [Fact]
    public void OtherDatesGiveOtherRuns()
    {
        var seen = Enumerable.Range(0, 14).Select(i => DailyRun.For(Day.AddDays(i))).ToList();
        Assert.Equal(seen.Count, seen.Select(r => r.Seed).Distinct().Count());
        Assert.Equal(seen.Count, seen.Select(Fingerprint).Distinct().Count());
        // The same day of the week a week apart is the same round and still another run
        Assert.Equal(seen[0].Factory.Round, seen[7].Factory.Round);
        Assert.NotEqual(Fingerprint(seen[0]), Fingerprint(seen[7]));
    }

    [Fact]
    public void TheRoundIsTheDayOfTheWeeks()
    {
        Assert.Equal(0, DailyRun.RoundFor(new DateTime(2026, 11, 2)));   // Monday
        Assert.Equal(1, DailyRun.RoundFor(Day));                          // Tuesday
        Assert.Equal(6, DailyRun.RoundFor(new DateTime(2026, 11, 8)));   // Sunday
        Assert.Equal(1, DailyRun.For(Day).Factory.Round);
        // Wednesday's last trainer and Sunday's are the Factory Head, as the 21st and the 49th of a streak
        var (silver, gold) = FrontierBrains.Trainers(FrontierFacility.Factory);
        Assert.Equal(silver, DailyRun.For(new DateTime(2026, 11, 4)).Trainers[^1]);
        Assert.Equal(gold, DailyRun.For(new DateTime(2026, 11, 8)).Trainers[^1]);
    }

    [Fact]
    public void TodayIsTheFixedDateOrTheDayInUtc()
    {
        var keptDate = GameClock.FixedDate;
        var keptWall = WallClock.Fixed;
        try
        {
            GameClock.FixedDate = null;
            WallClock.Fixed = new DateTime(2026, 7, 1, 23, 30, 0);
            Assert.Equal(new DateTime(2026, 7, 1), DailyRun.Today());
            GameClock.FixedDate = new DateTime(2026, 6, 1);
            Assert.Equal(new DateTime(2026, 6, 1), DailyRun.Today());
        }
        finally
        {
            GameClock.FixedDate = keptDate;
            WallClock.Fixed = keptWall;
        }
    }

    [Fact]
    public void ARunIsTheFactorysRoundRentedFoughtAndTraded()
    {
        var run = DailyRun.For(Day);
        Assert.False(run.Rented);
        run.Rent(new[] { 0, 2, 4 });
        Assert.True(run.Rented);
        Assert.Equal(new[] { run.Offer[0].Set, run.Offer[2].Set, run.Offer[4].Set }, run.Factory.Team.Select(r => r.Set));
        run.Ready();
        var foe = run.NextFoe();
        Assert.Equal(BattleFactoryChallenge.TeamSize, foe.Party.Members.Count);
        Assert.All(run.PlayerParty().Members, p => Assert.Equal(50, p.Level));
        Assert.Equal(FrontierDatabase.Trainer(run.Trainers[0]).Name, run.NextTrainer.Name);
        uint firstSeed = run.BattleSeed;

        run.Won();
        Assert.True(run.CanTrade);
        Assert.NotEqual(firstSeed, run.BattleSeed);
        var beaten = run.Factory.Foe[1].Set;
        run.Trade(0, 1);
        Assert.Equal(beaten, run.Factory.Team[0].Set);
        Assert.False(run.CanTrade);
        Assert.Throws<InvalidOperationException>(() => run.Trade(0, 0));
        run.Ready();
        Assert.Equal(FrontierDatabase.Trainer(run.Trainers[1]).Name, run.NextTrainer.Name);

        for (int i = 1; i < DailyRun.Battles; i++)
        {
            run.Ready();
            run.Won();
        }
        Assert.True(run.Cleared);
        Assert.True(run.Over);
        Assert.Throws<InvalidOperationException>(() => run.Won());
    }

    [Fact]
    public void TheSameChoicesMeetTheSameTrainersAndTheSameThreeOnTwoMachines()
    {
        static string Play()
        {
            var run = DailyRun.For(Day);
            run.Rent(new[] { 5, 1, 3 });
            var seen = "";
            for (int i = 0; i < 4; i++)
            {
                run.Ready();
                seen += run.NextFoe().Name + ":" + string.Join(",", run.Factory.Foe.Select(r => r.Set)) + ":" + run.BattleSeed + ";";
                run.Won();
            }
            return seen;
        }
        Dice.Seed(3);
        string a = Play();
        Dice.Seed(4);
        string b = Play();
        Dice.Seed(null);
        Assert.Equal(a, b);
    }

    [Fact]
    public void TheRecordsKeepEachDaysFirstRunAlone()
    {
        var records = new DailyRecords();
        var run = DailyRun.For(Day, counts: !records.Played(Day));
        Assert.True(run.Counts);
        run.Rent(new[] { 0, 1, 2 });
        Assert.False(records.Keep(run));               // kept at nought as the three are rented: no new best
        Assert.True(records.Played(Day));
        run.Won();
        run.Won();
        Assert.True(records.Keep(run));
        run.Lose();
        records.Keep(run);
        Assert.Equal(2, records.On(Day)!.Wins);
        Assert.True(records.On(Day)!.Over);

        // A second run the same day is practice: whatever it does, the day stays as it was
        var practice = DailyRun.For(Day, counts: !records.Played(Day));
        Assert.False(practice.Counts);
        practice.Rent(new[] { 0, 1, 2 });
        for (int i = 0; i < 5; i++) { practice.Ready(); practice.Won(); }
        Assert.False(records.Keep(practice));
        Assert.Equal(2, records.On(Day)!.Wins);
        Assert.Single(records.Days);
        Assert.Equal(2, records.Best);
        Assert.Equal(0, records.Clears);

        string folder = Path.Combine(Path.GetTempPath(), "daily-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            string path = Path.Combine(folder, DailyRecords.FileName);
            Assert.Empty(DailyRecords.Read(path).Days);
            DailyRecords.Write(path, records);
            var back = DailyRecords.Read(path);
            Assert.Equal("2026-11-03", back.Days.Single().Date);
            Assert.Equal(2, back.Days.Single().Wins);
            Assert.False(File.Exists(path + ".tmp"));
        }
        finally { Directory.Delete(folder, true); }
    }

    [Fact]
    public void TheRoomHasADailyRowAndAThirdRowBetweenItsBattles()
    {
        Assert.Contains(RoomEntry.Daily, BattleRoomScreen.Entries);
        var records = new DailyRecords();
        var room = new BattleRoomScreen();
        room.Open(Array.Empty<BuiltTeam>(), Array.Empty<PuzzleFile>(), new BattleRoomRecords(), records, Day);
        Assert.Equal(Day, room.Today);
        while (BattleRoomScreen.Entries[room.Cursor] != RoomEntry.Daily) room.Move(1);
        room.Confirm();
        Assert.IsType<BeginDaily>(room.TakeRequest());

        var run = DailyRun.For(Day);
        run.Rent(new[] { 0, 1, 2 });
        run.Ready();
        run.Won();
        room.AfterDailyBattle(run, false);
        Assert.Equal(RoomPage.Between, room.Page);
        Assert.Equal(3, room.Rows);
        room.Move(1);
        room.Confirm();
        Assert.IsType<TradeDaily>(room.TakeRequest());
        run.Trade(0, 0);
        room.AfterDailyBattle(run, false);
        room.Move(1);
        room.Confirm();
        Assert.Null(room.TakeRequest());                // no second trade before the next battle
        room.Move(1);
        room.Confirm();
        Assert.IsType<GiveUpRun>(room.TakeRequest());

        run.Lose();
        room.AfterDailyBattle(run, false);
        Assert.Equal(RoomPage.RunOver, room.Page);
        room.Confirm();
        Assert.Equal(RoomPage.Menu, room.Page);
        Assert.Equal(RoomEntry.Daily, BattleRoomScreen.Entries[room.Cursor]);
        Assert.Null(room.Daily);
    }
}
