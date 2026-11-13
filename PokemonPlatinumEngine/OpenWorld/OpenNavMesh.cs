using System;
using System.Collections.Generic;
using System.IO;

namespace PokemonPlatinumEngine.OpenWorld;

/// <summary>
/// What crosses a link of the navigation mesh (plan 32, "The navigation mesh"): walking (on foot, stepping a kerb or
/// a short steep band, going down what the feet are held to), swimming (into water deeper than
/// <see cref="OpenBody.SwimDepth"/>), a drop (off a ledge, one way: the way back is elsewhere), and the climb and the
/// glide plan 34's mount opens, and its hop and high jump onto ground over a step (plan 34 · PD2: baked only for a
/// region whose <c>world.json</c> says it is ridden). A link is usable when every flag it carries is allowed.
/// </summary>
[Flags]
public enum OpenNavFlags : byte
{
    None = 0,
    Walk = 1,
    Swim = 2,
    Drop = 4,
    Climb = 8,
    Glide = 16,

    /// <summary>The mount's hop onto ground over a step and up to <see cref="OpenBody.HopHeight"/>'s clearance (plan 34 · PD2).</summary>
    Jump = 32,

    /// <summary>The mount's high jump onto ground the hop can't reach, up to <see cref="OpenBody.HighJumpHeight"/>'s clearance.</summary>
    HighJump = 64
}

/// <summary>
/// A polygon of the navigation mesh (plan 32 Â· OW4): convex on the ground (x and z), its corners the collision mesh's
/// own vertices in the mesh's order, all of one chunk; its hub the middle of its corners at the height of the ground
/// there, from which its heights are read by a fan to its edges. A <see cref="Swim"/> polygon is ground under water
/// deeper than the body swims, its <see cref="Surface"/> the water's (whose feet float <see cref="OpenBody.SwimDepth"/>
/// under it).
/// </summary>
public sealed class OpenNavPolygon
{
    public required int Id { get; init; }
    public required int ChunkX { get; init; }
    public required int ChunkZ { get; init; }

    /// <summary>The corners, as indices into <see cref="OpenNavMesh.Vertices"/>, turning one way round (<see cref="OpenNavMesh.Turn"/> positive).</summary>
    public required int[] Corners { get; init; }

    public required OpenPos Hub { get; init; }
    public required bool Swim { get; init; }

    /// <summary>The water's surface over a swim polygon, in millimetres; <see cref="BakedChunk.NoWater"/> otherwise.</summary>
    public required int Surface { get; init; }

    // The bounds on the ground, in millimetres (filled by the mesh)
    internal int MinX, MinZ, MaxX, MaxZ;
}

/// <summary>
/// A way from one polygon to another (plan 32 Â· OW4). A link of the mesh is an edge the two share: <see cref="A"/> and
/// <see cref="B"/> its ends, crossed anywhere between them. A link off the mesh (<see cref="OffMesh"/>: a step onto a
/// kerb, a drop off a ledge) is taken from one point (<see cref="A"/>, which is <see cref="B"/>) and lands at another
/// (<see cref="Land"/>). Each way is its own link, so a drop has none back.
/// </summary>
public readonly record struct OpenNavLink(int From, int To, OpenPos A, OpenPos B, OpenNavFlags Flags, bool OffMesh, OpenPos Land)
{
    /// <summary>The point a path crosses the link at when it can't say better: the edge's middle, or where an off-mesh link lands.</summary>
    public OpenVec Middle => OffMesh ? OpenVec.Of(Land) : (OpenVec.Of(A) + OpenVec.Of(B)) * 0.5;
}

/// <summary>
/// An open map's navigation mesh (plan 32, "The navigation mesh"; OW4): the walkable triangles of level 0's ground and
/// the boxes' tops, where the capsule stands without sliding and has its clearance (its height over the feet, its
/// radius round its body), merged into convex polygons, layered (a cave's floor and the hill over it are other
/// polygons over the same ground), joined by links that say what crosses them. <b>Built offline by
/// <c>tools/OpenWorldBake</c></b> and written beside the chunks as <c>baked/navmesh.bin</c>, so the game never builds
/// one and two machines never disagree. Paths are <see cref="NavPath"/>'s, what can be come to <see cref="OpenWalk"/>'s.
/// GPU-free, single-threaded, whole numbers in its file.
/// </summary>
public sealed class OpenNavMesh
{
    /// <summary>The format's number, the file's fifth byte; a file of another is refused.</summary>
    public const byte Format = 1;

