using System;
using System.Collections.Generic;

namespace PokemonPlatinumEngine.Models.SuperContests;

/// <summary>A Super Contest's rank (the original's <c>CONTEST_RANK_...</c>): each is entered with the ribbon of the one before.</summary>
public enum ContestRank { Normal, Great, Ultra, Master }

/// <summary>
/// What a move does in the Acting Competition, in the original's order (<c>generated/move_contest_effects.txt</c>);
/// <see cref="ContestRules"/> says what each is worth and <see cref="SuperContestActing"/> carries it out.
/// </summary>
public enum ContestEffect
{
    None, FirstNextTurn, LastNextTurn, DoubledJudge, TwoHeartsWhenVoltageUp, Basic, UniqueJudge, ConsecutiveUse, Voltage,
    AllSameJudge, LowersVoltage, DoubleNextTurn, StealVoltage, SuppressVoltage, RandomOrder, DoubleFinalAct,
    LowVoltageAdvantage, FirstPerformanceAdvantage, FinalPerformanceAdvantage, NoVoltageDown, TwoVoltageInARowAdvantage,
    HighScoreLaterTurn, MaxVoltageAdvantage, PityPoints
}

/// <summary>
/// A move on the contest stage (plan 06 · R17a; <c>moves.json</c>'s <c>contest</c>, from the decompilation's move
/// data): the quality it shows and its effect in a Super Contest's Acting Competition. Platinum's 467 moves have one;
/// later moves have none. Plan 21 · H11 adds Hoenn's own appeal and jam beside these.
/// </summary>
public sealed class MoveContest
{
    /// <summary>The quality the move shows: what the judges' voltage rises or falls to.</summary>
    public ContestStat Type { get; set; }

    /// <summary>What it does in the Acting Competition.</summary>
    public ContestEffect Effect { get; set; }

    /// <summary>The hearts it is worth by itself (<see cref="ContestRules.Appeal"/>, in hearts).</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public int Hearts => ContestRules.Appeal(Effect) / ContestRules.PointsPerHeart;
}

/// <summary>
/// The numbers of a Super Contest that hold whoever is on stage (plan 06 · R17a), from the original's tables: what
/// an effect is worth, how a move's quality moves a judge's voltage, how many hearts a look is worth, who may enter.
/// GPU-free.
/// </summary>
public static class ContestRules
{
    /// <summary><c>POINTS_PER_APPEAL_HEART</c>: the Acting Competition counts in tens, one heart each.</summary>
    public const int PointsPerHeart = 10;

    /// <summary>Four on stage, three judges (<c>CONTEST_NUM_PARTICIPANTS</c>, <c>CONTEST_NUM_JUDGES</c>).</summary>
    public const int Contestants = 4, Judges = 3;

    /// <summary>The judge in the middle is the one a full voltage pays most for (the original's <c>unk_10E</c>, always 1).</summary>
    public const int MainJudge = 1;

    /// <summary>A judge's voltage is full at five steps (in points), and then pays five hearts, eight with the main judge.</summary>
    public const int FullVoltage = 50, FullVoltageBonus = 50, MainJudgeBonus = 80;

    // Unk_020F568C's unk_04: the points a move's effect starts from
    private static readonly int[] Appeals =
    {
        0, 20, 20, 0, 20, 30, 10, 20, 0, 0, 20, 0, 0, 20, 20, 20, 0, 20, 20, 20, 10, 0, 20, 10
    };

    /// <summary>The points an effect starts from, ten to a heart (<c>sub_02095734</c>).</summary>
    public static int Appeal(ContestEffect effect) => Appeals[(int)effect];

    // Unk_ov17_022539C8: the contest's quality down, the move's across
    private static readonly int[,] VoltageTable =
    {
        { 10, 0, -10, -10, 0 },
        { 0, 10, 0, -10, -10 },
        { -10, 0, 10, 0, -10 },
        { -10, -10, 0, 10, 0 },
        { 0, -10, -10, 0, 10 }
    };

    /// <summary>
    /// What a move of one quality does to a judge's voltage in a contest of another (<c>ov17_02243AF0</c>): up a step
    /// for the contest's own, down a step for the two it clashes with, nothing for its neighbours.
    /// </summary>
    public static int Voltage(ContestStat contest, ContestStat move) => VoltageTable[(int)contest, (int)move];

    /// <summary>The points a judge gives each who performed for them, by how many others did too (<c>ov17_02243AE4</c>).</summary>
    public static int JudgePoints(int others) => others switch { 0 => 30, 1 => 20, 2 => 10, _ => 0 };

    // sub_02095928: the looks' thresholds, a heart for each one reached
    private static readonly int[][] ConditionSteps =
    {
        new[] { 10, 20, 30, 40, 50, 60, 70, 80 },
        new[] { 90, 110, 130, 150, 170, 190, 210, 230 },
        new[] { 170, 200, 230, 260, 290, 320, 350, 380 },
        new[] { 320, 360, 400, 440, 480, 520, 560, 600 }
    };

