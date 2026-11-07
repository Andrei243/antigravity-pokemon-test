using System;
using System.Collections.Generic;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models;

/// <summary>The three small games that train effort (plan 36 · TR1): a throw at a target, a guard against pellets, a dash.</summary>
public enum DrillKind { Throw, Guard, Dash }

/// <summary>What a drill earned, by its score's bands (<see cref="DrillBands"/>): bronze, silver or gold.</summary>
public enum DrillGrade { Bronze, Silver, Gold }

/// <summary>
/// What the player does in one frame of a drill (plan 36 · TR1): the button pressed this frame (<see cref="Tap"/>) or kept
/// down (<see cref="Held"/>; a frame with a <see cref="Tap"/> counts as held), and the arrows held (<see cref="Dx"/>, -1, 0 or 1).
/// </summary>
public readonly record struct DrillInput(bool Tap = false, bool Held = false, int Dx = 0)
{
    public static readonly DrillInput None = new();

    /// <summary>Whether the button is down this frame.</summary>
    public bool Down => Held || Tap;
}

/// <summary>
/// Which drill trains which stat (plan 36 · TR1): the throw trains Attack, or Sp. Atk as a shot of energy; the guard Defense,
/// or Sp. Def against pellets of energy; the dash Speed as a sprint of bursts, or HP as a long run with stamina. The
/// <see cref="Special"/> one of a pair is the same game with its own numbers and its own look. No drawing or input.
/// </summary>
public static class DrillKinds
{
    /// <summary>The six stats a drill trains, in the order the screens list them.</summary>
    public static readonly StatType[] Stats =
    {
        StatType.Attack, StatType.SpAttack, StatType.Defense, StatType.SpDefense, StatType.Speed, StatType.HP
    };

    /// <summary>The drill that trains a stat, and whether it is the pair's second (Sp. Atk, Sp. Def, HP).</summary>
    public static (DrillKind Kind, bool Special)? PlanFor(StatType stat) => stat switch
    {
        StatType.Attack => (DrillKind.Throw, false),
        StatType.SpAttack => (DrillKind.Throw, true),
        StatType.Defense => (DrillKind.Guard, false),
        StatType.SpDefense => (DrillKind.Guard, true),
        StatType.Speed => (DrillKind.Dash, false),
        StatType.HP => (DrillKind.Dash, true),
        _ => null
    };

    /// <summary>The two stats a drill trains.</summary>
    public static StatType[] StatsOf(DrillKind kind) => kind switch
    {
        DrillKind.Throw => new[] { StatType.Attack, StatType.SpAttack },
        DrillKind.Guard => new[] { StatType.Defense, StatType.SpDefense },
        _ => new[] { StatType.Speed, StatType.HP }
    };
}

/// <summary>
/// The score bands of the three drills (plan 36 · TR1; bands of ours, set so that <see cref="TrainingDrill.Autopilot"/>
/// at its usual skill earns silver and gold wants care, the way the Pokéathlon's medals are set): a score under
/// <see cref="Silver"/> is bronze, from it silver, and from <see cref="Gold"/> gold.
/// </summary>
public static class DrillBands
{
    public static int Silver(DrillKind kind, bool special = false) => kind switch { DrillKind.Throw => ThrowDrill.SilverScore, DrillKind.Guard => GuardDrill.SilverScore, _ => special ? DashDrill.LongSilverScore : DashDrill.SilverScore };

    public static int Gold(DrillKind kind, bool special = false) => kind switch { DrillKind.Throw => ThrowDrill.GoldScore, DrillKind.Guard => GuardDrill.GoldScore, _ => special ? DashDrill.LongGoldScore : DashDrill.GoldScore };

    public static DrillGrade GradeOf(DrillKind kind, int score, bool special = false) =>
        score >= Gold(kind, special) ? DrillGrade.Gold : score >= Silver(kind, special) ? DrillGrade.Silver : DrillGrade.Bronze;
}

