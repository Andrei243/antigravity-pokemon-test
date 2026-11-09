using System.Linq;
using Xunit;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 18 · W8 (decision 9 (a), answered 2026-11-03): each region's departure is a harbour that sails on and back to
/// every region entered once its own Hall of Fame is set; a return lands on the region's harbour with the whole team.
/// </summary>
[Collection("Shared clocks and dice")]
public class HarbourTests
{
    private static Pokemon Mon(string species, int level = 60) => new(PokemonDatabase.Get(species)!, level);

    // The built regions whose story ends in a Hall of Fame with a departure: every one with a harbour of its own
    private static Region[] Harbours => RegionDatabase.All
        .Where(r => r.IsBuilt && r.HasStory && RegionDatabase.LinkFrom(r.Id)?.DepartureMap != null).ToArray();

    [Fact]
    public void AHarbourOffersNothingBeforeItsHallOfFameAndThenTheWayOnFirstAndTheRegionsEntered()
    {
        var sinnoh = RegionDatabase.Get(RegionDatabase.Sinnoh)!;
        var story = new StoryState();
        story.EnterRegion(RegionDatabase.Sinnoh);
        Assert.Empty(RegionDatabase.LinksFrom(sinnoh, story));

        story.CompleteRegion(RegionDatabase.Sinnoh);
        Assert.Equal(new[] { RegionDatabase.Kanto }, RegionDatabase.LinksFrom(sinnoh, story).Select(l => l.To));

        // Hoenn and Johto entered: the way on first, then the others in the chain's order, on the harbour's own vessel
        story.EnterRegion(RegionDatabase.Hoenn);
        story.EnterRegion(RegionDatabase.Johto);
        var ways = RegionDatabase.LinksFrom(sinnoh, story);
        Assert.Equal(new[] { RegionDatabase.Kanto, RegionDatabase.Johto, RegionDatabase.Hoenn }, ways.Select(l => l.To));
        Assert.All(ways, w => Assert.Equal("ship", w.VesselWord));
        Assert.All(ways, w => Assert.Equal(RegionDatabase.Sinnoh, w.From));
    }

    [Fact]
    public void ARegionIsEnteredByItsFlagOrByWhatAnOlderSaveKeeps()
    {
        var story = new StoryState();
        Assert.False(story.HasEntered(RegionDatabase.Kanto));
        story.GiveBadge(RegionDatabase.Kanto, RegionDatabase.Get(RegionDatabase.Kanto)!.BadgeNames[0]);
        Assert.True(story.HasEntered(RegionDatabase.Kanto));

        var finished = new StoryState();
        finished.CompleteRegion(RegionDatabase.Johto);
        Assert.True(finished.HasEntered(RegionDatabase.Johto));

        var flagged = new StoryState();
        flagged.EnterRegion(RegionDatabase.Hoenn);
        Assert.True(flagged.Has("HoennEntered"));
        Assert.True(flagged.HasEntered(RegionDatabase.Hoenn));
    }

    [Fact]
    public void WithEveryRegionEnteredAndFinishedEveryHarbourReachesEveryOtherBuiltRegion()
    {
        var story = new StoryState();
        foreach (var r in RegionDatabase.All.Where(r => r.IsBuilt))
        {
            story.EnterRegion(r.Id);
            story.CompleteRegion(r.Id);
        }
        // (no harbour sails to a side region, plan 23 · Z17: Hisui is entered by its own way alone)
        var built = RegionDatabase.All.Where(r => r.IsBuilt && !r.Side).Select(r => r.Id).ToList();
        Assert.NotEmpty(Harbours);
        foreach (var region in Harbours)
        {
            var ways = RegionDatabase.LinksFrom(region, story);
            Assert.Equal(RegionDatabase.LinkFrom(region.Id)!.To, ways[0].To);
            Assert.Equal(built.Where(id => id != region.Id).OrderBy(id => id), ways.Select(w => w.To).OrderBy(id => id));
        }
    }

