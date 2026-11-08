using System;

namespace PokemonPlatinumEngine.OpenWorld;

/// <summary>
/// A script's walk to a marker (plan 32 · OW5, <c>walkto &lt;who&gt; &lt;marker&gt;</c>): the capsule steered at a full tilt
/// straight at the marker a tick at a time, by the same rules as the stick (it slides along what is in the way, stops
/// at deep water it may not swim), until its feet are within <see cref="Within"/> of it on the ground. There it is
/// put on the marker itself, facing the marker's way, so a walk played and a walk a skipped scene ends at once
/// (<see cref="Finish"/>) leave the walker on the same millimetre. A walk that comes no nearer for
/// <see cref="GiveUpTicks"/> ticks is given up where it stands, as the grid's scripted walk gives up at a wall. The
/// way round a wall is OW4's navigation; this walk goes straight. GPU-free; no clock but the ticks it is stepped by.
/// </summary>
public sealed class OpenMarkerWalk
{
    /// <summary>How near the marker on the ground the feet come before they are put on it, in millimetres.</summary>
    public const int Within = 100;

    /// <summary>Ticks without coming nearer before the walk is given up (three quarters of a second, as the grid's).</summary>
    public const int GiveUpTicks = 90;

    /// <summary>Who walks: <c>player</c>, or someone of the map (OW6).</summary>
    public string Who { get; }

    public OpenMarker Target { get; }
    public bool Run { get; }

    /// <summary>Whether the walk is over: arrived and put on the marker, or given up.</summary>
    public bool Done { get; private set; }

    /// <summary>Whether it was given up short of the marker.</summary>
    public bool GaveUp { get; private set; }

    private long nearest = long.MaxValue;
    private int stalled;

    public OpenMarkerWalk(string who, OpenMarker target, bool run)
    {
        Who = who;
        Target = target;
        Run = run;
    }

    /// <summary>The stick for the next tick: a full tilt at the marker, a share of it on the last stretch so as not to pass it; at rest once done.</summary>
    public OpenInput InputFor(OpenPlayer walker)
    {
        if (Done) return OpenInput.None;
        var pos = walker.Pos;
        double dx = Target.Pos.X - pos.X, dz = Target.Pos.Z - pos.Z;
        double distance = Math.Sqrt(dx * dx + dz * dz);
        if (distance == 0) return OpenInput.None;
        // A tick's stride at a full tilt, in millimetres: a shorter way left asks for less of the stick
        double stride = (Run ? OpenBody.RunSpeed : OpenBody.WalkSpeed) * OpenPos.PerMetre / OpenBody.TicksPerSecond;
        double tilt = Math.Min(1, distance / stride);
        return new OpenInput(dx / distance * tilt, dz / distance * tilt, Run);
    }

    /// <summary>After a tick: arrived (put on the marker), given up, or on its way.</summary>
    public void After(OpenPlayer walker)
    {
        if (Done) return;
        long left = walker.Pos.GroundDistanceSquared(Target.Pos);
        if (left <= (long)Within * Within)
        {
            Finish(walker);
            return;
        }
        if (left < nearest)
        {
            nearest = left;
            stalled = 0;
        }
        else if (++stalled >= GiveUpTicks)
        {
            Done = GaveUp = true;
        }
    }

    /// <summary>The walk ended at once: the walker put on the marker, facing its way (what a hurry does, and the walk's own end).</summary>
    public void Finish(OpenPlayer walker)
    {
        walker.Place(Target.Pos, Target.Yaw);
        Done = true;
    }
}
