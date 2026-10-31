using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// The Pokéathlon's third batch (plan 15 · E8): Relay Run, Circle Push and Goal Roll as pure functions of their seed and
/// the player's inputs (the purity and the bounded frames of every kind are <see cref="AthlonTests"/>' theories), the
/// five courses of <c>Data/athlon.json</c>, the medals' bands, the records board and the Athlete Shop paid in points.
/// </summary>
public class AthlonBatch3Tests
{
    private readonly ITestOutputHelper output;

    public AthlonBatch3Tests(ITestOutputHelper output) => this.output = output;

    private static Athlete A(int speed, int power, int skill, int stamina, int jump, string species = "Pikachu") =>
        new(species, new AthleteStats(speed, power, skill, stamina, jump));

    private static IReadOnlyList<AthlonTeam> Teams(Athlete mine, Athlete? theirs = null, float knack = 0.7f)
    {
        var them = theirs ?? A(3, 3, 3, 3, 3, "Bidoof");
        return Enumerable.Range(0, AthlonRun.TeamCount)
            .Select(t => new AthlonTeam(t == 0 ? "" : AthlonRun.RivalNames[t], t == 0, new[] { t == 0 ? mine : them, t == 0 ? mine : them, t == 0 ? mine : them }, knack))
            .ToList();
    }

    // ------------------------------------------------------------------ the courses

    /// <summary>
    /// Plan 15 · E7's four events, built beside this batch in another session: until they are merged a course may name
    /// them and is left out of the dome's list. Once they are events of the game this list must be emptied (the test
    /// below says so), so that a course can never wait for an event nobody is building.
    /// </summary>
    private static readonly string[] BuiltElsewhere = { };

    [Fact]
    public void EveryCourseNamesThreeEventsThatExist()
    {
        var file = AthlonData.File;
        Assert.Equal(new[] { "speed", "power", "skill", "stamina", "jump" }, file.Courses.Select(c => c.Key));
        foreach (var course in file.Courses)
        {
            Assert.Equal(3, course.Events.Count);
            Assert.Equal(3, course.Events.Distinct().Count());
            foreach (var e in course.Events)
                Assert.True(AthlonData.KindOf(e) != null || BuiltElsewhere.Contains(e), $"{course.Key}: {e} is no event");
        }
        // An allowance for an event that is built now is stale
        foreach (var e in BuiltElsewhere)
            Assert.True(AthlonData.KindOf(e) == null, $"{e} is an event of the game now: take it off BuiltElsewhere");
        // Every built event is in some course, and each of the five is offered once its events are all built
        foreach (var kind in Enum.GetValues<AthlonEventKind>())
            Assert.Contains(file.Courses, c => c.Events.Contains(kind.ToString()));
        Assert.Equal(file.Courses.Count, AthlonData.Courses.Count + AthlonData.Waiting.Count);
    }

    [Fact]
    public void ThePracticeCourseComesFirstAndTheBuiltCoursesAfterIt()
    {
        var all = AthlonCourses.All;
        Assert.Equal("practice", all[0].Key);
        var power = AthlonCourses.Get("power")!;
        Assert.Equal(new[] { AthlonEventKind.BlockSmash, AthlonEventKind.CirclePush, AthlonEventKind.GoalRoll }, power.Events);
        var stamina = AthlonCourses.Get("stamina")!;
        Assert.Equal(new[] { AthlonEventKind.RingDrop, AthlonEventKind.RelayRun, AthlonEventKind.BlockSmash }, stamina.Events);
        foreach (var course in all) Assert.All(course.Events, e => Assert.True(Enum.IsDefined(e)));
    }

    // ------------------------------------------------------------------ medals and the records

    [Theory]
    [InlineData(0, AthlonMedal.None)]
    [InlineData(79, AthlonMedal.None)]
    [InlineData(80, AthlonMedal.Bronze)]
    [InlineData(139, AthlonMedal.Bronze)]
    [InlineData(140, AthlonMedal.Silver)]
    [InlineData(199, AthlonMedal.Silver)]
    [InlineData(200, AthlonMedal.Gold)]
    [InlineData(999, AthlonMedal.Gold)]
    public void AMedalIsWonByItsScoresBand(int score, AthlonMedal medal) => Assert.Equal(medal, AthlonData.MedalFor(score));

