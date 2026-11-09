using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// Diplomas for every Pokédex and a Pokédex of our own (plan 40 · GE4; <c>docs/mechanics/rulings.md</c>, "Diplomas for every
/// Pokédex and the Pokédex of our own"): the words of every region's Pokédex, the Pokédex of the species of ours (its list, its
/// counts, its search, its diploma by decision 6's recommended rule, its place after the National one), the script word
/// <c>if diploma</c> and the built-in <c>DIPLOMAS</c>, and the five designers who read them.
/// </summary>
[Collection("Shared clocks and dice")]
public class DiplomaTests
{
    // ------------------------------------------------------------------ the words

    private static IEnumerable<string> EveryPokedex() =>
        RegionDatabase.All.SelectMany(r => r.Dexes).Append(Pokedex.National).Append(Pokedex.Ours).Distinct();

    [Fact]
    public void NoPokedexOfAnyRegionLacksItsWords()
    {
        // Every regional Pokédex of every region the data has, the National one and the Pokédex of ours
        Assert.True(EveryPokedex().Count() >= 9);
        foreach (string dex in EveryPokedex())
        {
            string key = Pokedex.KeyOf(dex);
            foreach (string path in new[] { "mode", "modeName", "diploma" })
                Assert.NotEqual($"pokedex.{path}.{key}", Text.Of($"pokedex.{path}.{key}"));
            // The line counts what the Pokédex lists, where it says so
            string line = Text.Of($"pokedex.diploma.line.{key}", ("count", 123));
            Assert.NotEqual($"pokedex.diploma.line.{key}", line);
        }
    }

    [Fact]
    public void TheWordsAreInRomanianToo()
    {
        var own = Text.Current;
        try
        {
            Text.Use("ro");
            foreach (string dex in EveryPokedex())
            {
                string key = Pokedex.KeyOf(dex);
                foreach (string path in new[] { "mode", "modeName", "diploma" })
                    Assert.NotEqual($"pokedex.{path}.{key}", Text.Of($"pokedex.{path}.{key}"));
                Assert.NotEqual($"pokedex.diploma.line.{key}", Text.Of($"pokedex.diploma.line.{key}", ("count", 123)));
            }
            Assert.NotEqual("pokedex.oursMark", Text.Of("pokedex.oursMark"));
        }
        finally { Text.Use(own); }
    }

    // ------------------------------------------------------------------ the Pokédex of our own

    private static void SeeAllOfOurs(Pokedex pokedex)
    {
        foreach (var species in PokemonDatabase.Ours()) pokedex.RegisterSeen(species.DexNumber);
    }

    [Fact]
    public void ItsEntriesAreTheSpeciesOfOursNumberedFromOneInNationalOrder()
    {
        var entries = Pokedex.Entries(Pokedex.Ours);
        Assert.Equal(PokemonDatabase.Ours().Count, entries.Count);
        Assert.True(entries.Count > 200);
        Assert.Equal(Enumerable.Range(1, entries.Count), entries.Select(e => e.Number));
        Assert.All(entries, e => Assert.True(e.Species.IsOurs));
        Assert.Equal(PokemonDatabase.Ours().OrderBy(s => s.DexNumber).Select(s => s.DexNumber), entries.Select(e => e.Species.DexNumber));
        // No region plays by it, and the games' list of regions is untouched
        Assert.Null(Pokedex.RegionOf(Pokedex.Ours));
        Assert.DoesNotContain(Pokedex.Ours, RegionDatabase.All.SelectMany(r => r.Dexes));
        // The National list still has them after Pecharunt, at their National numbers
        var national = Pokedex.Entries(Pokedex.National);
        Assert.Equal(1025 + entries.Count, national.Count);
        Assert.Equal(entries.Select(e => e.Species.DexNumber), national.Skip(1025).Select(e => e.Number));
    }

    [Fact]
    public void ItIsOfferedAfterTheNationalOneOnceNationalIsOpenAndOneOfOursHasBeenSeen()
    {
        var pokedex = new Pokedex();
        Assert.False(pokedex.OursOpen);
        pokedex.UnlockNational();
        // National open, none of ours seen: the National one is the last
        Assert.Equal(new[] { Pokedex.Sinnoh, Pokedex.National }, pokedex.Dexes);
        // One of the games' species is no help
        pokedex.RegisterSeen(PokemonDatabase.Get("Bidoof")!.DexNumber);
        Assert.Equal(new[] { Pokedex.Sinnoh, Pokedex.National }, pokedex.Dexes);
        // One of ours opens it, after the National one
        pokedex.RegisterSeen(PokemonDatabase.Ours()[0].DexNumber);
        Assert.True(pokedex.OursOpen);
        Assert.Equal(new[] { Pokedex.Sinnoh, Pokedex.National, Pokedex.Ours }, pokedex.Dexes);

        // Seen before the National one is open: it waits for it
        var early = new Pokedex();
        early.RegisterSeen(PokemonDatabase.Ours()[0].DexNumber);
        Assert.Equal(new[] { Pokedex.Sinnoh }, early.Dexes);
        early.UnlockNational();
        Assert.Equal(new[] { Pokedex.Sinnoh, Pokedex.National, Pokedex.Ours }, early.Dexes);
    }

