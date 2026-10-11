using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle.Sim.Ai;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumEngine.Models.Frontier;

/// <summary>A Battle Hall challenge put down between two battles (the original's <c>BattleHallSave</c>): the type ranks are the stats' own.</summary>
public sealed class HallPause
{
    public FrontierChallenge Way { get; set; }
    public int Battle { get; set; }
    public List<int> Slots { get; set; } = new();
    public List<int> Trainers { get; set; } = new();
    public List<int> Pokemon { get; set; } = new();

    /// <summary>The species the challenge was entered with: one put down is only taken up again by it.</summary>
    public string Species { get; set; } = "";
}

/// <summary>
/// What the Battle Hall keeps beside the stats: a challenge put down, and every species' longest streak in each way
/// of taking the challenge (the original's <c>BattleHallWinRecords</c>, a block of the save of its own).
/// </summary>
public sealed class HallRecord
{
    public HallPause? Paused { get; set; }
    public Dictionary<string, int> Singles { get; set; } = new();
    public Dictionary<string, int> Doubles { get; set; } = new();

    public Dictionary<string, int> Of(FrontierChallenge way) => way == FrontierChallenge.Double ? Doubles : Singles;

    /// <summary><c>BattleHallWinRecords_GetRecordForSpecies</c>.</summary>
    public int RecordOf(FrontierChallenge way, string species) => Of(way).GetValueOrDefault(species);

    /// <summary>Every species' Single record added up (<c>ScrCmd_GetBattleHallTotalSinglesRecord</c>, uncapped: the record keeper's count).</summary>
    public int TotalSingles => Singles.Values.Sum();

    /// <summary><c>ScrCmd_GetNumSpeciesWithBattleHallRecords</c>: the species that have won a battle here in any way.</summary>
    public int SpeciesWithRecords => Singles.Where(r => r.Value > 0).Select(r => r.Key).Union(Doubles.Where(r => r.Value > 0).Select(r => r.Key)).Count();
}

/// <summary>What the record keeper of the lobby says (<c>ScrCmd_GetBattleHallRecordKeeperStats</c>'s four answers).</summary>
public enum HallKeeperAnswer
{
    /// <summary>No battle has been won here yet.</summary>
    Nothing,
    /// <summary>A milestone was passed since the last time, and its Battle Points are handed over.</summary>
    Reward,
    /// <summary>The next milestone is still to come.</summary>
    Waiting,
    /// <summary>Every milestone has been passed.</summary>
    AllPassed
}

/// <summary>
/// A Battle Hall challenge under way (plan 06 · R18c): the original's <c>BattleHall</c> and the functions that work
/// on it (<c>src/overlay104/battle_hall.c</c>, <c>battle_hall_helpers.c</c>, <c>frscrcmd_battle_hall.c</c>,
/// <c>src/scrcmd_battle_hall.c</c>, <c>src/battle_hall_win_records.c</c>). One Pokémon of level 30 or more goes in
/// (two of one species for a Double challenge) at its own level, and meets ten opponents on the stage, one Pokémon
/// each (two the same), healed before every battle. Before each the type of the Pokémon to be met is chosen on the
/// board; a type's rank goes up with every win over it, to ten, and with it the group its Pokémon are drawn from,
/// their IVs, their level and how their trainer thinks. Ten won earn Battle Points and the round after; the Hall
/// Matron stands in the way at battles 50 and 170 of a Single streak. A streak belongs to the species it was begun
/// with, and each species keeps a record of its own.
///
/// The original draws on the game's one generator (<c>LCRNG_Next</c>), so every draw here is on the sixteen-bit draw
/// the caller hands over, in the original's order; what was drawn is kept (the trainers and the Pokémon by their
/// numbers), which is what a challenge put down is taken up again from. No drawing or input.
/// </summary>
public sealed class BattleHallChallenge
{
    /// <summary><c>HALL_BATTLES_PER_ROUND</c>, <c>HALL_MAX_TYPE_RANK</c>, <c>HALL_MAX_DISTINCT_ROUNDS</c>, the level the lobby asks for, and <c>NUM_POKEMON_TYPES</c>.</summary>
    public const int BattlesPerRound = 10, MaxRank = 10, DistinctRounds = 18, MinLevel = 30, RankSlots = 18;

    /// <summary>
    /// The seventeen types in the board's order (<c>sBattleHallAppGridOrder</c>), four to a row: a type's place here is
    /// what its rank is kept under and what the trainers' classes go by, not the original's own numbering of types.
    /// </summary>
    public static readonly PokemonType[] Board =
    {
        PokemonType.Normal, PokemonType.Fire, PokemonType.Water, PokemonType.Electric,
        PokemonType.Grass, PokemonType.Ice, PokemonType.Fighting, PokemonType.Poison,
        PokemonType.Ground, PokemonType.Flying, PokemonType.Psychic, PokemonType.Bug,
        PokemonType.Rock, PokemonType.Ghost, PokemonType.Dragon, PokemonType.Dark,
        PokemonType.Steel
    };

