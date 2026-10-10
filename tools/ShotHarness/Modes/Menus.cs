partial class Harness
{
    // ---------------------------------------------------------------- menus
    public void MenusMode()
    {
        // Start menu: sliding in, open, and the prompt behind QUIT GAME
        GoTo("TwinleafTown", 11, 8, Direction.Down);
        var startMenu = game.StartMenu;
        startMenu.Open();
        Frames(4); Shot("09a_startmenu_sliding");
        Frames(30); Shot("09_startmenu");
        startMenu.Move(-1);
        Frames(2); Shot("09b_startmenu_quit");
        startMenu.Confirm();
        Frames(30); Shot("09c_quit_prompt");
        startMenu.Cancel();
        startMenu.Hide();

        // The sign on arriving somewhere, and a notice
        var sign = game.LocationSign;
        sign.Show("Twinleaf Town");
        engine.ShowNotification("Game saved.");
        Frames(40); Shot("10_sign_and_notice");
        ShotCrop("10b_sign_native", 30, 20, 700, 150, 2);
        GoTo("Route201", 14, 10, Direction.Left);
        sign.Show("Route 201");
        Frames(40); Shot("10c_sign_route");
        sign.Hide();
        Frames(200);

        // Pokémon menu and summary, with a poisoned and a fainted Pokémon to show the status pills
        party.Members[1].Status = StatusCondition.Poison;
        party.Members[1].CurrentHP = party.Members[1].MaxHP / 3;
        party.Members[2].CurrentHP = 0;
        var partyScreen = game.PartyScreen;
        game.State = GameState.PartyMenu;
        partyScreen.Open();
        Frames(6); Shot("20a_party_opening");
        Frames(40); Shot("20_party");
        partyScreen.ShowSummary = true;
        Frames(30); Shot("20b_summary");
        partyScreen.MoveCursor(0, 1, party.Count);
        Frames(30); Shot("20c_summary_second");
        // Pokérus (plan 06 · R10): the PKRS tag after the status, on the summary and on the party's card
        party.Members[1].Pokerus = 0x34;
        Frames(4); Shot("20d_summary_pokerus");
        partyScreen.ShowSummary = false;
        Frames(30); Shot("20e_party_pokerus");
        party.Members[1].Pokerus = 0;
        partyScreen.Close();

        game.State = GameState.StarterSelect;
        var starters = game.StarterSelect;
        starters.Open();
        Frames(8); Shot("21a_starter_opening");
        starters.Move(1);
        Frames(40); Shot("21_starter");
        starters.Confirm();
        Frames(30); Shot("21b_starter_asking");
        starters.Cancel();
        starters.Close();
        foreach (var (item, count) in new[]
                 {
                     ("Potion", 5), ("Super Potion", 2), ("Antidote", 3), ("Revive", 1), ("Rare Candy", 2), ("Fire Stone", 1), ("Poké Ball", 10),
                     ("Great Ball", 3), ("Oran Berry", 4), ("Escape Rope", 2), ("Old Rod", 1), ("Town Map", 1), ("TM01", 1), ("Repel", 3),
                    ("Ether", 2), ("TM76", 1), ("HM06", 1)
                 })
            if (ItemDatabase.Get(item) is { } data) inventory.AddItem(data, count);
        game.State = GameState.BagMenu;
        var bag = game.BagScreen;
        bag.Open();
        Frames(6); Shot("22a_bag_opening");
        bag.MovePocket(1);
        bag.MoveCursor(1, 4);
        Frames(30); Shot("22_bag");
        bag.Confirm(inventory, party, engine.ShowNotification);
        Frames(4); Shot("22b_bag_actions");
        bag.Confirm(inventory, party, engine.ShowNotification);
        Frames(30); Shot("22c_bag_use_on");
        bag.CancelTarget();

        // The items of plan 06 · R11: TOSS and its "how many?", an Ether's choice of move, a TM's ABLE and NOT ABLE
        void PickItem(string name)
        {
            var data = ItemDatabase.Get(name)!;
            bag.CurrentPocket = data.Pocket;
            bag.SelectedIndex = inventory.GetPocketItems(data.Pocket).FindIndex(st => st.Data.Name == name);
        }
        void ChooseAction(BagAction action)
        {
            bag.Confirm(inventory, party, engine.ShowNotification);
            while (bag.Actions != null && bag.Actions[bag.ActionIndex] != action) bag.MoveCursor(1, bag.Actions.Count);
            bag.Confirm(inventory, party, engine.ShowNotification);
        }
        PickItem("Potion");
        ChooseAction(BagAction.Toss);
        bag.MoveToss(1, 0, inventory.GetQuantity(ItemDatabase.Get("Potion")!));
        bag.MoveToss(1, 0, inventory.GetQuantity(ItemDatabase.Get("Potion")!));
        Frames(20); Shot("22g_bag_toss");
        bag.Cancel();
        party.Members[0].Moves[0].CurrentPP = 3;
        PickItem("Ether");
        ChooseAction(BagAction.Use);
        bag.UseOnTarget(inventory, party, engine.ShowNotification, new EvolutionContext { Party = party, Bag = inventory });
        Frames(20); Shot("22h_bag_ether_move");
        bag.CancelMove();
        bag.CancelTarget();
        PickItem("TM76");
        ChooseAction(BagAction.Use);
        Frames(30); Shot("22i_bag_tm_able");
        bag.CancelTarget();
        PickItem("TM01");
        Frames(4); Shot("22d_bag_tm");
        bag.MovePocket(4);
        Frames(4); Shot("22e_bag_key_items");
        bag.MovePocket(-7);
        Frames(4); Shot("22f_bag_items");
        bag.Close();

        // The Trainer Card, opened as the start menu opens it (so it has the player's portrait), with two badges won
        game.State = GameState.Overworld;
        game.Story.SetBadges(0b11);
        game.ChooseFromStartMenu(StartMenuChoice.Trainer);
        Frames(40); Shot("28_trainer_card");
        // Turned over (plan 06 · R12): halfway, then its back
        var trainerCard = game.TrainerCard;
        trainerCard.Flip();
        Frames(6); Shot("28a_trainer_card_turning");
        Frames(30); Shot("28b_trainer_card_back");
        trainerCard.Close();
        // Another colour: a team in the Hall of Fame is the first star (cobalt), with a score and a debut on the back
        var scoreBefore = game.TrainerScore;
        var fameBefore = game.HallOfFame;
        var fame = new HallOfFame();
        fame.Enter(party, p => ("Lucas", 12345), new DateTime(2026, 10, 7, 14, 32, 0));
        game.HallOfFame = fame;
        game.TrainerScore = 2468;
        game.ChooseFromStartMenu(StartMenuChoice.Trainer);
        Frames(40); Shot("28c_trainer_card_cobalt");
        trainerCard.Flip();
        Frames(30); Shot("28d_trainer_card_cobalt_back");
        trainerCard.Close();
        game.TrainerScore = scoreBefore;
        game.Story.SetBadges(0);

        // Saving: the question over the field, and the moment after
        game.State = GameState.Overworld;
        game.ChooseFromStartMenu(StartMenuChoice.Save);
        var saving = game.SaveScreen;
        Frames(30); Shot("31_save_asking");
        saving.Confirm();
        Frames(12); Shot("31b_saved");
        Frames(120);
        Console.WriteLine($"after saving: state {game.State}, a save was written: {File.Exists("savegame.json")}");
        File.Delete("savegame.json");
        game.State = GameState.Overworld;
        game.State = GameState.Shop;
        var shop = game.Shop;
        // The counter's first question, the common stock with three badges, "how many?", then the bag's items to sell
        shop.Open("Oreburgh Poké Mart", MartDatabase.Stock(null, 3));
        Frames(30); Shot("29a_shop_question");
        shop.Confirm(inventory, 3000, engine.ShowNotification);
        shop.Move(0, 1, 3000); shop.Move(0, 1, 3000);
        Frames(12); Shot("29_shop");
        shop.Confirm(inventory, 3000, engine.ShowNotification);
        shop.Move(1, 0, 3000); shop.Move(1, 0, 3000);
        Frames(30); Shot("29b_shop_how_many");
        shop.Cancel();
        shop.Cancel();
        shop.Move(0, 1, 3000);
        shop.Confirm(inventory, 3000, engine.ShowNotification);
        shop.Move(0, 1, 3000, inventory: inventory);
        shop.Confirm(inventory, 3000, engine.ShowNotification);
        Frames(30); Shot("29c_shop_sell");
        shop.Close();
        // A counter of a town's own goods
        shop.Open("Veilstone Dept. Store", MartDatabase.Stock("veilstone_2f_mid", 0), ShopMode.Buying);
        Frames(30); Shot("29d_shop_specialties");
        shop.Close();
        var boxed = game.Boxes;
        foreach (var name in new[] { "Starly", "Bidoof", "Shinx", "Budew", "Kricketot", "Staravia", "Luxio", "Riolu", "Gible", "Prinplup" })
            boxed.Store(new Pokemon(PokemonDatabase.Get(name)!, 4 + boxed.Count * 3));
        game.State = GameState.PCStorage;
        var pc = game.PcScreen;
        pc.Open(boxed);
        Frames(30); Shot("30_pc");
        pc.Move(1, 0, party.Count); pc.Move(1, 0, party.Count); pc.Move(0, 1, party.Count);
        Frames(4); Shot("30b_pc_in_the_box");
        pc.Move(0, -1, party.Count); pc.Move(0, -1, party.Count);
        Frames(4); Shot("30c_pc_box_name");
        // Plan 06 · R12: the box's own menu, two of its wallpapers, a Pokémon's menu, one carried, its marks and where
        // it was met, the question before a release, and the box's name typed anew
        var bidoof = boxed[0, 1]!;
        bidoof.Met("Route 201", new DateTime(2026, 6, 1));
        bidoof.HeldItem = ItemDatabase.Get("Oran Berry");
        pc.Confirm(party, boxed, engine.ShowNotification);
        Frames(4); Shot("30d_pc_box_menu");
        pc.MenuIndex = 1;
        pc.Confirm(party, boxed, engine.ShowNotification);
        Frames(4); Shot("30e_pc_wallpaper_city");
        boxed.UnlockedWallpapers = 0xFF;
        boxed.Boxes[0].Wallpaper = PcBoxes.WallpaperNames.Count - 1;
        pc.Move(0, 1, party.Count);
        Frames(4); Shot("30f_pc_wallpaper_galactic");
        boxed.Boxes[0].Wallpaper = 0;
        pc.Confirm(party, boxed, engine.ShowNotification);
        Frames(4); Shot("30g_pc_menu");
        pc.Choose(PcAction.Move, party, boxed, engine.ShowNotification);
        pc.Move(0, 1, party.Count); pc.Move(0, 1, party.Count);
        Frames(4); Shot("30h_pc_carrying");
        pc.Confirm(party, boxed, engine.ShowNotification);
        pc.Confirm(party, boxed, engine.ShowNotification);
        pc.Choose(PcAction.Mark, party, boxed, engine.ShowNotification);
        pc.Confirm(party, boxed, engine.ShowNotification);
        for (int i = 0; i < 3; i++) pc.Move(1, 0, party.Count);
        pc.Confirm(party, boxed, engine.ShowNotification);
        pc.Move(1, 0, party.Count);
        Frames(4); Shot("30i_pc_marks");
        pc.MarkIndex = 6;
        pc.Confirm(party, boxed, engine.ShowNotification);
        Frames(4); Shot("30j_pc_marked");
        pc.Confirm(party, boxed, engine.ShowNotification);
        pc.Choose(PcAction.Release, party, boxed, engine.ShowNotification);
        Frames(20); Shot("30k_pc_release");
        pc.Cancel(party, boxed);
        for (int i = 0; i < 3; i++) pc.Move(0, -1, party.Count);
        pc.Confirm(party, boxed, engine.ShowNotification);
        pc.MenuIndex = 0;
        pc.Confirm(party, boxed, engine.ShowNotification);
        var naming = pc.Naming!;
        while (naming.Backspace()) { }
        foreach (char c in "Fav") naming.Type(c);
        naming.Move(3, 1);
        Frames(4); Shot("30l_pc_naming");
        naming.Type('s');
        pc.FinishNaming(boxed, naming.Result(boxed.Boxes[0].Name));
        Frames(4); Shot("30m_pc_named");
        pc.Close();
        game.Boxes = new PcBoxes();

        // The PC's Hall of Fame (plan 06 · R12): two teams, the newest first; a Pokémon of it, its moves, the older team
        var champions = new Party();
        foreach (var (species, level, nickname) in new[]
                 {
                     ("Torterra", 52, "Bramble"), ("Staraptor", 50, ""), ("Luxray", 49, "Volt"), ("Gastrodon", 48, ""), ("Lucario", 51, ""),
                     ("Garchomp", 54, "")
                 })
        {
            var member = new Pokemon(PokemonDatabase.Get(species)!, level);
            if (nickname.Length > 0) member.Nickname = nickname;
            champions.Add(member);
        }
        champions.Members[4].IsShiny = true;
        fame.Enter(champions, p => ("Lucas", 12345), new DateTime(2026, 10, 21));
        foreach (var member in champions.Members.Concat(party.Members)) PokemonSprites.Request(member.ModelName);
        PokemonSprites.Flush(game.RenderContext);
        var hallOfFame = game.HallOfFameScreen;
        game.State = GameState.HallOfFame;
        hallOfFame.Open();
        Frames(30); Shot("30n_hall_of_fame");
        hallOfFame.Move(0, 1, fame); hallOfFame.Move(0, 1, fame); hallOfFame.Move(0, 1, fame); hallOfFame.Move(0, 1, fame);
        Frames(4); Shot("30o_hall_of_fame_shiny");
        hallOfFame.Turn();
        Frames(4); Shot("30p_hall_of_fame_moves");
        hallOfFame.Move(1, 0, fame);
        hallOfFame.Turn();
        Frames(4); Shot("30q_hall_of_fame_first_team");
        hallOfFame.Close();
        game.HallOfFame = fameBefore;

        // The Journal (plan 06 · R12): two days, the newest first, with a Pokémon caught and a trainer beaten
        var journalBefore = game.Journal;
        var journal = new Journal();
        journal.TakenUp(new DateTime(2026, 6, 1), "Twinleaf Town");
        journal.Tell(new JournalEvent(JournalEventKind.LeftResearchLab));
        journal.Tell(new JournalEvent(JournalEventKind.ArrivedInLocation, "Sandgem Town"));
        journal.BeatTrainer("Youngster Tristan", "Route 202");
        journal.TakenUp(new DateTime(2026, 6, 3), "Jubilife City");
        journal.Tell(new JournalEvent(JournalEventKind.ShoppedAtMart));
        journal.Tell(new JournalEvent(JournalEventKind.ItemWasObtained, "the Pokétch"));
        journal.Tell(new JournalEvent(JournalEventKind.ArrivedInLocation, "Oreburgh City"));
        journal.Caught("Starly", "Route 203");
        journal.BeatTrainer("Rival " + PlayerIdentity.RivalName, "Route 203");
        PokemonSprites.Request("Starly");
        PokemonSprites.Flush(game.RenderContext);
        game.Journal = journal;
        game.State = GameState.Journal;
        var journalScreen = game.JournalScreen;
        journalScreen.Open();
        Frames(30); Shot("22j_journal");
        journalScreen.Turn(1, journal);
        Frames(4); Shot("22k_journal_older");
        journalScreen.Close();
        game.Journal = journalBefore;
        game.State = GameState.Overworld;

        // The battle's panels for switching and for the bag
        var mb = StartBattle("Shinx", 5);
        ToMainMenu(mb);
        mb.HUD.MenuState = BattleMenuState.SwitchPokemon;
        mb.HUD.SwitchMenuIndex = 1;
        Frames(2); Shot("23_battle_switch");
        mb.HUD.MenuState = BattleMenuState.Main;
        // The battle's bag (plan 06 · R11): its four pockets and the last item used, a pocket's items, who a Potion is
        // for, and which move an Ether is for
        mb.SelectMainMenuOption(1);
        Frames(12); Shot("24_battle_bag");
        ShotCrop("24b_battle_bag_native", 40, 820, 1140, 230, 2);
        mb.SelectBagPocket(0);
        Frames(12); Shot("24c_battle_bag_items");
        mb.SelectBagItem(mb.BagListed.ToList().FindIndex(st => st.Data.Name == "Potion"));
        Frames(12); Shot("24d_battle_bag_target");
        mb.BagBack(); mb.BagBack();
        mb.SelectBagItem(mb.BagListed.ToList().FindIndex(st => st.Data.Name == "Ether"));
        mb.SelectBagTarget(0);
        Frames(12); Shot("24e_battle_bag_move");
        mb.BagBack(); mb.BagBack(); mb.BagBack(); mb.BagBack();
        mb.HUD.MenuState = BattleMenuState.Main;

        // A trainer battle, for the row of balls under the foe's box and a long message
        var menuTrainer = MapDatabase.Get("Sinnoh").NPCs.First(n => n.IsTrainer).TrainerData!;
        mb = StartBattle("", 0, menuTrainer);
        Frames(131); Confirm(mb); Frames(74); Confirm(mb); Frames(74); Confirm(mb); Frames(2);
        Shot("25_trainer_hud");

        // The Pokédex: the Sinnoh list and an entry's three pages, the search and its results, the National Pokédex
        // and a diploma; then a battle against a species from a later generation
        var dexScreen = game.PokedexScreen;
        pokedex.RegisterCaught(387);
        pokedex.RegisterSeen(906);
        pokedex.RegisterSeen(396);
        pokedex.RegisterSeen(399);
        foreach (string caught in new[] { "Onix", "Bidoof", "Shinx", "Psyduck", "Machop" }) pokedex.RegisterCaught(PokemonDatabase.Get(caught)!.DexNumber);
        void Pick(string name) => dexScreen.SelectedIndex = dexScreen.Rows.ToList().FindIndex(r => r.Species.Name == name);
        // Menu sprites are baked the first time a menu asks for them: bake the ones these shots show beforehand
        foreach (int seen in pokedex.SeenSpecies) PokemonSprites.Request(PokemonDatabase.GetByDex(seen)!.Name);
        PokemonSprites.Flush(game.RenderContext);
        void Page(PokedexPage page) { while (dexScreen.Page != page) dexScreen.Sideways(1); }
        game.State = GameState.PokedexMenu;
        dexScreen.Open(pokedex, game.World.Portrait(PlayerIdentity.Character));
        Frames(30); Shot("26_pokedex_turtwig");
        Pick("Starly");
        Frames(2); Shot("26c_pokedex_seen_only");
        Pick("Bibarel");
        Frames(2); Shot("26d_pokedex_unseen");
        Pick("Starly");
        dexScreen.Confirm();
        Page(PokedexPage.Area);
        Frames(2); Shot("26e_pokedex_area_starly");
        Pick("Psyduck");
        Frames(2); Shot("26f_pokedex_area_psyduck");
        Pick("Turtwig");
        Frames(2); Shot("26g_pokedex_area_unknown");
        Page(PokedexPage.Size);
        Frames(2); Shot("26h_pokedex_size_turtwig");
        Pick("Onix");
        Frames(2); Shot("26i_pokedex_size_onix");
        dexScreen.Cancel();
        dexScreen.OpenSearch();
        for (int i = 0; i < 2; i++) dexScreen.Sideways(1);   // the heaviest first
        Frames(2); Shot("26j_pokedex_search");
        dexScreen.Search();
        Frames(2); Shot("26k_pokedex_results_heaviest");
        dexScreen.Cancel();
        pokedex.UnlockNational();
        dexScreen.OpenSearch();
        dexScreen.Sideways(1);
        dexScreen.Search();
        Pick("Sprigatito");
        Frames(2); Shot("26b_pokedex_later_generation");
        var complete = new Pokedex();
        foreach (var e in Pokedex.Entries(PokedexMode.Sinnoh)) complete.RegisterSeen(e.Species.DexNumber);
        dexScreen.Open(complete);
        Frames(20); Shot("26l_pokedex_diploma");
        dexScreen.Close();
        var later = StartBattle("Sprigatito", 5);
        ToMainMenu(later);
        Frames(2); Shot("27_battle_later_generation");

        // The move hints (plan 12 · Q10), last so that no shot before them moves: a Geodude seen before the battle,
        // which a Grass move hits four times as hard and a Normal one half as hard
        pokedex.RegisterSeen(PokemonDatabase.Get("Geodude")!.DexNumber);
        var hinted = StartBattle("Geodude", 5, hints: true);
        ToMainMenu(hinted);
        hinted.HUD.MenuState = BattleMenuState.Moves;
        Frames(2); Shot("23g_battle_hints");
        ShotCrop("23g2_battle_hints_native", 40, 820, 1140, 230, 2);

        if (args.Length > 2)
            Boards(args[2], new[]
            {
                "09_startmenu", "20b_summary", "23_battle_switch", "24_battle_bag", "21_starter", "22_bag", "26_pokedex_turtwig",
                "28_trainer_card", "29_shop", "30_pc"
            });

        party.Members[1].Status = StatusCondition.None;
        party.HealAll();
        game.State = GameState.Overworld;
    }
}
