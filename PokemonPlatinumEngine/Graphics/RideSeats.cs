using System;
using System.Collections.Generic;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// Where a grown rider sits a ride of the grid (style guide, "Live rigs in the field", "A ride's seat"): by species, since
/// a Stoutland's back is a knee's height and a Mudsdale's a shoulder's, and one share of every sprite's height put a teen's
/// feet on the ground beside a small ride. No GPU calls; a chibi region reads none of it.
/// </summary>
/// <param name="Seat">How high the hips sit, as a share of the rows the ride's card fills (a land ride) or of what shows of it over the water (a water ride).</param>
/// <param name="Ahead">How much nearer the camera than its rider the ride's card stands, in tiles: past the rider's legs.</param>
/// <param name="Tuck">How far the legs fold up astride, 0 to 1 (<see cref="CharacterPose.RideTuck"/>): a small ride's rider keeps the feet behind its card.</param>
/// <param name="Back">Half the ride's width where the rider sits, in tiles, which the thighs spread round; nought for the plain spread.</param>
internal readonly record struct RideSeat(float Seat, float Ahead, float Tuck, float Back);

internal static class RideSeats
{
    /// <summary>A land ride the table doesn't name: on the top of its back, nineteen twentieths of its card, the card half a tile ahead (the gate's own numbers).</summary>
    public static readonly RideSeat Land = new(0.95f, 0.5f, 0f, 0f);

    /// <summary>A water ride the table doesn't name: a little over half of what shows of it, as a chibi rider's.</summary>
    public static readonly RideSeat Water = new(0.55f, 0.06f, 0f, 0f);

    /// <summary>
    /// The rides of the Ride Pager and the regions' own, by species. A land ride's seat is the top of its back, under
    /// whatever rises over it (horns, a head, a mane); a water ride's is where a kneeling rider's knees are.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, RideSeat> Table = new Dictionary<string, RideSeat>(StringComparer.Ordinal)
    {
        // Land: the horns and the head stand over the back, and the small ones fold the rider's legs up
        ["Tauros"] = new(0.80f, 0.5f, 0.75f, 0.24f),
        ["Stoutland"] = new(0.70f, 0.5f, 0.55f, 0.22f),
        ["Mudsdale"] = new(0.74f, 0.55f, 0.1f, 0.30f),
        ["Machamp"] = new(0.84f, 0.5f, 0.6f, 0.26f),
        // (Kalos's Mamoswine keeps the seat its gate was shot and approved with: on the top of its hump)
        ["Mamoswine"] = Land,
        ["Gogoat"] = new(0.76f, 0.5f, 0.45f, 0.22f),
        ["Rhyhorn"] = new(0.84f, 0.5f, 0.9f, 0.26f),
        // Water: kneeling on a long neck's foot, a shark's back behind its fin, a manta's wide back
        ["Lapras"] = new(0.50f, 0.06f, 0f, 0f),
        ["Sharpedo"] = new(0.62f, 0.06f, 0f, 0f),
        ["Mantine"] = new(0.48f, 0.06f, 0f, 0f),
    };

    /// <summary>The seat of a ride by its species' name (a field sprite's name is read past any mark after it); the plain one for a species the table lacks.</summary>
    public static RideSeat Of(string? species, bool water)
    {
        if (species != null)
        {
            string name = FieldSprites.ModelOf(species);
            if (Table.TryGetValue(name, out var seat)) return seat;
        }
        return water ? Water : Land;
    }
}
