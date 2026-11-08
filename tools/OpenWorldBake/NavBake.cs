using PokemonPlatinumEngine.OpenWorld;
using Body = PokemonPlatinumEngine.OpenWorld.OpenBody;

namespace OpenWorldBake;

/// <summary>What a bake of the navigation mesh found, as counts (plan 32 · OW4): what the baker prints.</summary>
public sealed class NavReport
{
    public int Triangles, Walkable, Steps, Steep, Columns, Spans, Swum, Unclear, Walled, Polygons, Portals, StepLinks, DropLinks, SwimLinks, Pieces, Largest;

    public override string ToString() =>
        $"{Triangles} triangles of level 0 and the boxes' tops: {Walkable} walkable ({Steps} of them a steep band no higher than a step), {Steep} too steep or facing down; "
        + $"{Columns} columns of {NavBake.Cell} m with {Spans} places a body stands ({Swum} of them swum), {Unclear} under something lower than a body, {Walled} nearer a wall than a body's radius; "
        + $"{Polygons} polygons, {Portals} shared edges each way, {StepLinks} steps, {DropLinks} drops and {SwimLinks} swims off the mesh; "
        + $"{Pieces} pieces joined by walking and drops, the largest {Largest} polygons";
}

/// <summary>
/// The navigation mesh's bake (plan 32, "The navigation mesh"; OW4), from level 0's own ground of every chunk and the
/// tops of the map's boxes, as Recast bakes one from a collision mesh:
/// <list type="number">
/// <item>Each triangle is <b>walked</b> where the capsule stands on it without sliding (under 40°, or a steep band no
/// higher than a step, as <see cref="Body"/>'s <c>TakeFace</c> takes it), and <b>passed</b> where it is walked or lies
/// under water deeper than the body swims.</item>
/// <item>On the world's own grid of <see cref="Cell"/> columns, every walked triangle (or one under water that is swum)
/// that a column's middle meets is a <b>span</b>, a place a body stands, where it has its clearance there: nothing over
/// the feet lower than its height, nothing nearer its three spheres than its radius but walkable ground, as its pushes
/// take it. A column may hold several (a cave's floor under a hill's top): the mesh is layered.</item>
/// <item>A span is joined to the span of the next column east and south whose triangle is come to from its own through
/// passed triangles nearby (the surface itself, never a guess by heights).</item>
/// <item>The joined spans are covered by rectangles of up to <see cref="MostCells"/> columns a side within one chunk, all
/// swum or none, row by row from the north-west: each a polygon, exactly convex on the ground; the edges two share are
/// links each way.</item>
/// <item>From each span's side that joins nothing, probes past it look for ground a step up or any drop down, open at the
/// knees and the head and with nothing over the landing: a link off the mesh (a kerb stepped, a ledge dropped, a way
/// down only).</item>
/// </list>
/// Single-threaded, every loop in the grid's order or the collision mesh's keys, so the same chunks bake the same bytes
/// on any machine.
/// </summary>
public static class NavBake
{
    /// <summary>A column's side, in metres: level 0's own cell.</summary>
    public const double Cell = 0.5;

    /// <summary>A polygon's side at most, in columns (4 m): what bounds how far its fan's heights stand off its ground.</summary>
    public const int MostCells = 8;

    /// <summary>How far past a span's side the probes look for ground, in metres.</summary>
    public static readonly double[] Probes = { 0.6, 1.0, 1.6, 2.5, 4.0 };

    /// <summary>The longest drop a link is made for, in metres.</summary>
    public const double MostDrop = 20;

    /// <summary>How many triangles a join of two spans may pass through.</summary>
    private const int MostHops = 8;

    private const int CellMm = 500;
    private const int ChunkCells = OpenMap.ChunkSide / CellMm;

    // The collision mesh's keys, as OpenGround makes them (a chunk's triangle, then a box's after every chunk's)
    private const long BoxKeys = 1L << 62;
    private static long KeyOf(int chunkX, int chunkZ, int t) =>
        ((((long)(chunkZ + (1 << 18)) << 19) | (long)(chunkX + (1 << 18))) << 24) | (long)t;