    /// <summary>The file's name in a region's <c>baked</c> folder.</summary>
    public const string FileName = "navmesh.bin";

    /// <summary>How far, in metres, a place may stand over or under a polygon's ground and still be on it (layers stand a body's height apart).</summary>
    public const double Reach = 1.2;

    private static ReadOnlySpan<byte> Magic => "OWNM"u8;

    /// <summary>The side of a square of the index by which polygons are found, in millimetres.</summary>
    private const int Square = 4 * OpenPos.PerMetre;

    private readonly OpenPos[] vertices;
    private readonly OpenNavPolygon[] polygons;
    private readonly OpenNavLink[] links;
    private readonly int[][] linksFrom;
    private readonly Dictionary<(int, int), List<int>> squares = new();

    public OpenNavMesh(IReadOnlyList<OpenPos> vertices, IReadOnlyList<OpenNavPolygon> polygons, IReadOnlyList<OpenNavLink> links)
    {
        this.vertices = new OpenPos[vertices.Count];
        for (int i = 0; i < vertices.Count; i++) this.vertices[i] = vertices[i];
        this.polygons = new OpenNavPolygon[polygons.Count];
        for (int i = 0; i < polygons.Count; i++)
        {
            var p = polygons[i];
            if (p.Id != i) throw new InvalidDataException($"navigation polygon {i} says it is {p.Id}");
            if (p.Corners.Length < 3) throw new InvalidDataException($"navigation polygon {i} has {p.Corners.Length} corners");
            p.MinX = p.MinZ = int.MaxValue; p.MaxX = p.MaxZ = int.MinValue;
            foreach (int c in p.Corners)
            {
                if (c < 0 || c >= this.vertices.Length) throw new InvalidDataException($"navigation polygon {i} names vertex {c} of {this.vertices.Length}");
                var v = this.vertices[c];
                p.MinX = Math.Min(p.MinX, v.X); p.MaxX = Math.Max(p.MaxX, v.X);
                p.MinZ = Math.Min(p.MinZ, v.Z); p.MaxZ = Math.Max(p.MaxZ, v.Z);
            }
            this.polygons[i] = p;
            for (int z = Cell(p.MinZ); z <= Cell(p.MaxZ); z++)
                for (int x = Cell(p.MinX); x <= Cell(p.MaxX); x++)
                {
                    if (!squares.TryGetValue((x, z), out var list)) squares[(x, z)] = list = new List<int>();
                    list.Add(i);
                }
        }
        this.links = new OpenNavLink[links.Count];
        var from = new List<int>[this.polygons.Length];
        for (int i = 0; i < from.Length; i++) from[i] = new List<int>();
        for (int i = 0; i < links.Count; i++)
        {
            var l = links[i];
            if (l.From < 0 || l.From >= this.polygons.Length || l.To < 0 || l.To >= this.polygons.Length || l.From == l.To)
                throw new InvalidDataException($"navigation link {i} joins {l.From} to {l.To} of {this.polygons.Length}");
            this.links[i] = l;
            from[l.From].Add(i);
        }
        linksFrom = new int[from.Length][];
        for (int i = 0; i < from.Length; i++) linksFrom[i] = from[i].ToArray();
    }

    private static int Cell(int mm) => (int)Math.Floor(mm / (double)Square);

    public IReadOnlyList<OpenPos> Vertices => vertices;
    public IReadOnlyList<OpenNavPolygon> Polygons => polygons;
    public IReadOnlyList<OpenNavLink> Links => links;

    /// <summary>The links leaving a polygon, by their number.</summary>
    public IReadOnlyList<int> LinksFrom(int polygon) => linksFrom[polygon];

    /// <summary>
    /// What a body may cross with what the region grants it: walking and drops always, swimming with the grant, and,
    /// only while it is <paramref name="mounted"/> (plan 30 · FM12, plan 34 · PD2), the mount's hop always and its high
    /// jump, climbs and glides with their grants. A walker is never handed a link of the mount's, whatever its grants say,
    /// as the capsule reads no power but the swim on foot.
    /// </summary>
    public static OpenNavFlags Allowed(MovementGrants grants, bool mounted = false) =>
        OpenNavFlags.Walk | OpenNavFlags.Drop | ((grants & MovementGrants.Swim) != 0 ? OpenNavFlags.Swim : OpenNavFlags.None)
        | (mounted ? OpenNavFlags.Jump : OpenNavFlags.None)
        | (mounted && (grants & MovementGrants.HighJump) != 0 ? OpenNavFlags.HighJump : OpenNavFlags.None)
        | (mounted && (grants & MovementGrants.Climb) != 0 ? OpenNavFlags.Climb : OpenNavFlags.None)
        | (mounted && (grants & MovementGrants.Glide) != 0 ? OpenNavFlags.Glide : OpenNavFlags.None);

