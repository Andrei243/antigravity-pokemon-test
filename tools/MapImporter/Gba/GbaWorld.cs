using System.Globalization;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;

namespace MapImporter.Gba;

/// <summary>A map laid on a matrix: its top-left tile there.</summary>
public sealed record GbaPlaced(GbaMap Map, GbaLayout Layout, int X, int Y);

/// <summary>
/// A grid of tiles the GBA's maps are stitched into (plan 18 · W6): every map one map's <c>connections</c> reach,
/// each at its offset, padded to whole chunks of 32. Per tile: the behaviour it becomes, whether it is blocked, the
/// GBA's elevation, its look and which map it belongs to.
/// </summary>
public sealed class GbaMatrix
{
    public const int T = WorldChunkFile.Tiles;

    public int Id { get; init; }
    public List<GbaPlaced> Maps { get; } = new();
    public int Width { get; private set; }
    public int Height { get; private set; }
    public int ChunksWide => Width / T;
    public int ChunksHigh => Height / T;

    public byte[] Behaviour = Array.Empty<byte>();
    public bool[] Solid = Array.Empty<bool>();
    public byte[] Elevation = Array.Empty<byte>();
    public TerrainCover[] Cover = Array.Empty<TerrainCover>();
    /// <summary>The index into <see cref="Maps"/> of the map a tile belongs to; -1 where no map lies.</summary>
    public int[] Area = Array.Empty<int>();

    /// <summary>The ground's height on each tile, and where it slopes; <see cref="Deck"/> where a bridge stands over it.</summary>
    public float[] Ground = Array.Empty<float>();
    public float[] SlopeX = Array.Empty<float>();
    public float[] SlopeZ = Array.Empty<float>();
    public float[] Deck = Array.Empty<float>();

    internal void Allocate(int width, int height)
    {
        Width = (width + T - 1) / T * T;
        Height = (height + T - 1) / T * T;
        int n = Width * Height;
        Behaviour = new byte[n];
        Solid = Enumerable.Repeat(true, n).ToArray();
        Elevation = new byte[n];
        Cover = new TerrainCover[n];
        Area = Enumerable.Repeat(-1, n).ToArray();
        Ground = new float[n];
        SlopeX = new float[n];
        SlopeZ = new float[n];
        Deck = Enumerable.Repeat(float.NaN, n).ToArray();
    }

    public bool Has(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height && Area[y * Width + x] >= 0;
}

/// <summary>
/// The GBA's maps made into matrices and chunks of the world files (plan 18 · W6). The region's first town is the
/// origin of matrix 0, its overworld, and every map a <c>connection</c> reaches (up, down, left, right) is placed at
/// its offset; a set no connection reaches (the Sevii Islands, a cave, a room) is a matrix of its own. Dive and emerge
/// joins are warps, W7's to write. Two connections that disagree, or two maps that overlap, are reported, and the
/// first placed wins.
/// </summary>
public sealed class GbaWorld
{
    /// <summary>Sinnoh's usual ground (<c>WorldMapBuilder.GroundLevel</c>) is the GBA's elevation 3.</summary>
    public const float GroundLevel = WorldMapBuilder.GroundLevel;

    /// <summary>A level of the GBA's elevation, in tiles: a step over a level's edge (1.4) is over <c>FieldMovement.StepLimit</c>.</summary>
    public const float LevelHeight = 1.4f;

    public GbaMaps Source { get; }
    public List<GbaMatrix> Matrices { get; } = new();
    public List<string> Problems { get; } = new();

    /// <summary>Each behaviour's name and how many tiles of the stitched world have it; the unlabelled metatiles by tileset.</summary>
    public SortedDictionary<string, int> BehaviourUse { get; } = new(StringComparer.Ordinal);
    public SortedDictionary<string, SortedDictionary<int, int>> UnknownCover { get; } = new(StringComparer.Ordinal);

    public GbaWorld(GbaMaps source)
    {
        Source = source;
        Problems.AddRange(source.Problems);
        var placed = new HashSet<string>(StringComparer.Ordinal);
        var order = source.Maps.Where(m => source.Layouts.ContainsKey(m.Layout)).ToList();
        if (source.ById.TryGetValue(source.Game.StartMap, out var start) && order.Remove(start)) order.Insert(0, start);
        foreach (var map in order)
        {
            if (placed.Contains(map.Id)) continue;
            Matrices.Add(Stitch(map, Matrices.Count, placed));
        }
        foreach (var matrix in Matrices)
        {
            Fill(matrix);
            Heights(matrix);
        }
    }

