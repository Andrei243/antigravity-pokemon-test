using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using PokemonPlatinumEngine.Graphics;
using Raylib_cs;

namespace PokemonPlatinumTests;

/// <summary>Characters built with the SDF kit (plan 04 · G6): models, skeletons, weights, faces, animation and sprite frames.</summary>
public class CharacterKitTests
{
    private readonly ITestOutputHelper output;

    public CharacterKitTests(ITestOutputHelper output) => this.output = output;

    public static readonly string[] Humans =
    {
        "PLAYER", "RIVAL", "ROWAN", "NURSE", "MOM", "LADY", "CLERK", "YOUNGSTER", "LASS", "CLOWN", "LOOKER", "GENTLEMAN", "TRAINER",
        // Plan 11 · C5's named cast
        "POKETCH_PRESIDENT", "CHERYL", "MIRA", "BEBE", "CRASHER_WAKE"
    };

    public static IEnumerable<object[]> HumanTypes => Humans.Select(h => new object[] { h });

    [Theory]
    [MemberData(nameof(HumanTypes))]
    public void EveryCharacterIsOneSmoothSkinnedBody(string type)
    {
        var sw = Stopwatch.StartNew();
        var rig = CharacterModels.Build(type);
        output.WriteLine($"{type}: {rig.Mesh.VertexCount} vertices, {rig.Mesh.TriangleCount} triangles in {sw.ElapsedMilliseconds} ms");
        var mesh = rig.Mesh;

        Assert.InRange(mesh.VertexCount, 4000, ushort.MaxValue);
        Assert.Equal(HumanBones.Count, rig.Skeleton.Count);

        // Every vertex is bound to real bones with weights that add up to one
        for (int v = 0; v < mesh.VertexCount; v++)
        {
            float sum = 0f;
            for (int k = 0; k < 4; k++)
            {
                sum += mesh.BoneWeights[v * 4 + k];
                Assert.True(mesh.BoneIndices[v * 4 + k] < HumanBones.Count);
            }
            Assert.Equal(1f, sum, 3);
        }

        // Feet on the ground and a chibi's height (spiky hair stands higher, still well inside the sprite's frame)
        var min = new Vector3(float.MaxValue);
        var max = new Vector3(float.MinValue);
        foreach (var p in mesh.Positions) { min = Vector3.Min(min, p); max = Vector3.Max(max, p); }
        Assert.InRange(min.Y, -0.01f, 0.01f);
        Assert.InRange(max.Y, 1.1f, rig.Style.Hair == HairCut.Spiky ? 1.65f : 1.5f);
        Assert.InRange(max.X - min.X, 0.45f, 0.9f);

        // Hands, feet and head move with their own bones: the surface round each hand's joint follows the hand
        foreach (var (hand, foot) in new[] { (HumanBones.HandL, HumanBones.FootL), (HumanBones.HandR, HumanBones.FootR) })
        {
            var wrist = rig.Skeleton[hand].Joint;
            var palm = wrist + Vector3.Normalize(wrist - rig.Skeleton[hand - 1].Joint) * 0.05f;
            int nearPalm = Enumerable.Range(0, mesh.VertexCount).OrderBy(v => Vector3.DistanceSquared(mesh.Positions[v], palm)).First();
            Assert.True(mesh.WeightOf(nearPalm, hand) > 0.7f, $"{type}: the palm follows the hand only {mesh.WeightOf(nearPalm, hand):F2} of the way");
            var toe = rig.Skeleton[foot].Joint + new Vector3(0, -0.03f, 0.1f);
            int nearToe = Enumerable.Range(0, mesh.VertexCount).OrderBy(v => Vector3.DistanceSquared(mesh.Positions[v], toe)).First();
            Assert.True(mesh.WeightOf(nearToe, foot) > 0.9f);
        }
        int top = Enumerable.Range(0, mesh.VertexCount).OrderByDescending(v => mesh.Positions[v].Y).First();
        Assert.True(mesh.WeightOf(top, HumanBones.Head) + mesh.WeightOf(top, HumanBones.Hair) > 0.95f);

        // The face: its anchor is on the front of the head, and the patch the battle face is drawn on covers it
        Assert.NotNull(rig.FacePatch);
        Assert.True(rig.FacePatch!.TriangleCount > 150, $"face patch has {rig.FacePatch.TriangleCount} triangles");
        Assert.True(rig.Face.Anchor.Z > 0.2f);
        Assert.All(rig.FaceUVs!, uv => Assert.True(uv.X >= -0.01f && uv.X <= 1.01f && uv.Y >= -0.01f && uv.Y <= 1.01f));
    }

