using System.Numerics;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Core.Autoplay;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumTests;

/// <summary>
/// Continuous movement in the whole game with no window (plan 30 · FM7 to FM9): a test region (the engine's
/// <c>RegionMovementOverride</c>) walked freely counts every step <c>OnStep</c> counts as a cardinal walk over the same tiles and takes a door
/// from its front; the playthrough bot plays Kalos's opening in <see cref="MovementDirections.Eight"/> and in
/// <see cref="MovementDirections.Free"/> steering a stick; and the field fuzzer steering a stick finds nothing wrong in
/// Kalos.
/// </summary>
[Collection("Shared clocks and dice")]
public class FreeMovementGameTests
{
    private const float Frame = 1f / 60f;

    private static readonly RegionMovement FreeRegion = new() { Directions = MovementDirections.Free, Speed = MovementSpeed.Analog };
    // Kalos as it was before the gate, for the eight ways' half of the opening's test
    private static readonly RegionMovement EightRegion = new() { Directions = MovementDirections.Eight, Modes = new List<RegionMode> { RegionMode.Skates } };

    private readonly ITestOutputHelper output;

    public FreeMovementGameTests(ITestOutputHelper output) => this.output = output;

    // A game with no window begun in a region, in a save folder of its own, everything put back afterwards
    private static void Play(string region, Action<GameEngine> play)
    {
        string folder = Path.Combine(Path.GetTempPath(), "freemove-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var paths = SavePaths.Current;
        try
        {
            SavePaths.Current = SavePaths.In(folder);
            Dice.Seed(12);
            var engine = new GameEngine(new GameSettings { Muted = true });
            engine.InitializeHeadless();
            engine.PeopleStayPut = true;
            engine.NewGameRegion = region;
            engine.StartNewGame();
            engine.Drive.State = GameState.Overworld;
            play(engine);
        }
        finally
        {
            InputManager.Feed(null);
            Dice.Seed(null);
            Ruleset.Use(RulesPreset.Platinum);
            PlayerIdentity.InRegion(RegionDatabase.First);
            PlayerIdentity.SetRival(null);
            SavePaths.Current = paths;
            try { Directory.Delete(folder, recursive: true); } catch (IOException) { }
        }
    }

    private static void Run(GameEngine engine, int frames)
    {
        for (int f = 0; f < frames; f++)
        {
            InputManager.Feed(ActionFrame.None);
            engine.Update(Frame);
        }
    }

    [Fact]
    public void ATestRegionWalkedFreelyCountsTheStepsACardinalWalkCountsAndTakesADoorFromItsFront()
    {
        Play("Sinnoh", engine =>
        {
            Headless.PastTheOpening(engine);
            var game = engine.Drive;
            engine.RegionMovementOverride = FreeRegion;
            var lab = new Map(30, 7) { Name = "FreeLab" };
            lab.Warps.Add(new Warp { SourceX = 24, SourceY = 1, TargetMap = "PlayerHouse", TargetX = 5, TargetY = 8, TargetFacing = Direction.Up });

            // Along row 4, from column 2 to column 20, by the grid's steering and then by the stick
            int Walked(Action steer)
            {
                game.Place(lab, 2, 4, Direction.Right);
                game.TrainersLookOnArrival = false;
                int before = game.Pastimes.Steps;
                for (int f = 0; f < 600 && game.Player.GridX < 20; f++)
                {
                    steer();
                    InputManager.Feed(ActionFrame.None);
                    engine.Update(Frame);
                }
                engine.Steering = null;
                Run(engine, 30);
                Assert.Equal(20, game.Player.GridX);
                return game.Pastimes.Steps - before;
            }
            int cardinal = Walked(() => engine.Steering = (Direction.Right, false));
            int free = Walked(() => engine.Stick = (new Vector2(1f, 0f), false));
            Assert.Equal(18, cardinal);
            Assert.Equal(cardinal, free);
            Assert.True(game.Player.MovesFreely(lab));

            // Up to the door, a little off its line: the body is eased onto it and the warp is taken as the step ends
            game.Place(lab, 23, 4, Direction.Up);
            game.Player.SetOffset(70, 0);
            for (int f = 0; f < 240 && game.Map == lab; f++)
            {
                engine.Stick = (Vector2.Normalize(new Vector2(0.15f, -1f)), false);
                InputManager.Feed(ActionFrame.None);
                engine.Update(Frame);
            }
            engine.Stick = null;
            Run(engine, 90);
            Assert.Equal("PlayerHouse", game.Map.Name);
            Assert.Equal((0, 0), game.Player.Offset);

            // Saved between tiles and read back, the body wakes where it stood
            game.Place(lab, 10, 4, Direction.Right);
            game.Player.SetOffset(-40, 33);
            var save = game.BuildSave();
            Assert.Equal((-40, 33), (save.PlayerSubX, save.PlayerSubY));
            Assert.Equal(SaveData.CurrentSave, save.SaveVersion);
        });
        Assert.Null(new GameEngine(new GameSettings()).RegionMovementOverride);
    }

    /// <summary>
    /// Writes today's saves for the corpus (plan 24 · X3) where the harness can't be run: the engine's own
    /// <c>BuildSave</c> written as <see cref="SaveManager.Write"/> writes a slot's save, as the harness's <c>saves</c> mode
    /// does, past the opening in front of the house and standing between tiles in a free test region. Only with
    /// <c>WRITE_SAVE_FIXTURES=&lt;folder&gt;</c>; otherwise it checks that the second moment is what the corpus has.
    /// </summary>
    [Fact]
    public void TodaysSavesForTheCorpusAreTheEnginesOwn()
    {
        string? folder = Environment.GetEnvironmentVariable("WRITE_SAVE_FIXTURES");
        Play("Sinnoh", engine =>
        {
            Headless.PastTheOpening(engine);
            var game = engine.Drive;
            Run(engine, 2);
            string prefix = $"w{SaveData.CurrentWorld}-s{StoryState.CurrentVersion}-v{SaveData.CurrentSave}-";
            var opening = game.BuildSave();
            engine.RegionMovementOverride = FreeRegion;
            game.Player.SetOffset(40, -30);
            var between = game.BuildSave();
            engine.RegionMovementOverride = null;
            Assert.Equal((40, -30), (between.PlayerSubX, between.PlayerSubY));
            if (folder != null)
            {
                Directory.CreateDirectory(folder);
                File.WriteAllText(Path.Combine(folder, prefix + "past-the-opening.json"), SaveManager.Write(opening));
                File.WriteAllText(Path.Combine(folder, prefix + "between-tiles.json"), SaveManager.Write(between));
            }
        });
    }

    [Fact]
    public void KalossOpeningPlaysThroughOnEightWaysAndFreelyWithTheBotSteeringAStick()
    {
        foreach (var directions in new[] { MovementDirections.Eight, MovementDirections.Free })
        {
            Play("Kalos", engine =>
            {
                var game = engine.Drive;
                // Kalos's own file is free since the gate (2026-11-12): eight ways are laid over it, free is its own
                engine.RegionMovementOverride = directions == MovementDirections.Eight ? EightRegion : null;
                var bot = new Autoplayer(engine, seed: 1) { Itinerary = new Itinerary(Itineraries.KalosOpening) };
                for (double t = 0; t < 1500 && !bot.Stopped; t += Frame)
                {
                    bot.Frame(Frame);
                    if (bot.Itinerary!.Finished && t > 1) break;
                }
                bot.Release();
                var report = bot.Report;
                output.WriteLine($"{directions}: {report.Seconds:0} s of game time");
                output.WriteLine(report.ToMarkdown());
                Assert.True(report.Clean, report.ToMarkdown());
                Assert.Empty(report.LegsGivenUp);
                Assert.True(bot.Itinerary!.Finished, report.ToMarkdown());
                Assert.True(game.Story.Var("VAR_KALOS_STORY") >= 4, $"{directions}: VAR_KALOS_STORY {game.Story.Var("VAR_KALOS_STORY")}");
                Assert.Equal(directions, engine.MovementHere.Directions);
            });
        }
    }

    [Fact]
    public void TheFieldFuzzerSteeringAStickFindsNothingWrongInAFreeKalos()
    {
        double seconds = double.TryParse(Environment.GetEnvironmentVariable("FIELD_FUZZ_SECONDS"), out double s) ? s : 60;
        Play("Kalos", engine =>
        {
            var game = engine.Drive;
            // Kalos's own file (free since the gate), no override
            Assert.Equal(MovementDirections.Free, engine.MovementHere.Directions);
            StoryMigration.PastTheOpening(game.Story, ScriptLibrary.Default);
            game.Party.Add(new Pokemon(PokemonDatabase.Get("Froakie")!, 30));
            game.TrainersLookOnArrival = false;
            var kalos = MapDatabase.Get("Kalos");
            var spots = FieldFuzzer.Spots(kalos, (int)Math.Ceiling(seconds / 10), 5);
            Assert.NotEmpty(spots);
            var fuzzer = new Autoplayer(engine, new FieldFuzzer(5, stick: true));
            string? unsettled = null;
            foreach (var (x, y) in spots)
            {
                if (fuzzer.Stopped) break;
                if (!fuzzer.Settle(60)) { unsettled = $"{game.State} on {game.Map.Name} at {game.Player.GridX},{game.Player.GridY}, script {game.ScriptNow}"; break; }
                foreach (var p in game.Party.Members) p.HealFull();
                game.Place(kalos, x, y, Direction.Down);
                game.ArriveOnMap();
                for (double t = 0; t < 10 && !fuzzer.Stopped; t += Frame) fuzzer.Frame(Frame);
            }
            fuzzer.Release();
            var report = fuzzer.Report;
            output.WriteLine(report.ToMarkdown());
            Assert.Empty(report.Exceptions);
            Assert.Empty(report.Softlocks);
            Assert.Empty(report.OutOfRange);
            Assert.Empty(report.ScriptsGoneWrong);
            Assert.Null(unsettled);
        });
        // The stick's fuzzer is a fuzzer of the same seed, frame for frame
        var a = new FieldFuzzer(9, stick: true);
        var b = new FieldFuzzer(9, stick: true);
        var frames = Enumerable.Range(0, 2000).Select(_ => a.Next()).ToList();
        Assert.Equal(frames, Enumerable.Range(0, 2000).Select(_ => b.Next()).ToList());
        Assert.Contains(frames, f => f.MoveX != 0 && f.MoveY != 0 && Math.Abs(f.MoveX) != Math.Abs(f.MoveY));
        // And without the stick it is the grid's fuzzer, draw for draw
        var grid = new FieldFuzzer(9);
        var same = new FieldFuzzer(9, stick: false);
        Assert.Equal(Enumerable.Range(0, 500).Select(_ => grid.Next()).ToList(), Enumerable.Range(0, 500).Select(_ => same.Next()).ToList());
    }
}
