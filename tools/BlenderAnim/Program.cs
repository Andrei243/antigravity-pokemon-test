// The game's battle clips keyed on a rigged .glb with Blender, run with no window, and contact sheets of a file's clips.
//   dotnet run --project tools/BlenderAnim -- inspect <file.glb> [--map role=bone[,bone]]
//   dotnet run --project tools/BlenderAnim -- animate <in.glb> [--out <file.glb>] [--only idle,physical,...] [--map role=bone[,bone]]
//   dotnet run --project tools/BlenderAnim -- render <file.glb> [--out <dir>] [--frames <n>] [--size <px>] [--only <clip>,...]
// Every command takes --blender <exe>; else BLENDER, the PATH or Program Files is searched.
// animate writes <in>.battle.glb unless told otherwise, never over its input. Clips of the file that would play in
// place of one it writes are dropped (it says which); the others are kept. It ends by reading the file as the game
// does and printing which clip plays for each role, and ends with 1 if a role written isn't its new clip.
using System.Text.Json;
using PokemonPlatinumEngine.Graphics;

namespace BlenderAnim;

// A class with a Main, not top-level statements: those would make a global Program, which the test project
// (which sees this tool's internals) would confuse with the game's
internal static class Cli
{
    public static int Main(string[] args)
    {
        if (args.Length < 2 || args[0] is "-h" or "--help")
        {
            Console.WriteLine("usage: BlenderAnim inspect <file.glb> [--map role=bone] | animate <in.glb> [--out file.glb] [--only roles] [--map role=bone] | render <file.glb> [--out dir] [--frames n] [--size px] [--only clips]   (--blender exe)");
            Console.WriteLine($"roles: {string.Join(", ", BattleClips.All.Select(c => $"{c.Role} ({c.Clip})"))}; bones by role: root, spine, neck, head, front, back, tail, wings");
            return args.Length == 0 ? 2 : 0;
        }
        string verb = args[0], input = Path.GetFullPath(args[1]);
        var rest = args.Skip(2).ToList();
        string? blenderGiven = Take(rest, "--blender"), outPath = Take(rest, "--out");
        var only = (Take(rest, "--only") ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        int frames = int.TryParse(Take(rest, "--frames"), out int f) ? f : 6, size = int.TryParse(Take(rest, "--size"), out int s) ? s : 400;
        var map = new Dictionary<string, string[]>();
        while (Take(rest, "--map") is { } m)
        {
            int eq = m.IndexOf('=');
            if (eq <= 0) { Console.Error.WriteLine($"--map {m}: write role=bone"); return 2; }
            map[m[..eq]] = m[(eq + 1)..].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }
        if (rest.Count > 0) { Console.Error.WriteLine($"unknown argument {rest[0]}"); return 2; }
        if (!File.Exists(input)) { Console.Error.WriteLine($"no file at {input}"); return 2; }
        if (Blender.Find(blenderGiven) is not { } blender) { Console.Error.WriteLine(Blender.Hint); return 2; }

        try
        {
            return verb switch
            {
                "inspect" => Inspect(blender, input, map),
                "animate" => Animate(blender, input, outPath, only, map),
                "render" => Render(blender, input, outPath, frames, size, only),
                _ => Unknown(verb)
            };
        }
        catch (InvalidOperationException e)
        {
            Console.Error.WriteLine(e.Message);
            return 1;
        }
    }

    static int Unknown(string verb)
    {
        Console.Error.WriteLine($"unknown command {verb}: inspect, animate or render");
        return 2;
    }

    static int Inspect(string blender, string input, Dictionary<string, string[]> map)
    {
        var r = Blender.Run(blender, "inspect_rig.py", new() { ["input"] = input, ["map"] = map });
        var size = r.GetProperty("size");
        Console.WriteLine($"{Path.GetFileName(input)}: {r.GetProperty("bones").GetArrayLength()} bones, {size[0]:0.###} x {size[1]:0.###} x {size[2]:0.###} (x, length, height)");
        PrintRoles(r.GetProperty("roles"));
        Console.WriteLine("clips:");
        foreach (var a in r.GetProperty("actions").EnumerateArray())
            Console.WriteLine($"  {a.GetProperty("name").GetString()} (frames {a.GetProperty("frames")[0]:0.#} to {a.GetProperty("frames")[1]:0.#})");
        PrintGame(Clips(input));
        return 0;
    }

    static int Animate(string blender, string input, string? outPath, List<string> only, Dictionary<string, string[]> map)
    {
        var (clips, error) = BattleClips.Chosen(only);
        if (error != null) { Console.Error.WriteLine(error); return 2; }
        string output = Path.GetFullPath(outPath ?? Path.Combine(Path.GetDirectoryName(input)!, Path.GetFileNameWithoutExtension(input).Replace(".animated", "") + ".battle.glb"));
        if (string.Equals(output, input, StringComparison.OrdinalIgnoreCase)) { Console.Error.WriteLine("--out is the input: write somewhere else"); return 2; }
        var drop = BattleClips.ToDrop(Clips(input), clips);
        var r = Blender.Run(blender, "animate.py", new() { ["input"] = input, ["output"] = output, ["clips"] = clips, ["drop"] = drop, ["map"] = map });
        PrintRoles(r.GetProperty("roles"));
        Console.WriteLine($"wrote {output}");
        Console.WriteLine($"  keyed: {List(r.GetProperty("written"))}");
        if (r.GetProperty("dropped").GetArrayLength() > 0) Console.WriteLine($"  dropped (they would have played instead): {List(r.GetProperty("dropped"))}");
        if (r.GetProperty("kept").GetArrayLength() > 0) Console.WriteLine($"  kept: {List(r.GetProperty("kept"))}");
        var names = Clips(output);
        PrintGame(names);
        var plays = BattleClips.AsTheGameReads(names).ToDictionary(x => x.Role, x => x.Clip);
        var wrong = clips.Where(c => r.GetProperty("written").EnumerateArray().Any(w => w.GetString() == c.Value)
                                     && plays[BattleClips.RoleOf(c.Key)] != c.Value).ToList();
        foreach (var w in wrong) Console.Error.WriteLine($"!! the game plays {plays[BattleClips.RoleOf(w.Key)] ?? "nothing"} for {w.Key}, not {w.Value}");
        return wrong.Count == 0 ? 0 : 1;
    }

    static int Render(string blender, string input, string? outDir, int frames, int size, List<string> only)
    {
        string dir = Path.GetFullPath(outDir ?? Path.Combine(Path.GetDirectoryName(input)!, Path.GetFileNameWithoutExtension(input) + ".sheets"));
        var r = Blender.Run(blender, "render.py", new() { ["input"] = input, ["out"] = dir, ["frames"] = frames, ["size"] = size, ["only"] = only });
        foreach (var sheet in r.GetProperty("sheets").EnumerateArray())
            Console.WriteLine($"wrote {sheet.GetProperty("file").GetString()} ({sheet.GetProperty("seconds")} s, {frames} moments)");
        return 0;
    }

    /// <summary>A file's clips as the game reads them.</summary>
    static List<string> Clips(string path) => GltfReader.Read(path).Animations.Select(a => a.Name).ToList();

    static void PrintRoles(JsonElement roles)
    {
        Console.WriteLine("bones by role:");
        foreach (var role in roles.EnumerateObject())
            Console.WriteLine($"  {role.Name,-6} {(role.Value.GetArrayLength() == 0 ? "(none)" : List(role.Value))}");
    }

    static void PrintGame(IReadOnlyList<string> names)
    {
        Console.WriteLine("as the game plays it:");
        foreach (var (role, clip) in BattleClips.AsTheGameReads(names))
            Console.WriteLine($"  {role,-8} {clip ?? "(the body plan's)"}");
    }

    static string List(JsonElement array) => string.Join(", ", array.EnumerateArray().Select(e => e.GetString()));

    static string? Take(List<string> rest, string name)
    {
        int i = rest.IndexOf(name);
        if (i < 0 || i + 1 >= rest.Count) return null;
        string v = rest[i + 1];
        rest.RemoveRange(i, 2);
        return v;
    }
}
