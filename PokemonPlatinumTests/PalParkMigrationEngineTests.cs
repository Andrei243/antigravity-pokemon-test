using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Foreign;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI;
using static PokemonPlatinumTests.MigrationTests;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 08 · P21 through the whole game with no window: the title offers MIGRATE while the folder beside the saves holds
/// a file the readers know, the screen brings six into a save's park, the save is written, and the file is as it was.
/// It keeps its saves in a folder of its own, which the whole process shares, so it runs alone.
/// </summary>
[Collection("Shared clocks and dice")]
public class PalParkMigrationEngineTests
{
    private static readonly DateTime Noon = new(2026, 10, 21, 12, 0, 0, DateTimeKind.Utc);

    private static GameEngine Headless()
    {
        var engine = new GameEngine(new GameSettings { Muted = true });
        engine.InitializeHeadless();
        return engine;
    }

    private static byte[] GbaSaveOf(int count)
    {
        var mons = new Dictionary<(int, int), byte[]>();
        for (int i = 0; i < count; i++)
        {
            var species = PokemonPlatinumEngine.Data.PokemonDatabase.GetByDex(i + 1)!;
            var moves = species.Learnset.Select(m => PokemonPlatinumEngine.Data.MoveDatabase.Get(m.MoveName)).Where(m => m != null && m.Id is > 0 and <= 354)
                .Select(m => m!.Id).Distinct().Take(2).Concat(new[] { 0, 0, 0, 0 }).Take(4).ToArray();
            mons[(0, i)] = BuildGba(new GbaSpec(Personality: 1000u + (uint)i, Species: i + 1, Exp: Pokemon.GetExpForLevel(20, species.GrowthRate), Moves: moves));
        }
        return GbaFile(GbaSlot(GbaGame.Emerald, GbaStorage(mons), "BRENDAN", 0x1111_2222, 3));
    }

    [Fact]
    public void ASaveWithTheNationalPokedexIsGivenSixFromAFileInTheMigrateFolderAndTheFileIsLeftAlone()
    {
        string folder = Path.Combine(Path.GetTempPath(), "migrate-game-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var paths = SavePaths.Current;
        try
        {
            SavePaths.Current = SavePaths.In(folder);
            Assert.True(SaveSlots.Write(SavePaths.Current, 1, new SaveData { PlayerName = "Lucas", NationalPokedex = true }));
            string emerald = Path.Combine(SavePaths.Current.Migrate, "emerald.sav");

            // No folder, no MIGRATE: the title is the title it has always been
            var plain = Headless();
            Assert.DoesNotContain(TitleChoice.Migrate, plain.Drive.Title.Entries);
            Assert.False(Directory.Exists(SavePaths.Current.Migrate));

            Directory.CreateDirectory(SavePaths.Current.Migrate);
            File.WriteAllBytes(emerald, GbaSaveOf(8));
            var bytes = File.ReadAllBytes(emerald);
            var written = File.GetLastWriteTimeUtc(emerald);

            var engine = Headless();
            var game = engine.Drive;
            engine.MigrationClock = () => Noon;
            Assert.Contains(TitleChoice.Migrate, game.Title.Entries);

            game.OpenMigrate();
            Assert.Equal(GameState.Migrate, game.State);
            var screen = game.Migrate;
            Assert.Equal(MigratePhase.Files, screen.Phase);
            screen.Confirm();
            for (int i = 0; i < 6; i++) { screen.Confirm(); screen.Move(1, 0); }
            screen.Done();
            screen.Move(1, 0);
            screen.Confirm();
            Assert.Equal(MigratePhase.Working, screen.Phase);

            // The engine carries the request out in its next frame
            engine.Update(1f / 60f);
            Assert.Equal(MigratePhase.Result, screen.Phase);
            Assert.Equal(MigrationBlock.None, screen.Outcome);

            // The save in the slot has the six waiting, and remembers where they came from
            var slot = SaveSlots.Read(SavePaths.Current, 1);
            Assert.Equal(SaveLoad.Ok, slot.Outcome);
            Assert.Equal(6, slot.Save!.Pastimes!.Waiting!.Count);
            Assert.Equal("Bulbasaur", slot.Save.Pastimes.Waiting[0].SpeciesName);
            Assert.Equal(0x1111_2222u, Assert.Single(slot.Save.Pastimes.Sources!).TrainerId);
            Assert.Equal(6, slot.Save.Pastimes.Came!.Count);
            Assert.Equal("Lucas", slot.Save.PlayerName);

            // The file is the file it was
            Assert.Equal(bytes, File.ReadAllBytes(emerald));
            Assert.Equal(written, File.GetLastWriteTimeUtc(emerald));
            Assert.Equal(new[] { "emerald.sav" }, Directory.GetFiles(SavePaths.Current.Migrate).Select(Path.GetFileName).ToArray());

            // The same file, the same day, is told to wait; the screen shows why
            screen.Confirm();
            Assert.False(screen.IsActive);
            engine.Update(1f / 60f);
            Assert.Equal(GameState.Title, game.State);
            game.OpenMigrate();
            Assert.Equal(MigrationBlock.WaitForADay, game.Migrate.BlockOf(game.Migrate.Sources[0]));
        }
        finally
        {
            SavePaths.Current = paths;
            try { Directory.Delete(folder, recursive: true); } catch (IOException) { }
        }
    }

    [Fact]
    public void ASaveThatCannotBeWrittenKeepsTheSixAtHomeAndSaysSo()
    {
        string folder = Path.Combine(Path.GetTempPath(), "migrate-game-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var paths = SavePaths.Current;
        try
        {
            SavePaths.Current = SavePaths.In(folder);
            Assert.True(SaveSlots.Write(SavePaths.Current, 1, new SaveData { PlayerName = "Lucas", NationalPokedex = true }));
            Directory.CreateDirectory(SavePaths.Current.Migrate);
            File.WriteAllBytes(Path.Combine(SavePaths.Current.Migrate, "emerald.sav"), GbaSaveOf(8));

            var engine = Headless();
            var game = engine.Drive;
            engine.MigrationClock = () => Noon;
            game.OpenMigrate();
            var screen = game.Migrate;
            screen.Confirm();
            for (int i = 0; i < 6; i++) { screen.Confirm(); screen.Move(1, 0); }
            screen.Done();
            screen.Move(1, 0);
            screen.Confirm();

            // The slot's file goes while the question is on screen: nothing can be read, so nothing is brought
            File.Delete(SavePaths.Current.Slot(1));
            File.Delete(SaveManager.BackupOf(SavePaths.Current.Slot(1)));
            engine.Update(1f / 60f);
            Assert.Equal(MigratePhase.Result, screen.Phase);
            Assert.Equal(MigrationBlock.CouldNotSave, screen.Outcome);
            Assert.False(screen.Brought);
            Assert.Null(game.Migrate.Target!.Save.Pastimes);
        }
        finally
        {
            SavePaths.Current = paths;
            try { Directory.Delete(folder, recursive: true); } catch (IOException) { }
        }
    }
}
