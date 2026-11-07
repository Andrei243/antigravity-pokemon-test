using System.Text.Json;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 38 · EX1: a day's outbreaks of one species. The rules (<see cref="Outbreaks"/>) are GPU-free and decided by the day's
/// number alone, so a seed and a date give the same two every time; the grass's slots 0 to 5 are the outbreak's in its place
/// and nowhere else; the count of its species beaten or caught there gives a shiny roll more from thirty and two from sixty;
/// and with none in force a wild Pokémon is made with the draws it always was.
/// </summary>
public class OutbreakTests
{
    private static readonly string[] Regions =
        { RegionDatabase.Sinnoh, RegionDatabase.Kanto, RegionDatabase.Johto, RegionDatabase.Hoenn, RegionDatabase.Unova, RegionDatabase.Kalos };

    /// <summary>The day's numbers of a game, as the record-mixing generator moves them on.</summary>
    private static IEnumerable<uint> Days(uint first, int count)
    {
        uint number = first;
        for (int i = 0; i < count; i++) { yield return number; number = SpecialEncounters.Next(number); }
    }

    private static MapArea AreaWithTables(string key, string species = "Raticate")
    {
        var area = new MapArea { Key = key, Open = true };
        foreach (int weight in WorldAreaFile.LandSlotWeights)
            area.WildEncounters.Add(new WildEncounterEntry { SpeciesName = species, MinLevel = 20, MaxLevel = 24, Weight = weight });
        return area;
    }

    [Fact]
    public void ADayHasItsOutbreaksFromTheDaysNumberAloneAndTheNextDayOthers()
    {
        foreach (string region in Regions.Where(r => Outbreaks.PlacesOf(r).Count > 0))
        {
            foreach (uint day in Days(0xC0FFEEu, 12))
                Assert.Equal(Outbreaks.Today(region, day), Outbreaks.Today(region, day));
            var seen = Days(0xC0FFEEu, 12).Select(d => string.Join("|", Outbreaks.Today(region, d).Select(o => $"{o.Area}:{o.Species}"))).Distinct().Count();
            Assert.True(seen >= 8, $"{region}: only {seen} different days in twelve");
        }
        // Another game's day number is another day's draw
        Assert.NotEqual(Outbreaks.Today("Sinnoh", 11), Outbreaks.Today("Sinnoh", 12));
    }

    [Fact]
    public void ARegionHasTwoADayAtDifferentPlacesEachOfASpeciesOfItsOwnTableThatIsNotItsCommonest()
    {
        foreach (string region in Regions)
        {
            var places = Outbreaks.PlacesOf(region);
            if (places.Count == 0) continue;
            var world = World.LoadAll().Single(w => string.Equals(w.Index.Region, region, StringComparison.OrdinalIgnoreCase));
            var rows = Outbreaks.RowsOf(region);
            foreach (uint day in Days(7, 80))
            {
                var today = Outbreaks.Today(region, day);
                Assert.Equal(Outbreaks.PerDay, today.Count);
                Assert.Equal(today.Count, today.Select(o => o.Area).Distinct().Count());
                foreach (var outbreak in today)
                {
                    Assert.Contains(places, p => p.Area == outbreak.Area);
                    Assert.Equal(Outbreaks.ShareOf(outbreak.Slots), outbreak.Share);
                    if (rows.Any(r => r.Area == outbreak.Area && r.Species == outbreak.Species && r.Level == outbreak.Level)) continue; // a row of the region's own
                    // A generated one is of the place's own table: a species it lists and not the one the most slots' weights come to
                    var land = world.Area(outbreak.Area)!.Land!;
                    var listed = land.SelectMany(e => new[] { e.Species, e.Day, e.Night }).Concat(world.Area(outbreak.Area)!.Day ?? new()).Concat(world.Area(outbreak.Area)!.Night ?? new())
                        .Where(s => s != null).ToHashSet();
                    Assert.Contains(outbreak.Species, listed);
                    var weight = new Dictionary<string, int>();
                    for (int i = 0; i < land.Count; i++) weight[land[i].Species] = weight.GetValueOrDefault(land[i].Species) + (land[i].Weight ?? WorldAreaFile.LandSlotWeights[i]);
                    string commonest = land.Select(e => e.Species).Distinct().OrderByDescending(s => weight[s]).First();
                    Assert.NotEqual(commonest, outbreak.Species);
                    Assert.Equal(Outbreaks.GrassShare, outbreak.Share);
                }
            }
        }
    }

