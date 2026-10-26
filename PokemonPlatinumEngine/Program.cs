using System;
using System.Runtime.InteropServices;
using Raylib_cs;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Core.Diagnostics;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumEngine;

public static class Program
{
    /// <summary>
    /// The game: the command line read once (<see cref="CommandLine"/>), the switches that only print and exit, then
    /// the log opened and the game played inside the crash handler (<see cref="Guarded"/>). What it returns is the
    /// process's exit code: <see cref="CommandLine.Ended"/>, <see cref="CommandLine.Crashed"/>,
    /// <see cref="CommandLine.BadCommandLine"/>, or <c>--verify</c>'s own.
    /// </summary>
    public static int Main(string[] args)
    {
        var launch = CommandLine.Parse(args);
        if (!launch.IsValid)
        {
            foreach (string error in launch.Errors) Log.Say(error, error: true);
            Log.Say($"{CommandLine.Executable} --help lists the switches.", error: true);
            return CommandLine.BadCommandLine;
        }
        if (launch.Help)
        {
            Log.Say(CommandLine.Usage());
            return CommandLine.Ended;
        }
        if (launch.Verify)
        {
            var verified = Manifest.Verify(AppContext.BaseDirectory);
            foreach (string line in verified.Lines) Log.Say(line);
            return verified.Code;
        }

        if (launch.ValidatePacks)
        {
            // Every pack beside the game and the saves held to what the game can read (plan 16 · T15); nothing is put in use
            return PokemonPlatinumEngine.Data.PackCheck.CheckAll(PokemonPlatinumEngine.Data.DataLayers.PackFolders(Locate(launch).Folder), line => Log.Say(line));
        }

        if (launch.Where)
        {
            // Where the saves would be kept, and nothing else: no folder is made and no file opened
            Log.Say(Locate(launch).Describe());
            return CommandLine.Ended;
        }

        CommandLine.Launched = launch;
        // Where the saves, the options, the log and a crash's files are kept (plan 12 · Q2): decided once, here, for
        // the whole run. This is the only place that asks the real machine, so nothing but the game itself ever
        // writes in a user's own folder
        var paths = Settle(Locate(launch));
        SavePaths.Current = paths;
        // The log is beside the saves unless --log names another place; a run begins with what it is and where it runs
        string log = launch.LogPath ?? paths.LogFile;
        if (!Log.Open(log)) Log.Say($"The log couldn't be opened at {log}: this run keeps none.", error: true);
        Log.Heading($"==== Pokémon Platinum (a fan remake) {GameVersion.Describe()}, started {WallClock.Local.ToString("yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture)} ====");
        Log.Info($"Started with: {(launch.Given.Length > 0 ? launch.Given : "(no switches)")}");
        Log.Info($"The saves are kept in {paths.Folder}. {paths.Why}");
        Log.Info($"System: {RuntimeInformation.OSDescription} ({RuntimeInformation.OSArchitecture}), {RuntimeInformation.FrameworkDescription}");

