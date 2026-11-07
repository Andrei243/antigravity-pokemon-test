namespace OpenWorldBake;

/// <summary>
/// A first navigation (plan 32 · OW0; OW4 builds the real one): the walkable triangles of the collision mesh (under
/// 40°, with a capsule's clearance above), joined where they share an edge, and A* over their middles. It answers
/// the gate's questions: is the navigation layered (a cavern's floor and the hill over it are different triangles
/// over the same ground), and do a way through the tunnel and a way over the hill both exist.
/// </summary>
public sealed class NavProto
{
    readonly CollisionMesh mesh;
    public readonly List<int> Walkable = new();
    readonly Dictionary<int, int> nodeOf = new();
    readonly List<List<int>> links = new();
    readonly List<D3> middles = new();

    public int Components { get; }
    public int Unclear { get; }

    public NavProto(CollisionMesh mesh)
    {
        this.mesh = mesh;
        for (int t = 0; t < mesh.Triangles.Count; t++)
        {
            if (!mesh.Walkable(t)) continue;
            var (a, b, c, _) = mesh.Triangles[t];
            var middle = (a + b + c) * (1.0 / 3);
            // A capsule's clearance: nothing within its height straight above
            if (mesh.Raycast(middle + new D3(0, 0.05, 0), new D3(0, 1, 0), OpenBody.Height) is not null) { Unclear++; continue; }
            nodeOf[t] = Walkable.Count;
            Walkable.Add(t);
            middles.Add(middle);
            links.Add(new List<int>());
        }
        // Neighbours by shared edges: two walkable triangles with the same two corners (in millimetres)
        var byEdge = new Dictionary<(OpenPos, OpenPos), int>();
        for (int n = 0; n < Walkable.Count; n++)
        {
            var (a, b, c, _) = mesh.Triangles[Walkable[n]];
            var corners = new[] { OpenPos.Of(a), OpenPos.Of(b), OpenPos.Of(c) };
            for (int e = 0; e < 3; e++)
            {
                var p = corners[e]; var q = corners[(e + 1) % 3];
                var key = Compare(p, q) < 0 ? (p, q) : (q, p);
                if (byEdge.TryGetValue(key, out int other))
                {
                    links[n].Add(other);
                    links[other].Add(n);
                }
                else byEdge[key] = n;
            }
        }
        // How many separate pieces of walkable ground
        var seen = new bool[Walkable.Count];
        for (int s = 0; s < Walkable.Count; s++)
        {
            if (seen[s]) continue;
            Components++;
            int size = 0;
            var stack = new Stack<int>();
            stack.Push(s); seen[s] = true;
            while (stack.Count > 0)
            {
                size++;
                foreach (int m in links[stack.Pop()])
                    if (!seen[m]) { seen[m] = true; stack.Push(m); }
            }
            Largest = Math.Max(Largest, size);
        }
    }

    /// <summary>The triangles of the largest piece of walkable ground.</summary>
    public int Largest { get; }

    static int Compare(OpenPos p, OpenPos q) =>
        p.X != q.X ? p.X.CompareTo(q.X) : p.Y != q.Y ? p.Y.CompareTo(q.Y) : p.Z.CompareTo(q.Z);

    /// <summary>The walkable triangle under a point: the one a line down from it meets first.</summary>
    public int? NodeAt(D3 p)
    {
        foreach (var (y, t) in mesh.Column(p.X, p.Z))
            if (y <= p.Y + 0.5 && nodeOf.TryGetValue(t, out int n)) return n;
        return null;
    }

    /// <summary>Columns of a square of ground where two walkable floors lie more than 2 m apart, one over the other.</summary>
    public int LayeredColumns(double minX, double minZ, double maxX, double maxZ, double step = 1)
    {
        int layered = 0;
        for (double z = minZ; z <= maxZ; z += step)
            for (double x = minX; x <= maxX; x += step)
            {
                var floors = mesh.Column(x, z).Where(h => nodeOf.ContainsKey(h.Triangle)).Select(h => h.Y).ToList();
                if (floors.Count >= 2 && floors.Max() - floors.Min() > 2) layered++;
            }
        return layered;
    }

    /// <summary>A* over the triangles' middles; a node <paramref name="barred"/> says may not be used is passed by.</summary>
    public (List<D3> Path, double Length, int Visited)? Path(D3 from, D3 to, Func<D3, bool>? barred = null)
    {
        if (NodeAt(from) is not { } start || NodeAt(to) is not { } goal) return null;
        var g = new double[Walkable.Count];
        Array.Fill(g, double.MaxValue);
        var came = new int[Walkable.Count];
        Array.Fill(came, -1);
        var open = new PriorityQueue<int, (double, int)>();
        g[start] = 0;
        open.Enqueue(start, ((middles[start] - middles[goal]).Length, start));
        int visited = 0;
        var closed = new bool[Walkable.Count];
        while (open.Count > 0)
        {
            int n = open.Dequeue();
            if (closed[n]) continue;
            closed[n] = true;
            visited++;
            if (n == goal)
            {
                var path = new List<D3>();
                for (int m = goal; m >= 0; m = came[m]) path.Add(middles[m]);
                path.Reverse();
                return (path, g[goal], visited);
            }
            foreach (int m in links[n])
            {
                if (closed[m] || (barred != null && barred(middles[m]))) continue;
                double cost = g[n] + (middles[m] - middles[n]).Length;
                if (cost >= g[m]) continue;
                g[m] = cost;
                came[m] = n;
                open.Enqueue(m, (cost + (middles[m] - middles[goal]).Length, m));
            }
        }
        return null;
    }
}
