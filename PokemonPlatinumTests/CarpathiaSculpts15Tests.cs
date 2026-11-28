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
/// Carpathia's Tale Forms, the first half (plan 46 · CP12a, PokemonModels.Carpathia15.cs): sixteen forms of the kind
/// Tale, each bound in Data/tales.json to the tale the chapters' section 5 gives it, and each its species' sculpt taken
/// whole and lit by its tale: hand-built, a sculpt of its own, one piece, standing or hovering as its species does, with
/// its species' eyes and every decal covered. The model tests walk every model; these hold the sixteen alone.
/// </summary>
public class CarpathiaSculpts15Tests
{
    private readonly ITestOutputHelper output;

    public CarpathiaSculpts15Tests(ITestOutputHelper output) => this.output = output;

    /// <summary>The chapters' section 5, in its order, as far as CP12a goes: tale by tale, its Tale Forms' species.</summary>
    private static readonly (string Tale, string[] Species)[] Bound =
    {
        ("sanziene", new[] { "Sanziana", "Buburoza" }),
        ("fatfrumos", new[] { "Fierander", "Hutulmare" }),
        ("spiridus", new[] { "Spiriduke", "Harciogre" }),
        ("ielele", new[] { "Buciumbill", "Privigale", "Ielele" }),
        ("dochia", new[] { "Babadochia", "Capraneagra" }),
        ("uriasi", new[] { "Zimbrunt", "Ursabrun", "Hategant" }),
        ("mumapadurii", new[] { "Codrulynx", "Hribarde" }),
    };

    private static string[] All => PokemonDatabase.Carpathia().SelectMany(s => s.Forms ?? new()).Where(f => f.Kind == FormKind.Tale).Select(f => f.Name).ToArray();

    public static IEnumerable<object[]> Forms => All.Select(f => new object[] { f });

    private static string SpeciesOf(string form) => PokemonDatabase.SpeciesOfForm(form)!.Name;

    [Fact]
    public void SixteenTaleFormsAreInTheDataEachBoundToItsTale()
    {
        Assert.Equal(16, All.Length);
        var tales = TaleTable.Default;
        foreach (var (tale, species) in Bound)
            foreach (var name in species)
            {
                var owner = PokemonDatabase.Get(name)!;
                Assert.True(owner.IsCarpathian, name);
                var form = owner.Form(name + "-Tale");
                Assert.NotNull(form);
                Assert.Equal(FormKind.Tale, form!.Kind);
                // A look of its species: its types, stats, abilities and moves are the species' own
                Assert.Null(form.Types);
                Assert.Null(form.BaseStats);
                Assert.Null(form.Learnset);
                Assert.Same(owner, PokemonDatabase.SpeciesOfForm(form.Name));
                Assert.Equal(form.Name, tales.Get(tale)!.FormFor(name));
            }
        // Every binding of the table names a Tale Form of its species, and no Tale Form is bound twice
        var bindings = tales.All.SelectMany(t => t.Forms).ToList();
        Assert.All(bindings, f => Assert.Equal(FormKind.Tale, PokemonDatabase.Get(f.Species)!.Form(f.Form)!.Kind));
        Assert.Equal(bindings.Count, bindings.Select(f => f.Form).Distinct().Count());
        Assert.Equal(All.OrderBy(n => n), bindings.Select(f => f.Form).OrderBy(n => n));
    }

    [Fact]
    public void SixteenAreHandBuiltAndEachIsItsOwn()
    {
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
        Assert.InRange(m.Mesh.VertexCount, 4000, 70000);
        Assert.InRange(m.Shell.VertexCount, 1000, 70000);
        Assert.Equal(species.Hovers, m.Hovers);
        Assert.Equal(species.Decals.Count, m.Decals.Count);
        Assert.Equal(species.Decals.Count(d => d.IsEye), m.Decals.Count(d => d.IsEye));
        var pieces = Pieces(m.Mesh).Where(n => n > Speck).ToList();
        int speciesPieces = Pieces(species.Mesh).Count(n => n > Speck);
        Assert.True(pieces.Count <= Math.Max(1, speciesPieces), $"{form} is in {pieces.Count} pieces of {string.Join(", ", pieces)} vertices ({species.Species} in {speciesPieces})");
        float bottom = m.Mesh.Positions.Min(p => p.Y);
        if (m.Hovers && bottom > 0.04f) Assert.InRange(bottom, 0.06f * m.Height, 0.2f * m.Height);
        else Assert.InRange(bottom, -0.04f, 0.04f);
        // As big in the picture as its species, never grown wider (a wider model meshes coarser and loses its marks)
        Assert.InRange(m.Height, species.Height * 0.95f, species.Height * 1.25f);
        for (int bone = 1; bone < m.Skeleton.Count; bone++)
        {
            int moved = Enumerable.Range(0, m.Mesh.VertexCount).Count(v => m.Mesh.WeightOf(v, bone) > 0.5f);
            Assert.True(moved > 20, $"{form}: bone {m.Skeleton[bone].Name} carries only {moved} vertices");
        }
    }

    [Fact]
    public void EachDecalOfTheseCoversItsWholeSquare()
    {
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
