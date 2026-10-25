using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Story;
using SaveTool;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 12 · Q3: the save tool (tools/SaveTool). Its checks over saves made here (a species that does not exist, an IV
/// of 99, a bag item the data lacks, a hand-edited file), over every save of the corpus (none has an error), its
/// migration of an old save and its diff. It reads the databases of the process, so it runs with the others that do.
/// </summary>
[Collection("Shared clocks and dice")]
public class SaveToolTests
{
    private static SaveData Good()
    {
        var save = new SaveData
        {
            PlayerName = "Lucas", TrainerId = 4321, Started = new DateTime(2026, 6, 1), PlayTimeSeconds = 600, Money = 3000,
            CurrentMapName = "Sinnoh", PlayerGridX = 116, PlayerGridY = 886,
            WorldVersion = SaveData.CurrentWorld, StoryVersion = StoryState.CurrentVersion, SaveVersion = SaveData.CurrentSave,
            Inventory = { new SavedItemData { ItemName = "Potion", Quantity = 3 } },
            SeenSpecies = { 387 }, CaughtSpecies = { 387 }
        };
        save.Party.Add(new SavedPokemonData
        {
            SpeciesName = "Turtwig", DexNumber = 387, Level = 12, CurrentHP = 20,
            Moves = { new SavedMoveData { MoveName = "Tackle", CurrentPP = 30 } }
        });
        return save;
    }

    private static List<SaveFinding> Errors(SaveData save, string? text = null) =>
        SaveChecks.Validate(save, text).Where(f => f.Severity == Severity.Error).ToList();

    [Fact]
    public void ASaveTheDataHoldsIsFoundWhole()
    {
        MapDatabase.Initialize();
        Assert.Empty(Errors(Good()));
    }

    [Fact]
    public void ASaveWithASpeciesThatDoesntExistIsReported()
    {
        MapDatabase.Initialize();
        var save = Good();
        save.Party[0].SpeciesName = "Turtwiggy";
        save.Party[0].DexNumber = 0;
        var finding = Assert.Single(Errors(save), f => f.Where.StartsWith("team #1"));
        Assert.Contains("no species 'Turtwiggy'", finding.Problem);

        // With its number still right, the report names what the game would make of it
        save.Party[0].DexNumber = 387;
        Assert.Contains("number 387 is Turtwig", Assert.Single(Errors(save)).Problem);
    }

    [Fact]
    public void WhatAHandCanGetWrongIsNamed()
    {
        MapDatabase.Initialize();
        var save = Good();
        var p = save.Party[0];
        p.IvSpeed = 99;
        p.Level = 300;
        p.EvAttack = 252;
        p.EvDefense = 252;
        p.EvSpeed = 252;
        p.HeldItem = "Wonder Potato";
        p.Ability = "Being Great";
        p.Moves.Add(new SavedMoveData { MoveName = "Super Punch Of Doom", CurrentPP = 5 });
        p.Moves.Add(new SavedMoveData { MoveName = "Growl", CurrentPP = 99 });
        save.Inventory.Add(new SavedItemData { ItemName = "Mystery Egg", Quantity = 1 });
        save.Inventory.Add(new SavedItemData { ItemName = "Potion", Quantity = 5000 });
        save.Money = 5_000_000;
        save.CaughtSpecies.Add(9999);
        save.CurrentMapName = "Nowhere";

        string all = string.Join("\n", Errors(save).Select(f => f.ToString()));
        Assert.Contains("Speed IV 99 is outside 0 to 31", all);
        Assert.Contains("level 300 is outside 1 to 260", all);
        Assert.Contains("effort adds up to 756", all);
        Assert.Contains("there is no item 'Wonder Potato' to hold", all);
        Assert.Contains("there is no ability 'Being Great'", all);
        Assert.Contains("there is no move 'Super Punch Of Doom'", all);
        Assert.Contains("Growl has 99 PP of at most", all);
        Assert.Contains("there is no item 'Mystery Egg'", all);
        Assert.Contains("Potion x5000", all);
        Assert.Contains("money 5000000", all);
        Assert.Contains("9999 is no number", all);
        Assert.Contains("Nowhere is no map of this game", all);
    }

    [Fact]
    public void AFileEditedAfterTheGameWroteItIsFoundByItsChecksum()
    {
        MapDatabase.Initialize();
        string written = SaveManager.Write(Good());
        Assert.Empty(Errors(SaveManager.Read(written)!, written));
        // The same file with a number changed by hand, the checksum left as it was
        string edited = written.Replace("\"Money\": 3000", "\"Money\": 999999");
        Assert.NotEqual(written, edited);
        var finding = Assert.Single(Errors(SaveManager.Read(edited)!, edited));
        Assert.Equal("file", finding.Where);
        Assert.Contains("checksum", finding.Problem);
    }

    [Fact]
    public void ABoxedPokemonAndATeamOfSevenAreFound()
    {
        MapDatabase.Initialize();
        var save = Good();
        for (int i = 0; i < 6; i++) save.Party.Add(new SavedPokemonData { SpeciesName = "Starly", DexNumber = 396, Level = 5, CurrentHP = 10, Moves = { new SavedMoveData { MoveName = "Tackle", CurrentPP = 35 } } });
        save.Boxes = new SavedBoxes();
        for (int b = 0; b < 18; b++) save.Boxes.Boxes.Add(new SavedBox { Name = $"BOX {b + 1}" });
        save.Boxes.Boxes[0].Pokemon.Add(new SavedBoxedPokemon { Slot = 4, Pokemon = new SavedPokemonData { SpeciesName = "Bidoof", DexNumber = 399, Level = 3, CurrentHP = 9, Moves = { new SavedMoveData { MoveName = "Tackle", CurrentPP = 35 } } } });
        save.Boxes.Boxes[0].Pokemon.Add(new SavedBoxedPokemon { Slot = 4, Pokemon = new SavedPokemonData { SpeciesName = "Bidoof", DexNumber = 399, Level = 3, CurrentHP = 9, Moves = { new SavedMoveData { MoveName = "Tackle", CurrentPP = 35 } } } });
        string all = string.Join("\n", Errors(save).Select(f => f.ToString()));
        Assert.Contains("7 Pokémon on a team of at most 6", all);
        Assert.Contains("box 1: place 5 holds two Pokémon", all);
    }

    [Fact]
    public void EverySaveOfTheCorpusIsFoundWithoutAnError()
    {
        MapDatabase.Initialize();
        string folder = Path.Combine(AppContext.BaseDirectory, "Fixtures", "saves");
        var files = Directory.GetFiles(folder, "*.json");
        Assert.NotEmpty(files);
        foreach (string file in files)
        {
            string text = File.ReadAllText(file);
            var errors = Errors(SaveManager.Read(text)!, text);
            Assert.True(errors.Count == 0, $"{Path.GetFileName(file)}: {string.Join("; ", errors.Select(e => e.ToString()))}");
        }
    }

    [Fact]
    public void MigratingASaveOfTodayChangesNothingAndAnOldOneDoes()
    {
        MapDatabase.Initialize();
        var today = SaveMigrate.Run(Good());
        Assert.True(today.Unchanged, string.Join("; ", today.Changes));

        // The first save of the corpus is the oldest: it comes up to date, the original left as it was
        string folder = Path.Combine(AppContext.BaseDirectory, "Fixtures", "saves");
        string oldest = Directory.GetFiles(folder, "w0-*.json").Order(StringComparer.Ordinal).First();
        var old = SaveManager.Read(File.ReadAllText(oldest))!;
        int storyVersion = old.StoryVersion;
        var result = SaveMigrate.Run(old);
        Assert.False(result.Unchanged);
        Assert.Equal(storyVersion, old.StoryVersion);
        Assert.Equal(StoryState.CurrentVersion, result.Migrated.StoryVersion);
        Assert.Equal(SaveData.CurrentWorld, result.Migrated.WorldVersion);
        Assert.Equal(SaveData.CurrentSave, result.Migrated.SaveVersion);
        Assert.Contains(result.Changes, c => c.StartsWith("story version"));
        // And what it wrote is a save the checks hold whole, that migrates to nothing more
        string text = SaveManager.Write(result.Migrated);
        Assert.Empty(Errors(SaveManager.Read(text)!, text));
        Assert.True(SaveMigrate.Run(SaveManager.Read(text)!).Unchanged);
    }

    [Fact]
    public void TwoSavesDifferWhereTheyDifferAndNowhereElse()
    {
        var a = Good();
        var b = Good();
        string first = SaveManager.Write(a);
        Assert.Empty(SaveDiff.Compare(first, SaveManager.Write(b)));

        b.Money = 100;
        b.Party[0].Level = 13;
        b.StoryFlags.Add("FLAG_X");
        b.Inventory.Clear();
        var lines = SaveDiff.Compare(first, SaveManager.Write(b));
        Assert.Contains("~ Money: 3000 -> 100", lines);
        Assert.Contains("~ Party[0].Level: 12 -> 13", lines);
        Assert.Contains("+ StoryFlags[FLAG_X]: present", lines);
        Assert.Contains(lines, l => l.StartsWith("- Inventory[0]"));
        // The checksum is no difference of its own
        Assert.DoesNotContain(lines, l => l.Contains("Checksum"));
        // A flag listed in another order is the same set
        a.StoryFlags.AddRange(new[] { "FLAG_A", "FLAG_B" });
        b = Good();
        b.StoryFlags.AddRange(new[] { "FLAG_B", "FLAG_A" });
        Assert.Empty(SaveDiff.Compare(SaveManager.Write(a), SaveManager.Write(b)));
    }

    [Fact]
    public void ADumpTellsWhoAndWhatTheySave()
    {
        var text = SaveDump.Describe(Good());
        Assert.Contains("Lucas", text);
        Assert.Contains("Turtwig L12", text);
        Assert.Contains("Potion x3", text);
    }
}
