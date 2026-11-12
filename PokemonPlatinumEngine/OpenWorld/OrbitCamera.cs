using System;

namespace PokemonPlatinumEngine.OpenWorld;

/// <summary>
/// The open world's camera (plan 32 · OW8; style guide, "Open world"): it orbits behind the player at
/// <see cref="Back"/> metres, pitched <see cref="PitchDegrees"/> down at a point <see cref="Above"/> over the feet, and
/// is held off walls by a spring arm cast against the collision mesh (<see cref="Place"/>). The shoulders turn it
/// (<see cref="Turn"/>); it eases behind a walking player once it hasn't been turned by hand for a while; a script's
/// <c>camera look &lt;marker&gt;</c> sends the point it looks at to a marker (<see cref="LookAt"/>) and
/// <see cref="Home"/> brings it back. It is the picture's alone: nothing of the rules reads it, the hash keeps
/// nothing of it, and it is advanced by the frame's own time. GPU-free, and in double.
/// </summary>
public sealed class OrbitCamera
{
    /// <summary>How far behind the point it looks at the camera stands, in metres, when nothing holds it in.</summary>
    public const double Back = 5.5;

    /// <summary>How far it looks down, in degrees.</summary>
    public const double PitchDegrees = 16;

    /// <summary>How far over the feet the point it looks at is, in metres.</summary>
    public const double Above = 1.35;

    /// <summary>The vertical lens, in degrees (Platinum's 16° belongs to the grid).</summary>
    public const double LensDegrees = 50;

    /// <summary>
    /// The vertical lens under a script's zoom (<c>camera zoom</c>: 2 shows everything twice as large, 0.5 twice as much),
    /// in degrees: <see cref="LensDegrees"/> itself, to the bit, at a zoom of 1.
    /// </summary>
    public static double LensFor(double zoom) =>
        zoom == 1 || zoom <= 0 ? LensDegrees : 2 * Math.Atan(Math.Tan(LensDegrees * Math.PI / 360) / zoom) * 180 / Math.PI;

    /// <summary>The clip planes, in metres.</summary>
    public const double Near = 0.3, Far = 1400;

    /// <summary>How far short of what its arm meets the camera stops, and the shortest the arm is ever held.</summary>
    public const double Margin = 0.3, Shortest = 0.6;

    /// <summary>How fast the arm lets out again once what held it in has gone, in metres a second (it is held in at once).</summary>
    public const double ArmOut = 3.0;

    /// <summary>How fast the shoulders turn it, in radians a second (100°).</summary>
    public const double TurnRate = 100 * Math.PI / 180;

    /// <summary>The half-life of its ease behind a walking player, in seconds, and how long after a turn by hand it waits to begin.</summary>
    public const double FollowHalfLife = 0.9, FollowAfter = 2.0;

    /// <summary>A player walking toward the camera within this of straight on (radians, 45°) isn't followed: the camera would spin round.</summary>
    public const double TowardTheCamera = Math.PI / 4;

    /// <summary>How fast the player must be moving, in metres a second, for the camera to follow.</summary>
    public const double Walking = 0.2;

    /// <summary>A script's look: how far over the marker the point looked at is, and how long the way home takes.</summary>
    public const double LookAbove = 1.0, HomeSeconds = 0.8;

    /// <summary>The way the camera looks along the ground, in radians: nought south, a quarter turn east, as <see cref="OpenYaw"/>.</summary>
    public double Yaw { get; private set; }

    /// <summary>How far it looks down, in radians.</summary>
    public double Pitch { get; private set; } = PitchDegrees * Math.PI / 180;

    /// <summary>The arm's length the last time it was placed.</summary>
    public double Arm { get; private set; } = Back;

    /// <summary>Whether something held the arm in the last time it was placed.</summary>
    public bool Held { get; private set; }

    private double sinceTurned = double.PositiveInfinity;
    private bool placed;

    // A script's look: the point, how far toward it the camera has come (0 to 1), how fast it goes (shares a
    // second, less than nought on the way home), and the yaws it turns between
    private OpenVec? lookPoint;
    private double lookShare, lookSpeed, lookYawFrom, lookYawTo;

    /// <summary>The marker point a script's look holds the camera on, or null.</summary>
    public OpenVec? Looking => lookPoint;

    /// <summary>How far the camera has come toward a script's look, 0 to 1.</summary>
    public double LookShare => lookShare;

