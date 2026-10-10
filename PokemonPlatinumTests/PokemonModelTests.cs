using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using Raylib_cs;
using static PokemonPlatinumTests.MeshPieces;

namespace PokemonPlatinumTests;

/// <summary>Pokémon models, version 2 (plan 04 · G7): the SDF bodies, their skeletons, decals and clips.</summary>
public class PokemonModelTests
{
    private readonly ITestOutputHelper output;

    public PokemonModelTests(ITestOutputHelper output) => this.output = output;

    /// <summary>Every hand-built model: the species, and the forms with models of their own.</summary>
    private static IEnumerable<string> AllHandBuilt => PokemonModels.Species.Concat(PokemonModels.Forms);

    public static IEnumerable<object[]> HandBuilt => AllHandBuilt.Select(s => new object[] { s });

    /// <summary>The hand-built species and forms that don't have two eyes (every Unown has one, Mega Gengar three).</summary>
    private static readonly Dictionary<string, int> EyesOf = new Dictionary<string, int>
    {
        ["Zubat"] = 0, ["Combee"] = 6, ["Unown"] = 1, ["Nosepass"] = 0, ["Ralts"] = 0, ["Kirlia"] = 1, ["Magnemite"] = 1, ["Magneton"] = 3,
        ["Magnezone"] = 3, ["Yanmega"] = 0, ["Duskull"] = 1, ["Dusclops"] = 1, ["Dusknoir"] = 1, ["Piloswine"] = 0, ["Gengar-Mega"] = 3,
        ["Dugtrio"] = 6, ["Dugtrio-Alola"] = 6, ["Doduo"] = 4, ["Dodrio"] = 6,
        ["Exeggcute"] = 12, ["Exeggutor"] = 6, ["Exeggutor-Alola"] = 8, ["Weezing"] = 4, ["Weezing-Galar"] = 4, ["Kangaskhan"] = 4,
        ["Kangaskhan-Mega"] = 4, ["Staryu"] = 0, ["Starmie"] = 0, ["Starmie-Mega"] = 0, ["Chinchou"] = 0, ["Lunatone"] = 1, ["Claydol"] = 7,
        ["Beldum"] = 1, ["Regirock"] = 0, ["Regice"] = 0, ["Registeel"] = 0, ["Roggenrola"] = 0, ["Boldore"] = 0, ["Sigilyph"] = 1, ["Vanilluxe"] = 4, ["Klang"] = 1, ["Klinklang"] = 1, ["Litwick"] = 1, ["Deino"] = 0, ["Zweilous"] = 0,
        ["Honedge"] = 1, ["Aegislash"] = 1, ["Aegislash-Blade"] = 1,
        ["Binacle"] = 4, ["Trevenant"] = 1,
        ["Type: Null"] = 0, ["Dhelmise"] = 1,
        ["Nihilego"] = 0, ["Xurkitree"] = 0, ["Celesteela"] = 0, ["Kartana"] = 0, ["Blacephalon"] = 0, ["Meltan"] = 1, ["Melmetal"] = 1,
        ["Melmetal-Gmax"] = 1, ["Regigigas"] = 0, ["Darkrai"] = 1, ["Darkrai-Mega"] = 1, ["Cinderace-Gmax"] = 0, ["Inteleon-Gmax"] = 0,
        ["Rolycoly"] = 1, ["Flapple-Gmax"] = 0, ["Sandaconda-Gmax"] = 0,
        ["Runerigus"] = 1, ["Falinks"] = 12, ["Alcremie-Gmax"] = 0,
        ["Drakloak"] = 4, ["Dragapult"] = 6, ["Eternatus"] = 0, ["Eternatus-Eternamax"] = 0, ["Regieleki"] = 0, ["Regidrago"] = 0, ["Calyrex-Ice"] = 4,
        ["Calyrex-Shadow"] = 4,
        ["Tandemaus"] = 4, ["Maushold"] = 8, ["Maushold-Family-Of-Three"] = 6, ["Nacli"] = 0, ["Naclstack"] = 0, ["Garganacl"] = 0, ["Charcadet"] = 1, ["Bramblin"] = 0,
        ["Brambleghast"] = 0, ["Scovillain"] = 4, ["Scovillain-Mega"] = 4, ["Wugtrio"] = 6, ["Orthworm"] = 0, ["Greavard"] = 0,
        ["Houndstone"] = 0,
        ["Sandy Shocks"] = 3, ["Iron Jugulis"] = 6
    }.Concat(PokemonModels.Forms.Where(f => f.StartsWith("Unown-")).Select(f => KeyValuePair.Create(f, 1))).ToDictionary(e => e.Key, e => e.Value);

