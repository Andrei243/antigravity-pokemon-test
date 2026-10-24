using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Story;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// The National Park's Bug-Catching Contest (plan 15 · E5). The numbers are HeartGold's own
/// (<c>src/overlay_bug_contest.c</c>, <c>files/data/mushi/mushi_encount.csv</c> and <c>mushi_trainer.csv</c> of
/// pret/pokeheartgold), worked out by hand in each test's comment.
/// </summary>
public class BugContestTests
{
    private const string Gate36 = "route_36_national_park_gatehouse", Gate35 = "route_35_national_park_pokeathalon_gatehouse";

    private static Pokemon Mon(string species, int level, int iv = 0)
    {
        var p = new Pokemon(PokemonDatabase.Get(species)!, level);
        p.IvHP = p.IvAttack = p.IvDefense = p.IvSpAttack = p.IvSpDefense = p.IvSpeed = iv;
        p.RecalculateStats();
        p.CurrentHP = p.MaxHP;
        return p;
    }

    private static Party Team(params Pokemon[] members)
    {
        var party = new Party();
        foreach (var p in members) party.Add(p);
        return party;
    }

    // ------------------------------------------------------------------ the venue

    [Fact]
    public void TheVenueIsHeartGoldsFourTablesAndTenContestants()
    {
        var venue = BugContestVenue.NationalPark;
        Assert.Equal(new[] { 2, 4, 6 }, venue.Days);
        Assert.Equal(20, venue.Balls);
        Assert.Equal(20, venue.Minutes);
        Assert.Equal(4, venue.Encounters.Count);
        Assert.All(venue.Encounters, table =>
        {
            Assert.Equal(10, table.Count);
            // The rates fall to nothing, so a roll always lands on a row
            Assert.Equal(0, table[^1].Rate);
            Assert.True(table.Zip(table.Skip(1)).All(pair => pair.First.Rate >= pair.Second.Rate));
            Assert.All(table, slot => Assert.NotNull(PokemonDatabase.Get(slot.Species)));
        });
        Assert.Equal(new[] { "Don", "Ed", "Abby", "William", "Benny", "Barry", "Cindy", "Josh", "Samuel", "Kipp" }, venue.Contestants.Select(c => c.Name));
        Assert.All(venue.Contestants, c =>
        {
            Assert.Equal(8, c.Rows.Count);
            // Before the National Pokédex every contestant has a row of any day; after it, one of each contest day
            Assert.Contains(c.Rows, r => !r.National && r.Day == BugContestVenue.AnyDay);
            foreach (int day in venue.Days) Assert.Contains(c.Rows, r => r.National && r.Day == day);
            Assert.All(c.Rows, r => Assert.NotNull(PokemonDatabase.Get(r.Species)));
            Assert.True(Trainer.ClassName(c.Class) != c.Class || PokemonPlatinumEngine.Core.Text.Has($"classes.{Trainer.KeyOf(c.Class)}"));
        });
    }

    [Fact]
    public void TheDaysTableIsTheWeekdayHalvedOnceTheNationalPokedexIsHad()
    {
        var venue = BugContestVenue.NationalPark;
        Assert.Equal(0, venue.TableFor((int)DayOfWeek.Saturday, nationalDex: false));
        Assert.Equal(1, venue.TableFor((int)DayOfWeek.Tuesday, nationalDex: true));
        Assert.Equal(2, venue.TableFor((int)DayOfWeek.Thursday, nationalDex: true));
        Assert.Equal(3, venue.TableFor((int)DayOfWeek.Saturday, nationalDex: true));
        Assert.True(venue.OpenOn(DayOfWeek.Tuesday));
        Assert.False(venue.OpenOn(DayOfWeek.Monday));
    }

    // ------------------------------------------------------------------ the start and the team

    [Fact]
    public void OnlyTheFirstPokemonAbleToFightGoesInAndTheTeamComesBackInItsOrder()
    {
        var fainted = Mon("Pidgey", 10);
        fainted.CurrentHP = 0;
        var cyndaquil = Mon("Cyndaquil", 12);
        var sentret = Mon("Sentret", 8);
        var party = Team(fainted, cyndaquil, sentret);
        var contest = new BugContest();

        Assert.True(contest.Start(party, (int)DayOfWeek.Tuesday, nationalDex: false, new Random(1)));
        Assert.Equal(20, contest.Balls);
        Assert.Equal(1, contest.LeadIndex);
        Assert.Same(cyndaquil, Assert.Single(party.Members));
        Assert.Equal(BugContest.Others, contest.Entrants.Count);
        Assert.Equal(contest.Entrants.Count, contest.Entrants.Select(e => e.Id).Distinct().Count());

        // Hurt in the park: the team comes back with it as it is, in its own place, and the catch is handed back
        cyndaquil.CurrentHP = 3;
        var caterpie = Mon("Caterpie", 9);
        Assert.False(contest.Catch(caterpie));
        var returned = contest.Finish(party);
        Assert.Same(caterpie, returned);
        Assert.Equal(new[] { fainted, cyndaquil, sentret }, party.Members);
        Assert.Equal(3, cyndaquil.CurrentHP);
        Assert.False(contest.Active);
    }

