using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumTests;

/// <summary>
/// A secret base in the whole game with no window (plan 21 · H10): Secret Power at a tree on Route 118 opens the base
/// and goes in, what stands in it is laid out in the way, its door leads back out before the tree, and a save written
/// and continued finds the base as it was. Shares the dice, the save folder and the databases, so it runs alone.
/// </summary>
[Collection("Shared clocks and dice")]
public class SecretBaseGameTests
{
    private const float Frame = 1f / 60f;

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

    private static void Until(GameEngine engine, Func<bool> done, string what, bool press = false, int most = 60 * 120)
    {
        for (int f = 0; f < most; f++)
        {
            if (done()) return;
            Run(engine, press && f % 8 == 0 ? ActionFrame.Press(GameAction.Confirm) : ActionFrame.None);
        }
        Assert.Fail($"Waited {most / 60} seconds of game time for {what}; the game is in {engine.Drive.State}.");
    }

    [Fact]
    public void ABaseIsOpenedFurnishedLeftAndFoundAgainFromASave()
    {
        string folder = Path.Combine(Path.GetTempPath(), "bases-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var paths = SavePaths.Current;
        try
        {
            SavePaths.Current = SavePaths.In(folder);
            Dice.Seed(21);
            var engine = Headless();
            var game = engine.Drive;
            engine.NewGameRegion = "Hoenn";
            engine.StartNewGame();
            var digger = new Pokemon(PokemonDatabase.Get("Zigzagoon")!, 30);
            digger.Moves.Clear();
            digger.Moves.Add(new Move(MoveDatabase.Get("Secret Power")!));
            game.Party.Add(digger);
            SecretBases.Add(game.Decorations, DecorationDatabase.IdOf("Small Desk"));
            SecretBases.Add(game.Decorations, DecorationDatabase.IdOf("Surf Mat"));
            game.State = GameState.Overworld;

            // Route 118's tree, faced from the tile under it
            var hoenn = MapDatabase.Get("Hoenn");
            var spot = hoenn.SecretBaseSpots.Single(s => s.Value == 171).Key;
            game.Place(hoenn, spot.X, spot.Y + 1, Direction.Up);
            game.TrainersLookOnArrival = false;
            game.Interact();
            Until(engine, () => game.Map.Name == "SecretBase_Tree2" && game.State == GameState.Overworld && !engine.ScriptRunning, "the base gone into", press: true);
            var layout = SecretBaseLayouts.OfMap("SecretBase_Tree2")!;
            Assert.Equal((layout.PcFrontX, layout.PcFrontY), (game.Player.GridX, game.Player.GridY));
            Assert.Equal((171, spot.X, spot.Y), (game.SecretBase!.Id, game.SecretBase.X, game.SecretBase.Y));

            // A desk set down before the PC's row and laid out in the way; the mat walked over
            var room = game.Map;
            int deskX = 2, deskY = 8;
            Assert.True(!room.IsSolid(deskX, deskY) && !room.IsSolid(deskX + 1, deskY));
            game.SecretBase.Placed.Add(new PlacedDecoration { Slot = 0, X = deskX, Y = deskY });
            game.SecretBase.Placed.Add(new PlacedDecoration { Slot = 1, X = deskX + 1, Y = deskY });
            engine.LayOutSecretBase(room);
            Assert.Contains(room.Props, p => p.Type == PropType.Decoration && (p.X, p.Y) == (deskX, deskY));
            Assert.True(room.IsSolid(deskX, deskY));
            Assert.False(room.IsSolid(deskX + 1, deskY));

            // Out by the door, before the tree
            var door = room.Warps.Single();
            Assert.Equal(("Hoenn", spot.X, spot.Y + 1), (door.TargetMap, door.TargetX, door.TargetY));
            game.Place(room, door.SourceX, door.SourceY - 1, Direction.Down);
            engine.Steering = (Direction.Down, false);
            Until(engine, () => game.Map.Name == "Hoenn" && game.State == GameState.Overworld, "the way out");
            engine.Steering = null;
            Assert.Equal((spot.X, spot.Y + 1), (game.Player.GridX, game.Player.GridY));

            // Saved and continued: the base, its decorations and the PC as they were
            Assert.True(game.Save());
            int slot = game.SaveSlot;
            var again = Headless();
            again.ContinueGame(slot);
            Until(again, () => again.Drive.State == GameState.Overworld, "the continued game");
            Assert.Equal(171, again.Drive.SecretBase!.Id);
            Assert.Equal(2, again.Drive.SecretBase.Placed.Count);
            Assert.True(again.Drive.Decorations.All(d => d.Out));
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
}
