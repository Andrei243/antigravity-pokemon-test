using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumTests;

/// <summary>
/// What a season changes in Unova beside its look (plan 22 · U4; <c>docs/mechanics/rulings.md</c>, "Seasons in Unova"):
/// the season's chunks laid in place of spring's and put back, the season's tables and the dark grass's met, a pair
/// out of the dark grass, and Deerling's and Sawsbuck's coats, which turn with the season in Unova alone and are kept,
/// evolved and saved. Nothing of it reaches another region.
/// </summary>
[Collection("Shared clocks and dice")]
public class SeasonLayoutTests
{
    // Unova's maps of this class's own: the tests lay seasons on them
    private static readonly Lazy<World> LoadedWorld = new(() => World.LoadAll().Single(w => w.Index.Region == "Unova"));
    private static Map Built() => LoadedWorld.Value.BuildMaps().Single(m => m.Name == "Unova");

    private static IEnumerable<(int X, int Y)> TilesOf(Map.SwappedPlace place) =>
        place.Cells.SelectMany(c => Enumerable.Range(0, Map.ChunkTiles * Map.ChunkTiles)
            .Select(i => (c.X * Map.ChunkTiles + i % Map.ChunkTiles, c.Y * Map.ChunkTiles + i / Map.ChunkTiles)));

    private static string Ground(Map map, Map.SwappedPlace place) =>
        string.Concat(TilesOf(place).Select(t => $"{(map.IsSolid(t.X, t.Y) ? '#' : '.')}{(int)map.BehaviourAt(t.X, t.Y)}:{(int)map.GetGroundTile(t.X, t.Y)},"));

    [Fact]
    public void Route20sAutumnChunksAreLaidInAutumnAndSpringsComeBack()
    {
        var unova = Built();
        // Route 20's four cells change in autumn (U2, part b), the ranch's gate standing in one of them in both
        var autumn = Assert.Single(unova.SwappedPlaces, p => p.Season == Season.Autumn);
        Assert.Equal(4, autumn.Cells.Count);
        Assert.Null(unova.SeasonLaid);
        string spring = Ground(unova, autumn);
        var springSolid = TilesOf(autumn).Where(t => unova.IsSolid(t.X, t.Y)).ToHashSet();

        Assert.Single(unova.ApplySeason(Season.Autumn));
        Assert.Equal(Season.Autumn, unova.SeasonLaid);
        Assert.NotEqual(spring, Ground(unova, autumn));
        // Autumn's layout opens ground spring's trees shut
        Assert.Contains(springSolid, t => !unova.IsSolid(t.X, t.Y) && unova.IsWalkable(t.X, t.Y));
        // Laid twice is laid once
        Assert.Empty(unova.ApplySeason(Season.Autumn));
        // The story never moves a season's chunks
        Assert.Empty(unova.ApplySwappedPlaces(_ => 0, _ => true));
        Assert.Equal(Season.Autumn, unova.SeasonLaid);

        // Winter has no chunks of its own here yet, so it is spring's ground; and spring is spring's to the tile
        unova.ApplySeason(Season.Winter);
        Assert.Equal(spring, Ground(unova, autumn));
        unova.ApplySeason(Season.Autumn);
        unova.ApplySeason(null);
        Assert.Equal(spring, Ground(unova, autumn));
        unova.ApplySeason(Season.Autumn);
        unova.ApplySeason(Season.Spring);
        Assert.Equal(spring, Ground(unova, autumn));
    }

    [Fact]
    public void NoOtherRegionHasASeasonsChunks()
    {
        foreach (var world in World.LoadAll().Where(w => w.Index.Region != "Unova"))
            foreach (var entry in world.Index.Maps)
                Assert.Empty(world.SeasonalSwaps(entry));
        // and Unova's are only where the importer wrote the season's chunk
        var unova = LoadedWorld.Value;
        foreach (var entry in unova.Index.Maps)
            foreach (var (_, swap) in unova.SeasonalSwaps(entry))
                Assert.All(swap.Cells, c => Assert.NotNull(unova.Chunk(c.Chunk)));
    }

