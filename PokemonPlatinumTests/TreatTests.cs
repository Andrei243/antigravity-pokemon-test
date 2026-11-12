using System;
using System.Linq;
using Xunit;
using PokemonPlatinumEngine.Battle.Effects;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using static PokemonPlatinumTests.Scenario;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 35 · BN3, treats: every treat a flavour or none and a grade, the taste's doubling and halving for each nature,
/// the baker's three berries and the mixed loaf, and a counter selling them in every finished region.
/// </summary>
public class TreatTests
{
    private const int Today = 9800;

    [Fact]
    public void ThereAreElevenTreatsEachAFlavourOrNoneAndAGrade()
    {
        var all = Treats.All.ToList();
        Assert.Equal(11, all.Count);
        foreach (var t in all)
        {
            Assert.InRange(t.Treat!.Grade, 1, 3);
            Assert.Equal(t.Treat.Grade == 3, t.Treat.Flavor == null);
            Assert.InRange(t.Id, 9600, 9699);
        }
        foreach (var f in Enum.GetValues<Flavor>())
        {
            Assert.NotNull(Treats.Of(f, 1));
            Assert.NotNull(Treats.Of(f, 2));
            Assert.True(Treats.Of(f, 1)!.Price > 0);
        }
        Assert.NotNull(Treats.Of(null, 3));
    }

    [Fact]
    public void ATreatsFriendshipIsDoubledForTheLikedFlavourAndHalvedForTheDislikedForEveryNature()
    {
        foreach (var nature in Enum.GetValues<Nature>())
        {
            var (liked, disliked) = Poffins.TasteOf(nature);
            foreach (var f in Enum.GetValues<Flavor>())
            {
                var treat = Treats.Of(f, 1)!;
                var got = Treats.FriendshipFor(treat, nature);
                var expected = f == liked ? (8, 6, 4) : f == disliked ? (2, 1, 1) : (4, 3, 2);
                Assert.Equal(expected, got);
            }
            var loaf = Treats.Of(null, 3)!;
            Assert.Equal(liked != null ? (16, 12, 8) : (8, 6, 4), Treats.FriendshipFor(loaf, nature));
        }
    }

    [Fact]
    public void ATreatFillsItsGradeAndALikedOneRaisesFriendshipTwiceAsMuch()
    {
        // Adamant likes spicy and dislikes dry
        var a = Mon("Turtwig", 10); a.Nature = Nature.Adamant; a.Friendship = 50;
        var b = Mon("Turtwig", 10); b.Nature = Nature.Adamant; b.Friendship = 50;
        var book = new CareBook();
        Assert.True(Treats.Feed(book.Of(a), a, Today, Treats.Of(Flavor.Spicy, 1)!));
        Assert.True(Treats.Feed(book.Of(b), b, Today, Treats.Of(Flavor.Sweet, 1)!));
        Assert.Equal(2 * (b.Friendship - 50), a.Friendship - 50);
        var c = Mon("Turtwig", 10);
        var page = book.Of(c);
        Assert.True(Treats.Feed(page, c, Today, Treats.Of(null, 3)!));
        Assert.Equal(3, CareRules.FullnessOn(page, Today));
        Assert.True(Treats.Feed(page, c, Today, Treats.Of(Flavor.Dry, 2)!));
        Assert.False(Treats.Feed(page, c, Today, Treats.Of(Flavor.Dry, 1)!));
    }

    [Fact]
    public void TheBakerTakesThreeBerriesOfTheFlavourTheBagHasMostOfAndRefusesWithout()
    {
        var bag = new Inventory();
        Assert.Null(Treats.Bake(bag, Treats.MostOf(bag), mixed: false));
        bag.AddItem(ItemDatabase.Get("Cheri Berry")!, 2);
        Assert.Null(Treats.MostOf(bag));
        bag.AddItem(ItemDatabase.Get("Chesto Berry")!, 4);
        Assert.Equal(Flavor.Dry, Treats.MostOf(bag));
        var made = Treats.Bake(bag, Treats.MostOf(bag), mixed: false);
        Assert.Equal(Treats.Of(Flavor.Dry, 2), made);
        Assert.Equal(1, bag.GetQuantity(ItemDatabase.Get("Chesto Berry")!));
        Assert.Equal(1, bag.GetQuantity(made!));
        // The loaf takes one of every flavour
        Assert.False(Treats.CanBake(bag, null, mixed: true));
        foreach (string berry in new[] { "Pecha Berry", "Rawst Berry", "Aspear Berry" }) bag.AddItem(ItemDatabase.Get(berry)!, 1);
        Assert.Equal(Treats.Of(null, 3), Treats.Bake(bag, null, mixed: true));
        Assert.Equal(1, bag.GetQuantity(ItemDatabase.Get("Cheri Berry")!));
    }

    [Fact]
    public void EveryFinishedRegionHasACounterSellingTheTreatsAndABaker()
    {
        MapDatabase.Initialize();
        var stock = MartDatabase.Specialties[Treats.Stock];
        Assert.Equal(5, stock.Count);
        foreach (var map in new[] { "VeilstoneStoreB1F", "Goldenrod_DeptStore_1F", "CeladonCity_DepartmentStore_1F", "LilycoveCity_DepartmentStore_1F", "CasteliaCity" })
        {
            var m = MapDatabase.Get(map);
            Assert.Equal(map, m.Name);
            var counter = m.Everyone.Single(n => n.Script == "TreatCounter");
            var baker = m.Everyone.Single(n => n.Script == "TreatBaker");
            foreach (var who in new[] { counter, baker })
            {
                // Someone can stand before them and talk (across a counter or face to face)
                var (dx, dy) = who.Facing switch { Direction.Up => (0, -1), Direction.Down => (0, 1), Direction.Left => (-1, 0), _ => (1, 0) };
                int x = who.GridX + dx, y = who.GridY + dy;
                bool across = m.BehaviourAt(x, y) == TileBehavior.Counter;
                if (across) { x += dx; y += dy; }
                Assert.True(m.IsWalkable(x, y), $"{map}: nobody can stand before the {who.Name} at {who.GridX},{who.GridY}");
            }
        }
    }

    [Fact]
    public void TheBakersScriptBakesThroughAHost()
    {
        var host = new HeadlessScriptHost();
        foreach (string berry in new[] { "Oran Berry", "Oran Berry", "Oran Berry" }) host.Bag.AddItem(ItemDatabase.Get(berry)!, 1);
        var script = ScriptParser.Parse("test", "script Bake\n  treat can most\n  if result == 0 goto No\n  treat bake most\n  say \"{item}\"\n  end\nlabel No\n  say \"no\"\n  end\n").First();
        var flavour = CareRules.StrongestFlavour(ItemDatabase.Get("Oran Berry")!);
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(script);
        runner.RunToEnd();
        Assert.Contains(host.Transcript, t => t.Text == Treats.Of(flavour, 2)!.DisplayName);
    }
}
