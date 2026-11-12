using System.Text.Json;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 38 · EX2: the day's report and the visitor. The report (<see cref="FieldReport"/>) lists the region's outbreaks
/// with their counts once its Hall of Fame is entered; a scan (<see cref="Visitors"/>) calls the weekday's visitor up once
/// a day with the National Pokédex, for an hour of the game's own time, in the first of its places with no outbreak; it is
/// met in that place's grass in slots 2 to 5 and nowhere else; the screen's report page runs with no window.
/// </summary>
public class FieldReportTests
{
    private static readonly Region Sinnoh = RegionDatabase.Get(RegionDatabase.Sinnoh)!;

    private static StoryState Champion()
    {
        var story = new StoryState();
        story.Set(Sinnoh.StoryCompleteFlag);
        return story;
    }

    private static MapArea AreaWithTables(string key)
    {
        var area = new MapArea { Key = key, Open = true };
        foreach (int weight in WorldAreaFile.LandSlotWeights)
            area.WildEncounters.Add(new WildEncounterEntry { SpeciesName = "Bidoof", MinLevel = 20, MaxLevel = 24, Weight = weight });
        return area;
    }

    // A Sunday, whose row in Sinnoh is the first
    private static readonly DateTime Sunday = new(2026, 11, 8);

    [Fact]
    public void EveryRegionWithVisitorsHasARowForEachDayOfTheWeekAtPlacesThatCanHaveAnOutbreak()
    {
        foreach (string region in new[] { RegionDatabase.Sinnoh, RegionDatabase.Kanto, RegionDatabase.Johto, RegionDatabase.Hoenn, RegionDatabase.Unova })
        {
            var rows = Visitors.RowsOf(region);
            Assert.Equal(7, rows.Count);
            var places = Outbreaks.PlacesOf(region).Select(p => p.Area).ToHashSet();
            foreach (DayOfWeek day in Enum.GetValues<DayOfWeek>())
            {
                var row = Visitors.RowFor(region, day);
                Assert.NotNull(row);
                Assert.All(row!.Places, p => Assert.Contains(p, places));
                var species = PokemonDatabase.Get(row.Species)!;
                Assert.False(species.Legendary || species.Mythical || species.IsOurs);
                Assert.False(species.DexNumbers.ContainsKey(RegionDatabase.Get(region)!.Dex!), $"{region}: {row.Species} is the region's own");
            }
        }
    }

    [Fact]
    public void TheReportListsTheDaysOutbreaksAndTheirCountsOnlyPastTheHallOfFame()
    {
        var encounters = new SpecialEncounters { DailyNumber = 0x55AA };
        Assert.Null(FieldReport.Of(Sinnoh, new StoryState(), encounters, true, Sunday));

        var day = Outbreaks.Today(RegionDatabase.Sinnoh, encounters.DailyNumber);
        for (int i = 0; i < 7; i++) encounters.CountOutbreak(day[0].Area);
        var report = FieldReport.Of(Sinnoh, Champion(), encounters, true, Sunday)!;
        Assert.Equal(day, report.Outbreaks.Select(o => o.Outbreak));
        Assert.Equal(7, report.Outbreaks[0].Met);
        Assert.Equal(0, report.Outbreaks[1].Met);
        Assert.Null(report.Visitor);
        // The next day the counts are gone and the outbreaks others
        encounters.DaysPass(1);
        var next = FieldReport.Of(Sinnoh, Champion(), encounters, true, Sunday.AddDays(1))!;
        Assert.All(next.Outbreaks, o => Assert.Equal(0, o.Met));
    }

