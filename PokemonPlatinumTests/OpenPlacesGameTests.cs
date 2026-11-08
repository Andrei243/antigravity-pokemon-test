using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.OpenWorld;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 32 · OW5 in the whole game with no window, on the open test world's ground with its places: a trigger's script
/// runs once as the capsule's centre crosses into it, again only once it has been left, and not while the story isn't
/// at its value; the house's door goes into its grid room and the room's way out lands on the door's marker; a
/// scene's walk to a marker ends on the marker played and skipped alike; a script's warpto from the grid comes to an
/// open map's marker, and a tile warp to an open map goes wrong; a battle begun in an open map comes back to it. It
/// seeds the dice, so it runs with the recordings, alone.
/// </summary>
[Collection("Recordings")]
public class OpenPlacesGameTests
{
    private const float Frame = 1f / 60f;

    private static readonly ScriptLibrary Scripts = ScriptLibrary.FromSources(
        ("lab_cave", """
            script Cavern
              addvar VAR_LAB_CAVE_RUNS 1
              end
            script Scene
              scene "Walk"
              walkto player cavern_east
              faceto player cavern_middle
              camera look cavern_middle
              endscene
              end
            script Fight
              wildbattle "Bidoof" 2
              end
            """),
        ("lab_meadow", """
            script OnEnter
              addvar VAR_LAB_ARRIVALS 1
              end
            """),
        ("common", """
            script ToTheHill
              warpto "OpenLab" hill_foot
              end
            """));

    private static (GameEngine.Driver Game, OpenMap Map) Lab(GameEngine engine)
    {
        Headless.PastTheOpening(engine);
        var game = engine.Drive;
        game.UseScripts(Scripts);
        var map = OpenWorldPlacesTests.LabWithPlaces();
        game.AddOpenMap(map);
        game.AddRoom(OpenWorldPlacesTests.LabHouse());
        return (game, map);
    }

    private static void Run(GameEngine engine, int frames, ActionFrame? frame = null)
    {
        for (int f = 0; f < frames; f++)
        {
            InputManager.Feed(frame ?? ActionFrame.None);
            engine.Update(Frame);
        }
        InputManager.Feed(null);
    }

    private static ActionFrame Stick(sbyte x, sbyte y, bool run = false)
    {
        var f = ActionFrame.Stick(x, y);
        var r = ActionFrame.Press(GameAction.Run);
        return run ? new ActionFrame(f.Down | r.Down, f.Pressed | r.Pressed) { MoveX = x, MoveY = y } : f;
    }

    [Fact]
    public void ATriggersScriptRunsOnceAsTheCentreCrossesIntoItAndOnlyWhileTheStoryWaits()
    {
        Headless.Play(engine =>
        {
            var (game, map) = Lab(engine);
            game.PlaceOpen(map, "tunnel_west");
            // East at a run into the cavern (six seconds: 27 m) and stood in it: once
            Run(engine, 360, Stick(127, 0, run: true));
            Assert.Equal(1, game.Story.Var("VAR_LAB_CAVE_RUNS"));
            Assert.Contains("t:cavern", game.OpenInside);
            Run(engine, 60);
            Assert.Equal(1, game.Story.Var("VAR_LAB_CAVE_RUNS"));
            // Out to the east and back in: once more
            Run(engine, 240, Stick(127, 0, run: true));
            Assert.DoesNotContain("t:cavern", game.OpenInside);
            Run(engine, 240, Stick(-127, 0, run: true));
            Assert.Equal(2, game.Story.Var("VAR_LAB_CAVE_RUNS"));
            // The story elsewhere: crossed, and nothing runs
            game.Story.SetVar("VAR_LAB_CAVE", 1);
            Run(engine, 240, Stick(-127, 0, run: true));
            Assert.DoesNotContain("t:cavern", game.OpenInside);
            Run(engine, 240, Stick(127, 0, run: true));
            Assert.Contains("t:cavern", game.OpenInside);
            Assert.Equal(2, game.Story.Var("VAR_LAB_CAVE_RUNS"));
            Assert.Equal(GameState.Overworld, game.State);
            Assert.True(engine.InOpenField);
        });
    }

