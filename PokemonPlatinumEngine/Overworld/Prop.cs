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
    /// <summary>A bed two tiles long, its head against the north wall (plan 02 · S4).</summary>
    Bed,
    /// <summary>A desk with a computer on it (the player's own, in the bedroom).</summary>
    Computer,
    /// <summary>
    /// A flight of stairs at the side of a room, two tiles each way, climbing west to the floor above, as the Team
    /// Galactic Eterna Building's do (plan 02 · S6): its warp is the tile east of its foot.
    /// </summary>
    SideStairsUp,
    /// <summary>A stairwell at the side of a room going down eastward to the floor below, behind a rail: its warp is the tile west of its head.</summary>
    SideStairsDown,
    /// <summary>The same flight turned round, climbing east: its warp is the tile west of its foot (the Lost Tower's, plan 02 · S7).</summary>
    SideStairsUpEast,
    /// <summary>The same stairwell turned round, going down westward: its warp is the tile east of its head (the Lost Tower's).</summary>
    SideStairsDownWest,
    /// <summary>A headstone, a tile wide: the Lost Tower's small graves (plan 02 · S7).</summary>
    Headstone,
    /// <summary>A tomb, two tiles each way: the Lost Tower's large graves.</summary>
    Tomb,
    /// <summary>A bicycle on its stand, two tiles wide: what a cycle shop shows (Eterna City's, plan 02 · S6).</summary>
    Bicycle,
    /// <summary>A waste bin.</summary>
    TrashCan,
    /// <summary>A tall wooden wardrobe against the wall: where the player changes clothes at home (plan 11 · C10).</summary>
    Wardrobe,
    /// <summary>A rail of clothes on hangers, as wide as its tiles: what a boutique shows (plan 11 · C10).</summary>
    ClothesRack,

    // Outdoors: a rock too big to step over, on land or standing in water
    Boulder,

    // Obstacles a field move clears: a small tree for Cut, a cracked rock for Rock Smash, a round boulder for
    // Strength. Since plan 02 · S2 they are objects of the map, as the original's are (NPC.Obstacle; Map.AddObstacle),
    // drawn as cards and cleared or pushed by their scripts; the kinds stay here for their art.
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
    /// <summary>A raised bed of flowers in a kerb of pale stone (Amity Square).</summary>
    FlowerBed,
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

    // The east and the sea (plan 01 · M7)
    /// <summary>A heap of iron ore (Iron Island).</summary>
    OreHeap,
    /// <summary>The steel tower over a mine's shaft, with its winding wheel, as tall as its model (Iron Island).</summary>
    Headframe,
    /// <summary>The steel platform at the foot of a lift.</summary>
    LiftBase,
    /// <summary>A leaf of a drawbridge over the planks the ground shows: trusses along the deck and a portal at its bank (Canalave City).</summary>
    Drawbridge,
    /// <summary>The Great Marsh's little tram on its rails.</summary>
    Tram,
    /// <summary>A coin viewer on a post.</summary>
    Binoculars,
    /// <summary>An open shelter on four posts under a roof (the Hotel Grand Lake).</summary>
    Pavilion,

    // The north and the end (plan 01 · M8)
    /// <summary>A stack of rock standing in the sea, the water lapping round its foot (Sunyshore City).</summary>
    SeaStack,
    /// <summary>A low drift of snow banked against a wall (Snowpoint City's harbour).</summary>
    Snowdrift,
    /// <summary>A tear in the air taller than a person, edged in light: where Spear Pillar is torn open (plan 02 · S12).</summary>
    Rift,
    /// <summary>The dark a rift casts on the ground below it: the way down into the Distortion World.</summary>
    RiftShadow,

    // The Gyms' puzzles (plan 01 · M9): things of the map, like the obstacles, drawn as cards (GymArt)
    /// <summary>The Veilstone Gym's punching bag, which a kick sends along its track.</summary>
    PunchingBag,
    /// <summary>A stack of tyres in the Veilstone Gym's way, knocked down by a punching bag.</summary>
    TireStack,
    /// <summary>A post in the way out of the Hearthome Gym's Leader's room, gone once she is beaten.</summary>
    Bollard,

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
