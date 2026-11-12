using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models.Cabinets;

/// <summary>A run cleared on the board: its species' place (0 to 3), its length, the cascade it fell in (1 the swap's own) and the damage it dealt.</summary>
public readonly record struct MatchClear(int Piece, int Length, int Cascade, int Damage, float Effect);

/// <summary>How a swap went: refused (not neighbours, or no line made), or made, with what it cleared.</summary>
public enum SwapResult { NotNeighbours, NoLine, Made, Over }

/// <summary>
/// The match-three cabinet (plan 42 · PS6; an idea from Pokémon Shuffle, every rule and number ours; rulings, "The
/// arcade"), GPU-free: a board <see cref="Side"/> by <see cref="Side"/> of the icons of four species, and a stage's foe.
/// Two neighbours swapped that make a line of three or more clear it; each run deals damage by its species' first type
/// against the foe's types on the battle's own chart (<see cref="TypeChart.GetEffectiveness"/>), more for four or five
/// and more again for each cascade, and what is cleared falls and is refilled from the board's own generator. A swap
/// that makes no line is refused and costs nothing; a board with no swap left is shuffled. Every draw is on a
/// <see cref="SplitMix"/> seeded by the stage, so a stage is a pure function of its seed and the swaps.
/// </summary>
public sealed class MatchThree
{
    public const int Side = 6, Cells = Side * Side, Pieces = 4;

    /// <summary>A run of three's damage before the chart; four and five add to it.</summary>
    public const int Base = 30, PerExtra = 15;

    /// <summary>Each cascade after the first adds this share of a run's damage.</summary>
    public const float CascadeBonus = 0.25f;

    private readonly SplitMix random;
    private readonly int[] cells = new int[Cells];

    public CabinetStage Stage { get; }

    /// <summary>The four species on the board, by their place 0 to 3.</summary>
    public IReadOnlyList<string> Species { get; }
    private readonly PokemonType[] pieceTypes;

    public int Hp { get; private set; }
    public int SwapsLeft { get; private set; }
    public int Score { get; private set; }
    public int Shuffles { get; private set; }

    /// <summary>What the last swap cleared, in the order it fell (empty after a refused one).</summary>
    public List<MatchClear> LastClears { get; } = new();

    public bool Cleared => Hp <= 0;
    public bool Over => Cleared || SwapsLeft <= 0;

    public MatchThree(CabinetStage stage, IReadOnlyList<string> species)
    {
        Stage = stage;
        if (species.Count != Pieces) throw new ArgumentException($"a board takes {Pieces} species", nameof(species));
        Species = species.ToArray();
        pieceTypes = Species.Select(s => PokemonDatabase.Get(s)?.PrimaryType ?? PokemonType.Normal).ToArray();
        Hp = stage.Hp;
        SwapsLeft = stage.Swaps;
        random = new SplitMix(stage.Seed);
        Deal();
    }

    /// <summary>The piece at column <paramref name="x"/>, row <paramref name="y"/> (0 to 3).</summary>
    public int this[int x, int y] => cells[y * Side + x];

    /// <summary>The board as rows of digits, for tests and the harness's lines.</summary>
    public string Rows() => string.Join("/", Enumerable.Range(0, Side).Select(y => string.Concat(Enumerable.Range(0, Side).Select(x => (char)('0' + this[x, y])))));

    // A board drawn cell by cell, never a third of a kind beside two (left or above); redrawn while it has no swap
    private void Deal()
    {
        do
        {
            for (int y = 0; y < Side; y++)
                for (int x = 0; x < Side; x++)
                {
                    int piece;
                    do piece = random.Int(Pieces);
                    while ((x >= 2 && cells[y * Side + x - 1] == piece && cells[y * Side + x - 2] == piece)
                        || (y >= 2 && cells[(y - 1) * Side + x] == piece && cells[(y - 2) * Side + x] == piece));
                    cells[y * Side + x] = piece;
                }
        } while (!HasSwap());
    }

    /// <summary>The foe's types, which the chart is read against.</summary>
    public PokemonType FoeType1 => Stage.FoeTypes.Count > 0 ? Stage.FoeTypes[0] : PokemonType.Normal;
    public PokemonType? FoeType2 => Stage.FoeTypes.Count > 1 ? Stage.FoeTypes[1] : null;

    /// <summary>A piece's species' first type, which it hits with and whose colour its tile wears.</summary>
    public PokemonType TypeOf(int piece) => pieceTypes[piece];

    /// <summary>How a piece's species does against the foe (the battle's chart under the modern rules).</summary>
    public float EffectOf(int piece) => TypeChart.GetEffectiveness(pieceTypes[piece], FoeType1, FoeType2, Ruleset.Modern);

