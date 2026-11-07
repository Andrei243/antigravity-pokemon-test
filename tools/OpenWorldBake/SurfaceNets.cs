namespace OpenWorldBake;

/// <summary>
/// A chunk of the land at one level of detail, as the baker keeps it: every vertex in whole millimetres, a normal
/// and a cover each, and the triangles (the ground's first, then the skirts that hide the seam against a chunk of
/// another level, which nothing but the renderer reads).
/// </summary>
public sealed class ChunkMesh
{
    public required int ChunkX { get; init; }
    public required int ChunkZ { get; init; }
    public required int Lod { get; init; }
    public required double Cell { get; init; }
    /// <summary>Three a vertex: x, y, z in millimetres.</summary>
    public required int[] Positions { get; init; }
    /// <summary>Three a vertex.</summary>
    public required float[] Normals { get; init; }
    public required Cover[] Covers { get; init; }
    /// <summary>Three a triangle, counter-clockwise seen from the air: the ground's own, the apron's, then the skirts'.</summary>
    public required int[] Indices { get; init; }
    /// <summary>The chunk's own ground: what is collided with and walked (every piece of the world is one chunk's own).</summary>
    public required int GroundTriangles { get; init; }
    /// <summary>The row west and north that the neighbours there own too: drawn, so a level never stops short of its border.</summary>
    public required int ApronTriangles { get; init; }

    public int VertexCount => Positions.Length / 3;
    public int TriangleCount => Indices.Length / 3;

    public D3 Vertex(int v) => new(Mm.Metres(Positions[v * 3]), Mm.Metres(Positions[v * 3 + 1]), Mm.Metres(Positions[v * 3 + 2]));

    /// <summary>The chunk as bytes: what two bakes are compared by.</summary>
    public byte[] Bytes()
    {
        using var stream = new MemoryStream();
        using var w = new BinaryWriter(stream);
        w.Write(ChunkX); w.Write(ChunkZ); w.Write(Lod); w.Write(Cell);
        w.Write(Positions.Length); foreach (int p in Positions) w.Write(p);
        w.Write(Normals.Length); foreach (float n in Normals) w.Write(n);
        w.Write(Covers.Length); foreach (var c in Covers) w.Write((byte)c);
        w.Write(Indices.Length); foreach (int i in Indices) w.Write(i);
        w.Write(GroundTriangles);
        w.Write(ApronTriangles);
        w.Flush();
        return stream.ToArray();
    }
}

