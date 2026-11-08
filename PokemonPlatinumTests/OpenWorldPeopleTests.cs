using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenWorldBake;
using PokemonPlatinumEngine.OpenWorld;
using PokemonPlatinumEngine.Story;
using Xunit;
using OpenBody = PokemonPlatinumEngine.OpenWorld.OpenBody;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 32 · OW6, GPU-free: OW3's open fault mended (a fall that can land nowhere is caught and put back where the
/// body last stood, on the lab's edge as on boxes); a trainer's cone (a wall between hides the player, the cone's edge
/// sees, past it and past the length doesn't, a kerb lower than the line doesn't hide); who is asked (the beaten,
/// the held, the absent and the same trainer twice passed by); a trainer walking up the lab's navigation mesh and a
/// pair walking up together; someone wandering within their bounds on the mesh, the same twice, and nobody beyond
/// the reach; a follower who never cuts a corner the player walked round; the people's index; the places' files'
/// people read, laid and refused; and the scripts' commands naming people.
/// </summary>
public class OpenWorldPeopleTests
{
    const int T = OpenClock.TicksPerSecond;
    static readonly OpenBox Floor = OpenBox.Metres("floor", -40, -1, -40, 40, 0, 40);

    static OpenGround Ground(params OpenBox[] boxes) => new(null, boxes);

    // ------------------------------------------------------------------ the open test world, with its navigation mesh

    /// <summary>The lab's level 0 and its navigation mesh, baked once into a folder of their own.</summary>
    static readonly Lazy<string> labBakedWithNav = new(() =>
    {
        string from = OpenWorldCapsuleTests.LabBaked;
        string dir = Path.Combine(Path.GetTempPath(), "openlab-nav-" + Guid.NewGuid().ToString("N"));
        foreach (string file in Directory.GetFiles(from, "*", SearchOption.AllDirectories))
        {
            string to = Path.Combine(dir, Path.GetRelativePath(from, file));
            Directory.CreateDirectory(Path.GetDirectoryName(to)!);
            File.Copy(file, to);
        }
        var land = BakedRegion.Open(from);
        var chunks = land.Chunks.Select(c => land.Chunk(c.X, c.Z, 0)!).ToList();
        // with the places' solids, as the region's bake has it: the house is walked round
        var (solids, waters) = RegionBake.PlacedOf(RegionDescription.Read(OpenWorldBakeTests.LabFolder));
        Assert.Single(solids);
        File.WriteAllBytes(Path.Combine(dir, OpenNavMesh.FileName), NavBake.Build(chunks, solids, waters).Bytes());
        return dir;
    });

    /// <summary>The open test world's map with its places and people, on level 0 with the navigation mesh.</summary>
    internal static OpenMap LabWithPeople()
    {
        var map = OpenPlaceFiles.LoadRegion(OpenWorldBakeTests.LabFolder, withLand: false).Single();
        map.Land = BakedRegion.Open(labBakedWithNav.Value);
        return map;
    }

    /// <summary>A field of the map with the player on a marker, ticked so many ticks with the stick at rest, after each the given look.</summary>
    static void Tick(OpenField field, int ticks, Action? after = null, OpenInput? input = null)
    {
        for (int i = 0; i < ticks; i++)
        {
            field.Player.Step(field.Map, input ?? OpenInput.None, field.Grants);
            field.StepTheRest();
            after?.Invoke();
        }
    }

    // ------------------------------------------------------------------ OW3's fault: a fall that can land nowhere

    [Fact]
    public void AFallThatCanLandNowhereIsCaughtAndPutBackWhereItLastStood()
    {
        // A shelf ten metres long in the air, nothing under it: walked off its east end
        var ground = Ground(OpenBox.Metres("shelf", -2, -1, -2, 10, 0, 2));
        List<(OpenPos Pos, OpenStance Stance, bool Caught)> Walk()
        {
            var body = new OpenBody(new OpenPos(0, 0, 0), OpenYaw.East);
            var trace = new List<(OpenPos, OpenStance, bool)>();
            for (int i = 0; i < 12 * T; i++)
            {
                body.Step(ground, new OpenInput(1, 0));
                trace.Add((body.Pos, body.Stance, body.Caught));
            }
            return trace;
        }
        var walk = Walk();
        Assert.Contains(walk, s => s.Stance == OpenStance.Falling);
        Assert.Contains(walk, s => s.Caught);
        // Never further down than a body's height and a tick's fall, and every catch put back on the shelf
        Assert.All(walk, s => Assert.InRange(s.Pos.Y, -(int)(OpenBody.CatchDrop * 1000) - 200, 1));
        foreach (var (pos, _, caught) in walk.Where(s => s.Caught))
        {
            Assert.InRange(pos.X, 0, 10_000);
            Assert.Equal(0, pos.Y);
        }
        // And the same frames the same micrometres
        Assert.Equal(walk, Walk());
    }

