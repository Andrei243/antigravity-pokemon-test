using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models;

/// <summary>
/// What a resident of a photo tour's course is doing (plan 44 · SD5). The first six are its day; <see cref="Fruit"/>,
/// <see cref="Woke"/>, <see cref="Turn"/> and <see cref="Flee"/> are reactions; <see cref="Rare"/> is its rare action.
/// </summary>
public enum TourAct { Idle, Walk, Sleep, Eat, Play, Show, Fruit, Woke, Turn, Flee, Rare }

/// <summary>The weather a ride is taken in (plan 44 · SD5, the user's answer 64: rare actions hang on weather and time).</summary>
public enum TourWeather { Clear, Rain, Snow }

/// <summary>A step of a resident's day: an action for so many seconds; a walk's end point; a playmate; a move's kind shown.</summary>
public sealed record TourStepData
{
    [DataRequired] public TourAct Act { get; init; }
    public float Seconds { get; init; } = 4f;

    /// <summary>Where a walk ends, x and z.</summary>
    public float[]? To { get; init; }

    /// <summary>The resident (by its place in the course's list) a play is with.</summary>
    public int? With { get; init; }

    /// <summary>The kind of move shown off: <c>Physical</c>, <c>Special</c> or <c>Status</c>.</summary>
    public MoveCategory Move { get; init; } = MoveCategory.Physical;

    /// <summary>Which way it faces while idle, sleeping or eating (degrees; 0 faces +z, toward the rail's start); null keeps the way it faced.</summary>
    public float? Face { get; init; }
}

/// <summary>
/// A resident's rare action (the fourth star): what it does and what brings it on. By the user's answer 64 a rare
/// action hangs on the ride's weather and its time: the hours of the day it happens in, the weather, and the stretch of
/// the rail's clock in which the resident is ready for it.
/// </summary>
public sealed record TourRareData
{
    /// <summary>A short word for what it looks like, the line's key under <c>photoTour.rare.</c> (<c>dance</c>, <c>glow</c>, <c>takeoff</c>).</summary>
    [DataRequired] public string Kind { get; init; } = "";
    public float Seconds { get; init; } = 5f;

    /// <summary>The stretch of the rail's clock (seconds from the start) in which it may begin.</summary>
    public float From { get; init; }
    public float To { get; init; } = 9999f;

    /// <summary>The weather it needs; null for any.</summary>
    public TourWeather? Weather { get; init; }

    /// <summary>The hours it may happen in, from and to (the second not included, and wrapping past midnight); null for any.</summary>
    public int[]? Hours { get; init; }
}

/// <summary>A resident of a course: a species, where it stands as the ride begins, its day and its rare action.</summary>
public sealed record TourResidentData
{
    [DataRequired] public string Species { get; init; } = "";

    /// <summary>Where it stands, x and z (the ground is at nought; a resident on the water floats at nought too).</summary>
    [DataRequired] public float[] At { get; init; } = Array.Empty<float>();

    /// <summary>Which way it faces as the ride begins, in degrees (0 faces +z).</summary>
    public float Face { get; init; }

    /// <summary>A shy one runs from the whistle and comes to no fruit.</summary>
    public bool Shy { get; init; }

    public List<TourStepData> Day { get; init; } = new();
    public TourRareData? Rare { get; init; }
}

/// <summary>Something of the course that hides what stands behind it from the lens (a clump of reeds): its foot, its width and height.</summary>
public sealed record TourScreenData
{
    [DataRequired] public float[] At { get; init; } = Array.Empty<float>();
    public float Width { get; init; } = 2f;
    public float Height { get; init; } = 1.4f;
}

/// <summary>A course of the photo tour (<c>Data/photo-tour.json</c>, ours, written by hand, read strictly).</summary>
public sealed record TourCourseData
{
    [DataRequired] public string Id { get; init; } = "";

    /// <summary>How long the cart takes along the rail, in seconds.</summary>
    public float Seconds { get; init; } = 180f;

