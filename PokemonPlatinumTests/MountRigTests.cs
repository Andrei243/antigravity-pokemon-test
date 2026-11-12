using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Graphics.OpenWorld;
using PokemonPlatinumEngine.OpenWorld;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// The mount as a rig (plan 30 · FM13; style guide, "Open world", "The mount"; rulings, "The mount as a rig"): the
/// ride forms ridden, the seat found on each of the eight forms' own meshes, the rider astride clear of the mount's
/// flanks, getting on and off, a change of form, how the mount stands gliding, swimming and climbing, and the orbit
/// camera over the mount, which on foot is the walker's to the bit. GPU-free: the models are meshed on the CPU.
/// </summary>
public class MountRigTests
{
    private readonly ITestOutputHelper output;

    public MountRigTests(ITestOutputHelper output) => this.output = output;

    private const float Frame = 1f / 60f;

    private static readonly string[] EightForms =
    {
        "Koraidon-Limited-Build", "Koraidon-Sprinting-Build", "Koraidon-Swimming-Build", "Koraidon-Gliding-Build",
        "Miraidon-Low-Power-Mode", "Miraidon-Drive-Mode", "Miraidon-Aquatic-Mode", "Miraidon-Glide-Mode"
    };

    private static readonly object RiderLock = new();
    private static CharacterRig? rider;

    private static CharacterRig Rider()
    {
        lock (RiderLock) return rider ??= CharacterModels.Build("PLAYER~Teen");
    }

    private static MountInput Input(bool mounted, OpenStance stance, Vector3 feet, double time, int mount = MountRules.Koraidon, (float, float) cling = default) =>
        new(mounted, mount, stance, feet, 0f, cling, time);

    /// <summary>The rig stepped a number of frames, the feet moving east at a speed, returning the last frame.</summary>
    private static MountFrame? Run(MountRig rig, int frames, OpenStance stance, ref Vector3 feet, ref double time, float speed = 0f, bool mounted = true, int mount = MountRules.Koraidon, (float, float) cling = default)
    {
        MountFrame? last = null;
        for (int f = 0; f < frames; f++)
        {
            feet += new Vector3(speed * Frame, 0f, 0f);
            time += Frame;
            last = rig.Step(Frame, Input(mounted, stance, feet, time, mount, cling));
        }
        return last;
    }

    [Fact]
    public void RiddenTheMountIsItsRunningFormAndItsLimitedFormOnlyOffTheSaddle()
    {
        foreach (var (mount, running, swim, glide, limited) in new[]
        {
            (MountRules.Koraidon, "Koraidon-Sprinting-Build", "Koraidon-Swimming-Build", "Koraidon-Gliding-Build", "Koraidon-Limited-Build"),
            (MountRules.Miraidon, "Miraidon-Drive-Mode", "Miraidon-Aquatic-Mode", "Miraidon-Glide-Mode", "Miraidon-Low-Power-Mode")
        })
        {
            Assert.Equal(running, MountRules.FormFor(mount, OpenStance.Standing, ridden: true));
            Assert.Equal(running, MountRules.FormFor(mount, OpenStance.Falling, ridden: true));
            Assert.Equal(running, MountRules.FormFor(mount, OpenStance.Climbing, ridden: true));
            Assert.Equal(swim, MountRules.FormFor(mount, OpenStance.Swimming, ridden: true));
            Assert.Equal(glide, MountRules.FormFor(mount, OpenStance.Gliding, ridden: true));
            foreach (var stance in Enum.GetValues<OpenStance>())
                Assert.Equal(limited, MountRules.FormFor(mount, stance, ridden: false));
        }
        // Each a seat line of its own
        Assert.Equal(EightForms.OrderBy(f => f), MountLooks.SeatLines.Keys.OrderBy(f => f));
    }

