// First-draft 3D sculpts of the new species, made by Tripo from the concept pictures of tools/GeminiArt.
//   dotnet run --project tools/TripoSculpt -- list [--set kanto|johto|hoenn|sinnoh|megas]
//   dotnet run --project tools/TripoSculpt -- generate (--set s | --all | name ... | --image <file> --name <Name>)
//                                                      [--model <id>] [--face-limit <n>] [--seed <n>] [--no-texture] [--pbr] [--autofix]
//                                                      [--force] [--limit <n>] [--delay <ms>]
//   dotnet run --project tools/TripoSculpt -- rig (--set s | --all | name ...) [--rig-type biped|quadruped|hexapod|octopod|serpentine|aquatic|avian|others] [--rig-model <id>]
//                                                 [--animations idle,hurt,...] [--reanimate] [--force] [--limit <n>]
//   dotnet run --project tools/TripoSculpt -- install [--set s] [--as <Species>] [--private] [--static] [--force] [name ...]
//   dotnet run --project tools/TripoSculpt -- selftest
// The key is read from the TRIPO_API_KEY environment variable and never written anywhere (platform.tripo3d.ai/api-keys).
// Every task costs credits, so generate does nothing until it is told which pictures; --limit 1 is the way to try one.
// Sculpts go to tools/TripoSculpt/out/<set>/<slug>.glb with a <slug>.json note of the task (tracked by git: drafts, not game art).
// rig checks the body, rigs the sculpt and retargets preset animations onto it (<slug>.animated.glb, the clips named for the roles the game reads).
// install copies the ones you approve to assets/models/<Species>.glb (tracked by git; --private: overrides/models, which is not),
// where the game picks them up (docs/model-files.md).

return await Cli.Run(args);

static class Cli
{
    public static async Task<int> Run(string[] args)
    {
        if (args.Length == 0 || args[0] is "-h" or "--help")
        {
            Console.WriteLine("usage: TripoSculpt list [--set s] | generate (--set s | --all | name ... | --image file --name Name) [options] | rig (--set s | --all | name ...) [--rig-type t] [--rig-model id] [--animations a,b] [--limit n] | install [--set s] [--as Species] [--private] [--static] [--force] [name ...] | selftest");
            Console.WriteLine("generate options: --model id  --face-limit n  --seed n  --no-texture  --pbr  --autofix  --force  --limit n  --delay ms");
            return args.Length == 0 ? 2 : 0;
        }

        var rest = args.Skip(1).ToList();
        string verb = args[0];
        if (verb == "selftest") return await SelfTest.Run();

        string root = FindRoot();
        string? set = Take(rest, "--set");
        string? image = Take(rest, "--image");
        string? asName = Take(rest, "--name") ?? Take(rest, "--as");
        bool all = Flag(rest, "--all");
        bool untracked = Flag(rest, "--private");
        bool staticOnly = Flag(rest, "--static");
        bool reanimate = Flag(rest, "--reanimate");
        string? rigType = Take(rest, "--rig-type");
        string? rigModel = Take(rest, "--rig-model");
        var animations = Take(rest, "--animations")?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                             .Select(a => a.Contains(':') ? a : "preset:" + a).ToList();

        var o = Options.Default;
        o = o with
        {
            Model = Take(rest, "--model") ?? Environment.GetEnvironmentVariable("TRIPO_MODEL") ?? o.Model,
            FaceLimit = IntOf(rest, "--face-limit") ?? o.FaceLimit,
            Seed = IntOf(rest, "--seed"),
            Limit = IntOf(rest, "--limit") ?? o.Limit,
            DelayMs = IntOf(rest, "--delay") ?? o.DelayMs,
            Texture = !Flag(rest, "--no-texture"),
            Pbr = Flag(rest, "--pbr"),
            Autofix = Flag(rest, "--autofix"),
            Force = Flag(rest, "--force"),
        };
        if (rest.Any(r => r.StartsWith("--"))) { Console.Error.WriteLine($"unknown option {rest.First(r => r.StartsWith("--"))}"); return 2; }

        var concepts = Concept.ReadAll(root);
        if (image != null)
        {
            if (!File.Exists(image)) { Console.Error.WriteLine($"no picture at {image}"); return 2; }
            if (asName == null) { Console.Error.WriteLine("--image needs --name <Name>, the species the sculpt is for"); return 2; }
            concepts = new List<Concept> { new("adhoc", Slug(asName), asName, Path.GetFullPath(image)) };
            rest.Clear();
            asName = null;
        }
        var chosen = concepts.Where(c => set == null || c.Set == set)
                             .Where(c => rest.Count == 0 || rest.Any(c.Matches))
                             .ToList();

        switch (verb)
        {
            case "list":
                if (concepts.Count == 0) Console.WriteLine("no concept pictures yet: run tools/GeminiArt first (they are in docs/art/reference/gemini).");
                foreach (var g in chosen.GroupBy(c => c.Set))
                    Console.WriteLine($"{g.Key,-8} {g.Count(),4} pictures, {g.Count(c => File.Exists(c.Glb(root)))} sculpted");
                foreach (var c in chosen.Where(_ => rest.Count > 0))
                    Console.WriteLine($"  {c.Set}/{c.Slug}  {(File.Exists(c.Glb(root)) ? "sculpted" : "-")}");
                Console.WriteLine($"{chosen.Count} in all");
                return 0;

            case "generate":
                return await Generate(root, chosen, rest, set, all, image != null, o);

            case "rig":
                return await Rig(root, chosen, rest, set, all, new RigOptions(rigType, rigModel, animations, o.Force, o.Limit, o.DelayMs, o.Timeout, o.PollMs, reanimate));

            case "install":
                if (chosen.Count == 0) { Console.Error.WriteLine("nothing matches."); return 2; }
                if (asName != null && chosen.Count != 1) { Console.Error.WriteLine("--as names one species: choose one sculpt."); return 2; }
                if (set == null && rest.Count == 0) { Console.Error.WriteLine("say which: --set <set> or the names."); return 2; }
                Sculptor.Install(root, chosen, asName, o.Force, Console.Out, untracked, staticOnly);
                return 0;

            default:
                Console.Error.WriteLine($"unknown command {verb}");
                return 2;
        }
    }

