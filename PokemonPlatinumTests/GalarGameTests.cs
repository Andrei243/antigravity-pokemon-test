using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumTests;

/// <summary>
/// Galar in the whole game with no window (plan 33 · GL1): the first open region the game loads by itself. A new game
/// there wakes in the bedroom on the grid; out of the house's door the player comes out into the open map on the door's
/// marker, and Hop's scene at the door runs as they walk into it and ends with the story moved on. It seeds the dice, so
/// it runs with the recordings, alone.
/// </summary>
[Collection("Recordings")]
public class GalarGameTests
{
    private const float Frame = 1f / 60f;

    private static void Run(GameEngine engine, int frames, ActionFrame? frame = null)
    {
        for (int f = 0; f < frames; f++)
        {
            InputManager.Feed(frame ?? ActionFrame.None);
            engine.Update(Frame);
        }
        InputManager.Feed(null);
    }

    [Fact]
    public void ANewGameInGalarWalksOutOfTheHouseIntoTheOpenMapAndMeetsHopAtTheDoor()
    {
        Headless.Play(engine =>
        {
            var game = engine.Drive;
            Assert.Equal(("Postwick_PlayersHouse_2F", 4, 6), (game.Map.Name, game.Player.GridX, game.Player.GridY));
            Assert.False(engine.InOpenField);
            Assert.True(game.Story.Has("FLAG_GALAR_STORY_BEGUN"));
            Assert.NotNull(engine.OpenMapNamed("Galar"));
            // Past Mum's first morning, inside the door downstairs, and down onto the way out
            game.Story.SetVar("VAR_GALAR_STORY", 1);
            game.Story.SetVar("VAR_GALAR_VERSION", 1);
            game.Place(MapDatabase.Get("Postwick_PlayersHouse_1F"), 6, 10, Direction.Down);
            for (int f = 0; f < 60 * 5 && !engine.InOpenField; f++) Run(engine, 1, new ActionFrame(1u << (int)GameAction.Down, 0));
            Run(engine, 120);
            Assert.True(engine.InOpenField);
            Assert.Equal("Galar", game.OpenMap!.Name);
            var door = game.OpenMap.Marker("players_door")!;
            Assert.Equal((door.Pos.X, door.Pos.Z), (game.OpenPlayer!.Pos.X, game.OpenPlayer.Pos.Z));
            // South into Hop at the door: his scene, read on to its end
            for (int f = 0; f < 60 * 4 && !engine.ScriptRunning; f++) Run(engine, 1, ActionFrame.Stick(0, 127));
            Assert.True(engine.ScriptRunning);
            for (int f = 0; f < 60 * 60 && engine.ScriptRunning; f++)
                Run(engine, 1, f % 8 == 0 ? ActionFrame.Press(GameAction.Confirm) : ActionFrame.None);
            Assert.False(engine.ScriptRunning);
            Assert.Equal(2, game.Story.Var("VAR_GALAR_STORY"));
            Assert.True(game.Story.Has("FLAG_GALAR_HIDE_HOP_POSTWICK"));
            Assert.True(engine.InOpenField);
        }, region: RegionDatabase.Galar);
    }
}
