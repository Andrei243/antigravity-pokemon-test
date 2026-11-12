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

    private static Pokemon Staraptor => new(PokemonDatabase.Get("Staraptor")!, 40);

    [Fact]
    public void TheScreenLandsOverABeaconAndSaysNoElsewhere()
    {
        var map = With("twinleaf_town", "snowpoint_city");
        var screen = new PokemonPlatinumEngine.UI.SoarScreen();
        var flight = new Soaring(map, (5, 5));
        screen.Open(flight, Staraptor);
        Assert.True(screen.IsActive);
        // Over no beacon: A is refused, the flight goes on
        flight.Place(map.Region.View.X + 0.1f, map.Region.View.Y + 0.1f);
        screen.Confirm();
        Assert.True(screen.IsActive);
        Assert.Null(screen.TakeLanding());

        var snow = Spot(map, "snowpoint_city");
        flight.Place(snow.Spot.Cells[0].X + 0.5f, snow.Spot.Cells[0].Y + 0.5f);
        Assert.Equal(snow, screen.Under);
        screen.Confirm();
        Assert.False(screen.IsActive);
        Assert.False(screen.Turned);
        Assert.Equal(snow, screen.TakeLanding());
        Assert.Null(screen.TakeLanding());
    }

    [Fact]
    public void TheScreensCancelEndsAtTheNearestTownAndItsStepsBankTheFlier()
    {
        var map = With("twinleaf_town");
        var screen = new PokemonPlatinumEngine.UI.SoarScreen();
        screen.Open(new Soaring(map, (5, 5)), Staraptor);
        float heading = screen.Flight!.Heading;
        for (int i = 0; i < 30; i++) screen.Steer(1f, 0f, false, 1f / 30f);
        Assert.True(screen.Bank > 0.5f);
        Assert.NotEqual(heading, screen.Flight.Heading);
        for (int i = 0; i < 60; i++) screen.Steer(0f, 0f, false, 1f / 30f);
        Assert.True(screen.Bank < 0.1f);
        screen.Cancel();
        Assert.True(screen.Turned);
        Assert.Equal("twinleaf_town", screen.TakeLanding()!.Town.Area);
    }

    [Fact]
    public void FlysChoiceOffersSoarOnlyWhereTheGameSaysAndHandsItOverOnce()
    {
        var screen = new PokemonPlatinumEngine.UI.TownMapScreen();
        var map = With("twinleaf_town");
        screen.Open(map, TownMapMode.Fly, Staraptor);
        Assert.False(screen.CanSoar);
        Assert.False(screen.AskSoar());
        screen.SoarableTab = tab => tab == 0;
        Assert.True(screen.CanSoar);
        Assert.True(screen.AskSoar());
        Assert.False(screen.IsActive);
        Assert.True(screen.TakeSoar());
        Assert.False(screen.TakeSoar());
        // Looking at the map out of the bag offers no soaring, whatever is told
        screen.Open(map, TownMapMode.Item);
        screen.SoarableTab = _ => true;
        Assert.False(screen.CanSoar);
    }

    [Fact]
    public void TheSkysCameraRisesWithTheHeightAndTheCloudsAreTheSameForATime()
    {
        var flight = new Soaring(With("twinleaf_town"), (10, 10));
        Assert.Equal(PokemonPlatinumEngine.Graphics.SkyRenderer.PitchLow, PokemonPlatinumEngine.Graphics.SkyRenderer.PitchOf(Soaring.Lowest), 3);
        Assert.Equal(PokemonPlatinumEngine.Graphics.SkyRenderer.PitchHigh, PokemonPlatinumEngine.Graphics.SkyRenderer.PitchOf(Soaring.Highest), 3);
        var (eye, target) = PokemonPlatinumEngine.Graphics.SkyRenderer.CameraOf(flight);
        Assert.True(eye.Y > target.Y);
        // Facing north, the camera is south of the flier (behind it)
        Assert.True(eye.Z > PokemonPlatinumEngine.Graphics.SkyRenderer.PlaceOf(flight).Z);
        var a = PokemonPlatinumEngine.Graphics.SkyRenderer.CloudsOf(Sinnoh, 12.5f);
        var b = PokemonPlatinumEngine.Graphics.SkyRenderer.CloudsOf(Sinnoh, 12.5f);
        Assert.Equal(a, b);
        Assert.NotEmpty(a);
        Assert.All(a, c => Assert.InRange(c.Altitude, PokemonPlatinumEngine.Graphics.SkyRenderer.AltitudeOf(2f), PokemonPlatinumEngine.Graphics.SkyRenderer.AltitudeOf(4f)));
        Assert.NotEqual(a, PokemonPlatinumEngine.Graphics.SkyRenderer.CloudsOf(Sinnoh, 40f));
    }
}
