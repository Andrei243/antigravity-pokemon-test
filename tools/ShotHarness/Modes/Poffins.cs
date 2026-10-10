partial class Harness
{
    // ---------------------------------------------------------------- Poffins and the contest condition (plan 06 · R14c)

    // Hearthome's Poffin House and Pokémon Fan Club, a Poffin cooked at the pot through each of its stages, the Poffin
    // Case opened from the bag, a Poffin fed and the condition it raised, the summary's condition page, and the Mild
    // Poffin of the Contest Hall's boy (pf*; not part of "all"). Everything goes through the game's own scripts,
    // screens and buttons; it prints what each left behind (the case, the bag, the condition, the flags).
    public void PoffinsMode()
    {
        game.State = GameState.Overworld;
        game.LocationSign.Hide();
        var bag = game.Bag;
        bag.AddItem(ItemDatabase.Get("Pecha Berry")!, 2);
        bag.AddItem(ItemDatabase.Get("Cheri Berry")!, 2);
        bag.AddItem(ItemDatabase.Get("Oran Berry")!, 1);
        DialogueManager Box() => game.Dialogue;
        void Until(Func<bool> holds, string what, int most = 900)
        {
            for (int i = 0; i < most && !holds(); i++) Frames(1);
            if (!holds()) Console.WriteLine($"  !! never happened: {what}");
        }
        void Whole() { Until(() => Box().IsActive, "text on the screen"); Box().FinishLine(); Frames(2); }
        void Next() { Box().Advance(); Frames(2); }
        // Reads every line on to the end of the script, shooting the line that holds a word
        void ReadOn(string? word = null, string? shot = null)
        {
            for (int i = 0; i < 40 && (Box().IsActive || engine.Choice.IsOpen || game.State == GameState.Dialogue); i++)
            {
                if (engine.Choice.IsOpen) { engine.Choice.Confirm(); Frames(4); continue; }
                Whole();
                if (word != null && shot != null && Box().VisibleText.Contains(word)) { Shot(shot); shot = null; }
                Next();
            }
            Until(() => game.State == GameState.Overworld && !Box().IsActive, "the field again");
        }

        // ---- The Pokémon Fan Club: the chairman gives the Poffin Case
        At("Sinnoh", 488, 712, Direction.Up);
        game.LocationSign.Hide();
        Frames(4);
        Shot("pf01_hearthome_doors");
        At("HearthomeFanClub", 6, 15, Direction.Up);
        Frames(6);
        Shot("pf02_fan_club");
        At("HearthomeFanClub", 7, 7, Direction.Left);
        game.Interact(); Frames(3);
        ReadOn("Poffin Case", "pf03_chairman");
        Console.WriteLine($"  the case: {bag.GetQuantity(ItemDatabase.Get("Poffin Case")!)}; flag {game.Story.Has("FLAG_RECEIVED_HEARTHOME_CITY_POKEMON_FAN_CLUB_POFFIN_CASE")}");

        // ---- The Poffin House: the cook's question, the bag's berries, and the pot
        At("PoffinHouse", 8, 12, Direction.Up);
        Frames(6);
        Shot("pf04_poffin_house");
        At("PoffinHouse", 5, 11, Direction.Up);
        game.Interact(); Frames(3);
        Whole(); Next();
        Until(() => engine.Choice.IsOpen, "the cook's question"); Frames(20);
        Shot("pf05_cook_question");
        engine.Choice.Confirm(); Frames(4);
        Until(() => game.State == GameState.BagMenu, "the bag's berries", 300);
        Frames(30);
        Shot("pf06_bag_berries");
        game.BagScreen.Confirm(bag, game.Party, _ => { });
        Frames(2);
        var cooking = game.Cooking;
        Console.WriteLine($"  cooking: {cooking.Phase} with {string.Join(", ", cooking.Berries.Select(b => b.Name))}; {bag.GetQuantity(ItemDatabase.Get("Pecha Berry")!)} Pecha left");
        Frames(28);
        Shot("pf07_pour");
        Until(() => cooking.Phase == CookingPhase.Stir, "the stirring");
        Frames(12);
        Shot("pf08_stir_start");

        // A steady hand: the arrow's way, easing off before the batter spills and stirring again before it burns
        void Stir(Func<bool> until, int most = 3600, Func<PoffinPot, int?>? hold = null)
        {
            for (int i = 0; i < most && cooking.Phase == CookingPhase.Stir && !until(); i++)
            {
                var pot = cooking.Pot!;
                int speed = Math.Abs(pot.Speed);
                cooking.Held = hold?.Invoke(pot) ?? (speed > 3000 ? 0 : pot.Backward ? -1 : 1);
                FrameClock.Fixed = ++tick / 60.0;
                engine.Update(1f / 60f);
                if (i % 6 == 5) engine.Draw();
            }
        }
        Stir(() => Math.Abs(cooking.Pot!.Speed) > 2200 && cooking.Pot.PhaseFrames > 150);
        Frames(2);
        Shot("pf09_stirring");
        // Too fast: holding on spills it over
        Stir(() => cooking.SinceSpill < 0.15f, 900, pot => pot.Backward ? -1 : 1);
        Frames(2);
        Shot("pf10_spilling");
        // Let go and it burns
        Stir(() => cooking.SinceWarning < 0.3f || cooking.SinceBurn < 0.3f, 900, _ => 0);
        Frames(4);
        Shot("pf11_burning");
        Stir(() => cooking.Pot!.Phase == 2 && cooking.Pot.PhaseFrames > 120);
        Frames(2);
        Shot("pf12_last_stage");
        Stir(() => false);
        cooking.Held = null;
        Frames(10);
        Shot("pf13_done");
        Until(() => cooking.Phase == CookingPhase.Results, "the results");
        Frames(40);
        Shot("pf14_results_numbers");
        Until(() => cooking.PhaseTime >= PoffinCookingScreen.CardShown, "the results' card");
        Frames(10);
        Shot("pf15_results");
        var pot = cooking.Pot!;
        Console.WriteLine($"  cooked: {cooking.Made?.Name} Lv. {cooking.Made?.Level} smooth {cooking.Made?.Smoothness} in {pot.Frames} frames, {pot.Burns} burned, {pot.Spills} spilled");
        cooking.PressConfirm(); Frames(6);
        Shot("pf16_put_away");
        cooking.PressConfirm(); Frames(20);
        Shot("pf17_another");
        cooking.MoveChoice(1); Frames(2);
        cooking.PressConfirm(); Frames(4);
        ReadOn();
        Console.WriteLine($"  the case holds {game.Poffins.Count}: {string.Join(", ", game.Poffins.All.Select(p => p.Name))}");

        // ---- The Poffin Case from the bag: a few more Poffins of every kind, the list, a tab, feeding one
        foreach (var flavours in new[] { new[] { 60, 30, 30, 30, 30, 40 }, new[] { 5, 22, 9, 0, 0, 25 }, new[] { 18, 9, 0, 0, 0, 30 },
                     new[] { 0, 0, 0, 12, 7, 22 }, new[] { 0, 0, 0, 0, 0, 30 }, new[] { 0, 0, 25, 0, 0, 20 }, new[] { 4, 8, 12, 16, 0, 35 } })
            game.Poffins.Add(Poffins.Make(flavours.Take(5).ToList(), flavours[5], false, new Random(4)));
        game.ChooseFromStartMenu(StartMenuChoice.Bag);
        Frames(4);
        for (int i = 0; i < 7; i++) game.BagScreen.MovePocket(1);
        var keyItems = bag.GetPocketItems(ItemPocket.KeyItems);
        int caseRow = keyItems.FindIndex(s => s.Name == "Poffin Case");
        game.BagScreen.MoveCursor(caseRow, keyItems.Count);
        Frames(20);
        game.BagScreen.Confirm(bag, game.Party, _ => { });
        Frames(10);
        Shot("pf18_bag_open");
        game.BagScreen.Confirm(bag, game.Party, _ => { });
        Frames(3);
        var poffinCase = game.PoffinCaseScreen;
        Frames(24);
        Shot("pf19_case");
        for (int i = 0; i < 3; i++) poffinCase.MoveTab(1);
        Frames(10);
        Shot("pf20_case_sweet");
        poffinCase.Confirm(); Frames(8);
        Shot("pf21_case_actions");
        poffinCase.Confirm(); Frames(24);
        Shot("pf22_case_team");
        var fed = game.Party.Members[0];
        poffinCase.Confirm(); Frames(20);
        Shot("pf23_case_condition");
        poffinCase.Confirm(); Frames(20);
        Shot("pf24_case_eating");
        poffinCase.Confirm(); Frames(60);
        Shot("pf25_case_after");
        Console.WriteLine($"  {fed.DisplayName} ({fed.Nature}) ate: cool {fed.Cool} beauty {fed.Beauty} cute {fed.Cute} smart {fed.Smart} tough {fed.Tough} sheen {fed.Sheen}, friendship {fed.Friendship}; the case holds {game.Poffins.Count}");
        poffinCase.Confirm(); Frames(4);
        poffinCase.Cancel(); Frames(4);
        Console.WriteLine($"  back to: {game.State}");
        game.BagScreen.Close();
        Frames(2);
        game.State = GameState.Overworld;

        // ---- The summary's condition page, once the Contest Hall has been visited
        game.Story.Set(GameEngine.ContestHallVisitedFlag);
        fed.Beauty = 140;
        fed.Smart = 60;
        fed.Tough = 90;
        fed.Sheen = 130;
        game.ChooseFromStartMenu(StartMenuChoice.Pokemon);
        Frames(4);
        game.PartyScreen.Confirm(game.Party);
        game.PartyScreen.Confirm(game.Party);
        Frames(20);
        Shot("pf26_summary");
        game.PartyScreen.TurnSummaryPage(1);
        Frames(20);
        Shot("pf27_summary_condition");
        game.PartyScreen.Close();
        game.State = GameState.Overworld;

        // ---- The Contest Hall's boy and his Mild Poffin
        At("ContestHallLobby", 16, 7, Direction.Up);
        Frames(4);
        int before = game.Poffins.Count;
        game.Interact(); Frames(3);
        ReadOn("Mild Poffin!", "pf28_mild_poffin");
        // He walks off toward the practice counter, eight steps
        Frames(150);
        Shot("pf29_boy_walked_off");
        Console.WriteLine($"  the boy gave {game.Poffins.Count - before} Poffin(s): {game.Poffins.All[^1].Name}; flag {game.Story.Has("FLAG_RECEIVED_CONTEST_HALL_LOBBY_MILD_POFFIN")}");
    }
}
