global using Cover = PokemonPlatinumEngine.OpenWorld.OpenCover;

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

    /// <summary>Whether a column lies on the grid (from its first control point to its last).</summary>
    public bool Holds(double x, double z) =>
        x >= OriginX && z >= OriginZ && x <= OriginX + (Width - 1) * Spacing && z <= OriginZ + (Depth - 1) * Spacing;

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

/// <summary>A brush changes the land's height over the control points (plan 32: a plateau, a ridge, a crater, a river's bed).</summary>
public interface IBrush
{
    /// <summary>The height at a column once the brush is laid on <paramref name="h"/>, the height under it.</summary>
    double Apply(double x, double z, double h);
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

    public double Apply(double x, double z, double h) => h + Height(x, z);
}

/// <summary>What is cut out of the land, or added to it: a signed distance, below nought inside the shape.</summary>
public interface ICarve
{
    double Distance(D3 p);
    D3 Min { get; }
    D3 Max { get; }
}

/// <summary>A tunnel: a chain of capsules along a line, its round vault cut off by a flat floor (none below <see cref="NoFloor"/>).</summary>
public sealed record Tunnel(D3[] Line, double Radius, double Floor) : ICarve
{
    /// <summary>A floor nothing reaches: a chain with none (a rock bridge, an arch).</summary>
    public const double NoFloor = -1e6;

    public double Distance(D3 p)
    {
        double best = double.MaxValue;
        for (int s = 0; s + 1 < Line.Length; s++)
        {
            var a = Line[s]; var ab = Line[s + 1] - a;
            double t = Math.Clamp(D3.Dot(p - a, ab) / D3.Dot(ab, ab), 0, 1);
            best = Math.Min(best, (p - (a + ab * t)).Length);
        }
        if (Line.Length == 1) best = (p - Line[0]).Length;
        return Math.Max(best - Radius, Floor - p.Y);
    }

    public D3 Min => new(Line.Min(p => p.X) - Radius, Math.Max(Floor, Line.Min(p => p.Y) - Radius), Line.Min(p => p.Z) - Radius);
    public D3 Max => new(Line.Max(p => p.X) + Radius, Line.Max(p => p.Y) + Radius, Line.Max(p => p.Z) + Radius);
}

/// <summary>A cavern: an ellipsoid of air with a flat floor (none below <see cref="Tunnel.NoFloor"/>).</summary>
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

    public D3 Min => new(Centre.X - Radii.X, Math.Max(Floor, Centre.Y - Radii.Y), Centre.Z - Radii.Z);
    public D3 Max => Centre + Radii;
}

/// <summary>A road: a line over the land with a half width, painted as path and never graded (the prototype's; a region's are <see cref="GradedRoad"/>s).</summary>
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

/// <summary>
/// An open world's land, as the baker reads it (plan 32): the control points, the brushes over them, the roads graded
/// into them, the carves out of them and the additions on them, the water over them and the paint on them, in
/// metres, with chunks of 32 m by 32 m columns. The ground is one signed-distance field: above nought is air, below
/// is rock.
/// </summary>
public sealed class TerrainWorld
{
    public required string Name { get; init; }
    public double ChunkSize { get; init; } = 32;
    public required int ChunksX { get; init; }
    public required int ChunksZ { get; init; }
    public required double MinX { get; init; }
    public required double MinZ { get; init; }

    /// <summary>The prototype's one grid of control points; a region's are <see cref="Grids"/>.</summary>
    public ControlGrid? Ground { get; init; }

    /// <summary>A region's grids: a column takes its height from the first that holds it, or <see cref="BaseHeight"/>.</summary>
    public List<ControlGrid> Grids { get; } = new();

    /// <summary>The height where no grid lies.</summary>
    public double BaseHeight { get; init; }

    public List<IBrush> Brushes { get; } = new();
    public List<ICarve> Carves { get; } = new();