    /// <summary>The camera begun afresh behind someone facing a way (a map come into, a place put down on): its arm let out whole.</summary>
    public void Reset(int yaw)
    {
        Yaw = OpenYaw.Radians(yaw);
        Pitch = PitchDegrees * Math.PI / 180;
        Arm = Back;
        Length = Back;
        Held = false;
        placed = false;
        sinceTurned = double.PositiveInfinity;
        lookPoint = null;
        lookShare = lookSpeed = 0;
        OffTheMount();
    }

    /// <summary>The next placing lets the arm out whole where nothing holds it (a shot set up), as after <see cref="Reset"/>.</summary>
    public void Settle() => placed = false;

    /// <summary>Turned by the shoulders: <paramref name="share"/> from −1 (to the left, anticlockwise from above) to 1.</summary>
    public void Turn(double share, double seconds)
    {
        if (share == 0 || seconds <= 0) return;
        Yaw = Wrap(Yaw - Math.Clamp(share, -1, 1) * TurnRate * seconds);
        sinceTurned = 0;
    }

    /// <summary>How far the arm is let out where nothing holds it, in metres: <see cref="Back"/>, unless a tool posed it otherwise.</summary>
    public double Length { get; private set; } = Back;

    /// <summary>The steepest and the shallowest a tool may pose the camera, and its longest arm (a scenario's <c>look</c>).</summary>
    public const double HighestPitch = 89 * Math.PI / 180, LowestPitch = -60 * Math.PI / 180, LongestArm = 120;

    /// <summary>
    /// Posed by a tool at once (plan 32 · OW10: a scenario's <c>look</c>): the yaw, the pitch and the arm's length, held
    /// until the camera is begun afresh; it eases behind a walker as ever once left untouched.
    /// </summary>
    public void Pose(double yaw, double pitch, double length)
    {
        Yaw = Wrap(yaw);
        Pitch = Math.Clamp(pitch, LowestPitch, HighestPitch);
        Length = Math.Clamp(length, Shortest, LongestArm);
        sinceTurned = 0;
        placed = false;
    }

    /// <summary>Turned to look a way along the ground at once (a tool's: the harness's four yaws).</summary>
    public void Face(double yaw)
    {
        Yaw = Wrap(yaw);
        sinceTurned = 0;
    }

    /// <summary>
    /// A script's look (<c>camera look &lt;marker&gt;</c>): over <paramref name="seconds"/> the point looked at goes from
    /// over the player to <see cref="LookAbove"/> over <paramref name="point"/>, and the camera turns round behind the
    /// player, away from it. Nought seconds is there at once (a scene in a hurry).
    /// </summary>
    public void LookAt(OpenVec point, OpenVec feet, double seconds)
    {
        lookPoint = point + new OpenVec(0, LookAbove, 0);
        lookYawFrom = Yaw;
        double dx = point.X - feet.X, dz = point.Z - feet.Z;
        lookYawTo = dx * dx + dz * dz > 1e-6 ? Math.Atan2(dx, dz) : Yaw;
        // From wherever it stood: a look sent while another holds goes on from there
        if (seconds <= 0) { lookShare = 1; lookSpeed = 0; Yaw = lookYawTo; }
        else { lookShare = 0; lookSpeed = 1 / seconds; }
    }

    /// <summary>Back to the player over <paramref name="seconds"/>: the point comes home, and the camera keeps the way it was turned.</summary>
    public void Home(double seconds = HomeSeconds)
    {
        if (lookPoint == null) return;
        // The way it was turned stays: only the point it looks at comes home
        lookYawFrom = lookYawTo = Yaw;
        if (seconds <= 0) { lookPoint = null; lookShare = 0; lookSpeed = 0; }
        else lookSpeed = -1 / seconds;
    }

    /// <summary>
    /// A frame of the camera: a script's look eased on, and behind a walking player once it hasn't been turned by hand
    /// for <see cref="FollowAfter"/> seconds. <paramref name="playerYaw"/> is the way the player faces (65,536ths), and
    /// <paramref name="speed"/> how fast they move along the ground.
    /// </summary>
    public void Advance(double seconds, int playerYaw, double speed)
    {
        if (seconds <= 0) return;
        sinceTurned += seconds;
        if (lookPoint != null)
        {
            lookShare = Math.Clamp(lookShare + lookSpeed * seconds, 0, 1);
            Yaw = Wrap(lookYawFrom + Angle(lookYawTo - lookYawFrom) * Smooth(lookShare));
            if (lookSpeed < 0 && lookShare <= 0) { lookPoint = null; lookSpeed = 0; }
            return;
        }
        if (speed < Walking || sinceTurned < FollowAfter) return;
        double diff = Angle(OpenYaw.Radians(playerYaw) - Yaw);
        if (Math.Abs(diff) > Math.PI - TowardTheCamera) return;
        Yaw = Wrap(Yaw + diff * (1 - Math.Pow(0.5, seconds / FollowHalfLife)));
    }

