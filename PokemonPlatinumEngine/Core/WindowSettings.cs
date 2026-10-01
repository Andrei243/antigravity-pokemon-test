using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Core;

/// <summary>Applies the window part of the player's options: size, borderless full screen and V-Sync.</summary>
public static class WindowSettings
{
    private static bool fullscreen;
    private static Vector2 windowedPosition;

    public static void Apply(GameSettings settings)
    {
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
        var (width, height) = FullscreenSize(Raylib.GetMonitorWidth(monitor), Raylib.GetMonitorHeight(monitor));

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
        if (!settings.FirstRun) return;

        int monitor = Raylib.GetCurrentMonitor();
        int mw = Raylib.GetMonitorWidth(monitor), mh = Raylib.GetMonitorHeight(monitor);
        (settings.WindowWidth, settings.WindowHeight) = GameSettings.LargestWindowFor(mw, mh);
        Raylib.SetWindowSize(settings.WindowWidth, settings.WindowHeight);

        var origin = Raylib.GetMonitorPosition(monitor);
        Raylib.SetWindowPosition((int)origin.X + (mw - settings.WindowWidth) / 2, (int)origin.Y + (mh - settings.WindowHeight) / 2);
        settings.Save();
    }
}
