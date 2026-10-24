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
                // Plan 19 · K5: the paved square before Vermilion's harbour, three by three with its edges (the set
                // beside the town roads K4 named), which read as the lawn and sand round it
                (0x1BD, 0x1BF, TerrainCover.Paving), (0x1C5, 0x1C7, TerrainCover.Paving), (0x1CD, 0x1CF, TerrainCover.Paving),
                // Plan 19 · K6, part b: the rest of the town roads' set (their ends and corners by Saffron City's gates
                // and Cinnabar Island's lab), every one walked on and none blocked (tools/MapImporter --unknown)
                (0x178, 0x17F, TerrainCover.Path), (0x191, 0x191, TerrainCover.Path), (0x198, 0x19A, TerrainCover.Path),
            },
            // Plan 19 · K6, part b (K5's left-over): Saffron City's streets, which read as lawn: every metatile of its own
            // tileset walked on and never blocked (tools/MapImporter --unknown SaffronCity) is the city's paving; its
            // buildings' own metatiles, all blocked, are left for the houses found at their doors
            ["gTileset_SaffronCity"] = new (int, int, TerrainCover)[]
            {
                (0x281, 0x282, TerrainCover.Paving), (0x285, 0x286, TerrainCover.Paving), (0x295, 0x297, TerrainCover.Paving),
                (0x2A8, 0x2AA, TerrainCover.Paving), (0x2C5, 0x2C7, TerrainCover.Paving), (0x2DC, 0x2DE, TerrainCover.Paving),
                (0x2E1, 0x2E1, TerrainCover.Paving), (0x2E3, 0x2E5, TerrainCover.Paving), (0x2E8, 0x2ED, TerrainCover.Paving),
                (0x2F1, 0x2F1, TerrainCover.Paving), (0x2F3, 0x2F5, TerrainCover.Paving), (0x2F8, 0x2FF, TerrainCover.Paving),
                (0x304, 0x304, TerrainCover.Paving), (0x306, 0x306, TerrainCover.Paving), (0x30E, 0x311, TerrainCover.Paving),
                (0x314, 0x317, TerrainCover.Paving), (0x327, 0x327, TerrainCover.Paving), (0x333, 0x333, TerrainCover.Paving),
                (0x33D, 0x33E, TerrainCover.Paving), (0x343, 0x343, TerrainCover.Paving),
            },
            // Plan 19 · K6, part b: Cinnabar Island's own open ground, the volcanic island's sandy earth round its lab
            // and the Mansion's yard
            ["gTileset_CinnabarIsland"] = new (int, int, TerrainCover)[]
            {
                (0x293, 0x293, TerrainCover.Path), (0x29E, 0x2A2, TerrainCover.Path), (0x2A6, 0x2A6, TerrainCover.Path),
                (0x2AB, 0x2AC, TerrainCover.Path), (0x2B6, 0x2B7, TerrainCover.Path), (0x2BD, 0x2BE, TerrainCover.Path),
            },
            // Plan 19 · K5: Vermilion City's own: the pier's planks south of the square and the gangway at its head
            // (MB_SOUTH_ARROW_WARP, the ways onto the S.S. Anne)
            ["gTileset_VermilionCity"] = new (int, int, TerrainCover)[]
            {
                (0x2EB, 0x2ED, TerrainCover.Bridge), (0x302, 0x303, TerrainCover.Bridge), (0x308, 0x30A, TerrainCover.Bridge),
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
            // The Cycling Road (plan 19 · K6; Route 17 is all but the only user of these): its three asphalt lanes and
            // their ends, the posts of its fences, and the mowed verges between, which pull a rider south as the lanes do
            // (MB_CYCLING_ROAD_PULL_DOWN). Its water (MB_CYCLING_ROAD_WATER) is water by its behaviour
            ["gTileset_CeladonCity"] = new (int, int, TerrainCover)[]
            {
                (0x320, 0x322, TerrainCover.Paving), (0x328, 0x32A, TerrainCover.Paving), (0x330, 0x332, TerrainCover.Paving),
                (0x340, 0x345, TerrainCover.Paving),
                (0x323, 0x323, TerrainCover.Fence), (0x32B, 0x32B, TerrainCover.Fence), (0x333, 0x333, TerrainCover.Fence),
                (0x325, 0x325, TerrainCover.Fence), (0x32D, 0x32D, TerrainCover.Fence), (0x335, 0x335, TerrainCover.Fence),
                (0x324, 0x324, TerrainCover.Grass), (0x32C, 0x32C, TerrainCover.Grass), (0x334, 0x334, TerrainCover.Grass),
                (0x338, 0x33F, TerrainCover.Grass), (0x346, 0x351, TerrainCover.Grass),
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
        TileBehavior.River or TileBehavior.Sea or TileBehavior.DiveWater or TileBehavior.Waterfall or TileBehavior.CurrentEast
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
                // Plan 21 · H2, part b (Route 104, Petalburg Woods, Rustboro City, Route 116): the shaded lawn of the
                // woods' floor between its trees, a ledge's end, the crossing where Rustboro's streets meet, the sandy
                // path's other pieces along Route 104's beach, the rocks standing in the water, the rocky shore round
                // the sea and Rustboro's pond, and the forest's last trees beside its paths
                (0x00D, 0x00D, TerrainCover.Grass), (0x0D3, 0x0D3, TerrainCover.Grass), (0x05E, 0x05E, TerrainCover.Paving),
                (0x123, 0x127, TerrainCover.Path), (0x12B, 0x12C, TerrainCover.Path),
                (0x150, 0x151, TerrainCover.Boulder), (0x158, 0x159, TerrainCover.Boulder),
                (0x175, 0x175, TerrainCover.Cliff), (0x17A, 0x17A, TerrainCover.Cliff),
                (0x1F2, 0x1F3, TerrainCover.Tree),
            },
            // Plan 21 · H2, part b: Rustboro City, a city of stone on a raised ground, read from its layout
            ["gTileset_Rustboro"] = new (int, int, TerrainCover)[]
            {
                // The flagstones of the city's streets and squares, their shaded edges along the buildings, and the
                // kerbs of the two streets that cross it
                (0x2BB, 0x2BB, TerrainCover.Paving), (0x2BD, 0x2BD, TerrainCover.Paving), (0x2C3, 0x2C4, TerrainCover.Paving),
                (0x2E0, 0x2E1, TerrainCover.Paving), (0x2E8, 0x2E8, TerrainCover.Paving), (0x2EA, 0x2EA, TerrainCover.Paving),
                (0x2F1, 0x2F1, TerrainCover.Paving), (0x2F8, 0x2FA, TerrainCover.Paving), (0x300, 0x302, TerrainCover.Paving),
                (0x308, 0x30A, TerrainCover.Paving),
                // The street lamps in pairs along the streets: the post on its blocked tile, and the paving under the lamp's head
                (0x212, 0x212, TerrainCover.Paving), (0x21A, 0x21A, TerrainCover.Lamp),
                // The low stone walls the raised city stands behind, and the rails along their tops
                (0x2A7, 0x2A7, TerrainCover.Fence), (0x2B7, 0x2B7, TerrainCover.Fence), (0x2BE, 0x2BF, TerrainCover.Fence),
                (0x2DE, 0x2DF, TerrainCover.Fence), (0x2E9, 0x2E9, TerrainCover.Fence), (0x2EC, 0x2EC, TerrainCover.Fence),
                (0x2FC, 0x2FC, TerrainCover.Fence), (0x315, 0x315, TerrainCover.Fence), (0x318, 0x31B, TerrainCover.Fence),
                (0x31D, 0x31D, TerrainCover.Fence), (0x320, 0x321, TerrainCover.Fence), (0x32C, 0x32D, TerrainCover.Fence),
                (0x352, 0x352, TerrainCover.Fence),
            },
            // Plan 21 · H2, part b: the way down a house's stairs (Rustboro's flats), whose label (the Trick House's)
            // would make it a house standing in the room
            ["gTileset_GenericBuilding"] = new (int, int, TerrainCover)[]
            {
                (0x219, 0x219, TerrainCover.Path),
            },
            // Plan 21 · H3: the way down the stairs on the upper floor of Stern's shipyard, whose label would make it a
            // building standing in the room (as the flats' stairs above)
            ["gTileset_Facility"] = new (int, int, TerrainCover)[]
            {
                (0x2C3, 0x2C5, TerrainCover.Path), (0x2CB, 0x2CD, TerrainCover.Path),
            },
            // Plan 21 · H2, part b: Rusturf Tunnel's floor (its walls are the cave's rock wherever nobody walks)
            ["gTileset_RusturfTunnel"] = new (int, int, TerrainCover)[]
            {
                (0x201, 0x204, TerrainCover.CaveFloor),
            },
            // Plan 21 · H3: Slateport City, read from its layout: the stone road that runs north and south through it
            // and its branches (three by three with their edges and the corners where they meet), the paved squares
            // before the Battle Tent and the Name Rater's, the market's and the harbour's flagstones, and the low walls
            // and rails round the market and along the beach
            ["gTileset_Slateport"] = new (int, int, TerrainCover)[]
            {
                (0x208, 0x20A, TerrainCover.Paving), (0x210, 0x212, TerrainCover.Paving), (0x216, 0x216, TerrainCover.Paving),
                (0x218, 0x21A, TerrainCover.Paving), (0x21E, 0x21E, TerrainCover.Paving), (0x232, 0x233, TerrainCover.Paving),
                (0x23A, 0x23B, TerrainCover.Paving), (0x270, 0x272, TerrainCover.Paving), (0x277, 0x27A, TerrainCover.Paving),
                (0x202, 0x202, TerrainCover.Paving), (0x246, 0x247, TerrainCover.Paving), (0x24E, 0x24F, TerrainCover.Paving),
                (0x2D5, 0x2D6, TerrainCover.Paving), (0x2DD, 0x2DE, TerrainCover.Paving), (0x330, 0x334, TerrainCover.Paving),
                (0x33B, 0x33D, TerrainCover.Paving), (0x343, 0x343, TerrainCover.Paving), (0x345, 0x345, TerrainCover.Paving),
                (0x34B, 0x34D, TerrainCover.Paving),
                (0x23C, 0x23C, TerrainCover.Fence), (0x245, 0x245, TerrainCover.Fence), (0x24D, 0x24D, TerrainCover.Fence),
                (0x255, 0x255, TerrainCover.Fence), (0x25D, 0x25D, TerrainCover.Fence), (0x2B8, 0x2BB, TerrainCover.Fence),
                (0x27E, 0x27E, TerrainCover.Fence), (0x2FE, 0x2FF, TerrainCover.Fence), (0x32D, 0x32D, TerrainCover.Fence),
            },
            // Plan 21 · H3: Dewford Town, read from its layout: the planks of the jetty where Mr. Briney moors, and the
            // palms the island is ringed by
            ["gTileset_Dewford"] = new (int, int, TerrainCover)[]
            {
                (0x228, 0x229, TerrainCover.Bridge),
                (0x23A, 0x23A, TerrainCover.Tree), (0x243, 0x243, TerrainCover.Tree),
            },
            // Plan 21 · H4, part b: Lavaridge Town, Mt. Chimney and Jagged Pass, read from what is walked: every metatile
            // of the tileset that no behaviour or label names and that is open more often than blocked is the
            // volcano's grey earth (the ash on the summit's paths, the pass's tracks, the town's lanes); what is
            // blocked is left to the rock and the trees the builder stands where nothing is named (World.BareRock),
            // since a house of the town is found at its door and the mountain's faces are rock either way
            ["gTileset_Lavaridge"] = new (int, int, TerrainCover)[]
            {
                (0x202, 0x202, TerrainCover.Path), (0x209, 0x209, TerrainCover.Path), (0x210, 0x210, TerrainCover.Path),
                (0x216, 0x217, TerrainCover.Path), (0x21E, 0x220, TerrainCover.Path), (0x222, 0x224, TerrainCover.Path),
                (0x226, 0x227, TerrainCover.Path), (0x22C, 0x22C, TerrainCover.Path), (0x241, 0x242, TerrainCover.Path),
                (0x252, 0x254, TerrainCover.Path), (0x25A, 0x25B, TerrainCover.Path), (0x271, 0x271, TerrainCover.Path),
                (0x28A, 0x28A, TerrainCover.Path), (0x28C, 0x28C, TerrainCover.Path), (0x291, 0x292, TerrainCover.Path),
                (0x298, 0x298, TerrainCover.Path), (0x29C, 0x29C, TerrainCover.Path), (0x29E, 0x29E, TerrainCover.Path),
                (0x2A0, 0x2A0, TerrainCover.Path), (0x2A3, 0x2A3, TerrainCover.Path), (0x2AA, 0x2AA, TerrainCover.Path),
                (0x2AC, 0x2AC, TerrainCover.Path), (0x2AE, 0x2AF, TerrainCover.Path), (0x2BB, 0x2BC, TerrainCover.Path),
                (0x2C4, 0x2C4, TerrainCover.Path), (0x2D0, 0x2D1, TerrainCover.Path), (0x2D4, 0x2D4, TerrainCover.Path),
                (0x2DE, 0x2DF, TerrainCover.Path), (0x2FE, 0x303, TerrainCover.Path), (0x31D, 0x31D, TerrainCover.Path),
            },
            // Plan 21 · H6: Fortree City and Routes 119 and 120, read from their layouts. The plank platforms before the
            // tree houses' doors (elevation 4, the walkways' height) and the river's bridges, the ladders down from a
            // platform, the forest floor's earth paths that wander under them, and the great trees, trunks, canopies
            // and bushes the city stands among. A house's own metatiles and the Weather Institute's are left out: each
            // is found at its door
            ["gTileset_Fortree"] = new (int, int, TerrainCover)[]
            {
                (0x23B, 0x23F, TerrainCover.Bridge), (0x211, 0x211, TerrainCover.Bridge), (0x213, 0x215, TerrainCover.Bridge),
                (0x219, 0x219, TerrainCover.Bridge), (0x21B, 0x21D, TerrainCover.Bridge),
                (0x245, 0x245, TerrainCover.Steps), (0x24D, 0x24D, TerrainCover.Steps),
                (0x241, 0x241, TerrainCover.Path), (0x24B, 0x24C, TerrainCover.Path), (0x255, 0x255, TerrainCover.Path),
                (0x258, 0x259, TerrainCover.Path), (0x260, 0x262, TerrainCover.Path), (0x268, 0x26A, TerrainCover.Path),
                (0x26C, 0x26C, TerrainCover.Path), (0x276, 0x276, TerrainCover.Path), (0x29B, 0x29C, TerrainCover.Path),
                (0x2CA, 0x2CA, TerrainCover.Path), (0x2D0, 0x2D2, TerrainCover.Path), (0x2D5, 0x2D6, TerrainCover.Path),
                (0x2E6, 0x2E6, TerrainCover.Path), (0x2F5, 0x2F5, TerrainCover.Path), (0x2FE, 0x2FE, TerrainCover.Path),
                (0x306, 0x306, TerrainCover.Path),
                (0x206, 0x207, TerrainCover.Tree), (0x20F, 0x20F, TerrainCover.Tree), (0x222, 0x223, TerrainCover.Tree),
                (0x22A, 0x22A, TerrainCover.Tree), (0x230, 0x232, TerrainCover.Tree), (0x238, 0x23A, TerrainCover.Tree),
                (0x240, 0x240, TerrainCover.Tree), (0x242, 0x244, TerrainCover.Tree), (0x246, 0x24A, TerrainCover.Tree),
                (0x250, 0x253, TerrainCover.Tree), (0x25C, 0x25D, TerrainCover.Tree), (0x263, 0x265, TerrainCover.Tree),
                (0x26D, 0x26D, TerrainCover.Tree), (0x270, 0x270, TerrainCover.Tree), (0x272, 0x273, TerrainCover.Tree),
            },
            // Plan 21 · H7, part a: Lilycove City, read from what is walked, as Lavaridge's rows are: every metatile of
            // the tileset no behaviour or label names that is open more often than blocked is the city's paving (the
            // squares before the Department Store and the Contest Hall, the streets that cross between the houses, the
            // steps down to the beach); what is blocked is left to the houses, found at their doors, and to the rock
            ["gTileset_Lilycove"] = new (int, int, TerrainCover)[]
            {
                (0x20A, 0x20B, TerrainCover.Paving), (0x212, 0x213, TerrainCover.Paving), (0x21A, 0x21B, TerrainCover.Paving),
                (0x222, 0x223, TerrainCover.Paving), (0x22A, 0x22F, TerrainCover.Paving), (0x232, 0x234, TerrainCover.Paving),
                (0x23A, 0x23C, TerrainCover.Paving), (0x240, 0x240, TerrainCover.Paving), (0x260, 0x262, TerrainCover.Paving),
                (0x27A, 0x27A, TerrainCover.Paving), (0x28A, 0x28C, TerrainCover.Paving), (0x294, 0x294, TerrainCover.Paving),
                (0x2C0, 0x2C2, TerrainCover.Paving), (0x2E7, 0x2E7, TerrainCover.Paving), (0x348, 0x34A, TerrainCover.Paving),
            },
            // Plan 21 · H8, part a: Mossdeep City, read from what is walked as Lilycove's rows are: every metatile of the
            // tileset no behaviour or label names that is open more often than blocked is the island's sandy path (the
            // walks between its lawns, round the Space Center's yard and down to the shore); what is blocked is left to
            // the houses, found at their doors, and to the white rock. Its beach is the primary tileset's, whose open
            // metatiles nothing names take the nearest ground, the sand beside them
            ["gTileset_Mossdeep"] = new (int, int, TerrainCover)[]
            {
                (0x294, 0x295, TerrainCover.Path), (0x297, 0x297, TerrainCover.Path), (0x2A6, 0x2A6, TerrainCover.Path),
                (0x2F0, 0x2F1, TerrainCover.Path), (0x2FA, 0x2FB, TerrainCover.Path), (0x301, 0x305, TerrainCover.Path),
                (0x30A, 0x30A, TerrainCover.Path), (0x30C, 0x30C, TerrainCover.Path), (0x31A, 0x31B, TerrainCover.Path),
                (0x31E, 0x31F, TerrainCover.Path), (0x358, 0x35A, TerrainCover.Path), (0x371, 0x376, TerrainCover.Path),
                (0x379, 0x37E, TerrainCover.Path), (0x381, 0x386, TerrainCover.Path), (0x389, 0x38C, TerrainCover.Path),
                (0x38E, 0x38E, TerrainCover.Path), (0x391, 0x396, TerrainCover.Path), (0x399, 0x39E, TerrainCover.Path),
            },
            // Plan 21 · H8, part b: Sootopolis City, read from what is walked as Lilycove's and Mossdeep's rows are: the
            // crater's floor of white stone round the harbour (0x2D9, its terraces' tops too), the steps and walks cut
            // into the terraces between the houses (0x244, 0x245), and the stone before the Cave of Origin's mouth and
            // the houses' doors (0x221, 0x229, 0x238). The crater's walls and the houses are blocked and left to the rock
            // and to the doors
            ["gTileset_Sootopolis"] = new (int, int, TerrainCover)[]
            {
                (0x2D9, 0x2D9, TerrainCover.Paving), (0x244, 0x245, TerrainCover.Paving),
                (0x221, 0x221, TerrainCover.Paving), (0x229, 0x229, TerrainCover.Paving), (0x238, 0x238, TerrainCover.Paving),
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
            // Water ankle deep is water to look at, walked through (plan 21 · H8, part a: the wide shallows round
            // Mossdeep City and the islets of the eastern sea, whose metatiles nothing else named)
            if (behaviour == TileBehavior.ShallowWater) return TerrainCover.Water;
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