    [Theory]
    [InlineData("PLAYER")]
    [InlineData("NURSE")]
    [InlineData("YOUNGSTER")]
    [InlineData("MIRA")]
    [InlineData("CRASHER_WAKE")]
    public void EveryHatShowsOverTheHair(string type)
    {
        // The hair is built before the hat, so a hat set too close to the head disappears into it without a trace
        var rig = CharacterModels.Build(type);
        var mesh = rig.Mesh;
        static bool Near(Color a, Color b) => Math.Abs(a.R - b.R) + Math.Abs(a.G - b.G) + Math.Abs(a.B - b.B) < 30;
        int Above(Color c) => Enumerable.Range(0, mesh.VertexCount).Count(v => mesh.Positions[v].Y > rig.Face.Anchor.Y && Near(mesh.Colors[v], c));
        int hat = Above(rig.Style.HatColor), band = Above(rig.Style.HatBand);
        output.WriteLine($"{type}: {hat} vertices in the hat's colour, {band} in its band's");
        Assert.True(hat > 400, $"{type}: only {hat} vertices of the hat show");
        Assert.True(band > 40, $"{type}: only {band} vertices of the hat's band or badge show");
    }

    [Theory]
    [InlineData("NURSE")]
    [InlineData("RIVAL")]
    [InlineData("ROWAN")]
    public void HairIsLitAsOneMass(string type)
    {
        // Style guide, "Light on hair": the normals change gently from vertex to vertex across the hair (95 % of
        // neighbours within 7°; 21° before the softening), so the shade's edge runs across the head instead of
        // breaking up over every lock and groove
        var rig = CharacterModels.Build(type);
        var mesh = rig.Mesh;
        var turns = new List<float>();
        for (int i = 0; i < mesh.Indices.Length; i += 3)
            for (int k = 0; k < 3; k++)
            {
                int a = mesh.Indices[i + k], b = mesh.Indices[i + (k + 1) % 3];
                if (mesh.Materials[a] != (byte)SurfaceMaterial.Hair || mesh.Materials[b] != (byte)SurfaceMaterial.Hair) continue;
                float cos = Math.Clamp(Vector3.Dot(mesh.Normals[a], mesh.Normals[b]), -1f, 1f);
                turns.Add(MathF.Acos(cos) * 180f / MathF.PI);
            }
        turns.Sort();
        float p95 = turns[(int)(turns.Count * 0.95f)];
        output.WriteLine($"{type}: {turns.Count} hair edges, 95 % turn less than {p95:F1}°");
        Assert.True(turns.Count > 5000);
        Assert.True(p95 < 12f, $"{type}: the hair's normals turn up to {p95:F1}° between neighbours");
    }

    [Theory]
    [InlineData("STARTERBRIEFCASE")]
    [InlineData("RIFT")]
    public void PropsAreBuiltWithTheKitToo(string type)
    {
        var rig = CharacterModels.Build(type);
        Assert.True(rig.Mesh.VertexCount > 1000);
        Assert.Null(rig.FacePatch);
    }
}


/// <summary>Animation clips, sprite frames and faces (plan 04 · G6).</summary>
public class CharacterAnimationTests
{
    private static readonly CharacterRig Player = CharacterModels.Build("PLAYER");

