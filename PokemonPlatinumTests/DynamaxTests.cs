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
/// Dynamax and Gigantamax (plan 06 · R22), each by Pokémon Showdown's rule at the pinned commit (data/conditions.ts'
/// dynamax condition, sim/battle-actions.ts' getMaxMove and getActiveMaxMove, sim/pokemon.ts' getDynamaxRequest and
/// getUndynamaxedHP, sim/battle.ts' endTurn, data/moves.ts' Max Moves, G-Max Moves and Max Guard). One rule to a test,
/// under the modern rules and on the rules alone, both sides answered for from outside unless the trainer AI is what is
/// tested; the rolls fixed by kind (<see cref="CoreScenario.Calm"/>: no critical hit, the strongest roll). The Pokémon
/// have no IVs, EVs or nature (<see cref="Scenario.Mon"/>): a stat at level 50 is base + 5, and HP base + 60.
/// </summary>
public class DynamaxTests
{
    private static readonly Ruleset Modern = Ruleset.Modern;

    /// <summary>A wild battle with the player holding a Dynamax Band (or not), under the rules given.</summary>
    private static BattleCore Wild(Pokemon[] mine, Pokemon foe, bool band = true, Ruleset? rules = null)
    {
        var core = new BattleCore(new CoreSetup
        {
            PlayerParty = PartyOf(mine), WildPokemon = new List<Pokemon> { foe }, Random = Calm(), Rules = rules ?? Modern,
            PlayerController = null, EnemyController = null, PlayerName = "Lucas", DynamaxBand = band
        });
        core.Start();
        return core;
    }

    /// <summary>A battle against a trainer with a Dynamax Band of their own; the player's side has one too.</summary>
    private static BattleCore Against(Pokemon[] mine, Pokemon[] theirs, IBattleController? foeMind = null, Ruleset? rules = null,
        BattleFormat format = BattleFormat.Single)
    {
        var trainer = new Trainer { Id = "ace", Name = "Vera", TrainerClass = "Ace Trainer", PrizeMoney = 100, DynamaxBand = true, DoubleBattle = format == BattleFormat.Double };
        foreach (var p in theirs) trainer.Party.Add(p);
        var core = new BattleCore(new CoreSetup
        {
            PlayerParty = PartyOf(mine), Trainers = new List<Trainer> { trainer }, Random = Calm(), Rules = rules ?? Modern, Format = format,
            PlayerController = null, EnemyController = foeMind, PlayerName = "Lucas", DynamaxBand = true
        });
        core.Start();
        return core;
    }

    private static Party PartyOf(params Pokemon[] mine)
    {
        var party = new Party();
        foreach (var p in mine) party.Add(p);
        return party;
    }

    /// <summary>A Pokémon at its Dynamax Level, with the Gigantamax Factor or not.</summary>
    private static Pokemon Dyna(Pokemon p, int level = 10, bool gigantamax = false)
    {
        p.DynamaxLevel = level;
        p.GigantamaxFactor = gigantamax;
        return p;
    }

    private static List<string> DTurn(BattleCore core, int mine = 0, int foe = 0)
    {
        core.Submit(BattleChoice.Fight(Mine, mine) with { Dynamax = true }, BattleChoice.Fight(Foe, foe));
        return Lines(core.TakeLog());
    }

    [Fact]
    public void ADynamaxAtLevelTenDoublesTheHpAndSaysSo()
    {
        // Snorlax's 160 + 60 = 220; at Dynamax Level 10 the ratio is 1.5 + 0.5 = 2: 440 of 440
        var mine = Dyna(Mon("Snorlax", 50, "Splash"));
        var core = Wild(new[] { mine }, Mon("Magikarp", 50, "Splash"));
        core.Submit(BattleChoice.Fight(Mine, 0) with { Dynamax = true }, BattleChoice.Fight(Foe, 0));
        var log = core.TakeLog();
        var line = log.OfType<Said>().Single(s => s.Text == "Snorlax swelled to a giant size with Dynamax energy!");
        Assert.Contains(new Dynamaxed(Mine, null, false, 440, 440), line.Shows);
        Assert.Equal((440, 440), (mine.MaxHP, mine.CurrentHP));
    }

    [Fact]
    public void TheDynamaxLevelSetsTheShareTheHpIsRaisedBy()
    {
        // At level 0 the ratio is 1.5: 220 × 30 / 20 = 330, and 101 HP left × 1.5 = 151.5, rounded down 151
        var mine = Dyna(Mon("Snorlax", 50, "Splash"), level: 0);
        mine.CurrentHP = 101;
        DTurn(Wild(new[] { mine }, Mon("Magikarp", 50, "Splash")));
        Assert.Equal((330, 151), (mine.MaxHP, mine.CurrentHP));
    }