    /// <summary>The rail's points, each x, y and z: the lens rides through them at an even pace.</summary>
    [DataRequired] public List<float[]> Rail { get; init; } = new();

    /// <summary>The weathers a ride may be taken in, one drawn by the ride's seed (more of a kind is likelier).</summary>
    public List<TourWeather> Weathers { get; init; } = new() { TourWeather.Clear };

    /// <summary>The report's total that opens the course.</summary>
    public int OpensAt { get; init; }

    /// <summary>The course's look (<see cref="TourLooks"/>: <c>river</c>, <c>wood</c>, <c>reef</c>, <c>cavern</c>, <c>pass</c>, <c>garden</c>).</summary>
    public string Look { get; init; } = "river";

    /// <summary>The song played on the ride; null keeps the place's.</summary>
    public string? Music { get; init; }

    /// <summary>The kinds of place its residents live in (plan 44 · SD6): every resident's Pokédex habitat is one of them.</summary>
    public List<DexHabitat> Habitats { get; init; } = new();

    /// <summary>
    /// A course ridden after dark whatever the clock says (plan 44 · SD6, the Night Garden): a ride begun by day is taken at
    /// <see cref="TourRide.NightHour"/>, one begun at night at its own hour (<see cref="TourRide.HourFor"/>).
    /// </summary>
    public bool Night { get; init; }

    public List<TourResidentData> Residents { get; init; } = new();
    public List<TourScreenData> Screens { get; init; } = new();
}

/// <summary>A reward of the guide's (plan 44 · SD6): an item, handed over once the report's total reaches <see cref="At"/>.</summary>
public sealed record TourRewardData
{
    [DataRequired] public int At { get; init; }
    [DataRequired] public string Item { get; init; } = "";
    public int Count { get; init; } = 1;
}

/// <summary>What <c>Data/photo-tour.json</c> holds.</summary>
public sealed record PhotoTourFile
{
    public List<TourCourseData> Courses { get; init; } = new();

    /// <summary>The guide's rewards, the report's total rising (plan 44 · SD6).</summary>
    public List<TourRewardData> Rewards { get; init; } = new();
}

/// <summary>The courses' looks (plan 44 · SD6), which <c>Graphics/CourseScene</c> builds and the renderer lights.</summary>
public static class TourLooks
{
    public static IReadOnlyList<string> All { get; } = new[] { "river", "wood", "reef", "cavern", "pass", "garden" };
}

/// <summary>The photo tour's courses (plan 44 · SD5, SD6).</summary>
public static class PhotoTourCourses
{
    public const string FileName = "photo-tour.json";

    private static readonly Lazy<PhotoTourFile> file = new(() => GameDataFiles.Load<PhotoTourFile>(FileName, strict: true));

    public static IReadOnlyList<TourCourseData> All => file.Value.Courses;

    public static IReadOnlyList<TourRewardData> Rewards => file.Value.Rewards;

    public static TourCourseData? Get(string? id) => All.FirstOrDefault(c => c.Id == id);
}

/// <summary>How tall a resident stands on a course, in units (plan 44 · SD5): 0.35 + 0.8 × its metres, 0.4 to 2.4.</summary>
public static class TourSizes
{
    public static float HeightOf(string look)
    {
        string name = PokemonLook.Base(look);
        float metres = PokemonDatabase.Get(name)?.Height
            ?? (PokemonDatabase.SpeciesOfForm(name) is { } of ? of.Form(name)?.Height ?? of.Height : 0.5f);
        return HeightFor(metres);
    }

    public static float HeightFor(float metres) => Math.Clamp(0.35f + 0.8f * metres, 0.4f, 2.4f);
}

