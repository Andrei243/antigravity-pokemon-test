using System;
using System.Collections.Generic;
using System.Linq;

namespace PokemonPlatinumEngine.OpenWorld;

/// <summary>
/// A solid box of an open map (plan 32 · OW3, "the boxes of buildings and props come from the kit"): its corners in
/// millimetres, upright and lined up with the axes. OW5's places lay the kit's buildings and props as these; OW3's
/// tests build their corridors, kerbs and walls of them. Its top is ground like any, its sides walls.
/// </summary>
public sealed record OpenBox(string Name, OpenPos Min, OpenPos Max)
{
    public OpenBox Checked()
    {
        if (Min.X >= Max.X || Min.Y >= Max.Y || Min.Z >= Max.Z) throw new ArgumentException($"the box {Name} has no inside: {Min} to {Max}");
        return this;
    }

    /// <summary>A box from its corners in metres.</summary>
    public static OpenBox Metres(string name, double x0, double y0, double z0, double x1, double y1, double z1) =>
        new OpenBox(name, OpenPos.FromMetres(x0, y0, z0), OpenPos.FromMetres(x1, y1, z1)).Checked();
}

/// <summary>
/// Water laid over an open map by hand (plan 32 · OW3): a rectangle on the ground and its surface's height, in
/// millimetres, beside the water the baker baked into each chunk (OW5's water volumes are these). Where two stand over
/// one place the higher surface is the water's.
/// </summary>
public sealed record OpenWaterBox(string Name, int MinX, int MinZ, int MaxX, int MaxZ, int Level)
{
    public bool Holds(double x, double z) =>
        x * OpenPos.PerMetre >= MinX && x * OpenPos.PerMetre <= MaxX && z * OpenPos.PerMetre >= MinZ && z * OpenPos.PerMetre <= MaxZ;

    /// <summary>A rectangle in metres, x0 to x1 east and z0 to z1 south, with its surface at <paramref name="level"/>.</summary>
    public static OpenWaterBox Metres(string name, double x0, double z0, double x1, double z1, double level) =>
        new(name, OpenPos.Millimetres(x0), OpenPos.Millimetres(z0), OpenPos.Millimetres(x1), OpenPos.Millimetres(z1), OpenPos.Millimetres(level));
}

/// <summary>
/// What the capsule is collided with in an open map (plan 32, "Collision"; OW3): level 0's own ground of each baked
/// chunk (its first <see cref="BakedChunk.GroundTriangles"/>, never the apron or the skirts), read the first time a
/// query comes near it, and the map's solid boxes, each made twelve triangles facing out. Kept in metres in double
/// from their millimetres, in squares of 2 m for the queries. Single-threaded and ours: nothing is answered by a
/// physics engine (decision 2).
/// <para>
/// <b>The answers never hang on the order chunks were read in.</b> Every triangle has a key of its own, its chunk's
/// row, column and number in the chunk (a box's after every chunk's), each square keeps its triangles in the keys'
/// order, and a query reads every chunk whose triangles could reach it before it looks, so a game continued from a
/// save, which reads its chunks in another order, is answered as the game it was saved from. Where two answers tie
/// (two floors at one height, two walls as deep) the first in that order wins.
/// </para>
/// GPU-free.
/// </summary>
public sealed class OpenGround
{
    /// <summary>The side of a square of the index, in metres.</summary>
    public const double Square = 2.0;

    /// <summary>How far a chunk's own ground reaches past its east and south borders, in metres (the row it shares with the neighbours there).</summary>
    private const double Reach = 1.0;

    /// <summary>The steepest the capsule stands on without sliding, as the walkable navigation takes it: 40° (cos 40° ≈ 0.766).</summary>
    public const double WalkableY = 0.766;

    /// <summary>What faces up at all: a face whose normal is below this is a wall or a ceiling, never stood on (decision 14).</summary>
    public const double FacesUpY = 0.05;

