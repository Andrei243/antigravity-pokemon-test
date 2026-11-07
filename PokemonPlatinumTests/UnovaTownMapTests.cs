using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 38 · EX10: Unova's Town Map. The region had every other thing of a finished region but the map, so Fly could not
/// reach it, the Town Map said nothing there and its Pokédex listing's AREA page drew no picture. The file is written by
/// hand like the others (a cell for each chunk of the overworld, the names Unova's own, every word ours).
/// </summary>
public class UnovaTownMapTests
{
    // Maps of this class's own, so nothing another test does to the database's maps shows here. Read only.
    private static readonly Lazy<World> LoadedWorld = new(() => World.LoadAll().Single(w => w.Index.Region == "Unova"));
    private static readonly Lazy<Dictionary<string, Map>> BuiltMaps = new(() => LoadedWorld.Value.BuildMaps().ToDictionary(m => m.Name));

    private static Map Overworld => BuiltMaps.Value["Unova"];
    private static Map MapNamed(string name) => BuiltMaps.Value.TryGetValue(name, out var map) ? map : MapDatabase.Get(name);
    private static Region Unova => RegionDatabase.Get(RegionDatabase.Unova)!;
    private static Region Sinnoh => RegionDatabase.Get(RegionDatabase.Sinnoh)!;
    private static RegionMap Town => RegionMap.Of(RegionDatabase.Unova) ?? throw new InvalidOperationException("Unova has no map");

    /// <summary>Castelia City is a map of ours, not a place on the overworld's grid: its block stands on two cells of empty sea.</summary>
    private const string Castelia = "castelia_city";

    [Fact]
    public void UnovasMapIsAGridOfTheOverworldsChunksAndEveryBlockIsNamedAsItsPlaceIs()
    {
        var map = Town;
        Assert.Equal("Unova", map.Map);
        Assert.Equal((Overworld.ChunkColumns, Overworld.ChunkRows), (map.Columns, map.Rows));
        Assert.Equal((0, 1, 28, 24), map.View);
        Assert.Equal((1, 22), map.Start);
        Assert.Equal("Aspertia City", map.BlockAt(1, 22)!.Name);
        Assert.All(map.Blocks, b => Assert.True(map.InBounds(b.X, b.Y), $"{b.Name} at {b.X},{b.Y}"));
        Assert.Empty(map.Hidden);
        foreach (var block in map.Blocks)
        {
            if (block.Area.Key == Castelia) { Assert.Equal("Castelia City", block.Name); continue; }
            var area = Overworld.AreaAt(block.X * Map.ChunkTiles + 1, block.Y * Map.ChunkTiles + 1);
            Assert.True(area is { Open: true }, $"{block.Name} at {block.X},{block.Y} lies on no open area");
            Assert.Equal(area!.DisplayName, block.Name);
            Assert.Equal(area.Key, block.Area.Key);
        }
        // Every open area the overworld lays out has its cells on the map: each chunk of land with an open area has a block
        for (int cy = 0; cy < Overworld.ChunkRows; cy++)
            for (int cx = 0; cx < Overworld.ChunkColumns; cx++)
                if (Overworld.AreaAt(cx * Map.ChunkTiles + 1, cy * Map.ChunkTiles + 1) is { Open: true })
                    Assert.NotNull(map.BlockAt(cx, cy));
    }

    [Fact]
    public void EveryPlaceOfTheMapIsOnItAndHasSomethingToSayThatTheFontCanWrite()
    {
        var map = Town;
        var used = map.Blocks.Select(b => b.Area).ToHashSet();
        Assert.True(map.Blocks.Count >= 250, $"{map.Blocks.Count} blocks");
        foreach (var place in map.Areas)
        {
            Assert.True(used.Contains(place), $"{place.Key} is on no block");
            Assert.False(string.IsNullOrWhiteSpace(place.About), place.Key);
            // The interface's font is Latin-1; anything else is drawn as a question mark
            Assert.All(place.About, c => Assert.True(c < 256, $"{place.Key}: '{c}'"));
            Assert.True(place.About.Length <= 110, $"{place.Key}: {place.About.Length} characters won't fit three lines");
        }
        // Routes are told of from the start, towns and cities once they have been come to
        foreach (var block in map.Blocks)
            Assert.Equal(block.Kind == RegionBlockKind.Town, block.Area.Flag != null);
    }

