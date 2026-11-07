using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using Raylib_cs;

namespace PokemonPlatinumTests;

/// <summary>
/// The grown kit (plan 30 · FM4, part a; style guide, "Grown characters"): the five builds, the 26-bone skeleton, the
/// sculpting helpers and the base layer, beside the chibi kit and never in its way. Part b's hair cuts, hats, garments
/// and faces add their twins of <see cref="CharacterKitTests"/>' rules here.
/// </summary>
public class GrownKitTests
{
    private readonly ITestOutputHelper output;

    public GrownKitTests(ITestOutputHelper output) => this.output = output;

    /// <summary>A look of the table asked in each grown build: the five the harness's <c>fw40</c> boards show, and the curved two.</summary>
    public static IEnumerable<object[]> Builds => new[]
    {
        new object[] { "PLAYER", "Teen" }, new object[] { "CLERK", "Grown" }, new object[] { "BLACKBELT", "GrownBroad" },
        new object[] { "YOUNGSTER", "Child" }, new object[] { "OLD_MAN", "GrownElder" },
        new object[] { "DAWN", "Teen" }, new object[] { "NURSE", "Grown" }
    };

    private static readonly Dictionary<string, CharacterRig> Rigs = new();

    private static CharacterRig Rig(string type)
    {
        lock (Rigs)
        {
            if (!Rigs.TryGetValue(type, out var rig)) Rigs[type] = rig = CharacterModels.Build(type);
            return rig;
        }
    }

    private static (Vector3 Min, Vector3 Max) Bounds(SdfMesh mesh)
    {
        var min = new Vector3(float.MaxValue);
        var max = new Vector3(float.MinValue);
        foreach (var p in mesh.Positions) { min = Vector3.Min(min, p); max = Vector3.Max(max, p); }
        return (min, max);
    }

    private static int Nearest(SdfMesh mesh, Vector3 to) =>
        Enumerable.Range(0, mesh.VertexCount).OrderBy(v => Vector3.DistanceSquared(mesh.Positions[v], to)).First();

    private static bool Near(Color a, Color b, int by = 30) => Math.Abs(a.R - b.R) + Math.Abs(a.G - b.G) + Math.Abs(a.B - b.B) < by;

    [Theory]
    [MemberData(nameof(Builds))]
    public void EachBuildStandsAtItsFramesHeightInOneSmoothSkinnedBody(string look, string buildName)
    {
        var build = Enum.Parse<BodyBuild>(buildName);
        var rig = Rig($"{look}~{build}");
        var frame = CharacterModels.GrownFrameOf(build, rig.Style.CurvedFigure());
        var mesh = rig.Mesh;
        var (min, max) = Bounds(mesh);
        output.WriteLine($"{look} as {build}: {mesh.VertexCount} vertices, {mesh.TriangleCount} triangles, {max.Y:0.000} tall, {max.X - min.X:0.000} wide, " +
            $"{frame.Crown / (2f * frame.HeadR):0.00} heads");

        Assert.Equal((CharacterKit.Grown, build), (rig.Style.Kit, rig.Style.Build));
        Assert.Equal(HumanBones.GrownCount, rig.Skeleton.Count);
        Assert.True(rig.Skeleton.Count <= SkinnedModel.MaxBones);

        // Every vertex bound to real bones with weights that add up to one
        for (int v = 0; v < mesh.VertexCount; v++)
        {
            float sum = 0f;
            for (int k = 0; k < 4; k++)
            {
                sum += mesh.BoneWeights[v * 4 + k];
                Assert.True(mesh.BoneIndices[v * 4 + k] < HumanBones.GrownCount);
            }
            Assert.Equal(1f, sum, 3);
        }

        // Feet on the ground, the crown at the frame's with the hair, or a hat, over it, and inside the grown frame's
        // limits (style guide, "Grown characters", "The frame's limits")
        Assert.InRange(min.Y, -0.01f, 0.01f);
        Assert.InRange(max.Y, frame.Crown - 0.005f, frame.Crown + 0.12f);
        Assert.InRange(max.X - min.X, 0.4f, 1.0f);
        Assert.InRange(frame.Crown / (2f * frame.HeadR), 4.8f, 7.7f);

        // A mesh can index 65,535 vertices: the body with its hair, its hat and what it holds, and the clothes apart
        Assert.InRange(mesh.VertexCount, 15000, 60000);
        if (rig.Clothes != null) Assert.InRange(rig.Clothes.VertexCount, 1000, 60000);
    }

    [Fact]
    public void TheFramesAreTheStyleGuidesTable()
    {
        // The style guide's table of the five builds, read from the guide itself, is what the frames are made of
        string guide = File.ReadAllText(Path.Combine(Repository.Root, "docs", "art", "style-guide.md"));
        int found = 0;
        foreach (string line in guide.Split('\n'))
        {
            var cells = line.Trim().Split('|', StringSplitOptions.TrimEntries);
            if (cells.Length < 9 || !cells[1].StartsWith('`') || !Enum.TryParse<BodyBuild>(cells[1].Trim('`'), out var build)) continue;
            if (CharacterStyle.KitOf(build) != CharacterKit.Grown) continue;
            float Number(string text) => float.Parse(text, CultureInfo.InvariantCulture);
            var head = cells[4].Split(" at ");
            var f = CharacterModels.GrownFrameOf(build);
            Assert.Equal(Number(cells[3]), f.Crown, 3);
            Assert.Equal(Number(head[0]), f.HeadR, 3);
            Assert.Equal(Number(head[1]), f.HeadC.Y, 3);
            Assert.Equal(Number(cells[5]), MathF.Round(f.Crown / (2f * f.HeadR) * 2f) / 2f, 1);
            Assert.Equal(Number(cells[6]), f.HipY, 3);
            Assert.Equal(Number(cells[7]), 2f * f.Shoulder.X, 3);
            Assert.Equal(Number(cells[8]), f.Girth, 3);
            found++;
        }
        Assert.Equal(5, found);
    }

