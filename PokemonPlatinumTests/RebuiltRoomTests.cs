using System.Text.Json;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumTests;

/// <summary>
/// The rooms of the first chapters, made by hand before the world was imported and laid out again on the original's
/// plans last of all (plan 01 · M11, the last rooms): the ground floors of the seventeen Pokémon Centers and the
/// thirteen Poké Marts, each town's a copy of one plan with its own people. What stood on the hand-made tiles was
/// carried across: the towns' doors, the floors above and below, the nurse's and the clerks' counters, the PC, the
/// tile a white-out wakes on, and a save made in the old room (<see cref="SaveData.PlaceOn"/>). Where the people
/// stand is <see cref="RoomPlanTests"/>'s.
/// </summary>
public class RebuiltRoomTests
{
    // Maps of this class's own, so nothing another test does to the database's maps shows here
    private static readonly Lazy<Map> BuiltSinnoh = new(() =>
        World.LoadAll().Single(w => w.Index.Region == "Sinnoh").BuildMaps().Single(m => m.Name == "Sinnoh"));

    private static Map Room(string name) => GameDataFiles.Load<MapFile>(Path.Combine(MapDatabase.Folder, name + ".json")).ToMap();

    public static readonly string[] Centers =
    {
        "PokemonCenter", "JubilifePokemonCenter", "OreburghPokemonCenter", "FloaromaPokemonCenter", "EternaPokemonCenter",
        "HearthomePokemonCenter", "SolaceonPokemonCenter", "VeilstonePokemonCenter", "PastoriaPokemonCenter", "CanalavePokemonCenter",
        "CelesticPokemonCenter", "SnowpointPokemonCenter", "SunyshorePokemonCenter", "PokemonLeagueSouthPokemonCenter",
        "FightAreaPokemonCenter", "SurvivalAreaPokemonCenter", "ResortAreaPokemonCenter",
    };

    // Each Mart with the key of its town's own counter, where the original has a second clerk
    public static readonly (string Room, string? Counter)[] Marts =
    {
        ("PokeMart", null), ("JubilifePokeMart", "jubilife"), ("OreburghPokeMart", "oreburgh"), ("FloaromaPokeMart", "floaroma"),
        ("EternaPokeMart", "eterna_mart"), ("HearthomePokeMart", "hearthome"), ("SolaceonPokeMart", "solaceon"), ("PastoriaPokeMart", "pastoria"),
        ("CanalavePokeMart", "canalave"), ("SnowpointPokeMart", "snowpoint"), ("SunyshorePokeMart", "sunyshore"),
        ("FightAreaPokeMart", null), ("SurvivalAreaPokeMart", null),
    };

    private const string Blockade = "FLAG_HIDE_POKECENTER_BASEMENT_BLOCKADE";

    private static Pokemon Mon(int friendship)
    {
        var mon = new Pokemon(PokemonDatabase.Get("Turtwig")!, 20, Gender.Male, Nature.Hardy, false);
        mon.Friendship = friendship;
        return mon;
    }

