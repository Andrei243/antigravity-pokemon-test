using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;
using static PokemonPlatinumTests.Scenario;

namespace PokemonPlatinumTests;

/// <summary>
/// Materials, crafting and cooking, CR4 (plan 39, decision 4): the TM maker. A machine is known from its first time in
/// the bag (and an older save knows those in its bag), its cost follows its move's type and power, a made machine goes
/// into the bag with the materials and money taken (and is refused short of either), no HM is listed, a made machine
/// teaches and is used up as a found one does, and the PC offers the maker with the kit and not without.
/// </summary>
[Collection("Shared clocks and dice")]
public class TmMakerTests
{
    private static ItemData Item(string name) => ItemDatabase.Get(name)!;

    private static Inventory BagWith(params string[] names)
    {
        var bag = new Inventory();
        foreach (var name in names) bag.AddItem(Item(name), 1);
        return bag;
    }

    private static MaterialPouch Pouch(params (string Id, int N)[] held)
    {
        var pouch = new MaterialPouch();
        foreach (var (id, n) in held) pouch.Add(id, n);
        return pouch;
    }

    // ---------------------------------------------------------------- what is known

    [Fact]
    public void AMachineIsKnownFromItsFirstTimeInTheBagAndStaysKnownWhenUsedUp()
    {
        var bag = new Inventory();
        Assert.Empty(TmMaker.Known(bag));
        bag.AddItem(Item("TM27"), 1);
        bag.AddItem(Item("TM01"), 1);
        Assert.Equal(new[] { "TM01", "TM27" }, TmMaker.Known(bag).Select(i => i.Name));
        Assert.True(bag.RemoveItem(Item("TM27"), 1));
        Assert.Equal(0, bag.GetQuantity(Item("TM27")));
        Assert.Contains("TM27", TmMaker.Known(bag).Select(i => i.Name));
        // Clearing the bag (a new game) forgets them
        bag.Clear();
        Assert.Empty(TmMaker.Known(bag));
    }

    [Fact]
    public void NoHmIsListedOrMade()
    {
        var hm = ItemDatabase.GetAll().First(MoveTeaching.IsHm);
        var bag = BagWith(hm.Name);
        Assert.Empty(TmMaker.Known(bag));
        Assert.Empty(bag.MachinesHad);
        Assert.False(TmMaker.Makeable(hm));
        var pouch = Pouch(("softFluff", 99), ("rockFlake", 99), ("geodeHeart", 99));
        var made = TmMaker.Make(hm, bag, pouch, 99999);
        Assert.False(made.Made);
        Assert.Equal(Text.Of("crafting.tm.why.hm"), made.Why);
    }

    [Fact]
    public void TheTrsAreMachinesToo_ListedAfterTheTms()
    {
        var tr = ItemDatabase.GetAll().First(i => i.Name.StartsWith("TR", StringComparison.Ordinal) && TmMaker.Makeable(i));
        var bag = BagWith(tr.Name, "TM27");
        Assert.Equal(new[] { "TM27", tr.Name }, TmMaker.Known(bag).Select(i => i.Name));
    }

    [Fact]
    public void EveryMachineOfTheDataHasACost()
    {
        var machines = ItemDatabase.GetAll().Where(TmMaker.Makeable).ToList();
        Assert.True(machines.Count >= 190, "Platinum's 92 TMs and the 100 TRs");
        foreach (var machine in machines)
        {
            var cost = TmMaker.CostOf(machine);
            Assert.NotNull(cost);
            Assert.True(MaterialTable.Exists(cost!.CommonId), machine.Name);
            Assert.True(cost.RareId == null || MaterialTable.Exists(cost.RareId), machine.Name);
            Assert.True(cost.Common >= 1 && cost.Money >= 1, machine.Name);
        }
    }

