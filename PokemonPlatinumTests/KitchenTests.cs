using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumTests;

/// <summary>
/// Materials, crafting and cooking, CR6 (plan 39; plan 10 · F8 taken over): cooking at the camp and the powers its dishes
/// start. A pot's dish is the recipe its ingredients hold, and the plain dish otherwise; the grade is a pure function of
/// the pot's seed and the frames pressed on, widened by the timing assist; the team is healed by the grade, cured and fed
/// once a visit; the dish's power starts, is read where it bites and wears off by the play time; with no dish eaten every
/// roll is as before; the camp is offered with the kit while the FOLLOWER row is off and not before; and the pantry's
/// counter takes money and fills the pouch.
/// </summary>
[Collection("Shared clocks and dice")]
public class KitchenTests
{
    private static KitchenIngredient Good(string id) => new(id, true);
    private static KitchenIngredient Thing(string name) => new(name, false);

    // ---------------------------------------------------------------- the table

    [Fact]
    public void TheKitchensTableKeepsItsRules()
    {
        var problems = Meals.Problems(Meals.Table).ToList();
        Assert.True(problems.Count == 0, string.Join("; ", problems.Select(p => $"{p.Name}: {p.What}")));
        Assert.Equal(10, MaterialTable.All.Count(m => m.Kind == MaterialKind.Pantry));
        // Every power a dish starts is one something reads
        Assert.All(Meals.Table.Dishes.Where(d => d.Power != null), d => Assert.Contains(d.Power!.Value, PowerTable.Read));
        // The check finds a dish that wants what no pantry sells (since plan 36 · TR7 every kind has a reader, so no dish
        // can start a power nothing reads: PowerTable.Read is every kind)
        Assert.Equal(Enum.GetValues<PowerKind>().Length, PowerTable.Read.Count);
        var broken = new KitchenRecipes
        {
            Plain = "campStew", Grades = Meals.Table.Grades,
            Dishes = { new KitchenDish { Id = "campStew" }, new KitchenDish { Id = "expStew", Power = PowerKind.Exp, Needs = { new KitchenNeed { Material = "softFluff" } } } }
        };
        var found = Meals.Problems(broken).ToList();
        Assert.Contains(found, p => p.Name == "expStew" && p.What.Contains("no good of the pantry"));
    }

    [Fact]
    public void ADishComesFromItsIngredientsAndThePlainDishFromAnyOthers()
    {
        Assert.Equal("beanHotpot", Meals.DishFor(new[] { Good("onions"), Good("beans"), Good("potatoes") }).Id);
        // More wins over fewer: potatoes and butter alone are a hash; with beans and onions too, the hotpot
        Assert.Equal("potatoHash", Meals.DishFor(new[] { Good("potatoes"), Good("butter") }).Id);
        Assert.Equal("beanHotpot", Meals.DishFor(new[] { Good("potatoes"), Good("butter"), Good("beans"), Good("onions") }).Id);
        Assert.Equal("berryPilaf", Meals.DishFor(new[] { Good("rice"), Thing("Cheri Berry") }).Id);
        Assert.Equal("honeyOatCakes", Meals.DishFor(new[] { Good("oats"), Thing("Honey") }).Id);
        Assert.Equal("mushroomBake", Meals.DishFor(new[] { Good("cheese"), Thing("Big Mushroom"), Good("butter") }).Id);
        Assert.Equal("campStew", Meals.DishFor(new[] { Good("rice") }).Id);
        Assert.Equal("campStew", Meals.DishFor(new[] { Thing("Oran Berry"), Good("cheese") }).Id);
        // A need is met once: two of them want two things
        Assert.Equal("campStew", Meals.DishFor(new[] { Good("beans") }).Id);
        // The pilaf's type is its first berry's Natural Gift's
        Assert.Equal(ItemDatabase.Get("Cheri Berry")!.NaturalGiftType, Meals.TypeFor(new[] { Good("rice"), Thing("Cheri Berry"), Thing("Oran Berry") }));
        Assert.Null(Meals.TypeFor(new[] { Good("rice") }));
    }

