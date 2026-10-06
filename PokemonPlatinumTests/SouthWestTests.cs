using System.Text.Json;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumTests;

/// <summary>
/// The south-west of Sinnoh as plan 01 · M5 opened it: Jubilife City on the map of the region, the first caves
/// (their rock, their camera, their light, the dark), rock that stands up in the open country with the mouths of
/// the caves in it, the mine's conveyors, and what the areas' overlays put in them.
/// </summary>
public class SouthWestTests
{
    // Maps of this class's own, so nothing another test does to the database's maps shows here. Read only.
    private static readonly Lazy<World> LoadedWorld = new(() => World.LoadAll().Single(w => w.Index.Region == "Sinnoh"));
    private static readonly Lazy<Dictionary<string, Map>> BuiltMaps = new(() => LoadedWorld.Value.BuildMaps().ToDictionary(m => m.Name));
    private static readonly Lazy<Dictionary<Map, HashSet<(int X, int Y)>>> Walked =
        new(() => WorldWalk.From(MapNamed, RegionDatabase.Get(RegionDatabase.Sinnoh)!.Start!));

    private static World Sinnoh => LoadedWorld.Value;
    private static Map Overworld => BuiltMaps.Value["Sinnoh"];
    private static Map MapNamed(string name) => BuiltMaps.Value.TryGetValue(name, out var map) ? map : MapDatabase.Get(name);

    /// <summary>The tiles of the overworld's warps that lead nowhere yet.</summary>
    private static readonly Lazy<HashSet<(int X, int Y)>> ShutWays = new(() =>
        Sinnoh.Index.Areas.Select(Sinnoh.Area).Where(a => a!.Matrix == 0).SelectMany(a => a!.Warps)
            .Where(w => Overworld.GetWarpAt(w.X, w.Z) == null).Select(w => (w.X, w.Z)).ToHashSet());

    private static readonly string[] Caves = { "OreburghGate1F", "OreburghGateB1F", "OreburghMineB1F", "OreburghMineB2F", "RavagedPath" };

    private static IEnumerable<(int X, int Y)> Tiles(Map map)
    {
        for (int y = 0; y < map.Height; y++)
            for (int x = 0; x < map.Width; x++)
                yield return (x, y);
    }

    // ------------------------------------------------------------------ caves

    [Fact]
    public void ACaveIsRockWhereTheOpenCountryIsForest()
    {
        foreach (string name in Caves)
        {
            var map = BuiltMaps.Value[name];
            Assert.True(map.IsCave, $"{name} is not a cave");
            Assert.Equal(MapSetting.Cave, map.Setting);

            var types = Tiles(map).Select(t => map.GetGroundTile(t.X, t.Y)).Distinct().ToList();
            Assert.Contains(TileType.CaveWall, types);
            Assert.Contains(TileType.CaveFloor, types);
            Assert.True(!types.Contains(TileType.Tree) && !types.Contains(TileType.Grass) && !types.Contains(TileType.TallGrass) && !types.Contains(TileType.Dirt),
                $"{name} has {string.Join(", ", types)}: lawn, trees or earth in a cave");
            // Rock blocks the way, whatever lies under it in the world's data
            Assert.All(Tiles(map).Where(t => map.GetGroundTile(t.X, t.Y) == TileType.CaveWall), t => Assert.True(map.IsSolid(t.X, t.Y)));

            // Whatever isn't blocked can be come to: the space beyond the walls, which the original leaves open
            // and empty, is rock here. (A ledge is hopped over, never stood on; people stand in their own way.)
            var reached = Walked.Value[map];
            foreach (var (x, y) in Tiles(map))
            {
                if (map.IsSolid(x, y) || map.GetNpcAt(x, y) != null || FieldMovement.LedgeDirection(map.BehaviourAt(x, y)) != null) continue;
                Assert.True(reached.Contains((x, y)), $"{name}: the open tile ({x},{y}) can't be come to, and isn't rock");
            }
        }

        // The meadow behind Floaroma Town is a matrix of its own, and no cave: lawn and flowers under the sky
        var meadow = BuiltMaps.Value["FloaromaMeadow"];
        Assert.False(meadow.IsCave);
        Assert.True(Tiles(meadow).Count(t => meadow.GetGroundTile(t.X, t.Y) == TileType.FlowerGrass) > 1000, "the meadow has no flowers");
        Assert.DoesNotContain(Tiles(meadow), t => meadow.GetGroundTile(t.X, t.Y) is TileType.CaveWall or TileType.CaveFloor);
    }

