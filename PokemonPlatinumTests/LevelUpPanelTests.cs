using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI;
using PokemonPlatinumEngine.UI.Kit;
using static PokemonPlatinumTests.CoreScenario;
using static PokemonPlatinumTests.Scenario;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 08 · P5, the level-up panel: the log's <see cref="LevelRose"/> carries the stats before the level and at it,
/// a battle shows them as two pages over the level's line and before the level's move, and a Rare Candy used from
/// the bag shows the same two pages before its move and its evolution.
/// <para>
/// A scenario's Pokémon has no IVs, no effort and a neutral nature, so a stat is 2 × base × level / 100 + 5 (and
/// + level + 5 more for HP), rounded down. Turtwig's base stats are 55, 68, 64, 45, 55 and 31 (HP, Attack, Defense,
/// Sp. Atk, Sp. Def, Speed):
/// at level 8: 110 × 8 / 100 = 8, + 18 = 26; 136 × 8 / 100 = 10, + 5 = 15; 128 × 8 / 100 = 10, + 5 = 15;
/// 90 × 8 / 100 = 7, + 5 = 12; 110 × 8 / 100 = 8, + 5 = 13; 62 × 8 / 100 = 4, + 5 = 9;
/// at level 9: 990 / 100 = 9, + 19 = 28; 1224 / 100 = 12, + 5 = 17; 1152 / 100 = 11, + 5 = 16;
/// 810 / 100 = 8, + 5 = 13; 990 / 100 = 9, + 5 = 14; 558 / 100 = 5, + 5 = 10.
/// So the level adds 2, 2, 1, 1, 1 and 1.
/// </para>
/// </summary>
public class LevelUpPanelTests
{
    private const string At8 = "26/15/15/12/13/9", At9 = "28/17/16/13/14/10";
    private static readonly int[] Gains = { 2, 2, 1, 1, 1, 1 };

    private static ItemData Item(string name) => ItemDatabase.Get(name)!;

    private static Pokemon Weak(Pokemon p)
    {
        p.CurrentHP = 1;
        return p;
    }

    /// <summary>A Turtwig a point short of level 9, where it learns Absorb; with four moves it has no room for it.</summary>
    private static Pokemon Turtwig(bool full = false)
    {
        var turtwig = full ? Mon("Turtwig", 8, "Tackle", "Withdraw", "Growl", "Leer") : Mon("Turtwig", 8, "Tackle");
        turtwig.CurrentExp = turtwig.ExpForNextLevel - 1;
        return turtwig;
    }

    /// <summary>Reads a battle on until a level's panel is up (or the battle is over); returns what was said on the way.</summary>
    private static List<string> UntilLevel(BattleEngine battle)
    {
        var said = new List<string>();
        for (int i = 0; i < 400 && battle.LevelUp == null && !battle.IsBattleOver; i++)
        {
            if (battle.IsWaitingForConfirm) said.Add(battle.CurrentMessage);
            battle.ConfirmMessage();
            battle.Update(1f / 60f);
        }
        return said;
    }

    private static int[] GainsOf(LevelUpPanel panel) => LevelUpPanel.Order.Select(panel.Gain).ToArray();

    private static int[] ValuesOf(LevelUpPanel panel) => LevelUpPanel.Order.Select(panel.Value).ToArray();

    private static (BagScreen Bag, Inventory Inventory, Party Party, List<string> Notes) CandyFor(params Pokemon[] team)
    {
        var party = new Party();
        foreach (var p in team) party.Add(p);
        var inventory = new Inventory();
        inventory.AddItem(Item("Rare Candy"), 3);
        var bag = new BagScreen();
        bag.Open();
        bag.BeginTargetChoice(Item("Rare Candy"));
        return (bag, inventory, party, new List<string>());
    }

    private static void Use(BagScreen bag, Inventory inventory, Party party, List<string> notes) =>
        bag.UseOnTarget(inventory, party, notes.Add, new EvolutionContext { Party = party, Bag = inventory });

    // ================================================================== the panel itself

