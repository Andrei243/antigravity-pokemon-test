using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Core;

/// <summary>A Safari Game as a save keeps it: the balls and the steps left.</summary>
public sealed record SafariSave(int Balls, int Steps);

public class SaveData
{
    /// <summary>The <see cref="WorldVersion"/> from which Sinnoh's overworld is one map made from the imported world (plan 01 · M2).</summary>
    public const int ImportedWorld = 1;

    /// <summary>The <see cref="WorldVersion"/> from which Jubilife City is part of that map and no longer one of its own (plan 01 · M5).</summary>
    public const int ImportedJubilife = 2;

    /// <summary>The <see cref="WorldVersion"/> of saves made today.</summary>
    public const int CurrentWorld = ImportedJubilife;

    public string PlayerName { get; set; } = "Lucas";

    /// <summary>Which of the two characters the player is. Saves from before the choice existed are the boy.</summary>
    public PlayerLook Look { get; set; }

    /// <summary>The rival's name, given in the introduction (plan 02 · S4). Older saves call him by his own.</summary>
    public string RivalName { get; set; } = PlayerIdentity.DefaultRivalName;

    /// <summary>
    /// What the player wears (plan 11 · C9): a garment of <c>clothes.json</c> in each slot or the look's own. Null in
    /// older saves and where nothing was ever changed: the look's own clothes.
    /// </summary>
    public Outfit? Outfit { get; set; }

    /// <summary>The garments bought at a boutique, by id. Null in older saves: nothing bought.</summary>
    public List<string>? Wardrobe { get; set; }

    /// <summary>The number on the Trainer Card, drawn when the game began. 0 in older saves: one is drawn on loading.</summary>
    public int TrainerId { get; set; }

    /// <summary>The day the adventure began; null in saves from before it was recorded.</summary>
    public DateTime? Started { get; set; }

