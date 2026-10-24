using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// The Pokéathlon (plan 15 · E6): the athletic stats from the data, the three first events as pure functions of their
/// seed and the player's inputs at thirty frames a second, a course to its scoreboard, the records in the save and the
/// dome's clerk. The numbers are our own (docs/mechanics/rulings.md, "The Pokéathlon"), worked out in each comment.
/// </summary>
public class AthlonTests
{
    private static Athlete A(int speed, int power, int skill, int stamina, int jump, string species = "Pikachu") =>
        new(species, new AthleteStats(speed, power, skill, stamina, jump));

    private static IReadOnlyList<AthlonTeam> Teams(Athlete mine, Athlete? theirs = null)
    {
        var them = theirs ?? A(3, 3, 3, 3, 3, "Bidoof");
        return Enumerable.Range(0, AthlonRun.TeamCount)
            .Select(t => new AthlonTeam(t == 0 ? "" : AthlonRun.RivalNames[t], t == 0, new[] { t == 0 ? mine : them, t == 0 ? mine : them, t == 0 ? mine : them }, 0.7f))
            .ToList();
    }

    private static Party Team(params string[] species)
    {
        var party = new Party();
        foreach (var s in species) party.Add(new Pokemon(PokemonDatabase.Get(s)!, 20));
        return party;
    }

    // ------------------------------------------------------------------ the stats

    [Fact]
    public void EverySpeciesAndFormHasEachStatFromOneToFive()
    {
        foreach (var species in PokemonDatabase.GetAll())
        {
            foreach (var s in new[] { species }.Concat((species.Forms ?? new()).Select(species.AsForm)))
            {
                var stats = AthleteStats.Of(s);
                foreach (int v in stats.All)
                    Assert.True(v is >= AthleteStats.Least and <= AthleteStats.Most, $"{s.Name}: {stats}");
            }
        }
    }

    [Fact]
    public void TheStatsFollowTheDataTheyAreMadeOf()
    {
        // Ninjask (base Speed 160) is as fast as a Pokémon gets, Shuckle (5) as slow; Snorlax (460 kg) jumps least,
        // a light bird more; Blissey's HP and Special Defence make it a stayer
        Assert.Equal(5, AthleteStats.Of(PokemonDatabase.Get("Ninjask")!).Speed);
        Assert.Equal(1, AthleteStats.Of(PokemonDatabase.Get("Shuckle")!).Speed);
        Assert.Equal(1, AthleteStats.Of(PokemonDatabase.Get("Snorlax")!).Jump);
        Assert.True(AthleteStats.Of(PokemonDatabase.Get("Pidgey")!).Jump >= 4);
        Assert.True(AthleteStats.Of(PokemonDatabase.Get("Blissey")!).Stamina >= 4);
        // The same species gives the same stats on any machine and at any time
        Assert.Equal(AthleteStats.Of(PokemonDatabase.Get("Bidoof")!), AthleteStats.Of(PokemonDatabase.Get("Bidoof")!));
    }

    // ------------------------------------------------------------------ every event

    public static IEnumerable<object[]> Kinds() => Enum.GetValues<AthlonEventKind>().Select(k => new object[] { k });

    [Theory]
    [MemberData(nameof(Kinds))]
    public void AnEventIsTheSameForTheSameSeedAndInputs(AthlonEventKind kind)
    {
        var teams = Teams(A(4, 4, 4, 4, 4));
        string Play(int seed)
        {
            var ev = AthlonEvent.Create(kind, teams, seed);
            while (!ev.IsDone) ev.Step(ev.Autopilot());
            return string.Join(",", Enumerable.Range(0, AthlonRun.TeamCount).Select(ev.Points)) + $"@{ev.Frames}";
        }
        Assert.Equal(Play(77), Play(77));
        // ...and the rivals think on the event's own generator: another seed is another event
        Assert.Contains(Enumerable.Range(1, 6).Select(Play), s => s != Play(77));
    }

