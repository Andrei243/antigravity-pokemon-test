using System;
using System.Collections.Generic;

namespace PokemonPlatinumEngine.OpenWorld;

/// <summary>
/// What a region lets the player do beyond walking in an open map (plan 32, "Movement"): asked of the story by
/// whichever region first grants one (the Rotom Bike's water, the mount's swim, sprint, jump, glide and climb). OW3
/// builds the one it needs, swimming; new grants go at the end.
/// </summary>
[Flags]
public enum MovementGrants
{
    None = 0,

    /// <summary>The capsule goes into water deeper than <see cref="OpenBody.SwimDepth"/> and swims; without it, it stops at the edge.</summary>
    Swim = 1
}

/// <summary>
/// A tick's steering (plan 32 · OW3): the stick's tilt east and south, each −1 to 1 (as <c>InputManager.GetStick</c>
/// gives it, its x east and its y down the screen, which is south), and whether the body runs.
/// </summary>
public readonly record struct OpenInput(double StickX, double StickZ, bool Run = false)
{
    public static readonly OpenInput None = default;
}

/// <summary>What the capsule is doing (plan 32 · OW3).</summary>
public enum OpenStance
{
    /// <summary>On the ground (walkable or steep: a steep face is stood on and slid down, decision 14).</summary>
    Standing,
    Falling,
    Swimming
}

/// <summary>
/// The player's capsule in an open map (plan 32, "Movement"; OW3, built on OW0's prototype): a tick at a time at 120
/// a second, steered by the stick's tilt, walking at 1.5 m/s and running at 4.5 m/s in proportion to the tilt. Its
/// body above the step is three spheres of <see cref="Radius"/> along its axis, each pushed out of whatever is not
/// walkable ground (the collide-and-slide loop, four pushes at most a tick); its footing is a line straight down
/// from the step's height. It steps up <see cref="StepUp"/> without a jump, is held to the ground going down by up to
/// <see cref="SnapDown"/> a tick, and falls off anything more, under gravity, landing where a line down the fall
/// meets ground (a <see cref="HardLanding"/> past a metre). It stands on any face turned up and never on one turned
/// down (decision 14): on a face steeper than 40° it slides down at <see cref="SlideSpeed"/> and the stick can't take
/// it up, so a body pushed over the foot of a steep wall stands on it and slides off it, never through it. Water
/// deeper than <see cref="SwimDepth"/> is swum with <see cref="MovementGrants.Swim"/>; without it a step into it is
/// refused, so the body stops at the edge (a body that is in it, put there or fallen in, swims out).
/// <para>
/// <b>Determinism</b> (decisions 2 and 11): the body's place is whole micrometres in 64-bit integers, its fall's
/// speed whole micrometres a tick; every tick is worked in double from them and rounded back once, so nothing builds
/// up and the same ticks give the same micrometre anywhere. Only the four operations and the square root are used
/// (the yaw's arctangent too, <see cref="OpenTriangles.YawOf"/>). <see cref="Pos"/> tells the place in millimetres,
/// as everything kept and named is.
/// </para>
/// GPU-free; no clock but the ticks it is stepped by.
/// </summary>
public sealed class OpenBody
{
    public const int TicksPerSecond = OpenClock.TicksPerSecond;

    /// <summary>The body: plan 30 · FM6's grown scale, 1.6 m tall and 0.35 m round.</summary>
    public const double Radius = 0.35, Height = 1.6;

    /// <summary>What is stepped up without a jump, and what the feet are held down by going down, in metres.</summary>
    public const double StepUp = 0.35, SnapDown = 0.5;

    /// <summary>Speeds over the ground at a full tilt, metres a second (numbers of ours, not the Ruleset's).</summary>
    public const double WalkSpeed = 1.5, RunSpeed = 4.5, SwimSpeed = 1.5;

    /// <summary>How fast a body standing on a face steeper than 40° slides down it, metres a second over the ground (decision 14).</summary>
    public const double SlideSpeed = 2.0;

    /// <summary>Gravity, and the fastest a fall goes, metres a second.</summary>
    public const double Gravity = 9.81, FastestFall = 50;

    /// <summary>A fall longer than this, in metres, ends in a landing (plan 32: "falls off a drop of more than 1 m with a landing").</summary>
    public const double LandingDrop = 1.0;

    /// <summary>Water deeper than this over the ground is swum, the feet held this far under its surface; shallower is waded.</summary>
    public const double SwimDepth = 1.1;

    /// <summary>A tilt under this is the stick at rest.</summary>
    public const double DeadZone = 0.02;

