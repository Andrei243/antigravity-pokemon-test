using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Overworld;
using Stance = PokemonPlatinumEngine.Overworld.Stance;

namespace PokemonPlatinumTests;

/// <summary>
/// The grown kit's animation (plan 30 · FM5; style guide, "Grown animation"): the gait blended by speed with its feet
/// planted at any tilt, the run's blend, turning and the step-turn, the lean, starts and stops, the stances and rides,
/// talking and the emotes on the 26-bone skeleton, and the chibi kit's clips untouched beside them.
/// </summary>
public class GrownAnimationTests
{
    private readonly ITestOutputHelper output;

    public GrownAnimationTests(ITestOutputHelper output) => this.output = output;

    private static readonly Dictionary<string, CharacterRig> Rigs = new();

    private static CharacterRig Rig(string type)
    {
        lock (Rigs)
        {
            if (!Rigs.TryGetValue(type, out var rig)) Rigs[type] = rig = CharacterModels.Build(type);
            return rig;
        }
    }

    private static Matrix4x4[] Posed(CharacterRig rig, CharacterPose p)
    {
        var pose = new SkeletonPose(rig.Skeleton.Count);
        CharacterAnimation.Apply(rig, p, pose);
        var skin = new Matrix4x4[rig.Skeleton.Count];
        rig.Skeleton.Evaluate(pose, skin);
        return skin;
    }

    /// <summary>Where a bone's joint is in a pose: its own joint carried by its parent.</summary>
    private static Vector3 Joint(CharacterRig rig, Matrix4x4[] skin, int bone) =>
        Vector3.Transform(rig.Skeleton[bone].Joint, skin[rig.Skeleton[bone].Parent]);

    private static bool Same(SkeletonPose a, SkeletonPose b)
    {
        for (int i = 0; i < a.Count; i++)
            if (a.Rotation[i] != b.Rotation[i] || a.Offset[i] != b.Offset[i] || a.Scale[i] != b.Scale[i]) return false;
        return true;
    }

    [Fact]
    public void AChibiRigIgnoresEveryGrownInputAndKeepsItsOwnClips()
    {
        var rig = Rig("PLAYER");
        var plain = new CharacterPose { Walk = 0.3f, WalkBlend = 1f, Time = 0.4f, Emote = Emote.Wave, EmoteTime = 0.5f };
        var grownInputs = plain with
        {
            RunBlend = 0.7f, Stride = 2f, Lean = 0.1f, StepTurn = 0.5f, StepTurnAngle = 2f, Start = 0.5f, Stop = 0.5f,
            Talking = true, Riding = Riding.Mount
        };
        var a = new SkeletonPose(rig.Skeleton.Count);
        var b = new SkeletonPose(rig.Skeleton.Count);
        var c = new SkeletonPose(rig.Skeleton.Count);
        CharacterAnimation.Apply(rig, plain, a);
        CharacterAnimation.Apply(rig, grownInputs, b);
        CharacterAnimation.ApplyChibi(rig, plain, c);
        Assert.True(Same(a, b), "a grown input moved a chibi rig");
        Assert.True(Same(a, c), "a chibi rig is posed by the chibi clips");
    }

    [Fact]
    public void AGrownRigIsPosedByItsOwnClipsWhichMoveTheSevenItAppends()
    {
        var rig = Rig("ROWAN~Grown");
        var walk = new CharacterPose { Walk = 0.45f, WalkBlend = 1f, Time = 0.4f };
        var grown = new SkeletonPose(rig.Skeleton.Count);
        var chibi = new SkeletonPose(rig.Skeleton.Count);
        CharacterAnimation.Apply(rig, walk, grown);
        CharacterAnimation.ApplyChibi(rig, walk, chibi);
        Assert.False(Same(grown, chibi));
        // Somewhere in the gait each appended bone turns: the chest against the hips, the neck, the collarbones, the toes as a heel lifts, the coat's tails
        var moved = new HashSet<int>();
        for (int f = 0; f < 16; f++)
        {
            var pose = new SkeletonPose(rig.Skeleton.Count);
            CharacterAnimation.Apply(rig, walk with { Walk = f / 16f }, pose);
            for (int b = HumanBones.Count; b < HumanBones.GrownCount; b++)
                if (MathF.Abs(Quaternion.Dot(pose.Rotation[b], Quaternion.Identity)) < 0.9999f || pose.Offset[b] != Vector3.Zero) moved.Add(b);
        }
        Assert.Equal(Enumerable.Range(HumanBones.Count, HumanBones.GrownCount - HumanBones.Count), moved.OrderBy(b => b));
    }

