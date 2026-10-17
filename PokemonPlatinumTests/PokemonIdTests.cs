using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.Frontier;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 07 · O2: the Pokémon's own id and the save's version. An id is made of the trainer's number and a count, so it
/// is the same every run and moves no seeded draw; it is unique among the player's Pokémon, kept through everything
/// that is still the same Pokémon (a battle's copies, an evolution, a hatching, Transform, a save and a load, a trade)
/// and not given to what is another (the Battle Frontier's copies); a save from before is given ids as it loads, the
/// same ones each time.
/// </summary>
public class PokemonIdTests
{
    private static Pokemon Mon(string species, int level = 10, int seed = 1) => new(PokemonDatabase.Get(species)!, level, new Random(seed));

    // ------------------------------------------------------------------ the ids themselves

    [Fact]
    public void AnIdIsTheTrainersNumberAndACount()
    {
        var id = PokemonIds.Make(0xDEADBEEF, 7);
        Assert.Equal("deadbeef-504b-8001-8000-000000000007", id.ToString());
        Assert.Equal(((uint)0xDEADBEEF, 7u), PokemonIds.Parts(id)!.Value);
        // RFC 9562: version 8, variant 10
        Assert.Equal('8', id.ToString()[14]);
        Assert.Equal('8', id.ToString()[19]);
        Assert.NotEqual(PokemonIds.Make(1, 7), PokemonIds.Make(2, 7));
        Assert.NotEqual(PokemonIds.Make(1, 7), PokemonIds.Make(1, 8));
        Assert.Equal(PokemonIds.Make(1, uint.MaxValue), PokemonIds.Make(1, uint.MaxValue));
        Assert.Null(PokemonIds.Parts(Guid.Empty));
        Assert.Null(PokemonIds.Parts(Guid.Parse("01234567-89ab-cdef-0123-456789abcdef")));
    }

    [Fact]
    public void IdsAreGivenToThoseWithNoneAndToASecondHolderOfOne()
    {
        var a = Mon("Turtwig");
        var b = Mon("Bidoof");
        var c = Mon("Starly");
        var d = Mon("Shinx");
        // c already holds the count's next id; d holds a's would-be id twice over with b
        c.Id = PokemonIds.Make(5, 0);
        b.Id = PokemonIds.Make(9, 3);
        d.Id = b.Id;
        uint next = 0;
        int given = PokemonIds.Give(new[] { a, b, c, d }, 5, ref next);

        Assert.Equal(2, given);
        Assert.Equal(PokemonIds.Make(9, 3), b.Id);  // the first holder keeps it
        Assert.Equal(PokemonIds.Make(5, 0), c.Id);  // and so does one that held an id of the count already
        Assert.Equal(PokemonIds.Make(5, 1), a.Id);  // passed over: 0 was held
        Assert.Equal(PokemonIds.Make(5, 2), d.Id);
        Assert.Equal(3u, next);
        Assert.Equal(4, new[] { a, b, c, d }.Select(p => p.Id).Distinct().Count());

        // Given again, nothing changes
        Assert.Equal(0, PokemonIds.Give(new[] { a, b, c, d }, 5, ref next));
        Assert.Equal(3u, next);
    }

    // ------------------------------------------------------------------ the same Pokémon keeps its id

    [Fact]
    public void ABattlesCopiesAreTheSamePokemon()
    {
        var p = Mon("Turtwig", 20);
        p.Id = PokemonIds.Make(42, 1);
        Assert.Equal(p.Id, p.Clone().Id);

        var mirror = new BattleMirror();
        var copy = mirror.Copy(p);
        Assert.NotSame(p, copy);
        Assert.Equal(p.Id, copy.Id);

        // What the rules did is taken over, the id with it, and a Pokémon taking another's state takes its id
        var other = Mon("Bidoof");
        other.CopyStateFrom(p);
        Assert.Equal(p.Id, other.Id);
    }

