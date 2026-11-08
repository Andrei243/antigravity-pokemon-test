using System;
using System.Collections.Generic;

namespace PokemonPlatinumEngine.OpenWorld;

/// <summary>How someone of an open map moves by themselves (plan 32 · OW6, the grid's <c>PersonMovement</c>'s kinds at metre scale).</summary>
public enum OpenPersonMoves
{
    /// <summary>Stands where they were put, facing their way.</summary>
    Still,

    /// <summary>Stands where they were put and looks about, a quarter turn at a time.</summary>
    LookAbout,

    /// <summary>Walks about within their bounds (a polygon, or a circle of a radius round where they were put) on the navigation mesh.</summary>
    Wander
}

/// <summary>
/// A trainer's eyes in an open map (plan 32, "Volumes": "a sight cone is a trainer's: a length, an angle, and a ray
/// against the collision mesh that must reach the player's chest"): how far, in millimetres over the ground, and how
/// far to either side of the way they face, in 65,536ths of a turn. A trainer sees a place when it lies within the
/// cone on the ground and a line from their eyes (<see cref="EyeHeight"/> over their feet) to a chest there
/// (<see cref="ChestHeight"/> over its feet) meets nothing of the collision mesh on the way: a wall, a hill or a
/// building's box between hides the player. Only whole numbers and the four operations go into the cone (the way
/// toward the place is <see cref="OpenTriangles.YawOf"/>'s), and the ray is the ground's own, so two machines agree.
/// GPU-free.
/// </summary>
public readonly record struct OpenSight(int Length, int HalfAngle)
{
    /// <summary>Where the eyes and the chest are, in millimetres over the feet (plan 30 · FM6's grown body, 1.6 m tall).</summary>
    public const int EyeHeight = 1500, ChestHeight = 1200;

    /// <summary>The longest sight a trainer may have, in millimetres: only people this near the player are asked whether they see them (the grid's <c>SightReach</c>).</summary>
    public const int Longest = 30_000;

    /// <summary>A sight of so many metres and a cone so many degrees wide in all (a file's numbers).</summary>
    public static OpenSight Of(double metres, double degrees) =>
        new(OpenPos.Millimetres(metres), (int)Math.Round(degrees / 2 * OpenYaw.Turn / 360.0, MidpointRounding.AwayFromZero));

    /// <summary>Whether a place lies within the cone of someone standing at <paramref name="feet"/> facing <paramref name="yaw"/>, on the ground (no ray).</summary>
    public bool InCone(OpenPos feet, int yaw, OpenPos target)
    {
        long dx = (long)target.X - feet.X, dz = (long)target.Z - feet.Z;
        long d2 = dx * dx + dz * dz;
        if (d2 == 0 || d2 > (long)Length * Length) return false;
        int toward = OpenTriangles.YawOf(dx, dz);
        return Math.Abs(OpenYaw.Between(yaw, toward)) <= HalfAngle;
    }

    /// <summary>Whether someone standing at <paramref name="feet"/> facing <paramref name="yaw"/> sees a body standing at <paramref name="target"/>: in the cone, and nothing of <paramref name="ground"/> between their eyes and its chest.</summary>
    public bool Sees(OpenGround ground, OpenPos feet, int yaw, OpenPos target) =>
        InCone(feet, yaw, target) && Clear(ground, feet, target);

    /// <summary>Whether a line from eyes over <paramref name="feet"/> to a chest over <paramref name="target"/> meets nothing of the ground.</summary>
    public static bool Clear(OpenGround ground, OpenPos feet, OpenPos target)
    {
        var eye = OpenVec.Of(feet.Plus(0, EyeHeight, 0));
        var chest = OpenVec.Of(target.Plus(0, ChestHeight, 0));
        var way = chest - eye;
        double length = way.Length;
        if (length < 1e-6) return true;
        var dir = way * (1 / length);
        return ground.Raycast(eye, dir, length) is not { } hit || hit >= length - 0.01;
    }
}

/// <summary>
/// Someone of an open map (plan 32 · OW6): a person or a trainer put down in a place, with the capsule they walk with
/// (<see cref="Body"/>, the player's own rules, no grants), what moves them by themselves (<see cref="Moves"/> within
/// <see cref="Bounds"/>), and, for a trainer, their eyes (<see cref="Sight"/>). What they say and do when spoken to,
/// and the trainer of the data they battle as, are the engine's, which makes them a person of the grid's kind for the
/// scripts (the trainer's script path is the grid's). Whether they are on the map at all the story decides by their
/// flags (<see cref="HiddenBy"/>, <see cref="ShownBy"/>), as the grid's people. GPU-free; no clock but the ticks.
/// </summary>
public sealed class OpenPerson
{
    /// <summary>How far round where they were put someone who wanders with no bounds of their own walks, in millimetres.</summary>
    public const int WanderRadius = 3000;

