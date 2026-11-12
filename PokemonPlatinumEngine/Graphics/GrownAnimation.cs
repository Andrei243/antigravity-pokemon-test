using System;
using System.Numerics;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// The grown kit's clips (plan 30 · FM5; style guide, "Grown animation"): standing, the walk and the run as one gait
/// blended by speed, the step-turn, starts and stops, a lean into a turn, the hop, talking, the stances, the rides and
/// the emotes, for the 26-bone grown skeleton. Wherever a grown body stands its legs are solved to where its feet
/// should be (<see cref="SolveLegs"/>), so a dip, a lean or a bow never moves a foot, and the gait's feet move back
/// under the body at the body's own speed, so a foot never slides. The chibi kit's clips are
/// <see cref="CharacterAnimation.ApplyChibi"/> and are not touched by any of this. Angles as in
/// <see cref="CharacterAnimation"/>: radians about the model's axes, the character facing +Z with its left at +X; a
/// positive pitch swings a hanging limb back and tips the face down; a positive roll lifts the left arm out. No GPU calls.
/// </summary>
internal static class GrownAnimation
{
    private const float Tau = MathF.PI * 2f;

    /// <summary>How much of the cycle a foot is on the ground walking, and running.</summary>
    public const float WalkStance = 0.58f, RunStance = 0.38f;

    /// <summary>A seat's top, as a share of the hip joints' height (style guide, "Grown animation", "Stances").</summary>
    public const float SeatShare = 0.45f;

    /// <summary>A grown body's legs, read off its skeleton: hip joint to ankle, the thigh's and the shin's lengths, the hip joints' height and the body's size beside a grown adult's.</summary>
    internal readonly record struct Legs(float Length, float Thigh, float Shin, float HipY, float Size);

    /// <summary>The legs of a skeleton of the grown kit (the left one; the right mirrors it).</summary>
    public static Legs LegsOf(Skeleton sk)
    {
        var j = sk[HumanBones.ThighL].Joint;
        var k = sk[HumanBones.ShinL].Joint;
        var a = sk[HumanBones.FootL].Joint;
        return new Legs(j.Y - a.Y, Vector3.Distance(j, k), Vector3.Distance(k, a), j.Y, j.Y / 0.82f);
    }

    /// <summary>The gait's own stride (two steps) when nobody hands one: 1.4 of the leg walking, 2.4 running.</summary>
    public static float StrideOf(float leg, float runBlend) => leg * (1.4f + 1.0f * Math.Clamp(runBlend, 0f, 1f));

    /// <summary>Where a grown sitter's seat is, in model units before the rig's scale.</summary>
    public static float SeatOf(Skeleton sk) => SeatShare * LegsOf(sk).HipY;

    [ThreadStatic] private static SkeletonPose? gaitScratch, emoteScratch;

    /// <summary>Where each foot's ankle should be in model space, and how far each foot is pitched against the ground (positive lifts the heel).</summary>
    private struct Feet
    {
        public Vector3 AnkleL, AnkleR;
        public float PitchL, PitchR;

        public static Feet Lerp(Feet a, Feet b, float t) => new()
        {
            AnkleL = Vector3.Lerp(a.AnkleL, b.AnkleL, t), AnkleR = Vector3.Lerp(a.AnkleR, b.AnkleR, t),
            PitchL = a.PitchL + (b.PitchL - a.PitchL) * t, PitchR = a.PitchR + (b.PitchR - a.PitchR) * t
        };
    }

    public static void Apply(CharacterRig rig, CharacterPose p, SkeletonPose pose)
    {
        var sk = rig.Skeleton;
        var legs = LegsOf(sk);
        pose.Reset();
        Standing(p.Time, legs, pose);
        var rest = RestFeet(sk);

        if (p.Riding != Riding.None)
        {
            Ride(sk, p, legs, pose);
            return;
        }
        if (p.Stance != Overworld.Stance.Stand)
        {
            StanceClip(sk, p, legs, pose);
            return;
        }

        var feet = rest;
        float walk = Math.Clamp(p.WalkBlend, 0f, 1f);
        if (walk > 0f)
        {
            var gait = Scratch(ref gaitScratch, pose.Count);
            gait.Reset();
            Standing(p.Time, legs, gait);
            var stepping = p.Skating ? Skate(sk, p, legs, gait) : Gait(sk, p, legs, gait);
            pose.BlendToward(gait, walk);
            feet = Feet.Lerp(rest, stepping, walk);
        }
        // A glide (skates let go of, or ice) with no step under way: the skater's stance alone
        if (p.Gliding && !(p.Skating && walk > 0f)) Glide(legs, pose, 1f - walk);

        // What moves the body over its feet before they are solved: a start, a stop, a lean, a step-turn's dip
        if (p.Start > 0f && p.Start < 1f)
        {
            float s = MathF.Sin(p.Start * MathF.PI);
            pose.Turn(HumanBones.Spine, 0.08f * s);
            pose.Offset[HumanBones.Root] += new Vector3(0, -0.008f * legs.Size * s, 0.012f * legs.Size * s);
        }
        if (p.Stop > 0f && p.Stop < 1f)
        {
            float s = MathF.Sin(p.Stop * MathF.PI);
            pose.Turn(HumanBones.Spine, -0.06f * s);
            pose.Offset[HumanBones.Root] += new Vector3(0, -0.012f * legs.Size * s, -0.006f * legs.Size * s);
            pose.Turn(HumanBones.UpperArmL, -0.18f * s);
            pose.Turn(HumanBones.UpperArmR, -0.18f * s);
        }
        if (p.Lean != 0f)
        {
            float lean = Math.Clamp(p.Lean, -GrownMotion.MaxLean, GrownMotion.MaxLean);
            pose.Turn(HumanBones.Root, 0f, 0f, -lean);
            pose.Turn(HumanBones.Head, 0f, 0.4f * lean);
        }
        float turn = p.StepTurn > 0f && p.StepTurn < 1f ? p.StepTurn : 0f;
        if (turn > 0f)
        {
            float remaining = p.StepTurnAngle * (1f - Smooth(0f, 1f, turn));
            pose.Turn(HumanBones.Head, 0f, Math.Clamp(0.45f * remaining, -0.6f, 0.6f));
            pose.Turn(HumanBones.Chest, 0f, Math.Clamp(0.25f * remaining, -0.35f, 0.35f));
            pose.Offset[HumanBones.Root] += new Vector3(0, -0.012f * legs.Size * MathF.Sin(turn * MathF.PI), 0);
        }

        SolveLegs(sk, pose, feet);

        // What lifts a foot off the ground after: a step-turn's two steps, a hop
        if (turn > 0f)
        {
            // The foot on the side turned toward first, then the other
            bool leftFirst = p.StepTurnAngle > 0f;
            float first = turn < 0.5f ? MathF.Sin(turn / 0.5f * MathF.PI) : 0f;
            float second = turn >= 0.5f ? MathF.Sin((turn - 0.5f) / 0.5f * MathF.PI) : 0f;
            LiftLeg(pose, leftFirst ? first : second, true);
            LiftLeg(pose, leftFirst ? second : first, false);
        }
        if (p.Hop > 0f && p.Hop < 1f) Hop(p.Hop, pose);
        if (p.Talking) Talk(p.Time, pose);

        float duration = CharacterAnimation.Duration(p.Emote);
        if (p.Emote != Emote.None && p.EmoteTime < duration)
        {
            var extra = Scratch(ref emoteScratch, pose.Count);
            extra.Reset();
            Standing(p.Time, legs, extra);
            EmoteClip(p.Emote, p.EmoteTime, legs, extra);
            SolveLegs(sk, extra, rest);
            float t = p.EmoteTime;
            float envelope = Smooth(0f, 0.18f, t) * (1f - Smooth(duration - 0.22f, duration, t));
            pose.BlendToward(extra, envelope);
        }
    }

