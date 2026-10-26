using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// The post-game of Hoenn, part e (plan 21 · H12): the Weather Institute's reports, the abnormal weather they track and the
/// two caves it opens, Terra Cave behind Routes 114 to 118 with Groudon and the Marine Cave under the sea floors of Routes
/// 105 to 129 with Kyogre (docs/mechanics/rulings.md, "The Weather Institute's reports, the Marine Cave and Terra Cave").
/// Each scene starts as the game starts it, on maps of this class's own.
/// </summary>
public class HoennWeatherCavesTests
{
    private static readonly ScriptLibrary Scripts = ScriptLibrary.Default;
    private static readonly Lazy<World> Hoenn = new(() => World.LoadAll().Single(w => w.Index.Region == RegionDatabase.Hoenn));

    /// <summary>
    /// Emerald's sixteen places (VAR_ABNORMAL_WEATHER_LOCATION 1 to 16): the flag of ours that opens it, the map and tile of
    /// its way into the cave, the tile the way back out puts the player on, and a stretch of the dark water laid over it
    /// (the Marine Cave's only).
    /// </summary>
    private static readonly (int Place, string Flag, string Map, int X, int Y, int BackX, int BackY)[] Places =
    {
        (1, "ROUTE_114_NORTH", "Hoenn", 47, 6, 47, 7), (2, "ROUTE_114_SOUTH", "Hoenn", 46, 48, 46, 49),
        (3, "ROUTE_115_WEST", "Hoenn", 21, 48, 21, 49), (4, "ROUTE_115_EAST", "Hoenn", 36, 52, 36, 53),
        (5, "ROUTE_116_NORTH", "Hoenn", 99, 135, 99, 136), (6, "ROUTE_116_SOUTH", "Hoenn", 119, 128, 119, 129),
        (7, "ROUTE_118_EAST", "Hoenn", 282, 146, 282, 147), (8, "ROUTE_118_WEST", "Hoenn", 249, 146, 249, 147),
        (9, "ROUTE_105_NORTH", "Underwater_Route105", 13, 266, 13, 267), (10, "ROUTE_105_SOUTH", "Underwater_Route105", 17, 328, 17, 329),
        (11, "ROUTE_125_WEST", "Underwater_Route125", 662, 70, 662, 71), (12, "ROUTE_125_EAST", "Underwater_Route125", 685, 90, 685, 91),
        (13, "ROUTE_127_NORTH", "HoennUnderwater", 697, 145, 697, 146), (14, "ROUTE_127_SOUTH", "HoennUnderwater", 707, 178, 707, 179),
        (15, "ROUTE_129_WEST", "Underwater_Route129", 666, 263, 666, 264), (16, "ROUTE_129_EAST", "Underwater_Route129", 672, 281, 672, 282)
    };

    private static string FlagOf(string place) => "FLAG_HOENN_ABNORMAL_WEATHER_" + place;

    private sealed class Game
    {
        private readonly Dictionary<string, Map> maps = new(StringComparer.OrdinalIgnoreCase);
        public StoryState Story { get; } = new();
        public Party Party { get; } = new();
        public Inventory Bag { get; } = new();
        public Map Map { get; private set; } = null!;
        public (int X, int Y) Tile { get; set; }
        public BattleOutcome Fight { get; set; } = BattleOutcome.Won;
        public HeadlessScriptHost Last { get; private set; } = null!;
        public Queue<int> Rolls { get; } = new();

        public Game()
        {
            foreach (var m in Hoenn.Value.BuildMaps()) maps[m.Name] = m;
            StoryMigration.BeginNewGame(Story, Scripts);
            Party.Add(new Pokemon(PokemonDatabase.Get("Swampert")!, 70, new Random(1)));
            Story.SetBadges("Hoenn", 0xFF);
            foreach (var map in maps.Values.Distinct()) Settle(map);
        }

        public Map MapOf(string name) => maps[name];

