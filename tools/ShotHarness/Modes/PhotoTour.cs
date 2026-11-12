using System;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.UI;

partial class Harness
{
    // Plan 44 · SD5 (tu01 to tu08; not part of `all`): the photo tour's guide on Pastoria's observatory floor, a ride on the
    // Reed River from its opening, the viewfinder, a fruit thrown, the whistle, the closing and the judged panel, and the
    // best photograph kept in the photo book. The ride is played by the screen's own clock with its autopilot's hand on
    // the aim (`PhotoTourScreen.Autopilot`); the fruit and the whistle are pressed as a player presses them.
    public void TourMode()
    {
        game.Pokedex.UnlockNational();
        if (Environment.GetEnvironmentVariable("TOUR_PART") == "courses")
        {
            At("PastoriaCityObservatoryGate2F", 10, 5, Direction.Right);
            TourCourses();
            return;
        }
        At("PastoriaCityObservatoryGate2F", 10, 5, Direction.Right);
        game.LocationSign.Hide();
        Frames(30);
        Shot("tu01_guide");

        game.OpenPhotoTour("reed_river");
        var s = game.PhotoTour;
        s.Autopilot = true;
        for (int i = 0; i < 1800 && !game.TourModelsReady; i++) Frames(1);
        Frames(30);
        Shot("tu02_ride_start");
        Frames(60 * 6);
        Shot("tu03_viewfinder");
        s.Throw();
        Frames(18);
        Shot("tu04_fruit");
        Frames(60 * 3);
        s.Whistle();
        Frames(10);
        Shot("tu05_whistle");
        var ride = s.Ride!;
        Console.WriteLine($"tour: weather {ride.Weather} hour {ride.Hour} seed {ride.Seed}");
        // On to the end of the rail (or the film), the closing caught half way
        for (int i = 0; i < 60 * 200 && s.Phase == TourPhase.Riding; i++) Frames(1);
        Frames(24);
        Shot("tu08_ride_end");
        for (int i = 0; i < 120 && s.Phase != TourPhase.Judged; i++) Frames(1);
        Frames(30);
        Shot("tu07_judged");
        Console.WriteLine($"tour: {ride.Photos.Count} photographs, total {s.Total}, rares {string.Join(", ", Enumerable.Range(0, ride.Course.Residents.Count).Where(ride.RareSeen).Select(i => ride.Course.Residents[i].Species))}");
        foreach (var b in s.Shown) Console.WriteLine($"  {b.Species} {b.Stars}★ {b.Points}");
        game.KeepTourPhoto();
        Frames(3);
        Shot("tu09_kept");
        Console.WriteLine($"book {game.PhotoBook.Count}: {string.Join(", ", game.PhotoBook.Prints.Select(x => x.File + "@" + x.How + " " + x.Lead))}; report {game.TourReport.Total}");
        s.Leave();
        Frames(10);

        TourCourses();
    }

    // Plan 44 · SD6 (tu06, tu20 to tu25, tu10, tu11): each course ridden in the weather and at the hour of one of its rare
    // actions (the ride's seed is still drawn from the game's dice), fast-forwarded by the screen's own ticks to just
    // before it and shot as it plays; then the report on its first page and on a four-star print. The courses past the
    // Reed River are opened by points laid in the report for the harness alone and taken out again before the report's
    // shots, so the report shows only what the rides took. `TOUR_PART=courses` runs this part alone.
    private void TourCourses()
    {
        var s = game.PhotoTour;
        var report = game.TourReport;
        var before = report.ToSave();
        report.Keep(new TourBest { Course = "harness", Species = "Bidoof", Stars = 1, Points = 1_000_000 });
        var rares = new (string Course, string Species, string Shot)[]
        {
            ("reed_river", "Ludicolo", "tu06_rare"),
            ("reed_river", "Ludicolo", "tu20_reed_river_rare"),
            ("deep_wood", "Butterfree", "tu21_deep_wood_rare"),
            ("reef_shore", "Lapras", "tu22_reef_shore_rare"),
            ("cavern", "Solrock", "tu23_cavern_rare"),
            ("high_pass", "Snover", "tu24_high_pass_rare"),
            ("night_garden", "Umbreon", "tu25_night_garden_rare")
        };
        foreach (var (courseId, species, shot) in rares)
        {
            var course = PhotoTourCourses.Get(courseId)!;
            int index = course.Residents.FindIndex(r => r.Species == species);
            var rare = course.Residents[index].Rare!;
            var weather = rare.Weather ?? course.Weathers[0];
            int hour = rare.Hours is { Length: >= 2 } h ? h[0] : course.Night ? 22 : 12;
            game.OpenPhotoTour(courseId, weather, hour);
            s.Autopilot = true;
            for (int i = 0; i < 1800 && !game.TourModelsReady; i++) Frames(1);
            for (int i = 0; i < 120 && s.Phase != TourPhase.Riding; i++) Frames(1);
            var ride = s.Ride!;
            // The screen's own ticks, as many as the frames would have played, to a second and a half before it
            while (s.Phase == TourPhase.Riding && ride.Time < rare.From - 1.5f) s.Advance(TourRide.Tick, 0, 0, 0, true);
            for (int i = 0; i < 600 && ride.ActOf(index) != TourAct.Rare; i++) Frames(1);
            Frames(80);
            Shot(shot);
            Console.WriteLine($"{shot}: {courseId} {weather} {ride.Hour}h, {species} {ride.ActOf(index)} at {ride.Time:0.0}s, film {ride.Photos.Count}");
            s.EndEarly();
            for (int i = 0; i < 200 && s.Phase != TourPhase.Judged; i++) Frames(1);
            var four = s.Bests.Where(b => b.Stars == 4).Select(b => b.Species).ToList();
            Console.WriteLine($"  judged: {s.Shown.Count} species, total {s.Total}, four stars: {string.Join(", ", four)}");
            s.Leave();
            Frames(10);
        }

        // The report as the rides left it, without the harness's points
        var after = report.ToSave()!;
        report.Load(after with { Best = after.Best.Where(b => b.Course != "harness").ToList() });
        Console.WriteLine($"report: {report.Total} points, {report.FourStars} four-star, medals {game.Medals.Count}");
        game.OpenTourReport();
        var r = game.TourReportScreen;
        for (int i = 0; i < 600 && !game.TourPrintShown; i++) Frames(1);
        Frames(20);
        Shot("tu10_report");
        // On to a four-star print, the course turned and the row moved as a player would
        for (int c = 0; c < r.Courses.Count; c++)
        {
            int at = r.Rows.ToList().FindIndex(b => b.Stars == 4);
            if (at >= 0)
            {
                for (int k = 0; k < at; k++) r.MoveCursor(1);
                break;
            }
            r.ChangeCourse(1);
        }
        for (int i = 0; i < 600 && !game.TourPrintShown; i++) Frames(1);
        Frames(20);
        Shot("tu11_print");
        Console.WriteLine($"print: {r.Course.Id} {r.Selected?.Species} {r.Selected?.Stars}★ {r.Selected?.Points}");
        r.Cancel();
        Frames(10);
        report.Load(before);
    }
}
