using System.Collections.Generic;
using PokemonPlatinumEngine.Data;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 40 · GE1's check, for Kalos (plan 23 · Z4b): every species of X and Y's three Pokédexes (Central, Coastal and
/// Mountain Kalos, 457 together) can be had in Kalos with nobody else (<see cref="SpeciesSources.Of(string)"/>), but what
/// <see cref="Owed"/> names, each with who owes it. Since Kalos's last species (plan 23: the Jaw and Sail Fossils of the
/// Glittering Cave, and Diancie, Hoopa and Volcanion each met in a place of its own after the Hall of Fame) nothing is owed.
/// </summary>
public class KalosSpeciesTests
{
    private const string Region = RegionDatabase.Kalos;

    /// <summary>What Kalos's Pokédexes have that nothing in Kalos gives yet, and who owes it. A session that gives one takes it off.</summary>
    public static readonly Dictionary<string, string> Owed = new()
    {
    };

    /// <summary>The items and places Kalos's evolutions ask for that nothing in Kalos gives, and who owes each.</summary>
    public static readonly Dictionary<string, string> OwedNeeds = new()
    {
    };

    [Fact]
    public void EverySpeciesOfKalossThreePokedexesCanBeHadInKalosButWhatIsOwed()
    {
        var dex = SpeciesSources.DexOf(Region);
        Assert.Equal(457, dex.Count);
        var problems = RegionSpeciesCheck.Problems(SpeciesSources.Of(Region), dex, Owed);
        Assert.True(problems.Count == 0, string.Join("\n", problems));
    }

    [Fact]
    public void EveryItemAndPlaceKalossEvolutionsAskForIsInKalosButWhatIsOwed()
    {
        var problems = RegionSpeciesCheck.NeedProblems(SpeciesSources.Of(Region), SpeciesSources.DexOf(Region), OwedNeeds);
        Assert.True(problems.Count == 0, string.Join("\n", problems));
    }

    [Fact]
    public void KalossOwnWaysAreCountedTheProfessorsThreeTheTablesAndTheTimeAfterTheHallOfFame()
    {
        var kalos = SpeciesSources.Of(Region);
        foreach (string s in new[] { "Chespin", "Fennekin", "Froakie" }) Assert.Contains("professor", kalos.Ways[s]);
        // Plan 23 · Z4b's: Zygarde and Mewtwo met by their scripts, Azure Bay's grass
        Assert.Contains("terminus_cave_depths", kalos.Ways["Zygarde"]);
        Assert.Contains("unknown_dungeon", kalos.Ways["Mewtwo"]);
        Assert.Contains("azure_bay", kalos.Ways["Binacle"]);
        // Kalos's last places: Yveltal in the Chamber of Emptiness, the stone shop's stones, the Battle Maison's counter
        Assert.Contains("chamber_of_emptiness", kalos.Ways["Yveltal"]);
        Assert.Contains("lumiose_stones", kalos.Items["Dusk Stone"]);
        Assert.Contains("KiloudeCity_BattleMaison", kalos.Items["Razor Claw"]);
        Assert.Contains("lost_hotel", kalos.Ways["Rotom"]);
        // Kalos's last species: the two Fossils of the Glittering Cave, revived in Ambrette, and the three mythicals
        Assert.Contains("glittering_cave", kalos.Items["Jaw Fossil"]);
        Assert.Contains("glittering_cave", kalos.Items["Sail Fossil"]);
        Assert.Contains("reflection_cave", kalos.Ways["Diancie"]);
        Assert.Contains("lost_hotel", kalos.Ways["Hoopa"]);
        Assert.Contains("couriway_town", kalos.Ways["Volcanion"]);
        // Sinnoh's Game Corner, Underground and Battle Frontier give Kalos nothing
        Assert.DoesNotContain(kalos.Items.Values, w => w.Contains("Underground") || w.Contains("Battle Points"));
    }
}