    [Fact]
    public void WhatIsInReachIsThePantrysGoodsAndTheBagsBerriesHoneyAndMushrooms()
    {
        var pouch = new MaterialPouch();
        pouch.Add("rice", 2);
        pouch.Add("softFluff", 9);
        var bag = new Inventory();
        bag.AddItem(ItemDatabase.Get("Oran Berry")!, 3);
        bag.AddItem(ItemDatabase.Get("Honey")!, 1);
        bag.AddItem(ItemDatabase.Get("Potion")!, 1);
        bag.AddItem(ItemDatabase.Get("Tiny Mushroom")!, 1);
        var reach = Meals.OnHand(pouch, bag);
        Assert.Equal(Good("rice"), reach[0]);
        Assert.DoesNotContain(Good("softFluff"), reach);
        Assert.Contains(Thing("Oran Berry"), reach);
        Assert.Contains(Thing("Honey"), reach);
        Assert.Contains(Thing("Tiny Mushroom"), reach);
        Assert.DoesNotContain(Thing("Potion"), reach);
    }

    // ---------------------------------------------------------------- the pot

    private static Kitchen Cook(uint seed, TimingAssist assist, Func<Kitchen, bool> press)
    {
        var pot = new Kitchen(seed, assist);
        for (int frame = 0; frame < 5000 && !pot.Done; frame++) pot.Step(press(pot));
        Assert.True(pot.Done);
        return pot;
    }

    [Fact]
    public void TheGradeIsAPureFunctionOfTheSeedAndThePresses()
    {
        // Pressed in the middle of the zone, every round is perfect, and the dish superb
        var best = Cook(7, TimingAssist.Off, p => MathF.Abs(p.Needle - p.Zone) <= p.HalfWindow / 4f);
        Assert.All(best.Scores, s => Assert.Equal(StirScore.Perfect, s));
        Assert.Equal(DishGrade.Superb, best.Grade);
        // Never pressed: every round runs out, and the dish is plain
        var none = Cook(7, TimingAssist.Off, _ => false);
        Assert.All(none.Scores, s => Assert.Equal(StirScore.Miss, s));
        Assert.Equal(Kitchen.Rounds * Kitchen.RoundFrames, none.Frame);
        Assert.Equal(DishGrade.Plain, none.Grade);
        // Pressed at once, before the needle has come anywhere near: misses
        var early = Cook(7, TimingAssist.Off, p => p.RoundFrame == 0 && p.Zone > 0.3f);
        Assert.Contains(StirScore.Miss, early.Scores);

        // The same seed and the same presses give the same pot, frame for frame; another seed moves the zone
        var a = Cook(99, TimingAssist.Off, p => p.Frame % 17 == 3);
        var b = Cook(99, TimingAssist.Off, p => p.Frame % 17 == 3);
        Assert.Equal(a.Scores, b.Scores);
        Assert.Equal(a.Frame, b.Frame);
        Assert.NotEqual(new Kitchen(1, TimingAssist.Off).Zone, new Kitchen(2, TimingAssist.Off).Zone);

        // The grades' steps
        Assert.Equal(DishGrade.Plain, Kitchen.GradeOf(2));
        Assert.Equal(DishGrade.Good, Kitchen.GradeOf(3));
        Assert.Equal(DishGrade.Great, Kitchen.GradeOf(5));
        Assert.Equal(DishGrade.Superb, Kitchen.GradeOf(7));
    }

    [Fact]
    public void TheTimingAssistWidensTheZoneOrStirsForThePlayer()
    {
        Assert.Equal(Kitchen.Half * 2f, new Kitchen(3, TimingAssist.Generous).HalfWindow);
        Assert.Equal(Kitchen.Half, new Kitchen(3, TimingAssist.Off).HalfWindow);
        // Automatic stirs as the needle comes into the zone: never a miss, never pressed by the player
        var automatic = Cook(11, TimingAssist.Automatic, _ => false);
        Assert.DoesNotContain(StirScore.Miss, automatic.Scores);
        Assert.True(automatic.Grade >= DishGrade.Good);
    }

    // ---------------------------------------------------------------- the meal

    private static Party Team()
    {
        var party = new Party();
        party.Add(new Pokemon(PokemonDatabase.Get("Starly")!, 20));
        party.Add(new Pokemon(PokemonDatabase.Get("Bidoof")!, 20));
        party.Add(new Pokemon(PokemonDatabase.Get("Shinx")!, 20));
        return party;
    }

