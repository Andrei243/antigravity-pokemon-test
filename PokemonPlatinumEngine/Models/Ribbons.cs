using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models;

/// <summary>What a ribbon is given for, which is also what its picture here takes its shape from.</summary>
public enum RibbonKind
{
    /// <summary>The Hall of Fame of Hoenn, and Sinnoh's.</summary>
    Champion,
    /// <summary>Hoenn's contests, twenty of them, and the painting's.</summary>
    HoennContest,
    /// <summary>Sinnoh's Super Contests, twenty of them.</summary>
    Contest,
    /// <summary>Hoenn's Battle Tower, and Sinnoh's six.</summary>
    Tower,
    /// <summary>For effort values filled, in Hoenn or at the Sunyshore Market.</summary>
    Effort,
    /// <summary>Julia's, one for each day of the week.</summary>
    Daily,
    /// <summary>The Ribbon Syndicate's three, bought.</summary>
    Syndicate,
    /// <summary>Dr. Footstep's.</summary>
    Footprint,
    /// <summary>The gifts of Hoenn's events and the ribbons only an event ever gave.</summary>
    Event
}

/// <summary>
/// Platinum's eighty ribbons (plan 06 · R17b): what each is for, who gives it and when, in this game or not yet. From
/// the original's <c>src/ribbon.c</c>, <c>scrcmd_party.c</c>, <c>Party_GiveChampionRibbons</c> and the scripts of the
/// Sunyshore Market, Julia's house, Dr. Footstep's house and the Ribbon Syndicate. A Pokémon's ribbons are
/// <see cref="Pokemon.Ribbons"/>. No drawing, no input.
/// </summary>
public static class RibbonRules
{
    public const string Effort = "RIBBON_HOENN_EFFORT", SinnohChamp = "RIBBON_SINNOH_CHAMP", Footprint = "RIBBON_FOOTPRINT",
        Gorgeous = "RIBBON_GORGEOUS", Royal = "RIBBON_ROYAL", GorgeousRoyal = "RIBBON_GORGEOUS_ROYAL";

    /// <summary>The effort values a Pokémon has when it can gain no more (<c>MAX_EVS_ALL_STATS</c>): the Effort Ribbon's price.</summary>
    public const int EffortNeeded = 510;

    /// <summary>The friendship Dr. Footstep's ribbon asks: all there is.</summary>
    public const int FootprintFriendship = 255;

    /// <summary>How many different ribbons the team must show to be let into the Ribbon Syndicate (<c>CountPartyRibbons</c>).</summary>
    public const int SyndicateNeeds = 10;

    /// <summary>The Syndicate's three ribbons in the order they are sold, with what each costs.</summary>
    public static readonly IReadOnlyList<(string Ribbon, int Price)> SyndicateRibbons = new[]
    {
        (Gorgeous, 10_000), (Royal, 100_000), (GorgeousRoyal, 999_999)
    };

    /// <summary>Whether a Pokémon has a ribbon (<c>ScrCmd_GetPartyMonRibbon</c>).</summary>
    public static bool Has(Pokemon pokemon, string ribbon) => pokemon.Ribbons.Contains(ribbon);

    /// <summary>Puts a ribbon on a Pokémon (<c>ScrCmd_SetPartyMonRibbon</c>); false when it has it already, or the name is no ribbon.</summary>
    public static bool Give(Pokemon pokemon, string ribbon) => RibbonDatabase.Get(ribbon) != null && pokemon.Ribbons.Add(ribbon);

    /// <summary>A Pokémon's ribbons in the order the original lists them (the order of its summary's page).</summary>
    public static List<RibbonRecord> Of(Pokemon pokemon) =>
        RibbonDatabase.All.Where(r => pokemon.Ribbons.Contains(r.Id)).ToList();

    /// <summary>
    /// How many different ribbons the team has between its Pokémon, Eggs left out (<c>ScrCmd_CountPartyRibbons</c>):
    /// what the Ribbon Syndicate's doorkeeper counts.
    /// </summary>
    public static int CountPartyRibbons(Party party) =>
        RibbonDatabase.All.Count(r => party.Members.Any(p => !p.IsEgg && p.Ribbons.Contains(r.Id)));

