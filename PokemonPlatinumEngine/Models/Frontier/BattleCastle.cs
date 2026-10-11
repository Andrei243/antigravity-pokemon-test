using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle.Sim.Ai;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumEngine.Models.Frontier;

/// <summary>One of the challenger's Pokémon as a Battle Castle challenge put down keeps it (<c>CASTLE_SAVE_MON_HP</c>, <c>…_PP</c>, <c>…_HELD_ITEMS</c>).</summary>
public sealed class CastleKept
{
    public int Hp { get; set; }
    public List<int> Pp { get; set; } = new();
    public string? Item { get; set; }
}

/// <summary>A Battle Castle challenge put down between two battles (what <c>BattleCastle_Save</c> writes).</summary>
public sealed class CastlePause
{
    public FrontierChallenge Way { get; set; }
    public int Battle { get; set; }
    public List<int> Slots { get; set; } = new();
    public List<int> Trainers { get; set; } = new();
    public List<int> FoeSets { get; set; } = new();
    public List<CastleKept> Team { get; set; } = new();

    /// <summary>The species entered: a challenge put down is only taken up again by them.</summary>
    public List<string> Species { get; set; } = new();
}

/// <summary>What the Battle Castle keeps beside the stats (the original's <c>BattleCastleSave</c>, its persistent part and its game record).</summary>
public sealed class CastleRecord
{
    public CastlePause? Paused { get; set; }

    /// <summary><c>CASTLE_SAVE_HAVE_HEARD_INTRO</c>: the valet has explained once why items are taken at the door.</summary>
    public bool HeardIntro { get; set; }

    /// <summary><c>RECORD_CASTLE_POINTS_RECEIVED</c>.</summary>
    public int PointsReceived { get; set; }
}

/// <summary>The three things a rank is bought for at the Castle's counters (<c>BATTLE_CASTLE_RANK_…</c>).</summary>
public enum CastleRank { Healing, Rentals, Summary }

/// <summary>What is restored for Castle Points (<c>MENU_ENTRY_RESTORE_…</c>).</summary>
public enum CastleHeal { Hp, Pp, All }

/// <summary>How something asked for at a counter came out.</summary>
public enum CastleAnswer
{
    Done,
    /// <summary>Too few Castle Points.</summary>
    NotEnough,
    /// <summary>The rank of this counter doesn't reach it yet.</summary>
    RankTooLow,
    /// <summary>It would change nothing: HP and PP are full, the level can't go further, the rank is the highest.</summary>
    NoUse,
    /// <summary>The Pokémon holds something already: asked again, the two are swapped.</summary>
    Holding
}

/// <summary>What has been done to one of the opponent's Pokémon's level (<c>LEVEL_ADJUSTMENT_…</c>).</summary>
public enum CastleLevel { Same, Up, Down }

/// <summary>
/// A Battle Castle challenge under way (plan 06 · R18c): the original's <c>BattleCastle</c> and the functions that
/// work on it (<c>src/overlay104/battle_castle.c</c>, <c>battle_castle_helpers.c</c>, <c>frscrcmd_battle_castle.c</c>)
/// with the rules of the valets' two counters (<c>src/applications/frontier/battle_castle/self_app.c</c>,
/// <c>opponent_app.c</c>). Three Pokémon go in, brought down to level 50 and with nothing held, and meet seven
/// trainers one after another. Nothing is restored between battles: HP, PP and what is held carry on, and only a
/// status and a faint are undone. Every battle won earns Castle Points by how it was won, and they buy what the
/// challenge otherwise goes without: HP and PP back, berries and items to hold, a look at the next trainer's Pokémon,
/// their levels moved, their stats and moves told, a battle passed, and the ranks that open more of each. The points
/// and ranks carry from round to round while the streak runs; the Castle Valet stands in the way at battles 21 and
/// 49 of a Single streak.
///
/// The original draws on the game's one generator (<c>LCRNG_Next</c>), so every draw here is on the sixteen-bit draw
/// the caller hands over, in the original's order. No drawing or input.
/// </summary>
public sealed class BattleCastleChallenge
{
    /// <summary><c>CASTLE_BATTLES_PER_ROUND</c>, <c>CASTLE_PARTY_SIZE_SOLO</c>, <c>CASTLE_MAX_DISTINCT_ROUNDS</c>, <c>CASTLE_MAX_LEVEL</c>.</summary>
    public const int BattlesPerRound = 7, TeamSize = 3, DistinctRounds = 8, Level = 50;

