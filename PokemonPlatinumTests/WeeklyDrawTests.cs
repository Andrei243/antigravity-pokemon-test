using System;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>Johto's lucky number draw (plan 42 · PS3) beside Jubilife's daily ticket, which must not move.</summary>
public class WeeklyDrawTests
{
    [Fact]
    public void TheWeeksTicketIsTheSameAllWeekAndMovesOnTheNext()
    {
        var monday = new DateOnly(2026, 11, 9);
        Assert.Equal(DayOfWeek.Monday, monday.DayOfWeek);
        int ticket = Lottery.WeeklyTicket(monday, 12345);
        for (int d = 0; d < 7; d++) Assert.Equal(ticket, Lottery.WeeklyTicket(monday.AddDays(d), 12345));
        Assert.Equal(Lottery.WeekOf(monday), Lottery.WeekOf(monday.AddDays(6)));
        Assert.Equal(Lottery.WeekOf(monday) + 1, Lottery.WeekOf(monday.AddDays(7)));
        Assert.NotEqual(ticket, Lottery.WeeklyTicket(monday.AddDays(7), 12345));
        Assert.NotEqual(ticket, Lottery.WeeklyTicket(monday.AddDays(-1), 12345));
        // Another game draws another number the same week
        Assert.NotEqual(ticket, Lottery.WeeklyTicket(monday, 54321));
        // A ticket is five digits at most
        for (int w = 0; w < 500; w++) Assert.InRange(Lottery.WeeklyTicket(w, 7), 0, 65535);
        Assert.Equal("00042", Lottery.TicketText(42));
    }

    [Fact]
    public void DigitsAreMatchedAsJubilifesAre()
    {
        Assert.Equal(0, Lottery.Matching(12345, 12340));
        Assert.Equal(1, Lottery.Matching(12345, 99995));
        Assert.Equal(3, Lottery.Matching(12345, 99345));
        Assert.Equal(5, Lottery.Matching(12345, 12345));
        Assert.Equal(2, Lottery.Matching(45, 845));  // matched from the right, the shorter padded with noughts... until they part
    }

    [Fact]
    public void TheWeeksPrizesAreHeartGoldsWhichAreJubilifes()
    {
        for (int digits = 0; digits <= 5; digits++) Assert.Equal(Lottery.Prize(digits), Lottery.WeeklyPrize(digits));
        Assert.Null(Lottery.WeeklyPrize(0));
        Assert.Equal("Master Ball", Lottery.WeeklyPrize(5));
    }

    [Fact]
    public void SinnohsDailyTicketAndPrizesHaveNotMoved()
    {
        // The day's number × 1,103,515,245 + 12,345, its high half (the original's bug)
        Assert.Equal((int)(unchecked(7u * 1103515245u + 12345u) >> 16), Lottery.TicketOf(7));
        Assert.Equal(new[] { null, "Ultra Ball", "PP Up", "Exp. Share", "Max Revive", "Master Ball" },
            Enumerable.Range(0, 6).Select(Lottery.Prize));
        var draw = Lottery.Check(12345, 99345, Array.Empty<Pokemon>(), Array.Empty<Pokemon>());
        Assert.Equal(0, draw.Digits);
    }

    [Fact]
    public void TheLuckyNumberStationIsOnJohtosDialAlone()
    {
        Assert.Equal(RadioStation.LuckyNumber, Enum.GetValues<RadioStation>().Last());
        var spot = RadioDial.SpotsOf(RadioSignal.Johto).Single(s => s.Slot == 9);
        Assert.Equal(RadioStation.LuckyNumber, RadioDial.StationOf(spot.Slot, 9));
        Assert.Equal(RadioStation.LuckyNumber, RadioDial.StationOf(spot.Slot, 22));
        // The cursor reaches its middle in steps from the dial's, and its ring touches no other station's
        var dial = new RadioDial { Signal = RadioSignal.Johto };
        int dx = (spot.X - RadioDial.CenterX) / RadioDial.Step, dy = (spot.Y - RadioDial.CenterY) / RadioDial.Step;
        for (int k = 0; k < Math.Abs(dx); k++) dial.Move(Math.Sign(dx), 0);
        for (int k = 0; k < Math.Abs(dy); k++) dial.Move(0, Math.Sign(dy));
        Assert.Equal((spot, true), dial.Tuned());
        foreach (var other in RadioDial.SpotsOf(RadioSignal.Johto).Where(s => s.Slot != 9))
        {
            double d = Math.Sqrt((other.X - spot.X) * (other.X - spot.X) + (other.Y - spot.Y) * (other.Y - spot.Y));
            Assert.True(d > other.Heard + spot.Heard, $"the lucky number's ring meets slot {other.Slot}'s");
        }
        Assert.DoesNotContain(RadioDial.SpotsOf(RadioSignal.Kanto), s => s.Slot == 9);
        // Its line is one of the three, with the week's number
        string line = RadioShows.LuckyLine(new Random(1), 4321);
        Assert.Contains(line, Lottery.RadioLines(4321));
        Assert.Contains(Lottery.RadioLines(4321), l => l.Contains("04321"));
    }