    /// <summary>The way back from the point looked at to the camera, a unit length: behind the yaw and up by the pitch.</summary>
    public OpenVec BackWay => new(-Math.Sin(Yaw) * Math.Cos(Pitch), Math.Sin(Pitch), -Math.Cos(Yaw) * Math.Cos(Pitch));

    /// <summary>
    /// Where the camera stands and what it looks at, for the feet where they are drawn: the arm from over the feet back
    /// along <see cref="BackWay"/>, stopped <see cref="Margin"/> short of what it meets on <paramref name="ground"/> and
    /// never under <see cref="Shortest"/>; held in at once and let out at <see cref="ArmOut"/> metres a second over
    /// <paramref name="seconds"/> (the first placing after <see cref="Reset"/> lets it out whole).
    /// </summary>
    public (OpenVec Position, OpenVec Target) Place(OpenGround? ground, OpenVec feet, double seconds)
    {
        // Riding (plan 30 · FM13): over the mount, further back, a lead ahead in a sprint; nothing of it at a share of nought
        double ride = RideShare;
        var head = feet + new OpenVec(0, ride == 0 ? Above : Above + (rideAbove - Above) * ride, 0);
        var back = BackWay;
        double length = Length;
        if (ride != 0)
        {
            double pitch = Pitch + (ridePitch - PitchDegrees) * Math.PI / 180 * ride;
            back = new OpenVec(-Math.Sin(Yaw) * Math.Cos(pitch), Math.Sin(pitch), -Math.Cos(Yaw) * Math.Cos(pitch));
            length = Math.Max(Shortest, Length + (rideArm - Back) * ride);
            head += new OpenVec(Math.Sin(Yaw), 0, Math.Cos(Yaw)) * (rideLead * ride);
        }
        double allowed = length;
        Held = false;
        if (ground != null && ground.HasAnything && ground.Raycast(head, back, length + Margin) is { } hit)
        {
            allowed = Math.Max(Shortest, hit - Margin);
            Held = true;
        }
        if (!placed || allowed <= Arm) Arm = allowed;
        else Arm = Math.Min(allowed, Arm + ArmOut * Math.Max(0, seconds));
        placed = true;
        var position = head + back * Arm;
        var target = head;
        if (lookPoint is { } point)
        {
            double s = Smooth(lookShare);
            target = head + (point - head) * s;
        }
        return (position, target);
    }

    private static double Smooth(double t) => t * t * (3 - 2 * t);

    // ---------------------------------------------------------------- the mount (plan 30 · FM13)

    /// <summary>Riding at the trot or standing: the point looked at over the feet, the arm, the pitch in degrees (style guide, "Open world", "The mount").</summary>
    public const double RideAbove = 2.4, RideArm = 7.4, RidePitchDegrees = 18;

    /// <summary>A sprint's longer arm, wider lens (degrees) and lead ahead (metres), whole from <see cref="SprintFrom"/> + 5 m a second.</summary>
    public const double SprintArm = 1.2, SprintLens = 6, SprintLead = 1.5, SprintFrom = 6;

    /// <summary>A glide's point, arm, pitch and lens; a climb's point, arm and pitch; a swim's point and arm.</summary>
    public const double GlideAbove = 2.2, GlideArm = 8.5, GlidePitchDegrees = 26, GlideLens = 54,
        ClimbAbove = 2.2, ClimbArm = 7.0, ClimbPitchDegrees = 4, SwimAbove = 1.8, SwimArm = 6.8;

    /// <summary>How long the camera takes to come over to the mount and back (the share's run from nought to one), and its ease between stances' numbers (a half-life), in seconds.</summary>
    public const double RideSeconds = 0.5, RideHalfLife = 0.3;

    private double rideShare;
    private double rideAbove = RideAbove, rideArm = RideArm, ridePitch = RidePitchDegrees, rideLens = LensDegrees, rideLead;

