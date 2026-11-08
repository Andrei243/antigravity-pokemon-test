using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using OpenWorldBake;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Graphics.OpenWorld;
using PokemonPlatinumEngine.OpenWorld;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using Xunit;
using OrbitCamera = PokemonPlatinumEngine.OpenWorld.OrbitCamera;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 32 · OW8, the open world's renderer I, its rules without a window: the orbit camera (behind the player, the
/// shoulders' turn, the ease behind a walking player, the spring arm against the collision mesh, a script's look and
/// its way home), whether rock is over it, the rings of levels, the shadow maps' fitting, a chunk's vertices with their
/// covers and its water with its depth, the covers' atlas, the weather in the air, what a camera sees, the plain beyond
/// the region and the light. On boxes and on the open test world's own level 0. GPU-free.
/// </summary>
public class OpenRendererTests
{
    static readonly Lazy<(RegionDescription Description, TerrainWorld Land)> lab = new(() =>
    {
        var description = RegionDescription.Read(OpenWorldBakeTests.LabFolder);
        return (description, description.Land());
    });

    static readonly ConcurrentDictionary<(int, int), BakedChunk?> chunks = new();

    /// <summary>The lab's ground as the game collides with it, its chunks' level 0 baked once for the class.</summary>
    static OpenGround LabGround() => CapsuleRoute.GroundOf(lab.Value.Description, lab.Value.Land, chunks);

    /// <summary>A chunk's level 0 as the game loads it.</summary>
    static BakedChunk Finest(int x, int z) =>
        chunks.GetOrAdd((x, z), _ => RegionBake.Finest(lab.Value.Land, lab.Value.Description.World, x, z))!;

    const double Degree = Math.PI / 180;

    // ------------------------------------------------------------------ the orbit camera

    [Fact]
    public void TheCameraBeginsBehindThePlayerAndLooksTheirWay()
    {
        var camera = new OrbitCamera();
        camera.Reset(OpenYaw.North);
        var feet = new OpenVec(10, 2, 20);
        var (eye, target) = camera.Place(null, feet, 0);
        // Looking north, it stands south of the player, over them, 5.5 m back along the arm, pitched 16° down
        Assert.Equal(feet.X, eye.X, 6);
        Assert.True(eye.Z > feet.Z + 5);
        Assert.Equal(feet.Y + OrbitCamera.Above, target.Y, 6);
        Assert.Equal(OrbitCamera.Back, (eye - target).Length, 6);
        Assert.Equal(OrbitCamera.Back * Math.Sin(OrbitCamera.PitchDegrees * Degree), eye.Y - target.Y, 6);
        Assert.False(camera.Held);
    }

    [Fact]
    public void TheShouldersTurnItAHundredDegreesASecond()
    {
        var camera = new OrbitCamera();
        camera.Reset(OpenYaw.South);
        // The right shoulder turns the view to the right: looking south, toward the west, the yaw going down
        camera.Turn(1, 0.5);
        Assert.Equal(-50 * Degree, OrbitCamera.Angle(camera.Yaw), 6);
        camera.Turn(-1, 1.0);
        Assert.Equal(50 * Degree, OrbitCamera.Angle(camera.Yaw), 6);
    }

    [Fact]
    public void ItEasesBehindAWalkingPlayerOnceItHasntBeenTurnedForTwoSeconds()
    {
        var camera = new OrbitCamera();
        camera.Reset(OpenYaw.South);
        // Walking east: half of the way round in 0.9 s
        camera.Advance(0.9, OpenYaw.East, 1.5);
        Assert.Equal(45 * Degree, OrbitCamera.Angle(camera.Yaw), 4);
        // Standing still: nothing
        camera.Advance(1.0, OpenYaw.East, 0);
        Assert.Equal(45 * Degree, OrbitCamera.Angle(camera.Yaw), 4);
        // Turned by hand: two seconds of nothing, whatever the walk
        camera.Turn(1, 0.1);
        double turned = camera.Yaw;
        camera.Advance(1.9, OpenYaw.East, 1.5);
        Assert.Equal(turned, camera.Yaw, 9);
        camera.Advance(0.5, OpenYaw.East, 1.5);
        Assert.True(OrbitCamera.Angle(camera.Yaw) > OrbitCamera.Angle(turned));
    }

    [Fact]
    public void APlayerWalkingTowardTheCameraIsntFollowedRound()
    {
        var camera = new OrbitCamera();
        camera.Reset(OpenYaw.South);
        // Looking south, the player walks north (straight at the camera), and then 40° off it: neither turns it
        camera.Advance(2.0, OpenYaw.North, 1.5);
        Assert.Equal(0, OrbitCamera.Angle(camera.Yaw), 9);
        camera.Advance(2.0, OpenYaw.North + OpenYaw.Turn * 40 / 360, 1.5);
        Assert.Equal(0, OrbitCamera.Angle(camera.Yaw), 9);
        // 50° off it is followed
        camera.Advance(2.0, OpenYaw.North + OpenYaw.Turn * 50 / 360, 1.5);
        Assert.NotEqual(0, OrbitCamera.Angle(camera.Yaw), 3);
    }

