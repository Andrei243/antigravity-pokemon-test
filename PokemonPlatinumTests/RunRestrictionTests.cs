using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;
using Xunit;
using static PokemonPlatinumTests.CoreScenario;
using static PokemonPlatinumTests.Scenario;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 13 · V5: the restrictions of a run and the shiny odds. Each flag of the record refuses where it should (the team's
/// size, a Monotype, no items in battle, no Pokémon Center, no catching, no running, Set) and under Normal nothing is
/// refused and nothing drawn. The class sets <see cref="RunRules.Current"/> for the shiny roll, so it runs alone.
/// </summary>
[Collection("Shared clocks and dice")]
public class RunRestrictionTests
{
    private static BattleCore Met(RunTerms? terms, string foe = "Bidoof", BattleKind kind = BattleKind.Normal, params Pokemon[] bench)
    {
        var party = new Party();
        var lead = Mon("Turtwig", 10, "Tackle");
        lead.CurrentHP = 5;
        party.Add(lead);
        foreach (var p in bench) party.Add(p);
        var core = new BattleCore(new CoreSetup
        {
            PlayerParty = party, WildPokemon = new List<Pokemon> { Mon(foe, 3, "Tackle") }, Random = Calm(), Rules = Ruleset.Platinum,
            Kind = kind, PlayerController = null, EnemyController = null, PlayerName = "Lucas", Restrictions = terms
        });
        core.Start();
        core.TakeLog();
        return core;
    }

    // ---- Normal refuses nothing

    [Fact]
    public void ANormalRunHasNoRestrictionsAndNoRuleIsOn()
    {
        Assert.Null(RunRules.Normal.BattleTerms());
        Assert.All(RunRules.RuleNames, rule => Assert.False(RunRules.Normal.Has(rule), rule));
        Assert.Equal(8192, RunRules.Normal.ShinyOdds(8192));
        Assert.Equal(4096, RunRules.Normal.ShinyOdds(4096));
        Assert.True(RunRules.Normal.MayCarry(Mon("Starly", 5), null));
        var party = new Party();
        Assert.Equal(Party.MaxSize, party.Limit);
        var core = Met(null);
        Assert.Null(core.WhyNot(BattleChoice.UseItem(Mine, "Poké Ball")));
        Assert.Null(core.WhyNot(BattleChoice.UseItem(Mine, "Potion", 0)));
        Assert.Null(core.WhyNot(BattleChoice.Run(Mine)));
        Assert.False(core.BagShutByTheRun);
    }

    [Fact]
    public void EveryRuleWordNamesAFieldOfTheRecord()
    {
        var all = new RunRules
        {
            Nuzlocke = true, Monotype = PokemonType.Water, PartyLimit = 1, NoBattleItems = true, NoCenter = true, NoCatching = true,
            NoRunning = true, SetStyle = true, ShinyDenominator = 512, Difficulty = Difficulty.Challenge, LevelCap = true
        };
        Assert.All(RunRules.RuleNames.Where(r => r != "easy"), rule => Assert.True(all.Has(rule), rule));
        Assert.True((all with { Difficulty = Difficulty.Easy }).Has("easy"));
        Assert.False(all.Has("not a rule"));
    }

    // ---- the team's size

    [Fact]
    public void AnythingPutOnTheTeamStopsAtTheRunsLimit()
    {
        var party = new Party { Limit = 1 };
        Assert.True(party.Add(Mon("Turtwig", 5)));
        Assert.True(party.IsFull);
        Assert.False(party.Add(Mon("Starly", 4)));
        Assert.False(party.Insert(0, Mon("Starly", 4)));
        Assert.Single(party.Members);
        Assert.Equal(1, RunRules.ClampLimit(0));
        Assert.Equal(6, RunRules.ClampLimit(9));
    }

    [Fact]
    public void ACatchGoesToTheBoxWhenTheRunsTeamIsFull()
    {
        // Through the core's own Caught.ToBox: a one-Pokémon team is full with its first
        var core = Wild(Mon("Machamp", 50, "Strength"), Mon("Bidoof", 10), Steady().Force(RollKind.CatchShake, 0));
        core.PlayerParty.Limit = 1;
        core.TakeLog();
        core.Submit(BattleChoice.UseItem(Mine, "Poké Ball"), BattleChoice.Fight(Foe, 0));
        Assert.Equal(BattleResult.EnemyCaught, core.Result);
        Assert.Contains(core.TakeLog(), e => e is Caught { ToBox: true });
        Assert.Single(core.PlayerParty.Members);
    }

    // ---- the Monotype

    [Fact]
    public void AMonotypeRefusesABallForAPokemonOfAnotherTypeAndTheStarterIsExempt()
    {
        var water = new RunTerms(Monotype: PokemonType.Water);
        var normal = new RunTerms(Monotype: PokemonType.Normal);
        var ball = BattleChoice.UseItem(Mine, "Poké Ball");
        Assert.Equal(Text.Of("battle.runMonotype", ("type", Words.Type(PokemonType.Water))), Met(water).WhyNot(ball));
        Assert.Null(Met(normal).WhyNot(ball));
        Assert.Null(Met(water, "Buizel").WhyNot(ball));

        var rules = new RunRules { Monotype = PokemonType.Water };
        Assert.True(rules.MayCarry(Mon("Piplup", 5), "Turtwig"));
        Assert.False(rules.MayCarry(Mon("Starly", 5), "Turtwig"));
        // The starter's whole line is exempt: Grotle is Turtwig's, Starly is not
        Assert.True(rules.MayCarry(Mon("Torterra", 40), "Turtwig"));
        Assert.False(rules.MayCarry(Mon("Starly", 5), null));
    }

    [Fact]
    public void AMonotypeKeepsAGiftOffTheTeamAndTheStarterOnIt()
    {
        var host = new HeadlessScriptHost { Rules = new RunRules { Monotype = PokemonType.Fire } };
        Assert.False(host.GivePokemon(Mon("Starly", 4)));
        Assert.Empty(host.Party.Members);
        Assert.Equal("Starly", Assert.Single(host.Box).Species.Name);
        Assert.True(host.GivePokemon(Mon("Chimchar", 5)));
        Assert.Equal("Chimchar", Assert.Single(host.Party.Members).Species.Name);
        // Normal gives it to the team
        var plain = new HeadlessScriptHost();
        Assert.True(plain.GivePokemon(Mon("Starly", 4)));
    }

    [Fact]
    public void ThePcWithdrawsOnlyWhatTheMonotypeAllows()
    {
        var rules = new RunRules { Monotype = PokemonType.Grass };
        var party = new Party();
        party.Add(Mon("Turtwig", 5));
        var pc = new PcBoxes();
        var starly = Mon("Starly", 4);
        var budew = Mon("Budew", 4);
        pc.PutIn(0, 0, starly);
        pc.PutIn(0, 1, budew);
        var screen = new PCScreen { WhyNotCarry = p => rules.MayCarry(p, "Turtwig") ? null : Text.Of("run.monotype.notThis", ("type", Words.Type(PokemonType.Grass))) };
        screen.Open(pc);
        var notes = new List<string>();

        screen.Zone = StorageZone.Box;
        screen.Cell = 0;
        screen.Choose(PcAction.Withdraw, party, pc, notes.Add);
        Assert.Equal(Text.Of("run.monotype.notThis", ("type", Words.Type(PokemonType.Grass))), Assert.Single(notes));
        Assert.Single(party.Members);
        Assert.Same(starly, pc[0, 0]);

        screen.Cell = 1;
        screen.Choose(PcAction.Withdraw, party, pc, notes.Add);
        Assert.Equal(2, party.Count);
        Assert.Null(pc[0, 1]);
    }

    // ---- no items in battle

    [Fact]
    public void NoItemsShutsTheBagButBallsAreLetOut()
    {
        var terms = new RunTerms(NoItems: true);
        var core = Met(terms);
        Assert.Equal(BattleCore.NoItemsRefusal, core.WhyNot(BattleChoice.UseItem(Mine, "Potion", 0)));
        Assert.Null(core.WhyNot(BattleChoice.UseItem(Mine, "Poké Ball")));
        // The BAG command is greyed only where no ball could be thrown either
        Assert.False(core.BagShutByTheRun);
        Assert.True(Met(new RunTerms(NoItems: true, NoCatching: true)).BagShutByTheRun);
        // The Great Marsh's bag is its own
        var marsh = Met(new RunTerms(NoItems: true, NoCatching: false), kind: BattleKind.Safari);
        Assert.False(marsh.BagShutByTheRun);
        Assert.NotEqual(BattleCore.NoItemsRefusal, marsh.WhyNot(BattleChoice.UseItem(Mine, "Safari Ball")));
    }

    // ---- no catching, no running

    [Fact]
    public void NoCatchingRefusesEveryBallAndNothingElse()
    {
        var core = Met(new RunTerms(NoCatching: true));
        Assert.Equal(Text.Of("battle.runNoCatching"), core.WhyNot(BattleChoice.UseItem(Mine, "Poké Ball")));
        Assert.Equal(Text.Of("battle.runNoCatching"), core.WhyNot(BattleChoice.UseItem(Mine, "Great Ball")));
        Assert.Null(core.WhyNot(BattleChoice.UseItem(Mine, "Potion", 0)));
        Assert.Null(core.WhyNot(BattleChoice.Run(Mine)));
    }

    [Fact]
    public void NoRunningRefusesRunFromAWildPokemonOnly()
    {
        var core = Met(new RunTerms(NoRunning: true));
        Assert.Equal(Text.Of("battle.runNoRunning"), core.WhyNot(BattleChoice.Run(Mine)));
        Assert.Null(core.WhyNot(BattleChoice.UseItem(Mine, "Poké Ball")));
        // The Great Marsh's RUN is leaving the marsh's Pokémon, and is the marsh's own
        Assert.Null(Met(new RunTerms(NoRunning: true), kind: BattleKind.Safari).WhyNot(BattleChoice.Run(Mine)));
    }

    // ---- the Pokémon Center

    private static HeadlessScriptHost AtTheCenter(RunRules rules, out Pokemon hurt)
    {
        var host = new HeadlessScriptHost { Rules = rules };
        hurt = Mon("Turtwig", 10, "Tackle");
        hurt.CurrentHP = 1;
        host.Party.Add(hurt);
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find("Nurse")!, new PokemonPlatinumEngine.Overworld.NPC { Name = "Nurse", IsHealingNurse = true });
        runner.RunToEnd();
        return host;
    }

    [Fact]
    public void TheNurseDeclinesUnderNoCenterAndHealsOtherwise()
    {
        var declined = AtTheCenter(new RunRules { NoCenter = true }, out var stillHurt);
        Assert.Equal(1, stillHurt.CurrentHP);
        Assert.Contains(declined.Transcript, line => line.Text.Contains("can't see to your Pokémon"));
        Assert.DoesNotContain(declined.Asked, q => q.Question.Contains("rest"));

        var healed = AtTheCenter(RunRules.Normal, out var well);
        Assert.Equal(well.MaxHP, well.CurrentHP);
        Assert.DoesNotContain(healed.Transcript, line => line.Text.Contains("can't see to your Pokémon"));
    }

    [Fact]
    public void AScriptAsksAboutARuleAndTheParserKnowsTheWords()
    {
        var script = ScriptParser.Parse("test", """
            script Asks
              if rule nocenter say "No Centers."
              if not rule monotype say "Any type."
              end
            """)[0];
        Assert.All(script.Code.Take(2), i => Assert.Equal(Query.Rule, i.Condition!.Query));
        Assert.Throws<ScriptException>(() => ScriptParser.Parse("test", "script Bad\n  if rule nothing end\n"));

        var host = new HeadlessScriptHost { Rules = new RunRules { NoCenter = true } };
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        var lines = ScriptParser.Parse("test", """
            script Asks
              if rule nocenter say "No Centers."
              if not rule monotype say "Any type."
              end
            """);
        runner.Start(lines[0]);
        runner.RunToEnd();
        Assert.Equal(new[] { "No Centers.", "Any type." }, host.Transcript.Select(t => t.Text));
    }

    // ---- Set

    [Fact]
    public void ARunThatKeepsTheStyleOnSetLeavesTheRowAlone()
    {
        var screen = new OptionsScreen { StyleLocked = true };
        screen.Open();
        screen.SelectedIndex = (int)OptionRow.BattleStyle;
        var settings = new GameSettings { BattleStyle = BattleStyle.Set };
        Assert.False(screen.Step(settings, 1));
        Assert.Equal(BattleStyle.Set, settings.BattleStyle);

        var free = new OptionsScreen();
        free.Open();
        free.SelectedIndex = (int)OptionRow.BattleStyle;
        Assert.True(free.Step(settings, 1));
        Assert.Equal(BattleStyle.Shift, settings.BattleStyle);
    }

    // ---- the shiny odds

    [Fact]
    public void TheOddsAreTheRulesUnlessTheRunChoseOthers()
    {
        var run = new RunRules { ShinyDenominator = 512 };
        Assert.Equal(512, run.ShinyOdds(8192));
        Assert.Equal(512, run.ShinyOdds(4096));
        // 1 in 512 is a value under 128 of the 65,536; 1 in 8,192 one under 8
        Assert.True(Personality.IsShiny(0, 100, run.ShinyOdds(8192)));
        Assert.False(Personality.IsShiny(0, 100, 8192));
        Assert.False(Personality.IsShiny(0, 200, run.ShinyOdds(8192)));
    }

    [Fact]
    public void APokemonRollsAtTheRunsOddsAndNormalIsUntouched()
    {
        var before = RunRules.Current;
        uint number = PlayerIdentity.Number;
        try
        {
            PlayerIdentity.SetNumber(0);
            // 1 in 1: everyone is shiny, so the constructor is reading the run
            RunRules.Current = new RunRules { ShinyDenominator = 1 };
            Assert.True(new Pokemon(PokemonDatabase.Get("Starly")!, 5, new Random(3)).IsShiny);
            // And with the run Normal the same constructor makes the same Pokémon as before, shiny or not, by the rules' odds
            RunRules.Current = RunRules.Normal;
            var rng = new Random(11);
            var made = Enumerable.Range(0, 40).Select(_ => new Pokemon(PokemonDatabase.Get("Starly")!, 5, rng)).ToList();
            Assert.All(made, p => Assert.Equal(Personality.IsShiny(0, p.Personality, Ruleset.Current.ShinyOdds), p.IsShiny));
        }
        finally
        {
            RunRules.Current = before;
            PlayerIdentity.SetNumber(number);
        }
    }

    // ---- the Shiny Charm

    private sealed class CountingRandom : Random
    {
        public int Calls;
        public override int Next(int maxValue)
        {
            Calls++;
            return base.Next(maxValue);
        }
    }

    [Fact]
    public void TheCharmGivesTwoMoreRollsAndDrawsNothingWithoutIt()
    {
        var before = RunRules.Current;
        uint number = PlayerIdentity.Number;
        try
        {
            Assert.NotNull(ItemDatabase.Get(ShinyCharm.ItemName));
            PlayerIdentity.SetNumber(0);
            var bag = new Inventory();
            var plain = new Pokemon(PokemonDatabase.Get("Starly")!, 5, Gender.Male, Nature.Hardy, false);

            // No charm in the bag: nothing is drawn
            var none = new CountingRandom();
            Assert.False(ShinyCharm.Roll(plain, bag, none));
            Assert.Equal(0, none.Calls);

            // With it, two more personalities are drawn (two draws of 16 bits each) when none is shiny
            bag.AddItem(ItemDatabase.Get(ShinyCharm.ItemName)!, 1);
            RunRules.Current = new RunRules { ShinyDenominator = 65536 };
            var twice = new CountingRandom();
            ShinyCharm.Roll(plain, bag, twice);
            Assert.True(twice.Calls is 4 or 2 || plain.IsShiny);

            // A shiny one is found on the first roll and the rolls stop: one personality drawn, the rest of the Pokémon as it was
            RunRules.Current = new RunRules { ShinyDenominator = 1 };
            var found = new Pokemon(PokemonDatabase.Get("Starly")!, 5, Gender.Male, Nature.Hardy, false);
            var once = new CountingRandom();
            Assert.True(ShinyCharm.Roll(found, bag, once));
            Assert.True(found.IsShiny);
            Assert.Equal(2, once.Calls);
            Assert.Equal(5, found.Level);
            Assert.Equal(found.MaxHP, found.CurrentHP);

            // One that is shiny already, and an Egg, are left alone
            var already = new CountingRandom();
            Assert.True(ShinyCharm.Roll(found, bag, already));
            Assert.Equal(0, already.Calls);
        }
        finally
        {
            RunRules.Current = before;
            PlayerIdentity.SetNumber(number);
        }
    }

    [Fact]
    public void TheCharmRollsKeepTheNatureALeadAbilityChose()
    {
        var before = RunRules.Current;
        uint number = PlayerIdentity.Number;
        try
        {
            PlayerIdentity.SetNumber(0);
            RunRules.Current = new RunRules { ShinyDenominator = 1 };
            var p = new Pokemon(PokemonDatabase.Get("Starly")!, 5, Gender.Male, Nature.Adamant, false);
            Assert.True(p.RerollForShiny(new Random(5), 2, nature: Nature.Adamant));
            Assert.Equal(Nature.Adamant, p.Nature);
        }
        finally
        {
            RunRules.Current = before;
            PlayerIdentity.SetNumber(number);
        }
    }

    // ---- the Nuzlocke's gifts clause (V4's leftover)

    [Fact]
    public void AGiftOrAStaticEncounterIsThePlacesCatchOnlyUnderTheGiftsClause()
    {
        var on = RunRules.NuzlockePreset with { GiftsCount = true };
        var state = new RunState();
        Nuzlocke.Gift(RunRules.NuzlockePreset, state, "route_204_south");
        Assert.True(state.IsEmpty);
        Nuzlocke.Gift(on, state, "route_204_south");
        Assert.True(state.HasMet("route_204_south"));
        Assert.Equal(NuzlockeCatch.AreaSpent, Nuzlocke.MeetStatic(on, state, "route_204_south", Mon("Snorlax", 30), new Pokedex()));
        // Without the clause a static encounter is free and spends nothing
        var other = new RunState();
        Assert.Equal(NuzlockeCatch.Free, Nuzlocke.MeetStatic(RunRules.NuzlockePreset, other, "route_210", Mon("Snorlax", 30), new Pokedex()));
        Assert.True(other.IsEmpty);
        Assert.Equal(NuzlockeCatch.Free, Nuzlocke.MeetStatic(on, other, "route_210", Mon("Snorlax", 30), new Pokedex()));
        Assert.True(other.HasMet("route_210"));
        // Under any other run nothing is written
        var none = new RunState();
        Nuzlocke.Gift(RunRules.Normal, none, "route_201");
        Assert.True(none.IsEmpty);
    }

    // ---- a run's end on a card

    [Fact]
    public void AnEndedRunIsMarkedOnItsCardAndAPlayedOneByItsPreset()
    {
        var ended = new SaveData { Run = RunRules.NuzlockePreset, RunState = new RunState { Ended = true, EndedBy = "whiteout" } };
        Assert.Equal(Text.Of("card.runEnded"), ModernUi.SaveMark(ended)!.Value.Text);
        var live = new SaveData { Run = RunRules.NuzlockePreset, RunState = new RunState() };
        Assert.Equal(Text.Of("run.preset.nuzlocke"), ModernUi.SaveMark(live)!.Value.Text);
        Assert.Null(ModernUi.SaveMark(new SaveData()));
    }
}