    [Fact]
    public void ABodyWalkedOffTheLabsEdgeIsCaughtOnItsMeadowAndStandsThere()
    {
        var map = LabWithPeople();
        var ground = map.Ground;
        var start = OpenPos.FromMetres(58, ground.Footing(58, 20, 10, -10)!.Value.Y, 20);
        var body = new OpenBody(start, OpenYaw.East);
        int caught = 0, lowest = int.MaxValue;
        for (int i = 0; i < 8 * T; i++)
        {
            body.Step(ground, new OpenInput(1, 0));
            if (body.Caught) caught++;
            lowest = Math.Min(lowest, body.Pos.Y);
        }
        Assert.True(caught > 0, "walking east past x 64 m, where no chunk is, nothing caught the fall");
        Assert.True(lowest > -3000, $"the body fell to {lowest} mm");
        // At rest it stands on the meadow by the edge, where it last stood
        for (int i = 0; i < T; i++) body.Step(ground, OpenInput.None);
        Assert.Equal(OpenStance.Standing, body.Stance);
        Assert.InRange(body.Pos.X, 60_000, 65_000);   // (a chunk's ground reaches half a metre past its edge)
        // A real drop still lands: off the shelf onto the floor three metres down, never caught
        var drop = new OpenBody(new OpenPos(0, 3000, 0));
        var floors = Ground(Floor, OpenBox.Metres("shelf", -2, 2, -2, 2, 3, 2));
        bool everCaught = false;
        for (int i = 0; i < 3 * T; i++) { drop.Step(floors, new OpenInput(1, 0)); everCaught |= drop.Caught; }
        Assert.False(everCaught);
        Assert.Equal((0, OpenStance.Standing), (drop.Pos.Y, drop.Stance));
    }

    // ------------------------------------------------------------------ the cone

    static readonly OpenSight Ten = OpenSight.Of(10, 60);   // ten metres, thirty degrees to either side

    [Fact]
    public void ATrainerSeesWithinTheConeToItsEdgeAndNotPastIt()
    {
        var ground = Ground(Floor);
        var feet = new OpenPos(0, 0, 0);
        Assert.Equal(OpenYaw.Turn / 12, Ten.HalfAngle);
        Assert.True(Ten.Sees(ground, feet, OpenYaw.East, new OpenPos(8000, 0, 0)));
        Assert.True(Ten.Sees(ground, feet, OpenYaw.East, new OpenPos(10_000, 0, 0)));
        Assert.False(Ten.Sees(ground, feet, OpenYaw.East, new OpenPos(10_001, 0, 0)));   // past the length
        Assert.False(Ten.Sees(ground, feet, OpenYaw.West, new OpenPos(8000, 0, 0)));    // behind them
        // At the cone's edge: 29° off the way they face is seen, 31° isn't (x east, z south: south of east)
        OpenPos At(double degrees) => OpenPos.FromMetres(8 * Math.Sin(Math.PI / 2 - degrees * Math.PI / 180), 0, 8 * Math.Cos(Math.PI / 2 - degrees * Math.PI / 180));
        Assert.True(Ten.Sees(ground, feet, OpenYaw.East, At(29)));
        Assert.True(Ten.Sees(ground, feet, OpenYaw.East, At(-29)));
        Assert.False(Ten.Sees(ground, feet, OpenYaw.East, At(31)));
        Assert.False(Ten.Sees(ground, feet, OpenYaw.East, At(-31)));
        Assert.False(Ten.Sees(ground, feet, OpenYaw.East, feet));   // nobody sees their own feet
    }

