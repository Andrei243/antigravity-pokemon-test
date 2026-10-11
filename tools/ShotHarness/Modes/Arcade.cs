using PokemonPlatinumEngine.Models.Frontier;

partial class Harness
{
    // ---------------------------------------------------------------- the Battle Arcade (plan 06 · R18d)

    // The Battle Arcade from the Frontier's grounds to its Battle Points (ba*; not part of `all`), through the game's
    // own steps, TryInteract, ArriveOnMap, scripts, question boxes, team screen, game board and battles: the Arcade
    // from outside and its door, the lobby from its mat, its floor, its counter and its lounge, the records' board
    // with nothing on it, the second counter and one of the lobby's people; the first counter's questions, the three
    // entered, the corridor, the game room, the host's welcome, a trainer walking in; the game board coming up,
    // counting down, with its events shown and the cursor running, and stopped; the event announced, the battle
    // under it, the attendant restoring the three, the question between games; the events of a first round one after
    // another (a status, a berry lent, a faster cursor, a random one), the seventh game won, its Battle Points, the
    // lobby's congratulations and the board of records afterwards; a challenge put down, found waiting, taken up
    // again and given up; a fifth round's board with its bonuses, a battle fought under a sandstorm, the teams
    // swapped, a Battle Point and a battle passed; and the Arcade Star at the twenty-first game with her print. It
    // wins every battle (the foes are left a single HP and whoever leads is kept on its feet), stops the board's
    // cursor by the frame, shows an event it wants by rigging the board's panels (ArcadeBoard.Rig: the cursor is still
    // stopped by the board's own rules), and prints what each left behind.
    public void ArcadeMode()
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

        // What the next questions are answered with (the first answer once it has run out), what the next boards are
        // made to show and what is done as one comes up, and what is shot as the team's screen and a battle's first
        // menu come up
        var answers = new Queue<int>();
        var rig = new Queue<ArcadeEffect>();
        Action<ArcadeBoardScreen>? atBoard = null;
        string? teamShot = null, enteredShot = null;
        Action<BattleEngine>? atMenu = null;
        bool sure = false;

