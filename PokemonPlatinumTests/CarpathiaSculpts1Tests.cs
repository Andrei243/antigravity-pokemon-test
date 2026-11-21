using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// Carpathia's batch 1 of sculpts (plan 46 · CP4, PokemonModels.Carpathia1.cs): Lynkit to Aricorn, each hand-built, a
/// sculpt of its own, one smooth body in one piece with its two eyes and every decal covered, and its shiny palette
/// given by the rule of its number. What PokemonModelTests holds of every model, for this batch alone.
/// </summary>
public class CarpathiaSculpts1Tests
{
    private static readonly string[] Batch =
    {
        "Lynkit", "Firlynx", "Codrulynx", "Salamint", "Furnander", "Fierander", "Pelicub", "Pelireed", "Buciumbill",
        "Trillet", "Nightrill", "Privigale", "Harciog", "Harciogre", "Chirpet", "Fiddlecrick", "Buburel", "Buburoza",
        "Arichip", "Aricorn"
    };

    public static IEnumerable<object[]> Names => Batch.Select(n => new object[] { n });

    [Fact]
    public void TheBatchIsTheCarpathianPokedexsFirstTwenty()
    {
        for (int i = 0; i < Batch.Length; i++)
        {
            var species = PokemonDatabase.Get(Batch[i]);
            Assert.NotNull(species);
            Assert.Equal(PokemonDatabase.CarpathiaFrom + i, species!.DexNumber);
        }
    }

    [Fact]
    public void EveryOneIsHandBuiltAndItsOwnSculpt()
    {
        foreach (var name in Batch)
        {
            Assert.True(PokemonModels.HasModel(name), $"{name} has no sculpt of its own");
            Assert.Contains(name, PokemonModels.Species);
            Assert.Equal(name, PokemonModels.Build(name).Species);
        }
        var signatures = Batch.Select(PokemonModels.Signature).ToArray();
        Assert.Equal(signatures.Length, signatures.Distinct().Count());
    }

    [Fact]
    public void EachTakesItsShinyPaletteByTheRuleOfItsNumber()
    {
        foreach (var name in Batch)
        {
            int number = PokemonDatabase.Get(name)!.DexNumber;
            Assert.Equal(number, ShinyPalettes.NumberOf(name));
            Assert.InRange(ShinyPalettes.AngleOf(number), 60f, 300f);
        }
        Assert.Equal(Batch.Length, Batch.Select(ShinyPalettes.Describe).Distinct().Count());
    }

    [Theory]
    [MemberData(nameof(Names))]
    public void EachIsOneSmoothSkinnedBodyWithTwoEyes(string name)
    {
        var m = PokemonModels.Build(name);
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
    [MemberData(nameof(Names))]
    public void EachIsInOnePiece(string name)
    {
        var pieces = MeshPieces.Pieces(PokemonModels.Get(name).Mesh).Where(n => n > MeshPieces.Speck).ToList();
        Assert.True(pieces.Count == 1, $"{name} is in {pieces.Count} pieces of {string.Join(", ", pieces)} vertices");
    }

    [Theory]
    [MemberData(nameof(Names))]
    public void EachDecalCoversItsWholeSquare(string name)
    {
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
                if (System.MathF.Abs(uv.X - center.X) > 2f * half.X || System.MathF.Abs(uv.Y - center.Y) > 2f * half.Y) continue;
                left |= uv.X <= center.X - half.X;
                right |= uv.X >= center.X + half.X;
                top |= uv.Y <= center.Y - half.Y;
                bottom |= uv.Y >= center.Y + half.Y;
            }
            Assert.True(left && right && top && bottom, $"{name}: decal {d} ({(m.Decals[d].IsEye ? "eye" : "mark")}) isn't covered");
        }
    }
}
