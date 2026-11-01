using System.Text.Json;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 12 · Q28 and Q29: trades between the player's own saves and the Home box they share. Saves are made by headless
/// games in folders of the tests' own and changed through <see cref="HomeMoves"/> as the title's HOME changes them, so
/// the tests run alone.
/// </summary>
[Collection("Shared clocks and dice")]
public class HomeTests
{
    private const float Frame = 1f / 60f;

    // ------------------------------------------------------------------ trades (Q28)

    [Fact]
    public void ATradeMovesOnePokemonEachWayAndBothSavesLoad()
    {
        Play(paths =>
        {
            var (a, b) = TwoSaves(paths, "Bidoof", "Starly");
            var outcome = HomeMoves.Trade(paths, 1, a, 2, b);
            Assert.Equal(HomeRefusal.None, outcome);
            var one = Read(paths, 1);
            var two = Read(paths, 2);
            Assert.Contains(one.Party, p => p.Id == b && p.SpeciesName == "Starly");
            Assert.DoesNotContain(one.Party, p => p.Id == a);
            Assert.Contains(two.Party, p => p.Id == a && p.SpeciesName == "Bidoof");
            Assert.DoesNotContain(two.Party, p => p.Id == b);
            Assert.Null(one.Receipts);
            Assert.Null(two.Receipts);
            // Each carries the other save's player as its original trainer, and is listed to be taken up
            Assert.Equal(two.PlayerName, one.Party.Single(p => p.Id == b).OriginalTrainer!.Name);
            Assert.Contains(one.ArrivedByTrade!, t => t.Id == b && t.For == "Bidoof");
            // Both still load as a game
            foreach (int slot in new[] { 1, 2 })
            {
                var engine = Headless();
                engine.ContinueGame(slot);
                Until(engine, () => engine.Drive.State == GameState.Overworld, "the game");
            }
        });
    }

    [Fact]
    public void AnArrivingKadabraEvolvesAsItsSaveIsContinuedAndOneHoldingAnEverstoneDoesnt()
    {
        // (a Machoke for the Everstone: Platinum lets a Kadabra evolve through one, Evolution.Find keeps that quirk)
        foreach (bool everstone in new[] { false, true })
            Play(paths =>
            {
                var (a, kadabra) = TwoSaves(paths, "Bidoof", everstone ? "Machoke" : "Kadabra", 30, holdEverstone: everstone);
                Assert.Equal(HomeRefusal.None, HomeMoves.Trade(paths, 1, a, 2, kadabra));
                var engine = Headless();
                engine.ContinueGame(1);
                var game = engine.Drive;
                if (everstone)
                {
                    Until(engine, () => game.State == GameState.Overworld && engine.TradesWaiting == 0, "the field");
                    for (int f = 0; f < 120; f++) Run(engine, ActionFrame.None);
                    Assert.NotEqual(GameState.Evolution, game.State);
                    Assert.Equal("Machoke", game.Party.Members.Single(p => p.Id == kadabra).Species.Name);
                    return;
                }
                Until(engine, () => game.State == GameState.Evolution, "the trade's evolution");
                Until(engine, () => game.State == GameState.Overworld, "the field after it", press: true, most: 60 * 60);
                Assert.Equal("Alakazam", game.Party.Members.Single(p => p.Id == kadabra).Species.Name);
                Assert.True(game.Pokedex.IsCaught(PokemonDatabase.Get("Alakazam")!.DexNumber));
                // Saved, it is no longer waiting to be taken up
                Assert.True(game.Save());
                Assert.Null(Read(paths, 1).ArrivedByTrade);
            });
    }

