using System;
using System.Collections.Generic;
using System.Linq;
using OpenWorldBake;
using PokemonPlatinumEngine.OpenWorld;
using Xunit;
using OpenBody = PokemonPlatinumEngine.OpenWorld.OpenBody;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 34 · PD2's done-when, on the open test world baked as the game reads it (tools/OpenWorldBake/worlds/lab, its
/// mount's yard on the west meadow): the navigation mesh's links for the mount's hop, high jump, climb and glide, each
/// taken by a path only mounted and with its grant, and each ridden by the game's own capsule from where the link is
/// taken to where it lands and refused without its grant; the trot, the sprint and the swim on the lab's meadow and
/// pond. A region not ridden bakes no link of the mount's. GPU-free.
/// </summary>
public class MountLabTests
{
    const int T = OpenClock.TicksPerSecond;
    const MovementGrants Every = MovementGrants.Dash | MovementGrants.Swim | MovementGrants.HighJump | MovementGrants.Glide | MovementGrants.Climb;

    static readonly Lazy<RegionDescription> description = new(() => RegionDescription.Read(OpenWorldBakeTests.LabFolder));

    static readonly Lazy<List<BakedChunk>> level0 = new(() =>
    {
        var land = description.Value.Land();
        return RegionBake.ChunksOf(description.Value.World).Select(c => RegionBake.Finest(land, description.Value.World, c.X, c.Z)).ToList();
    });

    static readonly Lazy<(OpenNavMesh Mesh, NavReport Report)> nav = new(() =>
    {
        var report = new NavReport();
        return (NavBake.Build(level0.Value, report: report, mount: true), report);
    });

    static readonly Lazy<OpenGround> ground = new(() =>
    {
        var chunks = level0.Value.ToDictionary(c => (c.ChunkX, c.ChunkZ));
        return new OpenGround((x, z) => chunks.GetValueOrDefault((x, z)));
    });

    static OpenNavMesh Mesh => nav.Value.Mesh;
    static OpenGround Ground => ground.Value;

    /// <summary>The highest ground at x, z under <paramref name="from"/> metres.</summary>
    static OpenPos On(double x, double z, double from = 60) => OpenPos.FromMetres(x, Ground.Footing(x, z, from, -60)!.Value.Y, z);

    /// <summary>A link of the mount's yard (x -64 to -44, z -30 to -8) that carries <paramref name="flag"/>, taken from at least <paramref name="from"/> metres up.</summary>
    static OpenNavLink YardLink(OpenNavFlags flag, double from = -10) => Mesh.Links
        .Where(l => l.OffMesh && (l.Flags & flag) != 0 && l.A.MetresX is > -64 and < -44 && l.A.MetresZ is > -30 and < -8 && l.A.MetresY > from)
        .OrderBy(l => l.A.X).ThenBy(l => l.A.Z).First();

    static (double X, double Z) Way(OpenNavLink l)
    {
        double dx = l.Land.MetresX - l.A.MetresX, dz = l.Land.MetresZ - l.A.MetresZ, d = Math.Sqrt(dx * dx + dz * dz);
        return (dx / d, dz / d);
    }

    /// <summary>A body set down <paramref name="back"/> metres before a link's takeoff, mounted or not, settled.</summary>
    static OpenBody Before(OpenNavLink l, double back, bool mounted, MovementGrants grants)
    {
        var (dx, dz) = Way(l);
        var at = On(l.A.MetresX - dx * back, l.A.MetresZ - dz * back, l.A.MetresY + 1);
        var body = new OpenBody(at);
        if (mounted) body.Mount();
        for (int i = 0; i < 2; i++) body.Step(Ground, OpenInput.None, grants);
        return body;
    }

    /// <summary>Rides toward a link's landing, pressing the button on the first tick, until the feet stand on its height or the time is up; then still.</summary>
    static OpenBody JumpOnto(OpenNavLink l, double back, bool mounted, MovementGrants grants)
    {
        var body = Before(l, back, mounted, grants);
        var (dx, dz) = Way(l);
        body.Step(Ground, new OpenInput(dx, dz, Jump: true), grants);
        for (int i = 0; i < 3 * T; i++)
        {
            if (body.Stance == OpenStance.Standing && body.Pos.MetresY > l.Land.MetresY - 0.1) break;
            body.Step(Ground, new OpenInput(dx, dz), grants);
        }
        for (int i = 0; i < T / 2; i++) body.Step(Ground, OpenInput.None, grants);
        return body;
    }

