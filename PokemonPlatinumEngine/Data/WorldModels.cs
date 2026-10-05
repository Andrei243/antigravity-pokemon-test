using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Data;

/// <summary>What the game makes of one of the original's models when it stands on a map of the world.</summary>
public enum ModelRole
{
    /// <summary>Walls and a roof on the tiles the model blocks.</summary>
    Building,
    /// <summary>Something of ours that stands on the ground where the model does.</summary>
    Scenery,
    /// <summary>A building's door. The building's own art shows it; plan 04 · G9 makes it open.</summary>
    Door,
    /// <summary>Part of the ground: a waterfall, a lake's surface, a ramp. The tiles under it draw it.</summary>
    Terrain
}

/// <summary>
/// One model of the imported world by its short name, and what stands in the game where it stands in the
/// original. The name, the box and the place are all the import brings; the look is ours.
/// </summary>
public sealed record WorldModel(string Name, ModelRole Role)
{
    public BuildingKind Kind { get; init; }

    /// <summary>The town whose way of building it follows; null for buildings that look the same everywhere.</summary>
    public Architecture? Town { get; init; }

    /// <summary>Storeys, or 0 to go by how tall the model stands.</summary>
    public int Storeys { get; init; }

    /// <summary>The name over the door of a business, in our own words.</summary>
    public string? Sign { get; init; }

    public PropType Prop { get; init; }

    /// <summary>
    /// For a building whose thin pieces are not a fence or a low wall: what they are. The mine's yard has
    /// <see cref="PropType.Conveyor"/> here, and <see cref="WorldMapBuilder.Conveyors"/> lays its belts.
    /// </summary>
    public PropType? Thin { get; init; }

    /// <summary>
    /// The session of plan 01 that builds its area and gives it a look of its own; until then it has a plain
    /// stand-in of the right size. Null for a model that has its look.
    /// </summary>
    public string? StandInUntil { get; init; }

    /// <summary>What it is, in a few words.</summary>
    public string What { get; init; } = "";
}

/// <summary>
/// The catalogue of the models that stand outdoors in Sinnoh (plan 01 · M4): 163 of the original's 360, the rest
/// being furniture; and of those that stand in the caves that are open (plan 01 · M5 on). <c>tools/MapImporter</c> lists them with their sizes and places in <c>docs/world-models.md</c>
/// and reports any that this table doesn't know.
/// </summary>
public static class WorldModels
{
    private static WorldModel House(string name, Architecture town, string what, int storeys = 0) =>
        new(name, ModelRole.Building) { Kind = BuildingKind.House, Town = town, Storeys = storeys, What = what };

    private static WorldModel Built(string name, BuildingKind kind, string what, Architecture? town = null, int storeys = 0, string? sign = null, string? until = null,
        PropType? thin = null) =>
        new(name, ModelRole.Building) { Kind = kind, Town = town, Storeys = storeys, Sign = sign, StandInUntil = until, Thin = thin, What = what };

    private static WorldModel Thing(string name, PropType prop, string what, string? until = null) =>
        new(name, ModelRole.Scenery) { Prop = prop, StandInUntil = until, What = what };

    private static WorldModel Door(string name) => new(name, ModelRole.Door) { What = "a door" };

    private static WorldModel Ground(string name, string what, string? until = null) =>
        new(name, ModelRole.Terrain) { StandInUntil = until, What = what };

