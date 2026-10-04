using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using Xunit.Abstractions;

namespace PokemonPlatinumTests;

/// <summary>
/// The model generator (plan 03 · D5): every species gets a sculpt of its own from its data, the same every time,
/// on the body plans the clips know, and one species of each body kind meshes into a sound model.
/// </summary>
public class PokemonGeneratorTests
{
    private readonly ITestOutputHelper output;

    public PokemonGeneratorTests(ITestOutputHelper output) => this.output = output;

    /// <summary>The sculpt's shapes, without its name, so two species can be compared.</summary>
    private static string Shapes(PokeBuilder b)
    {
        string text = b.Sdf.Describe();
        return text[(text.IndexOf('|') + 1)..];
    }

    [Fact]
    public void EverySpeciesHasADistinctModelOfItsOwn()
    {
        var all = PokemonDatabase.GetAll().OrderBy(s => s.DexNumber).ToList();
        Assert.Equal(1025, all.Count);
        var seen = new Dictionary<string, string>();
        var kinds = new Dictionary<BodyKind, int>();
        foreach (var species in all)
        {
            var g = PokemonGenomes.For(species.Name);
            Assert.NotNull(g);
            kinds[g!.Kind] = kinds.GetValueOrDefault(g.Kind) + 1;
            var b = PokemonGenerator.Build(g);
            var m = b.Model;
            Assert.Equal(species.Name, m.Species);
            Assert.InRange(m.Skeleton.Count, 2, SkinnedModel.MaxBones);
            Assert.Equal(PokeRole.Root, m.Bones[0].Role);
            Assert.Equal(PokeRole.Body, m.Bones[1].Role);
            Assert.True(m.Decals.Count(d => d.IsEye) >= 1, $"{species.Name} has no eyes");
            Assert.InRange(m.Fill, 0.45f, 1f);
            Assert.True(b.Sdf.Shapes.Any(s => s.Op == SdfOp.Add), species.Name);
            // Every bone moves at least one shape
            for (int bone = 1; bone < m.Skeleton.Count; bone++)
                Assert.True(b.Sdf.Shapes.Any(s => s.Bone == bone && s.Op == SdfOp.Add), $"{species.Name}: bone {m.Skeleton[bone].Name} has nothing on it");
            string shapes = Shapes(b);
            Assert.False(seen.TryGetValue(shapes, out var twin), $"{species.Name} is sculpted exactly like {twin}");
            seen[shapes] = species.Name;
        }
        output.WriteLine(string.Join(", ", kinds.OrderByDescending(k => k.Value).Select(k => $"{k.Key} {k.Value}")));
        // Every kind of body is used
        Assert.Equal(Enum.GetValues<BodyKind>().Length, kinds.Count);
    }

    [Fact]
    public void ASpeciesIsSculptedTheSameEveryTime()
    {
        foreach (var name in new[] { "Pikachu", "Onix", "Butterfree", "Voltorb", "Charizard", "Magikarp" })
            Assert.Equal(Shapes(PokemonGenerator.Build(name)!), Shapes(PokemonGenerator.Build(name)!));
    }

    [Fact]
    public void TheGenomeFollowsTheData()
    {
        PokeGenome G(string name) => PokemonGenomes.For(name)!;
        Assert.Equal(BodyKind.Insect, G("Butterfree").Kind);
        Assert.Equal(WingKind.Butterfly, G("Butterfree").Wings);
        Assert.Equal(PatternKind.TwoTone, G("Voltorb").Pattern);
        Assert.True(G("Onix").Rocky);
        Assert.Equal(BodyKind.Serpent, G("Onix").Kind);
        Assert.Equal(Special.Mound, G("Diglett").Special);
        Assert.Equal(WingKind.Dragon, G("Charizard").Wings);
        Assert.True(G("Magikarp").Hovers);
        Assert.True(G("Gastly").Hovers);
        Assert.False(G("Geodude").Hovers);
        Assert.Equal(MouthKind.Trunk, G("Phanpy").Mouth);
        Assert.Equal(BackKind.Shell, G("Squirtle").Back);
        Assert.Equal(BodyKind.Cocoon, G("Metapod").Kind);
        Assert.Equal(TopKind.None, G("Metapod").Top);
        Assert.Equal(-1, G("Pichu").Stage);
        Assert.Equal(2, G("Venusaur").Stage);
        // Small species fill less of their frame than big ones
        Assert.True(G("Joltik").Fill < G("Pikachu").Fill && G("Pikachu").Fill < G("Snorlax").Fill);
        // A family shares its build: the same kind of ears and the same shade of colour drift
        Assert.Equal(G("Vulpix").Ears, G("Ninetales").Ears);
    }