    /// <summary>A body still in a wall by more than this after its pushes, beyond what it was in before, doesn't move that tick, in metres.</summary>
    public const double Tolerance = 0.001;

    private const int MostPushes = 4;

    /// <summary>The heights of the three spheres' centres over the feet.</summary>
    private static readonly double[] Spheres = { StepUp + Radius, (StepUp + Height) / 2, Height - Radius };

    private long mx, my, mz;
    private long fallSpeed;     // micrometres a tick, downward negative
    private long fallFrom;      // micrometres: where a fall began
    private bool settled;
    private double residual = double.MaxValue;
    private double steepX, steepZ;   // the way down the face stood on, when it is steeper than 40°
    private readonly List<int> near = new();

    public OpenBody(OpenPos feet, int yaw = 0)
    {
        mx = feet.X * 1000L; my = feet.Y * 1000L; mz = feet.Z * 1000L;
        Yaw = OpenYaw.Normalize(yaw);
    }

    /// <summary>Where the feet are, in micrometres: the body's own state.</summary>
    public (long X, long Y, long Z) Micrometres => (mx, my, mz);

    /// <summary>Where the feet are, to the millimetre (half away from nought).</summary>
    public OpenPos Pos => new(ToMm(mx), ToMm(my), ToMm(mz));

    /// <summary>The way the body faces, in 65,536ths of a turn (<see cref="OpenYaw"/>): the stick's way as it last moved.</summary>
    public int Yaw { get; private set; }

    public OpenStance Stance { get; private set; } = OpenStance.Standing;
    public bool Grounded => Stance == OpenStance.Standing;

    /// <summary>The ticks stepped.</summary>
    public long Tick { get; private set; }

    /// <summary>The fall's speed, micrometres a tick, downward negative (nought unless falling).</summary>
    public long FallMicrometres => fallSpeed;

    // What happened in the last tick
    /// <summary>Pushes off walls in the last tick (four at most).</summary>
    public int Pushes { get; private set; }

    /// <summary>Whether the body stood on a face steeper than 40° in the last tick and slid down it.</summary>
    public bool Sliding { get; private set; }

    /// <summary>Whether the last tick's step was undone: a wall it couldn't be pushed out of, or deep water without the grant.</summary>
    public bool Refused { get; private set; }

    /// <summary>Whether the last tick's step was refused for deep water (the body stopped at the edge).</summary>
    public bool StoppedAtWater { get; private set; }

    /// <summary>The height fallen, in millimetres, the tick a fall or a splash ended in; null otherwise.</summary>
    public int? Landed { get; private set; }

    /// <summary>Whether the last tick's fall was longer than <see cref="LandingDrop"/> and ended on ground.</summary>
    public bool HardLanding { get; private set; }

    /// <summary>Whether the last tick's fall ended in the water.</summary>
    public bool Splashed { get; private set; }

    private static int ToMm(long micrometres) => (int)(micrometres >= 0 ? (micrometres + 500) / 1000 : (micrometres - 500) / 1000);
    private static long ToMicro(double metres) => (long)Math.Round(metres * 1e6, MidpointRounding.AwayFromZero);

