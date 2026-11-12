using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace PokemonPlatinumEngine.Models;

/// <summary>A subject's box on the frame, in its 1920 × 1080 pixels.</summary>
public readonly record struct TourBox(float X, float Y, float W, float H)
{
    public float Area => Math.Max(0f, W) * Math.Max(0f, H);
    public float Right => X + W;
    public float Bottom => Y + H;

    /// <summary>The area two boxes share.</summary>
    public float Overlap(TourBox other)
    {
        float w = Math.Min(Right, other.Right) - Math.Max(X, other.X);
        float h = Math.Min(Bottom, other.Bottom) - Math.Max(Y, other.Y);
        return w > 0f && h > 0f ? w * h : 0f;
    }
}

/// <summary>A subject as the guide judged it: its stars, the five parts of its points, and its points.</summary>
public sealed record JudgedSubject(int Resident, string Look, TourAct Act, int Stars, TourBox Box, float Visible,
    int Size, int Placement, int Facing, int Uncovered, int Kind, int Points);

/// <summary>
/// The guide's judging of a photograph (plan 44 · SD5): each resident in the frame given points for its size (best at a
/// third to two thirds of the frame's height), its placement, facing the lens, being uncovered and others of its kind
/// beside it, multiplied by its stars for what it was doing: one for a habit, two for its day, three for a reaction,
/// four for the rare action. All our own numbers; GPU-free.
/// </summary>
public static class PhotoJudge
{
    /// <summary>A subject's width on the frame as a share of its height.</summary>
    public const float WidthShare = 0.8f;

    /// <summary>A subject counts only when this much of it shows and it is this many pixels tall.</summary>
    public const float LeastVisible = 0.3f, LeastHeight = 40f;

    public const int MostSize = 400, MostPlacement = 200, MostFacing = 200, MostUncovered = 200, KindEach = 100, MostKind = 200;

    /// <summary>The stars for what a resident was doing.</summary>
    public static int StarsOf(TourAct act) => act switch
    {
        TourAct.Idle or TourAct.Walk => 1,
        TourAct.Sleep or TourAct.Eat or TourAct.Play or TourAct.Show => 2,
        TourAct.Rare => 4,
        _ => 3
    };

    /// <summary>The box a subject stands in on the frame: its feet to its head, as wide as <see cref="WidthShare"/> of that; null behind the lens.</summary>
    public static TourBox? BoxOf(TourCamera camera, TourSubject s) => BoxOf(camera, s.Position, s.Height, s.Height * WidthShare);

    /// <summary>The box of anything standing at <paramref name="foot"/>, so tall and so wide (a subject or a screen).</summary>
    public static TourBox? BoxOf(TourCamera camera, Vector3 foot, float height, float width)
    {
        if (!camera.Project(foot, out var feet, out float depth) || !camera.Project(foot + new Vector3(0, height, 0), out var head, out _)) return null;
        float w = width * camera.PixelsPerUnit(depth);
        float top = Math.Min(head.Y, feet.Y), bottom = Math.Max(head.Y, feet.Y);
        float cx = (feet.X + head.X) / 2f;
        return new TourBox(cx - w / 2f, top, w, bottom - top);
    }

    /// <summary>Points for its height as a share of the frame's: the most from a third to two thirds, falling to none at none and at the whole.</summary>
    public static int SizePoints(float share) =>
        share < 1f / 3f ? (int)(MostSize * Math.Max(0f, share) * 3f)
        : share <= 2f / 3f ? MostSize
        : (int)(MostSize * Math.Max(0f, 1f - (share - 2f / 3f) * 3f));

    /// <summary>Points for how near the middle its box's middle stands.</summary>
    public static int PlacementPoints(TourBox box)
    {
        float dx = (box.X + box.W / 2f - TourCamera.FrameWidth / 2f) / (TourCamera.FrameWidth / 2f);
        float dy = (box.Y + box.H / 2f - TourCamera.FrameHeight / 2f) / (TourCamera.FrameHeight / 2f);
        return (int)(MostPlacement * Math.Max(0f, 1f - MathF.Sqrt(dx * dx + dy * dy)));
    }

