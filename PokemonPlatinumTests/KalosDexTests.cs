using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumTests;

/// <summary>
/// Kalos's three regional Pokédexes and their diplomas (plan 23 · Z4, the loose ends): a region file may name several
/// Pokédexes (<c>dexes</c>), entering the region gives the player all of them, each is listed, each has a diploma for
/// every species of its list seen, and the words for each are in the text table.
/// </summary>
public class KalosDexTests
{
    private static readonly string[] Three = { "kalos-central", "kalos-coastal", "kalos-mountain" };
    private static Region Kalos => RegionDatabase.Get("Kalos")!;

    [Fact]
    public void KalosFileNamesThreePokedexesAndEachIsNumberedInTheData()
    {
        Assert.Equal(Three, Kalos.Dexes);
        Assert.Equal("kalos-central", Kalos.Dex);
        foreach (string dex in Three)
        {
            var entries = Pokedex.Entries(dex);
            Assert.InRange(entries.Count, 140, 160);
            Assert.Equal(1, entries[0].Number);
            Assert.Same(Kalos, Pokedex.RegionOf(dex));
        }
        // A region with one Pokédex still has it as its only entry
        Assert.Equal(new[] { "kanto" }, RegionDatabase.Get("Kanto")!.Dexes);
        Assert.Empty(RegionDatabase.Get("Galar")!.Dexes);
    }

    [Fact]
    public void EnteringKalosGivesAllThreeAndTheNationalOneStaysClosedUntilItsRule()
    {
        var dex = new Pokedex();
        Assert.Equal(new[] { Pokedex.Sinnoh }, dex.Dexes);
        dex.Enter(Kalos);
        Assert.Equal(new[] { Pokedex.Sinnoh }.Concat(Three), dex.Dexes);
        Assert.Equal(Three, dex.Entered.Where(Three.Contains));
        // Restored from a save as it is written
        var back = new Pokedex();
        back.Restore(false, Array.Empty<string>(), dex.Entered);
        Assert.Equal(dex.Dexes, back.Dexes);
        // An old save that only entered Central still has the other two to enter by arriving in Kalos again
        var old = new Pokedex();
        old.Restore(false, Array.Empty<string>(), new[] { "kalos-central" });
        Assert.Equal(new[] { Pokedex.Sinnoh, "kalos-central" }, old.Dexes);
        old.Enter(Kalos);
        Assert.Equal(new[] { Pokedex.Sinnoh }.Concat(Three), old.Dexes);
    }

    [Fact]
    public void EachOfTheThreeHasItsOwnDiplomaForEverySpeciesSeen()
    {
        var dex = new Pokedex();
        dex.Enter(Kalos);
        Assert.Empty(dex.AwardDiplomas());
        foreach (string id in Three)
        {
            foreach (var e in Pokedex.Entries(id)) dex.RegisterSeen(e.Species.DexNumber);
            Assert.True(dex.IsComplete(id));
            Assert.Equal(new[] { id }, dex.AwardDiplomas().Where(Three.Contains));
            Assert.Contains(id, dex.Diplomas);
            Assert.Equal(id, Pokedex.FromDiplomaKey(Pokedex.DiplomaKey(id)));
        }
        Assert.Empty(dex.AwardDiplomas());
    }

    [Fact]
    public void TheTextTableHasWordsForEachPokedexAndItsDiploma()
    {
        foreach (string id in Three)
        {
            string key = Pokedex.KeyOf(id);
            foreach (string path in new[] { "mode", "modeName", "diploma" })
                Assert.NotEqual($"pokedex.{path}.{key}", Text.Of($"pokedex.{path}.{key}"));
            Assert.NotEqual($"pokedex.diploma.line.{key}", Text.Of($"pokedex.diploma.line.{key}", ("count", 150)));
        }
    }
}
