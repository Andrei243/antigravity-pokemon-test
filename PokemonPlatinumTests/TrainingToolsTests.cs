using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.Frontier;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumTests;

/// <summary>
/// The training tools, TR3 (plan 36; <c>docs/mechanics/rulings.md</c>, "Peak training and the training tools"): the training
/// hall's stall and its prices, the Battle Points list and its three clerks, the Bottle Caps of the Battle Zone, and
/// <see cref="ItemSources"/>, by which every tool can be had in Sinnoh after its Hall of Fame (decision 2) but those another
/// plan owes, named below as <c>SinnohSpeciesTests</c> names its species.
/// </summary>
[Collection("Shared clocks and dice")]
public class TrainingToolsTests
{
    private static readonly ScriptLibrary Scripts = ScriptLibrary.Default;
    private const string Hall = GameEngine.SunyshoreTrainingHall;

    private static readonly string[] Mints =
    {
        "Lonely", "Adamant", "Naughty", "Brave", "Bold", "Impish", "Lax", "Relaxed", "Modest", "Mild", "Rash", "Quiet",
        "Calm", "Gentle", "Careful", "Sassy", "Timid", "Hasty", "Jolly", "Naive", "Serious"
    };

    /// <summary>
    /// The later games' training tools the rules run (plan 06 · R28b and beside it): the mints, the Exp. Candies, the Ability
    /// Capsule and Patch, the Bottle Caps, the Dynamax Candy and Max Mushrooms, the nectars, the mochi, Legends: Arceus's
    /// candies, Sword and Shield's TRs and the charms.
    /// </summary>
    private static IReadOnlyList<string> Tools()
    {
        var candies = new Regex(@"^(Health|Mighty|Tough|Smart|Courage|Quick) Candy( L| XL)?$");
        var uses = new HashSet<string> { LaterItemUse.Mint, LaterItemUse.ExpCandy, LaterItemUse.AbilityCapsule, LaterItemUse.AbilityPatch,
            LaterItemUse.DynamaxCandy, LaterItemUse.Nectar };
        return ItemDatabase.GetAll()
            .Where(i => uses.Contains(i.FieldUse ?? "") || HyperTraining.IsCap(i) || i.Name.EndsWith(" Mochi") || candies.IsMatch(i.Name)
                || Regex.IsMatch(i.Name, @"^TR\d\d$") || i.Name is "Max Mushrooms" or "Shiny Charm" or "Oval Charm")
            .Select(i => i.Name).OrderBy(n => n, StringComparer.Ordinal).ToList();
    }

    /// <summary>
    /// The tools no way of Sinnoh gives yet, each with the plan that owes it (plan 36's list). The session that places one
    /// takes its line off; a line whose tool has a way fails as stale.
    /// </summary>
    private static readonly Dictionary<string, string> Owed = Owing();

    private static Dictionary<string, string> Owing()
    {
        var owed = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Exp. Candy XS"] = "plan 33 (Galar's raids pay in the smallest and largest Exp. Candies)",
            ["Exp. Candy XL"] = "plan 33 (Galar's raids pay in the smallest and largest Exp. Candies)",
            ["Dynamax Candy"] = "plan 33 (Galar)",
            ["Max Mushrooms"] = "plan 33 (Galar)",
            ["Red Nectar"] = "plan 23 (Alola's meadows)",
            ["Yellow Nectar"] = "plan 23 (Alola's meadows)",
            ["Pink Nectar"] = "plan 23 (Alola's meadows)",
            ["Purple Nectar"] = "plan 23 (Alola's meadows)",
            ["Shiny Charm"] = "plan 40 (the charms for a completed Pokédex)",
        };
        foreach (string stat in new[] { "Health", "Mighty", "Tough", "Smart", "Courage", "Quick" })
            foreach (string size in new[] { "", " L", " XL" })
                owed[$"{stat} Candy{size}"] = "plan 23 (Hisui: Legends: Arceus's candies)";
        foreach (string mochi in new[] { "Health", "Muscle", "Resist", "Genius", "Clever", "Swift", "Fresh Start" })
            owed[$"{mochi} Mochi"] = "plan 34 (Paldea: the mochi)";
        for (int n = 0; n <= 99; n++) owed[$"TR{n:00}"] = "plan 33 (Galar: Sword and Shield's TRs; plan 36 · TR9 teaches the later moves)";
        return owed;
    }

