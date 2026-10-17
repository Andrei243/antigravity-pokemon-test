using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Xunit;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 18 · W2: badges by region. Each region keeps a mask of its own badges, named by its file; a script's badge words
/// are the region's the player is in; the field moves ask the region's own gates; an older save's one mask lands on the
/// region it was made in; and the Trainer Card has a page for each region once there are two.
/// </summary>
public class RegionBadgeTests
{
    private static HeadlessScriptHost Play(string file, string source, string? region = null, Action<StoryState>? before = null)
    {
        var library = ScriptLibrary.FromSources((file, source), ("common", ""));
        var host = new HeadlessScriptHost();
        if (region != null) host.Region = region;
        before?.Invoke(host.Story);
        var runner = new ScriptRunner(library, host);
        runner.Start(library.All.First(s => s.File == file));
        runner.RunToEnd();
        return host;
    }

    // ------------------------------------------------------------------ the story's masks

    [Fact]
    public void EachRegionKeepsItsOwnBadgesByItsOwnNames()
    {
        var story = new StoryState();
        story.GiveBadge("Sinnoh", "Coal");
        story.GiveBadge("Sinnoh", "icicle");
        story.GiveBadge("Kanto", "Boulder");

        Assert.Equal(0b1000001, story.BadgeMask("Sinnoh"));
        Assert.Equal(0b1, story.BadgeMask("Kanto"));
        Assert.Equal(2, story.BadgeCount("Sinnoh"));
        Assert.Equal(1, story.BadgeCount("kanto"));
        Assert.Equal(3, story.TotalBadges);
        Assert.True(story.HasBadge("Sinnoh", "Icicle"));
        Assert.False(story.HasBadge("Kanto", "Cascade"));
        // A region with no file has no badges, and nothing is kept for it
        Assert.Equal(0, story.BadgeMask("Johto"));
        story.SetBadges("Johto", 0xFF);
        Assert.Equal(0, story.BadgeMask("Johto"));
        // A name the region doesn't have is a mistake, not a badge
        Assert.Throws<ArgumentException>(() => story.GiveBadge("Sinnoh", "Boulder"));
        Assert.Throws<ArgumentException>(() => story.HasBadge("Kanto", "Coal"));
        // Bits past the region's own eight are dropped
        story.SetBadges("Kanto", 0x1FF);
        Assert.Equal(0xFF, story.BadgeMask("Kanto"));
    }

    [Fact]
    public void ASnapshotKeepsEveryRegionsBadges()
    {
        var story = new StoryState();
        story.SetBadges("Sinnoh", 0b101);
        story.SetBadges("Kanto", 0b11);
        var copy = new StoryState();
        copy.Restore(story.Snapshot());
        Assert.Equal(0b101, copy.BadgeMask("Sinnoh"));
        Assert.Equal(0b11, copy.BadgeMask("Kanto"));
        Assert.Equal(0b11, story.Snapshot().BadgesOf("kanto"));
    }

    // ------------------------------------------------------------------ the save

    [Fact]
    public void AnOlderSavesBadgesLandOnTheRegionItWasMadeIn()
    {
        // Saved in Sinnoh before plan 18 · W2: its one mask is Sinnoh's
        var sinnoh = new SaveData { Badges = 0b101, CurrentMapName = "Sinnoh", StoryVersion = 18 };
        var story = new StoryState();
        story.Restore(sinnoh.ToStory());
        Assert.Equal(0b101, story.BadgeMask("Sinnoh"));
        Assert.Equal(0, story.BadgeMask("Kanto"));
        Assert.Equal((0b101, 8), sinnoh.BadgesHere());

        // Saved in the Kanto stand-in: Kanto's
        var kanto = new SaveData { Badges = 0b11, CurrentMapName = "PalletTown", PlayerGridX = 9, PlayerGridY = 9, StoryVersion = 18 };
        story.Restore(kanto.ToStory());
        Assert.Equal(0, story.BadgeMask("Sinnoh"));
        Assert.Equal(0b11, story.BadgeMask("Kanto"));

        // A save of today keeps the regions' masks and never the old one
        var today = new SaveData { RegionBadges = new() { ["Sinnoh"] = 0b1, ["Kanto"] = 0b10 }, StoryVersion = StoryState.CurrentVersion };
        string json = JsonSerializer.Serialize(today);
        Assert.DoesNotContain("\"Badges\"", json);
        var loaded = JsonSerializer.Deserialize<SaveData>(json)!;
        story.Restore(loaded.ToStory());
        Assert.Equal(0b1, story.BadgeMask("Sinnoh"));
        Assert.Equal(0b10, story.BadgeMask("Kanto"));
    }