    [Fact]
    public void TheClipsArePure()
    {
        var rig = Rig("PLAYER~Teen");
        var p = new CharacterPose { Walk = 0.31f, WalkBlend = 0.8f, RunBlend = 0.4f, Lean = 0.05f, Time = 1.7f, Emote = Emote.Bow, EmoteTime = 0.6f, Talking = true };
        var a = new SkeletonPose(rig.Skeleton.Count);
        var b = new SkeletonPose(rig.Skeleton.Count);
        CharacterAnimation.Apply(rig, p, a);
        CharacterAnimation.Apply(rig, p with { Walk = 0.8f, Stance = Stance.Sit }, b);
        CharacterAnimation.Apply(rig, p, b);
        Assert.True(Same(a, b));
    }

    public static IEnumerable<object[]> Tilts => new[] { 0.2f, 0.4f, 0.7f, 1f, 1.3f, 1.6f, 2f }.Select(s => new object[] { s });

    [Theory]
    [MemberData(nameof(Tilts))]
    public void AWalkAtAnyTiltKeepsItsFeetPlanted(float share)
    {
        // Driven at a steady speed by the motion, the body goes on along +Z; while a foot is down its ball must stay
        // where it landed in the world (style guide, "Grown animation": the cycle goes on by the distance over the stride)
        var rig = Rig("PLAYER~Teen");
        var motion = GrownMotion.For(rig);
        float speed = share * motion.WalkPace;
        const float dt = 1f / 60f;
        float z = 0f;
        float ground = rig.Skeleton[HumanBones.ToeL].Joint.Y;
        var planted = new Dictionary<int, (float Z, float Worst)>();
        float worst = 0f;
        int stances = 0;
        for (int frame = 0; frame < 60 * 4; frame++)
        {
            motion.Step(dt, new Vector2(0f, speed));
            z += speed * dt;
            if (frame < 60) continue;
            var p = new CharacterPose { Time = frame * dt };
            motion.Fill(ref p);
            var skin = Posed(rig, p);
            foreach (int toe in new[] { HumanBones.ToeL, HumanBones.ToeR })
            {
                var ball = Joint(rig, skin, toe) + new Vector3(0, 0, z);
                bool down = ball.Y < ground + 0.0015f;
                if (!down) { planted.Remove(toe); continue; }
                if (!planted.TryGetValue(toe, out var at)) { planted[toe] = (ball.Z, 0f); stances++; continue; }
                float slip = MathF.Abs(ball.Z - at.Z);
                worst = MathF.Max(worst, slip);
            }
        }
        output.WriteLine($"{share:0.0} of walking pace ({speed:0.00} a second): stride {motion.Stride:0.000}, run {motion.RunBlend:0.00}, {stances} footfalls, the worst slip {worst:0.0000}");
        Assert.True(stances >= 4, "no foot came down");
        Assert.True(worst < 0.01f, $"a planted foot slid {worst:0.0000} at {share} of walking pace");
    }

    [Fact]
    public void TheRunComesInBetweenOnePointTwoAndOnePointEightTimesWalkingPaceAndTheStrideGrows()
    {
        var motion = new GrownMotion(0.7f);
        float Settle(float share)
        {
            for (int i = 0; i < 120; i++) motion.Step(1f / 60f, new Vector2(0f, share * motion.WalkPace));
            return motion.RunBlend;
        }
        Assert.InRange(Settle(1.15f), 0f, 0.001f);
        float mid = Settle(1.5f);
        Assert.InRange(mid, 0.3f, 0.7f);
        Assert.InRange(Settle(1.85f), 0.999f, 1f);
        Assert.Equal(1.4f * 0.7f, motion.StrideAt(motion.WalkPace), 4);
        Assert.Equal(2.4f * 0.7f, motion.StrideAt(2f * motion.WalkPace), 4);
        Assert.Equal(2.4f * 0.7f, motion.StrideAt(3f * motion.WalkPace), 4);
        Assert.True(motion.StrideAt(0.5f * motion.WalkPace) < motion.StrideAt(motion.WalkPace));
        // Walking blends in fully by an eighth of walking pace, and standing is none of it
        var slow = new GrownMotion(0.7f);
        for (int i = 0; i < 60; i++) slow.Step(1f / 60f, new Vector2(0.15f * slow.WalkPace, 0f));
        Assert.True(slow.WalkBlend > 0.99f);
        for (int i = 0; i < 60; i++) slow.Step(1f / 60f, Vector2.Zero);
        Assert.True(slow.WalkBlend < 0.01f);
    }

