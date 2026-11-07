using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 40 · GE1: plan 03 · D12a's check for FireRed's Kanto. Every species of FireRed's Kanto Pokédex (PokeAPI's
/// <c>kanto</c>, 151) can be had in Kanto with nobody else (<see cref="SpeciesSources.Kanto"/>), but what <see cref="Owed"/>
/// names, each with who owes it.
/// </summary>
public class KantoSpeciesTests
{
    private const string Region = RegionDatabase.Kanto;

    /// <summary>What the Kanto Pokédex has that nothing in Kanto gives yet, and who owes it. A session that gives one takes it off.</summary>
    public static readonly Dictionary<string, string> Owed = new()
    {
        // (Plan 40 · GE3 gave the rest: LeafGreen's species in the overlays' national slots, a Linking Cord in Celadon's
        // Pokémon Center and FireRed's Pokémon prizes at the Game Corner)
        // A mythical FireRed gave only by an event: had in another region (Faraway Island, plan 29 · LS11c)
        ["Mew"] = "had in another region: Hoenn's Faraway Island",
    };

    /// <summary>The items and places Kanto's evolutions ask for that nothing in Kanto gives, and who owes each.</summary>
    public static readonly Dictionary<string, string> OwedNeeds = new()
    {
    };

    [Fact]
    public void EverySpeciesOfTheKantoPokedexCanBeHadInKantoButWhatIsOwed()
    {
        var dex = SpeciesSources.DexOf(Region);
        Assert.Equal(151, dex.Count);
        var problems = RegionSpeciesCheck.Problems(SpeciesSources.Kanto, dex, Owed);
        Assert.True(problems.Count == 0, string.Join("\n", problems));
    }

    [Fact]
    public void EveryItemAndPlaceKantosEvolutionsAskForIsInKantoButWhatIsOwed()
    {
        var problems = RegionSpeciesCheck.NeedProblems(SpeciesSources.Kanto, SpeciesSources.DexOf(Region), OwedNeeds);
        Assert.True(problems.Count == 0, string.Join("\n", problems));
    }

    [Fact]
    public void TheProfessorsThreeAndKantosOwnLegendariesAreHad()
    {
        var kanto = SpeciesSources.Kanto;
        foreach (string s in new[] { "Bulbasaur", "Charmander", "Squirtle" }) Assert.Contains("professor", kanto.Ways[s]);
        foreach (string s in new[] { "Articuno", "Zapdos", "Moltres", "Mewtwo", "Snorlax", "Lapras", "Eevee" }) Assert.True(kanto.Has(s), s);
        // Sinnoh's Game Corner, Underground and Battle Frontier give Kanto nothing
        Assert.DoesNotContain(kanto.Items.Values, w => w.Contains("Underground") || w.Contains("Battle Points"));
    }
}