    /// <summary>Points for facing the lens: the cosine of the angle between its way and the way to the lens, on the ground.</summary>
    public static int FacingPoints(TourSubject s, Vector3 eye)
    {
        var to = new Vector2(eye.X - s.X, eye.Z - s.Z);
        if (to.LengthSquared() < 1e-6f) return MostFacing;
        to = Vector2.Normalize(to);
        float cos = MathF.Sin(s.Facing) * to.X + MathF.Cos(s.Facing) * to.Y;
        return (int)(MostFacing * Math.Max(0f, cos));
    }

    /// <summary>A photograph's total for a subject: its points times its stars and one, halved (so ×1 to ×2.5).</summary>
    public static int Total(int points, int stars) => points * (stars + 1) / 2;

    /// <summary>Every subject that counts in a photograph, judged, the best first.</summary>
    public static IReadOnlyList<JudgedSubject> Judge(TourFrame frame, TourCourseData? course = null)
    {
        var camera = frame.Camera;
        var frameBox = new TourBox(0, 0, TourCamera.FrameWidth, TourCamera.FrameHeight);
        // Each subject's box and how far it stands, nearer first
        var boxes = new List<(TourSubject S, TourBox Box, float Depth)>();
        foreach (var s in frame.Subjects)
            if (BoxOf(camera, s) is { } box && box.Overlap(frameBox) > 0f)
                boxes.Add((s, box, Vector3.Dot(s.Position - camera.Eye, camera.Forward)));
        var screens = new List<(TourBox Box, float Depth)>();
        if (course != null)
            foreach (var sc in course.Screens)
            {
                var foot = new Vector3(sc.At[0], 0, sc.At.Length > 1 ? sc.At[1] : 0);
                if (BoxOf(camera, foot, sc.Height, sc.Width) is { } box) screens.Add((box, Vector3.Dot(foot - camera.Eye, camera.Forward)));
            }

        var counted = new List<(TourSubject S, TourBox Box, float Visible)>();
        foreach (var (s, box, depth) in boxes)
        {
            if (box.Area <= 0f) continue;
            float clip = box.Overlap(frameBox) / box.Area;
            float covered = 0f;
            foreach (var (o, obox, odepth) in boxes)
                if (!ReferenceEquals(o, s) && odepth < depth) covered += obox.Overlap(box) / box.Area;
            foreach (var (sbox, sdepth) in screens)
                if (sdepth < depth) covered += sbox.Overlap(box) / box.Area;
            float visible = clip * Math.Clamp(1f - covered, 0f, 1f);
            if (visible >= LeastVisible && box.H >= LeastHeight) counted.Add((s, box, visible));
        }

        var judged = new List<JudgedSubject>();
        foreach (var (s, box, visible) in counted)
        {
            int size = SizePoints(box.H / TourCamera.FrameHeight);
            int place = PlacementPoints(box);
            int facing = FacingPoints(s, camera.Eye);
            int uncovered = (int)(MostUncovered * visible);
            int kind = Math.Min(MostKind, KindEach * counted.Count(c => !ReferenceEquals(c.S, s) && c.S.Look == s.Look));
            int stars = StarsOf(s.Act);
            judged.Add(new JudgedSubject(s.Resident, s.Look, s.Act, stars, box, visible, size, place, facing, uncovered, kind,
                Total(size + place + facing + uncovered + kind, stars)));
        }
        return judged.OrderByDescending(j => j.Points).ThenBy(j => j.Resident).ToList();
    }

    /// <summary>The photograph's subject: the best of those that count; null when none does.</summary>
    public static JudgedSubject? Best(TourFrame frame, TourCourseData? course = null) => Judge(frame, course).FirstOrDefault();
}

/// <summary>A best photograph the report keeps: its course, species, stars and points, and the frame itself, to redraw.</summary>
public sealed record TourBest
{
    public string Course { get; init; } = "";
    public string Species { get; init; } = "";
    public int Stars { get; init; }
    public int Points { get; init; }
    public TourFrame Frame { get; init; } = new();
}