    /// <summary>A triangle of the ground: its corners and its normal in metres, its bounds on the ground and its key.</summary>
    public readonly record struct Triangle(OpenVec A, OpenVec B, OpenVec C, OpenVec Normal, long Key,
        double MinX, double MinY, double MinZ, double MaxX, double MaxY, double MaxZ)
    {
        public bool Walkable => Normal.Y >= WalkableY;
        public bool FacesUp => Normal.Y >= FacesUpY;
    }

    private readonly Func<int, int, BakedChunk?>? chunks;
    private readonly HashSet<(int, int)> read = new();
    private readonly List<Triangle> triangles = new();
    private readonly List<int> marks = new();
    private int stamp;
    private readonly Dictionary<(int, int), List<int>> squares = new();
    private readonly Dictionary<(int, int), int[]> water = new();
    private readonly List<OpenWaterBox> waterBoxes = new();

    /// <summary>
    /// The ground of a map: level 0 of each chunk as <paramref name="level0"/> gives it (null where the map has none),
    /// and the boxes. Either may be empty.
    /// </summary>
    public OpenGround(Func<int, int, BakedChunk?>? level0, IEnumerable<OpenBox>? boxes = null, IEnumerable<OpenWaterBox>? waters = null)
    {
        chunks = level0;
        long key = BoxKeys;
        foreach (var box in boxes ?? Enumerable.Empty<OpenBox>())
        {
            AddBox(box.Checked(), key);
            key += 12;
        }
        waterBoxes.AddRange(waters ?? Enumerable.Empty<OpenWaterBox>());
        HasAnything = chunks != null || triangles.Count > 0 || waterBoxes.Count > 0;
    }

    /// <summary>A map's ground: its land's level 0 and its boxes and water.</summary>
    public static OpenGround Of(OpenMap map) =>
        new(map.Land is { } land ? (x, z) => land.Chunk(x, z, 0) : null, map.Solids, map.Waters);

    /// <summary>Whether there is any ground at all: a map with none (OW1's empty maps) is a place to stand, not to fall through.</summary>
    public bool HasAnything { get; }

    /// <summary>The triangles read so far (boxes' and chunks').</summary>
    public int TrianglesRead => triangles.Count;

    // A box's triangles come after every chunk's
    private const long BoxKeys = 1L << 62;

    private static long KeyOf(int chunkX, int chunkZ, int t) =>
        ((((long)(chunkZ + (1 << 18)) << 19) | (long)(chunkX + (1 << 18))) << 24) | (long)t;

    private void AddBox(OpenBox box, long key)
    {
        var lo = OpenVec.Of(box.Min);
        var hi = OpenVec.Of(box.Max);
        OpenVec P(int i) => new((i & 1) == 0 ? lo.X : hi.X, (i & 2) == 0 ? lo.Y : hi.Y, (i & 4) == 0 ? lo.Z : hi.Z);
        // Each face counter-clockwise seen from outside, two triangles each
        int[][] faces =
        {
            new[] { 2, 6, 7, 3 },   // top (+y)
            new[] { 0, 1, 5, 4 },   // bottom (−y)
            new[] { 4, 5, 7, 6 },   // south (+z)
            new[] { 0, 2, 3, 1 },   // north (−z)
            new[] { 1, 3, 7, 5 },   // east (+x)
            new[] { 0, 4, 6, 2 },   // west (−x)
        };
        foreach (var f in faces)
        {
            Add(P(f[0]), P(f[1]), P(f[2]), key++);
            Add(P(f[0]), P(f[2]), P(f[3]), key++);
        }
        SortSquares();
    }

    private void Add(OpenVec a, OpenVec b, OpenVec c, long key)
    {
        var normal = OpenVec.Cross(b - a, c - a).Normalized();
        var t = new Triangle(a, b, c, normal, key,
            Math.Min(a.X, Math.Min(b.X, c.X)), Math.Min(a.Y, Math.Min(b.Y, c.Y)), Math.Min(a.Z, Math.Min(b.Z, c.Z)),
            Math.Max(a.X, Math.Max(b.X, c.X)), Math.Max(a.Y, Math.Max(b.Y, c.Y)), Math.Max(a.Z, Math.Max(b.Z, c.Z)));
        int index = triangles.Count;
        triangles.Add(t);
        marks.Add(0);
        for (int z = Cell(t.MinZ); z <= Cell(t.MaxZ); z++)
            for (int x = Cell(t.MinX); x <= Cell(t.MaxX); x++)
            {
                if (!squares.TryGetValue((x, z), out var list)) squares[(x, z)] = list = new List<int>();
                list.Add(index);
            }
    }

