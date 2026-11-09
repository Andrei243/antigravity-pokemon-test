using System;
using System.Numerics;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// What drives a grown body's clips from how it moves (plan 30 · FM5; style guide, "Grown animation"): handed its
/// velocity over the ground every frame (analog, any speed and any way), it keeps the body's yaw, the gait's cycle
/// advanced by the distance walked over the stride (so the feet never slide), the walk's and the run's blends by speed,
/// the lean into a turn by the sideways acceleration, the step-turn from standing and the start and the stop, and fills a
/// <see cref="CharacterPose"/> with them (<see cref="Fill"/>). Speeds are in model units a second (a rig's units before its
/// scale), angles in radians with 0 facing +Z and a positive yaw turning to the body's left (+X). GPU-free: tools and
/// tests drive it as the field will.
/// </summary>
internal sealed class GrownMotion
{
    /// <summary>Walking pace, in legs (hip joint to ankle) a second.</summary>
    public const float WalkPacePerLeg = 1.75f;

    /// <summary>The run comes in between these shares of walking pace (plan 30: 1.2 to 1.8 times walking).</summary>
    public const float RunFrom = 1.2f, RunTo = 1.8f;

    /// <summary>How quickly the yaw eases toward the way the body goes, a second: standing or walking, and running.</summary>
    public const float SettleStanding = 18f, SettleRunning = 7f;

    /// <summary>From standing, a turn wider than this is a step-turn (plan 30: 120°).</summary>
    public static readonly float StepTurnFrom = 120f * MathF.PI / 180f;

    /// <summary>A step-turn's length, a start's and a stop's: each under a fifth of a second but the turn.</summary>
    public const float StepTurnSeconds = 0.25f, StartSeconds = 0.18f, StopSeconds = 0.18f;

    /// <summary>The most a body leans into a turn (plan 30: 8°).</summary>
    public static readonly float MaxLean = 8f * MathF.PI / 180f;

    /// <summary>Gravity in model units a second squared (a grown adult's model unit is about 1.07 m).</summary>
    public const float Gravity = 9.2f;

    /// <summary>Under this share of walking pace the body stands; the walk blends in fully by <see cref="WalkFullAt"/>.</summary>
    public const float StandBelow = 0.05f, WalkFullAt = 0.12f;

    public readonly float Leg;

    public float WalkPace => WalkPacePerLeg * Leg;

    public float Yaw, Cycle, Speed, WalkBlend, RunBlend, Lean, Stride;

    private float startAge = -1f, stopAge = -1f, turnAge = -1f, turnFrom, turnAngle;
    private Vector2 lastVelocity;
    private bool moving;

    public GrownMotion(float leg, float yaw = 0f)
    {
        Leg = leg;
        Yaw = yaw;
        Stride = StrideAt(0f);
    }

    /// <summary>A driver for a rig of the grown kit.</summary>
    public static GrownMotion For(CharacterRig rig, float yaw = 0f) => new(GrownAnimation.LegsOf(rig.Skeleton).Length, yaw);

    /// <summary>Whether a step-turn is under way.</summary>
    public bool Turning => turnAge >= 0f;

    /// <summary>
    /// The stride (two steps) at a speed: 0.75 of the leg standing to 1.4 at walking pace, then a leg more for each
    /// walking pace more, up to 2.4 at twice it.
    /// </summary>
    public float StrideAt(float speed)
    {
        float share = WalkPace > 0f ? speed / WalkPace : 0f;
        float legs = share <= 1f ? 0.75f + 0.65f * share : MathF.Min(2.4f, 1.4f + (share - 1f));
        return Leg * legs;
    }