    [Fact]
    public void TheYawEasesTowardTheWayGoneAndARunCurves()
    {
        float Eased(float share)
        {
            var m = new GrownMotion(0.7f);
            for (int i = 0; i < 120; i++) m.Step(1f / 60f, new Vector2(0f, share * m.WalkPace));
            // A quarter turn to the left (toward +X) at the same speed, a tenth of a second on
            for (int i = 0; i < 6; i++) m.Step(1f / 60f, new Vector2(share * m.WalkPace, 0f));
            return m.Yaw;
        }
        float walking = Eased(1f), running = Eased(2f);
        float quarter = MathF.PI / 2f;
        output.WriteLine($"a tenth of a second into a quarter turn: walking {walking:0.000}, running {running:0.000}");
        Assert.InRange(walking, quarter * (1f - MathF.Exp(-18f * 0.1f)) - 0.05f, quarter);
        Assert.True(running < walking - 0.3f, "a run turns slower than a walk");
        Assert.True(running > 0f);
    }

    [Fact]
    public void FromStandingATurnWiderThanAHundredAndTwentyDegreesIsAStepTurnOfAQuarterSecond()
    {
        var m = new GrownMotion(0.7f);
        float target = 150f * MathF.PI / 180f;
        var p = new CharacterPose();
        int frames = 0;
        m.Step(1f / 60f, Vector2.Zero, target);
        while (m.Turning && frames < 100)
        {
            m.Fill(ref p);
            Assert.InRange(p.StepTurn, 1e-5f, 1f);
            Assert.Equal(target, p.StepTurnAngle, 4);
            m.Step(1f / 60f, Vector2.Zero, target);
            frames++;
        }
        Assert.InRange(frames, 13, 16);
        Assert.Equal(target, m.Yaw, 3);
        m.Fill(ref p);
        Assert.Equal(0f, p.StepTurn);

        // A turn of a hundred degrees from standing eases round, with no step-turn
        var easy = new GrownMotion(0.7f);
        easy.Step(1f / 60f, Vector2.Zero, 100f * MathF.PI / 180f);
        Assert.False(easy.Turning);
        Assert.True(easy.Yaw > 0f);

        // The step-turn's clip leads with the head and lifts each foot in turn; the yaw itself is the motion's
        var rig = Rig("PLAYER~Teen");
        float ground = rig.Skeleton[HumanBones.ToeL].Joint.Y;
        var first = Posed(rig, new CharacterPose { Time = 0.4f, StepTurn = 0.25f, StepTurnAngle = target });
        var second = Posed(rig, new CharacterPose { Time = 0.4f, StepTurn = 0.75f, StepTurnAngle = target });
        Assert.True(Joint(rig, first, HumanBones.ToeL).Y > ground + 0.02f, "turning left, the left foot steps first");
        Assert.True(Joint(rig, first, HumanBones.ToeR).Y < ground + 0.005f);
        Assert.True(Joint(rig, second, HumanBones.ToeR).Y > ground + 0.02f, "then the right");
    }

    [Fact]
    public void TheBodyLeansIntoATurnByItsSidewaysAccelerationAtMostEightDegrees()
    {
        float Circle(float share, float yawRate)
        {
            var m = new GrownMotion(0.7f);
            float heading = 0f;
            for (int i = 0; i < 180; i++)
            {
                heading += yawRate / 60f;
                float v = share * m.WalkPace;
                m.Step(1f / 60f, new Vector2(v * MathF.Sin(heading), v * MathF.Cos(heading)));
            }
            return m.Lean;
        }
        float gentle = Circle(1f, 0.5f), sharp = Circle(2f, 3f), right = Circle(1f, -0.5f), straight = Circle(1f, 0f);
        output.WriteLine($"lean: gentle left {gentle:0.000}, sharp left running {sharp:0.000}, gentle right {right:0.000}");
        // a = v ω, the lean atan(a / g)
        var walkPace = new GrownMotion(0.7f).WalkPace;
        Assert.Equal(MathF.Atan(walkPace * 0.5f / GrownMotion.Gravity), gentle, 2);
        Assert.Equal(-gentle, right, 3);
        Assert.Equal(GrownMotion.MaxLean, sharp, 4);
        Assert.Equal(0f, straight, 4);

        // Leaning, the feet stay flat on the ground under the body
        var rig = Rig("PLAYER~Teen");
        float ground = rig.Skeleton[HumanBones.ToeL].Joint.Y;
        var skin = Posed(rig, new CharacterPose { Time = 0.4f, Lean = GrownMotion.MaxLean });
        foreach (int toe in new[] { HumanBones.ToeL, HumanBones.ToeR })
            Assert.InRange(Joint(rig, skin, toe).Y, ground - 0.003f, ground + 0.003f);
        Assert.True(Joint(rig, skin, HumanBones.Head).X > rig.Skeleton[HumanBones.Head].Joint.X + 0.1f, "leaning left, the head goes left");
    }

