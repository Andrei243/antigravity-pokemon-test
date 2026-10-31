using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models;

/// <summary>A course's medal (plan 15 · E8), by the course's total score: better is greater.</summary>
public enum AthlonMedal { None, Bronze, Silver, Gold }

/// <summary>
/// The Pokéathlon's data (plan 15 · E8; <c>Data/athlon.json</c>, written by hand): HeartGold's five courses as it pairs
/// the ten events, the medals' bands of our own on a course's total, and the Athlete Shop's goods, Platinum's items at
/// prices in Athlete Points of our own. A course naming an event this game hasn't built is kept out of
/// <see cref="Courses"/> and listed in <see cref="Waiting"/> until the event is built.
/// </summary>
public static class AthlonData
{
    public sealed class FileCourse
    {
        public string Key { get; set; } = "";
        public List<string> Events { get; set; } = new();
    }

    public sealed class FileMedals
    {
        public int Gold { get; set; }
        public int Silver { get; set; }
        public int Bronze { get; set; }
    }

    public sealed class FileGood
    {
        public string Item { get; set; } = "";
        public int Points { get; set; }
    }

    public sealed class AthlonFile
    {
        public List<FileCourse> Courses { get; set; } = new();
        public FileMedals Medals { get; set; } = new();
        public List<FileGood> Shop { get; set; } = new();
    }

    // (in two pieces: a literal of a section's name and a dot reads as a text key to TextTests)
    private const string FileName = "athlon" + ".json";

    private static readonly Lazy<AthlonFile> file = new(() => GameDataFiles.Load<AthlonFile>(FileName));

    /// <summary>The file as written.</summary>
    public static AthlonFile File => file.Value;

    /// <summary>An event's kind by its name in the file, or null where this game has no such event yet.</summary>
    public static AthlonEventKind? KindOf(string name) => Enum.TryParse<AthlonEventKind>(name, out var k) && Enum.IsDefined(k) ? k : null;

    /// <summary>The courses of the file whose events are all built, in the file's order.</summary>
    public static IReadOnlyList<AthlonCourse> Courses =>
        File.Courses.Where(c => c.Events.All(e => KindOf(e) != null))
            .Select(c => new AthlonCourse(c.Key, c.Events.Select(e => KindOf(e)!.Value).ToList()))
            .ToList();

    /// <summary>The courses left out for an event not built yet, by key, each with the events it waits for.</summary>
    public static IReadOnlyDictionary<string, IReadOnlyList<string>> Waiting =>
        File.Courses.Where(c => c.Events.Any(e => KindOf(e) == null))
            .ToDictionary(c => c.Key, c => (IReadOnlyList<string>)c.Events.Where(e => KindOf(e) == null).ToList());

    /// <summary>The medal a course's total score wins: gold, silver or bronze by the file's bands, none under bronze.</summary>
    public static AthlonMedal MedalFor(int score) =>
        score >= File.Medals.Gold ? AthlonMedal.Gold
        : score >= File.Medals.Silver ? AthlonMedal.Silver
        : score >= File.Medals.Bronze ? AthlonMedal.Bronze
        : AthlonMedal.None;

    /// <summary>The Athlete Shop's goods with their prices in points, items the data has (an unknown name is skipped).</summary>
    public static IReadOnlyList<(ItemData Item, int Points)> Shop =>
        File.Shop.Select(g => (Item: ItemDatabase.Get(g.Item), g.Points)).Where(g => g.Item != null && g.Points > 0)
            .Select(g => (g.Item!, g.Points)).ToList();
}

/// <summary>
/// What three of the batch's events share (plan 15 · E8): a body on a floor, pushed by the arrows (harder by Speed),
/// lunging on the button (further by Power, again sooner by Skill), and two that meet pushed apart as heavy things are,
/// each as heavy as its Power and Stamina make it; Ring Drop's rules, which E6 keeps for itself.
/// </summary>
internal static class AthlonBodies
{
    public const float Body = 12f, Friction = 0.9f;
    public const int LungeFrames = 8;