    [Fact]
    public void ACavesWallsStandAboveItsFloor()
    {
        var gate = BuiltMaps.Value["OreburghGate1F"];
        bool IsRock(int x, int y) => gate.GetGroundTile(x, y) == TileType.CaveWall;

        // The gate's floor is all on one level
        float floor = Assert.Single(Tiles(gate).Where(t => !IsRock(t.X, t.Y)).Select(t => gate.HeightAt(t.X, t.Y)).Distinct());
        var rises = Tiles(gate).Where(t => IsRock(t.X, t.Y)).Select(t => gate.HeightAt(t.X, t.Y) - floor).Distinct().OrderBy(r => r).ToList();
        Assert.Equal(new[] { WorldMapBuilder.CaveLipRise, WorldMapBuilder.CaveWallRise }, rises);

        // Rock right in front of a floor is a lip, so it hides no more than the feet of whoever walks behind it;
        // everything else stands the full height
        foreach (var (x, y) in Tiles(gate).Where(t => IsRock(t.X, t.Y)))
        {
            bool floorBehind = gate.InBounds(x, y - 1) && !IsRock(x, y - 1);
            Assert.Equal(floor + (floorBehind ? WorldMapBuilder.CaveLipRise : WorldMapBuilder.CaveWallRise), gate.HeightAt(x, y));
        }

        // The lip is a step with a face, not a slope: the floor beside it is drawn flat
        Assert.True(WorldMapBuilder.CaveLipRise >= Relief.WeldLimit);
        Assert.All(Tiles(gate).Where(t => !IsRock(t.X, t.Y)), t =>
        {
            var c = Relief.Corners(gate, t.X, t.Y);
            Assert.True(c.NW == c.NE && c.NE == c.SW && c.SW == c.SE, $"the floor at ({t.X},{t.Y}) is drawn sloping: {c}");
        });
    }

    [Fact]
    public void ACaveHasItsOwnCameraAndALightThatIgnoresTheClock()
    {
        var gate = BuiltMaps.Value["OreburghGate1F"];
        Assert.Equal(FieldCamera.Cave, gate.Camera);
        var view = MapScene.ViewOf(gate);
        Assert.Equal((MapScene.CavePitchDeg, MapScene.CaveFovYDeg, MapScene.CaveDistance), (view.PitchDeg, view.FovYDeg, view.Distance));
        Assert.True(view.PitchDeg > FieldView.Outdoor.PitchDeg && view.Distance < FieldView.Outdoor.Distance, "the cave's camera is nearer and steeper");
        Assert.Equal(FieldView.Outdoor, MapScene.ViewOf(Overworld));

        // The original's numbers (src/overlay005/field_camera.c): distances in sixteenths of a tile, half angles
        Assert.Equal(574.578f / 16f, MapScene.CaveDistance, 3);
        Assert.Equal(63.26f, MapScene.CavePitchDeg, 2);
        Assert.Equal(19.0f, MapScene.CaveFovYDeg, 1);

        // The meadow is looked at from nearer
        var meadow = BuiltMaps.Value["FloaromaMeadow"];
        Assert.Equal(FieldCamera.ZoomedIn, meadow.Camera);
        Assert.Equal(MapScene.ZoomedPitchDeg, MapScene.ViewOf(meadow).PitchDeg);

        // One light at every hour underground; the open country's follows the clock
        Assert.Equal(ArtLook.FieldRig(3f, gate), ArtLook.FieldRig(13f, gate));
        Assert.Equal(ArtLook.CaveFieldRig, ArtLook.FieldRig(21f, gate));
        Assert.NotEqual(ArtLook.FieldRig(3f, Overworld), ArtLook.FieldRig(13f, Overworld));

        // No weather comes in, though the floor below the gate is under a cloudy header
        var below = BuiltMaps.Value["OreburghGateB1F"];
        Assert.Equal("Cloudy", Sinnoh.Area("oreburgh_gate_b1f")!.Weather);
        Assert.Equal(FieldWeather.Clear, below.WeatherAt(46, 6));
    }