    [Fact]
    public void ThePanelListsTheStatsInTheOriginalsOrderAndTurnsItsTwoPages()
    {
        // The original's statParams: maximum HP, Attack, Defense, Sp. Atk, Sp. Def, Speed (not effort's order, which has Speed fourth)
        Assert.Equal(new[] { StatType.HP, StatType.Attack, StatType.Defense, StatType.SpAttack, StatType.SpDefense, StatType.Speed }, LevelUpPanel.Order);

        var before = new StatSpread { HP = 26, Attack = 15, Defense = 15, SpAttack = 12, SpDefense = 13, Speed = 9 };
        var after = new StatSpread { HP = 28, Attack = 17, Defense = 16, SpAttack = 13, SpDefense = 14, Speed = 10 };
        var panel = new LevelUpPanel(Mon("Turtwig", 9), 9, before, after);
        Assert.Equal((LevelUpPage.Gains, false), (panel.Page, panel.Closed));
        Assert.Equal(Gains, GainsOf(panel));
        Assert.Equal(new[] { 28, 17, 16, 13, 14, 10 }, ValuesOf(panel));

        // A press turns to the new values, another closes it
        Assert.False(panel.Press());
        Assert.Equal((LevelUpPage.Stats, false), (panel.Page, panel.Closed));
        Assert.True(panel.Press());
        Assert.True(panel.Closed);

        // It slides into place and stays there
        Assert.Equal(0f, new LevelUpPanel(Mon("Turtwig", 9), 9, before, after).Appear);
        panel.Advance(LevelUpPanel.AppearTime * 3f);
        Assert.Equal(1f, panel.Appear);

        // The style guide's numbers ("Battle panels", "A level"): 560 by 484, in place after 0.18 s
        Assert.Equal((560f, 484f, 0.18f), (ModernUi.LevelUpWidth, ModernUi.LevelUpHeight, LevelUpPanel.AppearTime));
    }

    // ================================================================== the log

    [Fact]
    public void TheLogsLevelCarriesTheStatsBeforeAndAfterAndTheGainsAddUp()
    {
        var turtwig = Turtwig();
        Assert.Equal(At8, turtwig.Stats().ToString());
        var core = Wild(turtwig, Weak(Mon("Magikarp", 5)));
        core.TakeLog();
        Turn(core);

        var rose = Assert.Single(core.Log.OfType<LevelRose>());
        Assert.Equal((turtwig, 9), (rose.Pokemon, rose.Level));
        Assert.Equal(At8, rose.Before.ToString());
        Assert.Equal(At9, rose.After.ToString());
        // The gains a panel makes of them are the ones worked out above, and added to the old stats they give the
        // Pokémon's own stats as they now stand, which is what the second page shows
        var panel = new LevelUpPanel(turtwig, rose.Level, rose.Before, rose.After);
        int[] own = { turtwig.MaxHP, turtwig.Attack, turtwig.Defense, turtwig.SpAttack, turtwig.SpDefense, turtwig.Speed };
        Assert.Equal(Gains, GainsOf(panel));
        Assert.Equal(own, LevelUpPanel.Order.Select(stat => rose.Before.Of(stat) + panel.Gain(stat)));
        Assert.Equal(own, ValuesOf(panel));

        // The spreads are the event's own: the Pokémon growing on doesn't move them
        turtwig.GainExp(100000, out _);
        Assert.Equal(At9, rose.After.ToString());
    }

    [Fact]
    public void TheLevelsEventStandsWhereItStoodInTheLog()
    {
        // The EXP's line, the EXP, the level, the level's line, the moves: a field more to the event, nothing moved
        var core = Wild(Turtwig(full: true), Weak(Mon("Magikarp", 5)));
        core.TakeLog();
        Turn(core);
        var log = core.Log.ToList();
        int exp = log.FindIndex(e => e is ExpGained);
        Assert.IsType<Said>(log[exp - 1]);
        Assert.IsType<LevelRose>(log[exp + 1]);
        Assert.Equal("Turtwig grew to Lv. 9!", Assert.IsType<Said>(log[exp + 2]).Text);
        Assert.Equal("Absorb", Assert.IsType<MoveWanted>(log[exp + 3]).Move);
    }

