using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// Alola's Town Map (plan 23, wave 91: the loose ends of Z5 to Z7). Each island is a matrix of its own, so the region's
/// map lays all four onto one grid, each at the cell of its north-west chunk (<see cref="RegionMap.Overworlds"/>):
/// Melemele at 0,0, Ula'ula at 17,0, Akala at 17,12 and Poni at 1,16. Fly reaches every island's Pokémon Centers, the
/// picture paints the four, and the Pokédex's area page lights Akala's, Ula'ula's and Poni's places on the same grid.
/// </summary>
public class AlolaTownMapTests
{
    // Maps of this class's own, so nothing another test does to the database's maps shows here. Read only.
    private static readonly Lazy<World> LoadedWorld = new(() => World.LoadAll().Single(w => w.Index.Region == "Alola"));
    private static readonly Lazy<Dictionary<string, Map>> BuiltMaps = new(() => LoadedWorld.Value.BuildMaps().ToDictionary(m => m.Name));
    private static Map MapNamed(string name) => BuiltMaps.Value.TryGetValue(name, out var map) ? map : MapDatabase.Get(name);
    private static Region Alola => RegionDatabase.Get(RegionDatabase.Alola)!;
    private static RegionMap Town => RegionMap.Of(RegionDatabase.Alola) ?? throw new InvalidOperationException("Alola has no map");

    private static readonly string[] Islands = { "Melemele", "Ulaula", "Akala", "Poni" };

    /// <summary>The island a cell of the grid lies on, and the cell on the island itself.</summary>
    private static (Map Island, int X, int Y)? IslandAt(int cx, int cy)
    {
        foreach (var (name, (ox, oy)) in Town.Overworlds)
        {
            var island = BuiltMaps.Value[name];
            int lx = cx - ox, ly = cy - oy;
            if (lx >= 0 && ly >= 0 && lx < island.ChunkColumns && ly < island.ChunkRows) return (island, lx, ly);
        }
        return null;
    }

    private static bool AreaInChunk(Map island, int lx, int ly, string key)
    {
        for (int y = 0; y < Map.ChunkTiles; y += 2)
            for (int x = 0; x < Map.ChunkTiles; x += 2)
                if (island.AreaAt(lx * Map.ChunkTiles + x, ly * Map.ChunkTiles + y)?.Key == key) return true;
        return false;
    }

    [Fact]
    public void TheFourIslandsLieOnOneGridWithoutOverlappingAndEveryBlockIsNamedAsItsPlaceIs()
    {
        var map = Town;
        Assert.Equal("Melemele", map.Map);
        Assert.Equal(Islands.OrderBy(n => n), map.Overworlds.Keys.OrderBy(n => n));
        Assert.Equal((0, 0), map.Overworlds["Melemele"]);
        Assert.All(Islands, n => Assert.True(map.IsOverworld(n)));
        Assert.False(map.IsOverworld("VastPoniCanyon"));
        // Each island's rectangle of chunks is its own, and inside the grid
        var taken = new HashSet<(int, int)>();
        foreach (var (name, (ox, oy)) in map.Overworlds)
        {
            var island = BuiltMaps.Value[name];
            Assert.True(ox + island.ChunkColumns <= map.Columns && oy + island.ChunkRows <= map.Rows, $"{name} reaches past the grid");
            for (int y = 0; y < island.ChunkRows; y++)
                for (int x = 0; x < island.ChunkColumns; x++)
                    Assert.True(taken.Add((ox + x, oy + y)), $"{name} overlaps another island at {ox + x},{oy + y}");
        }
        // Every block lies on its place on its island, and every island has blocks
        foreach (var block in map.Blocks)
        {
            var at = IslandAt(block.X, block.Y);
            Assert.NotNull(at);
            Assert.True(AreaInChunk(at!.Value.Island, at.Value.X, at.Value.Y, block.Area.Key), $"{block.Name} at {block.X},{block.Y} is not on {at.Value.Island.Name}'s {block.Area.Key}");
            Assert.Equal(at.Value.Island.Areas.First(a => a.Key == block.Area.Key).DisplayName, block.Name);
        }
        foreach (string island in Islands)
            Assert.Contains(map.Blocks, b => IslandAt(b.X, b.Y)!.Value.Island.Name == island);
        // A tile of an island is shown at the island's cell plus its own chunk
        Assert.Equal((17 + 4, 12 + 9), map.CellOn("Akala", 129, 309));
        Assert.Equal((7, 4), map.CellOn("Melemele", 238, 158));
        Assert.Equal("Heahea City", map.BlockAt(21, 21)!.Name);
        Assert.Equal("Malie City", map.BlockAt(map.CellOn("Ulaula", 34, 331).X, map.CellOn("Ulaula", 34, 331).Y)!.Name);
        Assert.Equal("Seafolk Village", map.BlockAt(map.CellOn("Poni", 34, 243).X, map.CellOn("Poni", 34, 243).Y)!.Name);
    }

