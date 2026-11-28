using System;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.Rides;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// The riding race (plan 42 · PS8; rulings, "The riding race"): the tracks' table, a race frame by frame (a rock cleared
/// by a jump and stumbled over on the ground, mud crossed by Power, a turn leant into, the stamina spent by a push), a
/// mount too small refused, the records and the pay by place, the screen played to its points and the stable's script.
/// </summary>
public class RaceTests
{
    private readonly ITestOutputHelper output;
    public RaceTests(ITestOutputHelper output) => this.output = output;

    private static RaceTrack Blossom => RaceTracks.Get("blossom")!;

    private static Athlete Mount(int speed = 3, int power = 3, int skill = 3, int stamina = 3, int jump = 3) =>
        new("Rhyhorn", new AthleteStats(speed, power, skill, stamina, jump));

    private static RidingRace Race(RaceTrack? track = null, Athlete? mine = null, int seed = 5)
    {
        track ??= Blossom;
        return new RidingRace(track, RaceTracks.Teams(track, mine ?? Mount()), seed);
    }

    private static RaceTrack Bare(params RaceObstacle[] obstacles) => new()
    {
        Id = "bare", Stable = "x", Length = 1200, Seconds = 60,
        Obstacles = obstacles.ToList(),
        Riders = Blossom.Riders, Pay = new() { 3, 2, 1, 0 }
    };

    private static void RunTo(RidingRace race, float distance, AthlonInput input)
    {
        for (int f = 0; f < 2000 && race.Racers[0].Distance < distance && !race.IsDone; f++) race.Step(input);
    }

    // ------------------------------------------------------------------ the table

    [Fact]
    public void EveryTrackHasThreeRidersObstaclesInOrderAndPaysNoMoreThanATowerRound()
    {
        Assert.NotEmpty(RaceTracks.All);
        foreach (var t in RaceTracks.All)
        {
            Assert.Equal(AthlonRun.TeamCount - 1, t.Riders.Count);
            Assert.All(t.Riders, r =>
            {
                Assert.InRange(r.Knack, 0f, 1f);
                Assert.NotNull(PokemonDatabase.Get(r.Mount));
                Assert.True(RidingRace.CanCarry(PokemonDatabase.Get(r.Mount)!), r.Mount);
            });
            Assert.Equal(AthlonRun.TeamCount, t.Pay.Count);
            Assert.True(t.Pay.Max() <= 3, "a race pays no more than a first round at the Tower");
            Assert.Equal(t.Pay.OrderByDescending(p => p), t.Pay);
            Assert.True(t.Obstacles.Select(o => o.At).SequenceEqual(t.Obstacles.Select(o => o.At).OrderBy(a => a)), "obstacles in order");
            Assert.All(t.Obstacles, o => Assert.InRange(o.At, 100, t.Length - 100));
            Assert.All(t.Obstacles.Where(o => o.Kind == RaceObstacleKind.Mud), o => Assert.True(o.Length > 0));
            Assert.All(t.Obstacles.Where(o => o.Kind == RaceObstacleKind.Turn), o => Assert.True(o.Dir is -1 or 1));
            Assert.NotNull(PokemonDatabase.Get("Rhyhorn"));
        }
    }

    [Fact]
    public void TheRaceIsARideNoCourseOfTheDomeNames()
    {
        Assert.True(AthlonEvent.IsRide(AthlonEventKind.Race));
        Assert.All(AthlonCourses.All, c => Assert.DoesNotContain(AthlonEventKind.Race, c.Events));
        Assert.IsType<RidingRace>(AthlonEvent.Create(AthlonEventKind.Race, RaceTracks.Teams(Blossom, Mount()), 1));
    }

    // ------------------------------------------------------------------ the mount

