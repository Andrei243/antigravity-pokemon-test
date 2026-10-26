using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace PokemonPlatinumEngine.Core.Diagnostics;

/// <summary>
/// What the game says of itself for a crash report: the screen it was on, where the player stood if a game was in
/// progress, the graphics card's name and the options as they were. Plain values, so the writer needs no window.
/// </summary>
/// <param name="SaveFile">The file the game in progress is saved in, by its name alone (<c>save2.json</c>, plan 12 · Q2); null with no game in progress.</param>
public sealed record GameFacts(string State, string? Map = null, string? Area = null, int? X = null, int? Y = null,
    string? Renderer = null, string? Settings = null, string? SaveFile = null);

/// <summary>Everything a crash report holds beside the exception.</summary>
public sealed record CrashFacts(DateTime When, string Build, string Os, string Runtime, string Arguments, GameFacts? Game, IReadOnlyList<string> Log)
{
    /// <summary>The facts of this process at this moment, with what the game says of itself.</summary>
    public static CrashFacts Now(GameFacts? game, string arguments) => new(
        WallClock.Local,
        GameVersion.Describe(),
        $"{RuntimeInformation.OSDescription} ({RuntimeInformation.OSArchitecture})",
        RuntimeInformation.FrameworkDescription,
        arguments,
        game,
        Diagnostics.Log.Tail());
}

/// <summary>What was left behind for the next start to tell (<see cref="CrashReport.TakePending"/>).</summary>
/// <param name="Report">The report's file, as the title names it: <c>logs/crash-20261012-194503.txt</c>.</param>
/// <param name="SaveKept">Whether the game as it stood was kept as <c>savegame.crash.json</c>.</param>
public sealed record CrashNote(string Report, bool SaveKept, DateTime When);

/// <summary>What handling a crash came to.</summary>
/// <param name="ReportPath">The report written, or null where none could be.</param>
/// <param name="SavePath">The crash save written, or null where there was no game in progress or it couldn't be written whole.</param>
/// <param name="SaveProblem">Why there is no crash save, in a few words; null where there is one.</param>
/// <param name="ReportFolder">The report's folder in <c>repros/</c> written beside it (plan 16 · T20), or null where the session wasn't recorded or it couldn't be written.</param>
public sealed record CrashOutcome(string? ReportPath, string? SavePath, string? SaveProblem, string? ReportFolder = null);

/// <summary>
/// The crash report (plan 16 · T3), written when the game stops on an exception nobody caught: a file in
/// <c>logs/</c> beside the save that says which build this was, on what machine, with what options, where the game
/// was and what went wrong, with the log's last lines; then the game as it stood, kept as
/// <c>savegame.crash.json</c> beside the save and never over it, written the way a save is (<see cref="SaveManager"/>)
/// and read back to make sure it is whole; then a note for the next start, whose title says what happened
/// (<c>TitleNotice.ForCrash</c>). No path in the report holds the name of the user's folder (<see cref="Scrub"/>).
/// Nothing leaves the machine.
/// <para>
/// What is kept: the newest <see cref="Keep"/> reports (older ones are deleted as a new one is written), and one
/// crash save with the one before it as <c>savegame.crash.bak</c> (the save's own way: a new crash save takes the
/// old one's place and the old one becomes the backup). To go on from a crash save, the player puts their slot's
/// save (<c>save1.json</c>) aside and renames the crash save in its place; nothing does that for them.
/// </para>
/// No drawing, no input: the writer takes plain values and an exception, so tests drive it with made-up ones.
/// </summary>
public static class CrashReport
{
    /// <summary>The folder of reports, beside the save.</summary>
    public const string FolderName = "logs";

    /// <summary>The game as it stood when it stopped, beside the save.</summary>
    public const string SaveName = "savegame.crash.json";

    /// <summary>The note a crash leaves for the next start, in the folder of reports.</summary>
    public const string PendingName = "crash.pending";

    /// <summary>How many reports are kept.</summary>
    public const int Keep = 10;

    /// <summary>The sections of a report, in its order.</summary>
    public static readonly string[] Sections = { "When", "Build", "Machine", "Started with", "Options", "Game", "What went wrong", "Log", "Crash save" };

    /// <summary>Where the saves are, and so where the reports and the crash save go: <see cref="SavePaths.Current"/>'s folder (plan 12 · Q2).</summary>
    public static string SaveFolder => SavePaths.Current.Folder;

    private static readonly JsonSerializerOptions NoteJson = new() { WriteIndented = true };

    // ------------------------------------------------------------------ the report's text

