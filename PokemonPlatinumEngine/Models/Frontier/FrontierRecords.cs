using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumEngine.Models.Frontier;

/// <summary>The Battle Frontier's facilities by the original's numbers (<c>enum BattleFacility</c>): the Factory counts twice, for its two levels.</summary>
public enum FrontierFacility
{
    Tower = 1,
    Factory,
    FactoryOpen,
    Castle,
    Hall,
    Arcade
}

/// <summary>How a challenge is fought (<c>FRONTIER_CHALLENGE_…</c>): alone with one Pokémon out, alone with two, or beside a partner.</summary>
public enum FrontierChallenge
{
    Single,
    Double,
    Multi
}

/// <summary>
/// The numbers the Battle Frontier keeps (<c>enum BattleFrontierStatsIndex</c>, the hundred of one player's own, in
/// the original's order, which the save keeps them by): each facility's record and latest streaks mode by mode, and
/// what else each counts. The ones kept for a friend over Wi-Fi are left out (decision 06 · 7).
/// </summary>
public enum FrontierStat
{
    TowerRecordStreakSingle, TowerLatestStreakSingle, TowerRecordStreakDouble, TowerLatestStreakDouble, TowerRecordStreakMulti,
    TowerLatestStreakMulti, TowerRecordStreakLinkMulti, TowerLatestStreakLinkMulti, TowerRecordStreakWifi, TowerLatestStreakWifi,
    FactoryRecordStreak50Single, FactoryLatestStreak50Single, FactoryRecordTradeCount50Single, FactoryLatestTradeCount50Single,
    FactoryRecordStreakOpenSingle, FactoryLatestStreakOpenSingle, FactoryRecordTradeCountOpenSingle, FactoryLatestTradeCountOpenSingle,
    FactoryRecordStreak50Double, FactoryLatestStreak50Double, FactoryRecordTradeCount50Double, FactoryLatestTradeCount50Double,
    FactoryRecordStreakOpenDouble, FactoryLatestStreakOpenDouble, FactoryRecordTradeCountOpenDouble, FactoryLatestTradeCountOpenDouble,
    FactoryRecordStreak50Multi, FactoryLatestStreak50Multi, FactoryRecordTradeCount50Multi, FactoryLatestTradeCount50Multi,
    FactoryRecordStreakOpenMulti, FactoryLatestStreakOpenMulti, FactoryRecordTradeCountOpenMulti, FactoryLatestTradeCountOpenMulti,
    HallCurrentStreakSingle, HallLatestStreakSingle, HallLatestSpeciesSingle, HallCurrentRanksNormalFightingSingle,
    HallCurrentRanksFlyingPoisonSingle, HallCurrentRanksGroundRockSingle, HallCurrentRanksBugGhostSingle, HallCurrentRanksSteelMysterySingle,
    HallCurrentRanksFireWaterSingle, HallCurrentRanksGrassElectricSingle, HallCurrentRanksPsychicIceSingle, HallCurrentRanksDragonDarkSingle,
    HallCurrentStreakDouble, HallLatestStreakDouble, HallLatestSpeciesDouble, HallCurrentRanksNormalFightingDouble,
    HallCurrentRanksFlyingPoisonDouble, HallCurrentRanksGroundRockDouble, HallCurrentRanksBugGhostDouble, HallCurrentRanksSteelMysteryDouble,
    HallCurrentRanksFireWaterDouble, HallCurrentRanksGrassElectricDouble, HallCurrentRanksPsychicIceDouble, HallCurrentRanksDragonDarkDouble,
    HallCurrentStreakMulti, HallLatestStreakMulti, HallLatestSpeciesMulti, HallCurrentRanksNormalFightingMulti, HallCurrentRanksFlyingPoisonMulti,
    HallCurrentRanksGroundRockMulti, HallCurrentRanksBugGhostMulti, HallCurrentRanksSteelMysteryMulti, HallCurrentRanksFireWaterMulti,
    HallCurrentRanksGrassElectricMulti, HallCurrentRanksPsychicIceMulti, HallCurrentRanksDragonDarkMulti, CastleRecordStreakSingle,
    CastleLatestStreakSingle, CastleLatestCpSingle, CastleSpentCpSingle, CastleRecordCpSingle, CastleRankHealingSingle, CastleRankRentalsSingle,
    CastleRankOppSummarySingle, CastleRecordStreakDouble, CastleLatestStreakDouble, CastleLatestCpDouble, CastleSpentCpDouble, CastleRecordCpDouble,
    CastleRankHealingDouble, CastleRankRentalsDouble, CastleRankOppSummaryDouble, CastleRecordStreakMulti, CastleLatestStreakMulti,
    CastleLatestCpMulti, CastleSpentCpMulti, CastleRecordCpMulti, CastleRankHealingMulti, CastleRankRentalsMulti, CastleRankOppSummaryMulti,
    ArcadeRecordStreakSingle, ArcadeLatestStreakSingle, ArcadeRecordStreakDouble, ArcadeLatestStreakDouble, ArcadeRecordStreakMulti,
    ArcadeLatestStreakMulti,
}

