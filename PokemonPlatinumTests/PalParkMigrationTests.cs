using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Foreign;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI;
using static PokemonPlatinumTests.MigrationTests;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 08 · P21: Pal Park's migration as Platinum's. Every file is built byte by byte (<see cref="MigrationTests"/>); the
/// time is handed in, so a day is a number. It reads species and learnsets through the process's databases and the rules
/// in force, so it runs with the tests that share them.
/// </summary>
[Collection("Shared clocks and dice")]
public class PalParkMigrationTests
{
    private static readonly DateTime Noon = new(2026, 10, 21, 12, 0, 0, DateTimeKind.Utc);
    private const uint Brendan = 0x1111_2222;

    // ------------------------------------------------------------------ files and saves to bring from

    /// <summary>A Pokémon of the GBA's that Platinum would take: a level 20 of its species, knowing two moves its line teaches.</summary>
    private static byte[] Legal(int dex, uint personality, uint otId = 0x0001_0002, int item = 0, int[]? moves = null)
    {
        var species = PokemonDatabase.GetByDex(dex)!;
        var known = moves ?? species.Learnset.Select(m => MoveDatabase.Get(m.MoveName)).Where(m => m != null && m.Id is > 0 and <= 354)
            .Select(m => m!.Id).Distinct().Take(2).Concat(new[] { 0, 0 }).Take(4).ToArray();
        if (known.Length < 4) known = known.Concat(new int[4 - known.Length]).ToArray();
        return BuildGba(new GbaSpec(Personality: personality, OtId: otId, Species: dex, Exp: Pokemon.GetExpForLevel(20, species.GrowthRate), Item: item, Moves: known, Nickname: ""));
    }

    /// <summary>A GBA save with <paramref name="count"/> Pokémon in its first box, species 1 upward.</summary>
    private static byte[] SaveOf(int count, uint trainer = Brendan, Action<Dictionary<(int, int), byte[]>>? change = null)
    {
        var mons = new Dictionary<(int, int), byte[]>();
        for (int i = 0; i < count; i++) mons[(0, i)] = Legal(i + 1, 1000u + (uint)i);
        change?.Invoke(mons);
        return GbaFile(GbaSlot(GbaGame.Emerald, GbaStorage(mons), "BRENDAN", trainer, 3));
    }

    private static ForeignSave Read(byte[] file)
    {
        var (save, refusal) = ForeignFile.Read(file);
        Assert.Equal(ForeignRefusal.None, refusal);
        return save!;
    }

    private static PalParkState Park() => new Pastimes().PalPark;

    // ------------------------------------------------------------------ the Pokémon one by one

    [Fact]
    public void APokemonThePlatinumChecksAllowComesAndEachRefusalHasItsReason()
    {
        var ledger = new PalParkLedger();
        var save = Read(SaveOf(8, change: m =>
        {
            m[(0, 1)] = BuildGba(new GbaSpec(Species: 2, Egg: true));
            m[(0, 2)] = Legal(3, 3000, moves: new[] { 15, 0, 0, 0 });               // Cut
            m[(0, 3)] = Legal(4, 3001, item: MigrationDatabase.Data.InvalidItems[0]);
            m[(0, 4)] = BuildGba(new GbaSpec(Species: 252, Personality: 3002));      // a number between Celebi and Treecko
            m[(0, 5)] = Legal(6, 3003, moves: new[] { 33, 0, 0, 0 });               // Charizard has not been taught Tackle
        }));
        var pokemon = save.Boxes[0].Slots;

        Assert.True(Migration.WhyNot(pokemon[0]!, ledger).Allowed);
        Assert.Equal(MigrationRefusal.Egg, Migration.WhyNot(pokemon[1]!, ledger).Refusal);
        Assert.Equal(MigrationRefusal.KnowsAnHm, Migration.WhyNot(pokemon[2]!, ledger).Refusal);
        Assert.Equal(MigrationRefusal.KeyItem, Migration.WhyNot(pokemon[3]!, ledger).Refusal);
        Assert.Equal(MigrationRefusal.NotARealSpecies, Migration.WhyNot(pokemon[4]!, ledger).Refusal);

        var impossible = Migration.WhyNot(pokemon[5]!, ledger);
        Assert.Equal(MigrationRefusal.Impossible, impossible.Refusal);
        Assert.Contains("Tackle", impossible.Detail);

        // Every refusal has words, and the refused Pokémon's reason is the screen's
        foreach (var refusal in Enum.GetValues<MigrationRefusal>().Where(r => r != MigrationRefusal.None))
            Assert.NotEmpty(Migration.Words(new MigrationVerdict(refusal, "x")));
    }