    /// <summary>The Castle Points a new round begins with over what was carried, and what passing a battle costs (<c>BattleCastle_Init</c>, the corridor's scene).</summary>
    public const int RoundPoints = 10, PassPrice = 50;

    /// <summary>The round from which a battle may be passed (the corridor's scene: the fourth).</summary>
    public const int PassFromRound = 3;

    /// <summary>What restoring costs, by what is restored, and the rank of the counter each asks for (<c>sHealingCosts</c>, <c>sHealMenuEntries</c>).</summary>
    public static readonly int[] HealPrices = { 10, 8, 12 };

    /// <summary>What the next rank of each counter costs, by the rank held (<c>sRankUpCosts</c>, and <c>CP_COST_RANK_UP</c> for the third, which has only two ranks).</summary>
    public static readonly int[][] RankPrices = { new[] { 0, 100, 100 }, new[] { 0, 100, 150 }, new[] { 0, 50, 50 } };
    public static readonly int[] HighestRank = { 3, 3, 2 };

    /// <summary><c>CP_COST_CHECK_IDENTITY</c>, <c>…_LEVEL_UP</c>, <c>…_LEVEL_DOWN</c>, <c>…_SHOW_STATS</c>, <c>…_SHOW_MOVES</c>.</summary>
    public const int IdentifyPrice = 1, LevelUpPrice = 1, LevelDownPrice = 15, StatsPrice = 2, MovesPrice = 5;

    /// <summary>The Battle Points a round earns by the round the streak goes on to (<c>sBPPerRoundSolo</c>), and for beating the Castle Valet.</summary>
    public static readonly int[] PointsByRound = { 0, 3, 3, 4, 4, 5, 5, 7, 7 };
    public const int DarachPoints = 20;

    /// <summary>What a round won adds to the Trainer Card's score (as every facility's round does).</summary>
    public const int RoundScore = 7;

    public const string WayVariable = "VAR_BATTLE_CASTLE_CHALLENGE_TYPE";

    public FrontierChallenge Way { get; }

    /// <summary>The battles won (or passed) in this round, 0 to 7 (<c>currentBattle</c>).</summary>
    public int Battle { get; private set; }
    public int Streak { get; private set; }
    public int Round { get; private set; }

    /// <summary>The places in the team of the three entered (<c>partySlots</c>).</summary>
    public List<int> Slots { get; private set; } = new();

    /// <summary>
    /// The three as the challenge carries them (<c>playersParty</c>): copies at level 50 or under, with nothing
    /// held but what was rented, whose HP and PP are whatever the battles left.
    /// </summary>
    public Party Team { get; private set; } = new();

    /// <summary>The PP the three went into the last battle with, as the original counts it (<c>startingPP</c>): what a battle used is told from it.</summary>
    public int[][] StartingPp { get; private set; } = Enumerable.Range(0, TeamSize).Select(_ => new int[4]).ToArray();

    /// <summary>The round's trainers by their numbers: fourteen, as the original draws them, of whom the first seven are met.</summary>
    public List<int> Trainers { get; private set; } = new();

    /// <summary>The trainer to come with their three (<c>opponentsParty</c>), as the counter may have changed them.</summary>
    public FrontierOpponent? Foe { get; private set; }

    /// <summary>What has been bought about each of the trainer's three (<c>appIdentityUnlocked</c>, <c>levelAdjustments</c>, <c>appStatsUnlocked</c>, <c>appMovesUnlocked</c>).</summary>
    public bool[] Identified { get; } = new bool[TeamSize];
    public CastleLevel[] Levels { get; } = new CastleLevel[TeamSize];
    public bool[] StatsShown { get; } = new bool[TeamSize];
    public bool[] MovesShown { get; } = new bool[TeamSize];

    /// <summary>One of the three fainted in the battle just won (<c>monFainted</c>): the valet says it was revived.</summary>
    public bool Fainted { get; private set; }

    public bool Resumed { get; private set; }

    /// <summary>The Castle Valet has been announced (<c>seenCastleValetIntro</c>).</summary>
    public bool ValetAnnounced { get; private set; }
    public bool RoundWon { get; private set; }
    public FrontierPrint BeatDarach { get; private set; }

    private BattleCastleChallenge(FrontierChallenge way) => Way = way;

