using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models.Contest;

/// <summary>Whether a Pokémon may enter a contest of a rank (<c>GetContestEntryEligibility</c>).</summary>
public enum AppealEntry
{
    /// <summary>Not yet worthy of the rank: it lacks the ribbon of the rank below.</summary>
    TooLow,
    /// <summary>It may, and the rank is its next.</summary>
    Equal,
    /// <summary>It may, though it has won this rank already.</summary>
    Above,
    Egg,
    Fainted
}

/// <summary>What a Hoenn contest left behind, for the script and the screen to tell.</summary>
public sealed record AppealOutcome(
    ContestStat Category, AppealRank Rank, int Placement, int Points, bool Won, string? Ribbon, bool GaveLuxuryBall, bool PaintingOffered, int ConditionPoints)
{
    public bool GaveRibbon => Ribbon != null;
}

/// <summary>
/// The rules around Hoenn's contests (plan 21 · H11; Emerald's <c>src/contest.c</c>, <c>contest_util.c</c> and
/// <c>data/scripts/contest_hall.inc</c>): who may enter, who they meet, what winning gives. The appeals themselves
/// are <see cref="AppealRound"/>'s. No drawing, no input.
/// </summary>
public static class HoennContestRules
{
    public const int Ranks = 4;

    /// <summary>The ribbon of a quality's contest at a rank (<c>RIBBON_HOENN_COOL</c>, <c>RIBBON_HOENN_COOL_SUPER</c>…).</summary>
    public static string RibbonOf(ContestStat category, AppealRank rank) =>
        "RIBBON_HOENN_" + category.ToString().ToUpperInvariant() + rank switch
        {
            AppealRank.Normal => "",
            AppealRank.Super => "_SUPER",
            AppealRank.Hyper => "_HYPER",
            _ => "_MASTER"
        };

    /// <summary>
    /// How many ranks of a quality's contest the Pokémon has won (Emerald's ribbon byte, 0 to 4): the highest rank whose
    /// ribbon it wears and one more.
    /// </summary>
    public static int RibbonCount(Pokemon pokemon, ContestStat category)
    {
        int count = 0;
        for (int rank = 0; rank < Ranks; rank++)
            if (pokemon.Ribbons.Contains(RibbonOf(category, (AppealRank)rank))) count = rank + 1;
        return count;
    }

    /// <summary>
    /// Whether the Pokémon may enter (<c>GetContestEntryEligibility</c>): never an Egg or a fainted one, and a rank above
    /// the first only with the ribbon of the rank below.
    /// </summary>
    public static AppealEntry Check(Pokemon pokemon, ContestStat category, AppealRank rank)
    {
        if (pokemon.IsEgg) return AppealEntry.Egg;
        if (pokemon.IsFainted) return AppealEntry.Fainted;
        int ribbon = RibbonCount(pokemon, category);
        if (ribbon > (int)rank) return AppealEntry.Above;
        if (ribbon >= (int)rank) return AppealEntry.Equal;
        return AppealEntry.TooLow;
    }

    /// <summary>Whether it has won this contest at this rank or higher before (<c>HasMonWonThisContestBefore</c>).</summary>
    public static bool HasWonBefore(Pokemon pokemon, ContestStat category, AppealRank rank) => RibbonCount(pokemon, category) > (int)rank;

    /// <summary>The scarf that adds twenty to a quality of the condition (<c>CreateContestMonFromParty</c>).</summary>
    public static string ScarfOf(ContestStat quality) => quality switch
    {
        ContestStat.Cool => "Red Scarf",
        ContestStat.Beauty => "Blue Scarf",
        ContestStat.Cute => "Pink Scarf",
        ContestStat.Smart => "Green Scarf",
        _ => "Yellow Scarf"
    };

    /// <summary>The player's Pokémon as it takes the stage: its condition with the scarf it holds counted, each held to 255.</summary>
    public static Appealer PlayerAppealer(Pokemon pokemon, string trainer)
    {
        var stats = new[] { pokemon.Cool, pokemon.Beauty, pokemon.Cute, pokemon.Smart, pokemon.Tough };
        string? held = pokemon.HeldItem?.Name;
        for (int q = 0; q < 5; q++)
            if (held != null && held == ScarfOf((ContestStat)q)) stats[q] += 20;
        for (int q = 0; q < 5; q++) stats[q] = Math.Min(255, stats[q]);
        return new Appealer
        {
            Trainer = trainer,
            Pokemon = pokemon,
            IsPlayer = true,
            Stats = stats,
            Sheen = pokemon.Sheen,
            Moves = pokemon.Moves.Select(m => AppealMove.Of(m.Data)).Take(4).ToList()
        };
    }

