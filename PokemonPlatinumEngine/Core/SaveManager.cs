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
    public string PlayerName { get; set; } = "Lucas";
    public int Money { get; set; } = 3000;
    public float PlayTimeSeconds { get; set; } = 0;
    public int Badges { get; set; } = 0;
    public string CurrentMapName { get; set; } = "TwinleafTown";
    public int PlayerGridX { get; set; } = 11;
    public int PlayerGridY { get; set; } = 8;
    public Direction PlayerFacing { get; set; } = Direction.Down;

    public List<SavedPokemonData> Party { get; set; } = new();
    public List<SavedPokemonData> BoxStorage { get; set; } = new();
    public List<SavedItemData> Inventory { get; set; } = new();
    public List<int> SeenSpecies { get; set; } = new();
    public List<int> CaughtSpecies { get; set; } = new();

    /// <summary>Ids of the trainers already beaten; they don't challenge again.</summary>
    public List<string> DefeatedTrainers { get; set; } = new();
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
            CurrentExp = p.CurrentExp
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
            CurrentExp = CurrentExp
        };
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
