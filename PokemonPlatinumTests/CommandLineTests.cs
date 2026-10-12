using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Core.Diagnostics;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumTests;

/// <summary>
/// The command line (plan 16 · T3): every switch of the table read into its value, bad input refused with a line
/// that says what was wrong, <c>--help</c> printed from the same table, what the switches change of the options for
/// one run and never of the file, and <c>--verify</c> against a manifest. No window, and nothing static is set.
/// </summary>
public class CommandLineTests
{
    private static LaunchOptions Parse(params string[] args) => CommandLine.Parse(args);

    // ------------------------------------------------------------------ the switches

    [Fact]
    public void NoSwitchIsTheGameAsItAlwaysStarted()
    {
        var launch = Parse();
        Assert.True(launch.IsValid);
        Assert.Equal(new LaunchOptions() with { Errors = launch.Errors }, launch);
        Assert.False(launch.Help || launch.Windowed || launch.Fullscreen || launch.SafeMode || launch.Verify || launch.Dev);
        Assert.Null(launch.Region);
        Assert.Null(launch.LogPath);
        Assert.Null(launch.CrashAfter);

        // And it changes nothing of the options
        var settings = new GameSettings { Quality = GraphicsQuality.Medium, Fullscreen = true, WindowWidth = 2560, WindowHeight = 1440 };
        string before = settings.Describe();
        launch.ApplyTo(settings);
        Assert.Equal(before, settings.Describe());
        // What a tool or a test starts with is the same: nothing given
        Assert.Equal("", new LaunchOptions().Given);
    }

    [Fact]
    public void EverySwitchOfTheTableIsRead()
    {
        Assert.True(Parse("--help").Help);
        Assert.True(Parse("-h").Help);
        Assert.True(Parse("-?").Help);
        Assert.True(Parse("/?").Help);
        Assert.Equal("Sinnoh", Parse("--region", "Sinnoh").Region);
        Assert.True(Parse("--windowed").Windowed);
        Assert.True(Parse("--fullscreen").Fullscreen);
        Assert.True(Parse("--safe-mode").SafeMode);
        Assert.True(Parse("--verify").Verify);
        Assert.Equal("somewhere/else.txt", Parse("--log", "somewhere/else.txt").LogPath);
        Assert.True(Parse("--dev").Dev);
        Assert.Equal(120, Parse("--crash-after", "120").CrashAfter);

        // Each row of the table is one of those: a switch a later session adds must be read here too
        var tried = new[] { "--help", "--region", "--windowed", "--fullscreen", "--safe-mode", "--verify", "--log", "--dev", "--crash-after" };
        Assert.Equal(tried, CommandLine.Switches.Select(s => s.Name).Take(tried.Length));
        foreach (var row in CommandLine.Switches)
        {
            // Given as it is written, with a value where it takes one, every switch changes something and is no mistake
            string value = row.Name switch { "--region" => "Sinnoh", "--crash-after" => "3", _ => "x" };
            var launch = row.Value == null ? Parse(row.Name) : Parse(row.Name, value);
            Assert.True(launch.IsValid, $"{row.Name}: {string.Join(" ", launch.Errors)}");
            Assert.NotEqual(new LaunchOptions() with { Errors = launch.Errors, Given = launch.Given }, launch);
            Assert.False(string.IsNullOrWhiteSpace(row.Help), $"{row.Name} says nothing of itself");
        }
    }

    [Fact]
    public void SeveralSwitchesGoTogetherAndAValueMayFollowAnEqualsSign()
    {
        var launch = Parse("--region=sinnoh", "--windowed", "--dev", "--log=out/game log.txt");
        Assert.True(launch.IsValid, string.Join(" ", launch.Errors));
        Assert.Equal(("sinnoh", true, true, "out/game log.txt"), (launch.Region, launch.Windowed, launch.Dev, launch.LogPath));
        // The region is found whatever its case, as the engine finds it
        Assert.Equal(RegionDatabase.Sinnoh, RegionDatabase.Get(launch.Region!)!.Id);
        // What was given is kept for the log, a word with a space in it between quotes
        Assert.Equal("--region=sinnoh --windowed --dev \"--log=out/game log.txt\"", launch.Given);
    }

