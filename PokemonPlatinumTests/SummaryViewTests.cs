using System.Linq;
using Xunit;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI;
using static PokemonPlatinumTests.CoreScenario;

namespace PokemonPlatinumTests;

/// <summary>The summary's view (plan 08 · P3): its pages, and the marks set on the first.</summary>
[Collection("Shared clocks and dice")]
public class SummaryViewTests
{
    private static (PartyScreen Screen, Party Party) Open(params Pokemon[] team)
    {
        var party = new Party();
        foreach (var p in team) party.Add(p);
        var screen = new PartyScreen();
        screen.Open();
        screen.Confirm(party);
        screen.Confirm(party);
        Assert.True(screen.ShowSummary);
        return (screen, party);
    }

    [Fact]
    public void ASummaryOpensOnTheInfoPageAndTurnsThroughThePagesInOrder()
    {
        var (screen, _) = Open(Of(Gender.Male, "Bidoof", 10));
        Assert.Equal(SummaryPage.Info, screen.View.Page);
        var seen = new System.Collections.Generic.List<SummaryPage>();
        for (int i = 0; i < screen.View.Pages.Count; i++)
        {
            seen.Add(screen.View.Page);
            screen.TurnSummaryPage(1);
        }
        Assert.Equal(screen.View.Pages, seen);
        // Round once, back where it began
        Assert.Equal(SummaryPage.Info, screen.View.Page);
        Assert.Equal(new[] { SummaryPage.Info, SummaryPage.Memo, SummaryPage.Skills, SummaryPage.BattleMoves, SummaryPage.ContestMoves, SummaryPage.Ribbons }, screen.View.Pages);
    }

    [Fact]
    public void ThePageNumbersAreTheOnesTheScreenAlwaysHad()
    {
        Assert.Equal((0, 1, 2), ((int)SummaryPage.Info, (int)SummaryPage.Condition, (int)SummaryPage.Ribbons));
        Assert.Equal((PartyScreen.StatsPage, PartyScreen.ConditionPage, PartyScreen.RibbonsPage), (0, 1, 2));
    }

    [Fact]
    public void TheMarksAreSetAndClearedOnTheInfoPage()
    {
        var lead = Of(Gender.Male, "Bidoof", 10);
        var (screen, _) = Open(lead);
        Assert.Equal(0, lead.Marks);
        Assert.True(screen.EditMarks(lead));
        Assert.Equal(0, screen.MarkCursor);
        screen.View.ToggleMark(lead);
        screen.View.MoveMark(1);
        screen.View.MoveMark(1);
        screen.View.ToggleMark(lead);
        Assert.Equal(0b101, lead.Marks);
        Assert.True(SummaryView.HasMark(lead, 2));
        Assert.False(SummaryView.HasMark(lead, 1));
        // The cursor goes round the six
        for (int i = 0; i < 4; i++) screen.View.MoveMark(1);
        Assert.Equal(0, screen.MarkCursor);
        screen.View.MoveMark(-1);
        Assert.Equal(5, screen.MarkCursor);
        screen.View.ToggleMark(lead);
        screen.View.ToggleMark(lead);
        Assert.Equal(0b101, lead.Marks);
        screen.View.LeaveMarks();
        Assert.Equal(-1, screen.MarkCursor);
    }

    [Fact]
    public void MarksAreSetOnlyOnTheInfoPageAndNeverOnAnEgg()
    {
        var lead = Of(Gender.Male, "Bidoof", 10);
        var (screen, _) = Open(lead);
        screen.TurnSummaryPage(1);
        Assert.False(screen.EditMarks(lead));
        Assert.Equal(-1, screen.MarkCursor);

        var egg = Of(Gender.Male, "Bidoof", 1);
        egg.IsEgg = true;
        var (eggScreen, _) = Open(egg);
        Assert.False(eggScreen.EditMarks(egg));
    }

    [Fact]
    public void TurningThePageOrMovingToAnotherPokemonPutsTheCursorAway()
    {
        var a = Of(Gender.Male, "Bidoof", 10);
        var b = Of(Gender.Male, "Starly", 10);
        var (screen, party) = Open(a, b);
        screen.EditMarks(a);
        screen.TurnSummaryPage(1);
        Assert.Equal(-1, screen.MarkCursor);
        screen.TurnSummaryPage(-1);
        screen.EditMarks(a);
        screen.MoveCursor(0, 1, party.Count);
        Assert.Equal(-1, screen.MarkCursor);
    }

    [Fact]
    public void TheMarksAreWhatTheBoxesKeep()
    {
        // The PC's marking menu reads the same bits (Markings: circle, triangle, square, heart, star, diamond)
        var p = Of(Gender.Male, "Bidoof", 10);
        p.Marks = (int)(Markings.Heart | Markings.Star);
        Assert.True(SummaryView.HasMark(p, 3));
        Assert.True(SummaryView.HasMark(p, 4));
        Assert.Equal(2, Enumerable.Range(0, 6).Count(m => SummaryView.HasMark(p, m)));
    }
}