    [Fact]
    public void AMountTooSmallOrTooHeavyIsRefused()
    {
        Assert.True(RidingRace.CanCarry(PokemonDatabase.Get("Rhyhorn")!));
        Assert.True(RidingRace.CanCarry(PokemonDatabase.Get("Tauros")!));
        Assert.False(RidingRace.CanCarry(PokemonDatabase.Get("Pidgey")!));
        Assert.False(RidingRace.CanCarry(PokemonDatabase.Get("Joltik")!));
        Assert.False(RidingRace.CanCarry(PokemonDatabase.Get("Wailord")!));
        var small = new Pokemon(PokemonDatabase.Get("Pidgey")!, 20);
        var big = new Pokemon(PokemonDatabase.Get("Tauros")!, 20);
        Assert.False(RidingRace.CanCarry(small));
        Assert.True(RidingRace.CanCarry(big));
        big.CurrentHP = 0;
        Assert.False(RidingRace.CanCarry(big));
    }

    // ------------------------------------------------------------------ the frame

    [Fact]
    public void ARockClearedByAJumpIsNoStumbleAndOneOnTheGroundIs()
    {
        var rock = new RaceObstacle { Kind = RaceObstacleKind.Rock, At = 300 };
        // On the ground: a stumble, the speed to a third and the rider stunned
        var onFoot = Race(Bare(rock));
        RunTo(onFoot, 270, AthlonInput.None);
        float before = onFoot.Racers[0].Speed;
        RunTo(onFoot, 305, AthlonInput.None);
        var r = onFoot.Racers[0];
        Assert.Equal(1, r.Stumbles);
        Assert.True(r.Speed < before * 0.6f);
        Assert.True(r.Stun > 0);

        // A jump pressed with the rock still ahead clears it
        var leapt = Race(Bare(rock));
        for (int f = 0; f < 2000 && leapt.Racers[0].Distance < 300 - 40; f++) leapt.Step(AthlonInput.None);
        leapt.Step(new AthlonInput(Tap: true));
        RunTo(leapt, 320, AthlonInput.None);
        Assert.Equal(0, leapt.Racers[0].Stumbles);
        Assert.Equal(1, leapt.Racers[0].Cleared);
    }

    [Fact]
    public void JumpRaisesTheAirAndAStunnedRiderCannotJump()
    {
        Assert.True(RidingRace.AirFrames(Mount(jump: 5).Stats) > RidingRace.AirFrames(Mount(jump: 1).Stats));
        var race = Race(Bare(new RaceObstacle { Kind = RaceObstacleKind.Rock, At = 120 }));
        RunTo(race, 121, AthlonInput.None);
        Assert.True(race.Racers[0].Stun > 0);
        race.Step(new AthlonInput(Tap: true));
        Assert.Equal(0, race.Racers[0].Air);
    }

    [Fact]
    public void PowerCarriesAMountThroughMudAndAPushLiftsIt()
    {
        var mud = new RaceObstacle { Kind = RaceObstacleKind.Mud, At = 200, Length = 300 };
        float SpeedInMud(int power, bool push)
        {
            var race = Race(Bare(mud), Mount(power: power, stamina: 5));
            RunTo(race, 260, push ? new AthlonInput(Held: true) : AthlonInput.None);
            return race.Racers[0].Speed;
        }
        Assert.True(SpeedInMud(5, false) > SpeedInMud(1, false));
        Assert.True(SpeedInMud(1, true) > SpeedInMud(1, false));
        // Out in the open the same mount is faster than it is in the mud
        var open = Race(Bare(), Mount(power: 1));
        RunTo(open, 260, AthlonInput.None);
        Assert.True(open.Racers[0].Speed > SpeedInMud(1, false));
    }

    [Fact]
    public void ATurnLeantIntoKeepsItsSpeedAndOneMissedLosesSome()
    {
        var turn = new RaceObstacle { Kind = RaceObstacleKind.Turn, At = 400, Dir = 1 };
        var missed = Race(Bare(turn));
        RunTo(missed, 400 + RidingRace.TurnReach - 1, AthlonInput.None);
        float before = missed.Racers[0].Speed;
        RunTo(missed, 400 + RidingRace.TurnReach + 3, AthlonInput.None);
        Assert.Equal(1, missed.Racers[0].TurnsMissed);
        Assert.True(missed.Racers[0].Speed < before);

        var leant = Race(Bare(turn));
        RunTo(leant, 400 - RidingRace.TurnReach + 5, AthlonInput.None);
        for (int f = 0; f < 200 && leant.Racers[0].Distance < 400 + RidingRace.TurnReach + 3; f++) leant.Step(new AthlonInput(Dx: 1));
        Assert.Equal(1, leant.Racers[0].TurnsLeant);
        Assert.Equal(0, leant.Racers[0].TurnsMissed);

        // The wrong way is no lean
        var wrong = Race(Bare(turn));
        RunTo(wrong, 400 - RidingRace.TurnReach + 5, AthlonInput.None);
        for (int f = 0; f < 200 && wrong.Racers[0].Distance < 400 + RidingRace.TurnReach + 3; f++) wrong.Step(new AthlonInput(Dx: -1));
        Assert.Equal(1, wrong.Racers[0].TurnsMissed);
    }

