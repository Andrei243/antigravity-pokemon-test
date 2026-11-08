using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.OpenWorld;
using Raylib_cs;

namespace OpenWorldBake;

/// <summary>
/// The navigation mesh drawn without a window (plan 32 · OW4, <c>--nav</c>): over the region's plan, each polygon's
/// outline (dark on the top of the land, violet where it lies under other ground: a cave's floor, a tunnel's), swim
/// polygons washed cyan, the shared edges green, a step off the mesh yellow and a drop red (a line from where it is
/// taken to where it lands, a dot at the landing), and paths white with their corners ringed.
/// </summary>
public static class NavPictures
{
    static readonly Color Outline = new(30, 34, 40, 255), Under = new(170, 90, 230, 255), Portal = new(60, 200, 90, 255),
        Step = new(250, 220, 40, 255), Drop = new(230, 50, 50, 255), SwimWash = new(40, 210, 240, 255), PathColour = new(255, 255, 255, 255);

    /// <summary>The mesh and <paramref name="paths"/> over a plan drawn at <paramref name="perMetre"/> pixels a metre from <paramref name="box"/>'s corner.</summary>
    public static void Over(PixelCanvas plan, OpenNavMesh mesh, OpenGround ground, (double X0, double Z0, double X1, double Z1) box, int perMetre,
        IEnumerable<NavPath>? paths = null)
    {
        (int X, int Y) At(double x, double z) => ((int)Math.Floor((x - box.X0) * perMetre), (int)Math.Floor((z - box.Z0) * perMetre));
        void Line(OpenVec a, OpenVec b, Color c)
        {
            var (x0, y0) = At(a.X, a.Z); var (x1, y1) = At(b.X, b.Z);
            plan.Line(x0, y0, x1, y1, c);
        }
        void Dot(OpenVec p, Color c, int r)
        {
            var (x, y) = At(p.X, p.Z);
            for (int dy = -r; dy <= r; dy++)
                for (int dx = -r; dx <= r; dx++)
                    if (dx * dx + dy * dy <= r * r) plan.SetRaw(Math.Clamp(x + dx, 0, plan.Width - 1), Math.Clamp(y + dy, 0, plan.Height - 1), c);
        }
        foreach (var p in mesh.Polygons)
        {
            var hub = OpenVec.Of(p.Hub);
            var top = ground.Footing(hub.X, hub.Z, 1000, -1000);
            bool under = top is { } t && t.Y - hub.Y > 2;
            if (p.Swim)
            {
                // A wash over the polygon's inside, a pixel in four
                for (int k = 0; k < p.Corners.Length; k++)
                {
                    var a = mesh.Corner(p, k); var b = mesh.Corner(p, (k + 1) % p.Corners.Length);
                    for (double s = 0.1; s < 1; s += 0.2) Line(hub + (a - hub) * s, hub + (b - hub) * s, SwimWash);
                }
            }
            for (int k = 0; k < p.Corners.Length; k++)
                Line(mesh.Corner(p, k), mesh.Corner(p, (k + 1) % p.Corners.Length), under ? Under : Outline);
        }
        foreach (var l in mesh.Links)
        {
            if (!l.OffMesh) { if (l.From < l.To) Line(OpenVec.Of(l.A), OpenVec.Of(l.B), Portal); continue; }
            var c = (l.Flags & OpenNavFlags.Drop) != 0 ? Drop : Step;
            Line(OpenVec.Of(l.A), OpenVec.Of(l.Land), c);
            Dot(OpenVec.Of(l.Land), c, Math.Max(1, perMetre / 6));
        }
        foreach (var path in paths ?? Enumerable.Empty<NavPath>())
        {
            for (int i = 1; i < path.Corners.Count; i++) Line(OpenVec.Of(path.Corners[i - 1]), OpenVec.Of(path.Corners[i]), PathColour);
            foreach (var c in path.Corners) Dot(OpenVec.Of(c), PathColour, Math.Max(1, perMetre / 4));
        }
    }
}
