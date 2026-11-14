using System;
using System.Collections.Generic;
using System.Numerics;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.OpenWorld;

namespace PokemonPlatinumEngine.Graphics.OpenWorld;

/// <summary>A tree of the land beyond the region (plan 32 · OW9): where its foot stands, its size and which row of the trees' impostor atlas it shows.</summary>
internal readonly record struct OpenFarTree(Vector3 Foot, float Scale, int Row);

/// <summary>
/// The rings to the horizon (plan 32 · OW9; style guide, "Open world", "Beyond the region"), in place of OW8's plain:
/// rounded rings round the region's rectangle, the first a metre inside its border at the border's own heights, kept
/// 4 m out, eased to the border's median by 124 m as OW8's plain was, and from there rising into far hills (value noise
/// of the map's own seed, up to <see cref="HillsRise"/> over the median by <see cref="HillsFull"/> metres out, with the
/// farthest rings lifted a little more so the hills close the view) out to <see cref="Reach"/>, ending in the fog. Each
/// ring is sampled about as finely as it lies from the next, every ring at the border's own 2 m and its corners in
/// quarter circles, and two rings are joined by walking both round together by where each point stands along its side
/// or round its corner (<see cref="RingPoint.U"/>), so the far land is a few thousand triangles whatever the region's
/// size. Forests of far trees stand on it (<see cref="Trees"/>), drawn as the trees' impostors. GPU-free.
/// </summary>
internal static class OpenHorizon
{
    /// <summary>How far under the region the first ring begins, and how far out it keeps the border's own heights, in metres (OW8's).</summary>
    public const float Under = 1f, Near = 4f;

    /// <summary>How far out the land has come to the border's median height, in metres (OW8's), and where the hills begin.</summary>
    public const float Level = 124f;

    /// <summary>How far under the border's own height the first rings lie (OW8's): under the region's ground, too little for the outlines.</summary>
    public const float Drop = 0.04f;

    /// <summary>How far out the hills have risen whole, how high they rise over the median, and how far the land reaches, in metres.</summary>
    public const float HillsFull = 640f, HillsRise = 72f, Reach = 2600f;

    /// <summary>The farthest rings' own lift over the hills (so the land's edge, deep in the fog, stands against the sky), from where to where.</summary>
    public const float LiftRise = 30f, LiftFrom = 900f;

    /// <summary>How far out each ring stands from the border, in metres: OW8's band, then the hills.</summary>
    public static readonly float[] Rings =
    {
        -Under, Near, 12f, 28f, 60f, Level, 165f, 215f, 275f, 345f, 425f, 515f, 615f, 730f, 860f, 1010f, 1180f, 1380f, 1620f, 1900f, 2220f, Reach
    };

    /// <summary>The points of a corner's quarter circle at the band's rings (every ring of the hills takes as many as its distance asks).</summary>
    public const int CornerPoints = 8;

    /// <summary>Where far trees may stand: from this far out to this far, on a grid of cells this wide.</summary>
    public const float TreesFrom = 70f, TreesTo = 1150f, TreeCell = 13f;

    /// <summary>A point of a ring: where along the loop it stands (side k from u = k to k + ½, its corner to k + 1), the border point it stands out from and the way out.</summary>
    internal readonly record struct RingPoint(float U, Vector2 Border, Vector2 Out, bool OnCorner);

    /// <summary>The land and its trees, and the sea beyond the region where its border is sea (null where it isn't).</summary>
    public sealed record Land(OpenVertices Mesh, List<OpenFarTree> Trees, float Median, OpenVertices? Sea = null, float SeaLevel = 0f);

    /// <summary>
    /// How long a run of the border must be under water to be the sea's (plan 34 · PD1's second session), in metres: a
    /// river or a lake cut by the border is narrower, and the land beyond it stays the land.
    /// </summary>
    public const float SeaRun = 40f;

    /// <summary>How far along the border the sea's share is eased in and out on either side of it, in metres.</summary>
    public const float SeaEase = 64f;

