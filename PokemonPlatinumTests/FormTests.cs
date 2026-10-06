using System;
using System.Linq;
using Xunit;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumTests;

/// <summary>
/// Forms (plan 03 · D11): what the data holds, what a Pokémon in a form is, how it is saved, what the modern rules
/// change in a species, and the model each form is shown with. How forms evolve is in <see cref="EvolutionTests"/>.
/// </summary>
public class FormTests
{
    /// <summary>A Pokémon with no IVs and a neutral nature, so its stats are the same every run.</summary>
    private static Pokemon Mon(string species, int level, Gender gender = Gender.Male) =>
        new(PokemonDatabase.Get(species)!, level, gender, Nature.Hardy, false);

    private static PokemonForm Form(string name) => PokemonDatabase.SpeciesOfForm(name)!.Form(name)!;

    [Fact]
    public void TheFormsOfEveryGenerationAreInTheData()
    {
        var forms = PokemonDatabase.GetAll().SelectMany(s => s.Forms ?? new()).ToList();
        Assert.Equal(97, forms.Count(f => f.Kind == FormKind.Mega));
        Assert.Equal(2, forms.Count(f => f.Kind == FormKind.Primal));
        Assert.Equal(34, forms.Count(f => f.Kind == FormKind.Gigantamax));
        Assert.Equal(57, forms.Count(f => f.Kind == FormKind.Regional));

        // Each answers to a name of its own, its species' and the form's, which no species has
        Assert.Equal(forms.Count, forms.Select(f => f.Name.ToLowerInvariant()).Distinct().Count());
        Assert.All(forms, f => Assert.Null(PokemonDatabase.Get(f.Name)));
        Assert.All(forms, f => Assert.StartsWith(PokemonDatabase.SpeciesOfForm(f.Name)!.Name + "-", f.Name));
        Assert.Same(PokemonDatabase.Get("Rotom"), PokemonDatabase.SpeciesOfForm("rotom-heat"));
        Assert.Null(PokemonDatabase.SpeciesOfForm("Rotom-Oven"));

        // Every Mega Stone brings out a Mega form of its species
        foreach (var stone in ItemDatabase.GetAll().Where(i => i.MegaStone != null))
        {
            var owner = PokemonDatabase.SpeciesOfForm(stone.MegaStone!.Form);
            Assert.Equal(stone.MegaStone.Species, owner?.Name);
            Assert.Equal(FormKind.Mega, owner!.Form(stone.MegaStone.Form)!.Kind);
        }

        // Unown's letters and Pikachu's caps are only looks; Arceus's plates are other types; Galarian Meowth is
        // another Pokémon; Castform's weathers are held in battle only
        Assert.Equal(FormKind.Look, Form("Unown-B").Kind);
        Assert.Equal(FormKind.Look, Form("Pikachu-Original-Cap").Kind);
        Assert.Equal(new[] { PokemonType.Fire }, Form("Arceus-Fire").Types);
        Assert.Equal(FormKind.Regional, Form("Meowth-Galar").Kind);
        Assert.Equal(new[] { PokemonType.Steel }, Form("Meowth-Galar").Types);
        Assert.Equal(FormKind.Battle, Form("Castform-Sunny").Kind);
        // A form held only in battle keeps the moves it had: it has no learnset of its own
        Assert.All(forms.Where(f => f.Kind is FormKind.Mega or FormKind.Primal or FormKind.Gigantamax or FormKind.Battle), f => Assert.Null(f.Learnset));
    }

