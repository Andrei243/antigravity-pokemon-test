using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using Xunit;
using static PokemonPlatinumTests.MeshPieces;

namespace PokemonPlatinumTests;

/// <summary>
/// Carpathia's sculpts, batch 3 (plan 46 · CP4, PokemonModels.Carpathia3.cs): Panusel to Cerbanor. Each is hand-built and
/// its own, in one piece, with two eyes and its decals covered, standing on its feet or floating as a flier does, and
/// taking its shiny palette by its number. The model tests walk every model; these hold the twenty alone.
/// </summary>
public class CarpathiaSculpts3Tests
{
    private readonly ITestOutputHelper output;

    public CarpathiaSculpts3Tests(ITestOutputHelper output) => this.output = output;

    private static readonly string[] Names =
    {
        "Panusel", "Panusa", "Melcut", "Melcobor", "Cartitel", "Cartiton", "Pupazel", "Pupazoi", "Noptila", "Fluturnopt",
        "Nevastel", "Nevastuica", "Urslet", "Ursabrun", "Lupling", "Lupodac", "Caprita", "Capraneagra", "Cerbel", "Cerbanor"
    };

    public static IEnumerable<object[]> Batch => Names.Select(n => new object[] { n });

    [Fact]
    public void EveryOneIsHandBuiltAndInTheData()
    {
        Assert.Equal(20, Names.Length);
        foreach (var n in Names)
        {
            var species = PokemonDatabase.Get(n);
            Assert.NotNull(species);
            Assert.InRange(species!.DexNumber, 10041, 10060);
            Assert.True(PokemonModels.HasModel(n), $"{n} has no sculpt of its own");
            Assert.Contains(n, PokemonModels.Species);
            Assert.Equal(n, PokemonModels.Build(n).Species);
        }
    }

    [Fact]
    public void EachIsASculptOfItsOwn()
    {
        var seen = new HashSet<string>();
        foreach (var n in Names) Assert.True(seen.Add(PokemonModels.Signature(n)), $"{n} shares its signature");
    }

    [Fact]
    public void EachTakesItsShinyPaletteByItsNumber()
    {
        foreach (var n in Names)
        {
            int number = PokemonDatabase.Get(n)!.DexNumber;
            Assert.Equal(number, ShinyPalettes.NumberOf(n));
            Assert.InRange(ShinyPalettes.AngleOf(number), 60f, 300f);
        }
        Assert.Equal(Names.Length, Names.Select(ShinyPalettes.Describe).Distinct().Count());
    }

    [Theory]
    [MemberData(nameof(Batch))]
    public void EachIsOneSkinnedBodyWithTwoEyesOnItsFeet(string species)
    {
        var m = PokemonModels.Build(species);
        var mesh = m.Mesh;
        output.WriteLine($"{species}: {m.Plan}, {m.Skeleton.Count} bones, {mesh.VertexCount} vertices, {m.Decals.Count} decals, height {m.Height:F2}");
        Assert.InRange(mesh.VertexCount, 4000, 60000);
        Assert.InRange(m.Shell.VertexCount, 1000, 60000);
        Assert.InRange(m.Skeleton.Count, 2, SkinnedModel.MaxBones);
        Assert.Equal(2, m.Decals.Count(d => d.IsEye));
        float bottom = mesh.Positions.Min(p => p.Y);
        if (m.Hovers && bottom > 0.04f) Assert.InRange(bottom, 0.06f * m.Height, 0.2f * m.Height);
        else Assert.InRange(bottom, -0.04f, 0.04f);
        for (int bone = 1; bone < m.Skeleton.Count; bone++)
        {
            int moved = Enumerable.Range(0, mesh.VertexCount).Count(v => mesh.WeightOf(v, bone) > 0.5f);
            Assert.True(moved > 20, $"{species}: bone {m.Skeleton[bone].Name} carries only {moved} vertices");
        }
        var pieces = Pieces(mesh).Where(n => n > Speck).ToList();
        Assert.True(pieces.Count == 1, $"{species} is in {pieces.Count} pieces of {string.Join(", ", pieces)} vertices");
    }

    [Fact]
    public void EachDecalOfTheseCoversItsWholeSquare()
    {
        // As PokemonModelTests.EachDecalCoversItsWholeSquare, for these twenty alone (that test walks every model)
        var uncovered = new List<string>();
        foreach (var n in Names)
        {
            var m = PokemonModels.Get(n);
            var uvs = m.DecalUVs!;
            for (int d = 0; d < m.Decals.Count; d++)
            {
                int col = d % m.AtlasColumns, row = d / m.AtlasColumns;
                var center = new System.Numerics.Vector2((col + 0.5f) / m.AtlasColumns, (row + 0.5f) / m.AtlasRows);
                var half = new System.Numerics.Vector2(0.25f / m.AtlasColumns, 0.25f / m.AtlasRows);
                bool left = false, right = false, top = false, bottom = false;
                foreach (var uv in uvs)
                {
                    if (MathF.Abs(uv.X - center.X) > 2f * half.X || MathF.Abs(uv.Y - center.Y) > 2f * half.Y) continue;
                    left |= uv.X <= center.X - half.X;
                    right |= uv.X >= center.X + half.X;
                    top |= uv.Y <= center.Y - half.Y;
                    bottom |= uv.Y >= center.Y + half.Y;
                }
                if (!(left && right && top && bottom)) uncovered.Add($"{n}: decal {d} ({(m.Decals[d].IsEye ? "eye" : "mark")})");
            }
        }
        Assert.True(uncovered.Count == 0, "not covered: " + string.Join(", ", uncovered));
    }

    [Fact]
    public void EachLineGrowsFromItsFirstStage()
    {
        foreach (var (small, big) in new[] { ("Panusel", "Panusa"), ("Melcut", "Melcobor"), ("Cartitel", "Cartiton"), ("Pupazel", "Pupazoi"),
            ("Noptila", "Fluturnopt"), ("Nevastel", "Nevastuica"), ("Urslet", "Ursabrun"), ("Lupling", "Lupodac"), ("Caprita", "Capraneagra"), ("Cerbel", "Cerbanor") })
        {
            Assert.True(PokemonModels.Build(big).Height > PokemonModels.Build(small).Height, $"{big} isn't taller than {small}");
            Assert.True(PokemonModels.Build(big).Fill > PokemonModels.Build(small).Fill, $"{big} doesn't fill more of its frame than {small}");
        }
    }
}
