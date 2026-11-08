using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumTests;

/// <summary>
/// Kalos's Gyms, its water and the Mamoswine ride (plan 23, after Z4b; docs/mechanics/rulings.md, "Kalos's Gyms, its water
/// and the Mamoswine ride"): the Prism Tower's quiz gates, Valerie's and Olympia's warp panels, Wulfric's turntables, the
/// water and rod tables of Kalos's coasts, rivers and lakes with the three rods and HM03 given, Route 17's drifts crossed
/// only on a lent Mamoswine, and the hordes and sky trainers of Kalos's routes.
/// </summary>
public class KalosGymsAndWaterTests
{
    private static readonly Lazy<World> LoadedWorld = new(() => World.LoadAll().Single(w => w.Index.Region == "Kalos"));
    private static readonly Lazy<Dictionary<string, Map>> BuiltMaps = new(() => LoadedWorld.Value.BuildMaps().ToDictionary(m => m.Name));
    private static readonly Lazy<Func<string, Map>> Rooms = new(MapDatabase.Fresh);
    private static readonly Lazy<Dictionary<Map, HashSet<(int X, int Y)>>> Walked =
        new(() => WorldWalk.From(MapNamed, RegionDatabase.Get(RegionDatabase.Kalos)!.Start!));

    private static World Kalos => LoadedWorld.Value;
    private static Map Overworld => BuiltMaps.Value["Kalos"];
    private static Map MapNamed(string name) => BuiltMaps.Value.TryGetValue(name, out var map) ? map : Rooms.Value(name);

    // ------------------------------------------------------------------ the Gyms

    [Fact]
    public void ThePrismTowersThreeGatesOpenByTheirQuizzesAndAWrongAnswerFightsTheFloorsTrainerFirst()
    {
        var hall = Rooms.Value("LumioseCity_PrismTowerGym");
        var gates = hall.Everyone.Where(n => n.NpcType == NPC.ElectricGateType).OrderByDescending(n => n.GridY).ToList();
        Assert.Equal(3, gates.Count);
        Assert.Equal(new[] { "FLAG_KALOS_PRISM_GATE_1", "FLAG_KALOS_PRISM_GATE_2", "FLAG_KALOS_PRISM_GATE_3" }, gates.Select(g => g.HiddenBy));
        // Each gate stands in the one gap of a wall across the hall
        foreach (var g in gates)
            for (int x = 1; x < hall.Width - 1; x++)
                if (x != g.GridX) Assert.True(hall.IsSolid(x, g.GridY), $"the wall of gate {g.HiddenBy} is open at {x}");

        // A right answer opens the first gate with no battle
        var story = new StoryState();
        var right = Play(story, "LumioseCity_PrismTowerGym", "Quiz1", hall, hall.Everyone.First(n => n.Key == "quiz_host_1"), answers: new[] { 0 });
        Assert.True(story.Has("FLAG_KALOS_PRISM_GATE_1"));
        Assert.DoesNotContain(right.Log, l => l.StartsWith("battle", StringComparison.Ordinal));
        // A wrong one at the second sets Hugo on the player, and the gate opens after
        var wrong = Play(story, "LumioseCity_PrismTowerGym", "Quiz2", hall, hall.Everyone.First(n => n.Key == "quiz_host_2"), answers: new[] { 0 });
        Assert.Contains(wrong.Log, l => l.StartsWith("battle kalos_schoolboy_hugo", StringComparison.Ordinal));
        Assert.True(story.Has("FLAG_KALOS_PRISM_GATE_2"));
        // Hugo once beaten, a wrong answer at the third still fights Adèle, who isn't
        var third = Play(story, "LumioseCity_PrismTowerGym", "Quiz3", hall, hall.Everyone.First(n => n.Key == "quiz_host_3"), answers: new[] { 1 });
        Assert.Contains(third.Log, l => l.StartsWith("battle kalos_schoolgirl_adele", StringComparison.Ordinal));
        Assert.True(story.Has("FLAG_KALOS_PRISM_GATE_3"));
        // With every gate open the walk of the whole game stands before Clemont
        Assert.Contains((6, 5), Walked.Value[MapNamed("LumioseCity_PrismTowerGym")]);
    }

