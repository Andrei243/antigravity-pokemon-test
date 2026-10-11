using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// The Pokémon League's rooms (plan 01 · M9, part 2d), each on the original's plan: the Pokémon Center with the door
/// and its guard, the five passages and their platform lifts as the original's code makes them work
/// (<c>src/platform_lift.c</c>), the rooms of the Elite Four with the doors that shut behind the challenger and open
/// once the room's master is beaten, the Champion's room, the hallway and the Hall of Fame, and the whole of it
/// walked in order with its scripts.
/// </summary>
[Collection("MapDatabase")]
public class LeagueTests
{
    private static readonly ScriptLibrary Scripts = ScriptLibrary.Default;

    private static Map Room(string name) =>
        GameDataFiles.Load<MapFile>(Path.Combine(MapDatabase.Folder, name + ".json")).ToMap();

    private const string Center = "PokemonLeagueNorthPokemonCenter", Hallway = "PokemonLeagueHallwayToHallOfFame", Hall = "PokemonLeagueHallOfFame",
        Champion = "PokemonLeagueChampionRoom";

    private static string Passage(string to) => $"PokemonLeagueElevatorTo{to}Room";
    private static string RoomOf(string who) => $"PokemonLeague{who}Room";

    private static readonly string[] Four = { "Aaron", "Bertha", "Flint", "Lucian" };

    /// <summary>The League from the Pokémon Center's door on, in the order it is walked.</summary>
    private static readonly string[] InOrder =
    {
        Passage("Aaron"), RoomOf("Aaron"), Passage("Bertha"), RoomOf("Bertha"), Passage("Flint"), RoomOf("Flint"),
        Passage("Lucian"), RoomOf("Lucian"), Passage("Champion"), Champion, Hallway, Hall
    };

    // ------------------------------------------------------------------ the rooms on the original's plans

    private static string[] Passage(int depth) =>
        new[] { "####.####" }.Concat(Enumerable.Repeat("#.......#", depth - 4)).Append("#########").ToArray();

    /// <summary>
    /// The original's rooms (<c>tools/MapImporter -- --room pokemon_league_&lt;room&gt;</c>, matrices 183 to 192 and 144),
    /// each from its row 2 down: '#' blocked, '.' open.
    /// </summary>
    private static readonly Dictionary<string, string[]> Plans = new()
    {
        [Passage("Aaron")] = Passage(17),
        [Passage("Bertha")] = Passage(17),
        [Passage("Flint")] = Passage(17),
        [Passage("Lucian")] = Passage(17),
        [Passage("Champion")] = Passage(25),
        [RoomOf("Aaron")] = new[]
        {
            "########.########",
            "####............#",
            "####.........#.##",
            "###..........#.##",
            "##...........#.##",
            "#............#.##",
            "#............#.##",
            "###.............#",
            "####............#",
            "####............#",
            "########.########"
        },
        [RoomOf("Bertha")] = new[]
        {
            "########.########",
            "#####.......#####",
            "#####.......#####",
            "#####.......#####",
            "#####.......#####",
            "#####.........###",
            "#####.........###",
            "####.........####",
            "#####........####",
            "#####........####",
            "########.########"
        },
        [RoomOf("Flint")] = Enumerable.Repeat("#####.......#####", 9).Prepend("########.########").Append("########.########").ToArray(),
        [RoomOf("Lucian")] = new[]
        {
            "########.########",
            "#...............#",
            "#..#.........#..#",
            "#..#.........#..#",
            "#...............#",
            "#...............#",
            "#...............#",
            "#..#.........#..#",
            "#..#.........#..#",
            "#...............#",
            "########.########"
        },
        [Champion] = new[] { "########.########" }
            .Concat(Enumerable.Repeat("#...............#", 6)).Concat(Enumerable.Repeat("#.##.........##.#", 3))
            .Concat(Enumerable.Repeat("#...............#", 3)).Concat(Enumerable.Repeat("#.##.........##.#", 3))
            .Append("#...............#").Append("#################").ToArray(),
        [Hallway] = new[] { "#####.#####" }
            .Concat(Enumerable.Repeat("#.........#", 3)).Concat(Enumerable.Repeat("#.#.....#.#", 2))
            .Concat(Enumerable.Repeat("#.........#", 4)).Concat(Enumerable.Repeat("#.#.....#.#", 2))
            .Concat(Enumerable.Repeat("#.........#", 4)).Concat(Enumerable.Repeat("#.#.....#.#", 2))
            .Concat(Enumerable.Repeat("#.........#", 4)).Append("###########").ToArray(),
        [Hall] = new[]
        {
            "#################",
            "#......###......#",
            "#......###......#",
            "#...............#",
            "#..#.........#..#",
            "#..#.........#..#",
            "#...............#",
            "#...............#",
            "#...............#",
            "#...............#",
            "#..#.........#..#",
            "#..#.........#..#",
            "#...............#",
            "#...............#",
            "#################"
        },
        // The Pokémon Center's hall: the counters (the original's table tiles) are blocked like the rest
        [Center] = new[]
        {
            "########.##.##########",
            "#........#...#.......#",
            "#....................#",
            "#....................#",
            "#....................#",
            "#....................#",
            "#....................#",
            "#........#...#..####.#",
            "#...............####.#",
            "###..................#"
        }
    };

