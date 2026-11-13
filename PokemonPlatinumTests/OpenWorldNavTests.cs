using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenWorldBake;
using PokemonPlatinumEngine.OpenWorld;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 32 · OW4, the navigation mesh, paths and what can be come to, on the open test world
/// (tools/OpenWorldBake/worlds/lab): the mesh baked from level 0 the same twice; layered (the cavern's floor and the
/// hill's top other polygons over the same ground); a path through the tunnel and one over the hill, both walked by the
/// game's own capsule; the rock bridge crossed and the river swum only with the grant; the plateau's ledge a way down
/// only; and every place of the test world come to by <see cref="OpenWalk"/>. GPU-free.
/// </summary>
public class OpenWorldNavTests
{
    private readonly ITestOutputHelper output;
    public OpenWorldNavTests(ITestOutputHelper output) => this.output = output;

    static readonly Lazy<(RegionDescription Description, TerrainWorld Land)> lab = new(() =>
    {
        var description = RegionDescription.Read(OpenWorldBakeTests.LabFolder);
        return (description, description.Land());
    });

    /// <summary>The lab's level 0, every chunk, baked once for the class.</summary>
    static readonly Lazy<List<BakedChunk>> level0 = new(() =>
    {
        var (description, land) = lab.Value;
        return RegionBake.ChunksOf(description.World).Select(c => RegionBake.Finest(land, description.World, c.X, c.Z)).ToList();
    });

    static readonly Lazy<(OpenNavMesh Mesh, NavReport Report)> nav = new(() =>
    {
        var report = new NavReport();
        return (NavBake.Build(level0.Value, report: report), report);
    });

    static OpenNavMesh Mesh => nav.Value.Mesh;

    /// <summary>The lab's ground as the capsule collides with it.</summary>
    static OpenGround Ground()
    {
        var chunks = level0.Value.ToDictionary(c => (c.ChunkX, c.ChunkZ));
        return new OpenGround((x, z) => chunks.GetValueOrDefault((x, z)));
    }

    static readonly Lazy<OpenGround> ground = new(Ground);

    /// <summary>The highest ground at x, z under <paramref name="from"/> metres.</summary>
    static OpenPos On(double x, double z, double from = 60) =>
        OpenPos.FromMetres(x, ground.Value.Footing(x, z, from, -60)!.Value.Y, z);

    static OpenPos WestRoad => On(-58, 22);
    static OpenPos EastRoad => On(46, -4);
    static OpenPos CavernFloor => On(0, 0, 1);
    static OpenPos HillTop => On(0, -2);

    /// <summary>The places of the test world a body can walk to from the west road (OW5's markers will name them).</summary>
    static IEnumerable<OpenMarker> Places() => new[]
    {
        new OpenMarker("west_road", WestRoad), new OpenMarker("tunnel_west_mouth", On(-36, 0, 3)), new OpenMarker("tunnel", On(-24, 0, 3)),
        new OpenMarker("cavern", CavernFloor), new OpenMarker("hill_top", HillTop), new OpenMarker("tunnel_east_mouth", On(36, 0, 3)),
        new OpenMarker("east_road", EastRoad), new OpenMarker("ridge", On(54, -18)), new OpenMarker("pond_shore", On(40, -22)),
        new OpenMarker("below_the_ledge", On(-50, -32)), new OpenMarker("north_meadow", On(0, -55)), new OpenMarker("bridge_crest", On(24, 44)),
        new OpenMarker("south_meadow", On(24, 58)), new OpenMarker("south_west_corner", On(-58, 60)), new OpenMarker("south_east_corner", On(60, 60))
    };

    static string Told(NavPath path) => string.Join(" ", path.Corners.Select(c => $"({c.MetresX:0.0},{c.MetresY:0.0},{c.MetresZ:0.0})"));

    static string Told(CapsuleRoute walk) => string.Join("\n", walk.Events);

    /// <summary>The game's capsule walked from a path's first corner through the rest, each passed within 15 cm, as a follower steers.</summary>
    static CapsuleRoute Walk(NavPath path, MovementGrants grants = MovementGrants.None) =>
        CapsuleRoute.Walk(Ground(), path.Corners[0], path.Corners.Skip(1).Select((c, i) => new RouteStop(c.MetresX, c.MetresZ, $"corner {i + 1}")).ToList(),
            grants, reach: 0.15);