    [Fact]
    public void AWeeksFlagsGoOnlyWhenTheWeekTurnsOver()
    {
        var story = new PokemonPlatinumEngine.Story.StoryState();
        var encounters = new SpecialEncounters();
        var monday = new DateOnly(2026, 11, 9);
        story.Set("FLAG_WEEKLY_CLAIMED_LUCKY_NUMBER");
        DailyEvents.DaysPass(1, story, encounters, null, new Random(1), today: monday.AddDays(3));
        Assert.True(story.Has("FLAG_WEEKLY_CLAIMED_LUCKY_NUMBER"));
        DailyEvents.DaysPass(1, story, encounters, null, new Random(1), today: monday.AddDays(7));
        Assert.False(story.Has("FLAG_WEEKLY_CLAIMED_LUCKY_NUMBER"));
        // Many days at once that cross a boundary clear it too
        story.Set("FLAG_WEEKLY_CLAIMED_LUCKY_NUMBER");
        DailyEvents.DaysPass(9, story, encounters, null, new Random(1), today: monday.AddDays(16));
        Assert.False(story.Has("FLAG_WEEKLY_CLAIMED_LUCKY_NUMBER"));
    }

    [Fact]
    public void TheRadioTowersCounterGivesTheWeeksPrizeOnce()
    {
        var scripts = PokemonPlatinumEngine.Story.ScriptLibrary.Default;
        var host = new PokemonPlatinumEngine.Story.HeadlessScriptHost { TrainerNumber = 0 };
        int ticket = Lottery.WeeklyTicket(host.Today, host.TrainerNumber);
        // The last two digits the ticket's, the third another
        int id = 50000 + (ticket / 100 % 10 + 1) % 10 * 100 + ticket % 100;
        var traded = new Pokemon(PokemonDatabase.Get("Sentret")!, 9) { OriginalTrainer = new TrainerMark("Pen Pal", id, PlayerLook.Girl) };
        host.Party.Add(new Pokemon(PokemonDatabase.Get("Chikorita")!, 9));
        host.Party.Add(traded);
        Assert.Equal(2, Lottery.Matching(ticket, id));
        Assert.True(Lottery.Matching(ticket, 0) < 2);

        void Run()
        {
            var runner = new PokemonPlatinumEngine.Story.ScriptRunner(scripts, host);
            runner.Start(scripts.Find("LuckyNumber", "goldenrod_radio_tower_1f")!, new NPC { Name = "Man", Key = "d23r0101_gsman1" });
            runner.RunToEnd();
            Assert.Empty(host.Problems);
        }

        Run();
        Assert.Contains(host.Transcript, l => l.Text.Contains(Lottery.TicketText(ticket)));
        Assert.Contains(host.Transcript, l => l.Text.Contains("Sentret") && l.Text.Contains("on your team"));
        Assert.Equal(1, host.Bag.GetQuantity(ItemDatabase.Get("PP Up")!));
        Assert.True(host.Story.Has("FLAG_WEEKLY_CLAIMED_LUCKY_NUMBER"));
        // Once a week
        Run();
        Assert.Equal(1, host.Bag.GetQuantity(ItemDatabase.Get("PP Up")!));
        host.Story.ClearWeekly();
        Run();
        Assert.Equal(2, host.Bag.GetQuantity(ItemDatabase.Get("PP Up")!));
        // Saying no checks nothing
        var declined = new PokemonPlatinumEngine.Story.HeadlessScriptHost();
        declined.Answers.Enqueue(1);
        var r = new PokemonPlatinumEngine.Story.ScriptRunner(scripts, declined);
        r.Start(scripts.Find("LuckyNumber", "goldenrod_radio_tower_1f")!, new NPC { Name = "Man", Key = "d23r0101_gsman1" });
        r.RunToEnd();
        Assert.False(declined.Story.Has("FLAG_WEEKLY_CLAIMED_LUCKY_NUMBER"));
    }

    [Fact]
    public void TheRadioReadsTheWeeksNumberOut()
    {
        var lines = Lottery.RadioLines(42);
        Assert.Equal(3, lines.Count);
        Assert.Contains("00042", lines[1]);
    }
}