/// <summary>
/// A drill (plan 36 · TR1): a game of a fixed rate, thirty frames a second like the Pokéathlon's events, half a minute
/// long, played by one Pokémon whose own stats change nothing in it. <see cref="Step"/> plays one frame; the drill is
/// a pure function of its seed and the inputs given, drawing its chance from a generator of its own (<see cref="SplitMix"/>),
/// never from <c>Dice</c>, so the harness and the tests repeat it. <see cref="Advance"/> plays whole frames of the time
/// it is given, never past the end. No drawing or input.
/// </summary>
public abstract class TrainingDrill
{
    public const int FramesPerSecond = 30;
    public const float Frame = 1f / FramesPerSecond;

    /// <summary>The frames a drill lasts: half a minute.</summary>
    public const int Length = 30 * FramesPerSecond;

    protected readonly SplitMix Rng;
    private readonly SplitMix autopilot;
    private float owed;
    private bool tapOwed;

    public DrillKind Kind { get; }
    public StatType Stat { get; }
    public int Seed { get; }

    /// <summary>The pair's second drill (Sp. Atk, Sp. Def, HP): the same game with numbers and a look of its own.</summary>
    public bool Special { get; }

    /// <summary>The frames played.</summary>
    public int Frames { get; private set; }

    public bool IsDone { get; private set; }

    /// <summary>The score so far.</summary>
    public int Score { get; protected set; }

    /// <summary>Seconds left on the drill's clock.</summary>
    public float SecondsLeft => Math.Max(0, Length - Frames) * Frame;

    /// <summary>What the score has earned, as it stands.</summary>
    public DrillGrade Grade => DrillBands.GradeOf(Kind, Score, Special);

    protected TrainingDrill(DrillKind kind, StatType stat, bool special, int seed)
    {
        Kind = kind;
        Stat = stat;
        Special = special;
        Seed = seed;
        Rng = new SplitMix((uint)seed * 2654435761u + 0x1B873593u);
        autopilot = new SplitMix((uint)seed ^ 0xA17F00Du);
    }

    /// <summary>The drill that trains a stat, from a seed. Throws for a stat no drill trains.</summary>
    public static TrainingDrill Create(StatType stat, int seed)
    {
        var (kind, special) = DrillKinds.PlanFor(stat) ?? throw new ArgumentOutOfRangeException(nameof(stat), stat, "No drill trains this stat.");
        return kind switch
        {
            DrillKind.Throw => new ThrowDrill(stat, special, seed),
            DrillKind.Guard => new GuardDrill(stat, special, seed),
            _ => new DashDrill(stat, special, seed)
        };
    }

    /// <summary>One frame of the drill with the player's input.</summary>
    public void Step(DrillInput input)
    {
        if (IsDone) return;
        Play(input);
        Frames++;
        if (Frames >= Length) IsDone = true;
    }

    /// <summary>
    /// Time passes: as many whole frames as it holds, never past the drill's end. A press given here goes into the first
    /// frame played, and waits for the next call if none is.
    /// </summary>
    public int Advance(float dt, DrillInput input)
    {
        if (input.Tap) tapOwed = true;
        owed += Math.Max(0f, dt);
        int played = 0;
        while (owed >= Frame && !IsDone)
        {
            owed -= Frame;
            Step(input with { Tap = tapOwed });
            tapOwed = false;
            played++;
        }
        if (IsDone) owed = 0f;
        return played;
    }

    /// <summary>
    /// What a player of the given <paramref name="knack"/> (0 to 1; a little under 0.6 is the usual: silver) would do this
    /// frame, on a generator of its own. Called once for each frame that is played.
    /// </summary>
    public DrillInput Autopilot(float knack = DefaultKnack) => Think(knack, autopilot);

    /// <summary>The skill the tests and the harness play at.</summary>
    public const float DefaultKnack = 0.55f;

