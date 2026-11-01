using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 12 · Q25: a lost battle fought again with the options' RETRY BATTLES on, and a battle suspended by the close
/// button's SUSPEND AND QUIT and taken up from its slot. Whole games with no window, on seeded dice, in folders of the
/// tests' own, so they run alone.
/// </summary>
[Collection("Shared clocks and dice")]
public class RetryAndSuspendTests
{
    private const float Frame = 1f / 60f;

    // ------------------------------------------------------------------ the rules

    [Fact]
    public void TheQuestionIsAskedOnlyAfterALossTheRowAndTheBattleAllow()
    {
        bool Offered(bool row = true, BattleResult result = BattleResult.PlayerDefeat, BattleKind kind = BattleKind.Normal, bool snapshot = true,
            bool mayBeLost = false, bool frontier = false, bool watching = false, bool bugContest = false, bool nuzlocke = false, RunTerms? terms = null) =>
            BattleRetry.Offered(row, result, kind, snapshot, mayBeLost, frontier, watching, bugContest, nuzlocke, terms);

        Assert.True(Offered());
        Assert.True(Offered(kind: BattleKind.Roamer));
        // A difficulty alone, and the later mechanics barred, are no challenge rule
        Assert.True(Offered(terms: new RunTerms(NoLaterMechanics: true)));
        Assert.False(Offered(row: false));
        Assert.False(Offered(result: BattleResult.PlayerVictory));
        Assert.False(Offered(result: BattleResult.PlayerRan));
        Assert.False(Offered(snapshot: false));
        Assert.False(Offered(mayBeLost: true));
        Assert.False(Offered(kind: BattleKind.CatchingLesson));
        Assert.False(Offered(kind: BattleKind.Safari));
        Assert.False(Offered(kind: BattleKind.PalPark));
        Assert.False(Offered(frontier: true));
        Assert.False(Offered(watching: true));
        Assert.False(Offered(bugContest: true));
        Assert.False(Offered(nuzlocke: true));
        Assert.False(Offered(terms: new RunTerms(NoItems: true)));
        Assert.False(Offered(terms: new RunTerms(Monotype: PokemonType.Fire)));
    }

    [Fact]
    public void SuspendAndQuitStandsWhereSaveAndQuitDoesOnlyInABattleThatCanBeLeft()
    {
        var prompt = new ClosePrompt();
        prompt.Open(canSave: false, canSuspend: true);
        Assert.Equal(new[] { CloseChoice.KeepPlaying, CloseChoice.SuspendAndQuit, CloseChoice.Quit }, prompt.Answers);
        Assert.Equal(Text.Of("close.suspendBody"), prompt.Body);
        prompt.Move(1);
        Assert.Equal(CloseChoice.SuspendAndQuit, prompt.Confirm());
        // Where the game can be saved it is SAVE AND QUIT, never both
        prompt.Open(canSave: true, canSuspend: true);
        Assert.DoesNotContain(CloseChoice.SuspendAndQuit, prompt.Answers);
        prompt.Open(canSave: false);
        Assert.Equal(new[] { CloseChoice.KeepPlaying, CloseChoice.Quit }, prompt.Answers);

        Assert.True(BattleRetry.Suspendable(true, false, BattleResult.None, BattleKind.Normal, false, false, false, false, true, true));
        Assert.False(BattleRetry.Suspendable(true, scriptRunning: true, BattleResult.None, BattleKind.Normal, false, false, false, false, true, true));
        Assert.False(BattleRetry.Suspendable(true, false, BattleResult.PlayerDefeat, BattleKind.Normal, false, false, false, false, true, true));
        Assert.False(BattleRetry.Suspendable(true, false, BattleResult.None, BattleKind.Safari, false, false, false, false, true, true));
        Assert.False(BattleRetry.Suspendable(true, false, BattleResult.None, BattleKind.Normal, false, false, false, false, recorded: false, true));
        Assert.False(BattleRetry.Suspendable(true, false, BattleResult.None, BattleKind.Normal, false, false, false, false, true, slotSaved: false));
    }

