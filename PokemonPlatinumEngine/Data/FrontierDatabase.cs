using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace PokemonPlatinumEngine.Data;

/// <summary>
/// One of the Battle Frontier's trainers (<c>res/trainers/frontier/data</c>; the original's <c>FrontierTrainerBase</c>
/// with the name its bank gives it): who they are and the sets their Pokémon are drawn from. Never a word of what
/// they say. Their number is their place in <see cref="FrontierData.Trainers"/>, which is what every table of the
/// Frontier goes by (how good their Pokémon's IVs are, which room of a streak they stand in).
/// </summary>
public sealed class FrontierTrainerRecord
{
    /// <summary>The trainer's constant without <c>FRONTIER_TRAINER_</c>, lower case (<c>youngster_jim</c>).</summary>
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Class { get; set; } = "";

    /// <summary>
    /// How the class walks into a battle room: the original's field look for it (<c>sTrainerClassToObjectID</c>:
    /// <c>youngster</c>, <c>ace_trainer_m</c>), <c>school_kid_m</c> for a class the table doesn't name.
    /// </summary>
    public string Look { get; set; } = "";

    /// <summary>The sets its Pokémon are drawn from, by their numbers; the original's empty set is left out, as its count leaves it out.</summary>
    public List<int> Sets { get; set; } = new();
}

/// <summary>
/// One of the Frontier's Pokémon sets (<c>res/trainers/frontier/pokemon</c>; <c>FrontierPokemonBase</c>): a species
/// with its four moves, the stats its effort goes into, its nature and what it holds. Its level, IVs, personality and
/// ability are made as it is sent to battle (<see cref="Models.Frontier.FrontierTrainers"/>).
/// </summary>
public sealed class FrontierSetRecord
{
    /// <summary>The set's name in the original's order file (<c>abomasnow_1</c>).</summary>
    public string Id { get; set; } = "";
    public string Species { get; set; } = "";
    public List<string> Moves { get; set; } = new();

    /// <summary>The stats its effort is shared between, a bit each in the original's order: HP, Attack, Defense, Speed, Sp. Atk, Sp. Def.</summary>
    public int EvFlags { get; set; }
    public Nature Nature { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Item { get; set; }
}

/// <summary>Something an Exchange Service Corner hands over for Battle Points.</summary>
public sealed class FrontierPrizeRecord
{
    public string Item { get; set; } = "";
    public int Price { get; set; }
}

/// <summary>
/// A row of one of the Battle Factory's tables of rental Pokémon (<c>BattleFactoryPokemonPool</c>): the sets a
/// Pokémon is drawn from, the IV all its stats take, and whether a streak's trades can lift a draw into the next row.
/// A row is found by a round's number or by a trainer's (the first whose <see cref="TrainerMax"/> the trainer's
/// number is under).
/// </summary>
public sealed class FactoryPoolRecord
{
    public int TrainerMax { get; set; }
    public int SetMin { get; set; }
    public int SetMax { get; set; }
    public int Ivs { get; set; }
    public bool Upgrades { get; set; }
}

/// <summary>The Battle Factory's tables (plan 06 · R18b; <c>src/overlay104/battle_factory_helpers.c</c>).</summary>
public sealed class FactoryTables
{
    /// <summary>
    /// The trainers of a round, round by round (<c>sBattleFactoryOpponentPools</c>): the first and the one past the
    /// last of those its first six battles are drawn from, and the same for its seventh. The last row is every
    /// round's from the eighth on.
    /// </summary>
    public List<int[]> Opponents { get; set; } = new();

    /// <summary>The rentals at Level 50, the player's by the round and a trainer's by the trainer (<c>sLevel50Pools</c>).</summary>
    public List<FactoryPoolRecord> Level50 { get; set; } = new();

    /// <summary>A trainer's Pokémon at the Open Level (<c>sOpenLevelOpponentPools</c>).</summary>
    public List<FactoryPoolRecord> OpenFoes { get; set; } = new();