/// <summary>A Frontier Brain's commemorative print for a facility (<c>VAR_…_PRINT_STATE</c> of 2 and of 4).</summary>
public enum FrontierPrint
{
    None,
    Silver,
    Gold
}

/// <summary>
/// What the Battle Tower keeps beside the stats (the original's <c>WifiBattleTowerRecord</c>, for the parts one
/// player alone has): how many sets of seven each mode's streak has come through, the state its trainers are drawn
/// from, and what the lobby has handed over.
/// </summary>
public sealed class TowerRecord
{
    /// <summary>The sets of seven a streak has won, mode by mode (<c>roomNums</c>): it is the room the next set is fought in.</summary>
    public List<int> Rooms { get; set; } = new() { 0, 0, 0, 0, 0 };

    /// <summary>The number the Tower's draws start from (<c>rngState</c>): made anew each day from the day's number.</summary>
    public uint RngState { get; set; }

    /// <summary>The trophies handed over for a Single streak of 20, 50 and 100 (the record's flags 13, 0 and 1).</summary>
    public bool BronzeTrophy { get; set; }
    public bool SilverTrophy { get; set; }
    public bool GoldTrophy { get; set; }

    /// <summary>A trophy was due and the Underground's PC had no room (flags 14, 2 and 3): the lobby says so once more.</summary>
    public bool BronzeWaits { get; set; }
    public bool SilverWaits { get; set; }
    public bool GoldWaits { get; set; }

    /// <summary>The mode of the challenge last begun (the save's field 0): what a challenge walked out on is ended by.</summary>
    public TowerMode LastMode { get; set; }

    /// <summary>A challenge saved between two battles, to be taken up again (<c>WifiBattleTowerSave</c>); null when none is.</summary>
    public TowerPause? Paused { get; set; }
}

/// <summary>A Battle Tower challenge put down between battles (the fields <c>sub_0204A8C8</c> writes).</summary>
public sealed class TowerPause
{
    public TowerMode Mode { get; set; }
    public int NextOpponent { get; set; }
    public List<int> Slots { get; set; } = new();
    public List<int> Trainers { get; set; } = new();
    public uint Seed { get; set; }
    public TowerPartner? Partner { get; set; }
}

/// <summary>
/// The Battle Frontier's records (plan 06 · R18a; the original's <c>BattleFrontierSave</c> and the game records of
/// the Frontier): every facility's numbers by <see cref="FrontierStat"/>, which streaks are still running, the
/// Battle Points and what was won and spent of them, and each facility's own part. Saved as
/// <c>SaveData.Frontier</c>. The commemorative prints are the story's own variables
/// (<see cref="FrontierBrains.PrintVariable"/>), so a script can ask them. No drawing or input.
/// </summary>
public sealed class FrontierRecords
{
    public const int Most = 9999, MostPoints = 9999;

    /// <summary>The hundred numbers, in <see cref="FrontierStat"/>'s order.</summary>
    public List<int> Stats { get; set; } = Enumerable.Repeat(0, StatCount).ToList();

    public static int StatCount => Enum.GetValues<FrontierStat>().Length;