    private bool unsorted;
    private void SortSquares() => unsorted = true;

    private void SortIfNeeded()
    {
        if (!unsorted) return;
        foreach (var list in squares.Values) list.Sort((p, q) => triangles[p].Key.CompareTo(triangles[q].Key));
        unsorted = false;
    }

    private static int Cell(double v) => (int)Math.Floor(v / Square);
    private static int ChunkOf(double v) => (int)Math.Floor(v / (OpenMap.ChunkSide / (double)OpenPos.PerMetre));

    /// <summary>Reads every chunk whose ground could lie between two corners on the ground, before anything there is asked.</summary>
    private void ReadAround(double x0, double z0, double x1, double z1)
    {
        if (chunks == null) return;
        int cx0 = ChunkOf(x0 - Reach), cx1 = ChunkOf(x1), cz0 = ChunkOf(z0 - Reach), cz1 = ChunkOf(z1);
        for (int cz = cz0; cz <= cz1; cz++)
            for (int cx = cx0; cx <= cx1; cx++)
            {
                if (!read.Add((cx, cz))) continue;
                if (chunks(cx, cz) is not { } chunk) continue;
                if (chunk.Level != 0) throw new InvalidOperationException($"the ground of chunk {cx},{cz} must be level 0, not {chunk.Level}");
                for (int t = 0; t < chunk.GroundTriangles; t++)
                    Add(OpenVec.Of(chunk.Vertex(chunk.Indices[t * 3])), OpenVec.Of(chunk.Vertex(chunk.Indices[t * 3 + 1])),
                        OpenVec.Of(chunk.Vertex(chunk.Indices[t * 3 + 2])), KeyOf(cx, cz, t));
                if (chunk.HasWater) water[(cx, cz)] = chunk.Water;
                SortSquares();
            }
    }

    /// <summary>A triangle by its index in what was read (the index a query gives back).</summary>
    public Triangle this[int index] => triangles[index];

    /// <summary>
    /// The triangles near a column, each once, in the order of the squares (north to south, west to east) and of the
    /// keys within each: those whose squares lie within <paramref name="reach"/> metres of x, z. Into a list of the
    /// caller's, which is cleared first.
    /// </summary>
    public void Near(double x, double z, double reach, List<int> into)
    {
        into.Clear();
        ReadAround(x - reach, z - reach, x + reach, z + reach);
        SortIfNeeded();
        int x0 = Cell(x - reach), x1 = Cell(x + reach), z0 = Cell(z - reach), z1 = Cell(z + reach);
        if (++stamp == int.MaxValue) { stamp = 1; for (int i = 0; i < marks.Count; i++) marks[i] = 0; }
        for (int cz = z0; cz <= z1; cz++)
            for (int cx = x0; cx <= x1; cx++)
                if (squares.TryGetValue((cx, cz), out var list))
                    foreach (int t in list)
                    {
                        if (marks[t] == stamp) continue;
                        marks[t] = stamp;
                        into.Add(t);
                    }
    }

    /// <summary>
    /// The highest ground facing up at a column between two heights, and its triangle's index: what the capsule
    /// stands on, steep or not (decision 14). A face turned down (a ceiling, the inside of the rock) is never stood on.
    /// </summary>
    public (double Y, int Triangle)? Footing(double x, double z, double top, double bottom, bool walkableOnly = false)
    {
        ReadAround(x, z, x, z);
        SortIfNeeded();
        if (!squares.TryGetValue((Cell(x), Cell(z)), out var list)) return null;
        (double Y, int Triangle)? best = null;
        foreach (int i in list)
        {
            var t = triangles[i];
            if (walkableOnly ? !t.Walkable : !t.FacesUp) continue;
            if (x < t.MinX - 1e-9 || x > t.MaxX + 1e-9 || z < t.MinZ - 1e-9 || z > t.MaxZ + 1e-9) continue;
            if (t.MinY > top + 1e-9 || t.MaxY < bottom - 1e-9) continue;
            if (OpenTriangles.HeightIn(x, z, t.A, t.B, t.C) is not { } y) continue;
            if (y > top + 1e-9 || y < bottom - 1e-9) continue;
            if (best == null || y > best.Value.Y) best = (y, i);
        }
        return best;
    }

