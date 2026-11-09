using System.IO;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.OpenWorld;
using PokemonPlatinumEngine.Overworld;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 30 · FM12 in the whole game with no window (rulings, "The mount on the open world"): in an open map of a test
/// region whose movement lists the mount (<c>RegionMovementOverride</c>, since no region's file does yet), the Mount
/// key is refused with no mount, gets on once the story has given one, trots at the mount's pace, jumps on A with nobody
/// to speak to, takes the story's powers only while ridden, and is refused off in the air; a fade's midpoint and a
/// script take the player off; a save made riding wakes riding where the mount may be ridden and on foot anywhere else;
/// and nothing of it is in the hash on foot. It seeds the dice, so it runs with the recordings, alone.
/// </summary>
[Collection("Recordings")]
public class MountGameTests
{
    private const float Frame = 1f / 60f;
    private static readonly RegionMovement Paldea = new() { Directions = MovementDirections.Free, Speed = MovementSpeed.Analog, Modes = new() { RegionMode.Mount } };

    private static OpenMap Field()
    {
        var map = new OpenMap("MountTestField", "OpenTest", new[] { new OpenMarker("start", new OpenPos(0, 0, 0), OpenYaw.East) });
        map.Add(OpenBox.Metres("floor", -100, -1, -100, 100, 0, 100));
        return map;
    }

    private static void Frames(GameEngine engine, int frames, ActionFrame? first = null, ActionFrame? rest = null)
    {
        for (int f = 0; f < frames; f++)
        {
            InputManager.Feed(f == 0 && first != null ? first : rest ?? ActionFrame.None);
            engine.Update(Frame);
        }
        InputManager.Feed(null);
    }

    [Fact]
    public void TheMountIsGotOnAndOffByItsKeyAndRidesAtItsPace()
    {
        Headless.Play(engine =>
        {
            Headless.PastTheOpening(engine);
            var game = engine.Drive;
            engine.RegionMovementOverride = Paldea;
            game.PlaceOpen(Field(), "start");
            Frames(engine, 2);
            var footRow = StateHash.Of(game.Flow);

            // No mount had yet
            Assert.Equal(MountRefusal.NoMount, game.WhyNotMount);
            Frames(engine, 1, ActionFrame.Press(GameAction.Mount));
            Assert.False(game.OpenMounted);
            Assert.Equal(Text.Of("mount.refused.NoMount"), game.Toast.Message);

            game.Story.SetVar(MountRules.MountVariable, MountRules.Koraidon);
            Frames(engine, 1, ActionFrame.Press(GameAction.Mount));
            Assert.True(game.OpenMounted);
            Assert.NotEqual(footRow[1], StateHash.Of(game.Flow)[1]);

            // A second at full tilt east: the trot, 6 m (120 ticks; the frame's first tick has the press of nothing)
            int x0 = game.OpenPlayer!.Pos.X;
            Frames(engine, 60, rest: ActionFrame.Stick(127, 0));
            Assert.InRange(game.OpenPlayer!.Pos.X - x0, 5_900, 6_010);

            // A on nobody: the jump, and in the air the key can't take the player off
            Frames(engine, 1, ActionFrame.Press(GameAction.Confirm));
            Assert.Equal(OpenStance.Falling, game.OpenPlayer!.Body.Stance);
            Frames(engine, 1, ActionFrame.Press(GameAction.Mount));
            Assert.True(game.OpenMounted);
            Assert.Equal(Text.Of("mount.refused.NotOnTheGround"), game.Toast.Message);
            Frames(engine, 90);
            Assert.Equal(OpenStance.Standing, game.OpenPlayer!.Body.Stance);

            // The story's powers are the body's while ridden: the dash makes the run a sprint
            game.Story.Set("FLAG_PALDEA_MOUNT_DASH");
            Frames(engine, 1);
            Assert.Equal(MovementGrants.Dash, game.OpenPlayer!.RideGrants);
            x0 = game.OpenPlayer!.Pos.X;
            var sprint = ActionFrame.Stick(127, 0) with { Down = ActionFrame.Stick(127, 0).Down | (1u << (int)GameAction.Run) };
            Frames(engine, 60, rest: sprint);
            Assert.InRange(game.OpenPlayer!.Pos.X - x0, 10_800, 11_010);

            // Off by the key on the ground
            Frames(engine, 1, ActionFrame.Press(GameAction.Mount));
            Assert.False(game.OpenMounted);
            Assert.Equal(MovementGrants.None, game.OpenPlayer!.RideGrants);
        });
    }

    [Fact]
    public void AFadeOrAScriptTakesThePlayerOffAndASaveWakesRidingOnlyWhereTheMountMayBeRidden()
    {
        Headless.Play(engine =>
        {
            Headless.PastTheOpening(engine);
            var game = engine.Drive;
            engine.RegionMovementOverride = Paldea;
            game.Story.SetVar(MountRules.MountVariable, MountRules.Miraidon);
            game.PlaceOpen(Field(), "start");
            Frames(engine, 2);
            Assert.True(game.ToggleMount());

            // Saved riding: the save says so, and wakes riding in a game where the mount may be ridden
            var save = game.BuildSave();
            Assert.True(save.OpenPlace!.Mounted);
            string written = SaveManager.Write(save);
            Assert.Contains("Mounted", written);

            // A fade over the field: off at its midpoint, not before
            game.StartTransition(GameState.Overworld);
            Assert.True(game.OpenMounted);
            Frames(engine, 120);
            Assert.False(game.OpenMounted);
            // On foot the save says nothing of it
            Assert.DoesNotContain("Mounted", SaveManager.Write(game.BuildSave()));

            // Taken up where the mount may be ridden: riding
            File.WriteAllText(game.SaveFile, written);
            engine.ContinueGame();
            Assert.True(engine.InOpenField);
            Assert.True(game.OpenMounted);
            // and the field opening from black keeps them riding
            Frames(engine, 90);
            Assert.Equal(GameState.Overworld, game.State);
            Assert.True(game.OpenMounted);

            // A script begun in the field takes the player off
            Assert.True(engine.StartScript("NextHint", file: "common"));
            Frames(engine, 1);
            Assert.False(game.OpenMounted);

            // Taken up where the movement lists no mount: on foot
            engine.RegionMovementOverride = null;
            File.WriteAllText(game.SaveFile, written);
            engine.ContinueGame();
            Assert.True(engine.InOpenField);
            Assert.False(game.OpenMounted);
            Assert.Equal(MountRefusal.NotHere, game.WhyNotMount);
        });
    }
}