    [Fact]
    public void TheLeaguesRoomsAreTheOriginalsTiles()
    {
        foreach (var (name, rows) in Plans)
        {
            var map = Room(name);
            Assert.Equal(rows[0].Length, map.Width);
            for (int r = 0; r < rows.Length; r++)
                for (int x = 0; x < rows[r].Length; x++)
                {
                    int y = 2 + r;
                    bool blocked = rows[r][x] == '#';
                    // The way into a room of the Four is in its front wall, which the door shuts as the challenger comes in:
                    // the room's own tile there is the wall
                    if (Four.Select(RoomOf).Contains(name) && (x, y) == (8, 12)) blocked = true;
                    // The Pokémon Center's escalator down (plan 01 · M11, part d): the original's two stand in an alcove
                    // south of the hall, which this room leaves out. The flight up takes the two tiles the original blocks
                    // in the hall's south-west corner; the flight down has no such tile and takes the hall's south-east one
                    if (name == Center && (x, y) == (20, 11)) blocked = true;
                    Assert.True(map.IsSolid(x, y) == blocked, $"{name}: ({x}, {y}) should be {(blocked ? "solid" : "open")}");
                }
            // Above the original's row 2 there is only wall, but in the Pokémon Center, whose nurse and clerks stand in row 1
            for (int y = 0; y < 2; y++)
                for (int x = 0; x < map.Width; x++)
                    if (name != Center || y == 0) Assert.True(map.IsSolid(x, y), $"{name}: ({x}, {y})");
            Assert.NotNull(RegionDatabase.RegionOfMap(name));
            Assert.Equal(RegionDatabase.Sinnoh, RegionDatabase.RegionOfMap(name)!.Id);
        }
        Assert.All(InOrder, name => Assert.Contains(name, Plans.Keys));
    }

    [Fact]
    public void ThePassagesAndTheChampionsRoomStandAtTheOriginalsHeights()
    {
        // Each passage: the upper floor from the door down to the row above the platform, the foot of the shaft below
        // (the original's plates; platform_lift.c's floorHeights)
        foreach (var (who, depth, top) in new[] { ("Aaron", 17, 10f), ("Bertha", 17, 5f), ("Flint", 17, 5f), ("Lucian", 17, 5f), ("Champion", 25, 5f) })
        {
            var map = Room(Passage(who));
            var lift = Assert.IsType<LeagueLift>(map.Puzzle);
            Assert.Equal((3, depth - 6, 0f, top), (lift.Where.X, lift.Where.Z, lift.Where.Bottom, lift.Where.Top));
            Assert.True(map.HasRelief);
            for (int y = 2; y < depth - 1; y++)
                for (int x = 1; x <= 7; x++)
                    if (!map.IsSolid(x, y)) Assert.Equal(y < lift.Where.Z ? top : 0f, map.HeightAt(x, y));
            Assert.Equal(HallStyle.League, map.Hall);
        }
        // The Champion's room: the gallery fifteen tiles up from the door to the row above the platform's
        var room = Room(Champion);
        var own = Assert.IsType<LeagueLift>(room.Puzzle);
        Assert.Equal((7, 8, 0f, 15f), (own.Where.X, own.Where.Z, own.Where.Bottom, own.Where.Top));
        for (int y = 2; y <= 18; y++)
            for (int x = 1; x <= 15; x++)
                if (!room.IsSolid(x, y)) Assert.Equal(y < 8 ? 15f : 0f, room.HeightAt(x, y));
        Assert.Equal(HallStyle.Champion, room.Hall);
        Assert.Equal(BattleArena.League, room.Arena);
        Assert.Null(room.ArenaType);
        // The rooms of the Four are themed on their types, as the League's stages are
        foreach (var (who, type) in new[] { ("Aaron", PokemonType.Bug), ("Bertha", PokemonType.Ground), ("Flint", PokemonType.Fire), ("Lucian", PokemonType.Psychic) })
        {
            var four = Room(RoomOf(who));
            Assert.Equal((BattleArena.League, (PokemonType?)type), (four.Arena, four.ArenaType));
        }
    }

