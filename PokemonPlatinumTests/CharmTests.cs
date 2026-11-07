using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// The charms of a Pokédex filled (plan 40 · GE5, decisions 3 and 4; <c>docs/mechanics/rulings.md</c>, "The charms, critical
/// captures and EXP for a catch"): which completion earns which (<c>Data/charms.json</c>), read from the Pokédex itself; what
/// is due handed over by any region's designer (<c>charm due</c>, <c>charm give</c>); and what each charm does in the bag,
/// against numbers worked out by hand, nothing of it without the charm.
/// </summary>
public class CharmTests
{
    private static readonly string[] Regional = { Charms.Oval, Charms.Exp, Charms.Catching, Charms.Mark };

    [Fact]
    public void TheTableIsTheFourRegionalCharmsInTheirOrderThenTheShinyCharm()
    {
        Assert.Equal(Regional, Charms.Table.Regional);
        Assert.Equal(Charms.Shiny, Charms.Table.National);
        // Each a key item of the data, brought in as the Shiny Charm was (the importer's LaterItemsByName)
        foreach (string charm in Regional.Append(Charms.Shiny))
        {
            var item = ItemDatabase.Get(charm);
            Assert.True(item != null, charm);
            Assert.Equal(ItemPocket.KeyItems, item!.Pocket);
            Assert.False(string.IsNullOrWhiteSpace(item.Description), charm);
        }
    }

    private static void SeeAll(Pokedex pokedex, string dex)
    {
        foreach (var entry in Pokedex.Entries(dex)) pokedex.RegisterSeen(entry.Species.DexNumber);
    }

    [Fact]
    public void EachRegionalPokedexSeenWholeEarnsTheNextCharmWhicheverRegionsItIs()
    {
        var pokedex = new Pokedex();
        Assert.Empty(Charms.Earned(pokedex));
        // Sinnoh's first: the first charm
        SeeAll(pokedex, Pokedex.Sinnoh);
        Assert.Equal(new[] { Charms.Oval }, Charms.Earned(pokedex));
        // Kanto's Pokédex is the player's once Kanto is entered; seen whole, the second charm
        var kanto = RegionDatabase.Get(RegionDatabase.Kanto)!;
        SeeAll(pokedex, kanto.Dex!);
        Assert.Equal(new[] { Charms.Oval }, Charms.Earned(pokedex));
        pokedex.Enter(kanto);
        Assert.Equal(new[] { Charms.Oval, Charms.Exp }, Charms.Earned(pokedex));

        // What the bag holds has been given: only the rest is due
        var bag = new Inventory();
        Assert.Equal(new[] { Charms.Oval, Charms.Exp }, Charms.Due(pokedex, bag));
        bag.AddItem(ItemDatabase.Get(Charms.Oval)!);
        Assert.Equal(new[] { Charms.Exp }, Charms.Due(pokedex, bag));
    }

    [Fact]
    public void ADiplomaHeldCountsAsItsPokedexSeenWhole()
    {
        var pokedex = new Pokedex();
        pokedex.Restore(nationalUnlocked: false, new[] { Pokedex.Sinnoh });
        Assert.Equal(new[] { Charms.Oval }, Charms.Earned(pokedex));
    }

    [Fact]
    public void TheNationalPokedexCaughtWholeEarnsTheShinyCharm()
    {
        var pokedex = new Pokedex();
        foreach (var species in Pokedex.Goal(Pokedex.National).Species) pokedex.RegisterCaught(species.DexNumber);
        // Not the player's yet: nothing for it
        Assert.DoesNotContain(Charms.Shiny, Charms.Earned(pokedex));
        pokedex.UnlockNational();
        // Sinnoh's Pokédex lists Manaphy, which the National one's goal leaves out (a mythical): not yet seen, so no regional charm
        Assert.Equal(new[] { Charms.Shiny }, Charms.Earned(pokedex));
        // Seen whole too, the regional charm comes first in the order a designer hands them over
        SeeAll(pokedex, Pokedex.Sinnoh);
        Assert.Equal(new[] { Charms.Oval, Charms.Shiny }, Charms.Earned(pokedex));
    }

    // ------------------------------------------------------------------ the designers