    private static SkeletonPose Scratch(ref SkeletonPose? scratch, int count) =>
        scratch is { } s && s.Count == count ? s : scratch = new SkeletonPose(count);

    private static float Smooth(float a, float b, float x)
    {
        float t = Math.Clamp((x - a) / (b - a), 0f, 1f);
        return t * t * (3f - 2f * t);
    }

    private static Quaternion Pitch(float angle) => Quaternion.CreateFromAxisAngle(Vector3.UnitX, angle);

    // ------------------------------------------------------------------ standing

    /// <summary>
    /// Standing: breathing in the chest, a slow sway of the weight from foot to foot, the arms down 15° from the A-pose
    /// with the elbows a little bent, the head looking ahead. The legs are left for <see cref="SolveLegs"/>.
    /// </summary>
    private static void Standing(float t, Legs legs, SkeletonPose pose)
    {
        float breath = MathF.Sin(t * Tau / 3.6f);
        pose.Offset[HumanBones.Root] += new Vector3(0.004f * legs.Size * MathF.Sin(t * 0.45f), 0, 0);
        pose.Turn(HumanBones.Spine, -0.012f - 0.006f * breath);
        pose.Offset[HumanBones.Chest] += new Vector3(0, 0.0015f * legs.Size * breath, 0);
        pose.Turn(HumanBones.Chest, -0.01f * breath);
        pose.Turn(HumanBones.Neck, -0.03f);
        pose.Turn(HumanBones.Head, -0.05f + 0.015f * MathF.Sin(t * 2.3f + 0.6f), 0.035f * MathF.Sin(t * 0.7f));
        pose.Turn(HumanBones.ClavicleL, 0f, 0f, -0.02f + 0.008f * breath);
        pose.Turn(HumanBones.ClavicleR, 0f, 0f, 0.02f - 0.008f * breath);
        pose.Turn(HumanBones.UpperArmL, 0.04f, 0f, -0.26f + 0.01f * breath);
        pose.Turn(HumanBones.UpperArmR, 0.04f, 0f, 0.26f - 0.01f * breath);
        pose.Turn(HumanBones.ForeArmL, -0.16f, 0f, -0.04f);
        pose.Turn(HumanBones.ForeArmR, -0.16f, 0f, 0.04f);
        pose.Turn(HumanBones.HandL, 0f, 0f, -0.08f);
        pose.Turn(HumanBones.HandR, 0f, 0f, 0.08f);
        pose.Turn(HumanBones.Hair, 0.02f * breath);
        pose.Turn(HumanBones.Bag, -0.02f * breath);
    }

    /// <summary>The feet where they were sculpted, flat on the ground.</summary>
    private static Feet RestFeet(Skeleton sk) => new()
    {
        AnkleL = sk[HumanBones.FootL].Joint, AnkleR = sk[HumanBones.FootR].Joint
    };

    // ------------------------------------------------------------------ the gait

    /// <summary>
    /// The walk and the run as one gait (style guide, "Grown animation"): each foot's ball moves straight back under
    /// the body while it is down and comes forward on a smooth step while it is up, the heel lifting about the ball
    /// before it leaves; the root drops where the stance leg needs it to reach; arms, chest, hips, the coat's tails, a
    /// skirt, the hair and the bag follow. Returns where the feet go.
    /// </summary>
    private static Feet Gait(Skeleton sk, CharacterPose p, Legs legs, SkeletonPose pose)
    {
        float run = p.RunBlend > 0f ? Math.Clamp(p.RunBlend, 0f, 1f) : p.Running ? 1f : 0f;
        float L = legs.Length;
        float stride = p.Stride > 0f ? p.Stride : StrideOf(L, run);
        float share = WalkStance + (RunStance - WalkStance) * run;
        float half = share * stride / 2f;
        float cycle = p.Walk - MathF.Floor(p.Walk);

        var (ankleL, pitchL, forwardL) = Foot(sk, HumanBones.FootL, HumanBones.ToeL, cycle, share, half, stride, run, L);
        float other = cycle + 0.5f;
        var (ankleR, pitchR, forwardR) = Foot(sk, HumanBones.FootR, HumanBones.ToeR, other - MathF.Floor(other), share, half, stride, run, L);

        // The root: running it rises between steps; it drops wherever a leg wouldn't otherwise reach its foot
        // (a leg a hundredth and a half short of straight is a knee bent 20°: walking it is half a hundredth, about 11°)
        float reach = (0.995f + (0.95f - 0.995f) * run) * (legs.Thigh + legs.Shin);
        float flight = (share + 0.5f) / 2f;
        float bob = run * L * (0.03f * MathF.Cos(2f * Tau * (cycle - flight)) - 0.045f);
        // A foot in the air holds the body down only as it comes to land (so the root meets the stance's without a jump);
        // as it lifts behind it is let go, and the leg reaches as far as it can
        float Holds(float phase, Vector3 hip, Vector3 ankle)
        {
            float needed = Needed(hip, ankle, reach);
            if (phase < share) return needed;
            float landing = Smooth(0.7f, 1f, (phase - share) / (1f - share));
            return needed + (1f - landing) * 0.25f * L;
        }
        float otherPhase = other - MathF.Floor(other);
        float root = MathF.Min(bob, MathF.Min(Holds(cycle, sk[HumanBones.ThighL].Joint, ankleL), Holds(otherPhase, sk[HumanBones.ThighR].Joint, ankleR)));
        pose.Offset[HumanBones.Root] += new Vector3(0, root, 0);

        float bounce = MathF.Sin(2f * Tau * cycle - 1.1f);
        float spread = MathF.Abs(forwardL - forwardR) / 2f;

        // The hips turn after the leg forward, the chest against them, the head looks ahead
        pose.Turn(HumanBones.Hips, 0.04f * run, -0.07f * forwardL);
        pose.Turn(HumanBones.Spine, 0.04f + 0.16f * run);
        pose.Turn(HumanBones.Chest, 0.02f * run, 0.09f * forwardL);
        pose.Turn(HumanBones.Neck, -0.02f - 0.06f * run);
        pose.Turn(HumanBones.Head, -0.03f - 0.1f * run + 0.02f * MathF.Cos(2f * Tau * cycle), -0.03f * forwardL);

        // The arms against the legs, the elbows bending as the run comes in, the forward arm bent more
        float swing = 0.28f + 0.27f * run, elbow = 0.2f + 1.15f * run;
        pose.Turn(HumanBones.UpperArmL, swing * forwardL, 0f, 0.06f * run);
        pose.Turn(HumanBones.UpperArmR, swing * forwardR, 0f, -0.06f * run);
        pose.Turn(HumanBones.ForeArmL, -elbow - 0.18f * MathF.Max(0f, -forwardL));
        pose.Turn(HumanBones.ForeArmR, -elbow - 0.18f * MathF.Max(0f, -forwardR));
        pose.Offset[HumanBones.ClavicleL] += new Vector3(0, -0.004f * legs.Size * forwardL, 0);
        pose.Offset[HumanBones.ClavicleR] += new Vector3(0, -0.004f * legs.Size * forwardR, 0);

        // What follows through: the coat's tails back and apart, a skirt's flare, the hair and the bag a beat behind
        pose.Turn(HumanBones.CoatTail, 0.06f + 0.12f * spread + 0.22f * run);
        pose.Scale[HumanBones.CoatTail] = new Vector3(1f + 0.16f * spread, 1f, 1f + 0.08f * spread);
        // A running knee lifts the skirt's front with it (plan 30 · FM10): as the foot in the air rises, the skirt tips
        // forward and flares, so the thigh coming up under it stays under it (the skirt's share of each thigh, sculpted, does
        // the rest)
        float footUp = MathF.Max(ankleL.Y, ankleR.Y) - sk[HumanBones.FootL].Joint.Y;
        float kneeUp = run * Math.Clamp(footUp / (0.3f * L), 0f, 1f);
        pose.Turn(HumanBones.Skirt, 0.03f + 0.05f * run - 0.45f * kneeUp, 0f, 0.04f * MathF.Sin(Tau * cycle - 0.7f));
        // A skirt flares fore and aft with the stride, a run's the more, so a leg forward stays under it
        pose.Scale[HumanBones.Skirt] = new Vector3(1f + (0.12f + 0.04f * run) * spread + 0.08f * kneeUp, 1f, 1f + (0.42f + 0.2f * run) * spread + 0.4f * kneeUp);
        pose.Turn(HumanBones.Hair, 0.04f + 0.12f * run + (0.05f + 0.04f * run) * bounce);
        pose.Turn(HumanBones.Bag, (0.05f + 0.06f * run) * bounce + 0.03f * run);
        pose.Offset[HumanBones.Bag] += new Vector3(0, (0.003f + 0.006f * run) * legs.Size * bounce, 0);

        return new Feet { AnkleL = ankleL, AnkleR = ankleR, PitchL = pitchL, PitchR = pitchR };
    }

