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
        ["Steelix"] = "plan 38 · EX11: dust clouds", ["Drilbur"] = "plan 38 · EX11: dust clouds",
        ["Excadrill"] = "plan 38 · EX11: dust clouds",
        ["Ducklett"] = "plan 38 · EX11: the bridges' shadows", ["Swanna"] = "plan 38 · EX11: the bridges' shadows",
        ["Alomomola"] = "plan 38 · EX11: rippling water", ["Wailmer"] = "plan 38 · EX11: rippling water",
        ["Wailord"] = "plan 38 · EX11: rippling water", ["Lapras"] = "plan 38 · EX11: rippling water",
        // White 2's, which Black 2's tables leave out (PokeAPI's white-2 encounters): placed in their own places
        ["Elekid"] = "GE3: White 2's, in its own places", ["Electabuzz"] = "GE3: White 2's, in its own places",
        ["Skitty"] = "GE3: White 2's, in its own places", ["Delcatty"] = "GE3: White 2's, in its own places",
        ["Numel"] = "GE3: White 2's, in its own places", ["Camerupt"] = "GE3: White 2's, in its own places",
        ["Reshiram"] = "GE3: White 2's legendary, in its own place",
        // Black 2's own places, gifts and trades that the import or the scripts have not given yet
        ["Buneary"] = "GE3: Black 2's grass, which the import has no place for", ["Lopunny"] = "GE3: Black 2's grass, which the import has no place for",
        ["Eevee"] = "GE3: Black 2's gift and grass", ["Vaporeon"] = "GE3: Black 2's gift and grass",
        ["Jolteon"] = "GE3: Black 2's gift and grass", ["Flareon"] = "GE3: Black 2's gift and grass",
        ["Espeon"] = "GE3: Black 2's gift and grass", ["Umbreon"] = "GE3: Black 2's gift and grass",
        ["Leafeon"] = "GE3: Black 2's gift and grass", ["Glaceon"] = "GE3: Black 2's gift and grass",
        ["Zorua"] = "GE3: Black 2's gift", ["Zoroark"] = "GE3: Black 2's gift",
        // Evolved by trading, and Unova gives no Linking Cord
        ["Magmortar"] = "GE3: a Linking Cord in Unova", ["Electivire"] = "GE3: a Linking Cord in Unova",
        ["Conkeldurr"] = "GE3: a Linking Cord in Unova", ["Escavalier"] = "GE3: a Linking Cord in Unova",
        ["Accelgor"] = "GE3: a Linking Cord in Unova",
        // Evolved by levelling in Chargestone Cave, whose overlay names no Magnetic Field
        ["Magnezone"] = "GE3: Chargestone Cave's Magnetic Field", ["Probopass"] = "GE3: Chargestone Cave's Magnetic Field",
        // Nacrene's museum, which restores no fossil yet
        ["Tirtouga"] = "GE3: fossils at Nacrene's museum", ["Carracosta"] = "GE3: fossils at Nacrene's museum",
        ["Archen"] = "GE3: fossils at Nacrene's museum", ["Archeops"] = "GE3: fossils at Nacrene's museum",
        // Black 2 gave them only by the Dream Radar, which this game has no stand-in for (rulings, "The Abundant Shrine's Landorus")
        ["Tornadus"] = "GE3: a way of ours, for Black 2's Dream Radar", ["Thundurus"] = "GE3: a way of ours, for Black 2's Dream Radar",
    };

    /// <summary>The items and places Unova's evolutions ask for that nothing in Unova gives, and who owes each.</summary>
    public static readonly Dictionary<string, string> OwedNeeds = new()
    {
        ["Linking Cord"] = "GE3: a Linking Cord in Unova",
        ["Magnetic Field"] = "GE3: Chargestone Cave's Magnetic Field",
        ["Moss Rock"] = "GE3: Pinwheel Forest's Moss Rock",
        ["Ice Rock"] = "GE3: Twist Mountain's Ice Rock",
        ["Electirizer"] = "GE3: an Electirizer in Unova",
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
