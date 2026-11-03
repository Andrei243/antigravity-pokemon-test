using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.Contest;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.Graphics;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 21 · H11, part f and H8, part e: the Safari Fan Club and the Contest Lady on the television, an appeal's burst on
/// the contest's stage, the Pokéblock feeders and the museum's paintings drawn where they stand, and Archie and Maxie
/// bringing the orbs back to Mt. Pyre's summit.
/// </summary>
[Collection("Shared clocks and dice")]
public class HoennPartFTests
{
    private static readonly Lazy<List<Map>> Maps = new(() => World.LoadAll().Single(w => w.Index.Region == RegionDatabase.Hoenn).BuildMaps().ToList());

    private static Map Room(string name) => Maps.Value.Single(m => m.Name == name);

    private static HeadlessScriptHost Run(HeadlessScriptHost host, string script, string file, NPC? self = null, params int[] answers)
    {
        foreach (int a in answers) host.Answers.Enqueue(a);
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        self ??= host.Map?.Everyone.FirstOrDefault(n => n.Script == script);
        runner.Start(ScriptLibrary.Default.Find(script, file)!, self);
        runner.RunToEnd();
        Assert.True(host.Problems.Count == 0, string.Join("; ", host.Problems));
        return host;
    }

    // ---- the television

    [Fact]
    public void TheSafariFanClubIsOnTheAirInHoennOnceAGameHasEnded()
    {
        var off = new BroadcastFacts { Region = RegionDatabase.Hoenn };
        Assert.DoesNotContain(Programme.SafariFanClub, Broadcasts.On(off));
        var on = off with { SafariOnAir = true, SafariCaught = 2, SafariBlocks = 1, PlayerName = "May" };
        Assert.Contains(Programme.SafariFanClub, Broadcasts.On(on));
        // Not in another region's television
        Assert.DoesNotContain(Programme.SafariFanClub, Broadcasts.On(on with { Region = "Sinnoh" }));

        var lines = Broadcasts.Lines(Programme.SafariFanClub, on);
        Assert.Equal(5, lines.Count);
        Assert.Contains("2 Pokémon", lines[2]);
        Assert.Contains("just one Pokéblock", lines[3]);
        Assert.Contains("without a catch", Broadcasts.Lines(Programme.SafariFanClub, on with { SafariCaught = 0 })[2]);
        Assert.Contains("not a single Pokéblock", Broadcasts.Lines(Programme.SafariFanClub, on with { SafariBlocks = 0 })[3]);
        Assert.Contains("7 Pokéblocks", Broadcasts.Lines(Programme.SafariFanClub, on with { SafariBlocks = 7 })[3]);
    }

    [Fact]
    public void ASafariGameCountsItsCatchesAndPokeblocksFromItsStartToItsEnd()
    {
        var story = new StoryState();
        story.SetVar(Broadcasts.SafariCaughtVariable, 9);
        story.Set(Broadcasts.SafariOnAirFlag);
        Broadcasts.BeginSafari(story);
        Assert.Equal(0, story.Var(Broadcasts.SafariCaughtVariable));
        Assert.False(story.Has(Broadcasts.SafariOnAirFlag));
        Broadcasts.CountSafariBattle(story, caught: true, pokeblocksThrown: 2);
        Broadcasts.CountSafariBattle(story, caught: false, pokeblocksThrown: 1);
        Broadcasts.CountSafariBattle(story, caught: true, pokeblocksThrown: 0);
        Assert.Equal((2, 3), (story.Var(Broadcasts.SafariCaughtVariable), story.Var(Broadcasts.SafariBlocksVariable)));
        Broadcasts.EndSafari(story);
        Assert.True(story.Has(Broadcasts.SafariOnAirFlag));
        Assert.True(story.Has(Broadcasts.SafariNewFlag));
    }

    [Fact]
    public void TheSafariScriptWordsPutTheShowOnTheAirInHoennAlone()
    {
        var script = ScriptParser.Parse("test", "script Go\n  safari start 500\n  safari end\n  end\n");
        foreach (var (region, aired) in new[] { (RegionDatabase.Hoenn, true), ("Sinnoh", false) })
        {
            var host = new HeadlessScriptHost { Region = region };
            var runner = new ScriptRunner(ScriptLibrary.Default, host);
            runner.Start(script.Single(s => s.Name == "Go"), null);
            runner.RunToEnd();
            Assert.Equal(aired, host.Story.Has(Broadcasts.SafariNewFlag));
        }
    }

    [Fact]
    public void ATelevisionShowsTheSafariFanClubFirstAndOnceThenTheUsualProgrammes()
    {
        var host = new HeadlessScriptHost { Region = RegionDatabase.Hoenn };
        Broadcasts.EndSafari(host.Story);
        host.Story.SetVar(Broadcasts.SafariCaughtVariable, 3);
        Run(host, "Television", "common");
        Assert.Contains(host.Transcript, l => l.Text.Contains("Safari Fan Club"));
        Assert.False(host.Story.Has(Broadcasts.SafariNewFlag));
        // It stays among the programmes a die draws
        Assert.True(host.Story.Has(Broadcasts.SafariOnAirFlag));
    }

