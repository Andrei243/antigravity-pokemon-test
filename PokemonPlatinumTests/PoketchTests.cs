using System;
using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.PoketchApps;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// The Pokétch's frame and its clock apps (plan 06 · R14b): what it keeps, the apps made afresh, the cursor that
/// touches them, the analog watch, the calendar, the link searcher, and the president who gives four of them.
/// </summary>
public class PoketchTests
{
    private static (Poketch Poketch, PoketchContext Context) WithApps(params PoketchApp[] apps)
    {
        var poketch = new Poketch { Enabled = true };
        foreach (var app in apps) poketch.Register(app);
        return (poketch, new PoketchContext { Poketch = poketch, Rng = new Random(1), Now = new DateTime(2009, 3, 22, 15, 30, 0) });
    }

    [Fact]
    public void EveryAppGivenSomewhereRunsAndTheTwoNobodyGivesDoNot()
    {
        foreach (var app in Enum.GetValues<PoketchApp>())
        {
            var state = PoketchAppState.Create(app);
            Assert.Equal(app, state.App);
            Assert.Equal(app is not (PoketchApp.Stopwatch or PoketchApp.AlarmClock), Poketch.Runs(app));
        }
    }

    [Fact]
    public void AnAppIsMadeAfreshEachTimeItComesUp()
    {
        var (poketch, context) = WithApps(PoketchApp.DigitalWatch, PoketchApp.Pedometer);
        var watch = Assert.IsType<DigitalWatchApp>(poketch.State);
        watch.Press(0, context);
        Assert.True(watch.Backlight);
        Assert.Same(watch, poketch.State);

        poketch.Next();
        Assert.IsType<PedometerApp>(poketch.State);
        poketch.Previous();
        Assert.False(Assert.IsType<DigitalWatchApp>(poketch.State).Backlight);
    }

    [Fact]
    public void TheHistoryKeepsTheLastTwelveAndASaveKeepsEverything()
    {
        var (poketch, _) = WithApps(PoketchApp.PokemonHistory, PoketchApp.ColorChanger);
        string[] species = { "Bidoof", "Starly", "Shinx", "Kricketot", "Budew", "Zubat", "Geodude", "Machop", "Psyduck", "Ponyta", "Abra", "Buneary", "Pachirisu", "Buizel" };
        foreach (string s in species) poketch.Remember(new Pokemon(PokemonDatabase.Get(s)!, 5));
        Assert.Equal(Poketch.HistoryLength, poketch.History.Count);
        Assert.Equal(species.Skip(2), poketch.History);

        poketch.ScreenColor = 5;
        poketch.Keep(PoketchApp.Calendar, new() { 3, 0b101 });
        var again = new Poketch();
        again.Load(poketch.Save());
        Assert.Equal(poketch.History, again.History);
        Assert.Equal(5, again.ScreenColor);
        Assert.Equal(new[] { 3, 0b101 }, again.Recall(PoketchApp.Calendar));
        Assert.Equal(poketch.Apps, again.Apps);
    }

    [Fact]
    public void TheCursorFollowsARowOrAColumnAndTouchesWhatItIsOn()
    {
        var (poketch, context) = WithApps(PoketchApp.Calendar);
        var view = new PoketchView();
        Assert.True(view.TakeInHand(poketch, context));
        Assert.True(view.Out);
        Assert.Equal(1, view.Cursor(poketch, context)!.Value.Id);

        view.MoveCursor(poketch, context, 1, 0);
        Assert.Equal(2, view.Cursor(poketch, context)!.Value.Id);
        view.MoveCursor(poketch, context, 0, 1);
        Assert.Equal(9, view.Cursor(poketch, context)!.Value.Id);
        // Nothing lies before the first day: the cursor stays
        view.MoveCursor(poketch, context, 0, -1);
        view.MoveCursor(poketch, context, 0, -1);
        Assert.Equal(2, view.Cursor(poketch, context)!.Value.Id);

        view.Touch(poketch, context);
        Assert.True(((CalendarApp)poketch.State!).IsMarked(3, 2));
        view.LetGo();
        Assert.Null(view.Cursor(poketch, context));
    }