    public static float LevelOf(int elevation) => GroundLevel + (elevation - 3) * LevelHeight;

    // ---------------------------------------------------------------- stitching

    private GbaMatrix Stitch(GbaMap first, int id, HashSet<string> placed)
    {
        var at = new Dictionary<string, (int X, int Y)>(StringComparer.Ordinal) { [first.Id] = (0, 0) };
        var list = new List<GbaMap> { first };
        var queue = new Queue<GbaMap>();
        queue.Enqueue(first);
        placed.Add(first.Id);
        while (queue.Count > 0)
        {
            var map = queue.Dequeue();
            var layout = Source.LayoutOf(map);
            var (mx, my) = at[map.Id];
            // FireRed's routes join a copy of Saffron City (SaffronCity_Connection, 48 by 40) that stands for the city
            // as seen from them; the city itself (66 by 55) is a matrix of its own, its connections reported: plan 19's
            foreach (var c in map.Connections)
            {
                if (!Source.ById.TryGetValue(c.Map, out var other) || !Source.Layouts.TryGetValue(other.Layout, out var theirs))
                {
                    if (c.Direction is "up" or "down" or "left" or "right") Problems.Add($"{map.Name}: a connection {c.Direction} to {c.Map}, which has no map or layout");
                    continue;
                }
                (int X, int Y)? there = c.Direction switch
                {
                    "up" => (mx + c.Offset, my - theirs.Height),
                    "down" => (mx + c.Offset, my + layout.Height),
                    "left" => (mx - theirs.Width, my + c.Offset),
                    "right" => (mx + layout.Width, my + c.Offset),
                    _ => null   // dive and emerge: warps between matrices
                };
                if (there is not { } spot) continue;
                if (at.TryGetValue(other.Id, out var known))
                {
                    if (known != spot)
                        Problems.Add($"{map.Name} puts {other.Name} {c.Direction} at {spot.X},{spot.Y}, but it was placed at {known.X},{known.Y}: the first placed wins");
                    continue;
                }
                if (placed.Contains(other.Id))
                {
                    Problems.Add($"{map.Name} joins {other.Name}, which is already on another matrix (its connection back is missing)");
                    continue;
                }
                at[other.Id] = spot;
                placed.Add(other.Id);
                list.Add(other);
                queue.Enqueue(other);
            }
        }

        int minX = at.Values.Min(p => p.X), minY = at.Values.Min(p => p.Y);
        var matrix = new GbaMatrix { Id = id };
        foreach (var map in list)
            matrix.Maps.Add(new GbaPlaced(map, Source.LayoutOf(map), at[map.Id].X - minX, at[map.Id].Y - minY));
        matrix.Allocate(matrix.Maps.Max(p => p.X + p.Layout.Width), matrix.Maps.Max(p => p.Y + p.Layout.Height));
        return matrix;
    }

    private void Fill(GbaMatrix m)
    {
        var overlaps = new HashSet<(int, int)>();
        for (int index = 0; index < m.Maps.Count; index++)
        {
            var (map, layout, ox, oy) = m.Maps[index];
            for (int y = 0; y < layout.Height; y++)
                for (int x = 0; x < layout.Width; x++)
                {
                    int i = (oy + y) * m.Width + ox + x;
                    if (m.Area[i] >= 0)
                    {
                        if (overlaps.Add((m.Area[i], index)))
                            Problems.Add($"{map.Name} overlaps {m.Maps[m.Area[i]].Map.Name} on matrix {m.Id}: the first placed wins");
                        continue;
                    }
                    ushort block = layout.Block(x, y);
                    int metatile = GbaLayout.MetatileOf(block);
                    int raw = Source.BehaviourOf(layout, metatile);
                    string name = Source.BehaviourNames.GetValueOrDefault(raw) ?? $"0x{raw:X2}";
                    var behaviour = GbaBehaviours.Of(name);
                    var (tileset, _) = Source.TilesetOf(layout, metatile);
                    string? label = Source.LabelOf(layout, metatile);
                    var cover = GbaCover.Of(behaviour, label, tileset, metatile);

                    m.Area[i] = index;
                    m.Behaviour[i] = (byte)behaviour;
                    m.Solid[i] = GbaLayout.CollisionOf(block) != 0;
                    m.Elevation[i] = (byte)GbaLayout.ElevationOf(block);
                    m.Cover[i] = cover;
                    BehaviourUse[name] = BehaviourUse.GetValueOrDefault(name) + 1;
                    if (cover == TerrainCover.Unknown)
                    {
                        if (!UnknownCover.TryGetValue(tileset, out var counts)) UnknownCover[tileset] = counts = new();
                        counts[metatile] = counts.GetValueOrDefault(metatile) + 1;
                    }
                }
        }
    }

