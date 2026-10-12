partial class Harness
{
    // ---------------------------------------------------------------- title screen
    public void TitleMode()
    {
        const float Dt = 1f / 60f;
        TitleScreen NewTitle(SaveData? save, TitleNotice notice = null)
        {
            var title = new TitleScreen(save, notice);
            game.Title = title;
            game.State = GameState.Title;
            return title;
        }
        void Seconds(float s) => Frames((int)MathF.Round(s / Dt));

        // The opening from the start, with no saved game: notice, three fly-over shots, Giratina, the title, the menu
        var opening = NewTitle(null);
        Seconds(1.6f); Shot("title_1_notice");
        Seconds(TitleScreen.NoticeTime - 1.6f + 1.9f); Shot("title_2_journey_twinleaf");
        Seconds(TitleScreen.SegmentTime); Shot("title_3_journey_route201");
        Seconds(TitleScreen.SegmentTime); Shot("title_4_journey_sandgem");
        Seconds(TitleScreen.SegmentTime - 1.9f + 1.0f); Shot("title_5_reveal_silhouette");
        Seconds(0.9f); Shot("title_6_reveal_lit");
        Seconds(2.2f); Shot("title_7_idle");
        Timing("title");
        opening.PressConfirm();
        Seconds(1f); Shot("title_8_menu_no_save");

        // With a saved game: three badges, twelve and a half hours, a party of five
        var save = new SaveData
        {
            PlayerName = "Lucas",
            CurrentMapName = "SandgemTown",
            PlayTimeSeconds = 12 * 3600 + 34 * 60 + 20,
            Badges = 0b0000_0111,
            Money = 12480,
            CaughtSpecies = Enumerable.Range(387, 14).ToList(),
            Party = new[] { ("Grotle", 24), ("Staravia", 22), ("Luxio", 23), ("Bibarel", 20), ("Riolu", 18) }
                .Select(p => SavedPokemonData.FromPokemon(new Pokemon(PokemonDatabase.Get(p.Item1)!, p.Item2))).ToList()
        };
        var saved = NewTitle(save);
        saved.PressConfirm();
        Seconds(1.2f);
        saved.PressConfirm();
        Seconds(1f); Shot("title_9_menu_continue");
        ShotCrop("title_9b_continue_native", 1016, 250, 824, 452, 2);
        saved.Move(1);
        Seconds(0.2f); Shot("title_10_menu_new_game");
        saved.PressConfirm();
        Seconds(0.2f); Shot("title_11_confirm_new_game");

        // "Yes" leads to the last question: whose rules. Platinum's are offered; the other answer has its own words
        saved.Move(1);
        saved.PressConfirm();
        Seconds(0.2f); Shot("title_12_rules_platinum");
        saved.Move(1);
        Seconds(0.2f); Shot("title_13_rules_modern");

        // A game played by the modern rules says so on its panel
        save.Rules = RulesPreset.Modern;
        var modern = NewTitle(save);
        modern.PressConfirm();
        Seconds(1.2f);
        modern.PressConfirm();
        Seconds(1f); Shot("title_14_continue_modern_rules");

        // A save that couldn't be read: the one before it was loaded, and the title says so before its menu (plan 12 · Q1)
        var recovered = NewTitle(save, TitleNotice.ForSave(SaveLoad.Recovered));
        recovered.PressConfirm();
        Seconds(1.2f);
        recovered.PressConfirm();
        Seconds(0.5f); Shot("title_15_notice_recovered");
        Console.WriteLine($"title notice: {recovered.Notice?.Heading}, phase {recovered.Phase}");
        recovered.PressConfirm();
        Seconds(1f);
        Console.WriteLine($"after the notice: phase {recovered.Phase}, entries {string.Join(", ", recovered.Entries)}");

        // The attract loop (plan 09 · L4), last so that nothing before it moves: the title left alone for its half
        // minute plays the opening again, without the notice; a button there comes back to the title, and on to the menu
        var alone = NewTitle(null);
        alone.PressConfirm();
        Seconds(TitleScreen.IdleTime - 0.5f); Shot("title_16_idle_before_the_loop");
        Console.WriteLine($"left alone for {TitleScreen.IdleTime - 0.5f:0.0} s: phase {alone.Phase}");
        Seconds(0.5f + 1.9f); Shot("title_17_loop_journey_twinleaf");
        Console.WriteLine($"... and for {TitleScreen.IdleTime + 1.9f:0.0} s: phase {alone.Phase}");
        Seconds(TitleScreen.SegmentTime * 2f); Shot("title_18_loop_journey_sandgem");
        Seconds(TitleScreen.SegmentTime - 1.9f + 1.9f); Shot("title_19_loop_reveal");
        Console.WriteLine($"the opening played again: phase {alone.Phase}");
        Seconds(TitleScreen.RevealTime + TitleScreen.IdleTime - 1.9f + 2.5f);
        Console.WriteLine($"left alone again: phase {alone.Phase}");
        alone.PressConfirm();
        Seconds(0.1f); Shot("title_20_loop_skipped");
        Console.WriteLine($"a button in it: phase {alone.Phase}");
        alone.PressConfirm();
        Seconds(1f);
        Console.WriteLine($"... and another: phase {alone.Phase}, entries {string.Join(", ", alone.Entries)}");

        // The start after a crash (plan 16 · T3), after everything else so that nothing before it moves: the title says
        // once that the game stopped, that the save is intact, where the report is and that the game as it stood was
        // kept; and, for a game that had never been saved and stopped before one began, the short of it. The notes
        // are made up here: the `crash` mode leaves a real one and shoots the title that takes it up
        var when = new DateTime(2026, 6, 1, 12, 0, 0);
        var crashed = NewTitle(save, TitleNotice.ForCrash(new CrashNote($"{CrashReport.FolderName}/{CrashReport.FileNameAt(when)}", true, when), SaveLoad.Ok));
        crashed.PressConfirm();
        Seconds(1.2f);
        crashed.PressConfirm();
        Seconds(0.5f); Shot("title_21_notice_crashed");
        Console.WriteLine($"after a crash: \"{crashed.Notice?.Heading}\": {crashed.Notice?.Body}");
        crashed.PressConfirm();
        Seconds(1f);
        Console.WriteLine($"after the notice: phase {crashed.Phase}, entries {string.Join(", ", crashed.Entries)}");

        var early = new TitleScreen(null, TitleNotice.ForCrash(new CrashNote($"{CrashReport.FolderName}/{CrashReport.FileNameAt(when)}", false, when), SaveLoad.None),
            TitleNotice.ForSave(SaveLoad.None));
        game.Title = early;
        game.State = GameState.Title;
        early.PressConfirm();
        Seconds(1.2f);
        early.PressConfirm();
        Seconds(0.5f); Shot("title_22_notice_crashed_no_save");
        Console.WriteLine($"after a crash with nothing saved: {early.Notice?.Body}");

        // And with a save that couldn't be read either: the crash's notice first, then the save's own
        var both = new TitleScreen(save, TitleNotice.ForCrash(new CrashNote($"{CrashReport.FolderName}/{CrashReport.FileNameAt(when)}", true, when), SaveLoad.Recovered),
            TitleNotice.ForSave(SaveLoad.Recovered));
        game.Title = both;
        both.PressConfirm();
        Seconds(1.2f);
        both.PressConfirm();
        Seconds(0.5f); Shot("title_23_notice_crashed_then_recovered");
        Console.WriteLine($"after a crash and a damaged save: first \"{both.Notice?.Heading}\": {both.Notice?.Body}");
        both.PressConfirm();
        Seconds(0.5f);
        Console.WriteLine($"... then \"{both.Notice?.Heading}\", phase {both.Phase}");
        both.PressConfirm();
        Seconds(0.5f);
        Console.WriteLine($"... then phase {both.Phase}");

        game.State = GameState.Overworld;
    }
}
