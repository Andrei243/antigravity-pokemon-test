partial class Harness
{
    /// <summary>
    /// Unova as Black 2's maps make it (plan 22 · U2; not part of <c>all</c>, since its people would move every battle of
    /// the modes after it): a new game begun there (<c>--region Unova</c>) in the player's room, Aspertia City, Route 19,
    /// Floccesy Town and Floccesy Ranch by day and after dark (u01 to u08), a few of their rooms (u10 to u16), Route 4,
    /// the Desert Resort's edge and the desert in their sandstorm, Nimbasa City and its amusement park by day and after
    /// dark (plan 22 · U6: u21 to u30) and a few of their rooms (u20, u31 to u34), Route 5, the drawbridge's end and its
    /// deck over the sea and Driftveil City by day and after dark (plan 22 · U6, part b: u35 to u42) and Nimbasa's and
    /// Driftveil's rooms with Clay's mine (u43 to u53), and the
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
            // Plan 22 · U6: Route 4, the Desert Resort's edge and the desert in their sandstorm, Nimbasa City and its park
            ("u21_route4_day", "u22_route4_night", "Unova", 405, 560, Direction.Up),
            ("u23_desert_edge_day", "u24_desert_edge_night", "Unova", 372, 492, Direction.Up),
            ("u25_desert_day", "u26_desert_night", "DesertResort", 50, 50, Direction.Up),
            ("u27_nimbasa_day", "u28_nimbasa_night", "Unova", 430, 470, Direction.Up),
            ("u29_amusement_park_day", "u30_amusement_park_night", "NimbasaCity_AmusementPark", 60, 14, Direction.Right),
            // Plan 22 · U6, part b: Route 5, the drawbridge's east end over the sea, and Driftveil City
            ("u35_route5_day", "u36_route5_night", "Unova", 350, 434, Direction.Up),
            ("u37_drawbridge_end_day", "u38_drawbridge_end_night", "DriftveilDrawbridge", 209, 47, Direction.Left),
            ("u39_driftveil_day", "u40_driftveil_night", "Unova", 190, 415, Direction.Up),
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
            // Plan 22 · U6: a house of flats on Route 4, Join Avenue, the desert's gate house, Elesa's runway and Nimbasa's Center
            ("u20_route4_house", "Route4_House1_1F", 7, 8, Direction.Up),
            ("u31_join_avenue", "JoinAvenue", 15, 74, Direction.Up),
            ("u32_desert_gate", "DesertResortGate", 5, 8, Direction.Up),
            ("u33_nimbasa_gym", "NimbasaCity_Gym", 15, 57, Direction.Up),
            ("u34_nimbasa_center", "NimbasaCity_PokemonCenter", 7, 13, Direction.Up),
            // Plan 22 · U6, part b: Nimbasa's other rooms, its gate to Route 5, Driftveil's rooms and Clay's mine of ours
            ("u43_big_stadium", "NimbasaCity_BigStadium", 11, 17, Direction.Up),
            ("u44_musical_theater", "MusicalTheater", 14, 18, Direction.Up),
            ("u45_battle_institute", "NimbasaCity_BattleInstitute", 7, 11, Direction.Up),
            ("u46_roller_coaster", "NimbasaCity_RollerCoaster", 14, 43, Direction.Up),
            ("u47_route5_gate", "NimbasaGate_Route5", 8, 5, Direction.Left),
            ("u48_driftveil_center", "DriftveilCity_PokemonCenter", 7, 17, Direction.Up),
            ("u49_plasma_house", "DriftveilCity_PlasmaHouse", 7, 22, Direction.Up),
            ("u50_market", "DriftveilCity_Market", 12, 14, Direction.Up),
            ("u51_gym_lobby", "DriftveilCity_GymLobby", 7, 13, Direction.Up),
            ("u52_clays_mine", "DriftveilCity_Gym", 10, 26, Direction.Up),
            ("u53_ferris_wheel", "NimbasaCity_AmusementPark", 57, 17, Direction.Up),
        };
        foreach (var (name, map, x, y, facing) in more)
        {
            At(map, x, y, facing); Frames(2); Shot(name);
        }

        // The drawbridge's middle, the player stood on its deck five tiles over the sea (plan 22 · U6, part b; a tile is
        // come onto at its ground, so the deck's height is given by hand)
        void OnTheDeck(string name)
        {
            At("DriftveilDrawbridge", 120, 47, Direction.Left);
            game.Player.SetHeight(game.Map.DeckAt(120, 47) ?? game.Map.HeightAt(120, 47));
            Frames(2); Shot(name);
            ShotCrop(name + "_native", 640, 300, 640, 360, 2);
            Console.WriteLine($"{name}: camera {game.Map.Camera}, deck {game.Map.DeckAt(120, 47)}, sea {game.Map.HeightAt(120, 30)}");
        }
        OnTheDeck("u41_drawbridge_deck_day");

        engine.Settings.TimeOfDay = TimeOfDay.Night;
        engine.ApplySettings(window: false);
        foreach (var (_, night, map, x, y, facing) in places)
        {
            At(map, x, y, facing); Frames(2); Shot(night);
        }
        OnTheDeck("u42_drawbridge_deck_night");
        engine.Settings.TimeOfDay = TimeOfDay.Day;
        engine.ApplySettings(window: false);

        // The walk the plan asks for, from the north door of Aspertia City's gate (the gate is a room since plan 22 · U5, so a
        // walk on the map of Unova starts past it) down Route 19 to Floccesy Town's Pokémon Center, tile by tile at running
        // pace, every frame timed and held to a sixtieth of a second as the screen holds it
        if (filter.Length == 0 || Environment.GetEnvironmentVariable("SHOTS_TIMING") == "1")
        {
            var unova = MapDatabase.Get("Unova");
            var path = WalkingPath(unova, (52, 702), (105, 694));
            var renderer = game.World;
            At("Unova", 52, 702, Direction.Up); Frames(30);
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
            Console.WriteLine($"walk from Aspertia City's gate to Floccesy Town's Pokémon Center: {path.Count} tiles, {total / Math.Max(1, frames):F2} ms/frame, "
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