    /// <summary>The Battle Points a round earns by the round the streak goes on to (<c>sBPPerRoundSolo</c>), and for beating the Hall Matron.</summary>
    public static readonly int[] PointsByRound = { 0, 1, 1, 1, 2, 2, 2, 3, 3, 3, 4, 4, 6, 6, 8, 8, 10, 10, 12 };
    public const int ArgentaPoints = 20;

    /// <summary>The IV of all six stats of a Pokémon met at each rank of its type (<c>sIVsPerTypeRank</c>); the Hall Matron's have 31.</summary>
    public static readonly int[] IvsByRank = { 8, 10, 12, 14, 16, 18, 20, 22, 24, 26 };

    /// <summary>
    /// What the record keeper hands over as every species' Single records, added up, pass each mark
    /// (<c>sBattleHallRecordMilestones</c>): the total and the Battle Points.
    /// </summary>
    public static readonly (int Record, int Points)[] Milestones =
    {
        (10, 1), (30, 3), (50, 5), (100, 5), (150, 5), (200, 5), (250, 5), (300, 5), (350, 5), (400, 5), (450, 5),
        (500, 10), (600, 10), (700, 10), (800, 10), (900, 10), (1000, 10),
        (1200, 30), (1400, 30), (1600, 30), (1800, 30),
        (2000, 50), (2500, 50), (3000, 50), (3500, 50), (4000, 50), (4500, 50), (5000, 50), (5500, 50), (6000, 50),
        (6500, 50), (7000, 50), (7500, 50), (8000, 50), (8500, 50), (9000, 50), (9500, 50),
        (10000, 100), (20000, 200), (30000, 300), (40000, 400), (50000, 500), (60000, 600), (70000, 700), (80000, 800),
        (90000, 900), (100000, 1000)
    };

    /// <summary>What a round won adds to the Trainer Card's score (<c>TRAINER_SCORE_EVENT_BATTLE_HALL_ROUND_COMPLETED</c>).</summary>
    public const int RoundScore = 7;

    /// <summary>The story's own variables the lobby keeps, as the original's scripts do.</summary>
    public const string WayVariable = "VAR_BATTLE_HALL_CHALLENGE_TYPE", MilestoneVariable = "VAR_BATTLE_HALL_NEXT_TOTAL_RECORD_MILESTONE";

    public FrontierChallenge Way { get; }

    /// <summary>The battles won in this round, 0 to 10 (<c>currentBattle</c>).</summary>
    public int Battle { get; private set; }

    /// <summary>The streak as it stands (<c>currentStreak</c>) and the rounds it has come through (<c>currentRound</c>).</summary>
    public int Streak { get; private set; }
    public int Round { get; private set; }

    /// <summary>The places in the team of the Pokémon entered: one, or two of one species (<c>partySlots</c>).</summary>
    public List<int> Slots { get; private set; } = new();

    /// <summary>The ranks of the seventeen types by their place on the board, and of the unnamed eighteenth (<c>typeRanks</c>): 0 to 10.</summary>
    public int[] Ranks { get; } = new int[RankSlots];

    /// <summary>
    /// The round's trainers, two places to a battle of which the second is only filled in a Double challenge
    /// (<c>trainerIDs</c>): nought where nobody was drawn, which the draw then takes for the first trainer.
    /// </summary>
    public int[] Trainers { get; private set; } = new int[BattlesPerRound * 2];

    /// <summary>The Pokémon met in each battle, by their numbers in the Hall's table (<c>monIndices</c>), laid out as the trainers are.</summary>
    public int[] Met { get; private set; } = new int[BattlesPerRound * 2];

    /// <summary>The place on the board of the type last chosen (<c>selectedTypeIdx</c>); -1 before any is.</summary>
    public int Cell { get; private set; } = -1;

    /// <summary>The level the Pokémon of the battle to come is at (<c>opponentsLevel</c>).</summary>
    public int OpponentsLevel { get; private set; }

    /// <summary>The square root of the entered Pokémon's level in the console's fixed point (<c>playerLevelSqrt</c>).</summary>
    public int LevelSqrt { get; private set; }

    /// <summary>The challenge was taken up again after a rest: the menu between battles comes first (<c>VAR_0x8003</c>).</summary>
    public bool Resumed { get; private set; }

    /// <summary>The round was won through (<c>saveStreak</c>): the streak runs on.</summary>
    public bool RoundWon { get; private set; }

    /// <summary>The Hall Matron was beaten in this challenge: for the silver print, or for the gold one.</summary>
    public FrontierPrint BeatArgenta { get; private set; }

    /// <summary>The opponents made for the battle to come (<see cref="MakeOpponent"/>), kept until it is fought.</summary>
    public FrontierOpponent? Standing { get; private set; }

    private BattleHallChallenge(FrontierChallenge way) => Way = way;