    /// <summary>
    /// Twice the signed area of a, b, c on the ground (x and z): positive one way round, negative the other. The
    /// polygons' corners turn the positive way.
    /// </summary>
    public static double Turn(double ax, double az, double bx, double bz, double cx, double cz) =>
        (bx - ax) * (cz - az) - (bz - az) * (cx - ax);

    /// <summary>A corner of a polygon, in metres.</summary>
    public OpenVec Corner(OpenNavPolygon p, int k) => OpenVec.Of(vertices[p.Corners[k]]);

    /// <summary>Whether x, z lies inside a polygon on the ground, within <paramref name="slack"/> metres of its edges.</summary>
    public bool Contains(OpenNavPolygon p, double x, double z, double slack = 0.001)
    {
        double px = x * OpenPos.PerMetre, pz = z * OpenPos.PerMetre, s = slack * OpenPos.PerMetre;
        if (px < p.MinX - s || px > p.MaxX + s || pz < p.MinZ - s || pz > p.MaxZ + s) return false;
        // A polygon is convex to within a few centimetres (the bake's slack), so this is the even-odd rule, not the
        // edges' half-planes, with a point within the slack of an edge counted in
        int n = p.Corners.Length;
        bool inside = false;
        for (int k = 0, j = n - 1; k < n; j = k++)
        {
            var a = vertices[p.Corners[k]];
            var b = vertices[p.Corners[j]];
            if ((a.Z > pz) != (b.Z > pz) && px < (b.X - a.X) * (pz - a.Z) / (b.Z - (double)a.Z) + a.X) inside = !inside;
        }
        if (inside) return true;
        for (int k = 0; k < n; k++)
        {
            var a = vertices[p.Corners[k]];
            var b = vertices[p.Corners[(k + 1) % n]];
            double ex = b.X - a.X, ez = b.Z - a.Z, l2 = ex * ex + ez * ez;
            double t = l2 > 0 ? Math.Clamp(((px - a.X) * ex + (pz - a.Z) * ez) / l2, 0, 1) : 0;
            double qx = a.X + ex * t - px, qz = a.Z + ez * t - pz;
            if (qx * qx + qz * qz <= s * s) return true;
        }
        return false;
    }

    /// <summary>
    /// The ground's height in a polygon at x, z, in metres: read off the fan from its hub to its edges (the bake holds
    /// each polygon to its ground within a few centimetres). Where x, z lies outside it, the nearest edge's fan.
    /// </summary>
    public double GroundIn(OpenNavPolygon p, double x, double z)
    {
        var hub = OpenVec.Of(p.Hub);
        int n = p.Corners.Length;
        double best = double.MaxValue, height = hub.Y;
        for (int k = 0; k < n; k++)
        {
            var a = Corner(p, k);
            var b = Corner(p, (k + 1) % n);
            if (OpenTriangles.HeightIn(x, z, hub, a, b) is { } y) return y;
            // Outside: the edge nearest, its line's height there
            double ex = b.X - a.X, ez = b.Z - a.Z, l2 = ex * ex + ez * ez;
            double t = l2 > 0 ? Math.Clamp(((x - a.X) * ex + (z - a.Z) * ez) / l2, 0, 1) : 0;
            double qx = a.X + ex * t - x, qz = a.Z + ez * t - z, d = qx * qx + qz * qz;
            if (d < best) { best = d; height = a.Y + (b.Y - a.Y) * t; }
        }
        return height;
    }

    /// <summary>Where a body's feet are in a polygon at x, z: on its ground, or floating under a swim polygon's surface.</summary>
    public double FeetIn(OpenNavPolygon p, double x, double z)
    {
        double ground = GroundIn(p, x, z);
        return p.Swim ? Math.Max(ground, p.Surface / (double)OpenPos.PerMetre - OpenBody.SwimDepth) : ground;
    }

