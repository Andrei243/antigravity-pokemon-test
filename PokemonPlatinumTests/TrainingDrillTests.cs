using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// The effort drills' rules (plan 36 · TR1): three small games of half a minute at thirty frames a second, each a pure
/// function of its seed and the presses; the grades their scores earn (bands of ours: the autopilot at its usual skill
/// earns silver, and gold wants care); and the effort a grade gives, 8, 16 or 24 points, cut by the rules' limits.
/// </summary>
public class TrainingDrillTests
{
    private static Pokemon Mon(string species = "Bidoof", int level = 100) => new(PokemonDatabase.Get(species)!, level);

    private static readonly StatType[] Six = DrillKinds.Stats;

    // ------------------------------------------------------------------ which drill trains what

    [Fact]
    public void EachOfTheSixStatsHasOneDrillAndEachDrillTrainsTwo()
    {
        foreach (var stat in Six) Assert.NotNull(DrillKinds.PlanFor(stat));
        Assert.Null(DrillKinds.PlanFor(StatType.Accuracy));
        Assert.Null(DrillKinds.PlanFor(StatType.Evasion));
        foreach (var kind in Enum.GetValues<DrillKind>())
        {
            var stats = DrillKinds.StatsOf(kind);
            Assert.Equal(2, stats.Length);
            Assert.All(stats, s => Assert.Equal(kind, DrillKinds.PlanFor(s)!.Value.Kind));
            Assert.False(DrillKinds.PlanFor(stats[0])!.Value.Special);
            Assert.True(DrillKinds.PlanFor(stats[1])!.Value.Special);
        }
        Assert.Throws<ArgumentOutOfRangeException>(() => TrainingDrill.Create(StatType.Accuracy, 1));
    }

    [Fact]
    public void TheDrillMadeForAStatIsTheOneThatTrainsIt()
    {
        Assert.IsType<ThrowDrill>(TrainingDrill.Create(StatType.Attack, 1));
        Assert.IsType<ThrowDrill>(TrainingDrill.Create(StatType.SpAttack, 1));
        Assert.IsType<GuardDrill>(TrainingDrill.Create(StatType.Defense, 1));
        Assert.IsType<GuardDrill>(TrainingDrill.Create(StatType.SpDefense, 1));
        Assert.IsType<DashDrill>(TrainingDrill.Create(StatType.Speed, 1));
        Assert.IsType<DashDrill>(TrainingDrill.Create(StatType.HP, 1));
        Assert.True(TrainingDrill.Create(StatType.HP, 1).Special);
        Assert.False(TrainingDrill.Create(StatType.Speed, 1).Special);
    }

    // ------------------------------------------------------------------ repeatable, whole frames, never past the end

    [Fact]
    public void ADrillIsTheSameForASeedAndItsPresses()
    {
        foreach (var stat in Six)
            for (int seed = 1; seed <= 4; seed++)
            {
                var a = TrainingDrill.Create(stat, seed);
                var b = TrainingDrill.Create(stat, seed);
                var inputs = new List<DrillInput>();
                while (!a.IsDone)
                {
                    var input = a.Autopilot();
                    inputs.Add(input);
                    a.Step(input);
                }
                foreach (var input in inputs) b.Step(input);
                Assert.True(b.IsDone);
                Assert.Equal(a.Score, b.Score);
                Assert.Equal(a.Grade, b.Grade);
                Assert.Equal(TrainingDrill.Length, inputs.Count);
            }
    }

    [Fact]
    public void AnAutopilotPlaysTheSameGradeTwiceFromASeed()
    {
        foreach (var stat in Six)
            for (int seed = 1; seed <= 4; seed++)
                Assert.Equal(TrainingDrill.Create(stat, seed).PlayItself(), TrainingDrill.Create(stat, seed).PlayItself());
    }

