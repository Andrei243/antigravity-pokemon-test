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
        };
        foreach (var (name, map, x, y, facing) in places)
        {
            At(map, x, y, facing); Frames(2); Shot(name);
            if (name is "h01_littleroot_day" or "h09_petalburg_day" or "h34_dewford_town" or "h43_slateport_city" or "h57_mauville_city" or "h65_verdanturf_town" or "h72_desert_storm" or "h76_fallarbor_town" or "h108_fortree_city" or "h110_fortree_gym") ShotCrop(name + "_native", 640, 300, 640, 360, 2);
        }

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
