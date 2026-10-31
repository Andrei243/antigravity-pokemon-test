using System;
using System.Collections.Generic;
using System.Numerics;

namespace PokemonPlatinumEngine.Models;

/// <summary>
/// The second batch of the Pokéathlon's events (plan 15 · E7; our own numbers on HeartGold's shapes, docs/mechanics/
/// rulings.md, "The Pokéathlon"): Pennant Capture, Snow Throw, Disc Catch and Lamp Jump, each on <see cref="AthlonEvent"/>'s
/// shape (thirty frames a second, the rivals' minds on the event's own generator), so each is a pure function of its
/// seed and the player's presses.
/// </summary>
public static class AthlonEventsBatch2
{
    /// <summary>The second batch's events by kind, or null for a kind of another batch.</summary>
    public static AthlonEvent? Create(AthlonEventKind kind, IReadOnlyList<AthlonTeam> teams, int seed) => kind switch
    {
        AthlonEventKind.PennantCapture => new PennantCapture(teams, seed),
        AthlonEventKind.SnowThrow => new SnowThrow(teams, seed),
        AthlonEventKind.DiscCatch => new DiscCatch(teams, seed),
        AthlonEventKind.LampJump => new LampJump(teams, seed),
        _ => null
    };

    /// <summary>The way the arrows point toward a place from another (each -1, 0 or 1), with a dead zone across.</summary>
    public static (int Dx, int Dy) ArrowsToward(Vector2 from, Vector2 to, float deadZone = 3f)
    {
        var d = to - from;
        int dx = MathF.Abs(d.X) > deadZone && MathF.Abs(d.X) > MathF.Abs(d.Y) * 0.4f ? Math.Sign(d.X) : 0;
        int dy = MathF.Abs(d.Y) > deadZone && MathF.Abs(d.Y) > MathF.Abs(d.X) * 0.4f ? Math.Sign(d.Y) : 0;
        return (dx, dy);
    }
}

/// <summary>
/// Pennant Capture (plan 15 · E7; a chase across a field): a minute on a field <see cref="Width"/> by
/// <see cref="Height"/>, one of each team's three on it at a time, run about with the arrows (faster by Speed). Pennants
/// are set out at places drawn on the field's own generator, never more than <see cref="MostPennants"/> at once; whoever
/// reaches one takes it. Running tires (less by Stamina) and a tired runner goes at half speed; a switch sends the next
/// of the three in where the last one stood. Two that run into each other bounce apart and stand dazed for frames fewer
/// by Skill. Points are three a pennant.
/// </summary>
public sealed class PennantCapture : AthlonEvent
{
    public const float Width = 240f, Height = 140f, Body = 9f, PennantReach = 7f, Edge = 14f;
    public const int MostPennants = 4, SetEvery = 30, Tired = 600, SwitchFrames = 12;
    public const int Minute = 60 * FramesPerSecond;

    public sealed class Chaser
    {
        public int Active, Flags, Dazed, Switching, Bumps;
        public Vector2 At, Facing = new(0f, -1f);
        public readonly int[] Fatigue = new int[AthlonRun.TeamSize];
        /// <summary>A rival's way, kept for a few frames between thoughts.</summary>
        public Vector2 Aim;
        public int AimFor;
    }

    private readonly Chaser[] chasers = new Chaser[AthlonRun.TeamCount];
    private readonly List<Vector2> pennants = new();
    private readonly Random field;

    public PennantCapture(IReadOnlyList<AthlonTeam> teams, int seed) : base(AthlonEventKind.PennantCapture, teams, seed)
    {
        field = new Random(seed ^ 0x3E7);
        for (int t = 0; t < chasers.Length; t++) chasers[t] = new Chaser { At = StartOf(t) };
        for (int i = 0; i < 3; i++) SetPennant();
    }

    public override int Length => Minute;
    public override AthleteStat Leans => AthleteStat.Speed;
    public IReadOnlyList<Chaser> Chasers => chasers;
    public IReadOnlyList<Vector2> Pennants => pennants;

    /// <summary>Where a team starts: the player at the bottom left, the rivals at the other corners.</summary>
    public static Vector2 StartOf(int team) => team switch
    {
        0 => new Vector2(Edge, Height - Edge),
        1 => new Vector2(Edge, Edge),
        2 => new Vector2(Width - Edge, Edge),
        _ => new Vector2(Width - Edge, Height - Edge)
    };