    [Fact]
    public void EveryFlagATownWaitsForIsSetByItsFirstArrival()
    {
        var map = Town;
        var spawns = SpawnLocations.In(RegionDatabase.Unova);
        foreach (var place in map.Areas.Where(a => a.Flag != null))
        {
            // Fly's towns are noted by the arrival itself (NoteArrival); the rest by the map's own list of arrivals
            bool spawnArrival = spawns.Any(s => s.UnlockedOnArrival && s.ArrivalFlag == place.Flag);
            Assert.True(spawnArrival || place.Arrival.Any(where => map.ArrivalFlags(where).Contains(place.Flag!)), $"nothing sets {place.Flag} ({place.Key})");
            if (spawnArrival) continue;
            // (Castelia City is no area of the world: its block names the areas and maps of its streets as its arrival)
            if (place.Key == Castelia) Assert.All(place.Arrival, where => Assert.True(LoadedWorld.Value.Index.Areas.Contains(where) || Unova.Maps.Contains(where), where));
            else Assert.Contains(place.Key, LoadedWorld.Value.Index.Areas);
        }
    }

    [Fact]
    public void FlysPlacesAreUnovasPokemonCentersOnTheCellsOfTheirTowns()
    {
        var map = Town;
        var spawns = SpawnLocations.In(RegionDatabase.Unova);
        // The player's house (1) is where a white-out goes, not a place to fly to; Victory Road and the League (12, 13) are
        // maps of their own and not on the overworld's grid
        Assert.Equal(new[] { 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 }, map.FlySpots.Select(f => f.Spawn));
        foreach (var spot in map.FlySpots)
        {
            var town = spawns.Single(s => s.Id == spot.Spawn);
            Assert.Equal("Unova", town.Map);
            Assert.True(town.UnlockedOnArrival);
            Assert.Contains(RegionMap.CellOf(town.X, town.Y), spot.Cells);
            Assert.All(spot.Cells, c => Assert.Equal(town.Area, Overworld.AreaAt(c.X * Map.ChunkTiles + 1, c.Y * Map.ChunkTiles + 1)?.Key));
            Assert.All(spot.Cells, c => Assert.Equal(RegionBlockKind.Town, map.BlockAt(c.X, c.Y)!.Kind));
        }
    }

