using System.Collections.Generic;
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
/// Plan 03 · D14, batch 7's supplement (Alola: docs/plans/D14-proposals-alola-supplement.md, all ten designs approved by
/// the user on 2026-10-26 with every question at its default): the Ultra Beasts as a legendary set, each at 225 with any
/// other Ultra Beast in the party (Poipole among them), to 660. The two small changes it needed are held here too: an
/// evolution row that names several species (<see cref="EvolutionData.AnyOfSpecies"/>) and a row's own
/// <see cref="SpeciesDesign.Legendary"/>.
/// </summary>
public class SpeciesPastHundredAlolaSupplementTests
{
    private static readonly (string From, string Into)[] Set =
    {
        ("Nihilego", "Nihiliad"), ("Buzzwole", "Buzzcolossus"), ("Pheromosa", "Pheromuse"), ("Xurkitree", "Xurkigrove"),
        ("Celesteela", "Celestrata"), ("Kartana", "Origamurai"), ("Guzzlord", "Devourmaw"), ("Naganadel", "Naganapex"),
        ("Stakataka", "Ziggurak"), ("Blacephalon", "Pyrocephalon")
    };

    /// <summary>The eleven Ultra Beasts of the data: the ten that grow and Poipole.</summary>
    private static readonly string[] UltraBeasts = Set.Select(s => s.From).Append("Poipole").ToArray();

    private static PokemonSpecies Species(string name) => PokemonDatabase.Get(name)!;

    private static int Total(PokemonSpecies s)
    {
        var x = SpeciesPastHundred.StatsOf(s);
        return x.HP + x.Attack + x.Defense + x.SpAttack + x.SpDefense + x.Speed;
    }

    private static string? LevelUp(Pokemon p, EvolutionContext context) => Evolution.Find(p, EvolutionTrigger.LevelUp, context)?.TargetSpecies;

    private static Party PartyOf(params Pokemon[] members)
    {
        var party = new Party();
        foreach (var m in members) party.Add(m);
        return party;
    }

    [Fact]
    public void TheTenAreRowsOfTheTableAfterTheirForebears()
    {
        var designs = SpeciesPastHundred.Load(SpeciesPastHundred.PathIn(Repository.Root)).Where(d => d.Legendary == true).ToList();
        Assert.Equal(Set.Select(s => s.Into), designs.Select(d => d.Name));
        foreach (var (from, into) in Set)
        {
            var s = Species(into);
            Assert.Equal(2000 + Species(from).DexNumber, s.DexNumber);
            Assert.Equal((Species(from).PrimaryType, Species(from).SecondaryType), (s.PrimaryType, s.SecondaryType));
            Assert.Equal(new[] { "Beast Boost" }, s.Abilities);
            Assert.Null(s.HiddenAbility);
            Assert.Equal(660, Total(s));
            Assert.Equal(from == "Naganadel" ? 120 : 90, Total(s) - Total(Species(from)));
            Assert.True(s.Height > Species(from).Height, into);
            // Celestrata keeps Celesteela's 999.9 kg, the data's heaviest (question 4); every other is heavier
            if (into == "Celestrata") Assert.Equal(Species(from).Weight, s.Weight);
            else Assert.True(s.Weight > Species(from).Weight, into);
        }
    }

    [Fact]
    public void TheyAreALegendarySetByTheRow()
    {
        // Question 2: the data marks no Ultra Beast legendary; the row does for ours. Caught at 3, banned from the
        // Battle Frontier as every legendary's evolution, while the forebears stay as the data has them
        foreach (var (from, into) in Set)
        {
            Assert.False(Species(from).Legendary);
            Assert.True(Species(into).Legendary);
            Assert.False(Species(into).Mythical);
            Assert.Equal(3, Species(into).CatchRate);
            Assert.False(FrontierRules.MayEnter(Mon(into, 225)));
            Assert.True(FrontierRules.MayEnter(Mon(from, 225)));
        }

        // A row without the field takes the forebear's, as every batch before
        Assert.False(Species("Arborsage").Legendary);
        Assert.True(Species("Kokonui").Legendary);
    }