    [Fact]
    public void AnOlderSaveWithTheCoalBadgeIsStillHandedRockSmash()
    {
        // The migration's older steps ask Sinnoh's badges: the mask is Sinnoh's before they run (version 5's HM06)
        var save = new SaveData { Badges = 0b1, CurrentMapName = "Sinnoh", StoryVersion = 4 };
        var story = new StoryState();
        story.Restore(save.ToStory());
        var bag = new PokemonPlatinumEngine.Models.Inventory();
        StoryMigration.Upgrade(story, save.StoryVersion, Array.Empty<PokemonPlatinumEngine.Models.Pokemon>(), ScriptLibrary.Default, bag);
        Assert.True(story.HasBadge("Sinnoh", "Coal"));
        Assert.Equal(1, bag.GetQuantity(ItemDatabase.Get("HM06")!));
    }

    // ------------------------------------------------------------------ scripts

    [Fact]
    public void GiveBadgeInASinnohScriptSetsSinnohsBitAndNoneOfKantos()
    {
        var host = Play("OreburghGym", "script A\n  givebadge coal\n  if badge coal setflag FLAG_HAS_COAL");
        Assert.Equal(0b1, host.Story.BadgeMask("Sinnoh"));
        Assert.Equal(0, host.Story.BadgeMask("Kanto"));
        Assert.True(host.Story.Has("FLAG_HAS_COAL"));
    }

    [Fact]
    public void BadgeCountInAKantoScriptCountsKantos()
    {
        var host = Play("PalletTown", """
            script A
              givebadge rainbow
              if badge boulder setflag FLAG_BOULDER
              if badges >= 3 setflag FLAG_THREE
              if badges >= 4 setflag FLAG_FOUR
              if var BADGE_COUNT == 3 setflag FLAG_COUNTED_THREE
            """, region: "Kanto", before: story =>
            {
                story.SetBadges("Sinnoh", 0xFF);
                story.SetBadges("Kanto", 0b11);
            });
        Assert.Equal(0b1011, host.Story.BadgeMask("Kanto"));
        Assert.Equal(0xFF, host.Story.BadgeMask("Sinnoh"));
        Assert.True(host.Story.Has("FLAG_BOULDER"));
        Assert.True(host.Story.Has("FLAG_THREE"));
        Assert.False(host.Story.Has("FLAG_FOUR"));
        Assert.True(host.Story.Has("FLAG_COUNTED_THREE"));
    }

    [Fact]
    public void AHostTakesTheRegionOfItsMap()
    {
        Assert.Equal("Sinnoh", new HeadlessScriptHost().Region);
        Assert.Equal("Kanto", new HeadlessScriptHost { Map = new Map(3, 3) { Name = "PalletTown" } }.Region);
        Assert.Equal("Sinnoh", new HeadlessScriptHost { Map = new Map(3, 3) { Name = "OreburghGym" } }.Region);
    }

    [Fact]
    public void ProblemsNameABadgeThePlacesRegionDoesntHave()
    {
        var problems = ScriptLibrary.FromSources(
            ("OreburghGym", "script A\n  givebadge rainbow\n  if badge boulder end"),
            ("twinleaf_town", "script B\n  givebadge coal"),
            ("PalletTown", "script C\n  givebadge rainbow\n  if badge coal end"),
            ("common", "script D\n  if badge rainbow end\n  if badge coal end")).Problems();
        Assert.Contains(problems, p => p.StartsWith("OreburghGym.txt(2)") && p.Contains("Sinnoh has no badge 'rainbow'"));
        Assert.Contains(problems, p => p.StartsWith("OreburghGym.txt(3)") && p.Contains("Sinnoh has no badge 'boulder'"));
        Assert.Contains(problems, p => p.StartsWith("PalletTown.txt(3)") && p.Contains("Kanto has no badge 'coal'"));
        Assert.DoesNotContain(problems, p => p.StartsWith("twinleaf_town.txt") || p.StartsWith("common.txt") || p.StartsWith("PalletTown.txt(2)"));
        // The files tell whose they are: a room, an area of a region's world, a file of no one region
        Assert.Equal("Sinnoh", ScriptLibrary.RegionOfFile("OreburghGym")?.Id);
        Assert.Equal("Sinnoh", ScriptLibrary.RegionOfFile("twinleaf_town")?.Id);
        Assert.Equal("Kanto", ScriptLibrary.RegionOfFile("PalletTown")?.Id);
        Assert.Null(ScriptLibrary.RegionOfFile("common"));
    }

    [Fact]
    public void TheGamesOwnScriptsNameOnlyTheirRegionsBadges() =>
        Assert.DoesNotContain(ScriptLibrary.Default.Problems(), p => p.Contains("badge"));

