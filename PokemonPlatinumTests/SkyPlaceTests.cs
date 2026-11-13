using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>The places of the day in the sky (plan 38 · EX7, part b): lit by the date, laid from their seeds, their tables and finds.</summary>
public class SkyPlaceTests
{
    private static readonly string[] Regions = { "Sinnoh", "Kanto", "Johto", "Hoenn" };
    private static readonly DateTime Day = new(2026, 11, 8);

    private static RegionMap MapOf(string region) => RegionMap.Of(region)!;

    [Fact]
    public void EveryRegionWithAHallOfFameBuiltHasPlacesInItsViewOffFlysPlaces()
    {
        foreach (string region in Regions)
        {
            var map = MapOf(region);
            Assert.True(map.SkyPlaces.Count >= 4, region);
            Assert.Equal(Enum.GetValues<SkyPlaceKind>().ToHashSet(), map.SkyPlaces.Select(p => p.Kind).ToHashSet());
            foreach (var p in map.SkyPlaces)
            {
                Assert.True(map.InView(p.X, p.Y), $"{region} {p.Index} at {p.X},{p.Y}");
                Assert.DoesNotContain(map.FlySpots, f => f.Cells.Contains((p.X, p.Y)));
            }
        }
    }

    [Fact]
    public void ADatesLitPlacesAreTheSameTwiceAndOthersTheNextDay()
    {
        var map = MapOf("Sinnoh");
        var first = SkyPlaces.Lit(map, Day);
        Assert.Equal(first, SkyPlaces.Lit(map, Day));
        Assert.InRange(first.Count, 2, 3);
        Assert.Equal(first.Count, first.Distinct().Count());
        Assert.NotEqual(first, SkyPlaces.Lit(map, Day.AddDays(1)));
        // Over a week every place is lit at least once
        var week = Enumerable.Range(0, 7).SelectMany(d => SkyPlaces.Lit(map, Day.AddDays(d))).ToHashSet();
        Assert.Equal(map.SkyPlaces.Count, week.Count);
    }

    [Fact]
    public void APlacesFloorIsLaidFromItsSeedInOnePieceWithItsBeaconOnIt()
    {
        foreach (string region in Regions)
            foreach (var place in MapOf(region).SkyPlaces)
                for (int d = 0; d < 3; d++)
                {
                    var date = Day.AddDays(d);
                    var floor = SkyPlaces.Lay(place, date);
                    Assert.Equal(floor.Layout, SkyPlaces.Lay(place, date).Layout);
                    var beacon = SkyPlaces.BeaconOf(floor);
                    Assert.True(floor.IsOpen(beacon.X, beacon.Y));
                    var reached = floor.Reachable(beacon, new HashSet<(int, int)>());
                    int open = 0;
                    for (int y = 0; y < SkyPlaces.Height; y++)
                        for (int x = 0; x < SkyPlaces.Width; x++)
                            if (floor.Inside(x, y) && floor.IsOpen(x, y)) open++;
                    Assert.Equal(open, reached.Count(t => floor.Inside(t.X, t.Y)));
                    var file = SkyPlaces.ToFile(place, date, "place");
                    var made = file.ToMap();
                    Assert.Equal(SkyPlaces.MapNameOf(region), made.Name);
                    Assert.False(made.IsSolid(beacon.X, beacon.Y));
                }
    }

    [Fact]
    public void EveryPieceIsOpenWhereSquaresJoin()
    {
        foreach (var kind in Enum.GetValues<SkyPlaceKind>())
            foreach (var piece in SkyPlaces.PiecesOf(kind))
            {
                Assert.Equal(SkyPlaces.Square, piece.Length);
                foreach (int i in new[] { 3, 4 })
                {
                    Assert.Equal('.', piece[0][i]);
                    Assert.Equal('.', piece[SkyPlaces.Square - 1][i]);
                    Assert.Equal('.', piece[i][0]);
                    Assert.Equal('.', piece[i][SkyPlaces.Square - 1]);
                }
                // The middle, where a beacon may stand, is open
                Assert.Equal('.', piece[3][3]);
            }
    }

