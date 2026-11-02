using System;
using System.Collections.Generic;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models;

/// <summary>
/// Buena's Password (plan 20, Johto's leftovers; HeartGold's Buena's Password show and its Blue Card): a word of the
/// day that the radio's show says in the evening and Buena, on the Radio Tower's second floor, asks for. Each night
/// she offers three words, the right one among them; a right answer is a point on the Blue Card (at most
/// <see cref="MostPoints"/>), once a night. The word and the three are decided by the date alone (a hash of the day's
/// number, never <see cref="Dice"/>), so the radio and Buena agree, and every run of a day draws the same. The words
/// are names of the data (species and moves), shown in the language chosen. HeartGold's own word lists couldn't be
/// read (no cache of its scripts here): ours are Johto's Pokémon and well-known moves. No drawing or input.
/// </summary>
public static class BuenasPassword
{
    /// <summary>A password answered right tonight (a daily flag of ours, cleared as the day changes).</summary>
    public const string AnsweredFlag = "FLAG_DAILY_JOHTO_BUENAS_PASSWORD";

    /// <summary>The Blue Card's points (a variable of ours).</summary>
    public const string PointsVar = "VAR_JOHTO_BLUE_CARD_POINTS";

    /// <summary>The most the Blue Card holds, as HeartGold's.</summary>
    public const int MostPoints = 30;

    /// <summary>The words, each a species (false) or a move (true) by its English name in the data.</summary>
    private static readonly (bool Move, string Name)[] Words =
    {
        (false, "Chikorita"), (false, "Cyndaquil"), (false, "Totodile"), (false, "Togepi"), (false, "Pichu"),
        (false, "Marill"), (false, "Sudowoodo"), (false, "Wooper"), (false, "Espeon"), (false, "Umbreon"),
        (false, "Murkrow"), (false, "Slowking"), (false, "Misdreavus"), (false, "Girafarig"), (false, "Dunsparce"),
        (false, "Steelix"), (false, "Snubbull"), (false, "Scizor"), (false, "Heracross"), (false, "Sneasel"),
        (false, "Teddiursa"), (false, "Slugma"), (false, "Swinub"), (false, "Corsola"), (false, "Delibird"),
        (false, "Skarmory"), (false, "Houndour"), (false, "Kingdra"), (false, "Phanpy"), (false, "Smeargle"),
        (false, "Miltank"), (false, "Larvitar"), (true, "Thunderbolt"), (true, "Surf"), (true, "Fire Blast"),
        (true, "Hyper Beam"), (true, "Psychic"), (true, "Earthquake"), (true, "Rollout"), (true, "Attract"),
        (true, "Fly"), (true, "Whirlpool"), (true, "Headbutt"), (true, "Ice Beam"), (true, "Sacred Fire"),
        (true, "Aeroblast"), (true, "Dragon Rage"), (true, "Sweet Scent"),
    };

    /// <summary>How many words there are.</summary>
    public static int Count => Words.Length;

    /// <summary>The day's number, counted from a fixed day.</summary>
    public static int DayNumber(DateTime day) => (int)(day.Date - new DateTime(2000, 1, 1)).TotalDays;

    // (a small hash of a number, so neighbouring days give unrelated words)
    private static int Mix(int n)
    {
        unchecked
        {
            uint x = (uint)n * 2654435761u;
            x ^= x >> 15;
            x *= 2246822519u;
            x ^= x >> 13;
            return (int)(x & 0x7FFFFFFF);
        }
    }

    /// <summary>Tonight's word, by its place in the list.</summary>
    public static int WordOf(DateTime day) => Mix(DayNumber(day)) % Words.Length;

    /// <summary>Where among Buena's three the right word stands tonight, 0 to 2.</summary>
    public static int RightPlace(DateTime day) => Mix(DayNumber(day) * 7 + 3) % 3;

    /// <summary>Buena's three words tonight, by their places in the list: the right one at <see cref="RightPlace"/>, the two others different from it and from each other.</summary>
    public static IReadOnlyList<int> Choices(DateTime day)
    {
        int right = WordOf(day), place = RightPlace(day);
        int a = (right + 1 + Mix(DayNumber(day) * 11 + 5) % (Words.Length - 1)) % Words.Length;
        int b = a;
        for (int k = 0; b == a || b == right; k++) b = (right + 1 + Mix(DayNumber(day) * 13 + 7 + k) % (Words.Length - 1)) % Words.Length;
        var others = new[] { a, b };
        var three = new int[3];
        for (int i = 0, o = 0; i < 3; i++) three[i] = i == place ? right : others[o++];
        return three;
    }

    /// <summary>A word as it is shown, in the language chosen.</summary>
    public static string Shown(int index)
    {
        var (move, name) = Words[((index % Words.Length) + Words.Length) % Words.Length];
        return move ? LocalNames.Move(name) : LocalNames.Species(name);
    }

    /// <summary>The English name of a word (what tests and the data checks read).</summary>
    public static string NameOf(int index) => Words[index].Name;

    /// <summary>Whether a word is a move (else a species).</summary>
    public static bool IsMove(int index) => Words[index].Move;

    /// <summary>Whether the show is on the air (and Buena takes the word): the evening, twilight and night.</summary>
    public static bool OnTheAir(TimeOfDay time) => time is TimeOfDay.Twilight or TimeOfDay.Night;
}
