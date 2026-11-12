using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 44 · SD6: the photo tour's six courses, the report and the guide's rewards, played with no window: every
/// resident of its course's habitats, every rare action reached by a ride at its weather and hour, every course opened by
/// the report, a print's record naming the subjects it redraws, the rewards handed over in turn, and the autopilot no
/// longer shooting a resident turned away.
/// </summary>
public class PhotoTourCoursesTests
{
    private readonly ITestOutputHelper output;

    public PhotoTourCoursesTests(ITestOutputHelper output) => this.output = output;

    [Fact]
    public void TheSixCoursesReadWithTheirLooksAndTheirResidentsLiveInTheirHabitats()
    {
        var courses = PhotoTourCourses.All;
        Assert.Equal(new[] { "reed_river", "deep_wood", "reef_shore", "cavern", "high_pass", "night_garden" }, courses.Select(c => c.Id));
        Assert.Equal(TourLooks.All, courses.Select(c => c.Look));
        // Each opens at more than the last, the first at once
        Assert.Equal(0, courses[0].OpensAt);
        for (int i = 1; i < courses.Count; i++) Assert.True(courses[i].OpensAt > courses[i - 1].OpensAt, courses[i].Id);
        foreach (var c in courses)
        {
            Assert.NotEmpty(c.Habitats);
            Assert.InRange(c.Residents.Count, 12, 16);
            Assert.InRange(c.Residents.Count(r => r.Rare != null), 4, 7);
            foreach (var (r, i) in c.Residents.Select((r, i) => (r, i)))
            {
                Assert.NotNull(PokemonDatabase.Get(r.Species));
                var habitat = DexEntries.HabitatOf(r.Species);
                Assert.True(habitat is { } h && c.Habitats.Contains(h), $"{c.Id}: {r.Species} lives in {habitat}");
                foreach (var step in r.Day)
                    if (step.With is int with) Assert.True(with >= 0 && with < c.Residents.Count && with != i, $"{c.Id}: {r.Species} plays with {with}");
                // The user's answer 64: a rare action hangs on the weather, the hour or both, and the weather is one the course has
                if (r.Rare is { } rare)
                {
                    Assert.True(rare.Weather != null || rare.Hours != null, $"{c.Id}: {r.Species}");
                    if (rare.Weather is { } w) Assert.Contains(w, c.Weathers);
                    Assert.Contains(rare.Kind, Rares);
                }
            }
            // Every resident stands within the lens's reach of the rail at some point of the ride
            var ride = new TourRide(c, 1, 12, c.Weathers[0]);
            foreach (var r in c.Residents)
            {
                float nearest = Enumerable.Range(0, (int)c.Seconds).Min(t =>
                {
                    var eye = ride.RailAt(t);
                    return MathF.Sqrt((eye.X - r.At[0]) * (eye.X - r.At[0]) + (eye.Z - r.At[1]) * (eye.Z - r.At[1]));
                });
                Assert.True(nearest <= 20f, $"{c.Id}: {r.Species} {nearest}");
            }
        }
    }

    /// <summary>The rare actions' kinds the words have (<c>photoTour.rare.*</c>).</summary>
    private static readonly string[] Rares =
        { "glow", "dance", "dam", "takeoff", "spark", "watch", "lift", "mimic", "glide", "song", "leap", "dive", "gems", "spin", "flutter", "shine", "snowfall", "aura", "flame", "rings", "flicker", "wisps", "sundance", "moondance" };

    [Fact]
    public void EveryRareActionsKindHasItsWords()
    {
        foreach (var kind in Rares) Assert.NotEqual($"photoTour.rare.{kind}", Text.Of($"photoTour.rare.{kind}"));
    }