    [Fact]
    public void NoneOfAPlacesSpeciesIsTheRegionsOwnOrALegendary()
    {
        foreach (string region in Regions)
        {
            var own = SpeciesSources.DexOf(region).ToHashSet();
            Assert.True(SkyPlaces.Data.Regions.TryGetValue(region, out var tables), region);
            foreach (var (kind, list) in tables!.Kinds)
            {
                Assert.True(Enum.TryParse<SkyPlaceKind>(kind, out _), kind);
                foreach (string s in list)
                {
                    var species = PokemonDatabase.Get(s);
                    Assert.True(species != null, $"{region} {kind}: {s} is no species");
                    Assert.False(species!.Legendary || species.Mythical, $"{region} {kind}: {s}");
                    Assert.DoesNotContain(s, own);
                }
            }
            foreach (var place in MapOf(region).SkyPlaces)
            {
                var table = SkyPlaces.Table(place, Day);
                Assert.Equal(12, table.Count);
                Assert.All(table, row => Assert.InRange(row.MinLevel, tables.Levels[0], tables.Levels[1]));
            }
        }
    }

    [Fact]
    public void APlacesFindsAreItemsOnItsOpenGroundAndGoForTheDay()
    {
        foreach (string item in SkyPlaces.Data.Finds) Assert.NotNull(ItemDatabase.Get(item));
        var place = MapOf("Johto").SkyPlaces[0];
        var finds = SkyPlaces.FindsOf(place, Day);
        Assert.InRange(finds.Count, 2, 3);
        Assert.Equal(finds, SkyPlaces.FindsOf(place, Day));
        var floor = SkyPlaces.Lay(place, Day);
        Assert.All(finds, f =>
        {
            Assert.True(floor.IsOpen(f.X, f.Y));
            Assert.StartsWith("FLAG_DAILY_", f.Flag);
        });
        Assert.Equal(finds.Count, finds.Select(f => (f.X, f.Y)).Distinct().Count());
        // On the map they are item balls behind their daily flags, which a new day clears
        var file = SkyPlaces.ToFile(place, Day, "place");
        Assert.Equal(finds.Select(f => f.Flag), file.Npcs.Where(n => n.NpcType == "ItemBall").Select(n => n.HiddenBy));
        var story = new PokemonPlatinumEngine.Story.StoryState();
        foreach (var f in finds) story.Set(f.Flag);
        story.ClearDaily();
        Assert.All(finds, f => Assert.False(story.Has(f.Flag)));
    }

    [Fact]
    public void AFlightOverAPlaceOfTheDayLandsThere()
    {
        var map = MapOf("Sinnoh");
        var lit = SkyPlaces.Lit(map, Day);
        var flight = new Soaring(new TownMapState(map, new PokemonPlatinumEngine.Story.StoryState())) { Places = lit };
        var place = lit[0];
        flight.Place(place.X + 0.5f, place.Y + 0.5f);
        Assert.Equal(place, flight.PlaceUnder);
        var screen = new PokemonPlatinumEngine.UI.SoarScreen();
        screen.Open(flight, new Pokemon(PokemonDatabase.Get("Staraptor")!, 50));
        screen.Confirm();
        Assert.False(screen.IsActive);
        Assert.Null(screen.TakeLanding());
        Assert.Equal(place, screen.TakePlace());
        Assert.Equal("Sinnoh", RegionDatabase.RegionOfMap(SkyPlaces.MapNameOf("Sinnoh"))!.Id);
    }

    [Fact]
    public void AGameSavedOnAPlaceOfTheDayWakesAtItsRegionsPlaceToComeBackTo()
    {
        // Sinnoh's last Pokémon Center's town by the original's variable, and the region's first with none
        var story = new PokemonPlatinumEngine.Story.StoryState();
        story.SetVar(SpawnLocations.VariableOf("Sinnoh"), 5);
        var five = SpawnLocations.Get(5)!;
        Assert.Equal(new MapSpot(five.Map, five.X, five.Y, Direction.Down), SkyPlaces.ReturnPointFor(SkyPlaces.MapNameOf("Sinnoh"), () => story));
        var first = SpawnLocations.In("Kanto")[0];
        Assert.Equal(new MapSpot(first.Map, first.X, first.Y, Direction.Down), SkyPlaces.ReturnPointFor(SkyPlaces.MapNameOf("Kanto"), () => story));
        // Kanto's own variable, never Sinnoh's
        var kantoThird = SpawnLocations.In("Kanto")[2];
        story.SetVar(SpawnLocations.VariableOf("Kanto"), kantoThird.Id);
        Assert.Equal(kantoThird.X, SkyPlaces.ReturnPointFor(SkyPlaces.MapNameOf("Kanto"), () => story)!.X);
        // Any other map is no place of the day
        Assert.Null(SkyPlaces.ReturnPointFor("Sinnoh", () => story));
        Assert.Null(SkyPlaces.ReturnPointFor(null, () => story));
    }
}
