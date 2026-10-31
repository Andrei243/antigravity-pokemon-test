using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;
using static PokemonPlatinumTests.CoreScenario;
using static PokemonPlatinumTests.Scenario;

namespace PokemonPlatinumTests;

/// <summary>
/// Apricorns, the Box and Kurt's balls (plan 20 · J6): a tree that bears one Apricorn a calendar day, the Box that
/// keeps them, the one order Kurt can be making and the day it takes, the twenty-four trees of Johto's maps and the
/// scripts that read them. The clock is the game's own, fixed by the test.
/// </summary>
[Collection("Shared clocks and dice")]
public class ApricornTests : IDisposable
{
    private static readonly DateTime Monday = new(2026, 10, 19);
    private static readonly ScriptLibrary Scripts = ScriptLibrary.Default;
    private static readonly Lazy<World> Johto = new(() => World.LoadAll().Single(w => w.Index.Region == RegionDatabase.Johto));

    public ApricornTests() => GameClock.FixedDate = Monday;

    public void Dispose() => GameClock.FixedDate = null;

    private static int Day(int plus = 0) => ApricornBox.DayOf(Monday.AddDays(plus));

    // ---------------------------------------------------------------- the rules

    [Fact]
    public void ATreeBearsOneApricornAndNoMoreUntilTheNextDay()
    {
        var box = new ApricornBox();
        string tree = ApricornBox.TreeKey("route_37", "r37_bonguri");
        Assert.True(box.InFruit(tree, Day()));

        Assert.True(box.Pick(tree, Apricorn.Red, Day()));
        Assert.Equal(1, box.Count(Apricorn.Red));
        Assert.False(box.InFruit(tree, Day()));
        Assert.False(box.Pick(tree, Apricorn.Red, Day()));
        Assert.Equal(1, box.Count(Apricorn.Red));

        // The same calendar day, hours later: still bare. The next day: in fruit
        Assert.False(box.InFruit(tree, Day()));
        Assert.True(box.InFruit(tree, Day(1)));
        Assert.True(box.Pick(tree, Apricorn.Red, Day(1)));
        Assert.Equal(2, box.Count(Apricorn.Red));

        // Another tree is its own
        Assert.True(box.InFruit(ApricornBox.TreeKey("route_37", "r37_bonguri_2"), Day(1)));
    }

    [Fact]
    public void TheBoxHoldsNinetyNineOfAColourAndATreeKeepsItsFruitWhenItIsFull()
    {
        var box = new ApricornBox();
        for (int i = 0; i < ApricornBox.Limit; i++) Assert.True(box.Pick($"a/{i}", Apricorn.Blue, Day()));
        Assert.Equal(99, box.Count(Apricorn.Blue));
        Assert.True(box.IsFull(Apricorn.Blue));
        string tree = ApricornBox.TreeKey("route_30", "r30_bonguri");
        Assert.False(box.Pick(tree, Apricorn.Blue, Day()));
        Assert.True(box.InFruit(tree, Day()));
        Assert.True(box.Pick(tree, Apricorn.Black, Day()));
    }

    [Fact]
    public void KurtMakesBallsOfOneColourInADayAndOneOrderAtATime()
    {
        var box = new ApricornBox();
        for (int i = 0; i < 3; i++) box.Pick($"a/{i}", Apricorn.Red, Day());
        box.Pick("a/9", Apricorn.Pink, Day());

        Assert.Equal(0, box.Status(Day()));
        Assert.Equal(0, box.PlaceOrder(Apricorn.Green, Day()));    // none of that colour
        Assert.Equal(3, box.PlaceOrder(Apricorn.Red, Day()));      // every Red Apricorn
        Assert.Equal(0, box.Count(Apricorn.Red));
        Assert.Equal(0, box.PlaceOrder(Apricorn.Pink, Day()));     // a second order is refused while one is in hand
        Assert.Equal(1, box.Count(Apricorn.Pink));

        Assert.Equal(1, box.Status(Day()));
        Assert.Null(box.Collect(Day()));
        Assert.Equal(2, box.Status(Day(1)));
        Assert.Equal(0, box.PlaceOrder(Apricorn.Pink, Day(1)));    // ...and while the balls wait to be collected
        Assert.Equal(("Level Ball", 3), box.Collect(Day(1)));
        Assert.Equal(0, box.Status(Day(1)));
        Assert.Null(box.Collect(Day(1)));
        Assert.Equal(1, box.PlaceOrder(Apricorn.Pink, Day(1)));
    }

