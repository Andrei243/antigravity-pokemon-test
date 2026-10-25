using System.Linq;
using System.Text.Json;
using Xunit;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumTests;

/// <summary>The chain of regions: every game starts in Sinnoh, finish each story to travel on to the next region, Kanto onward in generation order.</summary>
public class RegionTests
{
    [Fact]
    public void TestRegionsRunSinnohFirstThenInGenerationOrder()
    {
        var regions = RegionDatabase.All;

        // Plan 18 · W10: every new game begins in Sinnoh, and the chain goes on from Kanto in generation order
        Assert.Equal(RegionDatabase.Sinnoh, RegionDatabase.First.Id);
        Assert.Equal(RegionDatabase.Sinnoh, regions[0].Id);
        Assert.Equal(new[] { 4, 1, 2, 3, 5, 6, 7, 8, 9 }, regions.Select(r => r.Generation));
        Assert.Equal(regions.Count, regions.Select(r => r.Id).Distinct().Count());
        Assert.Equal(
            new[] { "Sinnoh", "Kanto", "Johto", "Hoenn", "Unova", "Kalos", "Alola", "Galar", "Paldea" },
            regions.Select(r => r.Id));
    }

    [Fact]
    public void TestNothingOffersAChoiceOfWhereTheJourneyBegins()
    {
        // No title phase, save field or script asks where to begin: the first region starts every game, and the
        // stand-in Kanto is reached through Sinnoh's Hall of Fame, or by --region or a tool
        Assert.True(RegionDatabase.First.IsBuilt);
        Assert.True(RegionDatabase.First.HasStory);
        Assert.DoesNotContain(System.Enum.GetNames<PokemonPlatinumEngine.UI.TitlePhase>(), n => n.Contains("Region"));
        Assert.DoesNotContain(typeof(SaveData).GetProperties(), p => p.Name is "StartRegion" or "OneRegion");
    }

    [Fact]
    public void TestEachRegionLinksOnlyToTheNextInTheChainAndTheLastToNothing()
    {
        var regions = RegionDatabase.All;
        for (int i = 0; i < regions.Count - 1; i++)
        {
            var link = RegionDatabase.LinkFrom(regions[i].Id);
            Assert.NotNull(link);
            Assert.Equal(regions[i + 1].Id, link!.To);
        }
        Assert.Null(RegionDatabase.LinkFrom(regions[^1].Id));
        Assert.Equal(regions.Count - 1, RegionDatabase.Links.Count);
    }

    [Fact]
    public void TestSinnohLeadsOnToKantoAndNothingLeadsBackToIt()
    {
        var link = RegionDatabase.LinkFrom(RegionDatabase.Sinnoh)!;
        Assert.Equal(RegionDatabase.Kanto, link.To);
        Assert.DoesNotContain(RegionDatabase.Links, l => l.To == RegionDatabase.Sinnoh);
        Assert.Equal(RegionDatabase.Unova, RegionDatabase.LinkFrom(RegionDatabase.Hoenn)!.To);

        // Finishing Sinnoh opens the way on to Kanto; the stand-in has maps, so the way is ready
        var progress = new PokemonPlatinumEngine.Story.StoryState();
        Assert.Equal(TravelCheck.StoryUnfinished, RegionDatabase.CheckTravel(link, progress));
        progress.CompleteRegion(RegionDatabase.Sinnoh);
        Assert.Equal(TravelCheck.Ready, RegionDatabase.CheckTravel(link, progress));
    }

    [Fact]
    public void TestKantoToJohtoIsByBoat()
    {
        Assert.Equal(Transport.Boat, RegionDatabase.LinkFrom(RegionDatabase.Kanto)!.Transport);
        Assert.Equal(RegionDatabase.Johto, RegionDatabase.LinkFrom(RegionDatabase.Kanto)!.To);
    }

    [Fact]
    public void TestEveryMapBelongsToExactlyOneRegion()
    {
        foreach (var name in MapDatabase.MapNames)
        {
            Assert.Single(RegionDatabase.All, r => r.Maps.Contains(name));
        }
    }

