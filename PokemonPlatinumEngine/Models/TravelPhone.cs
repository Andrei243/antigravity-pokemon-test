using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumEngine.Models;

/// <summary>The travel phone's cards (plan 43 · LF6), in their own order.</summary>
public enum PhoneCard { Calls, News, Map, Powers }

/// <summary>
/// The travel phone (plan 43 · LF6; decision 4): one phone of ours that works in every region whose file says
/// <c>"phone": true</c> (Unova and the regions after it), once <see cref="Flag"/> is set, with that region's book
/// (<see cref="PhoneBook.Of"/>). The Pokégear and the PokéNav stay Johto's and Hoenn's. What it remembers is the
/// story's flags (the phone's own, and a number by the Pokégear's <see cref="Pokegear.ContactFlag"/>), so a save
/// keeps it with no field of its own. No drawing or input.
/// </summary>
public static class TravelPhone
{
    /// <summary>The story's flag of the phone given (ours).</summary>
    public const string Flag = "FLAG_GOT_TRAVEL_PHONE";

    /// <summary>Whether the phone works in a region: its file says so.</summary>
    public static bool WorksIn(string region) => RegionDatabase.Get(region)?.File?.Phone == true;

    /// <summary>Whether the phone does anything here: the region's file says it works there, and the player has it.</summary>
    public static bool Has(StoryState story, string region) => story.Has(Flag) && WorksIn(region);

    /// <summary>The cards: the calls and the news always, the map where the region has a map.</summary>
    public static IReadOnlyList<PhoneCard> Cards(string region, bool powers = false)
    {
        var cards = new List<PhoneCard> { PhoneCard.Calls, PhoneCard.News };
        if (RegionMap.Of(region) != null) cards.Add(PhoneCard.Map);
        // POWERS (plan 36 · TR8's panel), once the Power Watch is in the bag: the phone carries it as the C-Gear did
        if (powers) cards.Add(PhoneCard.Powers);
        return cards;
    }

    /// <summary>The numbers registered in a region's book, in the book's order.</summary>
    public static IReadOnlyList<PhoneContact> Contacts(StoryState story, string region) =>
        PhoneBook.Of(region).Where(c => story.Has(Pokegear.ContactFlag(c.Id))).ToList();

    /// <summary>
    /// The day's news (the NEWS card): the programme a television would show today, picked by the day's number (the
    /// same all day, the next day's another), in the words <see cref="Broadcasts"/> gives it.
    /// </summary>
    public static IReadOnlyList<string> News(BroadcastFacts facts, int dayNumber)
    {
        var on = Broadcasts.On(facts);
        if (on.Count == 0) return Array.Empty<string>();
        var programme = on[(int)((uint)dayNumber % (uint)on.Count)];
        return Broadcasts.Lines(programme, facts);
    }

    /// <summary>
    /// Whether a save is past the scene that gives the phone and so is owed it as it loads or crosses
    /// (<c>GameEngine.OweTheTravelPhone</c>; no story version raised): in Unova, Bianca's Pokédex at Aspertia's outlook
    /// (the phone comes with it); in Kalos, Mom's letter at home (with the roller skates); in a region with the phone and
    /// no scene of its own (Galar), being there at all. A Champion who crossed into Unova or Kalos meets neither scene,
    /// and is owed the phone as they arrive.
    /// </summary>
    public static bool Owed(StoryState story) =>
        !story.Has(Flag) && RegionDatabase.All.Any(r => r.File?.Phone == true && PastItsScene(story, r.Id));

    /// <summary>Whether the story is past the scene that gives the phone in a region (or the region has none and has been entered).</summary>
    public static bool PastItsScene(StoryState story, string region) => region switch
    {
        RegionDatabase.Unova => story.HasEntered(region) && story.Has(StoryState.PokedexFlag),
        RegionDatabase.Kalos => story.HasEntered(region) && (story.Var(KalosStoryVar) >= KalosGivenAt || story.BadgeCount(region) > 0),
        _ => WorksIn(region) && story.HasEntered(region),
    };

    /// <summary>
    /// Whether a region's numbers are owed (<c>GameEngine.OweTheTravelPhone</c>): the phone works there, the story is past
    /// its scene, and none of its book is saved yet: a phone given in Unova gets Kalos's numbers on crossing into it past
    /// Mom's letter, a scene a Champion who crossed in never sees.
    /// </summary>
    public static bool NumbersOwed(StoryState story, string region) =>
        WorksIn(region) && PastItsScene(story, region) && !PhoneBook.Of(region).Any(c => story.Has(Pokegear.ContactFlag(c.Id)));

    /// <summary>Kalos's count and the value it holds once Mom has read the professor's letter (vaniville_town.txt).</summary>
    public const string KalosStoryVar = "VAR_KALOS_STORY";
    public const int KalosGivenAt = 4;
}