        public void Arrive(string name, int x, int y)
        {
            Map = maps[name];
            Map.ForgetForced();
            // (coming to another place forgets what lasted only there, as GameEngine.ChangePlace does)
            Story.ClearLocal();
            Tile = (x, y);
            if (Scripts.In(Map.ScriptFileAt(x, y), ScriptLibrary.OnTransition) is { } transition) Play(transition);
            Settle(Map);
            if (Scripts.In(Map.ScriptFileAt(x, y), ScriptLibrary.OnEnter) is { } script) Play(script);
        }

        private void Settle(Map map)
        {
            map.ApplySwappedPlaces(Story.Var, Story.Has);
            map.ApplyPresence(Story.Has);
            map.ApplyDoors(Story.Has);
        }

        public NPC? Present(string key) => Map.NPCs.FirstOrDefault(n => n.Key == key);

        public HeadlessScriptHost Talk(string key)
        {
            var npc = Map.NPCs.First(n => n.Key == key);
            Tile = (npc.GridX, npc.GridY + 1);
            return Play(Scripts.Find(FieldScripts.For(npc)!, npc.ScriptFile ?? Map.ScriptFileAt(npc.GridX, npc.GridY))!, npc);
        }

        /// <summary>A script of the place the player stands in, by its name (a trigger's), or of the common file.</summary>
        public HeadlessScriptHost Run(string name) => Play(Scripts.Find(name, Map?.ScriptFileAt(Tile.X, Tile.Y))!);

        /// <summary>Lays the weather over a place, as the scientist's draw does.</summary>
        public void Lay(int place)
        {
            Story.SetVar("VAR_ABNORMAL_WEATHER_LOCATION", place);
            Play(Scripts.Find("HoennAbnormalWeather", null)!);
        }

        public HeadlessScriptHost Play(Script script, NPC? subject = null)
        {
            var host = new HeadlessScriptHost(Story, Party, Bag)
            {
                Map = Map,
                PlayerTile = Tile,
                MapNamed = name => maps.GetValueOrDefault(name),
                Fight = _ => Fight,
                NeedsPokemon = true
            };
            foreach (int r in Rolls) host.Rolls.Enqueue(r);
            Rolls.Clear();
            var runner = new ScriptRunner(Scripts, host);
            runner.Start(script, subject);
            runner.RunToEnd();
            Assert.Empty(host.Problems);
            foreach (var map in maps.Values.Distinct()) Settle(map);
            if (host.Map != null) Map = host.Map;
            Tile = host.PlayerTile;
            return Last = host;
        }
    }

    [Fact]
    public void TheCavesAndTheSeaFloorsTheyAreReachedFromAreOpenInHoenn()
    {
        foreach (string name in new[] { "Underwater_Route105", "Underwater_Route125", "Underwater_Route129", "Underwater_MarineCave",
                     "MarineCave_Entrance", "MarineCave_End", "TerraCave_Entrance", "TerraCave_End" })
            Assert.Contains(name, RegionDatabase.Get(RegionDatabase.Hoenn)!.Maps);
        foreach (string key in new[] { "underwater_route_105", "underwater_route_125", "underwater_route_129", "underwater_marine_cave",
                     "marine_cave_entrance", "marine_cave_end", "terra_cave_entrance", "terra_cave_end" })
            Assert.Contains(key, Hoenn.Value.Index.Areas);
        var game = new Game();
        // Inside, each cave's mouth leads on to its inner room and back; the Marine Cave's pool dives down to the pool under
        // the sea, which comes up into it
        Assert.Equal("TerraCave_End", game.MapOf("TerraCave_Entrance").GetWarpAt(14, 1)!.TargetMap);
        Assert.Equal("TerraCave_Entrance", game.MapOf("TerraCave_End").GetWarpAt(5, 4)!.TargetMap);
        Assert.Equal("MarineCave_End", game.MapOf("MarineCave_Entrance").GetWarpAt(14, 1)!.TargetMap);
        Assert.Equal("MarineCave_Entrance", game.MapOf("MarineCave_End").GetWarpAt(20, 4)!.TargetMap);
        Assert.Equal("MarineCave_Entrance", game.MapOf("Underwater_MarineCave").EmergeAt(9, 6));
        Assert.Equal("Underwater_MarineCave", game.MapOf("MarineCave_Entrance").DiveAt(10, 10));
    }

