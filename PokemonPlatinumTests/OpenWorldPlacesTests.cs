using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ContentCheck;
using OpenWorldBake;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.OpenWorld;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 32 · OW5 with no window: a volume's shapes and a polygon held to whole numbers; a trigger crossed once as the
/// capsule's centre crosses into it, and again only after it has been left; the open test world's places read
/// strictly and laid on its map, refused with the file and the key named where they are wrong; a script's walk to a
/// marker on the lab's ground that ends on the marker played or finished at once; the marker forms of the commands
/// read; the scripts' open problems; the rock over a carve (OW2's finding at the lab's tunnel); and the content check's
/// open rules on the lab and on a lab broken on purpose.
/// </summary>
public class OpenWorldPlacesTests
{
    static string LabFolder => OpenWorldBakeTests.LabFolder;

    /// <summary>The lab's map as the game loads it with its places, on its baked level 0.</summary>
    internal static OpenMap LabWithPlaces()
    {
        var map = OpenPlaceFiles.LoadRegion(LabFolder, withLand: false).Single();
        map.Land = BakedRegion.Open(OpenWorldCapsuleTests.LabBaked);
        return map;
    }

    /// <summary>The house the lab's door leads into: a room of seven by eight, its way out at 3,7 leading back to the open map.</summary>
    internal static Map LabHouse(string openMap = "OpenLab") => GameDataFiles.Deserialize<MapFile>($$"""
        {
          "name": "OpenLabHouse", "displayName": "Test House", "interior": "House", "width": 7, "height": 8,
          "ground": ["#######", "#######", "#_____#", "#_____#", "#_____#", "#_____#", "#_____#", "###D###"],
          "solid":  ["#######", "#######", "#.....#", "#.....#", "#.....#", "#.....#", "#.....#", "###.###"],
          "warps": [ { "sourceX": 3, "sourceY": 7, "targetMap": "{{openMap}}", "targetX": 0, "targetY": 0, "targetFacing": "Down" } ],
          "props": [], "signboards": [], "npcs": [], "wildEncounters": []
        }
        """).ToMap();

    // ------------------------------------------------------------------ shapes and crossings

    [Fact]
    public void ABoxAndACylinderHoldWhatIsInsideThemToTheMillimetre()
    {
        var box = OpenShape.Box(new OpenPos(-1000, 0, -2000), new OpenPos(1000, 3000, 2000));
        Assert.True(box.Contains(new OpenPos(-1000, 0, -2000)));
        Assert.False(box.Contains(new OpenPos(1000, 0, 0)));   // the east side is out
        Assert.False(box.Contains(new OpenPos(0, 3000, 0)));   // and the top
        Assert.False(box.Contains(new OpenPos(0, -1, 0)));
        var cylinder = OpenShape.Cylinder(5000, -5000, -1000, 3000, 6000).Checked("c");
        Assert.Equal(6000, cylinder.Radius);
        Assert.Equal(new OpenPos(5000, -1000, -5000), cylinder.Base);
        Assert.True(cylinder.Contains(new OpenPos(5000 + 5999, 0, -5000)));
        Assert.False(cylinder.Contains(new OpenPos(5000 + 6000, 0, -5000)));
        Assert.False(cylinder.Contains(new OpenPos(5000 + 4300, 0, -5000 + 4300)));   // in the box round it, out of the circle
        Assert.Throws<ArgumentException>(() => OpenShape.Box(new OpenPos(0, 0, 0), new OpenPos(0, 1, 1)).Checked("flat"));
    }

    [Fact]
    public void APolygonHoldsItsInsideByTheEvenOddRuleWhicheverWayItIsWritten()
    {
        // An L, written clockwise and then anticlockwise
        var l = new List<(int X, int Z)> { (0, 0), (4000, 0), (4000, 1000), (1000, 1000), (1000, 4000), (0, 4000) };
        foreach (var polygon in new[] { l, Enumerable.Reverse(l).ToList() })
        {
            Assert.True(OpenPolygons.Holds(polygon, 500, 500));
            Assert.True(OpenPolygons.Holds(polygon, 3500, 500));
            Assert.True(OpenPolygons.Holds(polygon, 500, 3500));
            Assert.False(OpenPolygons.Holds(polygon, 3000, 3000));   // the L's notch
            Assert.False(OpenPolygons.Holds(polygon, -1, 500));
        }
    }