    [Fact]
    public void EveryHarbourIsAWalkableTileBesideItsAttendant()
    {
        foreach (var region in Harbours)
        {
            var spot = region.Harbour;
            Assert.True(spot != null, $"{region.Id} has a departure and no harbour");
            Assert.Equal(RegionDatabase.LinkFrom(region.Id)!.DepartureMap, spot!.Map);
            var map = MapDatabase.Get(spot.Map);
            Assert.True(map.IsWalkable(spot.X, spot.Y), $"{region.Id}'s harbour {spot} isn't walkable");
            Assert.Null(map.GetWarpAt(spot.X, spot.Y));
            var (dx, dy) = spot.Facing switch { Direction.Up => (0, -1), Direction.Down => (0, 1), Direction.Left => (-1, 0), _ => (1, 0) };
            Assert.Contains(map.Everyone, n => n.IsTransportAttendant && n.GridX == spot.X + dx && n.GridY == spot.Y + dy);
        }
    }

    [Fact]
    public void AChampionOfBothRegionsSailsFromCanalaveToKantoAndBackWithTheWholeTeam()
    {
        var sinnoh = MapDatabase.Get("Sinnoh");
        var captain = sinnoh.Everyone.Single(n => n.IsTransportAttendant);
        var host = new HeadlessScriptHost { Map = sinnoh, PlayerTile = (44, 751), MapNamed = MapDatabase.Get };
        foreach (var species in new[] { "Infernape", "Staraptor", "Luxray", "Garchomp", "Venusaur", "Pidgeot" }) host.Party.Add(Mon(species));
        host.Story.CompleteRegion(RegionDatabase.Sinnoh);
        host.Story.CompleteRegion(RegionDatabase.Kanto);
        var team = host.Party.Members.ToList();

        // Kanto is the one way from Sinnoh, already entered: the question is the one-way yes or no, and a return
        // lands on Vermilion's quay rather than at Kanto's first arrival
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find(FieldScripts.Attendant)!, captain);
        runner.RunToEnd();
        var kanto = RegionDatabase.Get(RegionDatabase.Kanto)!;
        Assert.Equal(new[] { RegionDatabase.Kanto }, host.Crossings);
        Assert.Equal(kanto.Harbour!.Map, host.Map!.Name);
        Assert.Equal((kanto.Harbour.X, kanto.Harbour.Y), host.PlayerTile);
        Assert.Equal(team, host.Party.Members);

        // Vermilion's captain asks where: Johto on, Sinnoh back, STAY; Sinnoh lands on Canalave's pier
        var vermilion = host.Map.Everyone.Single(n => n.IsTransportAttendant);
        host.Answers.Enqueue(1);
        var back = new ScriptRunner(ScriptLibrary.Default, host);
        back.Start(ScriptLibrary.Default.Find(FieldScripts.Attendant)!, vermilion);
        back.RunToEnd();
        var asked = host.Asked.Last();
        Assert.Equal("Sinnoh", asked.Answer);
        Assert.Equal(new[] { RegionDatabase.Kanto, RegionDatabase.Sinnoh }, host.Crossings);
        Assert.Equal("Sinnoh", host.Map!.Name);
        Assert.Equal((44, 751), host.PlayerTile);
        Assert.Equal(team, host.Party.Members);
        Assert.Empty(host.Boxes);
        Assert.True(host.Story.HasEntered(RegionDatabase.Kanto));
    }

    [Fact]
    public void AHarboursStayKeepsThePlayerOnTheQuay()
    {
        var kanto = MapDatabase.Get("Kanto");
        var captain = kanto.Everyone.First(n => n.IsTransportAttendant);
        var host = new HeadlessScriptHost { Map = kanto, PlayerTile = (301, 229), MapNamed = MapDatabase.Get };
        host.Party.Add(Mon("Venusaur"));
        host.Story.CompleteRegion(RegionDatabase.Kanto);
        host.Story.EnterRegion(RegionDatabase.Sinnoh);
        host.Answers.Enqueue(2);
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find(FieldScripts.Attendant)!, captain);
        runner.RunToEnd();
        // Johto on, Sinnoh back, and STAY third
        Assert.Equal("STAY", host.Asked.Last().Answer);
        Assert.Empty(host.Crossings);
        Assert.Equal("Kanto", host.Map!.Name);
    }
}