    [Fact]
    public void ADamagingMoveBecomesItsTypesMaxMoveOfItsPowerAsAMaxMove()
    {
        // Tackle's power as a Max Move is 90 (Max Strike, physical). Bidoof's Attack 50 against Snorlax's Defense 70:
        // floor(floor(22 × 90 × 50 / 70) / 50) + 2 = floor(1414 / 50) + 2 = 30; the strongest roll keeps 30, the
        // same-type bonus makes 30 × 6144 / 4096 = 45. Max Strike then lowers the foe's Speed a stage
        var foe = Mon("Snorlax", 50, "Splash");
        var core = Wild(new[] { Dyna(Mon("Bidoof", 50, "Tackle")) }, foe);
        var said = DTurn(core);
        InOrder(said, "Bidoof swelled to a giant size with Dynamax energy!", "Bidoof used Max Strike!");
        Assert.Equal(220 - 45, foe.CurrentHP);
        Assert.Equal(-1, foe.StatStages.GetValueOrDefault(StatType.Speed));
    }

    [Fact]
    public void TheMaxMovesLineCarriesWhatItWasMadeOf()
    {
        var core = Wild(new[] { Dyna(Mon("Bidoof", 50, "Tackle")) }, Mon("Snorlax", 50, "Splash"));
        core.Submit(BattleChoice.Fight(Mine, 0) with { Dynamax = true }, BattleChoice.Fight(Foe, 0));
        var line = core.TakeLog().OfType<Said>().Single(s => s.Text == "Bidoof used Max Strike!");
        Assert.Contains(new MaxMoveUsed(Mine, "Max Strike", "Tackle"), line.Shows);
    }

    [Fact]
    public void AMaxMoveThroughProtectDoesAQuarter()
    {
        // The 45 above, then modifyDamage's last step: 45 × 1024 / 4096 = 11.25, rounded half down 11
        var foe = Mon("Snorlax", 50, "Protect");
        var said = DTurn(Wild(new[] { Dyna(Mon("Bidoof", 50, "Tackle")) }, foe));
        InOrder(said, "Foe Snorlax protected itself!", "Bidoof used Max Strike!", "Foe Snorlax couldn't fully protect itself and got hurt!");
        Assert.Equal(220 - 11, foe.CurrentHP);
    }

    [Fact]
    public void AStatusMoveBecomesMaxGuardWhichGoesFirstAndStopsTheFoe()
    {
        // Bidoof (Speed 36) is slower than Jolteon (135); Max Guard goes at +4 whatever Growl's priority
        var mine = Dyna(Mon("Bidoof", 50, "Growl"));
        var said = DTurn(Wild(new[] { mine }, Mon("Jolteon", 50, "Tackle")));
        InOrder(said, "Bidoof used Max Guard!", "Bidoof protected itself!", "Foe Jolteon used Tackle!", "Bidoof protected itself!");
        Assert.Equal(mine.MaxHP, mine.CurrentHP);
    }

    [Fact]
    public void AMaxMoveGoesAtPriorityNoughtWhateverItWasMadeOf()
    {
        // A Max Strike made of Quick Attack goes after the faster Jolteon (Showdown reads the Max Move's own priority)
        var core = Wild(new[] { Dyna(Mon("Bidoof", 50, "Quick Attack")) }, Mon("Jolteon", 50, "Splash"));
        InOrder(DTurn(core), "Foe Jolteon used Splash!", "Bidoof used Max Strike!");
    }

    [Fact]
    public void ADynamaxLastsThreeTurnsAndGivesTheHpBackRoundedUp()
    {
        // 301 of 440 goes back to ceil(301 × 220 / 440) = ceil(150.5) = 151 of 220
        var mine = Dyna(Mon("Snorlax", 50, "Splash"));
        var core = Wild(new[] { mine }, Mon("Magikarp", 50, "Splash"));
        DTurn(core);
        mine.CurrentHP = 301;
        Assert.DoesNotContain("Snorlax shrank back to its usual size!", Turn(core));
        core.Submit(BattleChoice.Fight(Mine, 0), BattleChoice.Fight(Foe, 0));
        var log = core.TakeLog();
        var line = log.OfType<Said>().Single(s => s.Text == "Snorlax shrank back to its usual size!");
        Assert.Contains(new DynamaxEnded(Mine, null, 220, 151), line.Shows);
        Assert.Equal((220, 151), (mine.MaxHP, mine.CurrentHP));
    }