    public static float Run(AthleteStats s) => 1.2f + 0.25f * s.Speed;
    public static int Strain(AthleteStats s) => 7 - s.Stamina;
    public static int DazeFrames(AthleteStats s) => 30 - 4 * s.Skill;

    public Athlete ActiveOf(int team) => Teams[team].Athletes[chasers[team].Active];
    public bool IsTired(int team) => chasers[team].Fatigue[chasers[team].Active] >= Tired;

    private void SetPennant()
    {
        if (pennants.Count >= MostPennants) return;
        pennants.Add(new Vector2(Edge + (float)field.NextDouble() * (Width - 2 * Edge), Edge + (float)field.NextDouble() * (Height - 2 * Edge)));
    }

    protected override void Play(AthlonInput[] inputs)
    {
        if (Frames % SetEvery == SetEvery - 1) SetPennant();
        for (int t = 0; t < chasers.Length; t++)
        {
            var c = chasers[t];
            for (int a = 0; a < AthlonRun.TeamSize; a++)
                if (a != c.Active && c.Fatigue[a] > 0) c.Fatigue[a] = Math.Max(0, c.Fatigue[a] - 4);
            if (c.Switching > 0) { c.Switching--; continue; }
            if (c.Dazed > 0) { c.Dazed--; continue; }
            if (inputs[t].Switch != 0)
            {
                c.Active = ((c.Active + inputs[t].Switch) % AthlonRun.TeamSize + AthlonRun.TeamSize) % AthlonRun.TeamSize;
                c.Switching = SwitchFrames;
                continue;
            }
            var way = new Vector2(Math.Sign(inputs[t].Dx), Math.Sign(inputs[t].Dy));
            if (way == Vector2.Zero) continue;
            way = Vector2.Normalize(way);
            var stats = ActiveOf(t).Stats;
            float speed = Run(stats) * (IsTired(t) ? 0.5f : 1f);
            c.Facing = way;
            c.At = Vector2.Clamp(c.At + way * speed, new Vector2(Body, Body), new Vector2(Width - Body, Height - Body));
            c.Fatigue[c.Active] = Math.Min(Tired, c.Fatigue[c.Active] + Strain(stats));
        }

        // Two that meet bounce apart and are dazed (unless one is already)
        for (int i = 0; i < chasers.Length; i++)
            for (int j = i + 1; j < chasers.Length; j++)
            {
                var a = chasers[i];
                var b = chasers[j];
                var d = b.At - a.At;
                float dist = d.Length();
                if (dist >= 2f * Body) continue;
                var n = dist > 0.001f ? d / dist : new Vector2(1f, 0f);
                float push = 2f * Body - dist + 6f;
                a.At = Vector2.Clamp(a.At - n * push / 2f, new Vector2(Body, Body), new Vector2(Width - Body, Height - Body));
                b.At = Vector2.Clamp(b.At + n * push / 2f, new Vector2(Body, Body), new Vector2(Width - Body, Height - Body));
                if (a.Dazed == 0 && b.Dazed == 0 && a.Switching == 0 && b.Switching == 0)
                {
                    a.Dazed = DazeFrames(ActiveOf(i).Stats);
                    b.Dazed = DazeFrames(ActiveOf(j).Stats);
                    a.Bumps++;
                    b.Bumps++;
                }
            }

        // A pennant reached is taken, the team first in the order on a tie
        for (int t = 0; t < chasers.Length; t++)
        {
            var c = chasers[t];
            if (c.Switching > 0) continue;
            for (int p = pennants.Count - 1; p >= 0; p--)
                if (Vector2.Distance(c.At, pennants[p]) <= Body + PennantReach)
                {
                    pennants.RemoveAt(p);
                    c.Flags++;
                }
        }
    }

