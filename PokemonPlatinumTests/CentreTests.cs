using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumTests;

/// <summary>
/// The centre of Sinnoh as plan 01 · M6 opened it: Route 205's north, Eterna Forest and Eterna City, the Cycling
/// Road and Routes 206 to 208 with Wayward Cave and Mt. Coronet's south, Hearthome City with Amity Square, Route
/// 209, Solaceon Town and its ruins; the gate houses walked through, the way into a forest, a matrix the original
/// uses for several rooms, and what the overlays put in them.
/// </summary>
public class CentreTests
{
    // Maps of this class's own, so nothing another test does to the database's maps shows here. Read only.
    private static readonly Lazy<World> LoadedWorld = new(() => World.LoadAll().Single(w => w.Index.Region == "Sinnoh"));
    private static readonly Lazy<Dictionary<string, Map>> BuiltMaps = new(() => LoadedWorld.Value.BuildMaps().ToDictionary(m => m.Name));

    private static World Sinnoh => LoadedWorld.Value;
    private static Map Overworld => BuiltMaps.Value["Sinnoh"];

    private static string AreaKey(Map map, int x, int y) => map.AreaAt(x, y)!.Key;

    // ------------------------------------------------------------------ gate houses

    [Theory]
    // Route 208 to Hearthome City and back, west to east
    [InlineData(447, 726, 455, 726, Direction.Right, "hearthome_city")]
    [InlineData(454, 726, 446, 726, Direction.Left, "route_208")]
    // Hearthome City to Route 209 and back
    [InlineData(505, 726, 513, 726, Direction.Right, "route_209")]
    [InlineData(512, 727, 504, 727, Direction.Left, "hearthome_city")]
    // Eterna City down to the Cycling Road's north end, and back
    [InlineData(304, 569, 304, 577, Direction.Down, "route_206")]
    [InlineData(305, 576, 305, 568, Direction.Up, "eterna_city")]
    // The Cycling Road's south gate, both ways
    [InlineData(302, 681, 302, 689, Direction.Down, "route_206")]
    [InlineData(303, 688, 303, 680, Direction.Up, "route_206")]
    public void AGateHouseIsWalkedThroughToItsFarSide(int x, int y, int toX, int toY, Direction facing, string area)
    {
        var map = Overworld;
        var warp = map.GetWarpAt(x, y);
        Assert.NotNull(warp);
        Assert.Equal(("Sinnoh", toX, toY, facing), (warp!.TargetMap, warp.TargetX, warp.TargetY, warp.TargetFacing));
        Assert.Equal(area, AreaKey(map, toX, toY));
        Assert.True(map.IsWalkable(toX, toY));
        Assert.Null(map.GetWarpAt(toX, toY));
    }

    [Fact]
    public void AmitySquareIsEnteredThroughItsGatesFromHearthomeCity()
    {
        var map = Overworld;
        var square = BuiltMaps.Value["AmitySquare"];
        foreach (var (x, y) in new[] { (458, 683), (459, 683), (500, 683), (501, 683) })
        {
            var into = map.GetWarpAt(x, y)!;
            Assert.Equal("AmitySquare", into.TargetMap);
            Assert.True(square.IsWalkable(into.TargetX, into.TargetY));
            // And out by the same gate, a step south of it
            var back = square.Warps.OrderBy(w => Math.Abs(w.SourceX - into.TargetX) + Math.Abs(w.SourceY - into.TargetY)).First();
            Assert.Equal("Sinnoh", back.TargetMap);
            Assert.Equal("hearthome_city", AreaKey(map, back.TargetX, back.TargetY));
            Assert.True(Math.Abs(back.TargetX - x) <= 1 && back.TargetY == y + 1);
        }
        Assert.Equal(FieldCamera.ZoomedIn, square.Camera);
    }

    // ------------------------------------------------------------------ the towns