    /// <summary>
    /// Whose rules the adventure is played by, chosen as it began and never changed after. Saves from before the
    /// choice existed are Platinum's.
    /// </summary>
    [System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter))]
    public RulesPreset Rules { get; set; }

    public int Money { get; set; } = 3000;
    public float PlayTimeSeconds { get; set; } = 0;
    public int Badges { get; set; } = 0;
    public string CurrentMapName { get; set; } = "Sinnoh";
    public int PlayerGridX { get; set; } = 116;
    public int PlayerGridY { get; set; } = 886;
    public Direction PlayerFacing { get; set; } = Direction.Down;

    /// <summary>How the player was getting about: a save made while surfing loads out on the water.</summary>
    public PokemonPlatinumEngine.Overworld.TravelMode Travel { get; set; }

    /// <summary>
    /// The height the player stood at, which says which level they were on where a tile has two (a bridge and
    /// the water under it). Null in older saves: the player stands on the ground.
    /// </summary>
    public float? PlayerHeight { get; set; }

    /// <summary>
    /// Where Dig and an Escape Rope lead out of the caves: outside the way the player last went into them (plan 02
    /// · S2). Null in older saves, and outside the caves.
    /// </summary>
    public MapSpot? Exit { get; set; }

    /// <summary>The day the clock was last looked at, for Pokérus's days (plan 06 · R10). Null in older saves.</summary>
    public DateTime? LastDay { get; set; }

    /// <summary>The key item kept on the item button (the original's registered item), by name; null when none is.</summary>
    public string? RegisteredItem { get; set; }

    /// <summary>
    /// Whoever travels with the player (plan 02 · S6): the trainer of Platinum's data who battles beside them, and
    /// the person of the map who walks behind, by their key. Null when nobody does, and in older saves.
    /// </summary>
    public string? Partner { get; set; }
    public string? PartnerPerson { get; set; }

    /// <summary>The steps the Repel has left (plan 06 · R11); a save from before has none.</summary>
    public int RepelSteps { get; set; }

    /// <summary>The Pokétch: whether the player has it, its apps and the pedometer's count. Null in older saves.</summary>
    public PoketchSave? Poketch { get; set; }

    /// <summary>A Safari Game under way in the Great Marsh (plan 01 · M7): its balls and steps left. Null when none is.</summary>
    public SafariSave? Safari { get; set; }

    /// <summary>
    /// What the game remembers of its wild Pokémon (plan 06 · R13): the day's numbers, swarms, the Trophy Garden, the
    /// honey trees, the Poké Radar's battery, the roamers. Null in older saves, which are given a new one.
    /// </summary>
    public SpecialEncounters? Encounters { get; set; }

    /// <summary>
    /// Which layout of the world the position refers to. Saves from before the import (0, also what a file
    /// without the field reads as) stood on hand-made maps that no longer exist; <see cref="Place"/> moves them.
    /// </summary>
    public int WorldVersion { get; set; }

    // Where a save made on one of the old hand-made maps wakes up in the imported world, and the version from
    // which that map is gone: a name can come back as a map of the world (the lake), so the version decides
    private static readonly Dictionary<string, (int Until, MapSpot Spot)> OldMaps = new(StringComparer.OrdinalIgnoreCase)
    {
        ["TwinleafTown"] = (ImportedWorld, new MapSpot("Sinnoh", 116, 886)),
        ["Route201"] = (ImportedWorld, new MapSpot("Sinnoh", 112, 858)),
        ["LakeVerity"] = (ImportedWorld, new MapSpot("LakeVerity", 46, 53, Direction.Up)),
        ["SandgemTown"] = (ImportedWorld, new MapSpot("Sinnoh", 177, 843)),
        ["Route202"] = (ImportedWorld, new MapSpot("Sinnoh", 170, 829)),
        // Outside the door of Jubilife's Pokémon Center
        ["JubilifeCity"] = (ImportedJubilife, new MapSpot("Sinnoh", 180, 777))
    };

    /// <summary>Where the save's player stands on today's maps.</summary>
    public MapSpot Place() =>
        OldMaps.TryGetValue(CurrentMapName, out var moved) && WorldVersion < moved.Until
            ? moved.Spot
            : new MapSpot(CurrentMapName, PlayerGridX, PlayerGridY, PlayerFacing);

    public List<SavedPokemonData> Party { get; set; } = new();
    /// <summary>The PC as saves before plan 06 · R12 kept it: one list, laid out thirty to a box as it loads.</summary>
    public List<SavedPokemonData> BoxStorage { get; set; } = new();

    /// <summary>The Trainer Card's score (plan 06 · R12).</summary>
    public int TrainerScore { get; set; }

    /// <summary>The Hall of Fame (plan 06 · R12): how many teams have entered, when the first did, and the last thirty.</summary>
    public int HallOfFameTotal { get; set; }
    public DateTime? HallOfFameDebut { get; set; }
    public List<SavedHallOfFameEntry> HallOfFame { get; set; } = new();

    /// <summary>The Journal's pages, newest first (plan 06 · R12).</summary>
    public List<JournalPage> Journal { get; set; } = new();

    /// <summary>The PC's boxes, each Pokémon in its place (plan 06 · R12); null in a save from before.</summary>
    public SavedBoxes? Boxes { get; set; }
    public List<SavedItemData> Inventory { get; set; } = new();
    public List<int> SeenSpecies { get; set; } = new();
    public List<int> CaughtSpecies { get; set; } = new();

    /// <summary>Whether the Pokédex has been upgraded to the National Pokédex (false in older saves).</summary>
    public bool NationalPokedex { get; set; }

    /// <summary>The Pokédexes whose diploma the player has been given: "Sinnoh", "National".</summary>
    public List<string> Diplomas { get; set; } = new();

    /// <summary>Ids of the trainers already beaten; they don't challenge again.</summary>
    public List<string> DefeatedTrainers { get; set; } = new();

    /// <summary>Story flags set so far, including each region's Hall of Fame (see <see cref="Story.StoryState"/>).</summary>
    public List<string> StoryFlags { get; set; } = new();

    // ---- The rest of what the story remembers (plan 02 · S1). A save from before it has none of these, and
    // StoryVersion 0 says so: Story.StoryMigration brings such a save up to date as it is loaded.

    /// <summary>How much of the story the save knows about: <see cref="Story.StoryState.CurrentVersion"/> when it was written.</summary>
    public int StoryVersion { get; set; }

    /// <summary>The story's variables that aren't nought, by name.</summary>
    public Dictionary<string, int> StoryVariables { get; set; } = new();

    /// <summary>The items picked up off the ground and the hidden ones found, by their ids.</summary>
    public List<string> TakenItems { get; set; } = new();

    /// <summary>The species taken from the professor's briefcase, and the one the rival took; null until then.</summary>
    public string? PlayerStarter { get; set; }
    public string? RivalStarter { get; set; }

    /// <summary>The story as the save has it.</summary>
    public Story.StorySnapshot ToStory() =>
        new(StoryFlags, StoryVariables, DefeatedTrainers, TakenItems, Badges, PlayerStarter, RivalStarter);
}

public class SavedPokemonData
{
    public string SpeciesName { get; set; } = "Turtwig";

    /// <summary>The form it is in (plan 03 · D11); null for its species' own, and in saves from before forms.</summary>
    public string? Form { get; set; }
    public string Nickname { get; set; } = "Turtwig";
    public int Level { get; set; } = 5;
    public Gender Gender { get; set; } = Gender.Male;
    public Nature Nature { get; set; } = Nature.Hardy;
    public bool IsShiny { get; set; } = false;
    public int CurrentHP { get; set; } = 20;
    public int Status { get; set; } = 0;
    public int IvHP { get; set; } = 15;
    public int IvAttack { get; set; } = 15;
    public int IvDefense { get; set; } = 15;
    public int IvSpAttack { get; set; } = 15;
    public int IvSpDefense { get; set; } = 15;
    public int IvSpeed { get; set; } = 15;