    [Fact]
    public void ADarkCaveShowsACircleUntilFlashLightsIt()
    {
        // None of the south-west's caves is dark (the original's one is Wayward Cave)
        Assert.All(Caves, name => Assert.False(BuiltMaps.Value[name].IsDark));

        // Dark until Flash is used in it (plan 02 · S2: knowing the move isn't enough)
        var cave = new Map(8, 8) { Name = "Dark", Setting = MapSetting.Cave, IsDark = true };
        Assert.True(Darkness.Covers(cave));
        cave.Lit = true;
        Assert.False(Darkness.Covers(cave));
        Assert.False(Darkness.Covers(new Map(8, 8) { Name = "Lit", Setting = MapSetting.Cave }));

        // The circle: seen as it is to 2.2 tiles, gone at 3.4, and darker all the way between
        Assert.Equal(0f, Darkness.At(0f));
        Assert.Equal(0f, Darkness.At(Darkness.Radius));
        Assert.Equal(1f, Darkness.At(Darkness.Reach));
        Assert.Equal(1f, Darkness.At(20f));
        Assert.Equal(0.5f, Darkness.At(Darkness.Radius + Darkness.Soft / 2f), 3);
        for (float d = Darkness.Radius; d < Darkness.Reach; d += 0.1f) Assert.True(Darkness.At(d + 0.1f) >= Darkness.At(d));

        // Knowing the move is what lets the party menu offer it
        var party = new Party();
        party.Add(new Pokemon(PokemonDatabase.Get("Shinx")!, 12));
        Assert.False(FieldMovement.MovesOf(party).HasFlag(FieldMoves.Flash));
        party.Members[0].Moves[0] = new Move(MoveDatabase.Get("Flash")!);
        Assert.True(FieldMovement.MovesOf(party).HasFlag(FieldMoves.Flash));
    }

    // ------------------------------------------------------------------ rock in the open country

    [Fact]
    public void RockNobodyWalksOnStandsAboveTheGround()
    {
        var map = Overworld;
        // The mountain between Route 203 and Oreburgh City: the world's own heights have it as flat as the road
        Assert.Equal((TileType.Rock, true), (map.GetGroundTile(252, 749), map.IsSolid(252, 749)));
        float road = map.HeightAt(245, 749);
        Assert.Equal(road + WorldMapBuilder.CaveWallRise, map.HeightAt(252, 749));
        Assert.True(Relief.At(map, 252.5f, 749.5f) - Relief.At(map, 245.5f, 749.5f) >= WorldMapBuilder.CaveWallRise - 0.01f);

        // The road beside it is drawn as flat as before
        var corners = Relief.Corners(map, 245, 749);
        Assert.True(corners.NW == corners.NE && corners.NE == corners.SW && corners.SW == corners.SE);

        // Rock one walks on stays where it is: Oreburgh's western approach
        Assert.Equal((TileType.Rock, false), (map.GetGroundTile(260, 750), map.IsSolid(260, 750)));
        Assert.Equal(map.HeightAt(262, 750), map.HeightAt(260, 750));

        // Every tile of rock that blocks the way stands a lip or a wall above the ground nearest to it
        int raised = 0;
        foreach (var (x, y) in Tiles(map))
        {
            if (map.GetGroundTile(x, y) != TileType.Rock || !map.IsSolid(x, y)) continue;
            // A cracked rock stands on the ground as it is, and so does a way in that leads nowhere yet, shut
            if (map.Props.Any(p => p.Covers(x, y)) || ShutWays.Value.Contains((x, y))) continue;
            raised++;
            if (map.InBounds(x, y - 1) && !(map.GetGroundTile(x, y - 1) == TileType.Rock && map.IsSolid(x, y - 1)) && !map.IsDeepWater(x, y - 1))
                Assert.Equal(map.HeightAt(x, y - 1) + WorldMapBuilder.CaveLipRise, map.HeightAt(x, y));
        }
        Assert.True(raised > 2000, $"only {raised} tiles of rock stand up");
    }