    [Fact]
    public void ATriggerIsCrossedOnceAsTheCentreCrossesIntoItAndAgainOnlyOnceItHasBeenLeft()
    {
        var map = new OpenMap("Crossings", "test");
        map.Add(new OpenTrigger("gate", "test_place", OpenShape.Box(new OpenPos(0, 0, -1000), new OpenPos(2000, 2000, 1000)), "Gate"));
        map.Add(new OpenDoor("door", "test_place", OpenShape.Box(new OpenPos(1000, 0, -500), new OpenPos(1500, 2000, 500)), "Room", 3, 4, Direction.Up, "out"));
        var crossings = new OpenCrossings();
        // Put down west of it: nothing is crossed
        crossings.Settle(map, new OpenPos(-500, 0, 0));
        Assert.Empty(crossings.Cross(map, new OpenPos(-1, 0, 0)));
        // A step in: the trigger, once
        var first = crossings.Cross(map, new OpenPos(0, 0, 0));
        Assert.Equal("gate", Assert.Single(first).Trigger!.Name);
        Assert.Empty(crossings.Cross(map, new OpenPos(500, 0, 0)));
        // On into the doorway inside it: the door alone (the trigger is stood in already)
        Assert.Equal("door", Assert.Single(crossings.Cross(map, new OpenPos(1200, 0, 0))).Door!.Name);
        Assert.Empty(crossings.Cross(map, new OpenPos(1300, 0, 0)));
        // Out the east side and back in: the trigger again
        Assert.Empty(crossings.Cross(map, new OpenPos(2000, 0, 0)));
        Assert.Equal("gate", Assert.Single(crossings.Cross(map, new OpenPos(1999, 0, 0))).Trigger!.Name);
        // The centre is the body's middle: feet a metre and a quarter under the box's floor still cross it, two metres under don't
        crossings.Settle(map, new OpenPos(-500, -2000, 0));
        Assert.Empty(crossings.Cross(map, new OpenPos(500, -2000, 0)));
        Assert.Single(crossings.Cross(map, new OpenPos(500, -500, 0)));
        // Put down inside: what is stood in is no crossing
        crossings.Settle(map, new OpenPos(500, 0, 0));
        Assert.Empty(crossings.Cross(map, new OpenPos(600, 0, 0)));
        Assert.Contains("t:gate", crossings.Inside);
    }

    [Fact]
    public void ATriggerAndADoorCrossedInOneTickGoTriggerFirst()
    {
        var map = new OpenMap("Both", "test");
        map.Add(new OpenDoor("door", "p", OpenShape.Box(new OpenPos(0, 0, 0), new OpenPos(1000, 2000, 1000)), "Room", 1, 1, Direction.Up, "out"));
        map.Add(new OpenTrigger("mat", "p", OpenShape.Box(new OpenPos(0, 0, 0), new OpenPos(1000, 2000, 1000)), "Mat"));
        var crossings = new OpenCrossings();
        crossings.Settle(map, new OpenPos(-500, 0, 500));
        var both = crossings.Cross(map, new OpenPos(500, 0, 500));
        Assert.Equal(2, both.Count);
        Assert.NotNull(both[0].Trigger);
        Assert.NotNull(both[1].Door);
    }

    // ------------------------------------------------------------------ the places' files