    /// <summary>One tick on <paramref name="ground"/>, steered by <paramref name="input"/>, with what the region grants.</summary>
    public void Step(OpenGround ground, OpenInput input, MovementGrants grants = MovementGrants.None)
    {
        Tick++;
        Pushes = 0;
        Sliding = Refused = StoppedAtWater = HardLanding = Splashed = false;
        Landed = null;

        double x = mx / 1e6, y = my / 1e6, z = mz / 1e6;

        // The way the stick asks, and the face's own slide
        double sx = input.StickX, sz = input.StickZ;
        double length = Math.Sqrt(sx * sx + sz * sz);
        double dx = 0, dz = 0;
        if (length > DeadZone && !double.IsNaN(length))
        {
            double tilt = Math.Min(1, length);
            double speed = (Stance == OpenStance.Swimming ? SwimSpeed : input.Run ? RunSpeed : WalkSpeed) * tilt / TicksPerSecond;
            dx = sx / length * speed;
            dz = sz / length * speed;
            Yaw = OpenTriangles.YawOf(sx, sz);
        }
        // A map with nothing in it to stand on is a place to stand (OW1's empty maps): the body only turns
        if (!ground.HasAnything) return;
        if (!settled) Settle(ground, ref y, x, z);

        if (Stance == OpenStance.Standing && (steepX != 0 || steepZ != 0))
        {
            // On a face steeper than 40° the stick can't take the body up it, and the face slides it down
            double up = -(dx * steepX + dz * steepZ);
            if (up > 0) { dx += up * steepX; dz += up * steepZ; }
            dx += steepX * SlideSpeed / TicksPerSecond;
            dz += steepZ * SlideSpeed / TicksPerSecond;
            Sliding = true;
        }

        double nx = x + dx, nz = z + dz;
        bool moved = dx != 0 || dz != 0;
        if (moved || residual > 0 || Stance != OpenStance.Standing)
        {
            double remaining = PushOut(ground, ref nx, y, ref nz);
            if (remaining > Tolerance && remaining > residual)
            {
                // Still in a wall after four pushes, and deeper than before: the step is undone (so nothing creeps
                // through a wall a micrometre a tick)
                nx = x; nz = z;
                Refused = true;
            }
            else residual = remaining;
        }

        // Without the grant, a step that would end in deep water is refused: the body stops at the edge
        if (Stance != OpenStance.Swimming && (grants & MovementGrants.Swim) == 0 && (nx != x || nz != z) && WouldSwim(ground, nx, y, nz))
        {
            nx = x; nz = z;
            Refused = StoppedAtWater = true;
        }

        switch (Stance)
        {
            case OpenStance.Standing: Stand(ground, nx, ref y, nz); break;
            case OpenStance.Swimming: Swim(ground, nx, ref y, nz); break;
        }
        if (Stance == OpenStance.Falling) Fall(ground, nx, ref y, nz);

        mx = ToMicro(nx); my = ToMicro(y); mz = ToMicro(nz);
    }

    /// <summary>The body put down: on the ground within a step of its feet, in water deep enough to swim, or falling.</summary>
    private void Settle(OpenGround ground, ref double y, double x, double z)
    {
        settled = true;
        steepX = steepZ = 0;
        fallSpeed = 0;
        if (ground.WaterAt(x, z) is { } surface && DeepAt(ground, x, z, y, surface))
        {
            Stance = OpenStance.Swimming;
            y = surface - SwimDepth;
            return;
        }
        if (ground.Footing(x, z, y + StepUp, y - SnapDown) is { } f)
        {
            Stance = OpenStance.Standing;
            y = f.Y;
            TakeFace(ground, ground[f.Triangle], x, y, z);
            return;
        }
        Stance = OpenStance.Falling;
        fallFrom = ToMicro(y);
    }

    private void Stand(OpenGround ground, double x, ref double y, double z)
    {
        if (ground.Footing(x, z, y + StepUp, y - SnapDown) is { } f)
        {
            if (ground.WaterAt(x, z) is { } surface && surface - f.Y > SwimDepth)
            {
                // Waded in past the depth that is swum (a step the grant let through)
                Stance = OpenStance.Swimming;
                steepX = steepZ = 0;
                y = surface - SwimDepth;
                return;
            }
            y = f.Y;
            TakeFace(ground, ground[f.Triangle], x, y, z);
            return;
        }
        Stance = OpenStance.Falling;
        steepX = steepZ = 0;
        fallSpeed = 0;
        fallFrom = ToMicro(y);
    }

    private void Fall(OpenGround ground, double x, ref double y, double z)
    {
        double v = fallSpeed / 1e6 - Gravity / ((double)TicksPerSecond * TicksPerSecond);
        v = Math.Max(v, -FastestFall / TicksPerSecond);
        double next = y + v;
        var f = ground.Footing(x, z, y + 1e-6, next);
        if (ground.WaterAt(x, z) is { } surface && next <= surface - SwimDepth && (f == null || f.Value.Y < surface - SwimDepth))
        {
            // Into the water: the body comes up to swim
            double into = surface - SwimDepth;
            Landed = ToMm(fallFrom - ToMicro(into));
            Splashed = true;
            Stance = OpenStance.Swimming;
            fallSpeed = 0;
            y = into;
            return;
        }
        if (f is { } ground1)
        {
            double fallen = fallFrom / 1e6 - ground1.Y;
            Landed = ToMm(fallFrom - ToMicro(ground1.Y));
            HardLanding = fallen > LandingDrop;
            Stance = OpenStance.Standing;
            fallSpeed = 0;
            y = ground1.Y;
            TakeFace(ground, ground[ground1.Triangle], x, y, z);
            return;
        }
        y = next;
        fallSpeed = ToMicro(v);
    }

