using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models.Frontier;

/// <summary>
/// One of the sixteen in a Battle Dome tournament: the player (<see cref="Trainer"/> -1) or a trainer of the
/// Frontier's own table with the three Pokémon made for them (<see cref="Opponent"/>).
/// </summary>
public sealed class DomeEntrant
{
    /// <summary>The trainer's number among the Frontier's trainers; -1 for the player.</summary>
    public int Trainer { get; init; } = -1;

    /// <summary>The trainer's three, made as the Frontier makes a team (<see cref="FrontierTrainers.ForTrainers"/>); null for the player.</summary>
    public FrontierOpponent? Opponent { get; init; }

    public bool IsPlayer => Trainer < 0;

    /// <summary>The class and the name, as the bracket and the card show them ("Ace Trainer Grace"); empty for the player.</summary>
    public string Title => Opponent is { } o ? $"{o.Record.Class} {o.Record.Name}" : "";
}

/// <summary>
/// What the Battle Dome keeps beside the Frontier's other records (plan 14 · B8; <see cref="FrontierRecords.Dome"/>):
/// the tournaments won in a row and the most ever, every tournament won and every match, and whether a tournament
/// was walked out on, which ends the streak the next time one is begun.
/// </summary>
public sealed class DomeRecord
{
    /// <summary>The tournaments won in a row, counting the one just won.</summary>
    public int Streak { get; set; }

    /// <summary>The longest run of tournaments won in a row.</summary>
    public int Best { get; set; }

    /// <summary>Every tournament won, and every match won in any.</summary>
    public int Won { get; set; }
    public int Matches { get; set; }

    /// <summary>A tournament has been begun and not finished: one walked out on (a game closed in its hall) ends the streak.</summary>
    public bool InProgress { get; set; }
}

/// <summary>
/// The Battle Dome (plan 14 · B8; rulings, "The Battle Dome"): Emerald's tournament as a facility of the Battle
/// Frontier's annex. The player enters three Pokémon, which go in as copies at level 50 (<see cref="FrontierRules"/>)
/// so nothing a battle does reaches them; fifteen trainers of the Frontier's table are drawn by the rooms of the
/// Battle Tower's rows for the tournaments already won in a row, each with three of their own, and the sixteen are
/// shuffled into a <see cref="Bracket"/>. Before each match the player sees the opponent's card (their three) and
/// sends in two of their own; the opponent sends in the two of theirs that do best against the player's three
/// (<see cref="ChooseTwo"/>, a rule of ours). The other matches of a round are fought headless by the battle's own
/// core, the trainer AI on both sides (<see cref="FightHeadless"/>), so the bracket is a real one. Four wins take the
/// tournament; a loss ends it. Every draw is on the sixteen-bit draw the caller hands over. No drawing or input.
/// </summary>
public sealed class BattleDomeChallenge
{
    public const int Entrants = 16, TeamSize = 3, Fielded = 2, Player = 0;

    /// <summary>The places in the team of the three entered, in the order they were entered.</summary>
    public IReadOnlyList<int> Slots { get; }

    /// <summary>The sixteen: the player first, then the fifteen trainers in the order they were drawn.</summary>
    public IReadOnlyList<DomeEntrant> Field { get; }

    public Bracket Bracket { get; }

    /// <summary>The tournaments won in a row before this one.</summary>
    public int StreakBefore { get; }

    /// <summary>The two of the three the player sent in last, as places in <see cref="Slots"/> (0 to 2).</summary>
    public List<int> Sent { get; } = new();

    /// <summary>What the last headless match left: who met, who stood on the player's side, the seed and the winner (for the harness and the tests).</summary>
    public List<(int A, int B, int Home, uint Seed, int Winner)> Fought { get; } = new();

    private BattleDomeChallenge(IReadOnlyList<int> slots, IReadOnlyList<DomeEntrant> field, Bracket bracket, int streakBefore)
    {
        Slots = slots;
        Field = field;
        Bracket = bracket;
        StreakBefore = streakBefore;
    }

    /// <summary>Whether a Pokémon may go into the Dome: the Frontier's rule (no Egg, none of the banned).</summary>
    public static bool MayEnter(Pokemon pokemon) => FrontierRules.MayEnter(pokemon);

