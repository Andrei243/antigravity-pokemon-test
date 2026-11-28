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
/// The winter masks (plan 46 · CP13; <c>docs/mechanics/rulings.md</c>, "The winter masks"): one put on a Pokémon once a
/// battle with a move, its second type and ability the mask's through switches to the battle's end, the four roles, the
/// trainer AI's sense of when, the Vs. Recorder's file, the bag's MASKS pocket, the kit's patterns, and nothing at all
/// with no mask.
/// </summary>
public class MaskTests
{
    private static readonly Ruleset Modern = Ruleset.Modern;
    private static readonly string[] All = { WinterMasks.Bear, WinterMasks.Goat, WinterMasks.Horse, WinterMasks.Stag };

    /// <summary>A battle against a trainer under the modern rules, each side's masks given (null: none), both sides answered from outside unless the AI chooses for the foe.</summary>
    private static BattleCore Masked(Pokemon[] mine, Pokemon[] theirs, string[]? myMasks = null, string[]? theirMasks = null, bool ai = false,
        RunTerms? run = null, Ruleset? rules = null, bool doubles = false)
    {
        var party = new Party();
        foreach (var p in mine) party.Add(p);
        var trainer = new Trainer { Id = "dancer", Name = "Ilinca", TrainerClass = "Ace Trainer", PrizeMoney = 100, Masks = theirMasks?.ToList(), DoubleBattle = doubles };
        foreach (var p in theirs) trainer.Party.Add(p);
        var core = new BattleCore(new CoreSetup
        {
            PlayerParty = party, Trainers = new List<Trainer> { trainer }, Random = Calm(), Rules = rules ?? Modern,
            Format = doubles ? BattleFormat.Double : BattleFormat.Single,
            PlayerController = null, EnemyController = ai ? TrainerAi.Instance : null, PlayerName = "Lucas",
            Masks = myMasks, Restrictions = run
        });
        core.Start();
        return core;
    }

    /// <summary>A turn of a single battle: each side uses its first move (or the one given), putting the mask given on.</summary>
    private static (List<string> Said, List<BattleEvent> Log) Dance(BattleCore core, string? mine = null, string? theirs = null, int myMove = 0, int foeMove = 0)
    {
        var asked = Assert.IsType<ActionRequest>(core.Request).Places;
        var choices = new List<BattleChoice>();
        if (asked.Contains(Mine)) choices.Add(BattleChoice.Fight(Mine, myMove) with { Mask = mine });
        if (asked.Contains(Foe)) choices.Add(BattleChoice.Fight(Foe, foeMove) with { Mask = theirs });
        core.Submit(choices);
        var log = core.TakeLog().ToList();
        return (Lines(log), log);
    }

    // ---------------------------------------------------------------- the table and the items

    [Fact]
    public void TheFourMasksAreInTheDancesOrderEachAnItemOfTheMasksPocketAndAnAbilityThatRuns()
    {
        Assert.Equal(All, WinterMasks.All.Select(m => m.Id));
        foreach (var mask in WinterMasks.All)
        {
            var item = ItemDatabase.Get(mask.Item);
            Assert.NotNull(item);
            Assert.Equal(ItemPocket.Masks, item!.Pocket);
            Assert.Equal(0, item.Price);
            // Never held: no hold effect, so the golden battles' draw of held items never meets one
            Assert.Null(item.HoldEffect);
            Assert.False(PokemonPlatinumEngine.UI.BagScreen.CanGive(item));
            Assert.NotNull(AbilityDatabase.Get(mask.Ability));
            Assert.Same(mask, WinterMasks.OfItem(mask.Item));
            Assert.True(PokemonPlatinumEngine.Core.Text.Has($"masks.{mask.Id}.role"));
        }
        Assert.Equal(WinterMasks.StagStats.Count, WinterMasks.StagRounds);
    }

    // ---------------------------------------------------------------- putting it on

