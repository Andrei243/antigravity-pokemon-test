using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Models;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// The Pokéathlon's second batch (plan 15 · E7): Pennant Capture, Snow Throw, Disc Catch and Lamp Jump. That each is a
/// pure function of its seed and the player's inputs, and takes no more than its length, is held for every kind by
/// <see cref="AthlonTests"/>' theories; these hold each event's own rules, with our own numbers worked out in comments.
/// </summary>
public class AthlonBatch2Tests
{
    private static Athlete A(int speed, int power, int skill, int stamina, int jump, string species = "Pikachu") =>
        new(species, new AthleteStats(speed, power, skill, stamina, jump));

    private static IReadOnlyList<AthlonTeam> Teams(Athlete mine, float knack = 0.7f) =>
        Enumerable.Range(0, AthlonRun.TeamCount)
            .Select(t => new AthlonTeam(t == 0 ? "" : AthlonRun.RivalNames[t], t == 0, new[] { t == 0 ? mine : A(3, 3, 3, 3, 3, "Bidoof"), t == 0 ? mine : A(3, 3, 3, 3, 3, "Bidoof"), t == 0 ? mine : A(3, 3, 3, 3, 3, "Bidoof") }, knack))
            .ToList();

    [Fact]
    public void TheSecondBatchIsMadeByKindAndIsAtTheEnumsEnd()
    {
        var teams = Teams(A(3, 3, 3, 3, 3));
        Assert.IsType<PennantCapture>(AthlonEvent.Create(AthlonEventKind.PennantCapture, teams, 1));
        Assert.IsType<SnowThrow>(AthlonEvent.Create(AthlonEventKind.SnowThrow, teams, 1));
        Assert.IsType<DiscCatch>(AthlonEvent.Create(AthlonEventKind.DiscCatch, teams, 1));
        Assert.IsType<LampJump>(AthlonEvent.Create(AthlonEventKind.LampJump, teams, 1));
        Assert.Equal(3, (int)AthlonEventKind.PennantCapture);
        Assert.Null(AthlonEventsBatch2.Create(AthlonEventKind.HurdleDash, teams, 1));
    }

    // ------------------------------------------------------------------ Pennant Capture

    [Fact]
    public void ARunnerTakesThePennantsItReaches()
    {
        var ev = (PennantCapture)AthlonEvent.Create(AthlonEventKind.PennantCapture, Teams(A(5, 1, 1, 5, 1)), 7);
        var me = ev.Chasers[0];
        // Run to the nearest pennant for ten seconds: a runner of Speed 5 covers 2.45 a frame, the field 240 wide
        for (int f = 0; f < 300; f++)
        {
            var goal = ev.Pennants.OrderBy(p => System.Numerics.Vector2.Distance(p, me.At)).FirstOrDefault();
            var (dx, dy) = AthlonEventsBatch2.ArrowsToward(me.At, goal);
            ev.Step(new AthlonInput(Dx: dx, Dy: dy));
        }
        Assert.True(me.Flags >= 2, $"{me.Flags} pennants");
        Assert.Equal(3 * me.Flags, ev.Points(0));
        Assert.True(ev.Pennants.Count <= PennantCapture.MostPennants);
    }

    [Fact]
    public void RunningTiresARunnerAndTheNextOneIsFresh()
    {
        // Stamina 1: six of breath a frame (7 - 1), so 600 is spent in 100 frames of running; tired, it goes at half speed
        var ev = (PennantCapture)AthlonEvent.Create(AthlonEventKind.PennantCapture, Teams(A(3, 1, 1, 1, 1)), 7);
        var me = ev.Chasers[0];
        int f = 0;
        for (; f < 200 && !ev.IsTired(0); f++) ev.Step(new AthlonInput(Dx: f / 40 % 2 == 0 ? 1 : -1));
        Assert.Equal(100, f);
        ev.Step(new AthlonInput(Switch: 1));
        Assert.Equal(1, me.Active);
        Assert.False(ev.IsTired(0));
    }

    // ------------------------------------------------------------------ Snow Throw

    [Fact]
    public void TheArrowsPickWhomToThrowAt()
    {
        // From the bottom: left is the left team, up the top, right the right; from the top, down is the player
        Assert.Equal(1, SnowThrow.TargetFor(0, -1, 0));
        Assert.Equal(2, SnowThrow.TargetFor(0, 0, -1));
        Assert.Equal(3, SnowThrow.TargetFor(0, 1, 0));
        Assert.Equal(0, SnowThrow.TargetFor(2, 0, 1));
        Assert.Equal(-1, SnowThrow.TargetFor(0, 0, 0));
    }

    [Fact]
    public void AThrowFliesForFramesByPowerAndTheNextWaitsBySkill()
    {
        var ev = (SnowThrow)AthlonEvent.Create(AthlonEventKind.SnowThrow, Teams(A(3, 5, 5, 3, 3)), 2);
        var me = ev.Throwers[0];
        ev.Step(new AthlonInput(Dy: -1, Tap: true));
        Assert.Equal(2, me.Target);
        Assert.Equal(1, me.Thrown);
        // Power 5: 30 - 15 = 15 frames in the air; Skill 5: 40 - 20 = 20 frames to make the next
        var ball = ev.Balls.Single(b => b.From == 0);
        Assert.Equal(15, ball.Flight);
        Assert.Equal(20, me.Reload);
        ev.Step(new AthlonInput(Tap: true));
        Assert.Equal(1, me.Thrown);
    }

