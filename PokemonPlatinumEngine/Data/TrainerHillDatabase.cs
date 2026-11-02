using System.Collections.Generic;
using System.Linq;

namespace PokemonPlatinumEngine.Data;

/// <summary>
/// One Pokémon of a Trainer Hill trainer (Emerald's <c>struct BattleTowerPokemon</c> in <c>trainer_hill.h</c>): its
/// species, held item and moves, its effort and IVs in Emerald's order (HP, Attack, Defense, Speed, Sp. Atk, Sp. Def),
/// which of its abilities, and its personality. Numbers only; its level is the challenger's highest.
/// </summary>
public sealed class TrainerHillMon
{
    public string Species { get; set; } = string.Empty;
    public string? Item { get; set; }
    public List<string> Moves { get; set; } = new();
    public List<int> Evs { get; set; } = new();
    public List<int> Ivs { get; set; } = new();
    public int AbilityNum { get; set; }
    public uint Personality { get; set; }
}

/// <summary>
/// A trainer of a floor: Emerald's number for them (what the prize is worked out from), their class and look, a name of
/// our own, and the three Pokémon they bring (<c>sTrainerPartySlots</c>: the first trainer the first three of the
/// floor's six, the second the last three).
/// </summary>
public sealed class TrainerHillTrainerRecord
{
    public int Number { get; set; }
    public string Class { get; set; } = string.Empty;
    public string Look { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public List<TrainerHillMon> Team { get; set; } = new();
}

/// <summary>Where a floor's trainer stands in its sixteen by sixteen (<c>trainerCoords</c>), the way they face and how far they see.</summary>
public sealed class TrainerHillSpot
{
    public int X { get; set; }
    public int Y { get; set; }
    public Direction Facing { get; set; }
    public int Range { get; set; }
}

/// <summary>
/// A floor of a challenge: its two trainers, where they stand, and its sixteen rows of sixteen tiles from Emerald's
/// collision data (<c>graphics/trainer_hill/maps_*/floor_*/collision.bin</c>: '#' blocked, '.' open, 'v' a ledge hopped down to the south, by its metatile's behaviour). No metatile is
/// kept: the floor is drawn with our own pieces.
/// </summary>
public sealed class TrainerHillFloorRecord
{
    public List<TrainerHillTrainerRecord> Trainers { get; set; } = new();
    public List<TrainerHillSpot> Spots { get; set; } = new();
    public List<string> Rows { get; set; } = new();
}

/// <summary>One of the four challenges (Normal, Variety, Unique, Expert), its four floors.</summary>
public sealed class TrainerHillModeRecord
{
    public string Name { get; set; } = string.Empty;
    public List<TrainerHillFloorRecord> Floors { get; set; } = new();
}

/// <summary>What <c>trainer-hill.json</c> holds (written by <c>tools/DataImporter --trainer-hill</c>, never by hand).</summary>
public sealed class TrainerHillFile
{
    public List<TrainerHillModeRecord> Modes { get; set; } = new();

    /// <summary>Emerald's two sets of ten prize lists of six (<c>sPrizeListSets</c>), by item name.</summary>
    public List<List<List<string>>> PrizeSets { get; set; } = new();
}

/// <summary>
/// The Trainer Hill's tables (plan 21, part l; Emerald's <c>src/data/battle_frontier/trainer_hill.h</c> and the prize
/// lists of <c>src/trainer_hill.c</c>): the four challenges' floors with their trainers, teams and collision, and the
/// prizes. Read by <see cref="Models.Frontier.TrainerHill"/>.
/// </summary>
public static class TrainerHillDatabase
{
    public const string FileName = "trainer-hill.json";

    private static TrainerHillFile? data;
    private static TrainerHillFile Data => data ??= GameDataFiles.Load<TrainerHillFile>(FileName);

    public static IReadOnlyList<TrainerHillModeRecord> Modes => Data.Modes;
    public static IReadOnlyList<List<List<string>>> PrizeSets => Data.PrizeSets;

    /// <summary>A floor (1 to 4) of a challenge (0 to 3).</summary>
    public static TrainerHillFloorRecord Floor(int mode, int floor) => Data.Modes[mode].Floors[floor - 1];

    /// <summary>Every trainer of a challenge, floor by floor.</summary>
    public static IEnumerable<TrainerHillTrainerRecord> TrainersOf(int mode) => Data.Modes[mode].Floors.SelectMany(f => f.Trainers);
}
