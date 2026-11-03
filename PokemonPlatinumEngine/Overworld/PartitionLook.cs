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
    /// <summary>A table of dark wood on stout legs (the S.S. Aqua's mess; plan 20 · J15, part u).</summary>
    Table,
    /// <summary>Stacked wooden crates with slats and stencils (the Department Store's basement, the stockrooms).</summary>
    Crate,
    /// <summary>Control desks of cream steel with a dark panel of lit buttons and dials (the radio towers, the Power Plant).</summary>
    Console,
    /// <summary>Glass display cases on a wooden plinth (the Pewter Museum's halls).</summary>
    Case,
    /// <summary>Screens of dark timber over paper panels (Ecruteak's halls by the Bell Tower and the dance theatre).</summary>
    Lattice,
    /// <summary>Rails of weathered wood between posts, straw at their foot (the Moomoo Farm's stable).</summary>
    Fence,
}

/// <summary>A rectangle of a room's tiles whose inner walls have a look of their own (plan 20 · J15, part u).</summary>
public readonly record struct PartitionPatch(int X, int Y, int Width, int Height, PartitionLook Look)
{
    public bool Covers(int x, int y) => x >= X && x < X + Width && y >= Y && y < Y + Height;
}