    [Fact]
    public void APokemonThatHasComeBeforeIsRefusedByItsPersonalityAndItsTrainer()
    {
        var ledger = new PalParkLedger();
        var one = Read(SaveOf(6)).Boxes[0].Slots[0]!;
        Assert.True(Migration.WhyNot(one, ledger).Allowed);
        ledger.MarkCame(Migration.IdentityOf(one));
        Assert.Equal(MigrationRefusal.CameBefore, Migration.WhyNot(one, ledger).Refusal);

        // The same personality of another trainer is another Pokémon
        var other = Read(GbaFile(GbaSlot(GbaGame.Emerald, GbaStorage(new() { [(0, 0)] = Legal(1, 1000, otId: 0x0009_0009) }), "MAY", 5, 3))).Boxes[0].Slots[0]!;
        Assert.True(Migration.WhyNot(other, ledger).Allowed);
    }

    [Fact]
    public void ImpossibleExperienceAndEffortAreCaughtByTheLegalityCheck()
    {
        var species = PokemonDatabase.GetByDex(25)!;
        var tooMuchExp = Read(GbaFile(GbaSlot(GbaGame.Emerald, GbaStorage(new()
        {
            [(0, 0)] = BuildGba(new GbaSpec(Species: 25, Exp: Pokemon.GetExpForLevel(100, species.GrowthRate) + 1, Moves: new[] { 0, 0, 0, 0 })),
            [(0, 1)] = BuildGba(new GbaSpec(Species: 25, Personality: 77, Exp: 1000, Moves: new[] { 0, 0, 0, 0 }, Evs: new[] { 255, 255, 255, 255, 255, 255 }))
        }), "X", 1, 1))).Boxes[0].Slots;
        Assert.Equal(MigrationRefusal.Impossible, Migration.WhyNot(tooMuchExp[0]!, new PalParkLedger()).Refusal);
        Assert.Equal(MigrationRefusal.Impossible, Migration.WhyNot(tooMuchExp[1]!, new PalParkLedger()).Refusal);
    }

    // ------------------------------------------------------------------ the source as a whole

    [Fact]
    public void ASourceBringsNothingWithoutTheNationalPokedexOrSixInItsBoxes()
    {
        var park = Park();
        var enough = Read(SaveOf(8));
        var few = Read(SaveOf(5));
        Assert.Equal(MigrationBlock.NeedsNationalPokedex, Migration.WhyNotFrom(enough, false, park, Noon));
        Assert.Equal(MigrationBlock.TooFew, Migration.WhyNotFrom(few, true, park, Noon));
        Assert.Equal(MigrationBlock.None, Migration.WhyNotFrom(enough, true, park, Noon));
        foreach (var block in Enum.GetValues<MigrationBlock>().Where(b => b != MigrationBlock.None))
            Assert.NotEmpty(Migration.Words(block, TimeSpan.FromHours(5)));
    }

    [Fact]
    public void SixAreBroughtAndWaitInTheParkAndTheSourceIsWrittenDown()
    {
        var park = Park();
        var source = Read(SaveOf(8));
        var six = source.Boxes[0].Slots.Take(6).OfType<ForeignPokemon>().ToList();

        Assert.Equal(MigrationBlock.None, Migration.Bring(park, source, six, true, Noon, Ruleset.Modern));
        Assert.Equal(6, park.Waiting.Count);
        Assert.True(park.Stocked);
        Assert.Equal(new[] { "Bulbasaur", "Ivysaur", "Venusaur", "Charmander", "Charmeleon", "Charizard" }, park.Waiting.Select(p => p.Species.Name).ToArray());
        Assert.All(park.Waiting, p => Assert.Equal(20, p.Level));
        Assert.All(six, p => Assert.True(park.Ledger.HasCome(Migration.IdentityOf(p))));
        Assert.Equal(Brendan, Assert.Single(park.Ledger.Sources).TrainerId);
    }