    [Fact]
    public void ATeamWithNobodyAbleToFightIsTurnedAway()
    {
        var down = Mon("Pidgey", 10);
        down.CurrentHP = 0;
        var party = Team(down);
        Assert.False(new BugContest().Start(party, 2, false, new Random(1)));
        Assert.Single(party.Members);
    }

    [Fact]
    public void TheOthersAreTheSameForTheSameSeedAndFitTheDay()
    {
        var a = new BugContest();
        var b = new BugContest();
        a.Start(Team(Mon("Cyndaquil", 12)), (int)DayOfWeek.Thursday, true, new Random(42));
        b.Start(Team(Mon("Cyndaquil", 12)), (int)DayOfWeek.Thursday, true, new Random(42));
        Assert.Equal(a.Entrants, b.Entrants);
        foreach (var e in a.Entrants)
        {
            var rows = BugContestVenue.NationalPark.Contestants[e.Id].Rows.Where(r => r.Day == (int)DayOfWeek.Thursday || r.Day == BugContestVenue.AnyDay).ToList();
            var row = rows.First(r => r.Species == e.Species);
            // score + (rand % (2 * randmod)) - randmod: within the spread either way
            Assert.InRange(e.Score, row.Score - row.Spread, row.Score + row.Spread - 1);
        }
    }

    // ------------------------------------------------------------------ the park

    [Fact]
    public void ARollMeetsTheFirstRowWhoseRateItReaches()
    {
        var contest = new BugContest();
        contest.Start(Team(Mon("Cyndaquil", 12)), 2, false, new Random(3));
        // On the first table a roll of 80 to 99 is a Caterpie, 0 to 4 a Pinsir (rate 0), 5 to 9 a Scyther
        var counts = new Dictionary<string, int>();
        var rng = new Random(7);
        for (int i = 0; i < 4000; i++)
        {
            var (species, level) = contest.Meet(rng);
            var slot = contest.Slots.First(s => s.Species == species);
            Assert.InRange(level, slot.MinLevel, slot.MaxLevel);
            counts[species] = counts.GetValueOrDefault(species) + 1;
        }
        Assert.InRange(counts["Caterpie"], 600, 1000);
        Assert.InRange(counts["Pinsir"], 100, 300);
        Assert.InRange(counts["Scyther"], 100, 300);
    }

    [Fact]
    public void TheClockStopsTheContestAtTwentyMinutes()
    {
        var contest = new BugContest();
        contest.Start(Team(Mon("Cyndaquil", 12)), 2, false, new Random(3));
        contest.Tick(19 * 60f);
        Assert.False(contest.TimeUp);
        Assert.Equal(60f, contest.SecondsLeft, 3);
        contest.Tick(120f);
        Assert.True(contest.TimeUp);
        Assert.Equal(0f, contest.SecondsLeft);
    }

    [Fact]
    public void ASecondCatchAsksAndTheAnswerKeepsTheRightOne()
    {
        var contest = new BugContest();
        contest.Start(Team(Mon("Cyndaquil", 12)), 2, false, new Random(3));
        var first = Mon("Weedle", 10);
        var second = Mon("Scyther", 14);
        var third = Mon("Metapod", 9);
        Assert.False(contest.Catch(first));
        Assert.True(contest.Catch(second));
        contest.Keep(theNewOne: true);
        Assert.Same(second, contest.Caught);
        Assert.Null(contest.Pending);
        Assert.True(contest.Catch(third));
        contest.Keep(theNewOne: false);
        Assert.Same(second, contest.Caught);
    }

    // ------------------------------------------------------------------ the judging

    [Fact]
    public void JudgeIsHeartGoldsScore()
    {
        var table = BugContestVenue.NationalPark.Encounters[0];
        // A Scyther (worth 100) of level 14 of the table's 14, every IV 31, at full HP:
        // 100 + 14 * 100 / 14 + 186 * 100 / 186 + 100 = 400
        var best = Mon("Scyther", 14, iv: 31);
        Assert.Equal(400, BugContest.Judge(best, table));
        // A Caterpie (60) of level 9 of 18, IVs of 10 (60 in all), at half its HP rounded down:
        // 60 + 9 * 100 / 18 (50) + 60 * 100 / 186 (32) + hp * 100 / max
        var caterpie = Mon("Caterpie", 9, iv: 10);
        caterpie.CurrentHP = caterpie.MaxHP / 2;
        int hp = caterpie.CurrentHP * 100 / caterpie.MaxHP;
        Assert.Equal(60 + 50 + 32 + hp, BugContest.Judge(caterpie, table));
        // Nothing caught, or a species the day's table doesn't have, scores nothing
        Assert.Equal(0, BugContest.Judge(null, table));
        Assert.Equal(0, BugContest.Judge(Mon("Pikachu", 10), table));
    }

