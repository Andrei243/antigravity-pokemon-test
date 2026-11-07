using System.Diagnostics;
using System.Text.Json;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Data;

/// <summary>
/// <c>tools/VoiceGen/engine.json</c>: the engine plan 26 chose (Kokoro-82M, approved by the user on 2026-11-07), run as a
/// command line, a take at a time or, with <see cref="Serve"/>, one process for a whole run (<c>tools/VoiceGen/README.md</c>).
/// Until the file is there <c>generate</c> only says so.
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
    /// <c>{voice}</c>, <c>{direction}</c>, <c>{repo}</c> and <c>{voicegen}</c> (<see cref="CommandEngine.HomeFolder"/>)
    /// filled in. With <see cref="Serve"/> only <c>{repo}</c> and <c>{voicegen}</c> mean anything.
    /// </summary>
    public List<string> Args { get; set; } = new();

    /// <summary>Whether the engine is handed the text with the names respelled (<c>Data/voice/lexicon.json</c>).</summary>
    public bool UseRespelling { get; set; }

    /// <summary>
    /// Whether the program stays up for the whole run: it answers <c>ready</c> once it has loaded, then takes a take a line
    /// on its input as JSON (<c>textFile</c>, <c>out</c>, <c>seed</c>, <c>voice</c>, <c>direction</c>, <c>sample</c>,
    /// <c>sampleText</c>) and answers <c>ok</c> or <c>error: …</c> a line each.
    /// </summary>
    public bool Serve { get; set; }

    public int TimeoutSeconds { get; set; } = 600;
}

public sealed class CommandEngine : IVoiceEngine, IDisposable
{
    private readonly EngineConfig config;
    private readonly string repo;
    private Process? server;

    public string Name => config.Name;

    private CommandEngine(EngineConfig config, string repo)
    {
        this.config = config;
        this.repo = repo;
    }

    public static CommandEngine Read(string path, string repo) =>
        new(GameDataFiles.Deserialize<EngineConfig>(File.ReadAllText(path)), repo);

    /// <summary>
    /// The folder the engine's install and model live in (<c>tools/VoiceGen/setup-kokoro.sh</c>): <c>VOICEGEN_HOME</c>, else
    /// <c>~/.cache/pokemon-voicegen</c>. Never <c>POKEMON_CACHE</c>, which the dev scripts may remove whole at any time.
    /// </summary>
    public static string HomeFolder() =>
        Environment.GetEnvironmentVariable("VOICEGEN_HOME") is { Length: > 0 } c ? c
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".cache", "pokemon-voicegen");

    private string Fill(string text) => text.Replace("{repo}", repo).Replace("{voicegen}", HomeFolder());

    public (float[] Samples, int Rate)? Speak(VoiceJob job, int seed)
    {
        string scratch = Path.Combine(Path.GetTempPath(), "voicegen");
        Directory.CreateDirectory(scratch);
        string stem = VoicePacks.FileNameOf(job.Key) + "~" + seed;
        string textFile = Path.Combine(scratch, stem + ".txt"), output = Path.Combine(scratch, stem + ".wav");
        File.WriteAllText(textFile, config.UseRespelling && job.Respelled != null ? job.Respelled : job.Text);
        if (File.Exists(output)) File.Delete(output);
        string sample = job.Sample != null ? Path.Combine(repo, job.Sample) : "";
        bool made = config.Serve ? AskServer(job, seed, textFile, output, sample) : RunOnce(job, seed, textFile, output, sample);
        if (!made || !File.Exists(output)) return null;
        var wave = WavDecoder.Read(File.ReadAllBytes(output));
        File.Delete(output);
        File.Delete(textFile);
        return (wave.Mono, wave.Rate);
    }

    private bool RunOnce(VoiceJob job, int seed, string textFile, string output, string sample)
    {
        var start = new ProcessStartInfo(Fill(config.Command)) { UseShellExecute = false, RedirectStandardError = true, RedirectStandardOutput = true };
        foreach (string arg in config.Args)
            start.ArgumentList.Add(Fill(arg.Replace("{textFile}", textFile).Replace("{out}", output).Replace("{seed}", seed.ToString())
                .Replace("{sample}", sample).Replace("{sampleText}", job.SampleText ?? "")
                .Replace("{voice}", job.Voice).Replace("{direction}", job.Direction)));
        using var process = Process.Start(start);
        if (process == null) return false;
        var errors = process.StandardError.ReadToEndAsync();
        process.StandardOutput.ReadToEnd();
        if (!process.WaitForExit(config.TimeoutSeconds * 1000)) { process.Kill(true); return false; }
        if (process.ExitCode == 0) return true;
        Console.Error.WriteLine($"{job.Key} (seed {seed}): the engine ended with {process.ExitCode}. {errors.Result.Trim()}");
        return false;
    }

    private bool AskServer(VoiceJob job, int seed, string textFile, string output, string sample)
    {
        if (server == null || server.HasExited)
        {
            var start = new ProcessStartInfo(Fill(config.Command))
            {
                UseShellExecute = false, RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = false,
                StandardOutputEncoding = System.Text.Encoding.UTF8
            };
            foreach (string arg in config.Args) start.ArgumentList.Add(Fill(arg));
            start.Environment["PYTHONIOENCODING"] = "utf-8";
            server = Process.Start(start) ?? throw new InvalidOperationException("the engine did not start");
            string? hello = server.StandardOutput.ReadLine();
            if (hello != "ready") throw new InvalidOperationException($"the engine said '{hello}' where it should have said ready");
        }
        string request = JsonSerializer.Serialize(new Dictionary<string, object>
        {
            ["textFile"] = textFile, ["out"] = output, ["seed"] = seed, ["voice"] = job.Voice, ["direction"] = job.Direction,
            ["sample"] = sample, ["sampleText"] = job.SampleText ?? ""
        });
        server.StandardInput.WriteLine(request);
        server.StandardInput.Flush();
        string? answer = server.StandardOutput.ReadLine();
        if (answer == "ok") return true;
        Console.Error.WriteLine($"{job.Key} (seed {seed}): {answer ?? "the engine stopped"}");
        return false;
    }

    public void Dispose()
    {
        if (server == null) return;
        try
        {
            server.StandardInput.Close();
            if (!server.WaitForExit(10000)) server.Kill(true);
        }
        catch (InvalidOperationException) { }
        server.Dispose();
        server = null;
    }
}
