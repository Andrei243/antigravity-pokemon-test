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
    private const string Items = "GE3: Kalos's evolution stones and items";
    private const string Cord = "GE3: a Linking Cord in Kalos";
    private const string Sites = "GE3: Kalos's places of evolution (a Moss Rock, an Ice Rock, a Magnetic Field)";
    private const string Fossils = "plan 23: Ambrette's fossil laboratory";
    private const string Mythicals = "plan 29 · LS11: Kalos's mythicals, by events of ours";
    private const string OtherVersion = "plan 23: Y's legendary, in a place of its own (Kalos meets X's Xerneas)";

    /// <summary>What Kalos's Pokédexes have that nothing in Kalos gives yet, and who owes it. A session that gives one takes it off.</summary>
    public static readonly Dictionary<string, string> Owed = new()
    {
        // Evolved by a stone or an item no place of Kalos gives yet
        ["Simisage"] = Items, ["Simisear"] = Items, ["Simipour"] = Items, ["Raichu"] = Items, ["Gallade"] = Items,
        ["Florges"] = Items, ["Roserade"] = Items, ["Delcatty"] = Items, ["Vileplume"] = Items, ["Bellossom"] = Items,
        ["Aegislash"] = Items, ["Heliolisk"] = Items, ["Vaporeon"] = Items, ["Jolteon"] = Items, ["Flareon"] = Items,
        ["Nidoqueen"] = Items, ["Nidoking"] = Items, ["Victreebel"] = Items, ["Poliwrath"] = Items, ["Honchkrow"] = Items,
        ["Ludicolo"] = Items, ["Chandelure"] = Items, ["Weavile"] = Items, ["Wigglytuff"] = Items,
        // Evolved by trading, and Kalos gives no Linking Cord
        ["Alakazam"] = Cord, ["Aromatisse"] = Cord, ["Slurpuff"] = Cord, ["Rhyperior"] = Cord, ["Steelix"] = Cord,
        ["Machamp"] = Cord, ["Gigalith"] = Cord, ["Slowking"] = Cord, ["Golem"] = Cord, ["Escavalier"] = Cord,
        ["Accelgor"] = Cord, ["Gengar"] = Cord, ["Politoed"] = Cord, ["Conkeldurr"] = Cord, ["Scizor"] = Cord,
        // Evolved by levelling at a place no area of Kalos names yet
        ["Leafeon"] = Sites, ["Glaceon"] = Sites, ["Probopass"] = Sites, ["Magnezone"] = Sites,
        // Ambrette's fossil laboratory, which plan 23 · Z3 left unbuilt
        ["Tyrunt"] = Fossils, ["Tyrantrum"] = Fossils, ["Amaura"] = Fossils, ["Aurorus"] = Fossils, ["Aerodactyl"] = Fossils,
        // Given by events alone in X and Y
        ["Diancie"] = Mythicals, ["Hoopa"] = Mythicals, ["Volcanion"] = Mythicals,
        ["Yveltal"] = OtherVersion,
        // X and Y's wild Pokémon that the drawn tables (twelve to a place, no water) have no room for yet
        ["Surskit"] = Wild, ["Masquerain"] = Wild, ["Carvanha"] = Wild, ["Sharpedo"] = Wild, ["Psyduck"] = Wild,
        ["Golduck"] = Wild, ["Farfetch'd"] = Wild, ["Plusle"] = Wild, ["Minun"] = Wild, ["Scraggy"] = Wild,
        ["Scrafty"] = Wild, ["Venipede"] = Wild, ["Whirlipede"] = Wild, ["Scolipede"] = Wild, ["Whismur"] = Wild,
        ["Loudred"] = Wild, ["Exploud"] = Wild, ["Meditite"] = Wild, ["Medicham"] = Wild, ["Axew"] = Wild,
        ["Fraxure"] = Wild, ["Haxorus"] = Wild, ["Zangoose"] = Wild, ["Seviper"] = Wild, ["Lunatone"] = Wild,
        ["Solrock"] = Wild, ["Bagon"] = Wild, ["Shelgon"] = Wild, ["Salamence"] = Wild, ["Dwebble"] = Wild,
        ["Crustle"] = Wild, ["Tentacool"] = Wild, ["Tentacruel"] = Wild, ["Wailmer"] = Wild, ["Wailord"] = Wild,
        ["Luvdisc"] = Wild, ["Shellder"] = Wild, ["Cloyster"] = Wild, ["Qwilfish"] = Wild, ["Horsea"] = Wild,
        ["Seadra"] = Wild, ["Kingdra"] = Wild, ["Relicanth"] = Wild, ["Woobat"] = Wild, ["Swoobat"] = Wild,
        ["Cubone"] = Wild, ["Marowak"] = Wild, ["Kangaskhan"] = Wild, ["Ferroseed"] = Wild, ["Ferrothorn"] = Wild,
        ["Nidoran♀"] = Wild, ["Mime Jr."] = Wild, ["Mr. Mime"] = Wild, ["Solosis"] = Wild, ["Duosion"] = Wild,
        ["Reuniclus"] = Wild, ["Tauros"] = Wild, ["Miltank"] = Wild, ["Mareep"] = Wild, ["Flaaffy"] = Wild,
        ["Ampharos"] = Wild, ["Pinsir"] = Wild, ["Heracross"] = Wild, ["Pachirisu"] = Wild, ["Exeggcute"] = Wild,
        ["Exeggutor"] = Wild, ["Chatot"] = Wild, ["Mantyke"] = Wild, ["Mantine"] = Wild, ["Clamperl"] = Wild,
        ["Huntail"] = Wild, ["Gorebyss"] = Wild, ["Remoraid"] = Wild, ["Octillery"] = Wild, ["Corsola"] = Wild,
        ["Chinchou"] = Wild, ["Lanturn"] = Wild, ["Alomomola"] = Wild, ["Lapras"] = Wild, ["Shuckle"] = Wild,
        ["Barboach"] = Wild, ["Whiscash"] = Wild, ["Basculin"] = Wild, ["Rotom"] = Wild, ["Trubbish"] = Wild,
        ["Garbodor"] = Wild, ["Smoochum"] = Wild, ["Jynx"] = Wild, ["Dratini"] = Wild, ["Dragonair"] = Wild,
        ["Dragonite"] = Wild,
    };

    /// <summary>The items and places Kalos's evolutions ask for that nothing in Kalos gives, and who owes each.</summary>
    public static readonly Dictionary<string, string> OwedNeeds = new()
    {
        ["Leaf Stone"] = Items, ["Fire Stone"] = Items, ["Water Stone"] = Items, ["Thunder Stone"] = Items,
        ["Dawn Stone"] = Items, ["Shiny Stone"] = Items, ["Moon Stone"] = Items, ["Sun Stone"] = Items, ["Dusk Stone"] = Items,
        ["Sachet"] = Items, ["Whipped Dream"] = Items, ["Dragon Scale"] = Items, ["Protector"] = Items,
        ["Deep Sea Tooth"] = Items, ["Deep Sea Scale"] = Items, ["Razor Claw"] = Items, ["Razor Fang"] = Items,
        ["Linking Cord"] = Cord,
        ["Moss Rock"] = Sites, ["Ice Rock"] = Sites, ["Magnetic Field"] = Sites,
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
        // Sinnoh's Game Corner, Underground and Battle Frontier give Kalos nothing
        Assert.DoesNotContain(kalos.Items.Values, w => w.Contains("Underground") || w.Contains("Battle Points"));
    }
}