    /// <summary>What is joined to the land (a sea stack, a rock bridge): signed distances, below nought in rock.</summary>
    public List<ICarve> Additions { get; } = new();

    public List<Road> Roads { get; } = new();
    public List<GradedRoad> GradedRoads { get; } = new();
    public List<IWater> Water { get; } = new();
    public List<Paint> Paint { get; } = new();

    /// <summary>Above this height open ground is highland (the prototype's far hills; a region's file may say).</summary>
    public double HighlandAbove { get; init; } = 20;

    /// <summary>Above this height open ground is snow.</summary>
    public double SnowAbove { get; init; } = double.PositiveInfinity;

    /// <summary>How far over the water's surface its shore is sand.</summary>
    public double Shore { get; init; } = 0.6;

    /// <summary>The steepest the capsule walks and the navigation takes as floor: 40°, as the plan says.</summary>
    public const double WalkableY = 0.766;

    /// <summary>The land under the roads: the control points and the brushes, in their order.</summary>
    public double BaseAt(double x, double z)
    {
        double h;
        if (Ground != null) h = Ground.Sample(x, z);
        else
        {
            h = BaseHeight;
            foreach (var grid in Grids)
                if (grid.Holds(x, z)) { h = grid.Sample(x, z); break; }
        }
        foreach (var b in Brushes) h = b.Apply(x, z, h);
        return h;
    }

    /// <summary>The land's own surface, the roads graded into it (what is under a carve's air or an addition's rock).</summary>
    public double Height(double x, double z)
    {
        double h = BaseAt(x, z);
        foreach (var road in GradedRoads) h = road.Grade(x, z, h);
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
        foreach (var a in Additions) f = Math.Min(f, a.Distance(p));
        return f;
    }

    public (double X, double Z) ChunkOrigin(int cx, int cz) => (MinX + cx * ChunkSize, MinZ + cz * ChunkSize);

    public (int X, int Z) ChunkOf(double x, double z) =>
        ((int)Math.Floor((x - MinX) / ChunkSize), (int)Math.Floor((z - MinZ) / ChunkSize));

    /// <summary>The water's surface over a column (the highest body's there), or null.</summary>
    public double? WaterAt(double x, double z)
    {
        double? best = null;
        foreach (var water in Water)
            if (water.LevelAt(x, z) is { } level && (best == null || level > best)) best = level;
        return best;
    }

    /// <summary>The cover at a point of the surface with its normal: by paint, then by rule.</summary>
    public Cover CoverAt(D3 p, D3 n)
    {
        bool walkable = n.Y >= WalkableY;
        // Under the land's own surface by more than a step: inside a carve (a tunnel, a cavern, a cutting)
        if (p.Y < Height(p.X, p.Z) - 0.8) return walkable ? Cover.CaveFloor : (n.Y < -0.2 ? Cover.RockDark : Cover.Rock);
        foreach (var paint in Paint)
            if (paint.Holds(p)) return paint.Cover;
        if (!walkable) return n.Y < 0.45 ? Cover.RockDark : Cover.Rock;
        foreach (var road in Roads)
            if (road.DistanceXZ(p.X, p.Z) < road.HalfWidth) return Cover.Path;
        foreach (var road in GradedRoads)
            if (road.Covers(p.X, p.Z)) return road.Surface;
        if (Water.Count > 0 && WaterAt(p.X, p.Z) is { } level && p.Y < level + Shore) return p.Y < level - 1 ? Cover.Mud : Cover.Sand;
        if (p.Y > SnowAbove) return Cover.Snow;
        if (p.Y > HighlandAbove) return Cover.Highland;
        if (n.Y < 0.9) return Cover.GrassDeep;
        // Patches a few metres wide, one step lighter (the field's lawn recipe, here by vertex): blobs of a smooth noise
        return Noise.Smooth(p.X / 7, p.Z / 7, 31) > 0.66 ? Cover.GrassLight : Cover.Grass;
    }
}
