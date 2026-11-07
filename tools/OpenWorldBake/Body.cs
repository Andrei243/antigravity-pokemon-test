namespace OpenWorldBake;

/// <summary>A place in the open world: whole millimetres (plan 32 · decision 2), the feet's height in <see cref="Y"/>.</summary>
public readonly record struct OpenPos(int X, int Y, int Z)
{
    public D3 Metres => new(Mm.Metres(X), Mm.Metres(Y), Mm.Metres(Z));
    public static OpenPos Of(D3 p) => new(Mm.Of(p.X), Mm.Of(p.Y), Mm.Of(p.Z));
    public override string ToString() => $"({X} mm, {Y} mm, {Z} mm)";
}

/// <summary>
/// The prototype's capsule (plan 32, "Movement"), a tick at a time at 120 a second: walked and run by the stick's
/// tilt, stepping up 0.35 m, kept off walls by up to four pushes a tick, held to the ground going down slopes,
/// and falling off a drop with a landing. The body above the step is three spheres along its axis, each pushed
/// out of whatever is not floor; the floor is found by a line straight down from the step's height. Every
/// quantity is worked in double and rounded to the micrometre once a tick (in 64-bit integers), so nothing builds
/// up and two runs of the same sticks end on the same micrometre. Not the millimetre the plan first named: a walk
/// of 1.5 m/s is 12.5 mm a tick, which whole millimetres round up to 13 every other tick (2% fast), and a step
/// slantwise of under half a millimetre on one axis rounds to nothing there, so a stick a few degrees off an axis
/// walked straight along it (OpenWorldPrototypeTests holds both). <see cref="Pos"/> is still told in millimetres.
/// </summary>
public sealed class OpenBody
{
    public const int TicksPerSecond = 120;
    public const double Radius = 0.35, Height = 1.6, StepUp = 0.35, SnapDown = 0.5;
    public const double WalkSpeed = 1.5, RunSpeed = 4.5, Gravity = 9.81;

    long mx, my, mz;

    /// <summary>Where the feet are, in micrometres: the body's own state.</summary>
    public (long X, long Y, long Z) Micrometres => (mx, my, mz);

    /// <summary>Where the feet are, to the millimetre.</summary>
    public OpenPos Pos => new(ToMm(mx), ToMm(my), ToMm(mz));

    static int ToMm(long micrometres) => (int)Math.Round(micrometres / 1000.0, MidpointRounding.AwayFromZero);
    static long ToMicro(double metres) => (long)Math.Round(metres * 1e6, MidpointRounding.AwayFromZero);
    /// <summary>The way the body faces, in 65,536ths of a turn: 0 is south (+z), a quarter is east (+x).</summary>
    public int Yaw { get; private set; }
    public bool Grounded { get; private set; } = true;
    /// <summary>The fall's speed, micrometres a tick, downward negative.</summary>
    public int FallMicrometres { get; private set; }
    public long Tick { get; private set; }
    /// <summary>Pushes off walls this tick (at most four).</summary>
    public int Pushes { get; private set; }
    /// <summary>The height a fall began at, while falling.</summary>
    public double? FallFrom { get; private set; }
    /// <summary>The height fallen, the tick a fall ended in.</summary>
    public double? Landed { get; private set; }

    public OpenBody(OpenPos start, int yaw = 0)
    {
        mx = start.X * 1000L; my = start.Y * 1000L; mz = start.Z * 1000L;
        Yaw = yaw;
    }

    public double YawRadians => Yaw * (Math.PI * 2 / 65536.0);

