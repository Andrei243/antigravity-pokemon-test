using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Core.Autoplay;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 26: how the player's game finds a voice pack. <c>--voices &lt;folder&gt;</c> names the pack and turns the options'
/// VOICES row to All for the run; a pack made on the developer's machine (<c>~/.cache/pokemon-voicegen/voice/en</c>, or
/// <c>VOICE_PACK</c>) is held to the first chapter played with no window, and skipped where there is none.
/// </summary>
[Collection("Shared clocks and dice")]
public sealed class VoicePackSwitchTests : IDisposable
{
    private readonly ITestOutputHelper output;
    private readonly string folder = Path.Combine(Path.GetTempPath(), "voice-switch-" + Guid.NewGuid().ToString("N"));
    private readonly LaunchOptions launched = CommandLine.Launched;

    public VoicePackSwitchTests(ITestOutputHelper output)
    {
        this.output = output;
        Directory.CreateDirectory(folder);
    }

    public void Dispose()
    {
        CommandLine.Launched = launched;
        AudioManager.StopListening();
        if (Directory.Exists(folder)) Directory.Delete(folder, recursive: true);
    }

    private static LaunchOptions Parse(params string[] args) => CommandLine.Parse(args);

    // ------------------------------------------------------------------ the switch

    [Fact]
    public void VoicesNamesAFolderAndMakesTheRowAllForThatRunAlone()
    {
        var launch = Parse("--voices", folder);
        Assert.True(launch.IsValid, string.Join("\n", launch.Errors));
        Assert.Equal(Path.GetFullPath(folder), launch.VoicePack);
        Assert.Equal(Path.GetFullPath(folder), Parse("--voices=" + folder).VoicePack);

        var settings = new GameSettings();
        Assert.Equal(VoiceMode.Off, settings.Voices);
        string before = settings.ToFile();
        launch.ApplyTo(settings);
        Assert.Equal(VoiceMode.All, settings.Voices);
        // For this run only: what is written to settings.json is what the player had
        Assert.Equal(before, settings.ToFile());

        // Without the switch nothing changes of the row
        var plain = new GameSettings { Voices = VoiceMode.Story };
        Parse().ApplyTo(plain);
        Assert.Equal(VoiceMode.Story, plain.Voices);
        Assert.Null(Parse().VoicePack);
    }

    [Fact]
    public void VoicesWithNoFolderOrAFolderThatIsNotThereIsRefusedWithAWord()
    {
        Assert.Contains(Parse("--voices").Errors, e => e.Contains("--voices needs a value"));
        string missing = Path.Combine(folder, "nowhere");
        Assert.Contains(Parse("--voices", missing).Errors, e => e.Contains("--voices takes a folder that is there"));
        Assert.Contains(Parse("--voices", folder, "--voices", folder).Errors, e => e.Contains("--voices is given twice"));
        Assert.Contains("--voices <folder>", CommandLine.Usage());
    }

    // ------------------------------------------------------------------ where the game looks

    private static VoiceFile OneLine() => new()
    {
        Lines =
        {
            new VoiceLine { Key = "script:test.A#0.0", Voice = "rival", En = "Hey! Over here!", Hash = VoiceCatalogue.HashOf("Hey! Over here!"), Spoken = "Hey! Over here!" }
        }
    };

    [Fact]
    public void ThePackTheSwitchNamesIsTheOneTheGameSpeaksWith()
    {
        // A folder of languages (en/ inside it) and a folder that is the pack itself
        string languages = Path.Combine(folder, "languages"), single = Path.Combine(folder, "single");
        Assert.Equal(1, VoicePacks.WriteStandIns(Path.Combine(languages, "en"), OneLine(), "en", (_, _) => true));
        Assert.Equal(1, VoicePacks.WriteStandIns(single, OneLine(), "en", (_, _) => true));
        VoiceLibrary? a = null, b = null;
        try
        {
            CommandLine.Launched = Parse("--voices", languages);
            a = VoiceLibrary.Find("en");
            Assert.NotNull(a);
            Assert.Equal(Path.Combine(Path.GetFullPath(languages), "en"), a!.Folder);

            CommandLine.Launched = Parse("--voices", single);
            b = VoiceLibrary.Find("en");
            Assert.NotNull(b);
            Assert.Equal(Path.GetFullPath(single), b!.Folder);
            // A pack that is one language's is not another's
            Assert.Null(VoiceLibrary.Find("fr"));
            // And the folder given comes before the ones beside the game
            Assert.Equal(Path.GetFullPath(single), VoiceLibrary.FoldersFor("en").Skip(1).First());
        }
        finally
        {
            a?.Settle();
            b?.Settle();
        }
    }