    /// <summary>Whether a polygon lies under the hill: low, within the tunnel's and the cavern's reach, with rock over it.</summary>
    static bool UnderTheHill(int polygon)
    {
        var hub = Mesh.Polygons[polygon].Hub;
        return hub.MetresY < 4 && Math.Abs(hub.MetresX) < 37 && Math.Abs(hub.MetresZ) < 13
            && ground.Value.Footing(hub.MetresX, hub.MetresZ, 1000, -1000) is { } top && top.Y - hub.MetresY > 2;
    }

    [Fact]
    public void WhatTheBakeFound()
    {
        var (mesh, report) = nav.Value;
        output.WriteLine(report.ToString());
        output.WriteLine($"{mesh.Vertices.Count} vertices, {mesh.Polygons.Count} polygons, {mesh.Links.Count} links, {mesh.Bytes().Length:N0} bytes");
        // Merged: far fewer polygons than walkable triangles, and nearly all of the land one piece
        Assert.True(report.Polygons * 12 < report.Walkable, $"{report.Polygons} polygons of {report.Walkable} triangles");
        Assert.True(report.Largest * 10 > report.Polygons * 9, report.ToString());
        Assert.True(report.DropLinks > 0 && report.Steps > 0, report.ToString());
    }

    [Fact]
    public void TheMeshBakedTwiceIsTheSameToTheByteWhateverOrderItsChunksAreGivenIn()
    {
        var once = Mesh.Bytes();
        Assert.Equal(once, NavBake.Build(level0.Value).Bytes());
        Assert.Equal(once, NavBake.Build(Enumerable.Reverse(level0.Value)).Bytes());
        // And the file read back is the mesh
        var read = OpenNavMesh.Read(new MemoryStream(once));
        Assert.Equal(once, read.Bytes());
        Assert.Equal(Mesh.Polygons.Count, read.Polygons.Count);
        Assert.Equal(Mesh.Links, read.Links);
        Assert.Throws<InvalidDataException>(() => OpenNavMesh.Read(new MemoryStream(once[..4].Concat(new byte[] { 99 }).ToArray())));
    }

    [Fact]
    public void EveryPolygonIsConvexNoWiderThanItsBoundAndKeepsToItsGround()
    {
        var mesh = Mesh;
        int far = 0, points = 0;
        double worst = 0;
        string where = "";
        foreach (var p in mesh.Polygons)
        {
            int n = p.Corners.Length;
            for (int k = 0; k < n; k++)
            {
                var a = mesh.Vertices[p.Corners[(k - 1 + n) % n]]; var b = mesh.Vertices[p.Corners[k]]; var c = mesh.Vertices[p.Corners[(k + 1) % n]];
                Assert.True(OpenNavMesh.Turn(a.X, a.Z, b.X, b.Z, c.X, c.Z) > 0, $"polygon {p.Id} doesn't turn the one way at corner {k}");
            }
            var xs = p.Corners.Select(c => mesh.Vertices[c].X).ToList(); var zs = p.Corners.Select(c => mesh.Vertices[c].Z).ToList();
            Assert.InRange(xs.Max() - xs.Min(), 1, NavBake.MostCells * 500);
            Assert.InRange(zs.Max() - zs.Min(), 1, NavBake.MostCells * 500);
            // Within one chunk
            Assert.Equal(p.ChunkX, OpenMap.ChunkOf(new OpenPos(xs.Min(), 0, zs.Min())).X);
            Assert.Equal(p.ChunkZ, OpenMap.ChunkOf(new OpenPos(xs.Min(), 0, zs.Min())).Z);
            // Its hub inside it, its ground there where the collision mesh has it, within a few centimetres
            var hub = OpenVec.Of(p.Hub);
            Assert.True(mesh.Contains(p, hub.X, hub.Z), $"polygon {p.Id}'s hub lies outside it");
            Assert.Equal(p.Id, mesh.PolygonAt(OpenPos.FromMetres(hub.X, mesh.FeetIn(p, hub.X, hub.Z), hub.Z)));
            for (int k = 0; k < n; k++)
            {
                // Halfway from the hub to each corner, the fan's height against the ground's own
                var at = (hub + mesh.Corner(p, k)) * 0.5;
                double fan = mesh.GroundIn(p, at.X, at.Z);
                var column = ground.Value.Column(at.X, at.Z);
                if (column.Count == 0) continue;   // past the land's edge (a swim polygon at the region's border)
                points++;
                double nearest = column.Select(h => Math.Abs(h.Y - fan)).Min();
                if (nearest > 0.12) far++;
                if (nearest > worst) { worst = nearest; where = $"polygon {p.Id} at {at}"; }
            }
        }
        output.WriteLine($"{far} points more than 12 cm off their ground; the worst {worst:0.000} m, {where}");
        Assert.True(far * 100 < points, $"{far} of {points} points stand more than 12 cm off their ground");
        Assert.True(worst < 0.5, $"the worst {worst:0.000} m, {where}");
    }

