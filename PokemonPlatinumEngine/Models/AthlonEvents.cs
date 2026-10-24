using System;
using System.Collections.Generic;
using System.Numerics;

namespace PokemonPlatinumEngine.Models;

/// <summary>
/// Hurdle Dash (plan 15 · E6; a run with jumps; our own numbers on HeartGold's shape, docs/mechanics/rulings.md,
/// "The Pokéathlon"): each team's three run a leg each of <see cref="LegLength"/> units, one after another, over
/// <see cref="HurdlesPerLeg"/> hurdles; the button jumps. A runner gathers speed by Stamina to a top speed by Speed,
/// stays up for frames by Jump, and a hurdle met on the ground trips it to a stop for frames fewer by Skill. A team
/// through all three legs scores by its time; one still running when the minute is up scores by how far it came.
/// </summary>
public sealed class HurdleDash : AthlonEvent
{
    public const int LegLength = 1200, Legs = 3, HurdleGap = 150, HurdlesPerLeg = 7, HurdleHalf = 8;
    public const int Minute = 60 * FramesPerSecond;

    /// <summary>One team's runner.</summary>
    public sealed class Runner
    {
        public int Leg;
        public float X, Speed;
        public int Air, AirLength, Down, NextHurdle, Trips;
        public int FinishedAt = -1;
        /// <summary>The frame a rival means to jump at the next hurdle, drawn as it comes in sight (-1: none yet).</summary>
        public float JumpAt = float.NaN;
        public bool Finished => FinishedAt >= 0;
        /// <summary>How far through the three legs, 0 to 1.</summary>
        public float Done => Math.Min(1f, (Leg * LegLength + X) / (Legs * LegLength));
    }

    private readonly Runner[] runners = new Runner[AthlonRun.TeamCount];

    public HurdleDash(IReadOnlyList<AthlonTeam> teams, int seed) : base(AthlonEventKind.HurdleDash, teams, seed)
    {
        for (int t = 0; t < runners.Length; t++) runners[t] = new Runner();
    }

    public override int Length => Minute;
    public override AthleteStat Leans => AthleteStat.Speed;
    public IReadOnlyList<Runner> Runners => runners;

    /// <summary>Where a leg's hurdles stand: every <see cref="HurdleGap"/> units from the first gap on.</summary>
    public static float HurdleAt(int index) => HurdleGap * (index + 1);

    public static float TopSpeed(AthleteStats s) => 3f + 0.6f * s.Speed;
    public static float Gather(AthleteStats s) => 0.1f + 0.04f * s.Stamina;
    public static int AirFrames(AthleteStats s) => 10 + 3 * s.Jump;
    public static int DownFrames(AthleteStats s) => 24 - 3 * s.Skill;

    /// <summary>The athlete running for a team now (the one whose leg it is; the last once through).</summary>
    public Athlete RunnerOf(int team) => Teams[team].Athletes[Math.Min(runners[team].Leg, Legs - 1)];

    protected override bool Finished => Array.TrueForAll(runners, r => r.Finished);

    protected override void Play(AthlonInput[] inputs)
    {
        for (int t = 0; t < runners.Length; t++)
        {
            var r = runners[t];
            if (r.Finished) continue;
            var stats = RunnerOf(t).Stats;
            if (r.Down > 0)
            {
                r.Down--;
                continue;
            }
            if (inputs[t].Tap && r.Air == 0)
            {
                r.AirLength = AirFrames(stats);
                r.Air = r.AirLength;
            }
            r.Speed = Math.Min(TopSpeed(stats), r.Speed + Gather(stats));
            r.X += r.Speed;
            if (r.Air > 0) r.Air--;

            // The next hurdle: cleared once the runner is past it (a step is never as long as a hurdle is wide, so no
            // runner passes it unseen), met if the runner is over it on the ground
            if (r.NextHurdle < HurdlesPerLeg)
            {
                float h = HurdleAt(r.NextHurdle);
                if (r.X > h + HurdleHalf)
                {
                    r.NextHurdle++;
                    r.JumpAt = float.NaN;
                }
                else if (r.X >= h - HurdleHalf && r.Air == 0)
                {
                    Trip(r, h, stats);
                    r.NextHurdle++;
                    r.JumpAt = float.NaN;
                }
            }

            if (r.X >= LegLength)
            {
                r.Leg++;
                r.X -= LegLength;
                r.Speed *= 0.5f;
                r.Air = 0;
                r.NextHurdle = 0;
                r.JumpAt = float.NaN;
                if (r.Leg >= Legs)
                {
                    r.Leg = Legs;
                    r.X = 0f;
                    r.FinishedAt = Frames + 1;
                }
            }
        }
    }

