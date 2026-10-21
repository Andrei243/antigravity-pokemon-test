using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Foreign;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 08 · P22: Pokémon moved between this game's saves (with a receipt in both, so that a crash in the middle is
/// finished at the next start), and a Pokémon written out as a .pk4 file or as Showdown's text. Saves are made in folders of
/// their own; the whole process shares <see cref="SavePaths.Current"/>, so the tests that use it run alone.
/// </summary>
[Collection("Shared clocks and dice")]
public class SlotMigrationTests : IDisposable
{
    private readonly string folder = Path.Combine(Path.GetTempPath(), "slot-moves-" + Guid.NewGuid().ToString("N"));
    private readonly SavePaths paths;

    public SlotMigrationTests()
    {
        Directory.CreateDirectory(folder);
        paths = SavePaths.In(folder);
    }

    public void Dispose()
    {
        try { Directory.Delete(folder, recursive: true); } catch (IOException) { }
    }

    private static Pokemon Mon(int dex, int level = 20)
    {
        var mon = new Pokemon(PokemonDatabase.GetByDex(dex)!, level, 1000u + (uint)dex, new Random(dex)) { Id = Guid.NewGuid() };
        return mon;
    }

    /// <summary>A save with <paramref name="count"/> Pokémon in its first box, species 1 upward.</summary>
    private static SaveData SaveWith(string name, int count, bool national = true, int firstDex = 1)
    {
        var box = new SavedBox { Name = "BOX 1" };
        for (int i = 0; i < count; i++) box.Pokemon.Add(new SavedBoxedPokemon { Slot = i, Pokemon = SavedPokemonData.FromPokemon(Mon(firstDex + i)) });
        return new SaveData { PlayerName = name, NationalPokedex = national, Boxes = new SavedBoxes { Boxes = { box } } };
    }

    private static List<Guid> IdsOf(SaveData save, int count) =>
        SlotMoves.Boxed(save).Select(b => b.Place.Pokemon.Id!.Value).Take(count).ToList();

    private void Write(int slot, SaveData save) => Assert.True(SaveSlots.Write(paths, slot, save));

    private SaveData Read(int slot)
    {
        var read = SaveSlots.Read(paths, slot);
        Assert.Equal(SaveLoad.Ok, read.Outcome);
        return read.Save!;
    }

    // ------------------------------------------------------------------ a move

