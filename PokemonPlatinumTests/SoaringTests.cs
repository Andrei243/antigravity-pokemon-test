using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>Soaring's rules (plan 38 · EX7, part a): steering, bounds, beacons, landing and the way back.</summary>
public class SoaringTests
{
    private static RegionMap Sinnoh => RegionMap.Of("Sinnoh")!;

    private static TownMapState With(params string[] towns)
    {
        var story = new StoryState();
        foreach (string town in towns) story.Set(SpawnLocations.ArrivedIn(town)!.ArrivalFlag);
        return new TownMapState(Sinnoh, story);
    }

    private static TownMapSpot Spot(TownMapState map, string town) => map.Spots.First(s => s.Town.Area == town);

    [Fact]
    public void AFlightIsRefusedBeforeTheHallOfFameWithoutAFlierAndWithoutAMap()
    {
        Assert.Equal("soar.refused.noMap", Soaring.WhyNot(true, null, true));
        Assert.Equal("soar.refused.hallOfFame", Soaring.WhyNot(false, Sinnoh, true));
        Assert.Equal("soar.refused.noFlier", Soaring.WhyNot(true, Sinnoh, false));
        Assert.Null(Soaring.WhyNot(true, Sinnoh, true));
    }

    [Fact]
    public void TheFlierTurnsClimbsDivesAndFliesFasterWithRun()
    {
        var flight = new Soaring(With("twinleaf_town"), (10, 10));
        Assert.Equal(Soaring.Start, flight.Height);
        float y0 = flight.Y;
        flight.Step(0, 0, false, 1f);
        Assert.Equal(y0 - Soaring.Cruise, flight.Y, 3);
        flight.Step(1, 0, false, 90f / Soaring.TurnRate);
        Assert.Equal(90f, flight.Heading, 2);
        float x0 = flight.X;
        flight.Step(0, 0, true, 0.5f);
        Assert.Equal(x0 + Soaring.Fast * 0.5f, flight.X, 3);
        flight.Step(0, 1, false, 10f);
        Assert.Equal(Soaring.Highest, flight.Height);
        flight.Step(0, -1, false, 10f);
        Assert.Equal(Soaring.Lowest, flight.Height);
        flight.Step(-1, 0, false, 180f / Soaring.TurnRate);
        Assert.Equal(270f, flight.Heading, 2);
    }

    [Fact]
    public void TheFlierStaysWithinTheMapsView()
    {
        var flight = new Soaring(With("twinleaf_town"));
        for (int i = 0; i < 200; i++) flight.Step(0, 0, true, 0.5f);
        Assert.Equal(flight.Bounds.Top, flight.Y);
        flight.Step(1, 0, false, 180f / Soaring.TurnRate);
        for (int i = 0; i < 400; i++) flight.Step(0, 0, true, 0.5f);
        Assert.Equal(flight.Bounds.Bottom, flight.Y);
        Assert.InRange(flight.X, flight.Bounds.Left, flight.Bounds.Right);
    }

    [Fact]
    public void ABeaconStandsOverAnOpenPlaceAndConfirmLandsThereButNotOnAShutOne()
    {
        var map = With("twinleaf_town", "snowpoint_city");
        Assert.Contains(map.Spots, s => s.Town.Area == "snowpoint_city" && s.Open);
        var flight = new Soaring(map);
        Assert.Equal(map.Spots.Count(s => s.Open), flight.Beacons.Count());

        var snowpoint = Spot(map, "snowpoint_city");
        var cell = snowpoint.Spot.Cells[0];
        flight.Place(cell.X + 0.5f, cell.Y + 0.5f);
        Assert.Equal(snowpoint, flight.BeaconUnder);
        Assert.Equal(snowpoint, flight.Land());

        // Sunyshore City has not been come to: no beacon, no landing there
        var sunyshore = Spot(map, "sunyshore_city");
        Assert.False(sunyshore.Open);
        var shut = sunyshore.Spot.Cells[0];
        flight.Place(shut.X + 0.5f, shut.Y + 0.5f);
        Assert.Null(flight.Land());
        Assert.DoesNotContain(sunyshore, flight.Beacons);
    }

    [Fact]
    public void CancelLandsAtTheNearestOpenPlace()
    {
        var map = With("twinleaf_town", "snowpoint_city");
        var flight = new Soaring(map);
        var snow = Spot(map, "snowpoint_city").Spot.Cells[0];
        flight.Place(snow.X + 2.5f, snow.Y + 1.5f);
        Assert.Equal("snowpoint_city", flight.Nearest()!.Town.Area);
        var twin = Spot(map, "twinleaf_town").Spot.Cells[0];
        flight.Place(twin.X + 1.5f, twin.Y - 1.5f);
        Assert.Equal("twinleaf_town", flight.Nearest()!.Town.Area);
        Assert.Null(new Soaring(With()).Nearest());
    }
}
