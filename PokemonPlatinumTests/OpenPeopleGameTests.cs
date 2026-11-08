using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.OpenWorld;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 32 · OW6 in the whole game with no window, on the open test world's ground with its navigation mesh and its
/// people: a trainer who sees the player calls out, walks up and battles by the grid's own script, the battle comes
/// back to the open map and the trainer, beaten, never looks again; two who see the player at once walk up together
/// and battle side by side; someone is spoken to from in front; a scene walks someone to a marker and turns them,
/// played and skipped alike; whoever travels with the player follows along the trail. It seeds the dice, so it runs
/// with the recordings, alone.
/// </summary>
[Collection("Recordings")]
public class OpenPeopleGameTests
{
    private const float Frame = 1f / 60f;

    private static readonly ScriptLibrary Scripts = ScriptLibrary.FromSources(
        ("lab_meadow", """
            script Gather
              scene "Gather"
              walkto walker twins_gap
              faceto walker player
              faceto player walker
              endscene
              end
            """),
        // The game's own common scripts a trainer and a person run (common.txt's, word for word)
        ("common", """
            script Talk
              sayown
              end
            script Trainer
              if defeated self goto Beaten
              trainerline before
              battle self
              end
            label Beaten
              if rematch self goto Rematch
              trainerline after
              if result == 0 sayown
              end
            label Rematch
              trainerline before
              battle self rematch
              end
            script TrainerPair
              trainerline before
              trainerline before pair
              battle self and pair with partner
              end
            """));

