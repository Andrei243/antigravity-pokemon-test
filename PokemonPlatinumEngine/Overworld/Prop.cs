namespace PokemonPlatinumEngine.Overworld;

public enum PropType
{
    // Furniture: blocks movement
    Table,
    Chair,
    Sofa,
    Bookshelf,
    Television,
    Plant,
    Fridge,
    KitchenCounter,
    Stove,
    Stairs,
    Counter,
    HealingMachine,
    Bench,
    StoreShelf,
    LabDesk,
    LabMachine,

    // Outdoors: a rock too big to step over, on land or standing in water
    Boulder,

    // Obstacles a field move clears (plan 02 · S2 makes them give way; until then they only stand in the way):
    // a small tree for Cut, a cracked rock for Rock Smash, a round boulder for Strength
    CutTree,
    CrackedRock,
    StrengthBoulder,

    // Street furniture. A fence covers a run of tiles and joins up with the fences beside it; outdoors a Bench is a park bench
    Fence,
    LampPost,
    Mailbox,
    Planter,

    // Decoration: floor rugs and things hung on the back wall, never solid
    Rug,
    Window,
    Painting,
    Clock,
    WallEmblem
}

/// <summary>A piece of furniture or decoration placed in a room, covering a rectangle of tiles.</summary>
public sealed class Prop
{
    public PropType Type { get; init; }
    public int X { get; init; }
    public int Y { get; init; }
    public int Width { get; init; } = 1;
    public int Depth { get; init; } = 1;

    public bool IsSolid => Type < PropType.Rug;

    /// <summary>Reception and shop counters: you can talk to whoever stands behind them.</summary>
    public bool IsCounter => Type is PropType.Counter or PropType.LabDesk;

    public bool Covers(int x, int y) => x >= X && x < X + Width && y >= Y && y < Y + Depth;
}
