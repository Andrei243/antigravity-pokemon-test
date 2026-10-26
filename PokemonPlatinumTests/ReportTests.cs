using PokemonPlatinumEngine;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Core.Diagnostics;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 16 · T20: reports and replays. The Report key writes a folder that stands alone (the recording, the start's
/// save, the options, the log, the report) and replays to the frame it was written in; a crash in a recorded game
/// writes the same folder beside its report, whose movie plays to the frame that threw and throws there again; and
/// <c>--replay</c> is a switch. It seeds the dice and holds the clock, so it runs alone with the recordings.
/// </summary>
[Collection("Recordings")]
public class ReportTests
{
    private const float Frame = 1f / 60f;
    private static readonly DateTime Start = new(2026, 6, 1, 9, 30, 0, DateTimeKind.Utc);

    [Fact]
    public void TheReplaySwitchIsRead()
    {
        var launch = CommandLine.Parse(new[] { "--replay", "repros/20261026-101500" });
        Assert.True(launch.IsValid, string.Join(" ", launch.Errors));
        Assert.Equal("repros/20261026-101500", launch.ReplayFolder);
        Assert.Contains("--replay <folder>", CommandLine.Usage());
        var both = CommandLine.Parse(new[] { "--replay", "here", "--record", "there.ppmovie" });
        Assert.Contains(both.Errors, e => e.Contains("--replay and --record"));
        Assert.False(CommandLine.Parse(new[] { "--replay" }).IsValid);
    }

    [Fact]
    public void TheReportKeyIsF8AndNoFuzzerPressesIt()
    {
        Assert.Equal(new[] { Raylib_cs.KeyboardKey.F8 }, InputBindings.For(ButtonMode.Normal)[GameAction.Report].Keys);
        var fuzzer = new PokemonPlatinumEngine.Core.Autoplay.KeyFuzzer(3);
        for (int i = 0; i < 5000; i++) Assert.False(fuzzer.Next().IsDown(GameAction.Report));
    }

    [Fact]
    public void AReportsFolderHoldsEveryFileItsStartLoadsAndItReplaysToTheFrameItWasWrittenIn()
    {
        InputRecording? recording = null;
        string? folderWritten = null;
        uint[]? hashAtReport = null;
        int reportFrame = -1;

        Sandbox(folder =>
        {
            // A game saved in slot 1 beforehand, which the session continues from the title
            WriteASave(folder);
            var settings = new GameSettings { Muted = true };
            var recorder = MovieRecorder.Begin(settings, folder, "Sinnoh", seed: 515, fixFrameClock: true);
            var engine = new GameEngine(settings) { NewGameRegion = "Sinnoh", Recording = recorder.Recording };
            engine.InitializeHeadless();
            // Confirm pressed now and then through the title: its first card is the save's, which continues the game
            var source = new ListInput(f => f >= 60 * 40 ? null : f % 20 == 0 ? ActionFrame.Press(GameAction.Confirm) : ActionFrame.None, Start);
            while (source.Next(Frame) is { } frame)
            {
                recorder.Step(engine, frame);
                if (engine.Drive.State == GameState.Overworld && engine.Drive.Flow.Map != null) break;
            }
            Assert.True(engine.Drive.State == GameState.Overworld, $"The title didn't continue the save: {engine.Drive.State}");
            for (int f = 0; f < 120; f++) recorder.Step(engine, new InputFrame(Frame, Start.AddSeconds(100 + f * Frame).Ticks, ActionFrame.None));

            // F8
            recorder.Step(engine, new InputFrame(Frame, Start.AddSeconds(103).Ticks, ActionFrame.Press(GameAction.Report)));
            folderWritten = engine.LastReport;
            reportFrame = recorder.Recording.Frames.Count - 1;
            hashAtReport = StateHash.Of(engine.Drive.Flow);
            Assert.NotNull(folderWritten);
            Assert.StartsWith(Path.Combine(folder, ReportFolder.FolderName), folderWritten);
            Assert.Equal(Text.Of("sessionReport.written", ("folder", ReportFolder.Shown(folderWritten!))), engine.Drive.Flow.Toast.Message);

            // Every file but the picture, which a game with no window has none of
            foreach (string name in new[] { ReportFolder.MovieName, ReportFolder.StartName, ReportFolder.SettingsName, ReportFolder.LogName, ReportFolder.ReportName })
                Assert.True(File.Exists(Path.Combine(folderWritten!, name)), $"{name} is missing");
            Assert.Equal(SaveLoad.Ok, SaveManager.Load(Path.Combine(folderWritten!, ReportFolder.StartName)).Outcome);
            Assert.Equal(File.ReadAllBytes(Path.Combine(folder, SavePaths.SlotName(1))), File.ReadAllBytes(Path.Combine(folderWritten!, ReportFolder.StartName)));
            string report = File.ReadAllText(Path.Combine(folderWritten!, ReportFolder.ReportName));
            Assert.Contains(GameDataFiles.Version, report);
            Assert.Contains("Screen: Overworld", report);
            Assert.Contains($"Map: {engine.Drive.Flow.Map!.Name}", report);
            Assert.Contains("--replay", report);
            Assert.NotNull(GameSettings.FromJson(File.ReadAllText(Path.Combine(folderWritten!, ReportFolder.SettingsName))));
            recording = InputRecording.ReadFrom(Path.Combine(folderWritten!, ReportFolder.MovieName));
            Movie.Release();
        });

        // The folder's movie holds the frame it was written in, and plays back to the same state, the start's save
        // laid down from the movie itself
        Assert.Equal(reportFrame + 1, recording!.Frames.Count);
        Assert.True(recording.Frames[^1].Actions.IsPressed(GameAction.Report));
        Sandbox(folder =>
        {
            var player = new MoviePlayer(recording);
            var engine = player.Prepare(folder);
            player.PlayToEnd(engine);
            Assert.True(player.Parted == null, player.Parted?.ToString());
            Assert.True(player.Done);
            Assert.Empty(StateHash.Parted(hashAtReport!, StateHash.Of(engine.Drive.Flow)));
            // A replay writes no report of its own: nothing records it
            Assert.Null(engine.LastReport);
            Movie.Release();
        });
    }

    [Fact]
    public void ACrashInARecordedGameWritesAFolderThatPlaysToItsCrashAndThrowsThereAgain()
    {
        InputRecording? recording = null;
        CrashOutcome? outcome = null;
        int thrownAt = -1;

        // The bug put in on purpose: the first frame the game leaves its title, whatever led there
        static void Trap(GameEngine engine, int frame)
        {
            if (engine.Drive.State != GameState.Title) throw new InvalidOperationException($"Thrown on purpose at frame {frame}.");
        }

        Sandbox(folder =>
        {
            var settings = new GameSettings { Muted = true };
            var recorder = MovieRecorder.Begin(settings, folder, "Sinnoh", seed: 616, fixFrameClock: true);
            GameEngine? engine = null;
            int code = Program.Guarded(() => engine, "(a test)", () =>
            {
                engine = new GameEngine(settings) { NewGameRegion = "Sinnoh", Recording = recorder.Recording };
                CrashReport.ReportFolderWriter = engine.WriteCrashFolder;
                engine.InitializeHeadless();
                var source = new ListInput(f => f >= 60 * 60 ? null : f % 15 == 0 ? ActionFrame.Press(GameAction.Confirm) : ActionFrame.None, Start);
                int n = 0;
                while (source.Next(Frame) is { } frame)
                {
                    recorder.Step(engine, frame);
                    thrownAt = n;
                    Trap(engine, n++);
                }
                thrownAt = -1;
            }, folder);
            Assert.Equal(CommandLine.Crashed, code);
            Assert.True(thrownAt >= 0, "The game never left its title.");
            outcome = CrashReport.Last;
            Assert.NotNull(outcome?.ReportFolder);
            Assert.EndsWith("-crash", outcome!.ReportFolder);
            Assert.Contains($"{ReportFolder.FolderName}/", File.ReadAllText(outcome.ReportPath!));
            string report = File.ReadAllText(Path.Combine(outcome.ReportFolder!, ReportFolder.ReportName));
            Assert.Contains("Thrown on purpose", report);
            Assert.False(File.Exists(Path.Combine(outcome.ReportFolder!, ReportFolder.StartName)), "A game begun from the title has no start save.");
            recording = InputRecording.ReadFrom(Path.Combine(outcome.ReportFolder!, ReportFolder.MovieName));
            Movie.Release();
        });

        // The movie ends with the frame that threw
        Assert.Equal(thrownAt + 1, recording!.Frames.Count);
        Sandbox(folder =>
        {
            var player = new MoviePlayer(recording);
            var engine = player.Prepare(folder);
            int frame = 0;
            var again = Assert.Throws<InvalidOperationException>(() =>
            {
                while (player.Step(engine)) Trap(engine, frame++);
                Trap(engine, frame);
            });
            Assert.Equal($"Thrown on purpose at frame {thrownAt}.", again.Message);
            Assert.Null(player.Parted);
            Assert.True(player.Done);
            Movie.Release();
        });
    }

    // ---------------------------------------------------------------- helpers

    // A game past its opening saved in slot 1 of a folder, written as the save screen writes it
    private static void WriteASave(string folder)
    {
        Dice.Seed(3);
        var engine = new GameEngine(new GameSettings { Muted = true });
        engine.InitializeHeadless();
        engine.NewGameRegion = "Sinnoh";
        engine.StartNewGame();
        var game = engine.Drive;
        StoryMigration.PastTheOpening(game.Story, ScriptLibrary.Default);
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Turtwig")!, 12));
        game.Place(MapDatabase.Get("Sinnoh"), 116, 886, Direction.Down);
        game.State = GameState.Overworld;
        File.WriteAllText(Path.Combine(folder, SavePaths.SlotName(1)), SaveManager.Write(game.BuildSave()));
    }

    // A folder of its own for the saves, and everything a game puts in force put back afterwards
    private static void Sandbox(Action<string> run)
    {
        string folder = Path.Combine(Path.GetTempPath(), "report-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var paths = SavePaths.Current;
        var heard = AudioManager.Listen();
        try
        {
            SavePaths.Current = SavePaths.In(folder);
            run(folder);
        }
        finally
        {
            Movie.Release();
            CrashReport.StopWatching();
            AudioManager.StopListening();
            Dice.Seed(null);
            Ruleset.Use(RulesPreset.Platinum);
            PlayerIdentity.InRegion(RegionDatabase.First);
            InputManager.Reset();
            Text.Use("en");
            GameClock.Fixed = null;
            GameClock.FixedSeason = null;
            SavePaths.Current = paths;
            try { Directory.Delete(folder, recursive: true); } catch (IOException) { }
        }
    }
}