    // The original's people (res/field/events/events_pokemon_league_<room>.json): id, tile, facing
    public static IEnumerable<object[]> People => new[]
    {
        new object[] { Center, "door_guard", 11, 3, Direction.Down },
        new object[] { RoomOf("Aaron"), "aaron", 8, 5, Direction.Down },
        new object[] { RoomOf("Bertha"), "bertha", 8, 5, Direction.Down },
        new object[] { RoomOf("Flint"), "flint", 8, 5, Direction.Down },
        new object[] { RoomOf("Lucian"), "lucian", 8, 5, Direction.Down },
        new object[] { RoomOf("Aaron"), "entrance_door", 8, 12, Direction.Up },
        new object[] { RoomOf("Aaron"), "exit_door", 8, 2, Direction.Up },
        new object[] { RoomOf("Lucian"), "entrance_door", 8, 12, Direction.Up },
        new object[] { RoomOf("Lucian"), "exit_door", 8, 2, Direction.Up },
        new object[] { Champion, "cynthia", 8, 11, Direction.Down },
        new object[] { Hallway, "cynthia", 5, 22, Direction.Up },
        new object[] { Hallway, "prof_rowan", 4, 22, Direction.Up },
        new object[] { Hall, "cynthia", 9, 6, Direction.Left },
        new object[] { Hall, "prof_rowan", 7, 6, Direction.Right }
    };

    [Theory]
    [MemberData(nameof(People))]
    public void TheOriginalsPeopleStandOnTheOriginalsTiles(string room, string id, int x, int y, Direction facing)
    {
        var who = Room(room).Everyone.Single(n => n.Key == id);
        Assert.Equal((x, y, facing), (who.GridX, who.GridY, who.Facing));
    }

    [Fact]
    public void ThePokemonCentersPeopleStandWhereTheOriginalsDo()
    {
        var map = Room(Center);
        // The nurse, the two cashiers (the common counter and the League's own), the trainer and the guitarist
        Assert.Equal((4, 1), map.Everyone.Where(n => n.IsHealingNurse).Select(n => (n.GridX, n.GridY)).Single());
        Assert.Equal(new[] { (16, 1), (17, 1) }, map.Everyone.Where(n => n.IsPokeMartClerk).Select(n => (n.GridX, n.GridY)).OrderBy(t => t).ToArray());
        Assert.Equal("pokemon_league", map.Everyone.Single(n => (n.GridX, n.GridY) == (17, 1)).Mart);
        Assert.Contains(map.Everyone, n => (n.GridX, n.GridY, n.Facing) == (6, 9, Direction.Right) && n.DialogLines.Count > 0);
        Assert.Contains(map.Everyone, n => (n.GridX, n.GridY, n.Facing) == (17, 8, Direction.Up) && n.DialogLines.Count > 0);
        // The way out is the tile below the original's mat at (11, 11), and leads out to the League's grounds and back
        var world = MapDatabase.Get("Sinnoh");
        var door = world.Warps.Single(w => w.TargetMap == Center);
        Assert.Equal((11, 11), (door.TargetX, door.TargetY));
        var back = Assert.Single(map.Warps, w => w.TargetMap == "Sinnoh");
        Assert.Equal((11, 12), (back.SourceX, back.SourceY));
        Assert.Equal((door.SourceX, door.SourceY + 1), (back.TargetX, back.TargetY));
        // A lost battle wakes the player in front of the nurse, the counter between them
        Assert.False(map.IsSolid(4, 3));
    }

    [Fact]
    public void TheDoorsAreThingsOfTheMapThatStandInTheirDoorways()
    {
        foreach (string who in Four)
        {
            var map = Room(RoomOf(who));
            var doors = map.Everyone.Where(n => n.IsLeagueDoor).ToList();
            Assert.Equal(2, doors.Count);
            Assert.All(doors, d =>
            {
                Assert.True(d.IsThing);
                Assert.Equal(PropType.LeagueDoor, d.GymThing);
                // The original's doors run a script that does nothing (CommonScript_Dummy2037)
                Assert.Null(FieldScripts.For(d));
            });
            string upper = who.ToUpperInvariant();
            Assert.Equal($"FLAG_HIDE_POKEMON_LEAGUE_{upper}_ROOM_ENTRANCE_DOOR", doors.Single(d => d.Key == "entrance_door").HiddenBy);
            Assert.Equal($"FLAG_HIDE_POKEMON_LEAGUE_{upper}_ROOM_EXIT_DOOR", doors.Single(d => d.Key == "exit_door").HiddenBy);
            // The door on stands on the warp itself, so nobody goes through while it is there
            var on = Assert.Single(map.Warps);
            Assert.Equal((8, 2), (on.SourceX, on.SourceY));
            Assert.NotNull(map.GetNpcAt(8, 2));
        }
    }