    [Fact]
    public void EachColourBecomesTheBallOfItsKind()
    {
        var expected = new Dictionary<Apricorn, string>
        {
            [Apricorn.Red] = "Level Ball", [Apricorn.Blue] = "Lure Ball", [Apricorn.Yellow] = "Moon Ball", [Apricorn.Green] = "Friend Ball",
            [Apricorn.Pink] = "Love Ball", [Apricorn.White] = "Fast Ball", [Apricorn.Black] = "Heavy Ball"
        };
        foreach (var colour in Enum.GetValues<Apricorn>())
        {
            Assert.Equal(expected[colour], ApricornData.BallOf(colour));
            Assert.Equal(ItemPocket.PokeBalls, ItemDatabase.Get(expected[colour])?.Pocket);
            Assert.Equal(7, ApricornData.File.Colours.Count);
        }
    }

    [Fact]
    public void KurtRingsOnceWhenTheBallsAreReady()
    {
        var box = new ApricornBox();
        box.Pick("a/1", Apricorn.White, Day());
        box.PlaceOrder(Apricorn.White, Day());
        Assert.False(box.TakeReadyCall(Day()));
        Assert.True(box.TakeReadyCall(Day(1)));
        Assert.False(box.TakeReadyCall(Day(1)));
        Assert.False(box.TakeReadyCall(Day(5)));
    }

    [Fact]
    public void TheBoxTheTreesAndTheOrderAreSaved()
    {
        var box = new ApricornBox();
        Assert.Null(box.ToSave());
        box.Pick("route_37/r37_bonguri", Apricorn.Red, Day());
        box.Pick("route_37/r37_bonguri_2", Apricorn.Red, Day());
        box.Pick("route_36/r36_bonguri", Apricorn.Blue, Day());
        box.PlaceOrder(Apricorn.Red, Day());

        var back = new ApricornBox();
        back.Load(GameDataFiles.Deserialize<ApricornSave>(GameDataFiles.Serialize(box.ToSave())));
        Assert.Equal(1, back.Count(Apricorn.Blue));
        Assert.Equal(0, back.Count(Apricorn.Red));
        Assert.Equal(1, back.Status(Day()));
        Assert.Equal(2, back.Status(Day(1)));
        Assert.False(back.InFruit("route_37/r37_bonguri", Day()));
        Assert.True(back.InFruit("route_37/r37_bonguri", Day(1)));

        // A new game, or a save from before them, has none
        back.Load(null);
        Assert.Equal(0, back.Total);
        Assert.Null(back.ToSave());
    }

    // ---------------------------------------------------------------- the trees on the maps

    [Fact]
    public void EveryApricornTreeOfJohtosMapsIsAThingOfTheMapWithItsTablesColour()
    {
        var trees = Johto.Value.BuildMaps().SelectMany(m => m.Everyone).Where(n => n.IsApricornTree).ToList();
        Assert.Equal(ApricornData.File.Trees.Count, trees.Count);
        // Johto's 24, and HeartGold's Kanto's on Route 8 since plan 20 · J15, part d and Pewter City's two and Route 2's since part f
        // and Route 1's since part g and Fuchsia City's since part h
        Assert.Equal(30, trees.Count);
        Assert.All(trees, t =>
        {
            Assert.Equal(ApricornData.ColourOf(t.ScriptFile!, t.Key!), t.ApricornColour);
            Assert.True(t.IsThing);
            Assert.Equal(FieldScripts.ApricornTree, FieldScripts.For(t));
        });
        Assert.Equal(7, trees.Select(t => t.ApricornColour).Distinct().Count());
        // Route 37's three bear Red, the colour of a Level Ball
        Assert.Equal(3, trees.Count(t => t.ScriptFile == "route_37" && t.ApricornColour == Apricorn.Red));
    }

