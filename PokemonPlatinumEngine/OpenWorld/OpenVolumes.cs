using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.OpenWorld;

/// <summary>What shape a volume is (plan 32, "Volumes").</summary>
public enum OpenShapeKind
{
    /// <summary>An upright box lined up with the axes.</summary>
    Box,

    /// <summary>An upright cylinder: a circle on the ground between two heights.</summary>
    Cylinder
}

/// <summary>
/// A volume's shape (plan 32 · OW5): an upright box lined up with the axes, or an upright cylinder, in millimetres. A
/// cylinder is kept as the box round it (its middle is the box's, its radius half the box's width), so both are two
/// corners. A place is inside when it is no lower than the bottom and lower than the top, and within the box (its
/// west and north sides in, its east and south out) or strictly within the circle. Whole numbers throughout, so two
/// machines agree on every crossing. GPU-free.
/// </summary>
public sealed record OpenShape(OpenShapeKind Kind, OpenPos Min, OpenPos Max)
{
    /// <summary>A box from its two corners.</summary>
    public static OpenShape Box(OpenPos min, OpenPos max) => new(OpenShapeKind.Box, min, max);

    /// <summary>A cylinder standing at <paramref name="x"/>, <paramref name="z"/> from <paramref name="bottom"/> to <paramref name="top"/>.</summary>
    public static OpenShape Cylinder(int x, int z, int bottom, int top, int radius) =>
        new(OpenShapeKind.Cylinder, new OpenPos(x - radius, bottom, z - radius), new OpenPos(x + radius, top, z + radius));

    /// <summary>The middle of the shape, on its floor.</summary>
    public OpenPos Base => new((int)(((long)Min.X + Max.X) / 2), Min.Y, (int)(((long)Min.Z + Max.Z) / 2));

    /// <summary>The middle of the shape, half way up.</summary>
    public OpenPos Middle => new(Base.X, (int)(((long)Min.Y + Max.Y) / 2), Base.Z);

    /// <summary>A cylinder's radius, in millimetres (a box's half width).</summary>
    public int Radius => (int)(((long)Max.X - Min.X) / 2);

    /// <summary>Whether a place is inside.</summary>
    public bool Contains(OpenPos p)
    {
        if (p.Y < Min.Y || p.Y >= Max.Y) return false;
        if (Kind == OpenShapeKind.Box) return p.X >= Min.X && p.X < Max.X && p.Z >= Min.Z && p.Z < Max.Z;
        long dx = (long)p.X - Base.X, dz = (long)p.Z - Base.Z, r = Radius;
        return dx * dx + dz * dz < r * r;
    }

    /// <summary>Refuses a shape with no inside (a corner not below the other, a cylinder whose square isn't square).</summary>
    public OpenShape Checked(string name)
    {
        if (Min.X >= Max.X || Min.Y >= Max.Y || Min.Z >= Max.Z) throw new ArgumentException($"the volume {name} has no inside: {Min} to {Max}");
        if (Kind == OpenShapeKind.Cylinder && (long)Max.X - Min.X != (long)Max.Z - Min.Z) throw new ArgumentException($"the cylinder {name} is not round");
        return this;
    }

    public override string ToString() => Kind == OpenShapeKind.Box ? $"box {Min} to {Max}" : $"cylinder at {Base} of radius {Radius} mm up to {Max.Y} mm";
}

/// <summary>
/// A trigger (plan 32, "Volumes"; OW5): the open world's <c>StepTrigger</c>. Its script runs once as the capsule's
/// centre crosses into its shape, while the story is where it waits (its variable at its value, or always when it
/// names none). <see cref="Place"/> is the place it belongs to, whose script file is looked in first.
/// </summary>
public sealed record OpenTrigger(string Name, string Place, OpenShape Shape, string Script, string? Variable = null, int Value = 0)
{
    /// <summary>Whether the story is where the trigger waits for it.</summary>
    public bool Waits(Func<string, int> variable) => Variable == null || variable(Variable) == Value;
}

/// <summary>
/// A door from an open map into a grid room (plan 32 · decision 6; OW5): whoever's centre crosses into its shape goes
/// through the fade into <see cref="Room"/> at <see cref="RoomX"/>, <see cref="RoomY"/>, facing
/// <see cref="RoomFacing"/>; the room's own warp back out names the open map, and lands on the door's
/// <see cref="Out"/> marker. A door may wait for the story (<see cref="OpenedBy"/>, as an overlay's door does).
/// </summary>
public sealed record OpenDoor(string Name, string Place, OpenShape Shape, string Room, int RoomX, int RoomY, Direction RoomFacing, string Out, string? OpenedBy = null)
{
    /// <summary>Whether the story has opened it.</summary>
    public bool Open(Func<string, bool> flag) => OpenedBy == null || flag(OpenedBy);
}

/// <summary>A row of an encounter zone's table (plan 32 · OW5; OW7 spawns from it): a species, its levels and how often it comes.</summary>
public sealed record OpenSlot(string Species, int MinLevel, int MaxLevel, int Weight);

