using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Models.Frontier;

namespace PokemonPlatinumEngine.Models.Facilities;

/// <summary>
/// Nimbasa's Battle Institute's test (plan 37 · FC8, after Black and White's; <c>docs/mechanics/rulings.md</c>, "The
/// institute's test and the ladder"): five battles at 50, single or double, against trainers of the later sets rising by
/// the battle, each scored by a formula of ours to a total out of 9,999 and a rank of ours; the best of each format kept,
/// on a board of names of ours with the player's among them. Every draw on a seed of the test's and the battle. No drawing or input.
/// </summary>
public static class InstituteTest
{
    public const int Battles = 5, MostScore = 9999;

    /// <summary>The variables a test keeps: its format (1 single, 2 double), the battles fought, its seed and its score so far.</summary>
    public const string FormatVariable = "VAR_UNOVA_INSTITUTE_FORMAT", BattleVariable = "VAR_UNOVA_INSTITUTE_BATTLE",
        SeedVariable = "VAR_UNOVA_INSTITUTE_SEED", ScoreVariable = "VAR_UNOVA_INSTITUTE_SCORE";

    /// <summary>The best total of a format (1 single, 2 double).</summary>
    public static string BestVariable(int format) => format == 2 ? "VAR_UNOVA_INSTITUTE_BEST_DOUBLE" : "VAR_UNOVA_INSTITUTE_BEST_SINGLE";

    public static int EnteredIn(int format) => format == 2 ? 4 : 3;

    public static BattleFormat FormatOf(int format) => format == 2 ? BattleFormat.Double : BattleFormat.Single;

    /// <summary>The tiers of a battle's trainer (1 to 5): the weakest first, the strongest last.</summary>
    public static (int Lowest, int Highest) TiersFor(int battle) => battle switch { 1 => (1, 2), 2 => (1, 3), 3 => (2, 3), 4 => (2, 4), _ => (3, 4) };

    /// <summary>A battle's trainer, drawn for the test's seed and the battle, so drawn again it is the same.</summary>
    public static FacilityOpponent Foe(int format, int battle, int seed)
    {
        var (low, high) = TiersFor(battle);
        var rng = new BattleRandom(((uint)seed & 0xFFFF) | (uint)battle << 16 ^ 0x1E57_0000);
        return FacilityTrainers.Draw(EnteredIn(format), low, high, FacilityTrainers.IvsFor(battle), rng.Next16);
    }

    /// <summary>
    /// A battle's score (a formula of ours, at most 1,980): nothing for a loss; for a win 1,000, and 40 for each turn fewer
    /// than ten it took, up to 400 for the HP the team kept (its share in thousandths times 0.4), and 15 for each tier of the
    /// foe's sets together.
    /// </summary>
    public static int ScoreOf(bool won, int turns, int hpKeptPermille, int foeTiers)
    {
        if (!won) return 0;
        int speed = Math.Max(0, 10 - Math.Max(1, turns)) * 40;
        int hp = Math.Clamp(hpKeptPermille, 0, 1000) * 400 / 1000;
        return 1000 + speed + hp + Math.Max(0, foeTiers) * 15;
    }

    /// <summary>The HP a team kept, in thousandths of its whole.</summary>
    public static int HpKept(Party team)
    {
        int max = team.Members.Sum(p => p.MaxHP);
        return max == 0 ? 0 : team.Members.Sum(p => Math.Max(0, p.CurrentHP)) * 1000 / max;
    }

    /// <summary>The ranks of ours by the total: their thresholds, the first a test always reaches.</summary>
    public static readonly int[] RankThresholds = { 0, 2000, 4000, 6000, 8000, 9000 };

    /// <summary>A total's rank, 0 (Novice) to 5 (Master), as <c>ui.json</c>'s <c>institute.rank.N</c> names them.</summary>
    public static int RankOf(int total)
    {
        int rank = 0;
        for (int r = 1; r < RankThresholds.Length; r++)
            if (total >= RankThresholds[r]) rank = r;
        return rank;
    }

    /// <summary>The board's people of ours and their scores, the player's best placed among them.</summary>
    public static readonly IReadOnlyList<(string Name, int Score)> Board = new[]
    {
        ("Tessaly", 9120), ("Marek", 8710), ("Odile", 8240), ("Brann", 7650), ("Ysette", 6980),
        ("Lucan", 6100), ("Pernille", 5320), ("Joss", 4410), ("Carys", 3570), ("Wim", 2300)
    };

    /// <summary>Where a best total stands on the board, from 1 (a tie goes above the board's own).</summary>
    public static int PlaceOf(int best) => Board.Count(b => b.Score > best) + 1;
}

/// <summary>
/// The ladder at the Battle Institute's counter (plan 37 · FC8, decision 8): a rating of ours (an Elo with a fixed factor)
/// that a win raises and a loss lowers, opponents drawn about the rating with the later sets of its band's tiers, a season to
/// each calendar month of the game's date, each season's rules (Flat 50 single in the odd months, double in the even), bands
/// named by us, a prize in Battle Points by the best band a season reached, and the rating carried into the next season by
/// half. No drawing or input.
/// </summary>
public static class Ladder
{
    /// <summary>The rating a ladder begins at and returns half way to each season, and the factor of a game.</summary>
    public const int Start = 1000, Factor = 32;

