using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 12 · Q3: the autosave and the options' reset. The game runs with no window (plan 16 · T12) in a folder of the
/// test's own: a battle won writes the autosave and a script never does, a save by hand takes it away, a start after a
/// crash offers it, and the reset gives <c>new GameSettings()</c>. It shares the dice, the save folder and the databases
/// with the whole process, so it runs alone.
/// </summary>
[Collection("Shared clocks and dice")]
public class AutosaveTests
{
    private const float Frame = 1f / 60f;

    private static GameEngine Headless(bool autosave = true)
    {
        var engine = new GameEngine(new GameSettings { Muted = true, Autosave = autosave });
        engine.InitializeHeadless();
        engine.PeopleStayPut = true;
        return engine;
    }

    private static void Run(GameEngine engine, ActionFrame frame)
    {
        InputManager.Feed(frame);
        engine.Update(Frame);
    }

    private static void Until(GameEngine engine, Func<bool> done, string what, bool press = false, int most = 60 * 120)
    {
        for (int f = 0; f < most; f++)
        {
            if (done()) return;
            Run(engine, press && f % 8 == 0 ? ActionFrame.Press(GameAction.Confirm) : ActionFrame.None);
        }
        Assert.Fail($"Waited {most / 60} seconds of game time for {what}; the game is in {engine.Drive.State}.");
    }