    // ------------------------------------------------------------------ the gates

    [Fact]
    public void TheFieldMovesAskTheRegionsOwnGates()
    {
        // Kanto's file names its own: Surf wants the Soul Badge there, and Sinnoh's Fen Badge opens nothing in Kanto
        Assert.Equal("Soul", FieldMoveRules.BadgeFor(FieldMove.Surf, "Kanto"));
        Assert.Null(FieldMoveRules.BadgeFor(FieldMove.Defog, "Kanto"));
        Assert.Null(FieldMoveRules.BadgeFor(FieldMove.Surf, "Johto"));
        var spot = new FieldSpot { Water = true, Region = "Kanto" };
        var story = new StoryState();
        story.SetBadges("Sinnoh", 0xFF);
        Assert.Equal(FieldMoveError.Badge, FieldMoveRules.Check(FieldMove.Surf, spot, story));
        story.GiveBadge("Kanto", "Soul");
        Assert.Equal(FieldMoveError.None, FieldMoveRules.Check(FieldMove.Surf, spot, story));
        // Every gate of every region with a file names one of its own badges
        foreach (var region in RegionDatabase.All.Where(r => r.File != null))
            foreach (var (move, badge) in region.Gates)
            {
                Assert.True(Enum.TryParse<FieldMove>(move, out _), $"{region.Id}'s gate '{move}' is no field move");
                Assert.True(StoryState.FindBadge(region.Id, badge) >= 0, $"{region.Id}'s gate for {move} names '{badge}', not one of its badges");
            }
    }

    [Fact]
    public void ASpotOnAMapIsInItsMapsRegion()
    {
        Assert.Equal("Kanto", FieldMoveRules.SpotOf(new Map(5, 5) { Name = "PalletTown" }, 2, 2, Direction.Up, new Walker()).Region);
        Assert.Equal("Sinnoh", FieldMoveRules.SpotOf(new Map(5, 5) { Name = "Clear" }, 2, 2, Direction.Up, new Walker()).Region);
    }

    // ------------------------------------------------------------------ the Trainer Card

    private static CardRegion Card(string id, int badges = 0, bool champion = false)
    {
        var region = RegionDatabase.Get(id)!;
        return new CardRegion(region.Id, region.Generation, region.BadgeNames, badges, champion);
    }

    private static TrainerCardInfo Info(params CardRegion[] regions) =>
        new("Lucas", 12345, 3000, 10, 5, 60f, regions[0], null) { Regions = regions };

    [Fact]
    public void WithOneRegionTheCardIsItsFrontAndItsBack()
    {
        var info = Info(Card("Sinnoh", 0b11));
        Assert.Empty(info.RegionPages);
        Assert.Equal(2, info.Here.Count);
        var screen = new TrainerCardScreen();
        screen.Open(regionPages: info.RegionPages.Count);
        Assert.False(screen.Turn(1));
        Assert.Equal(CardPage.Front, screen.Page);
        screen.Flip();
        Assert.True(screen.ShowingBack);
        screen.Flip();
        Assert.Equal(CardPage.Front, screen.Page);
    }

    [Fact]
    public void WithTwoRegionsTheCardHasAPageForEach()
    {
        var info = Info(Card("Sinnoh", 0b111, champion: true), Card("Kanto", 0b1));
        Assert.Equal(new[] { "Sinnoh", "Kanto" }, info.RegionPages.Select(r => r.Id));
        var screen = new TrainerCardScreen();
        screen.Open(regionPages: info.RegionPages.Count);
        Assert.False(screen.Turn(-1));
        Assert.True(screen.Turn(1));
        Assert.Equal(CardPage.Back, screen.Page);
        Assert.True(screen.Turn(1));
        Assert.Equal((CardPage.Region, 0), (screen.Page, screen.RegionShown));
        Assert.True(screen.Turn(1));
        Assert.Equal((CardPage.Region, 1), (screen.Page, screen.RegionShown));
        Assert.False(screen.Turn(1));
        // A on a region's page goes back to the front
        screen.Flip();
        Assert.Equal(CardPage.Front, screen.Page);
        Assert.Equal(-1, screen.RegionShown);
    }

    [Fact]
    public void TheSaveSummaryShowsTheBadgesOfTheRegionItWasMadeIn()
    {
        var save = new SaveData { RegionBadges = new() { ["Sinnoh"] = 0xFF, ["Kanto"] = 0b101 }, CurrentMapName = "PalletTown", PlayerGridX = 9, PlayerGridY = 9 };
        Assert.Equal((0b101, 8), save.BadgesHere());
        Assert.Equal(2, TitleScreen.CountBadges(save.BadgesHere().Mask));
    }
}