    /// <summary>One tick: the stick's x (east) and z (south), each -1 to 1, and the run button.</summary>
    public void Step(CollisionMesh mesh, double stickX, double stickZ, bool run)
    {
        Tick++;
        Landed = null;
        Pushes = 0;
        double x = mx / 1e6, y = my / 1e6, z = mz / 1e6;

        double tilt = Math.Min(1, Math.Sqrt(stickX * stickX + stickZ * stickZ));
        if (tilt > 0.02)
        {
            double len = Math.Sqrt(stickX * stickX + stickZ * stickZ);
            double speed = (run ? RunSpeed : WalkSpeed) * tilt / TicksPerSecond;
            x += stickX / len * speed;
            z += stickZ / len * speed;
            Yaw = ((int)Math.Round(Math.Atan2(stickX, stickZ) / (Math.PI * 2) * 65536.0) % 65536 + 65536) % 65536;
        }

        // The body off whatever isn't floor: the deepest push first, four at most (the collide-and-slide loop)
        for (int round = 0; round < 4; round++)
        {
            double deepest = 0, nx = 0, nz = 0;
            foreach (double above in new[] { StepUp + Radius, (StepUp + Height) / 2, Height - Radius })
            {
                var centre = new D3(x, y + above, z);
                foreach (int t in mesh.Near(x, z, Radius + 0.05))
                {
                    if (mesh.Walkable(t)) continue;
                    var (a, b, c, normal) = mesh.Triangles[t];
                    var cp = Tri.ClosestPoint(centre, a, b, c);
                    var away = centre - cp;
                    double d = away.Length;
                    if (d >= Radius) continue;
                    double hx = away.X, hz = away.Z;
                    double hl = Math.Sqrt(hx * hx + hz * hz);
                    if (hl < 1e-6) { hx = normal.X; hz = normal.Z; hl = Math.Sqrt(hx * hx + hz * hz); }
                    if (hl < 1e-6) continue;   // a ceiling flat over the head: nothing to push sideways from
                    double depth = Radius - d;
                    if (depth > deepest) { deepest = depth; nx = hx / hl; nz = hz / hl; }
                }
            }
            if (deepest <= 1e-6) break;
            x += nx * (deepest + 1e-4);
            z += nz * (deepest + 1e-4);
            Pushes++;
        }

        // The floor: a line down from the step's height. A steep face under the centre is stood on and slid down
        // (the prototype's first answer to a body pushed over the foot of a slope: OW3 decides the real one)
        Sliding = false;
        if (Grounded)
        {
            if (mesh.Footing(x, z, y + StepUp, y - SnapDown) is { } ground)
            {
                y = ground.Y;
                FallMicrometres = 0;
                SlideOff(mesh, ground.Triangle, ref x, ref z);
            }
            else
            {
                Grounded = false;
                FallFrom = y;
            }
        }
        if (!Grounded)
        {
            double vy = FallMicrometres / 1e6 - Gravity / ((double)TicksPerSecond * TicksPerSecond);
            double next = y + vy;
            if (mesh.Footing(x, z, y + 1e-6, next) is { } ground)
            {
                Landed = (FallFrom ?? y) - ground.Y;
                y = ground.Y;
                Grounded = true;
                FallFrom = null;
                FallMicrometres = 0;
                SlideOff(mesh, ground.Triangle, ref x, ref z);
            }
            else
            {
                y = next;
                FallMicrometres = (int)Math.Round(vy * 1e6, MidpointRounding.AwayFromZero);
            }
        }
        mx = ToMicro(x); my = ToMicro(y); mz = ToMicro(z);
    }

    /// <summary>How fast a body standing on a face steeper than 40° slides down it, metres a second over the ground.</summary>
    public const double SlideSpeed = 2.0;

    /// <summary>Whether the body slid down a steep face this tick.</summary>
    public bool Sliding { get; private set; }

    void SlideOff(CollisionMesh mesh, int triangle, ref double x, ref double z)
    {
        if (mesh.Walkable(triangle)) return;
        var n = mesh.Triangles[triangle].Normal;
        double h = Math.Sqrt(n.X * n.X + n.Z * n.Z);
        if (h < 1e-9) return;
        x += n.X / h * SlideSpeed / TicksPerSecond;
        z += n.Z / h * SlideSpeed / TicksPerSecond;
        Sliding = true;
    }
}

