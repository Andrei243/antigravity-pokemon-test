using System;
using System.Collections.Generic;
using PokemonPlatinumEngine.Battle;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// What a move does to a body itself as it plays (plan 25 Â· Y16; style guide, "Move effects (G8)", "Bodies that
/// move with a move"): a rush toward the foe, a leap, a rise, a spin, a squash, a squeeze, a collapse, and the way a
/// PokÃ©mon leaves the field for the turn of a two-turn move and comes back. Lengths are in the body's own height, so
/// they read alike on both platforms. GPU-free: <see cref="BattleRenderer"/> moves and scales the model by it.
/// </summary>
/// <param name="Toward">Heights toward the other side's platform (negative: away).</param>
/// <param name="Up">Heights up off the platform.</param>
/// <param name="Sink">Heights down into the platform, which hides what goes under.</param>
/// <param name="Squash">Pressed flat: the height times 1 âˆ’ this, the width times 1 + half of it.</param>
/// <param name="Narrow">Squeezed in from the sides: the width times 1 âˆ’ this, the height times 1 + half of it.</param>
/// <param name="Spin">Turned about its own axis, radians.</param>
/// <param name="Grow">Its size, a factor.</param>
internal readonly record struct BodyMotion(float Toward = 0f, float Up = 0f, float Sink = 0f, float Squash = 0f, float Narrow = 0f, float Spin = 0f, float Grow = 1f)
{
    // Named, not new(): a struct's new() is all noughts, a size of nothing
    public static readonly BodyMotion Still = new(Grow: 1f);

    /// <summary>Whether it changes the body at all.</summary>
    public bool Moves => this != Still;

    /// <summary>Two motions at once: lengths added, sizes multiplied.</summary>
    public static BodyMotion operator +(BodyMotion a, BodyMotion b) => new(
        a.Toward + b.Toward, a.Up + b.Up, a.Sink + b.Sink, a.Squash + b.Squash, a.Narrow + b.Narrow, a.Spin + b.Spin, a.Grow * b.Grow);

    /// <summary>The height's factor and the width's, from the squash and the squeeze.</summary>
    public (float Width, float Height) Shape =>
        (Math.Max(0.2f, (1f + Squash * 0.5f) * (1f - Narrow)), Math.Max(0.2f, (1f - Squash) * (1f + Narrow * 0.5f)));
}

/// <summary>How a PokÃ©mon leaves the field for the turn of a two-turn move, and comes back.</summary>
internal enum GoneWay { Up, Under, Dark }

/// <summary>Which moves move a body, and how (plan 25 Â· Y16).</summary>
internal static class BodyMotions
{
    /// <summary>What a move does to its user's body.</summary>
    internal enum UserMove { Rush, Leap, Spin, RushSpin, Rise, Lift, Puff, Spend }

    /// <summary>What a move does to its target's body (or every body, for Gravity).</summary>
    internal enum TargetMove { Squeeze, Press }

    /// <summary>The farthest a rush takes a body toward the foe, in its height.</summary>
    public const float RushReach = 0.8f;

    /// <summary>How long a body takes to leave the field for a two-turn move, and to come back.</summary>
    public const float LeaveTime = 0.5f, ReturnTime = 0.35f;

    /// <summary>How small a spent body has grown, and how far it has sunk, once it has collapsed.</summary>
    public const float SpentGrow = 0.6f, SpentSink = 0.2f;

    public static readonly IReadOnlyDictionary<string, UserMove> Users = new Dictionary<string, UserMove>
    {
        // Lunges the original draws with the sprite (plan 25's owed lists, Y8b to Y10)
        ["Take Down"] = UserMove.Rush, ["Double-Edge"] = UserMove.RushSpin, ["Giga Impact"] = UserMove.Rush, ["Flare Blitz"] = UserMove.Rush,
        ["Brave Bird"] = UserMove.Rush, ["Volt Tackle"] = UserMove.Rush, ["Head Smash"] = UserMove.Rush, ["Skull Bash"] = UserMove.Rush,
        ["Superpower"] = UserMove.Rush, ["Aqua Tail"] = UserMove.Rush, ["Reversal"] = UserMove.Rush, ["U-turn"] = UserMove.Rush,
        ["Gyro Ball"] = UserMove.RushSpin,
        ["Jump Kick"] = UserMove.Leap, ["High Jump Kick"] = UserMove.Leap, ["Rolling Kick"] = UserMove.Leap,
        ["Thrash"] = UserMove.Spin, ["Petal Dance"] = UserMove.Spin,
        ["Waterfall"] = UserMove.Rise,
        ["Magnet Rise"] = UserMove.Lift,
        ["Belly Drum"] = UserMove.Puff, ["Slack Off"] = UserMove.Puff, ["Bulk Up"] = UserMove.Puff, ["Power Trick"] = UserMove.Puff,
        // The user fainting away
        ["Memento"] = UserMove.Spend, ["Final Gambit"] = UserMove.Spend, ["Healing Wish"] = UserMove.Spend, ["Lunar Dance"] = UserMove.Spend,
    };

