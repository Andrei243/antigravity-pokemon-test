namespace OpenWorldBake;

/// <summary>A tree of the prototype's scatter: where it stands, which kind and how big (metres).</summary>
public readonly record struct TreeSpot(double X, double Y, double Z, bool Pine, double Scale, int Seed);

/// <summary>
/// The gate's world (plan 32 · OW0): a meadow 768 m across ringed by far hills, and in its middle one hill 60 m
/// across and 12 m high, with a tunnel through it from west to east and a cavern under its top, on which the
/// tunnel opens. A road crosses the meadow to the tunnel's mouths. Everything here is a description written as
/// code, the shape OW2 writes as files; nothing is taken from any game.
/// </summary>
public static class Prototype
{
    public const string Name = "prototype";
    public const int Chunks = 24;
    public const double Extent = Chunks * 32 / 2.0;   // 384 m from the middle to an edge

    public const double HillRadius = 30, HillRise = 12;
    public const double TunnelRadius = 2.4, TunnelAxisY = 1.0, TunnelEnd = 40;

    /// <summary>The trigger: a box in the cavern under the hill's top, its height the cavern's (the top over it is outside).</summary>
    public static readonly TriggerBox Trigger = new("cavern", new D3(-6, -0.5, -5), new D3(6, 4, 5));

    /// <summary>The wild Pokémon's zone: the hill's top, from 6 m up (so the cavern under it is not in it).</summary>
    public static readonly Zone Zone = new("hilltop",
        Enumerable.Range(0, 8).Select(i => (14 * Math.Cos(i * Math.PI / 4), 14 * Math.Sin(i * Math.PI / 4))).ToArray(), 6, 20, "Wooloo");

    /// <summary>Where the walk begins, on the meadow south-west of the hill.</summary>
    public static readonly D3 Start = new(-56, 0, 14);

    /// <summary>
    /// Run across the meadow to the tunnel's west cutting, walk through the tunnel and the cavern, out at the east,
    /// round to the north-east flank, up to the top (over the cavern), then west along the tunnel's line, off the
    /// top of the west portal into the cutting (a drop with a landing), and out onto the meadow.
    /// </summary>
    public static readonly Waypoint[] Route =
    {
        new(-44, 0, true, "the west cutting's mouth"),
        new(-30, 0, false, "the west cutting"),
        new(-9, 0, false, "the cavern's west side"),
        // Into the cavern's north wall at a slant: the body slides along it and stops against it (plan 32, "Movement")
        new(-1, -12, false, "the cavern's north wall"),
        new(0, 0, false, "the cavern's middle"),
        new(30, 0, false, "the east cutting"),
        new(42, 0, false, "the east mouth"),
        new(44, -14, false, "the north-east meadow"),
        new(24, -18, false, "the north-east flank's foot"),
        new(8, -6, false, "the flank's shoulder"),
        new(0, 0, false, "the top, over the cavern"),
        new(-16, 0, false, "over the west portal"),
        new(-34, 0, false, "down in the west cutting"),
        new(-46, 6, false, "the meadow again"),
    };

    /// <summary>The person standing by the west cutting, turned toward the path; and the wild Pokémon on the top.</summary>
    public static readonly D3 Person = new(-41, 0, -4.5);
    public const double PersonYaw = Math.PI * 0.35;
    public static readonly D3 PokemonSpot = new(5.5, 0, -6);
    public const double PokemonYaw = -Math.PI * 0.7;

    public static TerrainWorld World()
    {
        // The control points: flat by the hill, a gentle roll over the meadow, and far hills round its edge
        var ground = new ControlGrid(-Extent - 32, -Extent - 32, 16, (int)(Extent * 2 / 16) + 5, (int)(Extent * 2 / 16) + 5, (i, j, x, z) =>
        {
            double r = Math.Sqrt(x * x + z * z);
            double roll = 1.4 * Noise.SmoothStep(72, 130, r) * (Noise.Hash01(i, j, 3) * 2 - 1);
            double far = 38 * Noise.SmoothStep(220, 345, r) * (0.55 + 0.45 * Noise.Hash01(i, j, 7));
            return roll + far;
        });
        var world = new TerrainWorld { Name = Name, ChunksX = Chunks, ChunksZ = Chunks, MinX = -Extent, MinZ = -Extent, Ground = ground };
        world.Brushes.Add(new Dome(0, 0, HillRadius, HillRise));
        world.Brushes.Add(new Dome(-9, -11, 9, 2.2));
        world.Carves.Add(new Tunnel(new[] { new D3(-TunnelEnd, TunnelAxisY, 0), new D3(TunnelEnd, TunnelAxisY, 0) }, TunnelRadius, 0));
        world.Carves.Add(new Cavern(new D3(0, 2.5, 0), new D3(13, 6.5, 11), 0));
        world.Roads.Add(new Road(new[] { new D3(-70, 0, 26), new D3(-50, 0, 6), new D3(-42, 0, 0), new D3(-34, 0, 0) }, 1.6));
        world.Roads.Add(new Road(new[] { new D3(34, 0, 0), new D3(46, 0, -4), new D3(66, 0, -20), new D3(90, 0, -30) }, 1.6));
        return world;
    }