    [Fact]
    public void EveryRideFormHasASeatOnTheTopOfItsOwnBody()
    {
        foreach (string form in EightForms)
        {
            var model = PokemonModels.Get(form);
            var seat = MountLooks.SeatOf(model);
            int mount = form.StartsWith("Koraidon") ? MountRules.Koraidon : MountRules.Miraidon;
            float metres = MountRig.MetresPerUnit(mount);
            output.WriteLine($"{form}: seat {seat.Rest} ({seat.Rest.Y * metres:0.00} m up), half width {seat.HalfWidth * metres:0.00} m, top {model.Height:0.000}");
            Assert.Equal(model.Find(PokeRole.Body), seat.Bone);
            Assert.InRange(seat.Rest.Y, 0.25f * model.Height, model.Height);
            Assert.InRange(seat.HalfWidth, 0.03f, 0.3f);
            // Nothing of the body stands over the seat on its column
            Assert.DoesNotContain(model.Mesh.Positions.Select((p, v) => (p, v)), pv =>
                MathF.Abs(pv.p.X) <= MountLooks.SeatColumn && MathF.Abs(pv.p.Z - seat.Rest.Z) <= MountLooks.SeatAlong
                && pv.p.Y > seat.Rest.Y + 1e-5f && pv.p.Y <= MountLooks.SeatCeilings.GetValueOrDefault(form, float.PositiveInfinity)
                && model.Mesh.WeightOf(pv.v, seat.Bone) >= 0.5f);
            // A riding form's back stands about a grown hip's height off the ground
            if (!form.Contains("Limited") && !form.Contains("Low-Power"))
                Assert.InRange(seat.Rest.Y * metres, form.Contains("Swim") || form.Contains("Aquatic") ? 0.55f : 0.85f, 1.5f);
        }
    }

    public static IEnumerable<object[]> RiddenForms => new[]
    {
        new object[] { "Koraidon-Sprinting-Build", "Seated" }, new object[] { "Koraidon-Swimming-Build", "Swim" },
        new object[] { "Koraidon-Gliding-Build", "Glide" }, new object[] { "Miraidon-Drive-Mode", "Dash" },
        new object[] { "Miraidon-Aquatic-Mode", "Swim" }, new object[] { "Miraidon-Glide-Mode", "Glide" }
    };

    [Theory]
    [MemberData(nameof(RiddenForms))]
    public void TheRidersLegsClearTheMountsFlanks(string form, string stanceName)
    {
        var stance = Enum.Parse<RideStance>(stanceName);
        var model = PokemonModels.Get(form);
        var seat = MountLooks.SeatOf(model);
        float metres = MountRig.MetresPerUnit(form.StartsWith("Koraidon") ? MountRules.Koraidon : MountRules.Miraidon);
        var rig = Rider();

        // The mount's flanks: its body's half width in bins of 4 cm along its length and up, in metres
        const float Bin = 0.04f;
        var flank = new Dictionary<(int, int), float>();
        var mesh = model.Mesh;
        for (int v = 0; v < mesh.VertexCount; v++)
        {
            if (mesh.WeightOf(v, seat.Bone) < 0.5f) continue;
            var p = mesh.Positions[v] * metres;
            var key = ((int)MathF.Round(p.Z / Bin), (int)MathF.Round(p.Y / Bin));
            flank[key] = MathF.Max(flank.GetValueOrDefault(key), MathF.Abs(p.X));
        }

        // The rider astride, its root under the seat by its saddle, posed by the stance with the mount's width
        var pose = new CharacterPose { Riding = Riding.Mount, RideStance = stance, Astride = seat.HalfWidth * metres / rig.Scale, Time = 0.4f };
        var skeleton = new SkeletonPose(rig.Skeleton.Count);
        CharacterAnimation.Apply(rig, pose, skeleton);
        var skin = new Matrix4x4[rig.Skeleton.Count];
        rig.Skeleton.Evaluate(skeleton, skin);
        float saddle = GrownAnimation.SaddleOf(rig.Skeleton) * rig.Scale;
        var root = Matrix4x4.CreateScale(rig.Scale) * Matrix4x4.CreateTranslation(seat.Rest * metres - new Vector3(0f, saddle, 0f));
        int[] legBones = { HumanBones.ThighL, HumanBones.ShinL, HumanBones.ThighR, HumanBones.ShinR };
        var body = rig.Mesh;
        int considered = 0, inside = 0;
        float worst = 0f;
        for (int v = 0; v < body.VertexCount; v++)
        {
            int strongest = -1;
            float most = 0f;
            for (int k = 0; k < 4; k++)
                if (body.BoneWeights[v * 4 + k] > most) { most = body.BoneWeights[v * 4 + k]; strongest = body.BoneIndices[v * 4 + k]; }
            if (Array.IndexOf(legBones, strongest) < 0) continue;
            var skinned = Vector3.Zero;
            for (int k = 0; k < 4; k++)
                skinned += Vector3.Transform(body.Positions[v], skin[body.BoneIndices[v * 4 + k]]) * body.BoneWeights[v * 4 + k];
            var p = Vector3.Transform(skinned, root);
            // What sits on the back is the saddle; what hangs down its sides must be outside it
            if (p.Y > seat.Rest.Y * metres - 0.08f) continue;
            if (!flank.TryGetValue(((int)MathF.Round(p.Z / Bin), (int)MathF.Round(p.Y / Bin)), out float half)) continue;
            considered++;
            float into = half - MathF.Abs(p.X);
            if (into > 0.03f) { inside++; worst = MathF.Max(worst, into); }
        }
        output.WriteLine($"{form} ({stance}): {considered} leg vertices beside the flanks, {inside} inside them (at worst {worst * 100:0.0} cm); spread {GrownAnimation.AstrideSpread(rig.Skeleton, GrownAnimation.LegsOf(rig.Skeleton), pose.Astride):0.00}");
        // (swimming, the rider kneels on its back, so no leg need hang beside it)
        Assert.True(considered > 0 || stance == RideStance.Swim, "no leg hangs beside the mount");
        Assert.True(inside <= considered / 50 || worst < 0.06f, $"{inside} of {considered} leg vertices inside the mount's flanks, at worst {worst * 100:0.0} cm");
    }

