// The open world's baker (plan 32 · OW2). A region's descriptions (Data/world3d/<region>/, or the baker's own test
// worlds in tools/OpenWorldBake/worlds/<region>/) are read strictly and baked into what the game loads, under the
// folder's baked/: every chunk at three levels, the finest by surface nets at half a metre and the others by thinning
// it, with its cover, normals, apron, skirts and water, and an index.
//
//   dotnet run --project tools/OpenWorldBake -- <region>                            bakes and writes; prints each level's counts
//   dotnet run --project tools/OpenWorldBake -- <region> --check                    writes nothing; ends with 1 where the baked files differ from the descriptions
//   dotnet run --project tools/OpenWorldBake -- <region> --chunk x,z                that chunk alone (with --check, checks it alone)
//   dotnet run --project tools/OpenWorldBake -- <region> --plan <out dir> [area]    the area (or the region) from above, and the same with levels 1 and 2's triangles
//   dotnet run --project tools/OpenWorldBake -- <region> --section <out dir> [area] [x1,z1 x2,z2 ...]   vertical cuts (across the area's middle unless given)
//   dotnet run --project tools/OpenWorldBake -- <region> --walk <out dir> [--swim] x,z x,z[!] ...   the game's capsule (plan 32 · OW3) walked from the first point through the rest (! runs), what happened printed, drawn over the plan and as a profile
//   dotnet run --project tools/OpenWorldBake -- <region> --seams                    how far each level's border stands off its neighbours' levels (nought, by design)
//   dotnet run --project tools/OpenWorldBake -- <region> --world <folder> --out <baked folder>   descriptions and bake elsewhere
//
// OW0's prototype gate (the hill as code, its capsule, camera and navigation) is still here for the harness's openproto:
//
//   dotnet run --project tools/OpenWorldBake -- prototype                           the bake's counts, the capsule's walk, the camera's arm, the navigation's numbers
//   dotnet run --project tools/OpenWorldBake -- prototype --section <out dir> [x1,z1 x2,z2 ...]   vertical cuts (along the tunnel and across the hill unless given)
//   dotnet run --project tools/OpenWorldBake -- prototype --plan <out dir>          the hill from above with the walk, the paths, the trigger, the zone and the trees
//   dotnet run --project tools/OpenWorldBake -- prototype --check                   bakes twice and compares to the byte; ends with 1 where they differ
//
// It prints counts and positions, never timings.

using System.Globalization;
using OpenWorldBake;

if (args.Length == 0)
{
    Console.Error.WriteLine("usage: OpenWorldBake <region> [--check] [--chunk x,z] [--plan <out dir> [area]] [--section <out dir> [area] [x1,z1 x2,z2 ...]] [--seams] [--walk <out dir> [--swim] x,z x,z ...] [--world <folder>] [--out <folder>]");
    Console.Error.WriteLine("       OpenWorldBake prototype [--section <out dir> [x1,z1 x2,z2 ...]] [--plan <out dir>] [--check] [--seams]");
    return 2;
}
if (args[0] != Prototype.Name) return RegionCommand.Run(args);
string? sectionDir = null, planDir = null;
bool check = false, seams = false;
var cuts = new List<((double, double) A, (double, double) B)>();
for (int i = 1; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--section":
            sectionDir = args[++i];
            while (i + 1 < args.Length && !args[i + 1].StartsWith("--"))
            {
                var a = Point(args[++i]);
                if (i + 1 >= args.Length || args[i + 1].StartsWith("--")) { Console.Error.WriteLine("--section: a cut needs two points"); return 2; }
                cuts.Add((a, Point(args[++i])));
            }
            break;
        case "--plan": planDir = args[++i]; break;
        case "--check": check = true; break;
        case "--seams": seams = true; break;
        default: Console.Error.WriteLine($"unknown argument {args[i]}"); return 2;
    }
}

static (double, double) Point(string text)
{
    var parts = text.Split(',');
    return (double.Parse(parts[0], CultureInfo.InvariantCulture), double.Parse(parts[1], CultureInfo.InvariantCulture));
}