    private static void Trip(Runner r, float h, AthleteStats stats)
    {
        r.Down = DownFrames(stats);
        r.Speed = 0f;
        r.Air = 0;
        r.X = h + HurdleHalf + 1f;
        r.Trips++;
    }

    protected override AthlonInput Think(int team, float knack, Random rng)
    {
        var r = runners[team];
        if (r.Finished || r.Down > 0 || r.Air > 0 || r.NextHurdle >= HurdlesPerLeg) return AthlonInput.None;
        var stats = RunnerOf(team).Stats;
        float h = HurdleAt(r.NextHurdle);
        if (float.IsNaN(r.JumpAt))
        {
            // The jump that clears it with room either side starts half the leap's length before its middle; a rival
            // aims there, off by more the less it plays well, and now and then forgets to jump at all
            float leap = AirFrames(stats) * TopSpeed(stats);
            float aim = h - leap / 2f + Wobble(rng) * (1f - knack) * 60f;
            r.JumpAt = rng.NextDouble() < (1f - knack) * 0.12f ? float.PositiveInfinity : aim;
        }
        return r.X + r.Speed >= r.JumpAt ? new AthlonInput(Tap: true) : AthlonInput.None;
    }

    /// <summary>Through all three: what was left of the minute, a point for every twelve frames; else how far, out of 30.</summary>
    public override int Points(int team)
    {
        var r = runners[team];
        return r.Finished ? Math.Max(0, (Minute - r.FinishedAt) / 12) : (int)(r.Done * 30f);
    }
}

/// <summary>
/// Block Smash (plan 15 · E6; taps against a clock): forty seconds, each team at a stack of blocks with one of its three
/// at the stack and the others resting. A press hits once the last hit's swing is over (frames fewer by Speed) for
/// twice its Power, a point more pressed the moment the swing is over (as many frames' grace as its Skill); a block takes
/// <see cref="Toughness"/>. Every hit tires the hitter (less by Stamina); a tired one hits half as hard, and one resting
/// gets its breath back. A switch sends the next of the three in, which takes a moment. Points are two a block.
/// </summary>
public sealed class BlockSmash : AthlonEvent
{
    public const int Toughness = 40, Tired = 100, SwitchFrames = 12;
    public const int Seconds40 = 40 * FramesPerSecond;

    public sealed class Smasher
    {
        public int Active, Broken, Damage, Swing, Ready, Switching, Hits, CleanHits;
        public readonly int[] Fatigue = new int[AthlonRun.TeamSize];
        /// <summary>Frames since the last hit landed (for the picture's shake), or a large number.</summary>
        public int SinceHit = 99;
    }

    private readonly Smasher[] smashers = new Smasher[AthlonRun.TeamCount];

    public BlockSmash(IReadOnlyList<AthlonTeam> teams, int seed) : base(AthlonEventKind.BlockSmash, teams, seed)
    {
        for (int t = 0; t < smashers.Length; t++) smashers[t] = new Smasher();
    }

    public override int Length => Seconds40;
    public override AthleteStat Leans => AthleteStat.Power;
    public IReadOnlyList<Smasher> Smashers => smashers;

    public static int SwingFrames(AthleteStats s) => 8 - s.Speed;
    public static int Strain(AthleteStats s) => 12 - 2 * s.Stamina;
    public static int Blow(AthleteStats s, bool tired, bool clean) => (tired ? s.Power : 2 * s.Power) + (clean ? 1 : 0);

    public Athlete ActiveOf(int team) => Teams[team].Athletes[smashers[team].Active];

    protected override void Play(AthlonInput[] inputs)
    {
        for (int t = 0; t < smashers.Length; t++)
        {
            var s = smashers[t];
            s.SinceHit++;
            for (int a = 0; a < AthlonRun.TeamSize; a++)
                if (a != s.Active && s.Fatigue[a] > 0) s.Fatigue[a]--;
            if (s.Switching > 0)
            {
                s.Switching--;
                continue;
            }
            if (inputs[t].Switch != 0)
            {
                s.Active = ((s.Active + inputs[t].Switch) % AthlonRun.TeamSize + AthlonRun.TeamSize) % AthlonRun.TeamSize;
                s.Switching = SwitchFrames;
                s.Swing = 0;
                s.Ready = 0;
                continue;
            }
            var stats = Teams[t].Athletes[s.Active].Stats;
            if (s.Swing > 0)
            {
                s.Swing--;
                continue;
            }
            if (!inputs[t].Tap)
            {
                s.Ready++;
                continue;
            }
            bool clean = s.Ready < stats.Skill;
            bool tired = s.Fatigue[s.Active] >= Tired;
            s.Damage += Blow(stats, tired, clean);
            s.Hits++;
            if (clean) s.CleanHits++;
            s.Fatigue[s.Active] = Math.Min(Tired, s.Fatigue[s.Active] + Strain(stats));
            s.Swing = SwingFrames(stats);
            s.Ready = 0;
            s.SinceHit = 0;
            while (s.Damage >= Toughness)
            {
                s.Damage -= Toughness;
                s.Broken++;
            }
        }
    }