    // BattleFrontierStats_GetCastleRecordStreakIndex and its fellows: eight numbers for each way
    private static FrontierStat Stat(FrontierChallenge way, int which) => (FrontierStat)((int)FrontierStat.CastleRecordStreakSingle + (int)way * 8 + which);

    public static FrontierStat RecordStreakStat(FrontierChallenge way) => Stat(way, 0);
    public static FrontierStat LatestStreakStat(FrontierChallenge way) => Stat(way, 1);
    public static FrontierStat LatestPointsStat(FrontierChallenge way) => Stat(way, 2);
    public static FrontierStat SpentPointsStat(FrontierChallenge way) => Stat(way, 3);
    public static FrontierStat RecordPointsStat(FrontierChallenge way) => Stat(way, 4);
    public static FrontierStat RankStat(FrontierChallenge way, CastleRank rank) => Stat(way, 5 + (int)rank);

    // ------------------------------------------------------------------ who may go in

    /// <summary>
    /// <c>BC_LOBBY_FUNC_CHECK_PARTY_ELIGIBLE</c> (<c>sub_02049EC4</c> told not to look at items, since they are taken
    /// at the door): three Pokémon that may enter, no two of one species.
    /// </summary>
    public static bool EnoughToEnter(Party party) =>
        party.Members.Where(FrontierRules.MayEnter).Select(p => p.Species.Name).Distinct().Count() >= TeamSize;

    /// <summary>Whether the three chosen can go in together: each allowed, and no two of one species.</summary>
    public static bool ValidEntry(Party party, IReadOnlyList<int> slots)
    {
        if (slots.Count != TeamSize || slots.Distinct().Count() != TeamSize || slots.Any(s => s < 0 || s >= party.Count)) return false;
        var chosen = slots.Select(s => party.Members[s]).ToList();
        return chosen.All(FrontierRules.MayEnter) && chosen.Select(p => p.Species.Name).Distinct().Count() == TeamSize;
    }

    // ------------------------------------------------------------------ beginning

    /// <summary>
    /// <c>BattleCastle_Init</c> for a new challenge: no battle won yet. Where a streak of this way is still running,
    /// its wins, its Castle Points and its ranks carry on; otherwise the points are nought and every rank the
    /// first. Either way ten Castle Points are added for the round. The three go in as copies, nothing held, brought
    /// down to level 50 where they are above it.
    /// </summary>
    public static BattleCastleChallenge Begin(FrontierRecords records, FrontierChallenge way, Party party, IReadOnlyList<int> slots)
    {
        if (way == FrontierChallenge.Multi) throw new InvalidOperationException("The Battle Castle's Multi challenge needs another player");
        var challenge = new BattleCastleChallenge(way) { Slots = slots.ToList() };
        records.Castle.Paused = null;
        if (records.IsRunning(FrontierFacility.Castle, (int)way)) challenge.Streak = records.Get(LatestStreakStat(way));
        else
        {
            records.Set(LatestPointsStat(way), 0);
            records.Set(SpentPointsStat(way), 0);
            foreach (var rank in Enum.GetValues<CastleRank>()) records.Set(RankStat(way, rank), 1);
        }
        challenge.Round = challenge.Streak / BattlesPerRound;
        records.Add(LatestPointsStat(way), RoundPoints);
        challenge.TakeTeam(party);
        return challenge;
    }

    private void TakeTeam(Party party)
    {
        Team = new Party();
        foreach (int slot in Slots)
        {
            var copy = FrontierRules.ForBattle(party.Members[slot], Level);
            copy.HeldItem = null;
            Team.Add(copy);
        }
    }

    /// <summary>
    /// <c>BattleCastle_Init</c> and <c>LoadTrainersAndMonsFromSave</c> for a challenge taken up again: the three with
    /// the HP, PP and items they were put down with, and the round's trainers. The trainer just beaten, whose three
    /// the original makes once more here from the sets it kept (with IVs read from a place it never wrote), is left
    /// alone: the next one is drawn as the challenge goes on. The PP a battle began with isn't counted again, as in
    /// the original, so the first battle after a rest counts as having used none.
    /// </summary>
    public static BattleCastleChallenge? Resume(FrontierRecords records, Party party)
    {
        if (records.Castle.Paused is not { } paused) return null;
        var challenge = new BattleCastleChallenge(paused.Way)
        {
            Battle = paused.Battle,
            Streak = records.Get(LatestStreakStat(paused.Way)),
            Slots = paused.Slots.ToList(),
            Trainers = paused.Trainers.ToList(),
            Resumed = true
        };
        challenge.Round = challenge.Streak / BattlesPerRound;
        challenge.TakeTeam(party);
        for (int i = 0; i < challenge.Team.Count && i < paused.Team.Count; i++)
        {
            var mon = challenge.Team.Members[i];
            var kept = paused.Team[i];
            mon.CurrentHP = Math.Clamp(kept.Hp, 0, mon.MaxHP);
            for (int m = 0; m < mon.Moves.Count && m < kept.Pp.Count; m++) mon.Moves[m].CurrentPP = Math.Clamp(kept.Pp[m], 0, mon.Moves[m].MaxPP);
            mon.HeldItem = kept.Item != null ? ItemDatabase.Get(kept.Item) : null;
        }
        // startingPP is left as the original leaves it on this path: all nought
        records.Castle.Paused = null;
        return challenge;
    }