    /// <summary>Every surface a vertical line through x, z meets, either way up, highest first, each height once.</summary>
    public List<(double Y, int Triangle)> Column(double x, double z)
    {
        ReadAround(x, z, x, z);
        SortIfNeeded();
        var hits = new List<(double Y, int Triangle)>();
        if (!squares.TryGetValue((Cell(x), Cell(z)), out var list)) return hits;
        foreach (int i in list)
        {
            var t = triangles[i];
            if (OpenTriangles.HeightIn(x, z, t.A, t.B, t.C) is { } y) hits.Add((y, i));
        }
        hits.Sort((p, q) => q.Y.CompareTo(p.Y) != 0 ? q.Y.CompareTo(p.Y) : triangles[p.Triangle].Key.CompareTo(triangles[q.Triangle].Key));
        for (int i = hits.Count - 1; i > 0; i--)
            if (hits[i - 1].Y - hits[i].Y < 1e-6) hits.RemoveAt(i);
        return hits;
    }

    /// <summary>The water's surface over a column (the baked water's 2 m cell, or a water box's), in metres, or null where there is none; the higher where two are.</summary>
    public double? WaterAt(double x, double z)
    {
        ReadAround(x, z, x, z);
        double? best = null;
        double side = OpenMap.ChunkSide / (double)OpenPos.PerMetre;
        int cx = ChunkOf(x), cz = ChunkOf(z);
        if (water.TryGetValue((cx, cz), out var cells))
        {
            int i = Math.Clamp((int)Math.Floor((x - cx * side) / (side / BakedChunk.WaterCells)), 0, BakedChunk.WaterCells - 1);
            int k = Math.Clamp((int)Math.Floor((z - cz * side) / (side / BakedChunk.WaterCells)), 0, BakedChunk.WaterCells - 1);
            int level = cells[k * BakedChunk.WaterCells + i];
            if (level != BakedChunk.NoWater) best = level / (double)OpenPos.PerMetre;
        }
        foreach (var box in waterBoxes)
            if (box.Holds(x, z) && (best == null || box.Level / (double)OpenPos.PerMetre > best)) best = box.Level / (double)OpenPos.PerMetre;
        return best;
    }

    /// <summary>The nearest hit of a ray within <paramref name="max"/> metres, either face, or null: a line of sight, the camera's arm.</summary>
    public double? Raycast(OpenVec origin, OpenVec dir, double max)
    {
        var end = origin + dir * max;
        double minX = Math.Min(origin.X, end.X), maxX = Math.Max(origin.X, end.X);
        double minZ = Math.Min(origin.Z, end.Z), maxZ = Math.Max(origin.Z, end.Z);
        ReadAround(minX, minZ, maxX, maxZ);
        SortIfNeeded();
        double? best = null;
        if (++stamp == int.MaxValue) { stamp = 1; for (int i = 0; i < marks.Count; i++) marks[i] = 0; }
        for (int cz = Cell(minZ - 0.01); cz <= Cell(maxZ + 0.01); cz++)
            for (int cx = Cell(minX - 0.01); cx <= Cell(maxX + 0.01); cx++)
            {
                if (!squares.TryGetValue((cx, cz), out var list)) continue;
                foreach (int i in list)
                {
                    if (marks[i] == stamp) continue;
                    marks[i] = stamp;
                    var t = triangles[i];
                    if (OpenTriangles.Ray(origin, dir, t.A, t.B, t.C) is { } hit && hit <= max && (best == null || hit < best)) best = hit;
                }
            }
        return best;
    }
}