    [Fact]
    public void TestRegionsListOnlyMapsThatExist()
    {
        var names = MapDatabase.MapNames.ToHashSet();
        foreach (var region in RegionDatabase.All)
        {
            Assert.All(region.Maps, m => Assert.Contains(m, names));
            Assert.Equal(region.IsBuilt, region.Maps.Count > 0);
        }
    }

    [Fact]
    public void TestBuiltRegionsStartAndArriveOnWalkableTilesOfTheirOwnMaps()
    {
        foreach (var region in RegionDatabase.All.Where(r => r.IsBuilt))
        {
            foreach (var spot in new[] { region.Start!, region.ArrivalSpot! })
            {
                Assert.Contains(spot.Map, region.Maps);
                Assert.True(MapDatabase.Get(spot.Map).IsWalkable(spot.X, spot.Y), $"{region.Id} puts the player on a blocked tile");
            }
        }
    }

    [Fact]
    public void TestWarpsNeverLeaveTheirRegion()
    {
        foreach (var region in RegionDatabase.All)
        {
            foreach (var map in region.Maps.Select(MapDatabase.Get))
            {
                Assert.All(map.Warps, w => Assert.Contains(w.TargetMap, region.Maps));
            }
        }
    }

    [Fact]
    public void TestEveryBuiltRegionWithAWayOnHasAnAttendantAtItsDeparture()
    {
        // Plan 18 · W4, part b: strict for every link between two built regions. Each names its departure, a map of its
        // own region, with an attendant on it (asked of Everyone: Sinnoh's captain waits for the Hall of Fame's flag),
        // but for the one whose region's plan hasn't decided its way across yet
        var undecided = new List<string>();
        foreach (var region in RegionDatabase.All.Where(r => r.IsBuilt))
        {
            var link = RegionDatabase.LinkFrom(region.Id);
            if (link == null || !RegionDatabase.Get(link.To)!.IsBuilt) continue;
            if (link.Transport == Transport.Undecided)
            {
                undecided.Add($"{link.From}>{link.To}");
                Assert.Null(link.DepartureMap);
                continue;
            }

            Assert.NotNull(link.DepartureMap);
            Assert.Contains(link.DepartureMap, region.Maps);
            Assert.Contains(MapDatabase.Get(link.DepartureMap!).Everyone, n => n.IsTransportAttendant);
        }
        // Since plan 21 · H9, part b every link between two built regions names its departure (Hoenn's is Lilycove's
        // harbour): a link added undecided between two built regions is a finding
        Assert.Empty(undecided);
    }

    [Fact]
    public void TestEveryLinkSaysWhatCarriesThePlayer()
    {
        // Plan 18 · W4, part b: the vessels of plan 18's table, our own words, and the attendant's lines say them
        Assert.Equal("ship", RegionDatabase.LinkFrom(RegionDatabase.Sinnoh)!.VesselWord);
        Assert.Equal("ship", RegionDatabase.LinkFrom(RegionDatabase.Kanto)!.VesselWord);
        Assert.Equal("ferry", RegionDatabase.LinkFrom(RegionDatabase.Johto)!.VesselWord);
        Assert.Equal("ship", RegionDatabase.LinkFrom(RegionDatabase.Hoenn)!.VesselWord);
        Assert.Equal(Transport.Plane, RegionDatabase.LinkFrom(RegionDatabase.Unova)!.Transport);
        Assert.Equal("plane", RegionDatabase.LinkFrom(RegionDatabase.Galar)!.VesselWord);
        foreach (var link in RegionDatabase.Links)
            foreach (var check in new[] { TravelCheck.StoryUnfinished, TravelCheck.DestinationNotBuilt, TravelCheck.Ready })
                Assert.Contains(RegionDatabase.AttendantLines(link, check), l => l.Contains(link.VesselWord) || check == TravelCheck.Ready && l.Contains("aboard"));
        // Sinnoh's captain is on Canalave's pier, beside Sailor Eldritch, and only once Sinnoh's Hall of Fame is entered
        var captain = MapDatabase.Get("Sinnoh").Everyone.Single(n => n.IsTransportAttendant);
        Assert.Equal((45, 751), (captain.GridX, captain.GridY));
        Assert.Equal("SinnohHallOfFame", captain.ShownBy);
        Assert.True(MapDatabase.Get("Sinnoh").IsWalkable(44, 751), "the captain is spoken to from the pier");
    }