    /// <summary>A challenge put down can only be taken up by the three it was entered with.</summary>
    public static bool StillValid(FrontierRecords records, Party party) =>
        records.Castle.Paused is { } paused && ValidEntry(party, paused.Slots)
        && paused.Slots.Select(s => party.Members[s].Species.Name).SequenceEqual(paused.Species);

    /// <summary>
    /// <c>SelectTrainersAndFirstBattlesMons</c>, as a round begins: the PP the three go in with is noted, the
    /// round's fourteen trainers are drawn and the first of them is given their three.
    /// </summary>
    public void DrawRound(Func<int> next16)
    {
        NotePp();
        Trainers = PickTrainers(Way, Round, BattlesPerRound * 2, next16);
        DrawFoe(next16);
    }

    private void NotePp()
    {
        for (int i = 0; i < TeamSize; i++)
            for (int m = 0; m < 4; m++)
                StartingPp[i][m] = i < Team.Count && m < Team.Members[i].Moves.Count ? Team.Members[i].Moves[m].CurrentPP : 0;
    }

    /// <summary>
    /// <c>BattleCastle_GetOpponentTrainerID</c>: who stands at a place of a round. In a Single challenge the battle
    /// that would be the streak's 21st is the Castle Valet's and the 49th his again; otherwise the trainer is drawn
    /// from the round's row, the seventh of each seven from its second half. The row's last trainer is never drawn.
    /// </summary>
    public static int TrainerFor(FrontierChallenge way, int round, int place, Func<int> next16)
    {
        var rows = FrontierDatabase.Castle.Opponents;
        if (way == FrontierChallenge.Single)
        {
            var (silverAt, goldAt) = FrontierBrains.Battles(FrontierFacility.Castle);
            var (silver, gold) = FrontierBrains.Trainers(FrontierFacility.Castle);
            int streak = round * BattlesPerRound + place + 1;
            if (streak == silverAt) return silver;
            if (streak == goldAt) return gold;
        }
        int[] row = rows[Math.Min(round, rows.Count - 1)];
        bool last = place == BattlesPerRound - 1 || place == BattlesPerRound * 2 - 1;
        int min = last ? row[2] : row[0], span = last ? row[3] - row[2] : row[1] - row[0];
        return min + (next16() & 0xFFFF) % span;
    }

    /// <summary><c>BattleCastle_PickOpponentTrainers</c>: a round's trainers, none twice.</summary>
    public static List<int> PickTrainers(FrontierChallenge way, int round, int count, Func<int> next16)
    {
        var trainers = new List<int>();
        while (trainers.Count < count)
        {
            int trainer = TrainerFor(way, round, trainers.Count, next16);
            if (!trainers.Contains(trainer)) trainers.Add(trainer);
        }
        return trainers;
    }

    /// <summary>
    /// <c>BattleCastle_SetupNextOpponentsParty</c>: the trainer of the battle to come is given their three, by the
    /// draw every facility but the Tower shares (<see cref="FrontierTrainers.ForTrainers"/>), at level 50.
    /// </summary>
    public void DrawFoe(Func<int> next16)
    {
        Foe = FrontierTrainers.ForTrainers(TeamSize, Trainers[Battle], Trainers[Battle + BattlesPerRound], false, next16, Level)[0];
        Resumed = false;
    }

    // ------------------------------------------------------------------ Castle Points and ranks

    /// <summary>The Castle Points held (<c>STAT_CASTLE_LATEST_CP_…</c>).</summary>
    public int Points(FrontierRecords records) => records.Get(LatestPointsStat(Way));

