using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>
/// One file of the Vs. Recorder's folder: the battle it holds, or why it can't be played (<see cref="Refused"/>).
/// </summary>
public sealed record ReplayEntry(string Path, string FileName, BattleRecordFile? File, ReplayRefusal Refused)
{
    /// <summary>The recording can be watched.</summary>
    public bool Playable => File != null && Refused == ReplayRefusal.None;

    /// <summary>Why it can't be played, in the player's words (empty for one that can).</summary>
    public string Notice => BattleRecordFile.Notice(Refused);
}

/// <summary>
/// The Vs. Recorder's list (plan 08 · P11; style guide, "Menu screens", "Vs. Recorder"): the battles kept in
/// <c>replays</c> beside the saves, newest first, the one under the cursor told on the right, and a file the game
/// can't play shown with its notice. The A button chooses a recording to watch (<see cref="TakeChoice"/>), B goes
/// back. Its logic takes no input (<see cref="Move"/>, <see cref="Confirm"/>, <see cref="Cancel"/>), so tests and the
/// harness drive it; <see cref="Read"/> reads a folder with no window.
/// </summary>
public class ReplayListScreen
{
    private const float AppearTime = 0.3f;
    private float openAge;
    private ReplayEntry? chosen;

    /// <summary>How many rows the list panel shows: as many as its panel holds (<see cref="ModernUi.RowsIn"/>).</summary>
    public static int VisibleRows => ModernUi.RowsIn(ModernUi.ContentBottom - ModernUi.ContentTop);

    public bool IsActive { get; private set; }

    /// <summary>The recordings, newest first.</summary>
    public IReadOnlyList<ReplayEntry> Entries { get; private set; } = Array.Empty<ReplayEntry>();

    public int Cursor { get; private set; }

    /// <summary>The first row the list shows.</summary>
    public int FirstRow { get; private set; }

    /// <summary>The recording under the cursor; null with none.</summary>
    public ReplayEntry? Selected => Cursor >= 0 && Cursor < Entries.Count ? Entries[Cursor] : null;

    /// <summary>How far the screen has slid into place, 0 to 1.</summary>
    public float Appear => Math.Clamp(openAge / AppearTime, 0f, 1f);

    /// <summary>
    /// Every file of the folder that the Vs. Recorder wrote, read: newest first, by the day its name begins with and
    /// then by when it was written. A folder that isn't there has none.
    /// </summary>
    public static List<ReplayEntry> Read(string folder)
    {
        var entries = new List<(ReplayEntry Entry, DateTime Written)>();
        if (!Directory.Exists(folder)) return new List<ReplayEntry>();
        foreach (string path in Directory.EnumerateFiles(folder, "*." + BattleRecordFile.Extension))
        {
            var reading = BattleRecordFile.ReadFile(path);
            DateTime written;
            try { written = System.IO.File.GetLastWriteTimeUtc(path); }
            catch (IOException) { written = DateTime.MinValue; }
            entries.Add((new ReplayEntry(path, System.IO.Path.GetFileNameWithoutExtension(path), reading.File, reading.Refused), written));
        }
        return entries
            .OrderByDescending(e => e.Entry.FileName.Length >= 8 ? e.Entry.FileName[..8] : "", StringComparer.Ordinal)
            .ThenByDescending(e => e.Written)
            .ThenByDescending(e => e.Entry.FileName, StringComparer.Ordinal)
            .Select(e => e.Entry).ToList();
    }

    public void Open(IReadOnlyList<ReplayEntry> entries)
    {
        Entries = entries;
        Cursor = 0;
        FirstRow = 0;
        chosen = null;
        openAge = 0f;
        IsActive = true;
        AudioManager.PlaySound("menu_open");
    }

    /// <summary>Comes back to the list as it was left, after a recording has been watched.</summary>
    public void Resume()
    {
        IsActive = true;
        chosen = null;
        openAge = AppearTime;
    }

    public void Close() => IsActive = false;

    /// <summary>The cursor up (−1) or down (+1), wrapping round.</summary>
    public void Move(int delta)
    {
        if (Entries.Count == 0) return;
        int next = UiNav.Wrap(Cursor, delta, Entries.Count);
        if (next == Cursor) return;
        Cursor = next;
        FirstRow = UiNav.Window(FirstRow, Cursor, Entries.Count, VisibleRows);
        AudioManager.PlaySound("cursor");
    }

    /// <summary>
    /// The A button: a recording that can be played is chosen and the list closes (the game then plays it); one that
    /// can't says no.
    /// </summary>
    public void Confirm()
    {
        if (Selected is not { } entry) return;
        if (!entry.Playable)
        {
            AudioManager.PlaySound("error");
            return;
        }
        chosen = entry;
        IsActive = false;
        AudioManager.PlaySound("select");
    }

    /// <summary>The B button: back to where the Vs. Recorder was used.</summary>
    public void Cancel()
    {
        IsActive = false;
        AudioManager.PlaySound("cancel");
    }

    /// <summary>The recording chosen to watch, handed over once.</summary>
    public ReplayEntry? TakeChoice()
    {
        var entry = chosen;
        chosen = null;
        return entry;
    }

    public void Update(float dt = 1f / 60f)
    {
        if (!IsActive) return;
        openAge += dt;
        if (InputManager.IsActionPressed(GameAction.Up)) Move(-1);
        else if (InputManager.IsActionPressed(GameAction.Down)) Move(1);
        else if (InputManager.IsActionPressed(GameAction.Confirm)) Confirm();
        else if (InputManager.IsActionPressed(GameAction.Cancel)) Cancel();
    }

    /// <summary>Draws the list; also while the picture closes on it or opens from it, when it is not active.</summary>
    public void Draw(int screenWidth, int screenHeight)
    {
        ModernUi.DrawReplayList(screenWidth, screenHeight, this, Appear);
    }
}
