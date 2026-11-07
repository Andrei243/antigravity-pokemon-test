namespace OpenWorldBake;

/// <summary>
/// The land's height as control points a few metres apart, read between them by Catmull-Rom in both directions
/// (plan 32, "The world described, and baked": the height grid of control points, interpolated smoothly).
/// </summary>
public sealed class ControlGrid
{
    public double OriginX { get; }
    public double OriginZ { get; }
    public double Spacing { get; }
    public int Width { get; }
    public int Depth { get; }
    readonly double[] heights;

    public ControlGrid(double originX, double originZ, double spacing, int width, int depth, Func<int, int, double, double, double> heightAt)
    {
        OriginX = originX; OriginZ = originZ; Spacing = spacing; Width = width; Depth = depth;
        heights = new double[width * depth];
        for (int j = 0; j < depth; j++)
            for (int i = 0; i < width; i++)
                heights[j * width + i] = heightAt(i, j, originX + i * spacing, originZ + j * spacing);
    }

    double At(int i, int j) => heights[Math.Clamp(j, 0, Depth - 1) * Width + Math.Clamp(i, 0, Width - 1)];

    static double CatmullRom(double p0, double p1, double p2, double p3, double t) =>
        0.5 * (2 * p1 + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t * t + (-p0 + 3 * p1 - 3 * p2 + p3) * t * t * t);

    public double Sample(double x, double z)
    {
        double fx = (x - OriginX) / Spacing, fz = (z - OriginZ) / Spacing;
        int i = (int)Math.Floor(fx), j = (int)Math.Floor(fz);
        double tx = fx - i, tz = fz - j;
        Span<double> rows = stackalloc double[4];
        for (int r = 0; r < 4; r++)
            rows[r] = CatmullRom(At(i - 1, j - 1 + r), At(i, j - 1 + r), At(i + 1, j - 1 + r), At(i + 2, j - 1 + r), tx);
        return CatmullRom(rows[0], rows[1], rows[2], rows[3], tz);
    }
}

/// <summary>A brush raises (or lowers) the land over the control points.</summary>
public interface IBrush
{
    double Height(double x, double z);
}

/// <summary>A round hill: a raised cosine of its rise over its radius, smooth at its foot and its top.</summary>
public sealed record Dome(double X, double Z, double Radius, double Rise) : IBrush
{
    public double Height(double x, double z)
    {
        double dx = x - X, dz = z - Z;
        double r = Math.Sqrt(dx * dx + dz * dz);
        return r >= Radius ? 0 : Rise * 0.5 * (1 + Math.Cos(Math.PI * r / Radius));
    }
}

/// <summary>What is cut out of the land: a signed distance, below nought inside (where the cut makes air).</summary>
public interface ICarve
{
    double Distance(D3 p);
    D3 Min { get; }
    D3 Max { get; }
}

/// <summary>A tunnel: a chain of capsules along a line, its round vault cut off by a flat floor.</summary>
public sealed record Tunnel(D3[] Line, double Radius, double Floor) : ICarve
{
    public double Distance(D3 p)
    {
        double best = double.MaxValue;
        for (int s = 0; s + 1 < Line.Length; s++)
        {
            var a = Line[s]; var ab = Line[s + 1] - a;
            double t = Math.Clamp(D3.Dot(p - a, ab) / D3.Dot(ab, ab), 0, 1);
            best = Math.Min(best, (p - (a + ab * t)).Length);
        }
        return Math.Max(best - Radius, Floor - p.Y);
    }

    public D3 Min => new(Line.Min(p => p.X) - Radius, Floor, Line.Min(p => p.Z) - Radius);
    public D3 Max => new(Line.Max(p => p.X) + Radius, Line.Max(p => p.Y) + Radius, Line.Max(p => p.Z) + Radius);
}

/// <summary>A cavern: an ellipsoid of air with a flat floor.</summary>
public sealed record Cavern(D3 Centre, D3 Radii, double Floor) : ICarve
{
    public double Distance(D3 p)
    {
        var d = p - Centre;
        var q = new D3(d.X / Radii.X, d.Y / Radii.Y, d.Z / Radii.Z);
        var q2 = new D3(d.X / (Radii.X * Radii.X), d.Y / (Radii.Y * Radii.Y), d.Z / (Radii.Z * Radii.Z));
        double k0 = q.Length, k1 = q2.Length;
        double ell = k1 < 1e-12 ? -Math.Min(Radii.X, Math.Min(Radii.Y, Radii.Z)) : k0 * (k0 - 1) / k1;
        return Math.Max(ell, Floor - p.Y);
    }

