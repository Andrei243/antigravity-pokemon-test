using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.OpenWorld;
using PokemonPlatinumEngine.Story;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 30 · FM12, built as plan 34 · PD2 (rulings, "The mount on the open world"): the mount's ways on plan 32's
/// capsule, each on a ground made in the test (boxes, as <see cref="OpenMovementTests"/> lays them): the trot and the
/// sprint to the micrometre and the sprint only with the dash, the hop and the high jump against ledges of 1, 2.5 and
/// 3 m, a roof stopping a jump, the glide's sink and reach and nothing without its grant, the climb only up a climbable
/// face and only with its grant, onto the top and back down to the foot, swimming only with its grant, a walker never
/// reading the mount's powers, and the rules of getting on and off. GPU-free.
/// </summary>
public class MountTests
{
    const int T = OpenClock.TicksPerSecond;
    static readonly OpenBox Floor = OpenBox.Metres("floor", -100, -1, -100, 100, 0, 100);
    static readonly OpenInput East = new(1, 0);
    static readonly OpenInput Jump = new(0, 0, Jump: true);
    const MovementGrants All = MovementGrants.Dash | MovementGrants.Swim | MovementGrants.HighJump | MovementGrants.Glide | MovementGrants.Climb;

    static OpenGround Ground(params OpenBox[] boxes) => new(null, boxes);

    static OpenBody Rider(double x = 0, double y = 0, double z = 0)
    {
        var body = new OpenBody(OpenPos.FromMetres(x, y, z));
        body.Mount();
        return body;
    }

    static void Run(OpenBody body, OpenGround ground, OpenInput input, int ticks, MovementGrants grants = MovementGrants.None)
    {
        for (int i = 0; i < ticks; i++) body.Step(ground, input, grants);
    }

    /// <summary>Steps east into a ledge with a press at the first tick, east for 0.8 s and still for a second more (the ledge ends 8 m east).</summary>
    static OpenBody JumpEastAt(OpenGround ground, MovementGrants grants)
    {
        var body = Rider();
        Run(body, ground, OpenInput.None, 2, grants);
        body.Step(ground, East with { Jump = true }, grants);
        Run(body, ground, East, T * 8 / 10, grants);
        Run(body, ground, OpenInput.None, T, grants);
        return body;
    }

    [Fact]
    public void TheMountTrotsAtSixMetresASecondAndSprintsAtElevenOnlyWithTheDash()
    {
        var ground = Ground(Floor);
        var trot = Rider();
        Run(trot, ground, East, T);
        Assert.Equal(6_000_000L, trot.Micrometres.X);

        var noDash = Rider();
        Run(noDash, ground, East with { Run = true }, T);
        Assert.Equal(6_000_000L, noDash.Micrometres.X);

        var sprint = Rider();
        Run(sprint, ground, East with { Run = true }, T, MovementGrants.Dash);
        // (11 m in 120 ticks is no whole number of micrometres a tick: 91,667 each)
        Assert.InRange(sprint.Micrometres.X, 10_999_900L, 11_000_100L);

        // A walker handed every power reads the swim alone: a run is a run, a press is nothing
        var walker = new OpenBody(new OpenPos(0, 0, 0));
        Run(walker, ground, East with { Run = true, Jump = true }, T, All);
        Assert.Equal(4_500_000L, walker.Micrometres.X);
        Assert.Equal(0L, walker.Micrometres.Y);
        Assert.Equal(OpenStance.Standing, walker.Stance);
    }

    [Theory]
    [InlineData(1.0, false, true)]
    [InlineData(1.5, false, false)]
    [InlineData(2.5, false, false)]
    [InlineData(2.5, true, true)]
    [InlineData(3.0, true, false)]
    public void TheHopClearsAMetreAndTheHighJumpTwoAndAHalfButNoMore(double ledge, bool high, bool clears)
    {
        // A ledge 1.2 m east of the body, its face to the west
        var ground = Ground(Floor, OpenBox.Metres("ledge", 1.2, 0, -5, 8, ledge, 5));
        var body = JumpEastAt(ground, high ? MovementGrants.HighJump : MovementGrants.None);
        Assert.Equal(OpenStance.Standing, body.Stance);
        if (clears)
        {
            Assert.Equal(OpenPos.Millimetres(ledge), body.Pos.Y);
            Assert.True(body.Pos.X > 1_500, $"on the ledge at {body.Pos}");
        }
        else
        {
            Assert.Equal(0, body.Pos.Y);
            Assert.True(body.Pos.X < 1_200 - 300, $"held at the face at {body.Pos}");
        }
    }