    [Theory]
    [MemberData(nameof(Kinds))]
    public void AnEventTakesNoMoreThanItsLength(AthlonEventKind kind)
    {
        var ev = AthlonEvent.Create(kind, Teams(A(1, 1, 1, 1, 1)), 3);
        // A huge stretch of time plays no frame past the event's end, and nothing more once it is over
        int played = ev.Advance(10_000f, AthlonInput.None);
        Assert.True(ev.IsDone);
        Assert.True(ev.Frames <= ev.Length);
        Assert.Equal(ev.Frames, played);
        Assert.Equal(0, ev.Advance(10f, new AthlonInput(Tap: true)));
        // A frame's worth of time is one frame, half of one is none until the other half comes
        var fresh = AthlonEvent.Create(kind, Teams(A(1, 1, 1, 1, 1)), 3);
        Assert.Equal(1, fresh.Advance(AthlonEvent.Frame * 1.01f, AthlonInput.None));
        Assert.Equal(0, fresh.Advance(AthlonEvent.Frame * 0.5f, AthlonInput.None));
        Assert.Equal(1, fresh.Advance(AthlonEvent.Frame * 0.5f, AthlonInput.None));
    }

    [Fact]
    public void APressBetweenFramesIsKeptForTheNextFrame()
    {
        // Block Smash takes a press only on a frame: a press given with too little time for a frame waits for it
        var ev = (BlockSmash)AthlonEvent.Create(AthlonEventKind.BlockSmash, Teams(A(5, 5, 1, 5, 1)), 1);
        Assert.Equal(0, ev.Advance(AthlonEvent.Frame * 0.4f, new AthlonInput(Tap: true)));
        Assert.Equal(1, ev.Advance(AthlonEvent.Frame * 0.7f, AthlonInput.None));
        Assert.Equal(1, ev.Smashers[0].Hits);
    }

    // ------------------------------------------------------------------ Hurdle Dash

    [Fact]
    public void ARunnerWhoNeverJumpsTripsAtEveryHurdle()
    {
        var ev = (HurdleDash)AthlonEvent.Create(AthlonEventKind.HurdleDash, Teams(A(3, 3, 3, 3, 3)), 9);
        while (!ev.IsDone) ev.Step(AthlonInput.None);
        var me = ev.Runners[0];
        // Every hurdle of every leg run: seven to a leg
        Assert.Equal(me.Leg * HurdleDash.HurdlesPerLeg + me.NextHurdle, me.Trips);
        Assert.True(me.Trips >= HurdleDash.HurdlesPerLeg);
    }

    [Fact]
    public void ARunnerWhoJumpsWellFinishesAndScoresByItsTime()
    {
        var ev = (HurdleDash)AthlonEvent.Create(AthlonEventKind.HurdleDash, Teams(A(5, 3, 3, 5, 5)), 9);
        while (!ev.IsDone) ev.Step(ev.Autopilot(1f));
        var me = ev.Runners[0];
        Assert.True(me.Finished);
        Assert.Equal(0, me.Trips);
        // Top speed 3 + 0.6 × 5 = 6 a frame, gathered at 0.1 + 0.04 × 5 = 0.3: 3,600 units in a little over 600 frames;
        // its points are what was left of the minute, a point for twelve frames
        Assert.InRange(me.FinishedAt, 600, 700);
        Assert.Equal((HurdleDash.Minute - me.FinishedAt) / 12, ev.Points(0));
    }

    // ------------------------------------------------------------------ Block Smash

    [Fact]
    public void ABlockSmasherHitsOnceEachSwingIsOver()
    {
        // Speed 5 swings for 8 − 5 = 3 frames, so a press every frame hits on frames 0, 4, 8 … 36: ten hits in forty
        // frames. Each is clean (pressed with no frame lost, under its Skill of 1) and fresh: 2 × 5 + 1 = 11. 110 is two
        // blocks of 40 and 30 on the third; Stamina 5 strains 12 − 10 = 2 a hit
        var ev = (BlockSmash)AthlonEvent.Create(AthlonEventKind.BlockSmash, Teams(A(5, 5, 1, 5, 1)), 1);
        for (int f = 0; f < 40; f++) ev.Step(new AthlonInput(Tap: true));
        var me = ev.Smashers[0];
        Assert.Equal(10, me.Hits);
        Assert.Equal(10, me.CleanHits);
        Assert.Equal(2, me.Broken);
        Assert.Equal(30, me.Damage);
        Assert.Equal(20, me.Fatigue[0]);
        Assert.Equal(4, ev.Points(0));
    }

