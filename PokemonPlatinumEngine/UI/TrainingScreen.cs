using System;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.UI;

/// <summary>What the drill screen shows: the stat chosen, the drill's briefing, the drill, its grade, the effort after.</summary>
public enum TrainingPhase { Stats, Briefing, Playing, Grade, Effort }

/// <summary>
/// The effort drills on screen (plan 36 · TR2; style guide, "Training"): opened on one Pokémon by the coach's
/// <c>train drill</c>, it asks which stat to train (six rows, each with the Pokémon's effort and the drill that trains it,
/// a stat the drills refuse told why), briefs the drill, counts three and plays it at thirty frames a second
/// (<see cref="TrainingDrill"/>), then shows its grade and the six effort bars before and after. The effort is given as
/// the drill ends (<see cref="EffortDrills.Award"/>) and the records kept (<see cref="TrainingRecords"/>). It closes with
/// <see cref="Outcome"/>: the grade won and one (1 bronze, 2 silver, 3 gold), or 0 when it was left before a drill was
/// played. Its logic takes no input (<see cref="Move"/>, <see cref="Confirm"/>, <see cref="Cancel"/>,
/// <see cref="Advance"/>, <see cref="Play"/>), so tests and the harness drive it; <see cref="Update"/> reads the keys.
/// </summary>
public sealed class TrainingScreen
{
    /// <summary>The count before a drill: three seconds, and GO for the half second after.</summary>
    public const float CountIn = 3f;

    private const float AppearTime = 0.3f;

    private Random? rng;

    public bool IsActive { get; private set; }
    public TrainingPhase Phase { get; private set; }
    public Pokemon? Pokemon { get; private set; }
    public TrainingRecords Records { get; private set; } = new();

    /// <summary>The stat row under the cursor (an index of <see cref="DrillKinds.Stats"/>).</summary>
    public int Cursor { get; private set; }

    public StatType Stat => DrillKinds.Stats[Math.Clamp(Cursor, 0, DrillKinds.Stats.Length - 1)];

    /// <summary>The drill under way or just played.</summary>
    public TrainingDrill? Drill { get; private set; }

    /// <summary>Why the stat last chosen can't be trained, shown under the rows until the cursor moves; null otherwise.</summary>
    public string? Refusal { get; private set; }

    /// <summary>The grade won and one, or 0 before any (and when the screen was left before a drill).</summary>
    public int Outcome { get; private set; }

    /// <summary>The effort the drill gave, and whether its score is the stat's new best.</summary>
    public int Gained { get; private set; }
    public bool NewBest { get; private set; }

    /// <summary>The six stats' effort and values as they were before the drill and as they are after it.</summary>
    public int[] EffortBefore { get; } = new int[6];
    public int[] EffortAfter { get; } = new int[6];
    public int[] ValueBefore { get; } = new int[6];
    public int[] ValueAfter { get; } = new int[6];

    /// <summary>For tools (as <c>ArcadeBoard.Rig</c>): the seed the next drill is made from in place of a draw; used once.</summary>
    public int? NextSeed { get; set; }

    /// <summary>
    /// For tools (as <c>PoffinCookingScreen.Held</c>): what plays a drill in the keys' place, asked once for each frame
    /// played as time passes (<see cref="Advance"/>); null, the keys play it. The game never sets it.
    /// </summary>
    public Func<TrainingDrill, DrillInput>? Pilot { get; set; }

    private float pilotOwed;

    /// <summary>Seconds since the phase began (its slide-in and the count before a drill).</summary>
    public float PhaseAge { get; private set; }

    public float Appear => Math.Clamp(PhaseAge / AppearTime, 0f, 1f);

    /// <summary>Seconds of the count still to go before the drill's first frame (nought once it runs).</summary>
    public float CountLeft => Phase == TrainingPhase.Playing ? Math.Max(0f, CountIn - PhaseAge) : 0f;

    /// <summary>The input held while a drill plays (the keys' in the game, a tool's otherwise).</summary>
    public DrillInput Input { get; set; }

