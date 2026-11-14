using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.Frontier;
using PokemonPlatinumEngine.Models.Rides;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// Trick surfing (plan 42 · PS7; rulings, "Trick surfing"): the courses' table, the ride frame by frame (a jump off the
/// crest, a trick by the arrow, a straight landing on a press in the window and a crooked one that falls, a combo's
/// quarters), the pay in Battle Points, the records round-tripping a save, the screen played to its points and the hut's
/// script. AthlonTests' theories take the ride's kind by themselves (the same for the same seed, no frame past its end).
/// </summary>
public class RideTests
{
    private readonly ITestOutputHelper output;
    public RideTests(ITestOutputHelper output) => this.output = output;

    private static SurfCourse Hauoli => SurfCourses.Get("hauoli")!;

    private static TrickSurf Ride(int seed = 5) => new(Hauoli, SurfCourses.Riders(Hauoli), seed);

    /// <summary>Carves down until the face's foot and climbs to the crest, then presses: frames until the player is in the air.</summary>
    private static void ToTheAir(TrickSurf ride)
    {
        var me = ride.Riders[0];
        for (int f = 0; f < 400 && me.Face > 0.05f; f++) ride.Step(new AthlonInput(Dy: 1));
        for (int f = 0; f < 400 && me.Face < TrickSurf.CrestLine; f++) ride.Step(new AthlonInput(Dy: -1));
        ride.Step(new AthlonInput(Tap: true));
        Assert.Equal(TrickSurf.Stance.Air, me.Stance);
    }

    // ------------------------------------------------------------------ the table

    [Fact]
    public void EveryCourseHasThreeOfTheClubAndPaysNoMoreThanATowerRound()
    {
        Assert.NotEmpty(SurfCourses.All);
        foreach (var c in SurfCourses.All)
        {
            Assert.Equal(AthlonRun.TeamCount - 1, c.Club.Count);
            Assert.All(c.Club, r => Assert.InRange(r.Knack, 0f, 1f));
            Assert.True(c.Seconds is >= 20 and <= 120, c.Id);
            // A course's best pays no more than a first round of seven at the Battle Tower (BattleTower.PointsByRoom[1])
            Assert.True(c.Pay.Max(p => p.Points) <= BattleTowerChallenge.PointsByRoom[1], c.Id);
            Assert.Equal(c.Pay.OrderBy(p => p.Score).Select(p => p.Points), c.Pay.Select(p => p.Points));
        }
        Assert.NotNull(PokemonDatabase.Get(SurfCourses.Mount));
    }

    [Fact]
    public void TheRideIsAnEventNoCourseOfTheDomeNames()
    {
        Assert.True(AthlonEvent.IsRide(AthlonEventKind.Surf));
        Assert.All(AthlonCourses.All, c => Assert.DoesNotContain(AthlonEventKind.Surf, c.Events));
        Assert.IsType<TrickSurf>(AthlonEvent.Create(AthlonEventKind.Surf, SurfCourses.Riders(Hauoli), 1));
    }

    // ------------------------------------------------------------------ the ride

    [Fact]
    public void CarvingGathersSpeedAndAPressBelowTheCrestJumpsNothing()
    {
        var ride = Ride();
        var me = ride.Riders[0];
        float before = me.Speed;
        for (int f = 0; f < 10; f++) ride.Step(new AthlonInput(Dy: 1));
        Assert.True(me.Speed > before);
        Assert.True(me.Face < 0.5f);
        ride.Step(new AthlonInput(Tap: true));
        Assert.Equal(TrickSurf.Stance.Riding, me.Stance);
    }

    [Fact]
    public void AStraightLandingScoresTheTrick()
    {
        var ride = Ride();
        var me = ride.Riders[0];
        ToTheAir(ride);
        // A spin from the first frame in the air, nothing else until the window, then the press
        ride.Step(TrickSurf.InputFor(SurfTrick.Spin));
        while (me.Air > ride.Window / 2) ride.Step(AthlonInput.None);
        ride.Step(new AthlonInput(Tap: true));
        while (me.Stance == TrickSurf.Stance.Air) ride.Step(AthlonInput.None);
        Assert.Equal(SurfLanding.Straight, me.LastLanding);
        // One spin, the first landing of a combo: 300 points, a multiplier of four quarters
        Assert.Equal(TrickSurf.TrickPoints(SurfTrick.Spin), me.Score);
        Assert.Equal(1, me.Combo);
        Assert.Equal(0, me.Falls);
        Assert.Equal(TrickSurf.Stance.Riding, me.Stance);
    }