    /// <summary>Its effort values (plan 06 · R10); 0 in saves from before they were gained.</summary>
    public int EvHP { get; set; }
    public int EvAttack { get; set; }
    public int EvDefense { get; set; }
    public int EvSpAttack { get; set; }
    public int EvSpDefense { get; set; }
    public int EvSpeed { get; set; }
    public int CurrentExp { get; set; } = 0;

    /// <summary>Null in saves from before abilities: the species' first ability is used.</summary>
    public string? Ability { get; set; }
    public string? HeldItem { get; set; }

    /// <summary>Null in saves from before friendship: the species' base friendship is used.</summary>
    public int? Friendship { get; set; }
    public int Beauty { get; set; }

    /// <summary>Null in saves from before personality values: a new one is rolled.</summary>
    public uint? Personality { get; set; }
    public string? Ball { get; set; }

    /// <summary>The trainer who first had it, when that wasn't the player (plan 06 · R10); left out for the player's own.</summary>
    public TrainerMark? OriginalTrainer { get; set; }

    /// <summary>Its Pokérus byte (<see cref="PokerusRules"/>); 0, left out, for one that never had it.</summary>
    public int Pokerus { get; set; }

    /// <summary>The PC's marks on it (plan 06 · R12), where it was met and on what day at what level, and its language when foreign.</summary>
    public int Marks { get; set; }
    public string? MetLocation { get; set; }
    public int MetLevel { get; set; }
    public DateTime? MetDate { get; set; }
    public string? Language { get; set; }

    /// <summary>Steps, move uses and knock-outs counted toward an evolution; left out when there are none.</summary>
    public Dictionary<string, int>? EvolutionProgress { get; set; }
    public List<SavedMoveData> Moves { get; set; } = new();

    public static SavedPokemonData FromPokemon(Pokemon p)
    {
        var saved = new SavedPokemonData
        {
            SpeciesName = p.Species.Name,
            Form = p.Form,
            Nickname = p.Nickname,
            Level = p.Level,
            Gender = p.Gender,
            Nature = p.Nature,
            IsShiny = p.IsShiny,
            CurrentHP = p.CurrentHP,
            Status = (int)p.Status,
            IvHP = p.IvHP,
            IvAttack = p.IvAttack,
            IvDefense = p.IvDefense,
            IvSpAttack = p.IvSpAttack,
            IvSpDefense = p.IvSpDefense,
            IvSpeed = p.IvSpeed,
            EvHP = p.EvHP,
            EvAttack = p.EvAttack,
            EvDefense = p.EvDefense,
            EvSpAttack = p.EvSpAttack,
            EvSpDefense = p.EvSpDefense,
            EvSpeed = p.EvSpeed,
            CurrentExp = p.CurrentExp,
            Ability = p.AbilityName,
            HeldItem = p.HeldItem?.Name,
            Friendship = p.Friendship,
            Beauty = p.Beauty,
            Personality = p.Personality,
            Ball = p.Ball,
            OriginalTrainer = p.OriginalTrainer,
            Pokerus = p.Pokerus,
            Marks = p.Marks,
            MetLocation = p.MetLocation,
            MetLevel = p.MetLevel,
            MetDate = p.MetDate,
            Language = p.Language,
            EvolutionProgress = p.EvolutionProgress.Count > 0 ? new Dictionary<string, int>(p.EvolutionProgress) : null
        };

        foreach (var m in p.Moves)
        {
            saved.Moves.Add(new SavedMoveData { MoveName = m.Name, CurrentPP = m.CurrentPP, PPUps = m.PPUps });
        }
        return saved;
    }

