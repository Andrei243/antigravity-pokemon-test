using PokemonPlatinumEngine.Models.Frontier;

partial class Harness
{
    // ---------------------------------------------------------------- the Battle Castle (plan 06 · R18c)

    // The Battle Castle from the Frontier's grounds to its Battle Points (bk*; not part of `all`), through the game's
    // own steps, TryInteract, ArriveOnMap, scripts, question boxes, team screen, valet's counters and battles: the
    // Castle from outside and its door, the lobby from its mat, its floor and counter, the records' board with
    // nothing on it, the second counter and one of the lobby's people; the first counter's questions, the three
    // entered, the valet's room, his welcome and his services with the Castle Points over the field, the throne
    // room, a trainer coming forward, the battle, the Castle Points it earned; the counter for one's own three (a
    // Pokémon's menu, the healing counter, its question, HP restored, a rank bought, the berries' list, a berry
    // rented, a summary) and the one about the trainer (one identified, its level raised, its stats told); the
    // seventh won, its Battle Points, the lobby's congratulations and the board afterwards; a challenge put down,
    // found waiting, taken up again and given up; a battle passed in the fourth round; and the Castle Valet at the
    // twenty-first battle with his print. It wins every battle (the foes are left a single HP, and the three are
    // left as the battle left them, but in the Castle Valet's, where whoever leads is kept whole) and prints what each
    // left behind.
    public void CastleMode()
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

        // What the next questions are answered with (the first answer once it has run out), what is done at a counter
        // the next time it opens, and what is shot as the team's screen and a battle's first menu come up
        var answers = new Queue<int>();
        var atCounter = new Queue<Action<CastleScreen>>();
        string? teamShot = null, enteredShot = null;
        Action<BattleEngine>? atMenu = null;
        bool sure = false;