    // BattleFrontierStats_GetHallCurrentStreakIndex and its fellows: twelve numbers for each way
    private static FrontierStat Stat(FrontierChallenge way, int which) => (FrontierStat)((int)FrontierStat.HallCurrentStreakSingle + (int)way * 12 + which);

    /// <summary>The streak a challenge ended with, which a species' record is taken from (<c>STAT_HALL_CURRENT_STREAK_…</c>).</summary>
    public static FrontierStat CurrentStreakStat(FrontierChallenge way) => Stat(way, 0);
    public static FrontierStat LatestStreakStat(FrontierChallenge way) => Stat(way, 1);

    /// <summary>The species of the streak, by its number in the National Pokédex (<c>STAT_HALL_LATEST_SPECIES_…</c>).</summary>
    public static FrontierStat LatestSpeciesStat(FrontierChallenge way) => Stat(way, 2);

    /// <summary>The stat two types' ranks share, four bits each (<c>BattleFrontierStats_GetHallCurrentTypeRanksIndex</c>).</summary>
    public static FrontierStat RanksStat(FrontierChallenge way, int cell) => Stat(way, 3 + cell / 2);

    // ------------------------------------------------------------------ who may go in

    /// <summary><c>CheckPartyIsBattleHallEligible</c>, for one Pokémon: no Egg, of level 30 or more, and not on the banlist.</summary>
    public static bool MayEnter(Pokemon pokemon) => FrontierRules.MayEnter(pokemon) && pokemon.Level >= MinLevel;

    /// <summary>
    /// <c>CheckPartyIsBattleHallEligible</c>: a Single challenge wants one Pokémon that may go in, a Double one two
    /// of the same species.
    /// </summary>
    public static bool EnoughToEnter(Party party, FrontierChallenge way)
    {
        var eligible = party.Members.Where(MayEnter).ToList();
        if (way != FrontierChallenge.Double) return eligible.Count >= 1;
        return eligible.GroupBy(p => p.Species.Name).Any(g => g.Count() >= 2);
    }

    /// <summary>Whether the Pokémon chosen can take the challenge together: each allowed, and for a Double challenge both of one species.</summary>
    public static bool ValidEntry(Party party, IReadOnlyList<int> slots, FrontierChallenge way)
    {
        int needed = way == FrontierChallenge.Double ? 2 : 1;
        if (slots.Count != needed || slots.Distinct().Count() != needed || slots.Any(s => s < 0 || s >= party.Count)) return false;
        var chosen = slots.Select(s => party.Members[s]).ToList();
        return chosen.All(MayEnter) && chosen.All(p => p.Species.Name == chosen[0].Species.Name);
    }

    // ------------------------------------------------------------------ beginning

    /// <summary>
    /// <c>BattleHall_Init</c> for a new challenge: no battle won yet; where a streak of this way is still running,
    /// its wins and its types' ranks, and otherwise every rank back at nought. Whatever challenge was put down is
    /// gone (<c>BattleHallSave_Init</c>).
    /// </summary>
    public static BattleHallChallenge Begin(FrontierRecords records, FrontierChallenge way, IReadOnlyList<int> slots)
    {
        if (way == FrontierChallenge.Multi) throw new InvalidOperationException("The Battle Hall's Multi challenge needs another player");
        var challenge = new BattleHallChallenge(way) { Slots = slots.ToList() };
        records.Hall.Paused = null;
        if (records.IsRunning(FrontierFacility.Hall, (int)way)) challenge.Streak = records.Get(LatestStreakStat(way));
        else
            for (int cell = 0; cell < RankSlots; cell++) SaveRank(records, way, cell, 0);
        challenge.Round = challenge.Streak / BattlesPerRound;
        challenge.LoadRanks(records);
        return challenge;
    }

    /// <summary>
    /// <c>BattleHall_Init</c> for a challenge taken up again: as it was put down (<see cref="Pause"/>), its streak
    /// the latest kept and its ranks the stats'; null when none was.
    /// </summary>
    public static BattleHallChallenge? Resume(FrontierRecords records)
    {
        if (records.Hall.Paused is not { } paused) return null;
        var challenge = new BattleHallChallenge(paused.Way)
        {
            Battle = paused.Battle,
            Streak = records.Get(LatestStreakStat(paused.Way)),
            Slots = paused.Slots.ToList(),
            Trainers = Padded(paused.Trainers),
            Met = Padded(paused.Pokemon),
            Resumed = true
        };
        challenge.Round = challenge.Streak / BattlesPerRound;
        challenge.LoadRanks(records);
        records.Hall.Paused = null;
        return challenge;
    }

    private static int[] Padded(List<int> kept)
    {
        var all = new int[BattlesPerRound * 2];
        for (int i = 0; i < all.Length && i < kept.Count; i++) all[i] = kept[i];
        return all;
    }