    /// <summary>One species of every body kind.</summary>
    public static IEnumerable<object[]> OneOfEachKind => new[]
    {
        ("Rattata", BodyKind.Quadruped), ("Charmander", BodyKind.Upright), ("Machoke", BodyKind.Humanoid), ("Oddish", BodyKind.Legs),
        ("Pidgey", BodyKind.Bird), ("Zubat", BodyKind.Bat), ("Butterfree", BodyKind.Insect), ("Ekans", BodyKind.Serpent),
        ("Caterpie", BodyKind.Crawler), ("Krabby", BodyKind.Arthropod), ("Magikarp", BodyKind.Fish), ("Voltorb", BodyKind.Ball),
        ("Grimer", BodyKind.Armed), ("Diglett", BodyKind.Blob), ("Magneton", BodyKind.Cluster), ("Metapod", BodyKind.Cocoon),
        ("Tentacool", BodyKind.Jelly), ("Octillery", BodyKind.Tentacled)
    }.Select(x => new object[] { x.Item1, x.Item2.ToString() });

    [Theory]
    [MemberData(nameof(OneOfEachKind))]
    public void EachBodyKindMeshesIntoASoundModel(string species, string kind)
    {
        Assert.Equal(kind, PokemonGenomes.For(species)!.Kind.ToString());
        var m = PokemonModels.Build(species);
        output.WriteLine($"{species} ({kind}): {m.Plan}, {m.Skeleton.Count} bones, {m.Mesh.VertexCount} vertices, {m.Decals.Count} decals");
        Assert.Null(m.Imported);
        Assert.Empty(Problems(m));
    }

    /// <summary>
    /// Every species' generated model, meshed and checked like the samples above. It takes a quarter of an hour the
    /// first time (the meshes are cached afterwards), so it runs only when POKEMON_MODELS_ALL=1.
    /// </summary>
    [Fact]
    public void EveryGeneratedModelMeshesSoundly()
    {
        if (Environment.GetEnvironmentVariable("POKEMON_MODELS_ALL") != "1") return;
        var problems = new System.Collections.Concurrent.ConcurrentBag<string>();
        var species = PokemonDatabase.GetAll().Select(s => s.Name).Where(n => !PokemonModels.HasModel(n)).ToList();
        System.Threading.Tasks.Parallel.ForEach(species, new System.Threading.Tasks.ParallelOptions { MaxDegreeOfParallelism = 2 }, name =>
        {
            foreach (var p in Problems(PokemonModels.Build(name))) problems.Add(p);
        });
        foreach (var p in problems.OrderBy(p => p)) output.WriteLine(p);
        Assert.Empty(problems);
    }