    [Fact]
    public void ATrainerDynamaxesOnlyOnceInABattle()
    {
        var core = Wild(new[] { Dyna(Mon("Snorlax", 50, "Splash")) }, Mon("Magikarp", 50, "Splash"));
        DTurn(core);
        Turn(core);
        Turn(core);
        Assert.Equal("You can Dynamax only once in a battle!", core.WhyNot(BattleChoice.Fight(Mine, 0) with { Dynamax = true }));
    }

    [Fact]
    public void TwoDynamaxesOfOneTrainersInATurnAreRefused()
    {
        var core = Against(new[] { Dyna(Mon("Bidoof", 50, "Tackle")), Dyna(Mon("Bidoof", 50, "Tackle")) },
            new[] { Mon("Magikarp", 50, "Splash"), Mon("Magikarp", 50, "Splash") }, format: BattleFormat.Double);
        Assert.Throws<ArgumentException>(() => core.Submit(BattleChoice.Fight(Mine, 0) with { Dynamax = true }, BattleChoice.Fight(Mine2, 0) with { Dynamax = true },
            BattleChoice.Fight(Foe, 0), BattleChoice.Fight(Foe2, 0)));
    }

    [Fact]
    public void WithoutADynamaxBandOrUnderPlatinumsRulesNobodyDynamaxes()
    {
        var core = Wild(new[] { Dyna(Mon("Bidoof", 50, "Tackle")) }, Mon("Snorlax", 50, "Splash"), band: false);
        Assert.Equal("Bidoof can't Dynamax!", core.WhyNot(BattleChoice.Fight(Mine, 0) with { Dynamax = true }));
        var platinum = Wild(new[] { Dyna(Mon("Bidoof", 50, "Tackle")) }, Mon("Snorlax", 50, "Splash"), rules: Ruleset.Platinum);
        Assert.False(platinum.CanDynamax(platinum.At(Mine)));
    }

    [Fact]
    public void AMegaStonesHolderAZCrystalsHolderAndZacianCantDynamax()
    {
        var charizard = Dyna(Mon("Charizard", 50, "Ember"));
        charizard.HeldItem = ItemDatabase.Get("Charizardite X");
        var mega = Wild(new[] { charizard }, Mon("Magikarp", 50, "Splash"));
        Assert.False(mega.CanDynamax(mega.At(Mine)));
        var bidoof = Dyna(Mon("Bidoof", 50, "Tackle"));
        bidoof.HeldItem = ItemDatabase.Get("Normalium Z");
        var core = Wild(new[] { bidoof }, Mon("Magikarp", 50, "Splash"));
        Assert.False(core.CanDynamax(core.At(Mine)));
        var zacian = Wild(new[] { Dyna(Mon("Zacian", 50, "Tackle")) }, Mon("Magikarp", 50, "Splash"));
        Assert.False(zacian.CanDynamax(zacian.At(Mine)));
    }

    [Fact]
    public void ADynamaxedPokemonDoesntFlinch()
    {
        // Fake Out goes at +3 after the Dynamax (which comes before every move) and its flinch is kept off
        var said = DTurn(Wild(new[] { Dyna(Mon("Snorlax", 50, "Tackle")) }, Mon("Meowth", 50, "Fake Out")));
        InOrder(said, "Foe Meowth used Fake Out!", "Snorlax used Max Strike!");
        Assert.DoesNotContain(said, l => l.Contains("flinched"));
    }

    [Fact]
    public void EncoreFailsAgainstADynamaxedPokemon()
    {
        var mine = Dyna(Mon("Snorlax", 50, "Tackle", "Splash"));
        var core = Wild(new[] { mine }, Mon("Jolteon", 50, "Encore"));
        DTurn(core);
        var said = Turn(core);
        InOrder(said, "Foe Jolteon used Encore!", "But it failed!");
    }

    [Fact]
    public void ADynamaxedPokemonCantBeDraggedOut()
    {
        var core = Against(new[] { Dyna(Mon("Snorlax", 50, "Splash")), Mon("Bidoof", 50, "Tackle") }, new[] { Mon("Jolteon", 50, "Roar") });
        var said = DTurn(core);
        Assert.Contains("Snorlax is far too huge to be moved!", said);
        Assert.Equal("Snorlax", core.At(Mine).Pokemon!.Species.Name);
    }

