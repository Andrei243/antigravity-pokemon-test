using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumTests;

/// <summary>
/// The Pokédex's rules (plan 03 · D10): the Sinnoh and National Pokédexes, their counts, the upgrade after the Hall
/// of Fame, the diplomas, the search, and where each species lives.
/// </summary>
public class PokedexTests
{
    private static int Dex(string name) => PokemonDatabase.Get(name)!.DexNumber;

    [Fact]
    public void EachPokedexListsItsSpeciesInItsOwnOrder()
    {
        var sinnoh = Pokedex.Entries(PokedexMode.Sinnoh);
        Assert.Equal(210, sinnoh.Count);
        Assert.Equal(Enumerable.Range(1, 210), sinnoh.Select(e => e.Number));
        Assert.Equal(("Turtwig", 1), (sinnoh[0].Species.Name, sinnoh[0].Number));
        Assert.Equal(("Giratina", 210), (sinnoh[^1].Species.Name, sinnoh[^1].Number));

        var national = Pokedex.Entries(PokedexMode.National);
        Assert.Equal(1025, national.Count);
        Assert.All(national, e => Assert.Equal(e.Species.DexNumber, e.Number));
    }

    [Fact]
    public void EachPokedexCountsOnlyItsOwnSpecies()
    {
        var dex = new Pokedex();
        dex.RegisterCaught(Dex("Turtwig"));
        dex.RegisterSeen(Dex("Starly"));
        dex.RegisterCaught(Dex("Bulbasaur"));   // not in Platinum's Sinnoh Pokédex
        Assert.Equal((2, 1), (dex.SeenIn(PokedexMode.Sinnoh), dex.CaughtIn(PokedexMode.Sinnoh)));
        Assert.Equal((3, 2), (dex.SeenIn(PokedexMode.National), dex.CaughtIn(PokedexMode.National)));
    }

    [Fact]
    public void TheNationalPokedexOpensAfterTheHallOfFameOnceEverySinnohSpeciesIsSeen()
    {
        var dex = new Pokedex();
        var story = new StoryProgress();
        Assert.Equal(new[] { PokedexMode.Sinnoh }, dex.Modes);
        Assert.False(dex.CanUnlockNational(story));

        // Every Sinnoh species seen, but the Hall of Fame not reached
        foreach (var e in Pokedex.Entries(PokedexMode.Sinnoh)) dex.RegisterSeen(e.Species.DexNumber);
        Assert.False(dex.CanUnlockNational(story));

        // The Hall of Fame with one species unseen
        story.CompleteRegion("Sinnoh");
        dex.SeenSpecies.Remove(Dex("Giratina"));
        Assert.False(dex.CanUnlockNational(story));

        dex.RegisterSeen(Dex("Giratina"));
        Assert.True(dex.CanUnlockNational(story));
        dex.UnlockNational();
        Assert.False(dex.CanUnlockNational(story));
        Assert.Equal(new[] { PokedexMode.Sinnoh, PokedexMode.National }, dex.Modes);
    }

    [Fact]
    public void ADiplomaIsGivenOnceForACompletePokedex()
    {
        var dex = new Pokedex();
        Assert.Equal((0, 210), dex.Progress(PokedexMode.Sinnoh));
        Assert.Empty(dex.AwardDiplomas());

        // The Sinnoh Pokédex asks for every one of its species seen
        foreach (var e in Pokedex.Entries(PokedexMode.Sinnoh)) dex.RegisterSeen(e.Species.DexNumber);
        Assert.Equal(new[] { PokedexMode.Sinnoh }, dex.AwardDiplomas());
        Assert.Empty(dex.AwardDiplomas());

        // The National one asks for every species caught but the mythical ones, and only once it is unlocked
        var (needed, caught) = Pokedex.Goal(PokedexMode.National);
        Assert.True(caught);
        Assert.Equal(1025 - 23, needed.Count);
        Assert.DoesNotContain(needed, s => s.Name is "Mew" or "Arceus" or "Pecharunt");
        foreach (var s in needed) dex.RegisterCaught(s.DexNumber);
        Assert.True(dex.IsComplete(PokedexMode.National));
        Assert.Empty(dex.AwardDiplomas());
        dex.UnlockNational();
        Assert.Equal(new[] { PokedexMode.National }, dex.AwardDiplomas());

        // A save brings both back
        var restored = new Pokedex();
        restored.Restore(dex.NationalUnlocked, dex.Diplomas);
        Assert.True(restored.NationalUnlocked);
        Assert.Equal(dex.Diplomas.Order(), restored.Diplomas.Order());
        restored.Clear();
        Assert.False(restored.NationalUnlocked);
        Assert.Empty(restored.Diplomas);
    }