    [Fact]
    public void StartsAndStopsTakeLessThanAFifthOfASecond()
    {
        Assert.True(GrownMotion.StartSeconds < 0.2f && GrownMotion.StopSeconds < 0.2f);
        var m = new GrownMotion(0.7f);
        var p = new CharacterPose();
        m.Step(1f / 60f, new Vector2(0, m.WalkPace));
        m.Fill(ref p);
        Assert.True(p.Start > 0f);
        int frames = 1;
        while (p.Start > 0f && frames < 60) { m.Step(1f / 60f, new Vector2(0, m.WalkPace)); m.Fill(ref p); frames++; }
        Assert.InRange(frames, 10, 12);
        for (int i = 0; i < 60; i++) m.Step(1f / 60f, new Vector2(0, m.WalkPace));
        m.Step(1f / 60f, Vector2.Zero);
        m.Fill(ref p);
        Assert.True(p.Stop > 0f);
        frames = 1;
        while (p.Stop > 0f && frames < 60) { m.Step(1f / 60f, Vector2.Zero); m.Fill(ref p); frames++; }
        Assert.InRange(frames, 10, 12);

        // Neither lifts a foot: they shift the body over its feet
        var rig = Rig("PLAYER~Teen");
        float ground = rig.Skeleton[HumanBones.ToeL].Joint.Y;
        foreach (var pose in new[] { new CharacterPose { Time = 0.4f, Start = 0.5f }, new CharacterPose { Time = 0.4f, Stop = 0.5f } })
        {
            var skin = Posed(rig, pose);
            foreach (int toe in new[] { HumanBones.ToeL, HumanBones.ToeR })
                Assert.InRange(Joint(rig, skin, toe).Y, ground - 0.003f, ground + 0.003f);
        }
    }

    [Theory]
    [InlineData("PLAYER~Teen")]
    [InlineData("BLACKBELT~GrownBroad")]
    [InlineData("YOUNGSTER~Child")]
    [InlineData("OLD_MAN~GrownElder")]
    public void StandingBreathingAndBowingNeverMoveAFoot(string type)
    {
        var rig = Rig(type);
        float ground = rig.Skeleton[HumanBones.ToeL].Joint.Y;
        var rest = new[] { rig.Skeleton[HumanBones.ToeL].Joint, rig.Skeleton[HumanBones.ToeR].Joint };
        foreach (float t in new[] { 0.4f, 1.3f, 2.9f })
            foreach (var pose in new[] { new CharacterPose { Time = t }, new CharacterPose { Time = t, Emote = Emote.Bow, EmoteTime = 0.6f }, new CharacterPose { Time = t, Talking = true } })
            {
                var skin = Posed(rig, pose);
                for (int i = 0; i < 2; i++)
                    Assert.True(Vector3.Distance(Joint(rig, skin, i == 0 ? HumanBones.ToeL : HumanBones.ToeR), rest[i]) < 0.004f, $"{type}: a foot moved standing");
            }
    }

