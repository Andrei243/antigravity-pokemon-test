using System;
using System.Linq;
using Xunit;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumTests;

/// <summary>
/// Requests (plan 12 · Q21): each errand's state is worked out from the story whenever it is asked, so the save gains no
/// field but the one pinned; its steps name flags and variables the scripts set; it can be finished by playing its scripts;
/// and the start menu and the page follow the options' row.
/// </summary>
public class RequestsTests
{
    private static Request Campaign => Requests.Find("poketch")!;

    private static void Play(HeadlessScriptHost host, string name, params int[] answers)
    {
        foreach (int a in answers) host.Answers.Enqueue(a);
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find(name)!, null, null);
        runner.RunToEnd();
    }

    [Fact]
    public void EveryRequestHasAUniqueIdAndItsWordsInTheTable()
    {
        Assert.Equal(Requests.All.Count, Requests.All.Select(r => r.Id).Distinct().Count());
        foreach (var r in Requests.All)
        {
            Assert.NotEmpty(r.Steps);
            foreach (string words in new[] { r.Title, r.GiverName, r.PlaceName, r.Reward }.Concat(r.Steps.Select(r.StepLine)))
            {
                Assert.False(words.StartsWith('['), $"{r.Id} has a line missing from the table of words: {words}");
                Assert.NotEmpty(words);
            }
            // A step's line fits the pinned panel's one line, and every condition reads
            foreach (var step in r.Steps)
                Assert.True(PokemonPlatinumEngine.UI.Kit.TextMetrics.Measure(r.StepLine(step), 24, PokemonPlatinumEngine.UI.Kit.UiWeight.ExtraBold) < 900 - 124, step.Name);
            Assert.NotEmpty(Requests.ConditionsOf(r).ToList());
        }
    }

    [Fact]
    public void EveryFlagAndVariableARequestNamesIsSetBySomeScript()
    {
        var flags = ScriptLibrary.Default.FlagsWritten;
        var variables = ScriptLibrary.Default.VariablesWritten;
        foreach (var r in Requests.All)
            foreach (var c in Requests.ConditionsOf(r))
                Assert.True(c.IsFlag ? flags.Contains(c.Name) : variables.Contains(c.Name), $"{r.Id} asks {c.Name}, which no script sets");
    }

    [Fact]
    public void AConditionReadsAsAnIfWritesIt()
    {
        var story = new StoryState();
        Assert.False(Requests.Holds("flag FLAG_A", story));
        Assert.True(Requests.Holds("not flag FLAG_A", story));
        story.Set("FLAG_A");
        Assert.True(Requests.Holds("flag FLAG_A", story));
        story.SetVar("VAR_A", 2);
        Assert.True(Requests.Holds("var VAR_A >= 2", story));
        Assert.True(Requests.Holds("var VAR_A == 2", story));
        Assert.False(Requests.Holds("var VAR_A > 2", story));
        Assert.True(Requests.Holds("var VAR_A != 3", story));
        Assert.Throws<FormatException>(() => Requests.Holds("badge coal", story));
    }

    [Fact]
    public void TheCampaignFollowsTheFlags()
    {
        var story = new StoryState();
        Assert.Equal(RequestState.Unknown, Requests.State(Campaign, story));
        Assert.False(Requests.AnyKnown(story));
        // The president tells it
        story.SetVar("VAR_POKETCH_CAMPAIGN_STATE", 2);
        Assert.Equal(RequestState.Open, Requests.State(Campaign, story));
        Assert.Equal("coupon1", Requests.Next(Campaign, story)!.Name);
        story.Set("FLAG_RECEIVED_COUPON_1");
        Assert.Equal("coupon2", Requests.Next(Campaign, story)!.Name);
        story.Set("FLAG_RECEIVED_COUPON_3");
        story.Set("FLAG_RECEIVED_COUPON_2");
        Assert.Equal("president", Requests.Next(Campaign, story)!.Name);
        story.Set("FLAG_RECEIVED_POKETCH");
        Assert.Equal(RequestState.Done, Requests.State(Campaign, story));
        Assert.Null(Requests.Next(Campaign, story));
        Assert.Contains(Campaign, Requests.InState(story, RequestState.Done));
    }

    [Fact]
    public void ACouponWonBeforeThePresidentToldTheCampaignMakesItKnown()
    {
        // The first two clowns are in the street with the president, and the first asks no question of the campaign's state
        var story = new StoryState();
        story.Set("FLAG_RECEIVED_COUPON_1");
        Assert.Equal(RequestState.Open, Requests.State(Campaign, story));
        Assert.Equal("coupon2", Requests.Next(Campaign, story)!.Name);
    }

    [Fact]
    public void AnOlderSaveWithThePoketchReadsDoneWithNoMigration()
    {
        // The opening and the second chapter as an older story left them: the Pokétch had, no coupon kept, no field of Q21's
        var host = new HeadlessScriptHost();
        Play(host, "common.OpeningDone");
        host.Story.Set("FLAG_RECEIVED_POKETCH");
        Assert.Equal(RequestState.Done, Requests.State(Campaign, host.Story));
        Assert.All(Campaign.Steps, s => Assert.True(Requests.IsDone(Campaign, s, host.Story)));
        // A save with no pin reads as none
        Assert.Null(new SaveData().PinnedRequest);
    }

    [Fact]
    public void TheCampaignIsFinishedByPlayingItsScripts()
    {
        // A street of the test's own, with the president on it, since his script walks him off
        var street = new PokemonPlatinumEngine.Overworld.Map(12, 12) { Name = "Street" };
        street.NPCs.Add(PokemonPlatinumEngine.Data.MapFile.BuildNpc(new PokemonPlatinumEngine.Data.MapFile.NpcRecord
        {
            Id = "poketch_co_president", Name = "President", NpcType = "Gentleman", X = 5, Y = 8
        }, "Street"));
        var host = new HeadlessScriptHost { Map = street };
        host.Story.SetVar("VAR_POKETCH_CAMPAIGN_STATE", 2);
        Play(host, "jubilife_city.Clown1", 0);
        Assert.Equal("coupon2", Requests.Next(Campaign, host.Story)!.Name);
        Play(host, "jubilife_city.Clown2", 0);
        Play(host, "jubilife_city.Clown3", 0);
        Assert.Equal("president", Requests.Next(Campaign, host.Story)!.Name);
        Play(host, "jubilife_city.PoketchCoPresident");
        Assert.Equal(RequestState.Done, Requests.State(Campaign, host.Story));
    }

    [Fact]
    public void TheRequestCommandStoresTheStep()
    {
        var library = ScriptLibrary.FromSources(("test", "script T\n  request poketch 3\n  end\n"));
        var host = new HeadlessScriptHost();
        var runner = new ScriptRunner(library, host);
        runner.Start(library.All.First(), null, null);
        runner.RunToEnd();
        Assert.Equal(3, host.Story.Var(Requests.Variable("poketch")));
        Assert.Equal("VAR_REQUEST_POKETCH", Requests.Variable("poketch"));
        Assert.False(ScriptRunner.StopsAHurry(library.All.First().Everything().First()));
        Assert.ThrowsAny<ScriptException>(() => ScriptParser.Parse("test", "script T\n  request nothing 1\n  end\n"));
    }

    [Fact]
    public void TheRowIsOnByDefaultAndTheMenuIsTodaysWithItOff()
    {
        var settings = new GameSettings();
        Assert.Equal(RulesDefault.On, settings.Requests);
        settings.Requests = RulesDefault.Rules;
        Assert.Equal(RulesDefault.On, settings.Requests);
        OptionsScreen.Change(settings, OptionRow.Requests, 1);
        Assert.Equal(RulesDefault.Off, settings.Requests);

        var menu = new StartMenu();
        int today = menu.EntryCount;
        menu.HasRequests = true;
        Assert.Equal(today + 1, menu.EntryCount);
        // After the player's card: the menu's entries in order are Pokédex, Pokémon, Bag, the card, REQUESTS
        menu.HasRequests = true;
        menu.Open();
        menu.Move(4);
        Assert.Equal(StartMenuChoice.Requests, menu.Confirm());
    }

    [Fact]
    public void ThePagePinsAnOpenRequestAndDropsAPinThatIsDone()
    {
        var story = new StoryState();
        story.SetVar("VAR_POKETCH_CAMPAIGN_STATE", 2);
        var screen = new RequestsScreen();
        screen.Open(story, null);
        Assert.Equal(2, screen.TabCount);
        Assert.Single(screen.OpenList);
        Assert.Empty(screen.DoneList);
        screen.Confirm();
        Assert.Equal("poketch", screen.Pinned);
        screen.Confirm();
        Assert.Null(screen.Pinned);
        screen.Confirm();
        // DONE pins nothing
        screen.ChangeTab(1);
        Assert.Equal(RequestsTab.Done, screen.Tab);
        screen.Confirm();
        Assert.Equal("poketch", screen.Pinned);
        // Left from OPEN wraps round to DONE when there is no THIS AREA
        screen.ChangeTab(1);
        Assert.Equal(RequestsTab.Open, screen.Tab);

        // Done, the pin goes as the page opens again
        story.Set("FLAG_RECEIVED_POKETCH");
        screen.Open(story, "poketch");
        Assert.Null(screen.Pinned);
        Assert.Empty(screen.OpenList);
        Assert.Single(screen.DoneList);
    }

    [Fact]
    public void ThisAreaIsATabOnlyWithTheCounts()
    {
        var screen = new RequestsScreen();
        screen.Open(new StoryState(), null, new AreaCounts(2, 3, 5), "Route 202", RequestsTab.Area);
        Assert.Equal(3, screen.TabCount);
        Assert.Equal(RequestsTab.Area, screen.Tab);
        Assert.Equal(5, screen.Counts!.Value.Species);
        screen.ChangeTab(1);
        Assert.Equal(RequestsTab.Open, screen.Tab);
        screen.ChangeTab(-1);
        Assert.Equal(RequestsTab.Area, screen.Tab);
        // Without the counts the page opens on OPEN
        screen.Open(new StoryState(), null, null, "", RequestsTab.Area);
        Assert.Equal(RequestsTab.Open, screen.Tab);
    }
}
