namespace OpenWorldBake;

/// <summary>A point on the ground (x and z, metres).</summary>
public readonly record struct P2(double X, double Z);

/// <summary>Lines and polygons on the ground, in double: what the brushes, roads, water and paint are measured by.</summary>
public static class Flat
{
    /// <summary>The distance from a point to a segment, and how far along it (0 to 1) the nearest point lies.</summary>
    public static (double Distance, double T) ToSegment(double x, double z, P2 a, P2 b)
    {
        double abx = b.X - a.X, abz = b.Z - a.Z;
        double len2 = abx * abx + abz * abz;
        double t = len2 < 1e-18 ? 0 : Math.Clamp(((x - a.X) * abx + (z - a.Z) * abz) / len2, 0, 1);
        double dx = x - (a.X + abx * t), dz = z - (a.Z + abz * t);
        return (Math.Sqrt(dx * dx + dz * dz), t);
    }

    /// <summary>Whether a point lies inside a polygon (even-odd; a point on an edge may fall either way).</summary>
    public static bool Inside(IReadOnlyList<P2> polygon, double x, double z)
    {
        bool inside = false;
        for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
        {
            var a = polygon[i]; var b = polygon[j];
            if ((a.Z > z) != (b.Z > z) && x < (b.X - a.X) * (z - a.Z) / (b.Z - a.Z) + a.X) inside = !inside;
        }
        return inside;
    }

    /// <summary>The distance from a point to a polygon's edge, signed: below nought inside.</summary>
    public static double ToPolygon(IReadOnlyList<P2> polygon, double x, double z)
    {
        double best = double.MaxValue;
        for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
            best = Math.Min(best, ToSegment(x, z, polygon[j], polygon[i]).Distance);
        return Inside(polygon, x, z) ? -best : best;
    }

    /// <summary>The box round some points: min x, min z, max x, max z.</summary>
    public static (double X0, double Z0, double X1, double Z1) Bounds(IEnumerable<P2> points) =>
        (points.Min(p => p.X), points.Min(p => p.Z), points.Max(p => p.X), points.Max(p => p.Z));
}

/// <summary>
/// A line on the ground with a value carried along it, made smooth: a Catmull-Rom spline through its points, read
/// as a polyline of steps of about a metre, with the value (a road's grade, a river's surface) interpolated by the
/// length along it between the points that give one. The steps are the same on any machine, since they are made
/// from the points alone.
/// </summary>
public sealed class Spline
{
    public P2[] Samples { get; }
    public double[] Values { get; }
    public double[] Along { get; }
    public (double X0, double Z0, double X1, double Z1) Box { get; }

    /// <param name="points">The line's points.</param>
    /// <param name="values">A value at each point, or null where it is to be read from <paramref name="fill"/> at the point.</param>
    public Spline(IReadOnlyList<P2> points, IReadOnlyList<double?> values, Func<P2, double> fill, double step = 1)
    {
        if (points.Count < 2) throw new ArgumentException("a line needs two points");
        var samples = new List<P2>();
        var at = new List<int>();          // for each point, its sample
        for (int s = 0; s + 1 < points.Count; s++)
        {
            P2 p0 = points[Math.Max(s - 1, 0)], p1 = points[s], p2 = points[s + 1], p3 = points[Math.Min(s + 2, points.Count - 1)];
            double length = Math.Sqrt((p2.X - p1.X) * (p2.X - p1.X) + (p2.Z - p1.Z) * (p2.Z - p1.Z));
            int steps = Math.Max(1, (int)Math.Ceiling(length / step));
            at.Add(samples.Count);
            for (int k = 0; k < steps; k++)
            {
                double t = k / (double)steps;
                samples.Add(new P2(CatmullRom(p0.X, p1.X, p2.X, p3.X, t), CatmullRom(p0.Z, p1.Z, p2.Z, p3.Z, t)));
            }
        }
        at.Add(samples.Count);
        samples.Add(points[^1]);
        Samples = samples.ToArray();
        Along = new double[Samples.Length];
        for (int i = 1; i < Samples.Length; i++)
        {
            double dx = Samples[i].X - Samples[i - 1].X, dz = Samples[i].Z - Samples[i - 1].Z;
            Along[i] = Along[i - 1] + Math.Sqrt(dx * dx + dz * dz);
        }
        // The value at each point (given, or read), then straight between them by the length along the line
        var known = new double[points.Count];
        for (int i = 0; i < points.Count; i++) known[i] = values[i] ?? fill(points[i]);
        Values = new double[Samples.Length];
        for (int s = 0; s + 1 < points.Count; s++)
        {
            int from = at[s], to = at[s + 1];
            double l0 = Along[from], l1 = Along[to];
            for (int i = from; i <= to; i++)
                Values[i] = l1 - l0 < 1e-12 ? known[s] : known[s] + (known[s + 1] - known[s]) * (Along[i] - l0) / (l1 - l0);
        }
        Box = Flat.Bounds(Samples);
    }

