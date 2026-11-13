using System.Diagnostics;
using PokemonPlatinumEngine.Graphics;

namespace BlenderAnim;

/// <summary>
/// Builds species' models from their Blender sculpts (<c>blender/sculpts/&lt;species&gt;.py</c>; plan 48 · BL1) and puts
/// them where the game looks for built models: <c>built/models</c> at the repository's root, which git ignores and a
/// published game carries. Each sculpt stands on the skeleton and clips of the game's own sculpt of the species, which
/// is exported here first with every model file switched off, so that a species already built is never exported as
/// its own file.
/// </summary>
internal static class Sculpt
{
    /// <summary>The script that sculpts a species: its name in lower case, letters and digits only.</summary>
    public static string ScriptOf(string species) =>
        Path.Combine(AppContext.BaseDirectory, "blender", "sculpts", new string(species.ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray()) + ".py");

    /// <summary>The species that have a sculpt, by their scripts' names.</summary>
    public static IEnumerable<string> Sculpted()
    {
        string dir = Path.Combine(AppContext.BaseDirectory, "blender", "sculpts");
        if (!Directory.Exists(dir)) yield break;
        var scripts = Directory.GetFiles(dir, "*.py").Select(Path.GetFileNameWithoutExtension).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (string species in PokemonModels.Species.Concat(PokemonModels.Forms))
            if (scripts.Contains(new string(species.ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray()))) yield return species;
    }

    /// <summary>The game's own sculpt of a species as a .glb, whatever model files there are.</summary>
    public static void Export(string species, string file)
    {
        bool before = Overrides.Enabled;
        Overrides.Enabled = false;
        try
        {
            GltfWriter.Save(PokemonModels.Build(species), file);
        }
        finally
        {
            Overrides.Enabled = before;
        }
    }

    public static string? RepositoryRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir != null; dir = dir.Parent)
            if (File.Exists(Path.Combine(dir.FullName, "PokemonPlatinum.sln"))) return dir.FullName;
        return null;
    }

    /// <summary>
    /// The repository's main checkout when <paramref name="root"/> is a git worktree of it (its <c>.git</c> is a file
    /// naming <c>&lt;main&gt;/.git/worktrees/&lt;name&gt;</c>), else null.
    /// </summary>
    public static string? MainCheckout(string root)
    {
        string dotGit = Path.Combine(root, ".git");
        if (!File.Exists(dotGit)) return null;
        string line = File.ReadAllText(dotGit).Trim();
        if (!line.StartsWith("gitdir:", StringComparison.Ordinal)) return null;
        string dir = line["gitdir:".Length..].Trim().Replace('\\', '/');
        int at = dir.IndexOf("/.git/worktrees/", StringComparison.Ordinal);
        return at > 0 && Directory.Exists(dir[..at]) ? Path.GetFullPath(dir[..at]) : null;
    }

    public static int Run(string blender, IReadOnlyList<string> named, string? outDir)
    {
        var species = named.Count == 1 && named[0] == "all" ? Sculpted().ToList() : named.ToList();
        if (species.Count == 0) { Console.Error.WriteLine("no species has a sculpt in blender/sculpts"); return 2; }
        foreach (string s in species)
        {
            if (!PokemonModels.Species.Concat(PokemonModels.Forms).Contains(s)) { Console.Error.WriteLine($"{s}: no species or form of that name (as PokemonModels spells it)"); return 2; }
            if (!File.Exists(ScriptOf(s))) { Console.Error.WriteLine($"{s}: no sculpt at blender/sculpts/{Path.GetFileName(ScriptOf(s))}"); return 2; }
        }
        string? root = RepositoryRoot();
        if (outDir == null && root == null) { Console.Error.WriteLine("the repository wasn't found above the tool: give --out <folder>"); return 2; }
        string target = Path.GetFullPath(outDir ?? Path.Combine(root!, "built", "models"));
        Directory.CreateDirectory(target);
        string work = Path.Combine(Path.GetTempPath(), "BlenderAnim", "sculpt-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(work);
        int failed = 0;
        try
        {
            foreach (string s in species)
            {
                var clock = Stopwatch.StartNew();
                string export = Path.Combine(work, s + ".export.glb"), first = Path.Combine(work, s + ".1.glb"), second = Path.Combine(work, s + ".2.glb");
                Export(s, export);
                string said = Build(blender, ScriptOf(s), export, first);
                Build(blender, ScriptOf(s), export, second);
                if (!File.ReadAllBytes(first).AsSpan().SequenceEqual(File.ReadAllBytes(second)))
                {
                    Console.Error.WriteLine($"!! {s}: two builds differ; nothing installed");
                    failed++;
                    continue;
                }
                string file = Path.Combine(target, s + ".glb");
                File.Copy(first, file, overwrite: true);
                // In a worktree the main checkout gets the file too: that is where the game is played from
                if (outDir == null && MainCheckout(root!) is { } main)
                {
                    Directory.CreateDirectory(Path.Combine(main, "built", "models"));
                    File.Copy(first, Path.Combine(main, "built", "models", s + ".glb"), overwrite: true);
                }
                var asset = GltfReader.Read(file);
                var eyes = asset.Nodes.Select(n => ImportedModels.EyesOf(n.Name)).Where(e => e != null).Select(e => e!.Value).Distinct().OrderBy(e => e).ToList();
                var surfaces = asset.Materials.Select(m => ImportedModels.SurfaceNamed(m.Name)?.ToString() ?? $"({m.Name})").Distinct().ToList();
                Console.WriteLine($"{s}: {new FileInfo(file).Length / 1024} KB in {clock.Elapsed.TotalSeconds:0} s -> {file}");
                Console.WriteLine($"  {said}");
                Console.WriteLine($"  eyes: {(eyes.Count == 0 ? "open only (no eye pieces)" : string.Join(", ", eyes))}; surfaces: {string.Join(", ", surfaces)}; clips: {asset.Animations.Count}");
            }
        }
        finally
        {
            try { Directory.Delete(work, recursive: true); } catch (IOException) { }
        }
        return failed == 0 ? 0 : 1;
    }

    /// <summary>Runs a sculpt's script in Blender and returns the kit's last line of what it built.</summary>
    static string Build(string blender, string script, string export, string output)
    {
        var start = new ProcessStartInfo(blender) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        foreach (string a in new[] { "--background", "--factory-startup", "--python-exit-code", "1", "--python", script, "--", export, output })
            start.ArgumentList.Add(a);
        using var p = Process.Start(start)!;
        var lines = new List<string>();
        p.OutputDataReceived += (_, e) => { if (e.Data != null) lock (lines) lines.Add(e.Data); };
        p.ErrorDataReceived += (_, e) => { if (e.Data != null) lock (lines) lines.Add(e.Data); };
        p.BeginOutputReadLine();
        p.BeginErrorReadLine();
        p.WaitForExit();
        if (p.ExitCode != 0 || !File.Exists(output))
            throw new InvalidOperationException($"Blender ended with {p.ExitCode} on {Path.GetFileName(script)}:{Environment.NewLine}{string.Join(Environment.NewLine, lines.TakeLast(30))}");
        lock (lines) return string.Join("; ", lines.Where(l => l.StartsWith("KIT: ") && !l.StartsWith("KIT: wrote")).Select(l => l[5..]));
    }
}