    public D3 Min => new(Centre.X - Radii.X, Floor, Centre.Z - Radii.Z);
    public D3 Max => Centre + Radii;
}

/// <summary>A road: a line over the land with a half width, painted as path (not yet graded: OW2's).</summary>
public sealed record Road(D3[] Line, double HalfWidth)
{
    public double DistanceXZ(double x, double z)
    {
        double best = double.MaxValue;
        for (int s = 0; s + 1 < Line.Length; s++)
        {
            double ax = Line[s].X, az = Line[s].Z, bx = Line[s + 1].X, bz = Line[s + 1].Z;
            double abx = bx - ax, abz = bz - az;
            double t = Math.Clamp(((x - ax) * abx + (z - az) * abz) / (abx * abx + abz * abz), 0, 1);
            double dx = x - (ax + abx * t), dz = z - (az + abz * t);
            best = Math.Min(best, Math.Sqrt(dx * dx + dz * dz));
        }
        return best;
    }
}

/// <summary>What covers the ground at a vertex: decided by rule (slope, height, under rock, a road), as `TerrainCover` names the grid's.</summary>
public enum Cover : byte { Grass, GrassLight, GrassDeep, Path, Rock, RockDark, CaveFloor, Highland }

/// <summary>
/// An open world's land, as the baker reads it (plan 32): the control points, the brushes over them, the carves
/// out of them, and the roads painted on them, in metres, with chunks of 32 m by 32 m columns. The ground is one
/// signed-distance field: above nought is air, below is rock.
/// </summary>
public sealed class TerrainWorld
{
    public required string Name { get; init; }
    public double ChunkSize { get; init; } = 32;
    public required int ChunksX { get; init; }
    public required int ChunksZ { get; init; }
    public required double MinX { get; init; }
    public required double MinZ { get; init; }
    public required ControlGrid Ground { get; init; }
    public List<IBrush> Brushes { get; } = new();
    public List<ICarve> Carves { get; } = new();
    public List<Road> Roads { get; } = new();

    /// <summary>The steepest the capsule walks and the navigation takes as floor: 40°, as the plan says.</summary>
    public const double WalkableY = 0.766;

    public double Height(double x, double z)
    {
        double h = Ground.Sample(x, z);
        foreach (var b in Brushes) h += b.Height(x, z);
        return h;
    }

    /// <summary>The height at a column and what turns the height's difference into a distance (one over the slope's cosine).</summary>
    public (double Height, double Norm) Column(double x, double z)
    {
        const double e = 0.25;
        double h = Height(x, z);
        double gx = (Height(x + e, z) - Height(x - e, z)) / (2 * e);
        double gz = (Height(x, z + e) - Height(x, z - e)) / (2 * e);
        return (h, Math.Sqrt(1 + gx * gx + gz * gz));
    }

    /// <summary>The land's signed distance: above nought is air.</summary>
    public double Field(D3 p)
    {
        var (h, norm) = Column(p.X, p.Z);
        double f = (p.Y - h) / norm;
        foreach (var c in Carves) f = Math.Max(f, -c.Distance(p));
        return f;
    }

    public (double X, double Z) ChunkOrigin(int cx, int cz) => (MinX + cx * ChunkSize, MinZ + cz * ChunkSize);

    public (int X, int Z) ChunkOf(double x, double z) =>
        ((int)Math.Floor((x - MinX) / ChunkSize), (int)Math.Floor((z - MinZ) / ChunkSize));

    /// <summary>The cover at a point of the surface with its normal, by rule.</summary>
    public Cover CoverAt(D3 p, D3 n)
    {
        bool walkable = n.Y >= WalkableY;
        // Under the land's own surface by more than a step: inside a carve (a tunnel, a cavern, a cutting)
        if (p.Y < Height(p.X, p.Z) - 0.8) return walkable ? Cover.CaveFloor : (n.Y < -0.2 ? Cover.RockDark : Cover.Rock);
        if (!walkable) return n.Y < 0.45 ? Cover.RockDark : Cover.Rock;
        foreach (var road in Roads)
            if (road.DistanceXZ(p.X, p.Z) < road.HalfWidth) return Cover.Path;
        if (p.Y > 20) return Cover.Highland;
        if (n.Y < 0.9) return Cover.GrassDeep;
        // Patches a few metres wide, one step lighter (the field's lawn recipe, here by vertex): blobs of a smooth noise
        return Noise.Smooth(p.X / 7, p.Z / 7, 31) > 0.66 ? Cover.GrassLight : Cover.Grass;
    }
}