    [Fact]
    public void EffortGainedFromTheFoeCountsInTheNewStatsNotInTheOld()
    {
        // BattleScript_GetExpTask gives the effort (SEQ_GET_EXP_START) before it caches the old stats and reckons
        // the new ones (SEQ_GET_EXP_WAIT_LEVEL_UP_EFFECT). With an HP IV of 1 and 3 HP effort, level 8 is
        // (110 + 1 + 3 / 4) × 8 / 100 = 8, + 18 = 26; Bidoof yields 1 HP, so level 9 is (110 + 1 + 4 / 4) × 9 / 100
        // = 1008 / 100 = 10, + 19 = 29, where without the Bidoof's point it would be 999 / 100 = 9, + 19 = 28
        var turtwig = Tipped();
        var core = Wild(turtwig, Weak(Mon("Bidoof", 5)));
        Turn(core);
        var rose = Assert.Single(core.Log.OfType<LevelRose>());
        Assert.Equal((26, 29), (rose.Before.HP, rose.After.HP));
        Assert.Equal(29, turtwig.MaxHP);
    }

    /// <summary>A Turtwig a point short of level 9 whose HP the next point of HP effort raises at that level.</summary>
    private static Pokemon Tipped()
    {
        var turtwig = Turtwig();
        turtwig.IvHP = 1;
        turtwig.EvHP = 3;
        turtwig.CalculateStats();
        turtwig.CurrentHP = turtwig.MaxHP;
        Assert.Equal(26, turtwig.MaxHP);
        return turtwig;
    }

    [Fact]
    public void SeveralLevelsAtOnceAreOneLevelWithTheGainsOfThemAll()
    {
        // The rules give all the EXP at once and say one level: a Turtwig of level 3 that beats a Chansey of level 40
        // goes up many levels, and the panel spans them, from its stats at 3 to its stats where it ends
        var turtwig = Mon("Turtwig", 3, "Tackle");
        string before = turtwig.Stats().ToString();
        var core = Wild(turtwig, Weak(Mon("Chansey", 40)));
        Turn(core);
        var rose = Assert.Single(core.Log.OfType<LevelRose>());
        Assert.True(rose.Level > 4, $"level {rose.Level}");
        Assert.Equal((turtwig.Level, before, turtwig.Stats().ToString()), (rose.Level, rose.Before.ToString(), rose.After.ToString()));
    }

    // ================================================================== a battle on screen

    [Fact]
    public void ABattleShowsTheGainsThenTheNewValuesOverTheLevelsLine()
    {
        var turtwig = Turtwig();
        var battle = Battle(turtwig, Weak(Mon("Magikarp", 5)));
        Assert.Null(battle.LevelUp);
        battle.SelectMove(0);
        var said = UntilLevel(battle);

        // The panel goes up with the level's line, which nobody has read on yet
        var panel = battle.LevelUp!;
        Assert.DoesNotContain("Turtwig grew to Lv. 9!", said);
        Assert.Equal("Turtwig grew to Lv. 9!", battle.CurrentMessage);
        Assert.True(battle.IsWaitingForConfirm);
        Assert.Equal(BattleMenuState.Message, battle.HUD.MenuState);
        Assert.Same(turtwig, panel.Pokemon);
        Assert.Equal((9, LevelUpPage.Gains), (panel.Level, panel.Page));
        Assert.Equal(Gains, GainsOf(panel));
        Assert.Equal((At8, At9), (panel.Before.ToString(), panel.After.ToString()));

        // A press: the new values, over the same line
        battle.ConfirmMessage();
        Assert.Same(panel, battle.LevelUp);
        Assert.Equal(LevelUpPage.Stats, panel.Page);
        Assert.Equal(new[] { 28, 17, 16, 13, 14, 10 }, ValuesOf(panel));
        Assert.Equal("Turtwig grew to Lv. 9!", battle.CurrentMessage);
        Assert.True(battle.IsWaitingForConfirm);

        // Another: the panel comes down and the battle goes on, to the move the level brought
        battle.ConfirmMessage();
        Assert.Null(battle.LevelUp);
        Assert.Equal("Turtwig learned Absorb!", battle.CurrentMessage);

        // And the numbers were the Pokémon's: the game's own Turtwig has them once the battle is over
        Settle(battle);
        Assert.True(battle.IsBattleOver);
        Assert.Null(battle.LevelUp);
        Assert.Equal(At9, turtwig.Stats().ToString());
    }