    [Fact]
    public void ItsArmIsHeldInAtOnceByAWallAndLetOutAgainAtThreeMetresASecond()
    {
        // A wall 2 m behind a player facing north (the camera south of them)
        var wall = new OpenGround(null, new[] { OpenBox.Metres("wall", -10, -1, 2, 10, 10, 2.5) });
        var camera = new OrbitCamera();
        camera.Reset(OpenYaw.North);
        var feet = new OpenVec(0, 0, 0);
        camera.Place(wall, feet, 1 / 60.0);
        Assert.True(camera.Held);
        double toWall = 2 / Math.Cos(OrbitCamera.PitchDegrees * Degree);
        Assert.Equal(toWall - OrbitCamera.Margin, camera.Arm, 3);
        // Never under 0.6 m, however near the wall
        var near = new OpenGround(null, new[] { OpenBox.Metres("wall", -10, -1, 0.5, 10, 10, 1) });
        camera.Place(near, feet, 1 / 60.0);
        Assert.Equal(OrbitCamera.Shortest, camera.Arm, 9);
        // The wall gone: let out at 3 m a second, to its whole length
        camera.Place(null, feet, 0.5);
        Assert.Equal(OrbitCamera.Shortest + 1.5, camera.Arm, 6);
        camera.Place(null, feet, 10);
        Assert.Equal(OrbitCamera.Back, camera.Arm, 9);
    }

    [Fact]
    public void ThroughTheTunnelAndTheCavernTheArmNeverEndsInRock()
    {
        var ground = LabGround();
        RouteStop[] tunnel =
        {
            new(-50, 8, "the west road's bend"), new(-36, 0, "the tunnel's west mouth"), new(-24, 0, "in the tunnel"),
            new(-8, 0, "the cavern's west"), new(0, 0, "the cavern's middle"), new(9, 1.5, "the cavern's east"),
            new(24, 0, "the tunnel east"), new(36, 0, "the tunnel's east mouth"), new(46, -4, "the east road")
        };
        var start = OpenPos.FromMetres(-58, ground.Footing(-58, 22, 60, -60)!.Value.Y, 22);
        var walk = CapsuleRoute.Walk(ground, start, tunnel);
        Assert.True(walk.Finished && walk.GivenUp.Count == 0, string.Join("\n", walk.Events));
        const int every = 6;
        double seconds = every / (double)OpenClock.TicksPerSecond;
        var camera = new OrbitCamera();
        var first = OpenVec.Of(start);
        int yaw = OpenYaw.Toward(tunnel[0].X - first.X, tunnel[0].Z - first.Z);
        camera.Reset(yaw);
        int under = 0, shortest = 0, places = 0;
        for (int i = every; i < walk.Samples.Count; i += every)
        {
            var feet = OpenVec.Of(walk.Samples[i].Pos);
            var was = OpenVec.Of(walk.Samples[i - every].Pos);
            double dx = feet.X - was.X, dz = feet.Z - was.Z, moved = Math.Sqrt(dx * dx + dz * dz);
            if (moved > 1e-3) yaw = OpenYaw.Toward(dx, dz);
            camera.Advance(seconds, yaw, moved / seconds);
            var (eye, _) = camera.Place(ground, feet, seconds);
            var head = feet + new OpenVec(0, OrbitCamera.Above, 0);
            places++;
            // Nothing between the head and the camera: the arm's whole span is clear, but where a wall nearer than the
            // shortest arm holds it at that
            if (camera.Arm <= OrbitCamera.Shortest + 1e-9) shortest++;
            else Assert.Null(ground.Raycast(head, (eye - head).Normalized(), (eye - head).Length));
            if (OpenUnderRock.Overhead(ground, eye)) under++;
        }
        Assert.True(under > 50, $"the camera was under rock at {under} of {places} places of the walk");
        Assert.True(shortest < places / 20, $"the arm was at its shortest at {shortest} of {places} places");

        // Behind a walker the tunnel (4.4 m wide, its vault 3.4 m up) leaves the arm whole; turned to look across it
        // under the hill (west of x = -20 it is a cutting open to the sky), the tunnel's wall holds the arm in short of
        // itself, and the camera is still in the tunnel
        var inside = new OpenVec(-17, ground.Footing(-17, 0, 3, -3)!.Value.Y, 0);
        camera.Reset(OpenYaw.North);
        var (across, _) = camera.Place(ground, inside, 0);
        Assert.True(camera.Held);
        Assert.InRange(camera.Arm, OrbitCamera.Shortest, 2.6);
        var from = inside + new OpenVec(0, OrbitCamera.Above, 0);
        Assert.Null(ground.Raycast(from, (across - from).Normalized(), (across - from).Length));
        Assert.True(OpenUnderRock.Overhead(ground, across));
    }

