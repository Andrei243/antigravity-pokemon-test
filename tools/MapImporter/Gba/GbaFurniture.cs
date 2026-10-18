using PokemonPlatinumEngine.Overworld;

namespace MapImporter.Gba;

/// <summary>
/// The interior table (plan 19 · K2): what piece of furniture a metatile of a GBA room is, so a room built from its
/// layout is furnished as Sinnoh's rooms are (<c>PropModels</c>) instead of standing as floor and blocks of wall. A
/// metatile is furniture by its behaviour where the behaviour names it (a bookshelf, a television, a Mart's shelf),
/// and otherwise by the ranges of its tileset written here by hand, judged from how each metatile is used on the
/// layouts of the rooms (where it stands, what it blocks, who stands beside it), as <see cref="GbaCover.Ranges"/> is:
/// no picture of the original is kept. The importer writes a run of one kind as a prop of the chunk named
/// <c>gba:&lt;PropType&gt;</c>, and <c>WorldMapBuilder</c> lays it in the room.
/// </summary>
public static class GbaFurniture
{
    /// <summary>The prefix of a chunk prop that is a piece of a GBA room's furniture.</summary>
    public const string Prefix = PokemonPlatinumEngine.Data.WorldMapBuilder.FurniturePrefix;

    /// <summary>The behaviours that say what stands on their tile.</summary>
    public static readonly IReadOnlyDictionary<string, PropType> ByBehaviour = new Dictionary<string, PropType>(StringComparer.Ordinal)
    {
        ["MB_BOOKSHELF"] = PropType.Bookshelf,
        ["MB_POKEMART_SHELF"] = PropType.StoreShelf,
        ["MB_NEATLY_LINED_UP_TOOLS"] = PropType.StoreShelf,
        ["MB_TELEVISION"] = PropType.Television,
        ["MB_KITCHEN"] = PropType.KitchenCounter,
        ["MB_CABINET"] = PropType.Fridge,
        ["MB_DRESSER"] = PropType.Wardrobe,
        ["MB_PC"] = PropType.Computer,
        ["MB_COMPUTER"] = PropType.LabMachine,
        ["MB_POWER_PLANT_MACHINE"] = PropType.LabMachine,
        ["MB_BLINKING_LIGHTS"] = PropType.LabMachine,
        ["MB_IMPRESSIVE_MACHINE"] = PropType.LabMachine,
        ["MB_CABLE_CLUB_WIRELESS_MONITOR"] = PropType.Computer,
        ["MB_TRASH_BIN"] = PropType.TrashCan,
        ["MB_COUNTER"] = PropType.Counter,
        // Hung on the back wall: drawn on the wall's row, never in the way
        ["MB_WINDOW"] = PropType.Window,
        ["MB_PORTHOLE"] = PropType.Window,
        ["MB_PAINTING"] = PropType.Painting,
        ["MB_REGION_MAP"] = PropType.Painting,
        ["MB_ADVERTISING_POSTER"] = PropType.Painting,
        ["MB_BATTLE_RECORDS"] = PropType.Painting,
        // Emerald's names (plan 21 · H1): a shop's shelves, and the questionnaire's terminal by a Mart's counter
        ["MB_SHOP_SHELF"] = PropType.StoreShelf,
        ["MB_QUESTIONNAIRE"] = PropType.Computer,
    };