    [Fact]
    public void ThePanelComesBeforeTheQuestionOfTheLevelsMove()
    {
        var battle = Battle(Turtwig(full: true), Weak(Mon("Magikarp", 5)));
        battle.SelectMove(0);
        var said = UntilLevel(battle);
        Assert.NotNull(battle.LevelUp);
        Assert.False(battle.IsLearningMove);
        Assert.DoesNotContain(said, line => line.Contains("Absorb"));

        // Through its first page the question hasn't begun
        battle.ConfirmMessage();
        Assert.NotNull(battle.LevelUp);
        Assert.False(battle.IsLearningMove);
        Assert.Null(battle.MoveToLearn);

        // The second press takes the panel down, and only then does the Pokémon try to learn its move
        battle.ConfirmMessage();
        Assert.Null(battle.LevelUp);
        Assert.Equal("Turtwig is trying to learn Absorb.", battle.CurrentMessage);
        Assert.Equal("Absorb", battle.MoveToLearn);
    }

    [Fact]
    public void ReadingABattleOnGetsPastTheLevelAsPastAnyLine()
    {
        // Whatever reads a battle on with ConfirmMessage (the tests' helpers, the harness) never waits on the panel:
        // its two pages take two confirms, with the level's line under both
        var battle = Battle(Turtwig(full: true), Weak(Mon("Magikarp", 5)));
        battle.SelectMove(0);
        var under = new List<string>();
        for (int i = 0; i < 400 && !battle.IsBattleOver; i++)
        {
            if (battle.LevelUp != null) under.Add(battle.CurrentMessage);
            battle.ConfirmMessage();
            battle.Update(1f / 60f);
        }
        Assert.True(battle.IsBattleOver);
        Assert.Null(battle.LevelUp);
        Assert.Equal(new[] { "Turtwig grew to Lv. 9!", "Turtwig grew to Lv. 9!" }, under);

        // The tests' own helper lists the level's line once, between the EXP and the level's move
        battle = Battle(Turtwig(full: true), Weak(Mon("Magikarp", 5)));
        var said = Turn(battle);
        Assert.True(battle.IsBattleOver);
        Assert.Single(said, line => line == "Turtwig grew to Lv. 9!");
        Assert.StartsWith("Turtwig gained", said[said.IndexOf("Turtwig grew to Lv. 9!") - 1]);
        InOrder(said, "Turtwig grew to Lv. 9!", "Turtwig is trying to learn Absorb.", "Turtwig did not learn Absorb.");
    }

    [Fact]
    public void NoPanelGoesUpWhereNoLevelRises()
    {
        // EXP that reaches no level
        var machop = Mon("Machop", 30, "Karate Chop");
        var battle = Battle(machop, Weak(Mon("Magikarp", 5)));
        battle.SelectMove(0);
        var said = UntilLevel(battle);
        Assert.True(battle.IsBattleOver);
        Assert.Null(battle.LevelUp);
        Assert.Contains(said, line => line.StartsWith("Machop gained"));
        Assert.Equal(30, machop.Level);

        // And a battle that gives no EXP at all: the Battle Frontier's
        var party = new Party();
        party.Add(Turtwig());
        var trainer = new Trainer { Id = "ace", Name = "Vera", TrainerClass = "Ace Trainer", PrizeMoney = 100 };
        trainer.Party.Add(Weak(Mon("Magikarp", 5)));
        var frontier = new BattleEngine(new BattleSetup
        {
            PlayerParty = party, Inventory = new Inventory(), Pokedex = new Pokedex(), Trainers = new List<Trainer> { trainer },
            Random = Steady(), Rules = Ruleset.Platinum, Frontier = true
        });
        Settle(frontier);
        frontier.SelectMove(0);
        UntilLevel(frontier);
        Assert.True(frontier.IsBattleOver);
        Assert.Null(frontier.LevelUp);
        Assert.Empty(frontier.Core.Log.OfType<LevelRose>());

        // Nor in the catching lesson, which plays itself and ends in a catch: nothing faints, so nothing is gained,
        // however near its level the assistant's Pokémon is
        var piplup = Mon("Piplup", 5, "Pound");
        piplup.CurrentExp = piplup.ExpForNextLevel - 1;
        var assistants = new Party();
        assistants.Add(piplup);
        var lesson = new BattleEngine(new BattleSetup
        {
            PlayerParty = assistants, Inventory = new Inventory(), Pokedex = new Pokedex(), WildPokemon = new List<Pokemon> { Mon("Bidoof", 2, "Tackle") },
            Kind = BattleKind.CatchingLesson, Random = Steady(), Rules = Ruleset.Platinum
        });
        // (its ball's throw holds the lines for a while: the clock is run on between them)
        for (int i = 0; i < 2000 && !lesson.IsBattleOver && lesson.LevelUp == null; i++)
        {
            lesson.ConfirmMessage();
            lesson.Update(0.1f);
        }
        Assert.True(lesson.IsBattleOver);
        Assert.Equal(BattleResult.EnemyCaught, lesson.Result);
        Assert.Null(lesson.LevelUp);
        Assert.Empty(lesson.Core.Log.OfType<LevelRose>());
        Assert.Equal(5, piplup.Level);
    }