    [Fact]
    public void TransformLeavesTheIdAsItWas()
    {
        var ditto = CoreScenario.Of(Gender.Male, "Ditto", 30, "Transform");
        ditto.Id = PokemonIds.Make(42, 2);
        var foe = CoreScenario.Of(Gender.Male, "Bidoof", 30, "Growl");
        var core = CoreScenario.Wild(ditto, foe);
        var said = CoreScenario.Turn(core);
        Assert.Contains(said, l => l.Contains("transform", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(PokemonIds.Make(42, 2), ditto.Id);
        Assert.Equal(Guid.Empty, foe.Id);
    }

    [Fact]
    public void AnEvolutionAndAHatchingAreTheSamePokemon()
    {
        var p = Mon("Turtwig", 18);
        p.Id = PokemonIds.Make(42, 3);
        p.EvolveInto(PokemonDatabase.Get("Grotle")!);
        Assert.Equal(PokemonIds.Make(42, 3), p.Id);

        var egg = Mon("Happiny", 1);
        egg.IsEgg = true;
        egg.Id = PokemonIds.Make(42, 4);
        Breeding.Hatch(egg, "Solaceon Town", new DateTime(2026, 6, 1));
        Assert.False(egg.IsEgg);
        Assert.Equal(PokemonIds.Make(42, 4), egg.Id);
    }

    [Fact]
    public void TheBattleFrontiersCopiesAreNobodys()
    {
        var p = Mon("Garchomp", 70);
        p.Id = PokemonIds.Make(42, 5);
        var copy = FrontierRules.ForBattle(p);
        Assert.Equal(Guid.Empty, copy.Id);
        Assert.Equal(PokemonIds.Make(42, 5), p.Id);
        var party = new Party();
        party.Add(p);
        Assert.All(FrontierRules.TeamFor(party, new[] { 0 }).Members, m => Assert.Equal(Guid.Empty, m.Id));
    }

    [Fact]
    public void ASavedPokemonKeepsItsIdAndOneWithNoneSavesNone()
    {
        var p = Mon("Shinx");
        p.Id = PokemonIds.Make(42, 6);
        var saved = SavedPokemonData.FromPokemon(p);
        Assert.Equal(p.Id, saved.Id);
        Assert.Equal(p.Id, saved.ToPokemon().Id);

        var nobodys = SavedPokemonData.FromPokemon(Mon("Starly"));
        Assert.Null(nobodys.Id);
        Assert.Equal(Guid.Empty, nobodys.ToPokemon().Id);
        Assert.DoesNotContain("\"Id\"", System.Text.Json.JsonSerializer.Serialize(nobodys));
    }

    // ------------------------------------------------------------------ the save's version

    /// <summary>A save of version 0, as every save from before plan 07 · O2 reads: Pokémon with no ids.</summary>
    private static string Legacy()
    {
        var save = new SaveData
        {
            TrainerId = 51234, CurrentMapName = "Sinnoh", WorldVersion = SaveData.CurrentWorld, StoryVersion = 19,
            Party = { SavedPokemonData.FromPokemon(Mon("Turtwig")), SavedPokemonData.FromPokemon(Mon("Bidoof")) },
            Boxes = new SavedBoxes { Boxes = { new SavedBox { Pokemon = { new SavedBoxedPokemon { Slot = 4, Pokemon = SavedPokemonData.FromPokemon(Mon("Shinx")) }, new SavedBoxedPokemon { Slot = 1, Pokemon = SavedPokemonData.FromPokemon(Mon("Starly")) } } } } },
            DayCare = new DayCareSave { Places = { new SavedDayCarePlace { Pokemon = SavedPokemonData.FromPokemon(Mon("Pachirisu")) }, null } }
        };
        string json = SaveManager.Write(save);
        // Written today it says the version it is; a save from before had none at all
        return json.Replace($"\"SaveVersion\": {SaveData.CurrentSave},", "").Replace("\"PokemonIdsIssued\": 0,", "");
    }

    [Fact]
    public void ASaveFromBeforeIsGivenIdsTheSameEachTime()
    {
        var first = SaveManager.Read(Legacy())!;
        var again = SaveManager.Read(Legacy())!;
        Assert.Equal(0, first.SaveVersion);
        Assert.All(SaveMigration.Owned(first), p => Assert.Null(p.Id));

        Assert.Equal(5, SaveMigration.Upgrade(first));
        Assert.Equal(5, SaveMigration.Upgrade(again));
        Assert.Equal(SaveData.CurrentSave, first.SaveVersion);
        Assert.Equal(5u, first.PokemonIdsIssued);

        var ids = SaveMigration.Owned(first).Select(p => p.Id!.Value).ToList();
        Assert.Equal(ids, SaveMigration.Owned(again).Select(p => p.Id!.Value));
        Assert.Equal(5, ids.Distinct().Count());
        // In the order the save keeps them: the team, the PC by place, the Day Care; the trainer's number is the card's
        uint number = SaveMigration.TrainerNumberOf(first);
        Assert.Equal(Enumerable.Range(0, 5).Select(i => PokemonIds.Make(number, (uint)i)), ids);
        Assert.Equal("Starly", SaveMigration.Owned(first).ElementAt(2).SpeciesName);

        // Brought up to date, it is left as it is
        Assert.Equal(0, SaveMigration.Upgrade(first));
        Assert.Equal(ids, SaveMigration.Owned(first).Select(p => p.Id!.Value));
    }

    [Fact]
    public void ASaveOfTodayIsLeftAsItIs()
    {
        var save = SaveManager.Read(Legacy())!;
        SaveMigration.Upgrade(save);
        string written = SaveManager.Write(save);
        var read = SaveManager.Read(written)!;
        Assert.Equal(0, SaveMigration.Upgrade(read));
        Assert.Equal(written, SaveManager.Write(read));
    }

    [Fact]
    public void TheLinkBattlesRecordIsKeptByFriendCode()
    {
        var record = new LinkBattleRecord();
        Assert.True(record.IsEmpty);
        Assert.Null(record.ToSave());
        record.Record("1234-5678-9012", LinkOutcome.Win);
        record.Record("1234-5678-9012", LinkOutcome.Win);
        record.Record("1234-5678-9012", LinkOutcome.Loss);
        record.Record("2222-3333-4444", LinkOutcome.Draw);
        Assert.Equal((2, 1, 1), (record.Wins, record.Losses, record.Draws));
        Assert.Equal(2, record.With("1234-5678-9012").Wins);
        Assert.Equal(0, record.With("9999-9999-9999").Wins);
        Assert.Throws<ArgumentException>(() => record.Record(" ", LinkOutcome.Win));

        var save = new SaveData { LinkBattles = record.ToSave() };
        var back = LinkBattleRecord.From(SaveManager.Read(SaveManager.Write(save))!.LinkBattles);
        Assert.Equal((2, 1, 1), (back.Wins, back.Losses, back.Draws));
        Assert.DoesNotContain("LinkBattles", SaveManager.Write(new SaveData()));
    }
}

/// <summary>
/// Plan 07 · O2 in a whole game with no window: the ids a new game gives, a save and a continue that keep them, a save
/// from before given them as it loads and the same ones again, and a trade that would bring back an id already held.
/// It shares the dice, the save folder and the rules with the whole process, so it runs alone.
/// </summary>
[Collection("Shared clocks and dice")]
public class PokemonIdGameTests
{
    [Fact]
    public void IdsAreUniqueAndOutliveASaveALoadAndATrade()
    {
        string folder = Path.Combine(Path.GetTempPath(), "pokemon-ids-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var paths = SavePaths.Current;
        try
        {
            SavePaths.Current = SavePaths.In(folder);
            Dice.Seed(7);

            var engine = Headless();
            var game = engine.Drive;
            engine.NewGameRegion = "Sinnoh";
            engine.StartNewGame();
            Assert.Equal(0u, game.PokemonIdsIssued);

            game.Party.Add(new Pokemon(PokemonDatabase.Get("Turtwig")!, 12));
            game.Party.Add(new Pokemon(PokemonDatabase.Get("Starly")!, 8));
            game.Boxes.Store(new Pokemon(PokemonDatabase.Get("Bidoof")!, 4));
            Assert.All(game.OwnedPokemon, p => Assert.Equal(Guid.Empty, p.Id));

            // Saved: every Pokémon of the player's has an id of the trainer's, each its own
            Assert.True(game.Save());
            var ids = game.OwnedPokemon.Select(p => p.Id).ToList();
            Assert.Equal(3, ids.Distinct().Count());
            Assert.DoesNotContain(Guid.Empty, ids);
            Assert.Equal(3u, game.PokemonIdsIssued);
            var written = SaveManager.Load(game.SaveFile).Save!;
            Assert.Equal(SaveData.CurrentSave, written.SaveVersion);
            Assert.Equal(ids, SaveMigration.Owned(written).Select(p => p.Id!.Value));

            // Saved again, nothing moves; a Pokémon caught since takes the next count
            game.Party.Add(new Pokemon(PokemonDatabase.Get("Shinx")!, 6));
            Assert.True(game.Save());
            Assert.Equal(ids, game.OwnedPokemon.Take(2).Concat(game.OwnedPokemon.Skip(3)).Select(p => p.Id));
            var shinx = game.Party.Members[2];
            Assert.Equal(PokemonIds.Make(PokemonIds.Parts(ids[0])!.Value.Trainer, 3), shinx.Id);

            // Continued: the same Pokémon, the same ids, and the count goes on from where it was
            int slot = game.SaveSlot;
            var again = Headless();
            again.ContinueGame(slot);
            var loaded = again.Drive.OwnedPokemon.Select(p => p.Id).ToList();
            Assert.Equal(game.OwnedPokemon.Select(p => p.Id), loaded);
            Assert.Equal(4u, again.Drive.PokemonIdsIssued);

            // A trade that would bring back an id the player holds is refused, and nothing changes; one with none is given the next
            var copyOfShinx = again.Drive.Party.Members[2].Clone();
            int count = again.Drive.Party.Count;
            Assert.False(again.ReceiveTradedPokemon(copyOfShinx));
            Assert.Equal(count, again.Drive.Party.Count);
            var stranger = new Pokemon(PokemonDatabase.Get("Buizel")!, 15);
            Assert.True(again.ReceiveTradedPokemon(stranger));
            Assert.Equal(PokemonIds.Make(PokemonIds.Parts(ids[0])!.Value.Trainer, 4), stranger.Id);
            // A friend's Pokémon keeps the id it came with
            var friends = new Pokemon(PokemonDatabase.Get("Pikachu")!, 15) { Id = PokemonIds.Make(77, 9) };
            Assert.True(again.ReceiveTradedPokemon(friends));
            Assert.Equal(PokemonIds.Make(77, 9), friends.Id);
            Assert.Equal(again.Drive.OwnedPokemon.Count(), again.Drive.OwnedPokemon.Select(p => p.Id).Distinct().Count());
        }
        finally
        {
            Dice.Seed(null);
            Ruleset.Use(RulesPreset.Platinum);
            SavePaths.Current = paths;
            try { Directory.Delete(folder, recursive: true); } catch (IOException) { }
        }
    }

    [Fact]
    public void ASaveFromBeforeLoadsWithTheSameIdsEachTime()
    {
        string folder = Path.Combine(Path.GetTempPath(), "pokemon-ids-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var paths = SavePaths.Current;
        try
        {
            SavePaths.Current = SavePaths.In(folder);
            // A save of the corpus from before ids (plan 24 · X3), its team, boxes and Day Care
            string old = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "saves", "w4-s19-past-the-opening.json"));
            Assert.Equal(0, SaveManager.Read(old)!.SaveVersion);
            File.WriteAllText(SavePaths.Current.Slot(1), old);

            List<Guid> Loaded()
            {
                Dice.Seed(3);
                var engine = Headless();
                engine.ContinueGame(1);
                return engine.Drive.OwnedPokemon.Select(p => p.Id).ToList();
            }
            var first = Loaded();
            Assert.NotEmpty(first);
            Assert.DoesNotContain(Guid.Empty, first);
            Assert.Equal(first.Count, first.Distinct().Count());
            Assert.Equal(first, Loaded());
        }
        finally
        {
            Dice.Seed(null);
            Ruleset.Use(RulesPreset.Platinum);
            SavePaths.Current = paths;
            try { Directory.Delete(folder, recursive: true); } catch (IOException) { }
        }
    }

    private static GameEngine Headless()
    {
        var engine = new GameEngine(new GameSettings { Muted = true });
        engine.InitializeHeadless();
        engine.PeopleStayPut = true;
        return engine;
    }
}