    private sealed class Tri
    {
        public int Id, A, B, C;
        public OpenVec Pa, Pb, Pc, Normal, Centre;
        public bool Walked, Passed, FacesUp;
        public double MinX, MinZ, MaxX, MaxZ;
        public readonly List<int> Next = new();

        /// <summary>The height of the triangle's plane over x, z.</summary>
        public double PlaneAt(double x, double z) => Pa.Y - (Normal.X * (x - Pa.X) + Normal.Z * (z - Pa.Z)) / Normal.Y;
    }

    private sealed class Span
    {
        public int Id, I, K, Tri, Polygon = -1, East = -1, South = -1, West = -1, North = -1;
        public double Y, Feet;
        public bool Swim;
        public int Surface = BakedChunk.NoWater;
    }

    /// <summary>
    /// The mesh of a region's level-0 chunks (each <see cref="BakedChunk.Level"/> 0), with the map's boxes and its water
    /// laid by hand. <paramref name="report"/>, if given, is filled with what was found.
    /// </summary>
    public static OpenNavMesh Build(IEnumerable<BakedChunk> level0, IEnumerable<OpenBox>? boxes = null, IEnumerable<OpenWaterBox>? waters = null,
        NavReport? report = null)
    {
        report ??= new NavReport();
        var chunks = level0.OrderBy(c => c.ChunkZ).ThenBy(c => c.ChunkX).ToList();
        var byChunk = new Dictionary<(int, int), BakedChunk>();
        foreach (var chunk in chunks)
        {
            if (chunk.Level != 0) throw new ArgumentException($"chunk {chunk.ChunkX},{chunk.ChunkZ} is level {chunk.Level}: the navigation is baked from level 0");
            if (!byChunk.TryAdd((chunk.ChunkX, chunk.ChunkZ), chunk)) throw new ArgumentException($"chunk {chunk.ChunkX},{chunk.ChunkZ} given twice");
        }
        var boxList = boxes?.ToList() ?? new List<OpenBox>();
        var ground = new OpenGround((x, z) => byChunk.GetValueOrDefault((x, z)), boxList, waters);

        // ---------------------------------------------------------------- the triangles, and which are walked and passed
        var vertexId = new Dictionary<OpenPos, int>();
        int V(OpenPos p)
        {
            if (vertexId.TryGetValue(p, out int id)) return id;
            vertexId[p] = vertexId.Count;
            return vertexId.Count - 1;
        }
        var tris = new List<Tri>();
        var byKey = new Dictionary<long, int>();
        void AddTri(long key, OpenPos a, OpenPos b, OpenPos c)
        {
            var va = OpenVec.Of(a); var vb = OpenVec.Of(b); var vc = OpenVec.Of(c);
            var t = new Tri
            {
                Id = tris.Count, A = V(a), B = V(b), C = V(c), Pa = va, Pb = vb, Pc = vc,
                Normal = OpenVec.Cross(vb - va, vc - va).Normalized(), Centre = (va + vb + vc) * (1.0 / 3),
                MinX = Math.Min(va.X, Math.Min(vb.X, vc.X)), MaxX = Math.Max(va.X, Math.Max(vb.X, vc.X)),
                MinZ = Math.Min(va.Z, Math.Min(vb.Z, vc.Z)), MaxZ = Math.Max(va.Z, Math.Max(vb.Z, vc.Z))
            };
            byKey[key] = t.Id;
            tris.Add(t);
        }
        foreach (var chunk in chunks)
            for (int t = 0; t < chunk.GroundTriangles; t++)
                AddTri(KeyOf(chunk.ChunkX, chunk.ChunkZ, t),
                    chunk.Vertex(chunk.Indices[t * 3]), chunk.Vertex(chunk.Indices[t * 3 + 1]), chunk.Vertex(chunk.Indices[t * 3 + 2]));
        for (int i = 0; i < boxList.Count; i++)
        {
            // A box's top, as OpenGround makes it: its first two triangles
            var lo = boxList[i].Min; var hi = boxList[i].Max;
            OpenPos P(int k) => new((k & 1) == 0 ? lo.X : hi.X, (k & 2) == 0 ? lo.Y : hi.Y, (k & 4) == 0 ? lo.Z : hi.Z);
            long key = BoxKeys + 12L * i;
            AddTri(key, P(2), P(6), P(7));
            AddTri(key + 1, P(2), P(7), P(3));
        }
        report.Triangles = tris.Count;
        foreach (var t in tris)
        {
            t.FacesUp = t.Normal.Y >= OpenGround.FacesUpY;
            if (!t.FacesUp) { report.Steep++; continue; }
            if (t.Normal.Y >= OpenGround.WalkableY) t.Walked = true;
            else if (StepBand(ground, t.Normal, t.Centre)) { t.Walked = true; report.Steps++; }
            else report.Steep++;
            if (t.Walked) report.Walkable++;
            t.Passed = t.Walked || ground.WaterAt(t.Centre.X, t.Centre.Z) is { } surface && surface - t.Centre.Y > Body.SwimDepth;
        }

        // Triangles that share an edge, both passed
        var edges = new Dictionary<(int, int), List<int>>();
        static (int, int) Edge(int a, int b) => a < b ? (a, b) : (b, a);
        foreach (var t in tris)
        {
            if (!t.Passed) continue;
            foreach (var e in new[] { Edge(t.A, t.B), Edge(t.B, t.C), Edge(t.C, t.A) })
            {
                if (!edges.TryGetValue(e, out var list)) edges[e] = list = new List<int>();
                foreach (int other in list) { t.Next.Add(other); tris[other].Next.Add(t.Id); }
                list.Add(t.Id);
            }
        }
        foreach (var t in tris) t.Next.Sort();

        // ---------------------------------------------------------------- the spans, column by column
        int i0 = chunks.Count == 0 ? 0 : chunks.Min(c => c.ChunkX) * ChunkCells, i1 = chunks.Count == 0 ? -1 : (chunks.Max(c => c.ChunkX) + 1) * ChunkCells - 1;
        int k0 = chunks.Count == 0 ? 0 : chunks.Min(c => c.ChunkZ) * ChunkCells, k1 = chunks.Count == 0 ? -1 : (chunks.Max(c => c.ChunkZ) + 1) * ChunkCells - 1;
        var spans = new List<Span>();
        var cells = new Dictionary<(int, int), List<int>>();
        var near = new List<int>();
        double[] spheres = { Body.StepUp + Body.Radius, (Body.StepUp + Body.Height) / 2, Body.Height - Body.Radius };
        for (int k = k0; k <= k1; k++)
            for (int i = i0; i <= i1; i++)
            {
                if (!byChunk.ContainsKey((FloorDiv(i, ChunkCells), FloorDiv(k, ChunkCells)))) continue;
                report.Columns++;
                double x = (i + 0.5) * Cell, z = (k + 0.5) * Cell;
                var column = ground.Column(x, z);
                double? water = ground.WaterAt(x, z);
                foreach (var (y, index) in column)
                {
                    if (!byKey.TryGetValue(ground[index].Key, out int id)) continue;
                    var t = tris[id];
                    if (!t.FacesUp) continue;
                    bool swim = water is { } w && w - y > Body.SwimDepth;
                    if (!swim && !t.Walked) continue;
                    double feet = swim ? water!.Value - Body.SwimDepth : y;
                    if (cells.TryGetValue((i, k), out var here) && here.Any(s => Math.Abs(spans[s].Feet - feet) < 0.1)) continue;
                    // Its clearance: nothing over the feet lower than the body, no wall nearer than its radius
                    bool covered = false;
                    foreach (var (yy, _) in column)
                        if (yy > feet + 0.02 && yy <= feet + Body.Height) { covered = true; break; }
                    if (covered) { report.Unclear++; continue; }
                    // The whole column is the body's to stand anywhere in: clear by its radius and half a column, and
                    // walked ground (or water swum) under each of its corners, so a path along a polygon's edge never
                    // grazes a wall or stands on a face that slides
                    if (Walled(ground, near, spheres, x, feet, z, Body.Radius + Cell / 2)) { report.Walled++; continue; }
                    if (!swim && !CornersWalked(ground, tris, byKey, x, z, feet)) { report.Walled++; continue; }
                    var span = new Span { Id = spans.Count, I = i, K = k, Tri = id, Y = y, Feet = feet, Swim = swim, Surface = swim ? OpenPos.Millimetres(water!.Value) : BakedChunk.NoWater };
                    spans.Add(span);
                    if (here == null) cells[(i, k)] = here = new List<int>();
                    here.Add(span.Id);
                    report.Spans++;
                    if (swim) report.Swum++;
                }
            }

        // ---------------------------------------------------------------- spans joined east and south through the surface
        var visited = new Dictionary<int, int>();
        var frontier = new Queue<int>();
        bool Joined(Span a, Span b)
        {
            if (Math.Abs(a.Feet - b.Feet) > 0.75) return false;
            if (a.Tri == b.Tri) return true;
            double minX = Math.Min(a.I, b.I) * Cell - Cell, maxX = (Math.Max(a.I, b.I) + 1) * Cell + Cell;
            double minZ = Math.Min(a.K, b.K) * Cell - Cell, maxZ = (Math.Max(a.K, b.K) + 1) * Cell + Cell;
            double lowY = Math.Min(a.Y, b.Y) - 0.75, highY = Math.Max(a.Y, b.Y) + 0.75;
            visited.Clear(); frontier.Clear();
            visited[a.Tri] = 0; frontier.Enqueue(a.Tri);
            while (frontier.Count > 0)
            {
                int t = frontier.Dequeue();
                int hops = visited[t];
                if (hops >= MostHops) continue;
                foreach (int n in tris[t].Next)
                {
                    if (visited.ContainsKey(n)) continue;
                    var nt = tris[n];
                    if (nt.MaxX < minX || nt.MinX > maxX || nt.MaxZ < minZ || nt.MinZ > maxZ || nt.Centre.Y < lowY || nt.Centre.Y > highY) continue;
                    if (n == b.Tri) return true;
                    visited[n] = hops + 1;
                    frontier.Enqueue(n);
                }
            }
            return false;
        }
        foreach (var s in spans)
        {
            foreach (var (di, dk) in new[] { (1, 0), (0, 1) })
            {
                if (!cells.TryGetValue((s.I + di, s.K + dk), out var there)) continue;
                int best = -1;
                double nearest = double.MaxValue;
                foreach (int o in there)
                {
                    var t = spans[o];
                    if ((di == 1 ? t.West : t.North) >= 0) continue;
                    double d = Math.Abs(t.Feet - s.Feet);
                    if (d < nearest && Joined(s, t)) { nearest = d; best = o; }
                }
                if (best < 0) continue;
                if (di == 1) { s.East = best; spans[best].West = s.Id; }
                else { s.South = best; spans[best].North = s.Id; }
            }
        }

        // ---------------------------------------------------------------- rectangles of joined spans: the polygons
        var rects = new List<(int I, int K, int W, int H, Span[,] Spans)>();
        for (int k = k0; k <= k1; k++)
            for (int i = i0; i <= i1; i++)
            {
                if (!cells.TryGetValue((i, k), out var here)) continue;
                foreach (int start in here)
                {
                    var s = spans[start];
                    if (s.Polygon >= 0) continue;
                    int cx = FloorDiv(i, ChunkCells), cz = FloorDiv(k, ChunkCells);
                    bool Fits(Span t, int ii, int kk) => t.Polygon < 0 && t.Swim == s.Swim && FloorDiv(ii, ChunkCells) == cx && FloorDiv(kk, ChunkCells) == cz;
                    var row = new List<Span> { s };
                    while (row.Count < MostCells && row[^1].East >= 0 && Fits(spans[row[^1].East], i + row.Count, k))
                    {
                        row.Add(spans[row[^1].East]);
                        if (!Flat(new List<List<Span>> { row })) { row.RemoveAt(row.Count - 1); break; }
                    }
                    var rowsOf = new List<List<Span>> { row };
                    while (rowsOf.Count < MostCells)
                    {
                        var above = rowsOf[^1];
                        var next = new List<Span>();
                        foreach (var a in above)
                        {
                            if (a.South < 0) break;
                            var b = spans[a.South];
                            if (!Fits(b, a.I, k + rowsOf.Count) || next.Count > 0 && next[^1].East != b.Id) break;
                            next.Add(b);
                        }
                        if (next.Count != above.Count) break;
                        rowsOf.Add(next);
                        if (!Flat(rowsOf)) { rowsOf.RemoveAt(rowsOf.Count - 1); break; }
                    }
                    int w = row.Count, h = rowsOf.Count;
                    var grid = new Span[w, h];
                    for (int r = 0; r < h; r++)
                        for (int c = 0; c < w; c++)
                        {
                            grid[c, r] = rowsOf[r][c];
                            grid[c, r].Polygon = rects.Count;
                        }
                    rects.Add((i, k, w, h, grid));
                }
            }
        report.Polygons = rects.Count;

        var vertexOf = new Dictionary<OpenPos, int>();
        var vertices = new List<OpenPos>();
        int Vertex(OpenPos p)
        {
            if (vertexOf.TryGetValue(p, out int id)) return id;
            vertexOf[p] = vertices.Count;
            vertices.Add(p);
            return vertices.Count - 1;
        }
        // A corner's height: the plane of the span in the corner's column, held within a step of the span's own height
        double GroundAt(Span s, double x, double z) => Math.Clamp(tris[s.Tri].PlaneAt(x, z), s.Y - Body.StepUp, s.Y + Body.StepUp);
        var polygons = new List<OpenNavPolygon>();
        foreach (var (ri, rk, w, h, grid) in rects)
        {
            double x0 = ri * Cell, x1 = (ri + w) * Cell, z0 = rk * Cell, z1 = (rk + h) * Cell;
            var corners = new[]
            {
                Vertex(OpenPos.FromMetres(x0, GroundAt(grid[0, 0], x0, z0), z0)),
                Vertex(OpenPos.FromMetres(x1, GroundAt(grid[w - 1, 0], x1, z0), z0)),
                Vertex(OpenPos.FromMetres(x1, GroundAt(grid[w - 1, h - 1], x1, z1), z1)),
                Vertex(OpenPos.FromMetres(x0, GroundAt(grid[0, h - 1], x0, z1), z1))
            };
            double hx = (x0 + x1) / 2, hz = (z0 + z1) / 2;
            var middle = grid[Math.Min(w / 2, w - 1), Math.Min(h / 2, h - 1)];
            var first = grid[0, 0];
            polygons.Add(new OpenNavPolygon
            {
                Id = polygons.Count, ChunkX = FloorDiv(ri, ChunkCells), ChunkZ = FloorDiv(rk, ChunkCells), Corners = corners,
                Hub = OpenPos.FromMetres(hx, GroundAt(middle, hx, hz), hz), Swim = first.Swim,
                Surface = first.Swim ? grid.Cast<Span>().Max(s => s.Surface) : BakedChunk.NoWater
            });
        }

        // ---------------------------------------------------------------- links across the sides two polygons share
        var shared = new SortedDictionary<(int, int), (double Lo, double Hi, double YLo, double YHi, bool AlongZ)>();
        void Share(Span a, Span b, bool alongZ)
        {
            int p = a.Polygon, q = b.Polygon;
            if (p == q) return;
            // The cell side between them: x fixed and z from lo to hi, or z fixed and x from lo to hi
            double lo = alongZ ? a.K * Cell : a.I * Cell, hi = lo + Cell, y = (a.Y + b.Y) / 2;
            var key = p < q ? (p, q) : (q, p);
            if (shared.TryGetValue(key, out var s))
                shared[key] = (Math.Min(s.Lo, lo), Math.Max(s.Hi, hi), lo < s.Lo ? y : s.YLo, hi > s.Hi ? y : s.YHi, alongZ);
            else shared[key] = (lo, hi, y, y, alongZ);
        }
        foreach (var s in spans)
        {
            if (s.East >= 0) Share(s, spans[s.East], alongZ: true);
            if (s.South >= 0) Share(s, spans[s.South], alongZ: false);
        }
        var links = new List<OpenNavLink>();
        var joinedPolygons = new HashSet<(int, int)>();
        foreach (var ((p, q), (lo, hi, ylo, yhi, alongZ)) in shared)
        {
            // Where the side lies: east of p's rectangle or south of it (either way round: p may be the east or south one)
            var rp = rects[p]; var rq = rects[q];
            double fixedAt = alongZ ? Math.Max(rp.I, rq.I) * Cell : Math.Max(rp.K, rq.K) * Cell;
            var a = alongZ ? OpenPos.FromMetres(fixedAt, ylo, lo) : OpenPos.FromMetres(lo, ylo, fixedAt);
            var b = alongZ ? OpenPos.FromMetres(fixedAt, yhi, hi) : OpenPos.FromMetres(hi, yhi, fixedAt);
            links.Add(new OpenNavLink(p, q, a, b, polygons[q].Swim ? OpenNavFlags.Swim : OpenNavFlags.Walk, false, default));
            links.Add(new OpenNavLink(q, p, a, b, polygons[p].Swim ? OpenNavFlags.Swim : OpenNavFlags.Walk, false, default));
            joinedPolygons.Add((p, q)); joinedPolygons.Add((q, p));
            report.Portals++;
        }

        // ---------------------------------------------------------------- links off the mesh: steps and drops
        var off = new SortedDictionary<(int, int), (double Length, OpenNavLink Link)>();
        foreach (var s in spans)
        {
            foreach (var (dx, dz, joined) in new[] { (1, 0, s.East), (-1, 0, s.West), (0, 1, s.South), (0, -1, s.North) })
            {
                if (joined >= 0) continue;
                double mx = (s.I + 0.5 + dx * 0.5) * Cell, mz = (s.K + 0.5 + dz * 0.5) * Cell;
                foreach (double reach in Probes)
                {
                    double px = mx + dx * reach, pz = mz + dz * reach;
                    if (Landing(ground, tris, byKey, s.Feet, px, pz) is not { } landFeet) continue;
                    // The span the landing is in: its column's, at the landing's height
                    int pi = (int)Math.Floor(px / Cell), pk = (int)Math.Floor(pz / Cell);
                    if (!cells.TryGetValue((pi, pk), out var there)) continue;
                    int q = -1;
                    double nearest = 0.3;
                    foreach (int o in there)
                        if (Math.Abs(spans[o].Feet - landFeet) < nearest) { nearest = Math.Abs(spans[o].Feet - landFeet); q = spans[o].Polygon; }
                    if (q < 0 || q == s.Polygon) continue;
                    double dy = landFeet - s.Feet;
                    bool drop = dy < -Body.SnapDown;
                    if (!Open(ground, mx, s.Feet, mz, dx, dz, reach, landFeet, drop)) continue;
                    var flags = (drop ? OpenNavFlags.Drop : OpenNavFlags.Walk) | (polygons[q].Swim ? OpenNavFlags.Swim : OpenNavFlags.None);
                    var from = OpenPos.FromMetres(mx, s.Feet, mz);
                    var link = new OpenNavLink(s.Polygon, q, from, from, flags, true, OpenPos.FromMetres(px, landFeet, pz));
                    double length = Math.Sqrt(reach * reach + dy * dy);
                    if (!joinedPolygons.Contains((s.Polygon, q)) && (!off.TryGetValue((s.Polygon, q), out var best) || length < best.Length - 1e-9))
                        off[(s.Polygon, q)] = (length, link);
                    break;
                }
            }
        }
        foreach (var (_, (_, link)) in off)
        {
            links.Add(link);
            if ((link.Flags & OpenNavFlags.Drop) != 0) report.DropLinks++;
            else if ((link.Flags & OpenNavFlags.Swim) != 0) report.SwimLinks++;
            else report.StepLinks++;
        }
        links.Sort((x, y) => x.From != y.From ? x.From.CompareTo(y.From) : x.To != y.To ? x.To.CompareTo(y.To) : x.OffMesh.CompareTo(y.OffMesh));

        var mesh = new OpenNavMesh(vertices, polygons, links);
        (report.Pieces, report.Largest) = Pieces(mesh);
        return mesh;
    }

