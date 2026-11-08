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
/// belongs to and its markers; OW2 gives it its baked land (<see cref="Land"/>), OW5 its areas, volumes and
/// doors, OW6 its people. Chunks are 32 m by 32 m columns of any height (<see cref="ChunkOf"/>). GPU-free.
/// </summary>
public sealed partial class OpenMap
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

    /// <summary>
    /// The map's land as <c>tools/OpenWorldBake</c> baked it (plan 32 · OW2), or null for a map with none (OW1's empty
    /// test maps). OW3's collision takes level 0's ground (<see cref="Ground"/>), OW8's renderer every level.
    /// </summary>
    public BakedRegion? Land
    {
        get => land;
        set { land = value; ground = null; }
    }
    private BakedRegion? land;

    private readonly List<OpenBox> solids = new();
    private readonly List<OpenWaterBox> waters = new();
    private OpenGround? ground;

    /// <summary>The map's solid boxes (plan 32 · OW3: the kit's buildings and props as OW5 lays them), in the order added.</summary>
    public IReadOnlyList<OpenBox> Solids => solids;

    /// <summary>The map's water laid by hand beside its land's (plan 32 · OW3), in the order added.</summary>
    public IReadOnlyList<OpenWaterBox> Waters => waters;

    /// <summary>Adds a solid box (the ground is made again the next time it is asked for).</summary>
    public void Add(OpenBox box)
    {
        solids.Add(box.Checked());
        ground = null;
    }

    /// <summary>Adds water laid by hand.</summary>
    public void Add(OpenWaterBox water)
    {
        waters.Add(water);
        ground = null;
    }

    /// <summary>
    /// What the capsule collides with (plan 32 · OW3): the land's level 0 and the solid boxes, made the first time it
    /// is asked for and again after either changes.
    /// </summary>
    public OpenGround Ground => ground ??= OpenGround.Of(this);

    /// <summary>The map's markers, in the order they were added.</summary>
    public IReadOnlyList<OpenMarker> Markers => order;
    private readonly List<OpenMarker> order = new();

    /// <summary>Adds a marker; two of one name are refused, since a script names it.</summary>
    public void Add(OpenMarker marker)
    {
        if (personByKey.ContainsKey(marker.Name))
            throw new ArgumentException($"open map {Name} has a marker and someone both named {marker.Name}: a script's faceto couldn't tell them apart");
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