    /// <summary><c>BattleCastleApp_GetRank</c>: 1 to 3.</summary>
    public int Rank(FrontierRecords records, CastleRank rank) => Math.Max(1, records.Get(RankStat(Way, rank)));

    /// <summary><c>BattleCastle_ReceiveCastlePoints</c>.</summary>
    public void Receive(FrontierRecords records, int points)
    {
        records.Add(LatestPointsStat(Way), points);
        records.Castle.PointsReceived = Math.Min(999_999, records.Castle.PointsReceived + points);
    }

    /// <summary><c>BattleCastle_SpendCastlePoints</c>: what is spent is counted too.</summary>
    public void Spend(FrontierRecords records, int points)
    {
        records.Subtract(LatestPointsStat(Way), points);
        records.Add(SpentPointsStat(Way), points);
    }

    /// <summary>
    /// A counter's next rank is bought (<c>IncreaseRank</c>): the healing counter's second lets PP be restored and
    /// its third both at once, the rental counter's second lends items and its third more of them and of the berries,
    /// and the second of the third counter tells an opponent's moves.
    /// </summary>
    public CastleAnswer RankUp(FrontierRecords records, CastleRank rank)
    {
        int held = Rank(records, rank);
        if (held >= HighestRank[(int)rank]) return CastleAnswer.NoUse;
        int price = RankPrices[(int)rank][held];
        if (Points(records) < price) return CastleAnswer.NotEnough;
        Spend(records, price);
        records.Set(RankStat(Way, rank), held + 1);
        return CastleAnswer.Done;
    }

    /// <summary>What the next rank of a counter costs; 0 once it is the highest.</summary>
    public int RankPrice(FrontierRecords records, CastleRank rank)
    {
        int held = Rank(records, rank);
        return held >= HighestRank[(int)rank] ? 0 : RankPrices[(int)rank][held];
    }

    // ------------------------------------------------------------------ the first counter: one's own three

    private static bool MissingPp(Pokemon pokemon) => pokemon.Moves.Any(m => m.CurrentPP != m.MaxPP);

    /// <summary>
    /// One of the three is restored (<c>MAIN_SUBSTATE_HEAL_YES_NO</c>, <c>HealPokemon</c>): its HP for ten points,
    /// its PP for eight from the counter's second rank, both for twelve from its third. Asked in the original's
    /// order: the rank, the points, and whether there is anything to restore.
    /// </summary>
    public CastleAnswer Heal(FrontierRecords records, int slot, CastleHeal what)
    {
        var pokemon = Team.Members[slot];
        if (Rank(records, CastleRank.Healing) < (int)what + 1) return CastleAnswer.RankTooLow;
        if (Points(records) < HealPrices[(int)what]) return CastleAnswer.NotEnough;
        bool hp = pokemon.CurrentHP != pokemon.MaxHP, pp = MissingPp(pokemon);
        if (what switch { CastleHeal.Hp => !hp, CastleHeal.Pp => !pp, _ => !hp && !pp }) return CastleAnswer.NoUse;
        Spend(records, HealPrices[(int)what]);
        // A Max Potion, a Max Elixir, or one after the other
        if (what != CastleHeal.Pp && pokemon.CurrentHP > 0) pokemon.CurrentHP = pokemon.MaxHP;
        if (what != CastleHeal.Hp)
            foreach (var move in pokemon.Moves) move.RestorePP();
        return CastleAnswer.Done;
    }

    /// <summary>What the rental counter lends at its rank: the berries, or the items (none of those at the first rank).</summary>
    public IReadOnlyList<FrontierPrizeRecord> Lent(FrontierRecords records, bool berries)
    {
        var tables = FrontierDatabase.Castle;
        int rank = Rank(records, CastleRank.Rentals);
        if (!berries && rank < 2) return Array.Empty<FrontierPrizeRecord>();
        return berries ? tables.Berries.Take(tables.BerriesByRank[rank - 1]).ToList() : tables.Items.Take(tables.ItemsByRank[rank - 1]).ToList();
    }

