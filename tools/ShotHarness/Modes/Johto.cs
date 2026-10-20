partial class Harness
{
    /// <summary>
    /// Johto as HeartGold's maps make it (plan 20 · J2; not part of <c>all</c>, since its people would move every battle of
    /// the modes after it): a new game begun there (<c>--region Johto</c>), each open area by day and after dark from the
    /// open tile nearest its middle (<c>jw01</c> to <c>jw10</c>), every room from its own middle (<c>jr*</c>), and the walk
    /// from the player's door in New Bark Town to Cherrygrove City's Pokémon Center at running pace, every frame timed,
    /// with what streaming cost.
    /// </summary>
    public void JohtoMode()
    {
        // JOHTO_FROM=well plays only the second chapter (plan 20 · J7)
        if (Environment.GetEnvironmentVariable("JOHTO_FROM") == "well") { JohtoScenes(second: true); engine.NewGameRegion = "Sinnoh"; return; }
        // JOHTO_FROM=sea plays only the third chapter (plan 20 · J9), in a game set as the second leaves it
        if (Environment.GetEnvironmentVariable("JOHTO_FROM") == "sea") { JohtoScenes(third: true); engine.NewGameRegion = "Sinnoh"; return; }
        // JOHTO_FROM=rage plays only the fourth chapter's part a (plan 20 · J11), in a game set as the third leaves it
        if (Environment.GetEnvironmentVariable("JOHTO_FROM") == "rage") { JohtoScenes(fourth: true); engine.NewGameRegion = "Sinnoh"; return; }
        // JOHTO_FROM=takeover plays only the fourth chapter's part b (plan 20 · J11), in a game set as part a leaves it
        if (Environment.GetEnvironmentVariable("JOHTO_FROM") == "takeover") { JohtoScenes(takeover: true); engine.NewGameRegion = "Sinnoh"; return; }
        // JOHTO_FROM=west shoots only the west and the sea (plan 20 · J8), in a new game of its own
        if (Environment.GetEnvironmentVariable("JOHTO_FROM") == "west")
        {
            engine.NewGameRegion = "Johto";
            engine.StartNewGame();
            game.SaveSlot = 1;
            game.LocationSign.Hide();
            JohtoWestShots();
            engine.NewGameRegion = "Sinnoh";
            return;
        }
        // JOHTO_FROM=north shoots only the north (plan 20 · J10), in a new game of its own
        if (Environment.GetEnvironmentVariable("JOHTO_FROM") == "north")
        {
            engine.NewGameRegion = "Johto";
            engine.StartNewGame();
            game.SaveSlot = 1;
            game.LocationSign.Hide();
            JohtoMountainShots();
            engine.NewGameRegion = "Sinnoh";
            return;
        }
        // JOHTO_FROM=league shoots only the way to the League (plan 20 · J10, part b), in a new game of its own
        if (Environment.GetEnvironmentVariable("JOHTO_FROM") == "league")
        {
            engine.NewGameRegion = "Johto";
            engine.StartNewGame();
            game.SaveSlot = 1;
            game.LocationSign.Hide();
            JohtoLeagueShots();
            engine.NewGameRegion = "Sinnoh";
            return;
        }
        engine.NewGameRegion = "Johto";
        engine.StartNewGame();
        game.State = GameState.Overworld;
        game.SaveSlot = 1;
        game.LocationSign.Hide();
        Frames(30);
        Console.WriteLine($"a new game in Johto begins on {game.Map.Name} at ({game.Player.GridX}, {game.Player.GridY}), "
            + $"in {game.Map.AreaAt(game.Player.GridX, game.Player.GridY)?.DisplayName ?? "nowhere"}, with {string.Join(", ", game.Party.Members.Select(p => $"{p.Species.Name} {p.Level}"))}");
        Shot("jw00_new_game");

        string[] areas = { "new_bark", "route_29", "cherrygrove", "route_30", "route_31", "violet" };
        void Areas(string time)
        {
            for (int i = 0; i < areas.Length; i++)
            {
                var (map, x, y) = AreaSpot(areas[i]);
                At(map.Name, x, y, Direction.Up); Frames(2);
                Shot($"jw{(i < 5 ? 2 * i + (time == "night" ? 2 : 1) : 13 + (time == "night" ? 1 : 0)):00}_{areas[i]}_{time}");
                if (time == "day") Console.WriteLine($"{areas[i]}: {map.Name} ({x}, {y}), {map.DisplayNameAt(x, y)}, music {map.BgmTrackAt(x, y)}");
            }
        }
        Areas("day");
        // Route 32 (plan 20 · J4), from its head and by the Pokémon Center
        At("Johto", 470, 320, Direction.Down); Frames(2); Shot("jw15_route_32_north");
        At("Johto", 466, 412, Direction.Down); Frames(2); Shot("jw16_route_32_south");
        // Close up at the screen's own resolution: New Bark Town's turbines and houses, Cherrygrove's front
        At("Johto", 690, 400, Direction.Up); Frames(2); Shot("jw11_new_bark_houses"); ShotCrop("jw11_new_bark_houses_native", 640, 300, 640, 360, 2);
        At("Johto", 560, 395, Direction.Up); Frames(2); Shot("jw12_cherrygrove_center"); ShotCrop("jw12_cherrygrove_center_native", 640, 300, 640, 360, 2);

        string[] rooms =
        {
            "new_bark_player_house_1f", "new_bark_player_house_2f", "new_bark_rival_house_1f", "new_bark_rival_house_2f",
            "new_bark_elms_lab_1f", "new_bark_elms_lab_2f", "new_bark_southwest_house", "route_29_route_46_gatehouse",
            "cherrygrove_pokecenter_1f", "cherrygrove_pokecenter_b1f", "cherrygrove_pokemart", "cherrygrove_guide_gent_house",
            "cherrygrove_southwest_house", "cherrygrove_southeast_house", "route_30_apricorn_house", "route_30_mr_pokemon_house",
            "route_31_violet_gatehouse", "violet_pokecenter_1f", "violet_pokecenter_b1f", "violet_pokemart", "violet_pokemon_school",
            "violet_northwest_house", "violet_bellsprout_onyx_trade_house", "violet_route_36_gatehouse",
            // plan 20 · J4: Violet's Gym, Sprout Tower's floors and Route 32's rooms
            "violet_gym", "sprout_tower_1f", "sprout_tower_2f", "sprout_tower_3f", "route_32_pokecenter_1f", "route_32_pokecenter_b1f",
            "route_32_ruins_of_alph_gatehouse"
        };
        for (int i = 0; i < rooms.Length; i++)
        {
            var (room, x, y) = AreaSpot(rooms[i]);
            At(room.Name, x, y, Direction.Up); Frames(2);
            Shot($"jr{i + 1:00}_{rooms[i]}");
            Console.WriteLine($"{rooms[i]}: {room.Name} {room.Width}×{room.Height}, {room.NPCs.Count} people, {room.Props.Count} pieces, ways out {string.Join(", ", room.Warps.Select(w => $"({w.SourceX}, {w.SourceY}) to {w.TargetMap}"))}");
        }

        engine.Settings.TimeOfDay = TimeOfDay.Night;
        engine.ApplySettings(window: false);
        Areas("night");
        engine.Settings.TimeOfDay = TimeOfDay.Day;
        engine.ApplySettings(window: false);

        // The walk the plan asks for: from the player's door in New Bark Town along Route 29 to Cherrygrove City's
        // Pokémon Center, tile by tile at running pace, every frame timed and held to a sixtieth of a second
        if (filter.Length == 0 || Environment.GetEnvironmentVariable("SHOTS_TIMING") == "1")
        {
            var johto = MapDatabase.Get("Johto");
            var path = WalkingPath(johto, (695, 397), (564, 392));
            var renderer = game.World;
            At("Johto", 695, 397, Direction.Left); Frames(30);
            renderer.ResetStreamingStats();
            double total = 0, worst = 0;
            int frames = 0, mostChunks = 0;
            (int X, int Y) worstAt = default;
            var watch = new System.Diagnostics.Stopwatch();
            foreach (var (x, y) in path)
            {
                game.Player.SetPosition(x, y, Direction.Left);
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
            Console.WriteLine($"walk from the player's door in New Bark Town to Cherrygrove City's Pokémon Center: {path.Count} tiles, {total / frames:F2} ms/frame, "
                + $"worst frame {worst:F1} ms at ({worstAt.X}, {worstAt.Y}), at most {mostChunks} chunks loaded");
            Console.WriteLine($"  streaming: {renderer.Streaming}");
            foreach (var key in areas)
            {
                var (map, x, y) = AreaSpot(key);
                At(map.Name, x, y, Direction.Up);
                Timing("johto " + key);
            }
        }
        JohtoScenes();
        // And the Pokégear's cards, on the game the chapter left (plan 20 · J3)
        PokegearShots();
        // And the centre plan 20 · J5 opened: the Ruins of Alph, Union Cave, Route 33 and Azalea Town
        JohtoCentreShots();
        JohtoNorthShots();
        // And the west and the sea plan 20 · J8 opened: Routes 38 to 41, Olivine, its lighthouse, Cianwood and the Whirl Islands
        JohtoWestShots();
        // And the north plan 20 · J10 opened: Routes 42 to 46, Mt. Mortar, Mahogany, the Lake of Rage, the Ice Path,
        // Blackthorn, the Dragon's Den and the Dark Cave
        JohtoMountainShots();
        // And the way to the League plan 20 · J10, part b opened: Routes 27 and 26, Tohjo Falls, the reception gate,
        // Victory Road and the Indigo Plateau with the League's rooms
        JohtoLeagueShots();
        // And last the second chapter played by its own scripts, in a new game of its own (plan 20 · J7: jst30 to jst59)
        JohtoScenes(second: true);
        engine.NewGameRegion = "Sinnoh";
    }
}