    [Fact]
    public void EachNewTownHasACenterAndAMartOfItsOwn()
    {
        var map = Overworld;
        MapDatabase.Initialize();
        foreach (var (town, center, mart) in new[]
                 {
                     ("eterna_city", "EternaPokemonCenter", "EternaPokeMart"), ("hearthome_city", "HearthomePokemonCenter", "HearthomePokeMart"),
                     ("solaceon_town", "SolaceonPokemonCenter", "SolaceonPokeMart")
                 })
        {
            foreach (string room in new[] { center, mart })
            {
                var door = Assert.Single(map.Warps, w => w.TargetMap == room);
                Assert.Equal(town, AreaKey(map, door.SourceX, door.SourceY));
                var inside = MapDatabase.Get(room);
                Assert.True(inside.IsWalkable(door.TargetX, door.TargetY));
                var exit = Assert.Single(inside.Warps);
                Assert.Equal(("Sinnoh", door.SourceX, door.SourceY + 1), (exit.TargetMap, exit.TargetX, exit.TargetY));
                Assert.Equal(RegionDatabase.Sinnoh, RegionDatabase.RegionOfMap(room)!.Id);
            }
            Assert.Contains(MapDatabase.Get(center).NPCs, n => n.IsHealingNurse);
            Assert.Contains(MapDatabase.Get(mart).NPCs, n => n.IsPokeMartClerk);
        }

        // Each town is built in its own way
        Assert.Equal(Architecture.HalfTimber, map.ArchitectureAt(310, 545));
        Assert.Equal(Architecture.Townhouse, map.ArchitectureAt(480, 700));
        Assert.Equal(Architecture.Farm, map.ArchitectureAt(570, 660));
    }

    [Fact]
    public void ADoorOfAPlaceNotBuiltYetStaysShut()
    {
        var map = Overworld;
        // The Lost Tower, Hearthome's Contest Hall, Solaceon's Day Care (Mt. Coronet's upper floors, shut here until
        // plan 01 · M8, are open, and Eterna's Gym since M9)
        foreach (var (x, y) in new[] { (568, 680), (479, 691), (553, 645) })
        {
            Assert.Null(map.GetWarpAt(x, y));
            Assert.True(map.IsSolid(x, y), $"the way in at ({x},{y}) leads nowhere and can be walked into");
        }
        var forest = BuiltMaps.Value["EternaForest"];
        Assert.Null(forest.GetWarpAt(74, 15));   // the Old Chateau
        Assert.Contains(forest.PlacedBuildings!, b => b.Kind == BuildingKind.Mansion);
    }

    // ------------------------------------------------------------------ forests and caves

    [Fact]
    public void AForestIsEnteredThroughTheDarkUnderItsTrees()
    {
        var map = Overworld;
        // Route 205's way into Eterna Forest: the dark is walked where it is the way in
        var into = map.GetWarpAt(206, 581)!;
        Assert.Equal("EternaForest", into.TargetMap);
        int dark = 0;
        for (int y = 570; y < 590; y++)
            for (int x = 196; x < 216; x++)
                if (map.GetGroundTile(x, y) == TileType.ForestMouth) dark++;
        Assert.True(dark > 0, "no dark at the forest's way in");

        // The forest is seen with the zoomed-in camera and isn't a cave
        var forest = BuiltMaps.Value["EternaForest"];
        Assert.Equal(FieldCamera.ZoomedIn, forest.Camera);
        Assert.False(forest.IsCave);
        Assert.False(forest.IsDark);

        // The way out to Route 205's north half
        Assert.Contains(forest.Warps, w => w.TargetMap == "Sinnoh" && AreaKey(map, w.TargetX, w.TargetY) == "route_205_north");
    }

    [Fact]
    public void WaywardCaveIsTheDarkCave()
    {
        var upper = BuiltMaps.Value["WaywardCave1F"];
        var lower = BuiltMaps.Value["WaywardCaveB1F"];
        Assert.True(upper.IsCave && upper.IsDark);
        Assert.True(lower.IsCave);
        Assert.False(lower.IsDark);
        // Both of Route 206's ways in lead into it
        Assert.Equal(2, Overworld.Warps.Count(w => w.TargetMap == "WaywardCave1F"));

        var coronet = BuiltMaps.Value["MtCoronet1FSouth"];
        Assert.True(coronet.IsCave);
        Assert.Equal(FieldCamera.Cave, coronet.Camera);
        Assert.Contains(coronet.Warps, w => w.TargetMap == "Sinnoh" && AreaKey(Overworld, w.TargetX, w.TargetY) == "route_207");
        Assert.Contains(coronet.Warps, w => w.TargetMap == "Sinnoh" && AreaKey(Overworld, w.TargetX, w.TargetY) == "route_208");
    }

    [Fact]
    public void TheOutsideOfMtCoronetHasTheOriginalsCamera()
    {
        // CAMERA_TYPE_MT_CORONET_EXT_SOUTH: 866.6 units away, pitched 73.11°, half a vertical view of 6.33°
        Assert.Equal(FieldCamera.CoronetSouth, WorldMapBuilder.CameraOf("MtCoronetExtSouth"));
        Assert.Equal(FieldCamera.Default, WorldMapBuilder.CameraOf("MtCoronetExtNorth"));
        var view = MapScene.ViewOf(new Map(4, 4) { Camera = FieldCamera.CoronetSouth });
        Assert.Equal(73.11f, view.PitchDeg, 2);
        Assert.Equal(12.67f, view.FovYDeg, 2);
    }