    /// <summary>How far under the sea's surface the floor beyond the region lies at the border, and how much deeper it goes a metre out (to <see cref="SeaDeepest"/>).</summary>
    public const float SeaFloor = 1.5f, SeaSlope = 0.01f, SeaDeepest = 8f;

    /// <summary>
    /// The land beyond a region whose chunks span <paramref name="bounds"/> (west, north, east, south, in chunks), the
    /// ground's height at a point given by <paramref name="heightAt"/> (null where there is none), its hills of
    /// <paramref name="seed"/>; with its far trees unless <paramref name="trees"/> is false. Where <paramref name="waterAt"/>
    /// (the water's surface over a point, null where there is none) finds a run of the border at least <see cref="SeaRun"/>
    /// long under water, the land beyond it is the sea's (plan 34 · PD1's second session): its floor stays under the
    /// water's surface out to the horizon, no hills rise there, and a sea at that surface is laid over it out to
    /// <see cref="Reach"/> (<see cref="Land.Sea"/>). With no such run, or no <paramref name="waterAt"/>, the land is as it was.
    /// </summary>
    public static Land Of((int X0, int Z0, int X1, int Z1) bounds, Func<float, float, float?> heightAt, uint seed, bool trees = true,
        Func<float, float, float?>? waterAt = null)
    {
        float side = OpenMap.ChunkSide / (float)OpenPos.PerMetre;
        var rect = (X0: bounds.X0 * side, Z0: bounds.Z0 * side, X1: (bounds.X1 + 1) * side, Z1: (bounds.Z1 + 1) * side);
        var basePoints = Loop(rect, 2f, CornerPoints);

        // The border's own height at each point of the band: the lower of the ground 0.3 m and 1 m inside, OW8's rule
        var own = new Dictionary<float, float>();
        var all = new List<float>();
        foreach (var p in basePoints)
        {
            var edge = p.Border - p.Out * 0.3f;
            var inner = p.Border - p.Out * Under;
            float? h = heightAt(edge.X, edge.Y), hi = heightAt(inner.X, inner.Y);
            float best = h is { } a && hi is { } b ? MathF.Min(a, b) : h ?? hi ?? float.NaN;
            own[p.U] = best;
            if (!float.IsNaN(best)) all.Add(best);
        }
        all.Sort();
        float median = all.Count > 0 ? all[all.Count / 2] : 0f;
        foreach (var key in new List<float>(own.Keys)) if (float.IsNaN(own[key])) own[key] = median;

        var (seaShare, seaLevel) = waterAt == null ? (null, 0f) : SeaShares(basePoints, own, waterAt);
        // A ring point's share of the sea, by the border point nearest it along the loop
        var baseU = new float[basePoints.Count];
        for (int i = 0; i < baseU.Length; i++) baseU[i] = basePoints[i].U;
        float SeaOf(float u)
        {
            if (seaShare == null) return 0f;
            int i = Array.BinarySearch(baseU, u);
            if (i < 0)
            {
                i = ~i;
                if (i >= baseU.Length) i = 0;
                else if (i > 0 && u - baseU[i - 1] < baseU[i] - u) i--;
            }
            return seaShare[i];
        }

        var hills = new Hills(seed);
        float LandHeight(RingPoint p, float d, Vector2 at)
        {
            if (d <= Level)
            {
                float border = (own.TryGetValue(p.U, out var o) ? o : median) - Drop;
                if (d <= Near) return border;
                float t = (d - Near) / (Level - Near);
                return border + (median - border) * (t * t * (3f - 2f * t));
            }
            return median + Rise(d) * hills.At(at) + LiftRise * Smooth(LiftFrom, Reach, d);
        }
        float HeightOf(RingPoint p, float d, Vector2 at)
        {
            float land = LandHeight(p, d, at);
            float s = SeaOf(p.U);
            if (s <= 0f) return land;
            float border = own.TryGetValue(p.U, out var o) ? o : median;
            float floor = MathF.Min(border, seaLevel - SeaFloor) - MathF.Min(MathF.Max(d, 0f) * SeaSlope, SeaDeepest);
            return land + (floor - land) * s;
        }

        // Each ring's points: the band's are the base loop's; the hills' as fine as the gap to the next ring
        var rings = new List<(float D, List<(Vector3 At, float U)> Points)>();
        for (int r = 0; r < Rings.Length; r++)
        {
            float d = Rings[r];
            var points = d <= Level ? basePoints : Loop(rect, MathF.Max(2f, RingGap(r)), Math.Max(2, (int)MathF.Ceiling(MathF.PI / 2f * d / MathF.Max(2f, RingGap(r)))));
            var ring = new List<(Vector3, float)>(points.Count);
            foreach (var p in points)
            {
                var at = d < 0 ? Inset(p, rect, -d) : p.Border + p.Out * d;
                ring.Add((new Vector3(at.X, HeightOf(p, d, at), at.Y), p.U));
            }
            rings.Add((d, ring));
        }

        var mesh = new OpenVertices(16384);
        // The band is lit as level ground, as OW8's plain was (a river's channel eased out into it is no dark wedge); the hills by their faces
        for (int r = 0; r + 1 < rings.Count; r++) Zip(mesh, rings[r].Points, rings[r + 1].Points, median, level: rings[r + 1].D <= Level);
        var farTrees = trees ? Trees(mesh, rect, seed) : new List<OpenFarTree>();
        if (seaShare == null) return new Land(mesh, farTrees, median);

        // No tree stands in the sea, nor on the shore within a metre of it
        farTrees.RemoveAll(t => t.Foot.Y < seaLevel + 1f);
        return new Land(mesh, farTrees, median, SeaOver(rect, basePoints, seaShare, seaLevel), seaLevel);
    }