    /// <summary>How far the camera has come over to the mount, 0 to 1 (smoothstep of its run): nought on foot, and then nothing of the mount reaches the camera.</summary>
    public double RideShare => rideShare <= 0 ? 0 : rideShare >= 1 ? 1 : Smooth(rideShare);

    /// <summary>
    /// A frame of the mount for the camera: its share run toward riding or not over <see cref="RideSeconds"/>, and its
    /// numbers eased toward the stance's (<paramref name="speed"/> over the ground, metres a second, for a sprint's).
    /// Back on foot with the share at nought, the numbers are the trot's again for the next ride.
    /// </summary>
    public void Ride(bool mounted, OpenStance stance, double speed, double seconds)
    {
        if (seconds <= 0) return;
        rideShare = Math.Clamp(rideShare + (mounted ? 1 : -1) * seconds / RideSeconds, 0, 1);
        if (!mounted && rideShare <= 0)
        {
            (rideAbove, rideArm, ridePitch, rideLens, rideLead) = (RideAbove, RideArm, RidePitchDegrees, LensDegrees, 0);
            return;
        }
        double sprint = Math.Clamp((speed - SprintFrom) / 5, 0, 1);
        var (above, arm, pitch, lens, lead) = !mounted ? (rideAbove, rideArm, ridePitch, rideLens, rideLead) : stance switch
        {
            OpenStance.Gliding => (GlideAbove, GlideArm, GlidePitchDegrees, GlideLens, 0.0),
            OpenStance.Climbing => (ClimbAbove, ClimbArm, ClimbPitchDegrees, LensDegrees, 0.0),
            OpenStance.Swimming => (SwimAbove, SwimArm, RidePitchDegrees, LensDegrees, 0.0),
            _ => (RideAbove, RideArm + SprintArm * sprint, RidePitchDegrees, LensDegrees + SprintLens * sprint, SprintLead * sprint)
        };
        double k = 1 - Math.Pow(0.5, seconds / RideHalfLife);
        rideAbove += (above - rideAbove) * k;
        rideArm += (arm - rideArm) * k;
        ridePitch += (pitch - ridePitch) * k;
        rideLens += (lens - rideLens) * k;
        rideLead += (lead - rideLead) * k;
    }

    /// <summary>Off the mount at once (a fade's midpoint took the player off): the share at nought and the numbers the trot's.</summary>
    public void OffTheMount()
    {
        rideShare = 0;
        (rideAbove, rideArm, ridePitch, rideLens, rideLead) = (RideAbove, RideArm, RidePitchDegrees, LensDegrees, 0);
    }

    /// <summary>The vertical lens with the mount's share of its widening over <paramref name="lens"/> (the zoom's), in degrees: <paramref name="lens"/> itself, to the bit, on foot.</summary>
    public double RideLens(double lens)
    {
        double s = RideShare;
        return s == 0 ? lens : lens + (rideLens - LensDegrees) * s;
    }

    /// <summary>The ride's share and its numbers as they stand, for a tool to print.</summary>
    public (double Share, double Above, double Arm, double Pitch, double Lens, double Lead) RideNow => (RideShare, rideAbove, rideArm, ridePitch, rideLens, rideLead);

    /// <summary>An angle brought into −π to π.</summary>
    public static double Angle(double radians)
    {
        double a = radians % (Math.PI * 2);
        if (a > Math.PI) a -= Math.PI * 2;
        else if (a < -Math.PI) a += Math.PI * 2;
        return a;
    }

    private static double Wrap(double radians)
    {
        double a = radians % (Math.PI * 2);
        return a < 0 ? a + Math.PI * 2 : a;
    }
}

/// <summary>
/// Whether the camera is under rock (plan 32 · OW8; style guide, "Open world", "Under rock"): a ray straight up from it
/// meets the land within <see cref="Reach"/> metres. The cave's light eases in over <see cref="EaseSeconds"/> as it
/// goes under and out again as it comes out (<see cref="Share"/>). The picture's alone; GPU-free.
/// </summary>
public sealed class OpenUnderRock
{
    /// <summary>How far up the rock may be, in metres.</summary>
    public const double Reach = 30;

    /// <summary>How long the cave's light takes to come in, or to go.</summary>
    public const double EaseSeconds = 0.8;

    /// <summary>Whether the camera was under rock the last time it was asked.</summary>
    public bool Under { get; private set; }

    /// <summary>How far the cave's light is in, 0 to 1, eased.</summary>
    public double Share { get; private set; }

