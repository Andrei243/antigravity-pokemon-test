using System;
using System.Numerics;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// The characters' animation clips, written in code: idle breathing, the walk and run cycles, the ledge hop and
/// the emotes, blended by the pose's inputs. Hair, bag and skirt follow through: they lag the body's bounce and
/// swing back after it. Angles are in radians about the model axes (the character faces +Z; L is +X): a positive
/// pitch swings a hanging limb backward and tips the face down; a positive roll lifts the left arm outward.
/// No GPU calls.
/// </summary>
internal static class CharacterAnimation
{
    private const float Tau = MathF.PI * 2f;

    /// <summary>How long each emote lasts, in seconds.</summary>
    public static float Duration(Emote emote) => emote switch
    {
        Emote.Wave => 1.6f,
        Emote.Surprised => 0.9f,
        Emote.Nod => 1.0f,
        Emote.Cheer => 1.4f,
        Emote.Throw => 0.75f,
        Emote.Bow => 1.2f,
        Emote.Shrug => 1.0f,
        Emote.Point => 1.2f,
        Emote.Laugh => 1.4f,
        _ => 0f
    };

    /// <summary>
    /// The emote a script's gesture plays (plan 11 · C11); none for a jump, which is the hop's clip played on the
    /// spot (<see cref="HopOnTheSpot"/>).
    /// </summary>
    public static Emote EmoteOf(Gesture gesture) => gesture switch
    {
        Gesture.Wave => Emote.Wave,
        Gesture.Nod => Emote.Nod,
        Gesture.Cheer => Emote.Cheer,
        Gesture.Surprised => Emote.Surprised,
        Gesture.Bow => Emote.Bow,
        Gesture.Shrug => Emote.Shrug,
        Gesture.Point => Emote.Point,
        Gesture.Laugh => Emote.Laugh,
        _ => Emote.None
    };

    /// <summary>The face a script's mood holds (plan 11 · C11).</summary>
    public static Expression ExpressionOf(Mood mood) => mood switch
    {
        Mood.Happy => Expression.Happy,
        Mood.Surprised => Expression.Surprised,
        Mood.Sad => Expression.Sad,
        Mood.Angry => Expression.Angry,
        _ => Expression.Neutral
    };

    /// <summary>
    /// The pose of someone playing a gesture <paramref name="age"/> seconds into its round: an emote over the idle,
    /// or for a jump the hop's progress (the lift is the caller's, <see cref="JumpLift"/>).
    /// </summary>
    public static void Act(ref CharacterPose pose, Gesture gesture, float age)
    {
        if (gesture == Gesture.Jump)
        {
            pose.Hop = HopOnTheSpot(age);
            return;
        }
        pose.Emote = EmoteOf(gesture);
        pose.EmoteTime = age;
    }

    /// <summary>A jump on the spot's progress through the hop's clip, <paramref name="age"/> seconds into it; 0 once it is over.</summary>
    public static float HopOnTheSpot(float age) => age > 0f && age < Acting.JumpSeconds ? age / Acting.JumpSeconds : 0f;

    /// <summary>How high a jump on the spot lifts its jumper, in texels of the field (32 to a tile): eight at its height.</summary>
    public static float JumpLift(float age) => MathF.Round(Acting.JumpTexels * MathF.Sin(HopOnTheSpot(age) * MathF.PI));

    /// <summary>When a throw lets go of the ball, seconds into the emote.</summary>
    public const float ThrowRelease = 0.3f;

    /// <summary>The face an emote pulls, unless the pose asks for another.</summary>
    public static Expression ExpressionOf(CharacterPose p)
    {
        if (p.Expression != Expression.Neutral) return p.Expression;
        if (p.EmoteTime >= Duration(p.Emote)) return Expression.Neutral;
        return p.Emote switch
        {
            Emote.Wave or Emote.Cheer or Emote.Laugh => Expression.Happy,
            Emote.Surprised => Expression.Surprised,
            _ => Expression.Neutral
        };
    }

    private static SkeletonPose? walkScratch, extraScratch;