    [Fact]
    public void EveryRareActionIsReachedByARideAtItsWeatherAndHourAndPhotographedAtFourStars()
    {
        var missed = new List<string>();
        foreach (var c in PhotoTourCourses.All)
            for (int i = 0; i < c.Residents.Count; i++)
            {
                var r = c.Residents[i];
                if (r.Rare is not { } rare) continue;
                var weather = rare.Weather ?? c.Weathers[0];
                int hour = rare.Hours is { Length: >= 2 } h ? h[0] : c.Night ? 22 : 12;
                var ride = new TourRide(c, 11, hour, weather);
                TourAutopilot.Play(ride);
                Assert.True(ride.RareSeen(i), $"{c.Id}: {r.Species}'s {rare.Kind} at {hour}h in {weather}");
                var best = TourResults.BestOf(ride);
                if (!best.Any(b => b.Species == r.Species && b.Stars == 4)) missed.Add($"{c.Id}: {r.Species}'s {rare.Kind}");
            }
        Assert.Empty(missed);
    }

    [Fact]
    public void EveryCourseIsOpenedByTheReport()
    {
        var report = new TourReport();
        var opened = new List<string> { "reed_river" };
        uint seed = 1;
        // Rides on every course open, each at noon in the weather its seed draws, until the last is open
        for (int round = 0; round < 12 && opened.Count < PhotoTourCourses.All.Count; round++)
            foreach (var c in PhotoTourCourses.All.Where(report.IsOpen).ToList())
            {
                var ride = new TourRide(c, seed, 12, TourRide.WeatherFor(c, seed));
                seed++;
                TourAutopilot.Play(ride);
                int total = report.Take(ride);
                output.WriteLine($"round {round}: {c.Id} {total} (report {report.Total})");
                foreach (var next in PhotoTourCourses.All.Where(n => report.IsOpen(n) && !opened.Contains(n.Id))) opened.Add(next.Id);
            }
        Assert.Equal(PhotoTourCourses.All.Select(c => c.Id), opened);
    }

    [Fact]
    public void APrintsRecordNamesTheSubjectsItRedraws()
    {
        var c = PhotoTourCourses.Get("deep_wood")!;
        var ride = new TourRide(c, 4, 12, TourWeather.Clear);
        TourAutopilot.Play(ride);
        var report = new TourReport();
        report.Take(ride);
        Assert.NotEmpty(report.Best);
        foreach (var b in report.Best)
        {
            // The frame holds every resident of the course, its subject among them, and judges it as it was kept
            Assert.Equal(c.Id, b.Frame.Course);
            Assert.Equal(c.Residents.Select(r => r.Species), b.Frame.Subjects.Select(s => s.Look));
            Assert.Contains(PhotoJudge.Judge(b.Frame, c), j => j.Look == b.Species && j.Stars == b.Stars && j.Points == b.Points);
        }
        // The report's page for a course is its species' bests, one each
        Assert.Equal(report.On(c.Id).Count, report.Best.Select(b => b.Species).Distinct().Count());
        Assert.Equal(report.TotalOn(c.Id), report.Total);
    }

    [Fact]
    public void TheGuidesRewardsAreHandedOverInTurnAsTheTotalRises()
    {
        var rewards = PhotoTourCourses.Rewards;
        Assert.NotEmpty(rewards);
        for (int i = 1; i < rewards.Count; i++) Assert.True(rewards[i].At > rewards[i - 1].At);
        foreach (var r in rewards) Assert.NotNull(ItemDatabase.Get(r.Item));

        var report = new TourReport();
        Assert.Null(report.RewardDue(rewards));
        report.Keep(new TourBest { Course = "reed_river", Species = "Psyduck", Stars = 1, Points = rewards[1].At });
        Assert.Equal(rewards[0], report.RewardDue(rewards));
        report.Claim();
        Assert.Equal(rewards[1], report.RewardDue(rewards));
        report.Claim();
        Assert.Null(report.RewardDue(rewards));
        // What was handed over is kept with the report
        var again = new TourReport();
        again.Load(report.ToSave());
        Assert.Equal(2, again.Rewarded);
        Assert.Null(again.RewardDue(rewards));
    }

