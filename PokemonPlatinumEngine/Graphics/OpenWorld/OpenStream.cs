using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Threading.Tasks;
using Raylib_cs;
using PokemonPlatinumEngine.OpenWorld;

namespace PokemonPlatinumEngine.Graphics.OpenWorld;

/// <summary>A chunk at a level, on the card: its ground and apron, its skirts, its water, the box round them and the water's cells.</summary>
internal sealed class OpenChunkMeshes
{
    public required int X { get; init; }
    public required int Z { get; init; }
    public required int Level { get; init; }
    public Mesh Surface;
    public int SurfaceTriangles;
    public Mesh? Skirts;
    public Mesh? Water;
    public int WaterTriangles;
    public Vector3 Min, Max;
    public required int[] WaterCells { get; init; }
    public required List<float> EdgeHeights { get; init; }

    /// <summary>The grass's tufts on the chunk's finest ground (plan 32 · OW9), none at a thinned level.</summary>
    public IReadOnlyList<OpenTuft> Tufts { get; init; } = Array.Empty<OpenTuft>();
    public bool HasWater => Water != null;

    public void Unload()
    {
        Gpu.Unload(Surface);
        if (Skirts is { } skirts) Gpu.Unload(skirts);
        if (Water is { } water) Gpu.Unload(water);
    }
}

/// <summary>
/// The open world's chunks streamed (plan 32 · OW8; style guide, "Open world", "Levels"): each chunk of the map's baked
/// land wanted at the level its ring asks (<see cref="OpenRings"/>), read and made ready on a worker that gives way
/// (the file read afresh, never through <see cref="BakedRegion.Chunk"/>, which is the rules' and one thread's), and
/// uploaded on the window's thread at most <see cref="UploadBudgetMs"/> a frame, except where a chunk the camera sees
/// has nothing to draw yet, which the frame waits for. A chunk is drawn at the level it has until the level wanted is
/// on the card: there is never a hole. As <c>WorldRenderer</c>'s streaming is for the grid's chunks.
/// </summary>
internal sealed class OpenStream
{
    /// <summary>The time a frame may spend uploading chunks it can do without, in milliseconds.</summary>
    public const double UploadBudgetMs = 2.5;

    /// <summary>Chunks further than this from the feet are let go, in metres (past the fog's end).</summary>
    public const double KeepReach = 1200;

    private sealed class Coming
    {
        public required int Level;
        public Task<Prepared>? Pending;
        public Queue<Action>? Uploading;
        public OpenChunkMeshes? Arriving;
    }

    /// <summary>A chunk made ready on the worker: its meshes in their own memory, not yet on the card.</summary>
    private sealed class Prepared
    {
        public required OpenChunkMeshes Chunk;
        public Mesh Surface;
        public Mesh? Skirts;
        public Mesh? Water;
    }

    private readonly Dictionary<(int X, int Z), OpenChunkMeshes> ready = new();
    private readonly Dictionary<(int X, int Z), Coming> coming = new();
    // Levels wanted no more whose worker hadn't finished: let go once it has, never waited for
    private readonly List<Task<Prepared>> draining = new();
    private readonly System.Diagnostics.Stopwatch clock = new();
    private OpenMap? map;
    private (int X0, int Z0, int X1, int Z1) bounds;

    /// <summary>The map whose chunks these are.</summary>
    public OpenMap? Map => map;

    /// <summary>What the stream has cost: chunks made ready, the most a frame spent, the frames that waited.</summary>
    public int Chunks { get; private set; }
    public double WorstFrameMs { get; private set; }
    public int Waits { get; private set; }
    public double LastFrameMs { get; private set; }

    /// <summary>The chunks on the card now, each at the level it is drawn at.</summary>
    public IEnumerable<OpenChunkMeshes> Ready => ready.Values;

    /// <summary>How many chunks are on the card at each level.</summary>
    public int[] AtLevel(int levels)
    {
        var counts = new int[Math.Max(1, levels)];
        foreach (var c in ready.Values) if (c.Level < counts.Length) counts[c.Level]++;
        return counts;
    }

    /// <summary>The region's chunks' extent, in chunks (west, north, east, south), once a map is in use.</summary>
    public (int X0, int Z0, int X1, int Z1) Bounds => bounds;

    /// <summary>The map drawn: another lets go of everything of the last.</summary>
    public void Use(OpenMap? next)
    {
        if (ReferenceEquals(next, map)) return;
        Clear();
        map = next;
        bounds = (0, 0, -1, -1);
        if (map?.Land is { } land)
        {
            int x0 = int.MaxValue, z0 = int.MaxValue, x1 = int.MinValue, z1 = int.MinValue;
            foreach (var (x, z) in land.Chunks) { x0 = Math.Min(x0, x); z0 = Math.Min(z0, z); x1 = Math.Max(x1, x); z1 = Math.Max(z1, z); }
            if (x0 <= x1) bounds = (x0, z0, x1, z1);
        }
    }

