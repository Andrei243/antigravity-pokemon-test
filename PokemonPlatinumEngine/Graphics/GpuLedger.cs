using System;
using System.Collections.Generic;
using System.Linq;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>What kind of thing the graphics card holds (plan 16 · T21).</summary>
public enum GpuKind
{
    Texture,
    Target,
    Framebuffer,
    Mesh,
    Buffer,
    Shader,
    Font,
}

/// <summary>One owner's count of one kind, and its bytes.</summary>
public readonly record struct GpuCount(GpuKind Kind, string Owner, int Count, long Bytes);

/// <summary>The ledger at one moment: a row to each kind and owner that holds anything.</summary>
public sealed class GpuSnapshot
{
    public IReadOnlyList<GpuCount> Rows { get; }

    public GpuSnapshot(IReadOnlyList<GpuCount> rows) { Rows = rows; }

    public int Count(GpuKind kind, string owner) =>
        Rows.Where(r => r.Kind == kind && r.Owner == owner).Sum(r => r.Count);

    public int CountOf(string owner) => Rows.Where(r => r.Owner == owner).Sum(r => r.Count);

    public int Total => Rows.Sum(r => r.Count);

    public long Bytes => Rows.Sum(r => r.Bytes);

    /// <summary>The owners in the snapshot, in order.</summary>
    public IEnumerable<string> Owners => Rows.Select(r => r.Owner).Distinct().OrderBy(o => o, StringComparer.Ordinal);
}

/// <summary>
/// Everything the game holds on the graphics card, by kind and owner, with its bytes (plan 16 · T21). No GPU calls:
/// <see cref="Gpu"/> tells it of every load and unload, keyed by the kind and the id the driver gave. An unload of
/// something it was never told of (a texture raylib made itself) changes nothing, so no count goes below nought.
/// Locked, since a load may come from any thread that owns the window's context.
/// </summary>
public sealed class GpuLedger
{
    private readonly Dictionary<(GpuKind, uint), (string Owner, long Bytes)> live = new();
    private readonly object gate = new();

    /// <summary>The game's own.</summary>
    public static GpuLedger Game { get; } = new();

    public void Add(GpuKind kind, uint id, string owner, long bytes)
    {
        if (id == 0) return;
        lock (gate) live[(kind, id)] = (owner, Math.Max(0, bytes));
    }

    /// <summary>Takes something off the ledger; true when it was there.</summary>
    public bool Remove(GpuKind kind, uint id)
    {
        lock (gate) return live.Remove((kind, id));
    }

    public bool Holds(GpuKind kind, uint id)
    {
        lock (gate) return live.ContainsKey((kind, id));
    }

    public GpuSnapshot Snapshot()
    {
        lock (gate)
        {
            var rows = live
                .GroupBy(e => (e.Key.Item1, e.Value.Owner))
                .Select(g => new GpuCount(g.Key.Item1, g.Key.Owner, g.Count(), g.Sum(e => e.Value.Bytes)))
                .OrderBy(r => r.Owner, StringComparer.Ordinal).ThenBy(r => r.Kind)
                .ToList();
            return new GpuSnapshot(rows);
        }
    }

    /// <summary>The bytes of a picture of this size, at four bytes a texel unless said otherwise.</summary>
    public static long PictureBytes(int width, int height, int bytesPerTexel = 4) =>
        (long)Math.Max(0, width) * Math.Max(0, height) * bytesPerTexel;
}