    [Fact]
    public void TheTeamIsHealedByTheGradeCuredAndFedOnceAVisit()
    {
        var party = Team();
        var (starly, bidoof, shinx) = (party.Members[0], party.Members[1], party.Members[2]);
        starly.CurrentHP = 1;
        bidoof.Status = StatusCondition.Poison;
        shinx.CurrentHP = 0;
        shinx.Status = StatusCondition.Faint;
        int friendship = starly.Friendship;
        var visit = new CampVisit();
        var powers = new PowerState();

        var served = Meals.Serve(party, Meals.Dish("beanHotpot")!, DishGrade.Great, powers, visit, null);
        Assert.Equal(1 + starly.MaxHP * 75 / 100, starly.CurrentHP);
        Assert.Equal(StatusCondition.None, bidoof.Status);
        Assert.Equal(0, shinx.CurrentHP); // a fainted Pokémon isn't brought round
        Assert.Equal(StatusCondition.Faint, shinx.Status);
        Assert.Equal(1, served.Cured);
        Assert.True(starly.Friendship > friendship);
        Assert.Equal(3, served.Fed);

        // A second meal in the visit heals and starts its power, and befriends nobody again
        int after = starly.Friendship;
        var again = Meals.Serve(party, Meals.Dish("campStew")!, DishGrade.Superb, powers, visit, null);
        Assert.Equal(starly.MaxHP, starly.CurrentHP);
        Assert.Equal(after, starly.Friendship);
        Assert.Equal(0, again.Fed);
        Assert.Null(again.Power); // the plain dish starts nothing
        Assert.True(powers.Has(PowerKind.Encounter)); // and leaves the hotpot's in force
    }

    [Fact]
    public void ADishStartsItsPowerByTheGradeAndItWearsOffByThePlayTime()
    {
        var powers = new PowerState();
        var served = Meals.Serve(Team(), Meals.Dish("beanHotpot")!, DishGrade.Superb, powers, null, null);
        var row = Meals.GradeRow(DishGrade.Superb);
        Assert.Equal(PowerKind.Encounter, served.Power!.Kind);
        Assert.Equal(row.Level, powers.LevelOf(PowerKind.Encounter));
        Assert.Equal(PowerTable.Strength(PowerKind.Encounter, row.Level), powers.Strength(PowerKind.Encounter));
        Assert.Equal(row.Seconds, powers.SecondsLeft(PowerKind.Encounter));

        // Stealth puts the encounters' out, and the reverse
        Meals.Serve(Team(), Meals.Dish("quietBroth")!, DishGrade.Plain, powers, null, null);
        Assert.False(powers.Has(PowerKind.Encounter));
        Assert.Equal(Meals.GradeRow(DishGrade.Plain).Level, powers.LevelOf(PowerKind.Stealth));

        // A type's encounters takes the pot's type
        Meals.Serve(Team(), Meals.Dish("berryPilaf")!, DishGrade.Good, powers, null, PokemonType.Fire);
        Assert.Equal(PokemonType.Fire, powers.TypeOf(PowerKind.TypeEncounter));

        // The play time counts it down; it wears off once, and is gone
        float left = powers.SecondsLeft(PowerKind.Stealth);
        Assert.Empty(powers.Tick(left - 1f));
        Assert.True(powers.Has(PowerKind.Stealth));
        Assert.Contains(PowerKind.Stealth, powers.Tick(2f));
        Assert.False(powers.Has(PowerKind.Stealth));
        Assert.Equal(0, powers.Strength(PowerKind.Stealth));
    }

    [Fact]
    public void ThePowersAreSavedAndNullWhenNoneIsInForce()
    {
        var powers = new PowerState();
        Assert.Null(powers.ToSave());
        powers.Grant(PowerKind.TypeEncounter, 2, 300, PokemonType.Water);
        powers.Grant(PowerKind.Materials, 9, 120);
        var saved = powers.ToSave()!;
        var back = new PowerState();
        back.Load(saved);
        Assert.Equal(PokemonType.Water, back.TypeOf(PowerKind.TypeEncounter));
        Assert.Equal(PowerTable.TopLevel, back.LevelOf(PowerKind.Materials));
        Assert.Equal(120f, back.SecondsLeft(PowerKind.Materials));
        // No type for a type's encounters, no time, no level: refused
        Assert.False(back.Grant(PowerKind.TypeEncounter, 1, 60));
        Assert.False(back.Grant(PowerKind.Hatch, 1, 0));
        Assert.False(back.Grant(PowerKind.Hatch, 0, 60));
    }

