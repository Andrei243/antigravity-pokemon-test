using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Core.Autoplay;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumTests;

[CollectionDefinition("Recordings", DisableParallelization = true)] public class RecordingsCollection { }

/// <summary>
/// Plan 16 · T19: recorded play and a game that repeats. A recording's file reads back as it was written, its frames
/// play back as they were recorded, the hash of what decides play moves with a step and not with time passing, and a
/// headless game fed ten minutes of seeded actions from the title, recorded, then played back twice, ends with the
/// same save and the same hashes all three times; a reading of the computer's clock put into a replay is named by the
/// second it parts at, and a recording of other data is refused. It seeds the dice and holds the clock, so it runs alone.
/// </summary>
[Collection("Recordings")]
public class RecordingTests
{
    private const float Frame = 1f / 60f;
    private static readonly DateTime Start = new(2026, 6, 1, 9, 30, 0, DateTimeKind.Utc);

    private static InputRecording Sample()
    {
        var recording = new InputRecording
        {
            GameVersion = "0.1.0",
            DataVersion = GameDataFiles.Version,
            Seed = 4242,
            Settings = new GameSettings { Muted = true, TextSpeed = TextSpeed.Fast }.InForce(),
            Offset = TimeSpan.FromHours(3),
            Region = "Kanto",
            Saves = new Dictionary<string, byte[]> { ["save1.json"] = new byte[] { 1, 2, 3 }, ["settings.json"] = "{}"u8.ToArray() }
        };
        for (int i = 0; i < 200; i++)
            recording.Frames.Add(new InputFrame(Frame * (1 + i % 3), Start.AddSeconds(i).Ticks, new ActionFrame((uint)i * 7u, (uint)i % 5u), (WindowEvents)(i % 4)));
        recording.Checkpoints.Add(new MovieCheckpoint(59, 1.01, new uint[] { 1, 2, 3, 4, 5, 6, 7 }));
        recording.Checkpoints.Add(new MovieCheckpoint(119, 2.02, new uint[] { 9, 8, 7, 6, 5, 4, 3 }));
        return recording;
    }

    [Fact]
    public void ARecordingsFileReadsBackAsItWasWritten()
    {
        var recording = Sample();
        using var stream = new MemoryStream();
        recording.Write(stream);
        stream.Position = 0;
        var back = InputRecording.Read(stream);

        Assert.Equal(recording.Format, back.Format);
        Assert.Equal(recording.GameVersion, back.GameVersion);
        Assert.Equal(recording.DataVersion, back.DataVersion);
        Assert.Equal(recording.Seed, back.Seed);
        Assert.Equal(recording.Settings, back.Settings);
        Assert.Equal(recording.Offset, back.Offset);
        Assert.Equal(recording.Region, back.Region);
        Assert.Equal(recording.Saves.Keys.OrderBy(k => k), back.Saves.Keys.OrderBy(k => k));
        foreach (var (name, bytes) in recording.Saves) Assert.Equal(bytes, back.Saves[name]);
        Assert.Equal(recording.Frames, back.Frames);
        Assert.Equal(recording.Checkpoints.Select(c => (c.Frame, c.Seconds, string.Join(",", c.Hash))),
            back.Checkpoints.Select(c => (c.Frame, c.Seconds, string.Join(",", c.Hash))));
        // The options come back as they were in force
        var settings = GameSettings.FromJson(back.Settings);
        Assert.True(settings.Muted);
        Assert.Equal(TextSpeed.Fast, settings.TextSpeed);
        // Gzipped: two hundred frames of twenty-one bytes and a header in well under their size
        Assert.True(stream.Length < 200 * 21, $"{stream.Length} bytes");
    }

    [Fact]
    public void TheMovieSourceGivesBackExactlyWhatWasRecorded()
    {
        var recording = Sample();
        var source = new MovieSource(recording);
        var given = new List<InputFrame>();
        // Whatever the loop measures, a recording's own seconds are given
        while (source.Next(0.1f) is { } frame) given.Add(frame);
        Assert.Equal(recording.Frames, given);
        Assert.True(source.Done);
        Assert.Null(source.Next(0.1f));
        Assert.Equal(new DateTime(Start.AddSeconds(5).Ticks, DateTimeKind.Utc), given[5].Utc);
    }

