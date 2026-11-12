using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumTests;

/// <summary>
/// Restaurants and stalls, CR7 (plan 39): every row of <c>recipes.json</c>'s <c>stands</c> names a dish with a power at a
/// place whose script serves it at the row's price; a meal takes its price and starts its power, and is refused short of
/// money; each waiter's script plays on every way; and the five are hidden until the crafting kit is in the bag.
/// </summary>
public class MealStandTests
{
    private static readonly string[] Places = { "SevenStarsRestaurant", "celadon_city_restaurant", "olivine_cafe", "slateport_city", "CasteliaCity" };

    [Fact]
    public void EveryStandRowNamesADishWithAPowerAtAPlaceWhoseScriptServesItAtThatPrice()
    {
        Assert.Equal(Places.OrderBy(p => p), RecipeTable.File.Stands.Select(s => s.Place).Distinct().OrderBy(p => p));
        foreach (string place in Places)
        {
            var rows = RecipeTable.StandAt(place);
            Assert.Equal(3, rows.Count);
            Assert.All(rows, r => Assert.NotNull(Meals.Dish(r.Dish)?.Power));
            var script = ScriptLibrary.Default.In(place, "MealStand");
            Assert.NotNull(script);
            var prices = script!.Everything().Where(i => i.Op == Op.TakeMoney).Select(i => i.Number).ToList();
            var dishes = script.Everything().Where(i => i is { Op: Op.Craft, Name: "meal" }).Select(i => i.Other).ToList();
            Assert.Equal(rows.Select(r => r.Price), prices);
            Assert.Equal(rows.Select(r => r.Dish), dishes);
        }
    }

    private static HeadlessScriptHost Order(string place, int answer, int money)
    {
        var host = new HeadlessScriptHost { Money = money };
        host.Bag.AddItem(ItemDatabase.Get(GameEngine.CraftingKitItem)!, 1);
        host.Party.Add(new Pokemon(PokemonDatabase.Get("Bidoof")!, 20));
        host.Answers.Enqueue(answer);
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        var keeper = new NPC { Name = "Waiter", NpcType = "Waiter", Key = "meal_stand", ScriptFile = place, Script = "MealStand" };
        var map = new Map(8, 8);
        map.Add(keeper);
        host.Map = map;
        runner.Start(ScriptLibrary.Default.In(place, "MealStand")!, keeper);
        runner.RunToEnd();
        Assert.Empty(host.Problems);
        return host;
    }

    [Fact]
    public void AMealInCeladonsRestaurantTakesItsPriceAndStartsItsPower()
    {
        var row = RecipeTable.StandAt("celadon_city_restaurant")[0];
        var host = Order("celadon_city_restaurant", 0, 5000);
        Assert.Equal(5000 - row.Price, host.Money);
        Assert.True(host.Powers!.Has(Meals.Dish(row.Dish)!.Power!.Value));
    }

    [Fact]
    public void AMealIsRefusedShortOfMoney()
    {
        var host = Order("olivine_cafe", 0, 100);
        Assert.Equal(100, host.Money);
        Assert.Empty(host.Powers!.InForce);
        Assert.Contains(host.Transcript, t => t.Text.Contains("more than you have"));
    }

    [Fact]
    public void EveryWaiterAndStallIsHiddenUntilTheKitIsInTheBag()
    {
        var rooms = new[] { "SevenStarsRestaurant", "CasteliaCity" };
        foreach (string room in rooms)
        {
            var map = MapDatabase.Get(room);
            var stand = Assert.Single(map.Everyone, n => n.Script == "MealStand");
            Assert.Equal(GameEngine.CraftingKitFlag, stand.ShownBy);
        }
        foreach (var world in World.LoadAll().Where(w => w.Index.Region is "Kanto" or "Johto" or "Hoenn"))
            foreach (string key in new[] { "celadon_city_restaurant", "olivine_cafe", "slateport_city" })
                if (world.Overlay(key) is { } overlay)
                {
                    var stand = Assert.Single(overlay.Npcs ?? new(), n => n.Script == "MealStand");
                    Assert.Equal(GameEngine.CraftingKitFlag, stand.ShownBy);
                }
    }
}
