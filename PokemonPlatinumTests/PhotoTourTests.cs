using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 44 · SD5: the photo tour's ride, its judge and its report, played with no window: a ride the same twice for a
/// seed and the same presses, a fruit turning the nearest resident's day, a box and a photograph's points worked out by
/// hand, the film and the fruit counted, the report keeping the best, and its round trip through a save.
/// </summary>
public class PhotoTourTests
{
    private static TourCourseData Reed => PhotoTourCourses.Get("reed_river")!;

    private static IEnumerable<TourInput> Presses(int ticks)
    {
        for (int t = 0; t < ticks; t++)
            yield return new TourInput(Turn: t % 120 < 60 ? 0.5f : -0.5f, Tilt: t % 90 < 45 ? -0.3f : 0.3f, Zoom: t % 200 < 100 ? 1f : -1f,
                Shoot: t % 40 == 0, Throw: t % 300 == 150, Whistle: t % 500 == 250);
    }

    private static string Played(TourRide ride, IEnumerable<TourInput> inputs)
    {
        foreach (var input in inputs) ride.Step(input);
        return System.Text.Json.JsonSerializer.Serialize(ride.FrameNow()) + System.Text.Json.JsonSerializer.Serialize(ride.Photos);
    }

    [Fact]
    public void TheReedRiverReadsWithFourteenResidentsAndRareActionsOnWeatherAndTime()
    {
        var c = Reed;
        Assert.Equal(14, c.Residents.Count);
        Assert.Equal(0, c.OpensAt);
        // The user's answer 64: every rare action hangs on the weather, the hour or both
        foreach (var r in c.Residents.Where(r => r.Rare != null))
            Assert.True(r.Rare!.Weather != null || r.Rare.Hours != null, r.Species);
        Assert.Contains(c.Residents, r => r.Rare?.Weather == TourWeather.Rain);
        Assert.Contains(c.Residents, r => r.Rare?.Hours != null);
        foreach (var r in c.Residents) Assert.NotNull(PokemonPlatinumEngine.Data.PokemonDatabase.Get(r.Species));
    }

    [Fact]
    public void ARideIsTheSameTwiceForASeedAndTheSamePresses()
    {
        string a = Played(new TourRide(Reed, 42, 12, TourWeather.Clear), Presses(3000));
        string b = Played(new TourRide(Reed, 42, 12, TourWeather.Clear), Presses(3000));
        Assert.Equal(a, b);
        string c = Played(new TourRide(Reed, 43, 12, TourWeather.Clear), Presses(3000));
        Assert.NotEqual(a, c);
    }

    [Fact]
    public void TheFilmAndTheFruitAreCounted()
    {
        var ride = new TourRide(Reed, 1, 12, TourWeather.Clear);
        for (int i = 0; i < 12; i++) ride.Step(new TourInput(Throw: true));
        Assert.Equal(0, ride.FruitLeft);
        for (int i = 0; i < 70; i++) ride.Step(new TourInput(Shoot: true));
        Assert.Equal(0, ride.FilmLeft);
        Assert.Equal(TourRide.FilmRoll, ride.Photos.Count);
        Assert.True(ride.Over);
        int ticks = ride.Ticks;
        ride.Step(new TourInput(Shoot: true));
        Assert.Equal(ticks, ride.Ticks);
    }

    [Fact]
    public void AFruitsLandingTurnsTheNearestResidentsDay()
    {
        // A course of two idlers: the fruit lands nearer the first, which comes and eats it; the other stays idle
        var course = new TourCourseData
        {
            Id = "test",
            Seconds = 60,
            Rail = new() { new[] { 0f, 1.6f, 0f }, new[] { 0f, 1.6f, -60f } },
            Residents = new()
            {
                new TourResidentData { Species = "Bidoof", At = new[] { 1f, -10f }, Day = new() { new TourStepData { Act = TourAct.Idle, Seconds = 30 } } },
                new TourResidentData { Species = "Psyduck", At = new[] { 0f, -18f }, Day = new() { new TourStepData { Act = TourAct.Idle, Seconds = 30 } } }
            }
        };
        var ride = new TourRide(course, 7, 12, TourWeather.Clear);
        // Pitched down so the fruit lands about ten ahead, by the Bidoof
        for (int i = 0; i < 30; i++) ride.Step(new TourInput(Tilt: -0.3f));
        var landing = ride.FruitLanding();
        ride.Step(new TourInput(Throw: true));
        for (int i = 0; i < (int)(TourRide.FruitFlight * 30) + 2; i++) ride.Step(default);
        Assert.Equal(TourAct.Fruit, ride.ActOf(0));
        Assert.Equal(TourAct.Idle, ride.ActOf(1));
        // It walks to the fruit and stops short of it
        for (int i = 0; i < 90; i++) ride.Step(default);
        Assert.True(Vector2.Distance(new(ride.PositionOf(0).X, ride.PositionOf(0).Z), new(landing.X, landing.Z)) < 0.6f);
        Assert.Equal(3, PhotoJudge.StarsOf(TourAct.Fruit));
    }

