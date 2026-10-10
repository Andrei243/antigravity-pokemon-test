using System;
using System.Collections.Generic;
using System.Linq;

namespace PokemonPlatinumEngine.Models.SuperContests;

/// <summary>The four steps of the dance, in the order of the original's four buttons (<c>ov17_0224DD28</c>: 1 to 4).</summary>
public enum DanceStep { Jump = 1, Forward = 2, Left = 3, Right = 4 }

/// <summary>
/// A dance's music as the rules need it (<c>Unk_ov17_02253084</c>, <c>Unk_ov17_02252FC4</c>): the tempo in beats a
/// minute, the bars each lead dancer gets, the steps to a bar, the beats to a bar (16, or 12 for the two in three
/// time), and how many frames off the beat still count: within the first number an excellent step, within the
/// second a good one.
/// </summary>
public sealed record DanceSong(int Tempo, int Bars, int Steps, int BeatsPerBar, int Excellent, int Good)
{
    /// <summary>The length of a beat in ten-thousandths of a frame, thirty frames to the second (the original's <c>unk_18</c>).</summary>
    public int Beat => 60 * 30 * 10000 / Tempo;

    /// <summary>The frames of a bar (<c>unk_1C</c>): its first half is the lead's, its second the others'.</summary>
    public int BarFrames => (int)(((long)Beat * BeatsPerBar + 5000) / 10000);

    /// <summary>The half-beats of a bar: the places a step can fall on.</summary>
    public int Slots => BeatsPerBar * 2;

    private static readonly DanceSong[] Songs =
    {
        new(120, 2, 3, 16, 2, 3), new(120, 2, 4, 16, 2, 3), new(100, 2, 4, 12, 1, 2), new(60, 2, 4, 12, 2, 3),
        new(150, 2, 4, 16, 1, 2), new(132, 2, 4, 16, 1, 2), new(140, 2, 4, 16, 1, 2)
    };

    /// <summary>
    /// The music of a contest (<c>Unk_ov17_0225312C</c>): one tune for the Normal Rank, one for the Great Rank, and at
    /// the Ultra and Master Ranks one for each quality.
    /// </summary>
    public static DanceSong For(ContestRank rank, ContestStat category) => rank switch
    {
        ContestRank.Normal => Songs[0],
        ContestRank.Great => Songs[1],
        _ => Songs[2 + (int)category]
    };
}

/// <summary>A step someone danced: how near the beat it fell (0 and 1 excellent, 2 and 3 good, 4 a miss) and what it scored.</summary>
public readonly record struct DanceMark(int Contestant, DanceStep Step, int Grade, int Points, int Slot, int Frame, bool WrongStep)
{
    public bool Excellent => Grade <= 1;
    public bool Missed => Grade >= 4;
}

/// <summary>
/// The Dance Competition (plan 06 · R17a), ported from the original (<c>src/overlay017/ov17_0224CFB8.c</c> for the
/// dance's course and how a step is graded, <c>ov17_0224E930.c</c> for how the opponents dance,
/// <c>ov17_0224A0FC.c</c> for the points, <c>ov17_0223DAD0.c</c> for the music and the turns). Each contestant leads
/// in turn, the fourth entry first and the player last; a lead dances two bars. In a bar's first half the lead makes
/// up steps on the beat; in its second the others repeat them, the same step half a bar later. A step is worth two
/// points on the beat, one near it, none off it or when it is the wrong step. Time is counted in frames, thirty to
/// the second, as the original's; whoever shows it calls <see cref="Press"/> and <see cref="Advance"/> a frame at a
/// time. No drawing, no input, no clock of its own.
/// </summary>
public sealed class SuperContestDance
{
    public const int FramesPerSecond = 30, Rotations = 4;

    private readonly SuperContest contest;
    public DanceSong Song { get; }

    /// <summary>Which lead's turn it is (0 to 3), the bar of it, and the frame of the bar.</summary>
    public int Rotation { get; private set; }
    public int Bar { get; private set; }
    public int Frame { get; private set; }
    public bool IsOver => Rotation >= Rotations;

    /// <summary>Who stands where: the first is the lead (the original's <c>unk_05</c>).</summary>
    public int[] Positions { get; } = { 3, 2, 1, 0 };
    public int Lead => Positions[0];
    public int PositionOf(int contestant) => Array.IndexOf(Positions, contestant);