    /// <summary>
    /// A tournament begun with the three entered (<paramref name="slots"/>): a tournament walked out on ends the
    /// streak first, then fifteen trainers are drawn (distinct, from the Tower's row of the room the streak has come
    /// to: the more tournaments won in a row, the stronger the field) and given three each, and the sixteen are drawn
    /// into the bracket.
    /// </summary>
    public static BattleDomeChallenge Begin(FrontierRecords records, IReadOnlyList<int> slots, Func<int> next16)
    {
        var dome = records.Dome;
        if (dome.InProgress) dome.Streak = 0;
        dome.InProgress = true;

        int room = Math.Min(7, dome.Streak);
        int[] row = FrontierDatabase.TowerRooms[room];
        var field = new List<DomeEntrant> { new() };
        var taken = new HashSet<int>();
        while (field.Count < Entrants)
        {
            int trainer = row[0] + (next16() & 0xFFFF) % (row[1] - row[0] + 1);
            if (!taken.Add(trainer)) continue;
            var made = FrontierTrainers.ForTrainers(TeamSize, trainer, trainer, false, next16)[0];
            field.Add(new DomeEntrant { Trainer = trainer, Opponent = made });
        }
        return new BattleDomeChallenge(slots.ToList(), field, Bracket.Draw(Entrants, next16), dome.Streak);
    }

    // ------------------------------------------------------------------ the round

    /// <summary>The round under way (0 to 3; 3 is the final), or 4 once the tournament is over.</summary>
    public int Round => Bracket.Round;

    /// <summary>The player has lost no match.</summary>
    public bool PlayerStillIn => Bracket.StillIn(Player);

    /// <summary>The player has won the tournament.</summary>
    public bool PlayerWon => Bracket.Champion == Player;

    /// <summary>The one the player meets in the round under way, or null once they are out or the tournament is over.</summary>
    public DomeEntrant? Opponent => Bracket.OpponentOf(Round, Player) is >= 0 and var o && PlayerStillIn && !Bracket.IsOver ? Field[o] : null;

    /// <summary>The player's three as they are in the team (the Pokémon themselves, for the card and the choice).</summary>
    public List<Pokemon> OwnThree(Party party) =>
        Slots.Where(s => s >= 0 && s < party.Count).Select(s => party.Members[s]).ToList();

    /// <summary>An entrant's three: the player's own, or the copies a trainer's team was made as.</summary>
    public List<Pokemon> ThreeOf(int entrant, Party party) => Field[entrant].IsPlayer ? OwnThree(party) : Field[entrant].Opponent!.Pokemon.ToList();

    /// <summary>
    /// The player sends two of their three in (places in <see cref="Slots"/>); anything but two different places of
    /// the three sends the first two.
    /// </summary>
    public void Send(IEnumerable<int> places)
    {
        var two = places.Where(p => p >= 0 && p < Slots.Count).Distinct().Take(Fielded).ToList();
        Sent.Clear();
        Sent.AddRange(two.Count == Fielded ? two : new List<int> { 0, 1 });
    }

    /// <summary>The team the player's battle is fought with: copies at level 50 of the two sent in, in the order they were sent.</summary>
    public Party TeamForBattle(Party party)
    {
        if (Sent.Count != Fielded) Send(Array.Empty<int>());
        return FrontierRules.TeamFor(party, Sent.Select(p => Slots[p]));
    }

    /// <summary>The opponent of the round as a trainer of the battle, with the two of their three that do best against the player's.</summary>
    public Trainer OpponentAsTrainer(Party party)
    {
        var opponent = Opponent ?? throw new InvalidOperationException("The player meets nobody in this round.");
        return AsTrainer(opponent, OwnThree(party));
    }

    /// <summary>
    /// The two of a team of three sent against another three (a rule of ours, where Emerald's AI rates each move): each
    /// is scored by how well its best damaging move does against each of the other three (the type chart of the rules
    /// in force, the move's power), and the two best go in, the first in the team's order among equals.
    /// </summary>
    public static List<Pokemon> ChooseTwo(IReadOnlyList<Pokemon> mine, IReadOnlyList<Pokemon> theirs)
    {
        float Score(Pokemon p) => theirs.Sum(t => p.Moves
            .Where(m => m.Data.Category != MoveCategory.Status)
            .Select(m => Math.Max(40, m.Data.Power) * TypeChart.GetEffectiveness(m.Data.Type, t.PrimaryType, t.SecondaryType, Ruleset.Current)
                * (p.HasType(m.Data.Type) ? 1.5f : 1f))
            .DefaultIfEmpty(0f).Max());
        return mine.Select((p, i) => (p, i, s: Score(p))).OrderByDescending(x => x.s).ThenBy(x => x.i).Take(Fielded).OrderBy(x => x.i).Select(x => x.p).ToList();
    }