    [Fact]
    public void AnAppWithNothingToTouchIsNotTakenInHand()
    {
        var (poketch, context) = WithApps(PoketchApp.PartyStatus);
        Assert.False(new PoketchView().TakeInHand(poketch, context));
        Assert.False(new PoketchView().TakeInHand(new Poketch(), context));
    }

    [Fact]
    public void ThePedometersButtonPutsTheCountBackToNought()
    {
        var (poketch, context) = WithApps(PoketchApp.Pedometer);
        for (int i = 0; i < 40; i++) poketch.Step();
        poketch.State!.Press(PedometerApp.Reset.Id, context);
        Assert.Equal(0, poketch.Steps);
    }

    [Theory]
    [InlineData(15, 30, 17, 30)]
    [InlineData(0, 0, 0, 0)]
    [InlineData(12, 59, 4, 59)]
    [InlineData(23, 11, 55, 11)]
    public void TheAnalogWatchsHandsStepAsTheOriginalsDo(int hour, int minute, int hourHand, int minuteHand) =>
        Assert.Equal((hourHand, minuteHand), AnalogWatchApp.Hands(new DateTime(2009, 3, 22, hour, minute, 0)));

    [Fact]
    public void TheCalendarMarksADayAndForgetsLastMonthsMarks()
    {
        var (poketch, context) = WithApps(PoketchApp.Calendar);
        var calendar = (CalendarApp)poketch.State!;
        // March 2009 began on a Sunday
        Assert.Equal(0, CalendarApp.FirstCell(context.Now));
        Assert.Equal(31, calendar.Buttons(context).Count);
        calendar.Press(22, context);
        calendar.Press(5, context);
        calendar.Press(5, context);
        Assert.True(calendar.IsMarked(3, 22));
        Assert.False(calendar.IsMarked(3, 5));

        poketch.Close();
        Assert.True(((CalendarApp)poketch.State!).IsMarked(3, 22));

        context.Now = new DateTime(2009, 4, 1);
        Assert.Equal(3, CalendarApp.FirstCell(context.Now));
        ((CalendarApp)poketch.State!).Press(1, context);
        Assert.False(((CalendarApp)poketch.State!).IsMarked(3, 22));
        Assert.True(((CalendarApp)poketch.State!).IsMarked(4, 1));
    }

    [Fact]
    public void TheLinkSearcherSearchesAndFindsNobody()
    {
        var (poketch, context) = WithApps(PoketchApp.LinkSearcher);
        var searcher = (LinkSearcherApp)poketch.State!;
        searcher.Press(0, context);
        searcher.Update(1f, context);
        Assert.False(searcher.Searched);
        searcher.Update(1.5f, context);
        Assert.True(searcher.Searched);
        Assert.Equal(0, searcher.Found);
    }

    [Fact]
    public void ThePresidentGivesAnAppForTheFirstThirdFifthAndSeventhBadge()
    {
        var story = new StoryState();
        var host = new HeadlessScriptHost(story);
        host.Poketch.Enabled = true;
        void Talk()
        {
            var runner = new ScriptRunner(ScriptLibrary.Default, host);
            runner.Start(ScriptLibrary.Default.Find("President", "PoketchCompany")!, null);
            runner.RunToEnd();
        }

        Talk();
        Assert.Empty(host.Poketch.Apps);
        story.GiveBadge(Badge.Coal);
        Talk();
        Talk();
        Assert.Equal(new[] { PoketchApp.MemoPad }, host.Poketch.Apps);

        foreach (var badge in new[] { Badge.Forest, Badge.Cobble, Badge.Fen, Badge.Relic, Badge.Mine, Badge.Icicle }) story.GiveBadge(badge);
        for (int i = 0; i < 4; i++) Talk();
        Assert.Equal(new[] { PoketchApp.MemoPad, PoketchApp.MarkingMap, PoketchApp.LinkSearcher, PoketchApp.MoveTester }, host.Poketch.Apps);
    }
}