    [Fact]
    public void DifferentSeedsGiveDifferentDrills()
    {
        foreach (var stat in Six)
        {
            var scores = Enumerable.Range(1, 8).Select(seed => { var d = TrainingDrill.Create(stat, seed); d.PlayItself(); return d.Score; }).Distinct().Count();
            Assert.True(scores > 1, $"{stat}: every seed gave the same score");
        }
    }

    [Fact]
    public void AdvancePlaysWholeFramesAndNeverPastTheEnd()
    {
        foreach (var stat in Six)
        {
            var drill = TrainingDrill.Create(stat, 7);
            Assert.Equal(1, drill.Advance(0.04f, DrillInput.None));
            Assert.Equal(1, drill.Advance(0.04f, DrillInput.None));
            Assert.Equal(2, drill.Frames);

            // A press given with too little time for a frame waits for the next one
            var shot = TrainingDrill.Create(StatType.Attack, 7);
            Assert.Equal(0, shot.Advance(0.01f, new DrillInput(Tap: true)));
            Assert.Equal(1, shot.Advance(0.03f, DrillInput.None));
            Assert.Single(((ThrowDrill)shot).InFlight);

            int played = drill.Advance(100f, DrillInput.None);
            Assert.Equal(TrainingDrill.Length - 2, played);
            Assert.True(drill.IsDone);
            Assert.Equal(TrainingDrill.Length, drill.Frames);
            Assert.Equal(0, drill.Advance(5f, DrillInput.None));
            drill.Step(new DrillInput(Tap: true));
            Assert.Equal(TrainingDrill.Length, drill.Frames);
            Assert.Equal(0f, drill.SecondsLeft);
        }
        Assert.Equal(900, TrainingDrill.Length);
    }

    [Fact]
    public void AnAdvanceInSinglesAndOneBigAdvanceGiveTheSameDrill()
    {
        foreach (var stat in Six)
        {
            var a = TrainingDrill.Create(stat, 3);
            var b = TrainingDrill.Create(stat, 3);
            var input = new DrillInput(Dx: 1);
            while (!a.IsDone) a.Step(input);
            b.Advance(1000f, input);
            Assert.Equal(a.Score, b.Score);
        }
    }

    // ------------------------------------------------------------------ the bands

    [Fact]
    public void TheAutopilotAtItsUsualSkillEarnsSilverAndGoldWantsCare()
    {
        foreach (var stat in Six)
        {
            // A seed's grade: silver for the first and third of each (the sprint's and the long run's second is bronze, a
            // thin one)
            Assert.Equal(DrillGrade.Silver, TrainingDrill.Create(stat, 1).PlayItself());
            Assert.Equal(DrillGrade.Silver, TrainingDrill.Create(stat, 3).PlayItself());

            var usual = Enumerable.Range(1, 20).Select(seed => TrainingDrill.Create(stat, seed).PlayItself()).ToArray();
            Assert.DoesNotContain(DrillGrade.Gold, usual);
            Assert.True(usual.Count(g => g == DrillGrade.Silver) >= 12, $"{stat}: the usual skill earned silver only {usual.Count(g => g == DrillGrade.Silver)} times in 20");

            var careful = Enumerable.Range(1, 20).Select(seed => TrainingDrill.Create(stat, seed).PlayItself(0.9f)).ToArray();
            Assert.True(careful.Count(g => g == DrillGrade.Gold) >= 17, $"{stat}: care earned gold only {careful.Count(g => g == DrillGrade.Gold)} times in 20");
            Assert.DoesNotContain(DrillGrade.Bronze, careful);

            var careless = Enumerable.Range(1, 20).Select(seed => TrainingDrill.Create(stat, seed).PlayItself(0f)).ToArray();
            Assert.DoesNotContain(DrillGrade.Gold, careless);
            Assert.True(careless.Count(g => g == DrillGrade.Bronze) >= 6, $"{stat}: carelessness earned bronze only {careless.Count(g => g == DrillGrade.Bronze)} times in 20");
        }
    }

