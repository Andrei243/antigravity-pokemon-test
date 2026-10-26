using System;
using System.Linq;
using PokemonPlatinumEngine.Data;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 21 · H12, part c: plan 03 · D12a's check for Hoenn's own game. Every species of Emerald's Hoenn Pokédex can be had
/// in Hoenn with nobody else, from its open areas, its scripts and the professor's bag (<see cref="SpeciesSources.Hoenn"/>),
/// but those the list below owes to a later session, each with the reason; a way lost or a way given shows here.
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
    public static readonly string[] Owed =
    {
        // Ruby's and Sapphire's, which Emerald's tables leave out (Emerald had them by trade): a dual slot or the
        // migration's, as plan 03 · D12a decided for Sinnoh
        "Meditite", "Medicham", "Roselia", "Zangoose", "Lunatone",
        // Emerald's mass outbreak, which its television announces: Hoenn has no swarms yet
        "Surskit", "Masquerain",
        // Evolved by trading, and Hoenn sells or gives no Linking Cord yet
        "Alakazam", "Golem", "Machamp", "Kingdra", "Huntail", "Gorebyss",
        // New Mauville, not built
        "Magnemite", "Magneton", "Voltorb",
        // The Devon Corporation's revival of the desert's two fossils, not built
        "Lileep", "Cradily", "Anorith", "Armaldo",
        // Route 119's Feebas tiles, not built for Hoenn
        "Feebas", "Milotic",
        // The Marine and Terra Caves after the Weather Institute's reports (plan 21 · H12)
        "Kyogre", "Groudon",
        // The event Pokémon of decision 5 (Faraway, Birth and Navel Rock's tickets) and Jirachi's
        "Jirachi", "Deoxys"
    };

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
        var missing = dex.Where(s => !Hoenn.Has(s)).ToList();
        var lost = missing.Except(Owed).ToList();
        var given = Owed.Where(Hoenn.Has).Select(s => $"{s} ({Hoenn.Ways[s]})").ToList();
        Assert.True(lost.Count == 0 && given.Count == 0,
            $"Nothing in Hoenn gives: {string.Join(", ", lost.Select(s => $"\"{s}\""))}. Now given: {string.Join(", ", given)}");
    }
}
