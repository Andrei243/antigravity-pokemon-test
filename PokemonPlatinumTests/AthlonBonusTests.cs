using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// The Pokéathlon's bonuses and the halls' honours (plan 15, its last part): the bonuses are read from what an event
/// counted, change nothing of how it plays, are neither always nor never won by an even team, count toward the course's
/// total, and the records keep the player's; the honours come of the records alone.
/// </summary>
public class AthlonBonusTests
{
    private readonly ITestOutputHelper output;

    public AthlonBonusTests(ITestOutputHelper output) => this.output = output;

    private static Athlete A(int speed, int power, int skill, int stamina, int jump, string species = "Pikachu") =>
        new(species, new AthleteStats(speed, power, skill, stamina, jump));

    private static IReadOnlyList<AthlonTeam> Teams(Athlete mine, Athlete? theirs = null, float knack = 0.7f)
    {
        var them = theirs ?? A(3, 3, 3, 3, 3, "Bidoof");
        return Enumerable.Range(0, AthlonRun.TeamCount)
            .Select(t => new AthlonTeam(t == 0 ? "" : AthlonRun.RivalNames[t], t == 0, new[] { t == 0 ? mine : them, t == 0 ? mine : them, t == 0 ? mine : them }, knack))
            .ToList();
    }

    private static AthlonEvent Played(AthlonEventKind kind, int seed, IReadOnlyList<AthlonTeam> teams)
    {
        var ev = AthlonEvent.Create(kind, teams, seed);
        while (!ev.IsDone) ev.Step(ev.Autopilot());
        return ev;
    }

    [Fact]
    public void EveryBonusIsWonSometimesAndNotAlways()
    {
        // Even teams of autopilots (every stat 3) over twenty seeds: each event's two bonuses are won by some teams and
        // missed by others, so neither is a gift nor out of reach
        var rates = new List<(AthlonEventKind, int, int, int)>();
        foreach (var kind in Enum.GetValues<AthlonEventKind>())
        {
            int steady = 0, star = 0, played = 0;
            for (int seed = 1; seed <= 20; seed++)
            {
                var ev = Played(kind, seed * 17 + (int)kind, Teams(A(3, 3, 3, 3, 3)));
                for (int t = 0; t < AthlonRun.TeamCount; t++)
                {
                    var (st, sr) = AthlonBonuses.Earned(ev, t);
                    if (st) steady++;
                    if (sr) star++;
                    played++;
                }
            }
            output.WriteLine($"{kind}: steady {steady}/{played}, star {star}/{played}");
            rates.Add((kind, steady, star, played));
        }
        foreach (var (kind, steady, star, played) in rates)
        {
            Assert.True(steady > 0 && steady < played, $"{kind}: Steady won {steady} times of {played}");
            Assert.True(star > 0 && star < played, $"{kind}: Star won {star} times of {played}");
        }
    }

    [Fact]
    public void ABonusIsReadAndChangesNothingOfThePlay()
    {
        // The same seed and inputs give the same points whether the bonuses are read every frame or never
        foreach (var kind in Enum.GetValues<AthlonEventKind>())
        {
            var teams = Teams(A(4, 3, 2, 3, 4));
            var quiet = AthlonEvent.Create(kind, teams, 99);
            var asked = AthlonEvent.Create(kind, teams, 99);
            while (!quiet.IsDone)
            {
                quiet.Step(quiet.Autopilot());
                asked.Step(asked.Autopilot());
                for (int t = 0; t < AthlonRun.TeamCount; t++) AthlonBonuses.Of(asked, t);
            }
            for (int t = 0; t < AthlonRun.TeamCount; t++) Assert.Equal(quiet.Points(t), asked.Points(t));
        }
    }

