using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.UI;

partial class Harness
{
    /// <summary>
    /// Training (plan 36; not part of <c>all</c>; <c>TRAINING_PART=skills</c> runs one part). TR1, <c>skills</c> (tr01, tr02):
    /// a Bidoof played through two drills by the autopilot (the throw to gold for its Attack, the dash to silver for its Speed),
    /// its SKILLS page with the effort the drills gave and then with its effort full. It prints each drill's score and grade, the
    /// points given and the stats before and after. Later parts (the hall, the stall, the keeper, the powers) add their own.
    /// </summary>
    public void TrainingMode()
    {
        string part = Environment.GetEnvironmentVariable("TRAINING_PART") ?? "";
        if (part == "" || part == "skills") TrainingSkills();
        if (part == "" || part == "hall") TrainingHall();
    }

    /// <summary>
    /// TR2, <c>hall</c> (tr10 to tr19b): Sunyshore City's training hall. The alley's head with the billboard (tr10), a Bidoof
    /// walked in (tr11), the head coach spoken to (tr12), the drill screen on the stat choice (tr13), the throw's briefing
    /// (tr14) and the throw under way (tr15), played by the screen's own clock with the autopilot's hand at a skill that
    /// golds (<c>TrainingScreen.Pilot</c>), its grade (tr18) and the effort before and after (tr19), and SKILLS with the 24
    /// points (tr19b); then the guard (tr16) and the dash (tr17) under way from the coach again. It prints each drill's seed,
    /// score and grade, the effort, and what the coach handed over.
    /// </summary>
    void TrainingHall()
    {
        DialogueManager Box() => game.Dialogue;
        void Until(Func<bool> holds, string what, int most = 900)
        {
            for (int i = 0; i < most && !holds(); i++) Frames(1);
            if (!holds()) Console.WriteLine($"  !! never happened: {what}");
        }
        void Walk(Direction way, int tiles = 1)
        {
            for (int i = 0; i < tiles && !engine.ScriptRunning && game.State == GameState.Overworld; i++)
            {
                engine.Steering = (way, false);
                for (int f = 0; f < 30 && !game.Player.IsMoving && !engine.ScriptRunning && game.State == GameState.Overworld; f++) Frames(1);
                engine.Steering = null;
                for (int f = 0; f < 90 && game.Player.IsMoving; f++) Frames(1);
                Frames(1);
            }
        }
        var screen = game.TrainingScreen;
        // Talks to the coach and reads on, choosing A DRILL, the team's first, and at the drill screen the stat asked for;
        // the drill is shot once when `under way` frames into it, and the rest of the drill played by the screen's own clock
        void Drill(StatType stat, string? coachShot, string? choiceShot, string? briefingShot, string playShot, string? gradeShot, string? effortShot)
        {
            int seed = 1;
            while (seed < 400 && TrainingDrill.Create(stat, seed).PlayItself(0.9f) != DrillGrade.Gold) seed++;
            game.Player.Facing = Direction.Up;
            game.Interact();
            Frames(3);
            bool atChoice = false;
            for (int i = 0; i < 80 && (Box().IsActive || engine.Choice.IsOpen || game.State is GameState.Dialogue or GameState.PartyMenu or GameState.Training || engine.ScriptRunning); i++)
            {
                if (game.State == GameState.Training)
                {
                    if (screen.Phase == TrainingPhase.Stats && !atChoice)
                    {
                        atChoice = true;
                        while (screen.Stat != stat) screen.Move(0, 1);
                        Frames(30);
                        if (choiceShot != null) Shot(choiceShot);
                        screen.NextSeed = seed;
                        screen.Pilot = d => d.Autopilot(0.9f);
                        screen.Confirm(); Frames(30);
                        if (briefingShot != null) Shot(briefingShot);
                        screen.Confirm();
                        Frames(200);
                        screen.Play(300 - screen.Drill!.Frames, d => d.Autopilot(0.9f));
                        Frames(2);
                        Shot(playShot);
                        Until(() => screen.Phase != TrainingPhase.Playing, "the drill's end", 60 * 40);
                        var d0 = screen.Drill!;
                        Console.WriteLine($"  {d0.Kind}{(d0.Special ? " (special)" : "")} for {stat}: seed {seed}, score {d0.Score}, {d0.Grade}, +{screen.Gained} effort, new best {screen.NewBest}");
                        Frames(30);
                        if (gradeShot != null) Shot(gradeShot);
                        screen.Confirm(); Frames(30);
                        if (effortShot != null) Shot(effortShot);
                        screen.Confirm(); Frames(4);
                        screen.Pilot = null;
                    }
                    else Frames(1);
                    continue;
                }
                if (game.State == GameState.PartyMenu)
                {
                    game.PartyScreen.SelectedIndex = 0;
                    game.PartyScreen.Confirm(game.Party); Frames(3);
                    continue;
                }
                if (engine.Choice.IsOpen) { engine.Choice.Confirm(); Frames(4); continue; }
                if (!Box().IsActive) { Frames(1); continue; }
                Box().FinishLine(); Frames(2);
                if (coachShot != null && Box().VisibleText.Contains("Welcome")) { Shot(coachShot); coachShot = null; }
                if (Box().VisibleText.Contains("received")) Console.WriteLine($"  handed over: {Box().VisibleText}");
                Box().Advance(); Frames(2);
            }
            Until(() => game.State == GameState.Overworld && !Box().IsActive && !engine.ScriptRunning, "the field again");
        }

        party.Clear();
        var bidoof = new Pokemon(PokemonDatabase.Get("Bidoof")!, 50);
        party.Add(bidoof);
        game.Pokedex.RegisterSeen(bidoof.Species.DexNumber);
        game.Pokedex.RegisterCaught(bidoof.Species.DexNumber);
        game.Training.Clear();
        Console.WriteLine($"training hall: Bidoof's effort {EffortRules.Total(bidoof)}, Attack {bidoof.Attack}");

        // ---- The head of the alley between Sunyshore City's north houses: the way in and its billboard
        game.State = GameState.Overworld;
        At("Sinnoh", 879, 744, Direction.Up);
        game.LocationSign.Hide();
        Frames(20);
        Shot("tr10_hall_door");

        // ---- Walked in
        Walk(Direction.Up, 3);
        Until(() => game.Map.Name == GameEngine.SunyshoreTrainingHall && game.State == GameState.Overworld && !engine.ScriptRunning, "the hall");
        game.LocationSign.Hide();
        Frames(30);
        Shot("tr11_hall");
        Console.WriteLine($"  in the hall at {game.Player.GridX},{game.Player.GridY}");

        // ---- Across the counter from the head coach, and the throw to gold
        Walk(Direction.Right, 4);
        Walk(Direction.Up, 4);
        Console.WriteLine($"  before the coach at {game.Player.GridX},{game.Player.GridY}");
        Drill(StatType.Attack, "tr12_coach", "tr13_stat_choice", "tr14_briefing", "tr15_throw", "tr18_grade", "tr19_effort_after");
        Console.WriteLine($"  after the throw: Attack effort {bidoof.EvAttack}, Attack {bidoof.Attack}; golds {game.Training.Golds}, Protein in the bag {game.Bag.GetQuantity(ItemDatabase.Get("Protein")!)}");
        ShowSkills(bidoof, "tr19b_skills_after");

        // ---- The guard and the dash under way
        Drill(StatType.Defense, null, null, null, "tr16_guard", null, null);
        Drill(StatType.Speed, null, null, null, "tr17_dash", null, null);
        Console.WriteLine($"  after three drills: effort HP {bidoof.EvHP}, Attack {bidoof.EvAttack}, Defense {bidoof.EvDefense}, Speed {bidoof.EvSpeed}; drills {game.Training.Drills}, golds {game.Training.Golds}");
    }