    [Fact]
    public void ASuspensionOfAnotherDataVersionIsRefused()
    {
        var file = new BattleRecordFile { DataVersion = "another", Wild = { new RecordedPokemon() } };
        var suspension = new BattleSuspension { Battle = file.Serialize(), Foe = "Starly" };
        var (read, battle, refused) = BattleSuspension.Read(suspension.Serialize());
        Assert.Null(read);
        Assert.Null(battle);
        Assert.Equal(ReplayRefusal.OtherDataVersion, refused);
        Assert.Equal(ReplayRefusal.Damaged, BattleSuspension.Read("{ not json").Refused);
        // A file of this game's data reads whole
        file.DataVersion = GameDataFiles.Version;
        suspension.Battle = file.Serialize();
        var whole = BattleSuspension.Read(suspension.Serialize());
        Assert.NotNull(whole.Suspension);
        Assert.Equal("Starly", whole.Suspension!.Foe);
    }

    // ------------------------------------------------------------------ in the game

    [Fact]
    public void ALostWildBattleTriedAgainGivesBackTheMoneyTheItemsAndTheHpAgainstTheSamePokemon()
    {
        Play(folder =>
        {
            var (engine, game) = Begun(retry: true);
            var lead = game.Party.Members[0];
            int money = game.Money;
            var potion = ItemDatabase.Get("Potion")!;
            var before = game.BuildSave();

            game.StartWildBattle(new WildEncounterEntry { SpeciesName = "Rattata", MinLevel = 60, MaxLevel = 60, Weight = 100 });
            Until(engine, () => game.State == GameState.Battle, "the battle");
            var first = game.Battle!;
            var foe = first.EnemyPokemon;
            Lose(engine, first, usePotion: true);
            Assert.True(game.RetryPrompt.IsOpen, "A battle lost with the row on asks first.");
            Assert.Equal(2, game.Bag.GetQuantity(potion));
            // Nothing of the loss has happened yet: the money is where it was, nobody is home
            Assert.Equal(money, game.Money);
            Assert.Equal(GameState.Battle, game.State);

            game.AnswerRetry(RetryChoice.TryAgain);
            Until(engine, () => game.State == GameState.Battle && game.Battle != first, "the battle again");
            var again = game.Battle!;
            Assert.Equal(lead.MaxHP, lead.CurrentHP);
            Assert.Equal(3, game.Bag.GetQuantity(potion));
            Assert.Equal(money, game.Money);
            Assert.Equal(foe.Species.Name, again.EnemyPokemon.Species.Name);
            Assert.Equal(foe.Level, again.EnemyPokemon.Level);
            Assert.Equal(again.EnemyPokemon.MaxHP, again.EnemyPokemon.CurrentHP);
            Assert.Equal(1, game.Statistics.Get(Statistics.Retries));
            // The team, the bag and the money as the battle began (the save made before it, but its counters and play time)
            var after = game.BuildSave();
            Assert.Equal(Json(before.Party), Json(after.Party));
            Assert.Equal(Json(before.Inventory), Json(after.Inventory));
            Assert.Equal(before.Money, after.Money);

            // GIVE UP goes on as a loss always has: home, with some of the money gone
            Lose(engine, again, usePotion: false);
            Assert.True(game.RetryPrompt.IsOpen);
            game.AnswerRetry(RetryChoice.GiveUp);
            Until(engine, () => game.State == GameState.Overworld, "the field after giving up", press: true);
            Assert.True(game.Money < money, "A battle given up costs what a loss costs.");
            Assert.False(game.RetryPrompt.IsOpen);
        });
    }