    /// <summary>
    /// A berry or an item is rented for one of the three to hold (<c>MAIN_SUBSTATE_SELECT_ITEM_OR_BERRY_YES_NO</c>,
    /// <c>RentItem</c>). One holding something already is asked about first (<see cref="CastleAnswer.Holding"/>);
    /// with <paramref name="swap"/> what it held is given up for the new one.
    /// </summary>
    public CastleAnswer Rent(FrontierRecords records, int slot, string item, bool swap = false)
    {
        var pokemon = Team.Members[slot];
        var lent = Lent(records, true).Concat(Lent(records, false)).FirstOrDefault(l => l.Item == item);
        if (lent == null) return CastleAnswer.RankTooLow;
        if (Points(records) < lent.Price) return CastleAnswer.NotEnough;
        if (pokemon.HeldItem != null && !swap) return CastleAnswer.Holding;
        Spend(records, lent.Price);
        pokemon.HeldItem = ItemDatabase.Get(item);
        return CastleAnswer.Done;
    }

    // ------------------------------------------------------------------ the second counter: the trainer's three

    private FrontierOpponent Opposing => Foe ?? throw new InvalidOperationException("No trainer has been drawn yet");

    /// <summary>One of the trainer's Pokémon is told (<c>RevealPokemonInSlot</c>): which species it is, for a point.</summary>
    public CastleAnswer Identify(FrontierRecords records, int slot)
    {
        if (Identified[slot]) return CastleAnswer.NoUse;
        if (Points(records) < IdentifyPrice) return CastleAnswer.NotEnough;
        Spend(records, IdentifyPrice);
        Identified[slot] = true;
        return CastleAnswer.Done;
    }

    /// <summary>
    /// One of the trainer's Pokémon is made five levels higher for a point, or five lower for fifteen
    /// (<c>UpdateLevelAdjustment</c>): from level 50 it goes to 55 or to 45, and from either the other way takes it
    /// back to 50. Each one raised is worth seven Castle Points when the battle is won.
    /// </summary>
    public CastleAnswer AdjustLevel(FrontierRecords records, int slot, bool up)
    {
        if (Levels[slot] == (up ? CastleLevel.Up : CastleLevel.Down)) return CastleAnswer.NoUse;
        int price = up ? LevelUpPrice : LevelDownPrice;
        if (Points(records) < price) return CastleAnswer.NotEnough;
        Spend(records, price);
        Levels[slot] = Levels[slot] == CastleLevel.Same ? up ? CastleLevel.Up : CastleLevel.Down : CastleLevel.Same;
        var pokemon = Opposing.Pokemon[slot];
        int level = Levels[slot] switch { CastleLevel.Up => Level + 5, CastleLevel.Down => Level - 5, _ => Level };
        pokemon.LevelTo(level);
        pokemon.CurrentExp = Pokemon.GetExpForLevel(level, pokemon.Species.GrowthRate);
        pokemon.RecalculateStats();
        pokemon.CurrentHP = pokemon.MaxHP;
        return CastleAnswer.Done;
    }

    /// <summary>One of the trainer's Pokémon's stats, ability, nature and item are told for two points (<c>CP_COST_SHOW_STATS</c>), once: after that they can be looked at again.</summary>
    public CastleAnswer ShowStats(FrontierRecords records, int slot)
    {
        if (StatsShown[slot]) return CastleAnswer.Done;
        if (Points(records) < StatsPrice) return CastleAnswer.NotEnough;
        Spend(records, StatsPrice);
        StatsShown[slot] = true;
        return CastleAnswer.Done;
    }

    /// <summary>One of the trainer's Pokémon's moves are told for five points, from the counter's second rank (<c>CP_COST_SHOW_MOVES</c>).</summary>
    public CastleAnswer ShowMoves(FrontierRecords records, int slot)
    {
        if (Rank(records, CastleRank.Summary) < 2) return CastleAnswer.RankTooLow;
        if (MovesShown[slot]) return CastleAnswer.Done;
        if (Points(records) < MovesPrice) return CastleAnswer.NotEnough;
        Spend(records, MovesPrice);
        MovesShown[slot] = true;
        return CastleAnswer.Done;
    }

    // ------------------------------------------------------------------ battle by battle

    /// <summary>Whether the battle about to be fought is the Castle Valet's (<c>BC_FUNC_GET_NEXT_BATTLE_TYPE</c>): a Single streak of 20 or of 48.</summary>
    public FrontierPrint DarachNext => Way == FrontierChallenge.Single ? FrontierBrains.NextBattle(FrontierFacility.Castle, Streak) : FrontierPrint.None;

    /// <summary><c>BC_FUNC_CHECK_SEEN_VALET_INTRO</c>: whether the Castle Valet had been announced already; he has been now.</summary>
    public bool AnnounceValet()
    {
        bool before = ValetAnnounced;
        ValetAnnounced = true;
        return before;
    }