    // ------------------------------------------------------------------ skating

    /// <summary>
    /// The skater's stride (style guide, "Grown animation", "Skating"): one push for each two of the gait's cycles, each
    /// foot in turn driven out to its side and a little back on its wheels for nearly half its cycle, lifted and brought
    /// back under the body, then rolling under it while the other pushes; the body crouched and leaning into the way it
    /// goes, the hips turned toward the pushing foot, the arms swinging across the body against it. Returns where the feet go.
    /// </summary>
    private static Feet Skate(Skeleton sk, CharacterPose p, Legs legs, SkeletonPose pose)
    {
        float L = legs.Length;
        float cycle = p.Walk * 0.5f;
        cycle -= MathF.Floor(cycle);
        var rest = RestFeet(sk);
        (Vector3 Ankle, float Out) Push(Vector3 ankle, float phase, float side)
        {
            // Out and back on the ground, then lifted home, then rolling under the body
            float outward = phase < SkatePush ? Smooth(0f, SkatePush, phase) : phase < SkateReturn ? 1f - Smooth(SkatePush, SkateReturn, phase) : 0f;
            float lift = phase >= SkatePush && phase < SkateReturn ? MathF.Sin((phase - SkatePush) / (SkateReturn - SkatePush) * MathF.PI) : 0f;
            return (ankle + new Vector3(side * 0.26f * L * outward, 0.1f * L * lift, -0.16f * L * outward), outward);
        }
        var (ankleL, outL) = Push(rest.AnkleL, cycle, 1f);
        float other = cycle + 0.5f;
        var (ankleR, outR) = Push(rest.AnkleR, other - MathF.Floor(other), -1f);

        // Crouched over the skates, leaning into the way, the head up to look ahead
        float sway = outL - outR;
        pose.Offset[HumanBones.Root] += new Vector3(-0.05f * L * sway, -SkateCrouch * L, 0.02f * L);
        pose.Turn(HumanBones.Hips, 0.06f, 0.12f * sway);
        pose.Turn(HumanBones.Spine, 0.24f);
        pose.Turn(HumanBones.Chest, 0.06f, -0.1f * sway);
        pose.Turn(HumanBones.Neck, -0.1f);
        pose.Turn(HumanBones.Head, -0.2f);
        // The arms swing across the body, against the pushing leg; the elbows a little bent
        pose.Turn(HumanBones.UpperArmL, -0.45f * sway, 0f, -0.1f + 0.15f * MathF.Max(0f, sway));
        pose.Turn(HumanBones.UpperArmR, 0.45f * sway, 0f, 0.1f - 0.15f * MathF.Max(0f, -sway));
        pose.Turn(HumanBones.ForeArmL, -0.35f);
        pose.Turn(HumanBones.ForeArmR, -0.35f);
        // What follows through: the coat and the hair streaming back, the skirt and the bag with the sway
        pose.Turn(HumanBones.CoatTail, 0.3f);
        pose.Turn(HumanBones.Skirt, 0.05f, 0f, 0.05f * sway);
        pose.Turn(HumanBones.Hair, 0.2f);
        pose.Turn(HumanBones.Bag, 0.08f, 0f, 0.06f * sway);
        return new Feet { AnkleL = ankleL, AnkleR = ankleR };
    }

    /// <summary>How much of a foot's skating cycle it pushes for on the ground, and by when it is home under the body.</summary>
    public const float SkatePush = 0.42f, SkateReturn = 0.58f;

    /// <summary>How far the skater's hips drop, as a share of the leg.</summary>
    public const float SkateCrouch = 0.07f;

    /// <summary>The glide (skates let go of, or ice): the knees a little bent, leaning forward, the arms out for balance, by <paramref name="share"/>.</summary>
    private static void Glide(Legs legs, SkeletonPose pose, float share)
    {
        if (share <= 0f) return;
        pose.Offset[HumanBones.Root] += new Vector3(0, -0.05f * legs.Length * share, 0);
        pose.Turn(HumanBones.Spine, 0.14f * share);
        pose.Turn(HumanBones.Head, -0.12f * share);
        pose.Turn(HumanBones.UpperArmL, 0f, 0f, 0.35f * share);
        pose.Turn(HumanBones.UpperArmR, 0f, 0f, -0.35f * share);
        pose.Turn(HumanBones.Hair, 0.1f * share);
    }

    /// <summary>
    /// One foot of the gait <paramref name="phase"/> of the way through its cycle (it lands at 0): where its ankle goes,
    /// how far it is pitched, and how far forward of its middle it is (1 at its front, -1 at its back).
    /// </summary>
    private static (Vector3 Ankle, float Pitch, float Forward) Foot(Skeleton sk, int foot, int toe, float phase, float share, float half, float stride, float run, float L)
    {
        var ankleBind = sk[foot].Joint;
        var ballBind = sk[toe].Joint;
        float middle = ballBind.Z - 0.04f * L;
        float z, lift, pitch;
        if (phase < share)
        {
            // Down: the ball goes straight back as the body goes on; the heel lifts over the last third
            float u = phase / share;
            z = middle + half * (1f - 2f * u);
            lift = 0f;
            pitch = (0.55f + 0.15f * run) * Smooth(0.62f - 0.2f * run, 1f, u);
        }
        else
        {
            // Up: forward on a curve that leaves and meets the ground at the ground's own speed under the body (so the foot
            // is still in the world as it lifts and as it lands), lifted at the middle, the toes up a little before it lands flat
            float w = (phase - share) / (1f - share);
            float still = -(1f - share) * stride;
            float w2 = w * w, w3 = w2 * w;
            z = middle - half * (2f * w3 - 3f * w2 + 1f) + still * (w3 - 2f * w2 + w) + half * (3f * w2 - 2f * w3) + still * (w3 - w2);
            lift = (0.10f + 0.16f * run) * L * MathF.Sin(w * MathF.PI);
            pitch = (0.55f + 0.15f * run) * (1f - Smooth(0f, 0.45f, w)) - 0.22f * MathF.Sin(MathF.PI * Smooth(0.45f, 1f, w));
        }
        var ball = new Vector3(ballBind.X * 0.92f, ballBind.Y + lift, z);
        // The ankle turned about the ball, which stays where it is (the heel lifts about it)
        var fromBall = new Vector3((ankleBind.X - ballBind.X) * 0.92f, ankleBind.Y - ballBind.Y, ankleBind.Z - ballBind.Z);
        var ankle = ball + Vector3.Transform(fromBall, Pitch(pitch));
        float forward = half > 0f ? (z - middle) / half : 0f;
        return (ankle, pitch, forward);
    }