    /// <summary>The first of the team that is no Egg, whom every giver means (<c>GetFirstNonEggInParty</c>); -1 for none.</summary>
    public static int FirstNonEgg(Party party)
    {
        for (int i = 0; i < party.Count; i++)
            if (!party.Members[i].IsEgg) return i;
        return -1;
    }

    /// <summary>The ribbon Julia gives for the day's story (<c>scripts_sunyshore_city_northeast_house.s</c>).</summary>
    public static string Daily(DayOfWeek day) => day switch
    {
        DayOfWeek.Sunday => "RIBBON_SMILE",
        DayOfWeek.Monday => "RIBBON_ALERT",
        DayOfWeek.Tuesday => "RIBBON_SHOCK",
        DayOfWeek.Wednesday => "RIBBON_DOWNCAST",
        DayOfWeek.Thursday => "RIBBON_CARELESS",
        DayOfWeek.Friday => "RIBBON_RELAX",
        _ => "RIBBON_SNOOZE"
    };

    /// <summary>A Pokémon's effort values together (<c>GetPartyMonEVTotal</c>).</summary>
    public static int EffortTotal(Pokemon p) => p.EvHP + p.EvAttack + p.EvDefense + p.EvSpAttack + p.EvSpDefense + p.EvSpeed;

    /// <summary>Whether the woman of the Sunyshore Market gives the Effort Ribbon for it: its effort values are full.</summary>
    public static bool EarnsEffort(Pokemon p) => !p.IsEgg && EffortTotal(p) >= EffortNeeded;

    /// <summary>Whether Dr. Footstep gives his ribbon for it: its friendship is all there is.</summary>
    public static bool EarnsFootprint(Pokemon p) => !p.IsEgg && p.Friendship >= FootprintFriendship;

    /// <summary>
    /// How Dr. Footstep reads a Pokémon's feelings, 1 to 5 (the script's steps of friendship: under 70, from 70, 150,
    /// 200 and at 255).
    /// </summary>
    public static int FeelingLevel(Pokemon p) => p.Friendship >= 255 ? 5 : p.Friendship >= 200 ? 4 : p.Friendship >= 150 ? 3 : p.Friendship >= 70 ? 2 : 1;

    /// <summary>
    /// The ribbon the Syndicate's cashier offers a Pokémon next, and its price (<c>ResortAreaRibbonSyndicate1F_CashierF</c>):
    /// the Gorgeous Ribbon, then the Royal, then the Gorgeous Royal; null once it has all three.
    /// </summary>
    public static (string Ribbon, int Price)? SyndicateOffer(Pokemon p)
    {
        if (Has(p, GorgeousRoyal)) return null;
        if (Has(p, Royal)) return SyndicateRibbons[2];
        if (Has(p, Gorgeous)) return SyndicateRibbons[1];
        return SyndicateRibbons[0];
    }

    /// <summary>
    /// Every Pokémon of the team that is no Egg gets Sinnoh's Champion Ribbon as the team enters the Hall of Fame
    /// (<c>Party_GiveChampionRibbons</c>).
    /// </summary>
    public static void GiveChampion(Party party)
    {
        foreach (var p in party.Members.Where(p => !p.IsEgg)) p.Ribbons.Add(SinnohChamp);
    }

