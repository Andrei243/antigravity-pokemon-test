using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 18 · W9: Fly between regions, the one way back to a region left (the README's "Regions": one way on by
/// vessel, and Fly back). Fly's choice has a tab for every region entered, a town of another region is flown to as a
/// crossing, and each region keeps its own place to come back to.
/// </summary>
[Collection("Shared clocks and dice")]
public class FlyBetweenRegionsTests
{
    private static Region Sinnoh => RegionDatabase.Get(RegionDatabase.Sinnoh)!;
    private static Region Kanto => RegionDatabase.Get(RegionDatabase.Kanto)!;

    private static Pokemon Mon(string species, int level = 60) => new(PokemonDatabase.Get(species)!, level);

    /// <summary>A Champion of Sinnoh who has crossed into Kanto: Sinnoh's Hall of Fame, and Kanto's Pokédex entered.</summary>
    private static (StoryState Story, List<string> Dexes) ChampionInKanto()
    {
        var story = new StoryState();
        story.Set(Sinnoh.StoryCompleteFlag);
        return (story, new List<string> { Sinnoh.Dex!, Kanto.Dex! });
    }

    [Fact]
    public void ARegionNeverEnteredCantBeFlownTo()
    {
        var story = new StoryState();
        var dexes = new List<string> { Sinnoh.Dex! };
        Assert.False(Crossing.HasEntered(Kanto, story, dexes));
        Assert.False(Crossing.CanFly(Sinnoh, Kanto, story, dexes));
        Assert.Equal(new[] { RegionDatabase.Sinnoh }, Crossing.FlyRegions(Sinnoh, story, dexes).Select(r => r.Id));
        // Nor does Fly ever "cross" into the region it is in
        Assert.False(Crossing.CanFly(Sinnoh, Sinnoh, story, dexes));
    }

    [Fact]
    public void EveryRegionEnteredCanBeFlownToInTheChainsOrder()
    {
        var (story, dexes) = ChampionInKanto();
        Assert.True(Crossing.CanFly(Sinnoh, Kanto, story, dexes));
        Assert.True(Crossing.CanFly(Kanto, Sinnoh, story, dexes));
        Assert.Equal(new[] { RegionDatabase.Sinnoh, RegionDatabase.Kanto }, Crossing.FlyRegions(Kanto, story, dexes).Select(r => r.Id));
        Assert.Equal(new[] { RegionDatabase.Sinnoh, RegionDatabase.Kanto }, Crossing.FlyRegions(Sinnoh, story, dexes).Select(r => r.Id));
    }

    [Fact]
    public void ABadgeOrAHallOfFameEntersARegionAsTheTrainerCardCountsIt()
    {
        var story = new StoryState();
        Assert.False(Crossing.HasEntered(Kanto, story, Array.Empty<string>()));
        story.SetBadges(RegionDatabase.Kanto, 1);
        Assert.True(Crossing.HasEntered(Kanto, story, Array.Empty<string>()));
        story.SetBadges(RegionDatabase.Kanto, 0);
        story.Set(Kanto.StoryCompleteFlag);
        Assert.True(Crossing.HasEntered(Kanto, story, Array.Empty<string>()));
    }

    [Fact]
    public void ARegionNotBuiltIsNeverAFlightsTab()
    {
        var story = new StoryState();
        foreach (var region in RegionDatabase.All.Where(r => !r.IsBuilt))
        {
            story.Set(region.StoryCompleteFlag);
            Assert.False(Crossing.CanFly(Sinnoh, region, story, Array.Empty<string>()));
        }
    }

    [Fact]
    public void FlysChoiceChangesRegionByItsTabs()
    {
        var (story, _) = ChampionInKanto();
        var pallet = SpawnLocations.In(RegionDatabase.Kanto)[0];
        story.Set(SpawnLocations.FlyFlag(pallet));
        story.Set(SpawnLocations.FlyFlag(SpawnLocations.ArrivedIn("jubilife_city")!));
        var sinnohMap = RegionMap.Of(RegionDatabase.Sinnoh)!;
        var kantoMap = RegionMap.Of(RegionDatabase.Kanto)!;
        var tabs = new[] { new TownMapState(sinnohMap, story, RegionMap.CellOf(305, 540)), new TownMapState(kantoMap, story) };

        var screen = new TownMapScreen();
        screen.Open(tabs, 0, TownMapMode.Fly, flier: 1);
        Assert.True(screen.HasTabs);
        Assert.Equal(0, screen.Tab);
        Assert.Equal(0, screen.HomeTab);
        Assert.Equal(1, screen.Flier);
        Assert.Same(tabs[0], screen.State);
        // Nothing before the first tab
        Assert.False(screen.ChangeTab(-1));

        // Kanto's tab: its map whole, the cursor at its own first place, no player on it
        screen.ToggleZoom();
        Assert.True(screen.ChangeTab(1));
        Assert.Same(tabs[1], screen.State);
        Assert.False(screen.Zoomed);
        Assert.Equal(tabs[1].FirstCursor(), (screen.CursorX, screen.CursorY));
        Assert.Null(screen.State!.Here);
        Assert.False(screen.ChangeTab(1));
        Assert.Equal(0, screen.HomeTab);

        // Pallet Town is chosen, and the place is Kanto's
        var cell = kantoMap.FlySpots.First(f => f.Spawn == pallet.Id).Cells[0];
        for (int i = 0; i < 60 && (screen.CursorX, screen.CursorY) != cell; i++)
            screen.Move(Math.Sign(cell.X - screen.CursorX), Math.Sign(cell.Y - screen.CursorY));
        screen.Confirm();
        var chosen = screen.TakeChoice();
        Assert.NotNull(chosen);
        Assert.Equal(pallet.Id, chosen!.Id);
        Assert.Equal(Kanto.OverworldMap, chosen.Map);
    }

    [Fact]
    public void AScreenOpenedOnOneRegionHasNoTabs()
    {
        var story = new StoryState();
        var screen = new TownMapScreen();
        screen.Open(new TownMapState(RegionMap.Of(RegionDatabase.Sinnoh)!, story), TownMapMode.Fly);
        Assert.False(screen.HasTabs);
        Assert.False(screen.ChangeTab(1));
        Assert.False(screen.ChangeTab(-1));
    }

    [Fact]
    public void AFlightToAnotherRegionIsACrossingThereWithTheWholeTeam()
    {
        var (story, _) = ChampionInKanto();
        var party = new Party();
        foreach (var species in new[] { "Staraptor", "Infernape", "Garchomp" }) party.Add(Mon(species));
        var before = party.Members.ToList();
        var pallet = SpawnLocations.In(RegionDatabase.Kanto)[0];
        var host = new HeadlessScriptHost(story, party, new Inventory()) { FlyTo = pallet, Region = RegionDatabase.Sinnoh };

        Assert.True(host.Fly());
        Assert.Contains(host.Log, l => l == $"fly {RegionDatabase.Kanto}");
        Assert.Equal(new[] { RegionDatabase.Kanto }, host.Crossings);
        Assert.Equal((pallet.X, pallet.Y), host.PlayerTile);
        Assert.Equal(RegionDatabase.Kanto, host.Region);
        // Crossing.Depart under option B: the team whole, each given the region it left
        Assert.Equal(before, party.Members);
        Assert.All(party.Members, p => Assert.Equal(RegionDatabase.Sinnoh, p.Region));

        // And back to Jubilife City, Sinnoh's again
        var jubilife = SpawnLocations.ArrivedIn("jubilife_city")!;
        host.FlyTo = jubilife;
        Assert.True(host.Fly());
        Assert.Equal(new[] { RegionDatabase.Kanto, RegionDatabase.Sinnoh }, host.Crossings);
        Assert.Equal((jubilife.X, jubilife.Y), host.PlayerTile);
        Assert.Equal(RegionDatabase.Sinnoh, host.Region);
        Assert.Equal(before, party.Members);
    }

    [Fact]
    public void AFlightInsideTheRegionIsNoCrossing()
    {
        var story = new StoryState();
        var host = new HeadlessScriptHost(story, new Party(), new Inventory()) { FlyTo = SpawnLocations.ArrivedIn("eterna_city"), Region = RegionDatabase.Sinnoh };
        Assert.True(host.Fly());
        Assert.Empty(host.Crossings);
        Assert.Contains(host.Log, l => l.StartsWith("warp Sinnoh", StringComparison.Ordinal));
    }

    [Fact]
    public void EachRegionKeepsItsOwnPlaceToComeBackTo()
    {
        Assert.Equal(SpawnLocations.Variable, SpawnLocations.VariableOf(RegionDatabase.Sinnoh));
        Assert.Equal(SpawnLocations.Variable, SpawnLocations.VariableOf(null));
        Assert.Equal("VAR_SPAWN_LOCATION_KANTO", SpawnLocations.VariableOf(RegionDatabase.Kanto));

        var story = new StoryState();
        // Before any Pokémon Center: each region's first spawn
        Assert.Equal("twinleaf_town", SpawnLocations.Respawn(story, RegionDatabase.Sinnoh)!.Area);
        Assert.Equal("pallet_town", SpawnLocations.Respawn(story, RegionDatabase.Kanto)!.Area);

        // Viridian City's Center is Kanto's, and leaves Sinnoh's as it was
        var viridian = SpawnLocations.OfRoomAnywhere("ViridianCity_PokemonCenter_1F")!;
        Assert.Equal("viridian_city", viridian.Area);
        Assert.Equal("Kanto", viridian.Map);
        story.SetVar(SpawnLocations.VariableOf(RegionDatabase.Kanto), viridian.Id);
        story.SetVar(SpawnLocations.Variable, SpawnLocations.OfRoom("EternaPokemonCenter")!.Id);
        Assert.Equal("viridian_city", SpawnLocations.Respawn(story, RegionDatabase.Kanto)!.Area);
        Assert.Equal("eterna_city", SpawnLocations.Respawn(story, RegionDatabase.Sinnoh)!.Area);
        Assert.Equal(SpawnLocations.Respawn(story).Id, SpawnLocations.Respawn(story, RegionDatabase.Sinnoh)!.Id);
        // Sinnoh's rooms are found as they always were
        Assert.Equal(SpawnLocations.OfRoom("EternaPokemonCenter"), SpawnLocations.OfRoomAnywhere("EternaPokemonCenter"));
        Assert.Null(SpawnLocations.OfRoomAnywhere("PalParkLobby"));

        // Teleport in Kanto goes back to Kanto's Center, on Kanto's map
        var host = new HeadlessScriptHost(story, new Party(), new Inventory()) { Region = RegionDatabase.Kanto };
        Assert.True(host.Teleport());
        Assert.Contains(host.Log, l => l == $"warp Kanto {viridian.X} {viridian.Y}");
    }
}