    [Fact]
    public void AMaskPutOnSaysSoAndGivesItsSecondTypeAndAbility()
    {
        var bidoof = Mon("Bidoof", 30, "Splash");
        string before = bidoof.AbilityName!;
        var core = Masked(new[] { bidoof }, new[] { Mon("Snorlax", 50, "Splash") }, myMasks: All);
        var (said, log) = Dance(core, mine: WinterMasks.Bear);
        InOrder(said, "Lucas put the Bear Mask on Bidoof!", "Bidoof dances as the bear, with Fur Coat!");
        var worn = Assert.Single(log.OfType<Said>().SelectMany(s => s.Shows).OfType<MaskWorn>());
        Assert.Equal((Mine, WinterMasks.Bear), (worn.Place, worn.Mask));
        Assert.Equal(WinterMasks.Bear, bidoof.WornMask);
        Assert.Equal("Fur Coat", bidoof.AbilityName);
        Assert.Equal(new[] { PokemonType.Normal, PokemonType.Ground }, core.At(Mine).Types);
        Assert.NotEqual(before, bidoof.AbilityName);
    }

    [Fact]
    public void AMaskOfAPokemonsOwnFirstTypeLeavesItOfThatTypeAlone()
    {
        // Diglett is Ground alone: the bear's Ground in place of its second type leaves it Ground alone
        var core = Masked(new[] { Mon("Diglett", 30, "Splash") }, new[] { Mon("Snorlax", 50, "Splash") }, myMasks: All);
        Dance(core, mine: WinterMasks.Bear);
        Assert.Equal(new[] { PokemonType.Ground }, core.At(Mine).Types);
    }

    [Fact]
    public void TheMaskLastsThroughASwitchAndComesOffWithTheBattle()
    {
        var bidoof = Mon("Bidoof", 30, "Tackle", "Splash");
        string before = bidoof.AbilityName!;
        var core = Masked(new[] { bidoof, Mon("Starly", 30, "Tackle") }, new[] { Mon("Magikarp", 5, "Splash") }, myMasks: All);
        Dance(core, mine: WinterMasks.Stag, myMove: 1);
        SwitchTo(core, 1);
        Assert.Equal(WinterMasks.Stag, bidoof.WornMask);
        Assert.Equal("Sap Sipper", bidoof.AbilityName);
        SwitchTo(core, 0);
        Assert.Equal(new[] { PokemonType.Normal, PokemonType.Grass }, core.At(Mine).Types);
        for (int turn = 0; turn < 10 && core.Result == BattleResult.None; turn++) Turn(core);
        Assert.Equal(BattleResult.PlayerVictory, core.Result);
        Assert.Null(bidoof.WornMask);
        Assert.Equal(before, bidoof.AbilityName);
    }

    [Fact]
    public void ATrainerMasksOnePokemonABattle()
    {
        var core = Masked(new[] { Mon("Bidoof", 30, "Splash"), Mon("Starly", 30, "Splash") }, new[] { Mon("Snorlax", 50, "Splash") }, myMasks: All);
        Assert.Equal(All, core.MasksToWear(core.At(Mine)));
        Dance(core, mine: WinterMasks.Goat);
        Assert.Empty(core.MasksToWear(core.At(Mine)));
        Assert.Equal(PokemonPlatinumEngine.Core.Text.Of("battle.maskOnlyOnce"), core.WhyNotMask(core.At(Mine), WinterMasks.Stag));
        SwitchTo(core, 1);
        Assert.NotNull(core.WhyNotMask(core.At(Mine), WinterMasks.Stag));
        Assert.Throws<ArgumentException>(() => core.Submit(BattleChoice.Fight(Mine, 0) with { Mask = WinterMasks.Stag }, BattleChoice.Fight(Foe, 0)));
    }