    [Fact]
    public void ATrainerBehindAWallDoesntSeeAndOneBehindAKerbDoes()
    {
        var feet = new OpenPos(0, 0, 0);
        var player = new OpenPos(8000, 0, 0);
        // A wall three metres high between them, across the way
        Assert.False(Ten.Sees(Ground(Floor, OpenBox.Metres("wall", 4, 0, -3, 4.3, 3, 3)), feet, OpenYaw.East, player));
        // A kerb half a metre high: the line from the eyes (1.5 m) to the chest (1.2 m) passes over it
        Assert.True(Ten.Sees(Ground(Floor, OpenBox.Metres("kerb", 4, 0, -3, 4.3, 0.5, 3)), feet, OpenYaw.East, player));
        // The wall off to the side doesn't hide them
        Assert.True(Ten.Sees(Ground(Floor, OpenBox.Metres("wall", 4, 0, 2, 4.3, 3, 4)), feet, OpenYaw.East, player));
        // On the lab: north of the house, looking south, the house's step is behind its block
        var map = LabWithPeople();
        var watcher = OpenPos.FromMetres(-20, map.Ground.Footing(-20, -54, 10, -10)!.Value.Y, -54);
        var step = map.Marker("house_step")!.Pos;
        var wide = OpenSight.Of(16, 120);
        Assert.False(wide.Sees(map.Ground, watcher, OpenYaw.South, step));
        // and east of the house, in the open, the same distance off, is seen
        var east = OpenPos.FromMetres(-8, map.Ground.Footing(-8, -46, 10, -10)!.Value.Y, -46);
        Assert.True(wide.Sees(map.Ground, watcher, OpenYaw.South, east));
    }

    [Fact]
    public void WhoIsAskedPassesByTheBeatenTheHeldTheAbsentAndTheSameTrainerTwice()
    {
        var map = new OpenMap("Cones", "lab");
        map.Add(Floor);
        OpenPerson Trainer(string key, string id, int x) => new(key, "cones", new OpenPos(x, 0, 0), OpenYaw.East) { TrainerId = id, Sight = Ten };
        var a = Trainer("a", "camper_jacob", -6000);
        var b = Trainer("b", "picnicker_siena", -5000);
        var c = Trainer("c", "camper_jacob", -4000);   // the same trainer of the data as a (twins)
        foreach (var p in new[] { a, b, c }) map.Add(p);
        var player = new OpenPos(2000, 0, 0);
        Assert.Same(a, OpenApproach.FindSpotter(map, player, _ => true));
        Assert.Same(b, OpenApproach.FindSpotter(map, player, _ => true, except: a));   // never c, who is a again
        Assert.Same(b, OpenApproach.FindSpotter(map, player, p => p != a));
        a.Held = true;
        Assert.Same(b, OpenApproach.FindSpotter(map, player, _ => true));
        b.Present = false;
        Assert.Same(c, OpenApproach.FindSpotter(map, player, _ => true));
        Assert.Null(OpenApproach.FindSpotter(map, player, _ => false));
        // Nobody without a cone looks out
        var talker = new OpenPerson("d", "cones", new OpenPos(-3000, 0, 0), OpenYaw.East) { TrainerId = "youngster_tristan" };
        map.Add(talker);
        a.Held = false; b.Present = true;
        Assert.Null(OpenApproach.FindSpotter(map, player, p => p == talker));
    }

    // ------------------------------------------------------------------ the walk up