    // ---------------------------------------------------------------- the readers, and nothing moved without them

    [Fact]
    public void ATypesEncountersTakesTheCommonestSlotsAndDrawsNothing()
    {
        var slots = Enumerable.Range(0, 12).Select(i => new WildEncounterEntry { SpeciesName = i == 5 ? "Shinx" : i == 11 ? "Ponyta" : "Bidoof", Weight = 10 }).ToList();
        EncounterSlots.ToTheType(slots, PokemonType.Electric, 2);
        Assert.Equal(new[] { "Shinx", "Shinx", "Bidoof" }, slots.Take(3).Select(s => s.SpeciesName));
        // A type the place has none of leaves it as it is, and our own slots from 10 are never taken
        var none = Enumerable.Range(0, 12).Select(_ => new WildEncounterEntry { SpeciesName = "Bidoof" }).ToList();
        EncounterSlots.ToTheType(none, PokemonType.Fire, 6);
        Assert.All(none, s => Assert.Equal("Bidoof", s.SpeciesName));
        var ours = Enumerable.Range(0, 12).Select(i => new WildEncounterEntry { SpeciesName = i == 0 ? "Ponyta" : "Bidoof" }).ToList();
        EncounterSlots.ToTheType(ours, PokemonType.Fire, 12);
        Assert.Equal("Bidoof", ours[10].SpeciesName);
        Assert.Equal("Bidoof", ours[11].SpeciesName);
        Assert.Equal("Ponyta", ours[9].SpeciesName);
    }

    [Fact]
    public void TheEncountersPowerMovesAStepsOddsAsTheRadioDoesAndNothingWithout()
    {
        int Met(int bonus)
        {
            var steps = new EncounterSteps();
            var rng = new Random(5);
            int met = 0;
            for (int i = 0; i < 2000; i++) if (steps.Meets(20, false, rng, null, bonus)) met++;
            return met;
        }
        Assert.True(Met(30) > Met(0));
        Assert.True(Met(-30) < Met(0));
        // The moment carries nothing with no power in force
        var moment = new EncounterMoment();
        Assert.Equal(0, moment.PowerRate);
        Assert.Null(moment.PowerType);
    }

    [Fact]
    public void TheHatchingPowerTakesMoreCyclesOffAndNoneWithout()
    {
        Pokemon Egg()
        {
            var egg = new Pokemon(PokemonDatabase.Get("Bidoof")!, 1) { IsEgg = true };
            egg.EggCycles = 10;
            return egg;
        }
        var plain = new Party();
        plain.Add(Egg());
        Breeding.CountDown(plain);
        Assert.Equal(9, plain.Members[0].EggCycles);
        var fed = new Party();
        fed.Add(Egg());
        Breeding.CountDown(fed, PowerTable.Strength(PowerKind.Hatch, 2));
        Assert.Equal(10 - 1 - PowerTable.Strength(PowerKind.Hatch, 2), fed.Members[0].EggCycles);
    }

    [Fact]
    public void TheMaterialsPowerLeavesMoreAndDrawsTheSame()
    {
        var foe = new Pokemon(PokemonDatabase.Get("Starly")!, 10);
        var plain = new Random(42);
        var more = new Random(42);
        var without = MaterialDrops.For(foe, plain);
        var with = MaterialDrops.For(foe, more, 2);
        // The same draws, so the generators stand at the same place after
        Assert.Equal(plain.Next(), more.Next());
        foreach (var m in without.Where(m => !MaterialTable.Get(m.Id)!.Rare))
            Assert.Equal(m.Count + 2, with.First(w => w.Id == m.Id).Count);
        // With no power the leavings are as they always were
        Assert.Equal(MaterialDrops.For(foe, new Random(9)), MaterialDrops.For(foe, new Random(9), 0));
    }

    // ---------------------------------------------------------------- in the game

