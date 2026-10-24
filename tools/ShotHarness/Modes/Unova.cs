partial class Harness
{
    /// <summary>
    /// Unova as Black 2's maps make it (plan 22 · U2; not part of <c>all</c>, since its people would move every battle of
    /// the modes after it): a new game begun there (<c>--region Unova</c>) in the player's room, Aspertia City, Route 19,
    /// Floccesy Town and Floccesy Ranch by day and after dark (u01 to u08), a few of their rooms (u10 to u16), and the
    /// walk from the player's door in Aspertia City down Route 19 to Floccesy Town's Pokémon Center at running pace, every
    /// frame timed, with what streaming cost.
    /// </summary>
    public void UnovaMode()
    {
        engine.NewGameRegion = "Unova";
        engine.StartNewGame();
        game.State = GameState.Overworld;
        game.SaveSlot = 1;
        game.LocationSign.Hide();
        Frames(30);
        Console.WriteLine($"a new game in Unova begins on {game.Map.Name} at ({game.Player.GridX}, {game.Player.GridY}), "
            + $"with {(game.Party.Members.Count == 0 ? "nobody" : string.Join(", ", game.Party.Members.Select(p => $"{p.Species.Name} {p.Level}")))}");
        Shot("u00_new_game");

        // Each place by day, then the same tiles after dark
        (string Day, string Night, string Map, int X, int Y, Direction Facing)[] places =
        {
            ("u01_aspertia_day", "u02_aspertia_night", "Unova", 48, 739, Direction.Up),
            ("u03_route19_day", "u04_route19_night", "Unova", 73, 693, Direction.Right),
            ("u05_floccesy_day", "u06_floccesy_night", "Unova", 113, 678, Direction.Up),
            ("u07_ranch_day", "u08_ranch_night", "FloccesyRanch", 30, 38, Direction.Up),
        };
        foreach (var (day, _, map, x, y, facing) in places)
        {
            At(map, x, y, facing); Frames(2); Shot(day);
            ShotCrop(day + "_native", 640, 300, 640, 360, 2);
        }
        // Beside them: the street before the player's door, Route 20's grass and the ranch's gate on Route 20
        (string Name, string Map, int X, int Y, Direction Facing)[] more =
        {
            ("u01b_aspertia_home", "Unova", 47, 762, Direction.Down),
            ("u03b_route19_grass", "Unova", 87, 690, Direction.Left),
            ("u05b_route20", "Unova", 157, 656, Direction.Up),
            ("u07b_ranch_gate", "Unova", 142, 634, Direction.Up),
            ("u10_players_house", "AspertiaCity_PlayersHouse", 14, 5, Direction.Left),
            ("u11_aspertia_center", "AspertiaCity_PokemonCenter", 7, 13, Direction.Up),
            ("u12_trainers_school", "AspertiaCity_TrainerSchool", 10, 17, Direction.Up),
            ("u13_aspertia_gym", "AspertiaCity_Gym", 13, 16, Direction.Up),
            ("u14_alders_house", "FloccesyTown_AldersHouse", 6, 8, Direction.Up),
            ("u15_floccesy_center", "FloccesyTown_PokemonCenter", 7, 13, Direction.Up),
            ("u16_aspertia_gate", "AspertiaGate", 5, 5, Direction.Up),
        };
        foreach (var (name, map, x, y, facing) in more)
        {
            At(map, x, y, facing); Frames(2); Shot(name);
        }

        engine.Settings.TimeOfDay = TimeOfDay.Night;
        engine.ApplySettings(window: false);
        foreach (var (_, night, map, x, y, facing) in places)
        {
            At(map, x, y, facing); Frames(2); Shot(night);
        }
        engine.Settings.TimeOfDay = TimeOfDay.Day;
        engine.ApplySettings(window: false);

        // The walk the plan asks for: from the player's door in Aspertia City through its gate and down Route 19 to
        // Floccesy Town's Pokémon Center, tile by tile at running pace, every frame timed and held to a sixtieth of a
        // second as the screen holds it
        if (filter.Length == 0 || Environment.GetEnvironmentVariable("SHOTS_TIMING") == "1")
        {
            var unova = MapDatabase.Get("Unova");
            var path = WalkingPath(unova, (47, 762), (105, 694));
            var renderer = game.World;
            At("Unova", 47, 762, Direction.Down); Frames(30);
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
            Console.WriteLine($"walk from the player's door in Aspertia City to Floccesy Town's Pokémon Center: {path.Count} tiles, {total / Math.Max(1, frames):F2} ms/frame, "
                + $"worst frame {worst:F1} ms at ({worstAt.X}, {worstAt.Y}), at most {mostChunks} chunks loaded");
            Console.WriteLine($"  streaming: {renderer.Streaming}");
            foreach (var (label, x, y) in new[] { ("aspertia city", 48, 739), ("route 19", 73, 693), ("floccesy town", 113, 678), ("route 20", 157, 656) })
            {
                At("Unova", x, y, Direction.Up);
                Timing("unova " + label);
            }
        }
        // And last the first chapter, in a new game of its own (plan 22 · U5, part a: us01 to us30)
        UnovaScenes();
        engine.NewGameRegion = "Sinnoh";
    }
}
