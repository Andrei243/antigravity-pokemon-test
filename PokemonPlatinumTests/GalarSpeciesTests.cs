using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 40 · GE1's check, for Galar (plan 33 · GL1, its first part): every species of Sword and Shield's Galar Pokédex
/// (PokeAPI's <c>galar</c>) can be had in Galar with nobody else (<see cref="SpeciesSources.Of(string)"/>, which reads an
/// open region's zones, rooms and scripts), but what <see cref="Owed"/> names, each with who owes it. GL1 builds only
/// Postwick to Route 2, so most of the Pokédex is owed to the sessions after it; each takes its species off as it places
/// them.
/// </summary>
public class GalarSpeciesTests
{
    private const string Region = RegionDatabase.Galar;

    private const string Places = "plan 33 · GL2 to GL7: Galar's places after Route 2, the Wild Area to Wyndon and the post-game";
    private const string Legend = "plan 33 · GL7: the Slumbering Weald's legendary of each version";
    private const string Darkest = "plan 33 · GL6: Eternatus on the Darkest Day";

    private static Dictionary<string, string> Owe(string who, params string[] species) => species.ToDictionary(s => s, _ => who);

    /// <summary>What the Galar Pokédex has that nothing in Galar gives yet, and who owes it. A session that gives one takes it off.</summary>
    public static readonly Dictionary<string, string> Owed = Owe(Places,
        "Vikavolt", "Pidove", "Tranquill", "Unfezant", "Obstagoon",
        "Shiftry", "Minccino", "Cinccino", "Bounsweet", "Steenee", "Tsareena",
        "Oddish", "Gloom", "Vileplume", "Bellossom", "Budew", "Roselia", "Roserade",
        "Electrike", "Manectric", "Growlithe",
        "Arcanine", "Swinub", "Piloswine", "Mamoswine",
        "Froslass", "Mudbray", "Mudsdale", "Dwebble",
        "Crustle", "Golett", "Golurk", "Munna", "Musharna", "Natu", "Xatu", "Stufful",
        "Bewear", "Snover", "Abomasnow", "Krabby", "Kingler", "Corphish",
        "Crawdaunt", "Nincada", "Ninjask", "Shedinja",
        "Combee", "Vespiquen", "Bronzor",
        "Bronzong", "Drifloon", "Drifblim",
        "Cherubi", "Cherrim", "Stunky", "Skuntank",
        "Machop", "Machoke", "Machamp",
        "Magikarp", "Gyarados", "Goldeen", "Seaking", "Remoraid", "Octillery", "Shellder",
        "Cloyster", "Feebas", "Milotic", "Basculin", "Wishiwashi", "Pyukumuku", "Trubbish", "Garbodor",
        "Drilbur", "Excadrill",
        "Gigalith", "Timburr", "Gurdurr", "Conkeldurr", "Woobat",
        "Swoobat", "Noibat", "Noivern", "Arrokuda", "Barraskewda", "Meowth",
        "Perrserker", "Persian", "Milcery", "Alcremie", "Cutiefly", "Ribombee", "Ferroseed", "Ferrothorn",
        "Pumpkaboo", "Gourgeist", "Raichu", "Eevee", "Vaporeon", "Jolteon",
        "Flareon", "Espeon", "Umbreon", "Leafeon", "Glaceon", "Sylveon", "Applin", "Flapple",
        "Appletun", "Espurr", "Meowstic", "Swirlix", "Slurpuff", "Spritzee", "Aromatisse", "Dewpider",
        "Araquanid", "Wynaut", "Wobbuffet", "Farfetch'd", "Sirfetch'd", "Chinchou", "Lanturn", "Croagunk",
        "Toxicroak", "Scraggy", "Scrafty", "Stunfisk", "Shuckle", "Barboach", "Whiscash", "Shellos",
        "Gastrodon", "Wimpod", "Golisopod", "Binacle", "Barbaracle", "Corsola", "Cursola",
        "Salandit", "Salazzle", "Pawniard",
        "Bisharp", "Throh", "Sawk", "Koffing", "Weezing", "Bonsly", "Sudowoodo", "Cleffa",
        "Clefairy", "Clefable", "Togepi", "Togetic", "Togekiss", "Munchlax", "Snorlax", "Cottonee",
        "Whimsicott", "Rhyhorn", "Rhydon", "Rhyperior", "Gothita", "Gothorita", "Gothitelle", "Solosis",
        "Duosion", "Reuniclus", "Karrablast", "Escavalier", "Shelmet", "Accelgor", "Elgyem", "Beheeyem",
        "Cubchoo", "Beartic", "Rufflet", "Braviary", "Vullaby", "Mandibuzz", "Skorupi", "Drapion",
        "Litwick", "Lampent", "Chandelure", "Inkay", "Malamar", "Sneasel", "Weavile", "Sableye",
        "Mawile", "Maractus", "Sigilyph", "Riolu", "Lucario", "Torkoal", "Mimikyu", "Cufant",
        "Copperajah", "Qwilfish", "Frillish", "Jellicent", "Mareanie", "Toxapex", "Cramorant",
        "Hippopotas", "Hippowdon", "Durant", "Heatmor", "Helioptile",
        "Heliolisk", "Hawlucha", "Trapinch", "Vibrava", "Flygon",
        "Runerigus", "Honedge", "Doublade", "Aegislash", "Ponyta", "Rapidash",
        "Sinistea", "Polteageist", "Indeedee", "Trevenant", "Oranguru",
        "Passimian", "Morpeko", "Falinks", "Drampa", "Turtonator", "Togedemaru",
        "Clobbopus", "Grapploct", "Pincurchin", "Mantyke", "Mantine", "Wailmer", "Wailord", "Bergmite",
        "Avalugg", "Dhelmise", "Lapras", "Lunatone", "Solrock", "Mime Jr.", "Mr. Mime", "Mr. Rime",
        "Darumaka", "Darmanitan", "Eiscue", "Duraludon", "Rotom", "Ditto", "Dracozolt",
        "Arctozolt", "Dracovish", "Arctovish", "Charmander", "Charmeleon", "Charizard", "Type: Null", "Silvally",
        "Larvitar", "Pupitar", "Tyranitar", "Deino", "Zweilous", "Hydreigon", "Goomy", "Sliggoo",
        "Goodra", "Jangmo-o", "Hakamo-o", "Kommo-o", "Dreepy", "Drakloak", "Dragapult")
        .Concat(Owe(Legend, "Zacian", "Zamazenta"))
        .Concat(Owe(Darkest, "Eternatus"))
        .ToDictionary(p => p.Key, p => p.Value);