    [Fact]
    public void WhatCameKeepsItsTrainerAndItsPersonalityAndItsFriendshipGoesBackToTheBase()
    {
        var park = Park();
        var source = Read(SaveOf(6));
        var six = source.Boxes[0].Slots.OfType<ForeignPokemon>().ToList();
        Migration.Bring(park, source, six, true, Noon, Ruleset.Modern);
        var first = park.Waiting[0];
        Assert.Equal(1000u, first.Personality);
        Assert.Equal("RED", first.OriginalTrainer!.Name);
        Assert.Equal(unchecked((int)0x0001_0002u), first.OriginalTrainer.Id);
        Assert.Equal(ForeignPokemon.BaseFriendship, first.Friendship);
    }

    [Fact]
    public void ThingsThatAreNotSixDifferentOnesAreRefusedAndNothingChanges()
    {
        var park = Park();
        var source = Read(SaveOf(9, change: m => m[(0, 8)] = BuildGba(new GbaSpec(Species: 9, Egg: true))));
        var all = source.Boxes[0].Slots.OfType<ForeignPokemon>().ToList();

        Assert.Equal(MigrationBlock.NotSix, Migration.Bring(park, source, all.Take(5).ToList(), true, Noon));
        Assert.Equal(MigrationBlock.NotSix, Migration.Bring(park, source, all.Take(5).Append(all[0]).ToList(), true, Noon));
        Assert.Equal(MigrationBlock.Refused, Migration.Bring(park, source, all.Skip(3).Take(6).ToList(), true, Noon));
        Assert.Empty(park.Waiting);
        Assert.Empty(park.Ledger.Sources);
        Assert.Empty(park.Ledger.Came);
    }

    [Fact]
    public void WhileSixStillWaitNoMoreComeAndTheSameSourceWaitsADayAfterTheyLeave()
    {
        var park = Park();
        var source = Read(SaveOf(14));
        var slots = source.Boxes[0].Slots.OfType<ForeignPokemon>().ToList();

        Assert.Equal(MigrationBlock.None, Migration.Bring(park, source, slots.Take(6).ToList(), true, Noon));
        // They are still in the park
        Assert.Equal(MigrationBlock.WaitForADay, Migration.Bring(park, source, slots.Skip(6).Take(6).ToList(), true, Noon.AddHours(2)));
        var other = Read(SaveOf(14, trainer: 0x3333_4444));
        Assert.Equal(MigrationBlock.AlreadyStocked, Migration.WhyNotFrom(other, true, park, Noon.AddHours(2)));

        // The show is played and the six go to the boxes; the same source still waits for its day
        park.TakeSix("Pal Park", Noon.Date);
        Assert.Empty(park.Waiting);
        Assert.Equal(MigrationBlock.WaitForADay, Migration.WhyNotFrom(source, true, park, Noon.AddHours(23).AddMinutes(59)));
        Assert.Equal(TimeSpan.FromMinutes(1), park.Ledger.WaitLeft(Brendan, Noon.AddHours(23).AddMinutes(59)));
        // Another source needn't
        Assert.Equal(MigrationBlock.None, Migration.WhyNotFrom(other, true, park, Noon.AddHours(1)));
        // A day later the first may, but not the six it gave
        Assert.Equal(MigrationBlock.None, Migration.WhyNotFrom(source, true, park, Noon.AddDays(1)));
        Assert.Equal(MigrationBlock.Refused, Migration.Bring(park, source, slots.Take(6).ToList(), true, Noon.AddDays(1)));
        Assert.Equal(MigrationBlock.None, Migration.Bring(park, source, slots.Skip(6).Take(6).ToList(), true, Noon.AddDays(1)));
        Assert.Equal(12, park.Ledger.Came.Count);
    }

