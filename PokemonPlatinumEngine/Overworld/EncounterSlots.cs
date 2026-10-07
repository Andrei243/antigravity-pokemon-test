using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Overworld;

/// <summary>
/// What the moment brings to a place's wild Pokémon (plan 06 · R13): the time of day, the day itself, the day's
/// swarm, the Trophy Garden's and the Great Marsh's dailies, and what the field holds besides the tables (the Poké
/// Radar's chain, the roaming Pokémon). The game makes one for every step (<c>GameEngine.EncounterMomentNow</c>);
/// left out, a place's table is its own and nothing else is met.
/// </summary>
public sealed record EncounterMoment
{
    /// <summary>
    /// The time of day: the morning keeps the table's own slots 2 and 3, the day and the evening put the day's two
    /// in, the night and the late night the night's (<c>WildEncounters_ReplaceTimedEncounters</c>).
    /// </summary>
    public TimeOfDay Time { get; init; } = TimeOfDay.Morning;

    /// <summary>Today, whose date may change the odds of a step meeting anything (<see cref="SpecialDates"/>); null leaves them as they are.</summary>
    public DateTime? Today { get; init; }

    /// <summary>The area whose swarm is out today; null while swarms haven't begun.</summary>
    public string? SwarmArea { get; init; }

    /// <summary>Whether the player has the National Pokédex: the Trophy Garden's two come only with it, and the Great Marsh draws from its other list.</summary>
    public bool NationalDex { get; init; }

    /// <summary>The Trophy Garden's two, by species; null for a slot nobody has brought yet.</summary>
    public string? TrophyFirst { get; init; }
    public string? TrophySecond { get; init; }

    /// <summary>During a Safari Game: the day's number the Great Marsh's areas draw their species from (<c>marshDaily</c>).</summary>
    public uint? MarshDaily { get; init; }

    /// <summary>The Great Marsh's two lists of daily Pokémon.</summary>
    public GreatMarshTables? Marsh { get; init; }

    /// <summary>Someone travels with the player: no roamer is met, and the Poké Radar's patches don't call.</summary>
    public bool Partner { get; init; }

    /// <summary>The Poké Radar's chain while one is under way (it changes as patches are walked into); null without one.</summary>
    public RadarChain? Radar { get; init; }

    /// <summary>The height the player stands at: the Poké Radar's patches shake only at it.</summary>
    public float Height { get; init; }

    /// <summary>What the game remembers of its wild Pokémon: the roamers, the day's number Feebas's tiles go by; null for none.</summary>
    public SpecialEncounters? State { get; init; }
}

/// <summary>
/// A place's table of wild Pokémon at a moment (plan 06 · R13), the original's replacements in the grass's twelve
/// slots, each keeping its level and its weight: the time of day's two in slots 2 and 3, a swarm's two in 0 and 1,
/// the Trophy Garden's two in 6 and 7, the Great Marsh's daily Pokémon in 6 and 7, in that order
/// (<c>WildEncounters_TryWildEncounter</c>). The species a second game in the console calls up (8 and 9) are
/// plan 08's. No drawing or input.
/// </summary>
public static class EncounterSlots
{
    /// <summary>The Trophy Garden's key (<c>MapHeader_IsTrophyGarden</c>).</summary>
    public const string TrophyGarden = "trophy_garden";