    /// <summary>The bar's first half, when the lead dances; the others answer in the second.</summary>
    public bool LeadsNow => Frame < Song.BarFrames >> 1;

    public int[] Score { get; } = new int[ContestRules.Contestants];

    /// <summary>The lead's steps of this bar, which the others must repeat (<c>unk_1AD4</c>): the half-beat each fell on and the step.</summary>
    public IReadOnlyList<(int Slot, DanceStep Step)> Led => led;
    private readonly List<(int Slot, DanceStep Step)> led = new();

    /// <summary>The steps danced in the last frame, for whoever shows them, and every step of the bar so far.</summary>
    public IReadOnlyList<DanceMark> Danced => danced;
    private readonly List<DanceMark> danced = new();
    public IReadOnlyList<DanceMark> BarMarks => barMarks;
    private readonly List<DanceMark> barMarks = new();

    // The player: steps danced this bar, the frames until the next is read, and the step waiting for the frame
    private int playerSteps, playerWait;
    private DanceStep? pressed;

    // An opponent's dancing (UnkStruct_ov17_0224E958): its own generator, the steps it means to lead, the answer it has planned
    private sealed class Dancer
    {
        public uint Seed;
        public int Mind;
        public int[] Times = new int[8];
        public int[] Steps = new int[8];
        public bool[] Pending = new bool[8];
        public int Done, Wait;
        public bool Planned;
        public int PlannedTime, PlannedStep;

        // Contest_GetSeededRNGNext: the generator steps and gives its high half
        public int Next()
        {
            Seed = unchecked(Seed * 1812433253u + 1u);
            return (int)(Seed >> 16);
        }

        // ov17_0224E958: a new bar; the generator goes on
        public void Reset()
        {
            Array.Clear(Pending);
            Done = Wait = 0;
            Planned = false;
        }
    }

    private readonly Dancer[] dancers = new Dancer[ContestRules.Contestants];

    public SuperContestDance(SuperContest contest)
    {
        this.contest = contest;
        Song = DanceSong.For(contest.Rank, contest.Category);
        BeginRotation();
    }

    /// <summary>Whether the player's Pokémon may take a step this frame (<c>ov17_0224D710</c>'s conditions on the stylus).</summary>
    public bool CanPress
    {
        get
        {
            if (IsOver || playerSteps >= Song.Steps || playerWait > 1) return false;
            int bar = Song.BarFrames, quarter = Song.Beat / 2 / 2 / 10000;
            // The frame the step would fall on is the next one
            int at = Frame + 1;
            return PositionOf(0) == 0 ? at < bar / 2 - quarter : at >= bar >> 1 && at < bar - quarter;
        }
    }

    /// <summary>The player's step, for the next frame. False when none may be taken now.</summary>
    public bool Press(DanceStep step)
    {
        if (!CanPress) return false;
        pressed = step;
        return true;
    }

    /// <summary>
    /// A frame passes: at a bar's end the next begins (and after a lead's bars the next lead's turn); otherwise the
    /// player's step is graded if one was pressed, and each opponent dances its part.
    /// </summary>
    public void Advance()
    {
        if (IsOver) return;
        danced.Clear();
        Frame++;
        if (Frame >= Song.BarFrames)
        {
            Frame = 0;
            pressed = null;
            Bar++;
            if (Bar >= Song.Bars) EndRotation();
            else BeginBar();
            return;
        }

        if (playerWait > 0) playerWait--;
        if (pressed is { } step)
        {
            pressed = null;
            Dance(0, step, playerSteps);
            playerSteps++;
            // ov17_0224DC1C: the next step is read a beat less two frames later
            playerWait = Song.Beat / 10000 - 2;
        }
        for (int c = 1; c < dancers.Length; c++)
        {
            if (PositionOf(c) == 0) LeadAsOpponent(c);
            else AnswerAsOpponent(c);
        }
    }

    // ov17_0223EBB0, ov17_0224D544: a lead's turn begins; each opponent's generator is seeded from the contest's
    private void BeginRotation()
    {
        for (int c = 1; c < dancers.Length; c++)
            dancers[c] = new Dancer { Seed = (uint)contest.Next16(), Mind = contest.Contestants[c].Mind };
        Bar = 0;
        Frame = 0;
        BeginBar();
    }

    private void BeginBar()
    {
        led.Clear();
        barMarks.Clear();
        playerSteps = playerWait = 0;
        for (int c = 1; c < dancers.Length; c++) dancers[c].Reset();
        if (Lead != 0) Plan(dancers[Lead]);
    }