    [Fact]
    public void ADynamaxEndsAsItsPokemonIsCalledBack()
    {
        var mine = Dyna(Mon("Snorlax", 50, "Splash"));
        var core = Against(new[] { mine, Mon("Bidoof", 50, "Tackle") }, new[] { Mon("Magikarp", 50, "Splash") });
        DTurn(core);
        core.Submit(BattleChoice.Switch(Mine, 1), BattleChoice.Fight(Foe, 0));
        Assert.Contains(core.TakeLog(), e => e is DynamaxEnded { MaxHp: 220, Hp: 220 });
        Assert.Equal((220, 220), (mine.MaxHP, mine.CurrentHP));
    }

    [Fact]
    public void MaxFlareBringsTheSun()
    {
        var core = Wild(new[] { Dyna(Mon("Charmander", 50, "Ember")) }, Mon("Snorlax", 50, "Splash"));
        InOrder(DTurn(core), "Charmander used Max Flare!", "The sunlight turned harsh!");
        Assert.Equal(BattleWeather.Sun, core.Field.Weather);
    }

    [Fact]
    public void AChoiceItemDoesntHoldADynamaxedPokemon()
    {
        var mine = Dyna(Mon("Snorlax", 50, "Tackle", "Splash"));
        mine.HeldItem = ItemDatabase.Get("Choice Band");
        var core = Wild(new[] { mine }, Mon("Magikarp", 50, "Splash"));
        DTurn(core);
        Assert.Null(core.WhyNot(BattleChoice.Fight(Mine, 1)));
    }

    [Fact]
    public void ACharizardWithTheGigantamaxFactorTakesItsFormAndUsesGMaxWildfire()
    {
        // The flames then burn the foe's side at each turn's end for a sixth of its HP before any Dynamax: 220 / 6 = 36
        var charizard = Dyna(Mon("Charizard", 50, "Flamethrower", "Growl"), gigantamax: true);
        var foe = Mon("Snorlax", 50, "Splash");
        var core = Wild(new[] { charizard }, foe);
        core.Submit(BattleChoice.Fight(Mine, 0) with { Dynamax = true }, BattleChoice.Fight(Foe, 0));
        var log = core.TakeLog();
        var said = Lines(log);
        InOrder(said, "Charizard swelled into its Gigantamax form!", "Charizard used G-Max Wildfire!", "Flames whirl around the foe's team!", "Foe Snorlax is scorched by the whirling flames!");
        Assert.Contains(log.OfType<Said>().SelectMany(s => s.Shows), e => e is Dynamaxed { Form: "Charizard-Gmax", Gigantamax: true });
        Assert.Contains(log.OfType<Said>().SelectMany(s => s.Shows), e => e is SideConditionChanged { Condition: SideCondition.GMaxWildfire, Layers: 1 });
        Assert.Equal("Charizard-Gmax", charizard.Form);
        int before = foe.CurrentHP;
        // Max Guard the second turn, so that only the flames touch it
        Turn(core, mine: 1);
        Assert.Equal(before - 36, foe.CurrentHP);
    }

    [Fact]
    public void GMaxSteelsurgeBitesWhoeverComesInAsStealthRockOfSteel()
    {
        // A Bidoof comes in: Steel against Normal is even, an eighth of its 119: 14 (the Copperajah guards meanwhile)
        var foes = new[] { Mon("Snorlax", 50, "Splash"), Mon("Bidoof", 50, "Splash") };
        var core = Against(new[] { Dyna(Mon("Copperajah", 50, "Iron Head", "Splash"), gigantamax: true) }, foes);
        var said = DTurn(core);
        InOrder(said, "Copperajah used G-Max Steelsurge!", "Sharp steel shards float around the foe's team!");
        Assert.True(core.Field.Side(BattleSide.Enemy).GMaxSteelsurge);
        core.Submit(BattleChoice.Fight(Mine, 1), BattleChoice.Switch(Foe, 1));
        Assert.Contains("Steel shards dug into Foe Bidoof!", Lines(core.TakeLog()));
        Assert.Equal(119 - 14, foes[1].CurrentHP);
    }