    [Fact]
    public void AScriptsLookSendsThePointToTheMarkerAndHomeBringsItBack()
    {
        var camera = new OrbitCamera();
        camera.Reset(OpenYaw.South);
        var feet = new OpenVec(0, 0, 0);
        var marker = new OpenVec(20, 0, 0);
        camera.LookAt(marker, feet, 1.0);
        camera.Advance(0.5, OpenYaw.South, 0);
        var (_, half) = camera.Place(null, feet, 0);
        Assert.Equal(0.5, camera.LookShare, 9);
        // Half way by smoothstep: the point half way from the head to a metre over the marker
        Assert.Equal(10, half.X, 6);
        camera.Advance(0.6, OpenYaw.South, 0);
        var (eye, there) = camera.Place(null, feet, 0);
        Assert.Equal(new OpenVec(20, OrbitCamera.LookAbove, 0), there);
        // Turned to look east, from behind the player (west of them)
        Assert.Equal(Math.PI / 2, OrbitCamera.Angle(camera.Yaw), 6);
        Assert.True(eye.X < feet.X - 5);
        // Home: the point comes back over 0.8 s, the camera keeps its way
        camera.Home();
        camera.Advance(0.8, OpenYaw.South, 0);
        var (_, home) = camera.Place(null, feet, 0);
        Assert.Null(camera.Looking);
        Assert.Equal(feet + new OpenVec(0, OrbitCamera.Above, 0), home);
        Assert.Equal(Math.PI / 2, OrbitCamera.Angle(camera.Yaw), 6);
        // In a hurry: there at once
        camera.LookAt(marker, feet, 0);
        Assert.Equal(1, camera.LookShare);
    }

    [Fact]
    public void AScriptsZoomNarrowsTheLensAndAZoomOfOneIsTheLensItself()
    {
        Assert.Equal(OrbitCamera.LensDegrees, OrbitCamera.LensFor(1));
        // Twice as large: half the lens's tangent
        Assert.Equal(Math.Tan(25 * Degree) / 2, Math.Tan(OrbitCamera.LensFor(2) * Degree / 2), 9);
        Assert.True(OrbitCamera.LensFor(0.5) > OrbitCamera.LensDegrees);
    }

    // ------------------------------------------------------------------ rock overhead

    [Fact]
    public void TheCavesLightEasesInUnderRockAndOutInTheOpen()
    {
        var ground = LabGround();
        var rock = new OpenUnderRock();
        var cavern = new OpenVec(0, 2, 0);
        var meadow = new OpenVec(-58, 3, 22);
        Assert.True(OpenUnderRock.Overhead(ground, cavern));
        Assert.False(OpenUnderRock.Overhead(ground, meadow));
        rock.Advance(ground, cavern, 0.4);
        Assert.True(rock.Under);
        Assert.Equal(0.5, rock.Share, 9);
        rock.Advance(ground, cavern, 1);
        Assert.Equal(1, rock.Share);
        rock.Advance(ground, meadow, 0.2);
        Assert.Equal(0.75, rock.Share, 9);
        rock.Settle(ground, meadow);
        Assert.Equal(0, rock.Share);
        rock.Settle(ground, cavern);
        Assert.Equal(1, rock.Share);
    }

    // ------------------------------------------------------------------ the rings

    [Fact]
    public void TheBlockOfThreeByThreeIsFinestThenTheSecondLevelTo300MetresAndTheThirdBeyond()
    {
        var rings = new OpenRings();
        var feet = new OpenVec(10, 0, 10);
        rings.Move(feet);
        Assert.Equal((0, 0), rings.Focus);
        Assert.Equal(0, rings.LevelOf(1, 1, feet, 3));
        Assert.Equal(0, rings.LevelOf(-1, -1, feet, 3));
        Assert.Equal(1, rings.LevelOf(2, 0, feet, 3));
        Assert.Equal(1, rings.LevelOf(9, 0, feet, 3));        // its nearest point 278 m off
        Assert.Equal(2, rings.LevelOf(10, 0, feet, 3));       // 310 m
        // A chunk keeps the level it has within 10 m either side of the ring
        var on = new OpenVec(11, 0, 10);
        Assert.Equal(2, rings.LevelOf(10, 0, on, 3));                 // 309 m
        Assert.Equal(1, rings.LevelOf(10, 0, on, 3, current: 1));
        var back = new OpenVec(-5, 0, 10);
        Assert.Equal(1, rings.LevelOf(9, 0, back, 3));                // 293 m
        Assert.Equal(2, rings.LevelOf(9, 0, back, 3, current: 2));
        // Fewer levels: the last there is
        Assert.Equal(1, rings.LevelOf(20, 0, feet, 2));
    }

    [Fact]
    public void TheFocusMovesOnOnlyFourMetresPastItsChunksBorder()
    {
        var rings = new OpenRings();
        rings.Move(new OpenVec(30, 0, 10));
        Assert.Equal((0, 0), rings.Focus);
        rings.Move(new OpenVec(33, 0, 10));
        Assert.Equal((0, 0), rings.Focus);
        rings.Move(new OpenVec(36.5, 0, 10));
        Assert.Equal((1, 0), rings.Focus);
        // and back: only 4 m past the other way
        rings.Move(new OpenVec(30, 0, 10));
        Assert.Equal((1, 0), rings.Focus);
        rings.Move(new OpenVec(27.5, 0, 10));
        Assert.Equal((0, 0), rings.Focus);
    }

