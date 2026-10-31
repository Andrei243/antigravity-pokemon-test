using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Battle.Sim.Ai;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;
using Xunit;
using static PokemonPlatinumTests.Scenario;

namespace PokemonPlatinumTests;

/// <summary>
/// The Vs. Recorder's viewer (plan 08 · P11): the list of the battles kept, a file the game can't play shown with its
/// notice, USE on the Vs. Recorder handing it to the field, and a recording watched as a battle with nobody choosing:
/// the same lines as the battle fought, Run held three times as fast and never leaving, Menu leaving at any line, and
/// nothing of the game changed by any of it. The games here save into folders of the tests' own and share the dice,
/// the clocks and the input with the whole process, so the class runs alone.
/// </summary>
[Collection("Shared clocks and dice")]
public class ReplayViewerTests
{
    private const float Frame = 1f / 60f;
    private static readonly DateTime Day = new(2026, 10, 20);

    // ------------------------------------------------------------------ watching, with no game

    // A trainer's battle of two Pokémon a side fought on the battle's own screen and menus, and its recording
    private static (BattleEngine Battle, BattleRecordFile File) Recorded(uint seed = 20261020)
    {
        var mine = new Party();
        mine.Add(Mon("Monferno", 20, "Ember", "Mach Punch", "Taunt"));
        mine.Add(Mon("Staravia", 18, "Wing Attack", "Quick Attack"));
        var trainer = new Trainer { Id = "replay_test", Name = "Tristan", TrainerClass = "Youngster", PrizeMoney = 64, Ai = AiFlags.Basic };
        trainer.Party.Add(Mon("Shinx", 17, "Tackle", "Leer", "Spark"));
        trainer.Party.Add(Mon("Budew", 18, "Absorb", "Stun Spore", "Mega Drain"));
        var battle = new BattleEngine(new BattleSetup
        {
            PlayerParty = mine, Inventory = new Inventory(), Pokedex = new Pokedex(), Trainers = { trainer },
            Random = new BattleRandom(seed), Rules = Ruleset.Platinum, PlayerName = "Lucas"
        });
        Settle(battle);
        for (int turn = 0; turn < 60 && !battle.IsBattleOver; turn++)
        {
            int bench = Enumerable.Range(0, mine.Count).FirstOrDefault(i => !mine.Members[i].IsFainted && battle.PlayerSlots.All(b => b.Pokemon != mine.Members[i]), -1);
            if (battle.IsChoosingReplacement) battle.SelectSwitch(bench);
            else if (turn == 1 && bench >= 0) battle.SelectSwitch(bench);
            else battle.SelectMove(0);
            Settle(battle);
        }
        Assert.True(battle.IsBattleOver);
        return (battle, battle.RecordFile(Day)!);
    }

    // Watches to the end, Run held or not, and says how many frames it took and every line shown
    private static (int Frames, List<string> Lines) WatchToTheEnd(BattleEngine watched, bool fast)
    {
        var lines = new List<string>();
        bool waiting = false;
        int frames = 0;
        for (; frames < 60 * 60 * 20 && !watched.WatchOver; frames++)
        {
            watched.Watch(Frame, fast, leave: false);
            if (watched.IsWaitingForConfirm && !waiting) lines.Add(watched.CurrentMessage);
            waiting = watched.IsWaitingForConfirm;
        }
        return (frames, lines);
    }

    [Fact]
    public void ARecordingWatchedIsTheBattleFoughtLineForLineWithNobodyChoosing()
    {
        var (fought, file) = Recorded();
        var watched = BattleEngine.ForReplay(file);
        Assert.True(watched.Watching);
        Assert.Same(file, watched.Watched);

        var (_, lines) = WatchToTheEnd(watched, fast: false);
        Assert.True(watched.IsBattleOver);
        Assert.False(watched.WatchLeft);
        Assert.False(watched.WatchBroke);
        Assert.Equal(fought.Result, watched.Result);
        Assert.Equal(file.Answers.Count, watched.AnswersFed);
        Assert.Equal(Golden.BattleText.Written(fought.Core), Golden.BattleText.Written(watched.Core));
        // No menu ever opened: every line went on by itself
        Assert.NotEmpty(lines);
        Assert.NotEqual(BattleMenuState.Main, watched.HUD.MenuState);
    }

