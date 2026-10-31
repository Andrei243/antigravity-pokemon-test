using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.UI;

partial class Harness
{
    /// <summary>
    /// The Pokéathlon (plan 15 · E6; not part of <c>all</c>; <c>at*</c>): a game of Johto with a team of four, Route 35's
    /// gate house before the dome's door, the clerk's question (the clerk's script is started by the mode, since nobody
    /// stands at the counter until plan 20 · J13 opens the dome), the course choice, the team with its stats, each
    /// event's briefing, the event under way and its points, the scoreboard and the clerk's word after it. The player's
    /// team plays itself (<see cref="AthlonEvent.Autopilot"/>). It prints the team's stats, the rivals, every event's
    /// points, the totals, the place and the records.
    /// </summary>
    public void AthlonMode()
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
        foreach (var (species, level) in new[] { ("Quilava", 30), ("Pikachu", 22), ("Togepi", 12), ("Machop", 18) })
            team.Add(new Pokemon(PokemonDatabase.Get(species)!, level));
        foreach (var p in team.Members)
            Console.WriteLine($"  {p.Species.Name}: {AthleteStats.Of(p)}");
        DialogueManager Box() => game.Dialogue;
        var screen = game.AthlonScreen;

        void Until(System.Func<bool> holds, string what, int most = 900)
        {
            for (int i = 0; i < most && !holds(); i++) Frames(1);
            if (!holds()) Console.WriteLine($"  !! never happened: {what}");
        }
        void ReadOn(string word = null, string shot = null)
        {
            for (int i = 0; i < 120 && (Box().IsActive || engine.Choice.IsOpen || game.State == GameState.Dialogue || engine.ScriptRunning); i++)
            {
                if (game.State is not (GameState.Overworld or GameState.Dialogue)) break;
                if (engine.Choice.IsOpen)
                {
                    Frames(14);
                    if (word != null && shot != null && Box().VisibleText.Contains(word)) { Shot(shot); shot = null; }
                    engine.Choice.Confirm(); Frames(4);
                    continue;
                }
                if (!Box().IsActive) { Frames(6); continue; }
                Box().FinishLine(); Frames(2);
                if (Box().IsQuestion) { Frames(4); continue; }
                if (word != null && shot != null && Box().VisibleText.Contains(word)) { Shot(shot); shot = null; }
                Box().Advance(); Frames(2);
            }
        }
        void PlayEvent(string under, int frames, string points)
        {
            screen.Confirm();
            Frames(40);
            Shot(under + "_count");
            screen.Play(frames, ev => ev.Autopilot());
            Frames(1);
            Shot(under);
            screen.Play(int.MaxValue, ev => ev.Autopilot());
            Frames(30);
            Shot(points);
            int e = screen.Run!.EventIndex - 1;
            Console.WriteLine($"  {screen.Run.Course.Events[e]}: " + string.Join(", ",
                Enumerable.Range(0, AthlonRun.TeamCount).Select(t => $"{(t == 0 ? "player" : screen.Run.Teams[t].Name)} {screen.Run.PointsOf(e, t)}")));
            screen.Confirm();
            Frames(20);
        }

        // ---- The gate house, before the dome's door (open since plan 20 · J13), the dome's square and its lobby
        At("Route35_NationalParkGate", 6, 4, Direction.Left);
        Frames(20);
        Shot("at01_dome_door");
        At("PokeathlonDome", 42, 27, Direction.Up);
        Frames(30);
        Shot("at01c_dome_square");
        At("PokeathlonDome_1F", 23, 13, Direction.Up);
        Frames(30);
        Shot("at01d_dome_lobby");

        // ---- The clerk's question, across the middle of the desk, and the course choice
        At("PokeathlonDome_1F", 23, 10, Direction.Up);
        Frames(10);
        game.Interact();
        Frames(3);
        ReadOn("enter a course", "at01b_clerk_asks");
        Until(() => game.State == GameState.Athlon, "the Pokéathlon's screen", 300);
        Frames(30);
        Shot("at02_course_choice");

        // ---- The team: Quilava, Pikachu and Machop, the Togepi passed over
        screen.Confirm();
        Frames(20);
        screen.Confirm();
        screen.Move(0, 1);
        screen.Confirm();
        screen.Move(0, 1);
        Frames(10);
        Shot("at03_team");
        screen.Move(0, 1);
        screen.Confirm();
        Frames(30);
        Console.WriteLine("  rivals: " + string.Join("; ", screen.Run!.Teams.Skip(1).Select(t => $"{t.Name} ({t.Knack:0.00}) {string.Join(", ", t.Athletes.Select(a => $"{a.Species} {a.Stats}"))}")));
        Shot("at03b_briefing_hurdle_dash");

        // ---- The three events
        PlayEvent("at04_hurdle_dash", 330, "at04b_hurdle_dash_points");
        Shot("at05a_briefing_block_smash");
        PlayEvent("at05_block_smash", 360, "at05b_block_smash_points");
        Shot("at06a_briefing_ring_drop");
        PlayEvent("at06_ring_drop", 540, "at06b_ring_drop_points");

        // ---- The scoreboard, and the clerk's word after it
        Shot("at07_scoreboard");
        var run = screen.Run;
        Console.WriteLine($"  totals: {string.Join(", ", Enumerable.Range(0, AthlonRun.TeamCount).Select(t => $"{(t == 0 ? "player" : run.Teams[t].Name)} {run.Total(t)}"))}; "
            + $"place {screen.Outcome}, earned {run.Earned}, new record {screen.NewRecord}");
        screen.Confirm();
        Until(() => game.State is GameState.Overworld or GameState.Dialogue, "the field again", 120);
        ReadOn("Athlete Points", "at07b_clerk_after");
        Console.WriteLine($"  records: points {game.Athlon.Points}, best {game.Athlon.BestOf("practice")}, runs {game.Athlon.Runs}, wins {game.Athlon.Wins}");

        // ---- The second batch (plan 15 · E7) on a course of the mode's own, the same three, records of their own
        var batch2 = new AthlonCourse("batch2", new[] { AthlonEventKind.PennantCapture, AthlonEventKind.SnowThrow, AthlonEventKind.DiscCatch, AthlonEventKind.LampJump });
        screen.OpenOn(batch2, team, new AthlonRecords(), new System.Random(77), new[] { 0, 1, 3 });
        game.State = GameState.Athlon;
        // Every athlete's icon baked before the first shot of an event: the rivals are new to this run, and an icon asked
        // for in the frame of a shot is a stand-in in one run and itself in another
        foreach (var athlete in screen.Run!.Teams.SelectMany(t => t.Athletes))
            for (int i = 0; i < 600 && PokemonPlatinumEngine.Graphics.PokemonSprites.GetBaked(athlete.Species, PokemonPlatinumEngine.Graphics.SpriteView.Icon) == null; i++)
                Frames(1);
        Frames(30);
        Shot("at08a_briefing_pennant_capture");
        PlayEvent("at08_pennant_capture", 600, "at08b_pennant_capture_points");
        Shot("at09a_briefing_snow_throw");
        PlayEvent("at09_snow_throw", 420, "at09b_snow_throw_points");
        Shot("at10a_briefing_disc_catch");
        PlayEvent("at10_disc_catch", 590, "at10b_disc_catch_points");
        Shot("at11a_briefing_lamp_jump");
        PlayEvent("at11_lamp_jump", 700, "at11b_lamp_jump_points");
        screen.Confirm();
        Until(() => game.State is GameState.Overworld or GameState.Dialogue, "the field after the second batch", 120);

        engine.NewGameRegion = "Sinnoh";
    }
}
