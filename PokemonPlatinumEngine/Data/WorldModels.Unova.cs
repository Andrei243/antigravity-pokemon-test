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
        // (Black 2's Season Research Lab, on Route 6: the first catalogue took its name for a ticket machine, plan 22 · U7)
        Built("sikiken", BuildingKind.Lab, "the Season Research Lab on Route 6, in its fenced garden", Architecture.Airfield, sign: "SEASON LAB"),
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

        // ---------------------------------------------------------------- Route 16, Anville Town (t03_, tt_) and the Relic Castle (d4)
        // Plan 22 · U6, part c: the gate house at Route 16's east end toward the Marvelous Bridge, Anville's way of
        // building (Architecture.Railyard), its cars and turntable, the Lostlorn Forest's stumps, the Relic Passage's
        // rocks and the Relic Castle's stairs, sand hills, sand pits and heaps of treasure
        Built("h5_gate", BuildingKind.Gate, "the gate house at Route 16's east end, toward the Marvelous Bridge"),
        Built("tt_house01", BuildingKind.House, "a house of Anville Town", Architecture.Railyard),
        Thing("tt_train01", PropType.Tram, "an old train car standing on Anville's turntable"),
        Thing("tt_train11", PropType.Tram, "the train from Nimbasa standing at Anville's station"),
        Ground("tt_table01", "the rim of Anville's turntable (the ground draws it)"),
        Thing("stump", PropType.Boulder, "a tree stump in the Lostlorn Forest"),
        Thing("drock02", PropType.Boulder, "a large rock in the Relic Passage"),
        Ground("d4stair_u01", "stairs up in the Relic Castle: the warp's tile is the way"),
        Ground("d4stair_d01", "stairs down in the Relic Castle: the warp's tile is the way"),
        Ground("d4shill01", "a hill of sand in the Relic Castle (the ground draws it)"),
        Ground("quicksand", "a sand pit in the Relic Castle's floor, which drops whoever steps on it to the floor below"),
        Thing("d4treasure01", PropType.Crates, "a heap of the Relic Castle's old treasure"),
        Thing("d4treasure02", PropType.Crates, "a heap of the Relic Castle's old treasure"),

        // ---------------------------------------------------------------- Mistralton City (c06_, c6_), Route 6 and 7, Chargestone Cave (d6_) and the Celestial Tower
        // Plan 22 · U7, part a: Mistralton's way of building (Architecture.Airfield), its runway's planes and control tower
        // and the greenhouses round it, Route 6's Season Research Lab among the town's houses, Route 7's windmill and
        // the Celestial Tower, Chargestone Cave's charged crystals and the floors that spark, and the Gym's and the
        // lab's furnishings
        Built("c06_gym", BuildingKind.Gym, "Mistralton City's Pokémon Gym, Skyla's, built like a hangar", Architecture.Airfield),
        Built("c6_house", BuildingKind.House, "a house of Mistralton City, the cargo hall among them", Architecture.Airfield),
        Built("c6_bhouse", BuildingKind.House, "a greenhouse round Mistralton's runway, where the vegetables for the planes are grown", Architecture.Airfield),
        Built("c5_house", BuildingKind.House, "a house of Route 6, built like the port's", Architecture.Storehouse),
        Built("t7_house", BuildingKind.House, "a house of Lentimas Town, built like Mistralton's", Architecture.Airfield),
        Built("c6_ctower", BuildingKind.Tower, "Mistralton City's control tower", Architecture.Airfield),
        Built("r7_toh", BuildingKind.Tower, "the Celestial Tower at the end of Route 7, where Pokémon are laid to rest"),
        Thing("c6_plan", PropType.Airplane, "a small aeroplane on Mistralton's apron or runway"),
        Thing("c7_windmill", PropType.WindTurbine, "a windmill on the high ground of Route 7"),
        Thing("drock01", PropType.Boulder, "a rock in Mistralton Cave"),
        Thing("d6_denkiiwa01", PropType.ChargeStone, "a cluster of charged crystals in Chargestone Cave, which glows"),
        Ground("d6_yuka_biri02", "a patch of Chargestone Cave's floor that crackles with charge (the ground draws it)"),
        Ground("d6_yuka_biri03", "a patch of Chargestone Cave's floor that crackles with charge (the ground draws it)"),
        Ground("d0stair_u01", "stairs up in a cave: the warp's tile is the way"),
        Ground("d0stair_d01", "stairs down in a cave: the warp's tile is the way"),
        Furniture("toh_bell01", PropType.Statue, "the great bell at the top of the Celestial Tower"),
        Furniture("machine_l01", PropType.LabMachine, "a machine of the Season Research Lab"),
        Furniture("poster01", PropType.Painting, "a poster on a wall"),
        Furniture("win03", PropType.Window, "a window"),

        // ---------------------------------------------------------------- Reversal Mountain, the Strange House, Undella Town, Route 13 and Lacunosa Town
        // Plan 22 · U7, part b: the Strange House at the slopes' end and its old furniture, the machine hall on the western
        // slope, Undella Town's houses (stand-ins in the Resort's way of building until the town has one of its own) with the
        // beach's shades and chairs, its gate, a cottage of Route 13 and Lacunosa Town's houses (stand-ins in the Stone's way
        // until the town has one of its own)
        Built("d25_mhouse", BuildingKind.House, "the Strange House at the end of Reversal Mountain's slopes", Architecture.HalfTimber, until: "plan 22 · U10"),
        Built("d7_machine", BuildingKind.Lab, "the machine hall on Reversal Mountain's western slope", Architecture.Airfield, until: "plan 22 · U10"),
        Built("st_house01", BuildingKind.House, "a house of Undella Town", Architecture.Resort, until: "plan 22 · U10"),
        Built("st_house02", BuildingKind.House, "a house of Undella Town", Architecture.Resort, until: "plan 22 · U10"),
        Built("st_house03", BuildingKind.House, "the long house of Undella Town, the visitors' house", Architecture.Resort, until: "plan 22 · U10"),
        Built("h6_gate", BuildingKind.Gate, "the gate house between Undella Town and Route 13"),
        Built("mcottage", BuildingKind.House, "a cottage of Route 13", Architecture.Cottage, until: "plan 22 · U10"),
        Built("tg_house01", BuildingKind.House, "a house of Lacunosa Town", Architecture.Stone, until: "plan 22 · U10"),
        Built("tg_house02", BuildingKind.House, "the storyteller's house in Lacunosa Town", Architecture.Stone, until: "plan 22 · U10"),
        Built("tg_house04", BuildingKind.House, "a small house of Lacunosa Town", Architecture.Stone, until: "plan 22 · U10"),
        Thing("s_parasol", PropType.Pavilion, "a shade of striped cloth on Undella's beach"),
        Thing("o_parasol", PropType.Pavilion, "a shade of striped cloth on Undella's beach"),
        Thing("o_chair", PropType.Bench, "a deck chair on Undella's beach"),
        Furniture("d25_obj01", PropType.Table, "a long low chest of the Strange House"),
        Furniture("d25_obj01_b", PropType.Table, "a long low chest of the Strange House"),
        Furniture("d25_obj01_c", PropType.Table, "a long low chest of the Strange House"),
        Furniture("d25_obj02", PropType.Statue, "a tall old cabinet of the Strange House"),
        Furniture("d25_obj02_b", PropType.Statue, "a tall old cabinet of the Strange House"),
        Furniture("d25_obj02_c", PropType.Statue, "a tall old cabinet of the Strange House"),
        Furniture("d25_obj02_d", PropType.Statue, "a tall old cabinet of the Strange House"),
        Furniture("d25_obj03", PropType.Statue, "a stand clock of the Strange House"),
        Furniture("d25_obj03_b", PropType.Statue, "a stand clock of the Strange House"),
        Furniture("d25_obj04", PropType.Table, "the long table of the Strange House's rooms"),
        Furniture("d25_obj05", PropType.Table, "a long table of the Strange House's rooms"),

        // ---------------------------------------------------------------- Opelucid City (c8_), the Village Bridge (h04_) and the mall (mall)
        // Plan 22 · U8, part a: Opelucid's buildings stand in Blackthorn's way of building (dragon-blue gables on grey stone) and its modern blocks
        // in Jubilife's, the Village Bridge's in the harbour's, until the towns have ways of their own (the style guide's "Buildings" first)
        // (and since part b in Opelucid's own way, Skyline: pale panels under dragon-violet hips)
        Built("c8_buildb", BuildingKind.House, "a modern block of Opelucid City", Architecture.Skyline),
        Built("c8_gym", BuildingKind.Gym, "Opelucid City's Dragon Gym", Architecture.Skyline),
        Built("h04_house", BuildingKind.House, "a long house of the Village Bridge", Architecture.Harbour, until: "plan 22 · U10"),
        Built("h04_ligth", BuildingKind.House, "the great house on the Village Bridge's island", Architecture.Harbour, until: "plan 22 · U10"),
        Built("h04_ligth2", BuildingKind.House, "a small bright house of the Village Bridge", Architecture.Harbour, until: "plan 22 · U10"),
        Built("h3_gate", BuildingKind.Gate, "a gate house of the Village Bridge and Opelucid City"),
        Built("mall", BuildingKind.Shop, "Shopping Mall Nine on Route 9", Architecture.City, until: "plan 22 · U10"),

        // ---------------------------------------------------------------- Humilau City (c14_) and the Plasma Frigate's rooms
        // Plan 22 · U8, part b: Humilau's houses on stilts over the sea and its Gym in its own way of building (Stilts), the
        // fishing boat moored among them, and the Frigate's warp panels and ice cannon
        Built("c14_house", BuildingKind.House, "a house of Humilau City on its stilts over the sea", Architecture.Stilts),
        Built("c14_gym", BuildingKind.Gym, "Marlon's Gym in Humilau City", Architecture.Stilts),
        Thing("c14_boat", PropType.Boat, "a boat moored among Humilau's boardwalks"),
        Furniture("warp01", PropType.WarpPanel, "a warp panel of the Plasma Frigate (the panels' rides are the overlays' exits)"),
        Furniture("p_gun01", PropType.LabMachine, "the Plasma Frigate's ice cannon, over Kyurem's chamber"),

        // ---------------------------------------------------------------- the Giant Chasm
        // Plan 22 · U9, part a: the Plasma Frigate at rest on the ice in the middle of the crater (the ground under it is the crater's)
        Ground("c03_ship_03", "the Plasma Frigate at rest in the Giant Chasm's crater (not drawn, as at its other moorings: its ramp is the crater's warp into the ramp hall)"),
    };

    private static readonly Dictionary<string, WorldModel> UnovaByName = UnovaTable.ToDictionary(m => m.Name, StringComparer.Ordinal);

    /// <summary>Every model of Unova's own catalogue.</summary>
    public static IReadOnlyList<WorldModel> Unova => UnovaTable;
}