    [Fact]
    public void TheSaveKeepsTheMachinesHadAndAnOlderSaveKnowsThoseInItsBag()
    {
        var engine = new GameEngine(new GameSettings { Muted = true });
        engine.InitializeHeadless();
        engine.NewGameRegion = "Sinnoh";
        engine.StartNewGame();
        try
        {
            var game = engine.Drive;
            Assert.Null(game.BuildSave().MachinesHad);
            game.Bag.AddItem(Item("TM27"), 1);
            game.Bag.AddItem(Item("TM06"), 1);
            game.Bag.RemoveItem(Item("TM06"), 1);
            var save = game.BuildSave();
            Assert.Equal(new[] { "TM06", "TM27" }, save.MachinesHad);

            // Through the save's own JSON, and back into a game
            var read = GameDataFiles.Deserialize<SaveData>(GameDataFiles.Serialize(save));
            Assert.Equal(new[] { "TM06", "TM27" }, read.MachinesHad);
            read.MachinesHad = null;   // an older save: only the bag knows
            game.Bag.Clear();
            Assert.Empty(game.Bag.MachinesHad);
            game.ApplySave(read);
            Assert.Equal(new[] { "TM27" }, game.Bag.MachinesHad.OrderBy(n => n));
            read.MachinesHad = new List<string> { "TM06", "TM27", "HM01", "Gone" };
            game.ApplySave(read);
            Assert.Equal(new[] { "TM06", "TM27" }, game.Bag.MachinesHad.OrderBy(n => n));
        }
        finally
        {
            Ruleset.Use(RulesPreset.Platinum);
        }
    }

    // ---------------------------------------------------------------- the cost

    [Fact]
    public void TheCostFollowsTheMovesTypeAndPowerTier()
    {
        // TM27 is Return (variable power, a Normal move): a medium tier, no rare material
        var ret = TmMaker.CostOf(Item("TM27"))!;
        Assert.Equal(PokemonType.Normal, ret.Type);
        Assert.Equal("medium", ret.TierId);
        Assert.Equal(MaterialTable.OfType(PokemonType.Normal, rare: false)!.Id, ret.CommonId);
        Assert.Null(ret.RareId);

        // Hyper Beam (150) is the mighty tier, and takes the rare Normal material as well
        var beam = ItemDatabase.GetAll().First(i => TmMaker.Makeable(i) && i.TeachesMove == "Hyper Beam");
        var big = TmMaker.CostOf(beam)!;
        Assert.Equal("mighty", big.TierId);
        Assert.Equal(MaterialTable.OfType(PokemonType.Normal, rare: true)!.Id, big.RareId);
        Assert.True(big.Common > ret.Common && big.Money > ret.Money && big.Rare > 0);

        // A status move (Toxic) is the lightest tier, in its own type's material
        var toxic = ItemDatabase.GetAll().First(i => TmMaker.Makeable(i) && i.TeachesMove == "Toxic");
        var light = TmMaker.CostOf(toxic)!;
        Assert.Equal("light", light.TierId);
        Assert.Equal(PokemonType.Poison, light.Type);
        Assert.Equal(MaterialTable.OfType(PokemonType.Poison, rare: false)!.Id, light.CommonId);

        // The tiers climb: no heavier tier is cheaper in any way
        var tiers = RecipeTable.Tiers;
        for (int i = 1; i < tiers.Count; i++)
        {
            Assert.True(tiers[i].MaxPower > tiers[i - 1].MaxPower);
            Assert.True(tiers[i].Common >= tiers[i - 1].Common && tiers[i].Rare >= tiers[i - 1].Rare && tiers[i].Money > tiers[i - 1].Money);
        }
    }

    [Fact]
    public void ARareMaterialIsAskedForOnlyByAStrongMove()
    {
        foreach (var machine in ItemDatabase.GetAll().Where(TmMaker.Makeable))
        {
            var cost = TmMaker.CostOf(machine)!;
            var tier = RecipeTable.Tiers.Single(t => t.Id == cost.TierId);
            Assert.Equal(tier.Rare > 0, cost.RareId != null);
            Assert.Equal(tier.Common, cost.Common);
            Assert.Equal(tier.Money, cost.Money);
        }
    }

    // ---------------------------------------------------------------- making