    [Fact]
    public void TheWhistleWakesASleeperAndScaresAShyOne()
    {
        var course = new TourCourseData
        {
            Id = "test",
            Seconds = 60,
            Rail = new() { new[] { 0f, 1.6f, 0f }, new[] { 0f, 1.6f, -60f } },
            Residents = new()
            {
                new TourResidentData { Species = "Quagsire", At = new[] { 3f, -6f }, Day = new() { new TourStepData { Act = TourAct.Sleep, Seconds = 40 }, new TourStepData { Act = TourAct.Idle, Seconds = 1 } } },
                new TourResidentData { Species = "Surskit", At = new[] { -3f, -6f }, Shy = true, Day = new() { new TourStepData { Act = TourAct.Idle, Seconds = 40 } } }
            }
        };
        // The seed places the day; find one that starts the sleeper asleep
        uint seed = Enumerable.Range(1, 50).Select(s => (uint)s).First(s => new TourRide(course, s, 12, TourWeather.Clear).ActOf(0) == TourAct.Sleep);
        var ride = new TourRide(course, seed, 12, TourWeather.Clear);
        var before = ride.PositionOf(1);
        ride.Step(new TourInput(Whistle: true));
        Assert.Equal(TourAct.Woke, ride.ActOf(0));
        Assert.Equal(TourAct.Flee, ride.ActOf(1));
        for (int i = 0; i < 70; i++) ride.Step(default);
        Assert.True(Vector3.Distance(before, ride.PositionOf(1)) > 3.5f);
    }

    [Fact]
    public void ARareActionComesWithItsWeatherAndItsHour()
    {
        // Ludicolo dances in the rain; Ducklett takes off on a clear morning
        int ludicolo = Reed.Residents.FindIndex(r => r.Species == "Ludicolo");
        int ducklett = Reed.Residents.FindIndex(r => r.Species == "Ducklett");
        var rain = new TourRide(Reed, 3, 12, TourWeather.Rain);
        var clear = new TourRide(Reed, 3, 7, TourWeather.Clear);
        for (int i = 0; i < 175 * 30; i++)
        {
            rain.Step(default);
            clear.Step(default);
        }
        Assert.True(rain.RareSeen(ludicolo));
        Assert.False(rain.RareSeen(ducklett));
        Assert.False(clear.RareSeen(ludicolo));
        Assert.True(clear.RareSeen(ducklett));
    }

    [Fact]
    public void ABoxIsProjectedAsWorkedOutByHand()
    {
        // A lens at the origin looking along +z with tan(fov/2) = 0.5: a subject one tall at z 10 spans 1/10 / 0.5 of
        // half the frame's height, 108 pixels, from y 432 to 540; its width 0.8 of that, 86.4, about x 960
        float fov = 2f * MathF.Atan(0.5f);
        var cam = new TourCamera(Vector3.Zero, 0f, 0f, fov);
        var box = PhotoJudge.BoxOf(cam, new TourSubject { X = 0, Y = 0, Z = 10, Height = 1f })!.Value;
        Assert.Equal(432f, box.Y, 2);
        Assert.Equal(108f, box.H, 2);
        Assert.Equal(86.4f, box.W, 2);
        Assert.Equal(960f - 43.2f, box.X, 2);
        // Behind the lens: no box
        Assert.Null(PhotoJudge.BoxOf(cam, new TourSubject { Z = -5, Height = 1f }));
    }

    [Fact]
    public void APhotographsPointsAndStarsAreWorkedOutByHand()
    {
        // The lens at height 0.5, tan(fov/2) = 0.5; a Psyduck 1 tall at z 1.5 facing the lens (π faces −z): its box
        // from feet y 540 + (0.5/1.5)/0.5 × 540 = 900 to head y 540 − (0.5/1.5)/0.5 × 540 = 180, 720 tall (two thirds,
        // so the most for its size, 400); its middle at the frame's (200); facing the lens (200); uncovered (200); alone
        // (0): 1000 points, eating (two stars): 1000 × 3 / 2 = 1500
        float fov = 2f * MathF.Atan(0.5f);
        var frame = new TourFrame
        {
            EyeY = 0.5f, FovY = fov,
            Subjects = new() { new TourSubject { Resident = 0, Look = "Psyduck", Act = TourAct.Eat, Z = 1.5f, Height = 1f, Facing = MathF.PI } }
        };
        var j = PhotoJudge.Best(frame)!;
        Assert.Equal(720f, j.Box.H, 2);
        Assert.Equal(400, j.Size);
        Assert.Equal(200, j.Placement);
        Assert.Equal(200, j.Facing);
        Assert.Equal(200, j.Uncovered);
        Assert.Equal(0, j.Kind);
        Assert.Equal(2, j.Stars);
        Assert.Equal(1500, j.Points);

        // Turned away it earns nothing for facing; the size's falls past two thirds and rises to it from nothing
        var away = frame with { Subjects = new() { frame.Subjects[0] with { Facing = 0f } } };
        Assert.Equal(0, PhotoJudge.Best(away)!.Facing);
        Assert.Equal(200, PhotoJudge.SizePoints(1f / 6f));
        Assert.Equal(200, PhotoJudge.SizePoints(5f / 6f));
        Assert.Equal(0, PhotoJudge.SizePoints(1f));
        Assert.Equal(new[] { 1, 1, 2, 2, 2, 2, 3, 3, 3, 3, 4 }, Enum.GetValues<TourAct>().Select(PhotoJudge.StarsOf));
    }