    /// <summary>
    /// The hand-built species and forms whose parts float apart by design, and how many pieces they are in: Haunter's
    /// two hands, Probopass's two little noses, Porygon-Z's head, arms and tail round its body, the five spoons hovering
    /// over Mega Alakazam's head, the four hands floating free about Hoopa Unbound.
    /// </summary>
    private static readonly Dictionary<string, int> PiecesOf = new()
    {
        ["Haunter"] = 3, ["Probopass"] = 3, ["Porygon-Z"] = 5, ["Alakazam-Mega"] = 6, ["Shedinja"] = 2, ["Hoopa-Unbound"] = 5
    };

    [Theory]
    [MemberData(nameof(HandBuilt))]
    public void EverySpeciesIsOneSmoothSkinnedBody(string species)
    {
        var sw = Stopwatch.StartNew();
        var m = PokemonModels.Build(species);
        var mesh = m.Mesh;
        output.WriteLine($"{species}: {m.Plan}, {m.Skeleton.Count} bones, {mesh.VertexCount} vertices, shell {m.Shell.VertexCount}, " +
            $"{m.Decals.Count} decals on {m.DecalPatch?.TriangleCount} triangles, {sw.ElapsedMilliseconds} ms");

        Assert.InRange(mesh.VertexCount, 4000, 60000);
        Assert.InRange(m.Skeleton.Count, 2, SkinnedModel.MaxBones);
        Assert.InRange(m.Shell.VertexCount, 1000, 60000);
        for (int v = 0; v < mesh.VertexCount; v++)
        {
            float sum = 0f;
            for (int k = 0; k < 4; k++)
            {
                sum += mesh.BoneWeights[v * 4 + k];
                Assert.True(mesh.BoneIndices[v * 4 + k] < m.Skeleton.Count);
            }
            Assert.Equal(1f, sum, 3);
        }

        // Feet on the ground, or a flier floating a little above it as the generated ones do
        float bottom = mesh.Positions.Min(p => p.Y);
        if (m.Hovers && bottom > 0.04f) Assert.InRange(bottom, 0.06f * m.Height, 0.2f * m.Height);
        else Assert.InRange(bottom, -0.04f, 0.04f);

        // Every bone moves some of the surface
        for (int b = 1; b < m.Skeleton.Count; b++)
        {
            int moved = Enumerable.Range(0, mesh.VertexCount).Count(v => mesh.WeightOf(v, b) > 0.5f);
            Assert.True(moved > 20, $"{species}: bone {m.Skeleton[b].Name} carries only {moved} vertices");
        }

        // Two eyes (Zubat has none, Combee a pair on each of its three faces), each laid on the head over enough
        // triangles to show it whole
        Assert.Equal(EyesOf.GetValueOrDefault(species, 2), m.Decals.Count(d => d.IsEye));
        if (m.Decals.Count == 0) return;
        Assert.NotNull(m.DecalPatch);
        Assert.All(m.DecalUVs!, uv => Assert.True(uv.X >= 0f && uv.X <= 1f && uv.Y >= 0f && uv.Y <= 1f));
    }

    [Theory]
    [MemberData(nameof(HandBuilt))]
    public void EverySpeciesIsInOnePiece(string species)
    {
        // A part that doesn't reach the body floats beside it, as feet under a body or a spoon's bowl past its handle did
        var pieces = Pieces(PokemonModels.Get(species).Mesh).Where(n => n > Speck).ToList();
        Assert.True(pieces.Count == PiecesOf.GetValueOrDefault(species, 1), $"{species} is in {pieces.Count} pieces of {string.Join(", ", pieces)} vertices");
    }

