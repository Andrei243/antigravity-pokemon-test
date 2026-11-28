using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models;

/// <summary>A task of the loop catch's station as <c>Data/loop-catch.json</c> writes it (plan 44 · SD10).</summary>
public sealed record LoopTaskData
{
    /// <summary>The task's id, which a script's <c>loop go</c> names.</summary>
    [DataRequired] public string Id { get; init; } = "";
    [DataRequired] public string Species { get; init; } = "";
    /// <summary>The seconds between its strikes, a quarter either way.</summary>
    [DataRequired] public float StrikeEvery { get; init; }
    /// <summary>How far a strike reaches from the middle of its body.</summary>
    [DataRequired] public float Reach { get; init; }
}

/// <summary>The file of the loop catch's tasks.</summary>
public sealed record LoopCatchFile
{
    public List<LoopTaskData> Tasks { get; init; } = new();
}

/// <summary>The station's tasks, read strictly from <c>Data/loop-catch.json</c> (plan 44 · SD10). No drawing or input.</summary>
public static class LoopTasks
{
    public const string FileName = "loop-catch.json";

    private static readonly Lazy<LoopCatchFile> file = new(() => GameDataFiles.Load<LoopCatchFile>(FileName, strict: true));

    public static IReadOnlyList<LoopTaskData> All => file.Value.Tasks;

    public static LoopTaskData? Get(string? id) => All.FirstOrDefault(t => t.Id == id);
}

/// <summary>How a Pokémon of the loop catch moves, by its Pokédex shape.</summary>
public enum LoopGait { Run, Walk, Soar, Weave, Dart, Drift }

/// <summary>
/// What the disc is told in one tick: the stick's tilt (each of −1 to 1), the pointer's place where the mouse has the
/// disc (null for the stick), and whether the disc is held down to draw.
/// </summary>
public readonly record struct LoopInput(Vector2 Stick, Vector2? Pointer, bool Down);

/// <summary>How a try stands.</summary>
public enum LoopOutcome { Going, Calmed, SlippedAway }

/// <summary>What happened in a tick, for whoever shows or hears it.</summary>
[Flags]
public enum LoopEvents
{
    None = 0,
    /// <summary>A loop round the Pokémon was counted.</summary>
    Looped = 1,
    /// <summary>A strike began to wind up.</summary>
    WindUp = 2,
    /// <summary>A strike landed.</summary>
    Struck = 4,
    /// <summary>The strike broke the line.</summary>
    Broke = 8,
    /// <summary>The Pokémon was calmed.</summary>
    Calmed = 16,
    /// <summary>The disc's last charge is spent.</summary>
    Slipped = 32
}

/// <summary>
/// The loop catch (plan 44 · SD10; <c>docs/mechanics/rulings.md</c>, "The loop catch"): a Pokémon moves over a flat
/// field by its Pokédex shape and its Speed, and each loop the player's disc draws round it calms it a step, until a
/// strike of its breaks the line. Sixty ticks a second; the Pokémon's chance is its own generator on the seed, which
/// nothing the player does draws from. All lengths are the layout's units. No drawing or input.
/// </summary>
public sealed class LoopCatch
{
    public const int TicksPerSecond = 60;
    public const float Tick = 1f / TicksPerSecond;

    /// <summary>Where the disc may go, and where the Pokémon keeps (a loop fits round it anywhere there).</summary>
    public static readonly Vector2 DiscLeast = new(120, 130), DiscMost = new(1800, 1010);
    public static readonly Vector2 RoamLeast = new(380, 390), RoamMost = new(1540, 750);

