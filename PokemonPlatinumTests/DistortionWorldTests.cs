using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Overworld;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 01 · M8, part 2: the Distortion World, a floor to a map, islands floating over nothing (the map setting
/// <see cref="MapSetting.Void"/>), joined where the original carries the player between floors and across its gaps.
/// </summary>
public class DistortionWorldTests
{
    // Maps of this class's own, so nothing another test does to the database's maps shows here. Read only.
    private static readonly Lazy<World> LoadedWorld = new(() => World.LoadAll().Single(w => w.Index.Region == "Sinnoh"));
    private static readonly Lazy<Dictionary<string, Map>> BuiltMaps = new(() => LoadedWorld.Value.BuildMaps().ToDictionary(m => m.Name));

    private static Map MapNamed(string name) => BuiltMaps.Value.TryGetValue(name, out var map) ? map : MapDatabase.Get(name);

    /// <summary>The floors the story walks down, in its order, and the room the Turnback Cave portal leads to.</summary>
    public static readonly string[] Floors =
    {
        "DistortionWorld1F", "DistortionWorldB1F", "DistortionWorldB2F", "DistortionWorldB3F", "DistortionWorldB4F",
        "DistortionWorldB5F", "DistortionWorldB6F", "DistortionWorldB7F", "DistortionWorldGiratinaRoom", "DistortionWorldTurnbackCaveRoom"
    };

    public static IEnumerable<object[]> EveryFloor => Floors.Select(f => new object[] { f });

    private static IEnumerable<(int X, int Y)> Tiles(Map map)
    {
        for (int y = 0; y < map.Height; y++)
            for (int x = 0; x < map.Width; x++)
                yield return (x, y);
    }

    /// <summary>The walk from the rift at Spear Pillar (and the portal in Turnback Cave), as the whole game's walk takes it.</summary>
    private static readonly Lazy<Dictionary<Map, HashSet<(int X, int Y)>>> Walked = new(() => WorldWalk.From(MapNamed, WorldWalk.StoryArrivals));

    [Theory]
    [MemberData(nameof(EveryFloor))]
    public void AFloorIsIslandsOverNothing(string name)
    {
        var map = BuiltMaps.Value[name];
        Assert.Equal(MapSetting.Void, map.Setting);
        Assert.True(map.IsVoid);
        Assert.False(map.IsCave);

        // The nothing blocks, and is drawn nowhere: no forest, no rock fills it as it would the open country or a cave
        var types = Tiles(map).Select(t => map.GetGroundTile(t.X, t.Y)).ToHashSet();
        // (B3F's islands stand out of a sea, which fills the nothing round them)
        Assert.True(types.Contains(TileType.Void) || name == "DistortionWorldB3F", $"{name} has no void");
        Assert.Contains(TileType.DistortionGround, types);
        Assert.DoesNotContain(TileType.CaveWall, types);
        Assert.All(Tiles(map).Where(t => map.GetGroundTile(t.X, t.Y) == TileType.Void), t => Assert.True(map.IsSolid(t.X, t.Y), $"{name}: the void at {t} can be walked into"));

        // Past the map's edge is nothing too, not forest (or B3F's sea, which runs on)
        Assert.Equal(name == "DistortionWorldB3F" ? TileType.Water : TileType.Void, GroundBaker.TypeAt(map, -3, -3));

        // The drop lies under the island beside it, so each island's edge shows its underside
        foreach (var (x, y) in Tiles(map))
        {
            if (map.GetGroundTile(x, y) != TileType.DistortionGround || map.IsSolid(x, y)) continue;
            if (!map.InBounds(x, y + 1) || map.GetGroundTile(x, y + 1) != TileType.Void) continue;
            Assert.Equal(MathF.Floor(map.HeightAt(x, y)) - WorldMapBuilder.VoidDrop, map.HeightAt(x, y + 1));
            break;
        }
    }

    [Theory]
    [MemberData(nameof(EveryFloor))]
    public void NobodyWalksOffAnIsland(string name)
    {
        var map = BuiltMaps.Value[name];
        Assert.True(Walked.Value.TryGetValue(map, out var reached), $"{name} isn't come to from the rift");
        // Everything the walk reaches is the islands: their stone, their slabs, their grass and their water
        foreach (var (x, y) in reached!)
        {
            var type = map.GetGroundTile(x, y);
            Assert.True(type is not (TileType.Void or TileType.Tree or TileType.Rock), $"{name}: ({x},{y}) is {type} and was walked onto");
            Assert.False(map.IsSolid(x, y), $"{name}: the walk stood on ({x},{y}), which blocks");
        }
        // Light, weather and darkness are the Distortion World's own
        Assert.Equal(FieldWeather.Clear, map.WeatherAt(reached.First().X, reached.First().Y));
        Assert.False(map.IsDark);
    }

    [Fact]
    public void TheFloorsJoinFromTheRiftDownToGiratinasRoom()
    {
        // Every floor is come to from where the rift at Spear Pillar puts the player, through the slabs, the walls
        // and the ferrying rocks
        var fromTheRift = WorldWalk.From(MapNamed, WorldWalk.StoryArrivals[0]);
        foreach (string floor in Floors[..^1])
            Assert.True(fromTheRift.ContainsKey(BuiltMaps.Value[floor]), $"{floor} isn't come to from the rift");

        // A slab leads to the slab it stops at, which leads back
        foreach (var map in Floors.Select(f => BuiltMaps.Value[f]))
            foreach (var warp in map.Warps)
            {
                var onto = MapNamed(warp.TargetMap);
                Assert.True(onto.InBounds(warp.TargetX, warp.TargetY), $"{map.Name}: the way at ({warp.SourceX},{warp.SourceY}) leads off {onto.Name}");
                Assert.False(onto.IsSolid(warp.TargetX, warp.TargetY), $"{map.Name}: the way at ({warp.SourceX},{warp.SourceY}) leads into something solid");
                // (the waterfall rises to the foot of B4F's wall, whose way down ends on the pool's shore)
                if (onto.GetWarpAt(warp.TargetX, warp.TargetY) is { } back && onto.IsVoid && !map.IsDeepWater(warp.SourceX, warp.SourceY))
                    Assert.Equal((map.Name, warp.SourceX, warp.SourceY), (back.TargetMap, back.TargetX, back.TargetY));
            }

        // 1F's slab sinks to B1F's, B7F leads on into Giratina's room and back
        Assert.Equal(("DistortionWorldB1F", 40, 19), Target(BuiltMaps.Value["DistortionWorld1F"], 19, 44));
        Assert.Equal(("DistortionWorld1F", 19, 44), Target(BuiltMaps.Value["DistortionWorldB1F"], 40, 19));
        Assert.Equal(("DistortionWorldGiratinaRoom", 15, 23), Target(BuiltMaps.Value["DistortionWorldB7F"], 15, 24));
        Assert.Equal(("DistortionWorldB7F", 15, 25), Target(BuiltMaps.Value["DistortionWorldGiratinaRoom"], 15, 25));

        // The original's warp out onto Mt. Coronet from a tile of the void is left shut
        Assert.Null(BuiltMaps.Value["DistortionWorld1F"].GetWarpAt(10, 43));
    }

    private static (string, int, int) Target(Map map, int x, int y) =>
        map.GetWarpAt(x, y) is { } warp ? (warp.TargetMap, warp.TargetX, warp.TargetY) : ("none", 0, 0);

    [Fact]
    public void AWaySomewhereIsASlabSetIntoTheIsland()
    {
        foreach (var map in Floors.Select(f => BuiltMaps.Value[f]))
        {
            foreach (var warp in map.Warps)
            {
                var type = map.GetGroundTile(warp.SourceX, warp.SourceY);
                Assert.True(type is TileType.DistortionSlab or TileType.Water, $"{map.Name}: the way at ({warp.SourceX},{warp.SourceY}) is {type}");
            }
            // Only those: the original's other marks are the island's stone like the rest
            Assert.All(Tiles(map).Where(t => map.GetGroundTile(t.X, t.Y) == TileType.DistortionSlab), t => Assert.NotNull(map.GetWarpAt(t.X, t.Y)));
        }
    }

    [Fact]
    public void B2FsUpperStonesFloatOverItsLowerOnes()
    {
        // The importer writes the original's floating floors into the chunks: B2F's upper stones, eight tiles over
        // the lower, where the original's moving rocks carry the player between them
        var map = BuiltMaps.Value["DistortionWorldB2F"];
        Assert.False(map.IsSolid(18, 15));
        Assert.Equal(map.HeightAt(18, 38) + 8f, map.HeightAt(18, 15));
        Assert.Equal(("DistortionWorldB2F", 16, 15), Target(map, 16, 23));
        Assert.Equal(("DistortionWorldB2F", 34, 33), Target(map, 34, 36));
    }

    [Fact]
    public void B3FsIslandsStandOutOfTheSeaUnderThem()
    {
        // Water that reaches the map's edge is the sea far under the islands, and fills the nothing round them
        var map = BuiltMaps.Value["DistortionWorldB3F"];
        Assert.Equal(TileType.Water, map.GetGroundTile(0, 0));
        Assert.True(map.IsSolid(0, 0));
        Assert.DoesNotContain(Tiles(map), t => map.GetGroundTile(t.X, t.Y) == TileType.Void && t.X is 0 or 63);
        Assert.True(map.HeightAt(18, 10) - map.HeightAt(0, 0) >= WorldMapBuilder.VoidDrop);
        // B5F's pool, closed in by rock, is surfed on at the islands' own height
        var b5f = BuiltMaps.Value["DistortionWorldB5F"];
        Assert.False(b5f.IsSolid(47, 43));
        Assert.True(b5f.IsDeepWater(47, 43));
        Assert.True(MathF.Abs(b5f.HeightAt(47, 43) - b5f.HeightAt(44, 33)) < FieldMovement.StepLimit);
    }

    [Theory]
    [MemberData(nameof(EveryFloor))]
    public void TheDistortionWorldHasItsOwnMusicAndLight(string name)
    {
        var map = BuiltMaps.Value[name];
        var spot = Walked.Value[map].First();
        Assert.Equal("sinnoh/distortion_world", map.BgmTrackAt(spot.X, spot.Y));
        Assert.True(File.Exists(GameDataFiles.PathOf(Path.Combine("music", "sinnoh", "distortion_world.mml"))));

        // Its light ignores the clock, and the void behind everything is its own violet
        foreach (float hour in new[] { 2f, 8f, 13f, 18f, 22f })
            Assert.Equal(ArtLook.VoidFieldRig, ArtLook.FieldRig(hour, map));
        var back = ArtLook.VoidFieldRig.Background;
        Assert.True(back.B > back.G && back.R > back.G && back.B < 80, $"the void is {back}");
    }

    [Fact]
    public void AGapIsJumpedTheWayItFacesToTheThirdTileOn()
    {
        // A column of stepping stones with a gap between, as the Distortion World's are: . v ^ . (south then north)
        var map = new Map(5, 9) { Name = "Lab" };
        for (int y = 0; y < 9; y++)
            for (int x = 0; x < 5; x++)
                map.SetGroundTile(x, y, TileType.Void, isSolid: true);
        foreach (int y in new[] { 1, 4, 7 }) map.SetGroundTile(2, y, TileType.DistortionGround, isSolid: false);
        map.SetBehaviour(2, 2, TileBehavior.LongLedgeSouth);
        map.SetBehaviour(2, 3, TileBehavior.LongLedgeNorth);
        map.SetBehaviour(2, 5, TileBehavior.LongLedgeSouth);
        map.SetBehaviour(2, 6, TileBehavior.LongLedgeNorth);

        var walker = new Walker(Height: map.HeightAt(2, 1));
        var down = FieldMovement.Step(map, 2, 1, Direction.Down, walker);
        Assert.Equal((StepKind.Jump, 2, 4), (down.Kind, down.X, down.Y));
        var up = FieldMovement.Step(map, 2, 4, Direction.Up, walker);
        Assert.Equal((StepKind.Jump, 2, 1), (up.Kind, up.X, up.Y));
        // Sideways it is the drop it is, and so is a gap with nothing to land on
        map.SetGroundTile(1, 2, TileType.DistortionGround, isSolid: false);
        Assert.Equal(Obstacle.Ledge, FieldMovement.Step(map, 1, 2, Direction.Right, walker).Obstacle);
        map.SetGroundTile(2, 7, TileType.Void, isSolid: true);
        Assert.Equal(Obstacle.Ledge, FieldMovement.Step(map, 2, 4, Direction.Down, walker).Obstacle);
        // Nobody surfs across one
        Assert.False(FieldMovement.Step(map, 2, 1, Direction.Down, walker with { Mode = TravelMode.Surfing }).Moves);
    }
}
