using System;
using System.Linq;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>The photo tour's report (plan 44 · SD6; style guide, "The photo tour", "The report").</summary>
internal static partial class ModernUi
{
    private const float ReportListWidth = 820, ReportPrintInset = 16;

    /// <summary>Each course's tab colour, in the courses' order (style guide, "The report").</summary>
    private static readonly Color[] CourseColours =
    {
        new(76, 170, 96, 255),   // the Reed River: meadow green
        new(46, 120, 74, 255),   // the Deep Wood: deep green
        new(60, 150, 214, 255),  // the Reef Shore: sea blue
        new(128, 112, 100, 255), // the Cavern: stone brown
        new(132, 156, 196, 255), // the High Pass: slate blue
        new(124, 92, 196, 255)   // the Night Garden: dusk violet
    };

    public static Color CourseColour(int index) => CourseColours[Math.Clamp(index, 0, CourseColours.Length - 1)];

    /// <summary>The report: a tab to each course, the course's species on the left, the chosen one's print on the right.</summary>
    public static void DrawTourReport(int sw, int sh, TourReportScreen screen, Action<Rectangle>? drawPrint)
    {
        var report = screen.Report;
        Backdrop(sw, sh);
        ScreenTitle(Text.Of("photoTour.report.heading"));
        Hints(sw - Margin, 44, Hint("shared.keys.arrows", "photoTour.hints.course"), Hint("shared.keys.x", "photoTour.hints.back"));

        var tabs = screen.Courses.Select((c, i) =>
        {
            string id = c.Id;
            return (Text.Of($"photoTour.course.{id}"), report.IsOpen(c) ? UiIcon.Star : UiIcon.Key, CourseColour(i));
        }).ToList();
        Tabs(new Rectangle(Margin, ContentTop, sw - Margin * 2, 76), tabs, screen.CourseIndex);

        float top = ContentTop + 96;
        var list = new Rectangle(Margin, top, ReportListWidth, 24 + TourReportScreen.RowsShown * 92 + 16);
        Panel(list, 34);
        var rows = screen.Rows;
        if (!screen.CourseOpen)
            UiFonts.Draw(Text.Of("photoTour.report.shut", ("points", screen.Course.OpensAt)), list.X + 40, list.Y + 40, 30, Muted, UiWeight.Black);
        else if (rows.Count == 0)
            UiFonts.Draw(Text.Of("photoTour.report.empty"), list.X + 40, list.Y + 40, 30, Muted, UiWeight.Black);
        int shown = Math.Min(rows.Count - screen.FirstShown, TourReportScreen.RowsShown);
        for (int k = 0; k < shown; k++)
        {
            int i = screen.FirstShown + k;
            var best = rows[i];
            var r = new Rectangle(list.X + 24, list.Y + 24 + k * 92, list.Width - 72, 84);
            var ink = ListRow(r, i == screen.Cursor, LocalNames.Species(best.Species), nameX: 30, size: 32);
            for (int s = 0; s < best.Stars; s++) Star(new Vector2(r.X + 380 + s * 40, r.Y + r.Height / 2f), 16, Gold);
            string points = Text.Of("photoTour.points", ("points", best.Points));
            UiFonts.DrawCentered(points, r.X + r.Width - 26 - UiFonts.Measure(points, 30, UiWeight.Black), r.Y + r.Height / 2f, 30, ink, UiWeight.Black);
        }
        ScrollBar(list, screen.FirstShown, TourReportScreen.RowsShown, rows.Count);

        // The print: the chosen photograph redrawn from its record, framed like a photograph on a white mount
        float px = list.X + list.Width + 32, pw = sw - Margin - px;
        var print = new Rectangle(px + ReportPrintInset, top + ReportPrintInset, pw - ReportPrintInset * 2, (pw - ReportPrintInset * 2) * 9f / 16f);
        var mount = new Rectangle(px, top, pw, print.Height + ReportPrintInset * 2);
        UiShapes.Fill(mount, 10, new Color(250, 248, 240, 255));
        if (screen.Selected is { } chosen)
        {
            if (drawPrint != null) drawPrint(print);
            else
            {
                UiShapes.Fill(print, 4, new Color(36, 40, 52, 255));
                UiFonts.DrawCentered(Text.Of("photoTour.report.developing"), print.X + print.Width / 2f - UiFonts.Measure(Text.Of("photoTour.report.developing"), 30, UiWeight.Black) / 2f,
                    print.Y + print.Height / 2f, 30, Color.White, UiWeight.Black);
            }
            float y = mount.Y + mount.Height + 18;
            UiFonts.Draw(LocalNames.Species(chosen.Species), px, y, 40, Color.White, UiWeight.Black);
            for (int s = 0; s < chosen.Stars; s++) Star(new Vector2(px + pw - 22 - s * 46, y + 24), 20, Gold);
            y += 56;
            if (chosen.Stars >= 4 && RareKindOf(chosen) is { } kind)
            {
                UiFonts.Draw(Text.Of($"photoTour.rare.{kind}"), px, y, 30, Gold, UiWeight.Black);
                y += 42;
            }
            UiFonts.Draw(Text.Of("photoTour.points", ("points", chosen.Points)), px, y, 30, Color.White, UiWeight.Bold);
        }
        else UiShapes.Fill(print, 4, new Color(210, 206, 196, 255));

        // The report's total, the course's and its best ride, and the guide's word
        float foot = list.Y + list.Height + 24;
        UiFonts.Draw(Text.Of("photoTour.report.total", ("points", report.Total)), Margin, foot, 36, Color.White, UiWeight.Black);
        if (screen.CourseOpen)
        {
            string onCourse = Text.Of("photoTour.report.course", ("points", report.TotalOn(screen.Course.Id)));
            UiFonts.Draw(onCourse, Margin + 520, foot + 6, 28, Color.White, UiWeight.Bold);
            if (report.Rides.TryGetValue(screen.Course.Id, out int ride))
                UiFonts.Draw(Text.Of("photoTour.report.ride", ("points", ride)), Margin + 1000, foot + 6, 28, Color.White, UiWeight.Bold);
        }
        int band = TourReportScreen.BandOf(report.Total);
        UiFonts.Draw(Text.Of($"photoTour.report.word.{band}"), Margin, foot + 54, 30, Gold, UiWeight.Bold);
    }

    /// <summary>The rare action a four-star photograph caught: its subject's, on its course.</summary>
    private static string? RareKindOf(TourBest best)
    {
        if (PhotoTourCourses.Get(best.Frame.Course) is not { } course) return null;
        var subject = best.Frame.Subjects.FirstOrDefault(s => s.Look == best.Species && s.Act == TourAct.Rare);
        return subject != null && subject.Resident >= 0 && subject.Resident < course.Residents.Count ? course.Residents[subject.Resident].Rare?.Kind : null;
    }
}