    private static int FloorDiv(int value, int by) => (int)Math.Floor(value / (double)by);

    /// <summary>How far a span may stand off the surface drawn between its rectangle's four corner spans, in metres.</summary>
    public const double MostBend = 0.1;

    /// <summary>
    /// Whether a rectangle of spans (rows north to south, each west to east) is flat enough to be one polygon: every
    /// span within <see cref="MostBend"/> of the surface between its corners' (bilinear), so a polygon's fan from its
    /// hub to its corners keeps to its ground on a ridge or in a hollow as on a slope.
    /// </summary>
    private static bool Flat(List<List<Span>> rows)
    {
        int h = rows.Count, w = rows[0].Count;
        double y00 = rows[0][0].Y, y10 = rows[0][w - 1].Y, y01 = rows[h - 1][0].Y, y11 = rows[h - 1][w - 1].Y;
        for (int r = 0; r < h; r++)
            for (int c = 0; c < w; c++)
            {
                double u = w > 1 ? c / (double)(w - 1) : 0, v = h > 1 ? r / (double)(h - 1) : 0;
                double expected = (y00 * (1 - u) + y10 * u) * (1 - v) + (y01 * (1 - u) + y11 * u) * v;
                if (Math.Abs(rows[r][c].Y - expected) > MostBend) return false;
            }
        return true;
    }

