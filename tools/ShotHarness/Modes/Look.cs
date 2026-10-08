partial class Harness
{
    public void LookMode()
    {
        // The reference frames by day. Pass the folder of an earlier run as the third argument to also get
        // before/after boards (compare_*.png).
        foreach (var name in new[] { "Starly", "Shinx", "Bidoof" })
            if (party.Count < 6) party.Add(new Pokemon(PokemonDatabase.Get(name)!, 4 + party.Count));
        party.Members[1].CurrentHP = party.Members[1].MaxHP / 3;
        party.Members[3].CurrentHP = party.Members[3].MaxHP / 7;

        GoTo("TwinleafTown", 11, 8, Direction.Down); Frames(2); Shot("look_1_twinleaf");
        ShotCrop("look_1b_twinleaf_native", 720, 300, 480, 270, 2);

        GoTo("TwinleafTown", 12, 7, Direction.Up);
        game.Dialogue.ShowDialogue("Barry", new List<string> { "Barry: Hey, Lucas! You're finally ready! Professor Rowan is waiting at Lake Verity!" });
        game.State = GameState.Dialogue;
        Frames(180); Shot("look_2_dialogue");
        game.Dialogue = new DialogueManager();

        var pb = StartBattle("Shinx", 5);
        ToMainMenu(pb);
        Shot("look_3_battle");
        ShotCrop("look_3c_hud_native", 1200, 640, 480, 270, 2);
        pb.HUD.MenuState = BattleMenuState.Moves; Frames(1); Shot("look_3b_moves");
        pb.HUD.MenuState = BattleMenuState.Main;

        game.State = GameState.PartyMenu;
        game.PartyScreen.Open();
        Frames(40); Shot("look_4_party");
        game.PartyScreen.Close();

        GoTo("Route201", 24, 8, Direction.Up); Frames(2); Shot("look_5_route201");
        GoTo("PlayerHouse", 4, 6, Direction.Up); Frames(2); Shot("look_6_house");
        GoTo("LakeVerity", 14, 11, Direction.Up); Frames(2); Shot("look_7_lake");

        game.State = GameState.Options;
        game.Options.Open();
        Frames(1); Shot("look_8_options");
        game.State = GameState.Overworld;

        GoTo("TwinleafTown", 11, 8, Direction.Down);
        Timing("field");
        StartBattle("Luxray", 30);
        ToMainMenu(game.Battle);
        Timing("battle");

        if (args.Length > 2)
            Boards(args[2], new[] { "look_1_twinleaf", "look_2_dialogue", "look_3_battle", "look_3b_moves", "look_4_party", "look_5_route201", "look_6_house", "look_7_lake" });
    }
}
