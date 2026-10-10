using System;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumEngine.Models;

/// <summary>
/// What the game does as the days and the minutes go by (plan 06 · R14a; <c>src/unk_020559DC.c</c>): the original
/// reads the real-time clock against the save's game clock, and for each new day runs its daily events
/// (<c>FieldSystem_HandleDailyEvents</c>) before it hands the minutes since the last reading to what counts them
/// (<c>sub_02055B64</c>). No drawing or input: the engine reads the clock (<c>GameEngine.KeepTheClock</c>, by
/// <see cref="Core.GameClock"/>) and hands the state over.
/// </summary>
public static class DailyEvents
{
    /// <summary>The days the Solaceon News Press's deadline has left (<c>VAR_NEWS_PRESS_DEADLINE</c>), which each day counts down.</summary>
    public const string NewsPressDeadlineVar = "VAR_NEWS_PRESS_DEADLINE";

    /// <summary>The level of the day (<c>VAR_DAILY_RANDOM_LEVEL</c>, 2 to 99): what the man in the house on Route 221 asks to see.</summary>
    public const string DailyLevelVar = "VAR_DAILY_RANDOM_LEVEL";

    /// <summary>
    /// The Star Pieces hidden on Iron Island that come back from day to day (<c>sIronIslandHiddenItemFlags</c>): the
    /// area each lies in and the flag that says it was found.
    /// </summary>
    public static readonly (string Area, string Flag)[] IronIslandStarPieces =
    {
        ("iron_island_b1f_right_room", "FLAG_OBTAINED_HIDDEN_IRON_ISLAND_B1F_RIGHT_ROOM_STAR_PIECE"),
        ("iron_island_b2f_right_room", "FLAG_OBTAINED_HIDDEN_IRON_ISLAND_B2F_RIGHT_ROOM_STAR_PIECE"),
        ("iron_island_b2f_left_room", "FLAG_OBTAINED_HIDDEN_IRON_ISLAND_B2F_LEFT_ROOM_STAR_PIECE_1"),
        ("iron_island_b2f_left_room", "FLAG_OBTAINED_HIDDEN_IRON_ISLAND_B2F_LEFT_ROOM_STAR_PIECE_2")
    };

    /// <summary>The Honey hidden in Floaroma Meadow that comes back (<c>sFloaromaMeadowHiddenItemFlags</c>).</summary>
    public const string FloaromaMeadow = "floaroma_meadow";
    public static readonly string[] FloaromaMeadowHoney =
    {
        "FLAG_OBTAINED_HIDDEN_FLOAROMA_MEADOW_HONEY_1", "FLAG_OBTAINED_HIDDEN_FLOAROMA_MEADOW_HONEY_2",
        "FLAG_OBTAINED_HIDDEN_FLOAROMA_MEADOW_HONEY_3", "FLAG_OBTAINED_HIDDEN_FLOAROMA_MEADOW_HONEY_4",
        "FLAG_OBTAINED_HIDDEN_FLOAROMA_MEADOW_HONEY_5", "FLAG_OBTAINED_HIDDEN_FLOAROMA_MEADOW_HONEY_6"
    };

    /// <summary>
    /// What the day's events change: the story's flags and variables, the wild Pokémon's numbers, the team (for
    /// Pokérus), where the player is (an area's key: what is hidden there doesn't come back under their eyes), and
    /// the chance the day's draws come from.
    /// </summary>
    public sealed record Day(StoryState Story, SpecialEncounters Encounters, Party Party, string? Place, Random Chance);

    /// <summary>
    /// What a new game's script sets (<c>scripts_init_new_game.s</c>: <c>RandomizeJubilifeLottery</c>,
    /// <c>InitDailyRandomLevel</c>): the lottery's first number and the first level of the day, both drawn.
    /// </summary>
    public static void NewGame(StoryState story, Random chance)
    {
        story.SetVar(Lottery.NumberVar, Lottery.Drawn(chance));
        story.SetVar(DailyLevelVar, LevelOfTheDay(chance));
    }

    /// <summary>A level of the day (<c>SystemVars_InitDailyRandomLevel</c>): 2 to 99.</summary>
    public static int LevelOfTheDay(Random chance) => chance.Next(98) + 2;

    /// <summary>
    /// Days have passed since the clock was last read (<c>FieldSystem_HandleDailyEvents</c>), in the original's
    /// order: the daily flags cleared, the record-mixing group's number moved on once a day and the Great Marsh's and
    /// the swarm's numbers set from it, a day off Pokérus for each, the News Press's deadline counted down, the
    /// lottery's number of the day, a new level of the day, and two of Iron Island's Star Pieces and two of Floaroma
    /// Meadow's Honey come back. What the original does besides belongs to places this game hasn't yet: the
    /// Underground (plan 06 · R16a), the Trainer Case's badges' dust (the card has no badges to polish yet), the
    /// Villa's visitor (R16b), and the Battle Tower's, the Geonet's and the TV's daily numbers.
    /// </summary>
    public static void DaysPass(Day day, int days)
    {
        if (days <= 0) return;
        var story = day.Story;
        story.ClearDaily();
        day.Encounters.DaysPass(days);
        PokerusRules.DaysPass(day.Party, days);
        story.SetVar(NewsPressDeadlineVar, Math.Max(0, story.Var(NewsPressDeadlineVar) - days));
        story.SetVar(Lottery.NumberVar, Lottery.NumberOf(day.Encounters.DailyNumber));
        story.SetVar(DailyLevelVar, LevelOfTheDay(day.Chance));
        HiddenItemsReturn(day);
    }

    /// <summary>
    /// <c>FieldSystem_ClearDailyHiddenItemFlags</c>: two draws among Iron Island's four Star Pieces, each brought back
    /// unless the player is in its room, then, unless the player is in Floaroma Meadow, two draws among its six
    /// Honeys. A draw can fall twice on the same one, so as few as one comes back.
    /// </summary>
    public static void HiddenItemsReturn(Day day)
    {
        for (int i = 0; i < 2; i++)
        {
            var (area, flag) = IronIslandStarPieces[day.Chance.Next(IronIslandStarPieces.Length)];
            if (day.Place != area) day.Story.Unset(flag);
        }
        if (day.Place == FloaromaMeadow) return;
        for (int i = 0; i < 2; i++) day.Story.Unset(FloaromaMeadowHoney[day.Chance.Next(FloaromaMeadowHoney.Length)]);
    }

    /// <summary>
    /// The minutes the clock has moved on (<c>sub_02055B64</c>), in the original's order: the berry patches grow and
    /// dry, then the honey trees' honey runs down. (The Underground's gift penalty and the TV's programmes wait for
    /// their places; Shaymin's night is the engine's, by the hour.)
    /// </summary>
    public static void MinutesPass(BerryPatches berries, SpecialEncounters encounters, int minutes)
    {
        if (minutes <= 0) return;
        berries.MinutesPass(minutes);
        Overworld.HoneyTrees.MinutesPass(encounters, minutes);
    }
}
