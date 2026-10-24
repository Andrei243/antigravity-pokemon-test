using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using Xunit;
using static PokemonPlatinumTests.MeshPieces;
using static PokemonPlatinumTests.Scenario;

namespace PokemonPlatinumTests;

/// <summary>
/// Female looks (plan 09 · L6; style guide, "Female looks"): the look <c>X+f</c>, before any <c>+s</c>, of a female
/// whose species has a female look of its own; her own sculpt, from her species' sculptor asked for the female, in one
/// piece with every decal covered; her own signature, so no male's sprite is baked again; and every hand-built
/// species of Kanto the data flags among the looks.
/// </summary>
public class FemaleLookTests
{
    public static IEnumerable<object[]> Females => PokemonLook.Females.Select(s => new object[] { s });

    // ------------------------------------------------------------------ the data's flag and the list

    [Fact]
    public void EveryFlaggedHandBuiltSpeciesOfKantoHasAFemaleLook()
    {
        var flagged = PokemonDatabase.GetAll().Where(s => s.GenderDifferences && s.DexNumber <= 151).Select(s => s.Name).ToList();
        // PokeAPI's flag: 23 of Kanto's species, Venusaur to Eevee
        Assert.Equal(23, flagged.Count);
        foreach (var name in flagged.Where(PokemonModels.HasModel))
            Assert.True(PokemonLook.HasFemale(name), $"{name}'s females look different, and it has no female look");
    }

    [Theory]
    [InlineData(152, 251, 22)] // Johto: Meganium to Donphan (plan 09 · L7)
    [InlineData(252, 386, 18)] // Hoenn: Torchic to Relicanth (plan 09 · L7)
    [InlineData(387, 493, 31)] // Sinnoh: Starly to Mamoswine (plan 09 · L8)
    public void EveryFlaggedSpeciesOfJohtoHoennAndSinnohHasAFemaleLook(int first, int last, int count)
    {
        var flagged = PokemonDatabase.GetAll().Where(s => s.GenderDifferences && s.DexNumber >= first && s.DexNumber <= last).Select(s => s.Name).ToList();
        Assert.Equal(count, flagged.Count);
        foreach (var name in flagged)
        {
            // Every one of them is hand-built since plan 03's Johto and Hoenn batches
            Assert.True(PokemonModels.HasModel(name), $"{name} has no hand-built model");
            Assert.True(PokemonLook.HasFemale(name), $"{name}'s females look different, and it has no female look");
        }
    }

    [Fact]
    public void EveryFlaggedSpeciesHasAFemaleLookBySculptRuleOrForm()
    {
        // Plan 09 · L8, done when: all 1,025 species, each flagged one's female drawn as hers
        var flagged = PokemonDatabase.GetAll().Where(s => s.GenderDifferences).ToList();
        Assert.Equal(102, flagged.Count);
        int sculpted = 0, forms = 0;
        foreach (var s in flagged)
        {
            if (s.Form(s.Name + "-Female") is { } form)
            {
                // Her own form (Meowstic, Pyroar, Basculegion...), which is a model of its own already
                Assert.Equal(form.Name, Pokemon.FormOfGender(s, Gender.Female));
                Assert.False(PokemonLook.HasFemale(s.Name), $"{s.Name}'s female is a form, and it has a look besides");
                forms++;
                continue;
            }
            Assert.True(PokemonLook.HasFemale(s.Name), $"{s.Name}'s females look different, and it has no female look");
            if (PokemonModels.HasModel(s.Name))
            {
                Assert.Contains(s.Name, PokemonLook.Females);
                sculpted++;
            }
            else
                // The rule's net: a generated species' female is her male's genome made female
                Assert.True(PokemonGenomes.For(s.Name + PokemonLook.FemaleSuffix) is { Female: true }, $"{s.Name} has no female by the rule");
        }
        Assert.Equal(95, sculpted);
        Assert.Equal(7, forms);
    }

