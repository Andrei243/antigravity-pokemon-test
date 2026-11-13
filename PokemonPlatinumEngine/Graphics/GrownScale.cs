using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// Where the world round the people is built to a grown body (style guide, "Scale in a grown region"): the buildings'
/// doors, storeys and windows (<see cref="BuildingStyle.Grown"/>) and the festivals' things. A region whose file says its
/// kit is grown has it, but for Kalos, whose towns were shot and approved at the chibi kit's numbers before its people
/// grew: a session that redraws Kalos's towns to the grown numbers takes it off <see cref="NotYet"/>. No GPU calls.
/// </summary>
internal static class GrownScale
{
    /// <summary>Grown regions whose buildings and festival things keep the chibi kit's numbers for now.</summary>
    public static readonly string[] NotYet = { RegionDatabase.Kalos };

    /// <summary>Whether a region's buildings and festival things are built to a grown body.</summary>
    public static bool Of(Region? region) =>
        region != null && region.Kit == CharacterKit.Grown && System.Array.IndexOf(NotYet, region.Id) < 0;

    /// <summary>Whether a map's buildings and festival things are built to a grown body: the region it belongs to.</summary>
    public static bool Of(Map map) => Of(RegionDatabase.RegionOfMap(map.Name));
}
