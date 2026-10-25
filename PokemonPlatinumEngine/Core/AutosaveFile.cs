namespace PokemonPlatinumEngine.Core;

/// <summary>
/// What the title offers of an autosave (plan 12 · Q3): the slot it belongs to, the game in it, and whether the slot's
/// own save is older (<see cref="Newer"/>) or there is none (<see cref="Orphan"/>: a game that was autosaved and never
/// saved by hand).
/// </summary>
public sealed record AutosaveOffer(int Slot, SaveData Save, bool Orphan);

/// <summary>
/// The autosave (plan 12 · Q3): <c>autosave.json</c> beside the slots, written quietly while the game is played and
/// never over a slot's own save. It is a save like any other (a temporary file, the one before it as <c>autosave.bak</c>,
/// a checksum) that names its slot (<see cref="SaveData.AutosaveSlot"/>). The player's own save is saved by hand and
/// kept; a start after a crash offers the autosave when it is newer than the slot it belongs to
/// (<see cref="Offer"/>), and a save by hand of that slot takes it away (<see cref="Clear"/>), so one is never left
/// that is older than the game beside it. Every method takes the folder it works in. No drawing, no input.
/// </summary>
public static class AutosaveFile
{
    /// <summary>Writes the game as the autosave of a slot; false where the file couldn't be written. The data is left as it was.</summary>
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
        var kept = data.AutosaveSlot;
        data.AutosaveSlot = slot;
        try { return SaveManager.SaveGame(data, paths.Autosave); }
        finally { data.AutosaveSlot = kept; }
    }

    /// <summary>The autosave as <see cref="SaveManager.Load(string)"/> reads a save: nothing at all, whole, recovered from its backup, or damaged.</summary>
    public static (SaveLoad Outcome, SaveData? Save) Read(SavePaths paths) => SaveManager.Load(paths.Autosave);

    /// <summary>Whether an autosave file (or its backup) is there at all.</summary>
    public static bool Exists(SavePaths paths) => File.Exists(paths.Autosave) || File.Exists(SaveManager.BackupOf(paths.Autosave));

    /// <summary>The slot the autosave is for, or null where there is none or it can't be read.</summary>
    public static int? SlotOf(SavePaths paths) => Read(paths).Save?.AutosaveSlot is { } slot && SavePaths.IsSlot(slot) ? slot : null;

    /// <summary>Takes the autosave away, with its backup and anything half written; true when nothing of it is left.</summary>
    public static bool Clear(SavePaths paths)
    {
        try
        {
            File.Delete(SaveManager.BackupOf(paths.Autosave));
            File.Delete(SaveManager.TempOf(paths.Autosave));
            File.Delete(paths.Autosave);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            // In use, or a folder that can't be written: what couldn't be deleted is still there, and the answer says so
        }
        return !Exists(paths);
    }

    /// <summary>
    /// A save by hand into a slot: the autosave of that slot, now older than the game beside it, is taken away. An
    /// autosave of another slot (one a crash left, which the player passed over) is not touched.
    /// </summary>
    public static void ClearFor(SavePaths paths, int slot)
    {
        if (!Exists(paths)) return;
        // (One that can't be read belongs to nobody)
        int? of = SlotOf(paths);
        if (of == null || of == slot) Clear(paths);
    }

    /// <summary>
    /// Whether an autosave is newer than the game saved in its slot: the same game (the card's number and the day it
    /// began) with more time played. A slot holding another game, or an older autosave, is no offer.
    /// </summary>
    public static bool Newer(SaveData autosave, SaveData saved) =>
        autosave.TrainerId == saved.TrainerId && autosave.Started == saved.Started && autosave.PlayTimeSeconds > saved.PlayTimeSeconds;

    /// <summary>
    /// What the title offers: the autosave, if it reads whole and its slot is empty (a game never saved by hand) or holds
    /// the same game, older. Null where there is nothing to offer (none, damaged, for another game, or not newer).
    /// </summary>
    public static AutosaveOffer? Offer(SavePaths paths, IReadOnlyList<SaveSlot> slots)
    {
        var (outcome, autosave) = Read(paths);
        if (autosave == null || outcome is SaveLoad.None or SaveLoad.Damaged) return null;
        if (autosave.AutosaveSlot is not { } number || !SavePaths.IsSlot(number)) return null;
        var slot = slots.FirstOrDefault(s => s.Number == number);
        // A slot that is empty keeps nothing of its own: the autosave is the game (never saved by hand, or deleted since)
        if (slot == null || !slot.Taken) return new AutosaveOffer(number, autosave, Orphan: true);
        // A slot that holds a save too damaged to read can only be deleted: the autosave stays where it is
        if (slot.Save == null) return null;
        return Newer(autosave, slot.Save) ? new AutosaveOffer(number, autosave, Orphan: false) : null;
    }
}