    /// <summary>Plays the drill to its end with the autopilot and returns the grade.</summary>
    public DrillGrade PlayItself(float knack = DefaultKnack)
    {
        while (!IsDone) Step(Autopilot(knack));
        return Grade;
    }

    protected abstract void Play(DrillInput input);

    protected abstract DrillInput Think(float knack, SplitMix rng);

    /// <summary>A number drawn about nought: the sum of two draws, -1 to 1.</summary>
    protected static float Wobble(SplitMix rng) => rng.Float() + rng.Float() - 1f;
}

/// <summary>
/// The throw (plan 36 · TR1; Attack, or Sp. Atk as a shot of energy): a target slides along the back wall and shrinks as the
/// drill goes; the aim (<see cref="AimX"/>) moves left and right and a press lets a shot fly, which lands <see cref="Flight"/>
/// frames later where the aim was; the middle of the target scores three, its ring two, the board one. One shot at a time
/// and a breath between. The target's place is a pure function of the frame (<see cref="TargetAt"/>), so the player may
/// lead it.
/// </summary>
public sealed class ThrowDrill : TrainingDrill
{
    public const int Width = 1000, AimSpeed = 18, Flight = 9, Breath = 12;
    public const int SilverScore = 70, GoldScore = 125;

    /// <summary>One shot in flight.</summary>
    public sealed record Shot(int LandsAt, int X);

    private readonly int phaseStart;
    private readonly List<Shot> shots = new();
    private int nextFire;
    private int planned = int.MinValue;
    private float plannedError;

    public ThrowDrill(StatType stat, bool special, int seed) : base(DrillKind.Throw, stat, special, seed)
    {
        phaseStart = (int)(Rng.Next() % 1600u);
        AimX = Width / 2;
    }

    /// <summary>Where the aim stands, 0 to <see cref="Width"/>.</summary>
    public int AimX { get; private set; }

    /// <summary>The shots in the air.</summary>
    public IReadOnlyList<Shot> InFlight => shots;

    /// <summary>The last shot to land: the frame, the points it scored (0 for a miss) and where it came down.</summary>
    public (int Frame, int Points, int X)? Last { get; private set; }

    public int Hits { get; private set; }
    public int Misses { get; private set; }

    /// <summary>How far the target has gone by a frame: its speed grows from 6 to 12 units a frame, in whole numbers.</summary>
    private static long PhaseSum(int frame)
    {
        long q = frame / 150, r = frame % 150;
        return 6L * frame + 150L * q * (q - 1) / 2 + q * r;
    }

    /// <summary>Where the target's middle is at a frame: a triangle wave from 100 to 900.</summary>
    public int TargetAt(int frame)
    {
        int p = (int)((phaseStart + PhaseSum(frame)) % 1600);
        return 100 + (p < 800 ? p : 1600 - p);
    }

    /// <summary>The target's radius at a frame: 140 shrinking to 50 over the drill.</summary>
    public static int RadiusAt(int frame) => 140 - 90 * Math.Min(frame, Length) / Length;

    /// <summary>The points a shot scores landing at <paramref name="x"/> on a frame: 3, 2, 1 or 0.</summary>
    public int PointsAt(int frame, int x)
    {
        int dist = Math.Abs(x - TargetAt(frame)), r = RadiusAt(frame);
        return dist * 3 <= r ? 3 : dist * 3 <= 2 * r ? 2 : dist <= r ? 1 : 0;
    }

    /// <summary>Whether a press would let a shot fly now.</summary>
    public bool CanFire => Frames >= nextFire && !IsDone;

    protected override void Play(DrillInput input)
    {
        AimX = Math.Clamp(AimX + AimSpeed * Math.Sign(input.Dx), 0, Width);
        if (input.Tap && Frames >= nextFire)
        {
            shots.Add(new Shot(Frames + Flight, AimX));
            nextFire = Frames + Breath;
        }
        for (int i = shots.Count - 1; i >= 0; i--)
        {
            var shot = shots[i];
            if (shot.LandsAt != Frames) continue;
            int points = PointsAt(Frames, shot.X);
            Score += points;
            if (points > 0) Hits++; else Misses++;
            Last = (Frames, points, shot.X);
            shots.RemoveAt(i);
        }
    }