/// <summary>
/// Surface nets over the land's field, a chunk at a time, single-threaded (plan 32: "the meshers are ours,
/// single-threaded per chunk, and they round to millimetres"). The grid is the world's own: a corner's place is a
/// whole number of cells from the origin, so two chunks of a level compute the same vertex for the cell they share
/// and meet without a seam. A chunk meshes its own cells and one more on its east and south sides, and emits the
/// quads of the edges whose four cells it holds, so every edge of the world is emitted by exactly one chunk.
/// The engine's <c>SdfMesher</c> meshes an <c>SdfModel</c> (shapes, bones, colours, every core); a height field
/// with carves is none of those, which is why the prototype has a mesher of its own.
/// </summary>
public static class SurfaceNets
{
    public static ChunkMesh Mesh(TerrainWorld world, int cx, int cz, int lod, double cell, double skirt)
    {
        var (x0, z0) = world.ChunkOrigin(cx, cz);
        int n = (int)Math.Round(world.ChunkSize / cell);
        // The grid starts a cell west and north of the chunk: its own cells, the one more on its east and south that it
        // shares with its neighbours there, and the apron, the one on its west and north (below)
        long gi0 = (long)Math.Round(x0 / cell) - 1, gk0 = (long)Math.Round(z0 / cell) - 1;
        int cxn = n + 3, czn = n + 3;                       // corners across and down

        // The land's height at every corner's column, once
        var colH = new double[cxn * czn];
        var colN = new double[cxn * czn];
        for (int k = 0; k < czn; k++)
            for (int i = 0; i < cxn; i++)
                (colH[k * cxn + i], colN[k * cxn + i]) = world.Column((gi0 + i) * cell, (gk0 + k) * cell);

        // How far up and down the surface can be: the heights, and any carve over the chunk
        double lo = colH.Min(), hi = colH.Max();
        double bx0 = (gi0 - 1) * cell, bx1 = (gi0 + n + 2) * cell, bz0 = (gk0 - 1) * cell, bz1 = (gk0 + n + 2) * cell;
        foreach (var c in world.Carves.Where(c => c.Max.X >= bx0 && c.Min.X <= bx1 && c.Max.Z >= bz0 && c.Min.Z <= bz1)) lo = Math.Min(lo, c.Min.Y);
        // Every carve is asked at every corner, as the field asks them: two chunks then give the corner they share
        // the very same number, whichever carves are near either
        var carves = world.Carves.ToArray();
        long j0 = (long)Math.Floor(lo / cell) - 2, j1 = (long)Math.Ceiling(hi / cell) + 2;
        int ny = (int)(j1 - j0);                            // cells up; corners 0..ny
        int cyn = ny + 1;

        var f = new double[cxn * cyn * czn];
        int Corner(int i, int j, int k) => (k * cyn + j) * cxn + i;
        for (int k = 0; k < czn; k++)
            for (int i = 0; i < cxn; i++)
            {
                double h = colH[k * cxn + i], norm = colN[k * cxn + i];
                double x = (gi0 + i) * cell, z = (gk0 + k) * cell;
                for (int j = 0; j < cyn; j++)
                {
                    double y = (j0 + j) * cell;
                    double v = (y - h) / norm;
                    foreach (var c in carves) v = Math.Max(v, -c.Distance(new D3(x, y, z)));
                    f[Corner(i, j, k)] = v;
                }
            }
        D3 CornerAt(int i, int j, int k) => new((gi0 + i) * cell, (j0 + j) * cell, (gk0 + k) * cell);

        // A vertex in every cell the surface passes through: the mean of where it crosses the cell's edges
        int ncx = n + 2, ncy = ny, ncz = n + 2;
        var cellVertex = new int[ncx * ncy * ncz];
        Array.Fill(cellVertex, -1);
        var positions = new List<int>();
        Span<(int, int, int, int, int, int)> edges = stackalloc (int, int, int, int, int, int)[]
        {
            (0,0,0, 1,0,0), (0,1,0, 1,1,0), (0,0,1, 1,0,1), (0,1,1, 1,1,1),
            (0,0,0, 0,1,0), (1,0,0, 1,1,0), (0,0,1, 0,1,1), (1,0,1, 1,1,1),
            (0,0,0, 0,0,1), (1,0,0, 1,0,1), (0,1,0, 0,1,1), (1,1,0, 1,1,1)
        };
        for (int k = 0; k < ncz; k++)
            for (int j = 0; j < ncy; j++)
                for (int i = 0; i < ncx; i++)
                {
                    int solid = 0;
                    for (int c = 0; c < 8; c++)
                        if (f[Corner(i + (c & 1), j + ((c >> 1) & 1), k + (c >> 2))] < 0) solid++;
                    if (solid == 0 || solid == 8) continue;
                    double sx = 0, sy = 0, sz = 0;
                    int count = 0;
                    foreach (var (ai, aj, ak, bi, bj, bk) in edges)
                    {
                        double fa = f[Corner(i + ai, j + aj, k + ak)], fb = f[Corner(i + bi, j + bj, k + bk)];
                        if ((fa < 0) == (fb < 0)) continue;
                        double t = fa / (fa - fb);
                        var a = CornerAt(i + ai, j + aj, k + ak); var b = CornerAt(i + bi, j + bj, k + bk);
                        sx += a.X + (b.X - a.X) * t; sy += a.Y + (b.Y - a.Y) * t; sz += a.Z + (b.Z - a.Z) * t;
                        count++;
                    }
                    cellVertex[(k * ncy + j) * ncx + i] = positions.Count / 3;
                    positions.Add(Mm.Of(sx / count)); positions.Add(Mm.Of(sy / count)); positions.Add(Mm.Of(sz / count));
                }
        int CellVertex(int i, int j, int k) => cellVertex[(k * ncy + j) * ncx + i];
        D3 V(int v) => new(Mm.Metres(positions[v * 3]), Mm.Metres(positions[v * 3 + 1]), Mm.Metres(positions[v * 3 + 2]));

        // A quad across every edge the surface crosses whose four cells this chunk holds, facing the air. The chunk's
        // own are the edges whose cells lie in its own cells and the shared row east and south (every edge of the world
        // is the own of exactly one chunk); the apron is the rest it holds, a row west and north that its neighbours
        // there own too. The apron is drawn and never collided with: beside a chunk of another level it covers the
        // half cells each level's surface stops short of its border, which left a strip of nothing between the two
        var ground = new List<int>();
        var apronList = new List<int>();
        void Quad(int a, int b, int c, int d, D3 outward, List<int> into)
        {
            if (a < 0 || b < 0 || c < 0 || d < 0) return;
            var normal = D3.Cross(V(b) - V(a), V(c) - V(a)) + D3.Cross(V(c) - V(a), V(d) - V(a));
            if (D3.Dot(normal, outward) < 0) (b, d) = (d, b);
            Triangle(a, b, c, into);
            Triangle(a, c, d, into);
        }
        void Triangle(int a, int b, int c, List<int> into)
        {
            if (a == b || b == c || a == c) return;
            if (D3.Cross(V(b) - V(a), V(c) - V(a)).Length < 1e-9) return;
            into.Add(a); into.Add(b); into.Add(c);
        }
        // (Indices here are the grid's, a cell west and north of the chunk's own: its own cells are 1 to n)
        for (int k = 0; k <= n + 2; k++)
            for (int j = 0; j <= ny; j++)
                for (int i = 0; i <= n + 2; i++)
                {
                    bool here = f[Corner(i, j, k)] < 0;
                    // Along x: the cells (i, j-1..j, k-1..k); the chunk's own for i in 1..n and k in 2..n+1
                    if (i <= n + 1 && j >= 1 && j <= ny - 1 && k >= 1 && k <= n + 1 && here != (f[Corner(i + 1, j, k)] < 0))
                        Quad(CellVertex(i, j - 1, k - 1), CellVertex(i, j, k - 1), CellVertex(i, j, k), CellVertex(i, j - 1, k), new D3(here ? 1 : -1, 0, 0),
                            i >= 1 && i <= n && k >= 2 ? ground : apronList);
                    // Along y: the cells (i-1..i, j, k-1..k); the chunk's own for i and k in 2..n+1
                    if (j <= ny - 1 && i >= 1 && i <= n + 1 && k >= 1 && k <= n + 1 && here != (f[Corner(i, j + 1, k)] < 0))
                        Quad(CellVertex(i - 1, j, k - 1), CellVertex(i, j, k - 1), CellVertex(i, j, k), CellVertex(i - 1, j, k), new D3(0, here ? 1 : -1, 0),
                            i >= 2 && k >= 2 ? ground : apronList);
                    // Along z: the cells (i-1..i, j-1..j, k); the chunk's own for k in 1..n and i in 2..n+1
                    if (k <= n + 1 && i >= 1 && i <= n + 1 && j >= 1 && j <= ny - 1 && here != (f[Corner(i, j, k + 1)] < 0))
                        Quad(CellVertex(i - 1, j - 1, k), CellVertex(i, j - 1, k), CellVertex(i, j, k), CellVertex(i - 1, j, k), new D3(0, 0, here ? 1 : -1),
                            k >= 1 && k <= n && i >= 2 ? ground : apronList);
                }
        int groundTriangles = ground.Count / 3;
        int apronTriangles = apronList.Count / 3;
        var indices = ground.Concat(apronList).ToList();
        int surfaceTriangles = groundTriangles + apronTriangles;

        // Smooth normals: the faces round each vertex, by their area
        int vertexCount = positions.Count / 3;
        var sums = new D3[vertexCount];
        for (int t = 0; t < surfaceTriangles; t++)
        {
            int a = indices[t * 3], b = indices[t * 3 + 1], c = indices[t * 3 + 2];
            var face = D3.Cross(V(b) - V(a), V(c) - V(a));
            sums[a] += face; sums[b] += face; sums[c] += face;
        }

        // Skirts: every edge of the ground's border whose face looks up hangs a strip down, which hides a seam against
        // a chunk of another level (a chunk of the same level meets this one exactly, and its skirts are under the ground)
        var edgeCount = new Dictionary<long, int>();
        long Key(int a, int b) => a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a;
        for (int t = 0; t < surfaceTriangles; t++)
            for (int e = 0; e < 3; e++)
            {
                long key = Key(indices[t * 3 + e], indices[t * 3 + (e + 1) % 3]);
                edgeCount[key] = edgeCount.GetValueOrDefault(key) + 1;
            }
        var skirtPositions = new List<int>();
        var skirtOf = new Dictionary<int, int>();
        var skirtSource = new List<int>();
        int Below(int v)
        {
            if (skirtOf.TryGetValue(v, out int s)) return s;
            s = vertexCount + skirtPositions.Count / 3;
            skirtPositions.Add(positions[v * 3]); skirtPositions.Add(positions[v * 3 + 1] - Mm.Of(skirt)); skirtPositions.Add(positions[v * 3 + 2]);
            skirtOf[v] = s;
            skirtSource.Add(v);
            return s;
        }
        for (int t = 0; t < surfaceTriangles; t++)
        {
            int a0 = indices[t * 3], b0 = indices[t * 3 + 1], c0 = indices[t * 3 + 2];
            var face = D3.Cross(V(b0) - V(a0), V(c0) - V(a0)).Normalized();
            if (face.Y <= 0.2) continue;
            for (int e = 0; e < 3; e++)
            {
                int a = indices[t * 3 + e], b = indices[t * 3 + (e + 1) % 3];
                if (edgeCount[Key(a, b)] != 1) continue;
                int a1 = Below(a), b1 = Below(b);
                indices.Add(b); indices.Add(a); indices.Add(a1);
                indices.Add(b); indices.Add(a1); indices.Add(b1);
            }
        }

        var allPositions = positions.Concat(skirtPositions).ToArray();
        int total = allPositions.Length / 3;
        var normals = new float[total * 3];
        var covers = new Cover[total];
        for (int v = 0; v < total; v++)
        {
            int source = v < vertexCount ? v : skirtSource[v - vertexCount];
            var nn = sums[source].Normalized();
            if (nn.Length < 0.5) nn = new D3(0, 1, 0);
            normals[v * 3] = (float)nn.X; normals[v * 3 + 1] = (float)nn.Y; normals[v * 3 + 2] = (float)nn.Z;
            var p = new D3(Mm.Metres(allPositions[source * 3]), Mm.Metres(allPositions[source * 3 + 1]), Mm.Metres(allPositions[source * 3 + 2]));
            covers[v] = world.CoverAt(p, nn);
        }

        return new ChunkMesh
        {
            ChunkX = cx, ChunkZ = cz, Lod = lod, Cell = cell,
            Positions = allPositions, Normals = normals, Covers = covers,
            Indices = indices.ToArray(), GroundTriangles = groundTriangles, ApronTriangles = apronTriangles
        };
    }
}

