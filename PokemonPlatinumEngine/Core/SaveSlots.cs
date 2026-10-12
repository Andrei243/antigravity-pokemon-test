using System.Globalization;
using System.Text;

namespace PokemonPlatinumEngine.Core;

/// <summary>A slot as it was read: its number from 1, what became of its save (<see cref="SaveLoad"/>) and the game in it, if one could be read.</summary>
public sealed record SaveSlot(int Number, SaveLoad Outcome, SaveData? Save)
{
    /// <summary>Whether anything is kept in the slot: a save, or only a backup, readable or not.</summary>
    public bool Taken => Outcome != SaveLoad.None;
}

/// <summary>What became of the one save an install had before there were slots.</summary>
public enum OldSaveMove
{
    /// <summary>There was none to move.</summary>
    Nothing,
    /// <summary>It became a slot's save, its backup with it.</summary>
    Moved,
    /// <summary>Every slot holds a save already: it was left where it is, untouched.</summary>
    NoRoom,
    /// <summary>A file couldn't be moved (it is in use, or the folder can't be written): it was left as it is, and the next start tries again.</summary>
    Failed
}

/// <param name="Slot">The slot the old save went to; nought where it went nowhere.</param>
/// <param name="Problem">Why it couldn't be moved, where it couldn't.</param>
public sealed record OldSaveOutcome(OldSaveMove Move, int Slot = 0, string? Problem = null);

/// <summary>
/// The three slots a game is saved in (plan 12 · Q2), as files of <see cref="SavePaths"/>: <c>save1.json</c> to
/// <c>save3.json</c>, each written and read as <see cref="SaveManager"/> writes and reads a save (a temporary file,
/// the save before it kept as <c>save1.bak</c>, a checksum). A slot is empty when neither its save nor its backup is
/// there. Every method takes the folder it works in, so tests work in temporary ones. No drawing, no input.
/// </summary>
public static class SaveSlots
{
    /// <summary>
    /// The note <see cref="AdoptOldSave"/> leaves while it moves an old save: the slot it is moving it to. A start
    /// that finds one finishes that move, to that slot and no other.
    /// </summary>
    public const string MovingName = "savegame.moving";

    public static bool IsEmpty(SavePaths paths, int slot) =>
        !File.Exists(paths.Slot(slot)) && !File.Exists(SaveManager.BackupOf(paths.Slot(slot)));

    /// <summary>The lowest slot with nothing in it, which is where a new game is saved; null when all three are taken.</summary>
    public static int? FirstEmpty(SavePaths paths)
    {
        for (int slot = 1; slot <= SavePaths.Slots; slot++)
            if (IsEmpty(paths, slot)) return slot;
        return null;
    }

    /// <summary>The three slots in order, each read as <see cref="SaveManager.Load(string)"/> reads a save.</summary>
    public static IReadOnlyList<SaveSlot> Read(SavePaths paths)
    {
        var slots = new List<SaveSlot>();
        for (int slot = 1; slot <= SavePaths.Slots; slot++) slots.Add(Read(paths, slot));
        return slots;
    }

    public static SaveSlot Read(SavePaths paths, int slot)
    {
        var (outcome, save) = SaveManager.Load(paths.Slot(slot));
        return new SaveSlot(slot, outcome, save);
    }

    /// <summary>Writes a game into a slot, as <see cref="SaveManager.SaveGame(SaveData, string)"/> writes a save; the folder is made if it isn't there yet.</summary>
    public static bool Write(SavePaths paths, int slot, SaveData data)
    {
        try
        {
            Directory.CreateDirectory(paths.Folder);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            return false;
        }
        return SaveManager.SaveGame(data, paths.Slot(slot));
    }

    /// <summary>
    /// Deletes a slot for good, at the player's own word: its backup and anything half written first, its save last,
    /// so that a deletion cut short leaves the game as it was last saved and never an older one in its place. True
    /// when nothing of the slot is left.
    /// </summary>
    public static bool Delete(SavePaths paths, int slot)
    {
        string save = paths.Slot(slot);
        try
        {
            File.Delete(SaveManager.BackupOf(save));
            File.Delete(SaveManager.TempOf(save));
            File.Delete(save);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            // In use, or a folder that can't be written: what couldn't be deleted is still there, and the answer says so
        }
        return IsEmpty(paths, slot);
    }

    // ------------------------------------------------------------------ the save from before there were slots

    /// <summary>
    /// An install from before there were slots kept one save, <c>savegame.json</c>, with <c>savegame.bak</c> beside
    /// it. Found in the folder, they become the first empty slot's save and backup: slot 1, unless something is
    /// there already. Each file is renamed, never copied and never put over a file that is there, so the save is byte
    /// for byte what it was and its checksum holds. It happens once: afterwards there is no old save to find.
    /// <para>
    /// The two renames are not one step, so the slot is written down first (<see cref="MovingName"/>) and the note
    /// taken away last: a start that finds the note finishes the move it tells of. Without a note nothing is ever
    /// added to a slot that holds something, so a backup can't end up beside another game's save.
    /// </para>
    /// </summary>
    public static OldSaveOutcome AdoptOldSave(SavePaths paths)
    {
        string old = paths.OldSave, oldBackup = SaveManager.BackupOf(old), note = Path.Combine(paths.Folder, MovingName);
        try
        {
            int moved = 0;
            // A move begun by a start that didn't finish it: on to the slot it was going to
            if (ReadNote(note) is { } begun)
            {
                if (Move(paths, old, oldBackup, begun)) moved = begun;
                File.Delete(note);
            }
            else if (File.Exists(note)) File.Delete(note);

            // An old save here now (or what a finished move couldn't place) goes to the first empty slot
            if (File.Exists(old) || File.Exists(oldBackup))
            {
                if (FirstEmpty(paths) is not { } slot) return new OldSaveOutcome(OldSaveMove.NoRoom);
                WriteNote(note, slot);
                Move(paths, old, oldBackup, slot);
                File.Delete(note);
                moved = slot;
            }
            return moved > 0 ? new OldSaveOutcome(OldSaveMove.Moved, moved) : new OldSaveOutcome(OldSaveMove.Nothing);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            return new OldSaveOutcome(OldSaveMove.Failed, Problem: e.Message);
        }
    }

    // The save first, then its backup, each only into a place that is free. True when either was moved.
    private static bool Move(SavePaths paths, string old, string oldBackup, int slot)
    {
        string save = paths.Slot(slot), backup = SaveManager.BackupOf(save);
        bool moved = false;
        if (File.Exists(old) && !File.Exists(save))
        {
            File.Move(old, save);
            moved = true;
        }
        if (File.Exists(oldBackup) && !File.Exists(backup))
        {
            File.Move(oldBackup, backup);
            moved = true;
        }
        return moved;
    }

    private static int? ReadNote(string note)
    {
        if (!File.Exists(note)) return null;
        // A note cut short as it was written says nothing: nothing had been moved yet
        return int.TryParse(File.ReadAllText(note).Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out int slot) && SavePaths.IsSlot(slot) ? slot : null;
    }

    // On the disk before anything is moved, as a save's temporary file is
    private static void WriteNote(string note, int slot)
    {
        using var stream = new FileStream(note, FileMode.Create, FileAccess.Write, FileShare.None);
        stream.Write(Encoding.ASCII.GetBytes(slot.ToString(CultureInfo.InvariantCulture)));
        stream.Flush(flushToDisk: true);
    }
}
