using System;
using System.Text;

namespace PokemonPlatinumEngine.Battle.Sim;

/// <summary>
/// Whose words a battle's lines are in (plan 28 · I3, the screen's half). Every battle is told from the player's side:
/// "Turtwig" for one's own, "Foe Starly" for the other side's, "Go! Turtwig!" against "Barry sent out Starly!". A link
/// battle has two players, so the server's one log must read right to both: while a link battle's course runs
/// (<see cref="BattleCore.Link"/>), a line that differs by its reader's side is written with both readings in a marker
/// (<see cref="Sided"/>), and each side's view resolves the markers for its own reader (<see cref="For"/>) before
/// anything is sent. Outside a link battle nothing is marked and every line is written as it always was, so no golden
/// battle moved. GPU-free; the marker never reaches a screen.
/// </summary>
public static class Narration
{
    // A marker: Open, the side the first reading is for (P or E), the first reading, Between, the second reading, Close
    private const char Open = '\u0001', Between = '\u001F', Close = '\u0002';

    [ThreadStatic] private static bool neutral;

    /// <summary>Whether lines are being written for both sides now (a link battle's course is running on this thread).</summary>
    public static bool Neutral => neutral;

    /// <summary>Writes lines for both sides while the returned scope is open (only when <paramref name="link"/>).</summary>
    public static Scope ForBothSides(bool link) => new(link);

    /// <summary>A stretch of a battle's course written for both sides; scopes nest, the outer one's state put back at the end.</summary>
    public readonly struct Scope : IDisposable
    {
        private readonly bool before;

        public Scope(bool link)
        {
            before = neutral;
            neutral = link || before;
        }

        public void Dispose() => neutral = before;
    }

    /// <summary>
    /// A line that reads <paramref name="toThatSide"/> to a reader on <paramref name="side"/> and
    /// <paramref name="toTheOther"/> to a reader on the other: the second reading where nobody but the player reads.
    /// </summary>
    public static string Sided(BattleSide side, string toThatSide, string toTheOther)
    {
        if (!neutral) return side == BattleSide.Player ? toThatSide : toTheOther;
        return $"{Open}{(side == BattleSide.Player ? 'P' : 'E')}{toThatSide}{Between}{toTheOther}{Close}";
    }

    /// <summary>A line as a reader on <paramref name="reader"/> reads it: every marker resolved, the innermost first.</summary>
    public static string For(string text, BattleSide reader)
    {
        if (text.IndexOf(Open) < 0) return text;
        var s = text;
        while (true)
        {
            int close = s.IndexOf(Close);
            if (close < 0) return s;
            int open = s.LastIndexOf(Open, close);
            if (open < 0) return s;
            var inner = s.Substring(open + 1, close - open - 1);
            int between = inner.IndexOf(Between);
            if (inner.Length < 1 || between < 1) return s;
            var side = inner[0] == 'P' ? BattleSide.Player : BattleSide.Enemy;
            var reading = side == reader ? inner.Substring(1, between - 1) : inner.Substring(between + 1);
            s = new StringBuilder(s.Length).Append(s, 0, open).Append(reading).Append(s, close + 1, s.Length - close - 1).ToString();
        }
    }

    /// <summary>Whether a line carries a marker still.</summary>
    public static bool IsMarked(string text) => text.IndexOf(Open) >= 0;
}