    /// <summary>Runs a test with the saves in a folder of its own, and the game's chance and rules put back after.</summary>
    private static void InAFolder(Action<string> test)
    {
        string folder = Path.Combine(Path.GetTempPath(), "autosave-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var paths = SavePaths.Current;
        try
        {
            SavePaths.Current = SavePaths.In(folder);
            Dice.Seed(12);
            test(folder);
        }
        finally
        {
            InputManager.Feed(null);
            Dice.Seed(null);
            Ruleset.Use(RulesPreset.Platinum);
            PlayerIdentity.InRegion(RegionDatabase.First);
            SavePaths.Current = paths;
            try { Directory.Delete(folder, recursive: true); } catch (IOException) { }
        }
    }

    // A new game past the opening with a strong lead, standing on Twinleaf Town's street
    private static GameEngine NewGame(bool autosave = true)
    {
        var engine = Headless(autosave);
        engine.NewGameRegion = "Sinnoh";
        engine.StartNewGame();
        var game = engine.Drive;
        StoryMigration.PastTheOpening(game.Story, ScriptLibrary.Default);
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Turtwig")!, 50));
        game.Bag.AddItem(ItemDatabase.Get("Poké Ball")!, 10);
        game.State = GameState.Overworld;
        game.Place(MapDatabase.Get("Sinnoh"), 116, 886, Direction.Right);
        game.TrainersLookOnArrival = false;
        // (The place is come to: no first-arrival write is owed in these tests)
        for (int f = 0; f < 120; f++) Run(engine, ActionFrame.None);
        return engine;
    }

    [Fact]
    public void AutosaveWritesAfterABattleAndNeverInIt()
    {
        InAFolder(folder =>
        {
            var engine = NewGame();
            var game = engine.Drive;
            var lead = game.Party.Members[0];
            int before = engine.AutosavesWritten;
            AutosaveFile.Clear(SavePaths.Current);

            game.StartWildBattle(new WildEncounterEntry { SpeciesName = "Starly", MinLevel = 2, MaxLevel = 2, Weight = 100 });
            Until(engine, () => game.State == GameState.Battle, "the battle");
            var battle = game.Battle!;
            int hit = lead.Moves.FindIndex(m => m.Data.Category != MoveCategory.Status);
            Until(engine, () => battle.IsBattleOver || battle.HUD.MenuState == BattleMenuState.Main, "the battle's menu", press: true);
            for (int guard = 0; guard < 60 * 120 && !battle.IsBattleOver; guard++)
            {
                if (battle.HUD.MenuState == BattleMenuState.Main) battle.SelectMove(hit);
                Run(engine, guard % 8 == 0 ? ActionFrame.Press(GameAction.Confirm) : ActionFrame.None);
                // Never while the battle is on
                Assert.Equal(before, engine.AutosavesWritten);
            }
            Assert.True(battle.IsBattleOver);
            Assert.False(File.Exists(SavePaths.Current.Autosave));

            Until(engine, () => game.State == GameState.Overworld && engine.AutosavesWritten > before, "the autosave after the battle", press: true);
            Assert.Equal(before + 1, engine.AutosavesWritten);
            Assert.True(File.Exists(SavePaths.Current.Autosave));
            // Beside the slots and not over one: no slot's save was made
            Assert.False(File.Exists(SavePaths.Current.Slot(game.SaveSlot)));
            var (outcome, saved) = AutosaveFile.Read(SavePaths.Current);
            Assert.Equal(SaveLoad.Ok, outcome);
            Assert.Equal(game.SaveSlot, saved!.AutosaveSlot);
            Assert.Equal("Turtwig", saved.Party[0].SpeciesName);
        });
    }

    [Fact]
    public void AutosaveNeverWritesWhileAScriptRunsAndWritesWhenItEnds()
    {
        InAFolder(folder =>
        {
            var engine = NewGame();
            var game = engine.Drive;
            // Sandgem Town's Pokémon Center: the nurse's script heals (a moment of its own) and talks for a while
            var sinnoh = MapDatabase.Get("Sinnoh");
            game.PlayTime += 120f;
            int arrivedAt = engine.AutosavesWritten;
            var door = sinnoh.Warps.First(w => w.TargetMap == "PokemonCenter");
            game.Place(sinnoh, door.SourceX, door.SourceY + 1, Direction.Up);
            engine.Steering = (Direction.Up, false);
            Until(engine, () => game.State == GameState.Transition, "the door's fade");
            engine.Steering = null;
            Until(engine, () => game.State == GameState.Overworld, "the room");
            // Arriving is a moment too, but it writes only once the field is free: and it is, here, so let it go first
            Until(engine, () => engine.AutosavesWritten > arrivedAt, "the autosave on arriving");
            int arrived = engine.AutosavesWritten;

            var nurse = game.Map.NPCs.First(n => n.IsHealingNurse);
            game.Place(game.Map, nurse.GridX, nurse.GridY + 2, Direction.Up);
            game.Party.Members[0].CurrentHP = 1;
            game.Interact();
            Assert.True(engine.ScriptRunning);
            bool sawTheScript = false;
            for (int f = 0; f < 60 * 120 && (engine.ScriptRunning || game.State != GameState.Overworld); f++)
            {
                Run(engine, f % 8 == 0 ? ActionFrame.Press(GameAction.Confirm) : ActionFrame.None);
                if (engine.ScriptRunning)
                {
                    sawTheScript = true;
                    Assert.Equal(arrived, engine.AutosavesWritten);
                }
            }
            Assert.True(sawTheScript);
            // The heal was a moment; the write comes once the script has ended and the field is back
            for (int f = 0; f < 60 && engine.AutosavesWritten == arrived; f++) Run(engine, ActionFrame.None);
            Assert.Equal(arrived + 1, engine.AutosavesWritten);
            Assert.Equal(game.Party.Members[0].MaxHP, game.Party.Members[0].CurrentHP);
            // And what it holds is the healed team
            Assert.Equal(game.Party.Members[0].MaxHP, AutosaveFile.Read(SavePaths.Current).Save!.Party[0].CurrentHP);
        });
    }

    [Fact]
    public void WithTheOptionOffNothingIsWritten()
    {
        InAFolder(folder =>
        {
            var engine = NewGame(autosave: false);
            engine.Settings.Autosave = false;
            var game = engine.Drive;
            engine.AutosaveSoon(AutosaveMoment.Battle);
            engine.AutosaveSoon(AutosaveMoment.Badge);
            for (int f = 0; f < 120; f++) Run(engine, ActionFrame.None);
            Assert.Equal(0, engine.AutosavesWritten);
            Assert.False(File.Exists(SavePaths.Current.Autosave));
            Assert.Equal(GameState.Overworld, game.State);
        });
    }

    [Fact]
    public void ArrivingIsHeldToAGapButTheOtherMomentsAreNot()
    {
        InAFolder(folder =>
        {
            var engine = NewGame(autosave: false);
            engine.Settings.Autosave = true;
            engine.AutosaveSoon(AutosaveMoment.Badge);
            for (int f = 0; f < 10; f++) Run(engine, ActionFrame.None);
            Assert.Equal(1, engine.AutosavesWritten);
            // A place come to straight afterwards is within the gap: nothing is written
            engine.AutosaveSoon(AutosaveMoment.Arrival);
            for (int f = 0; f < 10; f++) Run(engine, ActionFrame.None);
            Assert.Equal(1, engine.AutosavesWritten);
            // A battle's is not held
            engine.AutosaveSoon(AutosaveMoment.Battle);
            for (int f = 0; f < 10; f++) Run(engine, ActionFrame.None);
            Assert.Equal(2, engine.AutosavesWritten);
        });
    }

    [Fact]
    public void ASaveByHandTakesAwayItsSlotsAutosaveAndNoOthers()
    {
        InAFolder(folder =>
        {
            var engine = NewGame();
            var game = engine.Drive;
            engine.AutosaveSoon(AutosaveMoment.Battle);
            for (int f = 0; f < 10; f++) Run(engine, ActionFrame.None);
            Assert.True(File.Exists(SavePaths.Current.Autosave));
            int slot = game.SaveSlot;

            // A game saved by hand in another slot leaves it be: the autosave is the first slot's
            game.SaveSlot = slot == 1 ? 2 : 1;
            Assert.True(game.Save());
            Assert.True(File.Exists(SavePaths.Current.Autosave));
            Assert.Equal(slot, AutosaveFile.SlotOf(SavePaths.Current));

            game.SaveSlot = slot;
            Assert.True(game.Save());
            Assert.False(File.Exists(SavePaths.Current.Autosave));
            Assert.False(AutosaveFile.Exists(SavePaths.Current));
        });
    }

    [Fact]
    public void ACrashLeavesTheAutosaveAndTheNextStartTakesItUp()
    {
        InAFolder(folder =>
        {
            // A game never saved by hand, autosaved at Sandgem Town's door, and the process gone
            var engine = NewGame();
            var game = engine.Drive;
            game.Party.Members[0].CurrentHP = 7;
            engine.AutosaveSoon(AutosaveMoment.Catch);
            for (int f = 0; f < 10; f++) Run(engine, ActionFrame.None);
            int slot = game.SaveSlot;
            var (x, y) = (game.Player.GridX, game.Player.GridY);
            Assert.False(File.Exists(SavePaths.Current.Slot(slot)));

            // The next start: the slot has no save of its own but a card for the autosave
            var again = Headless(autosave: false);
            var card = Assert.Single(again.Drive.Title.Cards);
            Assert.True(card.AutosaveOnly);
            Assert.Equal(slot, card.Number);
            Assert.Equal("Turtwig", card.Save!.Party[0].SpeciesName);
            // Continued from it, the game is where it was
            again.ContinueGame(slot, fromAutosave: true);
            Until(again, () => again.Drive.State == GameState.Overworld, "the continued game");
            Assert.Equal((x, y), (again.Drive.Player.GridX, again.Drive.Player.GridY));
            Assert.Equal(7, again.Drive.Party.Members[0].CurrentHP);
        });
    }

    [Fact]
    public void ANewerAutosaveIsOfferedBesideTheSavedGameAndAnOlderOneNot()
    {
        InAFolder(folder =>
        {
            var engine = NewGame();
            var game = engine.Drive;
            Assert.True(game.Save());
            int slot = game.SaveSlot;
            var saved = SaveSlots.Read(SavePaths.Current, slot).Save!;

            // An autosave of the same game, with more time played, is offered with the slot's card
            for (int f = 0; f < 300; f++) Run(engine, ActionFrame.None);
            game.Party.Members[0].CurrentHP = 5;
            engine.AutosaveSoon(AutosaveMoment.Heal);
            for (int f = 0; f < 10; f++) Run(engine, ActionFrame.None);

            var title = Headless(autosave: false).Drive.Title;
            var card = Assert.Single(title.Cards);
            Assert.False(card.AutosaveOnly);
            Assert.NotNull(card.Autosave);
            Assert.True(card.Autosave!.PlayTimeSeconds > card.Save!.PlayTimeSeconds);

            // Continue asks which; the autosave is highlighted, and Yes takes it up
            title.PressConfirm();
            title.PressConfirm();
            Assert.Equal(TitlePhase.Menu, title.Phase);
            title.PressConfirm();
            Assert.Equal(TitlePhase.ChooseAutosave, title.Phase);
            Assert.True(title.ConfirmYes);
            title.Move(1);
            Assert.False(title.ConfirmYes);
            title.PressCancel();
            Assert.Equal(TitlePhase.Menu, title.Phase);
            title.PressConfirm();
            title.PressConfirm();
            Assert.Equal(TitlePhase.Leaving, title.Phase);
            Assert.True(title.UseAutosave);
            Assert.Equal(slot, title.Slot);

            // The same, answered with the saved game
            var other = Headless(autosave: false).Drive.Title;
            other.PressConfirm();
            other.PressConfirm();
            other.PressConfirm();
            other.Move(1);
            other.PressConfirm();
            Assert.False(other.UseAutosave);

            // An autosave that isn't newer (a game saved by hand since: the file went with the save) is no offer
            Assert.True(game.Save());
            Assert.Null(AutosaveFile.Offer(SavePaths.Current, SaveSlots.Read(SavePaths.Current)));
            Assert.True(saved.PlayTimeSeconds <= SaveSlots.Read(SavePaths.Current, slot).Save!.PlayTimeSeconds);
        });
    }

    [Fact]
    public void AnAutosaveOfAnotherGameOrOneThatCantBeReadIsNoOffer()
    {
        InAFolder(folder =>
        {
            var paths = SavePaths.Current;
            var game = new SaveData { PlayerName = "Lucas", TrainerId = 4321, Started = new DateTime(2026, 6, 1), PlayTimeSeconds = 100 };
            var autosave = new SaveData { PlayerName = "Lucas", TrainerId = 4321, Started = new DateTime(2026, 6, 1), PlayTimeSeconds = 200 };
            Assert.True(SaveSlots.Write(paths, 2, game));
            Assert.True(AutosaveFile.Write(paths, 2, autosave));
            var slots = SaveSlots.Read(paths);
            var offer = AutosaveFile.Offer(paths, slots);
            Assert.NotNull(offer);
            Assert.False(offer!.Orphan);
            Assert.Equal(2, offer.Slot);

            // The slot holds another game now (a new game begun over a deleted one)
            Assert.True(SaveSlots.Write(paths, 2, new SaveData { PlayerName = "Dawn", TrainerId = 99, Started = new DateTime(2026, 7, 1), PlayTimeSeconds = 5 }));
            Assert.Null(AutosaveFile.Offer(paths, SaveSlots.Read(paths)));

            // Damaged
            File.WriteAllText(paths.Autosave, "{ not json");
            Assert.Null(AutosaveFile.Offer(paths, SaveSlots.Read(paths)));
            // Cleared
            Assert.True(AutosaveFile.Clear(paths));
            Assert.False(AutosaveFile.Exists(paths));
        });
    }

    [Fact]
    public void DeletingASlotTakesItsAutosaveAway()
    {
        InAFolder(folder =>
        {
            var engine = NewGame();
            var game = engine.Drive;
            Assert.True(game.Save());
            engine.AutosaveSoon(AutosaveMoment.Heal);
            for (int f = 0; f < 300; f++) Run(engine, ActionFrame.None);
            Assert.True(AutosaveFile.Exists(SavePaths.Current));
            int slot = game.SaveSlot;

            // On the title of a fresh start, DELETE A SAVE and a yes
            var again = Headless(autosave: false);
            var title = again.Drive.Title;
            title.PressConfirm();
            title.PressConfirm();
            while (title.Entries[title.SelectedIndex] != TitleChoice.Delete) title.Move(1);
            title.PressConfirm();
            title.Move(1);
            Assert.True(title.ConfirmYes);
            title.PressConfirm();
            InputManager.Feed(ActionFrame.None);
            again.Update(Frame);
            Assert.False(File.Exists(SavePaths.Current.Slot(slot)));
            Assert.False(AutosaveFile.Exists(SavePaths.Current));
        });
    }

    // ------------------------------------------------------------------ the options

    [Fact]
    public void TheResetGivesTheDefaultsAndKeepsTheScenesSeen()
    {
        var s = new GameSettings
        {
            Quality = GraphicsQuality.Low, MusicVolume = 30, Muted = true, Language = "ro", Autosave = true, FrameCap = 120,
            ButtonMode = ButtonMode.StartIsX, Frame = TextFrame.Wood, SkipScenes = SkipScenes.Any, TimeOfDay = TimeOfDay.Night,
            FieldHelpers = RulesDefault.Off, WindowWidth = 1280, WindowHeight = 720
        };
        s.Bindings["Confirm"] = new SavedBinding();
        s.SeeScene("opening.Lab");
        s.SetForThisRun(nameof(GameSettings.Fullscreen), true);
        s.ResetToDefaults();
        var fresh = new GameSettings();
        fresh.SeeScene("opening.Lab");
        Assert.Equal(fresh.ToFile(), s.ToFile());
        Assert.Equal(new[] { "opening.Lab" }, s.ScenesSeen);
        // A reset leaves nothing of a switch for this run: the file now says the Fullscreen default
        Assert.False(s.Fullscreen);

        var plain = new GameSettings { MusicVolume = 10 };
        plain.ResetToDefaults();
        Assert.Equal(new GameSettings().ToFile(), plain.ToFile());
    }

    [Fact]
    public void TheResetRowIsTheLastAndAsksFirst()
    {
        var rows = Enum.GetValues<OptionRow>();
        Assert.Equal(OptionRow.ResetDefaults, rows[^1]);
        Assert.Equal(OptionRow.Help, rows[^2]);
        Assert.True(Array.IndexOf(rows, OptionRow.Autosave) < Array.IndexOf(rows, OptionRow.Help));

        var settings = new GameSettings { MusicVolume = 20, Quality = GraphicsQuality.Medium };
        var screen = new OptionsScreen();
        screen.Open();
        while (Enum.GetValues<OptionRow>()[screen.SelectedIndex] != OptionRow.ResetDefaults) screen.Move(1);
        Assert.False(screen.ResetAsking);
        screen.Confirm(settings);
        Assert.True(screen.ResetAsking);
        Assert.False(screen.ResetYes);
        // No: nothing changes, and the question is put away
        Assert.False(screen.AnswerReset(settings, false));
        Assert.False(screen.ResetAsking);
        Assert.Equal(20, settings.MusicVolume);
        // Yes: the defaults, and the caller is told to apply and save them
        screen.Confirm(settings);
        Assert.True(screen.AnswerReset(settings, true));
        Assert.False(screen.ResetAsking);
        Assert.Equal(new GameSettings().ToFile(), settings.ToFile());
        // Answered once: a second answer does nothing
        Assert.False(screen.AnswerReset(settings, true));
    }

    [Fact]
    public void TheAutosaveRowGoesOnAndOffAndIsSavedWithTheOptions()
    {
        var s = new GameSettings();
        Assert.False(s.Autosave);
        OptionsScreen.Change(s, OptionRow.Autosave, 1);
        Assert.True(s.Autosave);
        string file = Path.Combine(Path.GetTempPath(), "options-" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            s.Save(file);
            Assert.True(GameSettings.Load(file).Autosave);
        }
        finally
        {
            File.Delete(file);
        }
        OptionsScreen.Change(s, OptionRow.Autosave, 1);
        Assert.False(s.Autosave);
    }
}
