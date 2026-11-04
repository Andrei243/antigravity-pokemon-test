using System.Diagnostics;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Data;

/// <summary>
/// <c>tools/VoiceGen/engine.json</c>: the engine plan 26's trials chose, run as a command line, a take at a time
/// (<c>tools/VoiceGen/README.md</c>). Nothing is set up until the user has chosen an engine and approved its licence and
/// download; until then <c>generate</c> only says so.
/// </summary>
public sealed class EngineConfig
{
    /// <summary>The engine and its version, written into each take's entry of <c>pack.json</c>.</summary>
    public string Name { get; set; } = "";

    /// <summary>The program to run (the engine's own Python in its own folder, never the system's).</summary>
    public string Command { get; set; } = "";

    /// <summary>
    /// Its arguments, with <c>{textFile}</c> (a file holding the text to say, UTF-8), <c>{out}</c> (the WAV to write),
    /// <c>{seed}</c>, <c>{sample}</c> and <c>{sampleText}</c> (a cloned voice's reference and its words; empty otherwise),
    /// <c>{voice}</c>, <c>{direction}</c> and <c>{repo}</c> filled in.
    /// </summary>
    public List<string> Args { get; set; } = new();

    /// <summary>Whether the engine is handed the text with the names respelled (<c>Data/voice/lexicon.json</c>).</summary>
    public bool UseRespelling { get; set; }

    public int TimeoutSeconds { get; set; } = 600;
}

public sealed class CommandEngine : IVoiceEngine
{
    private readonly EngineConfig config;
    private readonly string repo;

    public string Name => config.Name;

    private CommandEngine(EngineConfig config, string repo)
    {
        this.config = config;
        this.repo = repo;
    }

    public static CommandEngine Read(string path, string repo) =>
        new(GameDataFiles.Deserialize<EngineConfig>(File.ReadAllText(path)), repo);

    public (float[] Samples, int Rate)? Speak(VoiceJob job, int seed)
    {
        string scratch = Path.Combine(Path.GetTempPath(), "voicegen");
        Directory.CreateDirectory(scratch);
        string stem = VoicePacks.FileNameOf(job.Key) + "~" + seed;
        string textFile = Path.Combine(scratch, stem + ".txt"), output = Path.Combine(scratch, stem + ".wav");
        File.WriteAllText(textFile, config.UseRespelling && job.Respelled != null ? job.Respelled : job.Text);
        if (File.Exists(output)) File.Delete(output);
        var start = new ProcessStartInfo(config.Command) { UseShellExecute = false, RedirectStandardError = true, RedirectStandardOutput = true };
        foreach (string arg in config.Args)
            start.ArgumentList.Add(arg.Replace("{textFile}", textFile).Replace("{out}", output).Replace("{seed}", seed.ToString())
                .Replace("{sample}", job.Sample != null ? Path.Combine(repo, job.Sample) : "").Replace("{sampleText}", job.SampleText ?? "")
                .Replace("{voice}", job.Voice).Replace("{direction}", job.Direction).Replace("{repo}", repo));
        using var process = Process.Start(start);
        if (process == null) return null;
        string errors = process.StandardError.ReadToEnd();
        process.StandardOutput.ReadToEnd();
        if (!process.WaitForExit(config.TimeoutSeconds * 1000)) { process.Kill(true); return null; }
        if (process.ExitCode != 0 || !File.Exists(output))
        {
            Console.Error.WriteLine($"{job.Key} (seed {seed}): the engine ended with {process.ExitCode}. {errors.Trim()}");
            return null;
        }
        var wave = WavDecoder.Read(File.ReadAllBytes(output));
        File.Delete(output);
        File.Delete(textFile);
        return (wave.Mono, wave.Rate);
    }
}