    /// <summary>The player's rentals at the Open Level, by the round (<c>sOpenLevelPlayerPools</c>).</summary>
    public List<FactoryPoolRecord> OpenRentals { get; set; } = new();
}

public sealed class FrontierData
{
    /// <summary>The trainers in the original's order: the first is trainer 0.</summary>
    public List<FrontierTrainerRecord> Trainers { get; set; } = new();

    /// <summary>The sets in the original's order: set 0 is the empty one.</summary>
    public List<FrontierSetRecord> Sets { get; set; } = new();

    /// <summary>
    /// The Battle Tower's trainers room by room (<c>sBattleTowerTrainerRangesPerRoom</c>): the first and the last
    /// trainer's number of each of its eight rows, the last row for every room from the eighth on.
    /// </summary>
    public List<int[]> TowerRooms { get; set; } = new();

    /// <summary>The seventh trainer of each room is drawn from a row of these instead (<c>sBattleTowerBossTrainerRangesPerRoom</c>).</summary>
    public List<int[]> TowerBossRooms { get; set; } = new();

    /// <summary>The west booth of the Exchange Service Corner (<c>MART_FRONTIER_ID_EXCHANGE_SERVICE_CORNER_LEFT</c>): vitamins and held items.</summary>
    public List<FrontierPrizeRecord> ExchangeItems { get; set; } = new();

    /// <summary>The east booth (<c>..._RIGHT</c>): Technical Machines.</summary>
    public List<FrontierPrizeRecord> ExchangeMachines { get; set; } = new();

    /// <summary>The species no facility lets in (<c>sBattleFrontierBanlist</c>), in the original's order.</summary>
    public List<string> Banned { get; set; } = new();

    /// <summary>The Battle Factory's rounds and rentals (plan 06 · R18b).</summary>
    public FactoryTables Factory { get; set; } = new();
}

/// <summary>
/// The Battle Frontier's tables (plan 06 · R18a), imported by <c>tools/DataImporter</c> (<c>Importer.Frontier.cs</c>):
/// names and numbers only. The trainers and their sets are every facility's, the Tower's among them.
/// </summary>
public static class FrontierDatabase
{
    public const string FileName = "frontier.json";

    private static readonly FrontierData Data = GameDataFiles.Load<FrontierData>(FileName);
    private static readonly Dictionary<string, int> TrainerNumbers =
        Data.Trainers.Select((t, i) => (t.Id, i)).ToDictionary(p => p.Id, p => p.i, StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<string> BannedSpecies = new(Data.Banned, StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyList<FrontierTrainerRecord> Trainers => Data.Trainers;
    public static IReadOnlyList<FrontierSetRecord> Sets => Data.Sets;
    public static IReadOnlyList<int[]> TowerRooms => Data.TowerRooms;
    public static IReadOnlyList<int[]> TowerBossRooms => Data.TowerBossRooms;
    public static IReadOnlyList<FrontierPrizeRecord> ExchangeItems => Data.ExchangeItems;
    public static IReadOnlyList<FrontierPrizeRecord> ExchangeMachines => Data.ExchangeMachines;
    public static IReadOnlyList<string> Banned => Data.Banned;
    public static FactoryTables Factory => Data.Factory;

    public static FrontierTrainerRecord Trainer(int number) => Data.Trainers[number];
    public static FrontierSetRecord Set(int number) => Data.Sets[number];

    /// <summary>A trainer's number by its id (<c>tower_tycoon_palmer_silver</c>); -1 for none.</summary>
    public static int NumberOf(string id) => TrainerNumbers.TryGetValue(id, out int number) ? number : -1;

    /// <summary><c>Pokemon_IsOnBattleFrontierBanlist</c>.</summary>
    public static bool IsBanned(string species) => BannedSpecies.Contains(species);
}
