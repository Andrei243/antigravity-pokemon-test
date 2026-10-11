using System.Numerics;
using MapImporter;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumTests;

/// <summary>
/// The rest of the post-game as plan 01 · M10, part b opened it: Stark Mountain and its rooms, Snowpoint Temple's
/// floors, Fullmoon and Newmoon Island with their woods, the Seabreak Path and Flower Paradise behind the chunks the
/// story swaps, the Battle Frontier's grounds, and the Ribbon Syndicate's ground floor.
/// </summary>
public class PostGameTests
{
    // Maps of this class's own, so nothing another test does to the database's maps shows here. One test lays the
    // Seabreak Path's chunks down and takes them up again; the tests of a class run one after another.
    private static readonly Lazy<World> LoadedWorld = new(() => World.LoadAll().Single(w => w.Index.Region == "Sinnoh"));
    private static readonly Lazy<Dictionary<string, Map>> BuiltMaps = new(() => LoadedWorld.Value.BuildMaps().ToDictionary(m => m.Name));

    private static World Sinnoh => LoadedWorld.Value;
    private static Map Overworld => BuiltMaps.Value["Sinnoh"];

    // The rooms a walk goes through are a set of this class's own too: other classes reload the shared set meanwhile
    private static readonly Lazy<Func<string, Map>> Rooms = new(() => MapDatabase.Fresh());

    private static Map MapNamed(string name) => BuiltMaps.Value.TryGetValue(name, out var map) ? map : Rooms.Value(name);

    private static string? AreaKey(Map map, int x, int y) => map.AreaAt(x, y)?.Key;

    private static IEnumerable<NPC> PeopleOf(Map map, string area) => map.Everyone.Where(n => AreaKey(map, n.GridX, n.GridY) == area);

    private static Map Room(string name) => GameDataFiles.Load<MapFile>(Path.Combine(MapDatabase.Folder, name + ".json")).ToMap();

    /// <summary>What lies on the map of Sinnoh. The Seabreak Path and Flower Paradise are come to only once the story has revealed them.</summary>
    private static readonly string[] Outdoors = { "stark_mountain_outside", "fullmoon_island", "newmoon_island", "seabreak_path", "flower_paradise" };

    private static readonly string[] Revealed = { "seabreak_path", "flower_paradise" };

    /// <summary>What is a matrix, and so a map, of its own.</summary>
    private static readonly (string Key, string Map)[] Insides =
    {
        ("stark_mountain_room_1", "StarkMountainRoom1"), ("stark_mountain_room_2", "StarkMountainRoom2"), ("stark_mountain_room_3", "StarkMountainRoom3"),
        ("snowpoint_temple_1f", "SnowpointTemple1F"), ("snowpoint_temple_b1f", "SnowpointTempleB1F"), ("snowpoint_temple_b2f", "SnowpointTempleB2F"),
        ("snowpoint_temple_b3f", "SnowpointTempleB3F"), ("snowpoint_temple_b4f", "SnowpointTempleB4F"), ("snowpoint_temple_b5f", "SnowpointTempleB5F"),
        ("fullmoon_island_forest", "FullmoonIslandForest"), ("newmoon_island_forest", "NewmoonIslandForest"), ("battle_frontier", "BattleFrontier")
    };

    private static readonly (int X, int Y)[] Steps = { (0, -1), (1, 0), (-1, 0), (0, 1) };

    /// <summary>The whole game walked from its start and from where only the story takes the player (the ships' landings among them).</summary>
    private static readonly Lazy<Dictionary<Map, HashSet<(int X, int Y)>>> Walked = new(() =>
        WorldWalk.From(MapNamed, WorldWalk.StoryArrivals.Prepend(RegionDatabase.Get(RegionDatabase.Sinnoh)!.Start!).ToArray()));