    [Fact]
    public void EveryPlaceOfTheMapIsOnItAndHasSomethingToSayThatTheFontCanWrite()
    {
        var map = Town;
        var used = map.Blocks.Select(b => b.Area).ToHashSet();
        Assert.True(map.Blocks.Count >= 200, $"{map.Blocks.Count} blocks");
        foreach (var place in map.Areas)
        {
            Assert.True(used.Contains(place), $"{place.Key} is on no block");
            Assert.False(string.IsNullOrWhiteSpace(place.About), place.Key);
            Assert.All(place.About, c => Assert.True(c < 256, $"{place.Key}: '{c}'"));
            Assert.True(place.About.Length <= 110, $"{place.Key}: {place.About.Length} characters won't fit three lines");
        }
        // Routes are told of from the start, towns and cities once they have been come to; every open area of an island has a block
        foreach (var block in map.Blocks)
            Assert.Equal(block.Kind == RegionBlockKind.Town, block.Area.Flag != null);
        foreach (string name in Islands)
            foreach (var area in BuiltMaps.Value[name].Areas.Where(a => a.Open))
                Assert.Contains(map.Areas, p => p.Key == area.Key);
    }

    [Fact]
    public void EveryFlagATownWaitsForIsSetByItsFirstArrival()
    {
        var map = Town;
        var spawns = SpawnLocations.In(RegionDatabase.Alola);
        foreach (var place in map.Areas.Where(a => a.Flag != null))
        {
            bool spawnArrival = spawns.Any(s => s.UnlockedOnArrival && s.ArrivalFlag == place.Flag);
            Assert.True(spawnArrival || place.Arrival.Any(where => map.ArrivalFlags(where).Contains(place.Flag!)), $"nothing sets {place.Flag} ({place.Key})");
        }
    }

    [Fact]
    public void FlysPlacesAreTheIslandsPokemonCentersOnTheirOwnCells()
    {
        var map = Town;
        var spawns = SpawnLocations.In(RegionDatabase.Alola);
        // The player's house and every island's Pokémon Centers; Aether Paradise is a map of its own, off the islands' grid
        Assert.Equal(new[] { 1, 2, 3, 4, 5, 6, 20, 21, 22, 30, 31, 32 }, map.FlySpots.Select(f => f.Spawn));
        foreach (var spot in map.FlySpots)
        {
            var town = spawns.Single(s => s.Id == spot.Spawn);
            Assert.True(map.IsOverworld(town.Map), $"spawn {town.Id} on {town.Map}");
            Assert.Contains(map.CellOn(town.Map, town.X, town.Y), spot.Cells);
        }
        // Each island has a place to land
        foreach (string island in Islands)
            Assert.Contains(map.FlySpots, f => spawns.Single(s => s.Id == f.Spawn).Map == island);
    }

    [Fact]
    public void AChampionOfKalosWhoHasEnteredAlolaFliesToAnotherIsland()
    {
        var kalos = RegionDatabase.Get(RegionDatabase.Kalos)!;
        var story = new StoryState();
        var dexes = new List<string> { kalos.Dex!, Alola.Dex! };
        story.Set(kalos.StoryCompleteFlag);
        Assert.True(Crossing.CanFly(kalos, Alola, story, dexes));

        // Standing on Melemele, Malie City on Ula'ula is chosen once it has been come to
        var malie = SpawnLocations.In(RegionDatabase.Alola).Single(s => s.Id == 30);
        story.Set(SpawnLocations.FlyFlag(malie));
        var state = new TownMapState(Town, story, Town.CellOn("Melemele", 238, 158));
        Assert.Equal("Route 1", state.HereName);
        var screen = new TownMapScreen();
        screen.Open(state, TownMapMode.Fly, flier: new Pokemon(PokemonDatabase.Get("Charizard")!, 40));
        var cell = Town.FlySpots.Single(f => f.Spawn == malie.Id).Cells[0];
        for (int i = 0; i < 80 && (screen.CursorX, screen.CursorY) != cell; i++)
            screen.Move(Math.Sign(cell.X - screen.CursorX), Math.Sign(cell.Y - screen.CursorY));
        Assert.Equal(cell, (screen.CursorX, screen.CursorY));
        screen.Confirm();
        var chosen = screen.TakeChoice();
        Assert.Equal(malie.Id, chosen!.Id);
        Assert.Equal("Ulaula", chosen.Map);

        // Seafolk Village, not yet come to, is refused
        screen.Open(state, TownMapMode.Fly, flier: null);
        var seafolk = Town.FlySpots.Single(f => f.Spawn == 32).Cells[0];
        Assert.False(state.CanFlyTo(seafolk.X, seafolk.Y));
    }

