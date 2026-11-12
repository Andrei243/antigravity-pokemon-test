using System.Linq;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Story;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 22 · U11's leftovers: Join Avenue's growth (rulings, "Join Avenue's growth"), and the grottoes' rustle and sounds
/// (rulings, "The Hidden Grottoes").
/// </summary>
public class JoinAvenueTests
{
    private static HeadlessScriptHost Play(StoryState story, string script, params int[] answers)
    {
        var host = new HeadlessScriptHost(story, new Party(), new Inventory());
        foreach (int a in answers) host.Answers.Enqueue(a);
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find(script, "join_avenue")!, new PokemonPlatinumEngine.Overworld.NPC { Key = "0", Name = "Owner" });
        runner.RunToEnd();
        Assert.Empty(host.Problems);
        return host;
    }

    [Fact]
    public void TheAvenuesRankGrowsWithItsVisitors()
    {
        Assert.Equal(new[] { 1, 1, 2, 5, 10, 10 }, new[] { 0, 9, 10, 45, 90, 500 }.Select(JoinAvenue.RankOf));
        Assert.Equal(new[] { 3, 7, 12 }, new[] { 1, 5, 10 }.Select(JoinAvenue.VisitorsOn));
        Assert.Equal(new[] { 10, 1, 0 }, new[] { 0, 19, 95 }.Select(JoinAvenue.ToNextRank));
        // The shops widen at their levels, the shop girls' open at their ranks; every stock is a list of the marts
        Assert.Equal("join_avenue_market", JoinAvenue.StockAt("market", 1));
        Assert.Equal("join_avenue_market_2", JoinAvenue.StockAt("market", 2));
        Assert.Equal("join_avenue_market_3", JoinAvenue.StockAt("market", 4));
        var quiet = new StoryState();
        quiet.SetVar(JoinAvenue.Vars.Visitors, (JoinAvenue.StonesRank - 1) * JoinAvenue.VisitorsARank - 1);
        Assert.Null(JoinAvenue.StockOf(quiet, "stones"));
        Assert.Null(JoinAvenue.StockOf(quiet, "vitamins"));
        Assert.Equal("join_avenue_market", JoinAvenue.StockOf(quiet, "market"));
        foreach (string shop in new[] { "market", "flowers", "cafe", "stones", "vitamins", "antiques" })
            for (int level = 1; level <= JoinAvenue.TopLevel; level++)
                if (JoinAvenue.StockAt(shop, level) is { } stock)
                {
                    var items = MartDatabase.Specialties.GetValueOrDefault(stock);
                    Assert.NotNull(items);
                    Assert.All(items!, name => Assert.NotNull(ItemDatabase.Get(name)));
                }
    }

    [Fact]
    public void VisitorsComeOnceADayToAManagedAvenue()
    {
        var story = new StoryState();
        // No manager, no visitors
        Assert.Equal(0, JoinAvenue.Visit(story));
        story.Set(JoinAvenue.ManagerFlag);
        Assert.Equal(3, JoinAvenue.Visit(story));
        Assert.Equal(0, JoinAvenue.Visit(story));
        // a new day clears the day's flag
        story.ClearDaily();
        Assert.Equal(3, JoinAvenue.Visit(story));
        Assert.Equal(6, story.Var(JoinAvenue.Vars.Visitors));
        // through the ranks by days alone, to the top
        int days = 2;
        while (JoinAvenue.Rank(story) < JoinAvenue.TopRank) { story.ClearDaily(); JoinAvenue.Visit(story); days++; }
        Assert.InRange(days, 12, 20);
    }

    [Fact]
    public void TheOwnerMakesThePlayerManagerAndTheShopsOpenWithTheRank()
    {
        var story = new StoryState();
        var asked = Play(story, "Owner", 0);
        Assert.True(story.Has(JoinAvenue.ManagerFlag));
        Assert.Equal(3, story.Var(JoinAvenue.Vars.Visitors));
        Assert.Contains(asked.Transcript, l => l.Text.Contains("yours to run"));
        var report = Play(story, "Owner");
        Assert.Contains(report.Transcript, l => l.Text.Contains("3 visitors"));
        Assert.Contains(report.Transcript, l => l.Text.Contains("7 more visitors"));

        // The shop girls' shops are shut until their ranks
        Assert.Contains(Play(story, "Stones").Transcript, l => l.Text.Contains("isn't open yet"));
        story.SetVar(JoinAvenue.Vars.Visitors, (JoinAvenue.StonesRank - 1) * JoinAvenue.VisitorsARank);
        var stones = Play(story, "Stones");
        Assert.Contains(stones.Log, l => l.Contains("join_avenue_stones"));
        // the market's stock goes by what was spent in it, not by the rank
        Assert.Contains(Play(story, "Market").Log, l => l.EndsWith("join_avenue_market"));
        story.SetVar(JoinAvenue.Vars.Spent("market"), JoinAvenue.GoodsLevels[2]);
        Assert.Contains(Play(story, "Market").Log, l => l.Contains("join_avenue_market_3"));
        story.SetVar(JoinAvenue.Vars.Visitors, 1000);
        Assert.Contains(Play(story, "Vitamins").Log, l => l.Contains("join_avenue_vitamins"));
        Assert.Contains(Play(story, "Owner").Transcript, l => l.Text.Contains("as high as an avenue goes"));
    }

    // ------------------------------------------------------------------ the other shops and the visitors (U11's leftovers)

    private static HeadlessScriptHost Shop(StoryState story, Party party, string script, int money = 3000, params int[] answers)
    {
        var host = new HeadlessScriptHost(story, party, new Inventory()) { Money = money };
        foreach (int a in answers) host.Answers.Enqueue(a);
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find(script, "join_avenue")!, new PokemonPlatinumEngine.Overworld.NPC { Key = script.ToLowerInvariant(), Name = script });
        runner.RunToEnd();
        Assert.Empty(host.Problems);
        return host;
    }

    [Fact]
    public void TheOtherShopsDoMoreAtTheirLevels()
    {
        Assert.Equal(new[] { 4, 8, 20 }, new[] { 1, 2, 5 }.Select(JoinAvenue.DojoEffort));
        Assert.Equal(new[] { 10, 15, 30 }, new[] { 1, 2, 5 }.Select(JoinAvenue.SalonFriendship));
        Assert.Equal(new[] { 2, 8 }, new[] { 4, 10 }.Select(JoinAvenue.NurseryCycles));
        Assert.Equal("join_avenue_antiques", JoinAvenue.StockAt("antiques", 2));
        Assert.Equal("join_avenue_antiques_2", JoinAvenue.StockAt("antiques", 3));
        foreach (string stock in new[] { "join_avenue_antiques", "join_avenue_antiques_2" })
            Assert.All(MartDatabase.Specialties[stock], name => Assert.NotNull(ItemDatabase.Get(name)));
        // The raffle: the lowest prize at the bottom, the best at the top, and the level moves a draw up
        Assert.Equal("Potion", JoinAvenue.RafflePrize(0, 1));
        Assert.Equal("Rare Candy", JoinAvenue.RafflePrize(99, 1));
        Assert.Equal("Great Ball", JoinAvenue.RafflePrize(35, 1));
        Assert.Equal("Super Potion", JoinAvenue.RafflePrize(36, 5));
        Assert.All(JoinAvenue.RafflePrizes, p => Assert.NotNull(ItemDatabase.Get(p.Item)));
    }

    [Fact]
    public void AShopRisesALevelWithTheMoneySpentInIt()
    {
        Assert.Equal(new[] { 1, 1, 2, 4, 5, 5 }, new[] { 0, 4999, 5000, 30000, 60000, 1_000_000 }.Select(s => JoinAvenue.LevelAt("market", s)));
        Assert.Equal(new[] { 1, 2, 5 }, new[] { 1999, 2000, 20000 }.Select(s => JoinAvenue.LevelAt("dojo", s)));
        var story = new StoryState();
        // What the money fell by while the shop was open is the shop's; a sale that leaves more in hand adds nothing
        Assert.Equal(3500, JoinAvenue.Tally(story, "market", 10000, 6500));
        Assert.Equal(0, JoinAvenue.Tally(story, "market", 6500, 8000));
        Assert.Equal(3500, story.Var(JoinAvenue.Vars.Spent("market")));
        Assert.Equal(1500, JoinAvenue.ToNextLevel(story, "market"));
        // The raffle rises with what the whole avenue spends
        story.SetVar(JoinAvenue.Vars.Spent("dojo"), 7000);
        Assert.Equal(10500, JoinAvenue.SpentIn(story, "raffle"));
        Assert.Equal(2, JoinAvenue.LevelOf(story, "raffle"));
        // and a shop's script counts what was paid in it
        story.SetVar(JoinAvenue.Vars.Visitors, 1000);
        var party = new Party();
        party.Add(new Pokemon(PokemonDatabase.Get("Snivy")!, 20));
        Shop(story, party, "Dojo", 3000, 1);
        Assert.Equal(8000, story.Var(JoinAvenue.Vars.Spent("dojo")));
        Assert.Equal(3, JoinAvenue.LevelOf(story, "dojo"));
    }

    [Fact]
    public void AVisitorInvitedOpensAShopAsTheRankHasRoom()
    {
        // every visitor would open one of the five, and each of the five is offered by someone
        Assert.Equal(JoinAvenue.Invited.OrderBy(s => s), Enumerable.Range(0, JoinAvenue.VisitorList).Select(JoinAvenue.OfferOf).Distinct().OrderBy(s => s));
        Assert.Equal(new[] { 1, 2, 5, 5 }, new[] { 1, 2, 5, 10 }.Select(JoinAvenue.RoomAt));
        var story = new StoryState();
        story.Set(JoinAvenue.InvitationsFlag);
        story.Set(JoinAvenue.ManagerFlag);
        story.SetVar(JoinAvenue.Vars.Visitors, 3);
        // At rank 1 there is room for one: the first visitor's shop opens when asked and let
        string first = JoinAvenue.OfferOf(JoinAvenue.VisitorAt(1, 3));
        Assert.False(JoinAvenue.IsOpen(story, first));
        var asked = Shop(story, new Party(), "Visitor1", 3000, 0);
        Assert.True(story.Has(JoinAvenue.OpenFlag(first)));
        Assert.True(JoinAvenue.IsOpen(story, first));
        Assert.Contains(asked.Transcript, l => l.Text.Contains("ready in no time"));
        // asked again, the visitor only greets
        Assert.Single(Shop(story, new Party(), "Visitor1").Transcript);
        // no room for a second at rank 1, whoever asks
        string second = JoinAvenue.OfferOf(JoinAvenue.VisitorAt(2, 3));
        Assert.NotEqual(first, second);
        Assert.Contains(Shop(story, new Party(), "Visitor2", 3000, 0).Transcript, l => l.Text.Contains("isn't busy enough"));
        Assert.False(story.Has(JoinAvenue.OpenFlag(second)));
        // declined, nothing opens
        story.SetVar(JoinAvenue.Vars.Visitors, 10);
        int place = Enumerable.Range(1, 3).First(p => !JoinAvenue.IsOpen(story, JoinAvenue.OfferOf(JoinAvenue.VisitorAt(p, 10))));
        string declined = JoinAvenue.OfferOf(JoinAvenue.VisitorAt(place, 10));
        Assert.Contains(Shop(story, new Party(), $"Visitor{place}", 3000, 1).Transcript, l => l.Text.Contains("change your mind"));
        Assert.False(story.Has(JoinAvenue.OpenFlag(declined)));
        Assert.Equal(1, JoinAvenue.InvitedOpen(story));
    }

    [Fact]
    public void EveryShopCanBeOpenedByTheDaysVisitorsAsTheAvenueGrows()
    {
        var story = new StoryState();
        story.Set(JoinAvenue.InvitationsFlag);
        story.Set(JoinAvenue.ManagerFlag);
        int days = 0;
        while (JoinAvenue.InvitedOpen(story) < JoinAvenue.Invited.Length && days < 40)
        {
            story.ClearDaily();
            JoinAvenue.Visit(story);
            days++;
            for (int place = 1; place <= JoinAvenue.VisitorsStanding; place++)
            {
                int offer = JoinAvenue.Offer(story, place);
                if (offer is > JoinAvenue.NoOffer and < JoinAvenue.NoRoom) story.Set(JoinAvenue.OpenFlag(JoinAvenue.Invited[offer - 1]));
            }
        }
        Assert.Equal(JoinAvenue.Invited.Length, JoinAvenue.InvitedOpen(story));
        Assert.InRange(JoinAvenue.Rank(story), 5, 6);
    }

    [Fact]
    public void AnAvenueManagedBeforeInvitationsKeepsTheShopsItsRankHadOpened()
    {
        var story = new StoryState();
        story.Set(JoinAvenue.ManagerFlag);
        story.SetVar(JoinAvenue.Vars.Visitors, 30);  // rank 4
        JoinAvenue.Visit(story);
        Assert.True(story.Has(JoinAvenue.InvitationsFlag));
        Assert.Equal(new[] { true, true, true, true, false }, JoinAvenue.Invited.Select(s => JoinAvenue.IsOpen(story, s)));
        // and the shops keep what rank 4 gave them: the market's second stock, the flowers', the dojo's 8 effort
        Assert.Equal("join_avenue_market_2", JoinAvenue.StockOf(story, "market"));
        Assert.Equal("join_avenue_flowers_2", JoinAvenue.StockOf(story, "flowers"));
        Assert.Equal("join_avenue_cafe", JoinAvenue.StockOf(story, "cafe"));
        Assert.Equal(8, JoinAvenue.DojoEffort(JoinAvenue.LevelOf(story, "dojo")));
        Assert.Equal(1, JoinAvenue.LevelOf(story, "salon"));
        // a new manager has none open until a visitor is invited
        var fresh = new StoryState();
        Play(fresh, "Owner", 0);
        Assert.Equal(0, JoinAvenue.InvitedOpen(fresh));
    }

    [Fact]
    public void TheDojoTrainsAStatForItsFeeUpToTheCap()
    {
        var story = new StoryState();
        story.SetVar(JoinAvenue.Vars.Visitors, 1000);
        story.SetVar(JoinAvenue.Vars.Spent("dojo"), JoinAvenue.ServiceLevels[^1]);
        var party = new Party();
        party.Add(new Pokemon(PokemonDatabase.Get("Snivy")!, 20));
        // ATTACK, the first of the team: 20 effort at the top level, the fee taken
        var host = Shop(story, party, "Dojo", 3000, 1);
        Assert.Equal(20, party.Members[0].EvAttack);
        Assert.Equal(2000, host.Money);
        Assert.Contains(host.Transcript, l => l.Text.Contains("came out tougher"));
        // At the rules' cap of a stat it gains nothing and pays nothing
        party.Members[0].EvAttack = Ruleset.Current.EvStatCap;
        var full = Shop(story, party, "Dojo", 3000, 1);
        Assert.Equal(3000, full.Money);
        Assert.Contains(full.Transcript, l => l.Text.Contains("can't grow any tougher"));
        // Short of the fee
        Assert.Contains(Shop(story, party, "Dojo", 500).Transcript, l => l.Text.Contains("short of the fee"));
    }

    [Fact]
    public void TheSalonGroomsAndTheNurseryWarmsEggsOnceADay()
    {
        var story = new StoryState();
        story.SetVar(JoinAvenue.Vars.Visitors, 1000);
        var party = new Party();
        var snivy = new Pokemon(PokemonDatabase.Get("Snivy")!, 20) { Friendship = 70 };
        party.Add(snivy);
        var salon = Shop(story, party, "Salon", 3000, 0);
        Assert.Equal(70 + JoinAvenue.SalonFriendship(1), snivy.Friendship);
        Assert.Equal(JoinAvenue.SalonPrice, story.Var(JoinAvenue.Vars.Spent("salon")));
        Assert.Equal(2500, salon.Money);

        // An Egg warmed by the top rank's cycles, never under one, once a day
        var egg = new Pokemon(PokemonDatabase.Get("Snivy")!, 1) { IsEgg = true };
        egg.EggCycles = 20;
        party.Add(egg);
        var nursery = Shop(story, party, "Nursery");
        Assert.Equal(20 - JoinAvenue.NurseryCycles(10), egg.EggCycles);
        Assert.Contains(nursery.Transcript, l => l.Text.Contains("hatch all the sooner"));
        Assert.Contains(Shop(story, party, "Nursery").Transcript, l => l.Text.Contains("Once a day"));
        Assert.Equal(20 - JoinAvenue.NurseryCycles(10), egg.EggCycles);
        story.ClearDaily();
        egg.EggCycles = 3;
        Shop(story, party, "Nursery");
        Assert.Equal(1, egg.EggCycles);
    }

    [Fact]
    public void TheRaffleDrawsOneTicketADay()
    {
        var story = new StoryState();
        var host = Shop(story, new Party(), "Raffle", 3000, 0);
        Assert.Contains(host.Transcript, l => l.Text.Contains("Congratulations"));
        Assert.True(story.Has(JoinAvenue.RaffleTodayFlag));
        Assert.Contains(Shop(story, new Party(), "Raffle", 3000, 0).Transcript, l => l.Text.Contains("already"));
        story.ClearDaily();
        Assert.Contains(Shop(story, new Party(), "Raffle", 3000, 0).Transcript, l => l.Text.Contains("Congratulations"));
    }

    [Fact]
    public void ThreeVisitorsOfOursGreetThePlayerAndChangeWithTheDays()
    {
        // three different ones at every count, and others once more have come
        for (int visitors = 0; visitors < 40; visitors++)
            Assert.Equal(3, Enumerable.Range(1, JoinAvenue.VisitorsStanding).Select(p => JoinAvenue.VisitorAt(p, visitors)).Distinct().Count());
        Assert.NotEqual(JoinAvenue.VisitorAt(1, 3), JoinAvenue.VisitorAt(1, 7));
        // every visitor has a name and a greeting of the game's words
        for (int n = 0; n < JoinAvenue.VisitorList; n++)
        {
            Assert.True(PokemonPlatinumEngine.Core.Text.Has($"joinAvenue.visitor.{n}.name"));
            Assert.True(PokemonPlatinumEngine.Core.Text.Has($"joinAvenue.visitor.{n}.greeting"));
        }
        var story = new StoryState();
        story.SetVar(JoinAvenue.Vars.Visitors, 5);
        int who = JoinAvenue.VisitorAt(2, 5);
        // with the shop they would open already open, a visitor only greets
        story.Set(JoinAvenue.OpenFlag(JoinAvenue.OfferOf(who)));
        var host = Shop(story, new Party(), "Visitor2");
        var said = Assert.Single(host.Transcript);
        Assert.Equal(PokemonPlatinumEngine.Core.Text.Of($"joinAvenue.visitor.{who}.greeting"), said.Text);
        // and an invitation is said under the visitor's own name
        var other = new StoryState();
        other.SetVar(JoinAvenue.Vars.Visitors, 5);
        var asking = Shop(other, new Party(), "Visitor2", 3000, 0);
        Assert.True(asking.Transcript.Count > 1);
        Assert.All(asking.Transcript, l => Assert.Equal(PokemonPlatinumEngine.Core.Text.Of($"joinAvenue.visitor.{who}.name"), l.Speaker));
    }

    [Fact]
    public void AGrottoThatMayHoldSomethingRustles()
    {
        var story = new StoryState();
        var grotto = HiddenGrottoes.All[0];
        // Never come to: full, and heard
        Assert.True(HiddenGrottoes.Rustles(story, grotto, 0));
        Assert.Same(grotto, HiddenGrottoes.RustlingNear(story, grotto.Map, grotto.X, grotto.Y + 1, 0));
        Assert.Null(HiddenGrottoes.RustlingNear(story, grotto.Map, grotto.X + HiddenGrottoes.RustleReach + 1, grotto.Y, 0));
        Assert.Null(HiddenGrottoes.RustlingNear(story, "Somewhere", grotto.X, grotto.Y, 0));
        // Emptied: silent until a roll could have filled it
        HiddenGrottoes.Take(story, grotto, 100);
        Assert.False(HiddenGrottoes.Rustles(story, grotto, 100 + HiddenGrottoes.StepsPerRoll - 1));
        Assert.True(HiddenGrottoes.Rustles(story, grotto, 100 + HiddenGrottoes.StepsPerRoll));
        // and what is said of it is no draw: the content is unchanged
        Assert.Equal(HiddenGrottoes.Empty, story.Var(HiddenGrottoes.Vars.Content(grotto.Id)));
    }

    [Fact]
    public void TheGrottoesHaveSoundsOfTheirOwn()
    {
        foreach (string name in new[] { "grotto_enter", "grotto_rustle" })
        {
            Assert.Contains(name, SoundBank.Names);
            Assert.NotEqual(name, SoundBank.CaptionOf(name));
        }
        Assert.Contains(ScriptLibrary.Default.Find("GrottoMouth", "common")!.Code, i => i.Op == Op.Sound && i.Name == "grotto_enter");
    }
}
