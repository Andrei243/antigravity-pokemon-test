using System;
using System.IO;
using System.Linq;
using PokemonPlatinumEngine.Core;

namespace PokemonPlatinumTests;

/// <summary>
/// The three slots a game is saved in, and the one save from before there were slots (plan 12 · Q2): each slot a file
/// of its own, written and read as a save is; a slot deleted gone with its backup; the old save made a slot once, by
/// renaming, never over anything, and finished at the next start if it was cut short. Every file is in a folder of
/// the test's own, never the working directory and never a user's.
/// </summary>
public class SaveSlotsTests
{
    /// <summary>A folder of the test's own with the saves in it, deleted afterwards.</summary>
    private sealed class Folder : IDisposable
    {
        public string Path { get; } = Directory.CreateTempSubdirectory("pokemon-slots-").FullName;
        public SavePaths Paths => SavePaths.In(Path);
        public string File(string name) => System.IO.Path.Combine(Path, name);
        public string OldSave => File("savegame.json");
        public string OldBackup => File("savegame.bak");
        public string Note => File(SaveSlots.MovingName);

        /// <summary>The names of the files in the folder, in order.</summary>
        public string[] Files() => Directory.GetFiles(Path).Select(System.IO.Path.GetFileName).Order(StringComparer.Ordinal).ToArray()!;

        /// <summary>An old install's save, written twice as the game wrote it, so that its backup is there too.</summary>
        public void OldInstall(string earlier = "Earlier", string later = "Later")
        {
            Assert.True(SaveManager.SaveGame(Named(earlier), OldSave));
            Assert.True(SaveManager.SaveGame(Named(later), OldSave));
            Assert.True(System.IO.File.Exists(OldSave) && System.IO.File.Exists(OldBackup));
            Assert.False(System.IO.File.Exists(SaveManager.TempOf(OldSave)));
        }

        public void Dispose()
        {
            try { Directory.Delete(Path, recursive: true); }
            catch (IOException) { }
        }
    }

    private static SaveData Named(string name, int money = 3000) => new() { PlayerName = name, Money = money, CurrentMapName = "Sinnoh" };

    private static string? Who(SavePaths paths, int slot) => SaveSlots.Read(paths, slot).Save?.PlayerName;

    // ------------------------------------------------------------------ three slots

    [Fact]
    public void EachSlotIsASaveOfItsOwn()
    {
        using var folder = new Folder();
        var paths = folder.Paths;
        Assert.All(SaveSlots.Read(paths), slot => Assert.Equal((SaveLoad.None, false), (slot.Outcome, slot.Taken)));
        Assert.Equal(new[] { 1, 2, 3 }, SaveSlots.Read(paths).Select(s => s.Number));
        Assert.Equal(1, SaveSlots.FirstEmpty(paths));

        Assert.True(SaveSlots.Write(paths, 1, Named("Lucas")));
        Assert.True(SaveSlots.Write(paths, 3, Named("Barry")));
        Assert.Equal(new[] { "save1.json", "save3.json" }, folder.Files());
        var slots = SaveSlots.Read(paths);
        Assert.Equal(new[] { SaveLoad.Ok, SaveLoad.None, SaveLoad.Ok }, slots.Select(s => s.Outcome));
        Assert.Equal(new[] { "Lucas", null, "Barry" }, slots.Select(s => s.Save?.PlayerName));
        Assert.Equal(new[] { true, false, true }, slots.Select(s => s.Taken));
        Assert.Equal(2, SaveSlots.FirstEmpty(paths));

        // A slot written again keeps the save before it as its own backup, and no other slot's file is touched
        byte[] third = File.ReadAllBytes(paths.Slot(3));
        Assert.True(SaveSlots.Write(paths, 1, Named("Lucas", 4444)));
        Assert.Equal(new[] { "save1.bak", "save1.json", "save3.json" }, folder.Files());
        Assert.Equal(3000, SaveManager.Read(File.ReadAllText(SaveManager.BackupOf(paths.Slot(1))))!.Money);
        Assert.Equal(4444, SaveSlots.Read(paths, 1).Save!.Money);
        Assert.Equal(third, File.ReadAllBytes(paths.Slot(3)));

        // With all three taken a new game has no slot
        Assert.True(SaveSlots.Write(paths, 2, Named("Dawn")));
        Assert.Null(SaveSlots.FirstEmpty(paths));
        Assert.All(SaveSlots.Read(paths), slot => Assert.True(SaveManager.Intact(File.ReadAllText(paths.Slot(slot.Number)))));
    }

