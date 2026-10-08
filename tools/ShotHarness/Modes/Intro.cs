partial class Harness
{
    // ---------------------------------------------------------------- the new-game introduction (plan 04 · G10)

    // The professor's welcome step by step: fading in, each beat of his talk, the Pokémon coming out of its ball,
    // the choice of who to be, the name keyboard, the send-off. Run by itself (not as part of `all`) it also lets
    // the introduction end and shows the game it starts: the field, the Trainer Card and a battle as the girl.
    public void IntroMode()
    {
        var intro = game.Intro;
        game.State = GameState.Intro;
        intro.Open();
        // Presses the A button through whatever is being said until the introduction reaches a phase
        void Until(IntroPhase phase, int limit = 2000)
        {
            for (int guard = 0; guard < limit && intro.Phase != phase; guard++)
            {
                if (intro.Talking && intro.LineComplete) intro.PressConfirm();
                Frames(1);
            }
        }
        // Waits for the line being written to be all there
        void Line() { for (int guard = 0; guard < 600 && intro.Talking && !intro.LineComplete; guard++) Frames(1); Frames(2); }

        Frames(36); Shot("i01_fading_in");
        Until(IntroPhase.Greeting); Frames(40); Shot("i02_hello");
        Line(); intro.PressConfirm(); Line(); intro.PressConfirm(); Line(); Shot("i03_professor_rowan");
        Until(IntroPhase.World); Line(); Shot("i04_the_world");
        Until(IntroPhase.BallOpens); Frames(32); Shot("i05_ball");
        Frames(14); Shot("i06_flash");
        Frames(14); Shot("i07_pokemon_appears");
        Until(IntroPhase.Alongside); Frames(14); Shot("i08_pokemon_hops");
        Line(); Frames(60); Shot("i09_alongside");
        Timing("introduction");
        Until(IntroPhase.BallCloses); Frames(14); Shot("i10_pokemon_returns");
        Until(IntroPhase.AboutYou); Line(); Shot("i11_about_you");
        Until(IntroPhase.ChooseLook); Frames(40); Shot("i12_boy_or_girl");
        intro.Move(1, 0); Frames(40); Shot("i13_the_girl");
        intro.PressConfirm(); Frames(30); Shot("i14_so_you_are_a_girl");
        intro.PressConfirm();
        Until(IntroPhase.AskName); Line(); Shot("i15_your_name");
        Until(IntroPhase.EnterName); Frames(30); Shot("i16_keyboard");
        // "Maya", through the keyboard's own cursor: M is the third key of the second row
        intro.Move(0, 1); intro.Move(1, 0); intro.Move(1, 0); intro.PressConfirm();
        foreach (char c in "aya") intro.Entry!.Type(c);
        Frames(20); Shot("i17_keyboard_name");
        ShotCrop("i17b_keyboard_native", 640, 180, 1200, 760, 2);
        intro.PressStart(); Frames(4); Shot("i18_keyboard_ok");
        intro.PressConfirm(); Frames(30); Shot("i19_so_you_are_maya");
        intro.PressConfirm();
        // The friend next door (plan 02 · S4): he steps in beside the professor, and his name is the one Platinum gives
        // him unless another is typed
        Until(IntroPhase.AskRival); Frames(50); Line(); Shot("i19b_your_friend");
        Until(IntroPhase.EnterRival); Frames(30); Shot("i19c_his_name");
        intro.PressStart(); intro.PressConfirm(); Frames(30); Shot("i19d_so_his_name_is");
        intro.PressConfirm();
        Until(IntroPhase.Farewell); Line(); Shot("i20_farewell");
        Until(IntroPhase.SendOff); Frames(48); Shot("i21_send_off");
        Frames(40); Shot("i22_shrinking");
        Frames(30); Shot("i23_nearly_gone");

        if (mode == "intro")
        {
            // Let it end: the game begins as Maya, the girl
            for (int guard = 0; guard < 400 && game.State == GameState.Intro; guard++) Frames(1);
            Frames(60); Shot("i30_the_game_begins");
            // In her room, with the television's special on (plan 02 · S4)
            var tv = game.Dialogue;
            for (int guard = 0; guard < 600 && !(tv.IsActive && tv.IsCurrentLineComplete); guard++) Frames(1);
            Frames(10); Shot("i30b_the_television");
            // Past the first chapter for the rest: a Pokémon to battle with
            for (int guard = 0; guard < 3000 && game.State != GameState.Overworld; guard++)
            {
                if (tv.IsActive && tv.IsCurrentLineComplete) tv.Advance();
                Frames(1);
            }
            PastTheOpening();
            Frames(120);
            game.ChooseFromStartMenu(StartMenuChoice.Trainer);
            Frames(40); Shot("i31_her_trainer_card");
            game.TrainerCard.Close();
            game.State = GameState.Overworld;
            var her = StartBattle("Starly", 3);
            Frames(150); Shot("i32_her_battle");
            game.State = GameState.Overworld;
            // Sandgem's assistant is the one the player isn't: Lucas, with his own lines
            game.Map = MapDatabase.Get("Sinnoh");
            var helper = MapDatabase.Get("Sinnoh").NPCs.First(n => n.NpcType == "Assistant");
            game.Player.SetPosition(helper.GridX, helper.GridY + 1, Direction.Up);
            Frames(20);
            game.Dialogue.ShowDialogue(helper.Name, helper.DialogLines);
            game.State = GameState.Dialogue;
            Frames(90); Shot("i33_the_assistant");
        }
        else
        {
            intro.Close();
            game.State = GameState.Overworld;
        }
    }
}
