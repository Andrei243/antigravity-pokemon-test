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

        // Feet on the ground, the crown at the frame's with the hair's cap over it (none on the bald), and inside the
        // grown frame's limits (style guide, "Grown characters", "The frame's limits")
        Assert.InRange(min.Y, -0.01f, 0.01f);
        Assert.InRange(max.Y, frame.Crown - 0.005f, frame.Crown + (rig.Style.Hair == HairCut.Bald ? 0.01f : 0.03f));
        Assert.True(max.Y <= frame.Crown + 0.12f);
        Assert.InRange(max.X - min.X, 0.4f, 1.0f);
        Assert.InRange(frame.Crown / (2f * frame.HeadR), 4.8f, 7.7f);

        // A mesh can index 65,535 vertices: the body leaves room for part b's locks of hair
        Assert.InRange(mesh.VertexCount, 15000, 58000);
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
        // No face is painted yet (part b's), so none is drawn
        Assert.Null(rig.FacePatch);
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
    public void TheBaseLayerPaintsTheLooksColoursWhereTheyBelong()
    {
        // Until part b dresses them (style guide, "Grown characters"): the top on the body, the trousers on the legs, the
        // shoes and their soles, bare hands, neck and face, the hair's cap
        var rig = Rig("PLAYER~Teen");
        var s = rig.Style;
        var f = CharacterModels.GrownFrameOf(BodyBuild.Teen);
        var mesh = rig.Mesh;
        Color At(Vector3 p) => mesh.Colors[Nearest(mesh, p)];
        Assert.True(Near(At(new Vector3(0, f.ChestMidY, 0.2f)), s.Top), "the chest is the top's");
        Assert.True(Near(At(new Vector3(f.Elbow.X, f.Elbow.Y, 0.04f)), s.Top), "a sleeve runs past the elbow");
        Assert.True(Near(At(new Vector3(f.LegTop.X, (f.LegTop.Y + f.Knee.Y) / 2f, 0.1f)), s.Bottom), "the thigh is the trousers'");
        Assert.True(Near(At(new Vector3(f.Knee.X, (f.Knee.Y + f.Ankle.Y) / 2f, 0.06f)), s.Bottom), "the shin is the trousers'");
        Assert.True(Near(At(f.Wrist + Vector3.Normalize(f.Wrist - f.Elbow) * (0.6f * f.HandLength)), s.Skin), "the hand is bare");
        Assert.True(Near(At(new Vector3(0, (f.NeckFootY + f.Chin) / 2f + 0.01f, 0.06f)), s.Skin), "the neck is bare");
        Assert.True(Near(At(new Vector3(f.Ankle.X, 0.04f, f.Ankle.Z + 0.7f * f.FootLength)), s.Shoes), "the toes are the shoe's");
        Assert.True(Near(At(new Vector3(f.Ankle.X, 0f, f.Ankle.Z + 0.3f * f.FootLength)), s.Sole!.Value), "the sole is the sole's");
        Assert.True(Near(At(new Vector3(0, f.Crown + 0.02f, -0.02f)), s.HairColor), "the crown is the hair's");

        // Short sleeves end on the upper arm, shorts at mid-thigh
        var kid = Rig("YOUNGSTER~Child");
        var k = CharacterModels.GrownFrameOf(BodyBuild.Child);
        Color KidAt(Vector3 p) => kid.Mesh.Colors[Nearest(kid.Mesh, p)];
        Assert.True(Near(KidAt(Vector3.Lerp(k.Elbow, k.Wrist, 0.5f) + new Vector3(0, 0, 0.03f)), kid.Style.Skin), "a short sleeve leaves the forearm bare");
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
}