    // ---------------------------------------------------------------- the stall

    [Fact]
    public void TheStallSellsTheCandiesTheCapsuleAndTheMintsAtPricesOfOurs()
    {
        var stock = MartDatabase.Stock("training_hall", 8).Select(i => i.Name).ToList();
        Assert.Equal(new[] { "Exp. Candy S", "Exp. Candy M", "Exp. Candy L", "Ability Capsule" }.Concat(Mints.Select(m => m + " Mint")), stock);
        // No placeholder price is left on a tool: the mints, Exp. Candy XS and the Ability Patch are ours
        Assert.All(Mints, m => Assert.Equal(3000, ItemDatabase.Get(m + " Mint")!.Price));
        Assert.Equal(40, ItemDatabase.Get("Exp. Candy XS")!.Price);
        Assert.Equal(40000, ItemDatabase.Get("Ability Patch")!.Price);
        Assert.All(Tools().Where(t => !Owed.ContainsKey(t)), t => Assert.True(ItemDatabase.Get(t)!.Price > 20, $"{t} still has the import's placeholder price"));
    }

    private static HeadlessScriptHost Talk(string script, string place, StoryState? story, params int[] answers)
    {
        var host = new HeadlessScriptHost(story, new Party());
        foreach (int a in answers) host.Answers.Enqueue(a);
        var runner = new ScriptRunner(Scripts, host);
        runner.Start(Scripts.Find(script, place)!, null);
        runner.RunToEnd();
        Assert.Empty(host.Problems);
        return host;
    }

    private static StoryState Champion()
    {
        var story = new StoryState();
        story.Set(RegionDatabase.Get(RegionDatabase.Sinnoh)!.StoryCompleteFlag);
        return story;
    }

    [Fact]
    public void TheHallsClerkOpensHerStallOnlyForAChampion()
    {
        Assert.DoesNotContain(Talk("TrainingClerk", Hall, null).Log, l => l.StartsWith("open Shop"));
        Assert.Contains("open Shop training_hall", Talk("TrainingClerk", Hall, Champion()).Log);
    }

    // ---------------------------------------------------------------- the Battle Points list

    [Fact]
    public void TheClubsClerkTradesBattlePointsForAToolAndOnlyForAChampion()
    {
        Assert.Equal(new[] { "Bottle Cap", "Gold Bottle Cap", "Ability Capsule", "Ability Patch" }.Concat(Mints.Select(m => m + " Mint")),
            TrainingData.BattlePoints.Select(p => p.Item));
        Assert.All(TrainingData.BattlePoints, p => Assert.NotNull(ItemDatabase.Get(p.Item)));
        Assert.Same(TrainingData.BattlePoints, FrontierExchange.Stock(FrontierExchange.Training));

        // Before a Hall of Fame she asks nothing
        var before = Talk("TrainingExchange", "battle_frontier", null);
        Assert.Empty(before.Asked);

        // A Champion with 100 points takes a Bottle Cap for 48
        var host = new HeadlessScriptHost(Champion(), new Party());
        host.Frontier.GivePoints(100);
        host.Answers.Enqueue(0);
        var runner = new ScriptRunner(Scripts, host);
        runner.Start(Scripts.Find("TrainingExchange", "battle_frontier")!, null);
        runner.RunToEnd();
        Assert.Equal(1, host.Bag.GetQuantity(ItemDatabase.Get("Bottle Cap")!));
        Assert.Equal(52, host.Frontier.BattlePoints);

        // Too few points for a Gold Bottle Cap: nothing changes hands
        host.Answers.Enqueue(1);
        runner.Start(Scripts.Find("TrainingExchange", "battle_frontier")!, null);
        runner.RunToEnd();
        Assert.Equal(0, host.Bag.GetQuantity(ItemDatabase.Get("Gold Bottle Cap")!));
        Assert.Equal(52, host.Frontier.BattlePoints);
    }