    /// <summary>A battle may be passed from the fourth round on, though never the Castle Valet's (the corridor's menu).</summary>
    public bool MayPass => Round >= PassFromRound && DarachNext == FrontierPrint.None;

    /// <summary>
    /// <c>BattleCastle_GetAIMask</c>: the trainers of the first two rounds choose their moves at random, those of the
    /// third and fourth with the basic routine alone, and everyone after, like the Castle Valet, with all three.
    /// </summary>
    public AiFlags Mind
    {
        get
        {
            var (silver, gold) = FrontierBrains.Trainers(FrontierFacility.Castle);
            if (Way == FrontierChallenge.Single && Battle < Trainers.Count && (Trainers[Battle] == silver || Trainers[Battle] == gold)) return FrontierTrainers.Mind;
            return Round switch
            {
                0 or 1 => AiFlags.None,
                2 or 3 => AiFlags.Basic,
                _ => FrontierTrainers.Mind
            };
        }
    }

    /// <summary>The trainer of the battle about to be fought, whole (<c>Party_HealAllMembers</c> of their three), thinking as the round has them think.</summary>
    public Trainer OpponentForBattle()
    {
        foreach (var pokemon in Opposing.Pokemon) pokemon.HealFull();
        return Opposing.AsTrainer(Mind);
    }

    /// <summary>
    /// <c>BattleCastle_CalcCPEarnedFromBattle</c>: three points for each of the three still standing, three more for
    /// one at full HP, two for one at half or more and one for one under it, one for each without a status; eight
    /// for a battle that used five PP or fewer between the three, six for ten or fewer, four for fifteen or fewer;
    /// seven for each of the trainer's Pokémon that was raised five levels. One at the least.
    /// </summary>
    public int PointsFromBattle()
    {
        int earned = 0, remaining = 0;
        Fainted = false;
        foreach (var pokemon in Team.Members)
        {
            if (pokemon.CurrentHP > 0)
            {
                earned += 3;
                earned += pokemon.CurrentHP == pokemon.MaxHP ? 3 : pokemon.CurrentHP >= pokemon.MaxHP / 2 ? 2 : 1;
                if (pokemon.Status == StatusCondition.None) earned += 1;
            }
            else Fainted = true;
            remaining += pokemon.Moves.Take(4).Sum(m => m.CurrentPP);
        }
        earned += 7 * Levels.Count(l => l == CastleLevel.Up);
        int used = StartingPp.Sum(p => p.Sum()) - remaining;
        earned += used <= 5 ? 8 : used <= 10 ? 6 : used <= 15 ? 4 : 0;
        return Math.Max(1, earned);
    }

    /// <summary>
    /// After a battle won (<c>RECORD_BATTLE_CASTLE_VICTORIES</c>, <c>BC_FUNC_INCREMENT_CURRENT_BATTLE</c>,
    /// <c>…_STREAK</c>, <c>BC_FUNC_GET_CP_FROM_BATTLE</c>, <c>BC_FUNC_PREP_FOR_NEXT_BATTLE</c>): the win is counted,
    /// its Castle Points are earned, and the three are made ready for the next: whoever fainted is back with a
    /// single HP, every status is gone, and what was bought about the trainer just beaten is forgotten. Returns the
    /// Castle Points earned.
    /// </summary>
    public int Won(FrontierRecords records)
    {
        records.CountVictories(FrontierFacility.Castle, 1);
        Battle++;
        if (Streak < FrontierRecords.Most) Streak++;
        int earned = PointsFromBattle();
        Receive(records, earned);
        Prepare();
        return earned;
    }

    /// <summary>
    /// A battle is passed for fifty Castle Points (the corridor's scene, <c>_0A1C</c>): it counts for the round and
    /// for the streak as one won, but not for the battles won here, and earns nothing.
    /// </summary>
    public CastleAnswer Pass(FrontierRecords records)
    {
        if (!MayPass) return CastleAnswer.RankTooLow;
        if (Points(records) < PassPrice) return CastleAnswer.NotEnough;
        Spend(records, PassPrice);
        Battle++;
        if (Streak < FrontierRecords.Most) Streak++;
        Fainted = false;
        Prepare();
        return CastleAnswer.Done;
    }

