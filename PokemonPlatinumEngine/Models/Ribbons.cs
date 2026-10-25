using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models.SuperContests;

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
        RibbonKind.Champion => ribbon == SinnohChamp ? null : Text.Of("ribbonDeeds.waitsFor.hoenn"),
        RibbonKind.Syndicate => Text.Of("ribbonDeeds.waitsFor.syndicate"),
        // The Battle Tower gives its four (plan 06 · R18a); the two that take another player wait for one (plan 07)
        RibbonKind.Tower => ribbon.StartsWith("RIBBON_HOENN_", StringComparison.Ordinal) ? Text.Of("ribbonDeeds.waitsFor.hoenn")
            : ribbon == "RIBBON_PAIR_ABILITY" ? Text.Of("ribbonDeeds.waitsFor.friend")
            : ribbon == "RIBBON_WORLD_ABILITY" ? Text.Of("ribbonDeeds.waitsFor.world") : null,
        // Lilycove's Contest Hall gives Hoenn's contest ribbons and the painter her Artist Ribbon (plan 21 · H11)
        RibbonKind.HoennContest => null,
        _ => Text.Of("ribbonDeeds.waitsFor.event")
    };

    /// <summary>What a ribbon's page says of it, in our own words.</summary>
    public static string Describe(string ribbon)
    {
        if (ContestOf(ribbon) is var (quality, rank))
        {
            // Hoenn's contests are named for the quality's noun (Coolness) and have ranks of their own; a Super
            // Contest's ribbon says the quality and the rank as the contest's screen does
            if (ribbon.StartsWith("RIBBON_HOENN_", StringComparison.Ordinal))
                return Text.Of("ribbonDeeds.hoennContest", ("kind", Text.Of($"ribbonDeeds.hoennKind.{quality}")), ("rank", Text.Of($"ribbonDeeds.hoennRank.{rank}")));
            var sinnohRank = (ContestRank)rank;
            return Text.Of("ribbonDeeds.superContest", ("kind", Text.Of($"contest.category.{quality}")), ("rank", Text.Of($"contest.rank.{sinnohRank}")));
        }
        return ribbon switch
        {
            "RIBBON_HOENN_CHAMPION" => Text.Of("ribbonDeeds.hoennChampion"),
            "RIBBON_HOENN_WINNING" => Text.Of("ribbonDeeds.hoennWinning"),
            "RIBBON_HOENN_VICTORY" => Text.Of("ribbonDeeds.hoennVictory"),
            "RIBBON_HOENN_ARTIST" => Text.Of("ribbonDeeds.hoennArtist"),
            Effort => Text.Of("ribbonDeeds.effort"),
            "RIBBON_HOENN_MARINE" => Text.Of("ribbonDeeds.hoennMarine"),
            "RIBBON_HOENN_LAND" => Text.Of("ribbonDeeds.hoennLand"),
            "RIBBON_HOENN_SKY" => Text.Of("ribbonDeeds.hoennSky"),
            "RIBBON_HOENN_COUNTRY" => Text.Of("ribbonDeeds.hoennCountry"),
            "RIBBON_HOENN_NATIONAL" => Text.Of("ribbonDeeds.hoennNational"),
            "RIBBON_HOENN_EARTH" => Text.Of("ribbonDeeds.hoennEarth"),
            "RIBBON_HOENN_WORLD" => Text.Of("ribbonDeeds.hoennWorld"),
            SinnohChamp => Text.Of("ribbonDeeds.sinnohChampion"),
            "RIBBON_ABILITY" => Text.Of("ribbonDeeds.ability"),
            "RIBBON_GREAT_ABILITY" => Text.Of("ribbonDeeds.greatAbility"),
            "RIBBON_DOUBLE_ABILITY" => Text.Of("ribbonDeeds.doubleAbility"),
            "RIBBON_MULTI_ABILITY" => Text.Of("ribbonDeeds.multiAbility"),
            "RIBBON_PAIR_ABILITY" => Text.Of("ribbonDeeds.pairAbility"),
            "RIBBON_WORLD_ABILITY" => Text.Of("ribbonDeeds.worldAbility"),
            "RIBBON_ALERT" => Text.Of("ribbonDeeds.alert"),
            "RIBBON_SHOCK" => Text.Of("ribbonDeeds.shock"),
            "RIBBON_DOWNCAST" => Text.Of("ribbonDeeds.downcast"),
            "RIBBON_CARELESS" => Text.Of("ribbonDeeds.careless"),
            "RIBBON_RELAX" => Text.Of("ribbonDeeds.relax"),
            "RIBBON_SNOOZE" => Text.Of("ribbonDeeds.snooze"),
            "RIBBON_SMILE" => Text.Of("ribbonDeeds.smile"),
            Gorgeous => Text.Of("ribbonDeeds.gorgeous"),
            Royal => Text.Of("ribbonDeeds.royal"),
            GorgeousRoyal => Text.Of("ribbonDeeds.gorgeousRoyal"),
            Footprint => Text.Of("ribbonDeeds.footprint"),
            "RIBBON_RECORD" => Text.Of("ribbonDeeds.record"),
            "RIBBON_HISTORY" => Text.Of("ribbonDeeds.history"),
            "RIBBON_LEGEND" => Text.Of("ribbonDeeds.legend"),
            "RIBBON_RED" or "RIBBON_GREEN" or "RIBBON_BLUE" => Text.Of("ribbonDeeds.colour"),
            "RIBBON_FESTIVAL" => Text.Of("ribbonDeeds.festival"),
            "RIBBON_CARNIVAL" => Text.Of("ribbonDeeds.carnival"),
            "RIBBON_CLASSIC" => Text.Of("ribbonDeeds.classic"),
            "RIBBON_PREMIER" => Text.Of("ribbonDeeds.premier"),
            _ => Text.Of("ribbonDeeds.any")
        };
    }
}
