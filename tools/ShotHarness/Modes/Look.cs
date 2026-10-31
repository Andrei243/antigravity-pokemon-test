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
        GoTo("PlayerHouse", 6, 9, Direction.Up); Frames(2); Shot("look_6_house");
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

        // The help pages under OPTIONS and the move hints' row (plan 12 · Q10), last so that no shot before them moves
        game.State = GameState.Options;
        var options = game.Options;
        options.Open();
        void GoToRow(OptionRow row) { while (Enum.GetValues<OptionRow>()[options.SelectedIndex] != row) options.Move(1); }
        GoToRow(OptionRow.MoveHints);
        Frames(1); Shot("look_12_options_hints");
        // HELP is the last row, whatever rows have come before it since
        GoToRow(OptionRow.Help);
        options.Confirm();
        Frames(1); Shot("look_12_help");
        options.Help.Move(0, -1);
        options.Help.Move(0, -1);
        options.Help.Move(0, -1);
        options.Help.Move(1, 0);
        Frames(1); Shot("look_12b_help_controls");
        options.Help.Move(1, 0);
        Frames(1); Shot("look_12c_help_notes");
        options.Help.Cancel();
        options.Open();

        // The two rows of plan 16 · T3, at the end of the settings: as they are unless chosen, and each chosen otherwise.
        // The option to wait behind another window is on for a shot here and the run goes on all the same: the engine
        // never asks whether its window is in front, it is told (GameEngine.WindowFocused), and nobody here says it isn't
        GoToRow(OptionRow.FocusPause);
        Frames(1); Shot("look_13_options_pause");
        OptionsScreen.Change(engine.Settings, OptionRow.FocusPause, 1);
        Frames(30); Shot("look_13b_options_pause_on");
        Console.WriteLine($"pause in the background {(engine.Settings.FocusPause ? "on" : "off")}, the window in front as far as the game is told: {engine.WindowFocused}, held: {engine.HeldForFocus}");
        OptionsScreen.Change(engine.Settings, OptionRow.FocusPause, 1);
        GoToRow(OptionRow.FrameCap);
        Frames(1); Shot("look_13c_options_frame_limit");
        OptionsScreen.Change(engine.Settings, OptionRow.FrameCap, -1);
        Frames(1); Shot("look_13d_options_frame_limit_30");
        OptionsScreen.Change(engine.Settings, OptionRow.FrameCap, -1);
        Frames(1); Shot("look_13e_options_no_limit");
        engine.Settings.FrameCap = 60;
        Console.WriteLine($"the options as they were again: pause {engine.Settings.FocusPause}, frame limit {engine.Settings.FrameCap}");

        // Platinum's battle options and the speakers' third way (plan 08 · P1), at the end of the settings: as they are
        // unless chosen, then each the other way, then put back
        GoToRow(OptionRow.BattleScene);
        Frames(1); Shot("look_14_options_battle_scene");
        OptionsScreen.Change(engine.Settings, OptionRow.BattleScene, 1);
        Frames(1); Shot("look_14b_options_battle_scene_off");
        GoToRow(OptionRow.BattleStyle);
        Frames(1); Shot("look_14c_options_battle_style");
        OptionsScreen.Change(engine.Settings, OptionRow.BattleStyle, 1);
        Frames(1); Shot("look_14d_options_battle_style_set");
        GoToRow(OptionRow.Speakers);
        OptionsScreen.Change(engine.Settings, OptionRow.Speakers, -1);
        Frames(1); Shot("look_14e_options_speakers_mono");
        engine.Settings.BattleScene = true;
        engine.Settings.BattleStyle = BattleStyle.Shift;
        engine.Settings.Speakers = SpeakerMode.Stereo;
        engine.ApplySettings(window: false);
        Console.WriteLine($"the options as they were again: battle scene {engine.Settings.BattleScene}, battle style {engine.Settings.BattleStyle}, speakers {engine.Settings.Speakers}");

        // The pad's rows (plan 12 · Q4), the last before HELP: as they are unless chosen, each changed, and the key hints
        // naming the pad's buttons once the player's last touch was the pad
        GoToRow(OptionRow.ButtonMode);
        Frames(1); Shot("look_15_options_button_mode");
        OptionsScreen.Change(engine.Settings, OptionRow.ButtonMode, 1);
        Frames(1); Shot("look_15b_options_button_mode_start_is_x");
        OptionsScreen.Change(engine.Settings, OptionRow.ButtonMode, 1);
        Frames(1); Shot("look_15c_options_button_mode_l_is_a");
        GoToRow(OptionRow.StickDeadZone);
        OptionsScreen.Change(engine.Settings, OptionRow.StickDeadZone, 1);
        Frames(1); Shot("look_15d_options_stick_dead_zone");
        GoToRow(OptionRow.Gamepad);
        OptionsScreen.Change(engine.Settings, OptionRow.Gamepad, 1);
        Frames(1); Shot("look_15e_options_controller");
        engine.ApplySettings(window: false);
        InputManager.LastDevice = InputDevice.Pad;
        Frames(1); Shot("look_15f_options_hints_on_the_pad");
        GoToRow(OptionRow.Help);
        Frames(1); Shot("look_15g_options_help_on_the_pad");
        InputManager.LastDevice = InputDevice.Keyboard;
        engine.Settings.ButtonMode = ButtonMode.Normal;
        engine.Settings.StickDeadZone = 35;
        engine.Settings.GamepadIndex = 0;
        engine.ApplySettings(window: false);
        Console.WriteLine($"the options as they were again: button mode {engine.Settings.ButtonMode}, dead zone {engine.Settings.StickDeadZone}, pad {engine.Settings.GamepadIndex + 1}, device {InputManager.LastDevice}");
        options.Open();

        // The game in the console's second slot (plan 08 · P23), the row before Controls: None unless chosen, then FireRed
        GoToRow(OptionRow.SlotTwo);
        Frames(1); Shot("look_15h_options_slot_two");
        for (int i = 0; i < 4; i++) OptionsScreen.Change(engine.Settings, OptionRow.SlotTwo, 1);
        Frames(1); Shot("look_15i_options_slot_two_firered");
        Console.WriteLine($"slot two: {engine.Settings.SlotTwo}");
        engine.Settings.SlotTwo = SlotTwoGame.None;
        options.Open();

        // The Controls page (plan 12 · Q5): the list on the keyboard, the pad's column, waiting for a key, a clash, the
        // bottom of the list, then the hints drawn by action: a rebound key named, the pad's signs
        GoToRow(OptionRow.Controls);
        Frames(1); Shot("look_16_options_controls_row");
        options.Confirm(engine.Settings);
        Frames(1); Shot("look_16b_controls");
        options.Controls.MoveColumn(1);
        Frames(1); Shot("look_16c_controls_pad_column");
        options.Controls.MoveColumn(-1);
        while (options.Controls.SelectedAction != GameAction.Confirm) options.Controls.Move(1);
        options.Controls.Confirm();
        Frames(1); Shot("look_16d_controls_waiting");
        options.Controls.Capture(new CapturedInput(KeyboardKey.Enter, null));
        Frames(1); Shot("look_16e_controls_clash");
        options.Controls.Resolve(swap: true);
        Frames(1); Shot("look_16f_controls_swapped");
        Console.WriteLine($"controls: confirm {string.Join("/", options.Controls.Working[GameAction.Confirm].Keys)}, menu {string.Join("/", options.Controls.Working[GameAction.Menu].Keys)}, clashes {options.Controls.Working.Conflicts().Count}");
        options.Controls.Move(-1);
        options.Controls.Move(-1);
        Frames(1); Shot("look_16g_controls_wrapped_to_the_end");
        while (options.Controls.SelectedAction != GameAction.Run) options.Controls.Move(-1);
        Frames(1); Shot("look_16h_controls_run");
        InputManager.LastDevice = InputDevice.Pad;
        Frames(1); Shot("look_16i_controls_on_the_pad");
        InputManager.LastDevice = InputDevice.Keyboard;
        options.Controls.Close();
        // The hints drawn by action: Confirm and Cancel rebound on the keyboard and the pad
        engine.Settings.Bindings[nameof(GameAction.Confirm)] = new SavedBinding { Keys = new[] { nameof(KeyboardKey.Q) }, Pad = new[] { nameof(GamepadButton.RightFaceUp) } };
        engine.Settings.Bindings[nameof(GameAction.Cancel)] = new SavedBinding { Keys = new[] { nameof(KeyboardKey.B) }, Pad = new[] { nameof(GamepadButton.RightFaceLeft) } };
        engine.ApplySettings(window: false);
        GoToRow(OptionRow.Help);
        Frames(1); Shot("look_16j_hints_rebound");
        InputManager.LastDevice = InputDevice.Pad;
        Frames(1); Shot("look_16k_hints_rebound_on_the_pad");
        InputManager.LastDevice = InputDevice.Keyboard;
        engine.Settings.Bindings.Clear();
        engine.ApplySettings(window: false);
        Console.WriteLine($"the bindings as they were again: {engine.Settings.Bindings.Count} overrides, device {InputManager.LastDevice}");
        options.Open();

        // The rows of plan 12 · Q3, last so that no shot before them moves: AUTOSAVE off and on, RESET TO DEFAULTS as the very last
        // row and the question it asks, and the autosave's little disc over the field (shown without a write)
        GoToRow(OptionRow.Autosave);
        Frames(1); Shot("look_17_options_autosave");
        OptionsScreen.Change(engine.Settings, OptionRow.Autosave, 1);
        Frames(1); Shot("look_17b_options_autosave_on");
        OptionsScreen.Change(engine.Settings, OptionRow.Autosave, 1);
        GoToRow(OptionRow.ResetDefaults);
        Frames(1); Shot("look_17c_options_reset_row");
        options.Confirm(engine.Settings);
        Frames(20); Shot("look_17d_options_reset_question");
        options.AnswerReset(engine.Settings, false);
        for (int i = 0; i < 20; i++) options.Animate(0.05f); // the question slides out only as the options are animated: finish it, or every later options shot keeps it
        Console.WriteLine($"the reset asked: {options.ResetAsking}; autosave {(engine.Settings.Autosave ? "on" : "off")}");
        options.Open();
        game.State = GameState.Overworld;
        game.ShowAutosaveDisc(0.5f);
        Frames(1); Shot("look_17e_autosave_disc");
        ShotCrop("look_17f_autosave_disc_close", 0, 920, 160, 160, 4);
        game.ShowAutosaveDisc(-1f);

        // The Report key (plan 16 · T20), last so that no shot before it moves: F8 over the field writes a report's folder
        // into the output folder's repros/ (the harness's game is never recorded, so it lends the engine an empty
        // recording) and the toast names it once the frame's picture is taken
        engine.Recording = new InputRecording { DataVersion = GameDataFiles.Version, GameVersion = GameVersion.Describe() };
        InputManager.Feed(ActionFrame.Press(GameAction.Report));
        Frames(1);
        InputManager.Feed(null);
        Frames(1);
        string? report = engine.LastReport;
        Console.WriteLine(report == null ? "!! no report was written" : $"the report: {ReportFolder.Shown(report)}: {string.Join(", ", Directory.GetFiles(report).Select(Path.GetFileName).OrderBy(n => n, StringComparer.Ordinal))}");
        Frames(18); Shot("look_18_report");
        engine.Recording = null;

        // Dynamic resolution (plan 16 · T23), last so that no shot before it moves: its row off and on, then the scene held
        // at each step in Twinleaf Town and at a battle's menu (the menus and text stay at the screen's own resolution)
        game.State = GameState.Options;
        options.Open();
        GoToRow(OptionRow.DynamicResolution);
        Frames(1); Shot("look_19_options_dynamic_resolution");
        OptionsScreen.Change(engine.Settings, OptionRow.DynamicResolution, 1);
        Frames(1); Shot("look_19b_options_dynamic_resolution_on");
        OptionsScreen.Change(engine.Settings, OptionRow.DynamicResolution, 1);
        game.State = GameState.Overworld;
        float? heldScale = engine.FixedResolutionStep;
        GoTo("TwinleafTown", 11, 8, Direction.Down);
        foreach (int step in ResolutionScale.Steps)
        {
            engine.FixedResolutionStep = step / 100f;
            Frames(2); Shot($"look_19c_scale_twinleaf_{step}");
        }
        var scaled = StartBattle("Shinx", 5);
        ToMainMenu(scaled);
        foreach (int step in ResolutionScale.Steps)
        {
            engine.FixedResolutionStep = step / 100f;
            Frames(1); Shot($"look_19d_scale_battle_{step}");
        }
        Console.WriteLine($"the scene's share at the last step: {game.ResolutionStep}; dynamic resolution {(engine.Settings.DynamicResolution ? "on" : "off")}");
        engine.FixedResolutionStep = heldScale;

        // The timing assist and the rumble (plan 12 · Q24), last so that no shot before them moves: each row at each value
        game.State = GameState.Options;
        options.Open();
        GoToRow(OptionRow.TimingAssist);
        Frames(1); Shot("look_20_options_timing_assist");
        OptionsScreen.Change(engine.Settings, OptionRow.TimingAssist, 2);
        Frames(1); Shot("look_20b_options_timing_assist_automatic");
        OptionsScreen.Change(engine.Settings, OptionRow.TimingAssist, 1);
        GoToRow(OptionRow.Rumble);
        OptionsScreen.Change(engine.Settings, OptionRow.Rumble, 2);
        Frames(1); Shot("look_20c_options_rumble_full");
        OptionsScreen.Change(engine.Settings, OptionRow.Rumble, 1);
        game.ApplySettings();
        Console.WriteLine($"timing assist {engine.Settings.TimingAssist}, rumble {engine.Settings.Rumble}");

        // The screenshot key (plan 12 · Q16), last of all: F12 over the field writes the 4K picture into the output
        // folder's screenshots/ (the harness's folder is the game's), with no toast in it, and the toast names it after
        game.State = GameState.Overworld;
        GoTo("TwinleafTown", 11, 8, Direction.Down);
        Frames(4);
        InputManager.Feed(ActionFrame.Press(GameAction.Screenshot));
        Frames(1);
        InputManager.Feed(null);
        Frames(1);
        string? picture = game.LastScreenshot;
        Console.WriteLine(picture == null ? "!! no screenshot was written" : $"the screenshot: {Path.GetFileName(picture)}, {new FileInfo(picture).Length} bytes");
        Frames(18); Shot("look_21_screenshot_toast");

        // Captions and holding to confirm (plan 12 · Q9, part b), last of all: Modes/LookCaptions.cs
        LookCaptionsAndHold();

        if (args.Length > 2)
            Boards(args[2], new[] { "look_1_twinleaf", "look_2_dialogue", "look_3_battle", "look_3b_moves", "look_4_party", "look_5_route201", "look_6_house", "look_7_lake" });
    }
}