    [Fact]
    public void EachDecalCoversItsWholeSquare()
    {
        // The middle of every eye and marking lies on triangles of the patch: none falls off its surface
        foreach (var species in AllHandBuilt)
        {
            var m = PokemonModels.Get(species);
            var patch = m.DecalPatch!;
            for (int d = 0; d < m.Decals.Count; d++)
            {
                int col = d % m.AtlasColumns, row = d / m.AtlasColumns;
                var center = new Vector2((col + 0.5f) / m.AtlasColumns, (row + 0.5f) / m.AtlasRows);
                var cellHalf = new Vector2(0.25f / m.AtlasColumns, 0.25f / m.AtlasRows);
                // Triangles of this decal reach all four sides of the middle half of its cell
                var uvs = m.DecalUVs!;
                bool left = false, right = false, top = false, bottom = false;
                for (int i = 0; i < uvs.Length; i++)
                {
                    var uv = uvs[i];
                    if (MathF.Abs(uv.X - center.X) > 2f * cellHalf.X || MathF.Abs(uv.Y - center.Y) > 2f * cellHalf.Y) continue;
                    left |= uv.X <= center.X - cellHalf.X;
                    right |= uv.X >= center.X + cellHalf.X;
                    top |= uv.Y <= center.Y - cellHalf.Y;
                    bottom |= uv.Y >= center.Y + cellHalf.Y;
                }
                Assert.True(left && right && top && bottom, $"{species}: decal {d} ({(m.Decals[d].IsEye ? "eye" : "mark")}) isn't covered");
            }
        }
    }

    [Theory]
    [InlineData("Biped")]
    [InlineData("Quadruped")]
    [InlineData("Bird")]
    [InlineData("Serpent")]
    [InlineData("Fish")]
    [InlineData("Floating")]
    public void EveryBodyPlanHasItsSkeleton(string planName)
    {
        var plan = Enum.Parse<BodyPlan>(planName);
        var m = PokemonModels.Sample(plan);
        Assert.Equal(plan, m.Plan);
        int Count(PokeRole r) => m.Bones.Count(b => b.Role == r);
        Assert.Equal(PokeRole.Root, m.Bones[0].Role);
        Assert.Equal(PokeRole.Body, m.Bones[1].Role);
        switch (plan)
        {
            case BodyPlan.Biped:
                Assert.Equal(2, Count(PokeRole.Leg));
                Assert.Equal(2, Count(PokeRole.Arm));
                Assert.Equal(1, Count(PokeRole.Head));
                break;
            case BodyPlan.Quadruped:
                Assert.Equal(2, m.Bones.Count(b => b.Role == PokeRole.Leg && b.Front));
                Assert.Equal(2, m.Bones.Count(b => b.Role == PokeRole.Leg && !b.Front));
                break;
            case BodyPlan.Bird:
                Assert.Equal(2, Count(PokeRole.Wing));
                Assert.True(m.Hovers);
                break;
            case BodyPlan.Serpent:
                Assert.True(Count(PokeRole.Segment) >= 3);
                Assert.Equal(1, Count(PokeRole.Head));
                break;
            case BodyPlan.Fish:
                Assert.True(Count(PokeRole.Fin) >= 3);
                Assert.True(m.Hovers);
                break;
            case BodyPlan.Floating:
                Assert.True(m.Hovers);
                break;
        }
        // Legs hang from the root, so the body can move above planted feet
        foreach (var (info, b) in m.Bones.Select((info, b) => (info, b)).Where(x => x.info.Role == PokeRole.Leg))
            Assert.Equal(0, m.Skeleton[b].Parent);
    }

    /// <summary>Where a bone's joint is in the pose last animated.</summary>
    private static Vector3 Joint(PokeModel m, int b)
    {
        int parent = m.Skeleton[b].Parent;
        return parent < 0 ? Vector3.Transform(m.Skeleton[b].Joint, m.Skin[b]) : Vector3.Transform(m.Skeleton[b].Joint, m.Skin[parent]);
    }

    /// <summary>A point of bone b's sculpted geometry, where the pose last animated took it.</summary>
    private static Vector3 Point(PokeModel m, int b, Vector3 bind) => Vector3.Transform(bind, m.Skin[b]);