/// <summary>
/// The lens of a ride (plan 44 · SD5): where it stands, its yaw (0 looks along +z; the way <c>Matrix4x4.CreateRotationY</c>
/// turns +z), its pitch and its vertical field of view, in radians. <see cref="Project"/> is plain arithmetic on a
/// 1920 × 1080 frame, and the course's renderer films through the same numbers.
/// </summary>
public readonly record struct TourCamera(Vector3 Eye, float Yaw, float Pitch, float FovY)
{
    public const float FrameWidth = 1920f, FrameHeight = 1080f;

    public Vector3 Forward => new(MathF.Sin(Yaw) * MathF.Cos(Pitch), MathF.Sin(Pitch), MathF.Cos(Yaw) * MathF.Cos(Pitch));
    public Vector3 Right => Vector3.Normalize(Vector3.Cross(Forward, Vector3.UnitY));
    public Vector3 Up => Vector3.Cross(Right, Forward);

    /// <summary>How many pixels of the frame a unit spans at <paramref name="depth"/> in front of the lens.</summary>
    public float PixelsPerUnit(float depth) => FrameHeight / 2f / (depth * MathF.Tan(FovY / 2f));

    /// <summary>Where a point falls on the frame, and how far in front of the lens it is; false behind it.</summary>
    public bool Project(Vector3 point, out Vector2 onFrame, out float depth)
    {
        var d = point - Eye;
        var f = Forward;
        depth = Vector3.Dot(d, f);
        if (depth <= 0.1f)
        {
            onFrame = default;
            return false;
        }
        float k = PixelsPerUnit(depth);
        onFrame = new Vector2(FrameWidth / 2f + Vector3.Dot(d, Right) * k, FrameHeight / 2f - Vector3.Dot(d, Up) * k);
        return true;
    }
}

/// <summary>A frame's subject: a resident as the shutter caught it (plan 44 · SD5).</summary>
public sealed record TourSubject
{
    public int Resident { get; init; }
    public string Look { get; init; } = "";
    public TourAct Act { get; init; }

    /// <summary>Seconds into the action, for its pose.</summary>
    public float PoseTime { get; init; }
    public float X { get; init; }
    public float Y { get; init; }
    public float Z { get; init; }

    /// <summary>The way it faces, radians (0 faces +z).</summary>
    public float Facing { get; init; }

    /// <summary>How tall it stands (<see cref="TourSizes"/>).</summary>
    public float Height { get; init; } = 1f;

    /// <summary>The kind of move a <see cref="TourAct.Show"/> shows off.</summary>
    public MoveCategory Move { get; init; }

    /// <summary>Where it turns its head while it plays or reacts, x and z; null for straight ahead.</summary>
    public float[]? LooksAt { get; init; }

    public Vector3 Position => new(X, Y, Z);
}

/// <summary>
/// One frame of a ride, as plain values (plan 44 · SD5): the lens, the hour and weather, every resident and every fruit.
/// The shutter keeps it as the photograph; the renderer draws the live ride and a kept photograph from it alike.
/// </summary>
public sealed record TourFrame
{
    public string Course { get; init; } = "";
    public float Time { get; init; }
    public int Hour { get; init; }
    public TourWeather Weather { get; init; }
    public float EyeX { get; init; }
    public float EyeY { get; init; }
    public float EyeZ { get; init; }
    public float Yaw { get; init; }
    public float Pitch { get; init; }
    public float FovY { get; init; }
    public List<TourSubject> Subjects { get; init; } = new();

    /// <summary>Each fruit in the air or on the ground, x, y and z.</summary>
    public List<float[]> Fruits { get; init; } = new();

    public TourCamera Camera => new(new Vector3(EyeX, EyeY, EyeZ), Yaw, Pitch, FovY);
}

/// <summary>What the player does in a tick of a ride: the aim turned and tilted (−1 to 1), the zoom, and the three buttons.</summary>
public readonly record struct TourInput(float Turn = 0f, float Tilt = 0f, float Zoom = 0f, bool Shoot = false, bool Throw = false, bool Whistle = false);

