using System;
using System.Collections.Generic;

namespace PokemonPlatinumEngine.OpenWorld;

/// <summary>
/// A path over an open map's navigation mesh (plan 32 Â· OW4, "a path is A* over the polygons with a funnel through
/// their shared edges"): the polygons from the start's to the goal's by A* (each polygon entered at the middle of the
/// link it was come by, the cost the distance in metres from one such point to the next, the heuristic the straight
/// distance to the goal; ties to the lower polygon), then the corners a body walks to, pulled taut through the shared
/// edges by the funnel, a link off the mesh (a kerb stepped, a ledge dropped) a corner at its take-off and another
/// where it lands. GPU-free; only the four operations and the square root, so two machines find the same path.
/// </summary>
public sealed class NavPath
{
    /// <summary>The corners in order, from the start to the goal, in millimetres.</summary>
    public required IReadOnlyList<OpenPos> Corners { get; init; }

    /// <summary>The polygons passed through, from the start's to the goal's.</summary>
    public required IReadOnlyList<int> Polygons { get; init; }

    /// <summary>The links crossed, one fewer than the polygons.</summary>
    public required IReadOnlyList<int> Links { get; init; }

    /// <summary>The corners' length end to end, in metres.</summary>
    public required double Length { get; init; }

    /// <summary>How many polygons A* closed on the way (what a path cost to find).</summary>
    public required int Visited { get; init; }

    /// <summary>Every flag of the links crossed: whether the path swims, drops, steps off the mesh.</summary>
    public required OpenNavFlags Uses { get; init; }

    /// <summary>How far, in metres, a start or a goal off the mesh is taken to the nearest polygon.</summary>
    public const double SnapReach = 1.5;

    /// <summary>
    /// The path from one place to another crossing only links whose every flag <paramref name="allowed"/> has (walking
    /// and drops unless told otherwise), passing by every polygon <paramref name="barred"/> says may not be used (the
    /// start's and the goal's aside); null where there is none. A place a little off the mesh is taken to its nearest
    /// polygon within <see cref="SnapReach"/>.
    /// </summary>
    public static NavPath? Find(OpenNavMesh mesh, OpenPos from, OpenPos to, OpenNavFlags allowed = OpenNavFlags.Walk | OpenNavFlags.Drop,
        Func<int, bool>? barred = null)
    {
        if (mesh.Nearest(from, SnapReach) is not { } start || mesh.Nearest(to, SnapReach) is not { } goal) return null;
        var goalAt = OpenVec.Of(to);
        var startAt = OpenVec.Of(from);

        int n = mesh.Polygons.Count;
        var cost = new double[n];
        Array.Fill(cost, double.MaxValue);
        var entry = new OpenVec[n];
        var cameBy = new int[n];
        Array.Fill(cameBy, -1);
        var closed = new bool[n];
        var open = new PriorityQueue<int, (double, int)>();
        cost[start.Polygon] = 0;
        entry[start.Polygon] = startAt;
        open.Enqueue(start.Polygon, ((startAt - goalAt).Length, start.Polygon));
        int visited = 0;
        bool found = false;
        while (open.Count > 0)
        {
            int p = open.Dequeue();
            if (closed[p]) continue;
            closed[p] = true;
            visited++;
            if (p == goal.Polygon) { found = true; break; }
            foreach (int li in mesh.LinksFrom(p))
            {
                var link = mesh.Links[li];
                int q = link.To;
                if (closed[q] || (link.Flags & ~allowed) != 0) continue;
                if (barred != null && q != goal.Polygon && barred(q)) continue;
                OpenVec at;
                double step;
                if (link.OffMesh)
                {
                    var off = OpenVec.Of(link.A);
                    at = OpenVec.Of(link.Land);
                    step = (off - entry[p]).Length + (at - off).Length;
                }
                else
                {
                    at = link.Middle;
                    step = (at - entry[p]).Length;
                }
                double c = cost[p] + step;
                if (c >= cost[q]) continue;
                cost[q] = c;
                entry[q] = at;
                cameBy[q] = li;
                open.Enqueue(q, (c + (at - goalAt).Length, q));
            }
        }
        if (!found) return null;

        var corridor = new List<int>();
        for (int p = goal.Polygon; cameBy[p] >= 0; p = mesh.Links[cameBy[p]].From) corridor.Add(cameBy[p]);
        corridor.Reverse();
        var polygons = new List<int> { start.Polygon };
        var uses = OpenNavFlags.None;
        foreach (int li in corridor) { polygons.Add(mesh.Links[li].To); uses |= mesh.Links[li].Flags; }

        var corners = Pull(mesh, start.Point, goal.Point, corridor);
        // The places asked from and to, where they lay a little off the mesh
        if (!start.Point.Equals(from)) corners.Insert(0, from);
        if (!goal.Point.Equals(to)) corners.Add(to);
        for (int i = corners.Count - 1; i > 0; i--)
            if (corners[i].Equals(corners[i - 1])) corners.RemoveAt(i);
        double length = 0;
        for (int i = 1; i < corners.Count; i++) length += (OpenVec.Of(corners[i]) - OpenVec.Of(corners[i - 1])).Length;
        if (uses == OpenNavFlags.None) uses = OpenNavFlags.Walk;
        return new NavPath { Corners = corners, Polygons = polygons, Links = corridor, Length = length, Visited = visited, Uses = uses };
    }