/// <summary>A box that runs its script as the capsule's centre comes into it (plan 32, "Volumes").</summary>
public sealed record TriggerBox(string Name, D3 Min, D3 Max)
{
    public bool Holds(D3 p) => p.X >= Min.X && p.X <= Max.X && p.Y >= Min.Y && p.Y <= Max.Y && p.Z >= Min.Z && p.Z <= Max.Z;
}

/// <summary>An encounter zone: a polygon with a height range (plan 32), and the Pokémon standing in it.</summary>
public sealed record Zone(string Name, (double X, double Z)[] Polygon, double MinY, double MaxY, string Species)
{
    public bool Holds(D3 p)
    {
        if (p.Y < MinY || p.Y > MaxY) return false;
        bool inside = false;
        for (int i = 0, j = Polygon.Length - 1; i < Polygon.Length; j = i++)
        {
            var (xi, zi) = Polygon[i]; var (xj, zj) = Polygon[j];
            if ((zi > p.Z) != (zj > p.Z) && p.X < (xj - xi) * (p.Z - zi) / (zj - zi) + xi) inside = !inside;
        }
        return inside;
    }
}

/// <summary>Where the walk goes next, and whether it runs there.</summary>
public readonly record struct Waypoint(double X, double Z, bool Run, string Name);

/// <summary>A tick of the walk as kept: where the body stood and what it was doing.</summary>
public readonly record struct WalkSample(long Tick, OpenPos Pos, int Yaw, bool Grounded, int Leg, double Speed, double Walked);

/// <summary>The capsule walked along a route, steered at each waypoint with the stick at full tilt, with what happened.</summary>
public sealed class WalkRecord
{
    public List<WalkSample> Samples { get; } = new();
    public List<string> Events { get; } = new();
    public bool Finished { get; set; }
    public int TriggerEntries { get; set; }
    public double HighestY { get; set; } = double.MinValue;
    public double LongestFall { get; set; }
    public int MostPushes { get; set; }
    /// <summary>Ticks in which the body was pushed off a wall (sliding along it, or pressed against it).</summary>
    public int PushTicks { get; set; }
    /// <summary>Ticks in which the body stood on a face steeper than 40° and slid down it.</summary>
    public int SlideTicks { get; set; }
    /// <summary>The lowest the feet went: under the ground's lowest floor would be a fall through the world.</summary>
    public double LowestY { get; set; } = double.MaxValue;

    /// <summary>A hash of every position: what two walks of the same sticks are compared by.</summary>
    public long PositionHash()
    {
        unchecked
        {
            long h = 1469598103934665603;
            foreach (var s in Samples) { h = (h ^ s.Pos.X) * 1099511628211; h = (h ^ s.Pos.Y) * 1099511628211; h = (h ^ s.Pos.Z) * 1099511628211; h = (h ^ s.Yaw) * 1099511628211; }
            return h;
        }
    }

