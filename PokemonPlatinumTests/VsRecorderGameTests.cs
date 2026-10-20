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
/// The Vs. Recorder in a game (plan 08 · P10): a trainer's battle fought in a game with no window ends, once the field
/// is back, with the question "Record this battle?" when the Vs. Recorder is in the bag and the options' row is on;
/// YES writes the file into <c>replays</c> beside the saves, and that file, read back, is fought again to the same end.
/// With the row off nothing is asked and nothing written, but for the Elite Four's and the Champion's battles, which
/// are kept without a word. Each game keeps its saves in a folder of the test's own, and shares the dice and the rules
/// with the whole process, so the class runs alone.
/// </summary>
[Collection("Shared clocks and dice")]
public class VsRecorderGameTests
{
    private const float Frame = 1f / 60f;

    [Fact]
    public void WithTheRowOnATrainersBattleEndsWithTheQuestionAndYesKeepsAFileThatReplays()
    {
        InAGame(recordBattles: true, (engine, folder, tristan) =>
        {
            var game = engine.Drive;
            var battle = Win(engine, tristan);

            // The question waits for the field, and is the Vs. Recorder's own: the script's answer is left alone
            Until(engine, () => game.AskingToRecord && engine.Choice.IsOpen, "the Vs. Recorder's question");
            Assert.Equal(GameState.Dialogue, game.State);
            Assert.Equal(Text.Of("engine.recordBattle.question"), game.Dialogue.CurrentLine);
            Assert.NotNull(game.RecordOffered);
            Assert.False(Directory.Exists(Path.Combine(folder, SavePaths.ReplaysName)));

            // YES (the first answer) writes it, beside the saves, and says so
            Run(engine, ActionFrame.Press(GameAction.Confirm));
            Until(engine, () => game.LastRecorded != null, "the file written");
            Assert.False(game.AskingToRecord);
            Assert.Null(game.RecordOffered);
            string path = game.LastRecorded!;
            Assert.Equal(Path.Combine(folder, SavePaths.ReplaysName), Path.GetDirectoryName(path));
            Assert.True(File.Exists(path));
            Until(engine, () => game.Dialogue.IsActive, "the line that it was kept");
            Assert.Equal(Text.Of("engine.recordBattle.saved"), game.Dialogue.CurrentLine);
            Until(engine, () => game.State == GameState.Overworld && !game.Dialogue.IsActive, "the field again", press: true);

            // Read back and fought again with no screen: the same end, the same lines
            var reading = BattleRecordFile.ReadFile(path);
            Assert.Equal(ReplayRefusal.None, reading.Refused);
            var again = reading.File!.Replay();
            Assert.Equal(battle.Result, again.Result);
            Assert.Equal(Golden.BattleText.Written(battle.Core), Golden.BattleText.Written(again));
            Assert.Equal(RulesPreset.Modern, reading.File.Rules);
        });
    }

    [Fact]
    public void NoKeepsNothing()
    {
        InAGame(recordBattles: true, (engine, folder, tristan) =>
        {
            var game = engine.Drive;
            Win(engine, tristan);
            Until(engine, () => game.AskingToRecord && engine.Choice.IsOpen, "the Vs. Recorder's question");
            engine.Choice.Move(1);
            Run(engine, ActionFrame.Press(GameAction.Confirm));
            Until(engine, () => game.State == GameState.Overworld && !game.Dialogue.IsActive, "the field again", press: true);
            Assert.Null(game.LastRecorded);
            Assert.Null(game.RecordOffered);
            Assert.False(Directory.Exists(Path.Combine(folder, SavePaths.ReplaysName)));
        });
    }