    /// <summary>
    /// Each border point's share of the sea (nought to one) and the sea's surface: a point is wet where the water over a
    /// metre inside the border stands 0.3 m or more over its own ground; a run of wet points <see cref="SeaRun"/> long or
    /// longer is the sea's, eased in and out over <see cref="SeaEase"/> metres along the border either side. Null where
    /// no run is long enough. The surface is the median of the sea's points' water.
    /// </summary>
    private static (float[]? Share, float Level) SeaShares(List<RingPoint> points, Dictionary<float, float> own, Func<float, float, float?> waterAt)
    {
        int n = points.Count;
        var wet = new bool[n];
        var levels = new float?[n];
        for (int i = 0; i < n; i++)
        {
            var inner = points[i].Border - points[i].Out * Under;
            if (waterAt(inner.X, inner.Y) is { } w && w >= own[points[i].U] + 0.3f) { wet[i] = true; levels[i] = w; }
        }
        // Runs of wet points round the loop, each point standing for 2 m of the border (a corner's points for none)
        var sea = new bool[n];
        int start = Array.IndexOf(wet, false);
        if (start < 0) start = 0;
        bool any = false;
        for (int k = 0; k < n;)
        {
            int i = (start + k) % n;
            if (!wet[i]) { k++; continue; }
            int length = 0;
            float metres = 0f;
            while (length < n && wet[(start + k + length) % n])
            {
                if (!points[(start + k + length) % n].OnCorner) metres += 2f;
                length++;
            }
            if (metres >= SeaRun)
            {
                for (int j = 0; j < length; j++) sea[(start + k + j) % n] = true;
                any = true;
            }
            k += Math.Max(1, length);
        }
        if (!any) return (null, 0f);

        var all = new List<float>();
        for (int i = 0; i < n; i++) if (sea[i] && levels[i] is { } l) all.Add(l);
        all.Sort();
        float level = all[all.Count / 2];

        // Eased along the loop by the metres of border between (a corner's points stand on one spot, so its fan takes
        // one share): the share at a point is smoothstepped by its distance from the nearest sea point, to nought at the ease
        var share = new float[n];
        for (int i = 0; i < n; i++)
        {
            if (sea[i]) { share[i] = 1f; continue; }
            float best = float.MaxValue;
            foreach (int step in new[] { 1, -1 })
            {
                float metres = 0f;
                for (int d = 1; d < n && metres < SeaEase; d++)
                {
                    int j = ((i + step * d) % n + n) % n;
                    // The metres between a point and the one before it (none round a corner, whose points stand on one spot)
                    metres += Vector2.Distance(points[j].Border, points[((j - step) % n + n) % n].Border);
                    if (sea[j]) { best = MathF.Min(best, metres); break; }
                }
            }
            if (best < SeaEase)
            {
                float t = 1f - best / SeaEase;
                share[i] = t * t * (3f - 2f * t);
            }
        }
        return (share, level);
    }

