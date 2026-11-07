using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Story;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 40 · GE3: the ways the regions' checks owed, played headless. The fossils of Johto's Ruins and Pewter's museum,
/// Nacrene's two fossils and its scientist, a Linking Cord in each region, FireRed's Game Corner Pokémon, Silph Co.'s
/// Upgrade and Black 2's Eevee and Zorua. Every way through each is also played by <c>StoryTests</c>' walk of every script.
/// </summary>
public class OwedWaysTests
{
    private static HeadlessScriptHost Run(string name, string place, HeadlessScriptHost host)
    {
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find(name, place)!, null);
        runner.RunToEnd();
        return host;
    }

    private static int Held(HeadlessScriptHost host, string item) => host.Bag.GetQuantity(ItemDatabase.Get(item)!);

    private static HeadlessScriptHost WithAPokemon()
    {
        var host = new HeadlessScriptHost();
        host.Party.Add(new Pokemon(PokemonDatabase.Get("Cyndaquil")!, 20));
        return host;
    }

    [Fact]
    public void JohtosFossilsAreGivenOnceAndPewtersMuseumBringsThemBack()
    {
        var host = WithAPokemon();
        // The Ruins' researcher: the Helix Fossil, then on a later visit the Dome Fossil, then nothing more
        Run("Digger", "ruins_of_alph_research_center", host);
        Assert.Equal(1, Held(host, "Helix Fossil"));
        Run("Digger", "ruins_of_alph_research_center", host);
        Assert.Equal(1, Held(host, "Dome Fossil"));
        Run("Digger", "ruins_of_alph_research_center", host);
        Assert.Equal(2, Fossils.Count(host.Bag));
        // The curator's Old Amber, once
        Run("Curator", "pewter_museum_of_science", host);
        Run("Curator", "pewter_museum_of_science", host);
        Assert.Equal(1, Held(host, "Old Amber"));

        // Yes, and the first kind the bag holds in the table's order (the Old Amber)
        host.Answers.Enqueue(0);
        host.Answers.Enqueue(0);
        Run("FossilScientist", "pewter_museum_of_science", host);
        Assert.Equal(0, Held(host, "Old Amber"));
        Assert.Equal(PokemonDatabase.Get("Aerodactyl")!.DexNumber, host.Story.Var(Fossils.RevivedVariable));
        Run("FossilScientist", "pewter_museum_of_science", host);
        Assert.Equal(1, host.Party.Count);
        // Out and in again: handed over at HeartGold's level 20
        host.Story.ClearLocal();
        Run("FossilScientist", "pewter_museum_of_science", host);
        Assert.Equal("Aerodactyl", host.Party.Members[1].Species.Name);
        Assert.Equal(20, host.Party.Members[1].Level);
        Assert.Equal(0, host.Story.Var(Fossils.RevivedVariable));
    }

    [Fact]
    public void NacrenesRangerOffersOneFossilThenTheOtherAndItsScientistBringsThemBack()
    {
        var host = WithAPokemon();
        // The Plume Fossil first, then the Cover Fossil on the next visit
        host.Answers.Enqueue(1);
        Run("Ranger", "nacrene_city_2", host);
        Assert.Equal(1, Held(host, "Plume Fossil"));
        Assert.Equal(0, Held(host, "Cover Fossil"));
        Run("Ranger", "nacrene_city_2", host);
        Assert.Equal(1, Held(host, "Cover Fossil"));
        Run("Ranger", "nacrene_city_2", host);
        Assert.Equal(2, Fossils.Count(host.Bag));

        // Yes, and the first of the two in the table's order (the Cover Fossil)
        host.Answers.Enqueue(0);
        host.Answers.Enqueue(0);
        Run("FossilScientist", "nacrene_city_2", host);
        Assert.Equal(PokemonDatabase.Get("Tirtouga")!.DexNumber, host.Story.Var(Fossils.RevivedVariable));
        host.Story.ClearLocal();
        Run("FossilScientist", "nacrene_city_2", host);
        Assert.Equal("Tirtouga", host.Party.Members[1].Species.Name);
        Assert.Equal(25, host.Party.Members[1].Level);
    }

    [Theory]
    [InlineData("celadon_city_pokemon_center_2f")]
    [InlineData("goldenrod_pokecenter_1f")]
    [InlineData("lilycove_city_pokemon_center_2f")]
    [InlineData("castelia_city_pokemon_center")]
    public void EachRegionsReceptionistGivesALinkingCordWhenTheBagHoldsNone(string place)
    {
        var host = WithAPokemon();
        Run("LinkingCord", place, host);
        Assert.Equal(1, Held(host, Evolution.LinkingCord));
        Run("LinkingCord", place, host);
        Assert.Equal(1, Held(host, Evolution.LinkingCord));
    }

    [Fact]
    public void TheGameCornersFirstCounterTradesCoinsForFireRedsPokemon()
    {
        var host = WithAPokemon();
        host.Bag.AddItem(ItemDatabase.Get("Coin Case")!);
        host.Pastimes.Coins.Set(10000);
        // Porygon, the fifth, for 9,999 coins at level 26
        host.Answers.Enqueue(4);
        Run("PokemonPrizes", "celadon_city_game_corner_prize_room", host);
        Assert.Equal(1, host.Pastimes.Coins.Coins);
        Assert.Equal("Porygon", host.Party.Members[1].Species.Name);
        Assert.Equal(26, host.Party.Members[1].Level);
        // Too few coins for Abra: nothing changes hands
        host.Answers.Enqueue(0);
        Run("PokemonPrizes", "celadon_city_game_corner_prize_room", host);
        Assert.Equal(2, host.Party.Count);
        Assert.Equal(1, host.Pastimes.Coins.Coins);
    }

    [Fact]
    public void SilphCosReceptionistGivesTheUpgradeOnce()
    {
        var host = WithAPokemon();
        Run("Receptionist", "saffron_silph_co_hq", host);
        Run("Receptionist", "saffron_silph_co_hq", host);
        Assert.Equal(1, Held(host, "Upgrade"));
    }

    [Fact]
    public void BlackTwosEeveeWaitsForTheInsectBadge()
    {
        // (Rood's Zorua, which waits for the Quake Badge the same way, is played by StoryTests on his own church's map)
        var host = WithAPokemon();
        host.Region = RegionDatabase.Unova;
        Run("EeveeGift", "castelia_city_4", host);
        Assert.Equal(1, host.Party.Count);
        host.Story.GiveBadge(RegionDatabase.Unova, "insect");
        host.Answers.Enqueue(0);
        Run("EeveeGift", "castelia_city_4", host);
        Assert.Equal(new[] { "Cyndaquil", "Eevee" }, host.Party.Members.Select(p => p.Species.Name).ToArray());
        Assert.Equal(10, host.Party.Members[1].Level);
        // Once
        Run("EeveeGift", "castelia_city_4", host);
        Assert.Equal(2, host.Party.Count);
    }
}
