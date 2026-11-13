using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.Frontier;
using PokemonPlatinumEngine.Story;
using Xunit;
using static PokemonPlatinumTests.Scenario;

namespace PokemonPlatinumTests;

/// <summary>
/// Galar's Battle Tower ladder (plan 33 · GL7, part b; rulings, "Galar's Battle Tower ladder"): three wins take a rank
/// up, a loss from the fifth rank on takes a win back and never a rank, the Master Rank's third battle is Leon's at level
/// 50 with his last three, and the desk's script runs a run to its end on the story's own variables.
/// </summary>
public class GalarTowerTests
{
    [Fact]
    public void ThreeWinsTakeARankUpAndALossFromTheFifthRankCostsAWinNeverARank()
    {
        var story = new StoryState();
        Assert.Equal(0, GalarTower.Won(story));
        Assert.Equal(0, GalarTower.Won(story));
        Assert.Equal(1, GalarTower.Won(story));
        Assert.Equal(1, story.Var(GalarTower.Vars.Rank));
        Assert.Equal(0, story.Var(GalarTower.Vars.Wins));
        Assert.Equal(3 * GalarTower.PointsFor(0), story.Var(GalarTower.Vars.Earned));

        // Low ranks cost nothing on a loss
        GalarTower.Won(story);
        GalarTower.Lost(story);
        Assert.Equal(1, story.Var(GalarTower.Vars.Wins));

        // From the fifth rank a loss takes a win back, never below none and never a rank
        story.SetVar(GalarTower.Vars.Rank, GalarTower.LossCostsFrom);
        story.SetVar(GalarTower.Vars.Wins, 1);
        GalarTower.Lost(story);
        Assert.Equal(0, story.Var(GalarTower.Vars.Wins));
        GalarTower.Lost(story);
        Assert.Equal(0, story.Var(GalarTower.Vars.Wins));
        Assert.Equal(GalarTower.LossCostsFrom, story.Var(GalarTower.Vars.Rank));
    }

    [Fact]
    public void LeonWaitsAtTheMasterRanksThirdWinWithHisLastThreeAtFifty()
    {
        int master = GalarTower.Ranks - 1;
        Assert.False(GalarTower.LeonWaits(master, 0));
        Assert.True(GalarTower.LeonWaits(master, 2));
        Assert.False(GalarTower.LeonWaits(master - 1, 2));
        var leon = GalarTower.Leon()!;
        Assert.Equal("Leon", leon.Name);
        Assert.Equal(3, leon.Party.Count);
        Assert.All(leon.Party.Members, p => Assert.Equal(FrontierRules.Level, p.Level));
        Assert.Equal("Charizard", leon.Party.Members.Last().Species.Name);

        // Beating him counts and starts the Master Rank's count again
        var story = new StoryState();
        story.SetVar(GalarTower.Vars.Rank, master);
        story.SetVar(GalarTower.Vars.Wins, 2);
        Assert.Equal(2, GalarTower.Won(story));
        Assert.Equal(master, story.Var(GalarTower.Vars.Rank));
        Assert.Equal(0, story.Var(GalarTower.Vars.Wins));
        Assert.Equal(1, story.Var(GalarTower.Vars.LeonBeaten));
    }

    [Fact]
    public void TheDesksRunGoesUpTheLadderAndHandsOverItsPoints()
    {
        var story = new StoryState();
        story.Set("GalarHallOfFame");
        var party = new Party();
        foreach (var s in new[] { "Corviknight", "Toxtricity", "Dragapult" }) party.Add(Mon(s, 60, "Tackle"));
        var room = MapDatabase.Fresh()("Wyndon_RoseTower");
        var host = new HeadlessScriptHost(story, party, new Inventory()) { Region = RegionDatabase.Galar, Map = room };
        // BATTLE, then the three entered; battle on after the first two wins, then stop
        host.Answers.Enqueue(0);
        host.Answers.Enqueue(0);
        host.Answers.Enqueue(0);
        host.Answers.Enqueue(1);
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find("TowerDesk", "Wyndon_RoseTower")!, room.Everyone.Single(n => n.Id == "tower_receptionist"));
        runner.RunToEnd();
        Assert.Empty(host.Problems);
        // Three wins: up to the second rank, the run over and its points handed over
        Assert.Equal(1, story.Var(GalarTower.Vars.Rank));
        Assert.Equal(0, story.Var(GalarTower.Vars.State));
        Assert.Equal(3 * GalarTower.PointsFor(0), host.Frontier.BattlePoints);
        Assert.Equal(3, host.FrontierBattles.Count);
        Assert.All(host.FrontierBattles, b => Assert.All(b.Team.Members, p => Assert.True(p.Level <= FrontierRules.Level)));
    }
}