    /// <summary>
    /// The sea beyond the region (plan 34 · PD1's second session): a level surface at <paramref name="level"/> from the
    /// border out to <see cref="Reach"/>, laid as the water's own vertices (up, deep) over every stretch of the rings where
    /// the sea has a share; the land beyond rises through it where the share fades, so the shore is where they meet.
    /// </summary>
    private static OpenVertices SeaOver((float X0, float Z0, float X1, float Z1) rect, List<RingPoint> basePoints, float[] share, float level)
    {
        var sea = new OpenVertices(4096);
        var deep = new Vector2(OpenTerrainMesh.NoGroundDepth, 0f);
        var white = new Raylib_cs.Color(255, 255, 255, 255);
        float[] distances = { 0f, Near, 12f, 28f, 60f, Level, 215f, 345f, 515f, 730f, 1010f, 1380f, 1900f, Reach };
        int n = basePoints.Count;
        for (int i = 0; i < n; i++)
        {
            int j = (i + 1) % n;
            if (share[i] <= 0f && share[j] <= 0f) continue;
            var a = basePoints[i];
            var b = basePoints[j];
            for (int r = 0; r + 1 < distances.Length; r++)
            {
                Vector3 P(RingPoint p, float d) { var at = p.Border + p.Out * d; return new Vector3(at.X, level, at.Y); }
                Vector3 a0 = P(a, distances[r]), b0 = P(b, distances[r]), a1 = P(a, distances[r + 1]), b1 = P(b, distances[r + 1]);
                // Facing up, as the water's own: (a0, a1, b0) and (b0, a1, b1) wound so the cross of their edges points to the sky
                AddUp(sea, a0, a1, b0, deep, white);
                AddUp(sea, b0, a1, b1, deep, white);
            }
        }
        return sea;
    }

    private static void AddUp(OpenVertices mesh, Vector3 a, Vector3 b, Vector3 c, Vector2 depth, Raylib_cs.Color colour)
    {
        var n = Vector3.Cross(b - a, c - a);
        if (n.LengthSquared() < 1e-8f) return;
        if (n.Y < 0) (b, c) = (c, b);
        mesh.Add(a, Vector3.UnitY, depth, colour);
        mesh.Add(b, Vector3.UnitY, depth, colour);
        mesh.Add(c, Vector3.UnitY, depth, colour);
    }

    /// <summary>How far ring <paramref name="r"/> lies from the next one out, in metres (the last, from the one before).</summary>
    private static float RingGap(int r) => r + 1 < Rings.Length ? Rings[r + 1] - Rings[r] : Rings[r] - Rings[r - 1];

    /// <summary>How far the hills have risen at a distance out: nought to the band's end, whole by <see cref="HillsFull"/>.</summary>
    public static float Rise(float d) => HillsRise * Smooth(Level, HillsFull, d);

    private static float Smooth(float from, float to, float x)
    {
        float t = Math.Clamp((x - from) / (to - from), 0f, 1f);
        return t * t * (3f - 2f * t);
    }