    /// <summary>How far the root must drop for a leg whose hip is at <paramref name="hip"/> to reach <paramref name="ankle"/> with no knee straighter than <paramref name="reach"/> allows (0 or less).</summary>
    private static float Needed(Vector3 hip, Vector3 ankle, float reach)
    {
        float dx = ankle.X - hip.X, dz = ankle.Z - hip.Z;
        float across = dx * dx + dz * dz;
        if (across >= reach * reach) return -(hip.Y - ankle.Y);
        return MathF.Min(0f, MathF.Sqrt(reach * reach - across) - (hip.Y - ankle.Y));
    }

    // ------------------------------------------------------------------ the legs solved

    /// <summary>
    /// Turns each leg so its ankle is where <paramref name="feet"/> says, whatever the root and the hips have done: the
    /// knee bent until the hip reaches the ankle, the thigh turned onto it, the foot set at its pitch against the ground
    /// (not against the leg), the toes flat on the ground when the heel is lifted.
    /// </summary>
    private static void SolveLegs(Skeleton sk, SkeletonPose pose, Feet feet)
    {
        var hips = Local(sk, pose, HumanBones.Hips) * Local(sk, pose, HumanBones.Root);
        // Where a lean, a sway or a turned pelvis has lifted a hip past where its leg reaches, the body sinks until it reaches
        float sink = MathF.Max(Sink(sk, hips, HumanBones.ThighL, HumanBones.FootL, feet.AnkleL), Sink(sk, hips, HumanBones.ThighR, HumanBones.FootR, feet.AnkleR));
        if (sink > 0f)
        {
            pose.Offset[HumanBones.Root] -= new Vector3(0, sink, 0);
            hips = Local(sk, pose, HumanBones.Hips) * Local(sk, pose, HumanBones.Root);
        }
        if (!Matrix4x4.Invert(hips, out var undo)) return;
        var above = pose.Rotation[HumanBones.Root] * pose.Rotation[HumanBones.Hips];
        SolveLeg(sk, pose, HumanBones.ThighL, HumanBones.ShinL, HumanBones.FootL, HumanBones.ToeL, feet.AnkleL, feet.PitchL, undo, above);
        SolveLeg(sk, pose, HumanBones.ThighR, HumanBones.ShinR, HumanBones.FootR, HumanBones.ToeR, feet.AnkleR, feet.PitchR, undo, above);
    }

    // How far a hip must come down for its leg to reach its ankle with the knee no straighter than the sculpt's
    private static float Sink(Skeleton sk, Matrix4x4 hips, int thigh, int foot, Vector3 ankle)
    {
        var hip = Vector3.Transform(sk[thigh].Joint, hips);
        float reach = 0.9985f * Vector3.Distance(sk[thigh].Joint, sk[foot].Joint);
        float dx = ankle.X - hip.X, dz = ankle.Z - hip.Z;
        float across = MathF.Min(dx * dx + dz * dz, reach * reach * 0.98f);
        return MathF.Max(0f, (hip.Y - ankle.Y) - MathF.Sqrt(reach * reach - across));
    }

    private static Matrix4x4 Local(Skeleton sk, SkeletonPose pose, int b)
    {
        var joint = sk[b].Joint;
        return Matrix4x4.CreateTranslation(-joint) * Matrix4x4.CreateScale(pose.Scale[b]) * Matrix4x4.CreateFromQuaternion(pose.Rotation[b])
            * Matrix4x4.CreateTranslation(joint + pose.Offset[b]);
    }

    private static void SolveLeg(Skeleton sk, SkeletonPose pose, int thigh, int shin, int foot, int toe, Vector3 ankle, float pitch, Matrix4x4 undo, Quaternion above)
    {
        var j = sk[thigh].Joint;
        var k = sk[shin].Joint;
        var a = sk[foot].Joint;
        var target = Vector3.Transform(ankle, undo);
        float want = Vector3.Distance(target, j);
        float Reach(float bend) => Vector3.Distance(Vector3.Transform(a - k, Pitch(bend)) + k, j);

        // The knee's bend, by halves between straight and folded
        const float Folded = 2.6f;
        float bendAt;
        if (want >= Reach(0f)) bendAt = 0f;
        else if (want <= Reach(Folded)) bendAt = Folded;
        else
        {
            float lo = 0f, hi = Folded;
            for (int i = 0; i < 24; i++)
            {
                float mid = (lo + hi) / 2f;
                if (Reach(mid) > want) lo = mid; else hi = mid;
            }
            bendAt = (lo + hi) / 2f;
        }
        var knee = Pitch(bendAt);
        var thighTurn = FromTo(Vector3.Transform(a - k, knee) + k - j, target - j);
        pose.Rotation[thigh] = thighTurn;
        pose.Rotation[shin] = knee;
        var chain = above * thighTurn * knee;
        pose.Rotation[foot] = Quaternion.Normalize(Quaternion.Inverse(chain) * Pitch(pitch));
        pose.Rotation[toe] = pitch > 0f ? Pitch(-pitch) : Quaternion.Identity;
    }

    /// <summary>The shortest turn that takes the way of <paramref name="from"/> to the way of <paramref name="to"/>.</summary>
    internal static Quaternion FromTo(Vector3 from, Vector3 to)
    {
        var a = Vector3.Normalize(from);
        var b = Vector3.Normalize(to);
        float dot = Math.Clamp(Vector3.Dot(a, b), -1f, 1f);
        if (dot > 0.999999f) return Quaternion.Identity;
        var axis = Vector3.Cross(a, b);
        if (axis.LengthSquared() < 1e-12f) return Quaternion.CreateFromAxisAngle(Vector3.UnitX, MathF.PI);
        return Quaternion.CreateFromAxisAngle(Vector3.Normalize(axis), MathF.Acos(dot));
    }

    /// <summary>A leg lifted off the ground by <paramref name="h"/> (0 down, 1 at its highest): a step on the spot.</summary>
    private static void LiftLeg(SkeletonPose pose, float h, bool left)
    {
        if (h <= 0f) return;
        pose.Turn(left ? HumanBones.ThighL : HumanBones.ThighR, -0.32f * h);
        pose.Turn(left ? HumanBones.ShinL : HumanBones.ShinR, 0.62f * h);
        pose.Turn(left ? HumanBones.FootL : HumanBones.FootR, -0.12f * h);
    }

    // ------------------------------------------------------------------ hop, talking