    /// <summary>A challenge put down can only be taken up by the Pokémon it was entered with: still there, still allowed, still of that species.</summary>
    public static bool StillValid(FrontierRecords records, Party party) =>
        records.Hall.Paused is { } paused && ValidEntry(party, paused.Slots, paused.Way) && party.Members[paused.Slots[0]].Species.Name == paused.Species;

    // ------------------------------------------------------------------ the ranks

    private void LoadRanks(FrontierRecords records)
    {
        for (int cell = 0; cell < RankSlots; cell++) Ranks[cell] = LoadRank(records, Way, cell);
    }

    /// <summary><c>BattleHall_LoadTypeRanks</c>: a type's rank out of the stat it shares with its neighbour on the board, the lower four bits the first of the two.</summary>
    public static int LoadRank(FrontierRecords records, FrontierChallenge way, int cell) => records.Get(RanksStat(way, cell)) >> (cell % 2 * 4) & 0xF;

    /// <summary><c>BattleHall_SaveTypeRank</c>.</summary>
    public static void SaveRank(FrontierRecords records, FrontierChallenge way, int cell, int rank)
    {
        int pair = records.Get(RanksStat(way, cell)) & 0xFF;
        pair = cell % 2 == 0 ? pair & 0xF0 | rank & 0xF : pair & 0x0F | (rank & 0xF) << 4;
        records.Set(RanksStat(way, cell), pair);
    }

    /// <summary>The rank a type's cell of the board shows: one more than the wins over it, ten at most (<c>PrintTypesRanks</c>).</summary>
    public int ShownRank(int cell) => Math.Min(MaxRank, Ranks[cell] + 1);

    /// <summary>A type every one of whose ten ranks has been beaten can't be chosen (<c>GrayOutUnselectableCells</c>).</summary>
    public bool Beaten(int cell) => Ranks[cell] >= MaxRank;

    /// <summary>
    /// <c>BattleHall_CapTypeRanks</c>, before the board is opened: once all seventeen types have been beaten ten
    /// times each, every one goes back to its tenth rank, to be chosen again.
    /// </summary>
    public void CapRanks()
    {
        if (Enumerable.Range(0, Board.Length).All(Beaten))
            for (int cell = 0; cell < Board.Length; cell++) Ranks[cell] = MaxRank - 1;
    }

    // ------------------------------------------------------------------ the level

    /// <summary><c>BattleHall_GetHighestLevelInParty</c>: the level of the Pokémon entered, the higher of the two in a Double challenge.</summary>
    public int EnteredLevel(Party party) => Slots.Where(s => s >= 0 && s < party.Count).Select(s => party.Members[s].Level).DefaultIfEmpty(MinLevel).Max();

    /// <summary>
    /// The square root of a whole number in the console's fixed point, twelve bits after the point
    /// (<c>FX_Sqrt(FX32_CONST(level))</c>): the root of the number shifted up thirty-two more bits, rounded down, and
    /// then brought back down ten bits rounded to the nearest, as the console's own divider hands it over.
    /// </summary>
    public static int FixedSqrt(int value)
    {
        if (value <= 0) return 0;
        ulong shifted = (ulong)value << 12 << 32;
        ulong root = (ulong)Math.Sqrt(shifted);
        while (root * root > shifted) root--;
        while ((root + 1) * (root + 1) <= shifted) root++;
        return (int)(root + (1 << 9) >> 10);
    }

    /// <summary>
    /// <c>BattleHall_CalcOpponentsLevel</c>, in the original's single-precision arithmetic: the entered Pokémon's
    /// level less three times its square root, plus the chosen type's rank scaled by the level over five times that
    /// root, plus half a level for each other type already won over; rounded up, and never above the entered
    /// Pokémon's own level.
    /// </summary>
    public int CalcOpponentsLevel(int level)
    {
        int rank = Ranks[Cell];
        int baseLevel = (level << 12) - LevelSqrt * 3;
        float rankFactor = (float)(LevelSqrt / 4096f * 5.0);
        if (level / rankFactor < 1.0) rankFactor = rank;
        else rankFactor = (rank * level << 12) / 4096f / rankFactor;
        float types = 0f;
        for (int cell = 0; cell < RankSlots; cell++)
            if (cell == Cell || Ranks[cell] > 0) types += 1f;
        if (types != 0f) types -= 1f;
        types *= 0.5f;
        float exact = baseLevel / 4096f + rankFactor + types;
        int opponents = (int)exact;
        if (exact != opponents) opponents++;
        return Math.Min(Math.Min(opponents, level), 100);
    }

    // ------------------------------------------------------------------ the opponent

    private static bool IsArgenta(int trainer)
    {
        var (silver, gold) = FrontierBrains.Trainers(FrontierFacility.Hall);
        return trainer == silver || trainer == gold;
    }

    /// <summary>Whether the battle about to be fought is the Hall Matron's (<c>BH_FUNC_GET_NEXT_BATTLE_TYPE</c>): a Single streak of 49 or of 169.</summary>
    public FrontierPrint ArgentaNext => Way == FrontierChallenge.Single ? FrontierBrains.NextBattle(FrontierFacility.Hall, Streak) : FrontierPrint.None;

