using System.Collections.Generic;
using System.Linq;
using Xunit;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumTests;

/// <summary>
/// What to do next, and rumours (plan 12 · Q14): Sinnoh's objectives are worked out from the flags, variables and
/// badges its chapters set, a script's <c>objective</c> stores one for a region that has none to derive, and the
/// townsfolk's rumours follow the Badges. None of it writes a field of the save.
/// </summary>
public class ObjectiveTests
{
    private static string Sinnoh => RegionDatabase.Sinnoh;

    private static HeadlessScriptHost Play(string name, params int[] answers)
    {
        var host = new HeadlessScriptHost();
        foreach (int a in answers) host.Answers.Enqueue(a);
        PlayOn(host, name);
        return host;
    }

    private static void PlayOn(HeadlessScriptHost host, string name)
    {
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find(name)!, null, null);
        runner.RunToEnd();
    }

    [Fact]
    public void EveryObjectiveHasAUniqueNameAndNumberInTheOrderItIsPlayed()
    {
        Assert.Equal(Objectives.All.Count, Objectives.All.Select(o => o.Name).Distinct().Count());
        Assert.Equal(Objectives.All.Count, Objectives.All.Select(o => o.Id).Distinct().Count());
        foreach (var region in Objectives.All.GroupBy(o => o.Region))
            Assert.Equal(region.OrderBy(o => o.Id).Select(o => o.Id), region.Select(o => o.Id));
        // A number of 0 is "none", and Sinnoh's stay under 100 so that later regions have a block each
        Assert.All(Objectives.All.Where(o => o.Region == Sinnoh), o => Assert.InRange(o.Id, 1, 99));
    }

    [Fact]
    public void EveryObjectiveHasALineInTheTableAndItFitsThePanel()
    {
        foreach (var o in Objectives.All)
        {
            string line = Text.Of(o.Key);
            Assert.False(line.StartsWith('['), $"{o.Key} has no line in the table of words");
            Assert.NotEmpty(line);
            // The start menu's panel holds four lines of its width (the Trainer Card's, a little wider, the same)
            Assert.InRange(PokemonPlatinumEngine.UI.Kit.TextMetrics.Wrap(PlayerIdentity.Fill(line, "Player", PlayerLook.Boy, PlayerIdentity.DefaultRivalName),
                440 - 56, s => PokemonPlatinumEngine.UI.Kit.TextMetrics.Measure(s, 24, PokemonPlatinumEngine.UI.Kit.UiWeight.ExtraBold)).Count, 1, 4);
        }
    }

    [Fact]
    public void ANewGameIsAtTheFirstObjective()
    {
        var story = new StoryState();
        Assert.Equal("downstairs", Objectives.Current(story, Sinnoh)?.Name);
        Assert.Equal(Objectives.All[0].Id, Objectives.Current(story, Sinnoh)!.Id);
    }

    [Fact]
    public void WhenEverythingThatCanBeSetIsSetNothingPressesAnyMore()
    {
        // Every flag a script writes, what the game sets by itself as the player comes to a place, every Badge and a
        // starter: an objective whose condition names a flag nobody sets (a misspelling) would stay behind
        var story = new StoryState();
        foreach (string flag in ScriptLibrary.Default.FlagsWritten.Concat(SpawnLocations.All.Select(s => s.ArrivalFlag))
                     .Concat(new[] { "FLAG_FIRST_ARRIVAL_POKEMON_LEAGUE", StoryMigration.GameCompletedFlag }))
            story.Set(flag);
        foreach (string badge in RegionDatabase.Get(Sinnoh)!.BadgeNames) story.GiveBadge(Sinnoh, badge);
        foreach (string variable in ScriptLibrary.Default.VariablesWritten) story.SetVar(variable, 5);
        story.ChooseStarter("Turtwig");
        Assert.Equal("free", Objectives.Current(story, Sinnoh)?.Name);
    }

    [Fact]
    public void AnObjectiveFollowsTheFlagsAndNeverGoesBack()
    {
        var story = new StoryState();
        story.Set("FLAG_TALKED_TO_MOM");
        Assert.Equal("rivalHome", Objectives.Current(story, Sinnoh)?.Name);
        story.Set("FLAG_RIVAL_LEFT_HOME");
        Assert.Equal("route201", Objectives.Current(story, Sinnoh)?.Name);
        story.ChooseStarter("Turtwig");
        Assert.Equal("shoes", Objectives.Current(story, Sinnoh)?.Name);
        // A Badge won ahead of its turn leaves the ones before it to do, as the player skipped them
        story.GiveBadge(Sinnoh, "Coal");
        Assert.Equal("shoes", Objectives.Current(story, Sinnoh)?.Name);
    }

    [Fact]
    public void ASaveOfAnOlderStoryVersionReadsRightWithNoMigration()
    {
        // The opening done as story version 2 left it, a Coal Badge won: the chapter after the Badge
        var host = new HeadlessScriptHost();
        PlayOn(host, "common.OpeningDone");
        host.Story.Set("FLAG_TALKED_TO_TRAINERS_SCHOOL_RIVAL");
        host.Story.Set("FLAG_RECEIVED_POKETCH");
        host.Story.ChooseStarter("Chimchar");
        host.Story.GiveBadge(Sinnoh, "Coal");
        Assert.Equal("worksKey", Objectives.Current(host.Story, Sinnoh)?.Name);
    }

    [Fact]
    public void ScriptsStoreAnObjectiveForARegionWithNoneToDerive()
    {
        var host = Play("common.NextHint");
        Assert.Equal(0, host.Story.Var(Objectives.Variable));

        var library = ScriptLibrary.FromSources(("test", "script T\n  objective candice\n  end\n"));
        var h = new HeadlessScriptHost();
        var runner = new ScriptRunner(library, h);
        runner.Start(library.All.First(), null, null);
        runner.RunToEnd();
        Assert.Equal(Objectives.Find("candice")!.Id, h.Story.Var(Objectives.Variable));
        // ...which wins over a derived objective behind it, in the region it belongs to and no other
        Assert.Equal("candice", Objectives.Current(h.Story, Sinnoh)?.Name);
        Assert.Null(Objectives.Current(h.Story, RegionDatabase.Alola));
    }

    [Fact]
    public void ANameThatIsNoObjectiveIsAParseError()
    {
        Assert.ThrowsAny<ScriptException>(() => ScriptParser.Parse("test", "script T\n  objective nowhere\n  end\n"));
    }

    [Fact]
    public void TheLineIsFilledInWithTheNamesAndSaidAsAPlaceholder()
    {
        var library = ScriptLibrary.FromSources(("test", "script T\n  say \"{objective}\"\n  end\n"));
        var host = new HeadlessScriptHost();
        var runner = new ScriptRunner(library, host);
        runner.Start(library.All.First(), null, null);
        runner.RunToEnd();
        string said = host.Transcript.Single().Text;
        Assert.Contains("Go downstairs", said);
        Assert.DoesNotContain("{", said);
        Assert.Contains("Go downstairs", Objectives.Line(host.Story, Sinnoh));
        // A region with nothing to say says nothing
        Assert.Equal("", Objectives.Line(host.Story, RegionDatabase.Alola));
    }

    [Fact]
    public void TheNurseAndMomSayItWhenAskedAndOnlyThen()
    {
        var yes = Play("NextHint", 0);
        Assert.Contains(yes.Transcript, t => t.Text.Contains("Go downstairs"));
        var no = Play("NextHint", 1);
        Assert.DoesNotContain(no.Transcript, t => t.Text.Contains("Go downstairs"));
        // With the option off (the host says so) nothing is even asked
        var off = new HeadlessScriptHost { ShowObjectives = false };
        PlayOn(off, "NextHint");
        Assert.Empty(off.Transcript);
    }

    [Fact]
    public void RumoursFollowTheBadges()
    {
        string First(System.Action<StoryState> setup)
        {
            var host = new HeadlessScriptHost();
            setup(host.Story);
            PlayOn(host, "Rumour");
            return host.Transcript.Single().Text;
        }

        Assert.Contains("mining town", First(_ => { }));
        Assert.Contains("Floaroma", First(s => s.GiveBadge(Sinnoh, "Coal")));
        Assert.Contains("Eterna City", First(s => { s.GiveBadge(Sinnoh, "Coal"); s.GiveBadge(Sinnoh, "Forest"); }));
        Assert.Contains("new Champion", First(s => s.Set(StoryMigration.GameCompletedFlag)));
    }

    [Fact]
    public void EveryRumourIsFromTheCommonScriptAndSaysNothingThatIsNotInTheFiles()
    {
        // Every label of the script is reached by a condition on Badges or the Hall of Fame: nine stages and the
        // Champion's, each told two ways (which of the two is the die's, `random 2` and its result)
        var rumour = ScriptLibrary.Default.Find("Rumour")!;
        Assert.Equal(20, rumour.Everything().Count(i => i.Op == Op.Say));
        foreach (var i in rumour.Everything().Where(i => i.Op == Op.If))
            Assert.True(i.Condition!.Query is Query.Badges or Query.Flag or Query.Result, "a rumour asks only the Badges, the Hall of Fame and its die");
    }

    // The later regions (plan 12 · Q14, their part): each in a block of a hundred numbers, its first and last objective

    public static TheoryData<string, int, string, string> LaterRegions => new()
    {
        { RegionDatabase.Kanto, 100, "kantoSetOut", "kantoFree" },
        { RegionDatabase.Johto, 200, "johtoMom", "johtoFree" },
        { RegionDatabase.Hoenn, 300, "hoennNeighbours", "hoennFree" },
        { RegionDatabase.Unova, 400, "unovaHugh", "unovaFree" },
        { RegionDatabase.Kalos, 500, "kalosDownstairs", "kalosFree" },
    };

    [Theory]
    [MemberData(nameof(LaterRegions))]
    public void ALaterRegionsObjectivesHaveABlockOfTheirOwn(string region, int block, string first, string last)
    {
        var own = Objectives.All.Where(o => o.Region == region).ToList();
        Assert.NotEmpty(own);
        Assert.All(own, o => Assert.InRange(o.Id, block, block + 99));
        Assert.Equal(first, own[0].Name);
        Assert.Equal(last, own[^1].Name);
        // Only the last has nothing to say it is done
        Assert.Null(own[^1].Done);
        Assert.All(own.Take(own.Count - 1), o => Assert.NotNull(o.Done));
    }

    [Theory]
    [MemberData(nameof(LaterRegions))]
    public void ANewGameOfALaterRegionIsAtItsFirstObjective(string region, int block, string first, string last)
    {
        _ = block; _ = last;
        Assert.Equal(first, Objectives.Current(new StoryState(), region)?.Name);
    }

    [Theory]
    [MemberData(nameof(LaterRegions))]
    public void WhenEverythingALaterRegionsScriptsWriteIsSetNothingPressesThere(string region, int block, string first, string last)
    {
        _ = block; _ = first;
        // Every flag a script writes, every variable as far on as it goes, every Badge of every region
        var story = new StoryState();
        foreach (string flag in ScriptLibrary.Default.FlagsWritten) story.Set(flag);
        foreach (string variable in ScriptLibrary.Default.VariablesWritten.Where(v => v != Objectives.Variable)) story.SetVar(variable, 100);
        foreach (var r in new[] { RegionDatabase.Kanto, RegionDatabase.Johto, RegionDatabase.Hoenn, RegionDatabase.Unova, RegionDatabase.Kalos })
            foreach (string badge in RegionDatabase.Get(r)!.BadgeNames) story.GiveBadge(r, badge);
        Assert.Equal(last, Objectives.Current(story, region)?.Name);
    }

    [Theory]
    [MemberData(nameof(LaterRegions))]
    public void EveryDerivedConditionOfALaterRegionIsSomethingAScriptWrites(string region, int block, string first, string last)
    {
        _ = block; _ = first; _ = last;
        // Each objective but the last, alone of its region's, is done by the flags, variables and Badges the scripts set:
        // an objective whose flag nobody sets (a misspelling) would never be done
        var flags = ScriptLibrary.Default.FlagsWritten.ToList();
        var variables = ScriptLibrary.Default.VariablesWritten.ToList();
        foreach (var o in Objectives.All.Where(o => o.Region == region && o.Done != null))
        {
            bool reached = false;
            foreach (string flag in flags)
            {
                var s = new StoryState();
                s.Set(flag);
                if (o.Done!(s)) { reached = true; break; }
            }
            if (!reached)
                foreach (string variable in variables)
                {
                    var s = new StoryState();
                    s.SetVar(variable, 100);
                    if (o.Done!(s)) { reached = true; break; }
                }
            if (!reached)
            {
                var s = new StoryState();
                foreach (var r in new[] { RegionDatabase.Kanto, RegionDatabase.Johto, RegionDatabase.Hoenn, RegionDatabase.Unova, RegionDatabase.Kalos })
                    foreach (string badge in RegionDatabase.Get(r)!.BadgeNames) s.GiveBadge(r, badge);
                reached = o.Done!(s);
            }
            Assert.True(reached, $"nothing a script writes finishes {o.Name}");
        }
    }

    [Fact]
    public void AnObjectiveOfTheOpeningIsDoneOnceAnythingAfterItIs()
    {
        // A Champion who crossed into Hoenn and has won the Stone Badge is past the neighbours and the professor
        var story = new StoryState();
        story.GiveBadge(RegionDatabase.Hoenn, "Stone");
        Assert.Equal("hoennDevon", Objectives.Current(story, RegionDatabase.Hoenn)?.Name);
        // ...but a Badge won out of turn leaves the Gym before it to do, as in Sinnoh
        story = new StoryState();
        story.SetVar("VAR_BIRCH_LAB_STATE", 5);
        story.GiveBadge(RegionDatabase.Hoenn, "Knuckle");
        Assert.Equal("hoennRoxanne", Objectives.Current(story, RegionDatabase.Hoenn)?.Name);
    }

    [Fact]
    public void KalosFollowsItsChaptersCount()
    {
        var story = new StoryState();
        string[] expected = { "kalosDownstairs", "kalosDoor", "kalosAquacorde", "kalosHome", "kalosRoute2", "kalosForest", "kalosRoute3", "kalosSantalune", "kalosViola", "kalosRoute4", "kalosLab", "kalosLysandre",
            "kalosRoute5", "kalosCamphrier", "kalosPalace", "kalosSnorlax", "kalosCyllage", "kalosGrant", "kalosRoute10", "kalosShalour", "kalosKorrina", "kalosMastery", "kalosFree" };
        for (int n = 0; n <= 22; n++)
        {
            story.SetVar("VAR_KALOS_STORY", n);
            Assert.Equal(expected[n], Objectives.Current(story, RegionDatabase.Kalos)?.Name);
        }
    }

    [Fact]
    public void AScriptStoresAnObjectiveOnlyInTheRegionThePlayerIsIn()
    {
        var library = ScriptLibrary.FromSources(("test", "script T\n  objective kantoMisty\n  end\n"));
        void Run(HeadlessScriptHost h)
        {
            var runner = new ScriptRunner(library, h);
            runner.Start(library.All.First(), null, null);
            runner.RunToEnd();
        }
        var inKanto = new HeadlessScriptHost { Region = RegionDatabase.Kanto };
        Run(inKanto);
        Assert.Equal(Objectives.Find("kantoMisty")!.Id, inKanto.Story.Var(Objectives.Variable));
        Assert.Equal("kantoMisty", Objectives.Current(inKanto.Story, RegionDatabase.Kanto)?.Name);
        // HeartGold's Kanto Gyms are played from Johto: Johto's stored objective stays
        var inJohto = new HeadlessScriptHost { Region = RegionDatabase.Johto };
        inJohto.Story.SetVar(Objectives.Variable, Objectives.Find("johtoClair")!.Id);
        Run(inJohto);
        Assert.Equal(Objectives.Find("johtoClair")!.Id, inJohto.Story.Var(Objectives.Variable));
    }

    [Fact]
    public void TheChaptersScenesEndByStoringWhatComesNext()
    {
        // Every `objective` line of the game's scripts names an objective (the parser holds that too), and the later
        // regions' chapters end their scenes with one
        int stored = 0;
        foreach (var script in ScriptLibrary.Default.All)
            foreach (var i in script.Everything().Where(i => i.Op == Op.Objective))
            {
                stored++;
                Assert.NotNull(Objectives.Find(i.Name!));
            }
        Assert.True(stored >= 70, $"only {stored} scenes store what to do next");
    }

    [Fact]
    public void TheRowIsOnByDefaultAndFollowsTheRulesWhenAsked()
    {
        var settings = new GameSettings();
        Assert.Equal(RulesDefault.On, settings.NextObjective);
        settings.NextObjective = RulesDefault.Rules;
        Assert.Equal(RulesDefault.On, settings.NextObjective);
        settings.NextObjective = RulesDefault.Off;
        Assert.False(settings.NextObjective.Holds(RulesPreset.Modern));
    }
}