    public static float Push(AthleteStats s) => 0.12f + 0.05f * s.Speed;
    public static float LungeSpeed(AthleteStats s) => 1.5f + 0.5f * s.Power;
    public static int LungeRest(AthleteStats s) => 30 - 3 * s.Skill;
    public static float Mass(AthleteStats s) => 2f + s.Stamina + 0.5f * s.Power;

    /// <summary>The way the arrows hold, made a unit (or nought).</summary>
    public static Vector2 Way(AthlonInput input)
    {
        var way = new Vector2(Math.Sign(input.Dx), Math.Sign(input.Dy));
        return way == Vector2.Zero ? way : Vector2.Normalize(way);
    }

    /// <summary>Two bodies that overlap pushed apart, a lunge harder; true when they touched.</summary>
    public static bool Collide(ref Vector2 aAt, ref Vector2 aV, float ma, bool aLunging, ref Vector2 bAt, ref Vector2 bV, float mb, bool bLunging)
    {
        var d = bAt - aAt;
        float dist = d.Length();
        if (dist >= 2f * Body) return false;
        var n = dist > 0.001f ? d / dist : new Vector2(1f, 0f);
        float overlap = 2f * Body - dist;
        aAt -= n * overlap * mb / (ma + mb);
        bAt += n * overlap * ma / (ma + mb);
        float closing = Vector2.Dot(aV - bV, n);
        if (closing > 0f)
        {
            float kick = aLunging || bLunging ? 1.5f : 1f;
            float impulse = 1.8f * closing / (1f / ma + 1f / mb) * kick;
            aV -= n * impulse / ma;
            bV += n * impulse / mb;
        }
        return true;
    }

    /// <summary>The arrows that walk toward a way, each axis held only where it counts.</summary>
    public static (int Dx, int Dy) ArrowsToward(Vector2 want)
    {
        int dx = MathF.Abs(want.X) > MathF.Abs(want.Y) * 0.4f ? Math.Sign(want.X) : 0;
        int dy = MathF.Abs(want.Y) > MathF.Abs(want.X) * 0.4f ? Math.Sign(want.Y) : 0;
        return (dx, dy);
    }

    /// <summary>A way turned by an angle (radians).</summary>
    public static Vector2 Turn(Vector2 v, float angle)
    {
        float c = MathF.Cos(angle), s = MathF.Sin(angle);
        return new Vector2(v.X * c - v.Y * s, v.X * s + v.Y * c);
    }
}

/// <summary>
/// Relay Run (plan 15 · E8; laps against a clock, our own numbers on HeartGold's shape): a minute on an oval track of
/// <see cref="LapLength"/> units a lap, one of each team's three running at a time. A runner runs by itself at a pace by
/// its Speed and tires as it runs (less by Stamina); a tired one slows to half its pace and an exhausted one to a crawl.
/// Each stride is a little longer or shorter on the event's own generator. The button spurts (faster for <see cref="SpurtFrames"/>, tiring twice as fast, again sooner by Skill); a switch hands
/// over to the next of the three, which takes a moment, while those resting get their breath back. Points are four a lap,
/// counted by the quarter.
/// </summary>
public sealed class RelayRun : AthlonEvent
{
    public const int LapLength = 600, Tired = 300, SwitchFrames = 15, SpurtFrames = 15, PointsPerLap = 4;
    public const float SpurtBoost = 1.4f;
    public const int Minute = 60 * FramesPerSecond;

    /// <summary>One team's relay.</summary>
    public sealed class Relay
    {
        public int Active, Switching, Spurt, SpurtRest, Spurts, Handovers;
        public float Distance, Speed;
        /// <summary>The breath at which a rival means to hand over, drawn as each runner starts (NaN: not yet).</summary>
        public float HandAt = float.NaN;
        public readonly float[] Fatigue = new float[AthlonRun.TeamSize];
        public int Laps => (int)(Distance / LapLength);
        /// <summary>How far round the lap, 0 to 1.</summary>
        public float Round => Distance % LapLength / LapLength;
    }

