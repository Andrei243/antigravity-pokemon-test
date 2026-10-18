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
        };

    /// <summary>A metatile's furniture: by its behaviour where that names one, then by its tileset's ranges, or none.</summary>
    public static PropType? Of(string behaviourName, string tileset, int metatile)
    {
        if (ByBehaviour.TryGetValue(behaviourName, out var named)) return named;
        if (Ranges.TryGetValue(tileset, out var ranges))
            foreach (var (from, to, prop) in ranges)
                if (metatile >= from && metatile <= to) return prop;
        return null;
    }
}