    [Fact]
    public void TheJumpRisesAsHighAsItsHeightSaysAndARoofStopsIt()
    {
        var ground = Ground(Floor);
        var body = Rider();
        Run(body, ground, OpenInput.None, 2);
        body.Step(ground, Jump, MovementGrants.HighJump);
        int highest = 0;
        for (int i = 0; i < 2 * T; i++)
        {
            body.Step(ground, OpenInput.None, MovementGrants.HighJump);
            highest = Math.Max(highest, body.Pos.Y);
        }
        Assert.InRange(highest, 2_350, 2_450);
        Assert.Equal(OpenStance.Standing, body.Stance);

        // Under a roof 3 m up (its underside), the head stops at it: the feet rise no higher than 3 − 1.6 m
        var roofed = Ground(Floor, OpenBox.Metres("roof", -5, 3, -5, 5, 3.5, 5));
        var under = Rider();
        Run(under, roofed, OpenInput.None, 2);
        under.Step(roofed, Jump, MovementGrants.HighJump);
        int top = 0;
        for (int i = 0; i < 2 * T; i++)
        {
            under.Step(roofed, OpenInput.None, MovementGrants.HighJump);
            top = Math.Max(top, under.Pos.Y);
        }
        Assert.InRange(top, 1_300, 1_400);
        Assert.Equal((0, OpenStance.Standing), (under.Pos.Y, under.Stance));
    }

    [Fact]
    public void AGlideSinksTwoMetresASecondAtTheTrotsSpeedAndOnlyWithItsGrant()
    {
        // A plateau 20 m high whose east edge is at x 2; the ground under it at nought
        var ground = Ground(Floor, OpenBox.Metres("plateau", -20, 0, -20, 2, 20, 20));
        OpenBody Off(MovementGrants grants)
        {
            var body = Rider(0, 20, 0);
            Run(body, ground, OpenInput.None, 2, grants);
            // East off the edge: once falling, a press
            while (body.Stance == OpenStance.Standing) body.Step(ground, East, grants);
            body.Step(ground, East with { Jump = true }, grants);
            return body;
        }

        var glider = Off(MovementGrants.Glide);
        Assert.Equal(OpenStance.Gliding, glider.Stance);
        int y0 = glider.Pos.Y, x0 = glider.Pos.X;
        Run(glider, ground, East, T, MovementGrants.Glide);
        Assert.Equal(OpenStance.Gliding, glider.Stance);
        Assert.InRange(y0 - glider.Pos.Y, 1_990, 2_010);
        Assert.InRange(glider.Pos.X - x0, 5_990, 6_010);
        // On until the ground, about ten seconds from 20 m, sixty metres out; never a hard landing
        int ticks = 0;
        bool hard = false;
        while (glider.Stance == OpenStance.Gliding && ticks++ < 20 * T)
        {
            glider.Step(ground, East, MovementGrants.Glide);
            hard |= glider.HardLanding;
        }
        Assert.Equal((OpenStance.Standing, 0, false), (glider.Stance, glider.Pos.Y, hard));
        Assert.InRange(glider.Pos.X, 58_000, 66_000);

        // Without the grant the press does nothing: it falls, and hard
        var faller = Off(MovementGrants.None);
        Assert.Equal(OpenStance.Falling, faller.Stance);
        hard = false;
        for (int i = 0; i < 3 * T && faller.Stance != OpenStance.Standing; i++)
        {
            faller.Step(ground, East, MovementGrants.None);
            hard |= faller.HardLanding;
        }
        Assert.True(hard);
        Assert.InRange(faller.Pos.X, 2_000, 20_000);

        // A second press lets go: the body falls from where it was
        var letGo = Off(MovementGrants.Glide);
        letGo.Step(ground, East with { Jump = true }, MovementGrants.Glide);
        Assert.Equal(OpenStance.Falling, letGo.Stance);
    }

