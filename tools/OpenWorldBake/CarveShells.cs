namespace OpenWorldBake;

/// <summary>Where a carve's roof is thin: the carve's index in the land's list, the columns found, the thinnest and where it is.</summary>
public sealed record ThinShell(int Carve, int Columns, double Thinnest, double X, double Z, double CeilingY)
{
    /// <summary>The ground the thin roof covers, in square metres (each column a half-metre square).</summary>
    public double Area => Columns * CarveShells.Step * CarveShells.Step;
}

/// <summary>
/// How much rock lies over each carve (plan 32 · OW5, OW2's finding): surface nets at 0.5 m cells mesh a shell of rock
/// thinner than a cell with faces turned the wrong way (where the lab's tunnel passes 0.2 m under the hill's flank),
/// so a description holds <b>half a metre of rock over a carve</b>, or none (a cutting open to the sky, a portal).
/// <para>
/// Over each carve's ground, column by column a half metre apart, the carve's ceiling is found from its own distance,
/// and the rock over it by the land's field (every carve and addition in it) up to a metre. A column whose rock is
/// thicker than a few centimetres and thinner than <see cref="LeastRock"/> is a thin roof <b>where the land's surface
/// and the carve's roof run nearly side by side</b>: every column beside it holds within half a metre of it, so the
/// thin shell spans a cell or more. Where the surface comes down across the roof steeply (a portal's face, a mouth's
/// lip) the shell grows by half a metre a column or more and the mesher meets it cleanly, as the lab's portals show; that
/// is not told. GPU-free, single-threaded, the same anywhere.
/// </para>
/// </summary>
public static class CarveShells
{
    /// <summary>The rock a carve's roof must have, in metres.</summary>
    public const double LeastRock = 0.5;

    /// <summary>A shell thinner than this is the carve opening to the air, in metres.</summary>
    public const double Open = 0.05;

    /// <summary>The columns' spacing, in metres: a cell of level 0.</summary>
    public const double Step = 0.5;

    const double Reach = 1.0;

    /// <summary>Every carve of the land whose roof is thin somewhere, in the land's order.</summary>
    public static List<ThinShell> Thin(TerrainWorld land)
    {
        var found = new List<ThinShell>();
        for (int c = 0; c < land.Carves.Count; c++)
            if (Of(land, c) is { } thin) found.Add(thin);
        return found;
    }

    /// <summary>A carve's thin roof, or null where it has none.</summary>
    public static ThinShell? Of(TerrainWorld land, int index)
    {
        var carve = land.Carves[index];
        var (min, max) = (carve.Min, carve.Max);
        int nx = Math.Max(1, (int)Math.Ceiling((max.X - min.X) / Step)), nz = Math.Max(1, (int)Math.Ceiling((max.Z - min.Z) / Step));
        var rock = new double[nx, nz];
        var ceiling = new double[nx, nz];
        for (int i = 0; i < nx; i++)
            for (int j = 0; j < nz; j++)
            {
                double x = min.X + (i + 0.5) * Step, z = min.Z + (j + 0.5) * Step;
                (rock[i, j], ceiling[i, j]) = Column(land, carve, x, z, min.Y, max.Y);
            }

        int columns = 0;
        double thinnest = double.MaxValue, tx = 0, tz = 0, ty = 0;
        for (int i = 0; i < nx; i++)
            for (int j = 0; j < nz; j++)
            {
                double t = rock[i, j];
                if (double.IsNaN(t) || t <= Open || t >= LeastRock) continue;
                // The surface and the roof side by side: no neighbour a half metre or more thicker or thinner
                bool steep = false;
                foreach (var (di, dj) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
                {
                    int a = i + di, b = j + dj;
                    if (a < 0 || b < 0 || a >= nx || b >= nz || double.IsNaN(rock[a, b])) continue;
                    if (Math.Abs(rock[a, b] - t) >= Step) { steep = true; break; }
                }
                if (steep) continue;
                columns++;
                if (t < thinnest) (thinnest, tx, tz, ty) = (t, min.X + (i + 0.5) * Step, min.Z + (j + 0.5) * Step, ceiling[i, j]);
            }
        return columns == 0 ? null : new ThinShell(index, columns, thinnest, tx, tz, ty);
    }

    /// <summary>
    /// A column's rock over the carve's ceiling, up to <see cref="Reach"/> (more is told as the reach), and the
    /// ceiling's height; NaN where the carve has no air in the column.
    /// </summary>
    static (double Rock, double Ceiling) Column(TerrainWorld land, ICarve carve, double x, double z, double bottom, double top)
    {
        // The carve's ceiling: down from over its top until inside it, then halved to a centimetre
        double y = top + 0.1, inside = double.NaN;
        for (; y > bottom - 0.1; y -= 0.25)
            if (carve.Distance(new D3(x, y, z)) < 0) { inside = y; break; }
        if (double.IsNaN(inside)) return (double.NaN, double.NaN);
        double lo = inside, hi = inside + 0.25;
        for (int k = 0; k < 6; k++)
        {
            double mid = (lo + hi) / 2;
            if (carve.Distance(new D3(x, mid, z)) < 0) lo = mid; else hi = mid;
        }
        double roof = hi;

        // The rock over it: air at once is none, else up in tenths to the first air, halved to a centimetre
        if (land.Field(new D3(x, roof + 0.01, z)) >= 0) return (0, roof);
        double below = roof + 0.01, above = double.NaN;
        for (double up = roof + 0.1; up <= roof + Reach + 1e-9; up += 0.1)
        {
            if (land.Field(new D3(x, up, z)) >= 0) { above = up; break; }
            below = up;
        }
        if (double.IsNaN(above)) return (Reach, roof);
        for (int k = 0; k < 4; k++)
        {
            double mid = (below + above) / 2;
            if (land.Field(new D3(x, mid, z)) >= 0) above = mid; else below = mid;
        }
        return (above - roof, roof);
    }
}
