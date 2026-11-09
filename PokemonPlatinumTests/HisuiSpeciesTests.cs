using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 40 · GE1's check, begun for Hisui (plan 23 · Z17): every species of Legends: Arceus's Hisui Pokédex (PokeAPI's
/// <c>hisui</c>, 242) can be had in Hisui with nobody else (<see cref="SpeciesSources.Of(string)"/>), but what
/// <see cref="Owed"/> names, each with who owes it. Z17 builds the village and the beach alone, so all but the
/// professor's three are owed to the open areas of Z18 to Z20; a session that gives a species adds it to <see cref="Given"/>.
/// </summary>
public class HisuiSpeciesTests
{
    private const string Region = RegionDatabase.Hisui;

    private const string OpenAreas = "plan 23 · Z18 to Z20: Hisui's open areas, their wild Pokémon seen in the field";

    /// <summary>What Hisui gives today: the professor's three on Prelude Beach (plan 23 · Z17), and what they grow into.</summary>
    private static readonly string[] Given =
    {
        "Rowlet", "Dartrix", "Decidueye", "Cyndaquil", "Quilava", "Typhlosion", "Oshawott", "Dewott", "Samurott"
    };

    /// <summary>What the Hisui Pokédex has that nothing in Hisui gives yet, and who owes it. A session that gives one adds it to <see cref="Given"/>.</summary>
    public static Dictionary<string, string> Owed =>
        SpeciesSources.DexOf(Region).Where(s => !Given.Contains(s)).ToDictionary(s => s, _ => OpenAreas);

    /// <summary>The items and places Hisui's evolutions ask for that Hisui gives today: none (plan 23 · Z17).</summary>
    private static readonly string[] NeedsGiven = { };

    /// <summary>
    /// The items and places Hisui's evolutions ask for that nothing in Hisui gives, and who owes each: every one of them
    /// (the stones, the Linking Cord, Legends: Arceus's own items and places), owed to the open areas with their wild
    /// Pokémon. A session that gives one adds it to <see cref="NeedsGiven"/>.
    /// </summary>
    public static Dictionary<string, string> OwedNeeds =>
        RegionSpeciesCheck.Needs(SpeciesSources.Of(Region), SpeciesSources.DexOf(Region))
            .Keys.Where(k => !NeedsGiven.Contains(k)).ToDictionary(k => k, _ => OpenAreas);

    [Fact]
    public void EverySpeciesOfTheHisuiPokedexCanBeHadInHisuiButWhatIsOwed()
    {
        var dex = SpeciesSources.DexOf(Region);
        Assert.Equal(242, dex.Count);
        Assert.All(Given, s => Assert.Contains(s, dex));
        var problems = RegionSpeciesCheck.Problems(SpeciesSources.Of(Region), dex, Owed);
        Assert.True(problems.Count == 0, string.Join("\n", problems));
    }

    [Fact]
    public void EveryItemAndPlaceHisuisEvolutionsAskForIsInHisuiButWhatIsOwed()
    {
        var problems = RegionSpeciesCheck.NeedProblems(SpeciesSources.Of(Region), SpeciesSources.DexOf(Region), OwedNeeds);
        Assert.True(problems.Count == 0, string.Join("\n", problems));
    }

    [Fact]
    public void HisuisOwnWaysAreCountedTheProfessorsThree()
    {
        var hisui = SpeciesSources.Of(Region);
        foreach (string s in new[] { "Rowlet", "Cyndaquil", "Oshawott" }) Assert.Contains("professor", hisui.Ways[s]);
        // Sinnoh's Game Corner, Underground and Battle Frontier give Hisui nothing
        Assert.DoesNotContain(hisui.Items.Values, w => w.Contains("Underground") || w.Contains("Battle Points"));
    }
}