    [Fact]
    public void APokemonTradedBackIsItsFirstPlayersOwnAgain()
    {
        Play(paths =>
        {
            var (a, b) = TwoSaves(paths, "Bidoof", "Starly");
            Assert.Equal(HomeRefusal.None, HomeMoves.Trade(paths, 1, a, 2, b));
            Assert.NotNull(Read(paths, 2).Party.Single(p => p.Id == a).OriginalTrainer);
            Assert.Equal(HomeRefusal.None, HomeMoves.Trade(paths, 1, b, 2, a));
            Assert.Null(Read(paths, 1).Party.Single(p => p.Id == a).OriginalTrainer);
            Assert.Null(Read(paths, 2).Party.Single(p => p.Id == b).OriginalTrainer);
        });
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void ATradeStoppedAfterAnyWriteLeavesEachPokemonInExactlyOneSave(int stopAt)
    {
        Play(paths =>
        {
            var (a, b) = TwoSaves(paths, "Bidoof", "Starly");
            Assert.Equal(HomeRefusal.CouldNotSave, HomeMoves.Trade(paths, 1, a, 2, b, write => write == stopAt));
            HomeMoves.Resolve(paths);
            var one = Read(paths, 1);
            var two = Read(paths, 2);
            int CountOf(Guid id) => new[] { one, two }.Sum(s => SlotMoves.Held(s).Count(h => h == id));
            Assert.Equal(1, CountOf(a));
            Assert.Equal(1, CountOf(b));
            Assert.Null(one.Receipts);
            Assert.Null(two.Receipts);
            // A crash after the first write is carried forward: the trade is made
            Assert.Contains(two.Party, p => p.Id == a);
        });
    }

    [Fact]
    public void ATradeWhoseSecondSaveIsGoneIsUndone()
    {
        Play(paths =>
        {
            var (a, b) = TwoSaves(paths, "Bidoof", "Starly");
            HomeMoves.Trade(paths, 1, a, 2, b, write => write == 1);
            Assert.True(SaveSlots.Delete(paths, 2));
            HomeMoves.Resolve(paths);
            var one = Read(paths, 1);
            Assert.Contains(one.Party, p => p.Id == a);
            Assert.DoesNotContain(one.Party, p => p.Id == b);
            Assert.Null(one.Receipts);
            Assert.Null(one.ArrivedByTrade);
        });
    }

    [Fact]
    public void ARunUnderWayNeitherTradesNorBanksUntilItEnds()
    {
        Play(paths =>
        {
            var (a, b) = TwoSaves(paths, "Bidoof", "Starly", nuzlockeInFirst: true, extraInFirst: "Kricketot");
            Assert.Equal(HomeRefusal.RunUnderWay, HomeMoves.Trade(paths, 1, a, 2, b));
            Assert.Equal(HomeRefusal.RunUnderWay, HomeMoves.Deposit(paths, 1, a));
            Edit(paths, 1, s => s.RunState = new RunState { Ended = true });
            Assert.Equal(HomeRefusal.None, HomeMoves.Deposit(paths, 1, a));
        });
    }

    // ------------------------------------------------------------------ the Home box (Q29)

    [Fact]
    public void ADepositAndAWithdrawalGiveBackAPokemonWholeEveryField()
    {
        Play(paths =>
        {
            var (a, _) = TwoSaves(paths, "Bidoof", "Starly", extraInFirst: "Kricketot");
            var before = Json(Read(paths, 1).Party.Single(p => p.Id == a));
            Assert.Equal(HomeRefusal.None, HomeMoves.Deposit(paths, 1, a));
            Assert.DoesNotContain(Read(paths, 1).Party, p => p.Id == a);
            var (why, bank) = HomeMoves.ReadBank(paths);
            Assert.Equal(HomeRefusal.None, why);
            Assert.True(bank!.Holds(a));
            Assert.Null(bank.Receipts);
            Assert.Equal(before, Json(bank.Pokemon.Single().Pokemon));
            // Taken out into the other save, whole
            Assert.Equal(HomeRefusal.None, HomeMoves.Withdraw(paths, 2, a));
            Assert.Equal(before, Json(Read(paths, 2).Party.Single(p => p.Id == a)));
            Assert.Empty(HomeMoves.ReadBank(paths).Bank!.Pokemon);
        });
    }

    [Fact]
    public void ACopyIsRefusedByItsId()
    {
        Play(paths =>
        {
            var (a, _) = TwoSaves(paths, "Bidoof", "Starly", extraInFirst: "Kricketot");
            Assert.Equal(HomeRefusal.None, HomeMoves.Deposit(paths, 1, a));
            // A Pokémon of the same id put back into the save by hand (a copy) can't go in again
            var banked = HomeMoves.ReadBank(paths).Bank!.Pokemon.Single().Pokemon;
            Edit(paths, 1, s => s.Party.Add(banked));
            Assert.Equal(HomeRefusal.AlreadyThere, HomeMoves.Deposit(paths, 1, a));
            Assert.Equal(HomeRefusal.AlreadyThere, HomeMoves.Withdraw(paths, 1, a));
        });
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void AMoveToOrFromTheHomeBoxStoppedAfterAnyWriteLeavesItInExactlyOnePlace(int stopAt)
    {
        Play(paths =>
        {
            var (a, _) = TwoSaves(paths, "Bidoof", "Starly", extraInFirst: "Kricketot");
            Assert.Equal(HomeRefusal.CouldNotSave, HomeMoves.Deposit(paths, 1, a, write => write == stopAt));
            HomeMoves.Resolve(paths);
            Assert.Equal(1, Places(paths, a));
            Assert.Equal(HomeRefusal.CouldNotSave, HomeMoves.Withdraw(paths, 2, a, write => write == stopAt));
            HomeMoves.Resolve(paths);
            Assert.Equal(1, Places(paths, a));
            Assert.All(new[] { Read(paths, 1), Read(paths, 2) }, s => Assert.Null(s.Receipts));
            Assert.Null(HomeMoves.ReadBank(paths).Bank!.Receipts);
        });
    }

    [Fact]
    public void AHomeBoxOfAnotherDataVersionIsRefusedWithANotice()
    {
        Play(paths =>
        {
            var (a, _) = TwoSaves(paths, "Bidoof", "Starly", extraInFirst: "Kricketot");
            var other = HomeBank.Empty();
            other.DataVersion = "another";
            Assert.True(HomeBank.Save(other, paths.HomeBox));
            Assert.Equal(HomeRefusal.OtherData, HomeMoves.ReadBank(paths).Why);
            Assert.Equal(HomeRefusal.OtherData, HomeMoves.Deposit(paths, 1, a));
            Assert.Contains(Read(paths, 1).Party, p => p.Id == a);

            // The screen says so as the Home box is opened
            var screen = new HomeScreen();
            var slot = new HomeSlot(1, Read(paths, 1));
            screen.Open(new[] { slot }, null, HomeRefusal.OtherData);
            screen.Confirm();
            Assert.Equal(HomePhase.Result, screen.Phase);
            Assert.Equal(HomeRefusal.OtherData, screen.Outcome);
            Assert.False(string.IsNullOrEmpty(HomeMoves.Words(HomeRefusal.OtherData)));
        });
    }

    [Fact]
    public void TheHomeBoxIsWrittenBesideTheSaves()
    {
        var paths = SavePaths.In(Path.Combine(Path.GetTempPath(), "home-where"));
        Assert.Equal(Path.Combine(paths.Folder, "home.json"), paths.HomeBox);
        Assert.Contains(SavePaths.HomeBoxName, SavePaths.OwnFiles());
    }

    // ------------------------------------------------------------------ the screen and the title

    [Fact]
    public void TheScreenPicksOneOnEachSideAndAsksBeforeATrade()
    {
        Play(paths =>
        {
            TwoSaves(paths, "Bidoof", "Starly");
            var screen = new HomeScreen();
            screen.Open(new[] { new HomeSlot(1, Read(paths, 1)), new HomeSlot(2, Read(paths, 2)) }, HomeBank.Empty(), HomeRefusal.None);
            Assert.True(screen.CanTrade);
            screen.Confirm();                       // TRADE, straight to the two sides with two saves
            Assert.Equal(HomePhase.Trade, screen.Phase);
            screen.Confirm();                       // the left's
            Assert.Equal(1, screen.Side);
            screen.Confirm();                       // the right's
            Assert.Equal(HomePhase.ConfirmTrade, screen.Phase);
            screen.Move(1, 0);                      // YES
            screen.Confirm();
            var request = screen.TakeRequest();
            Assert.NotNull(request);
            Assert.Equal(HomeRequestKind.Trade, request!.Kind);
            Assert.Equal((1, 2), (request.Slot, request.OtherSlot));
        });
    }

    [Fact]
    public void TheTitleOffersHomeWithASaveAndItsColumnFitsWithThreeSavesAndMigrate()
    {
        var save = new SaveData { PlayerName = "Ann" };
        var title = TitleScreen.WithSlots(new[] { new TitleSlot(1, save), new TitleSlot(2, save), new TitleSlot(3, save) });
        Assert.Contains(TitleChoice.Home, title.Entries);
        title.OfferMigration(true);
        Assert.Contains(TitleChoice.Migrate, title.Entries);
        var rects = title.EntryRects();
        Assert.True(rects[^1].Y + rects[^1].Height <= 1012, $"The column ends at {rects[^1].Y + rects[^1].Height}.");
        Assert.DoesNotContain(TitleChoice.Home, TitleScreen.WithSlots(Array.Empty<TitleSlot>()).Entries);
    }

    // ------------------------------------------------------------------ helpers

    private static string Json(SavedPokemonData p) => JsonSerializer.Serialize(p);

    private static SaveData Read(SavePaths paths, int slot) => SaveSlots.Read(paths, slot).Save!;

    private static void Edit(SavePaths paths, int slot, Action<SaveData> change)
    {
        var save = Read(paths, slot);
        change(save);
        Assert.True(SaveSlots.Write(paths, slot, save));
    }

    private static int Places(SavePaths paths, Guid id) =>
        SlotMoves.Held(Read(paths, 1)).Count(h => h == id) + SlotMoves.Held(Read(paths, 2)).Count(h => h == id)
        + (HomeMoves.ReadBank(paths).Bank!.Holds(id) ? 1 : 0);

    // Two games saved in slots 1 and 2, each with one Pokémon of the species given (and a second in the first if asked);
    // the ids of the two leads
    private static (Guid First, Guid Second) TwoSaves(SavePaths paths, string first, string second, int level = 10,
        bool holdEverstone = false, bool nuzlockeInFirst = false, string? extraInFirst = null)
    {
        Guid Make(int seed, string species, string name, string? extra, RunRules? run)
        {
            Dice.Seed(seed);
            var engine = Headless();
            var game = engine.Drive;
            engine.NewGameRegion = "Sinnoh";
            engine.StartNewGame(name, PlayerLook.Boy, null, run);
            StoryMigration.PastTheOpening(game.Story, ScriptLibrary.Default);
            game.Party.Clear();
            var lead = new Pokemon(PokemonDatabase.Get(species)!, level);
            if (holdEverstone && species == second) lead.HeldItem = ItemDatabase.Get("Everstone");
            game.Party.Add(lead);
            if (extra != null) game.Party.Add(new Pokemon(PokemonDatabase.Get(extra)!, level));
            game.State = GameState.Overworld;
            Assert.True(game.Save());
            return lead.Id;
        }
        var nuzlocke = nuzlockeInFirst ? RunRules.Normal with { Nuzlocke = true } : null;
        var a = Make(11, first, "Ann", extraInFirst, nuzlocke);
        var b = Make(22, second, "Ben", null, null);
        Assert.Equal(1, SaveSlots.Read(paths).Count(s => s.Save?.PlayerName == "Ann"));
        return (a, b);
    }

    private static void Play(Action<SavePaths> body)
    {
        string folder = Path.Combine(Path.GetTempPath(), "home-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var paths = SavePaths.Current;
        try
        {
            SavePaths.Current = SavePaths.In(folder);
            body(SavePaths.Current);
        }
        finally
        {
            InputManager.Feed(null);
            Dice.Seed(null);
            Ruleset.Use(RulesPreset.Platinum);
            SavePaths.Current = paths;
            try { Directory.Delete(folder, recursive: true); } catch (IOException) { }
        }
    }

    private static GameEngine Headless()
    {
        var engine = new GameEngine(new GameSettings { Muted = true });
        engine.InitializeHeadless();
        engine.PeopleStayPut = true;
        return engine;
    }

    private static void Run(GameEngine engine, ActionFrame frame)
    {
        InputManager.Feed(frame);
        engine.Update(Frame);
    }

    private static void Until(GameEngine engine, Func<bool> done, string what, bool press = false, int most = 60 * 30)
    {
        for (int f = 0; f < most; f++)
        {
            if (done()) return;
            Run(engine, press && f % 8 == 0 ? ActionFrame.Press(GameAction.Confirm) : ActionFrame.None);
        }
        Assert.Fail($"Waited {most / 60} seconds of game time for {what}; the game is in {engine.Drive.State}.");
    }
}
