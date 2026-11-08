using System;
using System.Collections.Generic;

namespace PokemonPlatinumEngine.OpenWorld;

/// <summary>What a walk of someone of an open map is for (plan 32 · OW6): who started it decides what its end does.</summary>
public enum OpenWalkKind
{
    /// <summary>A script's <c>walkto</c>: ended on its marker, played or skipped alike.</summary>
    Script,

    /// <summary>A trainer's walk up to the player, ended a talking distance short.</summary>
    WalkUp,

    /// <summary>A leg of someone's wandering, ended where it reaches.</summary>
    Wander
}

/// <summary>
/// A walk of someone's capsule along a path's corners (plan 32 · OW6, on OW4's <see cref="NavPath"/>): steered at a
/// full tilt at each corner in turn by the stick's own rules, the next corner taken once the feet are within
/// <see cref="CornerReach"/> of it, a share of the tilt on the last stretch, and ended within <see cref="Within"/> of
/// the last (or <see cref="StopShort"/> short of it, a trainer's walk up). A walk to a marker is put on the marker
/// facing its way at its end, so a walk played and a walk a skipped scene ends at once (<see cref="Finish"/>) leave
/// the walker on the same millimetre, as OW5's walk of the player does. A walk that comes no nearer its corner for
/// <see cref="GiveUpTicks"/> ticks is given up where it stands. GPU-free; no clock but the ticks it is stepped by.
/// </summary>
public sealed class OpenPathWalk
{
    /// <summary>How near a corner on the ground the feet come before the next is steered for, in millimetres (OW4's tests walk paths so).</summary>
    public const int CornerReach = 150;

    /// <summary>How near the last corner on the ground the feet come before the walk is over, in millimetres.</summary>
    public const int Within = OpenMarkerWalk.Within;

    /// <summary>Ticks without coming nearer before the walk is given up (three quarters of a second, as the grid's).</summary>
    public const int GiveUpTicks = OpenMarkerWalk.GiveUpTicks;

    /// <summary>The places steered for, in order; the last is where the walk is going.</summary>
    public IReadOnlyList<OpenPos> Corners { get; }

    public OpenWalkKind Kind { get; }
    public bool Run { get; }

    /// <summary>The marker a script's walk ends on (put on it facing its way), or null.</summary>
    public OpenMarker? Target { get; }

    /// <summary>How far short of the last corner, in millimetres, the walk is over (a trainer's talking distance); <see cref="Within"/> unless told.</summary>
    public int StopShort { get; }

    /// <summary>The corner steered for now.</summary>
    public int Next { get; private set; }

    public bool Done { get; private set; }
    public bool GaveUp { get; private set; }

    private long nearest = long.MaxValue;
    private int stalled;

    public OpenPathWalk(IReadOnlyList<OpenPos> corners, OpenWalkKind kind, bool run = false, OpenMarker? target = null, int stopShort = Within)
    {
        if (corners.Count == 0) throw new ArgumentException("a walk goes somewhere", nameof(corners));
        Corners = corners;
        Kind = kind;
        Run = run;
        Target = target;
        StopShort = Math.Max(Within, stopShort);
    }

    /// <summary>
    /// A walk from one place to another over the map's navigation mesh, through links whose flags <paramref name="allowed"/>
    /// has, by polygons <paramref name="barred"/> doesn't bar: the path's corners after its first. With no mesh, or no
    /// path on it, it goes straight at the place where <paramref name="straight"/> says so (a script's walk, as OW5's),
    /// and is null otherwise.
    /// </summary>
    public static OpenPathWalk? Over(OpenMap map, OpenPos from, OpenPos to, OpenWalkKind kind, bool run = false, OpenMarker? target = null,
        int stopShort = Within, OpenNavFlags allowed = OpenNavFlags.Walk | OpenNavFlags.Drop, Func<int, bool>? barred = null, bool straight = false)
    {
        var corners = new List<OpenPos>();
        if (map.Land?.Nav is { } mesh && NavPath.Find(mesh, from, to, allowed, barred) is { } path)
        {
            for (int i = 1; i < path.Corners.Count; i++) corners.Add(path.Corners[i]);
            if (corners.Count == 0) corners.Add(to);
        }
        else if (straight) corners.Add(to);
        else return null;
        return new OpenPathWalk(corners, kind, run, target, stopShort);
    }

    /// <summary>The stick for the walker's next tick: a full tilt at the corner steered for, a share of it on the last stretch; at rest once done.</summary>
    public OpenInput InputFor(OpenBody walker)
    {
        if (Done) return OpenInput.None;
        var pos = walker.Pos;
        var corner = Corners[Next];
        double dx = corner.X - pos.X, dz = corner.Z - pos.Z;
        double distance = Math.Sqrt(dx * dx + dz * dz);
        if (distance == 0) return OpenInput.None;
        double tilt = 1;
        if (Next == Corners.Count - 1)
        {
            // A tick's stride at a full tilt, in millimetres: a shorter way left asks for less of the stick
            double stride = (Run ? OpenBody.RunSpeed : OpenBody.WalkSpeed) * OpenPos.PerMetre / OpenBody.TicksPerSecond;
            tilt = Math.Min(1, Math.Max(distance - (StopShort - Within), 0) / stride);
            if (tilt <= OpenBody.DeadZone) return OpenInput.None;
        }
        return new OpenInput(dx / distance * tilt, dz / distance * tilt, Run);
    }

    /// <summary>After the walker's tick: the next corner taken, the walk over, given up, or on its way. Returns whether it is over.</summary>
    public bool After(OpenBody walker)
    {
        if (Done) return true;
        var pos = walker.Pos;
        for (; ; )
        {
            long left = pos.GroundDistanceSquared(Corners[Next]);
            bool last = Next == Corners.Count - 1;
            long reach = last ? StopShort : CornerReach;
            if (left <= reach * reach)
            {
                if (last)
                {
                    Done = true;
                    return true;
                }
                Next++;
                nearest = long.MaxValue;
                stalled = 0;
                continue;
            }
            if (left < nearest)
            {
                nearest = left;
                stalled = 0;
            }
            else if (++stalled >= GiveUpTicks)
            {
                Done = GaveUp = true;
                return true;
            }
            return false;
        }
    }

    /// <summary>The walk ended at once: a walk to a marker puts its walker on it facing its way (what a hurry does, and the walk's own end); any other stops where it is.</summary>
    public void Finish(OpenPerson walker)
    {
        if (Target is { } marker) walker.PutAt(marker.Pos, marker.Yaw);
        Done = true;
    }
}