    protected override AthlonInput Think(int team, float knack, Random rng)
    {
        var s = smashers[team];
        if (s.Switching > 0 || s.Swing > 0) return AthlonInput.None;
        // A tired hitter is changed for the most rested of the other two, once that one has its breath
        if (s.Fatigue[s.Active] >= Tired - 10)
        {
            int best = -1;
            for (int a = 0; a < AthlonRun.TeamSize; a++)
                if (a != s.Active && (best < 0 || s.Fatigue[a] < s.Fatigue[best])) best = a;
            if (best >= 0 && s.Fatigue[best] < 40 && rng.NextDouble() < knack)
                return new AthlonInput(Switch: ((best - s.Active) % AthlonRun.TeamSize + AthlonRun.TeamSize) % AthlonRun.TeamSize == 1 ? 1 : -1);
        }
        return rng.NextDouble() < 0.3 + 0.6 * knack ? new AthlonInput(Tap: true) : AthlonInput.None;
    }

    public override int Points(int team) => 2 * smashers[team].Broken;
}

/// <summary>
/// Ring Drop (plan 15 · E6; a push on a platform): a minute on a round platform of radius <see cref="Radius"/>, one of
/// each team's three on it at a time. The arrows push a Pokémon about (harder by Speed), the button lunges (further by
/// Power, again sooner by Skill), and two that meet push each other apart as heavy things do, each as heavy as its
/// Power and Stamina make it. Whoever leaves the platform is out for a moment and the team's next comes on at its own
/// edge; the last team to touch it gets the knock-off if it was lately. Points are fifteen a knock-off, five off a fall.
/// </summary>
public sealed class RingDrop : AthlonEvent
{
    public const float Radius = 100f, Body = 12f, StartOut = 60f, Friction = 0.9f;
    public const int OutFrames = 45, CreditFrames = 90, LungeFrames = 8;
    public const int Minute = 60 * FramesPerSecond;

    public sealed class Pusher
    {
        public int Active;
        public Vector2 At, Velocity, Facing = new(0f, -1f);
        public int Lunge, Rest, Out = -1, Knocks, Falls;
        public int LastToucher = -1, TouchedAt = -9999;
        public bool On => Out < 0;
        /// <summary>A rival's way, kept for a few frames between thoughts.</summary>
        public Vector2 Aim;
        public int AimFor;
    }

    private readonly Pusher[] pushers = new Pusher[AthlonRun.TeamCount];

    public RingDrop(IReadOnlyList<AthlonTeam> teams, int seed) : base(AthlonEventKind.RingDrop, teams, seed)
    {
        for (int t = 0; t < pushers.Length; t++)
        {
            pushers[t] = new Pusher();
            Place(t);
        }
    }

    public override int Length => Minute;
    public override AthleteStat Leans => AthleteStat.Power;
    public IReadOnlyList<Pusher> Pushers => pushers;

    /// <summary>Where a team comes onto the platform: the player at the south, the rivals west, north and east.</summary>
    public static Vector2 StartOf(int team) => team switch
    {
        0 => new Vector2(0f, StartOut),
        1 => new Vector2(-StartOut, 0f),
        2 => new Vector2(0f, -StartOut),
        _ => new Vector2(StartOut, 0f)
    };

    public static float Push(AthleteStats s) => 0.12f + 0.05f * s.Speed;
    public static float LungeSpeed(AthleteStats s) => 1.5f + 0.5f * s.Power;
    public static int LungeRest(AthleteStats s) => 30 - 3 * s.Skill;
    public static float Mass(AthleteStats s) => 2f + s.Stamina + 0.5f * s.Power;

    public Athlete ActiveOf(int team) => Teams[team].Athletes[pushers[team].Active];

    private void Place(int team)
    {
        var p = pushers[team];
        p.At = StartOf(team);
        p.Velocity = Vector2.Zero;
        p.Facing = -Vector2.Normalize(p.At);
        p.Lunge = 0;
        p.Rest = 0;
        p.Out = -1;
        p.LastToucher = -1;
    }