    static double CatmullRom(double p0, double p1, double p2, double p3, double t) =>
        0.5 * (2 * p1 + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t * t + (-p0 + 3 * p1 - 3 * p2 + p3) * t * t * t);

    /// <summary>The nearest point of the line to a column: how far it is and the value there; nothing beyond <paramref name="reach"/> of the line's box.</summary>
    public (double Distance, double Value)? Nearest(double x, double z, double reach)
    {
        if (x < Box.X0 - reach || x > Box.X1 + reach || z < Box.Z0 - reach || z > Box.Z1 + reach) return null;
        double best = double.MaxValue, value = 0;
        for (int i = 0; i + 1 < Samples.Length; i++)
        {
            var a = Samples[i]; var b = Samples[i + 1];
            // A segment's own box first: most are far
            if (x < Math.Min(a.X, b.X) - reach || x > Math.Max(a.X, b.X) + reach || z < Math.Min(a.Z, b.Z) - reach || z > Math.Max(a.Z, b.Z) + reach) continue;
            var (d, t) = Flat.ToSegment(x, z, a, b);
            if (d < best) { best = d; value = Values[i] + (Values[i + 1] - Values[i]) * t; }
        }
        return best == double.MaxValue ? null : (best, value);
    }
}

/// <summary>A ridge: a raised cosine across a line, its rise falling to nought at half its width either side.</summary>
public sealed class Ridge(IReadOnlyList<P2> points, double width, double rise) : IBrush
{
    readonly Spline line = new(points, points.Select(_ => (double?)rise).ToList(), _ => rise);

    public double Apply(double x, double z, double h)
    {
        if (line.Nearest(x, z, width / 2) is not { } near || near.Distance >= width / 2) return h;
        return h + near.Value * 0.5 * (1 + Math.Cos(Math.PI * near.Distance / (width / 2)));
    }
}

/// <summary>A river's bed (or any channel): a raised cosine down across a line, as deep as it says at its middle.</summary>
public sealed class Channel(IReadOnlyList<P2> points, double width, double depth) : IBrush
{
    readonly Spline line = new(points, points.Select(_ => (double?)depth).ToList(), _ => depth);

    public double Apply(double x, double z, double h)
    {
        if (line.Nearest(x, z, width / 2) is not { } near || near.Distance >= width / 2) return h;
        return h - near.Value * 0.5 * (1 + Math.Cos(Math.PI * near.Distance / (width / 2)));
    }
}

/// <summary>A plateau: inside its polygon the land is at its height, eased back into the land over its falloff outside.</summary>
public sealed class Plateau(IReadOnlyList<P2> polygon, double height, double falloff) : IBrush
{
    readonly (double X0, double Z0, double X1, double Z1) box = Flat.Bounds(polygon);

    public double Apply(double x, double z, double h)
    {
        if (x < box.X0 - falloff || x > box.X1 + falloff || z < box.Z0 - falloff || z > box.Z1 + falloff) return h;
        double d = Flat.ToPolygon(polygon, x, z);
        if (d <= 0) return height;
        if (d >= falloff) return h;
        return height + (h - height) * Noise.SmoothStep(0, falloff, d);
    }
}

/// <summary>A crater: a bowl as deep as it says at its middle, with a rim raised round it, both raised cosines.</summary>
public sealed record Crater(double X, double Z, double Radius, double Depth, double Rim) : IBrush
{
    public double Apply(double x, double z, double h)
    {
        double dx = x - X, dz = z - Z, r = Math.Sqrt(dx * dx + dz * dz);
        if (r >= Radius * 1.5) return h;
        double bowl = r < Radius ? -Depth * 0.5 * (1 + Math.Cos(Math.PI * r / Radius)) : 0;
        // The rim peaks at the bowl's edge and falls to nought half a radius either side of it
        double fromRim = Math.Abs(r - Radius);
        double rim = fromRim < Radius * 0.5 ? Rim * 0.5 * (1 + Math.Cos(Math.PI * fromRim / (Radius * 0.5))) : 0;
        return h + bowl + rim;
    }
}

/// <summary>A box of rock or air (a cliff's notch, a ledge), its corners rounded by <see cref="Round"/>.</summary>
public sealed record Block(D3 Low, D3 High, double Round) : ICarve
{
    public double Distance(D3 p)
    {
        var c = (Low + High) * 0.5;
        var half = (High - Low) * 0.5 - new D3(Round, Round, Round);
        var q = new D3(Math.Abs(p.X - c.X) - half.X, Math.Abs(p.Y - c.Y) - half.Y, Math.Abs(p.Z - c.Z) - half.Z);
        var outside = new D3(Math.Max(q.X, 0), Math.Max(q.Y, 0), Math.Max(q.Z, 0));
        return outside.Length + Math.Min(Math.Max(q.X, Math.Max(q.Y, q.Z)), 0) - Round;
    }