    /// <summary>The grass's table at a moment: a copy of the area's own with the moment's species in their slots.</summary>
    public static List<WildEncounterEntry> Grass(MapArea area, EncounterMoment? moment)
    {
        var slots = area.WildEncounters.Select(Copy).ToList();
        if (moment == null || slots.Count != 12) return slots;
        void Put(int slot, string? species)
        {
            if (!string.IsNullOrEmpty(species)) slots[slot].SpeciesName = species;
        }

        var timed = moment.Time switch
        {
            TimeOfDay.Day or TimeOfDay.Twilight => area.DaySlots,
            TimeOfDay.Night or TimeOfDay.LateNight => area.NightSlots,
            _ => null
        };
        if (timed is { Count: 2 })
        {
            Put(2, timed[0]);
            Put(3, timed[1]);
        }
        // WildEncounters_ReplaceSwarmEncounters
        if (moment.SwarmArea == area.Key && area.SwarmSlots.Count == 2)
        {
            Put(0, area.SwarmSlots[0]);
            Put(1, area.SwarmSlots[1]);
        }
        // WildEncounters_ReplaceTrophyGardenEncounters
        if (area.Key == TrophyGarden && moment.NationalDex)
        {
            Put(6, moment.TrophyFirst);
            Put(7, moment.TrophySecond);
        }
        // WildEncounters_ReplaceGreatMarshDailyEncounters, during a Safari Game
        if (moment.MarshDaily is { } daily && moment.Marsh is { } marsh && GreatMarsh.DailySpecies(marsh, daily, area.Key, moment.NationalDex) is { } today)
        {
            Put(6, today);
            Put(7, today);
        }
        return slots;
    }

    /// <summary>The Poké Radar's hard shake puts its four in slots 4, 5, 10 and 11 (<c>TryGenerateGrassEncounter_WithRadar</c>).</summary>
    public static void PutRadarSpecies(List<WildEncounterEntry> slots, MapArea area)
    {
        if (slots.Count != 12 || area.RadarSlots.Count != 4) return;
        slots[4].SpeciesName = area.RadarSlots[0];
        slots[5].SpeciesName = area.RadarSlots[1];
        slots[10].SpeciesName = area.RadarSlots[2];
        slots[11].SpeciesName = area.RadarSlots[3];
    }

    private static WildEncounterEntry Copy(WildEncounterEntry e) => new()
    {
        SpeciesName = e.SpeciesName,
        MinLevel = e.MinLevel,
        MaxLevel = e.MaxLevel,
        Weight = e.Weight
    };
}

/// <summary>
/// Swarms (plan 06 · R13; <c>src/overlay006/swarm.c</c>): once they have begun, one place of 22 has a swarm each day,
/// the day's number taken modulo 22 (<c>Swarm_GetMapId</c>), and there its grass's slots 0 and 1 are the table's two
/// swarming species.
/// </summary>
public static class Swarms
{
    /// <summary>The places swarms come to, in the original's order (<c>sSwarmMapIdTable</c>).</summary>
    public static readonly string[] Areas =
    {
        "route_201", "route_202", "route_203", "route_206", "route_207", "route_208", "route_209", "route_214",
        "route_215", "route_217", "route_218", "route_221", "route_222", "route_224", "route_225", "route_226",
        "route_227", "route_228", "route_229", "route_230", "valley_windworks_outside", "eterna_forest"
    };

    /// <summary>The place of the day's swarm (<c>Swarm_GetMapId</c>).</summary>
    public static string AreaOf(uint swarmDaily) => Areas[swarmDaily % (uint)Areas.Length];

    /// <summary>Where the swarm is today, or null while swarms haven't begun.</summary>
    public static string? Today(SpecialEncounters state) => state.SwarmsOn ? AreaOf(state.SwarmDaily) : null;

    /// <summary>The species that swarms at a place, as it is told (<c>Swarm_GetMapIdAndSpecies</c>); empty for a place swarms never come to.</summary>
    public static string Species(string area) => SpecialEncounterTables.Sinnoh.Swarms.FirstOrDefault(s => s.Area == area)?.Species ?? "";

    /// <summary>The name of a place swarms come to.</summary>
    public static string PlaceName(string area) => SpecialEncounterTables.Sinnoh.Swarms.FirstOrDefault(s => s.Area == area)?.Name ?? "";
}

/// <summary>
/// The Great Marsh's daily Pokémon (plan 06 · R13; <c>great_marsh_daily_encounters.c</c>): during a Safari Game,
/// each of its six areas puts one species of a list of 32 in its grass's slots 6 and 7, the one at five bits of the
/// day's number, the area's own (<c>(marshDaily &gt;&gt; 5 × area) &amp; 31</c>). The list is the National
/// Pokédex's once the player has it.
/// </summary>
public static class GreatMarsh
{
    /// <summary>The marsh's areas, numbered 0 to 5 as the original numbers them (<c>GreatMarsh_GetAreaNumFromMapId</c>).</summary>
    public static readonly string[] Areas = { "great_marsh_1", "great_marsh_2", "great_marsh_3", "great_marsh_4", "great_marsh_5", "great_marsh_6" };