    private readonly Relay[] relays = new Relay[AthlonRun.TeamCount];

    public RelayRun(IReadOnlyList<AthlonTeam> teams, int seed) : base(AthlonEventKind.RelayRun, teams, seed)
    {
        for (int t = 0; t < relays.Length; t++) relays[t] = new Relay();
    }

    public override int Length => Minute;
    public override AthleteStat Leans => AthleteStat.Stamina;
    public IReadOnlyList<Relay> Relays => relays;

    public static float Pace(AthleteStats s) => 3f + 0.6f * s.Speed;
    /// <summary>The breath a frame of running costs: Stamina 1 is spent in about eight seconds, 5 in twenty-five.</summary>
    public static float Drain(AthleteStats s) => 1.4f - 0.2f * s.Stamina;
    public const float Recover = 2f;
    public static int SpurtRestFrames(AthleteStats s) => 36 - 4 * s.Skill;

    /// <summary>The share of its pace a runner keeps as it tires: all of it fresh, half at the line, a fifth past it.</summary>
    public static float Kept(float fatigue) => fatigue >= Tired ? 0.2f : 1f - 0.5f * fatigue / Tired;

    public Athlete RunnerOf(int team) => Teams[team].Athletes[relays[team].Active];

    protected override void Play(AthlonInput[] inputs)
    {
        for (int t = 0; t < relays.Length; t++)
        {
            var r = relays[t];
            for (int a = 0; a < AthlonRun.TeamSize; a++)
                if (a != r.Active) r.Fatigue[a] = Math.Max(0f, r.Fatigue[a] - Recover);
            if (r.SpurtRest > 0) r.SpurtRest--;
            if (r.Switching > 0)
            {
                r.Switching--;
                continue;
            }
            if (inputs[t].Switch != 0)
            {
                r.Active = ((r.Active + inputs[t].Switch) % AthlonRun.TeamSize + AthlonRun.TeamSize) % AthlonRun.TeamSize;
                r.Switching = SwitchFrames;
                r.Speed = 0f;
                r.Spurt = 0;
                r.Handovers++;
                r.HandAt = float.NaN;
                continue;
            }
            var stats = RunnerOf(t).Stats;
            if (inputs[t].Tap && r.Spurt == 0 && r.SpurtRest == 0)
            {
                r.Spurt = SpurtFrames;
                r.SpurtRest = SpurtFrames + SpurtRestFrames(stats);
                r.Spurts++;
            }
            float top = Pace(stats) * Kept(r.Fatigue[r.Active]) * (r.Spurt > 0 ? SpurtBoost : 1f);
            // A runner gathers its pace over half a second and gives it up at once; no two strides are quite alike
            r.Speed = Math.Min(top, r.Speed + top / 15f);
            r.Distance += r.Speed * 0.05f * Wobble(Rng);
            r.Distance += r.Speed;
            r.Fatigue[r.Active] = Math.Min(Tired + 60f, r.Fatigue[r.Active] + Drain(stats) * (r.Spurt > 0 ? 2f : 1f));
            if (r.Spurt > 0) r.Spurt--;
        }
    }