    [Theory]
    [InlineData("There is no switch --regoin.", "--regoin", "Sinnoh")]
    [InlineData("'Sinnoh' is no switch", "--regoin", "Sinnoh")]
    [InlineData("'play' is no switch", "play")]
    [InlineData("There is no switch -x.", "-x")]
    [InlineData("--region needs a value: --region <name>.", "--region")]
    [InlineData("--region needs a value", "--region", "--windowed")]
    [InlineData("--region needs a value", "--region=")]
    [InlineData("There is no region 'Atlantis'", "--region", "Atlantis")]
    [InlineData("--region is given twice.", "--region", "Sinnoh", "--region", "Kanto")]
    [InlineData("--log needs a value: --log <path>.", "--log")]
    [InlineData("--windowed takes no value.", "--windowed=yes")]
    [InlineData("--windowed and --fullscreen can't both be given.", "--windowed", "--fullscreen")]
    [InlineData("--safe-mode opens in a window", "--safe-mode", "--fullscreen")]
    [InlineData("--crash-after takes a number of frames", "--crash-after", "soon")]
    [InlineData("--crash-after takes a number of frames", "--crash-after", "0")]
    [InlineData("--crash-after takes a number of frames", "--crash-after", "-5")]
    public void BadInputIsRefusedWithALineThatSaysWhy(string says, params string[] args)
    {
        var launch = Parse(args);
        Assert.False(launch.IsValid);
        Assert.Contains(launch.Errors, e => e.Contains(says));
    }

    [Fact]
    public void EveryMistakeIsToldAtOnce()
    {
        var launch = Parse("--region", "Atlantis", "--windowed", "--fullscreen", "extra", "--frobnicate");
        Assert.Equal(4, launch.Errors.Count);
        // What could be read was read all the same
        Assert.True(launch.Windowed && launch.Fullscreen);
        Assert.Null(launch.Region);
    }

    [Fact]
    public void HelpListsEverySwitchOfTheTableButTheHiddenOnes()
    {
        string usage = CommandLine.Usage();
        foreach (var row in CommandLine.Switches)
        {
            if (row.Hidden) Assert.DoesNotContain(row.Name, usage);
            else
            {
                Assert.Contains(row.Form, usage);
                // Its help is there too, however the lines broke: its first words
                Assert.Contains(string.Join(' ', row.Help.Split(' ').Take(3)), usage);
            }
        }
        Assert.Contains("--region <name>", usage);
        Assert.Contains("--log <path>", usage);
        Assert.Contains(RegionDatabase.Sinnoh, usage);
        Assert.Contains(GameVersion.Version, usage);
        // The switches whose other half isn't built yet say so plainly
        Assert.Contains("none is made yet", CommandLine.Switches.Single(s => s.Name == "--verify").Help);
        Assert.Contains("Nothing reads it yet", CommandLine.Switches.Single(s => s.Name == "--dev").Help);
        Assert.Contains("packs", CommandLine.Switches.Single(s => s.Name == "--safe-mode").Help);
        // A console shows it whole and unbroken: no line wider than a hundred, and nothing but plain letters
        Assert.All(usage.Split('\n'), line => Assert.True(line.Length <= 100, line));
        Assert.All(usage, c => Assert.True(c is '\n' or (>= ' ' and <= '~'), $"'{c}' is no plain letter"));
        Assert.All(CommandLine.Switches, row => Assert.StartsWith("--", row.Name));
        Assert.Equal(CommandLine.Switches.Count, CommandLine.Switches.Select(s => s.Name).Concat(CommandLine.Switches.SelectMany(s => s.Aliases)).Distinct().Count()
            - CommandLine.Switches.Sum(s => s.Aliases.Count));
    }

    [Fact]
    public void TheExitCodesAreApart()
    {
        var codes = new[] { CommandLine.Ended, CommandLine.Crashed, CommandLine.BadCommandLine, Manifest.Differs, Manifest.NoManifest };
        Assert.Equal(codes.Length, codes.Distinct().Count());
        Assert.Equal(0, CommandLine.Ended);
        Assert.Equal(Manifest.AsShipped, CommandLine.Ended);
    }