    /// <summary>Where a joint of the sculpted skeleton is in the pose last animated.</summary>
    private static Vector3 Joint(CharacterRig rig, int bone)
    {
        int parent = rig.Skeleton[bone].Parent;
        var joint = rig.Skeleton[bone].Joint;
        return parent < 0 ? joint : Vector3.Transform(joint, rig.Skin[parent]);
    }

    [Theory]
    [InlineData("Walk", 8)]
    [InlineData("Run", 8)]
    [InlineData("Idle", 2)]
    [InlineData("Hop", 3)]
    [InlineData("Wave", 6)]
    [InlineData("Surprised", 6)]
    [InlineData("Nod", 6)]
    [InlineData("Cheer", 6)]
    public void EveryFrameIsBakedFromAPoseThatShowsThatFrame(string anim, int frames)
    {
        var strip = Enum.Parse<SpriteAnim>(anim);
        for (int facing = 0; facing < 4; facing++)
            for (int f = 0; f < frames; f++)
                foreach (bool blink in new[] { false, true })
                {
                    var key = new SpriteKey(Player, facing, strip, f, blink, Expression.Neutral);
                    var pose = CharacterSprites.BakePose(key);
                    var back = CharacterSprites.KeyOf(Player, pose, facing * MathF.PI / 2f);
                    // An emote shows its own face; everything else keeps the key's
                    Assert.Equal(key with { Expression = back.Expression }, back);
                }
    }

    [Fact]
    public void TheWalkSwingsTheLegsInTurnAndKeepsAFootOnTheGround()
    {
        float ankle = Player.Skeleton[HumanBones.FootL].Joint.Y;
        var leftZ = new List<float>();
        for (int f = 0; f < 8; f++)
        {
            Player.Animate(new CharacterPose { Walk = (f + 0.5f) / 8f, WalkBlend = 1f, Time = 0.4f });
            var l = Joint(Player, HumanBones.FootL);
            var r = Joint(Player, HumanBones.FootR);
            leftZ.Add(l.Z - r.Z);
            // The lower foot is near the ground in every frame: the hips drop as the legs spread
            Assert.InRange(MathF.Min(l.Y, r.Y), ankle - 0.03f, ankle + 0.03f);
        }
        // Half a cycle apart, the feet have swapped places (left ahead, then right ahead)
        Assert.True(leftZ[2] > 0.08f && leftZ[6] < -0.08f, string.Join(", ", leftZ.Select(z => z.ToString("F2"))));
    }

    [Fact]
    public void StandingTheArmsHangCloserThanInTheSculptedAPose()
    {
        Player.Animate(new CharacterPose { Time = 0.4f });
        float bind = Player.Skeleton[HumanBones.HandL].Joint.X;
        float idle = Joint(Player, HumanBones.HandL).X;
        Assert.True(idle < bind - 0.03f, $"the hand is at x {idle:F3}, sculpted at {bind:F3}");
    }

    [Fact]
    public void HairAndBagSwingABeatBehindTheBody()
    {
        // Over a walk cycle the hair and the bag tilt back and forth, not in step with the hips' bounce
        var hair = new List<float>();
        for (int f = 0; f < 16; f++)
        {
            var pose = new SkeletonPose(Player.Skeleton.Count);
            CharacterAnimation.Apply(Player, new CharacterPose { Walk = f / 16f, WalkBlend = 1f, Time = 0.4f }, pose);
            hair.Add(Vector3.Transform(Vector3.UnitZ, pose.Rotation[HumanBones.Hair]).Y);
        }
        Assert.True(hair.Max() - hair.Min() > 0.05f);
        var bag = new SkeletonPose(Player.Skeleton.Count);
        CharacterAnimation.Apply(Player, new CharacterPose { Walk = 0.1f, WalkBlend = 1f, Time = 0.4f }, bag);
        Assert.NotEqual(Quaternion.Identity, bag.Rotation[HumanBones.Bag]);
    }

