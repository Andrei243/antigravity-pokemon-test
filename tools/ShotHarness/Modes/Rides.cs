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
        // The riding race (plan 42 · PS8) is a game of its own in Kalos: RIDES_PART=race runs it alone
        if (Environment.GetEnvironmentVariable("RIDES_PART") == "race") { RaceRidesPart(); return; }
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

    /// <summary>
    /// The riding race (plan 42 · PS8; <c>RIDES_PART=race</c>; <c>rd20</c> to <c>rd27</c>): a new game in Kalos with a Tauros
    /// in the team, the stable beside Route 12's road (<c>rd20_stable</c>), the track's briefing (<c>rd21_briefing</c>), the
    /// count and the start (<c>rd22_start</c>), a rock cleared in the air (<c>rd23_jump</c>), a stumble on the ground
    /// (<c>rd24_stumble</c>), mud (<c>rd25_mud</c>) and a turn leant into (<c>rd26_turn</c>), the rest ridden by the
    /// autopilot to the results (<c>rd27_results</c>) and the points (<c>rd28_points</c>). It prints each rider's time,
    /// stumbles and turns, the place, what it paid and the points held.
    /// </summary>
    void RaceRidesPart()
    {
        engine.NewGameRegion = "Kalos";
        engine.StartNewGame();
        game.State = GameState.Overworld;
        game.SaveSlot = 1;
        game.LocationSign.Hide();
        game.Party.Add(new Pokemon(PokemonPlatinumEngine.Data.PokemonDatabase.Get("Tauros")!, 30));
        Frames(30);
        var screen = game.RideScreen;
        DialogueManager Box() => game.Dialogue;
        void Until(System.Func<bool> holds, string what, int most = 900)
        {
            for (int i = 0; i < most && !holds(); i++) Frames(1);
            if (!holds()) Console.WriteLine($"  !! never happened: {what}");
        }
        void ReadOn()
        {
            for (int i = 0; i < 120 && (Box().IsActive || engine.Choice.IsOpen || game.State == GameState.Dialogue || engine.ScriptRunning); i++)
            {
                if (game.State is not (GameState.Overworld or GameState.Dialogue)) break;
                if (engine.Choice.IsOpen) { Frames(14); engine.Choice.Confirm(); Frames(4); continue; }
                if (!Box().IsActive) { Frames(6); continue; }
                Box().FinishLine(); Frames(2);
                if (Box().IsQuestion) { Frames(4); continue; }
                Box().Advance(); Frames(2);
            }
        }

        At("Kalos", 220, 482, Direction.Up);
        Frames(30);
        game.LocationSign.Hide();
        Shot("rd20_stable");
        game.Interact();
        Frames(3);
        ReadOn();
        Until(() => game.State == GameState.Ride, "the race's screen", 300);
        for (int i = 0; i < 600 && PokemonSprites.GetBaked("Tauros", SpriteView.Icon) == null; i++) Frames(1);
        Frames(30);
        Shot("rd21_briefing");

        screen.Confirm();
        Frames(4);
        var race = screen.Race!;
        var me = race.Racers[0];
        screen.PlayRace(10, _ => new AthlonInput(Held: true));
        Frames(1);
        Shot("rd22_start");
        void Until3(System.Func<bool> holds, AthlonInput input)
        {
            for (int f = 0; f < 3000 && holds() && !race.IsDone; f++) screen.PlayRace(1, _ => input);
        }
        // Up to a rock, pushing, and the jump a little before it: cleared in the air
        float NextRock(float from) => race.Track.Obstacles.First(o => o.Kind == RaceObstacleKind.Rock && o.At > from).At;
        float rock = NextRock(me.Distance);
        Until3(() => me.Distance < rock - 60, new AthlonInput(Held: true));
        screen.PlayRace(1, _ => new AthlonInput(Tap: true));
        Until3(() => me.Distance < rock + 4, AthlonInput.None);
        Frames(1);
        Shot("rd23_jump");
        // The next rock met on the ground: a stumble
        float rock2 = NextRock(me.Distance);
        Until3(() => me.Distance < rock2 + 6, AthlonInput.None);
        Frames(1);
        Shot("rd24_stumble");
        // On to the mud: a push through it
        var mud = race.Track.Obstacles.First(o => o.Kind == RaceObstacleKind.Mud && o.At + o.Length > me.Distance);
        Until3(() => me.Distance < mud.At + mud.Length / 2f, new AthlonInput(Held: true));
        Frames(1);
        Shot("rd25_mud");
        // And a turn, leant into
        var turn = race.Track.Obstacles.First(o => o.Kind == RaceObstacleKind.Turn && o.At > me.Distance);
        Until3(() => me.Distance < turn.At, new AthlonInput(Dx: turn.Dir));
        Frames(1);
        Shot("rd26_turn");
        Console.WriteLine($"  by hand: stumbles {me.Stumbles}, cleared {me.Cleared}, turns leant {me.TurnsLeant}, missed {me.TurnsMissed}");

        screen.PlayRace(int.MaxValue, r => r.Autopilot(0.97f));
        Frames(30);
        Shot("rd27_results");
        foreach (int t in race.Ranking)
            Console.WriteLine($"  {(t == 0 ? "player" : race.Teams[t].Name)}: {(race.TimeOf(t) is { } s ? s.ToString("0.0") + " s" : "not home")} (stumbles {race.Racers[t].Stumbles}, turns leant {race.Racers[t].TurnsLeant})");
        Console.WriteLine($"  place {screen.Outcome}, paid {screen.Result.Paid}, new record {screen.Result.NewRecord}");
        screen.Confirm();
        Frames(30);
        Shot("rd28_points");
        screen.Confirm();
        Until(() => game.State is GameState.Overworld or GameState.Dialogue, "the field again", 120);
        ReadOn();
        Console.WriteLine($"  Battle Points held {game.Frontier.BattlePoints}; runs {game.Rides.RunsOf("race:blossom")}, best {game.Rides.BestOf("race:blossom")}");
    }
}
