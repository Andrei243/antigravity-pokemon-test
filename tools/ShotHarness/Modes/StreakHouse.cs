using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models.Facilities;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

partial class Harness
{
    /// <summary>
    /// The streak house (plan 37 · FC2, not part of <c>all</c>): Kiloude City's Battle Maison from its door, its lobby, the
    /// desk's choice of room (<c>fc20</c> to <c>fc22</c>), a triple won and a rotation won through the game's own desk, hall
    /// and battles (<c>fc23</c>, <c>fc24</c>), the single room's host at its 20th battle and the grand room open after
    /// (<c>fc25</c>), the records' board with the streaks (<c>fc26</c>) and the prize counter (<c>fc27</c>). It wins every
    /// battle (the foes are left a single HP) and prints each room's record as it stands.
    /// </summary>
    public void StreakHouseMode()
    {
        var story = game.Story;
        DialogueManager Box() => game.Dialogue;
        GameState State() => game.State;
        void Until(Func<bool> holds, string what, int most = 900)
        {
            for (int i = 0; i < most && !holds(); i++) Frames(1);
            if (!holds()) Console.WriteLine($"  !! never happened: {what}");
        }
        string? battleShot = null;
        void Win()
        {
            var fight = game.Battle;
            for (int guard = 0; guard < 900 && !fight.IsBattleOver; guard++)
            {
                if (fight.HUD.MenuState == BattleMenuState.Main)
                {
                    if (battleShot != null) { Skip(0.5); Shot(battleShot); battleShot = null; }
                    foreach (var foe in fight.EnemyParty?.Members ?? new List<Pokemon>()) foe.CurrentHP = Math.Min(foe.CurrentHP, 1);
                    foreach (var slot in fight.EnemySlots) if (slot.Pokemon is { } met) met.CurrentHP = Math.Min(met.CurrentHP, 1);
                    var lead = fight.PlayerPokemon;
                    // The whole of the player's side kept whole: a rotation's seats are no bench to send in from
                    foreach (var mine in fight.PlayerParty.Members) if (!mine.IsFainted) mine.CurrentHP = mine.MaxHP;
                    fight.SelectMainMenuOption(0);
                    if (lead != null) foreach (var m in lead.Moves) m.CurrentPP = m.MaxPP;
                    int hurts = lead == null ? 0 : lead.Moves.Select((m, i) => (m, i)).Where(p => p.m.Data.Category != MoveCategory.Status && p.m.Data.Type != PokemonType.Ghost)
                        .OrderByDescending(p => p.m.Data.Power).Select(p => p.i).DefaultIfEmpty(0).First();
                    fight.SelectMove(hurts);
                    if (fight.HUD.MenuState == BattleMenuState.SelectTarget) fight.SelectTarget(0);
                }
                else if (fight.HUD.MenuState == BattleMenuState.SwitchPokemon)
                {
                    // Whoever the rules let in next (in a rotation, never one on a seat)
                    var ask = fight.Core.Request;
                    bool sent = false;
                    for (int i = 0; i < fight.PlayerParty.Count && !sent; i++)
                    {
                        var candidate = fight.PlayerParty.Members[i];
                        if (candidate.IsFainted || fight.PlayerSlots.Any(s => s.Pokemon == candidate)) continue;
                        fight.SelectSwitch(i);
                        Frames(1);
                        if (fight.Core.Request != ask) { sent = true; Console.WriteLine($"  !! {candidate.DisplayName} sent in"); break; }
                        // Refused (a rotation's seat): read the refusal and try the next
                        Console.WriteLine($"  {candidate.DisplayName} refused: {fight.CurrentMessage}");
                        for (int k = 0; k < 20 && fight.HUD.MenuState != BattleMenuState.SwitchPokemon && !fight.IsBattleOver; k++) { fight.ConfirmMessage(); Skip(0.2); }
                    }
                    if (!sent) Confirm(fight);
                }
                else Confirm(fight);
                Skip(0.5);
            }
            Until(() => FieldAgain(), "the field again", 1500);
            Frames(10);
        }
        void EnterTeam()
        {
            var screen = game.PartyScreen;
            Frames(60);
            for (int slot = 0; slot < screen.EnterCount; slot++)
            {
                screen.SelectedIndex = slot;
                Frames(2);
                screen.Confirm(game.Party);
                Frames(6);
            }
            screen.Confirm(game.Party);
            Frames(4);
        }
        // Reads on to a line holding the text, answering each question with the next of the answers (the first answer
        // once they have run out), entering the team's first and winning every battle; shoots the question named
        var answers = new Queue<int>();
        (string Text, string Shot)? questionShot = null;
        void ReadTo(string? text, int most = 6000)
        {
            int idle = 0;
            for (int i = 0; i < most; i++)
            {
                if (text != null && Box().IsActive && Translated.Saying(Box().CurrentLine, text)) { Box().FinishLine(); Frames(2); Console.WriteLine($"  said: {text}"); return; }
                idle = !engine.ScriptRunning && !Box().IsActive && State() == GameState.Overworld ? idle + 1 : 0;
                if (text != null && idle > 90) break;
                if (text == null && idle > 30) return;
                if (State() == GameState.Battle) { Win(); continue; }
                if (State() == GameState.PartyMenu) { EnterTeam(); continue; }
                if (engine.Choice.IsOpen)
                {
                    Frames(10);
                    int answer = answers.Count > 0 ? answers.Dequeue() : 0;
                    for (int k = 0; k < answer; k++) engine.Choice.Move(1);
                    Frames(4);
                    if (questionShot is { } q && Box().IsActive && Translated.Saying(Box().CurrentLine, q.Text)) { Shot(q.Shot); questionShot = null; }
                    engine.Choice.Confirm();
                    Frames(4);
                }
                else if (Box().IsActive && Box().IsCurrentLineComplete && !Box().IsQuestion) { Box().Advance(); Frames(2); }
                else Frames(1);
            }
            Console.WriteLine($"  !! never said: {text ?? "the script's end"}");
        }
        void Talk(string map, int x, int y, Direction facing) { At(map, x, y, facing); Frames(2); game.Interact(); }
        void Records(string what)
        {
            var rows = StreakHouse.Rooms.Select(r => (r, game.Frontier.PeekFacility(r.Place, r.Way, r.Hard)))
                .Where(p => p.Item2 != null).Select(p => $"{p.r.Key} {p.Item2!.Streak}/{p.Item2.Best}/{p.Item2.Latest}{(p.Item2.HardOpen ? " grand open" : "")}{(p.Item2.Paused != null ? " paused" : "")}");
            Console.WriteLine($"  {what}: map {game.Map.Name} at ({game.Player.GridX}, {game.Player.GridY}), rooms [{string.Join(", ", rows)}], "
                + $"challenge {(game.Facility == null ? "none" : game.Facility.Room.Key)}, BP {game.Frontier.BattlePoints}, trophies {StreakHouse.Trophies(story)}");
        }

        // A game of Kalos past its Hall of Fame with six strong Pokémon of different species, none holding anything
        engine.NewGameRegion = "Kalos";
        engine.StartNewGame();
        game.State = GameState.Overworld;
        game.SaveSlot = 1;
        game.LocationSign.Hide();
        story.ChooseStarter("Froakie");
        foreach (var (species, level) in new[] { ("Greninja", 80), ("Talonflame", 78), ("Venusaur", 78), ("Lucario", 78), ("Goodra", 78), ("Hawlucha", 78) })
            game.Party.Add(new Pokemon(PokemonDatabase.Get(species)!, level));
        story.Set(StoryState.PokedexFlag);
        story.Set(StoryState.RunningShoesFlag);
        foreach (string badge in RegionDatabase.Get(RegionDatabase.Kalos)!.BadgeNames) story.GiveBadge(RegionDatabase.Kalos, badge);
        story.SetVar("VAR_KALOS_STORY", 31);
        story.Set("KalosHallOfFame");
        story.SetVar("VAR_KALOS_POSTGAME", 3);
        story.Set("FLAG_KALOS_TMV_PASS");
        story.Set("FLAG_KALOS_RIVAL_KILOUDE_BEATEN");
        Frames(2);

        // ---- the house from outside, its lobby and the desk's rooms
        At("Kalos", 592, 352, Direction.Up); Frames(6); Shot("fc20_house_door");
        At("KiloudeCity_BattleMaison", 8, 11, Direction.Up); Frames(6); Shot("fc21_house_lobby");

        // ---- a triple: Enter, the triple room; its first battle at its menu, won, and retired from
        answers = new Queue<int>(new[] { 0, 2 });
        questionShot = ("Which room", "fc22_house_rooms");
        Talk("KiloudeCity_BattleMaison", 4, 5, Direction.Up);
        battleShot = "fc23_house_triple";
        ReadTo("Your Pokémon are whole again");
        Records("a triple won");
        answers = new Queue<int>(new[] { 2 });
        ReadTo(null);
        Records("the triple retired from");

        // ---- a rotation: Enter, the rotation room
        answers = new Queue<int>(new[] { 0, 3 });
        Talk("KiloudeCity_BattleMaison", 4, 5, Direction.Up);
        battleShot = "fc24_house_rotation";
        ReadTo("Your Pokémon are whole again");
        Records("a rotation won");
        // Rest: the challenge is put down and saved
        answers = new Queue<int>(new[] { 1 });
        ReadTo(null);
        Records("the rotation rested");

        // ---- the single room's host at its 20th battle (the streak brought to 19 by hand), and the grand room after
        game.Frontier.Facility(StreakHouse.Place, "single", false).Streak = 19;
        game.Frontier.Facility(StreakHouse.Place, "single", false).Best = 19;
        // The rotation's pause waits at the desk first: it is taken up, then retired from in the hall
        answers = new Queue<int>(new[] { 2 });
        Talk("KiloudeCity_BattleMaison", 4, 5, Direction.Up);
        ReadTo("Your Pokémon are whole again");
        answers = new Queue<int>(new[] { 2 });
        ReadTo(null);
        Records("the rotation taken up again and retired from");
        answers = new Queue<int>(new[] { 0, 0 });
        Talk("KiloudeCity_BattleMaison", 4, 5, Direction.Up);
        ReadTo("steps forward");
        Frames(20);
        Shot("fc25_house_host");
        ReadTo("grand room of this hall is open");
        answers = new Queue<int>(new[] { 2 });
        ReadTo(null);
        Records("the host beaten");

        // ---- the board and the counter
        answers = new Queue<int>(new[] { 2 });
        Talk("KiloudeCity_BattleMaison", 4, 5, Direction.Up);
        for (int i = 0; i < 1200 && State() != GameState.FrontierRecords; i++)
        {
            if (engine.Choice.IsOpen) { Frames(10); int a = answers.Count > 0 ? answers.Dequeue() : 2; for (int k = 0; k < a; k++) engine.Choice.Move(1); engine.Choice.Confirm(); Frames(4); }
            else if (Box().IsActive && Box().IsCurrentLineComplete && !Box().IsQuestion) { Box().Advance(); Frames(2); }
            else Frames(1);
        }
        if (State() != GameState.FrontierRecords) Console.WriteLine("  !! the board never opened");
        Frames(30); Shot("fc26_house_board");
        game.FrontierBoard.Close();
        Frames(4);
        ReadTo(null);
        Talk("KiloudeCity_BattleMaison", 12, 5, Direction.Up);
        Until(() => engine.Choice.IsOpen || !engine.ScriptRunning, "the counter's prizes", 1200);
        for (int i = 0; i < 1200 && !engine.Choice.IsOpen && engine.ScriptRunning; i++)
        {
            if (Box().IsActive && Box().IsCurrentLineComplete && !Box().IsQuestion) { Box().Advance(); Frames(2); }
            else Frames(1);
        }
        Frames(20); Shot("fc27_house_counter");
        engine.Choice.Back();
        ReadTo(null);
        Records("the board and the counter");
    }
}
