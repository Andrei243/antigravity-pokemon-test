using System;
using System.Collections.Generic;
using System.Linq;

namespace PokemonPlatinumEngine.Models.Cabinets;

/// <summary>A cell of the picture as the player has marked it: not yet, filled, or crossed out.</summary>
public enum CellMark : byte { None, Filled, Crossed }

/// <summary>
/// The picture-logic cabinet (plan 42 · PS6; an idea from Picross, every rule ours; rulings, "The arcade"), GPU-free: a
/// square grid of <see cref="Size"/> cells whose filled cells are a species' silhouette, with the runs of each row and
/// column at its edge (<see cref="Clues"/>). A picture is offered only when line logic alone solves it to one answer
/// (<see cref="Solve"/>). The player fills or crosses cells; filling a cell outside the picture is a mistake, which
/// crosses it instead; the picture is solved once every cell of it is filled.
/// </summary>
public sealed class PictureLogic
{
    /// <summary>The grid sizes a picture is offered at, smallest first.</summary>
    public static readonly int[] Sizes = { 10, 15, 20 };

    private readonly bool[] picture;
    private readonly CellMark[] marks;

    public string Species { get; }
    public int Size { get; }
    public int Mistakes { get; private set; }
    public IReadOnlyList<int[]> RowClues { get; }
    public IReadOnlyList<int[]> ColumnClues { get; }

    public PictureLogic(string species, int size, bool[] picture)
    {
        if (picture.Length != size * size) throw new ArgumentException("a picture is its size squared", nameof(picture));
        Species = species;
        Size = size;
        this.picture = (bool[])picture.Clone();
        marks = new CellMark[size * size];
        (RowClues, ColumnClues) = Clues(size, picture);
    }

    public bool InPicture(int x, int y) => picture[y * Size + x];
    public CellMark this[int x, int y] => marks[y * Size + x];

    public int FilledCount => marks.Count(m => m == CellMark.Filled);
    public int PictureCount => picture.Count(b => b);

    public bool Solved
    {
        get
        {
            for (int i = 0; i < picture.Length; i++)
                if (picture[i] && marks[i] != CellMark.Filled) return false;
            return true;
        }
    }

    /// <summary>
    /// A cell filled: right (it is the picture's), a mistake (it isn't: crossed and counted), or nothing (marked already,
    /// or the picture solved). Returns whether it was filled.
    /// </summary>
    public bool Fill(int x, int y)
    {
        if (Solved || x < 0 || y < 0 || x >= Size || y >= Size) return false;
        int i = y * Size + x;
        if (marks[i] != CellMark.None) return false;
        if (picture[i])
        {
            marks[i] = CellMark.Filled;
            return true;
        }
        marks[i] = CellMark.Crossed;
        Mistakes++;
        return false;
    }

    /// <summary>A cell crossed out, or a cross taken off (never a filled cell's).</summary>
    public void Cross(int x, int y)
    {
        if (Solved || x < 0 || y < 0 || x >= Size || y >= Size) return;
        int i = y * Size + x;
        marks[i] = marks[i] switch { CellMark.None => CellMark.Crossed, CellMark.Crossed => CellMark.None, var m => m };
    }

    /// <summary>Whether a row's (or a column's) every picture cell is filled, so its clue reads done.</summary>
    public bool LineDone(bool row, int i)
    {
        for (int k = 0; k < Size; k++)
        {
            int c = row ? i * Size + k : k * Size + i;
            if (picture[c] && marks[c] != CellMark.Filled) return false;
        }
        return true;
    }

    // ------------------------------------------------------------------ clues and the solver

    /// <summary>The runs of filled cells along each row (top to bottom) and each column (left to right); an empty line is no run.</summary>
    public static (List<int[]> Rows, List<int[]> Columns) Clues(int size, bool[] picture)
    {
        var rows = new List<int[]>();
        var columns = new List<int[]>();
        for (int i = 0; i < size; i++)
        {
            rows.Add(RunsOf(Enumerable.Range(0, size).Select(k => picture[i * size + k])));
            columns.Add(RunsOf(Enumerable.Range(0, size).Select(k => picture[k * size + i])));
        }
        return (rows, columns);
    }

    private static int[] RunsOf(IEnumerable<bool> line)
    {
        var runs = new List<int>();
        int run = 0;
        foreach (bool b in line)
        {
            if (b) run++;
            else if (run > 0) { runs.Add(run); run = 0; }
        }
        if (run > 0) runs.Add(run);
        return runs.ToArray();
    }

