using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumTests;

/// <summary>
/// Kalos's four hand-drawn Gyms that were plain halls (plan 23, Kalos's Gyms; docs/mechanics/rulings.md, "Kalos's Gyms,
/// its water and the Mamoswine ride"): Viola's web (<see cref="SantaluneWeb"/>), Grant's wall (<see cref="CyllageWall"/>),
/// Korrina's rink (<see cref="ShalourRails"/>) and Ramos's tree (<see cref="CoumarinePoles"/>). Each puzzle's rule, and a
/// search of its hall by the field's own steps (<see cref="FieldMovement.Step"/>, the puzzle's state changing as the
/// player's steps change it): from the door to the tile before the Leader and back, nobody left where the door can't be
/// come to again, and the fewest toggles the way to the Leader needs.
/// </summary>
public class KalosGymPuzzlesTests
{
    private static readonly Lazy<Func<string, Map>> Rooms = new(MapDatabase.Fresh);

    /// <summary>The tile inside each hall's door, and the tile before each Leader.</summary>
    private static readonly (int X, int Y) Door = (6, 16), Leader = (6, 5);

    /// <summary>A hall and how its puzzle's state is read and set as a number (nought as it is laid out).</summary>
    private sealed record Hall(Map Map, Func<int> Get, Action<int> Set);

    private static Hall Santalune()
    {
        var map = Rooms.Value("SantaluneCity_Gym");
        var web = Assert.IsType<SantaluneWeb>(map.Puzzle);
        return new Hall(map, () => web.DewOnB ? 1 : 0, s => web.DewOnB = s == 1);
    }

    private static Hall Cyllage()
    {
        var map = Rooms.Value("CyllageCity_Gym");
        Assert.IsType<CyllageWall>(map.Puzzle);
        return new Hall(map, () => 0, _ => { });
    }

    private static Hall Shalour()
    {
        var map = Rooms.Value("ShalourCity_Gym");
        var rails = Assert.IsType<ShalourRails>(map.Puzzle);
        return new Hall(map, () => rails.Thrown ? 1 : 0, s => rails.Thrown = s == 1);
    }

    private static Hall Coumarine()
    {
        var map = Rooms.Value("CoumarineCity_Gym");
        var poles = Assert.IsType<CoumarinePoles>(map.Puzzle);
        return new Hall(map, () => poles.Down ? 1 : 0, s => poles.Down = s == 1);
    }

    private static readonly Direction[] Ways = { Direction.Up, Direction.Down, Direction.Left, Direction.Right };

    /// <summary>The steps the field allows from a tile in a state: where each ends and the state after it (a walk's step changes it as the player's does).</summary>
    private static IEnumerable<(int X, int Y, int S, bool Toggled)> Steps(Hall hall, int x, int y, int s, bool toggling)
    {
        foreach (var way in Ways)
        {
            hall.Set(s);
            var step = FieldMovement.Step(hall.Map, x, y, way, new Walker(TravelMode.OnFoot, hall.Map.HeightAt(x, y)));
            if (!step.Moves) continue;
            if (toggling && step.Kind == StepKind.Walk) hall.Map.Puzzle!.Crossed(hall.Map, step.X, step.Y, way);
            int next = hall.Get();
            yield return (step.X, step.Y, next, next != s);
        }
        hall.Set(s);
    }

    /// <summary>Every (tile, state) that can be come to from a start, with the fewest toggles to each (a search where only a toggle costs).</summary>
    private static Dictionary<(int X, int Y, int S), int> Explore(Hall hall, (int X, int Y) from, int state, bool toggling = true)
    {
        var best = new Dictionary<(int, int, int), int> { [(from.X, from.Y, state)] = 0 };
        var queue = new LinkedList<(int X, int Y, int S)>();
        queue.AddLast((from.X, from.Y, state));
        while (queue.Count > 0)
        {
            var node = queue.First!.Value;
            queue.RemoveFirst();
            int cost = best[node];
            foreach (var (nx, ny, ns, toggled) in Steps(hall, node.X, node.Y, node.S, toggling).ToList())
            {
                int c = cost + (toggled ? 1 : 0);
                if (best.TryGetValue((nx, ny, ns), out int had) && had <= c) continue;
                best[(nx, ny, ns)] = c;
                if (toggled) queue.AddLast((nx, ny, ns));
                else queue.AddFirst((nx, ny, ns));
            }
        }
        hall.Set(0);
        return best;
    }

