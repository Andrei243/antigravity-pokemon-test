using System;
using System.Collections.Generic;
using System.Linq;

namespace PokemonPlatinumEngine.OpenWorld;

/// <summary>
/// A way that is no link of the mesh (plan 32 · OW4, for OW5): a door within the map, a scripted warp, a person who
/// carries the player across. Whoever reaches <see cref="From"/> reaches <see cref="To"/>.
/// </summary>
public readonly record struct OpenWay(string Name, OpenPos From, OpenPos To);

/// <summary>
/// What can be come to in an open map (plan 32 · OW4, "OpenWalk"; the open counterpart of the grid's
/// <c>WorldWalk</c>): a flood over the navigation mesh's polygons from the places given, through every link whose
/// flags are allowed (walking and drops, and swimming with the grant), and through the ways that are no link (doors,
/// scripted warps) as their far ends come to be reached. A ledge is a way down only, so a flood from below never
/// reaches its top. GPU-free and the same on any machine: what is reached hangs on nothing but the mesh, the places
/// and the flags.
/// </summary>
public sealed class OpenWalk
{
    private readonly OpenNavMesh mesh;
    private readonly bool[] reached;

    /// <summary>The polygons reached, lowest first.</summary>
    public IReadOnlyList<int> Reached { get; }

    /// <summary>The ways that are no link that were taken, by name, in the order they were.</summary>
    public IReadOnlyList<string> WaysTaken { get; }

    private OpenWalk(OpenNavMesh mesh, bool[] reached, List<string> ways)
    {
        this.mesh = mesh;
        this.reached = reached;
        var list = new List<int>();
        for (int i = 0; i < reached.Length; i++) if (reached[i]) list.Add(i);
        Reached = list;
        WaysTaken = ways;
    }

    /// <summary>
    /// The flood from <paramref name="starts"/> (each taken to its nearest polygon within <see cref="NavPath.SnapReach"/>;
    /// one off the mesh starts nothing).
    /// </summary>
    public static OpenWalk From(OpenNavMesh mesh, IEnumerable<OpenPos> starts, OpenNavFlags allowed = OpenNavFlags.Walk | OpenNavFlags.Drop,
        IEnumerable<OpenWay>? ways = null)
    {
        var reached = new bool[mesh.Polygons.Count];
        var stack = new Stack<int>();
        void Seed(OpenPos at)
        {
            if (mesh.Nearest(at, NavPath.SnapReach) is { } found && !reached[found.Polygon])
            {
                reached[found.Polygon] = true;
                stack.Push(found.Polygon);
            }
        }
        foreach (var start in starts) Seed(start);
        var pending = (ways ?? Enumerable.Empty<OpenWay>()).Select(w => (Way: w, From: mesh.Nearest(w.From, NavPath.SnapReach)?.Polygon)).ToList();
        var taken = new List<string>();
        while (true)
        {
            while (stack.Count > 0)
            {
                int p = stack.Pop();
                foreach (int li in mesh.LinksFrom(p))
                {
                    var link = mesh.Links[li];
                    if ((link.Flags & ~allowed) != 0 || reached[link.To]) continue;
                    reached[link.To] = true;
                    stack.Push(link.To);
                }
            }
            // A way whose near end is reached opens its far end; again until none does
            bool more = false;
            for (int i = 0; i < pending.Count; i++)
            {
                if (pending[i].From is not { } from || !reached[from]) continue;
                taken.Add(pending[i].Way.Name);
                Seed(pending[i].Way.To);
                pending.RemoveAt(i--);
                more = true;
            }
            if (!more) break;
        }
        return new OpenWalk(mesh, reached, taken);
    }

    /// <summary>What can be come to in a map from <paramref name="starts"/> with what the region grants; null for a map with no navigation mesh.</summary>
    public static OpenWalk? Of(OpenMap map, MovementGrants grants, IEnumerable<OpenPos> starts, IEnumerable<OpenWay>? ways = null) =>
        map.Land?.Nav is { } nav ? From(nav, starts, OpenNavMesh.Allowed(grants), ways) : null;

    /// <summary>Whether a polygon was reached.</summary>
    public bool Reaches(int polygon) => reached[polygon];

    /// <summary>Whether a place can be come to: its nearest polygon within <see cref="NavPath.SnapReach"/> was reached.</summary>
    public bool Reaches(OpenPos pos) => mesh.Nearest(pos, NavPath.SnapReach) is { } found && reached[found.Polygon];

    /// <summary>The markers that can't be come to, in the order given.</summary>
    public List<OpenMarker> Unreached(IEnumerable<OpenMarker> markers) => markers.Where(m => !Reaches(m.Pos)).ToList();
}