    // A tile of Route 20 of the kind asked for
    private static (int X, int Y) OnRoute20(Map map, Func<int, int, bool> kind)
    {
        for (int y = 0; y < map.Height; y++)
            for (int x = 0; x < map.Width; x++)
                if (map.AreaAt(x, y)?.Key == "unova_route_20" && kind(x, y)) return (x, y);
        throw new InvalidOperationException("Route 20 has no such tile.");
    }

    [Fact]
    public void ASeasonsTablesAreTheOnesMetAndTheDarkGrassHasItsOwn()
    {
        var unova = Built();
        var (gx, gy) = OnRoute20(unova, (x, y) => unova.IsTallGrass(x, y) && !unova.IsDarkGrass(x, y));
        var route20 = unova.AreaAt(gx, gy)!;
        Assert.True(route20.SeasonTables.ContainsKey(Season.Summer));
        var spring = route20.TablesFor(null);
        Assert.Same(route20.WildEncounters, spring.Land);
        Assert.Equal(spring.LandRate, route20.TablesFor(Season.Spring).LandRate);

        foreach (var season in new[] { Season.Summer, Season.Autumn, Season.Winter })
        {
            var own = route20.TablesFor(season);
            var (table, rate) = unova.WildAt(gx, gy, moment: new EncounterMoment { Season = season });
            Assert.Equal(own.LandRate, rate);
            Assert.Equal(own.Land.Select(e => (e.SpeciesName, e.MinLevel)), table.Select(e => (e.SpeciesName, e.MinLevel)));
        }
        // Without a season (every other region's moment) the area's own are met
        var (plain, plainRate) = unova.WildAt(gx, gy, moment: new EncounterMoment());
        Assert.Equal(spring.LandRate, plainRate);
        Assert.Equal(spring.Land.Select(e => e.SpeciesName), plain.Select(e => e.SpeciesName));
        // Summer's grass is met more often than spring's on Route 20 (Black 2's rates)
        Assert.NotEqual(route20.TablesFor(Season.Summer).LandRate, spring.LandRate);

        // The dark grass is met from its own table, in each season
        var (dx, dy) = OnRoute20(unova, unova.IsDarkGrass);
        Assert.True(TileBehaviors.HasEncounters(unova.BehaviourAt(dx, dy)));
        foreach (Season? season in new Season?[] { null, Season.Summer, Season.Winter })
        {
            var own = route20.TablesFor(season);
            var (table, rate) = unova.WildAt(dx, dy, moment: new EncounterMoment { Season = season });
            Assert.Equal(own.DarkGrassRate, rate);
            Assert.Contains(table, e => e.SpeciesName == "Venipede");
            Assert.Equal(own.DarkGrass.Select(e => e.SpeciesName), table.Select(e => e.SpeciesName));
        }
    }

    [Fact]
    public void APairComesOutOfTheDarkGrassAloneNowAndThen()
    {
        var unova = Built();
        var (dx, dy) = OnRoute20(unova, unova.IsDarkGrass);
        var (gx, gy) = OnRoute20(unova, (x, y) => unova.IsTallGrass(x, y) && !unova.IsDarkGrass(x, y));
        int pairs = 0;
        for (int seed = 0; seed < 1000; seed++)
            if (Map.DarkGrassPairs(unova, dx, dy, 2, new Random(seed))) pairs++;
        Assert.InRange(pairs, 50, 150);
        // Never to a player with one able to fight, never off the dark grass, and nothing is drawn there
        for (int seed = 0; seed < 100; seed++)
        {
            var drawn = new Random(seed);
            var untouched = new Random(seed);
            Assert.False(Map.DarkGrassPairs(unova, dx, dy, 1, drawn));
            Assert.False(Map.DarkGrassPairs(unova, gx, gy, 6, drawn));
            Assert.Equal(untouched.Next(), drawn.Next());
        }
    }

