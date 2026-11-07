using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using OpenWorldBake;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.OpenWorld;
using Xunit;
using OpenBody = PokemonPlatinumEngine.OpenWorld.OpenBody;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 32 · OW3 on the open test world (tools/OpenWorldBake/worlds/lab), the game's capsule walking level 0's baked
/// ground as the game collides with it: through the tunnel and the cavern under the hill, over the hill's top above
/// the cavern, over the rock bridge and not into the river, stopped at the river's edge without the grant and swimming
/// it with, down the plateau's ledge and never up it, pushed at the cavern's walls every way without ever falling
/// through them (decision 14, OW0's bug), and the same walk twice, its chunks read in any order, on the same
/// micrometre. GPU-free.
/// </summary>
public class OpenWorldCapsuleTests
{
    const int T = OpenClock.TicksPerSecond;

    static readonly Lazy<(RegionDescription Description, TerrainWorld Land)> lab = new(() =>
    {
        var description = RegionDescription.Read(OpenWorldBakeTests.LabFolder);
        return (description, description.Land());
    });

    static readonly ConcurrentDictionary<(int, int), BakedChunk?> chunks = new();

    /// <summary>The lab's ground as the game collides with it, made afresh (its chunks baked once for the class).</summary>
    static OpenGround Ground() => CapsuleRoute.GroundOf(lab.Value.Description, lab.Value.Land, chunks);

    static OpenPos On(OpenGround ground, double x, double z, double from = 60) =>
        OpenPos.FromMetres(x, ground.Footing(x, z, from, -60)!.Value.Y, z);

    static string Told(CapsuleRoute walk) => string.Join("\n", walk.Events);

    static readonly RouteStop[] Tunnel =
    {
        new(-50, 8, "the west road's bend"), new(-36, 0, "the tunnel's west mouth"), new(-24, 0, "in the tunnel"),
        new(-8, 0, "the cavern's west"), new(0, 0, "the cavern's middle"), new(9, 1.5, "the cavern's east"),
        new(24, 0, "the tunnel east"), new(36, 0, "the tunnel's east mouth"), new(46, -4, "the east road")
    };

    [Fact]
    public void ThroughTheTunnelAndTheCavernUnderTheHill()
    {
        var ground = Ground();
        var walk = CapsuleRoute.Walk(ground, On(ground, -58, 22), Tunnel);
        Assert.True(walk.Finished && walk.GivenUp.Count == 0 && !walk.Stuck, Told(walk));
        // Under the hill the feet stay on the tunnel's and the cavern's flat floor, never up on the hill or under the floor
        var under = walk.Samples.Where(s => Math.Abs(s.Pos.X) < 24_000).ToList();
        Assert.True(under.Count > 30 * T / 2, $"{under.Count} ticks under the hill");
        Assert.All(under, s => Assert.InRange(s.Pos.Y, -20, 500));
        Assert.All(under, s => Assert.Equal(OpenStance.Standing, s.Stance));
        // ... with the hill's top over them: the column over the cavern's middle meets ground facing up over 10 m higher
        var column = ground.Column(0, 0);
        Assert.True(column[0].Y > 10, $"the top over the cavern is {column[0].Y:0.00} m");
        Assert.True(ground[column[0].Triangle].FacesUp);
        Assert.Contains(column, c => Math.Abs(c.Y) < 0.05 && ground[c.Triangle].FacesUp);
        // And a walk of 1.5 m/s: the route's length over its ticks
        Assert.DoesNotContain(walk.Samples, s => s.Stance == OpenStance.Falling);
    }

    [Fact]
    public void OverTheHillAndItsTopOverTheCavern()
    {
        var ground = Ground();
        var stops = new RouteStop[] { new(0, -26, "the north foot"), new(0, -12, "the north flank"), new(0, -2, "the top"), new(0, 14, "the south flank"), new(0, 34, "the south meadow") };
        var walk = CapsuleRoute.Walk(ground, On(ground, 0, -40), stops);
        Assert.True(walk.Finished && walk.GivenUp.Count == 0 && !walk.Stuck, Told(walk));
        Assert.True(walk.HighestY > 11, $"up to {walk.HighestY:0.00} m");
        // Over the cavern the feet are on the top, ten metres and more over the cavern's floor walked under it
        var top = walk.Samples.Where(s => Math.Abs(s.Pos.Z) < 4_000).ToList();
        Assert.NotEmpty(top);
        Assert.All(top, s => Assert.True(s.Pos.Y > 10_000, $"{s.Pos}"));
        Assert.DoesNotContain(walk.Samples, s => s.Sliding);
    }

