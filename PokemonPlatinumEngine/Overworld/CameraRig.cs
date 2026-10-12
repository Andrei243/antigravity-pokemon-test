using System;
using System.Collections.Generic;
using System.Numerics;

namespace PokemonPlatinumEngine.Overworld;

/// <summary>
/// The field camera's own moves (plan 09 · L4; style guide, "Story scenes", "The camera kit"): where it looks
/// instead of at the player (a spot it was sent to, someone else's feet, a point travelling a path), its lens, the
/// cinema bars and the pace of the field's time, as one state that is a function of the time handed to
/// <see cref="Advance"/>. It knows nothing of drawing: <c>WorldRenderer</c> moves it on by the camera's clock each
/// frame and asks where to look and through what lens, and <c>GameEngine</c> reads the bars and the pace. A story's
/// script drives it (<c>camera …</c>, <c>letterbox</c>, <c>slowmo</c>), and so can anything else that holds the
/// field: a Gym's puzzle, a way into a battle.
/// <para>
/// Every move eases on one curve (3t² − 2t³). A zoom changes the lens's field of view and never the pitch or the
/// distance, so the straightening of upright things, which is worked out for one pitch, holds at every zoom.
/// </para>
/// </summary>
public sealed class CameraRig
{
    /// <summary>How tall each cinema bar is once it is in, in layout units: the title's own.</summary>
    public const float BarHeight = 120f;

    /// <summary>How long a move takes unless it is told otherwise.</summary>
    public const float PanSeconds = 0.8f, ReleaseSeconds = 0.6f, FollowSeconds = 0.8f, ZoomSeconds = 0.8f, BarSeconds = 0.4f;

    /// <summary>The widest and the tightest lens: half as large (twice the ground) to four times as large.</summary>
    public const float WidestZoom = 0.5f, TightestZoom = 4f;

    /// <summary>A path's pace in tiles a second unless it is given one, the slowest and the fastest it may be given, and how long it takes to gather and to lose it.</summary>
    public const float PathPace = 4f, SlowestPath = 0.25f, FastestPath = 16f, PathEase = 0.5f;

    /// <summary>The most tiles a path goes through.</summary>
    public const int MostPathTiles = 8;

    /// <summary>How long slow motion lasts unless it is told, the longest it may, how long it takes to come on and to go, and the slowest the field may run.</summary>
    public const float SlowSeconds = 2f, LongestSlow = 60f, SlowEase = 0.2f, SlowestPace = 0.1f;

    private static float Ease(float t) => t * t * (3f - 2f * t);

    // ------------------------------------------------------------------ where it looks

    private enum Aim { Spot, Person, Path }

    // Whether the camera has been sent to look somewhere else than at the player, and at what
    private bool away;
    private Aim aim;
    private Vector2 spot;
    private NPC? person;

    // How far over from the player to where it was sent (0 to 1), and how fast that changes: back again when negative
    private float amount, rate;

    // Sent on from a place it had been sent to already: where it looked then, and how far it has come from there
    private Vector2 shiftFrom;
    private float shift = 1f, shiftRate;

    // A path: its corners, how far along each is, its pace, and how long it has been travelled
    private Vector2[] route = Array.Empty<Vector2>();
    private float[] routeAt = Array.Empty<float>();
    private float routePace = PathPace, routeTime, routeSeconds;

    // Where the camera last looked: a move begun from somewhere else than the player begins there
    private Vector2 looked;
    private bool hasLooked;

    /// <summary>True while the camera looks somewhere else than at the player, or is on its way back.</summary>
    public bool Away => away;

    /// <summary>How far over to where it was sent the camera is, 0 to 1, eased at both ends.</summary>
    public float PanEase => amount * amount * (3f - 2f * amount);

    /// <summary>
    /// Sends the camera to look at a spot (in tiles) over <paramref name="seconds"/>; it stays until it is sent
    /// elsewhere or released. From the player it eases out of where it is and into where it goes; from a spot it
    /// was sent to already it eases on from there.
    /// </summary>
    public void Pan(float x, float z, float seconds)
    {
        Send(seconds);
        aim = Aim.Spot;
        spot = new Vector2(x, z);
    }

