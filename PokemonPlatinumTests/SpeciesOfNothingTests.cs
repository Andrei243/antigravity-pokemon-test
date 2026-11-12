using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DataImporter;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 46 · CP1: species made from nothing, Carpathia's. The hand-written table
/// (<c>tools/DataImporter/Overrides/species-carpathia.json</c>) held whole, the importer's half on a placeholder line of
/// the tests' own (the table stays empty until the user approves a batch, CP3), and how the game counts them: outside
/// the 1,025, outside the Pokédex of our own, in a Carpathian Pokédex of their own.
/// </summary>
public class SpeciesOfNothingTests
{
    /// <summary>A placeholder line of two for the tests alone, never shipped: every value a row has, the second evolving from the first.</summary>
    private const string Placeholder = """
    {
      "Testlet": {
        "number": 1, "category": "Placeholder", "types": [ "Fire" ],
        "stats": { "hp": 45, "attack": 50, "defense": 45, "spAttack": 60, "spDefense": 50, "speed": 60 },
        "abilities": [ "Blaze" ], "hiddenAbility": null, "catchRate": 45, "growthRate": "MediumSlow",
        "eggGroups": [ "field" ], "effort": { "spAttack": 1 }, "baseExperience": 62, "height": 0.4, "weight": 6.5,
        "color": "red", "shape": "quadruped", "genderRatio": 1, "hatchCycles": 20, "friendship": 50,
        "learnset": [ { "level": 1, "moveName": "Tackle" }, { "level": 1, "moveName": "Growl" }, { "level": 7, "moveName": "Ember" } ],
        "tmMoves": [ "TM06" ], "tutorMoves": [], "eggMoves": [],
        "evolutions": [ { "method": "Level", "targetSpecies": "Testlark", "level": 16 } ]
      },
      "Testlark": {
        "number": 2, "category": "Placeholder", "types": [ "Fire", "Rock" ],
        "stats": { "hp": 70, "attack": 75, "defense": 70, "spAttack": 90, "spDefense": 75, "speed": 85 },
        "abilities": [ "Blaze" ], "hiddenAbility": null, "catchRate": 45, "growthRate": "MediumSlow",
        "eggGroups": [ "field" ], "effort": { "spAttack": 2 }, "baseExperience": 142, "height": 1.1, "weight": 38.0,
        "color": "red", "shape": "quadruped", "genderRatio": 1, "hatchCycles": 20, "friendship": 50,
        "learnset": [ { "level": 0, "moveName": "Rock Throw" }, { "level": 1, "moveName": "Tackle" } ],
        "tmMoves": [ "TM06" ], "tutorMoves": [], "eggMoves": [], "evolutions": [], "offspring": "Testlet"
      }
    }
    """;

    private static readonly string[] Moves = MoveDatabase.GetAll().Select(m => m.Name).ToArray();
    private static readonly string[] Abilities = AbilityDatabase.GetAll().Select(a => a.Name).ToArray();

    private static List<PokemonSpecies> Made(string json, out List<PokemonSpecies> all)
    {
        all = PokemonDatabase.Everyone().ToList();
        return SpeciesOfNothing.Add(all, SpeciesOfNothing.Parse(json), Moves, Abilities);
    }

    [Fact]
    public void TheShippedTableHoldsWholeRowsOfMovesAndAbilitiesThatRun()
    {
        var designs = SpeciesOfNothing.Load(SpeciesOfNothing.PathIn(Repository.Root));
        foreach (var d in designs)
        {
            Assert.True(SpeciesOfNothing.Missing(d).Count == 0, $"{d.Name} leaves out {string.Join(", ", SpeciesOfNothing.Missing(d))}");
            foreach (var move in d.Learnset!.Select(m => m.MoveName).Concat(d.TutorMoves!).Concat(d.EggMoves!))
                Assert.True(MoveDatabase.Get(move) != null && MoveDatabase.Runs(move), $"{d.Name}'s {move} is no move that runs");
            foreach (var tm in d.TmMoves!) Assert.True(ItemDatabase.Get(tm) != null, $"{d.Name}'s {tm} is no machine");
        }
        // The table is the data: whatever it holds is in species.json, in the Carpathian Pokédex's order
        Assert.Equal(designs.Select(d => d.Name).OrderBy(n => n), PokemonDatabase.Carpathia().Select(s => s.Name).OrderBy(n => n));
        Assert.True(designs.Count == 0 || designs.Min(d => d.Number) == 1, "the Carpathian Pokédex counts from 1");
    }

