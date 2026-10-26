using System.Globalization;
using System.Text;
using PokemonPlatinumEngine.Core.Diagnostics;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// What the game was started with (plan 16 · T3): the switches of the command line as plain values, read once in
/// <c>Program.Main</c>. A switch that isn't given leaves its value as it is here, which is the game as it always
/// started. A later session's switch is a value here and a row of <see cref="CommandLine.Switches"/>.
/// </summary>
public sealed record LaunchOptions
{
    /// <summary><c>--help</c>: print the switches and exit.</summary>
    public bool Help { get; init; }

    /// <summary><c>--region Kanto</c>: new games start in this region instead of Sinnoh, for testing.</summary>
    public string? Region { get; init; }

    /// <summary><c>--windowed</c> and <c>--fullscreen</c>: how the window opens this time, whatever the options say.</summary>
    public bool Windowed { get; init; }
    public bool Fullscreen { get; init; }

    /// <summary>
    /// <c>--safe-mode</c>: the plainest start, for a game that won't start as it is set up. The Low preset in a
    /// 1280×720 window for this run (<see cref="ApplyTo"/>), nothing read from the mesh cache and nothing from
    /// <c>overrides/</c> (<c>Program.Main</c> switches those off); packs, once plan 16 · T14 brings them, stay off too.
    /// </summary>
    public bool SafeMode { get; init; }

    /// <summary><c>--verify</c>: check the game's files against <c>manifest.sha256</c> and exit (<see cref="Manifest"/>).</summary>
    public bool Verify { get; init; }

    /// <summary><c>--log &lt;path&gt;</c>: where the log is written, in place of <c>log.txt</c> beside the save.</summary>
    public string? LogPath { get; init; }

    /// <summary>
    /// <c>--dev</c>: the developer's surface (plan 16's "One build, two faces"): plan 16 · T6's console and live reload
    /// (<see cref="DevMode"/>), turned on by <c>Program.Main</c>.
    /// </summary>
    public bool Dev { get; init; }

    /// <summary>
    /// <c>--crash-after &lt;frames&gt;</c>, which <c>--help</c> doesn't list: the game throws on purpose once it has
    /// drawn that many frames, so the crash handler can be seen at work on the real executable.
    /// </summary>
    public int? CrashAfter { get; init; }

    /// <summary>
    /// <c>--portable</c> (plan 12 · Q2): the saves, the options and the log are kept in the folder the game was
    /// started in, whatever else <see cref="SavePaths.Resolve"/> would choose.
    /// </summary>
    public bool Portable { get; init; }

    /// <summary><c>--save &lt;folder&gt;</c> (plan 12 · Q2): they are kept in this folder, which wins over everything.</summary>
    public string? SaveFolder { get; init; }

    /// <summary><c>--where</c> (plan 12 · Q2): say where they are kept, and why, and exit: nothing is opened, made or moved.</summary>
    public bool Where { get; init; }

    /// <summary>
    /// <c>--record &lt;file&gt;</c> (plan 16 · T19): the session, which the game always records in memory from the title,
    /// is written to this file as the game ends or crashes.
    /// </summary>
    public string? RecordPath { get; init; }

    /// <summary>What was wrong with the command line, a line to each mistake; empty when nothing was.</summary>
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();

    /// <summary>The command line as it was given, for the log and a crash report.</summary>
    public string Given { get; init; } = "";

    public bool IsValid => Errors.Count == 0;

    /// <summary>
    /// What the switches change of the options, for this run alone (<see cref="GameSettings.SetForThisRun"/>): a
    /// setting changed here is written to <c>settings.json</c> as it was before, unless the player then changes it.
    /// </summary>
    public void ApplyTo(GameSettings settings)
    {
        if (SafeMode)
        {
            settings.SetForThisRun(nameof(GameSettings.Quality), GraphicsQuality.Low);
            settings.SetForThisRun(nameof(GameSettings.WindowWidth), GameSettings.WindowSizes[0].Width);
            settings.SetForThisRun(nameof(GameSettings.WindowHeight), GameSettings.WindowSizes[0].Height);
        }
        if (SafeMode || Windowed) settings.SetForThisRun(nameof(GameSettings.Fullscreen), false);
        if (Fullscreen) settings.SetForThisRun(nameof(GameSettings.Fullscreen), true);
    }
}

/// <summary>A switch of the command line: a row of <see cref="CommandLine.Switches"/>.</summary>
/// <param name="Name">How it is written: <c>--region</c>.</param>
/// <param name="Value">What its value is called in <c>--help</c> (<c>name</c> for <c>--region &lt;name&gt;</c>), or null for a switch that takes none.</param>
/// <param name="Help">What <c>--help</c> says of it: what it does today, in a sentence or two.</param>
/// <param name="Set">Puts it into the options; the value is null for a switch that takes none.</param>
public sealed record Switch(string Name, string? Value, string Help, Func<LaunchOptions, string?, LaunchOptions> Set)
{
    /// <summary>Other ways of writing it (<c>-h</c> for <c>--help</c>).</summary>
    public IReadOnlyList<string> Aliases { get; init; } = Array.Empty<string>();