    public static void Apply(CharacterRig rig, CharacterPose p, SkeletonPose pose)
    {
        pose.Reset();
        switch (rig.Kind)
        {
            case RigKind.Briefcase:
                return;
            case RigKind.Rift:
                Rift(p, pose);
                return;
        }

        var walk = walkScratch is { } w && w.Count == pose.Count ? w : walkScratch = new SkeletonPose(pose.Count);
        var extra = extraScratch is { } e && e.Count == pose.Count ? e : extraScratch = new SkeletonPose(pose.Count);

        Idle(p.Time, pose);
        if (p.WalkBlend > 0f)
        {
            walk.Reset();
            if (p.Running) Run(p.Walk, walk);
            else Walk(p.Walk, walk);
            pose.BlendToward(walk, Math.Clamp(p.WalkBlend, 0f, 1f));
        }
        if (p.Hop > 0f && p.Hop < 1f)
        {
            extra.Reset();
            Hop(p.Hop, extra);
            pose.BlendToward(extra, MathF.Sin(p.Hop * MathF.PI));
        }
        float duration = Duration(p.Emote);
        if (p.Emote != Emote.None && p.EmoteTime < duration)
        {
            extra.Reset();
            Idle(p.Time, extra);
            EmoteClip(p.Emote, p.EmoteTime, extra);
            float t = p.EmoteTime;
            float envelope = Smooth(0f, 0.18f, t) * (1f - Smooth(duration - 0.22f, duration, t));
            pose.BlendToward(extra, envelope);
        }
    }

    private static float Smooth(float a, float b, float x)
    {
        float t = Math.Clamp((x - a) / (b - a), 0f, 1f);
        return t * t * (3f - 2f * t);
    }

    private static void Up(SkeletonPose pose, int bone, float dy) => pose.Offset[bone] += new Vector3(0, dy, 0);

    // ------------------------------------------------------------------ clips

    /// <summary>Standing: breathing, the arms down from the sculpted A-pose, the head tipped up toward the camera.</summary>
    private static void Idle(float t, SkeletonPose pose)
    {
        float breath = MathF.Sin(t * Tau / 3.4f);
        Up(pose, HumanBones.Spine, 0.003f * breath);
        pose.Turn(HumanBones.Spine, -0.02f - 0.012f * breath);
        pose.Turn(HumanBones.Head, -0.1f + 0.02f * MathF.Sin(t * 2.3f + 0.6f), 0.04f * MathF.Sin(t * 0.7f));
        pose.Turn(HumanBones.UpperArmL, 0.03f, 0f, -0.26f + 0.015f * breath);
        pose.Turn(HumanBones.UpperArmR, 0.03f, 0f, 0.26f - 0.015f * breath);
        pose.Turn(HumanBones.ForeArmL, -0.2f, 0f, -0.06f);
        pose.Turn(HumanBones.ForeArmR, -0.2f, 0f, 0.06f);
        pose.Turn(HumanBones.HandL, 0f, 0f, -0.1f);
        pose.Turn(HumanBones.HandR, 0f, 0f, 0.1f);
        pose.Turn(HumanBones.ShinL, 0.03f);
        pose.Turn(HumanBones.ShinR, 0.03f);
        pose.Turn(HumanBones.Hair, 0.03f * breath);
        pose.Turn(HumanBones.Bag, -0.02f * breath);
    }