    /// <summary>
    /// The polygon a place stands in: the one under it on the ground whose feet are nearest its height, within
    /// <see cref="Reach"/>; ties go to the lower number. Null where none is (a wall's foot, the air, a place never walked).
    /// </summary>
    public int? PolygonAt(OpenPos pos)
    {
        if (!squares.TryGetValue((Cell(pos.X), Cell(pos.Z)), out var list)) return null;
        double x = pos.MetresX, y = pos.MetresY, z = pos.MetresZ;
        int? best = null;
        double nearest = Reach;
        foreach (int i in list)
        {
            var p = polygons[i];
            if (!Contains(p, x, z)) continue;
            double d = Math.Abs(FeetIn(p, x, z) - y);
            if (d < nearest - 1e-9 || d <= nearest + 1e-9 && best is { } b && i < b) { nearest = d; best = i; }
        }
        return best;
    }

    /// <summary>
    /// The polygon nearest a place on the ground within <paramref name="within"/> metres (and <see cref="Reach"/> of its
    /// height), and the point of it nearest: where a place a little off the mesh (a marker by a wall) is walked from.
    /// </summary>
    public (int Polygon, OpenPos Point)? Nearest(OpenPos pos, double within = 1.5)
    {
        if (PolygonAt(pos) is { } inside) return (inside, pos);
        double x = pos.MetresX, y = pos.MetresY, z = pos.MetresZ;
        int reach = (int)Math.Ceiling(within * OpenPos.PerMetre);
        (int Polygon, OpenPos Point)? best = null;
        double nearest = within * within;
        var seen = new HashSet<int>();
        for (int cz = Cell(pos.Z - reach); cz <= Cell(pos.Z + reach); cz++)
            for (int cx = Cell(pos.X - reach); cx <= Cell(pos.X + reach); cx++)
            {
                if (!squares.TryGetValue((cx, cz), out var list)) continue;
                foreach (int i in list)
                {
                    if (!seen.Add(i)) continue;
                    var p = polygons[i];
                    // The nearest point of the polygon's edges, pulled a centimetre inside toward the hub
                    int n = p.Corners.Length;
                    for (int k = 0; k < n; k++)
                    {
                        var a = Corner(p, k);
                        var b = Corner(p, (k + 1) % n);
                        double ex = b.X - a.X, ez = b.Z - a.Z, l2 = ex * ex + ez * ez;
                        double t = l2 > 0 ? Math.Clamp(((x - a.X) * ex + (z - a.Z) * ez) / l2, 0, 1) : 0;
                        double qx = a.X + ex * t, qz = a.Z + ez * t;
                        double d = (qx - x) * (qx - x) + (qz - z) * (qz - z);
                        if (d > nearest + 1e-12 || d >= nearest - 1e-12 && best is { } b0 && i >= b0.Polygon) continue;
                        var hub = OpenVec.Of(p.Hub);
                        double hx = hub.X - qx, hz = hub.Z - qz, hl = Math.Sqrt(hx * hx + hz * hz);
                        if (hl > 0.02) { qx += hx / hl * 0.01; qz += hz / hl * 0.01; }
                        double feet = FeetIn(p, qx, qz);
                        if (Math.Abs(feet - y) > Reach) continue;
                        nearest = d;
                        best = (i, OpenPos.FromMetres(qx, feet, qz));
                    }
                }
            }
        return best;
    }

    // ------------------------------------------------------------------ the file

    /// <summary>The mesh as the file holds it.</summary>
    public byte[] Bytes()
    {
        using var stream = new MemoryStream();
        Write(stream);
        return stream.ToArray();
    }