    // ------------------------------------------------------------------ the shadows

    [Fact]
    public void EachShadowMapHoldsItsSliceOfTheViewAndTheFirstIsAbout35MetresAcross()
    {
        var eye = new Vector3(3, 4, 5);
        var forward = Vector3.Normalize(new Vector3(0.3f, -0.28f, 1f));
        float near = 0.3f;
        var right = Vector3.Normalize(Vector3.Cross(forward, Vector3.UnitY));
        var up = Vector3.Cross(right, forward);
        float tan = MathF.Tan(25f * MathF.PI / 180f), aspect = 16f / 9f;
        for (int i = 0; i < OpenCascades.Count; i++)
        {
            float far = OpenCascades.Ends[i];
            var (centre, radius) = OpenCascades.Slice(eye, forward, 50f, aspect, near, far);
            // The slice's eight corners are in the sphere
            foreach (float d in new[] { near, far })
                foreach (int sx in new[] { -1, 1 })
                    foreach (int sy in new[] { -1, 1 })
                    {
                        var corner = eye + forward * d + right * (sx * d * tan * aspect) + up * (sy * d * tan);
                        Assert.True(Vector3.Distance(corner, centre) <= radius * 1.0001f, $"map {i}: a corner {Vector3.Distance(corner, centre)} from the middle, radius {radius}");
                    }
            if (i == 0) Assert.InRange(radius * 2, 30f, 40f);
            near = far * (1f - OpenCascades.Blend);
        }
    }

    [Fact]
    public void AShadowMapsCameraIsSnappedToItsOwnTexels()
    {
        var sun = Vector3.Normalize(new Vector3(-0.5f, 0.9f, 0.5f));
        var (a, near, far) = OpenCascades.Fit(new Vector3(10, 0, 10), 20f, sun, 4096);
        var (b, _, _) = OpenCascades.Fit(new Vector3(10.001f, 0, 10.001f), 20f, sun, 4096);
        Assert.Equal(40f, a.FovY);
        // A millimetre's move leaves it where it was or moves it a whole texel (40 m / 4,096) along the map's sides
        float texel = 40f / 4096f;
        var forward = -sun;
        var right = Vector3.Normalize(Vector3.Cross(forward, Vector3.UnitY));
        var mapUp = Vector3.Cross(right, forward);
        var d = b.Target - a.Target;
        float r = Vector3.Dot(d, right) / texel, u = Vector3.Dot(d, mapUp) / texel;
        Assert.Equal(MathF.Round(r), r, 2);
        Assert.Equal(MathF.Round(u), u, 2);
        // Its box reaches 200 m toward the sun past the sphere
        Assert.Equal(1.0, near);
        Assert.Equal(20 + OpenCascades.Behind + 20 + 1, far, 3);
    }

    // ------------------------------------------------------------------ a chunk's vertices

    [Fact]
    public void AChunksGroundIsThreeVerticesATriangleEachWithItsCornersCovers()
    {
        var chunk = Finest(-1, -1);
        var (surface, skirts) = OpenTerrainMesh.Of(chunk);
        Assert.Equal((chunk.GroundTriangles + chunk.ApronTriangles) * 3, surface.Count);
        Assert.NotNull(skirts);
        Assert.Equal(chunk.SkirtTriangles * 3, skirts!.Count);
        for (int t = 0; t < chunk.GroundTriangles + chunk.ApronTriangles; t += 37)
        {
            int ia = chunk.Indices[t * 3], ib = chunk.Indices[t * 3 + 1], ic = chunk.Indices[t * 3 + 2];
            var uv = surface.Uvs[t * 3];
            Assert.Equal(uv, surface.Uvs[t * 3 + 1]);
            Assert.Equal(uv, surface.Uvs[t * 3 + 2]);
            Assert.Equal((OpenCoverArt.SlotOf(chunk.Covers[ia]), OpenCoverArt.SlotOf(chunk.Covers[ib]), OpenCoverArt.SlotOf(chunk.Covers[ic])), OpenTerrainMesh.SlotsOf(uv));
            Assert.Equal(OpenTerrainMesh.Corners, new[] { surface.Colours[t * 3], surface.Colours[t * 3 + 1], surface.Colours[t * 3 + 2] });
            Assert.Equal(chunk.Positions[ia * 3] / 1000f, surface.Positions[t * 3].X, 4);
            Assert.Equal(chunk.Positions[ib * 3 + 1] / 1000f, surface.Positions[t * 3 + 1].Y, 4);
        }
        // The box round them is the chunk's own (and its apron's): x from -32 to 0
        Assert.InRange(surface.Min.X, -33.5f, -31.5f);
        Assert.InRange(surface.Max.X, -0.5f, 1.5f);
    }

