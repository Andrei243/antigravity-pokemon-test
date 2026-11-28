using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using Xunit;
using static PokemonPlatinumTests.MeshPieces;

namespace PokemonPlatinumTests;

/// <summary>
/// Carpathia's batch 4 (plan 46 · CP4, PokemonModels.Carpathia4.cs): Zimbrel to Babut, the forests and the mountains,
/// each sculpted from nothing. Every one hand-built and its own, in one piece, with two eyes and every decal covered, and
/// its shiny palette given by the rule of its number (PokemonModelTests holds the rest for every hand-built model).
/// </summary>
public class CarpathiaSculpts4Tests
{
    private static readonly string[] Batch =
    {
        "Zimbrel", "Zimbrunt", "Acvilet", "Acviltor", "Huhurez", "Ciocbit", "Ciocanor", "Tritonel", "Tritanor", "Viperin",
        "Cornviper", "Radascuta", "Radaskon", "Fluturel", "Fluturapol", "Bradlet", "Bradonor", "Floricolt", "Coltarina", "Babut"
    };

    public static IEnumerable<object[]> Each => Batch.Select(s => new object[] { s });

    [Fact]
    public void TheBatchIsNosSixtyOneToEightyOfTheCarpathianPokedex()
    {
        var species = Batch.Select(n => PokemonDatabase.Get(n)).ToArray();
        Assert.All(species, s => Assert.NotNull(s));
        Assert.Equal(Enumerable.Range(10061, 20), species.Select(s => s!.DexNumber));
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

    [Theory]
    [MemberData(nameof(Each))]
    public void EachIsOneSkinnedPieceWithTwoEyesCovered(string name)
    {
        var m = PokemonModels.Get(name);
        Assert.InRange(m.Mesh.VertexCount, 4000, 60000);
        Assert.Equal(2, m.Decals.Count(d => d.IsEye));
        var pieces = Pieces(m.Mesh).Where(n => n > Speck).ToList();
        Assert.True(pieces.Count == 1, $"{name} is in {pieces.Count} pieces of {string.Join(", ", pieces)} vertices");

        // Feet on the ground, or a flier floating a little above it
        float bottom = m.Mesh.Positions.Min(p => p.Y);
        if (m.Hovers && bottom > 0.04f) Assert.InRange(bottom, 0.06f * m.Height, 0.2f * m.Height);
        else Assert.InRange(bottom, -0.04f, 0.04f);

        // Every bone moves some of the surface
        for (int b = 1; b < m.Skeleton.Count; b++)
        {
            int moved = Enumerable.Range(0, m.Mesh.VertexCount).Count(v => m.Mesh.WeightOf(v, b) > 0.5f);
            Assert.True(moved > 20, $"{name}: bone {m.Skeleton[b].Name} carries only {moved} vertices");
        }

        // Every decal lies on triangles that reach all four sides of the middle half of its cell
        var uvs = m.DecalUVs!;
        for (int d = 0; d < m.Decals.Count; d++)
        {
            int col = d % m.AtlasColumns, row = d / m.AtlasColumns;
            var center = new Vector2((col + 0.5f) / m.AtlasColumns, (row + 0.5f) / m.AtlasRows);
            var half = new Vector2(0.25f / m.AtlasColumns, 0.25f / m.AtlasRows);
            bool left = false, right = false, top = false, bottom2 = false;
            foreach (var uv in uvs)
            {
                if (MathF.Abs(uv.X - center.X) > 2f * half.X || MathF.Abs(uv.Y - center.Y) > 2f * half.Y) continue;
                left |= uv.X <= center.X - half.X;
                right |= uv.X >= center.X + half.X;
                top |= uv.Y <= center.Y - half.Y;
                bottom2 |= uv.Y >= center.Y + half.Y;
            }
            Assert.True(left && right && top && bottom2, $"{name}: decal {d} ({(m.Decals[d].IsEye ? "eye" : "mark")}) isn't covered");
        }
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

    [Fact]
    public void NoneHasAFemaleLookToKeep()
    {
        Assert.All(Batch, n => Assert.False(PokemonDatabase.Get(n)!.GenderDifferences));
    }
}
