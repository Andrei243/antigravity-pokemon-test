using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using Xunit;
using static PokemonPlatinumTests.CoreScenario;
using static PokemonPlatinumTests.Scenario;

namespace PokemonPlatinumTests;

/// <summary>
/// EXP for a catch (plan 40 · GE5, the critic's item 1; <c>Ruleset.ExpForACatch</c>, the modern rules' alone): since X and Y
/// a wild Pokémon caught gives the team the EXP its faint would have, its lines, levels and moves after the catch's own
/// line and before the battle ends, under the crossed region's half, the Exp. Charm, Easy's share and the run's cap. The
/// numbers are worked out by hand against <see cref="Formulas.ScaledExp"/> in the comments.
/// </summary>
public class CatchExpTests
{
    /// <summary>
    /// A battle against a wild Bidoof of level 10, the player's one Pokémon throwing a Poké Ball that holds (or, with
    /// <paramref name="knockOut"/>, using its first move on a Bidoof at 1 HP), both sides answered from outside.
    /// </summary>
    private static BattleCore Met(Pokemon mine, Ruleset rules, bool knockOut = false, bool crossed = false, bool expCharm = false,
        Difficulty difficulty = Difficulty.Normal, int cap = 0)
    {
        var foe = Mon("Bidoof", 10);
        if (knockOut) foe.CurrentHP = 1;
        var party = new Party();
        party.Add(mine);
        var core = new BattleCore(new CoreSetup
        {
            PlayerParty = party, WildPokemon = new List<Pokemon> { foe }, Random = Calm().Force(RollKind.CatchShake, 0), Rules = rules,
            Conditions = new BattleConditions { ExpCharm = expCharm }, PlayerController = null, EnemyController = null, PlayerName = "Lucas",
            CrossedExp = crossed, Difficulty = difficulty, RunLevelCap = cap
        });
        core.Start();
        core.TakeLog();
        core.Submit(knockOut ? BattleChoice.Fight(Mine, 0) : BattleChoice.UseItem(Mine, "Poké Ball"), BattleChoice.Fight(Foe, 0));
        return core;
    }

    private static int Gained(BattleCore core) => core.Log.OfType<ExpGained>().Sum(e => e.Amount);

    [Fact]
    public void ACaughtPokemonGivesTheTeamWhatItsFaintWouldHave()
    {
        // Bidoof (base EXP 58) at level 10 against a Machamp of 50, by the modern formula: 58 × 10 / 5 = 116, times
        // ((2 × 10 + 10) / (10 + 50 + 10))^2.5 = (3/7)^2.5 = 0.12025, is 13.949, rounded down 13, plus one: 14
        Assert.Equal(14, Formulas.ScaledExp(58, 10, 50, fought: true, traded: false, luckyEgg: false, pastEvolution: false));
        var caught = Met(Mon("Machamp", 50, "Strength"), Ruleset.Modern);
        Assert.Equal(BattleResult.EnemyCaught, caught.Result);
        Assert.Equal(14, Gained(caught));
        // The same as the Bidoof's faint gives
        var fainted = Met(Mon("Machamp", 50, "Strength"), Ruleset.Modern, knockOut: true);
        Assert.Equal(BattleResult.PlayerVictory, fainted.Result);
        Assert.Equal(Gained(fainted), Gained(caught));

        // Told after the catch's own line, and before the end; and the Bidoof gains none of its own
        var lines = Lines(caught.Log);
        Assert.Equal(new[] { "Lucas used one Poké Ball!", "Gotcha! Bidoof was caught!", "Machamp gained 14 EXP. Points!" }, lines.TakeLast(3));
        int gotcha = caught.Log.ToList().FindIndex(e => e is Caught), exp = caught.Log.ToList().FindIndex(e => e is ExpGained), ended = caught.Log.ToList().FindIndex(e => e is Ended);
        Assert.True(gotcha < exp && exp < ended, $"caught {gotcha}, EXP {exp}, ended {ended}");
        Assert.Single(caught.Log.OfType<ExpGained>());
        Assert.Equal(2, caught.PlayerParty.Count);
    }

    [Fact]
    public void PlatinumsRulesGiveNothingForACatch()
    {
        var caught = Met(Mon("Machamp", 50, "Strength"), Ruleset.Platinum);
        Assert.Equal(BattleResult.EnemyCaught, caught.Result);
        Assert.Empty(caught.Log.OfType<ExpGained>());
        Assert.Equal(new[] { "Lucas used one Poké Ball!", "Gotcha! Bidoof was caught!" }, Lines(caught.Log).TakeLast(2));
    }

    [Fact]
    public void ALevelReachedOnACatchBringsItsPanelAndItsMoveBeforeTheEnd()
    {
        // A Turtwig of level 8 one EXP short of 9: the Bidoof gives it 116 × ((30 / 28)^2.5 = 1.18826) = 137.84, rounded down
        // 137, plus one: 138, which takes it to 9 (419 EXP, Medium Slow) and not to 10 (560); at 9 it learns Absorb
        var turtwig = Mon("Turtwig", 8, "Tackle", "Withdraw");
        turtwig.CurrentExp = Pokemon.GetExpForLevel(9, turtwig.Species.GrowthRate) - 1;
        Assert.Equal(138, Formulas.ScaledExp(58, 10, 8, fought: true, traded: false, luckyEgg: false, pastEvolution: false));
        var core = Met(turtwig, Ruleset.Modern);
        Assert.Equal((9, 138), (turtwig.Level, Gained(core)));
        var log = core.Log.ToList();
        int rose = log.FindIndex(e => e is LevelRose { Level: 9 }), ended = log.FindIndex(e => e is Ended);
        Assert.True(rose > log.FindIndex(e => e is Caught) && rose < ended);
        var lines = Lines(log);
        Assert.Contains("Turtwig grew to Lv. 9!", lines);
        Assert.Contains("Turtwig learned Absorb!", lines);
        Assert.Contains(turtwig, core.LeveledUp);
    }