    [Fact]
    public void FrontierBuyRefusesAListWhosePriceChangedSinceThePrizeWasChosen()
    {
        var row = TrainingData.BattlePoints.First(p => p.Item == "Bottle Cap");
        int price = row.Price;
        try
        {
            var host = new HeadlessScriptHost(Champion(), new Party());
            host.Frontier.GivePoints(200);
            // The prize is chosen; then, before the trade, the list's price moves
            host.Decide = (question, answers) =>
            {
                if (question == "Sure?") row.Price = price + 10;
                return 0;
            };
            var runner = new ScriptRunner(Scripts, host);
            runner.Start(ScriptParser.Parse("t", "script T\n  frontier prize training\n  ask \"Sure?\"\n  frontier buy\n  end")[0], null);
            runner.RunToEnd();
            Assert.Equal(0, runner.Result);
            Assert.Equal(200, host.Frontier.BattlePoints);
            Assert.Equal(0, host.Bag.GetQuantity(ItemDatabase.Get("Bottle Cap")!));
        }
        finally
        {
            row.Price = price;
        }
    }

    [Fact]
    public void TheClubsClerkStandsAtTheThreeBattlePointsCounters()
    {
        var grounds = MapDatabase.Get("BattleFrontier");
        var sinnoh = grounds.Everyone.Single(n => n.Script == "TrainingExchange");
        Assert.Equal((50, 43), (sinnoh.GridX, sinnoh.GridY));

        var station = MapDatabase.Get("GearStation");
        Assert.Contains(station.Everyone, n => n.Script == "TrainingExchange");

        var hoenn = World.LoadAll().Single(w => w.Index.Region == RegionDatabase.Hoenn)
            .Overlay("battle_frontier_exchange_service_corner")!;
        Assert.Contains(hoenn.Npcs!, n => n.Script == "TrainingExchange");
    }

    // ---------------------------------------------------------------- every tool's source

    [Fact]
    public void ItemSourcesFindsAKnownItemInEachKindOfSource()
    {
        var sinnoh = ItemSources.In(RegionDatabase.Sinnoh);
        Assert.Contains(sinnoh.Of("Poké Ball"), s => s.Kind == ItemSourceKind.Stock && s.Where == "a Poké Mart");
        Assert.Contains(sinnoh.Of("Ability Capsule"), s => s.Kind == ItemSourceKind.Stock && s.Where == "the counter training_hall");
        Assert.Contains(sinnoh.Of("Gold Bottle Cap"), s => s.Kind == ItemSourceKind.Prize && s.Where.Contains("TrainingExchange"));
        Assert.Contains(sinnoh.Of("Bottle Cap"), s => s.Kind == ItemSourceKind.Ball && s.Where == "route_228");
        Assert.Contains(sinnoh.Of("Bottle Cap"), s => s.Kind == ItemSourceKind.Ball && s.Where == "route_226");
        Assert.Contains(sinnoh.Of("Max Repel"), s => s.Kind == ItemSourceKind.Hidden && s.Where == "route_228");
        Assert.Contains(sinnoh.Of("Old Charm"), s => s.Kind == ItemSourceKind.Given && s.Where.StartsWith("route_210_south."));
        Assert.True(sinnoh.Has("TM06", ItemSourceKind.Prize));
        Assert.True(sinnoh.Has("Rare Candy", ItemSourceKind.Prize));
        // A region's own game: Kanto's Machine Part is found in Cerulean's Gym, and nowhere in Sinnoh
        Assert.False(sinnoh.Has("Machine Part"));
        var everywhere = ItemSources.Everywhere;
        Assert.Contains(everywhere.Of("Machine Part"), s => s.Kind == ItemSourceKind.Found);
        // A word of a script that names no item (find own) is no item
        Assert.False(everywhere.Has("own"));
    }

    [Fact]
    public void EveryToolCanBeHadInSinnohAfterItsHallOfFame()
    {
        var sinnoh = ItemSources.In(RegionDatabase.Sinnoh);
        var tools = Tools();
        Assert.Contains("Bottle Cap", tools);
        Assert.Contains("Jolly Mint", tools);
        var missing = tools.Where(t => !sinnoh.Has(t) && !Owed.ContainsKey(t)).ToList();
        Assert.True(missing.Count == 0, "no way of Sinnoh gives: " + string.Join(", ", missing));
        var stale = Owed.Keys.Where(sinnoh.Has).ToList();
        Assert.True(stale.Count == 0, "owed, yet Sinnoh gives them now (take their lines off): " + string.Join(", ", stale));
        var unknown = Owed.Keys.Where(k => !tools.Contains(k)).ToList();
        Assert.True(unknown.Count == 0, "owed, yet no tool of the data: " + string.Join(", ", unknown));
    }
}
