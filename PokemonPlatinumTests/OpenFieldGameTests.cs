using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.OpenWorld;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 32 · OW1 in the whole game with no window: the player put in an empty open map stands there while the field's
/// clock runs two ticks a frame; the place is saved to the millimetre and a continued game wakes there, or on its grid
/// place where the game knows no open map of that name; the hash of what decides play moves with the open place and
/// its ticks and is the grid's own where no open map is in force; and a recording of ten seconds in an empty open map,
/// begun at the title with the save in its slot, plays back twice to the same hashes and save. With
/// <c>WRITE_SAVE_FIXTURES=&lt;folder&gt;</c> it also writes today's save in an open map for the corpus.
/// It seeds the dice and holds the clock, so it runs with the recordings, alone.
/// </summary>
[Collection("Recordings")]
public class OpenFieldGameTests
{
    private const float Frame = 1f / 60f;
    private static readonly DateTime Start = new(2026, 6, 1, 9, 30, 0, DateTimeKind.Utc);

    /// <summary>An open test region's map with nothing in it but a marker to stand on (OW2 gives the open test world its ground).</summary>
    private static OpenMap EmptyMap() =>
        new("OpenTestField", "OpenTest", new[] { new OpenMarker("start", new OpenPos(12_345, 2_500, -6_789), OpenYaw.East + 100) });

    private static void Run(GameEngine engine, int frames)
    {
        for (int f = 0; f < frames; f++)
        {
            InputManager.Feed(ActionFrame.None);
            engine.Update(Frame);
        }
        InputManager.Feed(null);
    }

    [Fact]
    public void ThePlayerInAnOpenMapStandsWhileTheClockRunsAndIsSavedToTheMillimetre()
    {
        Headless.Play(engine =>
        {
            Headless.PastTheOpening(engine);
            var game = engine.Drive;
            var gridRow = StateHash.Of(game.Flow);
            Assert.Null(game.OpenField);
            Assert.False(engine.InOpenField);
            Assert.Equal(FieldKind.Grid, engine.FieldHere);

            var map = EmptyMap();
            game.PlaceOpen(map, "start");
            Assert.True(engine.InOpenField);
            Assert.Same(map, game.OpenMap);
            Assert.Equal(new OpenPos(12_345, 2_500, -6_789), game.OpenPlayer!.Pos);
            // The grid's place is kept beside it, where the player last stood on the grid
            Assert.Equal(("Sinnoh", 116, 886), (game.Map.Name, game.Player.GridX, game.Player.GridY));

            // In an open map the place in the hash is the open one
            var entered = StateHash.Of(game.Flow);
            Assert.Equal(new[] { "place" }, StateHash.Parted(gridRow, entered));

            // Three seconds: 360 ticks, and the player stands where they were put (the capsule is OW3's)
            int steps = game.Pastimes.Steps;
            Run(engine, 180);
            Assert.Equal(360, game.OpenField!.Clock.Tick);
            Assert.Equal(new OpenPos(12_345, 2_500, -6_789), game.OpenPlayer!.Pos);
            Assert.Equal(steps, game.Pastimes.Steps);
            Assert.Equal(GameState.Overworld, game.State);
            Assert.Equal(new[] { "place" }, StateHash.Parted(entered, StateHash.Of(game.Flow)));

            // Saved: the open place to the millimetre, the grid's place beside it
            var save = game.BuildSave();
            Assert.Equal(SaveData.CurrentSave, save.SaveVersion);
            Assert.Equal(new OpenPlace("OpenTestField", new OpenPos(12_345, 2_500, -6_789), OpenYaw.East + 100), save.OpenPlace!.ToPlace());
            Assert.Equal(("Sinnoh", 116, 886), (save.CurrentMapName, save.PlayerGridX, save.PlayerGridY));
            Assert.True(game.Save());
            Assert.Equal(save.OpenPlace.ToPlace(), SaveManager.Read(File.ReadAllText(game.SaveFile))!.OpenPlace!.ToPlace());

            // Continued, with the open map known: the player wakes in it, its clock begun afresh
            game.Place(MapDatabase.Get("Sinnoh"), 120, 880, Direction.Up);
            Assert.Null(game.OpenField);
            Assert.NotEqual(gridRow[1], StateHash.Of(game.Flow)[1]);
            engine.ContinueGame();
            Assert.True(engine.InOpenField);
            Assert.Equal(save.OpenPlace!.ToPlace(), game.OpenField!.Place);
            Assert.Equal(0, game.OpenField.Clock.Tick);
            Assert.Equal(("Sinnoh", 116, 886), (game.Map.Name, game.Player.GridX, game.Player.GridY));

            // Placed back on the grid, the hash's place is the grid's own again, to the bit
            game.Place(MapDatabase.Get("Sinnoh"), 116, 886, Direction.Down);
            Assert.Equal(gridRow[1], StateHash.Of(game.Flow)[1]);
        });
    }

    [Fact]
    public void ASaveInAnOpenMapTheGameDoesntKnowWakesOnItsGridPlace()
    {
        Headless.Play(engine =>
        {
            Headless.PastTheOpening(engine);
            var game = engine.Drive;
            game.PlaceOpen(EmptyMap(), "start");
            string written = SaveManager.Write(game.BuildSave());

            // Another game, with no open map: the save wakes where it last stood on the grid
            Headless.Play(other =>
            {
                File.WriteAllText(other.Drive.SaveFile, written);
                other.ContinueGame();
                Assert.False(other.InOpenField);
                Assert.Equal(("Sinnoh", 116, 886), (other.Drive.Map.Name, other.Drive.Player.GridX, other.Drive.Player.GridY));
                // and its next save stands on the grid
                Assert.Null(other.Drive.BuildSave().OpenPlace);
            });
        });
    }