    [Fact]
    public void EveryWayInIsShutUntilItsPlaceIsLaidAndOnlyThatOneOpens()
    {
        var game = new Game();
        foreach (var p in Places)
            Assert.True(game.MapOf(p.Map).IsSolid(p.X, p.Y), $"{p.Flag} is open before any report");
        foreach (var laid in Places)
        {
            game.Lay(laid.Place);
            foreach (var p in Places)
            {
                bool open = p.Place == laid.Place;
                Assert.Equal(open, game.Story.Has(FlagOf(p.Flag)));
                Assert.Equal(!open, game.MapOf(p.Map).IsSolid(p.X, p.Y));
            }
            // The way in leads into the cave, and the way back out of it onto open ground beside the way in
            var way = game.MapOf(laid.Map).GetWarpAt(laid.X, laid.Y)!;
            Assert.Equal(laid.Place <= 8 ? "TerraCave_Entrance" : "Underwater_MarineCave", way.TargetMap);
            Assert.False(game.MapOf(laid.Map).IsSolid(laid.BackX, laid.BackY), $"{laid.Flag}'s way back out lands on rock");
        }
        // Over, every way is shut again
        game.Play(Scripts.Find("HoennAbnormalWeatherEnd", null)!);
        foreach (var p in Places) Assert.True(game.MapOf(p.Map).IsSolid(p.X, p.Y));
    }

    [Fact]
    public void TheWeatherLiesOverTheRouteAndTheMarineCavesDarkWaterOnlyWhileItsPlaceIsLaid()
    {
        var game = new Game();
        var hoenn = game.MapOf("Hoenn");
        // Route 114's southern mouth: a drought over the route
        Assert.NotEqual(FieldWeather.Drought, hoenn.WeatherAt(46, 49));
        game.Lay(2);
        Assert.Equal(FieldWeather.Drought, hoenn.WeatherAt(46, 49));
        // Route 125's western place: heavy rain over the route, and Emerald's deep water laid two tiles by two, diving onto
        // open floor
        game.Lay(11);
        Assert.NotEqual(FieldWeather.Drought, hoenn.WeatherAt(46, 49));
        Assert.Equal(FieldWeather.HeavyRain, hoenn.WeatherAt(648, 77));
        Assert.Equal("Underwater_Route125", hoenn.DiveAt(648, 77));
        // Every place of the sea has its dark water only while laid, on sea to surf on over open floor
        foreach (var p in Places.Where(p => p.Place >= 9))
        {
            game.Lay(p.Place);
            var (x, y) = DarkWater[p.Place];
            var floor = game.MapOf(p.Map);
            for (int dy = 0; dy < 2; dy++)
                for (int dx = 0; dx < 2; dx++)
                {
                    Assert.Equal(TileBehavior.DiveWater, hoenn.BehaviourAt(x + dx, y + dy));
                    Assert.True(FieldMovement.SurfacesOnto(hoenn, x + dx, y + dy),
                        $"{p.Flag}'s dark water at {x + dx},{y + dy} is no sea to surf on: it was {hoenn.FlaggedDarkWater[(x + dx, y + dy)].Was}, solid {hoenn.IsSolid(x + dx, y + dy)}");
                    Assert.False(floor.IsSolid(x + dx, y + dy), $"{p.Flag}'s dark water at {x + dx},{y + dy} dives onto rock");
                }
            foreach (var q in Places.Where(q => q.Place >= 9 && q.Place != p.Place))
                if (hoenn.FlaggedDarkWater[DarkWater[q.Place]].Was != TileBehavior.DiveWater)
                    Assert.NotEqual(TileBehavior.DiveWater, hoenn.BehaviourAt(DarkWater[q.Place].X, DarkWater[q.Place].Y));
        }
    }

