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
/// field move of the grid's and are not listed: Stoutland is a story flag alone (FLAG_ALOLA_RIDE_STOUTLAND), Mudsdale is one too
/// (plan 23 · Z7: MudsdaleFlag, lent by Tapu Village's rancher). Since wave 91 the three that carry the player on land
/// (<see cref="LandRides"/>) are ridden: the key item (<see cref="Item"/>) opens <c>common.RidePager</c>, whose
/// <c>ride "Tauros"</c> puts the player on its back (<see cref="TravelMode.Riding"/>, drawn sitting on its field sprite as a
/// surfer is) at a run's pace, and a charging Tauros breaks a cracked rock it runs into (<see cref="Charges"/>).
/// </summary>
public static class RidePager
{
    /// <summary>The key item that calls a ride (wave 91), handed over with Tauros by Hala's grand trial.</summary>
    public const string Item = "Ride Pager";

    /// <summary>The rides that carry the player on land, by species, and the flag each is registered by.</summary>
    public static readonly IReadOnlyList<(string Species, string Flag)> LandRides = new[]
    {
        ("Tauros", "FLAG_ALOLA_RIDE_TAUROS"),
        ("Stoutland", StoutlandFlag),
        ("Mudsdale", MudsdaleFlag),
    };

    /// <summary>A ride on land by its species; null where the pager has none.</summary>
    public static (string Species, string Flag)? LandRideOf(string species) =>
        LandRides.FirstOrDefault(r => string.Equals(r.Species, species, StringComparison.OrdinalIgnoreCase)) is { Species: not null } ride ? ride : null;

    /// <summary>
    /// Whether the player may get on a ride here: the region offers riding (<c>movement.modes</c> lists
    /// <see cref="RegionMode.Ride"/>, Alola's) and the ride is registered by its flag.
    /// </summary>
    public static bool CanRide(string species, StoryState story, Region region) =>
        region.Movement.Offers(RegionMode.Ride) && LandRideOf(species) is { } ride && story.Has(ride.Flag);

    /// <summary>Whether a ride breaks a cracked rock it runs into (Tauros's charge), as Rock Smash does.</summary>
    public static bool Charges(string? species) => string.Equals(species, "Tauros", StringComparison.OrdinalIgnoreCase);

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

    /// <summary>The flag that registers Mudsdale, whose Gallop crosses rough ground; no field move of the grid's either (plan 23 · Z7: Tapu Village's rancher lends it).</summary>
    public const string MudsdaleFlag = "FLAG_ALOLA_RIDE_MUDSDALE";

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
