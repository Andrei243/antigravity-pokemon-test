using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Core.Diagnostics;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// Applies the window part of the player's options: size, borderless full screen, V-Sync and the frame limit (plan
/// 16 · T3). Only the game's own window is given them (<c>Program.Main</c>, and the options through
/// <c>ApplySettings(window: true)</c>): the screenshot harness's hidden window never is, so it draws as fast as it can.
/// </summary>
public static class WindowSettings
{
    private static bool fullscreen;
    private static Vector2 windowedPosition;

    /// <summary>What raylib is asked to hold the game to for a frame limit: the limit itself, and nought (no limit) for anything that is none.</summary>
    public static int TargetFps(int frameCap) => frameCap > 0 ? frameCap : 0;

    public static void Apply(GameSettings settings)
    {
        Raylib.SetTargetFPS(TargetFps(settings.FrameCap));

        if (settings.Fullscreen != fullscreen)
        {
            if (settings.Fullscreen) EnterFullscreen();
            else LeaveFullscreen(settings);
        }
        else if (!fullscreen &&
            (Raylib.GetScreenWidth() != settings.WindowWidth || Raylib.GetScreenHeight() != settings.WindowHeight))
        {
            Raylib.SetWindowSize(settings.WindowWidth, settings.WindowHeight);
        }

        if (settings.VSync) Raylib.SetWindowState(ConfigFlags.VSyncHint);
        else Raylib.ClearWindowState(ConfigFlags.VSyncHint);
    }

    /// <summary>
    /// The size of the full-screen window on a monitor: one row taller than the display, so the last row hangs
    /// off the bottom edge. An OpenGL window that covers the monitor exactly is taken over by the graphics driver
    /// as an exclusive full-screen game (bypassing the desktop compositor and injecting its overlay), which
    /// flickers on NVIDIA cards; a window of any other size stays composited.
    /// </summary>
    public static (int Width, int Height) FullscreenSize(int monitorWidth, int monitorHeight) => (monitorWidth, monitorHeight + 1);

    private static void EnterFullscreen()
    {
        windowedPosition = Raylib.GetWindowPosition();
        int monitor = Raylib.GetCurrentMonitor();
        var origin = Raylib.GetMonitorPosition(monitor);
        // The monitor's own resolution, asked again as full screen is entered (the window may have moved to another)
        var native = AspectLayout.NativeOr(Raylib.GetMonitorWidth(monitor), Raylib.GetMonitorHeight(monitor));
        UI.OptionsScreen.Monitor = native;
        var (width, height) = FullscreenSize(native.Width, native.Height);

        Raylib.SetWindowState(ConfigFlags.UndecoratedWindow);
        Raylib.SetWindowPosition((int)origin.X, (int)origin.Y);
        Raylib.SetWindowSize(width, height);
        Raylib.SetWindowFocused(); // the taskbar only steps behind a full-screen window that is in front
        fullscreen = true;
    }

    private static void LeaveFullscreen(GameSettings settings)
    {
        Raylib.ClearWindowState(ConfigFlags.UndecoratedWindow);
        Raylib.SetWindowSize(settings.WindowWidth, settings.WindowHeight);
        Raylib.SetWindowPosition((int)windowedPosition.X, (int)windowedPosition.Y);
        fullscreen = false;
    }

    /// <summary>
    /// On the first run, picks the largest window the monitor can show (2560x1440 on a 4K display), centres it and
    /// remembers the choice. Full screen (F11) shows the game at the display's own resolution.
    /// </summary>
    public static void FitToMonitor(GameSettings settings)
    {
        int monitor = Raylib.GetCurrentMonitor();
        var (mw, mh) = AspectLayout.NativeOr(Raylib.GetMonitorWidth(monitor), Raylib.GetMonitorHeight(monitor));
        // The monitor's own resolution, found at every start (rulings, "Other shapes of screen, and the resolution")
        UI.OptionsScreen.Monitor = (mw, mh);
        Log.Info($"The monitor: {mw}×{mh} ({AspectLayout.RatioName(mw, mh)}).");

        // A window saved larger than the monitor it opens on is brought within it
        bool tooLarge = !settings.FirstRun && (settings.WindowWidth > mw || settings.WindowHeight > mh);
        if (!settings.FirstRun && !tooLarge) return;

        // A monitor of 16:9 keeps the listed sizes; another shape gets a window of its own ratio
        (settings.WindowWidth, settings.WindowHeight) = AspectLayout.For(mw, mh).IsStandard
            ? GameSettings.LargestWindowFor(mw, mh)
            : AspectLayout.LargestWindowFor(mw, mh);
        Raylib.SetWindowSize(settings.WindowWidth, settings.WindowHeight);

        var origin = Raylib.GetMonitorPosition(monitor);
        Raylib.SetWindowPosition((int)origin.X + (mw - settings.WindowWidth) / 2, (int)origin.Y + (mh - settings.WindowHeight) / 2);
        settings.Save();
    }
}
