using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.UI;

partial class Harness
{
    /// <summary>
    /// The Bug-Catching Contest (plan 15 · E5; not part of <c>all</c>; <c>ct*</c>): a game of Johto on a Tuesday, the
    /// officer of Route 36's gate offering a place, the park with the contest's corner, a battle with the Sport Ball in
    /// place of the bag and its catch, a second catch and the question of which to keep, the twenty minutes run out, the
    /// judging's board told place by place and the prize. It prints the team at each step, the five others drawn, every
    /// catch, the scores and the place.
    /// </summary>
    public void BugContestMode()
    {
        engine.NewGameRegion = "Johto";
        engine.StartNewGame();
        game.State = GameState.Overworld;
        game.SaveSlot = 1;
        game.LocationSign.Hide();
        engine.StartScript("common.JohtoOpeningDone");
        Frames(4);
        var team = game.Party;
        team.Clear();
        team.Add(new Pokemon(PokemonDatabase.Get("Quilava")!, 30));
        team.Add(new Pokemon(PokemonDatabase.Get("Togepi")!, 12));
        var contest = game.BugContest;
        DialogueManager Box() => game.Dialogue;

        void Until(System.Func<bool> holds, string what, int most = 900)
        {
            for (int i = 0; i < most && !holds(); i++) Frames(1);
            if (!holds()) Console.WriteLine($"  !! never happened: {what}");
        }
        void ReadOn(string word = null, string shot = null, params int[] answers)
        {
            int asked = 0;
            for (int i = 0; i < 120 && (Box().IsActive || engine.Choice.IsOpen || game.State == GameState.Dialogue || engine.ScriptRunning); i++)
            {
                if (game.State is not (GameState.Overworld or GameState.Dialogue)) break;
                if (engine.Choice.IsOpen)
                {
                    Frames(14);
                    int answer = asked < answers.Length ? answers[asked] : 0;
                    asked++;
                    for (int k = 0; k < answer; k++) engine.Choice.Move(1);
                    Frames(2);
                    if (word != null && shot != null && Box().VisibleText.Contains(word)) { Shot(shot); shot = null; }
                    engine.Choice.Confirm(); Frames(4);
                    continue;
                }
                if (!Box().IsActive) { Frames(6); continue; }
                Until(() => Box().IsActive, "text on the screen");
                Box().FinishLine(); Frames(2);
                if (Box().IsQuestion) { Frames(4); continue; }
                if (word != null && shot != null && Box().VisibleText.Contains(word)) { Shot(shot); shot = null; }
                Box().Advance(); Frames(2);
            }
        }
        // A battle of the park: thrown at until caught, the Sport Balls counted
        void Catch(string species, int level, string menuShot = null, string caughtShot = null)
        {
            game.StartWildBattle(new WildEncounterEntry { SpeciesName = species, MinLevel = level, MaxLevel = level });
            Until(() => game.State == GameState.Battle && game.Battle != null, "the battle", 300);
            var b = game.Battle;
            ToMainMenu(b);
            if (menuShot != null) Shot(menuShot);
            for (int throws = 0; throws < 20 && !b.IsBattleOver; throws++)
            {
                if (b.HUD.MenuState == BattleMenuState.Main) { b.SelectMainMenuOption(1); Skip(0.4); }
                for (int i = 0; i < 20 && b.HUD.MenuState != BattleMenuState.Main && !b.IsBattleOver; i++) { Confirm(b); Skip(0.6); }
            }
            if (caughtShot != null) Shot(caughtShot);
            for (int i = 0; i < 20 && game.State == GameState.Battle; i++) { if (!b.IsBattleOver || b.HUD.MenuState == BattleMenuState.Message) Confirm(b); Skip(0.5); }
            Until(() => game.State is GameState.Overworld or GameState.Dialogue, "the field again", 600);
            Console.WriteLine($"  {species} Lv. {level}: caught {b.Caught?.Species.Name ?? "nothing"}, Sport Balls {contest.Balls}, kept {contest.Caught?.Species.Name ?? "none"}, waiting {contest.Pending?.Species.Name ?? "none"}");
        }

        // A Tuesday by day (the harness's own date is put back at the end)
        var date = GameClock.FixedDate;
        var tuesday = (date ?? new DateTime(2026, 10, 20)).Date;
        while (tuesday.DayOfWeek != DayOfWeek.Tuesday) tuesday = tuesday.AddDays(1);
        GameClock.FixedDate = tuesday;
        engine.Settings.TimeOfDay = TimeOfDay.Day;
        engine.ApplySettings(window: false);

        // ---- The gate's counter
        At("Route36_NationalParkGate", 5, 6, Direction.Up);
        Frames(6);
        game.Interact(); Frames(3);
        ReadOn("take part", "ct01_gate_asks", 0);
        Console.WriteLine($"contest: on {contest.Active}, team {string.Join(", ", team.Members.Select(p => p.Species.Name))}, set aside {contest.Team.Count}, "
            + $"others {string.Join(", ", contest.Entrants.Select(e => $"{contest.NameOf(e.Id)} ({e.Species} {e.Score})"))}");
        Until(() => game.Map.Name == GameEngine.BugContestPark && game.State == GameState.Overworld, "the park", 900);
        game.LocationSign.Hide();
        Frames(20);
        Shot("ct02_park_entered");
        At(GameEngine.BugContestPark, 45, 52, Direction.Up);
        Frames(20);
        Shot("ct03_park_corner");

        // ---- A battle with the Sport Ball, and a second catch
        Catch("Scyther", 14, "ct04_battle_sport_ball", "ct04b_caught");
        Catch("Caterpie", 12);
        Until(() => Box().IsActive, "the question of which to keep", 300);
        ReadOn("instead", "ct05_swap_asked", 1);
        Console.WriteLine($"  after the question: kept {contest.Caught?.Species.Name}");

        // ---- The minutes run out: the gate and the judging
        contest.Tick(contest.SecondsLeft + 1f);
        Until(() => engine.ScriptRunning, "the time-up script", 120);
        for (int i = 0; i < 2400 && game.State != GameState.BugContestResults; i++)
        {
            if (Box().IsActive && Box().IsCurrentLineComplete && !Box().IsQuestion) { Box().Advance(); Frames(2); }
            else Frames(1);
        }
        var board = game.BugContestResults;
        Frames(20);
        Shot("ct06_results_judging");
        for (int told = 0; told < BugContestResultsScreen.Told; told++) { board.Confirm(); Frames(30); }
        Shot("ct06b_results");
        Console.WriteLine($"  judged: score {contest.Score}, place {contest.Placement}, prize {contest.Prize}; "
            + string.Join(", ", contest.Ranking.Select(r => $"{(r.Id == BugContest.Player ? "player" : contest.NameOf(r.Id))} {r.Species} {r.Score}")));
        board.Confirm(); Frames(6);
        ReadOn("Your prize", "ct07_prize");
        Console.WriteLine($"  after: on {contest.Active}, team {string.Join(", ", team.Members.Select(p => p.Species.Name))}, at {game.Map.Name}");

        GameClock.FixedDate = date;
        engine.NewGameRegion = "Sinnoh";
    }
}