    [Fact]
    public void TheFirstSaveOfAFreshInstallMakesTheFolder()
    {
        using var folder = new Folder();
        var paths = SavePaths.In(Path.Combine(folder.Path, "not", "there", "yet"));
        // Reading a folder that isn't there finds nothing and makes nothing
        Assert.All(SaveSlots.Read(paths), slot => Assert.Equal(SaveLoad.None, slot.Outcome));
        Assert.Equal(1, SaveSlots.FirstEmpty(paths));
        Assert.Equal(OldSaveMove.Nothing, SaveSlots.AdoptOldSave(paths).Move);
        Assert.True(SaveSlots.Delete(paths, 2));
        Assert.False(Directory.Exists(paths.Folder));

        Assert.True(SaveSlots.Write(paths, 1, Named("Lucas")));
        Assert.Equal("Lucas", Who(paths, 1));
    }

    [Fact]
    public void ASlotDeletedIsGoneAndItsBackupWithIt()
    {
        using var folder = new Folder();
        var paths = folder.Paths;
        foreach (int slot in new[] { 1, 2, 3 })
        {
            SaveSlots.Write(paths, slot, Named($"Player {slot}", 1000 * slot));
            SaveSlots.Write(paths, slot, Named($"Player {slot}", 1000 * slot + 1));
        }
        // What a save cut short left beside the second slot goes too
        File.WriteAllText(SaveManager.TempOf(paths.Slot(2)), "{ \"PlayerName\": \"Half");
        Assert.Equal(new[] { "save1.bak", "save1.json", "save2.bak", "save2.json", "save2.json.tmp", "save3.bak", "save3.json" }, folder.Files());
        byte[] first = File.ReadAllBytes(paths.Slot(1)), firstBackup = File.ReadAllBytes(SaveManager.BackupOf(paths.Slot(1)));

        Assert.True(SaveSlots.Delete(paths, 2));
        Assert.Equal(new[] { "save1.bak", "save1.json", "save3.bak", "save3.json" }, folder.Files());
        var gone = SaveSlots.Read(paths, 2);
        Assert.Equal((SaveLoad.None, false, null), (gone.Outcome, gone.Taken, gone.Save));
        Assert.True(SaveSlots.IsEmpty(paths, 2));
        Assert.Equal(2, SaveSlots.FirstEmpty(paths));

        // The others are byte for byte what they were, and still read as theirs
        Assert.Equal(first, File.ReadAllBytes(paths.Slot(1)));
        Assert.Equal(firstBackup, File.ReadAllBytes(SaveManager.BackupOf(paths.Slot(1))));
        Assert.Equal(("Player 1", "Player 3"), (Who(paths, 1), Who(paths, 3)));

        // Deleting what isn't there is no mistake, and a slot that is only a backup goes whole too
        Assert.True(SaveSlots.Delete(paths, 2));
        File.Delete(paths.Slot(3));
        Assert.Equal(SaveLoad.Recovered, SaveSlots.Read(paths, 3).Outcome);
        Assert.True(SaveSlots.Delete(paths, 3));
        Assert.Equal(new[] { "save1.bak", "save1.json" }, folder.Files());
    }

    [Fact]
    public void ASlotWhoseFilesAreHeldIsNotCalledDeleted()
    {
        using var folder = new Folder();
        var paths = folder.Paths;
        SaveSlots.Write(paths, 1, Named("Lucas"));
        // Something else has the save open and won't share it: nothing is left half done that reads as another game
        using (new FileStream(paths.Slot(1), FileMode.Open, FileAccess.Read, FileShare.None))
        {
            if (!OperatingSystem.IsWindows()) return;   // only Windows refuses to delete an open file
            Assert.False(SaveSlots.Delete(paths, 1));
            Assert.False(SaveSlots.IsEmpty(paths, 1));
        }
        Assert.Equal("Lucas", Who(paths, 1));
        Assert.True(SaveSlots.Delete(paths, 1));
    }

    [Fact]
    public void ASlotThatCantBeReadIsStillTaken()
    {
        using var folder = new Folder();
        var paths = folder.Paths;
        SaveSlots.Write(paths, 1, Named("Earlier"));
        SaveSlots.Write(paths, 1, Named("Later"));
        SaveSlots.Write(paths, 2, Named("Dawn"));

        // The save cut short: the one before it is the slot's game
        File.WriteAllText(paths.Slot(1), File.ReadAllText(paths.Slot(1))[..40]);
        var recovered = SaveSlots.Read(paths, 1);
        Assert.Equal((SaveLoad.Recovered, "Earlier", true), (recovered.Outcome, recovered.Save!.PlayerName, recovered.Taken));

        // Both damaged: no game, and the slot is no place for a new one until it is deleted
        File.WriteAllText(SaveManager.BackupOf(paths.Slot(1)), "not a save");
        var damaged = SaveSlots.Read(paths, 1);
        Assert.Equal((SaveLoad.Damaged, null, true), (damaged.Outcome, damaged.Save, damaged.Taken));
        Assert.Equal(3, SaveSlots.FirstEmpty(paths));
        Assert.Equal("Dawn", Who(paths, 2));

        Assert.True(SaveSlots.Delete(paths, 1));
        Assert.Equal(1, SaveSlots.FirstEmpty(paths));
    }

