using Raylib_cs;

namespace PokemonPlatinumEngine.Core;

/// <summary>Applies the window part of the player's options: size, borderless full screen and V-Sync.</summary>
public static class WindowSettings
{
    public static void Apply(GameSettings settings)
    {
        bool borderless = Raylib.IsWindowState(ConfigFlags.BorderlessWindowMode);
        if (settings.Fullscreen != borderless) Raylib.ToggleBorderlessWindowed();

        if (!settings.Fullscreen &&
            (Raylib.GetScreenWidth() != settings.WindowWidth || Raylib.GetScreenHeight() != settings.WindowHeight))
        {
            Raylib.SetWindowSize(settings.WindowWidth, settings.WindowHeight);
        }

        if (settings.VSync) Raylib.SetWindowState(ConfigFlags.VSyncHint);
        else Raylib.ClearWindowState(ConfigFlags.VSyncHint);
    }
}