    /// <summary>An Emerald contestant as a Pokémon on the stage: its species at a middling level, its condition and four moves from the table.</summary>
    public static Appealer OpponentAppealer(HoennContestOpponent opponent, Random chance)
    {
        var species = PokemonDatabase.Get(opponent.Species) ?? throw new InvalidOperationException($"There is no species '{opponent.Species}'.");
        var pokemon = new Pokemon(species, 30, chance)
        {
            IsShiny = false,
            Cool = opponent.Cool,
            Beauty = opponent.Beauty,
            Cute = opponent.Cute,
            Smart = opponent.Smart,
            Tough = opponent.Tough,
            Sheen = opponent.Sheen
        };
        pokemon.Moves.Clear();
        foreach (string move in opponent.Moves) pokemon.Moves.Add(MoveDatabase.Create(move));
        return new Appealer
        {
            Trainer = opponent.Trainer,
            Pokemon = pokemon,
            Stats = new[] { opponent.Cool, opponent.Beauty, opponent.Cute, opponent.Smart, opponent.Tough },
            Sheen = opponent.Sheen,
            Moves = opponent.Moves.Select(m => AppealMove.Of(MoveDatabase.Get(m))).ToList(),
            Source = opponent
        };
    }

    /// <summary>
    /// The contestants who may be met in a contest of this rank and quality (<c>SetContestants</c>): those of the table
    /// of the rank who enter the quality's contest, and turn up before or after the game's end as the game stands.
    /// </summary>
    public static List<HoennContestOpponent> Candidates(ContestStat category, AppealRank rank, bool afterTheGame) =>
        HoennContestDatabase.Opponents.Where(o =>
            o.Rank == (int)rank
            && o.Pools.Contains(category)
            && (afterTheGame ? o.Time != OpponentTime.OnlyBeforeTheGame : o.Time != OpponentTime.OnlyAfterTheGame)).ToList();

    /// <summary>
    /// Three opponents drawn one by one from the candidates (<c>SetContestants</c>), with the player last on the stage
    /// as Emerald seats them. <paramref name="afterTheGame"/> is the original's "game completed": Hoenn's Hall of Fame.
    /// </summary>
    public static List<Appealer> Stage(Pokemon player, string trainer, ContestStat category, AppealRank rank, bool afterTheGame, Random chance)
    {
        var pool = Candidates(category, rank, afterTheGame);
        var chosen = new List<HoennContestOpponent>();
        for (int i = 0; i < AppealRound.Contestants - 1; i++)
        {
            int at = chance.Next(pool.Count);
            chosen.Add(pool[at]);
            pool.RemoveAt(at);
        }
        var stage = chosen.Select(o => OpponentAppealer(o, chance)).ToList();
        stage.Add(PlayerAppealer(player, trainer));
        return stage;
    }

    /// <summary>
    /// What the contest gives the player's Pokémon once it is over (<c>GivePrizeIfWinner</c>, <c>GiveMonContestRibbon</c>,
    /// <c>GiveMonArtistRibbon</c>): the winner gets the ribbon of the rank, or a Luxury Ball for winning a Master Rank
    /// again; a Master Rank win of 800 points or more brings the painter. Practice gives nothing in Emerald; this is for
    /// the official contest alone.
    /// </summary>
    public static AppealOutcome Conclude(AppealRound round, Pokemon pokemon) =>
        Conclude(round.Category, round.Rank, pokemon, round.FinalStandings[round.PlayerIndex], round.TotalPoints[round.PlayerIndex],
            round.Appealers[round.PlayerIndex].Round1Points);

    /// <summary>As <see cref="Conclude(AppealRound, Pokemon)"/> for a contest whose place and points are known (tests, scripts played without a screen).</summary>
    public static AppealOutcome Conclude(ContestStat category, AppealRank rank, Pokemon pokemon, int place, int points, int condition = 0)
    {
        bool won = place == 0;
        string? ribbon = null;
        bool luxury = false, painting = false;
        if (won)
        {
            bool before = HasWonBefore(pokemon, category, rank);
            if (!before)
            {
                ribbon = RibbonOf(category, rank);
                pokemon.Ribbons.Add(ribbon);
            }
            else if (rank == AppealRank.Master)
            {
                luxury = true;
            }
            painting = rank == AppealRank.Master && points >= AppealRules.PaintingPoints;
        }
        return new AppealOutcome(category, rank, place, points, won, ribbon, luxury, painting, condition);
    }

