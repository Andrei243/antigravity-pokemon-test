using System.Collections.Generic;
using PokemonPlatinumEngine.Data;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 40 · GE1: plan 03 · D12a's check for Black 2's Unova. Every species of Black 2's Unova Pokédex (PokeAPI's
/// <c>updated-unova</c>, 301 with Victini) can be had in Unova with nobody else (<see cref="SpeciesSources.Unova"/>), but
/// what <see cref="Owed"/> names, each with who owes it.
/// </summary>
public class UnovaSpeciesTests
{
    private const string Region = RegionDatabase.Unova;

    /// <summary>What the Unova Pokédex has that nothing in Unova gives yet, and who owes it. A session that gives one takes it off.</summary>
    public static readonly Dictionary<string, string> Owed = new()
    {
        // Black 2's shaking grass, rippling water, dust clouds and bridges' shadows: imported, and met by nothing yet
        ["Dunsparce"] = "plan 38 · EX11: shaking grass", ["Audino"] = "plan 38 · EX11: shaking grass",
        ["Pansage"] = "plan 38 · EX11: shaking grass", ["Simisage"] = "plan 38 · EX11: shaking grass",
        ["Pansear"] = "plan 38 · EX11: shaking grass", ["Simisear"] = "plan 38 · EX11: shaking grass",
        ["Panpour"] = "plan 38 · EX11: shaking grass", ["Simipour"] = "plan 38 · EX11: shaking grass",
        ["Emolga"] = "plan 38 · EX11: shaking grass", ["Castform"] = "plan 38 · EX11: shaking grass",
        // (Steelix evolves from Onix by the Linking Cord since plan 40 · GE3)
        ["Drilbur"] = "plan 38 · EX11: dust clouds",
        ["Excadrill"] = "plan 38 · EX11: dust clouds",
        ["Ducklett"] = "plan 38 · EX11: the bridges' shadows", ["Swanna"] = "plan 38 · EX11: the bridges' shadows",
        ["Alomomola"] = "plan 38 · EX11: rippling water", ["Wailmer"] = "plan 38 · EX11: rippling water",
        ["Wailord"] = "plan 38 · EX11: rippling water", ["Lapras"] = "plan 38 · EX11: rippling water",
        // (Plan 40 · GE3 gave White 2's species in the overlays' national slots and its Reshiram on the Dragonspiral
        // Tower, the Castelia patch's Buneary in Pinwheel Forest, Black 2's Eevee and Zorua, a Linking Cord in Castelia's
        // Pokémon Center, Chargestone Cave's Magnetic Field, the Moss Rock and the Ice Rock, Nacrene's fossils, and
        // Tornadus and Thundurus at the Abundant Shrine for Black 2's Dream Radar)
    };

    /// <summary>The items and places Unova's evolutions ask for that nothing in Unova gives, and who owes each.</summary>
    public static readonly Dictionary<string, string> OwedNeeds = new()
    {
    };

    [Fact]
    public void EverySpeciesOfTheUnovaPokedexCanBeHadInUnovaButWhatIsOwed()
    {
        var dex = SpeciesSources.DexOf(Region);
        Assert.Equal(301, dex.Count);
        var problems = RegionSpeciesCheck.Problems(SpeciesSources.Unova, dex, Owed);
        Assert.True(problems.Count == 0, string.Join("\n", problems));
    }

    [Fact]
    public void EveryItemAndPlaceUnovasEvolutionsAskForIsInUnovaButWhatIsOwed()
    {
        var problems = RegionSpeciesCheck.NeedProblems(SpeciesSources.Unova, SpeciesSources.DexOf(Region), OwedNeeds);
        Assert.True(problems.Count == 0, string.Join("\n", problems));
    }

    [Fact]
    public void UnovasOwnWaysAreCountedTheSeasonsTheDarkGrassAndTheGrottoes()
    {
        var unova = SpeciesSources.Unova;
        foreach (string s in new[] { "Snivy", "Tepig", "Oshawott" }) Assert.Contains("professor", unova.Ways[s]);
        Assert.Contains(unova.Ways.Values, w => w.Contains("dark grass"));
        Assert.Contains(unova.Ways.Values, w => w.Contains("Hidden Grotto"));
        // Sinnoh's Game Corner, Underground and Battle Frontier give Unova nothing
        Assert.DoesNotContain(unova.Items.Values, w => w.Contains("Underground") || w.Contains("Battle Points"));
    }
}