    /// <summary>
    /// Line logic alone: each row and column in turn is given every cell that all its placements agree on, until
    /// nothing more is learnt. Returns the grid it reached (true filled, false empty, null unknown); a picture is offered
    /// only when that grid has no unknown left, which is also the proof it has one answer.
    /// </summary>
    public static bool?[] Solve(int size, IReadOnlyList<int[]> rows, IReadOnlyList<int[]> columns)
    {
        var grid = new bool?[size * size];
        bool changed = true;
        while (changed)
        {
            changed = false;
            for (int pass = 0; pass < 2; pass++)
                for (int i = 0; i < size; i++)
                {
                    var line = new bool?[size];
                    for (int k = 0; k < size; k++) line[k] = grid[pass == 0 ? i * size + k : k * size + i];
                    var learnt = SolveLine(line, pass == 0 ? rows[i] : columns[i]);
                    if (learnt == null) return grid; // a contradiction: no clue's picture, never for a clue made from one
                    for (int k = 0; k < size; k++)
                    {
                        int c = pass == 0 ? i * size + k : k * size + i;
                        if (grid[c] == null && learnt[k] != null)
                        {
                            grid[c] = learnt[k];
                            changed = true;
                        }
                    }
                }
        }
        return grid;
    }

    /// <summary>Whether line logic finishes a picture alone (and so it has one answer).</summary>
    public static bool SolvesAlone(int size, bool[] picture)
    {
        var (rows, columns) = Clues(size, picture);
        var grid = Solve(size, rows, columns);
        for (int i = 0; i < grid.Length; i++)
            if (grid[i] != picture[i]) return false;
        return true;
    }

    /// <summary>
    /// What every placement of a line's runs that agrees with its known cells has in common, worked out from both ends
    /// (whether the first k runs can fill the first j cells, and the last ones the rest); null when none agrees.
    /// </summary>
    internal static bool?[]? SolveLine(bool?[] line, int[] runs)
    {
        int n = line.Length, m = runs.Length;
        // fwd[k, j]: the first k runs can lie in cells [0, j) with cell j−1 (if any) empty or the end of run k
        var fwd = new bool[m + 1, n + 1];
        fwd[0, 0] = true;
        for (int j = 1; j <= n; j++) fwd[0, j] = fwd[0, j - 1] && line[j - 1] != true;
        for (int k = 1; k <= m; k++)
            for (int j = 1; j <= n; j++)
            {
                // cell j−1 empty
                bool can = line[j - 1] != true && fwd[k, j - 1];
                // run k ends at cell j−1
                int len = runs[k - 1];
                int start = j - len;
                if (!can && start >= 0 && Fits(line, start, len))
                {
                    if (k == 1) can = fwd[0, start];
                    else if (start >= 1 && line[start - 1] != true) can = fwd[k - 1, start - 1];
                }
                fwd[k, j] = can;
            }
        if (!fwd[m, n]) return null;
        // bwd[k, j]: runs k.. can lie in cells [j, n)
        var bwd = new bool[m + 2, n + 2];
        bwd[m, n] = true;
        for (int j = n - 1; j >= 0; j--) bwd[m, j] = bwd[m, j + 1] && line[j] != true;
        for (int k = m - 1; k >= 0; k--)
            for (int j = n - 1; j >= 0; j--)
            {
                bool can = line[j] != true && bwd[k, j + 1];
                int len = runs[k];
                if (!can && j + len <= n && Fits(line, j, len))
                {
                    if (k == m - 1) can = bwd[m, j + len];
                    else if (j + len < n && line[j + len] != true) can = bwd[k + 1, j + len + 1];
                }
                bwd[k, j] = can;
            }
        var canFill = new bool[n];
        var canEmpty = new bool[n];
        // A cell may be empty when some split puts the first k runs before it and the rest after it
        for (int j = 0; j < n; j++)
            if (line[j] != true)
                for (int k = 0; k <= m; k++)
                    if (fwd[k, j] && bwd[k, j + 1]) { canEmpty[j] = true; break; }
        // A cell may be filled by run k placed at [s, s+len) with everything round it possible
        for (int k = 0; k < m; k++)
        {
            int len = runs[k];
            for (int s = 0; s + len <= n; s++)
            {
                if (!Fits(line, s, len)) continue;
                bool before = k == 0 ? fwd[0, s] : s >= 1 && line[s - 1] != true && fwd[k, s - 1];
                if (!before) continue;
                bool after = k == m - 1 ? bwd[m, s + len] : s + len < n && line[s + len] != true && bwd[k + 1, s + len + 1];
                if (!after) continue;
                for (int c = s; c < s + len; c++) canFill[c] = true;
            }
        }
        var result = new bool?[n];
        for (int j = 0; j < n; j++)
        {
            if (!canFill[j] && !canEmpty[j]) return null;
            result[j] = canFill[j] && canEmpty[j] ? null : canFill[j];
        }
        return result;
    }

    private static bool Fits(bool?[] line, int start, int len)
    {
        for (int c = start; c < start + len; c++)
            if (line[c] == false) return false;
        return true;
    }
}
