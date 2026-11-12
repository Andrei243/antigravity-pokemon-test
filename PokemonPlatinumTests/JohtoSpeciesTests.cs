using System.Collections.Generic;
using PokemonPlatinumEngine.Data;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 40 · GE1: plan 03 · D12a's check for HeartGold's Johto. Every species of HeartGold's Johto Pokédex (PokeAPI's
/// <c>updated-johto</c>, 256) can be had in Johto, HeartGold's Kanto with it, with nobody else
/// (<see cref="SpeciesSources.Johto"/>), but what <see cref="Owed"/> names, each with who owes it.
/// </summary>
public class JohtoSpeciesTests
{
    private const string Region = RegionDatabase.Johto;

    /// <summary>What the Johto Pokédex has that nothing in Johto gives yet, and who owes it. A session that gives one takes it off.</summary>
    public static readonly Dictionary<string, string> Owed = new()
    {
        // (Plan 40 · GE3 gave SoulSilver's species in the overlays' national slots, a Linking Cord in Goldenrod's
        // Pokémon Center, Silph Co.'s Upgrade, the Bug-Catching Contest's stones and Pewter's fossils)
        // (Plan 38 · EX4 gave HeartGold's Headbutt trees: Pineco, Exeggcute, Heracross, Aipom and their evolutions)
        // A mythical HeartGold gave only by an event: had in another region (Faraway Island, plan 29 · LS11c)
        ["Mew"] = "had in another region: Hoenn's Faraway Island",
    };

    /// <summary>The items and places Johto's evolutions ask for that nothing in Johto gives, and who owes each.</summary>
    public static readonly Dictionary<string, string> OwedNeeds = new()
    {
    };

    [Fact]
    public void EverySpeciesOfTheJohtoPokedexCanBeHadInJohtoButWhatIsOwed()
    {
        var dex = SpeciesSources.DexOf(Region);
        Assert.Equal(256, dex.Count);
        var problems = RegionSpeciesCheck.Problems(SpeciesSources.Johto, dex, Owed);
        Assert.True(problems.Count == 0, string.Join("\n", problems));
    }

    [Fact]
    public void EveryItemAndPlaceJohtosEvolutionsAskForIsInJohtoButWhatIsOwed()
    {
        var problems = RegionSpeciesCheck.NeedProblems(SpeciesSources.Johto, SpeciesSources.DexOf(Region), OwedNeeds);
        Assert.True(problems.Count == 0, string.Join("\n", problems));
    }

    [Fact]
    public void JohtosOwnWaysAreCountedTheBugContestTheRadioAndTheLegendaries()
    {
        var johto = SpeciesSources.Johto;
        foreach (string s in new[] { "Chikorita", "Cyndaquil", "Totodile" }) Assert.Contains("professor", johto.Ways[s]);
        foreach (string s in new[] { "Ho-Oh", "Lugia", "Celebi", "Suicune", "Raikou", "Entei" }) Assert.True(johto.Has(s), s);
    }
}
