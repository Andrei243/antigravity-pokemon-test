using PokemonPlatinumEngine.Graphics;
using Raylib_cs;

namespace OpenWorldBake;

/// <summary>
/// Looking at a baked world without a window (plan 32, "Looking without a window"): a vertical section along a
/// line, which is how a cave under a hill is seen, and a plan from above. Drawn on the CPU and written as PNGs.
/// </summary>
public static class Pictures
{
    static readonly Color Air = new(206, 226, 240, 255), Rock = new(122, 112, 104, 255), Soil = new(98, 140, 78, 255);
    static readonly Color Edge = new(30, 28, 36, 255), Grid = new(170, 192, 210, 255);

    public static Color CoverColour(Cover cover) => cover switch
    {
        Cover.Grass => new Color(104, 190, 98, 255),
        Cover.GrassLight => new Color(120, 200, 102, 255),
        Cover.GrassDeep => new Color(80, 162, 88, 255),
        Cover.Path => new Color(222, 204, 160, 255),
        Cover.Rock => new Color(150, 140, 128, 255),
        Cover.RockDark => new Color(112, 104, 98, 255),
        Cover.CaveFloor => new Color(128, 108, 88, 255),
        _ => new Color(136, 168, 112, 255)
    };

    /// <summary>
    /// A vertical cut from <paramref name="a"/> to <paramref name="b"/> (x and z), from <paramref name="low"/> to
    /// <paramref name="high"/> metres up, eight pixels a metre: rock, air and the surface between them; the walk's
    /// positions within 1.5 m of the cut (blue under rock, orange in the open), the trigger's box and the zone's height.
    /// </summary>
    public static PixelCanvas Section(TerrainWorld world, (double X, double Z) a, (double X, double Z) b, double low, double high,
        WalkRecord? walk = null, TriggerBox? trigger = null, Zone? zone = null, int perMetre = 8)
    {
        double length = Math.Sqrt((b.X - a.X) * (b.X - a.X) + (b.Z - a.Z) * (b.Z - a.Z));
        int w = (int)Math.Ceiling(length * perMetre), h = (int)Math.Ceiling((high - low) * perMetre);
        var canvas = new PixelCanvas(w, h);
        var solid = new bool[w, h];
        for (int s = 0; s < w; s++)
        {
            double t = (s + 0.5) / w;
            double x = a.X + (b.X - a.X) * t, z = a.Z + (b.Z - a.Z) * t;
            double surface = world.Height(x, z);
            for (int r = 0; r < h; r++)
            {
                double y = high - (r + 0.5) / perMetre;
                bool rock = world.Field(new D3(x, y, z)) < 0;
                solid[s, r] = rock;
                canvas.SetRaw(s, r, rock ? (y > surface - 0.8 ? Soil : Rock) : (Math.Abs(y % 5) < 0.5 / perMetre * 2 && y > low ? Grid : Air));
            }
        }
        for (int s = 0; s < w; s++)
            for (int r = 0; r < h; r++)
                if (solid[s, r] && ((s > 0 && !solid[s - 1, r]) || (s + 1 < w && !solid[s + 1, r]) || (r > 0 && !solid[s, r - 1]) || (r + 1 < h && !solid[s, r + 1])))
                    canvas.SetRaw(s, r, Edge);

        (double S, double Away) Along(double x, double z)
        {
            double dx = b.X - a.X, dz = b.Z - a.Z;
            double t = ((x - a.X) * dx + (z - a.Z) * dz) / (length * length);
            double px = a.X + dx * t, pz = a.Z + dz * t;
            return (t * length, Math.Sqrt((x - px) * (x - px) + (z - pz) * (z - pz)));
        }
        int Col(double s) => (int)Math.Floor(s * perMetre);
        int Row(double y) => (int)Math.Floor((high - y) * perMetre);

        if (zone != null)
        {
            // The zone's floor and ceiling where the cut crosses its polygon
            for (int s = 0; s < w; s++)
            {
                double t = (s + 0.5) / w;
                double x = a.X + (b.X - a.X) * t, z = a.Z + (b.Z - a.Z) * t;
                if (!zone.Holds(new D3(x, zone.MinY, z))) continue;
                foreach (double y in new[] { zone.MinY, zone.MaxY })
                    if (Row(y) >= 0 && Row(y) < h && s % 4 < 2) canvas.SetRaw(s, Row(y), new Color(214, 170, 30, 255));
            }
        }
        if (trigger != null)
        {
            var (s0, away0) = Along(trigger.Min.X, trigger.Min.Z);
            var (s1, away1) = Along(trigger.Max.X, trigger.Max.Z);
            int c0 = Col(Math.Min(s0, s1)), c1 = Col(Math.Max(s0, s1)), r0 = Row(trigger.Max.Y), r1 = Row(trigger.Min.Y);
            var red = new Color(220, 40, 40, 255);
            for (int c = c0; c <= c1; c++) { Dot(canvas, c, r0, red); Dot(canvas, c, r1, red); }
            for (int r = r0; r <= r1; r++) { Dot(canvas, c0, r, red); Dot(canvas, c1, r, red); }
        }
        if (walk != null)
        {
            foreach (var sample in walk.Samples.Where((_, i) => i % 12 == 0))
            {
                var p = sample.Pos.Metres;
                var (s, away) = Along(p.X, p.Z);
                if (away > 1.5 || s < 0 || s > length) continue;
                bool under = p.Y < world.Height(p.X, p.Z) - 0.8;
                var colour = under ? new Color(40, 90, 230, 255) : new Color(240, 120, 20, 255);
                // The capsule's feet and head, a mark at each
                Dot(canvas, Col(s), Row(p.Y + 0.05), colour);
                Dot(canvas, Col(s), Row(p.Y + OpenBody.Height), PixelCanvas.Mix(colour, Air, 0.5f));
            }
        }
        return canvas;
    }