    [Fact]
    public void TheDaysVisitorsNeverTakeThePlaceOfSixThatWereBroughtOver()
    {
        var pool = SpeciesSources.Sinnoh.PalParkPool;
        var chance = new Random(4);

        // Visitors yield to another day's visitors, as they did before the migration
        var visited = Park();
        Assert.True(visited.Welcome(pool, Noon, chance));
        Assert.True(visited.Welcome(pool, Noon.AddDays(1), chance));

        // Six that came over wait where they are, whatever day it is, until the show has been played
        var park = Park();
        var source = Read(SaveOf(8));
        Migration.Bring(park, source, source.Boxes[0].Slots.Take(6).OfType<ForeignPokemon>().ToList(), true, Noon, Ruleset.Modern);
        Assert.All(park.Waiting, p => Assert.True(park.IsMigrant(p)));
        Assert.False(park.Welcome(pool, Noon.AddDays(5), chance));
        Assert.Equal(new[] { "Bulbasaur", "Ivysaur", "Venusaur", "Charmander", "Charmeleon", "Charizard" }, park.Waiting.Select(p => p.Species.Name).ToArray());
        Assert.All(visited.Waiting, p => Assert.False(visited.IsMigrant(p)));

        // Once they have gone to the boxes the park is the visitors' again
        park.TakeSix("Pal Park", Noon.Date);
        Assert.True(park.Welcome(pool, Noon.AddDays(5), chance));
    }

    [Fact]
    public void TwentySourcesAreRememberedAndTheOldestIsForgotten()
    {
        var ledger = new PalParkLedger();
        for (uint id = 1; id <= 20; id++) ledger.Remember(id, Noon.AddMinutes(id));
        Assert.Equal(20, ledger.Sources.Count);
        // A source that gives again keeps its row
        ledger.Remember(5, Noon.AddHours(5));
        Assert.Equal(20, ledger.Sources.Count);
        // A twenty-first takes the place of the oldest, which was 1
        ledger.Remember(21, Noon.AddHours(6));
        Assert.Equal(20, ledger.Sources.Count);
        Assert.DoesNotContain(ledger.Sources, s => s.TrainerId == 1);
        Assert.Contains(ledger.Sources, s => s.TrainerId == 5);
        Assert.Contains(ledger.Sources, s => s.TrainerId == 21);
        Assert.Null(ledger.SinceLast(1, Noon.AddDays(2)));
        Assert.Equal(TimeSpan.Zero, ledger.WaitLeft(1, Noon));
    }

    // ------------------------------------------------------------------ a Platinum save and loose files as sources

    [Fact]
    public void APlatinumSaveAndAFolderOfPokemonFilesAreSourcesToo()
    {
        var mons = new Dictionary<(int, int), byte[]>();
        for (int i = 0; i < 6; i++) mons[(0, i)] = BuildDs(new DsSpec(Personality: 0x00AB0000u + (uint)i * 0x2000, Species: 10 + i, Exp: 2000, Moves: new[] { 0, 0, 0, 0 }));
        var normal = PlatinumBody("DAWN", 0x0042_0042, mons, out var boxes);
        var platinum = Read(PlatinumFile(normal, boxes, 5, 4));
        var park = Park();
        Assert.Equal(MigrationBlock.None, Migration.WhyNotFrom(platinum, true, park, Noon));
        Assert.Equal(MigrationBlock.None, Migration.Bring(park, platinum, platinum.All.ToList(), true, Noon, Ruleset.Modern));
        Assert.Equal(6, park.Waiting.Count);
        Assert.Equal(0x0042_0042u, park.Ledger.Sources[0].TrainerId);

        // Loose files count as one source, thirty to a box
        var files = Enumerable.Range(0, 7).Select(i => Read(BuildDs(new DsSpec(Personality: 0x00CD0000u + (uint)i * 0x2000, Species: 20 + i, Exp: 2000, Moves: new[] { 0, 0, 0, 0 })))).ToList();
        var combined = Migration.Combine(files);
        Assert.Equal(ForeignKind.Files, combined.Kind);
        Assert.Equal(7, combined.Count);
        Assert.Single(combined.Boxes);
        var park2 = Park();
        Assert.Equal(MigrationBlock.None, Migration.Bring(park2, combined, combined.All.Take(6).ToList(), true, Noon, Ruleset.Modern));
        Assert.Equal(Migration.LooseFilesId, park2.Ledger.Sources[0].TrainerId);
    }

    // ------------------------------------------------------------------ a save of the game