    // BattleCastle_PrepForNextBattle
    private void Prepare()
    {
        NotePp();
        foreach (var pokemon in Team.Members)
        {
            if (pokemon.CurrentHP == 0) pokemon.CurrentHP = 1;
            pokemon.Status = StatusCondition.None;
            pokemon.SleepTurns = 0;
            pokemon.ToxicCounter = 0;
            pokemon.ResetStatStages();
        }
        Array.Clear(Identified);
        Array.Clear(Levels);
        Array.Clear(StatsShown);
        Array.Clear(MovesShown);
    }

    public bool RoundDone => Battle >= BattlesPerRound;

    /// <summary>The Castle Valet was beaten (the throne room's script): the print waits in the lobby, the first time for each.</summary>
    public void BeatValet(StoryState story, FrontierPrint which)
    {
        BeatDarach = which;
        FrontierBrains.Beaten(story, FrontierFacility.Castle, which);
    }

    // ------------------------------------------------------------------ the end of a round, and of a challenge

    // BattleCastle_Save: the latest streak always; the record, the Castle Points that go with it and whether the
    // streak runs on unless the challenge is only being put down
    private void Save(FrontierRecords records, bool pausing)
    {
        records.Set(LatestStreakStat(Way), Streak);
        if (pausing) return;
        int before = records.Get(RecordStreakStat(Way));
        int after = records.SetIfBetter(RecordStreakStat(Way), Streak);
        int points = records.Get(LatestPointsStat(Way));
        if (Streak == before) records.SetIfBetter(RecordPointsStat(Way), points);
        else if (before < after) records.Set(RecordPointsStat(Way), points);
        records.SetRunning(FrontierFacility.Castle, (int)Way, RoundWon);
    }

    /// <summary><c>BattleCastle_SaveOnCompletingRound</c>: the streak runs on with its Castle Points and ranks, and the next round is one further (up to the eighth).</summary>
    public void EndRound(FrontierRecords records)
    {
        RoundWon = true;
        if (Round < DistinctRounds) Round++;
        Battle = 0;
        Save(records, pausing: false);
    }

    /// <summary><c>BattleCastle_SaveOnLoss</c>: the streak ends where it stands, and every counter is back at its first rank.</summary>
    public void EndLost(FrontierRecords records)
    {
        RoundWon = false;
        foreach (var rank in Enum.GetValues<CastleRank>()) records.Set(RankStat(Way, rank), 1);
        Save(records, pausing: false);
    }

    /// <summary><c>BattleCastle_GetEarnedBP</c>, after <see cref="EndRound"/>: by the round the streak goes on to, and twenty for a Single round that ended with the Castle Valet beaten.</summary>
    public int PointsEarned
    {
        get
        {
            var (silverAt, goldAt) = FrontierBrains.Battles(FrontierFacility.Castle);
            if (Way == FrontierChallenge.Single && (Streak == silverAt || Streak == goldAt)) return DarachPoints;
            return PointsByRound[Math.Min(Round, DistinctRounds)];
        }
    }

    /// <summary><c>BC_FUNC_QUICKSAVE</c>: the challenge is put down between two battles, with the three as they stand and the trainer to come.</summary>
    public void Pause(FrontierRecords records, Party party)
    {
        Save(records, pausing: true);
        records.Castle.Paused = new CastlePause
        {
            Way = Way,
            Battle = Battle,
            Slots = Slots.ToList(),
            Trainers = Trainers.ToList(),
            FoeSets = Foe?.Sets.ToList() ?? new List<int>(),
            Team = Team.Members.Select(p => new CastleKept { Hp = p.CurrentHP, Pp = p.Moves.Select(m => m.CurrentPP).ToList(), Item = p.HeldItem?.Name }).ToList(),
            Species = Slots.Where(s => s >= 0 && s < party.Count).Select(s => party.Members[s].Species.Name).ToList()
        };
    }

    /// <summary>
    /// <c>BattleCastle_ClearActiveStreak</c>: a challenge was left without being put down. The streak of its way is
    /// over: no longer running, its latest streak and its Castle Points nought, every counter at its first rank.
    /// </summary>
    public static void Forfeit(FrontierRecords records, FrontierChallenge way)
    {
        records.SetRunning(FrontierFacility.Castle, (int)way, false);
        records.Set(LatestStreakStat(way), 0);
        records.Set(LatestPointsStat(way), 0);
        foreach (var rank in Enum.GetValues<CastleRank>()) records.Set(RankStat(way, rank), 1);
        records.Castle.Paused = null;
    }
}