    // ------------------------------------------------------------------ what a switch changes of the options, for one run

    private static GameSettings Read(string json) =>
        JsonSerializer.Deserialize<GameSettings>(json, new JsonSerializerOptions { Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() } })!;

    [Fact]
    public void SafeModeIsTheLowPresetInTheSmallestWindowForThisRunAndTheFileKeepsWhatThePlayerChose()
    {
        var settings = new GameSettings { Quality = GraphicsQuality.High, Fullscreen = true, WindowWidth = 2560, WindowHeight = 1440, Muted = false };
        Parse("--safe-mode").ApplyTo(settings);
        Assert.Equal((GraphicsQuality.Low, false, 1280, 720), (settings.Quality, settings.Fullscreen, settings.WindowWidth, settings.WindowHeight));

        // Whatever is saved meanwhile (the M key saves the options) is saved as the player left it
        settings.Muted = true;
        var file = Read(settings.ToFile());
        Assert.Equal((GraphicsQuality.High, true, 2560, 1440, true), (file.Quality, file.Fullscreen, file.WindowWidth, file.WindowHeight, file.Muted));
        // ... and the run itself is still the safe one
        Assert.Equal((GraphicsQuality.Low, false, 1280, 720), (settings.Quality, settings.Fullscreen, settings.WindowWidth, settings.WindowHeight));

        // A setting the player changes in that run is theirs from then on, whatever they change it to; the others stay as they were
        settings.Quality = GraphicsQuality.Medium;
        file = Read(settings.ToFile());
        Assert.Equal((GraphicsQuality.Medium, true, 2560, 1440), (file.Quality, file.Fullscreen, file.WindowWidth, file.WindowHeight));
        settings.Quality = GraphicsQuality.Low;
        Assert.Equal(GraphicsQuality.Low, Read(settings.ToFile()).Quality);
        Assert.True(Read(settings.ToFile()).Fullscreen);
    }

    [Fact]
    public void WindowedAndFullscreenAreForThisRunToo()
    {
        var full = new GameSettings { Fullscreen = true };
        Parse("--windowed").ApplyTo(full);
        Assert.False(full.Fullscreen);
        Assert.True(Read(full.ToFile()).Fullscreen);

        var windowed = new GameSettings { Fullscreen = false };
        Parse("--fullscreen").ApplyTo(windowed);
        Assert.True(windowed.Fullscreen);
        Assert.False(Read(windowed.ToFile()).Fullscreen);
        // F11 in that run turns it off: that is the player's doing, and it is saved
        windowed.Fullscreen = false;
        Assert.False(Read(windowed.ToFile()).Fullscreen);
        windowed.Fullscreen = true;
        Assert.True(Read(windowed.ToFile()).Fullscreen);

        // Safe mode with --windowed is still what the player had before both
        var both = new GameSettings { Fullscreen = true, Quality = GraphicsQuality.Medium };
        Parse("--safe-mode", "--windowed").ApplyTo(both);
        Assert.False(both.Fullscreen);
        Assert.Equal((true, GraphicsQuality.Medium), (Read(both.ToFile()).Fullscreen, Read(both.ToFile()).Quality));

        Assert.Throws<ArgumentException>(() => new GameSettings().SetForThisRun("NoSuchSetting", 1));
    }

    [Fact]
    public void WithNoSwitchTheFileIsTheOptionsAsTheyAre()
    {
        var settings = new GameSettings { Quality = GraphicsQuality.Low, FrameCap = 120, FocusPause = true };
        Assert.Equal(settings.Describe(), settings.ToFile());
        string path = Path.Combine(Path.GetTempPath(), $"pokemon-settings-{Guid.NewGuid():N}.json");
        try
        {
            settings.Save(path);
            var read = GameSettings.Load(path);
            Assert.Equal((GraphicsQuality.Low, 120, true), (read.Quality, read.FrameCap, read.FocusPause));
            Assert.False(read.FirstRun);
        }
        finally
        {
            File.Delete(path);
        }
    }

    // ------------------------------------------------------------------ --verify

    [Fact]
    public void WithNoManifestVerifySaysSoAndIsNeitherAPassNorADifference()
    {
        string folder = Directory.CreateTempSubdirectory("pokemon-verify-").FullName;
        try
        {
            var none = Manifest.Verify(folder);
            Assert.Equal(Manifest.NoManifest, none.Code);
            Assert.Contains(Manifest.FileName, none.Lines[0]);
            Assert.Contains("release", string.Join(" ", none.Lines));
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    [Fact]
    public void VerifyHoldsEachFileOfTheManifestAgainstItsHash()
    {
        string folder = Directory.CreateTempSubdirectory("pokemon-verify-").FullName;
        try
        {
            Directory.CreateDirectory(Path.Combine(folder, "Data", "maps"));
            File.WriteAllText(Path.Combine(folder, "Data", "species.json"), "{ \"species\": [] }");
            File.WriteAllText(Path.Combine(folder, "Data", "maps", "Room.json"), "{}");
            File.WriteAllText(Path.Combine(folder, "savegame.json"), "the player's own, which no manifest names");
            string manifest = Path.Combine(folder, Manifest.FileName);
            // As sha256sum writes it: a comment and a blank line say nothing, an asterisk marks a binary file
            File.WriteAllLines(manifest, new[]
            {
                "# Pokémon Platinum (a fan remake) 0.1.0", "",
                Manifest.LineFor(folder, "Data/species.json"),
                Manifest.LineFor(folder, Path.Combine("Data", "maps", "Room.json")).Replace("  ", " *")
            });
            Assert.EndsWith("  Data/species.json", Manifest.LineFor(folder, "Data/species.json"));
            // (known: the SHA-256 of nothing)
            File.WriteAllText(Path.Combine(folder, "empty"), "");
            Assert.Equal("e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855", Manifest.HashOf(Path.Combine(folder, "empty")));

            var whole = Manifest.Verify(folder);
            Assert.Equal(Manifest.AsShipped, whole.Code);
            Assert.Equal("2 files checked against manifest.sha256: all as shipped.", Assert.Single(whole.Lines));

            // A file changed, a file gone, a line that is no line, a file outside the game's folder
            File.AppendAllText(Path.Combine(folder, "Data", "species.json"), " ");
            File.Delete(Path.Combine(folder, "Data", "maps", "Room.json"));
            File.AppendAllLines(manifest, new[] { "not a line of a manifest", new string('0', 64) + "  ../elsewhere.txt" });
            var broken = Manifest.Verify(folder);
            Assert.Equal(Manifest.Differs, broken.Code);
            Assert.Contains(broken.Lines, l => l.StartsWith("changed") && l.EndsWith("Data/species.json"));
            Assert.Contains(broken.Lines, l => l.StartsWith("missing") && l.EndsWith("Data/maps/Room.json"));
            Assert.Contains(broken.Lines, l => l.StartsWith("unreadable") && l.Contains("line 5"));
            Assert.Contains(broken.Lines, l => l.StartsWith("outside") && l.EndsWith("../elsewhere.txt"));
            Assert.Equal("3 files checked against manifest.sha256: 4 not as shipped.", broken.Lines[^1]);
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    [Theory]
    [InlineData("0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef  Data/a.json", true)]
    [InlineData("0123456789ABCDEF0123456789abcdef0123456789abcdef0123456789abcdef *Assets/b.png", true)]
    [InlineData("0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef Data/a.json", false)]   // one space
    [InlineData("0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdeg  Data/a.json", false)]  // no hash
    [InlineData("0123456789abcdef  Data/a.json", false)]
    [InlineData("0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef  ", false)]
    public void AManifestLineIsAHashTwoSpacesAndAFile(string line, bool reads)
    {
        Assert.Equal(reads, Manifest.TryParse(line, out string hash, out string file));
        if (reads) Assert.Equal((64, true), (hash.Length, file.Contains('/')));
    }
}
