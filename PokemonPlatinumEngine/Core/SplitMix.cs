using System;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// A small deterministic random generator (SplitMix32): one seed, one sequence, on every machine and every version of
/// .NET (never <c>System.Random</c>'s, whose numbers may change between versions, nor a string's <c>GetHashCode</c>).
/// Plan 13 · V6: moved here from <c>Graphics/PokemonGenome.cs</c> so the randomiser and the sculptors share it; its numbers
/// are unchanged, so no model's cache key or sprite signature moves. <see cref="Hash"/>, <see cref="Fnv"/> and
/// <see cref="Mix"/> are the stateless forms, for whoever decides by place: the same seed and place always give the same
/// answer, however many have been asked before.
/// </summary>
public class SplitMix
{
    private uint state;

    public SplitMix(uint seed) => state = seed;

    public uint Next()
    {
        uint z = state += 0x9E3779B9u;
        z = (z ^ (z >> 16)) * 0x85EBCA6Bu;
        z = (z ^ (z >> 13)) * 0xC2B2AE35u;
        return z ^ (z >> 16);
    }

    /// <summary>0 (inclusive) to 1 (exclusive).</summary>
    public float Float() => (Next() >> 8) * (1f / 16777216f);

    public float Range(float a, float b) => a + (b - a) * Float();

    public int Int(int n) => n <= 1 ? 0 : (int)(Next() % (uint)n);

    public bool Chance(float p) => Float() < p;

    public T Pick<T>(params T[] items) => items[Int(items.Length)];

    public static uint Hash(int value, uint salt)
    {
        uint h = (uint)value * 0x27D4EB2Du ^ salt * 0x165667B1u;
        h ^= h >> 15;
        h *= 0x2C1B3C6Du;
        h ^= h >> 12;
        return h;
    }

    /// <summary>FNV-1a of a text's UTF-16 characters: a string's number that is the same on every run (unlike <c>GetHashCode</c>).</summary>
    public static uint Fnv(string text)
    {
        uint h = 2166136261u;
        foreach (char c in text)
        {
            h = (h ^ (c & 0xFFu)) * 16777619u;
            h = (h ^ (uint)(c >> 8)) * 16777619u;
        }
        return h;
    }

    /// <summary>A number decided by three: the stateless SplitMix step over a seed and two salts.</summary>
    public static uint Mix(uint seed, uint a, uint b)
    {
        var first = new SplitMix(seed ^ a * 0x9E3779B1u);
        first.Next();
        return new SplitMix(first.Next() ^ b * 0x85EBCA77u).Next();
    }
}