    // ---------------------------------------------------------------- heights

    /// <summary>
    /// The GBA's elevations as heights (plan 18 · W6): a level is 1.4 tiles above elevation 3, so a straight step over
    /// a level's edge is a cliff; elevation 0 ("any level": stairs, a ledge's foot, a cave's mouth) slopes from the
    /// level on one side to the level on the other, half of 1.4 from either, which walks and draws as a slope; the
    /// water's elevation 1 takes the level of the nearest land, so a shore is level with its water; elevation 15 (a
    /// bridge) is a deck at the level of its ends over the ground of the level below.
    /// </summary>
    private static void Heights(GbaMatrix m)
    {
        int w = m.Width, n = w * m.Height;
        var level = new float[n];
        var known = new bool[n];
        var queue = new Queue<int>();
        for (int i = 0; i < n; i++)
        {
            int e = m.Elevation[i];
            if (m.Area[i] < 0 || e is 0 or 1 or 15) continue;
            level[i] = LevelOf(e);
            known[i] = true;
            queue.Enqueue(i);
        }
        // Water, the "any level" tiles and bridges take the nearest land's level until they are worked out
        var nearest = (float[])level.Clone();
        var reached = (bool[])known.Clone();
        while (queue.Count > 0)
        {
            int i = queue.Dequeue();
            int x = i % w, y = i / w;
            foreach (var (dx, dy) in Sides)
            {
                int nx = x + dx, ny = y + dy;
                if (!m.Has(nx, ny)) continue;
                int j = ny * w + nx;
                if (reached[j]) continue;
                reached[j] = true;
                nearest[j] = nearest[i];
                queue.Enqueue(j);
            }
        }

        for (int i = 0; i < n; i++)
        {
            if (m.Area[i] < 0) { m.Ground[i] = GroundLevel; continue; }
            int x = i % w, y = i / w;
            int e = m.Elevation[i];
            float fallback = reached[i] ? nearest[i] : GroundLevel;
            m.Ground[i] = known[i] ? level[i] : fallback;
            if (e == 0)
            {
                // A run of "any level" tiles between two levels along one way is a ramp from one to the other
                if (Ends(m, known, level, x, y, 0, 1) is { } ns && ns.A != ns.B)
                {
                    m.SlopeZ[i] = (ns.B - ns.A) / ns.Length;
                    m.Ground[i] = ns.A + m.SlopeZ[i] * ns.Before;
                }
                else if (Ends(m, known, level, x, y, 1, 0) is { } we && we.A != we.B)
                {
                    m.SlopeX[i] = (we.B - we.A) / we.Length;
                    m.Ground[i] = we.A + m.SlopeX[i] * we.Before;
                }
            }
            else if (e == 15)
            {
                // The deck is at the level of the bridge's ends; the ground is the lowest beside it (the water it crosses)
                float lo = fallback, hi = fallback;
                foreach (var (dx, dy) in new[] { (0, 1), (1, 0) })
                    if (Ends(m, known, level, x, y, dx, dy, 15) is { } ends)
                        hi = Math.Max(hi, Math.Max(ends.A, ends.B));
                foreach (var (dx, dy) in Sides)
                {
                    int nx = x + dx, ny = y + dy;
                    if (m.Has(nx, ny) && m.Elevation[ny * w + nx] != 15 && reached[ny * w + nx]) lo = Math.Min(lo, nearest[ny * w + nx]);
                }
                m.Ground[i] = lo;
                if (hi > lo) m.Deck[i] = hi;
            }
        }
    }

    private static readonly (int, int)[] Sides = { (0, -1), (0, 1), (-1, 0), (1, 0) };

    /// <summary>
    /// Along one way through a run of tiles of the same elevation, the known levels at its two ends (west or north
    /// first), the run's length in tiles and how many of its tiles lie before this one.
    /// </summary>
    private static (float A, float B, float Length, float Before)? Ends(GbaMatrix m, bool[] known, float[] level, int x, int y, int dx, int dy, int elevation = 0)
    {
        int w = m.Width;
        int Walk(int sx, int sy, int step, out float? found)
        {
            int count = 0;
            found = null;
            int cx = x + sx * step, cy = y + sy * step;
            while (m.Has(cx, cy) && m.Elevation[cy * w + cx] == elevation && count < 8)
            {
                count++;
                cx += sx * step; cy += sy * step;
            }
            if (m.Has(cx, cy) && known[cy * w + cx]) found = level[cy * w + cx];
            return count;
        }
        int before = Walk(dx, dy, -1, out var a);
        int after = Walk(dx, dy, 1, out var b);
        if (a == null || b == null) return null;
        return (a.Value, b.Value, before + after + 1, before);
    }