    /// <summary>The fewest toggles from the door to a tile (any state), or null where it can't be come to.</summary>
    private static int? Toggles(Dictionary<(int X, int Y, int S), int> reached, (int X, int Y) to) =>
        reached.Where(r => (r.Key.X, r.Key.Y) == to).Select(r => (int?)r.Value).Min();

    /// <summary>The door's tile can be come to again from every (tile, state) the hall lets the player reach.</summary>
    private static void NobodyIsStranded(Hall hall, string name)
    {
        var reached = Explore(hall, Door, 0);
        foreach (var (x, y, s) in reached.Keys)
            Assert.True(Explore(hall, (x, y), s).Keys.Any(k => (k.X, k.Y) == Door), $"{name}: from {x},{y} in state {s} the door is out of reach");
    }

    // ------------------------------------------------------------------ Viola's web

    [Fact]
    public void ViolasWebHoldsWhereTheDewLiesAndAKnotShakesIt()
    {
        var hall = Santalune();
        var map = hall.Map;
        var web = (SantaluneWeb)map.Puzzle!;
        web.Reset();
        // Every tile of the web is open in the room's file over the dark; the rest of the dark is shut
        for (int y = 6; y <= 12; y++)
            for (int x = 1; x < map.Width - 1; x++)
                Assert.Equal(!SantaluneWeb.Web.ContainsKey((x, y)), map.IsSolid(x, y));
        // Every radial strand lies beside a ring, a knot or one of the two floors, so a strand is never far from what holds
        foreach (var ((x, y), strand) in SantaluneWeb.Web.Where(w => w.Value is SantaluneWeb.Strand.A or SantaluneWeb.Strand.B))
            Assert.True(new[] { (x, y - 1), (x, y + 1), (x - 1, y), (x + 1, y) }.Any(n =>
                SantaluneWeb.Web.TryGetValue(n, out var other) && other is SantaluneWeb.Strand.Ring or SantaluneWeb.Strand.Knot
                || n.Item2 is <= 5 or >= 13), $"the strand at {x},{y} is far from anything that holds");

        // As laid out the dew is on set A: A holds, B refuses a step, a ring always holds
        Assert.False(web.Refuses(map, 3, 11, 0f, false));
        Assert.True(web.Refuses(map, 6, 11, 0f, false));
        Assert.True(web.Refuses(map, 4, 8, 0f, false));
        Assert.False(web.Refuses(map, 5, 10, 0f, false));
        // A step onto a ring shakes nothing; a step onto a knot runs the dew to set B
        web.Crossed(map, 5, 10, Direction.Left);
        Assert.False(web.DewOnB);
        web.Crossed(map, 1, 10, Direction.Left);
        Assert.True(web.DewOnB);
        Assert.True(web.Refuses(map, 3, 11, 0f, false));
        Assert.False(web.Refuses(map, 4, 8, 0f, false));
        // Coming in lays it out again
        web.Arrive(map, new PokemonPlatinumEngine.Story.StoryState(), new Random(1));
        Assert.False(web.DewOnB);

        // From the door: no way to Viola without shaking the web; the way there shakes it twice, and the way back once
        var still = Explore(hall, Door, 0, toggling: false);
        Assert.Null(Toggles(still, Leader));
        var reached = Explore(hall, Door, 0);
        Assert.Equal(2, Toggles(reached, Leader));
        foreach (var ((x, y, s), _) in reached.Where(r => (r.Key.X, r.Key.Y) == Leader))
            Assert.NotNull(Toggles(Explore(hall, Leader, s), Door));
        NobodyIsStranded(hall, "Santalune");
    }

    // ------------------------------------------------------------------ Grant's wall