    /// <summary>What is wrong with a value, or null when nothing is.</summary>
    public Func<string, string?>? Check { get; init; }

    /// <summary>Left out of <c>--help</c>: a switch for testing the game itself.</summary>
    public bool Hidden { get; init; }

    /// <summary>The switch as <c>--help</c> writes it: <c>--region &lt;name&gt;</c>.</summary>
    public string Form => Value == null ? Name : $"{Name} <{Value}>";
}

/// <summary>
/// The command line (plan 16 · T3): a table of switches, read once into a <see cref="LaunchOptions"/>. A session
/// that adds a switch adds a value to <see cref="LaunchOptions"/> and a row to <see cref="Switches"/>, at its end;
/// <see cref="Parse"/> and <see cref="Usage"/> (what <c>--help</c> prints) both go by the table, so the row is all
/// there is to write. A value follows its switch as the next word (<c>--region Sinnoh</c>) or after an equals sign
/// (<c>--region=Sinnoh</c>). Anything the table doesn't know, a value that is missing or wrong, and switches that
/// can't go together are mistakes: the game says so and doesn't start (<c>Program.Main</c> exits with
/// <see cref="BadCommandLine"/>). No drawing, no input, no window.
/// </summary>
public static class CommandLine
{
    /// <summary>What the game exits with: a run that ended as the player chose, one that stopped on a crash, and a command line it couldn't read. <c>--verify</c> has its own (<see cref="Manifest"/>).</summary>
    public const int Ended = 0, Crashed = 1, BadCommandLine = 2;

    /// <summary>The name the usage calls the game by: the executable's.</summary>
    public const string Executable = "PokemonPlatinumEngine";

    public static readonly IReadOnlyList<Switch> Switches = new Switch[]
    {
        new("--help", null, "Show this and exit.", (o, _) => o with { Help = true }) { Aliases = new[] { "-h", "-?", "/?" } },
        new("--region", "name", $"New games start in this region instead of Sinnoh, for testing: {string.Join(", ", RegionDatabase.All.Select(r => r.Id))}.",
            (o, v) => o with { Region = v })
        {
            Check = v => RegionDatabase.Get(v) == null ? $"There is no region '{v}'. The regions are {string.Join(", ", RegionDatabase.All.Select(r => r.Id))}." : null
        },
        new("--windowed", null, "Open in a window this time, whatever the options say.", (o, _) => o with { Windowed = true }),
        new("--fullscreen", null, "Open full screen this time, whatever the options say.", (o, _) => o with { Fullscreen = true }),
        new("--safe-mode", null, "Start as plainly as the game can, this time: the Low preset in a 1280x720 window, every model meshed afresh instead of read from the cache, "
            + "and nothing read from overrides/ (packs, once there are any, will stay off too). Slow to start. The options are saved as they were unless you change them.",
            (o, _) => o with { SafeMode = true }),
        new("--verify", null, $"Check the game's files against {Manifest.FileName} and exit: {Manifest.AsShipped} when all are as shipped, {Manifest.Differs} when a file is missing or changed, "
            + $"{Manifest.NoManifest} when there is no {Manifest.FileName} (only a release build has one; none is made yet).",
            (o, _) => o with { Verify = true }),
        new("--log", "path", $"Write the log to this file instead of {Log.FileName} beside the save.", (o, v) => o with { LogPath = v }),
        new("--dev", null, "Developer mode: the console on the grave key (` - help lists its commands), and the scripts, overlays, rooms, songs and model files "
            + "reloaded as they are saved, read from the repository's data when the game runs from it.", (o, _) => o with { Dev = true }),
        new("--crash-after", "frames", "Throw on purpose after that many frames, to see the crash handler at work.",
            (o, v) => o with { CrashAfter = int.Parse(v!, CultureInfo.InvariantCulture) })
        {
            Hidden = true,
            Check = v => int.TryParse(v, NumberStyles.None, CultureInfo.InvariantCulture, out int n) && n > 0 ? null : $"--crash-after takes a number of frames, 1 or more, not '{v}'."
        },
        new("--portable", null, $"Keep the saves, the options and the log in the folder the game is started in, instead of your own folder for the game. "
            + $"An empty {SavePaths.PortableMarker} beside the game does the same every time.",
            (o, _) => o with { Portable = true }),
        new("--save", "folder", "Keep the saves, the options and the log in this folder.", (o, v) => o with { SaveFolder = v })
        {
            Check = v => v.IndexOfAny(Path.GetInvalidPathChars()) >= 0 ? $"--save takes a folder, and '{v}' can't be one." : null
        },
        new("--where", null, "Say where the saves, the options and the log are kept, and why, and exit. Nothing is opened, made or moved.",
            (o, _) => o with { Where = true }),
        new("--record", "file", "Write this session's recording (every frame's keys and clock, kept from the title) to this file as the game ends or crashes, to be played back.", (o, v) => o with { RecordPath = v }) { Check = v => v.IndexOfAny(Path.GetInvalidPathChars()) >= 0 ? $"--record takes a file, and '{v}' can't be one." : null }
    };