    [Fact]
    public void OverTheRockBridgeAndNotIntoTheRiver()
    {
        var ground = Ground();
        var stops = new RouteStop[] { new(24, 31, "before the bridge"), new(24, 44, "the bridge's crest"), new(24, 57, "beyond the river") };
        var walk = CapsuleRoute.Walk(ground, On(ground, 24, 24), stops);
        Assert.True(walk.Finished && walk.GivenUp.Count == 0 && !walk.Stuck, Told(walk));
        Assert.Equal(0, walk.SwimTicks);
        // On the crest, over the water's surface (under a metre below nought) by more than two metres
        var crest = walk.Samples.Where(s => Math.Abs(s.Pos.Z - 44_000) < 1_500).ToList();
        Assert.NotEmpty(crest);
        Assert.All(crest, s => Assert.True(s.Pos.Y > 2_000, $"{s.Pos}"));
        Assert.True(ground.WaterAt(24, 44) is { } surface && surface < 0, "the river runs under the bridge");
    }

    [Fact]
    public void TheRiverStopsTheBodyAtItsEdgeWithoutTheGrantAndIsSwumWithIt()
    {
        var ground = Ground();
        var stops = new RouteStop[] { new(8, 36, "the north bank"), new(8, 54, "the south bank") };
        var dry = CapsuleRoute.Walk(ground, On(ground, 8, 28), stops);
        Assert.Contains("the south bank", dry.GivenUp);
        Assert.Equal(0, dry.SwimTicks);
        Assert.True(dry.End.Z < 44_000, $"{dry.End}\n{Told(dry)}");
        Assert.Contains(dry.Events, e => e.Contains("the water's edge"));

        var wet = CapsuleRoute.Walk(ground, On(ground, 8, 28), stops, MovementGrants.Swim);
        Assert.True(wet.Finished && wet.GivenUp.Count == 0 && !wet.Stuck, Told(wet));
        Assert.True(wet.SwimTicks > T, $"{wet.SwimTicks} ticks swum");
        Assert.Equal(OpenStance.Standing, wet.Samples[^1].Stance);
    }

    [Fact]
    public void TheLedgeIsAWayDownOnly()
    {
        var ground = Ground();
        var down = CapsuleRoute.Walk(ground, On(ground, -50, -52), new RouteStop[] { new(-50, -32, "below the ledge") });
        Assert.True(down.Finished && !down.Stuck, Told(down));
        Assert.True(down.Samples[0].Pos.Y > 3_000, $"began at {down.Samples[0].Pos}");
        Assert.True(down.End.Y < 1_000, $"{down.End}");
        Assert.True(down.SlideTicks > 0 || down.Samples.Any(s => s.Stance == OpenStance.Falling), "came down the steep face");

        var up = CapsuleRoute.Walk(ground, On(ground, -50, -32), new RouteStop[] { new(-50, -52, "on the ledge", Run: true) });
        Assert.Contains("on the ledge", up.GivenUp);
        Assert.True(up.HighestY < 1.0, $"up to {up.HighestY:0.00} m\n{Told(up)}");
    }

    [Fact]
    public void PushedAtTheCavernsWallsEveryWayTheBodyNeverFallsThroughThem()
    {
        // Decision 14: OW0's capsule, pushed over the cavern wall's steep foot, found no walkable floor under it and
        // fell through the world. From the cavern's middle, at a walk and at a run, into its walls 24 ways for six
        // seconds each, and into the tunnel's walls on both sides
        var ground = Ground();
        foreach (var (x, z, ways) in new[] { (0.0, 0.0, 24), (-20.0, 0.0, 2), (18.0, 0.0, 2) })
            foreach (bool run in new[] { false, true })
                for (int w = 0; w < ways; w++)
                {
                    double angle = ways == 2 ? Math.PI * w : Math.PI * 2 * w / ways;
                    var body = new OpenBody(OpenPos.FromMetres(x, 0, z));
                    var input = new OpenInput(Math.Sin(angle), Math.Cos(angle), run);
                    int falling = 0, longest = 0, lowest = int.MaxValue, highest = int.MinValue;
                    for (int t = 0; t < 6 * T; t++)
                    {
                        body.Step(ground, input);
                        falling = body.Stance == OpenStance.Falling ? falling + 1 : 0;
                        longest = Math.Max(longest, falling);
                        lowest = Math.Min(lowest, body.Pos.Y);
                        highest = Math.Max(highest, body.Pos.Y);
                    }
                    string at = $"from {x},{z} way {w} of {ways}{(run ? " running" : "")}: ended {body.Pos}";
                    Assert.True(lowest >= -20, $"{at}, down to {lowest} mm");
                    Assert.True(highest < 4_000, $"{at}, up to {highest} mm");
                    Assert.True(longest < T / 4, $"{at}, falling {longest} ticks");
                    Assert.Equal(OpenStance.Standing, body.Stance);
                }
    }

