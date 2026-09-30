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
}