    [Fact]
    public void ATreeIsToldWhetherItsFruitIsOn()
    {
        var map = Johto.Value.BuildMaps().First(m => m.Everyone.Any(n => n.IsApricornTree));
        var box = new ApricornBox();
        var tree = map.NPCs.First(n => n.IsApricornTree);
        GameEngine.ShowApricornTrees(map, box, Day());
        Assert.True(tree.ApricornInFruit);

        box.Pick(ApricornBox.TreeKey(tree.ScriptFile!, tree.Key!), tree.ApricornColour!.Value, Day());
        GameEngine.ShowApricornTrees(map, box, Day());
        Assert.False(tree.ApricornInFruit);
        GameEngine.ShowApricornTrees(map, box, Day(1));
        Assert.True(tree.ApricornInFruit);
    }

    [Fact]
    public void TheTreesLookIsAFruitingOneAndABareOne()
    {
        foreach (var colour in Enum.GetValues<Apricorn>())
        {
            var fruit = ApricornArt.Paint(colour, true);
            Assert.Equal((ApricornArt.Width, ApricornArt.Height), (fruit.Width, fruit.Height));
            Assert.NotEqual(ApricornArt.KeyOf(colour, true), ApricornArt.KeyOf(colour, false));
        }
        Assert.Equal(ApricornArt.KeyOf(Apricorn.Red, false), ApricornArt.KeyOf(Apricorn.Blue, false));
    }

    // ---------------------------------------------------------------- the scripts

    private static NPC Tree(string area = "route_37", string id = "r37_bonguri", Apricorn colour = Apricorn.Red) => new()
    {
        Name = "Apricorn tree", NpcType = NPC.ApricornTreeType, ApricornColour = colour, Key = id, ScriptFile = area
    };

    private static HeadlessScriptHost Play(HeadlessScriptHost host, string script, string? file, NPC? subject)
    {
        var runner = new ScriptRunner(Scripts, host);
        runner.Start(Scripts.Find(script, file)!, subject);
        runner.RunToEnd();
        Assert.Empty(host.Problems);
        return host;
    }

    private static string Said(HeadlessScriptHost host) => string.Join(" | ", host.Transcript.Select(t => t.Text));

    private static HeadlessScriptHost WithBox(HeadlessScriptHost? host = null)
    {
        host ??= new HeadlessScriptHost();
        host.Bag.AddItem(ItemDatabase.Get(GameEngine.ApricornBoxItem)!, 1);
        return host;
    }

    [Fact]
    public void ATreeIsPickedIntoTheBoxOnceADay()
    {
        var host = WithBox();
        var tree = Tree();
        Play(host, FieldScripts.ApricornTree, null, tree);
        Assert.Equal(1, host.Apricorns.Count(Apricorn.Red));
        Assert.Contains("There's a Red Apricorn hanging on the tree!", Said(host));
        Assert.Contains("You picked the Red Apricorn.", Said(host));

        var again = Play(WithBox(new HeadlessScriptHost { Apricorns = host.Apricorns }), FieldScripts.ApricornTree, null, tree);
        Assert.Equal(1, host.Apricorns.Count(Apricorn.Red));
        Assert.Contains("no fruit on it", Said(again));

        GameClock.FixedDate = Monday.AddDays(1);
        Play(WithBox(new HeadlessScriptHost { Apricorns = host.Apricorns }), FieldScripts.ApricornTree, null, tree);
        Assert.Equal(2, host.Apricorns.Count(Apricorn.Red));
    }

