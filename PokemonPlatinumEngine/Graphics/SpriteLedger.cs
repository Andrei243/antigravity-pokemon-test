using System;
using System.Collections.Generic;
using System.Linq;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// Which species' menu sprites are held, and which go when there are too many (plan 03 · D13): with every species
/// in the National Pokédex and thirty to a box, a game that kept each sprite it had ever shown came to hold
/// thousands of textures. No GPU calls: <see cref="PokemonSprites"/> asks it what to free. Time is counted in
/// ticks, one a frame; the sprites longest unasked for go first, never one that is always kept (the stand-in, the
/// species made at the start) nor one asked for a moment ago, which a screen may be about to draw again.
/// </summary>
internal sealed class SpriteLedger
{
    private readonly Dictionary<string, long> asked = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> always = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>How many species' sprites are held before the oldest go.</summary>
    public int Capacity { get; }

    /// <summary>For how many ticks after it was last asked for a sprite is never let go.</summary>
    public int Fresh { get; }

    public long Now { get; private set; }

    public int Count => asked.Count;

    public SpriteLedger(int capacity, int fresh)
    {
        Capacity = capacity;
        Fresh = fresh;
    }

    /// <summary>A species' sprites have been made: they are held from now, for good if <paramref name="forGood"/>.</summary>
    public void Keep(string name, bool forGood = false)
    {
        asked[name] = Now;
        if (forGood) always.Add(name);
    }

    /// <summary>A species' sprites were asked for just now.</summary>
    public void Touch(string name)
    {
        if (asked.ContainsKey(name)) asked[name] = Now;
    }

    public bool Holds(string name) => asked.ContainsKey(name);

    public void Forget(string name)
    {
        asked.Remove(name);
        always.Remove(name);
    }

    /// <summary>A frame has passed.</summary>
    public void Tick() => Now++;

    /// <summary>
    /// The species whose sprites are to be let go now, the longest unasked for first, so that
    /// <see cref="Capacity"/> are left: none when no more are held than that, and fewer than the surplus when the
    /// rest are fresh or kept for good.
    /// </summary>
    public List<string> Overflow()
    {
        int surplus = asked.Count - Capacity;
        if (surplus <= 0) return new List<string>();
        return asked.Where(a => !always.Contains(a.Key) && Now - a.Value > Fresh)
            .OrderBy(a => a.Value).ThenBy(a => a.Key, StringComparer.OrdinalIgnoreCase)
            .Take(surplus).Select(a => a.Key).ToList();
    }
}