    [Fact]
    public void ADrillLeftAloneEarnsBronzeAndNoMore()
    {
        foreach (var stat in Six)
        {
            var drill = TrainingDrill.Create(stat, 5);
            drill.Advance(100f, DrillInput.None);
            Assert.True(drill.IsDone);
            Assert.Equal(DrillGrade.Bronze, drill.Grade);
        }
    }

    [Fact]
    public void TheBandsRiseFromBronzeToSilverToGold()
    {
        foreach (var kind in Enum.GetValues<DrillKind>())
            foreach (bool special in new[] { false, true })
            {
                int silver = DrillBands.Silver(kind, special), gold = DrillBands.Gold(kind, special);
                Assert.True(0 < silver && silver < gold);
                Assert.Equal(DrillGrade.Bronze, DrillBands.GradeOf(kind, silver - 1, special));
                Assert.Equal(DrillGrade.Silver, DrillBands.GradeOf(kind, silver, special));
                Assert.Equal(DrillGrade.Silver, DrillBands.GradeOf(kind, gold - 1, special));
                Assert.Equal(DrillGrade.Gold, DrillBands.GradeOf(kind, gold, special));
            }
    }

    // ------------------------------------------------------------------ the throw

    [Fact]
    public void AShotLandsNineFramesLaterWhereTheAimWasAndScoresBySeatInTheTarget()
    {
        var drill = new ThrowDrill(StatType.Attack, false, 11);
        Assert.Equal(500, drill.AimX);
        drill.Step(new DrillInput(Tap: true));
        Assert.Single(drill.InFlight);
        Assert.Equal(9, drill.InFlight[0].LandsAt);
        Assert.Equal(500, drill.InFlight[0].X);
        // Another press within the breath lets nothing fly
        for (int i = 0; i < 8; i++) drill.Step(i == 3 ? new DrillInput(Tap: true) : DrillInput.None);
        Assert.Single(drill.InFlight);
        Assert.False(drill.CanFire);
        drill.Step(DrillInput.None); // frame 9: the shot lands
        Assert.Empty(drill.InFlight);
        Assert.Equal(drill.PointsAt(9, 500), drill.Score);
        Assert.Equal(9, drill.Last!.Value.Frame);
        Assert.Equal(500, drill.Last.Value.X);
        // Two frames on the breath is over and the next press lets one fly
        drill.Step(DrillInput.None);
        drill.Step(DrillInput.None);
        Assert.True(drill.CanFire);
    }

    [Fact]
    public void TheTargetScoresThreeInItsMiddleTwoInItsRingAndOneOnTheBoard()
    {
        var drill = new ThrowDrill(StatType.Attack, false, 2);
        foreach (int frame in new[] { 0, 100, 450, 899 })
        {
            int at = drill.TargetAt(frame), r = ThrowDrill.RadiusAt(frame);
            Assert.Equal(3, drill.PointsAt(frame, at));
            Assert.Equal(3, drill.PointsAt(frame, at + r / 3));
            Assert.Equal(2, drill.PointsAt(frame, at + r / 3 + 1));
            Assert.Equal(2, drill.PointsAt(frame, at - 2 * r / 3));
            Assert.Equal(1, drill.PointsAt(frame, at - 2 * r / 3 - 1));
            Assert.Equal(1, drill.PointsAt(frame, at + r));
            Assert.Equal(0, drill.PointsAt(frame, at + r + 1));
            Assert.InRange(at, 100, 900);
        }
        Assert.Equal(140, ThrowDrill.RadiusAt(0));
        Assert.True(ThrowDrill.RadiusAt(TrainingDrill.Length) <= 50);
    }

    [Fact]
    public void TheAimStaysOnTheWall()
    {
        var drill = new ThrowDrill(StatType.SpAttack, true, 4);
        for (int i = 0; i < 200; i++) drill.Step(new DrillInput(Dx: -1));
        Assert.Equal(0, drill.AimX);
        for (int i = 0; i < 200; i++) drill.Step(new DrillInput(Dx: 1));
        Assert.Equal(ThrowDrill.Width, drill.AimX);
    }