    [Fact]
    public void ATrainerWalksUpTheMeshAndAPairWalksUpTogether()
    {
        var map = LabWithPeople();
        var gap = map.Marker("twins_gap")!;
        var field = new OpenField(map, gap.Pos, gap.Yaw);
        Tick(field, 2);
        var first = OpenApproach.FindSpotter(map, field.Player.Pos, _ => true);
        Assert.NotNull(first);
        Assert.Equal("camper_west", first!.Key);
        var second = OpenApproach.FindSpotter(map, field.Player.Pos, _ => true, except: first);
        Assert.Equal("picnicker_east", second?.Key);
        var one = new OpenApproach(first);
        var two = new OpenApproach(second!, turnPlayer: false);
        field.PeopleMayWander = false;
        int ticks = 0, bothWalking = 0;
        Tick(field, 20 * T, () =>
        {
            one.Watch(map, field.Player);
            two.Watch(map, field.Player);
            ticks++;
            if (first.Walk != null && second!.Walk != null) bothWalking++;
        });
        Assert.True(one.IsDone && two.IsDone, $"after {ticks} ticks: {first} and {second}");
        Assert.False(one.Stopped || two.Stopped);
        // They walked at once, side by side in time, and stand a talking distance from the player's feet
        Assert.True(bothWalking > 5 * T, $"both walked {bothWalking} ticks");
        foreach (var t in new[] { first, second! })
        {
            double d = Math.Sqrt(t.Pos.GroundDistanceSquared(field.Player.Pos));
            Assert.InRange(d, 700, OpenApproach.TalkDistance + 20);
            int toward = OpenTriangles.YawOf(field.Player.Pos.X - t.Pos.X, field.Player.Pos.Z - t.Pos.Z);
            Assert.InRange(Math.Abs(OpenYaw.Between(t.Yaw, toward)), 0, 400);
        }
        // The player turned to the first, and stood still all the while
        Assert.InRange(Math.Abs(OpenYaw.Between(field.Player.Yaw, OpenTriangles.YawOf(first.Pos.X - field.Player.Pos.X, first.Pos.Z - field.Player.Pos.Z))), 0, 400);
        Assert.Equal((gap.Pos.X, gap.Pos.Z), (field.Player.Pos.X, field.Player.Pos.Z));
    }

    [Fact]
    public void ATrainersWalkUpGoesRoundAWallOverTheMesh()
    {
        // North of the house, the player south of it on its step: the walk goes round the house's block, never through it
        var map = LabWithPeople();
        var watcher = new OpenPerson("watcher", "lab_meadow", OpenPos.FromMetres(-20, 0.2, -54), OpenYaw.South) { TrainerId = "youngster_tristan" };
        map.Add(watcher);
        var step = map.Marker("house_step")!;
        var field = new OpenField(map, step.Pos, step.Yaw);
        Tick(field, 2);
        var walkUp = new OpenApproach(watcher);
        var house = map.Solids.Single(s => s.Name == "the house");
        Tick(field, 30 * T, () =>
        {
            walkUp.Watch(map, field.Player);
            var p = watcher.Pos;
            Assert.False(p.X > house.Min.X - 300 && p.X < house.Max.X + 300 && p.Z > house.Min.Z - 300 && p.Z < house.Max.Z + 300,
                $"the trainer walked into the house at {p}");
        });
        Assert.True(walkUp.IsDone && !walkUp.Stopped);
        Assert.InRange(Math.Sqrt(watcher.Pos.GroundDistanceSquared(field.Player.Pos)), 700, OpenApproach.TalkDistance + 20);
    }

    // ------------------------------------------------------------------ wandering

    [Fact]
    public void SomeoneWandersWithinTheirBoundsOnTheMeshTheSameTwiceAndNobodyBeyondTheReach()
    {
        List<OpenPos> Wander(string marker)
        {
            var map = LabWithPeople();
            var at = map.Marker(marker)!;
            var field = new OpenField(map, at.Pos, at.Yaw);
            var walker = map.Person("walker")!;
            var trace = new List<OpenPos>();
            Tick(field, 40 * T, () => trace.Add(walker.Pos));
            return trace;
        }
        var trace = Wander("north_meadow");
        var walkerHome = LabWithPeople().Person("walker")!;
        // It moved, a good way and more than once, always within its bounds (a radius of four metres) and on the meadow
        Assert.True(trace.Distinct().Count() > 4 * T, $"{trace.Distinct().Count()} places");
        Assert.True(trace.Max(p => p.GroundDistanceSquared(walkerHome.Home)) > 1_000_000L, "it never went a metre from home");
        Assert.All(trace, p => Assert.True(walkerHome.InBounds(p), $"{p} is out of its bounds"));
        Assert.All(trace, p => Assert.InRange(p.Y, -600, 1200));
        Assert.Equal(trace, Wander("north_meadow"));
        // From the west road, more than the reach away, it stands where it was put
        var far = Wander("west_road");
        Assert.Single(far.Distinct());
    }

    // ------------------------------------------------------------------ the follower

