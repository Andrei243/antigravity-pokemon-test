using System;
using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumTests;

/// <summary>
/// The Lilycove Lady (plan 21 · H11, part c; rulings, "The Lilycove Lady"): Emerald's Favor Lady's tables against our items,
/// her count of things of her kind and of the one she wants most, and her talk played by <see cref="HeadlessScriptHost"/>
/// in Lilycove's Pokémon Center: an item she likes, one she doesn't, the prize and a new favour, and nothing more that day.
/// </summary>
[Collection("Shared clocks and dice")]
public class HoennLilycoveLadyTests
{
    private const string Area = "lilycove_city_pokemon_center_1f";

    private static readonly Lazy<Map> Center = new(() =>
        World.LoadAll().Single(w => w.Index.Region == RegionDatabase.Hoenn).BuildMaps().Single(m => m.Name == "LilycoveCity_PokemonCenter_1F"));

    private static HeadlessScriptHost Talk(HeadlessScriptHost host, params int[] answers)
    {
        foreach (int a in answers) host.Answers.Enqueue(a);
        var lady = host.Map!.Everyone.First(n => n.Key == "lilycove_lady");
        host.PlayerTile = (lady.GridX + 1, lady.GridY);
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find(lady.Script!, Area)!, lady);
        runner.RunToEnd();
        Assert.True(host.Problems.Count == 0, string.Join("; ", host.Problems));
        return host;
    }

    private static HeadlessScriptHost WithFavor(int favor, int best, int given = 0)
    {
        var host = new HeadlessScriptHost { Map = Center.Value, Region = RegionDatabase.Hoenn };
        host.Story.SetVar(LilycoveLady.FavorVariable, favor + 1);
        host.Story.SetVar(LilycoveLady.BestVariable, best);
        host.Story.SetVar(LilycoveLady.GivenVariable, given);
        return host;
    }

    private static int Count(HeadlessScriptHost host, string item) => host.Bag.GetQuantity(ItemDatabase.Get(item)!);

    [Fact]
    public void EveryItemOfHerTablesIsAnItemOfTheData()
    {
        Assert.Equal(6, LilycoveLady.Accepted.Length);
        foreach (var name in LilycoveLady.Accepted.SelectMany(l => l).Concat(LilycoveLady.Prizes))
            Assert.True(ItemDatabase.Get(name) != null, $"no item '{name}'");
        // The sizes of Emerald's lists
        Assert.Equal(new[] { 12, 16, 14, 20, 10, 10 }, LilycoveLady.Accepted.Select(l => l.Length));
    }

    [Fact]
    public void ShesTheFavorLadyOfEmeraldsThirdOfTrainerNumbersAndCountsAsEmeraldDoes()
    {
        Assert.Equal((0, 1, 2), (LilycoveLady.EmeraldLadyOf(0), LilycoveLady.EmeraldLadyOf(2), LilycoveLady.EmeraldLadyOf(5)));
        // Slippery: a Repel is one more, a Nugget nothing, the one she wants most the threshold at once
        Assert.Equal(1, LilycoveLady.CountAfter(0, 0, 0, "Super Repel"));
        Assert.Equal(2, LilycoveLady.CountAfter(0, 0, 2, "Nugget"));
        Assert.Equal(LilycoveLady.Threshold, LilycoveLady.CountAfter(0, 0, 0, "Repel"));
        var random = new Random(4);
        for (int i = 0; i < 50; i++)
        {
            var (favor, best) = LilycoveLady.Pick(() => random.Next(0x10000));
            Assert.InRange(favor, 0, 5);
            Assert.InRange(best, 0, LilycoveLady.Accepted[favor].Length - 1);
        }
    }

    [Fact]
    public void AnItemOfHerKindIsCountedAndTakenAndSheWaitsForTheNextDay()
    {
        // Shiny, wanting a Nugget most; a Stardust is of her kind
        var host = WithFavor(3, 9);
        host.Bag.AddItem(ItemDatabase.Get("Stardust")!, 2);
        host.ItemChoice = "Stardust";
        Talk(host, 0);
        Assert.Equal((1, 1), (host.Story.Var(LilycoveLady.GivenVariable), Count(host, "Stardust")));
        Assert.True(host.Story.Has("FLAG_DAILY_HOENN_FAVOR_LADY"));
        // The same day she only thanks the player
        Talk(host);
        Assert.Equal(1, Count(host, "Stardust"));
    }

    [Fact]
    public void AnItemNotOfHerKindIsKeptAndNotCounted()
    {
        var host = WithFavor(0, 0, 2);
        host.Bag.AddItem(ItemDatabase.Get("Nugget")!);
        host.ItemChoice = "Nugget";
        Talk(host, 0);
        Assert.Equal((2, 0), (host.Story.Var(LilycoveLady.GivenVariable), Count(host, "Nugget")));
    }

    [Fact]
    public void TheFifthOfHerKindEarnsThePrizeAndANewFavour()
    {
        // Pointy, four counted: a Quick Claw is the fifth, and the prize is PP Max
        var host = WithFavor(5, 0, 4);
        host.Bag.AddItem(ItemDatabase.Get("Poison Barb")!);
        host.ItemChoice = "Poison Barb";
        Talk(host, 0);
        Assert.Equal(1, Count(host, "PP Max"));
        Assert.Equal(0, host.Story.Var(LilycoveLady.GivenVariable));
        Assert.InRange(host.Story.Var(LilycoveLady.FavorVariable), 1, 6);
    }

    [Fact]
    public void TheOneSheWantsMostEarnsThePrizeAtOnce()
    {
        // Roundish, wanting the Pearl (place 1) most: a Luxury Ball's favour is slippery, so roundish gives a Nugget
        var host = WithFavor(1, 1);
        host.Bag.AddItem(ItemDatabase.Get("Pearl")!);
        host.ItemChoice = "Pearl";
        Talk(host, 0);
        Assert.Equal(1, Count(host, "Nugget"));
    }

    [Fact]
    public void ADeclinedFavourChangesNothingAndAFirstTalkDrawsAFavour()
    {
        var host = new HeadlessScriptHost { Map = Center.Value, Region = RegionDatabase.Hoenn };
        Talk(host, 1);
        Assert.InRange(host.Story.Var(LilycoveLady.FavorVariable), 1, 6);
        Assert.False(host.Story.Has("FLAG_DAILY_HOENN_FAVOR_LADY"));
    }
}