    [Fact]
    public void ATiredSmasherHitsHalfAsHardAndASwitchSendsTheNextIn()
    {
        // Stamina 1 strains 10 a hit: ten hits and it is tired, and its eleventh is Power's 3 with no clean point
        // (Skill 1, pressed a frame late)
        var ev = (BlockSmash)AthlonEvent.Create(AthlonEventKind.BlockSmash, Teams(A(5, 3, 1, 1, 1)), 1);
        var me = ev.Smashers[0];
        while (me.Hits < 10) ev.Step(new AthlonInput(Tap: true));
        Assert.Equal(BlockSmash.Tired, me.Fatigue[0]);
        int before = me.Broken * BlockSmash.Toughness + me.Damage;
        while (me.Swing > 0) ev.Step(AthlonInput.None);
        ev.Step(AthlonInput.None);
        ev.Step(new AthlonInput(Tap: true));
        Assert.Equal(before + 3, me.Broken * BlockSmash.Toughness + me.Damage);

        ev.Step(new AthlonInput(Switch: 1));
        Assert.Equal(1, me.Active);
        Assert.Equal(BlockSmash.SwitchFrames, me.Switching);
        // The one benched gets its breath back a point a frame (from the frame after the switch)
        for (int f = 0; f < 20; f++) ev.Step(AthlonInput.None);
        Assert.Equal(BlockSmash.Tired - 20, me.Fatigue[0]);
    }

    // ------------------------------------------------------------------ Ring Drop

    [Fact]
    public void APusherThatWalksOffTheRingFallsAndTheNextComesOn()
    {
        var ev = (RingDrop)AthlonEvent.Create(AthlonEventKind.RingDrop, Teams(A(5, 1, 1, 5, 1)), 4);
        var me = ev.Pushers[0];
        // South from the south edge: off the platform within a second
        for (int f = 0; f < 60 && me.On; f++) ev.Step(new AthlonInput(Dy: 1));
        Assert.False(me.On);
        Assert.Equal(1, me.Falls);
        for (int f = 0; f <= RingDrop.OutFrames; f++) ev.Step(AthlonInput.None);
        Assert.True(me.On);
        Assert.Equal(1, me.Active);
        Assert.Equal(Math.Max(0, 15 * me.Knocks - 5 * me.Falls), ev.Points(0));
    }

    [Fact]
    public void ALungeGoesFurtherThanAWalkAndRestsBeforeTheNext()
    {
        // Power 5 lunges at 1.5 + 2.5 = 4 a frame on top of the walk, and Skill 5 rests 30 − 15 = 15 frames
        var ev = (RingDrop)AthlonEvent.Create(AthlonEventKind.RingDrop, Teams(A(1, 5, 5, 3, 1)), 4);
        var me = ev.Pushers[0];
        ev.Step(new AthlonInput(Tap: true, Dy: -1));
        Assert.Equal(RingDrop.LungeFrames - 1, me.Lunge);
        Assert.Equal(15, me.Rest);
        float after = me.At.Y;
        Assert.True(RingDrop.StartOf(0).Y - after > 3f);
        ev.Step(new AthlonInput(Tap: true, Dy: -1));
        Assert.Equal(14, me.Rest);
    }

    // ------------------------------------------------------------------ a course

    private static PokemonSpecies[] Three(params string[] names) => names.Select(n => PokemonDatabase.Get(n)!).ToArray();