    [Fact]
    public void AFollowerNeverCutsACornerTheyWereWalkedRoundAndKeepsTheirGap()
    {
        // A block at x 2 to 6, z −6 to 2: the player runs east along z 2.5 half a metre past its south side, then north
        // along x 6.5 past its east: a follower cutting the corner would pass over the block's own corner
        var block = OpenBox.Metres("block", 2, 0, -6, 6, 3, 2);
        var map = new OpenMap("Corner", "lab");
        map.Add(Floor);
        map.Add(block);
        var field = new OpenField(map, OpenPos.FromMetres(-4, 0, 2.5), OpenYaw.East);
        field.Follower = new OpenFollower("lead", field.Player.Pos, field.Player.Yaw);
        var follower = field.Follower;
        double nearest = double.MaxValue;
        int behind = 0;
        void Look()
        {
            var p = follower.Pos;
            double dx = Math.Max(Math.Max(block.Min.X - p.X, p.X - block.Max.X), 0), dz = Math.Max(Math.Max(block.Min.Z - p.Z, p.Z - block.Max.Z), 0);
            nearest = Math.Min(nearest, Math.Sqrt(dx * dx + dz * dz));
            if (follower.Pos != field.Player.Pos) behind++;
        }
        Tick(field, 280, Look, new OpenInput(1, 0, Run: true));     // east, at a run (37.5 mm a tick), 10.5 m to x 6.5
        Tick(field, 330, Look, new OpenInput(0, -1, Run: true));    // north past its east side, to z −9.875
        Tick(field, T, Look);
        // The player went round the corner, and so did the follower: never nearer the block than the trail's half metre
        Assert.True(field.Player.Pos.Z < -6000 && field.Player.Pos.X > 6000, $"the player is at {field.Player.Pos}");
        Assert.True(nearest >= 450, $"the follower came {nearest:0} mm from the block");
        // and stood its gap behind the player at the end, on the trail
        double gap = Math.Sqrt(follower.Pos.DistanceSquared(field.Player.Pos));
        Assert.InRange(gap, OpenFollower.Gap - 60, OpenFollower.Gap + 5);
        Assert.True(behind > 600, $"{behind} ticks behind");
        // A place the player was put down at begins the trail afresh, the follower on them
        field.Player.Place(OpenPos.FromMetres(-20, 0, -20), OpenYaw.South);
        Tick(field, 1);
        Assert.Equal(field.Player.Pos, follower.Pos);
    }

    // ------------------------------------------------------------------ the index

    [Fact]
    public void ThePeoplesIndexAnswersAsAScanDoes()
    {
        var map = new OpenMap("Crowd", "lab");
        var rng = new Random(7);
        for (int i = 0; i < 200; i++)
            map.Add(new OpenPerson($"p{i}", "crowd", new OpenPos(rng.Next(-100_000, 100_000), 0, rng.Next(-100_000, 100_000)), 0));
        void Same()
        {
            for (int k = 0; k < 40; k++)
            {
                var at = new OpenPos(rng.Next(-110_000, 110_000), 0, rng.Next(-110_000, 110_000));
                int reach = rng.Next(1000, 40_000);
                var scanned = map.People.Where(p => p.Pos.GroundDistanceSquared(at) <= (long)reach * reach).ToList();
                Assert.Equal(scanned, map.PeopleNear(at, reach));
            }
        }
        Same();
        // Moved about, and told
        foreach (var p in map.People)
        {
            p.PutAt(new OpenPos(rng.Next(-100_000, 100_000), 0, rng.Next(-100_000, 100_000)), 0);
            map.Moved(p);
        }
        Same();
        Assert.Throws<ArgumentException>(() => map.Add(new OpenPerson("P0", "crowd", default, 0)));
        Assert.Throws<ArgumentException>(() => map.Add(new OpenPerson("player", "crowd", default, 0)));
        map.Add(new OpenMarker("m", default));
        Assert.Throws<ArgumentException>(() => map.Add(new OpenPerson("m", "crowd", default, 0)));
        Assert.Throws<ArgumentException>(() => map.Add(new OpenMarker("p1", default)));
    }

    // ------------------------------------------------------------------ the places' files