    private static GameEngine Headless(bool follower)
    {
        Dice.Seed(41);
        var engine = new GameEngine(new GameSettings { Muted = true, Follower = follower });
        engine.InitializeHeadless();
        engine.PeopleStayPut = true;
        engine.NewGameRegion = "Sinnoh";
        engine.StartNewGame();
        var game = engine.Drive;
        StoryMigration.PastTheOpening(game.Story, ScriptLibrary.Default);
        game.State = GameState.Overworld;
        game.TrainersLookOnArrival = false;
        if (game.Party.Count == 0) game.Party.Add(new Pokemon(PokemonDatabase.Get("Turtwig")!, 12));
        return engine;
    }

    private static void Done()
    {
        InputManager.Feed(null);
        Dice.Seed(null);
        Ruleset.Use(RulesPreset.Platinum);
    }

    [Fact]
    public void TheCampIsOfferedWithTheKitWhileTheFollowerRowIsOffAndNotBefore()
    {
        try
        {
            var engine = Headless(follower: false);
            var game = engine.Drive;
            Assert.False(game.HasCamp);
            game.Bag.AddItem(ItemDatabase.Get(GameEngine.CraftingKitItem)!, 1);
            Assert.True(game.HasCamp);
            game.ChooseFromStartMenu(StartMenuChoice.Camp);
            Assert.Equal(GameState.Camp, game.State);
            Assert.True(game.Camp.CanCook);

            // Without the kit the camp (the follower's) cooks nothing
            var follower = Headless(follower: true).Drive;
            follower.ChooseFromStartMenu(StartMenuChoice.Camp);
            Assert.False(follower.Camp.CanCook);
            follower.Camp.AskToCook();
            Assert.Equal(CampPhase.Team, follower.Camp.Phase);
        }
        finally { Done(); }
    }

    [Fact]
    public void ADishCookedAtTheCampHealsTheTeamAndStartsItsPowerForItsLength()
    {
        try
        {
            var engine = Headless(follower: false);
            var game = engine.Drive;
            game.Bag.AddItem(ItemDatabase.Get(GameEngine.CraftingKitItem)!, 1);
            foreach (string good in new[] { "beans", "onions", "potatoes" }) game.Crafting.Pouch.Add(good, 1);
            var lead = game.Party.Members[0];
            lead.CurrentHP = 1;
            game.ChooseFromStartMenu(StartMenuChoice.Camp);
            var camp = game.Camp;

            camp.AskToCook();
            Assert.Equal(CampPhase.Pantry, camp.Phase);
            // The pantry's goods come first, in the table's order
            Assert.Equal(new[] { Good("beans"), Good("potatoes"), Good("onions") }, camp.OnHand.Take(3));
            void To(Func<CampScreen, bool> where)
            {
                for (int i = 0; i <= camp.OnHand.Count && !where(camp); i++) camp.PantryMove(1);
                Assert.True(where(camp));
            }
            foreach (string good in new[] { "beans", "onions", "potatoes" })
            {
                To(c => !c.OnCook && c.OnHand[c.PantryCursor] == Good(good));
                camp.PantryConfirm();
            }
            Assert.Equal(3, camp.Pot.Count);
            Assert.Equal("beanHotpot", camp.Expected.Id);
            // A second of the potatoes isn't held
            camp.PantryConfirm();
            Assert.Equal(3, camp.Pot.Count);
            // B takes the last out, and A puts it back
            camp.PantryCancel();
            Assert.Equal(2, camp.Pot.Count);
            camp.PantryConfirm();
            To(c => c.OnCook);
            camp.PantryConfirm();
            Assert.Equal(CampPhase.Stirring, camp.Phase);

            // Taking the pot off uses nothing
            camp.TakeOff();
            Assert.Equal(CampPhase.Pantry, camp.Phase);
            Assert.Equal(1, game.Crafting.Pouch.Count("beans"));
            camp.PantryConfirm();
            Assert.Equal(CampPhase.Stirring, camp.Phase);

            // Stirred in the middle of the zone every round, a frame at a time
            while (camp.Phase == CampPhase.Stirring)
            {
                var pot = camp.Stirring!;
                camp.StepPot(MathF.Abs(pot.Needle - pot.Zone) <= pot.HalfWindow / 4f);
            }
            Assert.Equal(CampPhase.Served, camp.Phase);
            Assert.Equal(DishGrade.Superb, camp.Served!.Grade);
            Assert.Equal(lead.MaxHP, lead.CurrentHP);
            Assert.Equal(0, game.Crafting.Pouch.Count("beans"));
            Assert.True(camp.Visit.WasFed(lead));
            var row = Meals.GradeRow(DishGrade.Superb);
            Assert.Equal(row.Level, game.Powers.LevelOf(PowerKind.Encounter));
            camp.ServedConfirm();
            Assert.Equal(CampPhase.Team, camp.Phase);

            // The step's moment carries the power, and the play time wears it off with one line on the toast
            camp.Cancel();
            InputManager.Feed(ActionFrame.None);
            engine.Update(1f / 60f);
            Assert.Equal(GameState.Overworld, game.State);
            Assert.True(game.Powers.SecondsLeft(PowerKind.Encounter) < row.Seconds);
            InputManager.Feed(ActionFrame.None);
            engine.Update(row.Seconds);
            Assert.False(game.Powers.Has(PowerKind.Encounter));
            InputManager.Feed(ActionFrame.None);
            engine.Update(1f / 60f);
            Assert.Contains(PowerTable.NameOf(PowerKind.Encounter), game.Toast.Message);
        }
        finally { Done(); }
    }