    protected override AthlonInput Think(int team, float knack, Random rng)
    {
        var c = chasers[team];
        if (c.Switching > 0 || c.Dazed > 0) return AthlonInput.None;
        if (IsTired(team) && rng.NextDouble() < 0.1 + 0.4 * knack) return new AthlonInput(Switch: 1);
        if (--c.AimFor <= 0)
        {
            // The nearest pennant, the others weighed by how near another team is to them; off by more the less it plays well
            Vector2 goal = new(Width / 2f, Height / 2f);
            float best = float.MaxValue;
            foreach (var p in pennants)
            {
                float cost = Vector2.Distance(c.At, p) + Wobble(rng) * (1f - knack) * 80f;
                if (cost < best) { best = cost; goal = p; }
            }
            c.Aim = goal;
            c.AimFor = 4 + rng.Next(6);
        }
        var (dx, dy) = AthlonEventsBatch2.ArrowsToward(c.At, c.Aim);
        return new AthlonInput(Dx: dx, Dy: dy);
    }

    public override int Points(int team) => 3 * chasers[team].Flags;
}

/// <summary>
/// Snow Throw (plan 15 · E7; snowballs across a square): forty seconds, the four teams at the square's four sides, one of
/// each team's three throwing at a time. The arrows pick whom to throw at (the team that way), the button throws once the
/// last throw is made up again (sooner by Skill), and the ball flies to its mark in frames fewer by Power. Keeping the
/// button down ducks, which lets a ball fly over and throws nothing; one hit stands dazed for frames fewer by Stamina.
/// A switch sends the next of the three up. Points are three a hit and one a ball ducked.
/// </summary>
public sealed class SnowThrow : AthlonEvent
{
    public const float Side = 70f;
    public const int SwitchFrames = 12;
    public const int Seconds40 = 40 * FramesPerSecond;

    public sealed class Thrower
    {
        public int Active, Target = -1, Reload, Dazed, Switching, Hits, Taken, Ducked, Thrown;
        public bool Ducking;
        /// <summary>A rival's next throw, held until it is made.</summary>
        public int WillThrowAt = -1;
        /// <summary>The last ball a rival made up its mind about (by number), and whether it means to duck it.</summary>
        public int Considered = -1;
        public bool WillDuck;
    }

    /// <summary>A snowball in the air: who threw it at whom, how long it has flown and how long it flies.</summary>
    public sealed class Ball
    {
        public int Number, From, To, Age, Flight;
        public float Progress => Flight > 0 ? (float)Age / Flight : 1f;
    }

    private readonly Thrower[] throwers = new Thrower[AthlonRun.TeamCount];
    private readonly List<Ball> balls = new();
    private int thrown;

    public SnowThrow(IReadOnlyList<AthlonTeam> teams, int seed) : base(AthlonEventKind.SnowThrow, teams, seed)
    {
        for (int t = 0; t < throwers.Length; t++) throwers[t] = new Thrower { Target = (t + 2) % AthlonRun.TeamCount };
    }

    public override int Length => Seconds40;
    public override AthleteStat Leans => AthleteStat.Skill;
    public IReadOnlyList<Thrower> Throwers => throwers;
    public IReadOnlyList<Ball> Balls => balls;

    /// <summary>Where a team stands: the player at the bottom, the rivals left, top and right.</summary>
    public static Vector2 PlaceOf(int team) => team switch
    {
        0 => new Vector2(0f, Side),
        1 => new Vector2(-Side, 0f),
        2 => new Vector2(0f, -Side),
        _ => new Vector2(Side, 0f)
    };

    public static int ReloadFrames(AthleteStats s) => 40 - 4 * s.Skill;
    public static int FlightFrames(AthleteStats s) => 30 - 3 * s.Power;
    public static int DazeFrames(AthleteStats s) => 45 - 5 * s.Stamina;

    public Athlete ActiveOf(int team) => Teams[team].Athletes[throwers[team].Active];

    /// <summary>The team the arrows point at from a team's side: the one whose way is nearest the arrows'.</summary>
    public static int TargetFor(int team, int dx, int dy)
    {
        if (dx == 0 && dy == 0) return -1;
        var way = Vector2.Normalize(new Vector2(dx, dy));
        int best = -1;
        float most = float.MinValue;
        for (int t = 0; t < AthlonRun.TeamCount; t++)
        {
            if (t == team) continue;
            float dot = Vector2.Dot(Vector2.Normalize(PlaceOf(t) - PlaceOf(team)), way);
            if (dot > most) { most = dot; best = t; }
        }
        return best;
    }