    /// <summary>
    /// The type chosen on the board and the opponent drawn for it (<c>BattleHall_GetTypeSelectionAppResult</c>,
    /// <c>BH_FUNC_SET_BASE_LEVEL</c>, <c>BattleHall_SetupNextOpponent</c>): the level the Pokémon will be at, the
    /// trainer's class and the trainer, and the Pokémon of the type.
    /// </summary>
    public void Choose(int cell, Party party, Func<int> next16)
    {
        if (cell < 0 || cell >= Board.Length) throw new InvalidOperationException("That is no type of the Battle Hall's board");
        Cell = cell;
        Resumed = false;
        Standing = null;
        int level = EnteredLevel(party);
        LevelSqrt = FixedSqrt(level);
        int rank = Ranks[cell];
        OpponentsLevel = CalcOpponentsLevel(level);

        int trainers = Way == FrontierChallenge.Single ? 1 : 2;
        var wanted = PickClasses(cell, trainers, rank, next16);
        PickTrainers(wanted, rank, next16);

        var print = Trainers[Battle * 2] switch
        {
            var t when t == FrontierBrains.Trainers(FrontierFacility.Hall).Silver => FrontierPrint.Silver,
            var t when t == FrontierBrains.Trainers(FrontierFacility.Hall).Gold => FrontierPrint.Gold,
            _ => FrontierPrint.None
        };
        string species = party.Members[Slots[0]].Species.Name;
        PickPokemon(Board[cell], rank, species, print, next16);
    }

    // BattleHall_CheckTypeRank
    private static int Capped(int rank) => Math.Min(rank, MaxRank - 1);

    /// <summary>
    /// <c>BattleHall_PickOpponentTrainerClasses</c>: a class for each trainer, one of the eight of the type's rank
    /// or, one time in three, one of the four the type itself brings.
    /// </summary>
    public static string[] PickClasses(int cell, int trainers, int rank, Func<int> next16)
    {
        var tables = FrontierDatabase.Hall;
        var wanted = new string[trainers];
        for (int i = 0; i < trainers; i++)
        {
            int roll = (next16() & 0xFFFF) % 12;
            wanted[i] = roll < 8 ? tables.ClassesByRank[Capped(rank)][roll] : tables.ClassesByType[cell][roll - 8];
        }
        return wanted;
    }

    /// <summary>
    /// <c>BattleHall_PickOpponentTrainers</c>: the Hall Matron at battles 50 and 170 of a Single streak; otherwise,
    /// for each class wanted, the first trainer of that class from a place drawn among all the Frontier's trainers
    /// onward who hasn't stood in this round. When the whole list holds none, another of the rank's classes is drawn
    /// and the search goes on from where it stands.
    /// </summary>
    private void PickTrainers(string[] wanted, int rank, Func<int> next16)
    {
        var tables = FrontierDatabase.Hall;
        int offset = Battle * 2;
        if (Way == FrontierChallenge.Single)
        {
            var (silverAt, goldAt) = FrontierBrains.Battles(FrontierFacility.Hall);
            var (silver, gold) = FrontierBrains.Trainers(FrontierFacility.Hall);
            int streak = Round * BattlesPerRound + Battle + 1;
            if (streak == silverAt) { Trainers[offset] = silver; return; }
            if (streak == goldAt) { Trainers[offset] = gold; return; }
        }
        int count = tables.TrainerClasses.Count, selected = 0;
        while (selected < wanted.Length)
        {
            int trainer = (next16() & 0xFFFF) % count, start = trainer;
            while (true)
            {
                string wantedClass = wanted[selected];
                if (wantedClass == tables.TrainerClasses[trainer] && !Trainers.Take(offset + selected).Contains(trainer))
                {
                    Trainers[offset + selected] = trainer;
                    selected++;
                    break;
                }
                if (++trainer >= count) trainer = 0;
                if (trainer != start) continue;
                string[] row = tables.ClassesByRank[Capped(rank)];
                string other;
                do other = row[(next16() & 0xFFFF) % row.Length];
                while (other == wantedClass);
                wanted[selected] = other;
            }
        }
    }

    /// <summary>The numbers a rank's Pokémon are drawn between (<c>sPotentialMatchupRangesByRank</c>): the first group for the first two ranks, the first two for the next three, the middle two for the three after, and the last two for the last two.</summary>
    public static (int Min, int Max) RangeOfRank(int rank)
    {
        var groups = FrontierDatabase.Hall.Groups;
        int last = FrontierDatabase.Hall.Pokemon.Count - 1;
        return Capped(rank) switch
        {
            0 or 1 => (groups[0], groups[1] - 1),
            2 or 3 or 4 => (groups[0], groups[2] - 1),
            5 or 6 or 7 => (groups[1], groups[3] - 1),
            _ => (groups[2], last)
        };
    }