    [Fact]
    public void AMachineIsMadeIntoTheBagWithTheMaterialsAndMoneyTakenAndRefusedShortOfEither()
    {
        var tm = Item("TM27");
        var bag = BagWith("TM27");
        bag.RemoveItem(tm, 1);
        var cost = TmMaker.CostOf(tm)!;
        var pouch = Pouch((cost.CommonId, cost.Common - 1));

        // One material short
        var short1 = TmMaker.Make(tm, bag, pouch, 99999);
        Assert.False(short1.Made);
        Assert.Equal(Text.Of("crafting.tm.why.short", ("name", MaterialTable.NameOf(cost.CommonId))), short1.Why);
        Assert.Equal(0, bag.GetQuantity(tm));
        Assert.Equal(cost.Common - 1, pouch.Count(cost.CommonId));

        // Materials enough, money short
        pouch.Add(cost.CommonId, 5);
        var poor = TmMaker.Make(tm, bag, pouch, cost.Money - 1);
        Assert.False(poor.Made);
        Assert.Equal(Text.Of("crafting.tm.why.money"), poor.Why);
        Assert.Equal(0, bag.GetQuantity(tm));
        Assert.Equal(cost.Common + 4, pouch.Count(cost.CommonId));

        // Enough of both: exactly the cost is taken and one machine comes
        var made = TmMaker.Make(tm, bag, pouch, cost.Money);
        Assert.True(made.Made);
        Assert.Equal(cost.Money, made.MoneySpent);
        Assert.Equal(1, bag.GetQuantity(tm));
        Assert.Equal(4, pouch.Count(cost.CommonId));
    }

    [Fact]
    public void AStrongMovesRareMaterialIsTakenToo()
    {
        var beam = ItemDatabase.GetAll().First(i => TmMaker.Makeable(i) && i.TeachesMove == "Hyper Beam");
        var bag = BagWith(beam.Name);
        var cost = TmMaker.CostOf(beam)!;
        var pouch = Pouch((cost.CommonId, cost.Common + 3));
        Assert.Equal(Text.Of("crafting.tm.why.short", ("name", MaterialTable.NameOf(cost.RareId!))), TmMaker.WhyNot(beam, bag, pouch, 99999));
        pouch.Add(cost.RareId!, cost.Rare + 1);
        Assert.True(TmMaker.Make(beam, bag, pouch, 99999).Made);
        Assert.Equal(3, pouch.Count(cost.CommonId));
        Assert.Equal(1, pouch.Count(cost.RareId!));
        Assert.Equal(2, bag.GetQuantity(beam));
    }

    [Fact]
    public void AMachineNeverInTheBagIsNotMadeAndTheBagStopsAtNinetyNine()
    {
        var tm = Item("TM27");
        var cost = TmMaker.CostOf(tm)!;
        var pouch = Pouch((cost.CommonId, 999));
        var empty = new Inventory();
        Assert.Equal(Text.Of("crafting.tm.why.unknown"), TmMaker.WhyNot(tm, empty, pouch, 99999));
        var bag = new Inventory();
        bag.AddItem(tm, TmMaker.MostInBag);
        Assert.Equal(Text.Of("crafting.tm.why.full"), TmMaker.WhyNot(tm, bag, pouch, 99999));
        Assert.Equal(999, pouch.Count(cost.CommonId));
    }

    [Fact]
    public void AMadeMachineTeachesAsAFoundOneDoesAndIsUsedUpAndMadeAgain()
    {
        var tm = Item("TM27");
        var cost = TmMaker.CostOf(tm)!;
        var species = PokemonDatabase.GetAll().First(s => s.TmMoves?.Contains("TM27") == true);
        var bag = BagWith("TM27");
        // The found one is taught and used up, as the bag does
        var first = Mon(species.Name, 30);
        Assert.Equal(TeachAnswer.Able, MoveTeaching.Answer(first, tm));
        MoveTeaching.Learn(first, tm.TeachesMove!);
        bag.RemoveItem(tm, 1);
        Assert.True(first.Knows("Return"));
        Assert.Equal(0, bag.GetQuantity(tm));

        // The same machine is made again: the very same item, which teaches the next Pokémon of the line
        var pouch = Pouch((cost.CommonId, cost.Common));
        Assert.True(TmMaker.Make(tm, bag, pouch, cost.Money).Made);
        var stack = bag.AllItems.Single(s => s.Data.Name == "TM27");
        Assert.Same(tm, stack.Data);
        var second = Mon(species.Name, 30);
        Assert.Equal(TeachAnswer.Able, MoveTeaching.Answer(second, stack.Data));
        MoveTeaching.Learn(second, stack.Data.TeachesMove!);
        bag.RemoveItem(stack.Data, 1);
        Assert.True(second.Knows("Return"));
        Assert.Equal(0, bag.GetQuantity(tm));
        Assert.Contains("TM27", bag.MachinesHad);
    }