    public const float BodyRadius = 70f;
    public const float DiscSpeed = 900f;
    public const float LineLength = 1900f;
    /// <summary>The line takes a new point once the disc has moved this far.</summary>
    public const float PointSpacing = 8f;
    public const float LeastLoopArea = 15000f;
    /// <summary>A loop goes round the whole body: it passes no nearer its middle than this.</summary>
    public const float LoopClearance = BodyRadius;
    public const int CalmNeeded = 3;
    public const int Charges = 4;
    public const float WindUpSeconds = 0.9f;
    /// <summary>A loop startles it: it winds up this soon after one that doesn't calm it.</summary>
    public const float StartleSeconds = 0.4f;
    /// <summary>The pace's share kept with each loop, and how much longer the wait for a strike grows with each.</summary>
    public const float CalmedPace = 0.8f, CalmedPatience = 1.25f;
    private const float WindUpPace = 0.35f;

    private readonly SplitMix chance;
    private readonly List<Vector2> line = new();
    private readonly TimingAssist assist;
    private Vector2 heading;
    private float turn, gaitLeft, untilStrike, windUp, clock;
    private bool resting;
    private bool mustLift;

    public LoopTaskData Task { get; }
    public LoopGait Gait { get; }
    /// <summary>The Pokémon's pace as it began, in units a second.</summary>
    public float Pace { get; }

    /// <summary>The middle of the Pokémon's body.</summary>
    public Vector2 At { get; private set; }
    /// <summary>The way it is going (its sprite faces by it); nought while it stands.</summary>
    public Vector2 Velocity { get; private set; }
    public Vector2 Disc { get; private set; } = new(960, 920);
    /// <summary>Whether the disc is drawing.</summary>
    public bool Drawing { get; private set; }
    public IReadOnlyList<Vector2> Line => line;
    public int Calm { get; private set; }
    public int Strength { get; private set; } = Charges;
    public LoopOutcome Outcome { get; private set; }
    public int Ticks { get; private set; }

    /// <summary>Whether it is winding up a strike, and how far through (0 to 1).</summary>
    public bool WindingUp => windUp > 0f;
    public float WindUpShare => windUp > 0f ? Math.Clamp(1f - windUp / WindUpTime, 0f, 1f) : 0f;
    /// <summary>The seconds until it next winds up.</summary>
    public float UntilStrike => untilStrike;
    private float WindUpTime => TimingAssists.Window(WindUpSeconds, assist);

    public LoopCatch(LoopTaskData task, uint seed, TimingAssist assist = TimingAssist.Off)
    {
        Task = task;
        this.assist = assist;
        chance = new SplitMix(seed);
        var species = PokemonDatabase.Get(task.Species) ?? throw new ArgumentException($"there is no species '{task.Species}'");
        Gait = GaitOf(species);
        Pace = PaceOf(species);
        At = new Vector2(chance.Range(RoamLeast.X + 200, RoamMost.X - 200), chance.Range(RoamLeast.Y + 60, RoamMost.Y - 60));
        heading = OfAngle(chance.Range(0, MathF.Tau));
        untilStrike = NextWait();
    }

    /// <summary>How a species moves here, by the shape its Pokédex page sorts it under.</summary>
    public static LoopGait GaitOf(PokemonSpecies species) => species.Shape switch
    {
        "Wings" or "Bug Wings" => LoopGait.Soar,
        "Squiggle" or "Tentacles" => LoopGait.Weave,
        "Fish" => LoopGait.Dart,
        "Ball" or "Blob" or "Heads" => LoopGait.Drift,
        "Upright" or "Humanoid" or "Arms" => LoopGait.Walk,
        _ => LoopGait.Run
    };

    /// <summary>A species' pace, in units a second, by its base Speed.</summary>
    public static float PaceOf(PokemonSpecies species) => Math.Clamp(40f + 1.6f * species.BaseSpeed, 60f, 260f);

    private static Vector2 OfAngle(float a) => new(MathF.Cos(a), MathF.Sin(a));

    private float NextWait() => Task.StrikeEvery * chance.Range(0.75f, 1.25f) * MathF.Pow(CalmedPatience, Calm);

    /// <summary>One tick of the try.</summary>
    public LoopEvents Step(LoopInput input)
    {
        if (Outcome != LoopOutcome.Going) return LoopEvents.None;
        Ticks++;
        var events = LoopEvents.None;
        Move();
        events |= MoveDisc(input);
        if (Outcome == LoopOutcome.Going) events |= Strikes();
        return events;
    }