    [Fact]
    public void AnOpenMapMayNotTakeAGridMapsName()
    {
        Headless.Play(engine =>
        {
            Assert.Throws<ArgumentException>(() => engine.Drive.AddOpenMap(new OpenMap("Sinnoh", "OpenTest")));
            Assert.Throws<ArgumentException>(() => engine.Drive.PlaceOpen(EmptyMap(), "nowhere"));
        });
    }

    [Fact]
    public void TodaysSaveInAnOpenMapForTheCorpusIsTheEnginesOwn()
    {
        string? folder = Environment.GetEnvironmentVariable("WRITE_SAVE_FIXTURES");
        Headless.Play(engine =>
        {
            Headless.PastTheOpening(engine);
            var game = engine.Drive;
            Run(engine, 2);
            game.PlaceOpen(EmptyMap(), "start");
            Run(engine, 2);
            var save = game.BuildSave();
            Assert.NotNull(save.OpenPlace);
            if (folder != null)
            {
                Directory.CreateDirectory(folder);
                string prefix = $"w{SaveData.CurrentWorld}-s{StoryState.CurrentVersion}-v{SaveData.CurrentSave}-";
                File.WriteAllText(Path.Combine(folder, prefix + "in-an-open-map.json"), SaveManager.Write(save));
            }
        });
    }

    [Fact]
    public void TenSecondsRecordedInAnEmptyOpenMapPlayBackTheSameTwice()
    {
        InputRecording? recording = null;
        string recorded = "", first = "", second = "";
        List<MovieCheckpoint>? firstRows = null, secondRows = null;
        long ticksRecorded = 0;

        Sandbox(folder =>
        {
            // The save in slot 1: a game past the opening, stood in the empty open map
            string written = "";
            Headless.Play(engine =>
            {
                Headless.PastTheOpening(engine);
                engine.Drive.PlaceOpen(EmptyMap(), "start");
                written = SaveManager.Write(engine.Drive.BuildSave());
            });
            File.WriteAllText(SavePaths.Current.Slot(1), written);

            // Recorded: the title, CONTINUE on the slot's card, then the open field
            var settings = new GameSettings { Muted = true };
            var recorder = MovieRecorder.Begin(settings, folder, "Sinnoh", seed: 321, fixFrameClock: true);
            var engine = new GameEngine(settings) { NewGameRegion = "Sinnoh" };
            engine.InitializeHeadless();
            engine.Drive.AddOpenMap(EmptyMap());
            int inField = -1;
            var source = new ListInput(f =>
            {
                if (inField < 0 && engine.InOpenField && engine.Drive.State == GameState.Overworld) inField = f;
                if (inField >= 0 && f >= inField + 60 * 10) return null;
                if (f >= 60 * 60) return null;
                return inField < 0 && f % 6 == 0 ? ActionFrame.Press(GameAction.Confirm) : ActionFrame.None;
            }, Start);
            while (source.Next(Frame) is { } frame) recorder.Step(engine, frame);
            Assert.True(inField >= 0, $"the game never came into the open map: {engine.Drive.State}, on {engine.Drive.Map?.Name}");
            ticksRecorded = engine.Drive.OpenField!.Clock.Tick;
            recorded = Final(engine);
            recording = recorder.Recording;
            Movie.Release();
        });
        Assert.NotNull(recording);
        Assert.InRange(ticksRecorded, 120 * 10 - 2, 120 * 10 + 4);
        Assert.Contains("\"OpenPlace\"", recorded);

        using (var stream = new MemoryStream())
        {
            recording!.Write(stream);
            stream.Position = 0;
            recording = InputRecording.Read(stream);
        }

        Sandbox(folder => (first, firstRows) = PlayBack(recording, folder));
        Sandbox(folder => (second, secondRows) = PlayBack(recording, folder));

        Assert.Equal(recorded, first);
        Assert.Equal(recorded, second);
        Assert.Equal(Rows(recording.Checkpoints), Rows(firstRows!));
        Assert.Equal(Rows(recording.Checkpoints), Rows(secondRows!));
    }

    // ---------------------------------------------------------------- helpers (as RecordingTests')

    private static (string Final, List<MovieCheckpoint> Rows) PlayBack(InputRecording recording, string folder)
    {
        var player = new MoviePlayer(recording);
        var engine = player.Prepare(folder);
        engine.Drive.AddOpenMap(EmptyMap());
        player.PlayToEnd(engine);
        Assert.True(player.Parted == null, player.Parted?.ToString());
        Assert.True(player.Done);
        Assert.True(engine.InOpenField);
        string final = Final(engine);
        Movie.Release();
        return (final, player.Checkpoints);
    }

    private static List<string> Rows(IEnumerable<MovieCheckpoint> rows) => rows.Select(r => $"{r.Frame}: {string.Join(",", r.Hash)}").ToList();

    private static string Final(GameEngine engine)
    {
        var flow = engine.Drive.Flow;
        return $"{flow.State} {string.Join(",", StateHash.Of(flow))}\n{SaveManager.Write(engine.Drive.BuildSave())}";
    }

    private static void Sandbox(Action<string> run)
    {
        string folder = Path.Combine(Path.GetTempPath(), "openrec-" + Guid.NewGuid().ToString("N"));
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