    [Fact]
    public void ThePantrysCounterTakesMoneyAndFillsThePouch()
    {
        try
        {
            var engine = Headless(follower: false);
            var game = engine.Drive;
            game.Bag.AddItem(ItemDatabase.Get(GameEngine.CraftingKitItem)!, 1);
            game.Money = 1000;
            game.OpenCraftingAt(CraftingPlace.Pantry);
            var screen = game.CraftingScreen;
            Assert.Equal(CraftingTab.Pantry, screen.Tab);
            Assert.Equal(new[] { CraftingTab.Materials, CraftingTab.Make, CraftingTab.Pantry, CraftingTab.Crosses }, screen.Tabs);
            var good = screen.SelectedGood!;
            screen.Confirm();
            screen.ChangeAmount(2);
            Assert.Equal(3, screen.HowMany);
            screen.Confirm();
            Assert.Equal(3, game.Crafting.Pouch.Count(good.Id));
            InputManager.Feed(ActionFrame.None);
            engine.Update(1f / 60f);
            Assert.Equal(1000 - 3 * good.Worth, game.Money);
            // The purse stops the number
            screen.Confirm();
            screen.ChangeAmount(999);
            Assert.Equal((1000 - 3 * good.Worth) / good.Worth, screen.HowMany);
        }
        finally { Done(); }
    }

    // ---------------------------------------------------------------- the script's words

    [Fact]
    public void TheScriptsWordsOpenThePantryAndServeAMeal()
    {
        var host = new HeadlessScriptHost();
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        // No kit: no pantry
        runner.Start(ScriptParser.Parse("test", "script T\n  craft pantry\n  end\n").First(), null);
        runner.RunToEnd();
        Assert.Equal(0, runner.Result);
        Assert.DoesNotContain(host.Log, l => l.Contains("Crafting pantry"));
        host.Bag.AddItem(ItemDatabase.Get(GameEngine.CraftingKitItem)!, 1);
        runner.Start(ScriptParser.Parse("test", "script T\n  craft pantry\n  end\n").First(), null);
        runner.RunToEnd();
        Assert.Contains(host.Log, l => l.Contains("open Crafting pantry"));

        host.Party.Add(new Pokemon(PokemonDatabase.Get("Bidoof")!, 10));
        host.Party.Members[0].CurrentHP = 1;
        runner.Start(ScriptParser.Parse("test", "script T\n  craft meal \"mushroomBake\" superb\n  end\n").First(), null);
        runner.RunToEnd();
        Assert.Equal(1, runner.Result);
        Assert.Equal(host.Party.Members[0].MaxHP, host.Party.Members[0].CurrentHP);
        Assert.Equal(PowerTable.TopLevel, host.Powers!.LevelOf(PowerKind.Materials));

        // A dish the kitchen doesn't have is a mistake of the script
        Assert.ThrowsAny<Exception>(() => ScriptParser.Parse("test", "script T\n  craft meal \"roastBoot\"\n  end\n"));
    }
}
