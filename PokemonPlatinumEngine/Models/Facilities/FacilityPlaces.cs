using System;
using System.Collections.Generic;

namespace PokemonPlatinumEngine.Models.Facilities;

/// <summary>
/// The later battle places a script names with <c>facility &lt;place&gt;</c> (plan 37 · FC2), each with its ways and the
/// room of each: the streak house first, and each later place of plan 37 a row here and a class of its own, never a
/// command of its own. No drawing or input.
/// </summary>
public static class FacilityPlaces
{
    /// <summary>The places' words, in order; a place's number is its place here.</summary>
    public static readonly string[] Names = { StreakHouse.Place };

    public static bool IsPlace(string word) => Array.IndexOf(Names, word) >= 0;

    public static int Index(string place) => Array.IndexOf(Names, place);

    /// <summary>A place's ways, in the order its desk and board give them.</summary>
    public static IReadOnlyList<string> WaysOf(string place) => place switch
    {
        StreakHouse.Place => StreakHouse.Ways,
        _ => throw new ArgumentException($"No later battle place is called {place}", nameof(place))
    };

    /// <summary>A way's room, normal or hard.</summary>
    public static StreakRoom RoomOf(string place, string way, bool hard) => place switch
    {
        StreakHouse.Place => StreakHouse.Room(way, hard),
        _ => throw new ArgumentException($"No later battle place is called {place}", nameof(place))
    };

    /// <summary>Every word of every place's ways, which the script reader takes after <c>way</c>.</summary>
    public static IEnumerable<string> AllWays
    {
        get
        {
            foreach (string place in Names)
                foreach (string way in WaysOf(place)) yield return way;
        }
    }
}
