using PokemonPlatinumEngine.Models.Frontier;

partial class Harness
{
    // ---------------------------------------------------------------- the venues of plan 14, B11

    // The World Tournament (vn3*; the end of `venues`, or alone with VENUES_FROM=tournament), through the game's own
    // TryInteract, scripts, question boxes, the tournament's screen, the team's screen and battles: the desk in the annex's
    // lobby, its events with the Champion's locked (vn30_pwt_events), three entered on the team's screen (vn31_pwt_pick),
    // the bracket of eight as the first round opens (vn32_pwt_bracket), Sinnoh Leaders won to the title; then the
    // Champion's event entered and Cynthia on the stage the first time she is met (vn33_pwt_cynthia), and its title. Every
    // battle is won (the foes are left a single HP at each menu, the lead kept whole). It prints each event's field and
    // bracket, who met whom headless and who won, the record and the Battle Points, and whether the save is byte for byte
    // what it was but for its record (the plan's "Done when").
    public void VenuesTournament(bool fresh)
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
        string? eventsShot = null, pickShot = null, bracketShot = null;
        int eventToChoose = 0;

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
            Console.WriteLine($"  match of round {(game.Tournament?.Round ?? -1) + 1}: {fight.Result} in {fight.Core.Turn} turns against "
                + $"{string.Join("/", fight.EnemyParty.Members.Select(p => $"{p.Species.Name} Lv{p.Level}"))}");
            for (int i = 0; i < 1500 && State() is GameState.Battle or GameState.Transition; i++) Tick();
            Frames(6);
        }
        void EnterTeam()
        {
            var screen = game.PartyScreen;
            Frames(60);
            if (pickShot != null) { Shot(pickShot); pickShot = null; }
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
        // The tournament's screen: on its events the event asked for is chosen; on the bracket it is shot and put away
        void AtTheScreen()
        {
            var screen = game.TournamentScreen;
            Frames(40);
            if (screen.Step == PokemonPlatinumEngine.UI.TournamentStep.Events)
            {
                if (eventsShot != null) { Shot(eventsShot); eventsShot = null; }
                for (int k = 0; k < eventToChoose; k++) { screen.Move(0, 1); Frames(2); }
                screen.Confirm();
                Frames(4);
                Console.WriteLine($"  event chosen: {screen.Answer}");
                return;
            }
            if (bracketShot != null) { Shot(bracketShot); bracketShot = null; }
            screen.Cancel();
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
                if (State() == GameState.Tournament) { AtTheScreen(); continue; }
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
        void Tell(string what)
        {
            if (game.Tournament is not { } t) { Console.WriteLine($"  {what}: no tournament"); return; }
            Console.WriteLine($"  {what}: {t.Event.Id}, round {t.Round + 1}, seats {string.Join(" ", t.Bracket.Seats)}");
            Console.WriteLine($"    the field: {string.Join(", ", t.Field.Skip(1).Select(e => $"{e.Title} ({string.Join("/", e.Three.Select(p => p.Species.Name))})"))}");
            for (int r = 0; r < t.Bracket.Rounds; r++)
                Console.WriteLine($"    round {r + 1}: winners {string.Join(" ", t.Bracket.Winners[r])}");
        }
        // The save as it would be written, but for the Frontier's records (the tournament's record and the Battle Points) and the clock
        string SaveButTheRecord()
        {
            var save = game.BuildSave();
            save.Frontier = null;
            save.PlayTimeSeconds = 0;
            return SaveManager.Write(save);
        }
        // One event entered at the desk and played through the hall to its end
        void PlayEvent(int which, string? stageShot, string? stageWord)
        {
            eventToChoose = which;
            Talk(12, 8, Direction.Right);
            answers.Enqueue(0);
            ReadOn(() => Here().Name == GameEngine.TournamentHallMap && State() == GameState.Overworld);
            Tell("drawn");
            for (int guard = 0; guard < 6 && game.Tournament != null; guard++)
            {
                ReadOn(() => Says("steps onto the stage") || Says("title of the World Tournament") || game.Tournament == null || Here().Name != GameEngine.TournamentHallMap);
                if (!Says("steps onto the stage")) break;
                if (stageShot != null && stageWord != null && Says(stageWord))
                {
                    Box().FinishLine(); Frames(6); Shot(stageShot);
                    stageShot = null;
                }
                Box().Advance();
                Tick();
            }
            ReadOn(() => Says("title of the World Tournament") || Here().Name != GameEngine.TournamentHallMap);
            if (game.Tournament is { } done)
                Console.WriteLine($"  headless matches: {string.Join("; ", done.Fought.Select(f => $"{f.A} v {f.B} -> {f.Winner}"))}");
            if (stageShot != null) Console.WriteLine($"  !! {stageWord} never came to the stage against the player (out in the headless matches)");
            ReadOn(() => Here().Name == GameEngine.DomeLobbyMap && State() == GameState.Overworld);
            ReadOn();
            Console.WriteLine($"  the record: titles {string.Join(", ", records.Tournament.Titles.Select(t => $"{t.Key} {t.Value}"))}, entered {records.Tournament.Entered}, "
                + $"matches {records.Tournament.Matches}, in progress {records.Tournament.InProgress}; {records.BattlePoints} BP");
        }

        // Three strong ones that may go in together, one under level 50 to be raised to it
        team.Clear();
        team.Add(Mon("Garchomp", 62, "Leftovers"));
        team.Add(Mon("Lucario", 58, "Life Orb"));
        team.Add(Mon("Staraptor", 44, "Choice Band"));

        At(GameEngine.DomeLobbyMap, 12, 8, Direction.Right);
        Frames(30);
        string before = SaveButTheRecord();

        // ---- Sinnoh Leaders: the events, the pick, the bracket, three matches to the title
        eventsShot = "vn30_pwt_events";
        pickShot = "vn31_pwt_pick";
        bracketShot = "vn32_pwt_bracket";
        PlayEvent(0, null, null);

        // ---- the Champion's event, opened by that title: Cynthia on the stage
        PlayEvent(1, "vn33_pwt_cynthia", "Cynthia");

        game.Player.SetPosition(12, 8, Direction.Right);
        Frames(4);
        string after = SaveButTheRecord();
        var lines = before.Split('\n');
        var changed = after.Split('\n').Where((l, i) => i >= lines.Length || lines[i] != l).ToList();
        Console.WriteLine(before == after
            ? "  the save is byte for byte what it was but for its record"
            : $"  !! the save moved beyond its record: {changed.Count} lines, first {string.Join(" | ", changed.Take(4).Select(l => l.Trim()))}");
    }
}