    // A drill played by the autopilot from a seed, then its grade's effort given
    int DrillFor(Pokemon pokemon, StatType stat, int seed, float knack)
    {
        var drill = TrainingDrill.Create(stat, seed);
        var grade = drill.PlayItself(knack);
        int before = EffortRules.Get(pokemon, stat);
        int gain = EffortDrills.Award(pokemon, stat, grade);
        Console.WriteLine($"  {drill.Kind} for {stat}: seed {seed}, score {drill.Score}, {grade} -> +{gain} effort ({before} -> {EffortRules.Get(pokemon, stat)})");
        return gain;
    }

    void ShowSkills(Pokemon pokemon, string shot)
    {
        var screen = game.PartyScreen;
        game.State = GameState.PartyMenu;
        screen.Open();
        screen.ShowSummary = true;
        while (screen.SummaryPage != PartyScreen.SkillsPage) screen.TurnSummaryPage(1);
        Frames(40); Shot(shot);
        screen.ShowSummary = false;
        game.State = GameState.Overworld;
    }

    void TrainingSkills()
    {
        Console.WriteLine($"training: rules in force {(Ruleset.Current == Ruleset.Modern ? "modern" : "platinum")}, a stat holds {Ruleset.Current.EvStatCap} effort at most, {EffortRules.MaxTotal} in all");
        party.Clear();
        var bidoof = new Pokemon(PokemonDatabase.Get("Bidoof")!, 50);
        party.Add(bidoof);
        game.Pokedex.RegisterSeen(bidoof.Species.DexNumber);
        game.Pokedex.RegisterCaught(bidoof.Species.DexNumber);
        Console.WriteLine($"  before: Attack {bidoof.Attack}, Speed {bidoof.Speed}, effort {EffortRules.Total(bidoof)}");

        // Two drills, as a player would play them: the throw well, the dash fairly
        int seed = 1;
        while (TrainingDrill.Create(StatType.Attack, seed).PlayItself(0.9f) != DrillGrade.Gold) seed++;
        DrillFor(bidoof, StatType.Attack, seed, 0.9f);
        seed = 1;
        while (TrainingDrill.Create(StatType.Speed, seed).PlayItself() != DrillGrade.Silver) seed++;
        DrillFor(bidoof, StatType.Speed, seed, TrainingDrill.DefaultKnack);
        Console.WriteLine($"  after: Attack {bidoof.Attack}, Speed {bidoof.Speed}, effort {EffortRules.Total(bidoof)} (HP {bidoof.EvHP}, Attack {bidoof.EvAttack}, Defense {bidoof.EvDefense}, Sp. Atk {bidoof.EvSpAttack}, Sp. Def {bidoof.EvSpDefense}, Speed {bidoof.EvSpeed})");
        ShowSkills(bidoof, "tr01_skills_effort");

        // Full: two stats at the most a stat holds and the total at 510, one at its limit shown in gold
        bidoof.EvHP = 0;
        bidoof.EvAttack = Ruleset.Current.EvStatCap;
        bidoof.EvDefense = 0;
        bidoof.EvSpAttack = 0;
        bidoof.EvSpDefense = 6;
        bidoof.EvSpeed = Ruleset.Current.EvStatCap;
        bidoof.ReckonStats();
        Console.WriteLine($"  nearly full: effort {EffortRules.Total(bidoof)}; refusals: {EffortDrills.WhyNot(bidoof, StatType.Attack)} / {EffortDrills.WhyNot(bidoof, StatType.Defense) ?? "none"}");
        bidoof.EvDefense = 510 - EffortRules.Total(bidoof);
        bidoof.ReckonStats();
        Console.WriteLine($"  at 510: effort {EffortRules.Total(bidoof)}; {EffortDrills.WhyNot(bidoof, StatType.SpAttack)}");
        ShowSkills(bidoof, "tr02_skills_effort_full");
    }
}