    [Fact]
    public void TheCavernsFloorAndTheHillsTopAreDifferentPolygonsOverTheSameGround()
    {
        var mesh = Mesh;
        Assert.True(HillTop.MetresY > 10, $"the top is at {HillTop}");
        var floor = mesh.PolygonAt(CavernFloor);
        var top = mesh.PolygonAt(HillTop);
        Assert.NotNull(floor);
        Assert.NotNull(top);
        Assert.NotEqual(floor, top);
        // Over the very same column
        var overTheFloor = mesh.PolygonAt(OpenPos.FromMetres(0, ground.Value.Footing(0, 0, 60, -60)!.Value.Y, 0));
        Assert.NotNull(overTheFloor);
        Assert.NotEqual(floor, overTheFloor);
        Assert.True(mesh.Polygons[overTheFloor!.Value].Hub.MetresY - mesh.Polygons[floor!.Value].Hub.MetresY > 10);
        // In the air between them, nothing
        Assert.Null(mesh.PolygonAt(OpenPos.FromMetres(0, 6, 0)));
        // And a walk across the cavern's floor finds its polygons under the hill's
        int layered = 0;
        for (double x = -10; x <= 10; x += 1)
            if (mesh.PolygonAt(OpenPos.FromMetres(x, 0, 0)) is { } low && ground.Value.Footing(x, 0, 60, -60) is { } high
                && mesh.PolygonAt(OpenPos.FromMetres(x, high.Y, 0)) is { } up && up != low) layered++;
        Assert.True(layered >= 18, $"{layered} columns with a floor under the top");
    }

    [Fact]
    public void AWayThroughTheTunnelAndOneOverTheHillBothExistAndDiffer()
    {
        // From the tunnel's west mouth to its east: straight through under the hill
        var west = On(-36, 0, 3); var east = On(36, 0, 3);
        var through = NavPath.Find(Mesh, west, east);
        Assert.NotNull(through);
        output.WriteLine($"through: {through!.Length:0.0} m, {through.Polygons.Count} polygons, {through.Visited} closed: {Told(through)}");
        Assert.Contains(through.Polygons, UnderTheHill);
        Assert.All(through.Corners, c => Assert.InRange(c.MetresY, -0.2, 1));
        Assert.InRange(through.Length, 72, 74);

        // The tunnel and the cavern barred: the way over the hill, longer and up its flank
        var over = NavPath.Find(Mesh, west, east, barred: UnderTheHill);
        Assert.NotNull(over);
        output.WriteLine($"over: {over!.Length:0.0} m, {over.Polygons.Count} polygons, {over.Visited} closed: {Told(over)}");
        Assert.DoesNotContain(over.Polygons, UnderTheHill);
        // Its length is the corners' (straight on the ground between them, the climb not counted), and still longer
        Assert.True(over.Length > through.Length, $"{over.Length:0.0} m over, {through.Length:0.0} m through");
        Assert.True(over.Corners.Max(c => c.MetresY) > 1.5, Told(over));
        Assert.Contains(over.Polygons, p => Mesh.Polygons[p].Hub.MetresY > 8);
        Assert.NotEqual(through.Corners, over.Corners);

        // Up onto the top over the cavern
        var up = NavPath.Find(Mesh, WestRoad, HillTop);
        Assert.NotNull(up);
        Assert.True(up!.Corners[^1].MetresY > 10);
        Assert.DoesNotContain(up.Polygons, UnderTheHill);

        // A* closes a few hundred polygons, not OW0's tens of thousands of triangles
        Assert.True(through.Visited < 3000, $"{through.Visited} closed");
    }

