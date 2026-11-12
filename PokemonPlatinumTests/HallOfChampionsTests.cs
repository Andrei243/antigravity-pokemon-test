using System;
using System.Linq;
using System.Text.Json;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 41 · EG2: every region's Hall of Fame in one hall. An entry keeps its region, each region's first team outlives the
/// thirty, the record sorts by region in tabs, and the Laurel Gallery stands a plinth for each region finished.
/// </summary>
public class HallOfChampionsTests
{
    private static Party Team(string species = "Bidoof", int level = 50)
    {
        var party = new Party();
        party.Add(new Pokemon(PokemonDatabase.Get(species)!, level));
        return party;
    }

    private static (string, int) Mine(Pokemon _) => ("Lucas", 1);

    [Fact]
    public void AnEntryKeepsItsRegionAndItsNumberThroughASave()
    {
        var records = new HallOfFame();
        records.Enter(Team(), Mine, new DateTime(2026, 11, 12), region: RegionDatabase.Kanto);
        var saved = JsonSerializer.Deserialize<SavedHallOfFameEntry>(JsonSerializer.Serialize(SavedHallOfFameEntry.From(records.Entries[0])))!;
        var back = saved.ToEntry();
        Assert.Equal(RegionDatabase.Kanto, back.Region);
        Assert.Equal(1, back.Number);
    }

    [Fact]
    public void AnOlderSavesEntriesHaveNoRegionAndReadAsBefore()
    {
        var back = JsonSerializer.Deserialize<SavedHallOfFameEntry>("{\"Date\":\"2026-10-31T00:00:00\",\"Team\":[]}")!.ToEntry();
        Assert.Null(back.Region);
        Assert.Equal(0, back.Number);
        // and an entry with no region writes neither field, so a save of the old shape is the same bytes
        string json = JsonSerializer.Serialize(SavedHallOfFameEntry.From(back));
        Assert.DoesNotContain("Region", json);
        Assert.DoesNotContain("Number", json);
    }

    [Fact]
    public void EachRegionsFirstTeamOutlivesThirtyLaterOnes()
    {
        var records = new HallOfFame();
        records.Enter(Team("Shinx"), Mine, new DateTime(2026, 1, 1), region: RegionDatabase.Sinnoh);
        for (int i = 0; i < HallOfFame.Kept; i++) records.Enter(Team(), Mine, new DateTime(2026, 2, 1), region: RegionDatabase.Kanto);
        Assert.DoesNotContain(records.Entries, e => e.Region == RegionDatabase.Sinnoh);
        var sinnoh = records.Of(RegionDatabase.Sinnoh);
        Assert.Single(sinnoh);
        Assert.Equal("Shinx", sinnoh[0].Entry.Team[0].Species);
        Assert.Equal(1, sinnoh[0].Number);
        // Kanto's first is among the thirty kept, and shown once
        Assert.Equal(HallOfFame.Kept, records.Of(RegionDatabase.Kanto).Count);

        // and a save keeps them
        var again = new HallOfFame();
        again.Restore(records.Total, records.Debut, records.Entries, records.Firsts.Values);
        Assert.Single(again.Of(RegionDatabase.Sinnoh));
        Assert.Equal(HallOfFame.Kept, again.Of(RegionDatabase.Kanto).Count);
    }

    [Fact]
    public void TheScreensTabsSortTheTeamsByRegionOnceTwoHaveEntries()
    {
        var records = new HallOfFame();
        var screen = new HallOfFameScreen();
        records.Enter(Team(), Mine, new DateTime(2026, 1, 1), region: RegionDatabase.Sinnoh);
        screen.Open();
        Assert.Empty(screen.Tabs(records));
        Assert.Single(screen.Teams(records));

        records.Enter(Team("Pikachu"), Mine, new DateTime(2026, 2, 1), region: RegionDatabase.Kanto);
        records.Enter(Team("Pikachu"), Mine, new DateTime(2026, 3, 1), region: RegionDatabase.Kanto);
        screen.Open();
        Assert.Equal(new[] { RegionDatabase.Sinnoh, RegionDatabase.Kanto }, screen.Tabs(records));
        // The PC opens on the newest team's region
        Assert.Equal(RegionDatabase.Kanto, screen.Tab);
        Assert.Equal(2, screen.Teams(records).Count);
        screen.ChangeTab(1, records);
        Assert.Equal(RegionDatabase.Sinnoh, screen.Tab);
        Assert.Single(screen.Teams(records));
        // and a plinth opens on its own region's tab
        screen.Open(RegionDatabase.Sinnoh);
        Assert.Equal(RegionDatabase.Sinnoh, screen.Tab);
        Assert.Equal("Bidoof", screen.Shown(records)!.Value.Entry.Team[0].Species);
    }

