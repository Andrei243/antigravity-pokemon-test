using System;
using System.Runtime.InteropServices;
using Raylib_cs;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Core.Diagnostics;
using PokemonPlatinumEngine.Graphics;

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

        CommandLine.Launched = launch;
        // The log is beside the save unless --log names another place; a run begins with what it is and where it runs
        if (!Log.Open(launch.LogPath ?? Log.FileName)) Log.Say($"The log couldn't be opened at {launch.LogPath ?? Log.FileName}: this run keeps none.", error: true);
        Log.Heading($"==== Pokémon Platinum (a fan remake) {GameVersion.Describe()}, started {WallClock.Local.ToString("yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture)} ====");
        Log.Info($"Started with: {(launch.Given.Length > 0 ? launch.Given : "(no switches)")}");
        Log.Info($"System: {RuntimeInformation.OSDescription} ({RuntimeInformation.OSArchitecture}), {RuntimeInformation.FrameworkDescription}");

        GameEngine? engine = null;
        int code = Guarded(() => engine, launch.Given, () => Play(launch, made => engine = made));
        Log.Info(code == CommandLine.Ended ? "The game ended as the player chose." : "The game stopped on a crash.");
        Log.Close();
        return code;
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

    private static void Play(LaunchOptions launch, Action<GameEngine> made)
    {
        // The window opens at the size and V-Sync choice saved in the options (sized for the monitor the first time),
        // but for what a switch asks of this run alone (--windowed, --fullscreen, --safe-mode)
        var settings = GameSettings.Load();
        launch.ApplyTo(settings);
        if (launch.SafeMode)
        {
            // Safe mode shows the game as it is built: every model meshed afresh, and no hand-made file read
            SdfCache.Enabled = false;
            Overrides.Enabled = false;
            Log.Info("Safe mode: the Low preset in a 1280x720 window, no mesh cache, no overrides.");
        }
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

        // Initialize Engine
        GameEngine engine = new(settings);
        made(engine);
        engine.NewGameRegion = launch.Region;
        engine.Initialize();
        // What the start-up had to say is on the disk before the first frame
        Log.Flush();

        // Main Game Loop: the window's close button asks first (plan 12 · Q1), and the game ends once it is answered
        long frames = 0;
        while (!engine.QuitRequested)
        {
            if (Raylib.WindowShouldClose()) engine.RequestClose();

            // Toggle full screen on F11 or Alt+Enter
            if (Raylib.IsKeyPressed(KeyboardKey.F11) || (Raylib.IsKeyDown(KeyboardKey.LeftAlt) && Raylib.IsKeyPressed(KeyboardKey.Enter)))
            {
                engine.ToggleFullscreen();
            }

            // Whether the window is the one in front: with the options' "Pause in the background" on, the game waits while it isn't
            engine.WindowFocused = Raylib.IsWindowFocused();

            float dt = Raylib.GetFrameTime();
            if (dt > 0.1f) dt = 0.1f; // Prevent spiral of death on lag spike

            engine.Update(dt);
            engine.Draw();

            // --crash-after: the crash handler seen at work on the real game
            if (launch.CrashAfter is { } crashAfter && ++frames >= crashAfter)
                throw new InvalidOperationException($"Thrown on purpose after {frames} frames (--crash-after).");
        }

        // Cleanup
        engine.Close();
        Raylib.CloseWindow();
    }
}