    [Fact]
    public void TheAppendedBonesHangWhereTheGuideSays()
    {
        var rig = Rig("PLAYER~Teen");
        var sk = rig.Skeleton;
        var chibi = CharacterModels.Build("PLAYER").Skeleton;
        // The chibi kit's 19 at their numbers and names, then the seven
        for (int b = 0; b < HumanBones.Count; b++) Assert.Equal(chibi[b].Name, sk[b].Name);
        Assert.Equal(new[] { "neck", "chest", "clavicle.L", "clavicle.R", "toe.L", "toe.R", "coatTail" },
            Enumerable.Range(HumanBones.Count, HumanBones.GrownCount - HumanBones.Count).Select(b => sk[b].Name));
        (int Bone, int Parent)[] hangs =
        {
            (HumanBones.Chest, HumanBones.Spine), (HumanBones.Neck, HumanBones.Chest), (HumanBones.Head, HumanBones.Neck),
            (HumanBones.ClavicleL, HumanBones.Chest), (HumanBones.ClavicleR, HumanBones.Chest),
            (HumanBones.UpperArmL, HumanBones.ClavicleL), (HumanBones.UpperArmR, HumanBones.ClavicleR), (HumanBones.Bag, HumanBones.Chest),
            (HumanBones.ToeL, HumanBones.FootL), (HumanBones.ToeR, HumanBones.FootR), (HumanBones.CoatTail, HumanBones.Hips),
            (HumanBones.Hair, HumanBones.Head), (HumanBones.Skirt, HumanBones.Hips)
        };
        foreach (var (bone, parent) in hangs) Assert.Equal(parent, sk[bone].Parent);

        // Posed parents first, each bone once
        var order = sk.Order.ToList();
        Assert.Equal(Enumerable.Range(0, sk.Count), order.OrderBy(b => b));
        for (int b = 0; b < sk.Count; b++)
            if (sk[b].Parent >= 0) Assert.True(order.IndexOf(sk[b].Parent) < order.IndexOf(b), $"{sk[b].Name} is posed before its parent");

        // The chibi kit's skeleton is posed in the order it was made, as it always was
        Assert.Equal(HumanBones.Count, chibi.Count);
        Assert.Equal(Enumerable.Range(0, HumanBones.Count), chibi.Order);
    }

    [Fact]
    public void ASkeletonHangsABoneFromALaterOne()
    {
        // A chain root -> a -> b, then c appended under root and b hung from it: turning c carries b, and a stays
        var sk = new Skeleton();
        sk.Add("root", -1, Vector3.Zero);
        int a = sk.Add("a", 0, new Vector3(0, 1, 0));
        int b = sk.Add("b", a, new Vector3(0, 2, 0));
        int c = sk.Add("c", 0, new Vector3(1, 0, 0));
        sk.Reparent(b, c);
        Assert.Equal(c, sk[b].Parent);
        Assert.True(sk.Order.ToList().IndexOf(c) < sk.Order.ToList().IndexOf(b));

        var pose = new SkeletonPose(sk.Count);
        pose.Turn(c, 0f, 0f, MathF.PI / 2f);
        var skin = new Matrix4x4[sk.Count];
        sk.Evaluate(pose, skin);
        var bAt = Vector3.Transform(sk[b].Joint, skin[b]);
        var turned = Vector3.Transform(new Vector3(0, 2, 0) - new Vector3(1, 0, 0), Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathF.PI / 2f)) + new Vector3(1, 0, 0);
        Assert.True(Vector3.Distance(turned, bAt) < 1e-4f, $"b is at {bAt}, turned with c to {turned}");
        Assert.Equal(new Vector3(0, 1, 0), Vector3.Transform(sk[a].Joint, skin[a]));

