using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumTests;

/// <summary>
/// A save that survives (plan 12 · Q1): the file written beside a temporary one and moved into place, the backup of
/// the save before, the checksum, what reading says happened, a whole save read back as it was written, and the
/// window's close button. Every file is in a folder of the test's own, never the working directory.
/// </summary>
public class SaveFileTests
{
    /// <summary>A folder of the test's own, deleted afterwards.</summary>
    private sealed class Folder : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pokemon-save-" + Guid.NewGuid().ToString("N"));
        public Folder() => Directory.CreateDirectory(Path);
        public string Save => System.IO.Path.Combine(Path, SaveManager.FileName);
        public void Dispose()
        {
            try { Directory.Delete(Path, recursive: true); }
            catch (IOException) { }
        }
    }

    private static SaveData Named(string name, int money = 3000) => new() { PlayerName = name, Money = money, CurrentMapName = "Sinnoh" };

    // ------------------------------------------------------------------ writing and reading

    [Fact]
    public void NothingSavedReadsAsNone()
    {
        using var folder = new Folder();
        var (outcome, save) = SaveManager.Load(folder.Save);
        Assert.Equal(SaveLoad.None, outcome);
        Assert.Null(save);
    }

    [Fact]
    public void ASaveIsMovedIntoPlaceAndTheOneBeforeKeptAsTheBackup()
    {
        using var folder = new Folder();
        Assert.True(SaveManager.SaveGame(Named("Earlier"), folder.Save));
        Assert.False(File.Exists(SaveManager.BackupOf(folder.Save)), "a first save has nothing to back up");
        Assert.True(SaveManager.SaveGame(Named("Later"), folder.Save));

        Assert.Equal(Path.Combine(folder.Path, "savegame.bak"), SaveManager.BackupOf(folder.Save));
        Assert.False(File.Exists(SaveManager.TempOf(folder.Save)), "the temporary file is moved, not left behind");
        Assert.Equal("Later", SaveManager.Read(File.ReadAllText(folder.Save))!.PlayerName);
        Assert.Equal("Earlier", SaveManager.Read(File.ReadAllText(SaveManager.BackupOf(folder.Save)))!.PlayerName);

        var (outcome, save) = SaveManager.Load(folder.Save);
        Assert.Equal(SaveLoad.Ok, outcome);
        Assert.Equal("Later", save!.PlayerName);
    }

    [Fact]
    public void AFileCutShortLoadsTheBackupAndSaysSo()
    {
        using var folder = new Folder();
        SaveManager.SaveGame(Named("Earlier"), folder.Save);
        SaveManager.SaveGame(Named("Later"), folder.Save);
        string whole = File.ReadAllText(folder.Save);
        File.WriteAllText(folder.Save, whole[..(whole.Length / 2)]);

        var (outcome, save) = SaveManager.Load(folder.Save);
        Assert.Equal(SaveLoad.Recovered, outcome);
        Assert.Equal("Earlier", save!.PlayerName);
    }

    [Fact]
    public void ASaveChangedByHandFailsItsChecksum()
    {
        using var folder = new Folder();
        SaveManager.SaveGame(Named("Earlier", 1111), folder.Save);
        SaveManager.SaveGame(Named("Later", 2222), folder.Save);
        string text = File.ReadAllText(folder.Save);
        Assert.Contains("\"Money\": 2222", text);
        File.WriteAllText(folder.Save, text.Replace("\"Money\": 2222", "\"Money\": 9999"));
        Assert.False(SaveManager.Intact(File.ReadAllText(folder.Save)));

        var (outcome, save) = SaveManager.Load(folder.Save);
        Assert.Equal(SaveLoad.Recovered, outcome);
        Assert.Equal(1111, save!.Money);
    }

    [Fact]
    public void BothDamagedSaysDamaged()
    {
        using var folder = new Folder();
        SaveManager.SaveGame(Named("Earlier"), folder.Save);
        SaveManager.SaveGame(Named("Later"), folder.Save);
        File.WriteAllText(folder.Save, "{ \"PlayerName\": \"La");
        File.WriteAllText(SaveManager.BackupOf(folder.Save), "not a save at all");

        var (outcome, save) = SaveManager.Load(folder.Save);
        Assert.Equal(SaveLoad.Damaged, outcome);
        Assert.Null(save);

        // A save damaged with no backup at all is as lost
        File.Delete(SaveManager.BackupOf(folder.Save));
        Assert.Equal(SaveLoad.Damaged, SaveManager.Load(folder.Save).Outcome);
    }

    [Fact]
    public void ACrashWhileWritingLeavesTheSaveAsItWas()
    {
        using var folder = new Folder();
        SaveManager.SaveGame(Named("Kept"), folder.Save);
        // The game killed halfway through writing the next: the temporary file is cut short, nothing was moved
        string next = SaveManager.Write(Named("Lost"));
        File.WriteAllText(SaveManager.TempOf(folder.Save), next[..(next.Length / 3)]);

        var (outcome, save) = SaveManager.Load(folder.Save);
        Assert.Equal(SaveLoad.Ok, outcome);
        Assert.Equal("Kept", save!.PlayerName);

        // And the next save goes over the temporary file as if nothing had happened
        Assert.True(SaveManager.SaveGame(Named("Next"), folder.Save));
        Assert.Equal("Next", SaveManager.Load(folder.Save).Save!.PlayerName);
        Assert.Equal("Kept", SaveManager.Read(File.ReadAllText(SaveManager.BackupOf(folder.Save)))!.PlayerName);
    }

    [Fact]
    public void ADamagedSaveIsNeverCopiedOverAGoodBackup()
    {
        using var folder = new Folder();
        SaveManager.SaveGame(Named("Earlier"), folder.Save);
        SaveManager.SaveGame(Named("Later"), folder.Save);
        File.WriteAllText(folder.Save, "{ \"PlayerName\"");
        Assert.Equal(SaveLoad.Recovered, SaveManager.Load(folder.Save).Outcome);

        // The game goes on from the backup and saves: the damaged file is replaced, the backup kept
        SaveManager.SaveGame(Named("Again"), folder.Save);
        Assert.Equal("Earlier", SaveManager.Read(File.ReadAllText(SaveManager.BackupOf(folder.Save)))!.PlayerName);
        Assert.Equal(SaveLoad.Ok, SaveManager.Load(folder.Save).Outcome);
    }

    // ------------------------------------------------------------------ the checksum

    [Fact]
    public void TheChecksumIsTheFilesFirstFieldAndTheSameEveryTime()
    {
        var data = Named("Lucas");
        string json = SaveManager.Write(data);
        Assert.StartsWith("{\n  \"Checksum\": \"", json.Replace("\r\n", "\n"));
        Assert.Equal(json, SaveManager.Write(data));
        Assert.Null(data.Checksum);
        Assert.True(SaveManager.Intact(json));
        Assert.NotNull(SaveManager.Read(json)!.Checksum);
        Assert.Matches("^[0-9a-f]{16}$", SaveManager.Read(json)!.Checksum!);

        // Another save has another checksum
        Assert.NotEqual(SaveManager.Read(json)!.Checksum, SaveManager.Read(SaveManager.Write(Named("Dawn")))!.Checksum);
    }

    [Fact]
    public void ASaveChecksTheSameWhateverItsLineEndings()
    {
        string json = SaveManager.Write(Named("Lucas")).Replace("\r\n", "\n");
        Assert.True(SaveManager.Intact(json));
        Assert.True(SaveManager.Intact(json.Replace("\n", "\r\n")));
    }

    [Fact]
    public void AnOldFileWithoutAChecksumLoads()
    {
        using var folder = new Folder();
        // As the game wrote it before plan 12 · Q1: no checksum, straight to the file
        var data = Named("Before", 4321);
        string old = System.Text.Json.JsonSerializer.Serialize(data, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        Assert.DoesNotContain("\"Checksum\": \"", old);
        File.WriteAllText(folder.Save, old);

        var (outcome, save) = SaveManager.Load(folder.Save);
        Assert.Equal(SaveLoad.Ok, outcome);
        Assert.Equal(4321, save!.Money);
        Assert.Null(save.Checksum);
    }

    [Fact]
    public void EverySaveOfTheCorpusLoadsAsItIsRead()
    {
        string corpus = Path.Combine(AppContext.BaseDirectory, "Fixtures", "saves");
        var files = Directory.GetFiles(corpus, "*.json");
        Assert.NotEmpty(files);
        foreach (string file in files)
        {
            using var folder = new Folder();
            string text = File.ReadAllText(file);
            Assert.True(SaveManager.Intact(text), $"{Path.GetFileName(file)} doesn't check");
            File.WriteAllText(folder.Save, text);
            var (outcome, save) = SaveManager.Load(folder.Save);
            Assert.True(outcome == SaveLoad.Ok, $"{Path.GetFileName(file)} loads as {outcome}");
            Assert.Equal(SaveManager.Write(SaveManager.Read(text)!), SaveManager.Write(save!));
        }
    }

    // ------------------------------------------------------------------ a whole save read back

    /// <summary>
    /// What a Pokémon has that its save doesn't keep yet, by name: the battle's own state (a sleep's count, the toxic
    /// count, the stat stages), which a battle sets afresh. Plan 07 · O2's id is kept since O2; whatever a later
    /// session adds to a Pokémon is added here, or to the save, as it comes.
    /// </summary>
    private static readonly string[] NotKept = { nameof(Pokemon.SleepTurns), nameof(Pokemon.ToxicCounter), nameof(Pokemon.StatStages) };

    /// <summary>A Pokémon with everything a save keeps changed from what a new one has.</summary>
    private static Pokemon Changed()
    {
        var p = new Pokemon(PokemonDatabase.Get("Turtwig")!, 30, new Random(5));
        p.Nickname = "Moss";
        p.Gender = Gender.Female;
        p.Nature = Nature.Jolly;
        p.IsShiny = true;
        p.AbilityName = "Shell Armor";
        p.HeldItem = ItemDatabase.Get("Leftovers");
        p.Friendship = 201;
        p.Cool = 3;
        p.Beauty = 12;
        p.Cute = 4;
        p.Smart = 5;
        p.Tough = 6;
        p.Sheen = 77;
        p.Personality = 0xBEEF;
        p.Id = PokemonIds.Make(51234, 17);
        p.Ball = "Dusk Ball";
        p.OriginalTrainer = new TrainerMark("Barry", 51234, PlayerLook.Boy);
        p.Pokerus = 0x13;
        p.Marks = 0b101001;
        p.MetLocation = "Route 201";
        p.MetLevel = 5;
        p.MetDate = new DateTime(2026, 3, 1);
        p.Language = "FRA";
        p.Ribbons.Add("RIBBON_COOL");
        p.CapsuleId = 3;
        p.Capsule = new BallCapsule(new[] { new BallSeal(SealDatabase.Seal("HEART_SEAL_A"), 180, 60) });
        p.EvolutionProgress["steps"] = 9;
        p.IvHP = 31;
        p.IvAttack = 30;
        p.IvDefense = 2;
        p.IvSpAttack = 17;
        p.IvSpDefense = 8;
        p.IvSpeed = 25;
        p.EvHP = 9;
        p.EvAttack = 60;
        p.EvDefense = 1;
        p.EvSpAttack = 4;
        p.EvSpDefense = 100;
        p.EvSpeed = 31;
        p.CurrentExp += 25;
        p.CalculateStats();
        p.CurrentHP = 7;
        p.Status = StatusCondition.Poison;
        p.ReplaceMove(0, "Razor Leaf");
        p.Moves[0].PPUps = 2;
        p.Moves[0].CurrentPP = 3;
        p.Moves[1].CurrentPP = 1;
        // What the save doesn't keep is changed too: it is passed over by name
        p.SleepTurns = 2;
        p.ToxicCounter = 3;
        p.StatStages[StatType.Speed] = -2;
        return p;
    }

    [Fact]
    public void AWholeSaveReadsBackAsItWasWritten()
    {
        using var folder = new Folder();
        var mine = Changed();
        var boxed = new Pokemon(PokemonDatabase.Get("Bidoof")!, 4, new Random(9)) { Nickname = "Bido" };
        var data = new SaveData
        {
            PlayerName = "Dawn",
            Look = PlayerLook.Girl,
            RivalName = "Barry",
            TrainerId = 51234,
            Started = new DateTime(2026, 2, 14),
            Rules = RulesPreset.Modern,
            Money = 12480,
            PlayTimeSeconds = 45296.5f,
            Badges = 0b0000_0111,
            CurrentMapName = "Sinnoh",
            PlayerGridX = 166,
            PlayerGridY = 846,
            PlayerFacing = Direction.Left,
            Travel = TravelMode.Surfing,
            PlayerHeight = 1.5f,
            MapTrail = new[] { 3, 26, 4, 26 },
            LastDay = new DateTime(2026, 3, 2),
            RegisteredItem = "Bicycle",
            RepelSteps = 42,
            Safari = new SafariSave(17, 300, 2),
            WorldVersion = SaveData.CurrentWorld,
            StoryVersion = 18,
            HallOfFameTotal = 1,
            Party = new List<SavedPokemonData> { SavedPokemonData.FromPokemon(mine) },
            Boxes = new SavedBoxes
            {
                CurrentBox = 1,
                Boxes = new List<SavedBox> { new() { Name = "BOX 1", Wallpaper = 2, Pokemon = { new SavedBoxedPokemon { Slot = 5, Pokemon = SavedPokemonData.FromPokemon(boxed) } } } }
            },
            Inventory = new List<SavedItemData> { new() { ItemName = "Potion", Quantity = 4 }, new() { ItemName = "Bicycle", Quantity = 1 } },
            SeenSpecies = new List<int> { 387, 396, 399 },
            CaughtSpecies = new List<int> { 387, 399 },
            NationalPokedex = true,
            Diplomas = new List<string> { "Sinnoh" },
            DefeatedTrainers = new List<string> { "youngster_tristan" },
            StoryFlags = new List<string> { "FLAG_GAME_COMPLETED" },
            StoryVariables = new Dictionary<string, int> { ["VAR_ROAMING_MESPRIT_STATE"] = 2 },
            TakenItems = new List<string> { "route_201_potion" },
            GreetedPeople = new List<string> { "twinleaf_town.mom" },
            ScenesSeen = new List<string> { "RowanLab.The Pokedex", "route_201.The professor" },
            PlayerStarter = "Turtwig",
            RivalStarter = "Chimchar"
        };

        Assert.True(SaveManager.SaveGame(data, folder.Save));
        var (outcome, loaded) = SaveManager.Load(folder.Save);
        Assert.Equal(SaveLoad.Ok, outcome);
        // The whole file is the same written again from what was read
        Assert.Equal(SaveManager.Write(data), SaveManager.Write(loaded!));

        // And the Pokémon made from it is the one saved, everything it has but what the save names as not kept
        AssertSame(mine, loaded!.Party[0].ToPokemon());
        AssertSame(boxed, loaded.Boxes!.Boxes[0].Pokemon[0].Pokemon.ToPokemon());
    }

    [Fact]
    public void WhatTheSaveDoesntKeepIsNamedRight()
    {
        foreach (string name in NotKept)
            Assert.True(typeof(Pokemon).GetProperty(name, BindingFlags.Public | BindingFlags.Instance) != null, $"Pokemon has no {name}: take it off the list");
    }

    private static void AssertSame(Pokemon a, Pokemon b)
    {
        foreach (var property in typeof(Pokemon).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (NotKept.Contains(property.Name) || property.GetIndexParameters().Length > 0) continue;
            object? x = property.GetValue(a), y = property.GetValue(b);
            switch (x)
            {
                case List<Move> moves:
                    Assert.Equal(moves.Select(m => (m.Name, m.CurrentPP, m.PPUps)), ((List<Move>)y!).Select(m => (m.Name, m.CurrentPP, m.PPUps)));
                    break;
                case ISet<string> set:
                    Assert.True(set.SetEquals((ISet<string>)y!), $"{property.Name}: {string.Join(", ", set)} and {string.Join(", ", (ISet<string>)y!)}");
                    break;
                case IDictionary dictionary:
                    Assert.Equal(Entries(dictionary), Entries((IDictionary)y!));
                    break;
                default:
                    Assert.True(Equals(x, y), $"{property.Name}: {x} and {y}");
                    break;
            }
        }

        static List<string> Entries(IDictionary dictionary)
        {
            var entries = new List<string>();
            var each = dictionary.GetEnumerator();
            while (each.MoveNext()) entries.Add($"{each.Key} = {each.Value}");
            entries.Sort(StringComparer.Ordinal);
            return entries;
        }
    }

    // ------------------------------------------------------------------ the window's close button

    [Fact]
    public void TheCloseButtonOffersToSaveWhereTheGameCanBeSaved()
    {
        var prompt = new ClosePrompt();
        prompt.Open(canSave: true);
        Assert.True(prompt.IsOpen);
        Assert.Equal(new[] { CloseChoice.KeepPlaying, CloseChoice.SaveAndQuit, CloseChoice.Quit }, prompt.Answers);
        Assert.Equal(StartMenu.QuitAnswers, prompt.Answers.Select(ClosePrompt.Label));
        Assert.Equal(0, prompt.Index);
        prompt.Move(1);
        Assert.Equal(CloseChoice.SaveAndQuit, prompt.Confirm());
        Assert.False(prompt.IsOpen);
        Assert.Equal(CloseChoice.None, prompt.Confirm());

        prompt.Open(canSave: true);
        Assert.Equal(0, prompt.Index);
        prompt.Move(5);
        Assert.Equal(CloseChoice.Quit, prompt.Confirm());

        prompt.Open(canSave: true);
        Assert.Equal(CloseChoice.KeepPlaying, prompt.Confirm());
    }

    [Fact]
    public void WhereTheGameCantBeSavedTheCloseButtonOffersOnlyToQuitOrStay()
    {
        var prompt = new ClosePrompt();
        prompt.Open(canSave: false);
        Assert.Equal(new[] { CloseChoice.KeepPlaying, CloseChoice.Quit }, prompt.Answers);
        prompt.Move(-1);
        Assert.Equal(0, prompt.Index);
        prompt.Move(1);
        prompt.Move(1);
        Assert.Equal(1, prompt.Index);
        Assert.Equal(CloseChoice.Quit, prompt.Confirm());

        // B keeps playing, wherever the cursor is
        prompt.Open(canSave: false);
        prompt.Move(1);
        Assert.Equal(CloseChoice.KeepPlaying, prompt.Cancel());
        Assert.False(prompt.IsOpen);
    }

    [Theory]
    [InlineData(false, GameState.Title, true)]
    [InlineData(false, GameState.Options, true)]
    [InlineData(false, GameState.Intro, false)]
    [InlineData(true, GameState.Overworld, false)]
    [InlineData(true, GameState.Battle, false)]
    [InlineData(true, GameState.Options, false)]
    [InlineData(true, GameState.Title, false)]
    public void TheCloseButtonEndsTheGameAtOnceOnlyWhereNothingCanBeLost(bool gameBegun, GameState state, bool atOnce)
    {
        Assert.Equal(atOnce, ClosePrompt.QuitsAtOnce(gameBegun, state));
    }
}
