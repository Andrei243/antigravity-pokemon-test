using System;
using System.Collections.Generic;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Story;

/// <summary>
/// What is true of the game just now that a television may speak of (plan 08 · P8): the host gathers it
/// (<see cref="IScriptHost.Broadcast"/>) and <see cref="Broadcasts"/> makes a programme of it. Plain values, so a test
/// writes one by hand.
/// </summary>
public sealed record BroadcastFacts
{
    /// <summary>The region the player is in, by its id: the Lottery Corner is Jubilife TV's, and only Sinnoh's televisions tell its number.</summary>
    public string Region { get; init; } = RegionDatabase.Sinnoh;
    public string PlayerName { get; init; } = "";

    /// <summary>Whether the player has a Pokédex (<see cref="StoryState.PokedexFlag"/>), and its counts.</summary>
    public bool HasPokedex { get; init; }
    public int Seen { get; init; }
    public int Caught { get; init; }

    /// <summary>The Badges won in the region the player is in, and how many it has.</summary>
    public int Badges { get; init; }
    public int BadgesInRegion { get; init; }

    /// <summary>How long the game has been played, in seconds.</summary>
    public float PlayTimeSeconds { get; init; }

    /// <summary>The clock as the game reads it: the hour and the minute, the time of day and the day of the week.</summary>
    public int Hour { get; init; } = 10;
    public int Minute { get; init; }
    public TimeOfDay TimeOfDay { get; init; } = TimeOfDay.Morning;
    public DayOfWeek Weekday { get; init; } = DayOfWeek.Monday;

    /// <summary>The day's number the Lottery Corner's ticket is read from (<see cref="Models.Lottery.TicketOf"/>).</summary>
    public uint DailyNumber { get; init; }

    /// <summary>How many times a team has entered the Hall of Fame.</summary>
    public int LeagueVictories { get; init; }

    /// <summary>The team: how many there are, and the first that is no Egg, by the name shown, with its level (null for none).</summary>
    public int TeamSize { get; init; }
    public string? Lead { get; init; }
    public int LeadLevel { get; init; }
}

/// <summary>The programmes a television shows when it has nothing of its own (plan 08 · P8; plan 08's decision 5).</summary>
public enum Programme
{
    /// <summary>The time check: the hour, the day and what the time of day brings. Always on.</summary>
    Clock,
    /// <summary>The Pokédex Report: the player's Pokédex, seen and caught. Once the player has one.</summary>
    Pokedex,
    /// <summary>The trainer survey: the player's team, Badges and time on the road. Once the player has a Pokémon.</summary>
    Survey,
    /// <summary>The Lottery Corner: the day's number, which the counter at Jubilife TV draws against. In Sinnoh.</summary>
    Lottery,
    /// <summary>News of the Hall of Fame, once a team has entered it.</summary>
    HallOfFame,
}

/// <summary>
/// The programmes of a television that has no line of its own (plan 08 · P8; the original's TV programmes, in our own
/// words and of our own making): each is lines composed from what is true of the game (<see cref="BroadcastFacts"/>),
/// never anything made up, and which is on is drawn on the game's own die. The script's <c>broadcast</c> says them
/// (<c>common.Television</c>). The interviews (plan 08 · P9) join the programmes once the reporters ask their
/// questions. GPU-free.
/// </summary>
public static class Broadcasts
{
    /// <summary>
    /// The facts a script host already knows (<see cref="IScriptHost.Broadcast"/>'s default): the team, the Badges,
    /// the Pokédex, the day. The clock's hour and the play time are left as they are made, for a host that knows them.
    /// </summary>
    public static BroadcastFacts FactsOf(IScriptHost host)
    {
        Models.Pokemon? lead = null;
        foreach (var p in host.Party.Members)
            if (!p.IsEgg) { lead = p; break; }
        return new BroadcastFacts
        {
            Region = host.Region,
            PlayerName = host.PlayerName,
            HasPokedex = host.Story.Has(StoryState.PokedexFlag),
            Seen = host.SeenInSinnoh,
            Caught = host.Caught,
            Badges = host.Story.BadgeCount(host.Region),
            BadgesInRegion = RegionDatabase.Get(host.Region)?.BadgeNames.Count ?? 0,
            TimeOfDay = host.TimeOfDay,
            Weekday = host.Weekday,
            DailyNumber = host.Encounters.DailyNumber,
            LeagueVictories = host.LeagueVictories,
            TeamSize = host.Party.Count,
            Lead = lead?.DisplayName,
            LeadLevel = lead?.Level ?? 0
        };
    }

