using System;
using System.Linq;
using PokemonPlatinumEngine;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// Locked hearts in a game with no window (plan 44 · SD3's "done when"): a Sinnoh agent of Lockwork's battled through the
/// game's own trainer battle, the locked Pokémon reclaimed with the Reclaimer and taken into the team as the battle ends,
/// or knocked out without it and noted as missed for the courier.
/// </summary>
[Collection("Shared clocks and dice")]
public class HeartsGameTests
{
    private static readonly float Frame = 1f / 60f;

    private static GameEngine Headless()
    {
        var engine = new GameEngine(new GameSettings { Muted = true, BattleStyle = BattleStyle.Set });
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

    private static (GameEngine Engine, GameEngine.Driver Game, Pokemon Lead) Begin(bool reclaimer)
    {
        Dice.Seed(44);
        var engine = Headless();
        var game = engine.Drive;
        engine.NewGameRegion = "Sinnoh";
        engine.StartNewGame();
        game.Story.Set("SinnohHallOfFame");
        game.Party.Clear();
        var lead = new Pokemon(PokemonDatabase.Get("Luxray")!, 90);
        game.Party.Add(lead);
        game.Bag.AddItem(ItemDatabase.Get("Poké Ball")!, 60);
        if (reclaimer) game.Bag.AddItem(ItemDatabase.Get(HeartLock.ReclaimerItem)!, 1);
        game.State = GameState.Overworld;
        game.Place(MapDatabase.Get("Sinnoh"), 626, 343, Direction.Up);
        return (engine, game, lead);
    }

    private static void Fight(GameEngine engine, GameEngine.Driver game, Pokemon lead, bool throwBalls)
    {
        var agent = game.Map.Everyone.Single(n => n.Id == "lockwork_agent_1");
        var team = new Trainer { Id = "lockwork_sinnoh_agent_1" };
        TrainerDatabase.Fill(team, TrainerDatabase.Get(team.Id)!);
        agent.TrainerData = team;
        game.StartTrainerBattle(agent);
        Until(engine, () => game.State == GameState.Battle, "the battle");
        var battle = game.Battle!;
        int hit = lead.Moves.FindIndex(m => m.Data.Category != MoveCategory.Status);
        for (int guard = 0; guard < 60 * 300 && !battle.IsBattleOver; guard++)
        {
            if (battle.HUD.MenuState == BattleMenuState.Main)
            {
                if (throwBalls && battle.EnemyPokemon.Lock != null && battle.Core.WhyNot(PokemonPlatinumEngine.Battle.Sim.BattleChoice.UseItem(new PokemonPlatinumEngine.Battle.Sim.Place(BattleSide.Player, 0), "Poké Ball")) == null)
                    battle.UseBagItem("Poké Ball");
                else battle.SelectMove(hit);
            }
            Run(engine, guard % 8 == 0 ? ActionFrame.Press(GameAction.Confirm) : ActionFrame.None);
        }
        Assert.True(battle.IsBattleOver, "The battle with Lockwork's agent never ended.");
        Until(engine, () => game.State == GameState.Overworld, "the field after the battle", press: true);
    }

    [Fact]
    public void AnAgentsLockedPokemonIsReclaimedIntoTheTeam()
    {
        var paths = SavePaths.Current;
        try
        {
            var (engine, game, lead) = Begin(reclaimer: true);
            Fight(engine, game, lead, throwBalls: true);
            var stunky = game.Party.Members.Single(p => p.Species.Name == "Stunky");
            Assert.Equal("lock_sinnoh_stunky", stunky.Lock!.Id);
            Assert.Contains("lock_sinnoh_stunky", game.Hearts.Reclaimed);
            Assert.Empty(game.Hearts.Missed);
            Assert.NotNull(stunky.Id);
            Assert.True(game.Pokedex.IsCaught(stunky.Species.DexNumber));
        }
        finally
        {
            InputManager.Feed(null);
            Dice.Seed(null);
            Ruleset.Use(RulesPreset.Platinum);
            SavePaths.Current = paths;
        }
    }

    [Fact]
    public void WithoutTheReclaimerALockedPokemonKnockedOutIsMissed()
    {
        var paths = SavePaths.Current;
        try
        {
            var (engine, game, lead) = Begin(reclaimer: false);
            Fight(engine, game, lead, throwBalls: false);
            Assert.DoesNotContain(game.Party.Members, p => p.Species.Name == "Stunky");
            Assert.Contains("lock_sinnoh_stunky", game.Hearts.Missed);
            Assert.Empty(game.Hearts.Reclaimed);
            // The courier carries it
            Assert.Equal("Stunky", Assert.Single(HeartLock.CourierTeam(game.Hearts.Missed)).Species.Name);
        }
        finally
        {
            InputManager.Feed(null);
            Dice.Seed(null);
            Ruleset.Use(RulesPreset.Platinum);
            SavePaths.Current = paths;
        }
    }
}