    [Fact]
    public void FruitCanBeLeftOnTheTreeAndNothingIsPickedWithoutABox()
    {
        var host = WithBox();
        host.Answers.Enqueue(1);
        Play(host, FieldScripts.ApricornTree, null, Tree());
        Assert.Equal(0, host.Apricorns.Total);

        var boxless = Play(new HeadlessScriptHost(), FieldScripts.ApricornTree, null, Tree());
        Assert.Equal(0, boxless.Apricorns.Total);
        Assert.Contains("If only you had something to keep the fruit in", Said(boxless));
    }

    [Fact]
    public void AFullBoxLeavesTheFruitOnTheTree()
    {
        var host = WithBox();
        for (int i = 0; i < ApricornBox.Limit; i++) host.Apricorns.Pick($"a/{i}", Apricorn.Red, Day());
        Play(host, FieldScripts.ApricornTree, null, Tree());
        Assert.Equal(99, host.Apricorns.Count(Apricorn.Red));
        Assert.True(host.Apricorns.InFruit("route_37/r37_bonguri", Day()));
        Assert.Contains("can't hold any more", Said(host));
    }

    [Fact]
    public void TheManOnRoute30GivesTheBoxOnce()
    {
        var host = Play(new HeadlessScriptHost(), "ApricornMan", "route_30_apricorn_house", null);
        var box = ItemDatabase.Get(GameEngine.ApricornBoxItem)!;
        Assert.Equal(1, host.Bag.GetQuantity(box));
        Play(host, "ApricornMan", "route_30_apricorn_house", null);
        Assert.Equal(1, host.Bag.GetQuantity(box));
        Assert.Equal(ItemPocket.KeyItems, box.Pocket);
        Assert.Equal("ApricornBox", box.FieldUse);
        Assert.True(BagScreen.UsedInField(box));
    }

    // Kurt's house: his script on the map the game lays out, as he stands in it
    private static (Map Map, NPC Kurt) KurtsHouse()
    {
        var map = Johto.Value.BuildMaps().Single(m => m.Name == "Azalea_KurtsHouse");
        return (map, map.NPCs.Single(n => n.Key == "t23r0501_gantetsu"));
    }

    private static HeadlessScriptHost TalkToKurt(ApricornBox box, Inventory? bag = null, params int[] answers)
    {
        var (map, kurt) = KurtsHouse();
        var story = new PokemonPlatinumEngine.Story.StoryState();
        story.Set("FLAG_BEAT_AZALEA_ROCKETS");
        var host = new HeadlessScriptHost(story, new Party(), bag ?? new Inventory()) { Map = map, Apricorns = box };
        foreach (int a in answers) host.Answers.Enqueue(a);
        return Play(host, "Kurt", "azalea_kurt_house", kurt);
    }

    [Fact]
    public void KurtTurnsARedApricornIntoALevelBallByTomorrowAndRefusesASecondOrder()
    {
        var box = new ApricornBox();
        box.Pick("route_37/r37_bonguri", Apricorn.Red, Day());
        box.Pick("route_37/r37_bonguri_2", Apricorn.Red, Day());
        box.Pick("route_36/r36_bonguri", Apricorn.Blue, Day());
        var bag = new Inventory();

        // Yes, make some; the first colour, Red
        var ordered = TalkToKurt(box, bag, 0, 0);
        Assert.Equal(0, box.Count(Apricorn.Red));
        Assert.Equal(1, box.Count(Apricorn.Blue));
        Assert.Equal(1, box.Status(Day()));
        Assert.Contains("2 × Level Ball", Said(ordered));
        Assert.Contains("Come back tomorrow", Said(ordered));
        Assert.Equal(0, bag.GetQuantity(ItemDatabase.Get("Level Ball")!));

        // The same day: he is still at work, and takes no second order
        var working = TalkToKurt(box, bag);
        Assert.Contains("still in the works", Said(working));
        Assert.Equal(1, box.Count(Apricorn.Blue));

        // The next day: the balls
        GameClock.FixedDate = Monday.AddDays(1);
        var ready = TalkToKurt(box, bag);
        Assert.Equal(2, bag.GetQuantity(ItemDatabase.Get("Level Ball")!));
        Assert.Contains("2 × Level Ball", Said(ready));
        Assert.Equal(0, box.Status(Day(1)));

        // ...and he takes a new one
        TalkToKurt(box, bag, 0, 1);
        Assert.Equal(0, box.Count(Apricorn.Blue));
        Assert.Equal(1, box.Status(Day(1)));
    }

