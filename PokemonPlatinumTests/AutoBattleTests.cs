using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI;
using Xunit;
using static PokemonPlatinumTests.Scenario;

namespace PokemonPlatinumTests;

/// <summary>
/// Auto-battle (plan 12 · Q12; style guide, "Battle panels", "AUTO"): the commands' fifth button plays a battle to its
/// end with choices that draw nothing, so its record replays the same; it stops on a faint on the player's side and
/// never throws a ball; the options' row offers it, off unless chosen.
/// </summary>
public class AutoBattleTests
{
    private static BattleEngine Wild(Party party, Pokemon foe, uint seed, Inventory? bag = null)
    {
        var battle = new BattleEngine(new BattleSetup
        {
            PlayerParty = party, Inventory = bag ?? new Inventory(), Pokedex = new Pokedex(), WildPokemon = { foe },
            Random = new BattleRandom(seed), Rules = Ruleset.Platinum
        });
        battle.AutoOn = true;
        Settle(battle);
        return battle;
    }

    /// <summary>Runs the battle's frames as the game does, a thirtieth of a second each, until it is over or AUTO has stopped.</summary>
    private static int Run(BattleEngine battle, int frames = 20000)
    {
        int f = 0;
        for (; f < frames && !battle.IsBattleOver && battle.AutoRunning; f++) battle.Update(1f / 30f);
        return f;
    }

    [Fact]
    public void AnAutoBattleEndsAndItsRecordReplaysTheSame()
    {
        static (Party Mine, Pokemon Foe) Teams()
        {
            var party = new Party();
            party.Add(Mon("Luxray", 50, "Growl", "Spark", "Bite", "Leer"));
            return (party, Mon("Bidoof", 12, "Tackle", "Growl"));
        }
        var (party, foe) = Teams();
        var bag = new Inventory();
        bag.AddItem(ItemDatabase.Get("Poké Ball")!, 10);
        bag.AddItem(ItemDatabase.Get("Potion")!, 3);
        var battle = Wild(party, foe, 20261031, bag);

        Assert.True(battle.AutoOffered);
        battle.AutoUntilStopped();
        Assert.True(battle.AutoRunning);
        Run(battle);
        // The win's lines are read on by AUTO too
        for (int f = 0; f < 2000 && !battle.IsBattleOver; f++) battle.Update(1f / 30f);
        Assert.True(battle.IsBattleOver);
        Assert.Equal(BattleResult.PlayerVictory, battle.Result);

        // Every answer was a move (the strongest, Spark or Bite against a Normal type, never a status move), so no
        // ball was thrown and nothing came out of the bag
        var answered = battle.Core.Record!.Answers.SelectMany(a => a).ToList();
        Assert.NotEmpty(answered);
        Assert.All(answered, c => Assert.Equal(ChoiceKind.Fight, c.Kind));
        Assert.All(answered, c => Assert.Contains(c.Move, new[] { 1, 2 }));
        Assert.Equal(10, bag.GetQuantity(ItemDatabase.Get("Poké Ball")!));

        // The same battle with no screen at all, from the record alone
        var (mine, wild) = Teams();
        var again = new BattleCore(new CoreSetup
        {
            PlayerParty = mine, WildPokemon = new List<Pokemon> { wild }, Random = new BattleRandom(battle.Core.Record!.Seed),
            Rules = Ruleset.Platinum, PlayerName = PlayerIdentity.Name
        });
        again.Replay(battle.Core.Record!);
        Assert.Equal(Golden.BattleText.Written(battle.Core), Golden.BattleText.Written(again));
        Assert.Equal(battle.Result, again.Result);
    }

    [Fact]
    public void AutoStopsOnAFaintOnThePlayersSide()
    {
        var party = new Party();
        party.Add(Mon("Bidoof", 3, "Tackle"));
        party.Add(Mon("Luxray", 50, "Spark"));
        var battle = Wild(party, Mon("Metagross", 60, "Meteor Mash"), 7);

        battle.AutoUntilStopped();
        Run(battle);
        Assert.False(battle.AutoRunning);
        Assert.Equal("faint", battle.AutoStoppedFor);
        Assert.False(battle.IsBattleOver);
        Assert.True(party.Members[0].IsFainted);

        // The lines after the faint wait for the player, and the replacement is theirs to choose
        Settle(battle);
        Assert.True(battle.IsChoosingReplacement);
    }