    [Fact]
    public void TheRecordsKeepEachCoursesBestMedalThroughASave()
    {
        var records = new AthlonRecords();
        records.Record("power", 150, 2);
        Assert.Equal(AthlonMedal.Silver, records.MedalOf("power"));
        records.Record("power", 90, 3);
        Assert.Equal(AthlonMedal.Silver, records.MedalOf("power"));
        records.Record("power", 210, 1);
        Assert.Equal(AthlonMedal.Gold, records.MedalOf("power"));
        Assert.Equal(AthlonMedal.None, records.MedalOf("stamina"));

        var back = GameDataFiles.Deserialize<AthlonRecords>(GameDataFiles.Serialize(records.Copy()));
        Assert.Equal(AthlonMedal.Gold, back.MedalOf("power"));
        Assert.Equal(210, back.BestOf("power"));
    }

    [Fact]
    public void TheRecordsBoardOpensAndClosesOnAnyButton()
    {
        var screen = new AthlonScreen();
        var records = new AthlonRecords();
        records.Record("stamina", 100, 1);
        screen.OpenRecords(records);
        Assert.True(screen.IsActive);
        Assert.Equal(AthlonPhase.Records, screen.Phase);
        screen.Confirm();
        Assert.False(screen.IsActive);
        screen.OpenRecords(records);
        screen.Cancel();
        Assert.False(screen.IsActive);
        Assert.Equal(0, screen.Outcome);
    }

    [Fact]
    public void ACoursesScoreboardShowsItsMedal()
    {
        var party = new Party();
        foreach (var s in new[] { "Machop", "Geodude", "Pikachu" }) party.Add(new Pokemon(PokemonDatabase.Get(s)!, 20));
        var screen = new AthlonScreen();
        screen.Open(party, new AthlonRecords(), new Random(5));
        int power = AthlonCourses.All.ToList().FindIndex(c => c.Key == "power");
        for (int i = 0; i < power; i++) screen.Move(0, 1);
        screen.Confirm();
        for (int i = 0; i < 3; i++)
        {
            screen.Confirm();
            screen.Move(0, 1);
        }
        while (screen.Phase != AthlonPhase.Scoreboard)
        {
            if (screen.Phase == AthlonPhase.Playing) screen.Play(int.MaxValue, ev => ev.Autopilot());
            else screen.Confirm();
        }
        Assert.Equal("power", screen.Run!.Course.Key);
        Assert.Equal(AthlonData.MedalFor(screen.Run.Earned), screen.Medal);
        Assert.Equal(screen.Medal, screen.Records.MedalOf("power"));
    }

    // ------------------------------------------------------------------ the Athlete Shop

    [Fact]
    public void EveryGoodOfTheShopIsAnItemOfTheDataAtAPrice()
    {
        Assert.Equal(AthlonData.File.Shop.Count, AthlonData.Shop.Count);
        Assert.All(AthlonData.Shop, g => Assert.True(g.Points > 0));
    }

    [Fact]
    public void TheShopRefusesWhatThePointsCantPay()
    {
        var shop = new ShopScreen();
        shop.OpenForPoints("ATHLETE SHOP", AthlonData.Shop);
        Assert.True(shop.InPoints);
        Assert.Equal(ShopMode.Buying, shop.Mode);
        var bag = new Inventory();
        var said = new List<string>();
        int stone = shop.Stock.ToList().FindIndex(i => i.Name == "Fire Stone");
        shop.SelectedIndex = stone;
        Assert.Equal(2500, shop.PriceOf(shop.Selected!));

        // 2,000 points don't reach a stone: nothing changes hands
        int points = 2000;
        Assert.Equal(0, shop.Confirm(bag, points, said.Add));
        Assert.False(shop.ChoosingQuantity);
        Assert.Equal(0, bag.GetQuantity(shop.Selected!));
        Assert.Equal(Text.Of("athlon.shop.notEnough"), said.Last());

        // 5,000 reach two, and no more
        points = 5000;
        shop.Confirm(bag, points, said.Add);
        Assert.True(shop.ChoosingQuantity);
        shop.Move(1, 0, points);
        Assert.Equal(2, shop.Quantity);
        shop.Move(1, 0, points);
        Assert.Equal(1, shop.Quantity); // past two it wraps back to one
        shop.Move(1, 0, points);
        Assert.Equal(2, shop.Quantity);
        points += shop.Confirm(bag, points, said.Add);
        Assert.Equal(0, points);
        Assert.Equal(2, bag.GetQuantity(ItemDatabase.Get("Fire Stone")!));

        // The points spent come off the records' points as the game hands them over
        var records = new AthlonRecords();
        records.Earn(300);
        shop.SelectedIndex = shop.Stock.ToList().FindIndex(i => i.Name == "Heart Scale");
        int held = records.Points;
        shop.Confirm(bag, held, said.Add);
        held += shop.Confirm(bag, held, said.Add);
        records.Points = held;
        Assert.Equal(0, records.Points);

        // There is nothing to sell: B leaves the list and the shop
        shop.Cancel();
        Assert.False(shop.IsActive);
    }

