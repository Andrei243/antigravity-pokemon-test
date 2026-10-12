using System.Text.Json;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumTests;

/// <summary>
/// The rooms of the first chapters, made by hand before the world was imported and laid out again on the original's
/// plans last of all (plan 01 · M11, the last rooms): the ground floors of the seventeen Pokémon Centers and the
/// thirteen Poké Marts, each town's a copy of one plan with its own people, and in Jubilife City the Trainers' School,
/// the Pokétch Company on its three floors and Jubilife TV's lobby, and in Twinleaf Town the rival's house on its two.
/// What stood on the hand-made tiles was
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
            bool speaks = who.DialogLines.Count > 0 || who.IsHealingNurse || who.IsPokeMartClerk || who.IsPCTerminal || who.IsItemBall
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

    // ------------------------------------------------------------------ Jubilife City's three

    [Fact]
    public void TheTrainersSchoolIsTheOriginalsPlanWithItsSceneOnTheOriginalsTiles()
    {
        var map = Room("TrainersSchool");
        Assert.Equal((17, 13), (map.Width, map.Height));
        Assert.Equal((1, 3), map.RoomCorner());
        LeadsInAndBackOut(map, 7, 11);
        EveryoneStandsOnFloorWithSomethingToSay(map);

        // The blackboard on the back wall, read from the two tiles before it; the rival stands on one of them until he leaves
        Assert.Contains(map.Props, p => p.Type == PropType.Blackboard && (p.X, p.Y, p.Width) == (5, 2, 2));
        Assert.False(new Prop { Type = PropType.Blackboard }.IsSolid);
        foreach (int x in new[] { 5, 6 })
        {
            Assert.NotNull(map.GetSignboardAt(x, 2));
            Assert.Equal("Blackboard", map.SignScripts[(x, 2)]);
            Assert.False(map.IsSolid(x, 3));
        }
        // The notebook lies on the pupils' table, and the X Attack by the west wall
        Assert.NotNull(map.GetSignboardAt(4, 9));
        Assert.Contains(map.Props, p => p.Type == PropType.Table && p.Covers(4, 9));
        Assert.Contains(map.Everyone, n => n.IsItemBall && (n.GridX, n.GridY, n.Item) == (1, 4, "X Attack"));
        // The east classroom is come into round the end of its wall
        Assert.All(Enumerable.Range(3, 6), y => Assert.True(map.IsSolid(10, y)));
        Assert.All(Enumerable.Range(9, 3), y => Assert.False(map.IsSolid(10, y)));
    }

    [Theory]
    // Spoken to from the south he runs round the player; from either side, straight down (the original's two movements)
    [InlineData(6, 4, Direction.Up)]
    [InlineData(7, 3, Direction.Left)]
    [InlineData(5, 3, Direction.Right)]
    public void TheRivalRunsFromTheBlackboardToTheSchoolsDoorWhereverThePlayerStands(int x, int y, Direction facing)
    {
        var map = Room("TrainersSchool");
        var rival = map.FindPerson("rival")!;
        Assert.Equal((6, 3), (rival.GridX, rival.GridY));
        Assert.True(map.IsWalkable(x, y));
        var host = new HeadlessScriptHost { Map = map, PlayerTile = (x, y), PlayerFacing = facing };
        host.Bag.AddItem(ItemDatabase.Get("Parcel")!, 1);
        Run("Rival", "TrainersSchool", host, rival);
        // He ends on the mat inside the door, never having walked through anything or anyone, and is gone
        Assert.Equal((7, 11), (rival.GridX, rival.GridY));
        Assert.DoesNotContain(rival, map.NPCs);
        Assert.True(host.Story.Has("FLAG_HIDE_TRAINERS_SCHOOL_RIVAL") && host.Story.Has("FLAG_TALKED_TO_TRAINERS_SCHOOL_RIVAL"));
        Assert.Equal(1, host.Story.Var("VAR_POKETCH_CAMPAIGN_STATE"));
        Assert.Equal(1, host.Bag.GetQuantity(ItemDatabase.Get("Town Map")!));
    }

    [Fact]
    public void ThePoketchCompanyHasTheOriginalsThreeFloorsAndTwoDoors()
    {
        var ground = Room("PoketchCompany");
        Assert.Equal((21, 14), (ground.Width, ground.Height));
        // Each of the city's two doors leads onto the mat inside its own, and each of the room's back out before its own
        var doors = BuiltSinnoh.Value.Warps.Where(w => w.TargetMap == "PoketchCompany").OrderBy(w => w.SourceX).ToList();
        var exits = ground.Warps.Where(w => w.TargetMap == "Sinnoh").OrderBy(w => w.SourceX).ToList();
        Assert.Equal(2, doors.Count);
        Assert.Equal(new[] { (3, 13), (16, 13) }, exits.Select(w => (w.SourceX, w.SourceY)));
        for (int i = 0; i < 2; i++)
        {
            Assert.Equal((exits[i].SourceX, exits[i].SourceY - 1, Direction.Up), (doors[i].TargetX, doors[i].TargetY, doors[i].TargetFacing));
            Assert.Equal((doors[i].SourceX, doors[i].SourceY + 1), (exits[i].TargetX, exits[i].TargetY));
            Assert.True(ground.IsWalkable(doors[i].TargetX, doors[i].TargetY));
        }

        // The floors are joined by their stairs both ways, each flight beside its warp, whoever takes one set down beside the other end
        string[] floors = RoomPlanTests.PoketchCompany;
        for (int i = 0; i < floors.Length - 1; i++)
        {
            Map below = Room(floors[i]), above = Room(floors[i + 1]);
            var up = Assert.Single(below.Warps, w => w.TargetMap == above.Name);
            var down = Assert.Single(above.Warps, w => w.TargetMap == below.Name);
            Assert.Equal((down.SourceX + 1, down.SourceY, Direction.Right), (up.TargetX, up.TargetY, up.TargetFacing));
            Assert.Equal((up.SourceX - 1, up.SourceY, Direction.Left), (down.TargetX, down.TargetY, down.TargetFacing));
            Assert.True(above.IsWalkable(up.TargetX, up.TargetY) && below.IsWalkable(down.TargetX, down.TargetY));
            Assert.Contains(below.Props, p => p.Type == PropType.SideStairsUpEast && p.X == up.SourceX + 1);
            Assert.Contains(above.Props, p => p.Type == PropType.SideStairsDownWest && p.X + p.Width == down.SourceX);
        }
        foreach (string name in floors)
        {
            var map = Room(name);
            Assert.Equal(RegionDatabase.Sinnoh, RegionDatabase.RegionOfMap(name)!.Id);
            Assert.Equal(ground.BgmTrack, map.BgmTrack);
            EveryoneStandsOnFloorWithSomethingToSay(map);
        }

        // The president before his family's counter, with the apps he hands out by the Badges (PoketchTests has the script)
        var president = Assert.Single(ground.Everyone, n => n.Name == "President");
        Assert.Equal((18, 5, "President", "FLAG_HIDE_POKETCH_CO_1F_POKETCH_CO_PRESIDENT"), (president.GridX, president.GridY, president.Script, president.HiddenBy));
        var receptionist = Assert.Single(ground.Everyone, n => n.Key == "receptionist");
        Assert.True(ground.IsCounter(receptionist.GridX, receptionist.GridY + 1) && ground.IsWalkable(receptionist.GridX, receptionist.GridY + 2));
        // The president's father and his two Pokémon upstairs
        Assert.Equal(new[] { "Clefairy", "Pachirisu" }, Room("PoketchCompany2F").Everyone.Where(n => n.Species != null).Select(n => n.Species).OrderBy(s => s));
        // The top floor's four PCs each tell of a handful of apps, read from the tile before them
        var top = Room("PoketchCompany3F");
        var guides = new[] { (1, 11, 0, 1), (9, 11, 0, 1), (15, 11, 0, 1), (1, 4, 1, 0) };
        foreach (var (gx, gy, dx, dy) in guides)
        {
            Assert.StartsWith("A guide to the Pokétch", top.GetSignboardAt(gx, gy));
            Assert.True(top.IsSolid(gx, gy) && top.IsWalkable(gx + dx, gy + dy), $"the guide at {gx},{gy} can't be read");
        }
        // Every app Platinum has is told of by one of them
        string told = string.Join(" ", guides.Select(g => top.GetSignboardAt(g.Item1, g.Item2)));
        foreach (string app in new[]
                 {
                     "Digital Watch", "Calculator", "Memo Pad", "Pedometer", "Pokémon List", "Friendship Checker", "Dowsing Machine", "Berry Searcher",
                     "Day-Care Checker", "Pokémon History", "Counter", "Analog Watch", "Marking Map", "Link Searcher", "Coin Toss", "Move Tester",
                     "Calendar", "Dot Artist", "Roulette", "Trainer Counter", "Kitchen Timer", "Color Changer", "Matchup Checker", "Stopwatch", "Alarm Clock",
                 })
            Assert.Contains(app, told);
    }

    [Fact]
    public void JubilifeTVsLobbyIsTheOriginalsPlanWithItsUpperFloorsRopedOff()
    {
        var map = Room("JubilifeTV1F");
        Assert.Equal((22, 20), (map.Width, map.Height));
        LeadsInAndBackOut(map, 9, 18);
        EveryoneStandsOnFloorWithSomethingToSay(map);

        // The Lottery Corner's clerk inside her desk, spoken to across it
        var clerk = Assert.Single(map.Everyone, n => n.Script == "LotteryClerk");
        Assert.Equal((9, 7, Direction.Down), (clerk.GridX, clerk.GridY, clerk.Facing));
        Assert.True(map.IsCounter(9, 8) && map.IsWalkable(9, 9));
        Assert.All(new[] { (7, 6), (7, 7), (7, 8), (8, 8), (9, 8), (10, 8), (11, 8), (11, 7), (11, 6), (8, 5), (9, 5), (10, 5) },
            t => Assert.True(map.IsSolid(t.Item1, t.Item2), $"{t} is a way in behind the desk"));

        // The stairs and the lift lead nowhere until the station's floors are built (plan 08 · P8): a rope and a card say so
        Assert.Single(map.Warps);
        Assert.Contains(map.Props, p => p.Type == PropType.SideStairsUp && p.Covers(13, 4));
        Assert.Contains("NO VISITORS", map.GetSignboardAt(13, 4));
        Assert.True(map.IsWalkable(14, 4));
        Assert.Contains("STAFF ONLY", map.GetSignboardAt(18, 3));
        Assert.True(map.IsSolid(18, 3) && map.IsWalkable(18, 4));
        Assert.Contains(map.Everyone, n => string.Join(" ", n.DialogLines).Contains("shut to visitors"));
    }

    // ------------------------------------------------------------------ the rival's house

    [Fact]
    public void TheRivalsHouseIsTheOriginalsPlanOnBothFloors()
    {
        var ground = Room("RivalHouse");
        Assert.Equal((13, 12, InteriorStyle.House), (ground.Width, ground.Height, ground.Interior));
        LeadsInAndBackOut(ground, 6, 10);
        EveryoneStandsOnFloorWithSomethingToSay(ground);
        var mom = Assert.Single(ground.Everyone);
        Assert.Equal((4, 6, "RivalMom"), (mom.GridX, mom.GridY, mom.Script));

        // The stairs in the north-west corner lead up to the head of the stairs in his room and back down beside their foot
        var room = Room("RivalHouse2F");
        Assert.Equal((12, 12, InteriorStyle.House), (room.Width, room.Height, room.Interior));
        var up = Assert.Single(ground.Warps, w => w.TargetMap == "RivalHouse2F");
        var down = Assert.Single(room.Warps);
        Assert.Equal(((2, 3), (4, 4, Direction.Right)), ((up.SourceX, up.SourceY), (up.TargetX, up.TargetY, up.TargetFacing)));
        Assert.Equal(("RivalHouse", (3, 4), (1, 3, Direction.Left)), (down.TargetMap, (down.SourceX, down.SourceY), (down.TargetX, down.TargetY, down.TargetFacing)));
        Assert.True(room.IsWalkable(4, 4) && ground.IsWalkable(1, 3));
        Assert.Contains(ground.Props, p => p.Type == PropType.SideStairsUpEast && (p.X, p.Y) == (3, 3));
        Assert.Contains(room.Props, p => p.Type == PropType.SideStairsDownWest && p.X + p.Width == down.SourceX);
        Assert.Equal(ground.BgmTrack, room.BgmTrack);

        // He stands at his desk, behind the flag that takes him out of the house for good
        var rival = Assert.Single(room.Everyone);
        Assert.Equal((9, 5, Direction.Up, "FLAG_HIDE_TWINLEAF_TOWN_RIVAL_HOUSE_2F_RIVAL"), (rival.GridX, rival.GridY, rival.Facing, rival.HiddenBy));
        // His console on the floor before the television and the note on his PC are read where the original's are
        Assert.Contains("console", room.GetSignboardAt(6, 4));
        Assert.Contains("Adventure Rule", room.GetSignboardAt(9, 4));
        Assert.True(room.IsWalkable(6, 5) && !room.IsSolid(9, 5));
    }

    [Fact]
    public void TheRivalRunsOutOfHisRoomByTheOriginalsWayAsThePlayerComesUpTheStairs()
    {
        var room = Room("RivalHouse2F");
        var rival = room.FindPerson("rival")!;
        // The player comes up onto the tile beside the stairs' head, looking into the room
        var host = new HeadlessScriptHost { Map = room, PlayerTile = (4, 4), PlayerFacing = Direction.Right };
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.In("RivalHouse2F", ScriptLibrary.OnEnter)!);
        runner.RunToEnd();
        Assert.Empty(host.Problems);
        // The player has stepped clear, and he has run round the room's furniture to the stairs and is gone
        Assert.Equal((4, 5), host.PlayerTile);
        Assert.Equal((3, 4), (rival.GridX, rival.GridY));
        Assert.DoesNotContain(rival, room.NPCs);
        Assert.True(host.Story.Has("FLAG_RIVAL_LEFT_HOME") && host.Story.Has("FLAG_HIDE_TWINLEAF_TOWN_RIVAL_HOUSE_2F_RIVAL"));
        Assert.Equal((1, 2), (host.Story.Var("VAR_RIVAL_HOUSE_STATE"), host.Story.Var("VAR_TWINLEAF_TOWN_GUITARIST_TRIGGER_STATE")));

        // Come up again, nothing plays
        var again = new HeadlessScriptHost(host.Story) { Map = room, PlayerTile = (4, 4), PlayerFacing = Direction.Right };
        var second = new ScriptRunner(ScriptLibrary.Default, again);
        second.Start(ScriptLibrary.Default.In("RivalHouse2F", ScriptLibrary.OnEnter)!);
        second.RunToEnd();
        Assert.Empty(again.Transcript);
        Assert.Equal((4, 4), again.PlayerTile);
    }

    // ------------------------------------------------------------------ the player's house (plan 01 · M11, the last step)

    // Plays a script on until a line that says something is on the screen, as someone reading it would: where everyone
    // stands at that moment is where the scene has brought them
    private static void ReadTo(ScriptRunner runner, HeadlessScriptHost host, string text)
    {
        for (int i = 0; i < 400 && !host.Transcript.Any(l => l.Text.Contains(text)); i++)
        {
            host.Busy = false;
            host.Walking = false;
            runner.Update(0.1f);
        }
        Assert.Contains(host.Transcript, l => l.Text.Contains(text));
        Assert.Empty(host.Problems);
    }

    private static void ReadToTheEnd(ScriptRunner runner, HeadlessScriptHost host)
    {
        host.Pauses = false;
        host.Busy = false;
        host.Walking = false;
        runner.RunToEnd();
        Assert.Empty(host.Problems);
    }

    [Fact]
    public void ThePlayersHouseIsTheOriginalsPlanOnBothFloors()
    {
        var ground = Room("PlayerHouse");
        Assert.Equal((13, 12, InteriorStyle.House), (ground.Width, ground.Height, ground.Interior));
        LeadsInAndBackOut(ground, 6, 10);
        // Mom on her cushion before the television, looking up at it; she rests the team
        var mom = ground.FindPerson("mom")!;
        Assert.Equal((7, 8, Direction.Up, "Mom", true), (mom.GridX, mom.GridY, mom.Facing, mom.Script, mom.IsHealingNurse));
        Assert.Contains(ground.Props, p => p.Type == PropType.Cushion && (p.X, p.Y) == (7, 8));
        Assert.Contains(ground.Props, p => p.Type == PropType.Television && (p.X, p.Y, p.Width) == (6, 5, 2));
        // The rival's mother comes in by the door: its own tile is hers, and the mat is nobody's
        var visitor = ground.FindPerson("rival_mom")!;
        Assert.Equal((6, 11, "FLAG_HIDE_TWINLEAF_TOWN_PLAYER_HOUSE_1F_RIVAL_MOM"), (visitor.GridX, visitor.GridY, visitor.HiddenBy));
        Assert.Equal(2, ground.Everyone.Count());
        // The original's coordinate event is on the mat itself, for the first time out
        var warning = Assert.Single(ground.Triggers);
        Assert.Equal(("DoorWarning", 6, 10, "VAR_PLAYER_HOUSE_STATE", 1), (warning.Script, warning.X, warning.Y, warning.Variable, warning.Value));
        // The kitchen is the nook in the north-west, behind its counter; each thing is read where the original's is
        Assert.True(ground.IsCounter(2, 5) && ground.IsSolid(2, 5) && ground.IsWalkable(2, 4) && ground.IsWalkable(2, 6));
        foreach (var (x, y, says) in new[] { (6, 5, "round-up"), (7, 5, "round-up"), (5, 3, "pudding"), (1, 3, "sink"), (2, 3, "sink"), (1, 5, "cooking"), (2, 5, "cooking"), (3, 5, "cooking") })
            Assert.Contains(says, ground.GetSignboardAt(x, y));
        Assert.Equal("TV", ground.SignScripts[(6, 5)]);
        Assert.Equal("TV", ground.SignScripts[(7, 5)]);
        Assert.DoesNotContain(ScriptLibrary.Default.Problems().ToList(), p => p.Contains("PlayerHouse", StringComparison.Ordinal));

        // The stairs in the north-east corner lead up to the head of the stairs in the bedroom and back down beside their foot
        var room = Room("PlayerHouse2F");
        Assert.Equal((12, 12, InteriorStyle.House), (room.Width, room.Height, room.Interior));
        var up = Assert.Single(ground.Warps, w => w.TargetMap == "PlayerHouse2F");
        var down = Assert.Single(room.Warps);
        Assert.Equal(((10, 3), (7, 4, Direction.Left)), ((up.SourceX, up.SourceY), (up.TargetX, up.TargetY, up.TargetFacing)));
        Assert.Equal(("PlayerHouse", (8, 4), (11, 3, Direction.Right)), (down.TargetMap, (down.SourceX, down.SourceY), (down.TargetX, down.TargetY, down.TargetFacing)));
        Assert.True(room.IsWalkable(7, 4) && ground.IsWalkable(11, 3));
        Assert.Contains(ground.Props, p => p.Type == PropType.SideStairsUp && p.X + p.Width == up.SourceX);
        Assert.Contains(room.Props, p => p.Type == PropType.SideStairsDown && p.X == down.SourceX + 1);
        Assert.Equal(ground.BgmTrack, room.BgmTrack);

        // The adventure begins before the television, on the original's tile, with its four coordinate events round it
        var start = RegionDatabase.Get(RegionDatabase.Sinnoh)!.Start!;
        Assert.Equal(new MapSpot("PlayerHouse2F", 4, 6, Direction.Up), start);
        Assert.True(room.IsWalkable(4, 6));
        Assert.Equal(new[] { (3, 6), (4, 5), (4, 7), (5, 6) }, room.Triggers.Select(t => (t.X, t.Y)).Order());
        Assert.All(room.Triggers, t => Assert.Equal(("RivalRushesIn", "VAR_PLAYER_HOUSE_RIVAL_STATE", 0), (t.Script, t.Variable, t.Value)));
        Assert.Null(FieldScripts.TriggerAt(room, 4, 6, new StoryState()));
        // The rival comes up the stairs: the tile at their head is his (the original stands him on the flight itself)
        var rival = Assert.Single(room.Everyone);
        Assert.Equal((8, 4, "FLAG_HIDE_TWINLEAF_TOWN_PLAYER_HOUSE_2F_RIVAL"), (rival.GridX, rival.GridY, rival.HiddenBy));
        // The computer with its seat and the poster over its desk, the television, and the wardrobe where the original's
        // console lies (the window has moved a tile along the wall to stand clear of it)
        Assert.Contains("computer", room.GetSignboardAt(1, 4));
        Assert.Contains("television", room.GetSignboardAt(3, 4));
        Assert.Contains("television", room.GetSignboardAt(4, 4));
        Assert.Contains("menu", room.GetSignboardAt(2, 4));
        Assert.Contains(room.Props, p => p.Type == PropType.Painting && (p.X, p.Y) == (2, 3));
        Assert.Equal("Wardrobe", room.SignScripts[(5, 4)]);
        Assert.Contains(room.Props, p => p.Type == PropType.Wardrobe && (p.X, p.Y) == (5, 4));
        Assert.True(room.IsSolid(5, 4) && room.IsWalkable(5, 5) && !room.IsSolid(1, 5));
    }

    [Fact]
    public void MomComesToTheFootOfTheStairsAndGoesBackToHerCushion()
    {
        var map = Room("PlayerHouse");
        var mom = map.FindPerson("mom")!;
        // Down the stairs for the first time: the player comes off them onto 11, 3
        var host = new HeadlessScriptHost { Map = map, PlayerTile = (11, 3), PlayerFacing = Direction.Right, Pauses = true };
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.In("PlayerHouse", ScriptLibrary.OnEnter)!);
        // She has come round the room to the tile below the player (the original's walk, a tile further east)
        ReadTo(runner, host, "was just here");
        Assert.Equal((11, 4), (mom.GridX, mom.GridY));
        Assert.Equal(((11, 3), Direction.Down), (host.PlayerTile, host.PlayerFacing));
        ReadToTheEnd(runner, host);
        Assert.Equal((7, 8, Direction.Up), (mom.GridX, mom.GridY, mom.Facing));
        Assert.Equal(1, host.Story.Var("VAR_PLAYER_HOUSE_STATE"));
        Assert.True(host.Story.Has("FLAG_TALKED_TO_MOM"));

        // On the mat, the first time out, she turns to call after the player; once
        host = new HeadlessScriptHost(host.Story) { Map = map, PlayerTile = (6, 10), PlayerFacing = Direction.Down };
        var trigger = FieldScripts.TriggerAt(map, 6, 10, host.Story);
        Assert.Equal("DoorWarning", trigger?.Script);
        Run("DoorWarning", "PlayerHouse", host);
        Assert.Contains("tall grass", Said(host));
        Assert.Equal((Direction.Up, Direction.Down), (host.PlayerFacing, mom.Facing));
        Assert.Equal(2, host.Story.Var("VAR_PLAYER_HOUSE_STATE"));
        Assert.Null(FieldScripts.TriggerAt(map, 6, 10, host.Story));
    }

    [Fact]
    public void BackFromTheFirstBattleMomLeadsThePlayerFromTheKitchenToTheCushions()
    {
        var map = Room("PlayerHouse");
        var mom = map.FindPerson("mom")!;
        // route_201.txt brings the player in before the kitchen counter, as the original does
        Assert.Contains(ScriptLibrary.Default.In("route_201", "Briefcase")!.Code,
            i => i.Op == Op.Warp && (i.Name, i.X, i.Y) == ("PlayerHouse", 2, 6));
        var host = new HeadlessScriptHost { Map = map, PlayerTile = (2, 6), PlayerFacing = Direction.Up, Pauses = true };
        host.Story.SetVar("VAR_PLAYER_HOUSE_STATE", 3);
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.In("PlayerHouse", ScriptLibrary.OnEnter)!);
        // She was at the sink behind the counter, and has led the way round to the cushions: the two sit side by side
        ReadTo(runner, host, "Welcome home");
        Assert.Equal((7, 8, Direction.Left), (mom.GridX, mom.GridY, mom.Facing));
        Assert.Equal(((6, 8), Direction.Right), (host.PlayerTile, host.PlayerFacing));
        ReadToTheEnd(runner, host);
        Assert.True(host.Story.Has(StoryState.RunningShoesFlag));
        Assert.Equal(4, host.Story.Var("VAR_PLAYER_HOUSE_STATE"));
        Assert.Equal((7, 8, Direction.Up), (mom.GridX, mom.GridY, mom.Facing));
    }

    [Theory]
    // The side of Mom the player speaks to her from: below her, above, to her east, to her west (the original's four ways)
    [InlineData(7, 9, Direction.Up, 6, 9)]
    [InlineData(7, 7, Direction.Down, 6, 7)]
    [InlineData(8, 8, Direction.Left, 8, 9)]
    [InlineData(6, 8, Direction.Right, 6, 9)]
    public void TheRivalsMotherBringsTheParcelToWhereverThePlayerStandsBesideMom(int x, int y, Direction facing, int herX, int herY)
    {
        var map = Room("PlayerHouse");
        var mom = map.FindPerson("mom")!;
        var visitor = map.FindPerson("rival_mom")!;
        var host = new HeadlessScriptHost { Map = map, PlayerTile = (x, y), PlayerFacing = facing, Pauses = true };
        host.Story.SetVar("VAR_PLAYER_HOUSE_STATE", 4);
        host.Story.Set(StoryState.PokedexFlag);
        host.Story.Set("FLAG_HIDE_TWINLEAF_TOWN_PLAYER_HOUSE_1F_RIVAL_MOM");
        map.ApplyPresence(host.Story.Has);
        Assert.DoesNotContain(visitor, map.NPCs);
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.In("PlayerHouse", "Mom")!, mom);
        // She comes in a step past the mat
        ReadTo(runner, host, "gone already");
        Assert.Equal((6, 9), (visitor.GridX, visitor.GridY));
        Assert.Contains(visitor, map.NPCs);
        // and over to the player, beside them, to hand the parcel over
        ReadTo(runner, host, "Would you really?");
        Assert.Equal((herX, herY), (visitor.GridX, visitor.GridY));
        Assert.Equal(1, Math.Abs(herX - x) + Math.Abs(herY - y));
        ReadToTheEnd(runner, host);
        Assert.Equal(1, host.Bag.GetQuantity(ItemDatabase.Get("Journal")!));
        Assert.Equal(1, host.Bag.GetQuantity(ItemDatabase.Get("Parcel")!));
        // She has gone out by the door she came in by, and Mom is back at her television
        Assert.Equal((6, 11), (visitor.GridX, visitor.GridY));
        Assert.DoesNotContain(visitor, map.NPCs);
        Assert.Equal((7, 8, Direction.Up), (mom.GridX, mom.GridY, mom.Facing));
        Assert.Equal((x, y), host.PlayerTile);
        Assert.Equal(6, host.Story.Var("VAR_PLAYER_HOUSE_STATE"));
        Assert.True(host.Story.Has("FLAG_RECEIVED_PARCEL"));
    }

    [Theory]
    [InlineData(TimeOfDay.Morning, "Cute")]
    [InlineData(TimeOfDay.Day, "Smart")]
    [InlineData(TimeOfDay.Twilight, "Smart")]
    [InlineData(TimeOfDay.Night, "Tough")]
    [InlineData(TimeOfDay.LateNight, "Tough")]
    public void TheTelevisionDownstairsShowsTheContestOfTheHour(TimeOfDay hour, string contest)
    {
        var map = Room("PlayerHouse");
        var host = new HeadlessScriptHost { Map = map, PlayerTile = (6, 6), PlayerFacing = Direction.Up, TimeOfDay = hour };
        Run("TV", "PlayerHouse", host);
        Assert.Contains($"Normal Rank {contest} Contest", Said(host));
        Assert.All(host.Transcript, l => Assert.Equal("TV", l.Speaker));
    }

    [Theory]
    // The tile the first step took the player onto: north, west, east, south of where the adventure begins
    [InlineData(4, 5)]
    [InlineData(3, 6)]
    [InlineData(5, 6)]
    [InlineData(4, 7)]
    public void TheRivalBurstsInRunsToTheComputerAndBackWhicheverWayThePlayerStepped(int x, int y)
    {
        var room = Room("PlayerHouse2F");
        var rival = room.FindPerson("rival")!;
        var host = new HeadlessScriptHost { Map = room, PlayerTile = (x, y), Pauses = true };
        StoryMigration.BeginNewGame(host.Story, ScriptLibrary.Default);
        room.ApplyPresence(host.Story.Has);
        Assert.DoesNotContain(rival, room.NPCs);
        var trigger = FieldScripts.TriggerAt(room, x, y, host.Story);
        Assert.Equal("RivalRushesIn", trigger?.Script);
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.In("PlayerHouse2F", "RivalRushesIn")!);

        // He calls from the head of the stairs
        ReadTo(runner, host, "Did you see it?");
        Assert.Equal((8, 4), (rival.GridX, rival.GridY));
        Assert.Contains(rival, room.NPCs);
        // runs up to stand east of the player, who turns to him
        ReadTo(runner, host, "tons of them");
        Assert.Equal((x + 1, y), (rival.GridX, rival.GridY));
        Assert.Equal(Direction.Right, host.PlayerFacing);
        // is distracted by the computer: he is on its seat, looking at it, and the player looks after him
        ReadTo(runner, host, "new computer");
        Assert.Equal((1, 5, Direction.Up), (rival.GridX, rival.GridY, rival.Facing));
        Assert.Equal(Direction.Left, host.PlayerFacing);
        ReadTo(runner, host, "What was I saying?");
        Assert.Equal(Direction.Right, rival.Facing);
        // comes back to the player's other side
        ReadTo(runner, host, "ten-million fine");
        Assert.Equal((x - 1, y, Direction.Right), (rival.GridX, rival.GridY, rival.Facing));
        // and runs round them to the stairs, never through the furniture, and is gone
        ReadToTheEnd(runner, host);
        Assert.Equal((8, 4), (rival.GridX, rival.GridY));
        Assert.DoesNotContain(rival, room.NPCs);
        Assert.Equal((x, y), host.PlayerTile);
        Assert.Equal(1, host.Story.Var("VAR_PLAYER_HOUSE_RIVAL_STATE"));
        Assert.True(host.Story.Has("FLAG_HIDE_TWINLEAF_TOWN_PLAYER_HOUSE_2F_RIVAL"));
        Assert.Null(FieldScripts.TriggerAt(room, x, y, host.Story));
    }

    [Fact]
    public void AWhiteOutAtHomeWakesOnTheTileEastOfMom()
    {
        // In the player's house that is the cushion beside hers, the original's own tile (sSpawnLocations: 8, 8)
        var home = Room("PlayerHouse");
        Assert.Equal((8, 8, Direction.Left), GameEngine.HomeSpot(home.FindPerson("mom")!));
        foreach (string house in new[] { "PlayerHouse", "PalletPlayerHouse" })
        {
            var map = Room(house);
            var mom = Assert.Single(map.Everyone, n => n.IsHealingNurse);
            var (x, y, _) = GameEngine.HomeSpot(mom);
            Assert.True(map.IsWalkable(x, y), $"{house}: nobody can wake at {x},{y}");
        }
    }

    // ------------------------------------------------------------------ Professor Rowan's lab (plan 01 · M11, the last step)

    [Fact]
    public void TheLabIsTheOriginalsPlan()
    {
        var lab = Room("RowanLab");
        Assert.Equal((22, 17, InteriorStyle.Lab), (lab.Width, lab.Height, lab.Interior));
        LeadsInAndBackOut(lab, 7, 15);
        EveryoneStandsOnFloorWithSomethingToSay(lab);
        // The professor at the head of his hall; his two assistants among the machines half-way down; the child who
        // helps him and Professor Oak a step inside the mat, each behind a flag
        var who = lab.Everyone.ToDictionary(n => n.Key);
        Assert.Equal(new[] { "counterpart", "prof_oak", "prof_rowan", "scientist_f", "scientist_m" }, who.Keys.Order());
        Assert.Equal((7, 5, Direction.Down, "Rowan"), (who["prof_rowan"].GridX, who["prof_rowan"].GridY, who["prof_rowan"].Facing, who["prof_rowan"].Script));
        Assert.Equal((3, 12, "ScientistM"), (who["scientist_m"].GridX, who["scientist_m"].GridY, who["scientist_m"].Script));
        Assert.Equal((10, 12, "ScientistF"), (who["scientist_f"].GridX, who["scientist_f"].GridY, who["scientist_f"].Script));
        Assert.Equal((7, 14, "FLAG_HIDE_SANDGEM_TOWN_LAB_COUNTERPART"), (who["counterpart"].GridX, who["counterpart"].GridY, who["counterpart"].HiddenBy));
        Assert.Equal((7, 14, "FLAG_HIDE_SANDGEM_TOWN_LAB_PROF_OAK"), (who["prof_oak"].GridX, who["prof_oak"].GridY, who["prof_oak"].HiddenBy));
        Assert.Null(who["prof_rowan"].HiddenBy);
        // He stands in the open and is spoken to from every side
        foreach (var (x, y) in new[] { (7, 6), (7, 4), (6, 5), (8, 5) }) Assert.True(lab.IsWalkable(x, y));
        // What is read along the back wall, where the original's is
        foreach (var (x, says) in new[] { (3, "Rule No. 1"), (4, "Rule No. 2"), (5, "crammed"), (6, "reports"), (11, "PC"), (20, "sweets") })
        {
            Assert.Contains(says, lab.GetSignboardAt(x, 2));
            Assert.True(lab.IsSolid(x, 2) && lab.IsWalkable(x, 3));
        }
        // The way up the lab from the door to the professor is clear
        Assert.All(Enumerable.Range(6, 10), y => Assert.False(lab.IsSolid(7, y) || lab.IsSolid(8, y) || lab.IsSolid(6, y)));

        // The plan is no rectangle: the hall at the back is the room's whole width, the length of the lab before it
        // only its west half, and beside that there is nothing (style guide, "Rooms")
        var outside = MapScene.OutsideThePlan(lab)!;
        for (int y = 8; y <= 15; y++)
            for (int x = 14; x <= 20; x++)
            {
                Assert.Equal(TileType.Void, lab.GetGroundTile(x, y));
                Assert.True(outside[x, y]);
            }
        Assert.False(outside[7, 10] || outside[13, 10] || outside[16, 5]);
        // Void that the floor runs up to is a well inside its room, with the room's walls whole round it (Pal Park's
        // gallery, open over the lobby); a house's void is always outside its plan (the Pokémon Mansion)
        Assert.Null(MapScene.OutsideThePlan(Room("PalParkGallery")));
        var mansion = Room("PokemonMansion");
        var beyond = MapScene.OutsideThePlan(mansion)!;
        for (int y = 0; y < mansion.Height; y++)
            for (int x = 0; x < mansion.Width; x++)
                Assert.Equal(mansion.GetGroundTile(x, y) == TileType.Void, beyond[x, y]);
        Assert.Null(MapScene.OutsideThePlan(Room("PlayerHouse")));
    }

    [Fact]
    public void TheAssistantLeadsThePlayerUpTheLabToThePokedex()
    {
        var lab = Room("RowanLab");
        var assistant = lab.FindPerson("counterpart")!;
        // sandgem_town.txt brings the two in at the door: the player on the mat, the assistant a step ahead
        Assert.Contains(ScriptLibrary.Default.In("sandgem_town", "MeetAssistant")!.Code,
            i => i.Op == Op.Warp && (i.Name, i.X, i.Y) == ("RowanLab", 7, 15));
        var host = new HeadlessScriptHost { Map = lab, PlayerTile = (7, 15), PlayerFacing = Direction.Up, Pauses = true };
        host.Story.SetVar("VAR_SANDGEM_TOWN_STATE", 1);
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.In("RowanLab", ScriptLibrary.OnEnter)!);
        // Up the length of the lab: the player before the professor, the assistant aside at his left hand
        ReadTo(runner, host, "Let me have a look");
        Assert.Equal((7, 6), host.PlayerTile);
        Assert.Equal((8, 6, Direction.Left), (assistant.GridX, assistant.GridY, assistant.Facing));
        ReadToTheEnd(runner, host);
        Assert.True(host.Story.Has(StoryState.PokedexFlag));
        // The assistant has walked back down to the door and out
        Assert.Equal((8, 15), (assistant.GridX, assistant.GridY));
        Assert.DoesNotContain(assistant, lab.NPCs);
        Assert.True(host.Story.Has("FLAG_HIDE_SANDGEM_TOWN_LAB_COUNTERPART"));
        Assert.Equal(((7, 6), 1), (host.PlayerTile, host.Story.Var("VAR_SANDGEM_TOWN_LAB_STATE")));
    }

    [Theory]
    // The side of the professor he was spoken to from: below him, above, to his east, to his west (the original's four ways)
    [InlineData(7, 6, Direction.Up)]
    [InlineData(7, 4, Direction.Down)]
    [InlineData(8, 5, Direction.Left)]
    [InlineData(6, 5, Direction.Right)]
    public void ProfessorOakComesUpTheLabWhereverTheProfessorWasSpokenToFrom(int x, int y, Direction facing)
    {
        var lab = Room("RowanLab");
        NPC rowan = lab.FindPerson("prof_rowan")!, oak = lab.FindPerson("prof_oak")!;
        var host = new HeadlessScriptHost { Map = lab, PlayerTile = (x, y), PlayerFacing = facing, SeenInSinnoh = 210, Pauses = true };
        host.Story.SetVar("VAR_SANDGEM_TOWN_LAB_STATE", 1);
        host.Story.SetVar("VAR_SANDGEM_TOWN_STATE", 2);
        foreach (string flag in new[] { "FLAG_GAME_COMPLETED", "FLAG_HIDE_SANDGEM_TOWN_LAB_PROF_OAK", "FLAG_HIDE_SANDGEM_TOWN_LAB_COUNTERPART" }) host.Story.Set(flag);
        lab.ApplyPresence(host.Story.Has);
        host.Answers.Enqueue(0);
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.In("RowanLab", "Rowan")!, rowan);
        // Professor Oak has come up from the door to stand below the professor, and the player has stepped aside to the west
        ReadTo(runner, host, "Good day to you");
        Assert.Equal((7, 7, Direction.Up), (oak.GridX, oak.GridY, oak.Facing));
        Assert.Equal(((6, 6), Direction.Right), (host.PlayerTile, host.PlayerFacing));
        Assert.Contains(oak, lab.NPCs);
        // He turns to the player, who is right above him
        ReadTo(runner, host, "upgrade that Pokédex");
        Assert.Equal((6, 7, Direction.Up), (oak.GridX, oak.GridY, oak.Facing));
        Assert.Equal(Direction.Down, host.PlayerFacing);
        // He has gone back down the lab and out, and the player is before the professor again for the Poké Radar
        ReadTo(runner, host, "This is for you");
        Assert.Equal((6, 15), (oak.GridX, oak.GridY));
        Assert.DoesNotContain(oak, lab.NPCs);
        Assert.Equal(((7, 6), Direction.Up), (host.PlayerTile, host.PlayerFacing));
        ReadToTheEnd(runner, host);
        Assert.True(host.NationalDex);
        Assert.True(host.Story.Has("FLAG_HIDE_SANDGEM_TOWN_LAB_PROF_OAK"));
        Assert.Equal(1, host.Bag.GetQuantity(ItemDatabase.Get("Poké Radar")!));
    }

    [Theory]
    // The original's three things each has to say: before Sunyshore City is reached with the professor's news heard
    // (the lab's state 3), with the National Pokédex, and on any other day
    [InlineData("scientist_m", 1, false, "father")]
    [InlineData("scientist_m", 3, false, "good to be alive")]
    [InlineData("scientist_m", 3, true, "good to be alive")]
    [InlineData("scientist_m", 4, true, "how Pokémon evolve")]
    [InlineData("scientist_f", 1, false, "Kanto")]
    [InlineData("scientist_f", 3, false, "cold all over")]
    [InlineData("scientist_f", 4, false, "Kanto")]
    [InlineData("scientist_f", 4, true, "junior")]
    public void TheProfessorsAssistantsSayWhatTheDayCallsFor(string who, int state, bool national, string says)
    {
        var lab = Room("RowanLab");
        var scientist = lab.FindPerson(who)!;
        var host = new HeadlessScriptHost { Map = lab, PlayerTile = (scientist.GridX, scientist.GridY + 1), PlayerFacing = Direction.Up, NationalDex = national };
        host.Story.SetVar("VAR_SANDGEM_TOWN_LAB_STATE", state);
        Run(scientist.Script!, "RowanLab", host, scientist);
        Assert.Contains(says, Said(host));
        Assert.Equal(Direction.Down, scientist.Facing);
        Assert.All(host.Transcript, l => Assert.Equal("Scientist", l.Speaker));
    }

    [Fact]
    public void ASaveMadeInTheBedroomBeforeItWasLaidOutAgainWakesWhereTheAdventureBegins()
    {
        var room = MapDatabase.Get("PlayerHouse2F");
        var start = RegionDatabase.Get(RegionDatabase.Sinnoh)!.Start!;
        // The room is come into beside the head of its stairs
        Assert.Equal(new MapSpot("PlayerHouse2F", 7, 4, Direction.Left), SaveData.ArrivalIn(room, MapDatabase.Get));
        Assert.True(SaveData.RebuiltAfter("PlayerHouse2F", SaveData.RebuiltRooms) && SaveData.RebuiltAfter("PlayerHouse2F", 0));
        Assert.False(SaveData.RebuiltAfter("PlayerHouse2F", SaveData.CurrentWorld));
        // but a game begins in it, and the room a game begins in is come into by the tile it begins on: a game saved
        // before the first step (the old room's 4, 4, a television's tile now) wakes there, with the rival's scene one
        // step away whichever way it is taken, where beside the stairs the scene could be walked round
        foreach (var (x, y) in new[] { (4, 4), (3, 3), (6, 5), (1, 4), (7, 4) })
        {
            var old = new SaveData { CurrentMapName = "PlayerHouse2F", PlayerGridX = x, PlayerGridY = y, PlayerFacing = Direction.Left, WorldVersion = SaveData.RebuiltRooms };
            Assert.Equal(start, old.PlaceOn(MapDatabase.Get));
            var oldest = JsonSerializer.Deserialize<SaveData>($"{{\"CurrentMapName\":\"PlayerHouse2F\",\"PlayerGridX\":{x},\"PlayerGridY\":{y}}}")!;
            Assert.Equal(start, oldest.PlaceOn(MapDatabase.Get));
        }
        var fresh = new StoryState();
        foreach (var (dx, dy) in new[] { (0, -1), (-1, 0), (1, 0), (0, 1) })
            Assert.Equal("RivalRushesIn", FieldScripts.TriggerAt(room, start.X + dx, start.Y + dy, fresh)?.Script);
        // A save of today's stands where it stood, and one in a wall wakes on the same tile whatever its age
        var today = new SaveData { CurrentMapName = "PlayerHouse2F", PlayerGridX = 7, PlayerGridY = 6, PlayerFacing = Direction.Down, WorldVersion = SaveData.CurrentWorld };
        Assert.Equal(new MapSpot("PlayerHouse2F", 7, 6, Direction.Down), today.PlaceOn(MapDatabase.Get));
        var walled = new SaveData { CurrentMapName = "PlayerHouse2F", PlayerGridX = 0, PlayerGridY = 0, WorldVersion = SaveData.CurrentWorld };
        Assert.Equal(start, walled.PlaceOn(MapDatabase.Get));
    }

    // ------------------------------------------------------------------ saves

    public static readonly string[] JubilifeRooms = { "TrainersSchool", "PoketchCompany", "JubilifeTV1F" };

    public static readonly string[] RivalsHouse = { "RivalHouse", "RivalHouse2F" };

    // The last two hand-made places (plan 01 · M11, the last step). The bedroom over the house is the room a game
    // begins in, and has a test of its own: ASaveMadeInTheBedroomBeforeItWasLaidOutAgainWakesWhereTheAdventureBegins
    public static readonly string[] HomeAndLab = { "PlayerHouse", "RowanLab" };

    public static IEnumerable<object[]> Rebuilt =>
        Centers.Concat(Marts.Select(m => m.Room)).Concat(JubilifeRooms).Concat(RivalsHouse).Concat(HomeAndLab).Select(name => new object[] { name });

    [Theory]
    [MemberData(nameof(Rebuilt))]
    public void ASaveMadeInARoomLaidOutAgainWakesOnTheTileTheRoomIsComeIntoBy(string room)
    {
        var map = MapDatabase.Get(room);
        var arrival = SaveData.ArrivalIn(map, MapDatabase.Get);
        Assert.NotNull(arrival);
        // The mat inside the door, looking in (the Pokétch Company has two doors: its west one, the first of its ways out)
        // A floor with no door of its own (the rival's room): beside the head of its stairs, as one comes up them
        var door = BuiltSinnoh.Value.Warps.Where(w => w.TargetMap == room).OrderBy(w => w.SourceX).FirstOrDefault();
        var expected = door != null ? new MapSpot(room, door.TargetX, door.TargetY, Direction.Up) : new MapSpot(room, 4, 4, Direction.Right);
        Assert.Equal(room == "RivalHouse2F", door == null);
        Assert.Equal(expected, arrival);
        Assert.False(map.IsSolid(arrival!.X, arrival.Y));

        Assert.True(SaveData.RebuiltAfter(room, SaveData.ImportedJubilife));
        Assert.True(SaveData.RebuiltAfter(room, 0));
        Assert.False(SaveData.RebuiltAfter(room, SaveData.CurrentWorld));
        // (the house and the lab were laid out again a version after the rest)
        Assert.Equal(HomeAndLab.Contains(room), SaveData.RebuiltAfter(room, SaveData.RebuiltRooms));

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
        var today = new SaveData { CurrentMapName = room, PlayerGridX = arrival.X, PlayerGridY = arrival.Y, PlayerFacing = Direction.Down, WorldVersion = SaveData.CurrentWorld };
        Assert.Equal(new MapSpot(room, arrival.X, arrival.Y, Direction.Down), JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(today))!.PlaceOn(MapDatabase.Get));
    }

    [Fact]
    public void ASaveOnATileNobodyCanStandOnWakesWhereItsRoomIsComeIntoWhateverItsAge()
    {
        Assert.Equal(SaveData.RebuiltHome, SaveData.CurrentWorld);
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
            // A room no way leads back into (a lift's car a script rides) keeps the save's own tile; the room a game
            // begins in is come into by the tile it begins on
            var stuck = new SaveData { CurrentMapName = name, PlayerGridX = -1, PlayerGridY = -1, WorldVersion = SaveData.CurrentWorld };
            var begins = RegionDatabase.Get(RegionDatabase.Sinnoh)!.Start!;
            Assert.Equal(name == begins.Map ? begins : arrival ?? stuck.Place(), stuck.PlaceOn(MapDatabase.Get));
        }
        Assert.True(rooms >= 250, $"only {rooms} rooms have a tile to wake on");
    }
}