    [Fact]
    public void TheMouthOfACaveIsADarkHollowInTheRock()
    {
        var map = Overworld;
        var gate = BuiltMaps.Value["OreburghGate1F"];

        // Oreburgh Gate from Route 203: the way in is the last tile of the road, the hollow lies behind it
        var into = map.GetWarpAt(246, 749)!;
        Assert.Equal("OreburghGate1F", into.TargetMap);
        foreach (int z in new[] { 748, 749, 750 })
        {
            Assert.Equal((TileType.CaveMouth, true), (map.GetGroundTile(247, z), map.IsSolid(247, z)));
            Assert.Equal(map.HeightAt(246, 749), map.HeightAt(247, z));   // at the level of the ground, under the rock
        }
        Assert.NotEqual(TileType.CaveMouth, map.GetGroundTile(246, 749));
        Assert.Equal(TileType.CaveMouth, map.GetGroundTile(248, 749));     // two tiles deep, as the original paints its dark
        Assert.True(map.HeightAt(249, 749) >= map.HeightAt(247, 749) + WorldMapBuilder.CaveWallRise, "the rock doesn't stand above the mouth");

        // ...and out again on the far side, one step clear of the warp so it isn't taken twice
        Assert.True(gate.IsWalkable(into.TargetX, into.TargetY));
        Assert.Null(gate.GetWarpAt(into.TargetX, into.TargetY));
        var back = gate.GetWarpAt(4, 22)!;
        Assert.Equal(("Sinnoh", 245, 749, Direction.Left), (back.TargetMap, back.TargetX, back.TargetY, back.TargetFacing));
        Assert.Equal("Sinnoh", gate.GetWarpAt(27, 22)!.TargetMap);
        Assert.Equal("oreburgh_city", map.AreaAt(gate.GetWarpAt(27, 22)!.TargetX, gate.GetWarpAt(27, 22)!.TargetY)!.Key);
        Assert.Equal(TileType.CaveMouth, map.GetGroundTile(257, 749));

        // The Ravaged Path from Route 204: the way in lies in the line of the rock, the hollow on both sides of it,
        // so it is as dark as they are and still walked on
        Assert.Equal("RavagedPath", map.GetWarpAt(171, 705)!.TargetMap);
        Assert.Equal((TileType.CaveMouth, false), (map.GetGroundTile(171, 705), map.IsSolid(171, 705)));
        Assert.Equal((TileType.CaveMouth, true), (map.GetGroundTile(170, 705), map.IsSolid(170, 705)));
        Assert.Equal((TileType.CaveMouth, true), (map.GetGroundTile(172, 705), map.IsSolid(172, 705)));
        Assert.Equal((TileType.CaveMouth, true), (map.GetGroundTile(171, 704), map.IsSolid(171, 704)));
        Assert.Equal("RavagedPath", map.GetWarpAt(180, 698)!.TargetMap);

        // Every mouth lies at the level of the ground before it, and one that can be walked into leads into a cave
        int mouths = 0;
        foreach (var (x, y) in Tiles(map).Where(t => map.GetGroundTile(t.X, t.Y) == TileType.CaveMouth))
        {
            mouths++;
            if (map.IsSolid(x, y)) continue;
            Assert.True(map.GetWarpAt(x, y) is { } warp && MapNamed(warp.TargetMap).IsCave, $"the mouth at ({x},{y}) can be walked into and leads into no cave");
        }
        Assert.True(mouths >= 20, $"only {mouths} tiles of cave mouth on the map");

        // Inside a cave there is no such thing: a way out is floor, and rock round it
        Assert.All(Caves, name => Assert.DoesNotContain(Tiles(BuiltMaps.Value[name]), t => BuiltMaps.Value[name].GetGroundTile(t.X, t.Y) == TileType.CaveMouth));

        // Floaroma's way into its meadow is no cave
        Assert.Equal("FloaromaMeadow", map.GetWarpAt(180, 612)!.TargetMap);
        Assert.NotEqual(TileType.CaveMouth, map.GetGroundTile(180, 612));
    }