    [Fact]
    public void WithNoMaskOrUnderPlatinumsRulesOrWithNoLaterMechanicsNoMaskIsWorn()
    {
        var none = Masked(new[] { Mon("Bidoof", 30, "Splash") }, new[] { Mon("Snorlax", 50, "Splash") });
        Assert.NotNull(none.WhyNotMask(none.At(Mine), WinterMasks.Bear));
        var notThisOne = Masked(new[] { Mon("Bidoof", 30, "Splash") }, new[] { Mon("Snorlax", 50, "Splash") }, myMasks: new[] { WinterMasks.Goat });
        Assert.NotNull(notThisOne.WhyNotMask(notThisOne.At(Mine), WinterMasks.Bear));
        Assert.Null(notThisOne.WhyNotMask(notThisOne.At(Mine), WinterMasks.Goat));
        var platinum = Masked(new[] { Mon("Bidoof", 30, "Splash") }, new[] { Mon("Snorlax", 50, "Splash") }, myMasks: All, rules: Ruleset.Platinum);
        Assert.NotNull(platinum.WhyNotMask(platinum.At(Mine), WinterMasks.Bear));
        var barred = Masked(new[] { Mon("Bidoof", 30, "Splash") }, new[] { Mon("Snorlax", 50, "Splash") }, myMasks: All, run: new RunTerms(NoLaterMechanics: true));
        Assert.NotNull(barred.WhyNotMask(barred.At(Mine), WinterMasks.Bear));
        Assert.Throws<ArgumentException>(() => barred.Submit(BattleChoice.Fight(Mine, 0) with { Mask = WinterMasks.Bear }, BattleChoice.Fight(Foe, 0)));
    }

    [Fact]
    public void ABattleWithNoMaskWornWritesTheSameLogAsOneWithNoMaskHeld()
    {
        string Play(string[]? masks)
        {
            var core = Masked(new[] { Mon("Bidoof", 30, "Tackle") }, new[] { Mon("Starly", 28, "Tackle") }, myMasks: masks);
            for (int turn = 0; turn < 12 && core.Result == BattleResult.None; turn++) Dance(core);
            return string.Join('\n', Golden.BattleText.Written(core));
        }
        Assert.Equal(Play(null), Play(All));
    }

    // ---------------------------------------------------------------- the roles

    [Fact]
    public void TheBearDrawsTheFoesMovesMeantForItsAlly()
    {
        var bear = Mon("Bidoof", 30, "Splash");
        var ally = Mon("Starly", 30, "Splash");
        var core = Masked(new[] { bear, ally }, new[] { Mon("Machop", 30, "Karate Chop"), Mon("Magikarp", 30, "Splash") }, myMasks: All, doubles: true);
        int bearHp = bear.CurrentHP, allyHp = ally.CurrentHP;
        core.Submit(BattleChoice.Fight(Mine, 0) with { Mask = WinterMasks.Bear }, BattleChoice.Fight(Mine2, 0),
            BattleChoice.Fight(Foe, 0, Mine2), BattleChoice.Fight(Foe2, 0));
        core.TakeLog();
        Assert.Equal(allyHp, ally.CurrentHP);
        Assert.True(bear.CurrentHP < bearHp);
        // And on the next turn too
        bearHp = bear.CurrentHP;
        core.Submit(BattleChoice.Fight(Mine, 0), BattleChoice.Fight(Mine2, 0), BattleChoice.Fight(Foe, 0, Mine2), BattleChoice.Fight(Foe2, 0));
        Assert.Equal(allyHp, ally.CurrentHP);
        Assert.True(bear.CurrentHP < bearHp);
    }

    [Fact]
    public void TheGoatsAttackTakesTheTargetsRaisedStats()
    {
        var goat = Mon("Bidoof", 30, "Tackle", "Splash");
        var foe = Mon("Snorlax", 50, "Swords Dance", "Splash");
        var core = Masked(new[] { goat }, new[] { foe }, myMasks: All);
        Dance(core, mine: WinterMasks.Goat, myMove: 1);
        Assert.Equal(2, foe.StatStages.GetValueOrDefault(StatType.Attack));
        var (said, _) = Dance(core, myMove: 0, foeMove: 1);
        Assert.Contains(said, l => l.StartsWith("Bidoof stole"));
        Assert.Equal(0, foe.StatStages.GetValueOrDefault(StatType.Attack));
        Assert.Equal(2, goat.StatStages.GetValueOrDefault(StatType.Attack));
    }