    /// <summary>What is wrong with a meshed model: weights, its place on the ground, loose pieces, empty bones, decals off the surface.</summary>
    private static List<string> Problems(PokeModel m)
    {
        var found = new List<string>();
        var mesh = m.Mesh;
        string species = m.Species;
        if (mesh.VertexCount is < 3000 or > 60000) found.Add($"{species}: {mesh.VertexCount} vertices");
        for (int v = 0; v < mesh.VertexCount; v++)
        {
            float sum = 0f;
            for (int k = 0; k < 4; k++)
            {
                sum += mesh.BoneWeights[v * 4 + k];
                if (mesh.BoneIndices[v * 4 + k] >= m.Skeleton.Count) found.Add($"{species}: a vertex names a bone that doesn't exist");
            }
            if (MathF.Abs(sum - 1f) > 1e-3f)
            {
                found.Add($"{species}: weights add up to {sum}");
                break;
            }
        }

        // On the ground, or a little above it for fliers and floaters
        float bottom = mesh.Positions.Min(p => p.Y);
        if (m.Hovers ? bottom < 0.06f * m.Height || bottom > 0.2f * m.Height : MathF.Abs(bottom) > 0.04f)
            found.Add($"{species}: its lowest point is at {bottom:F3} ({(m.Hovers ? "hovering" : "standing")})");

        // In one piece: a part that doesn't reach the body floats beside it (specks of a few cells, where a thin
        // claw or horn tip breaks up in the mesh, are left alone)
        var pieces = Pieces(mesh).Where(n => n > 40).ToList();
        if (pieces.Count > 1) found.Add($"{species}: in {pieces.Count} pieces of {string.Join(", ", pieces)} vertices");

        // Every bone moves some of the surface
        for (int b = 1; b < m.Skeleton.Count; b++)
        {
            int moved = Enumerable.Range(0, mesh.VertexCount).Count(v => mesh.WeightOf(v, b) > 0.5f);
            if (moved <= 20) found.Add($"{species}: bone {m.Skeleton[b].Name} carries only {moved} vertices");
        }

        // Every eye and marking lies whole on the surface (as for the hand-built models)
        var uvs = m.DecalUVs ?? Array.Empty<Vector2>();
        for (int d = 0; d < m.Decals.Count; d++)
        {
            int col = d % m.AtlasColumns, row = d / m.AtlasColumns;
            var center = new Vector2((col + 0.5f) / m.AtlasColumns, (row + 0.5f) / m.AtlasRows);
            var half = new Vector2(0.25f / m.AtlasColumns, 0.25f / m.AtlasRows);
            bool left = false, right = false, top = false, low = false;
            foreach (var uv in uvs)
            {
                if (MathF.Abs(uv.X - center.X) > 2f * half.X || MathF.Abs(uv.Y - center.Y) > 2f * half.Y) continue;
                left |= uv.X <= center.X - half.X;
                right |= uv.X >= center.X + half.X;
                top |= uv.Y <= center.Y - half.Y;
                low |= uv.Y >= center.Y + half.Y;
            }
            if (!(left && right && top && low)) found.Add($"{species}: decal {d} ({(m.Decals[d].IsEye ? "eye" : "mark")}) isn't covered");
        }

        // The clips move it without tearing it apart
        m.Animate(new PokePose { Time = 0.7f, Attack = 0.48f, Kind = MoveCategory.Physical });
        if (m.Skin.Any(skin => Vector3.Transform(Vector3.Zero, skin).Length() > 4f * m.Height + 1f)) found.Add($"{species}: a physical move flings a bone away");
        return found;
    }

    /// <summary>The number of vertices in each of the mesh's separate pieces, largest first.</summary>
    private static List<int> Pieces(SdfMesh mesh)
    {
        var parent = Enumerable.Range(0, mesh.VertexCount).ToArray();
        int Find(int v)
        {
            while (parent[v] != v) v = parent[v] = parent[parent[v]];
            return v;
        }
        for (int t = 0; t < mesh.Indices.Length; t += 3)
        {
            int a = Find(mesh.Indices[t]);
            for (int k = 1; k < 3; k++)
            {
                int r = Find(mesh.Indices[t + k]);
                if (r != a) parent[r] = a;
            }
        }
        return Enumerable.Range(0, mesh.VertexCount).GroupBy(Find).Select(p => p.Count()).OrderByDescending(n => n).ToList();
    }
}
