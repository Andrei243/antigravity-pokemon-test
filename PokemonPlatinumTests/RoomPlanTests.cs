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
        new object[] { "ValleyWindworksBuilding", 1, 3, 12, 16 }
    };

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
        new object[] { "ValleyWindworksBuilding", "charon", 21, 5, Direction.Left }
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
        Assert.Equal("Sinnoh", Assert.Single(map.Warps).TargetMap);
        Assert.Equal((matX, matY + 1), (map.Warps[0].SourceX, map.Warps[0].SourceY));
        Assert.True(map.IsWalkable(matX, matY));
    }

    [Fact]
    public void EveryOtherRoomBeginsWhereTheHandMadeOnesDo()
    {
        var rebuilt = Plans.Select(p => (string)p[0]).ToHashSet();
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