    [Fact]
    public void KurtWantsAnApricornAndAnswersNoOneWhoHasNone()
    {
        var empty = TalkToKurt(new ApricornBox());
        Assert.Contains("Pick them from the trees", Said(empty));

        var box = new ApricornBox();
        box.Pick("a/1", Apricorn.Black, Day());
        var declined = TalkToKurt(box, null, 1);
        Assert.Equal(1, box.Count(Apricorn.Black));
        Assert.Equal(0, box.Status(Day()));
        Assert.Contains("No hurry", Said(declined));
    }

    [Fact]
    public void ABlackApricornIsReachedOnTheSecondPageOfColours()
    {
        var box = new ApricornBox();
        box.Pick("a/1", Apricorn.Black, Day());
        // Yes; "Other colours"; Black
        TalkToKurt(box, null, 0, 4, 2);
        Assert.Equal(("Heavy Ball", 1), (ApricornData.BallOf(Apricorn.Black), box.Order!.Count));
        Assert.Equal(1, box.Status(Day()));
    }

    [Fact]
    public void KurtOffersHisNumberToAPlayerWithAPokegearAndRingsWhenTheBallsAreDone()
    {
        var (map, kurt) = KurtsHouse();
        var story = new PokemonPlatinumEngine.Story.StoryState();
        story.Set("FLAG_BEAT_AZALEA_ROCKETS");
        story.Set("FLAG_GOT_POKEGEAR");
        var box = new ApricornBox();
        box.Pick("a/1", Apricorn.Yellow, Day());
        var host = new HeadlessScriptHost(story, new Party(), new Inventory()) { Map = map, Apricorns = box };
        foreach (int a in new[] { 0, 2, 0 }) host.Answers.Enqueue(a);
        Play(host, "Kurt", "azalea_kurt_house", kurt);
        Assert.True(story.Has(Pokegear.ContactFlag(GameEngine.KurtContact)));
        Assert.NotNull(PhoneBook.Get(GameEngine.KurtContact));

        // His call, once the day has passed
        GameClock.FixedDate = Monday.AddDays(1);
        Assert.True(box.TakeReadyCall(Day(1)));
        var call = Play(new HeadlessScriptHost(story, new Party(), new Inventory()) { Apricorns = box }, "Kurt", GameEngine.PhoneScripts, null);
        Assert.Contains("Moon Ball are done", Said(call));
        Assert.Contains("Moon Ball are done", Said(call));
    }

    [Fact]
    public void ANewGamesBoxIsEmpty()
    {
        var host = new HeadlessScriptHost();
        Assert.Equal(0, host.Apricorns.Total);
        Assert.Null(host.Apricorns.Order);
        Assert.Null(host.Apricorns.ToSave());
    }

    [Fact]
    public void TheBoxScreenListsTheSevenColours()
    {
        var box = new ApricornBox();
        box.Pick("a/1", Apricorn.Green, Day());
        var screen = new ApricornBoxScreen();
        screen.Open(box);
        Assert.True(screen.IsActive);
        Assert.Equal(Apricorn.Green, screen.Selected);   // the first colour held
        screen.Move(1);
        Assert.Equal(Apricorn.Pink, screen.Selected);
        screen.Move(-1);
        screen.Move(-1);
        Assert.Equal(Apricorn.Yellow, screen.Selected);
        screen.Cancel();
        Assert.False(screen.IsActive);
        Assert.Equal(7, ApricornBoxScreen.Colours.Length);
    }
}