    /// <summary>The three levels the prototype bakes: the finest round the hill, the second over the near meadow, the third everywhere.</summary>
    public static bool Wanted(int cx, int cz, int lod) => lod switch
    {
        0 => cx >= 10 && cx <= 13 && cz >= 10 && cz <= 13,
        1 => cx >= 6 && cx <= 17 && cz >= 6 && cz <= 17,
        _ => true
    };

    public static Bake BakeAll(TerrainWorld? world = null) => OpenWorldBake.Bake.Of(world ?? World(), Wanted);

    /// <summary>Only the hill's own chunks at the finest level: what the tests bake (a few hundred milliseconds).</summary>
    public static Bake BakeHill(TerrainWorld? world = null) =>
        OpenWorldBake.Bake.Of(world ?? World(), (cx, cz, lod) => lod == 0 && Wanted(cx, cz, 0));

    /// <summary>The trees: groves scattered by a hash, kept off the hill's top, the route, the road and the people.</summary>
    public static List<TreeSpot> Trees(TerrainWorld world)
    {
        var trees = new List<TreeSpot>();
        const double step = 5;
        int n = (int)(Extent * 2 / step);
        for (int j = 0; j < n; j++)
            for (int i = 0; i < n; i++)
            {
                double x = -Extent + (i + 0.15 + 0.7 * Noise.Hash01(i, j, 11)) * step;
                double z = -Extent + (j + 0.15 + 0.7 * Noise.Hash01(i, j, 12)) * step;
                double r = Math.Sqrt(x * x + z * z);
                if (r > Extent - 6) continue;
                // Groves: a coarse hash read smoothly, so trees stand together and clearings lie between; thinning
                // at a grove's edge
                double grove = Grove(x, z);
                double want = r < 40 ? 0.8 : 0.6;
                if (grove < want || Noise.Hash01(i, j, 13) > (grove - want) * 6) continue;
                if (Near(x, z, 6)) continue;
                var (h, norm) = world.Column(x, z);
                if (norm > 1.25) continue;
                bool pine = h > 14 || Noise.Hash01(i, j, 14) < 0.55;
                trees.Add(new TreeSpot(x, h, z, pine, 1.35 + 0.45 * Noise.Hash01(i, j, 15), i * 7919 + j));
            }
        return trees;
    }

    static double Grove(double x, double z)
    {
        const double s = 36;
        double fx = x / s, fz = z / s;
        int i = (int)Math.Floor(fx), j = (int)Math.Floor(fz);
        double tx = fx - i, tz = fz - j;
        tx = tx * tx * (3 - 2 * tx); tz = tz * tz * (3 - 2 * tz);
        double a = Noise.Hash01(i, j, 21), b = Noise.Hash01(i + 1, j, 21), c = Noise.Hash01(i, j + 1, 21), d = Noise.Hash01(i + 1, j + 1, 21);
        return (a * (1 - tx) + b * tx) * (1 - tz) + (c * (1 - tx) + d * tx) * tz;
    }

    /// <summary>Within <paramref name="reach"/> of the route, the roads, the people or the tunnel's cuttings.</summary>
    static bool Near(double x, double z, double reach)
    {
        var points = new List<(double X, double Z)> { (Start.X, Start.Z) };
        points.AddRange(Route.Select(w => (w.X, w.Z)));
        for (int s = 0; s + 1 < points.Count; s++)
            if (Segment(x, z, points[s], points[s + 1]) < reach) return true;
        if (Math.Abs(z) < 4 && Math.Abs(x) < TunnelEnd + 6) return true;
        foreach (var p in new[] { Person, PokemonSpot })
            if (Math.Sqrt((x - p.X) * (x - p.X) + (z - p.Z) * (z - p.Z)) < reach) return true;
        return Math.Sqrt(x * x + z * z) < 16;
    }

    static double Segment(double x, double z, (double X, double Z) a, (double X, double Z) b)
    {
        double abx = b.X - a.X, abz = b.Z - a.Z;
        double t = Math.Clamp(((x - a.X) * abx + (z - a.Z) * abz) / (abx * abx + abz * abz), 0, 1);
        double dx = x - (a.X + abx * t), dz = z - (a.Z + abz * t);
        return Math.Sqrt(dx * dx + dz * dz);
    }

    /// <summary>The ground under a point of the meadow: where the person and the Pokémon stand.</summary>
    public static D3 OnGround(CollisionMesh mesh, D3 p) =>
        p with { Y = mesh.GroundBelow(p.X, p.Z, 40, -5) ?? 0 };

    /// <summary>The walk of the route from the start.</summary>
    public static WalkRecord Walk(CollisionMesh mesh) =>
        WalkRecord.Run(mesh, OpenPos.Of(OnGround(mesh, Start)), Route, Trigger, Zone);
}
