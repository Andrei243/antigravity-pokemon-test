using System;
using System.Collections.Generic;
using System.Linq;

namespace PokemonPlatinumEngine.Models.SuperContests;

/// <summary>What a contestant does in a turn of acting: a move, shown to one of the three judges.</summary>
public readonly record struct ActingChoice(MoveData Move, int Judge);

/// <summary>What happens on stage in a turn of acting, for whoever shows it.</summary>
public enum ActingStep
{
    /// <summary>A contestant performs a move for a judge; <c>Points</c> is what the move is worth by itself, <c>Flag</c> whether someone performed for that judge already.</summary>
    Perform,
    /// <summary>The move's effect went off as it was performed; <c>Points</c> is what it added, <c>Variant</c> which of its lines fits, <c>Other</c> whoever it names.</summary>
    Effect,
    /// <summary>The turn before's doubling: <c>Points</c> added again.</summary>
    Doubled,
    /// <summary>The judge's voltage moved by <c>Change</c> to <c>Voltage</c>; <c>Points</c> is the bonus of a voltage filled.</summary>
    Voltage,
    /// <summary>A judge gives <c>Points</c> to each of those who performed for them (<c>Others</c>).</summary>
    Judge,
    /// <summary>An effect that waits for the judges (or for the whole turn) went off: as <see cref="Effect"/>.</summary>
    LateEffect,
    /// <summary>The turn is over: every score is counted and the next turn's order set.</summary>
    TurnEnd
}

/// <summary>One thing that happened in a turn of acting (<see cref="SuperContestActing.Perform"/>'s log).</summary>
public sealed record ActingEvent(ActingStep Step, int Contestant = -1)
{
    public int Judge { get; init; } = -1;
    public MoveData? Move { get; init; }
    public ContestEffect Effect { get; init; }
    public int Variant { get; init; }
    public int Other { get; init; } = -1;

    /// <summary>Points gained at this step (ten to a heart), and the contestant's points of the turn after it.</summary>
    public int Points { get; init; }
    public int Score { get; init; }
    public int Change { get; init; }
    public int Voltage { get; init; }
    public bool Flag { get; init; }
    public IReadOnlyList<int> Others { get; init; } = Array.Empty<int>();
}

/// <summary>
/// The Acting Competition (plan 06 · R17a), ported from the original's rules (<c>src/overlay017/ov17_02245F14.c</c>
/// for a turn's course and every effect, <c>ov17_02243AC4.c</c> for the judges, the voltage and the next turn's order,
/// <c>ov17_02246ECC.c</c> for how the opponents choose, <c>ov17_0223B140.c</c> for the order things happen in). Four
/// turns; in each, everyone chooses a move and a judge, then performs in the turn's order. Points are tens, a heart
/// each. Our names for the original's unnamed fields are in the comments. No drawing, no input, no clock.
/// </summary>
public sealed class SuperContestActing
{
    public const int Turns = 4;

    private readonly SuperContest contest;

    /// <summary>The turn under way, from 0; four when the round is over (the original's <c>unk_220.unk_00</c>).</summary>
    public int Turn { get; private set; }
    public bool IsOver => Turn >= Turns;

    /// <summary>Who performs when this turn: contestants from first to last (<c>unk_01</c>).</summary>
    public int[] Order { get; private set; }

    /// <summary>The three judges' voltage in points, ten to a step, five steps full (<c>unk_22</c>).</summary>
    public int[] Voltage { get; } = new int[ContestRules.Judges];

    /// <summary>Each contestant's points so far, and those of the turn before (<c>unk_12</c>, <c>unk_1A</c>).</summary>
    public int[] Total { get; } = new int[ContestRules.Contestants];
    public int[] LastTurn { get; } = new int[ContestRules.Contestants];

    /// <summary>The move each showed the turn before (<c>unk_26</c>), which may not be shown again at once.</summary>
    public MoveData?[] LastMove { get; } = new MoveData?[ContestRules.Contestants];

    // What a turn leaves for the next (the original's unk_30): whose score doubles, and whose move may be repeated
    private readonly bool[] doubleNext = new bool[ContestRules.Contestants];
    private readonly MoveData?[] repeatable = new MoveData?[ContestRules.Contestants];