    protected override void Play(AthlonInput[] inputs)
    {
        for (int t = 0; t < throwers.Length; t++)
        {
            var th = throwers[t];
            if (th.Reload > 0) th.Reload--;
            th.Ducking = false;
            if (th.Switching > 0) { th.Switching--; continue; }
            if (th.Dazed > 0) { th.Dazed--; continue; }
            var input = inputs[t];
            if (input.Switch != 0)
            {
                th.Active = ((th.Active + input.Switch) % AthlonRun.TeamSize + AthlonRun.TeamSize) % AthlonRun.TeamSize;
                th.Switching = SwitchFrames;
                continue;
            }
            int aimed = TargetFor(t, input.Dx, input.Dy);
            if (aimed >= 0) th.Target = aimed;
            if (input.Held && !input.Tap)
            {
                th.Ducking = true;
                continue;
            }
            if (input.Tap && th.Reload == 0 && th.Target >= 0)
            {
                var stats = ActiveOf(t).Stats;
                balls.Add(new Ball { Number = thrown++, From = t, To = th.Target, Flight = FlightFrames(stats) });
                th.Reload = ReloadFrames(stats);
                th.Thrown++;
            }
        }

        // The balls fly; one at its mark hits, unless its mark ducks
        for (int i = 0; i < balls.Count; i++)
        {
            var b = balls[i];
            if (++b.Age < b.Flight) continue;
            var mark = throwers[b.To];
            if (mark.Ducking) mark.Ducked++;
            else
            {
                mark.Taken++;
                throwers[b.From].Hits++;
                if (mark.Switching == 0) mark.Dazed = Math.Max(mark.Dazed, DazeFrames(ActiveOf(b.To).Stats));
            }
            balls.RemoveAt(i--);
        }
    }

    /// <summary>The frames until the first ball thrown at a team lands, or a large number.</summary>
    public int IncomingIn(int team) => NextBallAt(team)?.Left ?? 999;

    private (int Number, int Left)? NextBallAt(int team)
    {
        (int Number, int Left)? soonest = null;
        foreach (var b in balls)
            if (b.To == team && (soonest == null || b.Flight - b.Age < soonest.Value.Left)) soonest = (b.Number, b.Flight - b.Age);
        return soonest;
    }

    protected override AthlonInput Think(int team, float knack, Random rng)
    {
        var th = throwers[team];
        if (th.Switching > 0 || th.Dazed > 0) return AthlonInput.None;
        // A ball about to land: whether to duck it is made up once (more often the better it plays), and held to
        if (NextBallAt(team) is { } next && next.Left <= 5)
        {
            if (th.Considered != next.Number)
            {
                th.Considered = next.Number;
                th.WillDuck = rng.NextDouble() < 0.1 + 0.4 * knack;
            }
            if (th.WillDuck) return new AthlonInput(Held: true);
        }
        if (th.Reload > 0) return AthlonInput.None;
        if (th.WillThrowAt < 0)
        {
            int pick = rng.Next(AthlonRun.TeamCount - 1);
            th.WillThrowAt = pick >= team ? pick + 1 : pick;
        }
        var way = PlaceOf(th.WillThrowAt) - PlaceOf(team);
        int dx = Math.Sign(MathF.Round(way.X / Side)), dy = Math.Sign(MathF.Round(way.Y / Side));
        if (MathF.Abs(way.X) > MathF.Abs(way.Y) * 1.5f) dy = 0;
        else if (MathF.Abs(way.Y) > MathF.Abs(way.X) * 1.5f) dx = 0;
        if (th.Target != th.WillThrowAt) return new AthlonInput(Dx: dx, Dy: dy);
        if (rng.NextDouble() >= 0.2 + 0.5 * knack) return AthlonInput.None;
        th.WillThrowAt = -1;
        return new AthlonInput(Tap: true);
    }

    public override int Points(int team) => 3 * throwers[team].Hits + throwers[team].Ducked;
}