    // ------------------------------------------------------------------ the save from before there were slots

    [Fact]
    public void TheOldSaveBecomesSlotOneByteForByteWithItsBackup()
    {
        using var folder = new Folder();
        var paths = folder.Paths;
        folder.OldInstall();
        byte[] save = File.ReadAllBytes(folder.OldSave), backup = File.ReadAllBytes(folder.OldBackup);

        var moved = SaveSlots.AdoptOldSave(paths);
        Assert.Equal(new OldSaveOutcome(OldSaveMove.Moved, 1), moved);
        // Renamed, not copied: the old names are gone, nothing else is left, and each file is what it was
        Assert.Equal(new[] { "save1.bak", "save1.json" }, folder.Files());
        Assert.Equal(save, File.ReadAllBytes(paths.Slot(1)));
        Assert.Equal(backup, File.ReadAllBytes(SaveManager.BackupOf(paths.Slot(1))));

        // So its checksum still holds, and it loads as the game it was, with the save before it behind it
        Assert.True(SaveManager.Intact(File.ReadAllText(paths.Slot(1))));
        Assert.NotNull(SaveManager.Read(File.ReadAllText(paths.Slot(1)))!.Checksum);
        var slot = SaveSlots.Read(paths, 1);
        Assert.Equal((SaveLoad.Ok, "Later"), (slot.Outcome, slot.Save!.PlayerName));
        File.WriteAllText(paths.Slot(1), "cut sho");
        Assert.Equal((SaveLoad.Recovered, "Earlier"), (SaveSlots.Read(paths, 1).Outcome, Who(paths, 1)));
    }

    [Fact]
    public void TheMoveHappensOnce()
    {
        using var folder = new Folder();
        var paths = folder.Paths;
        folder.OldInstall();
        Assert.Equal(OldSaveMove.Moved, SaveSlots.AdoptOldSave(paths).Move);
        byte[] save = File.ReadAllBytes(paths.Slot(1)), backup = File.ReadAllBytes(SaveManager.BackupOf(paths.Slot(1)));

        // Every start after it finds nothing to move and changes nothing
        for (int start = 0; start < 3; start++)
        {
            Assert.Equal(new OldSaveOutcome(OldSaveMove.Nothing), SaveSlots.AdoptOldSave(paths));
            Assert.Equal(new[] { "save1.bak", "save1.json" }, folder.Files());
            Assert.Equal(save, File.ReadAllBytes(paths.Slot(1)));
            Assert.Equal(backup, File.ReadAllBytes(SaveManager.BackupOf(paths.Slot(1))));
        }
        // And a folder with no old save at all is left alone
        using var fresh = new Folder();
        Assert.Equal(OldSaveMove.Nothing, SaveSlots.AdoptOldSave(fresh.Paths).Move);
        Assert.Empty(fresh.Files());
    }

    [Fact]
    public void AnOldSaveWithNoBackupMovesAlone()
    {
        using var folder = new Folder();
        var paths = folder.Paths;
        SaveManager.SaveGame(Named("Once"), folder.OldSave);
        Assert.Equal(new OldSaveOutcome(OldSaveMove.Moved, 1), SaveSlots.AdoptOldSave(paths));
        Assert.Equal(new[] { "save1.json" }, folder.Files());
        Assert.Equal("Once", Who(paths, 1));
    }

    [Fact]
    public void AnOldBackupWithNoSaveBecomesASlotThatIsRecovered()
    {
        // An old install whose save was lost and whose backup was not: the game before the last is still a game
        using var folder = new Folder();
        var paths = folder.Paths;
        folder.OldInstall();
        File.Delete(folder.OldSave);

        Assert.Equal(new OldSaveOutcome(OldSaveMove.Moved, 1), SaveSlots.AdoptOldSave(paths));
        Assert.Equal(new[] { "save1.bak" }, folder.Files());
        var slot = SaveSlots.Read(paths, 1);
        Assert.Equal((SaveLoad.Recovered, "Earlier", true), (slot.Outcome, slot.Save!.PlayerName, slot.Taken));
    }