    // ------------------------------------------------------------------ the mine's conveyors

    [Fact]
    public void AYardsThinPiecesCarryItsConveyors()
    {
        // A run of four carries a belt; two pieces across an open tile are a gantry, and the belt goes through it
        // to the run on one side and to a shed on the other; a piece alone is a pier
        var thin = new HashSet<(int X, int Z)> { (5, 0), (5, 1), (5, 2), (5, 3), (4, 6), (6, 6), (4, 7), (6, 7), (12, 12) };
        var shed = new HashSet<(int X, int Z)> { (5, 10), (5, 11) };
        bool Blocked(int x, int z) => thin.Contains((x, z)) || shed.Contains((x, z));
        var props = WorldMapBuilder.Conveyors(thin, Blocked);

        var belt = Assert.Single(props, p => p.Type == PropType.Conveyor);
        Assert.Equal((5, 0, 1, 10), (belt.X, belt.Y, belt.Width, belt.Depth));   // the run, the gap, the gantry, and on to the shed
        var gantries = props.Where(p => p.Type == PropType.Gantry).ToList();
        Assert.Contains(gantries, g => (g.X, g.Y, g.Width, g.Depth) == (4, 6, 3, 2));
        Assert.Contains(gantries, g => (g.X, g.Y, g.Width, g.Depth) == (12, 12, 1, 1));
        Assert.Equal(2, gantries.Count);

        // A belt reaches no further than its reach for something to meet: here nothing lies north or south
        var lonely = WorldMapBuilder.Conveyors(new[] { (4, 20), (6, 20) }, (x, z) => (x, z) is (4, 20) or (6, 20));
        var span = Assert.Single(lonely, p => p.Type == PropType.Conveyor);
        Assert.Equal((5, 20, 1, 1), (span.X, span.Y, span.Width, span.Depth));

        // A conveyor stands over open ground: it blocks nothing by itself
        Assert.False(new Prop { Type = PropType.Conveyor }.IsSolid);
        Assert.False(new Prop { Type = PropType.Gantry }.IsSolid);
        Assert.True(new Prop { Type = PropType.Drums }.IsSolid);

        // Oreburgh's yard: belts from the pit head north to the sorting shed and from the winding tower to the coal
        var map = Overworld;
        var belts = map.Props.Where(p => p.Type == PropType.Conveyor).ToList();
        Assert.True(belts.Count >= 4, $"{belts.Count} conveyors in Oreburgh's yard");
        Assert.All(belts, b => Assert.Equal("oreburgh_city", map.AreaAt(b.X, b.Y)!.Key));
        var main = belts.Single(b => b.X == 299);
        Assert.True(main.Depth >= 20, $"the belt up the yard is {main.Depth} tiles long");
        Assert.Contains(Enumerable.Range(main.Y, main.Depth), z => map.IsWalkable(299, z));     // walked under
        Assert.Contains(Enumerable.Range(main.Y, main.Depth), z => map.IsSolid(299, z));        // and carried on piers
        Assert.Equal(3, map.Props.Count(p => p.Type == PropType.Gantry && p.X == 298 && p.Width == 3));
        Assert.Equal(WorldModels.Of("c3_s03")!.Thin, PropType.Conveyor);
        Assert.Null(WorldModels.Of("c3_s03")!.StandInUntil);

        // In the mine itself: a conveyor down either side of the coal face, drums and crates by the way in
        var deep = BuiltMaps.Value["OreburghMineB2F"];
        Assert.Equal(new[] { 6, 25 }, deep.Props.Where(p => p.Type == PropType.Conveyor).Select(p => p.X).OrderBy(x => x));
        var first = BuiltMaps.Value["OreburghMineB1F"];
        Assert.Equal(4, first.Props.Count(p => p.Type == PropType.Drums));
        Assert.Equal(3, first.Props.Count(p => p.Type == PropType.Crates));
    }

    // ------------------------------------------------------------------ the city