    /// <summary>
    /// The report as text, every section in <see cref="Sections"/> under its heading, scrubbed of the user's folder.
    /// </summary>
    /// <param name="save">What became of the crash save: its file's name, or why there is none.</param>
    public static string Compose(Exception exception, CrashFacts facts, string save)
    {
        var text = new StringBuilder();
        text.Append("Pokémon Platinum (a fan remake) stopped unexpectedly.\n");
        text.Append("This file stays on this computer. If you report the problem, it is the thing to send.\n");

        void Section(string name, string body)
        {
            text.Append("\n== ").Append(name).Append(" ==\n");
            text.Append(body.ReplaceLineEndings("\n").TrimEnd('\n')).Append('\n');
        }

        var game = facts.Game;
        Section(Sections[0], facts.When.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) + " (this computer's time)");
        Section(Sections[1], facts.Build);
        Section(Sections[2], $"System: {facts.Os}\nRuntime: {facts.Runtime}\nRenderer: {Or(game?.Renderer, "unknown (no window was open, or it couldn't be asked)")}");
        Section(Sections[3], facts.Arguments.Length > 0 ? facts.Arguments : "(no switches)");
        Section(Sections[4], Or(game?.Settings, "(not read yet)"));
        Section(Sections[5], game == null ? "(the game hadn't been made yet)"
            : $"Screen: {game.State}\n" + (game.Map == null ? "No game in progress."
                : $"Map: {game.Map}{(game.Area != null ? $" ({game.Area})" : "")}\nTile: {game.X}, {game.Y}"
                + (game.SaveFile != null ? $"\nSaved in: {game.SaveFile}" : "")));
        Section(Sections[6], Describe(exception));
        Section($"{Sections[7]} (its last {facts.Log.Count} lines)", facts.Log.Count > 0 ? string.Join("\n", facts.Log) : "(nothing was logged)");
        Section(Sections[8], save);
        return Scrub(text.ToString());
    }

    private static string Or(string? value, string otherwise) => string.IsNullOrWhiteSpace(value) ? otherwise : value;

    /// <summary>An exception with whatever it wraps, each with its type, its message and where it was thrown.</summary>
    public static string Describe(Exception exception)
    {
        var text = new StringBuilder();
        int depth = 0;
        for (Exception? e = exception; e != null && depth < 8; e = e.InnerException, depth++)
        {
            if (depth > 0) text.Append("\nCaused by:\n");
            text.Append(e.GetType().FullName).Append(": ").Append(e.Message).Append('\n');
            text.Append(string.IsNullOrEmpty(e.StackTrace) ? "   (no stack: it was never thrown)" : e.StackTrace).Append('\n');
        }
        return text.ToString();
    }

    private static readonly Regex WindowsHome = new(@"(?i)([A-Za-z]:[\\/]+(?:Users|Documents and Settings)[\\/]+)[^\\/\r\n""'<>|:*?]+", RegexOptions.Compiled);
    private static readonly Regex UnixHome = new(@"((?<![\w.])/(?:home|Users)/)[^/\s""':]+", RegexOptions.Compiled);

    /// <summary>
    /// A text without the name of anyone's folder: this user's home becomes <c>~</c>, and the name after
    /// <c>C:\Users\</c>, <c>/home/</c> or <c>/Users/</c> in any other path (a stack trace names the folders of the
    /// machine the game was built on) becomes <c>~</c> too.
    /// </summary>
    public static string Scrub(string text) => Scrub(text, Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));

    public static string Scrub(string text, string? home)
    {
        if (!string.IsNullOrEmpty(home) && home.Length > 3)
        {
            // The folder as it is written, and with the other kind of slash
            foreach (string form in new[] { home, home.Replace('\\', '/'), home.Replace('/', '\\') }.Distinct())
                text = text.Replace(form, "~", StringComparison.OrdinalIgnoreCase);
        }
        text = WindowsHome.Replace(text, "$1~");
        return UnixHome.Replace(text, "$1~");
    }

    // ------------------------------------------------------------------ writing it

    /// <summary>The report's file for a moment: <c>crash-20261012-194503.txt</c>.</summary>
    public static string FileNameAt(DateTime when) => $"crash-{when.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture)}.txt";