    /// <summary>
    /// One frame of <paramref name="dt"/> seconds moving at <paramref name="velocity"/> over the ground (x and z of the
    /// model's space): standing still, <paramref name="face"/> is a way to turn to, if any.
    /// </summary>
    public void Step(float dt, Vector2 velocity, float? face = null)
    {
        if (dt <= 0f) return;
        float speed = velocity.Length();
        var accel = (velocity - lastVelocity) / dt;
        lastVelocity = velocity;
        Speed = speed;

        bool nowMoving = speed > StandBelow * WalkPace;
        if (nowMoving && !moving) { startAge = 0f; stopAge = -1f; turnAge = -1f; }
        else if (!nowMoving && moving && WalkBlend > 0.5f) { stopAge = 0f; startAge = -1f; }
        moving = nowMoving;
        if (startAge >= 0f && (startAge += dt) >= StartSeconds) startAge = -1f;
        if (stopAge >= 0f && (stopAge += dt) >= StopSeconds) stopAge = -1f;

        // The blends ease after the speed; the stride follows it, and the cycle goes on by the distance over the stride
        float walkWanted = nowMoving ? Smooth(0f, WalkFullAt * WalkPace, speed) : 0f;
        WalkBlend += (walkWanted - WalkBlend) * (1f - MathF.Exp(-14f * dt));
        float runWanted = Smooth(RunFrom * WalkPace, RunTo * WalkPace, speed);
        RunBlend += (runWanted - RunBlend) * (1f - MathF.Exp(-8f * dt));
        if (nowMoving)
        {
            Stride = StrideAt(speed);
            Cycle += speed * dt / Stride;
            Cycle -= MathF.Floor(Cycle);
        }

        // The yaw: eased toward the way the body goes; from standing, a turn wider than 120° is a step-turn
        float? want = nowMoving ? MathF.Atan2(velocity.X, velocity.Y) : face;
        if (turnAge >= 0f)
        {
            turnAge += dt;
            float u = MathF.Min(1f, turnAge / StepTurnSeconds);
            Yaw = Wrap(turnFrom + turnAngle * Smooth(0f, 1f, u));
            if (u >= 1f) turnAge = -1f;
        }
        else if (want is { } heading)
        {
            float diff = Wrap(heading - Yaw);
            if (!nowMoving && MathF.Abs(diff) > StepTurnFrom)
            {
                turnAge = 0f;
                turnFrom = Yaw;
                turnAngle = diff;
            }
            else
            {
                float rate = SettleStanding + (SettleRunning - SettleStanding) * RunBlend;
                Yaw = Wrap(Yaw + diff * (1f - MathF.Exp(-rate * dt)));
            }
        }

        // The lean: into the turn by the sideways acceleration against gravity, at most 8°
        var left = new Vector2(MathF.Cos(Yaw), -MathF.Sin(Yaw));
        float leanWanted = nowMoving ? Math.Clamp(MathF.Atan(Vector2.Dot(accel, left) / Gravity), -MaxLean, MaxLean) : 0f;
        Lean = Math.Clamp(Lean + (leanWanted - Lean) * (1f - MathF.Exp(-10f * dt)), -MaxLean, MaxLean);
    }

    /// <summary>Hands the clips what this frame's motion is.</summary>
    public void Fill(ref CharacterPose pose)
    {
        pose.Walk = Cycle;
        pose.WalkBlend = WalkBlend;
        pose.RunBlend = RunBlend;
        pose.Running = RunBlend >= 0.5f;
        pose.Stride = Stride;
        pose.Lean = Lean;
        pose.StepTurn = turnAge >= 0f ? Math.Clamp(turnAge / StepTurnSeconds, 1e-4f, 0.9999f) : 0f;
        pose.StepTurnAngle = turnAge >= 0f ? turnAngle : 0f;
        pose.Start = startAge >= 0f ? Math.Clamp(startAge / StartSeconds, 1e-4f, 0.9999f) : 0f;
        pose.Stop = stopAge >= 0f ? Math.Clamp(stopAge / StopSeconds, 1e-4f, 0.9999f) : 0f;
    }

    /// <summary>An angle brought into -π .. π.</summary>
    public static float Wrap(float a)
    {
        a %= MathF.Tau;
        if (a > MathF.PI) a -= MathF.Tau;
        else if (a < -MathF.PI) a += MathF.Tau;
        return a;
    }

    private static float Smooth(float a, float b, float x)
    {
        float t = Math.Clamp((x - a) / (b - a), 0f, 1f);
        return t * t * (3f - 2f * t);
    }
}