    [Fact]
    public void TheEndOfABridgeIsNoCaveMouth()
    {
        // The original paints its cave's dark at the west end of Route 205's bridge: here it is the path
        var map = Overworld;
        for (int y = 530; y <= 532; y++)
        {
            Assert.NotEqual(TileType.CaveMouth, map.GetGroundTile(264, y));
            Assert.True(map.IsWalkable(264, y));
        }
    }

    // ------------------------------------------------------------------ the Solaceon Ruins

    [Fact]
    public void RoomsThatShareAMatrixAreMapsOfTheirOwn()
    {
        var shared = Sinnoh.Index.Maps.Where(m => m.Matrix == 31).Select(m => m.Name).ToList();
        Assert.Equal(new[] { "SolaceonRuinsRoom1SoutheastDeadEnd", "SolaceonRuinsRoom2SoutheastDeadEnd", "SolaceonRuinsRoom4SoutheastDeadEnd", "SolaceonRuinsRoom5SoutheastDeadEnd" },
            shared.OrderBy(n => n));

        // Each leads back into its own room, not the first room that uses the matrix
        foreach (var (deadEnd, room) in new[] { ("SolaceonRuinsRoom1SoutheastDeadEnd", "SolaceonRuinsRoom1"), ("SolaceonRuinsRoom4SoutheastDeadEnd", "SolaceonRuinsRoom4") })
        {
            var map = BuiltMaps.Value[deadEnd];
            Assert.All(map.Warps, w => Assert.Equal(room, w.TargetMap));
            Assert.Contains(BuiltMaps.Value[room].Warps, w => w.TargetMap == deadEnd);
        }

        // From Solaceon Town in, and out the same way
        var into = Overworld.Warps.Single(w => w.TargetMap == "SolaceonRuinsRoom1");
        Assert.Equal("solaceon_town", AreaKey(Overworld, into.SourceX, into.SourceY));
        Assert.True(BuiltMaps.Value["SolaceonRuinsRoom1"].IsCave);
    }

    [Fact]
    public void AnItemReachedOnlyFromAPlaceNotBuiltYetIsHeldBack()
    {
        // The Rare Candy on the ledge over the ruins is reached through Maniac Tunnel, which the Ruin Maniac digs (plan 02)
        Assert.DoesNotContain(Overworld.Everyone, n => n.IsItemBall && n.Item == "Rare Candy" && (n.GridX, n.GridY) == (594, 653));
        Assert.Contains(Overworld.Everyone, n => n.IsItemBall && AreaKey(Overworld, n.GridX, n.GridY) == "solaceon_town");
    }

    // ------------------------------------------------------------------ what stands there

    [Fact]
    public void AmitySquareHasItsFlowerBedsAndStones()
    {
        var square = BuiltMaps.Value["AmitySquare"];
        Assert.Contains(square.Props, p => p.Type == PropType.FlowerBed);
        Assert.Contains(square.Props, p => p.Type == PropType.Outcrop);
        Assert.DoesNotContain(WorldModels.All.Where(m => m.StandInUntil == "M6"), m => true);
    }

    [Fact]
    public void TheCentresTrainersArePlatinums()
    {
        var map = Overworld;
        var trainers = map.NPCs.Where(n => n.IsTrainer && AreaKey(map, n.GridX, n.GridY) is "route_206" or "route_207" or "route_208" or "route_209").ToList();
        Assert.True(trainers.Count >= 25, $"only {trainers.Count} trainers on Routes 206 to 209");
        Assert.All(trainers, t => Assert.True(t.TrainerData!.Party.Count > 0 && t.TrainerData.PrizeMoney > 0));

        // Eterna Forest's trainers think as Platinum's do and have its teams
        var forest = BuiltMaps.Value["EternaForest"];
        var jack = forest.NPCs.Single(n => n.TrainerData?.Name == "Jack");
        var record = TrainerDatabase.Get("bug_catcher_jack")!;
        Assert.Equal(record.Party.Select(p => (p.Species, p.Level)), jack.TrainerData!.Party.Members.Select(p => (p.Species.Name, p.Level)));
        Assert.Equal(record.PrizeMoney, jack.TrainerData.PrizeMoney);
    }
}