    /// <summary>The numbers of one of the four groups (<c>sMatchupGroupRanges</c>).</summary>
    public static (int Min, int Max) RangeOfGroup(int group)
    {
        var groups = FrontierDatabase.Hall.Groups;
        return (groups[group], group + 1 < groups.Count ? groups[group + 1] - 1 : FrontierDatabase.Hall.Pokemon.Count - 1);
    }

    /// <summary>The group a species stands in (<c>BattleHall_PickNextOpponentPokemon</c>): the last, the strongest, for one the Hall's table doesn't hold.</summary>
    public static int GroupOf(string species)
    {
        var table = FrontierDatabase.Hall.Pokemon;
        int index = table.FindIndex(1, p => p.Species == species) - 1;
        if (index < 0) index = RangeOfGroup(3).Min;
        for (int group = 0; group < 4; group++)
            if (index < RangeOfGroup(group).Max) return group;
        return 3;
    }

    /// <summary>
    /// <c>BattleHall_PickNextOpponentPokemon</c>: from a place drawn in the range, the first Pokémon onward that is
    /// of the type chosen, isn't the challenger's own species and hasn't been met in this round. Once the search has
    /// come all the way round, only the one met last is kept out. The Hall Matron's is of any type, from the
    /// challenger's own group the first time and from the strongest the second. The walk steps over the last number
    /// of its range, as the original's does.
    /// </summary>
    private void PickPokemon(PokemonType type, int rank, string species, FrontierPrint print, Func<int> next16)
    {
        var table = FrontierDatabase.Hall;
        int current = Battle * 2;
        var (min, max) = print switch
        {
            FrontierPrint.Gold => RangeOfGroup(3),
            FrontierPrint.Silver => RangeOfGroup(GroupOf(species)),
            _ => RangeOfRank(rank)
        };
        int size = max - min + 1;
        int index = min + (next16() & 0xFFFF) % size - 1, start = index, steps = 0;
        bool looped = false;
        while (true)
        {
            bool free = !looped ? !Met.Take(current).Contains(index + 1) : current < 2 || Met[current - 2] != index + 1;
            // Our own guard, where the original would search for ever: after eight times round, whoever comes next
            if (free && (steps > size * 8 || table.Pokemon[index + 1].Species != species && (print != FrontierPrint.None || table.Types[index + 1].Contains(type))))
            {
                Met[current] = index + 1;
                return;
            }
            index++;
            if (index + 1 >= max) index = min - 1;
            if (index == start || ++steps > size * 4) looped = true;
        }
    }

    /// <summary>
    /// <c>BattleHall_GetAIMask</c>: the trainers of a type's first three ranks choose their moves at random, those
    /// of the next four with the basic routine alone, and those of the last three, like the Hall Matron, with all three.
    /// </summary>
    public AiFlags Mind
    {
        get
        {
            if (Way == FrontierChallenge.Single && IsArgenta(Trainers[Battle * 2])) return FrontierTrainers.Mind;
            int rank = Cell >= 0 ? Ranks[Cell] : 0;
            return rank + 1 >= 8 ? FrontierTrainers.Mind : rank + 1 >= 4 ? AiFlags.Basic : AiFlags.None;
        }
    }

    /// <summary>
    /// The trainer of the battle about to be fought with their Pokémon (<c>BattleHall_SetupBattle</c>,
    /// <c>BattleHall_LoadOpponentMonData</c>): the number it carries and a personality for its nature are drawn, the
    /// personality is moved on by the original's step until it isn't shiny, and in a Double challenge the second
    /// Pokémon is the same one again, a step back. Its IVs go by the rank of the type, its level is the one worked
    /// out as the type was chosen; the Hall Matron's has 31 in every stat and the challenger's own level.
    /// </summary>
    public FrontierOpponent MakeOpponent(Party party, Func<int> next16)
    {
        int offset = Battle * 2, trainer = Trainers[offset];
        bool matron = IsArgenta(trainer);
        int rank = Cell >= 0 ? Ranks[Cell] : 0;
        int ivs = matron ? 31 : IvsByRank[Capped(rank)];
        int level = matron ? EnteredLevel(party) : OpponentsLevel;
        var set = FrontierDatabase.Hall.Pokemon[Met[offset]];

        uint otId = ((uint)next16() & 0xFFFF) | ((uint)next16() & 0xFFFF) << 16;
        uint personality = FrontierTrainers.PersonalityFor(set.Nature, otId, next16);
        if (Way == FrontierChallenge.Double)
        {
            // The second place of the battle is loaded too, though nothing was drawn for it: the archive's empty set, with a number and a personality of its own
            uint unused = ((uint)next16() & 0xFFFF) | ((uint)next16() & 0xFFFF) << 16;
            FrontierTrainers.PersonalityFor(FrontierDatabase.Hall.Pokemon[Met[offset + 1]].Nature, unused, next16);
        }

        var pokemon = new List<Pokemon>();
        var personalities = new List<uint>();
        for (int i = 0, n = Way == FrontierChallenge.Double ? 2 : 1; i < n; i++)
        {
            personality = Step(personality, otId);
            personalities.Add(personality);
            pokemon.Add(FrontierTrainers.Build(set, personality, ivs, level));
        }
        return Standing = new FrontierOpponent { Number = trainer, Sets = new List<int> { Met[offset] }, Pokemon = pokemon, OtId = otId, Personalities = personalities };
    }

