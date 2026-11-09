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
    Swim = 1,

    // The mount's powers (plan 30 · FM12, plan 34 · PD2), read only while the body is mounted (OpenBody.Mounted)

    /// <summary>The mount sprints on the run button (<see cref="OpenBody.SprintSpeed"/>).</summary>
    Dash = 2,

    /// <summary>The mount's jump clears <see cref="OpenBody.HighJumpHeight"/>, not only <see cref="OpenBody.HopHeight"/>.</summary>
    HighJump = 4,

    /// <summary>A press in the air holds a glide (<see cref="OpenStance.Gliding"/>).</summary>
    Glide = 8,

    /// <summary>The mount clings to and climbs a steep face that is climbable (<see cref="OpenGround.Climbable"/>).</summary>
    Climb = 16
}

/// <summary>
/// A tick's steering (plan 32 · OW3): the stick's tilt east and south, each −1 to 1 (as <c>InputManager.GetStick</c>
/// gives it, its x east and its y down the screen, which is south), and whether the body runs; and (plan 30 · FM12)
/// whether the mount's button was pressed this tick, to jump, glide or let go, which only a mounted body reads.
/// </summary>
public readonly record struct OpenInput(double StickX, double StickZ, bool Run = false, bool Jump = false)
{
    public static readonly OpenInput None = default;
}

/// <summary>What the capsule is doing (plan 32 · OW3; plan 30 · FM12's two at the end, a mounted body's alone).</summary>
public enum OpenStance
{
    /// <summary>On the ground (walkable or steep: a steep face is stood on and slid down, decision 14).</summary>
    Standing,
    Falling,
    Swimming,

    /// <summary>The mount gliding down (plan 30 · FM12): sinking at <see cref="OpenBody.GlideSink"/>, steered at the trot's speed.</summary>
    Gliding,

    /// <summary>The mount clinging to a climbable face (plan 30 · FM12), moved up, down and along it by the stick.</summary>
    Climbing
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

    /// <summary>
    /// A fall caught (plan 32 · OW6, OW4's finding): a body falling with nothing facing up and no water anywhere under
    /// it in its column, once it has fallen more than this many metres, can land nowhere, and is put back where it
    /// last stood. Below a body's height, so a step off a lip slantwise onto the ground beside it is no catch.
    /// </summary>
    public const double CatchDrop = Height;

    /// <summary>The floor of the world (plan 32 · OW6): a fall longer than this many metres is caught whatever lies under it.</summary>
    public const double LongestFall = 200;

    // The mount (plan 30 · FM12, plan 34 · PD2): numbers of plan 34's and ours, metres and metres a second

    /// <summary>The mount at the stick's full tilt, and sprinting once the dash is granted.</summary>
    public const double TrotSpeed = 6.0, SprintSpeed = 11.0;

    /// <summary>The mount swimming (once the swim is granted), and climbing a face.</summary>
    public const double MountSwimSpeed = 4.0, ClimbSpeed = 1.5;

    /// <summary>
    /// How far the feet rise in the mount's own jump, and in the high jump once granted. A body lands on ground a step
    /// over its feet, so each clears a ledge <see cref="StepUp"/> higher: 1.25 m, and 2.75 m (plan 34: "clears 2.5 m").
    /// </summary>
    public const double HopHeight = 0.9, HighJumpHeight = 2.4;

    /// <summary>How fast a glide sinks.</summary>
    public const double GlideSink = 2.0;

    /// <summary>How near a sphere's centre a climbable face must be to be clung to, past the radius.</summary>
    private const double ClingReach = 0.08;

    /// <summary>How far a clinging body is drawn into its face a tick (the walls' own pushes put it back).</summary>
    private const double ClingPull = 0.02;

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

    // Where the body last stood on walkable ground, in micrometres: where a fall that can land nowhere puts it back
    private long goodX, goodY, goodZ;