    [Fact]
    public void TheLabsPlacesAreReadAndLaidOnItsMap()
    {
        var map = OpenPlaceFiles.LoadRegion(LabFolder, withLand: false).Single();
        Assert.Equal(("OpenLab", "lab"), (map.Name, map.Region));
        Assert.Equal(new[] { "lab_meadow", "lab_hill", "lab_cave", "lab_river" }, map.Areas.Select(a => a.Key));
        Assert.Equal(new OpenPos(-58_000, 500, 22_000), map.Marker("west_road")!.Pos);
        Assert.Equal(OpenYaw.East, map.Marker("WEST_ROAD")!.Yaw);   // degrees to 65,536ths, the name by any case
        Assert.Equal(OpenYaw.West, map.Marker("cavern_east")!.Yaw);

        var trigger = Assert.Single(map.Triggers);
        Assert.Equal(("cavern", "lab_cave", "Cavern", "VAR_LAB_CAVE", 0), (trigger.Name, trigger.Place, trigger.Script, trigger.Variable, trigger.Value));
        Assert.Equal(OpenShapeKind.Cylinder, trigger.Shape.Kind);
        Assert.Equal(6000, trigger.Shape.Radius);

        var door = Assert.Single(map.Doors);
        Assert.Equal(("house_door", "lab_meadow", "OpenLabHouse", 3, 6, Direction.Up, "house_step"), (door.Name, door.Place, door.Room, door.RoomX, door.RoomY, door.RoomFacing, door.Out));
        Assert.Same(door, map.DoorBackFrom("OpenLabHouse", 3, 7));
        Assert.Null(map.DoorBackFrom("ElsewhereHouse", 3, 7));

        // The hill's top (and OW7's south meadow, OpenWorldWildTests's)
        Assert.Equal(new[] { "south_meadow", "hill_top" }.OrderBy(n => n), map.Zones.Select(z => z.Name).OrderBy(n => n));
        var zone = Assert.Single(map.Zones, z => z.Name == "hill_top");
        Assert.Equal(new[] { "Wooloo", "Skwovet" }, zone.Slots.Select(s => s.Species));
        Assert.True(zone.Contains(new OpenPos(0, 12_000, -2_000)));
        Assert.False(zone.Contains(new OpenPos(0, 0, 0)));   // the cavern under it is no part of it
        Assert.Same(zone, map.ZoneAt(new OpenPos(0, 12_000, -2_000)));
        Assert.Contains(map.Solids, s => s.Name == "the house");

        // The area of a place: the later of the region's areas that holds it, so the cave under the hill is its own
        Assert.Equal("lab_cave", map.ScriptFileAt(new OpenPos(0, 500, 0)));
        Assert.Equal("lab_hill", map.ScriptFileAt(new OpenPos(0, 12_000, -2_000)));
        Assert.Equal("lab_meadow", map.ScriptFileAt(new OpenPos(-58_000, 500, 22_000)));
        Assert.Equal("lab_river", map.ScriptFileAt(new OpenPos(0, 0, 40_000)));
        Assert.Equal("OpenLab", map.ScriptFileAt(new OpenPos(500_000, 0, 0)));
    }

    [Theory]
    [InlineData("""{ "marker": [] }""", "lab_cave.json: $.marker: no such field")]
    [InlineData("""{ "markers": [ { "name": "a", "at": [1, 2] } ] }""", "$.markers[0].at: a point in the air is [x, y, z]")]
    [InlineData("""{ "markers": [ { "name": "a b", "at": [1, 2, 3] } ] }""", "$.markers[0].name: a marker's name is letters, digits and _")]
    [InlineData("""{ "markers": [ { "at": [1, 2, 3] } ] }""", "missing")]
    [InlineData("""{ "triggers": [ { "name": "t", "script": "S", "box": { "min": [0, 0, 0], "max": [1, 1, 1] }, "cylinder": { "at": [0, 0], "radius": 1, "bottom": 0, "top": 1 } } ] }""", "$.triggers[0]: a volume is a box or a cylinder, not both")]
    [InlineData("""{ "triggers": [ { "name": "t", "script": "S" } ] }""", "$.triggers[0]: a volume is a box or a cylinder, not both")]
    [InlineData("""{ "doors": [ { "name": "d", "box": { "min": [0, 0, 0], "max": [1, 1, 1] }, "room": "R", "at": [3], "out": "o" } ] }""", "$.doors[0].at: a room's tile is [x, y]")]
    [InlineData("""{ "zones": [ { "name": "z", "polygon": [[0, 0], [1, 0], [1, 1]], "slots": [ { "species": "Bidoof", "minLevel": 5, "maxLevel": 3 } ] } ] }""", "$.zones[0].slots[0]: levels from one")]
    [InlineData("""{ "zones": [ { "name": "z", "polygon": [[0, 0], [1, 0]], "slots": [] } ] }""", "$.zones[0].polygon: a polygon has three points or more")]
    public void APlaceWrittenWrongIsRefusedWithItsFileAndKeyNamed(string text, string told)
    {
        var e = Assert.Throws<InvalidDataException>(() => OpenPlaceFiles.Read(text, "places/lab_cave.json"));
        Assert.Contains(told, e.Message);
        Assert.StartsWith("places/lab_cave.json: ", e.Message);
    }