    /// <summary>What a ribbon is given for.</summary>
    public static RibbonKind Kind(string ribbon) => ribbon switch
    {
        "RIBBON_HOENN_CHAMPION" or SinnohChamp => RibbonKind.Champion,
        Effort => RibbonKind.Effort,
        "RIBBON_HOENN_WINNING" or "RIBBON_HOENN_VICTORY" => RibbonKind.Tower,
        "RIBBON_ABILITY" or "RIBBON_GREAT_ABILITY" or "RIBBON_DOUBLE_ABILITY" or "RIBBON_MULTI_ABILITY" or "RIBBON_PAIR_ABILITY" or "RIBBON_WORLD_ABILITY" => RibbonKind.Tower,
        "RIBBON_ALERT" or "RIBBON_SHOCK" or "RIBBON_DOWNCAST" or "RIBBON_CARELESS" or "RIBBON_RELAX" or "RIBBON_SNOOZE" or "RIBBON_SMILE" => RibbonKind.Daily,
        Gorgeous or Royal or GorgeousRoyal => RibbonKind.Syndicate,
        Footprint => RibbonKind.Footprint,
        _ when ContestOf(ribbon) != null => ribbon.StartsWith("RIBBON_HOENN_", StringComparison.Ordinal) ? RibbonKind.HoennContest : RibbonKind.Contest,
        "RIBBON_HOENN_ARTIST" => RibbonKind.HoennContest,
        _ => RibbonKind.Event
    };

    /// <summary>
    /// For a contest's ribbon, the quality and the rank from 0 (Hoenn's Normal, Super, Hyper, Master; Sinnoh's Normal,
    /// Great, Ultra, Master); null for any other.
    /// </summary>
    public static (ContestStat Quality, int Rank)? ContestOf(string ribbon)
    {
        string body = ribbon.StartsWith("RIBBON_HOENN_", StringComparison.Ordinal) ? ribbon["RIBBON_HOENN_".Length..]
            : ribbon.StartsWith("RIBBON_", StringComparison.Ordinal) ? ribbon["RIBBON_".Length..] : ribbon;
        var parts = body.Split('_');
        if (parts.Length is < 1 or > 2 || !Enum.TryParse<ContestStat>(parts[0], ignoreCase: true, out var quality)) return null;
        if (!Enum.IsDefined(quality) || char.IsDigit(parts[0][0])) return null;
        int rank = parts.Length == 1 ? 0 : parts[1] switch
        {
            "SUPER" or "GREAT" => 1,
            "HYPER" or "ULTRA" => 2,
            "MASTER" => 3,
            _ => -1
        };
        return rank < 0 ? null : (quality, rank);
    }

    /// <summary>
    /// Whether this game has someone who gives the ribbon yet, and if not, what it waits for: null for one that can be
    /// had. Our own account of the plans, for the summary's page and the write-up.
    /// </summary>
    public static string? WaitsFor(string ribbon) => Kind(ribbon) switch
    {
        RibbonKind.Contest or RibbonKind.Daily or RibbonKind.Footprint => null,
        RibbonKind.Effort => null,
        RibbonKind.Champion => ribbon == SinnohChamp ? null : "a Pokémon brought over from Hoenn",
        RibbonKind.Syndicate => "the Resort Area's Ribbon Syndicate",
        // The Battle Tower gives its four (plan 06 · R18a); the two that take another player wait for one (plan 07)
        RibbonKind.Tower => ribbon.StartsWith("RIBBON_HOENN_", StringComparison.Ordinal) ? "a Pokémon brought over from Hoenn"
            : ribbon == "RIBBON_PAIR_ABILITY" ? "a friend to battle beside in the Battle Tower"
            : ribbon == "RIBBON_WORLD_ABILITY" ? "the Battle Tower's battles with the world" : null,
        RibbonKind.HoennContest => "a Pokémon brought over from Hoenn",
        _ => "an event"
    };

    private static readonly string[] HoennRanks = { "Normal", "Super", "Hyper", "Master" };
    private static readonly string[] SinnohRanks = { "Normal", "Great", "Ultra", "Master" };

