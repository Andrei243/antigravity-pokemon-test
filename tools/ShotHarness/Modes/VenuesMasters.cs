partial class Harness
{
    // ---------------------------------------------------------------- the venues of plan 14, B15

    // The Masters' Hall and the titles (vn4*; the end of `venues`, or alone with VENUES_FROM=masters), through the game's
    // own warps, TryInteract, script, question box, party screen and battle: the hall come into by the annex lobby's west
    // door after the Hall of Fame (vn40_masters_hall), the receptionist's question answered and a Bidoof chosen with the
    // party screen's own buttons, the master stepped down to the mark (vn41_master_steps_up), the battle won with the bag
    // shut, the title said (vn42_master_title), and the Trainer Card's front with the title under the name and its page of
    // titles (vn43_card_titles). It wins the battle (the master's Pokémon is left a single HP at each menu, the lead kept
    // whole), and prints who was chosen, the master's team, the titles before and after and the latest.
    public void VenuesMasters(bool fresh)
    {
        if (fresh)
        {
            engine.StartNewGame();
            PastTheOpening();
            game.State = GameState.Overworld;
            game.LocationSign.Hide();
        }
        var team = game.Party;
        var story = game.Story;
        DialogueManager Box() => game.Dialogue;
        GameState State() => game.State;
        Map Here() => game.Map;
        int quiet = 0;
        void Tick()
        {
            FrameClock.Fixed = ++tick / 60.0;
            engine.Update(1f / 60f);
            if (++quiet % 6 == 0) engine.Draw();
        }
        void Until(Func<bool> holds, string what, int most = 900)
        {
            for (int i = 0; i < most && !holds(); i++) Tick();
            if (!holds()) Console.WriteLine($"  !! never happened: {what}");
        }
        bool Says(string word) => Box().IsActive && Translated.Saying(Box().CurrentLine, word);
        string? stepShot = null;
        void ReadTheBattle()
        {
            var fight = game.Battle!;
            Console.WriteLine($"  the master's battle: bag closed {fight.BagClosed}; {fight.Trainers[0].FullTitle}'s "
                + string.Join(", ", fight.EnemyParty.Members.Select(p => $"{p.Species.Name} {p.Level} ({string.Join("/", p.Moves.Select(m => m.Name))})")));
            for (int guard = 0; guard < 1500 && !fight.IsBattleOver; guard++)
            {
                if (fight.HUD.MenuState == BattleMenuState.Main)
                {
                    var lead = fight.PlayerPokemon;
                    lead.CurrentHP = lead.MaxHP;
                    lead.Status = StatusCondition.None;
                    foreach (var move in lead.Moves) move.CurrentPP = move.MaxPP;
                    foreach (var foe in fight.EnemyParty.Members) foe.CurrentHP = Math.Min(foe.CurrentHP, 1);
                    var target = fight.EnemyPokemon;
                    int pick = lead.Moves.FindIndex(m => m.Data.Category != MoveCategory.Status
                        && TypeChart.GetEffectiveness(m.Data.Type, target.PrimaryType, target.SecondaryType) > 0f);
                    fight.SelectMainMenuOption(0);
                    fight.SelectMove(Math.Max(0, pick));
                    if (fight.HUD.MenuState == BattleMenuState.SelectTarget) fight.SelectTarget(0);
                }
                else
                {
                    fight.ConfirmMessage();
                    Tick();
                }
                Skip(0.4);
            }
            Console.WriteLine($"  the master's battle: {fight.Result} in {fight.Core.Turn} turns");
            for (int i = 0; i < 1500 && State() is GameState.Battle or GameState.Transition; i++) Tick();
            Frames(6);
        }
        void ReadOn(Func<bool>? until = null, int most = 30000)
        {
            for (int i = 0; i < most; i++)
            {
                if (until != null && until()) return;
                if (until == null && !engine.ScriptRunning && !Box().IsActive && State() == GameState.Overworld) return;
                if (State() == GameState.Battle) { ReadTheBattle(); continue; }
                if (engine.Choice.IsOpen)
                {
                    Skip(12 / 60.0);
                    engine.Choice.Confirm();
                    Skip(4 / 60.0);
                }
                else if (Box().IsActive && Box().IsCurrentLineComplete && !Box().IsQuestion)
                {
                    // The master on the mark, as his first line is read
                    if (stepShot != null && Says("So this is your")) { Frames(4); Shot(stepShot); stepShot = null; }
                    Box().Advance(); Tick(); Tick();
                }
                else Tick();
            }
            Console.WriteLine("  !! the script never got there");
        }

        // The game past the Hall of Fame, and a team of a Starly and a Bidoof that has come a long way
        story.Set("SinnohHallOfFame");
        team.Members.Clear();
        var starly = new Pokemon(PokemonDatabase.Get("Starly")!, 40);
        var bidoof = new Pokemon(PokemonDatabase.Get("Bidoof")!, 90);
        bidoof.Moves.Clear();
        foreach (string move in new[] { "Superpower", "Hyper Fang", "Waterfall", "Rock Smash" }) bidoof.Moves.Add(new Move(MoveDatabase.Get(move)));
        team.Members.Add(starly);
        team.Members.Add(bidoof);
        int titlesBefore = MasterTitles.Count(story);

        // ---- the annex's lobby, and the hall through its west door
        At("BattleFrontierAnnex", 1, 6, Direction.Left);
        Frames(10);
        // A step west into the door, as the player takes it
        for (int i = 0; i < 600 && Here().Name != "MastersHall"; i++) { engine.Steering = (Direction.Left, false); Tick(); }
        engine.Steering = null;
        Until(() => Here().Name == "MastersHall" && State() == GameState.Overworld, "the Masters' Hall", 600);
        Frames(40);
        Shot("vn40_masters_hall");
        Console.WriteLine($"  in the hall: on {Here().Name} at ({game.Player.GridX},{game.Player.GridY}); people {string.Join(", ", Here().NPCs.Select(n => $"{n.Key} ({n.GridX},{n.GridY})"))}");

        // ---- the receptionist: Yes, and the Bidoof chosen with the party screen's own buttons
        game.Player.SetPosition(4, 10, Direction.Left);
        Frames(3);
        game.Interact();
        Frames(3);
        ReadOn(() => State() == GameState.PartyMenu);
        Frames(30);
        var screen = game.PartyScreen;
        screen.SelectedIndex = 1;
        Frames(4);
        Console.WriteLine($"  choosing: the cursor on {team.Members[screen.SelectedIndex].Species.Name}");
        screen.Confirm(team);
        Frames(4);
        stepShot = "vn41_master_steps_up";
        ReadOn(() => Says("holds the title of"));
        if (Says("holds the title of")) { Box().FinishLine(); Frames(4); Shot("vn42_master_title"); }
        else Console.WriteLine("  !! the title was never said");
        ReadOn();
        Console.WriteLine($"  titles {titlesBefore} -> {MasterTitles.Count(story)}; latest {MasterTitles.Latest(story) ?? "none"}; "
            + $"master_bidoof beaten {story.HasDefeated("master_bidoof")}; the Bidoof now level {bidoof.Level}, still the team's {team.Members.Contains(bidoof)}");

        // ---- the Trainer Card: the title under the name, and the page of titles
        game.State = GameState.Overworld;
        game.ChooseFromStartMenu(StartMenuChoice.Trainer);
        Frames(40);
        Shot("vn43a_card_latest_title");
        if (game.TrainerCard.GoTo(PokemonPlatinumEngine.UI.CardPage.Titles))
        {
            Frames(30);
            Shot("vn43_card_titles");
        }
        else Console.WriteLine("  !! the card has no page of titles");
        Console.WriteLine($"  the card's pages: titles {game.TrainerCard.HasTitlesPage}, rows {game.TrainerCard.LastTitleRow + 1} to scroll");
        game.TrainerCard.Close();
        game.State = GameState.Overworld;
        Frames(10);
    }
}
