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

        // ---------------------------------------------------------------- Castelia City (c03_, c3_)
        // Plan 22 · U5, part c: Castelia's way of building (Architecture.Metropolis). Its towers are drawn into each
        // street's own model with no prop of their own; the reader writes a prop of ours for each block of them
        Built("c3_tower", BuildingKind.Tower, "a tower of Castelia City, drawn into its street's own model (a prop of ours, plan 22 · U5, part c)", Architecture.Metropolis),
        Built("c03_gym", BuildingKind.Gym, "Castelia's Pokémon Gym, Burgh's", Architecture.Metropolis),
        Thing("c03_slamp", PropType.LampPost, "a street lamp of Castelia"),
        Thing("c03_ship", PropType.Boat, "a ship at one of Castelia's piers"),
        Thing("c03_fountain", PropType.Fountain, "the fountain in the middle of Castelia's plaza"),
        Thing("out_vender01", PropType.VendingMachine, "a vending machine in the street"),
        Ground("c03_cloud", "the clouds over Castelia's piers (no model of its own: the sky is the field's)"),
        Ground("c03_kanban", "a sign on a tower's face: the sign event's tile draws it"),
        Ground("door_bar", "the door of a café on Narrow Street (the warp's tile draws it)"),

        // ---------------------------------------------------------------- Route 4 and Nimbasa City (r4_, c04, c4_)
        // Route 4 opened for Colress's scene (plan 22 · U5, part c); its houses of flats, Join Avenue's gate and Nimbasa
        // City since plan 22 · U6: the flats are Castelia's panel blocks, Nimbasa builds in its own way (Architecture.Funfair)
        Built("r4_build", BuildingKind.Apartments, "a house of flats on Route 4, built as the road grows", Architecture.Metropolis),
        Built("buill", BuildingKind.Apartments, "a block of flats by Nimbasa City's squares", Architecture.Funfair),
        Built("jagate", BuildingKind.Gate, "the gate into Join Avenue"),
        // (an archway walked under, at the amusement park's way in from the city and inside it: its ground is the way's)
        Ground("c4_gate", "an archway of Nimbasa's amusement park, walked under (no model of its own: the way through is open ground)"),
        Built("c04station", BuildingKind.Terminal, "Nimbasa City's Gear Station", Architecture.Funfair),
        Built("thouse", BuildingKind.House, "a house of Nimbasa City", Architecture.Funfair),
        Built("c04ballpark", BuildingKind.Hall, "Nimbasa City's Big Stadium", Architecture.Funfair, sign: "STADIUM"),
        Built("c04tcourt", BuildingKind.Hall, "Nimbasa City's Small Court", Architecture.Funfair, sign: "COURT"),
        Built("c04theater", BuildingKind.Hall, "Nimbasa City's Musical Theater", Architecture.Funfair, sign: "MUSICAL"),
        Built("c04_gym", BuildingKind.Gym, "Nimbasa City's Pokémon Gym, Elesa's, in the amusement park", Architecture.Funfair),
        Built("c04_poke01", BuildingKind.Shop, "a stall of Nimbasa's amusement park", Architecture.Funfair),
        Built("c04_poke02", BuildingKind.Shop, "a stall of Nimbasa's amusement park", Architecture.Funfair),
        Built("c04_fwheel", BuildingKind.Hall, "the amusement park's Ferris wheel (a hall stands in until its wheel is drawn)", Architecture.Funfair,
            sign: "FERRIS WHEEL", until: "plan 22 · U6, part b"),
        Thing("c04_plant", PropType.Planter, "a planter of the amusement park"),
        // ---------------------------------------------------------------- the Desert Resort (d4_)
        Thing("d4_ruins", PropType.Outcrop, "ruins half buried in the Desert Resort's sand"),
        Thing("roadcone", PropType.Bollard, "a road cone of Route 4's works"),
        Thing("sandbag", PropType.Crates, "sandbags of Route 4's works"),
        Thing("fream", PropType.Crates, "a frame of scaffolding of Route 4's works"),
        Thing("fountain", PropType.Fountain, "a fountain by Nimbasa City's gate"),

        // ---------------------------------------------------------------- rooms: the houses
        Furniture("chair01", PropType.Chair, "a chair"),
        Furniture("chair03", PropType.Chair, "a chair"),
        Furniture("chushion01", PropType.Cushion, "a floor cushion"),
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
        Furniture("pcdesk01", PropType.Computer, "a desk with a PC on it"),
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
        Furniture("m_gym03_mayu01", PropType.Column, "a cocoon of honey in the Castelia Gym"),
        Furniture("m_gym03_mayu02", PropType.Column, "a cocoon of honey in the Castelia Gym"),
        Furniture("m_gym03_mayu03", PropType.Column, "a cocoon of honey in the Castelia Gym"),

        // ---------------------------------------------------------------- Driftveil City (c5_) and its rooms
        // Plan 22 · U6, part b: Driftveil's way of building (Architecture.Storehouse), the port's red-brick warehouses,
        // the market, the old church where the former Team Plasma live, Clay's Gym, the gates at the drawbridge's foot
        // and south to the World Tournament, and the tournament's hall in view beyond
        Built("c5_rbuild", BuildingKind.House, "a red-brick building of Driftveil City, its houses among them", Architecture.Storehouse),
        Built("c5_church", BuildingKind.Chapel, "the old church on the hill by Driftveil's Gym, where the former Team Plasma live", Architecture.Storehouse),
        Built("c5_market", BuildingKind.Shop, "Driftveil Market", Architecture.Storehouse, sign: "MARKET"),
        Built("c5_build", BuildingKind.Gym, "Driftveil City's Pokémon Gym, Clay's", Architecture.Storehouse),
        Built("rgate", BuildingKind.Gate, "the gate house at the foot of the Driftveil Drawbridge"),
        Built("wbtgate", BuildingKind.Gate, "the gate south of Driftveil City toward the Pokémon World Tournament"),
        Built("c5_wbt", BuildingKind.Hall, "the Pokémon World Tournament's hall, south of Driftveil City", Architecture.Storehouse, sign: "PWT"),
        Thing("c5_statue", PropType.Statue, "the statue at the drawbridge's foot in Driftveil City"),
        Thing("c5_box", PropType.Crates, "crates stacked on Driftveil's quay"),
        Thing("wbt_fountain", PropType.Fountain, "the fountain before the World Tournament's hall"),
        Thing("sikiken", PropType.VendingMachine, "a ticket machine in the street"),
        // (rooms)
        Furniture("object02", PropType.Statue, "a statue in the Musical Theater's lobby"),
        Furniture("in_vender01", PropType.VendingMachine, "a vending machine"),
        Furniture("box01", PropType.Crates, "a pile of boxes"),
        Furniture("box02", PropType.Crates, "a pile of boxes"),
        Furniture("cashier01", PropType.Counter, "a till on Driftveil Market's counter"),
        Furniture("shelf06", PropType.Bookshelf, "a shelf"),
        Furniture("monitor01", PropType.Computer, "the Battle Institute's screen"),
        Ground("elevator01", "a lift's doors in a house of flats (the room's walls are its own)"),
        Ground("warp02", "the turnstiles to a stadium's field (the room's floor is its own)"),
        Ground("c04_object01", "a piece of the old roller coaster's track (the room's walls are its own)"),
    };

    private static readonly Dictionary<string, WorldModel> UnovaByName = UnovaTable.ToDictionary(m => m.Name, StringComparer.Ordinal);

    /// <summary>Every model of Unova's own catalogue.</summary>
    public static IReadOnlyList<WorldModel> Unova => UnovaTable;
}