    [Fact]
    public void APlaceNamedForNoAreaAndTwoMarkersOfOneNameStopTheLoad()
    {
        string folder = CopyOfTheLab();
        try
        {
            File.WriteAllText(Path.Combine(folder, "places", "nowhere.json"), "{}");
            Assert.Contains("places/nowhere.json: is named for no area", Assert.Throws<InvalidDataException>(() => OpenPlaceFiles.LoadRegion(folder, withLand: false)).Message);
            File.Delete(Path.Combine(folder, "places", "nowhere.json"));
            File.WriteAllText(Path.Combine(folder, "places", "lab_river.json"), """{ "markers": [ { "name": "west_road", "at": [0, 0, 40] } ] }""");
            Assert.Contains("two markers named west_road", Assert.Throws<InvalidDataException>(() => OpenPlaceFiles.LoadRegion(folder, withLand: false)).Message);
        }
        finally { Directory.Delete(folder, recursive: true); }
    }

    static string CopyOfTheLab()
    {
        string folder = Path.Combine(Path.GetTempPath(), "openlab-places-" + Guid.NewGuid().ToString("N"));
        foreach (string file in Directory.GetFiles(LabFolder, "*.json", SearchOption.AllDirectories))
        {
            string to = Path.Combine(folder, Path.GetRelativePath(LabFolder, file));
            if (to.Contains(Path.DirectorySeparatorChar + "baked" + Path.DirectorySeparatorChar)) continue;
            Directory.CreateDirectory(Path.GetDirectoryName(to)!);
            File.Copy(file, to);
        }
        return folder;
    }

    // ------------------------------------------------------------------ a walk to a marker

    [Fact]
    public void AWalkToAMarkerEndsOnItPlayedOrFinishedAtOnce()
    {
        var map = LabWithPlaces();
        var from = map.Marker("house_path")!;
        var to = map.Marker("house_step")!;

        var played = new OpenField(map, from.Pos, from.Yaw);
        var walk = new OpenMarkerWalk("player", to, run: false);
        int ticks = 0;
        while (!walk.Done && ticks < OpenClock.TicksPerSecond * 30)
        {
            played.Run(1.0 / OpenClock.TicksPerSecond, f => walk.InputFor(f.Player), f => { walk.After(f.Player); return false; });
            ticks++;
        }
        Assert.True(walk.Done && !walk.GaveUp, $"walked {ticks} ticks to {played.Player.Pos}");
        Assert.Equal(to.Pos, played.Player.Pos);
        Assert.Equal(to.Yaw, played.Player.Yaw);
        // Six metres at a walk: four seconds, give or take the last stretch
        Assert.InRange(ticks, 4 * OpenClock.TicksPerSecond - 20, 4 * OpenClock.TicksPerSecond + 10);

        // Finished at once (a skipped scene's), from the same start: the same millimetre and yaw
        var skipped = new OpenField(map, from.Pos, from.Yaw);
        var hurried = new OpenMarkerWalk("player", to, run: false);
        skipped.Finish(hurried);
        Assert.True(hurried.Done);
        Assert.Equal(played.Player.Pos, skipped.Player.Pos);
        Assert.Equal(played.Player.Yaw, skipped.Player.Yaw);
    }

    [Fact]
    public void AWalkIntoAWallGivesUpWhereItStands()
    {
        var map = LabWithPlaces();
        // From the house's step straight at a marker behind its block: the walk comes no nearer and is given up
        map.Add(new OpenMarker("behind_the_house", new OpenPos(-20_000, 100, -55_000)));
        var field = new OpenField(map, map.Marker("house_step")!.Pos, 0);
        var walk = new OpenMarkerWalk("player", map.Marker("behind_the_house")!, run: true);
        for (int t = 0; t < OpenClock.TicksPerSecond * 20 && !walk.Done; t++)
            field.Run(1.0 / OpenClock.TicksPerSecond, f => walk.InputFor(f.Player), f => { walk.After(f.Player); return false; });
        Assert.True(walk.Done && walk.GaveUp);
        Assert.InRange(field.Player.Pos.Z, -43_000 + 340, -42_000);   // against the house's south wall
    }