/// <summary>
/// Johto's seven balls thrown (plan 20 · J6; the rules are plan 06 · R28a's, <c>CatchCalculator.Later.cs</c>): each
/// ball's bonus moves the catch only where its condition holds, and a Friend Ball's catch is a friend.
/// </summary>
public class ApricornBallTests
{
    private static Pokemon Of(string species, int level, Gender gender) => new(PokemonDatabase.Get(species)!, level, gender, Nature.Hardy, false);

    /// <summary>The roll that holds with the better rate and not with the worse: the four shakes are all rolled against it.</summary>
    private static int RollBetween(int speciesRate, int worse, int better, Pokemon foe)
    {
        int low = Formulas.ShakeRate(Formulas.CatchRate(speciesRate, worse, foe.MaxHP, foe.CurrentHP, foe.Status));
        int high = Formulas.ShakeRate(Formulas.CatchRate(speciesRate, better, foe.MaxHP, foe.CurrentHP, foe.Status));
        Assert.True(high > low, $"{high} is not above {low}");
        return low;
    }

    /// <summary>A ball thrown at a wild Pokémon under the modern rules, every shake rolled as given: whether it held.</summary>
    private static bool Caught(string ball, Pokemon mine, Pokemon foe, int roll, bool fishing = false)
    {
        var party = new Party();
        party.Add(mine);
        var core = new BattleCore(new CoreSetup
        {
            PlayerParty = party, WildPokemon = new List<Pokemon> { foe }, Random = Steady().Force(RollKind.SideEffect, 99).Force(RollKind.CatchShake, roll),
            Rules = Ruleset.Modern, Conditions = new BattleConditions { Fishing = fishing }, PlayerController = null, EnemyController = null, PlayerName = "Lucas"
        });
        core.Start();
        core.TakeLog();
        core.Submit(BattleChoice.UseItem(Mine, ball), BattleChoice.Fight(Foe, 0));
        return core.Result == BattleResult.EnemyCaught;
    }

    [Fact]
    public void AFastBallHoldsBetterOnAFastPokemonAndAPokeBallDoesNot()
    {
        var jolteon = Of("Jolteon", 30, Gender.Male);
        int roll = RollBetween(jolteon.CatchRate, 10, 40, jolteon);
        Assert.True(Caught("Fast Ball", Mon("Pikachu", 30), jolteon, roll));
        Assert.False(Caught("Poké Ball", Mon("Pikachu", 30), Of("Jolteon", 30, Gender.Male), roll));
        // A slow Pokémon gets nothing from it
        var snorlax = Of("Snorlax", 30, Gender.Male);
        Assert.False(Caught("Fast Ball", Mon("Pikachu", 30), snorlax, RollBetween(snorlax.CatchRate, 10, 40, snorlax)));
    }

    [Fact]
    public void ALevelBallGoesByHowFarTheThrowersLevelIsAbove()
    {
        var foe = Of("Snorlax", 10, Gender.Male);
        int roll = RollBetween(foe.CatchRate, 10, 80, foe);
        Assert.True(Caught("Level Ball", Mon("Machamp", 50), foe, roll));
        Assert.False(Caught("Level Ball", Mon("Machamp", 10), Of("Snorlax", 10, Gender.Male), roll));
    }

    [Fact]
    public void ALureBallDoesNothingUnlessTheBattleBeganOnARod()
    {
        var foe = Of("Snorlax", 20, Gender.Male);
        int roll = RollBetween(foe.CatchRate, 10, 40, foe);
        Assert.False(Caught("Lure Ball", Mon("Pikachu", 30), foe, roll, fishing: false));
        Assert.True(Caught("Lure Ball", Mon("Pikachu", 30), Of("Snorlax", 20, Gender.Male), roll, fishing: true));
    }