    [Fact]
    public void TeamsFromBeforeTheRegionWasKeptStandUnderEarlierTeams()
    {
        var records = new HallOfFame();
        records.Restore(1, new DateTime(2025, 6, 1), new[] { new HallOfFameEntry(new DateTime(2025, 6, 1), HallOfFame.TeamOf(Team("Turtwig"), Mine)) });
        records.Enter(Team(), Mine, new DateTime(2026, 1, 1), region: RegionDatabase.Kanto);
        var screen = new HallOfFameScreen();
        screen.Open(HallOfFameScreen.EarlierTab);
        Assert.Contains(HallOfFameScreen.EarlierTab, screen.Tabs(records));
        Assert.Equal("Turtwig", screen.Shown(records)!.Value.Entry.Team[0].Species);
    }

    [Fact]
    public void TheGalleryStandsAPlinthForEachRegionFinished()
    {
        var story = new StoryState();
        var records = new HallOfFame();
        var circuit = new CircuitState();
        Assert.Empty(LaurelGallery.Plinths(story, records, circuit));
        story.Set(RegionDatabase.Get(RegionDatabase.Sinnoh)!.StoryCompleteFlag);
        story.Set(RegionDatabase.Get(RegionDatabase.Kanto)!.StoryCompleteFlag);
        Assert.Equal(new[] { RegionDatabase.Sinnoh, RegionDatabase.Kanto }, LaurelGallery.Plinths(story, records, circuit).Select(p => p.Of));
        // EARLIER TEAMS once older teams are kept and more than one region is finished; the circuit's once a final is won
        records.Restore(1, null, new[] { new HallOfFameEntry(new DateTime(2025, 6, 1), HallOfFame.TeamOf(Team(), Mine)) });
        circuit.Won = 1;
        circuit.Finals.Add(new SavedHallOfFameEntry());
        var plinths = LaurelGallery.Plinths(story, records, circuit);
        Assert.Contains(plinths, p => p.Of == "earlier");
        Assert.Contains(plinths, p => p.Of == ChampionsCircuit.CircuitRegion);
        // no two plinths share a tile, and every one has its script in the gallery's file
        var places = LaurelGallery.EveryPlace().ToList();
        Assert.Equal(places.Count, places.Select(p => (p.X, p.Y)).Distinct().Count());
        foreach (var p in places) Assert.NotNull(ScriptLibrary.Default.Find(p.Script, LaurelGallery.Map));
    }

    [Fact]
    public void EachRegionsHallOfFameWearsItsOwnChampionRibbonAndEntersItsRegion()
    {
        var host = new HeadlessScriptHost { Region = RegionDatabase.Hoenn };
        host.Party.Add(new Pokemon(PokemonDatabase.Get("Treecko")!, 50));
        host.EnterHallOfFame();
        Assert.Equal(RegionDatabase.Hoenn, host.HallOfFame.Entries[^1].Region);
        Assert.True(host.HallOfFame.Firsts.ContainsKey(RegionDatabase.Hoenn));
    }

    [Fact]
    public void APlinthsScriptOpensTheRecordOnItsRegion()
    {
        var host = new HeadlessScriptHost();
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find("PlinthKanto", LaurelGallery.Map)!, null);
        runner.RunToEnd();
        Assert.Empty(host.Problems);
        Assert.Contains("open HallOfFame Kanto", host.Log);
    }
}
