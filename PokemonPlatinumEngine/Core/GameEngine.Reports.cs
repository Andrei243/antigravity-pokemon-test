using System;
using System.IO;
using PokemonPlatinumEngine.Core.Diagnostics;
using PokemonPlatinumEngine.UI;
using Raylib_cs;

namespace PokemonPlatinumEngine.Core;

// A report of the session, and a session played back in the game's own window (plan 16 · T20)
public partial class GameEngine
{
    /// <summary>
    /// The session's recording (plan 16 · T19), which <c>Program.Main</c> hands the engine it records: the Report key
    /// writes it into a report's folder (<see cref="WriteReport"/>). Null for a game nobody records (a test's, the
    /// harness's, a replay's own), whose Report key does nothing.
    /// </summary>
    public InputRecording? Recording { get; set; }

    /// <summary>
    /// Set for a replay in the game's window (<c>--replay</c>): what the recording's options and keys do to the game
    /// they do, but the window is the player's and stays as it is: full screen isn't toggled and no size is applied.
    /// </summary>
    public bool WindowLocked { get; set; }

    /// <summary>A notice that stays at the top of the screen until it is taken away (a replay's: where it parted, its end); null for none.</summary>
    public string? StandingNotice { get; set; }

    /// <summary>The last report this run wrote, its folder; null before any.</summary>
    public string? LastReport { get; private set; }

    // The report whose picture waits for the frame to be drawn
    private string? reportAwaitingScreen;

    /// <summary>The Report key (F8, <see cref="GameAction.Report"/>): read every frame, even while the game waits behind another window.</summary>
    private void KeepTheReportKey()
    {
        if (Recording == null || InputManager.CaptureRequested || !InputManager.IsActionPressed(GameAction.Report)) return;
        WriteReport();
    }

    /// <summary>
    /// Writes a report of the session into <c>repros/</c> beside the save (<see cref="ReportFolder"/>): the recording
    /// to this frame, the start's save, the options, the log, the picture once this frame is drawn, and
    /// <c>report.txt</c>. A toast says where it went (after the picture is taken, so it isn't in it). Changes nothing of
    /// the game: a replay of the recording goes on as the session did. Null where nothing is recorded or the folder
    /// couldn't be written.
    /// </summary>
    public string? WriteReport()
    {
        if (Recording == null) return null;
        try
        {
            string folder = ReportFolder.Write(SavePaths.Current.Folder, Recording, DescribeForCrash(), Settings.ToFile(), WallClock.Local);
            LastReport = folder;
            Log.Info($"A report to replay was written to {folder} ({Recording.Frames.Count} frames).");
            if (IsHeadless) flow.Toast.Show(Text.Of("sessionReport.written", ("folder", ReportFolder.Shown(folder))));
            else reportAwaitingScreen = folder;
            return folder;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            Log.Warn($"The report couldn't be written: {e.Message}");
            flow.Toast.Show(Text.Of("sessionReport.notWritten", ("why", e.Message)));
            return null;
        }
    }

    /// <summary>
    /// A crash's report folder (<see cref="CrashReport.ReportFolderWriter"/>): the recording to the frame that threw,
    /// which a replay comes to and throws in again; no picture, since the frame was never drawn. Null where nothing is recorded.
    /// </summary>
    internal string? WriteCrashFolder(string saveFolder, Exception exception)
    {
        if (Recording == null) return null;
        GameFacts facts;
        try { facts = DescribeForCrash(); }
        catch (Exception) { facts = new GameFacts(flow.State.ToString()); }
        return ReportFolder.Write(saveFolder, Recording, facts, Settings.ToFile(), WallClock.Local, $"{exception.GetType().FullName}: {exception.Message}");
    }

    /// <summary>The picture of the frame a report was written in, once it is drawn (before the toast that names the folder).</summary>
    private void TakeReportScreen()
    {
        if (reportAwaitingScreen is not { } folder) return;
        reportAwaitingScreen = null;
        try
        {
            var image = Raylib.LoadImageFromTexture(virtualScreen.Texture);
            Raylib.ImageFlipVertical(ref image);
            Raylib.ImageResize(ref image, VirtualWidth, VirtualHeight);
            Raylib.ExportImage(image, Path.Combine(folder, ReportFolder.ScreenName));
            Raylib.UnloadImage(image);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            Log.Warn($"The report's picture couldn't be written: {e.Message}");
        }
        flow.Toast.Show(Text.Of("sessionReport.written", ("folder", ReportFolder.Shown(folder))));
    }

    /// <summary>The standing notice over everything but the close button's question.</summary>
    private void DrawStandingNotice()
    {
        if (StandingNotice is { } notice) ModernUi.DrawToast(VirtualWidth, notice, 1f);
    }
}