    // The face a mounted body clings to: its way out along the ground (plan 30 · FM12)
    private double clingX, clingZ;

    /// <summary>
    /// Whether the body is the mount ridden (plan 30 · FM12, plan 34 · PD2): its speeds, its jump, and the powers its
    /// grants give (<see cref="MovementGrants.Dash"/> and on), which a walker never reads.
    /// </summary>
    public bool Mounted { get; private set; }

    /// <summary>The player gets on (<see cref="OpenWorld.MountRules.WhyNot"/> has said yes): nothing else of the body changes.</summary>
    public void Mount() => Mounted = true;

    /// <summary>
    /// The player gets off, wherever the body is (a fade's or a script's dismount is forced): gliding or clinging, it
    /// falls on foot from there; swimming, it swims on.
    /// </summary>
    public void Dismount()
    {
        if (!Mounted) return;
        Mounted = false;
        if (Stance is OpenStance.Gliding or OpenStance.Climbing) LetGo();
    }

    /// <summary>A glide or a cling ended in the air: falling from where the body is, with no speed.</summary>
    private void LetGo()
    {
        if (Stance == OpenStance.Climbing)
        {
            // Off the face a little, so the fall doesn't begin in it
            mx += ToMicro(clingX * 0.05);
            mz += ToMicro(clingZ * 0.05);
        }
        Stance = OpenStance.Falling;
        fallSpeed = 0;
        fallFrom = my;
    }

    /// <summary>The way out of the face clung to, along the ground (nought unless climbing).</summary>
    public (double X, double Z) Cling => Stance == OpenStance.Climbing ? (clingX, clingZ) : (0, 0);

    /// <summary>
    /// The speed a jump leaves the ground with, in metres a tick, for the feet to rise <paramref name="height"/>: the
    /// fall's own sum (each tick the speed less gravity's share of a tick, then the move) worked backward.
    /// </summary>
    public static double JumpSpeed(double height)
    {
        double g = Gravity / ((double)TicksPerSecond * TicksPerSecond);
        // Rising n ticks of v − g, v − 2g, …: about v²/2g − v/2 metres
        return g / 2 + Math.Sqrt(g * g / 4 + 2 * g * height);
    }

    public OpenBody(OpenPos feet, int yaw = 0)
    {
        mx = feet.X * 1000L; my = feet.Y * 1000L; mz = feet.Z * 1000L;
        (goodX, goodY, goodZ) = (mx, my, mz);
        Yaw = OpenYaw.Normalize(yaw);
    }

    /// <summary>Whether the last tick caught a fall that could land nowhere and put the body back where it last stood (plan 32 · OW6).</summary>
    public bool Caught { get; private set; }

    /// <summary>Where the body last stood on walkable ground, to the millimetre: where a caught fall puts it back.</summary>
    public OpenPos LastGood => new(ToMm(goodX), ToMm(goodY), ToMm(goodZ));

    /// <summary>Where the feet are, in micrometres: the body's own state.</summary>
    public (long X, long Y, long Z) Micrometres => (mx, my, mz);

    /// <summary>Where the feet are, to the millimetre (half away from nought).</summary>
    public OpenPos Pos => new(ToMm(mx), ToMm(my), ToMm(mz));

    /// <summary>The way the body faces, in 65,536ths of a turn (<see cref="OpenYaw"/>): the stick's way as it last moved.</summary>
    public int Yaw { get; private set; }

    /// <summary>Turns the body to face a way where it stands (a script's <c>faceto</c>, plan 32 · OW5): nothing else of it changes.</summary>
    public void Face(int yaw) => Yaw = OpenYaw.Normalize(yaw);

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
        Sliding = Refused = StoppedAtWater = HardLanding = Splashed = Caught = false;
        Landed = null;
        // A walker reads the swim alone: the mount's powers are the mount's (plan 30 · FM12)
        if (!Mounted) grants &= MovementGrants.Swim;

