using System;
using System.Collections.Generic;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>
/// The photo tour's report (plan 44 · SD6; style guide, "The photo tour", "The report"): a page to each course (its
/// tab shut, with the total that opens it, until the report reaches it), the course's species with the best photograph of
/// each, its stars and points, and the chosen one's print redrawn from its record beside the list; under it the report's
/// total and the guide's word for it. Left and right turn the course, up and down the row, B leaves. The logic takes no
/// input but what it is handed; <see cref="Update"/> reads the keys.
/// </summary>
public sealed class TourReportScreen
{
    /// <summary>How many rows of a course's species show at once.</summary>
    public const int RowsShown = 7;

    /// <summary>The report's totals at which the guide's word changes, lowest first.</summary>
    private static readonly int[] BandFloors = { 0, 5000, 15000, 30000, 50000, 75000 };

    public static int Bands => BandFloors.Length;

    /// <summary>The guide's word for a total: <c>photoTour.report.word.&lt;band&gt;</c>.</summary>
    public static int BandOf(int total)
    {
        int band = 0;
        for (int i = 0; i < BandFloors.Length; i++)
            if (total >= BandFloors[i]) band = i;
        return band;
    }

    private TourReport report = new();

    public bool IsActive { get; private set; }
    public int CourseIndex { get; private set; }
    public int Cursor { get; private set; }
    public int FirstShown { get; private set; }

    /// <summary>The report shown.</summary>
    public TourReport Report => report;

    public IReadOnlyList<TourCourseData> Courses => PhotoTourCourses.All;
    public TourCourseData Course => Courses[Math.Clamp(CourseIndex, 0, Courses.Count - 1)];
    public bool CourseOpen => report.IsOpen(Course);

    /// <summary>The course's species, the best photograph of each, the best first; none on a shut course.</summary>
    public IReadOnlyList<TourBest> Rows => CourseOpen ? report.On(Course.Id) : Array.Empty<TourBest>();

    /// <summary>The photograph under the cursor, whose print is drawn; null on an empty page.</summary>
    public TourBest? Selected => Rows is { Count: > 0 } rows ? rows[Math.Clamp(Cursor, 0, rows.Count - 1)] : null;

    public void Open(TourReport shown)
    {
        report = shown;
        IsActive = true;
        CourseIndex = Cursor = FirstShown = 0;
    }

    public void MoveCursor(int step)
    {
        int count = Rows.Count;
        if (count == 0) return;
        Cursor = UiNav.Wrap(Cursor, step, count);
        FirstShown = UiNav.Window(FirstShown, Cursor, count, RowsShown);
    }

    public void ChangeCourse(int step)
    {
        CourseIndex = UiNav.Wrap(CourseIndex, step, Courses.Count);
        Cursor = FirstShown = 0;
    }

    public void Cancel() => IsActive = false;

    /// <summary>A frame of the game: the keys read.</summary>
    public void Update()
    {
        if (!IsActive) return;
        if (InputManager.IsActionPressed(GameAction.Cancel) || InputManager.IsActionPressed(GameAction.Menu)) Cancel();
        else if (InputManager.IsActionPressed(GameAction.Right)) ChangeCourse(1);
        else if (InputManager.IsActionPressed(GameAction.Left)) ChangeCourse(-1);
        else if (InputManager.IsActionPressed(GameAction.Down)) MoveCursor(1);
        else if (InputManager.IsActionPressed(GameAction.Up)) MoveCursor(-1);
    }
}