    [Fact]
    public void EveryCoverHasItsOwnCellAndEveryPackRoundTrips()
    {
        var slots = OpenCoverArt.Order.Select(OpenCoverArt.SlotOf).ToList();
        Assert.Equal(slots.Count, slots.Distinct().Count());
        Assert.Equal(Enum.GetValues<OpenCover>().Length, OpenCoverArt.Order.Length);
        Assert.True(OpenCoverArt.Order.Length <= OpenCoverArt.Slots);
        foreach (var a in Enum.GetValues<OpenCover>())
            foreach (var b in Enum.GetValues<OpenCover>())
            {
                var uv = OpenTerrainMesh.Covers(a, b, OpenCover.Rock);
                Assert.Equal((OpenCoverArt.SlotOf(a), OpenCoverArt.SlotOf(b), OpenCoverArt.SlotOf(OpenCover.Rock)), OpenTerrainMesh.SlotsOf(uv));
            }
    }

    [Fact]
    public void TheWaterLiesOverTheRiverWithItsDepthAndNowhereOnTheDryMeadow()
    {
        var river = Finest(0, 1);
        var water = OpenTerrainMesh.WaterOf(river);
        Assert.NotNull(water);
        Assert.Equal(0, water!.Count % 3);
        var depths = water.Uvs.Take(water.Count).Select(uv => uv.X).ToList();
        // Every triangle faces up
        for (int t = 0; t < water.Count; t += 3)
            Assert.True(Vector3.Cross(water.Positions[t + 1] - water.Positions[t], water.Positions[t + 2] - water.Positions[t]).Y > 0);
        // Deep in the river's middle, dry (less than nought) up its banks and shallow between: the dry bank is never
        // taken for deep water, or the deep band would come right up to the shore
        Assert.True(depths.Max() > 0.8f, $"the deepest {depths.Max()}");
        Assert.True(depths.Max() < OpenTerrainMesh.NoGroundDepth, $"the deepest {depths.Max()}");
        Assert.Contains(depths, d => d < 0);
        Assert.Contains(depths, d => d > 0 && d < 0.3f);
        // The surface stands at the river's height there (its points from -0.7 to -0.95 m)
        Assert.All(water.Positions.Take(water.Count), p => Assert.InRange(p.Y, -1.0f, -0.6f));
        // A chunk of dry meadow has none
        Assert.Null(OpenTerrainMesh.WaterOf(Finest(-2, -1)));
    }

    [Fact]
    public void ThePondsStackHasNoWaterInIt()
    {
        // The stack stands from the pond's bed to 3 m over it at (43, -40), 1.4 m round: under its top no water is drawn
        var pond = Finest(1, -2);
        var water = OpenTerrainMesh.WaterOf(pond)!;
        int inside = 0;
        for (int t = 0; t < water.Count; t += 3)
        {
            var middle = (water.Positions[t] + water.Positions[t + 1] + water.Positions[t + 2]) / 3f;
            float depth = (water.Uvs[t].X + water.Uvs[t + 1].X + water.Uvs[t + 2].X) / 3f;
            if (Vector2.Distance(new Vector2(middle.X, middle.Z), new Vector2(43, -40)) >= 0.9f) continue;
            inside++;
            Assert.True(depth < 0, $"water {depth} m deep inside the stack at {middle}");
        }
        Assert.True(inside > 0);
    }

    [Fact]
    public void TheWaterOfAFarLevelIsLaidAtTwoMetres()
    {
        var levels = RegionBake.Chunk(lab.Value.Land, lab.Value.Description.World, 1, -2);
        var fine = OpenTerrainMesh.WaterOf(levels[0])!;
        var far = OpenTerrainMesh.WaterOf(levels[2])!;
        // The pond, 16 quads to a 2 m cell at level 0 and one at level 2
        Assert.Equal(fine.Count, far.Count * 16);
    }

    [Fact]
    public void AFarLevelsWaterIsNeverDeepAtItsWestAndNorthEdges()
    {
        // A thinned level has no apron: its west edge lies on its neighbour's ground, which once read as 4 m of water, a
        // band of the deep across the river at every border between chunks
        foreach (int x in new[] { 0, 1 })
        {
            var levels = RegionBake.Chunk(lab.Value.Land, lab.Value.Description.World, x, 1);
            for (int level = 0; level < levels.Length; level++)
            {
                var water = OpenTerrainMesh.WaterOf(levels[level])!;
                float west = x * 32f;
                var edge = Enumerable.Range(0, water.Count).Where(v => MathF.Abs(water.Positions[v].X - west) < 1e-3f).Select(v => water.Uvs[v].X).ToList();
                Assert.NotEmpty(edge);
                Assert.True(edge.Max() < 2.5f, $"chunk {x},1 level {level}: {edge.Max()} m deep at its west edge");
            }
        }
    }

    // ------------------------------------------------------------------ the covers' atlas