    [Fact]
    public void EveryGrownEmoteStartsAndEndsAtRest()
    {
        var rig = Rig("PLAYER~Teen");
        var idle = new SkeletonPose(rig.Skeleton.Count);
        CharacterAnimation.Apply(rig, new CharacterPose { Time = 0.4f }, idle);
        foreach (var emote in Enum.GetValues<Emote>().Where(e => e != Emote.None))
        {
            float d = CharacterAnimation.Duration(emote);
            var mid = new SkeletonPose(rig.Skeleton.Count);
            CharacterAnimation.Apply(rig, new CharacterPose { Time = 0.4f, Emote = emote, EmoteTime = 0.5f * d }, mid);
            Assert.False(Same(mid, idle), $"{emote} does nothing");
            foreach (float t in new[] { 0f, d })
            {
                var pose = new SkeletonPose(rig.Skeleton.Count);
                CharacterAnimation.Apply(rig, new CharacterPose { Time = 0.4f, Emote = emote, EmoteTime = t }, pose);
                for (int b = 0; b < pose.Count; b++)
                    Assert.True(MathF.Abs(Quaternion.Dot(pose.Rotation[b], idle.Rotation[b])) > 0.9999f, $"{emote} at {t}s: bone {b} is not at rest");
            }
        }

        // The wave's hand is over the shoulder, the point's straight ahead at the shoulder's height, the throw lets go over the head
        float shoulder = rig.Skeleton[HumanBones.UpperArmR].Joint.Y;
        var wave = Posed(rig, new CharacterPose { Time = 0.4f, Emote = Emote.Wave, EmoteTime = 0.8f });
        Assert.True(Joint(rig, wave, HumanBones.HandR).Y > shoulder + 0.1f, "the waving hand is over the shoulder");
        var point = Posed(rig, new CharacterPose { Time = 0.4f, Emote = Emote.Point, EmoteTime = 0.6f });
        var hand = Joint(rig, point, HumanBones.HandR);
        Assert.InRange(hand.Y, shoulder - 0.12f, shoulder + 0.08f);
        Assert.True(hand.Z > 0.35f, "the pointing hand is out ahead");
        var throwing = Posed(rig, new CharacterPose { Time = 0.4f, Emote = Emote.Throw, EmoteTime = CharacterAnimation.ThrowRelease });
        Assert.True(Joint(rig, throwing, HumanBones.HandR).Y > shoulder, "the ball goes from over the shoulder");
    }

    [Fact]
    public void TheSitterSitsOnTheSeatWithItsFeetOnTheFloorAndTheFieldsSeatsAreThatHigh()
    {
        foreach (string type in new[] { "PLAYER~Teen", "CLERK~Grown", "BLACKBELT~GrownBroad", "YOUNGSTER~Child" })
        {
            var rig = Rig(type);
            float ground = rig.Skeleton[HumanBones.ToeL].Joint.Y;
            var skin = Posed(rig, new CharacterPose { Time = 0.4f, Stance = Stance.Sit });
            float seat = GrownAnimation.SeatOf(rig.Skeleton);
            float hip = Joint(rig, skin, HumanBones.ThighL).Y;
            output.WriteLine($"{type}: seat {seat:0.000}, hip joints at {hip:0.000}, knee at {Joint(rig, skin, HumanBones.ShinL).Y:0.000}");
            Assert.InRange(hip - seat, 0.03f, 0.12f);
            foreach (int toe in new[] { HumanBones.ToeL, HumanBones.ToeR })
                Assert.InRange(Joint(rig, skin, toe).Y, ground - 0.004f, ground + 0.004f);
            // The knees out ahead of the hips, the thigh near level
            var knee = Joint(rig, skin, HumanBones.ShinL);
            output.WriteLine($"  knee {knee}, hip joint {Joint(rig, skin, HumanBones.ThighL)}");
            Assert.True(knee.Z > Joint(rig, skin, HumanBones.ThighL).Z + 0.6f * GrownAnimation.LegsOf(rig.Skeleton).Thigh);
        }
        // A grown region's chair is the Grown build's seat at the field's size (1.6 tiles to its 1.585 crown), within a texel
        float grown = GrownAnimation.SeatShare * CharacterModels.GrownFrameOf(BodyBuild.Grown).HipY * 1.6f / 1.585f;
        Assert.InRange(Seats.GrownHeightOf(PropType.Chair), grown - 1f / 32f, grown + 1f / 32f);
        Assert.True(Seats.GrownHeightOf(PropType.Bench) <= Seats.GrownHeightOf(PropType.Chair));
        Assert.True(Seats.GrownHeightOf(PropType.Cushion) < Seats.GrownHeightOf(PropType.Bench));
        Assert.Equal(0f, Seats.GrownHeightOf(PropType.LampPost));
    }