    // ov17_0223ED9C: the lead goes to the back and everyone moves up
    private void EndRotation()
    {
        int lead = Positions[0];
        for (int i = 0; i < Positions.Length - 1; i++) Positions[i] = Positions[i + 1];
        Positions[^1] = lead;
        Rotation++;
        led.Clear();
        barMarks.Clear();
        if (!IsOver) BeginRotation();
        else
            for (int c = 0; c < Score.Length; c++) contest.Contestants[c].DanceScore = Score[c];
    }

    // ------------------------------------------------------------------ a step and its grade

    /// <summary>
    /// The half-beat nearest a frame of the bar and how many frames off it is (<c>ov17_0224DDE4</c>): the half-beats
    /// are counted up to the frame in ten-thousandths, and the nearer of the one before and the one after wins, the
    /// one before when they are as near.
    /// </summary>
    public (int Off, int Slot) Nearest(int frame)
    {
        long at = (long)frame * 10000, half = Song.Beat / 2;
        long before = 0, mark = 0;
        uint count = 0;
        for (; mark < at; mark += half)
        {
            before = mark;
            count++;
        }
        long after = mark;
        before = (before + 5000) / 10000;
        after = (after + 5000) / 10000;
        long offBefore = Math.Abs(frame - before), offAfter = Math.Abs(frame - after);
        if (offBefore <= offAfter) return ((int)offBefore, (int)(unchecked(count - 1) & 0xff));
        return ((int)offAfter, (int)(count & 0xff));
    }

    // ov17_0224DE54 and ov17_0224B20C: a step is graded and scored; the lead's is kept for the others to repeat
    private void Dance(int contestant, DanceStep step, int stepNumber)
    {
        bool leads = PositionOf(contestant) == 0;
        var (off, slot) = Nearest(Frame);
        int answered = -1;
        if (!leads)
        {
            int wanted = slot - Song.Slots / 2;
            answered = led.FindIndex(l => l.Slot == wanted);
            if (answered < 0) off = 0xff;
        }
        int grade = off <= Song.Excellent ? 0 : off <= Song.Good ? 2 : 4;
        bool wrong = false;
        if (answered >= 0 && grade < 4 && led[answered].Step != step)
        {
            grade = 4;
            wrong = true;
        }
        int points = grade <= 1 ? 2 : grade <= 3 ? 1 : 0;
        Score[contestant] += points;
        if (leads && led.Count < 8 && !led.Any(l => l.Slot == slot)) led.Add((slot, step));
        var mark = new DanceMark(contestant, step, grade, points, slot, Frame, wrong);
        danced.Add(mark);
        barMarks.Add(mark);
    }

    // ------------------------------------------------------------------ how the opponents dance

    // Unk_ov17_02254A80: how loose each of the four ways of dancing is
    private static readonly int[] Slack = { 1, 2, 3, 4 };

    // ov17_0224E990: the steps an opponent means to lead this bar: which half-beats, how far off each, and which step
    private void Plan(Dancer d)
    {
        int slots = Song.Slots;
        bool triple = Song.BeatsPerBar != 16;
        int late = (int)((Song.Beat / (triple ? 6 : 4) + 5000L) / 10000);
        bool busy = contest.Rank >= ContestRank.Ultra;
        // The lower ranks dance on whole beats, the upper ones on half-beats too
        int places = busy ? slots / 2 : slots / 2 / 2;
        long unit = busy ? Song.Beat / 2 : Song.Beat;

        // One place more than the original clears, so the neighbour past the last reads as free (it reads past its array there)
        bool[] taken = new bool[slots / 2 + 1];
        int count = 0;
        while (count < Song.Steps)
        {
            int place = d.Next() % places;
            if (place == 0 || taken[place]) continue;
            if (busy)
            {
                if (place == places - 1 && taken[place - 1]) continue;
                if (taken[place - 1] || taken[place + 1]) continue;
                if ((place & 1) != 0 && (d.Next() & 0xff) < 128) continue;
            }
            taken[place] = true;
            count++;
        }

        int n = 0;
        for (int place = 0; place < slots / 2; place++)
            if (taken[place]) d.Times[n++] = (int)((place * unit + 5000) / 10000);

        // ov17_0224EBE0
        int slack = Slack[d.Mind];
        slack = contest.Rank switch { ContestRank.Normal => slack * 2, ContestRank.Great => slack, ContestRank.Ultra => slack / 2, _ => slack / 3 };
        for (int i = 0; i < Song.Steps; i++)
        {
            int at = d.Times[i] + d.Next() % (2 + slack) - (1 + slack / 2);
            if (at < 0) at = 0;
            if (at - d.Times[i] >= late) at = d.Times[i] + late - 1;
            d.Times[i] = at;
            d.Pending[i] = true;
        }

        // Unk_ov17_02254A84: how often a step repeats the one before
        int repeat = contest.Rank switch { ContestRank.Normal => 90, ContestRank.Great => 40, _ => 0 };
        d.Steps[0] = d.Next() % 4;
        for (int i = 1; i < Song.Steps; i++)
            d.Steps[i] = i < Song.Steps - 1 && repeat > d.Next() % 100 ? d.Steps[i - 1] : d.Next() % 4;
    }