    /// <summary>Whether a contestant's next score is doubled (the turn before's <see cref="ContestEffect.DoubleNextTurn"/>).</summary>
    public bool IsDoubled(int contestant) => doubleNext[contestant];

    public SuperContestActing(SuperContest contest, int[]? firstOrder = null)
    {
        this.contest = contest;
        Order = (firstOrder ?? contest.ActingOrder()).ToArray();
    }

    /// <summary>
    /// Whether a contestant may show a move now (<c>ov17_02243A98</c>): never the one shown the turn before, unless
    /// that turn its effect was <see cref="ContestEffect.ConsecutiveUse"/> (which lets it be shown twice, not thrice).
    /// </summary>
    public bool CanUse(int contestant, MoveData move) =>
        move.Contest != null && !(move == LastMove[contestant] && repeatable[contestant] != move);

    // ------------------------------------------------------------------ how the opponents choose

    private readonly record struct Slot(MoveData? Move, ContestEffect Effect, ContestStat Type);

    /// <summary>
    /// What the three opponents do this turn (<c>ov17_02246ECC</c>), in their order, each by the table
    /// (<see cref="ActingAiTable"/>): every row for its place in the order whose question the stage answers yes adds
    /// its weight to the moves it names and to the judges the question marked; the heaviest move wins, then its
    /// heaviest judge, ties by draws made beforehand. A move with no judge preferred shies from the judge the player
    /// chose, the more often the lower the rank. The first entry is the player's own choice, as given.
    /// </summary>
    public ActingChoice[] Choose(ActingChoice player)
    {
        var choices = new ActingChoice[ContestRules.Contestants];
        choices[0] = player;
        for (int c = 1; c < ContestRules.Contestants; c++) choices[c] = ChooseFor(c, player.Judge);
        return choices;
    }

    private ActingChoice ChooseFor(int contestant, int playerJudge)
    {
        var who = contest.Contestants[contestant];
        // ov17_02246F24: the four moves; one that may not be shown is an empty place, whose effect and quality read as
        // nought (None, and Cool: the original's cleared bytes, which the questions below still count)
        var slots = new Slot[4];
        for (int m = 0; m < 4; m++)
        {
            var move = m < who.Pokemon.Moves.Count ? who.Pokemon.Moves[m].Data : null;
            slots[m] = move != null && CanUse(contestant, move)
                ? new Slot(move, move.Contest!.Effect, move.Contest.Type)
                : new Slot(null, ContestEffect.None, ContestStat.Cool);
        }
        int[] moveWeight = new int[4];
        int[,] judgeWeight = new int[4, ContestRules.Judges];

        int place = Array.IndexOf(Order, contestant) + 1;
        foreach (var row in ActingAiTable.Rows)
        {
            if (row.Place != place) continue;
            bool[] marked = new bool[ContestRules.Judges];
            if (!Asks(row.Question, contestant, slots, marked)) continue;
            switch (row.Judges)
            {
                case 0: Array.Clear(marked); break;
                case 2 or 3: for (int j = 0; j < marked.Length; j++) marked[j] = !marked[j]; break;
            }
            int weight = row.Weight(who.Mind);
            for (int m = 0; m < 4; m++)
            {
                bool named = row.Moves switch
                {
                    ActingAiTable.OwnQuality => slots[m].Type == contest.Category,
                    ActingAiTable.TwoHearts => ContestRules.Appeal(slots[m].Effect) >= 20,
                    _ => (int)slots[m].Effect == row.Moves
                };
                if (!named) continue;
                moveWeight[m] += weight;
                for (int j = 0; j < marked.Length; j++)
                    if (marked[j]) judgeWeight[m, j] += weight;
            }
        }

        // The draws that settle ties, made before anything is compared
        int[] moveDraw = new int[4], judgeDraw = new int[ContestRules.Judges];
        for (int m = 0; m < 4; m++) moveDraw[m] = contest.Next16();
        for (int j = 0; j < judgeDraw.Length; j++) judgeDraw[j] = contest.Next16();

        int best = Array.FindIndex(slots, s => s.Move != null);
        if (best < 0) throw new InvalidOperationException($"{who.Pokemon.DisplayName} has no move it may show.");
        for (int m = best + 1; m < 4; m++)
            if (slots[m].Move != null && (moveWeight[m] > moveWeight[best] || (moveWeight[m] == moveWeight[best] && moveDraw[m] > moveDraw[best])))
                best = m;

        // No judge preferred: keep away from the player's, 230 times in 256 at the Normal Rank down to never at the Master Rank
        if (Enumerable.Range(0, ContestRules.Judges).All(j => judgeWeight[best, j] == 0))
        {
            int[] shy = { 230, 128, 51, 0 };
            if ((contest.Next16() & 0xff) < shy[(int)contest.Rank]) judgeWeight[best, playerJudge] -= 100;
        }
        int judge = 0;
        for (int j = 1; j < ContestRules.Judges; j++)
            if (judgeWeight[best, j] > judgeWeight[best, judge] || (judgeWeight[best, j] == judgeWeight[best, judge] && judgeDraw[j] > judgeDraw[judge]))
                judge = j;
        return new ActingChoice(slots[best].Move!, judge);
    }

