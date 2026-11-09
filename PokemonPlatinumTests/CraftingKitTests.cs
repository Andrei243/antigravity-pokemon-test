using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;
using static PokemonPlatinumTests.CoreScenario;
using static PokemonPlatinumTests.Scenario;

namespace PokemonPlatinumTests;

/// <summary>
/// Materials, crafting and cooking, CR3 (plan 39): the kit's MAKE tab. Every recipe is held to its rules and is never
/// cheaper in materials than its item in the Mart; everything it takes can be had; a recipe is refused short of a material
/// or an item, or with the bag full of what it makes; making takes and gives exactly; a made Feather Ball and Heavy Ball
/// catch by their own rules; the apprentice trades three of a common material for one rare; a made thing is exactly its
/// recipe's item, which the randomiser never maps; and the screen asks how many and makes them.
/// </summary>
[Collection("Shared clocks and dice")]
public class CraftingKitTests
{
    private static ItemData Item(string name) => ItemDatabase.Get(name)!;

    private static MaterialPouch Enough(CraftRecipe recipe, int times)
    {
        var pouch = new MaterialPouch();
        foreach (var need in recipe.Takes.Where(n => n.Material != null)) pouch.Add(need.Material!, need.Count * times);
        return pouch;
    }

    private static Inventory Bag(CraftRecipe recipe, int times)
    {
        var bag = new Inventory();
        foreach (var need in recipe.Takes.Where(n => n.Item != null)) bag.AddItem(Item(need.Item!), need.Count * times);
        return bag;
    }

    // ---------------------------------------------------------------- the table

    [Fact]
    public void EveryRecipeKeepsItsRulesAndNoneIsCheaperThanTheMart()
    {
        Assert.NotEmpty(Crafting.Recipes);
        var problems = Crafting.Problems(Crafting.Recipes).ToList();
        Assert.True(problems.Count == 0, string.Join("; ", problems.Select(p => $"{p.Name}: {p.What}")));
        foreach (var recipe in Crafting.Recipes)
            Assert.True(Crafting.WorthOf(recipe) >= Crafting.PriceOf(recipe), $"{recipe.Id}: worth {Crafting.WorthOf(recipe)} for {Crafting.PriceOf(recipe)}");
        // The plan's list: the three balls, Hisui's throwing balls, medicine, Repels, an Escape Rope, Honey and the four mulches
        var made = Crafting.Recipes.Select(r => r.Makes).ToHashSet();
        foreach (string name in new[] { "Poké Ball", "Great Ball", "Ultra Ball", "Feather Ball", "Wing Ball", "Jet Ball", "Leaden Ball", "Gigaton Ball",
                     "Heavy Ball", "Potion", "Repel", "Escape Rope", "Honey", "Growth Mulch", "Damp Mulch", "Stable Mulch", "Gooey Mulch" })
            Assert.Contains(name, made);
    }

    [Fact]
    public void TheCheckFindsARecipeCheaperThanTheMartAndOneOfNothing()
    {
        var cheap = new CraftRecipe { Id = "cheapBall", Makes = "Ultra Ball", Takes = { new CraftNeed { Material = "softFluff", Count = 1 } } };
        var nothing = new CraftRecipe { Id = "noBall", Makes = "Moon Rock Of Nowhere", Takes = { new CraftNeed { Material = "softFluff", Count = 1 } } };
        var noValue = new CraftRecipe { Id = "noValue", Makes = "Feather Ball", Takes = { new CraftNeed { Material = "looseFeather", Count = 50 } } };
        var problems = Crafting.Problems(new[] { cheap, nothing, noValue }).ToList();
        Assert.Contains(problems, p => p.Name == "cheapBall" && p.What.Contains("cheaper than the Mart"));
        Assert.Contains(problems, p => p.Name == "noBall" && p.What.Contains("no item"));
        Assert.Contains(problems, p => p.Name == "noValue" && p.What.Contains("no price"));
    }

    [Fact]
    public void EverythingARecipeTakesCanBeHad()
    {
        // Every material a recipe takes is a Pokémon's, which wild Pokémon of its type leave (the rare one now and then, or
        // three common ones at the apprentice's), or the pantry's, sold at the workshop's counter. The field's materials are
        // CR2's gathering, which no recipe of CR3 asks for
        foreach (var recipe in Crafting.Recipes)
            foreach (var need in recipe.Takes)
            {
                if (need.Material is { } id)
                {
                    var material = MaterialTable.Get(id)!;
                    Assert.True(material.Kind is MaterialKind.Pokemon or MaterialKind.Pantry, $"{recipe.Id} takes {id}, which nothing gives yet (plan 39 · CR2)");
                    if (material.Kind == MaterialKind.Pokemon)
                        Assert.Contains(PokemonDatabase.GetAll(), s => s.PrimaryType == material.Type || s.SecondaryType == material.Type);
                }
                else Assert.True(ItemSources.Everywhere.Has(need.Item!), $"{recipe.Id} takes {need.Item}, which nothing gives");
            }
    }