    /// <summary>The middle of the vertices bone b carries, at rest.</summary>
    private static Vector3 Centre(PokeModel m, int b)
    {
        var sum = Vector3.Zero;
        int n = 0;
        for (int v = 0; v < m.Mesh.VertexCount; v++)
            if (m.Mesh.WeightOf(v, b) > 0.9f) { sum += m.Mesh.Positions[v]; n++; }
        return n > 0 ? sum / n : m.Skeleton[b].Joint;
    }

    [Theory]
    [InlineData("Riolu")]
    [InlineData("Shinx")]
    [InlineData("Turtwig")]
    public void StandingFeetStayPlantedWhileIdle(string species)
    {
        var m = PokemonModels.Get(species);
        foreach (float t in new[] { 0.3f, 0.9f, 1.7f, 2.6f })
        {
            m.Animate(new PokePose { Time = t });
            for (int b = 0; b < m.Bones.Count; b++)
            {
                if (m.Bones[b].Role != PokeRole.Leg) continue;
                var foot = Centre(m, b);
                Assert.True(Vector3.Distance(Point(m, b, foot), foot) < 0.002f, $"{species}: a foot slides at {t} s");
            }
        }
    }

    [Theory]
    [InlineData("Riolu")]
    [InlineData("Shinx")]
    [InlineData("Starly")]
    [InlineData("Gible")]
    public void APhysicalMoveDrawsBackThenStrikesForward(string species)
    {
        var m = PokemonModels.Get(species);
        int head = m.Find(PokeRole.Head);
        var rest = Centre(m, head);
        m.Animate(new PokePose { Time = 0f, Attack = 0.26f, Kind = MoveCategory.Physical });
        var drawn = Point(m, head, rest);
        m.Animate(new PokePose { Time = 0f, Attack = 0.48f, Kind = MoveCategory.Physical });
        var struck = Point(m, head, rest);
        Assert.True(struck.Z > drawn.Z + 0.02f * m.Height, $"{species}: the head doesn't come forward ({drawn.Z:F3} to {struck.Z:F3})");
    }

    [Theory]
    [InlineData("Riolu")]
    [InlineData("Shinx")]
    [InlineData("Piplup")]
    public void ASpecialMoveRearsUpWithoutLunging(string species)
    {
        var m = PokemonModels.Get(species);
        int head = m.Find(PokeRole.Head);
        var rest = Centre(m, head);
        m.Animate(new PokePose { Time = 0f });
        var idle = Point(m, head, rest);
        m.Animate(new PokePose { Time = 0f, Attack = 0.36f, Kind = MoveCategory.Special });
        var charged = Point(m, head, rest);
        Assert.True(charged.Y > idle.Y, $"{species}: the head doesn't rise as it gathers power");
        Assert.True(charged.Z < idle.Z + 0.01f, $"{species}: it leans in while gathering power");
    }

    [Theory]
    [InlineData("Riolu")]
    [InlineData("Shinx")]
    [InlineData("Starly")]
    [InlineData("Turtwig")]
    [InlineData("Giratina")]
    public void FaintingBringsTheHeadDown(string species)
    {
        var m = PokemonModels.Get(species);
        int head = m.Find(PokeRole.Head);
        var rest = Centre(m, head);
        m.Animate(new PokePose { Time = 0f });
        float up = Point(m, head, rest).Y;
        m.Animate(new PokePose { Time = 0f, Faint = 1f });
        float down = Point(m, head, rest).Y;
        Assert.True(down < up - 0.15f * m.Height, $"{species}: the head only drops from {up:F2} to {down:F2}");
    }

    [Fact]
    public void AHitKnocksTheBodyBack()
    {
        var m = PokemonModels.Get("Lucario");
        int head = m.Find(PokeRole.Head);
        var rest = Centre(m, head);
        m.Animate(new PokePose { Time = 0f });
        var idle = Point(m, head, rest);
        m.Animate(new PokePose { Time = 0f, Hurt = 0.12f });
        Assert.True(Point(m, head, rest).Z < idle.Z - 0.02f * m.Height);
    }