    // ------------------------------------------------------------------ the guard

    private static GuardDrill GuardAt(int seed = 3) => new(StatType.Defense, false, seed);

    /// <summary>Plays the guard until a frame, moving into the lane of the first pellet and pressing at <paramref name="pressAt"/> (or never).</summary>
    private static GuardDrill GuardPlayed(int pressAt, int untilFrame, bool moveIn = true, int seed = 3)
    {
        var drill = GuardAt(seed);
        var first = drill.Pellets[0];
        int lastDx = 0;
        while (drill.Frames < untilFrame)
        {
            int dx = 0;
            if (moveIn && drill.Lane != first.Lane && lastDx == 0) dx = Math.Sign(first.Lane - drill.Lane);
            lastDx = dx;
            drill.Step(new DrillInput(Tap: drill.Frames == pressAt, Dx: dx));
        }
        return drill;
    }

    [Fact]
    public void APelletCaughtOnTimeScoresTwoLateOneAndMissedStuns()
    {
        var probe = GuardAt();
        var first = probe.Pellets[0];
        int arrives = first.Arrives;

        var onTime = GuardPlayed(arrives, arrives + GuardDrill.Window + 1);
        Assert.Equal(2, onTime.Pellets[0].Caught);
        Assert.Equal(2, onTime.Score);
        Assert.Equal(0, onTime.Stuns);

        // Pressed a few frames late, or a few early, still catches it for one
        var late = GuardPlayed(arrives + 4, arrives + GuardDrill.Window + 1);
        Assert.Equal(1, late.Pellets[0].Caught);
        var early = GuardPlayed(arrives - 4, arrives + GuardDrill.Window + 1);
        Assert.Equal(1, early.Pellets[0].Caught);

        // Pressed too late for the window: missed, and stunned
        var missed = GuardPlayed(arrives + GuardDrill.Window + 3, arrives + GuardDrill.Window + 2);
        Assert.Equal(0, missed.Score);
        Assert.Equal(1, missed.Stuns);
        Assert.True(missed.Stun > 0);
    }

    [Fact]
    public void APelletInAnotherLaneIsMissedAndAStunnedGuardCanNeitherMoveNorRaise()
    {
        // The guard stays in its lane (the middle one), so the seed is one whose first pellet falls in another
        int seed = Enumerable.Range(1, 50).First(s => GuardAt(s).Pellets[0].Lane != 1);
        var drill = GuardAt(seed);
        var first = drill.Pellets[0];
        while (drill.Frames < first.Arrives + GuardDrill.Window + 1)
            drill.Step(new DrillInput(Tap: drill.Frames == first.Arrives));
        Assert.Equal(0, drill.Score);
        Assert.Equal(1, drill.Stuns);
        Assert.Equal(GuardDrill.StunFrames, drill.Stun + 0);

        int lane = drill.Lane, raised = drill.LastRaise;
        drill.Step(new DrillInput(Tap: true, Dx: 1));
        Assert.Equal(lane, drill.Lane);
        Assert.Equal(raised, drill.LastRaise);
        while (drill.Stun > 0) drill.Step(DrillInput.None);
        drill.Step(new DrillInput(Dx: 1));
        Assert.Equal(lane + 1, drill.Lane);
    }

