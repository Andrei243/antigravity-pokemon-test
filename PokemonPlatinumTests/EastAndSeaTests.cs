using System.Text.Json;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumTests;

/// <summary>
/// The east of Sinnoh and its sea as plan 01 · M7 opened them: Routes 210 and 215 to Celestic Town and Veilstone
/// City, Route 214 and Lake Valor, Route 213 and Pastoria City with the Great Marsh and its Safari Game, Route 212
/// with its calendar's weather and its puddles, Routes 218 to 221, Canalave City with its drawbridge and the ferry to
/// Iron Island and its lifts.
/// </summary>
public class EastAndSeaTests
{
    // Maps of this class's own, so nothing another test does to the database's maps shows here. Read only.
    private static readonly Lazy<World> LoadedWorld = new(() => World.LoadAll().Single(w => w.Index.Region == "Sinnoh"));
    private static readonly Lazy<Dictionary<string, Map>> BuiltMaps = new(() => LoadedWorld.Value.BuildMaps().ToDictionary(m => m.Name));

    private static World Sinnoh => LoadedWorld.Value;
    private static Map Overworld => BuiltMaps.Value["Sinnoh"];

    private static string? AreaKey(Map map, int x, int y) => map.AreaAt(x, y)?.Key;

    private static IEnumerable<(int X, int Y)> TilesOf(Map map, string area)
    {
        for (int y = 0; y < map.Height; y++)
            for (int x = 0; x < map.Width; x++)
                if (AreaKey(map, x, y) == area) yield return (x, y);
    }

    // ------------------------------------------------------------------ the weather calendar

    [Fact]
    public void TheCalendarHasADayOfALeapYearForEachOfItsFivePlaces()
    {
        var calendar = Sinnoh.Calendar;
        Assert.Equal(new[] { "Route212South", "Route213", "Route216", "AcuityLakefront", "SnowpointCity" }.OrderBy(p => p), calendar.Keys.OrderBy(p => p));
        Assert.All(calendar.Values, days => Assert.Equal(366, days.Length));

        // sYearlyWeather's first and last rows, and the 29th of February's
        Assert.Equal(FieldWeather.Rain, calendar["Route212South"][0]);
        Assert.Equal(FieldWeather.Clear, calendar["Route213"][0]);
        Assert.Equal(FieldWeather.HeavyRain, calendar["Route212South"][59]);
        Assert.Equal(FieldWeather.Thunderstorm, calendar["Route212South"][365]);
        Assert.Equal(FieldWeather.Rain, calendar["Route213"][365]);
    }

    [Theory]
    [InlineData(2028, 1, 1, 0)]
    [InlineData(2028, 2, 29, 59)]   // a leap year's own day
    [InlineData(2028, 3, 1, 60)]
    [InlineData(2027, 2, 28, 58)]
    [InlineData(2027, 3, 1, 60)]    // any other year skips the 29th's row, as FieldSystem_GetWeather does
    [InlineData(2026, 6, 1, 152)]
    [InlineData(2026, 12, 31, 365)]
    public void ADayIsItsRowOfALeapYear(int year, int month, int day, int row)
    {
        Assert.Equal(row, Weathers.CalendarDay(new DateTime(year, month, day)));
    }

    [Fact]
    public void Route212sSouthAndRoute213FollowTheCalendar()
    {
        var map = Overworld;
        MapArea AreaOf(string key) => TilesOf(map, key).Select(t => map.AreaAt(t.X, t.Y)!).First();
        var south = AreaOf("route_212_south");
        var lake = AreaOf("route_213");
        Assert.NotNull(south.Calendar);
        Assert.Equal(FieldWeather.Rain, south.WeatherOn(new DateTime(2027, 1, 1)));
        Assert.Equal(FieldWeather.HeavyRain, south.WeatherOn(new DateTime(2028, 2, 29)));
        Assert.Equal(FieldWeather.Thunderstorm, south.WeatherOn(new DateTime(2026, 12, 31)));
        Assert.NotNull(lake.Calendar);
        Assert.Equal(FieldWeather.Clear, lake.WeatherOn(new DateTime(2027, 1, 1)));
        Assert.Equal(FieldWeather.Rain, lake.WeatherOn(new DateTime(2026, 12, 31)));

        // The rest keep their header's weather whatever the day: the fog of Route 210's north, Route 215's rain
        foreach (var (area, weather) in new[] { ("route_210_north", FieldWeather.Fog), ("route_215", FieldWeather.Rain), ("route_212_north", FieldWeather.Clear) })
        {
            var here = AreaOf(area);
            Assert.Null(here.Calendar);
            Assert.Equal(weather, here.WeatherOn(new DateTime(2027, 1, 1)));
            Assert.Equal(weather, here.WeatherOn(new DateTime(2026, 12, 31)));
        }
    }