    /// <summary>Keeps the camera on someone else's feet, getting onto them over <paramref name="seconds"/>.</summary>
    public void Follow(NPC who, float seconds)
    {
        Send(seconds);
        aim = Aim.Person;
        person = who;
    }

    /// <summary>
    /// Brings the camera back onto the player over <paramref name="seconds"/> and leaves the lens, the bars and
    /// the pace as they are: <c>camera follow player</c>.
    /// </summary>
    public void FollowPlayer(float seconds)
    {
        if (away) rate = -1f / MathF.Max(0.01f, seconds);
    }

    /// <summary>Whoever the camera keeps on, while it does; nobody when it looks at the player, a spot or a path.</summary>
    public NPC? Following => away && aim == Aim.Person ? person : null;

    /// <summary>
    /// The camera is on the player again at once, with the lens, the bars and the pace left as they are: whoever it
    /// kept on is on another map now (a scene warped away from them).
    /// </summary>
    public void ComeHome()
    {
        Home();
        amount = rate = 0f;
    }

    /// <summary>
    /// Sends the camera along a path through spots (in tiles) from where it looks now, at a pace in tiles a second
    /// that it gathers over <see cref="PathEase"/> and loses over the same at the end; it stays at the last.
    /// Returns how long the path takes.
    /// </summary>
    public float Path(IReadOnlyList<Vector2> through, float tilesPerSecond = PathPace)
    {
        if (through.Count == 0) return 0f;
        var corners = new List<Vector2>();
        // (Before the camera has ever looked anywhere there is nowhere to come from: the path begins at its first spot)
        if (hasLooked) corners.Add(looked);
        corners.AddRange(through);
        route = corners.ToArray();
        routeAt = new float[route.Length];
        for (int i = 1; i < route.Length; i++) routeAt[i] = routeAt[i - 1] + Vector2.Distance(route[i - 1], route[i]);
        routePace = Math.Clamp(tilesPerSecond, SlowestPath, FastestPath);
        routeTime = 0f;
        routeSeconds = PathSeconds(routeAt[^1], routePace);

        // The path's own first leg is how the camera gets there: it is over to it at once, and nothing is eased on from
        away = true;
        aim = Aim.Path;
        amount = 1f;
        rate = 0f;
        shift = 1f;
        return routeSeconds;
    }

    private void Send(float seconds)
    {
        float speed = 1f / MathF.Max(0.01f, seconds);
        if (away && amount > 0f && hasLooked)
        {
            // Already looking somewhere else (or on the way there or back): on from where it looks
            shiftFrom = looked;
            shift = 0f;
            shiftRate = speed;
            amount = 1f;
        }
        else shift = 1f;
        away = true;
        rate = speed;
    }

    /// <summary>How long a path of a length takes at a pace: the length at that pace, and the time its pace takes to gather.</summary>
    public static float PathSeconds(float length, float pace) => length <= 0f ? 0f : length / pace + Ramp(length, pace);

    // A path too short to gather its pace and lose it again in half a second each does both in what time it has
    private static float Ramp(float length, float pace) => MathF.Min(PathEase, length / pace);

    /// <summary>
    /// How far along a path of a length the camera is after a time, at a pace: gathering it on the easing curve,
    /// holding it, and losing it the same way, so it leaves its first spot and comes to its last at no speed.
    /// </summary>
    public static float Along(float seconds, float length, float pace)
    {
        if (length <= 0f || seconds <= 0f) return 0f;
        float ramp = Ramp(length, pace), whole = length / pace + ramp;
        if (seconds >= whole) return length;
        // The distance covered while the pace is gathered: the integral of the easing curve, u³ − u⁴ ∕ 2
        static float Gathered(float u) => u * u * u - u * u * u * u / 2f;
        if (seconds <= ramp) return pace * ramp * Gathered(seconds / ramp);
        if (seconds >= whole - ramp) return length - pace * ramp * Gathered((whole - seconds) / ramp);
        return pace * ramp / 2f + pace * (seconds - ramp);
    }