    [Fact]
    public void TheGuardMovesOneLanePerPressOfAnArrowAndRaisesWithACoolDown()
    {
        var drill = GuardAt();
        Assert.Equal(1, drill.Lane);
        drill.Step(new DrillInput(Dx: 1));
        drill.Step(new DrillInput(Dx: 1)); // held: no second step
        Assert.Equal(2, drill.Lane);
        drill.Step(DrillInput.None);
        drill.Step(new DrillInput(Dx: 1));
        Assert.Equal(2, drill.Lane);
        drill.Step(DrillInput.None);
        drill.Step(new DrillInput(Dx: -1));
        Assert.Equal(1, drill.Lane);

        drill.Step(new DrillInput(Tap: true));
        int first = drill.LastRaise;
        Assert.True(drill.GuardUp);
        drill.Step(new DrillInput(Tap: true));
        Assert.Equal(first, drill.LastRaise);
        for (int i = 0; i < GuardDrill.Cooldown; i++) drill.Step(DrillInput.None);
        drill.Step(new DrillInput(Tap: true));
        Assert.NotEqual(first, drill.LastRaise);
    }

    [Fact]
    public void ThePelletsComeInThreeLanesFromTheSeedAlone()
    {
        var a = GuardAt(9).Pellets;
        var b = GuardAt(9).Pellets;
        Assert.Equal(a.Select(p => (p.Lane, p.Arrives)), b.Select(p => (p.Lane, p.Arrives)));
        Assert.InRange(a.Count, 36, 56);
        Assert.All(a, p => Assert.InRange(p.Lane, 0, GuardDrill.Lanes - 1));
        Assert.All(a, p => Assert.InRange(p.Arrives, 40, TrainingDrill.Length - GuardDrill.Window - 3));
        for (int i = 1; i < a.Count; i++) Assert.True(a[i].Arrives > a[i - 1].Arrives + 10);
    }

    // ------------------------------------------------------------------ the dash

    private static DashDrill Run(StatType stat, int frames, Func<DashDrill, DrillInput> input, int seed = 1)
    {
        var drill = (DashDrill)TrainingDrill.Create(stat, seed);
        while (drill.Frames < frames && !drill.IsDone) drill.Step(input(drill));
        return drill;
    }

    [Fact]
    public void PressesOnTheBeatBuildThePaceAndOnesOffItLoseIt()
    {
        var on = Run(StatType.Speed, 130, d => new DrillInput(Tap: d.Frames % d.BeatEvery == 0 && d.Frames > 0));
        Assert.True(on.Streak >= 8, $"streak {on.Streak}");
        Assert.True(on.Pace > 3.5f);

        // The same, then a press in the middle of two beats
        var wrong = Run(StatType.Speed, 140, d => new DrillInput(Tap: (d.Frames % d.BeatEvery == 0 && d.Frames > 0 && d.Frames < 130) || d.Frames == 133));
        Assert.True(wrong.Streak < on.Streak);
        Assert.Equal(0, wrong.Streak);
    }

    [Fact]
    public void AGapOfBeatsBreaksTheStreakAndNothingPressedRunsNowhere()
    {
        var gap = Run(StatType.Speed, 200, d => new DrillInput(Tap: d.Frames % d.BeatEvery == 0 && d.Frames > 0 && d.Frames < 100));
        Assert.Equal(0, gap.Streak);
        var idle = Run(StatType.Speed, TrainingDrill.Length, _ => DrillInput.None);
        Assert.Equal(0f, idle.Distance);
        Assert.Equal(0, idle.Score);
    }

    [Fact]
    public void AHurdleMetOnTheGroundIsAStumbleAndOneClearedInTheAirIsNot()
    {
        // On the beat all the way, never held: the first hurdle (600 units) trips the runner
        var runner = Run(StatType.Speed, 400, d => new DrillInput(Tap: d.Frames % d.BeatEvery == 0 && d.Frames > 0));
        Assert.True(runner.Stumbles >= 1);
        Assert.Equal(0, runner.Cleared);

        // The autopilot jumps them
        var drill = TrainingDrill.Create(StatType.Speed, 1);
        drill.PlayItself(0.9f);
        var dash = (DashDrill)drill;
        Assert.True(dash.Cleared >= 5, $"cleared {dash.Cleared}");
        Assert.True(dash.Cleared > dash.Stumbles);
    }