/// <summary>
/// A ride along a course (plan 44 · SD5): thirty ticks a second, a pure function of the course, its seed, the hour, the
/// weather and the inputs it is given, so a seed and the same presses make the same ride. Sixty frames of film, ten
/// fruit and a whistle; the aim within 60° of the rail's way, its pitch within 25° and its zoom 1 to 3.
/// </summary>
public sealed class TourRide
{
    public const int TicksPerSecond = 30;
    public const float Tick = 1f / TicksPerSecond;
    public const int FilmRoll = 60, FruitBag = 10;
    public const float MaxAim = 60f * MathF.PI / 180f, MaxPitch = 25f * MathF.PI / 180f;
    public const float MinZoom = 1f, MaxZoom = 3f;

    /// <summary>The lens's field of view at a zoom of one.</summary>
    public const float BaseFov = 40f * MathF.PI / 180f;

    /// <summary>How fast the aim turns (rad/s), tilts and zooms (×/s) with the stick held over.</summary>
    public const float TurnSpeed = 70f * MathF.PI / 180f, TiltSpeed = 40f * MathF.PI / 180f, ZoomSpeed = 1.6f;

    /// <summary>A fruit's flight, its reach, and how near a resident must be to come to it.</summary>
    public const float FruitFlight = 0.8f, FruitNearest = 4f, FruitFurthest = 16f, FruitPull = 8f, FruitLasts = 10f, EatTime = 3f, ComeSpeed = 1.5f;

    /// <summary>The whistle's reach and how long it rests between blows.</summary>
    public const float WhistleReach = 25f, WhistleRest = 2f, TurnTime = 2f, WokeTime = 2.5f, FleeTime = 2f;

    private readonly TourCourseData course;
    private readonly Resident[] residents;
    private readonly List<Fruit> fruits = new();
    private readonly float[] railLength;
    private readonly float total;
    private readonly List<TourFrame> photos = new();
    private float whistleRest;

    public TourCourseData Course => course;
    public uint Seed { get; }
    public int Hour { get; }
    public TourWeather Weather { get; }
    public int Ticks { get; private set; }
    public float Time => Ticks * Tick;

    /// <summary>The aim off the rail's way (positive to the right), the pitch and the zoom.</summary>
    public float Aim { get; private set; }
    public float Pitch { get; private set; }
    public float Zoom { get; private set; } = MinZoom;

    public int FilmLeft { get; private set; } = FilmRoll;
    public int FruitLeft { get; private set; } = FruitBag;

    /// <summary>Seconds since the last whistle (negative before any), for the rings drawn.</summary>
    public float WhistleAge { get; private set; } = -1f;

    /// <summary>Seconds since the last shot (negative before any), for the flash drawn.</summary>
    public float ShotAge { get; private set; } = -1f;

    /// <summary>The photographs taken, in order.</summary>
    public IReadOnlyList<TourFrame> Photos => photos;

    /// <summary>Over at the rail's end, or with the film gone.</summary>
    public bool Over => Time >= course.Seconds || FilmLeft <= 0;

    /// <summary>How far along the rail, 0 to 1.</summary>
    public float Progress => Math.Clamp(Time / course.Seconds, 0f, 1f);

    /// <summary>The hour a night course is ridden at when it is begun by day.</summary>
    public const int NightHour = 21;

    /// <summary>The hour a ride on a course is taken at: the clock's, or <see cref="NightHour"/> on a night course begun by day.</summary>
    public static int HourFor(TourCourseData course, int hour)
    {
        hour = ((hour % 24) + 24) % 24;
        return course.Night && hour is >= 5 and < 19 ? NightHour : hour;
    }

    /// <summary>The weather a seed draws from a course's list.</summary>
    public static TourWeather WeatherFor(TourCourseData course, uint seed) =>
        course.Weathers.Count == 0 ? TourWeather.Clear : course.Weathers[new SplitMix(seed ^ 0x5EEDu).Int(course.Weathers.Count)];