    [Theory]
    [InlineData("Wave")]
    [InlineData("Surprised")]
    [InlineData("Nod")]
    [InlineData("Cheer")]
    [InlineData("Throw")]
    public void EmotesStartAndEndInTheStandingPose(string name)
    {
        var emote = Enum.Parse<Emote>(name);
        var idle = new SkeletonPose(Player.Skeleton.Count);
        CharacterAnimation.Apply(Player, new CharacterPose { Time = 0.4f }, idle);
        float duration = CharacterAnimation.Duration(emote);
        foreach (float t in new[] { 0f, duration, duration + 1f })
        {
            var pose = new SkeletonPose(Player.Skeleton.Count);
            CharacterAnimation.Apply(Player, new CharacterPose { Time = 0.4f, Emote = emote, EmoteTime = t }, pose);
            for (int b = 0; b < pose.Count; b++)
                Assert.True(MathF.Abs(Quaternion.Dot(pose.Rotation[b], idle.Rotation[b])) > 0.9999f, $"{name} at {t}s: bone {b} is not at rest");
        }
        // Somewhere along the way, something has moved
        bool moved = false;
        for (float t = 0.1f; t < 0.9f; t += 0.05f)
        {
            var during = new SkeletonPose(Player.Skeleton.Count);
            CharacterAnimation.Apply(Player, new CharacterPose { Time = 0.4f, Emote = emote, EmoteTime = t * duration }, during);
            moved |= Enumerable.Range(0, during.Count).Any(b => MathF.Abs(Quaternion.Dot(during.Rotation[b], idle.Rotation[b])) < 0.999f);
        }
        Assert.True(moved, $"{name} never moves");
    }

    [Fact]
    public void PixelFacesAreTwoEyesEitherSideOfTheAnchor()
    {
        var style = CharacterStyle.For("PLAYER");
        var front = new PixelCanvas(40, 58);
        CharacterFaces.Stamp(front, new Vector2(20.4f, 20.6f), 0, style, Expression.Neutral, blink: false);
        var eyes = Enumerable.Range(0, 40).Where(x => front.IsOpaque(x, 20)).ToList();
        Assert.Equal(new[] { 17, 18, 22, 23 }, eyes);
        Assert.Equal(Color.White, front.Get(17, 21));   // the highlight on the upper left of each eye
        Assert.Equal(Color.White, front.Get(22, 21));
        Assert.True(front.IsOpaque(20, 25));           // the mouth, under the gap between them

        var blink = new PixelCanvas(40, 58);
        CharacterFaces.Stamp(blink, new Vector2(20.4f, 20.6f), 0, style, Expression.Neutral, blink: true);
        Assert.Equal(4, Enumerable.Range(0, 40).Count(x => blink.IsOpaque(x, 21)));
        Assert.DoesNotContain(Enumerable.Range(0, 40), x => blink.IsOpaque(x, 20) || blink.IsOpaque(x, 22));

        var side = new PixelCanvas(40, 58);
        CharacterFaces.Stamp(side, new Vector2(26.2f, 20.6f), 1, style, Expression.Neutral, blink: false);
        Assert.Equal(2, Enumerable.Range(0, 40).Count(x => side.IsOpaque(x, 22)));

        var back = new PixelCanvas(40, 58);
        CharacterFaces.Stamp(back, new Vector2(20f, 20f), 2, style, Expression.Neutral, blink: false);
        Assert.DoesNotContain(Enumerable.Range(0, 40 * 58), i => back.IsOpaque(i % 40, i / 40));
    }

