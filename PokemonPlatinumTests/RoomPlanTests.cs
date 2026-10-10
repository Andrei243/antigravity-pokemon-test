using System.Collections.Generic;
using System.IO;
using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumTests;

/// <summary>
/// Rooms rebuilt to the original's floor plans (the houses of plan 06 · R12's trades, plan 02 · S6's Valley Windworks):
/// the original's people, ways out, triggers and things read stand on the tiles its events give them, so a scene can be
/// written from the original's coordinates as they are.
/// </summary>
[Collection("MapDatabase")]
public class RoomPlanTests
{
    private static Map Room(string name) =>
        GameDataFiles.Load<MapFile>(Path.Combine(MapDatabase.Folder, name + ".json")).ToMap();

    /// <summary>Each room, where its floor begins, and the original's exit mat (where whoever comes in stands).</summary>
    public static IEnumerable<object[]> Plans => new[]
    {
        new object[] { "OreburghNorthHouse1F", 3, 3, 11, 12 },
        new object[] { "EternaCondominiums1F", 3, 3, 11, 12 },
        new object[] { "SnowpointWestHouse", 1, 3, 4, 8 },
        new object[] { "ValleyWindworksBuilding", 1, 3, 12, 16 },
        // Eterna City's rooms of plan 02 · S6, part 2
        new object[] { "EternaCycleShop", 1, 3, 7, 11 },
        new object[] { "EternaUndergroundManHouse", 1, 3, 4, 8 },
        new object[] { "TeamGalacticEternaBuilding1F", 1, 6, 11, 15 },
        // Hearthome City's of plan 02 · S7, part 1
        new object[] { "ContestHallLobby", 1, 3, 16, 13 },
        // Route 209's Lost Tower, plan 02 · S7, part 2
        new object[] { "LostTower1F", 1, 3, 7, 14 }
    };

    /// <summary>Gate houses rebuilt to the original's plans: a door in each side wall, both to the map of Sinnoh.</summary>
    internal static readonly string[] Gates = { "Route209GateToHearthomeCity" };

    /// <summary>The Team Galactic Eterna Building's upper floors: no door, only the stairs, and each cut in two (one
    /// staircase of each pair a trap that leads back down into the other part).</summary>
    internal static readonly string[] UpperFloors = { "TeamGalacticEternaBuilding2F", "TeamGalacticEternaBuilding3F", "TeamGalacticEternaBuilding4F" };

    /// <summary>The Lost Tower's floors from the ground up: the stairs up at the west end of the back wall, the stairs
    /// down at the east end (the original's <c>StairsEast</c> and <c>StairsWest</c>).</summary>
    internal static readonly string[] LostTower = { "LostTower1F", "LostTower2F", "LostTower3F", "LostTower4F", "LostTower5F" };