    /// <summary>A hop: a crouch into it, the knees tucked at its top with the arms out at the shoulders, the legs reaching for the ground as it ends.</summary>
    private static void Hop(float progress, SkeletonPose pose)
    {
        float h = MathF.Sin(Math.Clamp(progress, 0f, 1f) * MathF.PI);
        float reachDown = Smooth(0.7f, 1f, progress);
        float tuck = h * (1f - 0.5f * reachDown);
        pose.Turn(HumanBones.ThighL, -0.55f * tuck);
        pose.Turn(HumanBones.ThighR, -0.45f * tuck);
        pose.Turn(HumanBones.ShinL, 1.0f * tuck);
        pose.Turn(HumanBones.ShinR, 0.85f * tuck);
        pose.Turn(HumanBones.FootL, -0.15f * tuck);
        pose.Turn(HumanBones.FootR, -0.15f * tuck);
        pose.Turn(HumanBones.UpperArmL, -0.25f * h, 0f, 0.95f * h);
        pose.Turn(HumanBones.UpperArmR, -0.25f * h, 0f, -0.95f * h);
        pose.Turn(HumanBones.ForeArmL, -0.3f * h);
        pose.Turn(HumanBones.ForeArmR, -0.3f * h);
        pose.Turn(HumanBones.Hips, -0.25f * tuck);
        pose.Turn(HumanBones.Spine, 0.1f * h + 0.25f * tuck);
        pose.Turn(HumanBones.Head, -0.12f * h);
        pose.Turn(HumanBones.Hair, -0.25f * h);
        pose.Turn(HumanBones.CoatTail, 0.3f * h);
        pose.Turn(HumanBones.Skirt, -0.1f * h);
        pose.Offset[HumanBones.Bag] += new Vector3(0, -0.012f * h, 0);
    }

    /// <summary>Talking with a hand: the right forearm raised before the body, the hand open, beating gently with the words; the head nodding a little.</summary>
    private static void Talk(float t, SkeletonPose pose)
    {
        float beat = MathF.Sin(t * 5.2f), slow = MathF.Sin(t * 1.3f);
        pose.Turn(HumanBones.UpperArmR, -0.25f, 0f, 0.14f);
        pose.Turn(HumanBones.ForeArmR, -0.95f + 0.14f * beat, 0f, 0.25f);
        pose.Turn(HumanBones.HandR, 0.15f * beat, 0f, -0.35f);
        pose.Turn(HumanBones.Chest, 0f, 0.035f * slow);
        pose.Turn(HumanBones.Head, 0.035f * MathF.Max(0f, MathF.Sin(t * 4.1f)), -0.03f * slow);
    }

    // ------------------------------------------------------------------ stances and rides

    /// <summary>
    /// A grown stance (style guide, "Grown animation", "Stances"): sitting on a seat <see cref="SeatShare"/> of the hip
    /// joints' height with the pelvis tilted back and the feet on the floor, sitting on the ground with the legs out,
    /// lying, swimming, fishing and tugging.
    /// </summary>
    private static void StanceClip(Skeleton sk, CharacterPose p, Legs legs, SkeletonPose pose)
    {
        var rest = RestFeet(sk);
        float hip = sk[HumanBones.ThighL].Joint.Y;
        switch (p.Stance)
        {
            case Overworld.Stance.Sit:
            {
                // The hip joints a little over the seat, the pelvis back 0.3 and the spine forward as much, so the thigh turns
                // about 70° at the hip; the shins down to the feet on the floor under the knees
                float seat = SeatShare * legs.HipY + 0.075f * legs.Size;
                pose.Offset[HumanBones.Root] += new Vector3(0, seat - hip, 0);
                pose.Turn(HumanBones.Hips, -0.3f);
                pose.Turn(HumanBones.Spine, 0.32f);
                var sat = rest;
                float ahead = sk[HumanBones.ThighL].Joint.Z + 0.94f * legs.Thigh;
                sat.AnkleL = sat.AnkleL with { X = sat.AnkleL.X * 1.05f, Z = ahead };
                sat.AnkleR = sat.AnkleR with { X = sat.AnkleR.X * 1.05f, Z = ahead };
                SolveLegs(sk, pose, sat);
                pose.Turn(HumanBones.UpperArmL, -0.12f, 0f, 0.1f);
                pose.Turn(HumanBones.UpperArmR, -0.12f, 0f, -0.1f);
                pose.Turn(HumanBones.ForeArmL, -0.75f);
                pose.Turn(HumanBones.ForeArmR, -0.75f);
                pose.Turn(HumanBones.Skirt, -0.9f);
                pose.Turn(HumanBones.CoatTail, -0.55f);
                break;
            }
            case Overworld.Stance.SitGround:
            {
                // The pelvis rolled well back, so the legs out ahead are only about 60° from it at the hip
                float low = 0.12f * legs.Size;
                pose.Offset[HumanBones.Root] += new Vector3(0, low - hip, 0);
                pose.Turn(HumanBones.Hips, -0.65f);
                pose.Turn(HumanBones.Spine, 0.45f);
                pose.Turn(HumanBones.Neck, 0.12f);
                pose.Turn(HumanBones.Head, 0.08f);
                var out_ = rest;
                float ahead = sk[HumanBones.ThighL].Joint.Z + 0.93f * (legs.Thigh + legs.Shin);
                float floor = 0.045f * legs.Size;
                out_.AnkleL = out_.AnkleL with { X = out_.AnkleL.X * 1.05f, Y = floor, Z = ahead };
                out_.AnkleR = out_.AnkleR with { X = out_.AnkleR.X * 1.05f, Y = floor, Z = ahead };
                out_.PitchL = out_.PitchR = -1.2f;
                SolveLegs(sk, pose, out_);
                pose.Turn(HumanBones.UpperArmL, 0.4f, 0f, -0.12f);
                pose.Turn(HumanBones.UpperArmR, 0.4f, 0f, 0.12f);
                pose.Turn(HumanBones.Skirt, -0.85f);
                pose.Turn(HumanBones.CoatTail, -0.9f);
                break;
            }
            case Overworld.Stance.Lie:
                pose.Turn(HumanBones.Root, -MathF.PI / 2f);
                pose.Offset[HumanBones.Root] += new Vector3(0, LieLift * legs.Size, 0);
                pose.Turn(HumanBones.Head, 0.1f);
                pose.Turn(HumanBones.UpperArmL, 0f, 0f, 0.1f);
                pose.Turn(HumanBones.UpperArmR, 0f, 0f, -0.1f);
                break;
            case Overworld.Stance.Swim:
            {
                // Treading water upright: long breaststroke sweeps at the shoulders' height, the legs kicking a little apart
                float phase = p.Time / Overworld.Seats.StrokeSeconds * Tau;
                float sweep = 0.5f + 0.5f * MathF.Sin(phase);
                pose.Turn(HumanBones.Spine, 0.08f);
                pose.Turn(HumanBones.Neck, -0.08f);
                pose.Turn(HumanBones.Head, -0.1f);
                pose.Turn(HumanBones.UpperArmL, -1.2f, 0f, -0.15f - 0.65f * sweep);
                pose.Turn(HumanBones.UpperArmR, -1.2f, 0f, 0.15f + 0.65f * sweep);
                pose.Turn(HumanBones.ForeArmL, -0.6f + 0.45f * sweep);
                pose.Turn(HumanBones.ForeArmR, -0.6f + 0.45f * sweep);
                pose.Turn(HumanBones.ThighL, -0.3f - 0.18f * MathF.Cos(phase), 0f, 0.1f);
                pose.Turn(HumanBones.ThighR, -0.3f - 0.18f * MathF.Cos(phase), 0f, -0.1f);
                pose.Turn(HumanBones.ShinL, 0.65f);
                pose.Turn(HumanBones.ShinR, 0.65f);
                pose.Turn(HumanBones.FootL, 0.5f);
                pose.Turn(HumanBones.FootR, 0.5f);
                pose.Turn(HumanBones.Hair, 0.04f * MathF.Sin(phase));
                pose.Turn(HumanBones.CoatTail, 0.4f);
                break;
            }
            case Overworld.Stance.Fish:
            case Overworld.Stance.Tug:
            {
                // The rod out over the water in both hands, the left foot a little ahead; a bite pulls the arms back and up
                float pull = p.Stance == Overworld.Stance.Tug ? MathF.Floor(p.Time / Overworld.Seats.TugSeconds % 1f * 3f) / 2f : 0f;
                pose.Turn(HumanBones.Spine, -0.1f * pull);
                pose.Turn(HumanBones.UpperArmR, -0.85f - 0.5f * pull, 0f, 0.18f);
                pose.Turn(HumanBones.ForeArmR, -0.5f - 0.3f * pull);
                pose.Turn(HumanBones.HandR, 1.85f - 0.7f * pull, -0.55f);
                pose.Turn(HumanBones.UpperArmL, -0.75f - 0.45f * pull, 0f, 0.38f);
                pose.Turn(HumanBones.ForeArmL, -0.75f - 0.3f * pull, 0f, 0.3f);
                var braced = rest;
                braced.AnkleL = braced.AnkleL with { Z = braced.AnkleL.Z + 0.14f * legs.Length };
                braced.AnkleR = braced.AnkleR with { Z = braced.AnkleR.Z - 0.04f * legs.Length };
                pose.Offset[HumanBones.Root] += new Vector3(0, -0.02f * legs.Size, -0.01f * legs.Size * pull);
                SolveLegs(sk, pose, braced);
                break;
            }
        }
    }