    [Fact]
    public void NeverASafarisPlaceALegendaryAMythicalOrAPlaceTheStoryHolds()
    {
        foreach (string region in Regions)
        {
            foreach (var place in Outbreaks.PlacesOf(region))
            {
                Assert.DoesNotContain("safari", place.Area);
                Assert.DoesNotContain("marsh", place.Area);
                Assert.DoesNotContain("gym", place.Area);
                Assert.DoesNotContain("national_park", place.Area);
                Assert.DoesNotContain("contest", place.Area);
                foreach (string species in place.Species)
                {
                    var s = PokemonDatabase.Get(species)!;
                    Assert.False(s.Legendary || s.Mythical, $"{region}/{place.Area}: {species}");
                    Assert.False(s.IsOurs, $"{region}/{place.Area}: {species} is a species of ours");
                }
            }
            foreach (var outbreak in Days(99, 120).SelectMany(d => Outbreaks.Today(region, d)))
            {
                var s = PokemonDatabase.Get(outbreak.Species)!;
                Assert.False(s.Legendary || s.Mythical || s.IsOurs, $"{region}: {outbreak.Species}");
                Assert.DoesNotContain("safari", outbreak.Area);
            }
        }
        // Sinnoh's Great Marsh and Trophy Garden hold tables of twelve and are not among its places
        Assert.DoesNotContain(Outbreaks.PlacesOf("Sinnoh"), p => p.Area.StartsWith("great_marsh") || p.Area == "trophy_garden");
        Assert.DoesNotContain(Outbreaks.PlacesOf("Hoenn"), p => p.Area.StartsWith("hoenn_safari_zone"));
    }

    [Fact]
    public void NeverAtThePlaceOfTheDaysSwarm()
    {
        foreach (uint day in Days(5, 60))
        {
            var plain = Outbreaks.Today("Sinnoh", day);
            foreach (var swarm in plain.Select(o => o.Area).Concat(new[] { Swarms.Areas[(int)(day % (uint)Swarms.Areas.Length)] }))
            {
                var swarmed = Outbreaks.Today("Sinnoh", day, swarm);
                Assert.Equal(Outbreaks.PerDay, swarmed.Count);
                Assert.DoesNotContain(swarmed, o => o.Area == swarm);
            }
        }
    }

    [Fact]
    public void OutbreaksBeginWithTheRegionsHallOfFame()
    {
        var sinnoh = RegionDatabase.Get(RegionDatabase.Sinnoh)!;
        var story = new StoryState();
        Assert.False(Outbreaks.Open(sinnoh, story));
        story.Set(RegionDatabase.Get(RegionDatabase.Kanto)!.StoryCompleteFlag);
        Assert.False(Outbreaks.Open(sinnoh, story));
        story.Set(sinnoh.StoryCompleteFlag);
        Assert.True(Outbreaks.Open(sinnoh, story));
        Assert.False(Outbreaks.Open(null, story));
    }

