partial class Harness
{
    /// <summary>
    /// Hoenn as Emerald's maps make it (plan 21 · H1; not part of <c>all</c>, since its people would move every battle of
    /// the modes after it): a new game begun there (<c>--region Hoenn</c>), Littleroot Town, Route 101 and Oldale Town by
    /// day and after dark (h01 to h06), Routes 102 and 103 and Petalburg City (h07 to h10), the rooms of the three
    /// towns furnished (h11 to h18), and the walk from the family's door to Petalburg City's Pokémon Center at running
    /// pace, every frame timed, with what streaming cost.
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
        };
        foreach (var (name, map, x, y, facing) in places)
        {
            At(map, x, y, facing); Frames(2); Shot(name);
            if (name is "h01_littleroot_day" or "h09_petalburg_day" or "h34_dewford_town" or "h43_slateport_city") ShotCrop(name + "_native", 640, 300, 640, 360, 2);
        }

        engine.Settings.TimeOfDay = TimeOfDay.Night;
        engine.ApplySettings(window: false);
        At("Hoenn", 130, 293, Direction.Up); Frames(2); Shot("h02_littleroot_night");
        At("Hoenn", 130, 272, Direction.Up); Frames(2); Shot("h04_route101_night");
        At("Hoenn", 130, 252, Direction.Up); Frames(2); Shot("h06_oldale_night");
        At("Hoenn", 56, 247, Direction.Up); Frames(2); Shot("h10_petalburg_night");
        At("Hoenn", 66, 373, Direction.Up); Frames(2); Shot("h51_dewford_night");
        At("Hoenn", 219, 283, Direction.Up); Frames(2); Shot("h52_slateport_night");
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
        engine.NewGameRegion = "Sinnoh";
    }
}