    protected override AthlonInput Think(int team, float knack, Random rng)
    {
        var r = relays[team];
        if (r.Switching > 0) return AthlonInput.None;
        // A tiring runner is handed over to the most rested of the other two, once that one has its breath back
        if (float.IsNaN(r.HandAt)) r.HandAt = Tired * (0.95f - 0.35f * knack + 0.25f * Wobble(rng));
        if (r.Fatigue[r.Active] >= r.HandAt)
        {
            int best = -1;
            for (int a = 0; a < AthlonRun.TeamSize; a++)
                if (a != r.Active && (best < 0 || r.Fatigue[a] < r.Fatigue[best])) best = a;
            if (best >= 0 && r.Fatigue[best] < Tired * 0.3f && rng.NextDouble() < 0.1 + 0.3 * knack)
                return new AthlonInput(Switch: ((best - r.Active) % AthlonRun.TeamSize + AthlonRun.TeamSize) % AthlonRun.TeamSize == 1 ? 1 : -1);
        }
        // A fresh runner spurts now and then
        bool spurt = r.Spurt == 0 && r.SpurtRest == 0 && r.Fatigue[r.Active] < Tired * 0.4f && rng.NextDouble() < 0.02 + 0.06 * knack;
        return spurt ? new AthlonInput(Tap: true) : AthlonInput.None;
    }

    /// <summary>Four a lap, counted by the quarter lap run.</summary>
    public override int Points(int team) => (int)(relays[team].Distance * PointsPerLap / LapLength);
}

/// <summary>
/// Circle Push (plan 15 · E8; a push for a place, our own numbers on HeartGold's shape): forty seconds on a square floor
/// <see cref="Half"/> units from its middle to each wall, with a circle of radius <see cref="CircleRadius"/> at its
/// middle; one of each team's three on the floor, moving and lunging as in Ring Drop, held in by the walls. Every frame
/// a Pokémon stands in the circle counts for its team, twice where it is alone there; a point for every
/// <see cref="FramesPerPoint"/>. Pushing tires a Pokémon (less by Stamina) and a tired one pushes at half its strength;
/// a switch sends the next of the three in at the team's own wall.
/// </summary>
public sealed class CirclePush : AthlonEvent
{
    public const float Half = 140f, CircleRadius = 48f, StartOut = 110f;
    public const int Tired = 600, SwitchFrames = 15, FramesPerPoint = 15;
    public const int Seconds40 = 40 * FramesPerSecond;

    public sealed class Pusher
    {
        public int Active, Lunge, Rest, Switching, Held, Alone;
        public Vector2 At, Velocity, Facing = new(0f, -1f);
        public readonly int[] Fatigue = new int[AthlonRun.TeamSize];
        /// <summary>A rival's way, kept for a few frames between thoughts.</summary>
        public Vector2 Aim;
        public int AimFor;
        /// <summary>The frames counted toward the points: a frame in the circle, two when alone in it.</summary>
        public int Counted => Held + Alone;
    }

    private readonly Pusher[] pushers = new Pusher[AthlonRun.TeamCount];

    public CirclePush(IReadOnlyList<AthlonTeam> teams, int seed) : base(AthlonEventKind.CirclePush, teams, seed)
    {
        for (int t = 0; t < pushers.Length; t++)
        {
            pushers[t] = new Pusher();
            Place(t);
        }
    }

    public override int Length => Seconds40;
    public override AthleteStat Leans => AthleteStat.Power;
    public IReadOnlyList<Pusher> Pushers => pushers;

    /// <summary>Where a team comes onto the floor: the player at the south wall, the rivals west, north and east.</summary>
    public static Vector2 StartOf(int team) => team switch
    {
        0 => new Vector2(0f, StartOut),
        1 => new Vector2(-StartOut, 0f),
        2 => new Vector2(0f, -StartOut),
        _ => new Vector2(StartOut, 0f)
    };

    /// <summary>The breath a frame of pushing costs.</summary>
    public static int Strain(AthleteStats s) => 7 - s.Stamina;

    public Athlete ActiveOf(int team) => Teams[team].Athletes[pushers[team].Active];

    public bool InCircle(int team) => pushers[team].Switching == 0 && pushers[team].At.Length() <= CircleRadius;

