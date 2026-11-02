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
        // The shops widen and open at their ranks; every stock is a list of the marts
        Assert.Equal("join_avenue_market", JoinAvenue.StockOf("market", 2));
        Assert.Equal("join_avenue_market_2", JoinAvenue.StockOf("market", 3));
        Assert.Equal("join_avenue_market_3", JoinAvenue.StockOf("market", 6));
        Assert.Null(JoinAvenue.StockOf("stones", JoinAvenue.StonesRank - 1));
        Assert.Null(JoinAvenue.StockOf("vitamins", JoinAvenue.VitaminsRank - 1));
        foreach (string shop in new[] { "market", "flowers", "cafe", "stones", "vitamins" })
            for (int rank = 1; rank <= JoinAvenue.TopRank; rank++)
                if (JoinAvenue.StockOf(shop, rank) is { } stock)
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
        Assert.Contains(Play(story, "Market").Log, l => l.Contains("join_avenue_market_3"));
        story.SetVar(JoinAvenue.Vars.Visitors, 1000);
        Assert.Contains(Play(story, "Vitamins").Log, l => l.Contains("join_avenue_vitamins"));
        Assert.Contains(Play(story, "Owner").Transcript, l => l.Text.Contains("as high as an avenue goes"));
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
