using PokemonPlatinumEngine.Models.Frontier;

partial class Harness
{
    // ---------------------------------------------------------------- the venues of plan 14, B9

    // The Battle Palace and the Battle Arena (vn1*; the end of `venues`, or alone with VENUES_FROM=palace), through the
    // game's own TryInteract, scripts, question boxes, team screen and battles: the annex's lobby with the two new desks,
    // the Palace's desk and rules, three entered, its hall, a Palace battle with its commands closed while the Pokémon
    // decides for itself (vn10_palace_thinking), a nature's line at half HP if one comes, the battle read to its end; then
    // the Arena's desk, its hall, a bout's menu, the judge's card over its verdict (vn11_arena_judged) and the battle read
    // on. A Palace battle can't be steered (nobody chooses), so it prints how it went; in the Arena the foe is left one HP
    // at each menu but the third turn's, so that a bout is judged. It prints each battle's lines that matter, the verdicts,
    // the records and the Battle Points.
    public void VenuesPalaceArena(bool fresh)
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
        Pokemon Mon(string species, int level, string item, Nature nature)
        {
            var p = new Pokemon(PokemonDatabase.Get(species)!, level);
            p.HeldItem = ItemDatabase.Get(item);
            p.Nature = nature;
            game.Pokedex.RegisterSeen(p.Species.DexNumber);
            game.Pokedex.RegisterCaught(p.Species.DexNumber);
            return p;
        }
        var answers = new Queue<int>();
        string? thinkingShot = "vn10_palace_thinking", flavourShot = "vn10b_palace_nature_line", menuShot = "vn11a_arena_menu", judgedShot = "vn11_arena_judged";
        int menus = 0;

