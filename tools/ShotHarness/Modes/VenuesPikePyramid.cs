using PokemonPlatinumEngine.Models.Frontier;

partial class Harness
{
    // ---------------------------------------------------------------- the venues of plan 14, B10

    // The Battle Pike and the Battle Pyramid (vn2*; the end of `venues`, or alone with VENUES_FROM=pike), through the game's
    // own TryInteract, scripts, question boxes, team screen and battles: the Pike's desk, three entered, the hall of three
    // doors with the attendant's hint (vn20_pike_doors), three rooms gone through by their doors' own scripts and what each
    // held (vn20b to vn20d); then the Pyramid's desk, three entered, the first floor in its dark (vn21_pyramid_dark), its
    // trainers battled and won, the circle wider (vn22_pyramid_lit), the stairs climbed floor by floor to the seventh
    // (vn23_pyramid_floor7) and the top. Every battle is won (the foes are left a single HP at each menu, the lead kept
    // whole). It prints each room's kind and what it did, each floor's seed, template, way in, stairs and trainers, the
    // light after each battle, the records and the Battle Points, and whether the seven floors made again from their seeds
    // are the same floors (the plan's "the same floors on a second run").
    public void VenuesPikePyramid(bool fresh)
    {
        if (fresh)
        {
            engine.StartNewGame();
            PastTheOpening();
            game.State = GameState.Overworld;
            game.LocationSign.Hide();
        }
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
        Pokemon Mon(string species, int level, string item)
        {
            var p = new Pokemon(PokemonDatabase.Get(species)!, level);
            p.HeldItem = ItemDatabase.Get(item);
            game.Pokedex.RegisterSeen(p.Species.DexNumber);
            game.Pokedex.RegisterCaught(p.Species.DexNumber);
            return p;
        }
        var answers = new Queue<int>();

        // A battle read on: the foes are left a single HP at each menu and the lead is kept whole
        void ReadTheBattle()
        {
            var fight = game.Battle!;
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
            Console.WriteLine($"  battle in {Here().Name}: {fight.Result} in {fight.Core.Turn} turns against "
                + $"{string.Join("/", fight.EnemyParty.Members.Select(p => $"{p.Species.Name} {p.CurrentHP}"))}");
            for (int i = 0; i < 1500 && State() is GameState.Battle or GameState.Transition; i++) Tick();
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
        void ReadOn(Func<bool>? until = null, int most = 30000)
        {
            for (int i = 0; i < most; i++)
            {
                if (until != null && until()) return;
                if (until == null && !engine.ScriptRunning && !Box().IsActive && State() == GameState.Overworld) return;
                if (State() == GameState.Battle) { ReadTheBattle(); continue; }
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
        void Talk(int x, int y, Direction facing)
        {
            game.Player.SetPosition(x, y, facing);
            Frames(3);
            game.Interact();
            Frames(3);
        }

        // Three strong ones that may go in together
        team.Clear();
        team.Add(Mon("Garchomp", 62, "Leftovers"));
        team.Add(Mon("Lucario", 58, "Life Orb"));
        team.Add(Mon("Staraptor", 55, "Choice Band"));

        // ---- the Battle Pike: its desk, three entered, the hall of doors and three rooms
        At(GameEngine.DomeLobbyMap, 7, 8, Direction.Up);
        Frames(30);
        Talk(2, 7, Direction.Left);
        answers.Enqueue(0);
        ReadOn(() => State() == GameState.PartyMenu);
        EnterTeam();
        ReadOn(() => Here().Name == GameEngine.PikeMap && Says("Room"));
        if (Says("Room")) { Box().FinishLine(); Frames(6); Shot("vn20_pike_doors"); }
        ReadOn();
        string[] doors = { "LeftDoor", "MiddleDoor", "RightDoor" };
        string[] roomShots = { "vn20b_pike_room", "vn20c_pike_room", "vn20d_pike_room" };
        for (int n = 0; n < 3 && game.Pike != null; n++)
        {
            var pike = game.Pike;
            Console.WriteLine($"  room {pike.Next}: the hint is of the {new[] { "left", "middle", "right" }[pike.HintedDoor % 3]} door, {pike.HintedRoom}");
            game.Player.SetPosition(3 + 3 * (n % 3), 3, Direction.Up);
            engine.StartScript(doors[n % 3], file: GameEngine.PikeMap);
            ReadOn(() => Here().Name == GameEngine.PikeRoomMap && State() == GameState.Overworld && (Box().IsActive || !engine.ScriptRunning));
            Frames(20);
            Shot(roomShots[n]);
            Console.WriteLine($"  behind it: {pike.Current}; foes {string.Join(", ", pike.Foes.Select(f => $"{f.Record.Class} {f.Record.Name}"))}; "
                + $"wild {pike.Wild?.Species.Name ?? "none"}; power {pike.Inflicted?.ToString() ?? "none"}; healed {pike.Healed}");
            ReadOn();
            if (game.Pike == null) break;
            game.Player.SetPosition(6, 3, Direction.Up);
            engine.StartScript("Exit", file: GameEngine.PikeRoomMap);
            ReadOn(() => Here().Name == GameEngine.PikeMap && State() == GameState.Overworld);
            ReadOn();
            Console.WriteLine($"  the three: {string.Join(", ", pike.Team.Members.Select(p => $"{p.Species.Name} {p.CurrentHP}/{p.MaxHP} {p.Status}"))}");
        }
        Console.WriteLine($"  the Pike's record: streak {records.Pike.Streak}, best {records.Pike.Best}, in progress {records.Pike.InProgress}");

        // ---- the Battle Pyramid: its desk, three entered, the first floor in the dark, its trainers, the stairs to the top
        At(GameEngine.DomeLobbyMap, 12, 8, Direction.Up);
        Frames(20);
        Talk(12, 7, Direction.Right);
        answers.Enqueue(0);
        ReadOn(() => State() == GameState.PartyMenu);
        EnterTeam();
        ReadOn(() => Here().Name == PyramidFloor.MapName && State() == GameState.Overworld && !engine.ScriptRunning && !Box().IsActive);
        Frames(40);
        Shot("vn21_pyramid_dark");
        var round = game.Pyramid;
        if (round == null) { Console.WriteLine("  !! no round of the Pyramid under way"); return; }
        var layouts = new List<string>();
        for (int floor = 1; floor <= BattlePyramidChallenge.Floors && game.Pyramid != null; floor++)
        {
            var layout = round.Layout!;
            string signature = $"{layout.Template}:{string.Join("", layout.Layout)}:{layout.Entrance}:{layout.Exit}:{string.Join(";", layout.Trainers)}:{string.Join(";", layout.Finds)}";
            layouts.Add(signature);
            Console.WriteLine($"  floor {floor}: seed {round.Seeds[floor - 1]}, template {layout.Template}, way in {layout.Entrance}, stairs {layout.Exit}, "
                + $"trainers {string.Join(", ", round.FloorFoes.Select(f => $"{f.Record.Class} {f.Record.Name}"))}, finds {layout.Finds.Count}, "
                + $"wild {string.Join("/", Here().WildEncounters.Select(e => e.SpeciesName))}");
            if (floor == 1)
            {
                // Every trainer of the first floor battled where they stand, and the light widened after each
                foreach (var trainer in Here().NPCs.Where(n => n.IsTrainer).ToList())
                {
                    game.Player.SetPosition(layout.Entrance.X, layout.Entrance.Y, Direction.Up);
                    engine.StartScript("Trainer", trainer);
                    ReadOn();
                    Console.WriteLine($"  light {round.Light} after {trainer.Name}, reach {Here().DarkReach:F2}");
                }
                game.Player.SetPosition(layout.Entrance.X, layout.Entrance.Y, Direction.Up);
                Frames(90);
                Shot("vn22_pyramid_lit");
            }
            if (floor == BattlePyramidChallenge.Floors) { Frames(30); Shot("vn23_pyramid_floor7"); }
            game.Player.SetPosition(layout.Exit.X, layout.Exit.Y, Direction.Up);
            engine.StartScript("Stairs", file: PyramidFloor.MapName);
            ReadOn(() => State() == GameState.Overworld && !engine.ScriptRunning && !Box().IsActive && (game.Pyramid == null || round.Floor > floor));
            ReadOn();
        }
        Frames(20);
        Shot("vn24_pyramid_lobby_after");
        Console.WriteLine($"  the Pyramid's record: streak {records.Pyramid.Streak}, best {records.Pyramid.Best}, rounds {records.Pyramid.Rounds}; {records.BattlePoints} BP");
        // The same floors from the same seeds: what a second run of the round lays out
        bool same = layouts.Count == BattlePyramidChallenge.Floors && Enumerable.Range(0, layouts.Count).All(i =>
        {
            var again = PyramidFloor.Generate(round.Seeds[i], i);
            return layouts[i] == $"{again.Template}:{string.Join("", again.Layout)}:{again.Entrance}:{again.Exit}:{string.Join(";", again.Trainers)}:{string.Join(";", again.Finds)}";
        });
        Console.WriteLine($"  the seven floors made again from their seeds: {(same ? "the same" : "!! not the same")} ({layouts.Count} climbed)");
    }
}