    [Fact]
    public void WithTheRowOffNothingIsAskedOrWritten()
    {
        InAGame(recordBattles: false, (engine, folder, tristan) =>
        {
            var game = engine.Drive;
            Win(engine, tristan);
            Until(engine, () => game.State == GameState.Overworld && !game.Dialogue.IsActive && !engine.ScriptRunning, "the field again", press: true);
            for (int f = 0; f < 120; f++) Run(engine, ActionFrame.None);
            Assert.False(game.AskingToRecord);
            Assert.Null(game.RecordOffered);
            Assert.False(engine.Choice.IsOpen);
            Assert.Null(game.LastRecorded);
            Assert.False(Directory.Exists(Path.Combine(folder, SavePaths.ReplaysName)));
        });
    }

    [Fact]
    public void WithoutTheVsRecorderNothingIsAsked()
    {
        InAGame(recordBattles: true, (engine, folder, tristan) =>
        {
            var game = engine.Drive;
            Win(engine, tristan);
            Until(engine, () => game.State == GameState.Overworld && !game.Dialogue.IsActive && !engine.ScriptRunning, "the field again", press: true);
            Assert.False(game.AskingToRecord);
            Assert.Null(game.LastRecorded);
        }, withRecorder: false);
    }

    [Fact]
    public void TheEliteFoursAndTheChampionsBattlesAreKeptWithoutAWord()
    {
        InAGame(recordBattles: false, (engine, folder, tristan) =>
        {
            var game = engine.Drive;
            // Tristan made one of the Elite Four for the battle (the class is what the Hall of Fame's battles are told by)
            var trainer = tristan.TrainerData!;
            string before = trainer.TrainerClass;
            trainer.TrainerClass = "Elite Four";
            try
            {
                Win(engine, tristan);
                // Written as the battle ends, before the field is back, and nothing is asked
                Assert.NotNull(game.LastRecorded);
                Assert.Equal(Path.Combine(folder, SavePaths.ReplaysName), Path.GetDirectoryName(game.LastRecorded));
                Until(engine, () => game.State == GameState.Overworld && !game.Dialogue.IsActive && !engine.ScriptRunning, "the field again", press: true);
                Assert.False(game.AskingToRecord);
                Assert.Null(game.RecordOffered);
                Assert.Equal(ReplayRefusal.None, BattleRecordFile.ReadFile(game.LastRecorded!).Refused);
            }
            finally
            {
                trainer.TrainerClass = before;
            }
        });
    }

    // A game past its opening, with an Empoleon that wins at once, the Vs. Recorder in the bag and the row as asked,
    // saving into a folder of the test's own, put down before Route 202's youngster
    private static void InAGame(bool recordBattles, Action<GameEngine, string, NPC> test, bool withRecorder = true)
    {
        string folder = Path.Combine(Path.GetTempPath(), "recorder-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var paths = SavePaths.Current;
        try
        {
            SavePaths.Current = SavePaths.In(folder);
            Dice.Seed(31);
            var engine = new GameEngine(new GameSettings { Muted = true, RecordBattles = recordBattles });
            engine.InitializeHeadless();
            engine.PeopleStayPut = true;
            var game = engine.Drive;
            engine.NewGameRegion = "Sinnoh";
            engine.StartNewGame();
            StoryMigration.PastTheOpening(game.Story, ScriptLibrary.Default);
            game.Party.Members.Insert(0, new Pokemon(PokemonDatabase.Get("Empoleon")!, 60));
            if (withRecorder) game.Bag.AddItem(ItemDatabase.Get(BattleRecordFile.RecorderItem)!);
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
            Ruleset.Use(RulesPreset.Platinum);
            SavePaths.Current = paths;
            try { Directory.Delete(folder, recursive: true); } catch (IOException) { }
        }
    }

    // The battle begun as the field begins one, and won with the lead's first damaging move, its lines read on
    private static BattleEngine Win(GameEngine engine, NPC trainer)
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
        Assert.True(battle.IsBattleOver, "The battle never ended.");
        Assert.Equal(BattleResult.PlayerVictory, battle.Result);
        // Read on to the battle's end, and no further: the question waits for the field
        Until(engine, () => game.State != GameState.Battle, "the battle's end", press: true);
        return battle;
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
