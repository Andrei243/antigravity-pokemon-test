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
/// Carpathia's batch 7 (plan 46 · CP4, PokemonModels.Carpathia7.cs): Starcel to Liliacon, the delta, the sea, the salt
/// mines and the caves (Nos. 10121 to 10140). Each is hand-built, a sculpt of its own, in one smooth skinned piece with
/// two eyes and every decal covered, and takes its shiny palette by its number: the checks of PokemonModelTests and
/// ShinyTests for these twenty alone, since those walk every model.
/// </summary>
public class CarpathiaSculpts7Tests
{
    private static readonly string[] Batch =
    {
        "Starcel", "Starcarno", "Plaurel", "Plaurnaut", "Tantarel", "Tantaroi", "Hutulet", "Hutulmare", "Delfinel", "Delfinora",
        "Calutmar", "Calutregal", "Sarel", "Salinax", "Valvisor", "Valvoara", "Ghetel", "Stalaghet", "Liliacel", "Liliacon"
    };

    public static IEnumerable<object[]> Each => Batch.Select(s => new object[] { s });

    [Fact]
    public void EveryOneIsHandBuiltAndASculptOfItsOwn()
    {
        foreach (var name in Batch)
        {
            var species = PokemonDatabase.Get(name);
            Assert.NotNull(species);
            Assert.InRange(species!.DexNumber, 10121, 10140);
            Assert.True(PokemonModels.HasModel(name), $"{name} has no sculpt of its own");
            Assert.Contains(name, PokemonModels.Species);
        }
        Assert.Equal(Batch.Length, Batch.Select(PokemonModels.Signature).Distinct().Count());
    }

    [Theory]
    [MemberData(nameof(Each))]
    public void EachIsOneSmoothSkinnedBodyWithTwoEyes(string name)
    {
        var m = PokemonModels.Build(name);
        Assert.Equal(name, m.Species);
        var mesh = m.Mesh;
        Assert.InRange(mesh.VertexCount, 4000, 60000);
        Assert.InRange(m.Skeleton.Count, 2, SkinnedModel.MaxBones);
        Assert.InRange(m.Shell.VertexCount, 1000, 60000);
        float bottom = mesh.Positions.Min(p => p.Y);
        if (m.Hovers && bottom > 0.04f) Assert.InRange(bottom, 0.06f * m.Height, 0.2f * m.Height);
        else Assert.InRange(bottom, -0.04f, 0.04f);
        for (int b = 1; b < m.Skeleton.Count; b++)
        {
            int moved = Enumerable.Range(0, mesh.VertexCount).Count(v => mesh.WeightOf(v, b) > 0.5f);
            Assert.True(moved > 20, $"{name}: bone {m.Skeleton[b].Name} carries only {moved} vertices");
        }
        Assert.Equal(2, m.Decals.Count(d => d.IsEye));
    }

    [Theory]
    [MemberData(nameof(Each))]
    public void EachIsInOnePiece(string name)
    {
        var pieces = Pieces(PokemonModels.Get(name).Mesh).Where(n => n > Speck).ToList();
        Assert.True(pieces.Count == 1, $"{name} is in {pieces.Count} pieces of {string.Join(", ", pieces)} vertices");
    }

    [Theory]
    [MemberData(nameof(Each))]
    public void EachDecalCoversItsWholeSquare(string name)
    {
        // As PokemonModelTests.EachDecalCoversItsWholeSquare, for this batch alone (that test walks every model)
        var m = PokemonModels.Get(name);
        var uvs = m.DecalUVs!;
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
            Assert.True(left && right && top && bottom, $"{name}: decal {d} ({(m.Decals[d].IsEye ? "eye" : "mark")}) isn't covered");
        }
    }

    [Theory]
    [MemberData(nameof(Each))]
    public void EachTakesItsShinyPaletteByItsNumberAndChangesItsMainColour(string name)
    {
        Assert.Equal(PokemonDatabase.Get(name)!.DexNumber, ShinyPalettes.NumberOf(name));
        var colors = PokemonModels.Get(name).Mesh.Colors;
        var main = ShinyPalettes.MainColor(colors);
        var shined = ShinyPalettes.For(name, colors).Map(main);
        Assert.False(shined.R == main.R && shined.G == main.G && shined.B == main.B, $"{name}'s main colour stays as it is");
    }

    [Fact]
    public void EachLineHasTwoPalettes()
    {
        for (int i = 0; i < Batch.Length; i += 2)
            Assert.NotEqual(ShinyPalettes.Describe(Batch[i]), ShinyPalettes.Describe(Batch[i + 1]));
    }
}
