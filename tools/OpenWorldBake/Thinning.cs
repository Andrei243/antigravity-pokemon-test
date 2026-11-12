namespace OpenWorldBake;

/// <summary>
/// The far levels made by thinning the finest mesh (plan 32 · decision 16): edges collapsed one at a time onto one of
/// their ends, cheapest first by the error quadrics of the faces round them (Garland and Heckbert's), so every vertex
/// a level keeps is one of the finest level's, to the millimetre. A collapse is refused where it would change the
/// mesh's shape of holes (the link condition: a cave's shell keeps its hole however thin it is), turn a face over or
/// make it nothing, remove a vertex of the chunk's border rows (so a thinned chunk meets a neighbour of any level on
/// the very vertices the finest has there), or join two covers the level keeps apart. Single-threaded, every tie
/// broken by the vertices' numbers, so the same chunk thins the same on any machine.
/// </summary>
public static class Thinning
{
    /// <summary>A level's bound: how far its vertices may stand off the planes of the finest faces they replace (the root of the summed squares), and whether covers of one family may be joined.</summary>
    public readonly record struct Level(double MaxError, bool AcrossFamilies);

    /// <summary>The fewest a face's normal may keep of its old one, as a cosine (about 66°).</summary>
    const double MinTurn = 0.4;

    /// <summary>The smallest a face's area may become, in square metres.</summary>
    const double MinArea = 1e-6;

    /// <summary>
    /// The finest net thinned to each level in turn (each level from the one before), with each level's vertices
    /// numbered by the finest's they are (<c>Source</c>).
    /// </summary>
    public static List<(RawChunk Chunk, int[] Source)> Thin(RawChunk fine, Cover[] covers, IReadOnlyList<Level> levels)
    {
        var thinner = new Thinner(fine, covers);
        var result = new List<(RawChunk, int[])>();
        foreach (var level in levels)
        {
            thinner.Run(level);
            result.Add(thinner.Snapshot());
        }
        return result;
    }

    sealed class Thinner
    {
        readonly RawChunk fine;
        readonly Cover[] covers;
        readonly D3[] pos;
        readonly bool[] locked, alive;
        readonly double[] q;                     // ten a vertex: the quadric's upper triangle
        readonly int[] tri;                      // three a triangle
        readonly bool[] apron, triAlive;
        readonly List<int>[] around;             // the live (and some dead) triangles round each vertex
        readonly int groundCount;

        public Thinner(RawChunk fine, Cover[] covers)
        {
            this.fine = fine;
            this.covers = covers;
            int n = fine.VertexCount;
            pos = new D3[n];
            for (int v = 0; v < n; v++) pos[v] = fine.Vertex(v);
            alive = Enumerable.Repeat(true, n).ToArray();
            locked = new bool[n];
            q = new double[n * 10];
            groundCount = fine.Ground.Count / 3;
            int count = groundCount + fine.Apron.Count / 3;
            tri = new int[count * 3];
            apron = new bool[count];
            triAlive = Enumerable.Repeat(true, count).ToArray();
            for (int t = 0; t < groundCount; t++) for (int e = 0; e < 3; e++) tri[t * 3 + e] = fine.Ground[t * 3 + e];
            for (int t = groundCount; t < count; t++) { apron[t] = true; for (int e = 0; e < 3; e++) tri[t * 3 + e] = fine.Apron[(t - groundCount) * 3 + e]; }
            around = new List<int>[n];
            for (int v = 0; v < n; v++) around[v] = new List<int>(6);
            for (int t = 0; t < count; t++) for (int e = 0; e < 3; e++) around[tri[t * 3 + e]].Add(t);

            // The border's rows, and any vertex on an edge that isn't two faces' (an edge of the net's own border, or
            // where a face too small to keep was left out), are kept
            for (int v = 0; v < n; v++) locked[v] = fine.OnTheBorder(v);
            var edges = new Dictionary<long, int>();
            for (int t = 0; t < count; t++)
                for (int e = 0; e < 3; e++)
                {
                    long key = Key(tri[t * 3 + e], tri[t * 3 + (e + 1) % 3]);
                    edges[key] = edges.GetValueOrDefault(key) + 1;
                }
            foreach (var (key, faces) in edges)
                if (faces != 2) { locked[(int)(key >> 32)] = true; locked[(int)(key & 0xFFFFFFFF)] = true; }

            // Each face's plane, to each of its vertices
            // (one span for every face: a stackalloc in the loop is never given back until the constructor ends, and a
            // chunk of many faces ran the stack out)
            Span<double> plane = stackalloc double[10];
            for (int t = 0; t < count; t++)
            {
                var a = pos[tri[t * 3]]; var b = pos[tri[t * 3 + 1]]; var c = pos[tri[t * 3 + 2]];
                var normal = D3.Cross(b - a, c - a);
                double length = normal.Length;
                if (length < 1e-12) continue;
                normal = normal * (1 / length);
                double d = -D3.Dot(normal, a);
                plane[0] = normal.X * normal.X; plane[1] = normal.X * normal.Y; plane[2] = normal.X * normal.Z; plane[3] = normal.X * d;
                plane[4] = normal.Y * normal.Y; plane[5] = normal.Y * normal.Z; plane[6] = normal.Y * d;
                plane[7] = normal.Z * normal.Z; plane[8] = normal.Z * d; plane[9] = d * d;
                for (int e = 0; e < 3; e++)
                {
                    int v = tri[t * 3 + e];
                    for (int i = 0; i < 10; i++) q[v * 10 + i] += plane[i];
                }
            }
        }