    [Fact]
    public void KeepingTheButtonDownDucksEveryBall()
    {
        var ev = (SnowThrow)AthlonEvent.Create(AthlonEventKind.SnowThrow, Teams(A(3, 3, 3, 3, 3)), 5);
        while (!ev.IsDone) ev.Step(new AthlonInput(Held: true));
        var me = ev.Throwers[0];
        Assert.Equal(0, me.Taken);
        Assert.Equal(0, me.Thrown);
        Assert.True(me.Ducked > 0, "no ball came the player's way");
        Assert.Equal(me.Ducked, ev.Points(0));
    }

    // ------------------------------------------------------------------ Disc Catch

    [Fact]
    public void EveryLaneGetsTheSameDiscsAndEachEndsCaughtOrMissed()
    {
        var ev = (DiscCatch)AthlonEvent.Create(AthlonEventKind.DiscCatch, Teams(A(4, 3, 4, 4, 4)), 11);
        Assert.True(ev.Discs.Count >= 30);
        Assert.All(ev.Discs, d => Assert.InRange(d.LandX, DiscCatch.LaneEdge, DiscCatch.LaneWidth - DiscCatch.LaneEdge));
        while (!ev.IsDone) ev.Step(ev.Autopilot(1f));
        // By the end every disc thrown more than its fall ago has ended in every lane, one way or the other
        int ended = ev.Discs.Count(d => d.ThrownAt + d.Fall < ev.Frames);
        for (int t = 0; t < AthlonRun.TeamCount; t++)
        {
            var c = ev.Catchers[t];
            Assert.True(c.Caught + c.Missed >= ended, $"team {t}: {c.Caught} + {c.Missed} of {ended}");
            Assert.True(c.CaughtInAir <= c.Caught);
        }
    }

    [Fact]
    public void ACatcherWhoRunsAndJumpsCatchesMoreThanOneWhoStands()
    {
        var teams = Teams(A(4, 3, 4, 4, 4));
        var still = (DiscCatch)AthlonEvent.Create(AthlonEventKind.DiscCatch, teams, 11);
        while (!still.IsDone) still.Step(AthlonInput.None);
        var playing = (DiscCatch)AthlonEvent.Create(AthlonEventKind.DiscCatch, teams, 11);
        while (!playing.IsDone) playing.Step(playing.Autopilot(1f));
        Assert.True(playing.Points(0) > still.Points(0), $"{playing.Points(0)} against {still.Points(0)}");
    }

    // ------------------------------------------------------------------ Lamp Jump

    private static LampJump Bounce(int pressBefore, int seconds = 20)
    {
        var ev = (LampJump)AthlonEvent.Create(AthlonEventKind.LampJump, Teams(A(3, 5, 3, 3, 5)), 3);
        var me = ev.Jumpers[0];
        for (int f = 0; f < seconds * AthlonEvent.FramesPerSecond; f++)
        {
            bool press = pressBefore >= 0 && me.V <= 0f && LampJump.FramesToLand(me.H, me.V) - 1 == pressBefore;
            ev.Step(new AthlonInput(Tap: press));
        }
        return ev;
    }

    [Fact]
    public void APressJustBeforeLandingSpringsHigherAndOneTooSoonDoesNot()
    {
        // Skill 3: five frames of grace. Pressed on the last frame, every bounce is good and climbs to Jump 5's 19 a
        // frame (a top of about 19 x 19 = 361, past all ten lamps 32 apart); with no press it stays at the first, 6 a
        // frame (a top of 33: one lamp)
        var good = Bounce(0).Jumpers[0];
        Assert.Equal(good.Bounces, good.Good);
        Assert.Equal(LampJump.Lamps, good.BestLamps);
        var none = Bounce(-1).Jumpers[0];
        Assert.Equal(0, none.Good);
        Assert.Equal(1, none.BestLamps);
        Assert.True(good.Lit > none.Lit);
        // Seven frames before landing is past the grace of five: no spring
        var soon = Bounce(LampJump.Grace(new AthleteStats(3, 5, 3, 3, 5)) + 2).Jumpers[0];
        Assert.Equal(0, soon.Good);
    }

    [Fact]
    public void TheThreeTakeAThirdOfTheMinuteEach()
    {
        var ev = (LampJump)AthlonEvent.Create(AthlonEventKind.LampJump, Teams(A(3, 3, 3, 3, 3)), 3);
        Assert.Equal(0, ev.ActiveIndex);
        for (int f = 0; f < LampJump.Turn; f++) ev.Step(AthlonInput.None);
        Assert.Equal(1, ev.ActiveIndex);
        for (int f = 0; f < LampJump.Turn; f++) ev.Step(AthlonInput.None);
        Assert.Equal(2, ev.ActiveIndex);
    }
}
