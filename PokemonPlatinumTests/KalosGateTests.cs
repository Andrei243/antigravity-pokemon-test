using System.Numerics;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 30's gate, opened by the user on 2026-11-12: Kalos's file says <see cref="MovementDirections.Free"/>,
/// <see cref="MovementSpeed.Analog"/> and the grown kit. A Kalos game moves freely with no override, on skates too, the
/// options' MOVEMENT row still puts it on the four-way grid, a save made between tiles wakes there, and a crossing out
/// of Kalos leaves nothing of it behind (the body back on its tile's centre, the grid, the chibi kit), and back in again
/// brings it all back.
/// </summary>
[Collection("Shared clocks and dice")]
public class KalosGateTests
{
    private const float Frame = 1f / 60f;

    private static void Play(Action<GameEngine> play)
    {
        string folder = Path.Combine(Path.GetTempPath(), "kalosgate-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var paths = SavePaths.Current;
        try
        {
            SavePaths.Current = SavePaths.In(folder);
            Dice.Seed(12);
            var engine = new GameEngine(new GameSettings { Muted = true });
            engine.InitializeHeadless();
            engine.PeopleStayPut = true;
            engine.NewGameRegion = RegionDatabase.Kalos;
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

    // A tile of Kalos's arrival town with three plain walks to the east of it (no grass, no stairs), to skate on
    private static (int X, int Y) OpenLane(Map map, int nearX, int nearY)
    {
        for (int r = 0; r < 40; r++)
            for (int y = nearY - r; y <= nearY + r; y++)
                for (int x = nearX - r; x <= nearX + r; x++)
                {
                    if (!map.InBounds(x, y) || !map.IsWalkable(x, y)) continue;
                    bool open = true;
                    for (int i = 0; i < 4 && open; i++)
                    {
                        var walker = new Walker(TravelMode.Skating, map.HeightAt(x + i, y));
                        var step = FieldMovement.Step(map, x + i, y, Direction.Right, walker);
                        open = step.Kind == StepKind.Walk && FreeMovement.Plain(map, x + i + 1, y);
                    }
                    if (open) return (x, y);
                }
        throw new InvalidOperationException("no open lane near the arrival");
    }

    [Fact]
    public void AKalosGameMovesFreelyOnItsOwnFileAndSkatesFreely()
    {
        Play(engine =>
        {
            var game = engine.Drive;
            Assert.Null(engine.RegionMovementOverride);
            Assert.Equal(MovementDirections.Free, engine.MovementHere.Directions);
            Assert.Equal(MovementSpeed.Analog, engine.MovementHere.Speed);
            Assert.True(engine.MovementHere.Offers(RegionMode.Skates));
            Assert.Equal(CharacterKit.Grown, engine.KitHere);

            var kalos = MapDatabase.Get("Kalos");
            var arrival = RegionDatabase.Get(RegionDatabase.Kalos)!.ArrivalSpot!;
            var (x, y) = OpenLane(kalos, arrival.X, arrival.Y);
            game.Place(kalos, x, y, Direction.Right);
            game.TrainersLookOnArrival = false;
            Run(engine, 2);
            Assert.True(game.Player.MovesFreely(kalos));
            Assert.Equal(CameraOf(kalos), FieldCamera.Kalos);

            // Pushed a little to the east and let go: the body stands off its tile's centre
            for (int f = 0; f < 6; f++) { engine.Stick = (new Vector2(0.3f, 0f), false); InputManager.Feed(ActionFrame.None); engine.Update(Frame); }
            engine.Stick = null;
            Run(engine, 1);
            Assert.True(game.Player.Offset != (0, 0) || game.Player.GridX != x, "the stick moved nothing");

            // With the skates given, the run held at full tilt skates freely
            game.Place(kalos, x, y, Direction.Right);
            game.Story.Set(SkateRules.Flag);
            bool skated = false;
            for (int f = 0; f < 40; f++)
            {
                engine.Stick = (new Vector2(1f, 0f), true);
                InputManager.Feed(ActionFrame.None);
                engine.Update(Frame);
                skated |= game.Player.Mode == TravelMode.Skating;
            }
            engine.Stick = null;
            Run(engine, 60);
            Assert.True(skated, "the skates were never put on under free movement");
            Assert.True(game.Player.GridX > x, $"skated from {x} to {game.Player.GridX}");

            // The options' MOVEMENT row puts Kalos back on the grid's four ways
            engine.Settings.Movement = MovementChoice.FourWays;
            Assert.Equal(MovementDirections.Four, engine.MovementHere.Directions);
            Run(engine, 1);
            Assert.False(game.Player.MovesFreely(kalos));
            engine.Settings.Movement = MovementChoice.AsTheRegion;
            Run(engine, 1);
            Assert.True(game.Player.MovesFreely(kalos));
        });
    }

    private static FieldCamera CameraOf(Map map) => map.Camera;

    [Fact]
    public void ASaveBetweenTilesInKalosWakesThereAndACrossingLeavesNothingOfKalosBehind()
    {
        Play(engine =>
        {
            var game = engine.Drive;
            var kalos = MapDatabase.Get("Kalos");
            var arrival = RegionDatabase.Get(RegionDatabase.Kalos)!.ArrivalSpot!;
            var (x, y) = OpenLane(kalos, arrival.X, arrival.Y);
            game.Place(kalos, x, y, Direction.Right);
            game.TrainersLookOnArrival = false;
            Run(engine, 2);

            // Saved between tiles and read back, the body wakes where it stood
            game.Player.SetOffset(-40, 33);
            var save = game.BuildSave();
            Assert.Equal((-40, 33), (save.PlayerSubX, save.PlayerSubY));
            game.ApplySave(save);
            Assert.Equal((-40, 33), game.Player.Offset);
            Assert.Equal((x, y), (game.Player.GridX, game.Player.GridY));

            // Crossed out of Kalos off its tile's centre (to Alola, which keeps eight ways and the chibi kit for now)
            game.Story.Set("KalosHallOfFame");
            game.TravelTo("Alola");
            Run(engine, 240);
            Assert.Equal("Alola", engine.Here.Id);
            Assert.Equal((0, 0), game.Player.Offset);
            Assert.Equal(MovementDirections.Eight, engine.MovementHere.Directions);
            Assert.False(game.Player.MovesFreely(game.Map));
            Assert.Equal(CharacterKit.Chibi, engine.KitHere);
            Assert.NotEqual(TravelMode.Skating, game.Player.Mode);

            // And back into Kalos: free and grown again
            game.TravelTo("Kalos");
            Run(engine, 240);
            Assert.Equal(RegionDatabase.Kalos, engine.Here.Id);
            Assert.Equal(MovementDirections.Free, engine.MovementHere.Directions);
            Assert.Equal(CharacterKit.Grown, engine.KitHere);
        });
    }
}
