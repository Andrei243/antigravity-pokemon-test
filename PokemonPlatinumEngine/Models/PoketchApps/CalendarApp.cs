using System;
using System.Collections.Generic;
using System.Linq;

namespace PokemonPlatinumEngine.Models.PoketchApps;

/// <summary>
/// The calendar (<c>calendar/main.c</c>): this month's days in rows of seven from Sunday, today shown, and each day
/// marked or unmarked by a touch. The marks are kept (<c>Poketch_SetCalendarMark</c>): the month they belong to and
/// a bit for each day; marking a day of another month forgets the old month's marks.
/// </summary>
public sealed class CalendarApp : PoketchAppState
{
    /// <summary>A day's cell, in blocks: seven columns of 6 from the left edge, six rows of 5 under the month's name.</summary>
    public const int Left = 2, Top = 6, CellW = 6, CellH = 5;

    private int markedMonth;
    private int marks;

    public override PoketchApp App => PoketchApp.Calendar;

    protected override void Opened()
    {
        if (Owner.Recall(App) is [var month, var bits]) (markedMonth, marks) = (month, bits);
    }

    /// <summary>The cell (0 to 41, row by row from Sunday) the month's first day stands in.</summary>
    public static int FirstCell(DateTime now) => (int)new DateTime(now.Year, now.Month, 1).DayOfWeek;

    public static PoketchButton Cell(int cell, int day) => new(day, Left + cell % 7 * CellW, Top + cell / 7 * CellH, CellW, CellH);

    /// <summary>Each day of the month touchable, its id the day's number.</summary>
    public override IReadOnlyList<PoketchButton> Buttons(PoketchContext context)
    {
        int first = FirstCell(context.Now), days = DateTime.DaysInMonth(context.Now.Year, context.Now.Month);
        return Enumerable.Range(1, days).Select(d => Cell(first + d - 1, d)).ToList();
    }

    public bool IsMarked(int month, int day) => month == markedMonth && day is >= 1 and <= 31 && (marks & (1 << (day - 1))) != 0;

    public override void Press(int day, PoketchContext context)
    {
        if (day < 1 || day > DateTime.DaysInMonth(context.Now.Year, context.Now.Month)) return;
        int month = context.Now.Month;
        // Poketch_SetCalendarMark / Poketch_ClearCalendarMark: another month's marks go first
        if (month != markedMonth)
        {
            markedMonth = month;
            marks = 0;
        }
        marks ^= 1 << (day - 1);
        Owner.Keep(App, new List<int> { markedMonth, marks });
        context.Sound("poketch");
    }
}