    [Fact]
    public void AScanIsRefusedBeforeTheHallOfFameWithoutTheNationalPokedexAndTwiceInADay()
    {
        var encounters = new SpecialEncounters { DailyNumber = 0x55AA };
        var day = Outbreaks.Today(RegionDatabase.Sinnoh, encounters.DailyNumber);
        Assert.Equal(ScanState.NotOpen, Visitors.Check(Sinnoh, new StoryState(), encounters, true, DayOfWeek.Sunday, day));
        Assert.Equal(ScanState.NoNationalDex, Visitors.Check(Sinnoh, Champion(), encounters, false, DayOfWeek.Sunday, day));
        Assert.Null(Visitors.Scan(Sinnoh, Champion(), encounters, false, DayOfWeek.Sunday, day));
        Assert.Null(encounters.ScannedOn);

        var visitor = Visitors.Scan(Sinnoh, Champion(), encounters, true, DayOfWeek.Sunday, day);
        Assert.NotNull(visitor);
        Assert.Equal(Visitors.RowFor(RegionDatabase.Sinnoh, DayOfWeek.Sunday)!.Species, visitor!.Species);
        Assert.Equal(Visitors.Length, visitor.SecondsLeft);
        Assert.Equal(ScanState.Scanned, Visitors.Check(Sinnoh, Champion(), encounters, true, DayOfWeek.Sunday, day));
        Assert.Null(Visitors.Scan(Sinnoh, Champion(), encounters, true, DayOfWeek.Sunday, day));

        // A new day takes the visitor with it and allows another scan
        encounters.DaysPass(1);
        Assert.Null(encounters.Visitor);
        Assert.Equal(ScanState.Ready, Visitors.Check(Sinnoh, Champion(), encounters, true, DayOfWeek.Monday, Outbreaks.Today(RegionDatabase.Sinnoh, encounters.DailyNumber)));
    }

    [Fact]
    public void TheVisitorIsNeverOnAnOutbreaksPlaceAndNothingCallsWhereEveryPlaceHasOne()
    {
        var row = Visitors.RowFor(RegionDatabase.Sinnoh, DayOfWeek.Sunday)!;
        var first = new Outbreak(row.Places[0], "Bidoof", 6);
        Assert.Equal(row.Places[1], Visitors.PlaceOf(row, new[] { first }));
        var both = row.Places.Select(p => new Outbreak(p, "Bidoof", 6)).ToList();
        Assert.Null(Visitors.PlaceOf(row, both));
        var encounters = new SpecialEncounters { DailyNumber = 3 };
        Assert.Equal(ScanState.Nothing, Visitors.Check(Sinnoh, Champion(), encounters, true, DayOfWeek.Sunday, both));
        // Over many days the scan's place is never one of the day's outbreaks
        uint number = 1;
        for (int i = 0; i < 120; i++, number = SpecialEncounters.Next(number))
        {
            var day = Outbreaks.Today(RegionDatabase.Sinnoh, number);
            var e = new SpecialEncounters { DailyNumber = number };
            if (Visitors.Scan(Sinnoh, Champion(), e, true, (DayOfWeek)(i % 7), day) is { } called)
                Assert.DoesNotContain(day, o => o.Area == called.Area);
        }
    }

    [Fact]
    public void TheVisitorIsMetInItsPlacesGrassInSlotsTwoToFiveDuringItsHourAndNotAfter()
    {
        var visitor = new VisitorCall { Region = RegionDatabase.Sinnoh, Species = "Breloom", Area = "route_225", MinLevel = 48, MaxLevel = 52, SecondsLeft = Visitors.Length };
        var area = AreaWithTables("route_225");
        var moment = new EncounterMoment { Visitor = visitor };
        var slots = EncounterSlots.Grass(area, moment);
        for (int i = 0; i < 12; i++)
            Assert.Equal(i is >= Visitors.FirstSlot and <= Visitors.LastSlot ? "Breloom" : "Bidoof", slots[i].SpeciesName);
        Assert.Equal(48, slots[2].MinLevel);
        Assert.Equal(40, Visitors.Share);
        // Another place's grass is its own
        Assert.All(EncounterSlots.Grass(AreaWithTables("route_226"), moment), s => Assert.Equal("Bidoof", s.SpeciesName));
        // ...and with no visitor out, its own grass is its own
        Assert.All(EncounterSlots.Grass(area, new EncounterMoment()), s => Assert.Equal("Bidoof", s.SpeciesName));

        // The hour runs down and the visitor leaves, once
        var encounters = new SpecialEncounters { Visitor = visitor };
        Assert.Null(Visitors.Tick(encounters, 1800f));
        Assert.NotNull(encounters.Visitor);
        Assert.Same(visitor, Visitors.Tick(encounters, 1800f));
        Assert.Null(encounters.Visitor);
        Assert.Null(Visitors.Tick(encounters, 10f));
    }

    [Fact]
    public void AnOutbreakTakesItsPlaceFirstSoTheVisitorIsNeverLaidOverOne()
    {
        var visitor = new VisitorCall { Region = RegionDatabase.Sinnoh, Species = "Breloom", Area = "route_225", MinLevel = 48, MaxLevel = 52, SecondsLeft = 60 };
        var moment = new EncounterMoment { Visitor = visitor, Outbreaks = new[] { new Outbreak("route_225", "Gible", 6) } };
        Assert.DoesNotContain(EncounterSlots.Grass(AreaWithTables("route_225"), moment), s => s.SpeciesName == "Breloom");
    }