    [Fact]
    public void PlatinumsOwnFormsHaveItsValues()
    {
        // The decompilation's: Rotom's appliances kept its Electric and Ghost types until Generation 5
        var heat = Form("Rotom-Heat");
        Assert.Equal(FormKind.Alternate, heat.Kind);
        Assert.Null(heat.Types);
        Assert.Equal((65, 107, 105, 107, 86), (heat.BaseStats!.Attack, heat.BaseStats.Defense, heat.BaseStats.SpAttack, heat.BaseStats.SpDefense, heat.BaseStats.Speed));
        Assert.Equal(new[] { PokemonType.Electric, PokemonType.Fire }, heat.Under(Ruleset.Modern).Types);
        Assert.Null(heat.Under(Ruleset.Platinum).Types);
        Assert.Null(heat.Types); // asking leaves the data as it is

        var origin = Form("Giratina-Origin");
        Assert.Equal(new[] { "Levitate" }, origin.Abilities);
        Assert.Equal((150, 120, 100, 120, 100, 90), (origin.BaseStats!.HP, origin.BaseStats.Attack, origin.BaseStats.Defense,
            origin.BaseStats.SpAttack, origin.BaseStats.SpDefense, origin.BaseStats.Speed));
        Assert.Equal((6.9f, 650f), (origin.Height, origin.Weight));

        Assert.Equal(new[] { PokemonType.Bug, PokemonType.Ground }, Form("Wormadam-Sandy").Types);
        Assert.Equal(new[] { PokemonType.Grass, PokemonType.Flying }, Form("Shaymin-Sky").Types);
        Assert.Equal(new[] { "Serene Grace" }, Form("Shaymin-Sky").Abilities);
    }

    [Fact]
    public void SpeciesTakeTheNewestValuesOnlyUnderTheModernRules()
    {
        // Clefairy has been a Fairy type since Generation 6, and Pikachu's defences went up then
        var clefairy = PokemonDatabase.Get("Clefairy")!;
        Assert.Equal(PokemonType.Normal, clefairy.PrimaryType);
        Assert.Equal(PokemonType.Fairy, clefairy.Under(Ruleset.Modern).PrimaryType);
        Assert.Equal(PokemonType.Normal, clefairy.Under(Ruleset.Platinum).PrimaryType);
        Assert.Equal(PokemonType.Normal, clefairy.PrimaryType);

        var pikachu = PokemonDatabase.Get("Pikachu")!;
        Assert.Equal((30, 40), (pikachu.BaseDefense, pikachu.BaseSpDefense));
        var modern = pikachu.Under(Ruleset.Modern);
        Assert.Equal((40, 50), (modern.BaseDefense, modern.BaseSpDefense));
        // The forms go with it
        Assert.Equal(new[] { PokemonType.Electric, PokemonType.Water }, PokemonDatabase.Get("Rotom")!.Under(Ruleset.Modern).Form("Rotom-Wash")!.Types);

        // A species the newer games left as it was is the same under either rules
        var turtwig = PokemonDatabase.Get("Turtwig")!;
        var modernTurtwig = turtwig.Under(Ruleset.Modern);
        Assert.Null(turtwig.Modern);
        Assert.Equal((turtwig.PrimaryType, turtwig.BaseAttack, turtwig.BaseSpeed), (modernTurtwig.PrimaryType, modernTurtwig.BaseAttack, modernTurtwig.BaseSpeed));
        Assert.True(Ruleset.Modern.ModernSpeciesValues);
        Assert.False(Ruleset.Platinum.ModernSpeciesValues);
    }

    [Fact]
    public void APokemonInAFormHasTheFormsTypesStatsAndAbilities()
    {
        var meowth = Mon("Meowth", 30);
        meowth.AbilityName = "Technician";
        Assert.Equal(32, meowth.Attack); // 2 × 45 × 30 / 100 + 5

        meowth.ChangeForm("meowth-galar");
        Assert.Equal("Meowth-Galar", meowth.Form);
        Assert.Equal("Meowth-Galar", meowth.ModelName);
        Assert.Equal((PokemonType.Steel, (PokemonType?)null), (meowth.PrimaryType, meowth.SecondaryType));
        Assert.True(meowth.HasType(PokemonType.Steel));
        Assert.False(meowth.HasType(PokemonType.Normal));
        Assert.Equal(44, meowth.Attack); // 2 × 65 × 30 / 100 + 5
        Assert.Equal("Tough Claws", meowth.AbilityName); // the second ability stays the second
        Assert.NotSame(meowth.Species.Learnset, meowth.Learnset);
        Assert.Equal("Meowth", meowth.Species.Name);

        // Back to the species' own
        meowth.ChangeForm(null);
        Assert.Equal((PokemonType.Normal, "Technician", 32), (meowth.PrimaryType, meowth.AbilityName, meowth.Attack));
        Assert.Equal("Meowth", meowth.ModelName);
        Assert.Throws<ArgumentException>(() => meowth.ChangeForm("Pikachu-Libre"));

        // Hit points go with the maximum: Zygarde's whole form doubles its base HP (168 → 276 at level 50)
        var zygarde = Mon("Zygarde", 50);
        Assert.Equal(168, zygarde.MaxHP);
        zygarde.CurrentHP = 100;
        zygarde.ChangeForm("Zygarde-Complete");
        Assert.Equal((276, 208), (zygarde.MaxHP, zygarde.CurrentHP));
        zygarde.ChangeForm(null);
        Assert.Equal((168, 100), (zygarde.MaxHP, zygarde.CurrentHP));

        // The rest of the form's values
        var shaymin = Mon("Shaymin", 30);
        shaymin.ChangeForm("Shaymin-Sky");
        Assert.Equal((0.4f, 5.2f), (shaymin.Height, shaymin.Weight));
        Assert.Equal("Serene Grace", shaymin.AbilityName);
    }