        double x = mx / 1e6, y = my / 1e6, z = mz / 1e6;

        // The way the stick asks, and the face's own slide
        double sx = input.StickX, sz = input.StickZ;
        double length = Math.Sqrt(sx * sx + sz * sz);
        double dx = 0, dz = 0;
        bool steered = length > DeadZone && !double.IsNaN(length);
        if (steered)
        {
            double tilt = Math.Min(1, length);
            double pace = Stance == OpenStance.Swimming ? (Mounted ? MountSwimSpeed : SwimSpeed)
                : Mounted ? (input.Run && (grants & MovementGrants.Dash) != 0 && Stance != OpenStance.Gliding ? SprintSpeed : TrotSpeed)
                : input.Run ? RunSpeed : WalkSpeed;
            double speed = pace * tilt / TicksPerSecond;
            dx = sx / length * speed;
            dz = sz / length * speed;
            // A body clinging to a face keeps facing it
            if (Stance != OpenStance.Climbing) Yaw = OpenTriangles.YawOf(sx, sz);
        }
        // A map with nothing in it to stand on is a place to stand (OW1's empty maps): the body only turns
        if (!ground.HasAnything) return;
        if (!settled) Settle(ground, ref y, x, z);

        if (Mounted)
        {
            // Clinging: the stick moves the body over the face, and the button lets go
            if (Stance == OpenStance.Climbing)
            {
                Climb(ground, input, x, y, z, steered ? sx / length * Math.Min(1, length) : 0, steered ? sz / length * Math.Min(1, length) : 0);
                return;
            }
            // The button: a jump from the ground, a glide in the air, and a glide let go
            if (input.Jump)
            {
                if (Stance == OpenStance.Standing) Jump((grants & MovementGrants.HighJump) != 0 ? HighJumpHeight : HopHeight);
                else if (Stance == OpenStance.Falling && (grants & MovementGrants.Glide) != 0) Stance = OpenStance.Gliding;
                else if (Stance == OpenStance.Gliding) LetGo();
            }
            // Steered into a climbable face: the body clings to it
            if ((grants & MovementGrants.Climb) != 0 && steered && Stance is OpenStance.Standing or OpenStance.Falling or OpenStance.Gliding
                && ClingTo(ground, x, y, z, sx / length, sz / length) is { } face)
            {
                (clingX, clingZ) = face;
                Stance = OpenStance.Climbing;
                steepX = steepZ = 0;
                fallSpeed = 0;
                Yaw = OpenTriangles.YawOf(-clingX, -clingZ);
                return;
            }
        }

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
        bool nowhere = Stance switch
        {
            OpenStance.Falling => Fall(ground, nx, ref y, nz),
            OpenStance.Gliding => Glide(ground, nx, ref y, nz),
            _ => false
        };
        if (nowhere)
        {
            // Nowhere to land (plan 32 · OW6): back where the body last stood, settled afresh at its next tick
            CatchTheFall();
            return;
        }