    /// <summary>Whether two cells are side by side.</summary>
    public static bool Neighbours(int a, int b) =>
        a >= 0 && b >= 0 && a < Cells && b < Cells && (Math.Abs(a - b) == Side || (Math.Abs(a - b) == 1 && a / Side == b / Side));

    /// <summary>
    /// Two neighbours swapped: refused when they aren't neighbours or the swap makes no line (nothing spent); otherwise
    /// a swap is spent and every run is cleared, cascade after cascade, before the board is shuffled if it has no swap left.
    /// </summary>
    public SwapResult Swap(int a, int b)
    {
        LastClears.Clear();
        if (Over) return SwapResult.Over;
        if (!Neighbours(a, b)) return SwapResult.NotNeighbours;
        (cells[a], cells[b]) = (cells[b], cells[a]);
        if (Runs().Count == 0)
        {
            (cells[a], cells[b]) = (cells[b], cells[a]);
            return SwapResult.NoLine;
        }
        SwapsLeft--;
        int cascade = 0;
        while (true)
        {
            var runs = Runs();
            if (runs.Count == 0) break;
            cascade++;
            var gone = new bool[Cells];
            // In order: rows from the top, then columns from the left (Runs' own order)
            foreach (var run in runs)
            {
                int piece = cells[run[0]];
                float effect = EffectOf(piece);
                float raw = (Base + PerExtra * (run.Count - 3)) * (1f + CascadeBonus * (cascade - 1)) * effect;
                int damage = (int)MathF.Floor(raw);
                LastClears.Add(new MatchClear(piece, run.Count, cascade, damage, effect));
                Hp = Math.Max(0, Hp - damage);
                Score += damage;
                foreach (int c in run) gone[c] = true;
            }
            Fall(gone);
        }
        if (!Over && !HasSwap()) Shuffle();
        return SwapResult.Made;
    }

    // Each column's survivors fall to its foot, and what is left at its top is drawn afresh, from the left
    private void Fall(bool[] gone)
    {
        for (int x = 0; x < Side; x++)
        {
            var kept = new List<int>();
            for (int y = Side - 1; y >= 0; y--)
                if (!gone[y * Side + x]) kept.Add(cells[y * Side + x]);
            for (int y = Side - 1, k = 0; y >= 0; y--, k++)
                cells[y * Side + x] = k < kept.Count ? kept[k] : random.Int(Pieces);
        }
    }

    /// <summary>The board's runs of three or more: along each row from the top, then down each column from the left.</summary>
    public List<List<int>> Runs()
    {
        var runs = new List<List<int>>();
        for (int y = 0; y < Side; y++)
            for (int x = 0; x < Side;)
            {
                int end = x;
                while (end + 1 < Side && cells[y * Side + end + 1] == cells[y * Side + x]) end++;
                if (end - x + 1 >= 3) runs.Add(Enumerable.Range(x, end - x + 1).Select(c => y * Side + c).ToList());
                x = end + 1;
            }
        for (int x = 0; x < Side; x++)
            for (int y = 0; y < Side;)
            {
                int end = y;
                while (end + 1 < Side && cells[(end + 1) * Side + x] == cells[y * Side + x]) end++;
                if (end - y + 1 >= 3) runs.Add(Enumerable.Range(y, end - y + 1).Select(r => r * Side + x).ToList());
                y = end + 1;
            }
        return runs;
    }

    /// <summary>Whether some swap of neighbours makes a line.</summary>
    public bool HasSwap() => FirstSwap() != null;

    /// <summary>The first swap that makes a line (a cell and its east or south neighbour), for a hint, a test or the harness.</summary>
    public (int A, int B)? FirstSwap()
    {
        for (int c = 0; c < Cells; c++)
            foreach (int d in new[] { c + 1, c + Side })
            {
                if (!Neighbours(c, d)) continue;
                (cells[c], cells[d]) = (cells[d], cells[c]);
                bool makes = Runs().Count > 0;
                (cells[c], cells[d]) = (cells[d], cells[c]);
                if (makes) return (c, d);
            }
        return null;
    }

    // The board's pieces dealt round again until no line stands and a swap is left (a Fisher-Yates on the board's own generator)
    private void Shuffle()
    {
        Shuffles++;
        for (int tries = 0; tries < 100; tries++)
        {
            for (int i = Cells - 1; i > 0; i--)
            {
                int j = random.Int(i + 1);
                (cells[i], cells[j]) = (cells[j], cells[i]);
            }
            if (Runs().Count == 0 && HasSwap()) return;
        }
        Deal();
    }

    /// <summary>The coins a cleared stage pays: its own, and a coin for each swap left over.</summary>
    public int Payout => Cleared ? Stage.Coins + SwapsLeft * Stage.CoinsPerSwapLeft : 0;
}