    [Fact]
    public void TheFirstSlotsOfTheGrassAreTheOutbreaksInItsPlaceAndNowhereElse()
    {
        var area = AreaWithTables("route_201");
        var other = AreaWithTables("route_202");
        var outbreak = new Outbreak("route_201", "Starly", Outbreaks.SlotsFor(Outbreaks.GrassShare));
        Assert.Equal(6, outbreak.Slots);
        Assert.Equal(80, outbreak.Share);
        var moment = new EncounterMoment { Outbreaks = new[] { outbreak } };

        var grass = EncounterSlots.Grass(area, moment);
        Assert.Equal(Enumerable.Repeat("Starly", 6).Concat(Enumerable.Repeat("Raticate", 6)), grass.Select(e => e.SpeciesName));
        // Each slot keeps its own weight and levels, and eight in ten of the meetings are the outbreak's
        Assert.Equal(WorldAreaFile.LandSlotWeights, grass.Select(e => e.Weight));
        Assert.All(grass.Take(6), e => { Assert.True(e.Outbreak); Assert.Equal(20, e.MinLevel); Assert.Null(e.Moves); });
        Assert.All(grass.Skip(6), e => Assert.False(e.Outbreak));
        Assert.Equal(80, grass.Where(e => e.SpeciesName == "Starly").Sum(e => e.Weight));
        // The area's own table is untouched, and another place's, and a moment with none or before the Hall of Fame
        Assert.All(area.WildEncounters, e => Assert.Equal("Raticate", e.SpeciesName));
        Assert.All(EncounterSlots.Grass(other, moment), e => Assert.Equal("Raticate", e.SpeciesName));
        Assert.All(EncounterSlots.Grass(area, new EncounterMoment()), e => Assert.Equal("Raticate", e.SpeciesName));
        Assert.All(EncounterSlots.Grass(area, new EncounterMoment { Outbreaks = Array.Empty<Outbreak>() }), e => Assert.Equal("Raticate", e.SpeciesName));

        // Eight in ten of the Pokémon met are the outbreak's
        var rng = new Random(11);
        int met = 0;
        for (int i = 0; i < 20000; i++)
            if (WildEncounterRules.Meet(grass, false, null, rng)!.SpeciesName == "Starly") met++;
        Assert.InRange(met / 20000.0, 0.78, 0.82);
    }

    [Fact]
    public void TheSlotsOfTheSecondSlotsGameAndOursKeepTheirsAndALevelAndMovesAreTheRowsOwn()
    {
        var area = AreaWithTables("route_102", "Zigzagoon");
        area.NationalSlots.AddRange(new[] { "Hoothoot", "Sentret" });
        area.OursLand.Add(new OursSlot("Ratchisel", 105, 109));
        area.OursLand.Add(new OursSlot("Najarok", 106, 110));
        string[] moves = { "Bide", "Harden", "Leech Seed" };
        var outbreak = new Outbreak("route_102", "Seedot", Outbreaks.SlotsFor(50), 3, moves);
        Assert.Equal(3, outbreak.Slots);
        Assert.Equal(50, outbreak.Share);

        var grass = EncounterSlots.Grass(area, new EncounterMoment { NationalDex = true, OursOpen = true, Outbreaks = new[] { outbreak } });
        Assert.Equal(new[] { "Seedot", "Seedot", "Seedot", "Zigzagoon", "Zigzagoon", "Zigzagoon" }, grass.Take(6).Select(e => e.SpeciesName));
        Assert.All(grass.Take(3), e => { Assert.Equal(3, e.MinLevel); Assert.Equal(3, e.MaxLevel); Assert.Equal(moves, e.Moves); });
        Assert.All(grass.Skip(3).Take(3), e => Assert.Equal(20, e.MinLevel));
        Assert.Equal(new[] { "Hoothoot", "Sentret" }, grass.Skip(OverlayNational.FirstLandSlot).Take(2).Select(e => e.SpeciesName));
        Assert.Equal(new[] { "Ratchisel", "Najarok" }, grass.Skip(OverlayOurs.FirstLandSlot).Select(e => e.SpeciesName));

        // What is met carries the level and the moves, and a Pokémon made with them knows them
        var rng = new Random(2);
        var seedot = Enumerable.Range(0, 400).Select(_ => WildEncounterRules.Meet(grass, false, null, rng)!).First(e => e.SpeciesName == "Seedot");
        Assert.Equal(3, seedot.MinLevel);
        Assert.Equal(moves, seedot.Moves);
        var pokemon = new Pokemon(PokemonDatabase.Get("Seedot")!, 3);
        Outbreaks.GiveMoves(pokemon, seedot.Moves!);
        Assert.Equal(moves, pokemon.Moves.Select(m => m.Data.Name));
        // A move the data doesn't have is passed over, and none known leaves the Pokémon as it was
        var plain = new Pokemon(PokemonDatabase.Get("Seedot")!, 3);
        var before = plain.Moves.Select(m => m.Data.Name).ToList();
        Outbreaks.GiveMoves(plain, new[] { "No Such Move" });
        Assert.Equal(before, plain.Moves.Select(m => m.Data.Name));
    }