    [Fact]
    public void ANearerSubjectCoversOneBehindIt()
    {
        float fov = 2f * MathF.Atan(0.5f);
        var frame = new TourFrame
        {
            FovY = fov,
            Subjects = new()
            {
                new TourSubject { Resident = 0, Look = "Bidoof", Act = TourAct.Idle, Z = 4f, Height = 1f, Facing = MathF.PI },
                new TourSubject { Resident = 1, Look = "Bidoof", Act = TourAct.Idle, Z = 8f, Height = 2f, Facing = MathF.PI }
            }
        };
        var judged = PhotoJudge.Judge(frame);
        var near = judged.Single(j => j.Resident == 0);
        Assert.Equal(1f, near.Visible, 3);
        // The far one shares its middle with the near one: the near box's overlap over its own area is taken off
        if (judged.FirstOrDefault(j => j.Resident == 1) is { } far) Assert.True(far.Visible < 1f);
    }

    [Fact]
    public void TheReportKeepsTheBestAndOpensByItsTotal()
    {
        var report = new TourReport();
        Assert.True(report.Keep(new TourBest { Course = "reed_river", Species = "Psyduck", Stars = 2, Points = 900 }));
        Assert.False(report.Keep(new TourBest { Course = "reed_river", Species = "Psyduck", Stars = 2, Points = 800 }));
        Assert.True(report.Keep(new TourBest { Course = "reed_river", Species = "Psyduck", Stars = 2, Points = 1100 }));
        Assert.True(report.Keep(new TourBest { Course = "reed_river", Species = "Psyduck", Stars = 3, Points = 1000 }));
        Assert.True(report.Keep(new TourBest { Course = "reed_river", Species = "Lotad", Stars = 1, Points = 500 }));
        // The total is each species' best: 1100 + 500
        Assert.Equal(1600, report.Total);
        Assert.Equal(3, report.Best.Count);
        Assert.True(report.IsOpen(Reed));
        Assert.False(report.IsOpen(new TourCourseData { Id = "later", OpensAt = 2000 }));
        Assert.True(report.KeepRide("reed_river", 1600));
        Assert.False(report.KeepRide("reed_river", 1200));
    }

    [Fact]
    public void AnAutopilotRideIsJudgedIntoTheReport()
    {
        var ride = new TourRide(Reed, 9, 12, TourWeather.Clear);
        TourAutopilot.Play(ride);
        Assert.True(ride.Over);
        Assert.NotEmpty(ride.Photos);
        var report = new TourReport();
        int total = report.Take(ride);
        Assert.True(total > 0);
        Assert.Equal(total, report.Rides["reed_river"]);
        Assert.Equal(TourResults.ShownOf(report.Best).Count, report.Best.Select(b => b.Species).Distinct().Count());
    }

    [Fact]
    public void TheReportRoundTripsThroughASave()
    {
        var ride = new TourRide(Reed, 9, 12, TourWeather.Clear);
        TourAutopilot.Play(ride);
        var report = new TourReport();
        report.Take(ride);
        var save = new SaveData { PhotoTour = report.ToSave() };
        string json = System.Text.Json.JsonSerializer.Serialize(save);
        var back = System.Text.Json.JsonSerializer.Deserialize<SaveData>(json)!;
        var again = new TourReport();
        again.Load(back.PhotoTour);
        Assert.Equal(report.Total, again.Total);
        Assert.Equal(System.Text.Json.JsonSerializer.Serialize(report.ToSave()), System.Text.Json.JsonSerializer.Serialize(again.ToSave()));
        // A best's frame judges the same after the round trip
        var b = again.Best[0];
        Assert.Equal(b.Points, PhotoJudge.Judge(b.Frame, Reed).Where(j => j.Look == b.Species && j.Stars == b.Stars).Max(j => j.Points));
        // An empty report saves nothing
        Assert.Null(new TourReport().ToSave());
    }

    [Fact]
    public void TheScreenOpensRidesAndJudges()
    {
        var screen = new PhotoTourScreen { Autopilot = true };
        screen.Open(Reed, 5, 12, TourWeather.Clear);
        screen.Advance(1f, 0, 0, 0, ready: false);
        Assert.Equal(TourPhase.Opening, screen.Phase);
        Assert.Equal(1f, screen.Dark);
        screen.Advance(1f, 0, 0, 0, ready: true);
        Assert.Equal(TourPhase.Riding, screen.Phase);
        for (int i = 0; i < 200 * 10 && screen.Phase != TourPhase.Judged; i++) screen.Advance(0.1f, 0, 0, 0, true);
        Assert.Equal(TourPhase.Judged, screen.Phase);
        Assert.True(screen.Total > 0);
        screen.Keep();
        Assert.True(screen.TakeKeepRequest());
        Assert.False(screen.TakeKeepRequest());
        screen.Leave();
        Assert.False(screen.IsActive);
    }
}