/// <summary>What the save keeps of the report (<c>SaveData.PhotoTour</c>, null while empty).</summary>
public sealed record TourReportSave
{
    public List<TourBest> Best { get; init; } = new();

    /// <summary>Each course's best ride's total.</summary>
    public Dictionary<string, int> Rides { get; init; } = new();
}

/// <summary>
/// The photo tour's report (plan 44 · SD5): the best photograph of each species at each star on each course, each
/// course's best ride, and the total (each species' best on each course, summed), which opens the courses.
/// </summary>
public sealed class TourReport
{
    private readonly List<TourBest> best = new();
    private readonly Dictionary<string, int> rides = new();

    public IReadOnlyList<TourBest> Best => best;
    public IReadOnlyDictionary<string, int> Rides => rides;

    public int Total => best.GroupBy(b => (b.Course, b.Species)).Sum(g => g.Max(b => b.Points));

    public bool IsEmpty => best.Count == 0 && rides.Count == 0;

    /// <summary>Whether a course is open by the report's total.</summary>
    public bool IsOpen(TourCourseData course) => Total >= course.OpensAt;

    /// <summary>A photograph offered: kept where it beats the best of its course, species and stars. True where it was kept.</summary>
    public bool Keep(TourBest photo)
    {
        int i = best.FindIndex(b => b.Course == photo.Course && b.Species == photo.Species && b.Stars == photo.Stars);
        if (i >= 0)
        {
            if (best[i].Points >= photo.Points) return false;
            best[i] = photo;
            return true;
        }
        best.Add(photo);
        return true;
    }

    /// <summary>A ride's total offered as its course's best ride; true where it beat the last.</summary>
    public bool KeepRide(string course, int total)
    {
        if (rides.TryGetValue(course, out int old) && old >= total) return false;
        rides[course] = total;
        return true;
    }

    /// <summary>A ride's photographs judged into the report: every best of the ride offered, and its total.</summary>
    public int Take(TourRide ride)
    {
        var bests = TourResults.BestOf(ride);
        foreach (var b in bests) Keep(b);
        int total = TourResults.TotalOf(bests);
        KeepRide(ride.Course.Id, total);
        return total;
    }

    public void Clear()
    {
        best.Clear();
        rides.Clear();
    }

    public TourReportSave? ToSave() => IsEmpty ? null : new TourReportSave { Best = best.ToList(), Rides = new Dictionary<string, int>(rides) };

    public void Load(TourReportSave? saved)
    {
        Clear();
        if (saved == null) return;
        best.AddRange(saved.Best);
        foreach (var (k, v) in saved.Rides) rides[k] = v;
    }
}

/// <summary>What a ride came to (plan 44 · SD5).</summary>
public static class TourResults
{
    /// <summary>Each species' best photograph of a ride, at each star it was caught at, the best first.</summary>
    public static IReadOnlyList<TourBest> BestOf(TourRide ride)
    {
        var all = new Dictionary<(string, int), TourBest>();
        foreach (var frame in ride.Photos)
            foreach (var j in PhotoJudge.Judge(frame, ride.Course))
            {
                var key = (j.Look, j.Stars);
                if (all.TryGetValue(key, out var had) && had.Points >= j.Points) continue;
                all[key] = new TourBest { Course = ride.Course.Id, Species = j.Look, Stars = j.Stars, Points = j.Points, Frame = frame };
            }
        return all.Values.OrderByDescending(b => b.Points).ThenBy(b => b.Species, StringComparer.Ordinal).ToList();
    }

    /// <summary>A ride's total: each species' best, summed.</summary>
    public static int TotalOf(IEnumerable<TourBest> bests) => bests.GroupBy(b => b.Species).Sum(g => g.Max(b => b.Points));

    /// <summary>Each species' one best photograph of a ride (what the judged panel lists).</summary>
    public static IReadOnlyList<TourBest> ShownOf(IEnumerable<TourBest> bests) =>
        bests.GroupBy(b => b.Species).Select(g => g.OrderByDescending(b => b.Points).First())
            .OrderByDescending(b => b.Points).ThenBy(b => b.Species, StringComparer.Ordinal).ToList();
}