    private void Place(int team)
    {
        var p = pushers[team];
        p.At = StartOf(team);
        p.Velocity = Vector2.Zero;
        p.Facing = -Vector2.Normalize(p.At);
        p.Lunge = 0;
        p.Rest = 0;
    }

    protected override void Play(AthlonInput[] inputs)
    {
        for (int t = 0; t < pushers.Length; t++)
        {
            var p = pushers[t];
            for (int a = 0; a < AthlonRun.TeamSize; a++)
                if (a != p.Active && p.Fatigue[a] > 0) p.Fatigue[a] = Math.Max(0, p.Fatigue[a] - 4);
            if (p.Switching > 0)
            {
                if (--p.Switching == 0) Place(t);
                continue;
            }
            if (inputs[t].Switch != 0)
            {
                p.Active = ((p.Active + inputs[t].Switch) % AthlonRun.TeamSize + AthlonRun.TeamSize) % AthlonRun.TeamSize;
                p.Switching = SwitchFrames;
                p.Velocity = Vector2.Zero;
                continue;
            }
            var stats = ActiveOf(t).Stats;
            bool tired = p.Fatigue[p.Active] >= Tired;
            var way = AthlonBodies.Way(inputs[t]);
            if (way != Vector2.Zero)
            {
                p.Facing = way;
                p.Velocity += way * AthlonBodies.Push(stats) * (tired ? 0.5f : 1f);
                p.Fatigue[p.Active] = Math.Min(Tired, p.Fatigue[p.Active] + Strain(stats));
            }
            if (p.Rest > 0) p.Rest--;
            if (inputs[t].Tap && p.Rest == 0)
            {
                p.Velocity += p.Facing * AthlonBodies.LungeSpeed(stats) * (tired ? 0.5f : 1f);
                p.Lunge = AthlonBodies.LungeFrames;
                p.Rest = AthlonBodies.LungeRest(stats);
            }
            if (p.Lunge > 0) p.Lunge--;
            p.Velocity *= AthlonBodies.Friction;
            p.At += p.Velocity;
        }

        for (int i = 0; i < pushers.Length; i++)
            for (int j = i + 1; j < pushers.Length; j++)
            {
                var a = pushers[i];
                var b = pushers[j];
                if (a.Switching > 0 || b.Switching > 0) continue;
                AthlonBodies.Collide(ref a.At, ref a.Velocity, AthlonBodies.Mass(ActiveOf(i).Stats), a.Lunge > 0,
                    ref b.At, ref b.Velocity, AthlonBodies.Mass(ActiveOf(j).Stats), b.Lunge > 0);
            }

        // The walls hold everyone in, a body's width from them
        float wall = Half - AthlonBodies.Body;
        foreach (var p in pushers)
        {
            if (MathF.Abs(p.At.X) > wall) { p.At.X = Math.Sign(p.At.X) * wall; p.Velocity.X = -p.Velocity.X * 0.4f; }
            if (MathF.Abs(p.At.Y) > wall) { p.At.Y = Math.Sign(p.At.Y) * wall; p.Velocity.Y = -p.Velocity.Y * 0.4f; }
        }

        int inside = 0;
        for (int t = 0; t < pushers.Length; t++) if (InCircle(t)) inside++;
        for (int t = 0; t < pushers.Length; t++)
        {
            if (!InCircle(t)) continue;
            pushers[t].Held++;
            if (inside == 1) pushers[t].Alone++;
        }
    }

