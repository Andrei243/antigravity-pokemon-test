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
/// Mega Evolution and Primal Reversion (plan 06 · R20), each by Pokémon Showdown's rule at the pinned commit
/// (sim/battle-actions.ts' canMegaEvo and runMegaEvo, sim/battle-queue.ts' order, sim/side.ts' once a battle,
/// data/items.ts' stones and orbs). One rule to a test, under the modern rules and on the rules alone, both sides
/// answered for from outside unless the trainer AI is what is tested; the rolls fixed by kind (<see cref="CoreScenario.Calm"/>).
/// The Pokémon have no IVs, EVs or nature (<see cref="Scenario.Mon"/>): a stat at level 50 is base + 5.
/// </summary>
public class MegaEvolutionTests
{
    private static readonly Ruleset Modern = Ruleset.Modern;

    /// <summary>A wild battle with the player holding a Key Stone (or not), under the rules given.</summary>
    private static BattleCore Wild(Pokemon[] mine, Pokemon foe, bool keyStone = true, Ruleset? rules = null)
    {
        var party = new Party();
        foreach (var p in mine) party.Add(p);
        var core = new BattleCore(new CoreSetup
        {
            PlayerParty = party, WildPokemon = new List<Pokemon> { foe }, Random = Calm(), Rules = rules ?? Modern,
            PlayerController = null, EnemyController = null, PlayerName = "Lucas", KeyStone = keyStone
        });
        core.Start();
        return core;
    }

    /// <summary>A battle against a trainer with a Key Stone of their own; the player's side has one too.</summary>
    private static BattleCore Against(Pokemon[] mine, Pokemon[] theirs, IBattleController? foeMind = null, Ruleset? rules = null,
        BattleFormat format = BattleFormat.Single)
    {
        var party = new Party();
        foreach (var p in mine) party.Add(p);
        var trainer = new Trainer { Id = "ace", Name = "Vera", TrainerClass = "Ace Trainer", PrizeMoney = 100, KeyStone = true, DoubleBattle = format == BattleFormat.Double };
        foreach (var p in theirs) trainer.Party.Add(p);
        var core = new BattleCore(new CoreSetup
        {
            PlayerParty = party, Trainers = new List<Trainer> { trainer }, Random = Calm(), Rules = rules ?? Modern, Format = format,
            PlayerController = null, EnemyController = foeMind, PlayerName = "Lucas", KeyStone = true
        });
        core.Start();
        return core;
    }

    private static Pokemon Holding(Pokemon p, string item)
    {
        p.HeldItem = ItemDatabase.Get(item) ?? throw new ArgumentException(item);
        return p;
    }

    private static Pokemon MegaCharizard() => Holding(Mon("Charizard", 50, "Splash"), "Charizardite X");

    private static List<string> MegaTurn(BattleCore core, int mine = 0, int foe = 0)
    {
        core.Submit(BattleChoice.Fight(Mine, mine) with { Mega = true }, BattleChoice.Fight(Foe, foe));
        return Lines(core.TakeLog());
    }

    [Fact]
    public void APokemonHoldingItsStoneMegaEvolvesBeforeTheMovesAndTakesTheFormsStatsAbilityAndTypes()
    {
        // Charizard at 50: Attack 84 + 5 = 89. Mega Charizard X (130, Tough Claws, Fire and Dragon): 130 + 5 = 135
        var mine = MegaCharizard();
        var core = Wild(new[] { mine }, Mon("Magikarp", 50, "Splash"));
        Assert.Equal(89, mine.Attack);
        var said = MegaTurn(core);
        InOrder(said, "Charizard's Charizardite X is reacting to Lucas's Key Stone!", "Charizard has Mega Evolved into Mega Charizard X!",
            "Charizard used Splash!");
        Assert.Equal("Charizard-Mega-X", mine.Form);
        Assert.Equal(135, mine.Attack);
        Assert.Equal("Tough Claws", mine.AbilityName);
        Assert.True(core.At(Mine).HasType(PokemonType.Dragon));
    }

    [Fact]
    public void TheLineThatSaysSoCarriesTheEvent()
    {
        var core = Wild(new[] { MegaCharizard() }, Mon("Magikarp", 50, "Splash"));
        core.Submit(BattleChoice.Fight(Mine, 0) with { Mega = true }, BattleChoice.Fight(Foe, 0));
        var line = core.TakeLog().OfType<Said>().Single(s => s.Text.Contains("has Mega Evolved"));
        Assert.Contains(new MegaEvolved(Mine, "Charizard-Mega-X", Primal: false), line.Shows);
    }

    [Fact]
    public void TheSpeedAMegaEvolutionGivesCountsForTheTurnItHappensIn()
    {
        // Beedrill at 50: Speed 75 + 5 = 80; Jolteon 130 + 5 = 135 moves first. Mega Beedrill: 145 + 5 = 150, so it
        // moves first in the turn it Mega Evolves (the queue sorted again from Generation 8)
        var core = Wild(new[] { Holding(Mon("Beedrill", 50, "Splash"), "Beedrillite") }, Mon("Jolteon", 50, "Splash"));
        InOrder(Turn(core), "Foe Jolteon used Splash!", "Beedrill used Splash!");
        InOrder(MegaTurn(core), "Beedrill has Mega Evolved into Mega Beedrill!", "Beedrill used Splash!", "Foe Jolteon used Splash!");
    }