    /// <summary>
    /// Writes the mesh: "OWNM", the format, then whole numbers only, each a zigzag of seven bits a byte (places as steps
    /// from the place before), so the bytes are the numbers' and the same on any machine.
    /// </summary>
    public void Write(Stream stream)
    {
        using var w = new BinaryWriter(stream, System.Text.Encoding.UTF8, leaveOpen: true);
        w.Write(Magic);
        w.Write(Format);
        var last = new OpenPos(0, 0, 0);
        w.Write7BitEncodedInt(vertices.Length);
        foreach (var v in vertices) Place(w, ref last, v);

        w.Write7BitEncodedInt(polygons.Length);
        int lastCorner = 0;
        foreach (var p in polygons)
        {
            Signed(w, p.ChunkX); Signed(w, p.ChunkZ);
            w.Write((byte)(p.Swim ? 1 : 0));
            if (p.Swim) Signed(w, p.Surface);
            Place(w, ref last, p.Hub);
            w.Write7BitEncodedInt(p.Corners.Length);
            foreach (int c in p.Corners) { Signed(w, c - lastCorner); lastCorner = c; }
        }

        w.Write7BitEncodedInt(links.Length);
        int lastFrom = 0;
        foreach (var l in links)
        {
            Signed(w, l.From - lastFrom); lastFrom = l.From;
            Signed(w, l.To - l.From);
            w.Write((byte)l.Flags);
            w.Write((byte)(l.OffMesh ? 1 : 0));
            Place(w, ref last, l.A);
            if (l.OffMesh) Place(w, ref last, l.Land);
            else Place(w, ref last, l.B);
        }
    }

    /// <summary>Reads a mesh written by <see cref="Write"/>; another format, or a file cut short, is refused.</summary>
    public static OpenNavMesh Read(Stream stream)
    {
        using var r = new BinaryReader(stream, System.Text.Encoding.UTF8, leaveOpen: true);
        Span<byte> magic = stackalloc byte[4];
        if (r.Read(magic) != 4 || !magic.SequenceEqual(Magic)) throw new InvalidDataException("not a navigation mesh (no OWNM)");
        byte format = r.ReadByte();
        if (format != Format) throw new InvalidDataException($"a navigation mesh of format {format}, not {Format}: bake the region again");
        var last = new OpenPos(0, 0, 0);
        var vertices = new OpenPos[r.Read7BitEncodedInt()];
        for (int i = 0; i < vertices.Length; i++) vertices[i] = Place(r, ref last);

        var polygons = new OpenNavPolygon[r.Read7BitEncodedInt()];
        int lastCorner = 0;
        for (int i = 0; i < polygons.Length; i++)
        {
            int cx = Unsigned(r), cz = Unsigned(r);
            bool swim = r.ReadByte() == 1;
            int surface = swim ? Unsigned(r) : BakedChunk.NoWater;
            var hub = Place(r, ref last);
            var corners = new int[r.Read7BitEncodedInt()];
            for (int k = 0; k < corners.Length; k++) { lastCorner += Unsigned(r); corners[k] = lastCorner; }
            polygons[i] = new OpenNavPolygon { Id = i, ChunkX = cx, ChunkZ = cz, Corners = corners, Hub = hub, Swim = swim, Surface = surface };
        }

        var links = new OpenNavLink[r.Read7BitEncodedInt()];
        int lastFrom = 0;
        for (int i = 0; i < links.Length; i++)
        {
            lastFrom += Unsigned(r);
            int to = lastFrom + Unsigned(r);
            var flags = (OpenNavFlags)r.ReadByte();
            bool off = r.ReadByte() == 1;
            var a = Place(r, ref last);
            var second = Place(r, ref last);
            links[i] = off ? new OpenNavLink(lastFrom, to, a, a, flags, true, second) : new OpenNavLink(lastFrom, to, a, second, flags, false, default);
        }
        return new OpenNavMesh(vertices, polygons, links);
    }

    /// <summary>A region's mesh from its <c>baked</c> folder, or null where it has none.</summary>
    public static OpenNavMesh? Open(string bakedFolder)
    {
        string path = Path.Combine(bakedFolder, FileName);
        if (!File.Exists(path)) return null;
        using var stream = File.OpenRead(path);
        return Read(stream);
    }

    private static void Place(BinaryWriter w, ref OpenPos last, OpenPos p)
    {
        Signed(w, (long)p.X - last.X); Signed(w, (long)p.Y - last.Y); Signed(w, (long)p.Z - last.Z);
        last = p;
    }

    private static OpenPos Place(BinaryReader r, ref OpenPos last)
    {
        last = new OpenPos(checked(last.X + Unsigned(r)), checked(last.Y + Unsigned(r)), checked(last.Z + Unsigned(r)));
        return last;
    }

    private static long Zig(long value) => (value << 1) ^ (value >> 63);
    private static long Unzig(long value) => (long)((ulong)value >> 1) ^ -(value & 1);
    private static void Signed(BinaryWriter w, long value) => w.Write7BitEncodedInt64(Zig(value));
    private static int Unsigned(BinaryReader r) => checked((int)Unzig(r.Read7BitEncodedInt64()));
}
