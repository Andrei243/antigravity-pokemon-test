using System;
using System.Collections.Generic;
using System.Linq;

namespace PokemonPlatinumEngine.OpenWorld;

/// <summary>
/// A named place in an open map (plan 32, "Volumes"): a name, a position and a yaw. What a script, a door and the
/// harness name instead of a tile. GPU-free.
/// </summary>
public sealed record OpenMarker(string Name, OpenPos Pos, int Yaw = 0)
{
    public int Yaw { get; init; } = OpenYaw.Normalize(Yaw);
}

/// <summary>
/// The open counterpart of the grid's <c>Map</c> (plan 32 · OW1), not a subclass of it: a map of an open region, its
/// outdoors and its continuous caves, described in millimetres. As OW1 builds it, it is the map's name, the region it
/// belongs to and its markers, and nothing of terrain: OW2 gives it its baked chunks, OW5 its areas, volumes and
/// doors, OW6 its people. Chunks are 32 m by 32 m columns of any height (<see cref="ChunkOf"/>). GPU-free.
/// </summary>
public sealed class OpenMap
{
    /// <summary>A chunk's side, in millimetres.</summary>
    public const int ChunkSide = 32 * OpenPos.PerMetre;

    private readonly Dictionary<string, OpenMarker> markers = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The map's name, as the save and a warp name it. No grid map may have the same name.</summary>
    public string Name { get; }

    /// <summary>The id of the region whose map it is (<c>Galar</c>, <c>Paldea</c>).</summary>
    public string Region { get; }

    public OpenMap(string name, string region, IEnumerable<OpenMarker>? markers = null)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("an open map has a name", nameof(name));
        Name = name;
        Region = region ?? "";
        foreach (var marker in markers ?? Enumerable.Empty<OpenMarker>()) Add(marker);
    }

    /// <summary>The map's markers, in the order they were added.</summary>
    public IReadOnlyList<OpenMarker> Markers => order;
    private readonly List<OpenMarker> order = new();

    /// <summary>Adds a marker; two of one name are refused, since a script names it.</summary>
    public void Add(OpenMarker marker)
    {
        if (!markers.TryAdd(marker.Name, marker))
            throw new ArgumentException($"open map {Name} has two markers named {marker.Name}");
        order.Add(marker);
    }

    /// <summary>The marker of that name (case aside), or null.</summary>
    public OpenMarker? Marker(string name) => markers.GetValueOrDefault(name);

    /// <summary>The chunk a place stands in: its column east and its row south, counted from nought at the origin, negative beyond it.</summary>
    public static (int X, int Z) ChunkOf(OpenPos pos) => (FloorDiv(pos.X, ChunkSide), FloorDiv(pos.Z, ChunkSide));

    private static int FloorDiv(int value, int by)
    {
        int q = value / by;
        return value % by != 0 && value < 0 ? q - 1 : q;
    }

    public override string ToString() => $"{Name} ({Region}, open)";
}