    protected override void Play(AthlonInput[] inputs)
    {
        // Each moves by its own will
        for (int t = 0; t < pushers.Length; t++)
        {
            var p = pushers[t];
            if (!p.On)
            {
                if (--p.Out < 0)
                {
                    p.Active = (p.Active + 1) % AthlonRun.TeamSize;
                    Place(t);
                }
                continue;
            }
            var stats = ActiveOf(t).Stats;
            var way = new Vector2(Math.Sign(inputs[t].Dx), Math.Sign(inputs[t].Dy));
            if (way != Vector2.Zero)
            {
                way = Vector2.Normalize(way);
                p.Facing = way;
                p.Velocity += way * Push(stats);
            }
            if (p.Rest > 0) p.Rest--;
            if (inputs[t].Tap && p.Rest == 0)
            {
                p.Velocity += p.Facing * LungeSpeed(stats);
                p.Lunge = LungeFrames;
                p.Rest = LungeRest(stats);
            }
            if (p.Lunge > 0) p.Lunge--;
            p.Velocity *= Friction;
            p.At += p.Velocity;
        }

        // Two that meet push each other apart, a lunge harder
        for (int i = 0; i < pushers.Length; i++)
            for (int j = i + 1; j < pushers.Length; j++)
            {
                var a = pushers[i];
                var b = pushers[j];
                if (!a.On || !b.On) continue;
                var d = b.At - a.At;
                float dist = d.Length();
                if (dist >= 2f * Body) continue;
                var n = dist > 0.001f ? d / dist : new Vector2(1f, 0f);
                float ma = Mass(ActiveOf(i).Stats), mb = Mass(ActiveOf(j).Stats);
                float overlap = 2f * Body - dist;
                a.At -= n * overlap * mb / (ma + mb);
                b.At += n * overlap * ma / (ma + mb);
                float closing = Vector2.Dot(a.Velocity - b.Velocity, n);
                if (closing > 0f)
                {
                    float kick = (a.Lunge > 0 || b.Lunge > 0) ? 1.5f : 1f;
                    float impulse = 1.8f * closing / (1f / ma + 1f / mb) * kick;
                    a.Velocity -= n * impulse / ma;
                    b.Velocity += n * impulse / mb;
                }
                a.LastToucher = j;
                a.TouchedAt = Frames;
                b.LastToucher = i;
                b.TouchedAt = Frames;
            }

        // Whoever is off the platform falls
        for (int t = 0; t < pushers.Length; t++)
        {
            var p = pushers[t];
            if (!p.On || p.At.Length() <= Radius) continue;
            p.Falls++;
            if (p.LastToucher >= 0 && Frames - p.TouchedAt <= CreditFrames) pushers[p.LastToucher].Knocks++;
            p.Out = OutFrames;
            p.Velocity = Vector2.Zero;
        }
    }

    protected override AthlonInput Think(int team, float knack, Random rng)
    {
        var p = pushers[team];
        if (!p.On) return AthlonInput.None;
        // The nearest other on the platform is the one to push; near the edge, back to the middle first
        int target = -1;
        float nearest = float.MaxValue;
        for (int t = 0; t < pushers.Length; t++)
        {
            if (t == team || !pushers[t].On) continue;
            float d = Vector2.Distance(pushers[t].At, p.At);
            if (d < nearest) { nearest = d; target = t; }
        }
        if (--p.AimFor <= 0)
        {
            Vector2 want = target >= 0 ? pushers[target].At - p.At : -p.At;
            if (p.At.Length() > Radius * (0.55f + 0.2f * knack)) want = -p.At;
            float turn = Wobble(rng) * (1f - knack) * 1.2f;
            float c = MathF.Cos(turn), s = MathF.Sin(turn);
            p.Aim = new Vector2(want.X * c - want.Y * s, want.X * s + want.Y * c);
            p.AimFor = 6 + rng.Next(6);
        }
        int dx = MathF.Abs(p.Aim.X) > MathF.Abs(p.Aim.Y) * 0.4f ? Math.Sign(p.Aim.X) : 0;
        int dy = MathF.Abs(p.Aim.Y) > MathF.Abs(p.Aim.X) * 0.4f ? Math.Sign(p.Aim.Y) : 0;
        bool lunge = target >= 0 && nearest < 4f * Body && p.Rest == 0 && rng.NextDouble() < 0.15 + 0.35 * knack;
        return new AthlonInput(Tap: lunge, Held: false, Dx: dx, Dy: dy);
    }

    public override int Points(int team) => Math.Max(0, 15 * pushers[team].Knocks - 5 * pushers[team].Falls);
}