        mx = ToMicro(nx); my = ToMicro(y); mz = ToMicro(nz);
        if (Stance == OpenStance.Standing && steepX == 0 && steepZ == 0) (goodX, goodY, goodZ) = (mx, my, mz);
    }

    /// <summary>A fall that can land nowhere caught: the body put back on the last walkable ground it stood on, still, to settle there at its next tick.</summary>
    private void CatchTheFall()
    {
        (mx, my, mz) = (goodX, goodY, goodZ);
        Stance = OpenStance.Standing;
        settled = false;
        fallSpeed = 0;
        steepX = steepZ = 0;
        residual = double.MaxValue;
        Caught = true;
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

    /// <summary>A tick of a fall: landed, splashed or still falling; true where it can land nowhere and is to be caught.</summary>
    private bool Fall(OpenGround ground, double x, ref double y, double z)
    {
        double v = fallSpeed / 1e6 - Gravity / ((double)TicksPerSecond * TicksPerSecond);
        v = Math.Max(v, -FastestFall / TicksPerSecond);
        double next = y + v;
        if (v > 0)
        {
            // Rising in a jump (plan 30 · FM12; a walker never rises): stopped by a face turned down over the head, and
            // the fall counted from the highest the feet came to
            if (ground.Underside(x, z, y + Height, next + Height) is { } roof)
            {
                next = Math.Max(y, roof - Height - 1e-4);
                v = 0;
            }
            y = next;
            fallSpeed = ToMicro(v);
            fallFrom = ToMicro(y);
            return false;
        }
        // A mounted body coming down lands on ground up to a step over its feet (a jump level with a ledge's top stands
        // on it); a walker's fall lands only under them, as OW3 has it
        var f = ground.Footing(x, z, Mounted ? y + StepUp : y + 1e-6, next);
        if (ground.WaterAt(x, z) is { } surface && next <= surface - SwimDepth && (f == null || f.Value.Y < surface - SwimDepth))
        {
            // Into the water: the body comes up to swim
            double into = surface - SwimDepth;
            Landed = ToMm(fallFrom - ToMicro(into));
            Splashed = true;
            Stance = OpenStance.Swimming;
            fallSpeed = 0;
            y = into;
            return false;
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
            return false;
        }
        y = next;
        fallSpeed = ToMicro(v);
        // The floor of the world: a fall too long, or one with nothing under it in its column to land on (OW4 found a
        // body at the east cutting's lip falling past the overhang's underside to −669 m)
        double drop = fallFrom / 1e6 - y;
        if (drop > LongestFall) return true;
        return drop > CatchDrop && ground.WaterAt(x, z) == null && ground.Footing(x, z, y, double.NegativeInfinity) == null;
    }

    /// <summary>The mount leaves the ground with the speed that raises its feet <paramref name="height"/> (plan 30 · FM12).</summary>
    private void Jump(double height)
    {
        Stance = OpenStance.Falling;
        steepX = steepZ = 0;
        fallSpeed = ToMicro(JumpSpeed(height));
        fallFrom = my;
    }

    /// <summary>
    /// A tick of a glide (plan 30 · FM12): the body sinks at <see cref="GlideSink"/>, landing on ground up to a step
    /// over its feet (never hard) or coming down into deep water to swim; true where it can land nowhere and is to be
    /// caught, as a fall is.
    /// </summary>
    private bool Glide(OpenGround ground, double x, ref double y, double z)
    {
        double next = y - GlideSink / TicksPerSecond;
        var f = ground.Footing(x, z, y + StepUp, next);
        if (ground.WaterAt(x, z) is { } surface && next <= surface - SwimDepth && (f == null || f.Value.Y < surface - SwimDepth))
        {
            double into = surface - SwimDepth;
            Landed = ToMm(fallFrom - ToMicro(into));
            Splashed = true;
            Stance = OpenStance.Swimming;
            fallSpeed = 0;
            y = into;
            return false;
        }
        if (f is { } landing)
        {
            Landed = ToMm(fallFrom - ToMicro(landing.Y));
            Stance = OpenStance.Standing;
            fallSpeed = 0;
            y = landing.Y;
            TakeFace(ground, ground[landing.Triangle], x, y, z);
            return false;
        }
        y = next;
        fallSpeed = 0;
        double drop = fallFrom / 1e6 - y;
        if (drop > LongestFall) return true;
        return drop > CatchDrop && ground.WaterAt(x, z) == null && ground.Footing(x, z, y, double.NegativeInfinity) == null;
    }

    /// <summary>
    /// The climbable face a body at x, y, z clings to (plan 30 · FM12): the nearest steep face of a climbable triangle
    /// (<see cref="OpenGround.Climbable"/>; not walkable, and not turned down past a third) within a sphere's reach of
    /// any of its three spheres' centres, as its way out along the ground; and with <paramref name="wantX"/>,
    /// <paramref name="wantZ"/> given, only a face the stick pushes into. Null where there is none.
    /// </summary>
    private (double X, double Z)? ClingTo(OpenGround ground, double x, double y, double z, double? wantX = null, double? wantZ = null)
    {
        ground.Near(x, z, Radius + ClingReach + 0.1, near);
        (double X, double Z)? best = null;
        double nearest = double.MaxValue;
        foreach (double above in Spheres)
        {
            var centre = new OpenVec(x, y + above, z);
            foreach (int i in near)
            {
                if (!ground.Climbable(i)) continue;
                var t = ground[i];
                if (t.Walkable || t.Normal.Y < -0.3) continue;
                double hx = t.Normal.X, hz = t.Normal.Z, hl = Math.Sqrt(hx * hx + hz * hz);
                if (hl < 1e-6) continue;
                hx /= hl; hz /= hl;
                if (wantX is { } wx && wantZ is { } wz && wx * hx + wz * hz > -0.5) continue;
                var cp = OpenTriangles.ClosestPoint(centre, t.A, t.B, t.C);
                var away = centre - cp;
                double d = Math.Sqrt(OpenVec.Dot(away, away));
                if (d > Radius + ClingReach || d >= nearest) continue;
                nearest = d;
                best = (hx, hz);
            }
        }
        return best;
    }

    /// <summary>
    /// A tick on a face (plan 30 · FM12): the stick into the face is up, away from it down, along it sideways, at
    /// <see cref="ClimbSpeed"/> by the tilt. At the top, walkable ground just past the face within a step of the feet
    /// is stepped onto; at the foot, the ground. Otherwise the body is drawn into the face a little and pushed out by
    /// the walls, and a move that would leave every climbable face is not made. The button lets go.
    /// </summary>
    private void Climb(OpenGround ground, OpenInput input, double x, double y, double z, double ux, double uz)
    {
        if (input.Jump)
        {
            LetGo();
            return;
        }
        if (ux == 0 && uz == 0) return;
        double up = -(ux * clingX + uz * clingZ);
        double tx = -clingZ, tz = clingX;
        double side = ux * tx + uz * tz;
        double step = ClimbSpeed / TicksPerSecond;
        double ny = y + up * step, nx = x + tx * side * step, nz = z + tz * side * step;
        if (up > 0)
        {
            double ax = nx - clingX * (Radius + 0.2), az = nz - clingZ * (Radius + 0.2);
            // (and open over it: ground with rock just over it is the floor inside the cliff, not its top)
            if (ground.Footing(ax, az, ny + StepUp, ny - 0.1, walkableOnly: true) is { } top
                && ground.Underside(ax, az, top.Y - 1e-3, top.Y + Height) == null)
            {
                StandAfterTheClimb(ground, ax, top, az);
                return;
            }
        }
        else if (up < 0 && ground.Footing(nx, nz, y + 0.01, ny, walkableOnly: true) is { } foot)
        {
            StandAfterTheClimb(ground, nx, foot, nz);
            return;
        }
        double px = nx - clingX * ClingPull, pz = nz - clingZ * ClingPull;
        PushOut(ground, ref px, ny, ref pz);
        if (ClingTo(ground, px, ny, pz) is not { } face) return;
        (clingX, clingZ) = face;
        mx = ToMicro(px); my = ToMicro(ny); mz = ToMicro(pz);
    }

    private void StandAfterTheClimb(OpenGround ground, double x, (double Y, int Triangle) footing, double z)
    {
        Stance = OpenStance.Standing;
        clingX = clingZ = 0;
        residual = double.MaxValue;
        TakeFace(ground, ground[footing.Triangle], x, footing.Y, z);
        mx = ToMicro(x); my = ToMicro(footing.Y); mz = ToMicro(z);
        if (steepX == 0 && steepZ == 0) (goodX, goodY, goodZ) = (mx, my, mz);
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