    [Fact]
    public void TheCapsuleWalkingThroughTheTunnelCrossesTheCavernsTriggerOnce()
    {
        var map = LabWithPlaces();
        var start = map.Marker("tunnel_west")!;
        var field = new OpenField(map, start.Pos, start.Yaw);
        var crossings = new OpenCrossings();
        crossings.Settle(map, start.Pos);
        var crossed = new List<(string Name, int X)>();
        // East at a run for ten seconds, a frame at a time: through the cavern and on into the tunnel beyond
        for (int frame = 0; frame < 600; frame++)
            field.Run(1.0 / 60, _ => new OpenInput(1, 0, true), f =>
            {
                foreach (var c in crossings.Cross(f.Map, f.Player.Pos)) crossed.Add((c.Trigger?.Name ?? c.Door!.Name, f.Player.Pos.X));
                return false;
            });
        var (name, x) = Assert.Single(crossed);
        Assert.Equal("cavern", name);
        Assert.InRange(x, -6_050, -5_900);   // as the centre crosses its radius of six metres
        Assert.True(field.Player.Pos.X > 9_000, $"{field.Player.Pos}");
    }

    // ------------------------------------------------------------------ the commands and the scripts' problems

    [Fact]
    public void TheMarkerFormsOfTheCommandsAreRead()
    {
        var code = ScriptParser.Parse("lab_cave", """
            script S
              walkto player cavern_east fast
              walkto self cavern_middle
              placeat player tunnel_west
              faceto player cavern_middle
              faceto self player
              warpto "OpenLab" hill_foot
              camera look cavern_east 1.5 nowait
              camera look cavern_middle
            """)[0].Code;
        Assert.Equal(new[] { Op.WalkTo, Op.WalkTo, Op.PlaceAt, Op.FaceTo, Op.FaceTo, Op.WarpTo, Op.LookAt, Op.LookAt }, code.Select(i => i.Op));
        Assert.Equal(("player", "cavern_east", true), (code[0].Name, code[0].Other, code[0].Option));
        Assert.False(code[1].Option);
        Assert.Equal(("OpenLab", "hill_foot"), (code[5].Name, code[5].Other));
        Assert.Equal(("cavern_east", 1.5f, true), (code[6].Name, code[6].Seconds, code[6].Option));
        Assert.Equal((ScriptParser.PanSeconds, false), (code[7].Seconds, code[7].Option));
        Assert.Throws<ScriptException>(() => ScriptParser.Parse("x", "script S\n walkto player\n"));
        Assert.Throws<ScriptException>(() => ScriptParser.Parse("x", "script S\n placeat player \"a marker\"\n"));
        Assert.Throws<ScriptException>(() => ScriptParser.Parse("x", "script S\n warpto OpenLab\n"));
    }

    [Fact]
    public void TheScriptsOpenProblemsNameTheFileAndTheLine()
    {
        var library = ScriptLibrary.FromSources(
            ("lab_cave", """
                script Fine
                  walkto player cavern_east
                  placeat player tunnel_west
                  faceto player cavern_middle
                  faceto player player
                  camera look cavern_east
                  warpto "OpenLab" hill_foot
                  warp "PlayerHouse" 3 4
                  end
                script Grid
                  walk player up 2
                  place player 3 4
                  camera pan 3 4
                  walkto player nowhere
                  camera look nowhere
                  end
                """),
            ("PlayerHouse", """
                script Open
                  walkto player cavern_east
                  warpto "OpenLab" nowhere
                  warpto "NoSuchMap" hill_foot
                  warp "OpenLab" 3 4
                  end
                """),
            ("common", "script Any\n warpto \"OpenLab\" hill_foot\n placeat player anything\n end\n"));
        var map = OpenPlaceFiles.LoadRegion(LabFolder, withLand: false).Single();
        var areas = map.Areas.ToDictionary(a => a.Key, _ => map, StringComparer.OrdinalIgnoreCase);
        var problems = OpenWorlds.OpenProblems(library, areas, name => name == map.Name ? map : null);
        Assert.Equal(new[]
        {
            "lab_cave.txt(11): 'walk' takes steps of tiles, and lab_cave is a place of an open map: walkto <who> <marker>.",
            "lab_cave.txt(12): 'place' names a tile, and lab_cave is a place of an open map: placeat <who> <marker>.",
            "lab_cave.txt(13): 'camera pan' names tiles, and lab_cave is a place of an open map: camera look <marker>.",
            "lab_cave.txt(14): 'walkto' names the marker 'nowhere', which the open map of lab_cave hasn't.",
            "lab_cave.txt(15): 'camera look' names the marker 'nowhere', which the open map of lab_cave hasn't.",
            "PlayerHouse.txt(2): 'walkto' names a marker of an open map, and PlayerHouse is a grid place: its forms name tiles.",
            "PlayerHouse.txt(3): warpto goes to the marker 'nowhere', which OpenLab hasn't.",
            "PlayerHouse.txt(4): warpto goes to 'NoSuchMap', which is no open map.",
            "PlayerHouse.txt(5): 'OpenLab' is an open map, gone to by a marker: warpto \"OpenLab\" <marker>.",
        }, problems.OrderBy(p => p.StartsWith("lab", StringComparison.Ordinal) ? 0 : 1).ThenBy(p => p, StringComparer.Ordinal));
    }