    /// <summary>A point of the first ring, inside the border: the rectangle inset, its corners kept square.</summary>
    private static Vector2 Inset(RingPoint p, (float X0, float Z0, float X1, float Z1) rect, float by)
    {
        float x = Math.Clamp(p.Border.X, rect.X0 + by, rect.X1 - by), z = Math.Clamp(p.Border.Y, rect.Z0 + by, rect.Z1 - by);
        if (!p.OnCorner) return p.Border - p.Out * by;
        return new Vector2(x, z);
    }

    /// <summary>
    /// The loop round a rectangle, clockwise from its north-west corner (as seen from above, x east and z south: north side
    /// west to east, then east, south and west): every <paramref name="spacing"/> metres along each side, starting at the
    /// side's own first corner, then <paramref name="corner"/> points round the corner at its end, each looking out its own way.
    /// </summary>
    internal static List<RingPoint> Loop((float X0, float Z0, float X1, float Z1) rect, float spacing, int corner)
    {
        var nw = new Vector2(rect.X0, rect.Z0);
        var ne = new Vector2(rect.X1, rect.Z0);
        var se = new Vector2(rect.X1, rect.Z1);
        var sw = new Vector2(rect.X0, rect.Z1);
        var sides = new (Vector2 From, Vector2 To, Vector2 Out)[]
        {
            (nw, ne, new Vector2(0, -1)), (ne, se, new Vector2(1, 0)), (se, sw, new Vector2(0, 1)), (sw, nw, new Vector2(-1, 0))
        };
        var loop = new List<RingPoint>();
        for (int k = 0; k < 4; k++)
        {
            var (from, to, outward) = sides[k];
            float length = Vector2.Distance(from, to);
            int steps = Math.Max(1, (int)MathF.Ceiling(length / spacing));
            for (int i = 0; i < steps; i++) loop.Add(new RingPoint(k + 0.5f * i / steps, Vector2.Lerp(from, to, i / (float)steps), outward, false));
            // Round the corner at the side's end, from this side's way out toward the next's (which the next side's first point takes)
            var next = sides[(k + 1) % 4].Out;
            for (int j = 0; j < corner; j++)
            {
                float a = MathF.PI / 2f * j / corner;
                var way = Vector2.Normalize(outward * MathF.Cos(a) + next * MathF.Sin(a));
                loop.Add(new RingPoint(k + 0.5f + 0.5f * j / corner, to, way, j > 0));
            }
        }
        return loop;
    }

    /// <summary>
    /// Two rings joined by triangles, walking round both together by their points' <c>U</c>: whichever ring's next point
    /// comes first along the loop is taken next. Every triangle faces up, and is covered by its slope and height.
    /// </summary>
    private static void Zip(OpenVertices mesh, List<(Vector3 At, float U)> a, List<(Vector3 At, float U)> b, float median, bool level)
    {
        int i = 0, j = 0, na = a.Count, nb = b.Count;
        float Next(List<(Vector3 At, float U)> ring, int index) => index + 1 < ring.Count ? ring[index + 1].U : 4f + ring[0].U;
        while (i < na || j < nb)
        {
            var ai = a[i % na].At;
            var bj = b[j % nb].At;
            bool stepA = j >= nb || (i < na && Next(a, i) <= Next(b, j));
            if (stepA)
            {
                Tri(mesh, ai, a[(i + 1) % na].At, bj, median, level);
                i++;
            }
            else
            {
                Tri(mesh, ai, b[(j + 1) % nb].At, bj, median, level);
                j++;
            }
        }
    }