    [Fact]
    public void UnfezantsFemaleIsOfUnovaAndHerFormIsNoneOfHerOwn()
    {
        Assert.True(PokemonLook.HasFemale("Unfezant"));
        Assert.Contains("Unfezant", PokemonLook.Females);
        // She is browner and has no pink: fewer of her colours are the male's pink plume's
        static int Pink(PokeModel m) => m.Mesh.Colors.Count(c => c.R > 190 && c.G < 120 && c.B > 90 && c.B < 150);
        Assert.True(Pink(PokemonModels.Build("Unfezant")) > 0);
        Assert.Equal(0, Pink(PokemonModels.Build("Unfezant+f")));
    }

    [Fact]
    public void AFemaleByTheRuleIsHerMalesGenomeWithASmallerHead()
    {
        // The rule (plan 09 · L8) for a flagged species nobody sculpted: tried on a flagged species' genome
        var male = PokemonGenomes.For("Rhyperior")!;
        var female = PokemonGenomes.For("Rhyperior+f")!;
        Assert.False(male.Female);
        Assert.True(female.Female);
        Assert.NotEqual(male.Describe(), female.Describe());
        Assert.EndsWith(",female", female.Describe());
        Assert.Equal(male.HeadScale * PokemonGenomes.FemaleHeadScale, female.HeadScale);
        Assert.Equal(male.Kind, female.Kind);
        Assert.Equal(male.Seed, female.Seed);
        // The male's genome is never changed by asking for her
        Assert.Equal(male.Describe(), PokemonGenomes.For("Rhyperior")!.Describe());
        // Only a species with a female look has one
        Assert.Null(PokemonGenomes.For("Turtwig+f"));
        Assert.Null(PokemonGenomes.For("Meowstic+f"));

        // Generated, she is the same creature with another shape
        var m = PokemonModels.Finish(PokemonGenerator.Build("Rhyperior")!);
        var f = PokemonModels.Finish(PokemonGenerator.Build("Rhyperior+f")!);
        Assert.False(m.Mesh.Positions.AsSpan().SequenceEqual(f.Mesh.Positions));
        Assert.Equal(m.Plan, f.Plan);
        Assert.Equal(m.Skeleton.Count, f.Skeleton.Count);
        Assert.Equal(m.Decals.Count(d => d.IsEye), f.Decals.Count(d => d.IsEye));
        Assert.InRange(f.Height / m.Height, 0.9f, 1.1f);
    }

    [Fact]
    public void ThePokedexDrawsAFemaleSeenInHerOwnLook()
    {
        // The FORMS page (plan 08 · P19) draws each look seen; a female's is hers (plan 09 · L8)
        Assert.Equal("Shinx+f", new SeenLook("Shinx", Gender.Female).Look);
        Assert.Equal("Shinx", new SeenLook("Shinx", Gender.Male).Look);
        Assert.Equal("Shinx", new SeenLook("Shinx", Gender.Genderless).Look);
        Assert.Equal("Gyarados+s", new SeenLook("Gyarados+s", Gender.Genderless).Look);
        Assert.Equal("Turtwig", new SeenLook("Turtwig", Gender.Female).Look);
        Assert.Equal("Meowstic-Female", new SeenLook("Meowstic-Female", Gender.Female).Look);
        Assert.Equal("Pikachu-Belle", new SeenLook("Pikachu-Belle", Gender.Female).Look);
    }

    [Fact]
    public void ATorchicsSpotIsTheMalesAlone()
    {
        // The male has a black spot low on his back (plan 09 · L7); the female hasn't: her colours have no near-black
        static int Dark(PokeModel m) => m.Mesh.Colors.Count(c => c.R < 70 && c.G < 70 && c.B < 70);
        Assert.True(Dark(PokemonModels.Build("Torchic")) > 0);
        Assert.True(Dark(PokemonModels.Build("Torchic+f")) < Dark(PokemonModels.Build("Torchic")));
    }