    /// <summary>A frame: is there rock over the camera, and the share eased toward it.</summary>
    public void Advance(OpenGround? ground, OpenVec camera, double seconds)
    {
        Under = Overhead(ground, camera);
        double step = Math.Max(0, seconds) / EaseSeconds;
        Share = Under ? Math.Min(1, Share + step) : Math.Max(0, Share - step);
    }

    /// <summary>At once (a map come into, a shot set up): the share whole or nothing.</summary>
    public void Settle(OpenGround? ground, OpenVec camera)
    {
        Under = Overhead(ground, camera);
        Share = Under ? 1 : 0;
    }

    /// <summary>Whether a ray straight up from a point meets the land within <see cref="Reach"/>.</summary>
    public static bool Overhead(OpenGround? ground, OpenVec point) =>
        ground != null && ground.HasAnything && ground.Raycast(point, new OpenVec(0, 1, 0), Reach) != null;
}

/// <summary>
/// Which level of detail each chunk of an open map is drawn at (plan 32, "Streaming rings"; style guide, "Open world",
/// "Levels"): the block of three chunks by three round the focus's chunk at level 0, then level 1 to
/// <see cref="SecondReach"/> metres, and level 2 beyond. The focus's chunk moves on only once the feet are
/// <see cref="BorderSlack"/> past its border, and a chunk keeps the far level it has within <see cref="RingSlack"/>
/// of the ring between them, so walking along a border swaps nothing back and forth. GPU-free.
/// </summary>
public sealed class OpenRings
{
    /// <summary>How far from the feet level 1 reaches, in metres (to the chunk's nearest point).</summary>
    public const double SecondReach = 300;

    /// <summary>How far past its chunk's border the focus goes before its chunk is the next.</summary>
    public const double BorderSlack = 4;

    /// <summary>How far either side of <see cref="SecondReach"/> a chunk keeps the level it has.</summary>
    public const double RingSlack = 10;

    private const double Side = OpenMap.ChunkSide / (double)OpenPos.PerMetre;

    /// <summary>The chunk the block of level 0 is round, once the focus has been told.</summary>
    public (int X, int Z)? Focus { get; private set; }

    /// <summary>Forgets the focus (another map).</summary>
    public void Reset() => Focus = null;

    /// <summary>Tells where the feet are: the focus's chunk moves on once they are <see cref="BorderSlack"/> past its border.</summary>
    public void Move(OpenVec feet)
    {
        var at = ((int)Math.Floor(feet.X / Side), (int)Math.Floor(feet.Z / Side));
        if (Focus is not { } focus) { Focus = at; return; }
        if (at == focus) return;
        double x0 = focus.X * Side - BorderSlack, z0 = focus.Z * Side - BorderSlack;
        double x1 = (focus.X + 1) * Side + BorderSlack, z1 = (focus.Z + 1) * Side + BorderSlack;
        if (feet.X < x0 || feet.X >= x1 || feet.Z < z0 || feet.Z >= z1) Focus = at;
    }

    /// <summary>
    /// The level a chunk is wanted at, of <paramref name="levels"/>, given the level it is drawn at now (−1 for none):
    /// 0 in the focus's block, 1 to <see cref="SecondReach"/>, 2 beyond.
    /// </summary>
    public int LevelOf(int chunkX, int chunkZ, OpenVec feet, int levels, int current = -1)
    {
        var focus = Focus ?? ((int)Math.Floor(feet.X / Side), (int)Math.Floor(feet.Z / Side));
        int last = Math.Max(0, levels - 1);
        if (Math.Abs(chunkX - focus.X) <= 1 && Math.Abs(chunkZ - focus.Z) <= 1) return 0;
        double d = DistanceTo(chunkX, chunkZ, feet);
        double reach = current switch { 1 => SecondReach + RingSlack, 2 => SecondReach - RingSlack, _ => SecondReach };
        return Math.Min(d < reach ? 1 : 2, last);
    }

    /// <summary>How far a chunk's nearest point on the ground is from the feet, in metres (nought inside it).</summary>
    public static double DistanceTo(int chunkX, int chunkZ, OpenVec feet)
    {
        double x0 = chunkX * Side, z0 = chunkZ * Side;
        double dx = Math.Max(0, Math.Max(x0 - feet.X, feet.X - (x0 + Side)));
        double dz = Math.Max(0, Math.Max(z0 - feet.Z, feet.Z - (z0 + Side)));
        return Math.Sqrt(dx * dx + dz * dz);
    }
}