    [Fact]
    public void AChampionWhoHasEnteredUnovaCanFlyThereAndLandsAtAspertiaCityAndFloccesyTown()
    {
        var story = new StoryState();
        story.Set(Sinnoh.StoryCompleteFlag);
        var dexes = new List<string> { Sinnoh.Dex! };
        // Not before its Pokédex is entered
        Assert.False(Crossing.CanFly(Sinnoh, Unova, story, dexes));
        Assert.Equal(new[] { RegionDatabase.Sinnoh }, Crossing.FlyRegions(Sinnoh, story, dexes).Select(r => r.Id));
        dexes.Add(Unova.Dex!);
        Assert.True(Crossing.CanFly(Sinnoh, Unova, story, dexes));
        Assert.True(Crossing.CanFly(Unova, Sinnoh, story, dexes));
        Assert.Equal(new[] { RegionDatabase.Sinnoh, RegionDatabase.Unova }, Crossing.FlyRegions(Sinnoh, story, dexes).Select(r => r.Id));

        // Aspertia City and Floccesy Town are open once they have been come to; the rest stay shut
        var aspertia = SpawnLocations.In(RegionDatabase.Unova).Single(s => s.Area == "aspertia_city" && s.Room == "AspertiaCity_PokemonCenter");
        var floccesy = SpawnLocations.In(RegionDatabase.Unova).Single(s => s.Area == "floccesy_town");
        story.Set(SpawnLocations.FlyFlag(aspertia));
        story.Set(SpawnLocations.FlyFlag(floccesy));
        var sinnohMap = RegionMap.Of(RegionDatabase.Sinnoh)!;
        var tabs = new[] { new TownMapState(sinnohMap, story, RegionMap.CellOf(305, 540)), new TownMapState(Town, story) };
        Assert.Equal(new[] { 2, 3 }, tabs[1].Spots.Where(s => s.Open).Select(s => s.Town.Id));

        var screen = new TownMapScreen();
        screen.Open(tabs, 0, TownMapMode.Fly, flier: new Pokemon(PokemonDatabase.Get("Staravia")!, 20));
        Assert.True(screen.HasTabs);
        Assert.True(screen.ChangeTab(1));
        Assert.Same(tabs[1], screen.State);
        Assert.False(screen.State!.Here.HasValue);
        MoveTo(screen, Town.FlySpots.Single(f => f.Spawn == floccesy.Id).Cells[0]);
        screen.Confirm();
        Assert.Equal(floccesy.Id, screen.TakeChoice()!.Id);

        // And a town not yet come to is refused
        screen.Open(tabs, 0, TownMapMode.Fly, flier: null);
        screen.ChangeTab(1);
        var nimbasa = SpawnLocations.In(RegionDatabase.Unova).Single(s => s.Area == "nimbasa_city");
        MoveTo(screen, Town.FlySpots.Single(f => f.Spawn == nimbasa.Id).Cells[0]);
        Assert.False(screen.State!.CanFlyTo(screen.CursorX, screen.CursorY));
        screen.Confirm();
        Assert.Null(screen.TakeChoice());

        // Aspertia City is chosen from the cell Black 2 draws it on
        screen.Open(tabs, 0, TownMapMode.Fly, flier: null);
        screen.ChangeTab(1);
        MoveTo(screen, Town.FlySpots.Single(f => f.Spawn == aspertia.Id).Cells[0]);
        screen.Confirm();
        var chosen = screen.TakeChoice();
        Assert.Equal(aspertia.Id, chosen!.Id);
        Assert.Equal("Unova", chosen.Map);
    }

    private static void MoveTo(TownMapScreen screen, (int X, int Y) cell)
    {
        for (int i = 0; i < 80 && (screen.CursorX, screen.CursorY) != cell; i++)
            screen.Move(Math.Sign(cell.X - screen.CursorX), Math.Sign(cell.Y - screen.CursorY));
        Assert.Equal(cell, (screen.CursorX, screen.CursorY));
    }

    [Fact]
    public void TheTownMapOpensWhereverTheGameStandsInUnova()
    {
        // The engine opens the map of the region a map belongs to: the overworld, its rooms and the maps of their own
        var unova = Unova;
        foreach (string name in unova.Maps)
            Assert.Same(Town, RegionMap.ForMap(name));
        var story = new StoryState();
        var state = new TownMapState(Town, story, RegionMap.CellOf(47, 740));
        Assert.Equal("Aspertia City", state.HereName);
        Assert.Equal((1, 23), state.FirstCursor());
        // Aspertia City's words wait for the first arrival; a route's don't wait for anything
        Assert.False(state.Tells(Town.BlockAt(1, 22)!.Area));
        Assert.True(state.Tells(Town.BlockAt(1, 21)!.Area));
        story.Set("FLAG_FIRST_ARRIVAL_ASPERTIA_CITY");
        Assert.True(new TownMapState(Town, story).Tells(Town.BlockAt(1, 22)!.Area));
    }