    protected override AthlonInput Think(int team, float knack, Random rng)
    {
        var p = pushers[team];
        if (p.Switching > 0) return AthlonInput.None;
        if (p.Fatigue[p.Active] >= Tired - 40)
        {
            int best = -1;
            for (int a = 0; a < AthlonRun.TeamSize; a++)
                if (a != p.Active && (best < 0 || p.Fatigue[a] < p.Fatigue[best])) best = a;
            if (best >= 0 && p.Fatigue[best] < Tired / 3 && rng.NextDouble() < 0.05 + 0.2 * knack)
                return new AthlonInput(Switch: ((best - p.Active) % AthlonRun.TeamSize + AthlonRun.TeamSize) % AthlonRun.TeamSize == 1 ? 1 : -1);
        }
        // Into the circle; once there, at whoever else is in it
        int target = -1;
        float nearest = float.MaxValue;
        for (int t = 0; t < pushers.Length; t++)
        {
            if (t == team || !InCircle(t)) continue;
            float d = Vector2.Distance(pushers[t].At, p.At);
            if (d < nearest) { nearest = d; target = t; }
        }
        if (--p.AimFor <= 0)
        {
            Vector2 want = p.At.Length() > CircleRadius * 0.5f ? -p.At : target >= 0 ? pushers[target].At - p.At : -p.At;
            p.Aim = AthlonBodies.Turn(want, Wobble(rng) * (1f - knack) * 1.2f);
            p.AimFor = 6 + rng.Next(6);
        }
        var (dx, dy) = p.Aim.Length() < 3f ? (0, 0) : AthlonBodies.ArrowsToward(p.Aim);
        bool lunge = target >= 0 && nearest < 4f * AthlonBodies.Body && p.Rest == 0 && rng.NextDouble() < 0.1 + 0.3 * knack;
        return new AthlonInput(Tap: lunge, Dx: dx, Dy: dy);
    }

    public override int Points(int team) => pushers[team].Counted / FramesPerPoint;
}

/// <summary>
/// Goal Roll (plan 15 · E8; a ball and four goals, our own numbers on HeartGold's shape): a minute on a square pitch
/// <see cref="Half"/> units from its middle to each side, a team's goal at the middle of its own side (the player's the
/// south, the rivals' west, north and east as in Ring Drop), one of each team's three on the pitch at a time. A Pokémon
/// that runs into the ball knocks it on (harder by Power); the button lunges (as in Ring Drop) and a lunge that meets
/// the ball strikes it along the striker's way, truer by Skill. The ball rolls, slowing, off the sides; into a goal it
/// scores <see cref="GoalPoints"/> for whoever touched it last, unless it is their own, which loses them
/// <see cref="OwnGoalPoints"/>. Then it starts again from the middle.
/// </summary>
public sealed class GoalRoll : AthlonEvent
{
    public const float Half = 150f, Mouth = 55f, BallRadius = 7f, BallFriction = 0.99f, StartOut = 110f;
    public const int GoalPoints = 15, OwnGoalPoints = 5, SwitchFrames = 15, Pause = 30;
    public const int Minute = 60 * FramesPerSecond;

    public sealed class Player
    {
        public int Active, Lunge, Rest, Switching, Goals, OwnGoals;
        public Vector2 At, Velocity, Facing = new(0f, -1f);
        public Vector2 Aim;
        public int AimFor;
    }

    private readonly Player[] players = new Player[AthlonRun.TeamCount];

    public Vector2 Ball { get; private set; }
    public Vector2 BallVelocity { get; private set; }
    /// <summary>Who touched the ball last (-1: nobody since it was put down).</summary>
    public int LastTouch { get; private set; } = -1;
    /// <summary>The frames left before a ball put down rolls again, after a goal.</summary>
    public int Waiting { get; private set; }
    /// <summary>The team whose goal the ball went into last, and when, for the picture (-1: none yet).</summary>
    public int LastGoalInto { get; private set; } = -1;
    public int LastGoalAt { get; private set; } = -9999;

    public GoalRoll(IReadOnlyList<AthlonTeam> teams, int seed) : base(AthlonEventKind.GoalRoll, teams, seed)
    {
        for (int t = 0; t < players.Length; t++)
        {
            players[t] = new Player();
            Place(t);
        }
    }

    public override int Length => Minute;
    public override AthleteStat Leans => AthleteStat.Skill;
    public IReadOnlyList<Player> Players => players;