    [Fact]
    public void EachCoversTileComesRoundItsEdgesAndItsPadIsItsOwn()
    {
        var atlas = OpenCoverArt.Paint();
        Assert.Equal((OpenCoverArt.Cell, OpenCoverArt.Cell * OpenCoverArt.Slots), (atlas.Width, atlas.Height));
        for (int s = 0; s < OpenCoverArt.Order.Length; s++)
        {
            var cover = OpenCoverArt.Order[s];
            var tile = OpenCoverArt.PaintTile(cover);
            int top = s * OpenCoverArt.Cell;
            for (int y = 0; y < OpenCoverArt.Cell; y++)
                for (int x = 0; x < OpenCoverArt.Cell; x++)
                {
                    int tx = ((x - OpenCoverArt.Pad) % OpenCoverArt.Tile + OpenCoverArt.Tile) % OpenCoverArt.Tile;
                    int ty = ((y - OpenCoverArt.Pad) % OpenCoverArt.Tile + OpenCoverArt.Tile) % OpenCoverArt.Tile;
                    Assert.Equal(tile.Get(tx, ty), atlas.Get(x, top + y));
                }
            // Mostly its base, with patches one step lighter
            var (baseColour, patch, _, _) = OpenCoverArt.ColoursOf(cover);
            int ofBase = 0, ofPatch = 0;
            for (int y = 0; y < OpenCoverArt.Tile; y++)
                for (int x = 0; x < OpenCoverArt.Tile; x++)
                {
                    var c = tile.Get(x, y);
                    if (c.Equals(baseColour)) ofBase++;
                    else if (c.Equals(patch)) ofPatch++;
                }
            Assert.True(ofBase + ofPatch > OpenCoverArt.Tile * OpenCoverArt.Tile * 3 / 4, $"{cover}: {ofBase} of the base and {ofPatch} of its patches");
            Assert.True(ofBase > 0 && ofPatch > 0, $"{cover} has no patches");
        }
    }

    [Fact]
    public void TheCoversColoursAreTheStyleGuides()
    {
        var guide = System.IO.File.ReadAllText(System.IO.Path.Combine(Repository.Root, "docs", "art", "style-guide.md"));
        foreach (var cover in OpenCoverArt.Order)
        {
            var (baseColour, patch, _, _) = OpenCoverArt.ColoursOf(cover);
            string row = $"`{baseColour.R},{baseColour.G},{baseColour.B}` / `{patch.R},{patch.G},{patch.B}`";
            Assert.True(guide.Contains(row), $"{cover}: the style guide's covers table has no {row}");
        }
    }

    // ------------------------------------------------------------------ the weather in the air

    [Fact]
    public void RainIsPiecesInABoxRoundTheCameraTheSameForTheSameMoment()
    {
        var pieces = new List<WeatherPiece>();
        var camera = new Vector3(100, 5, -40);
        OpenWeatherFx.Pieces(FieldWeather.Rain, camera, 12.5, 1f, pieces);
        Assert.InRange(pieces.Count, OpenWeatherFx.CountOf(FieldWeather.Rain) * 9 / 10, OpenWeatherFx.CountOf(FieldWeather.Rain));
        Assert.All(pieces, p =>
        {
            Assert.InRange(p.Head.X, camera.X - OpenWeatherFx.Across / 2, camera.X + OpenWeatherFx.Across / 2);
            Assert.InRange(p.Head.Y, camera.Y - OpenWeatherFx.Tall / 2, camera.Y + OpenWeatherFx.Tall / 2);
            Assert.InRange(p.Head.Z, camera.Z - OpenWeatherFx.Across / 2, camera.Z + OpenWeatherFx.Across / 2);
            Assert.True(Vector3.Distance(p.Head, camera) >= OpenWeatherFx.Nearest);
            // Falling: its tail over its head
            Assert.True(p.Tail.Y > p.Head.Y);
        });
        var again = new List<WeatherPiece>();
        OpenWeatherFx.Pieces(FieldWeather.Rain, camera, 12.5, 1f, again);
        Assert.Equal(pieces, again);
        // A moment on, they have moved; the cave's whole share leaves none, half of it about half
        OpenWeatherFx.Pieces(FieldWeather.Rain, camera, 12.6, 1f, again);
        Assert.NotEqual(pieces[0].Head, again[0].Head);
        OpenWeatherFx.Pieces(FieldWeather.Rain, camera, 12.5, 0f, again);
        Assert.Empty(again);
        OpenWeatherFx.Pieces(FieldWeather.Rain, camera, 12.5, 0.5f, again);
        Assert.InRange(again.Count, pieces.Count / 2 - 60, pieces.Count / 2 + 60);
        OpenWeatherFx.Pieces(FieldWeather.Clear, camera, 12.5, 1f, again);
        Assert.Empty(again);
        // Under a roof at the camera's height over the box's west half nothing falls: a quarter of the pieces are gone
        OpenWeatherFx.Pieces(FieldWeather.Rain, camera, 12.5, 1f, again, (x, z) => x < camera.X ? camera.Y : float.MinValue);
        Assert.All(again, p => Assert.True(p.Head.X >= camera.X || p.Head.Y >= camera.Y));
        Assert.InRange(again.Count, pieces.Count * 3 / 4 - 80, pieces.Count * 3 / 4 + 80);
    }

