using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models;

/// <summary>Nimbasa City's two stadiums (plan 42 · PS5).</summary>
public enum Stadium { BigStadium, SmallCourt }

/// <summary>The five sports played in them, each on a field of its own (Black 2's zones nimbasa_city_5 to _7, _10 and _11).</summary>
public enum Sport { Gridiron, Baseball, Football, Tennis, Basketball }

/// <summary>
/// Sports days in Nimbasa's stadiums (plan 42 · PS5; <c>docs/mechanics/rulings.md</c>, "Sports days"), GPU-free: which
/// sport each stadium holds on each day of the week (Black and White's table), the field each is played on, its three
/// athletes (Black 2's own, as the fields' objects stand them) and which of each athlete's four teams the story calls
/// for, and the hours a game is on (ours: the twilight hours, under the lights). The scripts of the lobbies and the
/// fields say the same with the language's own words (<c>if weekday</c>, <c>if time twilight</c>, <c>if badges</c>);
/// <c>SportsDayTests</c> holds the two to each other.
/// </summary>
public static class SportsDays
{
    /// <summary>The sport a stadium holds on a day: Black and White's table.</summary>
    public static Sport On(Stadium stadium, DayOfWeek day) => stadium switch
    {
        Stadium.BigStadium => day switch
        {
            DayOfWeek.Monday or DayOfWeek.Thursday or DayOfWeek.Sunday => Sport.Baseball,
            DayOfWeek.Tuesday or DayOfWeek.Friday => Sport.Football,
            _ => Sport.Gridiron,
        },
        _ => day is DayOfWeek.Monday or DayOfWeek.Wednesday or DayOfWeek.Friday or DayOfWeek.Sunday ? Sport.Tennis : Sport.Basketball,
    };

    /// <summary>The stadium a sport is played in.</summary>
    public static Stadium StadiumOf(Sport sport) => sport is Sport.Tennis or Sport.Basketball ? Stadium.SmallCourt : Stadium.BigStadium;

    /// <summary>The map of a sport's field.</summary>
    public static string FieldOf(Sport sport) => sport switch
    {
        Sport.Gridiron => "NimbasaCity_GridironField",
        Sport.Baseball => "NimbasaCity_BaseballField",
        Sport.Football => "NimbasaCity_FootballField",
        Sport.Tennis => "NimbasaCity_TennisCourt",
        _ => "NimbasaCity_BasketballCourt",
    };

    /// <summary>The map of a stadium's lobby, and of its stands.</summary>
    public static string LobbyOf(Stadium stadium) => stadium == Stadium.BigStadium ? "NimbasaCity_BigStadium" : "NimbasaCity_SmallCourt";
    public static string StandsOf(Stadium stadium) => stadium == Stadium.BigStadium ? "NimbasaCity_BigStadiumStands" : "NimbasaCity_SmallCourtStands";

    /// <summary>The class of a sport's athletes in Unova's trainers.</summary>
    public static string ClassOf(Sport sport) => sport switch
    {
        Sport.Gridiron => "Linebacker",
        Sport.Baseball => "Infielder",
        Sport.Football => "Striker",
        Sport.Tennis => "Smasher",
        _ => "Hoopster",
    };

    /// <summary>
    /// A sport's three athletes, as each field's objects stand them (their trainers' scripts name one of each
    /// athlete's four teams; the athlete is the name), in the order of their objects' flags (Black 2's 0x286 to 0x288).
    /// </summary>
    public static IReadOnlyList<string> AthletesOf(Sport sport) => sport switch
    {
        Sport.Gridiron => new[] { "dan", "bob", "jonah" },
        Sport.Baseball => new[] { "alex", "connor", "todd" },
        Sport.Football => new[] { "tony", "roberto", "marco" },
        Sport.Tennis => new[] { "elena", "aspen", "mari" },
        _ => new[] { "bobby", "john", "lamarcus" },
    };

    /// <summary>
    /// Which of an athlete's four teams the story calls for (ours, since Black 2's rule lies in scripts no source
    /// decompiles): the first under five of Unova's Badges, the second from five, the third with all eight, the fourth
    /// once Unova's Hall of Fame is entered. Their levels climb about 25, 33, 47 and 62.
    /// </summary>
    public static int TierOf(int unovaBadges, bool hallOfFame) => hallOfFame ? 4 : unovaBadges >= 8 ? 3 : unovaBadges >= 5 ? 2 : 1;

    /// <summary>
    /// An athlete's team at a tier: the trainers of Unova's table named for the athlete, ordered by their first
    /// Pokémon's level (the import numbers same-named trainers in the ROM's order, not by strength).
    /// </summary>
    public static string TeamOf(Sport sport, string athlete, int tier)
    {
        var ids = TeamsOf(sport, athlete);
        return ids[Math.Clamp(tier, 1, ids.Count) - 1];
    }

    /// <summary>An athlete's teams from the weakest to the strongest.</summary>
    public static IReadOnlyList<string> TeamsOf(Sport sport, string athlete)
    {
        string stem = $"unova_{ClassOf(sport).ToLowerInvariant()}_{athlete}";
        var ids = new[] { stem, stem + "_2", stem + "_3", stem + "_4" }
            .Select(id => (id, trainer: TrainerDatabase.Get(id, RegionDatabase.Unova)))
            .Where(t => t.trainer != null)
            .OrderBy(t => t.trainer!.Party.Count == 0 ? 0 : t.trainer.Party.Max(p => p.Level))
            .Select(t => t.id).ToList();
        return ids;
    }

    /// <summary>The daily flag of an athlete beaten today (cleared with the day's others by <see cref="DailyEvents"/>).</summary>
    public static string BeatenTodayFlag(Sport sport, int athlete) => $"FLAG_DAILY_UNOVA_{sport.ToString().ToUpperInvariant()}_{athlete + 1}";

    /// <summary>The story's flag of a game on: the stands' crowd and the lobbies' policemen are shown by it, and the fields are shut.</summary>
    public const string GameOnFlag = "FLAG_UNOVA_GAME_ON";

    /// <summary>Whether a game is on at a time of day: ours, the twilight hours (17:00 to 19:59), under the lights.</summary>
    public static bool GameOn(TimeOfDay time) => time == TimeOfDay.Twilight;
}