    [Fact]
    public void ARowIsMadeAWholeSpeciesNumberedFromTenThousandAndOne()
    {
        var made = Made(Placeholder, out var all);
        Assert.Equal(new[] { "Testlet", "Testlark" }, made.Select(s => s.Name));
        var first = made[0];
        Assert.Equal(10001, first.DexNumber);
        Assert.Equal(10002, made[1].DexNumber);
        Assert.Equal(1, first.DexNumbers[Pokedex.Carpathia]);
        Assert.Single(first.DexNumbers);
        Assert.Equal(0, first.Generation);
        Assert.True(first.IsOurs && first.IsCarpathian && !first.IsPastHundred);
        Assert.Equal(PokemonType.Fire, first.PrimaryType);
        Assert.Null(first.SecondaryType);
        Assert.Equal(PokemonType.Rock, made[1].SecondaryType);
        Assert.Equal((45, 50, 45, 60, 50, 60), (first.BaseHP, first.BaseAttack, first.BaseDefense, first.BaseSpAttack, first.BaseSpDefense, first.BaseSpeed));
        Assert.Equal(1, first.EvYield!.SpAttack);
        Assert.Equal((45, 62, GrowthRate.MediumSlow, 1, 20, 50), (first.CatchRate, first.BaseExpYield, first.GrowthRate, first.GenderRatio, first.HatchCycles, first.BaseFriendship));
        Assert.Equal((0.4f, 6.5f, "red", "quadruped"), (first.Height, first.Weight, first.Color, first.Shape));
        Assert.Equal(new[] { "Tackle", "Growl", "Ember" }, first.Learnset.Select(m => m.MoveName));
        Assert.Equal(new[] { "TM06" }, first.TmMoves);
        Assert.Null(first.TutorMoves);
        Assert.Equal("Testlark", Assert.Single(first.Evolutions!).TargetSpecies);
        Assert.Null(made[1].Evolutions);
        Assert.Equal("Testlet", made[1].Offspring);
        Assert.False(string.IsNullOrWhiteSpace(first.DexEntry));
        // After every species already there, in their numbers' order
        Assert.Equal(made, all.TakeLast(2));
        Assert.Equal(all.Count, all.Select(s => s.DexNumber).Distinct().Count());
    }

    [Fact]
    public void ARowThatLeavesAValueOutStopsTheImportNamingIt()
    {
        string noGrowth = Placeholder.Replace("\"growthRate\": \"MediumSlow\",", "").Replace("\"height\": 0.4, ", "");
        var e = Assert.Throws<InvalidDataException>(() => Made(noGrowth, out _));
        Assert.Contains("growthRate", e.Message);
        Assert.Contains("height", e.Message);
    }

    [Theory]
    [InlineData("\"moveName\": \"Ember\"", "\"moveName\": \"Emberz\"", "no move")]
    [InlineData("\"Blaze\"", "\"Blazing\"", "no ability")]
    [InlineData("\"targetSpecies\": \"Testlark\"", "\"targetSpecies\": \"Nobody\"", "no species")]
    [InlineData("\"Testlet\": {", "\"Pikachu\": {", "already")]
    [InlineData("\"number\": 2,", "\"number\": 1,", "Two species")]
    public void ARowOfThingsThatAreNotThereIsRefused(string from, string to, string said)
    {
        string json = Placeholder.Replace(from, to);
        Assert.NotEqual(Placeholder, json);
        var e = Assert.Throws<InvalidDataException>(() => Made(json, out _));
        Assert.Contains(said, e.Message);
    }

    [Fact]
    public void TheGamesCountsAndTheirLetsStayAsTheyWere()
    {
        Assert.Equal(1025, PokemonDatabase.GetAll().Count());
        Assert.All(PokemonDatabase.GetAll(), s => Assert.False(s.IsOurs));
        Assert.All(PokemonDatabase.Ours(), s => Assert.True(s.IsPastHundred && !s.IsCarpathian));
        Assert.All(PokemonDatabase.Carpathia(), s => Assert.True(s.IsCarpathian && s.IsOurs));
        Assert.Equal(PokemonDatabase.GetAll().Count() + PokemonDatabase.Ours().Count + PokemonDatabase.Carpathia().Count, PokemonDatabase.Everyone().Count());
        // The Pokédex of our own is D14's alone; the Carpathian Pokédex is Carpathia's alone, numbered from 1
        Assert.All(Pokedex.Entries(Pokedex.Ours), e => Assert.True(e.Species.IsPastHundred));
        var carpathian = Pokedex.Entries(Pokedex.Carpathia);
        Assert.Equal(PokemonDatabase.Carpathia().Count, carpathian.Count);
        Assert.All(carpathian, e => Assert.Equal(e.Species.DexNumber - PokemonDatabase.CarpathiaFrom + 1, e.Number));
        // A species made from nothing counts toward none of the National Pokédex's totals or its diploma
        var dex = new Pokedex();
        dex.RegisterCaught(PokemonDatabase.CarpathiaFrom);
        Assert.Equal(0, dex.SeenCount);
        Assert.Equal(0, dex.CaughtCount);
        dex.UnlockNational();
        Assert.False(dex.OursOpen, "a Carpathian species opens no Pokédex of our own");
        Assert.DoesNotContain(Pokedex.Goal(Pokedex.National).Species, s => s.IsCarpathian);
    }

    [Fact]
    public void TheCarpathianPokedexHasItsWords()
    {
        string key = Pokedex.KeyOf(Pokedex.Carpathia);
        foreach (var k in new[] { $"pokedex.mode.{key}", $"pokedex.modeName.{key}", $"pokedex.diploma.{key}", $"pokedex.diploma.line.{key}" })
            Assert.NotEqual(k, PokemonPlatinumEngine.Core.Text.Of(k));
    }
}