    [Fact]
    public void APieceMovesInTheWorldNotWithTheCamera()
    {
        // The camera moved a metre east: a piece still in the box stands where it stood
        var a = new List<WeatherPiece>();
        var b = new List<WeatherPiece>();
        OpenWeatherFx.Pieces(FieldWeather.Snow, new Vector3(0, 0, 0), 3, 1f, a);
        OpenWeatherFx.Pieces(FieldWeather.Snow, new Vector3(1, 0, 0), 3, 1f, b);
        int stayed = a.Count(p => b.Any(q => Vector3.DistanceSquared(p.Head, q.Head) < 1e-6f));
        Assert.True(stayed > a.Count * 9 / 10, $"{stayed} of {a.Count} stood still");
    }

    // ------------------------------------------------------------------ what a camera sees

    [Fact]
    public void ACameraSeesWhatIsBeforeItAndNotWhatIsBehindOrBesideIt()
    {
        var view = new OpenView(new Vector3(0, 2, 0), new Vector3(0, 1, 10), 50f, 16f / 9f, 0.3f, 1400f);
        Assert.True(view.Sees(new Vector3(-1, 0, 20), new Vector3(1, 1, 22)));
        Assert.False(view.Sees(new Vector3(-1, 0, -22), new Vector3(1, 1, -20)));
        Assert.False(view.Sees(new Vector3(80, 0, 20), new Vector3(82, 1, 22)));
        Assert.False(view.Sees(new Vector3(-1, 0, 1500), new Vector3(1, 1, 1502)));
        // A chunk the eye stands in is seen
        Assert.True(view.Sees(new Vector3(-16, -5, -16), new Vector3(16, 5, 16)));
    }

    // ------------------------------------------------------------------ the plain beyond the region

    [Fact]
    public void ThePlainMeetsTheRegionsBorderAtItsOwnHeightsAndFacesUp()
    {
        // A region of four chunks by four whose ground rises 1 m every 32 m east, with a channel at its east edge
        var plain = OpenRenderer.Horizon((-2, -2, 1, 1), (x, z) => x / 32f + (x > 60 && MathF.Abs(z) < 4 ? -2f : 0f));
        var points = plain.Positions.Take(plain.Count).ToList();
        Assert.Equal(0, plain.Count % 3);
        for (int t = 0; t < plain.Count; t += 3)
            Assert.True(Vector3.Cross(points[t + 1] - points[t], points[t + 2] - points[t]).Y > 0);
        // A metre inside the west border at the border's own height (the lower of 0.3 m and 1 m in) less a hair
        float west = -63.7f / 32f - OpenRenderer.HorizonDrop;
        Assert.Contains(points, p => MathF.Abs(p.X - (-63f)) < 0.01f && MathF.Abs(p.Y - west) < 0.001f);
        // In the channel at the east border, the channel's; beside it, the bank's
        Assert.Contains(points, p => p.X > 60 && MathF.Abs(p.Z) < 3 && p.Y < 0f);
        Assert.Contains(points, p => p.X > 60 && MathF.Abs(p.Z) > 10 && p.X < 70 && p.Y > 1.8f);
        // And out to the horizon
        Assert.Contains(points, p => p.X > 1900);
        Assert.Contains(points, p => p.Z < -1900);
    }

    [Fact]
    public void TheSceneIs1920AcrossOnEveryPreset()
    {
        Assert.Equal(0.5f, OpenRenderer.StepFor(3840));
        Assert.Equal(1920f / 2880f, OpenRenderer.StepFor(2880), 6);
        Assert.Equal(1f, OpenRenderer.StepFor(1920));
    }

    // ------------------------------------------------------------------ the light

    [Fact]
    public void TheOpenLightIsTheBattlesOfTheHourWithItsFogDrawnOut()
    {
        var day = OpenLook.Open(13.5f);
        var battle = ArtLook.BattleRig(13.5f);
        Assert.Equal(battle.Light, day.Light);
        Assert.Equal(battle.Sky, day.Sky);
        Assert.Equal((OpenLook.FogNear, OpenLook.FogFar, OpenLook.FogAmount), (day.FogNear, day.FogFar, day.FogAmount));
        Assert.Equal(0f, day.Post.TiltShift);
        Assert.Equal(0f, day.Post.Dof);
        Assert.True(OpenLook.Open(22f).Sky.Stars > 0);
        Assert.Equal(day, OpenLook.Weathered(day, FieldWeather.Clear));
        // Rain dims the sun and greys the sky
        var rain = OpenLook.Weathered(day, FieldWeather.Rain);
        Assert.True(rain.Light.SunColor.X < day.Light.SunColor.X);
        Assert.NotEqual(day.Sky.Zenith, rain.Sky.Zenith);
        // Fog hides the sky in its own colour, and the outlines of what it hides
        var fog = OpenLook.Weathered(day, FieldWeather.Fog);
        Assert.True(fog.Post.OutlineStrength < day.Post.OutlineStrength * 0.5f);
    }