    [Fact]
    public void TheMoveNeverOverwritesASlot()
    {
        using var folder = new Folder();
        var paths = folder.Paths;
        SaveSlots.Write(paths, 1, Named("In slot one"));
        byte[] first = File.ReadAllBytes(paths.Slot(1));
        folder.OldInstall();

        // Slot 1 holds a game: the old save takes the first slot that holds nothing
        Assert.Equal(new OldSaveOutcome(OldSaveMove.Moved, 2), SaveSlots.AdoptOldSave(paths));
        Assert.Equal(new[] { "save1.json", "save2.bak", "save2.json" }, folder.Files());
        Assert.Equal(first, File.ReadAllBytes(paths.Slot(1)));
        Assert.Equal(("In slot one", "Later"), (Who(paths, 1), Who(paths, 2)));

        // A slot that is only a backup, or can't be read at all, is taken too
        SaveSlots.Write(paths, 3, Named("Third"));
        SaveSlots.Write(paths, 3, Named("Third"));
        File.Delete(paths.Slot(3));
        folder.OldInstall("Older", "Old");
        byte[] old = File.ReadAllBytes(folder.OldSave), oldBackup = File.ReadAllBytes(folder.OldBackup);
        string[] before = folder.Files();

        // With every slot taken the old save is left exactly where it is, and nothing else is changed
        var left = SaveSlots.AdoptOldSave(paths);
        Assert.Equal(new OldSaveOutcome(OldSaveMove.NoRoom), left);
        Assert.Equal(before, folder.Files());
        Assert.Equal(old, File.ReadAllBytes(folder.OldSave));
        Assert.Equal(oldBackup, File.ReadAllBytes(folder.OldBackup));
        Assert.Equal(("In slot one", "Later", "Third"), (Who(paths, 1), Who(paths, 2), Who(paths, 3)));

        // Once a slot is deleted the next start gives it to the old save
        Assert.True(SaveSlots.Delete(paths, 1));
        Assert.Equal(new OldSaveOutcome(OldSaveMove.Moved, 1), SaveSlots.AdoptOldSave(paths));
        Assert.Equal(("Old", "Later", "Third"), (Who(paths, 1), Who(paths, 2), Who(paths, 3)));
        Assert.DoesNotContain("savegame.json", folder.Files());
    }

    /// <summary>How far a move to slot 2 got before the game stopped: the note written, then the save moved, then its backup.</summary>
    public enum CutShort { NoteWritten, SaveMoved, BothMoved }

    [Theory]
    [InlineData(CutShort.NoteWritten)]
    [InlineData(CutShort.SaveMoved)]
    [InlineData(CutShort.BothMoved)]
    public void AMoveCutShortIsFinishedAtTheNextStartToTheSlotItWasGoingTo(CutShort reached)
    {
        using var folder = new Folder();
        var paths = folder.Paths;
        // Slot 1 holds another game, saved once, so it has no backup of its own: the old save was on its way to slot 2
        SaveSlots.Write(paths, 1, Named("Another game"));
        byte[] another = File.ReadAllBytes(paths.Slot(1));
        folder.OldInstall();
        byte[] save = File.ReadAllBytes(folder.OldSave), backup = File.ReadAllBytes(folder.OldBackup);

        // The game stopped half-way: the note it wrote first says where the save was going
        File.WriteAllText(folder.Note, "2");
        if (reached >= CutShort.SaveMoved) File.Move(folder.OldSave, paths.Slot(2));
        if (reached >= CutShort.BothMoved) File.Move(folder.OldBackup, SaveManager.BackupOf(paths.Slot(2)));

        // Whatever was reached, the slots read as games at every moment: nothing is ever half a save
        Assert.Equal("Another game", Who(paths, 1));

        SaveSlots.AdoptOldSave(paths);
        Assert.Equal(new[] { "save1.json", "save2.bak", "save2.json" }, folder.Files());
        Assert.Equal(save, File.ReadAllBytes(paths.Slot(2)));
        Assert.Equal(backup, File.ReadAllBytes(SaveManager.BackupOf(paths.Slot(2))));
        Assert.Equal((SaveLoad.Ok, "Later"), (SaveSlots.Read(paths, 2).Outcome, Who(paths, 2)));
        // The other game is what it was, and the old save's backup did not end up beside it
        Assert.Equal(another, File.ReadAllBytes(paths.Slot(1)));
        Assert.False(File.Exists(SaveManager.BackupOf(paths.Slot(1))));
        // And it is over: the next start finds nothing to do
        Assert.Equal(OldSaveMove.Nothing, SaveSlots.AdoptOldSave(paths).Move);
    }