    [Fact]
    public void WhatAutoChoosesDrawsNothing()
    {
        var party = new Party();
        party.Add(Mon("Luxray", 50, "Growl", "Spark"));
        var battle = Wild(party, Mon("Bidoof", 12, "Tackle"), 99);
        uint before = ((BattleRandom)battle.Core.Random).State;
        var command = battle.AutoCommandNow();
        Assert.Equal(new AutoCommand(1, null), command);
        Assert.Equal(before, ((BattleRandom)battle.Core.Random).State);
    }

    [Fact]
    public void AutoTurnPlaysOneTurnAndLeavesTheNextToThePlayer()
    {
        var party = new Party();
        party.Add(Mon("Luxray", 50, "Spark"));
        var battle = Wild(party, Mon("Snorlax", 60, "Rest"), 3);
        Assert.True(battle.AutoTurn());
        Assert.False(battle.AutoRunning);
        Settle(battle);
        Assert.Equal(BattleMenuState.Main, battle.HUD.MenuState);
        Assert.Single(battle.Core.Record!.Answers);
    }

    [Fact]
    public void BackStopsAuto()
    {
        var party = new Party();
        party.Add(Mon("Luxray", 50, "Spark"));
        var battle = Wild(party, Mon("Snorlax", 60, "Rest"), 3);
        battle.AutoUntilStopped();
        battle.StopAuto("back");
        Assert.False(battle.AutoRunning);
        Assert.Equal("back", battle.AutoStoppedFor);
    }

    [Fact]
    public void TheCursorReachesAutoFromFightOnlyWhereItIsOffered()
    {
        var party = new Party();
        party.Add(Mon("Luxray", 50, "Spark"));
        var battle = Wild(party, Mon("Bidoof", 12, "Tackle"), 3);

        battle.HUD.MainMenuIndex = 0;
        battle.MoveCursor(-1, 0);
        Assert.Equal(BattleEngine.AutoIndex, battle.HUD.MainMenuIndex);
        // Up and down leave it where it is; right goes back to FIGHT
        battle.MoveCursor(0, 1);
        Assert.Equal(BattleEngine.AutoIndex, battle.HUD.MainMenuIndex);
        battle.MoveCursor(1, 0);
        Assert.Equal(0, battle.HUD.MainMenuIndex);
        // Up and down still go round the four commands
        battle.MoveCursor(0, -1);
        Assert.Equal(3, battle.HUD.MainMenuIndex);

        battle.AutoOn = false;
        Assert.False(battle.AutoOffered);
        battle.HUD.MainMenuIndex = 0;
        battle.MoveCursor(-1, 0);
        Assert.NotEqual(BattleEngine.AutoIndex, battle.HUD.MainMenuIndex);
        battle.AutoUntilStopped();
        Assert.False(battle.AutoRunning);
    }

    [Fact]
    public void TheRowIsOffUnlessChosenAndSaysWhatItDoes()
    {
        var settings = new GameSettings();
        Assert.False(settings.AutoBattle);
        var rows = Enum.GetValues<OptionRow>();
        Assert.Equal(Array.IndexOf(rows, OptionRow.BoxTools) + 1, Array.IndexOf(rows, OptionRow.AutoBattle));
        Assert.Equal(Array.IndexOf(rows, OptionRow.AutoBattle) + 1, Array.IndexOf(rows, OptionRow.Controls));

        var (_, off, helpOff) = OptionsScreen.Describe(settings, OptionRow.AutoBattle);
        OptionsScreen.Change(settings, OptionRow.AutoBattle, 1);
        Assert.True(settings.AutoBattle);
        var (label, on, helpOn) = OptionsScreen.Describe(settings, OptionRow.AutoBattle);
        Assert.Equal("Auto-battle", label);
        Assert.NotEqual(off, on);
        Assert.NotEqual(helpOff, helpOn);
        OptionsScreen.Change(settings, OptionRow.AutoBattle, -1);
        Assert.False(settings.AutoBattle);
    }
}