    [Fact]
    public void TheHorseCarriesItsWearerOutOnceItsMoveHasLanded()
    {
        var horse = Mon("Bidoof", 30, "Tackle");
        var core = Masked(new[] { horse, Mon("Starly", 30, "Tackle") }, new[] { Mon("Snorlax", 50, "Splash") }, myMasks: All);
        Dance(core, mine: WinterMasks.Horse);
        // Its move landed: the player is asked who comes in, in the middle of the turn
        Assert.Equal(Mine, Assert.IsType<ReplacementRequest>(core.Request).Place);
        SendIn(core, 1);
        Assert.Equal("Starly", core.At(Mine).Pokemon!.Species.Name);
        Assert.Equal(WinterMasks.Horse, horse.WornMask);
        Assert.Equal("Regenerator", horse.AbilityName);
    }

    [Fact]
    public void TheHorseWithNobodyToCarryInStaysAndAStatusMoveCarriesNoOne()
    {
        var core = Masked(new[] { Mon("Bidoof", 30, "Tackle", "Splash") }, new[] { Mon("Snorlax", 50, "Splash") }, myMasks: All);
        Dance(core, mine: WinterMasks.Horse);
        Assert.IsType<ActionRequest>(core.Request);
        var two = Masked(new[] { Mon("Bidoof", 30, "Splash"), Mon("Starly", 30, "Tackle") }, new[] { Mon("Snorlax", 50, "Splash") }, myMasks: All);
        Dance(two, mine: WinterMasks.Horse);
        Assert.IsType<ActionRequest>(two.Request);
    }

    [Fact]
    public void TheStagRaisesItsSideAStatATurnFiveTimes()
    {
        var stag = Mon("Bidoof", 30, "Splash");
        var core = Masked(new[] { stag }, new[] { Mon("Snorlax", 50, "Splash") }, myMasks: All);
        var (said, _) = Dance(core, mine: WinterMasks.Stag);
        Assert.Contains("Bidoof leads the dance!", said);
        Assert.Equal(1, stag.StatStages.GetValueOrDefault(StatType.Attack));
        for (int turn = 0; turn < 6; turn++) Dance(core);
        foreach (var stat in WinterMasks.StagStats) Assert.Equal(1, stag.StatStages.GetValueOrDefault(stat));
    }

    [Fact]
    public void AMaskedPokemonUsesNoOtherMechanicAndAMaskGoesWithNone()
    {
        var core = Masked(new[] { Mon("Bidoof", 30, "Splash") }, new[] { Mon("Snorlax", 50, "Splash") }, myMasks: All);
        Assert.Throws<ArgumentException>(() => core.Submit(BattleChoice.Fight(Mine, 0) with { Mask = WinterMasks.Bear, Terastallize = true }, BattleChoice.Fight(Foe, 0)));
        Dance(core, mine: WinterMasks.Bear);
        Assert.NotNull(core.WhyNotTera(core.At(Mine)));
        Assert.NotNull(core.WhyNotDynamax(core.At(Mine)));
    }

    // ---------------------------------------------------------------- the trainer AI

    [Fact]
    public void TheAiMasksItsLastPokemonWithTheMaskItsMomentWants()
    {
        var core = Masked(new[] { Mon("Snorlax", 50, "Splash") }, new[] { Mon("Bidoof", 30, "Tackle") }, theirMasks: new[] { WinterMasks.Horse, WinterMasks.Stag }, ai: true);
        Assert.Equal(WinterMasks.Stag, TrainerAi.MaskNow(core, core.At(Foe)));
        core.Submit(BattleChoice.Fight(Mine, 0));
        Assert.Equal(WinterMasks.Stag, core.At(Foe).Pokemon!.WornMask);
        // Raised stats across the field want the goat
        var goat = Masked(new[] { Mon("Snorlax", 50, "Swords Dance") }, new[] { Mon("Bidoof", 30, "Tackle") }, myMasks: null, theirMasks: All);
        Dance(goat);
        Assert.Equal(WinterMasks.Goat, TrainerAi.MaskNow(goat, goat.At(Foe)));
    }