    // ------------------------------------------------------------------ puddles

    [Fact]
    public void Route212HasPuddlesThatAreWalkedThrough()
    {
        var map = Overworld;
        var puddles = TilesOf(map, "route_212_south").Where(t => map.GetGroundTile(t.X, t.Y) == TileType.Puddle).ToList();
        Assert.True(puddles.Count > 20, $"only {puddles.Count} puddles on Route 212's south");
        Assert.All(puddles, t =>
        {
            Assert.True(map.BehaviourAt(t.X, t.Y) is TileBehavior.Puddle or TileBehavior.StillPuddle, $"({t.X},{t.Y}) is a puddle that behaves as {map.BehaviourAt(t.X, t.Y)}");
            Assert.True(map.IsWalkable(t.X, t.Y), $"the puddle at ({t.X},{t.Y}) can't be walked through");
        });

        // A puddle is told by its behaviour, whatever its texture, and a hand-made map can have them too
        Assert.Equal(TerrainCover.Puddle, TerrainCoverCodes.Parse('p'));
        var small = new Map(3, 1);
        small.SetGroundTile(1, 0, TileType.Puddle);
        Assert.Equal(TileBehavior.Puddle, small.BehaviourAt(1, 0));
    }

    [Fact]
    public void ABridgeOverAPathLeavesTheGroundUnderItWalkable()
    {
        // Route 210's south runs under a bridge for bicycles on its way to Celestic Town: walked under, not along
        Assert.Contains(Walked.Value[Overworld], t => AreaKey(Overworld, t.X, t.Y) == "celestic_town");
    }

    // ------------------------------------------------------------------ the gates and the roads between

    private static readonly Lazy<Dictionary<Map, HashSet<(int X, int Y)>>> Walked = new(() =>
        WorldWalk.From(name => BuiltMaps.Value.TryGetValue(name, out var map) ? map : MapDatabase.Get(name), RegionDatabase.Get(RegionDatabase.Sinnoh)!.Start!));

    [Theory]
    [InlineData("veilstone_city", "route_215")]
    [InlineData("route_215", "veilstone_city")]
    [InlineData("veilstone_city", "route_214")]
    [InlineData("route_214", "veilstone_city")]
    [InlineData("pastoria_city", "route_213")]
    [InlineData("route_213", "pastoria_city")]
    [InlineData("hearthome_city", "route_212_north")]
    [InlineData("route_212_north", "hearthome_city")]
    [InlineData("canalave_city", "route_218")]
    [InlineData("route_218", "canalave_city")]
    [InlineData("jubilife_city", "route_218")]
    [InlineData("route_218", "jubilife_city")]
    public void AGateHouseOfTheEastAndTheSeaIsWalkedThrough(string from, string to)
    {
        var map = Overworld;
        var ways = map.Warps.Where(w => w.TargetMap == "Sinnoh" && AreaKey(map, w.SourceX, w.SourceY) == from && AreaKey(map, w.TargetX, w.TargetY) == to).ToList();
        Assert.NotEmpty(ways);
        Assert.All(ways, w =>
        {
            Assert.True(map.IsWalkable(w.TargetX, w.TargetY), $"the way out at ({w.TargetX},{w.TargetY}) is blocked");
            Assert.Null(map.GetWarpAt(w.TargetX, w.TargetY));
        });
    }

