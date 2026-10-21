namespace PokemonPlatinumEngine.Overworld;

/// <summary>
/// How someone is set (plan 11 · C7; style guide, "Characters", "Stances"): standing, sitting on a seat, sitting on
/// the ground, or lying on their back.
/// </summary>
public enum Stance { Stand, Sit, SitGround, Lie }

/// <summary>
/// The furniture people take a stance on (plan 11 · C7): which stance a piece gives whoever stands on its tile, and how
/// high its seat or mattress is, in tiles. No drawing or input.
/// </summary>
public static class Seats
{
    /// <summary>The stance a piece of furniture gives whoever stands on it, or null for one that gives none.</summary>
    public static Stance? StanceOn(PropType type) => type switch
    {
        PropType.Bench or PropType.Chair or PropType.Sofa => Stance.Sit,
        PropType.Cushion => Stance.SitGround,
        PropType.Bed => Stance.Lie,
        _ => null
    };

    /// <summary>How high a seat or a mattress is, in tiles (the props' own texels, 32 to a tile; style guide, "Stances").</summary>
    public static float HeightOf(PropType type) => type switch
    {
        PropType.Chair => 14f / 32f,
        PropType.Bed => 14f / 32f,
        PropType.Sofa or PropType.Bench => 13f / 32f,
        PropType.Cushion => 5f / 32f,
        _ => 0f
    };

    /// <summary>
    /// How far a sitter's card stands from their tile's middle along the way they face, in tiles, so their legs hang
    /// in front of the seat's edge: a little past where each piece's seat ends (a sofa's reaches furthest).
    /// </summary>
    public static float ForwardOn(PropType? type) => type switch
    {
        PropType.Sofa => 0.45f,
        PropType.Cushion => 0.36f,
        _ => 0.28f
    };
}