    [Fact]
    public void JubilifesLampsStandInItsPaving()
    {
        var map = Overworld;
        var lamps = map.Props.Where(p => p.Type == PropType.LampPost && map.AreaAt(p.X, p.Y)?.Key == "jubilife_city").ToList();
        Assert.True(lamps.Count >= 12, $"{lamps.Count} street lamps in Jubilife City");
        Assert.All(lamps, l => Assert.Equal((TileType.Paving, true), (map.GetGroundTile(l.X, l.Y), map.IsSolid(l.X, l.Y))));
        // The pair that flanks the main street at the top of the square
        Assert.Contains(lamps, l => (l.X, l.Y) == (171, 752));
        Assert.Contains(lamps, l => (l.X, l.Y) == (177, 752));

        // No post of a fence is left standing by itself in the paving
        var fenced = map.Props.Where(p => p.Type is PropType.Fence or PropType.LowWall).Select(p => (p.X, p.Y)).ToHashSet();
        foreach (var (x, y) in fenced.Where(t => map.AreaAt(t.X, t.Y)?.Key == "jubilife_city"))
        {
            bool alone = !fenced.Contains((x + 1, y)) && !fenced.Contains((x - 1, y)) && !fenced.Contains((x, y + 1)) && !fenced.Contains((x, y - 1));
            Assert.False(alone && map.GetGroundTile(x, y + 1) == TileType.Paving && map.GetGroundTile(x, y - 1) == TileType.Paving,
                $"a fence post stands alone in the paving at ({x},{y})");
        }

        // Nothing under a building or a heap of coal took its ground from a tile the import couldn't name:
        // no square of lawn in the open of Oreburgh's yard (the heaps beside its row of trees lie on their lawn)
        for (int y = 777; y < 795; y++)
            for (int x = 299; x < 317; x++)
                Assert.True(map.GetGroundTile(x, y) is not (TileType.Grass or TileType.FlowerGrass), $"lawn in the mine's yard at ({x},{y})");
    }

    [Fact]
    public void ARailingTwoTilesThickRunsInTwoRows()
    {
        // A band two wide and five long, running north and south
        var band = new Dictionary<(int, int), bool>();
        for (int y = 0; y < 5; y++) { band[(0, y)] = false; band[(1, y)] = false; }
        Assert.True(OutdoorProps.Joins(band, 0, 1, 0, 1));     // along the band
        Assert.True(OutdoorProps.Joins(band, 1, 3, 0, -1));
        Assert.False(OutdoorProps.Joins(band, 0, 2, 1, 0));    // not across it, tile after tile
        Assert.False(OutdoorProps.Joins(band, 1, 2, -1, 0));
        Assert.False(OutdoorProps.Joins(band, 0, 0, -1, 0));   // nothing there

        // A single row, a corner and a T join as they always did
        var row = new Dictionary<(int, int), bool> { [(0, 0)] = false, [(1, 0)] = false, [(2, 0)] = false, [(1, 1)] = false };
        Assert.True(OutdoorProps.Joins(row, 0, 0, 1, 0));
        Assert.True(OutdoorProps.Joins(row, 1, 0, 0, 1));
        Assert.True(OutdoorProps.Joins(row, 1, 1, 0, -1));
    }

    [Fact]
    public void AWayInThatLeadsNowhereIsClosed()
    {
        var map = Overworld;
        // The Global Terminal's porch is an open tile in the world's data
        foreach (var (x, y) in new[] { (149, 778) })
        {
            Assert.Null(map.GetWarpAt(x, y));
            Assert.True(map.IsSolid(x, y), $"one can walk into the way in at ({x},{y}), which leads nowhere");
        }
        // The Trainers' School is entered the same way, and is open
        Assert.Equal("TrainersSchool", map.GetWarpAt(168, 776)!.TargetMap);
        Assert.False(map.IsSolid(168, 776));
        // The Lost Tower's rooms aren't built: its way in on Route 209 is shut (plan 01 · M11)
        Assert.Null(map.GetWarpAt(568, 680));
        Assert.True(map.IsSolid(568, 680));
        // Eterna Forest is (plan 01 · M6): Route 205 leads into it
        Assert.Equal("EternaForest", map.GetWarpAt(206, 581)!.TargetMap);
    }