    [Theory]
    [InlineData("LaverreCity_Gym", 6, 5)]
    [InlineData("AnistarCity_Gym", 6, 5)]
    public void TheWarpPanelHallsAreCrossedOnlyByTheirPanelsToTheLeader(string room, int x, int y)
    {
        var hall = Rooms.Value(room);
        var panels = hall.Warps.Where(w => w.TargetMap == room).ToList();
        Assert.NotEmpty(panels);
        foreach (var w in panels)
        {
            Assert.Equal(TileBehavior.WarpPanel, hall.BehaviourAt(w.SourceX, w.SourceY));
            // Every panel leads to a panel that leads back
            Assert.Contains(panels, back => (back.SourceX, back.SourceY) == (w.TargetX, w.TargetY) && (back.TargetX, back.TargetY) == (w.SourceX, w.SourceY));
            Assert.Contains(hall.Props, p => p.Type == PropType.WarpPanel && (p.X, p.Y) == (w.SourceX, w.SourceY));
        }
        // From the door, walking alone, the Leader's tile before her is out of reach
        var door = hall.Warps.First(w => w.TargetMap != room);
        var onFoot = Reach(hall, (door.SourceX, door.SourceY - 1), panels: false);
        Assert.DoesNotContain((x, y), onFoot);
        // ...and the panels take the player there (the walk of the whole game too)
        Assert.Contains((x, y), Reach(hall, (door.SourceX, door.SourceY - 1), panels: true));
        Assert.Contains((x, y), Walked.Value[MapNamed(room)]);
    }

    [Fact]
    public void WulfricsTurntablesTurnByTheirHubsAndOnlyTurningThemCrossesTheHall()
    {
        var hall = Rooms.Value("SnowbelleCity_Gym");
        var puzzle = Assert.IsType<SnowbelleTurntables>(hall.Puzzle);
        puzzle.Reset();
        // Every tile a turntable decides about is open in the room's file, and every other tile of the dark is shut
        for (int y = 0; y < hall.Height; y++)
            for (int x = 0; x < hall.Width; x++)
                if (SnowbelleTurntables.Covers(x, y)) Assert.False(hall.IsSolid(x, y), $"{x},{y} is a turntable's and solid");
        // An arm not lying on a tile refuses a step onto it, and a hub turns a quarter as it is stepped onto
        var hub = SnowbelleTurntables.Tables[1];
        Assert.Equal(1, puzzle.States[1]);
        Assert.True(puzzle.Open(hub.X, hub.Y + 1));
        Assert.False(puzzle.Open(hub.X, hub.Y - 1));
        Assert.True(puzzle.Refuses(hall, hub.X, hub.Y - 1, 0f, false));
        puzzle.Crossed(hall, hub.X, hub.Y, Direction.Up);
        Assert.Equal(2, puzzle.States[1]);

        // A search of the hall, the player's tile and the five states: no way to the walkway under Wulfric without a turn,
        // and a way with turns, the itinerary's (Itineraries.KalosLastChapters)
        var start = (X: 6, Y: 16, States: SnowbelleTurntables.Tables.Select(t => t.Start).ToArray());
        Assert.Null(Search(hall, puzzle, start, turning: false));
        var way = Search(hall, puzzle, start, turning: true);
        Assert.NotNull(way);
        Assert.Equal(19, way!.Value);
        puzzle.Reset();
    }

    // ------------------------------------------------------------------ the water

    private static readonly string[] Watered =
    {
        "ambrette_town", "cyllage_city", "kalos_route_8", "kalos_route_9", "shalour_city", "azure_bay", "kiloude_city",
        "kalos_route_15", "kalos_route_16", "couriway_town", "laverre_city", "pokemon_village"
    };

    [Fact]
    public void KalossWatersHaveTheirRodsAndSurfTables()
    {
        foreach (string key in Watered)
        {
            var area = Kalos.Area(key)!;
            Assert.True(area.OldRod is { Count: > 0 } && area.GoodRod is { Count: > 0 } && area.SuperRod is { Count: > 0 }, $"{key} has no rods' tables");
            if (key != "laverre_city") Assert.True(area.Water is { Count: > 0 }, $"{key} has no water table");
            foreach (var slot in (area.Water ?? new()).Concat(area.OldRod!).Concat(area.GoodRod!).Concat(area.SuperRod!))
                Assert.True(PokemonDatabase.Get(slot.Species)?.DexNumbers.Keys.Any(k => k.StartsWith("kalos-", StringComparison.Ordinal)) == true, $"{key}: {slot.Species} is no Kalos species");
            var built = Overworld.FindArea(key)!;
            Assert.NotEmpty(built.RodEncounters[(int)FishingRod.Super]);
        }
    }