    /// <summary>What is on for these facts, in a fixed order: the die picks among them.</summary>
    public static IReadOnlyList<Programme> On(BroadcastFacts facts)
    {
        var on = new List<Programme> { Programme.Clock };
        if (facts.HasPokedex) on.Add(Programme.Pokedex);
        if (facts.Lead != null) on.Add(Programme.Survey);
        if (facts.Region == RegionDatabase.Sinnoh) on.Add(Programme.Lottery);
        if (facts.LeagueVictories > 0) on.Add(Programme.HallOfFame);
        return on;
    }

    /// <summary>The programme a die of as many sides as there are on (0 to one less) picks.</summary>
    public static Programme Choose(BroadcastFacts facts, Func<int, int> die)
    {
        var on = On(facts);
        return on[Math.Clamp(die(on.Count), 0, on.Count - 1)];
    }

    /// <summary>A programme drawn and its lines: what <c>broadcast</c> says.</summary>
    public static IReadOnlyList<string> Compose(BroadcastFacts facts, Func<int, int> die) => Lines(Choose(facts, die), facts);

    /// <summary>A programme's lines for these facts, in the language chosen.</summary>
    public static IReadOnlyList<string> Lines(Programme programme, BroadcastFacts facts) => programme switch
    {
        Programme.Pokedex => Pokedex(facts),
        Programme.Survey => Survey(facts),
        Programme.Lottery => Lottery(facts),
        Programme.HallOfFame => HallOfFame(facts),
        _ => Clock(facts),
    };

    private static IReadOnlyList<string> Clock(BroadcastFacts f)
    {
        string time = $"{f.Hour}:{f.Minute:D2}";
        string remark = f.TimeOfDay switch
        {
            TimeOfDay.Morning => Text.Of("broadcast.clock.morning"),
            TimeOfDay.Day => Text.Of("broadcast.clock.day"),
            TimeOfDay.Twilight => Text.Of("broadcast.clock.twilight"),
            TimeOfDay.Night => Text.Of("broadcast.clock.night"),
            _ => Text.Of("broadcast.clock.lateNight"),
        };
        return new[]
        {
            Text.Of("broadcast.clock.open", ("time", time), ("day", Words.Weekday(f.Weekday))),
            remark,
            Text.Of("broadcast.clock.close")
        };
    }

    private static IReadOnlyList<string> Pokedex(BroadcastFacts f)
    {
        string remark = f.Caught switch
        {
            0 => Text.Of("broadcast.pokedex.none"),
            < 30 => Text.Of("broadcast.pokedex.few"),
            < 100 => Text.Of("broadcast.pokedex.many"),
            _ => Text.Of("broadcast.pokedex.great"),
        };
        return new[]
        {
            Text.Of("broadcast.pokedex.open", ("player", f.PlayerName)),
            Text.Of("broadcast.pokedex.counts", ("player", f.PlayerName), ("seen", f.Seen), ("n", f.Caught)),
            remark,
            Text.Of("broadcast.pokedex.close")
        };
    }

    private static IReadOnlyList<string> Survey(BroadcastFacts f)
    {
        string team = f.TeamSize <= 1
            ? Text.Of("broadcast.survey.alone", ("player", f.PlayerName), ("lead", f.Lead), ("level", f.LeadLevel))
            : Text.Of("broadcast.survey.team", ("player", f.PlayerName), ("n", f.TeamSize), ("lead", f.Lead), ("level", f.LeadLevel));
        string badges = f.Badges == 0 ? Text.Of("broadcast.survey.noBadges")
            : f.BadgesInRegion > 0 && f.Badges >= f.BadgesInRegion ? Text.Of("broadcast.survey.allBadges")
            : Text.Of("broadcast.survey.badges", ("player", f.PlayerName), ("n", f.Badges));
        int minutes = (int)(f.PlayTimeSeconds / 60f);
        string played = $"{minutes / 60}:{minutes % 60:D2}";
        return new[]
        {
            Text.Of("broadcast.survey.open", ("player", f.PlayerName)),
            team,
            badges,
            Text.Of("broadcast.survey.time", ("time", played)),
            Text.Of("broadcast.survey.close", ("player", f.PlayerName))
        };
    }

    private static IReadOnlyList<string> Lottery(BroadcastFacts f) => new[]
    {
        Text.Of("broadcast.lottery.open", ("ticket", Models.Lottery.TicketOf(f.DailyNumber).ToString("D5"))),
        Text.Of("broadcast.lottery.how"),
        Text.Of("broadcast.lottery.close")
    };

    private static IReadOnlyList<string> HallOfFame(BroadcastFacts f) => new[]
    {
        Text.Of("broadcast.fame.open", ("player", f.PlayerName)),
        Text.Of("broadcast.fame.entries", ("player", f.PlayerName), ("n", f.LeagueVictories)),
        Text.Of("broadcast.fame.close")
    };
}