    // ---------------------------------------------------------------- the world files

    /// <summary>The key of each area a matrix holds, in the order of its maps.</summary>
    public static List<string> KeysOf(GbaMatrix m) => m.Maps.Select(p => p.Map.Key).ToList();

    /// <summary>The map most of a chunk's tiles belong to, or -1 for a chunk of nothing.</summary>
    public static int Owner(GbaMatrix m, int cx, int cy)
    {
        var counts = new Dictionary<int, int>();
        for (int z = 0; z < GbaMatrix.T; z++)
            for (int x = 0; x < GbaMatrix.T; x++)
            {
                int a = m.Area[(cy * GbaMatrix.T + z) * m.Width + cx * GbaMatrix.T + x];
                if (a >= 0) counts[a] = counts.GetValueOrDefault(a) + 1;
            }
        return counts.Count == 0 ? -1 : counts.OrderByDescending(c => c.Value).ThenBy(c => c.Key).First().Key;
    }

    /// <summary>One chunk of a matrix as the world files keep it.</summary>
    public static WorldChunkFile Chunk(GbaMatrix m, int cx, int cy, int id)
    {
        const int T = GbaMatrix.T;
        int owner = Owner(m, cx, cy);
        var chunk = new WorldChunkFile { Id = id };
        bool mixed = false;
        var areas = new List<string>();
        for (int z = 0; z < T; z++)
        {
            var behaviours = new System.Text.StringBuilder();
            var solid = new System.Text.StringBuilder();
            var cover = new System.Text.StringBuilder();
            var area = new System.Text.StringBuilder();
            for (int x = 0; x < T; x++)
            {
                int i = (cy * T + z) * m.Width + cx * T + x;
                behaviours.Append(m.Behaviour[i].ToString("X2", CultureInfo.InvariantCulture));
                solid.Append(m.Solid[i] ? '#' : '.');
                cover.Append(TerrainCoverCodes.CodeOf(m.Cover[i]));
                bool own = m.Area[i] >= 0 && m.Area[i] != owner;
                mixed |= own;
                area.Append(own ? WorldChunkFile.AreaCode(m.Area[i]) : WorldChunkFile.NoArea);
            }
            chunk.Behaviours.Add(behaviours.ToString());
            chunk.Solid.Add(solid.ToString());
            chunk.Cover.Add(cover.ToString());
            areas.Add(area.ToString());
        }
        if (mixed) chunk.Areas = areas;
        chunk.Heights = Plates(m, cx, cy);
        return chunk;
    }

    /// <summary>The chunk's ground as plates: a flat plate per run of tiles at one height, merged down the rows where they agree; a slope or a deck a plate of its own.</summary>
    public static List<HeightPlate> Plates(GbaMatrix m, int cx, int cy)
    {
        const int T = GbaMatrix.T;
        var plates = new List<HeightPlate>();
        var open = new Dictionary<(int X, int W, float H), HeightPlate>();
        for (int z = 0; z < T; z++)
        {
            var row = new Dictionary<(int X, int W, float H), HeightPlate>();
            int x = 0;
            while (x < T)
            {
                int i = (cy * T + z) * m.Width + cx * T + x;
                if (m.Area[i] < 0) { x++; continue; }
                if (m.SlopeX[i] != 0 || m.SlopeZ[i] != 0)
                {
                    plates.Add(new HeightPlate { X = x, Z = z, Width = 1, Depth = 1, Height = m.Ground[i], SlopeX = m.SlopeX[i], SlopeZ = m.SlopeZ[i] });
                    x++;
                    continue;
                }
                float h = m.Ground[i];
                int start = x;
                while (x < T)
                {
                    int j = (cy * T + z) * m.Width + cx * T + x;
                    if (m.Area[j] < 0 || m.SlopeX[j] != 0 || m.SlopeZ[j] != 0 || m.Ground[j] != h) break;
                    x++;
                }
                var key = (start, x - start, h);
                if (open.TryGetValue(key, out var above)) above.Depth++;
                else plates.Add(above = new HeightPlate { X = start, Z = z, Width = x - start, Depth = 1, Height = h });
                row[key] = above;
            }
            open = row;
        }
        // A bridge's deck over the ground, a tile at a time along the row
        for (int z = 0; z < T; z++)
            for (int x = 0; x < T; x++)
            {
                int i = (cy * T + z) * m.Width + cx * T + x;
                if (m.Area[i] >= 0 && !float.IsNaN(m.Deck[i]))
                    plates.Add(new HeightPlate { X = x, Z = z, Width = 1, Depth = 1, Height = m.Deck[i] });
            }
        return plates;
    }