    /// <summary>A steep face stood on as ground, as the capsule takes it: walkable ground within a step over the feet a body's radius up the face.</summary>
    private static bool StepBand(OpenGround ground, OpenVec n, OpenVec c)
    {
        double h = Math.Sqrt(n.X * n.X + n.Z * n.Z);
        if (h < 1e-9) return true;
        double downX = n.X / h, downZ = n.Z / h;
        return ground.Footing(c.X - downX * Body.Radius, c.Z - downZ * Body.Radius, c.Y + Body.StepUp, c.Y - 0.05, walkableOnly: true) != null;
    }

    /// <summary>Whether a body with its feet at x, y, z has a face that isn't walkable ground nearer one of its three spheres than its radius.</summary>
    private static bool Walled(OpenGround ground, List<int> near, double[] spheres, double x, double y, double z, double radius)
    {
        ground.Near(x, z, radius + 0.1, near);
        foreach (double above in spheres)
        {
            var centre = new OpenVec(x, y + above, z);
            foreach (int i in near)
            {
                var t = ground[i];
                if (t.Walkable) continue;
                if (centre.X + radius < t.MinX || centre.X - radius > t.MaxX || centre.Z + radius < t.MinZ || centre.Z - radius > t.MaxZ
                    || centre.Y + Body.Radius < t.MinY || centre.Y - Body.Radius > t.MaxY) continue;
                var away = centre - OpenTriangles.ClosestPoint(centre, t.A, t.B, t.C);
                // Measured along the ground: a sphere's radius up and down, the radius asked for across
                double across = away.X * away.X + away.Z * away.Z;
                if (across < (radius - 0.001) * (radius - 0.001) && Math.Abs(away.Y) < Body.Radius) return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Whether walked ground lies under the column round x, z, at its corners, the middles of its sides and its own
    /// middle, within a step over the feet or a snap under them: a hole in the land where a carve's shell is thinner
    /// than a cell (OW2's finding) is never stood over.
    /// </summary>
    private static bool CornersWalked(OpenGround ground, List<Tri> tris, Dictionary<long, int> byKey, double x, double z, double feet)
    {
        foreach (var (dx, dz) in new[] { (-1, -1), (0, -1), (1, -1), (-1, 0), (1, 0), (-1, 1), (0, 1), (1, 1) })
        {
            // A hair inside the corner, so the face found is the column's own
            double cx = x + dx * (Cell / 2 - 0.01), cz = z + dz * (Cell / 2 - 0.01);
            bool found = false;
            foreach (var (y, index) in ground.Column(cx, cz))
            {
                if (y > feet + Body.StepUp + 1e-6) continue;
                if (y < feet - Body.SnapDown) break;
                if (byKey.TryGetValue(ground[index].Key, out int id) && tris[id].Walked) { found = true; break; }
                if (ground[index].FacesUp) break;   // the highest ground there slides
            }
            if (!found) return false;
        }
        return true;
    }

    /// <summary>
    /// Where feet at <paramref name="feet"/> land at x, z: the highest ground there no more than a step over them and no
    /// more than <see cref="MostDrop"/> under them, walkable (never a steep band, whose foot slides) or under water deep
    /// enough to swim, and the feet's height on it; null where there is none, or what is highest there is neither.
    /// </summary>
    private static double? Landing(OpenGround ground, List<Tri> tris, Dictionary<long, int> byKey, double feet, double x, double z)
    {
        double? water = ground.WaterAt(x, z);
        foreach (var (y, index) in ground.Column(x, z))
        {
            var face = ground[index];
            if (!face.FacesUp) continue;
            bool swim = water is { } w && w - y > Body.SwimDepth;
            double at = swim ? water!.Value - Body.SwimDepth : y;
            if (at > feet + Body.StepUp + 1e-6) continue;
            if (at < feet - MostDrop) return null;
            if (!swim && !face.Walkable) return null;
            return at;
        }
        return null;
    }

    /// <summary>
    /// Whether the way from feet at x, y, z out along dx, dz for <paramref name="s"/> metres, and down (or up) to the
    /// feet there, is open: nothing across it at the knees or the head, and nothing in the column over the landing
    /// between it and the body's height over the higher of the two.
    /// </summary>
    private static bool Open(OpenGround ground, double x, double y, double z, double dx, double dz, double s, double landing, bool drop)
    {
        double top = Math.Max(y, landing);
        foreach (double h in new[] { top + Body.StepUp + 0.1, top + Body.Height - 0.25 })
            if (ground.Raycast(new OpenVec(x, h, z), new OpenVec(dx, 0, dz), s) is not null) return false;
        double px = x + dx * s, pz = z + dz * s;
        foreach (var (yy, _) in ground.Column(px, pz))
            if (yy > landing + (drop ? 0.05 : 0.02) && yy <= top + Body.Height) return false;
        return true;
    }

    /// <summary>How many pieces the mesh falls into when walked and dropped through both ways (swims aside), and the largest's polygons.</summary>
    public static (int Pieces, int Largest) Pieces(OpenNavMesh mesh, OpenNavFlags allowed = OpenNavFlags.Walk | OpenNavFlags.Drop)
    {
        int n = mesh.Polygons.Count;
        var both = new List<int>[n];
        for (int i = 0; i < n; i++) both[i] = new List<int>();
        foreach (var l in mesh.Links)
            if ((l.Flags & ~allowed) == 0) { both[l.From].Add(l.To); both[l.To].Add(l.From); }
        var seen = new bool[n];
        int pieces = 0, largest = 0;
        for (int s = 0; s < n; s++)
        {
            if (seen[s]) continue;
            pieces++;
            int size = 0;
            var stack = new Stack<int>();
            stack.Push(s); seen[s] = true;
            while (stack.Count > 0)
            {
                size++;
                foreach (int m in both[stack.Pop()])
                    if (!seen[m]) { seen[m] = true; stack.Push(m); }
            }
            largest = Math.Max(largest, size);
        }
        return (pieces, largest);
    }
}
