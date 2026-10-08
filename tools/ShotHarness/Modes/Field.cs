partial class Harness
{
    // ---------------------------------------------------------------- overworld
    public void FieldMode()
    {
        GoTo("TwinleafTown", 11, 8, Direction.Down); Shot("01_twinleaf");
        GoTo("TwinleafTown", 6, 9, Direction.Up); Shot("02_twinleaf_house");
        GoTo("TwinleafTown", 11, 1, Direction.Up); Shot("02b_twinleaf_north");
        GoTo("TwinleafTown", 9, 14, Direction.Left); Shot("02c_twinleaf_pond");
        GoTo("PlayerHouse", 4, 6, Direction.Up); Shot("03_player_house");
        GoTo("RivalHouse", 5, 7, Direction.Up); Shot("03b_rival_house");
        GoTo("TwinleafTown", 17, 9, Direction.Up); Shot("03c_twinleaf_rival_door");
        GoTo("Route201", 14, 10, Direction.Left); Shot("04_route201");
        GoTo("Route201", 24, 8, Direction.Up); Shot("04b_route201_grass");
        GoTo("Route201", 22, 11, Direction.Down); Shot("04c_route201_ledge");
        GoTo("SandgemTown", 14, 8, Direction.Up); Shot("05_sandgem");
        GoTo("SandgemTown", 8, 19, Direction.Up); Shot("05b_sandgem_lab");
        GoTo("PokemonCenter", 5, 6, Direction.Up); Shot("06_pokecenter");
        GoTo("PokeMart", 4, 5, Direction.Up); Shot("06b_mart");
        GoTo("RowanLab", 5, 7, Direction.Up); Shot("06c_lab");
        GoTo("LakeVerity", 14, 11, Direction.Up); Shot("07_lake");
        GoTo("Route202", 14, 10, Direction.Up); Shot("07b_route202");
        GoTo("Route202", 15, 2, Direction.Up); Shot("07c_route202_north");
        GoTo("JubilifeCity", 20, 30, Direction.Up); Shot("08_jubilife_south");
        GoTo("JubilifeCity", 19, 18, Direction.Up); Shot("08b_jubilife_crossroads");
        GoTo("JubilifeCity", 7, 9, Direction.Up); Shot("08c_jubilife_school");
        GoTo("JubilifeCity", 30, 10, Direction.Up); Shot("08d_jubilife_poketch_tv");
        GoTo("JubilifeCity", 28, 29, Direction.Up); Shot("08e_jubilife_center_mart");
        GoTo("JubilifeCity", 9, 29, Direction.Up); Shot("08f_jubilife_terminal");
        GoTo("TrainersSchool", 6, 9, Direction.Up); Shot("09_trainers_school");
        GoTo("PoketchCompany", 5, 7, Direction.Up); Shot("09b_poketch_company");
        GoTo("JubilifePokemonCenter", 5, 6, Direction.Up); Shot("09c_jubilife_center");
        GoTo("PalletTown", 9, 9, Direction.Down); Shot("10_pallet");
        GoTo("PalletTown", 9, 17, Direction.Down); Shot("10b_pallet_pier");
        GoTo("PalletPlayerHouse", 4, 6, Direction.Up); Shot("10c_pallet_house");

        // A trainer spotting the player
        GoTo("Route201", 24, 9, Direction.Up);
        var tristan = MapDatabase.Get("Sinnoh").NPCs.First(n => n.IsTrainer);
        tristan.HasSpottedPlayer = true;
        tristan.ExclamationTimer = 5f;
        Frames(1); Shot("04d_route201_spotted");
        tristan.HasSpottedPlayer = false;
        tristan.ExclamationTimer = 0f;

        // Dialogue box
        GoTo("TwinleafTown", 12, 7, Direction.Up);
        game.Dialogue.ShowDialogue("Barry", new List<string> { "Barry: Hey, Lucas! You're finally ready! Professor Rowan is waiting at Lake Verity!" });
        game.State = GameState.Dialogue;
        Frames(180); Shot("08_dialogue");
        game.Dialogue = new DialogueManager();

        // With the folder of an earlier run as the third argument, before/after boards of the outdoor shots too
        if (args.Length > 2)
            Boards(args[2], new[] { "01_twinleaf", "02_twinleaf_house", "02b_twinleaf_north", "02c_twinleaf_pond", "04_route201", "04b_route201_grass", "04c_route201_ledge", "05_sandgem", "05b_sandgem_lab", "07_lake", "07b_route202", "07c_route202_north" });

        // Frame timing (no vsync in the hidden window)
        foreach (var m in new[] { "TwinleafTown", "Route201", "SandgemTown", "PokemonCenter" })
        {
            GoTo(m, m == "PokemonCenter" ? 5 : 12, m == "PokemonCenter" ? 6 : 10, Direction.Down);
            Timing(m);
        }
    }
}