    [Fact]
    public void AShopOpenedForMoneyAfterThePointsAsksMoneyAgain()
    {
        var shop = new ShopScreen();
        shop.OpenForPoints("ATHLETE SHOP", AthlonData.Shop);
        var potion = ItemDatabase.Get("Potion")!;
        shop.Open("MART", new[] { potion });
        Assert.False(shop.InPoints);
        Assert.Equal(potion.Price, shop.PriceOf(potion));
    }

    [Fact]
    public void TheStallAndTheRecordsDeskRunTheirScripts()
    {
        var host = new HeadlessScriptHost();
        host.Athlon.Earn(500);
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find("common.AthlonShop")!);
        runner.RunToEnd();
        Assert.Contains("open Athlon shop", host.Log);
        runner.Start(ScriptLibrary.Default.Find("common.AthlonRecords")!);
        runner.RunToEnd();
        Assert.Contains("open Athlon records", host.Log);
    }

    // ------------------------------------------------------------------ Relay Run

    [Fact]
    public void ARelayRunnerTiresAndSlowsAndAStayerRunsFurther()
    {
        // A runner that never hands over: Stamina 1 is spent in about eight seconds (300 / 1.2 a frame), and then
        // crawls at a fifth of its pace; Stamina 5 lasts twenty-five (300 / 0.4)
        int LapsOf(int stamina)
        {
            var ev = new RelayRun(Teams(A(3, 3, 3, stamina, 3)), 3);
            while (!ev.IsDone) ev.Step(AthlonInput.None);
            return ev.Relays[0].Laps;
        }
        Assert.Equal(0.2f, RelayRun.Kept(RelayRun.Tired));
        Assert.Equal(1f, RelayRun.Kept(0f));
        Assert.True(LapsOf(5) > LapsOf(1), $"{LapsOf(5)} laps against {LapsOf(1)}");
    }

    [Fact]
    public void AHandOverLetsTheTiredRunnerRestAndASpurtGoesFaster()
    {
        var ev = new RelayRun(Teams(A(3, 3, 3, 3, 3)), 3);
        for (int i = 0; i < 60; i++) ev.Step(AthlonInput.None);
        float ran = ev.Relays[0].Distance;
        float spent = ev.Relays[0].Fatigue[0];
        Assert.True(spent > 0f);
        ev.Step(new AthlonInput(Switch: 1));
        Assert.Equal(1, ev.Relays[0].Active);
        Assert.Equal(RelayRun.SwitchFrames, ev.Relays[0].Switching);
        // While the next one comes in nobody runs, and the one handed over gets its breath back
        for (int i = 0; i < RelayRun.SwitchFrames; i++) ev.Step(AthlonInput.None);
        Assert.Equal(ran, ev.Relays[0].Distance);
        Assert.True(ev.Relays[0].Fatigue[0] < spent);

        // Two runners alike, one spurting: the spurter is ahead after its spurt
        var calm = new RelayRun(Teams(A(3, 3, 3, 3, 3)), 9);
        var quick = new RelayRun(Teams(A(3, 3, 3, 3, 3)), 9);
        for (int i = 0; i < 40; i++)
        {
            calm.Step(AthlonInput.None);
            quick.Step(new AthlonInput(Tap: i == 20));
        }
        Assert.True(quick.Relays[0].Distance > calm.Relays[0].Distance);
        Assert.Equal(1, quick.Relays[0].Spurts);
    }

    // ------------------------------------------------------------------ Circle Push

    [Fact]
    public void APokemonAloneInTheCircleScoresTwiceAsFast()
    {
        // Put in the middle while the others start at their walls 110 away: for the first fifteen frames it is alone,
        // which counts each frame twice, thirty frames' worth, two points
        var ev = new CirclePush(Teams(A(3, 3, 3, 3, 3)), 4);
        ev.Pushers[0].At = Vector2.Zero;
        for (int i = 0; i < CirclePush.FramesPerPoint; i++) ev.Step(AthlonInput.None);
        Assert.Equal(CirclePush.FramesPerPoint, ev.Pushers[0].Held);
        Assert.Equal(CirclePush.FramesPerPoint, ev.Pushers[0].Alone);
        Assert.Equal(2, ev.Points(0));
        for (int t = 1; t < AthlonRun.TeamCount; t++) Assert.Equal(0, ev.Points(t));
    }

    [Fact]
    public void TheWallsHoldEveryoneIn()
    {
        var ev = new CirclePush(Teams(A(5, 5, 5, 5, 5)), 4);
        for (int i = 0; i < 300; i++) ev.Step(new AthlonInput(Tap: i % 10 == 0, Dy: 1));
        float wall = CirclePush.Half - AthlonBodies.Body;
        Assert.All(ev.Pushers, p => Assert.True(MathF.Abs(p.At.X) <= wall + 0.001f && MathF.Abs(p.At.Y) <= wall + 0.001f));
    }

    // ------------------------------------------------------------------ Goal Roll

    [Fact]
    public void AGoalScoresForWhoeverTouchedTheBallLastAndAnOwnGoalLoses()
    {
        var ev = new GoalRoll(Teams(A(3, 3, 3, 3, 3)), 6);
        // Rolling north into team 2's goal, last touched by the player: fifteen points to the player
        ev.Rig(new Vector2(0f, -GoalRoll.Half + 5f), new Vector2(0f, -10f), 0);
        ev.Step(AthlonInput.None);
        Assert.Equal(1, ev.Players[0].Goals);
        Assert.Equal(GoalRoll.GoalPoints, ev.Points(0));
        Assert.Equal(2, ev.LastGoalInto);
        Assert.Equal(Vector2.Zero, ev.Ball);
        Assert.Equal(GoalRoll.Pause, ev.Waiting);
        // Into the player's own goal: five off, never under nought
        ev.Rig(new Vector2(0f, GoalRoll.Half - 5f), new Vector2(0f, 10f), 0);
        ev.Step(AthlonInput.None);
        Assert.Equal(1, ev.Players[0].OwnGoals);
        Assert.Equal(GoalRoll.GoalPoints - GoalRoll.OwnGoalPoints, ev.Points(0));
        // Off a side outside a mouth it bounces back in
        ev.Rig(new Vector2(GoalRoll.Half - 8f, GoalRoll.Half - 8f), new Vector2(10f, 0f), -1);
        ev.Step(AthlonInput.None);
        Assert.True(ev.Ball.X <= GoalRoll.Half - GoalRoll.BallRadius + 0.001f);
        Assert.True(ev.BallVelocity.X < 0f);
    }

    // ------------------------------------------------------------------ how the events score

    [Theory]
    [InlineData(AthlonEventKind.RelayRun)]
    [InlineData(AthlonEventKind.CirclePush)]
    [InlineData(AthlonEventKind.GoalRoll)]
    [InlineData(AthlonEventKind.DiscCatch)]
    public void AnEventScoresWithinItsOwnRange(AthlonEventKind kind)
    {
        // Played by the autopilot over twenty seeds, each team's points stay within what a course's medals are made
        // for: a fair team scores between ten and a hundred and fifty an event
        var all = new List<int>();
        for (int seed = 1; seed <= 20; seed++)
        {
            var ev = AthlonEvent.Create(kind, Teams(A(3, 3, 3, 3, 3), A(3, 3, 3, 3, 3, "Bidoof")), seed);
            while (!ev.IsDone) ev.Step(ev.Autopilot());
            for (int t = 0; t < AthlonRun.TeamCount; t++) all.Add(ev.Points(t));
        }
        output.WriteLine($"{kind}: min {all.Min()}, mean {all.Average():0.0}, max {all.Max()}");
        Assert.True(all.Average() is >= 10 and <= 150, $"{kind}: mean {all.Average()}");
        Assert.True(all.Max() <= 250);
    }

    [Fact]
    public void AnEvenTeamOnEveryCourseLandsAboutBronze()
    {
        // Four even teams of autopilots (every stat 3, the same knack) over ten seeds: each of the five courses' mean total sits about
        // the bronze band (80) and under silver (140), so a medal above bronze wants a team that suits the course
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
                    var ev = AthlonEvent.Create(kind, teams, seed * 31 + (int)kind);
                    while (!ev.IsDone) ev.Step(ev.Autopilot());
                    for (int t = 0; t < AthlonRun.TeamCount; t++) sum[t] += ev.Points(t);
                }
                totals.AddRange(sum);
            }
            output.WriteLine($"{course.Key}: mean {totals.Average():0.0}, max {totals.Max()}");
            means[course.Key] = totals.Average();
        }
        foreach (var (key, mean) in means) Assert.True(mean is >= 60 and < 140, $"{key}: an even team scores {mean:0.0} on average");
    }
}