    [Fact]
    public void TheCapsuleWalksThePathsThroughTheTunnelAndOverTheHill()
    {
        foreach (var path in new[] { NavPath.Find(Mesh, WestRoad, EastRoad)!, NavPath.Find(Mesh, WestRoad, HillTop)!, NavPath.Find(Mesh, CavernFloor, HillTop)! })
        {
            var walk = Walk(path);
            output.WriteLine($"{path.Length:0.0} m, {walk.Samples.Count} ticks: {Told(path)}");
            Assert.True(walk.Finished && !walk.Stuck, $"{Told(path)}\n{Told(walk)}");
            Assert.True(walk.GivenUp.Count == 0, $"{Told(path)}\n{Told(walk)}");
            Assert.True(Math.Sqrt(walk.End.DistanceSquared(path.Corners[^1])) < 600, $"ended {walk.End}, the goal {path.Corners[^1]}");
        }
    }

    [Fact]
    public void TheCapsuleWalksPathsBetweenPlacesDrawnAtRandom()
    {
        // Thirty pairs of polygons drawn from a seed, on foot: wherever a path is found, the game's capsule follows it
        // to its end (on the right layer), never stuck and never giving a corner up
        var rng = new Random(4032);
        var mesh = Mesh;
        int walked = 0;
        var failures = new List<string>();
        for (int n = 0; n < 30; n++)
        {
            var a = mesh.Polygons[rng.Next(mesh.Polygons.Count)];
            var b = mesh.Polygons[rng.Next(mesh.Polygons.Count)];
            OpenPos At(OpenNavPolygon p) => OpenPos.FromMetres(p.Hub.MetresX, mesh.FeetIn(p, p.Hub.MetresX, p.Hub.MetresZ), p.Hub.MetresZ);
            if (a.Swim || b.Swim || NavPath.Find(mesh, At(a), At(b)) is not { } path) continue;
            walked++;
            var walk = Walk(path);
            double off = Math.Sqrt(walk.End.DistanceSquared(path.Corners[^1])) / 1000;
            if (!walk.Finished || walk.Stuck || walk.GivenUp.Count > 0 || off > 0.6)
                failures.Add($"{a.Id} to {b.Id}: {(walk.Finished ? "finished" : walk.Stuck ? "stuck" : "unfinished")}, {walk.GivenUp.Count} given up, ended {off:0.00} m off\n  {Told(path)}\n  {Told(walk).Replace("\n", "\n  ")}");
        }
        output.WriteLine($"{walked} paths walked");
        Assert.True(walked >= 20, $"{walked} paths");
        Assert.True(failures.Count == 0, string.Join("\n", failures));
    }

    [Fact]
    public void FromTheCavernUpOutOfTheTunnelAndOverTheRockBridge()
    {
        var path = NavPath.Find(Mesh, CavernFloor, On(24, 58));
        Assert.NotNull(path);
        output.WriteLine($"{path!.Length:0.0} m: {Told(path)}");
        Assert.Equal(OpenNavFlags.None, path.Uses & OpenNavFlags.Swim);
        // Over the river on the bridge's crest, two metres and more over the water
        Assert.All(path.Polygons, p => Assert.False(Mesh.Polygons[p].Swim));
        Assert.Contains(path.Polygons, p => Math.Abs(Mesh.Polygons[p].Hub.MetresZ - 44) < 2 && Mesh.Polygons[p].Hub.MetresY > 2);
        var walk = Walk(path);
        Assert.True(walk.Finished && !walk.Stuck && walk.GivenUp.Count == 0, $"{Told(path)}\n{Told(walk)}");
        Assert.Equal(0, walk.SwimTicks);
        Assert.True(Math.Sqrt(walk.End.DistanceSquared(path.Corners[^1])) < 600, $"ended {walk.End}, the goal {path.Corners[^1]}");
    }

    [Fact]
    public void TheRiverIsSwumOnlyWithTheGrant()
    {
        var north = On(8, 30); var south = On(8, 56);
        var dry = NavPath.Find(Mesh, north, south, OpenNavMesh.Allowed(MovementGrants.None))!;
        var wet = NavPath.Find(Mesh, north, south, OpenNavMesh.Allowed(MovementGrants.Swim))!;
        Assert.NotNull(dry);
        Assert.NotNull(wet);
        output.WriteLine($"dry {dry.Length:0.0} m: {Told(dry)}\nwet {wet.Length:0.0} m: {Told(wet)}");
        Assert.Equal(OpenNavFlags.None, dry.Uses & OpenNavFlags.Swim);
        Assert.NotEqual(OpenNavFlags.None, wet.Uses & OpenNavFlags.Swim);
        Assert.True(wet.Length + 10 < dry.Length, $"swum {wet.Length:0.0} m, round by the bridge {dry.Length:0.0} m");
        var swum = Walk(wet, MovementGrants.Swim);
        Assert.True(swum.Finished && !swum.Stuck, $"{Told(wet)}\n{Told(swum)}");
        Assert.True(swum.SwimTicks > 0);
    }