    /// <summary>
    /// What the running game was started with. <c>Program.Main</c> sets it once it has read the command line; a
    /// tool or a test that makes an engine of its own leaves it as it is: nothing given.
    /// </summary>
    public static LaunchOptions Launched { get; set; } = new();

    /// <summary>The switches read into their values, with every mistake in <see cref="LaunchOptions.Errors"/>.</summary>
    public static LaunchOptions Parse(IReadOnlyList<string> args)
    {
        var options = new LaunchOptions { Given = string.Join(' ', args.Select(a => a.Contains(' ') ? $"\"{a}\"" : a)) };
        var errors = new List<string>();
        var valued = new HashSet<string>();
        for (int i = 0; i < args.Count; i++)
        {
            string word = args[i], name = word;
            string? value = null;
            int equals = word.StartsWith("--", StringComparison.Ordinal) ? word.IndexOf('=') : -1;
            if (equals > 0) (name, value) = (word[..equals], word[(equals + 1)..]);

            var row = Switches.FirstOrDefault(s => s.Name == name || s.Aliases.Contains(name));
            if (row == null)
            {
                errors.Add(word.StartsWith('-') || word.StartsWith('/') ? $"There is no switch {name}." : $"'{word}' is no switch: the game takes switches only.");
                continue;
            }
            if (row.Value == null)
            {
                if (value != null) errors.Add($"{row.Name} takes no value.");
                else options = row.Set(options, null);
                continue;
            }
            // A value is the next word, unless that is a switch itself
            if (value == null && i + 1 < args.Count && !args[i + 1].StartsWith("--", StringComparison.Ordinal)) value = args[++i];
            if (string.IsNullOrWhiteSpace(value)) errors.Add($"{row.Name} needs a value: {row.Form}.");
            else if (!valued.Add(row.Name)) errors.Add($"{row.Name} is given twice.");
            else if (row.Check?.Invoke(value) is { } wrong) errors.Add(wrong);
            else options = row.Set(options, value);
        }
        // What can't go together
        if (options.Windowed && options.Fullscreen) errors.Add("--windowed and --fullscreen can't both be given.");
        if (options.SafeMode && options.Fullscreen) errors.Add("--safe-mode opens in a window: it can't be given with --fullscreen.");
        if (options.Portable && options.SaveFolder != null) errors.Add("--portable and --save can't both be given: each says where the saves are kept.");
        return options with { Errors = errors };
    }

    /// <summary>What <c>--help</c> prints: the build, then a row to each switch of the table that isn't hidden.</summary>
    public static string Usage(int width = 100)
    {
        var shown = Switches.Where(s => !s.Hidden).ToList();
        int column = shown.Max(s => s.Form.Length) + 4;
        var text = new StringBuilder();
        // (Plain letters only: a console that isn't set to Unicode would make something else of an accent)
        text.Append($"Pokemon Platinum (a fan remake) {GameVersion.Describe()}\n\n");
        text.Append($"  {Executable} [switches]\n\n");
        foreach (var row in shown)
        {
            var lines = WrapWords(row.Help, Math.Max(30, width - column));
            for (int i = 0; i < lines.Count; i++)
                text.Append("  ").Append((i == 0 ? row.Form : "").PadRight(column - 2)).Append(lines[i]).Append('\n');
        }
        text.Append('\n');
        text.AppendJoin('\n', WrapWords($"The game exits with {Ended} when it ends as you chose, {Crashed} when it stops on a crash (a report is written to "
            + $"{CrashReport.FolderName}/ beside the save) and {BadCommandLine} when it can't read its command line.", width));
        return text.ToString();
    }

    private static List<string> WrapWords(string text, int width)
    {
        var lines = new List<string>();
        var line = new StringBuilder();
        foreach (string word in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (line.Length > 0 && line.Length + 1 + word.Length > width)
            {
                lines.Add(line.ToString());
                line.Clear();
            }
            if (line.Length > 0) line.Append(' ');
            line.Append(word);
        }
        if (line.Length > 0 || lines.Count == 0) lines.Add(line.ToString());
        return lines;
    }
}
