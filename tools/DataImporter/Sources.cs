using System.Diagnostics;

namespace DataImporter;

/// <summary>
/// Where the importer reads from. By default it checks out pinned commits of both repositories (only the folders it
/// needs) into <c>tools/DataImporter/.cache</c>, so every run reads the same data. Move a pin forward to pick up
/// fixes upstream, then run the importer and review the diff of the data files.
/// </summary>
public static class Sources
{
    public const string DecompRepo = "https://github.com/pret/pokeplatinum.git";
    public const string DecompCommit = "c248fb3f8cc9934ded800e489567c5c0eeee92eb";
    public static readonly string[] DecompFolders = { "/res/pokemon/", "/res/moves/", "/res/items/data/", "/generated/" };

    public const string PokeApiRepo = "https://github.com/PokeAPI/pokeapi.git";
    public const string PokeApiCommit = "bc92d3b6029ef1abe9e7ad424c400b338f3c11fe";
    public static readonly string[] PokeApiFolders = { "/data/v2/csv/" };

    /// <summary>A sparse checkout of the pinned commit, made once and reused.</summary>
    public static string Checkout(string cache, string name, string repo, string commit, string[] folders)
    {
        string dir = Path.Combine(cache, name);
        string stamp = Path.Combine(dir, ".importer-commit");
        if (File.Exists(stamp) && File.ReadAllText(stamp).Trim() == commit) return dir;

        if (!Directory.Exists(Path.Combine(dir, ".git")))
        {
            Directory.CreateDirectory(cache);
            Git(cache, "clone", "--filter=blob:none", "--no-checkout", "--sparse", repo, name);
        }
        Git(dir, new[] { "sparse-checkout", "set", "--no-cone" }.Concat(folders).ToArray());
        Git(dir, "fetch", "--depth", "1", "origin", commit);
        Git(dir, "checkout", "--detach", commit);
        File.WriteAllText(stamp, commit);
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
