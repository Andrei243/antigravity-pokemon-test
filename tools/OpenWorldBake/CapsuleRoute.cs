using System.Collections.Concurrent;
using PokemonPlatinumEngine.OpenWorld;
using Capsule = PokemonPlatinumEngine.OpenWorld.OpenBody;

namespace OpenWorldBake;

/// <summary>Where a route walked with the game's capsule goes next (plan 32 · OW3): a point on the ground, run to or walked.</summary>
public readonly record struct RouteStop(double X, double Z, string Name, bool Run = false);

/// <summary>A tick of a route as kept: where the feet stood, how, and how far along the route.</summary>
public readonly record struct RouteSample(long Tick, OpenPos Pos, OpenStance Stance, int Stop, bool Pushed, bool Sliding, bool Refused);

/// <summary>
/// The game's own capsule (<see cref="Capsule"/>, plan 32 · OW3) walked along a route of stops on a region's baked
/// ground, the stick at full tilt toward the next stop, with what happened: what tests hold, and what
/// <c>--walk</c> draws over the region's plan and sections. A stop is passed once the feet are within 0.4 m of it on
/// the ground; a stop the body can't come nearer to for half a second while a wall, a face or the water's edge holds
/// it is given up (the walk goes on to the next); three seconds with under half a metre made good end the walk as stuck.
/// </summary>
public sealed class CapsuleRoute
{
    public List<RouteSample> Samples { get; } = new();
    public List<string> Events { get; } = new();

    /// <summary>The stops reached, by name, and those given up.</summary>
    public List<string> Reached { get; } = new();
    public List<string> GivenUp { get; } = new();

    public bool Finished { get; private set; }
    public bool Stuck { get; private set; }
    public int HardLandings { get; private set; }
    public double LongestFall { get; private set; }
    public int SlideTicks { get; private set; }
    public int PushTicks { get; private set; }
    public int SwimTicks { get; private set; }
    public double HighestY { get; private set; } = double.MinValue;
    public double LowestY { get; private set; } = double.MaxValue;

    /// <summary>Where the walk ended.</summary>
    public OpenPos End { get; private set; }

    /// <summary>A hash of every tick's place and stance: what two walks of one route are compared by.</summary>
    public long Hash()
    {
        unchecked
        {
            long h = 1469598103934665603;
            foreach (var s in Samples)
            {
                h = (h ^ s.Pos.X) * 1099511628211; h = (h ^ s.Pos.Y) * 1099511628211; h = (h ^ s.Pos.Z) * 1099511628211;
                h = (h ^ (int)s.Stance) * 1099511628211;
            }
            return h;
        }
    }

    public static CapsuleRoute Walk(OpenGround ground, OpenPos start, IReadOnlyList<RouteStop> stops,
        MovementGrants grants = MovementGrants.None, int yaw = 0, int mostTicks = OpenClock.TicksPerSecond * 600)
    {
        var walk = new CapsuleRoute();
        var body = new Capsule(start, yaw);
        int stop = 0;
        double closest = double.MaxValue;
        long closestTick = 0;
        var stuckFrom = start;
        long stuckTick = 0;
        while (body.Tick < mostTicks)
        {
            if (stop >= stops.Count) { walk.Finished = true; break; }
            var target = stops[stop];
            var here = OpenVec.Of(body.Pos);
            double dx = target.X - here.X, dz = target.Z - here.Z, dist = Math.Sqrt(dx * dx + dz * dz);
            if (dist < 0.4)
            {
                walk.Events.Add($"tick {body.Tick}: reached {target.Name} at {body.Pos} ({body.Stance})");
                walk.Reached.Add(target.Name);
                stop++;
                closest = double.MaxValue;
                continue;
            }
            if (dist < closest - 0.01) { closest = dist; closestTick = body.Tick; }
            else if (body.Tick - closestTick > OpenClock.TicksPerSecond / 2 && (body.Pushes > 0 || body.Sliding || body.Refused))
            {
                walk.Events.Add($"tick {body.Tick}: held {dist:0.000} m short of {target.Name} at {body.Pos}{(body.StoppedAtWater ? ", at the water's edge" : body.Sliding ? ", sliding" : "")}");
                walk.GivenUp.Add(target.Name);
                stop++;
                closest = double.MaxValue;
                continue;
            }
            body.Step(ground, new OpenInput(dx / dist, dz / dist, target.Run), grants);
            var pos = body.Pos;
            walk.Samples.Add(new RouteSample(body.Tick, pos, body.Stance, stop, body.Pushes > 0, body.Sliding, body.Refused));
            if (body.Pushes > 0) walk.PushTicks++;
            if (body.Sliding) walk.SlideTicks++;
            if (body.Stance == OpenStance.Swimming) walk.SwimTicks++;
            walk.HighestY = Math.Max(walk.HighestY, pos.MetresY);
            walk.LowestY = Math.Min(walk.LowestY, pos.MetresY);
            if (body.Landed is { } fall && fall > 50)
            {
                walk.LongestFall = Math.Max(walk.LongestFall, fall / 1000.0);
                if (body.HardLanding) walk.HardLandings++;
                walk.Events.Add($"tick {body.Tick}: {(body.Splashed ? "into the water" : body.HardLanding ? "a landing" : "down")} after {fall} mm at {pos}");
            }
            if (body.Tick - stuckTick >= OpenClock.TicksPerSecond * 3)
            {
                if (Math.Sqrt(pos.GroundDistanceSquared(stuckFrom)) < 500)
                {
                    walk.Events.Add($"tick {body.Tick}: !! stuck on the way to {target.Name} at {pos}");
                    walk.Stuck = true;
                    break;
                }
                stuckFrom = pos;
                stuckTick = body.Tick;
            }
        }
        walk.End = body.Pos;
        return walk;
    }

    /// <summary>
    /// A region's ground as the game collides with it, each chunk's level 0 baked from the descriptions the first
    /// time it is asked for (and kept in <paramref name="cache"/>, if given, for another ground of the same region).
    /// </summary>
    public static OpenGround GroundOf(RegionDescription description, TerrainWorld? land = null,
        ConcurrentDictionary<(int, int), BakedChunk?>? cache = null, IEnumerable<OpenBox>? boxes = null, IEnumerable<OpenWaterBox>? waters = null)
    {
        land ??= description.Land();
        var world = description.World;
        var chunks = RegionBake.ChunksOf(world).ToHashSet();
        cache ??= new ConcurrentDictionary<(int, int), BakedChunk?>();
        return new OpenGround((x, z) => cache.GetOrAdd((x, z), _ => chunks.Contains((x, z)) ? RegionBake.Finest(land, world, x, z) : null), boxes, waters);
    }
}
