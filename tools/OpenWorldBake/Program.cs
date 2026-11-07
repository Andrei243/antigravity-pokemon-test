// The open world's baker, a first cut (plan 32 · OW0, the prototype gate). Today it knows one world, the gate's
// prototype (a hill with a tunnel through it and a cavern under its top), written as code; OW2 reads a region's
// files from Data/world3d/<region>/ and writes what it bakes under baked/.
//
//   dotnet run --project tools/OpenWorldBake -- prototype                           the bake's counts, the capsule's walk, the camera's arm, the navigation's numbers
//   dotnet run --project tools/OpenWorldBake -- prototype --section <out dir> [x1,z1 x2,z2 ...]   vertical cuts (along the tunnel and across the hill unless given)
//   dotnet run --project tools/OpenWorldBake -- prototype --plan <out dir>          the hill from above with the walk, the paths, the trigger, the zone and the trees
//   dotnet run --project tools/OpenWorldBake -- prototype --check                   bakes twice and compares to the byte; ends with 1 where they differ
//
// It prints counts and positions, never timings.

using System.Globalization;
using OpenWorldBake;

if (args.Length == 0 || args[0] != Prototype.Name)
{
    Console.Error.WriteLine("usage: OpenWorldBake prototype [--section <out dir> [x1,z1 x2,z2 ...]] [--plan <out dir>] [--check]");
    return 2;
}
string? sectionDir = null, planDir = null;
bool check = false;
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
