using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using Raylib_cs;

namespace PokemonPlatinumTests;

/// <summary>
/// The original's set pieces (plan 09 · L12; style guide, "Rooms", "Machines at work", and "Life", "Set pieces"):
/// what each shows as a function of the time since it began, the pieces playing as the field keeps them, the frames
/// the machines are painted in, a held face redrawn only when its frame changes, the script's <c>anim</c>,
/// <c>shake</c> and <c>flicker</c>, and the rooms that play them.
/// </summary>
public class SetPieceTests
{
    private const float Frame = 1f / 60f;

    private static Map Room(string name) => GameDataFiles.Load<MapFile>(Path.Combine(MapDatabase.Folder, name + ".json")).ToMap();

    private static Pokemon Mon(string species = "Turtwig", int level = 20) => new(PokemonDatabase.Get(species)!, level, Gender.Male, Nature.Hardy, false);

    /// <summary>Every moment of a piece a frame shows, at sixty frames a second.</summary>
    private static IEnumerable<float> Moments(float seconds)
    {
        for (int f = 0; f * Frame < seconds; f++) yield return f * Frame;
    }

    // ------------------------------------------------------------------ the machines

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(6)]
    public void AMachineTakesABallForEachPokemonAndThenLightsThem(int team)
    {
        // A ball every 0.35 s, the first as the piece begins; then all of them lit until the piece is over
        Assert.Equal(1, SetPieces.Balls(0f, team));
        for (int ball = 0; ball < team; ball++)
        {
            Assert.Equal(ball + 1, SetPieces.Balls(ball * SetPieces.BallSeconds + 0.01f, team));
            Assert.Equal(ball + 1, SetPieces.Balls((ball + 1) * SetPieces.BallSeconds - 0.01f, team));
            Assert.False(SetPieces.Lit(ball * SetPieces.BallSeconds + 0.01f, team));
        }
        float lit = team * SetPieces.BallSeconds;
        Assert.Equal(lit + SetPieces.LitSeconds, SetPieces.MachineLength(team), 4);
        Assert.Equal(SetPieces.MachineLength(team), SetPieces.Length(SetPiece.Heal, team));
        Assert.Equal(SetPieces.MachineLength(team), SetPieces.Length(SetPiece.HallOfFame, team));
        // Never more balls than the team, never fewer once they are down, and the frames go only one way
        int last = 0;
        foreach (float age in Moments(SetPieces.MachineLength(team)))
        {
            int balls = SetPieces.Balls(age, team);
            Assert.InRange(balls, Math.Max(1, last), team);
            last = balls;
            Assert.Equal(age >= lit - 1e-4f, SetPieces.Lighting(age, team));
            if (SetPieces.Lit(age, team)) Assert.Equal(team, balls);
        }
        Assert.True(SetPieces.Lit(lit + 0.01f, team));
        Assert.False(SetPieces.Lighting(SetPieces.MachineLength(team) + 0.01f, team));
    }

    [Fact]
    public void TheBallsAreLitForAsLongAsTheFanfareOnItsOwnBeat()
    {
        // The heal's fanfare is two bars of four at 116 a minute, the last beat a rest: the balls are lit while it sounds
        var heal = MusicLibrary.Get("common/fanfare_heal")!;
        Assert.Equal(60f / 116f, SetPieces.LitBeat, 5);
        Assert.InRange(SetPieces.LitSeconds, 6.9f * SetPieces.LitBeat, 7.1f * SetPieces.LitBeat);
        Assert.InRange(SetPieces.LitSeconds, heal.Duration() - 1.2, heal.Duration());
        // Lit for three quarters of each beat, plain for the last
        const int team = 2;
        float lit = team * SetPieces.BallSeconds;
        for (int beat = 0; beat < 6; beat++)
        {
            float at = lit + beat * SetPieces.LitBeat;
            Assert.True(SetPieces.Lit(at + 0.02f, team), $"beat {beat} begins lit");
            Assert.True(SetPieces.Lit(at + SetPieces.LitBeat * 0.7f, team), $"beat {beat} is still lit");
            Assert.False(SetPieces.Lit(at + SetPieces.LitBeat * 0.8f, team), $"beat {beat} ends plain");
            Assert.True(SetPieces.Lighting(at + SetPieces.LitBeat * 0.8f, team));
        }
    }

    [Fact]
    public void TheTraysFramesAreEmptyOneBallToSixAndOneToSixLit()
    {
        Assert.Equal(13, SetPieces.TrayFrames);
        Assert.Equal(0, SetPieces.TrayFrame(0, false));
        // An empty tray can't be lit
        Assert.Equal(0, SetPieces.TrayFrame(0, true));
        var seen = new HashSet<int> { 0 };
        for (int balls = 1; balls <= 6; balls++)
            foreach (bool lit in new[] { false, true })
            {
                int frame = SetPieces.TrayFrame(balls, lit);
                Assert.InRange(frame, 1, SetPieces.TrayFrames - 1);
                Assert.True(seen.Add(frame));
                Assert.Equal((balls, lit), SetPieces.OnTray(frame));
            }
        Assert.Equal(SetPieces.TrayFrames, seen.Count);
        // A team is six at the most
        Assert.Equal(SetPieces.TrayFrame(6, false), SetPieces.TrayFrame(9, false));
        Assert.Equal(6, SetPieces.Balls(100f, 9));
    }

    [Fact]
    public void ATeamOfOneLightsOneBall()
    {
        var stage = new SetPieceStage();
        float takes = stage.Play(SetPiece.Heal, 1, 10.0);
        Assert.Equal(SetPieces.BallSeconds + SetPieces.LitSeconds, takes, 4);
        Assert.Equal((1, false), stage.Machine(SetPiece.Heal, 10.0 + 0.1));
        Assert.Equal((1, true), stage.Machine(SetPiece.Heal, 10.0 + SetPieces.BallSeconds + 0.05));
        Assert.True(stage.MachineAtWork(10.0 + SetPieces.BallSeconds + 0.05));
        Assert.False(stage.MachineAtWork(10.0 + 0.1));
        // And the frame the tray is held on is the one with one ball, lit
        var (balls, lit) = stage.Machine(SetPiece.Heal, 10.0 + SetPieces.BallSeconds + 0.05);
        Assert.Equal((1, true), SetPieces.OnTray(SetPieces.TrayFrame(balls, lit)));
        // Afterwards the tray is empty again: the nurse has the ball back
        Assert.Equal((0, false), stage.Machine(SetPiece.Heal, 10.0 + takes + 0.01));
        // It is the Pokémon Center's machine, not the Hall of Fame's
        Assert.Equal((0, false), stage.Machine(SetPiece.HallOfFame, 10.0 + 0.1));
    }

    [Fact]
    public void TheHallOfFamesMachineKeepsItsBallsUntilTheRoomIsLeft()
    {
        var stage = new SetPieceStage();
        float takes = stage.Play(SetPiece.HallOfFame, 4, 0.0);
        Assert.Equal((2, false), stage.Machine(SetPiece.HallOfFame, SetPieces.BallSeconds + 0.01));
        Assert.Equal((4, true), stage.Machine(SetPiece.HallOfFame, 4 * SetPieces.BallSeconds + 0.01));
        stage.Advance(takes + 5.0);
        Assert.Equal((4, false), stage.Machine(SetPiece.HallOfFame, takes + 5.0));
        stage.Clear();
        Assert.Equal((0, false), stage.Machine(SetPiece.HallOfFame, takes + 6.0));
    }

    // ------------------------------------------------------------------ the PC

    [Fact]
    public void APcsScreenStartsComesOnAndStaysOnUntilItIsSwitchedOff()
    {
        Assert.Equal((0, 1, 2, 3), (SetPieces.PcDark, SetPieces.PcStarting, SetPieces.PcLit, SetPieces.PcFrames));
        // On: starting, then lit; off: starting again, then dark
        Assert.Equal(SetPieces.PcStarting, SetPieces.PcFrame(true, 0f));
        Assert.Equal(SetPieces.PcStarting, SetPieces.PcFrame(true, SetPieces.PcStartingSeconds - 0.01f));
        Assert.Equal(SetPieces.PcLit, SetPieces.PcFrame(true, SetPieces.PcStartingSeconds + 0.01f));
        Assert.Equal(SetPieces.PcLit, SetPieces.PcFrame(true, 1000f));
        Assert.Equal(SetPieces.PcStarting, SetPieces.PcFrame(false, 0f));
        Assert.Equal(SetPieces.PcDark, SetPieces.PcFrame(false, SetPieces.PcGoingSeconds + 0.01f));
        // The script waits a little longer than the screen takes, either way
        Assert.True(SetPieces.Length(SetPiece.PcOn, 0) >= SetPieces.PcStartingSeconds);
        Assert.True(SetPieces.Length(SetPiece.PcOff, 0) >= SetPieces.PcGoingSeconds);

        var stage = new SetPieceStage();
        Assert.Null(stage.Screen(0.0));
        // With no PC on there is nothing to switch off
        Assert.Equal(0f, stage.Play(SetPiece.PcOff, 0, 0.0));
        Assert.Equal(SetPieces.PcOnSeconds, stage.Play(SetPiece.PcOn, 0, 1.0, (12, 3)));
        Assert.Equal((12, 3, SetPieces.PcStarting), stage.Screen(1.1));
        stage.Advance(60.0);
        Assert.Equal((12, 3, SetPieces.PcLit), stage.Screen(60.0));
        Assert.Equal(SetPieces.PcOffSeconds, stage.Play(SetPiece.PcOff, 0, 61.0));
        Assert.Equal((12, 3, SetPieces.PcStarting), stage.Screen(61.1));
        Assert.Equal((12, 3, SetPieces.PcDark), stage.Screen(61.0 + SetPieces.PcGoingSeconds + 0.01));
        stage.Advance(62.0);
        Assert.Null(stage.Screen(62.0));
    }

    // ------------------------------------------------------------------ the lift

    [Theory]
    [InlineData(true, 4)]
    [InlineData(false, 4)]
    [InlineData(true, 3)]
    public void ALiftsArrowRunsTheWayItGoesOnceToEachHum(bool up, int hums)
    {
        Assert.Equal(7, SetPieces.IndicatorFrames);
        float ride = hums * SetPieces.HumSeconds;
        Assert.Equal(ride + SetPieces.ArrivalSeconds, SetPieces.ElevatorLength(hums), 4);
        int first = up ? 1 : 4;
        var rounds = new List<int>();
        int last = -1;
        foreach (float age in Moments(ride))
        {
            int frame = SetPieces.IndicatorFrame(up, age, hums);
            Assert.InRange(frame, first, first + 2);
            if (frame != last) rounds.Add(frame - first);
            last = frame;
        }
        // At its place, a texel on, two on, and again: once to each hum
        Assert.Equal(Enumerable.Range(0, hums * 3).Select(i => i % 3), rounds);
        // At rest before and after
        Assert.Equal(0, SetPieces.IndicatorFrame(up, -0.1f, hums));
        Assert.Equal(0, SetPieces.IndicatorFrame(up, ride + 0.01f, hums));
    }

    [Fact]
    public void ALiftShuddersInWholeTexelsAndIsStillOnceItHasArrived()
    {
        const int hums = 4;
        float ride = hums * SetPieces.HumSeconds;
        var thrown = Moments(SetPieces.ElevatorLength(hums) + 0.5f).Select(age => SetPieces.Shudder(age, hums)).ToList();
        Assert.All(thrown, t => Assert.InRange(t, -2, 1));
        Assert.Contains(1, thrown);
        Assert.Contains(-2, thrown);
        // A texel up while it rides, a thirtieth of a second in every four
        var riding = Moments(ride).Select(age => SetPieces.Shudder(age, hums)).ToList();
        Assert.All(riding, t => Assert.InRange(t, 0, 1));
        Assert.Equal(riding.Count / 4, riding.Count(t => t == 1), 2.0);
        // Two texels down for a fifteenth as it stops, and then nothing
        Assert.Equal(-2, SetPieces.Shudder(ride + 0.01f, hums));
        Assert.Equal(0, SetPieces.Shudder(ride + 0.1f, hums));
        Assert.Equal(0, SetPieces.Shudder(-1f, hums));

        var stage = new SetPieceStage();
        stage.Play(SetPiece.ElevatorUp, hums, 5.0);
        Assert.InRange(stage.Indicator(5.0 + 0.2), 1, 3);
        stage.Play(SetPiece.ElevatorDown, hums, 20.0);
        Assert.InRange(stage.Indicator(20.0 + 0.2), 4, 6);
        stage.Advance(20.0 + SetPieces.ElevatorLength(hums) + 0.1);
        Assert.Equal((0, 0), (stage.Indicator(30.0), stage.Shudder(30.0)));
    }

    // ------------------------------------------------------------------ a shake and a flicker

    [Theory]
    [InlineData(1)]
    [InlineData(8)]
    [InlineData(20)]
    public void AShakeThrowsSomeoneWholeTexelsToEitherSideAndLeavesThemWhereTheyStood(int times)
    {
        float length = SetPieces.ShakeLength(times);
        Assert.Equal(times * 4f / 30f, length, 4);
        var offsets = Moments(length).Select(age => SetPieces.ShakeOffset(age, times)).ToList();
        Assert.All(offsets, o => Assert.Contains(o, new[] { -SetPieces.ShakeReach, 0, SetPieces.ShakeReach }));
        // East, back, west, back, so many times over: as many throws east as west, and as many as it was told
        int Throws(int reach) => Enumerable.Range(0, offsets.Count).Count(i => offsets[i] == reach && (i == 0 || offsets[i - 1] != reach));
        Assert.Equal(times, Throws(SetPieces.ShakeReach));
        Assert.Equal(times, Throws(-SetPieces.ShakeReach));
        // Back where they began, before and after
        Assert.Equal(0, SetPieces.ShakeOffset(-0.01f, times));
        Assert.Equal(0, SetPieces.ShakeOffset(length, times));
        Assert.Equal(0, SetPieces.ShakeOffset(length + 3f, times));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(6)]
    [InlineData(7)]
    public void AFlickerChangesAsOftenAsItIsToldAndEndsSeen(int times)
    {
        float length = SetPieces.FlickerLength(times);
        Assert.Equal(times * SetPieces.FlickerSeconds, length, 4);
        // Seen before; unseen first; then seen and unseen by turns
        Assert.True(SetPieces.FlickerSeen(-0.01f, times));
        Assert.False(SetPieces.FlickerSeen(0.01f, times));
        var seen = Moments(length + 0.5f).Select(age => SetPieces.FlickerSeen(age, times)).Prepend(true).ToList();
        int changes = Enumerable.Range(1, seen.Count - 1).Count(i => seen[i] != seen[i - 1]);
        // So many changes while it plays, a fifth of a second apart; an odd count leaves them unseen at the piece's
        // end, where they are seen again, which is one change more
        Assert.Equal(times + times % 2, changes);
        Assert.True(seen[^1]);
        Assert.True(SetPieces.FlickerSeen(length, times));
        for (int change = 0; change < times; change++)
            Assert.Equal(change % 2 == 1, SetPieces.FlickerSeen(change * SetPieces.FlickerSeconds + 0.1f, times));
    }

    [Fact]
    public void TheStageShakesAndFlickersWhoeverItIsToldAndNobodyElse()
    {
        var statue = new NPC { Name = "Regigigas" };
        var other = new NPC { Name = "Onlooker" };
        var stage = new SetPieceStage();
        Assert.False(stage.AnyoneMoved);
        Assert.Equal(SetPieces.ShakeLength(8), stage.Shake(statue, 8, 100.0));
        Assert.True(stage.AnyoneMoved);
        Assert.Equal(SetPieces.ShakeReach, stage.OffsetOf(statue, 100.0 + 0.01));
        Assert.Equal(-SetPieces.ShakeReach, stage.OffsetOf(statue, 100.0 + 2.5 / 30.0));
        Assert.Equal(0, stage.OffsetOf(other, 100.0 + 0.01));
        Assert.Equal(0, stage.OffsetOf(null, 100.0 + 0.01));
        Assert.True(stage.Seen(statue, 100.0 + 0.01));
        stage.Advance(100.0 + SetPieces.ShakeLength(8) + 0.01);
        Assert.False(stage.AnyoneMoved);
        Assert.Equal(0, stage.OffsetOf(statue, 102.0));

        // Nobody is the player
        Assert.Equal(SetPieces.FlickerLength(6), stage.Flicker(null, 6, 200.0));
        Assert.False(stage.Seen(null, 200.0 + 0.05));
        Assert.True(stage.Seen(null, 200.0 + SetPieces.FlickerSeconds + 0.05));
        Assert.True(stage.Seen(statue, 200.0 + 0.05));
        stage.Clear();
        Assert.True(stage.Seen(null, 200.0 + 0.05));
        Assert.False(stage.AnyoneMoved);
    }

    // ------------------------------------------------------------------ what is heard

    [Fact]
    public void EveryPieceIsHeardAtItsMomentsAndEachCueOnce()
    {
        // A machine: a ball's sound for each ball, then the fanfare as they light
        var heal = SetPieces.Cues(SetPiece.Heal, 3).ToList();
        Assert.Equal(new[] { 0f, SetPieces.BallSeconds, 2 * SetPieces.BallSeconds }, heal.Where(c => c.Sound == SetPieces.TraySound).Select(c => c.At));
        Assert.Equal(3 * SetPieces.BallSeconds, Assert.Single(heal, c => c.Fanfare).At);
        var hall = SetPieces.Cues(SetPiece.HallOfFame, 2).ToList();
        Assert.Equal(2, hall.Count(c => c.Sound == SetPieces.SocketSound));
        Assert.Single(hall, c => c.Fanfare);
        // A PC's two sounds, a lift's hum to each round of its lights and its chime
        Assert.Equal("pc_on", Assert.Single(SetPieces.Cues(SetPiece.PcOn, 0)).Sound);
        Assert.Equal("pc_off", Assert.Single(SetPieces.Cues(SetPiece.PcOff, 0)).Sound);
        var ride = SetPieces.Cues(SetPiece.ElevatorDown, 3).ToList();
        Assert.Equal(new[] { "lift_hum", "lift_hum", "lift_hum", "lift_ding" }, ride.Select(c => c.Sound));
        Assert.Equal(3 * SetPieces.HumSeconds, ride[^1].At);
        // Every sound is in the bank, and the three that are this session's are of our own making
        foreach (var piece in Enum.GetValues<SetPiece>())
            foreach (var cue in SetPieces.Cues(piece, 4))
                if (cue.Sound != null)
                {
                    Assert.Contains(cue.Sound, SoundBank.Names);
                    Assert.NotNull(SoundBank.Get(cue.Sound));
                }
        foreach (string name in new[] { "machine_ball", "lift_hum", "lift_ding" }) Assert.Contains(name, SoundBank.Names);

        // The stage hands each cue over once, as the field's clock comes to it, with the column it sounds from
        var stage = new SetPieceStage();
        stage.Play(SetPiece.Heal, 2, 50.0, (7, 4));
        var heard = new List<(SetPieceCue Cue, int? X)>();
        for (double now = 50.0; now < 50.0 + SetPieces.MachineLength(2) + 1.0; now += Frame) stage.TakeCues(now, heard);
        Assert.Equal(new[] { SetPieces.TraySound, SetPieces.TraySound, null }, heard.Select(h => h.Cue.Sound));
        Assert.True(heard[^1].Cue.Fanfare);
        Assert.All(heard, h => Assert.Equal(7, h.X));
        // Coming to another place cuts a piece short: nothing more is heard of it
        stage.Play(SetPiece.ElevatorUp, 4, 80.0);
        heard.Clear();
        stage.TakeCues(80.0 + 0.1, heard);
        Assert.Single(heard);
        stage.Clear();
        stage.TakeCues(90.0, heard);
        Assert.Single(heard);
    }

    // ------------------------------------------------------------------ the frames as they are painted

    private static PixelCanvas Painted(int width, int height, Action<PixelCanvas> paint)
    {
        var canvas = new PixelCanvas(width, height);
        paint(canvas);
        return canvas;
    }

    private static bool Same(PixelCanvas a, PixelCanvas b)
    {
        for (int y = 0; y < a.Height; y++)
            for (int x = 0; x < a.Width; x++)
                if (!a.Get(x, y).Equals(b.Get(x, y))) return false;
        return true;
    }

    private static int Count(PixelCanvas c, Func<Color, bool> which)
    {
        int n = 0;
        for (int y = 0; y < c.Height; y++)
            for (int x = 0; x < c.Width; x++)
                if (which(c.Get(x, y))) n++;
        return n;
    }

    [Fact]
    public void TheTrayIsPaintedEmptyWithEachBallAndLit()
    {
        var frames = Enumerable.Range(0, SetPieces.TrayFrames).Select(f => Painted(28, 22, c => PropModels.PaintHealerTray(c, f))).ToList();
        // Thirteen different pictures, all of the face's size and opaque all over
        for (int a = 0; a < frames.Count; a++)
        {
            Assert.Equal(28 * 22, Count(frames[a], c => c.A == 255));
            for (int b = a + 1; b < frames.Count; b++) Assert.False(Same(frames[a], frames[b]), $"frames {a} and {b} are the same picture");
        }
        // A ball is eight texels of red: none on the empty tray, eight more with each ball, and paler when lit
        bool Red(Color c) => c is { R: 236, G: 64, B: 56 };
        bool LitRed(Color c) => c is { R: 255, G: 150, B: 130 };
        Assert.Equal(0, Count(frames[0], c => Red(c) || LitRed(c)));
        for (int balls = 1; balls <= 6; balls++)
        {
            Assert.Equal((balls * 8, 0), (Count(frames[SetPieces.TrayFrame(balls, false)], Red), Count(frames[SetPieces.TrayFrame(balls, false)], LitRed)));
            Assert.Equal((0, balls * 8), (Count(frames[SetPieces.TrayFrame(balls, true)], Red), Count(frames[SetPieces.TrayFrame(balls, true)], LitRed)));
        }
        // The rim of the tray is the same in every frame: the sheet's border round the face is the first frame's
        foreach (var frame in frames)
            for (int x = 0; x < 28; x++)
            {
                Assert.Equal(frames[0].Get(x, 0), frame.Get(x, 0));
                Assert.Equal(frames[0].Get(x, 21), frame.Get(x, 21));
            }
        // Nothing of it is white: a lit tray is pale, never blank
        Assert.All(frames, f => Assert.Equal(0, Count(f, c => c is { R: 255, G: 255, B: 255 })));

        // The console's screen has a cross while the machine works
        var rest = Painted(28, 28, c => PropModels.PaintHealerFront(c, 0));
        var work = Painted(28, 28, c => PropModels.PaintHealerFront(c, 1));
        Assert.False(Same(rest, work));
        Assert.Equal(rest.Get(0, 0), work.Get(0, 0));
    }

    [Fact]
    public void APcsScreenIsPaintedDarkStartingAndOn()
    {
        var frames = Enumerable.Range(0, SetPieces.PcFrames).Select(f => Painted(18, 16, c => PropModels.PaintMonitor(c, f))).ToList();
        Assert.False(Same(frames[0], frames[1]) || Same(frames[1], frames[2]) || Same(frames[0], frames[2]));
        // Dark: no blue of a lit screen; on: the three lines of text; starting: one bright line across the middle
        bool Line(Color c) => c is { R: 190, G: 230, B: 252 };
        Assert.Equal(0, Count(frames[SetPieces.PcDark], Line));
        Assert.Equal(14, Count(frames[SetPieces.PcStarting], Line));
        Assert.True(Count(frames[SetPieces.PcLit], Line) > 14);
        // The lamp under the screen: red, amber, green
        Assert.Equal(new Color(170, 70, 70, 255), frames[SetPieces.PcDark].Get(14, 13));
        Assert.Equal(new Color(250, 190, 70, 255), frames[SetPieces.PcStarting].Get(14, 13));
        Assert.Equal(new Color(98, 220, 120, 255), frames[SetPieces.PcLit].Get(14, 13));
        // The casing round the screen is the same in all three
        Assert.All(frames, f => Assert.Equal(frames[0].Get(0, 0), f.Get(0, 0)));
    }

    [Fact]
    public void ALiftsIndicatorIsPaintedAtRestAndWithItsArrowRunning()
    {
        var frames = Enumerable.Range(0, SetPieces.IndicatorFrames).Select(f => Painted(24, 12, c => PropModels.PaintLiftIndicator(c, f))).ToList();
        for (int a = 0; a < frames.Count; a++)
            for (int b = a + 1; b < frames.Count; b++) Assert.False(Same(frames[a], frames[b]), $"frames {a} and {b} are the same picture");
        bool Lit(Color c) => c is { R: 250, G: 190, B: 70 };
        bool Dim(Color c) => c is { R: 132, G: 100, B: 46 };
        // An arrow is eighteen texels: both dim at rest, one lit on a ride and the other still dim
        Assert.Equal((0, 36), (Count(frames[0], Lit), Count(frames[0], Dim)));
        for (int f = 1; f < frames.Count; f++) Assert.Equal((18, 18), (Count(frames[f], Lit), Count(frames[f], Dim)));
        // Up, the lit arrow is on the left and climbs a texel a step; down, it is on the right and sinks
        int Top(PixelCanvas c) => Enumerable.Range(0, 12).First(y => Enumerable.Range(0, 24).Any(x => Lit(c.Get(x, y))));
        int Left(PixelCanvas c) => Enumerable.Range(0, 24).First(x => Enumerable.Range(0, 12).Any(y => Lit(c.Get(x, y))));
        Assert.Equal(new[] { 3, 2, 1 }, new[] { Top(frames[1]), Top(frames[2]), Top(frames[3]) });
        Assert.Equal(new[] { 3, 4, 5 }, new[] { Top(frames[4]), Top(frames[5]), Top(frames[6]) });
        Assert.True(Left(frames[1]) < 12 && Left(frames[4]) >= 12);
    }

    [Fact]
    public void TheHallOfFamesBallIsLitWithARimOfLight()
    {
        var plain = Painted(12, 12, c => LeagueArt.PaintMachineBall(c));
        var lit = Painted(12, 12, c => LeagueArt.PaintMachineBall(c, lit: true));
        Assert.False(Same(plain, lit));
        // The same shape: every texel that is part of the one is part of the other
        for (int y = 0; y < 12; y++)
            for (int x = 0; x < 12; x++)
                Assert.Equal(plain.IsOpaque(x, y), lit.IsOpaque(x, y));
        // Its rim is light where the plain ball's is dark
        Assert.Equal(new Color(255, 246, 196, 255), lit.Get(0, 6));
        Assert.True(plain.Get(0, 6).R < 120);
        Assert.True(GymPieces.HasMachine(Room("PokemonLeagueHallOfFame")));
        Assert.False(GymPieces.HasMachine(Room("PokemonLeagueHallwayToHallOfFame")));
        Assert.False(GymPieces.HasMachine(Room("PokemonCenter")));
    }

    // ------------------------------------------------------------------ held faces

    [Fact]
    public void AHeldFaceIsRedrawnOnlyWhenItsFrameChanges()
    {
        var tray = new FaceFrames(PropModels.HealerTray, SetPieces.TrayFrames, 0f);
        Assert.True(tray.IsHeld);
        // The sheet holds the first frame: nothing to redraw until another is asked for, however long that is
        int redrawn = 0;
        for (double time = 0.0; time < 30.0; time += Frame)
            if (tray.Turn(time) != null) redrawn++;
        Assert.Equal(0, redrawn);
        // A healing of three: the tray is redrawn once for each ball, on and off with the fanfare's beat, and once to empty it
        var stage = new SetPieceStage();
        float takes = stage.Play(SetPiece.Heal, 3, 30.0);
        var shown = new List<int>();
        for (double time = 30.0; time < 30.0 + takes + 2.0; time += Frame)
        {
            var (balls, lit) = stage.Machine(SetPiece.Heal, time);
            tray.Hold(SetPieces.TrayFrame(balls, lit));
            if (tray.Turn(time) is { } frame) shown.Add(frame);
        }
        Assert.Equal(new[] { 1, 2, 3 }, shown.Take(3));
        Assert.Equal(0, shown[^1]);
        Assert.All(shown.Skip(3).SkipLast(1), f => Assert.Contains(f, new[] { 3, 9 }));
        Assert.Equal(9, shown[3]);
        // No frame is drawn twice running: a redraw is always a change
        for (int i = 1; i < shown.Count; i++) Assert.NotEqual(shown[i - 1], shown[i]);
        int frames = (int)((takes + 2.0) / Frame);
        Assert.True(shown.Count < 24 && shown.Count < frames / 10, $"{shown.Count} redraws in {frames} frames");
        // A frame out of range is kept within it
        tray.Hold(99);
        Assert.Equal(SetPieces.TrayFrames - 1, tray.Turn(100.0));
        tray.Hold(-3);
        Assert.Equal(0, tray.Turn(100.0));

        // A face that moves goes by the clock as it did, and takes no telling
        var water = new FaceFrames("fountain", 4, 6f);
        Assert.False(water.IsHeld);
        water.Hold(3);
        Assert.Null(water.Turn(0.0));
        Assert.Equal(1, water.Turn(1.0 / 6.0 + 0.001));
        Assert.Null(water.Turn(1.0 / 6.0 + 0.01));
        Assert.Equal(0, water.At(4.0 / 6.0 + 0.001));
    }

    [Fact]
    public void AHeldFaceIsPaintedInAllItsFramesAndTheSheetHoldsTheFirst()
    {
        var sheet = new ArtSheet();
        var art = sheet.PaintFrames("tray", 28, 22, SetPieces.TrayFrames, PropModels.PaintHealerTray);
        var (key, region, frames, rate) = Assert.Single(sheet.Moving);
        Assert.Equal(("tray", art, 0f), (key, region, rate));
        Assert.Equal(SetPieces.TrayFrames, frames.Length);
        Assert.All(frames, f => Assert.Equal((28, 22), (f.Width, f.Height)));
        var whole = sheet.ToCanvas();
        for (int y = 0; y < 22; y++)
            for (int x = 0; x < 28; x++)
                Assert.Equal(frames[0].Get(x, y), whole.Get(art.X + x, art.Y + y));
        // Asked for again, it is the same art and nothing is painted twice
        Assert.Equal(art, sheet.PaintFrames("tray", 28, 22, SetPieces.TrayFrames, (_, _) => throw new InvalidOperationException("painted twice")));
    }

    public static IEnumerable<object[]> Centers => Directory.EnumerateFiles(GameDataFiles.PathOf(MapDatabase.Folder), "*PokemonCenter.json")
        .Select(f => new object[] { Path.GetFileNameWithoutExtension(f) });

    [Theory]
    [MemberData(nameof(Centers))]
    public void EveryPokemonCenterHasItsMachineBesideTheNurseAndAPcThatComesOn(string name)
    {
        var center = Room(name);
        var nurse = Assert.Single(center.Everyone, n => n.IsHealingNurse);
        var machine = Assert.Single(center.Props, p => p.Type == PropType.HealingMachine);
        // She turns west to it, as the original's nurse does
        Assert.Equal((nurse.GridX, nurse.GridY), (machine.X + machine.Width, machine.Y));
        Assert.Null(nurse.Script);
        var terminal = Assert.Single(center.Everyone, n => n.IsPCTerminal);
        Assert.True(PropModels.IsTerminal(center, terminal.GridX, terminal.GridY));

        // Its scene's art has the held faces the pieces ask for: the tray, the console's front, and the screen of the
        // PC that is used; the computer behind the counter keeps its lit screen
        var kit = new KitBuilder(new ArtSheet(), MapScene.VerticalScaleOf(center));
        foreach (var prop in center.Props) PropModels.Build(kit, prop, center);
        for (int y = 0; y < center.Height; y++)
            for (int x = 0; x < center.Width; x++)
                if (center.GetGroundTile(x, y) == TileType.PC) PropModels.BuildPc(kit, x, y, PropModels.IsTerminal(center, x, y));
        var held = kit.Sheet.Moving.Where(m => m.Rate == 0f).ToDictionary(m => m.Key, m => m.Frames.Length);
        Assert.Equal(SetPieces.TrayFrames, held[PropModels.HealerTray]);
        Assert.Equal(2, held[PropModels.HealerFront]);
        Assert.Equal(SetPieces.PcFrames, held[PropModels.PcScreen(terminal.GridX, terminal.GridY)]);
        Assert.Single(held.Keys, k => k.StartsWith(PropModels.PcScreenPrefix));
        if (center.Props.Any(p => p.Type == PropType.Computer && !PropModels.IsTerminal(center, p.X, p.Y)))
            Assert.Contains(kit.Sheet.Faces, f => f.Key == "monitor.front");
    }

    [Theory]
    [InlineData("VistaLighthouseElevator")]
    [InlineData("ResortAreaRibbonSyndicateElevator")]
    [InlineData("VeilstoneStoreElevator")]
    [InlineData("BattleTowerElevator")]
    public void EveryLiftsCarHasAnIndicatorAndItsRideIsTheLiftsOwnPiece(string name)
    {
        // The original's PlayElevatorAnimation is in seven scripts; these four have a car here (Jubilife TV's lift is
        // roped off, and Hearthome's two houses' lifts lead straight to their floors)
        var car = Room(name);
        var indicator = Assert.Single(car.Props, p => p.Type == PropType.LiftIndicator);
        var kit = new KitBuilder(new ArtSheet(), MapScene.VerticalScaleOf(car));
        PropModels.Build(kit, indicator, car);
        var (key, _, frames, rate) = Assert.Single(kit.Sheet.Moving);
        Assert.Equal((PropModels.LiftLights, SetPieces.IndicatorFrames, 0f), (key, frames.Length, rate));
        // Four rounds of the car's lights, three in the Battle Tower's, as the original's scripts say
        var rides = ScriptLibrary.Default.All.Where(s => s.File == name).SelectMany(s => s.Everything())
            .Where(i => i.Op == Op.Anim).ToList();
        Assert.Contains(rides, i => i.Piece == SetPiece.ElevatorUp);
        Assert.Contains(rides, i => i.Piece == SetPiece.ElevatorDown);
        Assert.All(rides, i => Assert.Equal(name == "BattleTowerElevator" ? 3 : 4, i.Number));
    }

    // ------------------------------------------------------------------ the script's words

    [Fact]
    public void TheWordsOfAPieceAreRead()
    {
        var code = ScriptParser.Parse("test", """
            script Scene
              anim heal
              anim pc on
              anim pc off
              anim elevator up
              anim elevator down 3
              anim halloffame
              shake regigigas
              shake player 3
              flicker cresselia
              flicker self 4
              end
            """)[0].Code;
        Assert.Equal(new[] { SetPiece.Heal, SetPiece.PcOn, SetPiece.PcOff, SetPiece.ElevatorUp, SetPiece.ElevatorDown, SetPiece.HallOfFame },
            code.Take(6).Select(i => i.Piece));
        Assert.All(code.Take(6), i => Assert.Equal(Op.Anim, i.Op));
        // A lift hums four times unless told; someone is shaken eight times and flickers six, the original's counts
        Assert.Equal((SetPieces.Hums, 3), (code[3].Number, code[4].Number));
        Assert.Equal((4, 8, 6), (SetPieces.Hums, SetPieces.Shakes, SetPieces.Flickers));
        Assert.Equal((Op.Shake, "regigigas", 8), (code[6].Op, code[6].Name, code[6].Number));
        Assert.Equal((Op.Shake, "player", 3), (code[7].Op, code[7].Name, code[7].Number));
        Assert.Equal((Op.Flicker, "cresselia", 6), (code[8].Op, code[8].Name, code[8].Number));
        Assert.Equal((Op.Flicker, "self", 4), (code[9].Op, code[9].Name, code[9].Number));
        // Whoever is shaken or flickers is someone of the place, as anyone a script names is
        var named = ScriptLibrary.PeopleNamed(ScriptParser.Parse("test", "script Scene\n  shake regigigas\n  flicker cresselia 2\n  shake self\n  end\n")[0]).Select(p => p.Who);
        Assert.Equal(new[] { "regigigas", "cresselia" }, named);
    }

    [Theory]
    [InlineData("anim", "the line ends where 'heal' or 'pc' or 'elevator' or 'halloffame'")]
    [InlineData("anim dance", "'dance' is not 'heal' or 'pc' or 'elevator' or 'halloffame'")]
    [InlineData("anim pc", "'on' or 'off'")]
    [InlineData("anim elevator", "'up' or 'down'")]
    [InlineData("anim elevator sideways", "'up' or 'down'")]
    [InlineData("anim elevator up 0", "a lift hums 1 to")]
    [InlineData("anim elevator up 40", "a lift hums 1 to")]
    [InlineData("shake", "who")]
    [InlineData("shake self 0", "shaken 1 to")]
    [InlineData("flicker self 99", "flickers 1 to")]
    [InlineData("flicker self often", "how many times")]
    public void AWrongPieceIsTold(string line, string message)
    {
        var wrong = Assert.Throws<ScriptException>(() => ScriptParser.Parse("test", "script Scene\n  " + line + "\n  end\n"));
        Assert.Contains(message, wrong.Message);
    }

    [Fact]
    public void ASceneCallsEveryPieceInALineAndWaitsForEach()
    {
        var library = ScriptLibrary.FromSources(("test", """
            script Scene
              anim heal
              anim pc on
              anim pc off
              anim elevator up
              anim elevator down 3
              anim halloffame
              shake statue
              flicker statue 4
              shake player 2
              setflag FLAG_SCENE_OVER
              end
            """), ("common", ""));
        var map = new Map(8, 8);
        map.NPCs.Add(new NPC { Name = "Statue", Key = "statue", GridX = 3, GridY = 3 });
        // A team of three, one of them an Egg: the nurse's tray takes a ball for each that is no Egg (CountPartyNonEggs),
        // the Hall of Fame's machine one for each of the team (GetPartyCount)
        var host = new HeadlessScriptHost { Map = map };
        host.Party.Add(Mon());
        host.Party.Add(Mon("Starly"));
        host.Party.Add(Mon("Togepi"));
        host.Party.Members[2].IsEgg = true;
        var runner = new ScriptRunner(library, host);
        runner.Start(library.All.First(s => s.File == "test"), null);
        runner.RunToEnd();
        Assert.Equal(new[]
        {
            "anim heal 2", "anim pc on", "anim pc off", "anim elevator up 4", "anim elevator down 3", "anim halloffame 3",
            "shake Statue 8", "flicker Statue 4", "shake player 2"
        }, host.Log);
        Assert.True(host.Story.Has("FLAG_SCENE_OVER"));
        Assert.Equal((3, 3), (map.NPCs[0].GridX, map.NPCs[0].GridY));

        // The script waits for each piece for as long as it takes: the runner's own wait, as for a camera's move
        var waited = new HeadlessScriptHost { Map = map };
        waited.Party.Add(Mon());
        var slow = new ScriptRunner(library, waited);
        slow.Start(library.All.First(s => s.File == "test"), null);
        slow.Update(0.01f);
        Assert.Equal(new[] { "anim heal 1" }, waited.Log);
        slow.Update(SetPieces.MachineLength(1) - 0.1f);
        Assert.Single(waited.Log);
        slow.Update(0.2f);
        Assert.Equal(new[] { "anim heal 1", "anim pc on" }, waited.Log);
        slow.Update(SetPieces.PcOnSeconds + 0.01f);
        slow.Update(SetPieces.PcOffSeconds + 0.01f);
        Assert.Equal("anim elevator up 4", waited.Log[^1]);
        slow.Update(SetPieces.ElevatorLength(4) - 0.05f);
        Assert.Equal("anim elevator up 4", waited.Log[^1]);
        slow.RunToEnd();
        Assert.Equal(9, waited.Log.Count);

        // A script that may only set things up plays none of them
        foreach (string line in new[] { "anim heal", "shake statue", "flicker statue" })
        {
            var quiet = new HeadlessScriptHost { Map = map, ShowsNothing = true };
            var only = ScriptLibrary.FromSources(("test", "script Scene\n  " + line + "\n  end\n"), ("common", ""));
            var setUp = new ScriptRunner(only, quiet);
            setUp.Start(only.All.First(s => s.File == "test"), null);
            Assert.Throws<ScriptException>(() => setUp.RunToEnd());
        }
    }

    // ------------------------------------------------------------------ the rooms that play them

    [Fact]
    public void TheNurseTurnsToHerMachineAndTheScriptWaitsForItsBalls()
    {
        var center = Room("JubilifePokemonCenter");
        var nurse = center.Everyone.Single(n => n.IsHealingNurse);
        var host = new HeadlessScriptHost { Map = center };
        var hurt = Mon();
        hurt.CurrentHP = 1;
        host.Party.Add(hurt);
        host.Party.Add(Mon("Starly"));
        host.Party.Add(Mon("Shinx"));
        host.Party.Members[1].IsEgg = true;
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find(FieldScripts.For(nurse)!, center.Name)!, nurse);
        runner.RunToEnd();
        // Her words and her question as they were; the machine in place of the bare fanfare, with a ball for each of
        // the two that are no Egg; and she is facing the counter again
        Assert.Equal(new[] { "anim heal 2" }, host.Log);
        Assert.Equal(new[] { "Good day", "Shall I see to them", "Leave them with me", "There we are", "Take care" },
            new[] { "Good day", "Shall I see to them", "Leave them with me", "There we are", "Take care" }
                .Where(words => host.Transcript.Any(t => t.Text.Contains(words)) || host.Asked.Any(a => a.Question.Contains(words))));
        Assert.Equal(hurt.MaxHP, hurt.CurrentHP);
        Assert.Equal(Direction.Down, nurse.Facing);
        // In the script she turns west before the machine plays and back before the team is well
        var code = ScriptLibrary.Default.Find(FieldScripts.Nurse, null)!.Code;
        int anim = code.ToList().FindIndex(i => i.Op == Op.Anim);
        Assert.Equal((Op.Face, Direction.Left), (code[anim - 1].Op, code[anim - 1].Direction));
        Assert.Equal((Op.Face, Direction.Down), (code[anim + 1].Op, code[anim + 1].Direction));
        Assert.Equal(Op.Heal, code[anim + 2].Op);
        Assert.DoesNotContain(code, i => i.Op == Op.Fanfare);

        // Come round after a lost battle, she does the same
        var after = new HeadlessScriptHost { Map = center };
        after.Party.Add(Mon());
        var again = new ScriptRunner(ScriptLibrary.Default, after);
        again.Start(ScriptLibrary.Default.Find(FieldScripts.BlackOutCenter, center.Name)!, nurse);
        again.RunToEnd();
        Assert.Equal(new[] { "anim heal 1" }, after.Log);
    }

    [Fact]
    public void ThePcComesOnAndGoesOffHoweverItIsLeft()
    {
        var center = Room("JubilifePokemonCenter");
        var terminal = center.Everyone.Single(n => n.IsPCTerminal);
        HeadlessScriptHost Use(bool sealCase, params int[] answers)
        {
            var host = new HeadlessScriptHost { Map = center };
            if (sealCase) host.Bag.AddItem(ItemDatabase.Get("Seal Case")!, 1);
            foreach (int a in answers) host.Answers.Enqueue(a);
            var runner = new ScriptRunner(ScriptLibrary.Default, host);
            runner.Start(ScriptLibrary.Default.Find(FieldScripts.Pc, center.Name)!, terminal);
            runner.RunToEnd();
            return host;
        }
        // Straight to the boxes; the boxes by the menu; the menu's SWITCH OFF; the capsules with no Seal to open them for
        Assert.Equal(new[] { "anim pc on", "open Pc", "anim pc off" }, Use(false).Log);
        Assert.Equal(new[] { "anim pc on", "open Pc", "anim pc off" }, Use(true, 0).Log);
        Assert.Equal(new[] { "anim pc on", "anim pc off" }, Use(true, 2).Log);
        var none = Use(true, 1);
        Assert.Equal("anim pc on", none.Log[0]);
        Assert.Equal("anim pc off", none.Log[^1]);
        Assert.Contains(none.Transcript, t => t.Text.Contains("no Seals"));
    }

    [Fact]
    public void RegigigasShakesAndCresseliaFlickersAsTheOriginalsDo()
    {
        // ShakeObject LOCALID_REGIGIGAS, 8 and FlickerObject LOCALID_CRESSELIA, 6
        var wakes = ScriptLibrary.Default.Find("Regigigas", "snowpoint_temple_b5f")!.Everything().Single(i => i.Op == Op.Shake);
        Assert.Equal(("regigigas", 8), (wakes.Name, wakes.Number));
        var goes = ScriptLibrary.Default.Find("Cresselia", "fullmoon_island_forest")!.Code.ToList();
        int flicker = goes.FindIndex(i => i.Op == Op.Flicker);
        Assert.Equal(("cresselia", 6), (goes[flicker].Name, goes[flicker].Number));
        // It is taken off the map once it has flickered, and for good by its flag
        Assert.Equal((Op.Hide, "cresselia"), (goes[flicker + 1].Op, goes[flicker + 1].Name));
        Assert.Equal(Op.SetFlag, goes[flicker + 2].Op);
        // Six changes take as long as the three goings and comings the scene had before
        Assert.Equal(1.2f, SetPieces.FlickerLength(6), 4);
        // Nobody else in the game is shaken or flickers but the two uses plan 09's table names and the three beasts of the Burned Tower (plan 20 · J7)
        var all = ScriptLibrary.Default.All.SelectMany(s => s.Everything()).ToList();
        Assert.Single(all, i => i.Op == Op.Shake);
        Assert.Equal(4, all.Count(i => i.Op == Op.Flicker));
        Assert.Equal(3, all.Count(i => i.Op == Op.Flicker && i.Name.StartsWith("d18r0102_")));
        Assert.Single(all, i => i.Op == Op.Anim && i.Piece == SetPiece.HallOfFame);
        Assert.Equal(2, all.Count(i => i.Op == Op.Anim && i.Piece == SetPiece.Heal));
    }
}
