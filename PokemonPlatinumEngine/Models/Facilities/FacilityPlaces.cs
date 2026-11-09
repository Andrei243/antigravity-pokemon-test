using System;
using System.Collections.Generic;
using System.Linq;

namespace PokemonPlatinumEngine.Models.Facilities;

/// <summary>
/// The later battle places a script names with <c>facility &lt;place&gt;</c> (plan 37 · FC2), each with its ways and the
/// room of each: the streak house first, and each later place of plan 37 a row here and a class of its own, never a
/// command of its own. No drawing or input.
/// </summary>
public static class FacilityPlaces
{
    /// <summary>The places' words, in order; a place's number is its place here.</summary>
    public static readonly string[] Names = { StreakHouse.Place, BattleTree.Place };

    public static bool IsPlace(string word) => Array.IndexOf(Names, word) >= 0;

    public static int Index(string place) => Array.IndexOf(Names, place);

    /// <summary>A place's ways, in the order its desk and board give them.</summary>
    public static IReadOnlyList<string> WaysOf(string place) => place switch
    {
        StreakHouse.Place => StreakHouse.Ways,
        BattleTree.Place => BattleTree.Ways,
        _ => throw new ArgumentException($"No later battle place is called {place}", nameof(place))
    };

    /// <summary>A way's room, normal or hard.</summary>
    public static StreakRoom RoomOf(string place, string way, bool hard) => place switch
    {
        StreakHouse.Place => StreakHouse.Room(way, hard),
        BattleTree.Place => BattleTree.Room(way, hard),
        _ => throw new ArgumentException($"No later battle place is called {place}", nameof(place))
    };

    /// <summary>How many of a place's ways have a hard room and so a trophy (every way but one with no host: the Battle Tree's Multi room).</summary>
    public static int TrophyWays(string place) => WaysOf(place).Count(w => RoomOf(place, w, true).Hard);

    /// <summary>How many of a place's hard rooms' trophies the story holds (the records' board's print).</summary>
    public static int Trophies(string place, Story.StoryState story) => place switch
    {
        StreakHouse.Place => StreakHouse.Trophies(story),
        BattleTree.Place => BattleTree.Trophies(story),
        _ => 0
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
