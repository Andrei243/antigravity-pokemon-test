namespace PokemonPlatinumEngine.Overworld;

/// <summary>
/// What a room's inner walls are built as (plan 20 · J15, part s; style guide, "Rooms", "Partitions of their own"): the
/// room's own wall cut to the wainscot, or a look of the room's own where its blocked tiles stand for something else.
/// Only drawing reads it: what blocks is the map's.
/// </summary>
public enum PartitionLook
{
    /// <summary>The room's own wall cut to the wainscot (every room the catalogue names nothing for).</summary>
    Wall,
    /// <summary>Clipped hedges in planters with flowers in them (the Goldenrod Gym's maze).</summary>
    Hedge,
    /// <summary>Shop and office counters of pale wood under a laminate top (the tunnel's stalls, Buena's floor).</summary>
    Counter,
    /// <summary>Riveted steel bulkheads painted ship's grey (the S.S. Aqua's decks, the tunnel's service ways).</summary>
    Bulkhead,
    /// <summary>Rough boulders of grey stone (the Cianwood Gym's pool and its rocks).</summary>
    Rock,
    /// <summary>Blocks of pale blue ice (the Mahogany Gym's halls).</summary>
    Ice,
    /// <summary>Square pillars of pale stone under a capital (the Glitter Lighthouse's floors).</summary>
    Pillar,
}
