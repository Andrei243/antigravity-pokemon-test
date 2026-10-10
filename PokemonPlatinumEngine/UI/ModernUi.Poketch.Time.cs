using System;
using Raylib_cs;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models.PoketchApps;

namespace PokemonPlatinumEngine.UI;

/// <summary>The Pokétch's apps of the clock and the calendar, and the link searcher (plan 06 · R14b; style guide, "The Pokétch").</summary>
internal static partial class ModernUi
{
    private static readonly string[] Months =
        { "JANUARY", "FEBRUARY", "MARCH", "APRIL", "MAY", "JUNE", "JULY", "AUGUST", "SEPTEMBER", "OCTOBER", "NOVEMBER", "DECEMBER" };

    /// <summary>
    /// The analog watch: a round dial of blocks 33 across, a block of ink at each hour and two at the quarters, and
    /// the two hands drawn block by block from the middle (the hour hand 9 long, the minute hand 14); a touch lights
    /// the dial's paper.
    /// </summary>
    private static void PoketchAnalogWatch(Rectangle screen, AnalogWatchApp app, PoketchContext context)
    {
        if (app.Bright > 0f) Raylib.DrawRectangleRec(new Rectangle(screen.X + Block, screen.Y + Block, screen.Width - 2 * Block, screen.Height - 2 * Block), Lighter(LcdPaper, 0.25f));
        const int cx = 22, cy = 18, radius = 16;
        // The dial's edge: every block whose middle lies a block's width from the circle
        for (int y = cy - radius - 1; y <= cy + radius + 1; y++)
            for (int x = cx - radius - 1; x <= cx + radius + 1; x++)
            {
                float d = MathF.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                if (MathF.Abs(d - radius) < 0.5f) LcdCells(screen, x, y, 1, 1, LcdMid);
            }
        for (int h = 0; h < 12; h++)
        {
            float a = h / 12f * MathF.Tau;
            int x = cx + (int)MathF.Round(MathF.Sin(a) * (radius - 2)), y = cy - (int)MathF.Round(MathF.Cos(a) * (radius - 2));
            int size = h % 3 == 0 ? 2 : 1;
            LcdCells(screen, x - (size - 1) / 2 - (size == 2 && x < cx ? 1 : 0), y - (size == 2 && y < cy ? 1 : 0), size, size, LcdInk);
        }
        var (hour, minute) = AnalogWatchApp.Hands(context.Now);
        Hand(screen, cx, cy, minute / 60f, 14, LcdInk);
        Hand(screen, cx, cy, hour / 60f, 9, LcdInk);
        LcdCells(screen, cx - 1, cy - 1, 2, 2, LcdInk);
    }

    /// <summary>A hand of blocks from the dial's middle, a turn's fraction round from twelve.</summary>
    private static void Hand(Rectangle screen, int cx, int cy, float turn, int length, Color color)
    {
        float a = turn * MathF.Tau, dx = MathF.Sin(a), dy = -MathF.Cos(a);
        for (int i = 0; i <= length * 2; i++)
        {
            float t = i / 2f;
            LcdCells(screen, cx + (int)MathF.Floor(dx * t), cy + (int)MathF.Floor(dy * t), 1, 1, color);
        }
    }

    /// <summary>
    /// The calendar: the month's name at the top, the days in rows of seven from Sunday, each a cell 6 blocks by 5
    /// with its number (Sundays in the middle tone); today's cell inked with its number in the paper's colour, a marked day filled in the middle tone.
    /// </summary>
    private static void PoketchCalendar(Rectangle screen, CalendarApp app, PoketchContext context)
    {
        var now = context.Now;
        LcdText(screen, Months[now.Month - 1], PoketchAppState.Columns / 2f, 1, 28);
        int first = CalendarApp.FirstCell(now);
        foreach (var cell in app.Buttons(context))
        {
            int day = cell.Id;
            bool today = day == now.Day, marked = app.IsMarked(now.Month, day);
            // A marked day is filled in the middle tone, today in ink; the number stays clear of both
            if (today || marked) LcdCells(screen, cell.X + 1, cell.Y + 1, cell.W - 1, cell.H - 1, today ? LcdInk : LcdMid);
            bool sunday = (first + day - 1) % 7 == 0;
            var color = today ? LcdPaper : marked ? LcdInk : sunday ? LcdMid : LcdInk;
            LcdText(screen, day.ToString(), cell.X + 0.5f + cell.W / 2f, cell.Y + 1, 24, color);
        }
    }

    /// <summary>
    /// The link searcher: a word to touch the screen, two waves of blocks spreading from the middle while it
    /// searches, then the number of people found in the seven-segment figures (always none here).
    /// </summary>
    private static void PoketchLinkSearcher(Rectangle screen, LinkSearcherApp app, PoketchContext context)
    {
        if (app.Searching > 0f)
        {
            LcdText(screen, "SEARCHING...", PoketchAppState.Columns / 2f, 3, 28);
            float t = (float)(FrameClock.Now % 1.0);
            for (int wave = 0; wave < 2; wave++)
            {
                int r = 2 + (int)((t + wave * 0.5f) % 1f * 12f);
                for (int y = -r; y <= r; y++)
                    for (int x = -r; x <= r; x++)
                        if (MathF.Abs(MathF.Sqrt(x * x + y * y) - r) < 0.5f) LcdCells(screen, 22 + x, 22 + y, 1, 1, LcdMid);
            }
            LcdCells(screen, 21, 21, 2, 2, LcdInk);
            return;
        }
        if (!app.Searched)
        {
            LcdText(screen, "TOUCH TO SEARCH", PoketchAppState.Columns / 2f, 14, 28);
            LcdText(screen, "FOR PEOPLE NEARBY", PoketchAppState.Columns / 2f, 19, 28);
            return;
        }
        SegmentDigit(screen.X + 19 * Block, screen.Y + 7 * Block, app.Found);
        LcdText(screen, "NOBODY NEARBY", PoketchAppState.Columns / 2f, 24, 28);
    }
}
