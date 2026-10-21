using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Xunit;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.UI;
using static PokemonPlatinumTests.CoreScenario;
using static PokemonPlatinumTests.Scenario;

namespace PokemonPlatinumTests;

/// <summary>
/// The options of plan 08 · P2: the speed-up, auto-advance, autorun and the text frame. All off, or plain, unless
/// chosen, so the game draws and plays what it did.
/// </summary>
[Collection("Shared clocks and dice")]
public class QuickerBattleTests
{
    [Fact]
    public void TheDefaultsChangeNothing()
    {
        var s = new GameSettings();
        Assert.Equal((SpeedUp.Off, AutoAdvance.Off, false, TextFrame.Plain), (s.SpeedUp, s.AutoAdvance, s.Autorun, s.Frame));
        Assert.Equal(1f, GameSettings.SpeedFactor(SpeedUp.Off));
        Assert.Null(GameSettings.AutoAdvanceAfter(AutoAdvance.Off));
    }

    [Fact]
    public void TheFactorsAndPausesGrow()
    {
        Assert.Equal(new[] { 1f, 2f, 3f }, new[] { SpeedUp.Off, SpeedUp.Double, SpeedUp.Triple }.Select(GameSettings.SpeedFactor));
        Assert.True(GameSettings.AutoAdvanceAfter(AutoAdvance.Slow) > GameSettings.AutoAdvanceAfter(AutoAdvance.Fast));
    }

    [Fact]
    public void TheRowsGoRoundTheirValues()
    {
        var s = new GameSettings();
        foreach (var (row, count) in new[] { (OptionRow.SpeedUp, 3), (OptionRow.AutoAdvance, 3), (OptionRow.Autorun, 2), (OptionRow.Frame, 6) })
        {
            var seen = new HashSet<string>();
            for (int i = 0; i < count; i++)
            {
                seen.Add($"{s.SpeedUp}/{s.AutoAdvance}/{s.Autorun}/{s.Frame}");
                OptionsScreen.Change(s, row, 1);
            }
            // Back where it began after one lap, and a step back undoes a step on
            Assert.Equal(count, seen.Count);
            OptionsScreen.Change(s, row, 1);
            OptionsScreen.Change(s, row, -1);
        }
        Assert.Equal((SpeedUp.Off, AutoAdvance.Off, false, TextFrame.Plain), (s.SpeedUp, s.AutoAdvance, s.Autorun, s.Frame));
    }

    [Fact]
    public void TheFrameAndTheOtherRowsRoundTripThroughTheSettingsFile()
    {
        var s = new GameSettings { SpeedUp = SpeedUp.Triple, AutoAdvance = AutoAdvance.Fast, Autorun = true, Frame = TextFrame.Ribbon };
        string path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"quicker-{Guid.NewGuid():N}.json");
        try
        {
            s.Save(path);
            var back = GameSettings.Load(path);
            Assert.Equal((SpeedUp.Triple, AutoAdvance.Fast, true, TextFrame.Ribbon), (back.SpeedUp, back.AutoAdvance, back.Autorun, back.Frame));
        }
        finally { System.IO.File.Delete(path); }