    [Fact]
    public void TestTheWayOnOpensOnlyAfterTheRegionsHallOfFame()
    {
        var progress = new PokemonPlatinumEngine.Story.StoryState();
        var kantoToJohto = RegionDatabase.LinkFrom(RegionDatabase.Kanto)!;

        Assert.Equal(TravelCheck.StoryUnfinished, RegionDatabase.CheckTravel(kantoToJohto, progress));

        progress.CompleteRegion(RegionDatabase.Kanto);
        Assert.True(progress.IsRegionComplete(RegionDatabase.Kanto));
        Assert.False(progress.IsRegionComplete(RegionDatabase.Johto));

        // Johto is built since plan 20 · J2, so the ship sails, and Hoenn since plan 21 · H1, so the way on from Johto opens
        // with its Hall of Fame; Unova since plan 22 · U2, so Hoenn's opens on, and Kalos has no maps yet, so nothing goes
        // on from Unova
        Assert.Equal(TravelCheck.Ready, RegionDatabase.CheckTravel(kantoToJohto, progress));
        var johtoToHoenn = RegionDatabase.LinkFrom(RegionDatabase.Johto)!;
        Assert.Equal(TravelCheck.StoryUnfinished, RegionDatabase.CheckTravel(johtoToHoenn, progress));
        progress.CompleteRegion(RegionDatabase.Johto);
        Assert.Equal(TravelCheck.Ready, RegionDatabase.CheckTravel(johtoToHoenn, progress));
        progress.CompleteRegion(RegionDatabase.Hoenn);
        Assert.Equal(TravelCheck.Ready, RegionDatabase.CheckTravel(RegionDatabase.LinkFrom(RegionDatabase.Hoenn)!, progress));
        progress.CompleteRegion(RegionDatabase.Unova);
        Assert.Equal(TravelCheck.DestinationNotBuilt, RegionDatabase.CheckTravel(RegionDatabase.LinkFrom(RegionDatabase.Unova)!, progress));
    }

    [Fact]
    public void TestJohtoIsBuiltWithItsFileAStartAndMapsOfItsOwn()
    {
        // Plan 20 · J2: Johto's record, its start in New Bark Town and every map its world makes, each its own
        var johto = RegionDatabase.Get(RegionDatabase.Johto)!;
        Assert.True(johto.IsBuilt);
        Assert.NotNull(johto.File);
        Assert.Equal(new[] { "Chikorita", "Cyndaquil", "Totodile" }, johto.Starters);
        Assert.Equal("NewBark_PlayersHouse_2F", johto.Start!.Map);
        Assert.Equal("Johto", johto.ArrivalSpot!.Map);
        Assert.Equal("Johto", johto.OverworldMap);
        var world = World.LoadAll().Single(w => w.Index.Region == "Johto");
        foreach (var entry in world.Index.Maps)
        {
            Assert.Contains(entry.Name, johto.Maps);
            Assert.Equal(RegionDatabase.Johto, RegionDatabase.RegionOfMap(entry.Name)?.Id);
        }
        // and the attendant at Kanto's departure takes a Champion there
        var link = RegionDatabase.LinkFrom(RegionDatabase.Kanto)!;
        Assert.Equal(RegionDatabase.Johto, link.To);
        Assert.Contains(MapDatabase.Get(link.DepartureMap!).NPCs, n => n.IsTransportAttendant);
        // Plan 20 · J12: and from Johto the ship to Hoenn sails from Olivine's port hall, its sailor before the quay
        var onward = RegionDatabase.LinkFrom(RegionDatabase.Johto)!;
        Assert.Equal(RegionDatabase.Hoenn, onward.To);
        Assert.Equal(Transport.Boat, onward.Transport);
        Assert.Equal("Olivine_PortHall", onward.DepartureMap);
        Assert.Contains(onward.DepartureMap, johto.Maps);
        Assert.Contains(MapDatabase.Get(onward.DepartureMap!).NPCs, n => n.IsTransportAttendant);
    }

