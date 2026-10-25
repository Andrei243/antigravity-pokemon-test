using System;
using System.Linq;
using System.Text.Json;
using DataImporter;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.Frontier;
using Xunit;
using static PokemonPlatinumTests.Scenario;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 03 · D14, batch 8's supplement (Galar and Hisui: docs/plans/D14-proposals-galar-supplement.md, all 16 designs
/// approved by the user on 2026-10-26, every question at its default): the 15 regional forms of Galar and Hisui that
/// end lines of one or two, each a row grown from its form (the evolution's <c>fromForm</c>) and numbered by question 1
/// (Galar 5000, Hisui 6000, + the species' number), and Urshifu's Ursensei from either style. The rules every design is
/// held to are <see cref="SpeciesPastHundredTests"/>'; here what is new: a design grown from a form, from that form alone.
/// </summary>
public class SpeciesPastHundredGalarSupplementTests
{
    private static PokemonSpecies Species(string name) => PokemonDatabase.Get(name)!;

    private static string? Find(Pokemon p, EvolutionTrigger trigger, EvolutionContext? context = null) =>
        Evolution.Find(p, trigger, context ?? new EvolutionContext())?.TargetSpecies;

    private static string? LevelUp(Pokemon p, EvolutionContext? context = null) => Find(p, EvolutionTrigger.LevelUp, context);

    private static Pokemon InForm(string species, string? form, int level, params string[] moves)
    {
        var p = Mon(species, level, moves);
        if (form != null) p.ChangeForm(form);
        return p;
    }

    private static readonly (string Name, string Form, int Number)[] Forms =
    {
        ("Glimmane", "Rapidash-Galar", 5078), ("Slowslinger", "Slowbro-Galar", 5080), ("Slowmagus", "Slowking-Galar", 5199),
        ("Purifume", "Weezing-Galar", 5110), ("Frostruma", "Darmanitan-Galar-Standard", 5555), ("Snareflat", "Stunfisk-Galar", 5618),
        ("Hypnoskye", "Articuno-Galar", 5144), ("Stridestorm", "Zapdos-Galar", 5145), ("Duskphoenix", "Moltres-Galar", 5146),
        ("Obsidhound", "Arcanine-Hisui", 6059), ("Apricharge", "Electrode-Hisui", 6101), ("Fandangleaf", "Lilligant-Hisui", 6549),
        ("Spectrevix", "Zoroark-Hisui", 6571), ("Augurhawk", "Braviary-Hisui", 6628), ("Bergbreaker", "Avalugg-Hisui", 6713)
    };

    [Fact]
    public void TheSupplementsSixteenAreGrownFromTheirFormsAndNumberedByRegion()
    {
        var designs = SpeciesPastHundred.Load(SpeciesPastHundred.PathIn(Repository.Root)).ToDictionary(d => d.Name);
        foreach (var (name, form, number) in Forms)
        {
            var d = designs[name];
            Assert.Equal(form, d.Evolution.FromForm);
            Assert.Equal(FormKind.Regional, Species(d.From).Form(form)!.Kind);
            Assert.Equal(number, Species(name).DexNumber);
            Assert.Same(Species(name), PokemonDatabase.GetByDex(number));
            bool legend = Species(d.From).Legendary;
            var (low, high) = form.Contains("Hisui") ? (238, 240) : (226, legend ? 244 : 240);
            Assert.InRange(d.Evolution.Level, low, high);

            // Told of as first found in Galar, where its form came from, not where its species did
            Assert.Contains("Galar", Species(name).DexEntry);
        }
        Assert.Equal(2892, Species("Ursensei").DexNumber);
        Assert.Null(designs["Ursensei"].Evolution.FromForm);

        // Question 1's blocks: Alola 4000, Galar 5000, Hisui 6000, Paldea 7000, a species' own design 2000
        Assert.Equal(4026, SpeciesPastHundred.NumberOf(Species("Raichu"), "Raichu-Alola"));
        Assert.Equal(7128, SpeciesPastHundred.NumberOf(Species("Tauros"), "Tauros-Paldea-Combat-Breed"));
        Assert.Equal(2078, SpeciesPastHundred.NumberOf(Species("Rapidash"), null));

        // The birds a set of three at 244 holding their type's item, to 660; Ursensei +80 to 630; Frostruma's Slush Rush
        foreach (var (bird, item) in new[] { ("Hypnoskye", "Twisted Spoon"), ("Stridestorm", "Black Belt"), ("Duskphoenix", "Black Glasses") })
        {
            Assert.Equal((EvolutionMethod.LevelHoldingItem, 244, item), (designs[bird].Evolution.Method, designs[bird].Evolution.Level, designs[bird].Evolution.Item));
            Assert.Equal(660, designs[bird].Total);
            Assert.True(Species(bird).Legendary);
        }
        Assert.Equal(630, designs["Ursensei"].Total);
        Assert.Equal("Slush Rush", Species("Frostruma").HiddenAbility);
        Assert.Equal(600, designs["Obsidhound"].Total);

        // No design for the Hisuian lines of three (question 2)
        Assert.All(new[] { "Typhlosion", "Samurott", "Decidueye", "Goodra" },
            name => Assert.DoesNotContain(Species(name).Evolutions ?? new(), e => e.FromForm != null && Species(e.TargetSpecies).IsOurs));
    }