    [Fact]
    public void TheRidesAndTalkingPoseWhatTheyShould()
    {
        var rig = Rig("PLAYER~Teen");
        var rest = new SkeletonPose(rig.Skeleton.Count);
        CharacterAnimation.Apply(rig, new CharacterPose { Time = 0.4f }, rest);

        // On a Bicycle the feet go round with the cycle
        var a = Posed(rig, new CharacterPose { Time = 0.4f, Riding = Riding.Bicycle, Walk = 0f, WalkBlend = 1f });
        var b = Posed(rig, new CharacterPose { Time = 0.4f, Riding = Riding.Bicycle, Walk = 0.25f, WalkBlend = 1f });
        Assert.True(Vector3.Distance(Joint(rig, a, HumanBones.FootL), Joint(rig, b, HumanBones.FootL)) > 0.05f);
        // Astride a mount the body crouches lower over it as it runs
        var trot = Posed(rig, new CharacterPose { Time = 0.4f, Riding = Riding.Mount });
        var sprint = Posed(rig, new CharacterPose { Time = 0.4f, Riding = Riding.Mount, RunBlend = 1f });
        output.WriteLine($"the head astride: {Joint(rig, trot, HumanBones.Head).Y:0.000} trotting, {Joint(rig, sprint, HumanBones.Head).Y:0.000} sprinting");
        Assert.True(Joint(rig, sprint, HumanBones.Head).Y < Joint(rig, trot, HumanBones.Head).Y - 0.03f);
        Assert.True(Joint(rig, trot, HumanBones.ShinL).X > rig.Skeleton[HumanBones.ShinL].Joint.X + 0.06f, "the knees go out round the mount's back");
        // Surfing, kneeling: the knees down near what is ridden, the hips over them
        var surf = Posed(rig, new CharacterPose { Time = 0.4f, Riding = Riding.Surf });
        Assert.InRange(Joint(rig, surf, HumanBones.ShinL).Y, -0.02f, 0.12f);
        // Talking raises the right forearm before the body
        var talk = Posed(rig, new CharacterPose { Time = 0.4f, Talking = true });
        var still = Posed(rig, new CharacterPose { Time = 0.4f });
        Assert.True(Joint(rig, talk, HumanBones.HandR).Y > Joint(rig, still, HumanBones.HandR).Y + 0.1f);
        Assert.True(Joint(rig, talk, HumanBones.HandR).Z > Joint(rig, still, HumanBones.HandR).Z + 0.08f);
    }

    // The clips that raise an arm to the shoulder's height or over it. Since plan 30 · FM10 the shoulder's weight reaches
    // down the chest (GrownShoulderReach, GrownSleeveReach) and the body and its outline stretch nowhere; a garment's deep
    // crease under the arm still stretches a few edges past four times at the top of a throw, a wave or a cheer (linear
    // skinning's own limit there), which the test holds under MostAtTheArmpit for each clip, and nothing else may tear
    private static bool ArmsUp(string clip) => clip is "cheer" or "wave" or "throw" or "hop" or "bicycle" or "mount" or "surf";

    /// <summary>The most edges of a garment or its outline the armpit may stretch in one clip (FM5 found up to 250).</summary>
    private const int MostAtTheArmpit = 60;

    private static bool AtTheArmpit(CharacterRig rig, Vector3 p)
    {
        foreach (int arm in new[] { HumanBones.UpperArmL, HumanBones.UpperArmR })
        {
            var shoulder = rig.Skeleton[arm].Joint;
            if (Vector3.Distance(p, shoulder) < 0.28f && p.Y < shoulder.Y + 0.04f && MathF.Abs(p.X) > 0.4f * MathF.Abs(shoulder.X)) return true;
        }
        return false;
    }

    /// <summary>A mesh's vertices and normals as a pose carries them.</summary>
    private static (Vector3[] At, Vector3[] Normal) Skinned(SdfMesh mesh, Matrix4x4[] skin)
    {
        var at = new Vector3[mesh.VertexCount];
        var normal = new Vector3[mesh.VertexCount];
        for (int v = 0; v < mesh.VertexCount; v++)
        {
            for (int k = 0; k < 4; k++)
            {
                float w = mesh.BoneWeights[v * 4 + k];
                if (w == 0f) continue;
                var m = skin[mesh.BoneIndices[v * 4 + k]];
                at[v] += Vector3.Transform(mesh.Positions[v], m) * w;
                normal[v] += Vector3.TransformNormal(mesh.Normals[v], m) * w;
            }
            if (normal[v] != Vector3.Zero) normal[v] = Vector3.Normalize(normal[v]);
        }
        return (at, normal);
    }