    [Fact]
    public void MenuLeavesAtAnyLineAndRunHeldNeverDoes()
    {
        var (_, file) = Recorded();

        // Run held the whole way: never left, to the end, in at most half the frames
        var (slowFrames, slowLines) = WatchToTheEnd(BattleEngine.ForReplay(file), fast: false);
        var held = BattleEngine.ForReplay(file);
        var (fastFrames, fastLines) = WatchToTheEnd(held, fast: true);
        Assert.False(held.WatchLeft);
        Assert.True(held.IsBattleOver);
        Assert.Equal(file.Result, held.Result);
        Assert.Equal(slowLines, fastLines);
        Assert.True(fastFrames * 2 <= slowFrames, $"Held, it took {fastFrames} frames against {slowFrames}.");

        // Menu at the nth line shown leaves there, whatever line it is, and nothing goes on afterwards
        for (int n = 0; n < slowLines.Count; n++)
        {
            var watched = BattleEngine.ForReplay(file);
            int shown = 0;
            bool waiting = false;
            for (int f = 0; f < 60 * 60 * 20 && shown <= n; f++)
            {
                watched.Watch(Frame, fast: true, leave: false);
                if (watched.IsWaitingForConfirm && !waiting) shown++;
                waiting = watched.IsWaitingForConfirm;
            }
            Assert.Equal(slowLines[n], watched.CurrentMessage);
            watched.Watch(Frame, fast: false, leave: true);
            Assert.True(watched.WatchLeft, $"Menu at line {n} didn't leave.");
            Assert.True(watched.WatchOver);
            int fed = watched.AnswersFed;
            for (int f = 0; f < 600; f++) watched.Watch(Frame, fast: true, leave: false);
            Assert.Equal(slowLines[n], watched.CurrentMessage);
            Assert.Equal(fed, watched.AnswersFed);
        }
    }

    [Fact]
    public void ARecordWhoseAnswersRunOutBreaksOffAndLeavesNothingWaiting()
    {
        var (_, file) = Recorded();
        file.Answers.RemoveRange(1, file.Answers.Count - 1);
        var watched = BattleEngine.ForReplay(file);
        WatchToTheEnd(watched, fast: true);
        Assert.True(watched.WatchBroke);
        Assert.True(watched.WatchOver);
        Assert.False(watched.WatchLeft);
        Assert.Equal(1, watched.AnswersFed);
    }

    // ------------------------------------------------------------------ the list

    [Fact]
    public void TheListShowsEveryFileNewestFirstAndTheRefusedWithTheirNotice()
    {
        string folder = Path.Combine(Path.GetTempPath(), "replays-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            var (_, file) = Recorded();
            string good = file.WriteTo(folder);
            file.Day = Day.AddDays(1);
            string newer = file.WriteTo(folder);
            File.WriteAllText(Path.Combine(folder, "20261019-Damaged.json"), "{ this is not a recording");
            var other = BattleRecordFile.Read(file.Serialize()).File!;
            other.DataVersion = "0000000000000000";
            File.WriteAllText(Path.Combine(folder, "20261018-OtherData.json"), other.Serialize());
            File.WriteAllText(Path.Combine(folder, "notes.txt"), "not a recording at all");

            var entries = ReplayListScreen.Read(folder);
            Assert.Equal(new[] { Path.GetFileNameWithoutExtension(newer), Path.GetFileNameWithoutExtension(good), "20261019-Damaged", "20261018-OtherData" },
                entries.Select(e => e.FileName));
            Assert.True(entries[0].Playable && entries[1].Playable);
            Assert.Equal(ReplayRefusal.Damaged, entries[2].Refused);
            Assert.Equal(ReplayRefusal.OtherDataVersion, entries[3].Refused);
            Assert.All(entries.Skip(2), e =>
            {
                Assert.False(e.Playable);
                Assert.Equal(BattleRecordFile.Notice(e.Refused), e.Notice);
                Assert.False(string.IsNullOrWhiteSpace(e.Notice));
            });
            Assert.All(entries.Take(2), e => Assert.Equal("", e.Notice));

            // A refused file can't be chosen; a playable one is handed over once and closes the list
            var screen = new ReplayListScreen();
            screen.Open(entries);
            screen.Move(2);
            screen.Confirm();
            Assert.True(screen.IsActive);
            Assert.Null(screen.TakeChoice());
            screen.Move(-2);
            screen.Confirm();
            Assert.False(screen.IsActive);
            Assert.Equal(entries[0], screen.TakeChoice());
            Assert.Null(screen.TakeChoice());
            // The cursor wraps, and B closes with nothing chosen
            screen.Resume();
            screen.Move(-1);
            Assert.Equal(entries.Count - 1, screen.Cursor);
            screen.Cancel();
            Assert.False(screen.IsActive);
            Assert.Null(screen.TakeChoice());
        }
        finally
        {
            try { Directory.Delete(folder, recursive: true); } catch (IOException) { }
        }
    }

