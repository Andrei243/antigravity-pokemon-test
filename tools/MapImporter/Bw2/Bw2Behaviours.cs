using PokemonPlatinumEngine.Overworld;

namespace MapImporter.Bw2;

/// <summary>
/// Black 2's tile classes as this game's behaviours (plan 22 · U1). A tile of a Black 2 chunk carries a 32-bit type:
/// its low sixteen bits are its class, which the field code asks about one by one (<c>src/ov036/map_attr.c</c> and
/// <c>field_water_current.c</c> in the decompilation), and its high sixteen are flags, of which the lowest blocks the
/// tile. A class the field code names becomes Platinum's value where Platinum has one that means the same, the GBA's
/// (plan 18 · W6) where the GBA has one, and a value of Black 2's own from 0xB0 where neither has (dark grass among
/// them). A class the code asks about only for its battle background, or not at all, is in no row: it becomes
/// <see cref="TileBehavior.None"/> and the run's report counts it, for the session that opens a place using it.
/// The descriptions are ours, from what the code does with each class.
/// </summary>
public static class Bw2Behaviours
{
    /// <summary>The flag that blocks a tile, in the type's high half.</summary>
    public const int BlockedFlag = 1;

    /// <summary>Each class the table knows: what it is, and the value it becomes.</summary>
    public static readonly IReadOnlyDictionary<int, (string What, TileBehavior Becomes)> Table = new Dictionary<int, (string, TileBehavior)>
    {
        [0x00] = ("Plain ground", TileBehavior.None),
        [0x01] = ("A wall: plain, always blocked", TileBehavior.None),
        [0x04] = ("Tall grass", TileBehavior.TallGrass),
        [0x05] = ("Tall grass of a second look", TileBehavior.TallGrass),
        [0x21] = ("Tall grass of a third look", TileBehavior.TallGrass),
        [0x06] = ("Dark grass: two wild Pokémon at a time", TileBehavior.DarkGrass),
        [0x07] = ("Dark grass of a second look", TileBehavior.DarkGrass),
        [0x22] = ("Dark grass of a third look", TileBehavior.DarkGrass),
        [0x08] = ("Very tall grass", TileBehavior.VeryTallGrass),
        [0x09] = ("Very tall dark grass", TileBehavior.VeryTallDarkGrass),
        [0x24] = ("The grass before a Hidden Grotto", TileBehavior.GrottoGrass),
        [0x0A] = ("A cave's floor", TileBehavior.CaveFloor),
        [0x0B] = ("Sand", TileBehavior.Sand),
        [0x0C] = ("Deep sand", TileBehavior.DeepSand),
        [0x23] = ("Sand of a second look", TileBehavior.Sand),
        [0x19] = ("A beach's sand", TileBehavior.Sand),
        [0x7C] = ("Quicksand", TileBehavior.Quicksand),
        [0x0E] = ("Snow", TileBehavior.ShadedSnow),
        [0x0F] = ("Snow no Bicycle crosses", TileBehavior.ShallowSnow),
        [0x10] = ("Ice that is slid on", TileBehavior.Ice),
        [0x18] = ("Ice that is slid on, of a second kind", TileBehavior.Ice),
        [0x1C] = ("A swamp", TileBehavior.MarshGrass),
        [0x1D] = ("A hole for a boulder pushed with Strength", TileBehavior.StrengthHole),
        [0x20] = ("A bridge's deck, with birds' shadows crossing it", TileBehavior.Bridge),
        [0x30] = ("The charged floor of Chargestone Cave", TileBehavior.ChargedFloor),
        [0x3D] = ("Water that is surfed", TileBehavior.River),
        [0x3E] = ("Water that is surfed, of a second kind", TileBehavior.River),
        [0x3F] = ("The sea", TileBehavior.Sea),
        [0x42] = ("The sea, of a second kind", TileBehavior.Sea),
        [0x43] = ("The sea, of a third kind", TileBehavior.Sea),
        [0x40] = ("A waterfall", TileBehavior.Waterfall),
        [0x41] = ("The water's edge, where Surf begins", TileBehavior.SurfEdge),
        [0x44] = ("The water's edge, of a second kind", TileBehavior.SurfEdge),
        [0x51] = ("No way out east", TileBehavior.BlockEast),
        [0x52] = ("No way out west", TileBehavior.BlockWest),
        [0x53] = ("No way out north", TileBehavior.BlockNorth),
        [0x54] = ("No way out south", TileBehavior.BlockSouth),
        [0x55] = ("No way out north or east", TileBehavior.BlockNorthAndEast),
        [0x56] = ("No way out north or west", TileBehavior.BlockNorthAndWest),
        [0x57] = ("No way out south or east", TileBehavior.BlockSouthAndEast),
        [0x58] = ("No way out south or west", TileBehavior.BlockSouthAndWest),
        // The jumps, as field_water_current.c's table of their moves orders them: north, south, west, east
        [0x72] = ("A ledge jumped east", TileBehavior.LedgeEast),
        [0x73] = ("A ledge jumped west", TileBehavior.LedgeWest),
        [0x74] = ("A ledge jumped north", TileBehavior.LedgeNorth),
        [0x75] = ("A ledge jumped south", TileBehavior.LedgeSouth),
        // The currents, by the walking move each starts in the same table's order (Route 17, Route 18)
        [0x94] = ("A current that carries a surfer east", TileBehavior.CurrentEast),
        [0x95] = ("A current that carries a surfer west", TileBehavior.CurrentWest),
        [0x96] = ("A current that carries a surfer north", TileBehavior.CurrentNorth),
        [0x97] = ("A current that carries a surfer south", TileBehavior.CurrentSouth),
        [0xBE] = ("A catwalk", TileBehavior.Catwalk),
        [0xBF] = ("A catwalk's end, where it is stepped onto", TileBehavior.Catwalk),
        [0xD4] = ("A reception counter", TileBehavior.Counter),
        [0xD6] = ("A PC", TileBehavior.Computer),
        [0xD8] = ("A television", TileBehavior.Television),
        [0xD9] = ("A bookcase", TileBehavior.Bookshelf),
        [0xDA] = ("A bookcase of a second kind", TileBehavior.Bookshelf),
        [0xDB] = ("A bookcase of a third kind", TileBehavior.Bookshelf),
        [0xDC] = ("A bookcase of a fourth kind", TileBehavior.Bookshelf),
        [0xDD] = ("A trash can", TileBehavior.TrashCan),
        [0xDE] = ("A trash can of a second kind", TileBehavior.TrashCan),
        [0xDF] = ("A shelf of goods", TileBehavior.ShopShelf),
        [0xE0] = ("A shelf of goods of a second kind", TileBehavior.ShopShelf),
        [0xE1] = ("A shelf of goods of a third kind", TileBehavior.ShopShelf),
        [0xE2] = ("A vending machine: an event's business", TileBehavior.None),
        [0xFE] = ("Plain, blocked", TileBehavior.None),
        [0xFF] = ("No class: blocked", TileBehavior.None),
        [0xFFFF] = ("No class: blocked", TileBehavior.None),
    };

    /// <summary>The class of a tile type: its low sixteen bits.</summary>
    public static int ClassOf(long type) => (int)(type & 0xFFFF);

    /// <summary>The flags of a tile type: its high sixteen bits.</summary>
    public static int FlagsOf(long type) => (int)((type >> 16) & 0xFFFF);

    /// <summary>Whether a tile of this type can't be walked onto: its blocking flag, or a class that is none.</summary>
    public static bool Blocked(long type) => (FlagsOf(type) & BlockedFlag) != 0 || ClassOf(type) is 0xFF or 0xFFFF;

    /// <summary>What a class becomes; <paramref name="known"/> false for a class in no row, which becomes <see cref="TileBehavior.None"/>.</summary>
    public static TileBehavior Of(int tileClass, out bool known)
    {
        known = Table.TryGetValue(tileClass, out var row);
        return known ? row.Becomes : TileBehavior.None;
    }

    public static TileBehavior Of(int tileClass) => Of(tileClass, out _);
}