        // A file from before the rows reads as the same
        var json = new JsonSerializerOptions { Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() } };
        var old = JsonSerializer.Deserialize<GameSettings>("{ \"VSync\": false }", json)!;
        Assert.Equal((SpeedUp.Off, AutoAdvance.Off, false, TextFrame.Plain), (old.SpeedUp, old.AutoAdvance, old.Autorun, old.Frame));
    }

    // ------------------------------------------------------------------ Autorun

    private static bool RunsAfterAStep(bool autorun, bool runHeld, bool shoes)
    {
        var map = Fixtures.Map("Route201");
        int x = 0, y = 0;
        bool found = false;
        for (int j = 1; j < map.Height - 1 && !found; j++)
            for (int i = 1; i < map.Width - 2 && !found; i++)
                if (map.IsWalkable(i, j) && map.IsWalkable(i + 1, j)) { x = i; y = j; found = true; }
        Assert.True(found);
        var player = new Player(x, y) { Autorun = autorun, HasRunningShoes = shoes };
        player.Facing = Direction.Right;
        try
        {
            var frame = runHeld ? ActionFrame.None.Next(GameAction.Right, GameAction.Run) : ActionFrame.None.Next(GameAction.Right);
            InputManager.Feed(frame);
            for (int f = 0; f < 3; f++) player.Update(1f / 60f, map, _ => { }, _ => { });
            return player.IsRunning;
        }
        finally { InputManager.Feed(null); }
    }

    [Fact]
    public void AutorunRunsWithRunUpAndWalksWithRunHeld()
    {
        Assert.False(RunsAfterAStep(autorun: false, runHeld: false, shoes: true));
        Assert.True(RunsAfterAStep(autorun: false, runHeld: true, shoes: true));
        Assert.True(RunsAfterAStep(autorun: true, runHeld: false, shoes: true));
        Assert.False(RunsAfterAStep(autorun: true, runHeld: true, shoes: true));
    }

    [Fact]
    public void AutorunNeedsTheRunningShoesToo()
    {
        Assert.False(RunsAfterAStep(autorun: true, runHeld: false, shoes: false));
    }

    // ------------------------------------------------------------------ Pace and auto-advance in a battle

    private static BattleEngine Sparring()
    {
        var party = new Party();
        party.Add(Of(Gender.Male, "Garchomp", 50, "Dragon Claw"));
        var battle = new BattleEngine(new BattleSetup
        {
            PlayerParty = party, Inventory = new Inventory(), Pokedex = new Pokedex(),
            WildPokemon = new List<Pokemon> { Of(Gender.Male, "Snorlax", 50) }, Random = Calm(), Rules = Ruleset.Platinum, Scene = true
        });
        Settle(battle);
        return battle;
    }

    /// <summary>Plays the first move out by itself, a frame of <paramref name="dt"/> at a time, pausing as the options say.</summary>
    private static (int Seconds, int LogLength, int Hp) PlayOut(float dt, float? pause)
    {
        var battle = Sparring();
        battle.AutoAdvanceAfter = pause;
        battle.SelectMove(0);
        float time = 0;
        for (int i = 0; i < 20000 && battle.HUD.MenuState != BattleMenuState.Main; i++)
        {
            battle.Update(dt);
            time += dt;
        }
        return ((int)time, battle.Core.Log.Count, battle.EnemyPokemon.CurrentHP);
    }

    [Fact]
    public void AutoAdvanceReadsAMoveOnWithoutAPress()
    {
        var battle = Sparring();
        battle.SelectMove(0);
        // Without it the first line waits for ever
        for (int i = 0; i < 300; i++) battle.Update(0.1f);
        Assert.NotEqual(BattleMenuState.Main, battle.HUD.MenuState);

        var played = PlayOut(0.05f, 1.0f);
        Assert.True(played.Seconds < 900);
    }

    [Fact]
    public void ASpedBattleReachesTheSameLogAsASlowOne()
    {
        // Three times the pace is three times the dt: presentation is a function of time, and the rules are not
        var slow = PlayOut(0.05f, 1.0f);
        var fast = PlayOut(0.15f, 1.0f);
        Assert.Equal(slow.LogLength, fast.LogLength);
        Assert.Equal(slow.Hp, fast.Hp);
        Assert.True(fast.Seconds <= slow.Seconds);
    }

    [Fact]
    public void AutoAdvanceNeverAnswersAQuestionInTheField()
    {
        var dialogue = new DialogueManager { AutoAdvanceAfter = 0.5f };
        dialogue.ShowDialogue("", new[] { "One.", "Two." });
        dialogue.FinishLine();
        dialogue.Update(1f);
        Assert.Equal("Two.", dialogue.CurrentLine);

        dialogue.ShowQuestion("", "Which?");
        dialogue.FinishLine();
        for (int i = 0; i < 20; i++) dialogue.Update(1f);
        Assert.True(dialogue.AwaitingAnswer);
    }
}