    [Fact]
    public void GrantsWallIsClimbedUpAndSidewaysByItsGripsAndGoneDownByItsRopes()
    {
        var hall = Cyllage();
        var map = hall.Map;
        var wall = (CyllageWall)map.Puzzle!;
        // Every tile of the wall and of its ropes is open in the room's file
        for (int y = 0; y < map.Height; y++)
            for (int x = 0; x < map.Width; x++)
                if (CyllageWall.IsWall(x, y) || CyllageWall.IsRope(x, y)) Assert.False(map.IsSolid(x, y), $"{x},{y} is the wall's and solid");
        // Bare rock refuses a step; a grip doesn't
        Assert.True(wall.Refuses(map, 3, 13, 0f, false));
        Assert.False(wall.Refuses(map, 6, 13, 0f, false));
        // Down from a grip onto the wall is barred, down off a pitch's foot onto the floor isn't
        Assert.True(wall.Bars(map, 6, 13, Direction.Down));
        Assert.False(wall.Bars(map, 6, 14, Direction.Down));
        Assert.False(wall.Bars(map, 6, 12, Direction.Up));
        // A rope is gone down, never up
        Assert.True(wall.Bars(map, 1, 13, Direction.Up));
        Assert.False(wall.Bars(map, 1, 13, Direction.Down));
        Assert.Equal(StepKind.Blocked, FieldMovement.Step(map, 1, 14, Direction.Up, new Walker(TravelMode.OnFoot, 0f)).Kind);
        Assert.Equal(Obstacle.Rail, FieldMovement.Step(map, 6, 12, Direction.Down, new Walker(TravelMode.OnFoot, 0f)).Obstacle);

        // Every grip leads on (up to the floor above, or sideways to a rope): nobody is left on the wall
        var reached = Explore(hall, Door, 0);
        Assert.Equal(0, Toggles(reached, Leader));
        Assert.NotNull(Toggles(Explore(hall, Leader, 0), Door));
        NobodyIsStranded(hall, "Cyllage");
        // ...and the wall is climbed: the way to Grant is longer than the walk straight up the hall
        Assert.True(Shortest(hall, Door, Leader) > Door.Y - Leader.Y + 2);
        // The two Mega Stones' balls in the floor's corners are come to beside them
        Assert.Contains(reached.Keys, k => (k.X, k.Y) is (1, 15) or (2, 16));
        Assert.Contains(reached.Keys, k => (k.X, k.Y) is (11, 15) or (10, 16));
    }

    /// <summary>The fewest steps from one tile to another in a hall whose state never changes.</summary>
    private static int Shortest(Hall hall, (int X, int Y) from, (int X, int Y) to)
    {
        var seen = new Dictionary<(int, int), int> { [from] = 0 };
        var queue = new Queue<(int X, int Y)>();
        queue.Enqueue(from);
        while (queue.Count > 0)
        {
            var at = queue.Dequeue();
            if (at == to) return seen[at];
            foreach (var (nx, ny, _, _) in Steps(hall, at.X, at.Y, 0, toggling: false).ToList())
                if (seen.TryAdd((nx, ny), seen[at] + 1)) queue.Enqueue((nx, ny));
        }
        return int.MaxValue;
    }

    // ------------------------------------------------------------------ Korrina's rink

    [Fact]
    public void KorrinasRampsHopTheirGapsOneWayAndALeverTurnsThemAllRound()
    {
        var hall = Shalour();
        var map = hall.Map;
        var rails = (ShalourRails)map.Puzzle!;
        rails.Reset();
        // Each gap is the dark, a rail's tile either side of it
        for (int i = 0; i < ShalourRails.Gaps.Length; i++)
        {
            var g = ShalourRails.Gaps[i];
            Assert.True(map.IsSolid(g.X, g.Y), $"gap {i} is open");
            var (before, beyond) = ShalourRails.EndsOf(i);
            Assert.False(map.IsSolid(before.X, before.Y));
            Assert.False(map.IsSolid(beyond.X, beyond.Y));
        }
        // As laid out the first gap is hopped north and not south; thrown, the other way
        Assert.True(rails.HopsOver(3, 13, Direction.Up));
        Assert.False(rails.HopsOver(3, 13, Direction.Down));
        Assert.Equal(StepKind.Hop, FieldMovement.Step(map, 3, 14, Direction.Up, new Walker(TravelMode.OnFoot, 0f)).Kind);
        // (a skater hops it too: the rink is skated)
        Assert.Equal(StepKind.Hop, FieldMovement.Step(map, 3, 14, Direction.Up, new Walker(TravelMode.Skating, 0f)).Kind);
        rails.Crossed(map, 2, 11, Direction.Left);
        Assert.True(rails.Thrown);
        Assert.False(rails.HopsOver(3, 13, Direction.Up));
        Assert.True(rails.HopsOver(3, 13, Direction.Down));
        Assert.True(rails.HopsOver(6, 10, Direction.Right));
        // A step onto a rail that is no lever throws nothing
        rails.Crossed(map, 4, 10, Direction.Right);
        Assert.True(rails.Thrown);
        Assert.True(ShalourRails.MayHopOver(3, 13, Direction.Up) && ShalourRails.MayHopOver(3, 13, Direction.Down));
        Assert.False(ShalourRails.MayHopOver(3, 13, Direction.Left));
        rails.Arrive(map, new PokemonPlatinumEngine.Story.StoryState(), new Random(1));
        Assert.False(rails.Thrown);

        // No way to Korrina without throwing the points; the way there throws them twice
        Assert.Null(Toggles(Explore(hall, Door, 0, toggling: false), Leader));
        var reached = Explore(hall, Door, 0);
        Assert.Equal(2, Toggles(reached, Leader));
        foreach (var ((_, _, s), _) in reached.Where(r => (r.Key.X, r.Key.Y) == Leader))
            Assert.NotNull(Toggles(Explore(hall, Leader, s), Door));
        NobodyIsStranded(hall, "Shalour");
    }

