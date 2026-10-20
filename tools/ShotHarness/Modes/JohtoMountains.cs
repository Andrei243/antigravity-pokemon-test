partial class Harness
{
    /// <summary>
    /// The north as plan 20 · J10, part a opens it, after the west and the sea in the <c>johto</c> mode (so nothing before
    /// it moves; <c>JOHTO_FROM=north</c> runs it alone in a new game of its own): the eight places with ground by day and
    /// after dark from the open tile nearest each one's middle (<c>jw66</c> to <c>jw81</c>), a few places close up
    /// (<c>jw82</c> to <c>jw86</c>), every new room and cave from its own middle (<c>jr106</c> on), and the walk from
    /// Mahogany Town's Pokémon Center along Route 44, through the Ice Path's four floors and down Blackthorn City to its
    /// Pokémon Center, timed, with what streaming cost.
    /// </summary>
    void JohtoMountainShots()
    {
        game.State = GameState.Overworld;
        game.Player.SetMode(TravelMode.OnFoot);
        string[] areas = { "route_42", "mahogany", "route_43", "lake_of_rage", "route_44", "blackthorn", "route_45", "route_46" };
        void Areas(string time)
        {
            for (int i = 0; i < areas.Length; i++)
            {
                var (map, x, y) = AreaSpot(areas[i]);
                At(map.Name, x, y, Direction.Up); Frames(2);
                Shot($"jw{66 + 2 * i + (time == "night" ? 1 : 0)}_{areas[i]}_{time}");
                if (time == "day") Console.WriteLine($"{areas[i]}: {map.Name} ({x}, {y}), {map.DisplayNameAt(x, y)}, music {map.BgmTrackAt(x, y)}");
            }
        }
        Areas("day");
        // Close up: Mt. Mortar's middle mouth on Route 42, Mahogany's souvenir shop and its houses of logs, the Lake of
        // Rage's shore, Blackthorn's Gym under the cliffs and the city's stone houses
        At("Johto", 436, 173, Direction.Up); Frames(2); Shot("jw82_route_42_mt_mortar");
        At("Johto", 528, 176, Direction.Up); Frames(2); Shot("jw83_mahogany_souvenir_shop"); ShotCrop("jw83_mahogany_native", 640, 300, 640, 360, 2);
        var (lake, lx, ly) = AreaSpot("lake_of_rage");
        At(lake.Name, lx, ly, Direction.Down); Frames(2); Shot("jw84_lake_of_rage_shore");
        At("Johto", 668, 153, Direction.Up); Frames(2); Shot("jw85_blackthorn_gym"); ShotCrop("jw85_blackthorn_native", 640, 300, 640, 360, 2);
        At("Johto", 660, 167, Direction.Up); Frames(2); Shot("jw86_blackthorn_houses");

        string[] rooms =
        {
            "route_42_ecruteak_gatehouse", "mount_mortar_1f_entrance", "mount_mortar_1f_back", "mount_mortar_2f", "mount_mortar_b1f",
            "mahogany_pokecenter_1f", "mahogany_pokecenter_b1f", "mahogany_souvenir_shop", "mahogany_east_house",
            "mahogany_gym_room_1", "mahogany_gym_room_2", "mahogany_gym_leader_room", "route_43_mahogany_gatehouse",
            "route_43_gatehouse", "lake_of_rage_hidden_power_house", "lake_of_rage_fishing_guru_house",
            "ice_path_1f", "ice_path_b1f", "ice_path_b2f", "ice_path_b3f",
            "blackthorn_pokecenter_1f", "blackthorn_pokecenter_b1f", "blackthorn_pokemart", "blackthorn_gym",
            "blackthorn_west_house", "blackthorn_east_house", "blackthorn_move_tutor_house",
            "dragons_den_entrance", "dragons_den", "dragons_den_shrine", "dark_cave_route_31_side", "dark_cave_route_45_side"
        };
        for (int i = 0; i < rooms.Length; i++)
        {
            var (room, x, y) = AreaSpot(rooms[i]);
            At(room.Name, x, y, Direction.Up); Frames(2);
            Shot($"jr{106 + i}_{rooms[i]}");
            Console.WriteLine($"{rooms[i]}: {room.Name} {room.Width}×{room.Height}, {room.NPCs.Count} people, {room.Props.Count} pieces, music {room.BgmTrackAt(x, y)}, ways out {string.Join(", ", room.Warps.Select(w => $"({w.SourceX}, {w.SourceY}) to {w.TargetMap}"))}");
        }

        engine.Settings.TimeOfDay = TimeOfDay.Night;
        engine.ApplySettings(window: false);
        Areas("night");
        engine.Settings.TimeOfDay = TimeOfDay.Day;
        engine.ApplySettings(window: false);

        WalkThroughTheIcePath();
    }

    /// <summary>
    /// The walk plan 20 · J10 asks for: from before Mahogany Town's Pokémon Center east along Route 44 to the Ice Path's
    /// mouth, through its floors by the way the original leads (down to B3F and back up its other side) and out into
    /// Blackthorn City to its Pokémon Center, leg by leg, tile by tile at running pace, every frame held to a sixtieth of a
    /// second. A leg with no way on foot (a boulder in it, the ice's own lines) is told and passed over.
    /// </summary>
    void WalkThroughTheIcePath()
    {
        if (filter.Length > 0 && Environment.GetEnvironmentVariable("SHOTS_TIMING") != "1") return;
        (string Map, (int X, int Y) From, (int X, int Y) To)[] legs =
        {
            ("Johto", (534, 184), (633, 169)),
            ("IcePath_1F", (11, 58), (55, 7)),
            ("IcePath_B1F", (6, 21), (25, 8)),
            ("IcePath_B2F", (26, 11), (16, 17)),
            ("IcePath_B3F", (5, 11), (24, 12)),
            ("IcePath_B2F", (50, 5), (43, 22)),
            ("IcePath_B1F", (27, 40), (6, 42)),
            ("IcePath_1F", (55, 20), (55, 40)),
            ("Johto", (697, 144), (674, 176)),
        };
        var renderer = game.World;
        double total = 0, worst = 0;
        int frames = 0, tiles = 0;
        string worstAt = "";
        bool first = true;
        var watch = new System.Diagnostics.Stopwatch();
        foreach (var (name, from, to) in legs)
        {
            var map = MapDatabase.Get(name);
            List<(int X, int Y)> path;
            try { path = WalkingPath(map, from, to); }
            catch (InvalidOperationException e) { Console.WriteLine($"!! {name}: {e.Message}"); continue; }
            // Neither end is stood on: both are ways out, and standing on one would take it
            if (path.Count > 0 && path[^1] == to) path.RemoveAt(path.Count - 1);
            if (path.Count == 0) continue;
            At(name, path[0].X, path[0].Y, Direction.Down); Frames(first ? 30 : 4);
            if (first) { renderer.ResetStreamingStats(); first = false; }
            foreach (var (x, y) in path)
            {
                game.Player.SetPosition(x, y, Direction.Down);
                for (int f = 0; f < 8; f++)
                {
                    watch.Restart();
                    FrameClock.Fixed = ++tick / 60.0;
                    engine.Update(1f / 60f);
                    engine.Draw();
                    double ms = watch.Elapsed.TotalMilliseconds;
                    total += ms; frames++;
                    if (ms > worst) { worst = ms; worstAt = $"{name} ({x}, {y})"; }
                    while (watch.Elapsed.TotalMilliseconds < 1000.0 / 60.0) Thread.Sleep(1);
                }
            }
            tiles += path.Count;
            Console.WriteLine($"  {name}: {path.Count} tiles from {from} toward {to}");
        }
        Console.WriteLine($"walk from Mahogany Town through the Ice Path to Blackthorn City: {tiles} tiles, {total / Math.Max(1, frames):F2} ms/frame, worst frame {worst:F1} ms at {worstAt}");
        Console.WriteLine($"  streaming: {renderer.Streaming}");
    }
}
