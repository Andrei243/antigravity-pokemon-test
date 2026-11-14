using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.Rides;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.UI;

partial class Harness
{
    /// <summary>
    /// The rides (plan 42 · PS7; not part of <c>all</c>; <c>rd*</c>): a new game in Alola, Hau'oli's beach before the surf
    /// club's hut (<c>rd01_hut</c>), the captain's question answered and the course's briefing (<c>rd02_briefing</c>),
    /// then the ride played by hand: carving down the wave's face (<c>rd03_down_the_face</c>), a spin in the air off the
    /// crest (<c>rd04_trick</c>), its straight landing on a press in the window (<c>rd05_landing</c>), a roll landed with
    /// no press, which falls (<c>rd06_fall</c>), the rest ridden by the autopilot to the results (<c>rd07_results</c>) and
    /// the Battle Points (<c>rd08_points</c>), and the captain's word after it (<c>rd09_captain_after</c>). It prints the
    /// riders' scores, combos and falls, the place, what it paid and the points held.
    /// </summary>
    public void RidesMode()
    {
        engine.NewGameRegion = "Alola";
        engine.StartNewGame();
        game.State = GameState.Overworld;
        game.SaveSlot = 1;
        game.LocationSign.Hide();
        Frames(30);
        var screen = game.RideScreen;
        DialogueManager Box() => game.Dialogue;

        void Until(System.Func<bool> holds, string what, int most = 900)
        {
            for (int i = 0; i < most && !holds(); i++) Frames(1);
            if (!holds()) Console.WriteLine($"  !! never happened: {what}");
        }
        void ReadOn(string? word = null, string? shot = null)
        {
            for (int i = 0; i < 120 && (Box().IsActive || engine.Choice.IsOpen || game.State == GameState.Dialogue || engine.ScriptRunning); i++)
            {
                if (game.State is not (GameState.Overworld or GameState.Dialogue)) break;
                if (engine.Choice.IsOpen)
                {
                    Frames(14);
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

        // ---- The beach before the hut, and the captain's question
        At("Melemele", 306, 176, Direction.Down);
        Frames(30);
        game.LocationSign.Hide();
        Shot("rd01_hut");
        game.Interact();
        Frames(3);
        ReadOn();
        Until(() => game.State == GameState.Ride, "the ride's screen", 300);
        // The Mantine's icon baked before the first shot, so no shot shows its stand-in in one run and itself in another
        for (int i = 0; i < 600 && PokemonSprites.GetBaked(SurfCourses.Mount, SpriteView.Icon) == null; i++) Frames(1);
        Frames(30);
        Shot("rd02_briefing");

        // ---- The ride, by hand: carve down, climb, jump, a spin and its landing
        screen.Confirm();
        Frames(4);
        var ride = screen.Ride!;
        var me = ride.Riders[0];
        screen.Play(18, _ => new AthlonInput(Dy: 1));
        Frames(1);
        Shot("rd03_down_the_face");
        void While(System.Func<bool> holds, AthlonInput input)
        {
            for (int f = 0; f < 600 && holds() && !ride.IsDone; f++) screen.Play(1, _ => input);
        }
        While(() => me.Face > 0.05f, new AthlonInput(Dy: 1));
        While(() => me.Face < TrickSurf.CrestLine, new AthlonInput(Dy: -1));
        screen.Play(1, _ => new AthlonInput(Tap: true));
        screen.Play(1, _ => TrickSurf.InputFor(SurfTrick.Spin));
        screen.Play(5, _ => AthlonInput.None);
        Frames(1);
        Shot("rd04_trick");
        // Nothing more until the window is half run, the press, and the water
        while (me.Stance == TrickSurf.Stance.Air && me.Air > ride.Window / 2) screen.Play(1, _ => AthlonInput.None);
        screen.Play(1, _ => new AthlonInput(Tap: true));
        while (me.Stance == TrickSurf.Stance.Air) screen.Play(1, _ => AthlonInput.None);
        screen.Play(4, _ => AthlonInput.None);
        Frames(1);
        Shot("rd05_landing");
        Console.WriteLine($"  first jump: air {me.AirLength}, window {ride.Window}; score {me.Score}, combo {me.Combo}, landing {me.LastLanding}");

        // ---- A roll with no press: a crooked landing, and the fall
        While(() => me.Face > 0.05f, new AthlonInput(Dy: 1));
        While(() => me.Face < TrickSurf.CrestLine, new AthlonInput(Dy: -1));
        screen.Play(1, _ => new AthlonInput(Tap: true));
        screen.Play(1, _ => TrickSurf.InputFor(SurfTrick.Roll));
        while (me.Stance == TrickSurf.Stance.Air) screen.Play(1, _ => AthlonInput.None);
        screen.Play(8, _ => AthlonInput.None);
        Frames(1);
        Shot("rd06_fall");
        Console.WriteLine($"  after the second jump: landing {me.LastLanding}, falls {me.Falls}, down {me.Down}");

        // ---- The rest by the autopilot, the results and the points
        // (the autopilot rides its best, so the run goes on to a first place's points)
        screen.Play(int.MaxValue, r => r.Autopilot(0.97f));
        Frames(30);
        Shot("rd07_results");
        foreach (int t in ride.Ranking)
            Console.WriteLine($"  {(t == 0 ? "player" : ride.Teams[t].Name)}: {ride.Riders[t].Score} (combo {ride.Riders[t].BestCombo}, tricks {ride.Riders[t].TricksLanded}, falls {ride.Riders[t].Falls})");
        Console.WriteLine($"  place {screen.Outcome}, paid {screen.Result.Paid}, new record {screen.Result.NewRecord}");
        screen.Confirm();
        Frames(30);
        Shot("rd08_points");
        screen.Confirm();
        Until(() => game.State is GameState.Overworld or GameState.Dialogue, "the field again", 120);
        ReadOn(screen.Outcome == 1 ? "Battle Point" : "riding", "rd09_captain_after");
        Console.WriteLine($"  Battle Points held {game.Frontier.BattlePoints}; runs {game.Rides.RunsOf("hauoli")}, best {game.Rides.BestOf("hauoli")}");
    }
}