    [Fact]
    public void TheGigantamaxFormsOfAFormAreItsOwn()
    {
        var urshifu = Mon("Urshifu", 50, "Surging Strikes");
        urshifu.ChangeForm("Urshifu-Rapid-Strike");
        urshifu.GigantamaxFactor = true;
        Assert.Equal("Urshifu-Rapid-Strike-Gmax", BattleCore.GigantamaxFormOf(urshifu));
        Assert.Equal("G-Max Rapid Flow", BattleCore.MaxMoveOf(urshifu.Species, urshifu.Moves[0].Data, true, urshifu.Form)?.Name);
        var plain = Mon("Urshifu", 50, "Wicked Blow");
        plain.GigantamaxFactor = true;
        Assert.Equal("Urshifu-Single-Strike-Gmax", BattleCore.GigantamaxFormOf(plain));
        Assert.Equal("G-Max One Blow", BattleCore.MaxMoveOf(plain.Species, plain.Moves[0].Data, true, plain.Form)?.Name);
        var meowth = Mon("Meowth", 50, "Scratch");
        meowth.ChangeForm("Meowth-Galar");
        meowth.GigantamaxFactor = true;
        Assert.Null(BattleCore.GigantamaxFormOf(meowth));
    }

    [Fact]
    public void TheFormAndHpGoBackAsTheBattleEnds()
    {
        var charizard = Dyna(Mon("Charizard", 50, "Flamethrower"), gigantamax: true);
        var core = Wild(new[] { charizard }, Mon("Magikarp", 5, "Splash"));
        DTurn(core);
        Assert.Equal(BattleResult.PlayerVictory, core.Result);
        Assert.Null(charizard.Form);
        Assert.Equal(78 + 60, charizard.MaxHP);
    }

    [Fact]
    public void TheTrainerAiDynamaxesWheneverItCan()
    {
        var said = Turn(Against(new[] { Mon("Snorlax", 50, "Splash") }, new[] { Dyna(Mon("Bidoof", 50, "Tackle")) }, TrainerAi.Instance));
        Assert.Contains("Foe Bidoof swelled to a giant size with Dynamax energy!", said);
        Assert.Contains("Foe Bidoof used Max Strike!", said);
    }

    [Fact]
    public void TheTrainerAiNeverDynamaxesUnderPlatinumsRules()
    {
        var said = Turn(Against(new[] { Mon("Snorlax", 50, "Splash") }, new[] { Dyna(Mon("Bidoof", 50, "Tackle")) }, TrainerAi.Instance, Ruleset.Platinum));
        Assert.DoesNotContain(said, l => l.Contains("Dynamax"));
    }

    [Fact]
    public void ABattleWithADynamaxReplaysTheSame()
    {
        BattleCore Make() => new(new CoreSetup
        {
            PlayerParty = PartyOf(Dyna(Mon("Charizard", 50, "Flamethrower", "Growl"), gigantamax: true)),
            WildPokemon = new List<Pokemon> { Mon("Snorlax", 50, "Tackle") }, Random = new BattleRandom(7), Rules = Modern,
            PlayerController = null, EnemyController = null, PlayerName = "Lucas", DynamaxBand = true
        });
        var first = Make();
        first.Start();
        first.Submit(BattleChoice.Fight(Mine, 0) with { Dynamax = true }, BattleChoice.Fight(Foe, 0));
        first.Submit(BattleChoice.Fight(Mine, 1), BattleChoice.Fight(Foe, 0));
        first.Submit(BattleChoice.Fight(Mine, 0), BattleChoice.Fight(Foe, 0));
        var again = Make();
        again.Replay(first.Record!);
        Assert.Equal(Lines(first.Log), Lines(again.Log));
        Assert.Contains("Charizard used Max Guard!", Lines(again.Log));
    }

    [Fact]
    public void TheMoveMenusItemButtonArmsTheDynamaxAndTheScreenTakesTheHp()
    {
        // A Dynamax Band in the bag is the player's; with no Mega Evolution or Z-Move to be had the item button arms it
        var bag = new Inventory();
        bag.AddItem(ItemDatabase.Get(BattleCore.DynamaxBandItem)!);
        var bidoof = Dyna(Mon("Bidoof", 50, "Tackle"));
        var battle = new BattleEngine(new BattleSetup
        {
            PlayerParty = PartyOf(bidoof), Inventory = bag, Pokedex = new Pokedex(),
            WildPokemon = new List<Pokemon> { Mon("Snorlax", 50, "Splash") }, Random = Calm(), Rules = Modern
        });
        Settle(battle);
        battle.SelectMainMenuOption(0);
        Assert.False(battle.CanZMoveNow);
        Assert.True(battle.CanDynamaxNow);
        Assert.True(battle.ToggleLaterMechanic());
        Assert.True(battle.DynamaxArmed);
        Assert.Equal("Max Strike", battle.MaxMoveAt(0)?.Name);
        battle.SelectMove(0);
        var said = Settle(battle);
        Assert.Contains("Bidoof used Max Strike!", said);
        Assert.Equal((238, 238), (bidoof.MaxHP, bidoof.CurrentHP));
    }
}
