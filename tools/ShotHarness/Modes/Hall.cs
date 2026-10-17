using PokemonPlatinumEngine.Models.Frontier;

partial class Harness
{
    // ---------------------------------------------------------------- the Battle Hall (plan 06 · R18c)

    // The Battle Hall from the Frontier's grounds to its Battle Points (bh*; not part of `all`), through the game's own
    // steps, TryInteract, ArriveOnMap, scripts, question boxes, team screen, board of types and battles: the Hall
    // from outside and its door, the lobby from its mat, its floor and counter, the records' board with nothing on
    // it, the record keeper, the second counter and the fan; the first counter's questions, the team with one
    // entered, the passage down, the stage and its announcer, the board (a type under the cursor, the question, the
    // entered Pokémon's place and its summary), the battle called, the trainer on the stage, the battle, the
    // Pokémon restored and the question between battles, the board with a rank won, the tenth won, its Battle
    // Points, the lobby's congratulations, the board afterwards and the record keeper's count; a challenge put down,
    // found waiting, taken up again and given up; a Double challenge with two of a kind; and the Hall Matron at the
    // fiftieth battle: the bigger crowd, the board going dark for her, her coming down, her battle, her print. It
    // wins every battle (the foes are left a single HP) and prints what each left behind.
    public void HallMode()
    {
        var story = game.Story;
        var team = game.Party;
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

        // What the next questions are answered with (the first answer once it has run out), who is entered the next
        // time the team is opened for it, what is done at the board the next time it opens (and when the Hall Matron
        // is next), and what is shot as those screens and a battle's first menu come up
        var answers = new Queue<int>();
        var entering = new Queue<int[]>();
        var atBoard = new Queue<Action<HallBoardScreen>>();
        Action<HallBoardScreen>? atMatronsBoard = null;
        string? teamShot = null, enteredShot = null;
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
        void EnterTeam()
        {
            var screen = game.PartyScreen;
            Frames(60);
            if (teamShot != null) { Shot(teamShot); teamShot = null; }
            var slots = entering.Count > 0 ? entering.Dequeue() : Enumerable.Range(0, screen.EnterCount).ToArray();
            foreach (int slot in slots)
            {
                screen.SelectedIndex = slot;
                Frames(2);
                screen.Confirm(team);
                Frames(6);
            }
            if (enteredShot != null) { Shot(enteredShot); enteredShot = null; }
            screen.Confirm(team);
            Frames(4);
        }
        // The board's cursor is taken to a type of its first four rows: along its row, then up or down
        void Point(HallBoardScreen screen, int cell)
        {
            for (int guard = 0; guard < 8 && screen.Cursor >= 16; guard++) screen.Move(0, -1);
            for (int guard = 0; guard < 8 && screen.Cursor % 4 != cell % 4; guard++) screen.Move(1, 0);
            for (int guard = 0; guard < 8 && screen.Cursor / 4 != cell / 4; guard++) screen.Move(0, screen.Cursor / 4 < cell / 4 ? 1 : -1);
        }
        void Choose(HallBoardScreen screen, int cell)
        {
            Point(screen, cell);
            screen.Confirm();
            Frames(6);
            screen.Confirm();
            Frames(6);
            // The Hall Matron's turn: her notice, her place, and yes
            if (screen.Step == HallBoardStep.MatronNotice)
            {
                screen.Confirm();
                Frames(6);
                screen.Confirm();
                Frames(6);
                screen.Confirm();
                Frames(6);
            }
        }
        // The board as it opens: what the mode asked for, or the first type not yet beaten ten times
        void PickAType()
        {
            var screen = game.HallBoard;
            Frames(40);
            Until(() => !PokemonSprites.Busy, "the entered Pokémon's picture", 3600);
            Frames(4);
            if (screen.MatronNext && atMatronsBoard != null) { var show = atMatronsBoard; atMatronsBoard = null; show(screen); }
            else if (atBoard.Count > 0) atBoard.Dequeue()(screen);
            else Choose(screen, Enumerable.Range(0, 16).First(c => !game.Hall!.Beaten(c)));
            Frames(4);
            if (game.Hall is { } hall && !screen.IsActive)
                Console.WriteLine($"  chose {BattleHallChallenge.Board[screen.Outcome]} (wins over it {hall.Ranks[screen.Outcome]}), battle {hall.Battle + 1} of the round, streak {hall.Streak}");
        }
        void ReadOn(Func<bool>? until = null, int most = 9000)
        {
            for (int i = 0; i < most; i++)
            {
                if (until != null && until()) return;
                if (until == null && !engine.ScriptRunning && !Box().IsActive && State() == GameState.Overworld) return;
                if (State() == GameState.Battle) { Win(); continue; }
                if (State() == GameState.PartyMenu) { EnterTeam(); continue; }
                if (State() == GameState.HallBoard) { PickAType(); continue; }
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
        string Told() => $"single {records.Get(FrontierStat.HallLatestStreakSingle)} latest, {records.Get(FrontierStat.HallCurrentStreakSingle)} current "
            + $"({(records.IsRunning(FrontierFacility.Hall, 0) ? "running" : "ended")}), double {records.Get(FrontierStat.HallLatestStreakDouble)} "
            + $"({(records.IsRunning(FrontierFacility.Hall, 1) ? "running" : "ended")}); by species {string.Join(", ", records.Hall.Singles.Select(r => $"{r.Key} {r.Value}"))}"
            + $" / {string.Join(", ", records.Hall.Doubles.Select(r => $"{r.Key} {r.Value}"))}; {records.BattlePoints} BP; victories {records.VictoriesAt(FrontierFacility.Hall)}; "
            + $"print state {story.Var("VAR_BATTLE_HALL_PRINT_STATE")}; lobby action {story.Var("VAR_BATTLE_HALL_LOBBY_LOAD_ACTION")}";
        Pokemon Mon(string species, int level, string? item = null)
        {
            var p = new Pokemon(PokemonDatabase.Get(species)!, level);
            if (item != null) p.HeldItem = ItemDatabase.Get(item);
            game.Pokedex.RegisterSeen(p.Species.DexNumber);
            game.Pokedex.RegisterCaught(p.Species.DexNumber);
            return p;
        }
        const string Lobby = "BattleHall", Stage = "BattleHallStage";

        // The team: one that takes the stage alone, two of a kind for a Double challenge, and one that may not go in
        team.Clear();
        team.Add(Mon("Garchomp", 62, "Leftovers"));
        team.Add(Mon("Bidoof", 32));
        team.Add(Mon("Bidoof", 35));
        team.Add(Mon("Dialga", 70));
        team.Add(Mon("Starly", 12));

        // ---- the Hall from the grounds, and the walk in at its door
        At("BattleFrontier", 28, 35, Direction.Left);
        Frames(40);
        Shot("bh01_hall_from_the_grounds");
        At("BattleFrontier", 25, 35, Direction.Left);
        Frames(10);
        Shot("bh02_hall_porch");
        At("BattleFrontier", 24, 34, Direction.Left);
        engine.Steering = (Direction.Left, false);
        Until(() => Here().Name == Lobby && State() == GameState.Overworld, "the lobby", 400);
        engine.Steering = null;
        Frames(30);
        Shot("bh03_lobby_from_its_door");
        ReadOn();
        Console.WriteLine($"  walked in at the door: on {Here().Name} at ({game.Player.GridX},{game.Player.GridY}), facing {game.Player.Facing}");
        At(Lobby, 16, 9, Direction.Left);
        Frames(8);
        Shot("bh04_lobby_floor");
        At(Lobby, 9, 10, Direction.Left);
        Frames(8);
        Shot("bh05_lobby_counter");
        At(Lobby, 6, 5, Direction.Up);
        Frames(8);
        Shot("bh06_lobby_seats");

        // ---- the records' machine with nothing on it, the record keeper, the second counter, the fan
        Talk(7, 9, Direction.Up);
        Board("bh07_records_nothing_yet");
        Talk(8, 9, Direction.Up);
        Line("keep the Hall's records", "bh08_record_keeper");
        ReadOn();
        Talk(7, 10, Direction.Left);
        Line("linked up", "bh09_multi_counter");
        ReadOn();
        Talk(17, 11, Direction.Right);
        Line("cheer for", "bh0a_the_fan");
        ReadOn();

        // ---- a Single challenge, from the counter to the Battle Points
        Talk(7, 9, Direction.Left);
        Question("Which challenge", "bh10_counter_question");
        teamShot = "bh11_team_to_enter";
        enteredShot = "bh12_one_entered";
        entering.Enqueue(new[] { 0 });
        Question("save it", "bh13_save_question");
        Line("This way", "bh14_this_way");
        Arrived("BattleHallCorridor", "bh15_passage", 40);
        Arrived(Stage, "bh16_stage", 10);
        Line("Which type", "bh17_the_announcer");
        // The board: as it opens, a type under the cursor, its question, the entered Pokémon's place and its summary
        atBoard.Enqueue(screen =>
        {
            Frames(20);
            Shot("bh18_board");
            Point(screen, 6);
            Frames(8);
            Shot("bh19_board_a_type");
            screen.Confirm();
            Frames(20);
            Shot("bh1a_board_question");
            screen.Cancel();
            // Down from the last row of types is the entered Pokémon's place
            Point(screen, 13);
            screen.Move(0, 1);
            Frames(8);
            Shot("bh1b_board_entered");
            screen.Confirm();
            Frames(30);
            Shot("bh1c_board_summary");
            screen.Cancel();
            Frames(4);
            Choose(screen, 0);
        });
        Line("at rank", "bh1d_battle_called");
        var hall = game.Hall!;
        Console.WriteLine($"  the first battle: trainer {FrontierDatabase.Trainer(hall.Trainers[0]).Class} {FrontierDatabase.Trainer(hall.Trainers[0]).Name}, "
            + $"Pokémon no. {hall.Met[0]} {FrontierDatabase.Hall.Pokemon[hall.Met[0]].Species}, level {hall.OpponentsLevel} against {hall.EnteredLevel(team)}");
        Line("takes the stage", "bh1e_trainer_on_the_stage");
        atMenu = fight =>
        {
            Skip(0.6);
            Shot("bh20_battle_menu");
            Console.WriteLine($"  the battle: {string.Join(", ", fight.PlayerParty.Members.Select(p => $"{p.Species.Name} Lv{p.Level} ({p.HeldItem?.Name ?? "nothing"})"))} against "
                + $"{string.Join(", ", fight.EnemyParty.Members.Select(p => $"{p.Species.Name} Lv{p.Level} (IV {p.IvAttack}, {p.Nature})"))}; frontier {fight.IsFrontier}");
        };
        ReadOn(() => State() == GameState.Battle);
        Skip(1.6);
        Shot("bh1f_battle_begins");
        Line("restored", "bh21_restored");
        Question("Are you ready", "bh22_next_battle_question");
        // The second battle: the board with a win over Normal on it, and another type taken
        atBoard.Enqueue(screen =>
        {
            Frames(20);
            Shot("bh23_board_a_rank_won");
            Choose(screen, 1);
        });
        Line("at rank", "bh24_another_type");
        // The rest of the round goes to the first type not yet beaten ten times
        Line("Ten battles", "bh25_ten_won");
        Line("received", "bh26_battle_points");
        Console.WriteLine($"  after the round: {Told()}; ranks {string.Join(",", hall.Ranks.Take(17))}");
        Line("all ten", "bh28_lobby_congratulations");
        ReadOn();
        Console.WriteLine($"  back in the lobby: challenge {(game.Hall == null ? "let go of" : "still held")}; {Told()}");
        Talk(7, 9, Direction.Up);
        Board("bh29_records_after_a_round");
        Talk(8, 9, Direction.Up);
        Line("add up to", "bh2a_keeper_counts");
        Line("received", "bh2b_keeper_points");
        ReadOn();
        Talk(17, 11, Direction.Right);
        Line("record", "bh2c_the_fan_afterwards");
        ReadOn();
        Console.WriteLine($"  the record keeper: {Told()}; next mark {story.Var(BattleHallChallenge.MilestoneVariable)}");

        // ---- the second round: a battle, and the challenge put down and found waiting
        Talk(7, 9, Direction.Left);
        entering.Enqueue(new[] { 0 });
        answers.Enqueue(0);
        answers.Enqueue(0);
        answers.Enqueue(1);
        Question("Are you ready", "bh30_rest_chosen");
        Question("Put the challenge down", "bh31_rest_question");
        answers.Enqueue(0);
        answers.Enqueue(1);
        Question("go on with it now", "bh33_waiting_question");
        ReadOn();
        Console.WriteLine($"  put down: {(records.Hall.Paused is { } p ? $"battle {p.Battle}, {p.Species}" : "nothing")}; {Told()}");
        // Taken up again, and given up
        Talk(7, 9, Direction.Left);
        answers.Enqueue(0);
        answers.Enqueue(2);
        Question("Are you ready", "bh34_taken_up_again");
        Question("Give the challenge up", "bh35_retire_question");
        Line("Thank you for taking", "bh36_lobby_given_up");
        ReadOn();
        Console.WriteLine($"  given up: {Told()}");

        // ---- a Double challenge: two of a kind, and each trainer with the same Pokémon twice
        Talk(7, 9, Direction.Left);
        answers.Enqueue(1);
        teamShot = "bh40_team_for_two";
        enteredShot = "bh41_two_of_a_kind";
        entering.Enqueue(new[] { 1, 2 });
        atBoard.Enqueue(screen =>
        {
            Frames(20);
            Shot("bh42_board_for_two");
            Choose(screen, 4);
        });
        Line("at rank", "bh43_double_battle_called");
        atMenu = fight =>
        {
            Skip(0.6);
            Shot("bh45_double_battle_menu");
            Console.WriteLine($"  the double battle: {string.Join(", ", fight.PlayerParty.Members.Select(q => $"{q.Species.Name} Lv{q.Level}"))} against "
                + $"{string.Join(", ", fight.EnemyParty.Members.Select(q => $"{q.Species.Name} Lv{q.Level} ({q.Nature}, personality {q.Personality:X8})"))}");
        };
        ReadOn(() => State() == GameState.Battle);
        Skip(1.6);
        Shot("bh44_double_battle");
        answers.Enqueue(2);
        Question("Are you ready", "bh46_double_between_battles");
        Line("Thank you for taking", "bh47_double_given_up");
        ReadOn();
        Console.WriteLine($"  after the double challenge: {Told()}");

        // ---- the Hall Matron: a streak of forty taken up, its fifth round's tenth battle
        records.Set(FrontierStat.HallLatestStreakSingle, 40);
        records.Set(FrontierStat.HallCurrentStreakSingle, 40);
        records.Set(FrontierStat.HallLatestSpeciesSingle, PokemonDatabase.Get("Garchomp")!.DexNumber);
        records.SetRunning(FrontierFacility.Hall, 0, true);
        for (int cell = 0; cell < 4; cell++) BattleHallChallenge.SaveRank(records, FrontierChallenge.Single, cell, 10);
        Talk(7, 9, Direction.Left);
        entering.Enqueue(new[] { 0 });
        Arrived(Stage, "bh50_stage_bigger_crowd", 10);
        atBoard.Enqueue(screen =>
        {
            Frames(20);
            Shot("bh51_board_types_beaten");
            Choose(screen, 4);
        });
        atMatronsBoard = screen =>
        {
            Point(screen, 14);
            screen.Confirm();
            Frames(6);
            screen.Confirm();
            Frames(24);
            Shot("bh52_board_matron_notice");
            screen.Confirm();
            Frames(24);
            Shot("bh53_board_her_place");
            screen.Confirm();
            Frames(20);
            Shot("bh54_board_matron_question");
            screen.Confirm();
            Frames(6);
        };
        ReadOn(() => Here().Name == Stage && game.Hall is { Battle: 9 } && State() == GameState.HallBoard);
        Line("look who is coming", "bh55_argenta_announced");
        Line("I am Argenta", "bh56_argenta");
        atMenu = fight =>
        {
            Skip(0.6);
            Shot("bh58_argenta_battle_menu");
            Console.WriteLine($"  Argenta's Pokémon: {string.Join(", ", fight.EnemyParty.Members.Select(q => $"{q.Species.Name} Lv{q.Level} ({q.HeldItem?.Name}, IV {q.IvAttack})"))}");
        };
        ReadOn(() => State() == GameState.Battle);
        Skip(1.6);
        Shot("bh57_argenta_battle");
        Line("bravo", "bh59_argenta_beaten");
        Line("received", "bh5a_twenty_points");
        Line("Silver Print", "bh5b_silver_print");
        ReadOn();
        Console.WriteLine($"  after the Hall Matron: {Told()}; brains met {records.BrainsMet}");
        Talk(7, 9, Direction.Up);
        Board("bh5c_records_with_a_print");
    }
}