    // ------------------------------------------------------------------ the Pokémon

    private void Move()
    {
        clock += Tick;
        float pace = Pace * MathF.Pow(CalmedPace, Calm);
        gaitLeft -= Tick;
        if (gaitLeft <= 0f) NextStretch();
        var way = heading;
        float share = 1f;
        switch (Gait)
        {
            case LoopGait.Run:
                share = resting ? 0f : 1.15f;
                break;
            case LoopGait.Walk:
                share = resting ? 0f : 0.85f;
                break;
            case LoopGait.Soar:
                heading = Turned(heading, turn * Tick);
                way = heading;
                break;
            case LoopGait.Weave:
                way = Turned(heading, MathF.Sin(clock * 4.2f) * 0.95f);
                break;
            case LoopGait.Dart:
                share = resting ? 0.25f : 2.1f;
                break;
            case LoopGait.Drift:
                heading = Turned(heading, turn * 0.4f * Tick);
                way = heading;
                share = 0.65f;
                break;
        }
        if (WindingUp) share *= WindUpPace;
        Velocity = way * (pace * share);
        var next = At + Velocity * Tick;
        if (next.X < RoamLeast.X || next.X > RoamMost.X) heading.X = next.X < RoamLeast.X ? MathF.Abs(heading.X) : -MathF.Abs(heading.X);
        if (next.Y < RoamLeast.Y || next.Y > RoamMost.Y) heading.Y = next.Y < RoamLeast.Y ? MathF.Abs(heading.Y) : -MathF.Abs(heading.Y);
        At = Vector2.Clamp(next, RoamLeast, RoamMost);
    }

    /// <summary>The next stretch of its gait: a new way, and for the gaits that stop, a rest or a run.</summary>
    private void NextStretch()
    {
        switch (Gait)
        {
            case LoopGait.Run:
                resting = !resting;
                gaitLeft = resting ? chance.Range(0.5f, 1.0f) : chance.Range(1.0f, 2.0f);
                if (!resting) heading = OfAngle(chance.Range(0, MathF.Tau));
                break;
            case LoopGait.Walk:
                resting = !resting;
                gaitLeft = resting ? chance.Range(0.6f, 1.2f) : chance.Range(1.2f, 2.2f);
                if (!resting) heading = OfAngle(chance.Range(0, MathF.Tau));
                break;
            case LoopGait.Soar:
                gaitLeft = chance.Range(1.0f, 2.0f);
                turn = chance.Range(0.6f, 1.6f) * (chance.Chance(0.5f) ? 1f : -1f);
                break;
            case LoopGait.Weave:
                gaitLeft = chance.Range(1.4f, 2.6f);
                heading = OfAngle(chance.Range(0, MathF.Tau));
                break;
            case LoopGait.Dart:
                resting = !resting;
                gaitLeft = resting ? chance.Range(0.7f, 1.3f) : 0.4f;
                if (!resting) heading = OfAngle(chance.Range(0, MathF.Tau));
                break;
            case LoopGait.Drift:
                gaitLeft = chance.Range(1.5f, 3.0f);
                turn = chance.Range(-1.8f, 1.8f);
                break;
        }
    }

    private static Vector2 Turned(Vector2 v, float a)
    {
        float c = MathF.Cos(a), s = MathF.Sin(a);
        return new Vector2(v.X * c - v.Y * s, v.X * s + v.Y * c);
    }

    // ------------------------------------------------------------------ the disc

    private LoopEvents MoveDisc(LoopInput input)
    {
        var stick = input.Stick;
        if (stick.LengthSquared() > 1f) stick = Vector2.Normalize(stick);
        var to = Vector2.Clamp(input.Pointer ?? Disc + stick * (DiscSpeed * Tick), DiscLeast, DiscMost);
        if (!input.Down)
        {
            Disc = to;
            Lift();
            mustLift = false;
            return LoopEvents.None;
        }
        // A line just broken is not taken up again until the disc has been lifted
        if (mustLift)
        {
            Disc = to;
            return LoopEvents.None;
        }
        if (!Drawing)
        {
            // The line begins where the disc comes down, never joined to where it was
            Disc = to;
            Drawing = true;
            line.Add(to);
            return LoopEvents.None;
        }
        Disc = to;
        return Extend(to);
    }

