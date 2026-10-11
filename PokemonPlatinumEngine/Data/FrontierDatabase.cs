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

    /// <summary>The form it is in, where it is in one (plan 06 · R18c: two of the Battle Hall's Wormadam, by their cloaks).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Form { get; set; }
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

/// <summary>
/// The Battle Hall's tables (plan 06 · R18c; <c>src/overlay104/battle_hall_helpers.c</c> and the archive of its own
/// Pokémon, <c>pl_bsdpm.narc</c>).
/// </summary>
public sealed class HallTables
{
    /// <summary>
    /// The Pokémon met in the Hall, in the original's order (<c>sBattleHallPotentialOpponents</c>, each with its set
    /// of the Hall's own archive): number 1 is the first, and the list's own first entry is the archive's empty one.
    /// </summary>
    public List<FrontierSetRecord> Pokemon { get; set; } = new();

    /// <summary>The two types each of them counts as on the board (<c>sBattleHallPotentialOpponentTypes</c>), by the same numbers.</summary>
    public List<PokemonType[]> Types { get; set; } = new();

    /// <summary>The numbers the four groups begin at (<c>GROUP_1_START</c> to <c>GROUP_4_START</c>), weakest first.</summary>
    public List<int> Groups { get; set; } = new();

    /// <summary>The eight classes a trainer is drawn from at each of a type's ten ranks (<c>sHallTrainerClassesByRank</c>), by their keys.</summary>
    public List<string[]> ClassesByRank { get; set; } = new();

    /// <summary>The four classes a type brings (<c>sHallTrainerClassesBySelectedType</c>), in the board's order of types with the unnamed one last.</summary>
    public List<string[]> ClassesByType { get; set; } = new();

    /// <summary>Every Frontier trainer's class by its key (<c>sFrontierTrainerClasses</c>: the class's constant, which tells a Pokéfan from a Pokéfan where the name doesn't).</summary>
    public List<string> TrainerClasses { get; set; } = new();
}

/// <summary>
/// The Battle Castle's tables (plan 06 · R18c; <c>src/overlay104/battle_castle_helpers.c</c> and the valets'
/// counters, <c>src/applications/frontier/battle_castle/self_app.c</c>).
/// </summary>
public sealed class CastleTables
{
    /// <summary>The trainers of a round, round by round (<c>sBattleCastleOpponentPools</c>), as the Battle Factory's rows are.</summary>
    public List<int[]> Opponents { get; set; } = new();

    /// <summary>The berries lent for Castle Points and what each costs (<c>sBerriesForRent</c>, <c>sBerryPrices</c>).</summary>
    public List<FrontierPrizeRecord> Berries { get; set; } = new();

    /// <summary>The items lent (<c>sItemsForRent</c>, <c>sItemPrices</c>).</summary>
    public List<FrontierPrizeRecord> Items { get; set; } = new();

    /// <summary>How many of each list a rank of the counter lends, first rank first (<c>sBerriesAvailableByRank</c>, <c>sItemsAvailableByRank</c>).</summary>
    public List<int> BerriesByRank { get; set; } = new();
    public List<int> ItemsByRank { get; set; } = new();
}

/// <summary>
/// One of the thirty-two events of the Battle Arcade's game board (<c>ARCADE_EFFECT_…</c>), in the original's order:
/// its constant's name, the rounds it may come up in and, for the seven that go by the battle too, the battles of a
/// round (<c>sEffectAvailabilityByRound</c>, <c>sEffectAvailabilityByBattle</c>).
/// </summary>
public sealed class ArcadeEffectRecord
{
    public string Name { get; set; } = "";

    /// <summary>Seven flags: the first six rounds, and every round from the seventh on.</summary>
    public int[] Rounds { get; set; } = Array.Empty<int>();

    /// <summary>Seven flags, a battle of the round each; null for an event that comes up at any battle.</summary>
    public int[]? Battles { get; set; }
}

/// <summary>
/// The Battle Arcade's tables (plan 06 · R18d; <c>src/overlay104/battle_arcade.c</c>, <c>battle_arcade_helpers.c</c>
/// and the game board, <c>src/applications/frontier/battle_arcade/main.c</c>).
/// </summary>
public sealed class ArcadeTables
{
    /// <summary>The trainers of a round, round by round (<c>sBattleArcadeOpponentPools</c>), as the Battle Factory's rows are.</summary>
    public List<int[]> Opponents { get; set; } = new();

    /// <summary>What the board gives a side to hold, by the round: three lists of items and three of berries (<c>sItemPools</c>, <c>sBerryPools</c>).</summary>
    public List<List<string>> Items { get; set; } = new();
    public List<List<string>> Berries { get; set; } = new();

    /// <summary>The board's events (<c>NUM_ARCADE_EFFECTS</c>).</summary>
    public List<ArcadeEffectRecord> Effects { get; set; } = new();

    /// <summary>How likely a panel is to be the foe's, one's own, the battle's or a bonus, by how the last battle went (<c>sCategoryWeights</c>, five rows of four).</summary>
    public List<int[]> Weights { get; set; } = new();

    /// <summary>The scores a last battle must reach for each of the first four rows of <see cref="Weights"/> (<c>sCategoryTierPerformanceThresholds</c>).</summary>
    public List<int> Thresholds { get; set; } = new();

    /// <summary>The frames the cursor waits on a panel at each of its eight speeds (<c>sCursorMoveFrames</c>).</summary>
    public List<int> CursorFrames { get; set; } = new();

    /// <summary>The cursor's speed as a round begins: a round and the speed from it on, the latest round first (<c>sRouletteSpeedByRound</c>).</summary>
    public List<int[]> SpeedByRound { get; set; } = new();

    /// <summary>What a battle scores by how many of the team ended it with a status, by how many fainted, and by the turns it took: a number of turns it was under and the score (<c>sPerformanceByNumWithStatus</c>, <c>…ByNumFainted</c>, <c>…ByTurnsElapsed</c>).</summary>
    public List<int> ScoreByStatus { get; set; } = new();
    public List<int> ScoreByFainted { get; set; } = new();
    public List<int[]> ScoreByTurns { get; set; } = new();

    /// <summary>The Battle Points a round earns, by the round the streak goes on to (<c>sBPPerRoundSolo</c>).</summary>
    public List<int> PointsByRound { get; set; } = new();
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

    /// <summary>The Battle Hall's Pokémon, groups and trainer classes (plan 06 · R18c).</summary>
    public HallTables Hall { get; set; } = new();

    /// <summary>The Battle Castle's rounds and what its counters lend (plan 06 · R18c).</summary>
    public CastleTables Castle { get; set; } = new();

    /// <summary>The Battle Arcade's rounds and its game board (plan 06 · R18d).</summary>
    public ArcadeTables Arcade { get; set; } = new();
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
    public static HallTables Hall => Data.Hall;
    public static CastleTables Castle => Data.Castle;
    public static ArcadeTables Arcade => Data.Arcade;

    public static FrontierTrainerRecord Trainer(int number) => Data.Trainers[number];
    public static FrontierSetRecord Set(int number) => Data.Sets[number];

    /// <summary>A trainer's number by its id (<c>tower_tycoon_palmer_silver</c>); -1 for none.</summary>
    public static int NumberOf(string id) => TrainerNumbers.TryGetValue(id, out int number) ? number : -1;

    /// <summary><c>Pokemon_IsOnBattleFrontierBanlist</c>.</summary>
    public static bool IsBanned(string species) => BannedSpecies.Contains(species);
}