    private static Trainer AsTrainer(DomeEntrant entrant, IReadOnlyList<Pokemon> against)
    {
        var o = entrant.Opponent!;
        var party = new Party();
        foreach (var p in ChooseTwo(o.Pokemon, against)) party.Add(FrontierRules.ForBattle(p));
        return new Trainer
        {
            Id = "frontier_" + o.Record.Id, Name = o.Record.Name, TrainerClass = o.Record.Class, PrizeMoney = 0,
            Ai = FrontierTrainers.Mind, Look = FrontierTrainers.LookOf(o.Record), Party = party
        };
    }

    // ------------------------------------------------------------------ after a match

    /// <summary>
    /// The player won the round's match: it is decided, and every other match of the round is fought headless
    /// (<see cref="FightHeadless"/>), top to bottom. True when that was the final.
    /// </summary>
    public bool Won(FrontierRecords records, Party party, Func<int> next16)
    {
        int round = Round;
        int match = Bracket.MatchOf(round, Player);
        Bracket.Decide(round, match, Player);
        records.Dome.Matches++;
        Bracket.DecideRound(round, (a, b) => FightHeadless(a, b, party, next16));
        return Bracket.IsOver;
    }

    /// <summary>The player lost the round's match: the opponent goes on, and the rest of the round is fought as above.</summary>
    public void Lost(Party party, Func<int> next16)
    {
        int round = Round;
        int match = Bracket.MatchOf(round, Player);
        Bracket.Decide(round, match, Bracket.OpponentOf(round, Player));
        Bracket.DecideRound(round, (a, b) => FightHeadless(a, b, party, next16));
    }

    /// <summary>
    /// A match between two trainers, fought by the battle's core with nobody watching: a draw says which of the two
    /// stands on the player's side (the trainer AI thinks for the other side with its flags, and draws its moves for
    /// the player's side, so the side is drawn to keep it fair), two more the battle's seed; each sends in the two of
    /// their three that do best against the other's. The winner is the side left standing.
    /// </summary>
    public int FightHeadless(int a, int b, Party party, Func<int> next16)
    {
        bool aHome = (next16() & 1) == 0;
        uint seed = (uint)(next16() & 0xFFFF) | (uint)(next16() & 0xFFFF) << 16;
        int home = aHome ? a : b, away = aHome ? b : a;
        var homeThree = ThreeOf(home, party);
        var awayThree = ThreeOf(away, party);
        var homeParty = new Party();
        foreach (var p in Field[home].IsPlayer ? homeThree.Take(Fielded) : ChooseTwo(homeThree, awayThree)) homeParty.Add(FrontierRules.ForBattle(p));
        var core = new BattleCore(new CoreSetup
        {
            PlayerParty = homeParty,
            Trainers = new List<Trainer> { AsTrainer(Field[away], homeThree) },
            Random = new BattleRandom(seed),
            Rules = Ruleset.Current,
            PlayerController = TrainerAi.Instance,
            Frontier = true,
            PlayerName = Field[home].Opponent?.Record.Name ?? "Player"
        });
        core.Start();
        int winner = core.Result switch
        {
            BattleResult.PlayerVictory => home,
            BattleResult.PlayerDefeat => away,
            // A battle that came to no end (it never should): the side with more of its HP left goes on
            _ => Share(core.PlayerParty) >= Share(core.Trainers[0].Party) ? home : away
        };
        Fought.Add((a, b, home, seed, winner));
        return winner;
    }

    private static float Share(Party side) =>
        side.Members.Sum(p => (float)p.CurrentHP) / Math.Max(1, side.Members.Sum(p => p.MaxHP));

    // ------------------------------------------------------------------ the end

    /// <summary>
    /// The tournament is over (won, or lost and left): the record is written. A win counts a tournament and lengthens
    /// the streak; a loss ends it. Either way the tournament is no longer in progress.
    /// </summary>
    public void Finish(FrontierRecords records, bool won)
    {
        var dome = records.Dome;
        dome.InProgress = false;
        if (!won)
        {
            dome.Streak = 0;
            return;
        }
        dome.Streak = Math.Min(FrontierRecords.Most, StreakBefore + 1);
        dome.Best = Math.Max(dome.Best, dome.Streak);
        dome.Won = Math.Min(FrontierRecords.Most, dome.Won + 1);
    }

    /// <summary>The Battle Points a tournament won earns (a rule of ours): three, and one more for each won in a row before it, up to ten.</summary>
    public static int PointsFor(int streakBefore) => Math.Min(10, 3 + Math.Max(0, streakBefore));

    public int PointsEarned => PlayerWon ? PointsFor(StreakBefore) : 0;
}