    /// <summary>
    /// Counts a contest held (entered, and won) and hangs the winner's picture on the Contest Hall's wall
    /// (<c>Contest_EndContest</c>'s counts, <c>SaveContestWinner</c>); the game and the headless host both call it.
    /// </summary>
    public static void Record(AppealOutcome outcome, Pokemon pokemon, string trainer, HoennContestRecords records)
    {
        records.Count(outcome);
        if (outcome.Won) records.Hang(PaintingOf(outcome, pokemon, trainer));
    }

    /// <summary>The winner as the wall and the museum show it.</summary>
    public static ContestPainting PaintingOf(AppealOutcome outcome, Pokemon pokemon, string trainer) => new()
    {
        Species = pokemon.Species.Name,
        Nickname = pokemon.Nickname,
        Trainer = trainer,
        Category = outcome.Category,
        Rank = outcome.Rank,
        Points = outcome.Points
    };

    /// <summary>Whether the painter's offer also gives the Artist Ribbon (<c>GiveMonArtistRibbon</c>): once for each Pokémon.</summary>
    public static bool GivesArtistRibbon(Pokemon pokemon) => !pokemon.Ribbons.Contains(ArtistRibbon);

    public const string ArtistRibbon = "RIBBON_HOENN_ARTIST";

    /// <summary>The flag that a painting of a quality's has been made (<c>FLAG_COOL_PAINTING_MADE</c> and its fellows).</summary>
    public static string PaintingFlag(ContestStat category) => "FLAG_" + category.ToString().ToUpperInvariant() + "_PAINTING_MADE";

    /// <summary>The flag a win at the Master Rank sets for its quality (Hoenn's own, beside Sinnoh's).</summary>
    public static string MasterFlag(ContestStat category) => "FLAG_HOENN_CONTEST_MASTER_" + category.ToString().ToUpperInvariant();
}

/// <summary>One painting hung in the Lilycove Museum or the Contest Hall: the winner as it was.</summary>
public sealed class ContestPainting
{
    public string Species { get; set; } = string.Empty;
    public string Nickname { get; set; } = string.Empty;
    public string Trainer { get; set; } = string.Empty;
    public ContestStat Category { get; set; }
    public AppealRank Rank { get; set; }
    public int Points { get; set; }
}

/// <summary>
/// What the game keeps of Hoenn's contests (plan 21 · H11, saved as <c>SaveData.HoennContests</c>): the contests entered
/// and won for each quality and rank, the pictures of the museum (one for each quality, from a Master Rank winner of
/// 800 points or more) and of the Contest Hall's wall (the last six winners). No drawing, no input.
/// </summary>
public sealed class HoennContestRecords
{
    public const int HallPaintings = 6;

    /// <summary>Entered and won, <c>[quality, rank]</c>, each held to 9,999.</summary>
    public int[][] Entered { get; set; } = NewGrid();
    public int[][] Won { get; set; } = NewGrid();

    /// <summary>The museum's paintings by quality, null where none hangs yet.</summary>
    public ContestPainting?[] Museum { get; set; } = new ContestPainting?[5];

    /// <summary>The wall of the Contest Hall: the last winners, newest first.</summary>
    public List<ContestPainting> Hall { get; set; } = new();

    private static int[][] NewGrid() => Enumerable.Range(0, 5).Select(_ => new int[HoennContestRules.Ranks]).ToArray();

    /// <summary>Counts a contest entered; <paramref name="outcome"/> is null while it is still to be held.</summary>
    public void Count(AppealOutcome outcome)
    {
        int q = (int)outcome.Category, r = (int)outcome.Rank;
        Entered[q][r] = Math.Min(9999, Entered[q][r] + 1);
        if (outcome.Won) Won[q][r] = Math.Min(9999, Won[q][r] + 1);
    }

    /// <summary>Whether nothing has been entered, hung or won (the save keeps null).</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public bool IsEmpty => TotalEntered == 0 && TotalWon == 0 && Museum.All(p => p == null) && Hall.Count == 0;

    public int TotalEntered => Entered.Sum(row => row.Sum());
    public int TotalWon => Won.Sum(row => row.Sum());

    /// <summary>The winners' wall takes the new winner on top and lets the seventh go (<c>SaveContestWinner</c>).</summary>
    public void Hang(ContestPainting winner)
    {
        Hall.Insert(0, winner);
        if (Hall.Count > HallPaintings) Hall.RemoveRange(HallPaintings, Hall.Count - HallPaintings);
    }

    /// <summary>The painter takes the winner to the museum for good, in the place of its quality's (<c>SaveMuseumContestPainting</c>).</summary>
    public void HangInMuseum(ContestPainting winner) => Museum[(int)winner.Category] = winner;

    /// <summary>How many of the museum's five paintings hang (<c>CountPlayerMuseumPaintings</c>).</summary>
    public int MuseumCount => Museum.Count(p => p != null);
}