    [Fact]
    public void AHeldReadingIsTheClocksForTheFrame()
    {
        try
        {
            WallClock.Hold(Start, TimeSpan.FromHours(2));
            Assert.Equal(Start, WallClock.UtcNow);
            Assert.Equal(Start.AddHours(2).Ticks, WallClock.Local.Ticks);
            Assert.Equal(11.5f, GameClock.Hour, 3);
            Assert.Equal(new DateTime(2026, 6, 1), GameClock.Today);
            // A tool's fixed moment still wins
            WallClock.Fixed = new DateTime(2000, 1, 1, 6, 0, 0);
            Assert.Equal(2000, WallClock.Local.Year);
        }
        finally
        {
            WallClock.Fixed = null;
            WallClock.LetGo();
        }
        Assert.Null(WallClock.Held);
    }

    [Fact]
    public void ARecordingOfOtherDataIsRefusedWithItsReason()
    {
        var other = new InputRecording { DataVersion = "0123456789abcdef" };
        Assert.Contains("other data", other.Refusal());
        var refused = Assert.Throws<RecordingRefusedException>(() => new MoviePlayer(other));
        Assert.Contains("0123456789abcdef", refused.Message);

        var newer = new InputRecording { Format = InputRecording.FormatVersion + 1, DataVersion = GameDataFiles.Version };
        Assert.Contains("newer", newer.Refusal());
        Assert.Null(Sample().Refusal());
    }

    [Fact]
    public void NoWorkerDrawsFromTheDice()
    {
        // A seeded generator is no safe thing to share between threads: the dice are drawn on the game's own thread
        // alone, and what bakes, meshes and synthesises on workers (the Graphics and Audio folders) keeps its own
        var engine = Path.Combine(Repository.Root, "PokemonPlatinumEngine");
        var drawing = new[] { "Graphics", "Audio" }
            .SelectMany(folder => Directory.EnumerateFiles(Path.Combine(engine, folder), "*.cs", SearchOption.AllDirectories))
            .Where(file => File.ReadAllText(file).Contains("Dice."))
            .Select(file => Path.GetRelativePath(engine, file)).ToList();
        Assert.True(drawing.Count == 0, "Drawn from the dice off the game's thread: " + string.Join(", ", drawing));
    }

    [Fact]
    public void TheHashMovesWithAStepAndNotWithTimePassing()
    {
        Sandbox(folder =>
        {
            Dice.Seed(5);
            var engine = new GameEngine(new GameSettings { Muted = true });
            engine.InitializeHeadless();
            engine.PeopleStayPut = true;
            engine.NewGameRegion = "Sinnoh";
            engine.StartNewGame();
            var game = engine.Drive;
            StoryMigration.PastTheOpening(game.Story, ScriptLibrary.Default);
            game.Party.Add(new Pokemon(PokemonDatabase.Get("Turtwig")!, 30));
            game.Place(MapDatabase.Get("Sinnoh"), 116, 886, Direction.Down);
            game.TrainersLookOnArrival = false;
            game.State = GameState.Overworld;

            var before = StateHash.Of(game.Flow);
            for (int f = 0; f < 90; f++) { InputManager.Feed(ActionFrame.None); engine.Update(Frame); }
            InputManager.Feed(null);
            // Time passed (play time, the field's life, the clock) and nothing that decides play moved
            Assert.Empty(StateHash.Parted(before, StateHash.Of(game.Flow)));

            int x = game.Player.GridX, y = game.Player.GridY;
            var open = new[] { Direction.Down, Direction.Up, Direction.Left, Direction.Right }
                .First(d => game.Map.IsWalkable(x + (d == Direction.Right ? 1 : d == Direction.Left ? -1 : 0), y + (d == Direction.Down ? 1 : d == Direction.Up ? -1 : 0)));
            engine.Steering = (open, false);
            for (int f = 0; f < 60 && (game.Player.GridX, game.Player.GridY) == (x, y); f++) { InputManager.Feed(ActionFrame.None); engine.Update(Frame); }
            engine.Steering = null;
            InputManager.Feed(null);
            Assert.NotEqual((x, y), (game.Player.GridX, game.Player.GridY));
            Assert.Contains("place", StateHash.Parted(before, StateHash.Of(game.Flow)));
        });
    }