    [Fact]
    public void TheSameWalkTwiceEndsOnTheSameMicrometreWhateverOrderItsChunksAreReadIn()
    {
        var start = On(Ground(), -58, 22);
        var once = CapsuleRoute.Walk(Ground(), start, Tunnel);
        var twice = CapsuleRoute.Walk(Ground(), start, Tunnel);
        Assert.Equal(once.Hash(), twice.Hash());
        Assert.Equal(once.End, twice.End);
        // A ground that read its chunks from the far side first (as a game continued in the east would)
        var other = Ground();
        foreach (var (x, z) in new[] { (60.0, 60.0), (40.0, -50.0), (5.0, 5.0), (-30.0, 10.0), (-60.0, -60.0) }) other.Footing(x, z, 60, -60);
        var third = CapsuleRoute.Walk(other, start, Tunnel);
        Assert.Equal(once.Hash(), third.Hash());
        Assert.True(once.Samples.Count > 50 * T, $"{once.Samples.Count} ticks");
    }

    [Fact]
    public void TheCapsulesGroundIsLevelZeroAsTheGameLoadsIt()
    {
        // What the capsule collides with is the very bytes the baker writes for level 0, its own ground alone
        var (description, land) = lab.Value;
        var finest = RegionBake.Finest(land, description.World, 0, -1);
        Assert.Equal(RegionBake.Chunk(land, description.World, 0, -1)[0].Bytes(), finest.Bytes());
        var ground = new OpenGround((x, z) => x == 0 && z == -1 ? finest : null);
        ground.Footing(16, -16, 60, -60);
        Assert.Equal(finest.GroundTriangles, ground.TrianglesRead);
    }

    // ------------------------------------------------------------- the lab as the game loads it

    /// <summary>
    /// The lab's level 0 written as a baked folder the game opens (<see cref="BakedRegion.Open"/>): an index and each
    /// chunk's level-0 file (the far levels, which nothing here reads, left out). Written once for the run.
    /// </summary>
    internal static string LabBaked => labBaked.Value;
    static readonly Lazy<string> labBaked = new(() =>
    {
        var (description, land) = lab.Value;
        string dir = Path.Combine(Path.GetTempPath(), "openlab-level0-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(dir, "0"));
        var index = new BakedIndexFile
        {
            Format = BakedChunk.Format, Region = description.World.Region, ChunkSide = OpenMap.ChunkSide,
            CellMm = 500, LevelErrorsMm = new List<int> { 0, 60, 300 }, Source = description.Source
        };
        foreach (var (x, z) in RegionBake.ChunksOf(description.World))
        {
            index.Chunks.Add(new[] { x, z });
            File.WriteAllBytes(Path.Combine(dir, BakedChunk.FileName(x, z, 0)), chunks.GetOrAdd((x, z), _ => RegionBake.Finest(land, description.World, x, z))!.Bytes());
        }
        File.WriteAllText(Path.Combine(dir, "index.json"), JsonSerializer.Serialize(index, GameDataFiles.Json));
        return dir;
    });

    /// <summary>The open test world's map as the game knows it, on its baked level 0.</summary>
    internal static OpenMap LabMap(string name = "OpenLab") =>
        new(name, "lab", new[] { new OpenMarker("west_road", On(Ground(), -58, 22), OpenYaw.East) }) { Land = BakedRegion.Open(LabBaked) };

    [Fact]
    public void TheMapsGroundIsItsLandsLevelZero()
    {
        var map = LabMap();
        var start = map.Marker("west_road")!.Pos;
        var walk = CapsuleRoute.Walk(map.Ground, start, Tunnel);
        Assert.Equal(CapsuleRoute.Walk(Ground(), start, Tunnel).Hash(), walk.Hash());
        // A solid laid on the map is collided with too: a wall across the tunnel's west mouth stops the walk there
        map.Add(OpenBox.Metres("the gate", -38, -1, -4, -37, 4, 4));
        var held = CapsuleRoute.Walk(map.Ground, start, Tunnel);
        Assert.Contains("the tunnel's west mouth", held.GivenUp);
        Assert.True(held.Samples.Where(s => s.Stop <= 2).All(s => s.Pos.X < -38_000 + 1), Told(held));
    }
}