    /// <summary>
    /// A frame: every chunk within <see cref="KeepReach"/> wanted at its ring's level, what isn't there yet begun on
    /// the worker (the chunks <paramref name="seen"/> says the camera sees first), the uploads run within the budget
    /// (or whole for a seen chunk with nothing drawn), each level swapped in once it is whole, and what is out of reach
    /// let go.
    /// </summary>
    public void Update(OpenRings rings, OpenVec feet, Func<int, int, bool> seen, bool everything = false)
    {
        LastFrameMs = 0;
        if (map?.Land is not { } land) return;
        int levels = land.Levels;
        clock.Restart();
        for (int i = draining.Count - 1; i >= 0; i--)
        {
            if (!draining[i].IsCompleted) continue;
            Free(draining[i].GetAwaiter().GetResult());
            draining.RemoveAt(i);
        }

        // What is wanted, at which level, and what to begin: the seen chunks first
        for (int pass = 0; pass < 2; pass++)
            foreach (var (x, z) in land.Chunks)
            {
                if (OpenRings.DistanceTo(x, z, feet) > KeepReach) continue;
                bool inView = seen(x, z);
                if (pass == 0 != inView) continue;
                int current = ready.TryGetValue((x, z), out var has) ? has.Level : -1;
                int want = rings.LevelOf(x, z, feet, levels, current);
                if (current == want) { coming.Remove((x, z), out var stale); Drop(stale); continue; }
                if (coming.TryGetValue((x, z), out var c) && c.Level == want) continue;
                if (c != null) { coming.Remove((x, z)); Drop(c); }
                coming[(x, z)] = Begin(land.Folder, x, z, want, bounds);
            }

        // The uploads: a seen chunk with nothing drawn now must be there now
        var done = new List<(int, int)>();
        foreach (var (key, c) in coming)
        {
            bool urgent = everything || !ready.ContainsKey(key) && seen(key.X, key.Z);
            if (c.Pending != null)
            {
                if (!c.Pending.IsCompleted)
                {
                    if (!urgent) continue;
                    c.Pending.Wait();
                    Waits++;
                }
                var prepared = c.Pending.GetAwaiter().GetResult();
                c.Pending = null;
                c.Arriving = prepared.Chunk;
                c.Uploading = Uploads(prepared);
            }
            while (c.Uploading!.Count > 0 && (urgent || clock.Elapsed.TotalMilliseconds < UploadBudgetMs)) c.Uploading.Dequeue()();
            if (c.Uploading.Count > 0) continue;
            if (ready.Remove(key, out var old)) old.Unload();
            ready[key] = c.Arriving!;
            Chunks++;
            done.Add(key);
        }
        foreach (var key in done) coming.Remove(key);

        // Out of reach: let go
        List<(int, int)>? far = null;
        foreach (var key in ready.Keys)
            if (OpenRings.DistanceTo(key.X, key.Z, feet) > KeepReach + 64) (far ??= new()).Add(key);
        if (far != null) foreach (var key in far) { ready.Remove(key, out var gone); gone!.Unload(); }

        LastFrameMs = clock.Elapsed.TotalMilliseconds;
        WorstFrameMs = Math.Max(WorstFrameMs, LastFrameMs);
    }

    /// <summary>Waits for every chunk under way and puts it on the card at the level it is wanted at (a shot set up).</summary>
    public void Settle(OpenRings rings, OpenVec feet)
    {
        for (int round = 0; round < 8; round++)
        {
            Update(rings, feet, (_, _) => true, everything: true);
            if (coming.Count == 0) break;
        }
        foreach (var task in draining) Free(task.GetAwaiter().GetResult());
        draining.Clear();
    }

    /// <summary>Whether nothing is under way: every chunk at the level it is wanted at.</summary>
    public bool Settled => coming.Count == 0;

    private static Coming Begin(string folder, int x, int z, int level, (int X0, int Z0, int X1, int Z1) bounds) =>
        new() { Level = level, Pending = Task.Run(() => Prepare(folder, x, z, level, bounds)) };

    /// <summary>On the worker, giving way: the chunk read from its file, its vertices worked out and packed.</summary>
    private static Prepared Prepare(string folder, int x, int z, int level, (int X0, int Z0, int X1, int Z1) bounds)
    {
        var thread = System.Threading.Thread.CurrentThread;
        var priority = thread.Priority;
        thread.Priority = System.Threading.ThreadPriority.BelowNormal;
        try
        {
            BakedChunk chunk;
            using (var stream = File.OpenRead(Path.Combine(folder, BakedChunk.FileName(x, z, level)))) chunk = BakedChunk.Read(stream);
            var (surface, skirts) = OpenTerrainMesh.Of(chunk);
            var water = OpenTerrainMesh.WaterOf(chunk);
            var meshes = new OpenChunkMeshes
            {
                X = x, Z = z, Level = level, WaterCells = chunk.Water,
                EdgeHeights = OpenTerrainMesh.EdgeHeights(chunk, x == bounds.X0, x == bounds.X1, z == bounds.Z0, z == bounds.Z1),
                Tufts = OpenGrass.Of(chunk),
                SurfaceTriangles = surface.Triangles, WaterTriangles = water?.Triangles ?? 0,
                Min = surface.Min, Max = surface.Max
            };
            if (water != null) { meshes.Min = Vector3.Min(meshes.Min, water.Min); meshes.Max = Vector3.Max(meshes.Max, water.Max); }
            return new Prepared
            {
                Chunk = meshes,
                Surface = surface.Pack(),
                Skirts = skirts?.Pack(),
                Water = water?.Pack()
            };
        }
        finally
        {
            thread.Priority = priority;
        }
    }