    [Fact]
    public void SixPokemonOfOneSaveWaitInTheParkOfAnotherAndAreNoLongerInTheFirst()
    {
        var source = SaveWith("Lucas", 8);
        Write(1, source);
        Write(2, SaveWith("Dawn", 0));
        var ids = IdsOf(source, 6);

        Assert.Equal(MigrationBlock.None, SlotMoves.Move(paths, 1, 2, ids));

        var (from, to) = (Read(1), Read(2));
        Assert.Equal(2, SlotMoves.Boxed(from).Count());
        Assert.Equal(ids.OrderBy(i => i), to.Pastimes!.Waiting!.Select(p => p.Id!.Value).OrderBy(i => i));
        Assert.Null(from.Receipts);
        Assert.Null(to.Receipts);
        Assert.Empty(SlotMoves.Held(from).Intersect(SlotMoves.Held(to)));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void AMoveCutShortByACrashIsFinishedByTheNextStartWhereverItStopped(int crashedAfter)
    {
        var source = SaveWith("Lucas", 8);
        Write(1, source);
        Write(2, SaveWith("Dawn", 0));
        var ids = IdsOf(source, 6);

        Assert.Equal(MigrationBlock.CouldNotSave, SlotMoves.Move(paths, 1, 2, ids, write => write == crashedAfter));
        Assert.Equal(1, SlotMoves.Resolve(paths));

        var (from, to) = (Read(1), Read(2));
        // Every Pokémon is in exactly one save, and no receipt is left open
        Assert.Equal(2, SlotMoves.Boxed(from).Count());
        Assert.Equal(6, to.Pastimes!.Waiting!.Count);
        Assert.Empty(SlotMoves.Held(from).Intersect(SlotMoves.Held(to)));
        Assert.Null(from.Receipts);
        Assert.Null(to.Receipts);
        // And a start with nothing to finish finishes nothing
        Assert.Equal(0, SlotMoves.Resolve(paths));
    }

    [Fact]
    public void AMoveWhoseReceivingSaveIsGoneGivesTheSixBackToTheSave()
    {
        var source = SaveWith("Lucas", 8);
        Write(1, source);
        Write(2, SaveWith("Dawn", 0));
        var ids = IdsOf(source, 6);
        Assert.Equal(MigrationBlock.CouldNotSave, SlotMoves.Move(paths, 1, 2, ids, write => write == 1));

        // The player deletes the receiving slot before the game starts again
        Assert.True(SaveSlots.Delete(paths, 2));
        SlotMoves.Resolve(paths);

        var from = Read(1);
        Assert.Equal(8, SlotMoves.Boxed(from).Count());
        Assert.Equal(ids.Concat(SlotMoves.Boxed(source).Select(b => b.Place.Pokemon.Id!.Value)).Distinct().OrderBy(i => i),
            SlotMoves.Held(from).OrderBy(i => i));
        Assert.Null(from.Receipts);
    }

    [Fact]
    public void AReceivingSlotThatIsAnotherGameNowIsTreatedAsGone()
    {
        var source = SaveWith("Lucas", 8);
        Write(1, source);
        Write(2, SaveWith("Dawn", 0));
        var ids = IdsOf(source, 6);
        // Crashed after the source let go of them, and the receiving slot was deleted and a new game saved in it
        Assert.Equal(MigrationBlock.CouldNotSave, SlotMoves.Move(paths, 1, 2, ids, write => write == 2));
        Assert.True(SaveSlots.Delete(paths, 2));
        Write(2, new SaveData { PlayerName = "New", NationalPokedex = true });

        SlotMoves.Resolve(paths);

        Assert.Equal(8, SlotMoves.Boxed(Read(1)).Count());
        Assert.Null(Read(1).Receipts);
        Assert.Null(Read(2).Pastimes?.Waiting);
    }

    [Fact]
    public void AMoveIsRefusedWithoutTheNationalPokedexWithAnOccupiedParkWithTooFewOrWithAnEggAndNothingIsWritten()
    {
        var source = SaveWith("Lucas", 8);
        Write(1, source);
        var ids = IdsOf(source, 6);
        byte[] before = File.ReadAllBytes(paths.Slot(1));

        Write(2, SaveWith("Dawn", 0, national: false));
        Assert.Equal(MigrationBlock.NeedsNationalPokedex, SlotMoves.Move(paths, 1, 2, ids));

        var stocked = SaveWith("Dawn", 0);
        stocked.Pastimes = new PastimesSave { Waiting = new() { SavedPokemonData.FromPokemon(Mon(150)) } };
        Write(2, stocked);
        Assert.Equal(MigrationBlock.AlreadyStocked, SlotMoves.Move(paths, 1, 2, ids));

        Write(2, SaveWith("Dawn", 0));
        Assert.Equal(MigrationBlock.NotSix, SlotMoves.Move(paths, 1, 2, ids.Take(5).ToList()));
        Assert.Equal(MigrationBlock.Refused, SlotMoves.Move(paths, 1, 2, ids.Take(5).Append(Guid.NewGuid()).ToList()));

        // An Egg can't come over
        var withEgg = SaveWith("Lucas", 8);
        withEgg.Boxes!.Boxes[0].Pokemon[0].Pokemon.IsEgg = true;
        Write(1, withEgg);
        Assert.Equal(MigrationBlock.Refused, SlotMoves.Move(paths, 1, 2, IdsOf(withEgg, 6)));

        Assert.Null(Read(2).Pastimes);
        Assert.Null(Read(2).Receipts);
        Write(1, source);
        Assert.Equal(before.Length, File.ReadAllBytes(paths.Slot(1)).Length);
    }

    [Fact]
    public void APokemonTheReceivingSaveAlreadyHasCannotComeOverAgain()
    {
        var source = SaveWith("Lucas", 8);
        Write(1, source);
        var ids = IdsOf(source, 6);
        var target = SaveWith("Dawn", 0);
        // The same Pokémon, in the receiving save's boxes already (a copy of the file was kept)
        target.Boxes!.Boxes[0].Pokemon.Add(new SavedBoxedPokemon { Slot = 0, Pokemon = source.Boxes!.Boxes[0].Pokemon[0].Pokemon });
        Write(2, target);

        Assert.Equal(MigrationBlock.Refused, SlotMoves.Move(paths, 1, 2, ids));
        Assert.Equal(8, SlotMoves.Boxed(Read(1)).Count());
    }

    // ------------------------------------------------------------------ the way out

    [Fact]
    public void APokemonWrittenAsAPk4IsReadBackAsItWas()
    {
        var mon = Mon(25, 30);
        mon.Nickname = "SPARKY";
        mon.EvHP = 12; mon.EvSpeed = 200;
        mon.IvAttack = 3; mon.IvSpDefense = 29;

        var bytes = DsPokemon.Write(mon, 0x1234_5678, "LUCAS", false)!;
        Assert.Equal(136, bytes.Length);
        Assert.True(DsPokemon.IsPlain(bytes));

        var back = DsPokemon.ReadPlain(bytes)!;
        Assert.Equal(25, back.Species);
        Assert.Equal(mon.Personality, back.Personality);
        Assert.Equal("SPARKY", back.Nickname);
        Assert.Equal("LUCAS", back.OtName);
        Assert.Equal(0x1234_5678u, back.OtId);
        Assert.Equal(new[] { 12, mon.EvAttack, mon.EvDefense, 200, mon.EvSpAttack, mon.EvSpDefense }, back.Evs.Take(6).ToArray());
        Assert.Equal(new[] { mon.IvHP, 3, mon.IvDefense, mon.IvSpeed, mon.IvSpAttack, 29 }, back.Ivs.Take(6).ToArray());
        Assert.Equal(mon.CurrentExp, back.Experience);

        var saved = back.ToSaved(Ruleset.Modern);
        Assert.Equal("Pikachu", saved.SpeciesName);
        Assert.Equal(30, saved.Level);
        Assert.Equal(mon.Moves.Select(m => m.Name), saved.Moves.Select(m => m.MoveName));
    }

    [Fact]
    public void ASpeciesOfALaterGameCannotBeWrittenAsAPk4ButCanAsText()
    {
        var later = Mon(700);
        Assert.Null(DsPokemon.Write(later, 1, "LUCAS", false));
        Assert.False(MigrateScreen.CanBePk4(Migration.Of(SavedPokemonData.FromPokemon(later), new SaveData())));
        Assert.StartsWith(later.Species.Name, ShowdownText.Of(later));
    }

    [Fact]
    public void ShowdownTextHasTheNameItemAbilityLevelEffortNatureIvsAndMovesInTheirOrder()
    {
        var mon = Mon(25, 50);
        mon.Nickname = "";
        mon.Gender = Gender.Male;
        mon.Nature = Nature.Timid;
        mon.Friendship = 255;
        mon.EvHP = 0; mon.EvAttack = 0; mon.EvDefense = 0; mon.EvSpAttack = 252; mon.EvSpDefense = 0; mon.EvSpeed = 252;
        mon.IvHP = 31; mon.IvAttack = 0; mon.IvDefense = 31; mon.IvSpAttack = 31; mon.IvSpDefense = 31; mon.IvSpeed = 31;
        mon.HeldItem = ItemDatabase.Get("Light Ball");

        var lines = ShowdownText.Of(mon).Split('\n');
        Assert.Equal("Pikachu (M) @ Light Ball", lines[0]);
        Assert.Equal($"Ability: {mon.AbilityName}", lines[1]);
        Assert.Equal("Level: 50", lines[2]);
        Assert.Equal("EVs: 252 SpA / 252 Spe", lines[3]);
        Assert.Equal("Timid Nature", lines[4]);
        Assert.Equal("IVs: 0 Atk", lines[5]);
        Assert.All(lines.Skip(6), l => Assert.StartsWith("- ", l));
        Assert.Equal(mon.Moves.Count, lines.Length - 6);

        mon.Nickname = "Zap";
        Assert.StartsWith("Zap (Pikachu) (M)", ShowdownText.Of(mon));
    }

    [Fact]
    public void AFileWrittenOutIsNeverWrittenOverAndNothingElseInTheFolderIsTouched()
    {
        string migrate = Path.Combine(folder, "migrate");
        Directory.CreateDirectory(migrate);
        string theirs = Path.Combine(migrate, "emerald.sav");
        File.WriteAllBytes(theirs, new byte[] { 1, 2, 3 });
        var save = SaveWith("Lucas", 3);
        var saved = save.Boxes!.Boxes[0].Pokemon[0].Pokemon;

        string? first = MigrationOut.Write(migrate, save, saved, WriteFormat.Pk4);
        string? second = MigrationOut.Write(migrate, save, saved, WriteFormat.Pk4);
        string? text = MigrationOut.Write(migrate, save, saved, WriteFormat.Showdown);

        Assert.EndsWith(".pk4", first);
        Assert.EndsWith(".pk4", second);
        Assert.NotEqual(first, second);
        Assert.EndsWith(".txt", text);
        Assert.Equal(136, new FileInfo(Path.Combine(migrate, "out", first!)).Length);
        Assert.StartsWith("Bulbasaur", File.ReadAllText(Path.Combine(migrate, "out", text!)));
        Assert.Equal(new byte[] { 1, 2, 3 }, File.ReadAllBytes(theirs));
        Assert.Equal(new[] { "emerald.sav", "out" }, Directory.GetFileSystemEntries(migrate).Select(Path.GetFileName).Order().ToArray());
    }

    // ------------------------------------------------------------------ the screen and the game

    private static MigrateScreen Screen(params (int Slot, SaveData Save)[] saves)
    {
        var screen = new MigrateScreen();
        var targets = saves.Select(s => new MigrateTarget(s.Slot, s.Save)).ToList();
        var sources = MigrationFolder.Slots(saves);
        screen.Open(targets, sources, () => new DateTime(2026, 10, 21, 12, 0, 0, DateTimeKind.Utc), withMenu: true);
        return screen;
    }

    [Fact]
    public void TheScreenOpensOnWhatToDoAndWritingOutLeadsFromASaveToAPokemonToAFormat()
    {
        var screen = Screen((1, SaveWith("Lucas", 3)), (2, SaveWith("Dawn", 0)));
        Assert.Equal(MigratePhase.Menu, screen.Phase);

        screen.Move(0, 1);
        screen.Confirm();
        Assert.Equal(MigratePhase.Save, screen.Phase);
        screen.Confirm();                      // slot 1
        Assert.Equal(MigratePhase.Box, screen.Phase);
        Assert.Equal(MigrateMode.WriteOut, screen.Mode);
        screen.Move(1, 0);                      // the team's box is first: its places are empty; the boxes follow
        screen.ChangeBox(1);
        Assert.NotNull(screen.Under);
        screen.Confirm();
        Assert.Equal(MigratePhase.Format, screen.Phase);
        Assert.Equal(WriteFormat.Pk4, screen.Format);
        screen.Move(0, 1);
        Assert.Equal(WriteFormat.Showdown, screen.Format);
        screen.Confirm();
        Assert.Equal(MigratePhase.Working, screen.Phase);

        var request = screen.TakeWriteOut()!;
        Assert.Equal(1, request.From.Slot);
        Assert.Equal(WriteFormat.Showdown, request.Format);
        Assert.Null(screen.TakeWriteOut());

        screen.FinishWriteOut("Ivysaur-000003EA.txt");
        Assert.Equal(MigratePhase.Result, screen.Phase);
        Assert.Equal("Ivysaur-000003EA.txt", screen.Wrote);
    }

    [Fact]
    public void TheOtherSavesAreSourcesToABringingSaveButNotItself()
    {
        var screen = Screen((1, SaveWith("Lucas", 8)), (2, SaveWith("Dawn", 0)));
        screen.Confirm();                       // bring Pokémon over
        Assert.Equal(MigratePhase.Save, screen.Phase);
        screen.Move(0, 1);
        screen.Confirm();                       // into slot 2
        Assert.Equal(MigratePhase.Files, screen.Phase);
        Assert.Equal(1, Assert.Single(screen.Sources).Slot);
        Assert.Equal(MigrationBlock.None, screen.BlockOf(screen.Sources[0]));
    }

    [Fact]
    public void ThroughTheWholeGameSixAreMovedBetweenSlotsAndAPokemonIsWrittenOut()
    {
        var before = SavePaths.Current;
        try
        {
            SavePaths.Current = paths;
            var source = SaveWith("Lucas", 8);
            Write(1, source);
            Write(2, SaveWith("Dawn", 0));

            var engine = new GameEngine(new GameSettings { Muted = true });
            engine.InitializeHeadless();
            var game = engine.Drive;
            // Two saves are enough to offer MIGRATE, with no folder at all
            Assert.Contains(TitleChoice.Migrate, game.Title.Entries);

            game.OpenMigrate();
            var screen = game.Migrate;
            screen.Confirm();
            screen.Move(0, 1);
            screen.Confirm();
            Assert.Equal(MigratePhase.Files, screen.Phase);
            screen.Confirm();
            for (int i = 0; i < 6; i++) { screen.Confirm(); screen.Move(1, 0); }
            screen.Done();
            screen.Move(1, 0);
            screen.Confirm();
            Assert.Equal(MigratePhase.Working, screen.Phase);
            engine.Update(1f / 60f);
            Assert.Equal(MigratePhase.Result, screen.Phase);
            Assert.Equal(MigrationBlock.None, screen.Outcome);

            Assert.Equal(2, SlotMoves.Boxed(Read(1)).Count());
            Assert.Equal(6, Read(2).Pastimes!.Waiting!.Count);

            // And one written out of the save the six came from
            var saved = Read(1).Boxes!.Boxes[0].Pokemon[0].Pokemon;
            string? file = engine.CarryOutWriteOut(new WriteOutRequest(new MigrateTarget(1, Read(1)), saved, WriteFormat.Pk4));
            Assert.NotNull(file);
            Assert.True(File.Exists(Path.Combine(paths.Migrate, "out", file!)));
            Assert.Equal(2, SlotMoves.Boxed(Read(1)).Count());
        }
        finally
        {
            SavePaths.Current = before;
        }
    }

    [Fact]
    public void AStartFindingAMoveCutShortFinishesItBeforeTheTitleReadsTheSlots()
    {
        var before = SavePaths.Current;
        try
        {
            SavePaths.Current = paths;
            var source = SaveWith("Lucas", 8);
            Write(1, source);
            Write(2, SaveWith("Dawn", 0));
            SlotMoves.Move(paths, 1, 2, IdsOf(source, 6), write => write == 1);

            var engine = new GameEngine(new GameSettings { Muted = true });
            engine.InitializeHeadless();

            Assert.Equal(2, SlotMoves.Boxed(Read(1)).Count());
            Assert.Null(Read(1).Receipts);
            Assert.Null(Read(2).Receipts);
        }
        finally
        {
            SavePaths.Current = before;
        }
    }
}