    /// <summary>
    /// The streaks still running, a bit for each way of taking a facility's challenge (the original's
    /// <c>streakActiveFlags</c> of each facility, and the Tower record's flags from 8 on), by the facility's number.
    /// </summary>
    public List<int> Running { get; set; } = Enumerable.Repeat(0, 7).ToList();

    public int BattlePoints { get; set; }

    /// <summary><c>RECORD_BATTLE_POINTS_RECEIVED</c> and <c>RECORD_BATTLE_POINTS_SPENT</c>.</summary>
    public int PointsReceived { get; set; }
    public int PointsSpent { get; set; }

    /// <summary>
    /// The battles won at each facility, by its number (<c>RECORD_BATTLE_TOWER_VICTORIES</c> and its four fellows):
    /// the Villa's catalogue lists its bust once none of the five is nought.
    /// </summary>
    public List<int> Victories { get; set; } = Enumerable.Repeat(0, 7).ToList();

    /// <summary><c>RECORD_BATTLE_TOWER_CHALLENGES</c>: the Tower's challenges ended, won or lost.</summary>
    public int TowerChallenges { get; set; }

    /// <summary><c>RECORD_TIMES_STARTED_BATTLE_FRONTIER_CHALLENGE</c>.</summary>
    public int ChallengesStarted { get; set; }

    /// <summary>The Frontier Brains met in battle (<c>RECORD_UNK_059</c>, counted as each walks in).</summary>
    public int BrainsMet { get; set; }

    public TowerRecord Tower { get; set; } = new();

    /// <summary>The Battle Factory's own part (plan 06 · R18b): a challenge put down, and the trades made.</summary>
    public FactoryRecord Factory { get; set; } = new();

    /// <summary>The Battle Hall's own part (plan 06 · R18c): a challenge put down, and every species' record.</summary>
    public HallRecord Hall { get; set; } = new();

    /// <summary>The Battle Castle's own part (plan 06 · R18c): a challenge put down, and what the valet has told.</summary>
    public CastleRecord Castle { get; set; } = new();

    // ------------------------------------------------------------------ the numbers

    /// <summary><c>BattleFrontierSave_GetStat</c>.</summary>
    public int Get(FrontierStat stat) => (int)stat < Stats.Count ? Stats[(int)stat] : 0;

    /// <summary><c>BattleFrontierSave_SetStat</c>: nothing is kept past 9,999.</summary>
    public int Set(FrontierStat stat, int value)
    {
        while (Stats.Count <= (int)stat) Stats.Add(0);
        return Stats[(int)stat] = Math.Clamp(value, 0, Most);
    }

    /// <summary><c>BattleFrontierSave_SetIfBetter</c>: the number if it beats what is kept; what is kept is returned.</summary>
    public int SetIfBetter(FrontierStat stat, int value)
    {
        int kept = Get(stat);
        if (kept < value) return Set(stat, value);
        return kept > Most ? Set(stat, Most) : kept;
    }

    /// <summary><c>BattleFrontierSave_AddToStat</c>.</summary>
    public int Add(FrontierStat stat, int amount) => Set(stat, Get(stat) + amount);

    /// <summary><c>BattleFrontierSave_SubtractFromStat</c>: never under nought.</summary>
    public int Subtract(FrontierStat stat, int amount) => Set(stat, Math.Max(0, Get(stat) - amount));

    /// <summary>Whether a streak is still running: its last challenge was won, so the next one goes on from it.</summary>
    public bool IsRunning(FrontierFacility facility, int way) => (Running[(int)facility] >> way & 1) == 1;

    public void SetRunning(FrontierFacility facility, int way, bool running)
    {
        if (running) Running[(int)facility] |= 1 << way;
        else Running[(int)facility] &= ~(1 << way);
    }

    /// <summary>A battle won at a facility counts for its game record (<c>GameRecords_AddToRecordValue</c>), up to the original's 999,999.</summary>
    public void CountVictories(FrontierFacility facility, int battles) =>
        Victories[(int)facility] = Math.Min(999_999, Victories[(int)facility] + battles);

    public int VictoriesAt(FrontierFacility facility) => Victories[(int)facility];