    /// <summary>
    /// One walk cycle per unit of <paramref name="cycle"/> (two steps). The hips drop as the legs spread so the
    /// standing foot stays on the ground; arms swing against the legs; hair and bag bounce a beat behind.
    /// </summary>
    private static void Walk(float cycle, SkeletonPose pose)
    {
        float th = cycle * Tau;
        float s = MathF.Sin(th), c = MathF.Cos(th);
        float bounce = MathF.Sin(2f * th - 1.1f);

        Up(pose, HumanBones.Root, -0.022f * s * s);
        pose.Turn(HumanBones.Hips, 0f, 0.08f * s, 0.025f * c);
        pose.Turn(HumanBones.Spine, 0.04f, -0.1f * s);
        pose.Turn(HumanBones.Head, -0.1f + 0.03f * MathF.Cos(2f * th), 0.03f * s);

        // Left leg forward while sin > 0; a leg bends at the knee as it swings through
        pose.Turn(HumanBones.ThighL, -0.42f * s);
        pose.Turn(HumanBones.ThighR, 0.42f * s);
        pose.Turn(HumanBones.ShinL, 0.08f + 0.55f * MathF.Max(0f, c));
        pose.Turn(HumanBones.ShinR, 0.08f + 0.55f * MathF.Max(0f, -c));
        pose.Turn(HumanBones.FootL, 0.28f * MathF.Max(0f, -s) - 0.16f * MathF.Max(0f, s) - 0.3f * MathF.Max(0f, c));
        pose.Turn(HumanBones.FootR, 0.28f * MathF.Max(0f, s) - 0.16f * MathF.Max(0f, -s) - 0.3f * MathF.Max(0f, -c));

        pose.Turn(HumanBones.UpperArmL, 0.32f * s, 0f, -0.2f);
        pose.Turn(HumanBones.UpperArmR, -0.32f * s, 0f, 0.2f);
        pose.Turn(HumanBones.ForeArmL, -0.25f - 0.25f * MathF.Max(0f, -s));
        pose.Turn(HumanBones.ForeArmR, -0.25f - 0.25f * MathF.Max(0f, s));
        pose.Turn(HumanBones.HandL, 0f, 0f, -0.08f);
        pose.Turn(HumanBones.HandR, 0f, 0f, 0.08f);

        pose.Turn(HumanBones.Hair, 0.05f + 0.07f * bounce);
        pose.Turn(HumanBones.Bag, 0.06f * bounce);
        Up(pose, HumanBones.Bag, 0.004f * bounce);
        pose.Turn(HumanBones.Skirt, 0.03f, 0f, 0.06f * MathF.Sin(th - 0.7f));
    }

    /// <summary>The run: longer strides with a moment off the ground, a forward lean, elbows bent.</summary>
    private static void Run(float cycle, SkeletonPose pose)
    {
        float th = cycle * Tau;
        float s = MathF.Sin(th), c = MathF.Cos(th);
        float bounce = MathF.Sin(2f * th - 1.1f);

        Up(pose, HumanBones.Root, 0.022f * MathF.Cos(2f * th) - 0.012f);
        pose.Turn(HumanBones.Hips, 0.1f, 0.1f * s, 0.03f * c);
        pose.Turn(HumanBones.Spine, 0.18f, -0.14f * s);
        pose.Turn(HumanBones.Head, -0.26f + 0.03f * MathF.Cos(2f * th), 0.04f * s);

        pose.Turn(HumanBones.ThighL, -0.72f * s - 0.1f);
        pose.Turn(HumanBones.ThighR, 0.72f * s - 0.1f);
        pose.Turn(HumanBones.ShinL, 0.25f + 1.05f * MathF.Max(0f, c));
        pose.Turn(HumanBones.ShinR, 0.25f + 1.05f * MathF.Max(0f, -c));
        pose.Turn(HumanBones.FootL, 0.4f * MathF.Max(0f, -s) - 0.2f * MathF.Max(0f, s));
        pose.Turn(HumanBones.FootR, 0.4f * MathF.Max(0f, s) - 0.2f * MathF.Max(0f, -s));

        pose.Turn(HumanBones.UpperArmL, 0.62f * s, 0f, -0.12f);
        pose.Turn(HumanBones.UpperArmR, -0.62f * s, 0f, 0.12f);
        pose.Turn(HumanBones.ForeArmL, -1.2f - 0.2f * MathF.Max(0f, -s));
        pose.Turn(HumanBones.ForeArmR, -1.2f - 0.2f * MathF.Max(0f, s));

        pose.Turn(HumanBones.Hair, 0.16f + 0.1f * bounce);
        pose.Turn(HumanBones.Bag, 0.12f * bounce + 0.04f);
        Up(pose, HumanBones.Bag, 0.01f * bounce);
        pose.Turn(HumanBones.Skirt, 0.08f, 0f, 0.08f * MathF.Sin(th - 0.7f));
    }