    [Fact]
    public void ThePlayerTakesATieAndThePrizesGoByPlace()
    {
        var contest = new BugContest();
        contest.Start(Team(Mon("Cyndaquil", 12)), 2, false, new Random(3));
        var best = Mon("Scyther", 14, iv: 31);
        contest.Catch(best);
        // Everyone else at the player's own score: a later entrant takes a tie, and the player is the last
        int score = BugContest.Judge(best, contest.Slots);
        var tied = contest.Entrants.Select(e => e with { Score = score }).ToList();
        contest.Entrants.Clear();
        contest.Entrants.AddRange(tied);
        contest.JudgeAll(new Random(1));
        Assert.Equal(0, contest.Placement);
        Assert.Equal(BugContest.Player, contest.Ranking[0].Id);
        // Before the National Pokédex the first prize is always the Sun Stone
        Assert.Equal("Sun Stone", contest.Prize);
        Assert.Equal(6, contest.Ranking.Count);
        Assert.True(contest.Ranking.Zip(contest.Ranking.Skip(1)).All(p => p.First.Score >= p.Second.Score));

        Assert.Equal("Everstone", BugContest.PrizeFor(1, 0));
        Assert.Equal("Sitrus Berry", BugContest.PrizeFor(2, 0));
        Assert.Equal("Shed Shell", BugContest.PrizeFor(3, 0));
        Assert.Equal("Oval Stone", BugContest.PrizeFor(0, 9));
        Assert.All(BugContest.Stones, s => Assert.NotNull(ItemDatabase.Get(s)));
    }

    [Fact]
    public void NothingCaughtPlacesLast()
    {
        var contest = new BugContest();
        contest.Start(Team(Mon("Cyndaquil", 12)), 2, false, new Random(3));
        contest.JudgeAll(new Random(1));
        Assert.Equal(3, contest.Placement);
        Assert.Equal("Shed Shell", contest.Prize);
        Assert.Equal(BugContest.Player, contest.Ranking[^1].Id);
    }

    // ------------------------------------------------------------------ the save

    [Fact]
    public void ASaveInTheParkLoadsIntoTheSameContest()
    {
        var lead = Mon("Cyndaquil", 12);
        var other = Mon("Sentret", 8);
        var party = Team(other, lead);
        party.Members[0].CurrentHP = 0;
        var contest = new BugContest();
        contest.Start(party, 4, false, new Random(5));
        contest.Catch(Mon("Paras", 15));
        contest.Balls = 13;
        contest.Tick(300f);
        var save = contest.ToSave()!;

        var loaded = new BugContest();
        var loadedParty = Team(lead);
        loaded.Resume(save, loadedParty);
        Assert.True(loaded.Active);
        Assert.Equal(13, loaded.Balls);
        Assert.Equal(300f, loaded.Elapsed);
        Assert.Equal(contest.Entrants, loaded.Entrants);
        Assert.Equal("Paras", loaded.Caught!.Species.Name);
        var caught = loaded.Finish(loadedParty);
        Assert.Equal(new[] { "Sentret", "Cyndaquil" }, loadedParty.Members.Select(p => p.Species.Name));
        Assert.Same(lead, loadedParty.Members[1]);
        Assert.Equal("Paras", caught!.Species.Name);
    }

    // ------------------------------------------------------------------ the battle's ball

    [Fact]
    public void TheSportBallIsTheContestsAloneAndTheContestsAlone()
    {
        BattleCore Battle(string? only)
        {
            var core = new BattleCore(new CoreSetup
            {
                PlayerParty = Team(Mon("Cyndaquil", 20)),
                WildPokemon = new List<Pokemon> { Mon("Caterpie", 9) },
                Random = new BattleRandom(1),
                Rules = Ruleset.Platinum,
                SpecialBalls = only != null ? 20 : 0,
                Conditions = new BattleConditions { OnlyBall = only },
                PlayerController = null,
                EnemyController = null
            });
            core.Start();
            return core;
        }
        var contest = Battle(BattleCore.BugContestBall);
        var place = contest.PlayerSlots[0].Place;
        Assert.Null(contest.WhyNot(BattleChoice.UseItem(place, "Sport Ball")));
        Assert.NotNull(contest.WhyNot(BattleChoice.UseItem(place, "Poké Ball")));
        Assert.NotNull(contest.WhyNot(BattleChoice.UseItem(place, "Potion")));
        var elsewhere = Battle(null);
        Assert.NotNull(elsewhere.WhyNot(BattleChoice.UseItem(elsewhere.PlayerSlots[0].Place, "Sport Ball")));
        Assert.Null(elsewhere.WhyNot(BattleChoice.UseItem(elsewhere.PlayerSlots[0].Place, "Poké Ball")));
    }

