using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models.SuperContests;

/// <summary>What is held: a whole contest for a ribbon, or one of its rounds for practice (the original's <c>CONTEST_COMPETITION_...</c>).</summary>
public enum ContestMode { Official, PracticeVisual, PracticeDance, PracticeActing }

/// <summary>One of the four on stage: the trainer, the Pokémon, what it wears and what each round gave it.</summary>
public sealed class Contestant
{
    public string Trainer { get; init; } = "";
    public Pokemon Pokemon { get; init; } = null!;
    public bool IsPlayer { get; init; }

    /// <summary>Which of the original's four ways an opponent thinks and dances (<see cref="ContestOpponent.Mind"/>); the player's is 0.</summary>
    public int Mind { get; init; }

    /// <summary>How well known the Pokémon is, 1 to 5: the applause is as loud.</summary>
    public int Fame { get; init; }
    public bool Woman { get; init; }

    /// <summary>The original's contestant this is, for an opponent.</summary>
    public ContestOpponent? Source { get; init; }

    /// <summary>The accessories it wears, by number, in the order they were put on; an opponent's ready-made dressing has their places too.</summary>
    public List<int> Accessories { get; } = new();
    public ContestDressing? Dressing { get; set; }

    /// <summary>The Visual Competition: the condition's score and the dressing-up's points for the theme, and the hearts each is worth.</summary>
    public int ConditionScore { get; set; }
    public int DressScore { get; set; }
    public int ConditionHearts { get; set; }
    public int DressHearts { get; set; }
    public int VisualHearts => ConditionHearts + DressHearts;

    /// <summary>The Dance Competition's points and the Acting Competition's (ten to a heart).</summary>
    public int DanceScore { get; set; }
    public int ActingScore { get; set; }

    /// <summary>
    /// The results: each round's share of the whole (visual, dance, acting), the length of its bar on the results'
    /// board, the bars together, and the place they give, 0 for the winner.
    /// </summary>
    public int[] Shares { get; } = new int[3];
    public int[] Bars { get; } = new int[3];
    public int Total { get; set; }
    public int Placement { get; set; }
}

/// <summary>
/// A Super Contest (plan 06 · R17a), as the original sets one up and reckons it (<c>src/contest.c</c>,
/// <c>src/unk_02094EDC.c</c>, <c>overlay017</c>): the theme, the three judges and the three opponents drawn, the
/// Visual Competition's hearts, and the results that weigh the three rounds a third each. The Dance and the Acting
/// Competitions are <see cref="SuperContestDance"/> and <see cref="SuperContestActing"/>, which write their scores
/// here. No drawing, no input, no clock; every draw is a 16-bit one from the generator it is given, in the original's
/// order, so a test that hands it a sequence knows what comes out.
/// </summary>
public sealed class SuperContest
{
    public ContestStat Category { get; }
    public ContestRank Rank { get; }
    public ContestMode Mode { get; }

    /// <summary>The Visual Competition's theme, a place of <see cref="SuperContestDatabase.Themes"/>.</summary>
    public int Theme { get; }

    /// <summary>The judges from left to right; the one in the middle (<see cref="ContestRules.MainJudge"/>) is the one drawn.</summary>
    public IReadOnlyList<ContestJudge> Judges { get; }

    /// <summary>The four contestants; the first is the player (<c>PLAYER_CONTESTANT_ID</c>).</summary>
    public IReadOnlyList<Contestant> Contestants { get; }
    public Contestant Player => Contestants[0];

    /// <summary>The results are in (<see cref="Finish"/>).</summary>
    public bool IsOver { get; private set; }

    private readonly Random chance;

    /// <summary>A draw of the original's generator: 16 bits (<c>Contest_GetRNGNext</c>).</summary>
    public int Next16() => chance.Next(0x10000);

    public bool IsPractice => Mode != ContestMode.Official;
    public bool HasVisual => Mode is ContestMode.Official or ContestMode.PracticeVisual;
    public bool HasDance => Mode is ContestMode.Official or ContestMode.PracticeDance;
    public bool HasActing => Mode is ContestMode.Official or ContestMode.PracticeActing;

    /// <summary>The number a contestant is announced by: the player is entry number 4 (<c>Contest_ContestantIDToContestantEntryNum</c>).</summary>
    public static int EntryNumber(int contestant) => ContestRules.Contestants - contestant;

