using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumTests;

/// <summary>
/// The looks the Pokédex has seen (plan 08 · P19): a look is a model's name and a gender, kept per species in the order
/// first seen, recorded by every way a Pokémon is met or had, saved, and rebuilt for a save from before them.
/// It shares the dice, the save folder and the rules with the whole process, so it runs alone.
/// </summary>
[Collection("Shared clocks and dice")]
public class PokedexLooksTests
{
    private static PokemonSpecies Species(string name) => PokemonDatabase.Get(name)!;

    private static Pokemon Mon(string species, Gender gender = Gender.Male, string? form = null, int level = 20)
    {
        var p = Scenario.Mon(species, level, "Tackle");
        p.Gender = gender;
        if (form != null) p.ChangeForm(form);
        return p;
    }

    private static GameEngine Headless()
    {
        var engine = new GameEngine(new GameSettings { Muted = true });
        engine.InitializeHeadless();
        engine.PeopleStayPut = true;
        return engine;
    }

    // ------------------------------------------------------------------ what the log and the Pokédex say

    [Fact]
    public void TheLogsSeenCarriesTheFormAndTheGenderOfWhatIsMet()
    {
        var east = Mon("Shellos", Gender.Female, "Shellos-East");
        var core = CoreScenario.Wild(Scenario.Mon("Bidoof", 20, "Tackle"), east);
        var seen = Assert.Single(core.Log.OfType<Seen>());
        Assert.Equal(("Shellos", "Shellos-East", Gender.Female), (seen.Species.Name, seen.Form, seen.Gender));

        var west = CoreScenario.Wild(Scenario.Mon("Bidoof", 20, "Tackle"), Mon("Shellos", Gender.Male));
        var plain = Assert.Single(west.Log.OfType<Seen>());
        Assert.Equal((null, Gender.Male), (plain.Form, plain.Gender));
    }

    [Fact]
    public void ABattleAgainstAnEastSeaShellosRecordsItsLookAndGender()
    {
        var dex = new Pokedex();
        var party = new Party();
        party.Add(Scenario.Mon("Bidoof", 20, "Tackle"));
        var battle = new BattleEngine(new BattleSetup
        {
            PlayerParty = party, Inventory = new Inventory(), Pokedex = dex, WildPokemon = new List<Pokemon> { Mon("Shellos", Gender.Female, "Shellos-East") },
            Random = Scenario.Steady(), Rules = Ruleset.Platinum
        });
        Scenario.Settle(battle);
        int shellos = Species("Shellos").DexNumber;
        Assert.True(dex.IsSeen(shellos));
        Assert.Equal(new[] { new SeenLook("Shellos-East", Gender.Female) }, dex.LooksOf(shellos));
        // The player's own are not met
        Assert.Empty(dex.LooksOf(Species("Bidoof").DexNumber));
    }

    [Fact]
    public void ABattleAgainstASpindaKeepsItsPersonalityForTheSpots()
    {
        var dex = new Pokedex();
        var party = new Party();
        party.Add(Scenario.Mon("Bidoof", 20, "Tackle"));
        var spinda = Mon("Spinda");
        spinda.Personality = 0x0BADF00D;
        var battle = new BattleEngine(new BattleSetup
        {
            PlayerParty = party, Inventory = new Inventory(), Pokedex = dex, WildPokemon = new List<Pokemon> { spinda },
            Random = Scenario.Steady(), Rules = Ruleset.Platinum
        });
        Scenario.Settle(battle);
        Assert.Equal(0x0BADF00Du, dex.SpindaPersonality);
    }

    [Fact]
    public void ALookIsKeptOnceInTheOrderItWasFirstSeenAndUnownsLettersKeepTheirOrder()
    {
        var dex = new Pokedex();
        foreach (var letter in new[] { "Unown-C", "Unown", "Unown-B", "Unown-C", "Unown" })
            dex.RegisterSeen(Mon("Unown", Gender.Genderless, letter == "Unown" ? null : letter));
        Assert.Equal(new[] { "Unown-C", "Unown", "Unown-B" }, dex.LooksOf(Species("Unown").DexNumber).Select(l => l.Name));

        // Both genders of a species are two looks, and a look seen as each is two cards
        dex.RegisterSeen(Mon("Starly", Gender.Male));
        dex.RegisterSeen(Mon("Starly", Gender.Female));
        dex.RegisterSeen(Mon("Starly", Gender.Male));
        Assert.Equal(new[] { Gender.Male, Gender.Female }, dex.LooksOf(Species("Starly").DexNumber).Select(l => l.Gender));
        Assert.Empty(dex.LooksOf(Species("Bidoof").DexNumber));
    }

    [Fact]
    public void ACatchAGiftAnEvolutionAndATradeEachRecordTheirLook()
    {
        var dex = new Pokedex();
        // A catch and a gift are a Pokémon in hand: seen, and the catch caught
        var caught = Mon("Meowth", Gender.Female, "Meowth-Alola");
        dex.RegisterCaught(caught);
        Assert.True(dex.IsCaught(Species("Meowth").DexNumber));
        Assert.Equal(new[] { new SeenLook("Meowth-Alola", Gender.Female) }, dex.LooksOf(Species("Meowth").DexNumber));

        // An evolution is the evolved Pokémon, and a Shedinja its own
        var persian = Mon("Persian", Gender.Female, "Persian-Alola");
        dex.RegisterCaught(persian);
        dex.RegisterCaught(Mon("Shedinja", Gender.Genderless));
        Assert.Contains(new SeenLook("Persian-Alola", Gender.Female), dex.LooksOf(Species("Persian").DexNumber).Select(l => l));
        Assert.Single(dex.LooksOf(Species("Shedinja").DexNumber));

        // An Egg is seen as its species and has no look yet
        var egg = Mon("Pichu");
        egg.IsEgg = true;
        dex.RegisterSeen(egg);
        Assert.True(dex.IsSeen(Species("Pichu").DexNumber));
        Assert.Empty(dex.LooksOf(Species("Pichu").DexNumber));

        // The first Spinda met gives its spots, and the second doesn't change them
        var first = Mon("Spinda");
        first.Personality = 0x1234ABCD;
        dex.RegisterSeen(first);
        var second = Mon("Spinda");
        second.Personality = 0x77777777;
        dex.RegisterSeen(second);
        Assert.Equal(0x1234ABCDu, dex.SpindaPersonality);
    }

