using System;
using System.Collections.Generic;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Overworld;

public enum TileType
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
    CaveFloor
}

/// <summary>Which kind of tree fills a map's forests: Sinnoh's layered pines or round broadleaf trees.</summary>
public enum TreeStyle
{
    Round,
    Pine
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
    Clapboard
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
