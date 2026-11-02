using ContentCheck;
using PokemonPlatinumEngine.Overworld;

namespace RoomEditor;

/// <summary>What the area view draws a tile's ground as: what blocks, or the original's behaviour.</summary>
public enum AreaLayer { Blocked, Behaviour }

/// <summary>
/// An open area of a world seen from above (plan 16 · T9, the area view): a window of its tiles, a chunk at a time, on
/// the map the game makes of its region (<see cref="GameContent"/>, which read the chunks of
/// <c>Data/world/&lt;region&gt;</c>), with each tile's blocked flag and behaviour, the content check's plan of the
/// window (<see cref="Plans.Of"/>: the ground baked on the CPU, everything the game put there tagged), and the rows of
/// the overlay being edited on their tiles, which change as the overlay is edited where the plan, made of the files on
/// disk, does not. A tile picked is where the overlay's own person, way out, trigger, prop or tile read is placed.
/// No window: the editor draws it, the tests drive it.
/// </summary>
public sealed class AreaView
{
    public string Key { get; }
    public Map Map { get; }
    private readonly GameContent content;

    /// <summary>Every tile of the area: the rectangle round its chunks.</summary>
    public (int X, int Y, int Width, int Height) Bounds { get; }

    /// <summary>The tiles shown: a chunk of the area (or less, at its edge).</summary>
    public (int X, int Y, int Width, int Height) Window { get; private set; }

    public AreaLayer Layer { get; set; } = AreaLayer.Blocked;

    private AreaView(GameContent content, string key, Map map, (int, int, int, int) bounds)
    {
        this.content = content;
        Key = key;
        Map = map;
        Bounds = bounds;
        var first = Chunks().First();
        ShowChunk(first.X, first.Y);
    }

    /// <summary>The view of an open area of the folder's worlds, or why there is none.</summary>
    public static AreaView Open(GameContent content, string key)
    {
        foreach (var region in content.Regions)
        {
            if (region.World.Area(key) == null) continue;
            if (!region.World.IsOpen(key) || region.World.MapOf(key) is not { } entry)
                throw new ArgumentException($"{key} isn't open: its world builds no map of it");
            var map = region.Maps?.FirstOrDefault(m => m.Name == entry.Name)
                ?? throw new ArgumentException($"the world of {region.Folder} would not load: run the content check");
            var bounds = map.AreaBounds(key) ?? throw new ArgumentException($"no chunk of {map.Name} is {key}'s");
            return new AreaView(content, key, map, bounds);
        }
        throw new ArgumentException($"no region has the area '{key}'");
    }

    /// <summary>Whether a tile is the area's own (an area's rectangle holds its neighbours' chunks too).</summary>
    public bool Belongs(int x, int y) => Map.InBounds(x, y) && Map.AreaAt(x, y)?.Key is { } key && string.Equals(key, Key, StringComparison.OrdinalIgnoreCase);

    public bool Blocked(int x, int y) => Map.InBounds(x, y) && Map.IsSolid(x, y);
    public TileBehavior Behaviour(int x, int y) => Map.InBounds(x, y) ? Map.BehaviourAt(x, y) : TileBehavior.None;

    /// <summary>The north-west tile of each chunk of the area, row by row.</summary>
    public IEnumerable<(int X, int Y)> Chunks()
    {
        var (x0, y0, w, h) = Bounds;
        for (int y = y0; y < y0 + h; y += Map.ChunkTiles)
            for (int x = x0; x < x0 + w; x += Map.ChunkTiles)
                // (a room's matrix shares a chunk among several areas, so every tile is asked)
                if (Enumerable.Range(0, Map.ChunkTiles * Map.ChunkTiles).Any(i => Belongs(x + i % Map.ChunkTiles, y + i / Map.ChunkTiles)))
                    yield return (x, y);
    }

    /// <summary>Shows the chunk a tile is in, cut to the area's rectangle.</summary>
    public void ShowChunk(int x, int y)
    {
        int cx = x - (x - Bounds.X) % Map.ChunkTiles, cy = y - (y - Bounds.Y) % Map.ChunkTiles;
        int w = Math.Min(Map.ChunkTiles, Bounds.X + Bounds.Width - cx), h = Math.Min(Map.ChunkTiles, Bounds.Y + Bounds.Height - cy);
        if (w <= 0 || h <= 0 || x < Bounds.X || y < Bounds.Y) throw new ArgumentException($"{x},{y} is outside {Key}");
        Window = (cx, cy, w, h);
    }

    /// <summary>Moves to the next chunk of the area (or the one before), round from the last to the first.</summary>
    public void NextChunk(int step = 1)
    {
        var chunks = Chunks().ToList();
        int at = chunks.FindIndex(c => c.X == Window.X && c.Y == Window.Y);
        var next = chunks[((at < 0 ? 0 : at + step) % chunks.Count + chunks.Count) % chunks.Count];
        ShowChunk(next.X, next.Y);
    }

    public bool InWindow(int x, int y) => x >= Window.X && y >= Window.Y && x < Window.X + Window.Width && y < Window.Y + Window.Height;

    /// <summary>The content check's plan of the window, as the files on disk make it.</summary>
    public Plan Plan(int? tile = null) => Plans.Of(content, $"{Key}@{Window.X},{Window.Y},{Window.Width},{Window.Height}", tile)[0];

    /// <summary>What a tile is, a line: its ground's layers and what the overlay has there.</summary>
    public string Describe(int x, int y, OverlayDocument? overlay)
    {
        if (!Map.InBounds(x, y)) return $"{x},{y}: off the map";
        string ground = $"{x},{y}: {Map.GetGroundTile(x, y)}, {Behaviour(x, y)}{(Blocked(x, y) ? ", blocked" : "")}{(Belongs(x, y) ? "" : $", {Map.AreaAt(x, y)?.Key ?? "no area"}'s")}";
        var rows = overlay?.RowsAt(x, y) ?? Array.Empty<OverlayRow>();
        return rows.Count == 0 ? ground : ground + "; " + string.Join("; ", rows.Select(r => r.Area + (r.Overlay.Length > 0 ? ": " + r.Overlay : "")));
    }

    /// <summary>
    /// Places the overlay's own thing of a kind on a tile of the area, as one step of the overlay, and says its row:
    /// a person, a way out, a trigger, a prop or a tile read. A tile that isn't the area's is refused.
    /// </summary>
    public OverlayRow Place(OverlayDocument overlay, OverlayRowKind kind, int x, int y, PropType prop = PropType.Planter)
    {
        if (!Belongs(x, y)) throw new ArgumentException($"{x},{y} isn't {Key}'s");
        string key = kind switch
        {
            OverlayRowKind.OwnPerson => overlay.AddPerson(x, y),
            OverlayRowKind.Exit => overlay.AddExit(x, y),
            OverlayRowKind.OwnTrigger => overlay.AddTrigger(x, y),
            OverlayRowKind.OwnProp => overlay.AddProp(prop, x, y),
            OverlayRowKind.ReadAt => overlay.AddReadAt(x, y),
            _ => throw new ArgumentException($"a {kind} is the area's, not placed by the overlay")
        };
        return overlay.Rows().Single(r => r.Kind == kind && r.Key == key);
    }
}