    [Fact]
    public void ARecordedGamePlayedBackTwiceEndsInTheSameSave()
    {
        const int Frames = 60 * 600; // ten minutes of game time
        InputRecording? recording = null;
        string recorded = "", first = "", second = "";
        List<MovieCheckpoint>? firstRows = null, secondRows = null;

        Sandbox(folder =>
        {
            // Recorded: a headless game from the title, fed seeded actions (the confirm and start buttons pressed now
            // and then for the first minutes, which take it through the title and the introduction, then the key fuzzer)
            var settings = new GameSettings { Muted = true };
            var recorder = MovieRecorder.Begin(settings, folder, "Sinnoh", seed: 777, fixFrameClock: true);
            var engine = new GameEngine(settings) { NewGameRegion = "Sinnoh" };
            engine.InitializeHeadless();
            var fuzzer = new KeyFuzzer(31);
            var source = new ListInput(f => f >= Frames ? null : Actions(f, fuzzer), Start);
            while (source.Next(Frame) is { } frame) recorder.Step(engine, frame);
            recorded = Final(engine);
            recording = recorder.Recording;
            Movie.Release();
        });
        Assert.NotNull(recording);
        // The actions took the game past the title into a game of its own, which is what the save compared below is of
        Assert.True(recorded.Contains("\"CurrentMapName\""), recorded[..Math.Min(400, recorded.Length)]);
        Assert.Equal(Frames, recording!.Frames.Count);
        Assert.InRange(recording.Checkpoints.Count, 599, 600);

        // Through its file, as a recording is played back by anyone
        using (var stream = new MemoryStream())
        {
            recording.Write(stream);
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

    [Fact]
    public void AReadingOfTheClockPutIntoAReplayIsNamedByTheSecondItPartsAt()
    {
        InputRecording? recording = null;
        Sandbox(folder =>
        {
            var settings = new GameSettings { Muted = true };
            var recorder = MovieRecorder.Begin(settings, folder, "Sinnoh", seed: 99, fixFrameClock: true);
            var engine = new GameEngine(settings) { NewGameRegion = "Sinnoh" };
            engine.InitializeHeadless();
            // (Nothing pressed: the title plays, and nothing a new game sets would cover the money changed below)
            var source = new ListInput(f => f >= 60 * 10 ? null : ActionFrame.None, Start);
            while (source.Next(Frame) is { } frame) recorder.Step(engine, frame);
            recording = recorder.Recording;
            Movie.Release();
        });

        MovieParting? parting = null;
        Sandbox(folder =>
        {
            var player = new MoviePlayer(recording!);
            var engine = player.Prepare(folder);
            // Four and a bit seconds in, something reads the computer's own clock and lets it change the money: a stray
            // clock as one would be found in the engine
#pragma warning disable RS0030
            player.PlayToEnd(engine, f => { if (f == 260) engine.Drive.Money += 1 + (int)(DateTime.UtcNow.Ticks % 7); });
#pragma warning restore RS0030
            parting = player.Parted;
            Movie.Release();
        });
        Assert.NotNull(parting);
        Assert.Equal(5, (int)Math.Floor(parting!.Seconds));
        Assert.Contains("money", parting.Groups);
        Assert.Contains("5", parting.ToString());
    }

    // ---------------------------------------------------------------- helpers

    // The first two minutes press confirm and now and then start; then the fuzzer mashes the pad
    private static ActionFrame Actions(int frame, KeyFuzzer fuzzer)
    {
        if (frame < 60 * 120)
        {
            if (frame % 97 == 0) return ActionFrame.Press(GameAction.Menu);
            return frame % 6 == 0 ? ActionFrame.Press(GameAction.Confirm) : ActionFrame.None;
        }
        return fuzzer.Next();
    }

    private static (string Final, List<MovieCheckpoint> Rows) PlayBack(InputRecording recording, string folder)
    {
        var player = new MoviePlayer(recording);
        var engine = player.Prepare(folder);
        player.PlayToEnd(engine);
        Assert.True(player.Parted == null, player.Parted?.ToString());
        Assert.True(player.Done);
        string final = Final(engine);
        Movie.Release();
        return (final, player.Checkpoints);
    }

    private static List<string> Rows(IEnumerable<MovieCheckpoint> rows) => rows.Select(r => $"{r.Frame}: {string.Join(",", r.Hash)}").ToList();

    // What a game came to: the hash's row and, once a game is under way, the save it would write (with the frame's
    // clock still held, as the save names the time it was written)
    private static string Final(GameEngine engine)
    {
        var flow = engine.Drive.Flow;
        string row = string.Join(",", StateHash.Of(flow));
        string save;
        try
        {
            save = flow.State != GameState.Title && flow.Map != null && flow.Player != null ? SaveManager.Write(engine.Drive.BuildSave()) : "(at the title)";
        }
        catch (Exception e)
        {
            save = $"(no save: {e.GetType().Name})";
        }
        return $"{flow.State} {row}\n{save}";
    }

    // A folder of its own for the saves, and everything a game puts in force put back afterwards
    private static void Sandbox(Action<string> run)
    {
        string folder = Path.Combine(Path.GetTempPath(), "recording-" + Guid.NewGuid().ToString("N"));
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