    [Fact]
    public void TheRunsCapHoldsACatchsEXPAsAFaintsIsHeld()
    {
        // Under a cap of 9 the Turtwig, from the start of level 8 (314 EXP), may be given only what takes it to 9's 419: 105 of the 138
        var turtwig = Mon("Turtwig", 8, "Tackle", "Withdraw");
        turtwig.CurrentExp = Pokemon.GetExpForLevel(8, turtwig.Species.GrowthRate);
        var core = Met(turtwig, Ruleset.Modern, cap: 9);
        Assert.Equal(9, turtwig.Level);
        Assert.Equal(Pokemon.GetExpForLevel(9, turtwig.Species.GrowthRate), turtwig.CurrentExp);
        Assert.Equal(Pokemon.GetExpForLevel(9, turtwig.Species.GrowthRate) - Pokemon.GetExpForLevel(8, turtwig.Species.GrowthRate), Gained(core));
        Assert.Contains("Turtwig can't grow past Lv. 9 yet.", Lines(core.Log));
    }

    [Fact]
    public void TheCrossedRegionsHalfTheExpCharmAndEasyEachTakeTheirShareInTurn()
    {
        // 14; a crossed region's half again, 21; the Exp. Charm's half again of that, 31.5 → 31; Easy's three halves, 46.5 → 46
        Assert.Equal(21, Gained(Met(Mon("Machamp", 50, "Strength"), Ruleset.Modern, crossed: true)));
        Assert.Equal(21, Gained(Met(Mon("Machamp", 50, "Strength"), Ruleset.Modern, expCharm: true)));
        Assert.Equal(31, Gained(Met(Mon("Machamp", 50, "Strength"), Ruleset.Modern, crossed: true, expCharm: true)));
        Assert.Equal(46, Gained(Met(Mon("Machamp", 50, "Strength"), Ruleset.Modern, crossed: true, expCharm: true, difficulty: Difficulty.Easy)));
        // The charm's share is on the line itself
        Assert.Contains("Machamp gained 31 EXP. Points!", Lines(Met(Mon("Machamp", 50, "Strength"), Ruleset.Modern, crossed: true, expCharm: true).Log));
    }

    [Fact]
    public void NothingIsGivenForTheGreatMarshsCatchNorForThePalParks()
    {
        foreach (var kind in new[] { BattleKind.Safari, BattleKind.PalPark })
        {
            var party = new Party();
            party.Add(Mon("Machamp", 50, "Strength"));
            // (The wild side chooses for itself, as in the game: a Safari battle asks the player alone)
            var core = new BattleCore(new CoreSetup
            {
                PlayerParty = party, WildPokemon = new List<Pokemon> { Mon("Bidoof", 10) }, Random = Calm().Force(RollKind.CatchShake, 0).Force(RollKind.Safari, 9),
                Rules = Ruleset.Modern, Kind = kind, SpecialBalls = 30, PlayerController = null, PlayerName = "Lucas"
            });
            core.Start();
            core.TakeLog();
            // (Both battles throw the bag's Safari Ball: a Park Ball is the line's word for it in Pal Park)
            core.Submit(BattleChoice.UseItem(Mine, "Safari Ball"));
            Assert.Equal(BattleResult.EnemyCaught, core.Result);
            Assert.Empty(core.Log.OfType<ExpGained>());
        }
    }

    [Fact]
    public void TheFacePlaysTheCatchsLevelBeforeTheBattleEnds()
    {
        // The game's own battle: the level's panel goes up over the catch's EXP, and the battle is over only after it
        var turtwig = Mon("Turtwig", 8, "Tackle", "Withdraw");
        turtwig.CurrentExp = Pokemon.GetExpForLevel(9, turtwig.Species.GrowthRate) - 1;
        var party = new Party();
        party.Add(turtwig);
        var bag = new Inventory();
        bag.AddItem(ItemDatabase.Get("Poké Ball")!);
        var battle = new BattleEngine(new BattleSetup
        {
            PlayerParty = party, Inventory = bag, Pokedex = new Pokedex(), WildPokemon = new List<Pokemon> { Mon("Bidoof", 10) },
            Random = Steady().Force(RollKind.CatchShake, 0), Rules = Ruleset.Modern, PlayerName = "Lucas"
        });
        Settle(battle);
        battle.UseItem(ItemDatabase.Get("Poké Ball")!);
        bool panel = false, overBeforePanel = false;
        var said = new List<string>();
        for (int i = 0; i < 2000 && !battle.IsBattleOver; i++)
        {
            if (battle.LevelUp != null) panel = true;
            if (battle.IsWaitingForConfirm && (said.Count == 0 || said[^1] != battle.CurrentMessage)) said.Add(battle.CurrentMessage);
            if (battle.IsWaitingForConfirm || battle.HUD.MenuState == BattleMenuState.LearnMove) battle.ConfirmMessage();
            battle.Update(1f / 60f);
        }
        overBeforePanel = !panel;
        Assert.True(battle.IsBattleOver);
        Assert.False(overBeforePanel, string.Join(" / ", said));
        Assert.Equal(9, turtwig.Level);
        int gotcha = said.FindIndex(s => s.StartsWith("Gotcha!")), grew = said.IndexOf("Turtwig grew to Lv. 9!");
        Assert.True(gotcha >= 0 && grew > gotcha, string.Join(" / ", said));
        Assert.Contains(party.Members, p => p.Species.Name == "Bidoof");
    }
}