    [Fact]
    public void TheRivalsAreTheSameForASeed()
    {
        string Draw(int seed) => string.Join(";", AthlonRun.Rivals(new Random(seed)).Select(t => t.Name + ":" + string.Join(",", t.Athletes.Select(a => a.Species))));
        Assert.Equal(Draw(12), Draw(12));
        foreach (var team in AthlonRun.Rivals(new Random(12)))
        {
            Assert.Equal(AthlonRun.TeamSize, team.Athletes.Select(a => a.Species).Distinct().Count());
            Assert.All(team.Athletes, a => Assert.False(PokemonDatabase.Get(a.Species)!.Legendary));
            Assert.InRange(team.Knack, 0.45f, 0.85f);
        }
        Assert.Equal(3, AthlonRun.Rivals(new Random(12)).Select(t => t.Name).Distinct().Count());
    }

    [Fact]
    public void ACoursePlaysItsThreeEventsToTheScoreboardAndIsRecorded()
    {
        var run = new AthlonRun(AthlonCourses.Practice, Three("Pikachu", "Machop", "Pidgey"), new Random(5));
        while (!run.IsOver)
        {
            var ev = run.StartEvent();
            while (!ev.IsDone) ev.Step(ev.Autopilot());
            run.EndEvent();
        }
        Assert.Equal(3, run.EventIndex);
        Assert.Equal(Enumerable.Range(0, 4).OrderBy(t => t), run.Ranking.OrderBy(t => t));
        Assert.Equal(run.Total(run.Ranking[0]), Enumerable.Range(0, 4).Max(run.Total));
        Assert.Equal(Enumerable.Range(0, 3).Sum(e => run.PointsOf(e, 0)), run.Earned);
        Assert.InRange(run.PlayerPlace, 1, 4);

        var records = new AthlonRecords();
        Assert.Equal(run.Earned > 0, records.Record(run.Course.Key, run.Earned, run.PlayerPlace));
        Assert.Equal(run.Earned, records.Points);
        Assert.Equal(run.Earned, records.BestOf("practice"));
        Assert.Equal(1, records.Runs);
        // A worse run keeps the best and still earns its points
        Assert.False(records.Record("practice", 0, 4));
        Assert.Equal(run.Earned, records.BestOf("practice"));
        Assert.Equal(2, records.Runs);
    }

    [Fact]
    public void TheSameCourseOnTheSameSeedEndsTheSame()
    {
        string Play()
        {
            var run = new AthlonRun(AthlonCourses.Practice, Three("Pikachu", "Machop", "Pidgey"), new Random(31));
            while (!run.IsOver)
            {
                var ev = run.StartEvent();
                while (!ev.IsDone) ev.Step(ev.Autopilot(0.6f));
                run.EndEvent();
            }
            return string.Join(",", Enumerable.Range(0, 4).Select(run.Total));
        }
        Assert.Equal(Play(), Play());
    }

    [Fact]
    public void ThePointsStopAtHeartGoldsCapAndAreSpentOnlyWhereTheyReach()
    {
        var records = new AthlonRecords { Points = AthlonRecords.MostPoints - 10 };
        records.Earn(500);
        Assert.Equal(AthlonRecords.MostPoints, records.Points);
        Assert.False(records.Spend(AthlonRecords.MostPoints + 1));
        Assert.True(records.Spend(99));
        Assert.Equal(AthlonRecords.MostPoints - 99, records.Points);
    }

    [Fact]
    public void TheRecordsGoThroughASaveAndAnOlderSaveHasNone()
    {
        var records = new AthlonRecords();
        records.Record("practice", 240, 1);
        var back = SaveManager.Read(SaveManager.Write(new SaveData { Athlon = records }))!;
        Assert.Equal(240, back.Athlon!.Points);
        Assert.Equal(240, back.Athlon.BestOf("practice"));
        Assert.Equal(1, back.Athlon.Wins);
        Assert.Null(SaveManager.Read(SaveManager.Write(new SaveData()))!.Athlon);
    }

    // ------------------------------------------------------------------ the screen