    private static Queue<Action> Uploads(Prepared prepared)
    {
        var steps = new Queue<Action>();
        var chunk = prepared.Chunk;
        steps.Enqueue(() => { var m = prepared.Surface; Gpu.Upload(ref m, "openchunk"); chunk.Surface = m; });
        if (prepared.Skirts is { } skirts) steps.Enqueue(() => { var m = skirts; Gpu.Upload(ref m, "openchunk"); chunk.Skirts = m; });
        if (prepared.Water is { } water) steps.Enqueue(() => { var m = water; Gpu.Upload(ref m, "openchunk"); chunk.Water = m; });
        return steps;
    }

    /// <summary>A level that is wanted no more: let go, whatever of it was on the card, or once its worker is done.</summary>
    private void Drop(Coming? c)
    {
        if (c == null) return;
        if (c.Pending != null)
        {
            if (c.Pending.IsCompleted) Free(c.Pending.GetAwaiter().GetResult());
            else draining.Add(c.Pending);
            return;
        }
        if (c.Arriving == null || c.Uploading == null) return;
        // Uploaded in part: what is on the card let go, and what isn't freed as the steps would have made it
        while (c.Uploading.Count > 0) c.Uploading.Dequeue()();
        c.Arriving.Unload();
    }

    /// <summary>A level that never reached the card: its meshes' own memory freed (an unloaded mesh's buffers are nought).</summary>
    private static void Free(Prepared prepared)
    {
        Gpu.Unload(prepared.Surface);
        if (prepared.Skirts is { } s) Gpu.Unload(s);
        if (prepared.Water is { } w) Gpu.Unload(w);
    }

    /// <summary>The water's surface over a point, from the chunks on the card (their 2 m cells), or null.</summary>
    public float? WaterAt(float x, float z)
    {
        float side = OpenMap.ChunkSide / (float)OpenPos.PerMetre, cell = side / BakedChunk.WaterCells;
        int cx = (int)MathF.Floor(x / side), cz = (int)MathF.Floor(z / side);
        if (!ready.TryGetValue((cx, cz), out var c)) return null;
        int i = Math.Clamp((int)MathF.Floor((x - cx * side) / cell), 0, BakedChunk.WaterCells - 1);
        int k = Math.Clamp((int)MathF.Floor((z - cz * side) / cell), 0, BakedChunk.WaterCells - 1);
        int level = c.WaterCells[k * BakedChunk.WaterCells + i];
        return level == BakedChunk.NoWater ? null : level / (float)OpenPos.PerMetre;
    }

    /// <summary>The surface of the water cell nearest a point within a reach, from the chunks on the card, or null.</summary>
    public float? WaterNear(float x, float z, float reach)
    {
        float side = OpenMap.ChunkSide / (float)OpenPos.PerMetre, cell = side / BakedChunk.WaterCells;
        float best = reach * reach;
        float? level = null;
        foreach (var c in ready.Values)
        {
            if (!c.HasWater) continue;
            float x0 = c.X * side, z0 = c.Z * side;
            if (x < x0 - reach || x > x0 + side + reach || z < z0 - reach || z > z0 + side + reach) continue;
            for (int k = 0; k < BakedChunk.WaterCells; k++)
                for (int i = 0; i < BakedChunk.WaterCells; i++)
                {
                    int l = c.WaterCells[k * BakedChunk.WaterCells + i];
                    if (l == BakedChunk.NoWater) continue;
                    float dx = x0 + (i + 0.5f) * cell - x, dz = z0 + (k + 0.5f) * cell - z;
                    float d = dx * dx + dz * dz;
                    if (d < best) { best = d; level = l / (float)OpenPos.PerMetre; }
                }
        }
        return level;
    }

    /// <summary>The median height of the region's outer border, from its chunks on the card: where the plain beyond it lies.</summary>
    public float? EdgeHeight()
    {
        var all = new List<float>();
        foreach (var c in ready.Values) all.AddRange(c.EdgeHeights);
        if (all.Count == 0) return null;
        all.Sort();
        return all[all.Count / 2];
    }

    /// <summary>Lets go of everything, on the card and under way.</summary>
    public void Clear()
    {
        foreach (var c in coming.Values) Drop(c);
        coming.Clear();
        foreach (var task in draining) Free(task.GetAwaiter().GetResult());
        draining.Clear();
        foreach (var c in ready.Values) c.Unload();
        ready.Clear();
    }
}