    /// <summary>The species the day puts in an area of the marsh; null for a place that isn't one of them.</summary>
    public static string? DailySpecies(GreatMarshTables tables, uint marshDaily, string area, bool nationalDex)
    {
        int number = Array.IndexOf(Areas, area);
        var list = nationalDex ? tables.National : tables.Local;
        if (number < 0 || list.Count < 32) return null;
        return list[(int)((marshDaily >> (5 * number)) & 0x1f)];
    }
}

/// <summary>
/// The Trophy Garden's daily Pokémon (plan 06 · R13; <c>trophy_garden_daily_encounters.c</c>): each day Mr. Backlot
/// can tell of one more of his sixteen (<c>TrophyGarden_AddNewMon</c>), drawn until it is neither of the two already
/// there; it takes the first slot and the first moves to the second. With the National Pokédex the two are met in
/// the garden's grass, slots 6 and 7.
/// </summary>
public static class TrophyGardenRules
{
    /// <summary>Brings one more of the list to the garden, as Mr. Backlot's script does once a day.</summary>
    public static void AddNew(SpecialEncounters state, IReadOnlyList<string> list, Random rng)
    {
        if (list.Count == 0) return;
        string? first = state.TrophyFirst is { } a && a < list.Count ? list[a] : null;
        string? second = state.TrophySecond is { } b && b < list.Count ? list[b] : null;
        while (true)
        {
            int index = rng.Next(list.Count);
            if (list[index] == first || list[index] == second) continue;
            state.TrophySecond = state.TrophyFirst;
            state.TrophyFirst = index;
            return;
        }
    }

    /// <summary>The species in a slot of the garden; null for none.</summary>
    public static string? SpeciesIn(int? slot, IReadOnlyList<string> list) => slot is { } i && i >= 0 && i < list.Count ? list[i] : null;
}

/// <summary>
/// The days of the year when a step is more or less likely to meet a wild Pokémon (plan 06 · R13;
/// <c>SpecialDates_ModifyEncounterRate</c>): the flat chance that an attempt gets through (40 in a hundred, 70 in very
/// tall grass or on a Bicycle) moves by five or ten on these days, and never below one.
/// </summary>
public static class SpecialDates
{
    // (month, day, change), the original's sSpecialDates and sEncounterRateModifiers
    private static readonly (int Month, int Day, int Change)[] Dates =
    {
        (1, 1, -10), (1, 11, 5), (1, 12, 10), (1, 29, 5),
        (2, 3, 5), (2, 11, 5), (2, 14, 0), (2, 27, 10),
        (3, 3, 5), (3, 18, 5), (3, 21, -10),
        (4, 1, 5), (4, 25, 5), (4, 26, -5), (4, 29, 5),
        (5, 1, 0), (5, 3, 5), (5, 4, 0), (5, 5, 5),
        (6, 21, 5),
        (7, 7, 10), (7, 18, 5), (7, 24, 5),
        (8, 13, -5), (8, 14, -5), (8, 15, 5), (8, 16, -5),
        (9, 7, 5), (9, 12, 5), (9, 15, 5), (9, 20, 5), (9, 23, -10), (9, 28, 5),
        (10, 5, 5), (10, 15, 5), (10, 30, 5),
        (11, 3, -5), (11, 12, 5), (11, 21, 5), (11, 23, 0),
        (12, 14, 5), (12, 23, 5), (12, 31, -5)
    };

    /// <summary>The flat chance on a day: the date's change, never below one; nothing changes a chance of nothing.</summary>
    public static int ModifyEncounterRate(int flat, DateTime day)
    {
        if (flat == 0) return 0;
        foreach (var (month, date, change) in Dates)
            if (month == day.Month && date == day.Day)
                return flat + change < 0 ? 1 : flat + change;
        return flat;
    }
}
