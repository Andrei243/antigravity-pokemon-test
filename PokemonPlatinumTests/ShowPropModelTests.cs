using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models.StageShow;

namespace PokemonPlatinumTests;

/// <summary>
/// The stage show's props as sculpts and where they sit (plan 42 · PS1; style guide, "Pokémon (version 2, G7)", "Props
/// worn"): every piece of the table sculpted and meshed, and on a species of each body plan and a few generated body
/// kinds the anchors find every slot the body has, each on a vertex of its role, none it lacks, and the same twice.
/// </summary>
public class ShowPropModelTests
{
    public static IEnumerable<object[]> Props => ShowPropDatabase.All.Select(p => new object[] { p.Id });

    [Theory]
    [MemberData(nameof(Props))]
    public void EveryPieceHasASlotAMoodATrickAndASculpt(string id)
    {
        var prop = ShowPropDatabase.Get(id)!;
        Assert.True(Enum.IsDefined(prop.Slot), $"{id}'s slot");
        Assert.True(Enum.IsDefined(prop.Mood), $"{id}'s mood");
        Assert.True(Enum.IsDefined(prop.Trick), $"{id}'s trick");
        Assert.Equal(2, prop.Colors.Count);
        foreach (var c in prop.Colors) ShowPropDatabase.ColourOf(c);
        Assert.True(ShowPropModels.Knows(prop.Shape), $"{id}'s shape '{prop.Shape}' has no sculpt");
        var mesh = ShowPropModels.Mesh(prop);
        Assert.True(mesh.Body.VertexCount > 100, $"{id} meshed to {mesh.Body.VertexCount} vertices");
        Assert.True(mesh.Body.VertexCount < 60000, $"{id} meshed to {mesh.Body.VertexCount} vertices");
        Assert.True(mesh.Shell.VertexCount > 0, $"{id}'s outline");
        // A piece of a single bone, every point a number
        Assert.All(mesh.Body.Positions, p => Assert.False(float.IsNaN(p.X) || float.IsNaN(p.Y) || float.IsNaN(p.Z)));
        Assert.All(mesh.Body.BoneIndices, b => Assert.Equal(0, b));
    }

    [Fact]
    public void EveryShapeOfTheTableIsSculptedAndEverySculptIsWorn()
    {
        var used = ShowPropDatabase.All.Select(p => p.Shape).Distinct().ToList();
        Assert.DoesNotContain(used, s => !ShowPropModels.Knows(s));
        Assert.DoesNotContain(ShowPropModels.Shapes, s => !used.Contains(s));
    }

    [Fact]
    public void TwoColourwaysOfAShapeAreKeptUnderNamesOfTheirOwn()
    {
        var black = ShowPropModels.Sculpt(ShowPropDatabase.Get("top_hat_black")!);
        var white = ShowPropModels.Sculpt(ShowPropDatabase.Get("top_hat_white")!);
        Assert.NotEqual(black.Name, white.Name);
        Assert.NotEqual(SdfCache.Key(black, ShowPropModels.Cell), SdfCache.Key(white, ShowPropModels.Cell));
    }

    // ------------------------------------------------------------------ anchors

    /// <summary>One species of each body plan, sculpted by hand, and the samples of the plans the story has few of.</summary>
    private static IEnumerable<(string Name, PokeModel Model)> Bodies()
    {
        foreach (var species in new[] { "Riolu", "Machop", "Shinx", "Turtwig", "Starly", "Magikarp", "Bronzor", "Onix" })
            yield return (species, PokemonModels.Build(species));
        foreach (var plan in new[] { BodyPlan.Serpent, BodyPlan.Fish, BodyPlan.Floating })
            yield return ("sample " + plan, PokemonModels.Sample(plan));
    }

    /// <summary>The first species of a few body kinds, generated from its data as a species with no sculpt would be.</summary>
    private static IEnumerable<(string Name, PokeModel Model)> Generated()
    {
        var wanted = new HashSet<BodyKind> { BodyKind.Quadruped, BodyKind.Humanoid, BodyKind.Bird, BodyKind.Fish, BodyKind.Serpent, BodyKind.Ball };
        var seen = new HashSet<BodyKind>();
        foreach (var sp in PokemonDatabase.GetAll().OrderBy(s => s.DexNumber))
        {
            if (seen.Count == wanted.Count) yield break;
            if (PokemonGenomes.For(sp.Name) is not { } genome || !wanted.Contains(genome.Kind) || !seen.Add(genome.Kind)) continue;
            yield return ($"{sp.Name} ({genome.Kind}, generated)", PokemonModels.Finish(PokemonGenerator.Build(genome)));
        }
    }

    private static bool HasEnough(PokeModel m, PokeRole role, int least) =>
        Enumerable.Range(0, m.Bones.Count).Any(b => m.Bones[b].Role == role && PropAnchors.On(m, b).Count >= least);

    [Fact]
    public void TheAnchorsFindTheSlotsEachBodyHasAndNoneItLacks()
    {
        foreach (var (name, m) in Bodies().Concat(Generated()))
        {
            var slots = PropAnchors.SlotsOf(m);
            bool hasEyes = m.Decals.Any(d => d.IsEye);
            bool hasHead = HasEnough(m, PokeRole.Head, PropAnchors.LeastHead);
            Assert.Equal(hasHead || hasEyes, slots.Contains(PropSlot.Head));
            Assert.Equal(hasHead || hasEyes, slots.Contains(PropSlot.Face));
            Assert.Equal(HasEnough(m, PokeRole.Arm, PropAnchors.LeastLimb), slots.Contains(PropSlot.Hand));
            Assert.Equal(HasEnough(m, PokeRole.Tail, PropAnchors.LeastLimb), slots.Contains(PropSlot.Tail));
            Assert.True(slots.Contains(PropSlot.Back), $"{name} has no back");
            if (!hasHead) Assert.DoesNotContain(PropSlot.Neck, slots);
            // A hand only on a body with arms: never on a fish, a quadruped or a bird
            if (m.Plan is BodyPlan.Fish or BodyPlan.Quadruped or BodyPlan.Bird)
                Assert.False(slots.Contains(PropSlot.Hand) && !HasEnough(m, PokeRole.Arm, PropAnchors.LeastLimb), $"{name} holds something");
        }
    }

