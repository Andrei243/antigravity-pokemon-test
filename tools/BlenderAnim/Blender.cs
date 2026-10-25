using System.Diagnostics;
using System.Text.Json;

namespace BlenderAnim;

/// <summary>Finds Blender and runs one of the tool's scripts in it with no window, handing it a job and reading its report.</summary>
internal static class Blender
{
    /// <summary>
    /// Blender's executable: <c>--blender</c>, else the <c>BLENDER</c> variable, else <c>blender</c> on the PATH, else the
    /// newest of Blender Foundation's folders under Program Files, else null.
    /// </summary>
    public static string? Find(string? given)
    {
        if (given != null) return File.Exists(given) ? Path.GetFullPath(given) : null;
        if (Environment.GetEnvironmentVariable("BLENDER") is { Length: > 0 } env && File.Exists(env)) return env;
        string exe = OperatingSystem.IsWindows() ? "blender.exe" : "blender";
        foreach (var dir in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            var path = Path.Combine(dir.Trim('"'), exe);
            if (File.Exists(path)) return path;
        }
        if (OperatingSystem.IsWindows())
        {
            var foundation = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Blender Foundation");
            if (Directory.Exists(foundation))
                return Directory.GetDirectories(foundation, "Blender*")
                                .Select(d => Path.Combine(d, exe)).Where(File.Exists)
                                .OrderByDescending(p => Version.TryParse(Path.GetFileName(Path.GetDirectoryName(p))!.Replace("Blender", "").Trim(), out var v) ? v : new Version())
                                .FirstOrDefault();
        }
        if (OperatingSystem.IsMacOS() && File.Exists("/Applications/Blender.app/Contents/MacOS/Blender")) return "/Applications/Blender.app/Contents/MacOS/Blender";
        return null;
    }

    public const string Hint = "no Blender found: install it (winget install BlenderFoundation.Blender.LTS.4.5), or give --blender <exe> or BLENDER";

    /// <summary>
    /// Runs <c>blender/&lt;script&gt;</c> on <paramref name="job"/> (a report path is added to it) and returns the report,
    /// or throws with Blender's own last lines when the script failed.
    /// </summary>
    public static JsonElement Run(string blender, string script, Dictionary<string, object?> job)
    {
        string dir = Path.Combine(Path.GetTempPath(), "BlenderAnim", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            string jobFile = Path.Combine(dir, "job.json"), reportFile = Path.Combine(dir, "report.json");
            job["report"] = reportFile;
            File.WriteAllText(jobFile, JsonSerializer.Serialize(job));
            string scriptPath = Path.Combine(AppContext.BaseDirectory, "blender", script);
            var start = new ProcessStartInfo(blender)
            {
                RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false,
            };
            foreach (var a in new[] { "--background", "--factory-startup", "--python-exit-code", "1", "--python", scriptPath, "--", jobFile })
                start.ArgumentList.Add(a);
            using var p = Process.Start(start)!;
            var tail = new List<string>();
            void Keep(string? line)
            {
                if (line == null) return;
                lock (tail)
                {
                    tail.Add(line);
                    if (tail.Count > 40) tail.RemoveAt(0);
                }
            }
            p.OutputDataReceived += (_, e) => Keep(e.Data);
            p.ErrorDataReceived += (_, e) => Keep(e.Data);
            p.BeginOutputReadLine();
            p.BeginErrorReadLine();
            p.WaitForExit();
            if (tail.FirstOrDefault(l => l.StartsWith("BA:ERROR ")) is { } said) throw new InvalidOperationException(said["BA:ERROR ".Length..]);
            if (p.ExitCode != 0 || !File.Exists(reportFile))
                throw new InvalidOperationException($"Blender ended with {p.ExitCode}:{Environment.NewLine}{string.Join(Environment.NewLine, tail)}");
            return JsonDocument.Parse(File.ReadAllText(reportFile)).RootElement.Clone();
        }
        finally
        {
            try { Directory.Delete(dir, true); } catch (IOException) { }
        }
    }
}