    [Fact]
    public void UnderRockTheCavesAirComesInAndTheSunStays()
    {
        var day = OpenLook.Open(13.5f);
        Assert.Equal(day, OpenLook.UnderRock(day, 0f));
        var cave = OpenLook.UnderRock(day, 1f);
        Assert.Equal(day.Light.SunDirection, cave.Light.SunDirection);
        Assert.Equal(day.Light.SunColor, cave.Light.SunColor);
        Assert.True(Vector3.Distance(OpenLook.CaveSky, cave.Light.SkyAmbient) < 1e-5f);
        Assert.Equal(ArtLook.CaveRig.FogFar, cave.FogFar, 3);
        Assert.Equal(0f, OpenLook.LampStrength(0f));
        Assert.Equal(1f, OpenLook.LampStrength(1f));
        Assert.Equal(0.5f, OpenLook.LampStrength(0.5f), 6);
    }

    // ------------------------------------------------------------------ the player between ticks

    [Fact]
    public void ThePlayerRemembersWhereTheFeetWereATickBeforeTheLast()
    {
        var map = new OpenMap("RendererTestFlat", "test");
        map.Add(OpenBox.Metres("floor", -50, -1, -50, 50, 0, 50));
        var player = new OpenPlayer(new OpenPos(0, 0, 0), OpenYaw.East);
        Assert.Equal(player.Pos, player.Before);
        player.Step(map, new OpenInput(1, 0), MovementGrants.None);
        var first = player.Pos;
        player.Step(map, new OpenInput(1, 0), MovementGrants.None);
        Assert.Equal(first, player.Before);
        Assert.True(player.Pos.X > first.X);
        player.Place(new OpenPos(5000, 0, 0), 0);
        Assert.Equal(player.Pos, player.Before);
    }
}

/// <summary>
/// Plan 32 · OW8 in the whole game with no window: the orbit camera begun behind the player as they are put in an open
/// map, turned by the shoulders while the field is theirs, sent by a script's <c>camera look</c> and brought home once
/// the script has ended. It seeds the dice, so it runs with the recordings, alone.
/// </summary>
[Collection("Recordings")]
public class OpenCameraGameTests
{
    private const float Frame = 1f / 60f;

    private static void Run(GameEngine engine, int frames, ActionFrame? frame = null)
    {
        for (int f = 0; f < frames; f++)
        {
            InputManager.Feed(frame ?? ActionFrame.None);
            engine.Update(Frame);
        }
        InputManager.Feed(null);
    }

    [Fact]
    public void TheCameraBeginsBehindTurnsByTheShouldersAndFollowsAScriptsLook()
    {
        Headless.Play(engine =>
        {
            Headless.PastTheOpening(engine);
            var game = engine.Drive;
            game.UseScripts(ScriptLibrary.FromSources(("lab_meadow", """
                script Look
                  camera look tunnel_west 1.0
                  wait 1.5
                  end
                script LookAndBack
                  camera look tunnel_west 0.5
                  camera release 0.5
                  wait 2.0
                  end
                """), ("common", """
                script Nothing
                  end
                """)));
            var map = OpenWorldPlacesTests.LabWithPlaces();
            game.AddOpenMap(map);
            game.PlaceOpen(map, "west_road");
            var placed = game.OpenPlayer!.Pos;
            // Behind the player, who faces east
            Assert.Equal(Math.PI / 2, OrbitCamera.Angle(game.OpenCamera.Yaw), 6);
            // The right shoulder held half a second: 50° to the right (toward the south)
            Run(engine, 30, new ActionFrame(1u << (int)GameAction.PageRight, 0));
            Assert.Equal(40 * Math.PI / 180, OrbitCamera.Angle(game.OpenCamera.Yaw), 2);
            // A script's look: the camera's point goes to the tunnel's west mouth while the script runs
            Assert.True(engine.StartScript("Look", file: "lab_meadow"));
            Run(engine, 70);
            Assert.True(engine.ScriptRunning);
            Assert.NotNull(game.OpenCamera.Looking);
            Assert.Equal(1, game.OpenCamera.LookShare, 6);
            Assert.Equal("tunnel_west", game.OpenLook);
            // and comes home once it has ended
            for (int f = 0; f < 600 && engine.ScriptRunning; f++) Run(engine, 1);
            Assert.False(engine.ScriptRunning);
            Run(engine, 60);
            Assert.Null(game.OpenCamera.Looking);
            // `camera release` brings it home while the script still runs
            Assert.True(engine.StartScript("LookAndBack", file: "lab_meadow"));
            Run(engine, 40);
            Assert.True(engine.ScriptRunning);
            Assert.NotNull(game.OpenCamera.Looking);
            Run(engine, 60);
            Assert.True(engine.ScriptRunning);
            Assert.Null(game.OpenCamera.Looking);
            for (int f = 0; f < 600 && engine.ScriptRunning; f++) Run(engine, 1);
            // The player wasn't moved by any of it
            Assert.Equal((placed.X, placed.Z), (game.OpenPlayer!.Pos.X, game.OpenPlayer.Pos.Z));
        });
    }
}