    protected override DrillInput Think(float knack, SplitMix rng)
    {
        // Aim where the target will be as a shot would land, off by more the less the player plays well, and fire
        // once the aim is there
        if (planned != Frames / 6)
        {
            planned = Frames / 6;
            plannedError = Wobble(rng) * (1.6f - knack) * RadiusAt(Frames) * 2f;
        }
        int want = (int)(TargetAt(Frames + Flight) + plannedError);
        int off = want - AimX;
        int dx = Math.Abs(off) <= AimSpeed / 2 ? 0 : Math.Sign(off);
        bool fire = Math.Abs(off) <= AimSpeed && Frames >= nextFire;
        return new DrillInput(Tap: fire, Held: fire, Dx: dx);
    }
}

/// <summary>
/// The guard (plan 36 · TR1; Defense, or Sp. Def against pellets of energy): pellets fall down three lanes toward the line
/// where the Pokémon guards (<see cref="Pellet.Arrives"/>, <see cref="Fall"/> frames after they appear). The guard moves
/// into a lane with the arrows, one lane for each press of one, and a press raises it for a moment. A pellet is caught by
/// a raise in its lane within <see cref="Window"/> frames of its arrival: within <see cref="Perfect"/> of it scores two (on
/// time), otherwise one (late or early); one that nothing caught stuns the guard for <see cref="StunFrames"/> frames, in
/// which it can neither move nor raise. A raise takes <see cref="Cooldown"/> frames to come again.
/// </summary>
public sealed class GuardDrill : TrainingDrill
{
    public const int Lanes = 3, Fall = 36, Window = 5, Perfect = 2, StunFrames = 20, Cooldown = 14, Up = 6;
    public const int SilverScore = 40, GoldScore = 78;

    /// <summary>One pellet: its lane and the frame it reaches the guard's line.</summary>
    public sealed class Pellet
    {
        public int Lane, Arrives;
        /// <summary>Points it was caught for (0 until caught), and whether it has been settled (caught or missed).</summary>
        public int Caught;
        public bool Settled;
    }

    private sealed class Raise
    {
        public int Frame, Lane;
        public bool Used;
    }

    private readonly List<Pellet> pellets = new();
    private readonly List<Raise> raises = new();
    private int lastDx, cool;
    private int[] planned = Array.Empty<int>();

    public GuardDrill(StatType stat, bool special, int seed) : base(DrillKind.Guard, stat, special, seed)
    {
        // The pellets are laid out from the seed alone: the first at frame 40, then a gap of 24 frames growing shorter to
        // 16, drawn a few frames either way
        int t = 40;
        while (t < Length - Window - 2)
        {
            pellets.Add(new Pellet { Lane = Rng.Int(Lanes), Arrives = t });
            int gap = (special ? 22 : 24) - (special ? 7 : 8) * t / Length;
            t += gap + Rng.Int(7) - 3;
        }
        planned = new int[pellets.Count];
        Array.Fill(planned, int.MinValue);
        Lane = 1;
    }

    public IReadOnlyList<Pellet> Pellets => pellets;

    /// <summary>The lane the guard stands in, 0 to 2.</summary>
    public int Lane { get; private set; }

    /// <summary>Frames of stun left.</summary>
    public int Stun { get; private set; }

    public int Stuns { get; private set; }

    /// <summary>The frame of the last raise (-1000 before any).</summary>
    public int LastRaise { get; private set; } = -1000;

    /// <summary>Whether the guard is up this frame.</summary>
    public bool GuardUp => Frames - LastRaise < Up;

