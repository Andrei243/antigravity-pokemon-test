using PokemonPlatinumEngine.Battle.Sim;

namespace PokemonPlatinumEngine.OpenWorld;

/// <summary>
/// The ground a battle begun in an open map is fought on (plan 32 · OW7; plan 32, "Battles"): the cover under the
/// player's capsule mapped to the original's <see cref="BattleTerrain"/>, as <c>GameEngine.TerrainAt</c> maps the
/// grid's tile, so the Dive and Dusk Balls, Camouflage, Nature Power and Secret Power and the battle's stage go by it.
/// Deep grass is the tall grass's; the lighter grasses, the highland and a path are the plain's; the two rocks are a
/// mountain's; the cave's floor a cave's; sand, snow and the marsh's mud their own; and in water (swimming, or standing
/// where the water is over the feet) the water's. GPU-free.
/// </summary>
public static class OpenBattleGround
{
    /// <summary>The terrain of a cover, or the water's for a body in water. Null cover (a building's top, a solid) is a building's floor.</summary>
    public static BattleTerrain TerrainOf(OpenCover? cover, bool inWater) => inWater ? BattleTerrain.Water : cover switch
    {
        OpenCover.GrassDeep => BattleTerrain.Grass,
        OpenCover.Grass or OpenCover.GrassLight or OpenCover.Highland or OpenCover.Path => BattleTerrain.Plain,
        OpenCover.Rock or OpenCover.RockDark => BattleTerrain.Mountain,
        OpenCover.CaveFloor => BattleTerrain.Cave,
        OpenCover.Sand => BattleTerrain.Sand,
        OpenCover.Snow => BattleTerrain.Snow,
        OpenCover.Mud => BattleTerrain.GreatMarsh,
        null => BattleTerrain.Building,
        _ => BattleTerrain.Plain
    };

    /// <summary>The terrain under a body standing at <paramref name="feet"/> on a map's ground, swimming or not.</summary>
    public static BattleTerrain Under(OpenMap map, OpenPos feet, bool swimming)
    {
        var ground = map.Ground;
        double x = feet.MetresX, z = feet.MetresZ, y = feet.MetresY;
        bool wet = swimming || ground.WaterAt(x, z) is { } water && water > y + 0.05;
        return TerrainOf(ground.CoverUnder(x, z, y + 0.6, y - 1.0), wet);
    }
}
