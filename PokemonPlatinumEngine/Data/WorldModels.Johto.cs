using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Data;

/// <summary>
/// Johto's catalogue (plan 20 · J2): HeartGold's models by their short names, as they stand on the chunks of the areas
/// open so far, and what each is here. A name Platinum's catalogue has too (<c>pc</c>, <c>fs</c>, <c>gate_a</c>,
/// <c>p_door</c>) is taken from Sinnoh's table, since the original's model of that name is the same building in both
/// games. What stands in a room (<c>bm_room.narc</c>: a bed, a table, a television) is a piece of furniture, laid a
/// tile at a time by <c>WorldMapBuilder.FurnishModels</c>; what stands outdoors is a building, a thing, a door or part
/// of the ground, as Sinnoh's are. Each later session of plan 20 adds the rows of the areas it opens.
/// </summary>
public static partial class WorldModels
{
    private static WorldModel Furniture(string name, PropType prop, string what) => new(name, ModelRole.Scenery) { Prop = prop, What = what };

    private static readonly WorldModel[] JohtoTable =
    {
        // ---------------------------------------------------------------- New Bark Town (wk_: the original's Wakaba)
        Built("wk_labo", BuildingKind.Lab, "Professor Elm's lab", Architecture.Breeze),
        House("wk_hhouse", Architecture.Breeze, "the player's house", storeys: 2),
        House("wk_h01", Architecture.Breeze, "a house of New Bark Town"),
        Thing("wk_sp1", PropType.WindTurbine, "one of New Bark Town's wind turbines, turning in the wind off the sea"),
        Ground("wind", "the wind over New Bark Town (no model of its own: the town's turbines turn in it)"),
        Door("wk_door1"), Door("wk_door2"), Door("wk_door3"),

        // ---------------------------------------------------------------- Cherrygrove City (yo_: Yoshino) and Route 30
        House("yo_h01", Architecture.Bayside, "a house of Cherrygrove City"),
        House("yo_h02", Architecture.Bayside, "the Guide Gent's house in Cherrygrove City"),
        Thing("yo_sp2", PropType.Planter, "a planter of flowers beside a house of Cherrygrove City"),
        Door("yo_door1"),
        House("r30_h01", Architecture.Breeze, "Mr. Pokémon's house at the north end of Route 30"),

        // ---------------------------------------------------------------- Violet City (kk_: Kikyou), in view from Route 31's
        // east end; the city opens with the next stretch of plan 20, which gives Sprout Tower its look
        House("kk_h01", Architecture.OldTimber, "a house of Violet City"),
        Built("kk_pc", BuildingKind.PokemonCenter, "Violet City's Pokémon Center"),
        Built("kk_fs", BuildingKind.PokeMart, "Violet City's Poké Mart"),
        Built("kk_gym", BuildingKind.Gym, "the Violet Gym"),
        Built("kk_school", BuildingKind.School, "the Pokémon Academy in Violet City", Architecture.OldTimber),
        Built("kk_mada", BuildingKind.Tower, "Sprout Tower, the old pagoda in its pond", Architecture.OldTimber, until: "plan 20 · J5 (Violet City and Sprout Tower)"),
        Thing("kk_tourou", PropType.LampPost, "a stone lantern of Violet City"),
        Ground("kk_bridge", "a bridge over Sprout Tower's pond: its own ground draws it"),
        Door("kk_door1"), Door("kk_door2"), Door("kk_door3"), Door("gym_door"),

        // ---------------------------------------------------------------- the Ruins of Alph (ar_), in view from Violet City's west
        // edge; they open with Chapter 1's chambers, plan 20 · J4
        Built("ar_center", BuildingKind.Lab, "the research centre at the Ruins of Alph", until: "plan 20 · J4 (the Ruins of Alph)"),
        Built("ar_study", BuildingKind.Lab, "the scientists' hut at the Ruins of Alph", until: "plan 20 · J4 (the Ruins of Alph)"),
        Built("ar_shrine1", BuildingKind.Shrine, "a chamber of the Ruins of Alph", until: "plan 20 · J4 (the Ruins of Alph)"),
        Built("ar_shrine1_2", BuildingKind.Shrine, "a chamber of the Ruins of Alph", until: "plan 20 · J4 (the Ruins of Alph)"),
        Built("ar_shrine1_3", BuildingKind.Shrine, "a chamber of the Ruins of Alph", until: "plan 20 · J4 (the Ruins of Alph)"),
        Built("ar_shrine1_4", BuildingKind.Shrine, "a chamber of the Ruins of Alph", until: "plan 20 · J4 (the Ruins of Alph)"),
        Built("ar_shrine1_5", BuildingKind.Shrine, "a chamber of the Ruins of Alph", until: "plan 20 · J4 (the Ruins of Alph)"),
        Built("ar_shrine1_6", BuildingKind.Shrine, "a chamber of the Ruins of Alph", until: "plan 20 · J4 (the Ruins of Alph)"),
        Built("ar_shrine1_7", BuildingKind.Shrine, "a chamber of the Ruins of Alph", until: "plan 20 · J4 (the Ruins of Alph)"),
        Built("ar_shrine2", BuildingKind.Shrine, "a chamber of the Ruins of Alph", until: "plan 20 · J4 (the Ruins of Alph)"),
        Built("ar_shrine3", BuildingKind.Shrine, "a chamber of the Ruins of Alph", until: "plan 20 · J4 (the Ruins of Alph)"),
        Door("ar_study_door"),
        Ground("kk_sky", "the sky over Violet City (no model of its own: the sky is the field's)"),
        Ground("object01", "an unnamed marker of the original's: nothing to draw"),

        // ---------------------------------------------------------------- Azalea Town (hw_: Hiwada), in view from Route 32's south
        // end; the town opens with plan 20 · J5, which gives it its own way of building
        Built("hw_h01", BuildingKind.House, "a house of Azalea Town", Architecture.OldTimber, until: "plan 20 · J5 (Azalea Town)"),
        Ground("hw_ido", "Azalea Town's well", until: "plan 20 · J5 (Azalea Town)"),
        Ground("hw_kemuri", "the smoke over Kurt's chimney", until: "plan 20 · J5 (Azalea Town)"),
        Ground("hw_maki", "a stack of firewood in Azalea Town", until: "plan 20 · J5 (Azalea Town)"),
        Door("hw_door1"),

        // ---------------------------------------------------------------- Sprout Tower's floors (plan 20 · J4): furniture
        Furniture("d15_light", PropType.LampPost, "a lantern of Sprout Tower"),
        Furniture("d15_obj", PropType.Statue, "a carved Bellsprout of Sprout Tower"),
        Furniture("d15_pillar", PropType.Column, "the swaying pillar at Sprout Tower's heart"),
        Ground("d15_stairup", "a ladder up: the warp's tile is the way"),
        Ground("d15_stairdw", "a ladder down: the warp's tile is the way"),

        // ---------------------------------------------------------------- the signs every town and route has: the tile draws its signpost
        Ground("board_a", "a small signpost: the sign event's tile draws it"),
        Ground("board_b", "a signpost: the sign event's tile draws it"),
        Ground("board_c", "a town's or a route's signpost: the sign event's tile draws it"),
        Ground("board_e", "a signpost: the sign event's tile draws it"),
        Ground("board_f", "a signpost: the sign event's tile draws it"),

        // ---------------------------------------------------------------- rooms (bm_room.narc): furniture
        Furniture("table02_2", PropType.Table, "a table"),
        Furniture("table_pc01", PropType.Table, "a table in a Pokémon Center's or a Mart's corner"),
        Furniture("table01_3", PropType.Table, "a long desk of the Pokémon Academy"),
        Ground("book02", "a book open on a desk"),
        Furniture("table_l01", PropType.LabDesk, "a desk"),
        Furniture("chair01", PropType.Chair, "a chair"),
        Furniture("chair02", PropType.Chair, "a chair"),
        Furniture("chair_pc01", PropType.Chair, "a seat in a Pokémon Center"),
        Furniture("chair_pc02", PropType.Chair, "a seat in a Pokémon Center"),
        Furniture("sofa01_l", PropType.Sofa, "a sofa"),
        Furniture("sofa01_r", PropType.Sofa, "a sofa"),
        Furniture("tv01", PropType.Television, "a television"),
        Furniture("plant01", PropType.Plant, "a plant in a pot"),
        Furniture("plant02_x", PropType.Plant, "a large plant in a gate house"),
        Furniture("ref01", PropType.Fridge, "a fridge"),
        Furniture("sink01", PropType.KitchenCounter, "a kitchen counter with its sink"),
        Furniture("shelf01", PropType.Bookshelf, "a shelf"),
        Furniture("shelf03", PropType.Bookshelf, "a shelf"),
        Furniture("shelf05", PropType.Bookshelf, "a shelf"),
        Furniture("shelf07", PropType.Bookshelf, "a narrow shelf"),
        Furniture("shelf08", PropType.Bookshelf, "a narrow shelf"),
        Furniture("shelf09a", PropType.Bookshelf, "a tall bookcase"),
        Furniture("shelf09b", PropType.Bookshelf, "a tall bookcase of the lab"),
        Furniture("bed_h01", PropType.Bed, "a bed"),
        Furniture("game_h01", PropType.Computer, "a games console before the television"),
        Furniture("clock01", PropType.Clock, "a clock on the wall"),
        Furniture("poster01", PropType.Painting, "a poster on the wall"),
        Furniture("dustbox01", PropType.TrashCan, "a bin"),
        Furniture("machine_l01", PropType.LabMachine, "a machine of the lab"),
        Furniture("machine_l03", PropType.LabMachine, "a machine of the lab"),
        Furniture("kaifuku_lab01", PropType.HealingMachine, "the lab's healing machine"),
        Furniture("labo_select", PropType.LabMachine, "the lab's machine that keeps the three Poké Balls"),
        Furniture("pc01", PropType.Computer, "a PC"),
        Furniture("machine_pc01", PropType.HealingMachine, "the Pokémon Center's healing machine"),
        Furniture("machine_pc02", PropType.Counter, "the Pokémon Center's counter"),
        Furniture("counter_pc04", PropType.Counter, "the end of a Pokémon Center's counter"),
        Furniture("stair_pc_d02", PropType.SideStairsDown, "the Pokémon Center's stairs down"),
        Furniture("stair_pc_u02", PropType.SideStairsUp, "the Pokémon Center's stairs up"),
        Furniture("shelf_fs02", PropType.StoreShelf, "a Mart's shelf"),
        Furniture("shelf_fs04", PropType.StoreShelf, "a Mart's shelf against the wall"),
        Furniture("shelf_fs06", PropType.StoreShelf, "a Mart's shelf"),
        Furniture("counter_fs01", PropType.Counter, "a Mart's counter"),
        Furniture("box01", PropType.Crates, "boxes"),
        // What the floor or the wall draws: mats, sheets of paper, posters, a map, doors in the back wall, flowers on a counter
        Ground("d_mat01", "the mat inside a door: the floor draws it"), Ground("d_mat04", "the mat at the foot of the stairs"),
        Ground("sheet_pc01", "a sheet on a counter"), Ground("sheet_fs02", "a sheet on a counter"),
        Ground("machine_pc03", "the front of the healing machine"), Ground("t_map01", "the region's map on the wall"),
        Ground("door_pc01", "a door in a Pokémon Center's back wall"), Ground("door_wi01", "a door in the back wall"),
        Ground("flower_pc01", "flowers on the counter"), Ground("poster_fs01", "a poster on the wall"),
    };

    private static readonly Dictionary<string, WorldModel> JohtoByName = JohtoTable.ToDictionary(m => m.Name, StringComparer.Ordinal);

    /// <summary>Every model of Johto's own catalogue.</summary>
    public static IReadOnlyList<WorldModel> Johto => JohtoTable;

    /// <summary>
    /// What a model is in a region's catalogue (<see cref="Region.Models"/>'s name; the world's region name does as
    /// well): Johto's table, then Sinnoh's for the names the two games share; Sinnoh's alone for Sinnoh and for a
    /// region whose catalogue has no file of its own.
    /// </summary>
    public static WorldModel? Of(string name, string? catalogue) =>
        string.Equals(catalogue, "johto", StringComparison.OrdinalIgnoreCase) ? JohtoByName.GetValueOrDefault(name) ?? Of(name) : Of(name);
}
