using System;
using System.Collections;
using System.Collections.Generic;

namespace PokemonPlatinumEngine.Overworld;

/// <summary>
/// The people and things on a map, as a list that also knows where each one stands (plan 01 · M12). It is a list
/// like any other, in the order they were put on the map; beside it it keeps them by the square of
/// <see cref="Cell"/> tiles they stand in, so that asking who is on a tile, or who is near a place, looks at a
/// handful of people and never at everyone the map of Sinnoh holds. The squares are kept up as the list changes and
/// as people move: a person tells the lists they are in when their tile changes (<see cref="NPC.GridX"/>,
/// <see cref="NPC.GridY"/>). Whoever is asked for by tile is the first on it in the list's own order, as a search of
/// the whole list would find. No drawing or input.
/// </summary>
public sealed class PeopleList : IList<NPC>, IReadOnlyList<NPC>
{
    /// <summary>The side of a square of the index, in tiles.</summary>
    public const int Cell = 8;

    private const int Shift = 3;

    private readonly List<NPC> items = new();

    // Who stands in each square, in the list's order: each with the number it was given as it was put on the list
    private readonly Dictionary<long, List<(long Order, NPC Who)>> squares = new();
    private readonly Dictionary<NPC, long> orders = new();
    private long next;

    private static long KeyOf(int x, int y) => ((long)(y >> Shift) << 32) | (uint)(x >> Shift);

    // ------------------------------------------------------------------ the list

    public int Count => items.Count;
    public bool IsReadOnly => false;

    public NPC this[int index]
    {
        get => items[index];
        set
        {
            Unplace(items[index]);
            items[index] = value;
            Renumber();
        }
    }

    public void Add(NPC npc)
    {
        items.Add(npc);
        // The same person twice on one list keeps the place of the first
        if (orders.ContainsKey(npc)) return;
        long order = next++;
        orders[npc] = order;
        SquareOf(npc.GridX, npc.GridY).Add((order, npc));
        npc.Watch(this);
    }

    public void AddRange(IEnumerable<NPC> people)
    {
        foreach (var npc in people) Add(npc);
    }

    public void Insert(int index, NPC npc)
    {
        if (index == items.Count)
        {
            Add(npc);
            return;
        }
        items.Insert(index, npc);
        Renumber();
    }

    public bool Remove(NPC npc)
    {
        if (!items.Remove(npc)) return false;
        if (!items.Contains(npc)) Unplace(npc);
        return true;
    }

    public void RemoveAt(int index) => Remove(items[index]);

    public int RemoveAll(Predicate<NPC> match)
    {
        int removed = items.RemoveAll(match);
        if (removed > 0) Renumber();
        return removed;
    }

    public void Clear()
    {
        foreach (var npc in orders.Keys) npc.Unwatch(this);
        items.Clear();
        squares.Clear();
        orders.Clear();
        next = 0;
    }

    public bool Contains(NPC npc) => orders.ContainsKey(npc);
    public int IndexOf(NPC npc) => items.IndexOf(npc);
    public void CopyTo(NPC[] array, int arrayIndex) => items.CopyTo(array, arrayIndex);
    public bool Exists(Predicate<NPC> match) => items.Exists(match);
    public NPC? Find(Predicate<NPC> match) => items.Find(match);

    public List<NPC>.Enumerator GetEnumerator() => items.GetEnumerator();
    IEnumerator<NPC> IEnumerable<NPC>.GetEnumerator() => items.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => items.GetEnumerator();

    // ------------------------------------------------------------------ by place

    /// <summary>The first on a tile, in the list's order; null when nobody is.</summary>
    public NPC? At(int x, int y)
    {
        if (!squares.TryGetValue(KeyOf(x, y), out var square)) return null;
        foreach (var (_, npc) in square)
            if (npc.GridX == x && npc.GridY == y) return npc;
        return null;
    }

    /// <summary>The first on a tile for whom something holds, in the list's order.</summary>
    public NPC? At<T>(int x, int y, T with, Func<NPC, T, bool> holds)
    {
        if (!squares.TryGetValue(KeyOf(x, y), out var square)) return null;
        foreach (var (_, npc) in square)
            if (npc.GridX == x && npc.GridY == y && holds(npc, with)) return npc;
        return null;
    }