    [Fact]
    public void FemalesOfSomeSpeciesAreAFormOfTheirOwn()
    {
        Assert.Equal("Meowstic-Female", Mon("Meowstic", 30, Gender.Female).Form);
        Assert.Null(Mon("Meowstic", 30).Form);
        Assert.Equal("Basculegion-Female", Mon("Basculegion", 40, Gender.Female).Form);
        Assert.Equal(92, Form("Basculegion-Female").BaseStats!.Attack);
        // Pyroar's mane is only a look; most species' females are the species itself
        Assert.Equal("Pyroar-Female", Mon("Pyroar", 40, Gender.Female).Form);
        Assert.Null(Mon("Luxray", 40, Gender.Female).Form);

        // Met in the wild, whichever it turns out to be
        for (int seed = 0; seed < 20; seed++)
        {
            var met = new Pokemon(PokemonDatabase.Get("Meowstic")!, 20, new Random(seed));
            Assert.Equal(met.Gender == Gender.Female ? "Meowstic-Female" : null, met.Form);
            Assert.Contains(met.AbilityName, met.Abilities);
        }

        // And by evolving: a female Litleo becomes a female Pyroar
        var litleo = Mon("Litleo", 35, Gender.Female);
        Evolution.Evolve(litleo, Evolution.Find(litleo, EvolutionTrigger.LevelUp, new EvolutionContext())!, new EvolutionContext());
        Assert.Equal(("Pyroar", "Pyroar-Female"), (litleo.Species.Name, litleo.Form));
    }

    [Fact]
    public void AFormIsSavedAndCopied()
    {
        var rotom = Mon("Rotom", 25);
        rotom.ChangeForm("Rotom-Wash");
        var saved = SavedPokemonData.FromPokemon(rotom);
        Assert.Equal("Rotom-Wash", saved.Form);
        var back = saved.ToPokemon();
        Assert.Equal("Rotom-Wash", back.Form);
        Assert.Equal((rotom.Defense, rotom.SpAttack, rotom.AbilityName), (back.Defense, back.SpAttack, back.AbilityName));

        var copy = Mon("Rotom", 25);
        copy.CopyStateFrom(rotom);
        Assert.Equal("Rotom-Wash", copy.Form);
        Assert.Equal(rotom.Defense, copy.Defense);

        // A save from before forms existed: the species' own
        Assert.Null(new SavedPokemonData { SpeciesName = "Rotom", Level = 25 }.ToPokemon().Form);
    }

