namespace OpenWorldBake;

/// <summary>
/// The triangles the capsule, the camera's arm and the navigation are asked of (plan 32, "Collision"): the ground of
/// the finest chunks baked, in double from their millimetres, kept in squares of 2 m for the queries. Single-threaded
/// and ours; nothing is answered from a physics engine.
/// </summary>
public sealed class CollisionMesh
{
    public const double Square = 2.0;

    public readonly List<(D3 A, D3 B, D3 C, D3 Normal)> Triangles = new();
    readonly Dictionary<(int, int), List<int>> squares = new();

    public static CollisionMesh Of(Bake bake, int lod = 0)
    {
        var mesh = new CollisionMesh();
        foreach (var ((cx, cz, l), chunk) in bake.Chunks.OrderBy(c => c.Key.Lod).ThenBy(c => c.Key.Z).ThenBy(c => c.Key.X))
        {
            if (l != lod) continue;
            for (int t = 0; t < chunk.GroundTriangles; t++)
                mesh.Add(chunk.Vertex(chunk.Indices[t * 3]), chunk.Vertex(chunk.Indices[t * 3 + 1]), chunk.Vertex(chunk.Indices[t * 3 + 2]));
        }
        return mesh;
    }

    public void Add(D3 a, D3 b, D3 c)
    {
        var normal = D3.Cross(b - a, c - a).Normalized();
        int index = Triangles.Count;
        Triangles.Add((a, b, c, normal));
        int x0 = Cell(Math.Min(a.X, Math.Min(b.X, c.X))), x1 = Cell(Math.Max(a.X, Math.Max(b.X, c.X)));
        int z0 = Cell(Math.Min(a.Z, Math.Min(b.Z, c.Z))), z1 = Cell(Math.Max(a.Z, Math.Max(b.Z, c.Z)));
        for (int z = z0; z <= z1; z++)
            for (int x = x0; x <= x1; x++)
            {
                if (!squares.TryGetValue((x, z), out var list)) squares[(x, z)] = list = new List<int>();
                list.Add(index);
            }
    }

    static int Cell(double v) => (int)Math.Floor(v / Square);

    public bool Walkable(int t) => Triangles[t].Normal.Y >= TerrainWorld.WalkableY;

    /// <summary>The triangles whose squares lie within <paramref name="reach"/> of a column, each once, in order.</summary>
    public IEnumerable<int> Near(double x, double z, double reach)
    {
        int x0 = Cell(x - reach), x1 = Cell(x + reach), z0 = Cell(z - reach), z1 = Cell(z + reach);
        if (x0 == x1 && z0 == z1)
        {
            if (squares.TryGetValue((x0, z0), out var only)) foreach (int t in only) yield return t;
            yield break;
        }
        var seen = new HashSet<int>();
        for (int cz = z0; cz <= z1; cz++)
            for (int cx = x0; cx <= x1; cx++)
                if (squares.TryGetValue((cx, cz), out var list))
                    foreach (int t in list)
                        if (seen.Add(t)) yield return t;
    }

    /// <summary>The highest walkable ground at a column between two heights, or null: where something stands.</summary>
    public double? GroundBelow(double x, double z, double top, double bottom) =>
        Footing(x, z, top, bottom, walkableOnly: true) is { } f ? f.Y : null;

    /// <summary>
    /// The highest ground facing up at a column between two heights, and its triangle: the capsule's footing, steep
    /// or not. A body stands on a steep face as on any ground and slides down it; only a face turned down (a ceiling,
    /// seen from inside the rock) is never stood on, so nothing falls through a slope it was pushed over.
    /// </summary>
    public (double Y, int Triangle)? Footing(double x, double z, double top, double bottom, bool walkableOnly = false)
    {
        (double Y, int Triangle)? best = null;
        if (!squares.TryGetValue((Cell(x), Cell(z)), out var list)) return null;
        foreach (int t in list)
        {
            if (walkableOnly ? !Walkable(t) : Triangles[t].Normal.Y < 0.05) continue;
            var (a, b, c, _) = Triangles[t];
            if (HeightIn(x, z, a, b, c) is not { } y) continue;
            if (y > top + 1e-9 || y < bottom - 1e-9) continue;
            if (best == null || y > best.Value.Y) best = (y, t);
        }
        return best;
    }

    /// <summary>Every surface a vertical line meets, walkable or not, highest first: what the plan's layers are counted by.</summary>
    public List<(double Y, int Triangle)> Column(double x, double z)
    {
        var hits = new List<(double, int)>();
        if (!squares.TryGetValue((Cell(x), Cell(z)), out var list)) return hits;
        foreach (int t in list)
        {
            var (a, b, c, _) = Triangles[t];
            if (HeightIn(x, z, a, b, c) is { } y) hits.Add((y, t));
        }
        hits.Sort((p, q) => q.Item1.CompareTo(p.Item1) != 0 ? q.Item1.CompareTo(p.Item1) : p.Item2.CompareTo(q.Item2));
        // A line through the edge two triangles share meets both at one height: the surface is counted once
        for (int i = hits.Count - 1; i > 0; i--)
            if (hits[i - 1].Item1 - hits[i].Item1 < 1e-6) hits.RemoveAt(i);
        return hits;
    }

    /// <summary>The height of a triangle over a column, if the column passes through it.</summary>
    static double? HeightIn(double x, double z, D3 a, D3 b, D3 c)
    {
        double d = (b.Z - c.Z) * (a.X - c.X) + (c.X - b.X) * (a.Z - c.Z);
        if (Math.Abs(d) < 1e-12) return null;
        double l1 = ((b.Z - c.Z) * (x - c.X) + (c.X - b.X) * (z - c.Z)) / d;
        double l2 = ((c.Z - a.Z) * (x - c.X) + (a.X - c.X) * (z - c.Z)) / d;
        double l3 = 1 - l1 - l2;
        const double eps = -1e-9;
        if (l1 < eps || l2 < eps || l3 < eps) return null;
        return l1 * a.Y + l2 * b.Y + l3 * c.Y;
    }

    /// <summary>The nearest hit of a ray within <paramref name="max"/> metres, or null: the camera's arm, a line of sight.</summary>
    public double? Raycast(D3 origin, D3 dir, double max)
    {
        var end = origin + dir * max;
        double reach = 0.01;
        double minX = Math.Min(origin.X, end.X) - reach, maxX = Math.Max(origin.X, end.X) + reach;
        double minZ = Math.Min(origin.Z, end.Z) - reach, maxZ = Math.Max(origin.Z, end.Z) + reach;
        double? best = null;
        var seen = new HashSet<int>();
        for (int cz = Cell(minZ); cz <= Cell(maxZ); cz++)
            for (int cx = Cell(minX); cx <= Cell(maxX); cx++)
            {
                if (!squares.TryGetValue((cx, cz), out var list)) continue;
                foreach (int t in list)
                {
                    if (!seen.Add(t)) continue;
                    var (a, b, c, _) = Triangles[t];
                    if (Tri.Ray(origin, dir, a, b, c) is { } hit && hit <= max && (best == null || hit < best)) best = hit;
                }
            }
        return best;
    }
}
