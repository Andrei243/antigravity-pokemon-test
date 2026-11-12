using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 21 · H12, part c: plan 03 · D12a's check for Hoenn's own game. Every species of Emerald's Hoenn Pokédex can be had
/// in Hoenn with nobody else, from its open areas, its scripts and the professor's bag (<see cref="SpeciesSources.Hoenn"/>),
/// but those the list below owes to a later session, each with who owes it (plan 40 · GE1 gave the list its owners and
/// the shape every region's check shares, <see cref="RegionSpeciesCheck"/>); a way lost or a way given shows here.
/// </summary>
public class HoennSpeciesTests
{
    private static readonly SpeciesSources Hoenn = SpeciesSources.Hoenn;

    /// <summary>The species of the Hoenn Pokédex (Emerald's 202), in its order.</summary>
    private static string[] HoennDex => PokemonDatabase.GetAll()
        .Where(s => s.DexNumbers != null && s.DexNumbers.ContainsKey("hoenn"))
        .OrderBy(s => s.DexNumbers!["hoenn"]).Select(s => s.Name).ToArray();

    /// <summary>
    /// What the Hoenn Pokédex has that nothing in Hoenn gives yet, and who owes it (plan 21's status, H12's "Left"). A
    /// session that gives one of them a way takes it off this list.
    /// </summary>
    public static readonly Dictionary<string, string> Owed = new()
    {
        // (Plan 40 · GE3 gave Ruby's and Sapphire's species in the overlays' national slots and a Linking Cord in
        // Lilycove's Pokémon Center)
        // (Plan 38 · EX4 gave Route 119's Feebas under the day's six tiles, and Milotic with it)
        // (Jirachi, which no island of decision 5 gave, wakes in Steven's cave since plan 29 · LS11c)
    };

    /// <summary>The items and places Hoenn's evolutions ask for that nothing in Hoenn gives, and who owes each.</summary>
    public static readonly Dictionary<string, string> OwedNeeds = new()
    {
    };

    [Fact]
    public void JirachiWakesInStevensCave()
    {
        Assert.Contains("met", Hoenn.Ways["Jirachi"]);
        Assert.Contains("meteor_falls_stevens_cave", Hoenn.Ways["Jirachi"]);
    }

    [Fact]
    public void RayquazaIsBattledOnSkyPillarsTopAndNosepassComesOutOfASmashedRock()
    {
        Assert.Contains("met", Hoenn.Ways["Rayquaza"]);
        Assert.Contains("sky_pillar_top", Hoenn.Ways["Rayquaza"]);
        Assert.Contains("a rock smashed", Hoenn.Ways["Nosepass"]);
        // The professor's bag gives the three
        foreach (string s in new[] { "Treecko", "Torchic", "Mudkip" }) Assert.True(Hoenn.Has(s), s);
        // and Sinnoh's own Game Corner and Underground give Hoenn nothing
        Assert.DoesNotContain(Hoenn.Items.Values, w => w.Contains("Underground") || w.Contains("Game Corner"));
    }

    [Fact]
    public void EverySpeciesOfTheHoennPokedexCanBeHadInHoennButWhatIsOwed()
    {
        var dex = HoennDex;
        Assert.Equal(202, dex.Length);
        Assert.Equal(dex, SpeciesSources.DexOf(RegionDatabase.Hoenn));
        var problems = RegionSpeciesCheck.Problems(Hoenn, dex, Owed);
        Assert.True(problems.Count == 0, string.Join("\n", problems));
    }

    [Fact]
    public void EveryItemAndPlaceHoennsEvolutionsAskForIsInHoennButWhatIsOwed()
    {
        var problems = RegionSpeciesCheck.NeedProblems(Hoenn, HoennDex, OwedNeeds);
        Assert.True(problems.Count == 0, string.Join("\n", problems));
    }
}
