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
        // LeafGreen's, which FireRed's tables leave out (PokeAPI's leafgreen encounters): placed in their own places
        ["Sandshrew"] = "GE3: LeafGreen's, in its own places", ["Sandslash"] = "GE3: LeafGreen's, in its own places",
        ["Vulpix"] = "GE3: LeafGreen's, in its own places", ["Ninetales"] = "GE3: LeafGreen's, in its own places",
        ["Bellsprout"] = "GE3: LeafGreen's, in its own places", ["Weepinbell"] = "GE3: LeafGreen's, in its own places",
        ["Victreebel"] = "GE3: LeafGreen's, in its own places", ["Slowpoke"] = "GE3: LeafGreen's, in its own places",
        ["Slowbro"] = "GE3: LeafGreen's, in its own places", ["Staryu"] = "GE3: LeafGreen's, in its own places",
        ["Starmie"] = "GE3: LeafGreen's, in its own places", ["Magmar"] = "GE3: LeafGreen's, in its own places",
        ["Pinsir"] = "GE3: LeafGreen's, in its own places",
        // FireRed's in-game trades, which plan 19 left "for plan 07's trades"
        ["Farfetch'd"] = "GE2: FireRed's own trades", ["Lickitung"] = "GE2: FireRed's own trades",
        ["Mr. Mime"] = "GE2: FireRed's own trades", ["Jynx"] = "GE2: FireRed's own trades",
        // Evolved by trading, and Kanto gives no Linking Cord
        ["Alakazam"] = "GE3: a Linking Cord in Kanto", ["Machamp"] = "GE3: a Linking Cord in Kanto",
        ["Golem"] = "GE3: a Linking Cord in Kanto", ["Gengar"] = "GE3: a Linking Cord in Kanto",
        // The Game Corner's prize Pokémon, which plan 19 · K5 left (celadon_city_game_corner_prize_room.txt)
        ["Porygon"] = "GE3: FireRed's Game Corner prizes",
        // A mythical FireRed gave only by an event: had in another region (Faraway Island, plan 29 · LS11c)
        ["Mew"] = "had in another region: Hoenn's Faraway Island",
    };

    /// <summary>The items and places Kanto's evolutions ask for that nothing in Kanto gives, and who owes each.</summary>
    public static readonly Dictionary<string, string> OwedNeeds = new()
    {
        ["Linking Cord"] = "GE3: a Linking Cord in Kanto",
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
