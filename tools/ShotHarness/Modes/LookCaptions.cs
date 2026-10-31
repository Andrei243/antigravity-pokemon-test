partial class Harness
{
    /// <summary>
    /// Plan 12 · Q9, part b (`look_11_*`), the end of `look` so that no shot before it moves: the CAPTIONS and HOLD TO
    /// CONFIRM rows on, captions beside a line of the text box in the field and over a battle's menu, and the reset's YES
    /// half held with its ring.
    /// </summary>
    private void LookCaptionsAndHold()
    {
        var options = game.Options;
        void GoToRow(OptionRow row) { while (Enum.GetValues<OptionRow>()[options.SelectedIndex] != row) options.Move(1); }

        game.State = GameState.Options;
        options.Open();
        GoToRow(OptionRow.Captions);
        OptionsScreen.Change(engine.Settings, OptionRow.Captions, 1);
        Frames(1); Shot("look_11_options_captions_on");
        GoToRow(OptionRow.HoldToConfirm);
        OptionsScreen.Change(engine.Settings, OptionRow.HoldToConfirm, 1);
        Frames(1); Shot("look_11b_options_hold_on");
        game.ApplySettings();
        Accessibility.CaptionsShown.Clear();

        // Captions beside a line: a door, a cry, and a ball's three wobbles counted on one pill
        game.State = GameState.Overworld;
        GoTo("TwinleafTown", 12, 7, Direction.Up);
        Frames(2);
        game.Dialogue.ShowDialogue("Barry", new List<string> { "Barry: Did you hear that? Something's out there in the grass!" });
        game.State = GameState.Dialogue;
        AudioManager.PlaySound("door_open");
        Frames(20);
        AudioManager.PlayCry(PokemonDatabase.Get("Starly")!);
        Frames(20);
        for (int i = 0; i < 3; i++) { AudioManager.PlaySound("ball_shake"); Frames(6); }
        Frames(40);
        Console.WriteLine("captions standing: " + string.Join(" | ", Accessibility.CaptionsShown.Lines.Select(l => $"{l.Text} x{l.Times} at {l.Age:0.00}s")));
        Shot("look_11c_captions_beside_a_line");
        ShotCrop("look_11d_captions_close", 60, 540, 760, 320, 2);
        game.Dialogue = new DialogueManager();
        Accessibility.CaptionsShown.Clear();

        // Over a battle's menu: a heavy hit and the foe's cry
        var battle = StartBattle("Shinx", 5);
        ToMainMenu(battle);
        Accessibility.CaptionsShown.Clear();
        AudioManager.PlaySound("hit_super");
        AudioManager.PlayCry(PokemonDatabase.Get("Shinx")!);
        AudioManager.PlaySound(PokemonPlatinumEngine.Audio.SoundBank.MoveSound("Tackle", PokemonType.Normal));
        Frames(12);
        Console.WriteLine("captions over the battle: " + string.Join(" | ", Accessibility.CaptionsShown.Lines.Select(l => l.Text)));
        Shot("look_11e_captions_battle");
        Accessibility.CaptionsShown.Clear();

        // The reset's YES held halfway: its ring
        game.State = GameState.Options;
        options.Open();
        GoToRow(OptionRow.ResetDefaults);
        options.Confirm(engine.Settings);
        for (int i = 0; i < 20; i++) options.Animate(0.05f);
        options.ToggleReset();
        // The confirm button pressed on YES and held through the game's own frames, about half of the hold
        var press = ActionFrame.Press(GameAction.Confirm);
        InputManager.Feed(press);
        Frames(1);
        InputManager.Feed(press.Next(GameAction.Confirm));
        Frames(11);
        Accessibility.CaptionsShown.Clear();
        Shot("look_11f_reset_held");
        ShotCrop("look_11g_reset_held_close", 900, 540, 600, 180, 2);
        Console.WriteLine($"the reset held: asking {options.ResetAsking}, fill {options.ResetHold.Fill:0.00}, captions {(engine.Settings.Captions ? "on" : "off")}");
        // Let go early: the question stands and nothing was reset
        InputManager.Feed(null);
        Frames(1);
        Console.WriteLine($"let go: asking {options.ResetAsking}, holding {options.ResetHold.Holding}, captions {(engine.Settings.Captions ? "on" : "off")}");
        options.AnswerReset(engine.Settings, false);
        for (int i = 0; i < 20; i++) options.Animate(0.05f);

        // Both rows put back
        engine.Settings.Captions = false;
        engine.Settings.HoldToConfirm = false;
        game.ApplySettings();
        game.State = GameState.Overworld;
    }
}