    /// <summary>
    /// Sets a contest up (<c>Contest_Init</c>): the theme is drawn first, then the judge in the middle, then the three
    /// opponents, whose Pokémon are then made. <paramref name="afterGame"/> is the original's "game completed and
    /// National Pokédex obtained", which changes who turns up.
    /// </summary>
    public SuperContest(Pokemon pokemon, string trainer, bool woman, ContestStat category, ContestRank rank, ContestMode mode, bool afterGame, Random chance)
        : this(pokemon, trainer, woman, category, rank, mode, chance, null, afterGame)
    {
    }

    /// <summary>
    /// A contest against three opponents of the caller's own choosing (a test's, most of all): the theme and the
    /// judge are still drawn, in that order, and the opponents' Pokémon made.
    /// </summary>
    public SuperContest(Pokemon pokemon, string trainer, bool woman, ContestStat category, ContestRank rank, ContestMode mode, Random chance, IReadOnlyList<ContestOpponent> opponents)
        : this(pokemon, trainer, woman, category, rank, mode, chance, opponents, false)
    {
    }

    private SuperContest(Pokemon pokemon, string trainer, bool woman, ContestStat category, ContestRank rank, ContestMode mode, Random chance, IReadOnlyList<ContestOpponent>? opponents, bool afterGame)
    {
        Category = category;
        Rank = rank;
        Mode = mode;
        this.chance = chance;
        Theme = ChooseTheme(rank, Next16);
        Judges = ChooseJudges(category, rank, Next16);
        var contestants = new List<Contestant>
        {
            new() { Trainer = trainer, Pokemon = pokemon, IsPlayer = true, Woman = woman, Fame = ContestRules.Fame(pokemon, category) }
        };
        foreach (var opponent in opponents ?? ChooseOpponents(category, rank, mode != ContestMode.Official, afterGame, Next16))
            contestants.Add(new Contestant
            {
                Trainer = opponent.Name,
                Pokemon = PokemonOf(opponent, chance),
                Mind = opponent.Mind,
                Fame = opponent.Fame,
                Woman = opponent.Gender == 1,
                Source = opponent
            });
        Contestants = contestants;

        // sub_020951B0: each opponent wears the dressing it keeps for the theme (none in a dance held for itself)
        if (mode != ContestMode.PracticeDance)
            foreach (var c in Contestants.Where(c => c.Source != null && c.Source.Dressings.Count > Theme))
            {
                c.Dressing = SuperContestDatabase.Dressings[c.Source!.Dressings[Theme]];
                c.Accessories.AddRange(c.Dressing.Ids);
            }
    }

    // ------------------------------------------------------------------ the draws

    /// <summary>
    /// The theme (<c>sub_02095A74</c>): any of the twelve at the Master Rank; under it one of three, with three more
    /// from the Great Rank and three more from the Ultra Rank.
    /// </summary>
    public static int ChooseTheme(ContestRank rank, Func<int> next16)
    {
        if (rank == ContestRank.Master) return next16() % 12;
        var themes = new List<int> { 2, 3, 4 };
        if (rank >= ContestRank.Great) themes.AddRange(new[] { 0, 1, 5 });
        if (rank >= ContestRank.Ultra) themes.AddRange(new[] { 6, 7, 8 });
        return themes[next16() % themes.Count];
    }

    /// <summary>
    /// The three judges (<c>sub_020954F0</c>): the rank's first two who sit at the sides for the quality, and in the
    /// middle one drawn from those who may sit there.
    /// </summary>
    public static IReadOnlyList<ContestJudge> ChooseJudges(ContestStat category, ContestRank rank, Func<int> next16)
    {
        var sides = new List<ContestJudge>();
        var middles = new List<ContestJudge>();
        foreach (var judge in SuperContestDatabase.Judges)
        {
            if (judge.Rank != rank) continue;
            int seat = judge.Seats[(int)category];
            if (seat > 1) middles.Add(judge);
            else if (seat == 1) sides.Add(judge);
        }
        if (sides.Count < 2 || middles.Count < 1) throw new InvalidOperationException($"No judges for a {rank} Rank {category} Contest.");
        var middle = middles[next16() % middles.Count];
        return new[] { sides[0], middle, sides[1] };
    }

