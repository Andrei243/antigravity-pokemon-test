using System;
using System.Collections.Generic;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Overworld;

public enum TileType : byte
{
    Grass,
    FlowerGrass,
    TallGrass,
    Path,
    Water,
    LedgeDown,
    Tree,
    TreeTrunk,
    RoofRed,
    RoofBlue,
    Wall,
    Door,
    Floor,
    Signpost,
    PC,
    RoofGreen,

    // Ground kinds beyond lawn and path (drawn by PixelGround; plan 01 places them)
    Sand,
    Dirt,
    Snow,
    CaveFloor,

    // Terrain features (plan 01 · M3). A tile's type is what it looks like; what it does is its TileBehavior,
    // which on a hand-made map follows from the type (Map.BehaviourAt)
    /// <summary>A ledge hopped over westward.</summary>
    LedgeLeft,
    /// <summary>A ledge hopped over eastward.</summary>
    LedgeRight,
    /// <summary>Bare rock underfoot: the tops of cliffs and crags.</summary>
    Rock,
    Ice,
    /// <summary>The deck of a bridge or a boardwalk.</summary>
    Planks,
    Stairs,
    /// <summary>Marsh mud.</summary>
    Marsh,

    // The made ground of towns (plan 01 · M4)
    /// <summary>Stone slabs: the streets and squares of cities.</summary>
    Paving,
    /// <summary>The deck of Sunyshore's raised walkways: steel, set with solar panels.</summary>
    Walkway,

    // Caves (plan 01 · M5)
    /// <summary>The rock a cave is cut into: nobody walks on it, and it stands up from the floor as the cave's wall.</summary>
    CaveWall,
    /// <summary>The dark inside the mouth of a cave, as the open country shows it: a hollow at the foot of the rock.</summary>
    CaveMouth,

    // Forests (plan 01 · M6)
    /// <summary>The dark under the trees where a forest is entered (Eterna Forest, Floaroma Meadow).</summary>
    ForestMouth,

    // The east (plan 01 · M7)
    /// <summary>A puddle: shallow water walked through, which mirrors whoever stands in it (Route 212).</summary>
    Puddle,

    // The Distortion World (plan 01 · M8)
    /// <summary>Nothing at all: the drop the Distortion World's islands float over. No ground is drawn there and nobody walks into it.</summary>
    Void,
    /// <summary>The grey-violet stone of the Distortion World's islands.</summary>
    DistortionGround,
    /// <summary>
    /// A pale slab set into an island, where the Distortion World carries the player somewhere else: a slab that
    /// rises or sinks to another floor, a stone that ferries across the drop, the foot of a wall walked sideways.
    /// </summary>
    DistortionSlab
}

/// <summary>What a map of the imported world is where nothing else is said (plan 01 · M5).</summary>
public enum MapSetting
{
    /// <summary>Open country under the sky: forest where no chunk says otherwise, lit by the time of day.</summary>
    Outdoors,
    /// <summary>The inside of a cave: rock where no chunk says otherwise, walls standing up from the floor, a light of its own.</summary>
    Cave,
    /// <summary>
    /// The Distortion World (plan 01 · M8): islands floating over nothing. Where nothing stands there is no ground at
    /// all, only the drop beneath the islands, which nobody walks into; the islands have a light of their own.
    /// </summary>
    Void
}

/// <summary>
/// The field's cameras, as Platinum's map headers name them (<c>sCameraTypes</c> in the original's
/// <c>src/overlay005/field_camera.c</c>): how far away, how steeply and how widely the field is looked at.
/// </summary>
public enum FieldCamera
{
    /// <summary>666.9 units away, pitched 59.05°, 16.18° of view: the overworld.</summary>
    Default,
    /// <summary>574.6 units away, pitched 63.26°, 19.0° of view: caves.</summary>
    Cave,
    /// <summary>515.5 units away, pitched 54.66°, 20.92° of view: Floaroma Meadow, Eterna Forest, Amity Square.</summary>
    ZoomedIn,
    /// <summary>866.6 units away, pitched 73.11°, 12.67° of view: the outside of Mt. Coronet's south face.</summary>
    CoronetSouth
}

/// <summary>Which kind of tree fills a map's forests: Sinnoh's layered pines or round broadleaf trees.</summary>
public enum TreeStyle
{
    Round,
    Pine
}

/// <summary>
/// The stage a battle is fought on (plan 04 · G8). Outdoors the ground under the player picks it (water, sand,
/// snow, a cave floor); a map can name its own (a forest, a gym, the League's rooms).
/// </summary>
public enum BattleArena
{
    Grass,
    Forest,
    Cave,
    Water,
    Snow,
    Sand,
    Indoors,
    /// <summary>A gym hall, themed on its leader's type.</summary>
    Gym,
    /// <summary>A room of the Pokémon League: an Elite Four member's type, or the Champion's room.</summary>
    League
}