    /// <summary>The name a script and the save know them by: unique in the map, letters, digits and _.</summary>
    public string Key { get; }

    /// <summary>The area of the place they were laid by: the script file their own scripts are in.</summary>
    public string Place { get; }

    /// <summary>Where they were put, and the way they faced: where a white-out or a map come to again puts them back.</summary>
    public OpenPos Home { get; }
    public int HomeYaw { get; }

    /// <summary>The look they are drawn with (a look of <c>characters.json</c>); null for the plain townsperson.</summary>
    public string? Look { get; init; }

    /// <summary>The name over what they say; null for their trainer's name, or nobody's.</summary>
    public string? ShownName { get; init; }

    /// <summary>Their own script, in their place's file or the common one; null for what they are (a trainer's, or their lines said).</summary>
    public string? Script { get; init; }

    /// <summary>What they say when spoken to and nothing else is said.</summary>
    public IReadOnlyList<string> Lines { get; init; } = Array.Empty<string>();

    /// <summary>The trainer of the data they battle as (<c>trainers.json</c>'s id), or null for a person who doesn't.</summary>
    public string? TrainerId { get; init; }

    /// <summary>A trainer's eyes; null for one who battles only when spoken to, and for everyone else.</summary>
    public OpenSight? Sight { get; init; }

    public OpenPersonMoves Moves { get; init; }

    /// <summary>Where they wander: a polygon on the ground, in millimetres (a circle of <see cref="WanderRadius"/> round <see cref="Home"/> unless told).</summary>
    public IReadOnlyList<(int X, int Z)> Bounds { get; init; }

    /// <summary>A flag that takes them off the map, and one without which they aren't on it.</summary>
    public string? HiddenBy { get; init; }
    public string? ShownBy { get; init; }

    /// <summary>Whether they are on the map now (the engine's, from their flags): someone absent is neither stepped, seen nor spoken to.</summary>
    public bool Present { get; set; } = true;

    /// <summary>The capsule they walk with.</summary>
    public OpenBody Body { get; private set; }

    public OpenPos Pos => Body.Pos;
    public int Yaw => Body.Yaw;

    /// <summary>What walks them now (a script's walk to a marker, a trainer's walk up, a leg of their wandering), or null.</summary>
    public OpenPathWalk? Walk { get; set; }

    /// <summary>Whether someone has them (a script, a walk up): they start no wandering of their own meanwhile.</summary>
    public bool Held { get; set; }

    /// <summary>Their place in the map's list: the order everyone is asked in.</summary>
    public int Index { get; internal set; } = -1;

    // The square of the map's index they are kept under
    internal (int X, int Z) Square;

    // Their wandering's own: ticks to wait before the next move, and the generator their choices are drawn from
    internal int Wait;
    internal Core.SplitMix? Draws;

    public OpenPerson(string key, string place, OpenPos home, int yaw)
    {
        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("someone of an open map has a name", nameof(key));
        Key = key;
        Place = place ?? "";
        Home = home;
        HomeYaw = OpenYaw.Normalize(yaw);
        Body = new OpenBody(home, yaw);
        Bounds = Circle(home, WanderRadius);
    }

    /// <summary>Puts them down at a place, facing a way: their capsule begun afresh there.</summary>
    public void PutAt(OpenPos pos, int yaw) => Body = new OpenBody(pos, yaw);

    /// <summary>Back where they were put, facing their way, with nothing walking them.</summary>
    public void SendHome()
    {
        PutAt(Home, HomeYaw);
        Walk = null;
        Wait = 0;
    }

    /// <summary>Whether a place is within their bounds on the ground.</summary>
    public bool InBounds(OpenPos p) => OpenPolygons.Holds(Bounds, p.X, p.Z);

    /// <summary>An octagon of a radius round a place: a circle's bounds, in whole millimetres.</summary>
    public static IReadOnlyList<(int X, int Z)> Circle(OpenPos at, int radius)
    {
        // The corners at the radius, every eighth of a turn (√½ for the slants: the square root only)
        int r = radius, s = (int)Math.Round(radius * Math.Sqrt(0.5), MidpointRounding.AwayFromZero);
        return new (int, int)[]
        {
            (at.X + r, at.Z), (at.X + s, at.Z + s), (at.X, at.Z + r), (at.X - s, at.Z + s),
            (at.X - r, at.Z), (at.X - s, at.Z - s), (at.X, at.Z - r), (at.X + s, at.Z - s)
        };
    }

    public override string ToString() => $"{Key} at {Pos}";
}