    [Fact]
    public void ASpeedierMountIsFasterAndAPushSpendsStaminaThatComesBack()
    {
        var slow = Race(Bare(), Mount(speed: 1));
        var fast = Race(Bare(), Mount(speed: 5));
        for (int f = 0; f < 90; f++) { slow.Step(AthlonInput.None); fast.Step(AthlonInput.None); }
        Assert.True(fast.Racers[0].Distance > slow.Racers[0].Distance);

        var push = Race(Bare(), Mount(stamina: 1));
        for (int f = 0; f < 30; f++) push.Step(new AthlonInput(Held: true));
        float spent = push.Racers[0].Stamina;
        Assert.True(spent < 1f);
        // Out of breath, the push does nothing; let go, it comes back
        for (int f = 0; f < 400 && push.Racers[0].Stamina > 0.02f; f++) push.Step(new AthlonInput(Held: true));
        Assert.False(push.Step_Boosting());
        for (int f = 0; f < 60; f++) push.Step(AthlonInput.None);
        Assert.True(push.Racers[0].Stamina > 0.2f);
    }

    [Fact]
    public void ARaceEndsWhenAllAreHomeOrTheClockRunsOutAndPlacesGoByHowFarTheyGot()
    {
        var race = Race(seed: 9);
        int frames = 0;
        while (!race.IsDone && frames++ < 5000) race.Step(race.Autopilot());
        Assert.True(race.IsDone);
        Assert.True(race.Frames <= race.Length);
        Assert.Equal(new[] { 0, 1, 2, 3 }.OrderBy(x => x), race.Ranking.OrderBy(x => x));
        for (int i = 1; i < 4; i++) Assert.True(race.Points(race.Ranking[i - 1]) >= race.Points(race.Ranking[i]));
        Assert.InRange(race.PlayerPlace, 1, 4);

        // A track too long for its clock leaves riders out, placed by distance
        var longTrack = Bare();
        longTrack.Length = 100000;
        longTrack.Seconds = 5;
        var out1 = Race(longTrack, seed: 2);
        while (!out1.IsDone) out1.Step(AthlonInput.None);
        Assert.All(out1.Racers, r => Assert.False(r.Finished));
        Assert.Null(out1.TimeOf(0));
        Assert.True(out1.Points(0) < 5001);
    }

    [Fact]
    public void ARaceIsAPureFunctionOfItsSeedAndTheInputsAndEveryFrameIsBounded()
    {
        string Run(int seed)
        {
            var race = Race(seed: seed);
            while (!race.IsDone) race.Step(race.Autopilot());
            return string.Join("|", race.Racers.Select(r => $"{r.FinishFrame}:{r.Distance:0.###}:{r.Stumbles}:{r.Cleared}:{r.TurnsLeant}"));
        }
        Assert.Equal(Run(4), Run(4));
        Assert.NotEqual(Run(4), Run(5));
        var any = Race(seed: 3);
        for (int f = 0; f < 600 && !any.IsDone; f++)
        {
            any.Step(new AthlonInput(Tap: f % 17 == 0, Held: f % 5 < 3, Dx: (f / 40) % 3 - 1));
            Assert.All(any.Racers, r =>
            {
                Assert.InRange(r.Stamina, 0f, 1f);
                Assert.InRange(r.Speed, 0f, 5f);
                Assert.True(r.Distance >= 0f);
            });
        }
    }