    /// <summary>
    /// <c>BattleHall_SetMonsPersonality</c>: the personality goes up by twenty-five times 10,001 (down again once it
    /// is past that), which keeps its nature, until it isn't a shiny one.
    /// </summary>
    public static uint Step(uint personality, uint otId)
    {
        const uint step = 25 * 10001;
        for (int tries = 0; tries < 64; tries++)
        {
            personality = personality > step ? personality - step : personality + step;
            if (!Personality.IsShiny(otId, personality, 8192)) break;
        }
        return personality;
    }

    /// <summary>The entered Pokémon as a team for a battle, whole and at their own level (<c>Party_HealAllMembers</c>, <c>Pokemon_Copy</c>).</summary>
    public Party TeamForBattle(Party party) => FrontierRules.TeamFor(party, Slots, 100);

    // ------------------------------------------------------------------ battle by battle

    /// <summary>The Hall Matron was beaten (the stage's script): the print waits in the lobby, the first time for each.</summary>
    public void BeatMatron(StoryState story, FrontierPrint which)
    {
        BeatArgenta = which;
        FrontierBrains.Beaten(story, FrontierFacility.Hall, which);
    }

    /// <summary>
    /// After a battle won (<c>RECORD_BATTLE_HALL_VICTORIES</c>, <c>BH_FUNC_INCREMENT_RANK_OF_BEATEN_TYPE</c>,
    /// <c>BH_FUNC_INCREMENT_CURRENT_BATTLE</c>, <c>BH_FUNC_INCREMENT_CURRENT_STREAK</c>): one more for the record of
    /// battles won here, the chosen type's rank up by one, and one more for the round and for the streak. True once
    /// the round's ten are beaten.
    /// </summary>
    public bool Won(FrontierRecords records)
    {
        records.CountVictories(FrontierFacility.Hall, 1);
        if (Cell >= 0 && Ranks[Cell] < MaxRank) Ranks[Cell]++;
        Battle++;
        if (Streak < FrontierRecords.Most) Streak++;
        Standing = null;
        return Battle >= BattlesPerRound;
    }

    // ------------------------------------------------------------------ the end of a round, and of a challenge

    // BattleHall_Save: the latest streak, the ranks and the species always; the streak a record is taken from and
    // whether it runs on unless the challenge is only being put down
    private void Save(FrontierRecords records, Party party, bool pausing)
    {
        records.Set(LatestStreakStat(Way), Streak);
        if (!pausing)
        {
            records.Set(CurrentStreakStat(Way), Streak);
            records.SetRunning(FrontierFacility.Hall, (int)Way, RoundWon);
        }
        for (int cell = 0; cell < RankSlots; cell++) SaveRank(records, Way, cell, Ranks[cell]);
        records.Set(LatestSpeciesStat(Way), SpeciesOf(party)?.DexNumber ?? 0);
    }

    private PokemonSpecies? SpeciesOf(Party party) => Slots.Count > 0 && Slots[0] >= 0 && Slots[0] < party.Count ? party.Members[Slots[0]].Species : null;

    /// <summary>
    /// <c>BattleHall_SaveOnCompletingRound</c>, when all ten are beaten: the streak runs on, the next round is one
    /// further (up to the eighteenth) and begins with no battle won. The caller adds <see cref="RoundScore"/> to the
    /// trainer's score.
    /// </summary>
    public void EndRound(FrontierRecords records, Party party)
    {
        RoundWon = true;
        if (Round < DistinctRounds) Round++;
        Battle = 0;
        Save(records, party, pausing: false);
    }

    /// <summary><c>BattleHall_SaveOnLoss</c>, when a battle is lost or the challenge given up: the streak ends where it stands, and every type is back at its first rank.</summary>
    public void EndLost(FrontierRecords records, Party party)
    {
        RoundWon = false;
        Array.Clear(Ranks);
        Save(records, party, pausing: false);
    }

    /// <summary>
    /// <c>BattleHall_UpdateWinRecordForCurrentMon</c>, as a challenge is left: the species' record in this way is
    /// the streak the challenge ended with, if that is longer. True when the record moved.
    /// </summary>
    public bool UpdateRecord(FrontierRecords records)
    {
        if (PokemonDatabase.GetByDex(records.Get(LatestSpeciesStat(Way))) is not { } species) return false;
        int streak = records.Get(CurrentStreakStat(Way)), kept = records.Hall.RecordOf(Way, species.Name);
        if (kept < streak) records.Hall.Of(Way)[species.Name] = Math.Min(FrontierRecords.Most, streak);
        return kept != streak;
    }