    [Fact]
    public void TheTownMapOpensWhereverTheGameStandsInAlolaAndEveryRoomHasACell()
    {
        foreach (string name in Alola.Maps)
            Assert.Same(Town, RegionMap.ForMap(name));
        var places = Town.PlacesOf(Alola.Maps.Select(MapNamed));
        Assert.All(Islands, n => Assert.False(places.ContainsKey(n)));
        // A room of Akala is shown on Akala, one of Poni on Poni, a cave at its mouth
        Assert.Equal(Town.CellOn("Akala", 129, 309), places["HeaheaCity_PokemonCenter_1F"]);
        Assert.Equal("Seafolk Village", Town.BlockAt(places["SeafolkVillage_PokemonCenter_1F"].X, places["SeafolkVillage_PokemonCenter_1F"].Y)!.Name);
        Assert.Equal(Town.CellOn("Poni", 104, 153), places["VastPoniCanyon"]);
        Assert.Equal(Town.CellOn("Ulaula", 290, 141), places["MountLanakila"]);
        // Aether Paradise, reached by ferry alone, is shown at Heahea City
        Assert.Equal("Heahea City", Town.BlockAt(places["AetherParadise"].X, places["AetherParadise"].Y)!.Name);
        Assert.All(places.Values, c => Assert.True(Town.InView(c.X, c.Y)));
        var without = Alola.Maps.Where(n => !Islands.Contains(n) && !places.ContainsKey(n)).ToList();
        Assert.True(without.Count == 0, "No cell for: " + string.Join(", ", without));
    }

    [Fact]
    public void ThePicturePaintsAllFourIslands()
    {
        var worlds = Town.Overworlds.Select(o => (BuiltMaps.Value[o.Key], o.Value.X, o.Value.Y)).ToList();
        var habitats = Habitats.Of(Alola)!;
        var picture = RegionMapArt.Paint(worlds, Town, (x, y) => habitats.LookAt(x, y) != ' ', new HashSet<string>());
        Assert.Equal((Town.View.Width * RegionMapArt.CellTexels, Town.View.Height * RegionMapArt.CellTexels), (picture.Width, picture.Height));
        // The middle of a town's cell on each island is land, not sea
        foreach (var (key, cell) in new[] { ("iki_town", (7, 3)), ("heahea_city", (21, 21)), ("malie_city", (18, 10)), ("seafolk_village", (2, 24)) })
        {
            int land = 0;
            for (int y = 0; y < RegionMapArt.CellTexels; y++)
                for (int x = 0; x < RegionMapArt.CellTexels; x++)
                {
                    var c = picture.Get(cell.Item1 * RegionMapArt.CellTexels + x, cell.Item2 * RegionMapArt.CellTexels + y);
                    if (!(c.B > c.R + 40 && c.B > c.G)) land++;
                }
            Assert.True(land > RegionMapArt.CellTexels * RegionMapArt.CellTexels / 3, $"{key}: {land} texels of land");
        }
        // And the sea between the islands is sea
        var between = picture.Get(14 * RegionMapArt.CellTexels + 16, 9 * RegionMapArt.CellTexels + 16);
        Assert.True(between.B > between.R + 40, $"the sea between Melemele and Akala is {between}");
    }

    [Fact]
    public void ThePokedexsAreaPageLightsEveryIslandsPlaces()
    {
        var habitats = Habitats.Of(Alola)!;
        Assert.True(habitats.Width <= Town.Columns && habitats.Height <= Town.Rows, $"{habitats.Width} by {habitats.Height}");
        var lit = new HashSet<(int, int)>();
        foreach (var species in PokemonDatabase.GetAll())
            foreach (var place in habitats.Of(species.Name))
                foreach (var cell in place.Cells)
                {
                    Assert.True(Town.InView(cell.X, cell.Y), $"{species.Name} at {place.Name}: {cell.X},{cell.Y} is off the map");
                    lit.Add(cell);
                }
        // Places of every island are lit where their blocks are, and the caves at their mouths
        foreach (string island in Islands)
            Assert.Contains(lit, c => IslandAt(c.Item1, c.Item2) is { } at && at.Island.Name == island);
        Assert.Contains(habitats.Of("Jangmo-o"), h => h.Cells.Contains(Town.CellOn("Poni", 104, 153)));
    }
}