    [Fact]
    public void ASaveKeepsTheVisitorAndItsTimeAndLeavesTheFieldsOutWhenEmpty()
    {
        var empty = JsonSerializer.Serialize(new SpecialEncounters());
        Assert.DoesNotContain("Visitor", empty);
        Assert.DoesNotContain("ScannedOn", empty);
        var encounters = new SpecialEncounters
        {
            DailyNumber = 9,
            ScannedOn = 9,
            Visitor = new VisitorCall { Region = "Sinnoh", Species = "Sawk", Area = "route_229", MinLevel = 48, MaxLevel = 52, SecondsLeft = 1234.5f }
        };
        var back = JsonSerializer.Deserialize<SpecialEncounters>(JsonSerializer.Serialize(encounters))!;
        Assert.Equal(9u, back.ScannedOn);
        Assert.Equal("Sawk", back.Visitor!.Species);
        Assert.Equal("route_229", back.Visitor.Area);
        Assert.Equal(1234.5f, back.Visitor.SecondsLeft);
    }

    [Fact]
    public void TheScreensReportPageTurnsOnTheItemButtonAndAsksForAScanWithNoWindow()
    {
        var region = RegionMap.Of(RegionDatabase.Sinnoh)!;
        var screen = new TownMapScreen();
        screen.Open(new TownMapState(region, Champion()), TownMapMode.Item);
        // No report given: the item button does nothing
        Assert.False(screen.HasReport);
        Assert.False(screen.ToggleReport());

        var encounters = new SpecialEncounters { DailyNumber = 0x55AA };
        screen.SetReport(FieldReport.Of(Sinnoh, Champion(), encounters, true, Sunday));
        Assert.True(screen.HasReport);
        Assert.True(screen.ToggleReport());
        Assert.True(screen.ReportShown);
        screen.Confirm();
        Assert.True(screen.TakeScan());
        Assert.False(screen.TakeScan());
        Assert.False(screen.Zoomed);
        // A report that may not scan says no, and asks nothing
        screen.SetReport(FieldReport.Of(Sinnoh, Champion(), encounters, false, Sunday));
        screen.Confirm();
        Assert.False(screen.TakeScan());
        // Back to the map: the A button zooms again
        Assert.True(screen.ToggleReport());
        screen.Confirm();
        Assert.True(screen.Zoomed);

        // Fly's choice has no report
        screen.Open(new TownMapState(region, Champion()), TownMapMode.Fly);
        screen.SetReport(FieldReport.Of(Sinnoh, Champion(), encounters, true, Sunday));
        Assert.False(screen.ToggleReport());
    }
}

/// <summary>Plan 38 · EX2, the engine's part: the scan from the Town Map, the moment's visitor and its hour.</summary>
[Collection("Shared clocks and dice")]
public class FieldReportGameTests
{
    [Fact]
    public void AScanFromTheGameCallsTheVisitorIntoTheMomentAndItsHourRunsDown()
    {
        var engine = new GameEngine(new GameSettings { Muted = true });
        engine.InitializeHeadless();
        engine.PeopleStayPut = true;
        engine.NewGameRegion = "Sinnoh";
        engine.StartNewGame();
        var game = engine.Drive;
        var before = GameClock.FixedDate;
        try
        {
            GameClock.FixedDate = new DateTime(2026, 11, 8);
            game.Encounters.DailyNumber = 0x55AA;
            Assert.Null(engine.FieldReportNow());
            Assert.False(engine.ScanForVisitor());

            game.Story.Set("SinnohHallOfFame");
            game.Pokedex.UnlockNational();
            Assert.Equal(ScanState.Ready, engine.FieldReportNow()!.Scan);
            Assert.True(engine.ScanForVisitor());
            Assert.NotNull(game.EncounterMomentNow().Visitor);
            Assert.Equal(ScanState.Scanned, engine.FieldReportNow()!.Scan);
            Assert.NotNull(engine.FieldReportNow()!.Visitor);
        }
        finally
        {
            GameClock.FixedDate = before;
            Ruleset.Use(RulesPreset.Platinum);
        }
    }
}