    [Fact]
    public void TheYardBakesEachOfTheMountsLinksAndARegionNotRiddenBakesNone()
    {
        var report = nav.Value.Report;
        Assert.True(report.HopLinks > 0 && report.HighJumpLinks > 0 && report.ClimbLinks > 0 && report.GlideLinks > 0, report.ToString());
        foreach (var flag in new[] { OpenNavFlags.Jump, OpenNavFlags.HighJump, OpenNavFlags.Climb, OpenNavFlags.Glide })
            Assert.True(YardLink(flag).Flags.HasFlag(flag));
        // The climbs are all up the block's painted east face, onto its top
        Assert.All(Mesh.Links.Where(l => (l.Flags & OpenNavFlags.Climb) != 0), l =>
        {
            Assert.InRange(l.A.MetresX, -56, -54.5);
            Assert.InRange(l.Land.MetresY, 6.0, 6.4);
        });
        // Baked as a region with no mount, the same chunks make the mesh they always made
        var plain = NavBake.Build(level0.Value);
        Assert.DoesNotContain(plain.Links, l => (l.Flags & (OpenNavFlags.Jump | OpenNavFlags.HighJump | OpenNavFlags.Climb | OpenNavFlags.Glide)) != 0);
        Assert.Equal(Mesh.Bytes(), NavBake.Build(Enumerable.Reverse(level0.Value), mount: true).Bytes());
    }

    [Fact]
    public void APathUpTheYardTakesTheMountsLinkOnlyMountedAndWithItsGrant()
    {
        var foot = On(-54, -20);
        var cases = new[]
        {
            (To: On(-50, -15), Flag: OpenNavFlags.Jump, Grant: MovementGrants.None),
            (To: On(-50, -23.5), Flag: OpenNavFlags.HighJump, Grant: MovementGrants.HighJump),
            (To: On(-59, -20), Flag: OpenNavFlags.Climb, Grant: MovementGrants.Climb)
        };
        foreach (var (to, flag, grant) in cases)
        {
            var ridden = NavPath.Find(Mesh, foot, to, OpenNavMesh.Allowed(grant, mounted: true));
            Assert.NotNull(ridden);
            Assert.True(ridden!.Uses.HasFlag(flag), $"{flag}: {ridden.Uses}");
            // On foot, with every power the region could grant, there is no way up
            Assert.Null(NavPath.Find(Mesh, foot, to, OpenNavMesh.Allowed(Every)));
            // Mounted without the grant (the hop needs none)
            if (grant != MovementGrants.None) Assert.Null(NavPath.Find(Mesh, foot, to, OpenNavMesh.Allowed(MovementGrants.None, mounted: true)));
        }
        // Off the block's top: a glide goes further than any drop, so a path far out is shorter with it
        var top = On(-59, -20);
        var far = On(-44, -20);
        var glided = NavPath.Find(Mesh, top, far, OpenNavMesh.Allowed(MovementGrants.Glide, mounted: true))!;
        var dropped = NavPath.Find(Mesh, top, far, OpenNavMesh.Allowed(MovementGrants.None, mounted: true))!;
        Assert.True(glided.Uses.HasFlag(OpenNavFlags.Glide), glided.Uses.ToString());
        Assert.False(dropped.Uses.HasFlag(OpenNavFlags.Glide));
    }

    [Fact]
    public void TheHopIsTheMountsAndAWalkerStaysUnderTheStep()
    {
        var hop = YardLink(OpenNavFlags.Jump);
        var rider = JumpOnto(hop, 2.2, mounted: true, MovementGrants.None);
        Assert.Equal(OpenStance.Standing, rider.Stance);
        Assert.InRange(rider.Pos.MetresY, hop.Land.MetresY - 0.1, hop.Land.MetresY + 0.3);

        var walker = JumpOnto(hop, 2.2, mounted: false, Every);
        Assert.True(walker.Pos.MetresY < hop.Land.MetresY - 0.3, $"{walker.Pos}");
    }

    [Fact]
    public void TheHighJumpReachesTheHighStepOnlyWithItsGrant()
    {
        var high = YardLink(OpenNavFlags.HighJump);
        var granted = JumpOnto(high, 3.0, mounted: true, MovementGrants.HighJump);
        Assert.Equal(OpenStance.Standing, granted.Stance);
        Assert.InRange(granted.Pos.MetresY, high.Land.MetresY - 0.1, high.Land.MetresY + 0.3);

        var refused = JumpOnto(high, 3.0, mounted: true, MovementGrants.None);
        Assert.True(refused.Pos.MetresY < high.Land.MetresY - 1, $"{refused.Pos}");
    }

