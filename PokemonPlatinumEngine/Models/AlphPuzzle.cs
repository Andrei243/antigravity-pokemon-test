using System;
using System.Collections.Generic;
using System.Linq;

namespace PokemonPlatinumEngine.Models;

/// <summary>
/// A stone panel of the Ruins of Alph's chambers (plan 20 · J5; <c>docs/mechanics/rulings.md</c>, "The Ruins of Alph's
/// panels"), as HeartGold's puzzle has it: a board of six squares by six whose middle four by four is the picture's
/// frame, and sixteen pieces of a picture that begin on the ring round it, each turned some quarter turns. A piece is
/// picked up, carried, turned and put down on any square but the four corners (on another piece, the two change
/// places); the panel is solved once every piece lies in its own place in the frame, upright. GPU-free and with no
/// input: <see cref="Move"/>, <see cref="Confirm"/>, <see cref="Cancel"/> and <see cref="Turn"/> are the screen's buttons,
/// so a test solves it. How it begins is the panel's own, the same every time it is come to (a draw of its own,
/// seeded by its name, never <c>Dice</c>), as the original's are fixed.
/// </summary>
public sealed class AlphPuzzle
{
    /// <summary>The board's side, and the frame's (its middle four by four).</summary>
    public const int Side = 6, Frame = 4, Pieces = Frame * Frame;

    /// <summary>The four panels by their names, with the Pokémon each shows (HeartGold's four chambers).</summary>
    public static readonly IReadOnlyDictionary<string, string> Pictures = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["kabuto"] = "Kabuto", ["omanyte"] = "Omanyte", ["aerodactyl"] = "Aerodactyl", ["hooh"] = "Ho-Oh"
    };

    private readonly int[] cells = new int[Side * Side];
    private readonly int[] turns = new int[Pieces];
    private int heldFrom = -1;

    public string Name { get; }

    /// <summary>The Pokémon the picture shows.</summary>
    public string Species => Pictures[Name];

    /// <summary>The square under the cursor, 0 to 5 each way.</summary>
    public int CursorX { get; private set; }
    public int CursorY { get; private set; }

    /// <summary>The piece in hand, or null.</summary>
    public int? Held { get; private set; }

    /// <summary>Pieces put down, picked up and turned so far.</summary>
    public int Moves { get; private set; }

    public AlphPuzzle(string name)
    {
        if (!Pictures.ContainsKey(name)) throw new ArgumentException($"no panel called {name}", nameof(name));
        Name = name;
        Array.Fill(cells, -1);
        // The ring's sixteen squares (all but the corners), and a draw of the panel's own over them
        var ring = Ring().ToList();
        uint state = Seed(name);
        uint Next() { state ^= state << 13; state ^= state >> 17; state ^= state << 5; return state; }
        var order = Enumerable.Range(0, Pieces).ToList();
        for (int i = order.Count - 1; i > 0; i--)
        {
            int j = (int)(Next() % (uint)(i + 1));
            (order[i], order[j]) = (order[j], order[i]);
        }
        for (int i = 0; i < Pieces; i++)
        {
            cells[ring[i]] = order[i];
            turns[order[i]] = (int)(Next() % 4);
        }
        CursorX = 1;
        CursorY = 1;
    }

    /// <summary>The squares of the ring round the frame, but the corners: where the pieces begin.</summary>
    public static IEnumerable<int> Ring()
    {
        for (int y = 0; y < Side; y++)
            for (int x = 0; x < Side; x++)
                if (OnRing(x, y) && !IsCorner(x, y)) yield return y * Side + x;
    }

    public static bool IsCorner(int x, int y) => (x == 0 || x == Side - 1) && (y == 0 || y == Side - 1);
    public static bool InFrame(int x, int y) => x >= 1 && x <= Frame && y >= 1 && y <= Frame;
    private static bool OnRing(int x, int y) => x == 0 || y == 0 || x == Side - 1 || y == Side - 1;

    /// <summary>The piece on a square (its place in the picture, 0 to 15 by rows), or -1.</summary>
    public int PieceAt(int x, int y) => x < 0 || y < 0 || x >= Side || y >= Side ? -1 : cells[y * Side + x];

    /// <summary>How many quarter turns clockwise a piece lies from upright.</summary>
    public int TurnsOf(int piece) => turns[piece];

    /// <summary>Every piece in its place in the frame and upright, and nothing in hand.</summary>
    public bool Solved
    {
        get
        {
            if (Held != null) return false;
            for (int y = 1; y <= Frame; y++)
                for (int x = 1; x <= Frame; x++)
                {
                    int piece = PieceAt(x, y);
                    if (piece != (y - 1) * Frame + (x - 1) || turns[piece] != 0) return false;
                }
            return true;
        }
    }

    /// <summary>The cursor a square along, wrapping round the board.</summary>
    public void Move(int dx, int dy)
    {
        CursorX = (CursorX + dx + Side) % Side;
        CursorY = (CursorY + dy + Side) % Side;
    }

    /// <summary>The cursor put on a square (a test's hand).</summary>
    public void Point(int x, int y)
    {
        CursorX = Math.Clamp(x, 0, Side - 1);
        CursorY = Math.Clamp(y, 0, Side - 1);
    }

    /// <summary>
    /// The A button: a piece under the cursor is picked up; with one in hand it is put down on the square, unless it
    /// is a corner, and a piece already there is picked up in its place. True when something moved.
    /// </summary>
    public bool Confirm()
    {
        if (Solved) return false;
        int cell = CursorY * Side + CursorX;
        if (Held is not { } held)
        {
            if (cells[cell] < 0) return false;
            Held = cells[cell];
            heldFrom = cell;
            cells[cell] = -1;
            Moves++;
            return true;
        }
        if (IsCorner(CursorX, CursorY)) return false;
        int there = cells[cell];
        cells[cell] = held;
        Held = there >= 0 ? there : null;
        heldFrom = there >= 0 ? cell : -1;
        Moves++;
        return true;
    }

    /// <summary>The B button: a piece in hand goes back where it was picked up. False when there was none (the panel is left).</summary>
    public bool Cancel()
    {
        if (Held is not { } held) return false;
        // Where it was picked up, or (after a swap, when that square is the other piece's now) the ring's first empty
        // square: one is always there, with sixteen pieces on thirty-two squares
        int back = heldFrom >= 0 && cells[heldFrom] < 0 ? heldFrom
            : Ring().Concat(Enumerable.Range(0, Side * Side)).First(c => cells[c] < 0 && !IsCorner(c % Side, c / Side));
        cells[back] = held;
        Held = null;
        heldFrom = -1;
        return true;
    }

    /// <summary>The shoulders: the piece in hand, or the one under the cursor, turned a quarter (clockwise for +1).</summary>
    public bool Turn(int way)
    {
        int piece = Held ?? PieceAt(CursorX, CursorY);
        if (piece < 0 || Solved) return false;
        turns[piece] = ((turns[piece] + Math.Sign(way)) % 4 + 4) % 4;
        Moves++;
        return true;
    }

    // FNV-1a of the panel's name: a seed that is the same on every machine (a string's own hash is not)
    private static uint Seed(string name)
    {
        uint h = 2166136261;
        foreach (char c in name) { h ^= c; h *= 16777619; }
        return h == 0 ? 1u : h;
    }
}