    // ------------------------------------------------------------------ Ramos's tree

    [Fact]
    public void RamossPolesAreClimbedUpAndSlidDownAndTheHangingOneComesDownByItsVine()
    {
        var hall = Coumarine();
        var map = hall.Map;
        var poles = (CoumarinePoles)map.Puzzle!;
        poles.Reset();
        // The three tiers stand at nought, two and four, and every pole stands on a face, which is shut
        Assert.Equal(0f, map.HeightAt(6, 15));
        Assert.Equal(2f, map.HeightAt(6, 9));
        Assert.Equal(4f, map.HeightAt(6, 5));
        foreach (var p in CoumarinePoles.Poles) Assert.True(map.IsSolid(p.X, p.Y), $"the pole at {p.X},{p.Y} stands on open floor");
        // A climbing pole goes up only, a sliding pole down only
        Assert.True(poles.HopsOver(2, 12, Direction.Up));
        Assert.False(poles.HopsOver(2, 12, Direction.Down));
        Assert.True(poles.HopsOver(10, 12, Direction.Down));
        Assert.False(poles.HopsOver(10, 12, Direction.Up));
        var climb = FieldMovement.Step(map, 2, 13, Direction.Up, new Walker(TravelMode.OnFoot, 0f));
        Assert.Equal((StepKind.Hop, 2, 11, 2f), (climb.Kind, climb.X, climb.Y, climb.Height));
        // The hanging pole is no way up until its vine is pulled, and stays down after
        Assert.False(poles.HopsOver(7, 7, Direction.Up));
        Assert.True(CoumarinePoles.MayHopOver(7, 7, Direction.Up));
        poles.Crossed(map, 1, 4, Direction.Left);
        Assert.True(poles.Down);
        Assert.True(poles.HopsOver(7, 7, Direction.Up));
        Assert.False(poles.HopsOver(7, 7, Direction.Down));
        poles.Crossed(map, 1, 4, Direction.Right);
        Assert.True(poles.Down);
        poles.Arrive(map, new PokemonPlatinumEngine.Story.StoryState(), new Random(1));
        Assert.False(poles.Down);

        // No way to Ramos without pulling the vine; the way there pulls it once, and slides back down to the door
        Assert.Null(Toggles(Explore(hall, Door, 0, toggling: false), Leader));
        var reached = Explore(hall, Door, 0);
        Assert.Equal(1, Toggles(reached, Leader));
        Assert.NotNull(Toggles(Explore(hall, Leader, 1), Door));
        NobodyIsStranded(hall, "Coumarine");
    }

    // ------------------------------------------------------------------ the walk of the whole game

    [Fact]
    public void TheWalkOfTheWholeGameStandsBeforeEachOfTheFourLeaders()
    {
        var world = World.LoadAll().Single(w => w.Index.Region == "Kalos");
        var built = world.BuildMaps().ToDictionary(m => m.Name);
        var rooms = MapDatabase.Fresh();
        Map Named(string name) => built.TryGetValue(name, out var map) ? map : rooms(name);
        var walked = WorldWalk.From(Named, RegionDatabase.Get(RegionDatabase.Kalos)!.Start!);
        foreach (string gym in new[] { "SantaluneCity_Gym", "CyllageCity_Gym", "ShalourCity_Gym", "CoumarineCity_Gym" })
            Assert.True(walked.Any(p => p.Key.Name == gym && p.Value.Contains(Leader)), $"the walk never stands before {gym}'s Leader");
    }
}