    [Fact]
    public void TheParkAndItsLedgerAreKeptInTheSaveAndComeBackFromIt()
    {
        var save = new SaveData { NationalPokedex = true };
        var source = Read(SaveOf(8));
        var six = source.Boxes[0].Slots.Take(6).OfType<ForeignPokemon>().ToList();
        Assert.Equal(MigrationBlock.None, Migration.Bring(save, source, six, Noon, Ruleset.Modern));

        // Through the save's own JSON
        var read = SaveManager.Read(SaveManager.Write(save))!;
        Assert.Equal(6, read.Pastimes!.Waiting!.Count);
        Assert.Equal(Brendan, Assert.Single(read.Pastimes.Sources!).TrainerId);
        Assert.Equal(6, read.Pastimes.Came!.Count);

        var again = new Pastimes();
        again.Load(read.Pastimes);
        Assert.True(again.PalPark.Stocked);
        Assert.Equal(MigrationBlock.WaitForADay, Migration.WhyNotFrom(source, true, again.PalPark, Noon.AddHours(3)));
        Assert.True(again.PalPark.Ledger.HasCome(Migration.IdentityOf(six[0])));

        // Without the National Pokédex a save takes nothing, and keeps nothing
        var young = new SaveData();
        Assert.Equal(MigrationBlock.NeedsNationalPokedex, Migration.Bring(young, source, six, Noon));
        Assert.Null(young.Pastimes);
    }

    [Fact]
    public void ASaveFromBeforeTheLedgerLoadsWithAnEmptyOne()
    {
        var pastimes = new Pastimes();
        pastimes.Load(new PastimesSave { Coins = 10 });
        Assert.Empty(pastimes.PalPark.Ledger.Sources);
        Assert.Empty(pastimes.PalPark.Ledger.Came);
        var saved = pastimes.ToSave();
        Assert.Null(saved.Sources);
        Assert.Null(saved.Came);
    }

    // ------------------------------------------------------------------ the folder

