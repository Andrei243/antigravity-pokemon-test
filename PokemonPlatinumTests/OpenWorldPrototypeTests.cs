using OpenWorldBake;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 32 · OW0, the prototype gate: what the first cut of the baker and the prototype's rules must hold, on the
/// gate's hill (a tunnel through it, a cavern under its top). GPU-free: tools/OpenWorldBake, nothing of the game.
/// </summary>
public class OpenWorldPrototypeTests
{
    // The hill's own chunks at the finest level, baked once for the class
    static readonly Lazy<(TerrainWorld World, Bake Bake, CollisionMesh Mesh)> hill = new(() =>
    {
        var world = Prototype.World();
        var bake = Prototype.BakeHill(world);
        return (world, bake, CollisionMesh.Of(bake));
    });

    static readonly Lazy<WalkRecord> walk = new(() => Prototype.Walk(hill.Value.Mesh));

    [Fact]
    public void AChunkBakedTwiceIsTheSameToTheByte()
    {
        // The chunk north-west of the hill's middle: the tunnel, the cavern and the hill's slope are all in it
        var a = SurfaceNets.Mesh(Prototype.World(), 11, 11, 0, Bake.Cells[0], Bake.Skirts[0]);
        var b = SurfaceNets.Mesh(Prototype.World(), 11, 11, 0, Bake.Cells[0], Bake.Skirts[0]);
        Assert.True(a.TriangleCount > 1000);
        Assert.Equal(a.Bytes(), b.Bytes());
    }

    [Fact]
    public void EveryTriangleFacesTheAir()
    {
        // A quad turned by its own shape came out the wrong way about where a gentle slope crosses a level of the grid:
        // back faces in lines along the land's contours, drawn black. Turned by the order of its cells, every face of
        // the ground away from a carve's sharp edges has air just in front of it and rock just behind (along a portal's
        // rim or where a cavern's floor meets its wall, 5 cm can cross a wedge thinner than that)
        var (world, bake, _) = hill.Value;
        bool NearACarve(D3 p) => world.Carves.Any(c => p.X > c.Min.X - 1 && p.X < c.Max.X + 1 && p.Z > c.Min.Z - 1 && p.Z < c.Max.Z + 1 && p.Y < c.Max.Y + 1);
        int faces = 0, wrong = 0;
        foreach (var chunk in bake.Chunks.Values)
            for (int t = 0; t < chunk.GroundTriangles + chunk.ApronTriangles; t++)
            {
                var a = chunk.Vertex(chunk.Indices[t * 3]); var b = chunk.Vertex(chunk.Indices[t * 3 + 1]); var c = chunk.Vertex(chunk.Indices[t * 3 + 2]);
                var middle = (a + b + c) * (1.0 / 3);
                if (NearACarve(middle)) continue;
                var n = D3.Cross(b - a, c - a).Normalized();
                faces++;
                if (world.Field(middle + n * 0.05) <= 0 || world.Field(middle - n * 0.05) >= 0) wrong++;
            }
        Assert.True(faces > 100_000);
        Assert.True(wrong == 0, $"{wrong} of {faces} faces don't face the air");
    }

    [Fact]
    public void TwoChunksOfALevelMeetWithoutASeam()
    {
        // Every vertex on the border column the two chunks share is a vertex of both, to the millimetre
        var world = Prototype.World();
        var west = SurfaceNets.Mesh(world, 11, 12, 0, Bake.Cells[0], Bake.Skirts[0]);
        var east = SurfaceNets.Mesh(world, 12, 12, 0, Bake.Cells[0], Bake.Skirts[0]);
        // The vertices of the surface (the chunk's own ground and its apron), not of the skirts hung under its border
        static IEnumerable<(int, int, int)> Surface(ChunkMesh c) =>
            c.Indices.Take((c.GroundTriangles + c.ApronTriangles) * 3).Distinct().Select(v => (c.Positions[v * 3], c.Positions[v * 3 + 1], c.Positions[v * 3 + 2]));
        var eastVertices = Surface(east).ToHashSet();
        // The border is x = 0; the west chunk's last cells' vertices lie just east of it, in the east chunk's first cells
        var shared = Surface(west).Where(p => p.Item1 > 0 && p.Item1 < 500).ToList();
        Assert.True(shared.Count > 64);
        Assert.All(shared, p => Assert.Contains(p, eastVertices));
        // And the east chunk's apron, a row west of the border, is the west chunk's own last row, to the millimetre
        var westVertices = Surface(west).ToHashSet();
        Assert.All(Surface(east).Where(p => p.Item1 < 0), p => Assert.Contains(p, westVertices));
    }

    [Fact]
    public void TheHillIsHollowUnderItsTopAndWalkableAboveIt()
    {
        var (world, _, mesh) = hill.Value;
        // The field: air in the tunnel and the cavern, rock over them, air over the top
        Assert.True(world.Field(new D3(-15, 1.5, 0)) > 0);
        Assert.True(world.Field(new D3(-15, 4.6, 0)) < 0);
        Assert.True(world.Field(new D3(0, 5, 0)) > 0);
        Assert.True(world.Field(new D3(0, 10.5, 0)) < 0);
        Assert.True(world.Field(new D3(0, 12.5, 0)) > 0);
        // The mesh: two walkable floors over the hill's middle, the cavern's at 0 and the top's at 12
        var floors = mesh.Column(0.37, 0.12).Where(h => mesh.Walkable(h.Triangle)).Select(h => h.Y).ToList();
        Assert.Equal(2, floors.Count);
        Assert.InRange(floors.Min(), -0.01, 0.05);
        Assert.InRange(floors.Max(), 11.8, 12.01);
    }