/// <summary>A world baked: its chunks at the levels asked for (OW0 keeps them in memory; OW2 writes them under baked/).</summary>
public sealed class Bake
{
    /// <summary>The cells of the three levels, in metres (plan 32: 0.5 near, coarser for the far rings).</summary>
    public static readonly double[] Cells = { 0.5, 2.0, 4.0 };

    /// <summary>How far each level's skirt hangs, in metres.</summary>
    public static readonly double[] Skirts = { 0.5, 2.0, 4.0 };

    public required TerrainWorld World { get; init; }
    public Dictionary<(int X, int Z, int Lod), ChunkMesh> Chunks { get; } = new();

    public static Bake Of(TerrainWorld world, Func<int, int, int, bool> wanted)
    {
        var bake = new Bake { World = world };
        for (int lod = 0; lod < Cells.Length; lod++)
            for (int cz = 0; cz < world.ChunksZ; cz++)
                for (int cx = 0; cx < world.ChunksX; cx++)
                    if (wanted(cx, cz, lod))
                        bake.Chunks[(cx, cz, lod)] = SurfaceNets.Mesh(world, cx, cz, lod, Cells[lod], Skirts[lod]);
        return bake;
    }

    /// <summary>The chunk at the finest level baked for it, up to <paramref name="finest"/>.</summary>
    public ChunkMesh? Best(int cx, int cz, int finest)
    {
        for (int lod = finest; lod < Cells.Length; lod++)
            if (Chunks.TryGetValue((cx, cz, lod), out var mesh)) return mesh;
        return null;
    }
}