    [Fact]
    public void AstrideAtNoughtIsTheOldSpreadAndAWiderMountSpreadsTheThighsMore()
    {
        var rig = Rider();
        SkeletonPose Pose(CharacterPose p)
        {
            var s = new SkeletonPose(rig.Skeleton.Count);
            CharacterAnimation.Apply(rig, p, s);
            return s;
        }
        var old = Pose(new CharacterPose { Riding = Riding.Mount, Time = 0.4f });
        var seated = Pose(new CharacterPose { Riding = Riding.Mount, RideStance = RideStance.Seated, Astride = 0f, Time = 0.4f });
        for (int b = 0; b < old.Count; b++)
        {
            Assert.Equal(old.Rotation[b], seated.Rotation[b]);
            Assert.Equal(old.Offset[b], seated.Offset[b]);
        }
        var legs = GrownAnimation.LegsOf(rig.Skeleton);
        Assert.Equal(0.34f, GrownAnimation.AstrideSpread(rig.Skeleton, legs, 0.01f));
        Assert.True(GrownAnimation.AstrideSpread(rig.Skeleton, legs, 0.3f) > GrownAnimation.AstrideSpread(rig.Skeleton, legs, 0.2f));
        // Each stance changes the seat's pose
        foreach (var stance in new[] { RideStance.Dash, RideStance.Swim, RideStance.Glide, RideStance.Climb })
        {
            var p = Pose(new CharacterPose { Riding = Riding.Mount, RideStance = stance, Time = 0.4f });
            Assert.Contains(Enumerable.Range(0, old.Count), b => p.Rotation[b] != old.Rotation[b] || p.Offset[b] != old.Offset[b]);
        }
    }

