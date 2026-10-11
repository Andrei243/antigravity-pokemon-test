using System;
using System.Collections;
using System.Collections.Generic;

namespace PokemonPlatinumEngine.Overworld;

/// <summary>
/// A list that counts its own changes (plan 01 · M12): whatever keeps an index of what is in it compares
/// <see cref="Version"/> with the number it was made at, and knows in one look whether the index still holds. A
/// map's warps are one: they change as the map is built and when the story reveals a place, and are asked about by
/// tile on every step.
/// </summary>
public sealed class TrackedList<T> : IList<T>, IReadOnlyList<T>
{
    private readonly List<T> items = new();

    /// <summary>How often the list has changed.</summary>
    public int Version { get; private set; }

    public int Count => items.Count;
    public bool IsReadOnly => false;

    public T this[int index]
    {
        get => items[index];
        set
        {
            items[index] = value;
            Version++;
        }
    }

    public void Add(T item)
    {
        items.Add(item);
        Version++;
    }

    public void AddRange(IEnumerable<T> more)
    {
        items.AddRange(more);
        Version++;
    }

    public void Insert(int index, T item)
    {
        items.Insert(index, item);
        Version++;
    }

    public bool Remove(T item)
    {
        if (!items.Remove(item)) return false;
        Version++;
        return true;
    }

    public void RemoveAt(int index)
    {
        items.RemoveAt(index);
        Version++;
    }

    public int RemoveAll(Predicate<T> match)
    {
        int removed = items.RemoveAll(match);
        if (removed > 0) Version++;
        return removed;
    }

    public void Clear()
    {
        if (items.Count == 0) return;
        items.Clear();
        Version++;
    }

    public bool Contains(T item) => items.Contains(item);
    public int IndexOf(T item) => items.IndexOf(item);
    public void CopyTo(T[] array, int arrayIndex) => items.CopyTo(array, arrayIndex);
    public bool Exists(Predicate<T> match) => items.Exists(match);
    public T? Find(Predicate<T> match) => items.Find(match);
    public int FindIndex(Predicate<T> match) => items.FindIndex(match);

    public List<T>.Enumerator GetEnumerator() => items.GetEnumerator();
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => items.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => items.GetEnumerator();
}