        // A battle read on: the Palace's has no menu, the Arena's leaves the foe a single HP but on a bout's third turn
        void ReadTheBattle(string venue)
        {
            var fight = game.Battle!;
            for (int guard = 0; guard < 1500 && !fight.IsBattleOver; guard++)
            {
                string line = fight.CurrentMessage ?? "";
                if (fight.HUD.MenuState == BattleMenuState.Message)
                {
                    if (thinkingShot != null && fight.Palace && line.Contains("is deciding for itself")) { Frames(20); Shot(thinkingShot); thinkingShot = null; }
                    if (flavourShot != null && fight.Palace && (line.Contains("hungry for the next") || line.Contains("hard gleam") || line.Contains("braces itself") || line.Contains("rumbling growl")))
                    { Frames(20); Shot(flavourShot); flavourShot = null; }
                    if (judgedShot != null && fight.JudgeCard != null) { Frames(30); Shot(judgedShot); judgedShot = null; }
                    if (fight.JudgeCard is { } v)
                        Console.WriteLine($"  the judge: mind {v.MindMine}-{v.MindTheirs}, skill {v.SkillMine}-{v.SkillTheirs}, body {v.BodyMine}-{v.BodyTheirs}, score {v.ScoreMine}-{v.ScoreTheirs}, to {v.Winner?.ToString() ?? "nobody"}");
                }
                if (fight.HUD.MenuState == BattleMenuState.Main)
                {
                    if (menuShot != null) { Frames(20); Shot(menuShot); menuShot = null; }
                    menus++;
                    var lead = fight.PlayerPokemon;
                    lead.CurrentHP = lead.MaxHP;
                    lead.Status = StatusCondition.None;
                    foreach (var move in lead.Moves) move.CurrentPP = move.MaxPP;
                    // Until a bout has been judged the foe is kept whole and the lead uses a move that hurts nobody, so the
                    // first bout runs its three turns; afterwards the foes are left a single HP and every battle is won
                    bool toTheJudge = judgedShot != null && fight.Arena;
                    foreach (var foe in fight.EnemyParty.Members) foe.CurrentHP = toTheJudge ? foe.MaxHP : Math.Min(foe.CurrentHP, 1);
                    var target = fight.EnemyPokemon;
                    int pick = toTheJudge ? lead.Moves.FindIndex(m => m.Data.Category == MoveCategory.Status)
                        : lead.Moves.FindIndex(m => m.Data.Category != MoveCategory.Status
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
            Console.WriteLine($"  {venue} battle {(game.Venue?.Battle ?? -1) + 1}: {fight.Result} in {fight.Core.Turn} turns, "
                + $"{string.Join("/", fight.PlayerParty.Members.Select(p => $"{p.Species.Name} {p.CurrentHP}"))} against "
                + $"{string.Join("/", fight.EnemyParty.Members.Select(p => $"{p.Species.Name} {p.CurrentHP}"))}; verdicts {fight.Core.Verdicts.Count}");
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
                if (State() == GameState.Battle) { ReadTheBattle(game.Map.Name); continue; }
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

        // Three of strong natures for the Palace: a Brave Garchomp, a Gentle Lucario, a Sassy Staraptor
        team.Clear();
        team.Add(Mon("Garchomp", 62, "Leftovers", Nature.Brave));
        team.Add(Mon("Lucario", 58, "Life Orb", Nature.Gentle));
        team.Add(Mon("Staraptor", 55, "Choice Band", Nature.Sassy));

        // ---- the lobby with its three desks
        At(GameEngine.DomeLobbyMap, 7, 8, Direction.Up);
        Frames(40);
        Shot("vn10c_annex_three_desks");

        // ---- the Palace: its desk, the rules, three entered, the hall and a battle read to its end
        Talk(2, 5, Direction.Left);
        answers.Enqueue(1);
        Question("take on the Battle Palace", "vn10d_palace_desk");
        Line("by its nature", "vn10e_palace_rules");
        ReadOn();
        Talk(2, 5, Direction.Left);
        answers.Enqueue(0);
        ReadOn(() => State() == GameState.PartyMenu);
        EnterTeam();
        ReadOn(() => Here().Name == "BattlePalace" && State() == GameState.Overworld);
        Frames(20);
        Shot("vn10f_palace_hall");
        if (game.Venue is { } palace)
            Console.WriteLine($"  the Palace's seven: {string.Join(", ", palace.Foes.Select(f => $"{f.Record.Class} {f.Record.Name} ({string.Join("/", f.Pokemon.Select(p => p.Species.Name))})"))}");
        // One battle is enough to see it; the round is then read to its end whichever way it goes
        ReadOn(() => Here().Name == GameEngine.DomeLobbyMap && State() == GameState.Overworld && !engine.ScriptRunning);
        ReadOn();
        Console.WriteLine($"  the Palace's record: streak {records.Palace.Streak}, best {records.Palace.Best}, rounds {records.Palace.Rounds}; {records.BattlePoints} BP");

        // ---- the Arena: its desk, three entered, the hall, a bout's menu and the judge's card
        Talk(12, 5, Direction.Right);
        answers.Enqueue(0);
        ReadOn(() => State() == GameState.PartyMenu);
        Frames(30);
        Shot("vn11b_arena_three_to_enter");
        EnterTeam();
        ReadOn(() => Here().Name == "BattleArena" && State() == GameState.Overworld);
        Frames(20);
        Shot("vn11c_arena_hall");
        ReadOn(() => Here().Name == GameEngine.DomeLobbyMap && State() == GameState.Overworld && !engine.ScriptRunning);
        ReadOn();
        Frames(20);
        Shot("vn11d_lobby_after");
        Console.WriteLine($"  the Arena's record: streak {records.Arena.Streak}, best {records.Arena.Best}, rounds {records.Arena.Rounds}; {records.BattlePoints} BP; {menus} menus answered");
        if (thinkingShot != null) Console.WriteLine("  !! no Palace battle said its line of choosing");
        if (judgedShot != null) Console.WriteLine("  !! no bout was judged");
    }
}