    /// <summary>The rules the effort is held to (the game's in force unless a tool says).</summary>
    public Ruleset? Rules { get; set; }

    /// <summary>Opens the screen on one Pokémon of the team, at the stat choice.</summary>
    public void Open(Pokemon pokemon, TrainingRecords records, Random random)
    {
        Pokemon = pokemon;
        Records = records;
        rng = random;
        Drill = null;
        Refusal = null;
        Outcome = 0;
        Gained = 0;
        NewBest = false;
        Cursor = 0;
        IsActive = true;
        Enter(TrainingPhase.Stats);
        AudioManager.PlaySound("select");
    }

    private void Enter(TrainingPhase phase)
    {
        Phase = phase;
        PhaseAge = 0f;
    }

    /// <summary>Why a stat can't be trained now, or null.</summary>
    public string? WhyNot(StatType stat) => Pokemon == null ? null : EffortDrills.WhyNot(Pokemon, stat, Rules);

    public void Move(int dx, int dy)
    {
        if (!IsActive || Phase != TrainingPhase.Stats) return;
        int step = dy != 0 ? dy : dx;
        if (step == 0) return;
        int count = DrillKinds.Stats.Length;
        Cursor = ((Cursor + step) % count + count) % count;
        Refusal = null;
        AudioManager.PlaySound("cursor");
    }

    public void Confirm()
    {
        if (!IsActive || Pokemon == null) return;
        switch (Phase)
        {
            case TrainingPhase.Stats:
            {
                if (WhyNot(Stat) is { } why)
                {
                    Refusal = why;
                    AudioManager.PlaySound("error");
                    return;
                }
                int seed = NextSeed ?? (rng ?? Dice.New()).Next();
                NextSeed = null;
                Drill = TrainingDrill.Create(Stat, seed);
                Enter(TrainingPhase.Briefing);
                AudioManager.PlaySound("select");
                break;
            }
            case TrainingPhase.Briefing:
                Input = DrillInput.None;
                pilotOwed = 0f;
                heard = default;
                Enter(TrainingPhase.Playing);
                AudioManager.PlaySound("select");
                break;
            case TrainingPhase.Grade:
                Enter(TrainingPhase.Effort);
                AudioManager.PlaySound("select");
                break;
            case TrainingPhase.Effort:
                AudioManager.PlaySound("select");
                IsActive = false;
                break;
        }
    }

    public void Cancel()
    {
        if (!IsActive) return;
        switch (Phase)
        {
            case TrainingPhase.Stats:
                AudioManager.PlaySound("cancel");
                IsActive = false;
                break;
            case TrainingPhase.Briefing:
                AudioManager.PlaySound("cancel");
                Drill = null;
                Enter(TrainingPhase.Stats);
                break;
            case TrainingPhase.Grade:
            case TrainingPhase.Effort:
                Confirm();
                break;
            // A drill under way is played to its end
        }
    }

    /// <summary>Time passes: the phase's clock, and while a drill plays its frames with <see cref="Input"/>.</summary>
    public void Advance(float dt)
    {
        if (!IsActive) return;
        float before = PhaseAge;
        PhaseAge += dt;
        if (Phase != TrainingPhase.Playing || Drill is not { } drill) return;
        float running = PhaseAge - Math.Max(before, CountIn);
        if (running <= 0f) return;
        if (Pilot != null)
        {
            // A tool's hand: a frame at a time, each with what it says for the drill as it stands
            pilotOwed += running;
            while (pilotOwed >= TrainingDrill.Frame && !drill.IsDone)
            {
                pilotOwed -= TrainingDrill.Frame;
                drill.Step(Pilot(drill));
            }
        }
        else if (drill.Advance(running, Input) > 0) Input = Input with { Tap = false };
        Hear(drill);
        if (drill.IsDone) Finish();
    }