        GameEngine? engine = null;
        int code = Guarded(() => engine, launch.Given, () => Play(launch, made => engine = made));
        if (code == CommandLine.Ended && playCode is { } own) code = own;
        Log.Info(code == CommandLine.Crashed ? "The game stopped on a crash." : "The game ended as the player chose.");
        Log.Close();
        return code;
    }

    /// <summary>
    /// Where this run keeps its saves (plan 12 · Q2), by <see cref="SavePaths.Resolve"/>: the switches first, then
    /// what is found beside the game, then the user's own folder. It only decides; nothing is made or moved.
    /// </summary>
    /// <param name="machine">The machine to decide for; null for this one, which only the game's own start asks.</param>
    internal static SavePaths Locate(LaunchOptions launch, SaveEnvironment? machine = null) =>
        SavePaths.Resolve(machine ?? SaveEnvironment.Here(), launch.Portable, launch.SaveFolder);

    /// <summary>
    /// Makes the folder the saves are kept in, if it isn't there yet (a fresh install's first start). Where it can't
    /// be made the game says so and keeps its files in the folder it was started in, as it always did.
    /// </summary>
    internal static SavePaths Settle(SavePaths paths)
    {
        try
        {
            Directory.CreateDirectory(paths.Folder);
            return paths;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            Log.Say($"The folder for the saves couldn't be made ({paths.Folder}: {e.Message}). They are kept in the folder the game was started in instead.", error: true);
            return SavePaths.In(Environment.CurrentDirectory, "The folder chosen for the saves couldn't be made, so they are kept in the folder the game was started in.");
        }
    }

    /// <summary>
    /// Runs the game inside the crash handler (plan 16 · T3): an exception nobody catches, in what is run or on any
    /// other thread meanwhile, leaves a report in <c>logs/</c>, the game as it stood as <c>savegame.crash.json</c>
    /// and a note for the next start's title (<see cref="CrashReport"/>), all beside the save. The player's own save
    /// is never touched. Returns what the process should exit with. The screenshot harness's <c>crash</c> mode runs
    /// a loop of its own through here, which is how the handler is seen at work without crashing a real game.
    /// </summary>
    /// <param name="engine">The game, once it has been made: what the report says of it and the crash save come from it.</param>
    /// <param name="arguments">The command line, for the report.</param>
    /// <param name="saveFolder">Where the save is, for a test with a folder of its own; null for the game's.</param>
    internal static int Guarded(Func<GameEngine?> engine, string arguments, Action run, string? saveFolder = null)
    {
        CrashReport.Watch(() => engine()?.DescribeForCrash(), () => engine()?.CrashSave(), arguments, saveFolder);
        try
        {
            run();
            return CommandLine.Ended;
        }
        catch (Exception e)
        {
            CrashReport.Caught(e);
            return CommandLine.Crashed;
        }
        finally
        {
            CrashReport.StopWatching();
        }
    }

    // What a run that ended without a crash exits with, where it isn't Ended (a replay refused)
    private static int? playCode;

    private static void Play(LaunchOptions launch, Action<GameEngine> made)
    {
        // The window opens at the size and V-Sync choice saved in the options (sized for the monitor the first time),
        // but for what a switch asks of this run alone (--windowed, --fullscreen, --safe-mode)
        var settings = GameSettings.Load();
        launch.ApplyTo(settings);
        // --replay (plan 16 · T20): the recording is read, and refused if this build can't play it, before a window opens
        MoviePlayer? player = null;
        if (launch.ReplayFolder is { } replayFolder)
        {
            player = OpenReplay(replayFolder);
            if (player == null)
            {
                playCode = CommandLine.ReplayRefused;
                return;
            }
        }
        // Developer mode (plan 16 · T6): the console, and the files watched, read from the repository's data before anything loads them
        if (DevMode.Wanted(launch, settings)) DevMode.Enable();
        if (launch.SafeMode)
        {
            // Safe mode shows the game as it is built: every model meshed afresh, and no hand-made file read
            SdfCache.Enabled = false;
            Overrides.Enabled = false;
            Log.Info("Safe mode: the Low preset in a 1280x720 window, no mesh cache, no overrides.");
        }
        // The packs beside the game and the saves, laid over the game's data before anything reads it (plan 16 · T14)
        Packs.Start(settings, SavePaths.Current.Folder, launch.NoPacks || launch.SafeMode);
        Raylib.SetConfigFlags(ConfigFlags.ResizableWindow | (settings.VSync ? ConfigFlags.VSyncHint : 0));
        Raylib.InitWindow(settings.WindowWidth, settings.WindowHeight, "Pokémon Platinum - Sinnoh Distortion Engine");
        Raylib.SetWindowMinSize(960, 540);
        Raylib.SetExitKey(KeyboardKey.Null); // Esc is "back" in the menus; the window closes with its own button
        // (Safe mode keeps its small window even the first time, and writes no options of its own choosing)
        if (!launch.SafeMode) WindowSettings.FitToMonitor(settings);
        // The window's size, full screen, V-Sync and the frame limit, which is 60 a second unless the options say otherwise
        WindowSettings.Apply(settings);
        Log.Info($"Renderer: {Gl.Renderer() ?? "unknown"}");
        Log.Info($"Options: {System.Text.RegularExpressions.Regex.Replace(settings.Describe(), @"\s+", " ")}");
        if (player != null)
        {
            Replay(player, launch.ReplayFolder!, made);
            Raylib.CloseWindow();
            return;
        }

        // The session is recorded from the title (plan 16 · T19): a seed drawn by chance goes into the dice before the
        // engine is made, so the field's chance and every map the start loads draw from it, and each frame's keys and
        // clock are kept with it. Only --record writes it anywhere
        var recorder = MovieRecorder.Begin(settings, SavePaths.Current.Folder, launch.Region);
        Log.Info($"Recording the session from seed {recorder.Recording.Seed}{(launch.RecordPath != null ? $", to be written to {launch.RecordPath}" : "")}.");

        // Initialize Engine
        GameEngine engine = new(settings) { Recording = recorder.Recording };
        made(engine);
        // A crash leaves a report's folder beside its report, which replays to the frame that threw (plan 16 · T20)
        CrashReport.ReportFolderWriter = engine.WriteCrashFolder;
        engine.NewGameRegion = launch.Region;
        engine.Initialize();
        // What the start-up had to say is on the disk before the first frame
        Log.Flush();

        // Main Game Loop: the window's close button asks first (plan 12 · Q1), and the game ends once it is answered
        long frames = 0;
        var live = new LiveInput();
        try
        {
            while (!engine.QuitRequested)
            {
                // The close button and whether the window is the one in front (with the options' "Pause in the
                // background" on, the game waits while it isn't) go into the frame, as they change the game
                live.Window = (Raylib.WindowShouldClose() ? WindowEvents.CloseAsked : WindowEvents.None)
                    | (Raylib.IsWindowFocused() ? WindowEvents.None : WindowEvents.Unfocused);

                float dt = Raylib.GetFrameTime();
                if (dt > 0.1f) dt = 0.1f; // Prevent spiral of death on lag spike

                recorder.Step(engine, live.Next(dt)!.Value);
                engine.Draw();

                // --crash-after: the crash handler seen at work on the real game
                if (launch.CrashAfter is { } crashAfter && ++frames >= crashAfter)
                    throw new InvalidOperationException($"Thrown on purpose after {frames} frames (--crash-after).");
            }
        }
        finally
        {
            Movie.Release();
            if (launch.RecordPath is { } path)
            {
                try
                {
                    recorder.Recording.WriteTo(path);
                    Log.Info($"The session's recording ({recorder.Recording.Frames.Count} frames) was written to {path}.");
                }
                catch (Exception e) when (e is IOException or UnauthorizedAccessException)
                {
                    Log.Warn($"The session's recording couldn't be written to {path}: {e.Message}");
                }
            }
        }

        // Cleanup
        engine.Close();
        Raylib.CloseWindow();
    }

    /// <summary>A report's recording, read and checked; null, said to the terminal and the log, where it can't be played.</summary>
    private static MoviePlayer? OpenReplay(string folder)
    {
        string movie = ReportFolder.MovieIn(folder);
        try
        {
            return new MoviePlayer(InputRecording.ReadFrom(movie));
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or InvalidDataException or EndOfStreamException or RecordingRefusedException)
        {
            string why = $"The recording {movie} can't be played: {e.Message}";
            Log.Warn(why);
            Log.Say(why, error: true);
            return null;
        }
    }

    /// <summary>
    /// A report played back in the game's window (plan 16 · T20, <c>--replay</c>): the title's files and the seed from
    /// the recording, the game saving into the folder's <c>replayed/</c> and never over the player's save, its options
    /// changing the game but never the window, and the frames fed from the recording. The keys do nothing but Menu,
    /// which leaves, and Run held, which plays four frames to each one drawn. A standing notice names the second the
    /// hash parts at, if it does, and the end.
    /// </summary>
    private static void Replay(MoviePlayer player, string folder, Action<GameEngine> made)
    {
        string root = File.Exists(folder) ? Path.GetDirectoryName(Path.GetFullPath(folder))! : Path.GetFullPath(folder);
        string replayed = Path.Combine(root, ReportFolder.ReplayedName);
        Log.Info($"Replaying {player.Recording.Frames.Count} frames ({player.Recording.Seconds:0.0} s) of {folder}, saving into {replayed}.");
        var engine = player.Prepare(replayed, headless: false);
        engine.WindowLocked = true;
        made(engine);
        engine.Initialize();
        engine.StandingNotice = Text.Of("replayNotice.playing", ("folder", ReportFolder.Shown(root)));
        Log.Flush();

        float shown = 0f;
        bool over = false;
        try
        {
            while (!Raylib.WindowShouldClose())
            {
                // The player's own keys, read outside the recording's frames: only Menu and Run mean anything
                var keys = InputManager.ReadKeys();
                if (keys.IsPressed(GameAction.Menu)) break;
                if (!over)
                {
                    int frames = keys.IsDown(GameAction.Run) ? 4 : 1;
                    for (int i = 0; i < frames && !over; i++) over = !player.Step(engine);
                    if (over)
                    {
                        engine.StandingNotice = player.Parted is { } parted
                            ? Text.Of("replayNotice.parted", ("seconds", parted.Seconds.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)), ("groups", string.Join(", ", parted.Groups)))
                            : Text.Of("replayNotice.ended");
                        Log.Info(player.Parted?.ToString() ?? $"The replay came to the recording's end at frame {player.Position}.");
                    }
                    else if ((shown += Raylib.GetFrameTime()) > Toast.SlideTime * 2f + Toast.HoldTime && engine.StandingNotice != null) engine.StandingNotice = null;
                }
                engine.Draw();
            }
        }
        finally
        {
            Movie.Release();
            engine.Close();
        }
    }
}