    /// <summary>
    /// Writes what a crash leaves, into the folder the save is in: the crash save first (so the report can say what
    /// became of it), then the report, then the note for the next start, and lets go of the reports beyond the newest
    /// <see cref="Keep"/>. Never throws: what couldn't be written is null in what it returns.
    /// </summary>
    /// <param name="save">The game as it stands, or null when no game is in progress. It may throw: a game caught in the middle of something may not be able to say what it holds.</param>
    public static CrashOutcome Write(string saveFolder, Exception exception, CrashFacts facts, Func<SaveData?>? save)
    {
        string? savePath = null, saveProblem;
        try
        {
            var data = save?.Invoke();
            if (data == null) saveProblem = "No game was in progress, so there was nothing to keep.";
            else
            {
                string path = Path.Combine(saveFolder, SaveName);
                // Written as a save is, through a temporary file, with the crash save before it kept as its backup;
                // and read back, because a save that doesn't read whole is worse than none
                if (SaveManager.SaveGame(data, path) && SaveManager.Load(path).Outcome == SaveLoad.Ok)
                {
                    savePath = path;
                    saveProblem = null;
                }
                else saveProblem = "The game as it stood couldn't be written whole, so nothing was kept.";
            }
        }
        catch (Exception e)
        {
            saveProblem = $"The game as it stood couldn't be put into a save ({e.GetType().Name}: {e.Message}), so nothing was kept.";
        }

        // The save the game was being played from is a slot's file (plan 12 · Q2), which the game names if it can
        string own = facts.Game?.SaveFile is { Length: > 0 } file ? $"your {file}" : $"your slot's save ({SavePaths.SlotName(1)} for slot 1)";
        string kept = savePath != null
            ? $"The game as it stood was kept as {SaveName}, beside your save and never over it.\n"
              + $"To go on from there, put {own} aside and give {SaveName} its name.\n"
              + "It was caught in the middle of whatever was happening, so your own save is the safer one."
            : saveProblem!;

        string? reportPath = null;
        try
        {
            string folder = Path.Combine(saveFolder, FolderName);
            Directory.CreateDirectory(folder);
            string name = FileNameAt(facts.When);
            // Two crashes in one second, or a clock a tool has fixed: the later one takes a number
            for (int n = 2; File.Exists(Path.Combine(folder, name)); n++)
                name = Path.GetFileNameWithoutExtension(FileNameAt(facts.When)) + $"-{n}.txt";
            string path = Path.Combine(folder, name);
            File.WriteAllText(path, Compose(exception, facts, kept).Replace("\n", Environment.NewLine), new UTF8Encoding(false));
            reportPath = path;
            Prune(folder, path);
            var note = new CrashNote($"{FolderName}/{name}", savePath != null, facts.When);
            File.WriteAllText(Path.Combine(folder, PendingName), JsonSerializer.Serialize(note, NoteJson));
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or NotSupportedException or System.Security.SecurityException)
        {
            // Nowhere to write: the console has been told, and there is nothing more to do
        }
        return new CrashOutcome(reportPath, savePath, saveProblem);
    }