    [Fact]
    public void AClimbableFaceIsClimbedOntoItsTopAndBackDownOnlyWithTheGrant()
    {
        // A cliff 6 m high whose west face is at x 1
        static OpenBox Cliff(bool climbable) => OpenBox.Metres("cliff", 1, 0, -10, 10, 6, 10) with { Climbable = climbable };
        var climbable = Ground(Floor, Cliff(true));

        var body = Rider();
        Run(body, climbable, East, T, MovementGrants.Climb);
        Assert.Equal(OpenStance.Climbing, body.Stance);
        Assert.Equal((-1.0, 0.0), body.Cling);
        // Up at 1.5 m a second: after two more seconds three metres up the face, held to it
        int y0 = body.Pos.Y;
        Run(body, climbable, East, 2 * T, MovementGrants.Climb);
        Assert.Equal(OpenStance.Climbing, body.Stance);
        Assert.InRange(body.Pos.Y - y0, 2_950, 3_050);
        Assert.InRange(body.Pos.X, 1_000 - 400, 1_000 - 300);
        // On to the top, standing on it
        for (int i = 0; i < 4 * T && body.Stance == OpenStance.Climbing; i++) body.Step(climbable, East, MovementGrants.Climb);
        Assert.Equal((OpenStance.Standing, 6_000), (body.Stance, body.Pos.Y));
        Assert.True(body.Pos.X > 1_000);

        // Down again: back to the face's top and down it to the foot
        var down = Rider();
        Run(down, climbable, East, T, MovementGrants.Climb);
        Run(down, climbable, East, T, MovementGrants.Climb);
        Assert.Equal(OpenStance.Climbing, down.Stance);
        Assert.True(down.Pos.Y > 0);
        for (int i = 0; i < 3 * T && down.Stance == OpenStance.Climbing; i++) down.Step(climbable, new OpenInput(-1, 0), MovementGrants.Climb);
        Assert.Equal((OpenStance.Standing, 0), (down.Stance, down.Pos.Y));

        // Sideways along it, at its pace
        var along = Rider();
        Run(along, climbable, East, T, MovementGrants.Climb);
        int z0 = along.Pos.Z, y1 = along.Pos.Y;
        Run(along, climbable, new OpenInput(0, 1), T, MovementGrants.Climb);
        Assert.Equal(OpenStance.Climbing, along.Stance);
        Assert.InRange(along.Pos.Z - z0, 1_450, 1_550);
        Assert.InRange(along.Pos.Y - y1, -5, 5);

        // A press lets go
        along.Step(climbable, new OpenInput(0, 0, Jump: true), MovementGrants.Climb);
        Assert.Equal(OpenStance.Falling, along.Stance);

        // Without the grant, or up a face that isn't climbable, the body stays at its foot
        foreach (var (ground, grants) in new[] { (climbable, MovementGrants.None), (Ground(Floor, Cliff(false)), MovementGrants.Climb) })
        {
            var stuck = Rider();
            Run(stuck, ground, East, 3 * T, grants);
            Assert.Equal((OpenStance.Standing, 0), (stuck.Stance, stuck.Pos.Y));
            Assert.InRange(stuck.Pos.X, 1_000 - 360, 1_000 - 340);
        }
    }