    /// <summary>The last pellet settled: the frame it was settled on, its lane and its points (0 for a miss).</summary>
    public (int Frame, int Lane, int Points)? Last { get; private set; }

    protected override void Play(DrillInput input)
    {
        if (cool > 0) cool--;
        if (Stun > 0)
        {
            Stun--;
            lastDx = input.Dx;
        }
        else
        {
            if (input.Dx != 0 && lastDx == 0) Lane = Math.Clamp(Lane + Math.Sign(input.Dx), 0, Lanes - 1);
            lastDx = input.Dx;
            if (input.Tap && cool == 0)
            {
                raises.Add(new Raise { Frame = Frames, Lane = Lane });
                LastRaise = Frames;
                cool = Cooldown;
            }
        }

        // A pellet is settled Window frames after it arrives, so that a raise a little late can still have caught it
        foreach (var pellet in pellets)
        {
            if (pellet.Settled || pellet.Arrives + Window != Frames) continue;
            Raise? best = null;
            foreach (var raise in raises)
                if (!raise.Used && raise.Lane == pellet.Lane && Math.Abs(raise.Frame - pellet.Arrives) <= Window
                    && (best == null || Math.Abs(raise.Frame - pellet.Arrives) < Math.Abs(best.Frame - pellet.Arrives)))
                    best = raise;
            pellet.Settled = true;
            if (best != null)
            {
                best.Used = true;
                pellet.Caught = Math.Abs(best.Frame - pellet.Arrives) <= Perfect ? 2 : 1;
                Score += pellet.Caught;
            }
            else
            {
                Stun = StunFrames;
                Stuns++;
            }
            Last = (Frames, pellet.Lane, pellet.Caught);
        }
    }

    protected override DrillInput Think(float knack, SplitMix rng)
    {
        // The next pellet nobody has answered: head for its lane, and press when its time comes, off by more the less the
        // player plays well
        int index = -1;
        for (int i = 0; i < pellets.Count; i++)
            if (!pellets[i].Settled && !Answered(i)) { index = i; break; }
        if (index < 0) return DrillInput.None;
        var pellet = pellets[index];
        if (planned[index] == int.MinValue) planned[index] = pellet.Arrives + (int)MathF.Round(Wobble(rng) * (1.1f - knack) * 14f);
        int dx = 0;
        if (Lane != pellet.Lane && lastDx == 0) dx = Math.Sign(pellet.Lane - Lane);
        bool press = Frames >= planned[index] && Lane == pellet.Lane && cool == 0 && Stun == 0;
        if (press) answered.Add(index);
        return new DrillInput(Tap: press, Held: press, Dx: dx);
    }

    private readonly HashSet<int> answered = new();
    private bool Answered(int i) => answered.Contains(i);
}

/// <summary>
/// The dash (plan 36 · TR1; Speed as a sprint of bursts, HP as a long run with stamina): a beat sounds every
/// <see cref="BeatEvery"/> frames and a press within <see cref="OnBeat"/> frames of one builds the pace; a press off the
/// beat (or a second on the same one) loses pace, and so does an unanswered beat its streak. A press held for
/// <see cref="HoldToJump"/> frames jumps (<see cref="AirFrames"/> frames in the air), and a hurdle met on the ground is a
/// stumble: pace falls to a third and the runner is stunned a moment, costing stamina in the long run. Stamina (the long
/// run alone) drains with every frame, faster with the stumbles and the wrong presses, and a runner with none left can
/// only crawl. The score is the distance run and a few points for every hurdle cleared.
/// </summary>
public sealed class DashDrill : TrainingDrill
{
    public const int OnBeat = 2, HoldToJump = 5, AirFrames = 14, StunFrames = 10, HurdleCount = 8;
    public const int SilverScore = 48, GoldScore = 105, LongSilverScore = 36, LongGoldScore = 82;
    public const int StartStamina = 1500;