    [Fact]
    public void TestFinishingTheRegionBeforeABuiltRegionLetsThePlayerTravel()
    {
        var progress = new PokemonPlatinumEngine.Story.StoryState();
        var sinnohToKanto = RegionDatabase.LinkFrom(RegionDatabase.Sinnoh)!;

        Assert.Equal(TravelCheck.StoryUnfinished, RegionDatabase.CheckTravel(sinnohToKanto, progress));

        // Finishing an unrelated region doesn't help
        progress.CompleteRegion(RegionDatabase.Hoenn);
        Assert.Equal(TravelCheck.StoryUnfinished, RegionDatabase.CheckTravel(sinnohToKanto, progress));

        progress.CompleteRegion(RegionDatabase.Sinnoh);
        Assert.Equal(TravelCheck.Ready, RegionDatabase.CheckTravel(sinnohToKanto, progress));
    }

    [Fact]
    public void TestTheAttendantNamesWhereTheWayGoes()
    {
        var link = RegionDatabase.LinkFrom(RegionDatabase.Kanto)!;
        foreach (var check in new[] { TravelCheck.StoryUnfinished, TravelCheck.DestinationNotBuilt, TravelCheck.Ready })
        {
            var lines = RegionDatabase.AttendantLines(link, check);
            Assert.NotEmpty(lines);
            Assert.Contains(lines, l => l.Contains("Johto"));
        }
    }

    [Fact]
    public void TestStoryFlagsSurviveTheSaveFile()
    {
        var progress = new PokemonPlatinumEngine.Story.StoryState();
        progress.CompleteRegion(RegionDatabase.Kanto);
        var save = new SaveData { StoryFlags = progress.Flags.ToList() };

        var loaded = JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(save))!;
        var restored = new PokemonPlatinumEngine.Story.StoryState();
        restored.Restore(loaded.StoryFlags);

        Assert.True(restored.IsRegionComplete(RegionDatabase.Kanto));
    }

    [Fact]
    public void TestSavesFromBeforeRegionsLoadInSinnohWithNoStoryDone()
    {
        // An older save has no StoryFlags and stands on one of Sinnoh's old hand-made maps
        var loaded = JsonSerializer.Deserialize<SaveData>("{\"CurrentMapName\":\"SandgemTown\"}")!;

        Assert.Empty(loaded.StoryFlags);
        Assert.Equal(RegionDatabase.Sinnoh, RegionDatabase.RegionOfMap(loaded.Place().Map)!.Id);
    }

    /// <summary>
    /// Plan 18 · W7: a built region whose world is imported has where its wild Pokémon live beside it, for the
    /// Pokédex's area page (the importer's <c>--data</c> writes it with the areas, for a GBA game as for Sinnoh).
    /// </summary>
    [Fact]
    public void TestEveryBuiltRegionsWorldFolderHasItsHabitats()
    {
        int worlds = 0;
        foreach (var region in RegionDatabase.All.Where(r => r.IsBuilt))
        {
            string folder = System.IO.Path.Combine(World.Folder, region.Folder);
            if (!System.IO.File.Exists(GameDataFiles.PathOf(System.IO.Path.Combine(folder, World.IndexFile)))) continue;
            worlds++;
            Assert.True(System.IO.File.Exists(GameDataFiles.PathOf(System.IO.Path.Combine(folder, WorldHabitatsFile.FileName))),
                $"{region.Id}'s world has no {WorldHabitatsFile.FileName}");
        }
        Assert.True(worlds >= 1);
    }
}
