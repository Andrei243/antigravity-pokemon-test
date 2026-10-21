namespace PokemonPlatinumEngine.Overworld;

/// <summary>
/// How someone is set (plan 11 · C7; style guide, "Characters", "Stances"): standing, sitting on a seat, sitting on
/// the ground, or lying on their back; and since C8 swimming, fishing with a rod out, and pulling on a bite.
/// </summary>
public enum Stance { Stand, Sit, SitGround, Lie, Swim, Fish, Tug }

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

    /// <summary>A swimmer's stroke, in seconds of the field's clock (plan 11 · C8): a ring spreads behind them at each.</summary>
    public const float StrokeSeconds = 1.2f;

    /// <summary>One pull on a bite, in seconds (plan 11 · C8): the tug's three frames go by in it, over and over.</summary>
    public const float TugSeconds = 0.45f;

    /// <summary>
    /// Whether someone standing on a tile swims there (plan 11 · C8): open deep water, with no deck under them (a
    /// <c>level</c>) and not wading in a pool.
    /// </summary>
    public static bool SwimsAt(Map map, NPC npc) =>
        npc.Level == null && npc.Wading == 0 && !npc.IsThing && !npc.IsPokemon
        && map.InBounds(npc.GridX, npc.GridY) && map.IsDeepWater(npc.GridX, npc.GridY) && !map.IsSolid(npc.GridX, npc.GridY);

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