    [Fact]
    public void EachRoomLeadsOnToTheNextAsTheOriginalsDoAndNoneLeadsBack()
    {
        // warp at 11,2 of the Pokémon Center to the first passage's foot; each passage's door to a room's way in; each
        // room's door on to the next passage's foot; the Champion's gallery to the hallway and the hallway to the Hall
        var joins = new (string From, int X, int Y, string To, int ToX, int ToY)[]
        {
            (Center, 11, 2, Passage("Aaron"), 4, 15), (Passage("Aaron"), 4, 2, RoomOf("Aaron"), 8, 11),
            (RoomOf("Aaron"), 8, 2, Passage("Bertha"), 4, 15), (Passage("Bertha"), 4, 2, RoomOf("Bertha"), 8, 11),
            (RoomOf("Bertha"), 8, 2, Passage("Flint"), 4, 15), (Passage("Flint"), 4, 2, RoomOf("Flint"), 8, 11),
            (RoomOf("Flint"), 8, 2, Passage("Lucian"), 4, 15), (Passage("Lucian"), 4, 2, RoomOf("Lucian"), 8, 11),
            (RoomOf("Lucian"), 8, 2, Passage("Champion"), 4, 23), (Passage("Champion"), 4, 2, Champion, 8, 18),
            (Champion, 8, 2, Hallway, 5, 23), (Hallway, 5, 2, Hall, 8, 15)
        };
        foreach (var (from, x, y, to, toX, toY) in joins)
        {
            var map = Room(from);
            var warp = map.GetWarpAt(x, y);
            Assert.True(warp != null, $"{from} has no way on at ({x}, {y})");
            Assert.Equal((to, toX, toY, Direction.Up), (warp!.TargetMap, warp.TargetX, warp.TargetY, warp.TargetFacing));
            var onto = Room(to);
            Assert.False(onto.IsSolid(toX, toY));
            // Whoever arrives stands on no warp: the way they came by is shut behind them
            Assert.Null(onto.GetWarpAt(toX, toY));
        }
        // No room past the Pokémon Center has a way back, and the Hall of Fame none at all
        foreach (string name in InOrder)
            Assert.DoesNotContain(Room(name).Warps, w => w.TargetMap == Center || InOrder.Take(Array.IndexOf(InOrder, name)).Contains(w.TargetMap));
        Assert.Empty(Room(Hall).Warps);
    }

    [Fact]
    public void TheHallOfFamesFloorsMirrorWhoeverWalksOnThem()
    {
        foreach (string name in new[] { Hallway, Hall })
        {
            var map = Room(name);
            Assert.Equal(HallStyle.HallOfFame, map.Hall);
            for (int y = 3; y < map.Height - 1; y++)
                for (int x = 1; x < map.Width - 1; x++)
                    Assert.Equal(TileBehavior.ShinyFloor, map.BehaviourAt(x, y));
        }
        Assert.Equal(BehaviourSupport.Plain, TileBehaviors.InGame(TileBehavior.ShinyFloor).Support);
    }

    // ------------------------------------------------------------------ the platform lifts

    [Fact]
    public void APlatformRisesOnceAtTheOriginalsPaceAndNeverComesDown()
    {
        // sPerMapPlatformLiftConfiguration: the platform's first tile and its two floors, three tiles by two
        Assert.Equal(new[] { (3, 11, 10f), (3, 11, 5f), (3, 11, 5f), (3, 11, 5f), (3, 19, 5f), (7, 8, 15f) },
            LeagueLift.Shafts.Select(s => (s.X, s.Z, s.Top)).ToArray());
        Assert.All(LeagueLift.Shafts, s => Assert.Equal(0f, s.Bottom));
        Assert.Equal((3, 2), (LeagueLift.Width, LeagueLift.Depth));
        // FX32_ONE * 2 a frame, sixteen to the tile, at thirty frames a second
        Assert.Equal(3.75f, LeagueLift.TilesPerSecond);

        var lift = new LeagueLift(LeagueLift.AaronName);
        Assert.False(lift.AtTop);
        Assert.True(lift.NotUsedWhenEntered);
        Assert.True(lift.Covers(3, 11) && lift.Covers(5, 12) && !lift.Covers(2, 11) && !lift.Covers(4, 13) && !lift.Covers(4, 10));
        Assert.Equal(0f, lift.FloorAt(4, 11, 0f));
        Assert.Null(lift.FloorAt(4, 10, 0f));

        Assert.True(lift.Trigger());
        Assert.True(lift.Moving && lift.AtTop);
        lift.Update(1f);
        Assert.Equal(3.75f, lift.Height, 3);
        // The plate's own height changes only once the platform has stopped (DynamicTerrainHeightManager_SetHeight comes
        // last); whoever stands on it is held at the platform's height meanwhile (Player_SetYPos)
        Assert.Equal(0f, lift.Floor);
        Assert.Equal(lift.Height, lift.FloorAt(4, 11, 0f));
        float taken = 1f;
        for (; lift.Moving && taken < 10f; taken += 1f / 60f) lift.Update(1f / 60f);
        Assert.InRange(taken, 10f / 3.75f - 0.05f, 10f / 3.75f + 0.05f);
        Assert.Equal((10f, 10f), (lift.Height, lift.Floor));
        // preventGoingDown: at the top it stays
        Assert.False(lift.Trigger());
        Assert.False(lift.Moving);

        // Laid out by where the player comes in: at the foot from below, at the top from the door above or for a game
        // saved on the platform up there
        lift.StandAt(0f);
        Assert.True(!lift.AtTop && lift.NotUsedWhenEntered && lift.Floor == 0f);
        lift.StandAt(10f);
        Assert.True(lift.AtTop && !lift.NotUsedWhenEntered && lift.Floor == 10f);
    }