    [Fact]
    public void OnFootNothingOfTheMountIsDrawnAndGettingOnBringsItOutOfItsBall()
    {
        var rig = new MountRig();
        var feet = new Vector3(10f, 2f, -4f);
        double time = 0;
        Assert.Null(Run(rig, 10, OpenStance.Standing, ref feet, ref time, mounted: false));

        // The first frame: the limited form at half its size, white, the rider on the ground beside it
        time += Frame;
        var first = rig.Step(Frame, Input(true, OpenStance.Standing, feet, time))!.Value;
        Assert.Equal("Koraidon-Limited-Build", first.Form);
        Assert.Equal(MountRig.MetresPerUnit(MountRules.Koraidon) * 0.5f, first.Scale, 4);
        Assert.Equal(1f, first.Flash, 4);
        Assert.Equal(0f, first.Seated);
        Assert.Equal(MountRig.Beside, Vector3.Distance(first.Ground, feet), 3);

        // Ridden from a fifth of a second: a change into the running form, swapped at its middle
        var forms = new List<string>();
        MountFrame last = first;
        for (int f = 0; f < 50; f++)
        {
            time += Frame;
            last = rig.Step(Frame, Input(true, OpenStance.Standing, feet, time))!.Value;
            if (forms.Count == 0 || forms[^1] != last.Form) forms.Add(last.Form);
        }
        Assert.Equal(new[] { "Koraidon-Limited-Build", "Koraidon-Sprinting-Build" }, forms);
        Assert.Equal(1f, last.Seated);
        Assert.Equal(0f, last.Flash);
        Assert.Equal(MountRig.MetresPerUnit(MountRules.Koraidon), last.Scale);
        Assert.Equal(RideStance.Seated, last.Stance);
    }

    [Fact]
    public void AChangeOfFormFlashesAndSquashesAtItsMiddleWhereItSwaps()
    {
        var rig = new MountRig();
        var feet = Vector3.Zero;
        double time = 0;
        Run(rig, 60, OpenStance.Standing, ref feet, ref time);
        var flashes = new List<(string Form, float Flash, float Scale)>();
        for (int f = 0; f < 30; f++)
        {
            time += Frame;
            var frame = rig.Step(Frame, Input(true, OpenStance.Gliding, feet, time))!.Value;
            flashes.Add((frame.Form, frame.Flash, frame.Scale));
        }
        int swap = flashes.FindIndex(x => x.Form == "Koraidon-Gliding-Build");
        Assert.InRange(swap * Frame, MountRig.ChangeSeconds / 2f - Frame, MountRig.ChangeSeconds / 2f + Frame);
        Assert.Equal(flashes.Max(x => x.Flash), flashes[swap].Flash > flashes[swap - 1].Flash ? flashes[swap].Flash : flashes[swap - 1].Flash);
        Assert.InRange(flashes.Max(x => x.Flash), MountRig.ChangeFlash - 0.05f, MountRig.ChangeFlash);
        Assert.Equal(0f, flashes[^1].Flash);
        Assert.Equal(MountRig.MetresPerUnit(MountRules.Koraidon), flashes[^1].Scale);
        Assert.True(flashes.Min(x => x.Scale) < MountRig.MetresPerUnit(MountRules.Koraidon) * (1f - MountRig.ChangeSquash * 0.8f));
    }

    [Fact]
    public void GettingOffHopsTheRiderDownAndTheMountGoesBack()
    {
        var rig = new MountRig();
        var feet = Vector3.Zero;
        double time = 0;
        Run(rig, 60, OpenStance.Standing, ref feet, ref time, speed: 3f);
        var frames = new List<MountFrame?>();
        for (int f = 0; f < 40; f++)
        {
            time += Frame;
            frames.Add(rig.Step(Frame, Input(false, OpenStance.Standing, feet, time)));
        }
        Assert.Equal(1f, frames[0]!.Value.Seated);
        int down = frames.FindIndex(x => x is { Seated: 0f });
        Assert.InRange(down * Frame, MountRig.HopDown - Frame, MountRig.HopDown + Frame);
        int gone = frames.FindIndex(x => x == null);
        Assert.InRange(gone * Frame, MountRig.OffSeconds - Frame, MountRig.OffSeconds + Frame);
        Assert.True(frames[gone - 1]!.Value.Flash > 0.9f);
        Assert.All(frames.Skip(gone), x => Assert.Null(x));

        // A reset (a fade's midpoint) plays nothing afterwards
        var again = new MountRig();
        Run(again, 60, OpenStance.Standing, ref feet, ref time);
        again.Reset();
        time += Frame;
        Assert.Null(again.Step(Frame, Input(false, OpenStance.Standing, feet, time)));
    }

