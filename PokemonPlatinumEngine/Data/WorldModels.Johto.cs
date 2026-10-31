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
        // edge; open since plan 20 · J5
        Built("ar_center", BuildingKind.Lab, "the research centre at the Ruins of Alph"),
        Built("ar_study", BuildingKind.Lab, "the scientists' hut at the Ruins of Alph"),
        Built("ar_shrine1", BuildingKind.Shrine, "a chamber of the Ruins of Alph"),
        Built("ar_shrine1_2", BuildingKind.Shrine, "a chamber of the Ruins of Alph"),
        Built("ar_shrine1_3", BuildingKind.Shrine, "a chamber of the Ruins of Alph"),
        Built("ar_shrine1_4", BuildingKind.Shrine, "a chamber of the Ruins of Alph"),
        Built("ar_shrine1_5", BuildingKind.Shrine, "a chamber of the Ruins of Alph"),
        Built("ar_shrine1_6", BuildingKind.Shrine, "a chamber of the Ruins of Alph"),
        Built("ar_shrine1_7", BuildingKind.Shrine, "a chamber of the Ruins of Alph"),
        Built("ar_shrine2", BuildingKind.Shrine, "a chamber of the Ruins of Alph"),
        Built("ar_shrine3", BuildingKind.Shrine, "a chamber of the Ruins of Alph"),
        Door("ar_study_door"),
        Ground("kk_sky", "the sky over Violet City (no model of its own: the sky is the field's)"),
        Ground("object01", "an unnamed marker of the original's: nothing to draw"),

        // ---------------------------------------------------------------- Azalea Town (hw_: Hiwada), in view from Route 32's south
        // end; the town opens with plan 20 · J5, which gives it its own way of building
        House("hw_h01", Architecture.Woodland, "a house of Azalea Town"),
        House("hw_h02", Architecture.Woodland, "Kurt's house or the charcoal kiln of Azalea Town"),
        Built("hw_gym", BuildingKind.Gym, "the Azalea Gym"),
        Ground("hw_ido", "the Slowpoke Well's mouth, ringed with stones: its own ground draws it"),
        Ground("hw_kemuri", "the smoke over Kurt's chimney (the chimney is the house's)"),
        Ground("hw_maki", "a stack of firewood in Azalea Town: its own ground draws it"),
        Door("hw_door1"), Door("hw_door2"),

        // ---------------------------------------------------------------- Route 34 and Ilex Forest (plan 20 · J5, part b)
        House("r34_educate", Architecture.Bayside, "the Pokémon Day Care on Route 34, with its fenced yard"),
        Door("r34_door1"),
        Thing("uba_hokora", PropType.Statue, "the little wooden shrine deep in Ilex Forest"),
        Ground("uba_eda", "the branches over Ilex Forest's shrine: the forest's own trees draw them"),
        Ground("d36_00_00", "Ilex Forest's canopy (no model of its own here: the forest's trees stand in it)"),
        Ground("d36_00_01", "Ilex Forest's canopy (no model of its own here: the forest's trees stand in it)"),
        Ground("d36_00_02", "Ilex Forest's canopy (no model of its own here: the forest's trees stand in it)"),
        Ground("d36_01_00", "Ilex Forest's canopy (no model of its own here: the forest's trees stand in it)"),
        Ground("d36_01_01", "Ilex Forest's canopy (no model of its own here: the forest's trees stand in it)"),
        Ground("d36_01_02", "Ilex Forest's canopy (no model of its own here: the forest's trees stand in it)"),

        // ---------------------------------------------------------------- Goldenrod City (ko_: Kogane), plan 20 · J5, part b
        House("ko_h01", Architecture.Boulevard, "a house of Goldenrod City"),
        House("ko_seimei", Architecture.Boulevard, "a tall house of Goldenrod City"),
        Built("ko_h03x", BuildingKind.Apartments, "a block of flats in Goldenrod City", Architecture.Boulevard),
        Built("ko_depart", BuildingKind.Shop, "the Goldenrod Department Store", Architecture.Boulevard, storeys: 6, sign: "STORE"),
        Built("ko_game", BuildingKind.Shop, "the Goldenrod Game Corner", Architecture.Boulevard, sign: "GAMES"),
        Built("ko_cycle", BuildingKind.Shop, "the Bike Shop", Architecture.Boulevard, sign: "BIKES"),
        Built("ko_flower", BuildingKind.Shop, "the flower shop", Architecture.Boulevard, sign: "FLOWERS"),
        Built("ko_gym", BuildingKind.Gym, "the Goldenrod Gym"),
        Built("ko_radio", BuildingKind.TvStation, "the Goldenrod Radio Tower", storeys: 5),
        Built("ko_station", BuildingKind.Terminal, "the Magnet Train station", Architecture.Boulevard),
        Built("ko_wifi", BuildingKind.Terminal, "the Global Terminal", storeys: 3),
        Thing("ko_funsui", PropType.Fountain, "a fountain of Goldenrod City"),
        Thing("ko_light", PropType.LampPost, "a street lamp of Goldenrod City"),
        Thing("ko_flag", PropType.Mast, "a banner on its pole in Goldenrod City"),
        Door("ko_door1"),

        // ---------------------------------------------------------------- Route 35 and the National Park (d22_)
        Built("poke_c_gate", BuildingKind.Gate, "the gate house between Route 35, the National Park and the Pokéathlon Dome"),
        Thing("d22_funsui", PropType.Fountain, "the National Park's fountain"),
        Ground("d22_mizu", "the water of the National Park's fountain: its own ground draws it"),
        // Plan 20 · J15: Mt. Silver Cave's falls and the backdrop round its summit
        Ground("dsm_fal1", "a waterfall in Mt. Silver Cave: its own ground draws the water"), Ground("dsm_fal2", "a waterfall in Mt. Silver Cave: its own ground draws the water"), Ground("dsm_bg1", "the peaks round Mt. Silver's summit (no model of its own: the field's sky stands in)"),

        // ---------------------------------------------------------------- Ecruteak City (en_: Enju), plan 20 · J5, part b
        House("en_h01a", Architecture.Heritage, "a house of Ecruteak City"),
        House("en_h01b", Architecture.Heritage, "a house of Ecruteak City"),
        Built("en_pc", BuildingKind.PokemonCenter, "Ecruteak City's Pokémon Center"),
        Built("en_fs", BuildingKind.PokeMart, "Ecruteak City's Poké Mart"),
        Built("en_gym", BuildingKind.Gym, "the Ecruteak Gym"),
        Built("en_kabu", BuildingKind.Hall, "the Ecruteak Dance Theater", Architecture.Heritage),
        Built("en_sekia", BuildingKind.Temple, "the gate before the Bell Tower", Architecture.Heritage),
        Built("en_yake", BuildingKind.Tower, "the Burned Tower, its upper floors lost to the fire", Architecture.Heritage, until: "plan 20 · J7 (the Burned Tower's own charred look)"),
        Built("en_gate01", BuildingKind.Gate, "the gate house to Route 38", Architecture.Heritage),
        Ground("en_gate02", "the wooden arch over Ecruteak City's south road, walked under: the road draws it", until: "plan 20 · J7 (Ecruteak's arches)"),
        Thing("en_light", PropType.LampPost, "a stone lantern of Ecruteak City"),
        Thing("en_sp1", PropType.Statue, "a stone marker of Ecruteak City"),
        Door("en_door1"), Door("en_door2"), Door("en_door3"),

        // ---------------------------------------------------------------- Sprout Tower's floors (plan 20 · J4): furniture
        Furniture("d15_light", PropType.Statue, "a stone lantern of Sprout Tower"),
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
        Furniture("shelf04", PropType.Bookshelf, "a shelf"),
        Furniture("chair_jpn01", PropType.Cushion, "a floor cushion of a Johto house (Kurt's)"),
        Furniture("kamidana", PropType.WallEmblem, "a household shrine on the wall"),
        Furniture("poster02", PropType.Painting, "a poster on the wall"),
        Furniture("radio", PropType.AudioSystem, "a radio"),
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
        // Plan 20 · J5, part b: Goldenrod's and Ecruteak's rooms
        Furniture("counter_fs04", PropType.Counter, "a counter of the Department Store"),
        Furniture("desk_cg", PropType.Counter, "the Game Corner's prize desk"),
        Furniture("dispenser01", PropType.VendingMachine, "a drinks machine on the store's roof"),
        Furniture("gym03_flower", PropType.FlowerStand, "a bed of flowers in the Goldenrod Gym"),
        Furniture("pc05", PropType.Computer, "a PC"),
        Furniture("plant03", PropType.Plant, "a plant in a pot"),
        Furniture("shelf_fs01", PropType.StoreShelf, "a shelf of the Department Store"),
        Furniture("shelf_fs03", PropType.StoreShelf, "a shelf of the Department Store"),
        Furniture("sofa01_d", PropType.Sofa, "a sofa"),
        Furniture("stair_pc_d01", PropType.SideStairsDown, "stairs down"),
        Ground("ele_door1", "a lift's doors in the back wall"),
        Ground("sheet_fs01", "a sheet on a counter"),
        Ground("name_ani", "the Name Rater's scroll on the wall"),
        Ground("obj_yaketa", "the Burned Tower's charred timbers: the floor and its walls draw them"),
        // What the floor or the wall draws: mats, sheets of paper, posters, a map, doors in the back wall, flowers on a counter
        Ground("d_mat01", "the mat inside a door: the floor draws it"), Ground("d_mat04", "the mat at the foot of the stairs"),
        Ground("sheet_pc01", "a sheet on a counter"), Ground("sheet_fs02", "a sheet on a counter"),
        Ground("machine_pc03", "the front of the healing machine"), Ground("t_map01", "the region's map on the wall"),
        Ground("door_pc01", "a door in a Pokémon Center's back wall"), Ground("door_wi01", "a door in the back wall"),
        Ground("flower_pc01", "flowers on the counter"), Ground("poster_fs01", "a poster on the wall"),
        // Plan 20 · J7: the Slowpoke Well, the Azalea and Ecruteak Gyms and the Department Store's lift
        Ground("dun_stairup", "a ladder up: the warp's tile is the way"),
        Ground("dun_stairdw", "a ladder down: the warp's tile is the way"),
        Ground("yado_light", "the light falling down the Slowpoke Well's shaft: the cave's own light stands for it"),
        Ground("gym02rope_1", "the Azalea Gym's web of ropes: the floor between its islands draws it"),
        Ground("gym02rope_2", "the Azalea Gym's web of ropes: the floor between its islands draws it"),
        Ground("gym02switch", "a lever for the Azalea Gym's carts: the sign's tile is read"),
        Ground("gym02switch_r", "a lever for the Azalea Gym's carts: the sign's tile is read"),
        Ground("g04_light1", "a candle of the Ecruteak Gym: the room's own light stands for it"),
        Ground("g04_light2", "a candle of the Ecruteak Gym: the room's own light stands for it"),
        Ground("r07_00bot", "the lift car's floor: the room draws it"),
        Ground("r07_01bot", "the lift car's floor: the room draws it"),
        Ground("r07_cs01", "the lift car's control panel: the attendant works it"),
        Ground("ev_o01", "the lift car's doors in the back wall"),

        // ---------------------------------------------------------------- plan 20 · J8: the west and the sea
        // Route 39's Moomoo Farm (r39_)
        Built("r39_naya", BuildingKind.Warehouse, "the Moomoo Farm's barn", Architecture.Farm),
        House("wk_h02", Architecture.Farm, "the Moomoo Farm's house"),
        Ground("r39_moumou", "the Moomoo Farm's pasture and its fence: the ground and the route's own fences draw them"),
        Door("r39_door1"),
        // Olivine City (as_: the original's Asagi) and its harbour
        House("as_h01", Architecture.Seaport, "a house of Olivine City"),
        Built("as_gym", BuildingKind.Gym, "the Olivine Gym"),
        Built("as_restaurant", BuildingKind.Shop, "the Olivine Café, where the sailors eat", Architecture.Seaport, sign: "CAFE"),
        Built("as_toudai", BuildingKind.Lighthouse, "the Glitter Lighthouse on its point"),
        Built("as_aquagate", BuildingKind.Hall, "Olivine's port hall, where the ship's passengers wait", Architecture.Seaport),
        Thing("as_yacht", PropType.Boat, "a yacht moored in Olivine's harbour"),
        Thing("as_bench", PropType.Bench, "a bench along Olivine's quay"),
        Ground("as_hata", "the strings of flags over Olivine's streets: the town's own light and the sky stand for them"),
        Door("as_door1"),
        // Cianwood City (tn_: Tanba) on its island
        House("tn_h01", Architecture.Cliffside, "a house of Cianwood City"),
        Built("tn_gym", BuildingKind.Gym, "the Cianwood Gym"),
        Built("tn_pharmacy", BuildingKind.Shop, "Cianwood's pharmacy, the oldest in Johto", Architecture.Cliffside, sign: "PHARMACY"),
        Built("tn_photo", BuildingKind.Shop, "the photo studio by Cianwood's shore", Architecture.Cliffside, sign: "PHOTO"),
        Ground("tn_dried", "fish hung to dry on Cianwood's quay: the ground draws its racks"),
        Ground("snap_camera", "the photographer's camera on its stand: he stands beside it"),
        Door("tn_door1"), Door("tn_door2"),
        // The whirlpools on the sea of Route 41 (uzushio): a ring of water
        // turning, crossed only with Whirlpool (style guide, "Water"); their tiles are the original's whirlpool behaviour
        Thing("uzushio", PropType.Whirlpool, "a whirlpool on the sea, three tiles across"),
        Ground("d_uzushio", "the whirling of the deep water round the Whirl Islands' lowest cave: the cave's own water stands for it (its tiles are no whirlpool)"),
        Ground("uzu_taki", "a waterfall in the Whirl Islands: its tiles draw it"),
        // The rooms: the Glitter Lighthouse's floors (bm_room's), its light room (bm_field's) and the Olivine Gym
        Ground("ladder_up", "a ladder up: the warp's tile is the way"),
        Ground("ladder_dw", "a ladder down: the warp's tile is the way"),
        Ground("tou3f_back", "the lighthouse's outer wall seen past its third floor's open side: the room's wall draws it"),
        Ground("toudai_sky", "the sky round the lighthouse's light room: the room's own light stands for it"),
        Ground("ele_door_dun", "the lighthouse lift's doors in the back wall"),
        Ground("mg06_fl1", "the Olivine Gym's floor of steel plates: the floor draws it"),
        Ground("mg06_bot", "the Olivine Gym's steel trim: the floor and its walls draw it"),
        Ground("mg06_swc", "the Olivine Gym's statue of a steel Pokémon behind its Leader: the wall draws it"),
        Ground("d_mat02", "a mat inside a side door: the floor draws it"),
        Furniture("shelf02", PropType.Bookshelf, "a shelf"),

        // ---------------------------------------------------------------- plan 20 · J8, part b: Routes 47 and 48 and the Safari Zone
        Ground("fall_r47", "the great waterfall off Route 47's cliffs: its tiles draw it"),
        // The Safari Zone's gate on Route 48 (msg_: the original's Safari gate)
        Built("msg_gtmn", BuildingKind.Gate, "the Safari Zone's gate house, at the head of its plaza", Architecture.Farm, sign: "SAFARI ZONE"),
        Door("sg_door_op"),
        Thing("msp_f", PropType.Mast, "a pennant on its pole at a corner of the Safari Zone's gate house"),
        Ground("msg_gtob", "the arch over the way into the Safari Zone's plaza: the plaza's own ground and fences draw its posts"),
        Thing("msg_tn01", PropType.Outcrop, "a long block of rock the gate's plaza is laid round"),
        Thing("msg_tn02", PropType.Outcrop, "a long block of rock the gate's plaza is laid round"),
        Thing("msg_tn03", PropType.Outcrop, "a long block of rock the gate's plaza is laid round"),
        Thing("msg_tn04", PropType.Outcrop, "a long block of rock the gate's plaza is laid round"),
        Furniture("msg_pc", PropType.Computer, "the Area Customizer, Baoba's machine in the gate"),

        // ---------------------------------------------------------------- plan 20 · J10, part a: the north
        // Mahogany Town (ch_: the original's Chouji), and the Lake of Rage's two houses, built as the town builds
        House("ch_h01", Architecture.Upland, "a house of Mahogany Town or by the Lake of Rage (the souvenir shop is one)"),
        Built("ch_gym", BuildingKind.Gym, "the Mahogany Gym"),
        Thing("ch_mihari", PropType.Column, "a wooden lookout post at an end of Mahogany's street"),
        Ground("antena_tree", "the tree beside the souvenir shop that hides a radio mast: the town's own trees stand for it"),
        Door("ch_door1"),
        // The Mahogany Gym's three rooms, on the field's models: the room draws their ice and their doors
        Ground("ch_r_cool", "the Mahogany Gym's floor of ice and its walls: the room draws them"),
        Ground("ch_r_door", "a door between the Mahogany Gym's rooms: the warp's tile is the way"),
        // The shade of the Lake of Rage's trees on the grass: the trees cast their own
        Ground("map15_01kokage", "the shade of the trees round the Lake of Rage: the trees cast their own"),
        Ground("map15_02kokage", "the shade of the trees round the Lake of Rage: the trees cast their own"),
        Ground("map16_01kokage", "the shade of the trees round the Lake of Rage: the trees cast their own"),
        Ground("map16_02kokage", "the shade of the trees round the Lake of Rage: the trees cast their own"),
        Ground("map17_01kokage", "the shade of the trees round the Lake of Rage: the trees cast their own"),
        // Blackthorn City (fu_: Fusube) under the mountain, and the Dragon's Den behind it
        House("fu_h01", Architecture.Dragonstone, "a house of Blackthorn City"),
        Built("fu_gym", BuildingKind.Gym, "the Blackthorn Gym"),
        Thing("fu_mon", PropType.Column, "a stone gatepost of the gate across Blackthorn's street"),
        Thing("fu_rock", PropType.Outcrop, "a crag of the mountain standing in Blackthorn City"),
        Door("fu_door1"),
        Ground("g08_00pt", "the Blackthorn Gym's floor over its lava: the room draws it"),
        Ground("g08_01pt", "the Blackthorn Gym's floor over its lava: the room draws it"),
        Ground("g08_02pt", "the Blackthorn Gym's floor over its lava: the room draws it"),
        Built("d17_tmpl", BuildingKind.Shrine, "the Dragon Shrine on its island in the Dragon's Den", Architecture.Dragonstone),
        Door("d17_tpdr"),
        Furniture("ms01_p01", PropType.Column, "a pillar of the Dragon Shrine"),
        Furniture("table02_1", PropType.Table, "a table"),
        // The falls of the north: their tiles draw them
        Ground("fall_suri", "the waterfall inside Mt. Mortar: its tiles draw it"),
        Ground("fall03_01", "a little waterfall on Route 45's mountainside: its tiles draw it"),

        // ---------------------------------------------------------------- plan 20 · J10, part b: the way to the League
        // Kanto's own models (kn_: the original's Kanto) where Routes 26, 27 and 22 cross into it: the houses on the
        // two routes are built as Viridian builds, the nearest town of Kanto's
        House("kn_h01", Architecture.Evergreen, "a house on Route 26 or Route 27"),
        House("to_h01", Architecture.Evergreen, "the house on Route 28, below Mt. Silver (plan 20 · J15)"),
        Door("kn_door2"),
        // The League's reception gate is one room seen from three roads: a face of it on each
        Built("kn_gate", BuildingKind.Gate, "the League's reception gate, its south face where Route 26 comes up to it"),
        Built("kn_gate_r", BuildingKind.Gate, "the League's reception gate, its east face where Route 22 comes from Viridian City"),
        Built("kn_gate_l", BuildingKind.Gate, "the League's reception gate, its west face where Route 28 comes down from Mt. Silver"),
        Ground("fall_toujou", "Tohjo Falls' waterfall over its cave's way through: its tiles draw it"),
        Ground("tkwa_sl", "a short flight of steps in Viridian City's terraces: its tiles draw it"),
        // The Indigo Plateau (pke_: the original's Pokémon League)
        Built("pke_league", BuildingKind.League, "the Pokémon League on the Indigo Plateau, its Pokémon Center and its Elite Four within", storeys: 2),
        Door("pke_door1"),
        Ground("pke_flower", "the Indigo Plateau's plaza and its beds of flowers: its own ground draws them"),
        Ground("pke_gate", "an arch over the way up to the League, walked under: the plaza's own ground draws its posts"),
        Thing("pke_obj01", PropType.LampPost, "a lamp at a corner of the League's plaza"),
        // The League's rooms (bm_room's and the rooms' own): what stands in them is the room's own floor and walls
        Furniture("pc02", PropType.Computer, "a PC"),
        Ground("gof_01", "Will's room: its floor of tiles and its mirrors, which the room draws"),
        Ground("gof_04", "Bruno's room: its floor of stone, which the room draws"),
        Ground("gof_05", "Karen's room: its dark floor, which the room draws"),
        Ground("chp_obj1", "the Champion's room: its floor and its dais, which the room draws"),
        Ground("leage_o03", "the Hall of Fame's machine against the back wall: the room's wall stands for it until plan 20 · J12"),

        // ---------------------------------------------------------------- plan 20 · J11, part b: the takeover's rooms
        // The Radio Tower's upper floors and its deck, and the Department Store's basement
        Furniture("shelf10_01", PropType.Bookshelf, "a tall shelf of records in the Radio Tower"),
        Ground("card_key01", "the Card Key's slot in the third floor's wall: the tile read before it is the way"),
        Furniture("scope01", PropType.Binoculars, "a pair of binoculars on the Radio Tower's observation deck"),
        Ground("radio_sky", "the sky round the Radio Tower's observation deck (no model of its own: the sky is the field's)"),
        Ground("snap_came_in", "a camera on its stand in a studio of the Radio Tower: the room's floor stands for it"),

        // ---------------------------------------------------------------- plan 20 · J13: the Pokéathlon Dome (d49_) and its lobby
        Built("poke_c_dome", BuildingKind.Hall, "the Pokéathlon Dome, the hall at the head of its square", sign: "DOME"),
        Ground("poke_c_dome2", "the Pokéathlon Dome's great roof over the arena: the hall's own roof stands for it"),
        Door("poke_c_door"),
        Thing("poke_c_light", PropType.LampPost, "a floodlight of the Pokéathlon Dome's square"),
        Ground("poke_c_obj", "the Pokéathlon Dome's square, its track and its stands: its own ground draws them"),
        Furniture("psentry_obj", PropType.Counter, "the Pokéathlon Dome's long reception desk"),
        Ground("psentry_rode", "the stripes of the track across the lobby's floor: the floor draws them"),
        Ground("psentry_d", "a bell on a desk of the Pokéathlon Dome's reception"),
        Furniture("pc03", PropType.Computer, "a PC on a desk"),
        Furniture("stair_pc_u01", PropType.SideStairsUp, "stairs up"),

        // ---------------------------------------------------------------- plan 20 · J15, part b: the S.S. Aqua and Vermilion
        // City (kbc_: the original's Kuchiba), built as plan 19 · K5 builds FireRed's Vermilion
        House("kbc_h01", Architecture.Portside, "a house of Vermilion City"),
        House("kbc_h02", Architecture.Portside, "a house of Vermilion City"),
        Built("kbc_love", BuildingKind.Hall, "the Pokémon Fan Club, a heart over its door", Architecture.Portside, sign: "FAN CLUB"),
        Built("kbc_gym", BuildingKind.Gym, "the Vermilion Gym"),
        Built("kbc_port", BuildingKind.Hall, "Vermilion's port hall, where the S.S. Aqua's passengers wait", Architecture.Portside),
        Thing("aqua_go", PropType.Boat, "the S.S. Aqua at her quay"),
        Ground("kbc_sl", "a flight of steps down to Vermilion's quays: its tiles draw it"),
        Door("kbc_dr01"), Door("kbc_dr02"), Door("kbc_dr03"),
        Built("kn_gate_a", BuildingKind.Gate, "the gate house at Vermilion City's north edge, to Route 6"),
        Ground("kn_chika", "the mouth of Diglett's Cave on Route 11 (the route is not open: its tiles draw it)"),
        Ground("maq_dr01", "a door off the S.S. Aqua's corridor: the warp's tile is the way"),
        Ground("mg09_es1", "the Vermilion Gym's electric fittings on its walls: the room's walls draw them"),
        Ground("mg09_es2", "the Vermilion Gym's electric fittings on its walls: the room's walls draw them"),
        Ground("mp3_sb", "the timetable board of Vermilion's port hall: the wall draws it"),
        Furniture("chair03", PropType.Chair, "a chair of the Pokémon Fan Club"),

        // ---------------------------------------------------------------- plan 20 · J15, part c: Route 6, Saffron City (ybc_,
        // ya_: the original's Yamabuki) and the Magnet Train, built in Goldenrod's brick as Kanto's great city
        House("ybc_h01", Architecture.Boulevard, "a house of Saffron City"),
        House("ybc_h02", Architecture.Boulevard, "a house of Saffron City"),
        House("ybc_h03", Architecture.Boulevard, "a house of Saffron City"),
        House("ybc_h04", Architecture.Boulevard, "the Copycat's house in Saffron City, two storeys tall", storeys: 2),
        House("ybc_mmc", Architecture.Boulevard, "Mr. Psychic's house in Saffron City"),
        Built("ybc_lnr", BuildingKind.Terminal, "Saffron City's Magnet Train station", Architecture.Boulevard, sign: "STATION"),
        Built("ybc_fgm", BuildingKind.Hall, "Saffron City's Fighting Dojo", Architecture.Boulevard, sign: "DOJO"),
        Built("ybc_slf", BuildingKind.Office, "Silph Co.'s head office in Saffron City", Architecture.Boulevard, storeys: 3, sign: "SILPH CO."),
        Built("ya_gym", BuildingKind.Gym, "the Saffron Gym"),
        Built("kn_gate_b", BuildingKind.Gate, "a gate house on a road into Saffron City that runs east and west"),
        Built("kn_chika_x", BuildingKind.Hall, "the Underground Path's entrance on Route 6 (the path is shut)"),
        House("ha_h01a", Architecture.Riverside, "a house of Cerulean City, and the one on Route 5 north of Saffron City"),
        Door("ha_dr01"), Door("ybc_dor1"), Door("ybc_dor2"), Door("ybc_dor3"),
        Ground("ymbk_sl", "a flight of steps of Saffron City's raised walks: its tiles draw it"),
        Ground("st_board", "a notice board by a road of Kanto: its tiles draw it"),
        Ground("y_j_warp", "the Saffron Gym's floor of warp panels: the room's tiles draw it"),

        // ---------------------------------------------------------------- plan 20 · J15, part d: Celadon City (ta_: the
        // original's Tamamushi) and Lavender Town (si_: Shion), built as plan 19 · K5 builds FireRed's
        House("ta_h01", Architecture.Jade, "a house of Celadon City"),
        House("ta_h02", Architecture.Jade, "a house of Celadon City"),
        House("ta_h03", Architecture.Jade, "a house of Celadon City", storeys: 2),
        Built("ta_depart", BuildingKind.Shop, "the Celadon Department Store", Architecture.Jade, storeys: 5, sign: "STORE"),
        Built("ta_mansion", BuildingKind.Apartments, "the Celadon Condominiums", Architecture.Jade, storeys: 3),
        Built("ta_game", BuildingKind.Shop, "the Celadon Game Corner (shut)", Architecture.Jade, sign: "GAMES"),
        Built("ta_gift", BuildingKind.Shop, "the Celadon Game Corner's prize corner (shut)", Architecture.Jade, sign: "PRIZES"),
        Built("ta_restaurant", BuildingKind.Shop, "Celadon City's restaurant", Architecture.Jade, sign: "RESTAURANT"),
        Built("ta_gym", BuildingKind.Gym, "the Celadon Gym"),
        Ground("tmms_sl", "a flight of steps between Celadon City's levels: its tiles draw it"),
        Ground("ta_j_obj1", "the Celadon Gym's garden, its beds and its paths: the room's tiles draw it"),
        House("si_h01", Architecture.Lilac, "a house of Lavender Town"),
        Built("si_temp", BuildingKind.Temple, "Lavender Town's House of Memories", Architecture.Lilac),
        Built("si_radio", BuildingKind.TvStation, "the Kanto Radio Tower in Lavender Town (shut)", storeys: 5),
        Door("si_door1"), Door("si_door2"),
        Furniture("soul_f", PropType.Statue, "a memorial stone in Lavender Town's House of Memories"),

        // ---------------------------------------------------------------- plan 20 · J15, part e: Cerulean City (ha_, hnda_: the
        // original's Hanada), built as plan 19 · K4 builds FireRed's, and the Sea Cottage on Route 25's cape (misaki_)
        House("ha_h01b", Architecture.Riverside, "a house of Cerulean City"),
        Built("ha_cycle", BuildingKind.Shop, "Cerulean City's bike shop, now the bike maniac's house", Architecture.Riverside, sign: "BIKES"),
        Built("ha_gym", BuildingKind.Gym, "the Cerulean Gym"),
        Ground("ha_j_pool", "the Cerulean Gym's pool: the room's water draws it"),
        Ground("hnda_sl", "a flight of steps by Cerulean City's river: its tiles draw it"),
        House("misaki_h01", Architecture.Riverside, "the Sea Cottage on Route 25's cape, where Bill lived"),
        Door("msk_dr01"),
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
        string.Equals(catalogue, "johto", StringComparison.OrdinalIgnoreCase) ? JohtoByName.GetValueOrDefault(name) ?? Of(name)
        // Unova's names are Black 2's own, and some of them (rock, plant) would mean other things in Sinnoh's table (plan 22 · U2)
        : string.Equals(catalogue, "unova", StringComparison.OrdinalIgnoreCase) ? UnovaByName.GetValueOrDefault(name)
        : Of(name);
}
