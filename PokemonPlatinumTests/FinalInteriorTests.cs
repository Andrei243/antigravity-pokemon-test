using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumTests;

/// <summary>
/// A gate house walked as the room it is since plan 01 · M11, part d: what the tests of the areas outside ask of a
/// gate, now that no overlay passes anyone through one.
/// </summary>
internal static class GateWalk
{
    private static Map Room(string name) => GameDataFiles.Load<MapFile>(Path.Combine(MapDatabase.Folder, name + ".json")).ToMap();

    /// <summary>
    /// The door out of a gate house on the far side from where <paramref name="into"/> puts one down: straight across
    /// its floor, the nearest of a wide door's tiles.
    /// </summary>
    public static Warp FarDoor(Warp into)
    {
        var gate = Room(into.TargetMap);
        Assert.True(gate.IsIndoors, $"{into.TargetMap} is no room");
        Assert.True(gate.IsWalkable(into.TargetX, into.TargetY), $"{into.TargetMap}: nobody can stand at ({into.TargetX},{into.TargetY})");
        return gate.Warps.Where(w => w.TargetMap != gate.Name)
            .OrderByDescending(w => Math.Max(Math.Abs(w.SourceX - into.TargetX), Math.Abs(w.SourceY - into.TargetY)))
            .ThenBy(w => Math.Abs(w.SourceX - into.TargetX) + Math.Abs(w.SourceY - into.TargetY))
            .First();
    }

    /// <summary>The far doors of the gate houses that stand between two areas of a map, one for each way in from the first.</summary>
    public static List<Warp> Through(Map map, string from, string to)
    {
        var ways = new List<Warp>();
        foreach (var into in map.Warps)
        {
            if (map.AreaAt(into.SourceX, into.SourceY)?.Key != from || !into.TargetMap.Contains("Gate", StringComparison.Ordinal)) continue;
            if (!File.Exists(GameDataFiles.PathOf(Path.Combine(MapDatabase.Folder, into.TargetMap + ".json")))) continue;
            var far = FarDoor(into);
            if (far.TargetMap == map.Name && map.AreaAt(far.TargetX, far.TargetY)?.Key == to) ways.Add(far);
        }
        return ways;
    }
}

/// <summary>
/// The rooms plan 01 · M11, part d built, on the original's plans: the fifteen gate houses that were walked through
/// until then (the gate to Route 209 is plan 02 · S7's), the tutors' and the move men's houses, Veilstone's
/// Department Store, the Pokémon Centers' other floors and the Global Terminal. Where their people stand is
/// <see cref="RoomPlanTests"/>'s.
/// </summary>
public partial class FinalInteriorTests
{
    // Maps of this class's own, so nothing another test does to the database's maps shows here
    private static readonly Lazy<World> LoadedWorld = new(() => World.LoadAll().Single(w => w.Index.Region == "Sinnoh"));
    private static readonly Lazy<Dictionary<string, Map>> BuiltMaps = new(() => LoadedWorld.Value.BuildMaps().ToDictionary(m => m.Name));

    private static World Sinnoh => LoadedWorld.Value;

    private static Map Room(string name) => GameDataFiles.Load<MapFile>(Path.Combine(MapDatabase.Folder, name + ".json")).ToMap();

    private static int Have(HeadlessScriptHost host, string item) => host.Bag.GetQuantity(ItemDatabase.Get(item)!);

    private static Pokemon Mon(string species = "Turtwig", int level = 20) => new(PokemonDatabase.Get(species)!, level, Gender.Male, Nature.Hardy, false);