    [Fact]
    public void TheAiKeepsItsMaskForItsAceOrItsLast()
    {
        // Bidoof is neither the ace (Staravia is of a higher level) nor the last
        var core = Masked(new[] { Mon("Snorlax", 50, "Splash") }, new[] { Mon("Bidoof", 30, "Tackle"), Mon("Staravia", 35, "Tackle") }, theirMasks: All);
        Assert.Null(TrainerAi.MaskNow(core, core.At(Foe)));
    }

    // ---------------------------------------------------------------- the record and the screen

    [Fact]
    public void ARecordedBattleWithAMaskReplaysTheSameItsFileKeepsThemAndTheGamesOwnPokemonGetsItsAbilityBack()
    {
        var mine = new Party();
        var bidoof = Mon("Bidoof", 30, "Tackle", "Splash");
        string before = bidoof.AbilityName!;
        mine.Add(bidoof);
        var trainer = new Trainer { Id = "mask_test", Name = "Ilinca", TrainerClass = "Ace Trainer", PrizeMoney = 64, Ai = PokemonPlatinumEngine.Battle.Sim.Ai.AiFlags.Basic, Masks = new List<string> { WinterMasks.Stag } };
        trainer.Party.Add(Mon("Starly", 20, "Tackle"));
        var bag = new Inventory();
        bag.AddItem(ItemDatabase.Get("Bear Mask")!, 1);
        bag.AddItem(ItemDatabase.Get("Goat Mask")!, 1);
        var battle = new BattleEngine(new BattleSetup
        {
            PlayerParty = mine, Inventory = bag, Pokedex = new Pokedex(), Trainers = { trainer },
            Random = new BattleRandom(46), Rules = Modern, PlayerName = "Lucas"
        });
        Settle(battle);
        Assert.Equal(new[] { WinterMasks.Bear, WinterMasks.Goat }, battle.Core.PlayerMasks);
        Assert.True(battle.CanWearMaskNow);
        Assert.Equal(WinterMasks.Bear, battle.MaskOffered);
        Assert.True(battle.ToggleMask());
        Assert.True(battle.ToggleMask());
        Assert.Equal(WinterMasks.Goat, battle.MaskArmed);
        battle.SelectMove(0);
        Settle(battle);
        Assert.Equal(WinterMasks.Goat, bidoof.WornMask);
        Assert.Equal("Prankster", bidoof.AbilityName);
        for (int turn = 0; turn < 40 && !battle.IsBattleOver; turn++)
        {
            if (battle.IsChoosingReplacement) battle.SelectSwitch(0);
            else battle.SelectMove(0);
            Settle(battle);
        }
        Assert.True(battle.IsBattleOver);
        Assert.Null(bidoof.WornMask);
        Assert.Equal(before, bidoof.AbilityName);
        Assert.Contains(battle.Core.Record!.Answers.SelectMany(a => a), c => c.Mask == WinterMasks.Goat);

        var file = battle.RecordFile(new DateTime(2026, 11, 28))!;
        Assert.Equal(new[] { WinterMasks.Bear, WinterMasks.Goat }, file.Masks);
        Assert.Equal(new[] { WinterMasks.Stag }, file.Trainers.Single().Masks);
        string text = file.Serialize();
        var read = BattleRecordFile.Read(text);
        Assert.Equal(ReplayRefusal.None, read.Refused);
        Assert.Equal(text, read.File!.Serialize());
        var again = read.File.Replay();
        Assert.Equal(battle.Result, again.Result);
        Assert.Equal(Golden.BattleText.Written(battle.Core), Golden.BattleText.Written(again));
    }

    [Fact]
    public void WithNoMaskInTheBagTheMenuOffersNone()
    {
        var mine = new Party();
        mine.Add(Mon("Bidoof", 30, "Tackle"));
        var battle = new BattleEngine(new BattleSetup
        {
            PlayerParty = mine, Inventory = new Inventory(), Pokedex = new Pokedex(), WildPokemon = new List<Pokemon> { Mon("Starly", 5, "Tackle") },
            Random = Steady(), Rules = Modern, PlayerName = "Lucas"
        });
        Settle(battle);
        Assert.Null(battle.Core.PlayerMasks);
        Assert.False(battle.CanWearMaskNow);
        Assert.False(battle.ToggleMask());
    }

