using System;
using System.IO;
using PokemonPlatinumEngine.Core.Diagnostics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Core;

// The screenshot key (plan 12 · Q16, its first half; photo mode is the rest)
public partial class GameEngine
{
    // A picture the key asked for, taken once this frame is drawn
    private bool screenshotWanted;

    /// <summary>The last picture the screenshot key wrote this run; null before any.</summary>
    public string? LastScreenshot { get; private set; }

    /// <summary>Whether a picture waits for the frame being drawn: the toasts are left out of it.</summary>
    internal bool ScreenshotWanted => screenshotWanted;

    /// <summary>
    /// The Screenshot key (F12, <see cref="GameAction.Screenshot"/>): read every frame, in every state, even while the
    /// game waits behind another window. Nothing in a game with no window, which draws nothing to take.
    /// </summary>
    private void KeepTheScreenshotKey()
    {
        if (IsHeadless || InputManager.CaptureRequested || !InputManager.IsActionPressed(GameAction.Screenshot)) return;
        screenshotWanted = true;
    }

    /// <summary>
    /// Writes the 3840×2160 picture of the frame just drawn as a PNG into <paramref name="folder"/> (made if need be),
    /// named by the wall clock (<see cref="Screenshots.PathFor"/>, which the harness fixes); returns its path. Changes
    /// nothing of the game.
    /// </summary>
    public string CaptureScreen(string folder)
    {
        Directory.CreateDirectory(folder);
        string path = Screenshots.PathFor(folder, WallClock.Local, File.Exists);
        var image = Raylib.LoadImageFromTexture(virtualScreen.Texture);
        Raylib.ImageFlipVertical(ref image);
        Raylib.ExportImage(image, path);
        Raylib.UnloadImage(image);
        return path;
    }

    /// <summary>The picture the key asked for, once the frame is drawn: a shutter, and a toast that names the file (after it, so it isn't in it).</summary>
    private void TakeScreenshot()
    {
        if (!screenshotWanted) return;
        screenshotWanted = false;
        try
        {
            string path = CaptureScreen(SavePaths.Current.Screenshots);
            LastScreenshot = path;
            Log.Info($"Screenshot written: {path}");
            AudioManager.PlaySound("shutter");
            flow.Toast.Show(Text.Of("screenshotKey.taken", ("file", Path.GetFileName(path))));
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            Log.Warn($"The screenshot couldn't be written: {e.Message}");
            flow.Toast.Show(Text.Of("screenshotKey.notTaken", ("why", e.Message)));
        }
    }

    /// <summary>A tool's press of the screenshot key: the next frame drawn is written. False in a game with no window.</summary>
    internal bool AskForScreenshot()
    {
        if (IsHeadless) return false;
        screenshotWanted = true;
        return true;
    }

    internal sealed partial class Driver
    {
        /// <summary>The screenshot key pressed by a tool (plan 12 · Q16): the next frame drawn goes beside the saves.</summary>
        public bool PressScreenshot() => game.AskForScreenshot();

        /// <summary>The last picture the key wrote; null before any.</summary>
        public string? LastScreenshot => game.LastScreenshot;
    }
}