    private void Swim(OpenGround ground, double x, ref double y, double z)
    {
        if (ground.WaterAt(x, z) is not { } surface)
        {
            // Out past the water's own cells: as on foot
            Stance = OpenStance.Standing;
            Stand(ground, x, ref y, z);
            return;
        }
        var f = ground.Footing(x, z, y + StepUp, double.NegativeInfinity);
        if (f is { } bed && surface - bed.Y <= SwimDepth)
        {
            // Shallow enough to stand: the body walks out
            Stance = OpenStance.Standing;
            y = bed.Y;
            TakeFace(ground, ground[bed.Triangle], x, y, z);
            return;
        }
        y = surface - SwimDepth;
    }

    /// <summary>Whether the body at x, z with its feet at y would be in water deeper than it swims: the ground under it, from a step over its feet down, is that far under the surface.</summary>
    private static bool WouldSwim(OpenGround ground, double x, double y, double z) =>
        ground.WaterAt(x, z) is { } surface && DeepAt(ground, x, z, y, surface);

    private static bool DeepAt(OpenGround ground, double x, double z, double y, double surface)
    {
        var f = ground.Footing(x, z, y + StepUp, double.NegativeInfinity);
        double bed = f?.Y ?? double.NegativeInfinity;
        return surface - bed > SwimDepth && y < surface;
    }

    /// <summary>
    /// The face stood on: steeper than 40°, the way down it is kept, and the body slides that way the next tick and
    /// the stick can't take it up. But a steep band no higher than a step is a step (the end of a rock bridge rising
    /// out of the meadow, a kerb's rounded edge): where walkable ground lies within <see cref="StepUp"/> over the feet
    /// a body's radius up the face, the face is stood on as any ground is, so what is stepped as a wall is stepped as a
    /// slope too.
    /// </summary>
    private void TakeFace(OpenGround ground, in OpenGround.Triangle face, double x, double y, double z)
    {
        steepX = steepZ = 0;
        if (face.Walkable) return;
        var n = face.Normal;
        double h = Math.Sqrt(n.X * n.X + n.Z * n.Z);
        if (h < 1e-9) return;
        double downX = n.X / h, downZ = n.Z / h;
        if (ground.Footing(x - downX * Radius, z - downZ * Radius, y + StepUp, y - 0.05, walkableOnly: true) != null) return;
        steepX = downX;
        steepZ = downZ;
    }

    /// <summary>
    /// The body's three spheres pushed off whatever isn't walkable ground near them, the deepest contact first, four
    /// pushes at most, each along the ground by as far as takes that contact clear. Returns how deep the body is
    /// still in, in metres (nought when clear).
    /// </summary>
    private double PushOut(OpenGround ground, ref double x, double y, ref double z)
    {
        ground.Near(x, z, Radius + 0.1, near);
        for (int round = 0; ; round++)
        {
            double deepest = 0, px = 0, pz = 0;
            foreach (double above in Spheres)
            {
                var centre = new OpenVec(x, y + above, z);
                foreach (int i in near)
                {
                    var t = ground[i];
                    if (t.Walkable) continue;
                    if (centre.X + Radius < t.MinX || centre.X - Radius > t.MaxX || centre.Z + Radius < t.MinZ || centre.Z - Radius > t.MaxZ
                        || centre.Y + Radius < t.MinY || centre.Y - Radius > t.MaxY) continue;
                    var cp = OpenTriangles.ClosestPoint(centre, t.A, t.B, t.C);
                    var away = centre - cp;
                    double d2 = OpenVec.Dot(away, away);
                    if (d2 >= Radius * Radius) continue;
                    double d = Math.Sqrt(d2);
                    double hx = away.X, hz = away.Z, hl = Math.Sqrt(hx * hx + hz * hz);
                    if (hl < 1e-6)
                    {
                        // The centre over the face itself: push the way the face looks along the ground
                        hx = t.Normal.X; hz = t.Normal.Z;
                        double nl = Math.Sqrt(hx * hx + hz * hz);
                        if (nl < 1e-6) continue;   // a ceiling flat over the head: nothing to push sideways from
                        hx /= nl; hz /= nl; hl = 0;
                    }
                    else { hx /= hl; hz /= hl; }
                    // How far along the ground takes the contact clear: s² + 2 s·hl + d² − R² = 0
                    double push = -hl + Math.Sqrt(hl * hl - d2 + Radius * Radius);
                    if (push > deepest) { deepest = push; px = hx; pz = hz; }
                }
            }
            if (deepest <= 1e-7) return 0;
            if (round == MostPushes) return deepest;
            x += px * (deepest + 1e-5);
            z += pz * (deepest + 1e-5);
            Pushes++;
            if (Math.Abs(px) + Math.Abs(pz) > 0 && deepest > 0.08) ground.Near(x, z, Radius + 0.1, near);
        }
    }
}