    // The original's people (res/field/events/events_<map>.json of the decompilation): local id, tile, facing
    public static IEnumerable<object[]> People => new[]
    {
        new object[] { "OreburghNorthHouse1F", "expert_f", 15, 8, Direction.Left },
        new object[] { "OreburghNorthHouse1F", "school_kid_f", 11, 7, Direction.Down },
        new object[] { "OreburghNorthHouse1F", "pokemon_breeder_m", 8, 10, Direction.Left },
        new object[] { "EternaCondominiums1F", "ninja_boy", 5, 5, Direction.Down },
        new object[] { "EternaCondominiums1F", "pokefan_m", 15, 8, Direction.Down },
        new object[] { "EternaCondominiums1F", "expert_m", 8, 10, Direction.Left },
        new object[] { "SnowpointWestHouse", "ace_trainer_m", 7, 6, Direction.Left },
        new object[] { "SnowpointWestHouse", "mindy", 5, 5, Direction.Down },
        new object[] { "ValleyWindworksBuilding", "galactic_grunt_1", 12, 14, Direction.Down },
        new object[] { "ValleyWindworksBuilding", "mars", 20, 7, Direction.Left },
        new object[] { "ValleyWindworksBuilding", "scientist_papa", 21, 3, Direction.Down },
        new object[] { "ValleyWindworksBuilding", "galactic_grunt_2", 3, 8, Direction.Left },
        new object[] { "ValleyWindworksBuilding", "galactic_grunt_3", 12, 3, Direction.Down },
        new object[] { "ValleyWindworksBuilding", "little_girl", 10, 8, Direction.Right },
        new object[] { "ValleyWindworksBuilding", "grunt_m", 18, 8, Direction.Down },
        new object[] { "ValleyWindworksBuilding", "charon", 21, 5, Direction.Left },
        new object[] { "EternaCycleShop", "pokefan_m", 3, 5, Direction.Down },
        new object[] { "EternaCycleShop", "youngster", 5, 10, Direction.Left },
        new object[] { "EternaUndergroundManHouse", "underground_man", 6, 5, Direction.Down },
        new object[] { "EternaUndergroundManHouse", "scientist_m", 3, 5, Direction.Down },
        new object[] { "EternaUndergroundManHouse", "youngster", 8, 7, Direction.Left },
        new object[] { "EternaUndergroundManHouse", "bug_catcher", 9, 6, Direction.Left },
        new object[] { "TeamGalacticEternaBuilding1F", "grunt_m_1", 3, 11, Direction.Down },
        new object[] { "TeamGalacticEternaBuilding1F", "grunt_m_2", 10, 11, Direction.Down },
        new object[] { "TeamGalacticEternaBuilding1F", "galactic_grunt_1", 16, 8, Direction.Left },
        new object[] { "TeamGalacticEternaBuilding1F", "galactic_grunt_2", 12, 8, Direction.Right },
        new object[] { "TeamGalacticEternaBuilding1F", "grunt_m_looker", 14, 15, Direction.Up },
        new object[] { "TeamGalacticEternaBuilding1F", "looker", 14, 15, Direction.Up },
        new object[] { "TeamGalacticEternaBuilding2F", "grunt_m", 7, 6, Direction.Up },
        new object[] { "TeamGalacticEternaBuilding2F", "grunt_f", 19, 8, Direction.Left },
        new object[] { "TeamGalacticEternaBuilding2F", "galactic_grunt_1", 3, 6, Direction.Up },
        new object[] { "TeamGalacticEternaBuilding2F", "galactic_grunt_2", 13, 8, Direction.Left },
        new object[] { "TeamGalacticEternaBuilding3F", "grunt_m", 12, 5, Direction.Right },
        new object[] { "TeamGalacticEternaBuilding3F", "galactic_grunt", 8, 7, Direction.Up },
        new object[] { "TeamGalacticEternaBuilding3F", "scientist_travon", 18, 5, Direction.Right },
        new object[] { "TeamGalacticEternaBuilding4F", "jupiter", 14, 6, Direction.Down },
        new object[] { "TeamGalacticEternaBuilding4F", "pokefan_m", 14, 9, Direction.Up },
        new object[] { "ContestHallLobby", "receptionist_official", 16, 4, Direction.Down },
        new object[] { "ContestHallLobby", "mom", 16, 10, Direction.Right },
        new object[] { "ContestHallLobby", "keira", 17, 10, Direction.Left },
        new object[] { "ContestHallLobby", "fantina", 22, 9, Direction.Left },
        new object[] { "ContestHallLobby", "rich_boy", 16, 6, Direction.Up },
        new object[] { "Route209GateToHearthomeCity", "battle_girl", 9, 4, Direction.Down },
        new object[] { "Route209GateToHearthomeCity", "rival", 8, 7, Direction.Left },
        new object[] { "LostTower1F", "pokemon_breeder_f_1", 8, 9, Direction.Down },
        new object[] { "LostTower1F", "pokemon_breeder_f_2", 2, 4, Direction.Right },
        new object[] { "LostTower2F", "youngster_oliver", 8, 10, Direction.Up },
        new object[] { "LostTower5F", "old_woman_1", 7, 9, Direction.Down },
        new object[] { "LostTower5F", "old_woman_2", 8, 9, Direction.Down }
    };