    [Fact]
    public void ACoursesTotalCountsItsBonusesAndAnEvenTeamStaysUnderSilver()
    {
        var run = new AthlonRun(AthlonCourses.Get("speed")!, new[] { "Pikachu", "Bidoof", "Starly" }.Select(n => PokemonDatabase.Get(n)!).ToList(), new Random(7));
        while (!run.IsOver)
        {
            var ev = run.StartEvent();
            while (!ev.IsDone) ev.Step(ev.Autopilot());
            run.EndEvent();
        }
        for (int t = 0; t < AthlonRun.TeamCount; t++)
        {
            int events = Enumerable.Range(0, run.Course.Events.Count).Sum(e => run.PointsOf(e, t));
            int bonus = Enumerable.Range(0, run.Course.Events.Count).Sum(e => AthlonBonuses.Sum(run.BonusesOf(e, t)));
            Assert.Equal(events + bonus, run.Total(t));
            Assert.True(bonus % AthlonBonuses.Points == 0);
            Assert.InRange(bonus, 0, 2 * AthlonBonuses.Points * run.Course.Events.Count);
        }
        Assert.Equal(run.Total(0), run.Earned);

        // An even team's bonuses lift its mean total but keep it under silver, as E8's bands mean
        var means = new Dictionary<string, double>();
        foreach (var course in AthlonData.Courses)
        {
            var totals = new List<int>();
            for (int seed = 1; seed <= 10; seed++)
            {
                var teams = Teams(A(3, 3, 3, 3, 3), A(3, 3, 3, 3, 3, "Bidoof"));
                var sum = new int[AthlonRun.TeamCount];
                foreach (var kind in course.Events)
                {
                    var ev = Played(kind, seed * 31 + (int)kind, teams);
                    for (int t = 0; t < AthlonRun.TeamCount; t++) sum[t] += ev.Points(t) + AthlonBonuses.Sum(AthlonBonuses.Of(ev, t));
                }
                totals.AddRange(sum);
            }
            output.WriteLine($"{course.Key}: mean with bonuses {totals.Average():0.0}");
            means[course.Key] = totals.Average();
        }
        foreach (var (key, mean) in means) Assert.True(mean < 140, $"{key}: an even team scores {mean:0.0} with its bonuses");
    }

    [Fact]
    public void TheRecordsKeepThePlayersBonusesAndTheHonoursComeOfThem()
    {
        var records = new AthlonRecords();
        foreach (var h in Enum.GetValues<AthlonHonour>()) Assert.False(records.Has(h));

        for (int i = 0; i < AthlonRecords.ForShoes; i++) records.Count(new[] { new AthlonBonus(AthlonBonusKind.Steady, 3) });
        Assert.True(records.Has(AthlonHonour.Shoes));
        Assert.False(records.Has(AthlonHonour.Clothes));
        for (int i = 0; i < AthlonRecords.ForClothes; i++) records.Count(new[] { new AthlonBonus(AthlonBonusKind.Star, 3) });
        Assert.True(records.Has(AthlonHonour.Clothes));
        Assert.Equal(AthlonRecords.ForShoes, records.SteadyBonuses);
        Assert.Equal(AthlonRecords.ForClothes, records.StarBonuses);

        for (int i = 0; i < AthlonRecords.ForFlag - 1; i++) records.Record("practice", 10, 1);
        Assert.False(records.Has(AthlonHonour.Flag));
        records.Record("practice", 10, 1);
        Assert.True(records.Has(AthlonHonour.Flag));

        // The Pokégear's sign for a medal on every course, the ball's for silver on every course, the statue for gold
        var courses = AthlonData.Courses.Select(c => c.Key).ToList();
        foreach (var c in courses.Skip(1)) records.Award(c, AthlonMedal.Gold);
        Assert.False(records.Has(AthlonHonour.Pokegear));
        records.Award(courses[0], AthlonMedal.Bronze);
        Assert.True(records.Has(AthlonHonour.Pokegear));
        Assert.False(records.Has(AthlonHonour.Ball));
        records.Award(courses[0], AthlonMedal.Silver);
        Assert.True(records.Has(AthlonHonour.Ball));
        Assert.False(records.Has(AthlonHonour.Statue));
        records.Award(courses[0], AthlonMedal.Gold);
        Assert.True(records.Has(AthlonHonour.Statue));

        // The Practice Course counts toward none of the medals' honours
        var practice = new AthlonRecords();
        practice.Award("practice", AthlonMedal.Gold);
        Assert.False(practice.Has(AthlonHonour.Pokegear));

        // A copy keeps the counts
        var copy = records.Copy();
        Assert.Equal(records.SteadyBonuses, copy.SteadyBonuses);
        Assert.Equal(records.StarBonuses, copy.StarBonuses);
    }
}