    [Fact]
    public void TheScreensPokemonHasThePanelsNumbersWhileThePanelIsUp()
    {
        // The screen's Turtwig is behind the rules' copy: it levels as its EXP is shown, without the Bidoof's point
        // of HP effort, and would say 28 where the panel says 29 (EffortGainedFromTheFoe…, above). It takes the
        // level's own numbers as the level is shown, so its HP box agrees with the panel
        var turtwig = Tipped();
        var battle = Battle(turtwig, Weak(Mon("Bidoof", 5)));
        battle.SelectMove(0);
        UntilLevel(battle);
        var panel = battle.LevelUp!;
        Assert.Equal((26, 29, 3), (panel.Before.HP, panel.After.HP, panel.Gain(StatType.HP)));
        Assert.Equal(panel.After.ToString(), turtwig.Stats().ToString());
        // Unhurt before the level, it has the three hit points the level gave it
        Assert.Equal(29, turtwig.CurrentHP);

        Settle(battle);
        Assert.Equal((29, 29), (turtwig.MaxHP, turtwig.CurrentHP));
        Assert.Equal(panel.After.ToString(), turtwig.Stats().ToString());
    }

    [Fact]
    public void ALevelOnTheBenchShowsThatPokemonsNumbers()
    {
        // The Exp. Share's holder levels without having fought: the panel is its own, not the lead's
        var lead = Mon("Machop", 30, "Karate Chop");
        var bench = With(Turtwig(), item: "Exp. Share");
        var party = new Party();
        party.Add(lead);
        party.Add(bench);
        var battle = new BattleEngine(new BattleSetup
        {
            PlayerParty = party, Inventory = new Inventory(), Pokedex = new Pokedex(), WildPokemon = new List<Pokemon> { Weak(Mon("Magikarp", 5)) },
            Random = Steady(), Rules = Ruleset.Platinum
        });
        Settle(battle);
        battle.SelectMove(0);
        UntilLevel(battle);
        var panel = battle.LevelUp!;
        Assert.Same(bench, panel.Pokemon);
        Assert.Equal((9, At8, At9), (panel.Level, panel.Before.ToString(), panel.After.ToString()));
        Assert.Equal("Turtwig grew to Lv. 9!", battle.CurrentMessage);
        Settle(battle);
        Assert.True(battle.IsBattleOver);
        Assert.Equal(At9, bench.Stats().ToString());
    }

    // ================================================================== a Rare Candy from the bag

    [Fact]
    public void ARareCandyFromTheBagShowsTheSameNumbersAsABattle()
    {
        // The battle's level, for a Turtwig just like the bag's (the Magikarp's point of Speed effort moves no stat)
        var core = Wild(Turtwig(), Weak(Mon("Magikarp", 5)));
        Turn(core);
        var rose = Assert.Single(core.Log.OfType<LevelRose>());

        var turtwig = Mon("Turtwig", 8, "Tackle");
        var (bag, inventory, party, notes) = CandyFor(turtwig);
        Assert.Null(bag.LevelUp);
        Use(bag, inventory, party, notes);

        var panel = bag.LevelUp!;
        Assert.Same(turtwig, panel.Pokemon);
        Assert.Equal((9, LevelUpPage.Gains), (panel.Level, panel.Page));
        Assert.Equal((rose.Before.ToString(), rose.After.ToString()), (panel.Before.ToString(), panel.After.ToString()));
        Assert.Equal(Gains, GainsOf(panel));
        // They are the Pokémon's own
        Assert.Equal(turtwig.Stats().ToString(), panel.After.ToString());
        Assert.Equal(2, inventory.GetQuantity(Item("Rare Candy")));
        // The level's line is the prompt's, under the panel; no notice repeats it
        Assert.Empty(notes);

        // Two presses, and the bag is back on its list; the move the level taught is told once the panel is read,
        // as a battle tells it after its panel
        bag.ConfirmLevelUp(notes.Add);
        Assert.Same(panel, bag.LevelUp);
        Assert.Equal(LevelUpPage.Stats, panel.Page);
        Assert.NotNull(bag.ChoosingFor);
        Assert.Empty(notes);
        bag.ConfirmLevelUp(notes.Add);
        Assert.Null(bag.LevelUp);
        Assert.Null(bag.ChoosingFor);
        Assert.Equal(new[] { "Turtwig learned Absorb!" }, notes);
        Assert.Contains(turtwig.Moves, m => m.Name == "Absorb");
    }