    [Fact]
    public void TheMountStandsByWhatItDoes()
    {
        var feet = new Vector3(0f, 5f, 0f);
        double time = 0;
        // Gliding: nose down
        var glide = new MountRig();
        Run(glide, 60, OpenStance.Standing, ref feet, ref time);
        var g = Run(glide, 30, OpenStance.Gliding, ref feet, ref time)!.Value;
        var forward = Vector3.TransformNormal(Vector3.UnitZ, g.Turn);
        Assert.Equal(-MathF.Sin(MountRig.GlidePitch), forward.Y, 4);
        Assert.Equal(RideStance.Glide, g.Stance);

        // Climbing a face whose way out is east: its back out of the face, its nose up it, its belly toward it
        var climb = new MountRig();
        Run(climb, 60, OpenStance.Standing, ref feet, ref time);
        var c = Run(climb, 30, OpenStance.Climbing, ref feet, ref time, cling: (1f, 0f))!.Value;
        var up = Vector3.TransformNormal(Vector3.UnitY, c.Turn);
        forward = Vector3.TransformNormal(Vector3.UnitZ, c.Turn);
        Assert.True(up.X > 0.9f, $"its back faces {up}");
        Assert.True(forward.Y > 0.9f && forward.X < 0f, $"its nose points {forward}");
        Assert.Equal(1f, Vector3.Cross(up, forward).Length(), 4);
        Assert.True(c.Origin.X < feet.X, "it stands toward the face");
        Assert.Equal(RideStance.Climb, c.Stance);
        Assert.Equal("Koraidon-Sprinting-Build", c.Form);

        // Swimming: afloat, its seat over the water's surface by its float
        var swim = new MountRig();
        Run(swim, 60, OpenStance.Standing, ref feet, ref time);
        var s = Run(swim, 40, OpenStance.Swimming, ref feet, ref time)!.Value;
        Assert.Equal("Koraidon-Swimming-Build", s.Form);
        Assert.True(s.Afloat);
        var model = PokemonModels.Get(s.Form);
        var seat = MountLooks.SeatOf(model);
        float surface = feet.Y + (float)OpenBody.SwimDepth;
        float seatY = Vector3.Transform(seat.Rest, MountLooks.Place(s, seat)).Y;
        Assert.Equal(surface + MountLooks.FloatUnder * s.Scale, seatY, 3);
    }

    [Fact]
    public void TheRiderSitsTheSeatThroughATrotAndCrouchesInADash()
    {
        var model = PokemonModels.Get("Koraidon-Sprinting-Build");
        var rig = new MountRig();
        var feet = Vector3.Zero;
        double time = 0;
        Run(rig, 60, OpenStance.Standing, ref feet, ref time);
        const float Saddle = 0.75f;
        var seat = MountLooks.SeatOf(model);
        var heights = new List<float>();
        MountFrame frame = default;
        for (int f = 0; f < 60; f++)
        {
            frame = Run(rig, 1, OpenStance.Standing, ref feet, ref time, speed: 6f)!.Value;
            var root = MountLooks.RiderRoot(model, frame, Saddle);
            // The seat carried by the body's bone, in the world
            var posed = Vector3.Transform(seat.Rest, model.Skin[seat.Bone]);
            var atSeat = Vector3.Transform(posed, frame.Root);
            Assert.Equal(atSeat.Y - Saddle, root.Translation.Y, 3);
            Assert.Equal(atSeat.X, root.Translation.X, 3);
            heights.Add(root.Translation.Y);
        }
        Assert.Equal(RideStance.Seated, frame.Stance);
        Assert.InRange(frame.RunBlend, 0.6f, 0.7f);
        // The trot's stride carries the rider up and down with the mount's back
        Assert.True(heights.Max() - heights.Min() > 1e-4f, "the rider never moved with the mount's back");
        var dash = Run(rig, 20, OpenStance.Standing, ref feet, ref time, speed: 11f)!.Value;
        Assert.Equal(RideStance.Dash, dash.Stance);
        Assert.Equal(1f, dash.RunBlend);
    }