    private static readonly WorldModel[] Table =
    {
        // ---------------------------------------------------------------- everywhere
        Built("pc", BuildingKind.PokemonCenter, "a Pokémon Center"),
        Built("pc_01", BuildingKind.PokemonCenter, "Snowpoint's Pokémon Center, under snow", Architecture.Snow),
        Built("fs", BuildingKind.PokeMart, "a Poké Mart"),
        Built("fs_01", BuildingKind.PokeMart, "Snowpoint's Poké Mart, under snow", Architecture.Snow),
        Built("gym00", BuildingKind.Gym, "a Gym, in the colours of its leader's type"),
        Built("gate_a", BuildingKind.Gate, "a gate house on a road that runs north and south"),
        Built("gate_b", BuildingKind.Gate, "a gate house on a road that runs east and west"),
        Thing("treeeff", PropType.HoneyTree, "a honey tree"),
        Thing("funsui", PropType.Fountain, "a fountain"),
        Door("p_door"), Door("gym_door00"), Door("c1_door1"), Door("c3_door1"), Door("c3_door2"), Door("c4_door1"), Door("c5_door_s"),
        Door("t1_door1"), Door("t2_door1"), Door("t2_door2"), Door("t3_door1"), Door("l2_door1"), Door("d3_door1"),
        Ground("wfall3_4", "a waterfall"), Ground("wfall3_5", "a waterfall"), Ground("wfall16_5", "a waterfall"), Ground("wfall11_14", "a waterfall"),
        Ground("l_lake", "the surface of a lake"), Ground("l_lake_l4", "the surface of a lake"),
        Ground("cy_slope", "a ramp for the Bicycle", until: "M6"),

        // ---------------------------------------------------------------- towns
        House("t1_h01", Architecture.Timber, "a house of Twinleaf Town"),
        House("t1_s01", Architecture.Timber, "the rival's house", storeys: 2),
        House("t1_s02", Architecture.Timber, "the player's house", storeys: 2),
        House("t2_h01", Architecture.Plaster, "a house of Sandgem Town"),
        Built("t2_s01", BuildingKind.Lab, "Professor Rowan's lab"),
        House("t2_s02", Architecture.Plaster, "the assistant's house", storeys: 2),
        House("t3_h01", Architecture.Cottage, "a house of Floaroma Town"),
        Built("t3_s01", BuildingKind.Shop, "Floaroma's flower shop", Architecture.Cottage, sign: "FLOWERS"),
        House("t4_h01", Architecture.Farm, "a house of Solaceon Town"),
        Built("t4_s01", BuildingKind.Shop, "the Pokémon Day Care, with its fenced yard", Architecture.Farm, sign: "DAY CARE"),
        House("t5_s01", Architecture.HalfTimber, "the elder's house in Celestic Town", storeys: 2),
        Built("t5_s02", BuildingKind.Shrine, "Celestic Town's shrine"),
        Built("t5_o01", BuildingKind.Shrine, "a small shrine in Celestic Town"),
        Built("t5_o01b", BuildingKind.Shrine, "a small shrine in Celestic Town"),
        House("t6_h01", Architecture.Resort, "a house of the Battle Zone"),
        Built("t6_s01", BuildingKind.Shop, "the Battleground", Architecture.Resort, sign: "BATTLE", until: "M10"),
        Built("t7_s01", BuildingKind.Hall, "the Ribbon Syndicate", Architecture.Resort, storeys: 2, sign: "RIBBONS", until: "M10"),
        House("t7_s02", Architecture.Resort, "the Villa"),
        Built("t7_s03", BuildingKind.House, "a pavilion in the Resort Area", Architecture.Resort, until: "M10"),

        // ---------------------------------------------------------------- Jubilife City
        Built("c1_b01a", BuildingKind.Apartments, "a block of flats with a way in", Architecture.City),
        Built("c1_b01c", BuildingKind.Apartments, "a block of flats", Architecture.City),
        Built("c1_b02a", BuildingKind.Apartments, "the condominiums", Architecture.City),
        Built("c1_b02c", BuildingKind.Apartments, "a block of flats", Architecture.City),
        Built("c1_b03", BuildingKind.Apartments, "a tall block", Architecture.City),
        Built("c1_s01", BuildingKind.Office, "the Pokétch Company", storeys: 4),
        Built("c1_s02", BuildingKind.TvStation, "Jubilife TV", storeys: 4),
        Built("c1_s03", BuildingKind.Terminal, "the Global Terminal", storeys: 3),
        Built("c1_school", BuildingKind.School, "the Trainers' School"),

        // ---------------------------------------------------------------- Canalave City
        House("c2_h01a", Architecture.Harbour, "a house of Canalave City"),
        Built("c2_s02", BuildingKind.Library, "Canalave Library", Architecture.Harbour, storeys: 3, sign: "LIBRARY"),
        Ground("c2_s03a", "the west leaf of Canalave's drawbridge", until: "M7"),
        Ground("c2_s03b", "the east leaf of Canalave's drawbridge", until: "M7"),
        Thing("c2_o02", PropType.Boat, "a ship at its pier"),
        Thing("c2_o04", PropType.Crates, "cargo on the quay"),

        // ---------------------------------------------------------------- Oreburgh City
        Built("c3_b01a", BuildingKind.Apartments, "a block of miners' flats", Architecture.Brick),
        House("c3_h01a", Architecture.Brick, "a house of Oreburgh City"),
        House("c3_h01b", Architecture.Brick, "a house of Oreburgh City"),
        Built("c3_s01", BuildingKind.Museum, "the Oreburgh Mining Museum", Architecture.Brick, sign: "MUSEUM"),
        Built("c3_s02", BuildingKind.Factory, "the mine's winding tower", sign: "MINE"),
        Built("c3_s03", BuildingKind.Factory, "the mine's yard: the pit head, and conveyors on their gantries", storeys: 1, thin: PropType.Conveyor),
        Built("c3_o02", BuildingKind.Factory, "the mine's sorting shed", storeys: 1),
        Thing("c3_o01a", PropType.CoalHeap, "a heap of coal"),
        Thing("c3_o01b", PropType.CoalHeap, "a heap of coal"),
        // Inside the mine
        Built("d01_o1", BuildingKind.Factory, "the mine's loading machine, with a conveyor down either side of the coal face", storeys: 1, thin: PropType.Conveyor),
        Thing("can01", PropType.Drums, "steel drums in the mine"),
        Thing("box02", PropType.Crates, "crates in the mine"),

        // ---------------------------------------------------------------- Eterna City
        House("c4_h01a", Architecture.HalfTimber, "a house of Eterna City or Celestic Town"),
        Built("c4_s01", BuildingKind.Shop, "Eterna's cycle shop", Architecture.HalfTimber, sign: "CYCLES"),
        Thing("c4_s03", PropType.Statue, "the statue of Eterna City"),
        Built("c4_s04", BuildingKind.Galactic, "Team Galactic's Eterna building", storeys: 4),
        Thing("c4_o01", PropType.Hedge, "a clipped hedge"),

        // ---------------------------------------------------------------- Hearthome City
        Built("c5_b01", BuildingKind.Apartments, "a block of flats in Hearthome City", Architecture.Townhouse),
        House("c5_h01", Architecture.Townhouse, "a house of Hearthome City"),
        Built("c5_s01", BuildingKind.Shop, "the Pokémon Fan Club", Architecture.Townhouse, sign: "FAN CLUB"),
        Built("c5_s02", BuildingKind.Chapel, "the Foreign Building"),
        Built("c5_s03", BuildingKind.Hall, "the Contest Hall", sign: "CONTEST"),
        Thing("c5_o03", PropType.Fountain, "a fountain"),
        Thing("c5_o01", PropType.Bench, "a bench"),
        Thing("c5_o01b", PropType.Bench, "a bench"),

        // ---------------------------------------------------------------- Pastoria City
        House("c6_h01", Architecture.Marsh, "a house of Pastoria City"),
        Built("c06_s01", BuildingKind.Hall, "the Great Marsh's gate and lookout", Architecture.Marsh, storeys: 2, sign: "MARSH"),
        Thing("c06_s02", PropType.Boat, "a boat at its pier", until: "M7"),

        // ---------------------------------------------------------------- Veilstone City
        House("c7_h01", Architecture.Stone, "a house of Veilstone City"),
        Built("c7_s01", BuildingKind.Shop, "the Game Corner", Architecture.Stone, sign: "GAMES"),
        Built("c7_s02a", BuildingKind.Warehouse, "Team Galactic's warehouse, with a way in"),
        Built("c7_s02b", BuildingKind.Warehouse, "a warehouse of Team Galactic"),
        Built("c7_s03", BuildingKind.Galactic, "Team Galactic's headquarters", storeys: 5),
        Built("c7_s04", BuildingKind.Shop, "the Veilstone Department Store", Architecture.Stone, storeys: 5, sign: "STORE"),
        Thing("c7_o02", PropType.Mast, "a mast beside Team Galactic's headquarters"),
        Thing("c7_o01a", PropType.Hedge, "something low beside the warehouses", until: "M7"),

        // ---------------------------------------------------------------- Sunyshore City
        House("c8_h01", Architecture.Seaside, "a house of Sunyshore City"),
        Built("c8_s02", BuildingKind.Shop, "the Sunyshore Market", Architecture.Seaside, sign: "MARKET"),
        Built("c8_s03", BuildingKind.Lighthouse, "the Vista Lighthouse"),
        Thing("c8_o02", PropType.Outcrop, "a rock standing off Sunyshore's shore", until: "M8"),

        // ---------------------------------------------------------------- Snowpoint City
        House("c9_h01", Architecture.Snow, "a house of Snowpoint City"),
        Built("c9_s01", BuildingKind.Temple, "Snowpoint Temple"),
        Built("c9_o01", BuildingKind.Warehouse, "a store shed at Snowpoint's harbour", Architecture.Snow, until: "M8"),
        Thing("c9_o02", PropType.Hedge, "something low under the snow", until: "M8"),
        Thing("c9_o02b", PropType.Hedge, "something low under the snow", until: "M8"),
        Thing("c9_o03", PropType.Crates, "cargo on the quay"),
        Thing("c09_s02", PropType.Boat, "the ferry between Snowpoint City and the Fight Area"),

        // ---------------------------------------------------------------- the Pokémon League and the Battle Zone
        Built("c10_s01", BuildingKind.League, "the Pokémon League", storeys: 3),
        Thing("c10_s02", PropType.LampPost, "a lantern on the League's steps"),
        Thing("c11_o01", PropType.Hedge, "planting along the Fight Area's quay", until: "M10"),
        Built("d31_s01", BuildingKind.Tower, "the Battle Tower", until: "M10"),
        Built("d31_s02", BuildingKind.Hall, "a hall of the Battle Park", until: "M10"),
        Thing("d31_o01", PropType.LampPost, "a lantern of the Battle Park"),
        Built("d32_s01", BuildingKind.Hall, "the Battle Castle", sign: "CASTLE", until: "M10"),
        Built("d32_s02", BuildingKind.Hall, "the Battle Hall", sign: "HALL", until: "M10"),
        Built("d32_s03", BuildingKind.Hall, "the Battle Factory", sign: "FACTORY", until: "M10"),
        Built("d32_s04", BuildingKind.Hall, "the Battle Arcade", sign: "ARCADE", until: "M10"),
        Built("d32_s05", BuildingKind.Warehouse, "a low building of the Battle Frontier", until: "M10"),
        Built("d32_s06", BuildingKind.Warehouse, "a low building of the Fight Area", until: "M10"),
        Ground("d16_o01", "lava on Stark Mountain's flank", until: "M10"),
        Thing("d16_o02", PropType.Outcrop, "the mouth of Stark Mountain", until: "M10"),

        // ---------------------------------------------------------------- along the routes
        Built("d2_s02", BuildingKind.Factory, "the Valley Windworks", sign: "WINDWORKS"),
        Thing("d2_s01", PropType.WindTurbine, "a wind turbine"),
        Thing("d2_s01a", PropType.WindTurbine, "a wind turbine"),
        Thing("d2_s01b", PropType.WindTurbine, "a wind turbine"),
        Built("d4_s01", BuildingKind.Factory, "the Fuego Ironworks", sign: "IRONWORKS"),
        Built("r206s01", BuildingKind.Gate, "the gate at the foot of Cycling Road"),
        Built("r209s01", BuildingKind.Tower, "the Lost Tower"),
        Thing("r209s02", PropType.Cairn, "the Hallowed Tower"),
        Built("r210h02", BuildingKind.Shop, "the Café Cabin", Architecture.Farm, sign: "CAFE"),
        Built("r212s01", BuildingKind.Mansion, "the Pokémon Mansion"),
        Thing("r212s02", PropType.Topiary, "a clipped tree, at the Mansion and in Canalave City"),
        Thing("r212s03", PropType.Topiary, "a clipped tree of the Mansion's garden"),
        Built("d23_yane", BuildingKind.Mansion, "the Mansion's back, over the Trophy Garden"),
        Built("r213s01", BuildingKind.Hotel, "the Hotel Grand Lake", Architecture.Resort, storeys: 2, sign: "HOTEL"),
        Built("r213s02", BuildingKind.House, "a pavilion of the Hotel Grand Lake", Architecture.Resort, until: "M7"),
        Built("l2_s01", BuildingKind.Shop, "the restaurant at Valor Lakefront", Architecture.Resort, storeys: 2, sign: "DINER"),
        House("l2_s02a", Architecture.Resort, "a cottage of the Hotel Grand Lake"),
        Built("r221s01", BuildingKind.Hall, "Pal Park", sign: "PAL PARK"),
        Thing("r224o01", PropType.Cairn, "the white rock of Route 224"),
        Thing("board_d", PropType.Billboard, "a billboard"),

        // ---------------------------------------------------------------- places of their own
        Thing("d11s01", PropType.Column, "a standing stone of Amity Square", until: "M6"),
        Thing("d11_o01a", PropType.Bench, "a bench in Amity Square"),
        Thing("d11_o01b", PropType.Bench, "a bench in Amity Square"),
        Thing("d11_o02a", PropType.Hedge, "a flower bed of Amity Square", until: "M6"),
        Thing("d11_o02b", PropType.Hedge, "a flower bed of Amity Square", until: "M6"),
        Thing("d6_o01", PropType.Crates, "the Great Marsh's tram", until: "M7"),
        Thing("d6_o02", PropType.Cairn, "the Great Marsh's lookout glasses", until: "M7"),
        Thing("d5_colum01", PropType.Column, "a column of Spear Pillar"), Thing("d5_colum02", PropType.Column, "a column of Spear Pillar"),
        Thing("d5_colum03", PropType.Column, "a column of Spear Pillar"), Thing("d5_colum04", PropType.Column, "a column of Spear Pillar"),
        Thing("d5_colum05", PropType.Column, "a broken column of Spear Pillar"), Thing("d5_colum06", PropType.Column, "a broken column of Spear Pillar"),
        Thing("d5_colum07", PropType.Column, "a fallen column of Spear Pillar"),
        Thing("d5_colum01x", PropType.Column, "a column of the Hall of Origin"), Thing("d5_colum02x", PropType.Column, "a column of the Hall of Origin"),
        Thing("d5_colum03x", PropType.Column, "a column of the Hall of Origin"), Thing("d5_colum04x", PropType.Column, "a column of the Hall of Origin"),
        Thing("d5_colum05x", PropType.Column, "a broken column of the Hall of Origin"), Thing("d5_colum06x", PropType.Column, "a broken column of the Hall of Origin"),
        Thing("d5_colum07x", PropType.Column, "a fallen column of the Hall of Origin"),
        Ground("d5_ana_d", "a rift at Spear Pillar", until: "M8"), Ground("d5_ana_p", "a rift at Spear Pillar", until: "M8"),
        Ground("d5_ana_pl", "the rift's shadow on Spear Pillar's floor", until: "M8")
    };

    private static readonly Dictionary<string, WorldModel> ByName = Table.ToDictionary(m => m.Name, StringComparer.Ordinal);

    /// <summary>Every model the catalogue knows.</summary>
    public static IReadOnlyList<WorldModel> All => Table;

    /// <summary>What a model is, by the original's short name for it; null for a name the catalogue doesn't know.</summary>
    public static WorldModel? Of(string name) => ByName.GetValueOrDefault(name);

    /// <summary>The type of the leader whose Gym stands in an area, by the area's key; null where there is none.</summary>
    public static PokemonType? GymTheme(string areaKey) => areaKey switch
    {
        "oreburgh_city" => PokemonType.Rock,
        "eterna_city" => PokemonType.Grass,
        "hearthome_city" => PokemonType.Ghost,
        "veilstone_city" => PokemonType.Fighting,
        "pastoria_city" => PokemonType.Water,
        "canalave_city" => PokemonType.Steel,
        "snowpoint_city" => PokemonType.Ice,
        "sunyshore_city" => PokemonType.Electric,
        _ => null
    };
}
