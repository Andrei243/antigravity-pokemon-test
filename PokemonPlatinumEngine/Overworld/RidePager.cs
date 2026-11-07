using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumEngine.Overworld;

/// <summary>
/// A Pokémon registered in Alola's Ride Pager (plan 23 · Z5, second part): its species, the field move it does in place
/// of a team member who knows it, the story flag that registers it and the region it answers in.
/// </summary>
public sealed record Ride(string Species, FieldMove Move, string Flag, string Region);

/// <summary>
/// Alola's Ride Pager (plan 23 · Z5; Sun and Moon's Poké Ride, as plan 23's Alola decisions have it: the pager's
/// Pokémon are grants). GPU-free, and nothing is stored but the story's flags: a ride is registered by its flag (the
/// kahuna's or the captain's script sets it), and while the player is in the ride's region its move is granted to the
/// field whatever the team knows, as <see cref="FieldHelp"/>'s helpers are but with no option and no HM, since the
/// island challenge hands the rides over in place of HMs. The ride's species is who is seen doing the move
/// (<see cref="FieldHelp.HelperFor(FieldMove, StoryState?, string?)"/>). Stoutland's Search and Mudsdale's Gallop are no
/// field move of the grid's and are not listed: Stoutland is a story flag alone (FLAG_ALOLA_RIDE_STOUTLAND), Mudsdale is
/// plan 23 · Z7's. The rider drawn on its Pokémon is not built (plan 23's Alola section, "The engine for Alola").
/// </summary>
public static class RidePager
{
    /// <summary>The rides, in the order the island challenge hands them over.</summary>
    public static readonly IReadOnlyList<Ride> Rides = new[]
    {
        // Hala's, after Melemele's grand trial: a charge that breaks cracked rocks
        new Ride("Tauros", FieldMove.RockSmash, "FLAG_ALOLA_RIDE_TAUROS", RegionDatabase.Alola),
        // Lana's, at Brooklet Hill: across the water
        new Ride("Lapras", FieldMove.Surf, "FLAG_ALOLA_RIDE_LAPRAS", RegionDatabase.Alola),
        // Kiawe's, after Wela Volcano Park's trial: to any place visited (plan 23 · Z6 hands it over)
        new Ride("Charizard", FieldMove.Fly, "FLAG_ALOLA_RIDE_CHARIZARD", RegionDatabase.Alola),
        // Poni's: boulders pushed (plan 23 · Z7 hands it over)
        new Ride("Machamp", FieldMove.Strength, "FLAG_ALOLA_RIDE_MACHAMP", RegionDatabase.Alola),
    };

    /// <summary>The flag that registers Stoutland, whose Search finds hidden items; no field move of the grid's.</summary>
    public const string StoutlandFlag = "FLAG_ALOLA_RIDE_STOUTLAND";

    /// <summary>The rides registered and answering in a region.</summary>
    public static IEnumerable<Ride> Registered(StoryState story, string region) =>
        Rides.Where(r => r.Region == region && story.Has(r.Flag));

    /// <summary>The field moves the registered rides grant in a region.</summary>
    public static IEnumerable<FieldMove> Granted(StoryState story, string region) =>
        Registered(story, region).Select(r => r.Move).Distinct();

    /// <summary>The ride that does a move in a region, if one is registered.</summary>
    public static Ride? RideFor(FieldMove move, StoryState story, string region) =>
        Registered(story, region).FirstOrDefault(r => r.Move == move);
}