    private static HeadlessScriptHost Run(string script, string place, HeadlessScriptHost host, NPC? who = null)
    {
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find(script, place)!, who);
        runner.RunToEnd();
        Assert.Empty(host.Problems);
        return host;
    }

    // ------------------------------------------------------------------ open, and come to

    [Fact]
    public void TheRestOfThePostGameIsOpen()
    {
        foreach (string key in Outdoors.Concat(Insides.Select(i => i.Key)))
        {
            Assert.True(Sinnoh.IsOpen(key), $"{key} isn't open");
            Assert.NotNull(Sinnoh.Overlay(key));
        }
        foreach (string key in Outdoors) Assert.True(Overworld.FindArea(key) is { Open: true }, key);
        foreach (var (key, name) in Insides)
        {
            Assert.Equal(name, Sinnoh.MapOf(key)!.Name);
            var map = BuiltMaps.Value[name];
            Assert.True(map.IsStreamed);
            Assert.Equal(key, Assert.Single(map.Areas, a => a.Open).Key);
            Assert.Equal(RegionDatabase.Sinnoh, RegionDatabase.RegionOfMap(name)!.Id);
        }
        Assert.Equal("Stark Mountain", Overworld.DisplayNameAt(750, 235));
        Assert.Equal("Fullmoon Island", Overworld.DisplayNameAt(40, 277));
        Assert.Equal("Newmoon Island", Overworld.DisplayNameAt(151, 277));
        Assert.Equal("Flower Paradise", Overworld.DisplayNameAt(911, 203));
        Assert.Equal("Seabreak Path", Overworld.DisplayNameAt(911, 300));
        Assert.Equal("Battle Frontier", BuiltMaps.Value["BattleFrontier"].DisplayNameAt(48, 40));
        Assert.Equal("Snowpoint Temple", BuiltMaps.Value["SnowpointTempleB3F"].DisplayNameAt(8, 8));

        // Stark Mountain's rooms and the temple are caves; the islands' woods and the Frontier are seen from close by
        Assert.All(Insides.Take(9), i => Assert.True(BuiltMaps.Value[i.Map].IsCave, i.Map));
        Assert.All(Insides.Skip(9), i => Assert.Equal((false, FieldCamera.ZoomedIn), (BuiltMaps.Value[i.Map].IsCave, BuiltMaps.Value[i.Map].Camera)));
    }

    [Fact]
    public void EverythingThereCanBeComeToButWhatTheStoryReveals()
    {
        var reached = Walked.Value;
        var missed = new List<string>();
        var places = Outdoors.Except(Revealed).Select(key => (Key: key, Map: Overworld)).Concat(Insides.Select(i => (i.Key, Map: BuiltMaps.Value[i.Map])));
        foreach (var (key, map) in places)
        {
            Assert.True(reached.TryGetValue(map, out var reach), $"{map.Name} can't be reached");
            bool Beside(int x, int y) => Steps.Any(s => reach!.Contains((x + s.X, y + s.Y)));
            Assert.True(reach!.Any(t => AreaKey(map, t.X, t.Y) == key), $"nobody can come to {key}");

            foreach (var npc in PeopleOf(map, key))
                if (!Beside(npc.GridX, npc.GridY) && !reach.Contains((npc.GridX, npc.GridY)) && !map.IsCounter(npc.GridX + (npc.Facing == Direction.Right ? 1 : npc.Facing == Direction.Left ? -1 : 0), npc.GridY))
                    missed.Add($"{key}: {npc.Key ?? npc.Name} at ({npc.GridX},{npc.GridY})");
            foreach (var (x, y) in map.Signboards.Keys.Where(t => AreaKey(map, t.Item1, t.Item2) == key))
                if (!Beside(x, y)) missed.Add($"{key}: the sign at ({x},{y})");
            foreach (var (x, y) in map.TileScripts.Keys.Where(t => AreaKey(map, t.Item1, t.Item2) == key))
                if (!Beside(x, y)) missed.Add($"{key}: what is read at ({x},{y})");
            foreach (var warp in map.Warps.Where(w => AreaKey(map, w.SourceX, w.SourceY) == key))
                if (!reach.Contains((warp.SourceX, warp.SourceY))) missed.Add($"{key}: the way to {warp.TargetMap} at ({warp.SourceX},{warp.SourceY})");
        }
        Assert.True(missed.Count == 0, "Out of reach: " + string.Join("; ", missed));

        // The Seabreak Path lies across a sea nobody crosses until the story swaps Route 224's chunks
        Assert.DoesNotContain(reached[Overworld], t => AreaKey(Overworld, t.X, t.Y) is "seabreak_path" or "flower_paradise");
        Assert.All(reached[Overworld], t => Assert.True(Overworld.AreaAt(t.X, t.Y)!.Open));
    }

    // ------------------------------------------------------------------ the chunks the story swaps

    [Fact]
    public void TheSeabreakPathLiesAcrossTheSeaOnceRoute224sChunksAreSwapped()
    {
        var map = Overworld;
        // world.json: the original's MapMatrix_RevealSeabreakPath writes four chunks into Route 224's cells while the
        // variable holds the path's number, 0x1028
        var swap = Assert.Single(Sinnoh.Index.Swaps);
        Assert.Equal(("Sinnoh", "VAR_HIDDEN_LOCATION_SEABREAK_PATH", 0x1028), (swap.Map, swap.Var, swap.Value));
        Assert.Equal(new[] { (28, 15, 119), (27, 16, 120), (28, 16, 121), (27, 17, 122) }, swap.Cells.Select(c => (c.ChunkX, c.ChunkY, c.Chunk)));
        // The game ships the chunks it swaps in as well as those it swaps out
        Assert.All(swap.Cells, c => Assert.NotNull(Sinnoh.Chunk(c.Chunk)));

        var place = Assert.Single(map.SwappedPlaces);
        Assert.Equal((swap.Var, swap.Value, false), (place.Var, place.Value, place.Swapped));
        Assert.Equal(swap.Cells.Select(c => (c.ChunkX, c.ChunkY)), place.Cells);
        Assert.All(place.Cells, c => Assert.Equal("route_224", AreaKey(map, c.X * Map.ChunkTiles + 1, c.Y * Map.ChunkTiles + 1)));
        Assert.True(place.Covers(28 * Map.ChunkTiles + 5, 15 * Map.ChunkTiles + 5));
        Assert.False(place.Covers(28 * Map.ChunkTiles + 5, 14 * Map.ChunkTiles + 5));

        (TileType, bool, float)[] Ground() => place.Cells.SelectMany(c => Enumerable.Range(0, Map.ChunkTiles * Map.ChunkTiles).Select(i =>
        {
            int x = c.X * Map.ChunkTiles + i % Map.ChunkTiles, y = c.Y * Map.ChunkTiles + i / Map.ChunkTiles;
            return (map.GetGroundTile(x, y), map.IsSolid(x, y), map.HeightAt(x, y));
        })).ToArray();
        var before = Ground();
        int props = map.Props.Count;
        var route = PeopleOf(map, "route_224").ToList();
        Assert.NotEmpty(route);

        // Any other number leaves Route 224 as it is
        Assert.Empty(map.ApplySwappedPlaces(_ => 0));
        Assert.Empty(map.ApplySwappedPlaces(_ => 0x1028 + 1));
        try
        {
            Assert.Same(place, Assert.Single(map.ApplySwappedPlaces(name => name == swap.Var ? 0x1028 : 0)));
            Assert.True(place.Swapped);
            Assert.Empty(map.ApplySwappedPlaces(_ => 0x1028));
            var after = Ground();
            Assert.NotEqual(before, after);

            // The events know nothing of chunks: everyone of Route 224 stands on open ground still, each sign is solid
            Assert.Equal(route, PeopleOf(map, "route_224"));
            Assert.All(route.Where(n => !n.IsThing || n.IsItemBall), n => Assert.False(map.IsSolid(n.GridX, n.GridY), $"{n.Key} at ({n.GridX},{n.GridY}) stands in something solid once the path shows"));
            Assert.All(map.Signboards.Keys.Where(t => AreaKey(map, t.Item1, t.Item2) == "route_224"), t => Assert.True(map.IsSolid(t.Item1, t.Item2)));

            // From Route 224's grass the walk goes up the path to Flower Paradise, and no further than what is open
            var walked = WorldWalk.From(MapNamed, new MapSpot("Sinnoh", 885, 535, Direction.Up))[map];
            Assert.Contains(walked, t => AreaKey(map, t.X, t.Y) == "seabreak_path");
            Assert.Contains(walked, t => AreaKey(map, t.X, t.Y) == "flower_paradise");
            Assert.Contains(walked, t => Math.Abs(t.X - 911) + Math.Abs(t.Y - 203) <= 2);
            Assert.All(walked, t => Assert.True(map.AreaAt(t.X, t.Y)!.Open));
        }
        finally
        {
            map.ApplySwappedPlaces(_ => 0);
        }
        Assert.False(place.Swapped);
        Assert.Equal(before, Ground());
        Assert.Equal(props, map.Props.Count);

        // A swap doesn't carry a building: one of chunks a building of the world's models stands on is refused
        var town = map.PlacedBuildings!.First();
        Assert.Throws<InvalidOperationException>(() => map.AddSwappedPlace("VAR_TEST", 1, new[] { (town.X0 / Map.ChunkTiles, town.Y0 / Map.ChunkTiles) }, map));
        Assert.Single(map.SwappedPlaces);

        // The path and the meadow at its end: Floaroma's theme stands in for the original's, nobody stands there (Shaymin
        // is an event gift's), and arriving at the meadow counts as coming to it
        Assert.All(Revealed, key => Assert.Equal("sinnoh/floaroma", map.FindArea(key)!.BgmTrack));
        Assert.All(Revealed, key => Assert.Empty(PeopleOf(map, key)));
        var story = new StoryState();
        Run(ScriptLibrary.OnEnter, "flower_paradise", new HeadlessScriptHost(story));
        Assert.True(story.Has("FLAG_FIRST_ARRIVAL_FLOWER_PARADISE"));
    }

    // ------------------------------------------------------------------ ships, gates and doors

    [Theory]
    [InlineData("fullmoon_island", 39, 277, "FLAG_FIRST_ARRIVAL_FULLMOON_ISLAND", "VAR_HIDDEN_LOCATION_FULL_MOON_ISLAND", 0x0208)]
    [InlineData("newmoon_island", 152, 277, "FLAG_FIRST_ARRIVAL_NEWMOON_ISLAND", "VAR_HIDDEN_LOCATION_NEW_MOON_ISLAND", 0x0229)]
    public void AnIslandsSailorSailsBackToCanalaveCity(string island, int landX, int landY, string arrival, string shown, int number)
    {
        var map = Overworld;
        // Where the original's TakeShipFromCanalave puts the player down: on the island's pier, beside its sailor
        Assert.Contains(WorldWalk.StoryArrivals, s => (s.Map, s.X, s.Y) == ("Sinnoh", landX, landY));
        Assert.Equal(island, AreaKey(map, landX, landY));
        Assert.True(map.IsWalkable(landX, landY));
        var sailor = Assert.Single(PeopleOf(map, island));
        Assert.Equal(("sailor", "Ferry", 1), (sailor.Key, sailor.Script, Math.Abs(sailor.GridX - landX) + Math.Abs(sailor.GridY - landY)));
        Assert.Equal(island == "newmoon_island" ? "FLAG_HIDE_NEWMOON_ISLAND_SAILOR" : "", sailor.HiddenBy ?? "");
        // The ship lies at the pier, and keeps off the planks the sailor stands on
        var boat = Assert.Single(map.Props, p => p.Type == PropType.Boat && AreaKey(map, p.X, p.Y) == island);
        Assert.False(boat.Covers(sailor.GridX, sailor.GridY));

        // He sails to Canalave City's quay
        var host = Run("Ferry", island, new HeadlessScriptHost { Map = map, PlayerTile = (landX, landY) }, sailor);
        Assert.Contains(host.Log, l => l.StartsWith("warp Sinnoh"));
        Assert.Equal("canalave_city", AreaKey(map, host.PlayerTile.X, host.PlayerTile.Y));
        Assert.True(map.IsWalkable(host.PlayerTile.X, host.PlayerTile.Y));
        var staying = new HeadlessScriptHost { Map = map, PlayerTile = (landX, landY) };
        staying.Answers.Enqueue(1);
        Assert.DoesNotContain(Run("Ferry", island, staying, sailor).Log, l => l.StartsWith("warp"));

        // Arriving counts as coming to the island and puts it on the maps (the original's EnableHiddenLocation)
        var story = new StoryState();
        Run(ScriptLibrary.OnEnter, island, new HeadlessScriptHost(story));
        Assert.True(story.Has(arrival));
        Assert.Equal(number, story.Var(shown));

        // The wood in the island's middle: two tiles in, two tiles out, and nobody in it until its Pokémon's session
        var wood = BuiltMaps.Value[Sinnoh.MapOf(island + "_forest")!.Name];
        var doors = map.Warps.Where(w => AreaKey(map, w.SourceX, w.SourceY) == island).ToList();
        Assert.Equal(2, doors.Count);
        Assert.All(doors, d => Assert.Equal(wood.Name, d.TargetMap));
        Assert.Equal(2, wood.Warps.Count(w => w.TargetMap == "Sinnoh" && AreaKey(map, w.TargetX, w.TargetY) == island && map.IsWalkable(w.TargetX, w.TargetY)));
        Assert.Empty(wood.Everyone);
        Assert.Equal(island == "fullmoon_island" ? "sinnoh/forest" : "sinnoh/turnback_cave", wood.Areas.Single(a => a.Open).BgmTrack);
    }

    [Fact]
    public void SnowpointTempleIsBuiltBehindDoorsTheStoryOpens()
    {
        var map = Overworld;
        var hall = BuiltMaps.Value["SnowpointTemple1F"];
        // Two doors side by side, as the original's, both shut until the story lets the Champion in (plan 02 · S15):
        // the guard stands before the east one, and the tile before the west one is the trigger that turns the player
        // back while VAR_SNOWPOINT_CITY_STATE is 0 (plan 02 · S11, part a)
        var doors = Sinnoh.Area("snowpoint_city")!.Warps.Select((w, i) => (Warp: w, Number: i)).Where(d => d.Warp.To == "snowpoint_temple_1f").ToList();
        Assert.Equal(new[] { (366, 197, 2), (367, 197, 6) }, doors.Select(d => (d.Warp.X, d.Warp.Z, d.Number)));
        Assert.All(doors, d =>
        {
            Assert.Contains(d.Number, Sinnoh.Overlay("snowpoint_city")!.Locked!);
            Assert.Null(map.GetWarpAt(d.Warp.X, d.Warp.Z));
            Assert.True(map.IsSolid(d.Warp.X, d.Warp.Z));
        });
        // Until then the walk of the whole game sets out from the tile inside the mat, where a door would leave it
        Assert.Contains(WorldWalk.StoryArrivals, s => (s.Map, s.X, s.Y) == ("SnowpointTemple1F", 8, 12));
        Assert.True(hall.IsWalkable(8, 12));
        var guard = Assert.Single(PeopleOf(map, "snowpoint_city"), n => n.Key == "temple_guard");
        Assert.Equal((367, 198), (guard.GridX, guard.GridY));
        var trigger = Assert.Single(map.Triggers, t => (t.X, t.Y) == (366, 198));
        Assert.Equal(("TempleGuard", "VAR_SNOWPOINT_CITY_STATE", 0), (trigger.Script, trigger.Variable, trigger.Value));
        var turned = Run("TempleGuard", "snowpoint_city", new HeadlessScriptHost { Map = map, PlayerTile = (366, 198), PlayerFacing = Direction.Up });
        Assert.Equal((366, 199), turned.PlayerTile);

        // The way out is the hall's mat, and the floors go down by stairs at each end, five floors under the hall
        var exit = Assert.Single(hall.Warps, w => w.TargetMap == "Sinnoh");
        Assert.Equal((8, 13, 366, 198), (exit.SourceX, exit.SourceY, exit.TargetX, exit.TargetY));
        string[] floors = { "SnowpointTemple1F", "SnowpointTempleB1F", "SnowpointTempleB2F", "SnowpointTempleB3F", "SnowpointTempleB4F", "SnowpointTempleB5F" };
        for (int i = 0; i < floors.Length; i++)
        {
            var floor = BuiltMaps.Value[floors[i]];
            Assert.Equal(i < floors.Length - 1 ? 1 : 0, floor.Warps.Count(w => w.TargetMap == floors[Math.Min(i + 1, floors.Length - 1)] && i < floors.Length - 1));
            Assert.Equal(i > 0 ? 1 : 0, floor.Warps.Count(w => i > 0 && w.TargetMap == floors[i - 1]));
            // Whoever takes a staircase is put down on open ground beside the one it leads to
            Assert.All(floor.Warps.Where(w => w.TargetMap != "Sinnoh"), w =>
            {
                var onto = BuiltMaps.Value[w.TargetMap];
                Assert.False(onto.IsSolid(w.TargetX, w.TargetY), $"{floor.Name}: the stairs at ({w.SourceX},{w.SourceY}) lead into something solid");
                Assert.Null(onto.GetWarpAt(w.TargetX, w.TargetY));
                Assert.Contains(onto.Warps, back => back.TargetMap == floor.Name && Math.Abs(back.SourceX - w.TargetX) + Math.Abs(back.SourceY - w.TargetY) == 1);
            });
            Assert.Equal(AreaClimate.Frozen, floor.Areas.Single(a => a.Open).Climate);
            Assert.Equal("sinnoh/forest", floor.Areas.Single(a => a.Open).BgmTrack);
        }
        // The hall's stairs stand in a passage that runs past them: one comes up them into the passage, not into its wall
        var up = Assert.Single(BuiltMaps.Value["SnowpointTempleB1F"].Warps, w => w.TargetMap == "SnowpointTemple1F");
        Assert.Equal((14, 4, Direction.Down), (up.TargetX, up.TargetY, up.TargetFacing));

        // Its ice is the field's own, floor by floor; rocks and a boulder stand in the way; Regigigas waits for the story
        Assert.Contains(Enumerable.Range(0, 32 * 32), i => hall.BehaviourAt(i % 32, i / 32) == TileBehavior.Ice);
        Assert.True(Enumerable.Range(0, 32 * 32).Count(i => BuiltMaps.Value["SnowpointTempleB1F"].BehaviourAt(i % 32, i / 32) == TileBehavior.Ice) >= 8);
        Assert.True(floors.Count(floor => Enumerable.Range(0, 32 * 32).Any(i => BuiltMaps.Value[floor].BehaviourAt(i % 32, i / 32) == TileBehavior.Ice)) >= 3, "ice on most floors");
        Assert.Equal(2, BuiltMaps.Value["SnowpointTempleB2F"].Everyone.Count(n => n.NpcType == NPC.CrackedRockType));
        Assert.Equal(1, BuiltMaps.Value["SnowpointTempleB4F"].Everyone.Count(n => n.NpcType == NPC.BoulderType));
        Assert.Equal("Calcium", Assert.Single(BuiltMaps.Value["SnowpointTempleB1F"].Everyone, n => n.IsItemBall).Item);
        Assert.Equal("Full Restore", Assert.Single(BuiltMaps.Value["SnowpointTempleB3F"].HiddenItems.Values).Item);
        Assert.Empty(BuiltMaps.Value["SnowpointTempleB5F"].Everyone);
        Assert.Contains("Sneasel", hall.Areas.Single(a => a.Open).WildEncounters.Select(e => e.SpeciesName));
        Assert.Contains("Jynx", BuiltMaps.Value["SnowpointTempleB5F"].Areas.Single(a => a.Open).WildEncounters.Select(e => e.SpeciesName));

        // The statue before the stairs is read from the three tiles in front of it, and the hall has its pillars
        var colossus = Assert.Single(hall.Props, p => p.Type == PropType.Colossus);
        Assert.Equal(("d20_o02", 3), (colossus.Model, colossus.Width));
        Assert.True(hall.Props.Count(p => p.Type == PropType.Column && p.Model == "d20_o01") >= 4);
        foreach (int x in new[] { 7, 8, 9 }) Assert.Equal("Statue", hall.TileScripts[(x, 6)]);
        Assert.Contains(Run("Statue", "snowpoint_temple_1f", new HeadlessScriptHost { Map = hall }).Transcript, l => l.Text.Contains("statue"));
        var story = new StoryState();
        Run(ScriptLibrary.OnEnter, "snowpoint_temple_1f", new HeadlessScriptHost(story));
        Assert.True(story.Has("FLAG_FIRST_ARRIVAL_SNOWPOINT_TEMPLE"));
    }

    [Fact]
    public void TheFrontiersGateIsWalkedThroughAndItsHallsDoorsAreShut()
    {
        var map = Overworld;
        var grounds = BuiltMaps.Value["BattleFrontier"];
        // The gate house between the Fight Area and the grounds has no room yet: one walks through it, both ways
        foreach (int i in new[] { 0, 1 })
        {
            var into = map.GetWarpAt(654 + i, 423)!;
            Assert.Equal(("BattleFrontier", 48 + i, 51, Direction.Up), (into.TargetMap, into.TargetX, into.TargetY, into.TargetFacing));
            Assert.True(grounds.IsWalkable(into.TargetX, into.TargetY));
            var back = grounds.GetWarpAt(48 + i, 52)!;
            Assert.Equal(("Sinnoh", 654 + i, 424, Direction.Down), (back.TargetMap, back.TargetX, back.TargetY, back.TargetFacing));
            // The ground before the gate is open, and the two who stand on it until the tag battle is won (Flint and
            // Volkner, plan 02 · S15, part a: nobody goes in before then, as in the original) go by that battle's flag
            Assert.False(map.IsSolid(back.TargetX, back.TargetY));
            Assert.All(map.Everyone.Where(n => (n.GridX, n.GridY) == (back.TargetX, back.TargetY)),
                n => Assert.Equal("FLAG_HIDE_FIGHT_AREA_BATTLERS_AND_SPECTATORS", n.HiddenBy));
        }
        // (the gate's two, the Battle Tower's two, and three each of the Battle Factory, the Battle Hall and the Battle Castle)
        Assert.Equal(13, grounds.Warps.Count);

        // The five facilities' doors are the rules lane's (plan 06 · R18). The Battle Tower's two lead into its lobby
        // (R18a), the Battle Factory's three to the head of its lobby's stairs (R18b), the Battle Hall's and the
        // Battle Castle's onto the mats inside their lobbies' east doors (R18c); every way into the Battle Arcade is
        // shut until its sitting, at the tiles the original has them
        var file = Sinnoh.Area("battle_frontier")!;
        var facilities = file.Warps.Where(w => w.To.StartsWith("battle_") && !w.To.Contains("gate")).ToList();
        Assert.Equal(14, facilities.Count);
        Assert.Equal(new[] { "battle_arcade", "battle_castle", "battle_factory", "battle_hall", "battle_tower" }, facilities.Select(w => w.To).Distinct().OrderBy(n => n));
        var halls = facilities.Where(w => w.To == "battle_arcade").ToList();
        Assert.Equal(3, halls.Count);
        Assert.All(halls, w =>
        {
            Assert.Null(grounds.GetWarpAt(w.X, w.Z));
            Assert.True(grounds.IsSolid(w.X, w.Z), $"the way into {w.To} at ({w.X},{w.Z}) can be walked into");
        });
        Assert.Equal(new[] { (48, 17), (49, 17) }, facilities.Where(w => w.To == "battle_tower").Select(w => (w.X, w.Z)));
        Assert.All(facilities.Where(w => w.To == "battle_tower"), w =>
        {
            var door = grounds.GetWarpAt(w.X, w.Z)!;
            Assert.Equal(("BattleTower", 15, 19, Direction.Up), (door.TargetMap, door.TargetX, door.TargetY, door.TargetFacing));
        });
        Assert.Equal(new[] { (73, 34), (77, 34), (77, 35) }, facilities.Where(w => w.To == "battle_factory").Select(w => (w.X, w.Z)).OrderBy(p => p));
        Assert.All(facilities.Where(w => w.To == "battle_factory"), w =>
        {
            var door = grounds.GetWarpAt(w.X, w.Z)!;
            Assert.Equal(("BattleFactory", 1, 7, Direction.Right), (door.TargetMap, door.TargetX, door.TargetY, door.TargetFacing));
        });
        Assert.Equal(new[] { (19, 34), (19, 35), (23, 34) }, facilities.Where(w => w.To == "battle_hall").Select(w => (w.X, w.Z)).OrderBy(p => p));
        Assert.All(facilities.Where(w => w.To == "battle_hall"), w =>
        {
            var door = grounds.GetWarpAt(w.X, w.Z)!;
            Assert.Equal(("BattleHall", 25, 9, Direction.Left), (door.TargetMap, door.TargetX, door.TargetY, door.TargetFacing));
        });
        Assert.Equal(new[] { (33, 62), (33, 63), (36, 62) }, facilities.Where(w => w.To == "battle_castle").Select(w => (w.X, w.Z)).OrderBy(p => p));
        Assert.All(facilities.Where(w => w.To == "battle_castle"), w =>
        {
            var door = grounds.GetWarpAt(w.X, w.Z)!;
            Assert.Equal(("BattleCastle", 21, 9, Direction.Left), (door.TargetMap, door.TargetX, door.TargetY, door.TargetFacing));
        });
        // The triggers under the Factory's, the Hall's and the Castle's porches walk whoever steps on them in (the
        // original's scripts 37, 34 and 35), over ground that can be walked; the Arcade's does nothing yet
        Assert.Equal(new[] { (22, 34, 1, 2, "EnterBattleHall"), (35, 62, 1, 2, "EnterBattleCastle"), (74, 34, 1, 2, "EnterBattleFactory") },
            grounds.Triggers.Select(t => (t.X, t.Y, t.Width, t.Depth, t.Script)).OrderBy(t => t));
        Assert.All(grounds.Triggers, porch => Assert.NotNull(ScriptLibrary.Default.Find(porch.Script, "battle_frontier")));
        Assert.All(new[] { (72, 34), (74, 34), (74, 35), (75, 34), (76, 34), (75, 35), (76, 35) }, t => Assert.True(grounds.IsWalkable(t.Item1, t.Item2), $"({t.Item1},{t.Item2}) before the Factory's doors can't be walked"));
        Assert.All(new[] { (24, 34), (22, 34), (22, 35), (21, 34), (20, 34) }, t => Assert.True(grounds.IsWalkable(t.Item1, t.Item2), $"({t.Item1},{t.Item2}) before the Hall's doors can't be walked"));
        Assert.All(new[] { (37, 62), (35, 62), (35, 63), (34, 62) }, t => Assert.True(grounds.IsWalkable(t.Item1, t.Item2), $"({t.Item1},{t.Item2}) before the Castle's doors can't be walked"));
        // Whoever comes out of a lobby stands on the grounds before its porch, and the lobby's doors lead there
        foreach (var (lobby, x, y) in new[] { ("BattleHall", 24, 34), ("BattleCastle", 37, 62) })
        {
            var room = GameDataFiles.Load<MapFile>(System.IO.Path.Combine(MapDatabase.Folder, lobby + ".json")).ToMap();
            Assert.All(room.Warps.Where(w => w.TargetMap == "BattleFrontier"), w => Assert.Equal((x, y, Direction.Right), (w.TargetX, w.TargetY, w.TargetFacing)));
            Assert.Contains(room.Warps, w => w.TargetMap == "BattleFrontier");
        }

        // Its people have lines of ours; two of the Exchange Service Corner's attendants trade Battle Points for their
        // booth's prizes (R18a) and the third says what she waits for; the reporter, and whoever the story brings,
        // aren't there
        var people = grounds.Everyone.Where(n => !n.IsThing).ToList();
        Assert.True(people.Count >= 20, $"only {people.Count} people in the grounds");
        Assert.All(people, n => Assert.True(n.DialogLines.Count > 0 || n.Script != null, $"{n.Key} has nothing to say"));
        Assert.DoesNotContain(people, n => n.Key is "reporter" or "rival" or "palmer");
        var attendants = people.Where(n => n.Key!.StartsWith("attendant_")).ToList();
        Assert.Equal(new[] { "ExchangeItems", "ExchangeMachines", "ScratchCards" }, attendants.Select(n => n.Script).OrderBy(s => s));
        Assert.All(attendants, a =>
        {
            // Each stands inside a booth and serves over its counter, the way they face
            int dx = a.Facing == Direction.Right ? 1 : -1;
            Assert.Contains(grounds.Props, p => p.Type == PropType.Booth && p.Covers(a.GridX, a.GridY));
            Assert.True(grounds.IsCounter(a.GridX + dx, a.GridY), $"{a.Key} has no counter before them");
            Assert.True(Walked.Value[grounds].Contains((a.GridX + 2 * dx, a.GridY)), $"nobody can come to {a.Key}'s counter");
            Assert.Contains(Run(a.Script!, "battle_frontier", new HeadlessScriptHost { Map = grounds, PlayerTile = (a.GridX + 2 * dx, a.GridY) }, a).Transcript, l => l.Text.Contains("Battle Point"));
        });

        // The signposts name the five facilities, and the statues and the model are read where they stand
        Assert.Equal(5, grounds.Signboards.Count);
        foreach (string name in new[] { "Battle Tower", "Battle Hall", "Battle Castle", "Battle Arcade", "Battle Factory" })
            Assert.Contains(grounds.Signboards.Values, text => text.StartsWith(name));
        Assert.Equal(new[] { "ScaleModel", "StatueEast", "StatueWest" }, grounds.TileScripts.Values.Distinct().OrderBy(s => s));
        Assert.All(grounds.TileScripts, t => Assert.True(grounds.IsSolid(t.Key.Item1, t.Key.Item2)));
        Assert.Equal("sinnoh/veilstone", grounds.Areas.Single(a => a.Open).BgmTrack);
    }

    // ------------------------------------------------------------------ the Ribbon Syndicate

    [Fact]
    public void TheRibbonSyndicateAsksForTenRibbonsAtItsDoor()
    {
        var map = Overworld;
        var door = map.GetWarpAt(824, 453)!;
        Assert.Equal(("ResortAreaRibbonSyndicate1F", 15, 15, Direction.Up), (door.TargetMap, door.TargetX, door.TargetY, door.TargetFacing));
        Assert.Equal("resort_area", AreaKey(map, 824, 453));
        var room = Room("ResortAreaRibbonSyndicate1F");
        Assert.Equal(RegionDatabase.Sinnoh, RegionDatabase.RegionOfMap(room.Name)!.Id);
        // (the lift in its back wall is the other way out, since plan 01 · M11, part c: NorthInteriorTests)
        var exit = Assert.Single(room.Warps, w => w.TargetMap == "Sinnoh");
        Assert.Equal(("Sinnoh", 824, 454, Direction.Down), (exit.TargetMap, exit.TargetX, exit.TargetY, exit.TargetFacing));
        Assert.True(map.IsWalkable(824, 454));

        // The hostess stands on the tile inside the mat: nobody passes her without the script's leave
        var hostess = Assert.Single(room.Everyone, n => n.Key == "beauty");
        Assert.Equal((15, 14), (hostess.GridX, hostess.GridY));
        Assert.All(new[] { (14, 15), (16, 15) }, t => Assert.True(room.IsWalkable(t.Item1, t.Item2)));

        // Nine ribbons: asked, told, and shown out to the tile before the door
        var lead = new Pokemon(PokemonDatabase.Get("Skitty")!, 30, Gender.Female, Nature.Hardy, false);
        foreach (var ribbon in RibbonDatabase.All.Take(9)) RibbonRules.Give(lead, ribbon.Id);
        var host = new HeadlessScriptHost { Map = room, PlayerTile = (15, 15), PlayerFacing = Direction.Up, MapNamed = name => name == "Sinnoh" ? map : null };
        host.Party.Add(lead);
        Run(ScriptLibrary.OnEnter, room.Name, host);
        Assert.False(host.Story.Has("FLAG_JOINED_RIBBON_SYNDICATE"));
        Assert.Contains(host.Transcript, l => l.Speaker == "Hostess" && l.Text.Contains("ribbons"));
        Assert.Contains("warp Sinnoh 824 454", host.Log);
        Assert.Equal((map, (824, 454), Direction.Down), (host.Map, host.PlayerTile, host.PlayerFacing));
        Assert.Equal((15, 14), (hostess.GridX, hostess.GridY));

        // The tenth: a member for good, and she steps aside to the east
        RibbonRules.Give(lead, RibbonDatabase.All.Skip(9).First().Id);
        host.Map = room;
        host.PlayerTile = (15, 15);
        host.Log.Clear();
        Run(ScriptLibrary.OnEnter, room.Name, host);
        Assert.True(host.Story.Has("FLAG_JOINED_RIBBON_SYNDICATE"));
        Assert.Equal(1, host.Story.Var("VAR_RESORT_AREA_RIBBON_SYNDICATE_1F_STATE"));
        Assert.DoesNotContain(host.Log, l => l.StartsWith("warp"));
        Assert.Equal((16, 14, Direction.Left), (hostess.GridX, hostess.GridY, hostess.Facing));

        // Every visit after, she is beside the way already and asks nothing
        var again = Room("ResortAreaRibbonSyndicate1F");
        var member = new HeadlessScriptHost(host.Story) { Map = again, PlayerTile = (15, 15) };
        Run(ScriptLibrary.OnEnter, again.Name, member);
        var aside = Assert.Single(again.Everyone, n => n.Key == "beauty");
        Assert.Equal((16, 14, Direction.Down), (aside.GridX, aside.GridY, aside.Facing));
        Assert.Empty(member.Transcript);
        Assert.Contains(Run(FieldScripts.For(aside)!, again.Name, member, aside).Transcript, l => l.Speaker == "Hostess");

        // The cashier sells the Syndicate's three ribbons over her counter (plan 06 · R17b's script)
        var cashier = Assert.Single(again.Everyone, n => n.Key == "cashier_f");
        Assert.Equal("RibbonSyndicateCashier", cashier.Script);
        Assert.Equal(FieldScripts.RibbonSyndicateCashier, ScriptLibrary.Default.Find(cashier.Script!, again.Name)!.FullName);
        Assert.True(again.IsCounter(15, 5) && again.IsWalkable(15, 6));
        member.Party.Add(lead);
        member.Money = 20_000;
        member.Answers.Enqueue(0);
        Run(cashier.Script!, again.Name, member, cashier);
        Assert.True(RibbonRules.Has(lead, RibbonRules.Gorgeous));
        Assert.Equal(10_000, member.Money);

        // The lady has a word for a boy and another for a girl, and the lift in the back wall leads into its car (it was
        // shut until plan 01 · M11, part c built the floor above: NorthInteriorTests)
        var lady = Assert.Single(again.Everyone, n => n.Key == "lady");
        string Says(PlayerLook look) => Run("Lady", again.Name, new HeadlessScriptHost { Map = again, PlayerLook = look }, lady).Transcript.Last().Text;
        Assert.NotEqual(Says(PlayerLook.Boy), Says(PlayerLook.Girl));
        Assert.Equal("ResortAreaRibbonSyndicateElevator", again.GetWarpAt(5, 2)?.TargetMap);
        Assert.Equal(2, again.Everyone.Count(n => n.IsPokemon));
    }

    // ------------------------------------------------------------------ people, trainers, items, wild Pokémon

    [Fact]
    public void TheStorysPeopleWaitForTheStory()
    {
        // Whoever the original's scenes bring on (Buck, Looker, Team Galactic and Charon, Heatran, the Magma Stone,
        // Regigigas, Cresselia and her Lunar Wing, Darkrai, Shaymin, the Frontier's reporter) is in the area's file
        // under their own flag and not on the map: plan 02 · S15 and the event gifts' session place them
        var waiting = new (string Area, string Id, string Flag)[]
        {
            ("stark_mountain_outside", "looker", "FLAG_HIDE_STARK_MOUNTAIN_OUTSIDE_LOOKER"), ("stark_mountain_outside", "buck", "FLAG_HIDE_STARK_MOUNTAIN_OUTSIDE_BUCK"),
            ("stark_mountain_outside", "grunt_m_1", "FLAG_HIDE_STARK_MOUNTAIN_OUTSIDE_GRUNTS"), ("stark_mountain_outside", "grunt_m_2", "FLAG_HIDE_STARK_MOUNTAIN_OUTSIDE_GRUNTS"),
            ("stark_mountain_room_1", "mars", "FLAG_HIDE_STARK_MOUNTAIN_ROOM_1_TEAM_GALACTIC"), ("stark_mountain_room_1", "jupiter", "FLAG_HIDE_STARK_MOUNTAIN_ROOM_1_TEAM_GALACTIC"),
            ("stark_mountain_room_1", "charon", "FLAG_HIDE_STARK_MOUNTAIN_ROOM_1_TEAM_GALACTIC"), ("stark_mountain_room_2", "buck", "FLAG_HIDE_STARK_MOUNTAIN_ROOM_2_BUCK"),
            ("stark_mountain_room_3", "heatran", "FLAG_HIDE_STARK_MOUNTAIN_ROOM_3_HEATRAN"), ("stark_mountain_room_3", "magma_stone", "FLAG_HIDE_STARK_MOUNTAIN_ROOM_3_MAGMA_STONE"),
            ("stark_mountain_room_3", "charon", "FLAG_HIDE_STARK_MOUNTAIN_ROOM_3_TEAM_GALACTIC"), ("stark_mountain_room_3", "croagunk", "FLAG_HIDE_STARK_MOUNTAIN_ROOM_3_CROAGUNK"),
            ("stark_mountain_room_3", "rock_smash_looker", "FLAG_HIDE_STARK_MOUNTAIN_ROOM_3_ROCK_SMASH_LOOKER"),
            ("snowpoint_temple_b5f", "regigigas", "FLAG_HIDE_SNOWPOINT_TEMPLE_B5F_REGIGIGAS"),
            ("fullmoon_island_forest", "cresselia", "FLAG_HIDE_FULLMOON_ISLAND_FOREST_CRESSELIA"), ("fullmoon_island_forest", "item_lunar_wing", "FLAG_OBTAINED_FULLMOON_ISLAND_FOREST_LUNAR_WING"),
            ("newmoon_island_forest", "darkrai", "FLAG_HIDE_NEWMOON_ISLAND_FOREST_DARKRAI"), ("flower_paradise", "shaymin", "FLAG_HIDE_FLOWER_PARADISE_SHAYMIN"),
            ("battle_frontier", "reporter", "FLAG_HIDE_BATTLE_FRONTIER_REPORTER")
        };
        foreach (var (area, id, flag) in waiting)
        {
            var on = BuiltMaps.Value[Sinnoh.MapOf(area)!.Name];
            Assert.Equal(flag, Assert.Single(Sinnoh.Area(area)!.Objects, o => o.Id == id).HiddenBy);
            Assert.DoesNotContain(PeopleOf(on, area), n => n.Key == id);
        }
        // Stark Mountain's last room is the story's from end to end: nobody stands in it, and nothing lies there
        Assert.Empty(BuiltMaps.Value["StarkMountainRoom3"].Everyone);
        Assert.Equal(new[] { "magma_stone", "rock_smash_looker" }, Sinnoh.Overlay("stark_mountain_room_3")!.HeldBack);
        Assert.Equal(new[] { "item_lunar_wing" }, Sinnoh.Overlay("fullmoon_island_forest")!.HeldBack);
        // The triggers their scenes hang on are in the files too, at the original's tiles, and do nothing yet
        Assert.Equal((752, 235, "VAR_STARK_MOUNTAIN_OUTSIDE_STATE"), Sinnoh.Area("stark_mountain_outside")!.Triggers.Select(t => (t.X, t.Z, t.Variable)).Single());
        Assert.Equal(new[] { (42, 69), (42, 70), (47, 3) }, Sinnoh.Area("stark_mountain_room_2")!.Triggers.Select(t => (t.X, t.Z)));
        Assert.DoesNotContain(Overworld.Triggers, t => AreaKey(Overworld, t.X, t.Y) == "stark_mountain_outside");
        Assert.Empty(BuiltMaps.Value["StarkMountainRoom2"].Triggers);
    }

    [Fact]
    public void StarkMountainsTrainersItemsAndWildPokemonAreTheOriginals()
    {
        var outside = Overworld;
        var first = BuiltMaps.Value["StarkMountainRoom1"];
        var second = BuiltMaps.Value["StarkMountainRoom2"];
        NPC Trainer(Map on, string key) => Assert.Single(on.Everyone, n => n.Key == key && n.IsTrainer);
        string Team(NPC npc) => string.Join(", ", npc.TrainerData!.Party.Members.Select(p => $"{p.Species.Name} {p.Level}"));

        // res/trainers/data of the decompilation, by way of trainers.json
        Assert.Equal("Dragonite 60", Team(Trainer(outside, "dragon_tamer_darien")));
        Assert.Equal("Raticate 57, Drifblim 58, Shiftry 59", Team(Trainer(second, "veteran_harlan")));
        Assert.Equal("Primeape 58, Electabuzz 58, Banette 59", Team(Trainer(second, "ace_trainer_keenan")));
        Assert.Equal("Breloom 58, Toxicroak 58", Team(Trainer(second, "black_belt_ray")));
        Assert.Equal(("Harlan", "Veteran", 5), (Trainer(second, "veteran_harlan").Name, Trainer(second, "veteran_harlan").TrainerData!.TrainerClass, Trainer(second, "veteran_harlan").TrainerData!.SightRange));

        // Every trainer the original stands on the mountain is in the game, with a team and two lines of ours
        int trainers = 0;
        foreach (var (key, on) in new[] { ("stark_mountain_outside", outside), ("stark_mountain_room_1", first), ("stark_mountain_room_2", second) })
            foreach (var o in Sinnoh.Area(key)!.Objects.Where(o => o.Trainer != null))
            {
                var npc = Assert.Single(on.Everyone, n => n.Key == o.Id && (n.GridX, n.GridY) == (o.X, o.Z));
                Assert.True(npc.IsTrainer, $"{key}: {o.Id} doesn't battle");
                Assert.All(npc.TrainerData!.Party.Members, p => Assert.InRange(p.Level, 55, 62));
                Assert.False(string.IsNullOrWhiteSpace(npc.TrainerData.DialogueBefore) || string.IsNullOrWhiteSpace(npc.TrainerData.DialogueAfter), $"{key}: {o.Id} has nothing to say");
                Assert.StartsWith(npc.TrainerData.TrainerClass + " " + npc.Name + ":", npc.TrainerData.DialogueBefore);
                trainers++;
            }
        Assert.True(trainers >= 17, $"only {trainers} trainers on Stark Mountain");
        // Where the original stands two face to face, they see the player together
        Assert.Equal(2, Math.Abs(Trainer(second, "ace_trainer_skylar").GridX - Trainer(second, "ace_trainer_natasha").GridX));

        // What lies about, in balls and in the ground
        NPC Ball(Map on, string key) => Assert.Single(on.Everyone, n => n.Key == key && n.IsItemBall);
        Assert.Equal("Life Orb", Ball(outside, "item_life_orb").Item);
        Assert.Equal("FLAG_OBTAINED_STARK_MOUNTAIN_OUTSIDE_MAX_ELIXIR", Ball(outside, "item_max_elixir").HiddenBy);
        Assert.Equal(new[] { "Escape Rope", "Full Heal", "Full Restore", "PP Up" }, first.Everyone.Where(n => n.IsItemBall).Select(n => n.Item).OrderBy(i => i));
        Assert.Equal(8, second.Everyone.Count(n => n.IsItemBall));
        Assert.Equal("Iron Ball", Ball(second, "item_iron_ball").Item);
        Assert.StartsWith("TM50", Ball(second, "item_tm50").Item);
        Assert.Equal("Star Piece", Assert.Single(first.HiddenItems.Values).Item);
        Assert.Contains("Flame Plate", second.HiddenItems.Values.Select(h => h.Item));
        Assert.Equal(7, second.HiddenItems.Count);
        // Boulders for Strength and rocks for Rock Smash stand in the way inside
        Assert.Equal((7, 1), (first.Everyone.Count(n => n.NpcType == NPC.BoulderType), first.Everyone.Count(n => n.NpcType == NPC.CrackedRockType)));
        Assert.Equal((3, 3), (second.Everyone.Count(n => n.NpcType == NPC.BoulderType), second.Everyone.Count(n => n.NpcType == NPC.CrackedRockType)));

        // The grass outside and the rock inside have Platinum's tables (encounters_stark_mountain_*.json)
        string[] Land(Map on, string key) => on.FindArea(key)!.WildEncounters.Select(e => e.SpeciesName).Distinct().ToArray();
        Assert.Contains("Camerupt", Land(outside, "stark_mountain_outside"));
        Assert.Contains("Skarmory", Land(outside, "stark_mountain_outside"));
        Assert.Contains("Magcargo", Land(first, "stark_mountain_room_1"));
        Assert.Contains("Koffing", Land(second, "stark_mountain_room_2"));
        Assert.Empty(Land(BuiltMaps.Value["StarkMountainRoom3"], "stark_mountain_room_3"));
        Assert.All(new[] { (outside, "stark_mountain_outside"), (first, "stark_mountain_room_1"), (second, "stark_mountain_room_2") }, t =>
        {
            var area = t.Item1.FindArea(t.Item2)!;
            Assert.Equal(WorldAreaFile.LandSlotWeights.Length, area.WildEncounters.Count);
            Assert.All(area.WildEncounters, e => Assert.InRange(e.MinLevel, 51, 55));
        });

        // The way up: Route 227's road, the mountain's mouth, and three rooms one behind the other
        var mouth = Assert.Single(outside.Warps, w => AreaKey(outside, w.SourceX, w.SourceY) == "stark_mountain_outside");
        Assert.Equal((750, 231, "StarkMountainRoom1"), (mouth.SourceX, mouth.SourceY, mouth.TargetMap));
        Assert.Equal("StarkMountainRoom2", Assert.Single(first.Warps, w => w.TargetMap != "Sinnoh").TargetMap);
        Assert.Contains(second.Warps, w => w.TargetMap == "StarkMountainRoom3");
        Assert.Contains("Stark Mountain", outside.Signboards[(748, 232)]);
        var story = new StoryState();
        Run(ScriptLibrary.OnEnter, "stark_mountain_outside", new HeadlessScriptHost(story));
        Run(ScriptLibrary.OnEnter, "stark_mountain_room_1", new HeadlessScriptHost(story));
        Assert.True(story.Has("FLAG_FIRST_ARRIVAL_STARK_MOUNTAIN_EXTERIOR") && story.Has("FLAG_FIRST_ARRIVAL_STARK_MOUNTAIN_INTERIOR"));
    }

    // ------------------------------------------------------------------ the air, the light and the camera

    [Fact]
    public void AVolcanoAndATempleOfIceHaveLightOfTheirOwn()
    {
        var outside = Overworld;
        Assert.Equal(AreaClimate.Volcanic, outside.FindArea("stark_mountain_outside")!.Climate);
        Assert.Equal(FieldWeather.Ash, outside.WeatherAt(750, 235));
        Assert.All(Insides.Take(3), i => Assert.Equal(AreaClimate.Volcanic, BuiltMaps.Value[i.Map].Areas.Single(a => a.Open).Climate));
        Assert.All(Insides.Skip(3).Take(6), i => Assert.Equal(AreaClimate.Frozen, BuiltMaps.Value[i.Map].Areas.Single(a => a.Open).Climate));
        // Nowhere else: the islands, the path, the Frontier and Route 227 under its ash are lit as the hour has them
        foreach (string key in new[] { "fullmoon_island", "newmoon_island", "seabreak_path", "flower_paradise", "route_227", "snowpoint_city", "twinleaf_town" })
            Assert.Equal(AreaClimate.Temperate, outside.FindArea(key)!.Climate);
        Assert.Equal(AreaClimate.Temperate, BuiltMaps.Value["BattleFrontier"].Areas.Single(a => a.Open).Climate);

        // The style guide's numbers ("Climates"). In the open: a dimmer, redder sun, an ashen haze brought in, and what
        // burns glowing by day
        var day = ArtLook.FieldRigFor(PokemonPlatinumEngine.Core.TimeOfDay.Day);
        var ash = ArtLook.InClimate(day, AreaClimate.Volcanic, cave: false);
        Assert.Equal(day.Light.SunColor * 0.82f * new Vector3(1.06f, 0.93f, 0.82f), ash.Light.SunColor);
        Assert.True(ash.FogAmount > day.FogAmount && ash.FogFar <= 66f && ash.FogNear <= 40f);
        Assert.Equal((ArtLook.VolcanicGlow, 0f), (ash.LampGlow, ash.CloudShade));
        Assert.Equal(0.75f, ArtLook.VolcanicGlow);
        Assert.Equal(day.Light.SunDirection, ash.Light.SunDirection);
        // It glows no brighter after dark, and in lava's own orange, not lamplight
        var night = ArtLook.InClimate(ArtLook.FieldRigFor(PokemonPlatinumEngine.Core.TimeOfDay.Night), AreaClimate.Volcanic, cave: false);
        Assert.Equal((ArtLook.VolcanicGlow, ArtLook.LavaGlow), (night.LampGlow, night.GlowColor));
        Assert.Equal(ArtLook.LavaGlow, ash.GlowColor);
        Assert.Equal(new Vector3(1f, 0.58f, 0.2f), ArtLook.LavaGlow);
        Assert.NotEqual(ArtLook.Lamplight, ArtLook.LavaGlow);

        // Inside the mountain: lit from below by its own heat, redder than it is bright
        var cave = ArtLook.FieldRig(12f, BuiltMaps.Value["OreburghGate1F"]);
        var hot = ArtLook.InClimate(cave, AreaClimate.Volcanic, cave: true);
        Assert.Equal((new Vector3(0.66f, 0.47f, 0.34f), new Vector3(0.54f, 0.3f, 0.2f)), (hot.Light.SunColor, hot.Light.GroundAmbient));
        Assert.True(hot.Light.GroundAmbient.X > hot.Light.GroundAmbient.Z * 2f);
        Assert.Equal((30, 16, 18), (hot.Background.R, hot.Background.G, hot.Background.B));
        Assert.Equal((ArtLook.VolcanicGlow, ArtLook.LavaGlow), (hot.LampGlow, hot.GlowColor));
        // The temple: a cold key, a blue bounce, and bloom only on what is brighter than its ice
        var cold = ArtLook.InClimate(cave, AreaClimate.Frozen, cave: true);
        Assert.Equal(new Vector3(0.56f, 0.6f, 0.68f), cold.Light.SunColor);
        Assert.True(cold.Light.GroundAmbient.Z > cold.Light.GroundAmbient.X && cold.Post.BloomThreshold >= 0.95f && cold.Post.Saturation < cave.Post.Saturation);
        Assert.Equal((16, 22, 36), (cold.Background.R, cold.Background.G, cold.Background.B));
        // Frozen country in the open is snow country, and a mild place is left alone
        Assert.Equal(ArtLook.Snowbound(day), ArtLook.InClimate(day, AreaClimate.Frozen, cave: false));
        Assert.Equal(day, ArtLook.InClimate(day, AreaClimate.Temperate, cave: false));
        Assert.Equal(cave, ArtLook.InClimate(cave, AreaClimate.Temperate, cave: true));

        // Stark Mountain's great room has the original's camera of its own, steeper and from further off than a cave's
        var great = BuiltMaps.Value["StarkMountainRoom2"];
        Assert.Equal(FieldCamera.StarkMountain, great.Camera);
        Assert.Equal(new FieldView(70.47f, 19.7f, 662.922f / 16f), MapScene.ViewOf(great));
        Assert.Equal(FieldCamera.Cave, BuiltMaps.Value["StarkMountainRoom1"].Camera);
        Assert.True(MapScene.ViewOf(great).PitchDeg > MapScene.ViewOf(BuiltMaps.Value["StarkMountainRoom1"].Camera == FieldCamera.Cave ? BuiltMaps.Value["StarkMountainRoom1"] : great).PitchDeg);
    }

    [Fact]
    public void HighGroundInTheNorthDoesNotWidenTheViewToTheSouth()
    {
        // The Battle Frontier's plaza lies sunk between the Tower's terrace in the north and the gate's in the south.
        // Each edge of the view asks only the ground beyond itself: asked as one, the terrace's height widened the view
        // to the south and drew chunks that lay almost under the camera
        var grounds = BuiltMaps.Value["BattleFrontier"];
        Assert.True(grounds.HasRelief);
        var renderer = new WorldRenderer(new RenderContext(3840, 2160));
        var sun = ArtLook.FieldRigFor(PokemonPlatinumEngine.Core.TimeOfDay.Day).Light.SunDirection;
        float perTile = 1f / MathF.Tan(MapScene.ViewOf(grounds).PitchDeg * MathF.PI / 180f);
        int caught = 0;
        var seen = new List<string>();
        foreach (int row in new[] { 30, 36, 40, 41, 44, 48 })
        {
            float ground = Relief.At(grounds, 48.5f, row + 0.5f);
            GroundRect At(float level) => renderer.VisibleRects(grounds, new Vector3(48.5f, level, row + 0.5f), level, sun).View;
            // With the ground said to lie above everything nothing rises into the view from the south, and with it
            // below everything nothing shows more of itself in the north: the view over flat ground, edge by edge
            float flatSouth = At(1000f).MaxZ, flatNorth = At(-1000f).MinZ;
            var view = At(ground);
            var (low, northHigh) = Relief.Range(grounds, (int)view.MinX, (int)flatNorth - 10, (int)view.MaxX + 1, (int)flatNorth + 1);
            var (_, high) = Relief.Range(grounds, (int)view.MinX, (int)flatSouth, (int)view.MaxX + 1, (int)flatSouth + 10);
            Assert.Equal(flatSouth + MathF.Max(0f, high - ground) * perTile, view.MaxZ, 3);
            Assert.Equal(flatNorth - MathF.Max(0f, ground - low) * perTile, view.MinZ, 3);
            seen.Add($"row {row}: ground {ground:0.##}, north {low:0.##} to {northHigh:0.##} (rows {(int)flatNorth - 10} to {(int)flatNorth}), south up to {high:0.##} (rows {(int)flatSouth} to {(int)flatSouth + 10})");
            // Where the north stands higher than anything to the south, its height is not what moves the south edge
            if (northHigh > high + 0.5f)
            {
                Assert.True(view.MaxZ < flatSouth + (northHigh - ground) * perTile - 0.1f, $"row {row}: the terrace widened the view to the south");
                caught++;
            }
        }
        Assert.True(caught >= 3, "too few rows of the plaza have the terrace to their north and lower ground to their south: " + string.Join("; ", seen));
    }

    // ------------------------------------------------------------------ what stands there

    [Fact]
    public void NoModelIsAStandInAnyMoreAndTheLastNineHaveLooksOfTheirOwn()
    {
        Assert.DoesNotContain(WorldModels.All, m => m.StandInUntil != null);
        foreach (var (model, prop) in new[] { ("d16_o01", PropType.Lava), ("d16_o02", PropType.Crater), ("d20_o01", PropType.Column), ("d20_o02", PropType.Colossus) })
            Assert.Equal((ModelRole.Scenery, prop), (WorldModels.Of(model)!.Role, WorldModels.Of(model)!.Prop));
        foreach (var (model, kind) in new[]
                 {
                     ("d31_s01", BuildingKind.Tower), ("d32_s01", BuildingKind.Hall), ("d32_s02", BuildingKind.Hall), ("d32_s03", BuildingKind.Hall),
                     ("d32_s04", BuildingKind.Hall), ("d32_s05", BuildingKind.Gate)
                 })
            Assert.Equal((ModelRole.Building, kind), (WorldModels.Of(model)!.Role, WorldModels.Of(model)!.Kind));
        // The four halls block only their outline in the original: their insides are theirs here
        Assert.All(new[] { "d32_s01", "d32_s02", "d32_s03", "d32_s04" }, m => Assert.True(WorldModels.Of(m)!.Hollow));
        Assert.False(WorldModels.Of("d31_s01")!.Hollow);

        // Stark Mountain: lakes of lava lie level in the rock at its foot, and the crater smokes over them
        var outside = Overworld;
        var lava = outside.Props.Where(p => p.Type == PropType.Lava).ToList();
        Assert.Equal(2, lava.Count);
        Assert.All(lava, l =>
        {
            Assert.Equal(("stark_mountain_outside", "d16_o01"), (AreaKey(outside, l.X, l.Y), l.Model));
            var heights = Enumerable.Range(0, l.Width * l.Depth).Select(i => outside.HeightAt(l.X + i % l.Width, l.Y + i / l.Width)).Distinct().ToList();
            Assert.Single(heights);
            Assert.True(outside.IsSolid(l.X + l.Width / 2, l.Y + l.Depth / 2), "lava can be walked into");
        });
        var crater = Assert.Single(outside.Props, p => p.Type == PropType.Crater);
        Assert.Equal("d16_o02", crater.Model);
        Assert.True(crater.Width >= 8 && crater.Depth >= 8, "the crater is the mountain's top");

        // The Battle Frontier: the Exchange Service Corner's booths, the model of the grounds and the two statues
        var grounds = BuiltMaps.Value["BattleFrontier"];
        Assert.Equal(new[] { (44, 36), (44, 42), (51, 42) }, grounds.Props.Where(p => p.Type == PropType.Booth).Select(p => (p.X, p.Y)).OrderBy(p => p));
        var miniature = Assert.Single(grounds.Props, p => p.Type == PropType.ScaleModel);
        Assert.Equal((50, 36, 4, 4), (miniature.X, miniature.Y, miniature.Width, miniature.Depth));
        Assert.Equal(new[] { (46, 48), (50, 48) }, grounds.Props.Where(p => p.Type == PropType.TrainerStatue).Select(p => (p.X, p.Y)).OrderBy(p => p));
        Assert.All(grounds.Props.Where(p => p.Type is PropType.ScaleModel or PropType.TrainerStatue), p =>
            Assert.All(Enumerable.Range(0, p.Width * p.Depth), i => Assert.True(grounds.IsSolid(p.X + i % p.Width, p.Y + i / p.Width))));
        // Its ground is paving, lawn and water, not the bare rock unknown textures leave
        var walked = Walked.Value[grounds];
        Assert.True(walked.Count > 1000, $"only {walked.Count} tiles of the grounds are walked");
        Assert.True(walked.Count(t => grounds.GetGroundTile(t.X, t.Y) == TileType.Paving) > walked.Count / 2, "the grounds are paved");
        Assert.DoesNotContain(walked, t => grounds.GetGroundTile(t.X, t.Y) is TileType.CaveWall or TileType.Tree or TileType.Water);
    }

    [Fact]
    public void TheFrontiersHallsStandBehindTheirForecourts()
    {
        var grounds = BuiltMaps.Value["BattleFrontier"];
        var buildings = grounds.PlacedBuildings!;
        Architecture town = grounds.ArchitectureAt(48, 40);
        foreach (var (model, sign, storeys) in new[] { ("d32_s01", "CASTLE", 3), ("d32_s02", "HALL", 2), ("d32_s03", "FACTORY", 3), ("d32_s04", "ARCADE", 3) })
        {
            // One building proper, with its name and its doors in its front, north of the passage to them
            var main = Assert.Single(buildings, b => b.Model == model && !b.Annex);
            Assert.Equal((BuildingKind.Hall, sign, storeys, false), (main.Kind, main.Sign, main.Storeys, main.Forecourt));
            Assert.NotEmpty(main.Doors);
            Assert.True(main.Width >= 10 && main.Depth >= 3, $"{sign} is {main.Width} by {main.Depth}");
            // What it blocks from the passage south is low: a forecourt nobody is hidden behind
            var courts = buildings.Where(b => b.Model == model && b.Forecourt).ToList();
            Assert.NotEmpty(courts);
            Assert.All(courts, c =>
            {
                Assert.True(c.Annex && c.Y0 > main.Y1, $"{sign}: a forecourt at ({c.X0},{c.Y0}) isn't south of the building");
                for (int y = c.Y0; y <= c.Y1; y++)
                    for (int x = c.X0; x <= c.X1; x++)
                        Assert.True(grounds.IsSolid(x, y));
            });
            Assert.True(courts.Sum(c => c.Width * c.Depth) >= 40, $"{sign}'s forecourt is a few tiles");
            // The passage along its front is walked, up to the doors that are shut
            int row = main.Y1 + 1;
            Assert.Contains(Walked.Value[grounds], t => t.Y == row && t.X >= main.X0 - 2 && t.X <= main.X1 + 2);
            // Nothing of it is the ring of small blocks the outline alone would give
            Assert.True(buildings.Count(b => b.Model == model && !b.Forecourt) <= 2, $"{sign} is in pieces");
        }
        Assert.Equal(BuildingModels.ForecourtHeight, 14);
        Assert.True(BuildingModels.ForecourtHeight < BuildingArt.Bay, "a forecourt is lower than whoever walks behind it");

        // Each has a look of its own (style guide, "Buildings")
        BuildingStyle Style(string model) => BuildingArt.StyleOf(buildings.Single(b => b.Model == model && !b.Annex), town);
        var castle = Style("d32_s01");
        Assert.Equal((WallKind.Stone, RoofShape.Flat, BuildingArt.CastleViolet), (castle.Wall, castle.Roof, castle.Accent));
        Assert.True(castle.Gear.HasFlag(RoofGear.Battlements) && castle.Gear.HasFlag(RoofGear.Turrets));
        var hall = Style("d32_s02");
        Assert.Equal((WallKind.PaleStone, RoofShape.Hip, BuildingArt.HallGold, WindowKind.Arched), (hall.Wall, hall.Roof, hall.RoofColor, hall.Window));
        var factory = Style("d32_s03");
        Assert.Equal((WallKind.DarkPanel, RoofShape.Flat, BuildingArt.FactoryOrange), (factory.Wall, factory.Roof, factory.Accent));
        Assert.True(factory.Gear.HasFlag(RoofGear.Stack));
        var arcade = Style("d32_s04");
        Assert.Equal((BuildingArt.ArcadeMagenta, WindowKind.Shop), (arcade.Accent, arcade.Window));
        Assert.True(arcade.Gear.HasFlag(RoofGear.Globe));
        Assert.Equal(4, new[] { castle, hall, factory, arcade }.Select(s => (s.Wall, s.Roof, s.Accent)).Distinct().Count());

        // The Battle Tower: a ground stage on its terrace, four stages narrowing above it, a beacon on top
        var tower = Assert.Single(buildings, b => b.Model == "d31_s01" && !b.Annex);
        Assert.Equal((BuildingKind.Tower, "TOWER", 2), (tower.Kind, tower.Sign, tower.Doors.Count));
        var tall = BuildingArt.StyleOf(tower, town);
        Assert.Equal((288, 4, BuildingArt.FrontierBlue), (tall.Shaft, tall.StageCount, tall.Accent));
        Assert.True(tall.Gear.HasFlag(RoofGear.Beacon));
        // The lodges either side of the gate
        Assert.Equal(2, buildings.Count(b => b.Model == "d32_s05" && b.Kind == BuildingKind.Gate));
        Assert.Equal(BuildingArt.FrontierBlue, BuildingArt.StyleOf(buildings.First(b => b.Model == "d32_s05"), town).RoofColor);
    }

    [Fact]
    public void TheNewPropsArePaintedWholeAndWhatBurnsIsMarkedAsLit()
    {
        static IEnumerable<Raylib_cs.Color> Texels(PixelCanvas c) => Enumerable.Range(0, c.Width * c.Height).Select(i => c.Get(i % c.Width, i / c.Width));

        // Lava: every texel painted, plates of dark crust with the cracks between them marked to glow (alpha under
        // 255 and over a half), and it moves
        var lava = new PixelCanvas(7 * 32, 7 * 32);
        Landmarks.PaintLava(lava, 0);
        Assert.All(Texels(lava), t => Assert.True(t.A > 128));
        var lit = Texels(lava).Where(t => t.A < 255).ToList();
        Assert.True(lit.Count > lava.Width * lava.Height / 20, $"only {lit.Count} texels of lava glow");
        Assert.True(lit.Count < lava.Width * lava.Height / 2, "the crust is dark");
        Assert.All(lit, t => Assert.True(t.R > t.B * 2, "what glows is red and yellow"));
        // Kept under white, or it would bloom to a blank
        Assert.DoesNotContain(Texels(lava), t => t.R > 250 && t.G > 240 && t.B > 200);
        var later = new PixelCanvas(lava.Width, lava.Height);
        Landmarks.PaintLava(later, 2);
        Assert.True(Texels(lava).Zip(Texels(later)).Count(p => !p.First.Equals(p.Second)) > 500, "lava moves from frame to frame");

        // The crater's rock is whole, and its throat glows where its sides don't
        var face = new PixelCanvas(96, 48);
        Landmarks.PaintBasaltFace(face);
        Assert.All(Texels(face), t => Assert.True(t.A > 128));
        var top = new PixelCanvas(96, 96);
        Landmarks.PaintBasaltTop(top, crater: true);
        Assert.Contains(Texels(top), t => t.A is > 128 and < 255);
        var tier = new PixelCanvas(96, 96);
        Landmarks.PaintBasaltTop(tier, crater: false);
        Assert.All(Texels(tier), t => Assert.True(t.A > 128));
        Assert.True(Texels(top).Count(t => t.A < 255) > Texels(tier).Count(t => t.A < 255), "the throat glows where a lower tier doesn't");
        var smoke = new PixelCanvas(64, 96);
        Landmarks.PaintSmoke(smoke, 0);
        Assert.Contains(Texels(smoke), t => t.A == 0);
        Assert.Contains(Texels(smoke), t => t.A == 255);

        // A booth's wall, awning and counter; the model of the grounds; a statue; the colossus
        var back = new PixelCanvas(96, Landmarks.BoothBack);
        Landmarks.PaintBoothBack(back);
        var counter = new PixelCanvas(96, Landmarks.BoothCounter);
        Landmarks.PaintCounter(counter, top: false, board: true);
        var awning = new PixelCanvas(96, 24);
        Landmarks.PaintAwning(awning, top: true);
        foreach (var canvas in new[] { back, counter, awning })
            Assert.All(Texels(canvas), t => Assert.True(t.A > 128));
        Assert.True(Texels(awning).Select(t => (t.R, t.G, t.B)).Distinct().Count() >= 4, "the awning is striped");
        var miniature = new PixelCanvas(4 * 32 - 16, 4 * 32 - 16);
        Landmarks.PaintMiniature(miniature);
        Assert.True(Texels(miniature).Select(t => (t.R, t.G, t.B)).Distinct().Count() >= 8, "the model shows the tower, the halls and the plaza");
        foreach (var paint in new Action<PixelCanvas>[] { Landmarks.PaintTrainerStatue, Landmarks.PaintColossus })
        {
            var sprite = new PixelCanvas(64, 96);
            paint(sprite);
            Assert.Contains(Texels(sprite), t => t.A == 0);
            Assert.True(Texels(sprite).Count(t => t.A == 255) > 600, "a figure fills its card");
        }

        // A forecourt: slabs inside a kerb, a course of cut stone on its face
        var court = new PixelCanvas(5 * BuildingArt.Bay, 3 * BuildingArt.Bay);
        BuildingArt.PaintForecourtTop(court);
        var kerb = new PixelCanvas(5 * BuildingArt.Bay, BuildingModels.ForecourtHeight);
        BuildingArt.PaintForecourtFace(kerb);
        foreach (var canvas in new[] { court, kerb })
            Assert.All(Texels(canvas), t => Assert.Equal(255, t.A));
        Assert.NotEqual(court.Get(1, 1), court.Get(24, 24));
    }

    [Theory]
    [InlineData("bf_hashi01", TerrainCover.Paving)]
    [InlineData("bf_hashi02", TerrainCover.Fence)]
    [InlineData("flobby_yuka01", TerrainCover.Paving)]
    [InlineData("bf_symbol_a", TerrainCover.Paving)]
    [InlineData("bf_shiba", TerrainCover.Grass)]
    [InlineData("bf_hei01", TerrainCover.Fence)]
    [InlineData("flobby_kabe", TerrainCover.Fence)]
    [InlineData("bf_ueki01", TerrainCover.Broadleaf)]
    [InlineData("bf_shop01", TerrainCover.Building)]
    [InlineData("bf_zou", TerrainCover.Building)]
    [InlineData("bf_mini_a", TerrainCover.Building)]
    public void TheFrontiersTexturesAreSorted(string texture, TerrainCover expected)
    {
        Assert.Equal(expected, Cover.OfTexture(texture, out bool known));
        Assert.True(known);
    }
}