    [Fact]
    public void TheLedgeIsAWayDownOnly()
    {
        var top = On(-50, -52); var below = On(-50, -32);
        Assert.True(top.MetresY > 3, $"{top}");
        var down = NavPath.Find(Mesh, top, below);
        Assert.NotNull(down);
        Assert.NotEqual(OpenNavFlags.None, down!.Uses & OpenNavFlags.Drop);
        Assert.Null(NavPath.Find(Mesh, below, top));
        // Without drops there is no way down either: the ledge's top is an island
        Assert.Null(NavPath.Find(Mesh, top, below, OpenNavFlags.Walk));
        var walk = Walk(down);
        Assert.True(walk.Finished && !walk.Stuck, $"{Told(down)}\n{Told(walk)}");
        Assert.True(walk.End.MetresY < 1);
        // What can be come to from below never takes in the top; from the top, it takes in everything below too
        var fromBelow = OpenWalk.From(Mesh, new[] { below });
        Assert.False(fromBelow.Reaches(top));
        var fromTop = OpenWalk.From(Mesh, new[] { top });
        Assert.True(fromTop.Reaches(WestRoad) && fromTop.Reaches(CavernFloor) && fromTop.Reaches(HillTop));
    }

    [Fact]
    public void OpenWalkReachesEveryPlaceOfTheTestWorld()
    {
        var walk = OpenWalk.From(Mesh, new[] { WestRoad });
        var unreached = walk.Unreached(Places());
        Assert.True(unreached.Count == 0, "not come to: " + string.Join(", ", unreached.Select(m => $"{m.Name} {m.Pos}")));
        // Not the ledge's top, nor the stack standing in the pond
        Assert.False(walk.Reaches(On(-50, -52)));
        Assert.False(walk.Reaches(On(43, -40, 10)));
        // With the grant, the pond's water too; and a way that is no link (a warp) takes the walk to the ledge's top
        var swimming = OpenWalk.From(Mesh, new[] { WestRoad }, OpenNavMesh.Allowed(MovementGrants.Swim));
        Assert.True(swimming.Reached.Count > walk.Reached.Count);
        Assert.Contains(swimming.Reached, p => Mesh.Polygons[p].Swim);
        var warped = OpenWalk.From(Mesh, new[] { WestRoad }, ways: new[] { new OpenWay("a lift", On(-50, -32), On(-50, -52)) });
        Assert.True(warped.Reaches(On(-50, -52)));
        Assert.Equal(new[] { "a lift" }, warped.WaysTaken);
    }