    private static void Tri(OpenVertices mesh, Vector3 a, Vector3 b, Vector3 c, float median, bool level)
    {
        var n = Vector3.Cross(b - a, c - a);
        // Nothing from above: a sliver standing upright (the first ring's corner, whose points stand on one spot at
        // their own heights, under the region's ground) is left out, as a triangle of no size is
        if (n.LengthSquared() < 1e-10f || MathF.Abs(n.Y) <= 1e-6f * n.Length()) return;
        if (n.Y < 0) { (b, c) = (c, b); n = -n; }
        var face = Vector3.Normalize(n);
        // Each corner its own cover by its height and the face's slope, so the program draws their borders texel by
        // texel across the triangles, as the region's own ground's, and no long triangle makes a sawtooth of them. The
        // band is level ground to the light and to the covers alike: a river's channel eased out into it is neither a
        // dark wedge of shade nor a streak of the deep grass
        var up = level ? Vector3.UnitY : face;
        float slope = level ? 1f : face.Y;
        var covers = OpenTerrainMesh.Covers(CoverOf(slope, a.Y - median), CoverOf(slope, b.Y - median), CoverOf(slope, c.Y - median));
        mesh.Add(a, up, covers, OpenTerrainMesh.Corners[0]);
        mesh.Add(b, up, covers, OpenTerrainMesh.Corners[1]);
        mesh.Add(c, up, covers, OpenTerrainMesh.Corners[2]);
    }

    /// <summary>The far land's cover by its slope (the face's normal's height) and its height over the median: as the baker's rules have them, in broad strokes.</summary>
    public static OpenCover CoverOf(float normalY, float over)
    {
        if (normalY < MathF.Cos(63f * MathF.PI / 180f)) return OpenCover.RockDark;
        if (normalY < MathF.Cos(40f * MathF.PI / 180f)) return OpenCover.Rock;
        if (over > 22f) return OpenCover.Highland;
        if (normalY < MathF.Cos(25f * MathF.PI / 180f)) return OpenCover.GrassDeep;
        return OpenCover.Grass;
    }

    /// <summary>
    /// The far forests: a cell of <see cref="TreeCell"/> metres on the world's own grid holds a tree where the forests'
    /// noise is over a share and the land under it is gentle, between <see cref="TreesFrom"/> and <see cref="TreesTo"/> out
    /// from the border; its foot is on the land's own triangles (found through a coarse index of them), its kind by
    /// another noise, so a forest is mostly pines or mostly round trees.
    /// </summary>
    private static List<OpenFarTree> Trees(OpenVertices mesh, (float X0, float Z0, float X1, float Z1) rect, uint seed)
    {
        const float IndexCell = 48f;
        var index = new Dictionary<(int, int), List<int>>();
        for (int t = 0; t < mesh.Count; t += 3)
        {
            var a = mesh.Positions[t]; var b = mesh.Positions[t + 1]; var c = mesh.Positions[t + 2];
            int x0 = (int)MathF.Floor(MathF.Min(a.X, MathF.Min(b.X, c.X)) / IndexCell), x1 = (int)MathF.Floor(MathF.Max(a.X, MathF.Max(b.X, c.X)) / IndexCell);
            int z0 = (int)MathF.Floor(MathF.Min(a.Z, MathF.Min(b.Z, c.Z)) / IndexCell), z1 = (int)MathF.Floor(MathF.Max(a.Z, MathF.Max(b.Z, c.Z)) / IndexCell);
            for (int z = z0; z <= z1; z++)
                for (int x = x0; x <= x1; x++)
                {
                    if (!index.TryGetValue((x, z), out var list)) index[(x, z)] = list = new List<int>();
                    list.Add(t);
                }
        }

        var forests = new Hills(seed ^ 0x2545F491u);
        var trees = new List<OpenFarTree>();
        int c0 = (int)MathF.Floor((rect.X0 - TreesTo) / TreeCell), c1 = (int)MathF.Ceiling((rect.X1 + TreesTo) / TreeCell);
        int r0 = (int)MathF.Floor((rect.Z0 - TreesTo) / TreeCell), r1 = (int)MathF.Ceiling((rect.Z1 + TreesTo) / TreeCell);
        for (int r = r0; r <= r1; r++)
            for (int c = c0; c <= c1; c++)
            {
                uint h = SplitMix.Mix(seed ^ 0x9E3779B9u, (uint)c, (uint)r);
                float x = (c + 0.15f + 0.7f * (h & 1023) / 1023f) * TreeCell, z = (r + 0.15f + 0.7f * ((h >> 10) & 1023) / 1023f) * TreeCell;
                float dx = MathF.Max(0f, MathF.Max(rect.X0 - x, x - rect.X1)), dz = MathF.Max(0f, MathF.Max(rect.Z0 - z, z - rect.Z1));
                float d = MathF.Sqrt(dx * dx + dz * dz);
                if (d < TreesFrom || d > TreesTo) continue;
                float forest = forests.Raw(new Vector2(x, z) * 1.6f);
                if (forest < 0.53f || ((h >> 20) & 1023) > 900) continue;
                if (FootOn(mesh, index, IndexCell, x, z) is not { } foot || foot.NormalY < 0.86f) continue;
                bool pine = forests.Raw(new Vector2(z, x) * 0.7f + new Vector2(311, 97)) > 0.48f;
                int variant = (int)((h >> 4) % OpenTree.Variants);
                float scale = 0.95f + 0.45f * ((h >> 24) & 255) / 255f;
                // A little into the ground, so no far tree stands over a dip of its triangle
                trees.Add(new OpenFarTree(new Vector3(x, foot.Y - 0.15f, z), scale, (pine ? 0 : OpenTree.Variants) + variant));
            }
        return trees;
    }

