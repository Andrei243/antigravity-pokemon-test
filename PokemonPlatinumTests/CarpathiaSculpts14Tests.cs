using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using Xunit;
using static PokemonPlatinumTests.MeshPieces;

namespace PokemonPlatinumTests;

/// <summary>
/// Carpathia's twenty-five regional forms (plan 46 · CP8, PokemonModels.Carpathia14.cs), each its species' sculpt taken
/// whole and changed: hand-built, a sculpt of its own (not its species'), one piece as its species is, standing on the
/// ground or hovering as it does, with its species' eyes and every decal covered. The model tests walk every model;
/// these hold the twenty-five alone.
/// </summary>
public class CarpathiaSculpts14Tests
{
    private readonly ITestOutputHelper output;

    public CarpathiaSculpts14Tests(ITestOutputHelper output) => this.output = output;

    private static string[] All => PokemonDatabase.GetAll().SelectMany(s => s.Forms ?? new()).Where(f => f.Name.EndsWith("-Carpathia")).Select(f => f.Name).ToArray();

    public static IEnumerable<object[]> Forms => All.Select(f => new object[] { f });

    private static string SpeciesOf(string form) => PokemonDatabase.SpeciesOfForm(form)!.Name;

    [Fact]
    public void TwentyFiveAreHandBuiltAndEachIsItsOwn()
    {
        Assert.Equal(25, All.Length);
        var seen = new HashSet<string>();
        foreach (var form in All)
        {
            Assert.True(PokemonModels.HasModel(form), $"{form} has no sculpt of its own");
            Assert.Contains(form, PokemonModels.Forms);
            string sig = PokemonModels.Signature(form);
            Assert.NotEqual(PokemonModels.Signature(SpeciesOf(form)), sig);
            Assert.True(seen.Add(sig), form);
            Assert.Equal(form, PokemonModels.Build(form).Species);
        }
    }

    [Theory]
    [MemberData(nameof(Forms))]
    public void EachKeepsItsSpeciesEyesStaysInOnePieceAndStandsWhereItShould(string form)
    {
        var m = PokemonModels.Build(form);
        var species = PokemonModels.Build(SpeciesOf(form));
        output.WriteLine($"{form}: {m.Mesh.VertexCount} vertices ({species.Species} {species.Mesh.VertexCount}), {m.Decals.Count} decals, height {m.Height:F2} ({species.Height:F2})");
        Assert.InRange(m.Mesh.VertexCount, 4000, 60000);
        Assert.InRange(m.Shell.VertexCount, 1000, 60000);
        Assert.Equal(species.Decals.Count(d => d.IsEye), m.Decals.Count(d => d.IsEye));
        var pieces = Pieces(m.Mesh).Where(n => n > Speck).ToList();
        Assert.True(pieces.Count == 1, $"{form} is in {pieces.Count} pieces of {string.Join(", ", pieces)} vertices");
        float bottom = m.Mesh.Positions.Min(p => p.Y);
        if (m.Hovers && bottom > 0.04f) Assert.InRange(bottom, 0.06f * m.Height, 0.2f * m.Height);
        else Assert.InRange(bottom, -0.04f, 0.04f);
        for (int bone = 1; bone < m.Skeleton.Count; bone++)
        {
            int moved = Enumerable.Range(0, m.Mesh.VertexCount).Count(v => m.Mesh.WeightOf(v, bone) > 0.5f);
            Assert.True(moved > 20, $"{form}: bone {m.Skeleton[bone].Name} carries only {moved} vertices");
        }
    }

    [Fact]
    public void EachDecalOfTheseCoversItsWholeSquare()
    {
        // As PokemonModelTests.EachDecalCoversItsWholeSquare, for these twenty-five alone
        var uncovered = new List<string>();
        foreach (var form in All)
        {
            var m = PokemonModels.Get(form);
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
                if (!(left && right && top && bottom)) uncovered.Add($"{form}: decal {d}{(m.Decals[d].IsEye ? " (eye)" : "")}");
            }
        }
        Assert.True(uncovered.Count == 0, "not covered: " + string.Join(", ", uncovered));
    }

    [Fact]
    public void APokemonInOneAsksForItsOwnModel()
    {
        foreach (var form in All)
        {
            var p = new Pokemon(PokemonDatabase.SpeciesOfForm(form)!, 30) { Form = form };
            Assert.Equal(form, p.ModelName);
        }
    }
}