    /// <summary>The bone a vertex of a mesh follows most.</summary>
    private static int Heaviest(SdfMesh mesh, int v)
    {
        int best = mesh.BoneIndices[v * 4];
        float most = -1f;
        for (int k = 0; k < 4; k++)
            if (mesh.BoneWeights[v * 4 + k] > most) { most = mesh.BoneWeights[v * 4 + k]; best = mesh.BoneIndices[v * 4 + k]; }
        return best;
    }

    /// <summary>How far a thigh may stand out of a skirt and not be seen: half the garment's outline, a few millimetres.</summary>
    private const float Through = CharacterModels.GrownOutlineThickness / 2f;

    [Theory]
    [InlineData("LASS~Teen")]
    [InlineData("SERENA~Teen")]
    [InlineData("NURSE~Grown")]
    [InlineData("MALVA~Grown")]
    public void TheThighsStayUnderASkirtThroughAStride(string type)
    {
        // Plan 30 · FM10 (FM5's finding (2)): at the widest of a walk's and a run's stride the forward thigh came out
        // through a skirt's side. No vertex of a thigh between the skirt's hem and the hips stands out of the skirt
        var rig = Rig(type);
        var clothes = rig.Clothes!;
        var skirt = Enumerable.Range(0, clothes.VertexCount).Where(v => Heaviest(clothes, v) == HumanBones.Skirt).ToArray();
        Assert.NotEmpty(skirt);
        float hem = skirt.Min(v => clothes.Positions[v].Y), hip = rig.Skeleton[HumanBones.ThighL].Joint.Y;
        var body = rig.Mesh;
        var thighs = Enumerable.Range(0, body.VertexCount)
            .Where(v => Heaviest(body, v) is HumanBones.ThighL or HumanBones.ThighR && body.Positions[v].Y > hem + 0.03f && body.Positions[v].Y < hip - 0.02f).ToArray();
        int worst = 0;
        string at = "";
        foreach (float run in new[] { -1f, 0f, 1f })
            for (int step = 0; step < (run < 0f ? 1 : 8); step++)
            {
                // (standing first: the skirt over the thighs as it was sculpted)
                var skin = Posed(rig, run < 0f ? new CharacterPose { Time = 0.4f } : new CharacterPose { Walk = step / 8f, WalkBlend = 1f, RunBlend = run, Time = 0.4f });
                var (bodyAt, _) = Skinned(body, skin);
                var (clothAt, clothNormal) = Skinned(clothes, skin);
                int out_ = 0, deep = 0;
                float deepest = 0f;
                foreach (int v in thighs)
                {
                    var p = bodyAt[v];
                    int near = skirt[0];
                    float best = float.MaxValue;
                    foreach (int c in skirt)
                    {
                        float d = Vector3.DistanceSquared(p, clothAt[c]);
                        if (d < best) { best = d; near = c; }
                    }
                    // A thigh seen below the hem (nearest the hem's cut face or its rim) is no thigh through the skirt: only
                    // where the skirt's side is nearest, a little above its hem, does it count
                    float by = Vector3.Dot(p - clothAt[near], clothNormal[near]);
                    if (clothNormal[near].Y > -0.7f && clothes.Positions[near].Y > hem + 0.02f && by > Through)
                    {
                        out_++;
                        deepest = Math.Max(deepest, by);
                        if (by > CharacterModels.GrownOutlineThickness) deep++;
                    }
                }
                if (out_ > 0) output.WriteLine($"  {(run < 0f ? "standing" : run > 0f ? "run" : "walk")} {step}/8: {out_} out, {deep} deeper than an outline, the deepest {deepest:0.000}");
                if (out_ > worst) { worst = out_; at = run < 0f ? "standing" : $"{(run > 0f ? "run" : "walk")} at {step}/8"; }
            }
        output.WriteLine($"{type}: {thighs.Length} vertices of the thighs under the skirt; at worst {worst} out of it ({at})");
        Assert.True(worst == 0, $"{worst} vertices of a thigh stand out of the skirt in the {at}");
    }

    public static IEnumerable<object[]> Looks => new[] { "PLAYER~Teen", "ROWAN~Grown", "LASS~Teen", "BLACKBELT~GrownBroad", "YOUNGSTER~Child", "OLD_MAN~GrownElder" }.Select(t => new object[] { t });

