using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Story;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 40 · GE1: the whole National Pokédex held to a way somewhere in the game (<see cref="SpeciesSources.Everywhere"/>,
/// every region's ways together, with Sinnoh's Pal Park, whose Catching Show draws from what Sinnoh has no other way to):
/// every species of Generations 1 to 5 but what <see cref="Owed"/> names, and every species of a later generation owed by
/// its region's plan (<see cref="OwnerOf"/>) until that plan's last session runs <see cref="SpeciesSources.Of(string)"/>
/// for it. <c>docs/mechanics/species-owed.md</c> is the report of what every region still owes, written from the checks.
/// </summary>
public class NationalSpeciesTests
{
    /// <summary>The highest National number of Generations 1 to 5 (Genesect).</summary>
    public const int HighestOfTheFifth = 649;

    /// <summary>What Generations 1 to 5 have that nothing anywhere in the game gives yet, and who owes it.</summary>
    public static readonly Dictionary<string, string> Owed = new()
    {
        // (Plan 33 · GL4's Galar Mine gave Drilbur and Excadrill; plan 40 · GE3 gave Nacrene's fossils, Reshiram, Tornadus and Thundurus; Kalos's water, plan 23, Alomomola)
    };

    /// <summary>The plan that owes a species of a later generation its ways, by its National number.</summary>
    public static string? OwnerOf(PokemonSpecies species) => species.DexNumber switch
    {
        <= HighestOfTheFifth => null,
        <= 721 => "plan 23: Kalos",
        <= 809 => "plan 23: Alola",
        <= 898 => "plan 33: Galar",
        <= 905 => "plan 23: Hisui",
        _ => "plan 34: Paldea"
    };

    private static bool HadSomewhere(string species) =>
        SpeciesSources.Everywhere.Has(species) || SpeciesSources.Sinnoh.PalParkPool.Contains(species);

    [Fact]
    public void EverySpeciesOfTheFirstFiveGenerationsIsHadSomewhereButWhatIsOwed()
    {
        var dex = PokemonDatabase.GetAll().Where(s => s.DexNumber is >= 1 and <= HighestOfTheFifth)
            .OrderBy(s => s.DexNumber).Select(s => s.Name).ToList();
        Assert.Equal(HighestOfTheFifth, dex.Count);
        var missing = dex.Where(s => !HadSomewhere(s)).ToList();
        var lost = missing.Where(s => !Owed.ContainsKey(s)).ToList();
        var given = Owed.Keys.Where(HadSomewhere).Select(s => $"{s} ({SpeciesSources.Everywhere.Ways.GetValueOrDefault(s) ?? "Pal Park"})").ToList();
        Assert.True(lost.Count == 0 && given.Count == 0,
            $"Nothing anywhere gives: {string.Join(", ", lost.Select(s => $"\"{s}\""))}. Now given: {string.Join(", ", given)}");
    }

    [Fact]
    public void EverySpeciesOfALaterGenerationIsHadOrOwedByItsRegionsPlan()
    {
        foreach (var s in PokemonDatabase.GetAll().Where(s => s.DexNumber > HighestOfTheFifth))
            Assert.NotNull(OwnerOf(s));
        // ... and some are had already, which each region's plan's check will count
        Assert.Contains(PokemonDatabase.GetAll(), s => s.DexNumber > HighestOfTheFifth && SpeciesSources.Everywhere.Has(s.Name));
    }

    [Fact]
    public void WhatARegionOwesAsHadInAnotherRegionIsHadThere()
    {
        foreach (var owed in new[] { KantoSpeciesTests.Owed, JohtoSpeciesTests.Owed, UnovaSpeciesTests.Owed, HoennSpeciesTests.Owed, KalosSpeciesTests.Owed })
            foreach (var (species, who) in owed.Where(o => o.Value.StartsWith("had in another region")))
                Assert.True(SpeciesSources.Everywhere.Has(species), $"{species} is owed as {who}, and nothing anywhere gives it");
    }

    [Fact]
    public void EveryRegionsWaysAreInEverywhere()
    {
        foreach (var region in new[] { SpeciesSources.Sinnoh, SpeciesSources.Kanto, SpeciesSources.Johto, SpeciesSources.Hoenn, SpeciesSources.Unova, SpeciesSources.Of(RegionDatabase.Kalos) })
            foreach (string s in region.Ways.Keys) Assert.True(SpeciesSources.Everywhere.Has(s), s);
    }

    [Fact]
    public void AWayGivenInATestsOwnWorldTakesASpeciesOffTheListAndAWayRemovedFailsIt()
    {
        var kanto = World.LoadAll().First(w => w.Index.Region == RegionDatabase.Kanto);
        var dex = SpeciesSources.DexOf(RegionDatabase.Kanto);
        var own = kanto.Index.Areas.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var starters = new[] { ("Bulbasaur", "the bag"), ("Charmander", "the bag"), ("Squirtle", "the bag") };
        // A script of the test's own that gives an owed species and the Linking Cord
        var given = ScriptLibrary.FromSources(("test_gift",
            "script Gift\n  givepokemon \"Porygon\" 5\n  give \"Linking Cord\"\n  end\n"));
        var with = SpeciesSources.Of(kanto, given, Array.Empty<MapFile>(), SpeciesSources.HighestInSinnoh,
            f => f == "test_gift", starters, noAreaTables: true);
        Assert.Contains("test_gift.Gift", with.Ways["Porygon"]);
        // (A list of the test's own, since Kanto's owes neither any more: plan 40 · GE3)
        var owed = new Dictionary<string, string>(KantoSpeciesTests.Owed) { ["Porygon"] = "a test" };
        var needs = new Dictionary<string, string> { ["Linking Cord"] = "a test" };
        Assert.Contains(RegionSpeciesCheck.Problems(with, dex, owed), p => p.StartsWith("Owed but now given") && p.Contains("Porygon"));
        Assert.Contains(RegionSpeciesCheck.NeedProblems(with, dex, needs), p => p.Contains("Linking Cord"));
        // Without it and with no professor's bag, the three are lost and the check names them
        var bare = SpeciesSources.Of(kanto, given, Array.Empty<MapFile>(), SpeciesSources.HighestInSinnoh, _ => false, null, noAreaTables: true);
        Assert.False(bare.Has("Porygon"));
        Assert.Contains(RegionSpeciesCheck.Problems(bare, dex, KantoSpeciesTests.Owed), p => p.StartsWith("Nothing gives") && p.Contains("Bulbasaur"));
    }

    // ------------------------------------------------------------------ the report

    [Fact]
    public void TheReportOfWhatEachRegionOwesIsWhatTheChecksGive()
    {
        string path = Path.Combine(Repository.Root, "docs", "mechanics", "species-owed.md");
        string text = Report();
        string? old = File.Exists(path) ? File.ReadAllText(path).Replace("\r\n", "\n") : null;
        if (old == text) return;
        if (Golden.GoldenFile.Accepting)
        {
            File.WriteAllText(path, text);
            return;
        }
        Assert.Fail($"{path} is not what the species checks give today: run with GOLDEN=accept and read the diff.");
    }

    /// <summary>The report: each region's own Pokédex, what it has, what it owes and who owes it, then the National Pokédex.</summary>
    public static string Report()
    {
        var md = new StringBuilder();
        md.Append("# Every species, region by region: what is owed\n\n");
        md.Append("Written by `NationalSpeciesTests.TheReportOfWhatEachRegionOwesIsWhatTheChecksGive` from `SpeciesSources` and the checks' ");
        md.Append("lists of what is owed (plan 40 · GE1); run `GOLDEN=accept dotnet test PokemonPlatinumTests --filter NationalSpeciesTests` to write it again. ");
        md.Append("A species is had in a region when its own game gives it there with nobody else: its open areas, rooms and scripts, its professor's three, ");
        md.Append("its own counters, and whatever evolves or hatches from those with the items and places the region gives (a trade's evolution by a Linking Cord). ");
        md.Append("A session that gives a way takes the species off its check's list, and the checks fail until it does.\n\n");

        md.Append("| Region | Pokédex | Species | Had | Owed |\n|---|---|---:|---:|---:|\n");
        var sinnohDex = SpeciesSources.DexOf(RegionDatabase.Sinnoh);
        md.Append($"| Sinnoh | `extended-sinnoh` | {sinnohDex.Count} | {sinnohDex.Count(SpeciesSources.Sinnoh.Has)} | 0 |\n");
        var checks = new (string Region, Dictionary<string, string> Owed, Dictionary<string, string> Needs)[]
        {
            (RegionDatabase.Kanto, KantoSpeciesTests.Owed, KantoSpeciesTests.OwedNeeds),
            (RegionDatabase.Johto, JohtoSpeciesTests.Owed, JohtoSpeciesTests.OwedNeeds),
            (RegionDatabase.Hoenn, HoennSpeciesTests.Owed, HoennSpeciesTests.OwedNeeds),
            (RegionDatabase.Unova, UnovaSpeciesTests.Owed, UnovaSpeciesTests.OwedNeeds),
            // Plan 23 · Z4b: Kalos, its three Pokédexes together
            (RegionDatabase.Kalos, KalosSpeciesTests.Owed, KalosSpeciesTests.OwedNeeds),
        };
        foreach (var (region, owed, _) in checks)
        {
            var dex = SpeciesSources.DexOf(region);
            md.Append($"| {region} | {string.Join(", ", RegionDatabase.Get(region)!.Dexes.Select(d => $"`{d}`"))} | {dex.Count} | {dex.Count(SpeciesSources.Of(region).Has)} | {owed.Count} |\n");
        }
        md.Append("\nSinnoh's Pokédex is had whole. What of Diamond's, Pearl's and Platinum's National Pokédex nothing else in Sinnoh gives ");
        md.Append($"({SpeciesSources.Sinnoh.PalParkPool.Count} species, `SinnohSpeciesTests.OnlyInPalPark`) comes by Pal Park's Catching Show (plan 03 · D12a).\n");

        foreach (var (region, owed, needs) in checks)
        {
            md.Append($"\n## {region}\n\n");
            foreach (var group in owed.GroupBy(o => o.Value).OrderBy(g => g.Key, StringComparer.Ordinal))
                md.Append($"- **{group.Key}**: {string.Join(", ", group.Select(o => o.Key))}\n");
            if (needs.Count > 0)
                md.Append($"\nItems and places its evolutions ask for and nothing gives: {string.Join("; ", needs.Select(n => $"{n.Key} ({n.Value})"))}.\n");
        }

        md.Append("\n## The National Pokédex\n\n");
        md.Append("Generations 1 to 5, had nowhere in the game yet:\n\n");
        foreach (var group in Owed.GroupBy(o => o.Value).OrderBy(g => g.Key, StringComparer.Ordinal))
            md.Append($"- **{group.Key}**: {string.Join(", ", group.Select(o => o.Key))}\n");
        md.Append("\nThe later generations, owed by their regions' plans until each plan's last session runs `SpeciesSources.Of` for its region:\n\n");
        md.Append("| Owed by | Species | Had somewhere | Not yet |\n|---|---:|---:|---:|\n");
        var later = PokemonDatabase.GetAll().Where(s => s.DexNumber > HighestOfTheFifth).OrderBy(s => s.DexNumber).ToList();
        foreach (var group in later.GroupBy(OwnerOf))
        {
            int had = group.Count(s => SpeciesSources.Everywhere.Has(s.Name));
            md.Append($"| {group.Key} | {group.Count()} | {had} | {group.Count() - had} |\n");
        }
        foreach (var group in later.Where(s => !SpeciesSources.Everywhere.Has(s.Name)).GroupBy(OwnerOf))
            md.Append($"\n**{group.Key}**, not yet had: {string.Join(", ", group.Select(s => s.Name))}.\n");
        return md.ToString();
    }
}