    // ------------------------------------------------------------------ the scripts

    private static HeadlessScriptHost Host(DayOfWeek day, params Pokemon[] team)
    {
        var host = new HeadlessScriptHost { Weekday = day };
        foreach (var p in team) host.Party.Add(p);
        return host;
    }

    private static void Run(HeadlessScriptHost host, string name, string place)
    {
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find(name, place)!);
        runner.RunToEnd();
    }

    [Fact]
    public void TheOfficerOpensTheContestOnAContestDayAndSendsThePlayerIn()
    {
        var host = Host(DayOfWeek.Tuesday, Mon("Cyndaquil", 12), Mon("Sentret", 8));
        host.Answers.Enqueue(0);
        Run(host, "Officer", Gate36);
        Assert.True(host.BugContest.Active);
        Assert.Single(host.Party.Members);
        Assert.True(host.Story.Has("FLAG_DAILY_BUG_CONTEST_ENTERED"));
        Assert.True(host.Story.Has(ScriptRunner.BugContestUnderWayFlag));
        Assert.Equal(BugContest.Others, BugContestVenue.NationalPark.Contestants.Count(c => host.Story.Has(ScriptRunner.BugContestantFlag(c.Name))));
        Assert.Contains(host.Log, l => l.StartsWith("warp NationalPark 74 39"));
    }

    [Fact]
    public void OnAnyOtherDayTheOfficerOnlyTalks()
    {
        var host = Host(DayOfWeek.Monday, Mon("Cyndaquil", 12));
        Run(host, "Officer", Gate35);
        Assert.False(host.BugContest.Active);
        Assert.Empty(host.Asked);
        Assert.DoesNotContain(host.Log, l => l.StartsWith("warp"));
    }

    [Fact]
    public void TheJudgingGivesThePrizeAndTheTeamBackWithTheCatch()
    {
        var host = Host(DayOfWeek.Saturday, Mon("Cyndaquil", 12), Mon("Sentret", 8));
        host.Answers.Enqueue(0);
        Run(host, "Officer", Gate35);
        Assert.Contains(host.Log, l => l.StartsWith("warp NationalPark 40 85"));
        host.BugContest.Catch(Mon("Scyther", 14, iv: 31));

        // Back in the gate before the time is up, and done
        host.Answers.Enqueue(0);
        Run(host, ScriptLibrary.OnEnter, Gate35);
        Assert.False(host.BugContest.Active);
        Assert.Contains(host.Log, l => l.StartsWith("open BugContestResults"));
        Assert.Equal(new[] { "Cyndaquil", "Sentret", "Scyther" }, host.Party.Members.Select(p => p.Species.Name));
        Assert.False(host.Story.Has(ScriptRunner.BugContestUnderWayFlag));
        Assert.DoesNotContain(BugContestVenue.NationalPark.Contestants, c => host.Story.Has(ScriptRunner.BugContestantFlag(c.Name)));
        // One prize, whatever the place
        Assert.Equal(1, new[] { "Sun Stone", "Everstone", "Sitrus Berry", "Shed Shell" }.Sum(i => host.Bag.GetQuantity(ItemDatabase.Get(i)!)));

        // Once a day: the officer has nothing more to offer
        host.Answers.Clear();
        Run(host, "Officer", Gate35);
        Assert.False(host.BugContest.Active);
    }

    [Fact]
    public void ACatchThatDoesNotFitTheTeamGoesToThePC()
    {
        var team = Enumerable.Range(0, 6).Select(_ => Mon("Sentret", 8)).ToArray();
        var host = Host(DayOfWeek.Thursday, team);
        host.Answers.Enqueue(0);
        Run(host, "Officer", Gate36);
        host.BugContest.Catch(Mon("Caterpie", 9));
        Run(host, "BugContestJudging", Gate36);
        Assert.Equal(6, host.Party.Count);
        Assert.Equal("Caterpie", Assert.Single(host.Box).Species.Name);
    }

    [Fact]
    public void TheSwapQuestionKeepsWhatThePlayerSays()
    {
        var host = Host(DayOfWeek.Tuesday, Mon("Cyndaquil", 12));
        host.Answers.Enqueue(0);
        Run(host, "Officer", Gate36);
        host.BugContest.Catch(Mon("Weedle", 10));
        host.BugContest.Catch(Mon("Pinsir", 14));
        host.Answers.Enqueue(0);
        Run(host, "BugContestSwap", Gate36);
        Assert.Equal("Pinsir", host.BugContest.Caught!.Species.Name);
        Assert.True(host.BugContest.Active);
    }
}
