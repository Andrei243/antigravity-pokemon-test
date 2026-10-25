partial class Harness
{
    /// <summary>
    /// Hoenn as Emerald's maps make it (plan 21 · H1; not part of <c>all</c>, since its people would move every battle of
    /// the modes after it): a new game begun there (<c>--region Hoenn</c>), Littleroot Town, Route 101 and Oldale Town by
    /// day and after dark (h01 to h06), Routes 102 and 103 and Petalburg City (h07 to h10), the rooms of the three
    /// towns furnished (h11 to h18), and the walk from the family's door to Petalburg City's Pokémon Center at running
    /// pace, every frame timed, with what streaming cost; the chapters' scenes after it, and last Dive and the sea floor
    /// under Route 124 (plan 21 · H5, h92 to h96, <c>HoennSeaShots</c>).
    /// </summary>
    public void HoennMode()
    {
        engine.NewGameRegion = "Hoenn";
        engine.StartNewGame();
        // The places are shot as the town stands once the van has come and gone: the van's ride and the chapter are
        // HoennScenes', at the end
        game.Story.SetVar("VAR_LITTLEROOT_INTRO_STATE", 7);
        game.State = GameState.Overworld;
        game.SaveSlot = 1;
        game.LocationSign.Hide();
        Frames(30);
        Console.WriteLine($"a new game in Hoenn begins on {game.Map.Name} at ({game.Player.GridX}, {game.Player.GridY}), "
            + $"in {game.Map.AreaAt(game.Player.GridX, game.Player.GridY)?.DisplayName ?? "nowhere"}, with {string.Join(", ", game.Party.Members.Select(p => $"{p.Species.Name} {p.Level}"))}");
        Shot("h00_new_game");

        (string Name, string Map, int X, int Y, Direction Facing)[] places =
        {
            ("h01_littleroot_day", "Hoenn", 130, 293, Direction.Up),
            ("h03_route101_day", "Hoenn", 130, 272, Direction.Up),
            ("h05_oldale_day", "Hoenn", 130, 252, Direction.Up),
            ("h07_route102", "Hoenn", 95, 250, Direction.Left),
            ("h08_route103", "Hoenn", 133, 228, Direction.Up),
            ("h09_petalburg_day", "Hoenn", 56, 247, Direction.Up),
            ("h09b_petalburg_gym", "Hoenn", 52, 243, Direction.Up),
            ("h09c_petalburg_south", "Hoenn", 52, 256, Direction.Down),
            ("h11_family_house", "LittlerootTown_BrendansHouse_1F", 5, 7, Direction.Up),
            ("h12_bedroom", "LittlerootTown_BrendansHouse_2F", 5, 6, Direction.Up),
            ("h13_neighbours_house", "LittlerootTown_MaysHouse_1F", 5, 7, Direction.Up),
            ("h14_birchs_lab", "LittlerootTown_ProfessorBirchsLab", 6, 10, Direction.Up),
            ("h15_oldale_center", "OldaleTown_PokemonCenter_1F", 7, 6, Direction.Up),
            ("h16_oldale_mart", "OldaleTown_Mart", 5, 6, Direction.Up),
            ("h17_petalburg_center", "PetalburgCity_PokemonCenter_1F", 7, 6, Direction.Up),
            ("h18_wallys_house", "PetalburgCity_WallysHouse", 5, 6, Direction.Up),
            // Plan 21 · H2, part b: Route 104's two halves, Petalburg Woods, Rustboro City, Route 116 and Rusturf Tunnel,
            // and their rooms
            ("h19_route104_south", "Hoenn", 20, 240, Direction.Up),
            ("h20_petalburg_woods", "PetalburgWoods", 15, 20, Direction.Up),
            ("h21_route104_north", "Hoenn", 20, 205, Direction.Up),
            ("h22_rustboro_city", "Hoenn", 20, 158, Direction.Up),
            ("h22b_rustboro_devon", "Hoenn", 15, 146, Direction.Up),
            ("h23_route116", "Hoenn", 70, 134, Direction.Right),
            ("h24_rusturf_tunnel", "RusturfTunnel", 8, 6, Direction.Right),
            ("h25_devon_lobby", "RustboroCity_DevonCorp_1F", 6, 6, Direction.Up),
            ("h26_devon_office", "RustboroCity_DevonCorp_3F", 10, 6, Direction.Up),
            ("h27_rustboro_gym", "RustboroCity_Gym", 5, 17, Direction.Up),
            ("h28_trainers_school", "RustboroCity_PokemonSchool", 5, 8, Direction.Up),
            ("h29_cutters_house", "RustboroCity_CuttersHouse", 5, 6, Direction.Up),
            ("h30_brineys_cottage", "Route104_MrBrineysHouse", 5, 6, Direction.Up),
            ("h31_flower_shop", "Route104_PrettyPetalFlowerShop", 4, 6, Direction.Up),
            ("h32_rustboro_center", "RustboroCity_PokemonCenter_1F", 7, 6, Direction.Up),
            // Plan 21 · H3, part a: Mr. Briney's jetties, Dewford Town and Granite Cave, Route 109's beach and Slateport City,
            // and their rooms
            ("h33_brineys_jetty", "Hoenn", 13, 233, Direction.Left),
            ("h34_dewford_town", "Hoenn", 66, 373, Direction.Up),
            ("h35_dewford_jetty", "Hoenn", 70, 371, Direction.Right),
            ("h36_route106_beach", "Hoenn", 51, 357, Direction.Left),
            ("h37_granite_cave", "GraniteCave_1F", 36, 11, Direction.Up),
            ("h38_granite_cave_b1f", "GraniteCave_B1F", 25, 14, Direction.Up),
            ("h39_stevens_room", "GraniteCave_StevensRoom", 7, 5, Direction.Down),
            ("h40_dewford_gym", "DewfordTown_Gym", 5, 25, Direction.Up),
            ("h41_dewford_hall", "DewfordTown_Hall", 5, 7, Direction.Up),
            ("h42_route109_beach", "Hoenn", 221, 340, Direction.Up),
            ("h43_slateport_city", "Hoenn", 219, 283, Direction.Up),
            ("h44_slateport_market", "Hoenn", 210, 305, Direction.Up),
            ("h45_oceanic_museum", "SlateportCity_OceanicMuseum_1F", 9, 6, Direction.Up),
            ("h46_museum_upstairs", "SlateportCity_OceanicMuseum_2F", 8, 4, Direction.Down),
            ("h47_sterns_shipyard", "SlateportCity_SternsShipyard_1F", 3, 12, Direction.Up),
            ("h48_seashore_house", "Route109_SeashoreHouse", 6, 8, Direction.Up),
            ("h49_slateport_harbour", "SlateportCity_Harbor", 11, 13, Direction.Up),
            ("h50_dewford_center", "DewfordTown_PokemonCenter_1F", 7, 6, Direction.Up),
            // Plan 21 · H3, part b: Route 110 and its Trick House, Mauville City, Route 117 and its Day Care, Verdanturf Town
            ("h53_route110_south", "Hoenn", 214, 236, Direction.Up),
            ("h54_trick_house_outside", "Hoenn", 211, 228, Direction.Up),
            ("h55_route110_north", "Hoenn", 232, 206, Direction.Up),
            ("h56_cycling_road_gate", "Route110_SeasideCyclingRoadSouthEntrance", 7, 5, Direction.Up),
            ("h57_mauville_city", "Hoenn", 220, 150, Direction.Up),
            ("h58_mauville_gym", "MauvilleCity_Gym", 4, 19, Direction.Up),
            ("h59_bike_shop", "MauvilleCity_BikeShop", 3, 7, Direction.Up),
            ("h60_mauville_center", "MauvilleCity_PokemonCenter_1F", 7, 6, Direction.Up),
            ("h61_trick_house", "Route110_TrickHouseEntrance", 5, 6, Direction.Up),
            ("h62_trick_house_puzzle", "Route110_TrickHousePuzzle1", 1, 20, Direction.Up),
            ("h63_route117", "Hoenn", 165, 150, Direction.Left),
            ("h64_day_care", "Route117_PokemonDayCare", 3, 7, Direction.Up),
            ("h65_verdanturf_town", "Hoenn", 130, 148, Direction.Up),
            ("h66_wandas_house", "VerdanturfTown_WandasHouse", 7, 6, Direction.Up),
            ("h67_verdanturf_tent", "VerdanturfTown_BattleTentLobby", 6, 8, Direction.Up),
            // Plan 21 · H3, part c, and H4, part a: Mauville's Game Corner, Routes 111 to 114 with the desert's storm and the
            // ash, Fiery Path, Fallarbor Town, Meteor Falls and their rooms
            ("h70_route111_south", "Hoenn", 213, 118, Direction.Up),
            ("h71_desert_edge", "Hoenn", 212, 64, Direction.Up),
            ("h72_desert_storm", "Hoenn", 225, 60, Direction.Up),
            ("h73_route112", "Hoenn", 188, 55, Direction.Up),
            ("h74_fiery_path", "FieryPath", 26, 35, Direction.Up),
            ("h75_route113_ash", "Hoenn", 150, 10, Direction.Left),
            ("h76_fallarbor_town", "Hoenn", 90, 12, Direction.Up),
            ("h77_route114", "Hoenn", 60, 40, Direction.Up),
            ("h78_meteor_falls", "MeteorFalls_1F_1R", 20, 18, Direction.Left),
            ("h79_winstrate_house", "Route111_WinstrateFamilysHouse", 3, 6, Direction.Up),
            ("h80_rest_stop", "Route111_OldLadysRestStop", 3, 6, Direction.Up),
            ("h81_cable_car_station", "Route112_CableCarStation", 6, 10, Direction.Up),
            ("h82_glass_workshop", "Route113_GlassWorkshop", 3, 7, Direction.Up),
            ("h83_fallarbor_center", "FallarborTown_PokemonCenter_1F", 7, 7, Direction.Up),
            ("h84_cozmos_house", "FallarborTown_CozmosHouse", 3, 7, Direction.Up),
            ("h85_fallarbor_tent", "FallarborTown_BattleTentLobby", 6, 8, Direction.Up),
            ("h86_fossil_maniacs_house", "Route114_FossilManiacsHouse", 4, 6, Direction.Up),
            ("h87_lanettes_house", "Route114_LanettesHouse", 5, 6, Direction.Up),
            ("h88_game_corner", "MauvilleCity_GameCorner", 11, 9, Direction.Up),
            // Plan 21 · H6, part a: Route 118, Route 119 in its rain with the Weather Institute, Fortree City in the
            // treetops with its rooms and Winona's Gym of rotating gates, and Route 120's rain and cloud (h100 to h117)
            ("h100_route118", "Hoenn", 270, 150, Direction.Up),
            ("h101_route118_steven", "Hoenn", 284, 152, Direction.Up),
            ("h102_route119_south", "Hoenn", 300, 125, Direction.Up),
            ("h103_route119_rain", "Hoenn", 300, 60, Direction.Up),
            ("h104_weather_institute", "Hoenn", 286, 35, Direction.Up),
            ("h105_institute_1f", "Route119_WeatherInstitute_1F", 9, 11, Direction.Up),
            ("h106_institute_2f", "Route119_WeatherInstitute_2F", 17, 2, Direction.Down),
            ("h107_route119_north", "Hoenn", 305, 20, Direction.Up),
            ("h108_fortree_city", "Hoenn", 335, 10, Direction.Up),
            ("h109_fortree_west", "Hoenn", 326, 9, Direction.Up),
            ("h110_fortree_gym", "FortreeCity_Gym", 15, 22, Direction.Up),
            ("h111_fortree_center", "FortreeCity_PokemonCenter_1F", 7, 7, Direction.Up),
            ("h112_fortree_mart", "FortreeCity_Mart", 3, 6, Direction.Up),
            ("h113_fortree_house", "FortreeCity_House1", 3, 4, Direction.Up),
            ("h114_route119_house", "Route119_House", 3, 7, Direction.Up),
            ("h115_route120_bridge", "Hoenn", 373, 17, Direction.Up),
            ("h116_route120_rain", "Hoenn", 380, 40, Direction.Up),
            ("h117_route120_cloud", "Hoenn", 380, 80, Direction.Up),
            // Plan 21 · H6, part b: Route 121, the Safari Zone's gate and its parts, the rest house, the Ancient Tomb,
            // the Scorched Slab and Fortree's second house
            ("h120_route121", "Hoenn", 428, 90, Direction.Up),
            ("h121_route121_pier", "Hoenn", 431, 93, Direction.Down),
            ("h122_safari_gate", "Route121_SafariZoneEntrance", 9, 4, Direction.Left),
            ("h123_safari_south", "HoennSafariZone", 72, 75, Direction.Down),
            ("h124_safari_southwest", "HoennSafariZone", 22, 50, Direction.Up),
            ("h125_safari_north", "HoennSafariZone", 62, 10, Direction.Up),
            ("h126_safari_northwest", "HoennSafariZone", 8, 9, Direction.Up),
            ("h127_safari_rest_house", "HoennSafariZone_RestHouse", 4, 7, Direction.Up),
            ("h128_ancient_tomb", "AncientTomb", 8, 28, Direction.Up),
            ("h129_ancient_tomb_chamber", "AncientTomb", 8, 10, Direction.Up),
            ("h130_scorched_slab", "ScorchedSlab", 7, 15, Direction.Up),
            ("h131_fortree_house_2", "FortreeCity_House2", 4, 4, Direction.Up),
            // Plan 21 · H7, part a: Lilycove City and its rooms, Routes 122 and 123, the Berry Master's house and Mt. Pyre
            ("h133_lilycove_city", "Hoenn", 507, 80, Direction.Up),
            ("h134_lilycove_cove", "Hoenn", 545, 86, Direction.Right),
            ("h135_lilycove_beach", "Hoenn", 520, 100, Direction.Down),
            ("h136_lilycove_center", "LilycoveCity_PokemonCenter_1F", 7, 7, Direction.Up),
            ("h137_store_1f", "LilycoveCity_DepartmentStore_1F", 8, 6, Direction.Up),
            ("h138_store_4f", "LilycoveCity_DepartmentStore_4F", 8, 5, Direction.Down),
            ("h139_store_rooftop", "LilycoveCity_DepartmentStoreRooftop", 10, 4, Direction.Up),
            ("h140_motel", "LilycoveCity_CoveLilyMotel_1F", 5, 7, Direction.Up),
            ("h141_museum", "LilycoveCity_LilycoveMuseum_1F", 9, 12, Direction.Up),
            ("h142_museum_gallery", "LilycoveCity_LilycoveMuseum_2F", 12, 8, Direction.Up),
            ("h143_fan_club", "LilycoveCity_PokemonTrainerFanClub", 5, 12, Direction.Up),
            ("h144_route122", "Hoenn", 442, 130, Direction.Up),
            ("h145_route123", "Hoenn", 400, 150, Direction.Up),
            ("h146_berry_masters_house", "Route123_BerryMastersHouse", 3, 6, Direction.Up),
            ("h147_mt_pyre_1f", "MtPyre_1F", 17, 17, Direction.Up),
            ("h148_mt_pyre_3f", "MtPyre_3F", 6, 8, Direction.Up),
            ("h149_mt_pyre_6f", "MtPyre_6F", 6, 6, Direction.Up),
            ("h150_mt_pyre_mountainside", "MtPyre_Exterior", 20, 30, Direction.Up),
            ("h151_mt_pyre_mist", "MtPyre_Exterior", 20, 12, Direction.Up),
            ("h152_mt_pyre_summit", "MtPyre_Summit", 23, 12, Direction.Up),
            // Plan 21 · H7, part b: the two teams' hideouts and the way into Team Magma's on Jagged Pass
            ("h155_jagged_pass_hideout", "JaggedPass", 16, 21, Direction.Up),
            ("h156_magma_hideout_1f", "MagmaHideout_1F", 10, 30, Direction.Up),
            ("h157_magma_hideout_2f", "MagmaHideout_2F_1R", 12, 18, Direction.Up),
            ("h158_magma_hideout_3f", "MagmaHideout_3F_1R", 10, 12, Direction.Up),
            ("h159_magma_hideout_groudon", "MagmaHideout_4F", 16, 22, Direction.Up),
            ("h160_aqua_hideout_1f", "AquaHideout_1F", 13, 20, Direction.Up),
            ("h161_aqua_hideout_b1f", "AquaHideout_B1F", 30, 8, Direction.Up),
            ("h162_aqua_hideout_panels", "AquaHideout_B1F", 45, 11, Direction.Up),
            ("h163_aqua_hideout_b2f", "AquaHideout_B2F", 24, 19, Direction.Left),
            ("h164_lilycove_cove", "Hoenn", 550, 78, Direction.Up),
            // Plan 21 · H8, part a: Mossdeep City and its rooms, the sea round it and the treasure hunter's house
            ("h165_mossdeep_city", "Hoenn", 668, 118, Direction.Up),
            ("h166_mossdeep_space_center", "Hoenn", 702, 120, Direction.Up),
            ("h167_mossdeep_east", "Hoenn", 690, 128, Direction.Up),
            ("h168_mossdeep_gym", "MossdeepCity_Gym", 7, 32, Direction.Up),
            ("h169_mossdeep_gym_arrows", "MossdeepCity_Gym", 3, 31, Direction.Up),
            ("h170_mossdeep_gym_twins", "MossdeepCity_Gym", 22, 9, Direction.Up),
            ("h171_space_center_1f", "MossdeepCity_SpaceCenter_1F", 7, 8, Direction.Up),
            ("h172_space_center_2f", "MossdeepCity_SpaceCenter_2F", 8, 6, Direction.Up),
            ("h173_stevens_house", "MossdeepCity_StevensHouse", 4, 6, Direction.Up),
            ("h174_mossdeep_center", "MossdeepCity_PokemonCenter_1F", 7, 6, Direction.Up),
            ("h175_mossdeep_mart", "MossdeepCity_Mart", 4, 6, Direction.Up),
            ("h176_mossdeep_house", "MossdeepCity_House2", 3, 7, Direction.Up),
            ("h177_treasure_hunters_house", "Route124_DivingTreasureHuntersHouse", 3, 7, Direction.Up),
            ("h178_route127_sandbar", "Hoenn", 682, 163, Direction.Up),
            // Plan 21 · H8, part b: Route 128's islet, the Seafloor Cavern, Sootopolis City and its rooms, the Cave of Origin
            ("h180_route128_islet", "Hoenn", 678, 242, Direction.Up),
            ("h181_seafloor_cavern_pool", "Underwater_SeafloorCavern", 6, 6, Direction.Up),
            ("h182_seafloor_cavern_mouth", "SeafloorCavern_Entrance", 10, 12, Direction.Up),
            ("h183_seafloor_cavern_boulders", "SeafloorCavern_Room2", 7, 6, Direction.Up),
            ("h184_seafloor_cavern_shelly", "SeafloorCavern_Room3", 7, 7, Direction.Up),
            ("h185_seafloor_cavern_kyogre", "SeafloorCavern_Room9", 17, 42, Direction.Up),
            ("h186_sootopolis_city", "SootopolisCity", 43, 33, Direction.Up),
            ("h187_sootopolis_harbour", "SootopolisCity", 29, 50, Direction.Up),
            ("h188_sootopolis_cave_mouth", "SootopolisCity", 31, 20, Direction.Up),
            ("h189_sootopolis_pool", "Underwater_SootopolisCity", 9, 6, Direction.Up),
            ("h190_sootopolis_center", "SootopolisCity_PokemonCenter_1F", 7, 6, Direction.Up),
            ("h191_sootopolis_mart", "SootopolisCity_Mart", 4, 6, Direction.Up),
            ("h192_sootopolis_house", "SootopolisCity_House1", 3, 6, Direction.Up),
            ("h193_cave_of_origin", "CaveOfOrigin_Entrance", 9, 15, Direction.Up),
            ("h194_cave_of_origin_b1f", "CaveOfOrigin_B1F", 9, 15, Direction.Up),
            // Plan 21 · H8, part c: Routes 129 to 131, Pacifidlog Town and its rooms, Sky Pillar, Juan's Gym, and Route
            // 120's bridge and Route 128's islet as the import now lays them
            ("h196_route129", "Hoenn", 660, 270, Direction.Up),
            ("h197_pacifidlog_town", "Hoenn", 488, 277, Direction.Up),
            ("h198_pacifidlog_center", "PacifidlogTown_PokemonCenter_1F", 7, 6, Direction.Up),
            ("h199_pacifidlog_house", "PacifidlogTown_House2", 4, 7, Direction.Up),
            ("h200_route131_sky_pillar", "Hoenn", 536, 268, Direction.Up),
            ("h201_sky_pillar_cave", "SkyPillar_Entrance", 6, 14, Direction.Up),
            ("h202_sky_pillar_outside", "SkyPillar_Outside", 14, 8, Direction.Up),
            ("h203_sky_pillar_1f", "SkyPillar_1F", 6, 11, Direction.Up),
            ("h204_sky_pillar_4f", "SkyPillar_4F", 5, 4, Direction.Right),
            ("h205_sky_pillar_top", "SkyPillar_Top", 14, 10, Direction.Up),
            ("h206_sootopolis_gym", "SootopolisCity_Gym_1F", 8, 23, Direction.Up),
            ("h207_sootopolis_gym_basement", "SootopolisCity_Gym_B1F", 8, 18, Direction.Up),
            ("h208_route120_bridge", "Hoenn", 376, 15, Direction.Left),
            ("h209_route128_islet_sand", "Hoenn", 678, 243, Direction.Up),
            // Plan 21 · H8, part d: Routes 132 to 134 and their currents, and Shoal Cave at low tide (a new game's: the
            // tide's flag is unset until the entrance's OnTransition reads the hour); h218 to h221 below at high tide
            ("h211_route132", "Hoenn", 430, 280, Direction.Left),
            ("h212_route133", "Hoenn", 360, 270, Direction.Left),
            ("h213_route134_slateport", "Hoenn", 262, 280, Direction.Left),
            ("h214_shoal_cave_entrance_low", "ShoalCave_LowTideEntranceRoom", 20, 29, Direction.Up),
            ("h215_shoal_cave_inner_low", "ShoalCave_LowTideInnerRoom", 31, 22, Direction.Up),
            ("h216_shoal_cave_lower", "ShoalCave_LowTideLowerRoom", 17, 5, Direction.Left),
            ("h217_shoal_cave_ice", "ShoalCave_LowTideIceRoom", 17, 9, Direction.Up),
            // Plan 21 · H9: Ever Grande City, its Pokémon Center, Victory Road's three floors and the Pokémon League's
            // rooms (h236 is the Hall of Fame; h237 the city after dark, below)
            ("h222_ever_grande_city", "Hoenn", 779, 233, Direction.Up),
            ("h223_ever_grande_league_gate", "Hoenn", 778, 190, Direction.Up),
            ("h224_ever_grande_terraces", "Hoenn", 783, 200, Direction.Up),
            ("h225_ever_grande_center", "EverGrandeCity_PokemonCenter_1F", 7, 6, Direction.Up),
            ("h226_victory_road_1f", "HoennVictoryRoad_1F", 15, 39, Direction.Up),
            ("h227_victory_road_b1f", "HoennVictoryRoad_B1F", 36, 12, Direction.Right),
            ("h228_victory_road_b2f", "HoennVictoryRoad_B2F", 43, 13, Direction.Down),
            ("h229_league_hall", "EverGrandeCity_PokemonLeague_1F", 9, 6, Direction.Up),
            ("h230_sidneys_room", "EverGrandeCity_SidneysRoom", 6, 11, Direction.Up),
            ("h231_phoebes_room", "EverGrandeCity_PhoebesRoom", 6, 11, Direction.Up),
            ("h232_glacias_room", "EverGrandeCity_GlaciasRoom", 6, 11, Direction.Up),
            ("h233_drakes_room", "EverGrandeCity_DrakesRoom", 6, 11, Direction.Up),
            ("h234_league_long_hall", "EverGrandeCity_Hall4", 5, 20, Direction.Up),
            ("h235_champions_room", "EverGrandeCity_ChampionsRoom", 6, 9, Direction.Up),
            ("h236_hall_of_fame", "EverGrandeCity_HallOfFame", 7, 14, Direction.Up),
            // Plan 21 · H9, part b: Lilycove City's harbour, its quay and the gangway (the captain comes with the Hall of Fame)
            ("h238_lilycove_harbour", "LilycoveCity_Harbor", 9, 12, Direction.Up),
        };
        foreach (var (name, map, x, y, facing) in places)
        {
            At(map, x, y, facing);
            // (On a bridge, on its deck: Route 120's, plan 21 · H8, part c)
            if (name == "h208_route120_bridge" && game.Map.DeckAt(x, y) is { } deck) game.Player.SetHeight(deck);
            Frames(2); Shot(name);
            if (name is "h01_littleroot_day" or "h09_petalburg_day" or "h34_dewford_town" or "h43_slateport_city" or "h57_mauville_city" or "h65_verdanturf_town" or "h72_desert_storm" or "h76_fallarbor_town" or "h108_fortree_city" or "h110_fortree_gym" or "h123_safari_south" or "h133_lilycove_city" or "h152_mt_pyre_summit" or "h159_magma_hideout_groudon" or "h162_aqua_hideout_panels" or "h165_mossdeep_city" or "h169_mossdeep_gym_arrows" or "h186_sootopolis_city" or "h187_sootopolis_harbour" or "h197_pacifidlog_town" or "h205_sky_pillar_top" or "h206_sootopolis_gym" or "h215_shoal_cave_inner_low" or "h222_ever_grande_city" or "h235_champions_room") ShotCrop(name + "_native", 640, 300, 640, 360, 2);
        }

        // Shoal Cave at high tide (plan 21 · H8, part d): the flag the entrance's OnTransition sets, its chunks laid by
        // world.json's swaps as the field next refreshes who and what is where, given a moment to be baked again
        game.Story.Set(ShoalTide.Flag);
        foreach (var (name, map, x, y, facing) in new[]
                 {
                     ("h218_shoal_cave_entrance_high", "ShoalCave_LowTideEntranceRoom", 20, 29, Direction.Up),
                     ("h219_shoal_cave_inner_high", "ShoalCave_LowTideInnerRoom", 31, 22, Direction.Up),
                     ("h220_shoal_cave_inner_high_west", "ShoalCave_LowTideInnerRoom", 12, 20, Direction.Up)
                 })
        {
            At(map, x, y, facing);
            Frames(30); Shot(name);
            if (name == "h219_shoal_cave_inner_high") ShotCrop(name + "_native", 640, 300, 640, 360, 2);
        }
        Console.WriteLine($"shoal cave: tide flag {game.Story.Has(ShoalTide.Flag)}, the inner room's swapped places {MapDatabase.Get("ShoalCave_LowTideInnerRoom").SwappedPlaces.Count}");
        game.Story.Unset(ShoalTide.Flag);
        At("ShoalCave_LowTideInnerRoom", 31, 22, Direction.Up);
        Frames(30); Shot("h221_shoal_cave_inner_low_again");

        engine.Settings.TimeOfDay = TimeOfDay.Night;
        engine.ApplySettings(window: false);
        At("Hoenn", 130, 293, Direction.Up); Frames(2); Shot("h02_littleroot_night");
        At("Hoenn", 130, 272, Direction.Up); Frames(2); Shot("h04_route101_night");
        At("Hoenn", 130, 252, Direction.Up); Frames(2); Shot("h06_oldale_night");
        At("Hoenn", 56, 247, Direction.Up); Frames(2); Shot("h10_petalburg_night");
        At("Hoenn", 66, 373, Direction.Up); Frames(2); Shot("h51_dewford_night");
        At("Hoenn", 219, 283, Direction.Up); Frames(2); Shot("h52_slateport_night");
        At("Hoenn", 220, 150, Direction.Up); Frames(2); Shot("h68_mauville_night");
        At("Hoenn", 130, 148, Direction.Up); Frames(2); Shot("h69_verdanturf_night");
        At("Hoenn", 225, 60, Direction.Up); Frames(2); Shot("h89_desert_night");
        At("Hoenn", 150, 10, Direction.Left); Frames(2); Shot("h90_route113_night");
        At("Hoenn", 90, 12, Direction.Up); Frames(2); Shot("h91_fallarbor_night");
        At("Hoenn", 335, 10, Direction.Up); Frames(2); Shot("h118_fortree_night");
        At("Hoenn", 300, 60, Direction.Up); Frames(2); Shot("h119_route119_rain_night");
        At("HoennSafariZone", 72, 75, Direction.Down); Frames(2); Shot("h132_safari_night");
        At("Hoenn", 507, 80, Direction.Up); Frames(2); Shot("h153_lilycove_night");
        At("MtPyre_Summit", 23, 12, Direction.Up); Frames(2); Shot("h154_mt_pyre_summit_night");
        At("Hoenn", 668, 118, Direction.Up); Frames(2); Shot("h179_mossdeep_night");
        At("SootopolisCity", 43, 33, Direction.Up); Frames(2); Shot("h195_sootopolis_night");
        At("Hoenn", 488, 277, Direction.Up); Frames(2); Shot("h210_pacifidlog_night");
        At("Hoenn", 779, 233, Direction.Up); Frames(2); Shot("h237_ever_grande_night");
        engine.Settings.TimeOfDay = TimeOfDay.Day;
        engine.ApplySettings(window: false);

        // The walk the plan asks for: from the family's door up Route 101, through Oldale Town and west along Route 102
        // to Petalburg City's Pokémon Center, tile by tile at running pace, every frame timed and held to a sixtieth of a
        // second as the screen holds it
        if (filter.Length == 0 || Environment.GetEnvironmentVariable("SHOTS_TIMING") == "1")
        {
            var hoenn = MapDatabase.Get("Hoenn");
            var path = WalkingPath(hoenn, (125, 291), (60, 249));
            var renderer = game.World;
            At("Hoenn", 125, 291, Direction.Up); Frames(30);
            renderer.ResetStreamingStats();
            double total = 0, worst = 0;
            int frames = 0, mostChunks = 0;
            (int X, int Y) worstAt = default;
            var watch = new System.Diagnostics.Stopwatch();
            foreach (var (x, y) in path)
            {
                game.Player.SetPosition(x, y, Direction.Up);
                for (int f = 0; f < 8; f++)
                {
                    watch.Restart();
                    FrameClock.Fixed = ++tick / 60.0;
                    engine.Update(1f / 60f);
                    engine.Draw();
                    double ms = watch.Elapsed.TotalMilliseconds;
                    total += ms; frames++;
                    if (ms > worst) { worst = ms; worstAt = (x, y); }
                    while (watch.Elapsed.TotalMilliseconds < 1000.0 / 60.0) Thread.Sleep(1);
                }
                mostChunks = Math.Max(mostChunks, renderer.LoadedChunks);
            }
            Console.WriteLine($"walk from the family's door in Littleroot Town to Petalburg City's Pokémon Center: {path.Count} tiles, {total / Math.Max(1, frames):F2} ms/frame, "
                + $"worst frame {worst:F1} ms at ({worstAt.X}, {worstAt.Y}), at most {mostChunks} chunks loaded");
            Console.WriteLine($"  streaming: {renderer.Streaming}");
            foreach (var (label, x, y) in new[] { ("littleroot town", 130, 293), ("route 101", 130, 272), ("oldale town", 130, 252), ("petalburg city", 56, 247) })
            {
                At("Hoenn", x, y, Direction.Up);
                Timing("hoenn " + label);
            }
        }
        // And last the first chapter played by its own scripts, in a new game of its own (plan 21 · H2)
        HoennScenes();
        // And after the chapter, Dive and the sea floor under Route 124 (plan 21 · H5, h92 to h96)
        HoennSeaShots();
        engine.NewGameRegion = "Sinnoh";
    }
}