/// <summary>
/// A ride played for a test, the harness and a host with no screen (plan 44 · SD5): it turns toward the resident worth the
/// most stars that is near, zooms it to about half the frame, shoots when the guide would score it well, throws a fruit
/// to one that idles and blows the whistle at a sleeper in view. A pure function of the ride as it stands.
/// </summary>
public static class TourAutopilot
{
    public static TourInput Next(TourRide ride, int ticksSinceShot)
    {
        var frame = ride.FrameNow();
        var cam = frame.Camera;
        float railYaw = ride.RailYaw(ride.Time);

        TourSubject? target = null;
        float bestScore = float.MinValue;
        foreach (var s in frame.Subjects)
        {
            float dx = s.X - cam.Eye.X, dz = s.Z - cam.Eye.Z;
            float dist = MathF.Sqrt(dx * dx + dz * dz);
            if (dist < 2f || dist > 26f) continue;
            float aim = Wrap(railYaw - MathF.Atan2(dx, dz));
            if (MathF.Abs(aim) > TourRide.MaxAim) continue;
            float score = PhotoJudge.StarsOf(s.Act) * 100f - dist * 3f;
            if (score > bestScore)
            {
                bestScore = score;
                target = s;
            }
        }
        if (target == null) return new TourInput(Turn: -Math.Sign(ride.Aim) * Math.Min(1f, MathF.Abs(ride.Aim) / (TourRide.TurnSpeed * TourRide.Tick)));

        float tx = target.X - cam.Eye.X, tz = target.Z - cam.Eye.Z;
        float flat = MathF.Sqrt(tx * tx + tz * tz);
        float wantAim = Wrap(railYaw - MathF.Atan2(tx, tz));
        float wantPitch = MathF.Atan2(target.Y + target.Height / 2f - cam.Eye.Y, flat);
        // The zoom that makes it about half the frame
        float atOne = target.Height * TourCamera.FrameHeight / 2f / (flat * MathF.Tan(TourRide.BaseFov / 2f));
        float wantZoom = Math.Clamp(0.45f * TourCamera.FrameHeight / Math.Max(1f, atOne), TourRide.MinZoom, TourRide.MaxZoom);

        float turn = Math.Clamp((wantAim - ride.Aim) / (TourRide.TurnSpeed * TourRide.Tick), -1f, 1f);
        float tilt = Math.Clamp((wantPitch - ride.Pitch) / (TourRide.TiltSpeed * TourRide.Tick), -1f, 1f);
        float zoom = Math.Clamp((wantZoom - ride.Zoom) / (TourRide.ZoomSpeed * TourRide.Tick), -1f, 1f);

        bool onTarget = MathF.Abs(wantAim - ride.Aim) < 0.03f && MathF.Abs(wantPitch - ride.Pitch) < 0.03f;
        var best = onTarget ? PhotoJudge.Best(frame, ride.Course) : null;
        bool shoot = best != null && ticksSinceShot >= 90 && best.Points >= 350;
        // A fruit every fifteen seconds at most, to one that idles within reach
        bool throwFruit = onTarget && ride.FruitLeft > 0 && target.Act is TourAct.Idle && flat is > TourRide.FruitNearest and < TourRide.FruitFurthest
            && ride.Time >= (TourRide.FruitBag - ride.FruitLeft + 1) * 15f;
        bool whistle = onTarget && target.Act == TourAct.Sleep && ride.WhistleAge is < 0f or > 15f;
        return new TourInput(turn, tilt, zoom, shoot, throwFruit, whistle);
    }

    /// <summary>A whole ride played by the autopilot.</summary>
    public static void Play(TourRide ride)
    {
        int since = 999;
        while (!ride.Over)
        {
            var input = Next(ride, since);
            ride.Step(input);
            since = input.Shoot ? 0 : since + 1;
        }
    }

    private static float Wrap(float a)
    {
        while (a > MathF.PI) a -= MathF.Tau;
        while (a < -MathF.PI) a += MathF.Tau;
        return a;
    }
}