    /// <summary>
    /// The corners through a corridor of links: the funnel pulled through each run of shared edges, broken at each link
    /// off the mesh into its take-off and its landing.
    /// </summary>
    private static List<OpenPos> Pull(OpenNavMesh mesh, OpenPos start, OpenPos goal, List<int> corridor)
    {
        var corners = new List<OpenPos>();
        var run = new List<(OpenVec Left, OpenVec Right)>();
        var from = OpenVec.Of(start);
        foreach (int li in corridor)
        {
            var link = mesh.Links[li];
            if (link.OffMesh)
            {
                var off = OpenVec.Of(link.A);
                Funnel(from, run, off, corners);
                corners.Add(link.A);
                corners.Add(link.Land);
                run.Clear();
                from = OpenVec.Of(link.Land);
            }
            else
            {
                // Which end is on the left as the edge is crossed: seen from the polygon left behind
                var behind = OpenVec.Of(mesh.Polygons[link.From].Hub);
                var a = OpenVec.Of(link.A);
                var b = OpenVec.Of(link.B);
                run.Add(Area(behind, a, b) < 0 ? (b, a) : (a, b));
            }
        }
        var end = OpenVec.Of(goal);
        Funnel(from, run, end, corners);
        corners.Add(goal);
        if (corners.Count == 0 || !corners[0].Equals(start)) corners.Insert(0, start);
        return corners;
    }

    /// <summary>Twice the signed area of a, b, c on the ground, as the funnel reads it (Mononen's triarea2).</summary>
    private static double Area(OpenVec a, OpenVec b, OpenVec c)
    {
        double ax = b.X - a.X, ay = b.Z - a.Z, bx = c.X - a.X, by = c.Z - a.Z;
        return bx * ay - ax * by;
    }

    /// <summary>How far, in metres, a corner the funnel bends at is drawn along its edge away from the end it lies on (half the edge at most).</summary>
    public const double CornerMargin = 0.25;

    /// <summary>
    /// A corner at one end of an edge, drawn <see cref="CornerMargin"/> toward the other end: the end lies on the mesh's
    /// border, by a wall's foot or a lip, and a body steering for it passes a little off it.
    /// </summary>
    private static OpenPos Inside(OpenVec corner, OpenVec other)
    {
        double dx = other.X - corner.X, dy = other.Y - corner.Y, dz = other.Z - corner.Z, length = Math.Sqrt(dx * dx + dz * dz);
        if (length < 1e-6) return OpenPos.FromMetres(corner.X, corner.Y, corner.Z);
        double s = Math.Min(CornerMargin, length / 2) / length;
        return OpenPos.FromMetres(corner.X + dx * s, corner.Y + dy * s, corner.Z + dz * s);
    }

    private static bool Same(OpenVec a, OpenVec b) => (a.X - b.X) * (a.X - b.X) + (a.Z - b.Z) * (a.Z - b.Z) < 1e-8;

    /// <summary>
    /// The simple stupid funnel (Mononen's) from <paramref name="start"/> through <paramref name="portals"/> to
    /// <paramref name="end"/>: the corners it bends at are added (the start and the end are not).
    /// </summary>
    private static void Funnel(OpenVec start, List<(OpenVec Left, OpenVec Right)> portals, OpenVec end, List<OpenPos> corners)
    {
        var all = new List<(OpenVec Left, OpenVec Right)>(portals.Count + 2) { (start, start) };
        all.AddRange(portals);
        all.Add((end, end));
        var apex = start; var left = start; var right = start;
        int apexIndex = 0, leftIndex = 0, rightIndex = 0;
        int guard = all.Count * all.Count + 16;
        for (int i = 1; i < all.Count && guard-- > 0; i++)
        {
            var (l, r) = all[i];
            // The right side
            if (Area(apex, right, r) <= 0)
            {
                if (Same(apex, right) || Area(apex, left, r) > 0)
                {
                    right = r; rightIndex = i;
                }
                else
                {
                    corners.Add(Inside(left, all[leftIndex].Right));
                    apex = left; apexIndex = leftIndex;
                    left = right = apex; leftIndex = rightIndex = apexIndex;
                    i = apexIndex;
                    continue;
                }
            }
            // The left side
            if (Area(apex, left, l) >= 0)
            {
                if (Same(apex, left) || Area(apex, right, l) < 0)
                {
                    left = l; leftIndex = i;
                }
                else
                {
                    corners.Add(Inside(right, all[rightIndex].Left));
                    apex = right; apexIndex = rightIndex;
                    left = right = apex; leftIndex = rightIndex = apexIndex;
                    i = apexIndex;
                    continue;
                }
            }
        }
    }
}
