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
        var sinnoh = Pokedex.Entries(Pokedex.Sinnoh);
        Assert.Equal(210, sinnoh.Count);
        Assert.Equal(Enumerable.Range(1, 210), sinnoh.Select(e => e.Number));
        Assert.Equal(("Turtwig", 1), (sinnoh[0].Species.Name, sinnoh[0].Number));
        Assert.Equal(("Giratina", 210), (sinnoh[^1].Species.Name, sinnoh[^1].Number));

        var national = Pokedex.Entries(Pokedex.National);
        // The games' 1,025, then the species of our own (plan 03 · D14) by their numbers
        Assert.Equal(1025 + PokemonDatabase.Ours().Count, national.Count);
        Assert.Equal(Enumerable.Range(1, 1025), national.Take(1025).Select(e => e.Number));
        Assert.All(national, e => Assert.Equal(e.Species.DexNumber, e.Number));
    }

    [Fact]
    public void EachPokedexCountsOnlyItsOwnSpecies()
    {
        var dex = new Pokedex();
        dex.RegisterCaught(Dex("Turtwig"));
        dex.RegisterSeen(Dex("Starly"));
        dex.RegisterCaught(Dex("Bulbasaur"));   // not in Platinum's Sinnoh Pokédex
        Assert.Equal((2, 1), (dex.SeenIn(Pokedex.Sinnoh), dex.CaughtIn(Pokedex.Sinnoh)));
        Assert.Equal((3, 2), (dex.SeenIn(Pokedex.National), dex.CaughtIn(Pokedex.National)));
    }

    [Fact]
    public void TheNationalPokedexOpensAfterTheHallOfFameOnceEverySinnohSpeciesIsSeen()
    {
        var dex = new Pokedex();
        var story = new PokemonPlatinumEngine.Story.StoryState();
        Assert.Equal(new[] { Pokedex.Sinnoh }, dex.Dexes);
        Assert.False(dex.CanUnlockNational(story));

        // Every Sinnoh species seen, but the Hall of Fame not reached
        foreach (var e in Pokedex.Entries(Pokedex.Sinnoh)) dex.RegisterSeen(e.Species.DexNumber);
        Assert.False(dex.CanUnlockNational(story));

        // The Hall of Fame with one species unseen
        story.CompleteRegion("Sinnoh");
        dex.SeenSpecies.Remove(Dex("Giratina"));
        Assert.False(dex.CanUnlockNational(story));

        dex.RegisterSeen(Dex("Giratina"));
        Assert.True(dex.CanUnlockNational(story));
        dex.UnlockNational();
        Assert.False(dex.CanUnlockNational(story));
        Assert.Equal(new[] { Pokedex.Sinnoh, Pokedex.National }, dex.Dexes);
    }

    [Fact]
    public void ADiplomaIsGivenOnceForACompletePokedex()
    {
        var dex = new Pokedex();
        Assert.Equal((0, 210), dex.Progress(Pokedex.Sinnoh));
        Assert.Empty(dex.AwardDiplomas());

        // The Sinnoh Pokédex asks for every one of its species seen
        foreach (var e in Pokedex.Entries(Pokedex.Sinnoh)) dex.RegisterSeen(e.Species.DexNumber);
        Assert.Equal(new[] { Pokedex.Sinnoh }, dex.AwardDiplomas());
        Assert.Empty(dex.AwardDiplomas());

        // The National one asks for every species caught but the mythical ones, and only once it is unlocked
        var (needed, caught) = Pokedex.Goal(Pokedex.National);
        Assert.True(caught);
        Assert.Equal(1025 - 23, needed.Count);
        Assert.DoesNotContain(needed, s => s.Name is "Mew" or "Arceus" or "Pecharunt");
        foreach (var s in needed) dex.RegisterCaught(s.DexNumber);
        Assert.True(dex.IsComplete(Pokedex.National));
        Assert.Empty(dex.AwardDiplomas());
        dex.UnlockNational();
        Assert.Equal(new[] { Pokedex.National }, dex.AwardDiplomas());

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
        string Names(string mode, PokedexQuery q) => string.Join(",", PokedexSearch.Run(dex, mode, q).Select(e => e.Species.Name));

        // The plain list of what has been seen, in the mode's own order
        Assert.True(Query().IsPlain);
        Assert.Equal("Turtwig,Chimchar,Piplup,Starly,Staraptor,Bidoof,Shinx,Onix", Names(Pokedex.Sinnoh, Query()));
        Assert.Equal("Bulbasaur,Onix,Turtwig,Chimchar,Piplup,Starly,Staraptor,Bidoof,Shinx", Names(Pokedex.National, Query()));

        // A to Z; by first letters; by types, one or both; by shape
        Assert.Equal("Bidoof,Chimchar,Onix,Piplup,Shinx,Staraptor,Starly,Turtwig", Names(Pokedex.Sinnoh, Query() with { Order = PokedexOrder.Alphabet }));
        Assert.Equal("Turtwig,Starly,Staraptor,Shinx", Names(Pokedex.Sinnoh, Query() with { NameGroup = 6 }));   // S T U
        Assert.Equal("Starly,Staraptor", Names(Pokedex.Sinnoh, Query() with { Type1 = PokemonType.Flying }));
        Assert.Equal("Onix", Names(Pokedex.Sinnoh, Query() with { Type1 = PokemonType.Rock, Type2 = PokemonType.Ground }));
        Assert.Equal("", Names(Pokedex.Sinnoh, Query() with { Type1 = PokemonType.Rock, Type2 = PokemonType.Water }));
        Assert.Equal("Onix", Names(Pokedex.Sinnoh, Query() with { Shape = "Squiggle" }));

        // By weight and height, only among the caught, whose size the Pokédex knows
        var heaviest = PokedexSearch.Run(dex, Pokedex.Sinnoh, Query() with { Order = PokedexOrder.Heaviest });
        Assert.Equal(7, heaviest.Count);
        Assert.DoesNotContain(heaviest, e => e.Species.Name == "Staraptor");
        Assert.True(heaviest.Zip(heaviest.Skip(1)).All(p => p.First.Species.Weight >= p.Second.Species.Weight));
        Assert.Equal(("Onix", "Starly"), (heaviest[0].Species.Name, heaviest[^1].Species.Name));
        Assert.Equal("Starly", PokedexSearch.Run(dex, Pokedex.Sinnoh, Query() with { Order = PokedexOrder.Lightest })[0].Species.Name);
        Assert.Equal("Onix", PokedexSearch.Run(dex, Pokedex.Sinnoh, Query() with { Order = PokedexOrder.Tallest })[0].Species.Name);
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

    // ------------------------------------------------------------------ the regional Pokédexes (plan 18 · W3)

    [Fact]
    public void EveryRegionalPokedexHasTheCountPokeApiGivesIt()
    {
        Assert.Equal(151, Pokedex.Entries("kanto").Count);
        Assert.Equal(210, Pokedex.Entries("extended-sinnoh").Count);
        Assert.Equal(400, Pokedex.Entries("galar").Count);
        Assert.Equal(242, Pokedex.Entries("hisui").Count);
        Assert.Equal(1025 + PokemonDatabase.Ours().Count, Pokedex.Entries(Pokedex.National).Count);
        Assert.Empty(Pokedex.Entries("no-such-pokedex"));
    }

    [Fact]
    public void PlatinumsOwnTableAndPokeApisExtendedSinnohAgreeEntryForEntry()
    {
        var ours = PokemonDatabase.GetAll().Where(s => s.SinnohNumber != null).ToDictionary(s => s.Name, s => s.SinnohNumber!.Value);
        var theirs = Pokedex.Entries("extended-sinnoh").ToDictionary(e => e.Species.Name, e => e.Number);
        Assert.Equal(ours.OrderBy(p => p.Key), theirs.OrderBy(p => p.Key));
        Assert.Equal(Pokedex.Entries(Pokedex.Sinnoh).Select(e => e.Species.Name), Pokedex.Entries("extended-sinnoh").Select(e => e.Species.Name));
    }

    [Fact]
    public void EveryPokedexIsNumberedFromItsFirstNumberToItsLastWithoutAGap()
    {
        var ids = PokemonDatabase.GetAll().SelectMany(s => s.DexNumbers.Keys).Distinct().Order().ToList();
        Assert.Contains("kanto", ids);
        Assert.Contains("national", ids);
        Assert.DoesNotContain("conquest-gallery", ids);   // only the main series
        foreach (string id in ids)
        {
            // (the National one's species of our own, plan 03 · D14, follow its 1,025 by their own numbers)
            var numbers = Pokedex.Entries(id).Where(e => !e.Species.IsOurs).Select(e => e.Number).ToList();
            int first = numbers[0];
            Assert.True(first is 0 or 1, $"{id} begins at {first}");
            Assert.Equal(Enumerable.Range(first, numbers.Count), numbers);
        }
        // Unova's two number Victini 000
        Assert.Equal(("Victini", 0), (Pokedex.Entries("original-unova")[0].Species.Name, Pokedex.Entries("original-unova")[0].Number));
        Assert.Equal(0, PokemonDatabase.Get("Victini")!.DexNumbers["updated-unova"]);
        // The National Pokédex is the species' own number
        Assert.All(PokemonDatabase.GetAll(), species => Assert.Equal(species.DexNumber, species.DexNumbers["national"]));
        Assert.Equal(25, PokemonDatabase.Get("Pikachu")!.DexNumbers["kanto"]);
    }

    [Fact]
    public void ARegionFilesDexAndRuleAreRead()
    {
        var sinnoh = RegionDatabase.Get("Sinnoh")!;
        var kanto = RegionDatabase.Get("Kanto")!;
        Assert.Equal(Pokedex.Sinnoh, sinnoh.Dex);
        Assert.Equal(new NationalUnlock(NationalUnlockRule.HallOfFameAndSeenAll, 0), sinnoh.NationalUnlock);
        Assert.Equal("kanto", kanto.Dex);
        Assert.Equal(new NationalUnlock(NationalUnlockRule.Caught, 60), kanto.NationalUnlock);
        Assert.Equal("updated-johto", RegionDatabase.Get("Johto")!.Dex);
        Assert.Equal("hoenn", RegionDatabase.Get("Hoenn")!.Dex);
        Assert.Equal("updated-unova", RegionDatabase.Get("Unova")!.Dex);
        Assert.Null(RegionDatabase.Get("Kalos")!.Dex);
        Assert.Equal(NationalUnlockRule.HallOfFame, NationalUnlock.Parse(null).Rule);
        Assert.Equal(NationalUnlockRule.HallOfFame, NationalUnlock.Parse("Caught").Rule);
        Assert.Equal(new NationalUnlock(NationalUnlockRule.Caught, 100), NationalUnlock.Parse("Caught100"));
        Assert.Same(kanto, Pokedex.RegionOf("kanto"));
        Assert.Same(sinnoh, Pokedex.RegionOf(Pokedex.Sinnoh));
        Assert.Null(Pokedex.RegionOf(Pokedex.National));
        // Every dex a region plays by is numbered in the data
        foreach (var region in RegionDatabase.All.Where(r => r.Dex != null)) Assert.NotEmpty(Pokedex.Entries(region.Dex!));
    }

    [Fact]
    public void KantosRuleOpensTheNationalPokedexAtSixtyCaughtAndSinnohsIsAsItWas()
    {
        var kanto = RegionDatabase.Get("Kanto")!;
        var story = new PokemonPlatinumEngine.Story.StoryState();
        var dex = new Pokedex();
        foreach (var e in Pokedex.Entries("kanto").Take(59)) dex.RegisterCaught(e.Species.DexNumber);
        Assert.False(dex.CanUnlockNational(story, kanto));
        dex.RegisterCaught(Pokedex.Entries("kanto")[59].Species.DexNumber);
        Assert.True(dex.CanUnlockNational(story, kanto));
        Assert.False(dex.CanUnlockNational(story), "Sinnoh's rule asks for its Hall of Fame and every species seen");
        dex.UnlockNational();
        Assert.False(dex.CanUnlockNational(story, kanto));
    }

    [Fact]
    public void ARegionEnteredGivesItsPokedexAndTheListingsCountTheirOwnSpecies()
    {
        var dex = new Pokedex();
        Assert.Equal(new[] { Pokedex.Sinnoh }, dex.Dexes);
        dex.Enter(RegionDatabase.Get("Kalos")!);   // no Pokédex yet: nothing is added
        Assert.Equal(new[] { Pokedex.Sinnoh }, dex.Dexes);
        dex.Enter(RegionDatabase.Get("Kanto")!);
        dex.Enter(RegionDatabase.Get("Kanto")!);
        Assert.Equal(new[] { Pokedex.Sinnoh, "kanto" }, dex.Dexes);   // the chain's order: Sinnoh first
        dex.UnlockNational();
        Assert.Equal(new[] { Pokedex.Sinnoh, "kanto", Pokedex.National }, dex.Dexes);

        // Seen and caught are shared: a Bulbasaur caught is in Kanto's list and not in Sinnoh's
        dex.RegisterCaught(Dex("Bulbasaur"));
        dex.RegisterSeen(Dex("Starly"));
        Assert.Equal((1, 1), (dex.SeenIn("kanto"), dex.CaughtIn("kanto")));
        Assert.Equal((1, 0), (dex.SeenIn(Pokedex.Sinnoh), dex.CaughtIn(Pokedex.Sinnoh)));

        // A save brings the entered Pokédexes back, with Sinnoh's always
        var restored = new Pokedex();
        restored.Restore(true, dex.Diplomas, dex.Entered);
        Assert.Equal(dex.Dexes, restored.Dexes);
        var older = new Pokedex();
        older.Restore(false, System.Array.Empty<string>());
        Assert.Equal(new[] { Pokedex.Sinnoh }, older.Dexes);
    }

    [Fact]
    public void ADiplomaIsSavedUnderTheNameTheFirstSavesGaveIt()
    {
        Assert.Equal("Sinnoh", Pokedex.DiplomaKey(Pokedex.Sinnoh));
        Assert.Equal("National", Pokedex.DiplomaKey(Pokedex.National));
        Assert.Equal("kanto", Pokedex.DiplomaKey("kanto"));
        Assert.Equal(Pokedex.Sinnoh, Pokedex.FromDiplomaKey("Sinnoh"));
        Assert.Equal(Pokedex.National, Pokedex.FromDiplomaKey("National"));
        Assert.Equal("kanto", Pokedex.FromDiplomaKey("kanto"));

        // Kanto's asks for its 151 seen, once the player has it
        var dex = new Pokedex();
        foreach (var e in Pokedex.Entries("kanto")) dex.RegisterSeen(e.Species.DexNumber);
        Assert.Empty(dex.AwardDiplomas());   // not entered yet
        dex.Enter(RegionDatabase.Get("Kanto")!);
        Assert.Equal(new[] { "kanto" }, dex.AwardDiplomas());
    }

    [Fact]
    public void ARegionWithNoHabitatsFileHasNoAreaAndSinnohsIsRead()
    {
        Assert.NotNull(Habitats.Of(RegionDatabase.Get("Sinnoh")!));
        Assert.Same(Habitats.Sinnoh, Habitats.Of(RegionDatabase.Get("Sinnoh")!));
        Assert.Null(Habitats.Of(RegionDatabase.Get("Kalos")!));
        // Unova has its own since plan 22 · U2 (Black 2's tables of the areas open)
        Assert.NotNull(Habitats.Of(RegionDatabase.Get("Unova")!));
        Assert.NotNull(Habitats.Of(RegionDatabase.Get("Hoenn")!));
        // Kanto has its own since plan 19 · K1, Johto since plan 20 · J1 (HeartGold's tables, before any area opens)
        Assert.NotNull(Habitats.Of(RegionDatabase.Get("Kanto")!));
        Assert.NotNull(Habitats.Of(RegionDatabase.Get("Johto")!));
    }
}