    [Fact]
    public void ItCountsItsOwnSpeciesAndTheGamesTotalsStayTheGames()
    {
        var pokedex = new Pokedex();
        var ours = PokemonDatabase.Ours();
        for (int i = 0; i < 5; i++) pokedex.RegisterSeen(ours[i].DexNumber);
        pokedex.RegisterCaught(ours[1].DexNumber);
        pokedex.RegisterCaught(ours[2].DexNumber);
        Assert.Equal(5, pokedex.SeenIn(Pokedex.Ours));
        Assert.Equal(2, pokedex.CaughtIn(Pokedex.Ours));
        Assert.Equal((5, ours.Count), pokedex.Progress(Pokedex.Ours));
        // Ours count toward no total of the games' Pokédexes
        Assert.Equal(0, pokedex.SeenCount);
        Assert.Equal(0, pokedex.CaughtCount);
        Assert.Equal(0, pokedex.SeenIn(Pokedex.National));
    }

    [Fact]
    public void ItsGoalIsEverySpeciesOfOursSeenNotCaught()
    {
        // Decision 6, the recommended answer: most of ours are met in trainers' teams, so seen, as a regional Pokédex asks
        var (species, caught) = Pokedex.Goal(Pokedex.Ours);
        Assert.False(caught);
        Assert.Equal(PokemonDatabase.Ours().Count, species.Count);
        // The National Pokédex's goal still leaves ours out
        Assert.DoesNotContain(Pokedex.Goal(Pokedex.National).Species, s => s.IsOurs);
    }

    [Fact]
    public void ItsDiplomaIsGivenOnceAfterTheNationalOneAndKeptInASave()
    {
        var pokedex = new Pokedex();
        pokedex.UnlockNational();
        var ours = PokemonDatabase.Ours();
        for (int i = 0; i < ours.Count - 1; i++) pokedex.RegisterSeen(ours[i].DexNumber);
        Assert.Empty(pokedex.AwardDiplomas());
        Assert.False(pokedex.IsComplete(Pokedex.Ours));
        pokedex.RegisterSeen(ours[^1].DexNumber);
        Assert.True(pokedex.IsComplete(Pokedex.Ours));
        Assert.Equal(new[] { Pokedex.Ours }, pokedex.AwardDiplomas());
        Assert.Empty(pokedex.AwardDiplomas());
        Assert.True(pokedex.HoldsDiplomaOf("Ours"));

        // The National diploma, earned too, is handed out before it (the order of the Pokédexes)
        foreach (var species in Pokedex.Goal(Pokedex.National).Species) pokedex.RegisterCaught(species.DexNumber);
        Assert.Equal(new[] { Pokedex.National }, pokedex.AwardDiplomas());

        // A save keeps it under the identifier, and brings it back
        Assert.Equal(Pokedex.Ours, Pokedex.FromDiplomaKey(Pokedex.DiplomaKey(Pokedex.Ours)));
        var restored = new Pokedex();
        restored.Restore(true, pokedex.Diplomas.Select(Pokedex.DiplomaKey).Select(Pokedex.FromDiplomaKey));
        Assert.Equal(pokedex.Diplomas.Order(), restored.Diplomas.Order());
        Assert.True(restored.HoldsDiplomaOf("Ours"));
        // It counts for no charm: the regional ones are earned by a region's Pokédex and the Shiny Charm by the National one's
        Assert.DoesNotContain(Pokedex.Ours, restored.Regional);
    }

    [Fact]
    public void ItsSearchLooksAmongTheSpeciesSeenAsEveryPokedexsDoes()
    {
        var pokedex = new Pokedex();
        pokedex.UnlockNational();
        var ours = PokemonDatabase.Ours();
        Assert.Empty(PokedexSearch.Run(pokedex, Pokedex.Ours, new PokedexQuery()));
        pokedex.RegisterSeen(ours[3].DexNumber);
        pokedex.RegisterSeen(ours[10].DexNumber);
        var found = PokedexSearch.Run(pokedex, Pokedex.Ours, new PokedexQuery());
        Assert.Equal(new[] { 4, 11 }, found.Select(e => e.Number));
        // By type: only the seen species with it
        var type = ours[3].PrimaryType;
        Assert.Contains(PokedexSearch.Run(pokedex, Pokedex.Ours, new PokedexQuery(Type1: type)), e => e.Number == 4);
    }