    [Fact]
    public void TheBodiesThatWalkOfferAHeadAFaceANeckAndABack()
    {
        foreach (var species in new[] { "Riolu", "Shinx", "Starly", "Machop" })
        {
            var slots = PropAnchors.SlotsOf(PokemonModels.Build(species));
            foreach (var slot in new[] { PropSlot.Head, PropSlot.Face, PropSlot.Neck, PropSlot.Back })
                Assert.True(slots.Contains(slot), $"{species} offers no {slot}: {string.Join(", ", slots)}");
        }
        Assert.Contains(PropSlot.Hand, PropAnchors.SlotsOf(PokemonModels.Build("Riolu")));
        Assert.Contains(PropSlot.Tail, PropAnchors.SlotsOf(PokemonModels.Build("Shinx")));
        Assert.DoesNotContain(PropSlot.Hand, PropAnchors.SlotsOf(PokemonModels.Build("Magikarp")));
        Assert.DoesNotContain(PropSlot.Hand, PropAnchors.SlotsOf(PokemonModels.Sample(BodyPlan.Fish)));
        Assert.DoesNotContain(PropSlot.Hand, PropAnchors.SlotsOf(PokemonModels.Build("Shinx")));
    }

    [Fact]
    public void EveryAnchorLiesOnAVertexOfItsRole()
    {
        foreach (var (name, m) in Bodies().Concat(Generated()))
        {
            var eyeBones = m.Decals.Where(d => d.IsEye).Select(d => d.Bone).ToHashSet();
            foreach (var (slot, a) in PropAnchors.Of(m))
            {
                var role = m.Bones[a.Bone].Role;
                bool right = slot switch
                {
                    PropSlot.Head => role == PokeRole.Head || eyeBones.Contains(a.Bone),
                    PropSlot.Face => role == PokeRole.Head || eyeBones.Contains(a.Bone),
                    PropSlot.Neck => role == PokeRole.Head,
                    PropSlot.Back => role == PokeRole.Body,
                    PropSlot.Hand => role == PokeRole.Arm,
                    PropSlot.Tail => role == PokeRole.Tail,
                    _ => false,
                };
                Assert.True(right, $"{name}'s {slot} is on a bone of {role}");
                var verts = PropAnchors.On(m, a.Bone);
                float nearest = verts.Min(v => Vector3.Distance(m.Mesh.Positions[v], a.Position));
                Assert.True(nearest <= m.Cell, $"{name}'s {slot} stands {nearest:0.0000} from its bone's surface (a cell is {m.Cell:0.0000})");
                Assert.True(a.Size > 0f && a.Size < m.Height * 2f, $"{name}'s {slot} is sized {a.Size}");
                // Its frame is whole and right-handed (the outline culls by it)
                Assert.InRange(a.Up.Length(), 0.99f, 1.01f);
                Assert.InRange(a.Forward.Length(), 0.99f, 1.01f);
                Assert.InRange(MathF.Abs(Vector3.Dot(a.Up, a.Forward)), 0f, 0.01f);
                foreach (var shape in ShowPropModels.Shapes)
                    Assert.True(PropAnchors.Placement(a, shape).GetDeterminant() > 0f, $"{name}'s {slot} turns {shape} inside out");
                if (slot == PropSlot.Neck) Assert.True(a.Ring.X > 0f && a.Ring.Y > 0f, $"{name}'s neck has no ring");
            }
        }
    }

    [Fact]
    public void AModelBuiltTwiceHasTheSameAnchors()
    {
        foreach (var species in new[] { "Riolu", "Shinx", "Starly", "Magikarp", "Bronzor" })
        {
            var first = PropAnchors.Of(PokemonModels.Build(species));
            var second = PropAnchors.Of(PokemonModels.Build(species));
            Assert.Equal(first.Keys.OrderBy(k => k), second.Keys.OrderBy(k => k));
            foreach (var (slot, a) in first) Assert.Equal(a, second[slot]);
        }
    }

    [Fact]
    public void AHatSitsOnTheTopOfTheHead()
    {
        var m = PokemonModels.Build("Riolu");
        var a = PropAnchors.Of(m)[PropSlot.Head];
        // The piece's origin is the anchor, and the anchor is near the top of the head
        Assert.Equal(a.Position, Vector3.Transform(Vector3.Zero, PropAnchors.Placement(a, "top_hat")));
        int head = m.Find(PokeRole.Head);
        float top = PropAnchors.On(m, head).Max(v => m.Mesh.Positions[v].Y);
        float bottom = PropAnchors.On(m, head).Min(v => m.Mesh.Positions[v].Y);
        Assert.True(a.Position.Y > bottom + 0.6f * (top - bottom), $"the hat sits at {a.Position.Y}, the head runs {bottom} to {top}");
        // A neck ring goes round the seam's middle, scaled to its width
        var neck = PropAnchors.Of(m)[PropSlot.Neck];
        var ringFront = Vector3.Transform(new Vector3(0, 0, 1), PropAnchors.Placement(neck, "scarf"));
        Assert.True(MathF.Abs(ringFront.Z - (neck.Center.Z + neck.Ring.Y * 1.08f)) < 1e-4f);
    }
}