var world = Prototype.World();
if (check)
{
    var first = Prototype.BakeAll(world);
    var second = Prototype.BakeAll(Prototype.World());
    int differ = 0;
    foreach (var (key, chunk) in first.Chunks)
        if (!second.Chunks.TryGetValue(key, out var other) || !chunk.Bytes().AsSpan().SequenceEqual(other.Bytes()))
        {
            differ++;
            Console.WriteLine($"chunk {key.X},{key.Z} level {key.Lod} differs between two bakes");
        }
    Console.WriteLine($"{first.Chunks.Count} chunks baked twice: {(differ == 0 ? "the same to the byte" : differ + " differ")}");
    return differ == 0 ? 0 : 1;
}

var bake = Prototype.BakeAll(world);
if (seams)
{
    // Where two levels meet: how far a chunk's border stands over its neighbour's surface at another level (what
    // its skirt shows there), the worst of each pair of levels
    var worst = new Dictionary<(int, int), (double Gap, string Where)>();
    var surfaces = new Dictionary<(int, int, int), CollisionMesh>();
    CollisionMesh SurfaceOf((int, int, int) key)
    {
        if (surfaces.TryGetValue(key, out var m)) return m;
        var c = bake.Chunks[key];
        m = new CollisionMesh();
        for (int t = 0; t < c.GroundTriangles + c.ApronTriangles; t++)
            m.Add(c.Vertex(c.Indices[t * 3]), c.Vertex(c.Indices[t * 3 + 1]), c.Vertex(c.Indices[t * 3 + 2]));
        return surfaces[key] = m;
    }
    foreach (var (key, chunk) in bake.Chunks)
    {
        var top = Enumerable.Range(0, chunk.GroundTriangles + chunk.ApronTriangles).SelectMany(t => new[] { chunk.Indices[t * 3], chunk.Indices[t * 3 + 1], chunk.Indices[t * 3 + 2] }).ToHashSet();
        var hung = Enumerable.Range(chunk.GroundTriangles + chunk.ApronTriangles, chunk.TriangleCount - chunk.GroundTriangles - chunk.ApronTriangles)
            .SelectMany(t => new[] { chunk.Indices[t * 3], chunk.Indices[t * 3 + 1], chunk.Indices[t * 3 + 2] }).Where(top.Contains).Distinct();
        foreach (int v in hung)
        {
            var p = chunk.Vertex(v);
            var (nx, nz) = world.ChunkOf(p.X, p.Z);
            if (nx == key.X && nz == key.Z) continue;
            foreach (var other in bake.Chunks.Keys.Where(k => k.X == nx && k.Z == nz && k.Lod != key.Lod))
            {
                if (SurfaceOf(other).Column(p.X, p.Z).FirstOrDefault() is not { Triangle: >= 0 } hit || hit.Y > p.Y + 5) continue;
                double gap = p.Y - hit.Y;
                // The hill's own chunks (a cave's shell) apart from the open ground's, which have no carve
                var pair = (key.Lod * 10 + other.Lod, Math.Sqrt(p.X * p.X + p.Z * p.Z) < 42 ? 1 : 0);
                if (!worst.TryGetValue(pair, out var w) || gap > w.Gap) worst[pair] = (gap, $"chunk {key.X},{key.Z} over {nx},{nz} at {p}");
            }
        }
    }
    foreach (var ((levels, hill), (gap, where)) in worst.OrderBy(w => w.Key))
        Console.WriteLine($"a level-{levels / 10} border over level {levels % 10}{(hill == 1 ? " on the hill" : " on open ground")}: at most {gap:0.000} m ({where})");
    return 0;
}
for (int lod = 0; lod < Bake.Cells.Length; lod++)
{
    var chunks = bake.Chunks.Where(c => c.Key.Lod == lod).Select(c => c.Value).ToList();
    Console.WriteLine($"level {lod} ({Bake.Cells[lod]} m cells): {chunks.Count} chunks, {chunks.Sum(c => c.VertexCount)} vertices, "
        + $"{chunks.Sum(c => c.GroundTriangles)} ground triangles, {chunks.Sum(c => c.ApronTriangles)} in aprons and {chunks.Sum(c => c.TriangleCount - c.GroundTriangles - c.ApronTriangles)} in skirts; "
        + $"the busiest chunk {chunks.Max(c => c.TriangleCount)} triangles");
}
var mesh = CollisionMesh.Of(bake);
Console.WriteLine($"collision: {mesh.Triangles.Count} triangles of the finest level, {Enumerable.Range(0, mesh.Triangles.Count).Count(mesh.Walkable)} of them walkable (under 40°)");