    /// <summary>The variables the ladder keeps: the rating, the season (year × 12 + month), the best band this season, and the last results.</summary>
    public const string RatingVariable = "VAR_UNOVA_LADDER_RATING", SeasonVariable = "VAR_UNOVA_LADDER_SEASON",
        BestBandVariable = "VAR_UNOVA_LADDER_BEST_BAND", WinsVariable = "VAR_UNOVA_LADDER_WINS", LossesVariable = "VAR_UNOVA_LADDER_LOSSES";

    /// <summary>The bands' floors (0 Rookie, 1 Challenger, 2 Veteran, 3 Elite, 4 Legend; <c>ladder.band.N</c>).</summary>
    public static readonly int[] BandFloors = { 0, 1100, 1300, 1500, 1700 };

    /// <summary>The Battle Points a season pays by the best band it reached.</summary>
    public static readonly int[] BandPrizes = { 2, 5, 10, 18, 30 };

    public static int BandOf(int rating)
    {
        int band = 0;
        for (int b = 1; b < BandFloors.Length; b++)
            if (rating >= BandFloors[b]) band = b;
        return band;
    }

    /// <summary>The tiers the later sets of a band's opponents are of.</summary>
    public static (int Lowest, int Highest) TiersFor(int band) => band switch { 0 => (1, 2), 1 => (2, 3), 2 => (3, 4), _ => (4, 4) };

    /// <summary>A season of the game's date: its year × 12 + its month (1 to 12).</summary>
    public static int SeasonOf(DateTime date) => date.Year * 12 + date.Month;

    /// <summary>A season's format: single battles in the odd months, double in the even.</summary>
    public static BattleFormat FormatOf(int season) => season % 2 == 1 ? BattleFormat.Single : BattleFormat.Double;

    public static int EnteredIn(int season) => FormatOf(season) == BattleFormat.Double ? 4 : 3;

    /// <summary>The rating carried into the next season: half the way back to <see cref="Start"/>.</summary>
    public static int Carried(int rating) => Start + (rating - Start) / 2;

    /// <summary>The rating after a game against an opponent of <paramref name="opponent"/>'s rating (the Elo, rounded to the nearest).</summary>
    public static int After(int rating, int opponent, bool won)
    {
        double expected = 1.0 / (1.0 + Math.Pow(10, (opponent - rating) / 400.0));
        return Math.Max(0, (int)Math.Round(rating + Factor * ((won ? 1 : 0) - expected), MidpointRounding.AwayFromZero));
    }

    /// <summary>An opponent's rating about the player's: within a hundred either way, on the caller's sixteen-bit draw.</summary>
    public static int OpponentRating(int rating, Func<int> next16) => Math.Max(0, rating - 100 + (next16() & 0xFFFF) % 201);

    /// <summary>An opponent of a rating: a trainer of ours with the later sets of its band's tiers at 50.</summary>
    public static FacilityOpponent Opponent(int season, int opponentRating, Func<int> next16)
    {
        var (low, high) = TiersFor(BandOf(opponentRating));
        return FacilityTrainers.Draw(EnteredIn(season), low, high, FacilityTrainers.IvsFor(BandOf(opponentRating) * 2), next16);
    }

    /// <summary>
    /// A new season reached (the game's month moved on since the ladder was last played): the rating carried by half, the
    /// best band reset to the band of the carried rating; returns the Battle Points the season gone pays (0 for none, or a
    /// first season).
    /// </summary>
    public static int TurnSeason(Story.StoryState story, int season)
    {
        int kept = story.Var(SeasonVariable);
        if (kept == season) return 0;
        int prize = 0;
        if (kept == 0) story.SetVar(RatingVariable, Start);
        else
        {
            if (story.Var(WinsVariable) + story.Var(LossesVariable) > 0) prize = BandPrizes[Math.Clamp(story.Var(BestBandVariable), 0, BandPrizes.Length - 1)];
            story.SetVar(RatingVariable, Carried(story.Var(RatingVariable)));
        }
        story.SetVar(SeasonVariable, season);
        story.SetVar(BestBandVariable, BandOf(story.Var(RatingVariable)));
        story.SetVar(WinsVariable, 0);
        story.SetVar(LossesVariable, 0);
        return prize;
    }

    /// <summary>A game played: the rating moved, the season's best band and results kept.</summary>
    public static void Played(Story.StoryState story, int opponent, bool won)
    {
        int rating = After(story.Var(RatingVariable), opponent, won);
        story.SetVar(RatingVariable, rating);
        story.SetVar(BestBandVariable, Math.Max(story.Var(BestBandVariable), BandOf(rating)));
        if (won) story.SetVar(WinsVariable, story.Var(WinsVariable) + 1);
        else story.SetVar(LossesVariable, story.Var(LossesVariable) + 1);
    }
}
