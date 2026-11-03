using MapDraw;
using PokemonPlatinumEngine.Data;

// dotnet run --project tools/MapDraw -- <region folder> [--check]
//   Reads tools/MapDraw/<region>/layout.json and its drawings and writes the region's chunks, overworld matrix and habitats
//   into PokemonPlatinumEngine/Data/world/<region>/ (the area files, overlays and world.json beside them are written by hand).
//   --check writes nothing and ends with 1 where the files are not what the drawings give.
if (args.Length < 1 || args[0].StartsWith("-", StringComparison.Ordinal) && args[0] != "--rooms")
{
    Console.WriteLine("dotnet run --project tools/MapDraw -- <region folder, e.g. kalos> [--check]");
    return 2;
}
// --rooms <map name>...: writes each room of Data/maps as the game writes a map (MapFile.FromMap), which is what the
// content check holds a room to, so a room laid out by hand or by a script is put in the game's own shape
if (args[0] == "--rooms")
{
    string maps = Path.Combine(Repository.Root(), "PokemonPlatinumEngine", "Data", "maps");
    foreach (string name in args.Skip(1))
    {
        string path = Path.Combine(maps, name + ".json");
        var map = GameDataFiles.Deserialize<MapFile>(File.ReadAllText(path)).ToMap();
        File.WriteAllText(path, GameDataFiles.Serialize(MapFile.FromMap(map)));
        Console.WriteLine($"wrote {name}");
    }
    return 0;
}
string region = args[0];
bool check = args.Contains("--check");
string root = Repository.Root();
string source = Path.Combine(root, "tools", "MapDraw", region);
// --out <world folder>: the files written elsewhere than the game's own Data (the sample pack's region, plan 16 · T14:
// `-- tidemark --out samples/packs/tidemark-isle/world/tidemark`), a path from the repository's root
int outAt = Array.IndexOf(args, "--out");
string target = outAt >= 0 && outAt + 1 < args.Length
    ? Path.GetFullPath(Path.Combine(root, args[outAt + 1]))
    : Path.Combine(root, "PokemonPlatinumEngine", "Data", "world", region);
var files = Repository.Files(source, target);
int differs = 0;
foreach (var (path, text) in files)
{
    bool same = File.Exists(path) && File.ReadAllText(path).Replace("\r\n", "\n") == text;
    if (same) continue;
    differs++;
    if (check) Console.WriteLine($"differs: {Path.GetRelativePath(root, path)}");
    else
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, text);
        Console.WriteLine($"wrote {Path.GetRelativePath(root, path)}");
    }
}
// A chunk the drawings no longer make is taken away
foreach (string stale in Repository.Stale(target, files))
{
    differs++;
    if (check) Console.WriteLine($"stale: {Path.GetRelativePath(root, stale)}");
    else { File.Delete(stale); Console.WriteLine($"deleted {Path.GetRelativePath(root, stale)}"); }
}
Console.WriteLine(differs == 0 ? "The world files are what the drawings give." : check ? $"{differs} files differ." : $"{differs} files written.");
return check && differs > 0 ? 1 : 0;

namespace MapDraw
{
    /// <summary>Where the files are, and what the drawings make of them (also what the tests compare).</summary>
    public static class Repository
    {
        public static string Root()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null && !File.Exists(Path.Combine(dir.FullName, "PokemonPlatinum.sln"))) dir = dir.Parent;
            return dir?.FullName ?? throw new DirectoryNotFoundException("The repository's root (PokemonPlatinum.sln) was not found.");
        }

        /// <summary>Every file the drawings in <paramref name="source"/> make in the world folder <paramref name="target"/>, with its text.</summary>
        public static List<(string Path, string Text)> Files(string source, string target)
        {
            var world = Drawing.Build(source);
            var list = new List<(string, string)>();
            foreach (var chunk in world.Chunks)
                list.Add((Path.Combine(target, "chunks", $"{chunk.Id:000}.json"), GameDataFiles.Serialize(chunk)));
            list.Add((Path.Combine(target, "matrices", "000.json"), GameDataFiles.Serialize(world.Matrix)));
            string areas = Path.Combine(target, "areas");
            var areaFiles = Directory.Exists(areas)
                ? Directory.GetFiles(areas, "*.json").Select(f => GameDataFiles.Deserialize<WorldAreaFile>(File.ReadAllText(f))).ToList()
                : new List<WorldAreaFile>();
            list.Add((Path.Combine(target, WorldHabitatsFile.FileName), GameDataFiles.Serialize(Drawing.Habitats(world, areaFiles))));
            return list;
        }

        /// <summary>Chunk files in the world folder that no drawing makes any more.</summary>
        public static IEnumerable<string> Stale(string target, IEnumerable<(string Path, string Text)> files)
        {
            string chunks = Path.Combine(target, "chunks");
            if (!Directory.Exists(chunks)) yield break;
            var made = new HashSet<string>(files.Select(f => Path.GetFullPath(f.Path)), StringComparer.OrdinalIgnoreCase);
            foreach (string f in Directory.GetFiles(chunks, "*.json"))
                if (!made.Contains(Path.GetFullPath(f))) yield return f;
        }
    }
}