    // ------------------------------------------------------------------ people

    [Fact]
    public void TheTwoHalvesOfARouteDoNotPlaceEachOthersPeopleTwice()
    {
        var map = Overworld;
        // Route 204's halves each list the two boys by the cave and its signboard
        Assert.Single(map.NPCs, n => (n.GridX, n.GridY) == (173, 706));
        Assert.Single(map.NPCs, n => (n.GridX, n.GridY) == (175, 700));
        Assert.Contains("Ravaged Path", map.Signboards[(170, 706)]);
        Assert.Equal(map.NPCs.Count, map.NPCs.Select(n => (n.GridX, n.GridY)).Distinct().Count());

        // Each says its own half's line
        Assert.Contains("Ravaged Path", map.GetNpcAt(173, 706)!.DialogLines[0]);
        Assert.Contains("types", map.GetNpcAt(175, 700)!.DialogLines[0]);
    }

    [Fact]
    public void TrainersSeeAsFarAsInPlatinumAndBringItsTeams()
    {
        var all = BuiltMaps.Value.Values.SelectMany(m => m.NPCs).Where(n => n.IsTrainer).ToDictionary(n => n.TrainerData!.Id + "/" + n.Name, n => n.TrainerData!);
        Trainer Named(string id) => all.First(t => t.Key.StartsWith(id + "/", StringComparison.Ordinal)).Value;

        // The original's ranges (the first number an object keeps in its event), not one for everybody
        Assert.Equal(5, Named("trainer_sebastian").SightRange);
        Assert.Equal(1, Named("trainer_kaitlin").SightRange);
        Assert.Equal(5, Named("trainer_tristan").SightRange);
        // The miners look at their work: they battle when spoken to
        Assert.Equal(0, Named("trainer_colin").SightRange);
        Assert.Equal(0, Named("trainer_mason").SightRange);

        // Teams by species and level, and moves where the trainer chose them (res/trainers/data in the decompilation)
        var sebastian = Named("trainer_sebastian");
        Assert.Equal(("Machop", 8), (sebastian.Party.Members[0].Species.Name, sebastian.Party.Members[0].Level));
        Assert.Equal(new[] { "Low Kick", "Leer" }, sebastian.Party.Members[0].Moves.Select(m => m.Name));
        var grant = Named("trainer_grant");
        Assert.Equal(new[] { "Riolu", "Staraptor", "Graveler" }, grant.Party.Members.Select(p => p.Species.Name));
        Assert.All(grant.Party.Members, p => Assert.Equal((34, 4), (p.Level, p.Moves.Count)));

        // Prize money is Platinum's: the last Pokémon's level, four times over, by the class's rate
        Assert.Equal(8 * 4 * 4, sebastian.PrizeMoney);              // Youngster 4
        Assert.Equal(34 * 4 * 20, grant.PrizeMoney);                // Veteran 20
        Assert.Equal(8 * 4 * 10, Named("trainer_colin").PrizeMoney); // Worker 10
        Assert.Equal(11 * 4 * 8, Named("trainer_taylor").PrizeMoney); // Aroma Lady 8
        Assert.Equal(11 * 4 * 4 * 2, Named("trainer_liv_and_liz").PrizeMoney);   // Twins 4, doubled for two at a time

        // A trainer with chosen moves is written back out with them
        var written = MapFile.FromMap(new Map(4, 4) { Name = "T", NPCs = { Overworld.NPCs.First(n => n.TrainerData?.Id == "trainer_sebastian") } });
        Assert.Equal(new[] { "Low Kick", "Leer" }, written.Npcs.Single().Trainer!.Party.Single().Moves);
        Assert.Null(MapFile.FromMap(new Map(4, 4) { Name = "T", NPCs = { Overworld.NPCs.First(n => n.TrainerData?.Id == "trainer_dallas") } }).Npcs.Single().Trainer!.Party.Single().Moves);
    }