    private Vector2 RoutePoint()
    {
        if (route.Length == 0) return spot;
        float along = Along(routeTime, routeAt[^1], routePace);
        for (int i = 1; i < route.Length; i++)
        {
            if (along > routeAt[i]) continue;
            float leg = routeAt[i] - routeAt[i - 1];
            return leg <= 0f ? route[i] : Vector2.Lerp(route[i - 1], route[i], (along - routeAt[i - 1]) / leg);
        }
        return route[^1];
    }

    /// <summary>Where the camera has been sent, this moment.</summary>
    private Vector2 Sent()
    {
        var at = aim switch
        {
            Aim.Person when person != null => new Vector2(person.DrawX + 0.5f, person.DrawY + 0.5f),
            Aim.Path => RoutePoint(),
            _ => spot
        };
        return shift >= 1f ? at : shiftFrom + (at - shiftFrom) * Ease(shift);
    }

    /// <summary>
    /// The point of the ground (in tiles) the camera looks at, given where the player's feet are: the player,
    /// unless it has been sent elsewhere.
    /// </summary>
    public Vector2 Look(Vector2 player)
    {
        var look = player;
        if (away)
        {
            var sent = Sent();
            look.X += (sent.X - player.X) * PanEase;
            look.Y += (sent.Y - player.Y) * PanEase;
        }
        looked = look;
        hasLooked = true;
        return look;
    }

    // ------------------------------------------------------------------ the lens

    private float zoomFrom = 1f, zoomTo = 1f, zoomAmount = 1f, zoomRate;

    /// <summary>
    /// The lens's factor: 1 is the place's own, 2 shows everything twice as large, 0.5 twice as much ground. On its
    /// way from one factor to another it is eased on their ratio, so the picture grows evenly.
    /// </summary>
    public float Zoom => zoomAmount >= 1f ? zoomTo : zoomFrom * MathF.Pow(zoomTo / zoomFrom, Ease(zoomAmount));

    /// <summary>Takes the lens to a factor (<see cref="WidestZoom"/> to <see cref="TightestZoom"/>) over <paramref name="seconds"/>.</summary>
    public void ZoomTo(float factor, float seconds)
    {
        zoomFrom = Zoom;
        zoomTo = Math.Clamp(factor, WidestZoom, TightestZoom);
        zoomAmount = seconds <= 0f ? 1f : 0f;
        zoomRate = 1f / MathF.Max(0.01f, seconds);
    }

    /// <summary>
    /// A lens's field of view (in degrees) under the zoom: the tangent of its half is divided by the factor, which
    /// is what makes everything that much larger. The pitch and the distance are never touched.
    /// </summary>
    public float Fov(float degrees) => FovAt(degrees, Zoom);

    /// <summary>A lens's field of view (in degrees) at a zoom's factor; the lens itself at a factor of 1.</summary>
    public static float FovAt(float degrees, float zoom)
    {
        if (zoom == 1f) return degrees;
        return 2f * MathF.Atan(MathF.Tan(degrees * MathF.PI / 360f) / zoom) * 180f / MathF.PI;
    }

    /// <summary>
    /// The widest the lens is or is on its way to: what it will show before long has to be made ready before it
    /// is in view (the chunks of a streamed map).
    /// </summary>
    public float WidestAhead => MathF.Min(Zoom, zoomTo);

    // ------------------------------------------------------------------ the bars

    private float bars, barRate;

    /// <summary>How tall each cinema bar is now, in whole layout units: 0 with none, <see cref="BarHeight"/> once they are in.</summary>
    public float Bars => MathF.Round(BarHeight * Ease(bars));

    /// <summary>Slides the cinema bars in or away over <paramref name="seconds"/>.</summary>
    public void Letterbox(bool on, float seconds)
    {
        barRate = (on ? 1f : -1f) / MathF.Max(0.01f, seconds);
        if (seconds <= 0f) bars = on ? 1f : 0f;
    }

    // ------------------------------------------------------------------ the field's pace

    // The share of its pace the field is slowed to, how long that still has, and how far it has come on (0 to 1)
    private float slowTo = 1f, slowLeft, slowLevel;

