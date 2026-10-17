using PokemonPlatinumEngine.Models.Frontier;

partial class Harness
{
    // ---------------------------------------------------------------- the Battle Frontier's frame and the Battle Tower (plan 06 · R18a)

    // The Battle Tower from the Frontier's grounds to its Battle Points (bt*; not part of `all`), through the game's
    // own TryInteract, ArriveOnMap, scripts, question boxes, team screen and battles: the Tower from outside, its
    // lobby and counters, the judge, the records' board, the counter turning a team away, the three Pokémon entered
    // and two with one item refused, the lift, the corridor, the battle room, a trainer stepping up, a battle with the
    // Bag refused and RUN's question, the attendant between battles, the seventh won and its Battle Points, the lobby
    // afterwards; the Tower Tycoon at the twenty-first battle, his print, ribbon and trophy; the Multi Battle Room
    // with a partner out of the Battle Salon, a challenge put down and found waiting; and the Exchange Service Corner.
    // It wins every battle (the foes are left a single HP) and prints what each left behind.
    public void TowerMode()
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
        // time the team is opened for it, and what is shot as those screens and a battle's first menu come up
        var answers = new Queue<int>();
        var entering = new Queue<int[]>();
        string? teamShot = null, enteredShot = null;
        Action<BattleEngine>? atMenu = null;
        int judged = 0;

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
                    // Someone of the player's fainted: whoever can still fight comes in
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
            if (screen.Choosing)
            {
                screen.SelectedIndex = judged;
                Frames(2);
                screen.Confirm(team);
                Frames(4);
                return;
            }
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
        // Reads on until the script has ended, or, given a test, until it holds: text read, questions answered, the
        // team entered, battles won
        void ReadOn(Func<bool>? until = null, int most = 9000)
        {
            for (int i = 0; i < most; i++)
            {
                if (until != null && until()) return;
                if (until == null && !engine.ScriptRunning && !Box().IsActive && State() == GameState.Overworld) return;
                if (State() == GameState.Battle) { Win(); continue; }
                if (State() == GameState.PartyMenu) { EnterTeam(); continue; }
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
        // A question's line with its answers beside it, the cursor on the one about to be given
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
        void Enter(string map, int x, int y, Direction facing = Direction.Up)
        {
            At(map, x, y, facing);
            game.ArriveOnMap();
            Frames(6);
            ReadOn();
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
        string Told() => $"streak {records.Get(FrontierStat.TowerLatestStreakSingle)}/{records.Get(FrontierStat.TowerRecordStreakSingle)} single, "
            + $"{records.Get(FrontierStat.TowerLatestStreakDouble)}/{records.Get(FrontierStat.TowerRecordStreakDouble)} double, "
            + $"{records.Get(FrontierStat.TowerLatestStreakMulti)}/{records.Get(FrontierStat.TowerRecordStreakMulti)} multi; rooms {string.Join(",", records.Tower.Rooms.Take(3))}; "
            + $"{records.BattlePoints} BP; victories {records.VictoriesAt(FrontierFacility.Tower)}; print state {story.Var("VAR_BATTLE_TOWER_PRINT_STATE")}";

        Pokemon Mon(string species, int level, string? item = null)
        {
            var p = new Pokemon(PokemonDatabase.Get(species)!, level);
            if (item != null) p.HeldItem = ItemDatabase.Get(item);
            pokedex.RegisterSeen(p.Species.DexNumber);
            pokedex.RegisterCaught(p.Species.DexNumber);
            return p;
        }
        void TeamOf(params Pokemon[] members)
        {
            team.Clear();
            foreach (var p in members) team.Add(p);
        }
        const string Lobby = "BattleTower";

        // ---- the Tower from the grounds, and its lobby
        TeamOf(Mon("Garchomp", 62, "Leftovers"), Mon("Dialga", 60), Mon("Lucario", 55));
        pokedex.RegisterSeen(PokemonDatabase.Get("Giratina")!.DexNumber);
        At("BattleFrontier", 48, 22, Direction.Up);
        Frames(30);
        Shot("bt01_tower_from_the_grounds");
        At("BattleFrontier", 48, 19, Direction.Up);
        Frames(10);
        Shot("bt02_tower_door");

        Enter(Lobby, 15, 19);
        Frames(20);
        Shot("bt03_lobby_from_its_door");
        At(Lobby, 15, 8, Direction.Up);
        Frames(6);
        Shot("bt04_lobby_counters");
        At(Lobby, 6, 11, Direction.Up);
        Frames(6);
        Shot("bt05_lobby_west");
        At(Lobby, 24, 11, Direction.Up);
        Frames(6);
        Shot("bt06_lobby_east");

        // ---- the judge, the records' machine, the Wi-Fi counter
        Talk(24, 8, Direction.Up);
        Line("Taken all together", "bt07_judge");
        Line("has most of", "bt08_judge_best");
        ReadOn();
        Talk(13, 6, Direction.Up);
        Board("bt09_records_nothing_yet");
        Talk(19, 6, Direction.Up);
        Line("closed", "bt0a_wifi_counter");
        ReadOn();

        // ---- the counter turns a team away: too few that may go in
        Talk(15, 6, Direction.Up);
        Question("Battle Room challenge", "bt10_counter_question");
        Question("Which Battle Room", "bt11_which_room");
        Line("You need", "bt12_too_few");
        Line("nor can", "bt13_banned_named");
        ReadOn();
        Console.WriteLine($"  turned away with a Dialga in a team of three: challenge {(game.Tower == null ? "none" : "begun")}");

        // ---- three entered, two of them holding the same item
        TeamOf(Mon("Garchomp", 62, "Leftovers"), Mon("Dialga", 60), Mon("Lucario", 55, "Leftovers"), Mon("Togekiss", 58, "Lum Berry"), Mon("Milotic", 48), Mon("Gengar", 52, "Focus Sash"));
        entering.Enqueue(new[] { 0, 1, 2, 3 });
        teamShot = "bt14_team_to_choose_from";
        enteredShot = "bt15_three_entered";
        Talk(15, 6, Direction.Up);
        Line("same item", "bt16_same_item_refused");
        ReadOn();
        Console.WriteLine($"  turned away with two Leftovers: challenge {(game.Tower == null ? "none" : "begun")}");

        // ---- a Single Battle Room challenge from the counter to the Battle Points
        team.Members[2].HeldItem = ItemDatabase.Get("Expert Belt");
        team.Members[0].CurrentHP = 20;
        entering.Enqueue(new[] { 0, 2, 3 });
        Talk(15, 6, Direction.Up);
        Question("save it", "bt17_save_question");
        Line("saved the game", "bt18_saved");
        ReadOn(() => Says("show you the way"));
        Console.WriteLine($"  the counter: {game.Tower?.Mode}, entered {string.Join(",", game.Tower?.Slots ?? new List<int>())}, the set's trainers {string.Join(",", game.Tower?.Trainers ?? new List<int>())}, "
            + $"Garchomp's HP {team.Members[0].CurrentHP}/{team.Members[0].MaxHP}, lobby action {story.Var("VAR_BATTLE_TOWER_LOBBY_LOAD_ACTION")}");
        ReadOn(() => Here().Name == Lobby && game.Player.GridY <= 4 || Here().Name != Lobby);
        Shot("bt19_to_the_lift");
        Arrived("BattleTowerElevator", "bt20_lift", 50);
        Arrived("BattleTowerCorridor", "bt21_corridor", 40);
        ReadOn(() => Here().Name == "BattleTowerCorridor" && game.Player.GridX >= 4 || Here().Name == "BattleTowerBattleRoom");
        Shot("bt22_corridor_to_a_door");
        Arrived("BattleTowerBattleRoom", "bt23_battle_room", 30);
        Line("steps up", "bt24_a_trainer_steps_up");
        atMenu = fight =>
        {
            Console.WriteLine($"  the battle: against {string.Join(" and ", fight.Trainers.Select(t => $"{t.TrainerClass} {t.Name}"))} with "
                + $"{string.Join(", ", fight.EnemyParty.Members.Select(p => $"{p.Species.Name} Lv. {p.Level} ({p.HeldItem?.Name ?? "no item"}, {p.Nature}, IVs {p.IvHP})"))}; "
                + $"mine {string.Join(", ", fight.PlayerParty.Members.Select(p => $"{p.Species.Name} Lv. {p.Level} {p.CurrentHP}/{p.MaxHP}"))}");
            Skip(0.6);
            Shot("bt26_battle_menu");
            fight.SelectMainMenuOption(1);
            Skip(0.5);
            Shot("bt27_bag_refused");
            fight.ConfirmMessage();
            Skip(0.3);
            fight.SelectMainMenuOption(3);
            Skip(0.5);
            Shot("bt28_forfeit_question");
            fight.ChooseLearn(1);
            Skip(0.3);
        };
        ReadOn(() => State() == GameState.Battle, 1500);
        Skip(1.6);
        Shot("bt25_battle_begins");
        int exp = team.Members[0].CurrentExp, money = game.Money;
        Line("restore your", "bt29_between_battles_healed");
        Console.WriteLine($"  after the first battle: EXP {team.Members[0].CurrentExp - exp:+0;-0;0}, money {game.Money - money:+0;-0;0}, the team's own levels {string.Join(",", team.Members.Select(p => p.Level))}, streak in hand {game.Tower?.Streak}");
        Question("Are you ready", "bt2a_next_battle_question");
        Line("Seven wins", "bt2b_seven_won");
        Line("received", "bt2c_battle_points");
        Console.WriteLine($"  seven won: {Told()}");
        Arrived(Lobby, "bt2d_lobby_afterwards", 10);
        Line("all seven", "bt2e_lobby_congratulations");
        ReadOn();
        Console.WriteLine($"  back in the lobby: challenge {(game.Tower == null ? "let go of" : "still held")}, lobby action {story.Var("VAR_BATTLE_TOWER_LOBBY_LOAD_ACTION")}, score {game.TrainerScore}");
        Talk(13, 6, Direction.Up);
        Board("bt2f_records_after_a_set");

        // ---- the Tower Tycoon: the third set's seventh battle
        answers.Clear();
        records.Set(FrontierStat.TowerLatestStreakSingle, 14);
        records.Set(FrontierStat.TowerRecordStreakSingle, 14);
        records.Tower.Rooms[0] = 2;
        entering.Enqueue(new[] { 0, 2, 3 });
        Talk(15, 6, Direction.Up);
        Line("on his way", "bt30_tycoon_announced");
        ReadOn(() => Says("So you are"));
        Box().FinishLine(); Frames(4);
        Shot("bt31_palmer");
        atMenu = fight =>
        {
            Console.WriteLine($"  the Tycoon's team: {string.Join(", ", fight.EnemyParty.Members.Select(p => $"{p.Species.Name} Lv. {p.Level} ({p.HeldItem?.Name ?? "no item"}, IVs {p.IvHP})"))}");
            Skip(0.6);
            Shot("bt33_palmer_battle_menu");
        };
        ReadOn(() => State() == GameState.Battle, 1500);
        Skip(1.6);
        Shot("bt32_palmer_battle_begins");
        Line("no loss at all", "bt34_palmer_beaten");
        Line("received", "bt35_palmer_battle_points");
        Console.WriteLine($"  the Tycoon beaten: {Told()}");
        Line("Silver Print", "bt36_silver_print");
        Line("ribbon", "bt37_ability_ribbon");
        Line("trophy", "bt38_trophy");
        ReadOn();
        Console.WriteLine($"  afterwards: {Told()}; ribbons {string.Join(" | ", team.Members.Select(p => $"{p.Species.Name}: {string.Join(",", p.Ribbons)}"))}; "
            + $"the base's PC {string.Join(",", game.Underground.PcGoods.Select(UndergroundDatabase.GoodName))}; brains met {records.BrainsMet}");
        Talk(13, 6, Direction.Up);
        Board("bt39_records_with_a_print");

        // ---- the Multi Battle Room: the Battle Salon, a partner, a battle side by side, the challenge put down
        answers.Clear();
        story.Set("FLAG_TRAVELED_WITH_CHERYL");
        story.Set("FLAG_TRAVELED_WITH_MIRA");
        story.Set("FLAG_RECEIVED_RIOLU_EGG_FROM_RILEY");
        entering.Enqueue(new[] { 0, 3 });
        Talk(11, 6, Direction.Up);
        Question("Multi Battle Room challenge", "bt40_multi_counter");
        ReadOn(() => Here().Name == "BattleTowerBattleSalon" && Says("Battle Salon"));
        Box().FinishLine(); Frames(4);
        Shot("bt41_battle_salon");
        ReadOn();
        Console.WriteLine($"  the salon: {string.Join(", ", Here().NPCs.Where(n => n.Key != null).Select(n => n.Key))}; partners made {game.Tower?.Candidates.Count}");
        var riley = Here().Everyone.First(n => n.Key == "riley");
        Talk(riley.GridX, riley.GridY + 1, Direction.Up);
        Question("together", "bt42_a_partner_offers");
        Arrived("BattleTowerCorridorMulti", "bt43_multi_corridor", 50);
        Arrived("BattleTowerMultiBattleRoom", "bt44_multi_battle_room", 36);
        Line("step up", "bt45_a_pair_steps_up");
        atMenu = fight =>
        {
            Console.WriteLine($"  the multi battle: beside {fight.Partner?.Name} with {string.Join(", ", fight.Partner?.Party.Members.Select(p => p.Species.Name) ?? Array.Empty<string>())}, "
                + $"against {string.Join(" and ", fight.Trainers.Select(t => $"{t.TrainerClass} {t.Name}"))}");
            Skip(0.6);
            Shot("bt47_multi_battle_menu");
        };
        ReadOn(() => State() == GameState.Battle, 1500);
        Skip(1.6);
        Shot("bt46_multi_battle_begins");
        answers.Enqueue(1);
        Question("Are you ready", "bt48_rest_chosen");
        Question("Put the challenge down", "bt49_rest_question");
        answers.Enqueue(0);
        answers.Enqueue(1);
        Question("waiting where you put it down", "bt4a_challenge_waiting");
        ReadOn();
        Console.WriteLine($"  put down: next opponent {records.Tower.Paused?.NextOpponent}, partner {records.Tower.Paused?.Partner?.Id}, lobby action {story.Var("VAR_BATTLE_TOWER_LOBBY_LOAD_ACTION")}; {Told()}");
        // Taken up again at the counter, and given up before the next battle
        Talk(11, 6, Direction.Up);
        answers.Enqueue(0);
        answers.Enqueue(2);
        Question("Give the challenge up", "bt4b_retire_question");
        Arrived(Lobby, "bt4c_lobby_after_giving_up", 10);
        ReadOn();
        Console.WriteLine($"  given up: {Told()}; challenge {(game.Tower == null ? "let go of" : "still held")}");

        // ---- the Exchange Service Corner
        answers.Clear();
        Enter("BattleFrontier", 48, 44, Direction.Up);
        Frames(20);
        Shot("bt50_exchange_service_corner");
        void Booth(string who, int answer, string list, string word, string shot)
        {
            var attendant = Here().Everyone.First(n => n.Key == who);
            var (dx, dy) = attendant.Facing switch { Direction.Left => (-1, 0), Direction.Right => (1, 0), Direction.Up => (0, -1), _ => (0, 1) };
            int reach = Here().IsWalkable(attendant.GridX + dx, attendant.GridY + dy) ? 1 : 2;
            var back = attendant.Facing switch { Direction.Left => Direction.Right, Direction.Right => Direction.Left, Direction.Up => Direction.Down, _ => Direction.Up };
            answers.Enqueue(answer);
            Talk(attendant.GridX + dx * reach, attendant.GridY + dy * reach, back);
            Question("", list);
            Line(word, shot);
            ReadOn();
        }
        Booth("attendant_southwest", 6, "bt51_exchange_list", "traded", "bt52_exchange_traded");
        Console.WriteLine($"  the west booth: {records.BattlePoints} BP left, Power Bracer {inventory.GetQuantity(ItemDatabase.Get("Power Bracer")!)}");
        Booth("attendant_southeast", 11, "bt53_exchange_machines", "don't have", "bt54_exchange_too_few");
        Console.WriteLine($"  the east booth: {records.BattlePoints} BP left, spent {records.PointsSpent} of {records.PointsReceived}");
    }
}
