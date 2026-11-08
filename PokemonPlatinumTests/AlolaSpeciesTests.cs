using System.Collections.Generic;
using PokemonPlatinumEngine.Data;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 40 · GE1's check, for Alola (plan 23, the wave after the loose ends): every species of Sun and Moon's Alola
/// Pokédex (PokeAPI's <c>original-alola</c>) can be had in Alola with nobody else (<see cref="SpeciesSources.Of(string)"/>),
/// but what <see cref="Owed"/> names, each with who owes it. Drawn by hand, Alola has twelve species to a place's grass
/// and five to its water, so most of what is owed is Sun and Moon's wild Pokémon still to place.
/// </summary>
public class AlolaSpeciesTests
{
    private const string Region = RegionDatabase.Alola;

    /// <summary>What the Alola Pokédex has that nothing in Alola gives yet, and who owes it. A session that gives one takes it off.</summary>
    public static readonly Dictionary<string, string> Owed = new()
    {
    };

    /// <summary>The items and places Alola's evolutions ask for that nothing in Alola gives, and who owes each.</summary>
    public static readonly Dictionary<string, string> OwedNeeds = new()
    {
    };

    [Fact]
    public void EverySpeciesOfTheAlolaPokedexCanBeHadInAlolaButWhatIsOwed()
    {
        var dex = SpeciesSources.DexOf(Region);
        Assert.Equal(302, dex.Count);
        var problems = RegionSpeciesCheck.Problems(SpeciesSources.Of(Region), dex, Owed);
        Assert.True(problems.Count == 0, string.Join("\n", problems));
    }

    [Fact]
    public void EveryItemAndPlaceAlolasEvolutionsAskForIsInAlolaButWhatIsOwed()
    {
        var problems = RegionSpeciesCheck.NeedProblems(SpeciesSources.Of(Region), SpeciesSources.DexOf(Region), OwedNeeds);
        Assert.True(problems.Count == 0, string.Join("\n", problems));
    }

    [Fact]
    public void AlolasOwnWaysAreCountedTheProfessorsThreeTheGuardiansTheFossilsAndTypeNull()
    {
        var alola = SpeciesSources.Of(Region);
        foreach (string s in new[] { "Rowlet", "Litten", "Popplio" }) Assert.Contains("professor", alola.Ways[s]);
        // The guardians in their ruins, the Fossil Restoration Center's four and Gladion's gift
        foreach (string s in new[] { "Tapu Koko", "Tapu Lele", "Tapu Bulu", "Tapu Fini", "Type: Null" }) Assert.True(alola.Ways.ContainsKey(s), s);
        // Sinnoh's Game Corner, Underground and Battle Frontier give Alola nothing
        Assert.DoesNotContain(alola.Items.Values, w => w.Contains("Underground") || w.Contains("Battle Points"));
    }
}