    /// <summary>
    /// The metatiles no behaviour names, by tileset. <c>gTileset_Building</c> is the primary tileset of every room of
    /// FireRed's, so its rows furnish the houses of every town; the secondary rows are the rooms' own.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, (int From, int To, PropType Prop)[]> Ranges =
        new Dictionary<string, (int, int, PropType)[]>(StringComparer.Ordinal)
        {
            ["gTileset_Building"] = new (int, int, PropType)[]
            {
                // The house's table, two metatiles by two in the middle of its rug, with a chair on either side
                (0x04C, 0x04D, PropType.Table), (0x054, 0x055, PropType.Table),
                // The rug round it: walked on
                (0x043, 0x046, PropType.Rug), (0x04B, 0x04B, PropType.Rug), (0x04E, 0x04E, PropType.Rug), (0x053, 0x053, PropType.Rug),
                (0x056, 0x056, PropType.Rug), (0x05B, 0x05E, PropType.Rug),
                // A potted plant in a corner: its pot blocks, its leaves are drawn over the tile above
                (0x04F, 0x04F, PropType.Plant), (0x05F, 0x05F, PropType.Plant),
                // The flight of stairs up (beside the stairs' warp, MB_UP_RIGHT_STAIR_WARP) and the flight down
                (0x016, 0x017, PropType.Stairs), (0x01E, 0x01F, PropType.Stairs),
                (0x005, 0x006, PropType.Stairs), (0x00D, 0x00E, PropType.Stairs),
            },
            ["gTileset_GenericBuilding1"] = new (int, int, PropType)[]
            {
                // The player's bedroom: the games console on the rug before the television, and the bed by the window, three
                // metatiles by three (plan 19 · K3: K2 read its blocked foot as a stool)
                (0x28E, 0x28E, PropType.Table),
                (0x283, 0x285, PropType.Bed), (0x28B, 0x28D, PropType.Bed), (0x293, 0x295, PropType.Bed),
                // The desk with the player's PC at the back wall's west end (its sign event reads it)
                (0x28F, 0x28F, PropType.Computer),
            },
            ["gTileset_GenericBuilding2"] = new (int, int, PropType)[]
            {
                // The gates' halls (Route 2's, Route 22's): a runner down the middle from door to door
                (0x29A, 0x29B, PropType.Rug), (0x2A2, 0x2A3, PropType.Rug), (0x2C2, 0x2C4, PropType.Rug), (0x2CA, 0x2CB, PropType.Rug),
                // Their tables with a chair either side, square and round
                (0x2D6, 0x2D7, PropType.Table), (0x2DE, 0x2DF, PropType.Table), (0x32F, 0x32F, PropType.Table),
                (0x2E6, 0x2E7, PropType.Cushion),
                // The benches along the side walls, and the planters between the back wall's windows
                (0x2F0, 0x2F1, PropType.Bench),
            },
            ["gTileset_Lab"] = new (int, int, PropType)[]
            {
                // Professor Oak's table, where the three Poké Balls lie
                (0x2A8, 0x2AA, PropType.LabDesk),
                // The machines along the west wall (MB_POWER_PLANT_MACHINE's other halves)
                (0x29F, 0x29F, PropType.LabMachine),
                // The potted plants in the corners by the door
                (0x29B, 0x29C, PropType.Plant),
            },
            ["gTileset_PokemonCenter"] = new (int, int, PropType)[]
            {
                // Behind the counter: the healing machine beside the nurse, and the counter's ends
                (0x2B2, 0x2B3, PropType.HealingMachine), (0x2B4, 0x2B4, PropType.Counter), (0x2B9, 0x2B9, PropType.Counter),
                (0x2BD, 0x2BD, PropType.Counter),
                // The table of the seats in the corner
                (0x29C, 0x29D, PropType.Table), (0x2A4, 0x2A5, PropType.Table),
                // The seats round it and on the upper floor's benches: walked onto
                (0x2F5, 0x2F5, PropType.Cushion), (0x2FD, 0x2FD, PropType.Cushion),
                // The upper floor's counters of the link rooms
                (0x2F9, 0x2F9, PropType.Counter), (0x2EF, 0x2EF, PropType.Counter),
                // The plants at the foot of the escalators upstairs
                (0x301, 0x301, PropType.Plant),
                // The escalators' blocked halves beside their warps (MB_UP_ESCALATOR, MB_DOWN_ESCALATOR), as a flight
                (0x2D8, 0x2D8, PropType.Stairs), (0x2E3, 0x2E3, PropType.Stairs),
            },
            ["gTileset_Mart"] = new (int, int, PropType)[]
            {
                // The till on the counter, and the counter's corner
                (0x2B1, 0x2B1, PropType.Counter), (0x2A0, 0x2A1, PropType.StoreShelf), (0x2B6, 0x2B6, PropType.StoreShelf),
            },
            ["gTileset_School"] = new (int, int, PropType)[]
            {
                // The teacher's desk in the middle of the classroom
                (0x299, 0x29A, PropType.LabDesk),
                // The plants in the room's front corners
                (0x289, 0x28A, PropType.Plant),
            },
            // Plan 19 · K3: the Pewter City Gym's hall, a floor of rock with boulders standing in it, and the two
            // statues by the door (the layout's blocked metatiles; the statues are the signs FireRed reads there)
            ["gTileset_PewterGym"] = new (int, int, PropType)[]
            {
                (0x2A2, 0x2A4, PropType.Boulder), (0x29B, 0x29B, PropType.Boulder),
                (0x2B2, 0x2B2, PropType.TrainerStatue),
            },
            // Plan 19 · K4, part b: Bill's Sea Cottage, his two teleporters three metatiles by three either side of the
            // cable between them (metatile_labels.h names their doors and lights), the low table in the middle of
            // the floor and the pots by the door
            ["gTileset_SeaCottage"] = new (int, int, PropType)[]
            {
                (0x286, 0x287, PropType.LabMachine), (0x289, 0x28B, PropType.LabMachine), (0x28E, 0x28F, PropType.LabMachine),
                (0x291, 0x293, PropType.LabMachine), (0x295, 0x297, PropType.LabMachine),
                (0x2A5, 0x2A6, PropType.Table),
                (0x2A3, 0x2A3, PropType.Plant),
            },
            // Plan 19 · K5: the S.S. Anne's cabins, galley and captain's cabin (K4's part b left them as floor and blocks
            // of wall). A cabin's bed of two metatiles by three against the west wall (the first plan) or three by two
            // at the back (the second), a table of one metatile between two chairs, and a plant by the door
            ["gTileset_SSAnne"] = new (int, int, PropType)[]
            {
                (0x338, 0x339, PropType.Bed), (0x340, 0x341, PropType.Bed), (0x348, 0x349, PropType.Bed),
                (0x342, 0x344, PropType.Bed), (0x34A, 0x34C, PropType.Bed),
                (0x329, 0x329, PropType.Table), (0x331, 0x331, PropType.Table), (0x309, 0x309, PropType.Table), (0x311, 0x311, PropType.Table),
                (0x328, 0x328, PropType.Cushion), (0x32A, 0x32A, PropType.Cushion), (0x330, 0x330, PropType.Cushion),
                (0x332, 0x332, PropType.Cushion), (0x308, 0x308, PropType.Cushion), (0x30A, 0x30A, PropType.Cushion),
                (0x310, 0x310, PropType.Cushion), (0x312, 0x312, PropType.Cushion),
                (0x333, 0x333, PropType.Plant), (0x313, 0x313, PropType.Plant),
                // The galley's three long worktops down the room, and its stoves along the back wall (MB_FOOD_SMELLS_TASTY)
                (0x2D0, 0x2D4, PropType.KitchenCounter), (0x2D8, 0x2DB, PropType.KitchenCounter), (0x2F2, 0x2F2, PropType.Stove),
                // The captain's desk in his cabin's corner and his bed along the west wall
                (0x305, 0x307, PropType.Table), (0x30D, 0x30F, PropType.Table),
                (0x2F5, 0x2F6, PropType.Bed), (0x2AB, 0x2AC, PropType.Bed), (0x2B3, 0x2B4, PropType.Bed),
            },
            // Plan 19 · K5: the Pokémon Tower's floors, rows of gravestones between the channelers
            ["gTileset_PokemonTower"] = new (int, int, PropType)[]
            {
                (0x291, 0x291, PropType.Headstone),
            },
        };