    [Fact]
    public void TheNightGardenIsRiddenAfterDarkWhateverTheClock()
    {
        var garden = PhotoTourCourses.Get("night_garden")!;
        Assert.True(garden.Night);
        Assert.Equal(TourRide.NightHour, new TourRide(garden, 1, 12, TourWeather.Clear).Hour);
        Assert.Equal(23, new TourRide(garden, 1, 23, TourWeather.Clear).Hour);
        Assert.Equal(2, new TourRide(garden, 1, 2, TourWeather.Clear).Hour);
        Assert.Equal(12, new TourRide(PhotoTourCourses.Get("reed_river")!, 1, 12, TourWeather.Clear).Hour);
    }

    [Fact]
    public void TheAutopilotNeverShootsAResidentTurnedAway()
    {
        // One Bidoof idling ahead, its back to the lens the whole ride (180° faces −z, the way the cart goes)
        var course = new TourCourseData
        {
            Id = "test",
            Seconds = 30,
            Rail = new() { new[] { 0f, 1.6f, 0f }, new[] { 0f, 1.6f, -30f } },
            Residents = new()
            {
                new TourResidentData { Species = "Bidoof", At = new[] { 2f, -24f }, Face = 180, Day = new() { new TourStepData { Act = TourAct.Idle, Seconds = 60, Face = 180 } } }
            }
        };
        var ride = new TourRide(course, 3, 12, TourWeather.Clear);
        TourAutopilot.Play(ride);
        Assert.Empty(ride.Photos);
    }

    [Fact]
    public void TheJudgedPanelScrollsPastItsEighthSpecies()
    {
        var screen = new PhotoTourScreen { Autopilot = true };
        screen.Open(PhotoTourCourses.Get("reed_river")!, 5, 12, TourWeather.Clear);
        screen.Advance(1f, 0, 0, 0, true);
        for (int i = 0; i < 2000 && screen.Phase != TourPhase.Judged; i++) screen.Advance(0.1f, 0, 0, 0, true);
        Assert.True(screen.Shown.Count > PhotoTourScreen.RowsShown, $"{screen.Shown.Count} species");
        Assert.Equal(0, screen.Cursor);
        Assert.Equal(0, screen.FirstShown);
        for (int i = 0; i < screen.Shown.Count - 1; i++) screen.MoveCursor(1);
        Assert.Equal(screen.Shown.Count - 1, screen.Cursor);
        Assert.Equal(screen.Shown.Count - PhotoTourScreen.RowsShown, screen.FirstShown);
        screen.MoveCursor(1);
        Assert.Equal(0, screen.Cursor);
        Assert.Equal(0, screen.FirstShown);
    }

    [Fact]
    public void TheReportScreenTurnsItsCoursesAndRows()
    {
        // Two photographs of the Reed River, short of what opens the Deep Wood
        var report = new TourReport();
        report.Keep(new TourBest { Course = "reed_river", Species = "Psyduck", Stars = 2, Points = 1200 });
        report.Keep(new TourBest { Course = "reed_river", Species = "Lotad", Stars = 1, Points = 600 });
        var screen = new TourReportScreen();
        screen.Open(report);
        Assert.True(screen.IsActive);
        Assert.Equal("reed_river", screen.Course.Id);
        Assert.NotNull(screen.Selected);
        Assert.Equal(report.On("reed_river")[0], screen.Selected);
        screen.MoveCursor(1);
        Assert.Equal(report.On("reed_river")[1], screen.Selected);
        // The next course is shut: its page has nothing on it
        screen.ChangeCourse(1);
        Assert.Equal("deep_wood", screen.Course.Id);
        Assert.False(screen.CourseOpen);
        Assert.Null(screen.Selected);
        screen.ChangeCourse(-1);
        Assert.Equal("reed_river", screen.Course.Id);
        Assert.Equal(0, screen.Cursor);
        screen.Cancel();
        Assert.False(screen.IsActive);
        // The guide's word goes by the report's total
        Assert.Equal(0, TourReportScreen.BandOf(0));
        Assert.True(TourReportScreen.BandOf(1_000_000) == TourReportScreen.Bands - 1);
    }
}