    [Fact]
    public void TheHousesDoorGoesIntoItsRoomAndTheRoomsWayOutLandsOnTheDoorsMarker()
    {
        Headless.Play(engine =>
        {
            var (game, map) = Lab(engine);
            game.PlaceOpen(map, "house_path");
            // North up the path into the doorway: the fade, and the room's tile facing up
            for (int f = 0; f < 60 * 10 && engine.InOpenField; f++) Run(engine, 1, Stick(0, -127));
            Run(engine, 120);
            Assert.False(engine.InOpenField);
            Assert.Equal(("OpenLabHouse", 3, 6, Direction.Up), (game.Map.Name, game.Player.GridX, game.Player.GridY, game.Player.Facing));
            Assert.Equal(GameState.Overworld, game.State);

            // Down onto the room's way out: the fade, and the door's marker in the open map, facing as it does
            for (int f = 0; f < 60 * 5 && !engine.InOpenField; f++) Run(engine, 1, new ActionFrame(1u << (int)GameAction.Down, 0));
            Run(engine, 120);
            Assert.True(engine.InOpenField);
            Assert.Same(map, game.OpenMap);
            var step = map.Marker("house_step")!;
            Assert.Equal((step.Pos.X, step.Pos.Z, step.Yaw), (game.OpenPlayer!.Pos.X, game.OpenPlayer.Pos.Z, game.OpenPlayer.Yaw));
            Assert.InRange(game.OpenPlayer.Pos.Y, -200, 400);   // settled on the meadow under the marker
            // The door's volume is behind the player, not stood in: nothing takes them back in while they stand
            Assert.Empty(game.OpenInside);
            Run(engine, 60);
            Assert.True(engine.InOpenField);
            // and the meadow's own arrival script ran as they came out
            Assert.Equal(1, game.Story.Var("VAR_LAB_ARRIVALS"));
        });
    }

    [Fact]
    public void AScenesWalkToAMarkerEndsOnItPlayedAndSkippedAlike()
    {
        var ends = new List<(OpenPos Pos, int Yaw, string? Look)>();
        foreach (bool skip in new[] { false, true })
            Headless.Play(engine =>
            {
                var (game, map) = Lab(engine);
                game.PlaceOpen(map, "cavern_middle");
                game.Flow.Runner.Impatient = skip;
                Assert.True(engine.StartScript("Scene", file: "lab_cave"));
                for (int f = 0; f < 60 * 30 && engine.ScriptRunning; f++) Run(engine, 1);
                Assert.False(engine.ScriptRunning);
                Assert.False(game.OpenWalking);
                Run(engine, 30);
                ends.Add((game.OpenPlayer!.Pos, game.OpenPlayer.Yaw, game.OpenLook));
            });
        Assert.Equal(ends[0], ends[1]);
        var map = OpenWorldPlacesTests.LabWithPlaces();
        var east = map.Marker("cavern_east")!.Pos;
        Assert.Equal((east.X, east.Z), (ends[0].Pos.X, ends[0].Pos.Z));
        // Turned from the marker to the cavern's middle: west and a little north
        Assert.InRange(ends[0].Yaw, OpenYaw.West - 3000, OpenYaw.West - 500);
        Assert.Equal("cavern_middle", ends[0].Look);
    }

    [Fact]
    public void AWarptoFromTheGridComesToAnOpenMapsMarkerAndAWarpThereGoesWrong()
    {
        Headless.Play(engine =>
        {
            var (game, map) = Lab(engine);
            Assert.False(engine.InOpenField);
            Assert.True(engine.StartScript("ToTheHill", file: "common"));
            Run(engine, 120);
            Assert.True(engine.InOpenField);
            var foot = map.Marker("hill_foot")!.Pos;
            Assert.Equal((foot.X, foot.Z), (game.OpenPlayer!.Pos.X, game.OpenPlayer.Pos.Z));
            Assert.Equal(1, game.Story.Var("VAR_LAB_ARRIVALS"));
            // A script's tile warp to an open map is a script gone wrong, never a step onto the map of Sinnoh
            engine.StartScript(ScriptParser.Parse("test", "script W\n warp \"OpenLab\" 3 4\n end\n")[0]);
            Run(engine, 30);
            Assert.True(engine.InOpenField);
            Assert.Contains(engine.ScriptsGoneWrong, m => m.Contains("is an open map, gone to by", StringComparison.Ordinal));
        });
    }

    [Fact]
    public void ABattleBegunInAnOpenMapComesBackToIt()
    {
        Headless.Play(engine =>
        {
            var (game, map) = Lab(engine);
            game.PlaceOpen(map, "cavern_middle");
            Run(engine, 10);
            var stood = game.OpenField!.Place;
            Assert.True(engine.StartScript("Fight", file: "lab_cave"));
            for (int f = 0; f < 60 * 5 && game.State != GameState.Battle; f++) Run(engine, 1);
            Assert.Equal(GameState.Battle, game.State);
            // Won with the lead's first damaging move, its lines read on with the confirm button (as HeadlessGameTests)
            var battle = game.Battle!;
            int hit = game.Party.Members[0].Moves.FindIndex(m => m.Data.Category != PokemonPlatinumEngine.Data.MoveCategory.Status);
            for (int f = 0; f < 60 * 120 && (game.State != GameState.Overworld || engine.ScriptRunning); f++)
            {
                if (!battle.IsBattleOver && battle.HUD.MenuState == PokemonPlatinumEngine.Battle.BattleMenuState.Main) battle.SelectMove(hit);
                Run(engine, 1, f % 8 == 0 ? ActionFrame.Press(GameAction.Confirm) : ActionFrame.None);
            }
            Assert.Equal(GameState.Overworld, game.State);
            Assert.True(engine.InOpenField);
            Assert.Equal(stood, game.OpenField!.Place);
        });
    }
}
