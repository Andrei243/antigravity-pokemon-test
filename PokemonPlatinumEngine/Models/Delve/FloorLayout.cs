using System;
using System.Collections.Generic;
using System.Linq;

namespace PokemonPlatinumEngine.Models.Delve;

/// <summary>
/// A floor laid from pieces (plan 38 · EX6, plan 44 · SD7), moved out of the Battle Pyramid's floor so that every floor
/// laid by rule shares it: the Pyramid's (<see cref="Frontier.PyramidFloor"/>), the hideaways' under the Underground and
/// the delve's hollows. A floor is a grid of squares, each one of a set of pieces of one size whose every piece is open
/// at the middle of each side, so any square joins its neighbours; where the grid's first tile lies on the map is the
/// caller's (a wall round it). What it draws and in what order is what the Pyramid drew, so every Pyramid seed gives the
/// floor it gave (<c>GoldenPyramidTests</c>, <c>pyramid.txt</c>). No drawing or input.
/// </summary>
public sealed class FloorLayout
{
    /// <summary>The pieces the squares are laid from, each <see cref="Square"/> rows of as many characters: '.' is open ground.</summary>
    public IReadOnlyList<string[]> Pieces { get; }

    /// <summary>A square's side, the squares to a side, and where the grid's first tile lies on the map.</summary>
    public int Square { get; }
    public int Squares { get; }
    public int Left { get; }
    public int Top { get; }

    /// <summary>The map the grid is laid on, its walls round it included.</summary>
    public int Width { get; }
    public int Height { get; }

    /// <summary>The piece of each square, row by row.</summary>
    public int[] Layout { get; }

    private readonly bool[] open;

    private FloorLayout(IReadOnlyList<string[]> pieces, int square, int squares, int left, int top, int width, int height, int[] layout)
    {
        (Pieces, Square, Squares, Left, Top, Width, Height, Layout) = (pieces, square, squares, left, top, width, height, layout);
        open = new bool[width * height];
        for (int s = 0; s < layout.Length; s++)
        {
            var piece = pieces[layout[s]];
            int sx = left + s % squares * square, sy = top + s / squares * square;
            for (int y = 0; y < square; y++)
                for (int x = 0; x < square; x++)
                    open[(sy + y) * width + sx + x] = piece[y][x] == '.';
        }
    }

    /// <summary>A grid laid with a piece drawn for each square in turn, row by row, one draw of the pieces' count each.</summary>
    public static FloorLayout Lay(IReadOnlyList<string[]> pieces, int square, int squares, int left, int top, int width, int height, Random rng) =>
        new(pieces, square, squares, left, top, width, height,
            Enumerable.Range(0, squares * squares).Select(_ => rng.Next(pieces.Count)).ToArray());

    /// <summary>A grid laid with the pieces named, square by square.</summary>
    public static FloorLayout Of(IReadOnlyList<string[]> pieces, int square, int squares, int left, int top, int width, int height, int[] layout) =>
        new(pieces, square, squares, left, top, width, height, layout);

    /// <summary>Two different squares for the way in and the way on: one draw for the first, then draws until another.</summary>
    public (int In, int On) ChooseWays(Random rng)
    {
        int count = Squares * Squares;
        int way = rng.Next(count), on;
        do on = rng.Next(count); while (on == way);
        return (way, on);
    }

    /// <summary>Whether a tile of the map is open ground.</summary>
    public bool IsOpen(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height && open[y * Width + x];

    /// <summary>Whether a tile lies inside the grid of squares (the walls round it not).</summary>
    public bool Inside(int x, int y) => x >= Left && x < Left + Square * Squares && y >= Top && y < Top + Square * Squares;

    /// <summary>The open tile nearest a square's middle.</summary>
    public (int X, int Y) MiddleOf(int square)
    {
        int sx = Left + square % Squares * Square, sy = Top + square / Squares * Square;
        (int X, int Y) best = (sx + 3, sy + 3);
        int bestD = int.MaxValue;
        for (int y = sy; y < sy + Square; y++)
            for (int x = sx; x < sx + Square; x++)
            {
                if (!IsOpen(x, y)) continue;
                int d = Math.Abs(2 * x - (2 * sx + Square - 1)) + Math.Abs(2 * y - (2 * sy + Square - 1));
                if (d < bestD) { bestD = d; best = (x, y); }
            }
        return best;
    }

    /// <summary>Every open tile walked to from a tile, by the four ways, past none of the tiles named.</summary>
    public HashSet<(int X, int Y)> Reachable((int X, int Y) from, IReadOnlySet<(int, int)> blocked)
    {
        var seen = new HashSet<(int X, int Y)> { from };
        var queue = new Queue<(int X, int Y)>();
        queue.Enqueue(from);
        while (queue.Count > 0)
        {
            var (x, y) = queue.Dequeue();
            foreach (var (dx, dy) in new[] { (0, -1), (0, 1), (-1, 0), (1, 0) })
            {
                var next = (x + dx, y + dy);
                if (!IsOpen(next.Item1, next.Item2) || blocked.Contains(next) || !seen.Add(next)) continue;
                queue.Enqueue(next);
            }
        }
        return seen;
    }
}
