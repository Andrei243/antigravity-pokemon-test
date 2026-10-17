using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;

namespace MapImporter.Gba;

/// <summary>
/// What a GBA tile looks like (plan 18 · W6), in <c>Cover.cs</c>'s order: the behaviour where it shows; then the
/// metatile's label where <c>metatile_labels.h</c> names it; then a table of metatile ranges per tileset, written by
/// hand in a region's own session from the importer's behaviour picture and the user's memory of the place (plan
/// 01 · M4's decision 8: no picture of the original is kept). What nothing names is <see cref="TerrainCover.Unknown"/>,
/// and the report lists it per tileset with counts.
/// </summary>
public static class GbaCover
{
    /// <summary>
    /// The ranges written by hand, by the tileset's symbol: <c>["gTileset_PalletTown"] = { (0x280, 0x287, Paving) }</c>,
    /// the metatiles as the layouts number them (a secondary tileset's from <see cref="GbaMaps.PrimaryCount"/>).
    /// Empty until plan 19 (Kanto) and plan 21 (Hoenn) write their own tilesets'.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, (int From, int To, TerrainCover Cover)[]> Ranges =
        new Dictionary<string, (int, int, TerrainCover)[]>(StringComparer.Ordinal);

    /// <summary>Words in a label, first match wins, and the ground each says. A blocked tile with a building's word is a wall.</summary>
    private static readonly (string Word, TerrainCover Cover)[] Words =
    {
        ("Roof", TerrainCover.Building), ("Wall", TerrainCover.Building), ("Door", TerrainCover.Building),
        ("Window", TerrainCover.Building), ("PokeCenter", TerrainCover.Building), ("PokeMart", TerrainCover.Building),
        ("Building", TerrainCover.Building), ("House", TerrainCover.Building),
        ("TallGrass", TerrainCover.TallGrass), ("LongGrass", TerrainCover.TallGrass),
        ("Tree", TerrainCover.Tree), ("Bush", TerrainCover.Tree), ("Shrub", TerrainCover.Tree),
        ("Water", TerrainCover.Water), ("Sea", TerrainCover.Water), ("Ocean", TerrainCover.Water), ("Pond", TerrainCover.Water),
        ("Fence", TerrainCover.Fence), ("Railing", TerrainCover.Fence),
        ("Flower", TerrainCover.Flowers),
        ("Sand", TerrainCover.Sand), ("Ash", TerrainCover.Sand),
        ("Boulder", TerrainCover.Boulder), ("Rock", TerrainCover.Rock), ("Cliff", TerrainCover.Cliff), ("Mountain", TerrainCover.Cliff),
        ("Stairs", TerrainCover.Steps), ("Steps", TerrainCover.Steps),
        ("Bridge", TerrainCover.Bridge), ("Log", TerrainCover.Bridge),
        ("Path", TerrainCover.Path), ("Dirt", TerrainCover.Path), ("Mud", TerrainCover.Path),
        ("Floor", TerrainCover.Paving), ("Paving", TerrainCover.Paving), ("Pavement", TerrainCover.Paving), ("Tile", TerrainCover.Paving),
        ("Ice", TerrainCover.Ice), ("Snow", TerrainCover.Snow),
        ("Cave", TerrainCover.CaveFloor),
        ("Lamp", TerrainCover.Lamp),
        ("Grass", TerrainCover.Grass), ("Mowed", TerrainCover.Grass), ("Plain", TerrainCover.Grass),
    };

    /// <summary>The ground a behaviour shows whatever the metatile, or null where the metatile must say.</summary>
    public static TerrainCover? OfBehaviour(TileBehavior behaviour) => behaviour switch
    {
        TileBehavior.TallGrass or TileBehavior.VeryTallGrass => TerrainCover.TallGrass,
        TileBehavior.River or TileBehavior.Sea or TileBehavior.DeepWater or TileBehavior.Waterfall or TileBehavior.CurrentEast
            or TileBehavior.CurrentWest or TileBehavior.CurrentNorth or TileBehavior.CurrentSouth => TerrainCover.Water,
        TileBehavior.Puddle => TerrainCover.Puddle,
        TileBehavior.Sand => TerrainCover.Sand,
        TileBehavior.Ice or TileBehavior.ThinIce => TerrainCover.Ice,
        TileBehavior.CaveFloor => TerrainCover.CaveFloor,
        TileBehavior.Bridge or TileBehavior.BridgeOverWater => TerrainCover.Bridge,
        TileBehavior.Door => TerrainCover.Building,
        _ => null
    };

    public static TerrainCover OfLabel(string label)
    {
        foreach (var (word, cover) in Words)
            if (label.Contains(word, StringComparison.Ordinal)) return cover;
        return TerrainCover.Unknown;
    }

    /// <summary>A tile's look: by its behaviour, its label, the hand-written ranges, or <see cref="TerrainCover.Unknown"/>.</summary>
    public static TerrainCover Of(TileBehavior behaviour, string? label, string tileset, int metatile)
    {
        if (OfBehaviour(behaviour) is { } shown) return shown;
        if (label != null && OfLabel(label) is var named && named != TerrainCover.Unknown) return named;
        if (Ranges.TryGetValue(tileset, out var ranges))
            foreach (var (from, to, cover) in ranges)
                if (metatile >= from && metatile <= to) return cover;
        return TerrainCover.Unknown;
    }
}