    private static (GameEngine.Driver Game, OpenMap Map) Lab(GameEngine engine, bool two = false)
    {
        Headless.PastTheOpening(engine);
        var game = engine.Drive;
        game.UseScripts(Scripts);
        if (two) game.Party.Add(new Pokemon(PokemonDatabase.Get("Starly")!, 30));
        var map = OpenWorldPeopleTests.LabWithPeople();
        game.AddOpenMap(map);
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

    /// <summary>A battle won with each lead's first damaging move, its lines read on with the confirm button, until the field is the player's again.</summary>
    private static void WinTheBattle(GameEngine engine)
    {
        var game = engine.Drive;
        var battle = game.Battle!;
        for (int f = 0; f < 60 * 240 && (game.State != GameState.Overworld || engine.ScriptRunning); f++)
        {
            if (game.State == GameState.Battle && !battle.IsBattleOver && battle.HUD.MenuState == BattleMenuState.Main)
            {
                var lead = battle.PlayerPokemon;
                foreach (var p in game.Party.Members) { p.CurrentHP = p.MaxHP; }
                battle.SelectMove(lead.Moves.FindIndex(m => m.Data.Category != MoveCategory.Status));
            }
            Run(engine, 1, f % 8 == 0 ? ActionFrame.Press(GameAction.Confirm) : ActionFrame.None);
        }
    }

    [Fact]
    public void ATrainerWhoSeesThePlayerWalksUpBattlesAndIsBeatenForGood()
    {
        Headless.Play(engine =>
        {
            var (game, map) = Lab(engine);
            // East along the north meadow toward the camper, who faces east: seen once in front of them
            game.PlaceOpen(map, "north_meadow");
            var camper = map.Person("camper_west")!;
            for (int f = 0; f < 60 * 20 && game.OpenApproach == null; f++) Run(engine, 1, ActionFrame.Stick(127, 0));
            Assert.NotNull(game.OpenApproach);
            Assert.Same(camper, game.OpenApproach!.Trainer);
            Assert.Null(game.OpenPairApproach);   // one Pokémon able to fight: one trainer at a time
            var seen = game.OpenPlayer!.Pos;
            // The "!", the walk up, and the challenge by the grid's script: the battle
            for (int f = 0; f < 60 * 30 && game.State != GameState.Battle; f++) Run(engine, 1, ActionFrame.Stick(127, 0));
            Assert.Equal(GameState.Battle, game.State);
            Assert.True(game.Battle!.IsTrainerBattle);
            // The player stood still while the trainer came, and the trainer stands a talking distance off, facing them
            Assert.Equal((seen.X, seen.Z), (game.OpenPlayer.Pos.X, game.OpenPlayer.Pos.Z));
            Assert.InRange(Math.Sqrt(camper.Pos.GroundDistanceSquared(seen)), 700, OpenApproach.TalkDistance + 20);
            WinTheBattle(engine);
            Run(engine, 2);
            Assert.True(engine.InOpenField);
            Assert.Equal((seen.X, seen.Z), (game.OpenPlayer.Pos.X, game.OpenPlayer.Pos.Z));
            Assert.True(game.Story.HasDefeated("camper_jacob"));
            Assert.True(game.OpenNpc(camper).HasBattled);
            Assert.False(camper.Held);
            // Beaten, they never call out again: walked about in front of them, nothing
            for (int f = 0; f < 60 * 3; f++) Run(engine, 1, ActionFrame.Stick(-127, 0));
            for (int f = 0; f < 60 * 3; f++) Run(engine, 1, ActionFrame.Stick(127, 0));
            Assert.Null(game.OpenApproach);
            Assert.Equal(GameState.Overworld, game.State);
        });
    }

    [Fact]
    public void TwoWhoSeeThePlayerAtOnceWalkUpTogetherAndBattleSideBySide()
    {
        Headless.Play(engine =>
        {
            var (game, map) = Lab(engine, two: true);
            game.PlaceOpen(map, "twins_gap");
            Run(engine, 2);
            Assert.NotNull(game.OpenApproach);
            Assert.NotNull(game.OpenPairApproach);
            var (first, second) = (game.OpenApproach!.Trainer, game.OpenPairApproach!.Trainer);
            Assert.Equal(("camper_west", "picnicker_east"), (first.Key, second.Key));
            for (int f = 0; f < 60 * 30 && game.State != GameState.Battle; f++) Run(engine, 1);
            Assert.Equal(GameState.Battle, game.State);
            Assert.True(game.Battle!.IsDouble);
            var at = game.OpenPlayer!.Pos;
            Assert.All(new[] { first, second }, t => Assert.InRange(Math.Sqrt(t.Pos.GroundDistanceSquared(at)), 700, OpenApproach.TalkDistance + 20));
            WinTheBattle(engine);
            Run(engine, 2);
            Assert.True(engine.InOpenField);
            Assert.True(game.Story.HasDefeated("camper_jacob") && game.Story.HasDefeated("picnicker_siena"));
        });
    }

    [Fact]
    public void SomeoneIsSpokenToFromInFront()
    {
        Headless.Play(engine =>
        {
            var (game, map) = Lab(engine);
            var walker = map.Person("walker")!;
            // Put south of them, facing north at them
            game.PlaceOpen(map, walker.Home.Plus(0, 0, 1200), OpenYaw.North);
            Run(engine, 2);
            Run(engine, 1, ActionFrame.Press(GameAction.Confirm));
            Run(engine, 2);
            Assert.Equal(GameState.Dialogue, game.State);
            Assert.Contains("Fine weather", game.Dialogue.CurrentLine);
            // They turned to the player
            Assert.InRange(Math.Abs(OpenYaw.Between(walker.Yaw, OpenYaw.South)), 0, 300);
            for (int f = 0; f < 60 * 5 && (engine.ScriptRunning || game.State != GameState.Overworld); f++)
                Run(engine, 1, f % 6 == 0 ? ActionFrame.Press(GameAction.Confirm) : ActionFrame.None);
            Run(engine, 2);
            Assert.False(walker.Held);
            // Facing away, nobody is spoken to
            game.PlaceOpen(map, walker.Home.Plus(0, 0, 1200), OpenYaw.South);
            Run(engine, 2);
            Run(engine, 1, ActionFrame.Press(GameAction.Confirm));
            Run(engine, 2);
            Assert.Equal(GameState.Overworld, game.State);
            Assert.False(engine.ScriptRunning);
        });
    }

    [Fact]
    public void AScenesWalkOfSomeoneToAMarkerEndsOnItPlayedAndSkippedAlike()
    {
        var ends = new List<(OpenPos Walker, int WalkerYaw, int PlayerYaw)>();
        foreach (bool skip in new[] { false, true })
            Headless.Play(engine =>
            {
                var (game, map) = Lab(engine);
                // Far from the trainers' cones: west of the walker, on the meadow
                game.PlaceOpen(map, map.Marker("north_meadow")!.Pos.Plus(-3000, 0, 0), OpenYaw.East);
                Run(engine, 2);
                game.Flow.Runner.Impatient = skip;
                Assert.True(engine.StartScript("Gather", file: "lab_meadow"));
                for (int f = 0; f < 60 * 40 && engine.ScriptRunning; f++) Run(engine, 1);
                Assert.False(engine.ScriptRunning);
                var walker = map.Person("walker")!;
                Assert.Null(walker.Walk);
                ends.Add((walker.Pos, walker.Yaw, game.OpenPlayer!.Yaw));
            });
        Assert.Equal(ends[0], ends[1]);
        var gap = OpenWorldPeopleTests.LabWithPeople().Marker("twins_gap")!.Pos;
        Assert.Equal((gap.X, gap.Z), (ends[0].Walker.X, ends[0].Walker.Z));
        // Each turned to the other: the walker west, the player east
        Assert.InRange(Math.Abs(OpenYaw.Between(ends[0].WalkerYaw, OpenYaw.West)), 0, 2000);
        Assert.InRange(Math.Abs(OpenYaw.Between(ends[0].PlayerYaw, OpenYaw.East)), 0, 2000);
    }

    [Fact]
    public void WhoeverTravelsWithThePlayerFollowsAlongTheTrail()
    {
        Headless.Play(engine =>
        {
            var (game, map) = Lab(engine);
            var lead = game.Party.Members[0];
            game.Flow.Along = new Follower(new NPC { Key = "lead" }, lead);
            game.PlaceOpen(map, "west_road");
            var follower = game.OpenFollower;
            Assert.NotNull(follower);
            Assert.Equal(lead.LookName, follower!.Who);
            Assert.Equal(game.OpenPlayer!.Pos, follower.Pos);
            // East along the road for three seconds: the follower keeps its gap behind on the trail
            Run(engine, 180, ActionFrame.Stick(127, 0));
            Run(engine, 60);
            double gap = Math.Sqrt(follower.Pos.DistanceSquared(game.OpenPlayer.Pos));
            Assert.InRange(gap, OpenFollower.Gap - 80, OpenFollower.Gap + 5);
            Assert.True(follower.Pos.X < game.OpenPlayer.Pos.X);
        });
    }
}