    [Theory]
    [MemberData(nameof(Females))]
    public void EveryFemaleLookIsOfAFlaggedHandBuiltSpecies(string species)
    {
        var data = PokemonDatabase.Get(species);
        Assert.NotNull(data);
        Assert.True(data!.GenderDifferences, $"{species} has a female look, and the data says its females look like its males");
        Assert.True(PokemonModels.HasModel(species));
        Assert.Contains(species, PokemonModels.Females);
    }

    // ------------------------------------------------------------------ the look's name

    [Fact]
    public void AFemalesLookIsHerModelWithPlusFBeforeTheShine()
    {
        var male = Mon("Pikachu", 20);
        Assert.Equal("Pikachu", male.LookName);
        var female = Mon("Pikachu", 20);
        female.Gender = Gender.Female;
        Assert.Equal("Pikachu+f", female.LookName);
        female.IsShiny = true;
        Assert.Equal("Pikachu+f+s", female.LookName);
        // A costume is a form of its own, with no female look of its own (its tail is a heart already)
        female.IsShiny = false;
        female.Form = "Pikachu-Belle";
        Assert.Equal("Pikachu-Belle", female.LookName);
        // A species whose females look like its males has no female look
        var turtwig = Mon("Turtwig", 20);
        turtwig.Gender = Gender.Female;
        Assert.Equal("Turtwig", turtwig.LookName);
        // An Egg shows no look of its own
        var egg = Mon("Eevee", 1);
        egg.Gender = Gender.Female;
        egg.IsEgg = true;
        Assert.Equal("Eevee", egg.LookName);
    }

    [Theory]
    [InlineData("Pikachu+f+s", "Pikachu", "Pikachu+f", true, true)]
    [InlineData("Pikachu+f", "Pikachu", "Pikachu+f", true, false)]
    [InlineData("Pikachu+s", "Pikachu", "Pikachu", false, true)]
    [InlineData("Mr. Mime", "Mr. Mime", "Mr. Mime", false, false)]
    [InlineData("Charizard-Mega-X+s", "Charizard-Mega-X", "Charizard-Mega-X", false, true)]
    public void ALookNameIsTakenApartIntoItsModelAndItsSculpt(string look, string model, string sculpt, bool female, bool shiny)
    {
        Assert.Equal(model, PokemonLook.Base(look));
        Assert.Equal(sculpt, PokemonLook.Unshined(look));
        Assert.Equal(female, PokemonLook.IsFemale(look));
        Assert.Equal(shiny, PokemonLook.IsShiny(look));
        Assert.Equal(look, PokemonLook.Of(model, female, shiny));
    }

    [Fact]
    public void AFemaleLookOnlyFollowsAModelThatHasOne()
    {
        Assert.Equal("Rhydon+f", PokemonLook.Of("Rhydon", true, false));
        Assert.Equal("Rhydon", PokemonLook.Of("Rhydon", false, false));
        Assert.Equal("Nidoking", PokemonLook.Of("Nidoking", true, false));
        // The two-argument form keeps a female look as it is
        Assert.Equal("Rhydon+f+s", PokemonLook.Of("Rhydon+f", true));
        Assert.Equal("Rhydon+f", PokemonLook.Of("Rhydon+f+s", false));
    }

    // ------------------------------------------------------------------ the sculpts

    [Theory]
    [MemberData(nameof(Females))]
    public void AFemaleSignatureIsHerOwnAndTheMalesIsUntouched(string species)
    {
        string male = PokemonModels.Signature(species);
        string female = PokemonModels.Signature(species + "+f");
        Assert.NotEqual(male, female);
        Assert.NotEqual(PokemonModels.Signature(species + "+s"), PokemonModels.Signature(species + "+f+s"));
        Assert.NotEqual(female, PokemonModels.Signature(species + "+f+s"));
        // Asking for her never moves his
        Assert.Equal(male, PokemonModels.Signature(species));
    }