    [Fact]
    public void TheLabsPeopleAreReadAndLaid()
    {
        var map = OpenPlaceFiles.LoadRegion(OpenWorldBakeTests.LabFolder, withLand: false).Single();
        Assert.Equal(new[] { "walker", "camper_west", "picnicker_east" }, map.People.Select(p => p.Key));
        var walker = map.Person("walker")!;
        Assert.Equal(("lab_meadow", OpenPersonMoves.Wander, null, null), (walker.Place, walker.Moves, walker.TrainerId, walker.Sight));
        Assert.Equal(new OpenPos(6000, 200, -54_000), walker.Home);
        Assert.True(walker.InBounds(new OpenPos(9900, 0, -54_000)) && !walker.InBounds(new OpenPos(10_100, 0, -54_000)));
        Assert.Single(walker.Lines);
        var camper = map.Person("CAMPER_WEST")!;
        Assert.Equal(("camper_jacob", OpenYaw.East, OpenPersonMoves.Still), (camper.TrainerId, camper.Yaw, camper.Moves));
        Assert.Equal(new OpenSight(16_000, 3641), camper.Sight);   // twenty degrees to either side, in 65,536ths
    }

    [Theory]
    [InlineData("""{ "people": [ { "name": "a", "at": [0, 0, 0], "sight": { "length": 5 } } ] }""", "$.people[0].sight: only a trainer looks out")]
    [InlineData("""{ "people": [ { "name": "a", "at": [0, 0, 0], "trainer": "camper_jacob", "sight": { "length": 50 } } ] }""", "$.people[0].sight.length")]
    [InlineData("""{ "people": [ { "name": "a", "at": [0, 0, 0], "radius": 3 } ] }""", "doesn't wander")]
    [InlineData("""{ "people": [ { "name": "a", "at": [0, 0, 0], "moves": "wander", "radius": 3, "bounds": [[0,0],[1,0],[1,1]] } ] }""", "not both")]
    [InlineData("""{ "people": [ { "name": "player", "at": [0, 0, 0] } ] }""", "$.people[0].name")]
    [InlineData("""{ "people": [ { "name": "a", "at": [0, 0] } ] }""", "$.people[0].at")]
    [InlineData("""{ "people": [ { "name": "a", "at": [0, 0, 0], "speed": 2 } ] }""", "no such field")]
    public void APersonsFileIsRefusedWhereItSaysWhatCantBe(string text, string says)
    {
        var e = Assert.ThrowsAny<Exception>(() => OpenPlaceFiles.Read(text, "lab_meadow.json"));
        Assert.Contains(says, e.Message);
    }

    // ------------------------------------------------------------------ the scripts

    [Fact]
    public void TheCommandsNameSomeoneOfTheMapAndAHostWithTheMapNamesNobodyElse()
    {
        var map = LabWithPeople();
        var host = new HeadlessScriptHost(new StoryState()) { OpenMap = map };
        host.WalkTo("walker", "north_meadow", false);
        host.FaceTo("walker", "camper_west");
        host.FaceTo("player", "walker");
        host.PlaceAt("camper_west", "twins_gap");
        Assert.Empty(host.Problems);
        Assert.Equal("north_meadow", host.OnMarker["walker"]);
        Assert.Equal("camper_west", host.FacingToward["walker"]);
        host.WalkTo("nobody", "north_meadow", false);
        host.FaceTo("walker", "nowhere");
        Assert.Equal(2, host.Problems.Count);
        Assert.Contains("nobody called 'nobody'", host.Problems[0]);
        Assert.Contains("no marker 'nowhere'", host.Problems[1]);

        // ScriptLibrary.OpenProblems with the people of a place: someone named, a person faced, and a name of nobody
        var library = ScriptLibrary.FromSources(("lab_meadow", """
            script Ok
              walkto walker north_meadow
              faceto player walker
              faceto walker camper_west
              placeat picnicker_east twins_gap
              end
            script Wrong
              walkto stranger north_meadow
              faceto walker stranger
              end
            """));
        var markers = map.Markers.Select(m => m.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var people = map.People.Select(p => p.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var problems = library.OpenProblems(f => f == "lab_meadow" ? markers : null, _ => null, f => f == "lab_meadow" ? people : null);
        Assert.Equal(2, problems.Count);
        Assert.Contains("'walkto' names 'stranger'", problems[0]);
        Assert.Contains("'faceto' names the marker 'stranger'", problems[1]);
    }
}