    [Fact]
    public void ATrickWithNoPressOrAnEarlyOneOrUnfinishedFalls()
    {
        // No press
        var ride = Ride();
        var me = ride.Riders[0];
        ToTheAir(ride);
        ride.Step(TrickSurf.InputFor(SurfTrick.Roll));
        while (me.Stance == TrickSurf.Stance.Air) ride.Step(AthlonInput.None);
        Assert.Equal(SurfLanding.Crooked, me.LastLanding);
        Assert.Equal(TrickSurf.Stance.Down, me.Stance);
        Assert.Equal(0, me.Score);
        Assert.Equal(1, me.Falls);

        // A press before the window spoils it, even with another in the window
        ride = Ride();
        me = ride.Riders[0];
        ToTheAir(ride);
        ride.Step(TrickSurf.InputFor(SurfTrick.Spin));
        while (me.Air > ride.Window + 2) ride.Step(AthlonInput.None);
        ride.Step(new AthlonInput(Tap: true));
        while (me.Air > 2) ride.Step(AthlonInput.None);
        ride.Step(new AthlonInput(Tap: true));
        while (me.Stance == TrickSurf.Stance.Air) ride.Step(AthlonInput.None);
        Assert.Equal(SurfLanding.Crooked, me.LastLanding);

        // A trick begun too late to finish is crooked whatever the press
        ride = Ride();
        me = ride.Riders[0];
        ToTheAir(ride);
        while (me.Air > 3) ride.Step(AthlonInput.None);
        ride.Step(TrickSurf.InputFor(SurfTrick.Flip));
        ride.Step(new AthlonInput(Tap: true));
        while (me.Stance == TrickSurf.Stance.Air) ride.Step(new AthlonInput(Tap: true));
        Assert.Equal(SurfLanding.Crooked, me.LastLanding);

        // ...and a fall is over in its frames, back on the face with nothing in hand
        int down = me.Down;
        Assert.Equal(TrickSurf.DownFrames(ride.StatsOf(0)), down);
        for (int f = 0; f < down; f++) ride.Step(AthlonInput.None);
        Assert.Equal(TrickSurf.Stance.Riding, me.Stance);
        Assert.Equal(0, me.Combo);
    }

    [Fact]
    public void AJumpWithNoTrickLandsPlainAndKeepsTheCombo()
    {
        var ride = Ride();
        var me = ride.Riders[0];
        ToTheAir(ride);
        while (me.Stance == TrickSurf.Stance.Air) ride.Step(AthlonInput.None);
        Assert.Equal(SurfLanding.Plain, me.LastLanding);
        Assert.Equal(0, me.Falls);
        Assert.Equal(0, me.Score);
    }

    [Fact]
    public void ACombosQuartersCountAndARepeatedTrickHalves()
    {
        Assert.Equal(4, TrickSurf.ComboQuarters(1));
        Assert.Equal(5, TrickSurf.ComboQuarters(2));
        Assert.Equal(8, TrickSurf.ComboQuarters(5));
        Assert.Equal(8, TrickSurf.ComboQuarters(40));

        var ride = Ride();
        var me = ride.Riders[0];
        var scores = new List<int>();
        for (int jump = 0; jump < 3; jump++)
        {
            ToTheAir(ride);
            ride.Step(TrickSurf.InputFor(SurfTrick.Spin));
            while (me.Trick != SurfTrick.None) ride.Step(AthlonInput.None);
            // a second spin in the same jump where it fits (it halves)
            bool second = me.Air - TrickSurf.TrickFrames(SurfTrick.Spin, ride.StatsOf(0)) > ride.Window;
            if (second)
            {
                ride.Step(TrickSurf.InputFor(SurfTrick.Spin));
                while (me.Trick != SurfTrick.None) ride.Step(AthlonInput.None);
            }
            while (me.Air > ride.Window / 2) ride.Step(AthlonInput.None);
            ride.Step(new AthlonInput(Tap: true));
            while (me.Stance == TrickSurf.Stance.Air) ride.Step(AthlonInput.None);
            Assert.Equal(SurfLanding.Straight, me.LastLanding);
            int sum = TrickSurf.TrickPoints(SurfTrick.Spin) + (second ? TrickSurf.TrickPoints(SurfTrick.Spin) / 2 : 0);
            Assert.Equal(sum * TrickSurf.ComboQuarters(jump + 1) / 4, me.LastTrickPoints);
            scores.Add(me.LastTrickPoints);
        }
        Assert.Equal(3, me.Combo);
        Assert.Equal(3, me.BestCombo);
        Assert.Equal(scores.Sum(), me.Score);
    }

    [Fact]
    public void TheGenerousAssistDoublesTheWindowAndAutomaticIsHeldToIt()
    {
        var ride = Ride();
        int off = ride.Window;
        ride.Assist = TimingAssist.Generous;
        Assert.Equal(off * 2, ride.Window);
        ride.Assist = TimingAssist.Automatic;
        Assert.Equal(off * 2, ride.Window);
    }