    [Fact]
    public void TheBlocksPaintedFaceIsClimbedOntoItsTopOnlyWithTheGrant()
    {
        var climb = YardLink(OpenNavFlags.Climb);
        OpenBody Up(MovementGrants grants)
        {
            var body = Before(climb, 1.0, mounted: true, grants);
            var (dx, dz) = Way(climb);
            bool clung = false;
            for (int i = 0; i < 12 * T; i++)
            {
                body.Step(Ground, new OpenInput(dx, dz), grants);
                clung |= body.Stance == OpenStance.Climbing;
                if (clung && body.Stance == OpenStance.Standing) break;
            }
            return body;
        }
        var climber = Up(MovementGrants.Climb);
        Assert.Equal(OpenStance.Standing, climber.Stance);
        Assert.InRange(climber.Pos.MetresY, climb.Land.MetresY - 0.15, climb.Land.MetresY + 0.35);

        var stuck = Up(MovementGrants.None);
        Assert.Equal(OpenStance.Standing, stuck.Stance);
        Assert.True(stuck.Pos.MetresY < climb.A.MetresY + 0.5, $"{stuck.Pos}");
    }

    [Fact]
    public void AGlideOffTheBlockLandsAtLeastAsFarAsItsLinkAndAFallWithoutItIsShortAndHard()
    {
        // The longest glide off the block's top
        var glide = Mesh.Links.Where(l => l.OffMesh && (l.Flags & OpenNavFlags.Glide) != 0 && l.A.MetresX is > -64 and < -44 && l.A.MetresZ is > -30 and < -8 && l.A.MetresY > 5)
            .OrderByDescending(l => Math.Abs(l.Land.MetresX - l.A.MetresX) + Math.Abs(l.Land.MetresZ - l.A.MetresZ)).ThenBy(l => l.A.X).ThenBy(l => l.A.Z).First();
        var (dx, dz) = Way(glide);
        double Out(OpenBody b) => (b.Pos.MetresX - glide.A.MetresX) * dx + (b.Pos.MetresZ - glide.A.MetresZ) * dz;
        double reach = Out(new OpenBody(glide.Land));
        (OpenBody Body, bool Hard) Off(MovementGrants grants)
        {
            var body = Before(glide, 0.6, mounted: true, grants);
            bool pressed = false, hard = false;
            for (int i = 0; i < 15 * T; i++)
            {
                bool press = !pressed && body.Stance == OpenStance.Falling;
                pressed |= press;
                body.Step(Ground, new OpenInput(dx, dz, Jump: press), grants);
                hard |= body.HardLanding;
                if (pressed && body.Stance == OpenStance.Standing) break;
            }
            return (body, hard);
        }
        var (glider, softly) = Off(MovementGrants.Glide);
        Assert.False(softly);
        Assert.Equal(OpenStance.Standing, glider.Stance);
        Assert.True(Out(glider) >= reach - 0.5, $"{Out(glider):0.0} of {reach:0.0}");

        var (faller, hard) = Off(MovementGrants.None);
        Assert.True(hard);
        Assert.True(Out(faller) < reach - 3, $"{Out(faller):0.0} of {reach:0.0}");
    }

    [Fact]
    public void TheMountTrotsAndSprintsOverTheMeadowAndSwimsThePondOnlyWithItsGrant()
    {
        double Across(MovementGrants grants, bool run)
        {
            var body = new OpenBody(On(-10, -56));
            body.Mount();
            for (int i = 0; i < 2; i++) body.Step(Ground, OpenInput.None, grants);
            double x0 = body.Pos.MetresX;
            for (int i = 0; i < T; i++) body.Step(Ground, new OpenInput(1, 0, Run: run), grants);
            return body.Pos.MetresX - x0;
        }
        Assert.InRange(Across(MovementGrants.None, run: false), 5.6, 6.05);
        Assert.InRange(Across(MovementGrants.None, run: true), 5.6, 6.05);
        Assert.InRange(Across(MovementGrants.Dash, run: true), 10.3, 11.05);

        (bool Swam, double Z) IntoThePond(MovementGrants grants)
        {
            var body = new OpenBody(On(40, -23));
            body.Mount();
            bool swam = false;
            for (int i = 0; i < 4 * T; i++)
            {
                body.Step(Ground, new OpenInput(0, -1), grants);
                swam |= body.Stance == OpenStance.Swimming;
            }
            return (swam, body.Pos.MetresZ);
        }
        var swimmer = IntoThePond(MovementGrants.Swim);
        Assert.True(swimmer.Swam && swimmer.Z < -32, $"{swimmer}");
        var refused = IntoThePond(MovementGrants.None);
        Assert.False(refused.Swam);
        Assert.True(refused.Z > swimmer.Z + 3, $"{refused} and {swimmer}");
    }
}