    [Fact]
    public void AMoonBallHoldsBetterOnAPokemonThatEvolvesWithAMoonStone()
    {
        var nidorino = Of("Nidorino", 20, Gender.Male);
        int roll = RollBetween(nidorino.CatchRate, 10, 40, nidorino);
        Assert.True(Caught("Moon Ball", Mon("Pikachu", 30), nidorino, roll));
        var snorlax = Of("Snorlax", 20, Gender.Male);
        Assert.False(Caught("Moon Ball", Mon("Pikachu", 30), snorlax, RollBetween(snorlax.CatchRate, 10, 40, snorlax)));
    }

    [Fact]
    public void ALoveBallNeedsTheSameSpeciesOfTheOtherGender()
    {
        var foe = Of("Snorlax", 20, Gender.Female);
        int roll = RollBetween(foe.CatchRate, 10, 80, foe);
        Assert.True(Caught("Love Ball", Of("Snorlax", 40, Gender.Male), foe, roll));
        Assert.False(Caught("Love Ball", Of("Snorlax", 40, Gender.Female), Of("Snorlax", 20, Gender.Female), roll));
        Assert.False(Caught("Love Ball", Of("Machamp", 40, Gender.Male), Of("Snorlax", 20, Gender.Female), roll));
    }

    [Fact]
    public void AHeavyBallAddsToTheCatchRateByWeight()
    {
        var snorlax = Of("Snorlax", 20, Gender.Male);
        // 460 kg: 30 more on the species' rate
        int low = Formulas.ShakeRate(Formulas.CatchRate(snorlax.CatchRate, 10, snorlax.MaxHP, snorlax.CurrentHP, snorlax.Status));
        int high = Formulas.ShakeRate(Formulas.CatchRate(snorlax.CatchRate + 30, 10, snorlax.MaxHP, snorlax.CurrentHP, snorlax.Status));
        Assert.True(high > low);
        Assert.True(Caught("Heavy Ball", Mon("Pikachu", 30), snorlax, low));
        Assert.False(Caught("Poké Ball", Mon("Pikachu", 30), Of("Snorlax", 20, Gender.Male), low));
    }

    [Fact]
    public void AFriendBallsCatchIsAFriendAtTwoHundred()
    {
        var party = new Party();
        party.Add(Mon("Pikachu", 30));
        var foe = Of("Bidoof", 5, Gender.Male);
        var core = new BattleCore(new CoreSetup
        {
            PlayerParty = party, WildPokemon = new List<Pokemon> { foe }, Random = Steady().Force(RollKind.SideEffect, 99).Force(RollKind.CatchShake, 0),
            Rules = Ruleset.Modern, Conditions = new BattleConditions(), PlayerController = null, EnemyController = null, PlayerName = "Lucas"
        });
        core.Start();
        core.TakeLog();
        core.Submit(BattleChoice.UseItem(Mine, "Friend Ball"), BattleChoice.Fight(Foe, 0));
        Assert.Equal(BattleResult.EnemyCaught, core.Result);
        Assert.Equal(200, core.PlayerParty.Members.Last().Friendship);
        Assert.Equal("Friend Ball", core.PlayerParty.Members.Last().Ball);
    }

    [Fact]
    public void EveryApricornBallHasALookOfItsOwn()
    {
        string[] balls = { "Fast Ball", "Level Ball", "Lure Ball", "Heavy Ball", "Love Ball", "Friend Ball", "Moon Ball" };
        var looks = balls.Select(BattleBall.LookOf).ToList();
        Assert.Equal(7, looks.Select(l => l.Kind).Distinct().Count());
        Assert.DoesNotContain(BallKind.Poke, looks.Select(l => l.Kind));
        Assert.Equal(7, looks.Distinct().Count());
        foreach (string ball in balls) Assert.Equal(ball, ItemDatabase.Get(ball)!.Name);
    }
}
