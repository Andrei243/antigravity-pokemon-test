using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// Carpathia's sculpts, batch 2 (plan 46 · CP4, PokemonModels.Carpathia2.cs): Ciobipup to Floarsol, each hand-built
/// and its own, in one piece, with its two eyes and every mark on the surface, taking its shiny palette by its number.
/// </summary>
public class CarpathiaSculpts2Tests
{
    private static readonly string[] Names =
    {
        "Ciobipup", "Mioritar", "Lanita", "Turcanica", "Barzlet", "Barzanor", "Spiridel", "Spiriduke", "Hribling", "Hribarde",
        "Pisicut", "Motanor", "Cocosel", "Cocosor", "Albinut", "Stupara", "Iepurel", "Iepuroi", "Soarel", "Floarsol"
    };

    public static IEnumerable<object[]> Batch => Names.Select(n => new object[] { n });

    [Fact]
    public void EverySpeciesOfTheBatchIsHandBuiltAndItsOwn()
    {
        Assert.All(Names, n =>
        {
            Assert.NotNull(PokemonDatabase.Get(n));
            Assert.True(PokemonModels.HasModel(n), $"{n} has no sculpt of its own");
            Assert.Contains(n, PokemonModels.Species);
        });
        Assert.Equal(Names.Length, Names.Select(PokemonModels.Signature).Distinct().Count());
    }

    [Theory]
    [MemberData(nameof(Batch))]
    public void EachIsInOnePieceWithTwoEyesAndItsMarksCovered(string species)
    {
        var m = PokemonModels.Build(species);
        Assert.Equal(species, m.Species);
        var pieces = MeshPieces.Pieces(m.Mesh).Where(n => n > MeshPieces.Speck).ToList();
        Assert.True(pieces.Count == 1, $"{species} is in {pieces.Count} pieces of {string.Join(", ", pieces)} vertices");
        Assert.Equal(2, m.Decals.Count(d => d.IsEye));

        float bottom = m.Mesh.Positions.Min(p => p.Y);
        if (m.Hovers && bottom > 0.04f) Assert.InRange(bottom, 0.06f * m.Height, 0.2f * m.Height);
        else Assert.InRange(bottom, -0.04f, 0.04f);
        for (int b = 1; b < m.Skeleton.Count; b++)
        {
            int moved = Enumerable.Range(0, m.Mesh.VertexCount).Count(v => m.Mesh.WeightOf(v, b) > 0.5f);
            Assert.True(moved > 20, $"{species}: bone {m.Skeleton[b].Name} carries only {moved} vertices");
        }

        // Every decal's middle half lies on triangles of the patch, as PokemonModelTests holds for every model
        var uvs = m.DecalUVs!;
        for (int d = 0; d < m.Decals.Count; d++)
        {
            int col = d % m.AtlasColumns, row = d / m.AtlasColumns;
            var center = new Vector2((col + 0.5f) / m.AtlasColumns, (row + 0.5f) / m.AtlasRows);
            var cellHalf = new Vector2(0.25f / m.AtlasColumns, 0.25f / m.AtlasRows);
            bool left = false, right = false, top = false, low = false;
            foreach (var uv in uvs)
            {
                if (System.MathF.Abs(uv.X - center.X) > 2f * cellHalf.X || System.MathF.Abs(uv.Y - center.Y) > 2f * cellHalf.Y) continue;
                left |= uv.X <= center.X - cellHalf.X;
                right |= uv.X >= center.X + cellHalf.X;
                top |= uv.Y <= center.Y - cellHalf.Y;
                low |= uv.Y >= center.Y + cellHalf.Y;
            }
            Assert.True(left && right && top && low, $"{species}: decal {d} ({(m.Decals[d].IsEye ? "eye" : "mark")}) isn't covered");
        }
    }

    [Fact]
    public void EachTakesItsShinyPaletteByItsNumber()
    {
        for (int i = 0; i < Names.Length; i++)
        {
            Assert.Equal(10021 + i, PokemonDatabase.Get(Names[i])!.DexNumber);
            Assert.Equal(10021 + i, ShinyPalettes.NumberOf(Names[i]));
        }
        Assert.Equal(Names.Length, Names.Select(ShinyPalettes.Describe).Distinct().Count());
    }
}
