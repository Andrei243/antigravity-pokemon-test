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
        // Hearthome City's Contest Hall, its lobby (plan 02 · S7)
        new object[] { "ContestHallLobby", 1, 3, 16, 13 }
    };

    /// <summary>The Team Galactic Eterna Building's upper floors: no door, only the stairs, and each cut in two (one
    /// staircase of each pair a trap that leads back down into the other part).</summary>
    internal static readonly string[] UpperFloors = { "TeamGalacticEternaBuilding2F", "TeamGalacticEternaBuilding3F", "TeamGalacticEternaBuilding4F" };

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
        new object[] { "ContestHallLobby", "receptionist_official_contest", 16, 4, Direction.Down },
        new object[] { "ContestHallLobby", "receptionist_link_contest", 5, 4, Direction.Down },
        new object[] { "ContestHallLobby", "receptionist_practice_contest", 27, 4, Direction.Down },
        new object[] { "ContestHallLobby", "ace_trainer_f", 28, 9, Direction.Left },
        new object[] { "ContestHallLobby", "clown", 25, 10, Direction.Down },
        new object[] { "ContestHallLobby", "mom", 16, 10, Direction.Right },
        new object[] { "ContestHallLobby", "keira", 17, 10, Direction.Left },
        new object[] { "ContestHallLobby", "school_kid_f", 4, 13, Direction.Left },
        new object[] { "ContestHallLobby", "rich_boy", 16, 6, Direction.Up },
        new object[] { "ContestHallLobby", "fantina", 22, 9, Direction.Left }
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
    public void EveryOtherRoomBeginsWhereTheHandMadeOnesDo()
    {
        var rebuilt = Plans.Select(p => (string)p[0]).Concat(UpperFloors).ToHashSet();
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

    // ------------------------------------------------------------------ rooms with relief (plan 01 · M9 1b)

    [Fact]
    public void OnlyTheRoomsRebuiltWithTheirReliefHaveAny()
    {
        // Every other hand-made map is flat, at the ground level, and does what its tiles' types say: it draws and is
        // walked exactly as before rooms could have heights
        var withRelief = new[] { "OreburghGym", "PastoriaGym" };
        foreach (var path in Directory.GetFiles(GameDataFiles.PathOf(MapDatabase.Folder), "*.json"))
        {
            string name = Path.GetFileNameWithoutExtension(path);
            var file = GameDataFiles.Load<MapFile>(Path.Combine(MapDatabase.Folder, name + ".json"));
            bool expected = withRelief.Contains(name);
            Assert.True(expected == (file.Heights != null), name);
            if (expected) continue;
            Assert.True(file.Behaviours == null && file.Slopes == null && file.Decks == null && file.GroundLevel == null, name);
            var map = file.ToMap();
            Assert.False(map.HasRelief, name);
            Assert.Equal(0f, map.GroundLevel);
            for (int y = 0; y < map.Height; y++)
                for (int x = 0; x < map.Width; x++)
                    Assert.Null(map.OwnBehaviourAt(x, y));
        }
    }

    [Fact]
    public void AMapFilesReliefReadsBackAsItWasWritten()
    {
        var map = new Map(4, 3) { Name = "Relief" };
        map.GroundLevel = 1f;
        map.SetHeight(0, 0, 0f);
        map.SetHeight(1, 0, 1f);
        map.SetHeight(2, 0, 1.5f, 0f, -1f);
        map.SetHeight(3, 0, 17.5f);
        map.SetHeight(1, 1, 2f, 1f, 0f);
        map.SetDeck(2, 2, 4f);
        map.SetBehaviour(3, 2, TileBehavior.PastoriaGymMiddle);

        var file = MapFile.FromMap(map);
        Assert.Equal(new[] { "023z", "0400", "0000" }, file.Heights);
        Assert.Equal(new[] { "..n.", ".e..", "...." }, file.Slopes);
        Assert.Equal(new[] { "....", "....", "..8." }, file.Decks);
        Assert.Equal(new[] { "........", "........", "......57" }, file.Behaviours);
        Assert.Equal(1f, file.GroundLevel);

        var back = GameDataFiles.Deserialize<MapFile>(GameDataFiles.Serialize(file)).ToMap();
        Assert.True(back.HasRelief);
        Assert.Equal(1f, back.GroundLevel);
        Assert.Equal((1.5f, (0f, -1f)), (back.HeightAt(2, 0), back.SlopeAt(2, 0)));
        Assert.Equal((2f, (1f, 0f)), (back.HeightAt(1, 1), back.SlopeAt(1, 1)));
        Assert.Equal(17.5f, back.HeightAt(3, 0));
        Assert.Equal(4f, back.DeckAt(2, 2));
        Assert.Null(back.DeckAt(1, 1));
        Assert.Equal(TileBehavior.PastoriaGymMiddle, back.BehaviourAt(3, 2));
        Assert.Null(back.OwnBehaviourAt(0, 0));
        Assert.Equal(GameDataFiles.Serialize(file), GameDataFiles.Serialize(MapFile.FromMap(back)));

        // Heights are whole halves of a tile; a layer of the wrong size, or slopes with no heights, is told
        Assert.Throws<System.ArgumentOutOfRangeException>(() => MapRelief.HeightCode(0.25f));
        file.Heights = new() { "023z", "0400" };
        Assert.Throws<InvalidDataException>(() => file.ToMap());
        file.Heights = null;
        Assert.Throws<InvalidDataException>(() => file.ToMap());
    }
}