    // ---------------------------------------------------------------- the screen

    private static GameEngine Headless()
    {
        var engine = new GameEngine(new GameSettings { Muted = true });
        engine.InitializeHeadless();
        engine.PeopleStayPut = true;
        return engine;
    }

    [Fact]
    public void TheScreenAtAPcListsTheMachinesMakesOneAndTheEngineTakesTheMoney()
    {
        var engine = Headless();
        try
        {
            var game = engine.Drive;
            engine.NewGameRegion = "Sinnoh";
            engine.StartNewGame();
            game.State = GameState.Overworld;
            game.Bag.AddItem(ItemDatabase.Get(GameEngine.CraftingKitItem)!, 1);
            game.Bag.AddItem(Item("TM27"), 1);
            game.Bag.RemoveItem(Item("TM27"), 1);
            var cost = TmMaker.CostOf(Item("TM27"))!;
            game.Crafting.Pouch.Add(cost.CommonId, cost.Common + 2);
            game.Money = cost.Money + 500;

            game.OpenCrafting(atMaker: true);
            var screen = game.CraftingScreen;
            Assert.Equal(GameState.Crafting, game.State);
            Assert.Equal(CraftingTab.Machines, screen.Tab);
            Assert.Equal(new[] { CraftingTab.Materials, CraftingTab.Make, CraftingTab.Machines, CraftingTab.Crosses }, screen.Tabs);
            Assert.Equal("TM27", screen.SelectedMachine!.Name);

            screen.Confirm();
            Assert.True(screen.LastMade);
            Assert.Equal(1, game.Bag.GetQuantity(Item("TM27")));
            Assert.Equal(2, game.Crafting.Pouch.Count(cost.CommonId));
            Assert.Equal(500, screen.Money);
            // The purse is the engine's: it takes the price on its next frame, once
            InputManager.Feed(ActionFrame.None);
            engine.Update(1f / 60f);
            Assert.Equal(500, game.Money);
            engine.Update(1f / 60f);
            Assert.Equal(500, game.Money);

            // Two materials and 500 left: the next is refused short of the price, and nothing is taken
            screen.Confirm();
            Assert.False(screen.LastMade);
            Assert.Equal(1, game.Bag.GetQuantity(Item("TM27")));
            Assert.Equal(0, screen.TakeSpent());

            // The next tab is the crosses (plan 39 · CR5), and after them the pouch; the kit opened anywhere has no maker
            screen.ChangeTab(1);
            Assert.Equal(CraftingTab.Crosses, screen.Tab);
            screen.ChangeTab(1);
            Assert.Equal(CraftingTab.Materials, screen.Tab);
            screen.Cancel();
            game.OpenCrafting();
            Assert.Equal(new[] { CraftingTab.Materials, CraftingTab.Make, CraftingTab.Crosses }, screen.Tabs);
            Assert.Empty(screen.Machines);
            screen.Confirm();
            Assert.Equal(1, game.Bag.GetQuantity(Item("TM27")));
        }
        finally
        {
            InputManager.Feed(null);
            Ruleset.Use(RulesPreset.Platinum);
        }
    }

    [Fact]
    public void TheMakersListScrollsAndWrapsAndKeepsTheCursorInView()
    {
        var bag = new Inventory();
        foreach (var machine in ItemDatabase.GetAll().Where(TmMaker.Makeable).Take(40)) bag.AddItem(machine, 1);
        var screen = new CraftingScreen();
        screen.Open(new CraftingState(), bag, 0, atMaker: true);
        Assert.Equal(40, screen.Machines.Count);
        for (int i = 0; i < 25; i++) screen.Move(1);
        Assert.InRange(screen.MachineCursor - screen.MachineFirst, 0, CraftingScreen.VisibleRows - 1);
        screen.Move(-1);
        for (int i = 0; i < 30; i++) screen.Move(-1);
        Assert.InRange(screen.MachineCursor - screen.MachineFirst, 0, CraftingScreen.VisibleRows - 1);
        Assert.NotNull(screen.SelectedCost);
    }

