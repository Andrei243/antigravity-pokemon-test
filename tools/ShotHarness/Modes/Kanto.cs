partial class Harness
{
    /// <summary>
    /// Kanto as FireRed's maps make it (plan 19 · K1; not part of <c>all</c>, since its people would move every battle of
    /// the modes after it): a new game begun there (<c>--region Kanto</c>), Pallet Town, Route 1 and Viridian City by day
    /// and after dark, the rooms of Pallet Town and Viridian City's Pokémon Center, and the walk from the player's door
    /// to Viridian City at running pace, every frame timed, with what streaming cost.
    /// </summary>
    public void KantoMode()
    {
        engine.NewGameRegion = "Kanto";
        engine.StartNewGame();
        game.State = GameState.Overworld;
        game.SaveSlot = 1;
        game.LocationSign.Hide();
        Frames(30);
        Console.WriteLine($"a new game in Kanto begins on {game.Map.Name} at ({game.Player.GridX}, {game.Player.GridY}), "
            + $"in {game.Map.AreaAt(game.Player.GridX, game.Player.GridY)?.DisplayName ?? "nowhere"}, with {string.Join(", ", game.Party.Members.Select(p => $"{p.Species.Name} {p.Level}"))}");
        Shot("k00_new_game");

        (string Name, string Map, int X, int Y, Direction Facing)[] places =
        {
            ("k01_pallet_day", "Kanto", 70, 272, Direction.Up),
            ("k03_route1_day", "Kanto", 72, 240, Direction.Up),
            ("k05_viridian_day", "Kanto", 74, 208, Direction.Up),
            ("k07_pallet_north", "Kanto", 72, 262, Direction.Up),
            ("k08_route1_south", "Kanto", 70, 252, Direction.Down),
            ("k09_viridian_north", "Kanto", 70, 192, Direction.Up),
            ("k10_viridian_gym", "Kanto", 84, 192, Direction.Up),
            ("k11_players_house", "PalletTown_PlayersHouse_1F", 4, 7, Direction.Up),
            ("k12_players_room", "PalletTown_PlayersHouse_2F", 9, 3, Direction.Down),
            ("k13_rivals_house", "PalletTown_RivalsHouse", 4, 7, Direction.Up),
            ("k14_oaks_lab", "PalletTown_ProfessorOaksLab", 6, 11, Direction.Up),
            ("k15_viridian_center", "ViridianCity_PokemonCenter_1F", 7, 7, Direction.Up),
            ("k16_viridian_mart", "ViridianCity_Mart", 4, 6, Direction.Up),
            // Plan 19 · K2: Route 2, its rooms, Viridian Forest and Route 22, and the rooms of K1 furnished
            ("k17_route2_south", "Kanto", 68, 170, Direction.Up),
            ("k18_route2_gate_south", "Route2_ViridianForest_SouthEntrance", 7, 8, Direction.Up),
            ("k19_viridian_forest", "ViridianForest", 29, 58, Direction.Up),
            ("k20_viridian_forest_north", "ViridianForest", 10, 14, Direction.Up),
            ("k21_route2_gate_north", "Route2_ViridianForest_NorthEntrance", 7, 8, Direction.Up),
            ("k22_route2_north", "Kanto", 66, 110, Direction.Down),
            ("k23_route2_house", "Route2_House", 4, 6, Direction.Up),
            ("k24_route2_east_building", "Route2_EastBuilding", 7, 8, Direction.Up),
            ("k25_route22", "Kanto", 22, 198, Direction.Left),
            ("k26_route22_gate", "Route22_NorthEntrance", 7, 8, Direction.Up),
            ("k27_players_room", "PalletTown_PlayersHouse_2F", 6, 7, Direction.Up),
            ("k28_viridian_center_2f", "ViridianCity_PokemonCenter_2F", 7, 7, Direction.Up),
            ("k29_viridian_school", "ViridianCity_School", 5, 6, Direction.Up),
            ("k30_viridian_house", "ViridianCity_House", 5, 7, Direction.Up),
            // Plan 19 · K3: Pewter City, its rooms, and Route 3
            ("k33_pewter_day", "Kanto", 70, 80, Direction.Up),
            ("k34_pewter_gym_outside", "Kanto", 63, 79, Direction.Up),
            ("k35_pewter_museum_outside", "Kanto", 66, 69, Direction.Up),
            ("k36_pewter_gym", "PewterCity_Gym", 6, 13, Direction.Up),
            ("k37_pewter_museum_1f", "PewterCity_Museum_1F", 14, 8, Direction.Up),
            ("k38_pewter_museum_2f", "PewterCity_Museum_2F", 11, 7, Direction.Up),
            ("k39_pewter_center", "PewterCity_PokemonCenter_1F", 7, 7, Direction.Up),
            ("k40_pewter_mart", "PewterCity_Mart", 4, 6, Direction.Up),
            ("k41_pewter_house_1", "PewterCity_House1", 4, 6, Direction.Up),
            ("k42_route3", "Kanto", 125, 78, Direction.Right),
            // Plan 19 · K4: Route 4, Mt. Moon, Cerulean City and its rooms, Nugget Bridge, Route 25 and Bill's cottage
            ("k44_route4_mt_moon", "Kanto", 175, 58, Direction.Up),
            ("k45_route4_center", "Route4_PokemonCenter_1F", 7, 7, Direction.Up),
            ("k46_mt_moon_1f", "MtMoon_1F", 18, 35, Direction.Up),
            ("k47_mt_moon_b1f", "MtMoon_B1F", 22, 19, Direction.Down),
            ("k48_mt_moon_b2f", "MtMoon_B2F", 15, 11, Direction.Left),
            ("k49_route4_ledges", "Kanto", 215, 60, Direction.Right),
            ("k50_cerulean_day", "Kanto", 287, 60, Direction.Up),
            ("k51_cerulean_north", "Kanto", 287, 48, Direction.Up),
            ("k52_cerulean_gym_outside", "Kanto", 295, 64, Direction.Up),
            ("k53_cerulean_gym", "CeruleanCity_Gym", 8, 16, Direction.Up),
            ("k54_cerulean_center", "CeruleanCity_PokemonCenter_1F", 7, 7, Direction.Up),
            ("k55_cerulean_mart", "CeruleanCity_Mart", 4, 6, Direction.Up),
            ("k56_cerulean_bike_shop", "CeruleanCity_BikeShop", 5, 7, Direction.Up),
            ("k57_cerulean_burgled_house", "CeruleanCity_House2", 3, 6, Direction.Up),
            ("k58_nugget_bridge", "Kanto", 287, 25, Direction.Up),
            ("k59_route25", "Kanto", 330, 8, Direction.Right),
            ("k60_sea_cottage", "Route25_SeaCottage", 7, 8, Direction.Up),
            // Plan 19 · K4, part b: Routes 5 and 6 with the Underground Path, Vermilion City and its rooms, the S.S. Anne,
            // Route 11 and Diglett's Cave; and the things that lay as Poké Balls with their own looks
            ("k63_route5", "Kanto", 292, 104, Direction.Down),
            ("k64_underground_path_north", "UndergroundPath_NorthEntrance", 6, 7, Direction.Up),
            ("k65_underground_path", "UndergroundPath_NorthSouthTunnel", 4, 30, Direction.Down),
            ("k66_route6", "Kanto", 292, 178, Direction.Down),
            ("k67_vermilion_day", "Kanto", 285, 213, Direction.Up),
            ("k68_vermilion_harbour", "Kanto", 287, 229, Direction.Down),
            ("k69_vermilion_gym_outside", "Kanto", 282, 227, Direction.Up),
            ("k70_vermilion_gym", "VermilionCity_Gym", 5, 18, Direction.Up),
            ("k71_fan_club", "VermilionCity_PokemonFanClub", 5, 9, Direction.Up),
            ("k72_vermilion_center", "VermilionCity_PokemonCenter_1F", 7, 7, Direction.Up),
            ("k73_vermilion_mart", "VermilionCity_Mart", 4, 6, Direction.Up),
            ("k74_fishing_guru", "VermilionCity_House1", 4, 6, Direction.Up),
            ("k75_ss_anne_gangway", "SSAnne_Exterior", 32, 9, Direction.Down),
            ("k76_ss_anne_1f", "SSAnne_1F_Corridor", 19, 3, Direction.Down),
            ("k77_ss_anne_cabin", "SSAnne_1F_Room2", 3, 6, Direction.Up),
            ("k78_ss_anne_2f", "SSAnne_2F_Corridor", 31, 8, Direction.Up),
            ("k79_ss_anne_kitchen", "SSAnne_Kitchen", 7, 3, Direction.Down),
            ("k80_ss_anne_deck", "SSAnne_Deck", 15, 9, Direction.Left),
            ("k81_captains_cabin", "SSAnne_CaptainsOffice", 4, 6, Direction.Up),
            ("k82_ss_anne_b1f", "SSAnne_B1F_Corridor", 18, 4, Direction.Left),
            ("k83_route11", "Kanto", 335, 222, Direction.Right),
            ("k84_route11_gate_2f", "Route11_EastEntrance_2F", 6, 6, Direction.Up),
            ("k85_digletts_cave", "DiglettsCave_B1F", 3, 4, Direction.Down),
            ("k86_sea_cottage_teleporters", "Route25_SeaCottage", 7, 6, Direction.Up),
            ("k87_oaks_desk", "PalletTown_ProfessorOaksLab", 5, 4, Direction.Up),
            ("k88_rivals_town_map", "PalletTown_RivalsHouse", 6, 6, Direction.Up),
            ("k89_museum_old_amber", "PewterCity_Museum_1F", 22, 5, Direction.Up),
            ("k90_mt_moon_fossils", "MtMoon_B2F", 15, 11, Direction.Left),
            // Plan 19 · K5, part a: Routes 9 and 10, Rock Tunnel, Lavender Town and its rooms, the Pokémon Tower's floors,
            // Route 8, the Underground Path under Saffron City, Route 7 and the gates; and K4's left-overs looked at again
            ("k93_route9", "Kanto", 345, 58, Direction.Right),
            ("k94_route10_north", "Kanto", 393, 74, Direction.Down),
            ("k95_route10_center", "Route10_PokemonCenter_1F", 7, 7, Direction.Up),
            ("k96_rock_tunnel_1f", "RockTunnel_1F", 17, 4, Direction.Down),
            ("k97_rock_tunnel_b1f", "RockTunnel_B1F", 33, 5, Direction.Down),
            ("k98_route10_south", "Kanto", 393, 112, Direction.Down),
            ("k99_lavender_day", "Kanto", 398, 143, Direction.Up),
            ("ka0_pokemon_tower_outside", "Kanto", 402, 139, Direction.Up),
            ("ka1_lavender_center", "LavenderTown_PokemonCenter_1F", 7, 7, Direction.Up),
            ("ka2_volunteer_house", "LavenderTown_VolunteerPokemonHouse", 4, 6, Direction.Up),
            ("ka3_lavender_house", "LavenderTown_House1", 4, 6, Direction.Up),
            ("ka4_lavender_mart", "LavenderTown_Mart", 4, 6, Direction.Up),
            ("ka5_tower_1f", "PokemonTower_1F", 11, 15, Direction.Up),
            ("ka6_tower_3f", "PokemonTower_3F", 10, 11, Direction.Up),
            ("ka7_tower_5f", "PokemonTower_5F", 11, 13, Direction.Up),
            ("ka8_tower_6f", "PokemonTower_6F", 12, 13, Direction.Up),
            ("ka9_route8", "Kanto", 345, 139, Direction.Left),
            ("kb0_underground_path_west_east", "UndergroundPath_EastWestTunnel", 40, 4, Direction.Left),
            ("kb1_route7", "Kanto", 250, 142, Direction.Right),
            ("kb2_saffron_gate", "Route8_WestEntrance", 7, 5, Direction.Left),
            ("kb3_vermilion_harbour_again", "Kanto", 287, 229, Direction.Down),
            ("kb4_ss_anne_cabin_furnished", "SSAnne_1F_Room1", 3, 4, Direction.Up),
            ("kb5_ss_anne_kitchen_furnished", "SSAnne_Kitchen", 7, 9, Direction.Up),
            ("kb6_captains_cabin_furnished", "SSAnne_CaptainsOffice", 5, 5, Direction.Left),
            // Plan 19 · K5, part b: Celadon City and its rooms, the Rocket Hideout, Routes 16 and 12 to their Snorlax,
            // Saffron City laid into the overworld, Silph Co.'s floors, Sabrina's Gym and the Dojo
            ("kb9_celadon_day", "Kanto", 214, 144, Direction.Up),
            ("kc0_celadon_dept_store_outside", "Kanto", 193, 137, Direction.Up),
            ("kc1_celadon_gym_outside", "Kanto", 191, 153, Direction.Up),
            ("kc2_dept_store_1f", "CeladonCity_DepartmentStore_1F", 6, 12, Direction.Up),
            ("kc3_dept_store_roof", "CeladonCity_DepartmentStore_Roof", 13, 6, Direction.Left),
            ("kc4_game_corner", "CeladonCity_GameCorner", 10, 11, Direction.Up),
            ("kc5_celadon_gym", "CeladonCity_Gym", 6, 16, Direction.Up),
            ("kc6_condominiums", "CeladonCity_Condominiums_1F", 12, 17, Direction.Up),
            ("kc7_hideout_b1f", "RocketHideout_B1F", 12, 4, Direction.Down),
            ("kc8_hideout_b2f", "RocketHideout_B2F", 28, 4, Direction.Down),
            ("kc9_hideout_b4f", "RocketHideout_B4F", 20, 24, Direction.Up),
            ("kd0_route16", "Kanto", 168, 141, Direction.Left),
            ("kd1_route12_snorlax", "Kanto", 398, 217, Direction.Down),
            ("kd2_saffron_day", "Kanto", 288, 150, Direction.Up),
            ("kd3_silph_outside", "Kanto", 289, 147, Direction.Up),
            ("kd4_silph_1f", "SilphCo_1F", 8, 18, Direction.Up),
            ("kd5_silph_5f", "SilphCo_5F", 22, 5, Direction.Down),
            ("kd6_silph_11f", "SilphCo_11F", 13, 5, Direction.Down),
            ("kd7_saffron_gym", "SaffronCity_Gym", 14, 21, Direction.Up),
            ("kd8_dojo", "SaffronCity_Dojo", 6, 12, Direction.Up),
            ("kd9_saffron_gate_route7", "Kanto", 260, 140, Direction.Right),
            // Plan 19 · K6, part a: Routes 13 to 15 to Fuchsia City, the Cycling Road, Fuchsia's rooms and the Safari Zone
            ("ke2_route13", "Kanto", 380, 279, Direction.Left),
            ("ke3_route14", "Kanto", 323, 300, Direction.Down),
            ("ke4_route15", "Kanto", 270, 321, Direction.Left),
            ("ke5_route17_cycling_road", "Kanto", 136, 240, Direction.Down),
            ("ke6_route18", "Kanto", 176, 322, Direction.Right),
            ("ke7_fuchsia_day", "Kanto", 216, 320, Direction.Up),
            ("ke8_fuchsia_pens", "Kanto", 220, 312, Direction.Up),
            ("ke9_fuchsia_gym_outside", "Kanto", 201, 334, Direction.Up),
            ("kf0_fuchsia_gym", "FuchsiaCity_Gym", 7, 19, Direction.Up),
            ("kf1_wardens_house", "FuchsiaCity_WardensHouse", 6, 8, Direction.Up),
            ("kf2_safari_gate", "FuchsiaCity_SafariZone_Entrance", 4, 6, Direction.Up),
            ("kf3_safari_center", "SafariZone_Center", 26, 26, Direction.Up),
            ("kf4_safari_west", "SafariZone_West", 26, 16, Direction.Up),
            ("kf5_safari_secret_house", "SafariZone_SecretHouse", 4, 8, Direction.Up),
            ("kf6_fuchsia_center", "FuchsiaCity_PokemonCenter_1F", 7, 7, Direction.Up),
            ("kf7_route15_gate_2f", "Route15_WestEntrance_2F", 6, 7, Direction.Up),
            // Plan 19 · K6, part b: Cinnabar Island, its rooms and the Pokémon Mansion, the Seafoam Islands' floors, the
            // Power Plant; Saffron's and Celadon's ways of building and the rooms' new furniture
            ("kf9_cinnabar_day", "Kanto", 74, 388, Direction.Up),
            ("kg0_cinnabar_gym_outside", "Kanto", 80, 387, Direction.Up),
            ("kg1_mansion_outside", "Kanto", 69, 386, Direction.Up),
            ("kg2_cinnabar_gym", "CinnabarIsland_Gym", 25, 21, Direction.Up),
            ("kg3_cinnabar_lab", "CinnabarIsland_PokemonLab_Entrance", 4, 8, Direction.Up),
            ("kg4_cinnabar_lab_testing_room", "CinnabarIsland_PokemonLab_ExperimentRoom", 7, 8, Direction.Up),
            ("kg5_cinnabar_center", "CinnabarIsland_PokemonCenter_1F", 7, 7, Direction.Up),
            ("kg6_mansion_1f", "PokemonMansion_1F", 8, 30, Direction.Up),
            ("kg7_mansion_2f", "PokemonMansion_2F", 14, 18, Direction.Up),
            ("kg8_mansion_b1f", "PokemonMansion_B1F", 33, 25, Direction.Up),
            ("kg9_seafoam_1f", "SeafoamIslands_1F", 22, 14, Direction.Up),
            ("kh0_seafoam_b1f", "SeafoamIslands_B1F", 22, 10, Direction.Up),
            ("kh1_seafoam_b3f", "SeafoamIslands_B3F", 21, 7, Direction.Down),
            ("kh2_seafoam_b4f_articuno", "SeafoamIslands_B4F", 9, 4, Direction.Up),
            ("kh3_power_plant", "PowerPlant", 6, 36, Direction.Up),
            ("kh4_power_plant_zapdos", "PowerPlant", 5, 13, Direction.Up),
            ("kh5_saffron_streets", "Kanto", 278, 153, Direction.Up),
            ("kh6_celadon_houses", "Kanto", 228, 135, Direction.Up),
            ("kh7_game_corner_slots", "CeladonCity_GameCorner", 8, 12, Direction.Up),
            ("kh8_silph_pads", "SilphCo_5F", 20, 8, Direction.Up),
            ("kh9_safari_rest_house_outside", "SafariZone_Center", 26, 20, Direction.Up),
        };
        foreach (var (name, map, x, y, facing) in places)
        {
            At(map, x, y, facing); Frames(2); Shot(name);
            if (name is "k01_pallet_day" or "k05_viridian_day") ShotCrop(name + "_native", 640, 300, 640, 360, 2);
        }
        // Plan 19 · K6, part b: the sea routes, surfing beside their swimmers
        foreach (var (name, x, y, facing) in new[]
                 {
                     ("ki0_route19_surfing", 216, 358, Direction.Up), ("ki1_route20_surfing", 108, 390, Direction.Left),
                     ("ki2_route20_seafoam_mouths", 150, 392, Direction.Up), ("ki3_route21_north_surfing", 72, 325, Direction.Up),
                     ("ki4_route21_south_surfing", 75, 355, Direction.Up)
                 })
        {
            At("Kanto", x, y, facing);
            game.Player.SetMode(TravelMode.Surfing);
            Frames(2); Shot(name);
            game.Player.SetMode(TravelMode.OnFoot);
        }

        engine.Settings.TimeOfDay = TimeOfDay.Night;
        engine.ApplySettings(window: false);
        At("Kanto", 70, 272, Direction.Up); Frames(2); Shot("k02_pallet_night");
        At("Kanto", 72, 240, Direction.Up); Frames(2); Shot("k04_route1_night");
        At("Kanto", 74, 208, Direction.Up); Frames(2); Shot("k06_viridian_night");
        At("Kanto", 22, 198, Direction.Left); Frames(2); Shot("k31_route22_night");
        At("ViridianForest", 29, 58, Direction.Up); Frames(2); Shot("k32_viridian_forest_night");
        At("Kanto", 70, 80, Direction.Up); Frames(2); Shot("k43_pewter_night");
        At("Kanto", 287, 60, Direction.Up); Frames(2); Shot("k61_cerulean_night");
        At("Kanto", 287, 25, Direction.Up); Frames(2); Shot("k62_nugget_bridge_night");
        At("Kanto", 285, 213, Direction.Up); Frames(2); Shot("k91_vermilion_night");
        At("Kanto", 335, 222, Direction.Right); Frames(2); Shot("k92_route11_night");
        At("Kanto", 398, 143, Direction.Up); Frames(2); Shot("kb7_lavender_night");
        At("Kanto", 345, 58, Direction.Right); Frames(2); Shot("kb8_route9_night");
        At("Kanto", 214, 144, Direction.Up); Frames(2); Shot("ke0_celadon_night");
        At("Kanto", 288, 150, Direction.Up); Frames(2); Shot("ke1_saffron_night");
        At("Kanto", 216, 320, Direction.Up); Frames(2); Shot("kf8_fuchsia_night");
        At("Kanto", 74, 388, Direction.Up); Frames(2); Shot("ki5_cinnabar_night");
        engine.Settings.TimeOfDay = TimeOfDay.Day;
        engine.ApplySettings(window: false);

        // The walk the plan asks for: from the player's door up Route 1 to Viridian City's Pokémon Center, tile by tile at
        // running pace, every frame timed and held to a sixtieth of a second as the screen holds it
        if (filter.Length == 0 || Environment.GetEnvironmentVariable("SHOTS_TIMING") == "1")
        {
            var kanto = MapDatabase.Get("Kanto");
            var path = WalkingPath(kanto, (66, 268), (74, 207));
            var renderer = game.World;
            At("Kanto", 66, 268, Direction.Up); Frames(30);
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
            Console.WriteLine($"walk from the player's door in Pallet Town to Viridian City's Pokémon Center: {path.Count} tiles, {total / frames:F2} ms/frame, "
                + $"worst frame {worst:F1} ms at ({worstAt.X}, {worstAt.Y}), at most {mostChunks} chunks loaded");
            Console.WriteLine($"  streaming: {renderer.Streaming}");
            foreach (var (label, x, y) in new[] { ("pallet town", 70, 272), ("route 1", 72, 240), ("viridian city", 74, 208) })
            {
                At("Kanto", x, y, Direction.Up);
                Timing("kanto " + label);
            }
        }
        // Plan 19 · K3: the first chapter's scenes, in a new game of their own
        KantoScenes();
        engine.NewGameRegion = "Sinnoh";
    }
}