    [Fact]
    public void TheCountGivesARollMoreFromThirtyAndTwoFromSixty()
    {
        Assert.Equal(0, Outbreaks.ExtraRolls(0));
        Assert.Equal(0, Outbreaks.ExtraRolls(29));
        Assert.Equal(1, Outbreaks.ExtraRolls(30));
        Assert.Equal(1, Outbreaks.ExtraRolls(59));
        Assert.Equal(2, Outbreaks.ExtraRolls(60));
        Assert.Equal(2, Outbreaks.ExtraRolls(500));
    }

    [Fact]
    public void ThePlacesOfRegionsWithAFileHaveTheirOwnRowsAndHoennsAreEmeraldsFiveAndOursForSurskit()
    {
        var rows = Outbreaks.RowsOf("Hoenn");
        // Emerald's table (src/tv.c, sPokeOutbreakSpeciesList): five rows at half the meetings
        var emeralds = rows.Where(r => r.Species != "Surskit").ToList();
        Assert.Equal(
            new[] { "route_102:Seedot:3", "route_114:Nuzleaf:15", "route_117:Seedot:13", "route_120:Seedot:25", "route_116:Skitty:8" },
            emeralds.Select(r => $"{r.Area}:{r.Species}:{r.Level}"));
        Assert.All(rows, r => Assert.Equal(50, r.Share));
        Assert.Equal(new[] { "Bide", "Harden", "Leech Seed" }, emeralds[0].Moves);
        // ...and ours: the Surskit Emerald's grass leaves out
        Assert.Equal(new[] { "route_102", "route_117", "route_119" }, rows.Where(r => r.Species == "Surskit").Select(r => r.Area));
        Assert.All(rows, r => Assert.Contains(Outbreaks.PlacesOf("Hoenn"), p => p.Area == r.Area));
        // No other region has a row
        foreach (string region in Regions.Where(r => r != "Hoenn")) Assert.Empty(Outbreaks.RowsOf(region));

        // Over the days Hoenn's outbreak is Surskit, and Seedot, on a route of the row
        var days = Days(1, 200).SelectMany(d => Outbreaks.Today("Hoenn", d)).ToList();
        var surskit = days.First(o => o.Species == "Surskit");
        Assert.Contains(rows, r => r.Species == "Surskit" && r.Area == surskit.Area && r.Level == surskit.Level);
        Assert.Equal(3, surskit.Slots);
        Assert.Contains(days, o => o.Species == "Seedot" && o.Moves?.Contains("Leech Seed") == true);
        // ...which the species check counts: Hoenn meets Surskit, and Masquerain by evolving it
        Assert.True(SpeciesSources.Hoenn.Has("Surskit"));
        Assert.True(SpeciesSources.Hoenn.Has("Masquerain"));
        Assert.Contains("an outbreak", SpeciesSources.Hoenn.Ways["Surskit"]);
    }

    [Fact]
    public void TheCountIsSavedAndANewDayClearsIt()
    {
        var state = new SpecialEncounters { DailyNumber = 3 };
        Assert.Null(state.OutbreakMet);
        Assert.Equal(0, state.OutbreakMetIn("route_201"));
        Assert.Equal(1, state.CountOutbreak("route_201"));
        Assert.Equal(2, state.CountOutbreak("route_201"));
        Assert.Equal(1, state.CountOutbreak("route_202"));

        var read = JsonSerializer.Deserialize<SpecialEncounters>(JsonSerializer.Serialize(state))!;
        Assert.Equal(2, read.OutbreakMetIn("route_201"));
        Assert.Equal(1, read.OutbreakMetIn("route_202"));
        // A count of nothing leaves the field out of the save
        Assert.DoesNotContain("OutbreakMet", JsonSerializer.Serialize(new SpecialEncounters()));

        read.DaysPass(1);
        Assert.Null(read.OutbreakMet);
        Assert.Equal(0, read.OutbreakMetIn("route_201"));
        state.DaysPass(0);
        Assert.Equal(2, state.OutbreakMetIn("route_201"));
    }