        // Only the foes are brought to a single HP; whoever leads is kept on its feet, with whatever the board did to it
        // (and whole and well for the battle the mode must win to show what follows it, the Arcade Star's)
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
                    if (lead.CurrentHP < lead.MaxHP / 2) lead.CurrentHP = lead.MaxHP / 2;
                    // In that battle the foes are left nothing but Struggle as well: a Medicham brought to a single HP
                    // is faster than the three and its Reversal fells each of them in one blow
                    if (sure)
                    {
                        lead.CurrentHP = lead.MaxHP;
                        lead.Status = StatusCondition.None;
                        foreach (var foe in fight.EnemyParty.Members) foreach (var move in foe.Moves) move.CurrentPP = 0;
                    }
                    var target = fight.EnemyPokemon;
                    int pick = lead.Moves.FindIndex(m => m.CurrentPP > 0 && m.Data.Category != MoveCategory.Status
                        && TypeChart.GetEffectiveness(m.Data.Type, target.PrimaryType, target.SecondaryType) > 0f);
                    if (pick < 0) { foreach (var move in lead.Moves) move.CurrentPP = move.MaxPP; pick = 0; }
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
            Console.WriteLine($"  the battle: {fight.Result} in {fight.Core.Turn} turns; the three afterwards: {string.Join(", ", fight.PlayerParty.Members.Select(p => $"{p.Species.Name} {p.CurrentHP}/{p.MaxHP} {p.Status} ({p.HeldItem?.Name ?? "nothing"})"))}");
            if (fight.Result != BattleResult.PlayerVictory)
                Console.WriteLine("  !! the battle was not won: " + string.Join(" | ", fight.Core.Log.OfType<Said>().Select(s => s.Text)));
            Until(() => State() != GameState.Battle && State() != GameState.Transition, "the field again", 1500);
            Frames(6);
        }
        void EnterTeam()
        {
            var screen = game.PartyScreen;
            Frames(60);
            if (teamShot != null) { Shot(teamShot); teamShot = null; }
            foreach (int slot in Enumerable.Range(0, screen.EnterCount).ToArray())
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
        // The game board as it comes up: made to show what the mode asked for, looked at if the mode wants to, and its
        // cursor stopped as soon as it runs
        void PlayBoard()
        {
            var screen = game.ArcadeBoard;
            var board = screen.Board!;
            if (rig.Count > 0) board.Rig(rig.Dequeue());
            if (atBoard != null) { var look = atBoard; atBoard = null; look(screen); }
            Until(() => board.Step >= ArcadeBoardStep.Running || !screen.IsActive, "the cursor running", 400);
            Frames(6);
            screen.PressStop();
            Until(() => !screen.IsActive, "the board's end", 200);
            Console.WriteLine($"  the board: round {board.Round + 1}, streak {board.Streak}, last battle's score {board.Performance} (row {board.Tier}), speed {board.Speed}{(board.Randomized ? ", random" : "")}; "
                + $"panels {string.Join(" ", board.Panels.Select(e => (int)e))}; stopped on panel {board.Cursor} after {board.Frame} frames: {board.Effect} (number {board.RandomIndex})");
            Until(() => State() != GameState.ArcadeBoard, "the room again", 60);
        }
        // Both threes' pictures are asked for and waited on before a board is looked at: its time runs while it is up
        void Pictures()
        {
            if (game.Arcade is not { } arcade) return;
            foreach (var p in arcade.Team.Members.Concat(arcade.Foe?.Pokemon ?? Enumerable.Empty<Pokemon>()))
            {
                PokemonModels.Request(p.ModelName);
                PokemonSprites.Request(p.ModelName);
            }
            long before = tick;
            Until(() => !PokemonSprites.Busy, "the board's pictures", 3600);
            // How long the bakers take is theirs to say, and the clock must not go by it: the wait is made up to a whole
            // three seconds, so two runs with the cache warm stand at the same frame afterwards and draw the same pictures
            while ((tick - before) % 180 != 0) Tick();
        }
        void ReadOn(Func<bool>? until = null, int most = 9000)
        {
            for (int i = 0; i < most; i++)
            {
                if (until != null && until()) return;
                if (until == null && !engine.ScriptRunning && !Box().IsActive && State() == GameState.Overworld) return;
                if (State() == GameState.Battle) { Win(); continue; }
                if (State() == GameState.PartyMenu) { EnterTeam(); continue; }
                if (State() == GameState.ArcadeBoard) { PlayBoard(); continue; }
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
        bool Says(string word) => Box().IsActive && Box().CurrentLine.Contains(word);
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
        string Told() => $"single {records.Get(BattleArcadeChallenge.LatestStreakStat(FrontierChallenge.Single))}/{records.Get(BattleArcadeChallenge.RecordStreakStat(FrontierChallenge.Single))} "
            + $"({(records.IsRunning(FrontierFacility.Arcade, 0) ? "running" : "ended")}); {records.BattlePoints} BP ({records.Arcade.BoardPoints} from the board); "
            + $"victories {records.VictoriesAt(FrontierFacility.Arcade)}; print state {story.Var("VAR_BATTLE_ARCADE_PRINT_STATE")}; lobby action {story.Var("VAR_BATTLE_ARCADE_LOBBY_LOAD_ACTION")}";
        string Three(IEnumerable<Pokemon> three) => string.Join(", ", three.Select(p => $"{p.Species.Name} Lv{p.Level} {p.CurrentHP}/{p.MaxHP} {p.Status} ({p.HeldItem?.Name ?? "nothing"})"));
        Pokemon Mon(string species, int level, string? item = null)
        {
            var p = new Pokemon(PokemonDatabase.Get(species)!, level);
            if (item != null) p.HeldItem = ItemDatabase.Get(item);
            game.Pokedex.RegisterSeen(p.Species.DexNumber);
            game.Pokedex.RegisterCaught(p.Species.DexNumber);
            return p;
        }
        // A streak that is still running at so many wins, for a round further on
        void Streak(int wins)
        {
            records.Set(BattleArcadeChallenge.LatestStreakStat(FrontierChallenge.Single), wins);
            records.SetIfBetter(BattleArcadeChallenge.RecordStreakStat(FrontierChallenge.Single), wins);
            records.SetRunning(FrontierFacility.Arcade, 0, true);
        }
        const string Lobby = "BattleArcade", Corridor = "BattleArcadeCorridor", Room = "BattleArcadeBattleRoom";

        // The team: three of different kinds, one over level 50, each holding something that is kept at the door
        team.Clear();
        team.Add(Mon("Garchomp", 62, "Leftovers"));
        team.Add(Mon("Luxray", 50, "Sitrus Berry"));
        team.Add(Mon("Staraptor", 47, "Quick Claw"));
        team.Add(Mon("Bidoof", 9));

        // ---- the Arcade from the grounds, and the walk in at its door
        At("BattleFrontier", 55, 63, Direction.Right);
        Frames(40);
        Shot("ba01_arcade_from_the_grounds");
        At("BattleFrontier", 58, 63, Direction.Right);
        Frames(10);
        Shot("ba02_arcade_porch");
        At("BattleFrontier", 59, 62, Direction.Right);
        engine.Steering = (Direction.Right, false);
        Until(() => Here().Name == Lobby && State() == GameState.Overworld, "the lobby", 400);
        engine.Steering = null;
        Frames(30);
        Shot("ba03_lobby_from_its_door");
        ReadOn();
        Console.WriteLine($"  walked in at the door: on {Here().Name} at ({game.Player.GridX},{game.Player.GridY}), facing {game.Player.Facing}");
        At(Lobby, 9, 6, Direction.Right);
        Frames(8);
        Shot("ba04_lobby_floor");
        At(Lobby, 15, 7, Direction.Up);
        Frames(8);
        Shot("ba05_lobby_counter");
        At(Lobby, 10, 12, Direction.Down);
        Frames(8);
        Shot("ba06_lobby_lounge");

        // ---- the records' machine with nothing on it, the second counter, somebody of the lobby
        Talk(16, 6, Direction.Up);
        Board("ba07_records_nothing_yet");
        Talk(15, 6, Direction.Up);
        Line("linked up", "ba08_multi_counter");
        ReadOn();
        Talk(12, 9, Direction.Right);
        Line("Ground types", "ba09_a_hiker");
        ReadOn();

        // ---- a Single challenge, from the counter to the Battle Points
        Talk(14, 6, Direction.Up);
        Question("Which challenge", "ba10_counter_question");
        teamShot = "ba11_team_to_enter";
        enteredShot = "ba12_three_entered";
        Question("save it", "ba13_save_question");
        Line("this way", "ba14_this_way");
        ReadOn(() => Here().Name == Corridor && State() == GameState.Overworld);
        Frames(34);
        Shot("ba15_corridor");
        Arrived(Room, "ba16_game_room", 10);
        var arcade = game.Arcade!;
        Console.WriteLine($"  the round's trainers: {string.Join(", ", arcade.Trainers.Take(7).Select(t => FrontierDatabase.Trainer(t).Class + " " + FrontierDatabase.Trainer(t).Name))}");
        Console.WriteLine($"  the three as they went in: {Three(arcade.Team.Members)}; the team's own: {Three(team.Members.Take(3))}");
        Console.WriteLine($"  the first trainer's three: {Three(arcade.Foe!.Pokemon)}");
        Line("anything can happen", "ba17_hosts_welcome");
        Line("Game no. 1", "ba18_game_one");
        Pictures();
        Line("game board going", "ba19_board_announced");


        // A board looked at: once its cursor runs, after so many more frames; and stopped, with every panel showing its event
        void ToCursor(ArcadeBoardScreen screen) => Until(() => screen.Board!.Step >= ArcadeBoardStep.Running, "the cursor", 300);
        void Shown(ArcadeBoardScreen screen, string shot, int frames)
        {
            ToCursor(screen);
            Frames(frames);
            Shot(shot);
        }
        void Stopped(ArcadeBoardScreen screen, string shot)
        {
            ToCursor(screen);
            Frames(6);
            screen.PressStop();
            Until(() => screen.Board!.Step >= ArcadeBoardStep.Stopped, "the stop", 20);
            Frames(8);
            Shot(shot);
        }

        // The first board, as its own draw laid it out: coming up, counting, running, stopped
        atBoard = screen =>
        {
            var board = screen.Board!;
            Frames(8);
            Shot("ba20_board_coming_up");
            Until(() => board.Step == ArcadeBoardStep.Three, "the count", 200);
            Frames(8);
            Shot("ba21_board_three");
            Until(() => board.Step == ArcadeBoardStep.One, "the count's end", 200);
            Frames(8);
            Shot("ba22_board_one");
            Shown(screen, "ba23_board_events", 4);
            Frames(36);
            Shot("ba24_board_cursor_on");
            screen.PressStop();
            Until(() => board.Step >= ArcadeBoardStep.Stopped, "the stop", 20);
            Frames(8);
            Shot("ba25_board_stopped");
        };
        Line("game board says", "ba26_event_announced");
        ReadOn(() => Says("battle begin"));
        Console.WriteLine($"  the first event: {arcade.Effect}; given {arcade.Given ?? "nothing"}; kept from it {arcade.Immune}; speed {arcade.Speed}; random next {arcade.Randomized}; conditions {arcade.Conditions}");
        Console.WriteLine($"  the three before the battle: {Three(arcade.Team.Members)}; the trainer's: {Three(arcade.Foe!.Pokemon)}");
        Line("battle begin", "ba27_let_the_battle_begin");
        atMenu = fight =>
        {
            Skip(0.6);
            Shot("ba29_battle_menu");
            Console.WriteLine($"  the battle: {Three(fight.PlayerParty.Members)} against {Three(fight.EnemyParty.Members)}; frontier {fight.IsFrontier}; weather {fight.Core.Field.Weather}");
        };
        ReadOn(() => State() == GameState.Battle);
        Skip(1.6);
        Shot("ba28_battle_begins");
        Line("see to your Pok", "ba2a_restored");
        Console.WriteLine($"  after the first battle: score {arcade.Performance}; {Three(arcade.Team.Members)}; {Told()}");
        Question("Are you ready", "ba2b_next_game_question");

        // The round's other six: poison for the trainer's three, a berry lent, a faster cursor, a random one, and nothing twice
        foreach (var effect in new[] { ArcadeEffect.PoisonFoe, ArcadeEffect.AllyGetBerry, ArcadeEffect.SpeedUp, ArcadeEffect.RandomizeCursor, ArcadeEffect.NoEvent, ArcadeEffect.NoEvent }) rig.Enqueue(effect);
        Line("Game no. 2", "ba30_game_two");
        Pictures();
        atBoard = screen => Stopped(screen, "ba31_board_poison");
        Line("poisoned", "ba32_poisoned");
        atMenu = fight =>
        {
            Skip(0.6);
            Shot("ba33_battle_poisoned_foe");
            Console.WriteLine($"  the second battle: against {Three(fight.EnemyParty.Members)}");
        };
        Line("Game no. 3", "ba34_game_three");
        Line("lent a", "ba35_berry_lent");
        Console.WriteLine($"  after the berry: {Three(arcade.Team.Members)}");
        Line("Game no. 4", "ba36_game_four");
        Pictures();
        atBoard = screen => Shown(screen, "ba37_board_with_a_berry_held", 10);
        Line("goes faster", "ba38_faster");
        Line("Game no. 5", "ba39_game_five");
        Line("wherever it pleases", "ba3a_random");
        Line("Game no. 6", "ba3b_game_six");
        Pictures();
        atBoard = screen => Shown(screen, "ba3c_board_fast_and_random", 15);
        Line("Nothing happens", "ba3d_nothing");
        Line("seventh win", "ba40_seven_won");
        Line("received", "ba41_battle_points");
        Console.WriteLine($"  after the round: {Told()}");
        Line("seven wins", "ba43_lobby_congratulations");
        ReadOn();
        Console.WriteLine($"  back in the lobby: challenge {(game.Arcade == null ? "let go of" : "still held")}; the team's own {Three(team.Members.Take(3))}; {Told()}");
        Talk(16, 6, Direction.Up);
        Board("ba44_records_after_a_round");

        // ---- the second round: a game under a sandstorm, and the challenge put down and found waiting
        rig.Enqueue(ArcadeEffect.Sandy);
        Talk(14, 6, Direction.Up);
        Line("raging sandstorm", "ba50_sandstorm");
        atMenu = fight =>
        {
            Skip(0.6);
            Shot("ba50b_battle_in_a_sandstorm");
            Console.WriteLine($"  the sandstorm's battle: weather {fight.Core.Field.Weather}, lasting {fight.Core.Field.WeatherLasts}");
        };
        answers.Enqueue(1);
        Question("Are you ready", "ba51_rest_chosen");
        Question("take a break", "ba52_rest_question");
        answers.Enqueue(0);
        answers.Enqueue(1);
        Question("go on with it now", "ba54_waiting_question");
        ReadOn();
        Console.WriteLine($"  put down: {(records.Arcade.Paused is { } p ? $"game {p.Battle}, speed {p.Speed}, items {string.Join("/", p.Items.Select(k => k ?? "-"))}" : "nothing")}; {Told()}");
        // Taken up again, and given up
        Talk(14, 6, Direction.Up);
        answers.Enqueue(0);
        answers.Enqueue(2);
        Question("Are you ready", "ba55_taken_up_again");
        Console.WriteLine($"  taken up: game {game.Arcade!.Battle}, {Three(game.Arcade.Team.Members)}");
        Question("Give the challenge up", "ba56_retire_question");
        Line("Thank you for playing.", "ba57_lobby_given_up");
        ReadOn();
        Console.WriteLine($"  given up: {Told()}");

        // ---- the fifth round: the bonuses. Its first board as drawn, then the teams swapped, a Battle Point and a battle passed
        Streak(28);
        Talk(14, 6, Direction.Up);
        ReadOn(() => Here().Name == Room && State() == GameState.Overworld && game.Arcade != null);
        arcade = game.Arcade!;
        Line("Game no. 1", "ba60_fifth_round");
        Pictures();
        atBoard = screen => Shown(screen, "ba61_board_fifth_round", 6);
        ReadOn(() => Says("battle begin"));
        Console.WriteLine($"  the fifth round's first event: {arcade.Effect}; the three {Three(arcade.Team.Members)}; the trainer's {Three(arcade.Foe!.Pokemon)}");
        foreach (var effect in new[] { ArcadeEffect.SwapMons, ArcadeEffect.Get1Bp, ArcadeEffect.NoBattle }) rig.Enqueue(effect);
        Line("Game no. 2", "ba62_game_two");
        Pictures();
        atBoard = screen => Stopped(screen, "ba63_board_swap");
        Line("change sides", "ba64_swap");
        atMenu = fight =>
        {
            Skip(0.6);
            Shot("ba65_battle_swapped");
            Console.WriteLine($"  the swapped battle: {Three(fight.PlayerParty.Members)} against {Three(fight.EnemyParty.Members)}");
        };
        Line("Game no. 3", "ba66_game_three");
        Line("received 1 BP", "ba67_a_battle_point");
        Console.WriteLine($"  after the Battle Point: {Told()}");
        Line("Game no. 4", "ba68_game_four");
        Line("is passed", "ba69_battle_passed");
        // After the pass the challenge is given up, to get on to the Arcade Star
        answers.Enqueue(2);
        Question("Are you ready", "ba6a_after_the_pass");
        Console.WriteLine($"  after the pass: game {arcade.Battle}, streak {arcade.Streak}; {Told()}");
        Line("Thank you for playing.", "ba6b_given_up_again");
        ReadOn();
        Console.WriteLine($"  after the fifth round's four: {Told()}");

        // ---- the Arcade Star: a streak of fourteen taken up, its third round's seventh game
        Streak(14);
        for (int i = 0; i < 7; i++) rig.Enqueue(ArcadeEffect.NoEvent);
        Talk(14, 6, Direction.Up);
        Line("Arcade Star has been watching", "ba70_dahlia_announced");
        Line("Listen to that crowd", "ba71_the_crowd");
        ReadOn(() => Here().NPCs.Any(n => n.Key == "opponent" && n.GridY == 13));
        Frames(6);
        Shot("ba72_dahlia_comes_in");
        Line("I'm Dahlia", "ba73_dahlia");
        sure = true;
        atMenu = fight =>
        {
            Skip(0.6);
            Shot("ba75_dahlia_battle_menu");
            Console.WriteLine($"  Dahlia's three: {Three(fight.EnemyParty.Members)}");
        };
        ReadOn(() => State() == GameState.Battle);
        Skip(1.6);
        Shot("ba74_dahlia_battle");
        Line("lovely win", "ba76_dahlia_beaten");
        Line("received 20 BP", "ba77_twenty_points");
        Line("Silver Print", "ba78_silver_print");
        ReadOn();
        Console.WriteLine($"  after the Arcade Star: {Told()}; brains met {records.BrainsMet}");
        Talk(16, 6, Direction.Up);
        Board("ba79_records_with_a_print");
    }
}
