using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Data;

/// <summary>
/// Unova's catalogue (plan 22 · U2): Black 2's props by the names the decompilation's folders give them, as they stand on
/// the chunks of the areas open so far and the ring round them, and what each is here. Every building is a stand-in in
/// one of Sinnoh's ways of building (<see cref="WorldModel.StandInUntil"/>) until the session that gives its town a way
/// of its own, a row of the style guide's "Buildings" first (plan 22's design, "Unova's world folder"): Aspertia's
/// (<see cref="Architecture.Hillside"/>), Floccesy's (<see cref="Architecture.Bellfield"/>) and Virbank's (<see cref="Architecture.Dockyard"/>) since plan 22 · U5. What stands in a
/// room is a piece of furniture, as Johto's are. Unova looks in no other region's table: Black 2's names are its own.
/// </summary>
public static partial class WorldModels
{

    private static readonly WorldModel[] UnovaTable =
    {
        // ---------------------------------------------------------------- Aspertia City (c12_)
        Built("c12_build", BuildingKind.House, "the player's house in Aspertia City", Architecture.Hillside),
        Built("c12_house", BuildingKind.House, "a house of Aspertia City", Architecture.Hillside),
        Built("c12_gym", BuildingKind.School, "Aspertia's Trainers' School, with the Gym behind it", Architecture.Hillside),
        Built("pc", BuildingKind.PokemonCenter, "a Pokémon Center of Unova"),
        Built("gate", BuildingKind.Gate, "a gate house between a town and a route"),
        Ground("c12_sky", "the sky over the Aspertia Outlook (no model of its own: the sky is the field's)"),
        Ground("c12_cloud01", "the clouds below the Aspertia Outlook (no model of its own: the sky is the field's)"),

        // ---------------------------------------------------------------- what stands about in the open
        Thing("rock", PropType.Boulder, "a stone lying in the grass"),
        Thing("searock", PropType.Boulder, "a large stone"),
        Thing("plant", PropType.Planter, "a planter of shrubs"),
        Thing("out_dust01", PropType.TrashCan, "a bin in the street"),
        Ground("sign01", "a signpost: the sign event's tile draws it"),
        Ground("sign02", "a signpost: the sign event's tile draws it"),
        Ground("sign03", "a town's or a route's signpost: the sign event's tile draws it"),
        Ground("sign04", "a signpost: the sign event's tile draws it"),

        // ---------------------------------------------------------------- Floccesy Town (t06_) and the ranch (d22_)
        Built("t06_house", BuildingKind.House, "a house of Floccesy Town", Architecture.Bellfield),
        Built("t06_ctower", BuildingKind.Tower, "Floccesy Town's bell tower", Architecture.Bellfield),
        Built("d22_farm", BuildingKind.House, "the farmhouse of Floccesy Ranch", Architecture.Farm),
        Thing("cart", PropType.Crates, "a cart of the ranch's hay"),

        // ---------------------------------------------------------------- Virbank City (c13_) and the Virbank Complex (d23_)
        // Plan 22 · U5, part b: Virbank's way of building (Architecture.Dockyard), its club's stairs down, the ship
        // to Castelia at the quay and the complex's tanks, stacks and towers
        Built("c13_house", BuildingKind.House, "a house of Virbank City, the ship terminal among them", Architecture.Dockyard),
        Built("nschool", BuildingKind.House, "the long house by the water in Virbank City", Architecture.Dockyard),
        Built("c13_gym", BuildingKind.Gym, "the way down into Roxie's club, the Virbank Gym", Architecture.Dockyard),
        Thing("c13_ship", PropType.Boat, "the ship to Castelia City at Virbank's quay"),
        Thing("d5_container", PropType.Container, "a shipping container on a quay"),
        Thing("c5_fvessel", PropType.Boat, "a boat moored at a quay"),
        Thing("d23_obj", PropType.Headframe, "a tank, a stack or a tower of the Virbank Complex, steel as tall as its model"),
        Thing("d7_inaba", PropType.Container, "a storage shed in the Virbank Complex"),

        // ---------------------------------------------------------------- rooms: the houses
        Furniture("chair01", PropType.Chair, "a chair"),
        Furniture("chair03", PropType.Chair, "a chair"),
        Furniture("chushion02", PropType.Cushion, "a floor cushion"),
        Furniture("table01", PropType.Table, "a table"),
        Furniture("desk01", PropType.Table, "a desk"),
        Furniture("desk02", PropType.Table, "a long desk"),
        Furniture("bed01", PropType.Bed, "a bed"),
        Furniture("tv01", PropType.Television, "a television"),
        Furniture("wii01", PropType.Computer, "a games console under the television"),
        Furniture("sink01", PropType.KitchenCounter, "a kitchen counter with its sink"),
        Furniture("ref01", PropType.Fridge, "a fridge"),
        Furniture("shelf01", PropType.Bookshelf, "a tall shelf"),
        Furniture("shelf02", PropType.Bookshelf, "a low shelf along a wall"),
        Furniture("shelf03", PropType.StoreShelf, "a shelf of goods"),
        Furniture("shelf04", PropType.Bookshelf, "a bookshelf"),
        Furniture("shelf05", PropType.Bookshelf, "a bookshelf"),
        Furniture("plant01", PropType.Plant, "a potted plant"),
        Furniture("vase01", PropType.Plant, "a vase of flowers"),
        Furniture("dust01", PropType.TrashCan, "a bin"),
        Furniture("art01", PropType.Painting, "a picture on the wall"),
        Furniture("win01", PropType.Window, "a window"),
        Furniture("stair_d01", PropType.SideStairsDown, "stairs down"),
        Furniture("stair_u01", PropType.SideStairsUp, "stairs up"),

        // ---------------------------------------------------------------- rooms: the Pokémon Center, the school and the Gym
        // A room's props are numbered apart from the open country's (plan 22 · U2): pc01 here is a computer, where the
        // open country's Pokémon Center is pc
        Furniture("pc01", PropType.Computer, "a PC on its desk"),
        Furniture("pcmachine01", PropType.Statue, "the round machine in the middle of a Pokémon Center"),
        Furniture("pcev01", PropType.Bookshelf, "one of the tall units along a Pokémon Center's back wall"),
        Furniture("pcchair01", PropType.Chair, "a chair in the Pokémon Center"),
        Furniture("pctable01", PropType.Table, "a low table between the Pokémon Center's chairs"),
        Furniture("pcshelf01", PropType.Bookshelf, "a shelf of magazines in the Pokémon Center"),
        Furniture("globe01s", PropType.Statue, "the globe by the Pokémon Center's link counter"),
        Furniture("c12_gymlight", PropType.Column, "a light in the Aspertia Gym"),
        Ground("mado_in5gym","the windows over the Trainers' School's hall (the room's walls are its own)"),
        Ground("c13gym_kabe1", "a wall of the Virbank Gym's stairs down (the room's walls are its own)"),
        Ground("door_c13gym", "the door from the Virbank Gym's stairs into the club (the warp's tile draws it)"),
    };

    private static readonly Dictionary<string, WorldModel> UnovaByName = UnovaTable.ToDictionary(m => m.Name, StringComparer.Ordinal);

    /// <summary>Every model of Unova's own catalogue.</summary>
    public static IReadOnlyList<WorldModel> Unova => UnovaTable;
}
