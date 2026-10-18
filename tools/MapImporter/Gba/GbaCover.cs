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
                // Plan 19 · K4: a town's road, three by three with its edges and inner corners (Pewter City's streets,
                // which K3 left reading as lawn, and Cerulean City's)
                (0x1B8, 0x1BC, TerrainCover.Path), (0x1C0, 0x1C4, TerrainCover.Path), (0x1C8, 0x1CA, TerrainCover.Path),
                // Ledges in the grass, hopped over south, west or east (MB_JUMP_*), with the posts at their ends
                (0x085, 0x087, TerrainCover.Grass), (0x08D, 0x08E, TerrainCover.Grass), (0x097, 0x097, TerrainCover.Grass),
                (0x0B0, 0x0B1, TerrainCover.Grass), (0x0C1, 0x0C1, TerrainCover.Grass), (0x0C8, 0x0C8, TerrainCover.Grass),
                // The mountains' own ledges on the paths round Mt. Moon and Route 24
                (0x08F, 0x08F, TerrainCover.Path), (0x096, 0x096, TerrainCover.Path), (0x0B8, 0x0B9, TerrainCover.Path),
                // The brown mountains' edges round Mt. Moon, Route 4 and Route 25 (the ones K1 left for later)
                (0x080, 0x082, TerrainCover.Cliff), (0x088, 0x088, TerrainCover.Cliff), (0x08A, 0x08A, TerrainCover.Cliff),
                (0x090, 0x090, TerrainCover.Cliff), (0x092, 0x092, TerrainCover.Cliff), (0x09E, 0x09F, TerrainCover.Cliff),
                (0x0A1, 0x0A1, TerrainCover.Cliff), (0x0A8, 0x0A8, TerrainCover.Cliff), (0x0B4, 0x0B5, TerrainCover.Cliff),
                (0x0B7, 0x0B7, TerrainCover.Cliff), (0x1F1, 0x1F1, TerrainCover.Cliff), (0x1F3, 0x1F4, TerrainCover.Cliff),
                (0x20F, 0x20F, TerrainCover.Cliff),
                // The dark mouth of a cave in a mountain's face (MB_CAVE_DOOR: Mt. Moon's two ways in)
                (0x0A9, 0x0A9, TerrainCover.CaveMouth),
                // A small stone in the grass, walked over, and the worn patches in a path
                (0x0C2, 0x0C2, TerrainCover.Grass), (0x0FE, 0x0FF, TerrainCover.Path), (0x106, 0x107, TerrainCover.Path),
                // The signposts on lawn (MB_SIGNPOST)
                (0x168, 0x169, TerrainCover.Grass),
                // Low walls of stone along Cerulean City's river and across Routes 24 and 25, and the row of bushes on
                // Cerulean's south edge
                (0x0DF, 0x0DF, TerrainCover.Fence), (0x135, 0x135, TerrainCover.Fence), (0x13D, 0x13F, TerrainCover.Fence),
                (0x146, 0x146, TerrainCover.Fence), (0x14D, 0x14D, TerrainCover.Fence), (0x157, 0x157, TerrainCover.Fence),
                (0x0D6, 0x0D7, TerrainCover.Tree),
            },
            // Plan 19 · K4: Cerulean City's own: the bridges north of the town and Nugget Bridge, their railings, and the
            // rocks along Route 25's shore
            ["gTileset_CeruleanCity"] = new (int, int, TerrainCover)[]
            {
                (0x2C6, 0x2C6, TerrainCover.Bridge), (0x2CE, 0x2CE, TerrainCover.Bridge), (0x2DD, 0x2DE, TerrainCover.Bridge),
                (0x2E5, 0x2E6, TerrainCover.Bridge), (0x2ED, 0x2ED, TerrainCover.Bridge),
                (0x2C5, 0x2C5, TerrainCover.Fence), (0x2C7, 0x2C7, TerrainCover.Fence), (0x2CD, 0x2CD, TerrainCover.Fence),
                (0x2CF, 0x2CF, TerrainCover.Fence), (0x2D5, 0x2D5, TerrainCover.Fence), (0x2D6, 0x2D6, TerrainCover.Bridge),
                (0x2D7, 0x2D7, TerrainCover.Fence), (0x2EE, 0x2EF, TerrainCover.Fence),
                (0x2DF, 0x2DF, TerrainCover.Fence),
                (0x2E7, 0x2E7, TerrainCover.Cliff),
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

    /// <summary>
    /// Emerald's rows (plan 21 · H1), apart from FireRed's: the two games both call their primary tileset
    /// <c>gTileset_General</c>, with other metatiles in it. They are asked before a metatile's label, since Emerald's
    /// labels name a lawn under a tree's overhang <c>Grass_TreeLeft</c> and a mountain's foot <c>RockWall_GrassBase</c>,
    /// which the words would take for a tree and a wall. Hoenn's first rows are for the tilesets of Littleroot Town,
    /// Routes 101 to 103, Oldale Town and Petalburg City; a house's own metatiles are left out, as Kanto's are.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, (int From, int To, TerrainCover Cover)[]> HoennRanges =
        new Dictionary<string, (int, int, TerrainCover)[]>(StringComparer.Ordinal)
        {
            ["gTileset_General"] = new (int, int, TerrainCover)[]
            {
                // The lawn, its edge along a pond's south bank, and the signposts on it (the sign is the event's)
                (0x001, 0x003, TerrainCover.Grass),
                // Flowers in the grass
                (0x004, 0x004, TerrainCover.Flowers),
                // A lawn under a tree's overhang, beside a tree or a tall grass's tree
                (0x00E, 0x00F, TerrainCover.Grass), (0x1CE, 0x1CF, TerrainCover.Grass),
                // Small trees and bushes standing in the grass (Route 103's east bank)
                (0x016, 0x017, TerrainCover.Tree), (0x0C6, 0x0C7, TerrainCover.Tree),
                // The forest every route is walled in by: four metatiles a tree, row over row, beside the earth paths
                (0x1D4, 0x1D7, TerrainCover.Tree), (0x1DC, 0x1DF, TerrainCover.Tree), (0x1E4, 0x1E7, TerrainCover.Tree),
                // Earth paths: three by three with their edges (MB_NORMAL beside the trees in the same rows)
                (0x1D0, 0x1D3, TerrainCover.Path), (0x1D8, 0x1DB, TerrainCover.Path), (0x1E0, 0x1E3, TerrainCover.Path),
                // Ledges in the grass, hopped over south, east or south-east (MB_JUMP_*)
                (0x06E, 0x06F, TerrainCover.Grass), (0x086, 0x087, TerrainCover.Grass), (0x08D, 0x08E, TerrainCover.Grass),
                (0x0D4, 0x0D6, TerrainCover.Grass), (0x0FF, 0x0FF, TerrainCover.Grass),
                // The rocky mountains' faces, edges and foot (Route 103's north rim)
                (0x070, 0x07F, TerrainCover.Cliff), (0x089, 0x089, TerrainCover.Cliff), (0x091, 0x091, TerrainCover.Cliff),
                (0x0A9, 0x0A9, TerrainCover.Cliff), (0x0CE, 0x0CE, TerrainCover.Cliff),
                // A town's paved road (Petalburg City's, Route 103's south bank), three by three with its edges
                (0x118, 0x11A, TerrainCover.Path), (0x120, 0x122, TerrainCover.Path), (0x128, 0x12A, TerrainCover.Path),
                (0x130, 0x131, TerrainCover.Path),
                // The soil round a berry patch
                (0x113, 0x115, TerrainCover.Path),
            },
            ["gTileset_Petalburg"] = new (int, int, TerrainCover)[]
            {
                // The doormat before a house's door, and the earth where the paths meet the towns' edges
                (0x201, 0x201, TerrainCover.Path), (0x206, 0x207, TerrainCover.Path), (0x271, 0x272, TerrainCover.Path),
                (0x278, 0x279, TerrainCover.Path), (0x27B, 0x27B, TerrainCover.Path), (0x252, 0x253, TerrainCover.Path),
                // The wooden fences round Petalburg's yards, and the fences' tops drawn over the lawn north of them
                (0x23F, 0x23F, TerrainCover.Fence), (0x244, 0x246, TerrainCover.Fence), (0x24C, 0x24E, TerrainCover.Fence),
                (0x254, 0x256, TerrainCover.Fence), (0x23C, 0x23E, TerrainCover.Grass),
                // A berry patch's frame (MB_BERRY_TREE_SOIL in its middle)
                (0x28C, 0x28E, TerrainCover.Path),
                // The hedge in the yard west of Petalburg's Gym
                (0x264, 0x266, TerrainCover.Tree),
            },
        };

    /// <summary>
    /// A tile's look: by its behaviour, its label, the hand-written ranges, or <see cref="TerrainCover.Unknown"/>. For
    /// Emerald (<paramref name="game"/>) its own ranges are asked before the label, and FireRed's never.
    /// </summary>
    public static TerrainCover Of(TileBehavior behaviour, string? label, string tileset, int metatile, GbaGame? game = null)
    {
        if (OfBehaviour(behaviour) is { } shown) return shown;
        if (game == GbaGame.Emerald)
        {
            if (HoennRanges.TryGetValue(tileset, out var hoenn))
                foreach (var (from, to, cover) in hoenn)
                    if (metatile >= from && metatile <= to) return cover;
            if (label != null && OfLabel(label) is var labelled && labelled != TerrainCover.Unknown) return labelled;
            return TerrainCover.Unknown;
        }
        if (label != null && OfLabel(label) is var named && named != TerrainCover.Unknown) return named;
        if (Ranges.TryGetValue(tileset, out var ranges))
            foreach (var (from, to, cover) in ranges)
                if (metatile >= from && metatile <= to) return cover;
        return TerrainCover.Unknown;
    }
}
