partial class Harness
{
    /// <summary>
    /// Ilex Forest to Ecruteak City as plan 20 · J5's part b opens them, at the very end of the <c>johto</c> mode (after
    /// the centre, so nothing before it moves): the eight places by day and after dark from the open tile nearest each
    /// one's middle (<c>jw27</c> to <c>jw42</c>), a few places close up (<c>jw43</c> to <c>jw47</c>), every new room from its
    /// own middle (<c>jr45</c> on), Whitney's battle as it opens (<c>jg01</c>), and a walk up Goldenrod City from its south
    /// end to the gate house to Route 35, timed, with what streaming cost.
    /// </summary>
    void JohtoNorthShots()
    {
        game.State = GameState.Overworld;
        string[] areas = { "ilex_forest", "route_34", "goldenrod", "route_35", "national_park", "route_36", "route_37", "ecruteak" };
        void Areas(string time)
        {
            for (int i = 0; i < areas.Length; i++)
            {
                var (map, x, y) = AreaSpot(areas[i]);
                At(map.Name, x, y, Direction.Up); Frames(2);
                Shot($"jw{27 + 2 * i + (time == "night" ? 1 : 0)}_{areas[i]}_{time}");
                if (time == "day") Console.WriteLine($"{areas[i]}: {map.Name} ({x}, {y}), {map.DisplayNameAt(x, y)}, music {map.BgmTrackAt(x, y)}");
            }
        }
        Areas("day");
        // Close up: Goldenrod's Radio Tower and Department Store, the Game Corner's street, Ecruteak's Burned Tower and
        // its Dance Theater
        At("Johto", 343, 351, Direction.Up); Frames(2); Shot("jw43_goldenrod_radio_tower");
        At("Johto", 365, 370, Direction.Up); Frames(2); Shot("jw44_goldenrod_department_store"); ShotCrop("jw44_goldenrod_department_store_native", 640, 300, 640, 360, 2);
        At("Johto", 352, 362, Direction.Up); Frames(2); Shot("jw45_goldenrod_game_corner");
        At("Johto", 376, 151, Direction.Up); Frames(2); Shot("jw46_ecruteak_burned_tower");
        At("Johto", 397, 176, Direction.Up); Frames(2); Shot("jw47_ecruteak_dance_theater"); ShotCrop("jw47_ecruteak_native", 640, 300, 640, 360, 2);

        string[] rooms =
        {
            "route_34_ilex_forest_gatehouse", "route_34_daycare",
            "goldenrod_pokecenter_1f", "goldenrod_pokecenter_b1f", "goldenrod_flower_shop", "goldenrod_bike_shop",
            "goldenrod_bills_house", "goldenrod_name_rater_house", "goldenrod_friendship_checker_house", "goldenrod_northeast_house",
            "goldenrod_gym", "goldenrod_game_corner", "goldenrod_department_store_1f", "goldenrod_department_store_2f",
            "goldenrod_department_store_3f", "goldenrod_department_store_4f", "goldenrod_department_store_5f",
            "goldenrod_department_store_6f", "goldenrod_radio_tower_1f", "goldenrod_tunnel_1f",
            "route_35_goldenrod_gatehouse", "route_35_national_park_pokeathalon_gatehouse", "route_36_national_park_gatehouse",
            "ecruteak_pokecenter_1f", "ecruteak_pokemart", "ecruteak_dance_theater", "ecruteak_southwest_house",
            "ecruteak_dowsing_machine_house", "ecruteak_bell_tower_barrier_station", "burned_tower_1f", "burned_tower_b1f"
        };
        for (int i = 0; i < rooms.Length; i++)
        {
            var (room, x, y) = AreaSpot(rooms[i]);
            At(room.Name, x, y, Direction.Up); Frames(2);
            Shot($"jr{45 + i}_{rooms[i]}");
            Console.WriteLine($"{rooms[i]}: {room.Name} {room.Width}×{room.Height}, {room.NPCs.Count} people, {room.Props.Count} pieces, ways out {string.Join(", ", room.Warps.Select(w => $"({w.SourceX}, {w.SourceY}) to {w.TargetMap}"))}");
        }

        engine.Settings.TimeOfDay = TimeOfDay.Night;
        engine.ApplySettings(window: false);
        Areas("night");
        engine.Settings.TimeOfDay = TimeOfDay.Day;
        engine.ApplySettings(window: false);
        WalkUpGoldenrod();

        // Whitney's Gym, last of all: spoken to through the game's own script, her battle at its menu
        {
            var gym = MapDatabase.Get("Goldenrod_Gym");
            var whitney = gym.NPCs.FirstOrDefault(n => n.Key == "t25gym0101_gsleader3");
            if (whitney != null)
            {
                At("Goldenrod_Gym", whitney.GridX, whitney.GridY + 1, Direction.Up); Frames(2);
                game.Interact();
                for (int i = 0; i < 1200 && game.State != GameState.Battle; i++)
                {
                    var box = game.Dialogue;
                    if (box.IsActive && box.IsCurrentLineComplete) { box.Advance(); Frames(2); }
                    else Frames(1);
                }
                if (game.State == GameState.Battle) { ToMainMenu(game.Battle!); Shot("jg01_whitney_battle"); }
                Console.WriteLine($"Whitney's battle: {(game.State == GameState.Battle ? "begun" : "!! not begun, state " + game.State)}");
            }
            else Console.WriteLine("!! Whitney isn't in her Gym");
        }
    }

    void WalkUpGoldenrod()
    {
        // The walk: up Goldenrod City from the Pokémon Center to the gate house to Route 35, tile by tile at running pace,
        // every frame held to a sixtieth of a second
        if (filter.Length == 0 || Environment.GetEnvironmentVariable("SHOTS_TIMING") == "1")
        {
            var johto = MapDatabase.Get("Johto");
            var path = WalkingPath(johto, (352, 370), (358, 327));
            var renderer = game.World;
            At("Johto", 352, 370, Direction.Up); Frames(30);
            renderer.ResetStreamingStats();
            double total = 0, worst = 0;
            int frames = 0;
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
                    worst = Math.Max(worst, ms);
                    while (watch.Elapsed.TotalMilliseconds < 1000.0 / 60.0) Thread.Sleep(1);
                }
            }
            Console.WriteLine($"walk up Goldenrod City to the Route 35 gate: {path.Count} tiles, {total / Math.Max(1, frames):F2} ms/frame, worst frame {worst:F1} ms");
            Console.WriteLine($"  streaming: {renderer.Streaming}");
        }
    }
}