    [Fact]
    public void EachFiresFromItsFormAndNeitherFromTheSpeciesNorTheOtherWayRound()
    {
        // The Galarian Rapidash knowing Psycho Cut becomes Glimmane; Kanto's knowing it doesn't, and the Galarian one by
        // day at Solarion's level becomes nothing
        Assert.Equal("Glimmane", LevelUp(InForm("Rapidash", "Rapidash-Galar", 230, "Psycho Cut"), new EvolutionContext { IsNight = true }));
        Assert.Null(LevelUp(InForm("Rapidash", null, 230, "Psycho Cut"), new EvolutionContext { IsNight = true }));
        Assert.Null(LevelUp(InForm("Rapidash", "Rapidash-Galar", 230, "Tackle"), new EvolutionContext { IsNight = false }));
        Assert.Equal("Solarion", LevelUp(InForm("Rapidash", null, 230, "Tackle"), new EvolutionContext { IsNight = false }));

        // Weezing: the Galarian one past Smoglom's level and knowing Strange Steam; Kanto's knowing it becomes Smoglom
        Assert.Equal("Purifume", LevelUp(InForm("Weezing", "Weezing-Galar", 230, "Strange Steam")));
        Assert.Null(LevelUp(InForm("Weezing", "Weezing-Galar", 229, "Strange Steam")));
        Assert.Equal("Smoglom", LevelUp(InForm("Weezing", null, 230, "Strange Steam")));

        // The birds: each holding the other region's item is not enough
        foreach (var (species, form, ours, theirItem, kanto, kantoItem) in new[]
        {
            ("Articuno", "Articuno-Galar", "Hypnoskye", "Twisted Spoon", "Glacirune", "Never-Melt Ice"),
            ("Zapdos", "Zapdos-Galar", "Stridestorm", "Black Belt", "Fulguris", "Magnet"),
            ("Moltres", "Moltres-Galar", "Duskphoenix", "Black Glasses", "Pyrevant", "Charcoal")
        })
        {
            var galar = InForm(species, form, 244);
            galar.HeldItem = ItemDatabase.Get(kantoItem);
            Assert.Null(LevelUp(galar));
            galar.HeldItem = ItemDatabase.Get(theirItem);
            Assert.Equal(ours, LevelUp(galar));
            var kantos = InForm(species, null, 244);
            kantos.HeldItem = ItemDatabase.Get(theirItem);
            Assert.Null(LevelUp(kantos));
            kantos.HeldItem = ItemDatabase.Get(kantoItem);
            Assert.Equal(kanto, LevelUp(kantos));
        }

        // A stone from the bag: the Hisuian form takes it, the species doesn't (nor the Galarian Darmanitan in its Zen Mode)
        foreach (var (species, form, ours, stone, level) in new[]
        {
            ("Arcanine", "Arcanine-Hisui", "Obsidhound", "Fire Stone", 238), ("Electrode", "Electrode-Hisui", "Apricharge", "Leaf Stone", 238),
            ("Lilligant", "Lilligant-Hisui", "Fandangleaf", "Sun Stone", 238), ("Darmanitan", "Darmanitan-Galar-Standard", "Frostruma", "Ice Stone", 231)
        })
        {
            var context = new EvolutionContext { Item = ItemDatabase.Get(stone) };
            Assert.Equal(ours, Find(InForm(species, form, level), EvolutionTrigger.UseItem, context));
            Assert.Null(Find(InForm(species, form, level - 1), EvolutionTrigger.UseItem, context));
            Assert.NotEqual(ours, Find(InForm(species, null, level), EvolutionTrigger.UseItem, context));
        }
        Assert.Null(Find(InForm("Darmanitan", "Darmanitan-Galar-Zen", 231), EvolutionTrigger.UseItem, new EvolutionContext { Item = ItemDatabase.Get("Ice Stone") }));
        Assert.Null(LevelUp(InForm("Darmanitan", "Darmanitan-Galar-Standard", 231), new EvolutionContext { IsNight = false }));

        // Zoroark by night, Braviary by day, Stunfisk in the rain, Avalugg at the Ice Rock: each its form's
        Assert.Equal("Spectrevix", LevelUp(InForm("Zoroark", "Zoroark-Hisui", 238), new EvolutionContext { IsNight = true }));
        Assert.Equal("Zoromirage", LevelUp(InForm("Zoroark", null, 238), new EvolutionContext { IsNight = true }));
        Assert.Equal("Augurhawk", LevelUp(InForm("Braviary", "Braviary-Hisui", 239), new EvolutionContext { IsNight = false }));
        Assert.Equal("Snareflat", LevelUp(InForm("Stunfisk", "Stunfisk-Galar", 229), new EvolutionContext { IsRaining = true }));
        Assert.Equal("Fluxflounder", LevelUp(InForm("Stunfisk", null, 229), new EvolutionContext { IsRaining = true }));
        Assert.Equal("Bergbreaker", LevelUp(InForm("Avalugg", "Avalugg-Hisui", 239), new EvolutionContext { Sites = new[] { "Ice Rock" } }));
        Assert.Null(LevelUp(InForm("Avalugg", null, 239), new EvolutionContext { Sites = new[] { "Ice Rock" } }));
    }

