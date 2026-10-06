namespace PokemonPlatinumEngine.Overworld;

/// <summary>Whether the Bicycle can be got on or off where the player is.</summary>
public enum BicycleCheck { Ok, CannotDismount, NotHere }

/// <summary>
/// The Bicycle's rules (plan 02 · S2), by the original's check before it is used (<c>CanUseBicycle</c>): no getting
/// off on the Cycling Road or on a plank only a Bicycle crosses, no getting on or off in grass taller than the
/// rider or in mud, nor where the place's header doesn't allow it (rooms, some caves), nor on the water. No drawing
/// and no input: the game asks it when the Bicycle is used from the bag or its button.
/// </summary>
public static class BicycleRules
{
    /// <summary>
    /// The original's flag for being on the Cycling Road (<c>FLAG_ON_CYCLING_ROAD</c>): set on coming through one of
    /// its gates, which only let riders through, and cleared by the next warp. While it is set the Bicycle can't be
    /// got off.
    /// </summary>
    public const string OnCyclingRoadFlag = "FLAG_ON_CYCLING_ROAD";

    public static BicycleCheck Check(Map map, int x, int y, TravelMode mode, bool onCyclingRoad)
    {
        var underfoot = map.BehaviourAt(x, y);
        if (mode == TravelMode.Cycling && (onCyclingRoad || FieldMovement.BikePlankRunsNorthSouth(underfoot) != null)) return BicycleCheck.CannotDismount;
        if (underfoot is TileBehavior.VeryTallGrass or TileBehavior.Mud or TileBehavior.DeepMud or TileBehavior.MarshGrass or TileBehavior.DeepMarshGrass)
            return BicycleCheck.NotHere;
        if (!map.BikeAllowedAt(x, y) || mode == TravelMode.Surfing) return BicycleCheck.NotHere;
        return BicycleCheck.Ok;
    }

    /// <summary>What is said when it can't be done, in our own words.</summary>
    public static string Why(BicycleCheck check) => check == BicycleCheck.CannotDismount
        ? "There's no getting off the Bicycle here."
        : "The Bicycle can't be ridden here.";
}
