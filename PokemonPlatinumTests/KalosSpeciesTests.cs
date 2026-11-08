using System.Collections.Generic;
using PokemonPlatinumEngine.Data;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 40 · GE1's check, for Kalos (plan 23 · Z4b): every species of X and Y's three Pokédexes (Central, Coastal and
/// Mountain Kalos, 457 together) can be had in Kalos with nobody else (<see cref="SpeciesSources.Of(string)"/>), but what
/// <see cref="Owed"/> names, each with who owes it. Drawn by hand, Kalos has twelve species to a place's grass and no
/// water yet, so most of what is owed is X and Y's wild Pokémon still to place.
/// </summary>
public class KalosSpeciesTests
{
    private const string Region = RegionDatabase.Kalos;

    private const string Wild = "plan 23: X and Y's wild Pokémon still to place in Kalos (the water, the hordes, the rest of the grass)";
    private const string Fossils = "plan 23: Ambrette's fossil laboratory";
    private const string Mythicals = "plan 29 · LS11: Kalos's mythicals, by events of ours";

    /// <summary>What Kalos's Pokédexes have that nothing in Kalos gives yet, and who owes it. A session that gives one takes it off.</summary>
    public static readonly Dictionary<string, string> Owed = new()
    {
        // Ambrette's fossil laboratory, which plan 23 · Z3 left unbuilt
        ["Tyrunt"] = Fossils, ["Tyrantrum"] = Fossils, ["Amaura"] = Fossils, ["Aurorus"] = Fossils, ["Aerodactyl"] = Fossils,
        // Given by events alone in X and Y
        ["Diancie"] = Mythicals, ["Hoopa"] = Mythicals, ["Volcanion"] = Mythicals,
        // X and Y's wild Pokémon that no drawn table holds yet
        ["Tauros"] = Wild, ["Miltank"] = Wild, ["Mareep"] = Wild, ["Flaaffy"] = Wild, ["Ampharos"] = Wild, ["Pinsir"] = Wild,
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
        // Sinnoh's Game Corner, Underground and Battle Frontier give Kalos nothing
        Assert.DoesNotContain(kalos.Items.Values, w => w.Contains("Underground") || w.Contains("Battle Points"));
    }
}