    /// <summary>
    /// The three opponents (<c>sub_02094F04</c>): of the rank's contestants who enter the quality's contests at this
    /// time of the story (those of practice rounds for a practice round, and never those for the rest), three are
    /// drawn one after another; a guest is never drawn that way, but when the list has any, one of them is drawn first
    /// and takes the place of one of the three at the end.
    /// </summary>
    public static IReadOnlyList<ContestOpponent> ChooseOpponents(ContestStat category, ContestRank rank, bool practice, bool afterGame, Func<int> next16)
    {
        var list = SuperContestDatabase.Opponents.Where(o =>
            o.Rank == rank
            && (afterGame ? o.Season != 1 : o.Season is not (2 or 3))
            && (practice ? o.Practice : !o.Practice && !o.SingleRound)
            && o.Contests.Contains(category)).ToList();
        const int wanted = ContestRules.Contestants - 1;
        if (list.Count < wanted) throw new InvalidOperationException($"No opponents for a {rank} Rank {category} Contest.");

        var guests = list.Where(o => o.Season == 3).ToList();
        ContestOpponent? guest = guests.Count > 0 ? guests[next16() % guests.Count] : null;

        var chosen = new List<ContestOpponent>();
        while (chosen.Count < wanted)
        {
            var pick = list[next16() % list.Count];
            if (pick.Season == 3) continue;
            chosen.Add(pick);
            list.Remove(pick);
        }
        if (guest != null) chosen[next16() % wanted] = guest;
        return chosen;
    }

    /// <summary>
    /// An opponent's Pokémon (<c>sub_02095380</c>): level 10, the trainer's gender where the species has two (the
    /// personality Cute Charm builds, with no nature added), never shiny, IVs drawn; the four moves, the nickname and
    /// the condition are the table's.
    /// </summary>
    public static Pokemon PokemonOf(ContestOpponent opponent, Random chance)
    {
        var species = PokemonDatabase.Get(opponent.Species) ?? throw new InvalidOperationException($"There is no species '{opponent.Species}'.");
        uint personality = Personality.CuteCharm(species, opponent.Gender == 0 ? Gender.Male : Gender.Female, 0);
        var pokemon = new Pokemon(species, 10, personality, chance)
        {
            IsShiny = false,
            Nickname = opponent.Nickname,
            Cool = opponent.Cool,
            Beauty = opponent.Beauty,
            Cute = opponent.Cute,
            Smart = opponent.Smart,
            Tough = opponent.Tough,
            Sheen = opponent.Sheen,
            OriginalTrainer = new TrainerMark(opponent.Name, 0, opponent.Gender == 0 ? Core.PlayerLook.Boy : Core.PlayerLook.Girl)
        };
        pokemon.Moves.Clear();
        foreach (string move in opponent.Moves) pokemon.Moves.Add(MoveDatabase.Create(move));
        return pokemon;
    }

    // ------------------------------------------------------------------ the Visual Competition

    /// <summary>
    /// Judges the Visual Competition (<c>ov17_0223F374</c>, <c>ov17_0223F334</c>): the player's Pokémon wears what it
    /// was given, as many as the rank allows; each Pokémon's condition is scored for the contest's quality and its
    /// accessories for the theme, and each score is turned into hearts.
    /// </summary>
    public void JudgeVisual(IEnumerable<int> playerAccessories)
    {
        Player.Accessories.Clear();
        Player.Accessories.AddRange(playerAccessories.Take(ContestRules.AccessoryLimit(Rank)));
        var theme = SuperContestDatabase.Themes[Theme];
        foreach (var c in Contestants)
        {
            var p = c.Pokemon;
            c.ConditionScore = ContestRules.ConditionScore(Category, new[] { p.Cool, p.Beauty, p.Cute, p.Smart, p.Tough }, p.Sheen, p.HeldItem?.Name);
            c.DressScore = c.Accessories.Sum(theme.PointsFor);
            c.ConditionHearts = ContestRules.ConditionHearts(Rank, c.ConditionScore);
            c.DressHearts = ContestRules.DressHearts(Rank, c.DressScore);
        }
    }

    // ------------------------------------------------------------------ the results

