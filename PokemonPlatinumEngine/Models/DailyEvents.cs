using System;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumEngine.Models;

/// <summary>
/// What a new day brings (plan 06 · R14a), the original's <c>FieldSystem_HandleDailyEvents</c> (<c>src/unk_020559DC.c</c>)
/// in its order, for the parts this game has: the daily flags cleared, the day's number moved on and the marsh's and
/// the swarm's numbers with it (the lottery's ticket is read from it, <see cref="Lottery.TicketOf"/>), the day's random
/// level drawn, and four of the hidden items that come back. Pokérus's days are the engine's (<c>KeepTheClock</c>). Not
/// here: the Underground's day, the badges' dirt (the Trainer Card has no polishing yet), the news press's deadline,
/// the Villa's visitor, the TV's programmes and the Geonet. No drawing or input.
/// </summary>
public static class DailyEvents
{
    /// <summary>The day's random level (<c>VAR_DAILY_RANDOM_LEVEL</c>): from 2 to 99, drawn each new day.</summary>
    public const string RandomLevelVariable = "VAR_DAILY_RANDOM_LEVEL";

    /// <summary>
    /// The star pieces hidden in Iron Island's rooms that come back (<c>sIronIslandHiddenItemFlags</c>): the room's
    /// area and the flag that says the piece was found.
    /// </summary>
    public static readonly (string Area, string Flag)[] IronIslandPieces =
    {
        ("iron_island_b1f_right_room", "FLAG_OBTAINED_HIDDEN_IRON_ISLAND_B1F_RIGHT_ROOM_STAR_PIECE"),
        ("iron_island_b2f_right_room", "FLAG_OBTAINED_HIDDEN_IRON_ISLAND_B2F_RIGHT_ROOM_STAR_PIECE"),
        ("iron_island_b2f_left_room", "FLAG_OBTAINED_HIDDEN_IRON_ISLAND_B2F_LEFT_ROOM_STAR_PIECE_1"),
        ("iron_island_b2f_left_room", "FLAG_OBTAINED_HIDDEN_IRON_ISLAND_B2F_LEFT_ROOM_STAR_PIECE_2")
    };

    /// <summary>The meadow's six hidden Honeys (<c>sFloaromaMeadowHiddenItemFlags</c>).</summary>
    public const string Meadow = "floaroma_meadow";
    public static readonly string[] MeadowHoney =
    {
        "FLAG_OBTAINED_HIDDEN_FLOAROMA_MEADOW_HONEY_1", "FLAG_OBTAINED_HIDDEN_FLOAROMA_MEADOW_HONEY_2",
        "FLAG_OBTAINED_HIDDEN_FLOAROMA_MEADOW_HONEY_3", "FLAG_OBTAINED_HIDDEN_FLOAROMA_MEADOW_HONEY_4",
        "FLAG_OBTAINED_HIDDEN_FLOAROMA_MEADOW_HONEY_5", "FLAG_OBTAINED_HIDDEN_FLOAROMA_MEADOW_HONEY_6"
    };

    /// <summary>
    /// <paramref name="days"/> new days have come while the player was in <paramref name="place"/> (an area's key, or a
    /// hand-made map's name). However many passed, the day's events happen once, as in the original, but the day's
    /// number moves on once for each.
    /// </summary>
    public static void DaysPass(int days, StoryState story, SpecialEncounters encounters, string? place, Random chance)
    {
        if (days <= 0) return;
        story.ClearDaily();
        encounters.DaysPass(days);
        // SystemVars_InitDailyRandomLevel: LCRNG_Next() % 98 + 2
        story.SetVar(RandomLevelVariable, chance.Next(98) + 2);
        ReturnHiddenItems(story, place, chance);
    }

    /// <summary>
    /// <c>FieldSystem_ClearDailyHiddenItemFlags</c>: two draws among Iron Island's four star pieces and two among the
    /// meadow's six Honeys, each found again unless the player stands in its place (the same piece may be drawn twice).
    /// </summary>
    public static void ReturnHiddenItems(StoryState story, string? place, Random chance)
    {
        for (int i = 0; i < 2; i++)
        {
            var (area, flag) = IronIslandPieces[chance.Next(IronIslandPieces.Length)];
            if (place != area) story.Unset(flag);
        }
        if (place == Meadow) return;
        for (int i = 0; i < 2; i++) story.Unset(MeadowHoney[chance.Next(MeadowHoney.Length)]);
    }
}
