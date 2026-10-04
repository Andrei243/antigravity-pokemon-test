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
    Walkway
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
}

public class WildEncounterEntry
{
    public string SpeciesName { get; set; } = "Bidoof";
    public int MinLevel { get; set; } = 2;
    public int MaxLevel { get; set; } = 4;
    public int Weight { get; set; } = 10;
}