    /// <summary>What a round gave each contestant: 0 the Visual Competition's hearts, 1 the dance's points, 2 the acting's.</summary>
    public int[] RoundScores(int round) => Contestants.Select(c => round switch { 0 => c.VisualHearts, 1 => c.DanceScore, _ => c.ActingScore }).ToArray();

    /// <summary>The weight of a round in ten-thousandths (<c>ov17_02251860</c>): a third each in a contest, the whole for a round held alone.</summary>
    public int Weight(int round) => Mode switch
    {
        ContestMode.Official => 3333,
        ContestMode.PracticeVisual => round == 0 ? 10000 : 0,
        ContestMode.PracticeDance => round == 1 ? 10000 : 0,
        _ => round == 2 ? 10000 : 0
    };

    /// <summary>
    /// Each contestant's share of a round (<c>ov17_02251930</c>): the round's weight is divided by the best score, in
    /// whole numbers, and each score takes that many hundredths, rounded. A round nobody scored in gives nobody
    /// anything (the original divides by nought there).
    /// </summary>
    public int[] Shares(int round)
    {
        int[] scores = RoundScores(round);
        int best = scores.Max();
        int each = best > 0 ? Weight(round) / best : 0;
        return scores.Select(s => (s * each + 50) / 100).ToArray();
    }

    /// <summary>
    /// The order of the Acting Competition's first turn (<c>ov17_0223EEB8</c>): the best of the Visual and Dance
    /// Competitions together goes first, and of two level the one entered sooner. A round of acting held alone starts
    /// with the player last (<c>Contest_Init</c>).
    /// </summary>
    public int[] ActingOrder()
    {
        if (Mode != ContestMode.Official) return new[] { 3, 2, 1, 0 };
        int[] visual = Shares(0), dance = Shares(1);
        return Enumerable.Range(0, ContestRules.Contestants).OrderByDescending(c => visual[c] + dance[c]).ToArray();
    }

    /// <summary>
    /// The results (<c>ov17_02251A1C</c>): each round's shares become bars on the board, a round's best the full
    /// length of its part, and the bars together decide the places; of two level, a draw for each decides. A round
    /// held alone only says how many did better than the player (<c>FieldTask_RunPracticeCompetition</c>).
    /// </summary>
    public void Finish()
    {
        IsOver = true;
        if (IsPractice)
        {
            int[] scores = RoundScores(Mode == ContestMode.PracticeVisual ? 0 : Mode == ContestMode.PracticeDance ? 1 : 2);
            Player.Placement = scores.Skip(1).Count(s => s > scores[0]);
            return;
        }

        for (int round = 0; round < 3; round++)
        {
            int[] shares = Shares(round);
            int best = shares.Max();
            // ov17_02251914: the round's part of the board's 192 dots
            int length = (192 * Weight(round) + 5000) / 10000;
            for (int c = 0; c < Contestants.Count; c++)
            {
                Contestants[c].Shares[round] = shares[c];
                Contestants[c].Bars[round] = length * (best > 0 ? 100 * shares[c] / best : 0) / 100;
            }
        }
        foreach (var c in Contestants) c.Total = c.Bars.Sum();

        int[] totals = Contestants.Select(c => c.Total).ToArray();
        int[] order = { 0, 1, 2, 3 };
        int[] draws = new int[4];
        for (int c = 0; c < 4; c++) draws[c] = Next16();
        for (int i = 0; i < 3; i++)
            for (int j = 3; j > i; j--)
                if (totals[j - 1] < totals[j] || (totals[j - 1] == totals[j] && draws[j - 1] < draws[j]))
                {
                    (totals[j - 1], totals[j]) = (totals[j], totals[j - 1]);
                    (order[j - 1], order[j]) = (order[j], order[j - 1]);
                    (draws[j - 1], draws[j]) = (draws[j], draws[j - 1]);
                }
        for (int place = 0; place < 4; place++) Contestants[order[place]].Placement = place;
    }

    /// <summary>Who came first.</summary>
    public Contestant Winner => Contestants.First(c => c.Placement == 0);

    /// <summary>The contestants from first to last.</summary>
    public IEnumerable<Contestant> Standing => Contestants.OrderBy(c => c.Placement);
}