    [Fact]
    public void TheThreeRodsAndSurfAreGivenOnce()
    {
        var story = new StoryState();
        var bag = new Inventory();
        Play(story, "AmbretteTown_Aquarium", "OldRod", Rooms.Value("AmbretteTown_Aquarium"), Rooms.Value("AmbretteTown_Aquarium").Everyone.First(n => n.Key == "keeper"), bag: bag);
        Play(story, "CoumarineCity_EastHouse", "GoodRod", Rooms.Value("CoumarineCity_EastHouse"), Rooms.Value("CoumarineCity_EastHouse").Everyone.First(n => n.Key == "fisherman"), bag: bag);
        Play(story, "couriway_town", "SuperRod", Overworld, Overworld.Everyone.First(n => n.Key == "fisher" && Overworld.AreaAt(n.GridX, n.GridY)?.Key == "couriway_town"), bag: bag);
        // A save already past the tower's scene is given HM03 by the guru when spoken to again
        story.SetVar("VAR_KALOS_STORY", 22);
        Play(story, "ShalourCity_TowerOfMastery", "Guru", Rooms.Value("ShalourCity_TowerOfMastery"), null, bag: bag);
        foreach (string item in new[] { "Old Rod", "Good Rod", "Super Rod", "HM03" })
            Assert.Equal(1, bag.GetQuantity(ItemDatabase.Get(item)!));
        // And nobody gives twice
        Play(story, "AmbretteTown_Aquarium", "OldRod", Rooms.Value("AmbretteTown_Aquarium"), Rooms.Value("AmbretteTown_Aquarium").Everyone.First(n => n.Key == "keeper"), bag: bag);
        Play(story, "couriway_town", "SuperRod", Overworld, Overworld.Everyone.First(n => n.Key == "fisher" && Overworld.AreaAt(n.GridX, n.GridY)?.Key == "couriway_town"), bag: bag);
        Play(story, "ShalourCity_TowerOfMastery", "Guru", Rooms.Value("ShalourCity_TowerOfMastery"), null, bag: bag);
        foreach (string item in new[] { "Old Rod", "Super Rod", "HM03" })
            Assert.Equal(1, bag.GetQuantity(ItemDatabase.Get(item)!));
    }

    // ------------------------------------------------------------------ the Mamoswine ride

    [Fact]
    public void Route17sDriftsStopAWalkerAndAreRiddenThroughOnALentMamoswine()
    {
        var map = Overworld;
        // Three bands of drift cross the route from edge to edge
        foreach (int x in new[] { 538, 560, 582 })
            for (int z = 199; z <= 223; z++)
                if (!map.IsSolid(x, z)) Assert.Equal(TileBehavior.SnowDrift, map.BehaviourAt(x, z));
        var onFoot = new Walker(TravelMode.OnFoot, map.HeightAt(535, 206));
        var step = FieldMovement.Step(map, 535, 206, Direction.Right, onFoot);
        Assert.Equal(Obstacle.Drift, step.Obstacle);
        var riding = FieldMovement.Step(map, 535, 206, Direction.Right, onFoot with { Mode = TravelMode.Riding });
        Assert.True(riding.Moves);
        Assert.Equal(Pace.Walk, FieldMovement.Step(map, 537, 206, Direction.Right, onFoot with { Mode = TravelMode.Riding }).Pace);
        Assert.Equal(Obstacle.Drift, FieldMovement.Step(map, 535, 206, Direction.Right, onFoot with { Mode = TravelMode.Skating }).Obstacle);

        // The Mamoswine is lent on Route 17 alone, with no flag and no pager, in no other region or area
        var kalos = RegionDatabase.Get(RegionDatabase.Kalos)!;
        var story = new StoryState();
        Assert.True(RidePager.CanRide("Mamoswine", story, kalos, "kalos_route_17"));
        Assert.False(RidePager.CanRide("Mamoswine", story, kalos, "dendemille_town"));
        Assert.False(RidePager.CanRide("Mamoswine", story, RegionDatabase.Get(RegionDatabase.Alola)!, "kalos_route_17"));
        Assert.Null(RidePager.LandRideOf("Mamoswine"));

        // Spoken to, one carries the player; spoken to again from its back, it lets them down
        var mamoswine = map.Everyone.First(n => n.Key == "mamoswine_west");
        var host = Play(story, "kalos_route_17", "Mamoswine", map, mamoswine, tile: (mamoswine.GridX - 1, mamoswine.GridY));
        Assert.Equal("Mamoswine", host.Riding);
        Assert.Contains("ride Mamoswine", host.Log);
        // ...but not anywhere else
        var elsewhere = Play(new StoryState(), "kalos_route_17", "Mamoswine", map, mamoswine, tile: (490, 210));
        Assert.Null(elsewhere.Riding);

        // A Mamoswine stands on open snow at each end and in each clearing between the bands, so nobody is stranded
        var spots = map.Everyone.Where(n => n.Species == "Mamoswine").Select(n => n.GridX).OrderBy(x => x).ToList();
        Assert.Equal(4, spots.Count);
        Assert.True(spots[0] < 536 && spots[1] is > 541 and < 558 && spots[2] is > 563 and < 580 && spots[3] > 585);
        // The walk of the whole game, which counts a drift as ridden through, reaches Anistar City
        Assert.Contains(Walked.Value[map], t => t.X > 610 && t.Y is > 190 and < 215);
    }

    // ------------------------------------------------------------------ hordes and sky trainers

