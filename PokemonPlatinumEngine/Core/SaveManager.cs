using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Core;

public class SaveData
{
    /// <summary>The <see cref="WorldVersion"/> of saves made today: Sinnoh's overworld is one map made from the imported world.</summary>
    public const int ImportedWorld = 1;

    public string PlayerName { get; set; } = "Lucas";

    /// <summary>Which of the two characters the player is. Saves from before the choice existed are the boy.</summary>
    public PlayerLook Look { get; set; }

    /// <summary>The number on the Trainer Card, drawn when the game began. 0 in older saves: one is drawn on loading.</summary>
    public int TrainerId { get; set; }

    /// <summary>The day the adventure began; null in saves from before it was recorded.</summary>
    public DateTime? Started { get; set; }

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
    /// Which layout of the world the position refers to. Saves from before the import (0, also what a file
    /// without the field reads as) stood on hand-made maps that no longer exist; <see cref="Place"/> moves them.
    /// </summary>
    public int WorldVersion { get; set; }

    // Where a save made on one of the old hand-made maps wakes up in the imported world
    private static readonly Dictionary<string, MapSpot> OldMaps = new(StringComparer.OrdinalIgnoreCase)
    {
        ["TwinleafTown"] = new MapSpot("Sinnoh", 116, 886),
        ["Route201"] = new MapSpot("Sinnoh", 112, 858),
        ["LakeVerity"] = new MapSpot("LakeVerity", 46, 53, Direction.Up),
        ["SandgemTown"] = new MapSpot("Sinnoh", 177, 843),
        ["Route202"] = new MapSpot("Sinnoh", 170, 829)
    };

    /// <summary>Where the save's player stands on today's maps.</summary>
    public MapSpot Place() =>
        WorldVersion < ImportedWorld && OldMaps.TryGetValue(CurrentMapName, out var moved)
            ? moved
            : new MapSpot(CurrentMapName, PlayerGridX, PlayerGridY, PlayerFacing);

    public List<SavedPokemonData> Party { get; set; } = new();
    public List<SavedPokemonData> BoxStorage { get; set; } = new();
    public List<SavedItemData> Inventory { get; set; } = new();
    public List<int> SeenSpecies { get; set; } = new();
    public List<int> CaughtSpecies { get; set; } = new();

    /// <summary>Ids of the trainers already beaten; they don't challenge again.</summary>
    public List<string> DefeatedTrainers { get; set; } = new();

    /// <summary>Story flags set so far, including each region's Hall of Fame (see StoryProgress).</summary>
    public List<string> StoryFlags { get; set; } = new();
}

public class SavedPokemonData
{
    public string SpeciesName { get; set; } = "Turtwig";
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

    /// <summary>Steps, move uses and knock-outs counted toward an evolution; left out when there are none.</summary>
    public Dictionary<string, int>? EvolutionProgress { get; set; }
    public List<SavedMoveData> Moves { get; set; } = new();

    public static SavedPokemonData FromPokemon(Pokemon p)
    {
        var saved = new SavedPokemonData
        {
            SpeciesName = p.Species.Name,
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
            CurrentExp = p.CurrentExp,
            Ability = p.AbilityName,
            HeldItem = p.HeldItem?.Name,
            Friendship = p.Friendship,
            Beauty = p.Beauty,
            Personality = p.Personality,
            Ball = p.Ball,
            EvolutionProgress = p.EvolutionProgress.Count > 0 ? new Dictionary<string, int>(p.EvolutionProgress) : null
        };

        foreach (var m in p.Moves)
        {
            saved.Moves.Add(new SavedMoveData { MoveName = m.Name, CurrentPP = m.CurrentPP });
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
            CurrentExp = CurrentExp,
            HeldItem = HeldItem != null ? ItemDatabase.Get(HeldItem) : null,
            Beauty = Beauty,
            Ball = Ball
        };
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