    [Fact]
    public void ATrainerFoughtAgainHasTheWholeTeamBack()
    {
        Play(folder =>
        {
            var (engine, game) = Begun(retry: true);
            var trainer = new Trainer { Id = "test_ace", Name = "Vera", TrainerClass = "Ace Trainer", PrizeMoney = 100 };
            trainer.Party.Add(new Pokemon(PokemonDatabase.Get("Staravia")!, 60));
            trainer.Party.Add(new Pokemon(PokemonDatabase.Get("Luxio")!, 60));
            var person = new NPC { Key = "vera", Name = "Vera", IsTrainer = true, TrainerData = trainer, GridX = game.Player.GridX + 1, GridY = game.Player.GridY };
            game.StartTrainerBattle(person);
            Until(engine, () => game.State == GameState.Battle, "the battle");
            var first = game.Battle!;
            Lose(engine, first, usePotion: false);
            Assert.True(game.RetryPrompt.IsOpen);
            game.AnswerRetry(RetryChoice.TryAgain);
            Until(engine, () => game.State == GameState.Battle && game.Battle != first, "the battle again");
            var team = game.Battle!.Trainers[0].Party.Members;
            Assert.Equal(new[] { "Staravia", "Luxio" }, team.Select(p => p.Species.Name));
            Assert.All(team, p => Assert.Equal(p.MaxHP, p.CurrentHP));
        });
    }

    [Fact]
    public void WithTheRowOffALossWhitesOutAsItAlwaysHas()
    {
        Play(folder =>
        {
            var (engine, game) = Begun(retry: false);
            int money = game.Money;
            game.StartWildBattle(new WildEncounterEntry { SpeciesName = "Rattata", MinLevel = 60, MaxLevel = 60, Weight = 100 });
            Until(engine, () => game.State == GameState.Battle, "the battle");
            Lose(engine, game.Battle!, usePotion: false);
            Assert.False(game.RetryPrompt.IsOpen);
            Until(engine, () => game.State == GameState.Overworld, "the field after the loss", press: true);
            Assert.True(game.Money < money);
            Assert.Equal(0, game.Statistics.Get(Statistics.Retries));
        });
    }

    [Fact]
    public void ASuspendedBattleTakenUpReachesTheSameLogAsOnePlayedWithoutABreakAndIsGoneOnceTakenUp()
    {
        Play(folder =>
        {
            // The battle fought without a break, suspended on the way at its third menu
            var (engine, game) = Begun(retry: false, level: 30);
            Assert.True(game.Save());
            int slot = game.SaveSlot;
            game.StartWildBattle(new WildEncounterEntry { SpeciesName = "Bidoof", MinLevel = 24, MaxLevel = 24, Weight = 100 });
            Until(engine, () => game.State == GameState.Battle, "the battle");
            var battle = game.Battle!;
            for (int menus = 0; menus < 3 && !battle.IsBattleOver; menus++) Turn(engine, battle);
            Assert.True(battle.HUD.MenuState == BattleMenuState.Main, "The battle stands at a menu.");
            Assert.True(game.CanSuspend);
            game.RequestClose();
            Assert.Contains(CloseChoice.SuspendAndQuit, game.ClosePrompt.Answers);
            game.AnswerClose(CloseChoice.SuspendAndQuit);
            Assert.True(BattleSuspension.Exists(SavePaths.Current, slot));
            Assert.True(engine.QuitRequested);
            var leftAt = battle.Core.Log.OfType<Said>().Select(s => s.Text).ToList();
            game.State = GameState.Battle;
            while (!battle.IsBattleOver) Turn(engine, battle);
            var whole = battle.Core.Log.OfType<Said>().Select(s => s.Text).ToList();

            // Taken up by a game started afresh: the same menu, then the same battle to its end
            var later = Headless();
            later.ContinueGame(slot);
            Assert.False(BattleSuspension.Exists(SavePaths.Current, slot), "The suspension is gone once taken up.");
            var taken = later.Drive.Battle!;
            Assert.NotNull(taken);
            Until(later, () => later.Drive.State == GameState.Battle, "the battle taken up");
            Assert.Equal(leftAt, taken.Core.Log.OfType<Said>().Select(s => s.Text).ToList());
            Assert.Equal(BattleMenuState.Main, taken.HUD.MenuState);
            while (!taken.IsBattleOver) Turn(later, taken);
            Assert.Equal(whole, taken.Core.Log.OfType<Said>().Select(s => s.Text).ToList());

            // Taken up once: the slot's own game the next time
            var third = Headless();
            third.ContinueGame(slot);
            Until(third, () => third.Drive.State == GameState.Overworld, "the slot's own game");
        });
    }