    [Fact]
    public void ThePokedexScreenListsItsRowsAndShowsItsDiplomaOnOpening()
    {
        var pokedex = new Pokedex();
        pokedex.UnlockNational();
        SeeAllOfOurs(pokedex);
        var screen = new PokedexScreen();
        screen.Open(pokedex);
        // The diploma comes up as the Pokédex opens, as every other's does
        Assert.Equal((PokedexFocus.Diploma, Pokedex.Ours), (screen.Focus, screen.ShownDiploma));
        screen.Confirm();
        Assert.Equal(PokedexFocus.List, screen.Focus);

        // The search panel changes to it: its rows, with its own numbers, are the species of ours
        screen.OpenSearch();
        while (screen.SearchRows[screen.SearchIndex] != PokedexSearchRow.Mode) screen.Move(-1);
        for (int i = 0; i < 4 && screen.DraftMode != Pokedex.Ours; i++) screen.Sideways(1);
        Assert.Equal(Pokedex.Ours, screen.DraftMode);
        Assert.Equal("Of our own", screen.ValueOf(PokedexSearchRow.Mode));
        Assert.Contains(PokedexSearchButton.Diploma, screen.Buttons);
    }

    // ------------------------------------------------------------------ the script word and the designers

    [Fact]
    public void ADiplomaIsAskedByARegionNationalOursOrAPokedexsIdentifier()
    {
        Assert.Equal(new[] { Pokedex.Ours }, Pokedex.DiplomaDexes("ours"));
        Assert.Equal(new[] { Pokedex.National }, Pokedex.DiplomaDexes("National"));
        Assert.Equal(new[] { Pokedex.Sinnoh }, Pokedex.DiplomaDexes("Sinnoh"));
        Assert.Equal(new[] { "updated-johto" }, Pokedex.DiplomaDexes("Johto"));
        Assert.Equal(new[] { "kalos-central", "kalos-coastal", "kalos-mountain" }, Pokedex.DiplomaDexes("Kalos"));
        Assert.Equal(new[] { "kalos-coastal" }, Pokedex.DiplomaDexes("kalos-coastal"));
        Assert.Empty(Pokedex.DiplomaDexes("Atlantis"));

        // Held means every Pokédex the name gives: Kalos asks all three
        var pokedex = new Pokedex();
        pokedex.Diplomas.Add("kalos-central");
        Assert.False(pokedex.HoldsDiplomaOf("Kalos"));
        pokedex.Diplomas.Add("kalos-coastal");
        pokedex.Diplomas.Add("kalos-mountain");
        Assert.True(pokedex.HoldsDiplomaOf("Kalos"));
        Assert.False(pokedex.HoldsDiplomaOf("Atlantis"));
    }

    [Fact]
    public void TheWordIsParsedAndAWrongNameIsAnError()
    {
        var script = ScriptParser.Parse("test", "script A\n  if diploma Johto goto Held\n  if not diploma Ours goto Held\n  if var DIPLOMAS >= 2 goto Held\n  end\nlabel Held\n  end\n")[0];
        Assert.Equal(Query.Diploma, script.Code[0].Condition!.Query);
        Assert.True(script.Code[1].Condition!.Negated);
        var wrong = Assert.Throws<ScriptException>(() => ScriptParser.Parse("test", "script A\n  if diploma Atlantis goto Held\n  end\nlabel Held\n  end\n"));
        Assert.Contains("Atlantis", wrong.Message);
    }

    // A bag that holds every charm: a regional diploma held earns charms (Charms.Earned), which a designer hands over before
    // anything else, so the talk of the diplomas is heard only when none is due
    private static Inventory AllTheCharms()
    {
        var bag = new Inventory();
        foreach (string charm in new[] { Charms.Oval, Charms.Exp, Charms.Catching, Charms.Mark, Charms.Shiny }) bag.AddItem(ItemDatabase.Get(charm)!);
        return bag;
    }

