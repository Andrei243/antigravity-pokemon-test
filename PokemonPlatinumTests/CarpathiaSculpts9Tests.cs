using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using Xunit;
using static PokemonPlatinumTests.MeshPieces;

namespace PokemonPlatinumTests;

/// <summary>
/// Carpathia's batch 9 (plan 46 · CP4, PokemonModels.Carpathia9.cs): Rapanor to Stimel, each hand-built from nothing.
/// The checks PokemonModelTests holds every model to, for this batch alone (that class walks every model), and the
/// batch's own: every sculpt its own, its shiny palette by its number.
/// </summary>
public class CarpathiaSculpts9Tests
{
    private static readonly string[] Batch =
    {
        "Rapanor", "Guvizel", "Speleurs", "Pesterurs", "Paianjenel", "Paienjenis", "Licurici", "Licuron", "Proteu", "Serpel",
        "Serpoaica", "Dunel", "Dunaroi", "Strigoias", "Strigoion", "Sanzienel", "Sanziana", "Zburel", "Zburator", "Stimel"
    };

    /// <summary>Proteu has no eyes at all, as its brief says: smooth skin where they would be.</summary>
    private static readonly Dictionary<string, int> EyesOf = new() { ["Proteu"] = 0 };

    public static IEnumerable<object[]> Names => Batch.Select(n => new object[] { n });

    [Fact]
    public void EverySpeciesOfTheBatchIsHandBuiltAndItsOwn()
    {
        foreach (var name in Batch)
        {
            Assert.NotNull(PokemonDatabase.Get(name));
            Assert.True(PokemonModels.HasModel(name), $"{name} has no sculpt of its own");
            Assert.Contains(name, PokemonModels.Species);
            Assert.Equal(name, PokemonModels.Build(name).Species);
        }
        var signatures = Batch.Select(PokemonModels.Signature).ToArray();
        Assert.Equal(Batch.Length, signatures.Distinct().Count());
    }

    [Fact]
    public void EachTakesItsShinyPaletteByTheRuleOfItsNumber()
    {
        foreach (var name in Batch)
        {
            var species = PokemonDatabase.Get(name)!;
            Assert.Equal(species.DexNumber, ShinyPalettes.NumberOf(name));
            Assert.InRange(ShinyPalettes.AngleOf(species.DexNumber), 60f, 300f);
        }
    }

    [Theory]
    [MemberData(nameof(Names))]
    public void EachIsOneSmoothSkinnedBodyInOnePiece(string name)
    {
        var m = PokemonModels.Get(name);
        var mesh = m.Mesh;
        Assert.InRange(mesh.VertexCount, 4000, 60000);
        Assert.InRange(m.Shell.VertexCount, 1000, 60000);
        Assert.InRange(m.Skeleton.Count, 2, SkinnedModel.MaxBones);

        float bottom = mesh.Positions.Min(p => p.Y);
        if (m.Hovers && bottom > 0.04f) Assert.InRange(bottom, 0.06f * m.Height, 0.2f * m.Height);
        else Assert.InRange(bottom, -0.04f, 0.04f);

        for (int b = 1; b < m.Skeleton.Count; b++)
        {
            int moved = Enumerable.Range(0, mesh.VertexCount).Count(v => mesh.WeightOf(v, b) > 0.5f);
            Assert.True(moved > 20, $"{name}: bone {m.Skeleton[b].Name} carries only {moved} vertices");
        }

        Assert.Equal(EyesOf.GetValueOrDefault(name, 2), m.Decals.Count(d => d.IsEye));
        var pieces = Pieces(mesh).Where(n => n > Speck).ToList();
        Assert.True(pieces.Count == 1, $"{name} is in {pieces.Count} pieces of {string.Join(", ", pieces)} vertices");
    }

    [Theory]
    [MemberData(nameof(Names))]
    public void EachDecalCoversItsWholeSquare(string name)
    {
        var m = PokemonModels.Get(name);
        if (m.Decals.Count == 0) return;
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
            Assert.True(left && right && top && bottom, $"{name}: decal {d} ({(m.Decals[d].IsEye ? "eye" : "mark")}) isn't covered");
        }
    }
}