        static long Key(int a, int b) => a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a;

        double Error(int a, int b, D3 p)
        {
            Span<double> s = stackalloc double[10];
            for (int i = 0; i < 10; i++) s[i] = q[a * 10 + i] + q[b * 10 + i];
            double x = p.X, y = p.Y, z = p.Z;
            double e = s[0] * x * x + 2 * s[1] * x * y + 2 * s[2] * x * z + 2 * s[3] * x
                + s[4] * y * y + 2 * s[5] * y * z + 2 * s[6] * y
                + s[7] * z * z + 2 * s[8] * z + s[9];
            return Math.Max(e, 0);
        }

        bool Joins(Level level, int a, int b) =>
            covers[a] == covers[b] || level.AcrossFamilies && PokemonPlatinumEngine.OpenWorld.OpenCovers.FamilyOf(covers[a]) == PokemonPlatinumEngine.OpenWorld.OpenCovers.FamilyOf(covers[b]);

        // The cheaper way to collapse an edge (the end removed, the end kept), or none
        (double Cost, int Remove, int Keep)? Best(Level level, int a, int b)
        {
            if (!Joins(level, a, b)) return null;
            double? toB = locked[a] ? null : Error(a, b, pos[b]);
            double? toA = locked[b] ? null : Error(a, b, pos[a]);
            if (toB is { } cb && (toA is not { } ca0 || cb <= ca0)) return (cb, a, b);
            if (toA is { } ca) return (ca, b, a);
            return null;
        }

        IEnumerable<int> Neighbours(int v)
        {
            var seen = new SortedSet<int>();
            foreach (int t in around[v])
                if (triAlive[t])
                    for (int e = 0; e < 3; e++) if (tri[t * 3 + e] != v) seen.Add(tri[t * 3 + e]);
            return seen;
        }

        bool Allowed(int remove, int keep)
        {
            if (!alive[remove] || !alive[keep] || locked[remove]) return false;
            // The link condition: the two ends share no neighbour but the far corners of the two faces on the edge
            var shared = new List<int>();
            foreach (int t in around[remove])
                if (triAlive[t] && Has(t, keep)) shared.Add(t);
            if (shared.Count != 2) return false;
            var corners = new HashSet<int>();
            foreach (int t in shared) for (int e = 0; e < 3; e++) { int v = tri[t * 3 + e]; if (v != remove && v != keep) corners.Add(v); }
            var keepNeighbours = Neighbours(keep).ToHashSet();
            foreach (int v in Neighbours(remove))
                if (v != keep && keepNeighbours.Contains(v) && !corners.Contains(v)) return false;
            // No face turned over or made nothing
            foreach (int t in around[remove])
            {
                if (!triAlive[t] || Has(t, keep)) continue;
                var a = pos[tri[t * 3]]; var b = pos[tri[t * 3 + 1]]; var c = pos[tri[t * 3 + 2]];
                var before = D3.Cross(b - a, c - a);
                D3 Moved(int e) => tri[t * 3 + e] == remove ? pos[keep] : pos[tri[t * 3 + e]];
                var after = D3.Cross(Moved(1) - Moved(0), Moved(2) - Moved(0));
                if (after.Length * 0.5 < MinArea) return false;
                if (D3.Dot(before.Normalized(), after.Normalized()) < MinTurn) return false;
            }
            return true;
        }