    [Theory]
    [InlineData("Aaron", 15)]
    [InlineData("Bertha", 15)]
    [InlineData("Flint", 15)]
    [InlineData("Lucian", 15)]
    [InlineData("Champion", 23)]
    public void APassagesPlatformCarriesTheChallengerFromItsFootToTheDoor(string who, int foot)
    {
        var game = new OpeningTests.Game(0);
        game.Arrive(Passage(who), 4, foot);
        var map = game.Map;
        var lift = (LeagueLift)map.Puzzle!;
        lift.ArriveAt(map, game.Story, new Random(1), 4, foot);
        Assert.False(lift.AtTop);
        int liftZ = lift.Where.Z;
        var trigger = Assert.Single(map.Triggers);
        Assert.Equal((4, liftZ, "VAR_MAP_LOCAL_0x00", 0), (trigger.X, trigger.Y, trigger.Variable, trigger.Value));

        // From the foot of the shaft the upper floor is out of reach, and the platform's middle is not
        var below = GymTests.Reach(map, 4, foot);
        Assert.Contains((4, liftZ), below);
        Assert.DoesNotContain((4, liftZ - 1), below);
        Assert.DoesNotContain((4, 2), below);

        // Stepping onto its middle sets it off, once (the trigger's variable is moved on)
        Assert.Contains("platformlift", game.Step("Lift").Log);
        Assert.True(lift.AtTop && lift.Floor == lift.Where.Top);
        Assert.False(game.Fires("Lift"));

        // At the top the way on is open to the door, and there is no stepping off the platform's sides or back down
        var above = GymTests.Reach(map, 4, liftZ, lift.Where.Top);
        Assert.Contains((4, 2), above);
        Assert.Contains((3, liftZ + 1), above);
        Assert.DoesNotContain((2, liftZ), above);
        Assert.DoesNotContain((4, liftZ + 2), above);
        Assert.Equal(RoomOf(who), map.GetWarpAt(4, 2)!.TargetMap);

        // Come in with the platform at the top, the trigger is dead (CheckPlatformLiftNotUsedWhenEnteredMap)
        lift.StandAt(lift.Where.Top);
        game.Play(Scripts.In(Passage(who), ScriptLibrary.OnEnter)!);
        Assert.False(game.Fires("Lift"));
        lift.StandAt(0f);
        game.Play(Scripts.In(Passage(who), ScriptLibrary.OnEnter)!);
        Assert.True(game.Fires("Lift"));
    }

    // ------------------------------------------------------------------ the guard

    private static OpeningTests.Game Challenger(int badges = 8, BattleOutcome fight = BattleOutcome.Won)
    {
        var game = new OpeningTests.Game(0, fight);
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Garchomp")!, 60));
        game.Story.SetBadges((1 << badges) - 1);
        return game;
    }

    [Fact]
    public void TheGuardKeepsTheDoorFromATrainerWithoutAllEightBadges()
    {
        var game = Challenger(badges: 7);
        game.Arrive(Center, 11, 11);
        var guard = game.Map.NPCs.Single(n => n.Key == "door_guard");
        var said = game.Talk("door_guard").Transcript;
        Assert.Contains(said, l => l.Text.Contains("eight"));
        Assert.Equal((11, 3), (guard.GridX, guard.GridY));
        Assert.False(game.Story.Has("FLAG_POKEMON_LEAGUE_DOOR_GUARD_MOVED_AWAY"));
        // He stands in the only way to the door
        Assert.DoesNotContain((11, 2), GymTests.Reach(game.Map, 11, 11));
    }

    [Theory]
    [InlineData(Direction.Up, 12, Direction.Down)]
    [InlineData(Direction.Right, 12, Direction.Left)]
    [InlineData(Direction.Left, 10, Direction.Right)]
    public void TheGuardSeesEightBadgesAndStepsOutOfTheDoorway(Direction playerFaces, int toX, Direction thenFaces)
    {
        var game = Challenger();
        game.Arrive(Center, 11, 11);
        var guard = game.Map.NPCs.Single(n => n.Key == "door_guard");
        game.Facing = playerFaces;
        var host = game.Talk("door_guard");
        Assert.Contains(host.Log, l => l.StartsWith("fanfare"));
        Assert.Equal((toX, 3, thenFaces), (guard.GridX, guard.GridY, guard.Facing));
        Assert.True(game.Story.Has("FLAG_POKEMON_LEAGUE_DOOR_GUARD_MOVED_AWAY"));
        Assert.Contains((11, 2), GymTests.Reach(game.Map, 11, 11));
        // Spoken to again he only wishes the challenger luck, and coming in again he stands east of the door
        Assert.DoesNotContain(game.Talk("door_guard").Log, l => l.StartsWith("fanfare"));
        game.Facing = Direction.Down;
        game.Arrive(Center, 11, 11);
        Assert.Equal((12, 3), (guard.GridX, guard.GridY));
        Assert.Contains((11, 2), GymTests.Reach(game.Map, 11, 11));
    }

    // ------------------------------------------------------------------ the rooms' scripts