    [Fact]
    public void SearchLooksOnlyAmongTheSpeciesSeenAndSortsAsPlatinumDoes()
    {
        var dex = new Pokedex();
        foreach (string name in new[] { "Turtwig", "Chimchar", "Piplup", "Starly", "Bidoof", "Shinx", "Onix" }) dex.RegisterCaught(Dex(name));
        dex.RegisterSeen(Dex("Staraptor"));
        dex.RegisterSeen(Dex("Bulbasaur"));

        PokedexQuery Query() => new();
        string Names(PokedexMode mode, PokedexQuery q) => string.Join(",", PokedexSearch.Run(dex, mode, q).Select(e => e.Species.Name));

        // The plain list of what has been seen, in the mode's own order
        Assert.True(Query().IsPlain);
        Assert.Equal("Turtwig,Chimchar,Piplup,Starly,Staraptor,Bidoof,Shinx,Onix", Names(PokedexMode.Sinnoh, Query()));
        Assert.Equal("Bulbasaur,Onix,Turtwig,Chimchar,Piplup,Starly,Staraptor,Bidoof,Shinx", Names(PokedexMode.National, Query()));

        // A to Z; by first letters; by types, one or both; by shape
        Assert.Equal("Bidoof,Chimchar,Onix,Piplup,Shinx,Staraptor,Starly,Turtwig", Names(PokedexMode.Sinnoh, Query() with { Order = PokedexOrder.Alphabet }));
        Assert.Equal("Turtwig,Starly,Staraptor,Shinx", Names(PokedexMode.Sinnoh, Query() with { NameGroup = 6 }));   // S T U
        Assert.Equal("Starly,Staraptor", Names(PokedexMode.Sinnoh, Query() with { Type1 = PokemonType.Flying }));
        Assert.Equal("Onix", Names(PokedexMode.Sinnoh, Query() with { Type1 = PokemonType.Rock, Type2 = PokemonType.Ground }));
        Assert.Equal("", Names(PokedexMode.Sinnoh, Query() with { Type1 = PokemonType.Rock, Type2 = PokemonType.Water }));
        Assert.Equal("Onix", Names(PokedexMode.Sinnoh, Query() with { Shape = "Squiggle" }));

        // By weight and height, only among the caught, whose size the Pokédex knows
        var heaviest = PokedexSearch.Run(dex, PokedexMode.Sinnoh, Query() with { Order = PokedexOrder.Heaviest });
        Assert.Equal(7, heaviest.Count);
        Assert.DoesNotContain(heaviest, e => e.Species.Name == "Staraptor");
        Assert.True(heaviest.Zip(heaviest.Skip(1)).All(p => p.First.Species.Weight >= p.Second.Species.Weight));
        Assert.Equal(("Onix", "Starly"), (heaviest[0].Species.Name, heaviest[^1].Species.Name));
        Assert.Equal("Starly", PokedexSearch.Run(dex, PokedexMode.Sinnoh, Query() with { Order = PokedexOrder.Lightest })[0].Species.Name);
        Assert.Equal("Onix", PokedexSearch.Run(dex, PokedexMode.Sinnoh, Query() with { Order = PokedexOrder.Tallest })[0].Species.Name);
        Assert.Equal("8.8 m", PokedexSearch.SizeLabel(PokedexOrder.Tallest, PokemonDatabase.Get("Onix")!));

        // Every shape the data has can be searched for, and names file under their first letter
        Assert.Equal(PokemonDatabase.GetAll().Select(s => s.Shape).Distinct().Order(), PokedexSearch.Shapes.Order());
        Assert.Equal('N', PokedexSearch.FirstLetter("Nidoran♀"));
        Assert.Equal('M', PokedexSearch.FirstLetter("Mr. Mime"));
        Assert.All(PokemonDatabase.GetAll(), s => Assert.Contains(PokedexSearch.NameGroups, g => g.Contains(PokedexSearch.FirstLetter(s.Name))));
    }

    [Fact]
    public void HabitatsSayWhereEachSpeciesLivesAndHowItIsMet()
    {
        var habitats = Habitats.Sinnoh;
        Assert.NotNull(habitats);
        Assert.Equal((30, 30), (habitats!.Width, habitats.Height));

        // Kricketot sings on Route 201 in the morning and at night, not by day
        var kricketot = habitats.Of("Kricketot").Single(h => h.Name == "Route 201");
        Assert.Equal(HabitatWays.Morning | HabitatWays.Night, kricketot.Ways);
        Assert.Equal(new[] { (3, 26), (4, 26) }, kricketot.Cells);

        // Lake Verity's water and rods
        var psyduck = habitats.Of("Psyduck").Single(h => h.Name == "Lake Verity");
        Assert.True(psyduck.Ways.HasFlag(HabitatWays.Surfing));
        Assert.True(habitats.Of("Magikarp").Count(h => h.Ways.HasFlag(HabitatWays.OldRod)) > 10);

        // A cave of many floors is one place, shown at each of its entrances
        var coronet = habitats.Of("Geodude").Where(h => h.Name == "Mt. Coronet").ToList();
        Assert.Single(coronet);
        Assert.True(coronet[0].Cells.Count > 1);

        // The Great Marsh is shown at Pastoria City, whose gate leads into it
        Assert.NotEmpty(habitats.Of("Wooper").Single(h => h.Name == "Great Marsh").Cells);

        // Species met only in other ways (gifts, legends in one spot) have no habitat here
        Assert.Empty(habitats.Of("Arceus"));
        Assert.Empty(habitats.Of("Turtwig"));

        // Times of day as wild Pokémon know them
        Assert.Equal(HabitatWays.Day, Habitats.GrassAt(TimeOfDay.Twilight));
        Assert.Equal(HabitatWays.Night, Habitats.GrassAt(TimeOfDay.LateNight));
        Assert.Equal(HabitatWays.Morning, Habitats.GrassAt(TimeOfDay.Morning));

        // Every place is on the map, on a chunk that has ground or water
        foreach (var species in PokemonDatabase.GetAll())
            foreach (var h in habitats.Of(species.Name))
            {
                Assert.NotEmpty(h.Cells);
                Assert.All(h.Cells, c => Assert.NotEqual(' ', habitats.LookAt(c.X, c.Y)));
            }
        Assert.Equal('T', habitats.LookAt(3, 27));   // Twinleaf Town
        Assert.Equal('~', habitats.LookAt(0, 7));    // the sea west of Canalave
    }
}
