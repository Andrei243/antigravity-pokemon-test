using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumEngine.Models;

/// <summary>The PokéNav's pages (Emerald's main menu of the PokéNav, <c>include/pokenav.h</c>'s order).</summary>
public enum PokeNavPage { Map, Condition, MatchCall, Ribbons }

/// <summary>
/// The PokéNav (plan 21 · H2, part b), Hoenn's key item screen as the Pokégear is Johto's: Mr. Stone gives it in
/// Devon's office (<c>FLAG_SYS_POKENAV_GET</c>, Emerald's own flag), a scientist of Devon adds Match Call to it outside
/// (<c>FLAG_HAS_MATCH_CALL</c>), and its pages are the map of Hoenn, the team's contest condition, the numbers
/// registered and the team's ribbons. What it remembers is the story's own flags, as the Pokégear's is: a number of
/// Hoenn's phone book (<c>Data/world/hoenn/phone.json</c>) by the same flag a Pokégear number is kept by
/// (<see cref="Pokegear.ContactFlag"/>). It is Hoenn's alone: in any other region it is nothing. No drawing or input.
/// </summary>
public static class PokeNav
{
    public const string Flag = "FLAG_SYS_POKENAV_GET", MatchCallFlag = "FLAG_HAS_MATCH_CALL";

    /// <summary>Whether the PokéNav does anything here: it is Hoenn's, and the player has been given it.</summary>
    public static bool Has(StoryState story, string region) =>
        string.Equals(region, RegionDatabase.Hoenn, StringComparison.OrdinalIgnoreCase) && story.Has(Flag);

    /// <summary>
    /// The pages it shows, in Emerald's order: the map and the condition always, Match Call once a scientist has added
    /// it, and the ribbons once a Pokémon of the team wears one (Emerald's menu leaves the page out until then).
    /// </summary>
    public static IReadOnlyList<PokeNavPage> Pages(StoryState story, IEnumerable<Pokemon> team)
    {
        var pages = new List<PokeNavPage> { PokeNavPage.Map, PokeNavPage.Condition };
        if (story.Has(MatchCallFlag)) pages.Add(PokeNavPage.MatchCall);
        if (team.Any(p => !p.IsEgg && p.Ribbons.Count > 0)) pages.Add(PokeNavPage.Ribbons);
        return pages;
    }

    /// <summary>The numbers registered in Match Call, in the book's order.</summary>
    public static IReadOnlyList<PhoneContact> Contacts(StoryState story) =>
        PhoneBook.Hoenn.Where(c => story.Has(Pokegear.ContactFlag(c.Id))).ToList();

    /// <summary>The five qualities of the contest condition, as the page draws them round its pentagon (cool at the top, then clockwise).</summary>
    public static int[] Condition(Pokemon p) => new[] { p.Cool, p.Beauty, p.Cute, p.Smart, p.Tough };
}
