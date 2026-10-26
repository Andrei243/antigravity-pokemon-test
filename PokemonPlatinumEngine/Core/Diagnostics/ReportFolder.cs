using System.Globalization;
using System.Text;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Core.Diagnostics;

/// <summary>
/// A report of the session (plan 16 · T20): a folder in <c>repros/</c> beside the save, written by the Report key
/// (F8) or by a crash, that stands alone and replays the session to the frame it was written at. It holds the session's
/// recording from the title (<see cref="MovieName"/>, plan 16 · T19), the save the game was continued from as it was
/// when the session began (<see cref="StartName"/>; none for a game begun this session, whose start is the title's
/// files inside the recording), the options as they are now, the log, the picture (<see cref="ScreenName"/>, which the
/// engine adds once the frame is drawn; none from a crash or a game with no window) and <see cref="ReportName"/>: the
/// build, the data, the graphics card, the screen, the map and the tile, and how to play it back. Nothing leaves the
/// machine: the player hands the folder over. No drawing, no input: it takes plain values, so tests drive it.
/// </summary>
public static class ReportFolder
{
    /// <summary>The folder the reports go in, beside the save.</summary>
    public const string FolderName = "repros";

    public const string MovieName = "movie" + InputRecording.Extension;
    // (Written in two, or the text check would take the file's name for a key of the table's "start" section)
    public const string StartName = "start" + ".json";
    public const string SettingsName = "settings.json";
    public const string LogName = "log.txt";
    public const string ScreenName = "screen.png";
    public const string ReportName = "report.txt";

    /// <summary>The folder a replay of a report saves into, inside the report's own, never over the player's save.</summary>
    public const string ReplayedName = "replayed";

    /// <summary>The report's folder for a moment: <c>20261026-101500</c>, and <c>-crash</c> after it for a crash's.</summary>
    public static string NameAt(DateTime when, bool crash = false) =>
        when.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture) + (crash ? "-crash" : "");

    /// <summary>A report's folder as the toast names it: <c>repros/20261026-101500</c>.</summary>
    public static string Shown(string folder) => $"{FolderName}/{Path.GetFileName(Path.TrimEndingDirectorySeparator(folder))}";

    /// <summary>
    /// The save the game was continued from, as the session's start found it: the slot's file the game is saved in,
    /// out of the files the title read (<see cref="InputRecording.Saves"/>); null for a game begun this session.
    /// </summary>
    public static byte[]? StartSave(InputRecording recording, string? saveFile) =>
        saveFile != null && recording.Saves.TryGetValue(saveFile, out var bytes) ? bytes : null;

    /// <summary>
    /// Writes a report's folder and gives its path. Throws an <see cref="IOException"/> or an
    /// <see cref="UnauthorizedAccessException"/> where the folder can't be written; a crash's caller catches them.
    /// </summary>
    /// <param name="saveFolder">The folder the save is in (<see cref="SavePaths.Current"/>'s).</param>
    /// <param name="game">What the game says of itself (<c>GameEngine.DescribeForCrash</c>).</param>
    /// <param name="settings">The options as they are now (<see cref="GameSettings.ToFile"/>).</param>
    /// <param name="why">For a crash, the exception in a line; null for the Report key's.</param>
    public static string Write(string saveFolder, InputRecording recording, GameFacts game, string settings, DateTime when, string? why = null)
    {
        string root = Path.Combine(saveFolder, FolderName);
        string name = NameAt(when, why != null);
        string folder = Path.Combine(root, name);
        // Two reports in one second, or a clock a tool has fixed: the later one takes a number
        for (int n = 2; Directory.Exists(folder); n++) folder = Path.Combine(root, $"{name}-{n}");
        Directory.CreateDirectory(folder);

        recording.WriteTo(Path.Combine(folder, MovieName));
        byte[]? start = StartSave(recording, game.SaveFile);
        if (start != null) File.WriteAllBytes(Path.Combine(folder, StartName), start);
        File.WriteAllText(Path.Combine(folder, SettingsName), settings, new UTF8Encoding(false));
        File.WriteAllText(Path.Combine(folder, LogName), LogText(), new UTF8Encoding(false));
        File.WriteAllText(Path.Combine(folder, ReportName), Compose(recording, game, when, why, start != null, folder).Replace("\n", Environment.NewLine), new UTF8Encoding(false));
        return folder;
    }

    /// <summary>The log as it stands: the file the game writes (T3's), read beside its writer, or the lines kept in memory where there is none.</summary>
    public static string LogText()
    {
        Log.Flush();
        if (Log.Path is { } path && File.Exists(path))
        {
            try
            {
                using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                using var reader = new StreamReader(file, Encoding.UTF8);
                return reader.ReadToEnd();
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
        var tail = Log.Tail();
        return tail.Count == 0 ? "(nothing was logged)\n" : string.Join("\n", tail) + "\n";
    }

    /// <summary><c>report.txt</c>: what the folder is, the build, the game where it stood, and how to replay it; scrubbed of the user's folder.</summary>
    public static string Compose(InputRecording recording, GameFacts game, DateTime when, string? why, bool hasStart, string? folder = null)
    {
        var text = new StringBuilder();
        text.Append(why == null ? "A report of a session of Pokémon Platinum (a fan remake), written with the Report key.\n"
            : "A report of a session of Pokémon Platinum (a fan remake) that stopped on a crash.\n");
        text.Append("It stays on this computer. It replays the session from the title to the moment it was written.\n\n");
        text.Append($"Written: {when.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)} (this computer's time)\n");
        text.Append($"Build: {GameVersion.Describe()}\n");
        text.Append($"Data: {recording.DataVersion}\n");
        text.Append($"Renderer: {(string.IsNullOrWhiteSpace(game.Renderer) ? "unknown" : game.Renderer)}\n");
        text.Append($"Screen: {game.State}\n");
        text.Append(game.Map == null ? "No game in progress.\n"
            : $"Map: {game.Map}{(game.Area != null ? $" ({game.Area})" : "")}\nTile: {game.X}, {game.Y}\n");
        if (game.SaveFile != null)
            text.Append(hasStart ? $"Saved in: {game.SaveFile}, as it was when the session began: {StartName}\n"
                : $"Saved in: {game.SaveFile}, which the session began without (a new game)\n");
        text.Append($"Recording: {recording.Frames.Count} frames, {recording.Seconds.ToString("0.0", CultureInfo.InvariantCulture)} s, seed {recording.Seed}\n");
        if (why != null) text.Append($"What went wrong: {why}\n");
        text.Append("\nTo play it back:\n");
        string at = folder != null ? $"\"{folder}\"" : "<this folder>";
        text.Append($"  in the game:     {CommandLine.Executable} --replay {at}\n");
        text.Append($"  in the harness:  dotnet run --project tools/ShotHarness -- <out dir> replay {at} [every <seconds>] [from <seconds>]\n");
        return CrashReport.Scrub(text.ToString());
    }

    /// <summary>The recording a report's folder holds; <paramref name="path"/> may also name a recording's file itself.</summary>
    public static string MovieIn(string path) =>
        File.Exists(path) ? path : Path.Combine(path, MovieName);
}