    public TourRide(TourCourseData course, uint seed, int hour, TourWeather weather)
    {
        this.course = course;
        Seed = seed;
        Hour = HourFor(course, hour);
        Weather = weather;
        railLength = new float[Math.Max(1, course.Rail.Count)];
        for (int i = 1; i < course.Rail.Count; i++) railLength[i] = railLength[i - 1] + Vector3.Distance(Point(i - 1), Point(i));
        total = Math.Max(0.001f, railLength[^1]);

        // Each resident begins somewhere in its day, drawn by the seed
        var rng = new SplitMix(seed);
        residents = new Resident[course.Residents.Count];
        for (int i = 0; i < residents.Length; i++)
        {
            var data = course.Residents[i];
            var r = new Resident(data, StartsOf(data), TourSizes.HeightOf(data.Species));
            if (data.Day.Count > 0)
            {
                r.Step = rng.Int(data.Day.Count);
                r.StepTime = rng.Float() * data.Day[r.Step].Seconds;
            }
            else rng.Next();
            r.From = r.Starts[r.Step];
            r.Pos = r.From;
            r.Facing = data.Face * MathF.PI / 180f;
            residents[i] = r;
        }
    }

    private Vector3 Point(int i) => new(course.Rail[i][0], course.Rail[i][1], course.Rail[i][2]);

    /// <summary>Where the lens is at a moment of the rail's clock.</summary>
    public Vector3 RailAt(float time)
    {
        if (course.Rail.Count == 1) return Point(0);
        float d = Math.Clamp(time / course.Seconds, 0f, 1f) * total;
        for (int i = 1; i < course.Rail.Count; i++)
            if (d <= railLength[i] || i == course.Rail.Count - 1)
            {
                float span = railLength[i] - railLength[i - 1];
                float t = span <= 0f ? 0f : Math.Clamp((d - railLength[i - 1]) / span, 0f, 1f);
                return Vector3.Lerp(Point(i - 1), Point(i), t);
            }
        return Point(course.Rail.Count - 1);
    }

    /// <summary>The rail's way at a moment (a yaw), from a second before to a second after, so a corner is turned smoothly.</summary>
    public float RailYaw(float time)
    {
        var d = RailAt(time + 1.5f) - RailAt(time - 1.5f);
        return d.LengthSquared() < 1e-6f ? MathF.PI : MathF.Atan2(d.X, d.Z);
    }

    /// <summary>The lens now: on the rail, the aim off the rail's way to the right, the pitch, the field of view by the zoom.</summary>
    public TourCamera Camera => new(RailAt(Time), RailYaw(Time) - Aim, Pitch, FovFor(Zoom));

    public static float FovFor(float zoom) => 2f * MathF.Atan(MathF.Tan(BaseFov / 2f) / Math.Clamp(zoom, MinZoom, MaxZoom));

    /// <summary>One tick of the ride with what the player did in it.</summary>
    public void Step(TourInput input)
    {
        if (Over) return;
        Aim = Math.Clamp(Aim + Math.Clamp(input.Turn, -1f, 1f) * TurnSpeed * Tick, -MaxAim, MaxAim);
        Pitch = Math.Clamp(Pitch + Math.Clamp(input.Tilt, -1f, 1f) * TiltSpeed * Tick, -MaxPitch, MaxPitch);
        Zoom = Math.Clamp(Zoom + Math.Clamp(input.Zoom, -1f, 1f) * ZoomSpeed * Tick, MinZoom, MaxZoom);

        if (input.Whistle && whistleRest <= 0f) Whistle();
        if (input.Throw && FruitLeft > 0) Throw();

        Ticks++;
        if (whistleRest > 0f) whistleRest -= Tick;
        if (WhistleAge >= 0f) WhistleAge += Tick;
        if (ShotAge >= 0f) ShotAge += Tick;
        Fruits();
        foreach (var r in residents) Live(r);

        // The shutter last, so the photograph is the tick as it stands
        if (input.Shoot && FilmLeft > 0)
        {
            photos.Add(FrameNow());
            FilmLeft--;
            ShotAge = 0f;
        }
    }