    // The table's questions (Unk_ov17_02253BBC), by their place in the original's list; the judges a question is about
    // are marked. The two that ask for "turn four" can never be true while a turn is chosen (the turns count from
    // nought to three): kept as they are.
    private bool Asks(int question, int contestant, Slot[] slots, bool[] marked)
    {
        bool Has(ContestEffect effect) => slots.Any(s => s.Effect == effect);
        bool HasOwn() => slots.Any(s => s.Type == contest.Category);
        bool Lowest() => !Total.Any(t => Total[contestant] > t);
        int Mark(Func<int, bool> which)
        {
            int count = 0;
            for (int j = 0; j < marked.Length; j++)
                if (which(Voltage[j]))
                {
                    marked[j] = true;
                    count++;
                }
            return count;
        }

        switch (question)
        {
            case 1: return Turn == Turns;
            case 2: return doubleNext[contestant];
            case 3: return Has(ContestEffect.Basic);
            case 4: return Has(ContestEffect.AllSameJudge);
            case 5: return Has(ContestEffect.StealVoltage);
            case 6: return Has(ContestEffect.DoubleFinalAct);
            case 7: return Has(ContestEffect.FirstPerformanceAdvantage);
            case 8: return Has(ContestEffect.FinalPerformanceAdvantage);
            case 9: return Has(ContestEffect.NoVoltageDown);
            case 10: return Has(ContestEffect.TwoVoltageInARowAdvantage);
            case 11: return Has(ContestEffect.HighScoreLaterTurn);
            case 12: return Lowest();
            case 13: return Turn == Turns && Lowest();
            case 14: return slots.Any(s => s.Effect == ContestEffect.TwoHeartsWhenVoltageUp && s.Type == contest.Category);
            case 15: return HasOwn();
            case 16 or 28: return HasOwn() && Mark(v => v == 40) > 0;
            case 17: return HasOwn() && Mark(v => v == 30) > 0;
            case 18: return Has(ContestEffect.Voltage);
            case 19: return Has(ContestEffect.SuppressVoltage);
            case 20: return Mark(v => v == 40) > 0;
            case 21: return Mark(v => v == 30) > 0;
            case 22: return Mark(v => v <= 10) > 0;
            case 23: return Voltage.All(v => v >= 20) && Mark(_ => true) > 0;
            case 24: return Voltage.All(v => v <= 20) && Mark(_ => true) > 0;
            case 25: return Mark(v => v <= 20) == 1;
            case 26: return Mark(v => v == 40) == 2;
            case 27: return Mark(v => v == 0) == 1;
            default: return false;
        }
    }

    // ------------------------------------------------------------------ a turn