    /// <summary>How far a grown lier's body is lifted off what it lies on, in the body's size: the depth of its back.</summary>
    public const float LieLift = 0.08f;

    /// <summary>
    /// Riding (style guide, "Grown animation", "Stances"): kneeling on a Surfer's back, seated on a Bicycle with the feet
    /// on a crank that turns with the cycle, or astride a mount, crouching lower as it runs. Nothing here touches the ground.
    /// </summary>
    private static void Ride(Skeleton sk, CharacterPose p, Legs legs, SkeletonPose pose)
    {
        float hip = sk[HumanBones.ThighL].Joint.Y;
        float run = p.RunBlend > 0f ? Math.Clamp(p.RunBlend, 0f, 1f) : p.Running ? 1f : 0f;
        switch (p.Riding)
        {
            case Riding.Surf:
            case Riding.Mount when p.RideStance == RideStance.Swim:
            {
                // (a mount swimming is ridden kneeling on its back, as a Surfer is (plan 30 · FM13): its floats are too
                // wide to sit astride, so the root, which the field puts its saddle under the seat, is raised by it)
                if (p.Riding == Riding.Mount) pose.Offset[HumanBones.Root] += new Vector3(0, legs.HipY - 0.075f * legs.Size, 0);
                // Kneeling: the thighs a little forward of upright, the shins along the back behind, the body forward over
                // the right hand held ahead, the left on the thigh
                float kneel = 0.045f * legs.Size + legs.Thigh * MathF.Cos(0.3f);
                pose.Offset[HumanBones.Root] += new Vector3(0, kneel - hip, 0);
                pose.Turn(HumanBones.ThighL, -0.3f, 0f, 0.06f);
                pose.Turn(HumanBones.ThighR, -0.3f, 0f, -0.06f);
                pose.Turn(HumanBones.ShinL, MathF.PI / 2f + 0.3f);
                pose.Turn(HumanBones.ShinR, MathF.PI / 2f + 0.3f);
                pose.Turn(HumanBones.FootL, 0.6f);
                pose.Turn(HumanBones.FootR, 0.6f);
                pose.Turn(HumanBones.Spine, 0.28f);
                pose.Turn(HumanBones.Chest, 0.1f);
                pose.Turn(HumanBones.Head, -0.32f);
                pose.Turn(HumanBones.UpperArmR, -0.95f, 0f, 0.1f);
                pose.Turn(HumanBones.ForeArmR, -0.3f);
                pose.Turn(HumanBones.UpperArmL, -0.35f, 0f, 0.05f);
                pose.Turn(HumanBones.ForeArmL, -0.65f);
                pose.Turn(HumanBones.CoatTail, -0.5f);
                pose.Turn(HumanBones.Skirt, -0.9f);
                pose.Turn(HumanBones.Hair, 0.08f + 0.04f * MathF.Sin(p.Time * 3f));
                break;
            }
            case Riding.Bicycle:
            {
                // Seated, leaning to the bars, the feet on pedals round a crank ahead of and below the hips
                pose.Offset[HumanBones.Root] += new Vector3(0, -0.12f * legs.Length, 0);
                pose.Turn(HumanBones.Hips, -0.3f);
                pose.Turn(HumanBones.Spine, 0.75f);
                pose.Turn(HumanBones.Neck, -0.15f);
                pose.Turn(HumanBones.Head, -0.3f);
                pose.Turn(HumanBones.UpperArmL, -1.05f, 0f, -0.1f);
                pose.Turn(HumanBones.UpperArmR, -1.05f, 0f, 0.1f);
                pose.Turn(HumanBones.ForeArmL, -0.35f);
                pose.Turn(HumanBones.ForeArmR, -0.35f);
                var j = sk[HumanBones.ThighL].Joint;
                var centre = new Vector3(0, hip - 0.12f * legs.Length - 0.62f * legs.Length, j.Z + 0.2f * legs.Length);
                float crank = 0.17f * legs.Length, turn = Tau * p.Walk;
                var feet = new Feet
                {
                    AnkleL = centre + new Vector3(sk[HumanBones.FootL].Joint.X, crank * MathF.Cos(turn), crank * MathF.Sin(turn)),
                    AnkleR = centre + new Vector3(sk[HumanBones.FootR].Joint.X, -crank * MathF.Cos(turn), -crank * MathF.Sin(turn)),
                    PitchL = -0.1f, PitchR = -0.1f
                };
                SolveLegs(sk, pose, feet);
                pose.Turn(HumanBones.CoatTail, 0.35f);
                pose.Turn(HumanBones.Skirt, -0.5f);
                pose.Turn(HumanBones.Hair, 0.1f + 0.1f * Math.Clamp(p.WalkBlend, 0f, 1f));
                break;
            }
            case Riding.Mount:
            {
                // Astride: the thighs out round the mount's back and forward, the shins down and back, the hands ahead;
                // crouched lower over it as it runs, the head up to see the way
                float rock = 0.012f * legs.Size * MathF.Sin(2f * Tau * p.Walk) * Math.Clamp(p.WalkBlend, 0f, 1f);
                pose.Offset[HumanBones.Root] += new Vector3(0, rock, 0);
                pose.Turn(HumanBones.Hips, -0.35f);
                // The thighs spread round the mount's back where its width is told (plan 30 · FM13), the old 0.34 otherwise
                float spread = p.Astride > 0f ? AstrideSpread(sk, legs, p.Astride) : 0.34f;
                pose.Turn(HumanBones.ThighL, -0.6f, 0f, spread);
                pose.Turn(HumanBones.ThighR, -0.6f, 0f, -spread);
                pose.Turn(HumanBones.ShinL, 1.1f);
                pose.Turn(HumanBones.ShinR, 1.1f);
                pose.Turn(HumanBones.FootL, 0.3f);
                pose.Turn(HumanBones.FootR, 0.3f);
                pose.Turn(HumanBones.Spine, 0.5f + 0.25f * run);
                pose.Turn(HumanBones.Chest, 0.1f * run);
                pose.Turn(HumanBones.Neck, -0.05f - 0.05f * run);
                pose.Turn(HumanBones.Head, -0.12f - 0.25f * run);
                pose.Turn(HumanBones.UpperArmL, -0.8f - 0.3f * run, 0f, -0.12f);
                pose.Turn(HumanBones.UpperArmR, -0.8f - 0.3f * run, 0f, 0.12f);
                pose.Turn(HumanBones.ForeArmL, -0.5f);
                pose.Turn(HumanBones.ForeArmR, -0.5f);
                pose.Turn(HumanBones.CoatTail, 0.2f + 0.3f * run);
                pose.Turn(HumanBones.Skirt, -0.6f);
                pose.Turn(HumanBones.Hair, 0.08f + 0.18f * run);
                RideStanceOver(p.RideStance, legs, pose);
                break;
            }
        }
    }

