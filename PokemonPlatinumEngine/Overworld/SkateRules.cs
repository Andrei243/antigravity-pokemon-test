using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumEngine.Overworld;

/// <summary>
/// Where the roller skates may be put on (plan 23 · Z1; docs/mechanics/rulings.md, "Kalos's roller skates"): in a
/// region whose people skate (Kalos, its file's <c>skates</c>) once the story has given them. What a skater may do is
/// <see cref="FieldMovement"/>'s (<see cref="TravelMode.Skating"/>); the run button puts them on, as it changes the
/// Bicycle's gear. No drawing and no input.
/// </summary>
public static class SkateRules
{
    /// <summary>The story's flag for the skates had: Kalos's first chapter sets it as Mom hands them over.</summary>
    public const string Flag = "FLAG_KALOS_ROLLER_SKATES";

    public static bool Allowed(Region region, StoryState story) => region.Skates && story.Has(Flag);
}