    [Theory]
    [MemberData(nameof(Females))]
    public void AFemaleSculptDiffersFromTheMales(string species)
    {
        var male = PokemonModels.Build(species);
        var female = PokemonModels.Build(species + "+f");
        bool shape = !male.Mesh.Positions.AsSpan().SequenceEqual(female.Mesh.Positions);
        bool colour = male.Mesh.Colors.Length != female.Mesh.Colors.Length || !male.Mesh.Colors.SequenceEqual(female.Mesh.Colors);
        bool marks = male.Decals.Count != female.Decals.Count;
        Assert.True(shape || colour || marks, $"{species}'s female is her male to the vertex, the colour and the mark");
        // The same creature: its plan, skeleton and eyes
        Assert.Equal(male.Plan, female.Plan);
        Assert.Equal(male.Skeleton.Count, female.Skeleton.Count);
        Assert.Equal(male.Decals.Count(d => d.IsEye), female.Decals.Count(d => d.IsEye));
        Assert.Equal(male.Hovers, female.Hovers);
        // And of about its size
        Assert.InRange(female.Height / male.Height, 0.9f, 1.1f);
    }

    [Theory]
    [MemberData(nameof(Females))]
    public void AFemaleIsInOnePiece(string species)
    {
        var pieces = Pieces(PokemonModels.Build(species + "+f").Mesh).Where(n => n > Speck).ToList();
        Assert.True(pieces.Count == 1, $"{species}'s female is in {pieces.Count} pieces of {string.Join(", ", pieces)} vertices");
    }

    [Theory]
    [MemberData(nameof(Females))]
    public void EachOfAFemalesDecalsCoversItsWholeSquare(string species)
    {
        var m = PokemonModels.Build(species + "+f");
        if (m.Decals.Count == 0) return;
        var uvs = m.DecalUVs!;
        for (int d = 0; d < m.Decals.Count; d++)
        {
            int col = d % m.AtlasColumns, row = d / m.AtlasColumns;
            var center = new Vector2((col + 0.5f) / m.AtlasColumns, (row + 0.5f) / m.AtlasRows);
            var cellHalf = new Vector2(0.25f / m.AtlasColumns, 0.25f / m.AtlasRows);
            bool left = false, right = false, top = false, bottom = false;
            foreach (var uv in uvs)
            {
                if (MathF.Abs(uv.X - center.X) > 2f * cellHalf.X || MathF.Abs(uv.Y - center.Y) > 2f * cellHalf.Y) continue;
                left |= uv.X <= center.X - cellHalf.X;
                right |= uv.X >= center.X + cellHalf.X;
                top |= uv.Y <= center.Y - cellHalf.Y;
                bottom |= uv.Y >= center.Y + cellHalf.Y;
            }
            Assert.True(left && right && top && bottom, $"{species}'s female: decal {d} ({(m.Decals[d].IsEye ? "eye" : "mark")}) isn't covered");
        }
    }

    [Fact]
    public void AShinyFemaleIsHerOwnSculptInHerSpeciesPalette()
    {
        var female = PokemonModels.Build("Pikachu+f");
        var shiny = PokemonModels.Build("Pikachu+f+s");
        Assert.Null(female.Palette);
        Assert.NotNull(shiny.Palette);
        Assert.Equal(female.Mesh.Positions, shiny.Mesh.Positions);
        // The palette is the species' own
        Assert.Equal(PokemonModels.Build("Pikachu+s").Palette!.Describe(), shiny.Palette!.Describe());
    }

    [Fact]
    public void AFemalePikachusTailEndsInAHeartAndAMalesIsUnchanged()
    {
        // The heart is a notch cut into the tail's last span: fewer of the female's vertices reach its tip
        var male = PokemonModels.Build("Pikachu");
        var female = PokemonModels.Build("Pikachu+f");
        var tip = new Vector3(0.18f, 0.53f, -0.35f);
        int Near(PokeModel m) => m.Mesh.Positions.Count(p => Vector3.Distance(p, tip) < 0.03f);
        Assert.True(Near(female) < Near(male), $"the female has {Near(female)} vertices at the tail's tip, the male {Near(male)}");
        // The male is the sculpt he always was: built twice the same
        Assert.Equal(male.Mesh.Positions, PokemonModels.Build("Pikachu").Mesh.Positions);
    }
}