    [Fact]
    public void AFolderThatIsntThereHasNoRecordingsAndTheListShowsWhatItsPanelHolds()
    {
        Assert.Empty(ReplayListScreen.Read(Path.Combine(Path.GetTempPath(), "no-replays-" + Guid.NewGuid().ToString("N"))));
        Assert.Equal(ModernUi.RowsIn(ModernUi.ContentBottom - ModernUi.ContentTop), ReplayListScreen.VisibleRows);
    }

    [Fact]
    public void UseOnTheVsRecorderHandsItToTheField()
    {
        var recorder = ItemDatabase.Get(BattleRecordFile.RecorderItem)!;
        Assert.True(BagScreen.UsedInField(recorder));
        Assert.Equal(new[] { BagAction.Use, BagAction.Register, BagAction.Cancel }, BagScreen.ActionsFor(recorder));

        var bag = new Inventory();
        bag.AddItem(recorder, 1);
        var screen = new BagScreen();
        screen.Open();
        screen.CurrentPocket = ItemPocket.KeyItems;
        var said = new List<string>();
        screen.Confirm(bag, new Party(), said.Add);
        screen.Confirm(bag, new Party(), said.Add);
        Assert.False(screen.IsActive);
        Assert.Empty(said);
        Assert.Equal(recorder, screen.TakeFieldUse());
        Assert.Null(screen.TakeFieldUse());
    }

    // ------------------------------------------------------------------ watching in a game

    [Fact]
    public void WatchingChangesNothingInTheGame()
    {
        InAGame((engine, folder, tristan) =>
        {
            var game = engine.Drive;
            var recorder = ItemDatabase.Get(BattleRecordFile.RecorderItem)!;

            // A battle fought and kept, as the Vs. Recorder keeps one
            Win(engine, tristan);
            Until(engine, () => game.AskingToRecord && engine.Choice.IsOpen, "the Vs. Recorder's question");
            Run(engine, ActionFrame.Press(GameAction.Confirm));
            Until(engine, () => game.LastRecorded != null, "the file written");
            Until(engine, () => game.State == GameState.Overworld && !game.Dialogue.IsActive && !engine.ScriptRunning, "the field again", press: true);
            for (int f = 0; f < 30; f++) Run(engine, ActionFrame.None);
            string before = Saved(game);

            // USE on the Vs. Recorder in the bag: its list, the recording chosen
            game.BagScreen.Open();
            game.State = GameState.BagMenu;
            game.BagScreen.CurrentPocket = ItemPocket.KeyItems;
            game.BagScreen.SelectedIndex = game.Bag.GetPocketItems(ItemPocket.KeyItems).FindIndex(s => s.Data == recorder);
            game.BagScreen.Confirm(game.Bag, game.Party, _ => { });
            game.BagScreen.Confirm(game.Bag, game.Party, _ => { });
            Run(engine, ActionFrame.None);
            Assert.Equal(GameState.ReplayList, game.State);
            Assert.Single(game.ReplayList.Entries);
            Assert.True(game.ReplayList.Entries[0].Playable);
            game.ReplayList.Confirm();

            // Watched to its end with Run held: back on the list, with nothing asked and nothing recorded again
            Until(engine, () => game.State == GameState.Battle && game.Battle?.Watching == true, "the recording on screen");
            var watched = game.Battle!;
            for (int f = 0; f < 60 * 60 * 10 && game.State != GameState.ReplayList; f++)
                Run(engine, ActionFrame.Press(GameAction.Run).Next(GameAction.Run));
            Assert.Equal(GameState.ReplayList, game.State);
            Assert.True(watched.IsBattleOver);
            Assert.False(watched.WatchLeft);
            Assert.False(game.ReplayBroke);
            Assert.Null(game.Battle);
            Assert.Null(game.RecordOffered);
            Assert.Single(Directory.GetFiles(Path.Combine(folder, SavePaths.ReplaysName)));

            // Again, left with Menu at a line of its first turn
            game.ReplayList.Confirm();
            Until(engine, () => game.State == GameState.Battle && game.Battle?.Watching == true, "the recording on screen again");
            var again = game.Battle!;
            Until(engine, () => again.IsWaitingForConfirm, "a line of the recording");
            Run(engine, ActionFrame.Press(GameAction.Menu));
            Until(engine, () => game.State == GameState.ReplayList, "the list again");
            Assert.True(again.WatchLeft);

            // B goes back to the bag, and the game is what it was
            game.ReplayList.Cancel();
            Run(engine, ActionFrame.None);
            Assert.Equal(GameState.BagMenu, game.State);
            Assert.True(game.BagScreen.IsActive);
            game.BagScreen.Close();
            game.State = GameState.Overworld;
            Assert.Equal(before, Saved(game));

            // And off the item button, it goes back to the field
            game.UseFieldItem(recorder);
            Assert.Equal(GameState.ReplayList, game.State);
            game.ReplayList.Cancel();
            Run(engine, ActionFrame.None);
            Assert.Equal(GameState.Overworld, game.State);
        });
    }

