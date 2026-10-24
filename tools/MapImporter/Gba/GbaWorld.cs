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

    /// <summary>The piece of furniture on each tile of a room (plan 19 · K2, <see cref="GbaFurniture"/>); null where none stands, and everywhere outdoors.</summary>
    public PropType?[] Furniture = Array.Empty<PropType?>();

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
        Furniture = new PropType?[n];
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

    /// <summary>The real maps laid in for a stand-in (<see cref="StandIns"/>), whose frame overlaps the routes on purpose.</summary>
    private readonly HashSet<string> laidIn = new(StringComparer.Ordinal);

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
            Matrices.Add(Stitch(map, Matrices.Count, placed, alone: StandIns.ContainsKey(map.Id)));
        }
        foreach (var matrix in Matrices.Skip(1)) LayUnderItsRoutes(matrix);
        foreach (var matrix in Matrices)
        {
            Fill(matrix);
            Heights(matrix);
        }
        // Where a faller comes down: the warp each hole names, on the floor below (plan 19 · K6, part b); and where a
        // panel throws its rider (plan 21 · H4, part b: Lavaridge's Gym, whose vents below pop their rider up onto plain
        // sand above, a tile the original never takes since its behaviour is no warp's)
        foreach (var matrix in Matrices)
            foreach (var (map, _, ox, oy) in matrix.Maps)
                foreach (var w in map.Warps)
                    if (matrix.Has(ox + w.X, oy + w.Y)
                        && (TileBehavior)matrix.Behaviour[(oy + w.Y) * matrix.Width + ox + w.X] is TileBehavior.Hole or TileBehavior.WarpPanel)
                        holeTargets.Add((w.DestMap, w.DestWarp));
    }

    private readonly HashSet<(string Map, int Warp)> holeTargets = new();

    /// <summary>
    /// A map whose layout the story changes for another (plan 19 · K6, part b): FireRed's <c>setmaplayoutindex</c>
    /// gives the Seafoam Islands' two lowest floors a layout of calm water once two boulders lie across their currents.
    /// The other layout's chunks are written after every matrix's (<see cref="AlternateOf"/>), so no chunk is numbered
    /// otherwise, and the region's <c>world.json</c> swaps them in by its <c>swaps</c> (<c>Map.ApplySwappedPlaces</c>).
    /// </summary>
    public static readonly IReadOnlyList<(string Map, string Layout)> Alternates = new[]
    {
        ("MAP_SEAFOAM_ISLANDS_B3F", "LAYOUT_SEAFOAM_ISLANDS_B3F_CURRENT_STOPPED"),
        ("MAP_SEAFOAM_ISLANDS_B4F", "LAYOUT_SEAFOAM_ISLANDS_B4F_CURRENT_STOPPED"),
        // Plan 19 · K9, part c: Dunsparce Tunnel dug through to Three Island's port (FireRed's ON_TRANSITION lays this
        // layout once the National Pokédex is had)
        ("MAP_THREE_ISLAND_DUNSPARCE_TUNNEL", "LAYOUT_THREE_ISLAND_DUNSPARCE_TUNNEL_DUG_OUT")
    };

    /// <summary>
    /// The matrix <paramref name="m"/> with the map <paramref name="mapId"/> laid from another of its layouts (the same
    /// size), for <see cref="Alternates"/>; null where the matrix doesn't hold it or the layout isn't there.
    /// </summary>
    public GbaMatrix? AlternateOf(GbaMatrix m, string mapId, string layoutId)
    {
        if (!Source.Layouts.TryGetValue(layoutId, out var layout) || m.Maps.All(p => p.Map.Id != mapId)) return null;
        var alt = new GbaMatrix { Id = m.Id };
        foreach (var p in m.Maps) alt.Maps.Add(p.Map.Id == mapId ? p with { Layout = layout } : p);
        alt.Allocate(m.Width, m.Height);
        Fill(alt, count: false);
        Heights(alt);
        return alt;
    }

    /// <summary>
    /// Whether a map's warp is only where someone falling through a hole above comes down (plan 19 · K6, part b): a
    /// hole names it, and its own tile is no way anywhere (the Seafoam Islands' floors keep one under each hole, on
    /// plain floor or water); or where a panel puts its rider down on plain floor (plan 21 · H4, part b: Lavaridge's
    /// Gym). The original never takes such a warp; here it is written as a landing
    /// (<see cref="AreaWarp.Landing"/>), which leads nowhere and shuts nothing.
    /// </summary>
    public bool IsLanding(string mapId, int warp, TileBehavior own) =>
        holeTargets.Contains((mapId, warp)) && own is not (TileBehavior.Hole or TileBehavior.Door or TileBehavior.WarpPanel
            or TileBehavior.EntranceNorth or TileBehavior.EntranceSouth or TileBehavior.EntranceEast or TileBehavior.EntranceWest
            or TileBehavior.ExitNorth or TileBehavior.ExitSouth or TileBehavior.ExitEast or TileBehavior.ExitWest
            or TileBehavior.StairsEast or TileBehavior.StairsWest);

    public static float LevelOf(int elevation) => GroundLevel + (elevation - 3) * LevelHeight;

    /// <summary>
    /// A map the routes connect to that only stands for another (plan 19 · K5, part b, decided there): FireRed's
    /// routes join <c>SaffronCity_Connection</c>, 48 by 40, a copy of the middle of Saffron City as seen from them,
    /// while the city itself (<c>SaffronCity</c>, 66 by 55, its houses, people and the far doors of its four gates) is
    /// a map no connection reaches. The city's tiles 10 to 57 across and 7 to 46 down are the stand-in's, metatile for
    /// metatile; the rest is a frame that copies the routes' ends round it. So the city is laid in where the stand-in
    /// would be, ten tiles west and seven north of it, and its frame gives way to the routes it copies
    /// (<see cref="Fill"/> lays it last); its neighbours are reached as the stand-in's are. The stand-in is a matrix of
    /// its own, with nothing joined to it, so the matrices' numbers stay what they were.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, (string Real, int X, int Y)> StandIns =
        new Dictionary<string, (string, int, int)>(StringComparer.Ordinal)
        {
            ["MAP_SAFFRON_CITY_CONNECTION"] = ("MAP_SAFFRON_CITY", -10, -7)
        };

    // ---------------------------------------------------------------- stitching

    /// <summary>
    /// A set of the sea floor's maps laid under the routes it lies under (plan 21 · H5): Emerald's underwater maps join
    /// one another by ordinary connections and their routes only by <c>emerge</c>, so the reader stitches them into a
    /// matrix of their own, from its own corner. Here that matrix is moved so that each floor's tiles have the
    /// coordinates of the overworld's tiles above them (Emerald joins a sea and its floor tile for tile, as the two
    /// layouts are the same size), and a dive lands on the same coordinates of another map. The matrix keeps its
    /// number; its empty chunks are none, so it costs nothing to stand where it stands. A floor whose route disagrees
    /// with where its neighbours put it is reported, and the first wins.
    /// </summary>
    private void LayUnderItsRoutes(GbaMatrix matrix)
    {
        if (Matrices.Count == 0 || matrix.Maps.Count == 0 || matrix.Maps.Any(p => p.Map.MapType != "MAP_TYPE_UNDERWATER")) return;
        var overworld = Matrices[0];
        (int X, int Y)? shift = null;
        foreach (var p in matrix.Maps)
            foreach (var c in p.Map.Connections.Where(c => c.Direction == "emerge"))
            {
                if (overworld.Maps.FirstOrDefault(o => o.Map.Id == c.Map) is not { } above) continue;
                if (above.Layout.Width != p.Layout.Width || above.Layout.Height != p.Layout.Height)
                {
                    Problems.Add($"{p.Map.Name} emerges into {above.Map.Name}, which is another size: the floor isn't laid under it");
                    continue;
                }
                var here = (X: above.X - p.X, Y: above.Y - p.Y);
                if (shift == null) shift = here;
                else if (shift != here)
                    Problems.Add($"{p.Map.Name} lies under {above.Map.Name} {here.X - shift.Value.X},{here.Y - shift.Value.Y} tiles from where its neighbours put it: the first placed wins");
            }
        if (shift is not { } by || by.X < 0 || by.Y < 0) return;
        var moved = matrix.Maps.Select(p => p with { X = p.X + by.X, Y = p.Y + by.Y }).ToList();
        matrix.Maps.Clear();
        matrix.Maps.AddRange(moved);
        matrix.Allocate(moved.Max(p => p.X + p.Layout.Width), moved.Max(p => p.Y + p.Layout.Height));
    }

    private GbaMatrix Stitch(GbaMap first, int id, HashSet<string> placed, bool alone = false)
    {
        var at = new Dictionary<string, (int X, int Y)>(StringComparer.Ordinal) { [first.Id] = (0, 0) };
        var list = new List<GbaMap> { first };
        var queue = new Queue<GbaMap>();
        // A real map laid in for a stand-in (StandIns): its neighbours are reached as the stand-in's
        var standsFor = new Dictionary<string, (GbaMap StandIn, int X, int Y)>(StringComparer.Ordinal);
        if (!alone) queue.Enqueue(first);
        placed.Add(first.Id);
        while (queue.Count > 0)
        {
            var map = queue.Dequeue();
            var layout = Source.LayoutOf(map);
            var (mx, my) = at[map.Id];
            var joins = map.Connections;
            if (standsFor.TryGetValue(map.Id, out var frame))
            {
                layout = Source.LayoutOf(frame.StandIn);
                (mx, my) = (mx - frame.X, my - frame.Y);
                joins = frame.StandIn.Connections;
            }
            foreach (var c in joins)
            {
                var target = c.Map;
                (int X, int Y) shift = (0, 0);
                GbaMap? standIn = null;
                if (StandIns.TryGetValue(c.Map, out var real) && Source.ById.TryGetValue(c.Map, out standIn))
                    (target, shift) = (real.Real, (real.X, real.Y));
                if (!Source.ById.TryGetValue(target, out var other) || !Source.Layouts.ContainsKey(other.Layout)
                    || !Source.Layouts.TryGetValue((standIn ?? other).Layout, out var theirs))
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
                if (there is not { } standing) continue;
                var spot = (X: standing.X + shift.X, Y: standing.Y + shift.Y);
                if (standIn != null) standsFor[other.Id] = (standIn, shift.X, shift.Y);
                if (standIn != null) laidIn.Add(other.Id);
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
        // A real map laid in for a stand-in goes last, so its frame gives way to the routes it copies
        foreach (var map in list.Where(m => !standsFor.ContainsKey(m.Id)).Concat(list.Where(m => standsFor.ContainsKey(m.Id))))
            matrix.Maps.Add(new GbaPlaced(map, Source.LayoutOf(map), at[map.Id].X - minX, at[map.Id].Y - minY));
        matrix.Allocate(matrix.Maps.Max(p => p.X + p.Layout.Width), matrix.Maps.Max(p => p.Y + p.Layout.Height));
        return matrix;
    }

    private void Fill(GbaMatrix m, bool count = true)
    {
        var overlaps = new HashSet<(int, int)>();
        for (int index = 0; index < m.Maps.Count; index++)
        {
            var (map, layout, ox, oy) = m.Maps[index];
            bool indoors = GbaImport.SettingOf(map) == MapSetting.Indoors;
            for (int y = 0; y < layout.Height; y++)
                for (int x = 0; x < layout.Width; x++)
                {
                    int i = (oy + y) * m.Width + ox + x;
                    if (m.Area[i] >= 0)
                    {
                        if (!laidIn.Contains(map.Id) && overlaps.Add((m.Area[i], index)))
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
                    var cover = GbaCover.Of(behaviour, label, tileset, metatile, Source.Game);

                    m.Area[i] = index;
                    m.Behaviour[i] = (byte)behaviour;
                    m.Solid[i] = GbaLayout.CollisionOf(block) != 0;
                    m.Elevation[i] = (byte)GbaLayout.ElevationOf(block);
                    // A tile the GBA leaves open is walked, whatever its metatile looks like: a mountain's top named for the
                    // mountain is open rock, never a cliff (the builder blocks every cliff; Route 119's way up to its
                    // bridge was one, plan 21 · H6)
                    if (cover == TerrainCover.Cliff && !m.Solid[i]) cover = TerrainCover.Rock;
                    m.Cover[i] = cover;
                    if (indoors) m.Furniture[i] = GbaFurniture.Of(name, tileset, metatile, Source.Game);
                    if (!count) continue;
                    BehaviourUse[name] = BehaviourUse.GetValueOrDefault(name) + 1;
                    if (cover == TerrainCover.Unknown)
                    {
                        if (!UnknownCover.TryGetValue(tileset, out var counts)) UnknownCover[tileset] = counts = new();
                        counts[metatile] = counts.GetValueOrDefault(metatile) + 1;
                    }
                }
        }
        if (Source.Game == GbaGame.Emerald && m.Id == 0) Sandbars(m);
    }

    /// <summary>
    /// Emerald's shallows out in the open sea, touching no shore (Route 128's islet, where Archie, Maxie and Steven stand;
    /// the reefs of the eastern and southern seas), drawn as a sandbar (plan 21 · H8, part c): Emerald paints such a patch
    /// pale against the deep water round it, and as water to look at it read as open sea with people standing on it.
    /// Shallows that run up to a shore stay water to look at, as part a made them.
    /// </summary>
    private static void Sandbars(GbaMatrix m)
    {
        int w = m.Width, n = w * m.Height;
        var seen = new bool[n];
        bool Shallow(int i) => m.Area[i] >= 0 && (TileBehavior)m.Behaviour[i] == TileBehavior.ShallowWater && m.Cover[i] == TerrainCover.Water;
        for (int s = 0; s < n; s++)
        {
            if (seen[s] || !Shallow(s)) continue;
            var patch = new List<int>();
            bool ashore = false;
            var queue = new Queue<int>();
            queue.Enqueue(s);
            seen[s] = true;
            while (queue.Count > 0)
            {
                int i = queue.Dequeue();
                patch.Add(i);
                int x = i % w, y = i / w;
                foreach (var (dx, dy) in Sides)
                {
                    int nx = x + dx, ny = y + dy;
                    if (!m.Has(nx, ny)) continue;
                    int j = ny * w + nx;
                    if (Shallow(j))
                    {
                        if (!seen[j]) { seen[j] = true; queue.Enqueue(j); }
                    }
                    else if (m.Area[j] >= 0 && m.Cover[j] != TerrainCover.Water) ashore = true;
                }
            }
            if (ashore) continue;
            foreach (int i in patch) m.Cover[i] = TerrainCover.Sand;
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
        // A room is level (plan 19 · K2): its elevations say only which layer a walker is on (an escalator's foot is a
        // level up), and drawn as heights they stood the Pokémon Center's escalator on a step of earth
        if (m.Maps.Count > 0 && GbaImport.SettingOf(m.Maps[0].Map) == MapSetting.Indoors)
        {
            Array.Fill(m.Ground, GroundLevel);
            return;
        }
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

        // Water a bridge crosses lies at one level, the lowest its shores give it (plan 21 · H8, part c): Route 120's pond
        // took its west half's level from the bridge's own raised ends, so the deck stood level with the water there and
        // no deck was laid. Each body of water (elevation 1, joined through the bridge's own tiles) that a bridge
        // crosses takes the lowest level of the land nearest any of its tiles; water no bridge crosses is left as it was,
        // and a waterfall parts one body from another (the river above it keeps its own level)
        bool Water(int i) => m.Area[i] >= 0 && m.Elevation[i] is 1 or 15 && reached[i]
            && (TileBehavior)m.Behaviour[i] is not (TileBehavior.Waterfall or TileBehavior.WaterfallLip);
        var seen = new bool[n];
        for (int s = 0; s < n; s++)
        {
            if (seen[s] || !Water(s)) continue;
            var body = new List<int>();
            bool bridged = false;
            float lowest = float.MaxValue;
            var flood = new Queue<int>();
            flood.Enqueue(s);
            seen[s] = true;
            while (flood.Count > 0)
            {
                int i = flood.Dequeue();
                body.Add(i);
                if (m.Elevation[i] == 15) bridged = true;
                else lowest = Math.Min(lowest, nearest[i]);
                int x = i % w, y = i / w;
                foreach (var (dx, dy) in Sides)
                {
                    int nx = x + dx, ny = y + dy;
                    if (!m.Has(nx, ny)) continue;
                    int j = ny * w + nx;
                    if (seen[j] || !Water(j)) continue;
                    seen[j] = true;
                    flood.Enqueue(j);
                }
            }
            if (!bridged || lowest == float.MaxValue) continue;
            foreach (int i in body)
                if (m.Elevation[i] == 1) nearest[i] = lowest;
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
                // A plank of the bridge whose metatile does nothing (where Route 120's Steven stands) is still the deck
                if (hi > lo && m.Cover[i] == TerrainCover.Path) m.Cover[i] = TerrainCover.Bridge;
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
            // (a bridge runs further than a ramp: Route 120's is sixteen tiles, and its middle took no deck at all)
            while (m.Has(cx, cy) && m.Elevation[cy * w + cx] == elevation && count < (elevation == 15 ? 64 : 8))
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
        // Between the maps of the open country lies the forest every route is walled in by (plan 19 · K1): left as
        // nothing, it was bare rock raised along Pallet Town's west. A room's or a cave's nothing stays nothing
        bool outdoors = m.Maps.Count > 0 && GbaImport.SettingOf(m.Maps[0].Map) == MapSetting.Outdoors;
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
                cover.Append(TerrainCoverCodes.CodeOf(outdoors && m.Area[i] < 0 && m.Solid[i] ? TerrainCover.Tree : m.Cover[i]));
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
        chunk.Props.AddRange(FurnitureOf(m, cx, cy));
        return chunk;
    }

    /// <summary>
    /// A chunk's furniture (plan 19 · K2) as props named for their kind (<c>gba:Table</c>): runs of one kind along a row,
    /// merged down the rows where they line up. A piece that blocks stands only on tiles the layout blocks, and one that
    /// is walked onto (a rug, a seat) only on open ones, so furnishing a room never changes where one can walk.
    /// </summary>
    public static IEnumerable<ChunkProp> FurnitureOf(GbaMatrix m, int cx, int cy)
    {
        const int T = GbaMatrix.T;
        if (m.Furniture.Length == 0) yield break;
        PropType? At(int x, int z)
        {
            int i = (cy * T + z) * m.Width + cx * T + x;
            if (m.Furniture[i] is not { } prop) return null;
            // What hangs on the wall is on the wall's own blocked tiles
            if (prop is PropType.Window or PropType.Painting or PropType.Clock or PropType.WallEmblem) return m.Solid[i] ? prop : null;
            // A museum's case blocks nothing by itself (Sinnoh's rooms block it by their own grid), but nobody walks
            // onto one: it stands on the layout's blocked tiles (plan 21 · H3, the Oceanic Museum)
            // (and so does a Game Corner's slot machine, plan 19 · K6, part b)
            if (prop is PropType.DisplayCase or PropType.SlotMachine) return m.Solid[i] ? prop : null;
            // A bed is one piece over the tiles FireRed blocks and those it lets the player climb onto (plan 19 · K3): the
            // map's own tiles still say which is which
            if (prop is PropType.Bed) return prop;
            return new Prop { Type = prop }.IsSolid == m.Solid[i] ? prop : null;
        }
        var done = new bool[T, T];
        for (int z = 0; z < T; z++)
            for (int x = 0; x < T; x++)
            {
                if (done[x, z] || At(x, z) is not { } prop) continue;
                int width = 1;
                while (x + width < T && !done[x + width, z] && At(x + width, z) == prop) width++;
                int depth = 1;
                while (z + depth < T && Enumerable.Range(x, width).All(xx => !done[xx, z + depth] && At(xx, z + depth) == prop)) depth++;
                for (int dz = 0; dz < depth; dz++)
                    for (int dx = 0; dx < width; dx++) done[x + dx, z + dz] = true;
                yield return new ChunkProp
                {
                    Name = GbaFurniture.Prefix + prop, BoxX = x, BoxZ = z, Width = width, Depth = depth,
                    X = x + width / 2f, Z = z + depth / 2f
                };
            }
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
        // A map dark until Flash is used takes the weather Sinnoh's dark caves have (Map.IsDark; plan 19 · K5: Rock Tunnel)
        Weather = map.Flash ? "DarkFlash" : map.Weather switch
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
        EscapeRope = map.Escaping,
        // Fly and Teleport go from a town, a city, a route or a sea route, by the map's type alone
        // (Overworld_MapTypeAllowsTeleportAndFly); the header has no flag of its own for them
        Fly = map.MapType is "MAP_TYPE_TOWN" or "MAP_TYPE_CITY" or "MAP_TYPE_ROUTE" or "MAP_TYPE_OCEAN_ROUTE"
    };

    /// <summary><c>MAPSEC_PALLET_TOWN</c> to "Pallet Town", <c>MAPSEC_ROUTE_1</c> to "Route 1".</summary>
    public static string NameOf(string section)
    {
        string bare = section.StartsWith("MAPSEC_", StringComparison.Ordinal) ? section[7..] : section;
        return string.Join(' ', bare.Split('_', StringSplitOptions.RemoveEmptyEntries)
            .Select(word => word.Length == 0 ? word : char.ToUpperInvariant(word[0]) + word[1..].ToLowerInvariant()));
    }
}
