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
/// The Mega Dynamax forms sculpted in wave 60, first half (PokemonModels.MegaMax1.cs): Abomasnow to Houndoom. Each is
/// a sculpt of its own, built on its Mega's, which the model tests hold to the rest (one piece, its eyes, its decals,
/// its feet); these hold what is particular to them, for these thirty-nine alone since the model tests walk every model.
/// </summary>
public class MegaDynamaxSculptsOneTests
{
    private readonly ITestOutputHelper output;

    public MegaDynamaxSculptsOneTests(ITestOutputHelper output) => this.output = output;

    private static readonly string[] Species =
    {
        "Abomasnow", "Absol", "Aerodactyl", "Aggron", "Alakazam", "Altaria", "Ampharos", "Audino", "Banette", "Baxcalibur", "Beedrill",
        "Blastoise", "Blaziken", "Camerupt", "Chandelure", "Charizard-X", "Charizard-Y", "Chesnaught", "Cinderace", "Decidueye", "Delphox",
        "Dragalge", "Dragonite", "Emboar", "Empoleon", "Excadrill", "Feraligatr", "Froslass", "Gallade", "Garchomp", "Gardevoir", "Gengar",
        "Glalie", "Glimmora", "Golisopod", "Greninja", "Gyarados", "Heracross", "Houndoom"
    };

    private static string MegaOf(string s) => s.StartsWith("Charizard-") ? "Charizard-Mega-" + s[^1] : s + "-Mega";

    public static IEnumerable<object[]> Forms => Species.Select(s => new object[] { MegaOf(s) + "-Mmax" });

    [Fact]
    public void ThirtyNineAreBuiltAndEachIsInTheData()
    {
        Assert.Equal(39, Species.Length);
        foreach (var s in Species)
        {
            string form = MegaOf(s) + "-Mmax";
            var owner = PokemonDatabase.SpeciesOfForm(form);
            Assert.NotNull(owner);
            Assert.Equal(FormKind.MegaDynamax, owner!.Form(form)!.Kind);
            Assert.True(PokemonModels.HasModel(form), form);
            Assert.Contains(form, PokemonModels.Forms);
        }
    }

    [Fact]
    public void EachIsASculptOfItsOwnAndNotItsMegasOrItsSpecies()
    {
        var seen = new HashSet<string>();
        foreach (var s in Species)
        {
            string mega = MegaOf(s), form = mega + "-Mmax";
            string sig = PokemonModels.Signature(form);
            Assert.NotEqual(PokemonModels.Signature(mega), sig);
            Assert.NotEqual(PokemonModels.Signature(PokemonDatabase.SpeciesOfForm(form)!.Name), sig);
            Assert.True(seen.Add(sig), form);
            Assert.Equal(form, PokemonModels.Build(form).Species);
        }
    }

    [Fact]
    public void APokemonInOneAsksForItsOwnModelAndOneInAnotherStillForItsMegas()
    {
        foreach (var s in Species)
        {
            string form = MegaOf(s) + "-Mmax";
            var species = PokemonDatabase.SpeciesOfForm(form)!;
            var p = new Pokemon(species, 50) { Form = form };
            Assert.Equal(form, p.ModelName);
        }
        // Every Mega Dynamax form of the games' Megas has its own sculpt since wave 60
    }

    [Theory]
    [MemberData(nameof(Forms))]
    public void EachKeepsItsMegasEyesStaysInOnePieceAndStandsOnTheGround(string form)
    {
        string mega = form[..^"-Mmax".Length];
        var mm = PokemonModels.Build(form);
        var baseModel = PokemonModels.Build(mega);
        output.WriteLine($"{form}: {mm.Mesh.VertexCount} vertices (Mega {baseModel.Mesh.VertexCount}), {mm.Decals.Count} decals, height {mm.Height:F2} (Mega {baseModel.Height:F2})");
        Assert.InRange(mm.Mesh.VertexCount, 4000, 60000);
        // Its eyes are its Mega's, and it adds an emblem's mark and no more eyes (Chandelure's stem is too narrow for a mark: its emblem is built)
        Assert.Equal(baseModel.Decals.Count(d => d.IsEye), mm.Decals.Count(d => d.IsEye));
        Assert.True(form.StartsWith("Chandelure") ? mm.Decals.Count == baseModel.Decals.Count : mm.Decals.Count >= baseModel.Decals.Count + 1, $"{form}: {mm.Decals.Count} decals, its Mega {baseModel.Decals.Count}");
        // As many separate pieces as its Mega (Alakazam's five crystals float, as its five spoons did)
        var pieces = Pieces(mm.Mesh).Where(n => n > Speck).ToList();
        var megaPieces = Pieces(baseModel.Mesh).Where(n => n > Speck).ToList();
        Assert.True(pieces.Count == megaPieces.Count, $"{form} is in {pieces.Count} pieces of {string.Join(", ", pieces)} vertices; {mega} in {megaPieces.Count}");
        float bottom = mm.Mesh.Positions.Min(p => p.Y);
        if (mm.Hovers && bottom > 0.04f) Assert.InRange(bottom, 0.06f * mm.Height, 0.2f * mm.Height);
        else Assert.InRange(bottom, -0.04f, 0.04f);
        // Every bone still carries some of the surface
        for (int bone = 1; bone < mm.Skeleton.Count; bone++)
        {
            int moved = Enumerable.Range(0, mm.Mesh.VertexCount).Count(v => mm.Mesh.WeightOf(v, bone) > 0.5f);
            Assert.True(moved > 20, $"{form}: bone {mm.Skeleton[bone].Name} carries only {moved} vertices");
        }
    }

    [Fact]
    public void EachDecalOfTheseCoversItsWholeSquare()
    {
        // As PokemonModelTests.EachDecalCoversItsWholeSquare, for these thirty-nine alone (that test walks every model)
        var uncovered = new List<string>();
        foreach (var s in Species)
        {
            string form = MegaOf(s) + "-Mmax";
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
                if (!(left && right && top && bottom)) uncovered.Add($"{form}: decal {d}");
            }
        }
        Assert.True(uncovered.Count == 0, "not covered: " + string.Join(", ", uncovered));
    }
}