    /// <summary>Jumping down a ledge: knees tucked, arms up for balance, hair lifting.</summary>
    private static void Hop(float progress, SkeletonPose pose)
    {
        float h = MathF.Sin(Math.Clamp(progress, 0f, 1f) * MathF.PI);
        pose.Turn(HumanBones.ThighL, -0.7f * h);
        pose.Turn(HumanBones.ThighR, -0.55f * h);
        pose.Turn(HumanBones.ShinL, 1.2f * h);
        pose.Turn(HumanBones.ShinR, 1.0f * h);
        pose.Turn(HumanBones.UpperArmL, -0.5f * h, 0f, 0.9f * h);
        pose.Turn(HumanBones.UpperArmR, -0.5f * h, 0f, -0.9f * h);
        pose.Turn(HumanBones.ForeArmL, -0.4f * h);
        pose.Turn(HumanBones.ForeArmR, -0.4f * h);
        pose.Turn(HumanBones.Spine, 0.12f * h);
        pose.Turn(HumanBones.Head, -0.15f * h);
        pose.Turn(HumanBones.Hair, -0.3f * h);
        Up(pose, HumanBones.Bag, -0.015f * h);
        pose.Turn(HumanBones.Skirt, -0.12f * h);
    }

    private static void EmoteClip(Emote emote, float t, SkeletonPose pose)
    {
        switch (emote)
        {
            case Emote.Bow or Emote.Shrug or Emote.Point or Emote.Laugh:
                CueClip(emote, t, pose);
                break;
            case Emote.Wave:
            {
                // The right hand up beside the head (out to the side, the forearm raised, so the big head
                // doesn't hide it), waving from the elbow
                float wave = MathF.Sin(t * Tau * 2.2f);
                pose.Rotation[HumanBones.UpperArmR] = Quaternion.Identity;
                pose.Turn(HumanBones.UpperArmR, -0.2f, 0f, -1.55f);
                pose.Rotation[HumanBones.ForeArmR] = Quaternion.Identity;
                pose.Turn(HumanBones.ForeArmR, -0.15f, 0f, -1.25f + 0.4f * wave);
                pose.Turn(HumanBones.Head, 0f, -0.06f, 0.08f);
                pose.Turn(HumanBones.Hair, 0f, 0f, -0.04f * wave);
                break;
            }
            case Emote.Surprised:
            {
                // A start: a little jump back, arms flung out, the head up
                float jump = MathF.Sin(Math.Clamp(t / 0.3f, 0f, 1f) * MathF.PI);
                Up(pose, HumanBones.Root, 0.05f * jump);
                pose.Offset[HumanBones.Root] += new Vector3(0, 0, -0.03f * Smooth(0f, 0.3f, t));
                pose.Turn(HumanBones.UpperArmL, -0.3f, 0f, 1.0f);
                pose.Turn(HumanBones.UpperArmR, -0.3f, 0f, -1.0f);
                pose.Turn(HumanBones.ForeArmL, -0.7f);
                pose.Turn(HumanBones.ForeArmR, -0.7f);
                pose.Turn(HumanBones.Spine, -0.12f);
                pose.Turn(HumanBones.Head, -0.2f);
                pose.Turn(HumanBones.Hair, -0.3f * jump);
                pose.Turn(HumanBones.ShinL, 0.3f * jump);
                pose.Turn(HumanBones.ShinR, 0.3f * jump);
                break;
            }
            case Emote.Nod:
                pose.Turn(HumanBones.Head, 0.28f * MathF.Max(0f, MathF.Sin(t * Tau * 2f)));
                pose.Turn(HumanBones.Hair, 0.1f * MathF.Max(0f, MathF.Sin(t * Tau * 2f - 0.8f)));
                break;
            case Emote.Throw:
            {
                // Overarm: the right arm swings back and up, comes over the top as the body turns into the throw,
                // lets go at ThrowRelease and follows through low across the body
                float back = Smooth(0f, 0.18f, t) * (1f - Smooth(0.18f, ThrowRelease, t));
                float over = Smooth(0.16f, ThrowRelease, t) * (1f - Smooth(ThrowRelease + 0.05f, 0.62f, t));
                float through = Smooth(ThrowRelease, 0.45f, t);
                pose.Rotation[HumanBones.UpperArmR] = Quaternion.Identity;
                pose.Turn(HumanBones.UpperArmR, 1.25f * back - 2.5f * over - 0.9f * through * (1f - over), 0f, -0.55f * back - 0.25f * over);
                pose.Rotation[HumanBones.ForeArmR] = Quaternion.Identity;
                pose.Turn(HumanBones.ForeArmR, -1.3f * back - 0.25f * over - 0.3f * through);
                pose.Turn(HumanBones.UpperArmL, -0.5f * back + 0.3f * through, 0f, 0.35f * back);
                pose.Turn(HumanBones.Spine, -0.12f * back + 0.22f * over + 0.12f * through, -0.4f * back + 0.3f * over);
                pose.Turn(HumanBones.Head, 0.08f * back - 0.1f * over, 0.25f * back - 0.15f * over);
                pose.Turn(HumanBones.ThighR, 0.25f * back - 0.15f * through);
                pose.Turn(HumanBones.ThighL, -0.3f * over);
                pose.Turn(HumanBones.Hair, 0.2f * back - 0.25f * over);
                break;
            }
            case Emote.Cheer:
            {
                // Both arms up, bouncing on the spot
                float bounce = MathF.Abs(MathF.Sin(t * Tau * 1.6f));
                Up(pose, HumanBones.Root, 0.03f * bounce);
                // A "\o/": upper arms out, forearms up, hands either side of the head
                pose.Rotation[HumanBones.UpperArmL] = Quaternion.Identity;
                pose.Rotation[HumanBones.UpperArmR] = Quaternion.Identity;
                pose.Turn(HumanBones.UpperArmL, -0.15f, 0f, 1.75f);
                pose.Turn(HumanBones.UpperArmR, -0.15f, 0f, -1.75f);
                pose.Rotation[HumanBones.ForeArmL] = Quaternion.Identity;
                pose.Rotation[HumanBones.ForeArmR] = Quaternion.Identity;
                pose.Turn(HumanBones.ForeArmL, -0.1f, 0f, 0.95f);
                pose.Turn(HumanBones.ForeArmR, -0.1f, 0f, -0.95f);
                pose.Turn(HumanBones.Head, -0.18f);
                pose.Turn(HumanBones.Hair, -0.12f * bounce);
                pose.Turn(HumanBones.ShinL, 0.25f * (1f - bounce));
                pose.Turn(HumanBones.ShinR, 0.25f * (1f - bounce));
                break;
            }
        }
    }