    [Fact]
    public void EveryOpenTownHasACenterAndAMartOfItsOwn()
    {
        var map = Overworld;
        foreach (var (town, center, mart) in new[]
                 {
                     ("jubilife_city", "JubilifePokemonCenter", "JubilifePokeMart"), ("oreburgh_city", "OreburghPokemonCenter", "OreburghPokeMart"),
                     ("floaroma_town", "FloaromaPokemonCenter", "FloaromaPokeMart")
                 })
        {
            foreach (string room in new[] { center, mart })
            {
                var door = Assert.Single(map.Warps, w => w.TargetMap == room);
                Assert.Equal(town, map.AreaAt(door.SourceX, door.SourceY)!.Key);
                var inside = MapDatabase.Get(room);
                Assert.Equal(room, inside.Name);
                Assert.True(inside.IsWalkable(door.TargetX, door.TargetY));
                // Leaving puts one down outside the same door
                var exit = Assert.Single(inside.Warps);
                Assert.Equal(("Sinnoh", door.SourceX, door.SourceY + 1), (exit.TargetMap, exit.TargetX, exit.TargetY));
                Assert.True(map.IsWalkable(exit.TargetX, exit.TargetY));
                Assert.Equal(RegionDatabase.Sinnoh, RegionDatabase.RegionOfMap(room)!.Id);
            }
            Assert.Contains(MapDatabase.Get(center).NPCs, n => n.IsHealingNurse);
            Assert.Contains(MapDatabase.Get(mart).NPCs, n => n.IsPokeMartClerk);
            Assert.Contains(MapStructures.BuildingsOf(map), b => b.Kind == BuildingKind.PokemonCenter && b.Doors.Any(d => d.Target == center));
            Assert.Contains(MapStructures.BuildingsOf(map), b => b.Kind == BuildingKind.PokeMart && b.Doors.Any(d => d.Target == mart));
        }
    }

    // ------------------------------------------------------------------ saves

    [Fact]
    public void ASaveMadeInTheHandMadeJubilifeCityWakesUpInTheCity()
    {
        // Jubilife City was a map of its own while the world round it was already imported
        var old = JsonSerializer.Deserialize<SaveData>($"{{\"CurrentMapName\":\"JubilifeCity\",\"PlayerGridX\":19,\"PlayerGridY\":17,\"WorldVersion\":{SaveData.ImportedWorld}}}")!;
        var spot = old.Place();
        Assert.Equal("Sinnoh", spot.Map);
        Assert.Equal("jubilife_city", Overworld.AreaAt(spot.X, spot.Y)!.Key);
        Assert.True(Overworld.IsWalkable(spot.X, spot.Y));
        Assert.Null(Overworld.GetWarpAt(spot.X, spot.Y));
        // Older still: from before any of the world was imported
        Assert.Equal(spot, JsonSerializer.Deserialize<SaveData>("{\"CurrentMapName\":\"JubilifeCity\",\"PlayerGridX\":3,\"PlayerGridY\":3}")!.Place());

        // Its rooms are where they were
        var room = new SaveData { CurrentMapName = "TrainersSchool", PlayerGridX = 6, PlayerGridY = 8, WorldVersion = SaveData.ImportedWorld };
        Assert.Equal(new MapSpot("TrainersSchool", 6, 8), room.Place());

        // A save of today stands where it says, in the city or anywhere else
        Assert.Equal(SaveData.ImportedJubilife, SaveData.CurrentWorld);
        var today = new SaveData { CurrentMapName = "Sinnoh", PlayerGridX = 175, PlayerGridY = 760, PlayerFacing = Direction.Up, WorldVersion = SaveData.CurrentWorld };
        Assert.Equal(new MapSpot("Sinnoh", 175, 760, Direction.Up), JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(today))!.Place());
        // The lake kept its name when it was imported, so the version decides
        var lake = new SaveData { CurrentMapName = "LakeVerity", PlayerGridX = 40, PlayerGridY = 50, WorldVersion = SaveData.CurrentWorld };
        Assert.Equal(("LakeVerity", 40, 50), (lake.Place().Map, lake.Place().X, lake.Place().Y));
        Assert.DoesNotContain("JubilifeCity", MapDatabase.MapNames);
    }
}
