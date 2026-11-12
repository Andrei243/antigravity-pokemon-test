using System.Collections.Generic;

namespace PokemonPlatinumEngine.Data;

/// <summary>A floor of FireRed's Trainer Tower: its kind (0 single, 1 double, 2 knockout), its prize and its trainers, each with six Pokémon.</summary>
public sealed class TrainerTowerFloorRecord
{
    /// <summary>FireRed's <c>CHALLENGE_TYPE_*</c> of the floor: 0 a single battle, 1 a double battle of two, 2 a knockout of three.</summary>
    public int Kind { get; set; }

    /// <summary>The prize the challenge hands over when this is its first floor (<c>GiveChallengePrize</c>).</summary>
    public string Prize { get; set; } = string.Empty;

    /// <summary>One trainer on a single floor, two on a double, three on a knockout, each with FireRed's six Pokémon.</summary>
    public List<TrainerHillTrainerRecord> Trainers { get; set; } = new();
}

/// <summary>One of FireRed's four challenges: eight floors.</summary>
public sealed class TrainerTowerChallengeRecord
{
    public string Name { get; set; } = string.Empty;
    public List<TrainerTowerFloorRecord> Floors { get; set; } = new();
}

/// <summary>
/// FireRed's Trainer Tower (plan 37 · FC6; <c>dotnet run --project tools/DataImporter -- --trainer-tower</c>, from
/// <c>src/trainer_tower_sets.c</c> and <c>src/trainer_tower.c</c>; numbers only: no name, no word of FireRed's).
/// </summary>
public sealed class TrainerTowerFile
{
    /// <summary>Single, Double, Knockout and Mixed, in FireRed's order (<c>gTrainerTowerFloors</c>).</summary>
    public List<TrainerTowerChallengeRecord> Challenges { get; set; } = new();

    /// <summary>Which two of a single floor's trainer's six come, by the floors cleared (<c>sSingleBattleChallengeMonIdxs</c>).</summary>
    public List<List<int>> SingleSlots { get; set; } = new();

    /// <summary>Which one of each double floor's two trainers' six comes (<c>sDoubleBattleChallengeMonIdxs</c>).</summary>
    public List<List<int>> DoubleSlots { get; set; } = new();

    /// <summary>Which one of each knockout floor's three trainers' six comes (<c>sKnockoutChallengeMonIdxs</c>).</summary>
    public List<List<int>> KnockoutSlots { get; set; } = new();
}

/// <summary>The Trainer Tower's data (<c>Data/trainer-tower.json</c>, generated: never edited by hand).</summary>
public static class TrainerTowerDatabase
{
    public const string FileName = "trainer-tower.json";

    private static TrainerTowerFile? data;
    private static TrainerTowerFile Data => data ??= GameDataFiles.Load<TrainerTowerFile>(FileName);

    public static IReadOnlyList<TrainerTowerChallengeRecord> Challenges => Data.Challenges;
    public static IReadOnlyList<List<int>> SingleSlots => Data.SingleSlots;
    public static IReadOnlyList<List<int>> DoubleSlots => Data.DoubleSlots;
    public static IReadOnlyList<List<int>> KnockoutSlots => Data.KnockoutSlots;

    /// <summary>A floor (1 to 8) of a challenge (0 to 3).</summary>
    public static TrainerTowerFloorRecord Floor(int challenge, int floor) => Data.Challenges[challenge].Floors[floor - 1];
}