    [Theory]
    [InlineData("Physical")]
    [InlineData("Special")]
    [InlineData("Status")]
    public void MovesStartAndEndInTheIdlePose(string kind)
    {
        var category = Enum.Parse<MoveCategory>(kind);
        var m = PokemonModels.Get("Infernape");
        var idle = new PokePose { Time = 1.3f };
        m.Animate(idle);
        var rest = m.Skin.ToArray();
        foreach (float a in new[] { 0.001f, 0.999f })
        {
            m.Animate(new PokePose { Time = 1.3f, Attack = a, Kind = category });
            for (int b = 0; b < m.Skin.Length; b++)
                Assert.True(Vector3.Distance(Vector3.Transform(Vector3.One, m.Skin[b]), Vector3.Transform(Vector3.One, rest[b])) < 0.01f, $"{kind} at {a}");
        }
    }

    [Fact]
    public void TheEyesFollowWhatIsHappening()
    {
        Assert.Equal(EyeState.Open, PokemonAnimation.EyesOf(new PokePose()));
        Assert.Equal(EyeState.Shut, PokemonAnimation.EyesOf(new PokePose { Blink = 1f }));
        Assert.Equal(EyeState.Squeeze, PokemonAnimation.EyesOf(new PokePose { Hurt = 0.3f }));
        Assert.Equal(EyeState.Fierce, PokemonAnimation.EyesOf(new PokePose { Attack = 0.4f, Kind = MoveCategory.Physical }));
        Assert.Equal(EyeState.Open, PokemonAnimation.EyesOf(new PokePose { Attack = 0.4f, Kind = MoveCategory.Status }));
        Assert.Equal(EyeState.Shut, PokemonAnimation.EyesOf(new PokePose { Faint = 0.8f, Hurt = 0.3f }));
    }

    [Fact]
    public void EveryEyeStateIsPaintedOnTheAtlas()
    {
        var m = PokemonModels.Get("Piplup");
        // Opaque pixels in the eyes' cells (the chest markings are the same in every state)
        int Opaque(EyeState state)
        {
            var c = PokemonDecals.Paint(m, state);
            int n = 0;
            for (int d = 0; d < m.Decals.Count; d++)
            {
                if (!m.Decals[d].IsEye) continue;
                int x0 = d % m.AtlasColumns * PokemonDecals.Cell, y0 = d / m.AtlasColumns * PokemonDecals.Cell;
                for (int y = y0; y < y0 + PokemonDecals.Cell; y++)
                    for (int x = x0; x < x0 + PokemonDecals.Cell; x++)
                        if (c.Get(x, y).A >= 128) n++;
            }
            return n;
        }
        int open = Opaque(EyeState.Open), shut = Opaque(EyeState.Shut), squeeze = Opaque(EyeState.Squeeze), fierce = Opaque(EyeState.Fierce);
        output.WriteLine($"open {open}, shut {shut}, squeeze {squeeze}, fierce {fierce}");
        Assert.True(open > 2000);
        Assert.InRange(shut, 100, open / 2);
        Assert.InRange(squeeze, 100, open / 2);
        Assert.True(fierce > open / 2);
    }

    [Fact]
    public void StretchedShapesKeepTheirEndsAndFlatten()
    {
        // A spike squashed into a blade still runs from its base to its tip, but is thinner one way than the other
        var b = new PokeBuilder("Stretch Test", 0.6f, BodyPlan.Biped, new Vector3(0, 0.3f, 0));
        var spike = b.Spike(PokeBuilder.Body, new Vector3(0, 0.2f, 0), new Vector3(0, 0.6f, 0), 0.08f, Color.Red, 0.4f);
        spike.Prepare();
        Assert.True(spike.Distance(new Vector3(0, 0.598f, 0)) < 0.005f);
        Assert.True(spike.Distance(new Vector3(0.07f, 0.22f, 0)) < 0f);
        Assert.True(spike.Distance(new Vector3(0, 0.22f, 0.07f)) > 0f);
    }

    [Fact]
    public void ModelsAreBuiltTheSameEveryTime()
    {
        // The mesh cache is keyed by the model's description, so the same species must describe itself the same way
        Assert.Equal(PokemonModels.Build("Gible").Mesh.VertexCount, PokemonModels.Build("Gible").Mesh.VertexCount);
    }
}