    /// <summary>
    /// A battle has been won at every facility (<c>ScrCmd_CheckMetFurnitureRequirements</c>, the Villa's Pokémon
    /// Bust): the Factory's two levels count as the one facility they are.
    /// </summary>
    public bool WonEverywhere =>
        VictoriesAt(FrontierFacility.Tower) > 0 && VictoriesAt(FrontierFacility.Factory) + VictoriesAt(FrontierFacility.FactoryOpen) > 0
        && VictoriesAt(FrontierFacility.Castle) > 0 && VictoriesAt(FrontierFacility.Hall) > 0 && VictoriesAt(FrontierFacility.Arcade) > 0;

    // ------------------------------------------------------------------ Battle Points

    /// <summary><c>BATTLE_POINTS_FUNC_ADD</c>, with the game record of the points received.</summary>
    public int GivePoints(int points)
    {
        BattlePoints = Math.Min(MostPoints, BattlePoints + points);
        PointsReceived = Math.Min(999_999, PointsReceived + points);
        return BattlePoints;
    }

    /// <summary><c>BATTLE_POINTS_FUNC_SUB</c> at an exchange counter: all or nothing here, where the original's shop never asks for more than there is.</summary>
    public bool SpendPoints(int points)
    {
        if (points < 0 || points > BattlePoints) return false;
        BattlePoints -= points;
        PointsSpent = Math.Min(999_999, PointsSpent + points);
        return true;
    }

    // ------------------------------------------------------------------ the day

    /// <summary>
    /// A new day (<c>sub_0206C008</c>, called by the day's events and as a game begins): the Tower's draws start from
    /// the day's number, times the kit's constant, plus one.
    /// </summary>
    public void NewDay(uint dailyNumber) => Tower.RngState = unchecked(dailyNumber * 1566083941u + 1u);
}

/// <summary>
/// The Frontier Brains' frame (plan 06 · R18a): at which battle of a streak each stands in the way, twice, and the
/// commemorative print each win leaves, silver and then gold. A print is the story's variable, 0 to 4 as the original
/// counts it: 1 once the Brain is beaten the first time and 2 once the lobby has handed the silver print over, 3 and
/// 4 the same for the gold one.
/// </summary>
public static class FrontierBrains
{
    public const string AllGoldFlag = "FLAG_OBTAINED_ALL_BATTLE_FRONTIER_GOLD_PRINTS";

    /// <summary><c>TRAINER_SCORE_EVENT_ALL_BATTLE_FRONTIER_GOLD_PRINTS</c>.</summary>
    public const int AllGoldScore = 1000;

    public static readonly FrontierFacility[] Printed =
        { FrontierFacility.Tower, FrontierFacility.Factory, FrontierFacility.Hall, FrontierFacility.Castle, FrontierFacility.Arcade };

    /// <summary>The variable a facility's print is kept in; the Factory's two levels share the one.</summary>
    public static string PrintVariable(FrontierFacility facility) => facility switch
    {
        FrontierFacility.Tower => "VAR_BATTLE_TOWER_PRINT_STATE",
        FrontierFacility.Factory or FrontierFacility.FactoryOpen => "VAR_BATTLE_FACTORY_PRINT_STATE",
        FrontierFacility.Hall => "VAR_BATTLE_HALL_PRINT_STATE",
        FrontierFacility.Castle => "VAR_BATTLE_CASTLE_PRINT_STATE",
        _ => "VAR_BATTLE_ARCADE_PRINT_STATE"
    };

    /// <summary>The battles of a streak at which the Brain is met, the silver and the gold time (<c>…_STREAK_SILVER_BATTLE</c>, <c>…_GOLD_BATTLE</c>).</summary>
    public static (int Silver, int Gold) Battles(FrontierFacility facility) => facility == FrontierFacility.Hall ? (50, 170) : (21, 49);

    /// <summary>The battles of one round of a facility (<c>…_BATTLES_PER_ROUND</c>).</summary>
    public static int BattlesPerRound(FrontierFacility facility) => facility == FrontierFacility.Hall ? 10 : 7;

