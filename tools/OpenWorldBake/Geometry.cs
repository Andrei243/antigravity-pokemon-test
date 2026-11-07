namespace OpenWorldBake;

/// <summary>A point or a direction in metres, in double: the baker's and the prototype's own arithmetic.</summary>
public readonly record struct D3(double X, double Y, double Z)
{
    public static D3 operator +(D3 a, D3 b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
    public static D3 operator -(D3 a, D3 b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
    public static D3 operator -(D3 a) => new(-a.X, -a.Y, -a.Z);
    public static D3 operator *(D3 a, double s) => new(a.X * s, a.Y * s, a.Z * s);
    public static double Dot(D3 a, D3 b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;
    public static D3 Cross(D3 a, D3 b) => new(a.Y * b.Z - a.Z * b.Y, a.Z * b.X - a.X * b.Z, a.X * b.Y - a.Y * b.X);
    public double Length => Math.Sqrt(X * X + Y * Y + Z * Z);
    public D3 Normalized() { double l = Length; return l > 0 ? this * (1 / l) : this; }
    public override string ToString() => $"({X:0.000}, {Y:0.000}, {Z:0.000})";
}

/// <summary>Whole millimetres (plan 32 · decision 2): what every vertex and every position is rounded to before it is kept.</summary>
public static class Mm
{
    public static int Of(double metres) => (int)Math.Round(metres * 1000.0, MidpointRounding.AwayFromZero);
    public static double Metres(int mm) => mm / 1000.0;
}

/// <summary>The bake's only chance: a hash of whole numbers, the same on any machine and in any order.</summary>
public static class Noise
{
    public static double Hash01(int x, int y, int salt)
    {
        unchecked
        {
            uint h = (uint)x * 0x8DA6B343u ^ (uint)y * 0xD8163841u ^ (uint)salt * 0xCB1AB31Fu;
            h ^= h >> 13;
            h *= 0x5BD1E995u;
            h ^= h >> 15;
            h *= 0x27D4EB2Du;
            h ^= h >> 16;
            return (h & 0xFFFFFF) / 16777216.0;
        }
    }

    /// <summary>A value noise in [0, 1): the hash at whole numbers, eased between them.</summary>
    public static double Smooth(double x, double z, int salt)
    {
        int i = (int)Math.Floor(x), j = (int)Math.Floor(z);
        double tx = x - i, tz = z - j;
        tx = tx * tx * (3 - 2 * tx); tz = tz * tz * (3 - 2 * tz);
        double a = Hash01(i, j, salt), b = Hash01(i + 1, j, salt), c = Hash01(i, j + 1, salt), d = Hash01(i + 1, j + 1, salt);
        return (a * (1 - tx) + b * tx) * (1 - tz) + (c * (1 - tx) + d * tx) * tz;
    }

    public static double SmoothStep(double a, double b, double x)
    {
        double t = Math.Clamp((x - a) / (b - a), 0, 1);
        return t * t * (3 - 2 * t);
    }
}

/// <summary>Triangle queries in double: the closest point (Ericson's) and a ray's hit (Möller and Trumbore's).</summary>
public static class Tri
{
    public static D3 ClosestPoint(D3 p, D3 a, D3 b, D3 c)
    {
        var ab = b - a; var ac = c - a; var ap = p - a;
        double d1 = D3.Dot(ab, ap), d2 = D3.Dot(ac, ap);
        if (d1 <= 0 && d2 <= 0) return a;
        var bp = p - b;
        double d3 = D3.Dot(ab, bp), d4 = D3.Dot(ac, bp);
        if (d3 >= 0 && d4 <= d3) return b;
        double vc = d1 * d4 - d3 * d2;
        if (vc <= 0 && d1 >= 0 && d3 <= 0) return a + ab * (d1 / (d1 - d3));
        var cp = p - c;
        double d5 = D3.Dot(ab, cp), d6 = D3.Dot(ac, cp);
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
    public static double? Ray(D3 origin, D3 dir, D3 a, D3 b, D3 c)
    {
        var e1 = b - a; var e2 = c - a;
        var p = D3.Cross(dir, e2);
        double det = D3.Dot(e1, p);
        if (Math.Abs(det) < 1e-12) return null;
        double inv = 1 / det;
        var t0 = origin - a;
        double u = D3.Dot(t0, p) * inv;
        if (u < 0 || u > 1) return null;
        var q = D3.Cross(t0, e1);
        double v = D3.Dot(dir, q) * inv;
        if (v < 0 || u + v > 1) return null;
        double t = D3.Dot(e2, q) * inv;
        return t >= 0 ? t : null;
    }
}