/// <summary>
/// Disc Catch (plan 15 · E7; discs out of the sky): a minute, each team in a lane <see cref="LaneWidth"/> wide under the
/// same discs, thrown on a timetable drawn from the seed alone (so every lane gets the same). A disc falls from
/// <see cref="Top"/> as it drifts to where it lands; the arrows run along the lane (faster by Speed), the button jumps
/// (higher by Jump), and a disc within reach (above the catcher by its jump, across by Skill) is caught. Every jump tires
/// the jumper (less by Stamina) and a tired one jumps half as high; a switch sends the next of the three in. Points are
/// two a catch and three a catch in the air.
/// </summary>
public sealed class DiscCatch : AthlonEvent
{
    public const float LaneWidth = 160f, Top = 120f, Standing = 14f, LaneEdge = 10f;
    public const int Tired = 100, SwitchFrames = 12, FirstDisc = 45, DiscEvery = 32;
    public const int Minute = 60 * FramesPerSecond;

    /// <summary>A disc of the timetable: when it is thrown, where it comes in and lands, and how long it falls.</summary>
    public readonly record struct Disc(int ThrownAt, float FromX, float LandX, int Fall)
    {
        public float XAt(int frame) => FromX + (LandX - FromX) * Math.Clamp((float)(frame - ThrownAt) / Fall, 0f, 1f);
        public float HeightAt(int frame) => Top * (1f - Math.Clamp((float)(frame - ThrownAt) / Fall, 0f, 1f));
        public bool InTheAir(int frame) => frame >= ThrownAt && frame < ThrownAt + Fall;
    }

    public sealed class Catcher
    {
        public int Active, Air, AirLength, Switching, Caught, CaughtInAir, Missed, Next;
        public float X = LaneWidth / 2f, Lift;
        public readonly int[] Fatigue = new int[AthlonRun.TeamSize];
        /// <summary>A rival's guess of where the disc it is after lands, drawn once for each disc.</summary>
        public float Guess = float.NaN;
        public int GuessFor = -1;
        /// <summary>Frames since the last catch (for the picture's flash), or a large number.</summary>
        public int SinceCatch = 99;
    }

    private readonly Catcher[] catchers = new Catcher[AthlonRun.TeamCount];
    private readonly Disc[] discs;
    // Each disc's end in each lane: 0 still to come or in the air, 1 caught, 2 missed
    private readonly int[,] ends;

    public DiscCatch(IReadOnlyList<AthlonTeam> teams, int seed) : base(AthlonEventKind.DiscCatch, teams, seed)
    {
        var timetable = new Random(seed ^ 0xD15C);
        var list = new List<Disc>();
        for (int at = FirstDisc; at < Minute - 30; at += DiscEvery + timetable.Next(12))
        {
            float from = LaneEdge + (float)timetable.NextDouble() * (LaneWidth - 2 * LaneEdge);
            float land = LaneEdge + (float)timetable.NextDouble() * (LaneWidth - 2 * LaneEdge);
            list.Add(new Disc(at, from, land, 28 + timetable.Next(18)));
        }
        discs = list.ToArray();
        ends = new int[AthlonRun.TeamCount, discs.Length];
        for (int t = 0; t < catchers.Length; t++) catchers[t] = new Catcher();
    }

    public override int Length => Minute;
    public override AthleteStat Leans => AthleteStat.Jump;
    public IReadOnlyList<Catcher> Catchers => catchers;
    public IReadOnlyList<Disc> Discs => discs;

    /// <summary>Whether a disc is still to be caught in a team's lane (neither caught nor missed).</summary>
    public bool Open(int team, int disc) => ends[team, disc] == 0;

    public static float Run(AthleteStats s) => 0.8f + 0.25f * s.Speed;
    public static float JumpHeight(AthleteStats s) => 20f + 8f * s.Jump;
    public static int AirFrames(AthleteStats s) => 12 + 2 * s.Jump;
    public static float Reach(AthleteStats s) => 5f + 1.5f * s.Skill;
    public static int Strain(AthleteStats s) => 30 - 5 * s.Stamina;

    public Athlete ActiveOf(int team) => Teams[team].Athletes[catchers[team].Active];