    [Fact]
    public void AStrongerRiderBeatsAWeakerOneOverManySeeds()
    {
        int wins = 0;
        for (int seed = 1; seed <= 20; seed++)
        {
            var race = Race(mine: Mount(5, 5, 5, 5, 5), seed: seed);
            while (!race.IsDone) race.Step(race.Autopilot(0.9f));
            if (race.PlayerPlace <= 2) wins++;
        }
        output.WriteLine($"top two in {wins} of 20");
        Assert.True(wins >= 12);
    }

    // ------------------------------------------------------------------ the records

    [Fact]
    public void APlacePaysByTheTrackAndTheRecordsRoundTripASave()
    {
        var records = new RideRecords();
        var first = records.RecordRace(Blossom, 9500, 1);
        Assert.Equal(3, first.Paid);
        Assert.True(first.NewRecord);
        Assert.Equal(2, records.RecordRace(Blossom, 9000, 2).Paid);
        Assert.Equal(1, records.RecordRace(Blossom, 9000, 3).Paid);
        Assert.Equal(0, records.RecordRace(Blossom, 5000, 4).Paid);
        Assert.Equal(4, records.RunsOf(RaceTracks.KeyOf(Blossom)));
        Assert.Equal(9500, records.BestOf(RaceTracks.KeyOf(Blossom)));
        Assert.Equal(6, records.PointsWon);
        var json = System.Text.Json.JsonSerializer.Serialize(new SaveData { Rides = records.ToSave() });
        var again = new RideRecords();
        again.Load(System.Text.Json.JsonSerializer.Deserialize<SaveData>(json)!.Rides);
        Assert.Equal(9500, again.BestOf("race:blossom"));
        Assert.Equal(6, again.PointsWon);
        // A race's key stands apart from a surf course's
        Assert.Equal(0, again.BestOf("blossom"));
    }

    // ------------------------------------------------------------------ the screen

    [Fact]
    public void TheScreenRacesToItsPointsTwiceTheSame()
    {
        string Run()
        {
            var screen = new RideScreen();
            var records = new RideRecords();
            screen.OpenRace(Blossom, Mount(), records, new Random(11));
            Assert.Equal(RidePhase.Briefing, screen.Phase);
            screen.Confirm();
            Assert.Equal(RidePhase.Riding, screen.Phase);
            screen.PlayRace(int.MaxValue, r => r.Autopilot());
            Assert.Equal(RidePhase.Results, screen.Phase);
            screen.Confirm();
            Assert.Equal(RidePhase.Points, screen.Phase);
            screen.Confirm();
            Assert.False(screen.IsActive);
            Assert.Equal(screen.Result.Place, screen.Outcome);
            Assert.Equal(1, records.RunsOf("race:blossom"));
            Assert.Equal(Blossom.PayFor(screen.Outcome), screen.Result.Paid);
            return $"{screen.Result}";
        }
        string a = Run();
        output.WriteLine(a);
        Assert.Equal(a, Run());
    }

    // ------------------------------------------------------------------ the stable

    [Theory]
    [InlineData(1, 9000, "stable's prize")]
    [InlineData(4, 4000, "hard on newcomers")]
    public void TheStablemasterRunsTheRaceAndPaysByPlace(int place, int score, string said)
    {
        var host = new HeadlessScriptHost { RidePlace = place, RideScore = score };
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find("kalos_route_12.RaceStable")!);
        runner.RunToEnd();
        Assert.Contains(host.Transcript, line => line.Text.Contains(said));
        Assert.Equal(1, host.Rides!.RunsOf("race:blossom"));
        Assert.Equal(Blossom.PayFor(place), host.Frontier.BattlePoints);
    }

    [Fact]
    public void WithNoMountTheStablemasterTurnsThePlayerAway()
    {
        var host = new HeadlessScriptHost { RaceMount = null };
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find("kalos_route_12.RaceStable")!);
        runner.RunToEnd();
        Assert.Contains(host.Transcript, line => line.Text.Contains("big enough to carry a rider"));
        Assert.Equal(0, host.Rides!.RunsOf("race:blossom"));
    }
}

internal static class RaceTestExtensions
{
    public static bool Step_Boosting(this RidingRace race)
    {
        race.Step(new AthlonInput(Held: true));
        return race.Racers[0].Boosting;
    }
}