    [Fact]
    public void AFormThatEvolvesIntoOneOfOursLeavesItsFormBehind()
    {
        var p = InForm("Rapidash", "Rapidash-Galar", 230, "Psycho Cut");
        var found = Evolution.Find(p, EvolutionTrigger.LevelUp, new EvolutionContext { IsNight = true })!;
        var outcome = Evolution.Evolve(p, found, new EvolutionContext());
        Assert.Equal(("Rapidash", "Glimmane"), (outcome.From.Name, outcome.Into.Name));
        Assert.Null(p.Form);
        Assert.True(p.HasType(PokemonType.Psychic) && p.HasType(PokemonType.Fairy));
        Assert.Contains(p.AbilityName, Species("Glimmane").Abilities);

        // Saved and read back, found by its number in Galar's block
        var saved = SavedPokemonData.FromPokemon(p);
        Assert.Equal(5078, saved.DexNumber);
        var read = JsonSerializer.Deserialize<SavedPokemonData>(JsonSerializer.Serialize(saved, GameDataFiles.Json), GameDataFiles.Json)!.ToPokemon();
        Assert.Equal(("Glimmane", 230), (read.Species.Name, read.Level));
        saved.SpeciesName = "Missingno";
        Assert.Equal("Glimmane", saved.ToPokemon().Species.Name);
    }

    [Fact]
    public void UrshifuOfEitherStyleBecomesUrsenseiByItsFriendship()
    {
        foreach (var style in new string?[] { null, "Urshifu-Rapid-Strike" })
        {
            var p = InForm("Urshifu", style, 244, "Tackle");
            p.Friendship = FriendshipRules.EvolveAt - 1;
            Assert.Null(LevelUp(p));
            p.Friendship = FriendshipRules.EvolveAt;
            var early = InForm("Urshifu", style, 243);
            early.Friendship = 255;
            Assert.Null(LevelUp(early));
            var found = Evolution.Find(p, EvolutionTrigger.LevelUp, new EvolutionContext());
            Assert.Equal("Ursensei", found?.TargetSpecies);

            // The Rapid Strike style loses its Water on evolving: one Ursensei, Fighting/Dark, whatever the style
            Evolution.Evolve(p, found!, new EvolutionContext());
            Assert.Null(p.Form);
            Assert.Equal((PokemonType.Fighting, (PokemonType?)PokemonType.Dark), (p.PrimaryType, p.SecondaryType));
        }

        // A legendary's evolution: banned from the Battle Frontier, caught at 3
        Assert.False(FrontierRules.MayEnter(Mon("Ursensei", 244)));
        Assert.Equal(3, Species("Ursensei").CatchRate);
        Assert.False(FrontierRules.MayEnter(Mon("Hypnoskye", 244)));
        Assert.True(FrontierRules.MayEnter(Mon("Glimmane", 230)));
    }
}