    [Fact]
    public void KalossRoutesHaveHordesAndSkyTrainersWhoBattleInTheAir()
    {
        Assert.True(Hordes.PlacesOf(RegionDatabase.Kalos).Count >= 15);
        var skyTrainers = new[] { "celeste", "gaston", "mireille", "thierry", "zoe" };
        foreach (string who in skyTrainers)
        {
            var trainer = TrainerDatabase.Get($"kalos_sky_trainer_{who}");
            Assert.NotNull(trainer);
            Assert.Equal("sky", trainer!.Format);
            Assert.True(SkyBattles.CanTakePart(TrainerDatabase.PartyOf(trainer)), $"{who}'s team can fly");
            var person = Overworld.Everyone.First(n => n.TrainerData?.Id == $"kalos_sky_trainer_{who}");
            Assert.Equal(0, person.TrainerData!.SightRange);
            Assert.True(person.TrainerData.InTheAir);
        }
    }

    // ------------------------------------------------------------------ helpers

    private static HashSet<(int X, int Y)> Reach(Map hall, (int X, int Y) from, bool panels)
    {
        var seen = new HashSet<(int X, int Y)> { from };
        var queue = new Queue<(int X, int Y)>(seen);
        while (queue.Count > 0)
        {
            var (x, y) = queue.Dequeue();
            if (panels && hall.GetWarpAt(x, y) is { } w && w.TargetMap == hall.Name && seen.Add((w.TargetX, w.TargetY))) queue.Enqueue((w.TargetX, w.TargetY));
            foreach (var dir in new[] { Direction.Up, Direction.Down, Direction.Left, Direction.Right })
            {
                var step = FieldMovement.Step(hall, x, y, dir, new Walker(TravelMode.OnFoot, hall.HeightAt(x, y)));
                // (the hall's people stand aside: a trainer is battled and stays where they are, off the way)
                var (dx, dy) = FieldMovement.Delta(dir);
                bool person = step.Obstacle == Obstacle.Person && !hall.IsSolid(x + dx, y + dy);
                if (!step.Moves && !person) continue;
                var to = step.Moves ? (step.X, step.Y) : (x + dx, y + dy);
                if (!panels && hall.GetWarpAt(to.Item1, to.Item2) is { } p && p.TargetMap == hall.Name) continue;
                if (seen.Add(to)) queue.Enqueue(to);
            }
        }
        return seen;
    }

    /// <summary>The fewest steps from the door to the walkway under Wulfric, the turntables turning as their hubs are stepped onto (or not at all).</summary>
    private static int? Search(Map hall, SnowbelleTurntables puzzle, (int X, int Y, int[] States) start, bool turning)
    {
        string Key(int x, int y, int[] s) => $"{x},{y},{string.Join("", s)}";
        var seen = new Dictionary<string, int> { [Key(start.X, start.Y, start.States)] = 0 };
        var queue = new Queue<(int X, int Y, int[] S, int D)>();
        queue.Enqueue((start.X, start.Y, start.States, 0));
        while (queue.Count > 0)
        {
            var (x, y, s, d) = queue.Dequeue();
            if ((x, y) == (6, 7)) return d;
            foreach (var dir in new[] { Direction.Up, Direction.Down, Direction.Left, Direction.Right })
            {
                puzzle.Set(s);
                var (dx, dy) = FieldMovement.Delta(dir);
                int nx = x + dx, ny = y + dy;
                if (!hall.InBounds(nx, ny) || hall.IsSolid(nx, ny) || puzzle.Refuses(hall, nx, ny, 0f, false)) continue;
                var next = (int[])s.Clone();
                int hub = SnowbelleTurntables.HubAt(nx, ny);
                if (hub >= 0 && turning) next[hub] = (next[hub] + 1) & 3;
                string k = Key(nx, ny, next);
                if (seen.ContainsKey(k)) continue;
                seen[k] = d + 1;
                queue.Enqueue((nx, ny, next, d + 1));
            }
        }
        return null;
    }

    private static HeadlessScriptHost Play(StoryState story, string file, string script, Map at, NPC? who,
        Inventory? bag = null, (int X, int Y)? tile = null, params int[] answers)
    {
        var party = new Party();
        party.Add(new Pokemon(PokemonDatabase.Get("Greninja")!, 80));
        var host = new HeadlessScriptHost(story, party, bag ?? new Inventory()) { Map = at, Region = RegionDatabase.Kalos, MapNamed = n => MapNamed(n) };
        foreach (int a in answers) host.Answers.Enqueue(a);
        if (tile is { } t) host.PlayerTile = t;
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find(script, file) ?? throw new Xunit.Sdk.XunitException($"{file}.{script} is missing"), who);
        runner.RunToEnd();
        Assert.Empty(host.Problems);
        return host;
    }
}