    // Platinum's teams (res/trainers/data/elite_four_*.json, champion_cynthia.json)
    public static IEnumerable<object[]> Teams => new[]
    {
        new object[] { "elite_four_aaron", "Elite Four", new[] { "Yanmega", "Scizor", "Vespiquen", "Heracross", "Drapion" }, 53 },
        new object[] { "elite_four_bertha", "Elite Four", new[] { "Whiscash", "Gliscor", "Hippowdon", "Golem", "Rhyperior" }, 55 },
        new object[] { "elite_four_flint", "Elite Four", new[] { "Houndoom", "Flareon", "Rapidash", "Infernape", "Magmortar" }, 57 },
        new object[] { "elite_four_lucian", "Elite Four", new[] { "Mr. Mime", "Espeon", "Bronzong", "Alakazam", "Gallade" }, 59 },
        new object[] { "champion_cynthia", "Champion", new[] { "Spiritomb", "Roserade", "Togekiss", "Lucario", "Milotic", "Garchomp" }, 62 }
    };

    [Theory]
    [MemberData(nameof(Teams))]
    public void TheLeaguesTeamsArePlatinums(string id, string trainerClass, string[] species, int highest)
    {
        var record = TrainerDatabase.Get(id)!;
        Assert.Equal(trainerClass, record.Class);
        Assert.Equal(species, record.Party.Select(p => p.Species).ToArray());
        Assert.Equal(highest, record.Party.Max(p => p.Level));
        // Every Pokémon of theirs has its four moves chosen, and they carry Full Restores
        Assert.All(record.Party, p => Assert.Equal(4, p.Moves!.Count));
        Assert.Contains("Full Restore", record.Items);
    }

    [Theory]
    [InlineData("Aaron")]
    [InlineData("Bertha")]
    [InlineData("Flint")]
    [InlineData("Lucian")]
    public void ARoomOfTheFourShutsBehindTheChallengerAndOpensOnceItsMasterIsBeaten(string who)
    {
        string upper = who.ToUpperInvariant(), id = who.ToLowerInvariant();
        string entrance = $"FLAG_HIDE_POKEMON_LEAGUE_{upper}_ROOM_ENTRANCE_DOOR", exit = $"FLAG_HIDE_POKEMON_LEAGUE_{upper}_ROOM_EXIT_DOOR";
        var game = Challenger();
        // The Pokémon Center's own script makes the rooms ready
        game.Arrive(Center, 11, 11);
        Assert.True(game.Story.Has(entrance));
        Assert.False(game.Story.Has(exit));

        game.Arrive(RoomOf(who), 8, 11);
        var map = game.Map;
        // Two steps in, and the door has shut behind
        Assert.Equal((8, 9), game.Tile);
        Assert.Contains("sound lift_stop", game.Last.Log);
        Assert.False(game.Story.Has(entrance));
        Assert.Contains(map.NPCs, n => n.Key == "entrance_door");
        Assert.Contains(map.NPCs, n => n.Key == "exit_door");
        // The door on is shut: the room is all there is
        var reach = GymTests.Reach(map, 8, 9);
        Assert.DoesNotContain((8, 2), reach);
        Assert.Contains((8, 6), reach);
        Assert.All(reach, t => Assert.InRange(t.Y, 3, 11));

        var battle = game.Talk(id);
        Assert.Contains($"battle elite_four_{id} Won", battle.Log);
        Assert.True(game.Story.Has($"FLAG_DEFEATED_{upper}"));
        Assert.True(game.Story.Has(exit));
        Assert.DoesNotContain(map.NPCs, n => n.Key == "exit_door");
        Assert.Contains((8, 2), GymTests.Reach(map, 8, 9));
        // Spoken to again there is no second battle
        Assert.DoesNotContain(game.Talk(id).Log, l => l.StartsWith("battle"));

        // A game saved in the room comes back with the door shut and nobody walked anywhere
        game.Tile = (6, 7);
        game.Play(Scripts.In(RoomOf(who), ScriptLibrary.OnEnter)!);
        Assert.Equal((6, 7), game.Tile);

        // Coming into the Pokémon Center again puts the room back as a challenger finds it
        game.Arrive(Center, 11, 11);
        Assert.True(game.Story.Has(entrance) && !game.Story.Has(exit) && !game.Story.Has($"FLAG_DEFEATED_{upper}"));
        game.Arrive(RoomOf(who), 8, 11);
        Assert.Equal((8, 9), game.Tile);
        Assert.Contains(map.NPCs, n => n.Key == "exit_door");
        Assert.Contains(map.NPCs, n => n.Key == "entrance_door");
        Assert.DoesNotContain((8, 2), GymTests.Reach(map, 8, 9));
        Assert.Contains($"battle elite_four_{id} Won", game.Talk(id).Log);
    }

