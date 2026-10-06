using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using PokemonPlatinumEngine.Battle.Sim.Ai;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Data;

/// <summary>
/// One of Platinum's trainers as <c>trainers.json</c> has it (plan 06 · R9, written by <c>tools/DataImporter</c> from
/// the decompilation's <c>res/trainers/data</c>): how the trainer thinks, the items it uses in battle, its team and
/// the prize money the original's formula gives. Never what the trainer says: our overlays write that.
/// </summary>
public sealed class TrainerRecord
{
    /// <summary>The trainer's constant without its prefix, lower case (<c>youngster_tristan</c>).</summary>
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Class { get; set; } = "";

    /// <summary>The routines of the AI script it thinks with (<see cref="AiFlags"/>, by name).</summary>
    public List<string> Ai { get; set; } = new();

    /// <summary>The items it can use in battle, in the order it looks at them.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public List<string> Items { get; set; } = new();

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool DoubleBattle { get; set; }

    public int PrizeMoney { get; set; }
    public List<TrainerPokemonRecord> Party { get; set; } = new();
}

/// <summary>A Pokémon of a trainer's team as the original keeps it, with the personality it works out for it.</summary>
public sealed class TrainerPokemonRecord
{
    public string Species { get; set; } = "";

    /// <summary>The form, by its name (<c>Shellos-East</c>); null for the species' own.</summary>
    public string? Form { get; set; }
    public int Level { get; set; }

    /// <summary>Its IVs, all six alike, as a share of 255 of the most (<c>iv_scale</c>).</summary>
    public int IvScale { get; set; }

    /// <summary>What its nature, gender and ability follow from (<c>TrainerData_BuildParty</c>).</summary>
    public uint Personality { get; set; }

    public string? Item { get; set; }

    /// <summary>The moves the trainer chose for it; null when it knows what its level taught it.</summary>
    public List<string>? Moves { get; set; }
}

/// <summary>Platinum's trainers, read from <c>Data/trainers.json</c> the first time one is asked for.</summary>
public static class TrainerDatabase
{
    public const string FileName = "trainers.json";

    private static readonly Dictionary<string, TrainerRecord> Trainers =
        GameDataFiles.Load<List<TrainerRecord>>(FileName).ToDictionary(t => t.Id, StringComparer.OrdinalIgnoreCase);

    public static IEnumerable<TrainerRecord> All => Trainers.Values;

    /// <summary>A trainer by its id (<c>youngster_tristan</c>) or its constant (<c>TRAINER_YOUNGSTER_TRISTAN</c>); null for none.</summary>
    public static TrainerRecord? Get(string? idOrConstant)
    {
        if (string.IsNullOrEmpty(idOrConstant)) return null;
        string key = idOrConstant.StartsWith("TRAINER_", StringComparison.Ordinal) ? idOrConstant["TRAINER_".Length..] : idOrConstant;
        return Trainers.TryGetValue(key, out var t) ? t : null;
    }

    /// <summary>
    /// Gives a trainer of the map what Platinum's data says of it: how it thinks and the items it uses, and, where the
    /// map's record names the same team (or none), the team itself as the original builds it, with its prize money.
    /// A team the map writes differently is left as it is.
    /// </summary>
    public static void Fill(Trainer trainer, TrainerRecord record)
    {
        trainer.Ai = AiFlagNames.Parse(record.Ai);
        trainer.Items = record.Items.ToList();
        var mine = trainer.Party.Members;
        bool same = mine.Count == 0 || (mine.Count == record.Party.Count &&
            mine.Zip(record.Party).All(pair => pair.First.Species.Name == pair.Second.Species && pair.First.Level == pair.Second.Level));
        if (!same) return;
        if (mine.Count == 0)
        {
            if (string.IsNullOrEmpty(trainer.Name)) trainer.Name = record.Name;
            if (string.IsNullOrEmpty(trainer.TrainerClass) || trainer.TrainerClass == "Trainer") trainer.TrainerClass = record.Class;
            trainer.PrizeMoney = record.PrizeMoney;
            trainer.DoubleBattle |= record.DoubleBattle;
        }
        trainer.Party.Clear();
        foreach (var m in record.Party) trainer.Party.Add(Build(m));
    }

    /// <summary>A fresh team of the trainer's, built as the original builds it.</summary>
    public static Party PartyOf(TrainerRecord record)
    {
        var party = new Party();
        foreach (var m in record.Party) party.Add(Build(m));
        return party;
    }

    /// <summary>
    /// A trainer's Pokémon as <c>TrainerData_BuildParty</c> makes it: every IV the IV scale's share of 31, the nature,
    /// gender and ability from its personality (the nature is the personality's remainder by 25, the gender its last
    /// byte against the species' ratio, the second ability its lowest bit), never shiny, holding its item and knowing
    /// the moves chosen for it or else what its level taught it.
    /// </summary>
    public static Pokemon Build(TrainerPokemonRecord m)
    {
        var species = PokemonDatabase.Get(m.Species) ?? throw new InvalidOperationException($"No species called {m.Species}");
        int iv = m.IvScale * 31 / 255;
        var pokemon = new Pokemon(species, m.Level, new Random((int)(m.Personality & 0x7FFFFFFF)), GenderOf(species, m.Personality), (Nature)(m.Personality % 25))
        {
            IsShiny = false,
            Personality = m.Personality,
            IvHP = iv, IvAttack = iv, IvDefense = iv, IvSpAttack = iv, IvSpDefense = iv, IvSpeed = iv
        };
        if (m.Form != null) pokemon.ChangeForm(m.Form);
        var abilities = pokemon.Abilities;
        pokemon.AbilityName = abilities.Count == 0 ? null : abilities.Count > 1 && (m.Personality & 1) == 1 ? abilities[1] : abilities[0];
        pokemon.HeldItem = m.Item == null ? null : ItemDatabase.Get(m.Item);
        if (m.Moves is { Count: > 0 } chosen)
        {
            pokemon.Moves.Clear();
            foreach (string move in chosen) pokemon.Moves.Add(MoveDatabase.Create(move));
        }
        pokemon.RecalculateStats();
        pokemon.CurrentHP = pokemon.MaxHP;
        return pokemon;
    }

    /// <summary>The original's gender from a personality: its last byte under the species' share of females (in 256ths) makes it female.</summary>
    public static Gender GenderOf(PokemonSpecies species, uint personality)
    {
        if (species.IsGenderless) return Gender.Genderless;
        int ratio = species.GenderRatio switch { 0 => 0, 8 => 254, var eighths => eighths * 32 - 1 };
        if (ratio == 0) return Gender.Male;
        if (ratio == 254) return Gender.Female;
        return (personality & 0xFF) < ratio ? Gender.Female : Gender.Male;
    }
}