    private static HeadlessScriptHost Designer(string script, string file, Pokedex? pokedex, Inventory? bag = null)
    {
        var host = new HeadlessScriptHost(bag: bag) { Pokedex = pokedex };
        var person = new NPC { Name = "Designer", Key = "designer", ScriptFile = file, DialogLines = new List<string> { "I plan the maps." } };
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find(script, file)!, person);
        runner.RunToEnd();
        Assert.Empty(host.Problems);
        return host;
    }

    private static string Said(HeadlessScriptHost host) => string.Join(" | ", host.Transcript.Select(t => t.Text));

    private static Pokedex TwoRegionsSeenWhole()
    {
        var pokedex = new Pokedex();
        var kanto = RegionDatabase.Get(RegionDatabase.Kanto)!;
        pokedex.Enter(kanto);
        SeeAll(pokedex, Pokedex.Sinnoh);
        SeeAll(pokedex, kanto.Dex!);
        return pokedex;
    }

    [Theory]
    [InlineData("Director", "GrandLakeValorLakefrontWestHouse")]
    [InlineData("Designer", "celadon_city_condominiums_3f")]
    [InlineData("Director", "celadon_condominiums_3f")]
    [InlineData("Designer", "lilycove_city_cove_lily_motel_2f")]
    public void ADesignerHandsOverEveryCharmDueAndThenNothing(string script, string file)
    {
        // Two regional Pokédexes seen whole: the Oval Charm and the Exp. Charm, one after the other, each as a gift is given
        var bag = new Inventory();
        var host = Designer(script, file, TwoRegionsSeenWhole(), bag);
        Assert.Equal(1, bag.GetQuantity(ItemDatabase.Get(Charms.Oval)!));
        Assert.Equal(1, bag.GetQuantity(ItemDatabase.Get(Charms.Exp)!));
        Assert.Equal(0, bag.GetQuantity(ItemDatabase.Get(Charms.Catching)!));
        Assert.Contains("received the Oval Charm", Said(host));
        Assert.Contains("received the Exp. Charm", Said(host));

        // Spoken to again with both in the bag, nothing is due: no charm, and the talk of another day
        var again = Designer(script, file, TwoRegionsSeenWhole(), bag);
        Assert.Equal(1, bag.GetQuantity(ItemDatabase.Get(Charms.Oval)!));
        Assert.DoesNotContain("received", Said(again));
    }

    [Fact]
    public void WithNothingDueADesignerSaysWhatHeAlwaysSaid()
    {
        var kanto = Designer("Designer", "celadon_city_condominiums_3f", new Pokedex());
        Assert.Equal("I plan the maps.", Said(kanto));
        // And a host with no Pokédex has nothing due
        var none = Designer("Designer", "lilycove_city_cove_lily_motel_2f", null);
        Assert.Equal("I plan the maps.", Said(none));
        // The game director keeps his own lines, from before plan 40 · GE5
        Assert.Contains("I'm the game director!", Said(Designer("Director", "GrandLakeValorLakefrontWestHouse", new Pokedex())));
    }

    // ------------------------------------------------------------------ what each does

    [Fact]
    public void TheOvalCharmRaisesTheCouplesChanceOfAnEgg()
    {
        // The later games' 40, 80 and 88 in place of 20, 50 and 70
        Assert.Equal((40, 80, 88, 0), (Charms.OvalChance(Compatibility.Low), Charms.OvalChance(Compatibility.Medium), Charms.OvalChance(Compatibility.High),
            Charms.OvalChance(Compatibility.None)));

        // Two Shinx of the player's get on (50). At the second's 255th step a draw of 0x8000 is 32768 × 100 / 65535 = 50:
        // under 50 makes an Egg, so none; under the Oval Charm's 80 it does
        foreach (bool charm in new[] { false, true })
        {
            var party = new Party();
            party.Add(Shinx(Gender.Female));
            party.Add(Shinx(Gender.Male));
            party.Add(new Pokemon(PokemonDatabase.Get("Bidoof")!, 20, new Random(1), Gender.Male, Nature.Hardy));
            var dayCare = new DayCare();
            dayCare.Leave(party, 0);
            dayCare.Leave(party, 0);
            Assert.Equal(Compatibility.Medium, dayCare.Compatibility);
            for (int i = 0; i < 255; i++) dayCare.Step(party, new Draws(0x8000), new DateTime(2026, 3, 10), Ruleset.Platinum, charm);
            Assert.Equal(charm, dayCare.HasEgg);
        }
    }

    private static Pokemon Shinx(Gender gender) => new(PokemonDatabase.Get("Shinx")!, 20, new Random(1), gender, Nature.Hardy);

    /// <summary>A generator whose draws are the numbers given, in order, each taken modulo what is asked.</summary>
    private sealed class Draws(params int[] values) : Random
    {
        private int next;
        public override int Next(int maxValue) => values[next++ % values.Length] % maxValue;
        public override int Next(int minValue, int maxValue) => minValue + Next(maxValue - minValue);
    }

    [Fact]
    public void TheExpCharmIsHalfAsMuchAgainRoundedDown()
    {
        Assert.Equal((21, 31, 1, 0), (Charms.ExpShare(14), Charms.ExpShare(21), Charms.ExpShare(1), Charms.ExpShare(0)));
    }

    [Fact]
    public void TheMarkCharmTriesEachMarkThriceAndNeverTakesOneAway()
    {
        var plain = new MarkMoment(TimeOfDay.Day, FieldWeather.Rain, Fished: true);
        var charmed = plain with { MarkCharm = true };
        int marked = 0, charmedMarked = 0;
        for (uint personality = 0; personality < 20000; personality++)
        {
            var mark = EncounterMarks.For(personality, plain);
            var withCharm = EncounterMarks.For(personality, charmed);
            if (mark != null)
            {
                marked++;
                // A mark found on the first try is still found: the charm's tries come after it
                Assert.NotNull(withCharm);
            }
            if (withCharm != null) charmedMarked++;
        }
        // About three times as many with the charm (each try's odds a third as long), never fewer
        Assert.True(charmedMarked > marked * 2, $"{marked} plain, {charmedMarked} with the charm");
        Assert.True(charmedMarked < marked * 4, $"{marked} plain, {charmedMarked} with the charm");
    }
}