    [Theory]
    [MemberData(nameof(People))]
    public void TheOriginalsPeopleStandOnTheOriginalsTiles(string room, string id, int x, int y, Direction facing)
    {
        var map = Room(room);
        var npc = Assert.Single(map.Everyone, n => n.Key == id);
        Assert.Equal((x, y, facing), (npc.GridX, npc.GridY, npc.Facing));
        Assert.False(map.IsSolid(x, y), $"{room}: {id} stands in something solid");
    }

    [Theory]
    [MemberData(nameof(Plans))]
    public void TheFloorBeginsWhereTheOriginalsDoesAndTheWayOutIsBelowItsMat(string room, int left, int back, int matX, int matY)
    {
        var map = Room(room);
        Assert.True(map.IsIndoors);
        Assert.Equal((left, back), map.RoomCorner());
        // The door is in the front wall, the row under the original's exit mat
        Assert.Equal(map.Height - 1, matY + 1);
        Assert.Equal(TileType.Door, map.GetGroundTile(matX, matY + 1));
        var door = Assert.Single(map.Warps, w => w.TargetMap == "Sinnoh");
        Assert.Equal((matX, matY + 1), (door.SourceX, door.SourceY));
        Assert.True(map.IsWalkable(matX, matY));
    }

    [Fact]
    public void AGateHouseHasADoorInEachSideWallToTheMapOfSinnoh()
    {
        foreach (string name in Gates)
        {
            var map = Room(name);
            Assert.True(map.IsIndoors);
            var doors = map.Warps.Where(w => w.TargetMap == "Sinnoh").OrderBy(w => w.SourceX).ToList();
            Assert.Equal(2, doors.Count);
            Assert.Equal((0, map.Width - 1), (doors[0].SourceX, doors[1].SourceX));
            Assert.All(doors, d => Assert.Equal(TileType.Door, map.GetGroundTile(d.SourceX, d.SourceY)));
            Assert.Equal((Direction.Left, Direction.Right), (doors[0].TargetFacing, doors[1].TargetFacing));
        }
    }

    [Fact]
    public void TheLostTowersStairsJoinEachFloorToTheNextBesideTheStairs()
    {
        for (int i = 0; i < LostTower.Length - 1; i++)
        {
            Map below = Room(LostTower[i]), above = Room(LostTower[i + 1]);
            var up = Assert.Single(below.Warps, w => w.TargetMap == above.Name);
            var down = Assert.Single(above.Warps, w => w.TargetMap == below.Name);
            // Each comes out beside the other's stairs, walking away from them
            Assert.Equal((down.SourceX + 1, down.SourceY, Direction.Right), (up.TargetX, up.TargetY, up.TargetFacing));
            Assert.Equal((up.SourceX - 1, up.SourceY, Direction.Left), (down.TargetX, down.TargetY, down.TargetFacing));
            Assert.True(above.IsWalkable(up.TargetX, up.TargetY));
            Assert.True(below.IsWalkable(down.TargetX, down.TargetY));
            Assert.Contains(below.Props, p => p.Type == PropType.SideStairsUpEast && p.X == up.SourceX + 1);
            Assert.Contains(above.Props, p => p.Type == PropType.SideStairsDownWest && p.X == down.SourceX - 2);
        }
        // Wild Pokémon live on every floor, and the top is lost in fog
        Assert.All(LostTower, name => Assert.Equal(12, Room(name).WildEncounters.Count));
        Assert.All(LostTower, name => Assert.Equal(TileBehavior.OldChateauFloor, Room(name).BehaviourAt(7, 6)));
        Assert.Equal(FieldWeather.Fog, Room("LostTower5F").WeatherAt(7, 6));
        Assert.Equal(FieldWeather.Clear, Room("LostTower4F").WeatherAt(7, 6));
    }

