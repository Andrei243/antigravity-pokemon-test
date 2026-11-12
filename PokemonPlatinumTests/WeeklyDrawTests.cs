using System;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Models;
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
    public void TheRadioReadsTheWeeksNumberOut()
    {
        var lines = Lottery.RadioLines(42);
        Assert.Equal(3, lines.Count);
        Assert.Contains("00042", lines[1]);
    }
}