    // ---------------------------------------------------------------- the PC

    private static HeadlessScriptHost RunPc(HeadlessScriptHost host)
    {
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find("PC", "common")!, null);
        runner.RunToEnd();
        Assert.Empty(host.Problems);
        return host;
    }

    [Fact]
    public void ThePcOffersTheMakerWithTheKitInTheBagAndNotWithout()
    {
        // No kit: the modern PC's three entries, as before
        var plain = new HeadlessScriptHost { Modern = true };
        plain.Answers.Enqueue(2);
        RunPc(plain);
        Assert.Equal("SWITCH OFF", plain.Asked.Last().Answer);   // the third entry is SWITCH OFF: there is no maker to choose
        Assert.DoesNotContain("open Crafting", plain.Log);

        // With the kit: TM MAKER is third, and opens the kit on the maker's tab
        var kit = new HeadlessScriptHost { Modern = true };
        kit.Bag.AddItem(ItemDatabase.Get(GameEngine.CraftingKitItem)!, 1);
        kit.Answers.Enqueue(2);
        RunPc(kit);
        Assert.Equal("TM MAKER", kit.Asked.Last().Answer);
        Assert.Contains("open Crafting tm", kit.Log);

        // The other entries are where they were: storage opens the boxes, the jobs board the jobs
        var boxes = new HeadlessScriptHost { Modern = true };
        boxes.Bag.AddItem(ItemDatabase.Get(GameEngine.CraftingKitItem)!, 1);
        boxes.Answers.Enqueue(0);
        RunPc(boxes);
        Assert.Contains("open Pc", boxes.Log);
        Assert.DoesNotContain("open Crafting", boxes.Log);

        // With the Seal Case as well: TM MAKER follows the capsules
        var both = new HeadlessScriptHost { Modern = true };
        both.Bag.AddItem(ItemDatabase.Get(GameEngine.CraftingKitItem)!, 1);
        both.Bag.AddItem(ItemDatabase.Get("Seal Case")!, 1);
        both.Answers.Enqueue(3);
        RunPc(both);
        Assert.Contains("open Crafting tm", both.Log);

        // Platinum's own PC, whatever the bag holds, offers no maker
        var platinum = new HeadlessScriptHost();
        platinum.Bag.AddItem(ItemDatabase.Get(GameEngine.CraftingKitItem)!, 1);
        RunPc(platinum);
        Assert.DoesNotContain("open Crafting", platinum.Log);
    }

    [Fact]
    public void TheCommandOpensTheMakerOnlyWithTheKit()
    {
        var lonely = new HeadlessScriptHost();
        var runner = new ScriptRunner(ScriptLibrary.Default, lonely);
        runner.Start(ScriptParser.Parse("t", "script T\n  craft tm\n  end")[0], null);
        runner.RunToEnd();
        Assert.Equal(0, runner.Result);
        Assert.DoesNotContain("open Crafting", lonely.Log);

        var kit = new HeadlessScriptHost();
        kit.Bag.AddItem(ItemDatabase.Get(GameEngine.CraftingKitItem)!, 1);
        var again = new ScriptRunner(ScriptLibrary.Default, kit);
        again.Start(ScriptParser.Parse("t", "script T\n  craft tm\n  end")[0], null);
        again.RunToEnd();
        Assert.Equal(1, again.Result);
        Assert.Contains("open Crafting tm", kit.Log);
    }

    // ---------------------------------------------------------------- the file

    [Fact]
    public void TheFileIsReadStrictly()
    {
        Assert.Throws<System.IO.InvalidDataException>(() =>
            GameDataFiles.ReadStrict<RecipesFile>("""{ "machines": { "tiers": [ { "id": "x", "maxPower": 5, "common": 1, "money": 1, "commmon": 2 } ] } }""", "recipes.json"));
        Assert.Throws<System.IO.InvalidDataException>(() =>
            GameDataFiles.ReadStrict<RecipesFile>("""{ "machines": { "tiers": [ { "id": "x", "common": 1, "money": 1 } ] } }""", "recipes.json"));
    }
}