    [Fact]
    public void EveryRecipeIsASureWayToItsItemOnceTheKitIsGiven()
    {
        foreach (var recipe in Crafting.Recipes)
            Assert.True(ItemSources.Everywhere.Has(recipe.Makes, ItemSourceKind.Made), recipe.Makes);
        Assert.Contains(ItemSources.Everywhere.Of("Feather Ball"), s => s.Kind == ItemSourceKind.Made && s.Where.Contains("SunyshoreWorkshop"));
    }

    // ---------------------------------------------------------------- making

    [Fact]
    public void MakingTakesAndGivesExactly()
    {
        var recipe = Crafting.Get("potion")!;
        var pouch = Enough(recipe, 3);
        pouch.Add("softFluff", 5);
        var bag = Bag(recipe, 3);
        Assert.Equal(3, Crafting.MostOf(recipe, bag, pouch));
        var made = Crafting.Make(recipe, 3, bag, pouch);
        Assert.True(made.Made);
        Assert.Equal(3, made.Count);
        Assert.Equal(3, bag.GetQuantity(Item("Potion")));
        Assert.Equal(0, bag.GetQuantity(Item("Oran Berry")));
        foreach (var need in recipe.Takes.Where(n => n.Material != null)) Assert.Equal(0, pouch.Count(need.Material!));
        Assert.Equal(5, pouch.Count("softFluff"));
    }

    [Fact]
    public void ARecipeIsRefusedShortOfAMaterialOrAnItemOrWithTheBagFull()
    {
        var recipe = Crafting.Get("potion")!;
        // A material short: nothing taken
        var pouch = Enough(recipe, 1);
        pouch.Take("sunBloom", 1);
        var bag = Bag(recipe, 1);
        var refused = Crafting.Make(recipe, 1, bag, pouch);
        Assert.False(refused.Made);
        Assert.Contains(MaterialTable.NameOf("sunBloom"), refused.Why);
        Assert.Equal(2, bag.GetQuantity(Item("Oran Berry")));
        Assert.Equal(0, bag.GetQuantity(Item("Potion")));
        // An item short
        pouch = Enough(recipe, 1);
        bag = new Inventory();
        Assert.Contains(Item("Oran Berry").DisplayName, Crafting.WhyNot(recipe, 1, bag, pouch));
        Assert.Equal(0, Crafting.MostOf(recipe, bag, pouch));
        // Enough for one and not for two
        bag = Bag(recipe, 1);
        Assert.Null(Crafting.WhyNot(recipe, 1, bag, pouch));
        Assert.NotNull(Crafting.WhyNot(recipe, 2, bag, pouch));
        // The bag full of what it makes
        bag.AddItem(Item("Potion"), Crafting.MostInBag);
        Assert.Contains(Item("Potion").DisplayName, Crafting.WhyNot(recipe, 1, bag, pouch));
        Assert.Equal(0, Crafting.MostOf(recipe, bag, pouch));
    }

    // ---------------------------------------------------------------- what is made catches by its own rules

    private static bool Caught(string ball, Pokemon mine, Pokemon foe, int roll)
    {
        var party = new Party();
        party.Add(mine);
        var core = new BattleCore(new CoreSetup
        {
            PlayerParty = party, WildPokemon = new List<Pokemon> { foe }, Random = Steady().Force(RollKind.SideEffect, 99).Force(RollKind.CatchShake, roll),
            Rules = Ruleset.Modern, PlayerController = null, EnemyController = null, PlayerName = "Lucas"
        });
        core.Start();
        core.TakeLog();
        core.Submit(BattleChoice.UseItem(Mine, ball), BattleChoice.Fight(Foe, 0));
        return core.Result == BattleResult.EnemyCaught;
    }

    private static Pokemon Of(string species, int level) => new(PokemonDatabase.Get(species)!, level, Gender.Male, Nature.Hardy, false);

    [Fact]
    public void AMadeFeatherBallAndHeavyBallCatchByTheirOwnRules()
    {
        // A Feather Ball made from a Starly's leavings and a Grass type's leaf
        var feather = Crafting.Get("featherBall")!;
        var bag = new Inventory();
        var pouch = Enough(feather, 1);
        Assert.True(Crafting.Make(feather, 1, bag, pouch).Made);
        Assert.Equal(1, bag.GetQuantity(Item("Feather Ball")));
        var starly = Of("Starly", 10);
        Assert.Contains(MaterialDrops.For(starly, new Random(1)), m => m.Id == "softFluff" || m.Id == "looseFeather");

        // By R28a's rules the Feather Ball's bonus is its own (none past a Poké Ball's here); both hold at a roll that holds
        int holds = Formulas.ShakeRate(Formulas.CatchRate(starly.CatchRate, 10, starly.MaxHP, starly.CurrentHP, starly.Status));
        Assert.True(Caught("Feather Ball", Of("Pikachu", 30), Of("Starly", 10), holds - 1));
        Assert.False(Caught("Feather Ball", Of("Pikachu", 30), Of("Starly", 10), holds + 1));

        // A Heavy Ball made, and its weight's bonus on a Snorlax: it holds where a Poké Ball doesn't
        var heavy = Crafting.Get("heavyBall")!;
        Assert.True(Crafting.Make(heavy, 1, bag, Enough(heavy, 1)).Made);
        var snorlax = Of("Snorlax", 30);
        int heavyRate = CatchCalculator.HeavyBallRate("Heavy Ball", snorlax, snorlax.CatchRate);
        Assert.True(heavyRate > snorlax.CatchRate);
        int low = Formulas.ShakeRate(Formulas.CatchRate(snorlax.CatchRate, 10, snorlax.MaxHP, snorlax.CurrentHP, snorlax.Status));
        Assert.True(Caught("Heavy Ball", Of("Pikachu", 30), Of("Snorlax", 30), low));
        Assert.False(Caught("Poké Ball", Of("Pikachu", 30), Of("Snorlax", 30), low));
    }