    /// <summary>The hearts a Pokémon's condition is worth in the Visual Competition, 0 to 8 (<c>sub_02095928</c>).</summary>
    public static int ConditionHearts(ContestRank rank, int conditionScore)
    {
        int hearts = 0;
        foreach (int step in ConditionSteps[(int)rank])
        {
            if (conditionScore < step) break;
            hearts++;
        }
        return hearts;
    }

    // sub_0209598C: the dressing-up's thresholds
    private static readonly int[][] DressSteps =
    {
        new[] { 3, 5, 8 }, new[] { 5, 10, 15 }, new[] { 7, 15, 23 }, new[] { 10, 20, 30 }
    };

    /// <summary>The hearts a dressing-up is worth, 0 to 4, from the points its accessories score for the theme (<c>sub_0209598C</c>).</summary>
    public static int DressHearts(ContestRank rank, int themePoints)
    {
        if (themePoints == 0) return 0;
        int hearts = 1;
        foreach (int step in DressSteps[(int)rank])
        {
            if (themePoints <= step) return hearts;
            hearts++;
        }
        return hearts;
    }

    /// <summary>How many accessories a rank lets a Pokémon wear (<c>ov22_02257580</c>), and the seconds there are to put them on.</summary>
    public static int AccessoryLimit(ContestRank rank) => 5 * ((int)rank + 1);
    public const int DressingSeconds = 60;

    /// <summary>
    /// The score of a Pokémon's condition in a contest of one quality (<c>ov17_0223F374</c>): the quality itself,
    /// half of its two neighbours and the sheen together, and a tenth more for the quality's own scarf, a twentieth
    /// for a neighbour's.
    /// </summary>
    public static int ConditionScore(ContestStat contest, IReadOnlyList<int> condition, int sheen, string? heldItem)
    {
        // The two neighbours as the original names them: Cool's are Tough and Beauty, and so on round
        int before = ((int)contest + 4) % 5, after = ((int)contest + 1) % 5;
        int score = condition[(int)contest] + (condition[before] + condition[after] + sheen) / 2;
        int scarf = Array.IndexOf(Scarves, heldItem);
        int modifier = scarf < 0 ? 100 : scarf == (int)contest ? 110 : scarf == before || scarf == after ? 105 : 100;
        return score * modifier / 100;
    }

    // The scarves in their qualities' order: Red is Cool's, Blue Beauty's, Pink Cute's, Green Smart's, Yellow Tough's
    private static readonly string?[] Scarves = { "Red Scarf", "Blue Scarf", "Pink Scarf", "Green Scarf", "Yellow Scarf" };

    /// <summary>The ribbon a rank of a quality gives, by the original's name (<c>RIBBON_COOL</c>, <c>RIBBON_CUTE_GREAT</c>).</summary>
    public static string Ribbon(ContestStat contest, ContestRank rank) =>
        "RIBBON_" + contest.ToString().ToUpperInvariant() + (rank == ContestRank.Normal ? "" : "_" + rank.ToString().ToUpperInvariant());

    /// <summary>The ribbon's name as it is said: "Cool Ribbon", "Cute Ribbon Great".</summary>
    public static string RibbonName(ContestStat contest, ContestRank rank) =>
        contest + " Ribbon" + (rank == ContestRank.Normal ? "" : " " + rank);

    /// <summary>How many of a quality's four ribbons a Pokémon has (<c>CountEarnedRibbonsForContestType</c>).</summary>
    public static int RibbonsOf(Pokemon pokemon, ContestStat contest)
    {
        int count = 0;
        foreach (ContestRank rank in Enum.GetValues<ContestRank>())
            if (pokemon.Ribbons.Contains(Ribbon(contest, rank))) count++;
        return count;
    }

    /// <summary>
    /// A Pokémon's fame on stage: one, and one more for each of the quality's ribbons in a row from the first
    /// (<c>CalcMonContestFame</c>). The applause is as loud.
    /// </summary>
    public static int Fame(Pokemon pokemon, ContestStat contest)
    {
        int fame = 1;
        foreach (ContestRank rank in Enum.GetValues<ContestRank>())
        {
            if (!pokemon.Ribbons.Contains(Ribbon(contest, rank))) break;
            fame++;
        }
        return fame;
    }

    /// <summary>
    /// Why a Pokémon may not enter, or null when it may (<c>CheckContestEligibility</c>): an Egg and a fainted
    /// Pokémon never; a rank asks for as many of the quality's ribbons as there are ranks under it; and two moves at
    /// least, since none may be shown twice running.
    /// </summary>
    public static string? WhyNot(Pokemon pokemon, ContestStat contest, ContestRank rank)
    {
        if (pokemon.IsEgg) return "egg";
        if (pokemon.CurrentHP <= 0) return "fainted";
        if (RibbonsOf(pokemon, contest) < (int)rank) return "rank";
        if (pokemon.Moves.Count < 2) return "moves";
        return null;
    }