    // The game as a save would keep it, but for the time played, which the frames move
    private static string Saved(GameEngine.Driver game)
    {
        var save = game.BuildSave();
        save.PlayTimeSeconds = 0;
        save.RunState = null; // the run timer counts the frames too (plan 13 · V10)
        return GameDataFiles.Serialize(save);
    }

    // A game past its opening, with an Empoleon that wins at once, the Vs. Recorder in the bag and its row on, saving
    // into a folder of the test's own, the clocks fixed, put down before Route 202's youngster
    private static void InAGame(Action<GameEngine, string, NPC> test)
    {
        string folder = Path.Combine(Path.GetTempPath(), "viewer-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var paths = SavePaths.Current;
        try
        {
            SavePaths.Current = SavePaths.In(folder);
            WallClock.Fixed = new DateTime(2026, 10, 20, 12, 0, 0);
            GameClock.FixedDate = Day;
            Dice.Seed(31);
            var engine = new GameEngine(new GameSettings { Muted = true, RecordBattles = true });
            engine.InitializeHeadless();
            engine.PeopleStayPut = true;
            var game = engine.Drive;
            engine.NewGameRegion = "Sinnoh";
            engine.StartNewGame();
            StoryMigration.PastTheOpening(game.Story, ScriptLibrary.Default);
            game.Party.Members.Insert(0, new Pokemon(PokemonDatabase.Get("Empoleon")!, 60));
            game.Bag.AddItem(ItemDatabase.Get(BattleRecordFile.RecorderItem)!);
            game.State = GameState.Overworld;
            game.TrainersLookOnArrival = false;

            var sinnoh = MapDatabase.Get("Sinnoh");
            var tristan = sinnoh.FindPerson("youngster_tristan", "route_202")
                ?? throw new InvalidOperationException("Route 202 has no youngster Tristan.");
            game.Place(sinnoh, tristan.GridX, tristan.GridY + 3, Direction.Up);
            test(engine, folder, tristan);
        }
        finally
        {
            InputManager.Feed(null);
            Dice.Seed(null);
            WallClock.Fixed = null;
            GameClock.FixedDate = null;
            Ruleset.Use(RulesPreset.Platinum);
            SavePaths.Current = paths;
            try { Directory.Delete(folder, recursive: true); } catch (IOException) { }
        }
    }

    private static void Win(GameEngine engine, NPC trainer)
    {
        var game = engine.Drive;
        game.StartTrainerBattle(trainer);
        Until(engine, () => game.State == GameState.Battle, "the battle");
        var battle = game.Battle!;
        int hit = Math.Max(0, game.Party.Members[0].Moves.FindIndex(m => m.Data.Category != MoveCategory.Status));
        for (int guard = 0; guard < 60 * 120 && !battle.IsBattleOver; guard++)
        {
            if (battle.HUD.MenuState == BattleMenuState.Main) battle.SelectMove(hit);
            Run(engine, guard % 8 == 0 ? ActionFrame.Press(GameAction.Confirm) : ActionFrame.None);
        }
        Assert.Equal(BattleResult.PlayerVictory, battle.Result);
        Until(engine, () => game.State != GameState.Battle, "the battle's end", press: true);
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
}