    [Theory]
    [MemberData(nameof(Looks))]
    public void NothingTearsInTheGrownClips(string type)
    {
        // As GrownKitTests holds of the chibi clips, and now sitting too: no edge of the body, its outline, the clothes or
        // their outline grows past four times its length in any of the grown clips
        var rig = Rig(type);
        (string Name, CharacterPose Pose)[] clips =
        {
            ("walk", new CharacterPose { Walk = 0.2f, WalkBlend = 1f, Time = 0.4f }), ("walk back", new CharacterPose { Walk = 0.7f, WalkBlend = 1f, Time = 0.4f }),
            ("run", new CharacterPose { Walk = 0.2f, WalkBlend = 1f, RunBlend = 1f, Time = 0.4f }), ("run back", new CharacterPose { Walk = 0.7f, WalkBlend = 1f, RunBlend = 1f, Time = 0.4f }),
            ("bow", new CharacterPose { Emote = Emote.Bow, EmoteTime = 0.6f, Time = 0.4f }),
            ("sit", new CharacterPose { Stance = Stance.Sit, Time = 0.4f }), ("sit on the ground", new CharacterPose { Stance = Stance.SitGround, Time = 0.4f }),
            ("step-turn", new CharacterPose { StepTurn = 0.25f, StepTurnAngle = 2.5f, Time = 0.4f }), ("hop", new CharacterPose { Hop = 0.5f, Time = 0.4f }),
            ("surf", new CharacterPose { Riding = Riding.Surf, Time = 0.4f }), ("bicycle", new CharacterPose { Riding = Riding.Bicycle, Walk = 0.3f, Time = 0.4f }),
            ("mount", new CharacterPose { Riding = Riding.Mount, RunBlend = 1f, Time = 0.4f }), ("cheer", new CharacterPose { Emote = Emote.Cheer, EmoteTime = 0.7f, Time = 0.4f }),
            ("wave", new CharacterPose { Emote = Emote.Wave, EmoteTime = 0.8f, Time = 0.4f }), ("throw", new CharacterPose { Emote = Emote.Throw, EmoteTime = CharacterAnimation.ThrowRelease, Time = 0.4f })
        };
        var parts = new List<(string, SdfMesh)> { ("body", rig.Mesh), ("outline", rig.Shell) };
        if (rig.Clothes is { } clothes) parts.Add(("clothes", clothes));
        if (rig.ClothesShell is { } clothesShell) parts.Add(("clothes' outline", clothesShell));
        var failures = new List<string>();
        foreach (var (name, pose) in clips)
        {
            var skin = Posed(rig, pose);
            foreach (var (part, mesh) in parts)
            {
                var at = new Vector3[mesh.VertexCount];
                for (int v = 0; v < mesh.VertexCount; v++)
                    for (int k = 0; k < 4; k++)
                        at[v] += Vector3.Transform(mesh.Positions[v], skin[mesh.BoneIndices[v * 4 + k]]) * mesh.BoneWeights[v * 4 + k];
                int torn = 0, armpit = 0;
                Vector3 where = default;
                for (int i = 0; i < mesh.Indices.Length; i += 3)
                    for (int k = 0; k < 3; k++)
                    {
                        int a = mesh.Indices[i + k], b = mesh.Indices[i + (k + 1) % 3];
                        float now = Vector3.Distance(at[a], at[b]);
                        if (now <= 0.02f || now <= 4f * Vector3.Distance(mesh.Positions[a], mesh.Positions[b])) continue;
                        if (part.StartsWith("clothes", StringComparison.Ordinal) && ArmsUp(name) && AtTheArmpit(rig, mesh.Positions[a]) && AtTheArmpit(rig, mesh.Positions[b])) armpit++;
                        else { torn++; where = mesh.Positions[a]; }
                    }
                if (armpit > 0) output.WriteLine($"{type}: {armpit} edges of the {part} stretch at the armpit in the {name}");
                if (armpit > MostAtTheArmpit) failures.Add($"{armpit} edges of the {part} stretch at the armpit in the {name}, more than {MostAtTheArmpit}");
                if (torn > 0) failures.Add($"{torn} edges of the {part} in the {name}, one at {where}");
            }
        }
        foreach (var f in failures) output.WriteLine($"{type}: {f}");
        Assert.Empty(failures);
    }
}