    /// <summary>
    /// <c>BattleHall_GetEarnedBP</c>, after <see cref="EndRound"/>: by the round the streak goes on to, twelve from
    /// the eighteenth on, and twenty for a Single round that ended with the Hall Matron beaten (a streak of 50 or of 170).
    /// </summary>
    public int PointsEarned
    {
        get
        {
            var (silverAt, goldAt) = FrontierBrains.Battles(FrontierFacility.Hall);
            if (Way == FrontierChallenge.Single && (Streak == silverAt || Streak == goldAt)) return ArgentaPoints;
            return PointsByRound[Math.Min(Round, DistinctRounds)];
        }
    }

    /// <summary><c>BH_FUNC_QUICKSAVE</c>: the challenge is put down between two battles, to be taken up where it stands.</summary>
    public void Pause(FrontierRecords records, Party party)
    {
        Save(records, party, pausing: true);
        records.Hall.Paused = new HallPause
        {
            Way = Way,
            Battle = Battle,
            Slots = Slots.ToList(),
            Trainers = Trainers.ToList(),
            Pokemon = Met.ToList(),
            Species = SpeciesOf(party)?.Name ?? ""
        };
    }

    /// <summary>
    /// <c>BattleHall_ClearActiveStreak</c>: a streak is let go of, because another species is entered or a challenge
    /// was left without being put down: no longer running, its latest streak nought.
    /// </summary>
    public static void Forfeit(FrontierRecords records, FrontierChallenge way)
    {
        records.SetRunning(FrontierFacility.Hall, (int)way, false);
        records.Set(LatestStreakStat(way), 0);
        records.Hall.Paused = null;
    }

    /// <summary>The species a running streak of this way belongs to (<c>BH_LOBBY_FUNC_GET_CURRENT_STREAK_SPECIES</c>); null with none running.</summary>
    public static string? StreakSpecies(FrontierRecords records, FrontierChallenge way) =>
        records.IsRunning(FrontierFacility.Hall, (int)way) ? PokemonDatabase.GetByDex(records.Get(LatestSpeciesStat(way)))?.Name : null;

    // ------------------------------------------------------------------ the lobby's people

    /// <summary>
    /// <c>ScrCmd_GetBattleHallRecordKeeperStats</c>: every species' Single record added up, set against the marks
    /// not yet passed. Each mark passed since the last time moves <paramref name="next"/> on and adds its Battle
    /// Points, which are handed over. What is told: the total, the last mark passed, the mark to come.
    /// </summary>
    public static (HallKeeperAnswer Answer, int Total, int Passed, int Coming, int Points) AskKeeper(FrontierRecords records, ref int next)
    {
        int total = records.Hall.TotalSingles, before = next, earned = 0, reached = 0;
        for (int i = next; i < Milestones.Length; i++)
        {
            if (Milestones[i].Record > total) continue;
            earned += Milestones[i].Points;
            next++;
            reached = i;
        }
        if (earned != 0) records.GivePoints(earned);
        var answer = total == 0 ? HallKeeperAnswer.Nothing : before != next ? HallKeeperAnswer.Reward : HallKeeperAnswer.Waiting;
        if (total != 0 && before >= Milestones.Length) answer = HallKeeperAnswer.AllPassed;
        // The original reads one past its table once the last mark is passed: the last mark stands for it here
        int coming = Milestones[Math.Min(next, Milestones.Length - 1)].Record;
        return (answer, total, Milestones[reached].Record, coming, earned);
    }

    /// <summary>
    /// Where the fan who follows the challenger's records stands in the lobby (<c>BattleHall_OnResume</c>): by the
    /// door until ten species have a record, nearer from the eleventh, and beside the counter from the hundred and first.
    /// </summary>
    public static int FanStage(FrontierRecords records) => records.Hall.SpeciesWithRecords switch { >= 101 => 2, >= 11 => 1, _ => 0 };

    /// <summary>Which of the fan's ten things to say fits the species with a record (<c>BattleHall_WinstonMessages</c>): 0 for none, 9 from 475.</summary>
    public static int FanTalk(FrontierRecords records)
    {
        int species = records.Hall.SpeciesWithRecords;
        int[] marks = { 1, 11, 31, 51, 101, 151, 251, 351, 475 };
        return marks.Count(m => species >= m);
    }

    /// <summary>
    /// Which well-known visitors may turn up in the lobby (<c>BattleHall_PickMajorNPC</c>), by every species' Single
    /// record added up and capped at ten thousand: nobody under 500, one from there, two from a thousand, four from
    /// ten thousand.
    /// </summary>
    public static int Visitors(FrontierRecords records) => Math.Min(10000, records.Hall.TotalSingles) switch { >= 10000 => 4, >= 1000 => 2, >= 500 => 1, _ => 0 };
}