    [Fact]
    public void ARoomIsShownAtTheCellItsWayInIsOn()
    {
        var places = Town.PlacesOf(Unova.Maps.Select(MapNamed));
        Assert.Equal(RegionMap.CellOf(48, 740), places["AspertiaCity_PokemonCenter"]);
        Assert.Equal(RegionMap.CellOf(105, 694), places["FloccesyTown_PokemonCenter"]);
        // Castelia City stands on its block, whichever way the player came to it
        Assert.Equal((11, 23), places["CasteliaCity_Plaza"]);
        Assert.Equal((11, 23), places["CasteliaCity_PokemonCenter"]);
        Assert.Equal("Castelia City", Town.BlockAt(places["CasteliaCity_Gym"].X, places["CasteliaCity_Gym"].Y)!.Name);
        Assert.False(places.ContainsKey("Unova"));
        // Every room and cave of Unova has a cell the map shows: some warp leads into it, or the file places it (the Abyssal
        // Ruins are dived to, the Gear Station's train rides to Anville Town, the League's last rooms are stairs and a lift away)
        Assert.All(places.Values, c => Assert.True(Town.InView(c.X, c.Y)));
        var without = Unova.Maps.Where(n => n != "Unova" && !places.ContainsKey(n)).ToList();
        Assert.Empty(without);
        // And every place the file names itself is a map of the region
        Assert.All(Town.Placed.Keys, n => Assert.Contains(n, Unova.Maps));
        Assert.True(places.Count >= 250, $"{places.Count} places");
    }

    [Fact]
    public void APlaceTheFileNamesItselfMustBeOnABlockOfTheView()
    {
        RegionMapFile File() => new()
        {
            Map = "Unova", Columns = 4, Rows = 4, View = new[] { 0, 0, 4, 4 }, Cursor = new[] { 0, 0, 3, 3 }, Start = new[] { 1, 1 },
            Areas = { new RegionPlaceRecord { Key = "a", About = "A." } },
            Blocks = { new RegionBlockRecord { X = 1, Y = 1, Name = "A", Area = "a" } },
            Places = { ["SomeCave"] = "1,1" }
        };
        Assert.Equal((1, 1), new RegionMap("test", File()).Placed["SomeCave"]);
        var empty = File();
        empty.Places["SomeCave"] = "2,2";
        Assert.Contains("SomeCave", Assert.Throws<InvalidDataException>(() => new RegionMap("test", empty)).Message);
        var outside = File();
        outside.Places["SomeCave"] = "9,9";
        Assert.Contains("SomeCave", Assert.Throws<InvalidDataException>(() => new RegionMap("test", outside)).Message);
        var two = File();
        two.Places["SomeCave"] = "1,1 1,2";
        Assert.Throws<InvalidDataException>(() => new RegionMap("test", two));
    }

    [Fact]
    public void ThePokedexsAreaPageDrawsUnovasMapWithEveryPlaceInView()
    {
        // A listing of Unova's own Pokédex draws Unova's map (PokedexScreen.AreaRegion), and the places a species lives
        // are lit on cells the map shows
        var unova = Unova;
        var pokedex = new Pokedex();
        pokedex.Enter(unova);
        var dex = new PokedexScreen();
        dex.Open(pokedex, null, unova);
        dex.OpenSearch();
        dex.Sideways(1);   // Sinnoh to Unova
        dex.Search();
        Assert.Equal(unova.Dex, dex.Mode);
        Assert.Same(unova, dex.AreaRegion);
        Assert.Same(Town, RegionMap.Of(dex.AreaRegion.Folder));

        var habitats = Habitats.Of(unova)!;
        // (its grid is cut to the land, the same cells of the same matrix)
        Assert.True(habitats.Width <= Town.Columns && habitats.Height <= Town.Rows, $"{habitats.Width} by {habitats.Height}");
        int lit = 0;
        foreach (var species in PokemonDatabase.GetAll())
            foreach (var place in habitats.Of(species.Name))
                foreach (var (x, y) in place.Cells)
                {
                    Assert.True(Town.InView(x, y), $"{species.Name} at {place.Name}: {x},{y} is off the map");
                    lit++;
                }
        Assert.True(lit > 1000, $"{lit} lit cells");
    }
}
