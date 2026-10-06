using System.Diagnostics;

namespace DataImporter;

/// <summary>
/// Where the importers read from. By default each checks out pinned commits of the repositories it needs (only the
/// folders it reads) into its own <c>.cache</c> folder, so every run reads the same data. Move a pin forward to pick
/// up fixes upstream, then run the importers and review the diff of the data files. <c>tools/MapImporter</c> compiles
/// this file too, so both read the same commit of the decompilation.
/// </summary>
public static class Sources
{
    public const string DecompRepo = "https://github.com/pret/pokeplatinum.git";
    public const string DecompCommit = "c248fb3f8cc9934ded800e489567c5c0eeee92eb";
    public static readonly string[] DecompFolders = { "/res/pokemon/", "/res/moves/", "/res/items/data/", "/res/trainers/data/", "/include/data/trainer_class_prize_mul.h", "/include/data/trainer_class_genders.h", "/include/data/mart_items.h", "/generated/" };

    public const string PokeApiRepo = "https://github.com/PokeAPI/pokeapi.git";
    public const string PokeApiCommit = "bc92d3b6029ef1abe9e7ad424c400b338f3c11fe";
    public static readonly string[] PokeApiFolders = { "/data/v2/csv/" };

    /// <summary>
    /// Pokémon Showdown (MIT), for what PokeAPI lacks: Z-Move and Max Move power, the Z-Moves and Max Moves
    /// themselves, whose each Mega Stone and Z-Crystal is, and the colour of each form. Four files, fetched one by one.
    /// </summary>
    public const string ShowdownRaw = "https://raw.githubusercontent.com/smogon/pokemon-showdown";
    public const string ShowdownCommit = "9fb3a5b99f1a0bea17f495c5cc1bfe04fdd19c3e";
    public static readonly string[] ShowdownFiles = { "data/moves.ts", "data/items.ts", "data/pokedex.ts", "LICENSE" };

    /// <summary>The named files of a pinned commit, downloaded once and reused until the commit or the list changes.</summary>
    public static string Download(string cache, string name, string rawBase, string commit, string[] files)
    {
        string dir = Path.Combine(cache, name);
        string stamp = Path.Combine(dir, ".importer-commit");
        string wanted = string.Join('\n', files.Prepend(commit));
        if (File.Exists(stamp) && File.ReadAllText(stamp).ReplaceLineEndings("\n").Trim() == wanted
            && files.All(f => File.Exists(Path.Combine(dir, f)))) return dir;

        using var http = new HttpClient();
        foreach (string file in files)
        {
            string url = $"{rawBase}/{commit}/{file}";
            Console.WriteLine($"  {url}");
            string target = Path.Combine(dir, file);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.WriteAllBytes(target, http.GetByteArrayAsync(url).GetAwaiter().GetResult());
        }
        File.WriteAllText(stamp, wanted);
        return dir;
    }

    /// <summary>A sparse checkout of the pinned commit, made once and reused until the commit or the folders change.</summary>
    public static string Checkout(string cache, string name, string repo, string commit, string[] folders)
    {
        string dir = Path.Combine(cache, name);
        string stamp = Path.Combine(dir, ".importer-commit");
        string wanted = string.Join('\n', folders.Prepend(commit));
        if (File.Exists(stamp) && File.ReadAllText(stamp).ReplaceLineEndings("\n").Trim() == wanted) return dir;

        if (!Directory.Exists(Path.Combine(dir, ".git")))
        {
            Directory.CreateDirectory(cache);
            Git(cache, "clone", "--filter=blob:none", "--no-checkout", "--sparse", repo, name);
        }
        Git(dir, new[] { "sparse-checkout", "set", "--no-cone" }.Concat(folders).ToArray());
        Git(dir, "fetch", "--depth", "1", "origin", commit);
        Git(dir, "checkout", "--detach", commit);
        File.WriteAllText(stamp, wanted);
        return dir;
    }

    private static void Git(string workingDir, params string[] args)
    {
        Console.WriteLine($"  git {string.Join(' ', args)}");
        var info = new ProcessStartInfo("git") { WorkingDirectory = workingDir, UseShellExecute = false };
        foreach (var a in args) info.ArgumentList.Add(a);
        using var process = Process.Start(info) ?? throw new InvalidOperationException("Could not start git");
        process.WaitForExit();
        if (process.ExitCode != 0) throw new InvalidOperationException($"git {string.Join(' ', args)} failed in {workingDir}");
    }
}