    [Fact]
    public void EveryOtherRoomBeginsWhereTheHandMadeOnesDo()
    {
        var rebuilt = Plans.Select(p => (string)p[0]).Concat(UpperFloors).Concat(Gates).Concat(LostTower).ToHashSet();
        foreach (string path in Directory.GetFiles(GameDataFiles.PathOf(MapDatabase.Folder), "*.json"))
        {
            string name = Path.GetFileNameWithoutExtension(path);
            var map = Room(name);
            if (!map.IsIndoors || rebuilt.Contains(name) || map.Interior == InteriorStyle.Gym) continue; // Gyms: GymTests
            Assert.Equal((1, 2), map.RoomCorner());
            // No hand-made room has walls of its own inside it
            for (int ty = 2; ty < map.Height - 1; ty++)
                for (int tx = 1; tx < map.Width - 1; tx++)
                    Assert.NotEqual(TileType.Wall, map.GetGroundTile(tx, ty));
        }
    }

    [Fact]
    public void TheWindworksKeepsTheOriginalsTriggerTrainersAndComputers()
    {
        var map = Room("ValleyWindworksBuilding");

        // Commander Mars waits beside the original's trigger: a column two tiles deep in front of her
        var trigger = Assert.Single(map.Triggers);
        Assert.Equal((19, 6, 1, 2), (trigger.X, trigger.Y, trigger.Width, trigger.Depth));
        Assert.Equal(("Mars", "VAR_VALLEY_WINDWORKS_TEAM_GALACTIC_STATE", 1), (trigger.Script, trigger.Variable, trigger.Value));

        // The two grunts are Platinum's, with their teams, and see two tiles ahead
        foreach (var (id, trainer, species) in new[] { ("galactic_grunt_2", "galactic_grunt_valley_windworks_2", "Zubat"), ("galactic_grunt_3", "galactic_grunt_valley_windworks_3", "Glameow") })
        {
            var grunt = map.Everyone.Single(n => n.Key == id);
            Assert.True(grunt.IsTrainer);
            Assert.Equal((trainer, 2, species), (grunt.TrainerData!.Id, grunt.TrainerData.SightRange, grunt.TrainerData.Party.Members[0].Species.Name));
            Assert.Equal("FLAG_HIDE_VALLEY_WINDWORKS_BUILDING_TEAM_GALACTIC", grunt.HiddenBy);
        }

        // The two computers are read from the tile west of them, as the original's are
        foreach (var (x, y) in new[] { (19, 9), (22, 9) })
        {
            Assert.Equal("Notes", map.SignScripts[(x, y)]);
            Assert.True(map.IsSolid(x, y));
            Assert.True(map.IsWalkable(x - 1, y));
        }

        // Past the hall's doorway in the partition, the trigger can't be walked round: the desk and the grunt close
        // the way on either side of it
        Assert.False(map.IsWalkable(18, 5));
        Assert.Contains(map.Everyone, n => (n.GridX, n.GridY) == (18, 8));
    }

    // The original's stairs between the Team Galactic Eterna Building's floors: each pair of warps leads one into the
    // other, and whoever goes up or down is put beside the stairs at the far end, walking away from them
    public static IEnumerable<object[]> Stairs => new[]
    {
        new object[] { "TeamGalacticEternaBuilding1F", 14, 6, "TeamGalacticEternaBuilding2F", 3, 3 },
        new object[] { "TeamGalacticEternaBuilding1F", 20, 6, "TeamGalacticEternaBuilding2F", 8, 3 },
        new object[] { "TeamGalacticEternaBuilding2F", 14, 3, "TeamGalacticEternaBuilding3F", 2, 3 },
        new object[] { "TeamGalacticEternaBuilding2F", 20, 3, "TeamGalacticEternaBuilding3F", 8, 3 },
        new object[] { "TeamGalacticEternaBuilding3F", 14, 3, "TeamGalacticEternaBuilding4F", 3, 3 },
        new object[] { "TeamGalacticEternaBuilding3F", 20, 3, "TeamGalacticEternaBuilding4F", 8, 3 }
    };