    // One contestant's part of a turn (the original's UnkStruct_ov17_022444BC)
    private sealed class Act
    {
        public MoveData Move = null!;
        public ContestEffect Effect;
        public int Judge;                       // unk_03
        public int Others;                      // unk_09: how many others chose the same judge
        public int Points;                      // unk_18
        public int Bonus;                       // unk_1E
        public int VoltageBonus;                // unk_08
        public int Seen;                        // unk_1A: the points when last looked at
        public int Compared;                    // unk_1C: the points once the judges have given theirs
        public bool NoVoltageUp, NoVoltageDown; // unk_28_1, unk_28_2
        public int OrderKind, OrderRank;        // unk_28_4, unk_28_6: 1 first, 2 last, 3 shuffled; the place among them
        public bool DoubleNext;                 // unk_28_12
    }

    private Act[] acts = Array.Empty<Act>();
    private int[] voltageNow = Array.Empty<int>();    // unk_B3
    private int[] voltageBefore = Array.Empty<int>(); // unk_B0
    private List<ActingEvent> log = new();

    // ov17_02246090: the contestant's points are looked at afresh and the bonus counted from nought
    private void Look(int c)
    {
        acts[c].Seen = acts[c].Points;
        acts[c].Bonus = 0;
        voltageBefore = (int[])voltageNow.Clone();
    }