    /// <summary>The top-left tile of each place's deep water on the sea above (Emerald's abnormal_weather.inc).</summary>
    private static readonly Dictionary<int, (int X, int Y)> DarkWater = new()
    {
        [9] = (10, 291), [10] = (20, 316), [11] = (648, 77), [12] = (693, 79),
        [13] = (697, 150), [14] = (701, 171), [15] = (656, 275), [16] = (682, 280)
    };

    [Fact]
    public void TheWayBackOutOfEachCaveLeadsToTheRouteTheWeatherIsOver()
    {
        foreach (var p in Places)
        {
            var game = new Game();
            game.Lay(p.Place);
            if (p.Place <= 8) game.Arrive("TerraCave_Entrance", 8, 17);
            else game.Arrive("Underwater_MarineCave", 9, 7);
            game.Tile = p.Place <= 8 ? (8, 18) : (9, 8);
            game.Run("Out");
            Assert.Equal((p.Map, p.BackX, p.BackY), (game.Map.Name, game.Tile.X, game.Tile.Y));
        }
    }

    [Fact]
    public void TheScientistReportsTheWeatherOnlyOnceThePlayerIsChampion()
    {
        var game = new Game();
        game.Story.SetVar("VAR_WEATHER_INSTITUTE_STATE", 1);
        game.Story.Set("FLAG_RECEIVED_CASTFORM");
        game.Arrive("Route119_WeatherInstitute_2F", 5, 7);
        game.Talk("weather_institute_2f_scientist");
        Assert.Equal(0, game.Story.Var("VAR_ABNORMAL_WEATHER_LOCATION"));
        Assert.DoesNotContain(Places, p => game.Story.Has(FlagOf(p.Flag)));

        // A Champion: a coin for the kind (1, the sea) and a die for the place (2: Route 125's west)
        game.Story.Set("HoennHallOfFame");
        game.Rolls.Enqueue(1);
        game.Rolls.Enqueue(2);
        var said = game.Talk("weather_institute_2f_scientist");
        Assert.Equal(11, game.Story.Var("VAR_ABNORMAL_WEATHER_LOCATION"));
        Assert.True(game.Story.Has(FlagOf("ROUTE_125_WEST")));
        Assert.Contains(said.Transcript, l => l.Text.Contains("Route 125"));
        // Asked again on the same visit, it is the same place
        game.Rolls.Enqueue(0);
        game.Rolls.Enqueue(5);
        game.Talk("weather_institute_2f_scientist");
        Assert.Equal(11, game.Story.Var("VAR_ABNORMAL_WEATHER_LOCATION"));
        // Come again, a new draw: the coin's 0 is a drought, the die's 5 Route 116's south
        game.Arrive("Route119_WeatherInstitute_2F", 5, 7);
        game.Rolls.Enqueue(0);
        game.Rolls.Enqueue(5);
        said = game.Talk("weather_institute_2f_scientist");
        Assert.Equal(6, game.Story.Var("VAR_ABNORMAL_WEATHER_LOCATION"));
        Assert.True(game.Story.Has(FlagOf("ROUTE_116_SOUTH")));
        Assert.False(game.Story.Has(FlagOf("ROUTE_125_WEST")));
        Assert.Contains(said.Transcript, l => l.Text.Contains("Route 116"));
    }

