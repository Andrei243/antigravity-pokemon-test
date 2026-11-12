using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Overworld;

namespace ContentCheck;

/// <summary>
/// The gathering spots of an overlay (plan 39 · CR2): an overlay's <c>npcs</c> entry whose <c>npcType</c> is one of the
/// five kinds. Each is shown by the crafting kit's flag (nothing of plan 39 is seen before the kit), stands on open ground
/// that is no water and no path, and is faced from an open tile beside it that nobody else stands on.
/// </summary>
public static class GatherSpots
{
    private static readonly string[] Types = { NPC.GatherRockType, NPC.GatherWoodType, NPC.GatherPlantType, NPC.GatherShoreType, NPC.GatherSnowType };

    public static bool IsSpot(string? npcType) => npcType != null && Types.Contains(npcType);

    /// <summary>What is wrong with a spot at a tile of a map, in words; nothing for a spot that is right.</summary>
    public static IEnumerable<string> Why(Map? map, int x, int y, string? shownBy)
    {
        if (shownBy != GameEngine.CraftingKitFlag) yield return $"a gathering spot is shown by {GameEngine.CraftingKitFlag}, not by {shownBy ?? "nothing"}";
        if (map == null || !map.InBounds(x, y)) yield break;
        if (map.IsSolid(x, y) || map.GetGroundTile(x, y) == TileType.Water) yield return "a gathering spot stands on blocked ground or water";
        if (map.GetGroundTile(x, y) is TileType.Path or TileType.Paving or TileType.Planks or TileType.Stairs) yield return "a gathering spot stands on a path";
        bool faced = false;
        foreach (var (dx, dy) in new[] { (0, 1), (0, -1), (1, 0), (-1, 0) })
        {
            int nx = x + dx, ny = y + dy;
            if (!map.InBounds(nx, ny) || map.IsSolid(nx, ny) || map.GetGroundTile(nx, ny) == TileType.Water) continue;
            if (map.Everyone.Any(n => n.GridX == nx && n.GridY == ny)) continue;
            faced = true;
        }
        if (!faced) yield return "a gathering spot has no open tile beside it to be faced from";
    }
}
