using System;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Overworld;

/// <summary>
/// The lead out of its ball (plan 10 · F3 and F4; style guide, "The follower"; <c>docs/mechanics/rulings.md</c>,
/// "The follower"): who walks behind the player, where it may, and what it says when spoken to. The walking itself
/// is <see cref="Follower"/>'s, the one follower the game has; this says only when the options' FOLLOWER row gives
/// one and what it answers. No drawing or input, and no roll but the one it is handed.
/// </summary>
public static class LeadFollower
{
    /// <summary>The key of the thing of the map the lead is dressed as on whatever map it walks.</summary>
    public const string BodyKey = "lead_follower";

    /// <summary>Who would walk behind the player: the team's first Pokémon that can stand (no Egg, nobody fainted).</summary>
    public static Pokemon? Walker(Party party) => party.FirstUsable;

    /// <summary>What of where the player is now decides whether the lead may be out of its ball.</summary>
    /// <param name="Mode">How the player travels: only on foot does anyone walk behind them.</param>
    /// <param name="Dark">A cave still dark (Flash not used there).</param>
    /// <param name="Underground">Below ground (plan 06 · R16a), where nothing of the team comes out.</param>
    /// <param name="AmitySquare">Amity Square, whose own walk lets a Pokémon out by its receptionists (plan 06 · R16c).</param>
    /// <param name="SomeoneElse">Someone else walks behind the player: a partner of the story, the square's walker.</param>
    public readonly record struct Place(TravelMode Mode, bool Dark, bool Underground, bool AmitySquare, bool SomeoneElse);

    /// <summary>
    /// Whether the lead walks behind the player here: the row is on, there is one that can stand, the player is on
    /// foot, and none of the places that put it back in its ball holds (style guide, "The follower").
    /// </summary>
    public static bool MayWalk(bool option, Pokemon? walker, Place place) =>
        option && walker != null && place.Mode == TravelMode.OnFoot
        && !place.Dark && !place.Underground && !place.AmitySquare && !place.SomeoneElse;

    // ------------------------------------------------------------------ spoken to (F4)

    /// <summary>The five bands of friendship the follower answers by: the lowest bound of each, from 0 to 4.</summary>
    public static readonly int[] Bands = { 0, 50, 100, 150, FriendshipRules.EvolveAt };

    /// <summary>The band a friendship falls in, 0 (wary) to 4 (devoted: the friendship evolutions ask, 220).</summary>
    public static int BandOf(int friendship)
    {
        int band = 0;
        for (int k = 1; k < Bands.Length; k++) if (friendship >= Bands[k]) band = k;
        return band;
    }

    /// <summary>The text key of what the follower is seen to do in a band (<c>followerTalk.band.N</c>, with <c>{name}</c>).</summary>
    public static string LineKey(int band)
    {
        int n = Math.Clamp(band, 0, Bands.Length - 1);
        return $"followerTalk.band.{n}";
    }

    /// <summary>The bubble over its head in a band: dots, a question, a note, a note, a heart.</summary>
    public static EmoteBubble BubbleOf(int band) => band switch
    {
        <= 0 => EmoteBubble.Dots,
        1 => EmoteBubble.Question,
        2 or 3 => EmoteBubble.Note,
        _ => EmoteBubble.Heart
    };

    /// <summary>Steps the follower walks before it can have found something again.</summary>
    public const int FindSteps = 256;

    /// <summary>One in this many talks, once a find is due, it is holding something.</summary>
    public const int FindOdds = 3;

    /// <summary>
    /// What a follower may turn up (a table of our own, as Pickup's is the original's): berries most often, then a
    /// Potion or a Poké Ball, now and then something better. Weights out of their sum.
    /// </summary>
    public static readonly (string Item, int Weight)[] Finds =
    {
        ("Oran Berry", 20), ("Pecha Berry", 12), ("Cheri Berry", 12), ("Chesto Berry", 10), ("Rawst Berry", 10),
        ("Potion", 14), ("Poké Ball", 10), ("Antidote", 6), ("Great Ball", 4), ("Ether", 2)
    };

    /// <summary>
    /// Whether the follower is holding something when spoken to, and what: nothing until it has walked
    /// <see cref="FindSteps"/> since its last find, then one talk in <see cref="FindOdds"/>, the item drawn by weight.
    /// Both draws are on <paramref name="rng"/>, the field's own generator (two draws, or none while no find is due).
    /// </summary>
    public static string? Find(int stepsSinceFind, Random rng)
    {
        if (stepsSinceFind < FindSteps) return null;
        if (rng.Next(FindOdds) != 0) return null;
        int total = 0;
        foreach (var (_, weight) in Finds) total += weight;
        int roll = rng.Next(total);
        foreach (var (item, weight) in Finds)
        {
            if (roll < weight) return item;
            roll -= weight;
        }
        return Finds[0].Item;
    }
}