    [Fact]
    public void KyogreRunFromIsBackAndCaughtIsGoneWithTheRainAndTheNextReportIsADrought()
    {
        var game = new Game();
        game.Story.Set("HoennHallOfFame");
        game.Lay(9);
        game.Arrive("MarineCave_End", 9, 27);
        Assert.NotNull(game.Present("marine_cave_kyogre"));
        Assert.Equal(1, game.Story.Var("VAR_TEMP_1"));
        game.Tile = (9, 26);
        game.Fight = BattleOutcome.Fled;
        game.Run("Kyogre");
        Assert.Contains("wildbattle Kyogre 70 Fled", game.Last.Log);
        Assert.False(game.Story.Has("FLAG_DEFEATED_KYOGRE"));
        Assert.True(game.Story.Has(FlagOf("ROUTE_105_NORTH")));
        Assert.Null(game.Present("marine_cave_kyogre"));
        game.Arrive("MarineCave_End", 9, 27);
        Assert.NotNull(game.Present("marine_cave_kyogre"));

        game.Tile = (9, 26);
        game.Fight = BattleOutcome.Caught;
        game.Run("Kyogre");
        Assert.True(game.Story.Has("FLAG_DEFEATED_KYOGRE"));
        Assert.DoesNotContain(Places, p => game.Story.Has(FlagOf(p.Flag)));
        game.Arrive("MarineCave_End", 9, 27);
        Assert.Null(game.Present("marine_cave_kyogre"));
        Assert.Equal(0, game.Story.Var("VAR_TEMP_1"));
        // and the way out still leads back to Route 105's floor, the rain over or not
        game.Arrive("Underwater_MarineCave", 9, 7);
        game.Tile = (9, 8);
        game.Run("Out");
        Assert.Equal(("Underwater_Route105", 13, 267), (game.Map.Name, game.Tile.X, game.Tile.Y));

        // Whatever the coin, the next report is a drought
        game.Story.SetVar("VAR_WEATHER_INSTITUTE_STATE", 1);
        game.Story.Set("FLAG_RECEIVED_CASTFORM");
        game.Arrive("Route119_WeatherInstitute_2F", 5, 7);
        game.Rolls.Enqueue(7);
        game.Talk("weather_institute_2f_scientist");
        Assert.Equal(8, game.Story.Var("VAR_ABNORMAL_WEATHER_LOCATION"));
    }

    [Fact]
    public void GroudonBeatenIsGoneAndWithBothBeatenTheWeatherIsOrdinaryAgain()
    {
        var game = new Game();
        game.Story.Set("HoennHallOfFame");
        game.Lay(7);
        game.Arrive("TerraCave_End", 17, 27);
        Assert.NotNull(game.Present("terra_cave_groudon"));
        game.Tile = (17, 26);
        game.Fight = BattleOutcome.Won;
        game.Run("Groudon");
        Assert.Contains("wildbattle Groudon 70 Won", game.Last.Log);
        Assert.True(game.Story.Has("FLAG_DEFEATED_GROUDON"));
        Assert.False(game.Story.Has(FlagOf("ROUTE_118_EAST")));
        game.Arrive("TerraCave_End", 17, 27);
        Assert.Null(game.Present("terra_cave_groudon"));
        // A step on the trigger's tile now does nothing
        game.Tile = (17, 26);
        Assert.DoesNotContain(game.Run("Groudon").Log, l => l.StartsWith("wildbattle"));

        game.Story.Set("FLAG_DEFEATED_KYOGRE");
        game.Story.SetVar("VAR_WEATHER_INSTITUTE_STATE", 1);
        game.Story.Set("FLAG_RECEIVED_CASTFORM");
        game.Arrive("Route119_WeatherInstitute_2F", 5, 7);
        var said = game.Talk("weather_institute_2f_scientist");
        Assert.Contains(said.Transcript, l => l.Text.Contains("stopped"));
        Assert.DoesNotContain(Places, p => game.Story.Has(FlagOf(p.Flag)));
    }

    [Fact]
    public void KyogreAndGroudonCanBeHadInHoenn()
    {
        Assert.True(SpeciesSources.Hoenn.Has("Kyogre"));
        Assert.True(SpeciesSources.Hoenn.Has("Groudon"));
        Assert.Contains("marine_cave_end", SpeciesSources.Hoenn.Ways["Kyogre"]);
        Assert.Contains("terra_cave_end", SpeciesSources.Hoenn.Ways["Groudon"]);
    }
}
