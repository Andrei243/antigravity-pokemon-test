using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.OpenWorld;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 32 · OW3, collision and the capsule, on grounds made in the tests themselves (boxes laid as the kit's
/// buildings will be, and slopes as a baked chunk of a few triangles): the speeds to the micrometre, the stick's
/// analog speed and its slant, the step up and the wall, the corridor that passes and the one that doesn't, the steep
/// face slid down and never climbed, drops with and without a landing, nothing through a wall at a run in frames of a
/// tenth of a second, no footing on a face turned down, water waded, refused at its edge and swum behind the grant,
/// and the same frames giving the same micrometres. GPU-free.
/// </summary>
public class OpenMovementTests
{
    const int T = OpenClock.TicksPerSecond;
    static readonly OpenBox Floor = OpenBox.Metres("floor", -40, -1, -40, 40, 0, 40);

    static OpenGround Ground(params OpenBox[] boxes) => new(null, boxes);

    /// <summary>A baked chunk at 0, 0 of flat-shaded strips running north to south: the ground's height at each x of <paramref name="xs"/> (metres), across z 0 to 32.</summary>
    internal static BakedChunk Profile(params (double X, double Y)[] xs)
    {
        var positions = new List<int>();
        var indices = new List<int>();
        for (int i = 0; i + 1 < xs.Length; i++)
        {
            int b = positions.Count / 3;
            foreach (var (x, y, z) in new[] { (xs[i].X, xs[i].Y, 0.0), (xs[i].X, xs[i].Y, 32.0), (xs[i + 1].X, xs[i + 1].Y, 32.0), (xs[i + 1].X, xs[i + 1].Y, 0.0) })
            {
                positions.Add(OpenPos.Millimetres(x)); positions.Add(OpenPos.Millimetres(y)); positions.Add(OpenPos.Millimetres(z));
            }
            // Counter-clockwise seen from the air: west-north, west-south, east-south; west-north, east-south, east-north
            indices.AddRange(new[] { b, b + 1, b + 2, b, b + 2, b + 3 });
        }
        int n = positions.Count / 3;
        return new BakedChunk
        {
            ChunkX = 0, ChunkZ = 0, Level = 0, ErrorMm = 0,
            Positions = positions.ToArray(), Normals = new sbyte[n * 3], Covers = new OpenCover[n],
            Indices = indices.ToArray(), GroundTriangles = indices.Count / 3, ApronTriangles = 0,
            Water = Enumerable.Repeat(BakedChunk.NoWater, BakedChunk.WaterCells * BakedChunk.WaterCells).ToArray()
        };
    }

    static OpenGround Chunk(BakedChunk chunk, IEnumerable<OpenBox>? boxes = null, IEnumerable<OpenWaterBox>? waters = null) =>
        new((x, z) => x == 0 && z == 0 ? chunk : null, boxes, waters);

    /// <summary>The body stepped so many ticks with one input, every tick's place kept.</summary>
    static List<(OpenPos Pos, OpenStance Stance, bool Hard, int? Landed, bool Water, bool Sliding)> Run(OpenBody body, OpenGround ground, OpenInput input, int ticks, MovementGrants grants = MovementGrants.None)
    {
        var trace = new List<(OpenPos, OpenStance, bool, int?, bool, bool)>();
        for (int i = 0; i < ticks; i++)
        {
            body.Step(ground, input, grants);
            trace.Add((body.Pos, body.Stance, body.HardLanding, body.Landed, body.StoppedAtWater, body.Sliding));
        }
        return trace;
    }

    static readonly OpenInput East = new(1, 0), South = new(0, 1);

    [Fact]
    public void ASecondOfWalkingIsOneAndAHalfMetresAndOfRunningFourAndAHalfToTheMicrometre()
    {
        var ground = Ground(Floor);
        var walk = new OpenBody(new OpenPos(0, 0, 0));
        Run(walk, ground, East, T);
        Assert.Equal((1_500_000L, 0L, 0L), walk.Micrometres);
        Assert.Equal(OpenYaw.East, walk.Yaw);
        var run = new OpenBody(new OpenPos(0, 0, 0));
        Run(run, ground, East with { Run = true }, T);
        Assert.Equal(4_500_000L, run.Micrometres.X);
        // The stick's analog speed: half a tilt is half the pace
        var half = new OpenBody(new OpenPos(0, 0, 0));
        Run(half, ground, new OpenInput(0, -0.5), T);
        Assert.Equal((0L, 0L, -750_000L), half.Micrometres);
        Assert.Equal(OpenYaw.North, half.Yaw);
        // A tilt past one is one
        var over = new OpenBody(new OpenPos(0, 0, 0));
        Run(over, ground, new OpenInput(2, 0), T);
        Assert.Equal(1_500_000L, over.Micrometres.X);
        // A stick 2° off an axis goes slantwise: 1.5 m · sin 2° ≈ 52.3 mm across in the second (decision 11)
        var slant = new OpenBody(new OpenPos(0, 0, 0));
        double a = 2 * Math.PI / 180;
        Run(slant, ground, new OpenInput(Math.Cos(a), Math.Sin(a)), T);
        Assert.InRange(slant.Micrometres.Z, 52_300, 52_400);
        Assert.InRange(slant.Micrometres.X, 1_499_000, 1_499_200);
        Assert.Equal(OpenYaw.East - 364, slant.Yaw);   // 2° short of east, toward south: 65,536 · 2/360 ≈ 364
        // And a stick at rest doesn't move it, or turn it
        Run(slant, ground, OpenInput.None, T);
        Assert.InRange(slant.Micrometres.X, 1_499_000, 1_499_200);
        Assert.Equal(OpenYaw.East - 364, slant.Yaw);
    }

    [Fact]
    public void TheYawIsTheWayTheStickPointsOnAnyMachine()
    {
        // The arctangent of the four operations against the runtime's own, round a whole turn
        for (int i = 0; i < 3600; i++)
        {
            double angle = i * Math.PI * 2 / 3600;
            double x = Math.Sin(angle) * (0.3 + i % 7 * 0.1), z = Math.Cos(angle) * (0.3 + i % 7 * 0.1);
            int want = OpenYaw.Normalize((int)Math.Round(Math.Atan2(x, z) * (OpenYaw.Turn / (Math.PI * 2)), MidpointRounding.AwayFromZero));
            int got = OpenTriangles.YawOf(x, z);
            int off = Math.Abs(got - want);
            Assert.True(Math.Min(off, OpenYaw.Turn - off) <= 1, $"{angle}: {got} for {want}");
        }
        Assert.Equal(OpenYaw.South, OpenTriangles.YawOf(0, 1));
        Assert.Equal(OpenYaw.East, OpenTriangles.YawOf(1, 0));
        Assert.Equal(OpenYaw.North, OpenTriangles.YawOf(0, -1));
        Assert.Equal(OpenYaw.West, OpenTriangles.YawOf(-1, 0));
    }

    [Fact]
    public void AKerbOfThirtyCentimetresIsSteppedAndAWallOfFiftyIsNot()
    {
        var kerb = new OpenBody(new OpenPos(0, 0, 0));
        var trace = Run(kerb, Ground(Floor, OpenBox.Metres("kerb", 2, -1, -5, 6, 0.3, 5)), East, 3 * T);
        Assert.Equal(300, kerb.Pos.Y);
        Assert.True(kerb.Pos.X > 4000, $"{kerb.Pos}");
        Assert.DoesNotContain(trace, s => s.Stance != OpenStance.Standing);

        var wall = new OpenBody(new OpenPos(0, 0, 0));
        trace = Run(wall, Ground(Floor, OpenBox.Metres("wall", 2, -1, -5, 6, 0.5, 5)), East, 3 * T);
        Assert.Equal(0, wall.Pos.Y);
        // Held off the wall by the lowest sphere, whose centre is 0.7 m up: within √(0.35² − 0.2²) ≈ 0.287 m of the face
        Assert.InRange(wall.Pos.X, 2000 - 300, 2000 - 270);
        Assert.All(trace, s => Assert.Equal(0, s.Pos.Y));
        Assert.True(wall.Pushes > 0);

        // And a wall the body's height, the same
        var high = new OpenBody(new OpenPos(0, 0, 0));
        Run(high, Ground(Floor, OpenBox.Metres("wall", 2, -1, -5, 6, 3, 5)), East with { Run = true }, 3 * T);
        Assert.InRange(high.Pos.X, 2000 - 351, 2000 - 349);
    }

    [Fact]
    public void SlantwiseIntoAWallTheBodySlidesAlongItAndRoundAnInsideCornerItStops()
    {
        // A wall along x = 2 from z −10 to 10, walked into at 45°: held 0.35 m off it, it goes on south at the stick's share
        var ground = Ground(Floor, OpenBox.Metres("wall", 2, -1, -10, 4, 3, 10), OpenBox.Metres("end wall", -6, -1, 6, 4, 3, 8));
        var body = new OpenBody(new OpenPos(0, 0, 0));
        double s = Math.Sqrt(0.5);
        var trace = Run(body, ground, new OpenInput(s, s), 3 * T);
        Assert.InRange(body.Pos.X, 1_649, 1_651);
        var along = trace.Where(t => t.Pos.X >= 1_649).ToList();
        Assert.True(along.Count > T, $"{along.Count} ticks along the wall");
        // Along it at 1.5 · cos 45° ≈ 1.06 m/s south, a tick's 8.84 mm
        Assert.InRange(along[^1].Pos.Z - along[0].Pos.Z, (along.Count - 1) * 8.8 - 2, (along.Count - 1) * 8.9 + 2);
        // On into the inside corner with the end wall at z 6: stopped there, 0.35 m off both
        Run(body, ground, new OpenInput(s, s), 5 * T);
        Assert.InRange(body.Pos.X, 1_649, 1_651);
        Assert.InRange(body.Pos.Z, 5_649, 5_651);
        var still = body.Pos;
        Run(body, ground, new OpenInput(s, s), T);
        Assert.Equal(still, body.Pos);
    }

    [Theory]
    [InlineData(0.8, 0.0, true)]
    [InlineData(0.8, 0.04, true)]
    [InlineData(0.6, 0.0, false)]
    [InlineData(0.6, 0.1, false)]
    [InlineData(0.68, 0.0, false)]
    public void ACorridorPassesTheBodyOnlyWhereItIsWiderThanTheBody(double width, double offset, bool passes)
    {
        // Two walls 2 m high along the way south, from z 0 to 5, the gap between them centred on x 0
        double h = width / 2;
        var ground = Ground(Floor, OpenBox.Metres("west wall", -h - 1, -1, 0, -h, 2, 5), OpenBox.Metres("east wall", h, -1, 0, h + 1, 2, 5));
        var body = new OpenBody(OpenPos.FromMetres(offset, 0, -2));
        var trace = Run(body, ground, South, 6 * T);
        if (passes)
        {
            Assert.True(body.Pos.Z > 5500, $"{width} m: held at {body.Pos}");
            // Never in either wall
            Assert.All(trace.Where(s => s.Pos.Z is > 0 and < 5000), s => Assert.InRange(s.Pos.X, -(int)(h * 1000) + 349, (int)(h * 1000) - 349));
        }
        else
        {
            Assert.True(body.Pos.Z < 0, $"{width} m: through to {body.Pos}");
            Assert.All(trace, s => Assert.True(s.Pos.Z < 0, $"{width} m: in at {s.Pos}"));
        }
    }

    [Fact]
    public void ASlopeOfFortyFiveDegreesIsSlidDownAndNeverClimbed()
    {
        // Flat to x 10, up at 45° to 10 m at x 20, flat again
        var chunk = Profile((0, 0), (10, 0), (20, 10), (32, 10));
        var ground = Chunk(chunk);

        // Put down half way up, with the stick at rest: it slides, 2 m/s over the ground, to the foot
        var body = new OpenBody(OpenPos.FromMetres(15, 5, 16));
        var trace = Run(body, ground, OpenInput.None, T);
        Assert.All(trace, s => Assert.True(s.Sliding));
        Assert.InRange(body.Micrometres.X, 13_000_000 - 20_000, 13_000_000 + 20_000);
        Assert.InRange(body.Micrometres.Y, 3_000_000 - 20_000, 3_000_000 + 20_000);
        Assert.All(trace, s => Assert.Equal(OpenStance.Standing, s.Stance));
        Run(body, ground, OpenInput.None, 3 * T);
        Assert.Equal(0, body.Pos.Y);
        Assert.False(body.Sliding);
        Assert.InRange(body.Pos.X, 9_000, 10_000);

        // Walked and run at it from the foot: the stick can't take it up
        foreach (bool run in new[] { false, true })
        {
            var climber = new OpenBody(OpenPos.FromMetres(8, 0, 16));
            trace = Run(climber, ground, East with { Run = run }, 5 * T);
            Assert.True(trace.Max(s => s.Pos.Y) < 60, $"run {run}: up to {trace.Max(s => s.Pos.Y)} mm");
            Assert.InRange(climber.Pos.X, 9_900, 10_100);
            // Slantwise at it, the stick's way along the face is kept: it walks along the foot
            var along = new OpenBody(OpenPos.FromMetres(9.9, 0, 4));
            Run(along, ground, new OpenInput(1, 1, run), 3 * T);
            Assert.True(along.Pos.Z > 6_500, $"run {run}: along the foot to {along.Pos}");
            Assert.True(along.Pos.Y < 60);
        }

        // A slope of 30° is walked up
        var gentle = Chunk(Profile((0, 0), (10, 0), (20, 10 * Math.Tan(Math.PI / 6)), (32, 10 * Math.Tan(Math.PI / 6))));
        var walker = new OpenBody(OpenPos.FromMetres(8, 0, 16));
        Run(walker, gentle, East, 12 * T);
        Assert.Equal(OpenPos.Millimetres(10 * Math.Tan(Math.PI / 6)), walker.Pos.Y);
        Assert.False(walker.Sliding);
    }

    [Fact]
    public void ADropOfMoreThanAMetreEndsInALandingAndALessOneDoesnt()
    {
        foreach (var (drop, falls, hard) in new[] { (3.0, true, true), (1.2, true, true), (0.7, true, false), (0.4, false, false) })
        {
            var ground = Ground(OpenBox.Metres("floor", -40, -1, -40, 40, 0, 40), OpenBox.Metres("platform", -10, 0, -10, 2, drop, 10));
            var body = new OpenBody(OpenPos.FromMetres(0, drop, 0));
            var trace = Run(body, ground, East, 3 * T);
            Assert.Equal(0, body.Pos.Y);
            Assert.Equal(OpenStance.Standing, body.Stance);
            Assert.Equal(falls, trace.Any(s => s.Stance == OpenStance.Falling));
            Assert.Equal(hard, trace.Any(s => s.Hard));
            if (falls) Assert.Equal(OpenPos.Millimetres(drop), trace.Single(s => s.Landed != null).Landed);
            // A drop of 3 m is about 0.78 s of falling: 9.81 m/s² from rest
            if (drop == 3.0) Assert.InRange(trace.Count(s => s.Stance == OpenStance.Falling), (int)(0.75 * T), (int)(0.81 * T));
        }
    }

    [Fact]
    public void NothingGoesThroughAWallAtARunInFramesOfATenthOfASecond()
    {
        // A wall two centimetres thick, run at for three seconds in frames of 0.1 s (12 ticks each), straight and slantwise
        var map = new OpenMap("WallTest", "OpenTest");
        map.Add(Floor);
        map.Add(OpenBox.Metres("thin wall", -10, -1, 3, 10, 3, 3.02));
        foreach (var input in new[] { new OpenInput(0, 1, true), new OpenInput(0.3, 1, true), new OpenInput(-1, 0.2, true) })
        {
            var field = new OpenField(map, new OpenPos(0, 0, 0), 0);
            for (int f = 0; f < 30; f++)
            {
                Assert.Equal(12, field.Advance(0.1, input));
                Assert.True(field.Player.Pos.Z < 3000 - 340, $"{input}: through to {field.Player.Pos}");
            }
        }
    }

    [Fact]
    public void AFaceTurnedDownIsNeverStoodOn()
    {
        // A slab overhead, 2 m to 3 m up: a body put just under it falls to the floor, never stands on its underside
        var ground = Ground(Floor, OpenBox.Metres("slab", -5, 2, -5, 5, 3, 5));
        var body = new OpenBody(OpenPos.FromMetres(0, 1.9, 0));
        var trace = Run(body, ground, OpenInput.None, 2 * T);
        Assert.Equal(0, body.Pos.Y);
        Assert.Contains(trace, s => s.Stance == OpenStance.Falling);
        // A face turned down alone, between a falling body and the floor, is fallen through
        var lone = Chunk(Profile((0, 0), (32, 0)), new[] { OpenBox.Metres("roof", 4, 2, 4, 12, 2.5, 12) });
        var faller = new OpenBody(OpenPos.FromMetres(8, 6, 8));
        Run(faller, lone, OpenInput.None, 2 * T);
        Assert.Equal(2500, faller.Pos.Y);   // the roof's top faces up and is landed on
        var under = new OpenBody(OpenPos.FromMetres(8, 1.5, 8));
        Run(under, lone, OpenInput.None, 2 * T);
        Assert.Equal(0, under.Pos.Y);       // its underside never caught the body put under it
    }

    [Fact]
    public void WaterIsWadedRefusedAtItsEdgeWithoutTheGrantAndSwumWithIt()
    {
        // Flat to x 10, down to −2 m at x 16 (18°), flat to x 24, up to nought at x 30, flat; water's surface at −0.3 over all of it
        var chunk = Profile((0, 0), (10, 0), (16, -2), (24, -2), (30, 0), (32, 0));
        var ground = Chunk(chunk, waters: new[] { OpenWaterBox.Metres("pool", 0, 0, 32, 32, -0.3) });

        // Without the grant: waded in until the water is 1.1 m deep, where the ground is −1.4 m (x 14.2), and no further
        var walker = new OpenBody(OpenPos.FromMetres(4, 0, 16));
        var trace = Run(walker, ground, East, 20 * T);
        Assert.DoesNotContain(trace, s => s.Stance == OpenStance.Swimming);
        Assert.Contains(trace, s => s.Water);
        Assert.InRange(walker.Pos.X, 14_100, 14_200);
        Assert.InRange(walker.Pos.Y, -1_400, -1_360);

        // With it: swum across, feet 1.1 m under the surface, and walked out up the far side
        var swimmer = new OpenBody(OpenPos.FromMetres(4, 0, 16));
        trace = Run(swimmer, ground, East, 18 * T, MovementGrants.Swim);
        Assert.Contains(trace, s => s.Stance == OpenStance.Swimming);
        Assert.All(trace.Where(s => s.Stance == OpenStance.Swimming), s => Assert.Equal(-1_400, s.Pos.Y));
        Assert.Equal(OpenStance.Standing, swimmer.Stance);
        Assert.Equal(0, swimmer.Pos.Y);
        Assert.True(swimmer.Pos.X > 30_500, $"{swimmer.Pos}");

        // Put down in deep water without the grant, a body swims out
        var fallenIn = new OpenBody(OpenPos.FromMetres(20, -1.0, 16));
        trace = Run(fallenIn, ground, East, 7 * T);
        Assert.Equal(OpenStance.Swimming, trace[0].Stance);
        Assert.Equal(OpenStance.Standing, fallenIn.Stance);
        Assert.Equal(0, fallenIn.Pos.Y);
    }

    [Fact]
    public void TheSameFramesGiveTheSameMicrometres()
    {
        // A ground of every kind of contact: a slope, a kerb, walls and a corridor; frames of lengths drawn from a seed,
        // the stick swung round by another; two runs, every tick's micrometres the same
        var boxes = new[]
        {
            OpenBox.Metres("kerb", 2, -1, 2, 6, 0.3, 6), OpenBox.Metres("wall", 3, -1, 10, 9, 2, 11),
            OpenBox.Metres("pillar", 8, -1, 4, 8.6, 3, 4.6), OpenBox.Metres("corridor west", 1, -1, 14, 1.6, 2, 20), OpenBox.Metres("corridor east", 2.4, -1, 14, 3, 2, 20)
        };
        var ground1 = Chunk(Profile((0, 0), (12, 0), (18, 6), (24, 6.5), (32, 6.5)), boxes);
        var ground2 = Chunk(Profile((0, 0), (12, 0), (18, 6), (24, 6.5), (32, 6.5)), boxes);
        List<(long, long, long, int)> Play(OpenGround ground)
        {
            var rng = new Random(77);
            var body = new OpenBody(OpenPos.FromMetres(1, 0, 1));
            var clock = new OpenClock();
            var trace = new List<(long, long, long, int)>();
            for (int f = 0; f < 60 * 40; f++)
            {
                double seconds = 1.0 / (30 + rng.Next(120));
                double angle = rng.NextDouble() * Math.PI * 2;
                var input = new OpenInput(Math.Sin(angle) * rng.NextDouble() * 1.2, Math.Cos(angle), rng.Next(3) == 0);
                int ticks = clock.Advance(seconds);
                for (int i = 0; i < ticks; i++)
                {
                    body.Step(ground, input);
                    var (x, y, z) = body.Micrometres;
                    trace.Add((x, y, z, body.Yaw));
                }
            }
            return trace;
        }
        var first = Play(ground1);
        var second = Play(ground2);
        Assert.True(first.Count > 2000);
        Assert.Equal(first, second);
        // It went somewhere
        Assert.True(first.Select(s => (s.Item1 / 100_000, s.Item3 / 100_000)).Distinct().Count() > 50);
    }

    [Fact]
    public void AMapWithNoGroundIsAPlaceToStand()
    {
        // OW1's empty maps: the player stands where put, turning with the stick but never moving or falling
        var field = new OpenField(new OpenMap("Empty", "OpenTest"), new OpenPos(1, 2, 3), 0);
        field.Advance(1.0, East);
        Assert.Equal(new OpenPos(1, 2, 3), field.Player.Pos);
        Assert.Equal(OpenYaw.East, field.Player.Yaw);
    }
}