    public static readonly IReadOnlyDictionary<string, TargetMove> Targets = new Dictionary<string, TargetMove>
    {
        ["Wrap"] = TargetMove.Squeeze, ["Bind"] = TargetMove.Squeeze, ["Crush Grip"] = TargetMove.Squeeze,
        ["Gravity"] = TargetMove.Press,
    };

    /// <summary>Whether a move leaves its user collapsed until it faints (<see cref="Spent"/>).</summary>
    public static bool SpendsTheUser(string move) => Users.TryGetValue(move, out var kind) && kind == UserMove.Spend;

    /// <summary>How a PokÃ©mon leaves the field for a move's turn out of sight.</summary>
    public static GoneWay WayOf(string move) => move switch
    {
        "Dig" or "Dive" => GoneWay.Under,
        "Shadow Force" or "Phantom Force" or "Commander" => GoneWay.Dark,
        _ => GoneWay.Up // Fly, Bounce, Sky Drop
    };

    private static float Smooth(float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        return t * t * (3f - 2f * t);
    }

    /// <summary>Out to its height at the impact, held, and back: the shape of a rush.</summary>
    private static float Dash(float age) =>
        age < BattleAnimator.ImpactTime ? Smooth(age / BattleAnimator.ImpactTime)
        : 1f - Smooth((age - BattleAnimator.ImpactTime - 0.1f) / 0.45f);

    /// <summary>What a move does to its user's body at <paramref name="age"/> of a cue <paramref name="length"/> long.</summary>
    public static BodyMotion OfUser(string move, float age, float length)
    {
        if (!Users.TryGetValue(move, out var kind) || age < 0f || age >= length) return BodyMotion.Still;
        float end = Math.Clamp((length - age) / 0.3f, 0f, 1f);
        switch (kind)
        {
            case UserMove.Rush:
                return new BodyMotion(Toward: RushReach * Dash(age));
            case UserMove.RushSpin:
                return new BodyMotion(Toward: RushReach * Dash(age), Spin: MathF.Tau * Smooth(age / (BattleAnimator.ImpactTime + 0.4f)));
            case UserMove.Leap:
            {
                float d = Dash(age);
                // An arc over the way: highest half way out, down on the target at the impact
                float arc = age < BattleAnimator.ImpactTime ? MathF.Sin(age / BattleAnimator.ImpactTime * MathF.PI) : 0f;
                return new BodyMotion(Toward: RushReach * 0.9f * d, Up: 0.3f * arc);
            }
            case UserMove.Spin:
            {
                float turns = move == "Petal Dance" ? 3f : 2f;
                return new BodyMotion(Spin: MathF.Tau * turns * Smooth(age / (length - 0.3f)), Up: 0.08f * MathF.Abs(MathF.Sin(age * 9f)) * end);
            }
            case UserMove.Rise:
                return new BodyMotion(Toward: 0.4f * Dash(age), Up: 0.35f * Dash(age));
            case UserMove.Lift:
                return new BodyMotion(Up: 0.35f * Smooth(age / 0.5f) * end + 0.03f * MathF.Sin(age * 6f) * Smooth(age / 0.5f) * end);
            case UserMove.Puff:
            {
                // Pressed down and springing up, three times
                float s = MathF.Sin(Math.Clamp(age / (length - 0.3f), 0f, 1f) * MathF.PI * 3f);
                return new BodyMotion(Squash: 0.14f * s * end);
            }
            case UserMove.Spend:
                return Spent(age, fainted: false, length);
        }
        return BodyMotion.Still;
    }

    /// <summary>What a move does to its target's body (Gravity's to every body) at <paramref name="age"/>.</summary>
    public static BodyMotion OfTarget(string move, float age, float length)
    {
        if (!Targets.TryGetValue(move, out var kind) || age < BattleAnimator.ImpactTime || age >= length) return BodyMotion.Still;
        float t = age - BattleAnimator.ImpactTime;
        float end = Math.Clamp((length - age) / 0.3f, 0f, 1f);
        return kind switch
        {
            // Squeezed three times, the grip tightening
            TargetMove.Squeeze => new BodyMotion(Narrow: 0.16f * MathF.Abs(MathF.Sin(t * MathF.PI * 3.2f)) * end),
            // Pressed flat as the weight comes down, and holding there a moment
            _ => new BodyMotion(Squash: 0.14f * Smooth(t / 0.15f) * end)
        };
    }