    [Fact]
    public void ATrainerMegaEvolvesOnlyOnceInABattle()
    {
        var core = Wild(new[] { MegaCharizard(), Holding(Mon("Beedrill", 50, "Splash"), "Beedrillite") }, Mon("Magikarp", 50, "Splash"));
        MegaTurn(core);
        SwitchTo(core, 1);
        Assert.Equal("You can Mega Evolve only once in a battle!", core.WhyNot(BattleChoice.Fight(Mine, 0) with { Mega = true }));
        Assert.Throws<ArgumentException>(() => MegaTurn(core));
    }

    [Fact]
    public void TwoMegaEvolutionsOfOneTrainersInATurnAreRefused()
    {
        var core = Against(new[] { MegaCharizard(), Holding(Mon("Beedrill", 50, "Splash"), "Beedrillite") },
            new[] { Mon("Magikarp", 50, "Splash"), Mon("Magikarp", 50, "Splash") }, format: BattleFormat.Double);
        Assert.Throws<ArgumentException>(() => core.Submit(BattleChoice.Fight(Mine, 0) with { Mega = true }, BattleChoice.Fight(Mine2, 0) with { Mega = true },
            BattleChoice.Fight(Foe, 0), BattleChoice.Fight(Foe2, 0)));
    }

    [Fact]
    public void WithoutAKeyStoneNobodyMegaEvolves()
    {
        var core = Wild(new[] { MegaCharizard() }, Mon("Magikarp", 50, "Splash"), keyStone: false);
        Assert.Equal("Charizard can't Mega Evolve!", core.WhyNot(BattleChoice.Fight(Mine, 0) with { Mega = true }));
    }

    [Fact]
    public void PlatinumsRulesHaveNoMegaEvolution()
    {
        var core = Wild(new[] { MegaCharizard() }, Mon("Magikarp", 50, "Splash"), rules: Ruleset.Platinum);
        Assert.NotNull(core.WhyNot(BattleChoice.Fight(Mine, 0) with { Mega = true }));
        Assert.False(core.CanMegaEvolve(core.At(Mine)));
    }

    [Fact]
    public void AStoneOfAnotherSpeciesDoesNothing()
    {
        var core = Wild(new[] { Holding(Mon("Charizard", 50, "Splash"), "Beedrillite") }, Mon("Magikarp", 50, "Splash"));
        Assert.False(core.CanMegaEvolve(core.At(Mine)));
    }

    [Fact]
    public void AMegaStaysMegaThroughASwitch()
    {
        var mine = MegaCharizard();
        var core = Wild(new[] { mine, Mon("Bidoof", 50, "Splash") }, Mon("Magikarp", 50, "Splash"));
        MegaTurn(core);
        SwitchTo(core, 1);
        SwitchTo(core, 0);
        Assert.Equal("Charizard-Mega-X", mine.Form);
        Assert.Equal("Tough Claws", mine.AbilityName);
    }

    [Fact]
    public void AMegaGoesBackToWhatItWasAsTheBattleEnds()
    {
        // Charizard's ability in its first slot is Blaze; Mega Charizard X's Tough Claws goes with the form
        var mine = Holding(Mon("Charizard", 50, "Tackle"), "Charizardite X");
        string before = mine.AbilityName!;
        var core = Wild(new[] { mine }, Mon("Magikarp", 5, "Splash"));
        MegaTurn(core);
        Assert.Equal(BattleResult.PlayerVictory, core.Result);
        Assert.Null(mine.Form);
        Assert.Equal(before, mine.AbilityName);
        Assert.Equal(89, mine.Attack);
    }

    [Fact]
    public void AMegaStoneInItsOwnSpeciesHandsCantBeKnockedOffNorTricked()
    {
        var mine = MegaCharizard();
        var core = Against(new[] { mine }, new[] { Mon("Sneasel", 50, "Knock Off", "Trick") });
        Turn(core, foe: 0);
        Assert.Equal("Charizardite X", mine.HeldItem?.Name);
        Turn(core, foe: 1);
        Assert.Equal("Charizardite X", mine.HeldItem?.Name);
    }

    [Fact]
    public void AStoneInAnotherSpeciesHandsCanBeKnockedOff()
    {
        var mine = Holding(Mon("Bidoof", 50, "Splash"), "Charizardite X");
        var core = Against(new[] { mine }, new[] { Mon("Sneasel", 50, "Knock Off") });
        Turn(core);
        Assert.Null(mine.HeldItem);
    }

    [Fact]
    public void KyogreHoldingTheBlueOrbRevertsAsItComesInAndBringsTheHeavyRain()
    {
        var kyogre = Holding(Mon("Kyogre", 50, "Splash"), "Blue Orb");
        var core = Wild(new[] { kyogre }, Mon("Magikarp", 50, "Splash"), keyStone: false);
        var said = Lines(core.TakeLog());
        Assert.Contains("Kyogre's Primal Reversion! It reverted to its primal form!", said);
        Assert.Equal("Kyogre-Primal", kyogre.Form);
        Assert.Equal("Primordial Sea", kyogre.AbilityName);
        Assert.Equal(BattleWeather.HeavyRain, core.Field.Weather);
    }