    // ov17_0224EDE0: in the bar's first half, the next step it planned is danced once its frame has come
    private void LeadAsOpponent(int c)
    {
        var d = dancers[c];
        if (!LeadsNow || d.Done >= Song.Steps) return;
        if (d.Wait > 0)
        {
            d.Wait--;
            return;
        }
        int next = Array.IndexOf(d.Pending, true);
        if (next < 0 || d.Times[next] > Frame) return;
        Dance(c, (DanceStep)(d.Steps[next] + 1), d.Done);
        d.Pending[next] = false;
        d.Done++;
        d.Wait = Song.Beat / 10000 - 2;
    }

    // ov17_0224EE90: in the second half, each of the lead's steps in turn is answered: a frame is planned for it, more
    // or less off by how loose the dancer is, and now and then the wrong step
    private void AnswerAsOpponent(int c)
    {
        var d = dancers[c];
        if (LeadsNow || d.Done >= Song.Steps || d.Done >= led.Count) return;
        if (d.Wait > 0)
        {
            d.Wait--;
            return;
        }
        var (slot, step) = led[d.Done];
        int since = Frame - Song.BarFrames / 2;
        if (!d.Planned)
        {
            int before = d.Done == 0 ? -1 : led[d.Done - 1].Slot;
            int stepBefore = d.Done == 0 ? 0 : (int)led[d.Done - 1].Step;
            int slack = AnswerSlack(d.Mind);
            int slips = AnswerSlips(d.Mind, slot, before, (int)step, stepBefore);
            int at = (int)((slot * (long)(Song.Beat / 2) + 5000) / 10000);
            int off = d.Next() % (2 + slack) - (1 + slack / 2);
            at = off + at < 0 ? 0 : at + off;
            int answer;
            if (d.Next() % 100 < slack + slips)
            {
                do answer = d.Next() % 4;
                while (answer + 1 == (int)step);
            }
            else answer = (int)step - 1;
            d.PlannedTime = at;
            d.PlannedStep = answer;
            d.Planned = true;
        }
        if (d.PlannedTime > since) return;
        Dance(c, (DanceStep)(d.PlannedStep + 1), d.Done);
        d.Planned = false;
        d.Done++;
        d.Wait = Song.Beat / 10000 - 2;
    }

    // ov17_0224EC24. The original first reckons a number from where the step falls and then throws it away for the
    // dancer's own looseness: kept as it comes out.
    private int AnswerSlack(int mind)
    {
        int slack = Slack[mind];
        return contest.Rank switch
        {
            ContestRank.Normal or ContestRank.Great => slack * 2,
            ContestRank.Ultra => slack + slack / 2,
            _ => slack
        };
    }

    // ov17_0224ECC4: the chances in a hundred of answering with the wrong step: each way of dancing has a step it
    // fumbles, a change of step is harder than a repeat, and so are a step off the beat of the one before and one long after it
    private int AnswerSlips(int mind, int slot, int before, int step, int stepBefore)
    {
        int slips = 0;
        int fumbled = mind switch { 0 => 1, 1 => 2, 2 => 4, _ => 3 };
        if (step == fumbled) slips += 3;
        if (stepBefore != 0 && step != stepBefore) slips += 8;
        if (before != -1)
        {
            if ((slot & 1) != (before & 1)) slips += 2;
            if (slot - before >= 8) slips += 5;
        }
        return contest.Rank switch
        {
            ContestRank.Normal or ContestRank.Great => slips * 2,
            ContestRank.Ultra => slips + slips / 2,
            _ => slips
        };
    }
}