    [Fact]
    public void TheBagsPanelComesBeforeTheLevelsMoveAndItsEvolution()
    {
        // A move that doesn't fit waits for the panel's two presses
        var full = Mon("Turtwig", 8, "Tackle", "Withdraw", "Growl", "Leer");
        var (bag, inventory, party, notes) = CandyFor(full);
        Use(bag, inventory, party, notes);
        Assert.NotNull(bag.LevelUp);
        Assert.Null(bag.Teaching);
        bag.ConfirmLevelUp(notes.Add);
        Assert.Null(bag.Teaching);
        bag.ConfirmLevelUp(notes.Add);
        Assert.Null(bag.LevelUp);
        Assert.Equal("Absorb", bag.Teaching!.Value.Move);

        // So does an evolution: Turtwig at 18
        var grown = Mon("Turtwig", 17, "Tackle");
        (bag, inventory, party, notes) = CandyFor(grown);
        Use(bag, inventory, party, notes);
        Assert.NotNull(bag.LevelUp);
        Assert.Null(bag.TakeEvolution());
        bag.ConfirmLevelUp(notes.Add);
        Assert.Null(bag.TakeEvolution());
        bag.ConfirmLevelUp(notes.Add);
        var request = bag.TakeEvolution()!;
        Assert.Equal((grown, "Grotle", true), (request.Pokemon, request.Evolution.TargetSpecies, request.Cancellable));
    }

    [Fact]
    public void NothingElseHappensInTheBagWhileThePanelIsUp()
    {
        var first = Mon("Turtwig", 8, "Tackle");
        var second = Mon("Bidoof", 5);
        var (bag, inventory, party, notes) = CandyFor(first, second);
        Use(bag, inventory, party, notes);
        Assert.NotNull(bag.LevelUp);

        // No second candy, no cursor, no way back: the panel is read first
        Use(bag, inventory, party, notes);
        bag.MoveTarget(1, 0, party.Count);
        bag.CancelTarget();
        Assert.Equal((9, 5, 0), (first.Level, second.Level, bag.TargetIndex));
        Assert.Equal(2, inventory.GetQuantity(Item("Rare Candy")));
        Assert.NotNull(bag.LevelUp);
        Assert.NotNull(bag.ChoosingFor);
    }

    [Fact]
    public void AFaintedPokemonsCandyShowsThePanelAndAnEggsDoesNothing()
    {
        // Platinum's Rare Candy brings a fainted Pokémon round with the hit points its level gave it: 28 - 26 = 2
        var fainted = Mon("Turtwig", 8, "Tackle");
        fainted.CurrentHP = 0;
        fainted.Status = StatusCondition.Faint;
        var (bag, inventory, party, notes) = CandyFor(fainted);
        Use(bag, inventory, party, notes);
        var panel = bag.LevelUp!;
        Assert.Equal((At8, At9), (panel.Before.ToString(), panel.After.ToString()));
        Assert.Equal((false, 2), (fainted.IsFainted, fainted.CurrentHP));

        // Nothing is used on an Egg: no level, no panel, the candy kept
        var egg = Mon("Turtwig", 1);
        egg.IsEgg = true;
        (bag, inventory, party, notes) = CandyFor(egg);
        Use(bag, inventory, party, notes);
        Assert.Null(bag.LevelUp);
        Assert.Equal(3, inventory.GetQuantity(Item("Rare Candy")));
    }
}