    /// <summary>
    /// Plays a turn (<c>ov17_0223BE58</c> to <c>ov17_0223C888</c>): each contestant performs in the turn's order
    /// (the move's own points, its effect, the doubling a move of the turn before left, the judge's voltage), then
    /// the judges give their points, the effects that wait for them go off, the scores are counted and the next
    /// turn's order is set. Returns what happened, in order.
    /// </summary>
    public IReadOnlyList<ActingEvent> Perform(ActingChoice[] choices)
    {
        if (IsOver) throw new InvalidOperationException("The Acting Competition is over.");
        if (choices.Length != ContestRules.Contestants) throw new ArgumentException("Four choices are wanted.", nameof(choices));
        for (int c = 0; c < choices.Length; c++)
        {
            if (!CanUse(c, choices[c].Move)) throw new InvalidOperationException($"{choices[c].Move.Name} may not be shown this turn.");
            if (choices[c].Judge is < 0 or >= ContestRules.Judges) throw new ArgumentOutOfRangeException(nameof(choices));
        }

        // ov17_02245F44: the turn's own state, and what the turn before left
        log = new List<ActingEvent>();
        bool[] doubled = (bool[])doubleNext.Clone();
        Array.Clear(repeatable);
        acts = choices.Select((choice, c) => new Act
        {
            Move = choice.Move,
            Effect = choice.Move.Contest!.Effect,
            Judge = choice.Judge,
            Others = choices.Count(o => o.Judge == choice.Judge) - 1
        }).ToArray();
        voltageNow = (int[])Voltage.Clone();
        voltageBefore = (int[])Voltage.Clone();

        for (int place = 0; place < Order.Length; place++)
        {
            int c = Order[place];
            var act = acts[c];
            bool already = Order.Take(place).Any(o => acts[o].Judge == act.Judge);

            // ov17_02245FB4, ov17_02246138: the judges' voltage as it stands, and the move's own points
            voltageNow = (int[])Voltage.Clone();
            Look(c);
            act.Points = ContestRules.Appeal(act.Effect);
            log.Add(new ActingEvent(ActingStep.Perform, c) { Move = act.Move, Judge = act.Judge, Effect = act.Effect, Points = act.Points, Score = act.Points, Flag = already });

            // ov17_02246160: the effect that goes off as the move is performed
            Look(c);
            if (Timing(act.Effect) == 3)
            {
                Run(c, place, ActingStep.Effect);
                act.Points += act.Bonus;
            }

            // ov17_02246228: the turn before's doubling
            Look(c);
            if (doubled[c])
            {
                int again = act.Points;
                act.Bonus += again;
                act.Points += act.Bonus;
                log.Add(new ActingEvent(ActingStep.Doubled, c) { Points = again, Score = act.Points });
            }

            // ov17_022463C4: the judge's voltage
            Look(c);
            int change = ContestRules.Voltage(contest.Category, act.Move.Contest!.Type);
            int j = act.Judge;
            if (change > 0 && !act.NoVoltageUp)
                voltageNow[j] = Math.Min(ContestRules.FullVoltage, voltageNow[j] + change);
            else if (change < 0 && !act.NoVoltageDown && voltageNow[j] > 0)
                voltageNow[j] = Math.Max(0, voltageNow[j] + change);
            else change = 0;
            if (voltageNow[j] >= ContestRules.FullVoltage)
                act.VoltageBonus = j == ContestRules.MainJudge ? ContestRules.MainJudgeBonus : ContestRules.FullVoltageBonus;
            act.Points += act.VoltageBonus;
            log.Add(new ActingEvent(ActingStep.Voltage, c) { Judge = j, Change = change, Voltage = voltageNow[j], Points = act.VoltageBonus, Score = act.Points });

            // ov17_02246018: a voltage that was filled starts again from nothing
            for (int k = 0; k < Voltage.Length; k++) Voltage[k] = voltageNow[k] >= ContestRules.FullVoltage ? 0 : voltageNow[k];
        }

        // ov17_0223C350: nothing has the original's fourth timing, but each is looked at in turn all the same
        foreach (int c in Order) Look(c);

        // ov17_02246518: each judge's points, to everyone who performed for them
        for (int c = 0; c < acts.Length; c++)
        {
            Look(c);
            acts[c].Points += ContestRules.JudgePoints(acts[c].Others);
        }
        // As the original tells it: the judge most performed for first
        foreach (int j in Enumerable.Range(0, ContestRules.Judges).OrderByDescending(j => acts.Count(a => a.Judge == j)))
        {
            var theirs = Order.Where(c => acts[c].Judge == j).ToArray();
            if (theirs.Length == 0) continue;
            log.Add(new ActingEvent(ActingStep.Judge) { Judge = j, Others = theirs, Points = ContestRules.JudgePoints(theirs.Length - 1) });
        }

        // The effects that wait for the judges, then those that wait for everyone's score
        for (int place = 0; place < Order.Length; place++)
        {
            int c = Order[place];
            Look(c);
            if (Timing(acts[c].Effect) != 5) continue;
            Run(c, place, ActingStep.LateEffect);
            acts[c].Points += acts[c].Bonus;
        }
        foreach (var act in acts) act.Compared = act.Points;
        for (int place = 0; place < Order.Length; place++)
        {
            int c = Order[place];
            Look(c);
            if (Timing(acts[c].Effect) != 6) continue;
            Run(c, place, ActingStep.LateEffect);
            acts[c].Points += acts[c].Bonus;
        }

        // ov17_0223C888: the scores are counted, the turn's leavings kept and the next order set
        for (int c = 0; c < acts.Length; c++)
        {
            Total[c] += acts[c].Points;
            LastTurn[c] = acts[c].Points;
            doubleNext[c] = acts[c].DoubleNext;
        }
        Order = NextOrder();
        for (int c = 0; c < acts.Length; c++) LastMove[c] = acts[c].Move;
        Turn++;
        log.Add(new ActingEvent(ActingStep.TurnEnd) { Others = Order.ToArray() });
        if (IsOver)
            for (int c = 0; c < acts.Length; c++) contest.Contestants[c].ActingScore = Total[c];
        return log;
    }

    // Unk_ov17_02253AF8's second number: 3 as the move is performed, 5 once the judges have given their points, 6 last of all
    private static int Timing(ContestEffect effect) => effect switch
    {
        ContestEffect.DoubledJudge or ContestEffect.UniqueJudge or ContestEffect.AllSameJudge => 5,
        ContestEffect.PityPoints => 6,
        _ => 3
    };

    private void Say(int c, ActingStep step, int variant = 0, int other = -1) =>
        log.Add(new ActingEvent(step, c) { Effect = acts[c].Effect, Variant = variant, Other = other, Points = acts[c].Bonus, Score = acts[c].Points + acts[c].Bonus, Move = acts[c].Move });