    /// <summary>
    /// Plays the drill's frames one by one, each with the input <paramref name="input"/> gives for the drill as it stands
    /// (for tests and the harness), past the count first. Returns the frames played.
    /// </summary>
    public int Play(int frames, Func<TrainingDrill, DrillInput> input)
    {
        if (!IsActive || Phase != TrainingPhase.Playing || Drill is not { } drill) return 0;
        if (PhaseAge < CountIn) PhaseAge = CountIn;
        int played = 0;
        for (; played < frames && !drill.IsDone; played++)
        {
            drill.Step(input(drill));
            PhaseAge += TrainingDrill.Frame;
            Hear(drill);
        }
        if (drill.IsDone) Finish();
        return played;
    }

    /// <summary>The drill is over: its effort given, the stats reckoned and the records kept.</summary>
    private void Finish()
    {
        var drill = Drill!;
        var p = Pokemon!;
        for (int i = 0; i < 6; i++)
        {
            EffortBefore[i] = EffortRules.Get(p, DrillKinds.Stats[i]);
            ValueBefore[i] = ValueOf(p, DrillKinds.Stats[i]);
        }
        var grade = drill.Grade;
        Gained = EffortDrills.Award(p, drill.Stat, grade, Rules);
        NewBest = Records.Record(drill.Stat, drill.Score, grade);
        for (int i = 0; i < 6; i++)
        {
            EffortAfter[i] = EffortRules.Get(p, DrillKinds.Stats[i]);
            ValueAfter[i] = ValueOf(p, DrillKinds.Stats[i]);
        }
        Outcome = (int)grade + 1;
        Enter(TrainingPhase.Grade);
        AudioManager.PlaySound(grade == DrillGrade.Gold ? "levelup" : "lift_ding");
    }

    /// <summary>A Pokémon's value of a stat, as its summary shows it.</summary>
    public static int ValueOf(Pokemon p, StatType stat) => stat switch
    {
        StatType.HP => p.MaxHP,
        StatType.Attack => p.Attack,
        StatType.Defense => p.Defense,
        StatType.SpAttack => p.SpAttack,
        StatType.SpDefense => p.SpDefense,
        _ => p.Speed
    };

    // The drill is heard as it plays: a point scored and a slip, each as it comes (the game's own sounds stand in)
    private (int Good, int Slip) heard;

    /// <summary>What a drill has done that is heard: the good things and the slips, counted.</summary>
    public static (int Good, string GoodSound, int Slip, string SlipSound) Heard(TrainingDrill drill) => drill switch
    {
        ThrowDrill t => (t.Hits, "coin_land", t.Misses, "bump"),
        GuardDrill g => (g.Score, "hit_weak", g.Stuns, "bump"),
        DashDrill d => (d.Cleared, "ledge", d.Stumbles, "bump"),
        _ => (0, "", 0, "")
    };

    private void Hear(TrainingDrill drill)
    {
        var now = Heard(drill);
        if (now.Good > heard.Good && now.GoodSound.Length > 0) AudioManager.PlaySound(now.GoodSound);
        if (now.Slip > heard.Slip && now.SlipSound.Length > 0) AudioManager.PlaySound(now.SlipSound);
        heard = (now.Good, now.Slip);
    }

    public void Update(float dt)
    {
        if (!IsActive) return;
        if (Phase == TrainingPhase.Playing && Pilot != null)
        {
            Advance(dt);
            return;
        }
        if (Phase == TrainingPhase.Playing)
        {
            int dx = (InputManager.IsActionDown(GameAction.Right) ? 1 : 0) - (InputManager.IsActionDown(GameAction.Left) ? 1 : 0);
            Input = new DrillInput(Tap: Input.Tap || InputManager.IsActionPressed(GameAction.Confirm), Held: InputManager.IsActionDown(GameAction.Confirm), Dx: dx);
            Advance(dt);
            return;
        }
        Advance(dt);
        if (InputManager.IsActionPressed(GameAction.Up)) Move(0, -1);
        else if (InputManager.IsActionPressed(GameAction.Down)) Move(0, 1);
        if (InputManager.IsActionPressed(GameAction.Confirm)) Confirm();
        else if (InputManager.IsActionPressed(GameAction.Cancel)) Cancel();
    }

    public void Draw(int sw, int sh)
    {
        if (IsActive) ModernUi.DrawTraining(sw, sh, this);
    }
}
