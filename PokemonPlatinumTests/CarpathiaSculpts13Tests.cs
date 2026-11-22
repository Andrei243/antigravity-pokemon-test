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
/// Carpathia's batch 13 (plan 46 · CP4): the legendaries and the mythicals, Furtunaur to Nazdravan, each hand-built
/// from nothing (PokemonModels.Carpathia13.cs): its own sculpt, in one piece, its eyes and marks covered, its shiny
/// palette by its number. The model tests walk every model; this holds the batch alone.
/// </summary>
public class CarpathiaSculpts13Tests
{
    private static readonly string[] Batch =
        { "Furtunaur", "Stindrac", "Ielele", "Mumapadura", "Maiastra", "Murgila", "Miezila", "Zorila", "Nazdravan" };

    public static IEnumerable<object[]> Names => Batch.Select(n => new object[] { n });

    [Fact]
    public void EveryOneIsASpeciesOfTheDataAndHandBuilt()
    {
        foreach (var name in Batch)
        {
            var s = PokemonDatabase.Get(name);
            Assert.NotNull(s);
            Assert.InRange(s!.DexNumber, 10241, 10249);
            Assert.False(s.GenderDifferences);
            Assert.True(PokemonModels.HasModel(name), $"{name} has no sculpt of its own");
            Assert.Contains(name, PokemonModels.Species);
            Assert.Equal(name, PokemonModels.Build(name).Species);
        }
    }

    [Fact]
    public void EachIsASculptOfItsOwn()
    {
        var signatures = Batch.Select(PokemonModels.Signature).ToArray();
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
        Assert.Equal(Batch.Length, Batch.Select(ShinyPalettes.Describe).Distinct().Count());
    }

    [Theory]
    [MemberData(nameof(Names))]
    public void EachIsOneSkinnedBodyWithTwoEyes(string name)
    {
        var m = PokemonModels.Build(name);
        Assert.InRange(m.Mesh.VertexCount, 4000, 60000);
        Assert.InRange(m.Skeleton.Count, 2, SkinnedModel.MaxBones);
        float bottom = m.Mesh.Positions.Min(p => p.Y);
        if (m.Hovers && bottom > 0.04f) Assert.InRange(bottom, 0.06f * m.Height, 0.2f * m.Height);
        else Assert.InRange(bottom, -0.04f, 0.04f);
        for (int bone = 1; bone < m.Skeleton.Count; bone++)
        {
            int moved = Enumerable.Range(0, m.Mesh.VertexCount).Count(v => m.Mesh.WeightOf(v, bone) > 0.5f);
            Assert.True(moved > 20, $"{name}: bone {m.Skeleton[bone].Name} carries only {moved} vertices");
        }
        // Two eyes each, as the model tests ask of every model they don't name: Furtunaur's side heads and two of the
        // three Iele have their eyes as marks
        Assert.Equal(2, m.Decals.Count(d => d.IsEye));
    }

    [Theory]
    [MemberData(nameof(Names))]
    public void EachIsInOnePiece(string name)
    {
        var mesh = PokemonModels.Get(name).Mesh;
        var pieces = Pieces(mesh).Where(n => n > Speck).ToList();
        Assert.True(pieces.Count == 1, $"{name} is in {pieces.Count} pieces of {string.Join(", ", pieces)} vertices; the loose ones lie at {Loose(mesh)}");
    }

    /// <summary>Where the pieces but the largest lie (the middle of each and its size), for a failure's message.</summary>
    private static string Loose(PokemonPlatinumEngine.Graphics.SdfMesh mesh)
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
        var groups = Enumerable.Range(0, mesh.VertexCount).GroupBy(Find).Where(g => g.Count() > Speck).OrderByDescending(g => g.Count()).Skip(1);
        return string.Join("; ", groups.Select(g =>
        {
            var ps = g.Select(v => mesh.Positions[v]).ToArray();
            var lo = ps.Aggregate(Vector3.Min);
            var hi = ps.Aggregate(Vector3.Max);
            return $"{(lo + hi) / 2f:F3} size {hi - lo:F3}";
        }));
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