    // ------------------------------------------------------------------ the pack made on the developer's machine

    private static string? RealPackFolder()
    {
        if (Environment.GetEnvironmentVariable("VOICE_PACK") is { Length: > 0 } given) return given;
        string home = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".cache", "pokemon-voicegen", "voice", "en");
        return Directory.Exists(home) ? home : null;
    }

    /// <summary>
    /// The first chapter played by the bot from a new game's bedroom with the pack in: every line the game asks a voice for
    /// that the catalogue speaks (an NPC's, the rival's, the professor's) has a current take in the voice that speaks it.
    /// </summary>
    [Fact]
    public void TheFirstChapterOfANewGameResolvesEveryLineToATake()
    {
        string? where = RealPackFolder();
        var pack = where == null ? null : VoiceLibrary.Open(where);
        if (pack == null)
        {
            output.WriteLine("No voice pack here (~/.cache/pokemon-voicegen/voice/en or VOICE_PACK): nothing to hold the first chapter to.");
            return;
        }
        var catalogue = VoiceCatalogue.Parse(File.ReadAllText(Path.Combine(Repository.Root, "PokemonPlatinumEngine", "Data", "voice", "lines.json")))
            .Lines.ToDictionary(l => l.Key, StringComparer.Ordinal);
        List<string> heard = new();
        try
        {
            Headless.Play(engine =>
            {
                engine.Settings.Voices = VoiceMode.All;
                engine.VoicePackOverride = pack;
                engine.ApplySettings(window: false);
                heard = AudioManager.Listen();
                var bot = new Autoplayer(engine, seed: 1) { Itinerary = new Itinerary(Itineraries.FirstChapter) };
                const float dt = 1f / 60f;
                for (double t = 0; t < 1800 && !bot.Stopped; t += dt)
                {
                    bot.Frame(dt);
                    if (bot.Itinerary!.Finished && t > 1) break;
                }
                bot.Release();
                Assert.True(bot.Itinerary!.Finished, bot.Report.ToMarkdown());
            });
        }
        finally
        {
            pack.Settle();
        }

        int spoken = 0, nobodys = 0;
        var missing = new SortedSet<string>(StringComparer.Ordinal);
        foreach (string entry in heard.Where(h => h.StartsWith("voice ", StringComparison.Ordinal) && h != "voice play" && h != "voice cut").Distinct())
        {
            string[] parts = entry.Split(' ');
            if (parts.Length < 3) continue;
            string key = parts[1], voice = parts[2];
            string lineKey = key.Contains('~') ? key[..key.IndexOf('~')] : key;
            if (voice == "-" || !catalogue.TryGetValue(lineKey, out var line) || line.Status != VoiceLine.StatusSpoken) { nobodys++; continue; }
            spoken++;
            if (pack.StateOf(key, line.En, voice).State != VoiceState.Current) missing.Add($"{key} ({voice})");
        }
        output.WriteLine($"{spoken} lines of the first chapter asked for a voice that the catalogue speaks; {nobodys} are nobody's or shown only; {missing.Count} have no current take.");
        Assert.True(spoken >= 60, $"only {spoken} spoken lines were asked for: the chapter did not play with the voices on");
        Assert.True(missing.Count == 0, "no current take for:\n" + string.Join("\n", missing.Take(40)));
    }
}