    [Fact]
    public void EveryFormOfAHandBuiltSpeciesIsHandBuilt()
    {
        // Every form of a hand-built species has a sculpt of its own: Platinum's own forms of its Sinnoh species
        // (Rotom's appliances, Giratina's Origin Forme, the cloaks, the East Sea, Cherrim in the sun, every letter and
        // sign of the Unown but A, which is the species itself) and the later games' (the regional forms, Dialga's and
        // Palkia's Origin Formes, the Megas, the Gigantamax forms, Pikachu's caps and costumes, the spiky-eared Pichu),
        // the forms of the Kanto, Johto, Hoenn and Unova species hand-built since among them (Castform's weathers, Deoxys's
        // formes, the Primal Kyogre and Groudon, Basculin's stripes, Darmanitan's Zen Modes). The few that look just like their species show
        // its sculpt.
        var sameLook = new[] { "Mothim-Sandy", "Mothim-Trash", "Pikachu-Starter", "Eevee-Starter" };
        Assert.Equal(199, PokemonModels.Forms.Length);
        Assert.Equal(PokemonModels.Forms.Length, PokemonModels.Forms.Distinct().Count());
        foreach (var form in PokemonModels.Forms)
        {
            var owner = PokemonDatabase.SpeciesOfForm(form);
            Assert.True(owner != null && PokemonModels.Species.Contains(owner.Name), form);
            Assert.Equal(form, owner!.Form(form)!.Name);
        }
        foreach (var species in PokemonModels.Species)
            foreach (var form in PokemonDatabase.Get(species)!.Forms ?? new())
            {
                bool same = sameLook.Contains(form.Name);
                Assert.True(PokemonModels.HasModel(form.Name) != same, form.Name);
                Assert.True(PokemonModels.Signature(species) == PokemonModels.Signature(form.Name) == same, form.Name);
            }
        // No two hand-built sculpts are the same: the cloaks, Mega Raichu X and Y, a Mega and its Z form
        var all = PokemonModels.Species.Concat(PokemonModels.Forms).ToList();
        Assert.Equal(all.Count, all.Select(PokemonModels.Signature).Distinct().Count());
    }

    [Fact]
    public void EachFormIsShownByAModelOfItsOwnOrItsHandBuiltSpecies()
    {
        // A generated form is sculpted from its own data, in its own colour, as one of its species' family
        var charizard = PokemonGenomes.For("Charizard")!;
        var megaX = PokemonGenomes.For("Charizard-Mega-X")!;
        Assert.Equal("Charizard-Mega-X", megaX.Species);
        Assert.Equal((PokemonType.Fire, (PokemonType?)PokemonType.Dragon), (megaX.Primary, megaX.Secondary));
        Assert.Equal(PokemonType.Flying, charizard.Secondary);
        Assert.Equal("Black", Form("Charizard-Mega-X").Color);
        Assert.NotEqual(charizard.Main, megaX.Main);
        Assert.Null(PokemonGenomes.For("Charizard-Mega-Z"));
        Assert.NotEqual(PokemonModels.Signature("Vulpix"), PokemonModels.Signature("Vulpix-Alola"));

        // Platinum's values, whatever rules the game is played by: Clefairy is sculpted a Normal type, and Rotom's
        // appliances keep its Ghost type
        Assert.Equal(PokemonType.Normal, PokemonGenomes.For("Clefairy")!.Primary);
        Assert.Equal(PokemonType.Ghost, PokemonGenomes.For("Rotom-Wash")!.Secondary);

        // A form that looks just like its hand-built species shows its species' sculpt
        Assert.False(PokemonModels.HasModel("Mothim-Sandy"));
        Assert.Equal(PokemonModels.Signature("Mothim"), PokemonModels.Signature("Mothim-Sandy"));
        Assert.Equal(PokemonModels.Signature("Pikachu"), PokemonModels.Signature("Pikachu-Starter"));

        // Every other form of a hand-built species is hand-built, a sculpt of its own (above)
        Assert.True(PokemonModels.HasModel("Rotom-Heat"));
        Assert.NotEqual(PokemonModels.Signature("Rotom"), PokemonModels.Signature("Rotom-Heat"));
        Assert.True(PokemonModels.HasModel("Garchomp-Mega"));
        Assert.NotEqual(PokemonModels.Signature("Garchomp"), PokemonModels.Signature("Garchomp-Mega"));

        // Every form has a genome that sculpts
        foreach (var form in PokemonDatabase.GetAll().SelectMany(s => s.Forms ?? new()))
        {
            var g = PokemonGenomes.For(form.Name);
            Assert.NotNull(g);
            Assert.NotNull(PokemonGenerator.Build(g!));
        }
    }
}