    [Fact]
    public void EachGoesFurtherAt225WithAnyOtherUltraBeast()
    {
        // Question 1: any of the eleven but itself; questions 3 and 5: Naganadel too, and Poipole counts, at 225
        foreach (var (from, into) in Set)
        {
            var p = Mon(from, 225);
            Assert.Null(LevelUp(p, new EvolutionContext()));
            Assert.Null(LevelUp(p, new EvolutionContext { Party = PartyOf(p, Mon(from, 225), Mon("Tapu Koko", 60)) }));
            foreach (var other in UltraBeasts.Where(u => u != from))
                Assert.Equal(into, LevelUp(p, new EvolutionContext { Party = PartyOf(p, Mon(other, 5)) }));
            Assert.Null(LevelUp(Mon(from, 224), new EvolutionContext { Party = PartyOf(Mon("Poipole", 5)) }));

            var found = Evolution.Find(p, EvolutionTrigger.LevelUp, new EvolutionContext { Party = PartyOf(p, Mon("Poipole", 5)) })!;
            Evolution.Evolve(p, found, new EvolutionContext());
            Assert.Equal(into, p.Species.Name);
        }
    }

    [Fact]
    public void ARowMayNameSeveralSpeciesAndKeepsThemThroughTheData()
    {
        // The engine's side, on a row of its own: any one of the list is enough, beside the one species of Species
        var row = new EvolutionData
        {
            Method = EvolutionMethod.LevelWithSpeciesInParty, TargetSpecies = "Nihiliad", Level = 225,
            AnyOfSpecies = new List<string> { "Buzzwole", "Poipole" }
        };
        var read = JsonSerializer.Deserialize<EvolutionData>(JsonSerializer.Serialize(row, GameDataFiles.Json), GameDataFiles.Json)!;
        Assert.Equal(row.AnyOfSpecies, read.AnyOfSpecies);
        Assert.Null(read.Species);
        Assert.DoesNotContain("anyOfSpecies", JsonSerializer.Serialize(new EvolutionData { TargetSpecies = "X" }, GameDataFiles.Json));

        // In species.json: each forebear's last row names the other ten of the eleven, and no one species
        foreach (var (from, into) in Set)
        {
            var last = Species(from).Evolutions!.Last();
            Assert.Equal((into, EvolutionMethod.LevelWithSpeciesInParty, 225, (string?)null), (last.TargetSpecies, last.Method, last.Level, last.Species));
            Assert.Equal(UltraBeasts.Where(u => u != from).OrderBy(u => u), last.AnyOfSpecies!.OrderBy(u => u));
        }

        // A row of one species is as it was: Mantine with an Octillery, not with anything else
        Assert.Equal("Raysail", LevelUp(Mon("Mantine", 112), new EvolutionContext { Party = PartyOf(Mon("Octillery", 5)) }));
        Assert.Null(LevelUp(Mon("Mantine", 112), new EvolutionContext { Party = PartyOf(Mon("Poipole", 5)) }));
    }

    [Fact]
    public void AnUltraBeastOfOursIsSavedAndFoughtAsAnyOther()
    {
        foreach (var name in new[] { "Celestrata", "Naganapex", "Origamurai" })
        {
            var p = new Pokemon(Species(name), 230, new System.Random(5));
            var saved = SavedPokemonData.FromPokemon(p);
            var read = JsonSerializer.Deserialize<SavedPokemonData>(JsonSerializer.Serialize(saved, GameDataFiles.Json), GameDataFiles.Json)!.ToPokemon();
            Assert.Equal((name, 230, p.Personality), (read.Species.Name, read.Level, read.Personality));
            Assert.Contains(Species(name).Learnset, l => l.Level > 100);
        }

        var battle = Battle(Mon("Devourmaw", 225, "Crunch"), Mon("Gumshoos", 50), Steady(), Ruleset.Modern);
        for (int turn = 0; turn < 10 && !battle.IsBattleOver; turn++) Turn(battle);
        Assert.True(battle.IsBattleOver);
    }
}
