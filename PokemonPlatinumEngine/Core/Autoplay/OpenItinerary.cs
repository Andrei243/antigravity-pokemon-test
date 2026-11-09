using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.OpenWorld;

namespace PokemonPlatinumEngine.Core.Autoplay;

/// <summary>What a leg of an <see cref="OpenItinerary"/> asks of the bot in an open map (plan 32 · OW10).</summary>
public enum OpenLegKind
{
    /// <summary>Stand on a marker of the map (within <see cref="OpenItinerary.Reached"/> of it on the ground).</summary>
    Marker,
    /// <summary>Stand at a place of the map, in millimetres.</summary>
    Place,
    /// <summary>Walk up to a wild Pokémon of a zone and meet it: the leg is done once a wild battle has been begun and is over.</summary>
    MeetWild
}

/// <summary>
/// A leg of the way across an open map: a marker by its name (<see cref="What"/>), a place (<see cref="At"/>), or a
/// zone whose wild Pokémon is to be met (<see cref="What"/> the zone's name).
/// </summary>
public sealed record OpenLeg(OpenLegKind Kind, string What = "", OpenPos At = default)
{
    public static OpenLeg Marker(string name) => new(OpenLegKind.Marker, name);
    public static OpenLeg Place(OpenPos at) => new(OpenLegKind.Place, At: at);
    public static OpenLeg MeetWild(string zone) => new(OpenLegKind.MeetWild, zone);

    public override string ToString() => Kind switch
    {
        OpenLegKind.Marker => $"Marker {What}",
        OpenLegKind.Place => $"Place {At}",
        _ => $"Meet a wild Pokémon of {What}"
    };
}

/// <summary>
/// The way across an open map for the playthrough bot (plan 32 · OW10), GPU-free: legs in order, each a place to stand
/// or a zone's wild Pokémon to meet, which the bot goes for while any are left (<see cref="Autoplayer.OpenWay"/>). As
/// the grid's <see cref="Itinerary"/> does, the legs say only where; the bot finds the way by the navigation mesh
/// (<see cref="Policy.Route(OpenMap, OpenPos, OpenPos, MovementGrants)"/>), steers the stick at each corner of the path by
/// the field's own rules, and plays every trainer who sees it, every trigger it crosses and every battle as they come.
/// A leg not done within <see cref="LegSeconds"/> of game time, or whose way is lost <see cref="MostTries"/> times, is
/// given up and named.
/// </summary>
public sealed class OpenItinerary
{
    /// <summary>How near a marker or a place on the ground the body must stand for the leg to be done, in millimetres.</summary>
    public const int Reached = 750;

    /// <summary>How many times a leg's way is laid afresh for want of progress before it is given up.</summary>
    public const int MostTries = 4;

    /// <summary>How long a leg may take, in seconds of game time spent in the field (battles and scenes not counted).</summary>
    public const float LegSeconds = 90f;

    private readonly List<OpenLeg> legs;

    public OpenItinerary(IEnumerable<OpenLeg> legs) => this.legs = legs.ToList();

    public IReadOnlyList<OpenLeg> Legs => legs;
    public int Index { get; private set; }
    public OpenLeg? Current => Index < legs.Count ? legs[Index] : null;
    public bool Finished => Index >= legs.Count;

    /// <summary>The legs done, in order, and those given up.</summary>
    public List<string> Done { get; } = new();
    public List<string> GivenUp { get; } = new();

    /// <summary>How often the current leg's way was laid afresh for want of progress, and the field's seconds spent on it.</summary>
    public int Tries { get; set; }
    public float Seconds { get; set; }

    /// <summary>The wild battles begun when the current leg began (a <see cref="OpenLegKind.MeetWild"/> leg is done at one more).</summary>
    public int WildAtStart { get; set; } = -1;

    /// <summary>On to the next leg, the current one done (or given up).</summary>
    public void Advance(bool givenUp = false)
    {
        if (Current is { } leg) (givenUp ? GivenUp : Done).Add(leg.ToString());
        Index++;
        Tries = 0;
        Seconds = 0;
        WildAtStart = -1;
    }
}