    /// <summary>The items and places Galar's evolutions ask for that nothing in Galar gives, and who owes each.</summary>
    public static readonly Dictionary<string, string> OwedNeeds = Owe(Places,
        "Thunder Stone", "Water Stone", "Leaf Stone", "Shiny Stone", "Sun Stone", "Dawn Stone", "Moon Stone",
        "Dusk Stone", "Reaper Cloth", "Linking Cord", "Metal Coat", "Protector", "Razor Claw", "Whipped Dream", "Sachet",
        "Strawberry Sweet", "Berry Sweet", "Love Sweet", "Star Sweet", "Clover Sweet", "Flower Sweet", "Ribbon Sweet",
        "Tart Apple", "Sweet Apple", "Cracked Pot", "Moss Rock", "Ice Rock");

    [Fact]
    public void EverySpeciesOfTheGalarPokedexCanBeHadInGalarButWhatIsOwed()
    {
        var dex = SpeciesSources.DexOf(Region);
        Assert.Equal(400, dex.Count);
        var problems = RegionSpeciesCheck.Problems(SpeciesSources.Of(Region), dex, Owed);
        Assert.True(problems.Count == 0, string.Join("\n", problems));
    }

    [Fact]
    public void EveryItemAndPlaceGalarsEvolutionsAskForIsInGalarButWhatIsOwed()
    {
        var problems = RegionSpeciesCheck.NeedProblems(SpeciesSources.Of(Region), SpeciesSources.DexOf(Region), OwedNeeds);
        Assert.True(problems.Count == 0, string.Join("\n", problems));
    }

    [Fact]
    public void GalarsOwnWaysAreCountedTheThreeAndTheRoutesGrassInTheField()
    {
        var galar = SpeciesSources.Of(Region);
        foreach (string s in new[] { "Grookey", "Scorbunny", "Sobble" }) Assert.Contains("professor", galar.Ways[s]);
        foreach (string s in new[] { "Skwovet", "Rookidee", "Wooloo", "Chewtle", "Yamper" }) Assert.Contains("in the field", galar.Ways[s]);
        // A Galarian form met in the field counts for its species
        Assert.Contains("galar_route_2", galar.Ways["Zigzagoon"]);
    }
}