    [Fact]
    public void TheSameRideGivesTheSameFrames()
    {
        List<MountFrame?> Ride()
        {
            var rig = new MountRig();
            var feet = Vector3.Zero;
            double time = 0;
            var all = new List<MountFrame?>();
            foreach (var (stance, speed, frames) in new[] { (OpenStance.Standing, 0f, 30), (OpenStance.Standing, 6f, 40), (OpenStance.Gliding, 6f, 30), (OpenStance.Swimming, 4f, 30) })
                for (int f = 0; f < frames; f++) all.Add(Run(rig, 1, stance, ref feet, ref time, speed));
            for (int f = 0; f < 30; f++) all.Add(Run(rig, 1, OpenStance.Standing, ref feet, ref time, mounted: false));
            return all;
        }
        Assert.Equal(Ride(), Ride());
    }

    [Fact]
    public void TheMountClimbsTheHarnesssBlockOnTheLab()
    {
        // The harness's climbing block (Modes/FreeWalkMount.cs) on the lab's meadow: the mount clings to it and climbs
        var map = OpenWorldPeopleTests.LabWithPeople();
        double foot = MountShots.BlockFoot(map);
        map.Add(MountShots.Block(foot));
        var start = OpenPos.FromMetres(-23, map.Ground.Footing(-23, -30, 20, -20)!.Value.Y, -30);
        var body = new OpenBody(start, OpenYaw.West);
        body.Mount();
        var grants = MovementGrants.Climb | MovementGrants.Dash;
        for (int t = 0; t < 4 * 120 && body.Stance != OpenStance.Climbing; t++) body.Step(map.Ground, new OpenInput(-1, 0), grants);
        output.WriteLine($"after steering west: {body.Pos}, {body.Stance}, cling {body.Cling}");
        Assert.Equal(OpenStance.Climbing, body.Stance);
        Assert.InRange(body.Pos.X, -26_000 + 100, -26_000 + 500);
        int y0 = body.Pos.Y;
        for (int t = 0; t < 120; t++) body.Step(map.Ground, new OpenInput(-1, 0), grants);
        output.WriteLine($"after a second up: {body.Pos}, {body.Stance}");
        Assert.True(body.Pos.Y > y0 + 1000, $"it rose {body.Pos.Y - y0} mm");
    }

    // ---------------------------------------------------------------- the camera

    [Fact]
    public void OnFootTheCameraIsTheWalkersToTheBit()
    {
        var plain = new OrbitCamera();
        var told = new OrbitCamera();
        plain.Reset(OpenYaw.East);
        told.Reset(OpenYaw.East);
        var feet = new OpenVec(3, 1, -2);
        for (int f = 0; f < 120; f++)
        {
            feet += new OpenVec(0.05, 0, 0.01);
            told.Ride(false, OpenStance.Standing, 3, Frame);
            plain.Advance(Frame, OpenYaw.East, 3);
            told.Advance(Frame, OpenYaw.East, 3);
            Assert.Equal(plain.Place(null, feet, Frame), told.Place(null, feet, Frame));
            Assert.Equal(OrbitCamera.LensDegrees, told.RideLens(OrbitCamera.LensDegrees));
            Assert.Equal(37.5, told.RideLens(37.5));
        }

        // Ridden and back off: once the share has run out the camera is the walker's again, exactly
        for (int f = 0; f < 90; f++) told.Ride(true, OpenStance.Standing, 11, Frame);
        Assert.Equal(1, told.RideShare);
        Assert.NotEqual(plain.Place(null, feet, Frame), told.Place(null, feet, Frame));
        for (int f = 0; f < 60; f++) told.Ride(false, OpenStance.Standing, 0, Frame);
        Assert.Equal(0, told.RideShare);
        Assert.Equal(plain.Place(null, feet, Frame), told.Place(null, feet, Frame));
    }