    /// <summary>
    /// How far a rider's root is under the top of what they sit astride, in model units (plan 30 · FM13): the hip
    /// joints' height less the seat's flesh under them, as the sitter's (<see cref="StanceClip"/>, 0.075 of the body's size).
    /// </summary>
    public static float SaddleOf(Skeleton sk)
    {
        var legs = LegsOf(sk);
        return legs.HipY - 0.075f * legs.Size;
    }

    /// <summary>The widest the thighs spread astride (a roll at the hip): wider stretches the crotch's edges past what the grown sculpt's weights hold.</summary>
    public const float MostSpread = 0.6f;

    /// <summary>How far a climbing rider is leant forward about the hips onto the mount's back (plan 30 · FM13), in radians: the field leans the root, the clip turns the thighs back by as much.</summary>
    public const float ClimbLean = 0.6f;

    /// <summary>The thighs' spread (a roll at the hip) that brings the knees clear of a mount <paramref name="halfWidth"/> wide at the seat, with a thigh's girth to spare.</summary>
    internal static float AstrideSpread(Skeleton sk, Legs legs, float halfWidth)
    {
        float hipX = MathF.Abs(sk[HumanBones.ThighL].Joint.X);
        float clear = halfWidth + 0.06f * legs.Size - hipX;
        float share = Math.Clamp(clear / MathF.Max(1e-4f, legs.Thigh), 0f, 0.95f);
        return Math.Clamp(MathF.Asin(share), 0.34f, MostSpread);
    }

    /// <summary>
    /// What a rider's stance adds over sitting astride (style guide, "Open world", "The mount"): a dash crouches lower
    /// with the head up, a glide sits up with the arms out wide, a climb lies flat along the mount's back with the head
    /// turned up the face. Seated adds nothing; a swim is ridden kneeling (<see cref="Ride"/>).
    /// </summary>
    private static void RideStanceOver(RideStance stance, Legs legs, SkeletonPose pose)
    {
        switch (stance)
        {
            case RideStance.Dash:
                pose.Turn(HumanBones.Spine, 0.08f);
                pose.Turn(HumanBones.Neck, -0.1f);
                pose.Turn(HumanBones.Head, -0.2f);
                pose.Turn(HumanBones.UpperArmL, -0.2f);
                pose.Turn(HumanBones.UpperArmR, -0.2f);
                pose.Turn(HumanBones.CoatTail, 0.3f);
                pose.Turn(HumanBones.Hair, 0.15f);
                break;
            case RideStance.Glide:
                pose.Turn(HumanBones.Spine, -0.25f);
                pose.Turn(HumanBones.Head, 0.1f);
                pose.Turn(HumanBones.UpperArmL, 0.35f, 0f, 0.75f);
                pose.Turn(HumanBones.UpperArmR, 0.35f, 0f, -0.75f);
                pose.Turn(HumanBones.ForeArmL, 0.3f);
                pose.Turn(HumanBones.ForeArmR, 0.3f);
                pose.Turn(HumanBones.CoatTail, 0.6f);
                pose.Turn(HumanBones.Hair, 0.3f);
                break;
            case RideStance.Climb:
                // (the field leans the whole rider ClimbLean forward onto the mount's back; the thighs are turned back by it)
                pose.Turn(HumanBones.ThighL, ClimbLean);
                pose.Turn(HumanBones.ThighR, ClimbLean);
                pose.Turn(HumanBones.Spine, 0.15f);
                pose.Turn(HumanBones.Hips, -0.15f);
                pose.Turn(HumanBones.Neck, -0.25f);
                pose.Turn(HumanBones.Head, -0.45f);
                pose.Turn(HumanBones.UpperArmL, -0.5f);
                pose.Turn(HumanBones.UpperArmR, -0.5f);
                break;
        }
    }

    // ------------------------------------------------------------------ emotes

