using PokemonPlatinumEngine.Graphics;
using Raylib_cs;

namespace OpenWorldBake;

/// <summary>
/// A region's bake drawn without a window (plan 32, "Looking without a window"): an area from above, its top surface
/// shaded and covered as the bake has it, the layers under it, the water, the roads and the areas, with a level's
/// triangles over it if asked; and a vertical cut along a line, the field's rock and air with each level's surface
/// where it crosses the cut. Drawn on the CPU and written as PNGs; the key is printed.
/// </summary>
public static class RegionPictures
{
    static readonly Color[] AreaColours =
    {
        new(250, 250, 250, 255), new(240, 200, 40, 255), new(40, 200, 230, 255), new(230, 70, 200, 255),
        new(250, 130, 40, 255), new(120, 230, 90, 255), new(200, 60, 60, 255), new(90, 110, 240, 255)
    };

    /// <summary>The ground of some chunks' meshes (their own triangles, and the aprons too if asked) as one mesh to cast down at.</summary>
    public static CollisionMesh Surface(IEnumerable<ChunkMesh> chunks, bool apron = false)
    {
        var mesh = new CollisionMesh();
        foreach (var chunk in chunks)
            for (int t = 0; t < chunk.GroundTriangles + (apron ? chunk.ApronTriangles : 0); t++)
                mesh.Add(chunk.Vertex(chunk.Indices[t * 3]), chunk.Vertex(chunk.Indices[t * 3 + 1]), chunk.Vertex(chunk.Indices[t * 3 + 2]));
        return mesh;
    }

    /// <summary>
    /// The land from above over a rectangle, <paramref name="perMetre"/> pixels a metre: the top of level 0 shaded by its
    /// slope and coloured by its cover, a violet wash where a walkable floor lies under it (a cave), blue where water
    /// stands over it, the roads' lines, each area's outline in its colour, and <paramref name="wire"/>'s triangles if
    /// given (a level's, to see how far it was thinned).
    /// </summary>
    public static PixelCanvas Plan(RegionDescription description, TerrainWorld land, CollisionMesh finest, (double X0, double Z0, double X1, double Z1) box,
        ChunkMesh[]? wire = null, int perMetre = 4)
    {
        int w = (int)Math.Ceiling((box.X1 - box.X0) * perMetre), h = (int)Math.Ceiling((box.Z1 - box.Z0) * perMetre);
        var canvas = new PixelCanvas(w, h);
        var light = new D3(-0.5, 0.75, -0.45).Normalized();
        for (int r = 0; r < h; r++)
            for (int s = 0; s < w; s++)
            {
                double x = box.X0 + (s + 0.5) / perMetre, z = box.Z0 + (r + 0.5) / perMetre;
                var column = finest.Column(x, z);
                if (column.Count == 0) { canvas.SetRaw(s, r, new Color(20, 18, 28, 255)); continue; }
                var (y, t) = column[0];
                var n = finest.Triangles[t].Normal;
                var c = Pictures.CoverColour(land.CoverAt(new D3(x, y, z), n));
                double shade = 0.72 + 0.35 * Math.Max(0, D3.Dot(n, light));
                c = new Color((byte)Math.Clamp(c.R * shade, 0, 255), (byte)Math.Clamp(c.G * shade, 0, 255), (byte)Math.Clamp(c.B * shade, 0, 255), (byte)255);
                var floors = column.Where(f => finest.Walkable(f.Triangle)).ToList();
                if (floors.Count >= 2 && floors[0].Y - floors[^1].Y > 2) c = PixelCanvas.Mix(c, new Color(130, 70, 200, 255), 0.35f);
                if (land.WaterAt(x, z) is { } level && level > y) c = PixelCanvas.Mix(c, new Color(50, 110, 210, 255), (float)Math.Clamp(0.45 + (level - y) * 0.15, 0.45, 0.8));
                canvas.SetRaw(s, r, c);
            }
        (int X, int Y) At(double x, double z) => ((int)Math.Floor((x - box.X0) * perMetre), (int)Math.Floor((z - box.Z0) * perMetre));
        void Line(double ax, double az, double bx, double bz, Color c) { var (x0, y0) = At(ax, az); var (x1, y1) = At(bx, bz); canvas.Line(x0, y0, x1, y1, c); }

        if (wire != null)
        {
            var ink = new Color(30, 26, 40, 255);
            foreach (var chunk in wire)
                for (int t = 0; t < chunk.GroundTriangles; t++)
                {
                    var a = chunk.Vertex(chunk.Indices[t * 3]); var b = chunk.Vertex(chunk.Indices[t * 3 + 1]); var cc = chunk.Vertex(chunk.Indices[t * 3 + 2]);
                    // Only the top: a face under another (a cave's) is left out, or the lines are a tangle
                    var top = finest.Column((a.X + b.X + cc.X) / 3, (a.Z + b.Z + cc.Z) / 3);
                    if (top.Count > 0 && top[0].Y - (a.Y + b.Y + cc.Y) / 3 > 1) continue;
                    Line(a.X, a.Z, b.X, b.Z, ink); Line(b.X, b.Z, cc.X, cc.Z, ink); Line(cc.X, cc.Z, a.X, a.Z, ink);
                }
        }
        foreach (var road in land.GradedRoads)
            for (int i = 0; i + 1 < road.Line.Samples.Length; i++)
                Line(road.Line.Samples[i].X, road.Line.Samples[i].Z, road.Line.Samples[i + 1].X, road.Line.Samples[i + 1].Z, new Color(120, 82, 40, 255));
        var areas = description.World.Areas;
        for (int a = 0; a < areas.Count; a++)
        {
            var polygon = areas[a].Polygon;
            for (int i = 0; i < polygon.Length; i++)
            {
                var p = polygon[i]; var q = polygon[(i + 1) % polygon.Length];
                Line(p[0], p[1], q[0], q[1], AreaColours[a % AreaColours.Length]);
            }
        }
        return canvas;
    }