    [Fact]
    public void WithoutANoteAnOldBackupIsNeverPutBesideAnotherGamesSave()
    {
        using var folder = new Folder();
        var paths = folder.Paths;
        SaveSlots.Write(paths, 1, Named("Another game"));
        folder.OldInstall();
        File.Delete(folder.OldSave);

        // A backup with no save and no note: it is an old game of its own, and goes to a slot of its own
        Assert.Equal(new OldSaveOutcome(OldSaveMove.Moved, 2), SaveSlots.AdoptOldSave(paths));
        Assert.Equal(new[] { "save1.json", "save2.bak" }, folder.Files());
        Assert.Equal((SaveLoad.Ok, "Another game"), (SaveSlots.Read(paths, 1).Outcome, Who(paths, 1)));
        Assert.Equal((SaveLoad.Recovered, "Earlier"), (SaveSlots.Read(paths, 2).Outcome, Who(paths, 2)));
    }

    [Theory]
    [InlineData("")]            // cut short as it was written: nothing had been moved yet
    [InlineData("7")]           // no slot
    [InlineData("slot two")]
    public void ANoteThatSaysNothingIsPassedOver(string note)
    {
        using var folder = new Folder();
        var paths = folder.Paths;
        folder.OldInstall();
        File.WriteAllText(folder.Note, note);

        Assert.Equal(new OldSaveOutcome(OldSaveMove.Moved, 1), SaveSlots.AdoptOldSave(paths));
        Assert.Equal(new[] { "save1.bak", "save1.json" }, folder.Files());
        Assert.Equal("Later", Who(paths, 1));

        // A note with no old save left to move is only taken away
        File.WriteAllText(folder.Note, "3");
        Assert.Equal(OldSaveMove.Nothing, SaveSlots.AdoptOldSave(paths).Move);
        Assert.Equal(new[] { "save1.bak", "save1.json" }, folder.Files());
    }

    [Fact]
    public void AnOldSaveThatCantBeMovedIsLeftAsItIsAndTriedAgain()
    {
        using var folder = new Folder();
        var paths = folder.Paths;
        folder.OldInstall();
        byte[] save = File.ReadAllBytes(folder.OldSave);
        if (!OperatingSystem.IsWindows()) return;   // only Windows refuses to rename an open file

        using (new FileStream(folder.OldSave, FileMode.Open, FileAccess.Read, FileShare.None))
        {
            var failed = SaveSlots.AdoptOldSave(paths);
            Assert.Equal(OldSaveMove.Failed, failed.Move);
            Assert.False(string.IsNullOrWhiteSpace(failed.Problem));
            // Nothing is lost and no slot is half made
            Assert.All(SaveSlots.Read(paths), slot => Assert.Equal(SaveLoad.None, slot.Outcome));
        }
        Assert.Equal(save, File.ReadAllBytes(folder.OldSave));

        // The next start finishes what this one began
        Assert.Equal(new OldSaveOutcome(OldSaveMove.Moved, 1), SaveSlots.AdoptOldSave(paths));
        Assert.Equal(new[] { "save1.bak", "save1.json" }, folder.Files());
        Assert.Equal(save, File.ReadAllBytes(paths.Slot(1)));
    }

    [Fact]
    public void EverySaveOfTheCorpusBecomesASlotAndLoadsAsItDid()
    {
        // The saves of every version the game has written, as an old install would have one: none has a checksum
        // before plan 12 · Q1, and each must still be the same game once it is slot 1
        string corpus = Path.Combine(AppContext.BaseDirectory, "Fixtures", "saves");
        var files = Directory.GetFiles(corpus, "*.json");
        Assert.NotEmpty(files);
        foreach (string file in files)
        {
            using var folder = new Folder();
            File.Copy(file, folder.OldSave);
            byte[] bytes = File.ReadAllBytes(file);

            Assert.Equal(new OldSaveOutcome(OldSaveMove.Moved, 1), SaveSlots.AdoptOldSave(folder.Paths));
            Assert.Equal(bytes, File.ReadAllBytes(folder.Paths.Slot(1)));
            var slot = SaveSlots.Read(folder.Paths, 1);
            Assert.True(slot.Outcome == SaveLoad.Ok, $"{Path.GetFileName(file)} reads as {slot.Outcome} once it is a slot");
            Assert.Equal(SaveManager.Write(SaveManager.Read(File.ReadAllText(file))!), SaveManager.Write(slot.Save!));
        }
    }
}
