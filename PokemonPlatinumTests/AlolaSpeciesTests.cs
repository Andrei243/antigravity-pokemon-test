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

    private const string Wild = "plan 23: Sun and Moon's wild Pokémon still to place in Alola (the water, SOS allies, Island Scan, Poni's plains and coast)";
    private const string Items = "plan 40 · GE3: Alola's evolution stones and items";
    private const string Cord = "plan 40 · GE3: a Linking Cord in Alola";
    private const string Sites = "plan 40 · GE3: Alola's places of evolution (a Moss Rock, an Ice Rock, a Magnetic Field)";
    private const string UltraSpace = "plan 23 · Z8: Ultra Space, the Ultra Beasts and Necrozma, and Cosmog after the League";
    private const string Mythicals = "plan 29 · LS11: Alola's mythicals, by events of ours";
    private const string OtherVersion = "plan 23: Moon's legendary, in a place of its own (Alola meets Sun's Solgaleo)";
    private const string Cells = "plan 23: Zygarde's cells and the Zygarde Cube";
    private const string Amber = "plan 23: the Old Amber of Sun and Moon";

    /// <summary>What the Alola Pokédex has that nothing in Alola gives yet, and who owes it. A session that gives one takes it off.</summary>
    public static readonly Dictionary<string, string> Owed = new()
    {
        // Evolved by a stone or an item no place of Alola gives yet
        ["Raichu"] = Items, ["Chansey"] = Items, ["Blissey"] = Items, ["Magnezone"] = Items, ["Arcanine"] = Items, ["Crabominable"] = Items,
        ["Jolteon"] = Items, ["Flareon"] = Items, ["Leafeon"] = Items, ["Glaceon"] = Items, ["Magmortar"] = Items, ["Probopass"] = Items,
        ["Porygon2"] = Items, ["Porygon-Z"] = Items, ["Electivire"] = Items, ["Froslass"] = Items,
        // Sun and Moon's wild Pokémon that the drawn tables (twelve to a place, five to its water) have no room for yet
        ["Bonsly"] = Wild, ["Sudowoodo"] = Wild, ["Happiny"] = Wild, ["Munchlax"] = Wild, ["Snorlax"] = Wild, ["Makuhita"] = Wild,
        ["Hariyama"] = Wild, ["Drifloon"] = Wild, ["Drifblim"] = Wild, ["Vullaby"] = Wild, ["Mandibuzz"] = Wild, ["Barboach"] = Wild,
        ["Whiscash"] = Wild, ["Luvdisc"] = Wild, ["Corsola"] = Wild, ["Mareanie"] = Wild, ["Toxapex"] = Wild, ["Shellder"] = Wild,
        ["Cloyster"] = Wild, ["Feebas"] = Wild, ["Milotic"] = Wild, ["Alomomola"] = Wild, ["Stufful"] = Wild, ["Bewear"] = Wild,
        ["Oranguru"] = Wild, ["Goomy"] = Wild, ["Sliggoo"] = Wild, ["Goodra"] = Wild, ["Castform"] = Wild, ["Wimpod"] = Wild,
        ["Golisopod"] = Wild, ["Phantump"] = Wild, ["Trevenant"] = Wild, ["Nosepass"] = Wild, ["Chinchou"] = Wild, ["Lanturn"] = Wild,
        ["Ditto"] = Wild, ["Cleffa"] = Wild, ["Clefairy"] = Wild, ["Clefable"] = Wild, ["Minior"] = Wild, ["Beldum"] = Wild,
        ["Metang"] = Wild, ["Metagross"] = Wild, ["Porygon"] = Wild, ["Turtonator"] = Wild, ["Elekid"] = Wild, ["Electabuzz"] = Wild,
        ["Klefki"] = Wild, ["Drampa"] = Wild, ["Snorunt"] = Wild, ["Glalie"] = Wild, ["Vulpix"] = Wild, ["Ninetales"] = Wild,
        ["Snubbull"] = Wild, ["Granbull"] = Wild, ["Relicanth"] = Wild, ["Dhelmise"] = Wild, ["Carvanha"] = Wild, ["Sharpedo"] = Wild,
        ["Lapras"] = Wild, ["Emolga"] = Wild, ["Scyther"] = Wild, ["Riolu"] = Wild, ["Lucario"] = Wild, ["Dratini"] = Wild,
        ["Dragonair"] = Wild, ["Dragonite"] = Wild,
        // Evolved by trading, and Alola gives no Linking Cord
        ["Slowking"] = Cord, ["Alakazam"] = Cord, ["Gengar"] = Cord, ["Machamp"] = Cord, ["Gigalith"] = Cord, ["Politoed"] = Cord,
        ["Golem"] = Cord, ["Scizor"] = Cord,
        // Zygarde's cells and cores, Sun and Moon's post-game hunt
        ["Zygarde"] = Cells,
        // The Old Amber, which Sun and Moon give and nothing here does yet
        ["Aerodactyl"] = Amber,
        // Ultra Space and what comes after the League there (plan 23 · Z8)
        ["Cosmog"] = UltraSpace, ["Cosmoem"] = UltraSpace, ["Nihilego"] = UltraSpace, ["Buzzwole"] = UltraSpace, ["Pheromosa"] = UltraSpace, ["Xurkitree"] = UltraSpace,
        ["Celesteela"] = UltraSpace, ["Kartana"] = UltraSpace, ["Guzzlord"] = UltraSpace, ["Necrozma"] = UltraSpace,
        // Moon's legendary (Alola meets Sun's Solgaleo)
        ["Lunala"] = OtherVersion,
        // Given by events alone in Sun and Moon
        ["Magearna"] = Mythicals, ["Marshadow"] = Mythicals,
    };

    /// <summary>The items and places Alola's evolutions ask for that nothing in Alola gives, and who owes each.</summary>
    public static readonly Dictionary<string, string> OwedNeeds = new()
    {
        ["Thunder Stone"] = Items,
        ["Oval Stone"] = Items,
        ["Fire Stone"] = Items,
        ["Ice Stone"] = Items,
        ["Upgrade"] = Items,
        ["Dubious Disc"] = Items,
        ["Electirizer"] = Items,
        ["Dawn Stone"] = Items,
        ["Leaf Stone"] = Items,
        ["Linking Cord"] = Cord,
        ["Magnetic Field"] = Sites,
        ["Moss Rock"] = Sites,
        ["Ice Rock"] = Sites,
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
