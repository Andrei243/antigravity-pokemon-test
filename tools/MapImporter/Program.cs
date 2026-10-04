// Reads Sinnoh's map layouts from the Platinum decompilation and writes them out as pictures, as the game's own
// world files and as reports. See tools/MapImporter/README.md.
//
//   dotnet run --project tools/MapImporter                      fetch the pinned decompilation (once) and import everything
//   dotnet run --project tools/MapImporter -- --out <dir>       write somewhere other than tools/MapImporter/out
//   dotnet run --project tools/MapImporter -- --decomp <dir>    read an existing pokeplatinum checkout
//   dotnet run --project tools/MapImporter -- --quick           the mosaics and reports only: no per-chunk or per-area pictures, no world files
//   dotnet run --project tools/MapImporter -- --data            only rewrite the game's own world files (PokemonPlatinumEngine/Data/world/sinnoh)

using System.Diagnostics;
using System.Globalization;
using DataImporter;
using MapImporter;
using PokemonPlatinumEngine.Data;

var clock = Stopwatch.StartNew();
string repo = FindRepo();
string? decompDir = Arg("--decomp");
string outDir = Arg("--out") ?? Path.Combine(repo, "tools", "MapImporter", "out");
bool quick = args.Contains("--quick");

if (decompDir == null)
{
    Console.WriteLine("Fetching pret/pokeplatinum…");
    decompDir = Sources.Checkout(Path.Combine(repo, "tools", "MapImporter", ".cache"), "pokeplatinum", Sources.DecompRepo, Sources.DecompCommit, DecompMaps.Folders);
}

var decomp = new DecompMaps(decompDir);
var world = new WorldWriter(decomp);
var byBehaviour = new Renders(decomp);
var byCover = new Renders(decomp) { CoverOf = world.CoverOf };

if (args.Contains("--data"))
{
    // The game's data folder in the source tree: what is written there is checked in
    string data = Arg("--data-dir") ?? Path.Combine(repo, "PokemonPlatinumEngine", "Data", World.Folder, "sinnoh");
    int written = world.WriteGameData(data, byCover.Name);
    foreach (string problem in world.Problems) Console.WriteLine("  " + problem);
    Console.WriteLine($"Wrote {written} world files to {data} ({clock.Elapsed.TotalSeconds:F1} s)");
    return world.Problems.Count == 0 ? 0 : 1;
}

MapDatabase.Initialize();   // the game's maps as they are built today, to compare with
var report = new Report(decomp, world, byCover);
Console.WriteLine($"Read {decomp.Headers.Count} areas, {decomp.MatrixCount} matrices and {decomp.LandCount} chunks ({clock.Elapsed.TotalSeconds:F1} s)");

// ---------------------------------------------------------------- pictures

var overworld = decomp.Matrix(0);
byCover.World(overworld, new Renders.Options { Scale = 4 }).Save(Path.Combine(outDir, "sinnoh.png"));
byBehaviour.World(overworld, new Renders.Options { Scale = 4 }).Save(Path.Combine(outDir, "sinnoh_behaviours.png"));
byCover.Heights(overworld, 2).Save(Path.Combine(outDir, "sinnoh_heights.png"));

// The hand-made maps the import hasn't replaced yet, each beside the area it stands in for. Twinleaf Town,
// Route 201, Lake Verity, Sandgem Town and Route 202 were replaced in plan 01 · M2.
var comparisons = new (string Imported, string HandMade)[] { ("jubilife_city", "JubilifeCity") }
    .Where(c => MapDatabase.MapNames.Contains(c.HandMade))
    .ToArray();
foreach (var (imported, handMade) in comparisons)
{
    var header = decomp.Headers.Values.First(h => h.Key == imported);
    var matrix = decomp.Matrix(header.Matrix);
    var map = MapDatabase.Get(handMade);
    int scale = matrix.Headers == null && matrix.Width > 2 ? 6 : 10;
    Renders.Board(
        ("IMPORTED: WHAT EACH TILE DOES", byBehaviour.Area(matrix, header, scale, out _)),
        ("IMPORTED: WHAT EACH TILE LOOKS LIKE", byCover.Area(matrix, header, scale, out _)),
        ("HAND-MADE, IN THE GAME TODAY", Renders.HandMade(map, scale))
    ).Save(Path.Combine(outDir, "compare", imported + ".png"));
}

int pictures = 3 + comparisons.Length;
if (!quick)
{
    for (int id = 0; id < decomp.LandCount; id++)
    {
        var setting = Renders.SettingOf(report.AreasOf(id).FirstOrDefault());
        byBehaviour.Chunk(id, 8, setting).Save(Path.Combine(outDir, "chunks", id.ToString("000", CultureInfo.InvariantCulture) + ".png"));
        pictures++;
    }
    foreach (var header in decomp.Headers.Values.OrderBy(h => h.Index))
    {
        var matrix = decomp.Matrix(header.Matrix);
        if (matrix.Headers != null && !matrix.Headers.Contains(header.Id)) continue;   // an area without ground of its own
        if (matrix.Land.All(l => l == Matrix.NoLand)) continue;
        byCover.Area(matrix, header, 8, out _).Save(Path.Combine(outDir, "areas", header.Key + ".png"));
        pictures++;
    }
}
Console.WriteLine($"Drew {pictures} pictures ({clock.Elapsed.TotalSeconds:F1} s)");

// ---------------------------------------------------------------- world files and reports

if (!quick)
{
    int files = world.WriteAll(Path.Combine(outDir, "world"), byCover.Name);
    Console.WriteLine($"Wrote {files} world files ({clock.Elapsed.TotalSeconds:F1} s)");
}

File.WriteAllText(Path.Combine(repo, "docs", "tile-behaviours.md"), report.TileBehaviours());
File.WriteAllText(Path.Combine(repo, "docs", "world-models.md"), report.WorldModelsDoc());
Directory.CreateDirectory(outDir);
File.WriteAllText(Path.Combine(outDir, "report.md"), report.Run(comparisons));
foreach (string problem in report.Problems.Take(20)) Console.WriteLine("  " + problem);
Console.WriteLine($"{report.Problems.Count} problems; report in {Path.Combine(outDir, "report.md")}");
Console.WriteLine($"Done in {clock.Elapsed.TotalSeconds:F1} s");
return 0;

string? Arg(string name)
{
    int i = Array.IndexOf(args, name);
    return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
}

static string FindRepo()
{
    foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
    {
        for (var dir = new DirectoryInfo(start); dir != null; dir = dir.Parent)
            if (File.Exists(Path.Combine(dir.FullName, "PokemonPlatinum.sln"))) return dir.FullName;
    }
    throw new DirectoryNotFoundException("Run the importer from inside the repository");
}
