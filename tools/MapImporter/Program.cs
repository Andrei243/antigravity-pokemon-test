// Reads Sinnoh's map layouts from the Platinum decompilation and writes them out as pictures, as the game's own
// world files and as reports. See tools/MapImporter/README.md.
//
//   dotnet run --project tools/MapImporter                      fetch the pinned decompilation (once) and import everything
//   dotnet run --project tools/MapImporter -- --out <dir>       write somewhere other than tools/MapImporter/out
//   dotnet run --project tools/MapImporter -- --decomp <dir>    read an existing pokeplatinum checkout
//   dotnet run --project tools/MapImporter -- --quick           the mosaics and reports only: no per-chunk or per-area pictures, no world files
//   dotnet run --project tools/MapImporter -- --data            only rewrite the game's own world files (PokemonPlatinumEngine/Data/world/sinnoh)
//   dotnet run --project tools/MapImporter -- --room <key>      print a room of the original as text, to write its map file from
//   dotnet run --project tools/MapImporter -- --game firered|emerald [--quick]   a Game Boy Advance game into out/kanto or out/hoenn (plan 18 · W6)
//   dotnet run --project tools/MapImporter -- --chunk <id>     print which texture lies over each tile of a chunk (the names only), to sort a new area's ground by

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

// A Game Boy Advance game (plan 18 · W6): its own pin, its own folder under out/
if (Arg("--game") is { } game)
    return MapImporter.Gba.GbaImport.Run(repo, game, decompDir, Path.Combine(outDir, MapImporter.Gba.GbaImport.RegionOf(game)), quick);

if (decompDir == null)
{
    Console.WriteLine("Fetching pret/pokeplatinum…");
    decompDir = Sources.Checkout(Path.Combine(repo, "tools", "MapImporter", ".cache"), "pokeplatinum", Sources.DecompRepo, Sources.DecompCommit, DecompMaps.Folders);
}

var decomp = new DecompMaps(decompDir);
var world = new WorldWriter(decomp);
var byBehaviour = new Renders(decomp);
var byCover = new Renders(decomp) { CoverOf = world.CoverOf };

if (Arg("--room") is { } room)
{
    Console.Write(RoomPlan.Describe(decomp, room));
    return 0;
}

if (Arg("--chunk") is { } chunkArg)
{
    Console.Write(ChunkTextures(decomp.Land(int.Parse(chunkArg, CultureInfo.InvariantCulture))));
    return 0;
}

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
// Route 201, Lake Verity, Sandgem Town and Route 202 were replaced in plan 01 · M2 and Jubilife City in M5, so
// there is none today.
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

// The GBA's section (plan 18 · W6) is the --game runs' to write: this run keeps it as it stands
string behavioursDoc = Path.Combine(repo, "docs", "tile-behaviours.md");
string gbaSection = File.Exists(behavioursDoc) ? MapImporter.Gba.GbaImport.SectionOf(File.ReadAllText(behavioursDoc)) : "";
File.WriteAllText(behavioursDoc, gbaSection.Length > 0 ? MapImporter.Gba.GbaImport.Splice(report.TileBehaviours(), gbaSection) : report.TileBehaviours());
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

// The texture named over each tile of a chunk: the ground's, and what stands over it where that differs. A letter to
// a name, capital where the tile is blocked; the names with the kind of ground Cover.cs sorts each into.
static string ChunkTextures(LandData land)
{
    var text = new System.Text.StringBuilder();
    var layers = land.ReadTerrain()?.LayersOver(land.Heights);
    if (layers == null) return "The chunk has no terrain model." + Environment.NewLine;
    const string letters = "abcdefghijklmnopqrstuvwxyz0123456789";
    foreach (var (title, names) in new[] { ("ground", layers.Ground), ("above", layers.Above) })
    {
        var codes = names.Where(n => n != null).Distinct().OrderBy(n => n, StringComparer.Ordinal).Select((n, i) => (Name: n!, Code: letters[i % letters.Length])).ToList();
        text.Append(title).AppendLine(" (a capital letter is a blocked tile, '.' nothing drawn, '#' nothing drawn and blocked)");
        for (int z = 0; z < LandData.Tiles; z++)
        {
            for (int x = 0; x < LandData.Tiles; x++)
            {
                string? name = names[z * LandData.Tiles + x];
                bool solid = land.Solid(x, z);
                char code = name == null ? (solid ? '#' : '.') : codes.First(c => c.Name == name).Code;
                text.Append(name != null && solid ? char.ToUpperInvariant(code) : code);
            }
            text.AppendLine();
        }
        foreach (var (name, code) in codes)
            text.AppendLine($"  {code} {name}: {(Cover.OfTexture(name, out bool known) is { } cover ? cover.ToString() : known ? "(nothing)" : "NOT SORTED")}");
    }
    return text.ToString();
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