    [Fact]
    public void TheClubRidesItsOwnWayAndTheAutopilotPlacesWell()
    {
        // Over a few seeds the club's best rider beats its weakest more often than not, and the autopilot's player scores
        int favoured = 0;
        for (int seed = 1; seed <= 12; seed++)
        {
            var ride = Ride(seed);
            while (!ride.IsDone) ride.Step(ride.Autopilot());
            output.WriteLine($"seed {seed}: " + string.Join(", ", Enumerable.Range(0, 4).Select(t =>
                $"{t}:{ride.Riders[t].Score} (combo {ride.Riders[t].BestCombo}, falls {ride.Riders[t].Falls}, jumps {ride.Riders[t].Jumps})")));
            if (ride.Riders[3].Score > ride.Riders[1].Score) favoured++;
            Assert.True(ride.Riders[0].Score > 0);
        }
        Assert.True(favoured >= 7, $"the best of the club beat the weakest {favoured} times of 12");
    }

    // ------------------------------------------------------------------ pay and records

    [Fact]
    public void AFirstPlacePaysByItsScoresBandAndAnythingElseNothing()
    {
        var c = Hauoli;
        var records = new RideRecords();
        Assert.Equal(1, c.PayFor(0));
        Assert.Equal(1, c.PayFor(c.Pay[1].Score - 1));
        Assert.Equal(2, c.PayFor(c.Pay[1].Score));
        Assert.Equal(3, c.PayFor(c.Pay[2].Score + 1));
        var first = records.Record(c, c.Pay[2].Score, 1);
        Assert.Equal(3, first.Paid);
        Assert.True(first.NewRecord);
        var second = records.Record(c, c.Pay[2].Score, 2);
        Assert.Equal(0, second.Paid);
        Assert.False(second.NewRecord);
        Assert.Equal(2, records.RunsOf(c.Id));
        Assert.Equal(3, records.PointsWon);
        Assert.Equal(c.Pay[2].Score, records.BestOf(c.Id));
    }

    [Fact]
    public void TheRecordsRoundTripASave()
    {
        var records = new RideRecords();
        Assert.Null(records.ToSave());
        records.Record(Hauoli, 7400, 1);
        var json = System.Text.Json.JsonSerializer.Serialize(new SaveData { Rides = records.ToSave() });
        var back = System.Text.Json.JsonSerializer.Deserialize<SaveData>(json)!;
        var again = new RideRecords();
        again.Load(back.Rides);
        Assert.Equal(7400, again.BestOf("hauoli"));
        Assert.Equal(1, again.RunsOf("hauoli"));
        Assert.Equal(records.PointsWon, again.PointsWon);
        // An older save has none
        again.Load(null);
        Assert.Null(again.ToSave());
    }

    // ------------------------------------------------------------------ the screen

    [Fact]
    public void TheScreenRidesACourseToItsPoints()
    {
        string Run()
        {
            var screen = new RideScreen();
            var records = new RideRecords();
            screen.Open(Hauoli, records, TimingAssist.Off, new Random(11));
            Assert.Equal(RidePhase.Briefing, screen.Phase);
            screen.Confirm();
            Assert.Equal(RidePhase.Riding, screen.Phase);
            screen.Play(int.MaxValue, ev => ev.Autopilot());
            Assert.Equal(RidePhase.Results, screen.Phase);
            screen.Confirm();
            Assert.Equal(RidePhase.Points, screen.Phase);
            screen.Confirm();
            Assert.False(screen.IsActive);
            Assert.Equal(screen.Result.Place, screen.Outcome);
            Assert.Equal(1, records.RunsOf("hauoli"));
            return $"{screen.Result}";
        }
        string a = Run();
        output.WriteLine(a);
        Assert.Equal(a, Run());
    }

    [Fact]
    public void TheBriefingMayBeLeftAndARideOnceBegunIsRiddenToItsEnd()
    {
        var screen = new RideScreen();
        var records = new RideRecords();
        screen.Open(Hauoli, records, TimingAssist.Off, new Random(3));
        screen.Cancel();
        Assert.False(screen.IsActive);
        Assert.Equal(0, screen.Outcome);
        Assert.Equal(0, records.RunsOf("hauoli"));

        screen.Open(Hauoli, records, TimingAssist.Off, new Random(3));
        screen.Confirm();
        screen.Cancel();
        Assert.Equal(RidePhase.Riding, screen.Phase);
        Assert.True(screen.IsActive);
    }

    // ------------------------------------------------------------------ the hut

    [Theory]
    [InlineData(1, 6000, "Battle Point")]
    [InlineData(3, 2500, "next time")]
    public void TheHutLendsAMantineAndPaysTheRide(int place, int score, string said)
    {
        var host = new HeadlessScriptHost { RidePlace = place, RideScore = score };
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find("hauoli_city.SurfHut")!);
        runner.RunToEnd();
        Assert.Contains(host.Transcript, line => line.Text.Contains(said));
        Assert.Equal(1, host.Rides!.RunsOf("hauoli"));
        Assert.Equal(place == 1 ? Hauoli.PayFor(score) : 0, host.Frontier.BattlePoints);
    }
}