    private static HeadlessScriptHost Run(string script, string place, HeadlessScriptHost host, NPC? who = null)
    {
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find(script, place) ?? throw new InvalidOperationException($"no script {place}.{script}"), who);
        runner.RunToEnd();
        Assert.Empty(host.Problems);
        return host;
    }

    private static string Said(HeadlessScriptHost host) => string.Join(" | ", host.Transcript.Select(t => t.Text));

    /// <summary>The gate houses of part d, each with the two areas it stands between (the second of the Amity Square's and the Frontier's is a map of its own).</summary>
    public static IEnumerable<object[]> Gates => new[]
    {
        new object[] { "Route218GateToJubilifeCity", "route_218", "jubilife_city" },
        new object[] { "Route218GateToCanalaveCity", "canalave_city", "route_218" },
        new object[] { "Route208GateToHearthomeCity", "route_208", "hearthome_city" },
        new object[] { "Route215GateToVeilstoneCity", "route_215", "veilstone_city" },
        new object[] { "Route213GateToPastoriaCity", "pastoria_city", "route_213" },
        new object[] { "Route222GateToSunyshoreCity", "route_222", "sunyshore_city" },
        new object[] { "Route228GateToRoute226", "route_226", "route_228" },
        new object[] { "Route212GateToHearthomeCity", "hearthome_city", "route_212_north" },
        new object[] { "Route225GateToFightArea", "route_225", "fight_area" },
        new object[] { "Route214GateToVeilstoneCity", "veilstone_city", "route_214" },
        new object[] { "Route206CyclingRoadNorthGate", "eterna_city", "route_206" },
        new object[] { "Route206CyclingRoadSouthGate", "route_206", "route_206" },
        new object[] { "HearthomeCityWestGateToAmitySquare", "amity_square", "hearthome_city" },
        new object[] { "HearthomeCityEastGateToAmitySquare", "amity_square", "hearthome_city" },
        new object[] { "BattleFrontierGateToFightArea", "battle_frontier", "fight_area" },
    };

    [Theory]
    [MemberData(nameof(Gates))]
    public void AGateHouseIsARoomBetweenItsTwoPlaces(string room, string one, string other)
    {
        var gate = Room(room);
        Assert.Equal(InteriorStyle.House, gate.Interior);
        Assert.Equal(RegionDatabase.Sinnoh, RegionDatabase.RegionOfMap(room)!.Id);

        // Every way in from outside is a door of the overlays, onto a tile one can stand on, facing into the room
        int ways = 0;
        foreach (var outside in BuiltMaps.Value.Values)
            foreach (var into in outside.Warps.Where(w => w.TargetMap == room))
            {
                ways++;
                string where = $"{room}, from {outside.Name} ({into.SourceX},{into.SourceY})";
                string area = outside.AreaAt(into.SourceX, into.SourceY)!.Key;
                Assert.True(area == one || area == other, $"{where}: {area} is neither of its places");
                Assert.True(gate.IsWalkable(into.TargetX, into.TargetY), $"{where} puts the player on a blocked tile ({into.TargetX},{into.TargetY})");
                Assert.Null(gate.GetWarpAt(into.TargetX, into.TargetY));
                // Straight across is the door in the far wall, which leads out into the other place, a step from its way
                // in and facing away from the building
                var far = GateWalk.FarDoor(into);
                var beyond = BuiltMaps.Value[far.TargetMap];
                string farArea = beyond.AreaAt(far.TargetX, far.TargetY)!.Key;
                Assert.True(farArea == one || farArea == other, $"{where}: its far door leads to {farArea}");
                if (one != other) Assert.NotEqual(area, farArea);
                Assert.True(beyond.IsWalkable(far.TargetX, far.TargetY), $"{where}: its far door lets out onto a blocked tile");
                Assert.Null(beyond.GetWarpAt(far.TargetX, far.TargetY));
                var (dx, dy) = FieldMovement.Delta(far.TargetFacing);
                var back = beyond.GetWarpAt(far.TargetX - dx, far.TargetY - dy);
                Assert.True(back?.TargetMap == room, $"{where}: turning round outside its far door doesn't lead back in");
                // And the door one came in by is behind one's back
                var (ix, iy) = FieldMovement.Delta(into.TargetFacing);
                var behind = gate.GetWarpAt(into.TargetX - ix, into.TargetY - iy);
                Assert.True(behind?.TargetMap == outside.Name, $"{where}: the door behind doesn't lead back out");
            }
        Assert.True(ways >= 2, $"{room} has {ways} ways in");
        // As its place's rooms have it: one of its two places' themes
        var themes = BuiltMaps.Value.Values.SelectMany(m => m.Warps.Where(w => w.TargetMap == room).Select(w => m.BgmTrackAt(w.SourceX, w.SourceY))).ToHashSet();
        Assert.Contains(gate.BgmTrack, themes);
    }

    [Fact]
    public void NoGateHouseIsPassedThroughAnyMore()
    {
        // The overlays' passages are gone with the last gate house (WorldMapBuilder.Join's "through", plan 01 · M6):
        // no overlay names one, and no warp of the world leads from one place of the map of Sinnoh straight into the
        // next past a building's door
        string folder = GameDataFiles.PathOf(Path.Combine("world", "sinnoh", "overlays"));
        foreach (string file in Directory.GetFiles(folder, "*.json"))
            Assert.DoesNotContain("\"through\":", File.ReadAllText(file));
        var sinnoh = BuiltMaps.Value["Sinnoh"];
        foreach (var gate in Gates.Select(g => (string)g[0]))
            Assert.Contains(BuiltMaps.Value.Values, m => m.Warps.Any(w => w.TargetMap == gate));
        Assert.DoesNotContain(sinnoh.Warps, w => w.CyclistsOnly);
        Assert.Equal(15, Gates.Count());
    }

    [Fact]
    public void EveryoneOfAGateStandsOnFloorWithSomethingToSay()
    {
        foreach (string name in Gates.Select(g => (string)g[0]))
        {
            var map = Room(name);
            Assert.NotEmpty(map.Everyone);
            foreach (var who in map.Everyone)
            {
                string where = $"{name}: {who.Key} at {who.GridX},{who.GridY}";
                Assert.False(map.IsSolid(who.GridX, who.GridY), where + " stands in something solid");
                Assert.DoesNotContain(map.Warps, w => (w.TargetX, w.TargetY) == (who.GridX, who.GridY) && w.TargetMap == name);
                bool speaks = who.DialogLines.Count > 0 || who.Script != null && ScriptLibrary.Default.Find(who.Script, name) != null;
                Assert.True(speaks, where + " has nothing to say");
                // Nobody stands where someone coming in is put down
                foreach (var outside in BuiltMaps.Value.Values)
                    Assert.DoesNotContain(outside.Warps, w => w.TargetMap == name && (w.TargetX, w.TargetY) == (who.GridX, who.GridY));
            }
            Assert.DoesNotContain(ScriptLibrary.Default.Problems().ToList(), p => p.Contains(name + ".txt", StringComparison.Ordinal));
        }
    }

    // ------------------------------------------------------------------ the Cycling Road's gates

    [Theory]
    [InlineData("Route206CyclingRoadNorthGate", 13, 8, 6, Direction.Up)]
    [InlineData("Route206CyclingRoadSouthGate", 2, 6, 8, Direction.Down)]
    public void TheCyclingRoadsGatesLetTheBicycleInAndOnlyRidersOntoTheRoad(string room, int roadRow, int keepersRow, int freeRow, Direction back)
    {
        var gate = Room(room);
        // The Bicycle is ridden through the room, as through no other
        Assert.True(gate.BikeAllowedAt(7, 7));
        Assert.False(Room("Route208GateToHearthomeCity").BikeAllowedAt(5, 7));
        Assert.True(GameDataFiles.Load<MapFile>(Path.Combine(MapDatabase.Folder, room + ".json")).Bike);
        // Its doors onto the road are for riders, and the others for anyone
        Assert.All(gate.Warps, w => Assert.Equal(w.SourceY == roadRow, w.CyclistsOnly));
        Assert.Equal(3, gate.Warps.Count(w => w.CyclistsOnly));

        // The row of the lane past the keepers: someone on foot is turned back
        var first = gate.Triggers.Single(t => t.Script == "OnlyBicycles");
        var second = gate.Triggers.Single(t => t.Script == "OffTheRoad");
        Assert.Equal((keepersRow, 0, freeRow, 1), (first.Y, first.Value, second.Y, second.Value));
        for (int x = first.X; x < first.X + first.Width; x++) Assert.True(gate.IsWalkable(x, first.Y), $"{room}: the row reaches over ({x},{first.Y})");
        var onFoot = new HeadlessScriptHost { Map = gate, PlayerTile = (7, keepersRow), PlayerFacing = back == Direction.Up ? Direction.Down : Direction.Up };
        Run("OnlyBicycles", room, onFoot);
        var (dx, dy) = FieldMovement.Delta(back);
        Assert.Equal((7 + dx, keepersRow + dy), onFoot.PlayerTile);
        Assert.Contains(onFoot.Transcript, t => t.Speaker == "Gate Keeper");
        Assert.False(onFoot.Story.Has(BicycleRules.ForcedInGateFlag));
        Assert.Equal(0, onFoot.Story.Var("VAR_MAP_LOCAL_0x02"));

        // A rider is waved through, and can't get off from there to the road
        var riding = new HeadlessScriptHost { Map = gate, PlayerTile = (7, keepersRow), PlayerMode = TravelMode.Cycling };
        Run("OnlyBicycles", room, riding);
        Assert.Equal((7, keepersRow), riding.PlayerTile);
        Assert.Empty(riding.Transcript);
        Assert.True(riding.Story.Has(BicycleRules.ForcedInGateFlag));
        Assert.Equal(1, riding.Story.Var("VAR_MAP_LOCAL_0x02"));
        // Coming back, the other row lets them off again
        Run("OffTheRoad", room, riding);
        Assert.False(riding.Story.Has(BicycleRules.ForcedInGateFlag));
        Assert.Equal(0, riding.Story.Var("VAR_MAP_LOCAL_0x02"));

        // Whoever comes in off the road is riding and stays so; whoever comes in from the town isn't held
        var offTheRoad = new HeadlessScriptHost { Map = gate, PlayerTile = (7, roadRow == 13 ? 12 : 3), PlayerMode = TravelMode.Cycling };
        Run("OnEnter", room, offTheRoad);
        Assert.True(offTheRoad.Story.Has(BicycleRules.ForcedInGateFlag));
        var fromTown = new HeadlessScriptHost { Map = gate, PlayerTile = (7, roadRow == 13 ? 3 : 12) };
        Run("OnEnter", room, fromTown);
        Assert.False(fromTown.Story.Has(BicycleRules.ForcedInGateFlag));
    }

    [Fact]
    public void TheNorthGatesAssistantGivesTheExpShareForThirtyFivePokemonSeen()
    {
        const string room = "Route206CyclingRoadNorthGate", flag = "FLAG_RECEIVED_ROUTE_206_CYCLING_ROAD_NORTH_GATE_EXP_SHARE";
        var few = Run("Assistant", room, new HeadlessScriptHost { SeenInSinnoh = 34 });
        Assert.Equal(0, Have(few, "Exp. Share"));
        Assert.False(few.Story.Has(flag));
        var enough = Run("Assistant", room, new HeadlessScriptHost { SeenInSinnoh = 35 });
        Assert.Equal(1, Have(enough, "Exp. Share"));
        Assert.True(enough.Story.Has(flag));
        Run("Assistant", room, enough);
        Assert.Equal(1, Have(enough, "Exp. Share"));
        // He is gone the next time the room is come into
        Run("OnEnter", room, enough);
        Assert.True(enough.Story.Has("FLAG_HIDE_ROUTE_206_CYCLING_ROAD_NORTH_GATE_SCIENTIST_M"));
        Assert.Equal("FLAG_HIDE_ROUTE_206_CYCLING_ROAD_NORTH_GATE_SCIENTIST_M", Room(room).Everyone.Single(n => n.Key == "scientist_m").HiddenBy);
    }

    [Fact]
    public void TheSouthGatesBattleGirlGivesAFlagOnce()
    {
        const string room = "Route206CyclingRoadSouthGate";
        var host = Run("BattleGirl", room, new HeadlessScriptHost());
        Assert.True(host.Story.Has("FLAG_RECEIVED_ROUTE_206_CYCLING_ROAD_SOUTH_GATE_ACCESSORY_FLAG"));
        int flag = SuperContestDatabase.Accessory("Flag");
        Assert.Equal(1, host.FashionCase.Count(flag));
        Run("BattleGirl", room, host);
        Assert.Equal(1, host.FashionCase.Count(flag));
        Assert.Contains("Hearthome", Said(host));
    }

    // ------------------------------------------------------------------ gifts and scenes

    [Fact]
    public void TheFishermanOfJubilifesGateGivesTheOldRodOnce()
    {
        const string room = "Route218GateToJubilifeCity";
        var refused = new HeadlessScriptHost();
        refused.Answers.Enqueue(1);
        Run("Fisherman", room, refused);
        Assert.Equal(0, Have(refused, "Old Rod"));
        var host = Run("Fisherman", room, new HeadlessScriptHost());
        Assert.Equal(1, Have(host, "Old Rod"));
        Assert.True(host.Story.Has("FLAG_RECEIVED_OLD_ROD"));
        Run("Fisherman", room, host);
        Assert.Equal(1, Have(host, "Old Rod"));
    }

    [Theory]
    [InlineData(5)]
    [InlineData(7)]
    [InlineData(9)]
    public void TheAssistantsFatherUpgradesThePokedexInCanalavesGateAndLeaves(int row)
    {
        const string room = "Route218GateToCanalaveCity";
        var gate = Room(room);
        var trigger = Assert.Single(gate.Triggers);
        Assert.Equal((7, 5, 1, 5, "Upgrade", "VAR_ROUTE_218_GATE_TO_CANALAVE_CITY_STATE", 0), (trigger.X, trigger.Y, trigger.Width, trigger.Depth, trigger.Script, trigger.Variable, trigger.Value));
        var father = gate.Everyone.Single(n => n.Key == "scientist_m");
        Assert.Equal((5, 7, "FLAG_HIDE_ROUTE_218_GATE_TO_CANALAVE_CITY_SCIENTIST_M"), (father.GridX, father.GridY, father.HiddenBy));

        var host = new HeadlessScriptHost { Map = gate, PlayerTile = (7, row), PlayerFacing = Direction.Left };
        Run("Upgrade", room, host);
        Assert.Empty(host.Problems);
        Assert.Contains("{assistant}", string.Join(" ", ScriptLibrary.Default.Find("Words", room)!.Everything().SelectMany(i => i.Lines)));
        Assert.True(host.Story.Has("FLAG_ENABLED_POKEDEX_FORM_DETECTION"));
        Assert.True(host.Story.Has("FLAG_HIDE_ROUTE_218_GATE_TO_CANALAVE_CITY_SCIENTIST_M"));
        Assert.Equal(1, host.Story.Var("VAR_ROUTE_218_GATE_TO_CANALAVE_CITY_STATE"));
        // He walked up beside the player and out by the east door
        Assert.Equal((10, 7), (father.GridX, father.GridY));
    }

    [Fact]
    public void TheTelevisionCrewTurnsThePlayerBackInTheGateUntilPastoria()
    {
        const string room = "Route212GateToHearthomeCity";
        var gate = Room(room);
        var crew = gate.Everyone.Where(n => n.HiddenBy == "FLAG_HIDE_ROUTE_212_BLOCKADE").ToList();
        Assert.Equal(new[] { "cameraman", "reporter" }, crew.Select(n => n.Key).Order());
        // A row north of the original's tiles, so the tile one comes in on from Route 212 is free
        Assert.All(crew, n => Assert.Equal(11, n.GridY));
        var trigger = Assert.Single(gate.Triggers);
        Assert.Equal((4, 10, 3, 1, "Blockade"), (trigger.X, trigger.Y, trigger.Width, trigger.Depth, trigger.Script));
        // Row 10 is the whole width of the corridor: nobody gets past it
        Assert.True(gate.IsSolid(3, 10) && gate.IsSolid(7, 10));

        var host = new HeadlessScriptHost { Map = gate, PlayerTile = (5, 10), PlayerFacing = Direction.Down };
        Run("Blockade", room, host);
        Assert.Equal((5, 9), host.PlayerTile);
        Assert.Contains("television", Said(host));
        // Once Pastoria City has been come to they are gone, and the row is a row like any other
        var later = new HeadlessScriptHost { Map = gate, PlayerTile = (5, 10), PlayerFacing = Direction.Down };
        later.Story.Set("FLAG_HIDE_ROUTE_212_BLOCKADE");
        Run("Blockade", room, later);
        Assert.Equal((5, 10), later.PlayerTile);
        Assert.Empty(later.Transcript);
    }

    [Fact]
    public void TheFrontiersGateWelcomesAFirstVisitOnceAndItsAttendantsExplain()
    {
        const string room = "BattleFrontierGateToFightArea", state = "VAR_BATTLE_FRONTIER_GATE_TO_FIGHT_AREA_STATE";
        var gate = Room(room);
        Assert.Equal(6, gate.Everyone.Count(n => n.Name == "Attendant"));
        var host = new HeadlessScriptHost { Map = gate, PlayerTile = (7, 20), PlayerFacing = Direction.Up };
        host.Answers.Enqueue(5);
        Run("OnEnter", room, host);
        Assert.Equal(1, host.Story.Var(state));
        Assert.Equal((7, 17), host.PlayerTile);
        Assert.Contains("Vs. Recorder", Said(host));
        // The second time through nobody stops the player
        var again = new HeadlessScriptHost { Map = gate, PlayerTile = (7, 20) };
        again.Story.SetVar(state, 1);
        Run("OnEnter", room, again);
        Assert.Empty(again.Transcript);
        Assert.Equal((7, 20), again.PlayerTile);
        // Each topic of the general desk is an answer and an end, and each of the five others tells of its hall
        for (int topic = 0; topic < 6; topic++)
        {
            var asked = new HeadlessScriptHost();
            asked.Answers.Enqueue(topic);
            Run("General", room, asked);
            Assert.Single(asked.Asked);
            Assert.True(asked.Transcript.Count >= (topic < 5 ? 2 : 1), $"topic {topic}: {Said(asked)}");
        }
        foreach (string hall in new[] { "Tower", "Factory", "Hall", "Castle", "Arcade" })
        {
            Assert.Contains(gate.Everyone, n => n.Script == hall);
            Assert.Contains("Battle " + hall, Said(Run(hall, room, new HeadlessScriptHost())));
        }
    }
}