    public Pokemon ToPokemon()
    {
        var sp = PokemonDatabase.Get(SpeciesName) ?? PokemonDatabase.Get("Turtwig")!;
        var p = new Pokemon(sp, Level, Gender, Nature, IsShiny)
        {
            Nickname = Nickname,
            CurrentHP = Math.Min(CurrentHP, 999),
            Status = (StatusCondition)Status,
            IvHP = IvHP,
            IvAttack = IvAttack,
            IvDefense = IvDefense,
            IvSpAttack = IvSpAttack,
            IvSpDefense = IvSpDefense,
            IvSpeed = IvSpeed,
            EvHP = EvHP,
            EvAttack = EvAttack,
            EvDefense = EvDefense,
            EvSpAttack = EvSpAttack,
            EvSpDefense = EvSpDefense,
            EvSpeed = EvSpeed,
            CurrentExp = CurrentExp,
            HeldItem = HeldItem != null ? ItemDatabase.Get(HeldItem) : null,
            Beauty = Beauty,
            Ball = Ball,
            OriginalTrainer = OriginalTrainer,
            Pokerus = Pokerus,
            Marks = Marks,
            MetLocation = MetLocation,
            MetLevel = MetLevel,
            MetDate = MetDate,
            Language = Language
        };
        if (Form != null) p.RestoreForm(Form);
        if (Ability != null) p.AbilityName = Ability;
        if (Friendship is { } friendship) p.Friendship = friendship;
        if (Personality is { } personality) p.Personality = personality;
        foreach (var (key, count) in EvolutionProgress ?? new()) p.EvolutionProgress[key] = count;
        p.RecalculateStats();
        p.CurrentHP = Math.Clamp(CurrentHP, 0, p.MaxHP);
        if (p.CurrentHP <= 0)
        {
            p.Status = StatusCondition.Faint;
        }

        if (Moves.Count > 0)
        {
            p.Moves.Clear();
            foreach (var sm in Moves)
            {
                var move = MoveDatabase.Create(sm.MoveName);
                move.PPUps = Math.Clamp(sm.PPUps, 0, 3);
                move.CurrentPP = sm.CurrentPP;
                p.Moves.Add(move);
            }
        }
        return p;
    }
}

public class SavedMoveData
{
    public string MoveName { get; set; } = "Tackle";
    public int CurrentPP { get; set; } = 35;

    /// <summary>The PP Ups it has taken (plan 06 · R11); a save from before has none.</summary>
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault)]
    public int PPUps { get; set; }
}

public class SavedItemData
{
    public string ItemName { get; set; } = "Poké Ball";
    public int Quantity { get; set; } = 5;
}

public static class SaveManager
{
    private static readonly string SaveFilePath = "savegame.json";

    public static bool HasSaveFile() => File.Exists(SaveFilePath);

    public static bool SaveGame(SaveData data)
    {
        try
        {
            string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SaveFilePath, json);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static SaveData? LoadGame()
    {
        try
        {
            if (!HasSaveFile()) return null;
            string json = File.ReadAllText(SaveFilePath);
            return JsonSerializer.Deserialize<SaveData>(json);
        }
        catch
        {
            return null;
        }
    }
}

/// <summary>The PC's boxes in a save (plan 06 · R12): the box the PC was left on, the wallpapers unlocked, and each box's name, wallpaper and Pokémon by place.</summary>
public class SavedBoxes
{
    public int CurrentBox { get; set; }
    public int UnlockedWallpapers { get; set; }
    public List<SavedBox> Boxes { get; set; } = new();

    public static SavedBoxes From(PcBoxes pc) => new()
    {
        CurrentBox = pc.CurrentBox,
        UnlockedWallpapers = pc.UnlockedWallpapers,
        Boxes = pc.Boxes.Select(b => new SavedBox
        {
            Name = b.Name,
            Wallpaper = b.Wallpaper,
            Pokemon = b.Slots.Select((p, slot) => (p, slot)).Where(t => t.p != null)
                .Select(t => new SavedBoxedPokemon { Slot = t.slot, Pokemon = SavedPokemonData.FromPokemon(t.p!) }).ToList()
        }).ToList()
    };

    public PcBoxes ToBoxes()
    {
        var pc = new PcBoxes { UnlockedWallpapers = UnlockedWallpapers, CurrentBox = Math.Clamp(CurrentBox, 0, PcBoxes.BoxCount - 1) };
        for (int i = 0; i < Math.Min(Boxes.Count, PcBoxes.BoxCount); i++)
        {
            var saved = Boxes[i];
            pc.Rename(i, saved.Name);
            if (pc.HasWallpaper(saved.Wallpaper)) pc.Boxes[i].Wallpaper = saved.Wallpaper;
            foreach (var boxed in saved.Pokemon)
                if (boxed.Slot is >= 0 and < PcBoxes.BoxSize && pc[i, boxed.Slot] == null)
                    pc.Boxes[i].Slots[boxed.Slot] = boxed.Pokemon.ToPokemon();
        }
        return pc;
    }
}

public class SavedBox
{
    public string Name { get; set; } = "";
    public int Wallpaper { get; set; }
    public List<SavedBoxedPokemon> Pokemon { get; set; } = new();
}

public class SavedBoxedPokemon
{
    public int Slot { get; set; }
    public SavedPokemonData Pokemon { get; set; } = new();
}

/// <summary>A team of the Hall of Fame in a save (plan 06 · R12).</summary>
public class SavedHallOfFameEntry
{
    public DateTime Date { get; set; }
    public List<HallOfFameMember> Team { get; set; } = new();

    public static SavedHallOfFameEntry From(HallOfFameEntry entry) => new() { Date = entry.Date, Team = entry.Team.ToList() };
    public HallOfFameEntry ToEntry() => new(Date, Team);
}