    /// <summary>The land's height over a point and its face's slope there, from the triangles of the cell it falls in.</summary>
    private static (float Y, float NormalY)? FootOn(OpenVertices mesh, Dictionary<(int, int), List<int>> index, float cell, float x, float z)
    {
        if (!index.TryGetValue(((int)MathF.Floor(x / cell), (int)MathF.Floor(z / cell)), out var list)) return null;
        foreach (int t in list)
        {
            if (OpenTerrainMesh.HeightIn(x, z, mesh.Positions[t], mesh.Positions[t + 1], mesh.Positions[t + 2]) is { } y)
                return (y, mesh.Normals[t].Y);
        }
        return null;
    }

    /// <summary>
    /// The far hills: value noise of three octaves over the ground (wavelengths of 430, 190 and 85 m) on a lattice of the
    /// seed's own numbers, shaped so that there are valleys between the hills, 0 to 1. Single precision and the four
    /// operations alone, the same on any machine.
    /// </summary>
    private sealed class Hills
    {
        private readonly uint seed;
        public Hills(uint seed) => this.seed = seed;

        private float Lattice(int x, int z) => (SplitMix.Mix(seed, (uint)x, (uint)z) & 0xFFFF) / 65535f;

        private float Noise(Vector2 p)
        {
            float fx = MathF.Floor(p.X), fz = MathF.Floor(p.Y);
            int ix = (int)fx, iz = (int)fz;
            float tx = p.X - fx, tz = p.Y - fz;
            tx = tx * tx * (3f - 2f * tx);
            tz = tz * tz * (3f - 2f * tz);
            float a = Lattice(ix, iz), b = Lattice(ix + 1, iz), c = Lattice(ix, iz + 1), d = Lattice(ix + 1, iz + 1);
            return a + (b - a) * tx + (c - a) * tz + (a - b - c + d) * tx * tz;
        }

        /// <summary>The three octaves mixed, 0 to 1.</summary>
        public float Raw(Vector2 p) => Noise(p / 430f) * 0.55f + Noise(p / 190f + new Vector2(17.3f, 41.1f)) * 0.3f + Noise(p / 85f + new Vector2(83.7f, 29.9f)) * 0.15f;

        /// <summary>The hills' height at a point, 0 to 1: the mix shaped so the low half is valley floor.</summary>
        public float At(Vector2 p)
        {
            float n = Raw(p);
            float t = Math.Clamp((n - 0.3f) / 0.5f, 0f, 1f);
            return t * t * (3f - 2f * t);
        }
    }
}
