using System;
using System.Collections.Generic;
using System.Linq;

namespace PokemonPlatinumEngine.Models.Frontier;

/// <summary>
/// A knockout bracket (plan 14 · B8): a number of entrants that is a power of two (sixteen in the Battle Dome, eight in
/// plan 14 · B11's World Tournament), seated in a draw, and rounds of matches whose winners meet in the next. An
/// entrant is a number from nought, which the owner of the bracket keeps its trainers by; a seat is a place in the
/// first round's order, top to bottom, and the matches of a round are the pairs of seats under it. Nothing here
/// knows who the entrants are or how a match is fought: whoever plays it hands the winner over (<see cref="Decide"/>)
/// or a way of deciding it (<see cref="DecideRound"/>). No drawing or input; the draw is on the sixteen-bit draw the
/// caller hands over.
/// </summary>
public sealed class Bracket
{
    /// <summary>The entrants in their seats, top to bottom: <c>Seats[s]</c> is the entrant in seat <c>s</c>.</summary>
    public List<int> Seats { get; set; } = new();

    /// <summary>The winner of each match, round by round (<c>Winners[round][match]</c>), or -1 while it isn't decided.</summary>
    public List<List<int>> Winners { get; set; } = new();

    /// <summary>How many entrants the bracket seats.</summary>
    public int Size => Seats.Count;

    /// <summary>How many rounds it takes to come to one: four for sixteen, three for eight.</summary>
    public int Rounds => Winners.Count;

    /// <summary>The matches of a round: half the entrants of the first, then half as many again each round.</summary>
    public int MatchesIn(int round) => Size >> (round + 1);

    /// <summary>A bracket of <paramref name="entrants"/> in seats of their own number, nothing decided.</summary>
    public static Bracket Of(int entrants)
    {
        if (entrants < 2 || (entrants & (entrants - 1)) != 0)
            throw new ArgumentOutOfRangeException(nameof(entrants), entrants, "A bracket seats a power of two of entrants.");
        var bracket = new Bracket { Seats = Enumerable.Range(0, entrants).ToList() };
        for (int round = 0; entrants >> (round + 1) > 0; round++)
            bracket.Winners.Add(Enumerable.Repeat(-1, entrants >> (round + 1)).ToList());
        return bracket;
    }

    /// <summary>
    /// The draw: the entrants shuffled into the seats one draw at a time from the last seat up (a Fisher and Yates
    /// shuffle on the caller's draw), so a seed gives the same bracket every time.
    /// </summary>
    public static Bracket Draw(int entrants, Func<int> next16)
    {
        var bracket = Of(entrants);
        for (int i = entrants - 1; i > 0; i--)
        {
            int j = (next16() & 0xFFFF) % (i + 1);
            (bracket.Seats[i], bracket.Seats[j]) = (bracket.Seats[j], bracket.Seats[i]);
        }
        return bracket;
    }

    /// <summary>
    /// Who meets in a match: in the first round the two seats under it, in every later round the winners of the two
    /// matches before it; -1 for a side not decided yet.
    /// </summary>
    public (int A, int B) Match(int round, int match)
    {
        if (round == 0) return (Seats[match * 2], Seats[match * 2 + 1]);
        return (Winners[round - 1][match * 2], Winners[round - 1][match * 2 + 1]);
    }

    /// <summary>The match of a round an entrant stands in, or -1 when they aren't in it (out, or not come to it yet).</summary>
    public int MatchOf(int round, int entrant)
    {
        for (int m = 0; m < MatchesIn(round); m++)
        {
            var (a, b) = Match(round, m);
            if (a == entrant || b == entrant) return m;
        }
        return -1;
    }

    /// <summary>The other side of the match an entrant stands in this round, or -1.</summary>
    public int OpponentOf(int round, int entrant)
    {
        int m = MatchOf(round, entrant);
        if (m < 0) return -1;
        var (a, b) = Match(round, m);
        return a == entrant ? b : a;
    }

    /// <summary>The round under way: the first with a match not decided, or <see cref="Rounds"/> once the bracket is over.</summary>
    public int Round
    {
        get
        {
            for (int r = 0; r < Rounds; r++)
                if (Winners[r].Any(w => w < 0)) return r;
            return Rounds;
        }
    }

    /// <summary>Every match is decided.</summary>
    public bool IsOver => Round >= Rounds;

    /// <summary>Who won the last match, once the bracket is over; -1 until then.</summary>
    public int Champion => IsOver ? Winners[^1][0] : -1;

    /// <summary>Whether an entrant has lost no match yet.</summary>
    public bool StillIn(int entrant)
    {
        for (int r = 0; r < Rounds; r++)
        {
            int m = MatchOf(r, entrant);
            if (m < 0) return false;
            int winner = Winners[r][m];
            if (winner < 0) return true;
            if (winner != entrant) return false;
        }
        return true;
    }

    /// <summary>How far an entrant came: the round they lost in, or <see cref="Rounds"/> for the champion.</summary>
    public int ReachedRound(int entrant)
    {
        for (int r = 0; r < Rounds; r++)
        {
            int m = MatchOf(r, entrant);
            if (m < 0 || Winners[r][m] < 0) return r;
            if (Winners[r][m] != entrant) return r;
        }
        return Rounds;
    }

    /// <summary>A match is decided: the winner must be one of the two who meet in it.</summary>
    public void Decide(int round, int match, int winner)
    {
        var (a, b) = Match(round, match);
        if (a < 0 || b < 0) throw new InvalidOperationException($"Match {match} of round {round} has nobody to meet in it yet.");
        if (winner != a && winner != b) throw new ArgumentException($"Entrant {winner} doesn't stand in match {match} of round {round}.", nameof(winner));
        Winners[round][match] = winner;
    }

    /// <summary>
    /// Every match of a round not decided yet is decided by <paramref name="decide"/> (handed the two who meet and
    /// answering the winner), but the match <paramref name="keep"/> stands in, which is left to its owner. A match is
    /// decided top to bottom, so the caller's draws come in the same order every time.
    /// </summary>
    public void DecideRound(int round, Func<int, int, int> decide, int keep = -1)
    {
        for (int m = 0; m < MatchesIn(round); m++)
        {
            if (Winners[round][m] >= 0) continue;
            var (a, b) = Match(round, m);
            if (a < 0 || b < 0 || a == keep || b == keep) continue;
            Decide(round, m, decide(a, b));
        }
    }

    /// <summary>The rest of the bracket decided by <paramref name="decide"/>, round after round, the owner's matches with it.</summary>
    public void DecideAll(Func<int, int, int> decide)
    {
        for (int r = Round; r < Rounds; r++) DecideRound(r, decide);
    }
}
