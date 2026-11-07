using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.OpenWorld;
using Raylib_cs;

namespace OpenWorldBake;

/// <summary>
/// A route walked by the game's capsule drawn without a window (plan 32 · OW3, <c>--walk</c>): over the region's plan,
/// its feet tick by tick, white standing, orange sliding down a steep face, red falling, cyan swimming, a yellow ring
/// at each stop reached and a magenta one at each given up; and its profile, the feet's height (in the same colours)
/// over every surface its columns meet (grey, the top darker), along the distance walked.
/// </summary>
public static class WalkPictures
{
    static readonly Color Standing = new(250, 250, 250, 255), Sliding = new(250, 140, 40, 255), Falling = new(220, 40, 40, 255),
        Swimming = new(40, 220, 240, 255), Reached = new(250, 220, 40, 255), GivenUp = new(230, 60, 220, 255);

    static Color Of(RouteSample s) => s.Stance switch
    {
        OpenStance.Falling => Falling,
        OpenStance.Swimming => Swimming,
        _ => s.Sliding ? Sliding : Standing
    };

    /// <summary>The walk's feet over a plan of the region drawn at <paramref name="perMetre"/> pixels a metre from <paramref name="box"/>'s corner.</summary>
    public static void Over(PixelCanvas plan, CapsuleRoute walk, IReadOnlyList<RouteStop> stops, (double X0, double Z0, double X1, double Z1) box, int perMetre)
    {
        (int X, int Y) At(double x, double z) => ((int)Math.Floor((x - box.X0) * perMetre), (int)Math.Floor((z - box.Z0) * perMetre));
        RouteSample? last = null;
        foreach (var s in walk.Samples)
        {
            if (last is { } p)
            {
                var (x0, y0) = At(p.Pos.MetresX, p.Pos.MetresZ);
                var (x1, y1) = At(s.Pos.MetresX, s.Pos.MetresZ);
                plan.Line(x0, y0, x1, y1, Of(s));
            }
            last = s;
        }
        foreach (var stop in stops)
        {
            var (x, y) = At(stop.X, stop.Z);
            var c = walk.GivenUp.Contains(stop.Name) ? GivenUp : Reached;
            for (int a = 0; a < 24; a++)
            {
                double t = a * Math.PI / 12;
                int r = Math.Max(3, perMetre / 2);
                plan.SetRaw(Math.Clamp(x + (int)Math.Round(Math.Cos(t) * r), 0, plan.Width - 1), Math.Clamp(y + (int)Math.Round(Math.Sin(t) * r), 0, plan.Height - 1), c);
            }
        }
    }

    /// <summary>The walk's profile: <paramref name="perMetre"/> pixels a metre along the distance walked and up.</summary>
    public static PixelCanvas Profile(CapsuleRoute walk, OpenGround ground, int perMetre = 8)
    {
        var points = new List<(double Along, RouteSample Sample)>();
        double along = 0;
        RouteSample? last = null;
        foreach (var s in walk.Samples)
        {
            if (last is { } p) along += Math.Sqrt(s.Pos.GroundDistanceSquared(p.Pos)) / 1000.0;
            points.Add((along, s));
            last = s;
        }
        // The heights shown: every surface the columns meet, and the feet
        double low = double.MaxValue, high = double.MinValue;
        var columns = new List<(double Along, List<(double Y, int Triangle)> Surfaces)>();
        int step = Math.Max(1, OpenClock.TicksPerSecond / 20);
        for (int i = 0; i < points.Count; i += step)
        {
            var (a, s) = points[i];
            var c = ground.Column(s.Pos.MetresX, s.Pos.MetresZ);
            columns.Add((a, c));
            foreach (var (y, _) in c) { low = Math.Min(low, y); high = Math.Max(high, y); }
            low = Math.Min(low, s.Pos.MetresY); high = Math.Max(high, s.Pos.MetresY + PokemonPlatinumEngine.OpenWorld.OpenBody.Height);
        }
        if (points.Count == 0) return new PixelCanvas(8, 8);
        low = Math.Floor(low - 2); high = Math.Ceiling(high + 2);
        int w = Math.Max(8, (int)Math.Ceiling(along * perMetre) + 1), h = Math.Max(8, (int)Math.Ceiling((high - low) * perMetre) + 1);
        var canvas = new PixelCanvas(w, h);
        int Col(double a) => Math.Clamp((int)Math.Floor(a * perMetre), 0, w - 1);
        int Row(double y) => Math.Clamp((int)Math.Floor((high - y) * perMetre), 0, h - 1);
        for (int r = 0; r < h; r++)
            for (int s = 0; s < w; s++)
                canvas.SetRaw(s, r, (int)Math.Floor(high - (r + 0.5) / perMetre) % 5 == 0 && r % perMetre == 0 ? new Color(200, 200, 210, 255) : new Color(232, 234, 240, 255));
        foreach (var (a, surfaces) in columns)
            for (int k = 0; k < surfaces.Count; k++)
            {
                var face = ground[surfaces[k].Triangle];
                var c = k == 0 ? new Color(70, 80, 70, 255) : face.FacesUp ? new Color(120, 110, 100, 255) : new Color(160, 150, 150, 255);
                canvas.SetRaw(Col(a), Row(surfaces[k].Y), c);
                canvas.SetRaw(Math.Min(w - 1, Col(a) + 1), Row(surfaces[k].Y), c);
            }
        for (int i = 1; i < points.Count; i++)
        {
            var (a0, s0) = points[i - 1];
            var (a1, s1) = points[i];
            canvas.Line(Col(a0), Row(s0.Pos.MetresY), Col(a1), Row(s1.Pos.MetresY), Of(s1) == Standing ? new Color(30, 30, 40, 255) : Of(s1));
        }
        return canvas;
    }
}