    [Fact]
    public void TheContestLadysPokemonReadyGoesOnTheAir()
    {
        var facts = new BroadcastFacts { Region = RegionDatabase.Hoenn, PlayerName = "May", ContestLadyShow = 4, ContestLadyGood = true };
        Assert.Contains(Programme.ContestLady, Broadcasts.On(facts));
        Assert.DoesNotContain(Programme.ContestLady, Broadcasts.On(facts with { ContestLadyShow = 0 }));
        var lines = Broadcasts.Lines(Programme.ContestLady, facts);
        Assert.Equal(5, lines.Count);
        Assert.Contains(LocalNames.Species(ContestLady.Species[3]), lines[1]);
        Assert.Contains("never looked better", lines[2]);
        Assert.Contains("a little off", Broadcasts.Lines(Programme.ContestLady, facts with { ContestLadyGood = false })[2]);
        Assert.Contains("May", lines[3]);

        // Her script sets it when her Pokémon is ready
        var host = new HeadlessScriptHost { Map = Room("LilycoveCity_PokemonCenter_1F"), Region = RegionDatabase.Hoenn };
        host.Rolls.Enqueue(3);
        host.Bag.AddItem(ItemDatabase.Get("Pokéblock Case")!, 1);
        host.Story.SetVar(ContestLady.CategoryVariable, 4);
        host.Story.SetVar(ContestLady.GoodVariable, 4);
        host.PokeblockCase.Add(new Pokeblock(PokeblockColor.Green, 0, 0, 0, 12, 0, 10));
        Run(host, "ContestLady", "lilycove_city_pokemon_center_1f", answers: new[] { 0, 0 });
        Assert.Equal(4, host.Story.Var(Broadcasts.ContestLadyShowVariable));
        Assert.Equal(1, host.Story.Var(Broadcasts.ContestLadyGoodVariable));
        Assert.True(host.Story.Has(Broadcasts.ContestLadyNewFlag));
    }

    // ---- the contest's stage

    [Fact]
    public void AnAppealsBurstIsAPureFunctionOfItsQualityAgeAndMove()
    {
        int seed = AppealBurst.SeedOf("Thunderbolt");
        Assert.Equal(seed, AppealBurst.SeedOf("Thunderbolt"));
        Assert.NotEqual(seed, AppealBurst.SeedOf("Surf"));
        Assert.Empty(AppealBurst.Sparks(ContestStat.Cool, -0.1f, seed));
        Assert.Empty(AppealBurst.Sparks(ContestStat.Cool, AppealBurst.Seconds, seed));
        var shapes = new Dictionary<ContestStat, SparkShape>
        {
            [ContestStat.Cool] = SparkShape.Star, [ContestStat.Beauty] = SparkShape.Glint, [ContestStat.Cute] = SparkShape.Heart,
            [ContestStat.Smart] = SparkShape.Ring, [ContestStat.Tough] = SparkShape.Shard,
        };
        foreach (var (quality, shape) in shapes)
            for (float age = 0f; age < AppealBurst.Seconds; age += 0.05f)
            {
                var a = AppealBurst.Sparks(quality, age, seed);
                Assert.Equal(a, AppealBurst.Sparks(quality, age, seed));
                Assert.InRange(a.Count, 8, AppealBurst.MostSparks);
                Assert.All(a, s => Assert.Equal(shape, s.Shape));
                Assert.All(a, s => Assert.InRange(s.Alpha, 0f, 1f));
                Assert.All(a, s => Assert.True(float.IsFinite(s.Offset.X) && float.IsFinite(s.Offset.Y)));
            }
    }

    // ---- what stands where

    [Fact]
    public void EveryFeederOfTheSafariZoneIsDrawnWhereItIsRead()
    {
        int feeders = 0;
        foreach (var overlay in System.IO.Directory.GetFiles(System.IO.Path.Combine(GameDataFiles.Directory, "world", "hoenn", "overlays"), "hoenn_safari_zone_*.json"))
        {
            string text = System.IO.File.ReadAllText(overlay);
            int read = text.Split("\"HoennFeeder\"").Length - 1;
            int drawn = text.Split("\"PokeblockFeeder\"").Length - 1;
            Assert.True(read == drawn, $"{System.IO.Path.GetFileName(overlay)}: {read} read, {drawn} drawn");
            feeders += drawn;
        }
        Assert.Equal(11, feeders);
        var zone = Maps.Value.Single(m => m.Name == "HoennSafariZone");
        Assert.Equal(11, zone.Props.Count(p => p.Type == PropType.PokeblockFeeder));
    }