    static void Dot(PixelCanvas canvas, int x, int y, Color c)
    {
        for (int dy = 0; dy < 2; dy++)
            for (int dx = 0; dx < 2; dx++)
                if (canvas.InBounds(x + dx, y + dy)) canvas.SetRaw(x + dx, y + dy, c);
    }

    /// <summary>
    /// The ground from above, <paramref name="perMetre"/> pixels a metre: the top surface's cover shaded by its slope,
    /// a violet wash where a walkable floor lies under it (the layers), the trees, the walk (white), the paths the
    /// navigation found (cyan through the tunnel, magenta with the tunnel barred), the trigger (red), the zone
    /// (yellow), the person (blue) and the Pokémon (orange).
    /// </summary>
    public static PixelCanvas Plan(TerrainWorld world, double minX, double minZ, double maxX, double maxZ, CollisionMesh? mesh = null,
        WalkRecord? walk = null, IEnumerable<TreeSpot>? trees = null, IEnumerable<(List<D3> Path, Color Colour)>? paths = null, int perMetre = 4)
    {
        int w = (int)((maxX - minX) * perMetre), h = (int)((maxZ - minZ) * perMetre);
        var canvas = new PixelCanvas(w, h);
        var light = new D3(-0.5, 0.75, -0.45).Normalized();
        for (int r = 0; r < h; r++)
            for (int s = 0; s < w; s++)
            {
                double x = minX + (s + 0.5) / perMetre, z = minZ + (r + 0.5) / perMetre;
                const double e = 0.25;
                double hx = (world.Height(x + e, z) - world.Height(x - e, z)) / (2 * e);
                double hz = (world.Height(x, z + e) - world.Height(x, z - e)) / (2 * e);
                var n = new D3(-hx, 1, -hz).Normalized();
                double y = world.Height(x, z);
                // The top surface may be cut open (the tunnel's cuttings): then the floor below is the top
                if (world.Field(new D3(x, y - 0.25, z)) > 0 && mesh != null && mesh.GroundBelow(x, z, y, -10) is { } floor) { y = floor; n = new D3(0, 1, 0); }
                var c = CoverColour(world.CoverAt(new D3(x, y, z), n));
                double shade = 0.72 + 0.35 * Math.Max(0, D3.Dot(n, light));
                c = new Color((byte)Math.Clamp(c.R * shade, 0, 255), (byte)Math.Clamp(c.G * shade, 0, 255), (byte)Math.Clamp(c.B * shade, 0, 255), (byte)255);
                if (mesh != null)
                {
                    var floors = mesh.Column(x, z).Where(f => mesh.Walkable(f.Triangle)).ToList();
                    if (floors.Count >= 2 && floors[0].Y - floors[^1].Y > 2) c = PixelCanvas.Mix(c, new Color(130, 70, 200, 255), 0.35f);
                }
                canvas.SetRaw(s, r, c);
            }
        (int X, int Y) At(double x, double z) => ((int)Math.Floor((x - minX) * perMetre), (int)Math.Floor((z - minZ) * perMetre));
        void Line(D3 p, D3 q, Color c) { var (x0, y0) = At(p.X, p.Z); var (x1, y1) = At(q.X, q.Z); canvas.Line(x0, y0, x1, y1, c); }

        if (trees != null)
            foreach (var t in trees)
            {
                var (x, y) = At(t.X, t.Z);
                if (canvas.InBounds(x, y)) canvas.Disc(x, y, (float)(t.Pine ? 1.2 * t.Scale : 1.6 * t.Scale), t.Pine ? new Color(34, 92, 64, 255) : new Color(52, 120, 60, 255));
            }
        var zone = Prototype.Zone;
        for (int i = 0; i < zone.Polygon.Length; i++)
        {
            var p = zone.Polygon[i]; var q = zone.Polygon[(i + 1) % zone.Polygon.Length];
            Line(new D3(p.X, 0, p.Z), new D3(q.X, 0, q.Z), new Color(240, 200, 40, 255));
        }
        var trig = Prototype.Trigger;
        var red = new Color(220, 40, 40, 255);
        Line(new D3(trig.Min.X, 0, trig.Min.Z), new D3(trig.Max.X, 0, trig.Min.Z), red);
        Line(new D3(trig.Max.X, 0, trig.Min.Z), new D3(trig.Max.X, 0, trig.Max.Z), red);
        Line(new D3(trig.Max.X, 0, trig.Max.Z), new D3(trig.Min.X, 0, trig.Max.Z), red);
        Line(new D3(trig.Min.X, 0, trig.Max.Z), new D3(trig.Min.X, 0, trig.Min.Z), red);
        if (paths != null)
            foreach (var (path, colour) in paths)
                for (int i = 0; i + 1 < path.Count; i++) Line(path[i], path[i + 1], colour);
        if (walk != null)
        {
            D3? last = null;
            foreach (var sample in walk.Samples.Where((_, i) => i % 24 == 0))
            {
                var p = sample.Pos.Metres;
                if (last is { } l) Line(l, p, Color.White);
                last = p;
            }
        }
        foreach (var (p, colour) in new[] { (Prototype.Person, new Color(40, 90, 230, 255)), (Prototype.PokemonSpot, new Color(240, 120, 20, 255)) })
        {
            var (x, y) = At(p.X, p.Z);
            canvas.Disc(x, y, 3, colour);
        }
        return canvas;
    }

    public static void Save(PixelCanvas canvas, string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        var image = canvas.ToImage();
        Raylib.ExportImage(image, path);
        Raylib.UnloadImage(image);
    }
}