    public D3 Min => Low;
    public D3 Max => High;
}

/// <summary>An upright cylinder of rock or air (a sea stack, a shaft), from its bottom to its top.</summary>
public sealed record Pillar(double X, double Z, double Radius, double Bottom, double Top) : ICarve
{
    public double Distance(D3 p)
    {
        double dx = p.X - X, dz = p.Z - Z;
        double across = Math.Sqrt(dx * dx + dz * dz) - Radius;
        double up = Math.Max(Bottom - p.Y, p.Y - Top);
        return Math.Min(Math.Max(across, up), 0) + new D3(Math.Max(across, 0), Math.Max(up, 0), 0).Length;
    }

    public D3 Min => new(X - Radius, Bottom, Z - Radius);
    public D3 Max => new(X + Radius, Top, Z + Radius);
}

/// <summary>
/// A road graded along its spline (plan 32 · OW2): its line made smooth, its grade read at its points (given, or the
/// land's height there) and run straight between them by the length along it, the land laid level across its width
/// at that grade and eased back into the land over its shoulders; painted with its surface.
/// </summary>
public sealed class GradedRoad
{
    public string Name { get; }
    public Spline Line { get; }
    public double HalfWidth { get; }
    public double Shoulder { get; }
    public Cover Surface { get; }

    public GradedRoad(string name, IReadOnlyList<P2> points, IReadOnlyList<double?> heights, double width, double shoulder, Cover surface, Func<P2, double> land)
    {
        Name = name;
        Line = new Spline(points, heights, land);
        HalfWidth = width / 2;
        Shoulder = shoulder;
        Surface = surface;
    }

    /// <summary>The grade at the line's nearest point to a column and how far it is, or null beyond its shoulders.</summary>
    public (double Distance, double Grade)? Near(double x, double z) => Line.Nearest(x, z, HalfWidth + Shoulder);

    /// <summary>The height once the road is graded into <paramref name="h"/>.</summary>
    public double Grade(double x, double z, double h)
    {
        if (Near(x, z) is not { } near) return h;
        if (near.Distance <= HalfWidth) return near.Grade;
        if (near.Distance >= HalfWidth + Shoulder) return h;
        return near.Grade + (h - near.Grade) * Noise.SmoothStep(HalfWidth, HalfWidth + Shoulder, near.Distance);
    }

    /// <summary>Whether the road's surface covers a column.</summary>
    public bool Covers(double x, double z) => Line.Nearest(x, z, HalfWidth) is { } near && near.Distance < HalfWidth;
}

/// <summary>A body of water: its surface's height over a column, or null where it isn't.</summary>
public interface IWater
{
    string Name { get; }
    double? LevelAt(double x, double z);
}

/// <summary>The sea: its level everywhere (or inside its polygon).</summary>
public sealed record Sea(string Name, double Level, IReadOnlyList<P2>? Polygon) : IWater
{
    public double? LevelAt(double x, double z) => Polygon == null || Flat.Inside(Polygon, x, z) ? Level : null;
}

/// <summary>A lake: its level inside its polygon.</summary>
public sealed record Lake(string Name, IReadOnlyList<P2> Polygon, double Level) : IWater
{
    public double? LevelAt(double x, double z) => Flat.Inside(Polygon, x, z) ? Level : null;
}

/// <summary>A river: its surface along its line, as wide as it says, its level running between its points' levels.</summary>
public sealed class River(string name, IReadOnlyList<P2> points, IReadOnlyList<double> levels, double width) : IWater
{
    readonly Spline line = new(points, levels.Select(l => (double?)l).ToList(), _ => 0);
    public string Name { get; } = name;

    public double? LevelAt(double x, double z) =>
        line.Nearest(x, z, width / 2) is { } near && near.Distance <= width / 2 ? near.Value : null;
}

/// <summary>A cover painted on the land's surface inside a polygon or a circle, between two heights.</summary>
public sealed class Paint(Cover cover, IReadOnlyList<P2>? polygon, P2? centre, double radius, double minY, double maxY)
{
    public Cover Cover { get; } = cover;

    public bool Holds(D3 p)
    {
        if (p.Y < minY || p.Y > maxY) return false;
        if (polygon != null) return Flat.Inside(polygon, p.X, p.Z);
        var c = centre!.Value;
        double dx = p.X - c.X, dz = p.Z - c.Z;
        return dx * dx + dz * dz <= radius * radius;
    }
}