    // The effects (ov17_02246578 to ov17_02246E68), each in the original's own arithmetic
    private void Run(int c, int place, ActingStep step)
    {
        var act = acts[c];
        Act? before = place > 0 ? acts[Order[place - 1]] : null;
        switch (act.Effect)
        {
            case ContestEffect.FirstNextTurn:
            {
                // Whoever already asked to go first moves back a place, and so on down
                int was = 4;
                int[] rank = new int[4];
                for (int o = 0; o < 4; o++)
                {
                    if (acts[o].OrderKind == 0) rank[o] = 0xff;
                    else
                    {
                        if (acts[o].OrderKind == 1 && acts[o].OrderRank == 0) was = o;
                        rank[o] = acts[o].OrderRank;
                    }
                }
                rank[c] = 0xff;
                for (int r = 0; r < 4; r++)
                {
                    int o = 0;
                    for (; o < 4; o++)
                        if (rank[o] != 0xff && r == rank[o] && rank[o] == acts[o].OrderRank)
                        {
                            rank[o]++;
                            break;
                        }
                    if (o == 4) break;
                }
                for (int o = 0; o < 4; o++)
                    if (rank[o] != 0xff) acts[o].OrderRank = rank[o];
                act.OrderKind = 1;
                act.OrderRank = 0;
                Say(c, step, was != 4 ? 1 : 0, was != 4 ? was : -1);
                break;
            }
            case ContestEffect.LastNextTurn:
            {
                int was = 4;
                int[] rank = new int[4];
                for (int o = 0; o < 4; o++)
                {
                    if (acts[o].OrderKind == 0) rank[o] = 0xff;
                    else
                    {
                        if (acts[o].OrderKind == 2 && acts[o].OrderRank == 3) was = o;
                        rank[o] = acts[o].OrderRank;
                    }
                }
                rank[c] = 0xff;
                for (int r = 3; r > -1; r--)
                {
                    int o = 0;
                    for (; o < 4; o++)
                        if (rank[o] != 0xff && r == rank[o] && rank[o] == acts[o].OrderRank)
                        {
                            rank[o]--;
                            break;
                        }
                    if (o == 4) break;
                }
                for (int o = 0; o < 4; o++)
                    if (rank[o] != 0xff) acts[o].OrderRank = rank[o];
                act.OrderKind = 2;
                act.OrderRank = 3;
                Say(c, step, was != 4 ? 1 : 0, was != 4 ? was : -1);
                break;
            }
            case ContestEffect.DoubledJudge:
                act.Bonus += act.Others * 20;
                Say(c, step, Math.Min(act.Others, 3));
                break;
            case ContestEffect.TwoHeartsWhenVoltageUp:
                if (ContestRules.Voltage(contest.Category, act.Move.Contest!.Type) > 0 && !act.NoVoltageUp)
                {
                    act.Bonus += 20;
                    Say(c, step);
                }
                break;
            case ContestEffect.UniqueJudge:
                if (act.Others == 0) act.Bonus += 30;
                Say(c, step, act.Others == 0 ? 0 : 1);
                break;
            case ContestEffect.ConsecutiveUse:
                // Only a move that isn't itself a repeat may be repeated
                if (act.Move != LastMove[c])
                {
                    repeatable[c] = act.Move;
                    Say(c, step);
                }
                break;
            case ContestEffect.Voltage:
                act.Bonus += voltageBefore[act.Judge];
                Say(c, step);
                break;
            case ContestEffect.AllSameJudge:
                if (act.Others == ContestRules.Contestants - 1) act.Bonus += 150;
                Say(c, step, act.Others == ContestRules.Contestants - 1 ? 0 : 1);
                break;
            case ContestEffect.LowersVoltage:
                if (act.NoVoltageDown || voltageNow.All(v => v == 0)) break;
                for (int j = 0; j < voltageNow.Length; j++)
                    if (voltageNow[j] > 0) voltageNow[j] -= 10;
                Say(c, step);
                break;
            case ContestEffect.DoubleNextTurn:
                act.DoubleNext = true;
                break;
            case ContestEffect.SuppressVoltage:
                for (int p = place; p < Order.Length; p++) acts[Order[p]].NoVoltageUp = true;
                Say(c, step);
                break;
            case ContestEffect.StealVoltage:
                if (before == null || before.VoltageBonus == 0) break;
                act.Bonus += before.VoltageBonus;
                Say(c, step, 0, Order[place - 1]);
                break;
            case ContestEffect.RandomOrder:
            {
                // Four draws, each a place among those not yet given one
                int[] rank = { 0, 1, 2, 3 };
                bool[] given = new bool[4];
                for (int r = 0; r < 4; r++)
                {
                    int pick = contest.Next16() % (4 - r);
                    for (int o = 0; o < 4; o++)
                    {
                        if (given[o]) continue;
                        if (pick == 0)
                        {
                            rank[o] = r;
                            given[o] = true;
                            break;
                        }
                        pick--;
                    }
                }
                for (int o = 0; o < 4; o++)
                {
                    acts[o].OrderKind = 3;
                    acts[o].OrderRank = rank[o];
                }
                Say(c, step);
                break;
            }
            case ContestEffect.DoubleFinalAct:
                if (place != Order.Length - 1) break;
                act.Bonus += act.Points;
                Say(c, step);
                break;
            case ContestEffect.LowVoltageAdvantage:
            {
                int[] worth = { 40, 30, 20, 10, 0, 0 };
                act.Bonus += worth[voltageBefore[act.Judge] / 10];
                Say(c, step);
                break;
            }
            case ContestEffect.FirstPerformanceAdvantage:
                if (place != 0) break;
                act.Bonus += 20;
                Say(c, step);
                break;
            case ContestEffect.FinalPerformanceAdvantage:
                if (place != Order.Length - 1) break;
                act.Bonus += 20;
                Say(c, step);
                break;
            case ContestEffect.NoVoltageDown:
                for (int p = place; p < Order.Length; p++) acts[Order[p]].NoVoltageDown = true;
                Say(c, step);
                break;
            case ContestEffect.TwoVoltageInARowAdvantage:
                if (before == null || act.NoVoltageUp || before.NoVoltageUp) break;
                if (ContestRules.Voltage(contest.Category, act.Move.Contest!.Type) > 0 && ContestRules.Voltage(contest.Category, before.Move.Contest!.Type) > 0)
                {
                    act.Bonus += 30;
                    Say(c, step, 0, Order[place - 1]);
                }
                break;
            case ContestEffect.HighScoreLaterTurn:
                act.Bonus += 10 * (place + 1);
                Say(c, step, place);
                break;
            case ContestEffect.MaxVoltageAdvantage:
                if (before == null || before.VoltageBonus < ContestRules.FullVoltageBonus) break;
                act.Bonus += 30;
                Say(c, step);
                break;
            case ContestEffect.PityPoints:
                // The least of the turn: nobody's points, as last looked at, are under this one's
                for (int o = 0; o < 4; o++)
                    if (o != c && acts[o].Seen < act.Compared) return;
                act.Bonus += 30;
                Say(c, step);
                break;
        }
    }

    // ov17_02243B0C: the lowest score of the turn goes first, and of two level the one who performed later; a move
    // that asked for a place overrides the score
    private int[] NextOrder()
    {
        int[] first = { -30000, -29990, -29980, -29970 }, last = { 29970, 29980, 29990, 30000 }, shuffled = { -10000, -500, 500, 10000 };
        int[] key = new int[4], who = { 0, 1, 2, 3 }, was = new int[4];
        for (int c = 0; c < 4; c++)
        {
            was[c] = Array.IndexOf(Order, c);
            key[c] = acts[c].OrderKind switch
            {
                1 => first[acts[c].OrderRank],
                2 => last[acts[c].OrderRank],
                3 => shuffled[acts[c].OrderRank],
                _ => LastTurn[c]
            };
        }
        for (int i = 0; i < 3; i++)
            for (int j = 3; j > i; j--)
                if (key[j - 1] > key[j] || (key[j - 1] == key[j] && was[j - 1] < was[j]))
                {
                    (key[j - 1], key[j]) = (key[j], key[j - 1]);
                    (who[j - 1], who[j]) = (who[j], who[j - 1]);
                    (was[j - 1], was[j]) = (was[j], was[j - 1]);
                }
        return who;
    }
}
