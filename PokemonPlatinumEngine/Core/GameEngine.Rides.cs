using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// Alola's Ride Pager on land (plan 23, wave 91; rulings, "Alola's loose ends"): the key item opens
/// <c>common.RidePager</c>, whose <c>ride</c> puts the player on a registered Pokémon's back (<see cref="TravelMode.Riding"/>)
/// or takes them off it; the ride ends at a door indoors and anywhere the region offers none; Tauros, run into a cracked
/// rock, breaks it (<c>common.TaurosCharge</c>, the Rock Smash the pager grants).
/// </summary>
public partial class GameEngine
{
    /// <summary>The script the Ride Pager opens from the bag or the item button.</summary>
    public const string RidePagerScript = "common.RidePager";

    /// <summary>The script a charging Tauros runs on the cracked rock it runs into.</summary>
    public const string TaurosChargeScript = "common.TaurosCharge";

    /// <summary>
    /// Gets on a ride (<paramref name="species"/>) or off one (null), the script's <c>ride</c>: refused where the place
    /// offers no riding, the ride isn't registered, indoors, or the player is on the water, on the Bicycle or mid-step.
    /// </summary>
    internal bool RideOn(string? species)
    {
        if (species == null)
        {
            // (Sharpedo, under a surfer, goes home and leaves the player surfing: plan 23, Alola's water ride)
            if (flow.Player.Mode == TravelMode.Surfing) return flow.Player.SetJet(null);
            if (!flow.Player.SetRiding(null)) return false;
            PlayFieldMusic();
            return true;
        }
        string? area = flow.Map.AreaAt(flow.Player.GridX, flow.Player.GridY)?.Key;
        bool lent = RidePager.LentHere(species, Here.Id, area);
        if (!lent && !MovementHere.Offers(RegionMode.Ride) || !RidePager.CanRide(species, flow.Story, Here, area) || flow.Map.IsIndoors) return false;
        // A ride over the water is called only under a surfer, a ride on land only off the water
        if (RidePager.IsWaterRide(species) != (flow.Player.Mode == TravelMode.Surfing)) return false;
        if (RidePager.IsWaterRide(species) ? !flow.Player.SetJet(RidePager.SpeciesOf(species)) : !flow.Player.SetRiding(RidePager.SpeciesOf(species))) return false;
        PlayFieldMusic();
        return true;
    }

    /// <summary>
    /// A ride ends where riding may not go (plan 23, wave 91): through a door indoors, into a region or place that offers
    /// none; and a lent ride (Kalos's Mamoswine) anywhere but the area that lends it. Run as a place changes, as the
    /// Bicycle is put away where it isn't allowed.
    /// </summary>
    private void DismountWhereNoRiding()
    {
        if (flow.Player.Mode != TravelMode.Riding) return;
        string? steed = flow.Player.Steed;
        if (RidePager.LentRideOf(steed) != null)
        {
            if (flow.Map.IsIndoors || !RidePager.LentHere(steed, Here.Id, flow.Map.AreaAt(flow.Player.GridX, flow.Player.GridY)?.Key))
                flow.Player.SetRiding(null);
            return;
        }
        if (flow.Map.IsIndoors || !MovementHere.Offers(RegionMode.Ride))
            flow.Player.SetRiding(null);
    }

    /// <summary>
    /// What a ride ran into this frame: a charging ride breaks a cracked rock (<see cref="RidePager.Charges"/>), by the same
    /// Rock Smash the pager grants. True where a script began.
    /// </summary>
    private bool ChargeThroughRock()
    {
        if (flow.Player.TakeRanInto() is not { } tile || flow.Player.Mode != TravelMode.Riding || !RidePager.Charges(flow.Player.Steed)) return false;
        if (flow.Map.GetNpcAt(tile.X, tile.Y) is not { Obstacle: PropType.CrackedRock } rock) return false;
        return StartScript(TaurosChargeScript, rock);
    }

    // The hidden thing Stoutland last stopped for, so its nose doesn't stop at every step beside the same one
    private (string Map, int X, int Y)? sniffed;

    /// <summary>
    /// Stoutland's Search on a step (plan 23, the rest of the islands): ridden, it stops with its nose down where something
    /// hidden lies within reach (<see cref="StoutlandSearch"/>), the player's bubble and a sound saying so, once for each.
    /// </summary>
    private void SniffOnStoutland()
    {
        if (flow.Player.Mode != TravelMode.Riding || !StoutlandSearch.Sniffs(flow.Player.Steed)) return;
        if (StoutlandSearch.Nearest(flow.Map, flow.Player.GridX, flow.Player.GridY, flow.Story) is not { } found) return;
        var spot = (flow.Map.Name, found.X, found.Y);
        if (sniffed == spot) return;
        sniffed = spot;
        ShowEmote(null, EmoteBubble.Exclaim);
        AudioManager.PlaySound("dowsing_ping");
    }

    /// <summary>A save with a ride registered and no pager in the bag is owed the pager as it loads (no story version).</summary>
    private void OweTheRidePager()
    {
        if (ItemDatabase.Get(RidePager.Item) is not { } item || flow.Bag.GetQuantity(item) > 0) return;
        foreach (var (_, flag) in RidePager.LandRides)
            if (flow.Story.Has(flag))
            {
                flow.Bag.AddItem(item, 1);
                return;
            }
    }
}
