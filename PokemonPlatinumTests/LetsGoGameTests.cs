using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.OpenWorld;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// Let's Go and the raids on Team Star's gates in the whole game with no window (plan 34 · PD5), on Paldea's own bake:
/// the key sends the lead at a wild Pokémon of the field, which is felled with nobody watching and gone from the field
/// while the lead keeps the EXP; a raid begun on Giacomo's gate whose clock runs out ends, and its script puts the
/// player back on the gate with nothing won; and a raid felled to its goal starts the boss's battle on the base's own
/// stage. It seeds the dice, so it runs with the recordings, alone.
/// </summary>
[Collection("Recordings")]
public class LetsGoGameTests
{
    private const float Frame = 1f / 60f;

    private static void Frames(GameEngine engine, int frames, ActionFrame? first = null, ActionFrame? rest = null)
    {
        for (int f = 0; f < frames; f++)
        {
            InputManager.Feed(f == 0 && first != null ? first : rest ?? ActionFrame.None);
            engine.Update(Frame);
        }
        InputManager.Feed(null);
    }

    private static OpenMap Paldea(GameEngine engine) => engine.OpenMapNamed("Paldea") ?? throw new InvalidOperationException("no open map Paldea");

    private static void Team(GameEngine engine)
    {
        var game = engine.Drive;
        game.Party.Clear();
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Pawmot")!, 40));
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Skeledirge")!, 40));
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Garganacl")!, 40));
        game.Story.SetVar("VAR_PALDEA_STORY", 7);
    }

    [Fact]
    public void TheKeySendsTheLeadAtAWildPokemonWhichIsFelledAndTheLeadKeepsTheExp()
    {
        Headless.Play(engine =>
        {
            var game = engine.Drive;
            Team(engine);
            var map = Paldea(engine);
            var zone = map.Zones.First(z => z.Place == "south_province_area_one");
            double cx = zone.Polygon.Average(p => p.X) / 1000.0, cz = zone.Polygon.Average(p => p.Z) / 1000.0;
            OpenPos On(double x, double z) => OpenPos.FromMetres(x, map.Ground.Footing(x, z, 90, 0)!.Value.Y, z);
            game.PlaceOpen(map, On(cx, cz + 3), OpenYaw.North);
            Frames(engine, 2);
            game.OpenWild!.Clear();
            var wild = game.OpenWild.Put(map, zone, zone.Slots[0], 5, On(cx, cz - 2), OpenYaw.South);
            var lead = game.Party.Members[0];
            int exp = lead.CurrentExp;

            // The key: the lead sent at the one five metres ahead
            Frames(engine, 1, ActionFrame.Press(GameAction.LetsGo));
            Assert.NotNull(game.LastLetsGo);
            var fought = game.LastLetsGo!;
            Assert.Equal(LetsGoEnd.Felled, fought.End);
            Assert.True(fought.Exp > 0);
            Assert.Equal(exp + fought.Exp, lead.CurrentExp);
            // Gone from the field, with no battle on the screen
            Assert.DoesNotContain(wild, game.OpenWild.All);
            Assert.Equal(GameState.Overworld, game.State);
            Assert.NotNull(game.LetsGoShowing);
            // The record replays the battle to the same end
            Assert.Equal(fought.Core.Result, fought.File.Replay().Result);

            // The show plays out and the key works again only once it has
            Frames(engine, 1, ActionFrame.Press(GameAction.LetsGo));
            Assert.Same(fought, game.LastLetsGo);
            Frames(engine, 150);
            Assert.Null(game.LetsGoShowing);
        }, region: "Paldea");
    }

    [Fact]
    public void ABasesClockEndsARaidAndItsScriptPutsThePlayerBackOnTheGate()
    {
        Headless.Play(engine =>
        {
            var game = engine.Drive;
            Team(engine);
            game.Story.SetVar("VAR_PALDEA_STARFALL", 1);
            var map = Paldea(engine);
            Assert.Equal("starfall_street", map.AreaAt(map.Marker("giacomo_yard")!.Pos)?.Key);
            game.PlaceOpen(map, "giacomo_yard");
            Frames(engine, 2);
            game.BeginStarRaid("giacomo");
            var raid = game.StarRaid;
            Assert.NotNull(raid);
            Assert.All(raid!.Standing, foe => Assert.NotNull(foe));

            // The clock run down to a second before its end: the raid goes on in the field
            raid.Run(StarRaid.Seconds * OpenClock.TicksPerSecond - OpenClock.TicksPerSecond);
            Frames(engine, 30);
            Assert.Same(raid, game.StarRaid);
            // and over it: the raid ends lost on the gate, its script says so and puts the player back there
            Frames(engine, 60);
            Assert.Null(game.StarRaid);
            Assert.True(raid.Lost);
            for (int i = 0; i < 20 && engine.ScriptRunning; i++) Frames(engine, 8, ActionFrame.Press(GameAction.Confirm));
            Assert.False(engine.ScriptRunning);
            var gate = map.Marker("giacomo_gate")!.Pos;
            Assert.True(game.OpenField!.Player.Pos.GroundDistanceSquared(gate) < 1_000L * 1_000L, $"the player stands at {game.OpenField.Player.Pos}, not the gate {gate}");
            Assert.False(game.Story.Has("FLAG_PALDEA_STAR_GIACOMO_RAIDED"));
            Assert.Equal(1, game.Story.Var("VAR_PALDEA_STARFALL"));
        }, region: "Paldea");
    }

    [Fact]
    public void ARaidFelledToItsGoalBringsTheBossOutAndHisBattleOnTheBasesStage()
    {
        Headless.Play(engine =>
        {
            var game = engine.Drive;
            Team(engine);
            game.Story.SetVar("VAR_PALDEA_STARFALL", 1);
            var map = Paldea(engine);
            game.PlaceOpen(map, "giacomo_gate");
            Frames(engine, 2);
            game.BeginStarRaid("giacomo");
            var raid = game.StarRaid!;
            // A Let's Go in the raid goes at the crew
            Frames(engine, 1, ActionFrame.Press(GameAction.LetsGo));
            Assert.NotNull(game.LastLetsGo);
            Assert.Contains(game.LastLetsGo!.Core.WildPokemon[0].Species.Name, raid.Base.Species);
            while (!raid.Over)
            {
                if (raid.Nearest(raid.Middle) is { } foe) raid.Fell(foe);
                else raid.Tick();
            }
            Frames(engine, 2);
            Assert.Null(game.StarRaid);
            for (int i = 0; i < 40 && game.State != GameState.Battle; i++) Frames(engine, 8, ActionFrame.Press(GameAction.Confirm));
            Assert.True(game.Story.Has("FLAG_PALDEA_STAR_GIACOMO_RAIDED"));
            Assert.Equal(GameState.Battle, game.State);
            Assert.Equal("paldea_star_giacomo", game.Battle!.Trainers[0].Id);
            Assert.Equal("Revavroom", game.Battle.Trainers[0].Party.Members[^1].Species.Name);
        }, region: "Paldea");
    }
}