    [Fact]
    public void TenStandInTheGrassOfAnOutbreaksPlaceAndSixAnywhereElse()
    {
        Assert.Equal(10, FieldPokemon.MostInOutbreak);
        Assert.Equal(Outbreaks.MostInOutbreak, FieldPokemon.MostInOutbreak);
        Assert.True(FieldPokemon.MostInOutbreak > FieldPokemon.MostInView);
    }
}

/// <summary>
/// Plan 38 · EX1: the engine's part. A Pokémon of the day's outbreak in its place is given the count's shiny rolls, drawn
/// only then; every other is made with the draws it always was. It shares the dice and the databases with the whole
/// process, so it runs alone.
/// </summary>
[Collection("Shared clocks and dice")]
public class OutbreakGameTests
{
    private static GameEngine NewGameInSinnoh()
    {
        var engine = new GameEngine(new GameSettings { Muted = true });
        engine.InitializeHeadless();
        engine.PeopleStayPut = true;
        engine.NewGameRegion = "Sinnoh";
        engine.StartNewGame();
        return engine;
    }

    private static (GameEngine Engine, Outbreak Outbreak, MapArea Area) PastTheHallOfFame()
    {
        var engine = NewGameInSinnoh();
        var game = engine.Drive;
        game.Story.Set("SinnohHallOfFame");
        game.Encounters.DailyNumber = 0x1234ABCD;
        var outbreak = Outbreaks.Today("Sinnoh", game.Encounters.DailyNumber)[0];
        return (engine, outbreak, new MapArea { Key = outbreak.Area, Open = true });
    }

    [Fact]
    public void TheMomentCarriesTheDaysOutbreaksOnlyPastTheHallOfFame()
    {
        var engine = NewGameInSinnoh();
        var game = engine.Drive;
        game.Encounters.DailyNumber = 77;
        Assert.Null(game.EncounterMomentNow().Outbreaks);

        game.Story.Set("SinnohHallOfFame");
        var moment = game.EncounterMomentNow();
        Assert.Equal(Outbreaks.PerDay, moment.Outbreaks!.Count);
        Assert.Equal(Outbreaks.Today("Sinnoh", 77), moment.Outbreaks);
        // The next day is others
        game.Encounters.DaysPass(1);
        Assert.NotEqual(moment.Outbreaks, game.EncounterMomentNow().Outbreaks);
    }

    [Fact]
    public void TheThirtyFirstOfTheOutbreaksSpeciesBeatenInItsPlaceIsGivenOneRollMoreAndTheSixtyFirstTwo()
    {
        var (engine, outbreak, area) = PastTheHallOfFame();
        var game = engine.Drive;
        var foe = new Pokemon(PokemonDatabase.Get(outbreak.Species)!, 10);
        var rolls = new List<int>();
        for (int n = 1; n <= 61; n++)
        {
            rolls.Add(engine.OutbreakRolls(area, outbreak.Species));
            engine.CountOutbreakIn(foe, outbreak.Area);
        }
        Assert.Equal(61, game.Encounters.OutbreakMetIn(outbreak.Area));
        // The first thirty are made as ever, the thirty-first has a roll more, the sixty-first two
        Assert.All(rolls.Take(30), r => Assert.Equal(0, r));
        Assert.All(rolls.Skip(30).Take(30), r => Assert.Equal(1, r));
        Assert.Equal(2, rolls[60]);
    }