    public static WalkRecord Run(CollisionMesh mesh, OpenPos start, IReadOnlyList<Waypoint> route, TriggerBox trigger, Zone zone, int maxTicks = 120 * 600)
    {
        var record = new WalkRecord();
        var body = new OpenBody(start);
        int leg = 0;
        bool inTrigger = false, inZone = false;
        double walked = 0;
        var last = body.Pos.Metres;
        var stuckFrom = last;
        long stuckTick = 0;
        double closest = double.MaxValue;
        long closestTick = 0;
        while (body.Tick < maxTicks)
        {
            if (leg >= route.Count) { record.Finished = true; break; }
            var target = route[leg];
            var here = body.Pos.Metres;
            double dx = target.X - here.X, dz = target.Z - here.Z;
            double dist = Math.Sqrt(dx * dx + dz * dz);
            if (dist < 0.4)
            {
                record.Events.Add($"tick {body.Tick}: reached {target.Name} at {body.Pos}");
                leg++;
                closest = double.MaxValue;
                continue;
            }
            // A waypoint in a wall: pushed against it, with no ground made toward it for half a second, the walk goes on
            if (dist < closest - 0.01) { closest = dist; closestTick = body.Tick; }
            else if (body.Tick - closestTick > OpenBody.TicksPerSecond / 2 && (body.Pushes > 0 || body.Sliding))
            {
                record.Events.Add($"tick {body.Tick}: stopped by the wall {dist:0.000} m short of {target.Name}, at {body.Pos}, after {record.PushTicks} ticks pushed off walls so far");
                leg++;
                closest = double.MaxValue;
                continue;
            }
            body.Step(mesh, dx / dist, dz / dist, target.Run);
            if (body.Pushes > 0) record.PushTicks++;
            if (body.Sliding) record.SlideTicks++;
            var (ux, uy, uz) = body.Micrometres;
            var now = new D3(ux / 1e6, uy / 1e6, uz / 1e6);
            double moved = Math.Sqrt((now.X - last.X) * (now.X - last.X) + (now.Z - last.Z) * (now.Z - last.Z));
            walked += moved;
            last = now;
            record.Samples.Add(new WalkSample(body.Tick, body.Pos, body.Yaw, body.Grounded, leg, moved * OpenBody.TicksPerSecond, walked));
            record.HighestY = Math.Max(record.HighestY, now.Y);
            record.LowestY = Math.Min(record.LowestY, now.Y);
            record.MostPushes = Math.Max(record.MostPushes, body.Pushes);
            if (body.Landed is { } fall && fall > 0.05)
            {
                record.LongestFall = Math.Max(record.LongestFall, fall);
                record.Events.Add($"tick {body.Tick}: landed after falling {fall:0.000} m at {body.Pos}");
            }
            var centre = now + new D3(0, 0.8, 0);
            bool nowInTrigger = trigger.Holds(centre);
            if (nowInTrigger && !inTrigger) { record.TriggerEntries++; record.Events.Add($"tick {body.Tick}: trigger '{trigger.Name}' runs (the centre came in at {body.Pos})"); }
            inTrigger = nowInTrigger;
            bool nowInZone = zone.Holds(now);
            if (nowInZone != inZone) record.Events.Add($"tick {body.Tick}: {(nowInZone ? "into" : "out of")} zone '{zone.Name}' at {body.Pos}");
            inZone = nowInZone;
            // Stuck: under half a metre made good in three seconds
            if (body.Tick - stuckTick >= OpenBody.TicksPerSecond * 3)
            {
                double gone = Math.Sqrt((now.X - stuckFrom.X) * (now.X - stuckFrom.X) + (now.Z - stuckFrom.Z) * (now.Z - stuckFrom.Z));
                if (gone < 0.5) { record.Events.Add($"tick {body.Tick}: !! stuck on the way to {target.Name} at {body.Pos}"); break; }
                stuckFrom = now;
                stuckTick = body.Tick;
            }
        }
        return record;
    }
}

/// <summary>
/// The orbit camera (plan 32, "Rendering"; GPU-free): a yaw and a pitch about a point over the player's feet, held
/// off walls by a spring arm cast against the collision mesh. The yaw is the way the camera looks, as the body's.
/// </summary>
public sealed class OrbitCamera
{
    public double Yaw, Pitch = 16 * Math.PI / 180, Distance = 5.5, Above = 1.35, FovY = 50;
    public const double Margin = 0.3, Shortest = 0.6;

    public (D3 Position, D3 Target, double Arm, bool Held) Place(CollisionMesh mesh, D3 feet)
    {
        var target = feet + new D3(0, Above, 0);
        // From the target back to the camera: behind the way it looks, and up by the pitch
        var back = new D3(-Math.Sin(Yaw) * Math.Cos(Pitch), Math.Sin(Pitch), -Math.Cos(Yaw) * Math.Cos(Pitch));
        double arm = Distance;
        bool held = false;
        if (mesh.Raycast(target, back, Distance + Margin) is { } hit)
        {
            arm = Math.Max(Shortest, hit - Margin);
            held = true;
        }
        return (target + back * arm, target, arm, held);
    }
}