    // ------------------------------------------------------------------ helpers

    private static string Json<T>(T value) => System.Text.Json.JsonSerializer.Serialize(value);

    // A game in a folder of the test's own, its dice seeded, the rules put back after
    private static void Play(Action<string> body)
    {
        string folder = Path.Combine(Path.GetTempPath(), "retry-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var paths = SavePaths.Current;
        try
        {
            SavePaths.Current = SavePaths.In(folder);
            Dice.Seed(25);
            body(folder);
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

    // A new game past its opening with one Pokémon, three Potions, standing in Twinleaf Town
    private static (GameEngine Engine, GameEngine.Driver Game) Begun(bool retry, int level = 2)
    {
        var engine = Headless();
        engine.Settings.RetryBattles = retry;
        var game = engine.Drive;
        engine.NewGameRegion = "Sinnoh";
        engine.StartNewGame();
        StoryMigration.PastTheOpening(game.Story, ScriptLibrary.Default);
        game.Party.Clear();
        game.Party.Add(new Pokemon(PokemonDatabase.Get(level < 10 ? "Magikarp" : "Turtwig")!, level));
        game.Bag.AddItem(ItemDatabase.Get("Potion")!, 3);
        game.State = GameState.Overworld;
        game.TrainersLookOnArrival = false;
        game.Place(MapDatabase.Get("Sinnoh"), 116, 886, Direction.Down);
        return (engine, game);
    }

    private static GameEngine Headless()
    {
        var engine = new GameEngine(new GameSettings { Muted = true });
        engine.InitializeHeadless();
        engine.PeopleStayPut = true;
        return engine;
    }

    // Read on to the menu, then a turn of the lead's first move, read on to the next menu or the end
    private static void Turn(GameEngine engine, BattleEngine battle)
    {
        Until(engine, () => battle.IsBattleOver || battle.HUD.MenuState == BattleMenuState.Main, "the battle's menu", press: true);
        if (battle.IsBattleOver) return;
        battle.SelectMove(0);
        Until(engine, () => battle.IsBattleOver || battle.HUD.MenuState == BattleMenuState.Main && !battle.IsWaitingForConfirm, "the next menu", press: true, startWith: 8);
    }

    // Splash until the lead falls (a Potion first, if asked), to the battle's last line
    private static void Lose(GameEngine engine, BattleEngine battle, bool usePotion)
    {
        bool used = false;
        for (int guard = 0; guard < 60 * 300 && !battle.IsBattleOver; guard++)
        {
            if (battle.HUD.MenuState == BattleMenuState.Main && !battle.IsWaitingForConfirm)
            {
                if (usePotion && !used)
                {
                    battle.PlayerPokemon!.CurrentHP = Math.Max(1, battle.PlayerPokemon.CurrentHP - 1);
                    battle.UseBagItem("Potion", 0);
                    used = true;
                }
                else battle.SelectMove(0);
            }
            Run(engine, guard % 8 == 0 ? ActionFrame.Press(GameAction.Confirm) : ActionFrame.None);
        }
        Assert.Equal(BattleResult.PlayerDefeat, battle.Result);
        Until(engine, () => engine.Drive.RetryPrompt.IsOpen || engine.Drive.State != GameState.Battle, "the loss's end", press: true);
    }

    private static void Run(GameEngine engine, ActionFrame frame)
    {
        InputManager.Feed(frame);
        engine.Update(Frame);
    }

    private static void Until(GameEngine engine, Func<bool> done, string what, bool press = false, int most = 60 * 120, int startWith = 0)
    {
        for (int f = startWith; f < most + startWith; f++)
        {
            if (done()) return;
            Run(engine, press && f % 8 == 0 ? ActionFrame.Press(GameAction.Confirm) : ActionFrame.None);
        }
        Assert.Fail($"Waited {most / 60} seconds of game time for {what}; the game is in {engine.Drive.State}.");
    }
}