    // The reports beyond the newest few are deleted, the one just written never
    private static void Prune(string folder, string justWritten)
    {
        var reports = Directory.GetFiles(folder, "crash-*.txt")
            .OrderByDescending(f => File.GetLastWriteTimeUtc(f)).ThenByDescending(f => f, StringComparer.Ordinal).ToList();
        reports.Remove(justWritten);
        foreach (string old in reports.Skip(Keep - 1))
        {
            try { File.Delete(old); }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }
        }
    }

    /// <summary>
    /// The note the last run left if it stopped on a crash, taken away so that it is told once; null when it didn't.
    /// </summary>
    public static CrashNote? TakePending(string saveFolder)
    {
        string path = Path.Combine(saveFolder, FolderName, PendingName);
        try
        {
            if (!File.Exists(path)) return null;
            string json = File.ReadAllText(path);
            File.Delete(path);
            return JsonSerializer.Deserialize<CrashNote>(json) is { Report.Length: > 0 } note ? note : null;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException or NotSupportedException)
        {
            // A note that can't be read is told as nothing, and not asked about again
            try { File.Delete(path); } catch (Exception f) when (f is IOException or UnauthorizedAccessException) { }
            return null;
        }
    }

    // ------------------------------------------------------------------ catching one

    private static readonly object Gate = new();
    private static Func<GameFacts?>? watchedGame;
    private static Func<SaveData?>? watchedSave;
    private static string watchedArguments = "";
    private static string? watchedFolder;
    private static bool watching, handled;

    /// <summary>What the last crash handled in this process came to; null before any.</summary>
    public static CrashOutcome? Last { get; private set; }

    /// <summary>
    /// From now until <see cref="StopWatching"/>, an exception nobody catches on any thread is written up before the
    /// process ends (<see cref="AppDomain.UnhandledException"/>), with what these two say of the game. The loop's own
    /// exceptions are handed to <see cref="Caught"/> by whoever catches them (<c>Program.Guarded</c>).
    /// </summary>
    /// <param name="saveFolder">Where the save is, and so where the crash is written; null for <see cref="SaveFolder"/>, the game's own.</param>
    public static void Watch(Func<GameFacts?> game, Func<SaveData?> save, string arguments, string? saveFolder = null)
    {
        lock (Gate)
        {
            watchedGame = game;
            watchedSave = save;
            watchedArguments = arguments;
            watchedFolder = saveFolder;
            handled = false;
            if (watching) return;
            watching = true;
            AppDomain.CurrentDomain.UnhandledException += OnUnhandled;
        }
    }

    public static void StopWatching()
    {
        lock (Gate)
        {
            if (watching) AppDomain.CurrentDomain.UnhandledException -= OnUnhandled;
            watching = false;
            watchedGame = null;
            watchedSave = null;
            ReportFolderWriter = null;
        }
    }

    /// <summary>
    /// Writes a report's folder for a crash (plan 16 · T20, <see cref="Diagnostics.ReportFolder"/>), given the save's
    /// folder and the exception, and gives its path: set, while watching, by whoever records the session
    /// (<c>GameEngine.WriteCrashFolder</c>), and let go by <see cref="StopWatching"/>. Null writes none.
    /// </summary>
    public static Func<string, Exception, string?>? ReportFolderWriter { get; set; }

    private static void OnUnhandled(object sender, UnhandledExceptionEventArgs e) =>
        Caught(e.ExceptionObject as Exception ?? new InvalidOperationException($"Something that is no exception was thrown: {e.ExceptionObject}"));

    /// <summary>
    /// An exception that ends the game: the log is told and flushed, and the report, the crash save and the note are
    /// written beside the save. Once only, however many threads fall over together; never throws.
    /// </summary>
    public static CrashOutcome? Caught(Exception exception)
    {
        // One at a time: a second thread falling over waits here until the first one's crash is written, so the
        // process doesn't end under it
        lock (Handling) return Handle(exception);
    }

    private static readonly object Handling = new();

    private static CrashOutcome? Handle(Exception exception)
    {
        Func<GameFacts?>? game;
        Func<SaveData?>? save;
        string arguments, folder;
        lock (Gate)
        {
            if (handled) return Last;
            handled = true;
            (game, save, arguments, folder) = (watchedGame, watchedSave, watchedArguments, watchedFolder ?? SaveFolder);
        }
        try
        {
            Diagnostics.Log.Error($"The game stopped unexpectedly: {exception.GetType().FullName}: {exception.Message}");
            GameFacts? facts = null;
            try { facts = game?.Invoke(); }
            catch (Exception e) { Diagnostics.Log.Info($"The game couldn't say where it was: {e.GetType().Name}: {e.Message}"); }

            var outcome = Write(folder, exception, CrashFacts.Now(facts, arguments), save);
            // The session's report beside it, which replays to the frame that threw (plan 16 · T20)
            string? reportFolder = null;
            try { reportFolder = ReportFolderWriter?.Invoke(folder, exception); }
            catch (Exception e) { Diagnostics.Log.Info($"The session's report couldn't be written: {e.GetType().Name}: {e.Message}"); }
            if (reportFolder != null)
            {
                outcome = outcome with { ReportFolder = reportFolder };
                string replay = $"{Environment.NewLine}== Replay =={Environment.NewLine}The session to this crash: {Diagnostics.ReportFolder.Shown(reportFolder)} (play it with --replay).{Environment.NewLine}";
                try { if (outcome.ReportPath != null) File.AppendAllText(outcome.ReportPath, replay, new UTF8Encoding(false)); }
                catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }
                Diagnostics.Log.Info($"The session to the crash was written to {Diagnostics.ReportFolder.Shown(reportFolder)}.");
            }
            // The log is told, and so is whoever started the game from a console
            string told = outcome.ReportPath != null ? $"A report was written to {FolderName}/{Path.GetFileName(outcome.ReportPath)}." : "No report could be written.";
            string kept = outcome.SavePath != null ? $"The game as it stood was kept as {SaveName}; your own save wasn't touched." : outcome.SaveProblem ?? "No crash save.";
            Diagnostics.Log.Info(told);
            Diagnostics.Log.Info(kept);
            Diagnostics.Log.Say(told, error: true);
            Diagnostics.Log.Say(kept, error: true);
            Diagnostics.Log.Flush();
            lock (Gate) Last = outcome;
            return outcome;
        }
        catch (Exception)
        {
            // The handler itself must never be the thing that fails
            return null;
        }
    }
}
