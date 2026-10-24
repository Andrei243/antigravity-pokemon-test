using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumEngine.Models;

/// <summary>
/// The rules of going from one region to another (plan 18 · W4, part b; <c>docs/mechanics/rulings.md</c>, "Regions"),
/// with no drawing or input. Under the user's option B (2026-10-21) the team crosses whole and at full strength: every
/// Pokémon may come (<see cref="CanTake"/>), so <see cref="Depart"/> leaves the party as it is and nothing rests in the
/// boxes. What a crossing changes is the region the player is in: its Pokédex is entered and <c>{rival}</c> names its
/// rival (<see cref="Arrive"/>); the levels its Pokémon are met at follow the region by themselves
/// (<c>GameEngine.LevelHere</c>), and money, the bag, the Pokédex, the Trainer Card and every region's badges go along
/// untouched. The journey on screen (the vessel's dark screen, <c>common.Crossing</c>) is plan 18 · W5's; the way back
/// (harbours) W8's, which sorts the party by <see cref="CanTake"/> too.
/// </summary>
public static class Crossing
{
    /// <summary>
    /// Whether a Pokémon may cross into a region with the player. Under option B every one may, whatever region it
    /// belongs to: the rule is kept as the one place a later choice (a game of one region, a format of plan 13) asks.
    /// </summary>
    public static bool CanTake(Pokemon pokemon, Region to, StoryState story) => true;

    /// <summary>
    /// The party as the player leaves for <paramref name="to"/>: whoever <see cref="CanTake"/> allows stays, in the
    /// order they were, and the rest go into the boxes (into the first free place; one the boxes can't hold stays, so
    /// nobody is lost). Gives back those moved, which under option B is nobody.
    /// </summary>
    public static IReadOnlyList<Pokemon> Depart(Party party, PcBoxes boxes, Region to, StoryState story)
    {
        var moved = new List<Pokemon>();
        foreach (var member in party.Members.ToList())
        {
            if (CanTake(member, to, story) || boxes.IsFull) continue;
            party.Remove(member);
            boxes.Store(member);
            moved.Add(member);
        }
        return moved;
    }

    /// <summary>
    /// Every Pokémon that belongs to no region yet is given <paramref name="region"/>, the one the player stands in
    /// (<see cref="Pokemon.Region"/>): run as a game is saved and loaded, as a Pokémon is received and before a
    /// crossing, so a Pokémon is never given the region it is crossing into. Gives back how many were given one.
    /// </summary>
    public static int Belong(IEnumerable<Pokemon> owned, string region)
    {
        int given = 0;
        foreach (var pokemon in owned)
        {
            if (pokemon.Region != null) continue;
            pokemon.Region = region;
            given++;
        }
        return given;
    }

    /// <summary>A Pokémon received by a trade joins the region of whoever received it, wherever it came from.</summary>
    public static void Join(Pokemon received, string region) => received.Region = region;

    /// <summary>
    /// What arriving in a region changes beyond where the player stands: its Pokédex is entered (plan 18 · W3) and
    /// <c>{rival}</c> names its rival from now on, the name the player gave or the region's own until a
    /// <c>namerival</c> asks. It sets nothing of the levels and nothing of the story.
    /// </summary>
    public static void Arrive(Region region, Pokedex pokedex)
    {
        pokedex.Enter(region);
        PlayerIdentity.InRegion(region);
    }

    /// <summary>Whether a Pokémon belongs to a region; one that belongs to none (an older save's) to <paramref name="savesOwn"/>.</summary>
    public static bool BelongsTo(Pokemon pokemon, string region, string savesOwn) =>
        string.Equals(pokemon.Region ?? savesOwn, region, StringComparison.OrdinalIgnoreCase);
}