        bool Has(int t, int v) => tri[t * 3] == v || tri[t * 3 + 1] == v || tri[t * 3 + 2] == v;

        void Collapse(int remove, int keep)
        {
            foreach (int t in around[remove])
            {
                if (!triAlive[t]) continue;
                if (Has(t, keep)) { triAlive[t] = false; continue; }
                for (int e = 0; e < 3; e++) if (tri[t * 3 + e] == remove) tri[t * 3 + e] = keep;
                around[keep].Add(t);
            }
            around[remove].Clear();
            alive[remove] = false;
            for (int i = 0; i < 10; i++) q[keep * 10 + i] += q[remove * 10 + i];
            around[keep].RemoveAll(t => !triAlive[t]);
        }

        sealed class ByCost : IComparer<(double Cost, int Remove, int Keep)>
        {
            public static readonly ByCost Instance = new();
            public int Compare((double Cost, int Remove, int Keep) x, (double Cost, int Remove, int Keep) y)
            {
                int c = x.Cost.CompareTo(y.Cost);
                if (c != 0) return c;
                c = x.Remove.CompareTo(y.Remove);
                return c != 0 ? c : x.Keep.CompareTo(y.Keep);
            }
        }

        /// <summary>Collapses edges, cheapest first, until none left costs no more than the level allows.</summary>
        public void Run(Level level)
        {
            double limit = level.MaxError * level.MaxError;
            var queue = new SortedSet<(double Cost, int Remove, int Keep)>(ByCost.Instance);
            var entries = new Dictionary<long, (double Cost, int Remove, int Keep)>();
            void Consider(int a, int b)
            {
                long key = Key(a, b);
                if (entries.Remove(key, out var old)) queue.Remove(old);
                if (Best(level, a, b) is { } best && best.Cost <= limit) { queue.Add(best); entries[key] = best; }
            }
            for (int t = 0; t < triAlive.Length; t++)
            {
                if (!triAlive[t]) continue;
                for (int e = 0; e < 3; e++)
                {
                    int a = tri[t * 3 + e], b = tri[t * 3 + (e + 1) % 3];
                    if (a < b) Consider(a, b);
                }
            }
            while (queue.Count > 0)
            {
                var top = queue.Min;
                queue.Remove(top);
                entries.Remove(Key(top.Remove, top.Keep));
                if (!Allowed(top.Remove, top.Keep)) continue;
                Collapse(top.Remove, top.Keep);
                foreach (int v in Neighbours(top.Keep)) Consider(top.Keep, v);
            }
        }

        /// <summary>The mesh as it stands: the live vertices in the finest's order, the live faces in theirs, the ground's before the apron's.</summary>
        public (RawChunk, int[]) Snapshot()
        {
            var number = new int[pos.Length];
            Array.Fill(number, -1);
            var source = new List<int>();
            var positions = new List<int>();
            var cells = new List<(int, int)>();
            for (int v = 0; v < pos.Length; v++)
            {
                if (!alive[v]) continue;
                number[v] = source.Count;
                source.Add(v);
                for (int i = 0; i < 3; i++) positions.Add(fine.Positions[v * 3 + i]);
                cells.Add(fine.Cells[v]);
            }
            var ground = new List<int>();
            var apronList = new List<int>();
            for (int t = 0; t < triAlive.Length; t++)
            {
                if (!triAlive[t]) continue;
                var into = apron[t] ? apronList : ground;
                for (int e = 0; e < 3; e++) into.Add(number[tri[t * 3 + e]]);
            }
            return (new RawChunk
            {
                ChunkX = fine.ChunkX, ChunkZ = fine.ChunkZ, Cell = fine.Cell, N = fine.N,
                Positions = positions, Cells = cells, Ground = ground, Apron = apronList
            }, source.ToArray());
        }
    }
}
