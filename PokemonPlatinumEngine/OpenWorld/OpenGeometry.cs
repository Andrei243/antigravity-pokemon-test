using System;

namespace PokemonPlatinumEngine.OpenWorld;

/// <summary>
/// A point or a direction in metres, in double (plan 32 · OW3): the open world's own arithmetic between ticks, which
/// <see cref="OpenBody"/> rounds to micrometres once a tick. Only the four operations and the square root, so two
/// machines agree to the bit. GPU-free.
/// </summary>
public readonly record struct OpenVec(double X, double Y, double Z)
{
    public static OpenVec operator +(OpenVec a, OpenVec b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
    public static OpenVec operator -(OpenVec a, OpenVec b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
    public static OpenVec operator *(OpenVec a, double s) => new(a.X * s, a.Y * s, a.Z * s);
    public static double Dot(OpenVec a, OpenVec b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;
    public static OpenVec Cross(OpenVec a, OpenVec b) => new(a.Y * b.Z - a.Z * b.Y, a.Z * b.X - a.X * b.Z, a.X * b.Y - a.Y * b.X);
    public double Length => Math.Sqrt(X * X + Y * Y + Z * Z);
    public OpenVec Normalized() { double l = Length; return l > 0 ? this * (1 / l) : this; }

    /// <summary>A place in millimetres as metres.</summary>
    public static OpenVec Of(OpenPos p) => new(p.X / (double)OpenPos.PerMetre, p.Y / (double)OpenPos.PerMetre, p.Z / (double)OpenPos.PerMetre);

    public override string ToString() => FormattableString.Invariant($"({X:0.000}, {Y:0.000}, {Z:0.000})");
}

/// <summary>A triangle's queries in double (plan 32 · OW3): the closest point to a point (Ericson's) and a ray's hit (Möller and Trumbore's).</summary>
public static class OpenTriangles
{
    /// <summary>The point of the triangle a, b, c nearest p.</summary>
    public static OpenVec ClosestPoint(OpenVec p, OpenVec a, OpenVec b, OpenVec c)
    {
        var ab = b - a; var ac = c - a; var ap = p - a;
        double d1 = OpenVec.Dot(ab, ap), d2 = OpenVec.Dot(ac, ap);
        if (d1 <= 0 && d2 <= 0) return a;
        var bp = p - b;
        double d3 = OpenVec.Dot(ab, bp), d4 = OpenVec.Dot(ac, bp);
        if (d3 >= 0 && d4 <= d3) return b;
        double vc = d1 * d4 - d3 * d2;
        if (vc <= 0 && d1 >= 0 && d3 <= 0) return a + ab * (d1 / (d1 - d3));
        var cp = p - c;
        double d5 = OpenVec.Dot(ab, cp), d6 = OpenVec.Dot(ac, cp);
        if (d6 >= 0 && d5 <= d6) return c;
        double vb = d5 * d2 - d1 * d6;
        if (vb <= 0 && d2 >= 0 && d6 <= 0) return a + ac * (d2 / (d2 - d6));
        double va = d3 * d6 - d5 * d4;
        if (va <= 0 && d4 - d3 >= 0 && d5 - d6 >= 0) return b + (c - b) * ((d4 - d3) / (d4 - d3 + (d5 - d6)));
        double denom = 1 / (va + vb + vc);
        double v = vb * denom, w = vc * denom;
        return a + ab * v + ac * w;
    }

    /// <summary>Where a ray meets a triangle, as a distance along the ray (either face), or null.</summary>
    public static double? Ray(OpenVec origin, OpenVec dir, OpenVec a, OpenVec b, OpenVec c)
    {
        var e1 = b - a; var e2 = c - a;
        var p = OpenVec.Cross(dir, e2);
        double det = OpenVec.Dot(e1, p);
        if (Math.Abs(det) < 1e-12) return null;
        double inv = 1 / det;
        var t0 = origin - a;
        double u = OpenVec.Dot(t0, p) * inv;
        if (u < 0 || u > 1) return null;
        var q = OpenVec.Cross(t0, e1);
        double v = OpenVec.Dot(dir, q) * inv;
        if (v < 0 || u + v > 1) return null;
        double t = OpenVec.Dot(e2, q) * inv;
        return t >= 0 ? t : null;
    }

    /// <summary>The height of a triangle over a column x, z, if the column passes through it (its edges included).</summary>
    public static double? HeightIn(double x, double z, OpenVec a, OpenVec b, OpenVec c)
    {
        double d = (b.Z - c.Z) * (a.X - c.X) + (c.X - b.X) * (a.Z - c.Z);
        if (Math.Abs(d) < 1e-12) return null;
        double l1 = ((b.Z - c.Z) * (x - c.X) + (c.X - b.X) * (z - c.Z)) / d;
        double l2 = ((c.Z - a.Z) * (x - c.X) + (a.X - c.X) * (z - c.Z)) / d;
        double l3 = 1 - l1 - l2;
        const double eps = -1e-9;
        if (l1 < eps || l2 < eps || l3 < eps) return null;
        return l1 * a.Y + l2 * b.Y + l3 * c.Y;
    }

    /// <summary>
    /// The yaw of a way along the ground (east <paramref name="x"/>, south <paramref name="z"/>), in 65,536ths of a
    /// turn, worked with the four operations and the square root alone (a polynomial for the arctangent, good to
    /// about 2e-6 of a radian against a 65,536th's 9.6e-5), so it is the same on any machine, which a runtime's own
    /// arctangent need not be. South where there is no way.
    /// </summary>
    public static int YawOf(double x, double z)
    {
        if (x == 0 && z == 0) return OpenYaw.South;
        // The angle from south (+z) toward east (+x): atan2(x, z), by octants
        double ax = Math.Abs(x), az = Math.Abs(z);
        bool swap = ax > az;
        double t = swap ? az / ax : ax / az;   // 0 to 1
        // atan(t) on [0, 1]: t reduced to [0, tan(π/12)] by the identity atan t = π/6 + atan((t√3 − 1)/(t + √3))
        const double Sqrt3 = 1.7320508075688772, Tan15 = 0.2679491924311227;
        double add = 0;
        if (t > Tan15) { t = (t * Sqrt3 - 1) / (t + Sqrt3); add = Math.PI / 6; }
        double t2 = t * t;
        double atan = add + t * (1 - t2 * (1.0 / 3 - t2 * (1.0 / 5 - t2 * (1.0 / 7 - t2 * (1.0 / 9 - t2 / 11)))));
        double angle = swap ? Math.PI / 2 - atan : atan;
        if (z < 0) angle = Math.PI - angle;
        if (x < 0) angle = -angle;
        return OpenYaw.Normalize((int)Math.Round(angle * (OpenYaw.Turn / (Math.PI * 2)), MidpointRounding.AwayFromZero));
    }
}