    /// <summary>
    /// Who the battle after <paramref name="streak"/> wins is against (<c>FRONTIER_NEXT_BATTLE_…</c>): the Brain for
    /// the silver print, for the gold one, or anyone.
    /// </summary>
    public static FrontierPrint NextBattle(FrontierFacility facility, int streak)
    {
        var (silver, gold) = Battles(facility);
        return streak + 1 == silver ? FrontierPrint.Silver : streak + 1 == gold ? FrontierPrint.Gold : FrontierPrint.None;
    }

    /// <summary>The Brain's two teams among the Frontier's trainers: the numbers of the silver and the gold one.</summary>
    public static (int Silver, int Gold) Trainers(FrontierFacility facility)
    {
        string who = facility switch
        {
            FrontierFacility.Tower => "tower_tycoon_palmer",
            FrontierFacility.Factory or FrontierFacility.FactoryOpen => "factory_head_thorton",
            FrontierFacility.Hall => "hall_matron_argenta",
            FrontierFacility.Castle => "castle_valet_darach",
            _ => "arcade_star_dahlia"
        };
        return (Data.FrontierDatabase.NumberOf(who + "_silver"), Data.FrontierDatabase.NumberOf(who + "_gold"));
    }

    /// <summary>The print the player has been handed for a facility.</summary>
    public static FrontierPrint PrintOf(StoryState story, FrontierFacility facility) =>
        story.Var(PrintVariable(facility)) switch { >= 4 => FrontierPrint.Gold, >= 2 => FrontierPrint.Silver, _ => FrontierPrint.None };

    /// <summary>
    /// The Brain was beaten (the battle rooms' scripts): the print waits to be handed over, the first time for each
    /// (0 becomes 1 for the silver one, 2 becomes 3 for the gold one; any other state stays).
    /// </summary>
    public static void Beaten(StoryState story, FrontierFacility facility, FrontierPrint which)
    {
        string variable = PrintVariable(facility);
        int state = story.Var(variable);
        if (which == FrontierPrint.Silver && state == 0) story.SetVar(variable, 1);
        else if (which == FrontierPrint.Gold && state == 2) story.SetVar(variable, 3);
    }

    /// <summary>
    /// The lobby hands over the print that waits (1 becomes 2, 3 becomes 4): which one it was, or none. The last gold
    /// print of the five sets the flag for them all (<c>Common_CheckAllFrontierGoldPrintsObtained</c>), once, and the
    /// caller adds <see cref="AllGoldScore"/> to the trainer's score when <paramref name="allGold"/> comes back true.
    /// </summary>
    public static FrontierPrint HandOver(StoryState story, FrontierFacility facility, out bool allGold)
    {
        allGold = false;
        string variable = PrintVariable(facility);
        switch (story.Var(variable))
        {
            case 1:
                story.SetVar(variable, 2);
                return FrontierPrint.Silver;
            case 3:
                story.SetVar(variable, 4);
                if (!story.Has(AllGoldFlag) && Printed.All(f => story.Var(PrintVariable(f)) == 4))
                {
                    story.Set(AllGoldFlag);
                    allGold = true;
                }
                return FrontierPrint.Gold;
            default:
                return FrontierPrint.None;
        }
    }

    /// <summary>A silver print or better at any facility (the Villa's Silver Pokémon Bust).</summary>
    public static bool AnyPrint(StoryState story) => Printed.Any(f => story.Var(PrintVariable(f)) >= 2);
}

/// <summary>The Exchange Service Corner (<c>ScrCmd_PokeMartFrontier</c>): what its two booths hand over for Battle Points.</summary>
public static class FrontierExchange
{
    /// <summary>The west booth's vitamins and held items, or the east booth's Technical Machines.</summary>
    public static IReadOnlyList<Data.FrontierPrizeRecord> Stock(bool machines) =>
        machines ? Data.FrontierDatabase.ExchangeMachines : Data.FrontierDatabase.ExchangeItems;

    /// <summary><c>Shop_GetItemBPPrice</c>: what something costs in Battle Points; 0 for what neither booth has.</summary>
    public static int PriceOf(string item) =>
        Data.FrontierDatabase.ExchangeItems.Concat(Data.FrontierDatabase.ExchangeMachines).FirstOrDefault(p => p.Item == item)?.Price ?? 0;
}
