using System;
using PokemonPlatinumEngine.Data;
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

    // ------------------------------------------------------------------ its mood and its words (plan 35 · BN4)

    /// <summary>How far from calm a mood must be for the follower to speak of it before anything else.</summary>
    public const int StrongMood = 40;

    /// <summary>Whether a mood is strong enough to be what the follower speaks of.</summary>
    public static bool IsStrong(int mood) => Math.Abs(mood) >= StrongMood;

    /// <summary>The text key of a strong mood's line (<c>followerTalk.mood.Glum</c> and the rest, with <c>{name}</c>).</summary>
    public static string MoodKey(CareMood mood) => $"followerTalk.mood.{mood}";

    /// <summary>The bubble a mood shows: dots glum or low, a note cheerful, a heart elated; calm shows the band's.</summary>
    public static EmoteBubble BubbleOf(CareMood mood, int band) => mood switch
    {
        CareMood.Glum or CareMood.Low => EmoteBubble.Dots,
        CareMood.Cheerful => EmoteBubble.Note,
        CareMood.Elated => EmoteBubble.Heart,
        _ => BubbleOf(band)
    };

    /// <summary>A glum follower turns its back on the player as it is spoken to.</summary>
    public static bool TurnsItsBack(CareMood mood) => mood == CareMood.Glum;

    /// <summary>What of where it stands the follower may speak of, the first that holds in this order.</summary>
    public enum Spot { None, Gym, PokemonCenter, Room, Cave, Water, Snow, Sand, TallGrass, Rain, Night, MetHere, Town }

    /// <summary>What the game tells of the place the follower stands in.</summary>
    public readonly record struct Surroundings(bool Gym, bool PokemonCenter, bool Room, bool Cave, bool ByWater, bool Snow, bool Sand,
        bool TallGrass, bool Rain, bool Night, bool MetHere, bool Town);

    /// <summary>Whether an area is a town or a city, by its key (<c>twinleaf_town</c>, <c>jubilife_city</c>).</summary>
    public static bool IsTown(string? areaKey) =>
        areaKey != null && (areaKey.EndsWith("_town", StringComparison.Ordinal) || areaKey.EndsWith("_city", StringComparison.Ordinal));

    /// <summary>The place's line, the first of the order that holds (<see cref="Spot"/>).</summary>
    public static Spot PlaceOf(Surroundings at) =>
        at.Gym ? Spot.Gym : at.PokemonCenter ? Spot.PokemonCenter : at.Room ? Spot.Room : at.Cave ? Spot.Cave
        : at.ByWater ? Spot.Water : at.Snow ? Spot.Snow : at.Sand ? Spot.Sand : at.TallGrass ? Spot.TallGrass
        : at.Rain ? Spot.Rain : at.Night ? Spot.Night : at.MetHere ? Spot.MetHere : at.Town ? Spot.Town : Spot.None;

    /// <summary>
    /// What a type makes of a place (a table of ours): a Water type by water or in rain, a Fire type in rain (it doesn't
    /// like it), an Ice type in snow, a Ghost type at night, a Grass type in tall grass, a Rock type in a cave, a Ground type
    /// on sand. The key of its line (<c>followerTalk.likes.*</c>), or null when its type says nothing of the place.
    /// </summary>
    public static string? Likes(Pokemon p, Surroundings at)
    {
        if (p.HasType(PokemonType.Water) && (at.ByWater || at.Rain)) return "followerTalk.likes.water";
        if (p.HasType(PokemonType.Fire) && at.Rain) return "followerTalk.likes.fireInRain";
        if (p.HasType(PokemonType.Ice) && at.Snow) return "followerTalk.likes.ice";
        if (p.HasType(PokemonType.Ghost) && at.Night) return "followerTalk.likes.ghost";
        if (p.HasType(PokemonType.Grass) && at.TallGrass) return "followerTalk.likes.grass";
        if (p.HasType(PokemonType.Rock) && at.Cave) return "followerTalk.likes.rock";
        if (p.HasType(PokemonType.Ground) && at.Sand) return "followerTalk.likes.ground";
        return null;
    }

    /// <summary>
    /// The line the follower says, as a text key: a strong mood's first; otherwise, one talk in two by a hash of its id, its
    /// steps and its tile (never a draw, so nothing seeded moves), what its type makes of the place, or the place's own line;
    /// otherwise its band's (F4).
    /// </summary>
    public static string LineFor(Pokemon p, int mood, int band, Surroundings at, int steps, int x, int y)
    {
        if (IsStrong(mood)) return MoodKey(CareRules.MoodOf(mood));
        uint roll = Core.SplitMix.Mix(Core.SplitMix.Fnv(p.Id.ToString()), (uint)steps, (uint)(x * 1024 + y));
        if ((roll & 1) == 0)
        {
            if (Likes(p, at) is { } liked) return liked;
            if (PlaceOf(at) is var place and not Spot.None) return $"followerTalk.place.{place}";
        }
        return LineKey(band);
    }

    /// <summary>Steps between two bubbles of the follower's own (after a battle, a level, an evolution, a new place, the weather's turn).</summary>
    public const int OwnBubbleSteps = 24;

    /// <summary>The bubble of its own something brings: a win a note (a heart elated), a loss or a faint dots, a level or an evolution a heart, a new place or the weather a question.</summary>
    public enum Moment { Won, Lost, Levelled, Evolved, NewPlace, Weather }

    public static EmoteBubble BubbleFor(Moment moment, CareMood mood) => moment switch
    {
        Moment.Won => mood == CareMood.Elated ? EmoteBubble.Heart : EmoteBubble.Note,
        Moment.Lost => EmoteBubble.Dots,
        Moment.Levelled or Moment.Evolved => EmoteBubble.Heart,
        _ => mood is CareMood.Glum or CareMood.Low ? EmoteBubble.Dots : EmoteBubble.Question
    };
}