    /// <summary>
    /// Everyone standing on the tiles from (<paramref name="x0"/>, <paramref name="y0"/>) to (<paramref name="x1"/>,
    /// <paramref name="y1"/>), both counted, in the list's order, put into <paramref name="into"/> (emptied first; a
    /// list of the caller's own, so a frame makes none). It is theirs to keep: people may move or leave while it is
    /// gone through.
    /// </summary>
    public List<NPC> In(int x0, int y0, int x1, int y1, List<NPC>? into = null)
    {
        into ??= new List<NPC>();
        into.Clear();
        if (x1 < x0 || y1 < y0) return into;
        long squaresAsked = ((long)(x1 >> Shift) - (x0 >> Shift) + 1) * ((long)(y1 >> Shift) - (y0 >> Shift) + 1);
        // A place so large that it holds more squares than the map has people: everyone is looked at instead
        if (squaresAsked > items.Count + 16)
        {
            foreach (var npc in items)
                if (npc.GridX >= x0 && npc.GridX <= x1 && npc.GridY >= y0 && npc.GridY <= y1) into.Add(npc);
            return into;
        }
        gathered ??= new List<(long, NPC)>();
        gathered.Clear();
        for (int sy = y0 >> Shift; sy <= y1 >> Shift; sy++)
            for (int sx = x0 >> Shift; sx <= x1 >> Shift; sx++)
            {
                if (!squares.TryGetValue(((long)sy << 32) | (uint)sx, out var square)) continue;
                foreach (var entry in square)
                    if (entry.Who.GridX >= x0 && entry.Who.GridX <= x1 && entry.Who.GridY >= y0 && entry.Who.GridY <= y1) gathered.Add(entry);
            }
        gathered.Sort(static (a, b) => a.Order.CompareTo(b.Order));
        foreach (var (_, npc) in gathered) into.Add(npc);
        return into;
    }

    [ThreadStatic] private static List<(long Order, NPC Who)>? gathered;

    /// <summary>Everyone within <paramref name="reach"/> tiles of a tile either way, in the list's order.</summary>
    public List<NPC> Near(int x, int y, int reach, List<NPC>? into = null) => In(x - reach, y - reach, x + reach, y + reach, into);

    // ------------------------------------------------------------------ keeping the squares

    private List<(long Order, NPC Who)> SquareOf(int x, int y)
    {
        long key = KeyOf(x, y);
        if (!squares.TryGetValue(key, out var square)) squares[key] = square = new List<(long, NPC)>(2);
        return square;
    }

    private void Unplace(NPC npc)
    {
        if (!orders.Remove(npc, out long order)) return;
        long key = KeyOf(npc.GridX, npc.GridY);
        if (squares.TryGetValue(key, out var square))
        {
            square.Remove((order, npc));
            if (square.Count == 0) squares.Remove(key);
        }
        npc.Unwatch(this);
    }

    /// <summary>The list's order changed otherwise than at its end: everyone is numbered and placed afresh.</summary>
    private void Renumber()
    {
        foreach (var npc in orders.Keys) npc.Unwatch(this);
        squares.Clear();
        orders.Clear();
        next = 0;
        foreach (var npc in items)
        {
            if (orders.ContainsKey(npc)) continue;
            long order = next++;
            orders[npc] = order;
            SquareOf(npc.GridX, npc.GridY).Add((order, npc));
            npc.Watch(this);
        }
    }

    /// <summary>Someone of the list has moved from one tile to another (told by the person as their tile changes).</summary>
    internal void Moved(NPC npc, int fromX, int fromY)
    {
        long from = KeyOf(fromX, fromY), to = KeyOf(npc.GridX, npc.GridY);
        if (from == to || !orders.TryGetValue(npc, out long order)) return;
        if (squares.TryGetValue(from, out var left))
        {
            left.Remove((order, npc));
            if (left.Count == 0) squares.Remove(from);
        }
        // Into the square they came to, in their place in the list's order
        var square = SquareOf(npc.GridX, npc.GridY);
        int at = square.Count;
        while (at > 0 && square[at - 1].Order > order) at--;
        square.Insert(at, (order, npc));
    }
}