    public static Vector2 StartOf(int team) => team switch
    {
        0 => new Vector2(0f, StartOut),
        1 => new Vector2(-StartOut, 0f),
        2 => new Vector2(0f, -StartOut),
        _ => new Vector2(StartOut, 0f)
    };

    /// <summary>The middle of a team's goal, on its own side.</summary>
    public static Vector2 GoalOf(int team) => StartOf(team) / StartOut * Half;

    /// <summary>How hard a strike sends the ball; how far off its way it goes (radians, less by Skill).</summary>
    public static float Strike(AthleteStats s) => 5f + 1.5f * s.Power;
    public static float Spread(AthleteStats s) => 0.35f - 0.06f * s.Skill;

    public Athlete ActiveOf(int team) => Teams[team].Athletes[players[team].Active];

    /// <summary>The ball put somewhere, rolling, last touched by a team (for tests; the game never calls it).</summary>
    internal void Rig(Vector2 ball, Vector2 velocity, int lastTouch)
    {
        Ball = ball;
        BallVelocity = velocity;
        LastTouch = lastTouch;
        Waiting = 0;
    }

    private void Place(int team)
    {
        var p = players[team];
        p.At = StartOf(team);
        p.Velocity = Vector2.Zero;
        p.Facing = -Vector2.Normalize(p.At);
        p.Lunge = 0;
        p.Rest = 0;
    }

    /// <summary>The goal a ball past a side has gone into, or -1 (off a side outside a mouth).</summary>
    private static int GoalAt(Vector2 b)
    {
        if (b.Y > Half && MathF.Abs(b.X) <= Mouth) return 0;
        if (b.X < -Half && MathF.Abs(b.Y) <= Mouth) return 1;
        if (b.Y < -Half && MathF.Abs(b.X) <= Mouth) return 2;
        if (b.X > Half && MathF.Abs(b.Y) <= Mouth) return 3;
        return -1;
    }