    private static HeadlessScriptHost Run(string script, string file, Pokedex? pokedex, Inventory? bag = null)
    {
        var host = new HeadlessScriptHost(bag: bag ?? AllTheCharms()) { Pokedex = pokedex };
        var person = new NPC { Name = "Designer", Key = "designer", ScriptFile = file, DialogLines = new List<string> { "I plan the maps." } };
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find(script, file)!, person);
        runner.RunToEnd();
        Assert.Empty(host.Problems);
        return host;
    }

    private static string Said(HeadlessScriptHost host) => string.Join(" | ", host.Transcript.Select(t => t.Text));

    private static Pokedex Holding(params string[] diplomas)
    {
        var pokedex = new Pokedex();
        // A comma joins the Pokédexes of a region that has several (Kalos's three)
        foreach (string dex in diplomas.SelectMany(d => d.Split(',')))
        {
            pokedex.Diplomas.Add(dex);
            // A Pokédex of a region is the player's once the region is entered
            if (Pokedex.RegionOf(dex) is { } region) pokedex.Enter(region);
        }
        return pokedex;
    }

    // The six designers, each with the script and file that hold him and the region's Pokédex he reads as his own
    public static IEnumerable<object[]> Designers => new[]
    {
        new object[] { "Director", "GrandLakeValorLakefrontWestHouse", Pokedex.Sinnoh },
        new object[] { "Designer", "celadon_city_condominiums_3f", "kanto" },
        new object[] { "Director", "celadon_condominiums_3f", "updated-johto" },
        new object[] { "Designer", "lilycove_city_cove_lily_motel_2f", "hoenn" },
        new object[] { "Designer", "castelia_city_pokemon_center", "updated-unova" },
        new object[] { "Designer", "LumioseCity_HotelRichissime", "kalos-central,kalos-coastal,kalos-mountain" },
    };

    [Theory]
    [MemberData(nameof(Designers))]
    public void ADesignerReadsTheDiplomasInHisOwnLines(string script, string file, string own)
    {
        var none = Said(Run(script, file, Holding()));
        // With no diploma he says the line he said before
        Assert.True(none.Length > 0);

        // One diploma that isn't his region's and isn't ours: the line for any diploma
        string other = own == "kanto" ? Pokedex.Sinnoh : "kanto";
        var any = Said(Run(script, file, Holding(other)));
        Assert.NotEqual(none, any);

        // His own region's: a line of its own
        var mine = Said(Run(script, file, Holding(own)));
        Assert.NotEqual(any, mine);
        Assert.NotEqual(none, mine);

        // Three that are not his own region's: the line for many (his own region's diploma comes before it)
        string[] others = new[] { Pokedex.Sinnoh, "kanto", "hoenn", "updated-johto", "updated-unova" }
            .Where(d => d != own).Take(2).Append(Pokedex.National).ToArray();
        var many = Said(Run(script, file, Holding(others)));
        Assert.NotEqual(mine, many);
        Assert.NotEqual(any, many);
        Assert.Equal(mine, Said(Run(script, file, Holding(others.Append(own).ToArray()))));

        // The Pokédex of ours' diploma beats the rest
        string[] three = others.Append(own).ToArray();
        var ours = Said(Run(script, file, Holding(three.Append(Pokedex.Ours).ToArray())));
        Assert.NotEqual(many, ours);
        Assert.NotEqual(mine, ours);
        Assert.Equal(ours, Said(Run(script, file, Holding(Pokedex.Ours))));

        // And a host with no Pokédex has no diploma to read
        Assert.Equal(none, Said(Run(script, file, null)));
    }

    [Theory]
    [MemberData(nameof(Designers))]
    public void ACharmDueIsHandedOverBeforeAnyTalkOfDiplomas(string script, string file, string own)
    {
        // A regional Pokédex seen whole earns the Oval Charm; the same Pokédex's diploma held, but the Charm not in the bag
        var pokedex = Holding(own, Pokedex.Ours);
        var bag = new Inventory();
        var host = Run(script, file, pokedex, bag);
        Assert.Equal(1, bag.GetQuantity(ItemDatabase.Get(Charms.Oval)!));
        Assert.Contains("received the Oval Charm", Said(host));
    }

    [Fact]
    public void TheDiplomasBuiltInVariableCountsThemAndAHostWithoutAPokedexHasNone()
    {
        var script = ScriptParser.Parse("test", "script Count\n  if var DIPLOMAS >= 2 goto Two\n  setvar VAR_TEST 1\n  end\nlabel Two\n  setvar VAR_TEST 2\n  end\n")[0];
        int Ran(Pokedex? pokedex)
        {
            var host = new HeadlessScriptHost { Pokedex = pokedex };
            var runner = new ScriptRunner(ScriptLibrary.Default, host);
            runner.Start(script, null);
            runner.RunToEnd();
            return host.Story.Var("VAR_TEST");
        }
        Assert.Equal(1, Ran(null));
        Assert.Equal(1, Ran(Holding(Pokedex.Sinnoh)));
        Assert.Equal(2, Ran(Holding(Pokedex.Sinnoh, "kanto")));
    }

    [Fact]
    public void UnovasDesignerStandsInCastelias()
    {
        var unova = World.LoadAll().Single(w => w.Index.Region == "Unova");
        var overlay = unova.Overlay("castelia_city_pokemon_center");
        Assert.NotNull(overlay);
        var designer = Assert.Single(overlay!.Npcs ?? new(), n => n.Script == "Designer");
        Assert.Equal("designer", designer.Id);
        Assert.NotNull(ScriptLibrary.Default.Find("Designer", "castelia_city_pokemon_center"));
    }
}