    private static HeadlessScriptHost Run(string script, string place, HeadlessScriptHost host, NPC? who = null)
    {
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find(script, place) ?? throw new InvalidOperationException($"no script {place}.{script}"), who);
        runner.RunToEnd();
        Assert.Empty(host.Problems);
        return host;
    }

    private static string Said(HeadlessScriptHost host) => string.Join(" ", host.Transcript.Select(l => l.Text));

    // The town's door leads onto the mat inside the room's own, and the room's door back out before the town's
    private static void LeadsInAndBackOut(Map room, int matX, int matY)
    {
        var door = Assert.Single(BuiltSinnoh.Value.Warps, w => w.TargetMap == room.Name);
        Assert.Equal((matX, matY, Direction.Up), (door.TargetX, door.TargetY, door.TargetFacing));
        var exit = Assert.Single(room.Warps, w => w.TargetMap == "Sinnoh");
        Assert.Equal((matX, matY + 1), (exit.SourceX, exit.SourceY));
        Assert.Equal((door.SourceX, door.SourceY + 1, Direction.Down), (exit.TargetX, exit.TargetY, exit.TargetFacing));
        Assert.Equal(TileType.Door, room.GetGroundTile(matX, matY + 1));
        Assert.True(room.IsWalkable(matX, matY), $"{room.Name}: someone stands on the mat inside the door");
        Assert.Equal(RegionDatabase.Sinnoh, RegionDatabase.RegionOfMap(room.Name)!.Id);
    }

    private static void EveryoneStandsOnFloorWithSomethingToSay(Map room)
    {
        foreach (var who in room.Everyone)
        {
            string where = $"{room.Name}: {who.Key} at {who.GridX},{who.GridY}";
            // (The PC's terminal is the PC's own tile)
            Assert.True(who.IsPCTerminal || !room.IsSolid(who.GridX, who.GridY), where + " stands in something solid");
            bool speaks = who.DialogLines.Count > 0 || who.IsHealingNurse || who.IsPokeMartClerk || who.IsPCTerminal
                || who.Script != null && ScriptLibrary.Default.Find(who.Script, room.Name) != null;
            Assert.True(speaks, where + " has nothing to say");
        }
        Assert.DoesNotContain(ScriptLibrary.Default.Problems().ToList(), p => p.Contains(room.Name + ".txt", StringComparison.Ordinal));
    }

    // ------------------------------------------------------------------ the Pokémon Centers

    [Fact]
    public void EveryPokemonCentersGroundFloorIsTheOriginalsOnePlan()
    {
        foreach (string name in Centers)
        {
            var map = Room(name);
            Assert.Equal((17, 14, InteriorStyle.PokemonCenter), (map.Width, map.Height, map.Interior));
            Assert.Equal((1, 3), map.RoomCorner());
            LeadsInAndBackOut(map, 8, 12);

            // The nurse behind her counter, spoken to across it from the tile a white-out wakes on
            var nurse = Assert.Single(map.Everyone, n => n.IsHealingNurse);
            Assert.Equal((8, 4, Direction.Down), (nurse.GridX, nurse.GridY, nurse.Facing));
            Assert.True(map.IsCounter(nurse.GridX, nurse.GridY + 1));
            Assert.Equal(TileBehavior.Counter, map.BehaviourAt(nurse.GridX, nurse.GridY + 1));
            Assert.True(map.IsWalkable(nurse.GridX, nurse.GridY + 2), $"{name}: someone stands where a white-out wakes");
            Assert.Contains(map.Props, p => p.Type == PropType.HealingMachine && p.Y == nurse.GridY);
            // Nobody walks in behind the counter
            foreach (var (x, y) in new[] { (8, 3), (9, 3), (8, 4), (9, 4) })
                foreach (var (dx, dy) in new[] { (0, 1), (1, 0), (-1, 0), (0, -1) })
                    Assert.True(map.IsSolid(x + dx, y + dy) || (x + dx is 8 or 9 && y + dy is 3 or 4), $"{name}: a way in behind the counter at {x + dx},{y + dy}");

            // The PC: its own tile against the back wall, its terminal on it, used from the tile before it
            var pc = Assert.Single(map.Everyone, n => n.IsPCTerminal);
            Assert.Equal((12, 3), (pc.GridX, pc.GridY));
            Assert.Equal(TileType.PC, map.GetGroundTile(12, 3));
            Assert.True(map.IsSolid(12, 3) && map.IsWalkable(12, 4), $"{name}: the PC can't be used");

            // The escalators in the south wall's alcoves, as the original has them: up at the west, down at the east
            var up = Assert.Single(map.Warps, w => w.TargetMap == name + "2F");
            var down = Assert.Single(map.Warps, w => w.TargetMap == name + "B1F");
            Assert.Equal(((2, 10), (14, 10)), ((up.SourceX, up.SourceY), (down.SourceX, down.SourceY)));
            Assert.Contains(map.Props, p => p.Type == PropType.SideStairsUp && (p.X, p.Y) == (1, 10));
            Assert.Contains(map.Props, p => p.Type == PropType.SideStairsDown && (p.X, p.Y) == (15, 10));
            Assert.All(new[] { (1, 9), (2, 9), (1, 11), (2, 11), (14, 9), (15, 9), (14, 11), (15, 11) }, t => Assert.True(map.IsSolid(t.Item1, t.Item2)));
            // The floors above and below lead back onto the mats beside them
            var above = Assert.Single(Room(name + "2F").Warps);
            var below = Assert.Single(Room(name + "B1F").Warps);
            Assert.Equal((name, 3, 10, Direction.Right), (above.TargetMap, above.TargetX, above.TargetY, above.TargetFacing));
            Assert.Equal((name, 13, 10, Direction.Left), (below.TargetMap, below.TargetX, below.TargetY, below.TargetFacing));
            Assert.True(map.IsWalkable(3, 10), $"{name}: someone stands at the head of the way up");
            var before = map.GetNpcAt(13, 10);
            Assert.True(before == null || before.HiddenBy == Blockade, $"{name}: {before?.Key} stands before the way down");

            EveryoneStandsOnFloorWithSomethingToSay(map);
            // Everyone but the nurse, the PC and whoever has a script has lines of their own
            Assert.True(map.Everyone.Count(n => !n.IsHealingNurse && !n.IsPCTerminal) >= 2, $"{name} has nobody in it");
        }
    }

    [Fact]
    public void AnAttendantKeepsTheBasementShutInTheFirstTwoTownsUntilTheCoalBadge()
    {
        foreach (string name in Centers)
        {
            var map = Room(name);
            var attendant = map.Everyone.SingleOrDefault(n => n.HiddenBy == Blockade);
            // The original has her in Sandgem Town and Jubilife City alone, the two Centers come to before Oreburgh's Gym
            Assert.Equal(name is "PokemonCenter" or "JubilifePokemonCenter", attendant != null);
            if (attendant == null) continue;
            Assert.Equal((13, 10), (attendant.GridX, attendant.GridY));
            Assert.NotEmpty(attendant.DialogLines);
            // Only the mat she stands on leads to the way down
            Assert.False(map.IsWalkable(13, 10));
            Assert.True(map.IsSolid(14, 9) && map.IsSolid(14, 11));
            map.ApplyPresence(flag => flag == Blockade);
            Assert.True(map.IsWalkable(13, 10));
        }
        // Roark's Badge is what sends her away (OreburghGym.txt, as the original's Gym script does)
        Assert.Contains("setflag " + Blockade, File.ReadAllText(GameDataFiles.PathOf(Path.Combine("scripts", "OreburghGym.txt"))));
    }

    [Fact]
    public void OreburghsGentlemanWondersAboutTeamGalacticAndDifferentlyOnceTheGameIsCompleted()
    {
        var before = Run("Gentleman", "OreburghPokemonCenter", new HeadlessScriptHost());
        Assert.Contains("take Pokémon away by force", Said(before));
        var after = new HeadlessScriptHost();
        after.Story.Set("FLAG_GAME_COMPLETED");
        Assert.Contains("sworn off", Said(Run("Gentleman", "OreburghPokemonCenter", after)));
        Assert.DoesNotContain("by force", Said(after));

        // The psychic asks and changes nothing: the look others see in the Union Room waits for the link (plan 13)
        var psychic = Run("Psychic", "OreburghPokemonCenter", new HeadlessScriptHost());
        Assert.Equal(3, psychic.Transcript.Count);
        Assert.Empty(psychic.Asked);
        Assert.Empty(psychic.Story.Flags);
    }

    [Fact]
    public void FloaromasTrainerFretsOverThePowerUntilTheWindworksIsFree()
    {
        var host = new HeadlessScriptHost();
        Assert.Contains("Valley Windworks", Said(Run("AceTrainer", "FloaromaPokemonCenter", host)));
        host.Transcript.Clear();
        host.Story.SetVar("VAR_VALLEY_WINDWORKS_TEAM_GALACTIC_STATE", 2);
        Assert.Contains("Eterna City", Said(Run("AceTrainer", "FloaromaPokemonCenter", host)));
        Assert.DoesNotContain("Windworks", Said(host));
    }

    [Theory]
    [InlineData(0, "hasn't quite taken to you", 0)]
    [InlineData(49, "hasn't quite taken to you", 0)]
    [InlineData(50, "warming to you", 0)]
    [InlineData(149, "warming to you", 0)]
    [InlineData(150, "fond of you", 1)]
    [InlineData(255, "fond of you", 1)]
    public void CelesticsPokefanGivesAGreatBallADayToATrainerWhosePokemonIsFondOfThem(int friendship, string says, int balls)
    {
        const string flag = "FLAG_DAILY_RECEIVED_CELESTIC_TOWN_POKECENTER_1F_GREAT_BALL";
        var host = new HeadlessScriptHost();
        host.Party.Add(Mon(friendship));
        Run("Pokefan", "CelesticPokemonCenter", host);
        Assert.Contains(says, Said(host));
        Assert.Equal(balls, host.Bag.GetQuantity(ItemDatabase.Get("Great Ball")!));
        Assert.Equal(balls == 1, host.Story.Has(flag));
        // Asked again the same day, he has only his word on trust (a new day clears the flag: DailyEvents)
        host.Transcript.Clear();
        Run("Pokefan", "CelesticPokemonCenter", host);
        Assert.Equal(balls, host.Bag.GetQuantity(ItemDatabase.Get("Great Ball")!));
        if (balls == 1) Assert.DoesNotContain("interesting Pokémon", Said(host));
        Assert.StartsWith("FLAG_DAILY_", flag);
    }

    [Fact]
    public void TheSurvivalAreasTrainerWondersAboutTheBuildingNextDoorUntilThePlayerHasBeenIn()
    {
        var host = new HeadlessScriptHost();
        Assert.Contains("building next door", Said(Run("AceTrainer", "SurvivalAreaPokemonCenter", host)));
        host.Transcript.Clear();
        host.Story.Set("FLAG_VISITED_BATTLEGROUND");
        Assert.Contains("Battle Factory", Said(Run("AceTrainer", "SurvivalAreaPokemonCenter", host)));
    }

    [Fact]
    public void EternasBunearyIsHomeBesideItsBoyOnceTeamGalacticHasLeft()
    {
        const string flag = "FLAG_HIDE_ETERNA_CITY_POKECENTER_1F_BUNEARY";
        var map = Room("EternaPokemonCenter");
        var buneary = Assert.Single(map.Everyone, n => n.Key == "buneary");
        var boy = Assert.Single(map.Everyone, n => n.Key == "school_kid_m");
        Assert.Equal(("Buneary", flag), (buneary.Species, buneary.HiddenBy));
        Assert.Equal(1, Math.Abs(buneary.GridX - boy.GridX) + Math.Abs(buneary.GridY - boy.GridY));
        // A new game hides it, and the Commander's defeat brings it back (common.NewGame; TeamGalacticEternaBuilding4F.txt)
        map.ApplyPresence(f => f == flag);
        Assert.DoesNotContain(buneary, map.NPCs);
        map.ApplyPresence(_ => false);
        Assert.Contains(buneary, map.NPCs);
    }

    // ------------------------------------------------------------------ the Poké Marts

    [Fact]
    public void EveryPokeMartIsTheOriginalsPlan()
    {
        foreach (var (name, counter) in Marts)
        {
            var map = Room(name);
            Assert.Equal((12, 13, InteriorStyle.PokeMart), (map.Width, map.Height, map.Interior));
            Assert.Equal((1, 3), map.RoomCorner());
            LeadsInAndBackOut(map, 3, 11);

            // The clerks behind the counter, each spoken to across it: the common counter's at the east end, and the
            // town's own goods beside her where the original has a second clerk
            var clerks = map.Everyone.Where(n => n.IsPokeMartClerk).OrderByDescending(n => n.GridX).ToList();
            Assert.Equal(counter == null ? 1 : 2, clerks.Count);
            Assert.Equal((3, 5, Direction.Down, null), (clerks[0].GridX, clerks[0].GridY, clerks[0].Facing, clerks[0].Mart));
            if (counter != null)
            {
                Assert.Equal((2, 5, Direction.Down, counter), (clerks[1].GridX, clerks[1].GridY, clerks[1].Facing, clerks[1].Mart));
                Assert.NotEmpty(MartDatabase.Stock(counter, 8));
            }
            foreach (var clerk in clerks)
            {
                Assert.True(map.IsCounter(clerk.GridX, clerk.GridY + 1));
                Assert.True(map.IsWalkable(clerk.GridX, clerk.GridY + 2), $"{name}: someone stands before the counter");
                Assert.NotEmpty(clerk.DialogLines);
            }
            // Nobody walks in behind the counter: its row to the west wall, its arm to the back wall
            Assert.All(new[] { (1, 6), (2, 6), (3, 6), (4, 6), (4, 5), (4, 4), (4, 3) }, t => Assert.True(map.IsSolid(t.Item1, t.Item2), $"{name}: {t} is open"));

            // The wall of shelves behind the open floor of the east end, and the shelves on the floor
            Assert.Contains(map.Props, p => p.Type == PropType.WallShelf && (p.X, p.Y, p.Width) == (6, 2, 4));
            Assert.All(Enumerable.Range(6, 4), x => Assert.False(map.IsSolid(x, 3)));
            Assert.True(map.Props.Count(p => p.Type == PropType.StoreShelf) >= 8, name);

            EveryoneStandsOnFloorWithSomethingToSay(map);
            Assert.True(map.Everyone.Count(n => !n.IsPokeMartClerk) >= 2, $"{name} has no customers");
        }
    }

    [Fact]
    public void PastoriasCounterHasItsCarving()
    {
        var map = Room("PastoriaPokeMart");
        var carving = map.GetSignboardAt(1, 6);
        Assert.NotNull(carving);
        Assert.Contains("Croagunk", carving!);
        // Read across nothing: from the tile before the counter's west end, where no clerk stands behind it
        Assert.True(map.IsWalkable(1, 7));
        Assert.Null(map.GetNpcAt(1, 5));
        Assert.All(Marts.Where(m => m.Room != "PastoriaPokeMart"), m => Assert.Null(Room(m.Room).GetSignboardAt(1, 6)));
    }

    [Fact]
    public void AWallOfShelvesBlocksNothing()
    {
        Assert.False(new Prop { Type = PropType.WallShelf, X = 6, Y = 2, Width = 4 }.IsSolid);
        Assert.True(new Prop { Type = PropType.StoreShelf, X = 7, Y = 5, Width = 2 }.IsSolid);
    }

    // ------------------------------------------------------------------ saves

    public static IEnumerable<object[]> Rebuilt => Centers.Concat(Marts.Select(m => m.Room)).Select(name => new object[] { name });

    [Theory]
    [MemberData(nameof(Rebuilt))]
    public void ASaveMadeInARoomLaidOutAgainWakesOnTheTileTheRoomIsComeIntoBy(string room)
    {
        var map = MapDatabase.Get(room);
        var arrival = SaveData.ArrivalIn(map, MapDatabase.Get);
        Assert.NotNull(arrival);
        // The mat inside the door, looking in
        var door = Assert.Single(BuiltSinnoh.Value.Warps, w => w.TargetMap == room);
        Assert.Equal(new MapSpot(room, door.TargetX, door.TargetY, Direction.Up), arrival);
        Assert.False(map.IsSolid(arrival!.X, arrival.Y));

        Assert.True(SaveData.RebuiltAfter(room, SaveData.ImportedJubilife));
        Assert.True(SaveData.RebuiltAfter(room, 0));
        Assert.False(SaveData.RebuiltAfter(room, SaveData.CurrentWorld));

        // Wherever the old room's tile falls on the new plan (before its nurse, behind the new counter, in a wall)
        foreach (var (x, y) in new[] { (5, 4), (5, 7), (8, 4), (1, 3), (4, 6), (9, 2) })
        {
            var old = new SaveData { CurrentMapName = room, PlayerGridX = x, PlayerGridY = y, PlayerFacing = Direction.Left, WorldVersion = SaveData.ImportedJubilife };
            Assert.Equal(arrival, old.PlaceOn(MapDatabase.Get));
            // A file from before the field existed reads as the oldest
            var oldest = JsonSerializer.Deserialize<SaveData>($"{{\"CurrentMapName\":\"{room}\",\"PlayerGridX\":{x},\"PlayerGridY\":{y}}}")!;
            Assert.Equal(arrival, oldest.PlaceOn(MapDatabase.Get));
        }

        // A save made in the room as it is today stands where it stood
        var today = new SaveData { CurrentMapName = room, PlayerGridX = arrival.X, PlayerGridY = arrival.Y - 3, PlayerFacing = Direction.Right, WorldVersion = SaveData.CurrentWorld };
        Assert.Equal(new MapSpot(room, arrival.X, arrival.Y - 3, Direction.Right), JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(today))!.PlaceOn(MapDatabase.Get));
    }

    [Fact]
    public void ASaveOnATileNobodyCanStandOnWakesWhereItsRoomIsComeIntoWhateverItsAge()
    {
        Assert.Equal(SaveData.RebuiltRooms, SaveData.CurrentWorld);
        // In a wall, or off the room's edge: the one rule serves every room, rebuilt or not
        foreach (var (room, x, y, expected) in new[]
                 {
                     ("PokemonCenter", 0, 0, new MapSpot("PokemonCenter", 8, 12, Direction.Up)),
                     ("PokemonCenter", 40, 40, new MapSpot("PokemonCenter", 8, 12, Direction.Up)),
                     ("PokeMart", -1, 5, new MapSpot("PokeMart", 3, 11, Direction.Up)),
                     // A floor with no door of its own: beside the escalator that leads up to it, or down
                     ("PokemonCenter2F", 0, 0, new MapSpot("PokemonCenter2F", 3, 10, Direction.Right)),
                     ("PokemonCenterB1F", 0, 0, new MapSpot("PokemonCenterB1F", 13, 10, Direction.Left)),
                 })
        {
            var save = new SaveData { CurrentMapName = room, PlayerGridX = x, PlayerGridY = y, WorldVersion = SaveData.CurrentWorld };
            Assert.Equal(expected, save.PlaceOn(MapDatabase.Get));
        }

        // A room that was not laid out again keeps an old save's tile
        var upstairs = new SaveData { CurrentMapName = "PokemonCenter2F", PlayerGridX = 8, PlayerGridY = 8, PlayerFacing = Direction.Up, WorldVersion = SaveData.ImportedJubilife };
        Assert.Equal(new MapSpot("PokemonCenter2F", 8, 8, Direction.Up), upstairs.PlaceOn(MapDatabase.Get));

        // Outdoors nothing is asked: the map of Sinnoh, a cave, a lake, and the old maps the world replaced go as they went
        foreach (var save in new[]
                 {
                     new SaveData { CurrentMapName = "Sinnoh", PlayerGridX = 116, PlayerGridY = 886, PlayerFacing = Direction.Up, WorldVersion = SaveData.ImportedJubilife },
                     new SaveData { CurrentMapName = "LakeVerity", PlayerGridX = 46, PlayerGridY = 53, WorldVersion = SaveData.CurrentWorld },
                     new SaveData { CurrentMapName = "JubilifeCity", PlayerGridX = 19, PlayerGridY = 17, WorldVersion = SaveData.ImportedWorld },
                     new SaveData { CurrentMapName = "TwinleafTown", PlayerGridX = 3, PlayerGridY = 3 },
                     new SaveData { CurrentMapName = "NoSuchPlace", PlayerGridX = 3, PlayerGridY = 3, WorldVersion = SaveData.CurrentWorld },
                 })
            Assert.Equal(save.Place(), save.PlaceOn(MapDatabase.Get));
    }

    [Fact]
    public void EveryRoomWithADoorOntoSinnohHasATileToWakeOn()
    {
        int rooms = 0;
        foreach (string path in Directory.GetFiles(GameDataFiles.PathOf(MapDatabase.Folder), "*.json"))
        {
            string name = Path.GetFileNameWithoutExtension(path);
            var map = MapDatabase.Get(name);
            if (!map.IsIndoors || !RegionDatabase.RegionOfMap(name)!.Id.Equals(RegionDatabase.Sinnoh)) continue;
            var arrival = SaveData.ArrivalIn(map, MapDatabase.Get);
            if (arrival != null)
            {
                Assert.Equal(name, arrival.Map);
                Assert.True(map.InBounds(arrival.X, arrival.Y) && !map.IsSolid(arrival.X, arrival.Y), $"{name}: wakes in a wall at {arrival.X},{arrival.Y}");
                rooms++;
            }
            // A room no way leads back into (a lift's car a script rides) keeps the save's own tile
            var stuck = new SaveData { CurrentMapName = name, PlayerGridX = -1, PlayerGridY = -1, WorldVersion = SaveData.CurrentWorld };
            Assert.Equal(arrival ?? stuck.Place(), stuck.PlaceOn(MapDatabase.Get));
        }
        Assert.True(rooms >= 250, $"only {rooms} rooms have a tile to wake on");
    }
}
