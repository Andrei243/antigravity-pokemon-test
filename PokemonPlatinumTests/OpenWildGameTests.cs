using System;
using System.Linq;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.OpenWorld;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 32 · OW7 in the whole game with no window, on the open test world's ground with its zones: a wild Pokémon the
/// player walks into is met at once, as a step in the grass would make it, at the level it came out with, and from
/// behind it is a back strike; the battle comes back to the open map with the Pokémon gone from it and its zone
/// waiting; a battle begun on sand is fought on the beach's stage; nothing is met while a script has the field. It
/// seeds the dice, so it runs with the recordings, alone.
/// </summary>
[Collection("Recordings")]
public class OpenWildGameTests
{
    private const float Frame = 1f / 60f;

    private static readonly ScriptLibrary Scripts = ScriptLibrary.FromSources(
        ("lab_meadow", """
            script Stand
              wait 3
              end
            """));

    private static (GameEngine.Driver Game, OpenMap Map) Lab(GameEngine engine)
    {
        Headless.PastTheOpening(engine);
        var game = engine.Drive;
        game.UseScripts(Scripts);
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

    private static OpenPos On(OpenMap map, double x, double z) =>
        OpenPos.FromMetres(x, map.Ground.Footing(x, z, 20, -20, walkableOnly: true)!.Value.Y, z);

    /// <summary>The south meadow's zone kept quiet (each as if one had just been met there), so only the one put down stands near.</summary>
    private static void Quiet(GameEngine.Driver game, OpenMap map)
    {
        foreach (var zone in map.Zones) game.OpenWild!.Ended(zone, OpenWildEnd.CaughtOrFelled);
        // ...and its grace let pass
        for (int i = 0; i < OpenWildField.Grace; i++) game.OpenWild!.Step(game.OpenField!, false);
    }

    [Fact]
    public void AWildPokemonThatNoticesThePlayerCriesOutOnceWithTheExclaimOverIt()
    {
        Headless.Play(engine =>
        {
            var (game, map) = Lab(engine);
            var zone = map.Zones.Single(z => z.Name == "south_meadow");
            game.PlaceOpen(map, On(map, 24, 22), OpenYaw.North);
            Run(engine, 2);
            Quiet(game, map);
            // A curious Wooloo ten metres south, facing the player
            var wooloo = game.OpenWild!.Put(map, zone, new OpenSlot("Wooloo", 5, 5, 1), 5, On(map, 24, 31.5), OpenYaw.North);
            var heard = AudioManager.Listen();
            try
            {
                bool exclaimed = false;
                for (int f = 0; f < 60 * 2; f++)
                {
                    Run(engine, 1);
                    exclaimed |= engine.GatherOpenFigures(game.OpenField!).Any(x => x.Key == "wild:" + wooloo.Id && x.Exclaiming);
                }
                Assert.True(exclaimed, "no \"!\" stood over the Wooloo");
                Assert.Single(heard, h => h == "cry Wooloo");
                Assert.Equal(GameState.Overworld, game.State);
            }
            finally
            {
                AudioManager.StopListening();
            }
        });
    }

    [Fact]
    public void AWildPokemonWalkedIntoIsMetAsTheGrassWouldMeetItAndIsGoneAfter()
    {
        Headless.Play(engine =>
        {
            var (game, map) = Lab(engine);
            var zone = map.Zones.Single(z => z.Name == "south_meadow");
            game.PlaceOpen(map, On(map, 14, 22), OpenYaw.East);
            Run(engine, 2);
            Quiet(game, map);
            // A Bidoof two metres east, facing east too: walked into from behind
            var bidoof = game.OpenWild!.Put(map, zone, zone.Slots.Single(s => s.Species == "Bidoof"), 4, On(map, 16, 22), OpenYaw.East);
            for (int f = 0; f < 60 * 3 && game.State != GameState.Battle; f++) Run(engine, 1, ActionFrame.Stick(127, 0));
            Assert.Equal(GameState.Battle, game.State);
            var stood = game.OpenPlayer!.Pos;
            var battle = game.Battle!;
            Assert.False(battle.IsTrainerBattle);
            Assert.Equal("Bidoof", battle.EnemyPokemon!.Species.Name);
            // At the level it came out with (Sinnoh's own: nothing to map for a game begun there)
            Assert.Equal(4, battle.EnemyPokemon.Level);
            Assert.True(battle.Core.Conditions.Surprised);
            Assert.Equal(BattleTerrain.Plain, battle.Core.Conditions.Terrain);
            Assert.Equal(BattleArena.Grass, game.BattleRenderer.Arena.Kind);
            Assert.DoesNotContain(bidoof, game.OpenWild!.All);
            Assert.Same(zone, game.OpenWildMet);
            // Won, back in the open map where it was met, the zone waiting the longer and nothing met in the grace
            int hit = game.Party.Members[0].Moves.FindIndex(m => m.Data.Category != MoveCategory.Status);
            for (int f = 0; f < 60 * 120 && (game.State != GameState.Overworld || engine.ScriptRunning); f++)
            {
                if (!battle.IsBattleOver && battle.HUD.MenuState == BattleMenuState.Main)
                {
                    foreach (var p in game.Party.Members) p.CurrentHP = p.MaxHP;
                    battle.SelectMove(hit);
                }
                Run(engine, 1, f % 8 == 0 ? ActionFrame.Press(GameAction.Confirm) : ActionFrame.None);
            }
            Assert.Equal(GameState.Overworld, game.State);
            Assert.True(engine.InOpenField);
            Assert.Null(game.OpenWildMet);
            Assert.True(game.OpenWild!.WaitOf(zone) > OpenWildField.RespawnAfterFlight);
            Assert.True(game.OpenWild.GraceLeft > 0);
            Assert.Equal((stood.X, stood.Z), (game.OpenPlayer!.Pos.X, game.OpenPlayer.Pos.Z));
        });
    }

    [Fact]
    public void ABattleBegunOnSandIsFoughtOnTheBeachsStage()
    {
        Headless.Play(engine =>
        {
            var (game, map) = Lab(engine);
            var zone = map.Zones.Single(z => z.Name == "south_meadow");
            var sand = OpenWorldWildTests.SandOf(map) ?? throw new InvalidOperationException("the lab has no sand on its river's shore");
            game.PlaceOpen(map, sand, OpenYaw.North);
            Run(engine, 2);
            Quiet(game, map);
            // One put right at the player's feet, facing them: met at the next tick, and no back strike
            game.OpenWild!.Put(map, zone, zone.Slots.Single(s => s.Species == "Bidoof"), 3, game.OpenPlayer!.Pos, OpenYaw.South);
            for (int f = 0; f < 60 * 5 && game.State != GameState.Battle; f++) Run(engine, 1);
            Assert.Equal(GameState.Battle, game.State);
            Assert.Equal(BattleTerrain.Sand, game.Battle!.Core.Conditions.Terrain);
            Assert.Equal(BattleArena.Sand, game.BattleRenderer.Arena.Kind);
            Assert.False(game.Battle.Core.Conditions.Surprised);
        });
    }

    [Fact]
    public void NothingIsMetWhileAScriptHasTheField()
    {
        Headless.Play(engine =>
        {
            var (game, map) = Lab(engine);
            var zone = map.Zones.Single(z => z.Name == "south_meadow");
            game.PlaceOpen(map, On(map, 24, 22), OpenYaw.East);
            Run(engine, 2);
            Quiet(game, map);
            Assert.True(engine.StartScript("Stand", file: "lab_meadow"));
            game.OpenWild!.Put(map, zone, zone.Slots.Single(s => s.Species == "Bidoof"), 3, game.OpenPlayer!.Pos, OpenYaw.South);
            Run(engine, 60);
            Assert.NotEqual(GameState.Battle, game.State);
            Assert.Single(game.OpenWild.All);
            // Once it has ended, the one at the player's feet is met
            for (int f = 0; f < 60 * 5 && game.State != GameState.Battle; f++) Run(engine, 1);
            Assert.Equal(GameState.Battle, game.State);
        });
    }

    [Fact]
    public void ZonesBringTheirPokemonOutAroundThePlayerInTheGame()
    {
        Headless.Play(engine =>
        {
            var (game, map) = Lab(engine);
            // North of the meadow facing north: the zone behind, out of view
            game.PlaceOpen(map, On(map, 24, 6), OpenYaw.North);
            Run(engine, 60 * 10);
            Assert.Equal(GameState.Overworld, game.State);
            Assert.NotEmpty(game.OpenWild!.All);
            Assert.All(game.OpenWild.All, w => Assert.Contains(w.Zone.Name, new[] { "south_meadow", "hill_top" }));
        });
    }
}
