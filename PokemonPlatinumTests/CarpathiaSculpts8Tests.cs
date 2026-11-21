using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// Carpathia's batch 8 (plan 46 · CP4, PokemonModels.Carpathia8.cs): Hatzlet to Rapanel, each hand-built and its own
/// sculpt, in one piece with two eyes, every decal covered and every bone carrying some of the surface, standing on
/// its platform or floating over it, its shiny palette given by the rule of its number.
/// </summary>
public class CarpathiaSculpts8Tests
{
    private static readonly string[] Names =
    {
        "Hatzlet", "Hatzegon", "Minisaur", "Hategant", "Namolel", "Namolcan", "Vidrut", "Vidroar", "Castorel", "Castoroi",
        "Stiucuta", "Stiucon", "Racut", "Racoroi", "Cormoran", "Nufarel", "Nufarina", "Meduzel", "Meduzaur", "Rapanel"
    };

    public static IEnumerable<object[]> Batch => Names.Select(n => new object[] { n });

    [Fact]
    public void EveryOneIsASpeciesOfTheDataAndHandBuilt()
    {
        foreach (var (name, i) in Names.Select((n, i) => (n, i)))
        {
            var species = PokemonDatabase.Get(name);
            Assert.NotNull(species);
            Assert.Equal(10141 + i, species!.DexNumber);
            Assert.True(PokemonModels.HasModel(name), $"{name} has no sculpt of its own");
            Assert.Contains(name, PokemonModels.Species);
        }
    }

    [Fact]
    public void EachIsASculptOfItsOwn()
    {
        var signatures = Names.Select(PokemonModels.Signature).ToArray();
        Assert.Equal(Names.Length, signatures.Distinct().Count());
        foreach (var name in Names) Assert.Equal(name, PokemonModels.Build(name).Species);
    }

    [Fact]
    public void EachTakesItsShinyPaletteByItsNumber()
    {
        foreach (var name in Names)
        {
            int number = PokemonDatabase.Get(name)!.DexNumber;
            Assert.Equal(number, ShinyPalettes.NumberOf(name));
            Assert.InRange(ShinyPalettes.AngleOf(number), 60f, 300f);
        }
    }

    [Theory]
    [MemberData(nameof(Batch))]
    public void EachIsOneSkinnedBodyWithTwoEyesCovered(string name)
    {
        var m = PokemonModels.Build(name);
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
        Assert.NotNull(m.DecalPatch);
        var uvs = m.DecalUVs!;
        for (int d = 0; d < m.Decals.Count; d++)
        {
            int col = d % m.AtlasColumns, row = d / m.AtlasColumns;
            var center = new Vector2((col + 0.5f) / m.AtlasColumns, (row + 0.5f) / m.AtlasRows);
            var cellHalf = new Vector2(0.25f / m.AtlasColumns, 0.25f / m.AtlasRows);
            bool left = false, right = false, top = false, low = false;
            foreach (var uv in uvs)
            {
                if (MathF.Abs(uv.X - center.X) > 2f * cellHalf.X || MathF.Abs(uv.Y - center.Y) > 2f * cellHalf.Y) continue;
                left |= uv.X <= center.X - cellHalf.X;
                right |= uv.X >= center.X + cellHalf.X;
                top |= uv.Y <= center.Y - cellHalf.Y;
                low |= uv.Y >= center.Y + cellHalf.Y;
            }
            Assert.True(left && right && top && low, $"{name}: decal {d} ({(m.Decals[d].IsEye ? "eye" : "mark")}) isn't covered");
        }
    }
}