    [Fact]
    public void ATradeAndTheStartersRegisterTheirLooksThroughTheEngine()
    {
        string folder = Path.Combine(Path.GetTempPath(), "pokemon-looks-" + Guid.NewGuid().ToString("N"));
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
            var received = Mon("Shellos", Gender.Female, "Shellos-East");
            Assert.True(engine.ReceiveTradedPokemon(received));
            Assert.Equal(new[] { new SeenLook("Shellos-East", Gender.Female) }, game.Pokedex.LooksOf(Species("Shellos").DexNumber));
            Assert.True(game.Pokedex.IsCaught(Species("Shellos").DexNumber));
        }
        finally
        {
            Dice.Seed(null);
            Ruleset.Use(RulesPreset.Platinum);
            SavePaths.Current = paths;
            try { Directory.Delete(folder, recursive: true); } catch (IOException) { }
        }
    }

    // ------------------------------------------------------------------ saves

    [Fact]
    public void TheLooksRoundTripThroughASaveInTheirOrderWithTheFirstSpinda()
    {
        string folder = Path.Combine(Path.GetTempPath(), "pokemon-looks-" + Guid.NewGuid().ToString("N"));
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
            var dex = game.Pokedex;
            dex.RegisterSeen(Mon("Shellos", Gender.Female, "Shellos-East"));
            dex.RegisterSeen(Mon("Shellos", Gender.Male));
            dex.RegisterSeen(Mon("Unown", Gender.Genderless, "Unown-C"));
            dex.RegisterSeen(Mon("Unown", Gender.Genderless, "Unown-B"));
            var spinda = Mon("Spinda");
            spinda.Personality = 0xCAFE0001;
            dex.RegisterSeen(spinda);
            Assert.True(game.Save());

            var written = SaveManager.Load(game.SaveFile).Save!;
            Assert.Contains(written.SeenLooks, l => l.Name == "Unown-C");
            Assert.Equal(0xCAFE0001u, written.SpindaPersonality);

            var again = Headless();
            again.ContinueGame(game.SaveSlot);
            var loaded = again.Drive.Pokedex;
            foreach (var species in new[] { "Shellos", "Unown", "Spinda" })
                Assert.Equal(dex.LooksOf(Species(species).DexNumber), loaded.LooksOf(Species(species).DexNumber));
            Assert.Equal(new[] { "Shellos-East", "Shellos" }, loaded.LooksOf(Species("Shellos").DexNumber).Select(l => l.Name));
            Assert.Equal(new[] { "Unown-C", "Unown-B" }, loaded.LooksOf(Species("Unown").DexNumber).Select(l => l.Name));
            Assert.Equal(0xCAFE0001u, loaded.SpindaPersonality);
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
    public void ASaveFromBeforeTheLooksRebuildsThemFromItsPokemonAndGivesTheRestTheirOwnForm()
    {
        string folder = Path.Combine(Path.GetTempPath(), "pokemon-looks-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var paths = SavePaths.Current;
        try
        {
            SavePaths.Current = SavePaths.In(folder);
            // A save of the corpus from before looks were kept (plan 24 · X3)
            string old = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "saves", "w4-s19-past-the-opening.json"));
            var data = SaveManager.Read(old)!;
            Assert.Empty(data.SeenLooks);
            Assert.NotEmpty(data.SeenSpecies);
            // One seen in a battle and not had since: Mew, whom nobody of the save's owns
            int mew = Species("Mew").DexNumber;
            Assert.DoesNotContain(mew, data.SeenSpecies);
            data.SeenSpecies.Add(mew);
            File.WriteAllText(SavePaths.Current.Slot(1), SaveManager.Write(data));
            Dice.Seed(3);
            var engine = Headless();
            engine.ContinueGame(1);
            var dex = engine.Drive.Pokedex;

            // Every species seen has a look; the player's own Pokémon give theirs (with their gender), the rest their own form
            Assert.All(dex.SeenSpecies, dexNumber => Assert.NotEmpty(dex.LooksOf(dexNumber)));
            foreach (var mon in engine.Drive.OwnedPokemon)
                Assert.Contains(new SeenLook(mon.ModelName, mon.Gender), dex.LooksOf(mon.Species.DexNumber));
            Assert.Equal(new[] { new SeenLook("Mew", Gender.Genderless) }, dex.LooksOf(mew));
        }
        finally
        {
            Dice.Seed(null);
            Ruleset.Use(RulesPreset.Platinum);
            SavePaths.Current = paths;
            try { Directory.Delete(folder, recursive: true); } catch (IOException) { }
        }
    }

    // ------------------------------------------------------------------ the upgrade

    [Fact]
    public void TheUpgradeIsTheFlagTheAidesSceneSets()
    {
        Assert.Equal("FLAG_ENABLED_POKEDEX_FORM_DETECTION", StoryState.FormDetectionFlag);
        string gate = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Data", "scripts", "Route218GateToCanalaveCity.txt"));
        Assert.Contains($"setflag {StoryState.FormDetectionFlag}", gate);
    }
}