    private void Lift()
    {
        Drawing = false;
        line.Clear();
    }

    /// <summary>The line taken on to <paramref name="to"/>: closed where it crosses itself, its oldest end let go past its length.</summary>
    private LoopEvents Extend(Vector2 to)
    {
        var from = line[^1];
        if (Vector2.Distance(from, to) < PointSpacing) return LoopEvents.None;
        // The newest stretch against every earlier one but the one it grows from, the nearest crossing first
        int crossed = -1;
        float nearest = float.MaxValue;
        var cross = default(Vector2);
        for (int i = 0; i + 2 < line.Count; i++)
            if (Crossing(from, to, line[i], line[i + 1], out float along, out var point) && along < nearest)
                (crossed, nearest, cross) = (i, along, point);
        if (crossed >= 0)
        {
            var shape = new List<Vector2> { cross };
            shape.AddRange(line.Skip(crossed + 1));
            if (Counts(shape, At)) return CountLoop(to);
            // A shape that holds nothing is cut out, and the line goes on from where it crossed
            line.RemoveRange(crossed + 1, line.Count - crossed - 1);
            line.Add(cross);
        }
        line.Add(to);
        Trim();
        return LoopEvents.None;
    }

    private LoopEvents CountLoop(Vector2 to)
    {
        Calm++;
        line.Clear();
        line.Add(to);
        windUp = 0f;
        // It strikes back at once: every loop but the last has to be got away from
        untilStrike = StartleSeconds;
        if (Calm < CalmNeeded) return LoopEvents.Looped;
        Outcome = LoopOutcome.Calmed;
        Velocity = Vector2.Zero;
        Lift();
        return LoopEvents.Looped | LoopEvents.Calmed;
    }

    private void Trim()
    {
        float length = 0f;
        for (int i = line.Count - 1; i > 0; i--)
        {
            length += Vector2.Distance(line[i], line[i - 1]);
            if (length > LineLength)
            {
                line.RemoveRange(0, i);
                return;
            }
        }
    }

    // ------------------------------------------------------------------ the strikes

    private LoopEvents Strikes()
    {
        if (windUp > 0f)
        {
            windUp -= Tick;
            if (windUp > 0f) return LoopEvents.None;
            windUp = 0f;
            untilStrike = NextWait();
            if (!WithinReach(line, At, Task.Reach)) return LoopEvents.Struck;
            Lift();
            mustLift = true;
            // The assist makes the lift itself: the line goes and nothing is spent
            if (assist == TimingAssist.Automatic) return LoopEvents.Struck;
            Strength--;
            if (Strength > 0) return LoopEvents.Struck | LoopEvents.Broke;
            Outcome = LoopOutcome.SlippedAway;
            return LoopEvents.Struck | LoopEvents.Broke | LoopEvents.Slipped;
        }
        untilStrike -= Tick;
        if (untilStrike > 0f) return LoopEvents.None;
        windUp = WindUpTime;
        return LoopEvents.WindUp;
    }

    /// <summary>The try given up: the Pokémon slips away.</summary>
    public void GiveUp()
    {
        if (Outcome != LoopOutcome.Going) return;
        Outcome = LoopOutcome.SlippedAway;
        Lift();
    }

    // ------------------------------------------------------------------ the geometry

    /// <summary>Whether a shape closed by the line counts as a loop round a body at <paramref name="body"/>.</summary>
    public static bool Counts(IReadOnlyList<Vector2> shape, Vector2 body) =>
        shape.Count >= 3 && Holds(shape, body) && AreaOf(shape) >= LeastLoopArea && NearestTo(shape, body, closed: true) >= LoopClearance;

