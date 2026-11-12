using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.Facilities;
using PokemonPlatinumEngine.Models.Frontier;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 37 · FC8: Nimbasa's Battle Institute's test (<see cref="InstituteTest"/>) and its ladder (<see cref="Ladder"/>):
/// the score's formula against battles worked by hand, the ranks, the best kept per format, the rating up and down by the
/// Elo, opponents in the rating's band and the same for a seed, a new month a new season with the rating carried by half and
/// the season's prize given once, and both played headless at the receptionist.
/// </summary>
public class InstituteLadderTests
{
    private static readonly Lazy<Func<string, Map>> Rooms = new(MapDatabase.Fresh);

    [Fact]
    public void TheScoreIsTheFormulaWorkedByHand()
    {
        // A loss scores nothing, whatever else
        Assert.Equal(0, InstituteTest.ScoreOf(false, 1, 1000, 12));
        // Won in 3 turns, half the HP kept, foe tiers 2 + 3 + 3: 1000 + (10 - 3) * 40 + 500 * 400 / 1000 + 8 * 15 = 1600
        Assert.Equal(1600, InstituteTest.ScoreOf(true, 3, 500, 8));
        // Won in 12 turns with nothing kept against the weakest: 1000 + 0 + 0 + 3 * 15
        Assert.Equal(1045, InstituteTest.ScoreOf(true, 12, 0, 3));
        // The most a battle can be: 1 turn, all kept, three of the strongest
        Assert.Equal(1000 + 360 + 400 + 180, InstituteTest.ScoreOf(true, 1, 1000, 12));
        Assert.True(InstituteTest.Battles * InstituteTest.ScoreOf(true, 1, 1000, 12) <= InstituteTest.MostScore);
        var team = new Party();
        var mon = new Pokemon(PokemonDatabase.Get("Haxorus")!, 50, new Random(1));
        team.Add(mon);
        mon.CurrentHP = mon.MaxHP / 2;
        Assert.InRange(InstituteTest.HpKept(team), 495, 500);
    }

    [Fact]
    public void TheRanksComeAtTheirThresholdsAndTheBoardPlacesTheBest()
    {
        Assert.Equal(0, InstituteTest.RankOf(1999));
        Assert.Equal(1, InstituteTest.RankOf(2000));
        Assert.Equal(4, InstituteTest.RankOf(8999));
        Assert.Equal(5, InstituteTest.RankOf(9999));
        Assert.Equal(1, InstituteTest.PlaceOf(9500));
        Assert.Equal(InstituteTest.Board.Count + 1, InstituteTest.PlaceOf(0));
        Assert.Equal(3, InstituteTest.PlaceOf(8300));
    }

    [Fact]
    public void ATestsTrainersRiseByTheBattleAndAreTheSameForASeed()
    {
        for (int battle = 1; battle <= InstituteTest.Battles; battle++)
        {
            var a = InstituteTest.Foe(1, battle, 42);
            var b = InstituteTest.Foe(1, battle, 42);
            Assert.Equal(a.Sets, b.Sets);
            Assert.Equal(3, a.Pokemon.Count);
            Assert.Equal(4, InstituteTest.Foe(2, battle, 42).Pokemon.Count);
            var (low, high) = InstituteTest.TiersFor(battle);
            Assert.All(a.Sets, s => Assert.InRange(FacilitySetDatabase.Get(s)!.Tier, low, high));
        }
        Assert.True(InstituteTest.TiersFor(5).Lowest > InstituteTest.TiersFor(1).Lowest);
    }

    [Fact]
    public void TheRatingMovesByTheEloAndOpponentsStandAboutIt()
    {
        // Even ratings: a win is +16, a loss -16
        Assert.Equal(1016, Ladder.After(1000, 1000, true));
        Assert.Equal(984, Ladder.After(1000, 1000, false));
        // Against a rating 400 higher a win is worth 32 × 10/11, rounded: 29
        Assert.Equal(1029, Ladder.After(1000, 1400, true));
        Assert.Equal(997, Ladder.After(1000, 1400, false));
        var rng = new Random(3);
        for (int n = 0; n < 200; n++) Assert.InRange(Ladder.OpponentRating(1200, () => rng.Next(0x10000)), 1100, 1300);
        Assert.Equal(0, Ladder.BandOf(1099));
        Assert.Equal(2, Ladder.BandOf(1300));
        Assert.Equal(4, Ladder.BandOf(2000));
        var a = Ladder.Opponent(Ladder.SeasonOf(new DateTime(2026, 3, 1)), 1550, new PokemonPlatinumEngine.Battle.Sim.BattleRandom(7).Next16);
        var b = Ladder.Opponent(Ladder.SeasonOf(new DateTime(2026, 3, 1)), 1550, new PokemonPlatinumEngine.Battle.Sim.BattleRandom(7).Next16);
        Assert.Equal(a.Sets, b.Sets);
        Assert.All(a.Sets, s => Assert.Equal(4, FacilitySetDatabase.Get(s)!.Tier));
        Assert.Equal(BattleFormat.Double, Ladder.FormatOf(Ladder.SeasonOf(new DateTime(2026, 4, 1))));
        Assert.Equal(BattleFormat.Single, Ladder.FormatOf(Ladder.SeasonOf(new DateTime(2026, 3, 1))));
    }