    [Fact]
    public void AFaceOfTheBakesClimbableRockIsClimbableWhereTwoCornersArePainted()
    {
        // A wall rising from x 4 to x 4.2 by 8 m: its corners painted in turn
        var chunk = OpenMovementTests.Profile((0, 0), (4, 0), (4.2, 8), (10, 8));
        var ground = new OpenGround((x, z) => x == 0 && z == 0 ? chunk : null);
        var near = new List<int>();
        ground.Near(4.1, 10, 1, near);
        var steep = near.Where(i => !ground[i].Walkable && ground[i].Normal.Y > -0.3).ToList();
        Assert.NotEmpty(steep);
        Assert.All(steep, i => Assert.False(ground.Climbable(i)));

        // One corner of three painted is not enough (a strip's triangles are its corners 4, 5, 6 and 4, 6, 7)
        chunk.Covers[5] = OpenCover.ClimbableRock;
        var one = new OpenGround((x, z) => x == 0 && z == 0 ? chunk : null);
        one.Near(4.1, 10, 1, near);
        Assert.All(near.Where(i => !one[i].Walkable && one[i].Normal.Y > -0.3), i => Assert.False(one.Climbable(i)));

        // The wall's strip is vertices 4 to 7: paint them, and the wall is climbable
        for (int v = 4; v < 8; v++) chunk.Covers[v] = OpenCover.ClimbableRock;
        var painted = new OpenGround((x, z) => x == 0 && z == 0 ? chunk : null);
        painted.Near(4.1, 10, 1, near);
        Assert.All(near.Where(i => !painted[i].Walkable && painted[i].Normal.Y > -0.3), i => Assert.True(painted.Climbable(i)));

        var body = Rider(3, 0, 10);
        Run(body, painted, East, T, MovementGrants.Climb);
        Assert.Equal(OpenStance.Climbing, body.Stance);
        for (int i = 0; i < 10 * T && body.Stance == OpenStance.Climbing; i++) body.Step(painted, East, MovementGrants.Climb);
        Assert.Equal((OpenStance.Standing, 8_000), (body.Stance, body.Pos.Y));
    }

    [Fact]
    public void TheMountSwimsOnlyWithItsGrantAndAtItsOwnPace()
    {
        // Deep water east of x 2, its surface over the bank's top (the ground under it 3 m down)
        var ground = new OpenGround(null, new[] { OpenBox.Metres("bank", -40, -1, -20, 2, -0.1, 20), OpenBox.Metres("bed", 2, -4, -20, 40, -3, 20) },
            new[] { OpenWaterBox.Metres("lake", 2, -20, 40, 20, 0) });
        var refused = Rider();
        Run(refused, ground, East, 2 * T);
        Assert.Equal(OpenStance.Standing, refused.Stance);
        Assert.True(refused.StoppedAtWater || refused.Pos.X <= 2_000);

        var swimmer = Rider();
        Run(swimmer, ground, East, T, MovementGrants.Swim);
        int x = 0;
        for (int i = 0; i < 2 * T; i++)
        {
            swimmer.Step(ground, East, MovementGrants.Swim);
            if (i == T - 1) x = swimmer.Pos.X;
        }
        Assert.Equal(OpenStance.Swimming, swimmer.Stance);
        Assert.InRange(swimmer.Pos.X - x, 3_990, 4_010);
    }

    [Fact]
    public void TheSameTicksRideToTheSameMicrometre()
    {
        var ground = Ground(Floor, OpenBox.Metres("ledge", 3, 0, -5, 8, 2.5, 5) with { Climbable = true });
        (long, long, long) Ride()
        {
            var body = Rider();
            for (int i = 0; i < 4 * T; i++)
                body.Step(ground, new OpenInput(1, Math.Sin(i * 0.05) * 0.4, Run: i % 50 < 25, Jump: i % 97 == 0), All);
            return body.Micrometres;
        }
        Assert.Equal(Ride(), Ride());
    }