    /// <summary>
    /// How fast the field's time runs: 1 unless slow motion is on, which eases to its share of the pace over
    /// <see cref="SlowEase"/>, holds it, and has eased back by the time its time is up.
    /// </summary>
    public float Pace => slowLevel <= 0f ? 1f : 1f + (slowTo - 1f) * Ease(slowLevel);

    /// <summary>
    /// Runs the field at a share of its pace (<see cref="SlowestPace"/> to 1) for <paramref name="seconds"/> of
    /// the clock's time, after which it is back at its pace by itself; a share of 1 ends slow motion now.
    /// </summary>
    public void SlowMotion(float share, float seconds)
    {
        if (share >= 1f || seconds <= 0f)
        {
            EndSlowMotion();
            return;
        }
        slowTo = MathF.Max(SlowestPace, share);
        slowLeft = MathF.Min(seconds, LongestSlow);
    }

    // It eases back from wherever it is: only as much time is left as that takes
    private void EndSlowMotion() => slowLeft = MathF.Min(slowLeft, slowLevel * SlowEase);

    // ------------------------------------------------------------------ all of it

    /// <summary>
    /// Brings everything back over <paramref name="seconds"/>: the camera to the player, the lens to the place's
    /// own, the bars away, the field to its pace.
    /// </summary>
    public void Release(float seconds)
    {
        rate = -1f / MathF.Max(0.01f, seconds);
        if (Zoom != 1f) ZoomTo(1f, seconds);
        if (bars > 0f) Letterbox(false, seconds);
        EndSlowMotion();
    }

    /// <summary>Everything back at once: a new game, a loaded one, a white-out.</summary>
    public void Reset()
    {
        Home();
        amount = rate = 0f;
        zoomFrom = zoomTo = zoomAmount = 1f;
        bars = barRate = 0f;
        slowLeft = slowLevel = 0f;
        hasLooked = false;
    }

    private void Home()
    {
        away = false;
        aim = Aim.Spot;
        person = null;
        shift = 1f;
    }

    /// <summary>True while nothing of the rig is in force: the camera on the player, the place's own lens, no bars, time at its pace.</summary>
    public bool AtRest => !away && Zoom == 1f && bars <= 0f && slowLevel <= 0f && slowLeft <= 0f;

    /// <summary>True while something of the rig is still on its way: a pan, a path, a zoom, the bars.</summary>
    public bool Moving => (away && ((rate > 0f && amount < 1f) || rate < 0f || shift < 1f || (aim == Aim.Path && routeTime < routeSeconds)))
        || zoomAmount < 1f || (barRate > 0f && bars < 1f) || (barRate < 0f && bars > 0f);

    /// <summary>
    /// Everything the rig is on its way to, it has reached (plan 12 · Q20: a scene skipped): the camera is where it
    /// was sent, or home again; a path is at its last spot; the lens is at its factor; the bars are in or away; and
    /// slow motion, which passes by itself, is over.
    /// </summary>
    public void Settle() => Advance(LongestSlow + 1f);

    /// <summary>Moves everything on by a time of the camera's clock.</summary>
    public void Advance(float dt)
    {
        amount = Math.Clamp(amount + rate * dt, 0f, 1f);
        if (amount <= 0f && rate < 0f) Home();
        if (shift < 1f) shift = MathF.Min(1f, shift + shiftRate * dt);
        if (away && aim == Aim.Path) routeTime = MathF.Min(routeSeconds, routeTime + dt);
        if (zoomAmount < 1f) zoomAmount = MathF.Min(1f, zoomAmount + zoomRate * dt);
        bars = Math.Clamp(bars + barRate * dt, 0f, 1f);
        if (slowLeft > 0f || slowLevel > 0f)
        {
            // It comes on at a steady rate and is never further on than the time left lets it get back from, so it
            // is gone exactly as its time is up
            slowLeft = MathF.Max(0f, slowLeft - dt);
            slowLevel = MathF.Min(MathF.Min(1f, slowLevel + dt / SlowEase), slowLeft / SlowEase);
        }
    }
}