    [Fact]
    public void BattleFacesAreCutOutAroundTheirFeatures()
    {
        var rig = Player;
        var open = CharacterFaces.Paint(rig.Style, rig.Face, Expression.Neutral, blink: false);
        int n = CharacterFaces.TextureSize;
        Assert.False(open.IsOpaque(0, 0));
        Assert.False(open.IsOpaque(n / 2, n / 2));      // between the eyes the skin shows through

        // The eyes sit either side of the anchor, with a white highlight in each
        float k = n / (2f * rig.Face.TextureHalfSize);
        int ey = (int)(n * 0.5f - (rig.Face.Anchor.Y - rig.Face.TextureCenter.Y) * k);
        foreach (float side in new[] { -1f, 1f })
        {
            int ex = (int)(n * 0.5f + side * rig.Face.EyeHalfSpacing * k);
            Assert.True(open.IsOpaque(ex, ey));
            Assert.Contains(Enumerable.Range(ex - 8, 16), x => Enumerable.Range(ey - 8, 16).Any(y => open.Get(x, y).Equals(Color.White)));
        }
        var shut = CharacterFaces.Paint(rig.Style, rig.Face, Expression.Neutral, blink: true);
        Assert.DoesNotContain(Enumerable.Range(0, n * n), i => shut.Get(i % n, i / n).Equals(Color.White));
    }

    [Fact]
    public void HandDrawnFramesAreLookedForUnderStableNames()
    {
        Assert.Equal("down_walk_3.png", CharacterSprites.OverrideName(new SpriteKey(Player, 0, SpriteAnim.Walk, 3, false, Expression.Neutral)));
        Assert.Equal("left_idle_0_blink.png", CharacterSprites.OverrideName(new SpriteKey(Player, 3, SpriteAnim.Idle, 0, true, Expression.Neutral)));
        Assert.Equal("right_wave_2_happy.png", CharacterSprites.OverrideName(new SpriteKey(Player, 1, SpriteAnim.Wave, 2, false, Expression.Happy)));
    }

    [Fact]
    public void MaterialZeroKeepsThePlainLookForModelsWithoutMaterials()
    {
        // Pokémon carry no material ids yet, so they read material 0: the soft terminator they always had, no shine, no glow
        Assert.Equal(SurfaceMaterials.Count, SurfaceMaterials.LightTable.Length);
        Assert.Equal(SurfaceMaterials.Count, SurfaceMaterials.ShadeTable.Length);
        Assert.Equal(SurfaceMaterials.Count, Enum.GetValues<SurfaceMaterial>().Length);
        Assert.Equal(new Vector4(0.12f, 0f, 1f, 0f), SurfaceMaterials.LightTable[0]);
        Assert.Equal(Vector4.One, SurfaceMaterials.ShadeTable[0]);
        Assert.True(SurfaceMaterials.LightTable[(int)SurfaceMaterial.Glow].W > 0.5f);
    }

    [Fact]
    public void SkinningCarriesTheBodyWithItsBones()
    {
        // The same sum the shader does: a vertex moves with its bones, weighted
        var mesh = Player.Mesh;
        Player.Animate(new CharacterPose { Walk = 0.3125f, WalkBlend = 1f, Time = 0.4f });
        Vector3 Skinned(int v)
        {
            var p = Vector3.Zero;
            for (int k = 0; k < 4; k++)
                p += Vector3.Transform(mesh.Positions[v], Player.Skin[mesh.BoneIndices[v * 4 + k]]) * mesh.BoneWeights[v * 4 + k];
            return p;
        }
        // The left leg is ahead: its shoe has moved forward, the right one back
        int leftToe = Enumerable.Range(0, mesh.VertexCount).Where(v => mesh.Positions[v].X > 0.03f && mesh.Positions[v].Y < 0.05f).OrderByDescending(v => mesh.Positions[v].Z).First();
        int rightToe = Enumerable.Range(0, mesh.VertexCount).Where(v => mesh.Positions[v].X < -0.03f && mesh.Positions[v].Y < 0.05f).OrderByDescending(v => mesh.Positions[v].Z).First();
        Assert.True(Skinned(leftToe).Z > mesh.Positions[leftToe].Z + 0.05f);
        Assert.True(Skinned(rightToe).Z < mesh.Positions[rightToe].Z - 0.05f);
    }
}