    /// <summary>What a ribbon's page says of it, in our own words.</summary>
    public static string Describe(string ribbon)
    {
        if (ContestOf(ribbon) is var (quality, rank))
        {
            bool hoenn = ribbon.StartsWith("RIBBON_HOENN_", StringComparison.Ordinal);
            string kind = quality == ContestStat.Smart ? (hoenn ? "Smartness" : "Smart") : hoenn ? quality switch
            {
                ContestStat.Cool => "Coolness",
                ContestStat.Beauty => "Beauty",
                ContestStat.Cute => "Cuteness",
                _ => "Toughness"
            } : quality.ToString();
            return hoenn
                ? $"For winning Hoenn's {kind} Contest at the {HoennRanks[rank]} Rank."
                : $"For winning a {kind} Super Contest at the {SinnohRanks[rank]} Rank.";
        }
        return ribbon switch
        {
            "RIBBON_HOENN_CHAMPION" => "For beating Hoenn's Champion and entering its Hall of Fame.",
            "RIBBON_HOENN_WINNING" => "For clearing the Level 50 challenge of Hoenn's Battle Tower.",
            "RIBBON_HOENN_VICTORY" => "For clearing the Level 100 challenge of Hoenn's Battle Tower.",
            "RIBBON_HOENN_ARTIST" => "For being chosen as the model of a painting in Hoenn.",
            Effort => "For a Pokémon that has worked as hard as a Pokémon can.",
            "RIBBON_HOENN_MARINE" => "A ribbon of the sea, given at a gathering far away.",
            "RIBBON_HOENN_LAND" => "A ribbon of the land, given at a gathering far away.",
            "RIBBON_HOENN_SKY" => "A ribbon of the sky, given at a gathering far away.",
            "RIBBON_HOENN_COUNTRY" => "For a champion of a tournament held across a country.",
            "RIBBON_HOENN_NATIONAL" => "For a Pokémon that came through every hardship set before it.",
            "RIBBON_HOENN_EARTH" => "For a hundred battles won in a row.",
            "RIBBON_HOENN_WORLD" => "For a champion of a tournament held across the world.",
            SinnohChamp => "For beating Sinnoh's Champion and entering the Hall of Fame.",
            "RIBBON_ABILITY" => "For beating the Tower Tycoon at the Battle Tower.",
            "RIBBON_GREAT_ABILITY" => "For beating the Tower Tycoon a second, harder time.",
            "RIBBON_DOUBLE_ABILITY" => "For a long streak in the Battle Tower's Double Battles.",
            "RIBBON_MULTI_ABILITY" => "For a long streak in the Battle Tower's Multi Battles.",
            "RIBBON_PAIR_ABILITY" => "For a long streak in the Battle Tower beside a friend.",
            "RIBBON_WORLD_ABILITY" => "For rising through the ranks of the Battle Tower's battles with the world.",
            "RIBBON_ALERT" => "A ribbon for remembering a moment of being wide awake.",
            "RIBBON_SHOCK" => "A ribbon for remembering a moment that came as a shock.",
            "RIBBON_DOWNCAST" => "A ribbon for remembering a moment of feeling low.",
            "RIBBON_CARELESS" => "A ribbon for remembering a slip that couldn't be taken back.",
            "RIBBON_RELAX" => "A ribbon for remembering a moment of ease.",
            "RIBBON_SNOOZE" => "A ribbon for remembering a deep, deep sleep.",
            "RIBBON_SMILE" => "A ribbon for remembering a moment that brought a smile.",
            Gorgeous => "A ribbon of dazzling make, and a dazzling price.",
            Royal => "A ribbon fit for royalty, at a price to match.",
            GorgeousRoyal => "The most lavish ribbon there is. Few have ever seen its price paid.",
            Footprint => "For a Pokémon whose heart is wholly open to its Trainer.",
            "RIBBON_RECORD" => "For a record that had never been set before.",
            "RIBBON_HISTORY" => "For a deed that went down in history.",
            "RIBBON_LEGEND" => "For a deed that became a legend.",
            "RIBBON_RED" or "RIBBON_GREEN" or "RIBBON_BLUE" => "A ribbon of one colour, from a gathering nobody remembers.",
            "RIBBON_FESTIVAL" => "A keepsake of a festival.",
            "RIBBON_CARNIVAL" => "A keepsake of a carnival.",
            "RIBBON_CLASSIC" => "A ribbon given at a classic gathering of Pokémon.",
            "RIBBON_PREMIER" => "A ribbon for a day set apart from all the others.",
            _ => "A ribbon."
        };
    }
}