    [Fact]
    public void TheRulesSayWhyNotInTheirOrderAndTheGrantsAreTheStorysFlags()
    {
        var yes = new MountAsk(true, true, MountRules.Koraidon, false, false, OpenStance.Standing);
        Assert.Equal(MountRefusal.None, MountRules.WhyNot(yes));
        Assert.Equal(MountRefusal.NotHere, MountRules.WhyNot(yes with { ModeHere = false, InOpenField = false, Mount = 0 }));
        Assert.Equal(MountRefusal.OnTheGrid, MountRules.WhyNot(yes with { InOpenField = false, Mount = 0 }));
        Assert.Equal(MountRefusal.NoMount, MountRules.WhyNot(yes with { Mount = 0, ScriptRunning = true }));
        Assert.Equal(MountRefusal.NoMount, MountRules.WhyNot(yes with { Mount = 3 }));
        Assert.Equal(MountRefusal.Scripted, MountRules.WhyNot(yes with { ScriptRunning = true, WithCompany = true }));
        Assert.Equal(MountRefusal.Company, MountRules.WhyNot(yes with { WithCompany = true, Stance = OpenStance.Falling }));
        foreach (var stance in new[] { OpenStance.Falling, OpenStance.Swimming, OpenStance.Gliding, OpenStance.Climbing })
        {
            Assert.Equal(MountRefusal.NotOnTheGround, MountRules.WhyNot(yes with { Stance = stance }));
            Assert.Equal(MountRefusal.NotOnTheGround, MountRules.WhyNotOff(stance));
        }
        Assert.Equal(MountRefusal.None, MountRules.WhyNotOff(OpenStance.Standing));

        var story = new StoryState();
        Assert.Equal(MovementGrants.None, MountRules.Grants(story));
        // In the Titans' order, each its own flag
        var expected = MovementGrants.None;
        foreach (var (power, flag) in MountRules.Powers)
        {
            story.Set(flag);
            expected |= power;
            Assert.Equal(expected, MountRules.Grants(story));
        }
        Assert.Equal(new[] { MovementGrants.Dash, MovementGrants.Swim, MovementGrants.HighJump, MovementGrants.Glide, MovementGrants.Climb },
            MountRules.Powers.Select(p => p.Power));

        // The forms the rig draws, each a form of the data (plan 30 · FM13: ridden or not; MountRigTests holds which)
        foreach (int mount in new[] { MountRules.Koraidon, MountRules.Miraidon })
            foreach (var stance in Enum.GetValues<OpenStance>())
                foreach (bool ridden in new[] { false, true })
                {
                    string form = MountRules.FormFor(mount, stance, ridden)!;
                    Assert.StartsWith(MountRules.SpeciesOf(mount)!, form);
                    Assert.Equal(MountRules.SpeciesOf(mount), PokemonPlatinumEngine.Data.PokemonDatabase.SpeciesOfForm(form)?.Name);
                }
        Assert.Null(MountRules.FormFor(0, OpenStance.Standing, false));
    }

    [Fact]
    public void OnTheNavigationMeshTheClimbAndTheGlideAreAllowedOnlyOnceGranted()
    {
        Assert.Equal(OpenNavFlags.None, OpenNavMesh.Allowed(MovementGrants.Dash | MovementGrants.HighJump) & (OpenNavFlags.Climb | OpenNavFlags.Glide | OpenNavFlags.Swim));
        Assert.True(OpenNavMesh.Allowed(MovementGrants.Climb).HasFlag(OpenNavFlags.Climb));
        Assert.True(OpenNavMesh.Allowed(MovementGrants.Glide).HasFlag(OpenNavFlags.Glide));
    }

    [Fact]
    public void GettingOffInTheAirFallsOnFootAndAWalkerHandedRideGrantsReadsNone()
    {
        var map = new OpenMap("MountTestField", "OpenTest");
        map.Add(Floor);
        var player = new OpenPlayer(new OpenPos(0, 0, 0), 0) { RideGrants = All };
        player.Step(map, OpenInput.None, MovementGrants.None);
        player.Step(map, Jump, MovementGrants.None);
        Assert.Equal((OpenStance.Standing, 0), (player.Body.Stance, player.Pos.Y));

        player.Body.Mount();
        player.Step(map, Jump, MovementGrants.None);
        Assert.Equal(OpenStance.Falling, player.Body.Stance);
        for (int i = 0; i < T / 2; i++) player.Step(map, OpenInput.None, MovementGrants.None);
        player.Step(map, Jump, MovementGrants.None);
        Assert.Equal(OpenStance.Gliding, player.Body.Stance);
        player.Body.Dismount();
        Assert.Equal((false, OpenStance.Falling), (player.Body.Mounted, player.Body.Stance));
    }
}
