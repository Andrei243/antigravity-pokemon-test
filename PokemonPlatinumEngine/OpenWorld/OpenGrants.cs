using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumEngine.OpenWorld;

/// <summary>
/// What the player on foot may do in an open map because the story has granted it (plan 33 · GL5, its second session;
/// rulings, "Circhester, Route 9, Spikemuth and Hammerlocke's stadium"): the mount's powers are <see cref="MountRules"/>'
/// and only while mounted, these are the walker's own, handed to <see cref="OpenField.Grants"/> every frame by the engine.
/// Today it is one: Galar's bike fitted for the water on Route 9 lets the player onto deep water, a swim of the player's
/// own until the Rotom Bike has an engine (plan 30's <c>RotomBike</c> mode). Worked out from the story whenever asked, so
/// nothing is saved. GPU-free.
/// </summary>
public static class OpenGrants
{
    /// <summary>The flag Route 9's mechanic sets as he fits the bike for the water.</summary>
    public const string BikeWaterFlag = "FLAG_GALAR_BIKE_WATER";

    /// <summary>The walker's grants the story has given.</summary>
    public static MovementGrants Of(StoryState story) =>
        story.Has(BikeWaterFlag) ? MovementGrants.Swim : MovementGrants.None;
}