    [Fact]
    public void APaintingsCardIsDrawnFromItsQualityAndColour()
    {
        Assert.Equal("contest:2:pink", PokemonPlatinumEngine.Graphics.HoennArt.ContestPaintingModel(2, "Pink"));
        Assert.Equal("contest:0:gray", PokemonPlatinumEngine.Graphics.HoennArt.ContestPaintingModel(0, null));
        var a = new PixelCanvas(56, 30);
        var b = new PixelCanvas(56, 30);
        PokemonPlatinumEngine.Graphics.HoennArt.PaintContestPainting(a, 1, "blue");
        PokemonPlatinumEngine.Graphics.HoennArt.PaintContestPainting(b, 3, "blue");
        Assert.Contains(Enumerable.Range(0, 56 * 30), i => !a.Get(i % 56, i / 56).Equals(b.Get(i % 56, i / 56)));
        var feeder = new PixelCanvas(24, 34);
        PokemonPlatinumEngine.Graphics.HoennArt.PaintFeeder(feeder);
        Assert.Contains(Enumerable.Range(0, 24 * 34), i => feeder.IsOpaque(i % 24, i / 24));
    }

    // ---- the orbs given back

    private static HeadlessScriptHost Summit()
    {
        var map = Room("MtPyre_Summit");
        return new HeadlessScriptHost { Map = map, Region = RegionDatabase.Hoenn };
    }

    [Fact]
    public void TheLeadersLeavingSootopolisSendsThemUpMtPyre()
    {
        var host = new HeadlessScriptHost { Map = Room("SootopolisCity"), Region = RegionDatabase.Hoenn };
        host.Story.SetVar("VAR_MT_PYRE_STATE", 1);
        host.Story.Set("FLAG_HIDE_MT_PYRE_SUMMIT_ARCHIE");
        host.Story.Set("FLAG_HIDE_MT_PYRE_SUMMIT_MAXIE");
        Run(host, "LeadersLeave", "sootopolis_city");
        Assert.Equal(2, host.Story.Var("VAR_MT_PYRE_STATE"));
        Assert.False(host.Story.Has("FLAG_HIDE_MT_PYRE_SUMMIT_ARCHIE"));
        Assert.False(host.Story.Has("FLAG_HIDE_MT_PYRE_SUMMIT_MAXIE"));
    }

    [Fact]
    public void AtTheSummitTheyGiveTheOrbsBackAndGoForGood()
    {
        var host = Summit();
        host.Story.SetVar("VAR_MT_PYRE_STATE", 2);
        host.Map!.ApplyPresence(host.Story.Has);
        Assert.Contains(host.Map.NPCs, n => n.Key == "mt_pyre_summit_archie");
        Assert.Contains(host.Map.NPCs, n => n.Key == "mt_pyre_summit_maxie");
        // Maxie stands beside Archie, not on his tile
        var archie = host.Map.NPCs.Single(n => n.Key == "mt_pyre_summit_archie");
        var maxie = host.Map.NPCs.Single(n => n.Key == "mt_pyre_summit_maxie");
        Assert.NotEqual((archie.GridX, archie.GridY), (maxie.GridX, maxie.GridY));

        Run(host, "LeadersTrigger", "mt_pyre_summit");
        Assert.Equal(3, host.Story.Var("VAR_MT_PYRE_STATE"));
        Assert.True(host.Story.Has("FLAG_HIDE_MT_PYRE_SUMMIT_ARCHIE"));
        Assert.True(host.Story.Has("FLAG_HIDE_MT_PYRE_SUMMIT_MAXIE"));
        Assert.Contains(host.Transcript, l => l.Text.Contains("Red Orb"));

        // The old woman's word afterwards, and the trigger does nothing again
        int said = host.Transcript.Count;
        Run(host, "OldWoman", "mt_pyre_summit");
        Assert.Contains("back where they belong", host.Transcript[^1].Text);
        Run(host, "LeadersTrigger", "mt_pyre_summit");
        Assert.Equal(said + 1, host.Transcript.Count);
    }

    [Fact]
    public void ASaveWhoseLeadersHadLeftFindsThemAtTheSummit()
    {
        var host = Summit();
        host.Story.SetVar("VAR_MT_PYRE_STATE", 1);
        host.Story.Set("FLAG_SOOTOPOLIS_ARCHIE_MAXIE_LEAVE");
        host.Story.Set("FLAG_HIDE_MT_PYRE_SUMMIT_ARCHIE");
        host.Story.Set("FLAG_HIDE_MT_PYRE_SUMMIT_MAXIE");
        Run(host, "OnEnter", "mt_pyre_summit");
        Assert.Equal(2, host.Story.Var("VAR_MT_PYRE_STATE"));
        Assert.False(host.Story.Has("FLAG_HIDE_MT_PYRE_SUMMIT_ARCHIE"));

        // Before the calm nothing changes
        var early = Summit();
        early.Story.SetVar("VAR_MT_PYRE_STATE", 1);
        Run(early, "OnEnter", "mt_pyre_summit");
        Assert.Equal(1, early.Story.Var("VAR_MT_PYRE_STATE"));
    }
}