    [Fact]
    public void EachNewTownHasItsCenterAndItsMart()
    {
        var map = Overworld;
        MapDatabase.Initialize();
        // Veilstone has its Department Store and Celestic no Mart (plan 01 · M11 builds the store)
        foreach (var (town, room) in new[]
                 {
                     ("veilstone_city", "VeilstonePokemonCenter"), ("pastoria_city", "PastoriaPokemonCenter"), ("pastoria_city", "PastoriaPokeMart"),
                     ("celestic_town", "CelesticPokemonCenter"), ("canalave_city", "CanalavePokemonCenter"), ("canalave_city", "CanalavePokeMart")
                 })
        {
            var door = Assert.Single(map.Warps, w => w.TargetMap == room);
            Assert.Equal(town, AreaKey(map, door.SourceX, door.SourceY));
            var inside = MapDatabase.Get(room);
            Assert.Equal(room, inside.Name);
            Assert.True(inside.IsWalkable(door.TargetX, door.TargetY));
            var exit = Assert.Single(inside.Warps);
            Assert.Equal(("Sinnoh", door.SourceX, door.SourceY + 1), (exit.TargetMap, exit.TargetX, exit.TargetY));
            Assert.Equal(RegionDatabase.Sinnoh, RegionDatabase.RegionOfMap(room)!.Id);
            Assert.Contains(inside.NPCs, n => room.EndsWith("Mart") ? n.IsPokeMartClerk : n.IsHealingNurse);
        }
    }

    // ------------------------------------------------------------------ the Great Marsh

    private const string Gate = "PastoriaCityObservatoryGate1F";

    private static Map GateRoom() => GameDataFiles.Load<MapFile>(Path.Combine(MapDatabase.Folder, Gate + ".json")).ToMap();

    [Fact]
    public void TheGreatMarshIsEnteredThroughPastoriasObservatoryAndLeftTheSameWay()
    {
        // The observatory is a room since plan 01 · M11, part b: Pastoria's door leads onto its mat, its back door into
        // the marsh, and the marsh's way out back to the tile inside that door
        var map = Overworld;
        var marsh = BuiltMaps.Value["GreatMarsh"];
        var gate = GateRoom();
        var into = map.Warps.Where(w => w.TargetMap == Gate).ToList();
        Assert.Equal(2, into.Count);
        Assert.All(into, w =>
        {
            Assert.Equal("pastoria_city", AreaKey(map, w.SourceX, w.SourceY));
            Assert.Equal((5, 11, Direction.Up), (w.TargetX, w.TargetY, w.TargetFacing));
        });
        Assert.DoesNotContain(map.Warps, w => w.TargetMap == "GreatMarsh");
        var back = Assert.Single(gate.Warps, w => w.TargetMap == "GreatMarsh");
        Assert.Equal((5, 2), (back.SourceX, back.SourceY));
        Assert.True(marsh.IsWalkable(back.TargetX, back.TargetY));
        Assert.Equal("great_marsh_6", AreaKey(marsh, back.TargetX, back.TargetY));
        var out_ = marsh.Warps.Where(w => w.TargetMap == Gate).ToList();
        Assert.Equal(2, out_.Count);
        Assert.All(out_, w =>
        {
            Assert.Equal((5, 3, Direction.Down), (w.TargetX, w.TargetY, w.TargetFacing));
            Assert.Equal(1, Math.Abs(w.SourceY - back.TargetY));
        });
        Assert.True(gate.IsWalkable(5, 3));
        var street = Assert.Single(gate.Warps, w => w.TargetMap == "Sinnoh");
        Assert.Equal("pastoria_city", AreaKey(map, street.TargetX, street.TargetY));
        Assert.True(map.IsWalkable(street.TargetX, street.TargetY));

        // The original's coordinate event lies before the attendant, across the way to the back door
        var trigger = Assert.Single(gate.Triggers);
        Assert.Equal((4, 6, 3, 1, "Counter"), (trigger.X, trigger.Y, trigger.Width, trigger.Depth, trigger.Script));
        Assert.Contains(gate.Everyone, n => n.Key == "cashier_m" && (n.GridX, n.GridY, n.Facing) == (2, 6, Direction.Right));

        // Saying no at the counter sends the player a step back, and costs nothing
        var host = PlayEntrance(answer: 1);
        Assert.False(host.Safari.Active);
        Assert.Equal((4, 7), host.PlayerTile);
        Assert.Equal(3000, host.Money);
        Assert.Empty(host.Problems);
    }