    [Fact]
    public void DeerlingAndSawsbuckWearTheSeason()
    {
        Assert.Null(SeasonalForms.FormFor("Deerling", Season.Spring));
        Assert.Equal("Deerling-Summer", SeasonalForms.FormFor("Deerling", Season.Summer));
        Assert.Equal("Sawsbuck-Winter", SeasonalForms.FormFor("Sawsbuck", Season.Winter));
        Assert.Null(SeasonalForms.FormFor("Bidoof", Season.Winter));

        var deerling = new Pokemon(PokemonDatabase.Get("Deerling")!, 20);
        foreach (var season in Enum.GetValues<Season>())
        {
            SeasonalForms.Wear(deerling, season);
            Assert.Equal(SeasonalForms.FormFor("Deerling", season), deerling.Form);
            Assert.False(SeasonalForms.Wear(deerling, season));
        }
        var bidoof = new Pokemon(PokemonDatabase.Get("Bidoof")!, 20);
        Assert.False(SeasonalForms.Wear(bidoof, Season.Autumn));
        Assert.Null(bidoof.Form);
    }

    [Fact]
    public void AnAutumnDeerlingEvolvesIntoAnAutumnSawsbuckAndIsSavedInItsCoat()
    {
        var deerling = new Pokemon(PokemonDatabase.Get("Deerling")!, 34);
        SeasonalForms.Wear(deerling, Season.Autumn);
        var evolution = Evolution.Find(deerling, EvolutionTrigger.LevelUp, new EvolutionContext());
        Assert.NotNull(evolution);
        Evolution.Evolve(deerling, evolution!, new EvolutionContext());
        Assert.Equal("Sawsbuck", deerling.Species.Name);
        Assert.Equal("Sawsbuck-Autumn", deerling.Form);

        var back = SavedPokemonData.FromPokemon(deerling).ToPokemon();
        Assert.Equal("Sawsbuck-Autumn", back.Form);
    }

    [Fact]
    public void ADeerlingOfTheTeamTurnsItsCoatWithUnovasSeasonAndKeepsItElsewhere()
    {
        var fixedSeason = GameClock.FixedSeason;
        var engine = new GameEngine(new GameSettings { Muted = true });
        try
        {
            engine.InitializeHeadless();
            engine.PeopleStayPut = true;
            engine.NewGameRegion = RegionDatabase.Unova;
            engine.StartNewGame();
            var game = engine.Drive;
            game.State = GameState.Overworld;
            var deerling = new Pokemon(PokemonDatabase.Get("Deerling")!, 20);
            game.Party.Add(deerling);

            // The options fix autumn: the coat and Route 20's layout at once
            engine.Settings.Season = Season.Autumn;
            engine.ApplySettings(window: false);
            Assert.Equal(Season.Autumn, engine.SeasonHere);
            Assert.Equal("Deerling-Autumn", deerling.Form);
            Assert.Equal(Season.Autumn, MapDatabase.Get("Unova").SeasonLaid);
            Assert.Equal(Season.Autumn, game.EncounterMomentNow().Season);

            // The calendar turns to winter behind a fade
            engine.Settings.Season = null;
            GameClock.FixedSeason = Season.Winter;
            game.TurnTheSeason();
            Assert.Equal(Season.Winter, game.FieldSeason);
            Assert.Equal("Deerling-Winter", deerling.Form);
            // (the winter's own layout is laid since plan 22 · U7: Route 7's drifts are chunks of winter's that spring's lack)
            Assert.Equal(Season.Winter, MapDatabase.Get("Unova").SeasonLaid);

            // In Sinnoh the coat stays what it is, and the moment has no season
            game.Place(MapDatabase.Get("Sinnoh"), 112, 880, Direction.Down);
            GameClock.FixedSeason = Season.Summer;
            game.TurnTheSeason();
            Assert.Null(engine.SeasonHere);
            Assert.Null(game.EncounterMomentNow().Season);
            Assert.Equal("Deerling-Winter", deerling.Form);

            // And a save keeps the coat
            var saved = game.BuildSave();
            Assert.Equal("Deerling-Winter", saved.Party.Single(p => p.SpeciesName == "Deerling").ToPokemon().Form);
        }
        finally
        {
            GameClock.FixedSeason = fixedSeason;
            engine.Settings.Season = null;
            MapDatabase.ApplySeason(null);
            engine.Close();
            Ruleset.Use(RulesPreset.Platinum);
        }
    }
}
