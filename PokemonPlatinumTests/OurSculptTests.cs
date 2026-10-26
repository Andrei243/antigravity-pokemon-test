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
    }


    /// <summary>Batch 5: the species of our own that evolve from Unova's (Nos. 2494 to 2649), sculpted from the user's reference pictures.</summary>
    private static PokemonSpecies[] Unova => PokemonDatabase.Ours().Where(s => s.DexNumber is >= 2494 and <= 2649).ToArray();

    [Fact]
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
    public void JellicrownsFemaleIsPinkAndHerOwn()
    {
        // Jellicent's female is a form of her own; Jellicrown keeps the difference as a look of the one species
        Assert.True(PokemonLook.HasFemale("Jellicrown"));
        Assert.NotEqual(PokemonModels.Signature("Jellicrown"), PokemonModels.Signature("Jellicrown+f"));
    }


    /// <summary>Batch 4: the species of our own that evolve from Sinnoh's (Nos. 2400 to 2482).</summary>
    private static PokemonSpecies[] Sinnoh => PokemonDatabase.Ours().Where(s => s.DexNumber is >= 2400 and <= 2499).ToArray();

    [Fact]
    public void EverySpeciesOfOursOfSinnohIsHandBuiltAndItsOwn()
    {
        Assert.Equal(39, Sinnoh.Length);
        foreach (var s in Sinnoh)
        {
            Assert.True(PokemonModels.HasModel(s.Name), $"{s.Name} has no sculpt of its own");
            Assert.Contains(s.Name, PokemonModels.Species);
            Assert.NotEqual(PokemonModels.Signature(ForebearOf(s).Name), PokemonModels.Signature(s.Name));
            Assert.Equal(s.Name, PokemonModels.Build(s.Name).Species);
        }
    }

    [Fact]
    public void ASinnohFemaleLookIsKeptWhereTheForebearsIs()
    {
        var flagged = Sinnoh.Where(s => s.GenderDifferences).Select(s => s.Name).OrderBy(n => n).ToArray();
        Assert.Equal(11, flagged.Length);
        Assert.All(flagged, n => Assert.Contains(n, PokemonLook.Females));
    }


    /// <summary>Batch 2: the species of our own that evolve from Johto's (Nos. 2152 to 2251).</summary>
    private static PokemonSpecies[] Johto => PokemonDatabase.Ours().Where(s => s.DexNumber is >= 2152 and <= 2251).ToArray();

    [Fact]
    public void EverySpeciesOfOursOfJohtoIsHandBuiltAndNotItsForebear()
    {
        Assert.Equal(34, Johto.Length);
        foreach (var s in Johto)
        {
            Assert.True(PokemonModels.HasModel(s.Name), $"{s.Name} has no sculpt of its own");
            Assert.Contains(s.Name, PokemonModels.Species);
            Assert.NotEqual(PokemonModels.Signature(ForebearOf(s).Name), PokemonModels.Signature(s.Name));
            Assert.Equal(s.Name, PokemonModels.Build(s.Name).Species);
        }
    }

    [Fact]
    public void EachJohtoDecalCoversItsWholeSquare()
    {
        // As PokemonModelTests.EachDecalCoversItsWholeSquare, for this batch alone (that test walks every model)
        foreach (var s in Johto)
            foreach (var name in s.GenderDifferences ? new[] { s.Name, s.Name + PokemonLook.FemaleSuffix } : new[] { s.Name })
            {
                var m = PokemonModels.Get(name);
                var uvs = m.DecalUVs!;
                for (int d = 0; d < m.Decals.Count; d++)
                {
                    int col = d % m.AtlasColumns, row = d / m.AtlasColumns;
                    var center = new System.Numerics.Vector2((col + 0.5f) / m.AtlasColumns, (row + 0.5f) / m.AtlasRows);
                    var half = new System.Numerics.Vector2(0.25f / m.AtlasColumns, 0.25f / m.AtlasRows);
                    bool left = false, right = false, top = false, bottom = false;
                    foreach (var uv in uvs)
                    {
                        if (System.MathF.Abs(uv.X - center.X) > 2f * half.X || System.MathF.Abs(uv.Y - center.Y) > 2f * half.Y) continue;
                        left |= uv.X <= center.X - half.X;
                        right |= uv.X >= center.X + half.X;
                        top |= uv.Y <= center.Y - half.Y;
                        bottom |= uv.Y >= center.Y + half.Y;
                    }
                    Assert.True(left && right && top && bottom, $"{name}: decal {d} isn't covered");
                }
            }
    }

    [Fact]
    public void AJohtoFemaleLookIsKeptWhereTheForebearsIs()
    {
        var flagged = Johto.Where(s => s.GenderDifferences).Select(s => s.Name).ToArray();
        Assert.Equal(11, flagged.Length);
        Assert.All(flagged, n => Assert.Contains(n, PokemonLook.Females));
    }

    /// <summary>Batch 8: the species of our own that evolve from Galar's and Hisui's (Nos. 2810 to 2905), with Ursensei and the supplement's designs grown from regional forms (Nos. 5000 to 6999).</summary>
    private static PokemonSpecies[] Galar => PokemonDatabase.Ours().Where(s => s.DexNumber is >= 2810 and <= 2905 or >= 5000 and < 7000).ToArray();

    [Fact]
    public void EverySpeciesOfOursOfGalarAndHisuiIsHandBuiltAndItsOwn()
    {
        Assert.Equal(58, Galar.Length);
        foreach (var s in Galar)
        {
            Assert.True(PokemonModels.HasModel(s.Name), $"{s.Name} has no sculpt of its own");
            Assert.Contains(s.Name, PokemonModels.Species);
            Assert.Equal(s.Name, PokemonModels.Build(s.Name).Species);
        }
    }

    [Fact]
    public void EachGalarDecalCoversItsWholeSquare()
    {
        // As PokemonModelTests.EachDecalCoversItsWholeSquare, for this batch alone (that test walks every model)
        foreach (var s in Galar)
            foreach (var name in s.GenderDifferences ? new[] { s.Name, s.Name + PokemonLook.FemaleSuffix } : new[] { s.Name })
            {
                var m = PokemonModels.Get(name);
                var uvs = m.DecalUVs!;
                for (int d = 0; d < m.Decals.Count; d++)
                {
                    int col = d % m.AtlasColumns, row = d / m.AtlasColumns;
                    var center = new System.Numerics.Vector2((col + 0.5f) / m.AtlasColumns, (row + 0.5f) / m.AtlasRows);
                    var half = new System.Numerics.Vector2(0.25f / m.AtlasColumns, 0.25f / m.AtlasRows);
                    bool left = false, right = false, top = false, bottom = false;
                    foreach (var uv in uvs)
                    {
                        if (System.MathF.Abs(uv.X - center.X) > 2f * half.X || System.MathF.Abs(uv.Y - center.Y) > 2f * half.Y) continue;
                        left |= uv.X <= center.X - half.X;
                        right |= uv.X >= center.X + half.X;
                        top |= uv.Y <= center.Y - half.Y;
                        bottom |= uv.Y >= center.Y + half.Y;
                    }
                    Assert.True(left && right && top && bottom, $"{name}: decal {d} isn't covered");
                }
            }
    }

    [Fact]
    public void AGalarFemaleLookIsKeptWhereTheForebearsIs()
    {
        // Indeedee's and Basculegion's females are forms of their own; Majordomee and Shoalwraith keep the difference as
        // a look of the one species, sculpted from the forebear's female form
        var flagged = Galar.Where(s => s.GenderDifferences).Select(s => s.Name).OrderBy(n => n).ToArray();
        Assert.Equal(new[] { "Majordomee", "Shoalwraith" }, flagged);
        foreach (var n in flagged)
        {
            Assert.Contains(n, PokemonLook.Females);
            Assert.NotEqual(PokemonModels.Signature(n), PokemonModels.Signature(n + PokemonLook.FemaleSuffix));
        }
    }
}