    /// <summary>Steps up to the observatory's counter, on the tile the door leads up to, and answers its question.</summary>
    private static HeadlessScriptHost PlayEntrance(int answer, int money = 3000, Action<HeadlessScriptHost>? before = null)
    {
        var host = new HeadlessScriptHost { Money = money, Map = GateRoom(), PlayerTile = (5, 6), PlayerFacing = Direction.Up };
        host.Answers.Enqueue(answer);
        before?.Invoke(host);
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find("Counter", Gate)!);
        runner.RunToEnd();
        return host;
    }

    [Fact]
    public void TheSafariGameCostsItsFeeForThirtyBallsAndFiveHundredSteps()
    {
        var host = PlayEntrance(answer: 0);
        Assert.True(host.Safari.Active);
        Assert.Equal((SafariGame.StartBalls, SafariGame.StartSteps), (host.Safari.Balls, host.Safari.Steps));
        Assert.Equal((30, 500), (SafariGame.StartBalls, SafariGame.StartSteps));
        Assert.Equal(3000 - SafariGame.Fee, host.Money);
        // Paid up, the player goes on through the door behind the counter, two tiles into the marsh
        Assert.Contains("warp GreatMarsh 68 116", host.Log);
        Assert.Empty(host.Problems);

        // Coming into the marsh while playing asks nothing
        int talks = host.Transcript.Count;
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find("OnEnter", "great_marsh_6")!);
        runner.RunToEnd();
        Assert.Equal(talks, host.Transcript.Count);
        Assert.Equal(3000 - SafariGame.Fee, host.Money);

        // Short of the fee: no game, and a step back from the counter
        var poor = PlayEntrance(answer: 0, money: 499);
        Assert.False(poor.Safari.Active);
        Assert.Equal(499, poor.Money);
        Assert.DoesNotContain(poor.Log, l => l.StartsWith("warp"));
        Assert.Equal((4, 7), poor.PlayerTile);

        // Nobody is in the marsh without a game: whoever is found there is shown back to the counter
        var stray = new HeadlessScriptHost();
        runner = new ScriptRunner(ScriptLibrary.Default, stray);
        runner.Start(ScriptLibrary.Default.Find("OnEnter", "great_marsh_6")!);
        runner.RunToEnd();
        Assert.Contains($"warp {Gate} 5 7", stray.Log);
    }

    [Fact]
    public void TheSafariGameEndsOnItsLastStep()
    {
        var game = new SafariGame();
        Assert.False(game.Step());   // no game, no steps counted
        game.Start();
        for (int i = 1; i < SafariGame.StartSteps; i++) Assert.False(game.Step());
        Assert.Equal(1, game.Steps);
        Assert.True(game.Step());
        Assert.Equal(0, game.Steps);
        Assert.False(game.Step());

        game.Start();
        game.Balls = 0;
        Assert.True(game.OutOfBalls);
        game.End();
        Assert.False(game.Active);
        Assert.False(game.OutOfBalls);

        // The attendant's announcement ends the game and takes the player back to the observatory, inside its door to
        // the marsh; the room's arrival walks them out past the counter (plan 01 · M11, part b)
        foreach (string end in new[] { FieldScripts.SafariTimeUp, FieldScripts.SafariOutOfBalls })
        {
            var host = new HeadlessScriptHost { MapNamed = name => name == Gate ? GateRoom() : null };
            host.Safari.Start();
            var runner = new ScriptRunner(ScriptLibrary.Default, host);
            runner.Start(ScriptLibrary.Default.Find(end)!);
            runner.RunToEnd();
            Assert.False(host.Safari.Active);
            Assert.Equal(2, host.Story.Var("VAR_SAFARI_GAME_STATE"));
            Assert.Equal((Gate, (5, 3)), (host.Map!.Name, host.PlayerTile));
            runner.Start(ScriptLibrary.Default.Find("OnEnter", Gate)!);
            runner.RunToEnd();
            Assert.Equal(0, host.Story.Var("VAR_SAFARI_GAME_STATE"));
            Assert.Equal((5, 7), host.PlayerTile);
            Assert.Empty(host.Problems);
        }

        // Coming back through the door with a game still on, the attendant asks: yes ends the game...
        var leaving = new HeadlessScriptHost { Map = GateRoom(), PlayerTile = (5, 3) };
        leaving.Safari.Start();
        leaving.Answers.Enqueue(0);
        var room = new ScriptRunner(ScriptLibrary.Default, leaving);
        room.Start(ScriptLibrary.Default.Find("OnEnter", Gate)!);
        room.RunToEnd();
        Assert.False(leaving.Safari.Active);
        Assert.Equal((5, 7), leaving.PlayerTile);
        // ... no leads back into the marsh with everything as it was
        var staying = new HeadlessScriptHost { Map = GateRoom(), PlayerTile = (5, 3) };
        staying.Safari.Start();
        staying.Safari.Balls = 12;
        staying.Answers.Enqueue(1);
        room = new ScriptRunner(ScriptLibrary.Default, staying);
        room.Start(ScriptLibrary.Default.Find("OnEnter", Gate)!);
        room.RunToEnd();
        Assert.True(staying.Safari.Active);
        Assert.Equal(12, staying.Safari.Balls);
        Assert.Contains("warp GreatMarsh 68 116", staying.Log);
        // and it says nothing to someone who only came in from the street
        var passing = new HeadlessScriptHost { Map = GateRoom(), PlayerTile = (5, 11) };
        room = new ScriptRunner(ScriptLibrary.Default, passing);
        room.Start(ScriptLibrary.Default.Find("OnEnter", Gate)!);
        room.RunToEnd();
        Assert.Empty(passing.Transcript);
        Assert.Equal((5, 11), passing.PlayerTile);
    }

    [Fact]
    public void ASafariGameIsSavedWithItsBallsAndSteps()
    {
        var save = new SaveData { Safari = new SafariSave(12, 345) };
        var read = JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(save))!;
        Assert.Equal(new SafariSave(12, 345), read.Safari);
        Assert.Null(JsonSerializer.Deserialize<SaveData>("{\"PlayerName\":\"Lucas\"}")!.Safari);
    }

    [Fact]
    public void TheGreatMarshHasItsTramAndItsViewers()
    {
        var marsh = BuiltMaps.Value["GreatMarsh"];
        Assert.Contains(marsh.Props, p => p.Type == PropType.Tram);
        Assert.Contains(marsh.Props, p => p.Type == PropType.Binoculars);
    }

    [Fact]
    public void TheEastsStandInsAreGoneAndBoatsSideBySideAreTwo()
    {
        Assert.DoesNotContain(WorldModels.All, m => m.StandInUntil == "M7");

        // The original moors Pastoria's two boats with boxes that overlap: each keeps its own side of the line between them
        var boats = Overworld.Props.Where(p => p.Type == PropType.Boat).ToList();
        // (the Fight Area's quay has three more of the model, plan 01 · M10)
        var pastoria = boats.Where(p => p.Model == "c06_s02" && AreaKey(Overworld, p.X, p.Y) == "pastoria_city").ToList();
        Assert.Equal(2, pastoria.Count);
        Assert.All(boats, a => Assert.DoesNotContain(boats, b => !ReferenceEquals(a, b)
            && a.X < b.X + b.Width && b.X < a.X + a.Width && a.Y < b.Y + b.Depth && b.Y < a.Y + a.Depth));
    }

    // ------------------------------------------------------------------ Canalave City and Iron Island

    [Fact]
    public void CanalavesDrawbridgeIsADrawbridge()
    {
        var map = Overworld;
        var leaves = map.Props.Where(p => p.Type == PropType.Drawbridge).ToList();
        Assert.Equal(2, leaves.Count);
        Assert.All(leaves, p => Assert.Equal("canalave_city", AreaKey(map, p.X, p.Y)));
        // Its deck of planks is walked across the canal
        Assert.All(leaves, p => Assert.Contains(Walked.Value[map], t => p.Covers(t.X, t.Y)));
    }

    [Theory]
    [InlineData("canalave_city", "iron_island")]
    [InlineData("iron_island", "canalave_city")]
    public void TheFerrySailsBetweenCanalaveAndIronIsland(string from, string to)
    {
        var map = Overworld;
        var sailor = Assert.Single(map.Everyone, n => n.Script == "Ferry" && AreaKey(map, n.GridX, n.GridY) == from);
        var host = new HeadlessScriptHost { Map = map, PlayerTile = (sailor.GridX + 1, sailor.GridY), PlayerFacing = Direction.Left };
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find(FieldScripts.For(sailor)!, from)!, sailor);
        runner.RunToEnd();
        Assert.Contains(host.Log, l => l.StartsWith("warp Sinnoh"));
        Assert.Equal(to, AreaKey(map, host.PlayerTile.X, host.PlayerTile.Y));
        Assert.True(map.IsWalkable(host.PlayerTile.X, host.PlayerTile.Y), $"the ferry lands the player on a blocked tile, ({host.PlayerTile.X},{host.PlayerTile.Y})");
        Assert.Null(map.GetNpcAt(host.PlayerTile.X, host.PlayerTile.Y));

        // Saying no keeps the player on the quay
        var staying = new HeadlessScriptHost { Map = map, PlayerTile = (sailor.GridX + 1, sailor.GridY) };
        staying.Answers.Enqueue(1);
        runner = new ScriptRunner(ScriptLibrary.Default, staying);
        runner.Start(ScriptLibrary.Default.Find(FieldScripts.For(sailor)!, from)!, sailor);
        runner.RunToEnd();
        Assert.DoesNotContain(staying.Log, l => l.StartsWith("warp"));
    }

    [Theory]
    [InlineData("IronIslandB1FRight", 11, 22, 11, 24)]
    [InlineData("IronIslandB1FRight", 11, 23, 11, 21)]
    [InlineData("IronIslandB2FLeft", 19, 43, 19, 45)]
    [InlineData("IronIslandB2FLeft", 19, 44, 19, 42)]
    [InlineData("IronIslandB3F", 9, 10, 9, 12)]
    [InlineData("IronIslandB3F", 9, 11, 9, 9)]
    public void IronIslandsLiftsCarryThePlayerBetweenLevels(string name, int x, int y, int toX, int toY)
    {
        var map = BuiltMaps.Value[name];
        Assert.True(map.IsCave);
        var lift = map.GetWarpAt(x, y);
        Assert.NotNull(lift);
        Assert.Equal((name, toX, toY), (lift!.TargetMap, lift.TargetX, lift.TargetY));
        Assert.True(map.IsWalkable(toX, toY));
        Assert.Null(map.GetWarpAt(toX, toY));
        Assert.Contains((toX, toY), Walked.Value[map]);
    }

    [Fact]
    public void EveryFloorOfIronIslandIsComeTo()
    {
        foreach (string name in new[] { "IronIsland1F", "IronIslandB1FLeft", "IronIslandB1FRight", "IronIslandB2FRight", "IronIslandB2FLeft", "IronIslandB3F" })
        {
            var map = BuiltMaps.Value[name];
            Assert.True(Walked.Value.TryGetValue(map, out var reached) && reached.Count > 20, $"{name} isn't come to");
        }
    }

    // ------------------------------------------------------------------ the people

    [Fact]
    public void TheEastsTrainersArePlatinums()
    {
        var map = Overworld;
        string[] routes = { "route_210_south", "route_210_north", "route_212_north", "route_212_south", "route_213", "route_214", "route_215", "route_218", "route_221" };
        var trainers = map.NPCs.Where(n => n.IsTrainer && routes.Contains(AreaKey(map, n.GridX, n.GridY))).ToList();
        Assert.True(trainers.Count >= 40, $"only {trainers.Count} trainers on the east's routes");
        Assert.All(trainers, t => Assert.True(t.TrainerData!.Party.Count > 0 && t.TrainerData.PrizeMoney > 0, $"{t.Name} has no team or no prize"));
    }
}
