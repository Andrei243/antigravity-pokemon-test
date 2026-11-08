using System;
using System.Numerics;

namespace PokemonPlatinumEngine.Graphics.OpenWorld;

/// <summary>
/// What a camera of the open world can see (plan 32 · OW8): its four sides and its near plane as planes through the
/// eye, asked whether a box can be in view (<see cref="Sees"/>), and how far the box is along the view. GPU-free.
/// </summary>
internal readonly struct OpenView
{
    public Vector3 Eye { get; }
    public Vector3 Forward { get; }
    private readonly Vector3 left, right, top, bottom;
    private readonly float near, far;

    public OpenView(Vector3 eye, Vector3 target, float fovYDegrees, float aspect, float near, float far)
    {
        Eye = eye;
        Forward = Vector3.Normalize(target - eye);
        var reference = MathF.Abs(Forward.Y) > 0.999f ? Vector3.UnitZ : Vector3.UnitY;
        var r = Vector3.Normalize(Vector3.Cross(Forward, reference));
        var u = Vector3.Cross(r, Forward);
        float tanV = MathF.Tan(fovYDegrees * MathF.PI / 360f), tanH = tanV * aspect;
        // Each side's plane through the eye, its normal facing into the view
        right = Forward * tanH - r;
        left = Forward * tanH + r;
        top = Forward * tanV - u;
        bottom = Forward * tanV + u;
        this.near = near;
        this.far = far;
    }

    /// <summary>Whether any of a box can be in view: on the inside of every side, past the near plane and within the far.</summary>
    public bool Sees(Vector3 min, Vector3 max)
    {
        if (!Inside(right, min, max) || !Inside(left, min, max) || !Inside(top, min, max) || !Inside(bottom, min, max)) return false;
        // The corner furthest along the view must be past the near plane, and the nearest within the far
        var furthest = Corner(Forward, min, max, furthest: true);
        if (Vector3.Dot(furthest - Eye, Forward) < near) return false;
        var nearest = Corner(Forward, min, max, furthest: false);
        return Vector3.Dot(nearest - Eye, Forward) <= far;
    }

    private bool Inside(Vector3 normal, Vector3 min, Vector3 max) => Vector3.Dot(Corner(normal, min, max, furthest: true) - Eye, normal) >= 0f;

    /// <summary>The box's corner furthest along a way (or the nearest).</summary>
    private static Vector3 Corner(Vector3 way, Vector3 min, Vector3 max, bool furthest) => furthest
        ? new Vector3(way.X >= 0 ? max.X : min.X, way.Y >= 0 ? max.Y : min.Y, way.Z >= 0 ? max.Z : min.Z)
        : new Vector3(way.X >= 0 ? min.X : max.X, way.Y >= 0 ? min.Y : max.Y, way.Z >= 0 ? min.Z : max.Z);
}
