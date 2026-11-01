partial class Harness
{
    // ---------------------------------------------------------------- HOME: trades between saves and the Home box (plan 12 · Q28, Q29)

    // Not part of `all` (it continues another save, which every later shot would see). Two saves are written into the
    // out dir's slots 1 and 2 (today's game, Lucas with a Turtwig and a Bidoof; and a copy of it as Dawn, with a Kadabra),
    // then the title with HOME under the cursor (hm01), the Home screen's menu (hm02), the two sides with a Pokémon
    // chosen on each and the question (hm03, hm04), the trade made (hm05), the Home box beside slot 1 and a Bidoof put in
    // (hm06, hm07), the last able Pokémon refused (hm08), and last slot 1 continued, its arriving Kadabra evolving
    // (hm09). It prints what each file holds after each step, and takes the saves and the Home box away at the end.
    public void HomeMode()
    {
        const float Dt = 1f / 60f;
        void Seconds(float s) => Frames((int)MathF.Round(s / Dt));
        var paths = SavePaths.Current;
        string Team(int slot) => string.Join(", ", SaveSlots.Read(paths, slot).Save?.Party.Select(p => $"{p.SpeciesName} {p.Level}{(p.OriginalTrainer is { } ot ? $" (OT {ot.Name})" : "")}") ?? Array.Empty<string>());
        string Bank() => HomeMoves.ReadBank(paths).Bank is { } b ? string.Join(", ", b.Ordered.Select(p => p.Pokemon.SpeciesName)) : "can't be read";

        SavedPokemonData Made(string species, int level, int n)
        {
            var saved = SavedPokemonData.FromPokemon(new Pokemon(PokemonDatabase.Get(species)!, level));
            saved.Id = new Guid(0x51280000 + n, 0, 0, new byte[8]);
            return saved;
        }
        var lucas = game.BuildSave();
        lucas.PlayerName = "Lucas";
        lucas.Party = new() { Made("Turtwig", 18, 1), Made("Bidoof", 15, 2) };
        var dawn = SaveManager.Read(SaveManager.Write(lucas))!;
        dawn.PlayerName = "Dawn";
        dawn.Look = PlayerLook.Girl;
        dawn.TrainerId = lucas.TrainerId ^ 0x5a5a;
        dawn.Party = new() { Made("Kadabra", 30, 3), Made("Starly", 14, 4) };
        SaveSlots.Write(paths, 1, lucas);
        SaveSlots.Write(paths, 2, dawn);
        HomeBank.Save(HomeBank.Empty(), paths.HomeBox);

        // The title, HOME under the cursor
        var title = TitleScreen.WithSlots(new[] { new TitleSlot(1, lucas), new TitleSlot(2, dawn) });
        game.Title = title;
        game.State = GameState.Title;
        title.PressConfirm();
        Seconds(1.2f);
        title.PressConfirm();
        Seconds(1f);
        while (title.Entries[title.SelectedIndex] != TitleChoice.Home) title.Move(1);
        Seconds(0.3f); Shot("hm01_title_home");
        Console.WriteLine($"title: {string.Join(", ", title.Entries)}; the column ends at {title.EntryRects()[^1].Y + title.EntryRects()[^1].Height}");

        game.OpenHome();
        var home = game.Home;
        Seconds(0.5f); Shot("hm02_home_menu");

        // A trade: Lucas's Turtwig for Dawn's Kadabra
        home.Confirm();
        Seconds(0.3f);
        home.Confirm();                 // the Turtwig; the cursor goes to the right side, already on the Kadabra
        Seconds(0.3f); Shot("hm03_home_trade_sides");
        home.Confirm();                 // the Kadabra
        home.Move(1, 0);                // YES
        Seconds(0.3f); Shot("hm04_home_trade_question");
        home.Confirm();
        Frames(3);
        Seconds(0.3f); Shot("hm05_home_traded");
        Console.WriteLine($"traded: {home.Outcome}; slot 1 {Team(1)}; slot 2 {Team(2)}");
        home.Confirm();
        home.Cancel();

        // The Home box beside slot 1: a Bidoof put in
        home.Move(0, 1);
        home.Confirm();                 // HOME BOX: which save?
        Seconds(0.3f);
        home.Confirm();                 // slot 1
        Seconds(0.3f); Shot("hm06_home_box");
        home.Confirm();
        Frames(3);
        home.Confirm();
        Seconds(0.3f); Shot("hm07_home_box_deposited");
        Console.WriteLine($"put in: slot 1 {Team(1)}; the Home box {Bank()}");

        // The last able Pokémon refused
        home.Confirm();
        Seconds(0.3f); Shot("hm08_home_refused");
        Console.WriteLine($"refused: {home.Outcome}: {HomeMoves.Words(home.Outcome)}");
        home.Confirm();
        home.Close();

        // Slot 1 continued: the Kadabra that came by trade evolves
        game.SaveSlot = 1;
        engine.ContinueGame();
        for (int i = 0; i < 600 && game.State != GameState.Evolution; i++) Frames(1);
        Console.WriteLine($"continued slot 1: {game.State}, trades waiting {game.TradesWaiting}");
        Seconds(2.5f); Shot("hm09_home_traded_evolves");

        SaveSlots.Delete(paths, 1);
        SaveSlots.Delete(paths, 2);
        File.Delete(paths.HomeBox);
        File.Delete(SaveManager.BackupOf(paths.HomeBox));
    }
}