    [Fact]
    public void AHeldPressOfFiveFramesJumpsAndLettingGoEarlyIsAWrongPress()
    {
        var jump = Run(StatType.Speed, 6, d => new DrillInput(Tap: d.Frames == 0, Held: d.Frames < 6));
        Assert.True(jump.InAir);
        var shortPress = Run(StatType.Speed, 6, d => new DrillInput(Tap: d.Frames == 0, Held: d.Frames < 2));
        Assert.False(shortPress.InAir);
        Assert.Equal(0, shortPress.Streak);
    }

    [Fact]
    public void TheLongRunSpendsStaminaAndAScoreIsTheDistanceAndTheHurdlesCleared()
    {
        var idle = Run(StatType.HP, 100, _ => DrillInput.None);
        Assert.Equal(DashDrill.StartStamina - 100, idle.Stamina);
        var sprint = Run(StatType.Speed, 100, _ => DrillInput.None);
        Assert.Equal(DashDrill.StartStamina, sprint.Stamina);

        var done = (DashDrill)TrainingDrill.Create(StatType.HP, 2);
        done.PlayItself(0.9f);
        Assert.InRange(done.Score - 4 * done.Cleared, (int)(done.Distance * 1000 / 40000) - 1, (int)(done.Distance * 1000 / 40000) + 1);
        Assert.True(done.Stamina >= 0);
    }

    // ------------------------------------------------------------------ the effort a drill gives

    [Fact]
    public void BronzeSilverAndGoldGiveEightSixteenAndTwentyFourPoints()
    {
        Assert.Equal(8, EffortDrills.PointsFor(DrillGrade.Bronze));
        Assert.Equal(16, EffortDrills.PointsFor(DrillGrade.Silver));
        Assert.Equal(24, EffortDrills.PointsFor(DrillGrade.Gold));
        foreach (var (grade, points) in new[] { (DrillGrade.Bronze, 8), (DrillGrade.Silver, 16), (DrillGrade.Gold, 24) })
            foreach (var stat in Six)
            {
                var p = Mon();
                Assert.Equal(points, EffortDrills.Award(p, stat, grade, Ruleset.Modern));
                Assert.Equal(points, EffortRules.Get(p, stat));
                Assert.Equal(points, EffortRules.Total(p));
            }
    }

    [Fact]
    public void AwardedEffortIsReckonedAtOnce()
    {
        // 24 effort is six points of stat at level 100: ((2B + IV + EV/4) × L / 100) + 5. The nature is a neutral one: a
        // drawn nature that raises Attack makes the six points six or seven after its tenth (rounded down), by whatever the
        // game's dice had come to when the test ran (it failed so in the full suite)
        var p = new Pokemon(PokemonDatabase.Get("Bidoof")!, 100, Gender.Male, Nature.Hardy, false);
        int before = p.Attack;
        Assert.Equal(24, EffortDrills.Award(p, StatType.Attack, DrillGrade.Gold, Ruleset.Modern));
        Assert.Equal(before + 6, p.Attack);
        int hp = p.MaxHP;
        EffortDrills.Award(p, StatType.HP, DrillGrade.Gold, Ruleset.Modern);
        Assert.Equal(hp + 6, p.MaxHP);
    }