        // A bone added after keeps its place after its parent, and a parent that hangs from the bone is refused
        int d = sk.Add("d", b, new Vector3(0, 3, 0));
        Assert.True(sk.Order.ToList().IndexOf(b) < sk.Order.ToList().IndexOf(d));
        Assert.Throws<ArgumentException>(() => sk.Reparent(c, d));
        Assert.Throws<ArgumentException>(() => sk.Reparent(c, c));
    }

    [Theory]
    [InlineData("PLAYER", "Teen")]
    [InlineData("BLACKBELT", "GrownBroad")]
    [InlineData("YOUNGSTER", "Child")]
    public void HandsFeetToesNeckChestAndHeadMoveWithTheirBones(string look, string buildName)
    {
        var build = Enum.Parse<BodyBuild>(buildName);
        var rig = Rig($"{look}~{build}");
        var sk = rig.Skeleton;
        var mesh = rig.Mesh;
        var f = CharacterModels.GrownFrameOf(build);
        foreach (var (hand, foot, toe) in new[] { (HumanBones.HandL, HumanBones.FootL, HumanBones.ToeL), (HumanBones.HandR, HumanBones.FootR, HumanBones.ToeR) })
        {
            var wrist = sk[hand].Joint;
            var palm = wrist + Vector3.Normalize(wrist - sk[hand - 1].Joint) * (0.5f * f.HandLength);
            Assert.True(mesh.WeightOf(Nearest(mesh, palm), hand) > 0.8f, $"the palm follows the hand only {mesh.WeightOf(Nearest(mesh, palm), hand):F2} of the way");
            // The tip of the shoe moves with the toes, the heel with the foot
            var ankle = sk[foot].Joint;
            var tip = new Vector3(ankle.X, 0.02f, ankle.Z + 0.8f * f.FootLength);
            var heel = new Vector3(ankle.X, 0.02f, ankle.Z - 0.2f * f.FootLength);
            Assert.True(mesh.WeightOf(Nearest(mesh, tip), toe) > 0.9f, $"the toes follow the toe's bone only {mesh.WeightOf(Nearest(mesh, tip), toe):F2} of the way");
            Assert.True(mesh.WeightOf(Nearest(mesh, heel), foot) > 0.9f);
        }
        // The nose's tip with the head, the neck's middle with the neck, the chest's front with the chest, a shoulder's top
        // with its collarbone in part
        var nose = Enumerable.Range(0, mesh.VertexCount).Where(v => MathF.Abs(mesh.Positions[v].X) < 0.01f && mesh.Positions[v].Y > f.Chin && mesh.Positions[v].Y < f.HeadC.Y)
            .OrderByDescending(v => mesh.Positions[v].Z).First();
        Assert.True(mesh.WeightOf(nose, HumanBones.Head) > 0.95f);
        var neck = Nearest(mesh, new Vector3(0, f.NeckFootY + 0.35f * (f.Chin - f.NeckFootY), 0.045f));
        Assert.True(mesh.WeightOf(neck, HumanBones.Neck) > 0.5f, $"the neck's middle follows the neck only {mesh.WeightOf(neck, HumanBones.Neck):F2} of the way");
        var chest = Nearest(mesh, new Vector3(0, f.ChestMidY, f.ChestD + 0.02f));
        Assert.True(mesh.WeightOf(chest, HumanBones.Chest) > 0.6f);
        var shoulder = Nearest(mesh, new Vector3(f.Shoulder.X * 0.6f, f.ShoulderY + 0.08f, -0.01f));
        Assert.True(mesh.WeightOf(shoulder, HumanBones.ClavicleL) > 0.25f, $"the shoulder's top follows its collarbone only {mesh.WeightOf(shoulder, HumanBones.ClavicleL):F2}");
    }

    [Theory]
    [MemberData(nameof(Builds))]
    public void TheNoseStandsOutOfTheFaceAndTheFaceIsWhereTheGuideSays(string look, string buildName)
    {
        var build = Enum.Parse<BodyBuild>(buildName);
        var rig = Rig($"{look}~{build}");
        var f = CharacterModels.GrownFrameOf(build, rig.Style.CurvedFigure());
        var mesh = rig.Mesh;
        float R = f.HeadR;
        // The face's anchor is the surface between the eyes, at the head's middle, the eyes a fifth of the head's width apart
        Assert.Equal(f.HeadC.Y, rig.Face.Anchor.Y, 4);
        Assert.Equal(0.30f * R, rig.Face.EyeHalfSpacing, 4);
        Assert.True(rig.Face.Anchor.Z > f.HeadC.Z + 0.7f * R);
        // The nose's tip stands forward of the face beside it (a cheek's surface at the nose's height)
        var tip = Enumerable.Range(0, mesh.VertexCount).Where(v => mesh.WeightOf(v, HumanBones.Head) > 0.9f && mesh.Positions[v].Y < f.HeadC.Y && mesh.Positions[v].Y > f.Chin)
            .Select(v => mesh.Positions[v]).OrderByDescending(p => p.Z).First();
        Assert.InRange(tip.Y, f.HeadC.Y - 0.5f * R, f.HeadC.Y - 0.2f * R);
        var cheek = Enumerable.Range(0, mesh.VertexCount).Where(v => MathF.Abs(mesh.Positions[v].X - 0.35f * R) < 0.01f && MathF.Abs(mesh.Positions[v].Y - tip.Y) < 0.01f)
            .Select(v => mesh.Positions[v].Z).Max();
        Assert.True(tip.Z > cheek + 0.15f * R, $"the nose's tip at z {tip.Z:0.000}, the cheek beside it at {cheek:0.000}");
        // The painted face's patch covers the front of the head (part b)
        Assert.NotNull(rig.FacePatch);
    }

    [Theory]
    [MemberData(nameof(Builds))]
    public void TheBodyIsOnePiece(string look, string build)
    {
        // Every triangle joined to every other through the mesh: no limb, hand, toe or ear floats apart
        var mesh = Rig($"{look}~{build}").Mesh;
        var root = Enumerable.Range(0, mesh.VertexCount).ToArray();
        int Find(int v) { while (root[v] != v) v = root[v] = root[root[v]]; return v; }
        for (int i = 0; i < mesh.Indices.Length; i += 3)
            for (int k = 1; k < 3; k++)
            {
                int a = Find(mesh.Indices[i]), b = Find(mesh.Indices[i + k]);
                if (a != b) root[a] = b;
            }
        var pieces = mesh.Indices.Distinct().Select(Find).GroupBy(r => r).Select(g => g.Count()).OrderByDescending(n => n).ToList();
        output.WriteLine($"{look} as {build}: pieces of {string.Join(", ", pieces.Take(5))} vertices");
        Assert.Single(pieces);
    }

    [Theory]
    [MemberData(nameof(Builds))]
    public void NeitherTheBodyNorItsOutlineTearsInTheChibiClips(string look, string build)
    {
        // A vertex weighted to both legs (the crotch), or the outline's shell weighted across the gap between the knees,
        // stretches into a fin when the legs part: no edge of either may grow past four times its length in a stride, a
        // run or a bow (style guide, "Grown characters": the legs' spacing and small blends, the seat, the shell's weights
        // from the body). Sitting turns the thighs a quarter turn, which stretches the hip's crease as any skin bent so
        // far by blended weights does: a grown sitter's clip is plan 30 · FM5's
        var rig = Rig($"{look}~{build}");
        (string Name, CharacterPose Pose)[] clips =
        {
            ("walk", new CharacterPose { Walk = 0.25f, WalkBlend = 1f, Time = 0.4f }), ("walk back", new CharacterPose { Walk = 0.75f, WalkBlend = 1f, Time = 0.4f }),
            ("run", new CharacterPose { Walk = 0.25f, WalkBlend = 1f, Running = true, Time = 0.4f }),
            ("bow", new CharacterPose { Emote = Emote.Bow, EmoteTime = 0.6f, Time = 0.4f })
        };
        foreach (var (part, mesh) in new[] { ("body", rig.Mesh), ("outline", rig.Shell) })
            foreach (var (name, pose) in clips)
            {
                rig.Animate(pose);
                var at = new Vector3[mesh.VertexCount];
                for (int v = 0; v < mesh.VertexCount; v++)
                    for (int k = 0; k < 4; k++)
                        at[v] += Vector3.Transform(mesh.Positions[v], rig.Skin[mesh.BoneIndices[v * 4 + k]]) * mesh.BoneWeights[v * 4 + k];
                int torn = 0;
                for (int i = 0; i < mesh.Indices.Length; i += 3)
                    for (int k = 0; k < 3; k++)
                    {
                        int a = mesh.Indices[i + k], b = mesh.Indices[i + (k + 1) % 3];
                        float now = Vector3.Distance(at[a], at[b]);
                        if (now > 0.02f && now > 4f * Vector3.Distance(mesh.Positions[a], mesh.Positions[b])) torn++;
                    }
                Assert.True(torn == 0, $"{look} as {build}: {torn} edges of the {part} tear in the {name}");
            }
    }

    [Fact]
    public void NothingIsThinnerThanTheGrownCellsAllow()
    {
        // Style guide, "Grown characters", "Meshing": no part thinner across than two and a half cells but the thumb
        Assert.True(CharacterModels.GrownMeshCell < CharacterModels.MeshCell);
        foreach (var build in new[] { BodyBuild.Teen, BodyBuild.Grown, BodyBuild.GrownBroad, BodyBuild.Child, BodyBuild.GrownElder })
        {
            var f = CharacterModels.GrownFrameOf(build);
            float cell = CharacterModels.GrownMeshCell;
            foreach (var (part, radius) in new[] { ("wrist", 0.024f * f.Girth), ("ankle", 0.031f * f.Girth), ("neck", 0.050f * f.Girth), ("elbow", 0.030f * f.Girth) })
                Assert.True(2f * radius >= 2.5f * cell, $"{build}'s {part} is {2f * radius / cell:0.0} cells across");
        }
        // A painted edge is soft over a cell and a half, so it never steps along the cells
        Assert.Equal(1.5f * CharacterModels.GrownMeshCell, CharacterModels.GrownPaintEdge, 6);
    }

    [Fact]
    public void TheChibiClipsPoseAGrownRigWithItsSevenAtRest()
    {
        var rig = Rig("PLAYER~Teen");
        float ankle = rig.Skeleton[HumanBones.FootL].Joint.Y;
        Vector3 Joint(int bone) => Vector3.Transform(rig.Skeleton[bone].Joint, rig.Skin[rig.Skeleton[bone].Parent]);

        // The walk keeps a foot on the ground, as on a chibi body
        for (int f = 0; f < 8; f++)
        {
            rig.Animate(new CharacterPose { Walk = (f + 0.5f) / 8f, WalkBlend = 1f, Time = 0.4f });
            Assert.InRange(MathF.Min(Joint(HumanBones.FootL).Y, Joint(HumanBones.FootR).Y), ankle - 0.04f, ankle + 0.04f);
        }
        // Standing, the arms hang closer than they were sculpted
        rig.Animate(new CharacterPose { Time = 0.4f });
        Assert.True(Joint(HumanBones.HandL).X < rig.Skeleton[HumanBones.HandL].Joint.X - 0.03f);
        // The head goes where the neck takes it: turned by a clip, the neck carries it
        Assert.True(Vector3.Distance(Joint(HumanBones.Head), rig.Skeleton[HumanBones.Head].Joint) < 0.05f);

        // No chibi clip moves the seven a grown body appends, and every emote starts and ends at rest on all 26
        foreach (var emote in Enum.GetValues<Emote>().Where(e => e != Emote.None))
        {
            var idle = new SkeletonPose(rig.Skeleton.Count);
            CharacterAnimation.Apply(rig, new CharacterPose { Time = 0.4f }, idle);
            float d = CharacterAnimation.Duration(emote);
            foreach (float t in new[] { 0f, 0.5f * d, d })
            {
                var pose = new SkeletonPose(rig.Skeleton.Count);
                CharacterAnimation.Apply(rig, new CharacterPose { Time = 0.4f, Emote = emote, EmoteTime = t }, pose);
                for (int b = HumanBones.Count; b < HumanBones.GrownCount; b++)
                    Assert.True(MathF.Abs(Quaternion.Dot(pose.Rotation[b], Quaternion.Identity)) > 0.99999f && pose.Offset[b] == Vector3.Zero, $"{emote} moves bone {b}");
                if (t == 0.5f * d) continue;
                for (int b = 0; b < pose.Count; b++)
                    Assert.True(MathF.Abs(Quaternion.Dot(pose.Rotation[b], idle.Rotation[b])) > 0.9999f, $"{emote} at {t}s: bone {b} is not at rest");
            }
        }
    }

    [Fact]
    public void TheLooksColoursAreWhereTheyBelongOnTheBodyAndItsClothes()
    {
        // Style guide, "Grown characters": the jacket and its sleeves on the clothes' layer, the body under it its lining,
        // the trousers on the legs, the shoes and their soles, bare hands, neck and face
        var rig = Rig("PLAYER~Teen");
        var s = rig.Style;
        var f = CharacterModels.GrownFrameOf(BodyBuild.Teen);
        var mesh = rig.Mesh;
        var clothes = rig.Clothes!;
        Color At(Vector3 p) => mesh.Colors[Nearest(mesh, p)];
        Color Worn(Vector3 p) => clothes.Colors[Nearest(clothes, p)];
        Assert.True(Near(Worn(new Vector3(-0.03f, f.ChestMidY - 0.04f, 0.2f)), s.Top), "the jacket's front is the top's");
        Assert.True(Near(Worn(new Vector3(f.Elbow.X, f.Elbow.Y, 0.04f)), s.Top), "a sleeve runs past the elbow");
        Assert.True(Near(At(new Vector3(0, f.ChestMidY, 0.2f)), new Color((int)(s.Top.R * 0.62f), (int)(s.Top.G * 0.62f), (int)(s.Top.B * 0.62f), 255), 40),
            "the body under the jacket is its lining");
        Assert.True(Near(At(new Vector3(f.LegTop.X, (f.LegTop.Y + f.Knee.Y) / 2f, 0.1f)), s.Bottom), "the thigh is the trousers'");
        Assert.True(Near(At(new Vector3(f.Knee.X, (f.Knee.Y + f.Ankle.Y) / 2f, 0.06f)), s.Bottom), "the shin is the trousers'");
        Assert.True(Near(At(f.Wrist + Vector3.Normalize(f.Wrist - f.Elbow) * (0.6f * f.HandLength)), s.Skin), "the hand is bare");
        Assert.True(Near(At(new Vector3(0, (f.NeckFootY + f.Chin) / 2f + 0.01f, 0.06f)), s.Skin), "the neck is bare");
        Assert.True(Near(At(new Vector3(f.Ankle.X, 0.04f, f.Ankle.Z + 0.7f * f.FootLength)), s.Shoes), "the toes are the shoe's");
        Assert.True(Near(At(new Vector3(f.Ankle.X, 0f, f.Ankle.Z + 0.3f * f.FootLength)), s.Sole!.Value), "the sole is the sole's");
        var clerk = Rig("CLERK~Grown");
        var c = CharacterModels.GrownFrameOf(BodyBuild.Grown);
        Assert.True(Near(clerk.Mesh.Colors[Nearest(clerk.Mesh, new Vector3(0, c.Crown + 0.03f, -0.02f))], clerk.Style.HairColor), "the crown is the hair's");

        // Short sleeves end on the upper arm, leaving the forearm the body's own, and shorts at mid-thigh
        var kid = Rig("YOUNGSTER~Child");
        var k = CharacterModels.GrownFrameOf(BodyBuild.Child);
        Color KidAt(Vector3 p) => kid.Mesh.Colors[Nearest(kid.Mesh, p)];
        var shortSleeve = kid.Clothes!;
        var forearm = Vector3.Lerp(k.Elbow, k.Wrist, 0.5f) + new Vector3(0, 0, 0.03f);
        Assert.True(shortSleeve.Positions.Min(p => Vector3.Distance(p, k.Elbow)) > 0.01f, "a short sleeve stops before the elbow");
        Assert.True(Near(KidAt(forearm), kid.Style.Skin), "a short sleeve leaves the forearm bare");
        Assert.True(Near(KidAt(new Vector3(k.Knee.X, (k.Knee.Y + k.Ankle.Y) / 2f, 0.05f)), kid.Style.Skin), "shorts leave the shin bare");
        Assert.True(Near(KidAt(new Vector3(k.LegTop.X, k.LegTop.Y - 0.03f, 0.08f)), kid.Style.Bottom), "shorts cover the top of the thigh");
    }

    [Fact]
    public void ALookIsAskedInABuildByItsName()
    {
        // The chibi kit, as every look has always been
        var boy = CharacterStyle.For("PLAYER");
        Assert.Equal((CharacterKit.Chibi, BodyBuild.Kid), (boy.Kit, boy.Build));
        // In a build of the grown kit, by name, any case, beside an outfit and something held in either order
        foreach (string type in new[] { "PLAYER~Teen", "player~teen", "PLAYER+Rod~Teen", "PLAYER~Teen+Rod", "PLAYER@red_cap.-.-.-.-~Teen" })
        {
            var grown = CharacterStyle.For(type);
            Assert.Equal((CharacterKit.Grown, BodyBuild.Teen), (grown.Kit, grown.Build));
            Assert.Equal(boy.Top, grown.Top);
        }
        Assert.Equal(HeldProp.Rod, CharacterStyle.For("PLAYER~Teen+Rod").Holds);
        Assert.Equal(HeldProp.Rod, CharacterStyle.For("PLAYER+Rod~Teen").Holds);
        // A number is no build
        Assert.NotEqual(CharacterKit.Grown, CharacterStyle.For("PLAYER~5").Kit);

        // The name of a look in a kit, and back: the build it stands as there
        Assert.Equal("PLAYER~Teen", CharacterStyle.InKit("PLAYER", CharacterKit.Grown));
        Assert.Equal("PLAYER", CharacterStyle.InKit("PLAYER~Teen", CharacterKit.Chibi));
        Assert.Equal("PLAYER", CharacterStyle.InKit("PLAYER", CharacterKit.Chibi));
        Assert.Equal("PLAYER+Rod~Teen", CharacterStyle.InKit("PLAYER+Rod", CharacterKit.Grown));
        Assert.Equal("ROWAN~Grown", CharacterStyle.InKit("ROWAN", CharacterKit.Grown));
        Assert.Equal("OLD_MAN~GrownElder", CharacterStyle.InKit("OLD_MAN", CharacterKit.Grown));
        Assert.Equal("TWIN~Child", CharacterStyle.InKit("TWIN", CharacterKit.Grown));

        // Every build read in each kit is a build of that kit, and a kit's own builds are themselves
        foreach (var build in Enum.GetValues<BodyBuild>())
            foreach (var kit in Enum.GetValues<CharacterKit>())
            {
                var into = CharacterStyle.BuildIn(build, kit);
                Assert.Equal(kit, CharacterStyle.KitOf(into));
                if (CharacterStyle.KitOf(build) == kit) Assert.Equal(build, into);
            }
        Assert.Equal(5, Enum.GetValues<BodyBuild>().Count(b => CharacterStyle.KitOf(b) == CharacterKit.Grown));
    }

    [Fact]
    public void TheTablesGrownLookIsAskedFirstInTheGrownKit()
    {
        var table = CharacterStyles.Parse("""
            { "looks": { "hiker": { "top": "#804020", "build": "Adult" }, "trekker": { "top": "#204080", "kit": "Grown", "build": "Teen" } },
              "grown": { "hiker": { "top": "#a06040", "build": "GrownBroad" } },
              "same": { "mountaineer": "hiker" } }
            """);
        var grown = table.Get("Hiker", CharacterKit.Grown);
        Assert.Equal((CharacterKit.Grown, BodyBuild.GrownBroad, new Color(160, 96, 64, 255)), (grown.Kit, grown.Build, grown.Top));
        Assert.Equal(new Color(128, 64, 32, 255), table.Get("hiker", CharacterKit.Chibi).Top);
        Assert.Equal(new Color(128, 64, 32, 255), table.Get("hiker").Top);
        Assert.Equal(new Color(160, 96, 64, 255), table.Get("mountaineer", CharacterKit.Grown).Top);
        // A look of the grown kit alone stands among the looks, and the grown block names only twins of the chibi
        Assert.Equal(CharacterKit.Grown, table.Get("trekker").Kit);
        Assert.Equal(new[] { "hiker" }, table.GrownNames);
        Assert.ThrowsAny<JsonException>(() => CharacterStyles.Parse("""{ "looks": {}, "grown": { "nobody": { "build": "Grown" } } }"""));
        Assert.ThrowsAny<JsonException>(() => CharacterStyles.Parse("""{ "looks": { "trekker": { "kit": "Grown" } }, "grown": { "trekker": {} } }"""));
        Assert.ThrowsAny<JsonException>(() => CharacterStyles.Parse("""{ "looks": { "hiker": { "kit": "Tall" } } }"""));
        // The game's own table reads its looks' kits (none grown yet: Kalos's cast is plan 30 · FM10's)
        Assert.All(CharacterStyles.Default.Names, n => Assert.Equal(CharacterKit.Chibi, CharacterStyles.Default.Get(n).Kit));
    }

    [Fact]
    public void TheCurvedFigureFollowsTheLashesUnlessTheLookSays()
    {
        Assert.True(CharacterStyle.For("DAWN~Teen").CurvedFigure());
        Assert.False(CharacterStyle.For("PLAYER~Teen").CurvedFigure());
        Assert.False(CharacterStyle.For("LASS~Child").CurvedFigure());
        var said = CharacterStyle.For("DAWN~Teen");
        said.Curved = false;
        Assert.False(said.CurvedFigure());
        said = CharacterStyle.For("PLAYER~Teen");
        said.Curved = true;
        Assert.True(said.CurvedFigure());

        // A waist narrower, hips wider, a bust on the chest; a child's figure never curves
        var straight = CharacterModels.GrownFrameOf(BodyBuild.Grown);
        var curved = CharacterModels.GrownFrameOf(BodyBuild.Grown, curved: true);
        Assert.True(curved.WaistW < straight.WaistW && curved.PelvisW > straight.PelvisW);
        Assert.False(CharacterModels.GrownFrameOf(BodyBuild.Child, curved: true).Curved);
        Assert.Throws<ArgumentException>(() => CharacterModels.GrownFrameOf(BodyBuild.Kid));
    }

    // ------------------------------------------------------------------ part b: hair, hats, clothes, held, faces

    /// <summary>Every hair cut of the kit, on a look of the table that has it, asked in a grown build (the harness's <c>fw40_cuts</c>).</summary>
    public static IEnumerable<object[]> Cuts => new[]
    {
        "CLERK~Grown", "RIVAL~Teen", "LASS~Teen", "ROWAN~Grown", "FLINT~Grown", "BERTHA~GrownElder", "CANDICE~Grown", "JASMINE~Grown", "GRUNT~Grown",
        "ROXIE~Teen", "BURGH~Grown", "MARSHAL~GrownBroad"
    }.Select(t => new object[] { t });

    /// <summary>Every hat of the kit, on a look of the table that wears it, asked in a grown build (the harness's <c>fw40_hats</c>).</summary>
    public static IEnumerable<object[]> Hats => new[]
    {
        "PLAYER~Teen", "YOUNGSTER~Child", "NURSE~Grown", "MIRA~Child", "CRASHER_WAKE~GrownBroad", "RILEY~Grown", "CYCLIST_M~Teen", "WORKER~Grown", "CAMPER~Teen",
        "NINJA_BOY~Teen", "BLACKBELT~GrownBroad", "RANCHER~Grown", "JOGGER~Grown", "SOCIALITE~Grown"
    }.Select(t => new object[] { t });

    [Fact]
    public void EveryCutAndEveryHatIsALookOfTheTable()
    {
        // The twins below cover the kit: a look of each cut and of each hat
        Assert.Equal(Enum.GetValues<HairCut>().OrderBy(c => c), Cuts.Select(c => CharacterStyle.For((string)c[0]).Hair).OrderBy(c => c));
        Assert.Equal(Enum.GetValues<Headwear>().Where(h => h != Headwear.None).OrderBy(h => h), Hats.Select(h => CharacterStyle.For((string)h[0]).Hat).OrderBy(h => h));
        Assert.All(Cuts.Concat(Hats), t => Assert.Equal(CharacterKit.Grown, CharacterStyle.For((string)t[0]).Kit));
    }

    [Theory]
    [MemberData(nameof(Cuts))]
    public void EveryCutStaysInsideTheGrownFrame(string type)
    {
        // Style guide, "Grown characters", "The frame's limits": no wider than 1.0 and no taller than the crown and 0.12;
        // the hair grows over the head (the bald's band behind it), never down over the face below the brows
        var rig = Rig(type);
        var f = CharacterModels.GrownFrameOf(rig.Style.Build, rig.Style.CurvedFigure());
        var mesh = rig.Mesh;
        var (min, max) = Bounds(mesh);
        Assert.True(max.Y <= f.Crown + 0.12f, $"{type}: the hair reaches {max.Y - f.Crown:0.000} over the crown");
        Assert.True(max.X - min.X <= 1.0f);
        var hair = Enumerable.Range(0, mesh.VertexCount).Where(v => mesh.Materials[v] == (byte)SurfaceMaterial.Hair).ToList();
        output.WriteLine($"{type}: {hair.Count} vertices of hair, {max.Y - f.Crown:0.000} over the crown");
        Assert.True(hair.Count > (rig.Style.Hair == HairCut.Bald ? 150 : 2000), $"{type}: only {hair.Count} vertices of hair");
        // Nothing of the hair hangs in front of the eyes: in front of the face, between the brows and the nose's tip,
        // there is no hair (a moustache grows below, sculpted brows above)
        float R = f.HeadR;
        var over = hair.Where(v => mesh.Positions[v].Z > f.HeadC.Z + 0.6f * R && MathF.Abs(mesh.Positions[v].X) < 0.42f * R
            && mesh.Positions[v].Y < f.HeadC.Y + 0.1f * R && mesh.Positions[v].Y > f.HeadC.Y - 0.3f * R).ToList();
        Assert.True(over.Count == 0, $"{type}: {over.Count} vertices of hair over the eyes");
    }

    [Theory]
    [MemberData(nameof(Hats))]
    public void EveryHatShowsOverTheGrownHair(string type)
    {
        // The hair is built before the hat, so a hat set too close to the head disappears into it without a trace; and a
        // hat stays inside the grown frame. A grown head is small: the least of them (a nurse's cap, a visor's peak) show
        // some 300 vertices, the nurse's cross a dozen
        var rig = Rig(type);
        var f = CharacterModels.GrownFrameOf(rig.Style.Build, rig.Style.CurvedFigure());
        var mesh = rig.Mesh;
        int Above(Color c) => Enumerable.Range(0, mesh.VertexCount).Count(v => mesh.Positions[v].Y > rig.Face.Anchor.Y && Near(mesh.Colors[v], c));
        int hat = Above(rig.Style.HatColor), band = Above(rig.Style.HatBand);
        output.WriteLine($"{type}: {hat} vertices in the hat's colour, {band} in its band's");
        Assert.True(hat > 250, $"{type}: only {hat} vertices of the hat show");
        Assert.True(band > 8, $"{type}: only {band} vertices of the hat's band or badge show");
        Assert.True(Bounds(mesh).Max.Y <= f.Crown + 0.12f);
    }

    [Theory]
    [InlineData("NURSE~Grown")]
    [InlineData("RIVAL~Teen")]
    [InlineData("ROWAN~Grown")]
    [InlineData("LASS~Teen")]
    public void GrownHairIsLitAsOneMass(string type)
    {
        // Style guide, "Light on hair": the normals change gently from vertex to vertex across the hair, so the shade's
        // edge runs across the head instead of breaking up over every lock and groove
        var mesh = Rig(type).Mesh;
        var turns = new List<float>();
        for (int i = 0; i < mesh.Indices.Length; i += 3)
            for (int k = 0; k < 3; k++)
            {
                int a = mesh.Indices[i + k], b = mesh.Indices[i + (k + 1) % 3];
                if (mesh.Materials[a] != (byte)SurfaceMaterial.Hair || mesh.Materials[b] != (byte)SurfaceMaterial.Hair) continue;
                turns.Add(MathF.Acos(Math.Clamp(Vector3.Dot(mesh.Normals[a], mesh.Normals[b]), -1f, 1f)) * 180f / MathF.PI);
            }
        turns.Sort();
        float p95 = turns[(int)(turns.Count * 0.95f)];
        output.WriteLine($"{type}: {turns.Count} hair edges, 95 % turn less than {p95:F1}°");
        Assert.True(turns.Count > 3000);
        Assert.True(p95 < 12f, $"{type}: the hair's normals turn up to {p95:F1}° between neighbours");
    }

    [Fact]
    public void TheClothesAreALayerOfTheirOwnWithTheirHemsWhereTheGuideSays()
    {
        // Style guide, "Grown characters", "Clothes"
        static int Heaviest(SdfMesh mesh, int v)
        {
            int best = 0;
            for (int k = 1; k < 4; k++)
                if (mesh.BoneWeights[v * 4 + k] > mesh.BoneWeights[v * 4 + best]) best = k;
            return mesh.BoneIndices[v * 4 + best];
        }
        // A swimsuit and a bare chest wear nothing over the body
        Assert.Null(Rig("SWIMMER_F~Grown").Clothes);
        Assert.Null(Rig("CRASHER_WAKE~GrownBroad").Clothes);

        // A coat's tails on the coat's tail bone, down to just below the knee; its shell beside it
        var rowan = Rig("ROWAN~Grown");
        var f = CharacterModels.GrownFrameOf(BodyBuild.Grown);
        var coat = rowan.Clothes!;
        Assert.NotNull(rowan.ClothesShell);
        Assert.InRange(coat.Positions.Min(p => p.Y), f.Knee.Y - 0.08f, f.Knee.Y - 0.02f);
        var tail = Enumerable.Range(0, coat.VertexCount).Where(v => coat.Positions[v].Y < f.Knee.Y + 0.05f).ToList();
        Assert.All(tail, v => Assert.Equal(HumanBones.CoatTail, Heaviest(coat, v)));
        // The legs under it are in trousers
        Assert.True(Near(rowan.Mesh.Colors[Nearest(rowan.Mesh, new Vector3(f.Knee.X, (f.Knee.Y + f.Ankle.Y) / 2f, 0.06f))], rowan.Style.Bottom));
        // A robe's to the ankles
        var robe = Rig("EXPERT_M~GrownElder");
        Assert.True(robe.Clothes!.Positions.Min(p => p.Y) < CharacterModels.GrownFrameOf(BodyBuild.GrownElder).Ankle.Y + 0.08f);

        // A skirt on the skirt's bone, its hem just above the knee, over bare legs
        var lass = Rig("LASS~Teen");
        var t = CharacterModels.GrownFrameOf(BodyBuild.Teen, curved: true);
        var skirt = lass.Clothes!;
        var low = Enumerable.Range(0, skirt.VertexCount).Where(v => skirt.Positions[v].Y < t.HipY - 0.05f).ToList();
        Assert.NotEmpty(low);
        Assert.All(low, v => Assert.Equal(HumanBones.Skirt, Heaviest(skirt, v)));
        Assert.InRange(skirt.Positions.Min(p => p.Y), t.Knee.Y + 0.02f, t.Knee.Y + 0.09f);
        Assert.True(Near(lass.Mesh.Colors[Nearest(lass.Mesh, new Vector3(t.Knee.X, t.Knee.Y, 0.05f))], lass.Style.Skin), "the knee under the skirt is bare");

        // A jacket ends just below the hips; a bag on its own bone behind; a collar round the neck
        var player = Rig("PLAYER~Teen");
        var jacket = player.Clothes!;
        var front = Enumerable.Range(0, jacket.VertexCount).Where(v => MathF.Abs(jacket.Positions[v].X) < 0.05f && jacket.Positions[v].Z > 0f).Select(v => jacket.Positions[v].Y).Min();
        Assert.InRange(front, t.HipY - 0.06f, t.HipY);
        var bag = Enumerable.Range(0, jacket.VertexCount).Where(v => jacket.Positions[v].Z < -t.ChestD - 0.04f && MathF.Abs(jacket.Positions[v].X) < 0.03f).ToList();
        Assert.NotEmpty(bag);
        Assert.Contains(bag, v => Heaviest(jacket, v) == HumanBones.Bag);
    }

    [Theory]
    [InlineData("FISHERMAN~Grown", "Rod", HumanBones.HandR)]
    [InlineData("BUG_CATCHER~Child", "Net", HumanBones.HandR)]
    [InlineData("PARASOL_LADY~Grown", "Parasol", HumanBones.Chest)]
    [InlineData("GUITARIST~Grown", "Guitar", HumanBones.Spine)]
    [InlineData("TUBER_M~Child", "FloatRing", HumanBones.Hips)]
    [InlineData("CAMERAMAN~Grown", "Camera", HumanBones.ClavicleR)]
    [InlineData("RUIN_MANIAC~Grown", "Pickaxe", HumanBones.HandR)]
    public void WhatIsHeldMovesWithItsBone(string type, string heldName, int bone)
    {
        // Style guide, "Grown characters", "What is held": part of the body on the bone it moves with; only a parasol's
        // canopy stands over the frame
        var held = Enum.Parse<HeldProp>(heldName);
        var rig = Rig(type);
        Assert.Equal(held, rig.Style.Holds);
        // A mesh of its own, with its own outline (a parasol's canopy is more than the body's or the clothes' has room for)
        var mesh = rig.Held!;
        Assert.NotNull(rig.HeldShell);
        Assert.True(mesh.VertexCount < ushort.MaxValue);
        var colour = rig.Style.PropColor ?? held switch
        {
            HeldProp.Rod => new Color(84, 66, 54, 255), HeldProp.Net => new Color(238, 238, 228, 255), HeldProp.Parasol => rig.Style.Top,
            HeldProp.Guitar => new Color(196, 120, 58, 255), HeldProp.FloatRing => new Color(240, 96, 80, 255), HeldProp.Camera => new Color(58, 60, 70, 255),
            _ => new Color(150, 108, 66, 255)
        };
        var on = Enumerable.Range(0, mesh.VertexCount).Where(v => Near(mesh.Colors[v], colour, 12) && mesh.WeightOf(v, bone) > 0.9f).ToList();
        output.WriteLine($"{type}: {on.Count} vertices of the {held} on bone {bone}");
        Assert.True(on.Count > 150, $"{type}: only {on.Count} vertices of the {held} move with bone {bone}");
        var f = CharacterModels.GrownFrameOf(rig.Style.Build, rig.Style.CurvedFigure());
        if (held != HeldProp.Parasol) Assert.True(Bounds(mesh).Max.Y <= f.Crown + 0.12f);
        float left = Math.Min(Bounds(mesh).Min.X, Bounds(rig.Mesh).Min.X), right = Math.Max(Bounds(mesh).Max.X, Bounds(rig.Mesh).Max.X);
        Assert.True(right - left <= 1.05f, $"{type}: {right - left:0.000} wide with the {held}");
    }

    [Theory]
    [MemberData(nameof(Builds))]
    public void EveryPartOfAGrownRigFitsAMesh(string look, string build)
    {
        // A mesh indexes 65,535 vertices: the body, its outline, the clothes and theirs each stay under it
        var rig = Rig($"{look}~{build}");
        foreach (var (part, mesh) in new[] { ("body", rig.Mesh), ("outline", rig.Shell), ("clothes", rig.Clothes), ("clothes' outline", rig.ClothesShell), ("held", rig.Held),
                     ("held's outline", rig.HeldShell), ("face", rig.FacePatch) })
            if (mesh != null) Assert.True(mesh.VertexCount < ushort.MaxValue, $"{look} as {build}: the {part} is {mesh.VertexCount} vertices");
    }

    [Theory]
    [InlineData("DAWN~Teen")]
    [InlineData("CLERK~Grown")]
    [InlineData("LUCIAN~Grown")]
    [InlineData("BYRON~GrownBroad")]
    public void TheGrownFaceIsPaintedOnItsPatch(string type)
    {
        // Style guide, "Grown characters", "Faces": 256 pixels, the eyes either side of the anchor with their whites and
        // highlights, cut out between them, shut with no white left, and the mouth's opening when speaking
        var rig = Rig(type);
        Assert.NotNull(rig.FacePatch);
        Assert.True(rig.FacePatch!.TriangleCount > 400, $"{type}: the face's patch has {rig.FacePatch.TriangleCount} triangles");
        Assert.All(rig.FaceUVs!, uv => Assert.True(uv.X >= -0.01f && uv.X <= 1.01f && uv.Y >= -0.01f && uv.Y <= 1.01f));
        Assert.All(Enumerable.Range(0, rig.FacePatch.VertexCount), v => Assert.Equal(1f, rig.FacePatch.WeightOf(v, HumanBones.Head)));

        var open = CharacterFaces.Paint(rig.Style, rig.Face, Expression.Neutral, blink: false);
        int n = GrownFaces.TextureSize;
        Assert.Equal(n, open.Width);
        float k = n / (2f * rig.Face.TextureHalfSize);
        int ey = (int)(n * 0.5f - (rig.Face.Anchor.Y - rig.Face.TextureCenter.Y) * k);
        if (rig.Style.Glasses == null) Assert.False(open.IsOpaque(n / 2, ey), "between the eyes the skin shows through");
        foreach (float side in new[] { -1f, 1f })
        {
            int ex = (int)(n * 0.5f + side * rig.Face.EyeHalfSpacing * k);
            Assert.True(open.IsOpaque(ex, ey));
            Assert.Contains(Enumerable.Range(ex - 16, 32), x => Enumerable.Range(ey - 16, 32).Any(y => open.Get(x, y).Equals(Color.White)));
        }
        var shut = CharacterFaces.Paint(rig.Style, rig.Face, Expression.Neutral, blink: true);
        Assert.DoesNotContain(Enumerable.Range(0, n * n), i => shut.Get(i % n, i / n).Equals(Color.White));
        // Every face of the five, open and shut, and speaking, differs from the neutral one
        foreach (var expression in Enum.GetValues<Expression>().Where(e => e != Expression.Neutral))
            Assert.True(Differs(CharacterFaces.Paint(rig.Style, rig.Face, expression, blink: false), open) > 40, $"{type}: {expression} looks neutral");
        Assert.True(Differs(CharacterFaces.Paint(rig.Style, rig.Face, Expression.Neutral, blink: false, mouthOpen: true), open) > 40);
        // Glasses are a rim round each eye, there with the eyes shut too
        if (rig.Style.Glasses != null)
        {
            var bare = rig.Style.Clone();
            bare.Glasses = null;
            foreach (bool blink in new[] { false, true })
                Assert.True(Opaque(CharacterFaces.Paint(rig.Style, rig.Face, Expression.Neutral, blink)) > Opaque(CharacterFaces.Paint(bare, rig.Face, Expression.Neutral, blink)) + 300);
        }
        // A look whose brows are sculpted has none painted
        if (rig.Style.BushyBrows)
        {
            var brows = rig.Style.Clone();
            brows.BushyBrows = false;
            Assert.True(Opaque(CharacterFaces.Paint(brows, rig.Face, Expression.Neutral, false)) > Opaque(open) + 100);
        }
    }

    private static int Opaque(PixelCanvas c) => Enumerable.Range(0, c.Width * c.Height).Count(i => c.IsOpaque(i % c.Width, i / c.Width));

    private static int Differs(PixelCanvas a, PixelCanvas b) =>
        Enumerable.Range(0, a.Width * a.Height).Count(i => !a.Get(i % a.Width, i / a.Width).Equals(b.Get(i % b.Width, i / b.Width)));
}