    [Fact]
    public void ANewMonthIsANewSeasonTheRatingCarriedByHalfAndThePrizeGivenOnce()
    {
        var story = new StoryState();
        int march = Ladder.SeasonOf(new DateTime(2026, 3, 9));
        Assert.Equal(0, Ladder.TurnSeason(story, march));
        Assert.Equal(Ladder.Start, story.Var(Ladder.RatingVariable));
        for (int n = 0; n < 12; n++) Ladder.Played(story, 1000, true);
        int rating = story.Var(Ladder.RatingVariable);
        Assert.True(rating > 1150);
        int band = Ladder.BandOf(rating);
        Assert.Equal(band, story.Var(Ladder.BestBandVariable));
        // The same month again: nothing
        Assert.Equal(0, Ladder.TurnSeason(story, march));
        // April: the season's prize by its best band, the rating half way back
        Assert.Equal(Ladder.BandPrizes[band], Ladder.TurnSeason(story, Ladder.SeasonOf(new DateTime(2026, 4, 2))));
        Assert.Equal(Ladder.Start + (rating - Ladder.Start) / 2, story.Var(Ladder.RatingVariable));
        Assert.Equal(0, Ladder.TurnSeason(story, Ladder.SeasonOf(new DateTime(2026, 4, 30))));
        // A season with no game played pays nothing
        Assert.Equal(0, Ladder.TurnSeason(story, Ladder.SeasonOf(new DateTime(2026, 5, 1))));
    }

    private static HeadlessScriptHost Host(StoryState story)
    {
        var party = new Party();
        foreach (string s in new[] { "Haxorus", "Volcarona", "Chandelure", "Hydreigon" })
            party.Add(new Pokemon(PokemonDatabase.Get(s)!, 60, new Random(party.Count + 1)));
        return new HeadlessScriptHost(story, party, new Inventory())
        {
            Map = Rooms.Value("NimbasaCity_BattleInstitute"), MapNamed = Rooms.Value, Region = RegionDatabase.Unova, Rng = new Random(9)
        };
    }

    private static void Desk(HeadlessScriptHost host)
    {
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find("Desk", "nimbasa_city_14")!, host.Map!.Everyone.First(n => n.Name == "Receptionist"));
        runner.RunToEnd();
        Assert.True(host.Problems.Count == 0, string.Join("; ", host.Problems));
    }

    [Fact]
    public void ATestIsScoredAndRankedAtTheReceptionistAndItsBestKept()
    {
        var closed = Host(new StoryState());
        Desk(closed);
        Assert.Empty(closed.FrontierBattles);

        var story = new StoryState();
        story.Set("UnovaHallOfFame");
        var host = Host(story);
        host.Answers.Enqueue(0); // the test
        host.Answers.Enqueue(0); // single battles
        host.Entering.Enqueue(new[] { 0, 1, 2 });
        Desk(host);
        Assert.Equal(InstituteTest.Battles, host.FrontierBattles.Count);
        Assert.All(host.FrontierBattles, b => Assert.All(b.Team.Members, p => Assert.Equal(50, p.Level)));
        int best = story.Var(InstituteTest.BestVariable(1));
        Assert.InRange(best, 5 * 1000, InstituteTest.MostScore);
        Assert.Contains(host.Transcript, l => l.Text.Contains("out of 9,999", StringComparison.Ordinal));
        // A worse test leaves the best as it was
        var worse = Host(story);
        worse.Fight = _ => BattleOutcome.Lost;
        foreach (int a in new[] { 0, 0 }) worse.Answers.Enqueue(a);
        worse.Entering.Enqueue(new[] { 0, 1, 2 });
        Desk(worse);
        Assert.Equal(best, story.Var(InstituteTest.BestVariable(1)));
        Assert.Equal(0, story.Var(InstituteTest.BestVariable(2)));
    }

    [Fact]
    public void TheLadderRisesOverThreeWinsFallsOnALossAndTurnsOnTheFirstOfTheMonth()
    {
        var story = new StoryState();
        story.Set("UnovaHallOfFame");
        var host = Host(story);
        host.Today = new DateTime(2026, 3, 20);
        var outcomes = new Queue<BattleOutcome>(new[] { BattleOutcome.Won, BattleOutcome.Won, BattleOutcome.Won, BattleOutcome.Lost });
        host.Fight = _ => outcomes.Dequeue();
        host.Answers.Enqueue(1); // the ladder
        host.Entering.Enqueue(new[] { 0, 1, 2 });
        foreach (int a in new[] { 0, 0, 0, 1 }) host.Answers.Enqueue(a);
        Desk(host);
        Assert.Equal(4, host.FrontierBattles.Count);
        Assert.All(host.FrontierBattles, b => Assert.Equal(3, b.Team.Count));
        int rating = story.Var(Ladder.RatingVariable);
        Assert.Equal(3, story.Var(Ladder.WinsVariable));
        Assert.Equal(1, story.Var(Ladder.LossesVariable));
        Assert.True(rating > Ladder.Start, $"{rating}");
        Assert.Equal(Ladder.SeasonOf(new DateTime(2026, 3, 1)), story.Var(Ladder.SeasonVariable));

        // The first of April: a new season of doubles, the rating carried by half, the prize paid
        var april = Host(story);
        april.Today = new DateTime(2026, 4, 1);
        april.Answers.Enqueue(1); // the ladder
        april.Entering.Enqueue(new[] { 0, 1, 2, 3 });
        april.Answers.Enqueue(1); // no other battle
        int points = april.Frontier.BattlePoints;
        Desk(april);
        Assert.Equal(Ladder.SeasonOf(new DateTime(2026, 4, 1)), story.Var(Ladder.SeasonVariable));
        Assert.Equal(1, story.Var(Ladder.WinsVariable));
        Assert.True(story.Var(Ladder.RatingVariable) > Ladder.Carried(rating));
        Assert.True(april.Frontier.BattlePoints > points);
        Assert.Equal(4, april.FrontierBattles.Single().Team.Count);
        Assert.True(april.FrontierBattles.Single().Doubles);
    }
}
