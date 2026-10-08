partial class Harness
{
    // ---------------------------------------------------------------- buildings, props and rooms (plan 04 · G5)
    public void BuildingsMode()
    {
        // Every building from the street, with native-resolution close-ups of one of each family
        (string Name, string Map, int X, int Y)[] streets =
        {
            ("b01_twinleaf_house", "TwinleafTown", 6, 9), ("b02_twinleaf_rival", "TwinleafTown", 17, 9), ("b03_twinleaf_town", "TwinleafTown", 11, 9),
            ("b04_sandgem_center", "SandgemTown", 6, 8), ("b05_sandgem_mart", "SandgemTown", 22, 8), ("b06_sandgem_lab", "SandgemTown", 7, 19),
            ("b07_jubilife_school", "JubilifeCity", 7, 9), ("b08_jubilife_poketch_tv", "JubilifeCity", 30, 9),
            ("b09_jubilife_center_mart", "JubilifeCity", 28, 28), ("b10_jubilife_terminal", "JubilifeCity", 9, 28),
            ("b11_jubilife_crossroads", "JubilifeCity", 19, 17), ("b12_pallet", "PalletTown", 5, 8)
        };
        foreach (var (name, map, x, y) in streets)
        {
            GoTo(map, x, y, Direction.Up); Frames(2); Shot(name);
            if (name is "b01_twinleaf_house" or "b04_sandgem_center" or "b07_jubilife_school" or "b10_jubilife_terminal" or "b12_pallet")
                ShotCrop(name + "_native", 640, 150, 640, 360, 2);
        }

        // The same streets after dark: windows, lamps and the light they throw
        engine.Settings.TimeOfDay = TimeOfDay.Night;
        engine.ApplySettings(window: false);
        GoTo("TwinleafTown", 11, 9, Direction.Up); Frames(2); Shot("b20_twinleaf_night");
        GoTo("SandgemTown", 14, 8, Direction.Up); Frames(2); Shot("b21_sandgem_night");
        GoTo("JubilifeCity", 28, 28, Direction.Up); Frames(2); Shot("b22_jubilife_night");
        GoTo("PlayerHouse", 4, 6, Direction.Up); Frames(2); Shot("b23_house_night");
        engine.Settings.TimeOfDay = TimeOfDay.LateNight;
        engine.ApplySettings(window: false);
        GoTo("TwinleafTown", 11, 9, Direction.Up); Frames(2); Shot("b24_twinleaf_late_night");
        engine.Settings.TimeOfDay = TimeOfDay.Twilight;
        engine.ApplySettings(window: false);
        GoTo("JubilifeCity", 19, 17, Direction.Up); Frames(2); Shot("b25_jubilife_twilight");
        GoTo("PokemonCenter", 5, 6, Direction.Up); Frames(2); Shot("b26_center_twilight");
        engine.Settings.TimeOfDay = TimeOfDay.Day;
        engine.ApplySettings(window: false);

        // Every room
        (string Name, string Map, int X, int Y)[] rooms =
        {
            ("b30_player_house", "PlayerHouse", 4, 6), ("b31_rival_house", "RivalHouse", 5, 7), ("b32_pallet_house", "PalletPlayerHouse", 4, 6),
            ("b33_pokemon_center", "PokemonCenter", 5, 6), ("b34_mart", "PokeMart", 4, 5), ("b35_lab", "RowanLab", 5, 7),
            ("b36_school", "TrainersSchool", 6, 9), ("b37_poketch", "PoketchCompany", 5, 7)
        };
        foreach (var (name, map, x, y) in rooms)
        {
            GoTo(map, x, y, Direction.Up); Frames(2); Shot(name);
            if (name is "b30_player_house" or "b33_pokemon_center" or "b34_mart" or "b35_lab")
            {
                ShotCrop(name + "_native_left", 400, 60, 640, 360, 2);
                ShotCrop(name + "_native_right", 900, 60, 640, 360, 2);
            }
        }

        GoTo("TwinleafTown", 11, 9, Direction.Up); Timing("twinleaf");
        GoTo("JubilifeCity", 28, 28, Direction.Up); Timing("jubilife");
        GoTo("JubilifeCity", 19, 17, Direction.Up); Timing("jubilife crossroads");
        GoTo("PokemonCenter", 5, 6, Direction.Up); Timing("pokemon center");
        GoTo("TrainersSchool", 6, 9, Direction.Up); Timing("trainers school");

        if (args.Length > 2)
            Boards(args[2], streets.Select(s => s.Name).Concat(rooms.Select(r => r.Name))
                .Concat(new[] { "b20_twinleaf_night", "b21_sandgem_night", "b22_jubilife_night", "b23_house_night", "b24_twinleaf_late_night", "b25_jubilife_twilight", "b26_center_twilight" }).ToArray());
    }
}