    /// <summary>
    /// The accessory a first win of a rank and quality gives (<c>Contest_CalcFirstTimeVictoryAccessoryReward</c>): a
    /// barrette, balloons, a hat and a stage in the quality's colour.
    /// </summary>
    public static string Prize(ContestStat contest, ContestRank rank) => Prizes[(int)contest][(int)rank];

    private static readonly string[][] Prizes =
    {
        new[] { "Red Barrette", "Red Balloons", "Top Hat", "Gold Pedestal" },
        new[] { "Blue Barrette", "Blue Balloons", "Silk Veil", "Glass Stage" },
        new[] { "Pink Barrette", "Pink Balloon", "Lace Headdress", "Flower Stage" },
        new[] { "Green Barrette", "Green Balloons", "Professor Hat", "Cube Stage" },
        new[] { "Yellow Barrette", "Yellow Balloon", "Heroic Headband", "Award Podium" }
    };

    /// <summary>
    /// What an effect does, in our own words on the original's two lines (<c>contest_effects.json</c>'s beats), for
    /// the list of moves.
    /// </summary>
    public static string Describe(ContestEffect effect) => effect switch
    {
        ContestEffect.FirstNextTurn => "Goes first in the next turn.",
        ContestEffect.LastNextTurn => "Goes last in the next turn.",
        ContestEffect.DoubledJudge => "+2 for each rival who chose the same judge.",
        ContestEffect.TwoHeartsWhenVoltageUp => "+2 if it raises the voltage.",
        ContestEffect.Basic => "A plain performance.",
        ContestEffect.UniqueJudge => "+3 if no rival chose the same judge.",
        ContestEffect.ConsecutiveUse => "May be performed two turns running.",
        ContestEffect.Voltage => "Adds the judge's voltage to the score.",
        ContestEffect.AllSameJudge => "+15 if all four chose the same judge.",
        ContestEffect.LowersVoltage => "Lowers every judge's voltage by one.",
        ContestEffect.DoubleNextTurn => "Doubles the next turn's score.",
        ContestEffect.StealVoltage => "Takes the voltage bonus of the act before.",
        ContestEffect.SuppressVoltage => "No voltage goes up for the rest of the turn.",
        ContestEffect.RandomOrder => "Shuffles the next turn's order.",
        ContestEffect.DoubleFinalAct => "Double score if performed last.",
        ContestEffect.LowVoltageAdvantage => "Scores more the lower the judge's voltage.",
        ContestEffect.FirstPerformanceAdvantage => "+2 if performed first.",
        ContestEffect.FinalPerformanceAdvantage => "+2 if performed last.",
        ContestEffect.NoVoltageDown => "No voltage goes down for the rest of the turn.",
        ContestEffect.TwoVoltageInARowAdvantage => "+3 if the voltage rises twice in a row.",
        ContestEffect.HighScoreLaterTurn => "Scores more the later it is performed.",
        ContestEffect.MaxVoltageAdvantage => "+3 right after a voltage is filled.",
        ContestEffect.PityPoints => "+3 if it scores the least of the turn.",
        _ => ""
    };
}

/// <summary>
/// How a contest came out for the player (<see cref="ContestRules.Conclude"/>): the place, 0 for the winner, and what
/// a win gave: the ribbon when the Pokémon hadn't it yet, and the accessory that was put in the Fashion Case.
/// </summary>
public sealed record ContestOutcome(ContestStat Category, ContestRank Rank, ContestMode Mode, string Pokemon, int Placement, bool Won, string? NewRibbon, string? Prize);

/// <summary>What a contest leaves behind once it is over (the original's <c>Contest_EndContest</c>); beside <see cref="ContestRules"/>.</summary>
public static class ContestAwards
{
    /// <summary>
    /// Ends a contest for the player's Pokémon (<c>Contest_EndContest</c>, <c>Contest_CalcFirstTimeVictoryAccessoryReward</c>):
    /// a round held for practice leaves nothing; a contest won gives the Pokémon the rank's ribbon and the friendship of
    /// a win, and the rank's accessory when the Fashion Case has room for it (each comes one of a kind, so once).
    /// </summary>
    public static ContestOutcome Conclude(Pokemon pokemon, ContestStat category, ContestRank rank, ContestMode mode, int placement, FashionCase fashionCase)
    {
        bool won = placement == 0 && mode == ContestMode.Official;
        string? ribbon = null, prize = null;
        if (won)
        {
            if (pokemon.Ribbons.Add(ContestRules.Ribbon(category, rank))) ribbon = ContestRules.RibbonName(category, rank);
            string accessory = ContestRules.Prize(category, rank);
            if (fashionCase.Add(accessory)) prize = accessory;
            FriendshipRules.Apply(pokemon, FriendshipEvent.ContestWin);
        }
        return new ContestOutcome(category, rank, mode, pokemon.DisplayName, placement, won, ribbon, prize);
    }
}