    /// <summary>The emotes at a grown body's size (style guide, "Grown animation"), over a standing pose whose feet are solved after.</summary>
    private static void EmoteClip(Emote emote, float t, Legs legs, SkeletonPose pose)
    {
        float d = CharacterAnimation.Duration(emote);
        switch (emote)
        {
            case Emote.Wave:
            {
                // The right hand above the shoulder, waving from the elbow
                float wave = MathF.Sin(t * Tau * 2.2f);
                pose.Rotation[HumanBones.UpperArmR] = Quaternion.Identity;
                pose.Turn(HumanBones.UpperArmR, -0.25f, 0f, -1.0f);
                pose.Rotation[HumanBones.ForeArmR] = Quaternion.Identity;
                pose.Turn(HumanBones.ForeArmR, -0.15f, 0f, -1.35f + 0.4f * wave);
                pose.Turn(HumanBones.ClavicleR, 0f, 0f, -0.4f);
                pose.Turn(HumanBones.Head, 0f, -0.08f, 0.06f);
                pose.Turn(HumanBones.Hair, 0f, 0f, -0.03f * wave);
                break;
            }
            case Emote.Surprised:
            {
                // A start back, the hands up before the chest, the head up
                float jump = MathF.Sin(Math.Clamp(t / 0.3f, 0f, 1f) * MathF.PI);
                pose.Offset[HumanBones.Root] += new Vector3(0, -0.015f * legs.Size * jump, -0.03f * legs.Size * Smooth(0f, 0.3f, t));
                pose.Turn(HumanBones.Spine, -0.1f);
                pose.Turn(HumanBones.Chest, -0.06f);
                pose.Turn(HumanBones.Head, -0.16f);
                pose.Turn(HumanBones.ClavicleL, 0f, 0f, 0.06f);
                pose.Turn(HumanBones.ClavicleR, 0f, 0f, -0.06f);
                pose.Turn(HumanBones.UpperArmL, -0.75f, 0f, 0.1f);
                pose.Turn(HumanBones.UpperArmR, -0.75f, 0f, -0.1f);
                pose.Turn(HumanBones.ForeArmL, -1.35f);
                pose.Turn(HumanBones.ForeArmR, -1.35f);
                pose.Turn(HumanBones.Hair, -0.2f * jump);
                break;
            }
            case Emote.Nod:
            {
                float nod = MathF.Max(0f, MathF.Sin(t * Tau * 2f));
                pose.Turn(HumanBones.Neck, 0.1f * nod);
                pose.Turn(HumanBones.Head, 0.2f * nod);
                pose.Turn(HumanBones.Hair, 0.08f * MathF.Max(0f, MathF.Sin(t * Tau * 2f - 0.8f)));
                break;
            }
            case Emote.Cheer:
            {
                // Both arms up in a V, bouncing on the knees
                float bounce = MathF.Abs(MathF.Sin(t * Tau * 1.6f));
                pose.Offset[HumanBones.Root] += new Vector3(0, -0.03f * legs.Size * (1f - bounce), 0);
                pose.Rotation[HumanBones.UpperArmL] = Quaternion.Identity;
                pose.Rotation[HumanBones.UpperArmR] = Quaternion.Identity;
                // The shoulders lifted on the collarbones and the forearms up from the elbows, so the armpit isn't stretched
                pose.Turn(HumanBones.UpperArmL, -0.15f, 0f, 0.95f);
                pose.Turn(HumanBones.UpperArmR, -0.15f, 0f, -0.95f);
                pose.Rotation[HumanBones.ForeArmL] = Quaternion.Identity;
                pose.Rotation[HumanBones.ForeArmR] = Quaternion.Identity;
                pose.Turn(HumanBones.ForeArmL, -0.1f, 0f, 1.15f);
                pose.Turn(HumanBones.ForeArmR, -0.1f, 0f, -1.15f);
                pose.Turn(HumanBones.ClavicleL, 0f, 0f, 0.25f);
                pose.Turn(HumanBones.ClavicleR, 0f, 0f, -0.25f);
                pose.Turn(HumanBones.Head, -0.18f);
                pose.Turn(HumanBones.Hair, -0.1f * bounce);
                break;
            }
            case Emote.Throw:
            {
                // Overarm: back and up, over the top as the chest turns into it, let go at the release, through low across the body
                float back = Smooth(0f, 0.18f, t) * (1f - Smooth(0.18f, CharacterAnimation.ThrowRelease, t));
                float over = Smooth(0.16f, CharacterAnimation.ThrowRelease, t) * (1f - Smooth(CharacterAnimation.ThrowRelease + 0.05f, 0.62f, t));
                float through = Smooth(CharacterAnimation.ThrowRelease, 0.45f, t);
                pose.Rotation[HumanBones.UpperArmR] = Quaternion.Identity;
                pose.Turn(HumanBones.UpperArmR, 1.2f * back - 2.6f * over - 0.95f * through * (1f - over), 0f, -0.6f * back - 0.25f * over);
                pose.Rotation[HumanBones.ForeArmR] = Quaternion.Identity;
                pose.Turn(HumanBones.ForeArmR, -1.4f * back - 0.25f * over - 0.3f * through);
                pose.Turn(HumanBones.ClavicleR, 0.1f * back - 0.12f * over, 0f, -0.1f * back);
                pose.Turn(HumanBones.UpperArmL, -0.55f * back + 0.3f * through, 0f, 0.35f * back);
                pose.Turn(HumanBones.Spine, -0.08f * back + 0.12f * over + 0.08f * through, -0.2f * back + 0.15f * over);
                pose.Turn(HumanBones.Chest, -0.06f * back + 0.12f * over + 0.06f * through, -0.25f * back + 0.2f * over);
                pose.Turn(HumanBones.Head, 0.06f * back - 0.08f * over, 0.3f * back - 0.25f * over);
                pose.Offset[HumanBones.Root] += new Vector3(0, -0.01f * legs.Size * over, 0.02f * legs.Size * (over + through * (1f - over)));
                pose.Turn(HumanBones.Hair, 0.15f * back - 0.2f * over);
                break;
            }
            case Emote.Bow:
            {
                // From the hips, the spine and the head following, the arms hanging straight down, the hips going back over the feet
                float bend = Smooth(0.05f, 0.4f, t) * (1f - Smooth(d - 0.45f, d - 0.1f, t));
                pose.Offset[HumanBones.Root] += new Vector3(0, 0, -0.04f * legs.Size * bend);
                pose.Turn(HumanBones.Hips, 0.45f * bend);
                pose.Turn(HumanBones.Spine, 0.2f * bend);
                pose.Turn(HumanBones.Chest, 0.08f * bend);
                pose.Turn(HumanBones.Neck, 0.08f * bend);
                pose.Turn(HumanBones.Head, 0.12f * bend);
                pose.Turn(HumanBones.UpperArmL, -0.75f * bend, 0f, -0.04f * bend);
                pose.Turn(HumanBones.UpperArmR, -0.75f * bend, 0f, 0.04f * bend);
                pose.Turn(HumanBones.ForeArmL, 0.1f * bend);
                pose.Turn(HumanBones.ForeArmR, 0.1f * bend);
                pose.Turn(HumanBones.Hair, 0.15f * bend);
                pose.Turn(HumanBones.Bag, -0.2f * bend);
                pose.Turn(HumanBones.CoatTail, -0.35f * bend);
                pose.Turn(HumanBones.Skirt, -0.3f * bend);
                break;
            }
            case Emote.Shrug:
            {
                // Both collarbones up, the forearms out with the palms up, the head tilted
                float up = Smooth(0.1f, 0.35f, t) * (1f - Smooth(d - 0.4f, d - 0.12f, t));
                pose.Turn(HumanBones.ClavicleL, 0f, 0f, 0.22f * up);
                pose.Turn(HumanBones.ClavicleR, 0f, 0f, -0.22f * up);
                pose.Turn(HumanBones.UpperArmL, -0.15f * up, 0f, -0.1f * up);
                pose.Turn(HumanBones.UpperArmR, -0.15f * up, 0f, 0.1f * up);
                pose.Turn(HumanBones.ForeArmL, -1.0f * up, 0f, 0.6f * up);
                pose.Turn(HumanBones.ForeArmR, -1.0f * up, 0f, -0.6f * up);
                pose.Turn(HumanBones.HandL, 0f, 0f, 0.4f * up);
                pose.Turn(HumanBones.HandR, 0f, 0f, -0.4f * up);
                pose.Turn(HumanBones.Head, 0.04f * up, 0f, 0.14f * up);
                break;
            }
            case Emote.Point:
            {
                // The right arm straight out ahead at the shoulder's height, the chest and the head turned after it
                float out_ = Smooth(0.05f, 0.3f, t) * (1f - Smooth(d - 0.35f, d - 0.08f, t));
                pose.Rotation[HumanBones.UpperArmR] = Quaternion.Slerp(pose.Rotation[HumanBones.UpperArmR], Quaternion.Identity, out_);
                pose.Turn(HumanBones.UpperArmR, -1.5f * out_, 0f, 0.36f * out_);
                pose.Rotation[HumanBones.ForeArmR] = Quaternion.Slerp(pose.Rotation[HumanBones.ForeArmR], Quaternion.Identity, out_);
                pose.Rotation[HumanBones.HandR] = Quaternion.Slerp(pose.Rotation[HumanBones.HandR], Quaternion.Identity, out_);
                pose.Turn(HumanBones.ClavicleR, -0.05f * out_);
                pose.Turn(HumanBones.Chest, 0f, -0.14f * out_);
                pose.Turn(HumanBones.Head, 0.03f * out_, -0.12f * out_);
                break;
            }
            case Emote.Laugh:
            {
                // The head back, the chest shaking with it, the hands at the belly
                float back = Smooth(0.05f, 0.3f, t) * (1f - Smooth(d - 0.35f, d - 0.08f, t));
                float shake = MathF.Abs(MathF.Sin(t * Tau * 3.5f)) * back;
                pose.Turn(HumanBones.Neck, -0.14f * back);
                pose.Turn(HumanBones.Head, -0.26f * back + 0.05f * shake);
                pose.Turn(HumanBones.Spine, -0.1f * back);
                pose.Offset[HumanBones.Chest] += new Vector3(0, 0.006f * legs.Size * shake, 0);
                pose.Turn(HumanBones.UpperArmL, -0.12f * back, 0f, 0.04f * back);
                pose.Turn(HumanBones.UpperArmR, -0.12f * back, 0f, -0.04f * back);
                pose.Turn(HumanBones.ForeArmL, -1.2f * back, 0f, -0.55f * back);
                pose.Turn(HumanBones.ForeArmR, -1.2f * back, 0f, 0.55f * back);
                pose.Turn(HumanBones.Hair, -0.12f * back + 0.06f * shake);
                break;
            }
        }
    }
}
