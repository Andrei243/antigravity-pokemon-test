#nullable enable

using System.Text.Json;

// What `profile` measured, written beside its output as `profile.json` (plan 16 · T16), and `ab`, which reads three
// of them without starting the game: a before, an after and a before again, run back to back by tools/dev/ab.sh.

/// <summary>One scene of `profile`: its frame times' median and spread, and each pass's median with the profiler on.</summary>
sealed class ProfileScene
{
    public string Preset { get; set; } = "";
    public string Name { get; set; } = "";
    public double Median { get; set; }
    public double P10 { get; set; }
    public double P90 { get; set; }
    public double Mean { get; set; }
    public Dictionary<string, double> Passes { get; set; } = new();
}

/// <summary>A run of `profile`: the renderer it ran on, the window, and its scenes in the order they were timed.</summary>
sealed class ProfileRun
{
    public string? Renderer { get; set; }
    public string Window { get; set; } = "";
    public List<ProfileScene> Scenes { get; set; } = new();

    static readonly JsonSerializerOptions Options = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public void Save(string path) => File.WriteAllText(path, JsonSerializer.Serialize(this, Options));

    public static ProfileRun Load(string path) => JsonSerializer.Deserialize<ProfileRun>(File.ReadAllText(path), Options)
        ?? throw new InvalidDataException(path + " holds no run");

    /// <summary>
    /// The run in a folder: its profile.json, or for a commit from before it, what its `profile` printed
    /// (profile.txt, written by tools/dev/ab.sh), each scene's mean standing for its median.
    /// </summary>
    public static ProfileRun LoadFolder(string folder)
    {
        string json = Path.Combine(folder, "profile.json"), text = Path.Combine(folder, "profile.txt");
        if (File.Exists(json)) return Load(json);
        if (!File.Exists(text)) throw new FileNotFoundException($"{folder} holds no profile.json or profile.txt");
        var run = new ProfileRun();
        string preset = "High";
        foreach (string line in File.ReadLines(text))
        {
            if (line.StartsWith("--- ")) { preset = line[4..].Trim(); continue; }
            if (line.StartsWith("window ")) { run.Window = line[7..].Split(',')[0]; continue; }
            int end = line.IndexOf(" ms/frame", StringComparison.Ordinal);
            int at = end < 0 ? -1 : line.LastIndexOf(": ", end, StringComparison.Ordinal);
            if (line.StartsWith(' ') || at < 0) continue;
            if (!double.TryParse(line[(at + 2)..end], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double ms)) continue;
            run.Scenes.Add(new ProfileScene { Preset = preset, Name = line[..at], Median = ms, P10 = ms, P90 = ms, Mean = ms });
        }
        return run;
    }

    /// <summary>Whether the renderer draws in software (Mesa's llvmpipe and its like), whose milliseconds say nothing of a graphics card's.</summary>
    public bool InSoftware => Renderer is { } r &&
        new[] { "llvmpipe", "softpipe", "swiftshader", "software" }.Any(word => r.Contains(word, StringComparison.OrdinalIgnoreCase));

    /// <summary>The value below which the given share of the times lie, read from the sorted times by the nearest rank.</summary>
    public static double Percentile(IReadOnlyList<double> sorted, double share) =>
        sorted.Count == 0 ? 0 : sorted[Math.Clamp((int)Math.Ceiling(share * sorted.Count) - 1, 0, sorted.Count - 1)];
}

static class Timings
{
    //   dotnet run --project tools/ShotHarness -- <dir> ab <before> <after> <before again>
    //
    // Each folder holds a run of `profile`. For each scene the three medians, the after's change against the mean of
    // the two befores, and "within noise" where that change is smaller than the befores' own difference: the
    // harness shares the graphics card with the desktop, so only a change bigger than the drift between two runs
    // of the same code means something. Refuses runs made in software.
    public static void Ab(string[] args, string startDir)
    {
        if (args.Length < 5) { Console.WriteLine("usage: <dir> ab <before> <after> <before again>"); Environment.ExitCode = 2; return; }
        var runs = args.Skip(2).Take(3).Select(folder => ProfileRun.LoadFolder(Path.GetFullPath(folder, startDir))).ToArray();
        if (runs.FirstOrDefault(r => r.InSoftware) is { } soft)
        {
            Console.WriteLine($"ab: a run was drawn in software ({soft.Renderer}); its milliseconds say nothing of the game on a graphics card");
            Environment.ExitCode = 2;
            return;
        }
        if (runs.Select(r => r.Window).Distinct().Count() > 1)
            Console.WriteLine($"ab: the runs' windows differ ({string.Join(", ", runs.Select(r => r.Window))}): compare like with like");
        Console.WriteLine($"renderer {runs.Select(r => r.Renderer).FirstOrDefault(r => r != null) ?? "unknown"}, window {runs[0].Window}");
        Console.WriteLine($"{"scene",-48} {"before",8} {"after",8} {"again",8} {"change",9}");
        static string Key(ProfileScene s) => s.Preset + "|" + s.Name;
        var after = runs[1].Scenes.ToDictionary(Key);
        var again = runs[2].Scenes.ToDictionary(Key);
        string? preset = null;
        foreach (var before in runs[0].Scenes)
        {
            if (!after.TryGetValue(Key(before), out var a) || !again.TryGetValue(Key(before), out var b)) continue;
            if (before.Preset != preset) { preset = before.Preset; Console.WriteLine($"--- {preset}"); }
            double mean = (before.Median + b.Median) / 2, change = a.Median - mean, noise = Math.Abs(before.Median - b.Median);
            string verdict = Math.Abs(change) <= noise ? "  within noise" : "";
            string share = mean > 0 ? $" ({change / mean * 100:+0;-0}%)" : "";
            Console.WriteLine($"{before.Name,-48} {before.Median,8:F2} {a.Median,8:F2} {b.Median,8:F2} {change,9:+0.00;-0.00}{share}{verdict}");
        }
        foreach (var missing in runs[0].Scenes.Select(Key).Except(after.Keys).Concat(after.Keys.Except(runs[0].Scenes.Select(Key))))
            Console.WriteLine($"only in one run: {missing.Replace('|', ' ')}");
    }
}