    /// <summary>The ride as it stands, as plain values.</summary>
    public TourFrame FrameNow()
    {
        var cam = Camera;
        return new TourFrame
        {
            Course = course.Id,
            Time = Time,
            Hour = Hour,
            Weather = Weather,
            EyeX = cam.Eye.X,
            EyeY = cam.Eye.Y,
            EyeZ = cam.Eye.Z,
            Yaw = cam.Yaw,
            Pitch = cam.Pitch,
            FovY = cam.FovY,
            Subjects = residents.Select((r, i) => new TourSubject
            {
                Resident = i,
                Look = r.Data.Species,
                Act = ActOf(r),
                PoseTime = PoseTimeOf(r),
                X = r.Pos.X,
                Y = r.Pos.Y,
                Z = r.Pos.Z,
                Facing = r.Facing,
                Height = r.Height,
                Move = ActOf(r) == TourAct.Show ? r.Data.Day[r.Step].Move : MoveCategory.Physical,
                LooksAt = r.LooksAt is { } at ? new[] { at.X, at.Z } : null
            }).ToList(),
            Fruits = fruits.Where(f => !f.Gone).Select(f => { var p = f.Where; return new[] { p.X, p.Y, p.Z }; }).ToList()
        };
    }

    /// <summary>What a resident is doing now (for a test).</summary>
    public TourAct ActOf(int resident) => ActOf(residents[resident]);

    /// <summary>Where a resident stands now (for a test).</summary>
    public Vector3 PositionOf(int resident) => residents[resident].Pos;

    /// <summary>Whether a resident's rare action has played this ride.</summary>
    public bool RareSeen(int resident) => residents[resident].RareDone;

    // ------------------------------------------------------------------ residents

    private sealed class Resident
    {
        public readonly TourResidentData Data;
        public readonly Vector3[] Starts;
        public readonly float Height;
        public int Step;
        public float StepTime;
        public Vector3 From, Pos;
        public float Facing;
        public TourAct? Reaction;
        public float ReactTime, ReactLength, ApproachLength;
        public Vector3 ReactFrom, ReactTo;
        public Fruit? Eating;
        public bool InRare, RareDone;
        public float RareTime;
        public Vector3? LooksAt;

        public Resident(TourResidentData data, Vector3[] starts, float height)
        {
            Data = data;
            Starts = starts;
            Height = height;
        }
    }

    /// <summary>Where each step of a day begins: the last walk's end before it, round the loop, or where the resident stands.</summary>
    private static Vector3[] StartsOf(TourResidentData data)
    {
        var at = new Vector3(data.At[0], 0f, data.At.Length > 1 ? data.At[1] : 0f);
        int n = data.Day.Count;
        var starts = new Vector3[Math.Max(1, n)];
        if (n == 0)
        {
            starts[0] = at;
            return starts;
        }
        // The position at the start of the loop: the last walk's end (a loop comes back to where it began)
        var pos = at;
        for (int k = n - 1; k >= 0; k--)
            if (data.Day[k].Act == TourAct.Walk && data.Day[k].To is { Length: >= 2 } to)
            {
                pos = new Vector3(to[0], 0f, to[1]);
                break;
            }
        for (int k = 0; k < n; k++)
        {
            starts[k] = pos;
            if (data.Day[k].Act == TourAct.Walk && data.Day[k].To is { Length: >= 2 } to) pos = new Vector3(to[0], 0f, to[1]);
        }
        return starts;
    }

    private static TourAct ActOf(Resident r) =>
        r.InRare ? TourAct.Rare : r.Reaction is { } react ? react : r.Data.Day.Count == 0 ? TourAct.Idle : r.Data.Day[r.Step].Act;

    private static float PoseTimeOf(Resident r) => r.InRare ? r.RareTime : r.Reaction != null ? r.ReactTime : r.StepTime;

    private bool HourFits(int[]? hours)
    {
        if (hours is not { Length: >= 2 }) return true;
        int a = hours[0], b = hours[1];
        return a <= b ? Hour >= a && Hour < b : Hour >= a || Hour < b;
    }