    protected override void Play(AthlonInput[] inputs)
    {
        for (int t = 0; t < catchers.Length; t++)
        {
            var c = catchers[t];
            c.SinceCatch++;
            for (int a = 0; a < AthlonRun.TeamSize; a++)
                if (a != c.Active && c.Fatigue[a] > 0) c.Fatigue[a]--;
            var stats = ActiveOf(t).Stats;
            if (c.Air > 0)
            {
                c.Air--;
                float p = 1f - (float)c.Air / c.AirLength;
                c.Lift = 4f * p * (1f - p) * JumpHeight(stats) * (c.Fatigue[c.Active] >= Tired ? 0.5f : 1f);
                if (c.Air == 0) c.Lift = 0f;
            }
            if (c.Switching > 0) c.Switching--;
            else if (inputs[t].Switch != 0 && c.Air == 0)
            {
                c.Active = ((c.Active + inputs[t].Switch) % AthlonRun.TeamSize + AthlonRun.TeamSize) % AthlonRun.TeamSize;
                c.Switching = SwitchFrames;
            }
            else
            {
                c.X = Math.Clamp(c.X + Math.Sign(inputs[t].Dx) * Run(stats), LaneEdge, LaneWidth - LaneEdge);
                if (inputs[t].Tap && c.Air == 0)
                {
                    c.AirLength = AirFrames(stats);
                    c.Air = c.AirLength;
                    c.Fatigue[c.Active] = Math.Min(Tired, c.Fatigue[c.Active] + Strain(stats));
                }
            }

            // Each disc of the lane: caught when in reach, missed once it lands
            stats = ActiveOf(t).Stats;
            for (int d = c.Next; d < discs.Length && discs[d].ThrownAt <= Frames; d++)
            {
                if (ends[t, d] != 0) continue;
                var disc = discs[d];
                if (!disc.InTheAir(Frames + 1))
                {
                    ends[t, d] = 2;
                    c.Missed++;
                    continue;
                }
                float h = disc.HeightAt(Frames + 1);
                if (c.Switching == 0 && h <= Standing + c.Lift && h >= c.Lift - 4f && MathF.Abs(disc.XAt(Frames + 1) - c.X) <= Reach(stats))
                {
                    ends[t, d] = 1;
                    c.Caught++;
                    if (c.Lift > 0f) c.CaughtInAir++;
                    c.SinceCatch = 0;
                }
            }
            while (c.Next < discs.Length && ends[t, c.Next] != 0) c.Next++;
        }
    }

    protected override AthlonInput Think(int team, float knack, Random rng)
    {
        var c = catchers[team];
        if (c.Switching > 0) return AthlonInput.None;
        if (c.Fatigue[c.Active] >= Tired - 10 && c.Air == 0 && rng.NextDouble() < 0.05 + 0.3 * knack) return new AthlonInput(Switch: 1);
        // The first disc of the lane still to come: run to where it lands, guessed once, off by more the less it plays well
        int d = c.Next;
        while (d < discs.Length && !Open(team, d)) d++;
        if (d >= discs.Length) return AthlonInput.None;
        var disc = discs[d];
        if (c.GuessFor != d)
        {
            c.GuessFor = d;
            c.Guess = disc.LandX + Wobble(rng) * (1f - knack) * 80f;
        }
        int frame = Math.Max(Frames, disc.ThrownAt);
        // Where it will be as it comes down to a jump's height
        var stats = ActiveOf(team).Stats;
        float h = disc.HeightAt(frame);
        float x = Frames >= disc.ThrownAt ? disc.XAt(frame) : disc.FromX;
        float aimX = Frames >= disc.ThrownAt && h < Standing + 16f ? x : c.Guess;
        int dx = MathF.Abs(aimX - c.X) > 2f ? Math.Sign(aimX - c.X) : 0;
        bool jump = c.Air == 0 && Frames >= disc.ThrownAt && h <= Standing + JumpHeight(stats) * 0.7f && h > Standing + 6f
            && MathF.Abs(x - c.X) <= Reach(stats) && rng.NextDouble() < 0.2 + 0.6 * knack;
        return new AthlonInput(Tap: jump, Dx: dx);
    }

    public override int Points(int team) => 2 * catchers[team].Caught + catchers[team].CaughtInAir;
}

