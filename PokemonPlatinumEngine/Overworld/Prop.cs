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
    /// <summary>A low wall of stone: what a town of brick and stone has where a village has a fence. It joins up like one.</summary>
    LowWall,
    LampPost,
    Mailbox,
    Planter,

    // What stands in Sinnoh's towns besides buildings, placed by the models of the imported world (Data/WorldModels).
    // Each covers the tiles of its model's box; the world's own data says which of them block the way.
    Fountain,
    /// <summary>A ship at its pier: lying north–south or east–west as its box is longer.</summary>
    Boat,
    WindTurbine,
    Statue,
    /// <summary>A broad-leaved tree standing alone, that wild Pokémon come to when it is slathered with honey.</summary>
    HoneyTree,
    Crates,
    CoalHeap,
    Hedge,
    /// <summary>A stone pillar as tall as its model (<see cref="Prop.Height"/>).</summary>
    Column,
    Topiary,
    /// <summary>A small pile of worked stones: the Hallowed Tower, a stone tablet.</summary>
    Cairn,
    Billboard,
    /// <summary>A mass of rock as large as its box.</summary>
    Outcrop,
    /// <summary>A lattice mast as tall as its model.</summary>
    Mast,
    /// <summary>Steel drums standing together: what a mine keeps its oil in.</summary>
    Drums,
    /// <summary>
    /// A raised conveyor belt, a tile wide, running north and south (deeper than wide) or east and west: on a
    /// steel pier wherever the tile under it is blocked, and a span one walks beneath wherever it is open.
    /// </summary>
    Conveyor,
    /// <summary>
    /// What carries a conveyor over the open ground: two steel legs with a beam across (three tiles one way,
    /// the middle one open), or a single pier when it covers one tile.
    /// </summary>
    Gantry,

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

    /// <summary>For a prop placed by a model of the world: how tall the model stands, in tiles. 0 otherwise.</summary>
    public float Height { get; init; }

    /// <summary>For a prop placed by a model of the world: the model's short name, which picks among looks of one type.</summary>
    public string Model { get; init; } = "";

    /// <summary>Whether it blocks every tile it covers. A conveyor and its gantry stand over open ground: the world says which of their tiles block.</summary>
    public bool IsSolid => Type < PropType.Rug && Type is not (PropType.Conveyor or PropType.Gantry);

    /// <summary>Reception and shop counters: you can talk to whoever stands behind them.</summary>
    public bool IsCounter => Type is PropType.Counter or PropType.LabDesk;

    public bool Covers(int x, int y) => x >= X && x < X + Width && y >= Y && y < Y + Depth;
}
