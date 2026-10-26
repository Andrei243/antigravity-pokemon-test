using PokemonPlatinumEngine.Models.Frontier;

partial class Harness
{
    // ---------------------------------------------------------------- the venues of plan 14 (B8 on)

    // The venues of the later games' formats (vn*; not part of `all`), through the game's own TryInteract, scripts,
    // question boxes, team screen, bracket and battles. B8, the Battle Dome: the guide before the lodge on the Battle
    // Frontier's plaza and the walk into the annex, its lobby and the Dome's desk, the rules, three entered, the hall,
    // the bracket as each round opens (vn01_dome_bracket), an opponent's card (vn02_dome_card), the pick of two beside
    // the card, the opponent stepping into the ring, the battle at its menu, and after four wins the bracket with the
    // player as champion and the Battle Points. It wins every battle (the foes are left a single HP at each menu), and
    // prints the bracket's draw, who met whom headless and who won, the two sent in each round, the record and the
    // points, and whether the final's Vs. Recorder file replays to the same lines.
    public void VenuesMode()
    {
        // B9's Palace and Arena alone (Modes/VenuesPalaceArena.cs)
        if (Environment.GetEnvironmentVariable("VENUES_FROM") == "palace")
        {
            VenuesPalaceArena(fresh: true);
            return;
        }
        engine.StartNewGame();
        PastTheOpening();
        game.State = GameState.Overworld;
        game.LocationSign.Hide();
        var team = game.Party;
        var records = game.Frontier;

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
        Pokemon Mon(string species, int level, string? item = null)
        {
            var p = new Pokemon(PokemonDatabase.Get(species)!, level);
            if (item != null) p.HeldItem = ItemDatabase.Get(item);
            game.Pokedex.RegisterSeen(p.Species.DexNumber);
            game.Pokedex.RegisterCaught(p.Species.DexNumber);
            return p;
        }

        // What the next questions are answered with (the first answer once it has run out), and what is shot as the
        // bracket, its card, its pick and a battle's first menu come up
        var answers = new Queue<int>();
        string? bracketShot = null, cardShot = null, pickShot = null, menuShot = null;
        bool replayTheFinal = false;

        void Win()
        {
            var fight = game.Battle!;
            int round = game.Dome?.Round ?? -1;
            for (int guard = 0; guard < 400 && !fight.IsBattleOver; guard++)
            {
                if (fight.HUD.MenuState == BattleMenuState.Main)
                {
                    if (menuShot != null) { Frames(20); Shot(menuShot); menuShot = null; }
                    foreach (var foe in fight.EnemyParty.Members) foe.CurrentHP = Math.Min(foe.CurrentHP, 1);
                    var lead = fight.PlayerPokemon;
                    lead.CurrentHP = lead.MaxHP;
                    lead.Status = StatusCondition.None;
                    foreach (var move in lead.Moves) move.CurrentPP = move.MaxPP;
                    var target = fight.EnemyPokemon;
                    int pick = lead.Moves.FindIndex(m => m.Data.Category != MoveCategory.Status
                        && TypeChart.GetEffectiveness(m.Data.Type, target.PrimaryType, target.SecondaryType) > 0f);
                    fight.SelectMainMenuOption(0);
                    fight.SelectMove(Math.Max(0, pick));
                    if (fight.HUD.MenuState == BattleMenuState.SelectTarget) fight.SelectTarget(0);
                }
                else if (fight.HUD.MenuState == BattleMenuState.SwitchPokemon)
                {
                    int next = fight.PlayerParty.Members.FindIndex(p => !p.IsFainted && !fight.PlayerSlots.Any(s => s.Pokemon == p));
                    fight.SelectSwitch(Math.Max(0, next));
                }
                else
                {
                    fight.ConfirmMessage();
                    Tick();
                }
                Skip(0.4);
            }
            Console.WriteLine($"  round {round + 1}'s match: {fight.Result} in {fight.Core.Turn} turns, "
                + $"{string.Join(" and ", fight.PlayerParty.Members.Select(p => p.Species.Name))} against "
                + $"{string.Join(" and ", fight.EnemyParty.Members.Select(p => p.Species.Name))}");
            if (replayTheFinal && round == 3)
            {
                if (fight.RecordFile(GameClock.Today) is { } file)
                {
                    var again = file.Replay();
                    bool same = again.Log.OfType<Said>().Select(s => s.Text).SequenceEqual(fight.Core.Log.OfType<Said>().Select(s => s.Text));
                    Console.WriteLine($"  the final's Vs. Recorder file replays to the same lines: {same}{(same ? "" : "  !!")}");
                }
                else if (fight.ChangedFromOutside) Console.WriteLine("  the harness kept the foes at one HP, so the Vs. Recorder keeps no file of the final (a battle changed in the middle wouldn't replay)");
                else Console.WriteLine("  !! the final left no record file");
            }
            Until(() => State() != GameState.Battle && State() != GameState.Transition, "the field again", 1500);
            Frames(6);
        }
        void EnterTeam()
        {
            var screen = game.PartyScreen;
            Frames(60);
            for (int slot = 0; slot < screen.EnterCount; slot++)
            {
                screen.SelectedIndex = slot;
                Frames(2);
                screen.Confirm(team);
                Frames(6);
            }
            screen.Confirm(team);
            Frames(4);
        }
        // The bracket: looked at and shot, its card opened on the next opponent; or the pick, the second and the third sent in
        void AtTheBracket()
        {
            var screen = game.Bracket;
            Frames(40);
            if (screen.Picking)
            {
                if (pickShot != null) { Frames(30); Shot(pickShot); pickShot = null; }
                screen.Move(0, 1);
                Frames(2);
                screen.Confirm();
                Frames(2);
                screen.Move(0, 1);
                Frames(2);
                screen.Confirm();
                Frames(10);
                screen.Confirm();
                Frames(4);
                Console.WriteLine($"  sent in: {string.Join(" and ", game.Dome!.OwnThree(team).Where((_, i) => screen.Outcome.Contains(i)).Select(p => p.Species.Name))}");
                return;
            }
            if (bracketShot != null) { Shot(bracketShot); bracketShot = null; }
            if (cardShot != null)
            {
                screen.Confirm();
                Frames(40);
                Shot(cardShot);
                cardShot = null;
                screen.Cancel();
                Frames(4);
            }
            screen.Cancel();
            Frames(4);
        }
        void ReadOn(Func<bool>? until = null, int most = 20000)
        {
            for (int i = 0; i < most; i++)
            {
                if (until != null && until()) return;
                if (until == null && !engine.ScriptRunning && !Box().IsActive && State() == GameState.Overworld) return;
                if (State() == GameState.Battle) { Win(); continue; }
                if (State() == GameState.PartyMenu) { EnterTeam(); continue; }
                if (State() == GameState.Bracket) { AtTheBracket(); continue; }
                if (engine.Choice.IsOpen)
                {
                    Skip(12 / 60.0);
                    int answer = answers.Count > 0 ? answers.Dequeue() : 0;
                    for (int k = 0; k < answer; k++) engine.Choice.Move(1);
                    Skip(2 / 60.0);
                    engine.Choice.Confirm();
                    Skip(4 / 60.0);
                }
                else if (Box().IsActive && Box().IsCurrentLineComplete && !Box().IsQuestion) { Box().Advance(); Tick(); Tick(); }
                else Tick();
            }
            Console.WriteLine("  !! the script never got there");
        }
        bool Says(string word) => Box().IsActive && Translated.Saying(Box().CurrentLine, word);
        void Line(string word, string shot)
        {
            ReadOn(() => Says(word));
            if (!Says(word)) { Console.WriteLine($"  !! never said: {word}"); return; }
            Box().FinishLine(); Frames(4); Shot(shot);
        }
        void Question(string word, string shot)
        {
            ReadOn(() => engine.Choice.IsOpen && Says(word));
            if (!engine.Choice.IsOpen) { Console.WriteLine($"  !! never asked: {word}"); return; }
            Frames(16);
            Shot(shot);
        }
        void Talk(int x, int y, Direction facing)
        {
            game.Player.SetPosition(x, y, facing);
            Frames(3);
            game.Interact();
            Frames(3);
        }
        void Bracket(string what)
        {
            if (game.Dome is not { } dome) { Console.WriteLine($"  {what}: no tournament"); return; }
            Console.WriteLine($"  {what}: round {dome.Round + 1}, seats {string.Join(" ", dome.Bracket.Seats)}");
            for (int r = 0; r < dome.Bracket.Rounds; r++)
                Console.WriteLine($"    round {r + 1}: winners {string.Join(" ", dome.Bracket.Winners[r])}");
        }

        // The team: three that may go in, and the levels of a team that has come a long way
        team.Clear();
        team.Add(Mon("Garchomp", 62, "Leftovers"));
        team.Add(Mon("Lucario", 58, "Life Orb"));
        team.Add(Mon("Staraptor", 55, "Choice Band"));

        // ---- the guide on the plaza and the walk into the annex
        At("BattleFrontier", 44, 62, Direction.Down);
        Frames(40);
        Shot("vn00_annex_guide");
        Talk(44, 63, Direction.Down);
        Question("Shall I show you in?", "vn00b_annex_guide_asks");
        ReadOn(() => Here().Name == GameEngine.DomeLobbyMap && State() == GameState.Overworld);
        Frames(30);
        Shot("vn00c_annex_lobby");
        ReadOn();
        Console.WriteLine($"  shown into the annex: on {Here().Name} at ({game.Player.GridX},{game.Player.GridY}), facing {game.Player.Facing}");

        // ---- the Dome's desk: the rules, then three entered
        Talk(7, 5, Direction.Up);
        answers.Enqueue(1);
        Question("enter a tournament", "vn00d_dome_desk");
        Line("Level 50", "vn00e_dome_rules");
        ReadOn();
        Talk(7, 5, Direction.Up);
        answers.Enqueue(0);
        ReadOn(() => State() == GameState.PartyMenu);
        Frames(40);
        Shot("vn00f_dome_three_to_enter");
        EnterTeam();
        Line("bracket has been drawn", "vn00g_dome_drawn");
        Bracket("drawn");
        if (game.Dome is { } drawn)
            Console.WriteLine($"  the field: {string.Join(", ", drawn.Field.Skip(1).Select(e => $"{e.Title} ({string.Join("/", e.Opponent!.Pokemon.Select(p => p.Species.Name))})"))}");

        // ---- the hall: the first round's bracket and an opponent's card, the pick, the ring and the battle
        bracketShot = "vn01_dome_bracket";
        cardShot = "vn02_dome_card";
        pickShot = "vn03_dome_pick";
        menuShot = "vn05_dome_battle";
        ReadOn(() => Here().Name == GameEngine.DomeHallMap && State() == GameState.Overworld);
        Frames(20);
        Shot("vn00h_dome_hall");
        Line("steps into the ring", "vn04_dome_ring");
        // ---- the rest of the tournament to its end, the final's record replayed
        replayTheFinal = true;
        ReadOn(() => Says("champion of the Battle Dome"));
        if (Says("champion of the Battle Dome")) { Box().FinishLine(); Frames(4); Shot("vn06_dome_champion"); }
        Bracket("after the final");
        if (game.Dome is { } done)
            Console.WriteLine($"  headless matches: {string.Join("; ", done.Fought.Select(f => $"{f.A} v {f.B} -> {f.Winner}"))}");
        bracketShot = "vn07_dome_bracket_won";
        Line("received", "vn08_dome_points");
        ReadOn(() => Here().Name == GameEngine.DomeLobbyMap && State() == GameState.Overworld);
        ReadOn();
        Frames(20);
        Shot("vn09_dome_lobby_after");
        Console.WriteLine($"  the record: streak {records.Dome.Streak}, best {records.Dome.Best}, won {records.Dome.Won}, matches {records.Dome.Matches}, "
            + $"in progress {records.Dome.InProgress}; {records.BattlePoints} BP; the team's levels {string.Join(", ", team.Members.Select(p => p.Level))}");

        // ---- B9: the Battle Palace and the Battle Arena, from the same lobby
        VenuesPalaceArena(fresh: false);
    }
}