    [Fact]
    public void ABattleLostInARoomLeavesItsDoorShut()
    {
        var game = Challenger(fight: BattleOutcome.Lost);
        game.Arrive(Center, 11, 11);
        game.Arrive(RoomOf("Aaron"), 8, 11);
        var lost = game.Talk("aaron");
        Assert.Contains("battle elite_four_aaron Lost", lost.Log);
        Assert.False(game.Story.Has("FLAG_DEFEATED_AARON"));
        Assert.False(game.Story.Has("FLAG_HIDE_POKEMON_LEAGUE_AARON_ROOM_EXIT_DOOR"));
        Assert.Contains(game.Map.NPCs, n => n.Key == "exit_door");
    }

    [Fact]
    public void TheLeagueIsWalkedInOrderFromTheGuardToTheHallOfFame()
    {
        var game = Challenger();
        game.Arrive(Center, 11, 11);
        game.Facing = Direction.Up;
        game.Talk("door_guard");
        game.Facing = Direction.Down;

        foreach (string who in Four)
        {
            game.Through(Passage(who));
            Assert.Equal((4, 15), game.Tile);
            game.Step("Lift");
            game.Through(RoomOf(who));
            Assert.Equal((8, 9), game.Tile);
            Assert.Contains($"battle elite_four_{who.ToLowerInvariant()} Won", game.Talk(who.ToLowerInvariant()).Log);
        }
        game.Through(Passage("Champion"));
        Assert.Equal((4, 23), game.Tile);
        game.Step("Lift");

        // The Champion's room is one scene: Cynthia's battle, the platform, and on through the hallway, where she and
        // the professor come up, into the Hall of Fame; and from its scene, home (plan 02 · S14:
        // VictoryRoadAndTheHallOfFameTests holds the scene itself)
        int before = game.Played.Count;
        game.Through(Champion);
        var scenes = game.Played.Skip(before).Take(3).ToList();
        Assert.Equal(new[] { $"{Champion}.OnEnter", $"{Hallway}.OnEnter", $"{Hall}.OnEnter" }, scenes.Select(s => s.Script).ToArray());

        var champion = scenes[0].Host;
        Assert.Contains("battle champion_cynthia Won", champion.Log);
        Assert.Contains("platformlift", champion.Log);
        Assert.True(game.Story.Has("FLAG_DEFEATED_CYNTHIA"));
        Assert.True(((LeagueLift)game.Maps[Champion].Puzzle!).AtTop);
        // The first time she greets a challenger; the words name the player
        Assert.Contains(champion.Transcript, l => l.Speaker == "Cynthia" && l.Text.Contains("Champion"));

        var hallway = scenes[1].Host;
        Assert.Contains(hallway.Transcript, l => l.Speaker == "Professor Rowan");
        Assert.Contains(hallway.Transcript, l => l.Speaker == "Cynthia" && l.Text.Contains("young man"));
        Assert.DoesNotContain(hallway.Transcript, l => l.Text.Contains("young lady"));

        var hall = scenes[2].Host;
        // The team is entered in the Hall of Fame and healed, the region is done, and the player is home
        Assert.Equal(1, hall.HallOfFame.Total);
        Assert.Equal("Garchomp", hall.HallOfFame.Entries.Single().Team.Last().Species);
        Assert.True(game.Story.Has(RegionDatabase.Get(RegionDatabase.Sinnoh)!.StoryCompleteFlag));
        Assert.True(game.Story.Has("FLAG_GAME_COMPLETED"));
        Assert.False(game.Story.Has("FLAG_ALT_MUSIC_CHAMPION_ROOM"));
        Assert.False(game.Story.Has("FLAG_HIDE_TURNBACK_CAVE_GIRATINA_ROOM_GIRATINA"));
        Assert.Equal("PlayerHouse2F", game.Map.Name);
        var start = RegionDatabase.Get(RegionDatabase.Sinnoh)!.Start!;
        Assert.Equal((start.X, start.Y), game.Tile);

        // A Champion who comes back finds the Four waiting again, and Flint with other words
        game.Arrive(Center, 11, 11);
        game.Arrive(RoomOf("Flint"), 8, 11);
        Assert.Equal((8, 9), game.Tile);
        var again = game.Talk("flint");
        Assert.Contains("battle elite_four_flint Won", again.Log);
        Assert.DoesNotContain(again.Transcript, l => l.Text.Contains("Volkner"));
    }

    [Fact]
    public void TheGirlIsShownIntoTheHallOfFameAsOne()
    {
        var game = Challenger();
        game.Look = PlayerLook.Girl;
        game.Story.Set("FLAG_HIDE_POKEMON_LEAGUE_HALLWAY_TO_HALL_OF_FAME_CYNTHIA");
        game.Story.Set("FLAG_HIDE_POKEMON_LEAGUE_HALLWAY_TO_HALL_OF_FAME_PROF_ROWAN");
        int before = game.Played.Count;
        game.Arrive(Hallway, 5, 23);
        var hallway = game.Played[before].Host;
        Assert.Contains(hallway.Transcript, l => l.Text.Contains("young lady"));
        // Both came on where the scene put them and went in ahead of her
        Assert.False(game.Story.Has("FLAG_HIDE_POKEMON_LEAGUE_HALLWAY_TO_HALL_OF_FAME_CYNTHIA"));
        Assert.Equal("PlayerHouse2F", game.Map.Name);
    }