    [Fact]
    public void APrimalReversionSpendsNoMegaEvolution()
    {
        var core = Wild(new[] { Holding(Mon("Groudon", 50, "Splash"), "Red Orb"), MegaCharizard() }, Mon("Magikarp", 50, "Splash"));
        Assert.Equal("Groudon-Primal", core.At(Mine).Pokemon!.Form);
        Assert.Equal(BattleWeather.HarshSun, core.Field.Weather);
        SwitchTo(core, 1);
        Assert.True(core.CanMegaEvolve(core.At(Mine)));
    }

    [Fact]
    public void RayquazaThatKnowsDragonAscentMegaEvolvesWithNoStoneAndBringsTheStrongWinds()
    {
        var rayquaza = Mon("Rayquaza", 50, "Splash", "Dragon Ascent");
        var core = Wild(new[] { rayquaza }, Mon("Magikarp", 50, "Splash"));
        var said = MegaTurn(core);
        InOrder(said, "Lucas's fervent wish has reached Rayquaza!", "Rayquaza has Mega Evolved into Mega Rayquaza!");
        Assert.Equal("Rayquaza-Mega", rayquaza.Form);
        Assert.Equal(BattleWeather.StrongWinds, core.Field.Weather);
    }

    [Fact]
    public void RayquazaWithoutDragonAscentCantMegaEvolve()
    {
        var core = Wild(new[] { Mon("Rayquaza", 50, "Splash") }, Mon("Magikarp", 50, "Splash"));
        Assert.False(core.CanMegaEvolve(core.At(Mine)));
    }

    [Fact]
    public void TheTrainerAiMegaEvolvesWheneverItCan()
    {
        var foe = Holding(Mon("Charizard", 50, "Splash"), "Charizardite X");
        var core = Against(new[] { Mon("Bidoof", 50, "Splash") }, new[] { foe }, TrainerAi.Instance);
        var said = Turn(core);
        Assert.Contains("Foe Charizard has Mega Evolved into Mega Charizard X!", said);
        Assert.Equal("Charizard-Mega-X", foe.Form);
    }

    [Fact]
    public void ABattleWithAMegaEvolutionReplaysTheSame()
    {
        BattleCore Make() => new(new CoreSetup
        {
            PlayerParty = PartyOf(Holding(Mon("Charizard", 50, "Flamethrower"), "Charizardite X")),
            WildPokemon = new List<Pokemon> { Mon("Snorlax", 50, "Tackle") }, Random = new BattleRandom(7), Rules = Modern,
            PlayerController = null, EnemyController = null, PlayerName = "Lucas", KeyStone = true
        });
        var first = Make();
        first.Start();
        first.Submit(BattleChoice.Fight(Mine, 0) with { Mega = true }, BattleChoice.Fight(Foe, 0));
        first.Submit(BattleChoice.Fight(Mine, 0), BattleChoice.Fight(Foe, 0));
        var again = Make();
        again.Replay(first.Record!);
        Assert.Equal(Lines(first.Log), Lines(again.Log));
        Assert.Contains("Charizard has Mega Evolved into Mega Charizard X!", Lines(again.Log));
    }

    [Fact]
    public void TheMoveMenusToggleSendsTheMegaEvolutionWithTheMoveAndTheScreenTakesTheForm()
    {
        // A Key Stone in the bag is the player's; the item button in the move menu arms it, the move takes it along
        var mine = MegaCharizard();
        var bag = new Inventory();
        bag.AddItem(ItemDatabase.Get(BattleCore.KeyStoneItem)!);
        var battle = new BattleEngine(new BattleSetup
        {
            PlayerParty = PartyOf(mine), Inventory = bag, Pokedex = new Pokedex(),
            WildPokemon = new List<Pokemon> { Mon("Magikarp", 50, "Splash") }, Random = Calm(), Rules = Modern
        });
        Settle(battle);
        battle.SelectMainMenuOption(0);
        Assert.True(battle.CanMegaEvolveNow);
        Assert.True(battle.ToggleMega());
        Assert.True(battle.MegaArmed);
        battle.SelectMove(0);
        var said = Settle(battle);
        Assert.Contains("Charizard has Mega Evolved into Mega Charizard X!", said);
        Assert.Equal("Charizard-Mega-X", mine.Form);
        Assert.False(battle.MegaArmed);
    }

    private static Party PartyOf(Pokemon p)
    {
        var party = new Party();
        party.Add(p);
        return party;
    }

    [Fact]
    public void TheTrainerAiNeverMegaEvolvesUnderPlatinumsRules()
    {
        var foe = Holding(Mon("Charizard", 50, "Splash"), "Charizardite X");
        var core = Against(new[] { Mon("Bidoof", 50, "Splash") }, new[] { foe }, TrainerAi.Instance, Ruleset.Platinum);
        Turn(core);
        Assert.Null(foe.Form);
    }
}
