using System;

namespace PokemonPlatinumEngine.Core;

/// <summary>Platinum's five light states (pret/pokeplatinum, src/rtc.c: <c>TimeOfDayForHour</c>).</summary>
public enum TimeOfDay { Morning, Day, Twilight, Night, LateNight }

/// <summary>
/// The time of day, taken from the computer's clock like the DS's real-time clock, unless the options fix it.
/// </summary>
public static class GameClock
{
    // One entry per hour, exactly as in the decompilation
    private static readonly TimeOfDay[] ByHour =
    {
        TimeOfDay.LateNight, TimeOfDay.LateNight, TimeOfDay.LateNight, TimeOfDay.LateNight,
        TimeOfDay.Morning, TimeOfDay.Morning, TimeOfDay.Morning, TimeOfDay.Morning, TimeOfDay.Morning, TimeOfDay.Morning,
        TimeOfDay.Day, TimeOfDay.Day, TimeOfDay.Day, TimeOfDay.Day, TimeOfDay.Day, TimeOfDay.Day, TimeOfDay.Day,
        TimeOfDay.Twilight, TimeOfDay.Twilight, TimeOfDay.Twilight,
        TimeOfDay.Night, TimeOfDay.Night, TimeOfDay.Night, TimeOfDay.Night
    };

    /// <summary>A fixed time of day chosen in the options; null follows the clock.</summary>
    public static TimeOfDay? Fixed { get; set; }

    public static TimeOfDay ForHour(int hour) => ByHour[((hour % 24) + 24) % 24];

    /// <summary>Hour of the day with its fraction. A fixed time of day sits in the middle of its period.</summary>
    public static float Hour => Fixed switch
    {
        TimeOfDay.LateNight => 2f,
        TimeOfDay.Morning => 7f,
        TimeOfDay.Day => 13.5f,
        TimeOfDay.Twilight => 18.5f,
        TimeOfDay.Night => 22f,
        _ => (float)DateTime.Now.TimeOfDay.TotalHours
    };

    public static TimeOfDay Now => ForHour((int)Hour);

    public static bool IsNight => Now is TimeOfDay.Night or TimeOfDay.LateNight;

    /// <summary>A day fixed by a tool or a test; null follows the computer's calendar.</summary>
    public static DateTime? FixedDate { get; set; }

    /// <summary>
    /// Today's date, for what counts days (Pokérus, plan 06 · R10): the computer's, like the DS's real-time clock,
    /// unless a tool fixed it.
    /// </summary>
    public static DateTime Today => FixedDate?.Date ?? DateTime.Today;

    /// <summary>
    /// This moment, for what counts minutes (the honey trees, plan 06 · R13): today at the hour of the day. Fixed
    /// by a tool (a date and a time of day), it stands still.
    /// </summary>
    public static DateTime Moment => Fixed == null && FixedDate == null ? DateTime.Now : Today.AddHours(Hour);
}
