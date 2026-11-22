using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using Xunit;
using static PokemonPlatinumTests.MeshPieces;

namespace PokemonPlatinumTests;

/// <summary>
/// Carpathia's sculpts, batch 6 (plan 46 · CP4, PokemonModels.Carpathia6.cs): Izvoras to Oracul, each hand-built
/// from nothing, its own sculpt, in one piece with its two eyes, its decals covered and its shiny palette by its
/// number. <see cref="PokemonModelTests"/> holds them with every other model; these are the batch's alone, to run
/// quickly while it is sculpted.
/// </summary>
public class CarpathiaSculpts6Tests
{
    private static readonly string[] Batch =
    {
        "Izvoras", "Izvorana", "Nametel", "Nameton", "Zaganul", "Cremenel", "Cremenor", "Aurel", "Aurifer", "Norel",
        "Tunetor", "Grindinel", "Vantulet", "Vantoroi", "Morunel", "Belugrand", "Somnel", "Somnoros", "Brotacel", "Oracul"
    };

    public static IEnumerable<object[]> Each => Batch.Select(s => new object[] { s });

    [Fact]
    public void EverySpeciesOfTheBatchIsHandBuilt()
    {
        foreach (var name in Batch)
        {
            var species = PokemonDatabase.Get(name);
            Assert.NotNull(species);
            Assert.InRange(species!.DexNumber, 10101, 10120);
            Assert.True(PokemonModels.HasModel(name), $"{name} has no sculpt of its own");
            Assert.Contains(name, PokemonModels.Species);
            Assert.Equal(name, PokemonModels.Build(name).Species);
        }
    }

    [Fact]
    public void EachIsASculptOfItsOwn()
    {
        var signatures = Batch.Select(PokemonModels.Signature).ToList();
        Assert.Equal(Batch.Length, signatures.Distinct().Count());
    }

    [Fact]
    public void EachTakesItsShinyPaletteByItsNumber()
    {
        foreach (var name in Batch)
        {
            int number = PokemonDatabase.Get(name)!.DexNumber;
            Assert.Equal(number, ShinyPalettes.NumberOf(name));
            Assert.InRange(ShinyPalettes.AngleOf(number), 60f, 300f);
        }
    }

    [Theory]
    [MemberData(nameof(Each))]
    public void EachIsOneBodyWithTwoEyesInOnePiece(string name)
    {
        var m = PokemonModels.Get(name);
        var mesh = m.Mesh;
        Assert.InRange(mesh.VertexCount, 4000, 60000);
        Assert.InRange(m.Skeleton.Count, 2, SkinnedModel.MaxBones);
        float bottom = mesh.Positions.Min(p => p.Y);
        if (m.Hovers && bottom > 0.04f) Assert.InRange(bottom, 0.06f * m.Height, 0.2f * m.Height);
        else Assert.InRange(bottom, -0.04f, 0.04f);
        for (int b = 1; b < m.Skeleton.Count; b++)
        {
            int moved = Enumerable.Range(0, mesh.VertexCount).Count(v => mesh.WeightOf(v, b) > 0.5f);
            Assert.True(moved > 20, $"{name}: bone {m.Skeleton[b].Name} carries only {moved} vertices");
        }
        Assert.Equal(2, m.Decals.Count(d => d.IsEye));
        var pieces = Pieces(mesh).Where(n => n > Speck).ToList();
        Assert.True(pieces.Count == 1, $"{name} is in {pieces.Count} pieces of {string.Join(", ", pieces)} vertices");
    }

    [Theory]
    [MemberData(nameof(Each))]
    public void EachDecalCoversItsWholeSquare(string name)
    {
        var m = PokemonModels.Get(name);
        var uvs = m.DecalUVs!;
        for (int d = 0; d < m.Decals.Count; d++)
        {
            int col = d % m.AtlasColumns, row = d / m.AtlasColumns;
            var center = new System.Numerics.Vector2((col + 0.5f) / m.AtlasColumns, (row + 0.5f) / m.AtlasRows);
            var half = new System.Numerics.Vector2(0.25f / m.AtlasColumns, 0.25f / m.AtlasRows);
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
