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
    /// Kanto's first rows are plan 19 · K1's, for the tilesets of Pallet Town, Route 1 and Viridian City, judged from
    /// how each metatile is used on their layouts (where it stands, what stands above and below it, whether it
    /// blocks); a house's own metatiles are left out on purpose, since a GBA house is found at its door from the
    /// blocked ground nobody named (<c>WorldMapBuilder.BuildingsAtDoors</c>).
    /// </summary>
    public static readonly IReadOnlyDictionary<string, (int From, int To, TerrainCover Cover)[]> Ranges =
        new Dictionary<string, (int, int, TerrainCover)[]>(StringComparer.Ordinal)
        {
            ["gTileset_General"] = new (int, int, TerrainCover)[]
            {
                // The lawn: a plain metatile and the two-by-two pattern of short grass the routes are laid in
                (0x001, 0x001, TerrainCover.Grass), (0x008, 0x009, TerrainCover.Grass), (0x010, 0x011, TerrainCover.Grass),
                // Flowers in the grass (Pallet Town's fenced bed, Route 1's verges)
                (0x004, 0x004, TerrainCover.Flowers),
                // The signposts (MB_SIGNPOST): the sign itself is the event's, standing on lawn
                (0x002, 0x003, TerrainCover.Grass),
                // The forest every route is walled in by: trunks and canopies, two metatiles to a tree, row over row
                (0x014, 0x017, TerrainCover.Tree), (0x01C, 0x01F, TerrainCover.Tree), (0x024, 0x027, TerrainCover.Tree),
                // The brown mountains (Viridian City's west, Route 22, Mt. Moon's slopes): their edges, faces and fill
                (0x068, 0x07D, TerrainCover.Cliff), (0x0B2, 0x0B3, TerrainCover.Cliff), (0x0BA, 0x0BB, TerrainCover.Cliff),
                (0x1F2, 0x1F2, TerrainCover.Cliff),
                // Earth paths: the routes' three-by-three sets of edges and their inner corners, and the bank under a pond
                (0x0BD, 0x0BF, TerrainCover.Path), (0x0C3, 0x0C7, TerrainCover.Path), (0x0CB, 0x0CF, TerrainCover.Path),
                (0x0D3, 0x0D5, TerrainCover.Path), (0x0DB, 0x0DD, TerrainCover.Path), (0x0E3, 0x0E5, TerrainCover.Path),
                (0x102, 0x105, TerrainCover.Path), (0x133, 0x133, TerrainCover.Path),
                // Wooden fences: the posts, rails and corners round a yard or across the end of a route
                (0x0E6, 0x0FD, TerrainCover.Fence),
                // A town's roads (Viridian City's, Route 22's) with their edges and corners
                (0x162, 0x167, TerrainCover.Path), (0x16A, 0x177, TerrainCover.Path), (0x195, 0x196, TerrainCover.Path),
                (0x19D, 0x19E, TerrainCover.Path),
                // Plan 19 · K2: the wide sandy paths of Route 2, Route 22 and Viridian Forest's clearings, three by three
                // with their edges (the same sets as the earth paths above, a column to the west)
                (0x0D0, 0x0D2, TerrainCover.Path), (0x0D8, 0x0DA, TerrainCover.Path), (0x0E0, 0x0E2, TerrainCover.Path),
                // The bushes either side of the gate on Route 22, and beside the stairs of the routes' gates
                (0x005, 0x005, TerrainCover.Tree),
            },
            // Plan 19 · K2: Viridian Forest, a maze of trees three metatiles wide and two deep, row over row
            ["gTileset_ViridianForest"] = new (int, int, TerrainCover)[]
            {
                (0x288, 0x293, TerrainCover.Tree), (0x298, 0x29B, TerrainCover.Tree), (0x2A0, 0x2A5, TerrainCover.Tree),
                // The forest floor between them, and the trodden ground before its gates
                (0x281, 0x287, TerrainCover.Grass), (0x294, 0x297, TerrainCover.Path), (0x29C, 0x29F, TerrainCover.Path),
            },
            ["gTileset_PalletTown"] = new (int, int, TerrainCover)[]
            {
                // The town's sandy ground and the edges where it meets the lawn
                (0x281, 0x283, TerrainCover.Path), (0x285, 0x286, TerrainCover.Path), (0x288, 0x288, TerrainCover.Path),
                (0x28C, 0x28C, TerrainCover.Path), (0x28E, 0x290, TerrainCover.Path), (0x295, 0x297, TerrainCover.Path),
                (0x29E, 0x29F, TerrainCover.Path), (0x2A6, 0x2A7, TerrainCover.Path), (0x2AE, 0x2AF, TerrainCover.Path),
                (0x2B6, 0x2B6, TerrainCover.Path), (0x2BE, 0x2BF, TerrainCover.Path), (0x2C6, 0x2C7, TerrainCover.Path),
                (0x2CE, 0x2CE, TerrainCover.Path),
                // The white fences round the flower bed and before the lab
                (0x284, 0x284, TerrainCover.Fence), (0x287, 0x287, TerrainCover.Fence),
                // The signpost by each house's door (MB_SIGNPOST): the sign is the event's, standing on the town's ground
                (0x2AD, 0x2AD, TerrainCover.Path),
                // The trees that close Route 1's south end beside its way into Pallet Town
                (0x2AA, 0x2AB, TerrainCover.Tree), (0x2B2, 0x2B2, TerrainCover.Tree), (0x2BA, 0x2BA, TerrainCover.Tree),
            },
            ["gTileset_ViridianCity"] = new (int, int, TerrainCover)[]
            {
                // The posts where a ledge meets a house's yard (west of the Pokémon Center's row): lawn that blocks
                (0x286, 0x287, TerrainCover.Grass),
            },
        };

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