    /// <summary>A matrix as the world files keep it, its chunks numbered from <paramref name="firstChunk"/> (chunks of nothing left out).</summary>
    public static WorldMatrixFile MatrixFile(GbaMatrix m, Func<int, int, int> chunkAt)
    {
        var file = new WorldMatrixFile { Id = m.Id, Width = m.ChunksWide, Height = m.ChunksHigh };
        bool one = m.Maps.Count == 1;
        if (!one)
        {
            file.Areas = new();
            file.AreaKeys = KeysOf(m);
        }
        int cellWidth = 3;
        for (int cy = 0; cy < m.ChunksHigh; cy++)
        {
            var cells = new List<string>();
            var areas = new List<string>();
            for (int cx = 0; cx < m.ChunksWide; cx++)
            {
                int id = chunkAt(cx, cy);
                cells.Add(id < 0 ? "-" : id.ToString(CultureInfo.InvariantCulture));
                int owner = Owner(m, cx, cy);
                areas.Add(owner < 0 ? "-" : owner.ToString(CultureInfo.InvariantCulture));
            }
            file.Chunks.Add(WorldMatrixFile.Row(cells, cellWidth));
            file.Areas?.Add(WorldMatrixFile.Row(areas, 2));
        }
        return file;
    }

    /// <summary>What an area's file says of a GBA map for now: what it is and how it behaves (its events are W7's).</summary>
    public static WorldAreaFile AreaFile(GbaMap map, int index, int matrix) => new()
    {
        Key = map.Key,
        Index = index,
        Name = NameOf(map.Section),
        Matrix = matrix,
        Kind = map.MapType switch
        {
            "MAP_TYPE_TOWN" or "MAP_TYPE_CITY" => "Town",
            "MAP_TYPE_ROUTE" or "MAP_TYPE_OCEAN_ROUTE" => "Outdoors",
            "MAP_TYPE_UNDERGROUND" => "Cave",
            "MAP_TYPE_UNDERWATER" => "Underwater",
            "MAP_TYPE_INDOOR" or "MAP_TYPE_SECRET_BASE" => "Indoors",
            _ => "Outdoors"
        },
        Sign = map.MapType switch
        {
            "MAP_TYPE_TOWN" => "Town",
            "MAP_TYPE_CITY" => "City",
            "MAP_TYPE_ROUTE" => "Route",
            "MAP_TYPE_OCEAN_ROUTE" => "Water",
            "MAP_TYPE_UNDERGROUND" => "Cave",
            "MAP_TYPE_INDOOR" or "MAP_TYPE_SECRET_BASE" => "Indoors",
            _ => "None"
        },
        Weather = map.Weather switch
        {
            "WEATHER_RAIN" => "Raining",
            "WEATHER_SHADE" or "WEATHER_SUNNY_CLOUDS" => "Cloudy",
            "WEATHER_RAIN_THUNDERSTORM" => "Thunderstorm",
            "WEATHER_DOWNPOUR" => "HeavyRain",
            "WEATHER_SNOW" => "Snowing",
            "WEATHER_SANDSTORM" => "Sandstorm",
            "WEATHER_FOG_HORIZONTAL" or "WEATHER_FOG_DIAGONAL" => "Fog",
            "WEATHER_VOLCANIC_ASH" => "SlowAshfall",
            _ => "Clear"
        },
        DayMusic = map.Music,
        NightMusic = map.Music,
        Bike = map.Cycling,
        Running = map.Running,
        EscapeRope = map.Escaping
    };

    /// <summary><c>MAPSEC_PALLET_TOWN</c> to "Pallet Town", <c>MAPSEC_ROUTE_1</c> to "Route 1".</summary>
    public static string NameOf(string section)
    {
        string bare = section.StartsWith("MAPSEC_", StringComparison.Ordinal) ? section[7..] : section;
        return string.Join(' ', bare.Split('_', StringSplitOptions.RemoveEmptyEntries)
            .Select(word => word.Length == 0 ? word : char.ToUpperInvariant(word[0]) + word[1..].ToLowerInvariant()));
    }
}
