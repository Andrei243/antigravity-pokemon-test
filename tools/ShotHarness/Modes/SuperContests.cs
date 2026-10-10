using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.SuperContests;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.UI;

partial class Harness
{
    // ---------------------------------------------------------------- the Super Contests (plan 06 · R17a)

    // The Contest Hall's counter and its receptionist's questions, a Cool Contest of the Normal Rank from the line-up
    // to the ribbon (the theme, the dressing-up from the Fashion Case and its clock, each look and its hearts, the dance
    // led and repeated, the acting's moves, judges and voltage, the results' board and the award), the prize handed
    // over, and a practice round's result (sc*; not part of "all"). Everything goes through the game's own TryInteract,
    // scripts, question boxes, team screen and the contest screen's own buttons; it prints what each round gave and what
    // the contest left behind (the ribbon, the Fashion Case, the Pokédex).
    public void SuperContestsMode()
    {
        game.State = GameState.Overworld;
        game.LocationSign.Hide();
        var team = game.Party;
        var fashion = game.FashionCase;
        var contest = game.SuperContest;
        DialogueManager Box() => game.Dialogue;
        void Until(Func<bool> holds, string what, int most = 1800)
        {
            for (int i = 0; i < most && !holds(); i++) Frames(1);
            if (!holds()) Console.WriteLine($"  !! never happened: {what}");
        }
        // Time without a picture of every frame: the contest's own clocks are what matter
        void Pass(Func<bool> until, int most = 12000, Action? each = null)
        {
            for (int i = 0; i < most && !until(); i++)
            {
                each?.Invoke();
                FrameClock.Fixed = ++tick / 60.0;
                engine.Update(1f / 60f);
                if (i % 6 == 5) engine.Draw();
            }
            engine.Draw();
        }
        void Whole() { Until(() => Box().IsActive, "text on the screen"); Box().FinishLine(); Frames(2); }
        void Next() { Box().Advance(); Frames(2); }
        // Reads on to the next question (shooting it under a name when one is given) and answers it with the row asked for
        void Answer(int row, string? shot = null)
        {
            for (int i = 0; i < 40 && !engine.Choice.IsOpen; i++)
            {
                if (game.State == GameState.PartyMenu) return;
                Whole();
                if (engine.Choice.IsOpen) break;
                Next();
            }
            Until(() => engine.Choice.IsOpen, "a question");
            Frames(18);
            if (shot != null) Shot(shot);
            for (int i = 0; i < row; i++) engine.Choice.Move(1);
            engine.Choice.Confirm();
            Frames(4);
        }
        // Reads every line until the game leaves the field's text (for a screen) or the script ends
        void ReadOn(Func<bool> until, string? word = null, string? shot = null)
        {
            for (int i = 0; i < 60 && !until(); i++)
            {
                if (!Box().IsActive) { Frames(2); continue; }
                Whole();
                if (word != null && shot != null && Box().VisibleText.Contains(word)) { Shot(shot); shot = null; }
                Next();
            }
        }
        void Sprites()
        {
            for (int i = 0; i < 2400 && PokemonSprites.Busy; i++) Frames(1);
            Frames(4);
        }

        // ---- The entrant: the first of the team, in good condition, with four Cool moves of different effects; a
        // Fashion Case with something of every kind
        var mine = team.Members[0];
        mine.Moves.Clear();
        foreach (string move in new[] { "Cut", "Double Kick", "Thunderbolt", "Swords Dance" }) mine.Moves.Add(MoveDatabase.Create(move));
        (mine.Cool, mine.Beauty, mine.Cute, mine.Smart, mine.Tough, mine.Sheen) = (60, 20, 6, 8, 30, 40);
        foreach (var (accessory, count) in new[]
                 {
                     ("White Fluff", 3), ("Round Pebble", 2), ("Blue Scale", 1), ("Red Feather", 3), ("Black Moustache", 1), ("Big Leaf", 2),
                     ("Shed Claw", 1), ("Thick Mushroom", 1), ("Snow Crystal", 2), ("Sparks", 2), ("Mystic Fire", 1), ("Wealthy Coin", 1),
                     ("Humming Note", 1), ("Shiny Powder", 2), ("Glitter Powder", 3), ("Red Flower", 2), ("Googly Specs", 1), ("Sweet Candy", 1),
                     ("Confetti", 2), ("Colored Parasol", 1), ("Cape", 1), ("Pink Barrette", 1), ("Blue Balloons", 1), ("Top Hat", 1),
                     ("Flower Stage", 1), ("Turtwig Mask", 1), ("Big Tree", 1), ("Crown", 1)
                 })
            fashion.Add(accessory, count);

        // ---- The lobby, past its first scene, the boy gone from before the counter
        foreach (string flag in new[] { "FLAG_HIDE_CONTEST_HALL_LOBBY_KEIRA", "FLAG_HIDE_CONTEST_HALL_LOBBY_MOM", "FLAG_HIDE_CONTEST_HALL_LOBBY_FANTINA",
                     "FLAG_HIDE_CONTEST_HALL_LOBBY_REPORTER", "FLAG_TALKED_TO_CONTEST_HALL_LOBBY_RICH_BOY", GameEngine.ContestHallVisitedFlag })
            game.Story.Set(flag);
        game.Story.SetVar("VAR_CONTEST_HALL_LOBBY_STATE", 1);
        At("ContestHallLobby", 16, 6, Direction.Up);
        game.ArriveOnMap();
        Frames(20);
        game.LocationSign.Hide();
        Frames(4);
        Shot("sc01_counter");

        // ---- The receptionist: enter, the rank, the contest, the Pokémon
        game.Interact(); Frames(3);
        Answer(0, "sc02_reception");
        Answer(0, "sc03_rank");
        Answer(0, "sc04_contest");
        ReadOn(() => game.State == GameState.PartyMenu);
        Until(() => game.State == GameState.PartyMenu, "the team to choose from");
        game.PartyScreen.SelectedIndex = 0;
        Frames(24);
        Shot("sc05_team");
        game.PartyScreen.Confirm(team); Frames(3);
        ReadOn(() => game.State is GameState.SuperContest or GameState.Transition, "has been entered", "sc06_entered");
        Until(() => game.State == GameState.SuperContest, "the contest");
        Sprites();
        Frames(30);
        var held = contest.Contest!;
        Console.WriteLine($"  the contest: {held.Rank} Rank {held.Category}, theme {SuperContestDatabase.Themes[held.Theme].Name}; judges {string.Join(", ", held.Judges.Select(j => j.Name))}");
        foreach (var c in held.Contestants)
            Console.WriteLine($"    entry {SuperContest.EntryNumber(held.Contestants.ToList().IndexOf(c))}: {c.Trainer}'s {c.Pokemon.DisplayName} ({c.Pokemon.Species.Name}), {string.Join(", ", c.Pokemon.Moves.Select(m => m.Data.Name))}");

        // ---- The line-up and the theme
        Shot("sc07_welcome");
        contest.PressConfirm(); Frames(24);
        Shot("sc08_entry_1");
        for (int i = 0; i < 3; i++) { contest.PressConfirm(); Frames(6); }
        Frames(18);
        Shot("sc09_entry_4");
        contest.PressConfirm(); Frames(24);
        Shot("sc10_theme");

        // ---- The dressing-up: the case, a few things put on, the clock running down
        contest.PressConfirm(); Frames(20);
        Shot("sc11_dressing");
        // The cursor to a place of the case: along its row, then down or up its column
        void Reach(int index)
        {
            const int columns = SuperContestScreen.CaseColumns;
            for (int i = 0; i < 40 && contest.Cursor % columns != index % columns; i++) contest.Move(contest.Cursor % columns < index % columns ? 1 : -1, 0);
            for (int i = 0; i < 40 && contest.Cursor != index; i++) contest.Move(0, contest.Cursor < index ? 1 : -1);
        }
        // What scores for the theme goes on first: the harness reads the theme's points as a player reads a guide
        var theme = SuperContestDatabase.Themes[held.Theme];
        var best = contest.InCase.Select((h, i) => (h.Accessory, Index: i)).OrderByDescending(h => theme.PointsFor(h.Accessory)).Take(contest.Limit).ToList();
        foreach (var (accessory, index) in best.Take(3))
        {
            Reach(index);
            contest.PressConfirm(); Frames(6);
        }
        Frames(12);
        Shot("sc12_dressing_three");
        foreach (var (accessory, index) in best.Skip(3))
        {
            Reach(index);
            contest.PressConfirm(); Frames(6);
        }
        Pass(() => contest.TimeLeft <= 9.4f);
        Shot("sc13_dressing_full_last_seconds");
        contest.PressStart(); Frames(4);
        Console.WriteLine($"  dressed in {string.Join(", ", held.Player.Accessories.Select(SuperContestDatabase.AccessoryName))}");
        foreach (var c in held.Contestants)
            Console.WriteLine($"    {c.Pokemon.DisplayName}: condition {c.ConditionScore} ({c.ConditionHearts} hearts), dressing {c.DressScore} ({c.DressHearts} hearts)");

        // ---- The looks: the first entry and the player's
        Frames(90);
        Shot("sc14_look_entry_1");
        for (int i = 0; i < 3; i++) { contest.PressConfirm(); Frames(6); }
        Frames(110);
        Shot("sc15_look_entry_4");

        // ---- The dance: announced, an opponent leading and the player repeating, the player leading, the points
        contest.PressConfirm(); Frames(24);
        Shot("sc16_dance_announced");
        contest.PressConfirm(); Frames(40);
        Shot("sc17_dance_lead_in");
        var dance = contest.Dance!;
        // The player's part, a frame of the dance ahead: on each whole beat when leading, and each of the lead's steps
        // again half a bar later when not
        int answered = 0, lastBar = -1, lastRotation = -1;
        void Step()
        {
            if (contest.Phase != SuperContestPhase.Dance || dance.IsOver) return;
            if (dance.Bar != lastBar || dance.Rotation != lastRotation) { answered = 0; lastBar = dance.Bar; lastRotation = dance.Rotation; }
            var song = dance.Song;
            int next = dance.Frame + 1;
            if (dance.Lead == 0)
            {
                var (off, slot) = dance.Nearest(next);
                if (off == 0 && slot % 4 == 2 && dance.CanPress) contest.Held = (DanceStep)(1 + slot / 4 % 4);
            }
            else if (answered < dance.Led.Count)
            {
                var (slot, step) = dance.Led[answered];
                int at = (int)(((slot + song.Slots / 2) * (long)(song.Beat / 2) + 5000) / 10000);
                if (next >= at)
                {
                    if (dance.CanPress) contest.Held = step;
                    answered++;
                }
            }
        }
        Pass(() => contest.Phase == SuperContestPhase.Dance && dance.Frame >= dance.Song.BarFrames * 3 / 8, each: Step);
        Shot("sc18_dance_watching");
        Pass(() => contest.Phase == SuperContestPhase.Dance && dance.Frame >= dance.Song.BarFrames * 7 / 8, each: Step);
        Shot("sc19_dance_repeating");
        Pass(() => dance.Lead == 0 && contest.Phase == SuperContestPhase.Dance && dance.Frame >= dance.Song.BarFrames * 3 / 8, each: Step);
        Shot("sc20_dance_leading");
        Pass(() => dance.Lead == 0 && contest.Phase == SuperContestPhase.Dance && dance.Bar == 1 && dance.Frame >= dance.Song.BarFrames * 3 / 4, each: Step);
        Shot("sc21_dance_others_repeat");
        Pass(() => contest.Phase == SuperContestPhase.DanceScores, each: Step);
        Frames(20);
        Shot("sc22_dance_points");
        Console.WriteLine($"  the dance: {string.Join(", ", held.Contestants.Select(c => $"{c.Pokemon.DisplayName} {c.DanceScore}"))}");

        // ---- The acting: announced, the moves, the judges, a turn playing, the second turn's barred move, the points
        contest.PressConfirm(); Frames(24);
        Shot("sc23_acting_announced");
        contest.PressConfirm(); Frames(24);
        Shot("sc24_acting_moves");
        var acting = contest.Acting!;
        Console.WriteLine($"  the acting's first order: {string.Join(", ", acting.Order.Select(c => held.Contestants[c].Pokemon.DisplayName))}");
        // Thunderbolt first (the third move), for the head judge
        contest.Move(0, 1); contest.Move(0, 1); Frames(10);
        contest.PressConfirm(); Frames(4);
        contest.Move(1, 0); Frames(20);
        Shot("sc25_acting_judges");
        contest.PressConfirm(); Frames(30);
        Shot("sc26_acting_first_act");
        // On through the turn, a shot at the first voltage that moves and at the first judge's points
        bool voltageShot = false, judgeShot = false, effectShot = false;
        void TurnOn(string? turnShot = null)
        {
            for (int i = 0; i < 80 && contest.Phase == SuperContestPhase.ActingShow; i++)
            {
                Frames(24);
                if (contest.Event is { } e)
                {
                    if (!effectShot && e.Step == ActingStep.Effect) { Shot("sc27_acting_effect"); effectShot = true; }
                    if (!voltageShot && e.Step == ActingStep.Voltage && e.Change > 0) { Shot("sc28_acting_voltage"); voltageShot = true; }
                    if (!judgeShot && e.Step == ActingStep.Judge) { Shot("sc29_acting_judge"); judgeShot = true; }
                }
                contest.PressConfirm(); Frames(2);
            }
        }
        TurnOn();
        Frames(24);
        Shot("sc30_acting_turn_2");
        Console.WriteLine($"  after turn 1: totals {string.Join(", ", acting.Total)}; voltage {string.Join(", ", acting.Voltage)}; next order {string.Join(", ", acting.Order)}");
        // The other turns: each move in turn, whichever may be shown, to a judge in turn
        for (int turn = 1; turn < SuperContestActing.Turns && contest.Phase == SuperContestPhase.ActingMove; turn++)
        {
            int pick = Enumerable.Range(0, mine.Moves.Count).First(m => acting.CanUse(0, mine.Moves[(turn + m) % mine.Moves.Count].Data));
            pick = (turn + pick) % mine.Moves.Count;
            while (contest.MoveCursor != pick) contest.Move(0, 1);
            contest.PressConfirm(); Frames(4);
            for (int j = 0; j < turn % 3; j++) contest.Move(1, 0);
            contest.PressConfirm(); Frames(6);
            TurnOn();
            Frames(6);
            Console.WriteLine($"  after turn {turn + 1}: totals {string.Join(", ", acting.Total)}; voltage {string.Join(", ", acting.Voltage)}");
        }
        Until(() => contest.Phase == SuperContestPhase.ActingScores, "the acting's end");
        Frames(24);
        Shot("sc31_acting_points");

        // ---- The results: each round's bars, the winner, the award
        contest.PressConfirm(); Frames(50);
        Shot("sc32_results_visual");
        contest.PressConfirm(); contest.PressConfirm(); Frames(50);
        Shot("sc33_results_dance");
        contest.PressConfirm(); contest.PressConfirm(); Frames(100);
        Shot("sc34_results_acting");
        contest.PressConfirm(); Frames(40);
        Shot("sc35_winner");
        foreach (var c in held.Standing)
            Console.WriteLine($"    {c.Placement + 1}. {c.Pokemon.DisplayName}: shares {string.Join("/", c.Shares)}, bars {string.Join("+", c.Bars)} = {c.Total}");
        contest.PressConfirm(); Frames(40);
        Shot("sc36_award");
        contest.PressConfirm(); Frames(4);
        // The board stays up while the screen goes dark
        Frames(6);
        Shot("sc36b_leaving");

        // ---- Back at the counter: the prize, and what the contest left
        Until(() => game.State is GameState.Overworld or GameState.Dialogue, "the lobby again");
        ReadOn(() => game.State == GameState.Overworld && !Box().IsActive && !engine.ScriptRunning, "received the", "sc37_prize");
        Until(() => game.State == GameState.Overworld && !Box().IsActive, "the field again");
        var outcome = game.ContestOutcome;
        Console.WriteLine($"  the outcome: place {outcome?.Placement + 1}, won {outcome?.Won}, ribbon {outcome?.NewRibbon ?? "none"}, prize {outcome?.Prize ?? "none"}");
        Console.WriteLine($"  {mine.DisplayName}'s ribbons: {string.Join(", ", mine.Ribbons)}; friendship {mine.Friendship}; the case holds {fashion.Total} accessories");
        Console.WriteLine($"  seen in the Pokédex: {string.Join(", ", held.Contestants.Skip(1).Select(c => $"{c.Pokemon.Species.Name} {game.Pokedex.IsSeen(c.Pokemon.Species.DexNumber)}"))}");

        // ---- The practice counter: a Visual Competition by itself, and how it went
        At("ContestHallLobby", 27, 6, Direction.Up);
        Frames(6);
        game.Interact(); Frames(3);
        Answer(0, "sc38_practice");
        Answer(0, "sc39_practice_round");
        Answer(1);
        ReadOn(() => game.State == GameState.PartyMenu);
        Until(() => game.State == GameState.PartyMenu, "the team to choose from");
        game.PartyScreen.SelectedIndex = 0;
        game.PartyScreen.Confirm(team); Frames(3);
        ReadOn(() => game.State is GameState.SuperContest or GameState.Transition);
        Until(() => game.State == GameState.SuperContest, "the practice round");
        Sprites();
        for (int i = 0; i < 40 && contest.Phase != SuperContestPhase.Dressing; i++) { contest.PressConfirm(); Frames(3); }
        contest.PressConfirm(); Frames(4);
        contest.PressStart(); Frames(4);
        for (int i = 0; i < 40 && contest.Phase != SuperContestPhase.PracticeResult; i++) { contest.PressConfirm(); Frames(3); }
        Frames(100);
        Shot("sc40_practice_result");
        Console.WriteLine($"  the practice: {string.Join(", ", contest.Contest!.Contestants.Select(c => $"{c.Pokemon.DisplayName} {c.VisualHearts}"))}; {contest.Contest.Player.Placement} did better");
        contest.PressConfirm(); Frames(4);
        Until(() => game.State is GameState.Overworld or GameState.Dialogue, "the lobby again");
        // "Practise again?": no, thanks
        Answer(0, "sc41_practice_again");
        ReadOn(() => game.State == GameState.Overworld && !Box().IsActive && !engine.ScriptRunning);
        Console.WriteLine($"  back in the lobby: {game.State}; ribbons {mine.Ribbons.Count}");
    }
}