    private void Live(Resident r)
    {
        var eye = RailAt(Time);
        // The rare action, once a ride, when its time, its hour and its weather have come
        if (!r.InRare && !r.RareDone && r.Reaction == null && r.Data.Rare is { } rare
            && Time >= rare.From && Time < rare.To && (rare.Weather == null || rare.Weather == Weather) && HourFits(rare.Hours))
        {
            r.InRare = true;
            r.RareDone = true;
            r.RareTime = 0f;
        }
        if (r.InRare)
        {
            r.RareTime += Tick;
            r.Facing = FaceToward(r.Pos, eye);
            if (r.RareTime >= r.Data.Rare!.Seconds) r.InRare = false;
            return;
        }
        if (r.Reaction is { } reaction)
        {
            r.ReactTime += Tick;
            switch (reaction)
            {
                case TourAct.Fruit:
                    if (r.ReactTime < r.ApproachLength)
                    {
                        r.Pos = Vector3.Lerp(r.ReactFrom, r.ReactTo, r.ReactTime / r.ApproachLength);
                        r.Facing = FaceToward(r.ReactFrom, r.ReactTo);
                    }
                    else r.Pos = r.ReactTo;
                    break;
                case TourAct.Flee:
                    r.Pos = Vector3.Lerp(r.ReactFrom, r.ReactTo, Math.Min(1f, r.ReactTime / r.ReactLength));
                    r.Facing = FaceToward(r.ReactFrom, r.ReactTo);
                    break;
                default:
                    r.Facing = FaceToward(r.Pos, eye);
                    break;
            }
            if (r.ReactTime >= r.ReactLength)
            {
                if (r.Eating != null) r.Eating.Eaten = true;
                r.Eating = null;
                r.Reaction = null;
                // The day goes on from where the reaction left it
                r.From = r.Pos;
                r.StepTime = 0f;
            }
            return;
        }
        if (r.Data.Day.Count == 0) return;
        var step = r.Data.Day[r.Step];
        r.StepTime += Tick;
        r.LooksAt = null;
        switch (step.Act)
        {
            case TourAct.Walk when step.To is { Length: >= 2 } to:
            {
                var end = new Vector3(to[0], 0f, to[1]);
                r.Pos = Vector3.Lerp(r.From, end, Math.Min(1f, r.StepTime / Math.Max(0.1f, step.Seconds)));
                if (Vector3.DistanceSquared(r.From, end) > 1e-4f) r.Facing = FaceToward(r.From, end);
                break;
            }
            case TourAct.Play when step.With is int with && with >= 0 && with < residents.Length && with != Array.IndexOf(residents, r):
                r.Facing = FaceToward(r.Pos, residents[with].Pos);
                r.LooksAt = residents[with].Pos;
                break;
            default:
                if (step.Face is float face) r.Facing = face * MathF.PI / 180f;
                break;
        }
        if (r.StepTime >= step.Seconds)
        {
            r.Step = (r.Step + 1) % r.Data.Day.Count;
            r.StepTime = 0f;
            r.From = r.Pos;
        }
    }

    private static float FaceToward(Vector3 from, Vector3 to) => MathF.Atan2(to.X - from.X, to.Z - from.Z);