    [Fact]
    public void AHeadlessHostPlaysTheOpenFormsAtOnceAndHoldsThemToItsMap()
    {
        var map = LabWithPlaces();
        var library = ScriptLibrary.FromSources(("lab_cave", """
            script S
              scene "Walk"
              placeat player tunnel_west
              walkto player cavern_east
              faceto player cavern_middle
              camera look cavern_middle
              walkto player nowhere
              endscene
              warpto "OpenLab" hill_foot
              end
            """), ("common", ""));
        foreach (bool hurry in new[] { false, true })
        {
            var host = new HeadlessScriptHost { OpenMap = map, OpenMapNamed = n => n == "OpenLab" ? map : null };
            var runner = new ScriptRunner(library, host) { Impatient = hurry };
            runner.Start(library.All.Single());
            runner.RunToEnd();
            Assert.Equal("hill_foot", host.OnMarker["player"]);
            Assert.Equal("cavern_middle", host.FacingToward["player"]);
            Assert.Equal("marker cavern_middle", host.CameraOn);
            Assert.Equal("OpenLab", host.OpenMapName);
            Assert.Equal(new OpenPlace("OpenLab", map.Marker("hill_foot")!.Pos, 0), host.OpenPlayer);
            Assert.Equal(new[] { "there is no marker 'nowhere' in the open map OpenLab" }, host.Problems);
            Assert.Equal(hurry ? 1 : 0, host.Skips);
        }
    }

    // ------------------------------------------------------------------ the rock over a carve and the content check

    [Fact]
    public void TheLabsTunnelHasLessThanHalfAMetreOfRockUnderTheHillsWestFlank()
    {
        // OW2's finding (the tunnel's roof 0.2 m under the hill's flank at x ±19, where the mesher kept faces turned): the
        // tunnel's alone, west of the cavern; the cavern's, the bridge's and the portals' are not told
        var land = RegionDescription.Read(LabFolder).Land();
        var thin = Assert.Single(CarveShells.Thin(land));
        Assert.Equal(0, thin.Carve);
        Assert.InRange(thin.X, -21, -18);
        Assert.InRange(thin.Thinnest, CarveShells.Open, CarveShells.LeastRock);
        Assert.InRange(thin.CeilingY, 2.9, 3.5);
    }

    [Theory]
    [InlineData(-1.0, true)]    // the crown 0.2 m under the meadow: told along its whole length
    [InlineData(-2.0, false)]   // 1.2 m of rock over it
    [InlineData(0.0, false)]    // open to the sky: a cutting
    public void ATunnelUnderLevelGroundIsToldWhereItsRoofIsThin(double y, bool told)
    {
        var land = new TerrainWorld { Name = "test", ChunksX = 1, ChunksZ = 1, MinX = -16, MinZ = -16, BaseHeight = 0 };
        land.Carves.Add(new Tunnel(new[] { new D3(-10, y, 0), new D3(10, y, 0) }, 0.8, Tunnel.NoFloor));
        var thin = CarveShells.Thin(land);
        Assert.Equal(told, thin.Count == 1);
        if (told) Assert.InRange(thin[0].Area, 10, 40);
    }

    static GameContent.OpenRegion LabRegion(List<Finding> unreadable) =>
        GameContent.ReadOpenRegion(LabFolder, "lab", unreadable);

    static ScriptLibrary LabScripts => ScriptLibrary.FromSources(("lab_cave", "script Cavern\n end\n"), ("common", ""));