    static async Task<int> Generate(string root, List<Concept> chosen, List<string> names, string? set, bool all, bool adhoc, Options o)
    {
        string? key = Environment.GetEnvironmentVariable("TRIPO_API_KEY");
        if (string.IsNullOrWhiteSpace(key))
        {
            Console.Error.WriteLine("set TRIPO_API_KEY first (the key is only read from the environment).");
            return 2;
        }
        if (!adhoc && !all && set == null && names.Count == 0)
        {
            Console.Error.WriteLine("every sculpt costs credits: say which pictures (--set <set>, --all, names, or --image/--name), and try one first with --limit 1.");
            return 2;
        }
        if (chosen.Count == 0) { Console.Error.WriteLine("nothing matches."); return 2; }

        int todo = chosen.Count(c => o.Force || !File.Exists(c.Glb(root)));
        Console.WriteLine($"{Math.Min(todo, o.Limit)} sculpt(s) to make with {o.Model}, up to {o.FaceLimit} triangles, {(o.Pbr ? "PBR textures" : o.Texture ? "base-colour textures" : "no textures")}.");

        var client = new TripoClient(key, Environment.GetEnvironmentVariable("TRIPO_BASE_URL"));
        var outcome = await Sculptor.Generate(root, client, chosen, o, Console.Out);
        return outcome.Failed == 0 ? 0 : 1;
    }

    static async Task<int> Rig(string root, List<Concept> chosen, List<string> names, string? set, bool all, RigOptions ro)
    {
        string? key = Environment.GetEnvironmentVariable("TRIPO_API_KEY");
        if (string.IsNullOrWhiteSpace(key)) { Console.Error.WriteLine("set TRIPO_API_KEY first (the key is only read from the environment)."); return 2; }
        if (!all && set == null && names.Count == 0)
        {
            Console.Error.WriteLine("rigging costs credits: say which sculpts (--set <set>, --all or names), and try one first with --limit 1.");
            return 2;
        }
        var todo = chosen.Where(c => File.Exists(c.Glb(root)) && (ro.Force || ro.Reanimate || !File.Exists(c.Animated(root)))).ToList();
        if (todo.Count == 0) { Console.Error.WriteLine("nothing to rig: no sculpt yet (generate first), or the rigged files are there already (--force)."); return 2; }
        Console.WriteLine($"{Math.Min(todo.Count, ro.Limit)} sculpt(s) to rig and animate " +
                          $"({(ro.Animations is { } a ? string.Join(", ", a) : "the default presets of the body Tripo finds")}).");
        var client = new TripoClient(key, Environment.GetEnvironmentVariable("TRIPO_BASE_URL"));
        var outcome = await Rigger.Run(root, client, todo, ro, Console.Out);
        return outcome.Failed == 0 ? 0 : 1;
    }

    static string Slug(string s) => System.Text.RegularExpressions.Regex.Replace(s, @"[^A-Za-z0-9\-]+", "-").Trim('-');

    static string? Take(List<string> a, string flag)
    {
        int i = a.IndexOf(flag);
        if (i < 0 || i + 1 >= a.Count) return null;
        string v = a[i + 1];
        a.RemoveRange(i, 2);
        return v;
    }

    static int? IntOf(List<string> a, string flag) => int.TryParse(Take(a, flag), out var v) ? v : null;

    static bool Flag(List<string> a, string flag) => a.Remove(flag);

    public static string FindRoot()
    {
        for (var d = new DirectoryInfo(Directory.GetCurrentDirectory()); d != null; d = d.Parent)
            if (File.Exists(Path.Combine(d.FullName, "PokemonPlatinum.sln"))) return d.FullName;
        throw new InvalidOperationException("run this from inside the repository");
    }
}