    private static float Flat(Vector3 a, Vector3 b) => MathF.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Z - b.Z) * (a.Z - b.Z));

    // ------------------------------------------------------------------ the fruit and the whistle

    private sealed class Fruit
    {
        public Vector3 From, To;
        public float Age;
        public bool Landed, Eaten, Gone, Claimed;

        /// <summary>Where it is: on its arc while it flies (rising 1.5 at the middle), on the ground once landed.</summary>
        public Vector3 Where
        {
            get
            {
                if (Landed) return To;
                float t = Math.Clamp(Age / FruitFlight, 0f, 1f);
                return Vector3.Lerp(From, To, t) + new Vector3(0, 1.5f * 4f * t * (1f - t), 0);
            }
        }
    }

    /// <summary>Where a fruit thrown now lands: where the aim meets the ground, 4 to 16 from the rail, else 12 straight ahead.</summary>
    public Vector3 FruitLanding()
    {
        var cam = Camera;
        var f = cam.Forward;
        var flat = Vector3.Normalize(new Vector3(f.X, 0, f.Z));
        float reach = 12f;
        if (f.Y < -0.02f)
        {
            float t = cam.Eye.Y / -f.Y;
            reach = Math.Clamp(t * MathF.Sqrt(f.X * f.X + f.Z * f.Z), FruitNearest, FruitFurthest);
        }
        var at = cam.Eye + flat * reach;
        return new Vector3(at.X, 0f, at.Z);
    }

    private void Throw()
    {
        FruitLeft--;
        fruits.Add(new Fruit { From = Camera.Eye, To = FruitLanding() });
    }

    private void Fruits()
    {
        foreach (var fruit in fruits)
        {
            if (fruit.Gone) continue;
            fruit.Age += Tick;
            if (!fruit.Landed && fruit.Age >= FruitFlight)
            {
                fruit.Landed = true;
                Land(fruit);
            }
            if (fruit.Eaten || fruit.Age >= FruitFlight + FruitLasts && !fruit.Claimed) fruit.Gone = true;
        }
    }

    /// <summary>A fruit landed: the nearest resident within reach that is awake, free and not shy comes and eats it.</summary>
    private void Land(Fruit fruit)
    {
        Resident? nearest = null;
        float best = FruitPull;
        foreach (var r in residents)
        {
            if (r.Data.Shy || r.InRare || r.Reaction != null || ActOf(r) == TourAct.Sleep) continue;
            float d = Flat(r.Pos, fruit.To);
            if (d < best)
            {
                best = d;
                nearest = r;
            }
        }
        if (nearest == null) return;
        var to = fruit.To;
        var toward = new Vector3(nearest.Pos.X - to.X, 0, nearest.Pos.Z - to.Z);
        // It stops just short of the fruit, on its own side
        var stop = toward.LengthSquared() > 1e-4f ? to + Vector3.Normalize(toward) * 0.4f : to;
        nearest.Reaction = TourAct.Fruit;
        nearest.ReactTime = 0f;
        nearest.ReactFrom = nearest.Pos;
        nearest.ReactTo = stop;
        nearest.ApproachLength = Math.Max(0.2f, Flat(nearest.Pos, stop) / ComeSpeed);
        nearest.ReactLength = nearest.ApproachLength + EatTime;
        nearest.Eating = fruit;
        fruit.Claimed = true;
    }

    /// <summary>
    /// The whistle: within its reach a sleeper wakes, a shy one runs four units off from the lens, and the rest turn to
    /// look at it; none in its rare action heeds it.
    /// </summary>
    private void Whistle()
    {
        whistleRest = WhistleRest;
        WhistleAge = 0f;
        var eye = RailAt(Time);
        foreach (var r in residents)
        {
            if (r.InRare || Flat(r.Pos, eye) > WhistleReach) continue;
            if (r.Reaction == null && ActOf(r) == TourAct.Sleep)
            {
                r.Reaction = TourAct.Woke;
                r.ReactLength = WokeTime;
                // and its day moves on past the sleep
                r.Step = (r.Step + 1) % r.Data.Day.Count;
            }
            else if (r.Data.Shy && r.Reaction is null or TourAct.Turn)
            {
                var away = new Vector3(r.Pos.X - eye.X, 0, r.Pos.Z - eye.Z);
                away = away.LengthSquared() > 1e-4f ? Vector3.Normalize(away) : Vector3.UnitZ;
                r.Reaction = TourAct.Flee;
                r.ReactLength = FleeTime;
                r.ReactFrom = r.Pos;
                r.ReactTo = r.Pos + away * 4f;
            }
            else if (r.Reaction == null)
            {
                r.Reaction = TourAct.Turn;
                r.ReactLength = TurnTime;
            }
            else continue;
            r.ReactTime = 0f;
        }
    }
}