        // The three are left as the battle leaves them, since that is the Castle's own rule: only the foes are brought
        // to a single HP, and whoever leads strikes with a move that reaches them
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
                    // ... but for the last one standing, which is kept on its feet so that the mode reaches its end, and
                    // for the battle the mode must win to show what follows it (the Castle Valet's)
                    if (fight.PlayerParty.Members.Count(p => !p.IsFainted) == 1 && lead.CurrentHP < lead.MaxHP / 3) lead.CurrentHP = lead.MaxHP / 2;
                    if (sure) { lead.CurrentHP = lead.MaxHP; lead.Status = StatusCondition.None; }
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
            Console.WriteLine($"  the battle: {fight.Result}; the three afterwards: {string.Join(", ", fight.PlayerParty.Members.Select(p => $"{p.Species.Name} {p.CurrentHP}/{p.MaxHP}"))}");
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
        // A counter as it opens: what the mode asked for, or straight out again
        void Counter()
        {
            var screen = game.CastleCounter;
            Frames(40);
            Until(() => !PokemonSprites.Busy, "the counter's pictures", 3600);
            Frames(4);
            if (atCounter.Count > 0) atCounter.Dequeue()(screen);
            for (int guard = 0; guard < 12 && screen.IsActive; guard++) screen.Cancel();
            Frames(4);
        }
        void ReadOn(Func<bool>? until = null, int most = 9000)
        {
            for (int i = 0; i < most; i++)
            {
                if (until != null && until()) return;
                if (until == null && !engine.ScriptRunning && !Box().IsActive && State() == GameState.Overworld) return;
                if (State() == GameState.Battle) { Win(); continue; }
                if (State() == GameState.PartyMenu) { EnterTeam(); continue; }
                if (State() == GameState.CastleCounter) { Counter(); continue; }
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
        string Told() => $"single {records.Get(BattleCastleChallenge.LatestStreakStat(FrontierChallenge.Single))}/{records.Get(FrontierStat.CastleRecordStreakSingle)} "
            + $"({(records.IsRunning(FrontierFacility.Castle, 0) ? "running" : "ended")}), {records.Get(BattleCastleChallenge.LatestPointsStat(FrontierChallenge.Single))} CP "
            + $"(record {records.Get(BattleCastleChallenge.RecordPointsStat(FrontierChallenge.Single))}, spent {records.Get(BattleCastleChallenge.SpentPointsStat(FrontierChallenge.Single))}), "
            + $"ranks {string.Join("/", Enum.GetValues<CastleRank>().Select(r => records.Get(BattleCastleChallenge.RankStat(FrontierChallenge.Single, r))))}; "
            + $"{records.BattlePoints} BP; victories {records.VictoriesAt(FrontierFacility.Castle)}; print state {story.Var("VAR_BATTLE_CASTLE_PRINT_STATE")}; "
            + $"lobby action {story.Var("VAR_BATTLE_CASTLE_LOBBY_LOAD_ACTION")}";
        string Three(IEnumerable<Pokemon> three) => string.Join(", ", three.Select(p => $"{p.Species.Name} Lv{p.Level} {p.CurrentHP}/{p.MaxHP} ({p.HeldItem?.Name ?? "nothing"})"));
        Pokemon Mon(string species, int level, string? item = null)
        {
            var p = new Pokemon(PokemonDatabase.Get(species)!, level);
            if (item != null) p.HeldItem = ItemDatabase.Get(item);
            game.Pokedex.RegisterSeen(p.Species.DexNumber);
            game.Pokedex.RegisterCaught(p.Species.DexNumber);
            return p;
        }
        // A menu's cursor is taken to one of its options
        void Pick(CastleScreen screen, CastleOption option)
        {
            for (int guard = 0; guard < 8 && screen.Options[screen.MenuCursor] != option; guard++) screen.Move(0, 1);
            screen.Confirm();
        }
        const string Lobby = "BattleCastle", Corridor = "BattleCastleCorridor", Throne = "BattleCastleBattleRoom";

        // The team: three of different kinds, one over level 50, each holding something that is kept at the door
        team.Clear();
        team.Add(Mon("Garchomp", 62, "Leftovers"));
        team.Add(Mon("Luxray", 50, "Sitrus Berry"));
        team.Add(Mon("Staraptor", 47, "Quick Claw"));
        team.Add(Mon("Bidoof", 9));

        // ---- the Castle from the grounds, and the walk in at its door
        At("BattleFrontier", 41, 63, Direction.Left);
        Frames(40);
        Shot("bk01_castle_from_the_grounds");
        At("BattleFrontier", 38, 63, Direction.Left);
        Frames(10);
        Shot("bk02_castle_porch");
        At("BattleFrontier", 37, 62, Direction.Left);
        engine.Steering = (Direction.Left, false);
        Until(() => Here().Name == Lobby && State() == GameState.Overworld, "the lobby", 400);
        engine.Steering = null;
        Frames(30);
        Shot("bk03_lobby_from_its_door");
        ReadOn();
        Console.WriteLine($"  walked in at the door: on {Here().Name} at ({game.Player.GridX},{game.Player.GridY}), facing {game.Player.Facing}");
        At(Lobby, 13, 9, Direction.Left);
        Frames(8);
        Shot("bk04_lobby_floor");
        At(Lobby, 8, 10, Direction.Left);
        Frames(8);
        Shot("bk05_lobby_counter");
        At(Lobby, 12, 13, Direction.Down);
        Frames(8);
        Shot("bk06_lobby_south");

        // ---- the records' machine with nothing on it, the second counter, somebody of the lobby
        Talk(6, 9, Direction.Up);
        Board("bk07_records_nothing_yet");
        Talk(6, 10, Direction.Left);
        Line("linked up", "bk08_multi_counter");
        ReadOn();

        // ---- a Single challenge, from the counter to the Battle Points
        Talk(6, 9, Direction.Left);
        Question("Which challenge", "bk10_counter_question");
        teamShot = "bk11_team_to_enter";
        enteredShot = "bk12_three_entered";
        Question("save it", "bk13_save_question");
        Line("This way", "bk14_this_way");
        Arrived(Corridor, "bk15_valets_room", 14);
        var castle = game.Castle!;
        Console.WriteLine($"  the round's trainers: {string.Join(", ", castle.Trainers.Take(7).Select(t => FrontierDatabase.Trainer(t).Class + " " + FrontierDatabase.Trainer(t).Name))}");
        Console.WriteLine($"  the three as they went in: {Three(castle.Team.Members)}; the team's own: {Three(team.Members.Take(3))}");
        Console.WriteLine($"  the first trainer's three: {Three(castle.Foe!.Pokemon)}");
        Line("kept at the door", "bk16_valets_welcome");
        Question("Castle Points.", "bk17_valets_services");
        Line("go up", "bk18_go_up");
        Arrived(Throne, "bk19_throne_room", 10);
        Line("comes forward", "bk1a_a_trainer_comes_forward");
        atMenu = fight =>
        {
            Skip(0.6);
            Shot("bk1c_battle_menu");
            Console.WriteLine($"  the battle: {Three(fight.PlayerParty.Members)} against {Three(fight.EnemyParty.Members)}; frontier {fight.IsFrontier}");
        };
        ReadOn(() => State() == GameState.Battle);
        Skip(1.6);
        Shot("bk1b_battle_begins");
        ReadOn(() => Here().Name == Throne && State() != GameState.Battle && State() != GameState.Transition && Box().IsActive);
        Line("Castle Points", "bk1d_her_ladyships_points");
        Line("received", "bk1e_castle_points_earned");
        Console.WriteLine($"  after the first battle: {Three(castle.Team.Members)}; {Told()}");
        Arrived(Corridor, "bk1f_back_with_the_valet", 30);
        Question("Are you ready", "bk20_next_battle_question");

        // The counter for one's own three: a Pokémon's menu, the healing counter, its question, HP restored; a rank
        // that the points don't reach; the berries, one rented; the summary
        answers.Enqueue(0);
        answers.Enqueue(1);
        castle.Team.Members[0].CurrentHP = Math.Max(1, castle.Team.Members[0].MaxHP / 3);
        atCounter.Enqueue(screen =>
        {
            Frames(20);
            Shot("bk21_counter_your_three");
            screen.Confirm();
            Frames(10);
            Shot("bk22_a_pokemons_menu");
            Pick(screen, CastleOption.Heal);
            Frames(10);
            Shot("bk23_healing_counter");
            Pick(screen, CastleOption.RestoreHp);
            Frames(16);
            Shot("bk24_healing_question");
            screen.Confirm();
            Frames(16);
            Shot("bk25_hp_restored");
            screen.Confirm();
            Pick(screen, CastleOption.RestorePp);
            Frames(16);
            Shot("bk26_rank_too_low");
            screen.Confirm();
            screen.Cancel();
            Pick(screen, CastleOption.Rental);
            Frames(10);
            Shot("bk27_rental_counter");
            Pick(screen, CastleOption.Berries);
            Frames(10);
            screen.Move(0, 1);
            screen.Move(0, 1);
            Frames(6);
            Shot("bk28_berries");
            screen.Confirm();
            Frames(16);
            Shot("bk29_rental_question");
            screen.Confirm();
            Frames(16);
            Shot("bk2a_berry_rented");
            screen.Confirm();
            screen.Cancel();
            screen.Cancel();
            Pick(screen, CastleOption.Summary);
            Frames(30);
            Shot("bk2b_summary");
            screen.Cancel();
            Pick(screen, CastleOption.Moves);
            Frames(16);
            Shot("bk2c_moves");
            screen.Cancel();
        });
        Question("Castle Points.", "bk20b_services_again");
        ReadOn(() => State() == GameState.CastleCounter);
        Counter();
        Console.WriteLine($"  after the first counter: {Three(castle.Team.Members)}; {Told()}");
        // The counter about the trainer to come: nobody known, one identified, its level raised, its stats told, and
        // the moves, which the counter's first rank doesn't tell
        answers.Enqueue(2);
        atCounter.Enqueue(screen =>
        {
            Frames(20);
            Shot("bk30_counter_their_three");
            screen.Confirm();
            Frames(10);
            Shot("bk31_their_menu");
            Pick(screen, CastleOption.Check);
            Frames(16);
            Shot("bk32_identify_question");
            screen.Confirm();
            Frames(16);
            Shot("bk33_identified");
            screen.Confirm();
            Pick(screen, CastleOption.Level);
            Frames(10);
            Pick(screen, CastleOption.LevelUp);
            Frames(16);
            Shot("bk34_level_question");
            screen.Confirm();
            Frames(16);
            Shot("bk35_five_levels_up");
            screen.Confirm();
            screen.Cancel();
            Pick(screen, CastleOption.Info);
            Frames(10);
            Shot("bk36_info_menu");
            Pick(screen, CastleOption.Stats);
            Frames(16);
            screen.Confirm();
            Frames(30);
            Shot("bk37_their_stats");
            screen.Cancel();
            Pick(screen, CastleOption.Moves);
            Frames(16);
            Shot("bk38_moves_want_a_rank");
            screen.Confirm();
            screen.Cancel();
            screen.Cancel();
            Frames(10);
            Shot("bk39_one_known");
        });
        ReadOn(() => State() == GameState.CastleCounter);
        Counter();
        Console.WriteLine($"  after the second counter: next {Three(castle.Foe!.Pokemon)}; identified {string.Join(",", castle.Identified)}; levels {string.Join(",", castle.Levels)}; {Told()}");
        Line("comes forward", "bk3a_second_trainer");
        Line("received", "bk3b_points_for_a_raised_foe");
        // The rest of the round
        Line("Seven battles", "bk40_seven_won");
        Line("received", "bk41_battle_points");
        Console.WriteLine($"  after the round: {Told()}");
        Line("all seven", "bk43_lobby_congratulations");
        ReadOn();
        Console.WriteLine($"  back in the lobby: challenge {(game.Castle == null ? "let go of" : "still held")}; the team's own {Three(team.Members.Take(3))}; {Told()}");
        Talk(6, 9, Direction.Up);
        Board("bk44_records_after_a_round");

        // ---- the second round: a battle, and the challenge put down and found waiting
        Talk(6, 9, Direction.Left);
        Line("as before", "bk50_valet_is_brief");
        answers.Enqueue(0);
        answers.Enqueue(1);
        Question("Are you ready", "bk51_rest_chosen");
        Question("Put the challenge down", "bk52_rest_question");
        answers.Enqueue(0);
        answers.Enqueue(1);
        Question("go on with it now", "bk54_waiting_question");
        ReadOn();
        Console.WriteLine($"  put down: {(records.Castle.Paused is { } p ? $"battle {p.Battle}, HP {string.Join("/", p.Team.Select(k => k.Hp))}, items {string.Join("/", p.Team.Select(k => k.Item ?? "-"))}" : "nothing")}; {Told()}");
        // Taken up again, and given up
        Talk(6, 9, Direction.Left);
        answers.Enqueue(0);
        answers.Enqueue(2);
        Question("Are you ready", "bk55_taken_up_again");
        Console.WriteLine($"  taken up: {Three(game.Castle!.Team.Members)}");
        Question("Give the challenge up", "bk56_retire_question");
        Line("Thank you for taking", "bk57_lobby_given_up");
        ReadOn();
        Console.WriteLine($"  given up: {Told()}");

        // ---- the fourth round: a battle passed for fifty Castle Points
        records.Set(BattleCastleChallenge.LatestStreakStat(FrontierChallenge.Single), 21);
        records.Set(FrontierStat.CastleRecordStreakSingle, 21);
        records.Set(BattleCastleChallenge.LatestPointsStat(FrontierChallenge.Single), 120);
        records.SetRunning(FrontierFacility.Castle, 0, true);
        Talk(6, 9, Direction.Left);
        Line("pass a battle", "bk60_pass_explained");
        answers.Enqueue(3);
        Question("Castle Points.", "bk61_pass_offered");
        Question("Passing this battle", "bk62_pass_question");
        Arrived(Throne, "bk63_throne_room_passing", 10);
        Line("presented a pass", "bk64_pass_presented");
        // Back with the valet: the challenge is given up, to get on to the Castle Valet
        answers.Enqueue(2);
        Question("Are you ready", "bk65_after_the_pass");
        Line("Thank you for taking", "bk66_given_up_again");
        ReadOn();
        Console.WriteLine($"  after the pass: {Told()}");

        // ---- the Castle Valet: a streak of fourteen taken up, its third round's seventh battle
        records.Set(BattleCastleChallenge.LatestStreakStat(FrontierChallenge.Single), 14);
        records.SetRunning(FrontierFacility.Castle, 0, true);
        Talk(6, 9, Direction.Left);
        Line("Castle Valet himself", "bk70_darach_announced");
        ReadOn(() => Here().Name == Throne && State() == GameState.Overworld && game.Castle is { Battle: 6 });
        Line("I am Darach", "bk71_darach");
        sure = true;
        atMenu = fight =>
        {
            Skip(0.6);
            Shot("bk73_darach_battle_menu");
            Console.WriteLine($"  Darach's three: {Three(fight.EnemyParty.Members)}");
        };
        ReadOn(() => State() == GameState.Battle);
        Skip(1.6);
        Shot("bk72_darach_battle");
        Line("Admirably", "bk74_darach_beaten");
        sure = false;
        Line("from his own hand", "bk75_twenty_points");
        Line("Silver Print", "bk76_silver_print");
        ReadOn();
        Console.WriteLine($"  after the Castle Valet: {Told()}; brains met {records.BrainsMet}");
        Talk(6, 9, Direction.Up);
        Board("bk77_records_with_a_print");
    }
}