var walk = Prototype.Walk(mesh);
foreach (string line in walk.Events) Console.WriteLine("  " + line);
var end = walk.Samples[^1];
Console.WriteLine($"walk: {(walk.Finished ? "finished" : "!! not finished")} at tick {end.Tick} ({end.Tick / (double)OpenBody.TicksPerSecond:0.00} s), {end.Walked:0.0} m over the ground, ends {end.Pos}");
Console.WriteLine($"  highest {walk.HighestY:0.000} m, lowest {walk.LowestY:0.000} m, longest fall {walk.LongestFall:0.000} m, {walk.PushTicks} ticks pushed off walls (at most {walk.MostPushes} pushes in one), "
    + $"{walk.SlideTicks} sliding down a steep face, the trigger ran {walk.TriggerEntries} time(s)");

// The camera's arm along the walk: how often a wall held it in, and its shortest
var camera = new OrbitCamera();
int held = 0;
double shortest = camera.Distance;
foreach (var sample in walk.Samples.Where((_, i) => i % 30 == 0))
{
    camera.Yaw = sample.Yaw * Math.PI * 2 / 65536;
    var placed = camera.Place(mesh, sample.Pos.Metres);
    if (placed.Held) held++;
    shortest = Math.Min(shortest, placed.Arm);
}
Console.WriteLine($"orbit camera behind the walker: held in by a wall at {held} of {walk.Samples.Count / 30 + 1} places, its arm at least {shortest:0.00} m");

var nav = new NavProto(mesh);
Console.WriteLine($"navigation: {nav.Walkable.Count} walkable triangles with a capsule's clearance ({nav.Unclear} without), {nav.Components} separate pieces (the largest {nav.Largest}), "
    + $"{nav.LayeredColumns(-30, -30, 30, 30)} columns of the hill's square metres with a floor over a floor");
var west = Prototype.OnGround(mesh, new D3(-50, 0, 0));
var east = Prototype.OnGround(mesh, new D3(50, 0, 0));
var through = nav.Path(west, east);
var over = nav.Path(west, east, p => p.Y < world.Height(p.X, p.Z) - 0.8);
var up = nav.Path(new D3(0, 0.2, 0), Prototype.OnGround(mesh, new D3(0, 0, 0)));
Console.WriteLine($"  west meadow to east meadow: {Describe(through)}; with the tunnel barred: {Describe(over)}");
Console.WriteLine($"  the cavern's floor to the top over it: {Describe(up)}");

static string Describe((List<D3> Path, double Length, int Visited)? path) =>
    path is { } p ? $"{p.Length:0.0} m over {p.Path.Count} triangles ({p.Visited} visited), highest {p.Path.Max(q => q.Y):0.0} m" : "!! no path";

if (sectionDir != null)
{
    if (cuts.Count == 0) { cuts.Add(((-60, 0), (60, 0))); cuts.Add(((0, -40), (0, 40))); }
    foreach (var (a, b) in cuts)
    {
        string name = $"section_{a.Item1:0}_{a.Item2:0}_{b.Item1:0}_{b.Item2:0}".Replace('-', 'm');
        string path = Path.Combine(sectionDir, name + ".png");
        Pictures.Save(Pictures.Section(world, a, b, -6, 24, walk, Prototype.Trigger, Prototype.Zone), path);
        Console.WriteLine($"wrote {path}");
    }
    Console.WriteLine("  key: rock grey, topsoil green, air pale with a line every 5 m; the walk's feet and heads blue under rock and orange in the open; the trigger red; the zone's floor and ceiling yellow");
}
if (planDir != null)
{
    string path = Path.Combine(planDir, "plan_hill.png");
    var paths = new List<(List<D3>, Raylib_cs.Color)>();
    if (through is { } t) paths.Add((t.Path, new Raylib_cs.Color(40, 220, 230, 255)));
    if (over is { } o) paths.Add((o.Path, new Raylib_cs.Color(230, 60, 200, 255)));
    Pictures.Save(Pictures.Plan(world, -70, -50, 70, 50, mesh, walk, Prototype.Trees(world), paths), path);
    Console.WriteLine($"wrote {path}");
    Console.WriteLine("  key: cover shaded by slope, violet where a floor lies under the top, trees dark green, the walk white, the path through the tunnel cyan, with the tunnel barred magenta, the trigger red, the zone yellow, the person blue, the Pokémon orange");
}
return 0;