    [Fact]
    public void TheScreenRunsACourseFromItsChoiceToItsScoreboard()
    {
        var screen = new AthlonScreen();
        var records = new AthlonRecords();
        screen.Open(Team("Pikachu", "Machop", "Pidgey", "Bidoof"), records, new Random(8));
        Assert.Equal(AthlonPhase.Courses, screen.Phase);
        screen.Confirm();
        Assert.Equal(AthlonPhase.Team, screen.Phase);
        // Three chosen, the second taken back and another chosen in its place
        screen.Confirm();
        screen.Move(0, 1);
        screen.Confirm();
        screen.Confirm();
        Assert.Single(screen.Chosen);
        screen.Move(0, 1);
        screen.Confirm();
        screen.Move(0, 1);
        screen.Confirm();
        Assert.Equal(AthlonPhase.Briefing, screen.Phase);
        Assert.Equal(new[] { "Pikachu", "Pidgey", "Bidoof" }, screen.Run!.Teams[0].Athletes.Select(a => a.Species));

        for (int e = 0; e < 3; e++)
        {
            Assert.Equal(AthlonPhase.Briefing, screen.Phase);
            screen.Confirm();
            Assert.Equal(AthlonPhase.Playing, screen.Phase);
            // Nothing is played while the count goes
            screen.Advance(1f);
            Assert.Equal(0, screen.Run.Current!.Frames);
            screen.Play(int.MaxValue, ev => ev.Autopilot());
            Assert.Equal(AthlonPhase.EventPoints, screen.Phase);
            screen.Confirm();
        }
        Assert.Equal(AthlonPhase.Scoreboard, screen.Phase);
        Assert.Equal(screen.Run.PlayerPlace, screen.Outcome);
        Assert.Equal(screen.Run.Earned, records.Points);
        screen.Confirm();
        Assert.False(screen.IsActive);
    }

    [Fact]
    public void AnEggCantCompeteAndBackingOutComesToNothing()
    {
        var team = Team("Pikachu", "Machop", "Pidgey");
        team.Members[1].IsEgg = true;
        Assert.False(AthlonRun.CanEnter(team));
        var screen = new AthlonScreen();
        screen.Open(team, new AthlonRecords(), new Random(1));
        screen.Confirm();
        screen.Move(0, 1);
        screen.Confirm();
        Assert.Empty(screen.Chosen);
        screen.Cancel();
        Assert.Equal(AthlonPhase.Courses, screen.Phase);
        screen.Cancel();
        Assert.False(screen.IsActive);
        Assert.Equal(0, screen.Outcome);
    }

    // ------------------------------------------------------------------ the clerk

    [Theory]
    [InlineData(1, "First place")]
    [InlineData(3, "Well run")]
    [InlineData(0, "Come back whenever")]
    public void TheClerkRunsACourseAndSpeaksOfThePlace(int place, string said)
    {
        var host = new HeadlessScriptHost { AthlonPlace = place };
        host.Party.Add(new Pokemon(PokemonDatabase.Get("Pikachu")!, 20));
        host.Party.Add(new Pokemon(PokemonDatabase.Get("Machop")!, 20));
        host.Party.Add(new Pokemon(PokemonDatabase.Get("Pidgey")!, 20));
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find(GameEngine.AthlonDeskScript)!);
        runner.RunToEnd();
        Assert.Contains(host.Transcript, line => line.Text.Contains(said));
        Assert.Equal(place > 0 ? host.AthlonScore : 0, host.Athlon.Points);
    }

    [Fact]
    public void TheClerkTurnsAwayATeamOfFewerThanThree()
    {
        var host = new HeadlessScriptHost();
        host.Party.Add(new Pokemon(PokemonDatabase.Get("Pikachu")!, 20));
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find(GameEngine.AthlonDeskScript)!);
        runner.RunToEnd();
        Assert.Contains(host.Transcript, line => line.Text.Contains("three Pokémon"));
        Assert.DoesNotContain(host.Log, line => line.StartsWith("open Athlon"));
    }
}
