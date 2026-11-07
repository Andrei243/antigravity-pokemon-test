using System.Collections.Generic;
using PokemonPlatinumEngine.Data;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 40 · GE1: plan 03 · D12a's check for HeartGold's Johto. Every species of HeartGold's Johto Pokédex (PokeAPI's
/// <c>updated-johto</c>, 256) can be had in Johto, HeartGold's Kanto with it, with nobody else
/// (<see cref="SpeciesSources.Johto"/>), but what <see cref="Owed"/> names, each with who owes it.
/// </summary>
public class JohtoSpeciesTests
{
    private const string Region = RegionDatabase.Johto;

    /// <summary>What the Johto Pokédex has that nothing in Johto gives yet, and who owes it. A session that gives one takes it off.</summary>
    public static readonly Dictionary<string, string> Owed = new()
    {
        // SoulSilver's, which HeartGold's tables leave out (PokeAPI's soulsilver encounters): placed in their own places
        ["Ledyba"] = "GE3: SoulSilver's, in its own places", ["Ledian"] = "GE3: SoulSilver's, in its own places",
        ["Vulpix"] = "GE3: SoulSilver's, in its own places", ["Ninetales"] = "GE3: SoulSilver's, in its own places",
        ["Meowth"] = "GE3: SoulSilver's, in its own places", ["Persian"] = "GE3: SoulSilver's, in its own places",
        ["Teddiursa"] = "GE3: SoulSilver's, in its own places", ["Ursaring"] = "GE3: SoulSilver's, in its own places",
        ["Delibird"] = "GE3: SoulSilver's, in its own places", ["Skarmory"] = "GE3: SoulSilver's, in its own places",
        // HeartGold's headbutt trees, not built (plan 38 · EX4)
        ["Pineco"] = "plan 38 · EX4: headbutt trees", ["Forretress"] = "plan 38 · EX4: headbutt trees",
        ["Exeggcute"] = "plan 38 · EX4: headbutt trees", ["Exeggutor"] = "plan 38 · EX4: headbutt trees",
        ["Heracross"] = "plan 38 · EX4: headbutt trees",
        ["Aipom"] = "plan 38 · EX4: headbutt trees", ["Ambipom"] = "plan 38 · EX4: headbutt trees",
        // Evolved by a stone that nothing in Johto gives (HeartGold's Bill's grandfather, the Pokéathlon's shop)
        ["Raichu"] = "GE3: a Thunder Stone in Johto", ["Jolteon"] = "GE3: a Thunder Stone in Johto",
        ["Arcanine"] = "GE3: a Fire Stone in Johto", ["Flareon"] = "GE3: a Fire Stone in Johto",
        ["Bellossom"] = "GE3: a Sun Stone in Johto", ["Sunflora"] = "GE3: a Sun Stone in Johto",
        // Evolved by trading, and Johto gives no Linking Cord
        ["Alakazam"] = "GE3: a Linking Cord in Johto", ["Machamp"] = "GE3: a Linking Cord in Johto",
        ["Golem"] = "GE3: a Linking Cord in Johto", ["Gengar"] = "GE3: a Linking Cord in Johto",
        ["Politoed"] = "GE3: a Linking Cord in Johto", ["Slowking"] = "GE3: a Linking Cord in Johto",
        ["Scizor"] = "GE3: a Linking Cord in Johto", ["Kingdra"] = "GE3: a Linking Cord in Johto",
        ["Porygon2"] = "GE3: a Linking Cord and an Upgrade in Johto",
        // Pewter's museum, which restores no fossil yet
        ["Omanyte"] = "GE3: fossils at Pewter's museum", ["Omastar"] = "GE3: fossils at Pewter's museum",
        ["Kabuto"] = "GE3: fossils at Pewter's museum", ["Kabutops"] = "GE3: fossils at Pewter's museum",
        ["Aerodactyl"] = "GE3: fossils at Pewter's museum",
        // A mythical HeartGold gave only by an event: had in another region (Faraway Island, plan 29 · LS11c)
        ["Mew"] = "had in another region: Hoenn's Faraway Island",
    };

    /// <summary>The items and places Johto's evolutions ask for that nothing in Johto gives, and who owes each.</summary>
    public static readonly Dictionary<string, string> OwedNeeds = new()
    {
        ["Linking Cord"] = "GE3: a Linking Cord in Johto",
        ["Thunder Stone"] = "GE3: a Thunder Stone in Johto",
        ["Fire Stone"] = "GE3: a Fire Stone in Johto",
        ["Sun Stone"] = "GE3: a Sun Stone in Johto",
        ["Upgrade"] = "GE3: an Upgrade in Johto",
    };

    [Fact]
    public void EverySpeciesOfTheJohtoPokedexCanBeHadInJohtoButWhatIsOwed()
    {
        var dex = SpeciesSources.DexOf(Region);
        Assert.Equal(256, dex.Count);
        var problems = RegionSpeciesCheck.Problems(SpeciesSources.Johto, dex, Owed);
        Assert.True(problems.Count == 0, string.Join("\n", problems));
    }

    [Fact]
    public void EveryItemAndPlaceJohtosEvolutionsAskForIsInJohtoButWhatIsOwed()
    {
        var problems = RegionSpeciesCheck.NeedProblems(SpeciesSources.Johto, SpeciesSources.DexOf(Region), OwedNeeds);
        Assert.True(problems.Count == 0, string.Join("\n", problems));
    }

    [Fact]
    public void JohtosOwnWaysAreCountedTheBugContestTheRadioAndTheLegendaries()
    {
        var johto = SpeciesSources.Johto;
        foreach (string s in new[] { "Chikorita", "Cyndaquil", "Totodile" }) Assert.Contains("professor", johto.Ways[s]);
        foreach (string s in new[] { "Ho-Oh", "Lugia", "Celebi", "Suicune", "Raikou", "Entei" }) Assert.True(johto.Has(s), s);
    }
}