    private long dist;
    private int streak, lastBeatHit, held, air, stun, nextHurdle, stamina = StartStamina, stumbles, cleared;
    private bool penaltyOnRelease;
    private int pace;
    private int nextBeat = 1, tapPlan = int.MinValue, holdLeft, jumpedFor = -1;
    private int holdJitter = int.MinValue;

    public DashDrill(StatType stat, bool special, int seed) : base(DrillKind.Dash, stat, special, seed)
    {
        lastBeatHit = 0;
    }

    /// <summary>Frames between beats: the long run keeps a slower one.</summary>
    public int BeatEvery => Special ? 18 : 14;

    private int MaxPace => Special ? (stamina <= 0 ? 2800 : 6200) : 7500;

    /// <summary>Distance run, in units.</summary>
    public float Distance => dist / 1000f;

    /// <summary>The current pace, in units a frame.</summary>
    public float Pace => pace / 1000f;

    public int Streak => streak;
    public int Stumbles => stumbles;
    public int Cleared => cleared;
    public bool InAir => air > 0;
    /// <summary>Frames of the jump still to come (plan 36 · TR2: the screen draws the arc from it).</summary>
    public int AirLeft => air;
    public int StunLeft => stun;

    /// <summary>Stamina left, 0 to <see cref="StartStamina"/> (the long run's; the sprint keeps its full).</summary>
    public int Stamina => Special ? Math.Max(0, stamina) : StartStamina;

    /// <summary>Where a hurdle stands, in units: the first at 600, one every 500 after.</summary>
    public static int HurdleAt(int index) => 600 + 500 * index;

    /// <summary>The next hurdle, or -1 once all are behind.</summary>
    public int NextHurdle => nextHurdle < HurdleCount ? nextHurdle : -1;

    /// <summary>The frame of the beat nearest a frame (the first is at <see cref="BeatEvery"/>).</summary>
    private int NearestBeat(int frame) => Math.Max(1, (int)MathF.Round(frame / (float)BeatEvery)) * BeatEvery;

    private void Lose(int stamina2)
    {
        streak = 0;
        pace = Math.Max(0, pace - 900);
        if (Special) stamina -= stamina2;
    }

    protected override void Play(DrillInput input)
    {
        int f = Frames;
        bool down = input.Down;
        bool tap = input.Tap && stun == 0;
        if (stun > 0) { stun--; down = false; }

        // A beat that nobody answered breaks the streak, four frames after it
        if (f >= 4 + BeatEvery && (f - 4) % BeatEvery == 0 && lastBeatHit != (f - 4) / BeatEvery) streak = 0;

        held = down ? held + 1 : 0;
        if (tap)
        {
            int beat = NearestBeat(f), k = beat / BeatEvery;
            if (Math.Abs(f - beat) <= OnBeat && k != lastBeatHit)
            {
                lastBeatHit = k;
                streak++;
                pace = Math.Min(MaxPace, pace + 500 + 150 * Math.Min(streak, 8));
            }
            else penaltyOnRelease = true;
        }
        // A press that turns into a jump is no wrong press; one let go before that is
        if (held >= HoldToJump) penaltyOnRelease = false;
        if (!down && penaltyOnRelease) { penaltyOnRelease = false; Lose(25); }
        if (held == HoldToJump && air == 0 && stun == 0) air = AirFrames;

        pace -= pace / 40;
        if (pace > MaxPace) pace = MaxPace;
        if (Special) stamina--;

        long before = dist;
        dist += pace;
        while (nextHurdle < HurdleCount && dist >= HurdleAt(nextHurdle) * 1000L)
        {
            if (air > 0) cleared++;
            else
            {
                stumbles++;
                pace /= 3;
                stun = StunFrames;
                if (Special) stamina -= 150;
            }
            nextHurdle++;
        }
        if (air > 0) air--;
        Score = (int)(dist / 40000) + 4 * cleared;
    }

