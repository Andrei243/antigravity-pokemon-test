using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// The sculpts of the species of our own (plan 03 · D14, PokemonModels.Ours.*.cs): each batch whose looks are built
/// is hand-built whole, every sculpt its own and not its forebear's, its shiny palette given by the rule of its number
/// and its female look kept where its forebear's is (the model tests hold each to the rest: one piece, its eyes, its
/// decals covered).
/// </summary>
public class OurSculptTests
{
    /// <summary>Batch 1: the species of our own that evolve from Kanto's (Nos. 2001 to 2151).</summary>
    private static PokemonSpecies[] Kanto => PokemonDatabase.Ours().Where(s => s.DexNumber is >= 2001 and <= 2151).ToArray();

    private static PokemonSpecies ForebearOf(PokemonSpecies s) =>
        PokemonDatabase.GetAll().First(f => f.Evolutions?.Any(e => e.TargetSpecies == s.Name) == true);

    [Fact]
    public void EverySpeciesOfOursOfKantoIsHandBuilt()
    {
        Assert.Equal(46, Kanto.Length);
        Assert.All(Kanto, s => Assert.True(PokemonModels.HasModel(s.Name), $"{s.Name} has no sculpt of its own"));
        Assert.All(Kanto, s => Assert.Contains(s.Name, PokemonModels.Species));
    }

    [Fact]
    public void EachIsASculptOfItsOwnAndNotItsForebears()
    {
        foreach (var s in Kanto)
        {
            var forebear = ForebearOf(s);
            Assert.NotEqual(PokemonModels.Signature(forebear.Name), PokemonModels.Signature(s.Name));
            Assert.Equal(s.Name, PokemonModels.Build(s.Name).Species);
        }
    }

    [Fact]
    public void EachTakesItsShinyPaletteByTheRuleOfItsNumber()
    {
        foreach (var s in Kanto)
        {
            Assert.Equal(s.DexNumber, ShinyPalettes.NumberOf(s.Name));
            Assert.InRange(ShinyPalettes.AngleOf(s.DexNumber), 60f, 300f);
            Assert.NotEqual(ShinyPalettes.Describe(ForebearOf(s).Name), ShinyPalettes.Describe(s.Name));
        }
    }

    [Fact]
    public void AFemaleLookIsKeptWhereTheForebearsIs()
    {
        // Raticate, Dodrio, Hypno, Seaking and Gyarados: their evolutions of ours keep the difference (the proposal's
        // question 8), sculpted as their forebears' are
        var flagged = Kanto.Where(s => s.GenderDifferences).Select(s => s.Name).OrderBy(n => n).ToArray();
        Assert.Equal(new[] { "Mesmeryx", "Ratchisel", "Sabrefin", "Tripluma", "Typhonarch" }, flagged);
        Assert.All(flagged, n => Assert.Contains(n, PokemonLook.Females));
    }

    /// <summary>Batch 3: the species of our own that evolve from Hoenn's (Nos. 2252 to 2386).</summary>
    private static PokemonSpecies[] Hoenn => PokemonDatabase.Ours().Where(s => s.DexNumber is >= 2252 and <= 2386).ToArray();

    [Fact]
    public void EverySpeciesOfOursOfHoennIsHandBuiltAndItsOwn()
    {
        Assert.Equal(51, Hoenn.Length);
        foreach (var s in Hoenn)
    /// <summary>Batch 5: the species of our own that evolve from Unova's (Nos. 2494 to 2649), sculpted from the user's reference pictures.</summary>
    private static PokemonSpecies[] Unova => PokemonDatabase.Ours().Where(s => s.DexNumber is >= 2494 and <= 2649).ToArray();

    public void EverySpeciesOfOursOfUnovaIsHandBuiltAndItsOwn()
    {
        Assert.Equal(52, Unova.Length);
        foreach (var s in Unova)
        {
            Assert.True(PokemonModels.HasModel(s.Name), $"{s.Name} has no sculpt of its own");
            Assert.Contains(s.Name, PokemonModels.Species);
            Assert.NotEqual(PokemonModels.Signature(ForebearOf(s).Name), PokemonModels.Signature(s.Name));
            Assert.Equal(s.Name, PokemonModels.Build(s.Name).Species);
        }
    }

    [Fact]
    public void AHoennFemaleLookIsSculptedWhereTheForebearsIs()
    {
        // Medicham, Swalot, Camerupt, Cacturne, Milotic and Relicanth: their evolutions of ours keep the difference
        var flagged = Hoenn.Where(s => s.GenderDifferences).Select(s => s.Name).OrderBy(n => n).ToArray();
        Assert.Equal(new[] { "Aeonfin", "Ascetrance", "Gulpgorge", "Prismaria", "Saguarox", "Volcamel" }, flagged);
        foreach (var n in flagged)
        {
            Assert.True(PokemonLook.HasFemale(n));
            Assert.NotEqual(PokemonModels.Signature(n), PokemonModels.Signature(n + PokemonLook.FemaleSuffix));
        }
    public void JellicrownsFemaleIsPinkAndHerOwn()
    {
        // Jellicent's female is a form of her own; Jellicrown keeps the difference as a look of the one species
        Assert.True(PokemonLook.HasFemale("Jellicrown"));
        Assert.NotEqual(PokemonModels.Signature("Jellicrown"), PokemonModels.Signature("Jellicrown+f"));
    }
}