    [Fact]
    public void RiddenTheCameraStandsOverTheMountAndASprintWidensAndLeads()
    {
        var feet = new OpenVec(0, 0, 0);
        OrbitCamera Settled(OpenStance stance, double speed)
        {
            var camera = new OrbitCamera();
            camera.Reset(OpenYaw.South);
            for (int f = 0; f < 240; f++) camera.Ride(true, stance, speed, Frame);
            return camera;
        }
        var walker = new OrbitCamera();
        walker.Reset(OpenYaw.South);
        var (walkerEye, walkerAt) = walker.Place(null, feet, 0);

        var trot = Settled(OpenStance.Standing, 6);
        var (eye, at) = trot.Place(null, feet, 0);
        Assert.Equal(OrbitCamera.RideAbove, at.Y, 3);
        Assert.Equal(OrbitCamera.RideArm, Math.Sqrt(OpenVec.Dot(eye - at, eye - at)), 3);
        Assert.True(Math.Sqrt(OpenVec.Dot(eye - at, eye - at)) > Math.Sqrt(OpenVec.Dot(walkerEye - walkerAt, walkerEye - walkerAt)));
        Assert.Equal(OrbitCamera.LensDegrees, trot.RideLens(OrbitCamera.LensDegrees), 3);

        // A sprint at 11 m/s: the arm longer, the lens wider, the point led ahead along the camera's way (south)
        var sprint = Settled(OpenStance.Standing, 11);
        var (sprintEye, sprintAt) = sprint.Place(null, feet, 0);
        Assert.Equal(OrbitCamera.RideArm + OrbitCamera.SprintArm, Math.Sqrt(OpenVec.Dot(sprintEye - sprintAt, sprintEye - sprintAt)), 2);
        Assert.Equal(OrbitCamera.LensDegrees + OrbitCamera.SprintLens, sprint.RideLens(OrbitCamera.LensDegrees), 2);
        Assert.Equal(OrbitCamera.SprintLead, sprintAt.Z, 2);

        // A glide looks further down; a climb looks nearly level up the face
        var glide = Settled(OpenStance.Gliding, 6);
        var climb = Settled(OpenStance.Climbing, 1.5);
        double Pitch(OrbitCamera c)
        {
            var (e, t) = c.Place(null, feet, 0);
            var d = e - t;
            return Math.Atan2(d.Y, Math.Sqrt(d.X * d.X + d.Z * d.Z)) * 180 / Math.PI;
        }
        Assert.Equal(OrbitCamera.GlidePitchDegrees, Pitch(glide), 2);
        Assert.Equal(OrbitCamera.ClimbPitchDegrees, Pitch(climb), 2);
        Assert.Equal(OrbitCamera.GlideLens, glide.RideLens(OrbitCamera.LensDegrees), 3);

        // Off the mount at once (a fade's midpoint): the walker's camera
        sprint.OffTheMount();
        Assert.Equal(0, sprint.RideShare);
        Assert.Equal(OrbitCamera.Above, sprint.Place(null, feet, 0).Target.Y, 6);
    }
}

/// <summary>
/// The climbing block the harness's `freewalk` mode lays on the lab (tools/ShotHarness/Modes/FreeWalkMount.cs, the
/// same numbers): 6 m by 8 m on the meadow under the ledge, 7 m tall, its bottom 2 cm over the highest ground under it,
/// since a block sunk into the ground has the meadow inside it, which the climb's step onto the top takes for open ground.
/// </summary>
internal static class MountShots
{
    public static double BlockFoot(OpenMap map)
    {
        double highest = double.MinValue;
        for (double x = -32; x <= -26; x += 0.5)
            for (double z = -34; z <= -26; z += 0.5)
                highest = Math.Max(highest, map.Ground.Footing(x, z, 20, -20)?.Y ?? 0);
        return highest + 0.02;
    }

    public static OpenBox Block(double foot) => OpenBox.Metres("the climbing block", -32, foot, -34, -26, foot + 7, -26) with { Climbable = true };
}