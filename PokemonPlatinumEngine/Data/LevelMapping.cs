using System;
using System.Collections.Generic;
using System.Linq;

namespace PokemonPlatinumEngine.Data;

/// <summary>
/// A region's levels for a team that crossed into it (plan 18 · W4, the user's option B of 2026-10-21): the region file's
/// <c>levels</c>, pairs of a level of the region's own game and the level it is met at here, <c>[[2, 62], [63, 104]]</c>.
/// A level between two pairs is on the straight line between them, rounded half up; one below the first or above the
/// last is on the line through the two nearest (a region's post-game climbs on past its Champion as its story did), and
/// a single pair moves every level by the same amount. Whatever comes out is held between 1 and
/// <see cref="Ruleset.HighestLevel"/>. The region's own files keep its game's levels: the mapping is applied as a
/// Pokémon is met (<c>GameEngine.LevelHere</c>), and only in a region the game didn't begin in. No drawing or input.
/// </summary>
public sealed class LevelMapping
{
    /// <summary>The pairs, by the region's own level, each (from, to).</summary>
    public IReadOnlyList<(int From, int To)> Points { get; }

    private LevelMapping(IReadOnlyList<(int From, int To)> points) => Points = points;

    /// <summary>
    /// The mapping a region file writes, or null when it writes none (Sinnoh, whose levels are Platinum's own). Throws on
    /// a pair that isn't two numbers, on levels of the region's own given twice or out of order, and on a mapping
    /// that would ever bring a higher level lower.
    /// </summary>
    public static LevelMapping? From(IReadOnlyList<int[]>? pairs)
    {
        if (pairs == null || pairs.Count == 0) return null;
        var points = new List<(int, int)>();
        foreach (var pair in pairs)
        {
            if (pair is not { Length: 2 }) throw new FormatException("a level mapping's pair is two levels: [from, to]");
            if (pair[0] < 1 || pair[1] < 1 || pair[1] > Ruleset.HighestLevel)
                throw new FormatException($"a level mapping's pair [{pair[0]}, {pair[1]}] is out of 1 to {Ruleset.HighestLevel}");
            points.Add((pair[0], pair[1]));
        }
        for (int i = 1; i < points.Count; i++)
        {
            if (points[i].Item1 <= points[i - 1].Item1) throw new FormatException("a level mapping's pairs go up by the region's own level");
            if (points[i].Item2 < points[i - 1].Item2) throw new FormatException("a level mapping never brings a higher level lower");
        }
        return new LevelMapping(points);
    }

    /// <summary>The level a Pokémon of the region's own level <paramref name="level"/> is met at.</summary>
    public int Map(int level)
    {
        if (Points.Count == 1) return Hold(level + Points[0].To - Points[0].From);
        // The segment the level is on, or the nearest one beyond either end
        int i = 0;
        while (i < Points.Count - 2 && level > Points[i + 1].From) i++;
        var (f1, t1) = Points[i];
        var (f2, t2) = Points[i + 1];
        long numerator = (long)(level - f1) * (t2 - t1), denominator = f2 - f1;
        return Hold(t1 + RoundHalfUp(numerator, denominator));
    }

    /// <summary>A division rounded to the nearest whole number, a half up, for either sign.</summary>
    private static int RoundHalfUp(long numerator, long denominator) =>
        (int)Math.Floor((2.0 * numerator + denominator) / (2.0 * denominator));

    private static int Hold(long level) => (int)Math.Clamp(level, 1, Ruleset.HighestLevel);

    public override string ToString() => string.Join(", ", Points.Select(p => $"{p.From} → {p.To}"));
}