    private static string Folder() => Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "migrate-" + Guid.NewGuid().ToString("N"))).FullName;

    [Fact]
    public void TheMigrateFolderListsWhatTheReadersKnowAndLeavesEverythingAsItWas()
    {
        string folder = Folder();
        try
        {
            File.WriteAllBytes(Path.Combine(folder, "emerald.sav"), SaveOf(8));
            File.WriteAllBytes(Path.Combine(folder, "a.pk3"), BuildGba(new GbaSpec(), encrypt: false));
            File.WriteAllBytes(Path.Combine(folder, "b.ek4"), BuildDs(new DsSpec()));
            File.WriteAllBytes(Path.Combine(folder, "broken.sav"), new byte[0x20000]);
            File.WriteAllBytes(Path.Combine(folder, "notes.txt"), "hello"u8.ToArray());
            var before = Directory.GetFiles(folder).OrderBy(f => f).Select(f => (f, File.ReadAllBytes(f), File.GetLastWriteTimeUtc(f))).ToList();

            var sources = MigrationFolder.Read(folder);

            Assert.Equal(new[] { "emerald.sav", Text.Of("migrate.files.name"), "broken.sav" }, sources.Select(s => s.Name).ToArray());
            Assert.True(sources[0].Readable);
            Assert.Equal(2, sources[1].Files);
            Assert.False(sources[2].Readable);
            Assert.Equal(ForeignRefusal.DamagedSave, sources[2].Refusal);
            Assert.NotEmpty(sources[2].Notice);
            Assert.True(MigrationFolder.Offers(sources));

            var after = Directory.GetFiles(folder).OrderBy(f => f).Select(f => (f, File.ReadAllBytes(f), File.GetLastWriteTimeUtc(f))).ToList();
            Assert.Equal(before.Select(b => b.f), after.Select(a => a.f));
            for (int i = 0; i < before.Count; i++)
            {
                Assert.Equal(before[i].Item2, after[i].Item2);
                Assert.Equal(before[i].Item3, after[i].Item3);
            }
        }
        finally { Directory.Delete(folder, true); }
    }

    [Fact]
    public void AFolderThatIsNotThereHasNothingAndIsNotMade()
    {
        string folder = Path.Combine(Path.GetTempPath(), "migrate-missing-" + Guid.NewGuid().ToString("N"));
        Assert.Empty(MigrationFolder.Read(folder));
        Assert.False(Directory.Exists(folder));
        Assert.False(MigrationFolder.Offers(MigrationFolder.Read(folder)));

        // A folder with only junk offers nothing
        string junk = Folder();
        try
        {
            File.WriteAllBytes(Path.Combine(junk, "notes.txt"), new byte[10]);
            Assert.False(MigrationFolder.Offers(MigrationFolder.Read(junk)));
        }
        finally { Directory.Delete(junk, true); }
    }

    // ------------------------------------------------------------------ the screen

    private static MigrateScreen Opened(bool national = true, int saves = 1, byte[]? file = null, params string[] extra)
    {
        string folder = Folder();
        File.WriteAllBytes(Path.Combine(folder, "emerald.sav"), file ?? SaveOf(8, change: m => m[(0, 7)] = BuildGba(new GbaSpec(Species: 9, Egg: true))));
        var sources = MigrationFolder.Read(folder);
        Directory.Delete(folder, true);
        var targets = Enumerable.Range(1, saves).Select(n => new MigrateTarget(n, new SaveData { NationalPokedex = national, PlayerName = "Lucas" + n })).ToList();
        var screen = new MigrateScreen();
        screen.Open(targets, sources, () => Noon);
        return screen;
    }

    [Fact]
    public void TheScreenChoosesAFileThenSixAndAsksBeforeItHandsThemOver()
    {
        var screen = Opened();
        // One save to bring them to: the choice of it is skipped
        Assert.Equal(MigratePhase.Files, screen.Phase);
        Assert.Equal(0, screen.Cursor);

        screen.Confirm();
        Assert.Equal(MigratePhase.Box, screen.Phase);
        Assert.Equal(0, screen.ChosenCount);

        // The first six places are eligible; the eighth is an Egg and says no
        for (int i = 0; i < 6; i++)
        {
            Assert.Equal(i, screen.Place);
            screen.Confirm();
            Assert.Equal(i + 1, screen.ChosenNumber(i));
            screen.Move(1, 0);
        }
        Assert.Equal(6, screen.ChosenCount);
        // The row of six has wrapped round; the next row's first is a Pokémon that could come, but six are chosen
        Assert.Equal(0, screen.Place);
        screen.Move(0, 1);
        Assert.True(screen.VerdictOf(screen.Under!).Allowed);
        screen.Confirm();
        Assert.Equal(6, screen.ChosenCount);
        // And its neighbour is an Egg, which says no whatever
        screen.Move(1, 0);
        screen.Confirm();
        Assert.Equal(6, screen.ChosenCount);
        Assert.False(screen.VerdictOf(screen.Under!).Allowed);
        screen.Move(0, -1);
        for (int i = 0; i < 4; i++) screen.Move(1, 0);
        Assert.Equal(5, screen.Place);

        // Choosing a chosen one puts it back; the question can't be asked with five
        screen.Confirm();
        Assert.Equal(5, screen.ChosenCount);
        screen.Done();
        Assert.Equal(MigratePhase.Box, screen.Phase);
        screen.Confirm();
        Assert.Equal(6, screen.ChosenCount);

        screen.Done();
        Assert.Equal(MigratePhase.Confirm, screen.Phase);
        Assert.False(screen.ConfirmYes);
        // No is the first answer: A on it goes back
        screen.Confirm();
        Assert.Equal(MigratePhase.Box, screen.Phase);
        screen.Done();
        screen.Move(1, 0);
        Assert.True(screen.ConfirmYes);
        screen.Confirm();
        Assert.Equal(MigratePhase.Working, screen.Phase);

        var request = screen.TakeRequest()!;
        Assert.Equal(6, request.Six.Count);
        Assert.Equal("emerald.sav", request.Source.Name);
        Assert.Null(screen.TakeRequest());

        screen.Finish(MigrationBlock.None);
        Assert.Equal(MigratePhase.Result, screen.Phase);
        Assert.True(screen.Brought);
        screen.Confirm();
        Assert.False(screen.IsActive);
    }

    [Fact]
    public void ASourceThatCannotGiveNowCannotBeOpenedAndTheScreenSaysWhy()
    {
        var noNational = Opened(national: false);
        Assert.Equal(MigrationBlock.NeedsNationalPokedex, noNational.BlockOf(noNational.Sources[0]));
        noNational.Confirm();
        Assert.Equal(MigratePhase.Files, noNational.Phase);
        Assert.NotEmpty(noNational.BlockWords(noNational.Sources[0]));

        var few = Opened(file: SaveOf(5));
        Assert.Equal(MigrationBlock.TooFew, few.BlockOf(few.Sources[0]));
        few.Confirm();
        Assert.Equal(MigratePhase.Files, few.Phase);

        var unreadable = Opened(file: new byte[0x20000]);
        Assert.False(unreadable.Sources[0].Readable);
        unreadable.Confirm();
        Assert.Equal(MigratePhase.Files, unreadable.Phase);
    }

    [Fact]
    public void WithSeveralSavesTheScreenAsksWhichFirstAndBackStepsOutOneAtATime()
    {
        var screen = Opened(saves: 2);
        Assert.Equal(MigratePhase.Save, screen.Phase);
        screen.Move(0, 1);
        screen.Confirm();
        Assert.Equal(2, screen.Target!.Slot);
        Assert.Equal(MigratePhase.Files, screen.Phase);
        screen.Confirm();
        Assert.Equal(MigratePhase.Box, screen.Phase);
        screen.Confirm();
        Assert.Equal(1, screen.ChosenCount);

        screen.Cancel();
        Assert.Equal(MigratePhase.Files, screen.Phase);
        Assert.Equal(0, screen.ChosenCount);
        screen.Cancel();
        Assert.Equal(MigratePhase.Save, screen.Phase);
        Assert.Equal(1, screen.Cursor);
        screen.Cancel();
        Assert.False(screen.IsActive);
    }

    [Fact]
    public void ABoxIsGoneRoundByTheBoxButtonsAndAPlaceWrapsRoundTheGrid()
    {
        var screen = Opened();
        screen.Confirm();
        Assert.Equal(0, screen.BoxNumber);
        screen.ChangeBox(1);
        Assert.Equal(1, screen.BoxNumber);
        Assert.Equal(0, screen.Place);
        screen.ChangeBox(-2);
        Assert.Equal(screen.BoxCount - 1, screen.BoxNumber);
        screen.ChangeBox(1);
        Assert.Equal(0, screen.BoxNumber);

        screen.Move(-1, 0);
        Assert.Equal(MigrateScreen.Columns - 1, screen.Place);
        screen.Move(0, -1);
        Assert.Equal(MigrateScreen.PerBox - 1, screen.Place);
    }

    // ------------------------------------------------------------------ the title

    [Fact]
    public void TheTitleOffersMigrateOnlyWithAFileAndASaveToBringItTo()
    {
        var save = new SaveData { PlayerName = "Lucas" };
        var plain = TitleScreen.WithSlots(new[] { new TitleSlot(1, save) });
        Assert.DoesNotContain(TitleChoice.Migrate, plain.Entries);

        var offered = TitleScreen.WithSlots(new[] { new TitleSlot(1, save) });
        offered.OfferMigration(true);
        Assert.Equal(new[] { TitleChoice.Continue, TitleChoice.NewGame, TitleChoice.Delete, TitleChoice.Migrate, TitleChoice.Jukebox, TitleChoice.Options, TitleChoice.Quit }, offered.Entries.ToArray());
        offered.OfferMigration(false);
        Assert.DoesNotContain(TitleChoice.Migrate, offered.Entries);

        // Nothing to bring them to
        var empty = TitleScreen.WithSlots(Array.Empty<TitleSlot>());
        empty.OfferMigration(true);
        Assert.DoesNotContain(TitleChoice.Migrate, empty.Entries);

        // With all the saves and the row the column still fits above the disclaimer
        var three = TitleScreen.WithSlots(new[] { new TitleSlot(1, save), new TitleSlot(2, save), new TitleSlot(3, save) });
        three.OfferMigration(true);
        var rects = three.EntryRects();
        Assert.Equal(9, rects.Count);
        Assert.InRange(rects[0].Y, 100, 200);
        Assert.True(rects[^1].Y + rects[^1].Height < 1040);
    }
}