    /// <summary>What each area's outline is drawn in, in the order of world.json.</summary>
    public static IEnumerable<string> AreaKey(RegionDescription description) =>
        description.World.Areas.Select((a, i) => $"{a.Key} ({a.Name ?? "no name"}, {a.MinY:0.#} m to {a.MaxY:0.#} m): {Name(AreaColours[i % AreaColours.Length])}");

    static string Name(Color c) => (c.R, c.G, c.B) switch
    {
        (250, 250, 250) => "white", (240, 200, 40) => "yellow", (40, 200, 230) => "cyan", (230, 70, 200) => "magenta",
        (250, 130, 40) => "orange", (120, 230, 90) => "green", (200, 60, 60) => "red", _ => "blue"
    };

    /// <summary>
    /// A vertical cut from <paramref name="a"/> to <paramref name="b"/>: the field's rock and air (as the prototype's
    /// section draws them), the water's surface in blue, and where each level's surface crosses the cut, level 0 black,
    /// level 1 orange and level 2 magenta, so a thinned level's cave can be seen to keep its hole.
    /// </summary>
    public static PixelCanvas Section(TerrainWorld land, IEnumerable<ChunkMesh> meshes, (double X, double Z) a, (double X, double Z) b, double low, double high, int perMetre = 8)
    {
        var canvas = Pictures.Section(land, a, b, low, high, perMetre: perMetre);
        double dx = b.X - a.X, dz = b.Z - a.Z, length = Math.Sqrt(dx * dx + dz * dz);
        double ux = dx / length, uz = dz / length;
        int Col(double s) => (int)Math.Floor(s * perMetre);
        int Row(double y) => (int)Math.Floor((high - y) * perMetre);
        double Away(D3 p) => (p.X - a.X) * uz - (p.Z - a.Z) * ux;
        double Along(D3 p) => (p.X - a.X) * ux + (p.Z - a.Z) * uz;

        // The water's surface along the cut
        for (int s = 0; s < canvas.Width; s++)
        {
            double t = (s + 0.5) / perMetre, x = a.X + ux * t, z = a.Z + uz * t;
            if (land.WaterAt(x, z) is { } level && level > land.Height(x, z) && Row(level) >= 0 && Row(level) < canvas.Height)
                canvas.SetRaw(s, Row(level), new Color(40, 100, 220, 255));
        }
        var colours = new[] { new Color(20, 18, 28, 255), new Color(240, 130, 30, 255), new Color(220, 40, 200, 255) };
        foreach (var chunk in meshes.OrderBy(m => m.Lod))
            for (int t = 0; t < chunk.GroundTriangles; t++)
            {
                var p = new[] { chunk.Vertex(chunk.Indices[t * 3]), chunk.Vertex(chunk.Indices[t * 3 + 1]), chunk.Vertex(chunk.Indices[t * 3 + 2]) };
                var d = p.Select(Away).ToArray();
                var cut = new List<D3>();
                for (int e = 0; e < 3; e++)
                {
                    int f = (e + 1) % 3;
                    if ((d[e] < 0) != (d[f] < 0)) cut.Add(p[e] + (p[f] - p[e]) * (d[e] / (d[e] - d[f])));
                }
                if (cut.Count != 2) continue;
                double s0 = Along(cut[0]), s1 = Along(cut[1]);
                if (Math.Max(s0, s1) < 0 || Math.Min(s0, s1) > length) continue;
                // Each level a pixel lower than the one before, so where they agree all can be seen
                int off = chunk.Lod;
                canvas.Line(Col(s0), Row(cut[0].Y) + off, Col(s1), Row(cut[1].Y) + off, colours[Math.Min(chunk.Lod, 2)]);
            }
        return canvas;
    }
}