    protected override DrillInput Think(float knack, SplitMix rng)
    {
        int f = Frames;
        // Holding for a jump: the press goes down at once and stays down
        if (holdLeft > 0)
        {
            holdLeft--;
            return new DrillInput(Held: true);
        }
        // A jump starts a few frames before the hurdle, off by more the less the player plays well
        if (NextHurdle is { } h and >= 0 && jumpedFor != h && air == 0 && stun == 0)
        {
            if (holdJitter == int.MinValue) holdJitter = (int)MathF.Round(Wobble(rng) * (1.1f - knack) * 9f);
            float ttc = (HurdleAt(h) * 1000f - dist) / Math.Max(pace, 2000);
            if (ttc <= 11 + holdJitter)
            {
                jumpedFor = h;
                holdJitter = int.MinValue;
                holdLeft = HoldToJump + 1;
                return new DrillInput(Tap: true, Held: true);
            }
        }
        // Otherwise a press on each beat, off by more the less the player plays well
        if (tapPlan == int.MinValue) tapPlan = nextBeat * BeatEvery + (int)MathF.Round(Wobble(rng) * (1.2f - knack) * 6.5f);
        if (f == tapPlan)
        {
            nextBeat++;
            tapPlan = int.MinValue;
            return new DrillInput(Tap: true, Held: true);
        }
        if (f > tapPlan) { nextBeat++; tapPlan = int.MinValue; }
        return DrillInput.None;
    }
}

/// <summary>Why a drill can't train a stat now, in words (<see cref="EffortDrills.WhyNot"/>); and what a drill gives.</summary>
public static class EffortDrills
{
    /// <summary>Effort for bronze, silver and gold: 8, 16 and 24 points (plan 36's decision 3).</summary>
    public static int PointsFor(DrillGrade grade) => grade switch { DrillGrade.Gold => 24, DrillGrade.Silver => 16, _ => 8 };

    /// <summary>
    /// The reason a drill can't train this stat of this Pokémon, or null: an Egg, a stat no drill trains, Shedinja's HP,
    /// a stat already at the most the rules allow (252 under the modern rules), a Pokémon already at 510 in all.
    /// </summary>
    public static string? WhyNot(Pokemon pokemon, StatType stat, Ruleset? rules = null)
    {
        if (pokemon.IsEgg) return Text.Of("training.why.egg");
        if (DrillKinds.PlanFor(stat) == null) return Text.Of("training.why.stat");
        if (stat == StatType.HP && pokemon.Species.Name == "Shedinja") return Text.Of("training.why.shedinja");
        if (EffortRules.Get(pokemon, stat) >= (rules ?? Ruleset.Current).EvStatCap) return Text.Of("training.why.limit", ("stat", Words.Stat(stat)));
        if (EffortRules.Total(pokemon) >= EffortRules.MaxTotal) return Text.Of("training.why.full");
        return null;
    }

    /// <summary>
    /// Gives a stat the effort a grade earned: plain points, with no Pokérus, Power item or Macho Brace, cut by the rules'
    /// limit for a stat and by 510 in all, and the stats reckoned again at once (battle effort waits for a level, as
    /// Platinum's does; a drill's is shown on the screen that gave it). Returns the points added, 0 where the drill
    /// refuses the Pokémon (<see cref="WhyNot"/>).
    /// </summary>
    public static int Award(Pokemon pokemon, StatType stat, DrillGrade grade, Ruleset? rules = null)
    {
        if (WhyNot(pokemon, stat, rules) != null) return 0;
        int current = EffortRules.Get(pokemon, stat);
        int gain = Math.Min(PointsFor(grade), Math.Min((rules ?? Ruleset.Current).EvStatCap - current, EffortRules.MaxTotal - EffortRules.Total(pokemon)));
        if (gain <= 0) return 0;
        EffortRules.Set(pokemon, stat, current + gain);
        pokemon.ReckonStats();
        return gain;
    }
}
