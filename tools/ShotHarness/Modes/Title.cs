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
        // (the lone save's card, wherever the column puts it: plan 12 · Q2 moved it up for the row under NEW GAME)
        ShotCrop("title_9b_continue_native", 1016, (int)saved.EntryRects()[0].Y, 824, 452, 2);
        saved.Move(1);
        Seconds(0.2f); Shot("title_10_menu_new_game");
        saved.PressConfirm();
        Seconds(0.2f); Shot("title_11_confirm_new_game");

        // "Yes" begins the new game: the question of whose rules that followed it is gone (plan 06 · R19), and with it
        // title_12 and title_13 and the card that said "modern rules" (title_14). The time they took is still spent,
        // so that nothing after them moves
        saved.Move(1);
        saved.PressConfirm();
        Seconds(0.2f); Shot("title_12_new_game_leaving");
        Seconds(0.2f);
        var again = NewTitle(save);
        again.PressConfirm();
        Seconds(1.2f);
        again.PressConfirm();
        Seconds(1f);

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

        // The slots (plan 12 · Q2), after everything else so that nothing before it moves. A game is saved in one of
        // three, and the menu has a card for each that holds a save. The saves are made up here, as the one above is:
        // no file stands behind them, so the slot the mode deletes is one whose files were never there
        SaveData Game(string name, PlayerLook look, int x, int y, float hours, int badges, int caught, params (string, int)[] team) => new()
        {
            PlayerName = name,
            Look = look,
            CurrentMapName = "Sinnoh",
            PlayerGridX = x,
            PlayerGridY = y,
            WorldVersion = SaveData.CurrentWorld,
            PlayTimeSeconds = hours * 3600f,
            Badges = (1 << badges) - 1,
            CaughtSpecies = Enumerable.Range(387, caught).ToList(),
            Party = team.Select(p => SavedPokemonData.FromPokemon(new Pokemon(PokemonDatabase.Get(p.Item1)!, p.Item2))).ToList()
        };
        var second = Game("Dawn", PlayerLook.Girl, 305, 531, 31.75f, 2, 22, ("Monferno", 21), ("Buizel", 19));
        var third = Game("Barry", PlayerLook.Boy, 860, 785, 96.2f, 8, 151, ("Empoleon", 58), ("Staraptor", 56), ("Roserade", 55));
        TitleScreen Slots(params TitleSlot[] taken)
        {
            var title = TitleScreen.WithSlots(taken);
            game.Title = title;
            game.State = GameState.Title;
            title.PressConfirm();
            Seconds(1.2f);
            title.PressConfirm();
            Seconds(1f);
            return title;
        }
        string Told(TitleScreen title) =>
            $"phase {title.Phase}, the cursor on {title.Entries[title.SelectedIndex]} (entry {title.SelectedIndex}), cards for slots {string.Join(", ", title.Cards.Select(c => c.Save != null ? $"{c.Number} ({c.Save.PlayerName})" : $"{c.Number} (can't be read)"))}; "
            + $"a new game would take slot {title.FreeSlot?.ToString() ?? "none"}";

        // Two saves: two short cards, and NEW GAME says which slot it takes
        var two = Slots(new TitleSlot(1, save), new TitleSlot(2, second));
        Shot("title_24_slots_two");
        Console.WriteLine($"two saves: {Told(two)}; the column from {two.EntryRects()[0].Y:0} to {two.EntryRects()[^1].Y + two.EntryRects()[^1].Height:0}");
        two.Move(2);
        two.PressConfirm();
        Seconds(0.2f); Shot("title_25_slots_new_game_asks");
        Console.WriteLine($"NEW GAME with two saves: phase {two.Phase}; \"{TitleScreen.NewGameBlurb(two.FreeSlot ?? 0, two.Cards.Count)}\"");
        two.PressCancel();

        // Three saves, the cursor on each in turn; a card continues its own slot's game
        var three = Slots(new TitleSlot(1, save), new TitleSlot(2, second), new TitleSlot(3, third));
        Shot("title_26_slots_three_first");
        Console.WriteLine($"three saves: {Told(three)}; the column from {three.EntryRects()[0].Y:0} to {three.EntryRects()[^1].Y + three.EntryRects()[^1].Height:0}");
        ShotCrop("title_26b_slots_card_native", 1016, (int)three.EntryRects()[0].Y - 8, 824, 2 * 144 + 18 + 16, 2);
        three.Move(1);
        Seconds(0.2f); Shot("title_27_slots_three_second");
        three.Move(1);
        Seconds(0.2f); Shot("title_28_slots_three_third");
        Console.WriteLine($"the cursor on the third card: {Told(three)}");

        // NEW GAME with every slot taken starts nothing: a notice says a new game needs a slot of its own
        three.Move(1);
        three.PressConfirm();
        Seconds(0.5f); Shot("title_29_slots_no_free_slot");
        Console.WriteLine($"NEW GAME with every slot taken: phase {three.Phase}, \"{three.Notice?.Heading}\": {three.Notice?.Body}");
        three.PressConfirm();
        Seconds(0.5f);
        Console.WriteLine($"... and after it: {Told(three)}");

        // DELETE A SAVE: which, with the cursor on the cards alone; the question that names the slot; and the slot gone
        three.Move(1);
        three.PressConfirm();
        Seconds(0.2f); Shot("title_30_slots_delete_which");
        Console.WriteLine($"DELETE A SAVE: {Told(three)}");
        three.Move(1);
        Seconds(0.2f); Shot("title_31_slots_delete_which_second");
        three.PressConfirm();
        Seconds(0.2f); Shot("title_32_slots_delete_asks");
        Console.WriteLine($"the question: phase {three.Phase}, \"{TitleScreen.DeleteHeading(three.DeletingSlot)}\", yes highlighted: {three.ConfirmYes}");
        three.Move(1);
        Seconds(0.2f); Shot("title_33_slots_delete_yes");
        three.PressConfirm();
        Seconds(0.5f); Shot("title_34_slots_deleted");
        Console.WriteLine($"after YES: {Told(three)}");

        // A slot that can't be read is a card too, which can only be deleted, and its notice names it
        var broken = TitleScreen.WithSlots(new[] { new TitleSlot(1, save), new TitleSlot(2, null), new TitleSlot(3, third) },
            TitleNotice.ForSlot(2, SaveLoad.Damaged), TitleNotice.ForSlot(3, SaveLoad.Recovered));
        game.Title = broken;
        broken.PressConfirm();
        Seconds(1.2f);
        broken.PressConfirm();
        Seconds(0.5f); Shot("title_35_slots_notice_cant_be_read");
        Console.WriteLine($"a slot that can't be read: \"{broken.Notice?.Heading}\": {broken.Notice?.Body}");
        broken.PressConfirm();
        Seconds(0.5f); Shot("title_36_slots_notice_last_save_damaged");
        Console.WriteLine($"... then \"{broken.Notice?.Heading}\": {broken.Notice?.Body}");
        broken.PressConfirm();
        Seconds(1f);
        broken.Move(1);
        Seconds(0.2f); Shot("title_37_slots_unreadable_card");
        Console.WriteLine($"its card: {Told(broken)}");
        broken.PressConfirm();
        Seconds(0.2f); Shot("title_38_slots_unreadable_delete_asks");
        Console.WriteLine($"a press on it: phase {broken.Phase}, slot {broken.DeletingSlot}");
        broken.PressCancel();

        // One save alone keeps the whole card whichever slot it is in, with DELETE A SAVE under NEW GAME
        var lone = Slots(new TitleSlot(3, third));
        Shot("title_39_slots_lone_in_slot_3");
        Console.WriteLine($"one save, in slot 3: {Told(lone)}");
        ShotCrop("title_39b_slots_lone_native", 1016, (int)lone.EntryRects()[0].Y - 8, 824, 468, 2);
        lone.Move(2);
        lone.PressConfirm();
        Seconds(0.2f); Shot("title_40_slots_lone_delete_asks");
        Console.WriteLine($"DELETE A SAVE with one save: phase {lone.Phase}, slot {lone.DeletingSlot}");
        lone.Move(1);
        lone.PressConfirm();
        Seconds(0.5f); Shot("title_41_slots_none_left");
        Console.WriteLine($"and with it deleted: {Told(lone)}");

        // After a crash, with several saves that all read whole
        var several = TitleScreen.WithSlots(new[] { new TitleSlot(1, save), new TitleSlot(2, second) },
            TitleNotice.ForCrash(new CrashNote($"{CrashReport.FolderName}/{CrashReport.FileNameAt(when)}", true, when), new[] { SaveLoad.Ok, SaveLoad.Ok, SaveLoad.None }));
        game.Title = several;
        several.PressConfirm();
        Seconds(1.2f);
        several.PressConfirm();
        Seconds(0.5f); Shot("title_42_slots_notice_crashed");
        Console.WriteLine($"after a crash with two saves: {several.Notice?.Body}");

        game.State = GameState.Overworld;
    }
}