    // ---------------------------------------------------------------- the bag and the kit

    [Fact]
    public void TheBagShowsTheMasksPocketOnlyWhileOneIsHeld()
    {
        var bag = new PokemonPlatinumEngine.UI.BagScreen();
        Assert.DoesNotContain(ItemPocket.Masks, bag.Pockets);
        bag.ShowsMasks = true;
        Assert.Equal(ItemPocket.Masks, bag.Pockets[^1]);
        bag.CurrentPocket = ItemPocket.KeyItems;
        bag.MovePocket(1);
        Assert.Equal(ItemPocket.Masks, bag.CurrentPocket);
        bag.ShowsMasks = false;
        bag.CurrentPocket = ItemPocket.KeyItems;
        bag.MovePocket(1);
        Assert.Equal(ItemPocket.Items, bag.CurrentPocket);
    }

    [Fact]
    public void AMasksRecipeWaitsForItsPatternAndMakesTheMaskOnceLearned()
    {
        Assert.Equal(new[] { "mask_bear", "mask_goat", "mask_horse", "mask_stag" }, Crafting.Patterns);
        var recipe = Crafting.Recipes.Single(r => r.Makes == "Bear Mask");
        var state = new CraftingState();
        foreach (var need in recipe.Takes) state.Pouch.Add(need.Material!, need.Count);
        var bag = new Inventory();
        Assert.False(Crafting.Known(recipe, state));
        Assert.NotNull(Crafting.WhyNot(recipe, 1, bag, state.Pouch, state));
        Assert.False(Crafting.Make(recipe, 1, bag, state.Pouch, state).Made);
        Assert.True(state.Learn("mask_bear"));
        Assert.False(state.Learn("mask_bear"));
        Assert.True(Crafting.Make(recipe, 1, bag, state.Pouch, state).Made);
        Assert.Equal(1, bag.GetQuantity(ItemDatabase.Get("Bear Mask")!));
        // The pattern is kept with the kit's save
        var again = new CraftingState();
        again.Load(state.ToSave());
        Assert.Equal(new[] { "mask_bear" }, again.Patterns);
    }

    private const string Workshop = """
        script Ilie
          craft knows "mask_bear"
          setvar VAR_FIRST RESULT
          craft learn "mask_bear"
          setvar VAR_SECOND RESULT
          craft learn "mask_bear"
          setvar VAR_THIRD RESULT
          craft knows "mask_bear"
          setvar VAR_FOURTH RESULT
          end
        """;

    [Fact]
    public void CraftLearnTeachesAPatternOnceAndCraftKnowsAsksIt()
    {
        var library = PokemonPlatinumEngine.Story.ScriptLibrary.FromSources(("test", Workshop), ("common", ""));
        var host = new PokemonPlatinumEngine.Story.HeadlessScriptHost();
        var runner = new PokemonPlatinumEngine.Story.ScriptRunner(library, host);
        runner.Start(library.All.First(s => s.File == "test"));
        runner.RunToEnd();
        Assert.Empty(host.Problems);
        Assert.Equal((0, 1, 0, 1), (host.Story.Var("VAR_FIRST"), host.Story.Var("VAR_SECOND"), host.Story.Var("VAR_THIRD"), host.Story.Var("VAR_FOURTH")));
        Assert.Contains("mask_bear", host.Crafting!.Patterns);
    }

    [Fact]
    public void APatternThatIsNoPatternOfTheKitIsAMistakeOfItsLine()
    {
        var error = Assert.ThrowsAny<Exception>(() => PokemonPlatinumEngine.Story.ScriptLibrary.FromSources(("test", "script X\n  craft learn \"mask_wolf\"\n  end\n"), ("common", "")));
        Assert.Contains("mask_wolf", error.Message);
    }
}