    /// <summary>
    /// A body collapsing as its move spends it: shrinking and sinking over the cue, held while it lies fainted, and
    /// back over <c>0.3</c> s if it stands after all (a Final Gambit that missed).
    /// </summary>
    public static BodyMotion Spent(float age, bool fainted, float length = BattleAnimator.EffectTime)
    {
        if (age < 0f) return BodyMotion.Still;
        float k = Smooth((age - 0.2f) / (length - 0.35f));
        if (age >= length && !fainted) k *= 1f - Math.Clamp((age - length) / 0.3f, 0f, 1f);
        return new BodyMotion(Sink: SpentSink * k, Grow: 1f - (1f - SpentGrow) * k);
    }

    /// <summary>A body leaving for a move's turn out of sight, <paramref name="p"/> 0..1 of the way gone.</summary>
    public static BodyMotion Leaving(GoneWay way, float p)
    {
        p = Math.Clamp(p, 0f, 1f);
        return way switch
        {
            // Up and out of the picture, quicker as it goes
            GoneWay.Up => new BodyMotion(Up: 4f * p * p, Squash: -0.15f * Smooth(p / 0.3f)),
            // Into the ground (the platform hides it as it goes)
            GoneWay.Under => new BodyMotion(Sink: 1.1f * Smooth(p)),
            // Shrinking into nothing
            _ => new BodyMotion(Grow: 1f - Smooth(p))
        };
    }

    /// <summary>A body coming back from out of sight, <paramref name="p"/> 0..1 of the way back.</summary>
    public static BodyMotion Returning(GoneWay way, float p)
    {
        p = Math.Clamp(p, 0f, 1f);
        float left = 1f - p;
        return way switch
        {
            GoneWay.Up => new BodyMotion(Up: 4f * left * left),
            GoneWay.Under => new BodyMotion(Sink: 1.1f * (1f - Smooth(p))),
            _ => new BodyMotion(Grow: Smooth(p))
        };
    }

    /// <summary>
    /// Everything moving the body in a place now: the moves playing from it and onto it, Gravity on every body, its
    /// going and coming back for a move's turn out of sight, and its collapse.
    /// </summary>
    public static BodyMotion Of(BattleAnimator anim, BattleSide side, int slot)
    {
        var v = anim[side, slot];
        var motion = BodyMotion.Still;
        foreach (var cue in anim.Cues)
        {
            // A Dynamax's end shudders before it falls in (plan 25 · YD3)
            if (cue.Kind == CueKind.DynamaxEnd && !cue.Fell && cue.ToSide == side && cue.ToSlot == slot) motion += Directions.Shudder(cue.Age);
            if (cue.Kind != CueKind.Move || cue.Charging || cue.Arriving) continue;
            // Read at the shown age, which a heavy signature move holds at its impact (plan 25 · Y17)
            float age = cue.ShownAge;
            if (cue.FromSide == side && cue.FromSlot == slot)
            {
                if (!SpendsTheUser(cue.Move)) motion += OfUser(cue.Move, age, cue.Length);
                motion += Directions.UserBody(cue);
            }
            if (cue.ToSide == side && cue.ToSlot == slot) motion += Directions.TargetBody(cue);
            if (!cue.Landed || !Targets.TryGetValue(cue.Move, out var kind)) continue;
            if (kind == TargetMove.Press || (cue.ToSide == side && cue.ToSlot == slot && !cue.OnSelf))
                motion += OfTarget(cue.Move, age, cue.Length);
        }
        if (v.SpentAge >= 0f) motion += Spent(v.SpentAge, v.Shown is { CurrentHP: <= 0 });
        // A giant that faints sinks into its clouds before it goes down (plan 25 · YD3)
        if (v.FallAge >= 0f) motion += new BodyMotion(Sink: BattleAnimator.GiantFallSink * Smooth(v.FallAge / BattleAnimator.GiantFallTime));
        if (v.Away && v.AwayAge >= 0f) motion += Leaving(WayOf(v.AwayMove), v.AwayAge / LeaveTime);
        if (!v.Away && v.BackAge >= 0f) motion += Returning(WayOf(v.AwayMove), v.BackAge / ReturnTime);
        return motion;
    }
}
