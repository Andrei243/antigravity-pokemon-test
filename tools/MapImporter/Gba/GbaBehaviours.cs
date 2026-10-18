using PokemonPlatinumEngine.Overworld;

namespace MapImporter.Gba;

/// <summary>
/// What each of the Game Boy Advance's metatile behaviours (<c>MB_*</c>, by name, since FireRed and Emerald number
/// them differently) becomes among this game's <see cref="TileBehavior"/> values (plan 18 · W6). Platinum's own value
/// where Platinum has one that means the same; one of the values the original leaves unused at the top of its byte
/// (<c>0xED</c> to <c>0xFD</c>) where it has none; and <see cref="TileBehavior.None"/> for what only says how a tile
/// is read or drawn (a sign, a cupboard, a poster), which is an event's or the art's business. A name the table
/// doesn't know is <see cref="TileBehavior.None"/> too, and the report lists it.
/// </summary>
public static class GbaBehaviours
{
    public static readonly IReadOnlyDictionary<string, TileBehavior> Table = new Dictionary<string, TileBehavior>(StringComparer.Ordinal)
    {
        ["MB_NORMAL"] = TileBehavior.None,
        ["MB_SHORT_GRASS"] = TileBehavior.None,
        ["MB_TALL_GRASS"] = TileBehavior.TallGrass,
        ["MB_ASHGRASS"] = TileBehavior.TallGrass,
        ["MB_LONG_GRASS"] = TileBehavior.VeryTallGrass,
        ["MB_LONG_GRASS_SOUTH_EDGE"] = TileBehavior.VeryTallGrass,
        ["MB_CAVE"] = TileBehavior.CaveFloor,
        ["MB_SAND_CAVE"] = TileBehavior.CaveFloor,
        ["MB_INDOOR_ENCOUNTER"] = TileBehavior.OldChateauFloor,
        ["MB_MOUNTAIN_TOP"] = TileBehavior.MountainFloor,
        ["MB_RUNNING_DISALLOWED"] = TileBehavior.NoRunning,
        ["MB_NO_RUNNING"] = TileBehavior.NoRunning,

        ["MB_POND_WATER"] = TileBehavior.River,
        ["MB_REFLECTION_UNDER_BRIDGE"] = TileBehavior.River,
        ["MB_FAST_WATER"] = TileBehavior.River,
        ["MB_OCEAN_WATER"] = TileBehavior.Sea,
        ["MB_CYCLING_ROAD_WATER"] = TileBehavior.Sea,
        ["MB_UNUSED_WATER"] = TileBehavior.Sea,
        ["MB_DEEP_WATER"] = TileBehavior.DeepWater,
        ["MB_INTERIOR_DEEP_WATER"] = TileBehavior.DeepWater,
        ["MB_SOOTOPOLIS_DEEP_WATER"] = TileBehavior.DeepWater,
        ["MB_UNUSED_SOOTOPOLIS_DEEP_WATER"] = TileBehavior.DeepWater,
        ["MB_UNUSED_SOOTOPOLIS_DEEP_WATER_2"] = TileBehavior.DeepWater,
        ["MB_WATERFALL"] = TileBehavior.Waterfall,
        ["MB_PUDDLE"] = TileBehavior.Puddle,
        ["MB_SHALLOW_WATER"] = TileBehavior.ShallowWater,
        ["MB_HOT_SPRINGS"] = TileBehavior.ShallowWater,
        ["MB_NO_SURFACING"] = TileBehavior.Underwater,
        ["MB_UNDERWATER_BLOCKED_ABOVE"] = TileBehavior.Underwater,
        ["MB_SEAWEED"] = TileBehavior.Seaweed,
        ["MB_SEAWEED_NO_SURFACING"] = TileBehavior.Seaweed,
        ["MB_EASTWARD_CURRENT"] = TileBehavior.CurrentEast,
        ["MB_WESTWARD_CURRENT"] = TileBehavior.CurrentWest,
        ["MB_NORTHWARD_CURRENT"] = TileBehavior.CurrentNorth,
        ["MB_SOUTHWARD_CURRENT"] = TileBehavior.CurrentSouth,

        ["MB_ICE"] = TileBehavior.Ice,
        ["MB_THIN_ICE"] = TileBehavior.ThinIce,
        ["MB_CRACKED_ICE"] = TileBehavior.ThinIce,
        ["MB_SAND"] = TileBehavior.Sand,
        ["MB_DEEP_SAND"] = TileBehavior.Sand,
        ["MB_FOOTPRINTS"] = TileBehavior.Sand,
        ["MB_CRACKED_FLOOR"] = TileBehavior.CrackedFloor,
        ["MB_CRACKED_FLOOR_HOLE"] = TileBehavior.Hole,
        ["MB_FALL_WARP"] = TileBehavior.Hole,
        ["MB_MT_PYRE_HOLE"] = TileBehavior.Hole,
        ["MB_BERRY_TREE_SOIL"] = TileBehavior.BerrySoil,

        ["MB_IMPASSABLE_EAST"] = TileBehavior.BlockEast,
        ["MB_IMPASSABLE_WEST"] = TileBehavior.BlockWest,
        ["MB_IMPASSABLE_NORTH"] = TileBehavior.BlockNorth,
        ["MB_IMPASSABLE_SOUTH"] = TileBehavior.BlockSouth,
        ["MB_IMPASSABLE_NORTHEAST"] = TileBehavior.BlockNorthAndEast,
        ["MB_IMPASSABLE_NORTHWEST"] = TileBehavior.BlockNorthAndWest,
        ["MB_IMPASSABLE_SOUTHEAST"] = TileBehavior.BlockSouthAndEast,
        ["MB_IMPASSABLE_SOUTHWEST"] = TileBehavior.BlockSouthAndWest,
        ["MB_IMPASSABLE_SOUTH_AND_NORTH"] = TileBehavior.BlockNorthAndSouth,
        ["MB_IMPASSABLE_WEST_AND_EAST"] = TileBehavior.BlockEastAndWest,
        ["MB_JUMP_EAST"] = TileBehavior.LedgeEast,
        ["MB_JUMP_WEST"] = TileBehavior.LedgeWest,
        ["MB_JUMP_NORTH"] = TileBehavior.LedgeNorth,
        ["MB_JUMP_SOUTH"] = TileBehavior.LedgeSouth,

        ["MB_WALK_EAST"] = TileBehavior.SlideEast,
        ["MB_WALK_WEST"] = TileBehavior.SlideWest,
        ["MB_WALK_NORTH"] = TileBehavior.SlideNorth,
        ["MB_WALK_SOUTH"] = TileBehavior.SlideSouth,
        ["MB_SLIDE_EAST"] = TileBehavior.SlideEast,
        ["MB_SLIDE_WEST"] = TileBehavior.SlideWest,
        ["MB_SLIDE_NORTH"] = TileBehavior.SlideNorth,
        ["MB_SLIDE_SOUTH"] = TileBehavior.SlideSouth,
        // FireRed's spin arrows are slides, and its stop tile the Spinner: on a map with stop tiles a slide goes on over
        // plain floor until one (plan 19 · K5, part b; FieldMovement.Spins)
        ["MB_SPIN_RIGHT"] = TileBehavior.SlideEast,
        ["MB_SPIN_LEFT"] = TileBehavior.SlideWest,
        ["MB_SPIN_UP"] = TileBehavior.SlideNorth,
        ["MB_SPIN_DOWN"] = TileBehavior.SlideSouth,
        ["MB_STOP_SPINNING"] = TileBehavior.Spinner,

        // Ways somewhere: a door, the mat at a room's edge (an arrow warp), a hole in a cave's wall, a ladder or a pad
        ["MB_WARP_DOOR"] = TileBehavior.Door,
        ["MB_ANIMATED_DOOR"] = TileBehavior.Door,
        ["MB_WATER_DOOR"] = TileBehavior.Door,
        ["MB_PETALBURG_GYM_DOOR"] = TileBehavior.Door,
        ["MB_CAVE_DOOR"] = TileBehavior.EntranceNorth,
        ["MB_NON_ANIMATED_DOOR"] = TileBehavior.EntranceNorth,
        ["MB_EAST_ARROW_WARP"] = TileBehavior.ExitEast,
        ["MB_WEST_ARROW_WARP"] = TileBehavior.ExitWest,
        ["MB_NORTH_ARROW_WARP"] = TileBehavior.ExitNorth,
        ["MB_SOUTH_ARROW_WARP"] = TileBehavior.ExitSouth,
        ["MB_WATER_SOUTH_ARROW_WARP"] = TileBehavior.ExitSouth,
        ["MB_DEEP_SOUTH_WARP"] = TileBehavior.ExitSouth,
        ["MB_UP_RIGHT_STAIR_WARP"] = TileBehavior.StairsEast,
        ["MB_DOWN_RIGHT_STAIR_WARP"] = TileBehavior.StairsEast,
        ["MB_UP_LEFT_STAIR_WARP"] = TileBehavior.StairsWest,
        ["MB_DOWN_LEFT_STAIR_WARP"] = TileBehavior.StairsWest,
        ["MB_LADDER"] = TileBehavior.WarpPanel,
        ["MB_REGULAR_WARP"] = TileBehavior.WarpPanel,
        ["MB_LAVARIDGE_1F_WARP"] = TileBehavior.WarpPanel,
        ["MB_LAVARIDGE_GYM_1F_WARP"] = TileBehavior.WarpPanel,
        ["MB_LAVARIDGE_GYM_B1F_WARP"] = TileBehavior.WarpPanel,
        ["MB_AQUA_HIDEOUT_WARP"] = TileBehavior.WarpPanel,
        ["MB_MOSSDEEP_GYM_WARP"] = TileBehavior.WarpPanel,
        ["MB_BATTLE_PYRAMID_WARP"] = TileBehavior.WarpPanel,
        ["MB_UNION_ROOM_WARP"] = TileBehavior.WarpPanel,
        ["MB_UP_ESCALATOR"] = TileBehavior.Escalator,
        ["MB_DOWN_ESCALATOR"] = TileBehavior.Escalator,

        ["MB_BRIDGE_OVER_OCEAN"] = TileBehavior.BridgeOverWater,
        ["MB_BRIDGE_OVER_POND_LOW"] = TileBehavior.BridgeOverWater,
        ["MB_BRIDGE_OVER_POND_MED"] = TileBehavior.BridgeOverWater,
        ["MB_BRIDGE_OVER_POND_HIGH"] = TileBehavior.BridgeOverWater,
        ["MB_BRIDGE_OVER_POND_MED_EDGE_1"] = TileBehavior.BridgeOverWater,
        ["MB_BRIDGE_OVER_POND_MED_EDGE_2"] = TileBehavior.BridgeOverWater,
        ["MB_BRIDGE_OVER_POND_HIGH_EDGE_1"] = TileBehavior.BridgeOverWater,
        ["MB_BRIDGE_OVER_POND_HIGH_EDGE_2"] = TileBehavior.BridgeOverWater,
        ["MB_PACIFIDLOG_VERTICAL_LOG_TOP"] = TileBehavior.BridgeOverWater,
        ["MB_PACIFIDLOG_VERTICAL_LOG_BOTTOM"] = TileBehavior.BridgeOverWater,
        ["MB_PACIFIDLOG_HORIZONTAL_LOG_LEFT"] = TileBehavior.BridgeOverWater,
        ["MB_PACIFIDLOG_HORIZONTAL_LOG_RIGHT"] = TileBehavior.BridgeOverWater,
        ["MB_FORTREE_BRIDGE"] = TileBehavior.Bridge,
        ["MB_UNUSED_BRIDGE"] = TileBehavior.Bridge,
        ["MB_BIKE_BRIDGE_OVER_BARRIER"] = TileBehavior.Bridge,

        ["MB_ISOLATED_VERTICAL_RAIL"] = TileBehavior.RailNorthSouth,
        ["MB_VERTICAL_RAIL"] = TileBehavior.RailNorthSouth,
        ["MB_ISOLATED_HORIZONTAL_RAIL"] = TileBehavior.RailEastWest,
        ["MB_HORIZONTAL_RAIL"] = TileBehavior.RailEastWest,
        ["MB_MUDDY_SLOPE"] = TileBehavior.BikeSlopeBottom,
        ["MB_BUMPY_SLOPE"] = TileBehavior.BumpySlope,
        ["MB_CYCLING_ROAD_PULL_DOWN"] = TileBehavior.CyclingRoadPull,
        ["MB_CYCLING_ROAD_PULL_DOWN_GRASS"] = TileBehavior.CyclingRoadPull,

        ["MB_SECRET_BASE_SPOT_RED_CAVE"] = TileBehavior.SecretBaseSpot,
        ["MB_SECRET_BASE_SPOT_RED_CAVE_OPEN"] = TileBehavior.SecretBaseSpot,
        ["MB_SECRET_BASE_SPOT_BROWN_CAVE"] = TileBehavior.SecretBaseSpot,
        ["MB_SECRET_BASE_SPOT_BROWN_CAVE_OPEN"] = TileBehavior.SecretBaseSpot,
        ["MB_SECRET_BASE_SPOT_YELLOW_CAVE"] = TileBehavior.SecretBaseSpot,
        ["MB_SECRET_BASE_SPOT_YELLOW_CAVE_OPEN"] = TileBehavior.SecretBaseSpot,
        ["MB_SECRET_BASE_SPOT_TREE_LEFT"] = TileBehavior.SecretBaseSpot,
        ["MB_SECRET_BASE_SPOT_TREE_LEFT_OPEN"] = TileBehavior.SecretBaseSpot,
        ["MB_SECRET_BASE_SPOT_SHRUB"] = TileBehavior.SecretBaseSpot,
        ["MB_SECRET_BASE_SPOT_SHRUB_OPEN"] = TileBehavior.SecretBaseSpot,
        ["MB_SECRET_BASE_SPOT_BLUE_CAVE"] = TileBehavior.SecretBaseSpot,
        ["MB_SECRET_BASE_SPOT_BLUE_CAVE_OPEN"] = TileBehavior.SecretBaseSpot,
        ["MB_SECRET_BASE_SPOT_TREE_RIGHT"] = TileBehavior.SecretBaseSpot,
        ["MB_SECRET_BASE_SPOT_TREE_RIGHT_OPEN"] = TileBehavior.SecretBaseSpot,

        // Furniture Platinum has a value for
        ["MB_COUNTER"] = TileBehavior.Counter,
        ["MB_PC"] = TileBehavior.Computer,
        ["MB_PLAYER_ROOM_PC_ON"] = TileBehavior.Computer,
        ["MB_SECRET_BASE_PC"] = TileBehavior.Computer,
        ["MB_SECRET_BASE_REGISTER_PC"] = TileBehavior.Computer,
        ["MB_COMPUTER"] = TileBehavior.Computer,
        ["MB_REGION_MAP"] = TileBehavior.TownMap,
        ["MB_TELEVISION"] = TileBehavior.Television,
        ["MB_BOOKSHELF"] = TileBehavior.Bookshelf,
        ["MB_POKEMON_CENTER_BOOKSHELF"] = TileBehavior.Bookshelf,
        ["MB_PICTURE_BOOK_SHELF"] = TileBehavior.SmallBookshelf,
        ["MB_POKEMART_SHELF"] = TileBehavior.ShopShelf,
        ["MB_SHOP_SHELF"] = TileBehavior.ShopShelf,
        ["MB_TRASH_BIN"] = TileBehavior.TrashCan,
        ["MB_TRASH_CAN"] = TileBehavior.TrashCan,
    };

    /// <summary>The value a behaviour's name becomes, and whether the table names it.</summary>
    public static TileBehavior Of(string? name, out bool known)
    {
        known = name != null && Table.ContainsKey(name);
        return known ? Table[name!] : TileBehavior.None;
    }

    public static TileBehavior Of(string? name) => Of(name, out _);
}
