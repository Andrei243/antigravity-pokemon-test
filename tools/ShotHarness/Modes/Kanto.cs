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
        };
        foreach (var (name, map, x, y, facing) in places)
        {
            At(map, x, y, facing); Frames(2); Shot(name);
            if (name is "k01_pallet_day" or "k05_viridian_day") ShotCrop(name + "_native", 640, 300, 640, 360, 2);
        }

        engine.Settings.TimeOfDay = TimeOfDay.Night;
        engine.ApplySettings(window: false);
        At("Kanto", 70, 272, Direction.Up); Frames(2); Shot("k02_pallet_night");
        At("Kanto", 72, 240, Direction.Up); Frames(2); Shot("k04_route1_night");
        At("Kanto", 74, 208, Direction.Up); Frames(2); Shot("k06_viridian_night");
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
        engine.NewGameRegion = "Sinnoh";
    }
}
