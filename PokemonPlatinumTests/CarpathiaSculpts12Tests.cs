using System.Linq;
using System.Numerics;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using Xunit;
using static PokemonPlatinumTests.MeshPieces;

namespace PokemonPlatinumTests;

/// <summary>
/// Carpathia's batch 12 (plan 46 · CP4, PokemonModels.Carpathia12.cs): Fluieras to Lunacolac, each hand-built, its own
/// sculpt, in one piece with two eyes on its surface, and its shiny palette given by its number. The model tests hold
/// every hand-built model to the same; these do it for this batch alone, quickly.
/// </summary>
public class CarpathiaSculpts12Tests
{
    public static readonly string[] Names =
    {
        "Fluieras", "Tobel", "Tobosar", "Filuta", "Carturel", "Steluta", "Stelar", "Mascuta", "Mascaron", "Sperietoare",
        "Sobitel", "Teracotor", "Cumpanel", "Pricolel", "Pricolici", "Ursivia", "Ursifira", "Ursimora", "Solzmeu", "Lunacolac"
    };

    public static TheoryData<string> Each => new(Names);

    [Fact]
    public void EverySpeciesOfTheBatchIsHandBuiltAndItsOwn()
    {
        Assert.All(Names, n => Assert.NotNull(PokemonDatabase.Get(n)));
        Assert.All(Names, n => Assert.True(PokemonModels.HasModel(n), $"{n} has no sculpt of its own"));
        Assert.All(Names, n => Assert.Contains(n, PokemonModels.Species));
        Assert.Equal(Names.Length, Names.Select(PokemonModels.Signature).Distinct().Count());
    }

    [Theory]
    [MemberData(nameof(Each))]
    public void EachIsOneSmoothBodyStandingOrHovering(string name)
    {
        var m = PokemonModels.Build(name);
        Assert.Equal(name, m.Species);
        var mesh = m.Mesh;
        Assert.InRange(mesh.VertexCount, 4000, 60000);
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

    [Fact]
    public void EachTakesItsShinyPaletteByItsNumber()
    {
        foreach (var n in Names)
            Assert.Equal(PokemonDatabase.Get(n)!.DexNumber, ShinyPalettes.NumberOf(n));
    }

    [Fact]
    public void NoneHasAFemaleLookToKeep()
    {
        Assert.All(Names, n => Assert.False(PokemonDatabase.Get(n)!.GenderDifferences, n));
    }
}
