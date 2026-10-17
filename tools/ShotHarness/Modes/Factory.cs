using PokemonPlatinumEngine.Models.Frontier;

partial class Harness
{
    // ---------------------------------------------------------------- the Battle Factory (plan 06 · R18b)

    // The Battle Factory from the Frontier's grounds to its Battle Points (bf*; not part of `all`), through the game's
    // own steps, TryInteract, ArriveOnMap, scripts, question boxes, rental screen and battles: the Factory from
    // outside and the walk in under its porch, the lobby from the head of its stairs and at its counter, the records'
    // board, the counter's questions, the corridor, the staff's hint, the six on offer, a rental's menu and summary,
    // the three chosen and the question, a trainer stepping up, the battle, the trade offered, one's own to give up,
    // the beaten three, the trade made, the round won and its Battle Points, the board afterwards; then a challenge put
    // down and found waiting, and the Factory Head at the twenty-first battle with his print. It wins every battle (the
    // foes are left a single HP) and prints what each left behind.
    public void FactoryMode()
    {
        var story = game.Story;
        var records = game.Frontier;
        game.State = GameState.Overworld;
        game.LocationSign.Hide();

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

        // What the next questions are answered with (the first answer once it has run out), what is done at the rental
        // screen the next time it opens, and what is shot as a battle's first menu comes up
        var answers = new Queue<int>();
        var atScreen = new Queue<Action<FactoryScreen>>();
        Action<BattleEngine>? atMenu = null;

        void Win()
        {
            var fight = game.Battle!;
            for (int guard = 0; guard < 400 && !fight.IsBattleOver; guard++)
            {
                if (fight.HUD.MenuState == BattleMenuState.Main)
                {
                    if (atMenu != null) { var show = atMenu; atMenu = null; show(fight); }
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
            Console.WriteLine($"  the battle: {fight.Result}");
            Until(() => State() != GameState.Battle && State() != GameState.Transition, "the field again", 1500);
            Frames(6);
        }
        // The rental screen as it opens: what the mode asked for, or the first three rented and no trade made
        void Rentals()
        {
            var screen = game.FactoryRentals;
            Frames(50);
            Until(() => !PokemonSprites.Busy, "the rentals' pictures", 3600);
            Frames(4);
            if (atScreen.Count > 0) { atScreen.Dequeue()(screen); Frames(4); return; }
            if (screen.Mode == FactoryScreenMode.Rent)
            {
                for (int n = 0; n < screen.Needed; n++)
                {
                    while (screen.Cursor != n) screen.Move(1, 0);
                    screen.Confirm();
                    screen.Move(0, 1);
                    screen.Confirm();
                }
                screen.Confirm();
            }
            else
            {
                screen.Cancel();
                screen.Move(-1, 0);
                screen.Confirm();
            }
            Frames(4);
        }
        void ReadOn(Func<bool>? until = null, int most = 9000)
        {
            for (int i = 0; i < most; i++)
            {
                if (until != null && until()) return;
                if (until == null && !engine.ScriptRunning && !Box().IsActive && State() == GameState.Overworld) return;
                if (State() == GameState.Battle) { Win(); continue; }
                if (State() == GameState.FactoryRentals) { Rentals(); continue; }
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
            int answer = answers.Count > 0 ? answers.Peek() : 0;
            for (int k = 0; k < answer; k++) engine.Choice.Move(1);
            Frames(3);
            Shot(shot);
            for (int k = 0; k < answer; k++) engine.Choice.Move(-1);
        }
        void Arrived(string map, string shot, int frames = 20)
        {
            ReadOn(() => Here().Name == map && State() == GameState.Overworld);
            Frames(frames);
            Shot(shot);
        }
        void Talk(int x, int y, Direction facing)
        {
            game.Player.SetPosition(x, y, facing);
            Frames(3);
            game.Interact();
            Frames(3);
        }
        void Board(string shot)
        {
            Until(() => State() == GameState.FrontierRecords, "the records' board");
            Frames(30);
            Shot(shot);
            game.FrontierBoard.Close();
            Frames(4);
            ReadOn();
        }
        string Told() => $"Lv. 50 single {records.Get(FrontierStat.FactoryLatestStreak50Single)}/{records.Get(FrontierStat.FactoryRecordStreak50Single)} "
            + $"(trades {records.Get(FrontierStat.FactoryLatestTradeCount50Single)}/{records.Get(FrontierStat.FactoryRecordTradeCount50Single)}, "
            + $"{(records.IsRunning(FrontierFacility.Factory, 0) ? "running" : "ended")}); {records.BattlePoints} BP; victories {records.VictoriesAt(FrontierFacility.Factory)}; "
            + $"trades ever {records.Factory.Trades}; print state {story.Var("VAR_BATTLE_FACTORY_PRINT_STATE")}; lobby action {story.Var("VAR_BATTLE_FACTORY_LOBBY_LOAD_ACTION")}";
        string Named(IEnumerable<FactoryRental> rentals) => string.Join(", ", rentals.Select(r => $"{r.Species} ({r.Item ?? "nothing"}, {r.Record.Nature}, IVs {r.Ivs})"));
        void Rent(FactoryScreen screen, int place)
        {
            while (screen.Cursor != place) screen.Move(1, 0);
            screen.Confirm();
            screen.Move(0, 1);
            screen.Confirm();
        }
        const string Lobby = "BattleFactory";

        // ---- the Factory from the grounds, and the walk in under its porch
        At("BattleFrontier", 70, 35, Direction.Right);
        Frames(40);
        Shot("bf01_factory_from_the_grounds");
        At("BattleFrontier", 72, 35, Direction.Right);
        Frames(10);
        Shot("bf02_factory_porch");
        At("BattleFrontier", 73, 35, Direction.Right);
        engine.Steering = (Direction.Right, false);
        Until(() => engine.ScriptRunning, "the trigger under the porch", 120);
        engine.Steering = null;
        Arrived(Lobby, "bf03_lobby_from_its_door", 30);
        ReadOn();
        Console.WriteLine($"  walked in under the porch: on {Here().Name} at ({game.Player.GridX},{game.Player.GridY}), facing {game.Player.Facing}");
        At(Lobby, 7, 7, Direction.Right);
        Frames(8);
        Shot("bf04_lobby_aisle");
        At(Lobby, 16, 9, Direction.Up);
        Frames(8);
        Shot("bf05_lobby_floor");
        At(Lobby, 22, 8, Direction.Up);
        Frames(8);
        Shot("bf06_lobby_counter");

        // ---- the records' machine with nothing on it, the second counter, somebody of the lobby
        Talk(22, 7, Direction.Up);
        Board("bf07_records_nothing_yet");
        Talk(23, 7, Direction.Up);
        Line("linked up", "bf08_multi_counter");
        ReadOn();
        Talk(17, 5, Direction.Left);
        Line("style", "bf09_someone_of_the_lobby");
        ReadOn();

        // ---- a Single challenge at Level 50, from the counter to the Battle Points
        Talk(21, 7, Direction.Up);
        Question("Which challenge", "bf10_counter_question");
        Question("which level", "bf11_level_question");
        Question("save it", "bf12_save_question");
        Line("This way", "bf13_this_way");
        Arrived("BattleFactoryCorridor", "bf14_corridor", 40);
        var factory = game.Factory!;
        Console.WriteLine($"  the round's trainers: {string.Join(", ", factory.Trainers.Take(7).Select(t => FrontierDatabase.Trainer(t).Class + " " + FrontierDatabase.Trainer(t).Name))}");
        Console.WriteLine($"  the six on offer: {Named(factory.Offer)}");
        Console.WriteLine($"  the first trainer's three: {Named(factory.NextFoe)}");
        Line("Their Pokémon are", "bf15_hint_three_species");

        // The six, a rental's menu, its summary, two chosen, the third and the question
        atScreen.Enqueue(screen =>
        {
            Frames(20);
            Shot("bf16_six_on_offer");
            screen.Move(1, 0);
            screen.Confirm();
            Frames(8);
            Shot("bf17_a_rentals_menu");
            screen.Confirm();
            Frames(30);
            Shot("bf18_a_rentals_summary");
            screen.Move(0, 1);
            Frames(30);
            Shot("bf19_the_next_ones_summary");
            screen.Move(0, -1);
            screen.Cancel();
            screen.Move(0, 1);
            screen.Confirm();
            Rent(screen, 3);
            Frames(8);
            Shot("bf1a_two_chosen");
            screen.Confirm();
            Frames(8);
            Shot("bf1b_remove_offered");
            screen.Cancel();
            Rent(screen, 5);
            Frames(20);
            Shot("bf1c_three_chosen_question");
            screen.Confirm();
        });
        Line("Please go in", "bf1d_go_in");
        Console.WriteLine($"  rented: {Named(factory.Team)}; trades {factory.Trades}");
        Arrived("BattleFactoryBattleRoom", "bf1e_battle_room", 10);
        Line("steps up", "bf1f_a_trainer_steps_up");
        atMenu = fight =>
        {
            Skip(0.6);
            Shot("bf21_battle_menu");
            Console.WriteLine($"  the battle: {string.Join(", ", fight.PlayerParty.Members.Select(p => $"{p.Species.Name} Lv{p.Level}"))} against "
                + $"{string.Join(", ", fight.EnemyParty.Members.Select(p => $"{p.Species.Name} Lv{p.Level}"))}; frontier {fight.IsFrontier}");
        };
        ReadOn(() => State() == GameState.Battle);
        Skip(1.6);
        Shot("bf20_battle_begins");
        Line("restored", "bf22_between_battles");
        Question("Are you ready", "bf23_next_battle_question");
        Line("Their Pokémon are", "bf24_next_hint");
        Question("trade one", "bf25_trade_question");
        // The trade: one's own three, the menu, the beaten three, the question, the trade made
        atScreen.Enqueue(screen =>
        {
            Frames(20);
            Shot("bf26_trade_your_three");
            screen.Move(1, 0);
            screen.Confirm();
            Frames(8);
            Shot("bf27_trade_menu");
            screen.Move(0, 1);
            screen.Confirm();
            Frames(8);
            Shot("bf28_trade_their_three");
            screen.Move(1, 0);
            screen.Move(1, 0);
            Frames(4);
            screen.Confirm();
            Frames(20);
            Shot("bf29_trade_accept_question");
            screen.Confirm();
            Frames(20);
            Shot("bf2a_trade_made");
            screen.Confirm();
        });
        ReadOn(() => Here().Name == "BattleFactoryBattleRoom" && State() == GameState.Overworld);
        Console.WriteLine($"  after the trade: {Named(factory.Team)}; trades {factory.Trades}; next {Named(factory.Foe)}");
        Line("Seven wins", "bf2b_seven_won");
        Line("received", "bf2c_battle_points");
        Console.WriteLine($"  after the round: {Told()}");
        Arrived(Lobby, "bf2d_lobby_afterwards", 10);
        Line("all seven", "bf2e_lobby_congratulations");
        ReadOn();
        Console.WriteLine($"  back in the lobby: challenge {(game.Factory == null ? "let go of" : "still held")}; {Told()}");
        Talk(22, 7, Direction.Up);
        Board("bf2f_records_after_a_round");

        // ---- the second round: two told of, a battle, and the challenge put down and found waiting
        Talk(21, 7, Direction.Up);
        Line("Two of their", "bf30_hint_two_species");
        answers.Enqueue(1);
        Question("Are you ready", "bf31_rest_chosen");
        Question("Put the challenge down", "bf32_rest_question");
        answers.Enqueue(0);
        answers.Enqueue(1);
        Arrived(Lobby, "bf33_lobby_put_down", 10);
        Question("go on with it now", "bf34_waiting_question");
        ReadOn();
        Console.WriteLine($"  put down: {(records.Factory.Paused is { } p ? $"battle {p.Battle}, {Named(p.Team)}" : "nothing")}; {Told()}");
        // Taken up again, and given up
        Talk(21, 7, Direction.Up);
        answers.Enqueue(0);
        answers.Enqueue(2);
        Question("Are you ready", "bf35_taken_up_again");
        Question("Give the challenge up", "bf36_retire_question");
        Line("Thank you for taking", "bf37_lobby_given_up");
        ReadOn();
        Console.WriteLine($"  given up: {Told()}");

        // ---- the Factory Head: a streak of fourteen taken up, its third round's seventh battle
        records.Set(FrontierStat.FactoryLatestStreak50Single, 14);
        records.Set(FrontierStat.FactoryRecordStreak50Single, 14);
        records.SetRunning(FrontierFacility.Factory, 0, true);
        Talk(21, 7, Direction.Up);
        Line("it knows", "bf40_hint_species_and_move");
        Line("Factory Head himself", "bf41_head_announced");
        ReadOn(() => Here().Name == "BattleFactoryBattleRoom" && State() == GameState.Overworld && game.Factory is { Battle: 6 });
        Line("you rented", "bf42_thorton_appears");
        Line("per cent", "bf43_thorton_reckons");
        atMenu = fight =>
        {
            Skip(0.6);
            Shot("bf45_thorton_battle_menu");
            Console.WriteLine($"  Thorton's three: {string.Join(", ", fight.EnemyParty.Members.Select(q => $"{q.Species.Name} Lv{q.Level} ({q.HeldItem?.Name}, IV {q.IvAttack})"))}");
        };
        ReadOn(() => State() == GameState.Battle);
        Skip(1.6);
        Shot("bf44_thorton_battle");
        Line("machine was wrong", "bf46_thorton_beaten");
        Line("received", "bf47_twenty_points");
        Line("Silver Print", "bf48_silver_print");
        ReadOn();
        Console.WriteLine($"  after the Factory Head: {Told()}; brains met {records.BrainsMet}");
        Talk(22, 7, Direction.Up);
        Board("bf49_records_with_a_print");
    }
}