/// <summary>How a town's houses are built (style guide, "Buildings"); public buildings look the same everywhere.</summary>
public enum Architecture
{
    /// <summary>Plank walls with timber posts (Twinleaf).</summary>
    Timber,
    /// <summary>Plaster over a stone base (Sandgem).</summary>
    Plaster,
    /// <summary>Brick and panel blocks with flat roofs (Jubilife).</summary>
    City,
    /// <summary>White clapboard (Pallet).</summary>
    Clapboard,

    // The rest of Sinnoh (plan 01 · M4): each follows the models of its town
    /// <summary>Brick under rust-brown roofs: a mining town (Oreburgh).</summary>
    Brick,
    /// <summary>White boards, rose roofs and flowers at every window (Floaroma).</summary>
    Cottage,
    /// <summary>Plaster between dark beams under steep moss-green roofs: old towns (Eterna, Celestic).</summary>
    HalfTimber,
    /// <summary>Warm plaster with shutters under hipped plum roofs (Hearthome).</summary>
    Townhouse,
    /// <summary>Dark boards under straw-coloured roofs: farmhouses (Solaceon).</summary>
    Farm,
    /// <summary>Cut stone under grey roofs (Veilstone).</summary>
    Stone,
    /// <summary>Boards under hipped reed-green roofs (Pastoria).</summary>
    Marsh,
    /// <summary>Brick under slate-blue roofs: a port (Canalave).</summary>
    Harbour,
    /// <summary>Logs under roofs deep in snow (Snowpoint).</summary>
    Snow,
    /// <summary>White walls under flat or orange roofs with solar panels (Sunyshore).</summary>
    Seaside,
    /// <summary>White boards under hipped turquoise roofs: the lakeside hotel and the Battle Zone's villas.</summary>
    Resort
}

/// <summary>Visual theme for indoor maps; <see cref="None"/> means the map is outdoors.</summary>
public enum InteriorStyle
{
    None,
    House,
    PokemonCenter,
    PokeMart,
    Lab
}

public class Warp
{
    public int SourceX { get; set; }
    public int SourceY { get; set; }
    public string TargetMap { get; set; } = string.Empty;
    public int TargetX { get; set; }
    public int TargetY { get; set; }
    public Direction TargetFacing { get; set; } = Direction.Down;

    /// <summary>
    /// A way onto the Cycling Road (plan 02 · S2): only a rider is let through, and comes out unable to get off
    /// the Bicycle until the next warp.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault)]
    public bool CyclistsOnly { get; set; }

    /// <summary>
    /// A door locked until the story sets this flag (plan 02 · S6: the Valley Windworks' door, opened with the Works
    /// Key): its tile stands in the way until then, and can be read. Null for a door that is always open.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string? OpenedBy { get; set; }
}

/// <summary>
/// An item nobody can see, found by looking at its tile (plan 02): the item, how many, and the story flag set once
/// it has been found, after which nothing is there. <see cref="Range"/> is from how many tiles away the original's
/// Dowsing Machine notices it.
/// </summary>
public sealed record HiddenItem(string Item, int Count, string Flag, int Range = 0);

/// <summary>
/// Tiles that start a script when the player steps onto one of them (plan 02 · S1): a rectangle, the script, and
/// the state of the story it waits for. As in the original, a trigger goes by a variable: it fires only while the
/// variable has the value, and the script it starts moves the variable on so that it doesn't fire again.
/// </summary>
public sealed class StepTrigger
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; } = 1;
    public int Depth { get; set; } = 1;

    /// <summary>The script's name, looked for in <see cref="ScriptFile"/> first and among the common ones second.</summary>
    public string Script { get; set; } = string.Empty;

    /// <summary>The file of the place the trigger belongs to; null on a hand-made map, whose name it is.</summary>
    public string? ScriptFile { get; set; }

    /// <summary>The variable it goes by; null for a trigger that fires every time.</summary>
    public string? Variable { get; set; }
    public int Value { get; set; }

    public bool Covers(int x, int y) => x >= X && x < X + Width && y >= Y && y < Y + Depth;
}

/// <summary>
/// A row of a place's table of wild Pokémon: a species, its levels and how often it comes up. What a step has met
/// (<see cref="Map.RollWildEncounter"/>) is a row of its own with the level decided, and the gender and nature
/// the lead's ability chose for it, if it chose any (<see cref="WildEncounterRules"/>).
/// </summary>
public class WildEncounterEntry
{
    public string SpeciesName { get; set; } = "Bidoof";
    public int MinLevel { get; set; } = 2;
    public int MaxLevel { get; set; } = 4;
    public int Weight { get; set; } = 10;

    /// <summary>The gender Cute Charm chose for the Pokémon met; never in a table.</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public Gender? Gender { get; set; }

    /// <summary>The nature Synchronize chose for the Pokémon met; never in a table.</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public Nature? Nature { get; set; }
}