    [Fact]
    public void TheCapsuleWalksThroughTheTunnelUpTheHillAndOffThePortal()
    {
        var record = walk.Value;
        Assert.True(record.Finished, string.Join("\n", record.Events));
        Assert.InRange(record.HighestY, 11.8, 12.1);
        // Never through the ground: the lowest the feet went is the tunnel's floor
        Assert.InRange(record.LowestY, -0.01, 0.01);
        // Off the west portal's top into the cutting: a drop of more than a metre, with a landing
        Assert.InRange(record.LongestFall, 3.0, 4.0);
        // Pressed into the cavern's wall at a slant: pushed off it, or slid back down its foot
        Assert.True(record.PushTicks + record.SlideTicks > 0);
        // The trigger in the cavern ran, and never for the walk over the top above it
        Assert.True(record.TriggerEntries >= 1);
        Assert.All(record.Samples.Where(s => s.Pos.Y > 6000), s => Assert.False(Prototype.Trigger.Holds(s.Pos.Metres + new D3(0, 0.8, 0))));
        // The zone is the top's: walked into on the flank, never in the cavern under it
        Assert.Contains(record.Events, e => e.Contains("into zone 'hilltop'"));
        Assert.All(record.Samples.Where(s => s.Pos.Y < 1000), s => Assert.False(Prototype.Zone.Holds(s.Pos.Metres)));
    }

    [Fact]
    public void TwoWalksOfTheSameSticksEndOnTheSameMillimetre()
    {
        var world = Prototype.World();
        var other = Prototype.Walk(CollisionMesh.Of(Prototype.BakeHill(world)));
        Assert.Equal(walk.Value.Samples.Count, other.Samples.Count);
        Assert.Equal(walk.Value.PositionHash(), other.PositionHash());
        Assert.Equal(walk.Value.Samples[^1].Pos, other.Samples[^1].Pos);
    }

    [Fact]
    public void ASecondOfWalkingIsAMetreAndAHalfAndOfRunningFourAndAHalf()
    {
        var flat = new CollisionMesh();
        flat.Add(new D3(-50, 0, -50), new D3(-50, 0, 50), new D3(50, 0, 50));
        flat.Add(new D3(-50, 0, -50), new D3(50, 0, 50), new D3(50, 0, -50));
        foreach (var (run, metres) in new[] { (false, 1.5), (true, 4.5) })
        {
            var body = new OpenBody(new OpenPos(0, 0, 0));
            for (int t = 0; t < OpenBody.TicksPerSecond; t++) body.Step(flat, 1, 0, run);
            Assert.Equal((long)(metres * 1e6), body.Micrometres.X);
            Assert.Equal(0, body.Pos.Y);
            Assert.Equal(16384, body.Yaw);
        }
        // A stick 2° off the x axis: 0.44 mm a tick across, which a position kept in whole millimetres would round
        // away every tick (the gate's finding on plan 32 · decision 2); kept in micrometres the body goes slantwise
        var slant = new OpenBody(new OpenPos(0, 0, 0));
        double angle = 2 * Math.PI / 180;
        for (int t = 0; t < OpenBody.TicksPerSecond; t++) slant.Step(flat, Math.Cos(angle), Math.Sin(angle), false);
        Assert.InRange(slant.Pos.Z, 51, 53);
        Assert.InRange(slant.Pos.X, 1498, 1500);
    }

    [Fact]
    public void TheCameraArmIsHeldInsideTheTunnelAndNeverEndsInRock()
    {
        var (world, _, mesh) = hill.Value;
        // Looking east down the tunnel from 10 m in, pitched up: the vault holds the arm in
        var camera = new OrbitCamera { Yaw = Math.PI / 2, Pitch = 35 * Math.PI / 180, Distance = 6 };
        var placed = camera.Place(mesh, new D3(-14, 0, 0));
        Assert.True(placed.Held);
        Assert.True(placed.Arm < 6);
        Assert.True(world.Field(placed.Position) > 0);
        // In the open it reaches its whole length
        var open = camera.Place(mesh, new D3(-50, 0, 20));
        Assert.False(open.Held);
        Assert.Equal(6, open.Arm, 6);
    }

    [Fact]
    public void TheNavigationIsLayeredAndHasAWayThroughAndAWayOver()
    {
        var (world, _, mesh) = hill.Value;
        var nav = new NavProto(mesh);
        Assert.True(nav.LayeredColumns(-30, -30, 30, 30) > 200);
        var west = Prototype.OnGround(mesh, new D3(-50, 0, 0));
        var east = Prototype.OnGround(mesh, new D3(50, 0, 0));
        var through = nav.Path(west, east);
        var over = nav.Path(west, east, p => p.Y < world.Height(p.X, p.Z) - 0.8);
        Assert.NotNull(through);
        Assert.NotNull(over);
        Assert.True(through!.Value.Path.Max(p => p.Y) < 0.5);
        Assert.True(over!.Value.Path.Max(p => p.Y) > 3);
        Assert.True(over.Value.Length > through.Value.Length);
        // The cavern's floor and the top over it are different floors: the way between leaves the hill and climbs it
        var up = nav.Path(new D3(0, 0.2, 0), Prototype.OnGround(mesh, new D3(0, 0, 0)));
        Assert.NotNull(up);
        Assert.True(up!.Value.Length > 40);
    }
}