    /// <summary>The emotes of plan 11 · C11, which a script plays on cue (<c>act</c>): a bow, a shrug, a point and a laugh.</summary>
    private static void CueClip(Emote emote, float t, SkeletonPose pose)
    {
        float d = Duration(emote);
        switch (emote)
        {
            case Emote.Bow:
            {
                // Bent forward from the waist and held a moment, the head down, the arms hanging straight from the
                // shoulders, then up again
                float bend = Smooth(0.05f, 0.4f, t) * (1f - Smooth(d - 0.45f, d - 0.1f, t));
                pose.Turn(HumanBones.Hips, 0.12f * bend);
                pose.Turn(HumanBones.Spine, 0.42f * bend);
                pose.Turn(HumanBones.Head, 0.3f * bend);
                pose.Turn(HumanBones.UpperArmL, -0.4f * bend, 0f, -0.06f * bend);
                pose.Turn(HumanBones.UpperArmR, -0.4f * bend, 0f, 0.06f * bend);
                pose.Turn(HumanBones.ForeArmL, 0.12f * bend);
                pose.Turn(HumanBones.ForeArmR, 0.12f * bend);
                pose.Turn(HumanBones.Hair, 0.2f * bend);
                pose.Turn(HumanBones.Bag, -0.15f * bend);
                pose.Turn(HumanBones.Skirt, -0.08f * bend);
                break;
            }
            case Emote.Shrug:
            {
                // Both shoulders up, the forearms out to the sides with the hands open, the head tilted
                float up = Smooth(0.1f, 0.35f, t) * (1f - Smooth(d - 0.4f, d - 0.12f, t));
                Up(pose, HumanBones.UpperArmL, 0.035f * up);
                Up(pose, HumanBones.UpperArmR, 0.035f * up);
                pose.Turn(HumanBones.UpperArmL, -0.15f * up, 0f, 0.2f * up);
                pose.Turn(HumanBones.UpperArmR, -0.15f * up, 0f, -0.2f * up);
                pose.Turn(HumanBones.ForeArmL, -0.9f * up, 0f, 0.55f * up);
                pose.Turn(HumanBones.ForeArmR, -0.9f * up, 0f, -0.55f * up);
                pose.Turn(HumanBones.HandL, 0f, 0f, 0.3f * up);
                pose.Turn(HumanBones.HandR, 0f, 0f, -0.3f * up);
                pose.Turn(HumanBones.Head, 0.04f * up, 0f, 0.16f * up);
                Up(pose, HumanBones.Spine, 0.006f * up);
                break;
            }
            case Emote.Point:
            {
                // The right arm straight out ahead at the shoulder's height, the head and the body turned a little
                // after it
                float out_ = Smooth(0.05f, 0.3f, t) * (1f - Smooth(d - 0.35f, d - 0.08f, t));
                pose.Rotation[HumanBones.UpperArmR] = Quaternion.Slerp(pose.Rotation[HumanBones.UpperArmR], Quaternion.Identity, out_);
                pose.Turn(HumanBones.UpperArmR, -1.5f * out_, 0f, 0.42f * out_);
                pose.Rotation[HumanBones.ForeArmR] = Quaternion.Slerp(pose.Rotation[HumanBones.ForeArmR], Quaternion.Identity, out_);
                pose.Rotation[HumanBones.HandR] = Quaternion.Slerp(pose.Rotation[HumanBones.HandR], Quaternion.Identity, out_);
                pose.Turn(HumanBones.Spine, 0f, -0.12f * out_);
                pose.Turn(HumanBones.Head, 0.04f * out_, -0.14f * out_);
                pose.Turn(HumanBones.Hair, 0f, 0f, 0.04f * out_);
                break;
            }
            case Emote.Laugh:
            {
                // The head tipped back, the shoulders shaking with it, the hands at the belly
                float back = Smooth(0.05f, 0.3f, t) * (1f - Smooth(d - 0.35f, d - 0.08f, t));
                float shake = MathF.Abs(MathF.Sin(t * Tau * 3.5f)) * back;
                pose.Turn(HumanBones.Head, -0.42f * back + 0.06f * shake);
                pose.Turn(HumanBones.Spine, -0.14f * back);
                Up(pose, HumanBones.Spine, 0.008f * shake);
                pose.Turn(HumanBones.UpperArmL, -0.3f * back, 0f, 0.05f * back);
                pose.Turn(HumanBones.UpperArmR, -0.3f * back, 0f, -0.05f * back);
                pose.Turn(HumanBones.ForeArmL, -0.75f * back, 0f, -0.25f * back);
                pose.Turn(HumanBones.ForeArmR, -0.75f * back, 0f, 0.25f * back);
                pose.Turn(HumanBones.Hair, -0.15f * back + 0.08f * shake);
                break;
            }
        }
    }

    /// <summary>The Rift floats, its heart pulses and its rings turn.</summary>
    private static void Rift(CharacterPose p, SkeletonPose pose)
    {
        float t = p.Time;
        pose.Offset[0] = new Vector3(0, 0.08f * MathF.Sin(t * 1.7f), 0);
        if (pose.Count < 4) return;
        float pulse = 1f + 0.06f * MathF.Sin(t * 3.1f);
        pose.Scale[1] = new Vector3(pulse);
        pose.Rotation[2] = Quaternion.CreateFromAxisAngle(Vector3.UnitY, t * 1.9f);
        pose.Rotation[3] = Quaternion.CreateFromAxisAngle(Vector3.UnitY, -t * 1.3f);
    }
}