    [Fact]
    public void TheContentCheckFindsTheLabsThinRoofAndNothingElse()
    {
        var unreadable = new List<Finding>();
        var region = LabRegion(unreadable);
        Assert.Empty(unreadable);
        var found = new List<Finding>();
        var house = LabHouse();
        OpenWorlds.CheckRegion(region, name => name == house.Name ? house : null, LabScripts, null, found);
        var finding = Assert.Single(found);
        Assert.Equal((Kind.Land, "lab/carve/hill.json", "the tunnel"), (finding.Kind, finding.File, finding.Name));
        Assert.Contains("of rock under the land at [-19", finding.What);
    }

    [Fact]
    public void TheContentCheckFindsAPlaceWrittenWrong()
    {
        var unreadable = new List<Finding>();
        var region = LabRegion(unreadable);
        var map = region.Maps!.Single();
        // A marker in the hill, a door into no room, a door whose room can't be come out of, a trigger in rock with no
        // script, a zone in the air with a species nobody has
        map.Add(new OpenMarker("in_the_hill", new OpenPos(0, 6_000, -10_000)));
        map.Add(new OpenDoor("nowhere_door", "lab_meadow", OpenShape.Box(new OpenPos(30_000, 0, 30_000), new OpenPos(31_000, 2_000, 31_000)), "NoSuchRoom", 1, 1, Direction.Up, "house_step"));
        map.Add(new OpenDoor("one_way_door", "lab_meadow", OpenShape.Box(new OpenPos(40_000, 0, 30_000), new OpenPos(41_000, 2_000, 31_000)), "Shut", 1, 1, Direction.Up, "no_marker"));
        map.Add(new OpenTrigger("buried", "lab_hill", OpenShape.Box(new OpenPos(-1_000, 5_000, -14_000), new OpenPos(1_000, 6_000, -13_000)), "Nothing"));
        map.Add(new OpenZone("sky", "lab_hill", new List<(int X, int Z)> { (-50_000, -50_000), (-40_000, -50_000), (-40_000, -40_000) }, 40_000, 50_000,
            new List<OpenSlot> { new("Nobodymon", 3, 4, 10) }, 3, 3));
        var house = LabHouse();
        var shut = GameDataFiles.Deserialize<MapFile>("""
            { "name": "Shut", "width": 3, "height": 3, "ground": ["###", "#_#", "###"], "solid": ["###", "#.#", "###"],
              "warps": [], "props": [], "signboards": [], "npcs": [], "wildEncounters": [] }
            """).ToMap();
        var found = new List<Finding>();
        OpenWorlds.CheckRegion(region, name => name == house.Name ? house : name == shut.Name ? shut : null, LabScripts, null, found);
        string Told(Finding f) => $"{f.Kind} {f.File} {f.Name}";
        Assert.Equal(new[]
        {
            "Land lab/carve/hill.json the tunnel",
            "Place lab/places/lab_hill.json in_the_hill",
            "Place lab/places/lab_hill.json buried",
            "Script lab/places/lab_hill.json buried",
            "Way lab/places/lab_meadow.json nowhere_door",
            "Way lab/places/lab_meadow.json one_way_door",
            "Way lab/places/lab_meadow.json one_way_door",
            "Data lab/places/lab_hill.json sky",
            "Place lab/places/lab_hill.json sky",
        }, found.Select(Told));
        Assert.Contains("goes into 'NoSuchRoom', which is no room", found[4].What);
        Assert.Contains("the marker 'no_marker', which OpenLab hasn't", found[5].What);
        Assert.Contains("where no way leads back out to OpenLab", found[6].What);
    }

    [Fact]
    public void ARoomsWayOutToAnOpenMapNeedsADoorOfItLeadingBackIn()
    {
        var map = OpenPlaceFiles.LoadRegion(LabFolder, withLand: false).Single();
        var found = new List<Finding>();
        OpenWorlds.CheckWaysIn(new[] { ("maps/OpenLabHouse.json", LabHouse()), ("maps/Other.json", LabHouse().Renamed("OtherHouse")) }, n => n == map.Name ? map : null, found);
        var finding = Assert.Single(found);
        Assert.Equal(("maps/Other.json", Kind.Way), (finding.File, finding.Kind));
        Assert.Contains("no door leads back into OtherHouse", finding.What);
    }
}

static class OpenWorldPlacesTestMaps
{
    /// <summary>A copy of a test room under another name.</summary>
    public static Map Renamed(this Map room, string name)
    {
        var file = MapFile.FromMap(room);
        file.Name = name;
        return file.ToMap();
    }
}