/// <summary>
/// Lamp Jump (plan 15 · E7; bounces on a spring): a minute, each team on a springboard under a column of
/// <see cref="Lamps"/> lamps <see cref="LampGap"/> apart, the three taking a third of the minute each. A Pokémon bounces
/// on its own; the button pressed in the last frames before it lands (as many as its Skill and two more) springs it
/// higher than it came down (by its Power), up to a height its Jump allows, and a bounce with no press, or one pressed
/// too soon, falls back to the first bounce's height. Each bounce lights the lamps its top passes. Points are a point
/// for every two lamps lit.
/// </summary>
public sealed class LampJump : AthlonEvent
{
    public const float Gravity = 0.5f, FirstBounce = 6f, LampGap = 32f;
    public const int Lamps = 10;
    public const int Minute = 60 * FramesPerSecond, Turn = Minute / AthlonRun.TeamSize;

    public sealed class Jumper
    {
        public float H, V = FirstBounce;
        public int LastTap = -99, Bounces, Good, Lit, Streak, BestLamps;
        /// <summary>The lamps the bounce under way has lit (for the picture), and how high its top was.</summary>
        public int LitNow;
        /// <summary>A rival's press for this bounce: frames before landing, drawn as it starts down.</summary>
        public int PressAt = -1;
    }

    private readonly Jumper[] jumpers = new Jumper[AthlonRun.TeamCount];

    public LampJump(IReadOnlyList<AthlonTeam> teams, int seed) : base(AthlonEventKind.LampJump, teams, seed)
    {
        for (int t = 0; t < jumpers.Length; t++) jumpers[t] = new Jumper();
    }

    public override int Length => Minute;
    public override AthleteStat Leans => AthleteStat.Jump;
    public IReadOnlyList<Jumper> Jumpers => jumpers;

    public static int Grace(AthleteStats s) => 2 + s.Skill;
    public static float Spring(AthleteStats s) => 0.8f + 0.3f * s.Power;
    public static float MostBounce(AthleteStats s) => 9f + 2f * s.Jump;

    /// <summary>The one on the springboard: each of the three a third of the minute.</summary>
    public int ActiveIndex => Math.Min(AthlonRun.TeamSize - 1, Frames / Turn);
    public Athlete ActiveOf(int team) => Teams[team].Athletes[ActiveIndex];

    /// <summary>How many lamps a top at this height lights.</summary>
    public static int LampsAt(float height) => Math.Clamp((int)(height / LampGap), 0, Lamps);

    protected override void Play(AthlonInput[] inputs)
    {
        for (int t = 0; t < jumpers.Length; t++)
        {
            var j = jumpers[t];
            var stats = ActiveOf(t).Stats;
            if (inputs[t].Tap) j.LastTap = Frames;
            float was = j.V;
            j.V -= Gravity;
            j.H += j.V;
            if (was > 0f && j.V <= 0f)
            {
                // The top: the lamps it passes light
                j.LitNow = LampsAt(j.H);
                j.Lit += j.LitNow;
                j.BestLamps = Math.Max(j.BestLamps, j.LitNow);
                j.PressAt = -1;
            }
            if (j.H <= 0f)
            {
                float came = -j.V;
                j.H = 0f;
                j.Bounces++;
                if (Frames - j.LastTap < Grace(stats))
                {
                    j.V = Math.Min(MostBounce(stats), came + Spring(stats));
                    j.Good++;
                    j.Streak++;
                }
                else
                {
                    j.V = FirstBounce;
                    j.Streak = 0;
                }
                j.LastTap = -99;
            }
        }
    }

    /// <summary>The frames until a jumper lands, as it falls now.</summary>
    public static int FramesToLand(float h, float v)
    {
        int n = 0;
        while (h > 0f && n < 200)
        {
            v -= Gravity;
            h += v;
            n++;
        }
        return n;
    }

    protected override AthlonInput Think(int team, float knack, Random rng)
    {
        var j = jumpers[team];
        if (j.V > 0f) return AthlonInput.None;
        var stats = ActiveOf(team).Stats;
        if (j.PressAt < 0)
        {
            // Pressed in the window, at a frame of it drawn now; too soon now and then, the more the less it plays well
            int grace = Grace(stats);
            j.PressAt = rng.NextDouble() < (1f - knack) * 0.45f ? grace + 1 + rng.Next(4) : rng.Next(grace);
        }
        // The press counts at the frame it is made, and the landing comes a frame of the physics later
        int toLand = FramesToLand(j.H, j.V) - 1;
        return toLand == j.PressAt ? new AthlonInput(Tap: true) : AthlonInput.None;
    }

    public override int Points(int team) => jumpers[team].Lit / 2;
}