    [Fact]
    public void TheWholeLeagueCanBeWalkedFromTheGameStart()
    {
        // The walk of the whole game goes past the guard (whose own script steps him aside), up each shaft on its
        // platform and through each door of the Four (a thing in the way until its room is won), as far as the Hall
        var maps = MapDatabase.Fresh();
        var reached = WorldWalk.FromEveryStart(maps);
        foreach (string name in InOrder)
            Assert.True(reached.ContainsKey(maps(name)), $"{name} isn't reached");
        Assert.Contains((8, 5), reached[maps(Hall)]);
    }
    // ------------------------------------------------------------------ the art (style guide, "The League's rooms")

    private static PixelCanvas Painted(int width, int height, Action<PixelCanvas> paint)
    {
        var canvas = new PixelCanvas(width, height);
        paint(canvas);
        return canvas;
    }

    private static IEnumerable<Raylib_cs.Color> Texels(PixelCanvas c)
    {
        for (int y = 0; y < c.Height; y++)
            for (int x = 0; x < c.Width; x++)
                if (c.IsOpaque(x, y)) yield return c.Get(x, y);
    }

    [Fact]
    public void TheLeaguesArtIsPaintedInFlatShadesBelowWhite()
    {
        var pieces = new Dictionary<string, PixelCanvas>
        {
            ["door shut"] = Painted(30, 56, c => LeagueArt.PaintDoor(c, open: false)),
            ["door open"] = Painted(30, 56, c => LeagueArt.PaintDoor(c, open: true)),
            ["gate top"] = Painted(30, 18, LeagueArt.PaintGateTop),
            ["gate front"] = Painted(30, 16, LeagueArt.PaintGateFront),
            ["lift top"] = Painted(94, 62, LeagueArt.PaintLiftTop),
            ["lift side"] = Painted(94, 8, LeagueArt.PaintLiftSide),
            ["rail"] = Painted(4, 160, LeagueArt.PaintRail),
            ["carpet"] = Painted(92, 32, c => LeagueArt.PaintCarpet(c, Tone.Of(170, 44, 56), top: true, bottom: false)),
            ["pier"] = Painted(54, 76, LeagueArt.PaintPier),
            ["pier top"] = Painted(54, 82, LeagueArt.PaintPierTop),
            ["console top"] = Painted(80, 40, LeagueArt.PaintConsoleTop),
            ["console front"] = Painted(80, 20, LeagueArt.PaintConsoleFront),
            ["screen"] = Painted(60, 44, LeagueArt.PaintScreen),
            ["pilaster"] = Painted(8, 74, LeagueArt.PaintPilaster)
        };
        foreach (var stone in Enum.GetValues<LeagueArt.Stone>()) pieces[$"face {stone}"] = LeagueArt.Face(stone);
        for (int frame = 0; frame < 4; frame++)
            for (int q = 0; q < 4; q++)
            {
                int f = frame, qx = q % 2, qy = q / 2;
                pieces[$"lava {f} {qx} {qy}"] = Painted(32, 32, c => LeagueArt.PaintLava(c, f, qx, qy));
            }

        foreach (var (name, art) in pieces)
        {
            var texels = Texels(art).ToList();
            Assert.NotEmpty(texels);
            // Nothing near white, so the room's light never blooms a piece blank; and placed shapes in a few flat shades
            Assert.True(texels.All(t => Math.Min(t.R, Math.Min(t.G, t.B)) < 236), $"{name} has a texel near white");
            Assert.True(texels.Distinct().Count() <= 16, $"{name} is painted in {texels.Distinct().Count()} shades");
        }

        // A face is the field's own size, so it repeats down a shaft as the rock faces do
        Assert.All(Enum.GetValues<LeagueArt.Stone>(), s =>
            Assert.Equal((32, NatureArt.FaceCap + NatureArt.FaceBody), (LeagueArt.Face(s).Width, LeagueArt.Face(s).Height)));
        // An open door shows the dark beyond where a shut one shows its leaves; both keep the frame and the round head
        Assert.Equal(LeagueArt.Beyond, pieces["door open"].Get(15, 30));
        Assert.NotEqual(LeagueArt.Beyond, pieces["door shut"].Get(15, 30));
        Assert.Equal(pieces["door open"].Get(15, 1), pieces["door shut"].Get(15, 1));
        Assert.False(pieces["door open"].IsOpaque(0, 0));
        // The lava moves: no two frames of a quarter are the same, and every frame is the same lava (the molten orange)
        var lava = Enumerable.Range(0, 4).Select(f => string.Concat(Texels(pieces[$"lava {f} 0 0"]).Select(t => $"{t.R},{t.G},{t.B};"))).ToList();
        Assert.Equal(4, lava.Distinct().Count());
        Assert.All(Enumerable.Range(0, 4), f => Assert.Contains(new Raylib_cs.Color(226, 96, 36, 255), Texels(pieces[$"lava {f} 0 0"])));
    }
}
