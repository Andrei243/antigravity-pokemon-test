namespace PokemonPlatinumEngine.OpenWorld;

/// <summary>
/// What covers the open world's ground at a vertex (plan 32, "The ground is one signed-distance field per chunk"),
/// as <c>TerrainCover</c> names the grid's: decided by the baker's rules (slope, height, under rock, a road, the
/// water's edge) and its paint brushes, and kept in a baked chunk a byte a vertex. New covers go at the end: a baked
/// chunk keeps the number. GPU-free.
/// </summary>
public enum OpenCover : byte
{
    Grass,
    GrassLight,
    GrassDeep,
    Path,
    Rock,
    RockDark,
    CaveFloor,
    Highland,
    Sand,
    Snow,
    Mud
}

/// <summary>Which covers are one family (plan 32 · OW2): what the far levels may blend across as they are thinned.</summary>
public static class OpenCovers
{
    /// <summary>The family a cover belongs to: the grasses and the highland are one, the two rocks another, every other cover its own.</summary>
    public static int FamilyOf(OpenCover cover) => cover switch
    {
        OpenCover.Grass or OpenCover.GrassLight or OpenCover.GrassDeep or OpenCover.Highland => 0,
        OpenCover.Rock or OpenCover.RockDark => 1,
        _ => 2 + (int)cover
    };
}
