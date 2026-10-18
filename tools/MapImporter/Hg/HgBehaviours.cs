using PokemonPlatinumEngine.Overworld;

namespace MapImporter.Hg;

/// <summary>
/// HeartGold's tile behaviours as this game's (plan 20 · J1). HGSS keeps Platinum's table (the same engine) but for
/// a handful of values, and names only some of its values (<c>include/constants/metatile_behavior.h</c>: the rest
/// are bare numbers, <c>TILE_BEHAVIOR_4</c>). A value HGSS names is mapped by its name onto Platinum's value of the
/// same meaning, wherever that stands; a bare number is Platinum's value of that number. Where HGSS names a value
/// Platinum has no meaning for, it keeps HGSS's number if Platinum leaves that number unused (Headbutt at 0x06,
/// Whirlpool at 0x11, Stop Sliding at 0x4D), and otherwise moves to one Platinum leaves unused (magma, which HGSS
/// puts where Platinum has its reflective floor, and the three ladders, which stand where Platinum has a ledge's
/// corners), 0xE6 to 0xE9. Nothing of Johto is in the game yet; J2 names the new values in <see cref="TileBehavior"/>.
/// </summary>
public static class HgBehaviours
{
    /// <summary>HGSS's own behaviours that Platinum has no value for, at the values the import writes them as.</summary>
    public const byte Headbutt = 0x06, Whirlpool = 0x11, StopSliding = 0x4D, Magma = 0xE6, LadderNorth = 0xE7, LadderSouth = 0xE8, LadderDown = 0xE9;

    /// <summary>Each name HGSS gives a value, and the value it becomes.</summary>
    public static readonly IReadOnlyDictionary<string, byte> ByName = new Dictionary<string, byte>(StringComparer.Ordinal)
    {
        ["1_UNUSED"] = (byte)TileBehavior.None,
        ["TALL_GRASS"] = (byte)TileBehavior.TallGrass,
        ["VERY_TALL_GRASS"] = (byte)TileBehavior.VeryTallGrass,
        ["HEADBUTT"] = Headbutt,
        ["CAVE_FLOOR"] = (byte)TileBehavior.CaveFloor,
        ["WATER_RIVER"] = (byte)TileBehavior.River,
        ["WHIRLPOOL"] = Whirlpool,
        ["WATERFALL"] = (byte)TileBehavior.Waterfall,
        ["WATER_SEA"] = (byte)TileBehavior.Sea,
        ["PUDDLE"] = (byte)TileBehavior.Puddle,
        ["SHALLOW_WATER"] = (byte)TileBehavior.ShallowWater,
        ["PUDDLE_NO_SPLASHING"] = (byte)TileBehavior.StillPuddle,
        ["ICE"] = (byte)TileBehavior.Ice,
        ["SAND"] = (byte)TileBehavior.Sand,
        ["MAGMA"] = Magma,
        ["REFLECTIVE"] = (byte)TileBehavior.ShinyFloor,
        ["JUMP_EAST"] = (byte)TileBehavior.LedgeEast,
        ["JUMP_WEST"] = (byte)TileBehavior.LedgeWest,
        ["JUMP_NORTH"] = (byte)TileBehavior.LedgeNorth,
        ["JUMP_SOUTH"] = (byte)TileBehavior.LedgeSouth,
        ["LADDER_NORTH"] = LadderNorth,
        ["LADDER_SOUTH"] = LadderSouth,
        ["LADDER_DOWN"] = LadderDown,
        ["SLIDE_EAST"] = (byte)TileBehavior.SlideEast,
        ["SLIDE_WEST"] = (byte)TileBehavior.SlideWest,
        ["SLIDE_NORTH"] = (byte)TileBehavior.SlideNorth,
        ["SLIDE_SOUTH"] = (byte)TileBehavior.SlideSouth,
        ["ROCK_CLIMB_NORTH_SOUTH"] = (byte)TileBehavior.RockClimbNorthSouth,
        ["ROCK_CLIMB_EAST_WEST"] = (byte)TileBehavior.RockClimbEastWest,
        ["STOP_SLIDING"] = StopSliding,
        ["WARP_STAIRS_EAST"] = (byte)TileBehavior.StairsEast,
        ["WARP_STAIRS_WEST"] = (byte)TileBehavior.StairsWest,
        ["WARP_ENTRANCE_EAST"] = (byte)TileBehavior.EntranceEast,
        ["WARP_ENTRANCE_WEST"] = (byte)TileBehavior.EntranceWest,
        ["WARP_ENTRANCE_NORTH"] = (byte)TileBehavior.EntranceNorth,
        ["WARP_ENTRANCE_SOUTH"] = (byte)TileBehavior.EntranceSouth,
        ["WARP_PANEL"] = (byte)TileBehavior.WarpPanel,
        ["DOOR"] = (byte)TileBehavior.Door,
        ["ESCALATOR_FLIP_FACE"] = (byte)TileBehavior.EscalatorFacingBack,
        ["ESCALATOR"] = (byte)TileBehavior.Escalator,
        ["WARP_EAST"] = (byte)TileBehavior.ExitEast,
        ["WARP_WEST"] = (byte)TileBehavior.ExitWest,
        ["WARP_NORTH"] = (byte)TileBehavior.ExitNorth,
        ["WARP_SOUTH"] = (byte)TileBehavior.ExitSouth,
        ["PC"] = (byte)TileBehavior.Computer,
        ["TOWN_MAP"] = (byte)TileBehavior.TownMap,
        ["TV"] = (byte)TileBehavior.Television,
        ["MUD"] = (byte)TileBehavior.Mud,
        ["SNOW"] = (byte)TileBehavior.ShallowSnow,
        ["SMALL_BOOKSHELF_1"] = (byte)TileBehavior.SmallBookshelf,
        ["BOOKSHELF_1"] = (byte)TileBehavior.Bookshelf,
        ["BOOKSHELF_2"] = (byte)TileBehavior.TallBookshelf,
        ["EMPTY_TRASH_CAN"] = (byte)TileBehavior.TrashCan,
        ["MART_SHELF_1"] = (byte)TileBehavior.ShopShelf,
        ["SMALL_BOOKSHELF_2"] = 0xEA,   // Platinum's own shelves, which the engine leaves unnamed
        ["MART_SHELF_2"] = 0xEB,
        ["MART_SHELF_3"] = 0xEC,
        ["NONE"] = 0xFF
    };

    /// <summary>The value a HeartGold behaviour becomes; <paramref name="known"/> is false for a name the table lacks (which keeps its number).</summary>
    public static byte Of(byte value, IReadOnlyDictionary<int, string> names, out bool known)
    {
        known = true;
        if (!names.TryGetValue(value, out string? name)) return value;
        if (ByName.TryGetValue(name, out byte becomes)) return becomes;
        known = false;
        return value;
    }

    /// <summary>A table of every value 0 to 255 to what it becomes, for reading a whole archive.</summary>
    public static byte[] Table(IReadOnlyDictionary<int, string> names)
    {
        var table = new byte[256];
        for (int v = 0; v < 256; v++) table[v] = Of((byte)v, names, out _);
        return table;
    }
}