    [Theory]
    [MemberData(nameof(Stairs))]
    public void TheEternaBuildingsStairsJoinItsFloorsAsTheOriginalsDo(string below, int ux, int uy, string above, int dx, int dy)
    {
        var lower = Room(below);
        var upper = Room(above);
        var up = lower.GetWarpAt(ux, uy)!;
        Assert.Equal((above, dx - 1, dy, Direction.Left), (up.TargetMap, up.TargetX, up.TargetY, up.TargetFacing));
        var down = upper.GetWarpAt(dx, dy)!;
        Assert.Equal((below, ux + 1, uy, Direction.Right), (down.TargetMap, down.TargetX, down.TargetY, down.TargetFacing));
        Assert.True(upper.IsWalkable(dx - 1, dy));
        Assert.True(lower.IsWalkable(ux + 1, uy));
        // The flights stand beside their warps: up to the west, down to the east
        Assert.Contains(lower.Props, p => p.Type == PropType.SideStairsUp && p.Covers(ux - 1, uy));
        Assert.Contains(upper.Props, p => p.Type == PropType.SideStairsDown && p.Covers(dx + 1, dy));
    }

    /// <summary>The tiles one can walk to on a floor from a tile, without its stairs, whoever stands about.</summary>
    private static HashSet<(int, int)> Reach(Map map, int x, int y)
    {
        var seen = new HashSet<(int, int)> { (x, y) };
        var queue = new Queue<(int X, int Y)>(seen);
        while (queue.Count > 0)
        {
            var (cx, cy) = queue.Dequeue();
            if (map.GetWarpAt(cx, cy) != null && (cx, cy) != (x, y)) continue;
            foreach (var (nx, ny) in new[] { (cx + 1, cy), (cx - 1, cy), (cx, cy + 1), (cx, cy - 1) })
                if (map.InBounds(nx, ny) && !map.IsSolid(nx, ny) && seen.Add((nx, ny))) queue.Enqueue((nx, ny));
        }
        return seen;
    }

    [Fact]
    public void OneOfEachFloorsTwoStaircasesIsATrap()
    {
        // Looker's warning: on each floor one way up comes out in a pocket with nothing but a grunt or two and an item,
        // whose only way on is back down, and the other comes out where the next way up is
        foreach (var (floor, trap, onward, next) in new[]
                 {
                     ("TeamGalacticEternaBuilding2F", (2, 3), (7, 3), (14, 3)),
                     ("TeamGalacticEternaBuilding3F", (7, 3), (1, 3), (14, 3)),
                     ("TeamGalacticEternaBuilding4F", (7, 3), (2, 3), (14, 7))
                 })
        {
            var map = Room(floor);
            Assert.DoesNotContain(next, Reach(map, trap.Item1, trap.Item2));
            Assert.Contains(next, Reach(map, onward.Item1, onward.Item2));
        }
        // Commander Jupiter stands where the right way leads, before the manager she holds
        var top = Room("TeamGalacticEternaBuilding4F");
        Assert.Equal((14, 6), (top.FindPerson("jupiter")!.GridX, top.FindPerson("jupiter")!.GridY));
    }

    [Fact]
    public void ARoomsInnerWallsArePaintedAsTheLowerPartOfItsWalls()
    {
        foreach (var style in new[] { InteriorStyle.House, InteriorStyle.Lab })
        {
            var whole = new PixelCanvas(32, PropModels.WallHeight);
            GroundBaker.PaintWall(whole, style);
            var inner = new PixelCanvas(32, PropModels.InnerWallHeight);
            GroundBaker.PaintWall(inner, style, PropModels.WallHeight - PropModels.InnerWallHeight);
            for (int y = 0; y < inner.Height; y++)
                for (int x = 0; x < 32; x++)
                    Assert.Equal(whole.Get(x, PropModels.WallHeight - PropModels.InnerWallHeight + y), inner.Get(x, y));
        }
    }
}