    protected override void Play(AthlonInput[] inputs)
    {
        for (int t = 0; t < players.Length; t++)
        {
            var p = players[t];
            if (p.Switching > 0)
            {
                if (--p.Switching == 0) Place(t);
                continue;
            }
            if (inputs[t].Switch != 0)
            {
                p.Active = ((p.Active + inputs[t].Switch) % AthlonRun.TeamSize + AthlonRun.TeamSize) % AthlonRun.TeamSize;
                p.Switching = SwitchFrames;
                p.Velocity = Vector2.Zero;
                continue;
            }
            var stats = ActiveOf(t).Stats;
            var way = AthlonBodies.Way(inputs[t]);
            if (way != Vector2.Zero)
            {
                p.Facing = way;
                p.Velocity += way * AthlonBodies.Push(stats);
            }
            if (p.Rest > 0) p.Rest--;
            if (inputs[t].Tap && p.Rest == 0)
            {
                p.Velocity += p.Facing * AthlonBodies.LungeSpeed(stats);
                p.Lunge = AthlonBodies.LungeFrames;
                p.Rest = AthlonBodies.LungeRest(stats);
            }
            if (p.Lunge > 0) p.Lunge--;
            p.Velocity *= AthlonBodies.Friction;
            p.At += p.Velocity;
            float wall = Half - AthlonBodies.Body;
            p.At = Vector2.Clamp(p.At, new Vector2(-wall), new Vector2(wall));
        }

        for (int i = 0; i < players.Length; i++)
            for (int j = i + 1; j < players.Length; j++)
            {
                var a = players[i];
                var b = players[j];
                if (a.Switching > 0 || b.Switching > 0) continue;
                AthlonBodies.Collide(ref a.At, ref a.Velocity, AthlonBodies.Mass(ActiveOf(i).Stats), a.Lunge > 0,
                    ref b.At, ref b.Velocity, AthlonBodies.Mass(ActiveOf(j).Stats), b.Lunge > 0);
            }

        if (Waiting > 0)
        {
            Waiting--;
            return;
        }

        // The ball: knocked on by whoever meets it, struck by a lunge, rolling and slowing, off the sides
        var ball = Ball;
        var bv = BallVelocity;
        for (int t = 0; t < players.Length; t++)
        {
            var p = players[t];
            if (p.Switching > 0) continue;
            var d = ball - p.At;
            float dist = d.Length();
            if (dist >= AthlonBodies.Body + BallRadius) continue;
            var n = dist > 0.001f ? d / dist : p.Facing;
            var stats = ActiveOf(t).Stats;
            ball = p.At + n * (AthlonBodies.Body + BallRadius);
            if (p.Lunge > 0)
            {
                float off = Wobble(Rng) * Spread(stats);
                bv = AthlonBodies.Turn(p.Facing, off) * Strike(stats);
                p.Lunge = 0;
            }
            else
            {
                float closing = Vector2.Dot(p.Velocity - bv, n);
                if (closing > 0f) bv += n * closing * (1.2f + 0.1f * stats.Power);
            }
            LastTouch = t;
        }
        ball += bv;
        bv *= BallFriction;

        int into = GoalAt(ball);
        if (into >= 0)
        {
            if (LastTouch >= 0)
            {
                if (LastTouch == into) players[into].OwnGoals++;
                else players[LastTouch].Goals++;
            }
            LastGoalInto = into;
            LastGoalAt = Frames;
            ball = Vector2.Zero;
            bv = Vector2.Zero;
            LastTouch = -1;
            Waiting = Pause;
        }
        else
        {
            float edge = Half - BallRadius;
            if (MathF.Abs(ball.X) > edge && !(MathF.Abs(ball.Y) <= Mouth)) { ball.X = Math.Sign(ball.X) * edge; bv.X = -bv.X * 0.7f; }
            if (MathF.Abs(ball.Y) > edge && !(MathF.Abs(ball.X) <= Mouth)) { ball.Y = Math.Sign(ball.Y) * edge; bv.Y = -bv.Y * 0.7f; }
        }
        Ball = ball;
        BallVelocity = bv;
    }

    protected override AthlonInput Think(int team, float knack, Random rng)
    {
        var p = players[team];
        if (p.Switching > 0) return AthlonInput.None;
        // The goal to strike at: the nearest that isn't its own, from the ball
        int aimAt = -1;
        float best = float.MaxValue;
        for (int t = 0; t < AthlonRun.TeamCount; t++)
        {
            if (t == team) continue;
            float d = Vector2.Distance(GoalOf(t), Ball);
            if (d < best) { best = d; aimAt = t; }
        }
        var toGoal = Vector2.Normalize(GoalOf(aimAt) - Ball);
        // Behind the ball as seen from that goal, then through it
        var behind = Ball - toGoal * (AthlonBodies.Body + BallRadius + 6f);
        if (--p.AimFor <= 0)
        {
            var want = Vector2.Distance(p.At, behind) > 10f ? behind - p.At : Ball - p.At;
            p.Aim = AthlonBodies.Turn(want, Wobble(rng) * (1f - knack) * 1.0f);
            p.AimFor = 4 + rng.Next(5);
        }
        var (dx, dy) = p.Aim.Length() < 2f ? (0, 0) : AthlonBodies.ArrowsToward(p.Aim);
        float toBall = Vector2.Distance(p.At, Ball);
        bool lined = Vector2.Dot(p.Facing, toGoal) > 0.6f;
        bool strike = toBall < AthlonBodies.Body + BallRadius + 14f && lined && p.Rest == 0 && rng.NextDouble() < 0.2 + 0.5 * knack;
        return new AthlonInput(Tap: strike, Dx: dx, Dy: dy);
    }

    public override int Points(int team) => Math.Max(0, GoalPoints * players[team].Goals - OwnGoalPoints * players[team].OwnGoals);
}