    [Fact]
    public void AMadeThingIsTheRecipesItemWhateverTheRun()
    {
        // The randomiser maps what the world and the scripts give; the kit asks nothing of it, under any run
        var run = RunRules.Normal with { Random = new RandomizerOptions { Seed = 12345, Items = true } };
        var randomizer = Randomizer.For(run)!;
        var feather = Crafting.Get("featherBall")!;
        var bag = new Inventory();
        Assert.True(Crafting.Make(feather, 1, bag, Enough(feather, 1)).Made);
        Assert.Equal(1, bag.GetQuantity(Item("Feather Ball")));
        Assert.NotNull(randomizer);
    }

    // ---------------------------------------------------------------- the apprentice

    [Fact]
    public void TheApprenticeTradesThreeOfTheMostHeldCommonForOneRare()
    {
        var pouch = new MaterialPouch();
        Assert.Null(Crafting.Tradable(pouch));
        pouch.Add("softFluff", 2);
        Assert.Null(Crafting.Trade(pouch));
        pouch.Add("softFluff", 2);
        pouch.Add("cinderAsh", 7);
        Assert.Equal("cinderAsh", Crafting.Tradable(pouch)!.Id);
        Assert.Equal("emberCore", Crafting.Trade(pouch)!.Id);
        Assert.Equal(4, pouch.Count("cinderAsh"));
        Assert.Equal(1, pouch.Count("emberCore"));
        // A rare material that is full is never traded for
        pouch.Add("plushTuft", MaterialTable.Limit);
        pouch.Take("cinderAsh", 4);
        Assert.Null(Crafting.Tradable(pouch));
    }

    [Fact]
    public void TheApprenticesScriptTradesOnlyWithTheKitAndEnough()
    {
        var host = new HeadlessScriptHost();
        host.Bag.AddItem(Item(GameEngine.CraftingKitItem), 1);
        host.Crafting!.Pouch.Add("looseFeather", 3);
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find("Apprentice", "SunyshoreWorkshop")!, null);
        runner.RunToEnd();
        Assert.Equal(0, host.Crafting.Pouch.Count("looseFeather"));
        Assert.Equal(1, host.Crafting.Pouch.Count("galePlume"));
    }

    // ---------------------------------------------------------------- the screen

    [Fact]
    public void TheMakeTabAsksHowManyAndMakesThem()
    {
        var state = new CraftingState();
        var recipe = Crafting.Get("pokeBall")!;
        foreach (var need in recipe.Takes) state.Pouch.Add(need.Material!, need.Count * 4);
        var bag = new Inventory();
        var screen = new CraftingScreen();
        screen.Open(state, bag, 0, CraftingPlace.Anywhere);
        Assert.Equal(new[] { CraftingTab.Materials, CraftingTab.Make }, screen.Tabs);
        screen.ChangeTab(1);
        Assert.Equal(CraftingTab.Make, screen.Tab);
        Assert.Equal("pokeBall", screen.SelectedRecipe!.Id);
        Assert.Equal(4, screen.MostNow);

        screen.Confirm();
        Assert.Equal(1, screen.HowMany);
        screen.ChangeAmount(10);
        Assert.Equal(4, screen.HowMany);
        screen.ChangeAmount(-1);
        Assert.Equal(3, screen.HowMany);
        // While the number is asked the cursor and the tab stay
        screen.Move(1);
        screen.ChangeTab(1);
        Assert.Equal("pokeBall", screen.SelectedRecipe!.Id);
        Assert.Equal(CraftingTab.Make, screen.Tab);
        screen.Confirm();
        Assert.Equal(0, screen.HowMany);
        Assert.True(screen.LastMade);
        Assert.Equal(3, bag.GetQuantity(Item("Poké Ball")));
        Assert.Equal(1, screen.MostNow);

        // B puts the number away without making anything
        screen.Confirm();
        screen.Cancel();
        Assert.Equal(0, screen.HowMany);
        Assert.True(screen.IsActive);
        Assert.Equal(3, bag.GetQuantity(Item("Poké Ball")));

        // A recipe that can't be made says why at the first A
        screen.Move(1);
        screen.Confirm();
        Assert.Equal(0, screen.HowMany);
        Assert.False(screen.LastMade);
        Assert.NotEmpty(screen.Notice);
    }
}