/// <summary>
/// An encounter zone (plan 32, "Volumes"; OW5 lays it, OW7 spawns its Pokémon): a polygon on the ground between two
/// heights, a table, how many of its Pokémon may stand in it at once (<see cref="Cap"/>) and how thickly they stand
/// (<see cref="Density"/>, Pokémon to a thousand square metres). GPU-free.
/// </summary>
public sealed record OpenZone(string Name, string Place, IReadOnlyList<(int X, int Z)> Polygon, int MinY, int MaxY, IReadOnlyList<OpenSlot> Slots, int Density, int Cap)
{
    /// <summary>Whether a place is inside: within the polygon (by the even-odd rule) and between its heights.</summary>
    public bool Contains(OpenPos p) => p.Y >= MinY && p.Y < MaxY && OpenPolygons.Holds(Polygon, p.X, p.Z);
}

/// <summary>
/// An area of an open map (plan 32, "What an open map is"): what a <c>MapArea</c> is to the grid, bounded by a polygon
/// with a height range instead of chunks, so a cave under a hill is an area of its own. Its key names its place's
/// file (<c>places/&lt;key&gt;.json</c>) and its scripts' (<c>scripts/&lt;key&gt;.txt</c>). Where two hold a place, the
/// later in the region's list is the one (a cave over the meadow round it).
/// </summary>
public sealed record OpenArea(string Key, string Name, IReadOnlyList<(int X, int Z)> Polygon, int MinY, int MaxY, string? Music = null, string? Weather = null)
{
    public bool Contains(OpenPos p) => p.Y >= MinY && p.Y < MaxY && OpenPolygons.Holds(Polygon, p.X, p.Z);
}

/// <summary>Polygons on the ground in millimetres, held to whole numbers. GPU-free.</summary>
public static class OpenPolygons
{
    /// <summary>
    /// Whether a point is inside a polygon by the even-odd rule: a ray east from it crosses the edges an odd number of
    /// times. Worked in 64-bit whole numbers (a cross product of millimetres), so it is the same anywhere.
    /// </summary>
    public static bool Holds(IReadOnlyList<(int X, int Z)> polygon, int x, int z)
    {
        bool inside = false;
        for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
        {
            var (xi, zi) = polygon[i];
            var (xj, zj) = polygon[j];
            if ((zi > z) == (zj > z)) continue;
            // The edge crosses the point's row: is the crossing east of the point? (x - xi)(zj - zi) < (xj - xi)(z - zi), the sign by the edge's way
            long left = ((long)x - xi) * ((long)zj - zi), right = ((long)xj - xi) * ((long)z - zi);
            if (zj > zi ? left < right : left > right) inside = !inside;
        }
        return inside;
    }
}

/// <summary>
/// What the capsule's centre crossed into (plan 32 · OW5): a trigger or a door, never both. The game runs a trigger's
/// script, or takes the player through a door.
/// </summary>
public sealed record OpenCrossing(OpenTrigger? Trigger, OpenDoor? Door);

/// <summary>
/// Which volumes the capsule's centre is in, tick by tick (plan 32 · OW5): what makes a trigger run <b>once as the
/// centre crosses into it</b>, and not again until it has left and come back, as the grid's step onto a trigger's
/// tile does. A place put down, a door come out of or a save loaded <see cref="Settle"/>s it, so what the player
/// stands in already is not crossed into. A trigger the story isn't waiting at is crossed all the same and fires no
/// more until left (the grid's trigger fires only on the step onto it). GPU-free.
/// </summary>
public sealed class OpenCrossings
{
    /// <summary>How far over the feet the capsule's centre is, in millimetres: half its height.</summary>
    public const int CentreHeight = 800;

    private readonly HashSet<string> inside = new(StringComparer.Ordinal);

    /// <summary>The capsule's centre over its feet.</summary>
    public static OpenPos CentreOf(OpenPos feet) => feet.Plus(0, CentreHeight, 0);

    /// <summary>The volumes the centre is in now count as crossed already: nothing fires for them.</summary>
    public void Settle(OpenMap map, OpenPos feet)
    {
        inside.Clear();
        var centre = CentreOf(feet);
        foreach (var t in map.Triggers) if (t.Shape.Contains(centre)) inside.Add("t:" + t.Name);
        foreach (var d in map.Doors) if (d.Shape.Contains(centre)) inside.Add("d:" + d.Name);
    }

    /// <summary>
    /// A tick's crossings: every trigger and door the centre is in now and wasn't at the last tick, the triggers first,
    /// each in the map's order (a trigger in a doorway runs before the door is gone through, as the grid's do).
    /// </summary>
    public List<OpenCrossing> Cross(OpenMap map, OpenPos feet)
    {
        var centre = CentreOf(feet);
        var crossed = new List<OpenCrossing>();
        var now = new HashSet<string>(StringComparer.Ordinal);
        foreach (var t in map.Triggers)
        {
            if (!t.Shape.Contains(centre)) continue;
            now.Add("t:" + t.Name);
            if (!inside.Contains("t:" + t.Name)) crossed.Add(new OpenCrossing(t, null));
        }
        foreach (var d in map.Doors)
        {
            if (!d.Shape.Contains(centre)) continue;
            now.Add("d:" + d.Name);
            if (!inside.Contains("d:" + d.Name)) crossed.Add(new OpenCrossing(null, d));
        }
        inside.Clear();
        inside.UnionWith(now);
        return crossed;
    }

    /// <summary>What the centre was in at the last tick, by kind and name (<c>t:cavern</c>, <c>d:house</c>): for tests and the harness.</summary>
    public IReadOnlyCollection<string> Inside => inside;
}
