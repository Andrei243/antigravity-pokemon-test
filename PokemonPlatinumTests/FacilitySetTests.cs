using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DataImporter;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.Facilities;
using PokemonPlatinumEngine.Models.Frontier;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 37 · FC1, the later sets: a battle set for each role of every species of the games that Showdown's random-battle
/// files name (<c>Data/facility-sets.json</c>, written by <c>tools/DataImporter</c>'s <see cref="FacilitySets"/>), each legal
/// by <see cref="Legality"/>, run in full and held to the Frontier's banlist, and the later places' trainers drawn from them
/// (<see cref="FacilityTrainers"/>). <c>frontier.json</c> is untouched.
/// </summary>
public class FacilitySetTests
{
    // ================================================================== the file

    [Fact]
    public void EverySetIsLegalRunsInFullAndHasItsSpeciesOwnAbilityAndAnItem()
    {
        var sets = FacilitySetDatabase.Sets;
        Assert.InRange(sets.Count, 700, 1200);
        Assert.Equal(sets.Count, sets.Select(s => s.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        foreach (var set in sets)
        {
            var species = PokemonDatabase.Get(set.Species);
            Assert.True(species != null, $"{set.Id}: {set.Species} is no species");
            Assert.False(species!.IsOurs, set.Id);
            Assert.InRange(set.Moves.Count, 1, 4);
            Assert.Equal(set.Moves.Count, set.Moves.Distinct().Count());
            var learnable = Legality.Learnable(species);
            foreach (string move in set.Moves)
            {
                Assert.True(learnable.Contains(move), $"{set.Id} can't learn {move}");
                var data = MoveDatabase.Get(move);
                Assert.True(data.Kind == MoveKind.Standard && data.Support == MoveEffectSupport.Full, $"{set.Id}: {move} doesn't run in full");
            }
            Assert.True(set.Ability != null && (species.Abilities.Contains(set.Ability) || species.HiddenAbility == set.Ability),
                $"{set.Id}: {set.Ability} isn't {species.Name}'s own");
            Assert.True(set.Item != null && ItemDatabase.Get(set.Item) != null, $"{set.Id}: {set.Item} is no item");
            Assert.InRange(set.Tier, 1, FacilitySetDatabase.Tiers);
            Assert.InRange(System.Numerics.BitOperations.PopCount((uint)set.EvFlags & 0x3F), 2, 3);
            Assert.Null(set.Form);
        }
    }

    [Fact]
    public void NoSetIsOfASpeciesTheFrontierBansAndTheReportListsEveryOtherSpecies()
    {
        var sets = FacilitySetDatabase.Sets;
        Assert.DoesNotContain(sets, s => FrontierDatabase.IsBanned(s.Species));
        foreach (string banned in FrontierDatabase.Banned)
            Assert.Contains(FacilitySetDatabase.Without, w => w.Species == banned && w.Why == FacilitySetGapReason.Banned);

        // Every species of the 1,025 has a set or stands on the report's list, never both
        var withSets = sets.Select(s => s.Species).ToHashSet();
        var without = FacilitySetDatabase.Without.Select(w => w.Species).ToList();
        Assert.Equal(without.Count, without.Distinct().Count());
        foreach (var species in PokemonDatabase.GetAll())
            Assert.True(withSets.Contains(species.Name) ^ without.Contains(species.Name), species.Name);

        // Every fully evolved species of the games that a file names has a set: what is left out of those is why
        var fullyEvolved = PokemonDatabase.GetAll().Where(s => s.Evolutions is not { Count: > 0 }).ToList();
        var missing = fullyEvolved.Where(s => !withSets.Contains(s.Name)).Select(s => FacilitySetDatabase.Without.Single(w => w.Species == s.Name)).ToList();
        Assert.All(missing, w => Assert.NotEqual(FacilitySetGapReason.NotNamed, w.Why));
        Assert.True(missing.Count <= FrontierDatabase.Banned.Count + 4, string.Join(", ", missing.Select(w => $"{w.Species} ({w.Why})")));

        // The later generations are there: Kalos's and Paldea's among them
        foreach (string later in new[] { "Greninja", "Talonflame", "Decidueye", "Dragapult", "Kingambit", "Meowscarada", "Garchomp" })
            Assert.Contains(sets, s => s.Species == later);
    }

    [Fact]
    public void TheSetsAreInNationalOrderAndATiersBandFollowsTheLevel()
    {
        var numbers = FacilitySetDatabase.Sets.Select(s => PokemonDatabase.Get(s.Species)!.DexNumber).ToList();
        Assert.Equal(numbers.OrderBy(n => n), numbers);

        // Lower the level, stronger the set: the bands fall as the level rises, every band reached
        Assert.Equal(4, FacilitySetDatabase.TierOfLevel(64));
        Assert.Equal(4, FacilitySetDatabase.TierOfLevel(78));
        Assert.Equal(3, FacilitySetDatabase.TierOfLevel(79));
        Assert.Equal(3, FacilitySetDatabase.TierOfLevel(83));
        Assert.Equal(2, FacilitySetDatabase.TierOfLevel(84));
        Assert.Equal(2, FacilitySetDatabase.TierOfLevel(88));
        Assert.Equal(1, FacilitySetDatabase.TierOfLevel(89));
        Assert.Equal(1, FacilitySetDatabase.TierOfLevel(100));
        for (int level = 60; level < 100; level++)
            Assert.True(FacilitySetDatabase.TierOfLevel(level) >= FacilitySetDatabase.TierOfLevel(level + 1));
        Assert.Equal(4, FacilitySetDatabase.TierOfTotal(600));
        Assert.Equal(1, FacilitySetDatabase.TierOfTotal(300));
        for (int tier = 1; tier <= FacilitySetDatabase.Tiers; tier++)
            Assert.Contains(FacilitySetDatabase.Sets, s => s.Tier == tier);

        // A Pokémon the random battles bring at a low level is of a higher tier than one they bring high
        int Best(string species) => FacilitySetDatabase.Of(species).Max(s => s.Tier);
        Assert.True(Best("Kingambit") > Best("Delibird"));
        Assert.True(Best("Garchomp") > Best("Luvdisc"));
    }

    [Fact]
    public void TheFrontiersOwnSetsNameNoAbilityOrTier()
    {
        // frontier.json is the original's and doesn't change: its sets carry neither of the later sets' two fields
        Assert.All(FrontierDatabase.Sets, s => Assert.True(s.Ability == null && s.Tier == 0, s.Id));
        string text = File.ReadAllText(Path.Combine(Repository.Root, "PokemonPlatinumEngine", "Data", FrontierDatabase.FileName));
        Assert.DoesNotContain("\"ability\"", text);
        Assert.DoesNotContain("\"tier\"", text);
    }

    // ================================================================== the rule of ours

    [Fact]
    public void TheItemNatureAndEffortFollowTheMovesAndTheStats()
    {
        var garchomp = PokemonDatabase.Get("Garchomp")!;
        var sweeper = new[] { "Earthquake", "Swords Dance", "Outrage", "Stone Edge" }.Select(MoveDatabase.Get).ToList();
        // One status move, fast and not frail: a Life Orb; physical and fast: Jolly, Attack and Speed
        Assert.Equal("Life Orb", FacilitySets.ItemOf(garchomp, sweeper));
        Assert.Equal(Nature.Jolly, FacilitySets.NatureOf(garchomp, sweeper));
        Assert.Equal(2 | 8, FacilitySets.EffortOf(garchomp, sweeper));

        var blissey = PokemonDatabase.Get("Blissey")!;
        var wall = new[] { "Soft-Boiled", "Toxic", "Seismic Toss", "Thunder Wave" }.Select(MoveDatabase.Get).ToList();
        // A support set, bulky: Leftovers, HP and the better defence (Sp. Def); Seismic Toss is physical, so Careful
        Assert.Equal("Leftovers", FacilitySets.ItemOf(blissey, wall));
        Assert.Equal(1 | 32, FacilitySets.EffortOf(blissey, wall));
        Assert.Equal(Nature.Careful, FacilitySets.NatureOf(blissey, wall));
        var specialWall = new[] { "Soft-Boiled", "Toxic", "Flamethrower", "Thunder Wave" }.Select(MoveDatabase.Get).ToList();
        Assert.Equal(Nature.Calm, FacilitySets.NatureOf(blissey, specialWall));

        var alakazam = PokemonDatabase.Get("Alakazam")!;
        var attacks = new[] { "Psychic", "Shadow Ball", "Focus Blast", "Energy Ball" }.Select(MoveDatabase.Get).ToList();
        // Four special attacks from a base Speed of 120: Choice Specs, Timid, Sp. Atk and Speed
        Assert.Equal("Choice Specs", FacilitySets.ItemOf(alakazam, attacks));
        Assert.Equal(Nature.Timid, FacilitySets.NatureOf(alakazam, attacks));
        Assert.Equal(16 | 8, FacilitySets.EffortOf(alakazam, attacks));

        var rest = new[] { "Rest", "Sleep Talk", "Earthquake", "Outrage" }.Select(MoveDatabase.Get).ToList();
        Assert.Equal("Chesto Berry", FacilitySets.ItemOf(garchomp, rest));
        Assert.Equal("fast_attacker", FacilitySets.Slug("Fast Attacker"));
        Assert.Equal("mr_mime", FacilitySets.Slug("Mr. Mime"));
        Assert.Equal("flabebe", FacilitySets.Slug("Flabébé"));
    }

    [Fact]
    public void TheImporterWritesTheSameFileTwiceAndOnlyLegalSets()
    {
        string folder = Path.Combine(Path.GetTempPath(), "facility-sets-" + Guid.NewGuid().ToString("N"));
        try
        {
            foreach (string file in MasterTrainers.SetFiles)
            {
                string path = Path.Combine(folder, file);
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllText(path, "{}");
            }
            File.WriteAllText(Path.Combine(folder, MasterTrainers.SetFiles[0]), """
                {
                  "garchomp": { "level": 74, "sets": [
                    { "role": "Setup Sweeper", "movepool": ["Earthquake", "Swords Dance", "Outrage", "Stone Edge", "Fire Fang"], "abilities": ["Rough Skin"] },
                    { "role": "Fast Support", "movepool": ["Earthquake", "Stealth Rock", "Psychic", "Hydro Pump"], "abilities": ["Sand Veil"] } ] },
                  "bidoof": { "level": 97, "sets": [ { "role": "Bulky Attacker", "movepool": ["Hyper Fang", "Psychic", "Quick Attack", "Return", "Hydro Pump"], "abilities": ["Simple"] } ] },
                  "luxray": { "level": 90, "sets": [ { "role": "Fast Attacker", "movepool": ["Spark", "Crunch", "Psychic", "Hydro Pump"], "abilities": ["Intimidate"] } ] },
                  "dialga": { "level": 70, "sets": [ { "role": "Bulky Attacker", "movepool": ["Draco Meteor", "Fire Blast", "Thunderbolt", "Earth Power"] } ] }
                }
                """);
            // Gen 9's role leaves Bidoof three of its five: gen 8's, which fits whole, is the newest that does
            File.WriteAllText(Path.Combine(folder, MasterTrainers.SetFiles[1]), """
                { "bidoof": { "level": 76, "sets": [ { "role": "Wallbreaker", "movepool": ["Hyper Fang", "Quick Attack", "Return", "Super Fang"] } ] } }
                """);
            string overrides = Path.Combine(folder, "overrides.json");
            File.WriteAllText(overrides, """
                {
                  // a correction and a set the import lacks
                  "bidoof_wallbreaker": { "item": "Sitrus Berry" },
                  "starly_ours": { "new": true, "species": "Starly", "moves": ["Quick Attack", "Wing Attack"], "evFlags": 10, "nature": "Jolly", "item": "Sharp Beak", "ability": "Keen Eye", "tier": 1 }
                }
                """);
            var species = PokemonDatabase.Everyone().ToList();
            var moves = MoveDatabase.GetAll().ToList();
            var items = ItemDatabase.GetAll().ToList();

            var first = FacilitySets.Build(folder, species, moves, items, FrontierDatabase.Banned, overrides);
            var second = FacilitySets.Build(folder, species, moves, items, FrontierDatabase.Banned, overrides);
            Assert.Equal(GameDataFiles.Serialize(first.Data), GameDataFiles.Serialize(second.Data));

            var sets = first.Data.Sets;
            // The added set after its species (Starly, before Bidoof), every set in National order; Garchomp's support role,
            // which leaves it two of four, gives nothing beside a role that fits whole
            Assert.Equal(new[] { "starly_ours", "bidoof_wallbreaker", "luxray_fast_attacker", "garchomp_setup_sweeper" }, sets.Select(s => s.Id));
            var bidoof = sets[1];
            Assert.Equal(new[] { "Hyper Fang", "Quick Attack", "Return", "Super Fang" }, bidoof.Moves.OrderBy(m => m, StringComparer.Ordinal));
            Assert.Equal("Sitrus Berry", bidoof.Item);
            Assert.Equal(4, bidoof.Tier);
            // Gen 8's role names no ability: its species' first
            Assert.Equal(PokemonDatabase.Get("Bidoof")!.Abilities[0], bidoof.Ability);
            var sweeper = sets[3];
            Assert.Equal(4, sweeper.Moves.Count);
            Assert.Equal(4, sweeper.Tier);
            Assert.Equal("Rough Skin", sweeper.Ability);
            // Two of Luxray's four it can't learn: no role fits whole, so it is made up with what its level taught it, still legal
            var luxray = sets[2];
            Assert.DoesNotContain("Psychic", luxray.Moves);
            Assert.Contains("Spark", luxray.Moves);
            Assert.Equal(4, luxray.Moves.Count);
            Assert.Equal(("Intimidate", 1), (luxray.Ability, luxray.Tier));
            Assert.Equal(1, first.MadeUp);
            var learnable = Legality.Learnable(PokemonDatabase.Get("Luxray")!);
            Assert.All(luxray.Moves, m => Assert.Contains(m, learnable));
            // Dialga is banned; the rest named by no file
            Assert.Contains(first.Data.Without, w => w.Species == "Dialga" && w.Why == FacilitySetGapReason.Banned);
            Assert.Contains(first.Data.Without, w => w.Species == "Pikachu" && w.Why == FacilitySetGapReason.NotNamed);
            Assert.DoesNotContain(first.Data.Without, w => species.First(s => s.Name == w.Species).IsOurs);

            // A patch naming a set the import lacks without "new" stops the import
            File.WriteAllText(overrides, """{ "nobody_set": { "item": "Leftovers" } }""");
            Assert.Throws<InvalidDataException>(() => FacilitySets.Build(folder, species, moves, items, FrontierDatabase.Banned, overrides));
            File.WriteAllText(overrides, """{ "bidoof_wallbreaker": { "itme": "Leftovers" } }""");
            Assert.Throws<InvalidDataException>(() => FacilitySets.Build(folder, species, moves, items, FrontierDatabase.Banned, overrides));
        }
        finally
        {
            if (Directory.Exists(folder)) Directory.Delete(folder, recursive: true);
        }
    }

    // ================================================================== the trainers

    private static Func<int> Draws(uint seed)
    {
        var rng = new BattleRandom(seed);
        return rng.Next16;
    }

    [Fact]
    public void ATeamIsTheSameForASeedAndKeepsSpeciesAndItemsApart()
    {
        for (uint seed = 1; seed <= 40; seed++)
        {
            var a = FacilityTrainers.Draw(6, 1, 4, FacilityTrainers.IvsFor(2), Draws(seed));
            var b = FacilityTrainers.Draw(6, 1, 4, FacilityTrainers.IvsFor(2), Draws(seed));
            Assert.Equal(a.Sets, b.Sets);
            Assert.Equal(a.Personalities, b.Personalities);
            Assert.Equal((a.Id, a.Class, a.Name, a.Look, a.OtId), (b.Id, b.Class, b.Name, b.Look, b.OtId));
            Assert.Equal(6, a.Pokemon.Count);
            Assert.Equal(6, a.Pokemon.Select(p => p.Species.Name).Distinct().Count());
            Assert.Equal(6, a.Pokemon.Select(p => p.HeldItem?.Name).Distinct().Count());
            Assert.All(a.Pokemon, p => Assert.False(p.Species.Legendary || p.Species.Mythical || FrontierDatabase.IsBanned(p.Species.Name)));

            // A partner's team drawn clear of the first
            var partner = FacilityTrainers.Draw(3, 1, 4, 20, Draws(seed + 1000), keepClearOf: a.Pokemon);
            Assert.Empty(partner.Pokemon.Select(p => p.Species.Name).Intersect(a.Pokemon.Select(p => p.Species.Name)));
            Assert.Empty(partner.Pokemon.Select(p => p.HeldItem?.Name).Intersect(a.Pokemon.Select(p => p.HeldItem?.Name)));
        }
        // Another seed, another team
        Assert.NotEqual(FacilityTrainers.Draw(3, 1, 4, 12, Draws(1)).Sets, FacilityTrainers.Draw(3, 1, 4, 12, Draws(2)).Sets);
    }

    [Fact]
    public void ATeamComesOfTheTiersAskedWithItsSetsNaturesAbilitiesAndIvs()
    {
        var team = FacilityTrainers.Draw(3, 4, 4, FacilityTrainers.IvsFor(5), Draws(7));
        foreach (var (pokemon, id) in team.Pokemon.Zip(team.Sets))
        {
            var set = FacilitySetDatabase.Get(id)!;
            Assert.Equal(4, set.Tier);
            Assert.Equal(set.Species, pokemon.Species.Name);
            Assert.Equal(set.Nature, pokemon.Nature);
            Assert.Equal(set.Ability, pokemon.AbilityName);
            Assert.Equal(set.Item, pokemon.HeldItem?.Name);
            Assert.Equal(set.Moves, pokemon.Moves.Select(m => m.Name));
            Assert.Equal((31, 31, 50), (pokemon.IvHP, pokemon.IvSpeed, pokemon.Level));
            Assert.False(pokemon.IsShiny);
        }
        Assert.Contains(FacilityTrainers.Trainers, t => t.Class == team.Class && t.Look == team.Look && t.Names.Contains(team.Name));
        Assert.StartsWith("facility_", team.Id);

        // Kept and built again with no draw, the same team
        var again = FacilityTrainers.Rebuild(team, FacilityTrainers.IvsFor(5));
        Assert.Equal(team.Pokemon.Select(p => (p.Species.Name, p.Personality, p.MaxHP, p.Attack)), again.Pokemon.Select(p => (p.Species.Name, p.Personality, p.MaxHP, p.Attack)));

        // The IVs rise with the room's rank
        Assert.Equal(new[] { 12, 16, 20, 24, 28, 31, 31 }, Enumerable.Range(0, 7).Select(FacilityTrainers.IvsFor));

        // As a battle takes the trainer: no prize money, the team as drawn
        var trainer = team.AsTrainer();
        Assert.Equal((team.Class, team.Name, 0, FrontierTrainers.Mind), (trainer.TrainerClass, trainer.Name, trainer.PrizeMoney, trainer.Ai));
        Assert.Equal(3, trainer.Party.Members.Count);
    }

    [Fact]
    public void ATeamOfThreeLaterSpeciesIsDrawnAndBuiltAtFiftyWithEveryMoveLegal()
    {
        // FC1's "done when": Kalos's and Paldea's species among them
        var kalos = PokemonDatabase.Get("Chespin")!.DexNumber;
        var paldea = PokemonDatabase.Get("Sprigatito")!.DexNumber;
        bool Later(FrontierSetRecord s) => PokemonDatabase.Get(s.Species)!.DexNumber >= kalos;
        for (uint seed = 1; seed <= 30; seed++)
        {
            var team = FacilityTrainers.Draw(3, 1, 4, 31, Draws(seed), only: Later);
            Assert.All(team.Pokemon, p =>
            {
                Assert.True(p.Species.DexNumber >= kalos, p.Species.Name);
                Assert.Equal(50, p.Level);
                Assert.Empty(Legality.Unlearnable(p));
                Assert.True(p.MaxHP > 0 && p.CurrentHP == p.MaxHP);
            });
        }
        var bothRegions = Enumerable.Range(1, 60).Select(seed => FacilityTrainers.Draw(3, 1, 4, 31, Draws((uint)seed), only: Later))
            .First(t => t.Pokemon.Any(p => p.Species.DexNumber < paldea) && t.Pokemon.Any(p => p.Species.DexNumber >= paldea));
        Assert.Equal(3, bothRegions.Pokemon.Count);
    }

    [Fact]
    public void TheTrainersFileHasClassesOfTheTablesAndLooksOfTheCast()
    {
        Assert.NotEmpty(FacilityTrainers.Trainers);
        var classes = TrainerDatabase.All.Select(t => t.Class).ToHashSet();
        foreach (var who in FacilityTrainers.Trainers)
        {
            Assert.Contains(who.Class, classes);
            Assert.True(CharacterStyles.Default.Has(who.Look), who.Look);
            Assert.Equal(8, who.Names.Count);
            Assert.Equal(8, who.Names.Distinct().Count());
        }
        // No two trainers of the file share an id
        var ids = FacilityTrainers.Trainers.SelectMany(t => t.Names.Select(n => FacilityTrainers.IdOf(t, n))).ToList();
        Assert.Equal(ids.Count, ids.Distinct().Count());
    }
}
