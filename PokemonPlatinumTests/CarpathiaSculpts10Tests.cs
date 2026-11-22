using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using Xunit;
using static PokemonPlatinumTests.MeshPieces;

namespace PokemonPlatinumTests;

/// <summary>
/// Carpathia's sculpts, batch 10 (plan 46 · CP4, PokemonModels.Carpathia10.cs): Stimara to Tramvaior. Each species is
/// hand-built, a sculpt of its own, in one piece, on its feet or hovering as the hovering ones do, with two eyes and its
/// decals covered, and takes its shiny palette by the rule of its number. These hold for the twenty alone what the model
/// tests hold for every model (those walk all of them).
/// </summary>
public class CarpathiaSculpts10Tests
{
    private readonly ITestOutputHelper output;

    public CarpathiaSculpts10Tests(ITestOutputHelper output) => this.output = output;

    private static readonly string[] Names =
    {
        "Stimara", "Stimalaur", "Ghiocel", "Martisoara", "Oupictat", "Frescuta", "Frescanta", "Dulgherel", "Poartaur", "Calusel",
        "Calusar", "Horuta", "Doinel", "Doinatoare", "Naiet", "Nailar", "Coloanel", "Coloanfinit", "Tramvaiel", "Tramvaior"
    };

    public static IEnumerable<object[]> Each => Names.Select(n => new object[] { n });

    [Fact]
    public void EveryOneIsACarpathianSpeciesAndHandBuilt()
    {
        foreach (var n in Names)
        {
            var s = PokemonDatabase.Get(n);
            Assert.NotNull(s);
            Assert.True(s!.IsCarpathian, n);
            Assert.True(PokemonModels.HasModel(n), $"{n} has no sculpt of its own");
            Assert.Contains(n, PokemonModels.Species);
        }
    }

    [Fact]
    public void EachIsASculptOfItsOwn()
    {
        var seen = new HashSet<string>();
        foreach (var n in Names)
        {
            Assert.True(seen.Add(PokemonModels.Signature(n)), n);
            Assert.Equal(n, PokemonModels.Build(n).Species);
        }
    }

    [Fact]
    public void EachTakesItsShinyPaletteByTheRuleOfItsNumber()
    {
        foreach (var n in Names)
        {
            int number = PokemonDatabase.Get(n)!.DexNumber;
            Assert.Equal(number, ShinyPalettes.NumberOf(n));
            Assert.InRange(ShinyPalettes.AngleOf(number), 60f, 300f);
        }
    }

    [Theory]
    [MemberData(nameof(Each))]
    public void EachShinyChangesItsMainColourAndEachWalkerLeavesAPrint(string n)
    {
        // As ShinyTests and FootprintTests hold for every hand-built model
        var m = PokemonModels.Get(n);
        var colors = m.Mesh.Colors;
        var main = ShinyPalettes.MainColor(colors);
        var shined = ShinyPalettes.For(n, colors).Map(main);
        Assert.False(shined.R == main.R && shined.G == main.G && shined.B == main.B, $"{n}'s main colour stays as it is");
        var print = Footprints.Make(m);
        if (m.Plan is BodyPlan.Biped or BodyPlan.Quadruped or BodyPlan.Bird)
            Assert.True(print != null && print.Count >= Footprints.Least, $"{n} ({m.Plan}) leaves a print of {print?.Count ?? 0} texels");
        else if (m.Plan is BodyPlan.Fish or BodyPlan.Floating)
            Assert.Null(print);
    }

    [Theory]
    [MemberData(nameof(Each))]
    public void EachIsOneSmoothBodyInOnePieceOnTheGround(string n)
    {
        var m = PokemonModels.Build(n);
        var mesh = m.Mesh;
        output.WriteLine($"{n}: {m.Plan}, {m.Skeleton.Count} bones, {mesh.VertexCount} vertices, shell {m.Shell.VertexCount}, {m.Decals.Count} decals, height {m.Height:F2}");
        Assert.InRange(mesh.VertexCount, 4000, 60000);
        Assert.InRange(m.Shell.VertexCount, 1000, 60000);
        Assert.Equal(2, m.Decals.Count(d => d.IsEye));
        var pieces = Pieces(mesh).Where(p => p > Speck).ToList();
        Assert.True(pieces.Count == 1, $"{n} is in {pieces.Count} pieces of {string.Join(", ", pieces)} vertices");
        float bottom = mesh.Positions.Min(p => p.Y);
        if (m.Hovers && bottom > 0.04f) Assert.InRange(bottom, 0.06f * m.Height, 0.2f * m.Height);
        else Assert.InRange(bottom, -0.04f, 0.04f);
        for (int bone = 1; bone < m.Skeleton.Count; bone++)
        {
            int moved = Enumerable.Range(0, mesh.VertexCount).Count(v => mesh.WeightOf(v, bone) > 0.5f);
            Assert.True(moved > 20, $"{n}: bone {m.Skeleton[bone].Name} carries only {moved} vertices");
        }
    }

    [Theory]
    [MemberData(nameof(Each))]
    public void EachDecalCoversItsWholeSquare(string n)
    {
        var m = PokemonModels.Get(n);
        Assert.NotNull(m.DecalPatch);
        var uvs = m.DecalUVs!;
        var uncovered = new List<int>();
        for (int d = 0; d < m.Decals.Count; d++)
        {
            int col = d % m.AtlasColumns, row = d / m.AtlasColumns;
            var center = new Vector2((col + 0.5f) / m.AtlasColumns, (row + 0.5f) / m.AtlasRows);
            var half = new Vector2(0.25f / m.AtlasColumns, 0.25f / m.AtlasRows);
            bool left = false, right = false, top = false, bottom = false;
            foreach (var uv in uvs)
            {
                if (MathF.Abs(uv.X - center.X) > 2f * half.X || MathF.Abs(uv.Y - center.Y) > 2f * half.Y) continue;
                left |= uv.X <= center.X - half.X;
                right |= uv.X >= center.X + half.X;
                top |= uv.Y <= center.Y - half.Y;
                bottom |= uv.Y >= center.Y + half.Y;
            }
            if (!(left && right && top && bottom)) uncovered.Add(d);
        }
        Assert.True(uncovered.Count == 0, $"{n}: decals not covered: {string.Join(", ", uncovered.Select(d => $"{d} ({(m.Decals[d].IsEye ? "eye" : "mark")})"))}");
    }
}