    /// <summary>
    /// Emerald's rows (plan 21 · H1), apart from FireRed's because the two games share tilesets' names (<c>gTileset_Building</c>,
    /// <c>gTileset_PokemonCenter</c>) and not their metatiles: the rooms of Littleroot Town, Oldale Town and Petalburg City,
    /// judged as Kanto's were, from the layouts and the sign events that say what is read where.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, (int From, int To, PropType Prop)[]> HoennRanges =
        new Dictionary<string, (int, int, PropType)[]>(StringComparer.Ordinal)
        {
            ["gTileset_BrendansMaysHouse"] = new (int, int, PropType)[]
            {
                // Downstairs: the kitchen's cupboards and sink along the back wall's west end, two rows deep
                (0x230, 0x234, PropType.KitchenCounter), (0x238, 0x23C, PropType.KitchenCounter),
                // Plants beside the television at the head of the rug (MB_TELEVISION, the primary tileset's), and the
                // table in the rug's middle
                (0x240, 0x241, PropType.Plant), (0x21A, 0x21B, PropType.Table),
                (0x222, 0x223, PropType.Table),
                // The rug round the table: walked on
                (0x218, 0x219, PropType.Rug), (0x21C, 0x21D, PropType.Rug), (0x220, 0x221, PropType.Rug), (0x224, 0x22E, PropType.Rug),
                (0x235, 0x236, PropType.Rug), (0x248, 0x24A, PropType.Rug),
                // The flight of stairs up beside the stairs' warp, and the one down beside the bedroom's
                (0x210, 0x210, PropType.Stairs), (0x213, 0x213, PropType.Stairs),
                // Upstairs: the PC on its desk and the notebook beside it at the back wall, the games console's television
                // and the clock on the wall (the sign events PC, Notebook, GameCube and WallClock read them)
                (0x25A, 0x25A, PropType.Computer), (0x25B, 0x25B, PropType.Table), (0x265, 0x265, PropType.Television),
                (0x28F, 0x28F, PropType.Painting),
                // The bed by the west wall, and the rug where the decorations stand
                (0x27B, 0x27D, PropType.Bed), (0x283, 0x285, PropType.Bed), (0x28B, 0x28D, PropType.Bed),
                (0x298, 0x29A, PropType.Rug), (0x2A0, 0x2A2, PropType.Rug), (0x2A8, 0x2AA, PropType.Rug),
            },
            ["gTileset_Lab"] = new (int, int, PropType)[]
            {
                // Professor Birch's lab: the bookshelves at the back wall and across the west half (the Bookshelf signs)
                (0x218, 0x219, PropType.Bookshelf), (0x20A, 0x20E, PropType.Bookshelf), (0x22C, 0x22D, PropType.Bookshelf),
                (0x234, 0x235, PropType.Bookshelf),
                // The PCs (the PC signs): at the back wall, and in the two corners by the door
                (0x21A, 0x21B, PropType.Computer), (0x230, 0x230, PropType.Computer), (0x238, 0x238, PropType.Computer),
                (0x23B, 0x23B, PropType.Computer), (0x243, 0x243, PropType.Computer),
                // The machines on the east wall (the Machine signs) and the equipment beside the corners' PCs
                (0x214, 0x216, PropType.LabMachine), (0x21C, 0x21E, PropType.LabMachine), (0x240, 0x240, PropType.LabMachine),
                (0x248, 0x248, PropType.LabMachine), (0x24B, 0x24B, PropType.LabMachine),
                // The professor's desk, and the potted plants
                (0x231, 0x231, PropType.LabDesk), (0x242, 0x242, PropType.LabDesk),
                (0x22E, 0x22E, PropType.Plant), (0x232, 0x232, PropType.Plant), (0x29A, 0x29A, PropType.Plant),
            },
            ["gTileset_PokemonCenter"] = new (int, int, PropType)[]
            {
                // The healing machine beside the nurse, and the counter's ends and front (MB_COUNTER's other metatiles)
                (0x22A, 0x22B, PropType.HealingMachine), (0x250, 0x251, PropType.Counter), (0x258, 0x259, PropType.Counter),
                (0x25B, 0x25B, PropType.Counter), (0x221, 0x221, PropType.Counter),
                // The table of the seats in the corner, and the plants
                (0x23D, 0x23E, PropType.Table), (0x245, 0x246, PropType.Table),
                (0x228, 0x228, PropType.Plant), (0x237, 0x237, PropType.Plant), (0x288, 0x288, PropType.Plant),
            },
            ["gTileset_Shop"] = new (int, int, PropType)[]
            {
                // The till's end of the counter, and the plant by the door
                (0x242, 0x242, PropType.Counter), (0x22D, 0x22D, PropType.Plant),
            },
            ["gTileset_GenericBuilding"] = new (int, int, PropType)[]
            {
                // A house's table, two metatiles by two, and the rug round it
                (0x248, 0x24B, PropType.Table), (0x250, 0x253, PropType.Table),
                (0x22B, 0x22C, PropType.Rug), (0x235, 0x237, PropType.Rug), (0x23D, 0x23F, PropType.Rug), (0x245, 0x247, PropType.Rug),
                (0x2E2, 0x2E2, PropType.Rug),
                // The cupboard along the back wall, and the potted plants in the corners
                (0x298, 0x298, PropType.KitchenCounter), (0x22A, 0x22A, PropType.Plant), (0x299, 0x299, PropType.Plant),
            },
        };

    /// <summary>
    /// A metatile's furniture: by its behaviour where that names one, then by its tileset's ranges (Emerald's own for
    /// <paramref name="game"/> Emerald), or none.
    /// </summary>
    public static PropType? Of(string behaviourName, string tileset, int metatile, GbaGame? game = null)
    {
        if (ByBehaviour.TryGetValue(behaviourName, out var named)) return named;
        if ((game == GbaGame.Emerald ? HoennRanges : Ranges).TryGetValue(tileset, out var ranges))
            foreach (var (from, to, prop) in ranges)
                if (metatile >= from && metatile <= to) return prop;
        return null;
    }
}