    [Fact]
    public void EffortIsCutAtTheStatsLimitAndAtFiveHundredAndTen()
    {
        var p = Mon();
        p.EvSpeed = 248;
        Assert.Equal(4, EffortDrills.Award(p, StatType.Speed, DrillGrade.Gold, Ruleset.Modern));
        Assert.Equal(252, p.EvSpeed);
        Assert.NotNull(EffortDrills.WhyNot(p, StatType.Speed, Ruleset.Modern));
        Assert.Equal(0, EffortDrills.Award(p, StatType.Speed, DrillGrade.Gold, Ruleset.Modern));
        Assert.Equal(252, p.EvSpeed);

        // Platinum's limit is a stat's 255
        Assert.Null(EffortDrills.WhyNot(p, StatType.Speed, Ruleset.Platinum));
        Assert.Equal(3, EffortDrills.Award(p, StatType.Speed, DrillGrade.Gold, Ruleset.Platinum));
        Assert.Equal(255, p.EvSpeed);

        var full = Mon();
        full.EvHP = 252; full.EvAttack = 252; full.EvDefense = 6;
        Assert.Equal(510, EffortRules.Total(full));
        Assert.NotNull(EffortDrills.WhyNot(full, StatType.SpAttack, Ruleset.Modern));
        Assert.Equal(0, EffortDrills.Award(full, StatType.SpAttack, DrillGrade.Gold, Ruleset.Modern));

        var nearly = Mon();
        nearly.EvHP = 252; nearly.EvAttack = 252; nearly.EvDefense = 2;
        Assert.Equal(4, EffortDrills.Award(nearly, StatType.SpAttack, DrillGrade.Gold, Ruleset.Modern));
        Assert.Equal(510, EffortRules.Total(nearly));
    }

    [Fact]
    public void AnEggAStatNoDrillTrainsAndShedinjasHpAreRefused()
    {
        var egg = Mon();
        egg.IsEgg = true;
        Assert.NotNull(EffortDrills.WhyNot(egg, StatType.Attack));
        Assert.Equal(0, EffortDrills.Award(egg, StatType.Attack, DrillGrade.Gold, Ruleset.Modern));
        Assert.Equal(0, egg.EvAttack);

        var p = Mon();
        Assert.NotNull(EffortDrills.WhyNot(p, StatType.Accuracy));
        Assert.Equal(0, EffortDrills.Award(p, StatType.Evasion, DrillGrade.Gold, Ruleset.Modern));

        var shedinja = Mon("Shedinja", 50);
        Assert.NotNull(EffortDrills.WhyNot(shedinja, StatType.HP));
        Assert.Equal(0, EffortDrills.Award(shedinja, StatType.HP, DrillGrade.Gold, Ruleset.Modern));
        Assert.Equal(8, EffortDrills.Award(shedinja, StatType.Attack, DrillGrade.Bronze, Ruleset.Modern));

        Assert.Null(EffortDrills.WhyNot(Mon(), StatType.Attack, Ruleset.Modern));
    }

    [Fact]
    public void PokerusAPowerItemAndTheMachoBraceAddNothingToADrill()
    {
        var infected = Mon();
        infected.Pokerus = 0x11;
        Assert.Equal(8, EffortDrills.Award(infected, StatType.Attack, DrillGrade.Bronze, Ruleset.Modern));
        Assert.Equal(8, infected.EvAttack);

        var weighted = Mon("Machop", 50);
        weighted.HeldItem = ItemDatabase.Get("Power Weight");
        Assert.Equal(8, EffortDrills.Award(weighted, StatType.HP, DrillGrade.Bronze, Ruleset.Modern));
        Assert.Equal(8, weighted.EvHP);

        var braced = Mon("Machop", 50);
        braced.HeldItem = ItemDatabase.Get("Macho Brace");
        Assert.Equal(24, EffortDrills.Award(braced, StatType.Speed, DrillGrade.Gold, Ruleset.Modern));
        Assert.Equal(24, braced.EvSpeed);
    }

    [Fact]
    public void ADrillTrainedStatTakesTheDojosEffortAndTheBattlesBothWithinTheSameLimits()
    {
        // The drill is a way beside the others: effort a battle gave is held to the same 252 and 510
        var p = Mon();
        EffortRules.Gain(p, new StatSpread { Attack = 3 }, Ruleset.Modern);
        Assert.Equal(3, p.EvAttack);
        Assert.Equal(24, EffortDrills.Award(p, StatType.Attack, DrillGrade.Gold, Ruleset.Modern));
        Assert.Equal(27, p.EvAttack);
    }
}
