partial class Harness
{
    // ---------------------------------------------------------------- the title's MIGRATE (plan 08 · P21)

    // Last of the title mode, so that nothing before it moves: a save with the National Pokédex, a GBA save built byte by
    // byte and written into this out dir's migrate folder (with a folder of Pokémon files and a file that is damaged), and
    // the screen through its steps. The folder is read only; the harness prints that it is as it was
    void MigrateShots(Action<float> seconds, Func<SaveData?, TitleNotice, TitleScreen> newTitle)
    {
        var noon = new DateTime(2026, 10, 21, 12, 0, 0, DateTimeKind.Utc);
        var migrate = SavePaths.Current.Migrate;
        Directory.CreateDirectory(migrate);
        int[] MovesOf(int dex) => PokemonDatabase.GetByDex(dex)!.Learnset.Select(m => MoveDatabase.Get(m.MoveName)).Where(m => m != null && m.Id is > 0 and <= 354)
            .Select(m => m!.Id).Distinct().Take(2).Concat(new[] { 0, 0, 0, 0 }).Take(4).ToArray();
        byte[] Legal(uint personality, int dex, string nickname = "", int item = 0) =>
            MigrateFixtures.Pokemon(personality, dex, Pokemon.GetExpForLevel(18 + (int)(personality % 9), PokemonDatabase.GetByDex(dex)!.GrowthRate), MovesOf(dex), nickname, item: item);

        var mons = new Dictionary<(int, int), byte[]>
        {
            [(0, 0)] = Legal(1001, 1), [(0, 1)] = Legal(1002, 4, "EMBER"), [(0, 2)] = Legal(1003, 7), [(0, 3)] = Legal(1004, 25, "VOLT"),
            [(0, 4)] = Legal(1005, 39), [(0, 5)] = Legal(1006, 52),
            // Refused, each for another reason: an Egg, a move the park won't take, a key item, a species the games don't share
            [(0, 6)] = MigrateFixtures.Pokemon(1007, 25, 0, new[] { 0, 0, 0, 0 }, egg: true),
            [(0, 7)] = MigrateFixtures.Pokemon(1008, 129, 1500, new[] { 15, 0, 0, 0 }, "CUTTER"),
            [(0, 8)] = Legal(1009, 63, "KEYS", MigrationDatabase.Data.InvalidItems[0]),
            [(0, 9)] = MigrateFixtures.Pokemon(1010, 252, 0, new[] { 0, 0, 0, 0 }),
            [(0, 10)] = Legal(1011, 133), [(0, 11)] = Legal(1012, 143, "SNORE"),
            [(1, 0)] = Legal(1013, 94), [(1, 1)] = Legal(1014, 150)
        };
        File.WriteAllBytes(Path.Combine(migrate, "emerald.sav"), MigrateFixtures.Save(mons, "TRAVEL", "BRENDAN", 0x1111_2222, 3));
        File.WriteAllBytes(Path.Combine(migrate, "damaged.sav"), new byte[0x20000]);
        var before = Directory.GetFiles(migrate).OrderBy(f => f).Select(f => (f, Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(f))))).ToList();

        var receiving = new SaveData
        {
            PlayerName = "Lucas", CurrentMapName = "SandgemTown", PlayTimeSeconds = 12 * 3600 + 34 * 60 + 20, Badges = 0b0000_0111, NationalPokedex = true,
            CaughtSpecies = Enumerable.Range(387, 14).ToList(),
            Party = new[] { ("Grotle", 24), ("Staravia", 22) }.Select(p => SavedPokemonData.FromPokemon(new Pokemon(PokemonDatabase.Get(p.Item1)!, p.Item2))).ToList()
        };

        // The title's menu with MIGRATE, the cursor on it
        var title = newTitle(receiving, null!);
        title.OfferMigration(true);
        title.PressConfirm();
        seconds(1.2f);
        title.PressConfirm();
        seconds(1f);
        title.Move(title.Entries.ToList().IndexOf(TitleChoice.Migrate) - title.SelectedIndex);
        seconds(0.2f); Shot("title_17_migrate_entry");
        Console.WriteLine($"the menu: {string.Join(", ", title.Entries)}");

        // The files: the save and the damaged one (and what a file that has to wait looks like, further down)
        var targets = new[] { new MigrateTarget(1, receiving) };
        var screen = game.Migrate;
        game.MigrationClock = () => noon;
        screen.Open(targets, MigrationFolder.Read(migrate), () => noon);
        game.State = GameState.Migrate;
        seconds(0.5f); Shot("title_17b_migrate_files");
        Console.WriteLine($"the files: {string.Join("; ", screen.Sources.Select(s => $"{s.Name} ({s.Kind}, {(s.Readable ? screen.BlockOf(s).ToString() : s.Refusal.ToString())})"))}");

        // The first box: three chosen, then the cursor on each of the four that can't come (an Egg, a move the park won't
        // take, a key item, a species the games don't share), then six chosen
        void Go(int place) => screen.Move(place % MigrateScreen.Columns - screen.Place % MigrateScreen.Columns, place / MigrateScreen.Columns - screen.Place / MigrateScreen.Columns);
        void Told()
        {
            Console.WriteLine($"under the cursor: {(screen.Under?.IsEgg == true ? "an Egg" : screen.Under?.SpeciesData?.Name)} → {(screen.VerdictOf(screen.Under!).Allowed ? "can come" : Migration.Words(screen.VerdictOf(screen.Under!)))}");
        }
        screen.Confirm();
        // The icons are baked from the models the first time a screen asks for them, a few a frame
        seconds(12f);
        for (int i = 0; i < 3; i++) { Go(i); screen.Confirm(); }
        Go(6);
        seconds(0.3f); Shot("title_17c_migrate_box"); Told();
        Go(7);
        seconds(0.2f); Shot("title_17c2_migrate_box_a_move"); Told();
        Go(8);
        seconds(0.2f); Shot("title_17c3_migrate_box_a_key_item"); Told();
        Go(9);
        seconds(0.2f); Shot("title_17c4_migrate_box_no_such_species"); Told();

        // Six chosen, and the question
        for (int i = 3; i < 6; i++) { Go(i); screen.Confirm(); }
        seconds(0.3f); Shot("title_17c5_migrate_six_chosen");
        Console.WriteLine($"chosen: {screen.ChosenCount} ({string.Join(", ", screen.Six.Select(p => p.SpeciesData?.Name))})");
        screen.Done();
        seconds(0.3f); Shot("title_17d_migrate_confirm");
        screen.Move(1, 0);
        seconds(0.2f); Shot("title_17d2_migrate_confirm_yes");

        // Carried out as the engine carries it out (on the save in hand; the harness's slots are not the title's)
        screen.Confirm();
        var request = screen.TakeRequest()!;
        var block = Migration.Bring(receiving, request.Source.Save!, request.Six, noon, Ruleset.Modern);
        screen.Finish(block);
        seconds(0.3f); Shot("title_17e_migrate_done");
        Console.WriteLine($"brought: {block}; waiting in the park: {receiving.Pastimes?.Waiting?.Count}; sources remembered: {receiving.Pastimes?.Sources?.Count}; came: {receiving.Pastimes?.Came?.Count}");

        // The same file again, three hours on: it waits, and its row says so
        screen.Confirm();
        screen.Open(targets, MigrationFolder.Read(migrate), () => noon.AddHours(3));
        seconds(0.5f); Shot("title_17f_migrate_waits");
        Console.WriteLine($"the file again: {screen.BlockOf(screen.Sources[0])} — {screen.BlockWords(screen.Sources[0])}");

        var after = Directory.GetFiles(migrate).OrderBy(f => f).Select(f => (f, Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(f))))).ToList();
        Console.WriteLine($"the migrate folder is as it was: {before.SequenceEqual(after)}");
        screen.Close();
        MigrateBetweenSavesShots(seconds, migrate, noon);
    }

    // ---------------------------------------------------------------- between this game's saves, and the way out (plan 08 · P22)

    // After P21's, so that nothing before moves: the screen as the title opens it (what to do), the other save among the files, its
    // boxes, and the way out: a save's team and boxes, a format, and the notice. The files go into this out dir's migrate/out
    void MigrateBetweenSavesShots(Action<float> seconds, string migrate, DateTime noon)
    {
        var owner = new SaveData
        {
            PlayerName = "Lucas", CurrentMapName = "SandgemTown", PlayTimeSeconds = 20 * 3600, Badges = 0b0000_1111, NationalPokedex = true,
            Party = new[] { ("Grotle", 24), ("Staravia", 22) }.Select(p => SavedPokemonData.FromPokemon(
                new Pokemon(PokemonDatabase.Get(p.Item1)!, p.Item2, 2000u + (uint)p.Item2) { Id = new Guid(p.Item2, 0, 0, new byte[8]) })).ToList(),
            Boxes = new SavedBoxes { Boxes = { new SavedBox { Name = "BOX 1" } } }
        };
        int[] dexes = { 1, 4, 7, 25, 39, 52, 94, 133, 700 };
        for (int i = 0; i < dexes.Length; i++)
        {
            var mon = new Pokemon(PokemonDatabase.GetByDex(dexes[i])!, 18 + i, 3000u + (uint)i, new Random(i)) { Id = new Guid(100 + i, 0, 0, new byte[8]) };
            owner.Boxes!.Boxes[0].Pokemon.Add(new SavedBoxedPokemon { Slot = i, Pokemon = SavedPokemonData.FromPokemon(mon) });
        }
        var other = new SaveData { PlayerName = "Dawn", CurrentMapName = "JubilifeCity", PlayTimeSeconds = 3600, NationalPokedex = true };
        var saves = new[] { (1, owner), (2, other) };
        var targets = saves.Select(s => new MigrateTarget(s.Item1, s.Item2)).ToList();
        var sources = MigrationFolder.Read(migrate);
        sources.AddRange(MigrationFolder.Slots(saves));
        var screen = game.Migrate;
        void Go(int place) => screen.Move(place % MigrateScreen.Columns - screen.Place % MigrateScreen.Columns, place / MigrateScreen.Columns - screen.Place / MigrateScreen.Columns);

        // What to do, then the other save among the files, and its boxes
        screen.Open(targets, sources, () => noon.AddDays(2), withMenu: true);
        game.State = GameState.Migrate;
        seconds(0.5f); Shot("title_17g_migrate_menu");
        screen.Confirm();
        screen.Move(0, 1);
        screen.Confirm();
        seconds(0.4f);
        int slot = screen.Sources.ToList().FindIndex(s => s.Slot != null);
        screen.Move(0, slot);
        seconds(0.3f); Shot("title_17h_migrate_from_slot");
        Console.WriteLine($"into slot 2, the files: {string.Join("; ", screen.Sources.Select(s => $"{s.Name} ({s.Kind}, {(s.Readable ? screen.BlockOf(s).ToString() : s.Refusal.ToString())})"))}");
        screen.Confirm();
        seconds(8f);
        for (int i = 0; i < 3; i++) { Go(i); screen.Confirm(); }
        Go(5);
        seconds(0.3f); Shot("title_17i_migrate_slot_box");
        screen.Close();

        // The way out
        screen.Open(targets, sources, () => noon.AddDays(2), withMenu: true);
        game.State = GameState.Migrate;
        screen.Move(0, 1);
        screen.Confirm();
        screen.Confirm();
        seconds(2f); Shot("title_17j_write_out_team");
        screen.ChangeBox(1);
        seconds(6f);
        Go(3);
        seconds(0.3f); Shot("title_17k_write_out_box");
        Go(8);
        seconds(0.3f); Shot("title_17l_write_out_text_only");
        Go(3);
        screen.Confirm();
        seconds(0.3f); Shot("title_17m_write_out_format");
        screen.Confirm();
        var request = screen.TakeWriteOut()!;
        string? file = MigrationOut.Write(migrate, request.From.Save, request.Pokemon, request.Format);
        screen.FinishWriteOut(file);
        seconds(0.3f); Shot("title_17n_write_out_done");
        Console.WriteLine($"written out: {file}; in the out folder: {string.Join(", ", Directory.GetFiles(Path.Combine(migrate, "out")).Select(Path.GetFileName))}");
        screen.Close();
    }

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
        // (Since plan 13 · V1 the ways to play come next; Confirm on Normal leaves at once, so this shot is what it was)
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

        MigrateShots(Seconds, NewTitle);

        // Last of the title mode, so that nothing before it moves (plan 13 · V1): the ways to play that NEW GAME asks next
        var fresh = NewTitle(null);
        fresh.PressConfirm();
        Seconds(1.2f);
        fresh.PressConfirm();
        Seconds(1f);
        fresh.PressConfirm();
        Seconds(0.5f); Shot("title_43_run_preset");
        Console.WriteLine($"the ways to play: phase {fresh.Phase}, rows {fresh.Run.Rows.Count}, on {fresh.Run.SelectedRow}, preset {fresh.Run.Rules.PresetKey}, normal {fresh.Run.Rules.IsNormal}");
        fresh.Change(1);
        Seconds(0.2f); Shot("title_44_run_nuzlocke");
        Console.WriteLine($"one step right: preset {fresh.Run.Rules.PresetKey}, rows {fresh.Run.Rows.Count}");
        while (fresh.Run.SelectedRow != PokemonPlatinumEngine.UI.Kit.RunRow.NoRunning) fresh.Move(1);
        fresh.Change(1);
        Seconds(0.2f); Shot("title_45_run_flag");
        Console.WriteLine($"a flag on: preset {fresh.Run.Rules.PresetKey}, on {fresh.Run.SelectedRow}, window from {fresh.Run.First}");
        while (fresh.Run.SelectedRow != PokemonPlatinumEngine.UI.Kit.RunRow.Start) fresh.Move(1);
        Seconds(0.2f); Shot("title_46_run_start");
        fresh.PressConfirm();
        Seconds(0.2f);
        Console.WriteLine($"START: phase {fresh.Phase}, chosen {fresh.ChosenRun.PresetKey}, no running {fresh.ChosenRun.NoRunning}");

        // A game saved with one of them is marked on its card and its summary
        save.Run = RunRules.NuzlockePreset;
        var marked = NewTitle(save);
        marked.PressConfirm();
        Seconds(1.2f);
        marked.PressConfirm();
        Seconds(1f);
        Shot("title_47_run_marked_card");
        Console.WriteLine($"the card of a game played as a Nuzlocke: mark \"{ModernUi.RunMark(save.Run)}\"");
        ShotCrop("title_47b_run_marked_native", 1016, (int)marked.EntryRects()[0].Y, 824, 452, 2);
        save.Run = null;

        FirstStartShots(Seconds);

        game.State = GameState.Overworld;
    }

    /// <summary>
    /// The first start (plan 16 · T24): the page a first run opens on, asked for here since the harness's options are no
    /// first run, then with a row changed and with the cursor on CONTINUE. The options are put back as they were and
    /// nothing is written: the page's CONTINUE is not pressed.
    /// </summary>
    void FirstStartShots(Action<float> seconds)
    {
        var settings = game.Settings;
        string language = settings.Language;
        var buttons = settings.ButtonMode;
        game.OpenFirstStart();
        var page = game.FirstStart;
        seconds(0.5f); Shot("title_0_first_start");
        Console.WriteLine($"first start: rows {string.Join(", ", page.Rows)}");

        // The language moved on: the page is in it at once
        if (page.Change(settings, 1)) game.ApplyFirstStartChange();
        seconds(0.2f); Shot("title_0b_first_start_language");
        Console.WriteLine($"first start: language {settings.Language}, the page's title \"{Text.Of("firstStart.title")}\"");
        page.Change(settings, -1);
        game.ApplyFirstStartChange();

        // The button preset changed
        while (page.Selected is { } row && row != OptionRow.ButtonMode) page.Move(1);
        if (page.Change(settings, 1)) game.ApplyFirstStartChange();
        seconds(0.2f); Shot("title_0b_first_start_buttonmode");
        Console.WriteLine($"first start: button mode {settings.ButtonMode}");

        // CONTINUE under the cursor, with what it does in the panel
        while (!page.OnContinue) page.Move(1);
        seconds(0.2f); Shot("title_0c_first_start_continue");

        settings.Language = language;
        settings.ButtonMode = buttons;
        game.ApplyFirstStartChange();
        page.Close();
        game.State = GameState.Title;
    }
}