    /// <summary>Whether any part of the line comes within <paramref name="reach"/> of a place.</summary>
    public static bool WithinReach(IReadOnlyList<Vector2> points, Vector2 at, float reach) =>
        points.Count > 0 && NearestTo(points, at, closed: false) <= reach;

    private static float NearestTo(IReadOnlyList<Vector2> points, Vector2 at, bool closed)
    {
        if (points.Count == 1) return Vector2.Distance(points[0], at);
        float best = float.MaxValue;
        int segments = closed ? points.Count : points.Count - 1;
        for (int i = 0; i < segments; i++)
        {
            Vector2 a = points[i], b = points[(i + 1) % points.Count], d = b - a;
            float t = d.LengthSquared() > 0f ? Math.Clamp(Vector2.Dot(at - a, d) / d.LengthSquared(), 0f, 1f) : 0f;
            best = Math.Min(best, Vector2.Distance(a + d * t, at));
        }
        return best;
    }

    /// <summary>Whether the closed shape holds a point (a ray cast across it).</summary>
    public static bool Holds(IReadOnlyList<Vector2> shape, Vector2 p)
    {
        bool inside = false;
        for (int i = 0, j = shape.Count - 1; i < shape.Count; j = i++)
        {
            Vector2 a = shape[i], b = shape[j];
            if ((a.Y > p.Y) != (b.Y > p.Y) && p.X < a.X + (p.Y - a.Y) / (b.Y - a.Y) * (b.X - a.X)) inside = !inside;
        }
        return inside;
    }

    public static float AreaOf(IReadOnlyList<Vector2> shape)
    {
        float twice = 0f;
        for (int i = 0, j = shape.Count - 1; i < shape.Count; j = i++)
            twice += shape[j].X * shape[i].Y - shape[i].X * shape[j].Y;
        return MathF.Abs(twice) / 2f;
    }

    /// <summary>Whether the stretch a to b crosses c to d, how far along a to b (0 to 1) and where.</summary>
    private static bool Crossing(Vector2 a, Vector2 b, Vector2 c, Vector2 d, out float along, out Vector2 point)
    {
        along = 0f;
        point = default;
        Vector2 r = b - a, s = d - c;
        float denominator = r.X * s.Y - r.Y * s.X;
        if (MathF.Abs(denominator) < 1e-6f) return false;
        var between = c - a;
        float t = (between.X * s.Y - between.Y * s.X) / denominator;
        float u = (between.X * r.Y - between.Y * r.X) / denominator;
        if (t < 0f || t > 1f || u < 0f || u > 1f) return false;
        along = t;
        point = a + r * t;
        return true;
    }
}

/// <summary>
/// A hand that draws rings round the Pokémon just outside its reach and lifts the disc while it winds up: what plays a
/// try in a game with no window, in the tests and in the harness. It draws no chance.
/// </summary>
public sealed class LoopAutopilot
{
    /// <summary>The pace its disc goes round at, in units a second (a mouse's).</summary>
    public const float HandSpeed = 1500f;
    /// <summary>How far outside the strike's reach its ring is drawn.</summary>
    public const float Margin = 24f;
    /// <summary>The ticks a try is given before it is given up (three minutes).</summary>
    public const int TickCap = 180 * LoopCatch.TicksPerSecond;

    private float angle;

    public LoopInput Next(LoopCatch game)
    {
        float radius = game.Task.Reach + Margin;
        bool down = !game.WindingUp;
        // A lifted disc waits where the next ring will begin
        if (down) angle += HandSpeed / radius * LoopCatch.Tick;
        var at = game.At + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius;
        return new LoopInput(Vector2.Zero, at, down);
    }

    /// <summary>A try played to its end; one that outlasts the cap is given up.</summary>
    public static LoopOutcome Play(LoopCatch game)
    {
        var hand = new LoopAutopilot();
        while (game.Outcome == LoopOutcome.Going && game.Ticks < TickCap) game.Step(hand.Next(game));
        game.GiveUp();
        return game.Outcome;
    }
}
