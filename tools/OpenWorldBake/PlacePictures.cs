using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.OpenWorld;
using Raylib_cs;

namespace OpenWorldBake;

/// <summary>
/// An open map's places drawn over its plan (plan 32 · OW5; "Looking without a window"): every solid, water box,
/// encounter zone, trigger, door and marker of its <c>places/</c>, each in its colour with a tag the key names, as
/// ContentCheck's <c>--plan</c> tags a grid room's. A volume is drawn by its outline on the ground, whatever its heights,
/// which the key gives. GPU-free; written by <see cref="Pictures.Save"/>.
/// </summary>
public static class PlacePictures
{
    static readonly Color Solid = new(150, 96, 52, 255), Water = new(60, 140, 255, 255), Zone = new(110, 230, 90, 255),
        Trigger = new(250, 220, 40, 255), Door = new(240, 70, 200, 255), Marker = new(255, 255, 255, 255), Ink = new(20, 18, 28, 255);

    /// <summary>Draws the maps' places over <paramref name="plan"/> (drawn over <paramref name="box"/> at <paramref name="perMetre"/>), and returns its key, a line each.</summary>
    public static List<string> Over(PixelCanvas plan, IEnumerable<OpenMap> maps, (double X0, double Z0, double X1, double Z1) box, int perMetre)
    {
        var key = new List<string>();
        (int X, int Y) At(int x, int z) => ((int)Math.Floor((x / 1000.0 - box.X0) * perMetre), (int)Math.Floor((z / 1000.0 - box.Z0) * perMetre));
        void Line(int ax, int az, int bx, int bz, Color c) { var (x0, y0) = At(ax, az); var (x1, y1) = At(bx, bz); plan.Line(x0, y0, x1, y1, c); }
        void Rect(OpenPos min, OpenPos max, Color c)
        {
            Line(min.X, min.Z, max.X, min.Z, c); Line(max.X, min.Z, max.X, max.Z, c);
            Line(max.X, max.Z, min.X, max.Z, c); Line(min.X, max.Z, min.X, min.Z, c);
        }
        void Shape(OpenShape shape, Color c)
        {
            if (shape.Kind == OpenShapeKind.Box) { Rect(shape.Min, shape.Max, c); return; }
            var mid = shape.Base;
            for (int i = 0; i < 32; i++)
            {
                double a0 = i * Math.PI / 16, a1 = (i + 1) * Math.PI / 16;
                Line(mid.X + (int)(Math.Sin(a0) * shape.Radius), mid.Z + (int)(Math.Cos(a0) * shape.Radius), mid.X + (int)(Math.Sin(a1) * shape.Radius), mid.Z + (int)(Math.Cos(a1) * shape.Radius), c);
            }
        }
        void Tag(int x, int z, string tag, Color c)
        {
            var (px, py) = At(x, z);
            plan.Rect(px - 2, py - 2, 5, 5, Ink);
            plan.Rect(px - 1, py - 1, 3, 3, c);
            // The tag's first letter and number as dots under it: a tiny stand-in for lettering, the key says the rest
            for (int i = 0; i < tag.Length && i < 4; i++) plan.Rect(px + 4 + i * 2, py + 3, 1, 1, c);
        }
        string M(int mm) => (mm / 1000.0).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
        string P(OpenPos p) => $"[{M(p.X)}, {M(p.Y)}, {M(p.Z)}]";

        int n = 0;
        foreach (var map in maps)
        {
            foreach (var s in map.Solids)
            {
                Rect(s.Min, s.Max, Solid);
                key.Add($"S{++n} solid {s.Name}: {P(s.Min)} to {P(s.Max)} (brown)");
            }
            foreach (var w in map.Waters)
            {
                Rect(new OpenPos(w.MinX, 0, w.MinZ), new OpenPos(w.MaxX, 0, w.MaxZ), Water);
                key.Add($"A{++n} water {w.Name} at {M(w.Level)} m (blue)");
            }
            foreach (var z in map.Zones)
            {
                for (int i = 0; i < z.Polygon.Count; i++)
                {
                    var (ax, az) = z.Polygon[i]; var (bx, bz) = z.Polygon[(i + 1) % z.Polygon.Count];
                    Line(ax, az, bx, bz, Zone);
                }
                key.Add($"Z{++n} zone {z.Name} ({z.Place}), {M(z.MinY)} m to {M(z.MaxY)} m, cap {z.Cap}: {string.Join(", ", z.Slots.Select(s => $"{s.Species} {s.MinLevel}-{s.MaxLevel}"))} (green)");
            }
            foreach (var t in map.Triggers)
            {
                Shape(t.Shape, Trigger);
                Tag(t.Shape.Base.X, t.Shape.Base.Z, "T", Trigger);
                key.Add($"T{++n} trigger {t.Name} ({t.Place}), {t.Shape}: {t.Script}{(t.Variable != null ? $" while {t.Variable} is {t.Value}" : "")} (yellow)");
            }
            foreach (var d in map.Doors)
            {
                Shape(d.Shape, Door);
                Tag(d.Shape.Base.X, d.Shape.Base.Z, "D", Door);
                key.Add($"D{++n} door {d.Name} ({d.Place}) into {d.Room} at {d.RoomX},{d.RoomY}, out onto {d.Out}{(d.OpenedBy != null ? $", opened by {d.OpenedBy}" : "")} (magenta)");
            }
            foreach (var m in map.Markers)
            {
                var (px, py) = At(m.Pos.X, m.Pos.Z);
                plan.Rect(px - 2, py - 2, 5, 5, Ink);
                plan.Rect(px - 1, py - 1, 3, 3, Marker);
                // A tick the way it faces: nought south (down the picture), a quarter east
                double yaw = OpenYaw.Radians(m.Yaw);
                plan.Line(px, py, px + (int)Math.Round(Math.Sin(yaw) * 7), py + (int)Math.Round(Math.Cos(yaw) * 7), Marker);
                key.Add($"M{++n} marker {m.Name} at {P(m.Pos)}, facing {m.Yaw * 360 / OpenYaw.Turn}° (white, its tick the way it faces)");
            }
        }
        return key;
    }
}