    [Fact]
    public void OnlyTheOutbreaksSpeciesInItsPlaceIsCounted()
    {
        var (engine, outbreak, area) = PastTheHallOfFame();
        var game = engine.Drive;
        var other = PokemonDatabase.GetAll().First(s => s.Name != outbreak.Species && !s.Legendary && !s.Mythical);
        engine.CountOutbreakIn(new Pokemon(other, 10), outbreak.Area);
        engine.CountOutbreakIn(new Pokemon(PokemonDatabase.Get(outbreak.Species)!, 10), "somewhere_else");
        engine.CountOutbreakIn(new Pokemon(PokemonDatabase.Get(outbreak.Species)!, 10), null);
        Assert.Null(game.Encounters.OutbreakMet);
        Assert.Equal(0, engine.OutbreakRolls(new MapArea { Key = "somewhere_else" }, outbreak.Species));
        Assert.Equal(0, engine.OutbreakRolls(null, outbreak.Species));
        engine.CountOutbreakIn(new Pokemon(PokemonDatabase.Get(outbreak.Species)!, 10), outbreak.Area);
        Assert.Equal(1, game.Encounters.OutbreakMetIn(outbreak.Area));

        // And nothing is counted before the Hall of Fame
        var before = NewGameInSinnoh();
        before.CountOutbreakIn(new Pokemon(PokemonDatabase.Get(outbreak.Species)!, 10), outbreak.Area);
        Assert.Null(before.Drive.Encounters.OutbreakMet);
        Assert.Equal(0, before.OutbreakRolls(area, outbreak.Species));
    }

    [Fact]
    public void AWildPokemonIsMadeWithTheDrawsItAlwaysWasUntilThirtyOfTheOutbreaksSpeciesAreMet()
    {
        var (engine, outbreak, area) = PastTheHallOfFame();
        var game = engine.Drive;
        var entry = new WildEncounterEntry { SpeciesName = outbreak.Species, MinLevel = 12, MaxLevel = 14, Weight = 20, Outbreak = true };

        // What a Pokémon is made from, and where the generator stands after it
        (uint Personality, int Level, int Next) Make(Random rng, MapArea? where)
        {
            Dice.Seed(41);
            game.FieldRandom = rng;
            var made = engine.MakeWild(entry, where);
            return (made.Personality, made.Level, rng.Next());
        }

        // Past the Hall of Fame but under thirty met, in its place or not: the same draws as before the Hall of Fame
        var past = Make(new Random(41), area);
        Assert.Equal(past, Make(new Random(41), new MapArea { Key = "somewhere_else" }));
        Assert.Equal(past, Make(new Random(41), null));
        for (int i = 0; i < 29; i++) game.Encounters.CountOutbreak(outbreak.Area);
        Assert.Equal(past, Make(new Random(41), area));

        // The thirty-first has a roll more: more is drawn from the generator
        game.Encounters.CountOutbreak(outbreak.Area);
        Assert.NotEqual(past.Next, Make(new Random(41), area).Next);
        // ...but a Pokémon of another place, or of another species, has none
        Assert.Equal(past, Make(new Random(41), new MapArea { Key = "somewhere_else" }));
        entry.SpeciesName = PokemonDatabase.GetAll().First(s => s.Name != outbreak.Species && !s.Legendary && !s.Mythical).Name;
        Assert.Equal(Make(new Random(41), new MapArea { Key = "somewhere_else" }), Make(new Random(41), area));
    }

    [Fact]
    public void AnOutbreakOfTheRegionsOwnGameGivesItsPokemonTheMovesAndNothingIsDrawnForThem()
    {
        var (engine, _, area) = PastTheHallOfFame();
        var game = engine.Drive;
        string[] moves = { "Harden", "Growth", "Nature Power", "Leech Seed" };
        var entry = new WildEncounterEntry { SpeciesName = "Nuzleaf", MinLevel = 15, MaxLevel = 15, Weight = 20, Outbreak = true, Moves = moves };
        var plain = new WildEncounterEntry { SpeciesName = "Nuzleaf", MinLevel = 15, MaxLevel = 15, Weight = 20 };

        // (A Pokémon's personality is drawn from the dice, its level and the rest from the field's generator: both seeded)
        Dice.Seed(5);
        game.FieldRandom = new Random(5);
        var given = engine.MakeWild(entry, area);
        Dice.Seed(5);
        game.FieldRandom = new Random(5);
        var without = engine.MakeWild(plain, area);
        Assert.Equal(moves, given.Moves.Select(m => m.Data.Name));
        Assert.Equal(without.Personality, given.Personality);
        Assert.Equal(15, given.Level);
    }
}