    [Fact]
    public void TheGameReadsTheMeshBesideTheChunksAndTheBotAndTheWalkOfTheGameAskIt()
    {
        // The lab's level 0 and its navigation mesh written as the baker writes them, opened as the game opens a region
        string dir = Path.Combine(Path.GetTempPath(), "openlab-nav-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(dir, "0"));
        try
        {
            var (description, _) = lab.Value;
            var index = new BakedIndexFile
            {
                Format = BakedChunk.Format, Region = description.World.Region, ChunkSide = OpenMap.ChunkSide,
                CellMm = 500, LevelErrorsMm = new List<int> { 0 }, Source = description.Source
            };
            foreach (var chunk in level0.Value)
            {
                index.Chunks.Add(new[] { chunk.ChunkX, chunk.ChunkZ });
                File.WriteAllBytes(Path.Combine(dir, BakedChunk.FileName(chunk.ChunkX, chunk.ChunkZ, 0)), chunk.Bytes());
            }
            File.WriteAllText(Path.Combine(dir, "index.json"), System.Text.Json.JsonSerializer.Serialize(index, PokemonPlatinumEngine.Data.GameDataFiles.Json));
            var land = BakedRegion.Open(dir);
            Assert.Null(land.Nav);   // a bake without its mesh has none
            File.WriteAllBytes(Path.Combine(dir, OpenNavMesh.FileName), Mesh.Bytes());
            var map = new OpenMap("OpenLab", "lab", Places()) { Land = BakedRegion.Open(dir) };
            Assert.Equal(Mesh.Bytes(), map.Land!.Nav!.Bytes());

            // The bot's route over it is the path's corners
            var route = PokemonPlatinumEngine.Core.Autoplay.Policy.Route(map, WestRoad, HillTop);
            Assert.Equal(NavPath.Find(Mesh, WestRoad, HillTop)!.Corners, route);
            Assert.Null(PokemonPlatinumEngine.Core.Autoplay.Policy.Route(map, On(-50, -32), On(-50, -52)));
            // The walk of the game takes an open map: every place, and with everything granted the water too
            var walk = PokemonPlatinumEngine.Overworld.WorldWalk.Open(map, new[] { map.Marker("west_road")!.Pos })!;
            Assert.Empty(walk.Unreached(map.Markers));
            Assert.Contains(walk.Reached, p => Mesh.Polygons[p].Swim);
            // A map with no mesh has neither
            var bare = new OpenMap("Bare", "lab");
            Assert.Null(PokemonPlatinumEngine.Core.Autoplay.Policy.Route(bare, WestRoad, HillTop));
            Assert.Null(PokemonPlatinumEngine.Overworld.WorldWalk.Open(bare, new[] { WestRoad }));
        }
        finally { Directory.Delete(dir, true); }
    }

    [Fact]
    public void TheBakeOfTheRegionWritesItsMeshBesideItsChunks()
    {
        // RegionBake.Files gives navmesh.bin with every chunk, and none when only some are baked
        var (description, _) = lab.Value;
        var some = RegionBake.Files(description, (x, z) => x == 0 && z == 0);
        Assert.DoesNotContain(OpenNavMesh.FileName, some.Keys);
        var report = new NavReport();
        var all = RegionBake.Files(description, navReport: report);
        // baked with the places' solids and water (plan 32 · OW6: the house on the meadow is walked round)
        var (solids, waters) = RegionBake.PlacedOf(description);
        Assert.Contains(solids, s => s.Name == "the house");
        var withPlaces = new NavReport();
        // the lab is ridden (plan 34 · PD2: its world.json says "mount"), so its bake holds the mount's links
        Assert.True(description.World.Mount);
        Assert.Equal(NavBake.Build(level0.Value, solids, waters, report: withPlaces, mount: true).Bytes(), all[OpenNavMesh.FileName]);
        Assert.Equal(withPlaces.ToString(), report.ToString());
        Assert.NotEqual(Mesh.Bytes(), all[OpenNavMesh.FileName]);
        // --check holds it as it holds the chunks: a changed byte of the mesh is a difference
        string dir = Path.Combine(Path.GetTempPath(), "openlab-navcheck-" + Guid.NewGuid().ToString("N"));
        try
        {
            RegionBake.Write(dir, all);
            Assert.Empty(RegionBake.Differences(dir, all));
            var bytes = File.ReadAllBytes(Path.Combine(dir, OpenNavMesh.FileName));
            bytes[^1] ^= 1;
            File.WriteAllBytes(Path.Combine(dir, OpenNavMesh.FileName), bytes);
            Assert.Contains(RegionBake.Differences(dir, all), d => d.StartsWith(OpenNavMesh.FileName));
        }
        finally { Directory.Delete(dir, true); }
    }

    [Fact]
    public void APathIsTheSameEveryTime()
    {
        var a = NavPath.Find(Mesh, WestRoad, On(24, 58))!;
        var b = NavPath.Find(OpenNavMesh.Read(new MemoryStream(Mesh.Bytes())), WestRoad, On(24, 58))!;
        Assert.Equal(a.Corners, b.Corners);
        Assert.Equal(a.Polygons, b.Polygons);
        Assert.Equal(a.Length, b.Length);
    }

    [Fact]
    public void ABoxAcrossTheTunnelsMouthIsWalkedRound()
    {
        // A gate across the west mouth: the mesh baked with it has no way through it on the floor; from one side of it to
        // the other the path goes up and round (and drops into the cutting from its lip)
        var gate = OpenBox.Metres("the gate", -38, -1, -12, -37, 4, 12);
        var mesh = NavBake.Build(level0.Value, new[] { gate });
        Assert.DoesNotContain(mesh.Polygons, p => p.Hub.MetresY < 3 && Math.Abs(p.Hub.MetresZ) < 12
            && p.Corners.Any(c => mesh.Vertices[c].MetresX > -38 && mesh.Vertices[c].MetresX < -37));
        var before = On(-41, 0, 3); var after = On(-34, 0, 3);
        Assert.InRange(NavPath.Find(Mesh, before, after)!.Length, 6.9, 7.5);
        var round = NavPath.Find(mesh, before, after);
        Assert.NotNull(round);
        output.WriteLine($"round the gate {round!.Length:0.0} m: {Told(round)}");
        Assert.True(round.Length > 20, Told(round));
        for (int i = 1; i < round.Corners.Count; i++)
        {
            // No leg of it crosses the gate's line within its width, low
            var a = round.Corners[i - 1]; var b = round.Corners[i];
            if ((a.MetresX + 37.5) * (b.MetresX + 37.5) >= 0) continue;
            double t = (-37.5 - a.MetresX) / (b.MetresX - a.MetresX), z = a.MetresZ + (b.MetresZ - a.MetresZ) * t, y = a.MetresY + (b.MetresY - a.MetresY) * t;
            Assert.False(Math.Abs(z) < 12 && y < 4, $"a leg crosses the gate at z {z:0.0}: {Told(round)}");
        }
        Assert.NotNull(NavPath.Find(mesh, WestRoad, CavernFloor));
        // A kerb is stepped onto: a box 30 cm high on the meadow has a step to its top and back
        var kerb = OpenBox.Metres("the kerb", -20, -0.5, 30, -16, 0.3, 34);
        var withKerb = NavBake.Build(level0.Value, new[] { kerb });
        var onTop = withKerb.PolygonAt(OpenPos.FromMetres(-18, 0.3, 32));
        Assert.NotNull(onTop);
        Assert.Contains(withKerb.Links, l => l.To == onTop && l.OffMesh && (l.Flags & OpenNavFlags.Drop) == 0);
        Assert.Contains(withKerb.Links, l => l.From == onTop && l.OffMesh);
    }

    [Fact]
    public void ARowOfSolidTrunksAcrossTheTunnelsMouthIsWalkedRoundAndTreesWithoutTrunksAreNot()
    {
        // Trunks 0.75 m apart across the west mouth (a gap of 0.31 m between posts, under the capsule's 0.7 m): the mesh
        // goes round as it does round the gate; the same trees as scenery change nothing of the mesh
        var row = new List<OpenTree>();
        for (double z = -12; z <= 12.01; z += 0.75)
            row.Add(new OpenTree(OpenPos.Millimetres(-37.5), OpenPos.Millimetres(z), 0, OpenTreeKind.Pine, 1000, 0, 0) { Trunk = true });
        // The game's ground has the posts after its boxes, and their feet on the tunnel's floor
        var chunks = level0.Value.ToDictionary(c => (c.ChunkX, c.ChunkZ));
        var ground = new OpenGround((x, z) => chunks.GetValueOrDefault((x, z)), null, null, row);
        output.WriteLine($"{ground.Trunks.Count} posts of {row.Count} trees");
        // A tree whose foot is on no ground (in the rock either side of the mouth) has none
        Assert.InRange(ground.Trunks.Count, 8, row.Count);
        Assert.All(ground.Trunks, p => Assert.True(p.Pillar && p.Min.MetresY < 0.5 && p.Max.MetresY > 3.5, p.ToString()));
        var withTrunks = new NavReport();
        var mesh = NavBake.Build(level0.Value, report: withTrunks, trees: row);
        Assert.True(withTrunks.Walled > nav.Value.Report.Walled, $"walled {withTrunks.Walled} with the trunks, {nav.Value.Report.Walled} without");
        // No polygon between the posts, and no step over the ground slips between them (the rays above would)
        Assert.Null(mesh.PolygonAt(OpenPos.FromMetres(-37.5, 0, -0.3)));
        var before = On(-41, 0, 3); var after = On(-34, 0, 3);
        var round = NavPath.Find(mesh, before, after);
        Assert.NotNull(round);
        output.WriteLine($"round the trunks {round!.Length:0.0} m: {Told(round)}");
        Assert.True(round.Length > 20, Told(round));
        var scenery = row.Select(t => t with { Trunk = false }).ToList();
        Assert.Equal(Mesh.Bytes(), NavBake.Build(level0.Value, trees: scenery).Bytes());
    }
}
